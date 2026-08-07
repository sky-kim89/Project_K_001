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
        // 기본 스탯 조립은 HeroStatResolver 가 소유한다 — 로비 표시와 같은 값이어야 한다
        _stat     = unitEntry != null
                    ? HeroStatResolver.RollBase(unitEntry)
                    : GeneralStatRoller.Roll(unitName, _level, _grade);

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

        // ── 특성 스텟 적용 (런 획득 특성) ─────────────────────
        TraitApplier.ApplyToGeneralStat(
            _stat,
            UserDataManager.Instance?.Get<RunTraitData>(),
            TraitDatabase.Current);

        // ── 방어율 소프트캡 실전 적용 (UI·전투 일치) ─────────────
        // UI(EffectiveDefensePct)와 전투(StatComponent.Final)가 같은 값을 사용하도록
        // 모든 스탯 계산이 끝난 뒤 원시 방어율을 소프트캡 공식으로 덮어씀
        ApplyDefenseSoftCap(_stat);

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
        var rolledId  = ActiveSkillRoller.Roll(_unitName, _job, activeDb, _grade);
        var skillData = activeDb?.Get(rolledId);

        float baseCooldown = skillData?.Cooldown ?? 15f;
        float rawCdr       = Mathf.Clamp(_stat.Get(StatType.SkillCooldownReduce), 0f, GameplayConfig.Current?.CooldownReduceMax ?? 0.9f);
        float effectiveCdr = CalcEffectiveCDR(rawCdr, GameplayConfig.Current?.CooldownReduceMax ?? 0.9f);
        em.AddComponentData(entity, new GeneralActiveSkillComponent
        {
            SkillId           = (int)rolledId,
            EffectValue       = skillData?.EffectValue    ?? 1f,
            EffectRadius      = skillData?.EffectRadius   ?? 0f,
            EffectDuration    = skillData?.EffectDuration ?? 0f,
            Cooldown          = baseCooldown * (1f - effectiveCdr),
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

        // ── 적 처치 / 스킬 사용 / 착탄 이벤트 버퍼 ── 모든 장군에 추가
        em.AddBuffer<EnemyKillEvent>(entity);
        em.AddBuffer<SkillUseEvent>(entity);
        em.AddBuffer<AttackHitEvent>(entity);   // OnAttackLanded 트리거용 — 모든 장군 공통

        // SoldierDeathEvent — OnSoldierDeath 트리거 패시브 또는 기존 SoldierDeathEmpower 가 없어도 추가
        // (CombatTriggerSystem 의 OnSoldierDeath 감지를 위해 항상 필요)
        if (!em.HasBuffer<SoldierDeathEvent>(entity))
            em.AddBuffer<SoldierDeathEvent>(entity);

        // ── GeneralTriggerSetComponent 빌드 (장비 + 특수 어빌리티) ──
        var trigSet  = new GeneralTriggerSetComponent();
        trigSet.ActiveEquipSlots = EquipmentApplier.ActiveSlotCount;
        var equipDb  = EquipmentDatabase.Current;
        if (equipDb != null && _unitEntry?.RunEquipSlots != null)
        {
            for (int i = 0; i < 3 && i < _unitEntry.RunEquipSlots.Length; i++)
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
                if (data != null && (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery) && data.GetTriggerType() != PassiveTrigger.None)
                    trigSet.TriggerAbilities.Add(data);
            }
        }
        // ── 특성 컴포넌트 + 트리거 핸들러 ────────────────────────
        var traitData = UserDataManager.Instance?.Get<RunTraitData>();
        if (traitData != null)
        {
            // 행동 기반 특성 (스택 없음, 고유 핸들러)
            if (traitData.HasTrait(TraitType.KnightHeroReturn))
                em.AddComponentData(entity, new TraitHeroReturnComponent());
            if (traitData.HasTrait(TraitType.ArcherRetreatFire))
                em.AddComponent<TraitRetreatFireTag>(entity);
            if (traitData.HasTrait(TraitType.ArcherRainFire))
            {
                em.AddComponent<TraitRainFireTag>(entity);
                trigSet.TraitTriggers.Add(new TraitRainFireHandler());
            }
            if (traitData.HasTrait(TraitType.ShieldCounterBlow))
            {
                em.AddComponent<TraitCounterBlowTag>(entity);
                em.AddComponentData(entity, new MirrorArmorComponent { ReflectRatio = 1.0f });
            }
            if (traitData.HasTrait(TraitType.MageAttackCdr))
            {
                em.AddComponent<TraitAttackCdrTag>(entity);
                trigSet.TraitTriggers.Add(new TraitAttackCdrHandler());
                Debug.Log($"[MageAttackCdr] 핸들러 등록 완료 — {_unitName}");
            }
            else
            {
                Debug.Log($"[MageAttackCdr] 특성 없음 — {_unitName} (HasTrait={traitData?.HasTrait(TraitType.MageAttackCdr)})");
            }
            if (traitData.HasTrait(TraitType.MageEchoSkill))
            {
                em.AddComponentData(entity, new TraitEchoSkillComponent());
                trigSet.TraitTriggers.Add(new TraitEchoSkillHandler());
            }

            // 스택 누적 특성 → TraitStackHandler 범용 등록
            var traitDb = TraitDatabase.Current;
            foreach (var acquired in traitData.AcquiredTraits)
            {
                var td = traitDb?.Get(acquired);
                if (td == null || td.StackTrigger == PassiveTrigger.None) continue;
                if (td.StackTrigger == PassiveTrigger.StageClear) continue; // BattleManager 처리
                trigSet.TraitTriggers.Add(new TraitStackHandler(acquired, td.StackTrigger));
            }
        }

        // 이미 누적된 스택 보너스 적용 (런 중간에 재스폰되는 경우 대비)
        ApplyAccumulatedTraitStacks(em, entity);

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

        if (em.HasBuffer<AttackHitEvent>(entity))
            em.GetBuffer<AttackHitEvent>(entity).Clear();

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

        if (em.HasComponent<TauntTag>(entity)) em.RemoveComponent<TauntTag>(entity);

        // ── 특성 컴포넌트 상태 초기화 ───────────────────────────────
        if (em.HasComponent<TraitHeroReturnComponent>(entity))
            em.SetComponentData(entity, new TraitHeroReturnComponent());
        if (em.HasComponent<TraitEchoSkillComponent>(entity))
            em.SetComponentData(entity, new TraitEchoSkillComponent());

        // 스테이지 시작마다 누적 스택 보너스 재적용 (StatusEffectBuffer 클리어 이후 실행)
        ApplyAccumulatedTraitStacks(em, entity);

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
        var rolledId  = ActiveSkillRoller.Roll(_unitName, _job, activeDb, _grade);
        var skillData = activeDb?.Get(rolledId);
        float rawCdr2       = Mathf.Clamp(_stat.Get(StatType.SkillCooldownReduce), 0f, GameplayConfig.Current?.CooldownReduceMax ?? 0.9f);
        float effectiveCdr2 = CalcEffectiveCDR(rawCdr2, GameplayConfig.Current?.CooldownReduceMax ?? 0.9f);
        em.SetComponentData(entity, new GeneralActiveSkillComponent
        {
            SkillId           = (int)rolledId,
            EffectValue       = skillData?.EffectValue    ?? 1f,
            EffectRadius      = skillData?.EffectRadius   ?? 0f,
            EffectDuration    = skillData?.EffectDuration ?? 0f,
            Cooldown          = (skillData?.Cooldown ?? 15f) * (1f - effectiveCdr2),
            CooldownRemaining = 0f,
        });

        // ── GeneralTriggerSetComponent 갱신 (managed) ────────────
        var trigSet = em.GetComponentObject<GeneralTriggerSetComponent>(entity);
        if (trigSet != null)
        {
            trigSet.EquipSlots[0]    = null;
            trigSet.EquipSlots[1]    = null;
            trigSet.EquipSlots[2]    = null;
            trigSet.EnhanceLevels[0] = 0;
            trigSet.EnhanceLevels[1] = 0;
            trigSet.EnhanceLevels[2] = 0;
            trigSet.ActiveEquipSlots = EquipmentApplier.ActiveSlotCount;
            trigSet.TriggerAbilities.Clear();
            trigSet.TraitTriggers.Clear();

            var equipDb = EquipmentDatabase.Current;
            if (equipDb != null && _unitEntry?.RunEquipSlots != null)
            {
                for (int i = 0; i < 3 && i < _unitEntry.RunEquipSlots.Length; i++)
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
                    if (data != null && (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery) && data.GetTriggerType() != PassiveTrigger.None)
                        trigSet.TriggerAbilities.Add(data);
                }
            }

            // ── 특성 트리거 핸들러 재등록 ─────────────────────────
            var traitData2 = UserDataManager.Instance?.Get<RunTraitData>();
            if (traitData2 != null)
            {
                // 행동 기반
                if (traitData2.HasTrait(TraitType.ArcherRainFire))
                    trigSet.TraitTriggers.Add(new TraitRainFireHandler());
                if (traitData2.HasTrait(TraitType.MageAttackCdr))
                    trigSet.TraitTriggers.Add(new TraitAttackCdrHandler());
                if (traitData2.HasTrait(TraitType.MageEchoSkill))
                    trigSet.TraitTriggers.Add(new TraitEchoSkillHandler());

                // 스택 누적 특성 → 범용 재등록
                var traitDb2 = TraitDatabase.Current;
                foreach (var acquired in traitData2.AcquiredTraits)
                {
                    var td = traitDb2?.Get(acquired);
                    if (td == null || td.StackTrigger == PassiveTrigger.None) continue;
                    if (td.StackTrigger == PassiveTrigger.StageClear) continue;
                    trigSet.TraitTriggers.Add(new TraitStackHandler(acquired, td.StackTrigger));
                }
            }
        }
    }

    // ── 스택 누적 보너스 재적용 ──────────────────────────────────
    // StatusEffectBuffer 클리어 후 호출. 누적 스택 × StackStatBonuses 를 Duration=-1 로 추가.
    // 이 장군의 UnitEntry 에서 스택을 읽어 이 장군에게만 적용 (장군별 독립).
    void ApplyAccumulatedTraitStacks(EntityManager em, Entity entity)
    {
        if (_unitEntry == null) return;
        if (!em.HasBuffer<StatusEffectBufferElement>(entity)) return;
        if (!em.HasComponent<StatComponent>(entity)) return;

        var runData  = UserDataManager.Instance?.Get<RunTraitData>();
        if (runData == null) return;

        var traitDb  = TraitDatabase.Current;
        var stat     = em.GetComponentData<StatComponent>(entity);
        var buf      = em.GetBuffer<StatusEffectBufferElement>(entity);

        foreach (var traitType in runData.AcquiredTraits)
        {
            int stacks = _unitEntry.GetTraitStack(traitType);
            if (stacks <= 0) continue;

            var td = traitDb?.Get(traitType);
            if (td == null || td.StackStatBonuses == null || td.StackStatBonuses.Length == 0) continue;

            foreach (var entry in td.StackStatBonuses)
            {
                float bonusPer = entry.IsPercent ? stat.Base[entry.Stat] * entry.Value : entry.Value;
                buf.Add(new StatusEffectBufferElement
                {
                    Stat       = entry.Stat,
                    Delta      = bonusPer * stacks,
                    Mode       = EffectMode.Add,
                    Duration   = -1f,
                    Remaining  = -1f,
                    SourceType = BuffSourceType.Passive,
                    SourceId   = (int)traitType,
                });
            }
        }
    }

    // ── 병사 스폰 ─────────────────────────────────────────────

    public void SpawnSoldiers()
    {
        if (string.IsNullOrEmpty(_soldierPoolKey)) return;

        int soldierCount = Mathf.RoundToInt(_stat.Get(StatType.SoldierCount));
        if (soldierCount <= 0) return;

        if (!TryGetComponent<EntityLink>(out var link) || link.Entity == Entity.Null)
        {
            Debug.LogWarning("[GeneralRuntimeBridge] 장군 Entity 없음 — 병사 스폰 취소");
            return;
        }

        // 병사 스탯 비율 — 공식은 SoldierRuntimeBridge 가 소유한다
        // (HeroDetailPopup "용병" 탭도 같은 함수를 쓴다)
        float statScaleRatio = SoldierRuntimeBridge.StatRatio(_stat.Get(StatType.CommandPower));

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

    /// <summary>
    /// 모든 스탯 계산 후 방어율에 소프트캡을 적용한다.
    /// UI(StatDisplayHelper.EffectiveDefensePct)와 실전 전투(StatComponent.Final)가 동일한 값을 갖는다.
    /// </summary>
    static void ApplyDefenseSoftCap(UnitStat stat)
    {
        var cfg = GameplayConfig.Current;
        if (cfg == null) return;

        float raw       = stat.Get(StatType.Defense);
        float effective = raw <= cfg.DefenseMax
            ? raw
            : cfg.DefenseMax + (raw - cfg.DefenseMax) * cfg.DefenseOverflowRate;
        effective = Mathf.Min(effective, cfg.DefenseEffectiveCap);

        if (Mathf.Abs(raw - effective) > 0.0001f)
            stat.Set(StatType.Defense, effective, UnitStat.BaseKey);
    }

    /// <summary>
    /// 쿨다운 감소 체감 공식.
    /// rawCDR 이 maxCDR 에 도달하면 effectiveCDR 도 maxCDR 에 도달한다(최상 달성 보장).
    /// 중간 값은 선형보다 낮게 반환해 초반 CDR 축적 효율을 떨어뜨린다.
    ///
    /// 예: maxCDR=0.9, power=1.5
    ///   raw 30% → effective 17%  (linear 30%)
    ///   raw 60% → effective 49%  (linear 60%)
    ///   raw 90% → effective 90%  (linear 90%)
    /// </summary>
    public static float CalcEffectiveCDR(float rawCDR, float maxCDR)
    {
        if (maxCDR <= 0f || rawCDR <= 0f) return 0f;
        const float power = 1.5f;
        return Mathf.Pow(rawCDR / maxCDR, power) * maxCDR;
    }
}
