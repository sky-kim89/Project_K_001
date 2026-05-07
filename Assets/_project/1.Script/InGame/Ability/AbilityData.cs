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
}
