using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  TraitApplier.cs
//  런 중 획득한 특성의 Effects 를 장수 UnitStat 에 적용.
//  GeneralRuntimeBridge.Initialize() 에서 유물 적용 직후 호출.
//
//  특수 StatType 처리:
//    GeneralSlotBonus — UnitStat 에 직접 가산하지 않음 (NormalMode 가 집계)
//    AllStatPenalty   — 누적 후 전체 스탯에 % 감산으로 적용
// ============================================================

public static class TraitApplier
{
    public static int GetGeneralSlotBonus(RunTraitData runData, TraitDatabase db)
        => GetSystemBonus(StatType.GeneralSlotBonus, runData, db);

    public static int GetEquipSlotBonus(RunTraitData runData, TraitDatabase db)
        => GetSystemBonus(StatType.EquipSlotBonus, runData, db);

    /// <summary>경험치 획득 가산 비율 (0.2 = +20%). 전투 보상 계산에서 사용.</summary>
    public static float GetExpGainBonus(RunTraitData runData, TraitDatabase db)
    {
        float bonus = 0f;
        if (runData == null || db == null) return bonus;
        foreach (var t in runData.AcquiredTraits)
        {
            var td = db.Get(t);
            if (td?.Effects == null) continue;
            foreach (var fx in td.Effects)
                if (fx.Stat == StatType.ExpGainBonus)
                    bonus += fx.Value;
        }
        return bonus;
    }

    /// <summary>
    /// 이 특성이 현재 런에서 쌓아 둔 스택 수.
    ///
    /// 스택은 장군별로 독립이므로(UnitEntry.RunTraitStacks) 하나의 대표값이 없다.
    /// UI 는 "이 특성이 지금 얼마나 자랐나" 를 보여주면 되므로 배치된 장군 중
    /// 가장 많이 쌓은 값을 돌려준다. 배치가 비어 있으면 보유 장수 전체에서 찾는다.
    /// </summary>
    public static int GetMaxStack(TraitType type)
    {
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (unitData == null) return 0;

        var deployed = UserDataManager.Instance.Get<DeploymentData>()?.GetDeployedUnits();
        bool filterByDeploy = deployed != null && deployed.Count > 0;

        int max = 0;
        foreach (var entry in unitData.Units)
        {
            if (filterByDeploy && !deployed.Contains(entry.UnitName)) continue;
            int s = entry.GetTraitStack(type);
            if (s > max) max = s;
        }
        return max;
    }

    static int GetSystemBonus(StatType target, RunTraitData runData, TraitDatabase db)
    {
        int bonus = 0;
        if (runData == null || db == null) return bonus;
        foreach (var t in runData.AcquiredTraits)
        {
            var td = db.Get(t);
            if (td?.Effects == null) continue;
            foreach (var fx in td.Effects)
                if (fx.Stat == target)
                    bonus += Mathf.RoundToInt(fx.Value);
        }
        return bonus;
    }

    public static void ApplyToGeneralStat(UnitStat stat, RunTraitData runData, TraitDatabase db)
    {
        if (stat == null || runData == null || db == null) return;

        float allStatPenalty = 0f;

        foreach (var type in runData.AcquiredTraits)
        {
            var td = db.Get(type);
            if (td?.Effects == null) continue;

            foreach (var fx in td.Effects)
            {
                if (fx.Stat == StatType.GeneralSlotBonus) continue;
                if (fx.Stat == StatType.EquipSlotBonus)   continue;
                if (fx.Stat == StatType.ExpGainBonus)     continue;

                if (fx.Stat == StatType.AllStatPenalty)
                {
                    allStatPenalty += fx.Value;
                    continue;
                }

                float delta = fx.IsPercent ? stat.Get(fx.Stat) * fx.Value : fx.Value;
                stat.Add(fx.Stat, delta, "trait");
            }
        }

        // 전환은 고정 효과가 전부 들어간 뒤에 계산한다 — 그래야 "이 스탯에
        // 투자한 총량" 이 환산 재료가 된다. 전체 감산(AllStatPenalty)보다는 앞이므로
        // 전환으로 번 몫도 페널티를 똑같이 맞는다.
        ApplyConversions(stat, runData, db);

        if (allStatPenalty <= 0f) return;

        // AllStatPenalty 는 현재 누적 스탯(base+passive+equip+ability+relic+trait) 기준 % 감산
        foreach (StatType s in System.Enum.GetValues(typeof(StatType)))
        {
            if (s == StatType.AllStatPenalty   || s == StatType.GeneralSlotBonus ||
                s == StatType.EquipSlotBonus   || s == StatType.ExpGainBonus) continue;
            float current = stat.Get(s);
            if (current == 0f) continue;
            stat.Add(s, -current * allStatPenalty, "trait_penalty");
        }
    }

    // ── 스탯 전환 ────────────────────────────────────────────
    /// <summary>
    /// TraitData.Conversions 를 적용한다 (중갑·거인·속공 등).
    ///
    /// ⚠ 모든 전환은 "전환 이전" 값을 기준으로 계산한다. 계산과 반영을 분리하지 않으면
    ///   ① 공격력을 올리는 전환 둘이 서로를 증폭시켜 곱이 폭주하고
    ///   ② 특성 획득 순서에 따라 최종 스탯이 달라진다 (로비 표시 ≠ 실제 전투).
    /// </summary>
    static void ApplyConversions(UnitStat stat, RunTraitData runData, TraitDatabase db)
    {
        List<(StatType to, float delta)> pending = null;

        foreach (var type in runData.AcquiredTraits)
        {
            var td = db.Get(type);
            if (td?.Conversions == null) continue;

            foreach (var c in td.Conversions)
            {
                if (c.PerUnit <= 0f) continue;

                float units = stat.Get(c.From) / c.PerUnit;
                if (units <= 0f) continue;

                float delta = stat.Get(c.To) * c.Rate * units;
                if (delta == 0f) continue;

                (pending ??= new List<(StatType, float)>()).Add((c.To, delta));
            }
        }

        if (pending == null) return;

        foreach (var (to, delta) in pending)
            stat.Add(to, delta, "trait_conv");
    }
}
