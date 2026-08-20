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
//  ■ 병사 적용은 여기 없다 (SoldierStatApplier 가 모아서 처리)
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
        => ApplyStatRelics(stat, job, inventory, db, generalOnly: false);

    /// <summary>
    /// 장수 전용(Unit_General) 유물만 적용한다.
    ///
    /// ⚠ 공통 출처가 전부 끝난 뒤에 부른다
    ///   먼저 붙이면 뒤에 오는 공통 % 옵션이 장수 전용으로 부풀려진 값을 기준으로
    ///   계산되고, 그 몫은 병사가 물려받는 층에 담긴다 —
    ///   결국 장수 전용 보너스의 일부가 병사에게 새어 들어간다.
    /// </summary>
    public static void ApplyGeneralOnly(
        UnitStat stat, UnitJob job,
        RelicInventoryData inventory, RelicDatabase db)
        => ApplyStatRelics(stat, job, inventory, db, generalOnly: true);

    static void ApplyStatRelics(
        UnitStat stat, UnitJob job,
        RelicInventoryData inventory, RelicDatabase db, bool generalOnly)
    {
        if (stat == null || inventory == null || db == null) return;

        foreach (var (id, level) in inventory.OwnedRelics)
        {
            if (level <= 0) continue;
            var data = db.Get(id);
            if (data == null || data.EffectType != RelicEffectType.Stat) continue;
            if (data.Target == AbilityTarget.Unit_Soldier) continue;
            if (!AbilityApplier.MatchesGeneralTarget(data.Target, job)) continue;

            bool isGeneralOnly = data.Target == AbilityTarget.Unit_General;
            if (isGeneralOnly != generalOnly) continue;

            string layer = isGeneralOnly ? UnitStat.GeneralOnlyKey : "relic";

            ApplyStatLine(stat, data.Stat1, data.Value1PerLevel, level, data.IsAbsoluteValue, layer);
            if (data.HasStat2)
                ApplyStatLine(stat, data.Stat2, data.Value2PerLevel, level, data.IsAbsoluteValue, layer);
        }
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

            // ⚠ 저장된 레벨을 그대로 믿지 않는다
            //   밸런스로 MaxLevel 을 내리면(출병 명령 Lv2 → Lv1) 이미 Lv2 로 저장된
            //   세이브가 그대로 두 배 효과를 낸다. 상한을 여기서 다시 건다.
            //   (Common 은 무한 강화라 상한이 없다 — RelicPopup 의 isInfinite 와 같은 규칙)
            int capped = data.Rarity == RelicRarity.Common
                ? level
                : Mathf.Min(level, data.MaxLevel);

            total += data.SystemValuePerLevel * capped;
        }
        return total;
    }

    /// <summary>
    /// System 타입 유물의 총합 정수 효과 반환 (새로고침 횟수, 선택지 수 등).
    /// </summary>
    public static int GetSystemInt(RelicSystemEffect effect, RelicInventoryData inventory, RelicDatabase db)
        => Mathf.RoundToInt(GetSystemValue(effect, inventory, db));

    /// <summary>기본으로 열려 있는 장수 배치 슬롯 수.</summary>
    public const int BaseGeneralSlots = 2;

    /// <summary>
    /// 현재 활성 장수 배치 슬롯 수. 기본 2칸 + 유물 보너스(출병 명령 Lv1 = +1칸).
    /// </summary>
    public static int GetActiveGeneralSlots(RelicInventoryData inventory, RelicDatabase db)
        => BaseGeneralSlots + GetSystemInt(RelicSystemEffect.GeneralSlotBonus, inventory, db);

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

    /// <summary>
    /// 전투 배속으로 쓸 수 있는 단계 수. 기본 1단계(1× 뿐) + 유물 레벨.
    /// Lv1 → 2단계(1×·2×), Lv2 → 3단계(1×·2×·3×).
    /// ⚠ 배속을 묻는 곳은 전부 여기를 거친다 — TopBarUI 가 직접 세면 유물이 무시된다.
    /// </summary>
    public static int GetBattleSpeedStepCount()
    {
        var udm = UserDataManager.Instance;
        return 1 + GetSystemInt(RelicSystemEffect.BattleSpeedUnlock,
                                udm?.Get<RelicInventoryData>(), RelicDatabase.Current);
    }

    // ── 내부 ──────────────────────────────────────────────────

    static void ApplyStatLine(UnitStat stat, StatType type, float valuePerLevel, int level,
                              bool isAbsolute, string layer = "relic")
    {
        float delta = isAbsolute
            ? valuePerLevel * level
            : stat.Get(type) * valuePerLevel * level;
        stat.Add(type, delta, layer);
    }

    static void Accumulate(Dictionary<StatType, float> dict, StatType type, float value)
    {
        dict.TryGetValue(type, out float existing);
        dict[type] = existing + value;
    }
}
