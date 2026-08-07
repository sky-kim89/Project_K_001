using System;
using UnityEngine;

// ============================================================
//  RunShopData.cs
//  런 상점 상태 저장 섹션.
//
//  ShopSeed     : 장비·장수 픽업에 사용하는 결정론적 시드.
//                 스테이지 클리어·새로고침 시 재생성.
//  RefreshCount : 이번 스테이지에서 새로고침한 횟수 (비용 계산용).
//  PurchasedEquip / PurchasedGeneral
//               : 구매 완료 슬롯 (재진입 시 품절 표시 복원용).
//  PurchasedTrait
//               : 구매 완료 특성 슬롯의 TraitType (0 = 미구매).
//                 특성 추첨 풀은 "미보유 특성" 으로만 채우므로,
//                 산 특성을 기록해 두지 않으면 다시 열었을 때 그 자리에
//                 다른 특성이 올라온다 — 사실상 공짜 새로고침이 된다.
//
//  초기화 시점:
//    - 스테이지 클리어 → NewStage() (새 시드 + 전체 초기화)
//    - 새로고침       → IncrementRefresh() (RefreshCount++ + 구매 초기화)
//    - 환생           → SetDefaults()
// ============================================================

public class RunShopData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.RunShop;

    RawData _raw = new();

    // ── 읽기 ────────────────────────────────────────────────────

    public int ShopSeed     => _raw.ShopSeed;
    public int RefreshCount => _raw.RefreshCount;

    public bool IsPurchasedEquip(int i)
        => i >= 0 && i < _raw.PurchasedEquip.Length && _raw.PurchasedEquip[i];
    public bool IsPurchasedGeneral(int i)
        => i >= 0 && i < _raw.PurchasedGeneral.Length && _raw.PurchasedGeneral[i];

    /// <summary>이 슬롯에서 산 특성. 구매하지 않았으면 TraitType.None.</summary>
    public TraitType GetPurchasedTrait(int i)
        => i >= 0 && i < _raw.PurchasedTrait.Length
            ? (TraitType)_raw.PurchasedTrait[i]
            : TraitType.None;

    // ── 쓰기 ────────────────────────────────────────────────────

    /// <summary>스테이지 클리어 시 호출 — 새 시드 생성, 전체 초기화.</summary>
    public void NewStage()
    {
        _raw.ShopSeed     = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        _raw.RefreshCount = 0;
        ClearPurchases();
    }

    /// <summary>새로고침 시 호출 — 카운터 증가, 구매 내역 초기화.</summary>
    public void IncrementRefresh()
    {
        _raw.RefreshCount++;
        ClearPurchases();
    }

    public void SetPurchasedEquip(int i)
    {
        if (i >= 0 && i < _raw.PurchasedEquip.Length) _raw.PurchasedEquip[i] = true;
    }
    public void SetPurchasedGeneral(int i)
    {
        if (i >= 0 && i < _raw.PurchasedGeneral.Length) _raw.PurchasedGeneral[i] = true;
    }
    public void SetPurchasedTrait(int i, TraitType trait)
    {
        if (i >= 0 && i < _raw.PurchasedTrait.Length) _raw.PurchasedTrait[i] = (int)trait;
    }

    // ── ISaveSection ────────────────────────────────────────────

    public string Serialize()              => JsonUtility.ToJson(_raw);
    public void   Deserialize(string json) => _raw = JsonUtility.FromJson<RawData>(json) ?? new RawData();
    public void   SetDefaults()
    {
        _raw = new RawData { ShopSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue) };
    }

    // ── 내부 ────────────────────────────────────────────────────

    void ClearPurchases()
    {
        _raw.PurchasedEquip   = new bool[EquipSlots];
        _raw.PurchasedGeneral = new bool[GeneralSlots];
        _raw.PurchasedTrait   = new int[TraitSlots];
    }

    // 슬롯 개수 — RunShopPopup 의 슬롯 수와 반드시 같아야 한다.
    public const int EquipSlots   = 4;
    public const int TraitSlots   = 2;
    public const int GeneralSlots = 5;

    [Serializable]
    class RawData
    {
        public int    ShopSeed         = 0;
        public int    RefreshCount     = 0;
        public bool[] PurchasedEquip   = new bool[EquipSlots];
        public bool[] PurchasedGeneral = new bool[GeneralSlots];
        public int[]  PurchasedTrait   = new int[TraitSlots];
    }
}
