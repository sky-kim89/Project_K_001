using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RewardCardUI.cs
//  획득한 보상 하나를 보여주는 카드. 전투 결과·이벤트 결과 공용.
//
//  ■ 구성 (스크린샷 스타일 — 아이콘이 카드 전면)
//    Card (Button)
//    ├─ Frame        등급·종류 색 테두리
//    ├─ IconImage    카드를 가득 채운다
//    ├─ AmountBadge  우하단 수량 배지 — 수량 개념이 없으면 비활성
//    ├─ RevealOverlay "?" (미개봉 박스 전용)
//    └─ Tooltip      InfoTooltipUI — 이름/설명/스탯
//
//  ■ 모드
//    Setup(RewardView)  — 확정된 보상. 누르면 상세 툴팁.
//    SetupBox(...)      — 랜덤 박스. "?" 로 시작, 누르면 개봉 → 결과로 갱신.
//
//  보상 종류별 내용(아이콘·색·이름·설명·스탯·수량)은 전부
//  RewardInfoResolver 가 만든다 — 카드는 그리기만 한다.
// ============================================================

public class RewardCardUI : MonoBehaviour
{
    [SerializeField] Image           _frame;
    [SerializeField] Image           _icon;
    [SerializeField] GameObject      _amountBadge;
    [SerializeField] TextMeshProUGUI _amountText;
    [SerializeField] GameObject      _revealOverlay;
    [SerializeField] Button          _cardButton;
    [SerializeField] InfoTooltipUI   _tooltip;

    public bool IsOpened { get; private set; }

    Action     _pendingOpen;
    RewardInfo _info;

    // ── 확정 보상 ────────────────────────────────────────────

    public void Setup(RewardView view)
    {
        IsOpened = true;
        _pendingOpen = null;

        _revealOverlay.SetActive(false);
        Apply(RewardInfoResolver.Resolve(view));

        _cardButton.onClick.RemoveAllListeners();
        _cardButton.onClick.AddListener(ShowTooltip);
    }

    // ── 박스 (탭 개봉) ───────────────────────────────────────

    public void SetupBox(ItemAmount source, int stageLevel,
                         Action<OpenedReward> onResult,
                         Action onOpened)
    {
        IsOpened = false;

        _revealOverlay.SetActive(true);
        Apply(RewardInfoResolver.Resolve(RewardView.OfBox(source.Item, source.Amount)));

        // 미개봉 상태에서는 아이콘·수량을 감춘다 — 무엇이 나올지 모르는 게 연출이다.
        _icon.enabled = false;
        _amountBadge.SetActive(false);

        _pendingOpen = () => OpenBox(source, stageLevel, onResult, onOpened);

        _cardButton.onClick.RemoveAllListeners();
        _cardButton.onClick.AddListener(() => _pendingOpen());
    }

    public void TryOpen()
    {
        if (!IsOpened) _pendingOpen();
    }

    // ── 생명주기 ─────────────────────────────────────────────
    //  툴팁은 열리면 루트 캔버스로 옮겨진다 — 카드가 사라져도
    //  같이 사라지지 않으므로 여기서 정리한다.

    void OnDisable() => _tooltip.Close();

    void OnDestroy()
    {
        if (_tooltip != null && _tooltip.IsOpen)
            Destroy(_tooltip.gameObject);
    }

    // ── 내부 ─────────────────────────────────────────────────

    void Apply(RewardInfo info)
    {
        _info = info;

        _frame.color = info.Accent;

        _icon.sprite  = info.Icon;
        _icon.color   = info.Icon != null ? Color.white : info.Accent;
        _icon.enabled = true;

        // 수량이 없는 보상(특성·어빌리티·장비)은 카운터 자체를 끈다.
        bool hasAmount = !string.IsNullOrEmpty(info.AmountLabel);
        _amountBadge.SetActive(hasAmount);
        if (hasAmount) _amountText.text = info.AmountLabel;
    }

    void ShowTooltip() => _tooltip.Show(_info.Name, _info.Description, _info.StatText);

    void OpenBox(ItemAmount source, int stageLevel,
                 Action<OpenedReward> onResult,
                 Action onOpened)
    {
        if (IsOpened) return;
        IsOpened = true;

        var result = RewardOpener.Commit(source, stageLevel);

        _revealOverlay.SetActive(false);

        // 개봉 결과로 카드를 다시 그린다 — 장비가 나왔으면 장비 카드가 된다.
        Apply(RewardInfoResolver.Resolve(
            result.Equipment != null
                ? RewardView.OfEquipment(result.Equipment.EquipmentId)
                : RewardView.OfItem(source.Item, source.Amount)));

        _cardButton.onClick.RemoveAllListeners();
        _cardButton.onClick.AddListener(ShowTooltip);

        onResult?.Invoke(result);
        onOpened?.Invoke();
    }
}
