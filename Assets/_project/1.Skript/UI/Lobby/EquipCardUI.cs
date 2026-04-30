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

    public void Setup(EquipmentData data, Action<EquipmentData> onSelect)
    {
        _data     = data;
        _onSelect = onSelect;

        if (_nameText  != null) _nameText.text   = data.EquipmentName;
        if (_gradeText != null)
        {
            _gradeText.text  = GradeStyle.GetLabel(data.Grade);
            _gradeText.color = GradeStyle.GetColor(data.Grade);
        }
        if (_gradeBar != null) _gradeBar.color = GradeStyle.GetColor(data.Grade);

        if (_icon != null)
        {
            _icon.sprite = data.Icon;
            _icon.color  = data.Icon != null ? Color.white : GradeStyle.GetColor(data.Grade);
        }

        if (_statText != null)
        {
            var sb = new System.Text.StringBuilder();
            if (data.StatEntries != null)
                foreach (var e in data.StatEntries)
                {
                    float val = data.GetStatValue(e, 0);
                    sb.Append(GetStatKorean(e.Stat)).Append(" +");
                    sb.AppendLine(e.Stat == StatType.Defense
                        ? $"{val * 100f:F1}%"
                        : $"{val:N0}");
                }
            _statText.text = sb.ToString().TrimEnd();
        }

        _button?.onClick.RemoveAllListeners();
        _button?.onClick.AddListener(() => _onSelect?.Invoke(_data));
    }

    static string GetStatKorean(StatType stat) => stat switch
    {
        StatType.MaxHp        => "체력",
        StatType.Attack       => "공격",
        StatType.Defense      => "방어율",
        StatType.MoveSpeed    => "이속",
        StatType.AttackSpeed  => "공속",
        StatType.AttackRange  => "사거리",
        StatType.SoldierCount => "용병수",
        StatType.CommandPower => "지휘력",
        _                     => stat.ToString(),
    };
}
