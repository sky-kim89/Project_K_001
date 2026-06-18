using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopPopup.cs
//  런 중간 상점 팝업.
//
//  상점 상태는 RunShopData(저장 섹션)에 보관한다.
//    ShopSeed     : 장비·장수 픽업 결정론적 시드
//    RefreshCount : 새로고침 횟수 (비용 계산 + 시드 오프셋)
//    PurchasedEquip / PurchasedGeneral : 구매 완료 슬롯
//
//  상점 갱신 시점:
//    - 스테이지 클리어 → InGameManager가 RunShopData.NewStage() 호출
//    - 팝업 오픈 시   → 시드로 결정론적 재생성 + 구매 플래그 복원
//    - 새로고침 버튼  → RefreshCount++ 후 재생성
//
//  특성 슬롯: 시드로 생성하되, 구매 여부는 RunTraitData.HasTrait()
//             로 판단하므로 별도 플래그 없음.
// ============================================================

public class RunShopPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("장비 슬롯 (4종)")]
    [SerializeField] RunShopEquipSlot[] _equipSlots;

    [Header("특성 슬롯 (2종)")]
    [SerializeField] RunShopTraitSlot[] _traitSlots;

    [Header("장수 슬롯 (5종)")]
    [SerializeField] RunShopGeneralSlot[] _generalSlots;

    [Header("UI")]
    [SerializeField] Button          _refreshBtn;
    [SerializeField] TextMeshProUGUI _refreshCostText;
    [SerializeField] Button          _closeBtn;

    const int RefreshBaseCost = 100;
    const int SeedPrime       = 7919;   // 새로고침별 시드 오프셋용 소수

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        _refreshBtn?.onClick.AddListener(OnRefresh);
    }

    protected override void OnAfterOpen()
    {
        // 시드로 결정론적 재생성 → 구매 플래그 복원
        GenerateShop();
    }

    // ── 상점 생성 ─────────────────────────────────────────────

    void GenerateShop()
    {
        var shopData = UserDataManager.Instance?.Get<RunShopData>();
        var rng      = new System.Random((shopData?.ShopSeed ?? 0) + (shopData?.RefreshCount ?? 0) * SeedPrime);

        SetupEquipSlots(rng, shopData);
        SetupTraitSlots(rng);
        SetupGeneralSlots(rng, shopData);
        RefreshRefreshBtn();
    }

    void SetupEquipSlots(System.Random rng, RunShopData shopData)
    {
        var db = EquipmentDatabase.Current;
        if (_equipSlots == null) return;

        int stageLevel = (UserDataManager.Instance?.Get<StageProgressData>()?.CurrentRunStage ?? 0) + 1;
        var usedIds    = new HashSet<string>();

        for (int i = 0; i < _equipSlots.Length; i++)
        {
            EquipmentData data = null;
            for (int retry = 0; retry < 30; retry++)
            {
                var candidate = db?.PickRandom(stageLevel, rng);
                if (candidate == null) break;
                if (usedIds.Add(candidate.EquipmentId)) { data = candidate; break; }
            }
            if (data == null) data = db?.PickRandom(stageLevel, rng);

            int idx = i;
            _equipSlots[i]?.Setup(data, data != null ? CalcEquipCost(data) : 0,
                (d, c) => OnBuyEquip(d, c, idx));

            if (shopData != null && shopData.IsPurchasedEquip(i))
                _equipSlots[i]?.SetSoldOut();
        }
    }

    void SetupTraitSlots(System.Random rng)
    {
        var db    = TraitDatabase.Current;
        var owned = UserDataManager.Instance?.Get<RunTraitData>();
        if (_traitSlots == null) return;

        var pool = new List<TraitData>();
        foreach (TraitType t in Enum.GetValues(typeof(TraitType)))
        {
            if (t == TraitType.None) continue;
            if ((int)t >= 1000) continue; // 직업 시너지 — 상점 비등장
            var td = db?.Get(t);
            if (td != null) pool.Add(td);
        }
        ShuffleSeeded(pool, rng);

        // 소유 특성은 품절 표시, 미소유 특성은 구매 가능
        int poolIdx = 0;
        for (int i = 0; i < _traitSlots.Length; i++)
        {
            // 미소유 특성 중 다음 후보 탐색
            TraitData data = null;
            while (poolIdx < pool.Count)
            {
                var candidate = pool[poolIdx++];
                if (owned == null || !owned.HasTrait(candidate.TraitType))
                {
                    data = candidate;
                    break;
                }
            }

            _traitSlots[i]?.Setup(data, 400, OnBuyTrait);
        }
    }

    void SetupGeneralSlots(System.Random rng, RunShopData shopData)
    {
        if (_generalSlots == null) return;

        int cost         = GameplayConfig.Current.HireMercenaryCost;
        int activeSlots  = RelicApplier.GetTotalActiveGeneralSlots();
        int deployed     = UserDataManager.Instance?.Get<DeploymentData>()?.GetDeployedUnits().Count ?? 0;
        bool slotsFull   = deployed >= activeSlots;

        var allNames = UserDataManager.Instance?.Get<UnitData>()?.GetAvailableNames();
        var used     = new HashSet<string>();

        for (int i = 0; i < _generalSlots.Length; i++)
        {
            UnitEntry entry = null;
            if (!slotsFull && allNames != null && allNames.Count > 0)
            {
                string chosen = null;
                for (int retry = 0; retry < 20; retry++)
                {
                    string nm = allNames[rng.Next(allNames.Count)];
                    if (used.Add(nm)) { chosen = nm; break; }
                }
                if (chosen == null) chosen = allNames[i % allNames.Count];
                entry = new UnitEntry
                {
                    UnitName     = chosen,
                    Level        = 1,
                    Exp          = 0,
                    GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)UnitJobRoller.GetBirthGrade(chosen)),
                };
            }

            int idx = i;
            _generalSlots[i]?.Setup(entry, cost, (e, c) => OnHireGeneral(e, c, idx));

            if (shopData != null && shopData.IsPurchasedGeneral(i))
                _generalSlots[i]?.SetSoldOut();
        }
    }

    void RefreshRefreshBtn()
    {
        var shopData = UserDataManager.Instance?.Get<RunShopData>();
        int cost = RefreshBaseCost * ((shopData?.RefreshCount ?? 0) + 1);
        if (_refreshCostText != null) _refreshCostText.text = $"{cost}";
        int gold = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
        if (_refreshBtn != null) _refreshBtn.interactable = gold >= cost;
    }

    // ── 구매 처리 ─────────────────────────────────────────────

    void OnBuyEquip(EquipmentData data, int cost, int slotIdx)
    {
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || items.Get(eItem.Gold) < cost) return;

        items.Add(eItem.Gold, -cost);
        UserDataManager.Instance?.Get<EquipInventoryData>()?.Add(data.EquipmentId);
        UserDataManager.Instance?.Get<RunShopData>()?.SetPurchasedEquip(slotIdx);
        UserDataManager.Instance?.RequestSave();
        RefreshRefreshBtn();
    }

    void OnBuyTrait(TraitData data, int cost)
    {
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || items.Get(eItem.Gold) < cost) return;

        items.Add(eItem.Gold, -cost);
        UserDataManager.Instance?.Get<RunTraitData>()?.AddTrait(data.TraitType);
        UserDataManager.Instance?.RequestSave();
        RefreshRefreshBtn();
        // 슬롯 UI의 품절 표시는 Setup() 내 onClick 리스너가 처리
    }

    void OnHireGeneral(UnitEntry entry, int cost, int slotIdx)
    {
        var items  = UserDataManager.Instance?.Get<ItemData>();
        var deploy = UserDataManager.Instance?.Get<DeploymentData>();
        var units  = UserDataManager.Instance?.Get<UnitData>();
        if (items == null || deploy == null || items.Get(eItem.Gold) < cost) return;

        int slot = -1;
        for (int i = 0; i < 5; i++)
        {
            if (string.IsNullOrEmpty(deploy.GetUnitAt(i))) { slot = i; break; }
        }
        if (slot < 0) return;

        items.Add(eItem.Gold, -cost);
        if (!units.HasUnit(entry.UnitName))
            units.AddUnit(new UnitEntry { UnitName = entry.UnitName, Level = 1, GradeUpCount = entry.GradeUpCount });
        deploy.Deploy(entry.UnitName, slot);
        UserDataManager.Instance?.Get<RunShopData>()?.SetPurchasedGeneral(slotIdx);
        JobSynergyEvaluator.Recalculate();
        UserDataManager.Instance?.RequestSave();
        RefreshRefreshBtn();
    }

    void OnRefresh()
    {
        var shopData = UserDataManager.Instance?.Get<RunShopData>();
        int cost     = RefreshBaseCost * ((shopData?.RefreshCount ?? 0) + 1);
        var items    = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || items.Get(eItem.Gold) < cost) return;

        items.Add(eItem.Gold, -cost);
        shopData?.IncrementRefresh();
        GenerateShop();
        UserDataManager.Instance?.RequestSave();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static int CalcEquipCost(EquipmentData data) => data.Grade switch
    {
        UnitGrade.Normal   => 200,
        UnitGrade.Uncommon => 350,
        UnitGrade.Rare     => 550,
        UnitGrade.Unique   => 800,
        UnitGrade.Epic     => 1200,
        _                  => 300,
    };

    static void ShuffleSeeded<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// 슬롯 컴포넌트는 각 별도 파일로 분리:
//   RunShopEquipSlot.cs / RunShopTraitSlot.cs / RunShopGeneralSlot.cs
