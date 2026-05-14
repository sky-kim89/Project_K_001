using UnityEngine;

// ============================================================
//  RelicData.cs
//  유물 1종을 정의하는 ScriptableObject.
//
//  EffectType == Stat:
//    Target, Stat1, Value1PerLevel (+ HasStat2, Stat2, Value2PerLevel) 를 사용.
//    IsAbsoluteValue = false 이면 % 곱연산 (기본값).
//    IsAbsoluteValue = true  이면 절대값 가산
//      → SoldierCount(+1/레벨), SkillCooldownReduce(+0.03/레벨) 등에 사용.
//
//  EffectType == System:
//    SystemEffect, SystemValuePerLevel 을 사용.
//    IsAbsoluteValue = true  이면 정수 취급 (새로고침 횟수, 선택지 수).
//    IsAbsoluteValue = false 이면 비율 (0.15 = 15%).
// ============================================================

[CreateAssetMenu(fileName = "Relic_", menuName = "ProjectK/Relic")]
public class RelicData : ScriptableObject
{
    [Header("기본 정보")]
    public RelicId     Id;
    public string      RelicName;
    public RelicRarity Rarity;
    public Sprite      Icon;
    [Range(1, 10)]
    public int         MaxLevel = 5;

    [Header("효과 유형")]
    public RelicEffectType EffectType;

    [Header("스텟 효과 (EffectType == Stat)")]
    public AbilityTarget Target = AbilityTarget.All;

    [Tooltip("첫 번째 적용 스텟")]
    public StatType Stat1;
    [Tooltip("IsAbsoluteValue=false 이면 % 비율 (0.05 = 5%/레벨)\nIsAbsoluteValue=true  이면 절대값 (1 = +1/레벨)")]
    public float    Value1PerLevel;

    public bool     HasStat2;
    public StatType Stat2;
    public float    Value2PerLevel;

    [Header("시스템 효과 (EffectType == System)")]
    public RelicSystemEffect SystemEffect;
    [Tooltip("IsAbsoluteValue=false 이면 비율 (0.15 = 15%)\nIsAbsoluteValue=true  이면 정수 취급 (1 = +1회/개)")]
    public float SystemValuePerLevel;

    [Header("공통")]
    [Tooltip("true 이면 값을 % 배율이 아닌 절대값으로 적용 (SoldierCount, SkillCooldownReduce 등)")]
    public bool IsAbsoluteValue;

    // ── 카테고리 ──────────────────────────────────────────────

    public RelicCategory GetCategory()
    {
        if (EffectType == RelicEffectType.Stat) return RelicCategory.Stat;
        return SystemEffect switch
        {
            RelicSystemEffect.AbilityRefreshCount   => RelicCategory.Ability,
            RelicSystemEffect.AbilityChoiceCount     => RelicCategory.Ability,
            RelicSystemEffect.AbilityAdvancedChance  => RelicCategory.Ability,
            _                                        => RelicCategory.Currency,
        };
    }

    // ── 설명 자동 생성 ────────────────────────────────────────

    /// <summary>현재 레벨 기준 효과 설명 문자열.</summary>
    public string GetDescription(int level)
    {
        if (EffectType == RelicEffectType.System)
            return BuildSystemDesc(level);

        string s = BuildStatLine(Stat1, Value1PerLevel, level);
        if (HasStat2) s += $"\n{BuildStatLine(Stat2, Value2PerLevel, level)}";
        return s;
    }

    string BuildStatLine(StatType stat, float valuePerLevel, int level)
    {
        float total = valuePerLevel * level;
        string label = StatLabel(stat);
        if (IsAbsoluteValue)
        {
            if (stat == StatType.SoldierCount)
                return $"{label} +{Mathf.RoundToInt(total)}명";
            if (stat == StatType.SkillCooldownReduce || stat == StatType.Defense || stat == StatType.CritChance)
                return $"{label} +{total * 100f:0}%p";
            return $"{label} +{total:0.#}";
        }
        return $"{label} +{total * 100f:0}%";
    }

    string BuildSystemDesc(int level)
    {
        float v = SystemValuePerLevel * level;
        return SystemEffect switch
        {
            RelicSystemEffect.AbilityRefreshCount   => $"어빌리티 새로고침 +{Mathf.RoundToInt(v)}회",
            RelicSystemEffect.AbilityChoiceCount    => $"어빌리티 선택지 +{Mathf.RoundToInt(v)}개",
            RelicSystemEffect.AbilityAdvancedChance => $"고급 이상 어빌리티 확률 +{v * 100f:0}%p",
            RelicSystemEffect.GoldGainBonus         => $"골드 획득량 +{v * 100f:0}%",
            RelicSystemEffect.SoldierSoulGainBonus  => $"병사 소울 획득량 +{v * 100f:0}%",
            RelicSystemEffect.ExpGainBonus          => $"경험치 획득량 +{v * 100f:0}%",
            RelicSystemEffect.EnemyMaxHpReduction   => $"적 최대 체력 -{v * 100f:0}%",
            RelicSystemEffect.EnemyAttackReduction  => $"적 공격력 -{v * 100f:0}%",
            _ => string.Empty,
        };
    }

    static string StatLabel(StatType s) => s switch
    {
        StatType.MaxHp               => "최대 체력",
        StatType.Attack              => "공격력",
        StatType.Defense             => "방어율",
        StatType.MoveSpeed           => "이동속도",
        StatType.AttackSpeed         => "공격속도",
        StatType.AttackRange         => "공격 사거리",
        StatType.CritChance          => "치명타 확률",
        StatType.CritDamage          => "치명타 데미지",
        StatType.SoldierCount        => "기본 병사 수",
        StatType.CommandPower        => "지휘력",
        StatType.SkillCooldownReduce => "스킬 쿨감",
        _ => s.ToString(),
    };
}
