using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DisEquipRowUI.cs
//  분해 팝업 장비 행 — 아이콘·등급·레벨·보상 표시.
//  HeroPanelCreator.BuildEquipRowTemplate() 에서 직렬화 필드 연결.
// ============================================================

public class DisEquipRowUI : MonoBehaviour
{
    [SerializeField] Image            _icon;
    [SerializeField] TextMeshProUGUI  _nameTmp;
    [SerializeField] TextMeshProUGUI  _gradeTmp;
    [SerializeField] TextMeshProUGUI  _levelTmp;
    [SerializeField] Image            _rewardIcon;
    [SerializeField] TextMeshProUGUI  _rewardTmp;

    public void Fill(EquipmentData equip)
    {
        if (_icon != null)
        {
            _icon.sprite = equip.Icon;
            _icon.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade);
        }

        if (_nameTmp != null) _nameTmp.text = equip.EquipmentName;

        if (_gradeTmp != null)
        {
            _gradeTmp.text  = GradeStyle.GetLabel(equip.Grade);
            _gradeTmp.color = GradeStyle.GetColor(equip.Grade);
        }

        if (_levelTmp  != null) _levelTmp.text  = $"아이템 Lv.{equip.ItemLevel}";
        if (_rewardTmp != null) _rewardTmp.text  = $"{equip.ItemLevel}석";

        if (_rewardIcon != null)
            _rewardIcon.sprite = SpriteManager.Instance?.Get(eItem.EquipUpgradeStone.IconKey());
    }
}
