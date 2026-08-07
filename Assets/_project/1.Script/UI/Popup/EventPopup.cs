using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EventPopup.cs
//  이벤트 팝업 — 선택지형 / 즉시보상형 양쪽을 처리한다.
//
//  사용법:
//    var p = PopupManager.Instance.Open<EventPopup>(PopupType.Event);
//    p.Setup(eventData);          // 기본
//    p.SetupAbilityResources(...) // 어빌리티 선택 보상이 있을 때 추가 호출
//
//  흐름:
//    선택지형 → 본문 + 버튼들 표시 → 선택 → 결과 텍스트 표시 → 확인 닫기
//    즉시보상형 → 본문 표시 → 보상 즉시 지급 → 결과 텍스트 표시 → 확인 닫기
//    어빌리티 선택 → 결과 표시 후 AbilitySelectPopup 연속 오픈 → 완료 후 확인 버튼 활성화
//    상점 열기   → 결과 표시 후 RunShopPopup 오픈 → 상점을 닫으면 확인 버튼 활성화
//                 (상점 스테이지의 "행상인의 좌판" 이벤트가 이 경로를 쓴다)
// ============================================================

public class EventPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    // ── Inspector ─────────────────────────────────────────────

    [Header("이벤트 정보")]
    [SerializeField] Image           _illustration;
    [SerializeField] TextMeshProUGUI _titleTmp;
    [SerializeField] TextMeshProUGUI _titleShadowTmp;   // 타이틀 그림자 사본
    [SerializeField] TextMeshProUGUI _bodyTmp;

    [Header("선택지")]
    [SerializeField] Transform       _choiceRoot;
    [SerializeField] Button          _choiceButtonTemplate;
    [SerializeField] GameObject      _choiceDivider;    // "선 택" 구분선

    [Header("결과")]
    [SerializeField] GameObject      _resultPanel;
    [SerializeField] TextMeshProUGUI _resultTmp;
    [SerializeField] Button          _confirmBtn;

    [Header("획득 보상 (전투 결과 팝업과 같은 카드)")]
    [SerializeField] Transform    _rewardArea;
    [SerializeField] RewardCardUI _rewardCardPrefab;

    // 삽화가 없는 이벤트용 placeholder 색 (EventPopupCreator.IllustBg 와 동일)
    static readonly Color IllustPlaceholder = new Color(0.10f, 0.07f, 0.16f, 1f);

    // ── 런타임 상태 ───────────────────────────────────────────

    EventData _data;

    // 어빌리티 선택 팝업 연속 처리에 필요한 리소스
    AbilityDatabase    _abilityDb;
    RunAbilityData     _runAbilityData;
    RelicInventoryData _relicInventory;
    RelicDatabase      _relicDb;
    ReincarnationData  _reincarnationData;

    int _pendingAbilityCount;

    readonly List<Button>       _choiceButtons = new();
    readonly List<RewardCardUI> _rewardCards   = new();

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>팝업 오픈 전 반드시 호출 (이벤트 데이터 주입).</summary>
    public EventPopup Setup(EventData data)
    {
        _data = data;
        return this;
    }

    /// <summary>어빌리티 선택 보상이 포함된 이벤트에만 추가 호출.</summary>
    public EventPopup SetupAbilityResources(
        AbilityDatabase    abilityDb,
        RunAbilityData     runAbilityData,
        RelicInventoryData relicInventory  = null,
        RelicDatabase      relicDb         = null,
        ReincarnationData  reincarnationData = null)
    {
        _abilityDb         = abilityDb;
        _runAbilityData    = runAbilityData;
        _relicInventory    = relicInventory;
        _relicDb           = relicDb;
        _reincarnationData = reincarnationData;
        return this;
    }

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _confirmBtn.onClick.AddListener(() => Close());
    }

    protected override void OnAfterOpen()
    {
        if (_data == null) return;

        // 삽화 — 스프라이트가 있으면 교체하고 tint 를 흰색으로 되돌린다.
        // (프리팹 기본 색은 어두운 placeholder 라, 흰색으로 안 바꾸면 삽화가 그 색에 곱해져 묻힌다)
        // 팝업은 재사용되므로 삽화가 없는 이벤트에서는 반드시 placeholder 로 되돌린다.
        _illustration.sprite = _data.Illustration;
        _illustration.color  = _data.Illustration != null ? Color.white : IllustPlaceholder;

        _titleTmp.text       = _data.Title;
        _titleShadowTmp.text = _data.Title;
        _bodyTmp.text        = _data.Body;

        _resultPanel.SetActive(false);
        _confirmBtn.gameObject.SetActive(false);

        // 즉시보상형은 선택지가 없으므로 "선 택" 구분선도 감춘다
        bool hasChoices = _data.Choices != null && _data.Choices.Length > 0;
        _choiceDivider.SetActive(hasChoices);

        if (hasChoices) BuildChoiceButtons();
        else            ProcessInstantReward();
    }

    protected override void OnAfterClose()
    {
        ClearChoiceButtons();
        ClearRewardCards();
        _data              = null;
        _abilityDb         = null;
        _runAbilityData    = null;
        _relicInventory    = null;
        _relicDb           = null;
        _reincarnationData = null;
        _pendingAbilityCount = 0;
    }

    // ── 즉시 보상형 ──────────────────────────────────────────

    void ProcessInstantReward()
    {
        bool needsAbility = HasReward(_data.InstantRewards, EventRewardType.OpenAbilitySelect);
        bool needsShop    = HasReward(_data.InstantRewards, EventRewardType.OpenRunShop);
        var granted = EventRewardHandler.Apply(_data.InstantRewards, OnAbilitySelectRequired);
        ShowResult(_data.InstantResultText, granted, suppressConfirm: needsAbility || needsShop);
        if (needsShop) OpenRunShop();
    }

    // ── 선택지 버튼 빌드 ─────────────────────────────────────

    void BuildChoiceButtons()
    {
        _choiceButtonTemplate.gameObject.SetActive(false);

        foreach (var choice in _data.Choices)
        {
            if (choice == null) continue;

            var btn = Instantiate(_choiceButtonTemplate, _choiceRoot);
            btn.gameObject.SetActive(true);

            // 버튼 레이블 + 비용 힌트.
            // 경로가 어긋나면 여기서 바로 터져야 한다 — 예전에 이름이 안 맞아
            // 라벨이 조용히 플레이스홀더로 남는 버그가 있었다.
            var labelTmp = btn.transform.Find("Body/LabelText").GetComponent<TextMeshProUGUI>();
            var hintTmp  = btn.transform.Find("Body/HintText").GetComponent<TextMeshProUGUI>();

            labelTmp.text = choice.Label;
            {
                string hint = BuildChoiceHint(choice);
                hintTmp.text = hint;
                hintTmp.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            }

            // 비용 충족 여부에 따라 버튼 비활성화
            bool canSelect = EventRewardHandler.CanApply(choice.SuccessRewards)
                          || choice.SuccessRate < 1f; // 실패 분기가 있는 선택지는 항상 활성
            btn.interactable = canSelect;

            var captured = choice;
            btn.onClick.AddListener(() => OnChoiceSelected(captured));
            _choiceButtons.Add(btn);
        }
    }

    void ClearChoiceButtons()
    {
        foreach (var b in _choiceButtons)
            if (b != null) Destroy(b.gameObject);
        _choiceButtons.Clear();
        _choiceButtonTemplate.gameObject.SetActive(false);
    }

    // ── 선택 처리 ─────────────────────────────────────────────

    void OnChoiceSelected(EventChoice choice)
    {
        // 모든 버튼 즉시 비활성화 (중복 선택 방지)
        foreach (var b in _choiceButtons) b.interactable = false;

        // 성공/실패 분기
        EventReward[] rewards = (choice.SuccessRate >= 1f || UnityEngine.Random.value <= choice.SuccessRate)
            ? choice.SuccessRewards
            : choice.FailRewards;

        bool needsAbility = HasReward(rewards, EventRewardType.OpenAbilitySelect);
        bool needsShop    = HasReward(rewards, EventRewardType.OpenRunShop);
        var granted = EventRewardHandler.Apply(rewards, OnAbilitySelectRequired);
        ShowResult(choice.ResultText, granted, suppressConfirm: needsAbility || needsShop);
        if (needsShop) OpenRunShop();
    }

    // ── 런 상점 체이닝 ───────────────────────────────────────
    //  상점을 닫아야 이벤트를 확인할 수 있다 — 확인 버튼은 그때 열린다.

    void OpenRunShop()
    {
        var shop = PopupManager.Instance.Open<RunShopPopup>(PopupType.RunShop);
        shop.SetOnClose(() => _confirmBtn.interactable = true);
    }

    // ── 결과 표시 ─────────────────────────────────────────────

    void ShowResult(string text, List<RewardView> granted, bool suppressConfirm)
    {
        ClearChoiceButtons();
        _choiceDivider.SetActive(false);

        _resultPanel.SetActive(true);
        _resultTmp.text = text ?? string.Empty;

        BuildRewardCards(granted);

        _confirmBtn.gameObject.SetActive(true);
        _confirmBtn.interactable = !suppressConfirm;
    }

    // ── 획득 보상 카드 ────────────────────────────────────────
    //  전투 결과 팝업과 같은 RewardCardUI 를 쓴다 — 눌러서 상세를 볼 수 있다.

    void BuildRewardCards(List<RewardView> granted)
    {
        ClearRewardCards();

        _rewardArea.gameObject.SetActive(granted.Count > 0);
        if (granted.Count == 0) return;

        foreach (var view in granted)
        {
            var card = Instantiate(_rewardCardPrefab, _rewardArea);
            card.Setup(view);
            _rewardCards.Add(card);
        }
    }

    /// <summary>어빌리티 선택처럼 결과 표시 뒤에 확정되는 보상을 뒤에 덧붙인다.</summary>
    void AppendRewardCard(RewardView view)
    {
        _rewardArea.gameObject.SetActive(true);
        var card = Instantiate(_rewardCardPrefab, _rewardArea);
        card.Setup(view);
        _rewardCards.Add(card);
    }

    void ClearRewardCards()
    {
        foreach (var c in _rewardCards)
            if (c != null) Destroy(c.gameObject);
        _rewardCards.Clear();
    }

    // ── 어빌리티 선택 팝업 체이닝 ────────────────────────────

    void OnAbilitySelectRequired(int count)
    {
        _pendingAbilityCount = count;
        OpenNextAbilitySelect();
    }

    void OpenNextAbilitySelect()
    {
        if (_pendingAbilityCount <= 0)
        {
            if (_confirmBtn != null) _confirmBtn.interactable = true;
            return;
        }

        _pendingAbilityCount--;

        var popup = PopupManager.Instance?.Open<AbilitySelectPopup>(PopupType.AbilitySelect);
        if (popup == null)
        {
            OpenNextAbilitySelect();
            return;
        }

        AbilityData[] choices = (_abilityDb != null && _runAbilityData != null)
            ? AbilityPicker.Pick(_abilityDb, _runAbilityData, _relicInventory, _relicDb)
            : null;

        popup.Setup(
            choices,
            chosen =>
            {
                if (chosen != null)
                {
                    _runAbilityData?.AddAbility(chosen.Id);
                    AppendRewardCard(RewardView.OfAbility(chosen.Id));   // 결과창에 이어 붙인다
                }
                UserDataManager.Instance?.RequestSave();
                OpenNextAbilitySelect();  // 다음 선택 또는 완료
            },
            _abilityDb, _runAbilityData, _relicInventory, _relicDb, _reincarnationData);
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static string BuildChoiceHint(EventChoice choice)
    {
        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(choice.CostHint))
            sb.Append($"<color=#FFAA44>{choice.CostHint}</color>");

        if (choice.SuccessRewards != null)
            foreach (var r in choice.SuccessRewards)
                if (!string.IsNullOrEmpty(r.Description))
                {
                    if (sb.Length > 0) sb.Append("   ");
                    sb.Append($"<color=#55EE88>{r.Description}</color>");
                }

        return sb.ToString();
    }

    /// <summary>
    /// 결과 표시 뒤에 다른 팝업을 이어 여는 보상(어빌리티 선택·상점)이 있는지.
    /// OpenAbilitySelect 는 횟수가 0이면 열 것이 없으므로 제외한다.
    /// </summary>
    static bool HasReward(EventReward[] rewards, EventRewardType type)
    {
        if (rewards == null) return false;
        foreach (var r in rewards)
        {
            if (r.Type != type) continue;
            if (type == EventRewardType.OpenAbilitySelect && r.IntValue <= 0) continue;
            return true;
        }
        return false;
    }
}
