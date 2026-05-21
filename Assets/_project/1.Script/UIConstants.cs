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

// ── 스탯 항목별 색상 ─────────────────────────────────────────
// MercCandidateCard / BattlePanel 슬롯 / HeroDetailPopup 공통 사용.
public static class StatColors
{
    public static readonly Color Hp      = new Color(0.35f, 1.00f, 0.45f);  // 초록
    public static readonly Color Atk     = new Color(1.00f, 0.45f, 0.35f);  // 붉은
    public static readonly Color Def     = new Color(0.40f, 0.70f, 1.00f);  // 파랑
    public static readonly Color Soldier = new Color(1.00f, 0.85f, 0.35f);  // 황금
}

// ── 스탯 보너스 출처별 색상 ───────────────────────────────────
// HeroPanelUI 합산 상세, AbilityListPopup 등에서 공통 사용.
// 출처가 달라도 이 색상이 보이면 "같은 계통의 보너스"임을 직관적으로 전달.
public static class StatBonusColors
{
    public const string Equip   = "5599FF";  // 장비  — 파랑
    public const string Passive = "55CC77";  // 패시브 — 초록
    public const string Ability = "FFAA44";  // 어빌리티 — 주황
    public const string Relic   = "CC66FF";  // 유물  — 보라

    public static Color EquipColor   => new Color(0.33f, 0.60f, 1.00f);
    public static Color PassiveColor => new Color(0.33f, 0.80f, 0.47f);
    public static Color AbilityColor => new Color(1.00f, 0.67f, 0.27f);
    public static Color RelicColor   => new Color(0.80f, 0.40f, 1.00f);
}

// ── 스탯 수치 포맷터 ─────────────────────────────────────────
// 스탯 타입에 따른 총합·델타 문자열 생성, 합산 과정 Rich Text 생성.
public static class StatDisplayHelper
{
    /// <summary>
    /// 원시 방어율(raw, 0~∞)에 소프트캡 공식 적용 후 퍼센트 값을 반환.
    /// 소프트캡 임계값·성장 계수·실효 상한은 GameplayConfig 에서 읽는다.
    /// </summary>
    public static float EffectiveDefensePct(float raw)
    {
        var   cfg      = GameplayConfig.Current;
        float softCap  = cfg.DefenseMax;
        float rate     = cfg.DefenseOverflowRate;
        float cap      = cfg.DefenseEffectiveCap;
        float effective = raw <= softCap ? raw : softCap + (raw - softCap) * rate;
        return Mathf.Min(effective, cap) * 100f;
    }

    /// <summary>스탯 총합 표시 문자열 (HUD·StatPanel 기본값 표시용)</summary>
    public static string FormatTotal(StatType stat, float value) => stat switch
    {
        StatType.Defense      => $"{EffectiveDefensePct(value):F1}%",
        StatType.AttackSpeed  => $"{value:F2}/초",
        StatType.MoveSpeed    => $"{value:F1}",
        StatType.AttackRange  => $"{value:F1}",
        StatType.SoldierCount          => $"{Mathf.RoundToInt(value)}명",
        StatType.CritChance            => $"{value * 100f:F1}%",
        StatType.CritDamage            => $"×{value:F2}",
        // 체감 공식 적용값 표시. 원시값과 동일하면 단순 표시, 다르면 "(체감 X%)" 병기
        StatType.SkillCooldownReduce   => FormatCDRTotal(value),
        _                              => $"{value:N0}",
    };

    static string FormatCDRTotal(float rawCDR)
    {
        float maxCDR      = GameplayConfig.Current?.CooldownReduceMax ?? 0.9f;
        float effectiveCDR = GeneralRuntimeBridge.CalcEffectiveCDR(rawCDR, maxCDR);
        float diff         = Mathf.Abs(rawCDR - effectiveCDR);
        if (diff < 0.001f)
            return $"{rawCDR * 100f:F1}%";
        return $"{effectiveCDR * 100f:F1}% <size=80%><color=#888888>(합산 {rawCDR * 100f:F1}%)</color></size>";
    }

    /// <summary>스탯 델타 문자열 (합산 상세 각 항목용, withSign = true 이면 +부호 포함)</summary>
    public static string FormatDelta(StatType stat, float value, bool withSign)
    {
        string sign = (withSign && value >= 0f) ? "+" : "";
        return stat switch
        {
            StatType.Defense      => $"{sign}{value * 100f:F1}%",  // 델타는 원시값 그대로 F1
            StatType.AttackSpeed  => $"{sign}{value:F2}",
            StatType.MoveSpeed    => $"{sign}{value:F1}",
            StatType.AttackRange  => $"{sign}{value:F1}",
            StatType.SoldierCount          => $"{sign}{Mathf.RoundToInt(value)}명",
            StatType.CritChance            => $"{sign}{value * 100f:F1}%",
            StatType.CritDamage            => $"{sign}{value:F2}",
            StatType.SkillCooldownReduce   => $"{sign}{value * 100f:F1}%",
            _                              => $"{sign}{value:N0}",
        };
    }

    /// <summary>
    /// 합산 과정 Rich Text 생성.
    /// "기본  +장비(파랑)  +패시브(초록)  +어빌리티(주황)  +유물(보라)"
    /// </summary>
    public static string BuildBreakdown(
        StatType stat,
        float baseVal, float equipVal, float passiveVal, float abilityVal, float relicVal = 0f)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(FormatDelta(stat, baseVal, false));
        if (equipVal   != 0f) sb.Append($"  <color=#{StatBonusColors.Equip}>{FormatDelta(stat, equipVal,   true)}</color>");
        if (passiveVal != 0f) sb.Append($"  <color=#{StatBonusColors.Passive}>{FormatDelta(stat, passiveVal, true)}</color>");
        if (abilityVal != 0f) sb.Append($"  <color=#{StatBonusColors.Ability}>{FormatDelta(stat, abilityVal, true)}</color>");
        if (relicVal   != 0f) sb.Append($"  <color=#{StatBonusColors.Relic}>{FormatDelta(stat, relicVal,   true)}</color>");

        // CDR·방어율은 체감 공식이 적용되므로 합산 후 실효값을 별도 표시
        if (stat == StatType.SkillCooldownReduce)
        {
            float rawTotal     = baseVal + equipVal + passiveVal + abilityVal + relicVal;
            float maxCDR       = GameplayConfig.Current?.CooldownReduceMax ?? 0.9f;
            float effectiveCDR = GeneralRuntimeBridge.CalcEffectiveCDR(rawTotal, maxCDR);
            if (Mathf.Abs(rawTotal - effectiveCDR) > 0.001f)
                sb.Append($"\n<color=#AAAAAA>→ 체감 {effectiveCDR * 100f:F1}%</color>");
        }
        else if (stat == StatType.Defense)
        {
            float rawTotal  = baseVal + equipVal + passiveVal + abilityVal + relicVal;
            float effective = StatDisplayHelper.EffectiveDefensePct(rawTotal);
            float rawPct    = rawTotal * 100f;
            if (Mathf.Abs(rawPct - effective) > 0.1f)
                sb.Append($"\n<color=#AAAAAA>→ 체감 {effective:F1}%</color>");
        }

        return sb.ToString();
    }
}
