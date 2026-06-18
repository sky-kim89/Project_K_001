using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  RelicApplier.cs
//  보유 유물을 UnitStat / 병사 ECS 스텟 / 시스템 보너스에 반영.
//
//  ■ 장군 스텟 적용 (ApplyToGeneralStat)
//    Stat 타입 유물 중 대상 조건이 맞는 것을 장군 UnitStat 에 합산.
//    IsAbsoluteValue=false → base × valuePerLevel × level 을 가산
//    IsAbsoluteValue=true  → valuePerLevel × level 을 직접 가산
//
//  ■ 병사 ECS 스텟 적용 (ApplyToSoldierEntity)
//    Unit_Soldier 대상 유물을 병사 ECS StatComponent.Base 에 반영.
//    GeneralRuntimeBridge.SpawnSoldiers() 에서 PassiveSkillApplier 직후 호출.
//
//  ■ 시스템 값 조회 (GetSystemValue / GetSystemInt)
//    System 타입 유물의 총합 효과를 반환.
//    AbilityPicker, InGameManager, 보상 시스템에서 참조.
//
//  ■ 적 약화 적용 (ApplyEnemyWeaken)
//    EnemyRuntimeBridge.Initialize() 에서 SpawnEntity() 직전에 호출.
// ============================================================

public static class RelicApplier
{
    // ── 장군 스텟 적용 ─────────────────────────────────────────

    public static void ApplyToGeneralStat(
        UnitStat stat, UnitJob job,
        RelicInventoryData inventory, RelicDatabase db)
    {
        if (stat == null || inventory == null || db == null) return;

        foreach (var (id, level) in inventory.OwnedRelics)
        {
            if (level <= 0) continue;
            var data = db.Get(id);
            if (data == null || data.EffectType != RelicEffectType.Stat) continue;
            if (data.Target == AbilityTarget.Unit_Soldier) continue;
            if (!AbilityApplier.MatchesGeneralTarget(data.Target, job)) continue;

            ApplyStatLine(stat, data.Stat1, data.Value1PerLevel, level, data.IsAbsoluteValue);
            if (data.HasStat2)
                ApplyStatLine(stat, data.Stat2, data.Value2PerLevel, level, data.IsAbsoluteValue);
        }
    }

    // ── 병사 ECS 스텟 적용 ─────────────────────────────────────

    public static void ApplyToSoldierEntity(
        Entity entity, EntityManager em,
        RelicInventoryData inventory, RelicDatabase db)
    {
        if (inventory == null || db == null) return;
        if (entity == Entity.Null || !em.Exists(entity)) return;
        if (!em.HasComponent<StatComponent>(entity)) return;

        // 비율 보너스와 절대값 보너스를 분리해 누적
        var ratios    = new Dictionary<StatType, float>();
        var absolutes = new Dictionary<StatType, float>();

        foreach (var (id, level) in inventory.OwnedRelics)
        {
            if (level <= 0) continue;
            var data = db.Get(id);
            if (data == null || data.EffectType != RelicEffectType.Stat) continue;
            if (data.Target != AbilityTarget.Unit_Soldier) continue;

            Accumulate(data.IsAbsoluteValue ? absolutes : ratios,
                data.Stat1, data.Value1PerLevel * level);
            if (data.HasStat2)
                Accumulate(data.IsAbsoluteValue ? absolutes : ratios,
                    data.Stat2, data.Value2PerLevel * level);
        }

        if (ratios.Count == 0 && absolutes.Count == 0) return;

        var sc = em.GetComponentData<StatComponent>(entity);

        foreach (var kvp in ratios)
            sc.Base[kvp.Key] += sc.Base[kvp.Key] * kvp.Value;

        foreach (var kvp in absolutes)
            sc.Base[kvp.Key] += kvp.Value;

        sc.Final = sc.Base;
        em.SetComponentData(entity, sc);

        if (ratios.ContainsKey(StatType.MaxHp) || absolutes.ContainsKey(StatType.MaxHp))
            em.SetComponentData(entity, new HealthComponent
                { CurrentHp = sc.Base[StatType.MaxHp] });
    }

    // ── 적 약화 적용 ───────────────────────────────────────────

    /// <summary>
    /// 적 UnitStat 에 유물 약화 효과를 적용한다.
    /// EnemyRuntimeBridge.Initialize() 에서 SpawnEntity() 직전에 호출.
    /// </summary>
    public static void ApplyEnemyWeaken(UnitStat stat, RelicInventoryData inventory, RelicDatabase db)
    {
        if (stat == null || inventory == null || db == null) return;

        float hpRatio  = GetSystemValue(RelicSystemEffect.EnemyMaxHpReduction,  inventory, db);
        float atkRatio = GetSystemValue(RelicSystemEffect.EnemyAttackReduction, inventory, db);

        if (hpRatio > 0f)
            stat.Add(StatType.MaxHp,   -stat.Get(StatType.MaxHp)  * Mathf.Clamp01(hpRatio),  "relic_weaken");
        if (atkRatio > 0f)
            stat.Add(StatType.Attack,  -stat.Get(StatType.Attack) * Mathf.Clamp01(atkRatio), "relic_weaken");
    }

    // ── 시스템 값 조회 ─────────────────────────────────────────

    /// <summary>
    /// System 타입 유물의 총합 효과 비율 반환 (0.15 = 15%).
    /// </summary>
    public static float GetSystemValue(RelicSystemEffect effect, RelicInventoryData inventory, RelicDatabase db)
    {
        if (inventory == null || db == null) return 0f;
        float total = 0f;

        foreach (var (id, level) in inventory.OwnedRelics)
        {
            if (level <= 0) continue;
            var data = db.Get(id);
            if (data == null || data.EffectType != RelicEffectType.System) continue;
            if (data.SystemEffect != effect) continue;
            total += data.SystemValuePerLevel * level;
        }
        return total;
    }

    /// <summary>
    /// System 타입 유물의 총합 정수 효과 반환 (새로고침 횟수, 선택지 수 등).
    /// </summary>
    public static int GetSystemInt(RelicSystemEffect effect, RelicInventoryData inventory, RelicDatabase db)
        => Mathf.RoundToInt(GetSystemValue(effect, inventory, db));

    /// <summary>
    /// 현재 활성 장수 배치 슬롯 수. 기본 1칸 + 유물 보너스.
    /// </summary>
    public static int GetActiveGeneralSlots(RelicInventoryData inventory, RelicDatabase db)
        => 1 + GetSystemInt(RelicSystemEffect.GeneralSlotBonus, inventory, db);

    /// <summary>
    /// 유물 + 특성 보너스를 합산한 최종 장수 슬롯 수 (최대 5).
    /// 슬롯 관련 UI·로직에서 이 메서드 하나만 호출할 것.
    /// </summary>
    public static int GetTotalActiveGeneralSlots()
    {
        var udm = UserDataManager.Instance;
        return Mathf.Min(
            GetActiveGeneralSlots(udm?.Get<RelicInventoryData>(), RelicDatabase.Current)
            + TraitApplier.GetGeneralSlotBonus(udm?.Get<RunTraitData>(), TraitDatabase.Current),
            5);
    }

    // ── 내부 ──────────────────────────────────────────────────

    static void ApplyStatLine(UnitStat stat, StatType type, float valuePerLevel, int level, bool isAbsolute)
    {
        float delta = isAbsolute
            ? valuePerLevel * level
            : stat.Get(type) * valuePerLevel * level;
        stat.Add(type, delta, "relic");
    }

    static void Accumulate(Dictionary<StatType, float> dict, StatType type, float value)
    {
        dict.TryGetValue(type, out float existing);
        dict[type] = existing + value;
    }
}
