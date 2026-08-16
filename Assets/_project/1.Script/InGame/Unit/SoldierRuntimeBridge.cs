using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  SoldierRuntimeBridge.cs
//  병사 프리팹 전용 RuntimeBridge.
//
//  GeneralRuntimeBridge 가 병사를 스폰한 뒤
//  Initialize(unitName, generalStat, statScaleRatio, generalEntity) 를 호출한다.
//  스탯은 장군 스탯에 비율을 곱해 산출한다.
// ============================================================

public class SoldierRuntimeBridge : UnitRuntimeBridge
{
    float     _statScaleRatio;
    Entity    _generalEntity;
    UnitJob   _job;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>GeneralRuntimeBridge 가 병사 스폰 직후 호출.</summary>
    public void Initialize(string unitName, UnitStat generalStat,
                           float statScaleRatio, Entity generalEntity,
                           UnitJob generalJob, string generalName, UnitGrade generalGrade)
    {
        _unitName       = unitName;
        _statScaleRatio = statScaleRatio;
        _generalEntity  = generalEntity;
        _job            = generalJob;
        _stat           = ScaleFromGeneral(generalStat, statScaleRatio);

        // 병사 등급 = 장군 등급 - 1 (Normal 하한)
        // Normal  → Normal / Uncommon → Normal / Rare → Uncommon / Unique → Rare / Epic → Unique
        UnitGrade soldierGrade = generalGrade > UnitGrade.Normal
            ? (UnitGrade)((int)generalGrade - 1)
            : UnitGrade.Normal;

        // 외형: 장군 이름 시드 사용 → 같은 장군 소속 병사는 동일 외형
        GetComponent<UnitAppearanceBridge>()?.ApplyAlly(generalName, generalJob, soldierGrade);

        SpawnEntity();
    }

    // ── UnitRuntimeBridge 구현 ───────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        _generalEntity  = Entity.Null;
        _statScaleRatio = 0f;
        _job            = UnitJob.Knight;
        // 외형은 Initialize() 에서 ApplyAlly() 를 통해 항상 새로 설정됨
    }

    protected override TeamType GetTeam()     => TeamType.Ally;
    protected override UnitType GetUnitType() => UnitType.Soldier;

    protected override void AddComponents(EntityManager em, Entity entity)
    {
        em.AddComponentData(entity, new SoldierComponent
        {
            GeneralEntity  = _generalEntity,
            StatScaleRatio = _statScaleRatio,
            IsInitialized  = true,
        });
        em.AddComponentData(entity, new UnitJobComponent { Job = _job });

        if (_job == UnitJob.Archer || _job == UnitJob.Mage)
        {
            em.AddComponent<RangedTag>(entity);
            em.AddBuffer<ProjectileLaunchRequest>(entity);
        }

        ApplyRainFireTrait(em, entity);
    }

    // ── 폭우 사격 (병사도 발동) ───────────────────────────────
    //  AttackHitEvent 버퍼가 있어야 UnitAttackSystem·ProjectileSystem 이
    //  착탄 이벤트를 넣어 준다. 트레이트가 없으면 버퍼도 달지 않는다
    //  — 병사 수십 명에게 쓰지도 않을 버퍼를 붙일 이유가 없다.
    //  실제 스플래시는 TraitRainFireSoldierSystem 이 처리한다.
    void ApplyRainFireTrait(EntityManager em, Entity entity)
    {
        bool hasTrait = UserDataManager.Instance?.Get<RunTraitData>()
                                       ?.HasTrait(TraitType.ArcherRainFire) ?? false;

        if (hasTrait)
        {
            if (!em.HasComponent<TraitRainFireTag>(entity))
                em.AddComponent<TraitRainFireTag>(entity);
            if (!em.HasBuffer<AttackHitEvent>(entity))
                em.AddBuffer<AttackHitEvent>(entity);
            else
                em.GetBuffer<AttackHitEvent>(entity).Clear();
            return;
        }

        // 런 도중 특성을 잃는 경로는 없지만, 풀 재사용은 런을 가로지른다.
        if (em.HasComponent<TraitRainFireTag>(entity))
            em.RemoveComponent<TraitRainFireTag>(entity);
        if (em.HasBuffer<AttackHitEvent>(entity))
            em.GetBuffer<AttackHitEvent>(entity).Clear();
    }

    protected override void OnEntityReset(EntityManager em, Entity entity)
    {
        if (em.HasComponent<SoldierComponent>(entity))
            em.SetComponentData(entity, new SoldierComponent
            {
                GeneralEntity  = _generalEntity,
                StatScaleRatio = _statScaleRatio,
                IsInitialized  = true,
            });

        if (em.HasComponent<UnitJobComponent>(entity))
            em.SetComponentData(entity, new UnitJobComponent { Job = _job });

        // 풀 재사용 시 이전 직업과 현재 직업이 다를 수 있음
        // — RangedTag / ProjectileLaunchRequest 를 현재 직업 기준으로 맞춤
        bool shouldBeRanged = _job == UnitJob.Archer || _job == UnitJob.Mage;
        bool hasRangedTag   = em.HasComponent<RangedTag>(entity);

        if (shouldBeRanged && !hasRangedTag)
        {
            em.AddComponent<RangedTag>(entity);
            em.AddBuffer<ProjectileLaunchRequest>(entity);
        }
        else if (!shouldBeRanged && hasRangedTag)
        {
            em.RemoveComponent<RangedTag>(entity);
            if (em.HasBuffer<ProjectileLaunchRequest>(entity))
                em.GetBuffer<ProjectileLaunchRequest>(entity).Clear();
        }
        else if (shouldBeRanged && em.HasBuffer<ProjectileLaunchRequest>(entity))
        {
            em.GetBuffer<ProjectileLaunchRequest>(entity).Clear();
        }

        ApplyRainFireTrait(em, entity);

        if (em.HasComponent<TauntTag>(entity)) em.RemoveComponent<TauntTag>(entity);
    }

    // ── 내부 ─────────────────────────────────────────────────

    static UnitStat ScaleFromGeneral(UnitStat generalStat, float ratio)
    {
        var scaled = new UnitStat();
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
        {
            float value = generalStat.Get(type);
            if (value == 0f) continue;
            scaled.Set(type, IsUnscaled(type) ? value : value * ratio);
        }
        return scaled;
    }

    // ── 병사 스탯 환산 규칙 (공용) ────────────────────────────
    //  HeroDetailPopup 의 "용병" 탭이 같은 공식을 써서 미리 보여 준다.
    //  ⚠ 여기를 고치면 그 화면 표시도 자동으로 따라간다 — 각자 계산하지 말 것.

    /// <summary>
    /// 병사 스탯 배율 — 기본 20% + 지휘력 1포인트당 +1%p.
    /// 상한 없음(GameplayConfig.SoldierStatRatioMax 가 0 이하일 때) — 지휘력을 쌓으면
    /// 장군 스탯의 100% 를 넘길 수 있다.
    /// </summary>
    public static float StatRatio(float commandPower)
    {
        var   cfg     = GameplayConfig.Current;
        float baseR   = cfg != null ? cfg.SoldierBaseStatRatio        : 0.2f;
        float perCmd  = cfg != null ? cfg.SoldierRatioPerCommandPower : 0.01f;
        float max     = cfg != null ? cfg.SoldierStatRatioMax         : 0f;

        float ratio = Mathf.Max(0f, baseR + commandPower * perCmd);
        return max > 0f ? Mathf.Min(ratio, max) : ratio;
    }

    /// <summary>배율이 적용되지 않는 스탯 — 사거리·이동속도·공격속도는 장군과 같다.</summary>
    public static bool IsUnscaled(StatType type)
        => type == StatType.AttackRange
        || type == StatType.MoveSpeed
        || type == StatType.AttackSpeed;
}
