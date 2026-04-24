using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ItemData.cs
//  재화·아이템 보유 수량 저장 섹션 (ISaveSection).
//
//  보유 데이터:
//  - eItem별 보유 수량 (Currency / Material)
//  - Special 아이템(장군·장비)은 수량 저장 없이 이벤트로 위임
//
//  사용법:
//    var items = UserDataManager.Instance.Get<ItemData>();
//
//    // 단일 획득
//    items.Add(eItem.Gold, 500);
//
//    // 배치 보상 지급 (스테이지 클리어 등)
//    items.AddBatch(new[]
//    {
//        new ItemAmount { Item = eItem.Gold,    Amount = 300 },
//        new ItemAmount { Item = eItem.BattleStone, Amount = 5 },
//        new ItemAmount { Item = eItem.General, Amount = 1, SpecificId = "Knight_Lv1" },
//    });
//
//    // 소비
//    if (items.CanSpend(eItem.Gem, 100))
//        items.Spend(eItem.Gem, 100);
//
//    UserDataManager.Instance.RequestSave();
//
//  Special 아이템 위임:
//    items.OnSpecialItemReceived += (itemType, amount, specificId) => { ... };
// ============================================================

public class ItemData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.ItemData;

    // ── 재화 변경 알림 이벤트 ────────────────────────────────
    // 수량이 바뀐 아이템과 변경 후 수량을 전달한다.
    // Special 아이템(장군·장비)은 수량 저장 없이 아래 이벤트만 발생.

    public static event Action<eItem, int>      OnItemChanged;

    // ── Special 아이템 위임 이벤트 ───────────────────────────
    public event Action<eItem, int, string> OnSpecialItemReceived;

    // ── 내부 직렬화 데이터 ───────────────────────────────────

    ItemRawData _raw = new();

    // ── 조회 ─────────────────────────────────────────────────

    public int Get(eItem item) => _raw.Get(item);

    public bool CanSpend(eItem item, int amount) => Get(item) >= amount;

    // ── 단일 획득 ────────────────────────────────────────────

    public void Add(eItem item, int amount, string specificId = "")
    {
        if (amount <= 0) return;

        if (IsSpecial(item))
        {
            OnSpecialItemReceived?.Invoke(item, amount, specificId);
            return;
        }

        _raw.Add(item, amount);
        OnItemChanged?.Invoke(item, _raw.Get(item));
    }

    // ── 배치 획득 (보상 묶음 처리) ───────────────────────────

    public void AddBatch(IEnumerable<ItemAmount> rewards)
    {
        foreach (ItemAmount reward in rewards)
            Add(reward.Item, reward.Amount, reward.SpecificId);
    }

    // ── 소비 ─────────────────────────────────────────────────

    /// <returns>소비 성공 여부. 잔액 부족이면 false 반환하고 수량 변경 없음.</returns>
    public bool Spend(eItem item, int amount)
    {
        if (!CanSpend(item, amount)) return false;
        _raw.Add(item, -amount);
        OnItemChanged?.Invoke(item, _raw.Get(item));
        return true;
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize() => JsonUtility.ToJson(_raw);

    public void SetDefaults()
    {
        _raw = new ItemRawData();
        _raw.Set(eItem.Gold,    500);
        _raw.Set(eItem.Gem,     30);
        _raw.Set(eItem.Energy,  100);
        _raw.Set(eItem.Stamina, 10);
        NotifyAll();
    }

    public void Deserialize(string json)
    {
        _raw = JsonUtility.FromJson<ItemRawData>(json) ?? new ItemRawData();
        NotifyAll();
    }

    void NotifyAll()
    {
        foreach (eItem item in System.Enum.GetValues(typeof(eItem)))
        {
            if (item == eItem.None || IsSpecial(item)) continue;
            OnItemChanged?.Invoke(item, _raw.Get(item));
        }
    }

    // ── 내부 ─────────────────────────────────────────────────

    static bool IsSpecial(eItem item) => (int)item >= 900;

    // ── 직렬화 전용 내부 클래스 ──────────────────────────────
    // JsonUtility 는 Dictionary 직렬화를 지원하지 않으므로 병렬 List 사용.

    [Serializable]
    class ItemRawData
    {
        public List<int> Keys   = new();
        public List<int> Values = new();

        public int Get(eItem item)
        {
            int idx = Keys.IndexOf((int)item);
            return idx < 0 ? 0 : Values[idx];
        }

        public void Set(eItem item, int value)
        {
            int key = (int)item;
            int idx = Keys.IndexOf(key);
            if (idx < 0)
            {
                Keys.Add(key);
                Values.Add(Mathf.Max(0, value));
            }
            else
            {
                Values[idx] = Mathf.Max(0, value);
            }
        }

        public void Add(eItem item, int delta)
        {
            Set(item, Get(item) + delta);
        }
    }
}

// ── 보상 단위 구조체 ──────────────────────────────────────────

[Serializable]
public struct ItemAmount
{
    public eItem  Item;
    public int    Amount;
    /// <summary>Special 아이템 전용. 특정 장군·장비 ID. 일반 재화는 비워둠.</summary>
    public string SpecificId;
}
