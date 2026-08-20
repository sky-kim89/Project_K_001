using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  EnemyRuntimeBridge.cs
//  적 프리팹(Enemy / Elite / Boss) 전용 RuntimeBridge.
//
//  EnemySpawner 가 스폰 직후 Initialize(unitName, unitType, race, level, statMult) 를
//  호출하면 EnemyStatRoller 로 스텟을 생성하고 ECS Entity 를 만든다.
//
//  엘리트: 스킬 ID를 unitName 시드로 결정, GeneralActiveSkillComponent 추가
//  보스:   AoE 반경·넉백·돌진 패턴 필드 초기화
// ============================================================

public class EnemyRuntimeBridge : UnitRuntimeBridge
{
    // 엘리트가 사용할 수 있는 스킬 ID 목록 (단순 공격계 스킬만 선택)
    static readonly int[] EliteSkillPool =
    {
        (int)ActiveSkillId.HeavyStrike,   // 1  강타
        (int)ActiveSkillId.LeapStrike,    // 3  도약강타
        (int)ActiveSkillId.Bind,          // 12 속박
        (int)ActiveSkillId.Berserker,     // 14 광전사
        (int)ActiveSkillId.IronShield,    // 15 철벽방어
        (int)ActiveSkillId.Shockwave,     // 18 충격파
    };

    // 보스가 사용할 수 있는 스킬 — 엘리트보다 판이 크고 광역 위주다.
    // ⚠ 소환·치유 계열은 넣지 않는다
    //   보스전이 늘어지기만 하고, 플레이어가 손쓸 수단이 없는 오토배틀에서는
    //   "언제 끝나나" 만 남는다. 보스는 짧고 아프게 끝나야 한다.
    static readonly int[] BossSkillPool =
    {
        (int)ActiveSkillId.Meteor,        //  9 메테오
        (int)ActiveSkillId.Shockwave,     // 18 충격파
        (int)ActiveSkillId.Blizzard,      // 10 블리자드
        (int)ActiveSkillId.ArrowRain,     // 16 화살비
        (int)ActiveSkillId.Bind,          // 12 속박
    };

    SpawnUnitType _unitType;
    bool          _knockbackImmune;

    // 프리팹 원본 크기. 풀 재사용 시 배율이 누적되지 않도록 항상 이 값을 기준으로 다시 잡는다.
    Vector3 _baseScale;

    void Awake() => _baseScale = transform.localScale;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>EnemySpawner 가 스폰 직후 호출.</summary>
    public void Initialize(string unitName, SpawnUnitType unitType, EnemyRace race,
                           int level = 1, float statMultiplier = 1f, float stageBias = 0f,
                           float scaleMultiplier = 1f, bool knockbackImmune = false)
    {
        _unitName        = unitName;
        _unitType        = unitType;
        _knockbackImmune = knockbackImmune;

        // 크기는 SpawnEntity() 전에 확정해야 한다 — UnitSizeComponent.Radius 가 localScale 에서 나온다
        transform.localScale = _baseScale * Mathf.Max(0.01f, scaleMultiplier);

        _stat     = EnemyStatRoller.Roll(unitName, unitType, level, statMultiplier, stageBias);

        GetComponent<UnitAppearanceBridge>()?.ApplyEnemy(race, unitName);

        // 유물 적 약화 적용 (EnemyMaxHpReduction / EnemyAttackReduction)
        var relicDb  = RelicDatabase.Current;
        var relicInv = UserDataManager.Instance?.Get<RelicInventoryData>();
        if (relicDb != null && relicInv != null)
            RelicApplier.ApplyEnemyWeaken(_stat, relicInv, relicDb);

        SpawnEntity();
    }

    // ── UnitRuntimeBridge 구현 ───────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        _unitType        = default;
        _knockbackImmune = false;
    }

    protected override TeamType GetTeam() => TeamType.Enemy;

    protected override UnitType GetUnitType() => _unitType switch
    {
        SpawnUnitType.Elite => UnitType.Elite,
        SpawnUnitType.Boss  => UnitType.Boss,
        _                   => UnitType.Enemy,
    };

    // ── 타입 전용 컴포넌트 추가 ──────────────────────────────

    protected override void AddComponents(EntityManager em, Entity entity)
    {
        switch (_unitType)
        {
            case SpawnUnitType.Boss:
                em.AddComponentData(entity, MakeBossComponent());

                // ⚠ 보스의 광역 스킬은 '폭주'(불지옥)에서만 준다
                //   BossSkillPool 은 메테오·블리자드·화살비 같은 판 전체를 덮는 스킬을
                //   EffectValue 2.5 / 반경 3.5 로 쓴다. 오토배틀이라 플레이어가 피할
                //   수단이 없어서, 낮은 난이도에서는 이 한 방이 승패를 통째로 정했다.
                //   아래 난이도의 보스는 AoE 평타 + 돌진으로 싸운다.
                if (FrenzyEnabled)
                {
                    em.AddComponentData(entity, new GeneralActiveSkillComponent
                    {
                        // ⚠ GetHashCode 가 아니라 안정 해시다
                        //   문자열 해시 시드는 프로세스마다 바뀐다 — 예전엔 같은 보스인데
                        //   앱을 다시 켤 때마다 스킬이 갈렸다.
                        SkillId           = BossSkillPool[
                                                (int)(UnitJobRoller.StableHash(_unitName) % (uint)BossSkillPool.Length)],
                        EffectValue       = 2.5f,   // 엘리트(1.5)보다 크게
                        EffectRadius      = 3.5f,
                        EffectDuration    = 4.0f,
                        Cooldown          = 16f * CooldownScale,   // 난이도 '각성' 반영
                        CooldownRemaining = 8f,     // 등장 직후 바로 터지면 대응할 여지가 없다
                    });
                }

                // ⚠ 버퍼는 스킬 유무와 상관없이 붙인다
                //   ActiveSkillCooldownSystem 이 AppendToBuffer 로 접근한다.
                //   조건부로 붙이면 없는 쪽에서 터진다.
                em.AddBuffer<ActiveSkillExecuteEvent>(entity);

                // 행동 패턴도 스킬이다. 대표 스킬과 달리 AI 만 발동하는 슬롯에 꽂는다.
                // 돌진은 항상 — 보스를 보스로 보이게 하는 동작이고, 직선 한 번이라
                // 광역 스킬처럼 판을 쓸어버리지 않는다.
                // 분쇄 강타는 '폭주'(불지옥) 난이도에서만.
                var bossSlots = em.AddBuffer<ActiveSkillSlot>(entity);
                bossSlots.Add(PatternSlot(ActiveSkillId.BossCharge, cooldown: 9f, first: 5f));
                if (FrenzyEnabled)
                    bossSlots.Add(PatternSlot(ActiveSkillId.BossSlam, cooldown: 13f, first: 9f));
                break;

            case SpawnUnitType.Elite:
                // unitName 시드로 스킬 결정 (결정적 랜덤 — 같은 이름은 같은 스킬)
                // 안정 해시 — 실행마다 달라지면 "같은 이름은 같은 스킬" 이 깨진다
                int skillIndex = (int)(UnitJobRoller.StableHash(_unitName) % (uint)EliteSkillPool.Length);
                int skillId    = EliteSkillPool[skillIndex];

                em.AddComponentData(entity, new EliteComponent
                {
                    HasSkill            = true,
                    KnockbackResistance = 0.5f,
                });
                em.AddComponentData(entity, new GeneralActiveSkillComponent
                {
                    SkillId           = skillId,
                    EffectValue       = 1.5f,   // 스킬 효과 수치 (스킬마다 의미가 다름)
                    EffectRadius      = 2.0f,
                    EffectDuration    = 3.0f,
                    Cooldown          = 12f * CooldownScale,   // 난이도 '각성' 반영
                    CooldownRemaining = 5f,     // 처음 5초 후 첫 발동
                });
                // ActiveSkillCooldownSystem 이 AppendToBuffer 하므로 버퍼 미리 추가
                em.AddBuffer<ActiveSkillExecuteEvent>(entity);

                // 폭주(무간) 난이도에서 엘리트가 보스의 돌진을 배운다.
                // 슬롯이 따로라 원래 갖고 있던 스킬은 그대로 쓴다.
                if (FrenzyEnabled)
                    em.AddBuffer<ActiveSkillSlot>(entity)
                      .Add(PatternSlot(ActiveSkillId.BossCharge, cooldown: 11f, first: 7f));
                break;
        }
    }

    // ── 난이도 · 패턴 슬롯 ───────────────────────────────────

    /// <summary>'폭주' 디버프가 켜져 있는가 (무간 난이도).</summary>
    static bool FrenzyEnabled => DifficultyConfig.CurrentTier()?.FrenzyPatterns ?? false;

    /// <summary>
    /// '각성' 디버프 — 우두머리 스킬 쿨다운 배율.
    /// ⚠ 0.45 아래로 내려가지 않게 막는다. 더 줄이면 보스 연출이 끝나기도 전에
    ///   다음 스킬이 나가 두 개가 겹쳐 보인다.
    /// </summary>
    static float CooldownScale =>
        Mathf.Max(0.45f, 1f - (DifficultyConfig.CurrentTier()?.BossCooldownCut ?? 0f));

    /// <summary>
    /// 패턴 스킬 슬롯 하나.
    /// first 는 첫 발동까지의 대기 — 등장하자마자 터지면 대응할 여지가 없다.
    /// </summary>
    static ActiveSkillSlot PatternSlot(ActiveSkillId id, float cooldown, float first) => new()
    {
        SkillId           = (int)id,
        EffectValue       = 1f,
        EffectRadius      = 0f,     // 반경은 스킬 SO 가 갖는다 (SlamRadius / HitRadius)
        EffectDuration    = 0f,
        Cooldown          = cooldown,
        CooldownRemaining = first,
    };

    // 풀에서 꺼낸 Entity 는 AddComponents 를 다시 타지 않는다.
    // 페이즈·돌진 타이머·넉백 내성이 지난 보스 값 그대로 남으므로 여기서 다시 찍는다.
    protected override void OnEntityReset(EntityManager em, Entity entity)
    {
        // ⚠ 엘리트도 반드시 지나가야 한다
        //   폭주 난이도에서 돌진 슬롯을 받은 엘리트가 풀에 남아 있다가
        //   낮은 난이도에서 다시 나오면 돌진을 그대로 들고 나온다.
        //   AddComponents 는 풀 재사용 시 다시 타지 않으므로 여기서 맞춰야 한다.
        if (_unitType == SpawnUnitType.Elite)
        {
            ResetPatternSlots(em, entity,
                              FrenzyEnabled ? ActiveSkillId.BossCharge : ActiveSkillId.None,
                              cooldown: 11f, first: 7f);
            ClearCastLock(em, entity);
            return;
        }

        if (_unitType != SpawnUnitType.Boss) return;

        if (em.HasComponent<BossComponent>(entity))
            em.SetComponentData(entity, MakeBossComponent());

        // 스킬 쿨다운도 되돌린다. 안 그러면 풀에서 나온 보스가
        // 지난 판의 남은 쿨다운을 그대로 들고 나와 등장하자마자 스킬을 쏜다.
        if (em.HasComponent<GeneralActiveSkillComponent>(entity))
        {
            var skill = em.GetComponentData<GeneralActiveSkillComponent>(entity);
            skill.CooldownRemaining = 8f;
            em.SetComponentData(entity, skill);
        }

        // 패턴 슬롯은 통째로 다시 만든다.
        // ⚠ 쿨다운만 되돌리면 안 된다 — 난이도가 바뀌면 슬롯 '구성' 자체가 달라진다.
        //   무간에서 잡았던 보스가 풀에 남아 있다가 낮은 난이도에서 다시 나오면
        //   분쇄 강타를 그대로 들고 나온다.
        if (em.HasBuffer<ActiveSkillSlot>(entity))
        {
            var slots = em.GetBuffer<ActiveSkillSlot>(entity);
            slots.Clear();
            slots.Add(PatternSlot(ActiveSkillId.BossCharge, cooldown: 9f, first: 5f));
            if (FrenzyEnabled)
                slots.Add(PatternSlot(ActiveSkillId.BossSlam, cooldown: 13f, first: 9f));
        }

        ClearCastLock(em, entity);
    }

    /// <summary>패턴 슬롯을 지우고 지정한 하나만 다시 넣는다. None 이면 비워만 둔다.</summary>
    static void ResetPatternSlots(EntityManager em, Entity entity,
                                  ActiveSkillId id, float cooldown, float first)
    {
        if (!em.HasBuffer<ActiveSkillSlot>(entity))
        {
            if (id == ActiveSkillId.None) return;
            em.AddBuffer<ActiveSkillSlot>(entity).Add(PatternSlot(id, cooldown, first));
            return;
        }

        var slots = em.GetBuffer<ActiveSkillSlot>(entity);
        slots.Clear();
        if (id != ActiveSkillId.None) slots.Add(PatternSlot(id, cooldown, first));
    }

    /// <summary>연출 도중 죽어 잠금이 남았을 수 있다 — 새로 나올 땐 반드시 풀려 있어야 한다.</summary>
    static void ClearCastLock(EntityManager em, Entity entity)
    {
        if (em.HasComponent<SkillCastLock>(entity))
            em.RemoveComponent<SkillCastLock>(entity);
    }

    BossComponent MakeBossComponent() => new()
    {
        PhaseCount             = 1,
        CurrentPhase           = 1,
        Phase2HpRatio          = 0.5f,
        Phase3HpRatio          = 0.25f,
        CCResistance           = 1f,
        // 기본 보스는 0.8 (완전 면역 → 약간 허용), 무한 보스는 완전 면역
        KnockbackResistance    = _knockbackImmune ? 1f : 0.8f,
        // AoE 공격 설정
        AoeRadius              = 2.5f,
        AoeSplashRatio         = 0.6f,  // 범위 내 60% 피해
        AttackKnockbackForce   = 4.0f,
        AttackKnockbackDuration= 0.25f,
        // 돌진은 ActiveSkillSlot 으로 옮겨갔다 — 여기에 필드를 두지 않는다.
    };
}
