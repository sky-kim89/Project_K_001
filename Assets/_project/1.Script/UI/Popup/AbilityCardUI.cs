using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilityCardUI.cs
//  AbilitySelectPopup 내 어빌리티 1장 카드 UI.
// ============================================================

public class AbilityCardUI : MonoBehaviour
{
    [SerializeField] Image            _gradeBar;
    [SerializeField] Image            _icon;
    [SerializeField] TextMeshProUGUI  _gradeTmp;
    [SerializeField] TextMeshProUGUI  _nameTmp;
    [SerializeField] TextMeshProUGUI  _targetTmp;
    [SerializeField] TextMeshProUGUI  _descTmp;
    [SerializeField] Button           _selectBtn;
    [SerializeField] TextMeshProUGUI  _levelTmp;

    /// <param name="currentLevel">현재 보유 레벨 (0=미보유). 선택 후 currentLevel+1 이 됨.</param>
    public void Setup(AbilityData data, Action<AbilityData> onClicked, int currentLevel = 0)
    {
        Color gradeColor = AbilityUIHelper.GradeColor(data.Grade);

        if (_gradeBar  != null) _gradeBar.color  = gradeColor;
        if (_icon      != null) _icon.sprite      = data.Icon;

        if (_gradeTmp != null)
        {
            _gradeTmp.text  = AbilityUIHelper.GradeLabel(data.Grade);
            _gradeTmp.color = gradeColor;
        }

        if (_nameTmp   != null) _nameTmp.text   = data.AbilityName;
        if (_targetTmp != null)
            _targetTmp.text = (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery)
                ? $"발동: {AbilityUIHelper.TriggerLabel(data.GetTriggerType())}"
                : AbilityUIHelper.TargetLabel(data.Target);
        if (_descTmp   != null) _descTmp.text   = BuildDesc(data);

        // 레벨 표시 — MaxLevel > 1 인 어빌리티만 (Normal/Advanced)
        if (_levelTmp != null)
        {
            int maxLv = data.MaxLevel;
            if (maxLv <= 1)
            {
                _levelTmp.gameObject.SetActive(false);
            }
            else
            {
                _levelTmp.gameObject.SetActive(true);
                int nextLv = currentLevel + 1;
                _levelTmp.text  = currentLevel > 0
                    ? $"Lv {currentLevel} → {nextLv} / {maxLv}"
                    : $"Lv {nextLv} / {maxLv}";
                _levelTmp.color = nextLv >= maxLv ? new Color(1f, 0.85f, 0.2f) : gradeColor;
            }
        }

        if (_selectBtn != null)
        {
            _selectBtn.onClick.RemoveAllListeners();
            var captured = data;
            _selectBtn.onClick.AddListener(() => onClicked?.Invoke(captured));
        }
    }

    static string BuildDesc(AbilityData data)
    {
        if (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery)
            return data.Description;

        string d = $"{AbilityUIHelper.StatLabel(data.Stat1)} {AbilityUIHelper.FormatStatValue(data.Stat1, data.Value1)}";
        if (data.HasStat2) d += $"\n{AbilityUIHelper.StatLabel(data.Stat2)} {AbilityUIHelper.FormatStatValue(data.Stat2, data.Value2)}";
        return d;
    }
}
