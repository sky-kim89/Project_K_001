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
//  EquipOffers / TraitOffers / GeneralOffers
//               : 현재 진열 중인 상품 자체. 구매로 보유 목록이 바뀌어도
//                 닫았다 다시 연 상점의 나머지 상품은 그대로 유지한다.
//  TraitBuyCount
//               : 이번 여정에서 **상점에서 산** 특성의 누적 개수. 특성 값이 여기 비례한다.
//                 ⚠ 위 PurchasedTrait 과 달리 스테이지·새로고침으로 지우지 않는다 —
//                   지우면 스테이지를 넘길 때마다 값이 첫 개당 가격으로 되돌아간다.
//                 ⚠ 보유 특성 수를 세지 않는 이유: 이벤트·유물로 받은 특성까지 세면
//                   "공짜로 받았더니 상점 값이 올랐다" 가 된다. 산 것만 값을 올린다.
//
//  초기화 시점:
//    - 스테이지 클리어 → NewStage() (새 시드 + 구매 슬롯 초기화, 누적 구매 수는 유지)
//    - 새로고침       → IncrementRefresh() (RefreshCount++ + 구매 슬롯 초기화)
//    - 환생           → SetDefaults() (여정이 끝나므로 누적 구매 수도 0)
// ============================================================

public class RunShopData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.RunShop;

    RawData _raw = new();

    // ── 읽기 ────────────────────────────────────────────────────

    public int ShopSeed     => _raw.ShopSeed;
    public int RefreshCount => _raw.RefreshCount;
    public bool OffersGenerated => _raw.OffersGenerated;

    /// <summary>이번 여정에서 상점 특성 칸을 사들인 누적 횟수. 특성 가격의 기준이다.</summary>
    public int TraitBuyCount => _raw.TraitBuyCount;

    public bool IsPurchasedEquip(int i)
        => i >= 0 && i < _raw.PurchasedEquip.Length && _raw.PurchasedEquip[i];
    public bool IsPurchasedGeneral(int i)
        => i >= 0 && i < _raw.PurchasedGeneral.Length && _raw.PurchasedGeneral[i];

    /// <summary>이 슬롯에서 산 특성. 구매하지 않았으면 TraitType.None.</summary>
    public TraitType GetPurchasedTrait(int i)
        => i >= 0 && i < _raw.PurchasedTrait.Length
            ? (TraitType)_raw.PurchasedTrait[i]
            : TraitType.None;

    public string GetEquipOffer(int i)    => _raw.EquipOffers[i];
    public TraitType GetTraitOffer(int i) => (TraitType)_raw.TraitOffers[i];
    public string GetGeneralOffer(int i)  => _raw.GeneralOffers[i];

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
    /// <summary>
    /// 특성 칸 구매 기록 + 누적 구매 수 증가.
    ///
    /// ⚠ 누적은 여기서만 올린다 — 상점 구매의 유일한 통로다.
    ///   이벤트·보상으로 받은 특성은 이 함수를 거치지 않으므로 값에 영향을 주지 않는다.
    /// </summary>
    public void SetPurchasedTrait(int i, TraitType trait)
    {
        if (i < 0 || i >= _raw.PurchasedTrait.Length) return;
        _raw.PurchasedTrait[i] = (int)trait;
        _raw.TraitBuyCount++;
    }

    public void BeginOffers()
    {
        _raw.OffersGenerated = false;
        _raw.EquipOffers     = new string[EquipSlots];
        _raw.TraitOffers     = new int[TraitSlots];
        _raw.GeneralOffers   = new string[GeneralSlots];
    }

    public void SetEquipOffer(int i, string equipmentId) => _raw.EquipOffers[i] = equipmentId;
    public void SetTraitOffer(int i, TraitType trait)     => _raw.TraitOffers[i] = (int)trait;
    public void SetGeneralOffer(int i, string unitName)   => _raw.GeneralOffers[i] = unitName;
    public void CompleteOffers()                          => _raw.OffersGenerated = true;

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
        BeginOffers();
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
        public bool     OffersGenerated = false;
        public string[] EquipOffers     = new string[EquipSlots];
        public int[]    TraitOffers     = new int[TraitSlots];
        public string[] GeneralOffers   = new string[GeneralSlots];
        // 여정 단위 누적 — ClearPurchases 가 건드리지 않는 유일한 구매 기록이다
        public int    TraitBuyCount    = 0;
    }
}
