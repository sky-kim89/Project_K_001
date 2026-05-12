using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  HeroStatResolver.cs
//  장수 스탯을 기본 → 패시브 → 장비 순으로 계산하는 단일 진입점.
//  HeroCardUI, HeroPanelUI 등 모든 UI가 이 결과를 사용한다.
//
//  사용:
//    HeroStatResult r = HeroStatResolver.Resolve(entry);
//    float hp = r.Total(StatType.MaxHp);
//    // 스탯 클릭 분해: r.Base / r.GetEquip() / r.GetPassive()
// ============================================================

public class HeroStatResult
{
    public UnitStat                    Base            = new();
    public Dictionary<StatType, float> EquipBonuses    = new();
    public Dictionary<StatType, float> PassiveBonuses  = new();
    public Dictionary<StatType, float> AbilityBonuses  = new();

    public float Total(StatType stat)
    {
        float b = Base?.Get(stat) ?? 0f;
        float e = EquipBonuses.TryGetValue(stat,   out var ev) ? ev : 0f;
        float p = PassiveBonuses.TryGetValue(stat,  out var pv) ? pv : 0f;
        float a = AbilityBonuses.TryGetValue(stat,  out var av) ? av : 0f;
        return b + e + p + a;
    }

    public float GetEquip(StatType stat)   => EquipBonuses.TryGetValue(stat,  out var v) ? v : 0f;
    public float GetPassive(StatType stat) => PassiveBonuses.TryGetValue(stat, out var v) ? v : 0f;
    public float GetAbility(StatType stat) => AbilityBonuses.TryGetValue(stat, out var v) ? v : 0f;
}

public static class HeroStatResolver
{
    /// <summary>UnitEntry 를 받아 기본·패시브·장비 보너스를 모두 계산한 결과를 반환.</summary>
    public static HeroStatResult Resolve(UnitEntry entry)
    {
        var result = new HeroStatResult();

        // 1. 기본 스탯 + 용병 업그레이드 보너스
        UnitStat stat = GeneralStatRoller.Roll(entry.UnitName, entry.Level, entry.Grade);
        if (entry.SoldierBonus > 0)
            stat.Add(StatType.SoldierCount, entry.SoldierBonus, "bonus");
        result.Base = stat;

        // 2. 패시브 보너스 (TriggerType == None & Target == General 만 적용)
        var passiveDb = PassiveSkillDatabase.Current;
        if (passiveDb != null)
        {
            var (p0, p1, p2) = PassiveSkillRoller.Roll(entry.UnitName);
            byte pSlots = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
            PassiveSkillType[] pts = { p0, p1, p2 };
            for (int pi = 0; pi < pSlots; pi++)
            {
                var pd = passiveDb.Get(pts[pi]);
                if (pd == null || pd.TriggerType != PassiveTrigger.None) continue;
                foreach (var e in pd.StatModifiers)
                {
                    if (e.Target != PassiveSkillApplier.ApplyTarget.General) continue;
                    float delta = e.IsPercent ? stat.Get(e.Stat) * e.Delta : e.Delta;
                    result.PassiveBonuses[e.Stat] =
                        result.PassiveBonuses.TryGetValue(e.Stat, out var cur) ? cur + delta : delta;
                }
            }
        }

        // 3. 장비 보너스
        var equipDb = EquipmentDatabase.Current;
        if (equipDb != null && entry.RunEquipSlots != null)
        {
            for (int s = 0; s < 2; s++)
            {
                string eid = s < entry.RunEquipSlots.Length ? entry.RunEquipSlots[s] : "";
                if (string.IsNullOrEmpty(eid)) continue;
                var equip = equipDb.Get(eid);
                if (equip == null) continue;
                int enhance = (entry.RunEquipEnhance != null && s < entry.RunEquipEnhance.Length)
                              ? entry.RunEquipEnhance[s] : 0;
                foreach (var e in equip.StatEntries)
                {
                    float val = equip.GetStatValue(e, enhance);
                    result.EquipBonuses[e.Stat] =
                        result.EquipBonuses.TryGetValue(e.Stat, out var cur) ? cur + val : val;
                }
            }
        }

        // 4. 어빌리티 보너스 (base+passive+equip 합산 기준 % 증가 — AbilityApplier와 동일 방식)
        var abilityDb     = AbilityDatabase.Current;
        var heldAbilities = UserDataManager.Instance?.Get<RunAbilityData>()?.HeldAbilities;
        if (abilityDb != null && heldAbilities?.Count > 0)
        {
            UnitJob job = UnitJobRoller.GetJob(entry.UnitName);

            // 비율 합산 (StatType별)
            var ratios = new Dictionary<StatType, float>();
            foreach (var id in heldAbilities)
            {
                var data = abilityDb.Get(id);
                if (data == null || data.Grade == AbilityGrade.Special) continue;
                if (data.Target == AbilityTarget.Unit_Soldier) continue;
                if (!AbilityApplier.MatchesGeneralTarget(data.Target, job)) continue;

                ratios[data.Stat1] = ratios.GetValueOrDefault(data.Stat1) + data.Value1;
                if (data.HasStat2)
                    ratios[data.Stat2] = ratios.GetValueOrDefault(data.Stat2) + data.Value2;
            }

            // 이 시점 result.Total() = base + passive + equip (어빌리티 미포함) → % 기준 동일
            foreach (var kvp in ratios)
            {
                float delta = result.Total(kvp.Key) * kvp.Value;
                if (UnityEngine.Mathf.Abs(delta) > 0.001f)
                    result.AbilityBonuses[kvp.Key] = delta;
            }
        }

        return result;
    }
}
