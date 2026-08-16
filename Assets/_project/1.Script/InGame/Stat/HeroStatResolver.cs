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
    public Dictionary<StatType, float> RelicBonuses    = new();
    public Dictionary<StatType, float> TraitBonuses    = new();

    public float Total(StatType stat)
    {
        float b = Base?.Get(stat) ?? 0f;
        float e = EquipBonuses.TryGetValue(stat,   out var ev) ? ev : 0f;
        float p = PassiveBonuses.TryGetValue(stat,  out var pv) ? pv : 0f;
        float a = AbilityBonuses.TryGetValue(stat,  out var av) ? av : 0f;
        float r = RelicBonuses.TryGetValue(stat,    out var rv) ? rv : 0f;
        float t = TraitBonuses.TryGetValue(stat,    out var tv) ? tv : 0f;

        // 쿨감만 곱연산 — 인게임(UnitStat.MultiplyResidual)과 같은 규칙이어야
        // 로비에서 본 수치와 전투에서 적용되는 수치가 일치한다.
        if (stat == StatType.SkillCooldownReduce)
            return CombineResidual(b, e, p, a, r, t);

        return b + e + p + a + r + t;
    }

    /// <summary>1 - Π(1 - v). 출처가 하나면 그 값 그대로.</summary>
    public static float CombineResidual(params float[] values)
    {
        float remain = 1f;
        foreach (float v in values) remain *= (1f - v);
        return 1f - remain;
    }

    public float GetEquip(StatType stat)   => EquipBonuses.TryGetValue(stat,  out var v) ? v : 0f;
    public float GetPassive(StatType stat) => PassiveBonuses.TryGetValue(stat, out var v) ? v : 0f;
    public float GetAbility(StatType stat) => AbilityBonuses.TryGetValue(stat, out var v) ? v : 0f;
    public float GetRelic(StatType stat)   => RelicBonuses.TryGetValue(stat,   out var v) ? v : 0f;
    public float GetTrait(StatType stat)   => TraitBonuses.TryGetValue(stat,   out var v) ? v : 0f;
}

public static class HeroStatResolver
{
    /// <summary>
    /// 기본 스탯 = 등급·레벨 롤 + 장수별 용병 강화(SoldierBonus) + 이벤트 병사 보너스.
    ///
    /// ⚠ 전투(GeneralRuntimeBridge.Initialize)도 반드시 이 함수를 쓴다.
    ///   한쪽에만 항목을 더하면 로비 표시와 실제 소환 수가 어긋난다.
    /// </summary>
    public static UnitStat RollBase(UnitEntry entry)
    {
        var stat = GeneralStatRoller.Roll(entry.UnitName, entry.Level, entry.Grade);

        if (entry.SoldierBonus > 0)
            stat.Add(StatType.SoldierCount, entry.SoldierBonus, "bonus");

        // 이벤트로 얻은 런 영구 병사 (RunEventBonusData) — 예전엔 저장만 되고
        // 어느 쪽에서도 읽지 않아 "병사 +1" 보상이 아무 효과가 없었다.
        int eventSoldiers = UserDataManager.Instance?.Get<RunEventBonusData>()?.ExtraSoldiers ?? 0;
        if (eventSoldiers != 0)
            stat.Add(StatType.SoldierCount, eventSoldiers, "event");

        return stat;
    }

    /// <summary>UnitEntry 를 받아 기본·패시브·장비 보너스를 모두 계산한 결과를 반환.</summary>
    public static HeroStatResult Resolve(UnitEntry entry)
    {
        var result = new HeroStatResult();

        // 1. 기본 스탯 (전투와 같은 함수 — RollBase 참고)
        UnitStat stat = RollBase(entry);
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

        // 3. 장비 보너스 (열린 슬롯 수는 EquipmentApplier 가 소유한다)
        int equipSlotCount = EquipmentApplier.ActiveSlotCount;
        var equipDb = EquipmentDatabase.Current;
        if (equipDb != null && entry.RunEquipSlots != null)
        {
            for (int s = 0; s < equipSlotCount; s++)
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
            // 절대값 스탯(CDR·CritChance·Defense)은 % of base 가 아닌 직접 가산
            foreach (var kvp in ratios)
            {
                float delta = AbilityApplier.IsAbsoluteStat(kvp.Key)
                    ? kvp.Value
                    : result.Total(kvp.Key) * kvp.Value;
                if (UnityEngine.Mathf.Abs(delta) > 0.001f)
                    result.AbilityBonuses[kvp.Key] = delta;
            }
        }

        // 5. 유물 보너스 (어빌리티까지 합산된 스탯 기준 % 증가 — RelicApplier와 동일 방식)
        var relicInventory = UserDataManager.Instance?.Get<RelicInventoryData>();
        var relicDb        = RelicDatabase.Current;
        if (relicInventory != null && relicDb != null)
        {
            UnitJob relicJob = UnitJobRoller.GetJob(entry.UnitName);
            foreach (var (id, level) in relicInventory.OwnedRelics)
            {
                if (level <= 0) continue;
                var data = relicDb.Get(id);
                if (data == null || data.EffectType != RelicEffectType.Stat) continue;
                if (data.Target == AbilityTarget.Unit_Soldier) continue;
                if (!AbilityApplier.MatchesGeneralTarget(data.Target, relicJob)) continue;

                AccumulateRelicStat(result, data.Stat1, data.Value1PerLevel, level, data.IsAbsoluteValue);
                if (data.HasStat2)
                    AccumulateRelicStat(result, data.Stat2, data.Value2PerLevel, level, data.IsAbsoluteValue);
            }
        }

        // 6. 특성 보너스 (런 중 획득한 특성 — RunTraitData 기준)
        var traitDb   = TraitDatabase.Current;
        var traitData = UserDataManager.Instance?.Get<RunTraitData>();
        if (traitDb != null && traitData != null)
        {
            float allStatPenalty = 0f;
            foreach (var type in traitData.AcquiredTraits)
            {
                var td = traitDb.Get(type);
                if (td == null) continue;
                foreach (var fx in td.Effects)
                {
                    if (fx.Stat == StatType.GeneralSlotBonus) continue;
                    if (fx.Stat == StatType.EquipSlotBonus)   continue;
                    if (fx.Stat == StatType.AllStatPenalty)
                    {
                        allStatPenalty += fx.Value;
                        continue;
                    }
                    float delta = fx.IsPercent ? result.Base.Get(fx.Stat) * fx.Value : fx.Value;
                    result.TraitBonuses[fx.Stat] =
                        result.TraitBonuses.TryGetValue(fx.Stat, out var cur) ? cur + delta : delta;
                }
            }
            // 6-b. 스탯 전환 (중갑·거인·속공 등)
            //  ⚠ TraitApplier.ApplyConversions 와 반드시 같은 순서·같은 기준이어야 한다.
            //    · 고정 효과가 전부 들어간 뒤, 전체 감산보다는 앞
            //    · 재료·보상 모두 "전환 이전" 합계 기준 (계산과 반영을 분리)
            //  한쪽만 고치면 로비에 뜬 수치와 실제 전투 수치가 어긋난다.
            List<(StatType to, float delta)> convPending = null;
            foreach (var type in traitData.AcquiredTraits)
            {
                var td = traitDb.Get(type);
                if (td?.Conversions == null) continue;

                foreach (var c in td.Conversions)
                {
                    if (c.PerUnit <= 0f) continue;

                    float units = result.Total(c.From) / c.PerUnit;
                    if (units <= 0f) continue;

                    float delta = result.Total(c.To) * c.Rate * units;
                    if (delta == 0f) continue;

                    (convPending ??= new List<(StatType, float)>()).Add((c.To, delta));
                }
            }
            if (convPending != null)
            {
                foreach (var (to, delta) in convPending)
                    result.TraitBonuses[to] =
                        result.TraitBonuses.TryGetValue(to, out var cur) ? cur + delta : delta;
            }

            if (allStatPenalty > 0f)
            {
                foreach (StatType s in System.Enum.GetValues(typeof(StatType)))
                {
                    if (s == StatType.AllStatPenalty || s == StatType.GeneralSlotBonus || s == StatType.EquipSlotBonus) continue;
                    float total = result.Total(s);
                    if (total <= 0f) continue;
                    float penalty = -total * allStatPenalty;
                    result.TraitBonuses[s] =
                        result.TraitBonuses.TryGetValue(s, out var cur) ? cur + penalty : penalty;
                }
            }
        }

        return result;
    }

    // % 유물은 현재 Total(base+passive+equip+ability+이전 유물)에 대해 계산 — RelicApplier.ApplyStatLine과 동일 순서
    static void AccumulateRelicStat(HeroStatResult result, StatType type, float valuePerLevel, int level, bool isAbsolute)
    {
        float delta = isAbsolute
            ? valuePerLevel * level
            : result.Total(type) * valuePerLevel * level;
        if (UnityEngine.Mathf.Abs(delta) < 0.001f) return;
        result.RelicBonuses[type] = result.RelicBonuses.TryGetValue(type, out var cur) ? cur + delta : delta;
    }
}
