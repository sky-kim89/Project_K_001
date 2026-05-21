using UnityEngine;
using BattleGame.Units;

// ============================================================
//  AbilityUIHelper.cs
//  AbilityCardUI · AbilityListPopup 공용 레이블·색상 헬퍼.
// ============================================================

public static class AbilityUIHelper
{
    public static string GradeLabel(AbilityGrade g) => g switch
    {
        AbilityGrade.Normal   => "일반",
        AbilityGrade.Advanced => "고급",
        AbilityGrade.Special  => "특수",
        AbilityGrade.Mastery  => "달인",
        _                     => "?"
    };

    public static Color GradeColor(AbilityGrade g) => g switch
    {
        AbilityGrade.Normal   => new Color(0.70f, 0.70f, 0.75f),
        AbilityGrade.Advanced => new Color(0.40f, 0.72f, 1.00f),
        AbilityGrade.Special  => new Color(1.00f, 0.80f, 0.20f),
        AbilityGrade.Mastery  => new Color(0.60f, 1.00f, 0.60f),
        _                     => Color.white
    };

    public static string TargetLabel(AbilityTarget t) => t switch
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

    public static string TriggerLabel(PassiveTrigger t) => t switch
    {
        PassiveTrigger.OnAttack       => "공격 시",
        PassiveTrigger.OnHit          => "피격 시",
        PassiveTrigger.OnEnemyKill    => "처치 시",
        PassiveTrigger.OnSoldierDeath => "병사 사망 시",
        PassiveTrigger.OnSkillUse     => "스킬 사용 시",
        _                             => "즉시"
    };

    public static string StatLabel(StatType t) => t switch
    {
        StatType.MaxHp               => "최대 체력",
        StatType.Attack              => "공격력",
        StatType.AttackSpeed         => "공격속도",
        StatType.MoveSpeed           => "이동속도",
        StatType.Defense             => "방어율",
        StatType.AttackRange         => "사거리",
        StatType.CritChance          => "치명타",
        StatType.SkillCooldownReduce => "스킬쿨감",
        StatType.SoldierCount        => "병사 수",
        StatType.CommandPower        => "지휘력",
        _                            => t.ToString()
    };

    /// 스탯 값을 절대값/비율로 포맷 (예: +1 / +8%)
    public static string FormatStatValue(StatType stat, float value) =>
        AbilityApplier.IsAbsoluteStat(stat)
            ? $"+{value:0}"
            : $"+{value * 100f:0.#}%";
}
