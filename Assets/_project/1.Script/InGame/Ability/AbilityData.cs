using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  AbilityData.cs
//  어빌리티 1종을 정의하는 ScriptableObject.
//
//  Value1 / Value2 는 % 비율 (예: 0.08 = 8%).
//  HasStat2 = false 이면 Stat2/Value2 는 무시된다.
// ============================================================

[CreateAssetMenu(fileName = "Ability_", menuName = "ProjectK/Ability")]
public class AbilityData : ScriptableObject
{
    public AbilityId     Id;
    public string        AbilityName;
    public AbilityGrade  Grade;
    public AbilityTarget Target;

    [Tooltip("첫 번째 적용 스텟")]
    public StatType Stat1;
    [Tooltip("% 비율 (0.08 = 8%)")]
    public float    Value1;

    [Tooltip("두 번째 스텟 보유 여부")]
    public bool     HasStat2;
    public StatType Stat2;
    [Tooltip("% 비율 (0.08 = 8%)")]
    public float    Value2;

    [Tooltip("최대 중복 보유 횟수 (Normal/Advanced=3, Special/Mastery=1)")]
    public int MaxLevel = 3;

    public Sprite   Icon;

    // ── 트리거 어빌리티 지원 (Special 등급 서브클래스 전용) ───

    /// <summary>
    /// 이 어빌리티의 트리거 종류. None 이면 트리거 없음 (스폰 시 즉시 적용).
    /// CombatTriggerSystem 이 이 값을 보고 디스패치 여부를 결정한다.
    /// </summary>
    public virtual PassiveTrigger GetTriggerType() => PassiveTrigger.None;

    /// <summary>
    /// UI에 표시할 효과 설명. Special 서브클래스가 오버라이드한다.
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// TriggerType 에 해당하는 이벤트 발생 시 호출.
    /// 기본 구현은 아무것도 하지 않음. 서브클래스(Special 등급)에서 오버라이드.
    /// </summary>
    public virtual void OnTrigger(PassiveTriggerContext ctx) { }

    /// <summary>
    /// 로비 스탯 화면(HeroDetailPopup·AbilityListPopup)에 미리 보여 줄 스탯 기여분.
    ///
    /// ■ 왜 필요한가
    ///   Special 등급은 효과를 Stat1/Value1 이 아니라 OnTrigger 코드로 들고 있다.
    ///   스탯 화면은 Stat1/Value1 만 읽으므로 Special 은 통째로 빠져 있었고,
    ///   "시간 왜곡: 스킬 쿨타임 -35%" 를 들고도 쿨타임 줄에 아무 변화가 없었다.
    ///   여기서 스스로 신고하게 해서 그 구멍을 막는다.
    ///
    /// ⚠ **조건 없이 항상 걸리는 효과만** 신고할 것
    ///   "전투 시작 시" 는 반드시 발동하므로 플레이어에겐 상시 효과다 — 신고 대상.
    ///   "처치 시" · "피격 시" 처럼 걸릴 수도 안 걸릴 수도 있는 것은 신고하지 말 것.
    ///   안 걸릴 수 있는 수치가 총합에 섞이면 화면의 숫자를 믿을 수 없게 된다.
    ///
    /// ratios : 스탯 → 비율(0.35 = 35%). 감소 효과는 음수로 넣는다.
    ///          절대값 스탯(SkillCooldownReduce 등)은 비율이 아니라 그대로 더해진다
    ///          — AbilityApplier.IsAbsoluteStat 규칙을 따른다.
    /// </summary>
    public virtual void CollectPreviewStats(Dictionary<StatType, float> ratios) { }
}
