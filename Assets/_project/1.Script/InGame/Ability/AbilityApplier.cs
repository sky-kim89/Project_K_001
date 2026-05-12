using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  AbilityApplier.cs
//  보유 어빌리티를 UnitStat / 병사 ECS 스텟에 적용하는 헬퍼.
//
//  ■ 장군 적용 (ApplyToGeneralStat)
//    All / Job_* / Range_* / Unit_General 대상 어빌리티를 장군 UnitStat 에 반영.
//    같은 스텟에 여러 어빌리티가 붙으면 % 합산 후 1회 적용 (합연산).
//    Unit_Soldier 대상은 제외 → ApplyToSoldierEntity 에서 별도 처리.
//
//  ■ 병사 적용 (ApplyToSoldierEntity)
//    Unit_Soldier 대상 어빌리티를 병사 ECS StatComponent.Base 에 직접 적용.
//    GeneralRuntimeBridge.SpawnSoldiers() 에서 PassiveSkillApplier 직후 호출.
// ============================================================

public static class AbilityApplier
{
    // ── 장군 스텟 적용 ─────────────────────────────────────────

    public static void ApplyToGeneralStat(
        UnitStat stat, UnitJob job,
        IReadOnlyList<AbilityId> ids, AbilityDatabase db)
    {
        if (stat == null || ids == null || db == null || ids.Count == 0) return;

        var bonuses = new Dictionary<StatType, float>();

        foreach (var id in ids)
        {
            var data = db.Get(id);
            if (data == null) continue;
            if (data.Grade == AbilityGrade.Special) continue;   // Special은 OnTrigger에서만 처리
            if (data.Target == AbilityTarget.Unit_Soldier) continue;
            if (!MatchesGeneralTarget(data.Target, job)) continue;

            Accumulate(bonuses, data.Stat1, data.Value1);
            if (data.HasStat2) Accumulate(bonuses, data.Stat2, data.Value2);
        }

        foreach (var kvp in bonuses)
        {
            float delta = stat.Get(kvp.Key) * kvp.Value;
            stat.Add(kvp.Key, delta, "ability");
        }
    }

    // ── 병사 ECS 스텟 적용 ─────────────────────────────────────

    public static void ApplyToSoldierEntity(
        Entity entity, EntityManager em,
        IReadOnlyList<AbilityId> ids, AbilityDatabase db)
    {
        if (ids == null || db == null || ids.Count == 0) return;
        if (entity == Entity.Null || !em.Exists(entity)) return;
        if (!em.HasComponent<StatComponent>(entity)) return;

        var bonuses = new Dictionary<StatType, float>();

        foreach (var id in ids)
        {
            var data = db.Get(id);
            if (data == null || data.Target != AbilityTarget.Unit_Soldier) continue;

            Accumulate(bonuses, data.Stat1, data.Value1);
            if (data.HasStat2) Accumulate(bonuses, data.Stat2, data.Value2);
        }

        if (bonuses.Count == 0) return;

        var  sc      = em.GetComponentData<StatComponent>(entity);
        bool changed = false;

        foreach (var kvp in bonuses)
        {
            float delta = sc.Base[kvp.Key] * kvp.Value;
            sc.Base[kvp.Key] += delta;
            changed = true;
        }

        if (!changed) return;

        sc.Final = sc.Base;
        em.SetComponentData(entity, sc);

        if (bonuses.ContainsKey(StatType.MaxHp))
            em.SetComponentData(entity, new HealthComponent
                { CurrentHp = sc.Base[StatType.MaxHp] });
    }

    // ── 내부 ──────────────────────────────────────────────────

    public static bool MatchesGeneralTarget(AbilityTarget target, UnitJob job)
    {
        switch (target)
        {
            case AbilityTarget.All:               return true;
            case AbilityTarget.Job_Knight:        return job == UnitJob.Knight;
            case AbilityTarget.Job_Archer:        return job == UnitJob.Archer;
            case AbilityTarget.Job_Mage:          return job == UnitJob.Mage;
            case AbilityTarget.Job_ShieldBearer:  return job == UnitJob.ShieldBearer;
            case AbilityTarget.Range_Melee:       return job == UnitJob.Knight || job == UnitJob.ShieldBearer;
            case AbilityTarget.Range_Ranged:      return job == UnitJob.Archer || job == UnitJob.Mage;
            case AbilityTarget.Unit_General:      return true;
            default: return false;
        }
    }

    static void Accumulate(Dictionary<StatType, float> bonuses, StatType type, float value)
    {
        bonuses.TryGetValue(type, out float existing);
        bonuses[type] = existing + value;
    }
}
