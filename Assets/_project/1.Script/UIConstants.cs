using UnityEngine;

// ============================================================
//  UIConstants.cs
//  등급·직업·보너스 출처별 색상 + 스탯 포맷터를 한곳에 모은 공유 상수.
//  GradeStyle / JobStyle / StatBonusColors / StatDisplayHelper 로 접근.
// ============================================================

public static class GradeStyle
{
    // ── 색상 ──────────────────────────────────────────────────
    public static Color GetColor(UnitGrade grade) => grade switch
    {
        UnitGrade.Normal   => new Color(0.55f, 0.55f, 0.55f),
        UnitGrade.Uncommon => new Color(0.25f, 0.80f, 0.35f),
        UnitGrade.Rare     => new Color(0.20f, 0.50f, 1.00f),
        UnitGrade.Unique   => new Color(0.70f, 0.30f, 1.00f),
        UnitGrade.Epic     => new Color(1.00f, 0.60f, 0.10f),
        _                  => Color.white,
    };

    public static string GetLabel(UnitGrade grade)
        => LocalizationManager.Instance?.Get(grade.ToString()) ?? grade.ToString();
}

// ── 유물 희귀도 색상 ─────────────────────────────────────────
// RelicRarity → UnitGrade 와 동일한 색상 체계로 매핑
public static class RelicStyle
{
    public static Color GetColor(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common    => new Color(0.55f, 0.55f, 0.55f),  // 회색
        RelicRarity.Uncommon  => new Color(0.25f, 0.80f, 0.35f),  // 초록
        RelicRarity.Rare      => new Color(0.20f, 0.50f, 1.00f),  // 파랑
        RelicRarity.Epic      => new Color(0.70f, 0.30f, 1.00f),  // 보라
        RelicRarity.Legendary => new Color(1.00f, 0.60f, 0.10f),  // 주황
        _                     => Color.white,
    };

    public static string GetLabel(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common    => "일반",
        RelicRarity.Uncommon  => "언커먼",
        RelicRarity.Rare      => "희귀",
        RelicRarity.Epic      => "영웅",
        RelicRarity.Legendary => "전설",
        _                     => rarity.ToString(),
    };
}

public static class JobStyle
{
    public static string GetLabel(UnitJob job)
        => LocalizationManager.Instance?.Get(job.ToString()) ?? job.ToString();
}

// ── 스탯 보너스 출처별 색상 ───────────────────────────────────
// HeroPanelUI 합산 상세, AbilityListPopup 등에서 공통 사용.
// 출처가 달라도 이 색상이 보이면 "같은 계통의 보너스"임을 직관적으로 전달.
public static class StatBonusColors
{
    public const string Equip   = "5599FF";  // 장비  — 파랑
    public const string Passive = "55CC77";  // 패시브 — 초록
    public const string Ability = "FFAA44";  // 어빌리티 — 주황

    public static Color EquipColor   => new Color(0.33f, 0.60f, 1.00f);
    public static Color PassiveColor => new Color(0.33f, 0.80f, 0.47f);
    public static Color AbilityColor => new Color(1.00f, 0.67f, 0.27f);
}

// ── 스탯 수치 포맷터 ─────────────────────────────────────────
// 스탯 타입에 따른 총합·델타 문자열 생성, 합산 과정 Rich Text 생성.
public static class StatDisplayHelper
{
    /// <summary>스탯 총합 표시 문자열 (HUD·StatPanel 기본값 표시용)</summary>
    public static string FormatTotal(StatType stat, float value) => stat switch
    {
        StatType.Defense      => $"{value * 100f:F1}%",
        StatType.AttackSpeed  => $"{value:F2}/초",
        StatType.MoveSpeed    => $"{value:F1}",
        StatType.AttackRange  => $"{value:F1}",
        StatType.SoldierCount => $"{Mathf.RoundToInt(value)}명",
        _                     => $"{value:N0}",
    };

    /// <summary>스탯 델타 문자열 (합산 상세 각 항목용, withSign = true 이면 +부호 포함)</summary>
    public static string FormatDelta(StatType stat, float value, bool withSign)
    {
        string sign = (withSign && value >= 0f) ? "+" : "";
        return stat switch
        {
            StatType.Defense      => $"{sign}{value * 100f:F1}%",
            StatType.AttackSpeed  => $"{sign}{value:F2}",
            StatType.MoveSpeed    => $"{sign}{value:F1}",
            StatType.AttackRange  => $"{sign}{value:F1}",
            StatType.SoldierCount => $"{sign}{Mathf.RoundToInt(value)}명",
            _                     => $"{sign}{value:N0}",
        };
    }

    /// <summary>
    /// 합산 과정 Rich Text 생성.
    /// "기본  +장비(파랑)  +패시브(초록)  +어빌리티(주황)"
    /// </summary>
    public static string BuildBreakdown(
        StatType stat,
        float baseVal, float equipVal, float passiveVal, float abilityVal)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(FormatDelta(stat, baseVal, false));
        if (equipVal   != 0f) sb.Append($"  <color=#{StatBonusColors.Equip}>{FormatDelta(stat, equipVal,   true)}</color>");
        if (passiveVal != 0f) sb.Append($"  <color=#{StatBonusColors.Passive}>{FormatDelta(stat, passiveVal, true)}</color>");
        if (abilityVal != 0f) sb.Append($"  <color=#{StatBonusColors.Ability}>{FormatDelta(stat, abilityVal, true)}</color>");
        return sb.ToString();
    }
}
