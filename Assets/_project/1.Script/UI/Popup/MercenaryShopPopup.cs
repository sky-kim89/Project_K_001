using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercenaryShopPopup.cs
//  용병 고용 팝업 — 엘리트 스테이지 클리어 보상.
//
//  ■ 화면 구성 (EventPopup 과 같은 톤)
//    전체화면 오버레이 + 세로 스트레치 패널.
//    헤더(◆ 용 병 태그 + 타이틀) → 구분선 → 후보 카드 → 하단 액션 바.
//
//  ■ 후보 표시는 MainPanel 과 같은 카드다
//    한 번에 한 명을 크게 보여 주고 ◀ ▶ 로 넘긴다 (페이지 도트로 위치 표시).
//    카드 자체가 `GeneralCandidateCardUI` — MainPanel 이 쓰는 그 컴포넌트다.
//    ⚠ 예전엔 좌측에 배치 5칸 + 우측에 작은 후보 3장을 동시에 늘어놓아
//      한 명당 정보가 너무 작았고, 해고 기능이 이 팝업에만 있었다.
//      해고는 HeroDetailPopup 으로 옮겼으므로 여기서는 "고용/분해" 하나만 묻는다.
//
//  ■ 두 가지 모드
//    Setup(slot)      — 유료. 실제로 고용했을 때만 HireMercenaryCost 를 차감한다.
//    SetupAsReward()  — 무료. 엘리트 스테이지 클리어 보상으로 InGameManager 가 연다.
//
//    ⚠ 예전엔 OnAfterClose 가 모드 구분 없이 **무조건** 골드를 뺐다.
//      고용하지 않고 그냥 닫아도, 분해를 골라도 돈이 나갔다.
// ============================================================

public class MercenaryShopPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("후보 카드 (MainPanel 과 같은 카드 — 한 장을 넘겨 본다)")]
    [SerializeField] GeneralCandidateCardUI _card;
    [SerializeField] Button                 _prevBtn;
    [SerializeField] Button                 _nextBtn;
    [SerializeField] Image[]                _pageDots;      // 후보 수만큼만 켠다

    [Header("액션")]
    [SerializeField] Button          _hireBtn;
    [SerializeField] TextMeshProUGUI _hireCostText;
    [SerializeField] Image           _hireCostIcon;
    [SerializeField] Button          _passBtn;
    [SerializeField] TextMeshProUGUI _passBtnLabel;
    [SerializeField] Image           _passShardIcon;
    [SerializeField] TextMeshProUGUI _hintText;

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    // ── 런타임 상태 ──────────────────────────────────────────

    readonly List<UnitEntry> _candidates = new();
    int  _pageIndex;
    int  _targetSlot = -1;
    int  _totalCandidateShards;
    bool _isFree;      // true = 보상 모드 (골드 미차감)
    bool _hired;       // 이번 오픈에서 실제로 고용했는가

    const int CandidateCount = 3;

    UnitEntry Current => (_pageIndex >= 0 && _pageIndex < _candidates.Count)
        ? _candidates[_pageIndex] : null;

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        _hireBtn ?.onClick.AddListener(OnHire);
        _passBtn ?.onClick.AddListener(OnDecompose);
        _prevBtn ?.onClick.AddListener(() => Move(-1));
        _nextBtn ?.onClick.AddListener(() => Move(+1));
    }

    /// <summary>유료 고용 — 지정 슬롯에 배치한다. 고용을 확정해야 골드가 빠진다.</summary>
    public MercenaryShopPopup Setup(int targetSlot)
    {
        _targetSlot = targetSlot;
        _isFree     = false;
        return this;
    }

    /// <summary>
    /// 무료 고용 — 엘리트 스테이지 클리어 보상.
    /// 빈 슬롯 아무 데나 배치하고 골드를 받지 않는다.
    /// </summary>
    public MercenaryShopPopup SetupAsReward()
    {
        _targetSlot = -1;
        _isFree     = true;
        return this;
    }

    protected override void OnAfterOpen()
    {
        _hired     = false;
        _pageIndex = 0;

        ApplyIcon(_passShardIcon, eItem.SoldierShard,        new Color(0.45f, 0.70f, 1.00f));
        ApplyIcon(_hireCostIcon,  eItem.Gold,                new Color(1.00f, 0.80f, 0.20f));

        GenerateCandidates();
        RefreshView();
    }

    protected override void OnAfterClose()
    {
        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.HeroDetail))
            pm.Get<HeroDetailPopup>(PopupType.HeroDetail)?.Close();

        // 유료 모드에서 실제로 고용했을 때만 차감한다.
        // 그냥 닫거나(고용 안 함) 분해를 골랐으면 돈이 나가면 안 된다.
        if (!_isFree && _hired)
        {
            int cost = GameplayConfig.Current?.HireMercenaryCost ?? 500;
            UserDataManager.Instance?.Get<ItemData>()?.Spend(eItem.Gold, cost);
        }
        UserDataManager.Instance?.RequestSave();
    }

    static void ApplyIcon(Image img, eItem item, Color fallback)
    {
        if (img == null) return;
        var sp = SpriteManager.Instance?.Get(item.IconKey());
        img.sprite = sp;
        img.color  = sp != null ? Color.white : fallback;
    }

    // ── 후보 생성 ─────────────────────────────────────────────

    void GenerateCandidates()
    {
        _candidates.Clear();
        _totalCandidateShards = 0;

        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (unitData == null) return;

        var pool  = unitData.GetAvailableNames();
        int count = Mathf.Min(CandidateCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(i, pool.Count);
            (pool[i], pool[idx]) = (pool[idx], pool[i]);

            // 등급은 이름 시드가 정한 태생 등급 그대로 — GradeUpCount 를 채우면
            // 전원 에픽이 되어 등급업(HeroDetailPopup)이 의미를 잃는다.
            var entry = new UnitEntry { UnitName = pool[i], Level = 1, Exp = 0 };
            _candidates.Add(entry);
            _totalCandidateShards += CalcShards(entry);
        }
    }

    // ── 페이지 넘기기 ─────────────────────────────────────────

    void Move(int delta)
    {
        if (_candidates.Count == 0) return;
        _pageIndex = (_pageIndex + delta + _candidates.Count) % _candidates.Count;
        RefreshView();
    }

    // ── 뷰 갱신 ───────────────────────────────────────────────

    void RefreshView()
    {
        RefreshCard();
        RefreshDots();
        RefreshActionBar();
    }

    void RefreshCard()
    {
        var entry = Current;
        if (_card == null || entry == null) return;

        // onSelect 는 카드 자체를 눌렀을 때 — 보여 주는 카드가 곧 선택이라 할 일이 없다.
        // onDetail 은 "자세히 보기" → HeroDetailPopup 미리보기.
        //
        // ⚠ SetSelected(true) 를 부르지 않는다 — 그 함수는 등급 테두리를 흰색으로 덮는다.
        //   등급이 매물마다 랜덤이라 테두리 색이 곧 정보다. Setup() 이 이미 등급색으로 맞춰 둔다.
        _card.Setup(_pageIndex, entry, null, OpenDetail);
    }

    void RefreshDots()
    {
        if (_pageDots == null) return;
        for (int i = 0; i < _pageDots.Length; i++)
        {
            if (_pageDots[i] == null) continue;
            bool used = i < _candidates.Count;
            _pageDots[i].gameObject.SetActive(used);
            if (!used) continue;
            _pageDots[i].color = i == _pageIndex
                ? Color.white
                : new Color(0.35f, 0.35f, 0.55f, 0.8f);
        }

        // 후보가 하나뿐이면 넘길 곳이 없다
        bool multi = _candidates.Count > 1;
        _prevBtn?.gameObject.SetActive(multi);
        _nextBtn?.gameObject.SetActive(multi);
    }

    void RefreshActionBar()
    {
        bool hasSlot = _targetSlot >= 0 || GetFirstEmptySlot() >= 0;
        bool canPay  = _isFree || CanAffordHire();

        if (_hireBtn != null)
        {
            _hireBtn.gameObject.SetActive(Current != null && hasSlot);
            _hireBtn.interactable = canPay;
        }

        // 보상 모드는 무료라 비용 표시를 숨긴다
        if (_hireCostText != null)
        {
            _hireCostText.gameObject.SetActive(!_isFree);
            int cost = GameplayConfig.Current?.HireMercenaryCost ?? 500;
            _hireCostText.text  = $"{cost:N0}";
            _hireCostText.color = canPay ? new Color(1.00f, 0.85f, 0.20f)
                                         : new Color(0.90f, 0.35f, 0.35f);
        }
        _hireCostIcon?.gameObject.SetActive(!_isFree);

        if (_passBtnLabel != null) _passBtnLabel.text = $"+{_totalCandidateShards}";
        _passBtn?.gameObject.SetActive(true);

        if (_hintText != null)
            _hintText.text = hasSlot
                ? "한 명을 고용하거나, 전부 돌려보내 용병 조각을 받는다."
                : "배치 슬롯이 가득 찼다 — 전부 돌려보내 용병 조각으로 바꿀 수 있다.";
    }

    bool CanAffordHire()
    {
        int cost = GameplayConfig.Current?.HireMercenaryCost ?? 500;
        return UserDataManager.Instance?.Get<ItemData>()?.CanSpend(eItem.Gold, cost) ?? false;
    }

    // ── 자세히 보기 ───────────────────────────────────────────

    void OpenDetail(UnitEntry entry)
    {
        var pm = PopupManager.Instance;
        if (pm == null) return;

        var detail = pm.IsOpen(PopupType.HeroDetail)
            ? pm.Get<HeroDetailPopup>(PopupType.HeroDetail)
            : pm.Open<HeroDetailPopup>(PopupType.HeroDetail);

        detail?.SetupPreview(entry);
    }

    // ── 고용 / 분해 ───────────────────────────────────────────

    void OnHire()
    {
        var entry = Current;
        if (entry == null) return;

        int slot = _targetSlot >= 0 ? _targetSlot : GetFirstEmptySlot();
        if (slot < 0) return;

        // 유료 모드는 잔액을 먼저 확인한다 — 배치까지 끝난 뒤 차감에 실패하면 공짜가 된다
        if (!_isFree && !CanAffordHire()) return;

        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        unitData?.AddUnit(entry);
        deployData?.Deploy(entry.UnitName, slot);
        JobSynergyEvaluator.Recalculate();
        _hired = true;

        CloseDetailAndSelf();
    }

    void OnDecompose()
    {
        UserDataManager.Instance?.Get<ItemData>()?.Add(eItem.SoldierShard, _totalCandidateShards);
        CloseDetailAndSelf();
    }

    void CloseDetailAndSelf()
    {
        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.HeroDetail))
            pm.Get<HeroDetailPopup>(PopupType.HeroDetail)?.Close();
        Close();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    /// <summary>
    /// 배치 가능한 첫 빈 슬롯. 유물·특성으로 열린 슬롯 수까지만 본다 —
    /// 잠긴 칸에 넣으면 고용은 됐는데 전투에 나오지 않는다.
    /// </summary>
    int GetFirstEmptySlot()
    {
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        if (deployData == null) return -1;

        int activeSlots = Mathf.Min(5, RelicApplier.GetTotalActiveGeneralSlots());
        for (int i = 0; i < activeSlots; i++)
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) return i;
        return -1;
    }

    static int CalcShards(UnitEntry e)
    {
        int[] bases = { 5, 10, 20, 35, 60 };
        return bases[Mathf.Clamp((int)e.Grade, 0, bases.Length - 1)] + e.Level / 5;
    }
}
