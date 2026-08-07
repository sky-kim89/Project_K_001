using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopPopup.cs
//  런 중간 상점 팝업 — "행상인의 좌판".
//
//  열리는 경로: 상점 스테이지 → EventPopup(TravelingMerchant)
//               → "상품을 본다" → 이 팝업.
//               (StageSelectUI 가 직접 열지 않는다)
//
//  ■ 화면 구성
//    상품 6칸 — 장비 4 + 특성 2. 전부 RunShopGoodsSlot(= RewardCard) 하나로
//               그린다. 이름·가격만 칸에 두고 상세는 카드를 눌러 툴팁으로 본다.
//    용병 5칸 — RunShopGeneralSlot (HeroCard + 고용 버튼).
//
//  ■ 상점 상태는 RunShopData(저장 섹션)에 보관한다.
//    ShopSeed       : 장비·장수 픽업 결정론적 시드
//    RefreshCount   : 새로고침 횟수 (비용 계산 + 시드 오프셋)
//    PurchasedEquip / PurchasedGeneral / PurchasedTrait : 구매 완료 슬롯
//
//  ■ 상점 갱신 시점
//    - 스테이지 클리어 → InGameManager 가 RunShopData.NewStage() 호출
//    - 팝업 오픈 시     → 시드로 결정론적 재생성 + 구매 플래그 복원
//    - 새로고침 버튼    → RefreshCount++ 후 재생성
// ============================================================

public class RunShopPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("상품 슬롯 (앞 4칸 = 장비, 뒤 2칸 = 특성)")]
    [SerializeField] RunShopGoodsSlot[] _goodsSlots;

    [Header("용병 슬롯 (5종)")]
    [SerializeField] RunShopGeneralSlot[] _generalSlots;

    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _goldText;
    [SerializeField] Button          _refreshBtn;
    [SerializeField] TextMeshProUGUI _refreshCostText;
    [SerializeField] Button          _closeBtn;

    const int EquipSlots      = RunShopData.EquipSlots;     // 4
    const int TraitSlots      = RunShopData.TraitSlots;     // 2
    const int TraitCost       = 400;
    const int RefreshBaseCost = 100;
    const int SeedPrime       = 7919;   // 새로고침별 시드 오프셋용 소수

    /// <summary>상점 장비 등급 하한 — 일반·고급은 전투 보상으로만 나온다.</summary>
    const UnitGrade MinEquipGrade = UnitGrade.Rare;

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn.onClick.AddListener(() => Close());
        _refreshBtn.onClick.AddListener(OnRefresh);
    }

    protected override void OnAfterOpen() => GenerateShop();

    // ── 상점 생성 ─────────────────────────────────────────────

    void GenerateShop()
    {
        var shopData = UserDataManager.Instance.Get<RunShopData>();
        var rng      = new System.Random(shopData.ShopSeed + shopData.RefreshCount * SeedPrime);

        SetupEquipSlots(rng, shopData);
        SetupTraitSlots(rng, shopData);
        SetupGeneralSlots(rng, shopData);
        RefreshHeader();
    }

    void SetupEquipSlots(System.Random rng, RunShopData shopData)
    {
        var db         = EquipmentDatabase.Current;
        int stageLevel = UserDataManager.Instance.Get<StageProgressData>().CurrentRunStage + 1;
        var usedIds    = new HashSet<string>();

        for (int i = 0; i < EquipSlots; i++)
        {
            var slot = _goodsSlots[i];
            var data = PickUniqueEquipment(db, stageLevel, rng, usedIds);

            if (data == null) { slot.SetEmpty(); continue; }

            int idx = i;
            slot.SetupEquipment(data, CalcEquipCost(data),
                                () => OnBuyEquip(data, CalcEquipCost(data), idx));

            if (shopData.IsPurchasedEquip(i)) slot.SetSoldOut("구매 완료");
        }
    }

    static EquipmentData PickUniqueEquipment(EquipmentDatabase db, int stageLevel,
                                             System.Random rng, HashSet<string> usedIds)
    {
        for (int retry = 0; retry < 30; retry++)
        {
            var candidate = db.PickRandom(stageLevel, rng, MinEquipGrade);
            if (candidate == null) return null;
            if (usedIds.Add(candidate.EquipmentId)) return candidate;
        }
        // 중복이라도 하나는 내놓는다 (풀이 슬롯 수보다 작은 초반 상점)
        return db.PickRandom(stageLevel, rng, MinEquipGrade);
    }

    // 추첨 풀은 "아직 없는 특성" 으로만 채운다.
    // 이미 산 슬롯은 풀을 건드리지 않고 저장된 특성을 그대로 품절로 보여준다
    // — 그러지 않으면 다시 열 때마다 새 특성이 올라와 공짜 새로고침이 된다.
    void SetupTraitSlots(System.Random rng, RunShopData shopData)
    {
        var db    = TraitDatabase.Current;
        var owned = UserDataManager.Instance.Get<RunTraitData>();

        var pool = new List<TraitData>();
        foreach (TraitType t in Enum.GetValues(typeof(TraitType)))
        {
            if (t == TraitType.None) continue;
            if ((int)t >= 1000) continue;                  // 직업 시너지 — 상점 비등장
            if (owned.HasTrait(t)) continue;
            var td = db.Get(t);
            if (td != null) pool.Add(td);
        }
        ShuffleSeeded(pool, rng);

        int poolIdx = 0;
        for (int i = 0; i < TraitSlots; i++)
        {
            var slot     = _goodsSlots[EquipSlots + i];
            var purchase = shopData.GetPurchasedTrait(i);
            int idx      = i;

            // 이미 산 슬롯 — 저장된 특성을 그대로 다시 그리고 품절 처리
            if (purchase != TraitType.None)
            {
                var bought = db.Get(purchase);
                slot.SetupTrait(bought, TraitCost, () => OnBuyTrait(bought, TraitCost, idx));
                slot.SetSoldOut("구매 완료");
                continue;
            }

            if (poolIdx >= pool.Count) { slot.SetEmpty(); continue; }

            var data = pool[poolIdx++];
            slot.SetupTrait(data, TraitCost, () => OnBuyTrait(data, TraitCost, idx));
        }
    }

    void SetupGeneralSlots(System.Random rng, RunShopData shopData)
    {
        int cost        = GameplayConfig.Current.HireMercenaryCost;
        int activeSlots = RelicApplier.GetTotalActiveGeneralSlots();
        int deployed    = UserDataManager.Instance.Get<DeploymentData>().GetDeployedUnits().Count;
        bool slotsFull  = deployed >= activeSlots;

        var allNames = UserDataManager.Instance.Get<UnitData>().GetAvailableNames();
        var used     = new HashSet<string>();

        for (int i = 0; i < _generalSlots.Length; i++)
        {
            UnitEntry entry = null;
            if (!slotsFull && allNames.Count > 0)
            {
                string chosen = null;
                for (int retry = 0; retry < 20; retry++)
                {
                    string nm = allNames[rng.Next(allNames.Count)];
                    if (used.Add(nm)) { chosen = nm; break; }
                }
                chosen ??= allNames[i % allNames.Count];
                entry = new UnitEntry
                {
                    UnitName     = chosen,
                    Level        = 1,
                    Exp          = 0,
                    GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)UnitJobRoller.GetBirthGrade(chosen)),
                };
            }

            int idx = i;
            _generalSlots[i].Setup(entry, cost, (e, c) => OnHireGeneral(e, c, idx));

            if (shopData.IsPurchasedGeneral(i)) _generalSlots[i].SetSoldOut();
        }
    }

    // ── 헤더 (보유 골드 · 새로고침 비용 · 구매 가능 여부) ─────

    void RefreshHeader()
    {
        int gold = UserDataManager.Instance.Get<ItemData>().Get(eItem.Gold);
        int cost = RefreshCost();

        _goldText.text        = $"{gold:N0}";
        _refreshCostText.text = $"{cost:N0}";
        _refreshBtn.interactable = gold >= cost;

        // 살 수 없는 상품은 버튼을 잠가 둔다 — 눌러 보고 아무 일도 안 일어나면
        // 무엇이 문제인지 알 수 없다.
        foreach (var slot in _goodsSlots)
            slot.SetAffordable(gold >= slot.Cost);

        foreach (var slot in _generalSlots)
            slot.SetAffordable(gold >= GameplayConfig.Current.HireMercenaryCost);
    }

    int RefreshCost()
        => RefreshBaseCost * (UserDataManager.Instance.Get<RunShopData>().RefreshCount + 1);

    // ── 구매 처리 ─────────────────────────────────────────────
    //  ⚠ 골드 차감은 반드시 ItemData.Spend() 로 한다.
    //    Add() 는 첫 줄이 `if (amount <= 0) return;` 이라 음수를 넣으면
    //    조용히 아무 일도 안 일어난다 — 전부 공짜로 사지던 원인이었다.
    //    Spend() 는 잔액 검사 + 차감을 한 번에 하고 성공 여부를 돌려준다.

    bool OnBuyEquip(EquipmentData data, int cost, int slotIdx)
    {
        var items = UserDataManager.Instance.Get<ItemData>();
        if (!items.Spend(eItem.Gold, cost)) return false;

        UserDataManager.Instance.Get<EquipInventoryData>().Add(data.EquipmentId);
        UserDataManager.Instance.Get<RunShopData>().SetPurchasedEquip(slotIdx);
        UserDataManager.Instance.RequestSave();
        RefreshHeader();
        return true;
    }

    bool OnBuyTrait(TraitData data, int cost, int slotIdx)
    {
        var items = UserDataManager.Instance.Get<ItemData>();
        if (!items.Spend(eItem.Gold, cost)) return false;

        UserDataManager.Instance.Get<RunTraitData>().AddTrait(data.TraitType);
        UserDataManager.Instance.Get<RunShopData>().SetPurchasedTrait(slotIdx, data.TraitType);
        UserDataManager.Instance.RequestSave();
        RefreshHeader();
        return true;
    }

    bool OnHireGeneral(UnitEntry entry, int cost, int slotIdx)
    {
        var items  = UserDataManager.Instance.Get<ItemData>();
        var deploy = UserDataManager.Instance.Get<DeploymentData>();
        var units  = UserDataManager.Instance.Get<UnitData>();

        int slot = -1;
        for (int i = 0; i < RunShopData.GeneralSlots; i++)
        {
            if (string.IsNullOrEmpty(deploy.GetUnitAt(i))) { slot = i; break; }
        }
        if (slot < 0) return false;

        // 빈 슬롯을 확인한 뒤에 차감한다 — 순서를 바꾸면 배치 실패 시 골드만 날아간다
        if (!items.Spend(eItem.Gold, cost)) return false;

        if (!units.HasUnit(entry.UnitName))
            units.AddUnit(new UnitEntry { UnitName = entry.UnitName, Level = 1, GradeUpCount = entry.GradeUpCount });
        deploy.Deploy(entry.UnitName, slot);
        UserDataManager.Instance.Get<RunShopData>().SetPurchasedGeneral(slotIdx);
        JobSynergyEvaluator.Recalculate();
        UserDataManager.Instance.RequestSave();
        RefreshHeader();
        return true;
    }

    void OnRefresh()
    {
        var items = UserDataManager.Instance.Get<ItemData>();
        if (!items.Spend(eItem.Gold, RefreshCost())) return;

        UserDataManager.Instance.Get<RunShopData>().IncrementRefresh();
        GenerateShop();
        UserDataManager.Instance.RequestSave();
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
