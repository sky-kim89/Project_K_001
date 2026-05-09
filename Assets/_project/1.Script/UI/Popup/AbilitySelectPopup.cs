using System;
using TMPro;
using UnityEngine;

// ============================================================
//  AbilitySelectPopup.cs
//  스테이지 클리어 후 어빌리티 3택 선택 팝업.
//
//  사용법:
//    var popup = PopupManager.Instance.Open<AbilitySelectPopup>(PopupType.AbilitySelect);
//    popup.Setup(choices, chosen => { runData.AddAbility(chosen.Id); });
// ============================================================

public class AbilitySelectPopup : PopupBase
{
    [Header("카드 슬롯 (3개, 왼쪽부터)")]
    [SerializeField] AbilityCardUI[] _cards;

    [Header("타이틀")]
    [SerializeField] TextMeshProUGUI _titleTmp;

    AbilityData[]        _choices;
    Action<AbilityData>  _onSelected;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>팝업을 열기 전에 반드시 호출한다.</summary>
    public void Setup(AbilityData[] choices, Action<AbilityData> onSelected)
    {
        _choices    = choices;
        _onSelected = onSelected;
    }

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void OnAfterOpen()
    {
        if (_titleTmp != null) _titleTmp.text = "어빌리티 선택";

        if (_cards == null) return;

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == null) continue;

            bool hasData = _choices != null && i < _choices.Length && _choices[i] != null;
            _cards[i].gameObject.SetActive(hasData);
            if (hasData) _cards[i].Setup(_choices[i], OnCardClicked);
        }
    }

    protected override void OnAfterClose()
    {
        _choices    = null;
        _onSelected = null;
    }

    // ── 선택 처리 ─────────────────────────────────────────────

    void OnCardClicked(AbilityData chosen)
    {
        var cb = _onSelected;
        Close(() => cb?.Invoke(chosen));
    }
}
