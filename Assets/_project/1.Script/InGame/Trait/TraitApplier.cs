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

                if (fx.Stat == StatType.AllStatPenalty)
                {
                    allStatPenalty += fx.Value;
                    continue;
                }

                float delta = fx.IsPercent ? stat.Get(fx.Stat) * fx.Value : fx.Value;
                stat.Add(fx.Stat, delta, "trait");
            }
        }

        if (allStatPenalty <= 0f) return;

        // AllStatPenalty 는 현재 누적 스탯(base+passive+equip+ability+relic+trait) 기준 % 감산
        foreach (StatType s in System.Enum.GetValues(typeof(StatType)))
        {
            if (s == StatType.AllStatPenalty || s == StatType.GeneralSlotBonus || s == StatType.EquipSlotBonus) continue;
            float current = stat.Get(s);
            if (current == 0f) continue;
            stat.Add(s, -current * allStatPenalty, "trait_penalty");
        }
    }
}
