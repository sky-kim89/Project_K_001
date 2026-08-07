using BattleGame.Units;
using Unity.Entities;

// ============================================================
//  EquipmentApplier.cs
//  장비 스탯을 UnitStat 레이어에 적용/제거하는 유틸.
//
//  레이어 키: "equip_0", "equip_1" (슬롯 인덱스)
//  장비 스탯은 절대값 — UnitStat.Add() 로 단순 누적
//
//  호출 흐름:
//    스폰 시   → ApplyAll(stat, entry, db)  → SpawnEntity() 직전
//    교체 시   → Apply(stat, equip, slot)  → UpdateEntityBase(entity, em, stat)
//    해제 시   → Remove(stat, slot)         → UpdateEntityBase(entity, em, stat)
//    회귀 시   → UnitData.ClearAllEquipments() 로 UnitEntry 초기화
// ============================================================

public static class EquipmentApplier
{
    public static string SlotKey(int slot) => $"equip_{slot}";

    /// <summary>
    /// 지금 쓸 수 있는 장비 슬롯 수 — 기본 2 + 특성으로 해금한 만큼.
    ///
    /// ⚠ 로비 표시(HeroStatResolver)와 전투 적용(ApplyAll)이 반드시 같은 값을 써야 한다.
    ///   예전에 ApplyAll 이 2로 못 박혀 있어 3번 슬롯 장비의 스탯이
    ///   로비에는 잡히고 전투에는 안 잡혔다 — 용병 수가 서로 달라 보이던 원인.
    /// </summary>
    public static int ActiveSlotCount
        => 2 + TraitApplier.GetEquipSlotBonus(
                   UserDataManager.Instance?.Get<RunTraitData>(), TraitDatabase.Current);

    /// <summary>
    /// 단일 장비를 UnitStat 에 적용.
    /// 같은 슬롯에 기존 장비가 있으면 먼저 제거 후 재적용.
    /// </summary>
    public static void Apply(UnitStat stat, EquipmentData equip, int slot, int enhanceLevel = 0)
    {
        if (equip == null) return;

        string key = SlotKey(slot);
        stat.RemoveKey(key);

        foreach (var entry in equip.StatEntries)
            stat.Add(entry.Stat, equip.GetStatValue(entry, enhanceLevel), key);
    }

    /// <summary>슬롯 장비 스탯 제거.</summary>
    public static void Remove(UnitStat stat, int slot) => stat.RemoveKey(SlotKey(slot));

    /// <summary>
    /// UnitEntry.RunEquipSlots 를 읽어 열려 있는 슬롯을 전부 UnitStat 에 적용.
    /// GeneralRuntimeBridge.Initialize() 에서 SpawnEntity() 직전에 호출.
    /// </summary>
    public static void ApplyAll(UnitStat stat, UnitEntry entry, EquipmentDatabase db)
    {
        if (entry.RunEquipSlots == null) return;

        int slotCount = ActiveSlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            if (i >= entry.RunEquipSlots.Length) break;

            string id = entry.RunEquipSlots[i];
            if (string.IsNullOrEmpty(id)) continue;

            var equip   = db.Get(id);
            int enhance = (entry.RunEquipEnhance != null && i < entry.RunEquipEnhance.Length)
                          ? entry.RunEquipEnhance[i] : 0;
            Apply(stat, equip, i, enhance);
        }
    }

    /// <summary>
    /// 인게임 중 장비 교체/강화 후 ECS StatComponent.Base 를 즉시 갱신.
    /// Final 도 Base 로 리셋해 다음 프레임 버프 재계산에 반영된다.
    /// </summary>
    public static void UpdateEntityBase(Entity entity, EntityManager em, UnitStat stat)
    {
        if (!em.HasComponent<StatComponent>(entity)) return;

        var sc = em.GetComponentData<StatComponent>(entity);
        sc.Base = StatBlock.FromUnitStat(stat);
        sc.ResetFinalToBase();
        em.SetComponentData(entity, sc);
    }
}
