using System;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EquipPickSlotUI.cs
//  EquipComparePopup 인벤토리 격자의 칸 하나.
//
//  ■ 왜 컴포넌트로 뺐나
//    예전에는 런타임에 맨 Image + Button 만 만들어 붙였다. 그래서
//    등급을 알 수 없었고, 눌러도 어느 칸을 골랐는지 표시가 없었다.
//    (선택 결과는 위쪽 비교 카드에만 반영돼 시선이 두 번 왕복했다)
//
//  ■ 구조 (EquipComparePopupCreator.BuildPickTemplate)
//    Slot (Button)
//    ├─ Frame       등급 색 테두리
//    ├─ Pit / Icon  아이콘
//    └─ SelectMark  선택된 칸에만 켜지는 밝은 테두리
// ============================================================

public class EquipPickSlotUI : MonoBehaviour
{
    [SerializeField] Image      _frame;
    [SerializeField] Image      _icon;
    [SerializeField] GameObject _selectMark;
    [SerializeField] Button     _btn;

    public void Setup(EquipmentData data, Action onClick)
    {
        _frame.color = GradeStyle.GetColor(data.Grade);

        _icon.sprite = data.Icon;
        _icon.color  = data.Icon != null ? Color.white : GradeStyle.GetColor(data.Grade);

        _selectMark.SetActive(false);

        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(() => onClick());
    }

    public void SetSelected(bool on) => _selectMark.SetActive(on);
}
