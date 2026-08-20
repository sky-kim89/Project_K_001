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
    /// <summary>
    /// 어빌리티 합산 표기.
    ///
    /// ⚠ 부호를 붙박이로 "+" 로 쓰지 않는다
    ///   어빌리티에도 페널티가 있다 (시간 왜곡: 공격력 -10%).
    ///   무조건 "+" 를 붙이면 "+-10%" 가 찍힌다.
    /// </summary>
    public static string FormatStatValue(StatType stat, float value)
    {
        if (stat == StatType.SoldierCount || stat == StatType.CommandPower)
            return Count(value);

        return Percent(value);
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
            // ⚠ 스탯 화면(StatDisplayHelper.FormatStat)과 단위가 같아야 한다
            //   거긴 % 인데 여기만 "배" 로 두면, 어빌리티가 "+0.3배" 라고 말한 뒤
            //   장수 스탯에는 "+30.0%" 로 붙어 같은 값인지 알 수 없다.
            StatType.CritDamage          => Percent(value),
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

    /// 전환 특성의 "재료 1단위" 표기. 0~1 비율 스탯은 %p 로, 나머지는 실수 그대로.
    static string FormatUnit(StatType stat, float perUnit) => stat switch
    {
        StatType.Defense             => $"{perUnit * 100f:0.#}%p",
        StatType.CritChance          => $"{perUnit * 100f:0.#}%p",
        StatType.SkillCooldownReduce => $"{perUnit * 100f:0.#}%p",
        _                            => $"{perUnit:0.#}",
    };

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

    /// <summary>
    /// 특성 SO 전체(고정 효과 + 누적 스택)를 스탯 텍스트로 변환.
    ///
    /// 스택 누적 특성(분노 축적·전우의 분노·대기만성)은 고정 효과가 비어 있어서
    /// Effects 만 읽으면 툴팁 스탯 줄이 통째로 사라진다 — 지금 얼마나 쌓였는지
    /// 확인할 방법이 없다. 그래서 누적치를 앞에 세우고 계산식을 괄호로 덧붙인다.
    ///
    /// showAccumulated=false : 아직 보유하지 않은 특성(상점 매물·보상 카드).
    ///                        남의 스택을 보여줄 이유가 없으므로 스택당 수치만 쓴다.
    /// </summary>
    public static string BuildStatText(TraitData data, bool showAccumulated = true)
    {
        if (data == null) return "";

        var sb = new StringBuilder(BuildStatText(data.Effects));

        // 전환 특성은 Effects 가 비어 있다 — 이 줄이 없으면 효과를 볼 방법이 없다.
        if (data.Conversions != null)
        {
            foreach (var c in data.Conversions)
            {
                if (c.PerUnit <= 0f) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append($"{LocalizationManager.Instance.Get(c.From.ToString())} {FormatUnit(c.From, c.PerUnit)}" +
                          $" → {LocalizationManager.Instance.Get(c.To.ToString())} {Percent(c.Rate)}");
            }
        }

        if (data.StackStatBonuses == null || data.StackStatBonuses.Length == 0)
            return sb.ToString();

        int stacks = showAccumulated ? TraitApplier.GetMaxStack(data.TraitType) : 0;
        string cap = data.MaxStacks > 0 ? $"/{data.MaxStacks}" : "";

        foreach (var e in data.StackStatBonuses)
        {
            if (sb.Length > 0) sb.Append('\n');

            string name = LocalizationManager.Instance.Get(e.Stat.ToString());
            string per  = FormatStatValue(e.Stat, e.Value, e.IsPercent);

            if (stacks <= 0)
            {
                sb.Append($"{name} 스택당 {per}");
                continue;
            }

            string total = FormatStatValue(e.Stat, e.Value * stacks, e.IsPercent);
            sb.Append($"{name} {total}  ({per} × {stacks}{cap}스택)");
        }
        return sb.ToString();
    }
}
