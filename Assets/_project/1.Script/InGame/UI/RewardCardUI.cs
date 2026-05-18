using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RewardCardUI.cs
//  전투 결과 팝업의 보상 카드 하나.
//
//  두 가지 모드:
//    SetupFixed()  — 직접 아이템(골드 등). 즉시 확정 표시.
//    SetupBox()    — 랜덤 박스. "?" 상태로 시작, 탭하면 개봉.
//
//  Note: NameText는 크리에이터에서 제거됨 — 필드만 유지.
// ============================================================

public class RewardCardUI : MonoBehaviour
{
    [SerializeField] Image           _bgImage;
    [SerializeField] Image           _icon;
    [SerializeField] TextMeshProUGUI _nameText;    // Creator에서 제거됨 (숨김 처리)
    [SerializeField] TextMeshProUGUI _amountText;
    [SerializeField] GameObject      _revealOverlay;
    [SerializeField] Button          _cardButton;

    public bool IsOpened { get; private set; }

    Action _pendingOpen;

    // ── 직접 아이템 (즉시 확정) ──────────────────────────────

    public void SetupFixed(Sprite icon, Color bgColor, string name, string amountLabel)
    {
        IsOpened = true;

        if (_revealOverlay != null) _revealOverlay.SetActive(false);
        if (_bgImage != null)       _bgImage.color = bgColor;
        if (_icon != null)
        {
            _icon.sprite  = icon;
            _icon.color   = icon != null ? Color.white : bgColor;
            _icon.enabled = true;
        }
        if (_nameText != null)   _nameText.gameObject.SetActive(false);
        if (_amountText != null) _amountText.text = amountLabel;

        _cardButton.gameObject.SetActive(false);
    }

    // ── 박스 아이템 (탭 개봉) ────────────────────────────────

    public void SetupBox(ItemAmount source, int stageLevel,
                         Action<OpenedReward> onResult,
                         Action onOpened)
    {
        IsOpened = false;

        if (_revealOverlay != null) _revealOverlay.SetActive(true);
        if (_bgImage != null)       _bgImage.color = new Color(0.25f, 0.25f, 0.35f, 1f);
        if (_icon != null)          _icon.enabled  = false;
        if (_nameText != null)      _nameText.gameObject.SetActive(false);
        if (_amountText != null)    _amountText.text = "";

        _pendingOpen = () => OpenBox(source, stageLevel, onResult, onOpened);

        _cardButton.gameObject.SetActive(true);
        _cardButton.onClick.RemoveAllListeners();
        _cardButton.onClick.AddListener(() => _pendingOpen());
    }

    public void TryOpen()
    {
        if (!IsOpened)
            _pendingOpen();
    }

    // ── 내부 ─────────────────────────────────────────────────

    void OpenBox(ItemAmount source, int stageLevel,
                 Action<OpenedReward> onResult,
                 Action onOpened)
    {
        if (IsOpened) return;
        IsOpened = true;

        var result = RewardOpener.Commit(source, stageLevel);

        if (_revealOverlay != null) _revealOverlay.SetActive(false);

        if (result.Equipment != null)
        {
            var equip = result.Equipment;
            var color = GradeStyle.GetColor(equip.Grade);

            if (_bgImage != null) _bgImage.color = color;
            if (_icon != null)
            {
                _icon.sprite  = equip.Icon;
                _icon.color   = equip.Icon != null ? Color.white : color;
                _icon.enabled = true;
            }
            if (_amountText != null) _amountText.text = GradeStyle.GetLabel(equip.Grade);
        }

        _cardButton.gameObject.SetActive(false);

        onResult?.Invoke(result);
        onOpened?.Invoke();
    }
}
