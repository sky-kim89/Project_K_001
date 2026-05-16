using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercenaryShopPopup.cs
//  용병 상점 팝업 (전체 화면 오버레이).
//
//  레이아웃:
//    SlotFullView  — 좌측, BattlePanel DeployArea와 동일 위치
//                    배치된 장수 5명 표시 + 해고 버튼
//    CandidatePanel— 우측 플로팅 패널
//                    후보 카드 3장 + 고용/분해 버튼
//
//  양쪽이 항상 동시에 표시되므로 현재 장수와 새 후보를 나란히 비교 가능.
//  자세히 보기 클릭 → HeroDetailPopup(preview) + 선택 카드 하일라이트.
//
//  팝업 닫힘 시 무조건 골드 소모 (GameplayConfig.HireMercenaryCost).
// ============================================================

public class MercenaryShopPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("뷰 컨테이너 (참조용)")]
    [SerializeField] GameObject _candidateView;
    [SerializeField] GameObject _slotFullView;

    [Header("후보 카드 (CandidateView)")]
    [SerializeField] MercCandidateCardUI[] _candidateCards;   // 3개

    [Header("고용 액션")]
    [SerializeField] Button          _hireBtn;
    [SerializeField] Button          _passBtn;
    [SerializeField] TextMeshProUGUI _passBtnLabel;
    [SerializeField] Image           _passShardIcon;

    [Header("슬롯 카드 (SlotFullView)")]
    [SerializeField] MercSlotCardUI[] _slotCards;             // 5개

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    // ── 런타임 상태 ──────────────────────────────────────────

    readonly List<UnitEntry> _candidates          = new();
    UnitEntry                _selected;
    int                      _targetSlot          = -1;
    int                      _totalCandidateShards;

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        _hireBtn ?.onClick.AddListener(OnHire);
        _passBtn ?.onClick.AddListener(OnDecompose);

        if (_passShardIcon != null)
        {
            var sp = SpriteManager.Instance?.GetItem(eItem.SoldierShard.IconKey());
            _passShardIcon.sprite = sp;
            _passShardIcon.color  = sp != null ? Color.white : new Color(0.45f, 0.70f, 1.00f);
        }
    }

    public MercenaryShopPopup Setup(int targetSlot)
    {
        _targetSlot = targetSlot;
        return this;
    }

    protected override void OnAfterOpen()
    {
        GenerateCandidates();
        RefreshView();
    }

    protected override void OnAfterClose()
    {
        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.HeroDetail))
            pm.Get<HeroDetailPopup>(PopupType.HeroDetail)?.Close();

        int cost = GameplayConfig.Current?.HireMercenaryCost ?? 500;
        UserDataManager.Instance?.Get<ItemData>()?.Spend(eItem.Gold, cost);
        UserDataManager.Instance?.RequestSave();
    }

    // ── 후보 생성 ─────────────────────────────────────────────

    void GenerateCandidates()
    {
        _candidates.Clear();
        _totalCandidateShards = 0;

        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (unitData == null) return;

        var pool  = unitData.GetAvailableNames();
        int count = Mathf.Min(3, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(i, pool.Count);
            (pool[i], pool[idx]) = (pool[idx], pool[i]);
            var entry = new UnitEntry { UnitName = pool[i], Level = 1, Exp = 0 };
            _candidates.Add(entry);
            _totalCandidateShards += CalcShards(entry);
        }

        if (_passBtnLabel != null)
            _passBtnLabel.text = $"+{_totalCandidateShards}";
    }

    // ── 뷰 갱신 (항상 양쪽 모두 표시) ────────────────────────

    void RefreshView()
    {
        _selected = null;
        ClearHighlights();
        RefreshHireBar();
        BuildCandidateCards();
        RefreshSlotView();
    }

    void BuildCandidateCards()
    {
        if (_candidateCards == null) return;
        for (int i = 0; i < _candidateCards.Length; i++)
        {
            if (_candidateCards[i] == null) continue;
            if (i < _candidates.Count)
            {
                int capturedIdx = i;
                _candidateCards[i].gameObject.SetActive(true);
                _candidateCards[i].Setup(_candidates[i],
                    () => SelectCandidate(_candidates[capturedIdx], capturedIdx));
            }
            else
            {
                _candidateCards[i].gameObject.SetActive(false);
            }
        }
    }

    void RefreshSlotView()
    {
        if (_slotCards == null) return;

        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();

        int deployedCount = 0;
        for (int i = 0; i < 5; i++)
            if (!string.IsNullOrEmpty(deployData?.GetUnitAt(i))) deployedCount++;
        bool canFire = deployedCount > 1;

        for (int i = 0; i < _slotCards.Length; i++)
        {
            if (_slotCards[i] == null) continue;

            string name  = deployData?.GetUnitAt(i) ?? "";
            var    entry = !string.IsNullOrEmpty(name) ? unitData?.GetUnit(name) : null;

            if (entry != null)
            {
                UnitEntry capturedEntry = entry;
                _slotCards[i].ShowOccupied(capturedEntry, () => FireMercenary(capturedEntry), canFire);
            }
            else
            {
                _slotCards[i].ShowEmpty();
            }
        }
    }

    // ── 후보 선택 + 하일라이트 ───────────────────────────────

    void SelectCandidate(UnitEntry entry, int cardIdx)
    {
        _selected = entry;

        for (int i = 0; i < _candidateCards.Length; i++)
            _candidateCards[i]?.SetHighlighted(i == cardIdx);

        RefreshHireBar();

        var pm = PopupManager.Instance;
        var detail = (pm != null && pm.IsOpen(PopupType.HeroDetail))
            ? pm.Get<HeroDetailPopup>(PopupType.HeroDetail)
            : pm?.Open<HeroDetailPopup>(PopupType.HeroDetail, onClose: OnHeroDetailClosed);

        detail?.SetupPreview(entry);
    }

    void OnHeroDetailClosed()
    {
        _selected = null;
        ClearHighlights();
        RefreshHireBar();
    }

    void ClearHighlights()
    {
        if (_candidateCards == null) return;
        foreach (var card in _candidateCards)
            card?.SetHighlighted(false);
    }

    void RefreshHireBar()
    {
        bool has     = _selected != null;
        bool hasSlot = GetFirstEmptySlot() >= 0;
        _hireBtn?.gameObject.SetActive(has && hasSlot);
        _passBtn?.gameObject.SetActive(true);
    }

    // ── 해고 ─────────────────────────────────────────────────

    void FireMercenary(UnitEntry entry)
    {
        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();

        deployData?.Undeploy(entry.UnitName);
        unitData?.RemoveUnit(entry.UnitName);

        _selected = null;
        ClearHighlights();
        RefreshView();
    }

    // ── 고용 / 분해 ───────────────────────────────────────────

    void OnHire()
    {
        if (_selected == null) return;

        int slot = _targetSlot >= 0 ? _targetSlot : GetFirstEmptySlot();
        if (slot < 0) return;

        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        unitData?.AddUnit(_selected);
        deployData?.Deploy(_selected.UnitName, slot);

        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.HeroDetail))
            pm.Get<HeroDetailPopup>(PopupType.HeroDetail)?.Close();

        Close();
    }

    void OnDecompose()
    {
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        itemData?.Add(eItem.SoldierShard, _totalCandidateShards);

        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.HeroDetail))
            pm.Get<HeroDetailPopup>(PopupType.HeroDetail)?.Close();

        Close();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    int GetFirstEmptySlot()
    {
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        if (deployData == null) return -1;
        for (int i = 0; i < 5; i++)
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) return i;
        return -1;
    }

    static int CalcShards(UnitEntry e)
    {
        int[] bases = { 5, 10, 20, 35, 60 };
        return bases[Mathf.Clamp((int)e.Grade, 0, bases.Length - 1)] + e.Level / 5;
    }
}
