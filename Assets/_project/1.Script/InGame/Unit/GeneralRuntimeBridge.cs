using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  GeneralRuntimeBridge.cs
//  장군 프리팹 전용 RuntimeBridge.
//
//  병사 스탯 비율:
//    statScaleRatio = 0.4f + CommandPower * 0.01f
//    CommandPower 1~30 기준 → 41%~70%
//
//  병사 진형 (세로 열):
//    전체 높이를 고정(FormationHeight)하고 병사 수로 나눠 간격 산출.
//    병사가 많을수록 자동으로 좁아짐 (최소 0.15, 최대 1.5).
// ============================================================

public class GeneralRuntimeBridge : UnitRuntimeBridge
{
    // ── UI 이벤트 ─────────────────────────────────────────────
    /// <summary>
    /// Initialize() 완료 후 발생. InGameHUD 가 구독해 GeneralPanelUI 를 생성한다.
    /// </summary>
    public static event System.Action<GeneralRuntimeBridge> OnSpawned;

    [Header("병사 설정")]
    [Tooltip("PoolController 에 등록된 병사 풀 키")]
    [SerializeField] string _soldierPoolKey = "Soldier";

    // 병사 진형: 장군 오른쪽에 격자(행 × 열)로 배치
    //   행(Row) — X 축(오른쪽): 장군에서 멀어질수록 뒤열
    //   열(Col) — Y 축(위아래): sqrt(N) 기반 자동 산출, 열 단위 중앙 정렬
    const float ColSpacing = 0.6f; // 병사 간 Y 간격
    const float RowSpacing = 0.7f; // 행 간 X 간격 (오른쪽)

    int       _level;
    UnitGrade _grade;
    UnitJob   _job;
    UnitEntry _unitEntry;   // AddComponents() 에서 GeneralTriggerSetComponent 빌드용

    // ── 패시브 스킬 슬롯 ─────────────────────────────────────
    PassiveSkillType _passive0;
    PassiveSkillType _passive1;
    PassiveSkillType _passive2;
    byte             _activePassiveCount;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// AllySpawner 가 스폰 직후 호출. 등급은 가중치 랜덤으로 자동 결정.
    /// unitEntry 를 전달하면 런 장비 스탯이 SpawnEntity() 전에 반영된다.
    /// </summary>
    public void Initialize(string unitName, int level = 1, UnitEntry unitEntry = null)
    {
        _unitName  = unitName;
        _level     = level;
        _unitEntry = unitEntry;
        // 등급 업그레이드 횟수 반영 (unitEntry 없으면 태생 등급 사용)
        _grade     = unitEntry != null ? unitEntry.Grade : UnitJobRoller.GetBirthGrade(unitName);
        _job      = UnitJobRoller.GetJob(unitName);
        _stat     = GeneralStatRoller.Roll(unitName, _level, _grade);

        // 용병 업그레이드 보너스 (HeroStatResolver와 동일하게 base 스탯 직후 적용)
        if (unitEntry?.SoldierBonus > 0)
            _stat.Add(StatType.SoldierCount, unitEntry.SoldierBonus, "bonus");

        // ── 패시브 스킬 결정 ──────────────────────────────────
        (_passive0, _passive1, _passive2) = PassiveSkillRoller.Roll(_unitName);
        _activePassiveCount               = PassiveSkillRoller.GetActiveSlotCount(_grade);

        // 패시브 스텟 즉시 반영 (SpawnEntity 전에 UnitStat 에 적용)
        var db = PassiveSkillDatabase.Current;
        if (db != null)
        {
            PassiveSkillApplier.ApplyToGeneralStat(_stat, GetActivePassives(), db);

            // TitanGeneral 크기 변경 (풀 재사용 시 이전 스케일 누적 방지: 항상 리셋 후 적용)
            transform.localScale = Vector3.one;
            float scaleMult = PassiveSkillApplier.GetGeneralScaleMultiplier(GetActivePassives(), db);
            if (!Mathf.Approximately(scaleMult, 1f))
                transform.localScale = new Vector3(scaleMult, scaleMult, scaleMult);
        }

        // ── 장비 스탯 적용 (패시브 이후, SpawnEntity 직전) ────
        var equipDb = EquipmentDatabase.Current;
        if (equipDb != null && unitEntry != null)
            EquipmentApplier.ApplyAll(_stat, unitEntry, equipDb);

        // ── 어빌리티 스탯 적용 (장군 대상 All/Job/Range/Unit_General) ──
        var abilityDb    = AbilityDatabase.Current;
        var heldAbilities = UserDataManager.Instance?.Get<RunAbilityData>()?.HeldAbilities;
        if (abilityDb != null && heldAbilities != null)
            AbilityApplier.ApplyToGeneralStat(_stat, _job, heldAbilities, abilityDb);

        // ── 유물 스텟 적용 (영구 보유) ────────────────────────────
        var relicDb        = RelicDatabase.Current;
        var relicInventory = UserDataManager.Instance?.Get<RelicInventoryData>();
        if (relicDb != null && relicInventory != null)
            RelicApplier.ApplyToGeneralStat(_stat, _job, relicInventory, relicDb);

        // 외형 적용 (ECS Entity 생성과 독립적으로 실행)
        GetComponent<UnitAppearanceBridge>()?.ApplyAlly(unitName, _job, _grade);

        SpawnEntity();
        SpawnSoldiers();

        // 배틀 통계 트래커에 장군 등록
        if (TryGetComponent<EntityLink>(out var entityLink) && entityLink.Entity != Unity.Entities.Entity.Null)
            BattleStatsTracker.Instance?.RegisterGeneral(entityLink.Entity, _unitName);

        OnSpawned?.Invoke(this);
    }

    /// <summary>외부에서 롤된 스탯을 읽을 때 사용.</summary>
    public UnitStat GetRolledStat() => _stat;

    // ── UnitRuntimeBridge 구현 ───────────────────────────────

    protected override TeamType GetTeam()     => TeamType.Ally;
    protected override UnitType GetUnitType() => UnitType.General;

    /// <summary>장군 전용 ECS 컴포넌트 추가 — 직업, 원거리 태그, 패시브/액티브 스킬, 발사 요청 버퍼.</summary>
    protected override void AddComponents(EntityManager em, Entity entity)
    {
        em.AddComponentData(entity, new GeneralComponent { CommandRadius = 15f });
        em.AddComponentData(entity, new UnitJobComponent { Job = _job });

        if (_job == UnitJob.Archer || _job == UnitJob.Mage)
        {
            em.AddComponent<RangedTag>(entity);
            em.AddBuffer<ProjectileLaunchRequest>(entity);
        }

        // ── 패시브 슬롯 컴포넌트 ─────────────────────────────
        em.AddComponentData(entity, new GeneralPassiveSetComponent
        {
            Slot0            = _passive0,
            Slot1            = _passive1,
            Slot2            = _passive2,
            ActiveSlotCount  = _activePassiveCount,
        });

        // ── 액티브 스킬: 이름+직업 기반 결정론적 선택 ──────────
        var activeDb  = ActiveSkillDatabase.Current;
        var rolledId  = ActiveSkillRoller.Roll(_unitName, _job, activeDb);
        var skillData = activeDb?.Get(rolledId);

        float baseCooldown = skillData?.Cooldown ?? 15f;
        float cdr          = Mathf.Clamp01(_stat.Get(StatType.SkillCooldownReduce));
        em.AddComponentData(entity, new GeneralActiveSkillComponent
        {
            SkillId           = (int)rolledId,
            EffectValue       = skillData?.EffectValue    ?? 1f,
            EffectRadius      = skillData?.EffectRadius   ?? 0f,
            EffectDuration    = skillData?.EffectDuration ?? 0f,
            Cooldown          = baseCooldown * (1f - cdr),
            CooldownRemaining = 0f,  // 첫 발동은 즉시 가능
        });

        // 실행 이벤트 버퍼 추가 (ActiveSkillCooldownSystem 이 여기에 씀)
        em.AddBuffer<ActiveSkillExecuteEvent>(entity);

        // ── 패시브별 런타임 상태 컴포넌트 ───────────────────
        var passives = GetActivePassives();

        if (PassiveSkillApplier.HasPassive(passives, PassiveSkillType.SoldierDeathEmpower))
        {
            em.AddComponentData(entity, new SoldierDeathEmpowerState { DeathCount = 0 });
            em.AddBuffer<SoldierDeathEvent>(entity);
        }

        if (PassiveSkillApplier.HasPassive(passives, PassiveSkillType.BloodPact))
        {
            em.AddComponentData(entity, new BloodPactState { LastBonusRatio = 0f });
            // BloodPact 는 HitEvent 콜백에서 StatusEffectBuffer 를 사용
            if (!em.HasBuffer<StatusEffectBufferElement>(entity))
                em.AddBuffer<StatusEffectBufferElement>(entity);
        }

        bool needsConditionState = PassiveSkillApplier.HasPassive(passives, PassiveSkillType.IronWill)
                                || PassiveSkillApplier.HasPassive(passives, PassiveSkillType.LastStand);
        if (needsConditionState)
        {
            em.AddComponentData(entity, new PassiveConditionState
            {
                IronWillTriggered   = false,
                LastStandTriggered  = false,
                InitialSoldierCount = Mathf.RoundToInt(_stat.Get(StatType.SoldierCount)),
            });
        }

        // ── 새 트리거 패시브용 공용 버퍼 ───────────────────
        bool needsStackBuf =
            PassiveSkillApplier.HasPassive(passives, PassiveSkillType.StrengthStack) ||
            PassiveSkillApplier.HasPassive(passives, PassiveSkillType.KillMomentum)  ||
            PassiveSkillApplier.HasPassive(passives, PassiveSkillType.KillEmpower);
        if (needsStackBuf)
            em.AddBuffer<CombatStackElement>(entity);

        // StatusEffectBuffer — 새 트리거 패시브 다수가 필요
        bool needsStatusBuf =
            PassiveSkillApplier.HasPassive(passives, PassiveSkillType.DefenseShield)   ||
            PassiveSkillApplier.HasPassive(passives, PassiveSkillType.CounterStrike)   ||
            PassiveSkillApplier.HasPassive(passives, PassiveSkillType.SkillAdrenaline);
        if (needsStatusBuf && !em.HasBuffer<StatusEffectBufferElement>(entity))
            em.AddBuffer<StatusEffectBufferElement>(entity);

        // ── 적 처치 / 스킬 사용 이벤트 버퍼 ── 모든 장군에 추가
        em.AddBuffer<EnemyKillEvent>(entity);
        em.AddBuffer<SkillUseEvent>(entity);

        // SoldierDeathEvent — OnSoldierDeath 트리거 패시브 또는 기존 SoldierDeathEmpower 가 없어도 추가
        // (CombatTriggerSystem 의 OnSoldierDeath 감지를 위해 항상 필요)
        if (!em.HasBuffer<SoldierDeathEvent>(entity))
            em.AddBuffer<SoldierDeathEvent>(entity);

        // ── GeneralTriggerSetComponent 빌드 (장비 + 특수 어빌리티) ──
        var trigSet  = new GeneralTriggerSetComponent();
        var equipDb  = EquipmentDatabase.Current;
        if (equipDb != null && _unitEntry?.RunEquipSlots != null)
        {
            for (int i = 0; i < 2 && i < _unitEntry.RunEquipSlots.Length; i++)
            {
                string id = _unitEntry.RunEquipSlots[i];
                if (string.IsNullOrEmpty(id)) continue;
                trigSet.EquipSlots[i]    = equipDb.Get(id);
                trigSet.EnhanceLevels[i] = (_unitEntry.RunEquipEnhance != null && i < _unitEntry.RunEquipEnhance.Length)
                    ? _unitEntry.RunEquipEnhance[i] : 0;
            }
        }

        var abilityDb2    = AbilityDatabase.Current;
        var heldAbilities2 = UserDataManager.Instance?.Get<RunAbilityData>()?.HeldAbilities;
        if (abilityDb2 != null && heldAbilities2 != null)
        {
            foreach (var aid in heldAbilities2)
            {
                var data = abilityDb2.Get(aid);
                if (data != null && data.Grade == AbilityGrade.Special && data.GetTriggerType() != PassiveTrigger.None)
                    trigSet.TriggerAbilities.Add(data);
            }
        }
        em.AddComponentObject(entity, trigSet);
    }

    /// <summary>풀 재사용 시 스킬 / 조건 상태 초기화 + 장군별 컴포넌트 갱신.</summary>
    protected override void OnEntityReset(EntityManager em, Entity entity)
    {
        // ── 버퍼 초기화 ──────────────────────────────────────────
        if (em.HasBuffer<ProjectileLaunchRequest>(entity))
            em.GetBuffer<ProjectileLaunchRequest>(entity).Clear();

        if (em.HasBuffer<ActiveSkillExecuteEvent>(entity))
            em.GetBuffer<ActiveSkillExecuteEvent>(entity).Clear();

        if (em.HasBuffer<SoldierDeathEvent>(entity))
            em.GetBuffer<SoldierDeathEvent>(entity).Clear();

        if (em.HasBuffer<EnemyKillEvent>(entity))
            em.GetBuffer<EnemyKillEvent>(entity).Clear();

        if (em.HasBuffer<SkillUseEvent>(entity))
            em.GetBuffer<SkillUseEvent>(entity).Clear();

        if (em.HasBuffer<CombatStackElement>(entity))
            em.GetBuffer<CombatStackElement>(entity).Clear();

        // ── 상태 컴포넌트 초기화 ─────────────────────────────────
        if (em.HasComponent<SoldierDeathEmpowerState>(entity))
            em.SetComponentData(entity, new SoldierDeathEmpowerState { DeathCount = 0 });

        if (em.HasComponent<BloodPactState>(entity))
            em.SetComponentData(entity, new BloodPactState { LastBonusRatio = 0f });

        if (em.HasComponent<PassiveConditionState>(entity))
            em.SetComponentData(entity, new PassiveConditionState
            {
                IronWillTriggered   = false,
                LastStandTriggered  = false,
                InitialSoldierCount = Mathf.RoundToInt(_stat.Get(StatType.SoldierCount)),
            });

        // ── 직업 갱신 ────────────────────────────────────────────
        em.SetComponentData(entity, new UnitJobComponent { Job = _job });

        // ── 원거리 태그·버퍼 갱신 ────────────────────────────────
        bool isRanged = _job == UnitJob.Archer || _job == UnitJob.Mage;
        if (isRanged)
        {
            if (!em.HasComponent<RangedTag>(entity))
                em.AddComponent<RangedTag>(entity);
            if (!em.HasBuffer<ProjectileLaunchRequest>(entity))
                em.AddBuffer<ProjectileLaunchRequest>(entity);
        }
        else
        {
            if (em.HasComponent<RangedTag>(entity))
                em.RemoveComponent<RangedTag>(entity);
        }

        // ── 패시브 슬롯 갱신 ─────────────────────────────────────
        em.SetComponentData(entity, new GeneralPassiveSetComponent
        {
            Slot0           = _passive0,
            Slot1           = _passive1,
            Slot2           = _passive2,
            ActiveSlotCount = _activePassiveCount,
        });

        // ── 액티브 스킬 갱신 ─────────────────────────────────────
        var activeDb  = ActiveSkillDatabase.Current;
        var rolledId  = ActiveSkillRoller.Roll(_unitName, _job, activeDb);
        var skillData = activeDb?.Get(rolledId);
        float cdr     = Mathf.Clamp01(_stat.Get(StatType.SkillCooldownReduce));
        em.SetComponentData(entity, new GeneralActiveSkillComponent
        {
            SkillId           = (int)rolledId,
            EffectValue       = skillData?.EffectValue    ?? 1f,
            EffectRadius      = skillData?.EffectRadius   ?? 0f,
            EffectDuration    = skillData?.EffectDuration ?? 0f,
            Cooldown          = (skillData?.Cooldown ?? 15f) * (1f - cdr),
            CooldownRemaining = 0f,
        });

        // ── GeneralTriggerSetComponent 갱신 (managed) ────────────
        var trigSet = em.GetComponentObject<GeneralTriggerSetComponent>(entity);
        if (trigSet != null)
        {
            trigSet.EquipSlots[0]    = null;
            trigSet.EquipSlots[1]    = null;
            trigSet.EnhanceLevels[0] = 0;
            trigSet.EnhanceLevels[1] = 0;
            trigSet.TriggerAbilities.Clear();

            var equipDb = EquipmentDatabase.Current;
            if (equipDb != null && _unitEntry?.RunEquipSlots != null)
            {
                for (int i = 0; i < 2 && i < _unitEntry.RunEquipSlots.Length; i++)
                {
                    string id = _unitEntry.RunEquipSlots[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    trigSet.EquipSlots[i]    = equipDb.Get(id);
                    trigSet.EnhanceLevels[i] = (_unitEntry.RunEquipEnhance != null && i < _unitEntry.RunEquipEnhance.Length)
                        ? _unitEntry.RunEquipEnhance[i] : 0;
                }
            }

            var abilityDb2     = AbilityDatabase.Current;
            var heldAbilities2 = UserDataManager.Instance?.Get<RunAbilityData>()?.HeldAbilities;
            if (abilityDb2 != null && heldAbilities2 != null)
            {
                foreach (var aid in heldAbilities2)
                {
                    var data = abilityDb2.Get(aid);
                    if (data != null && data.Grade == AbilityGrade.Special && data.GetTriggerType() != PassiveTrigger.None)
                        trigSet.TriggerAbilities.Add(data);
                }
            }
        }
    }

    // ── 병사 스폰 ─────────────────────────────────────────────

    void SpawnSoldiers()
    {
        if (string.IsNullOrEmpty(_soldierPoolKey)) return;

        int soldierCount = Mathf.RoundToInt(_stat.Get(StatType.SoldierCount));
        if (soldierCount <= 0) return;

        if (!TryGetComponent<EntityLink>(out var link) || link.Entity == Entity.Null)
        {
            Debug.LogWarning("[GeneralRuntimeBridge] 장군 Entity 없음 — 병사 스폰 취소");
            return;
        }

        // 병사 스탯 비율: 기본 40% + CommandPower 1포인트당 1%
        float commandPower    = _stat.Get(StatType.CommandPower);
        float statScaleRatio  = Mathf.Clamp01(0.4f + commandPower * 0.01f);

        // Y축 열 수: sqrt(N) 기반 자동 산출 — 병사 수에 따라 자연스러운 격자
        int colsY = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(soldierCount)));

        for (int i = 0; i < soldierCount; i++)
        {
            int row           = i / colsY;
            int col           = i % colsY;
            int soldiersInRow = Mathf.Min(colsY, soldierCount - row * colsY);

            // 현재 행 내에서 Y 중앙 정렬
            float yOffset = (col - (soldiersInRow - 1) * 0.5f) * ColSpacing;
            float xOffset = (row + 1) * RowSpacing;  // 장군 오른쪽(+X)

            Vector3 spawnPos = new Vector3(
                transform.position.x + xOffset,
                transform.position.y + yOffset,
                transform.position.z);

            GameObject soldierGO = PoolController.Instance?.Spawn(
                PoolType.Unit, _soldierPoolKey, spawnPos, Quaternion.identity);

            if (soldierGO == null)
            {
                Debug.LogWarning($"[GeneralRuntimeBridge] 병사 스폰 실패 (풀 키: '{_soldierPoolKey}')");
                continue;
            }

            // 병사 생존 카운트 반영 (AliveAllyCount 에 포함되지 않으므로 직접 추가)
            if (BattleManager.Instance != null)
                BattleManager.Instance.OnUnitSpawned(TeamType.Ally);

            if (soldierGO.TryGetComponent<SoldierRuntimeBridge>(out var soldier))
            {
                soldier.Initialize(_soldierPoolKey, _stat, statScaleRatio, link.Entity, _job, _unitName, _grade);

                // 병사에게 패시브 스텟 즉시 적용
                var db = PassiveSkillDatabase.Current;
                if (db != null && soldierGO.TryGetComponent<EntityLink>(out var soldierLink)
                    && soldierLink.Entity != Entity.Null)
                {
                    var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                    if (world != null)
                    {
                        PassiveSkillApplier.ApplyToSoldierEntity(
                            soldierLink.Entity, world.EntityManager,
                            GetActivePassives(), db);

                        // 어빌리티 병사 스텟 적용 (Unit_Soldier 대상)
                        var aDb  = AbilityDatabase.Current;
                        var abls = UserDataManager.Instance?.Get<RunAbilityData>()?.HeldAbilities;
                        if (aDb != null && abls != null)
                            AbilityApplier.ApplyToSoldierEntity(
                                soldierLink.Entity, world.EntityManager, abls, aDb);

                        // 유물 병사 스텟 적용 (Unit_Soldier 대상, 영구)
                        var rDb  = RelicDatabase.Current;
                        var rInv = UserDataManager.Instance?.Get<RelicInventoryData>();
                        if (rDb != null && rInv != null)
                            RelicApplier.ApplyToSoldierEntity(
                                soldierLink.Entity, world.EntityManager, rInv, rDb);
                    }
                }
            }
        }

        var logSkillId   = ActiveSkillRoller.Roll(_unitName, _job, ActiveSkillDatabase.Current);
        var logSkillData = ActiveSkillDatabase.Current?.Get(logSkillId);
        string skillName = logSkillData?.SkillName ?? logSkillId.ToString();

        Debug.Log($"[GeneralRuntimeBridge] '{_unitName}' 스폰 " +
                  $"| Lv:{_level}  등급:{_grade}  직업:{_job}  " +
                  $"HP:{_stat.Get(StatType.MaxHp):F0}  ATK:{_stat.Get(StatType.Attack):F0}  " +
                  $"병사:{soldierCount}명  스탯비율:{statScaleRatio:P0}  " +
                  $"패시브:[{_passive0},{_passive1},{_passive2}] 활성:{_activePassiveCount}슬롯  " +
                  $"액티브스킬:{skillName}({logSkillId})");
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    /// <summary>활성 슬롯 수만큼만 담은 패시브 배열을 반환한다.</summary>
    PassiveSkillType[] GetActivePassives()
    {
        switch (_activePassiveCount)
        {
            case 3: return new[] { _passive0, _passive1, _passive2 };
            case 2: return new[] { _passive0, _passive1 };
            default: return new[] { _passive0 };
        }
    }
}
