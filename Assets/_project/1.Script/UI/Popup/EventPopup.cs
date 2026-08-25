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
        ReincarnationData  reincarnationData = null)
    {
        _abilityDb         = abilityDb;
        _runAbilityData    = runAbilityData;
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
        _reincarnationData = null;
        _pendingAbilityCount = 0;
    }

    // ── 즉시 보상형 ──────────────────────────────────────────

    void ProcessInstantReward()
    {
        bool needsAbility = HasReward(_data.InstantRewards, EventRewardType.OpenAbilitySelect);
        bool needsShop    = HasReward(_data.InstantRewards, EventRewardType.OpenRunShop);
        bool needsMerc    = HasReward(_data.InstantRewards, EventRewardType.OpenMercenary);
        var granted = EventRewardHandler.Apply(_data.InstantRewards, OnAbilitySelectRequired);
        ShowResult(_data.InstantResultText, granted,
                   suppressConfirm: needsAbility || needsShop || needsMerc);
        if (needsShop) OpenRunShop();
        if (needsMerc) OpenMercenary();
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
        //
        // ⚠ 보상과 텍스트를 같은 판정에서 뽑아야 한다
        //   예전엔 보상만 분기하고 텍스트는 늘 ResultText 를 썼다.
        //   그래서 묘약을 마셔 디버프를 받아도 "강한 힘이 솟구칩니다 [강화 특성 2개]"
        //   가 떠서, 화면에 뜬 특성과 글이 정반대인 상황이 나왔다.
        bool success = choice.SuccessRate >= 1f
                    || UnityEngine.Random.value <= choice.SuccessRate;

        EventReward[] rewards = success ? choice.SuccessRewards : choice.FailRewards;

        // 실패 텍스트를 안 채운 옛 데이터는 성공 텍스트로 되돌아간다 (빈 화면보다는 낫다)
        string resultText = !success && !string.IsNullOrEmpty(choice.FailResultText)
            ? choice.FailResultText
            : choice.ResultText;

        bool needsAbility = HasReward(rewards, EventRewardType.OpenAbilitySelect);
        bool needsShop    = HasReward(rewards, EventRewardType.OpenRunShop);
        bool needsMerc    = HasReward(rewards, EventRewardType.OpenMercenary);
        var granted = EventRewardHandler.Apply(rewards, OnAbilitySelectRequired);
        ShowResult(resultText, granted,
                   suppressConfirm: needsAbility || needsShop || needsMerc);
        if (needsShop) OpenRunShop();
        if (needsMerc) OpenMercenary();
    }

    // ── 런 상점 체이닝 ───────────────────────────────────────
    //  상점을 닫아야 이벤트를 확인할 수 있다 — 확인 버튼은 그때 열린다.

    void OpenRunShop()
    {
        var shop = PopupManager.Instance.Open<RunShopPopup>(PopupType.RunShop);
        shop.SetOnClose(() => _confirmBtn.interactable = true);
    }

    // ── 용병 고용 체이닝 ─────────────────────────────────────
    //  상점과 같은 규칙 — 고용 팝업을 닫아야 이벤트 확인 버튼이 열린다.
    //  대가(골드·용병조각)는 이벤트 선택지의 SpendItem 이 이미 받았으므로
    //  여기서는 무료 모드로 연다.

    void OpenMercenary()
    {
        var merc = PopupManager.Instance.Open<MercenaryShopPopup>(
            PopupType.MercenaryShop, onClose: () => _confirmBtn.interactable = true);
        merc.SetupAsReward();
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
            ? AbilityPicker.Pick(_abilityDb, _runAbilityData)
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
            _abilityDb, _runAbilityData, _reincarnationData);
    }

    // ── 유틸 ─────────────────────────────────────────────────

    // 낼 수 있는 비용은 주황, 모자라는 비용은 빨강.
    const string CostColor = "FFAA44";
    const string LackColor = "FF5555";

    /// <summary>
    /// 선택지 버튼 우측 소자 — <b>비용만</b> 적는다 ("골드 -320").
    ///
    /// ■ 보상은 미리 알려주지 않는다
    ///   무엇을 얻을지 버튼에 적혀 있으면 고를 이유가 없어진다 — 이벤트가
    ///   자판기가 된다. 결과는 고른 뒤 본문(ResultText)과 보상 표시로만 드러낸다.
    ///   비용은 예외다. 내가 무엇을 내는지 모르고 누르게 하면 안 된다.
    ///
    /// ■ 비용은 손으로 적은 문구가 아니라 SpendItem 보상에서 뽑는다
    ///   버튼에 적힌 숫자와 실제로 빠져나가는 숫자가 어긋나면 안 된다.
    ///   ResolveAmount 를 거치므로 스테이지 비례(ScaleByStageReward) 비용도
    ///   그 순간의 실제 금액으로 찍힌다.
    ///
    /// ⚠ CostHint 는 SpendItem 이 하나도 없을 때만 쓴다
    ///   골드를 쓰는 선택지에 "필요: 골드 200" 이 적혀 있으면 자동 표시와 겹쳐
    ///   같은 말이 두 번 나온다. CostHint 는 아이템이 아닌 조건
    ///   (병사 수·특성 보유 같은 것)을 적는 자리로 남긴다.
    ///
    /// ⚠ EventReward.Description 은 여기서 쓰지 않는다
    ///   그 문구는 결과 표시용이다. 버튼에 끌어다 쓰면 "특성 획득" 같은
    ///   보상 예고가 되살아난다.
    /// </summary>
    static string BuildChoiceHint(EventChoice choice)
    {
        var sb = new System.Text.StringBuilder();

        //  ⚠ 색은 항목마다 따로 본다
        //    골드와 강화석을 같이 내는 선택지에서 강화석만 모자라면, 빨간 건
        //    강화석 하나여야 한다. 선택지 전체(CanApply)로 칠하면 멀쩡한 골드까지
        //    빨개져서 무엇이 모자란지 알 수 없다.
        var items     = UserDataManager.Instance?.Get<ItemData>();
        int costCount = 0;

        if (choice.SuccessRewards != null)
            foreach (var r in choice.SuccessRewards)
            {
                if (r.Type != EventRewardType.SpendItem) continue;   // 보상은 버튼에 적지 않는다
                if (r.Item == eItem.None)                continue;

                int amt = EventRewardHandler.ResolveAmount(r);
                if (amt <= 0) continue;

                // 판정은 CanApply 와 같은 함수를 쓴다 — 색과 버튼 활성이 갈리면 안 된다
                bool canPay = items == null || items.CanSpend(r.Item, amt);

                Append(sb, canPay ? CostColor : LackColor,
                       $"{r.Item.DisplayName()} -{amt:N0}");
                costCount++;
            }

        // 아이템 비용이 하나도 없을 때만 손으로 적은 조건 문구를 쓴다.
        // 문구만으로는 무엇을 얼마나 내는지 알 수 없어 충족 여부를 따질 수 없다 — 주황 고정.
        if (costCount == 0 && !string.IsNullOrEmpty(choice.CostHint))
            Append(sb, CostColor, choice.CostHint);

        return sb.ToString();
    }

    static void Append(System.Text.StringBuilder sb, string colorHex, string text)
    {
        if (sb.Length > 0) sb.Append("   ");
        sb.Append($"<color=#{colorHex}>{text}</color>");
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
