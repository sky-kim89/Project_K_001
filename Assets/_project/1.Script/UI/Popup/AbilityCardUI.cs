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

    public void Setup(AbilityData data, Action<AbilityData> onClicked)
    {
        Color gradeColor = GetGradeColor(data.Grade);

        if (_gradeBar  != null) _gradeBar.color  = gradeColor;
        if (_icon      != null) _icon.sprite      = data.Icon;

        if (_gradeTmp != null)
        {
            _gradeTmp.text  = GetGradeLabel(data.Grade);
            _gradeTmp.color = gradeColor;
        }

        if (_nameTmp   != null) _nameTmp.text   = data.AbilityName;
        if (_targetTmp != null)
            _targetTmp.text = data.Grade == AbilityGrade.Special
                ? $"발동: {GetTriggerLabel(data.GetTriggerType())}"
                : GetTargetLabel(data.Target);
        if (_descTmp   != null) _descTmp.text   = BuildDesc(data);

        if (_selectBtn != null)
        {
            _selectBtn.onClick.RemoveAllListeners();
            var captured = data;
            _selectBtn.onClick.AddListener(() => onClicked?.Invoke(captured));
        }
    }

    // ── 레이블 헬퍼 ───────────────────────────────────────────

    static string GetGradeLabel(AbilityGrade grade) => grade switch
    {
        AbilityGrade.Normal   => "일반",
        AbilityGrade.Advanced => "고급",
        AbilityGrade.Special  => "특수",
        _                     => "?"
    };

    static Color GetGradeColor(AbilityGrade grade) => grade switch
    {
        AbilityGrade.Normal   => new Color(0.70f, 0.70f, 0.75f),
        AbilityGrade.Advanced => new Color(0.40f, 0.72f, 1.00f),
        AbilityGrade.Special  => new Color(1.00f, 0.80f, 0.20f),
        _                     => Color.white
    };

    static string GetTargetLabel(AbilityTarget target) => target switch
    {
        AbilityTarget.All              => "전체",
        AbilityTarget.Job_Knight       => "기사",
        AbilityTarget.Job_Archer       => "궁수",
        AbilityTarget.Job_Mage         => "마법사",
        AbilityTarget.Job_ShieldBearer => "방패병",
        AbilityTarget.Range_Melee      => "근거리",
        AbilityTarget.Range_Ranged     => "원거리",
        AbilityTarget.Unit_General     => "장군",
        AbilityTarget.Unit_Soldier     => "병사",
        _                              => "?"
    };

    static string BuildDesc(AbilityData data)
    {
        if (data.Grade == AbilityGrade.Special)
            return data.Description;

        string d = $"{StatLabel(data.Stat1)} +{data.Value1 * 100f:0}%";
        if (data.HasStat2) d += $"\n{StatLabel(data.Stat2)} +{data.Value2 * 100f:0}%";
        return d;
    }

    static string GetTriggerLabel(PassiveTrigger t) => t switch
    {
        PassiveTrigger.OnAttack       => "공격 시",
        PassiveTrigger.OnHit          => "피격 시",
        PassiveTrigger.OnEnemyKill    => "처치 시",
        PassiveTrigger.OnSoldierDeath => "병사 사망 시",
        PassiveTrigger.OnSkillUse     => "스킬 사용 시",
        _                             => "즉시"
    };

    static string StatLabel(StatType type) => type switch
    {
        StatType.MaxHp               => "체력",
        StatType.Attack              => "공격력",
        StatType.AttackSpeed         => "공격속도",
        StatType.MoveSpeed           => "이동속도",
        StatType.Defense             => "방어력",
        StatType.AttackRange         => "사거리",
        StatType.CritChance          => "치명타",
        StatType.SkillCooldownReduce => "스킬쿨감",
        _                            => type.ToString()
    };
}
