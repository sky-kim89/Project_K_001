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
// ============================================================

public class EventPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    // ── Inspector ─────────────────────────────────────────────

    [Header("이벤트 정보")]
    [SerializeField] Image           _illustration;
    [SerializeField] TextMeshProUGUI _titleTmp;
    [SerializeField] TextMeshProUGUI _bodyTmp;

    [Header("선택지")]
    [SerializeField] Transform       _choiceRoot;
    [SerializeField] Button          _choiceButtonTemplate;

    [Header("결과")]
    [SerializeField] GameObject      _resultPanel;
    [SerializeField] TextMeshProUGUI _resultTmp;
    [SerializeField] Button          _confirmBtn;

    // ── 런타임 상태 ───────────────────────────────────────────

    EventData _data;

    // 어빌리티 선택 팝업 연속 처리에 필요한 리소스
    AbilityDatabase    _abilityDb;
    RunAbilityData     _runAbilityData;
    RelicInventoryData _relicInventory;
    RelicDatabase      _relicDb;
    ReincarnationData  _reincarnationData;

    int _pendingAbilityCount;

    readonly List<Button> _choiceButtons = new();

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
        _confirmBtn?.onClick.AddListener(() => Close());
    }

    protected override void OnAfterOpen()
    {
        if (_data == null) return;

        // 삽화
        if (_illustration != null)
        {
            bool hasIllust = _data.Illustration != null;
            _illustration.gameObject.SetActive(hasIllust);
            if (hasIllust) _illustration.sprite = _data.Illustration;
        }

        if (_titleTmp != null) _titleTmp.text = _data.Title;
        if (_bodyTmp  != null) _bodyTmp.text  = _data.Body;

        _resultPanel?.SetActive(false);
        _confirmBtn?.gameObject.SetActive(false);

        if (_data.Choices == null || _data.Choices.Length == 0)
            ProcessInstantReward();
        else
            BuildChoiceButtons();
    }

    protected override void OnAfterClose()
    {
        ClearChoiceButtons();
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
        bool needsAbility = HasAbilitySelectReward(_data.InstantRewards);
        EventRewardHandler.Apply(_data.InstantRewards, OnAbilitySelectRequired);
        ShowResult(_data.InstantResultText, suppressConfirm: needsAbility);
    }

    // ── 선택지 버튼 빌드 ─────────────────────────────────────

    void BuildChoiceButtons()
    {
        if (_choiceButtonTemplate == null) return;
        _choiceButtonTemplate.gameObject.SetActive(false);

        foreach (var choice in _data.Choices)
        {
            if (choice == null) continue;

            var btn = Instantiate(_choiceButtonTemplate, _choiceRoot);
            btn.gameObject.SetActive(true);

            // 버튼 레이블 + 비용 힌트
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = string.IsNullOrEmpty(choice.CostHint)
                    ? choice.Label
                    : $"{choice.Label}  <size=70%><color=#FFD700>{choice.CostHint}</color></size>";
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
        if (_choiceButtonTemplate != null)
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

        bool needsAbility = HasAbilitySelectReward(rewards);
        EventRewardHandler.Apply(rewards, OnAbilitySelectRequired);
        ShowResult(choice.ResultText, suppressConfirm: needsAbility);
    }

    // ── 결과 표시 ─────────────────────────────────────────────

    void ShowResult(string text, bool suppressConfirm)
    {
        ClearChoiceButtons();
        if (_resultPanel != null) _resultPanel.SetActive(true);
        if (_resultTmp   != null) _resultTmp.text = text ?? string.Empty;

        if (_confirmBtn != null)
        {
            _confirmBtn.gameObject.SetActive(true);
            _confirmBtn.interactable = !suppressConfirm;
        }
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
                if (chosen != null) _runAbilityData?.AddAbility(chosen.Id);
                UserDataManager.Instance?.RequestSave();
                OpenNextAbilitySelect();  // 다음 선택 또는 완료
            },
            _abilityDb, _runAbilityData, _relicInventory, _relicDb, _reincarnationData);
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static bool HasAbilitySelectReward(EventReward[] rewards)
    {
        if (rewards == null) return false;
        foreach (var r in rewards)
            if (r.Type == EventRewardType.OpenAbilitySelect && r.IntValue > 0)
                return true;
        return false;
    }
}
