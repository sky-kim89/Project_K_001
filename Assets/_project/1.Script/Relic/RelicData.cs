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

    [Header("가격")]
    [Tooltip("강화 비용 가중치. 1 = 희귀도 기본가.\n같은 희귀도인데 효과가 유독 세거나 약할 때만 건드린다 (1.5 = 1.5배).")]
    public float CostWeight = 1f;

    // ── 가격 ──────────────────────────────────────────────────

    /// <summary>
    /// 현재 레벨 → 다음 레벨 강화 비용 (pt).
    /// ⚠ 유물 가격을 묻는 곳은 전부 여기를 거친다 — 희귀도·가중치를 빠뜨릴 수 없게.
    /// </summary>
    public int LevelUpCost(int currentLevel)
        => ReincarnationData.LevelUpCost(currentLevel, Rarity, CostWeight);

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

        string target = GetTargetLabel();
        string s = BuildStatLine(Stat1, Value1PerLevel, level);
        if (HasStat2) s += $"\n{BuildStatLine(Stat2, Value2PerLevel, level)}";
        return $"[{target}]\n{s}";
    }

    public string GetTargetLabel()
        => LocalizationManager.Instance.Get(Target.ToString());

    /// <summary>
    /// 유물 스킬 줄 한 줄.
    ///
    /// ⚠ 색은 여기서 입힌다 — 부르는 쪽이 감싸면 빠뜨린다
    ///   유물 설명은 팝업·툴팁·도감·보상 카드 네 곳에서 불린다.
    ///   부르는 쪽마다 감싸게 하면 한 곳은 반드시 빠진다.
    /// </summary>
    string BuildStatLine(StatType stat, float valuePerLevel, int level)
    {
        float total = valuePerLevel * level;
        string label = LocalizationManager.Instance.Get(stat.ToString());
        string body;
        if (IsAbsoluteValue)
        {
            if (stat == StatType.SoldierCount)
                body = $"{label} +{Mathf.RoundToInt(total)}명";
            else if (stat == StatType.SkillCooldownReduce || stat == StatType.Defense || stat == StatType.CritChance)
                body = $"{label} +{total * 100f:0}%p";
            else
                body = $"{label} +{total:0.#}";
        }
        else
        {
            body = $"{label} +{total * 100f:0}%";
        }
        return StatBonusColors.Wrap(StatSource.Relic, body);
    }

    string BuildSystemDesc(int level)
    {
        float v = SystemValuePerLevel * level;
        // 스킬이 아니더라도 '유물이 준 것' 은 맞다 — 같은 색을 쓴다
        return StatBonusColors.Wrap(StatSource.Relic, SystemEffect switch
        {
            RelicSystemEffect.AbilityRefreshCount   => $"어빌리티 새로고침 +{Mathf.RoundToInt(v)}회",
            RelicSystemEffect.AbilityChoiceCount    => $"어빌리티 선택지 +{Mathf.RoundToInt(v)}개",
            RelicSystemEffect.AbilityAdvancedChance => $"고급 이상 어빌리티 확률 +{v * 100f:0}%p",
            RelicSystemEffect.GoldGainBonus         => $"골드 획득량 +{v * 100f:0}%",
            RelicSystemEffect.SoldierSoulGainBonus  => $"병사 소울 획득량 +{v * 100f:0}%",
            RelicSystemEffect.ExpGainBonus          => $"경험치 획득량 +{v * 100f:0}%",
            RelicSystemEffect.EnemyMaxHpReduction   => $"적 최대 체력 -{v * 100f:0}%",
            RelicSystemEffect.EnemyAttackReduction  => $"적 공격력 -{v * 100f:0}%",
            RelicSystemEffect.GeneralSlotBonus      => $"장수 배치 슬롯 +{Mathf.RoundToInt(v)}칸",
            RelicSystemEffect.BattleSpeedUnlock      => $"전투 배속 {1 + Mathf.RoundToInt(v)}× 까지 사용",
            _ => string.Empty,
        });
    }

}
