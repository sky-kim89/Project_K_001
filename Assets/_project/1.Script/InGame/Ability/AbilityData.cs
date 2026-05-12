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
}
