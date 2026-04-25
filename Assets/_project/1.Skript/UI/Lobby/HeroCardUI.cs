using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroCardUI.cs
//  영웅 목록에서 장군 1명을 표시하는 카드 컴포넌트.
// ============================================================

public class HeroCardUI : MonoBehaviour
{
    [SerializeField] Image           _gradeBorder;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] TextMeshProUGUI _gradeText;
    [SerializeField] Button          _button;

    public UnitEntry Entry { get; private set; }

    Action<UnitEntry> _onSelect;

    public void Setup(UnitEntry entry, Action<UnitEntry> onSelect)
    {
        Entry     = entry;
        _onSelect = onSelect;

        _nameText.text  = entry.UnitName;
        _levelText.text = $"Lv.{entry.Level}";
        _gradeText.text = GradeStyle.GetLabel(entry.Grade);

        Color gc = GradeStyle.GetColor(entry.Grade);
        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeText   != null) _gradeText.color   = gc;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => _onSelect?.Invoke(Entry));
    }

    public void SetSelected(bool selected)
    {
        if (_gradeBorder == null) return;
        _gradeBorder.color = selected ? Color.white : GradeStyle.GetColor(Entry?.Grade ?? UnitGrade.Normal);
    }
}
