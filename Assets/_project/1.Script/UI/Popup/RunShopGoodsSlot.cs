using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopGoodsSlot.cs
//  런 상점의 "상품 한 칸" — 장비·특성 공용.
//
//  ■ 왜 카드 하나로 통일했나
//    장비는 기본 스탯에 더해 추가 옵션(트리거)까지 붙는다.
//    "치명타 15% 확률: 공격력 +20% (5초)" 같은 줄은 어떤 칸 폭으로도
//    한 줄에 안 들어가고, 억지로 넣으면 줄이 접히거나 통째로 사라진다.
//    → 칸에는 아이콘·이름·가격만 두고, 상세는 눌렀을 때 툴팁으로 보여준다.
//      아이콘·툴팁은 전투 결과·이벤트 보상과 같은 RewardCardUI 를 쓴다.
//
//  ■ 구조 (프리팹: RunShopPopupCreator.BuildGoodsSlot)
//    Slot
//    ├─ CardHolder   ← RewardCardUI 를 런타임에 하나 만들어 넣는다
//    ├─ NameText     상품 이름 (한 줄, AutoSize)
//    ├─ KindText     "장비 · 희귀" / "특성"
//    ├─ BuyBtn       [금화] 가격  — 입체 버튼
//    └─ SoldPlate    구매 후 버튼 자리에 덮는 판 ("구매 완료")
//
//  품절 상태에서도 카드는 계속 누를 수 있다 — 무엇을 샀는지 다시 볼 수 있어야 한다.
// ============================================================

public class RunShopGoodsSlot : MonoBehaviour
{
    [SerializeField] RewardCardUI    _cardPrefab;
    [SerializeField] RectTransform   _cardHolder;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _kindText;
    [SerializeField] TextMeshProUGUI _costText;
    [SerializeField] Button          _buyBtn;
    [SerializeField] GameObject      _soldPlate;
    [SerializeField] TextMeshProUGUI _soldText;

    static readonly Color TraitKindColor = new(0.82f, 0.64f, 1.00f);
    static readonly Color EmptyColor     = new(0.42f, 0.44f, 0.55f);

    RewardCardUI _card;
    Func<bool>   _onBuy;   // 반환값 = 실제로 샀는지 (골드 부족이면 false)
    int          _cost;
    bool         _sold;

    /// <summary>이 칸의 가격. 살 것이 없으면 0.</summary>
    public int Cost => _cost;

    /// <summary>구매·품절 상태면 true — 골드 부족 판정에서 제외한다.</summary>
    public bool IsSold => _sold;

    // ── 상품 세팅 ────────────────────────────────────────────

    public void SetupEquipment(EquipmentData data, int cost, Func<bool> onBuy)
    {
        // 가운뎃점(·)은 기본 폰트에 있다는 보장이 없어 쓰지 않는다 — 띄어쓰기로 나눈다.
        Bind(RewardView.OfEquipment(data.EquipmentId),
             data.EquipmentName,
             $"{GradeStyle.GetLabel(data.Grade)} 장비",
             GradeStyle.GetColor(data.Grade),
             cost, onBuy);
    }

    public void SetupTrait(TraitData data, int cost, Func<bool> onBuy)
    {
        Bind(RewardView.OfTrait(data.TraitType),
             data.TraitName, "특성", TraitKindColor, cost, onBuy);
    }

    /// <summary>내놓을 물건이 없는 칸 (특성 풀 고갈 등).</summary>
    public void SetEmpty()
    {
        _onBuy = null;
        _cost  = 0;
        _sold  = true;

        if (_card != null) _card.gameObject.SetActive(false);

        _nameText.text  = "—";
        _nameText.color = EmptyColor;
        _kindText.text  = "";

        _buyBtn.gameObject.SetActive(false);
        _soldPlate.SetActive(true);
        _soldText.text = "품 절";
    }

    /// <summary>구매 완료 / 이미 보유 상태로 표시. 카드는 계속 눌러 볼 수 있다.</summary>
    public void SetSoldOut(string label)
    {
        _sold = true;
        _buyBtn.gameObject.SetActive(false);
        _soldPlate.SetActive(true);
        _soldText.text = label;
    }

    /// <summary>골드가 모자라면 버튼을 잠근다.</summary>
    public void SetAffordable(bool canAfford)
    {
        if (_sold) return;
        _buyBtn.interactable = canAfford;
    }

    /// <summary>
    /// 진열은 그대로 두고 가격표만 갈아 끼운다.
    ///
    /// ⚠ 값이 변하는 상품은 이걸로만 고칠 것 — Setup*() 을 다시 부르면 안 된다
    ///   특성처럼 "보유 개수에 따라 값이 오르는" 상품은 하나를 사면 옆 칸 값도
    ///   같이 올라야 한다. 그런데 슬롯을 다시 세우면 추첨 풀에서 방금 산 특성이
    ///   빠지면서 셔플 결과가 통째로 바뀌어, 아직 안 산 옆 칸의 특성이
    ///   다른 것으로 갈린다 — 공짜 새로고침이 된다.
    /// </summary>
    public void SetCost(int cost)
    {
        if (_sold) return;
        _cost          = cost;
        _costText.text = $"{cost:N0}";
    }

    // ── 내부 ─────────────────────────────────────────────────

    void Bind(RewardView view, string name, string kind, Color kindColor,
              int cost, Func<bool> onBuy)
    {
        _onBuy = onBuy;
        _cost  = cost;
        _sold  = false;

        // 카드는 한 번만 만들고 새로고침 때마다 내용만 갈아 끼운다.
        if (_card == null)
        {
            _card = Instantiate(_cardPrefab, _cardHolder);
            var rt = _card.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        _card.gameObject.SetActive(true);
        _card.Setup(view);

        _nameText.text  = name;
        _nameText.color = Color.white;
        _kindText.text  = kind;
        _kindText.color = kindColor;
        _costText.text  = $"{cost:N0}";

        _soldPlate.SetActive(false);
        _buyBtn.gameObject.SetActive(true);
        _buyBtn.interactable = true;
        _buyBtn.onClick.RemoveAllListeners();
        _buyBtn.onClick.AddListener(OnBuyClicked);
    }

    // 실제로 산 경우에만 품절 처리한다 —
    // 실패(골드 부족)했는데 품절로 덮으면 살 수 있는 물건이 사라진다.
    void OnBuyClicked()
    {
        if (_onBuy()) SetSoldOut("구매 완료");
    }
}
