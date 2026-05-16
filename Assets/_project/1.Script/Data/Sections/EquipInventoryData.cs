using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  EquipInventoryData.cs
//  보유(미장착) 장비 ID 목록 저장 섹션.
//
//  장착 시 → OwnedIds 에서 제거, UnitEntry.RunEquipSlots 로 이동.
//  교체 시 → 기존 장비는 소멸 (OwnedIds 로 반환하지 않음).
//  회귀 시 → RunEquipSlots·인벤토리 모두 초기화 (런 스코프 데이터).
// ============================================================

public class EquipInventoryData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.EquipInventory;

    [Serializable]
    class Raw { public List<string> Ids = new(); }

    Raw _raw = new();

    public IReadOnlyList<string> OwnedIds => _raw.Ids;

    public void Add(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _raw.Ids.Add(id);
    }

    public void Remove(string id) => _raw.Ids.Remove(id);

    public bool Has(string id) => _raw.Ids.Contains(id);

    public string Serialize()              => JsonUtility.ToJson(_raw);
    public void   Deserialize(string json) => _raw = JsonUtility.FromJson<Raw>(json) ?? new Raw();

    public void SetDefaults()
    {
        _raw = new Raw();
    }
}
