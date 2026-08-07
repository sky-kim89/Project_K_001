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
    ///
    /// 특성 설명문에서 수치를 뺐으므로(효과는 이 줄이 전부 말해준다)
    /// 감소 효과도 여기서 제대로 음수로 보여야 한다.
    public static string FormatStatValue(StatType stat, float value, bool isPercent)
    {
        // AllStatPenalty 는 "깎이는 양"을 양수로 저장한다 → 표시는 뒤집는다.
        if (stat == StatType.AllStatPenalty) return Percent(-value);

        if (isPercent) return Percent(value);

        return stat switch
        {
            StatType.Defense             => Percent(value),
            StatType.CritChance          => Percent(value),
            StatType.SkillCooldownReduce => Percent(value),
            StatType.ExpGainBonus        => Percent(value),
            StatType.SoldierCount        => Count(value),
            StatType.CommandPower        => Count(value),
            StatType.GeneralSlotBonus    => Count(value),
            StatType.EquipSlotBonus      => Count(value),
            _                            => value >= 0f ? $"+{value:0.#}" : $"{value:0.#}",
        };
    }

    // 음수면 "-" 가 값에 이미 붙으므로 "+" 만 조건부로 붙인다 ("+-15%" 방지).
    static string Percent(float v) => v >= 0f ? $"+{v * 100f:0.#}%" : $"{v * 100f:0.#}%";

    static string Count(float v)
    {
        int i = Mathf.RoundToInt(v);
        return i >= 0 ? $"+{i}" : i.ToString();
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
