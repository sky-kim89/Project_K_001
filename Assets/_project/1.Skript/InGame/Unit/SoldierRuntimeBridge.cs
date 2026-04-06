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
    float   _statScaleRatio;
    Entity  _generalEntity;
    UnitJob _job;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>GeneralRuntimeBridge 가 병사 스폰 직후 호출.</summary>
    public void Initialize(string unitName, UnitStat generalStat,
                           float statScaleRatio, Entity generalEntity, UnitJob generalJob)
    {
        _unitName       = unitName;
        _statScaleRatio = statScaleRatio;
        _generalEntity  = generalEntity;
        _job            = generalJob;
        _stat           = ScaleFromGeneral(generalStat, statScaleRatio);
        SpawnEntity();
    }

    // ── UnitRuntimeBridge 구현 ───────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        _generalEntity  = Entity.Null;
        _statScaleRatio = 0f;
        _job            = UnitJob.Knight;
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

        if (em.HasBuffer<ProjectileLaunchRequest>(entity))
            em.GetBuffer<ProjectileLaunchRequest>(entity).Clear();
    }

    // ── 내부 ─────────────────────────────────────────────────

    static UnitStat ScaleFromGeneral(UnitStat generalStat, float ratio)
    {
        var scaled = new UnitStat();
        foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
        {
            if (type == StatType.AttackRange || type == StatType.MoveSpeed || type == StatType.AttackSpeed)
            {
                // 배율 미적용 스텟
                float value = generalStat.Get(type);
                if (value != 0f)
                    scaled.Set(type, value);
                continue;
            }
            else
            {
                float value = generalStat.Get(type);
                if (value != 0f)
                    scaled.Set(type, value * ratio);
            }   
        }
        return scaled;
    }
}
