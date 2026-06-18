using System.Text;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  AbilityUIHelper.cs
//  AbilityCardUI · AbilityListPopup 공용 레이블·색상 헬퍼.
// ============================================================

public static class AbilityUIHelper
{
    public static Color GradeColor(AbilityGrade g) => g switch
    {
        AbilityGrade.Normal   => new Color(0.70f, 0.70f, 0.75f),
        AbilityGrade.Advanced => new Color(0.40f, 0.72f, 1.00f),
        AbilityGrade.Special  => new Color(1.00f, 0.80f, 0.20f),
        AbilityGrade.Mastery  => new Color(0.60f, 1.00f, 0.60f),
        _                     => Color.white
    };

    /// 어빌리티용 스탯 값 포맷 — 스탯 타입 기반 (IsPercent 없음).
    /// Defense·CritChance·CDR 는 0~1 비율, SoldierCount·CommandPower 는 정수, 나머지는 ×100%.
    public static string FormatStatValue(StatType stat, float value)
    {
        if (stat == StatType.SoldierCount || stat == StatType.CommandPower)
            return $"+{Mathf.RoundToInt(value)}";
        return $"+{value * 100f:0.#}%";
    }

    /// 특성·패시브용 스탯 값 포맷 — IsPercent 플래그 + 스탯 타입 동시 고려.
    /// IsPercent=true  : 기본 스탯의 N% 가산 → ×100 표시.
    /// IsPercent=false : 절대값 가산이지만 0~1 비율 스탯(방어율·치명타·쿨감)은 ×100 표시.
    public static string FormatStatValue(StatType stat, float value, bool isPercent)
    {
        if (isPercent)
            return $"+{value * 100f:0.#}%";
        return stat switch
        {
            StatType.Defense             => $"+{value * 100f:0.#}%",
            StatType.CritChance          => $"+{value * 100f:0.#}%",
            StatType.SkillCooldownReduce => $"+{value * 100f:0.#}%",
            StatType.SoldierCount        => $"+{Mathf.RoundToInt(value)}",
            StatType.CommandPower        => $"+{Mathf.RoundToInt(value)}",
            _                            => $"+{value:0.#}",
        };
    }

    /// TraitData.Effects 배열을 여러 줄 스탯 텍스트로 변환.
    public static string BuildStatText(TraitData.TraitStatEntry[] effects)
    {
        if (effects == null || effects.Length == 0) return "";
        var sb = new StringBuilder();
        foreach (var e in effects)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"{LocalizationManager.Instance.Get(e.Stat.ToString())} {FormatStatValue(e.Stat, e.Value, e.IsPercent)}");
        }
        return sb.ToString();
    }
}
