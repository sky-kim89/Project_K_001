using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  PassiveShieldEdge.cs  [OnBattleStart]
//  방패의 날 — 방어율 DefenseStep당 공격력 AttackPerStep% 증가.
//
//  Inspector:
//    TriggerType    = OnBattleStart
//    DefenseStep    : 보너스 1단계당 필요 방어율 (0.10 = 10%)
//    AttackPerStep  : 단계당 공격력 증가율 (0.04 = 4%)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Passive_ShieldEdge", menuName = "BattleGame/Passives/ShieldEdge")]
public class PassiveShieldEdge : PassiveSkillData
{
    [Header("방패의 날 설정")]
    [Tooltip("보너스 1단계당 필요 방어율 (0.10 = 10%)")]
    [Range(0.01f, 0.5f)]
    public float DefenseStep = 0.10f;

    [Tooltip("단계당 공격력 증가율 (0.04 = 4%)")]
    [Range(0f, 0.2f)]
    public float AttackPerStep = 0.04f;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        var stat = em.GetComponentData<StatComponent>(ctx.GeneralEntity);

        float defense = stat.Final[StatType.Defense];
        int   steps   = Mathf.FloorToInt(defense / DefenseStep);
        if (steps <= 0) return;

        float atkBonus = stat.Base[StatType.Attack] * AttackPerStep * steps;
        stat.Base[StatType.Attack]  += atkBonus;
        stat.Final[StatType.Attack] += atkBonus;
        em.SetComponentData(ctx.GeneralEntity, stat);
    }
}
