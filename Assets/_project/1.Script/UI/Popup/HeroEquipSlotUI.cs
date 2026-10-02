using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================
//  HeroEquipSlotUI.cs
//  HeroDetailPopup 의 장비 칸 하나 — 초상화 옆에 세로로 3칸 놓인다.
//
//  ■ 아이콘만 보여 준다
//    장비 이름·스탯·추가 옵션은 칸 안에 넣지 않는다.
//    칸을 누르면 EquipComparePopup 이 열려 거기서 전부 보여 준다.
//    (예전 "장비" 탭이 하던 일과 같다 — 탭만 없어졌다)
//
//  ■ 구조 (HeroDetailPopupCreator.BuildEquipSlot)
//    Slot
//    ├─ Frame        등급 색 테두리
//    ├─ IconPit / Icon
//    ├─ EmptyMark    빈 칸일 때만 (＋ 도형)
//    ├─ EnhanceBadge "+3" — 강화 0이면 숨김
//    └─ EnhanceBtn   [🔧 2] 강화 — 장비가 있을 때만
// ============================================================

public class HeroEquipSlotUI : MonoBehaviour
{
    [SerializeField] Image           _frame;
    [SerializeField] Image           _icon;
    [SerializeField] GameObject      _emptyMark;
    [SerializeField] TextMeshProUGUI _enhanceBadge;
    [SerializeField] Button          _selectBtn;
    [SerializeField] Button          _enhanceBtn;
    [SerializeField] TextMeshProUGUI _enhanceCostText;
    [SerializeField] Image           _enhanceCostIcon;

    //  ⚠ 프레임(＋ 네모)은 장비 자리만 나타낸다 — 높이를 바꾸지 말 것
    //    빈 칸이라고 강화 버튼 자리까지 프레임을 늘린 적이 있는데,
    //    그러면 "장비가 비었다" 가 아니라 "강화 버튼까지가 장비 칸" 으로 읽힌다.
    //    강화 버튼은 장비에 딸린 별개의 조작이지 장비 칸의 일부가 아니다.
    //    세 칸의 프레임은 채워졌든 비었든 항상 같은 크기다.

    static readonly Color EmptyFrame = new(0.24f, 0.25f, 0.34f, 1f);
    static readonly Color EmptyIcon  = new(0.16f, 0.17f, 0.24f, 1f);
    static readonly Color CostOk     = new(0.80f, 0.90f, 1.00f, 1f);
    static readonly Color CostShort  = new(0.90f, 0.35f, 0.35f, 1f);

    /// <summary>강화 비용 아이콘 — 팝업이 스프라이트를 채워 넣는다.</summary>
    public Image EnhanceCostIcon => _enhanceCostIcon;

    // ── 배선 (Awake 대신 팝업이 한 번 호출) ──────────────────

    public void Bind(Action onSelect, Action onEnhance)
    {
        _selectBtn.onClick.RemoveAllListeners();
        _selectBtn.onClick.AddListener(() => onSelect());

        HoldRepeatButton.Bind(_enhanceBtn, onEnhance);
    }

    // ── 표시 ─────────────────────────────────────────────────

    public void SetEquipment(EquipmentData data, int enhance, int cost, int owned)
    {
        _frame.color = GradeStyle.GetColor(data.Grade);

        _icon.enabled = true;
        _icon.sprite  = data.Icon;
        _icon.color   = data.Icon != null ? Color.white : GradeStyle.GetColor(data.Grade);

        _emptyMark.SetActive(false);

        _enhanceBadge.gameObject.SetActive(enhance > 0);
        _enhanceBadge.text = $"+{enhance}";

        _enhanceBtn.gameObject.SetActive(true);
        _enhanceCostText.text  = $"{cost}";
        _enhanceCostText.color = owned >= cost ? CostOk : CostShort;
    }

    public void SetEmpty()
    {
        _frame.color  = EmptyFrame;
        _icon.enabled = false;
        _icon.sprite  = null;
        _icon.color   = EmptyIcon;

        _emptyMark.SetActive(true);
        _enhanceBadge.gameObject.SetActive(false);
        _enhanceBtn.gameObject.SetActive(false);
    }
}

/// <summary>짧게 누르면 한 번, 길게 누르면 일정 간격으로 반복 실행하는 버튼.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class HoldRepeatButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    const float HoldDelay      = 0.45f;
    const float RepeatInterval = 0.12f;

    Button _button;
    Action _action;
    bool   _pressed;
    bool   _repeated;
    float  _remaining;

    public static void Bind(Button button, Action action)
    {
        var repeat = button.GetComponent<HoldRepeatButton>();
        if (repeat == null) repeat = button.gameObject.AddComponent<HoldRepeatButton>();

        repeat._action = action;
        button.onClick.RemoveAllListeners();
    }

    void Awake() => _button = GetComponent<Button>();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.IsInteractable()) return;

        _pressed   = true;
        _repeated  = false;
        _remaining = HoldDelay;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressed) return;

        _pressed = false;
        if (!_repeated && _button.IsInteractable()) _action();
    }

    public void OnPointerExit(PointerEventData eventData) => _pressed = false;

    void OnDisable() => _pressed = false;

    void Update()
    {
        if (!_pressed) return;
        if (!_button.IsInteractable()) { _pressed = false; return; }

        _remaining -= Time.unscaledDeltaTime;
        if (_remaining > 0f) return;

        _repeated  = true;
        _remaining = RepeatInterval;
        _action();
    }
}
