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

    // ── 스탯 품질 ─────────────────────────────────────────────
    //  같은 등급이라도 굴림이 좋았는지 나빴는지가 갈린다.
    //  내부 계산은 0~1 이고, 표시는 10배 해서 **버림**한 정수다 (0.93 → 9).

    /// <summary>표시용 품질 점수 (0~10, 버림).</summary>
    public static int QualityScore(string unitName)
        => Mathf.FloorToInt(UnitJobRoller.GetQuality(unitName) * 10f);

    /// <summary>"영웅 9" — 등급 라벨 + 품질 점수.</summary>
    public static string GetLabelWithQuality(UnitGrade grade, string unitName)
        => $"{GetLabel(grade)} {QualityScore(unitName)}";

    /// <summary>품질 색 — 낮으면 회색, 높을수록 밝은 금색.</summary>
    public static Color QualityColor(float quality) => quality switch
    {
        >= 0.90f => new Color(1.00f, 0.60f, 0.10f),   // 최상 (희귀 스킬 주인 구간)
        >= 0.75f => new Color(0.70f, 0.30f, 1.00f),
        >= 0.55f => new Color(0.20f, 0.50f, 1.00f),
        >= 0.35f => new Color(0.25f, 0.80f, 0.35f),
        _        => new Color(0.60f, 0.62f, 0.68f),
    };
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
    public const string Trait   = "FF6688";  // 특성  — 분홍

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

    /// <summary>
    /// 스탯 값 문자열.
    ///   isFinal=false (기본) : 원시 증감값 — withSign=true 이면 + 부호 포함 (장비·특성 보너스 표시용)
    ///   isFinal=true         : 최종 합산값 — 소프트캡·CDR 체감 공식 적용 (HUD·StatPanel 표시용)
    /// </summary>
    public static string FormatStat(StatType stat, float value, bool withSign = false, bool isFinal = false)
    {
        string sign = (withSign && value >= 0f) ? "+" : "";
        return stat switch
        {
            StatType.Defense => isFinal
                ? $"{EffectiveDefensePct(value):F1}%"
                : $"{sign}{value * 100f:F1}%",
            StatType.AttackSpeed => isFinal
                ? $"{value:F2}/초"
                : $"{sign}{value:F2}",
            StatType.MoveSpeed    => $"{sign}{value:F1}",
            StatType.AttackRange  => $"{sign}{value:F1}",
            StatType.SoldierCount => $"{sign}{Mathf.RoundToInt(value)}명",
            StatType.CritChance   => $"{sign}{value * 100f:F1}%",
            StatType.CritDamage   => isFinal ? $"×{value:F2}" : $"{sign}{value:F2}×",
            StatType.SkillCooldownReduce => isFinal
                ? FormatCDRFinal(value)
                : $"{sign}{value * 100f:F1}%",
            _ => $"{sign}{value:N0}",
        };
    }

    // 넘겨받는 값은 이미 곱연산으로 합쳐진 최종 쿨감이다 —
    // 여기서는 상한에 걸렸을 때만 그 사실을 알려 준다.
    // (예전에는 "3.3% (합산 10.0%)" 처럼 체감 공식 결과를 같이 적었는데,
    //  이제 출처가 하나면 액면가가 그대로 나오므로 두 수치가 갈릴 일이 없다)
    static string FormatCDRFinal(float cdr)
    {
        float maxCDR = GameplayConfig.Current != null ? GameplayConfig.Current.CooldownReduceMax : 0.8f;
        if (cdr <= maxCDR + 0.001f)
            return $"{cdr * 100f:F1}%";
        return $"{maxCDR * 100f:F1}% <size=80%><color=#888888>(상한)</color></size>";
    }

    /// <summary>
    /// 합산 과정 Rich Text 생성.
    /// "기본  +장비(파랑)  +패시브(초록)  +어빌리티(주황)  +유물(보라)"
    /// </summary>
    public static string BuildBreakdown(
        StatType stat,
        float baseVal, float equipVal, float passiveVal, float abilityVal, float relicVal = 0f, float traitVal = 0f)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(FormatStat(stat, baseVal));
        if (equipVal   != 0f) sb.Append($"  <color=#{StatBonusColors.Equip}>{FormatStat(stat, equipVal,   withSign: true)}</color>");
        if (passiveVal != 0f) sb.Append($"  <color=#{StatBonusColors.Passive}>{FormatStat(stat, passiveVal, withSign: true)}</color>");
        if (abilityVal != 0f) sb.Append($"  <color=#{StatBonusColors.Ability}>{FormatStat(stat, abilityVal, withSign: true)}</color>");
        if (relicVal   != 0f) sb.Append($"  <color=#{StatBonusColors.Relic}>{FormatStat(stat, relicVal,   withSign: true)}</color>");
        if (traitVal   != 0f) sb.Append($"  <color=#{StatBonusColors.Trait}>{FormatStat(stat, traitVal,   withSign: true)}</color>");

        // 쿨감은 출처끼리 더하지 않고 곱연산으로 겹친다 — 단순 합과 다르므로 결과를 적어 준다
        if (stat == StatType.SkillCooldownReduce)
        {
            float sum      = baseVal + equipVal + passiveVal + abilityVal + relicVal + traitVal;
            float combined = HeroStatResult.CombineResidual(
                baseVal, equipVal, passiveVal, abilityVal, relicVal, traitVal);
            float maxCDR   = GameplayConfig.Current != null ? GameplayConfig.Current.CooldownReduceMax : 0.8f;
            float final    = Mathf.Min(combined, maxCDR);

            if (Mathf.Abs(sum - final) > 0.001f)
                sb.Append($"\n<color=#AAAAAA>→ 중첩 적용 {final * 100f:F1}%" +
                          (combined > maxCDR + 0.001f ? " (상한)" : "") + "</color>");
        }
        else if (stat == StatType.Defense)
        {
            float rawTotal  = baseVal + equipVal + passiveVal + abilityVal + relicVal + traitVal;
            float effective = StatDisplayHelper.EffectiveDefensePct(rawTotal);
            float rawPct    = rawTotal * 100f;
            if (Mathf.Abs(rawPct - effective) > 0.1f)
                sb.Append($"\n<color=#AAAAAA>→ 체감 {effective:F1}%</color>");
        }

        return sb.ToString();
    }
}
