using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilitySelectPopup.cs
//  스테이지 클리어 후 어빌리티 3택 선택 팝업.
//
//  새로고침 기능:
//    - 가용 횟수 = RelicApplier.GetSystemInt(AbilityRefreshCount, ...)
//    - 누적 사용 = ReincarnationData.UsedRefreshCount (환생 전까지 초기화 안 됨)
//    - 남은 횟수 = 가용 - 누적
//
//  사용법:
//    var popup = PopupManager.Instance.Open<AbilitySelectPopup>(PopupType.AbilitySelect);
//    popup.Setup(choices, chosen => { runData.AddAbility(chosen.Id); },
//                db, runData, relicInventory, relicDb, reincarnationData);
// ============================================================

public class AbilitySelectPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("카드 슬롯 (3개, 왼쪽부터)")]
    [SerializeField] AbilityCardUI[] _cards;

    [Header("타이틀")]
    [SerializeField] TextMeshProUGUI _titleTmp;

    [Header("새로고침")]
    [SerializeField] Button          _refreshBtn;
    [SerializeField] TextMeshProUGUI _refreshCountTmp;

    AbilityData[]            _choices;
    Action<AbilityData>      _onSelected;
    readonly List<AbilityCardUI> _dynamicCards = new();

    AbilityDatabase    _abilityDb;
    RunAbilityData     _runData;
    RelicInventoryData _relicInventory;
    RelicDatabase      _relicDb;
    ReincarnationData  _reincarnationData;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>팝업을 열기 전에 반드시 호출한다.</summary>
    public void Setup(
        AbilityData[] choices,
        Action<AbilityData> onSelected,
        AbilityDatabase abilityDb         = null,
        RunAbilityData runData            = null,
        RelicInventoryData relicInventory = null,
        RelicDatabase relicDb             = null,
        ReincarnationData reincarnationData = null)
    {
        _choices           = choices;
        _onSelected        = onSelected;
        _abilityDb         = abilityDb;
        _runData           = runData;
        _relicInventory    = relicInventory;
        _relicDb           = relicDb;
        _reincarnationData = reincarnationData;
    }

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void OnAfterOpen()
    {
        if (_titleTmp != null) _titleTmp.text = "어빌리티 선택";

        RefreshCards();
        RefreshButton();

        if (_refreshBtn != null)
        {
            _refreshBtn.onClick.RemoveAllListeners();
            _refreshBtn.onClick.AddListener(OnRefreshClicked);
        }
    }

    protected override void OnAfterClose()
    {
        DestroyDynamicCards();
        _choices           = null;
        _onSelected        = null;
        _abilityDb         = null;
        _runData           = null;
        _relicInventory    = null;
        _relicDb           = null;
        _reincarnationData = null;
    }

    void DestroyDynamicCards()
    {
        foreach (var c in _dynamicCards)
            if (c != null) Destroy(c.gameObject);
        _dynamicCards.Clear();
    }

    // ── 새로고침 ─────────────────────────────────────────────

    void RefreshButton()
    {
        if (_refreshBtn == null) return;

        int available = (_relicInventory != null && _relicDb != null)
            ? RelicApplier.GetSystemInt(RelicSystemEffect.AbilityRefreshCount, _relicInventory, _relicDb)
            : 0;
        int used      = _reincarnationData?.UsedRefreshCount ?? 0;
        int remaining = Mathf.Max(0, available - used);

        _refreshBtn.interactable = remaining > 0 && _abilityDb != null && _runData != null;
        if (_refreshCountTmp != null)
            _refreshCountTmp.text = $"새로고침  {remaining}회 남음";
    }

    void OnRefreshClicked()
    {
        if (_abilityDb == null || _runData == null) return;

        int available = (_relicInventory != null && _relicDb != null)
            ? RelicApplier.GetSystemInt(RelicSystemEffect.AbilityRefreshCount, _relicInventory, _relicDb)
            : 0;
        int remaining = Mathf.Max(0, available - (_reincarnationData?.UsedRefreshCount ?? 0));
        if (remaining <= 0) return;

        _reincarnationData?.UseRefresh();
        UserDataManager.Instance?.RequestSave();

        _choices = AbilityPicker.Pick(_abilityDb, _runData, _relicInventory, _relicDb);
        RefreshCards();
        RefreshButton();
    }

    // ── 카드 갱신 ─────────────────────────────────────────────

    void RefreshCards()
    {
        if (_cards == null) return;

        DestroyDynamicCards();

        int choiceCount = _choices?.Length ?? 0;

        // 고정 슬롯 처리
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null) continue;
            bool hasData = i < choiceCount && _choices[i] != null;
            _cards[i].gameObject.SetActive(hasData);
            if (hasData) _cards[i].Setup(_choices[i], OnCardClicked, _runData?.GetLevel(_choices[i].Id) ?? 0);
        }

        // 고정 슬롯 초과분 — 첫 번째 카드를 템플릿으로 동적 생성
        if (_cards.Length > 0 && _cards[0] != null)
        {
            var parent = _cards[0].transform.parent;
            for (int i = _cards.Length; i < choiceCount; i++)
            {
                if (_choices[i] == null) continue;
                var extra = Instantiate(_cards[0], parent);
                extra.gameObject.SetActive(true);
                extra.Setup(_choices[i], OnCardClicked, _runData?.GetLevel(_choices[i].Id) ?? 0);
                _dynamicCards.Add(extra);
            }
        }
    }

    // ── 선택 처리 ─────────────────────────────────────────────

    void OnCardClicked(AbilityData chosen)
    {
        var cb = _onSelected;
        Close(() => cb?.Invoke(chosen));
    }
}
