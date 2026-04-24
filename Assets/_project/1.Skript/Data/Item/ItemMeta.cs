using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ItemMeta.cs
//  재화·아이템 메타데이터 정의.
//
//  ItemMetaEntry : 아이템 1종의 기획 데이터 (이름·설명·아이콘·카테고리 등)
//  ItemDatabase  : 전체 메타 목록 ScriptableObject
//
//  사용법:
//    ItemMetaEntry meta = ItemDatabase.Instance.Get(eItem.Gold);
//
//  새 아이템 추가 시:
//    1. eItem enum 에 항목 추가
//    2. ItemDatabase.asset 을 열어 Items 리스트에 메타 항목 추가
// ============================================================

// ── 아이템 1종 메타 ───────────────────────────────────────────

[Serializable]
public class ItemMetaEntry
{
    public eItem        Id;
    public string       DisplayName;
    [TextArea(1, 3)]
    public string       Description;
    public Sprite       Icon;
    public ItemCategory Category;
    [Tooltip("-1 = 무제한")]
    public int          MaxStack = -1;
}

// ── 메타 데이터베이스 SO ──────────────────────────────────────

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "ProjectK/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemMetaEntry> Items = new();

    // ── 런타임 캐시 ──────────────────────────────────────────

    Dictionary<eItem, ItemMetaEntry> _map;

    public void Init()
    {
        _map = new Dictionary<eItem, ItemMetaEntry>(Items.Count);
        foreach (ItemMetaEntry entry in Items)
            _map[entry.Id] = entry;
    }

    public ItemMetaEntry Get(eItem id)
    {
        if (_map == null) Init();
        _map.TryGetValue(id, out ItemMetaEntry entry);
        return entry;
    }

    public string GetDisplayName(eItem id)
    {
        ItemMetaEntry meta = Get(id);
        return meta != null ? meta.DisplayName : id.ToString();
    }

    public Sprite GetIcon(eItem id)
    {
        return Get(id)?.Icon;
    }
}
