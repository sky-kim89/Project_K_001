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
//    Setup(slot)      — 유료. 실제로 고용했을 때만 고용가를 차감한다 (등급별).
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

    [Header("현재 부대 (칸 클릭 → HeroDetailPopup 상세)")]
    [SerializeField] Button[]          _squadBtns;
    [SerializeField] TextMeshProUGUI[] _squadNames;
    [SerializeField] TextMeshProUGUI[] _squadLevels;

    [Tooltip("칸 바로 아래 [해고]. 처리·보호 규칙은 GeneralRoster 가 소유한다.")]
    [SerializeField] Button[]          _squadFireBtns;

    [Header("액션")]
    [SerializeField] Button          _hireBtn;
    [SerializeField] TextMeshProUGUI _hireBtnLabel;   // 모드마다 문구가 다르다
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
    int  _hiredCost;   // 고용 확정 시점의 가격 — 차감은 팝업이 닫힐 때 일어난다

    // ── 상점 재확인 모드 ─────────────────────────────────────
    //  런 상점(RunShopPopup)에서 고른 매물 한 명만 띄우고 "정말 살 것인가" 를 묻는다.
    //  후보 추첨도, 돌려보내기도 없다.
    UnitEntry     _shopEntry;
    System.Action _onShopHired;

    bool IsShopConfirm => _shopEntry != null;

    const int CandidateCount = 3;

    const string HireLabel     = "고     용";
    const string ShopBuyLabel  = "골드 주고 구매하기";

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

        for (int i = 0; i < _squadBtns.Length; i++)
        {
            int slot = i;
            _squadBtns[i].onClick.AddListener(() => OnSquadSlotClick(slot));
        }

        if (_squadFireBtns != null)
            for (int i = 0; i < _squadFireBtns.Length; i++)
            {
                int slot = i;
                _squadFireBtns[i]?.onClick.AddListener(() => OnSquadFireClick(slot));
            }
    }

    /// <summary>유료 고용 — 지정 슬롯에 배치한다. 고용을 확정해야 골드가 빠진다.</summary>
    public MercenaryShopPopup Setup(int targetSlot)
    {
        _targetSlot = targetSlot;
        _isFree     = false;
        ClearShopMode();
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
        ClearShopMode();
        return this;
    }

    /// <summary>
    /// 런 상점 매물 재확인 — 상점에서 고른 <b>그 한 명</b>만 띄우고 살지 묻는다.
    ///
    /// ■ 왜 상점에서 바로 고용하지 않고 한 단계를 더 두나
    ///   배치 슬롯이 꽉 찬 상태에서 상점이 직접 고용하면 "슬롯 없음" 으로
    ///   조용히 실패하는 것 말고 할 수 있는 게 없었다. 이 팝업에는 현재 부대
    ///   5칸이 있어, 누굴 내보낼지 그 자리에서 정하고(→ HeroDetailPopup 해고)
    ///   곧바로 구매까지 이어갈 수 있다.
    ///
    /// ■ 이 모드에서만 다른 것
    ///   · 후보 추첨을 하지 않는다 — 상점에서 본 매물이 그대로 나와야 한다.
    ///   · [돌려보내기] 를 감춘다 — 공짜로 받은 후보가 아니라 조각으로 바꿀 게 없다.
    ///   · 버튼 문구가 "골드 주고 구매하기" 가 된다.
    ///
    /// <paramref name="onHired"/> 는 <b>골드 차감까지 끝난 뒤</b> 불린다
    /// (차감은 닫기 애니메이션이 끝나는 OnAfterClose 에서 일어난다).
    /// </summary>
    public MercenaryShopPopup SetupFromShop(UnitEntry entry, System.Action onHired)
    {
        _targetSlot  = -1;
        _isFree      = false;
        _shopEntry   = entry;
        _onShopHired = onHired;
        return this;
    }

    // ⚠ 팝업 인스턴스는 재사용된다 — 다른 모드로 열 때 반드시 지운다.
    //   안 지우면 엘리트 보상 고용이 지난번 상점 매물을 그대로 띄운다.
    void ClearShopMode()
    {
        _shopEntry   = null;
        _onShopHired = null;
    }

    protected override void OnAfterOpen()
    {
        _hired     = false;
        _hiredCost = 0;
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
            UserDataManager.Instance?.Get<ItemData>()?.Spend(eItem.Gold, _hiredCost);
        UserDataManager.Instance?.RequestSave();

        // 상점에 결과를 알린다 — 차감이 끝난 뒤라야 상점 헤더의 보유 골드가 맞다.
        if (_hired) _onShopHired?.Invoke();
        ClearShopMode();
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

        // 상점 재확인 — 추첨하지 않는다. 상점에서 본 매물이 그대로 나와야 한다.
        if (IsShopConfirm)
        {
            _candidates.Add(_shopEntry);
            return;   // 조각 합계는 0 — 이 모드엔 돌려보내기가 없다
        }

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
        RefreshSquad();
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
            _hireBtn.gameObject.SetActive(Current != null);
            _hireBtn.interactable = hasSlot && canPay;
        }

        // 보상 모드는 무료라 비용 표시를 숨긴다
        if (_hireCostText != null)
        {
            _hireCostText.gameObject.SetActive(!_isFree);
            int cost = CurrentHireCost();
            _hireCostText.text  = $"{cost:N0}";
            _hireCostText.color = canPay ? new Color(1.00f, 0.85f, 0.20f)
                                         : new Color(0.90f, 0.35f, 0.35f);
        }
        _hireCostIcon?.gameObject.SetActive(!_isFree);

        // 버튼 문구 — 상점 재확인은 "산다" 는 뜻이 분명해야 한다
        if (_hireBtnLabel != null)
            _hireBtnLabel.text = IsShopConfirm ? ShopBuyLabel : HireLabel;

        // 돌려보내기 — 상점 재확인 모드엔 없다.
        //
        // ⚠ 회색으로 남겨 두지 않고 감춘다
        //   이 버튼의 뜻은 "받은 후보를 전부 조각으로 바꾼다" 다. 상점 매물은 아직
        //   받은 게 아니라서 바꿀 것이 없고, 남겨 두면 "+0" 이 붙은 죽은 버튼이 된다.
        //   할 수 없는 일을 자리만 차지한 채 보여 주는 쪽이 더 헷갈린다.
        if (_passBtnLabel != null) _passBtnLabel.text = $"+{_totalCandidateShards}";
        _passBtn?.gameObject.SetActive(!IsShopConfirm);

        if (_hintText != null)
            _hintText.text = hasSlot
                ? (IsShopConfirm
                    ? "이 용병을 고용한다. 골드는 구매를 확정할 때 빠진다."
                    : "한 명을 고용하거나, 전부 돌려보내 용병 조각을 받는다.")
                : "배치 슬롯이 가득 찼다 — 아래 부대에서 한 명을 해고하면 고용할 수 있다.";
    }

    // ── 현재 부대 ─────────────────────────────────────────────
    //
    //  슬롯이 꽉 차면 고용 버튼이 비활성화된다(RefreshActionBar).
    //  좋은 장수가 떴는데 아무것도 못 하는 상황을 막으려면 여기서 바로
    //  누굴 내보낼지 정할 수 있어야 한다 — 칸을 누르면 HeroDetailPopup 이 열린다.
    //
    //  ⚠ 해고를 여기서 처리하지 않는다
    //    해고 로직과 "마지막 1명은 해고 불가" 보호는 HeroDetailPopup 이 소유한다.
    //    여기서 또 구현하면 보호 규칙이 두 벌이 된다.

    static readonly Color SquadDimText = new Color(0.45f, 0.48f, 0.56f);
    static readonly Color SquadLvText  = new Color(0.78f, 0.82f, 0.90f);

    void RefreshSquad()
    {
        var deploy = UserDataManager.Instance?.Get<DeploymentData>();
        var units  = UserDataManager.Instance?.Get<UnitData>();

        // 유물·특성으로 열린 칸까지만 실제 슬롯이다 — 잠긴 칸에 넣으면 전투에 안 나온다
        int activeSlots = RelicTreeApplier.GetTotalActiveGeneralSlots();

        for (int i = 0; i < _squadBtns.Length; i++)
        {
            bool   unlocked = i < activeSlots;
            string occupant = unlocked ? deploy?.GetUnitAt(i) : null;
            var    entry    = string.IsNullOrEmpty(occupant) ? null : units?.GetUnit(occupant);

            _squadBtns[i].interactable = entry != null;

            _squadNames[i].text  = entry != null ? entry.UnitName
                                 : unlocked      ? "비어 있음"
                                                 : "잠 김";
            // 등급은 이름 색으로 표시한다 — 칸 배경을 갈아끼우면 버튼 눌림 색 계산이 어긋난다
            _squadNames[i].color = entry != null ? GradeStyle.GetColor(entry.Grade) : SquadDimText;

            _squadLevels[i].text  = entry != null ? $"Lv.{entry.Level}" : "—";
            _squadLevels[i].color = entry != null ? SquadLvText : SquadDimText;

            // 해고 — 사람이 서 있고, 마지막 1명이 아닐 때만 누를 수 있다.
            // ⚠ 보호 규칙을 여기서 다시 세지 않는다 (GeneralRoster 가 정본)
            if (_squadFireBtns != null && i < _squadFireBtns.Length && _squadFireBtns[i] != null)
            {
                _squadFireBtns[i].gameObject.SetActive(entry != null);
                _squadFireBtns[i].interactable = entry != null && GeneralRoster.CanFire();
            }
        }
    }

    void OnSquadSlotClick(int slot)
    {
        var deploy = UserDataManager.Instance.Get<DeploymentData>();
        var units  = UserDataManager.Instance.Get<UnitData>();

        // 빈 칸·잠긴 칸 버튼은 비활성이라 여기까지 오지 않는다
        var entry = units.GetUnit(deploy.GetUnitAt(slot));

        var detail = PopupManager.Instance.Open<HeroDetailPopup>(
            PopupType.HeroDetail, onClose: OnSquadDetailClosed);
        detail.Setup(entry);
    }

    // 해고했으면 빈 슬롯이 생겨 고용 버튼이 다시 살아나고,
    // 등급업·레벨업만 했어도 부대 표시가 달라진다 → 통째로 갱신한다.
    void OnSquadDetailClosed()
    {
        RefreshSquad();
        RefreshActionBar();
    }

    /// <summary>
    /// 칸 아래 [해고] — 슬롯이 꽉 찼을 때 여기서 바로 자리를 비운다.
    ///
    /// ■ 왜 상세 팝업을 거치지 않나
    ///   "좋은 용병이 떴는데 자리가 없다" 는 이 화면의 핵심 상황이다.
    ///   그때마다 칸 → 상세 팝업 → 해고 → 닫기까지 네 번을 눌러야 했다.
    ///
    /// ⚠ 확인 창을 띄우지 않는다
    ///   버튼이 부대 칸 아래에 붙어 있어 누가 지워지는지 눈으로 보이고,
    ///   마지막 1명은 애초에 눌리지 않는다. 되돌리기가 필요한 만큼
    ///   위험한 조작이면 그건 GeneralRoster 에서 막을 일이다.
    /// </summary>
    void OnSquadFireClick(int slot)
    {
        var deploy = UserDataManager.Instance.Get<DeploymentData>();
        string occupant = deploy.GetUnitAt(slot);

        // 빈 칸 버튼은 꺼져 있어 여기까지 오지 않는다
        if (!GeneralRoster.Fire(occupant)) return;

        RefreshSquad();
        RefreshActionBar();
    }

    bool CanAffordHire()
        => UserDataManager.Instance?.Get<ItemData>()?.CanSpend(eItem.Gold, CurrentHireCost()) ?? false;

    /// <summary>지금 보고 있는 후보의 고용가 — 등급이 높을수록 비싸다.</summary>
    int CurrentHireCost()
        => Current != null ? GameplayConfig.HireCost(Current.Grade)
                           : GameplayConfig.HireCost(UnitGrade.Normal);

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

        // ⚠ AddUnit 은 중복을 걸러 주지 않는다 (그냥 리스트에 Add 한다)
        //   후보 목록은 팝업을 열 때 뽑히므로, 그 사이 같은 이름을 다른 경로로
        //   얻었으면 부대에 같은 장수가 둘 생긴다.
        if (unitData != null && !unitData.HasUnit(entry.UnitName))
            unitData.AddUnit(entry);
        deployData?.Deploy(entry.UnitName, slot);
        JobSynergyEvaluator.Recalculate();
        _hired     = true;
        _hiredCost = GameplayConfig.HireCost(entry.Grade);

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

        int activeSlots = Mathf.Min(5, RelicTreeApplier.GetTotalActiveGeneralSlots());
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
