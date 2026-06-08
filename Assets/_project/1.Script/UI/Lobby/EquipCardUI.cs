using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EquipCardUI.cs
//  장비 선택 팝업에서 보유 장비 하나를 표시하는 카드.
// ============================================================

public class EquipCardUI : MonoBehaviour
{
    [SerializeField] Image           _gradeBar;
    [SerializeField] Image           _icon;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _gradeText;
    [SerializeField] TextMeshProUGUI _statText;
    [SerializeField] Button          _button;

    EquipmentData          _data;
    Action<EquipmentData>  _onSelect;

    public void Setup(EquipmentData data, Action<EquipmentData> onSelect, bool iconOnly = false)
    {
        _data     = data;
        _onSelect = onSelect;

        if (_gradeBar != null) _gradeBar.color = GradeStyle.GetColor(data.Grade);

        if (_icon != null)
        {
            _icon.sprite = data.Icon;
            _icon.color  = data.Icon != null ? Color.white : GradeStyle.GetColor(data.Grade);
        }

        if (_nameText  != null) _nameText.gameObject.SetActive(!iconOnly);
        if (_gradeText != null) _gradeText.gameObject.SetActive(!iconOnly);
        if (_statText  != null) _statText.gameObject.SetActive(!iconOnly);

        if (!iconOnly)
        {
            if (_nameText  != null) _nameText.text  = data.EquipmentName;
            if (_gradeText != null)
            {
                _gradeText.text  = GradeStyle.GetLabel(data.Grade);
                _gradeText.color = GradeStyle.GetColor(data.Grade);
            }

            if (_statText != null && data.StatEntries != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var e in data.StatEntries)
                {
                    float val = data.GetStatValue(e, 0);
                    sb.Append(LocalizationManager.Instance.Get(e.Stat.ToString())).Append(" +");
                    sb.AppendLine(StatDisplayHelper.FormatStat(e.Stat, val));
                }
                _statText.text = sb.ToString().TrimEnd();
            }
        }

        _button?.onClick.RemoveAllListeners();
        _button?.onClick.AddListener(() => _onSelect?.Invoke(_data));
    }
}
