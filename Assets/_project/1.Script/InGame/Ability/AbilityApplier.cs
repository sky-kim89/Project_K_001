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
//    Unit_Soldier 대상은 제외 → SoldierStatApplier 가 모아서 처리한다.
//
//  ■ 병사 적용은 여기 없다
//    Unit_Soldier 대상은 SoldierStatApplier 가 패시브·유물과 함께 한 번에 적용한다.
//    출처별로 따로 부르면 각자 Base 를 읽으며 고쳐서 적용 순서가 결과를 바꾼다.
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
            if (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery) continue;   // OnTrigger에서만 처리
            if (data.Target == AbilityTarget.Unit_Soldier) continue;
            // 공통(전체·직업·범위)만 여기서 — 장수 전용은 아래 ApplyGeneralOnly 가
            // 별도 레이어에 넣는다. 둘 다 여기서 처리하면 이중 가산이 된다.
            if (!MatchesCommonTarget(data.Target, job)) continue;

            Accumulate(bonuses, data.Stat1, data.Value1);
            if (data.HasStat2) Accumulate(bonuses, data.Stat2, data.Value2);
        }

        foreach (var kvp in bonuses)
        {
            // SkillCooldownReduce·CritChance·Defense 는 기본값이 0이거나 절대값 가산이므로
            // % of base 가 아니라 직접 가산 (RelicApplier.IsAbsoluteValue=true 와 동일 처리)
            float delta = IsAbsoluteStat(kvp.Key) ? kvp.Value : stat.Get(kvp.Key) * kvp.Value;
            stat.Add(kvp.Key, delta, "ability");
        }

        // 장수 전용(Unit_General)은 따로 모아 별도 레이어에 넣는다 — 병사 환산에서 빠진다.
        ApplyGeneralOnly(stat, ids, db);
    }

    /// <summary>
    /// Unit_General 타겟만 UnitStat.GeneralOnlyKey 레이어에 넣는다.
    ///
    /// ⚠ 레이어를 나누는 이유는 병사 때문이다
    ///   병사는 장수 스탯에서 환산되는데, 장수만 지목한 옵션까지 물려받으면
    ///   "장수 강화" 와 "부대 강화" 가 구분되지 않는다.
    ///   GeneralRuntimeBridge 가 CloneWithout(GeneralOnlyKey) 로 이 층만 걷어낸다.
    /// </summary>
    static void ApplyGeneralOnly(UnitStat stat, IReadOnlyList<AbilityId> ids, AbilityDatabase db)
    {
        var bonuses = new Dictionary<StatType, float>();

        foreach (var id in ids)
        {
            var data = db.Get(id);
            if (data == null || data.Target != AbilityTarget.Unit_General) continue;

            Accumulate(bonuses, data.Stat1, data.Value1);
            if (data.HasStat2) Accumulate(bonuses, data.Stat2, data.Value2);
        }

        foreach (var kvp in bonuses)
        {
            float delta = IsAbsoluteStat(kvp.Key) ? kvp.Value : stat.Get(kvp.Key) * kvp.Value;
            stat.Add(kvp.Key, delta, UnitStat.GeneralOnlyKey);
        }
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

    /// <summary>
    /// '공통' 타겟인가 — 장수와 병사 모두에게 닿는 옵션.
    ///
    /// ⚠ MatchesGeneralTarget 과 다르다
    ///   그쪽은 Unit_General 도 true 를 준다 (장수 스탯에 넣어야 하므로).
    ///   병사 쪽 판정에 그걸 그대로 쓰면 '장수 전용' 이 병사에게 새어 들어가
    ///   장수 강화와 부대 강화가 다시 구분되지 않는다.
    /// </summary>
    public static bool MatchesCommonTarget(AbilityTarget target, UnitJob job)
        => target != AbilityTarget.Unit_General
        && target != AbilityTarget.Unit_Soldier
        && MatchesGeneralTarget(target, job);

    /// <summary>
    /// 기본값이 0이거나 절대값(pp) 가산이 필요한 스탯.
    /// RelicApplier.IsAbsoluteValue=true 케이스와 동일한 스탯 집합.
    /// </summary>
    public static bool IsAbsoluteStat(StatType type)
        => type == StatType.SkillCooldownReduce
        || type == StatType.CritChance
        || type == StatType.Defense
        || type == StatType.SoldierCount   // 절대 가산 (+1, +2)
        || type == StatType.CommandPower;  // 절대 가산 (+10, +20)

    // ── 시스템 보너스 조회 (골드·경험치 획득량 증가 어빌리티) ─────

    public static float GetGoldBonusRatio(IReadOnlyList<AbilityId> ids, AbilityDatabase db)
    {
        float total = 0f;
        if (ids == null || db == null) return total;
        foreach (var id in ids)
            if (db.Get(id) is AbilityGoldBonus g) total += g.GoldBonusRatio;
        return total;
    }

    public static float GetExpBonusRatio(IReadOnlyList<AbilityId> ids, AbilityDatabase db)
    {
        float total = 0f;
        if (ids == null || db == null) return total;
        foreach (var id in ids)
            if (db.Get(id) is AbilityExpBonus e) total += e.ExpBonusRatio;
        return total;
    }

    static void Accumulate(Dictionary<StatType, float> bonuses, StatType type, float value)
    {
        bonuses.TryGetValue(type, out float existing);
        bonuses[type] = existing + value;
    }
}
