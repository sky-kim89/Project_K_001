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
    // ── 가격 정책 ────────────────────────────────────────────
    //  특성은 이 게임에서 가장 비싼 물건이다.
    //  런 내내 유지되고 스택까지 쌓이는데 값이 싸면(한 스테이지 보상의 절반 수준)
    //  보이는 족족 전부 사게 된다 — 고를 이유가 없으면 상점이 자판기가 된다.
    //
    //  ⚠ 스테이지가 아니라 **이번 여정에 상점에서 산 개수**에 비례한다
    //    스테이지 비례로 두면 "언제 샀느냐" 가 값을 정한다 — 늦게 시작한 특성일수록
    //    비싸서, 초반에 몰아 사는 게 항상 정답이 된다.
    //    구매 비례면 "몇 개째 사느냐" 가 값을 정한다. 살수록 다음 하나가 무거워져
    //    소수 정예로 갈지 넓게 모을지 고르게 된다.
    //    ⚠ 보유 개수가 아니다 — 이벤트·유물로 받은 특성은 값을 올리지 않는다.
    const int TraitCostBase      = 500;
    const int TraitCostPerBought = 400;   // 상점 구매 1개당 인상폭

    const int RefreshBaseCost = 100;
    const int SeedPrime       = 7919;   // 새로고침별 시드 오프셋용 소수

    /// <summary>상점 장비 등급 하한 — 일반·고급은 전투 보상으로만 나온다.</summary>
    const UnitGrade MinEquipGrade = UnitGrade.Rare;

    // 이번 상점이 뽑아 둔 용병 매물. 배치 슬롯이 늘어나면 이 목록을 그대로
    // 다시 그린다 — 다시 뽑으면 공짜 새로고침이 된다 (SetupGeneralSlots 주석 참고).
    UnitEntry[] _generalPicks;

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
        bool generateOffers = !shopData.OffersGenerated;
        if (generateOffers) shopData.BeginOffers();

        var rng      = new System.Random(shopData.ShopSeed + shopData.RefreshCount * SeedPrime);

        SetupEquipSlots(rng, shopData, generateOffers);
        SetupTraitSlots(rng, shopData, generateOffers);
        SetupGeneralSlots(rng, shopData, generateOffers);

        if (generateOffers)
        {
            shopData.CompleteOffers();
            UserDataManager.Instance.RequestSave();
        }
        RefreshHeader();
    }

    void SetupEquipSlots(System.Random rng, RunShopData shopData, bool generateOffers)
    {
        var db         = EquipmentDatabase.Current;
        int stageLevel = UserDataManager.Instance.Get<StageProgressData>().CurrentRunStage + 1;
        var usedIds    = new HashSet<string>();

        for (int i = 0; i < EquipSlots; i++)
        {
            var slot = _goodsSlots[i];
            var data = generateOffers
                ? PickUniqueEquipment(db, stageLevel, rng, usedIds)
                : db.Get(shopData.GetEquipOffer(i));

            if (generateOffers) shopData.SetEquipOffer(i, data != null ? data.EquipmentId : "");

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
    void SetupTraitSlots(System.Random rng, RunShopData shopData, bool generateOffers)
    {
        var db    = TraitDatabase.Current;
        var owned = UserDataManager.Instance.Get<RunTraitData>();

        // 이벤트 시나리오로 얻는 특성은 매물에서 뺀다 — 특정 선택지의 대가로
        // 주어지는 특성을 골드로도 살 수 있으면 그 선택이 무의미해진다.
        var eventTraits = EventRewardHandler.CollectEventTraits();

        var pool = new List<TraitData>();
        foreach (TraitType t in Enum.GetValues(typeof(TraitType)))
        {
            if (t == TraitType.None) continue;
            if ((int)t >= 1000) continue;         // 직업 시너지 — 배치로 자동 부여
            if (eventTraits.Contains(t)) continue;  // 이벤트 전용
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

            var data = generateOffers
                ? (poolIdx < pool.Count ? pool[poolIdx++] : null)
                : db.Get(shopData.GetTraitOffer(i));

            if (generateOffers)
                shopData.SetTraitOffer(i, data != null ? data.TraitType : TraitType.None);

            if (data == null) { slot.SetEmpty(); continue; }

            slot.SetupTrait(data, TraitCost(), () => OnBuyTrait(data, TraitCost(), idx));
            if (purchase != TraitType.None) slot.SetSoldOut("구매 완료");
        }
    }

    /// <summary>
    /// 용병 매물을 뽑아 두고 화면에 올린다. 뽑기는 팝업을 열 때(+새로고침) 한 번뿐이다.
    ///
    /// ⚠ 배치 슬롯이 꽉 찼어도 뽑기는 그대로 한다
    ///   예전엔 꽉 차 있으면 아예 안 뽑았다. 그러면 장수 슬롯이 늘어난 순간
    ///   (특성 구매) 보여 줄 매물이 없어서 다시 뽑아야 하고, 그 재추첨은
    ///   rng 를 소모한 뒤라 처음과 다른 얼굴이 나온다 — 사실상 공짜 새로고침이다.
    ///   뽑아만 두고 '보여 줄지' 는 RenderGeneralSlots 가 정한다.
    /// </summary>
    void SetupGeneralSlots(System.Random rng, RunShopData shopData, bool generateOffers)
    {
        var allNames = UserDataManager.Instance.Get<UnitData>().GetAvailableNames();
        var used     = new HashSet<string>();

        _generalPicks = new UnitEntry[_generalSlots.Length];

        for (int i = 0; i < _generalSlots.Length; i++)
        {
            string chosen = generateOffers ? null : shopData.GetGeneralOffer(i);
            if (generateOffers && allNames.Count > 0)
            {
                for (int retry = 0; retry < 20; retry++)
                {
                    string nm = allNames[rng.Next(allNames.Count)];
                    if (used.Add(nm)) { chosen = nm; break; }
                }
                chosen ??= allNames[i % allNames.Count];
            }

            if (generateOffers) shopData.SetGeneralOffer(i, chosen);
            if (string.IsNullOrEmpty(chosen)) continue;

            // 등급은 이름 시드가 정한 태생 등급 그대로 — 즉 매물마다 랜덤이다.
            // ⚠ 예전엔 GradeUpCount 로 Epic 까지 끌어올려 전 매물이 에픽이었다.
            //   그러면 등급업(HeroDetailPopup)이 항상 MAX 라 존재 의미가 없어진다.
            _generalPicks[i] = new UnitEntry
            {
                UnitName     = chosen,
                Level        = 1,
                Exp          = 0,
                GradeUpCount = 0,
            };
        }

        RenderGeneralSlots(shopData);
    }

    /// <summary>
    /// 뽑아 둔 매물을 다시 그린다. <b>추첨은 하지 않는다.</b>
    /// 배치 슬롯이 꽉 차도 매물과 구매 버튼을 유지한다. 자리를 비우는 것은
    /// 구매 버튼이 여는 MercenaryShopPopup 안에서 처리한다.
    /// </summary>
    void RenderGeneralSlots(RunShopData shopData)
    {
        if (_generalPicks == null) return;

        for (int i = 0; i < _generalSlots.Length; i++)
        {
            UnitEntry entry = _generalPicks[i];

            // 고용가는 매물 등급에 따라 다르다 (매물이 없으면 표시용 기본값)
            int idx  = i;
            int cost = entry != null
                ? GameplayConfig.HireCost(entry.Grade)
                : GameplayConfig.HireCost(UnitGrade.Normal);

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
            slot.SetAffordable(gold >= slot.Cost);
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

        var shopData = UserDataManager.Instance.Get<RunShopData>();

        UserDataManager.Instance.Get<RunTraitData>().AddTrait(data.TraitType);
        shopData.SetPurchasedTrait(slotIdx, data.TraitType);
        UserDataManager.Instance.RequestSave();

        // 장수 배치 슬롯이 늘어나는 특성(TraitEffect: GeneralSlotBonus)이면
        // 방금 그 자리에서 고용이 열려야 한다. 하단 줄만 다시 그린다 —
        // GenerateShop() 을 다시 부르면 상품 6칸까지 새로 갈린다.
        //
        // ⚠ 어떤 특성인지 따로 가려내지 않는다
        //   RelicTreeApplier.GetTotalActiveGeneralSlots() 가 유물·특성을 합쳐 계산하므로
        //   조건을 여기서 또 적으면 슬롯 공식이 두 벌이 된다. 슬롯 수가 그대로면
        //   RenderGeneralSlots 는 같은 그림을 다시 그릴 뿐이라 손해가 없다.
        RenderGeneralSlots(shopData);

        // ⚠ 특성 값은 구매 횟수에 비례한다 — 하나 사면 옆 칸도 그만큼 올라야 한다
        //   예전엔 여기서 RefreshHeader() 만 불렀다. 가격표는 슬롯을 세울 때
        //   찍힌 값 그대로 남고, 클릭 시 넘어가는 cost 는 람다 안에서 TraitCost() 를
        //   다시 계산했다 — 표시가 500 인 칸을 누르면 1,000 이 빠져나갔다.
        RefreshTraitCosts();
        RefreshHeader();
        return true;
    }

    /// <summary>안 팔린 특성 칸의 가격표를 현재 시세로 다시 찍는다.</summary>
    void RefreshTraitCosts()
    {
        int cost = TraitCost();
        for (int i = 0; i < TraitSlots; i++)
            _goodsSlots[EquipSlots + i].SetCost(cost);
    }

    /// <summary>
    /// 상점의 [고용] — <b>여기서 사지 않는다.</b>
    /// 고른 매물을 들고 용병 고용 팝업을 열 뿐이고, 구매·배치·차감은 전부 거기서 한다.
    ///
    /// ■ 왜 한 단계를 더 두나
    ///   예전엔 이 자리에서 바로 고용했다. 그런데 배치 슬롯이 꽉 차 있으면
    ///   빈 칸을 못 찾고 `return false` — 눌러도 아무 일이 안 일어났다.
    ///   좋은 매물이 떴는데 플레이어가 할 수 있는 게 없었다.
    ///   고용 팝업에는 현재 부대 5칸이 있어 누굴 내보낼지 그 자리에서 정하고
    ///   (칸 클릭 → HeroDetailPopup 해고) 곧바로 구매까지 이어갈 수 있다.
    ///
    /// ■ 구매 로직을 옮긴 것이지 복제한 게 아니다
    ///   골드 차감·중복 방지·JobSynergy 재계산은 MercenaryShopPopup 이 이미
    ///   전부 갖고 있다. 여기 남겨 두면 같은 규칙이 두 벌이 된다.
    ///
    /// 항상 false 를 돌려준다 — 이 시점엔 아직 아무 일도 일어나지 않았으므로
    /// 칸을 품절로 덮으면 안 된다. 품절 처리는 OnShopGeneralHired 가 한다.
    /// </summary>
    bool OnHireGeneral(UnitEntry entry, int cost, int slotIdx)
    {
        PopupManager.Instance
            .Open<MercenaryShopPopup>(PopupType.MercenaryShop)
            .SetupFromShop(entry, () => OnShopGeneralHired(slotIdx));
        return false;
    }

    /// <summary>고용 팝업이 구매를 확정하고 닫힌 뒤 호출된다 (골드 차감까지 끝난 시점).</summary>
    void OnShopGeneralHired(int slotIdx)
    {
        var shopData = UserDataManager.Instance.Get<RunShopData>();
        shopData.SetPurchasedGeneral(slotIdx);
        UserDataManager.Instance.RequestSave();

        // 산 칸만 품절로 바꾸고, 나머지 매물은 그대로 유지한다.
        RenderGeneralSlots(shopData);
        RefreshHeader();
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

    /// <summary>
    /// 특성 가격 — <b>이번 여정에 상점에서 산 특성 개수</b>에 비례한다.
    /// 0개 500 / 1개 900 / 2개 1,300 / 3개 1,700 / 6개 2,900 …
    ///
    /// ⚠ '보유 개수' 가 아니라 '구매 횟수' 다
    ///   예전엔 RunTraitData 의 보유 특성을 셌다. 그러면 이벤트 보상·유물로
    ///   공짜로 받은 특성까지 값을 밀어 올린다 — 받은 쪽이 손해가 되는 구조다.
    ///   상점에서 지갑을 연 만큼만 다음 값이 무거워진다 (RunShopData.TraitBuyCount).
    ///   덤으로 시너지 특성(1000~)을 걸러 낼 필요도 없어졌다 — 애초에 살 수 없다.
    ///
    /// ⚠ 값이 도중에 바뀌므로 표시도 같이 갱신해야 한다
    ///   한 칸을 사면 옆 칸 값이 즉시 오른다 — OnBuyTrait 이 RefreshTraitCosts()
    ///   를 부르는 이유다. 안 부르면 가격표와 실제 차감액이 어긋난다.
    /// </summary>
    static int TraitCost()
        => TraitCostBase
         + UserDataManager.Instance.Get<RunShopData>().TraitBuyCount * TraitCostPerBought;

    // 장비는 특성보다 싸되, 등급 차이가 선택으로 느껴질 만큼은 벌린다
    static int CalcEquipCost(EquipmentData data) => data.Grade switch
    {
        UnitGrade.Normal   => 250,
        UnitGrade.Uncommon => 450,
        UnitGrade.Rare     => 700,
        UnitGrade.Unique   => 1050,
        UnitGrade.Epic     => 1600,
        _                  => 400,
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
