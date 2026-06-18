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
//
//  특성 슬롯은 RunTraitData.HasTrait() 로 소유 여부를 직접 판단하므로
//  별도 구매 플래그를 저장하지 않는다.
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
        _raw.PurchasedEquip   = new bool[4];
        _raw.PurchasedGeneral = new bool[5];
    }

    [Serializable]
    class RawData
    {
        public int    ShopSeed        = 0;
        public int    RefreshCount    = 0;
        public bool[] PurchasedEquip  = new bool[4];
        public bool[] PurchasedGeneral = new bool[5];
    }
}
