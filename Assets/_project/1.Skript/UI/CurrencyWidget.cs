using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  CurrencyWidget.cs
//  재화 1종의 현재 수량을 표시하는 재사용 가능 UI 컴포넌트.
//
//  Inspector 설정:
//    Item       : 표시할 재화 종류 (eItem)
//    AmountText : 수량을 보여줄 TextMeshProUGUI
//    Icon       : (선택) 재화 아이콘 Image
//
//  동작:
//    - OnEnable 시 ItemData.OnItemChanged 구독 + 현재값 즉시 표시
//    - 해당 eItem 변경 이벤트 수신 시 텍스트 갱신
//    - OnDisable 시 구독 해제
// ============================================================

public class CurrencyWidget : MonoBehaviour
{
    [SerializeField] eItem           _item;
    [SerializeField] TextMeshProUGUI _amountText;
    [SerializeField] Image           _icon;

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()
    {
        ItemData.OnItemChanged += HandleItemChanged;
        Refresh();
    }

    void OnDisable()
    {
        ItemData.OnItemChanged -= HandleItemChanged;
    }

    // ── 이벤트 ────────────────────────────────────────────────

    void HandleItemChanged(eItem item, int newAmount)
    {
        if (item != _item) return;
        SetText(newAmount);
    }

    // ── 내부 ──────────────────────────────────────────────────

    void Refresh()
    {
        var items  = UserDataManager.Instance?.Get<ItemData>();
        int amount = items?.Get(_item) ?? 0;
        SetText(amount);
    }

    void SetText(int amount)
    {
        if (_amountText == null) return;
        _amountText.text = Format(amount);
    }

    // 10000 이상은 'k' 단위로 축약 (예: 12500 → "12.5k")
    static string Format(int amount)
    {
        if (amount >= 10000) return $"{amount / 1000f:F1}k";
        return amount.ToString("N0");
    }
}
