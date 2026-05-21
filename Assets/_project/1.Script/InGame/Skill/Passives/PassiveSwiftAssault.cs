using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  PassiveSwiftAssault.cs  [OnBattleStart]
//  속전속결 — 이동속도 증가분(Final - Base)만큼 공격속도도 동일하게 증가.
//
//  Inspector:
//    TriggerType = OnBattleStart
//    Ratio       : 이동속도 증가분 대비 공격속도 전환 비율 (1.0 = 100%)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Passive_SwiftAssault", menuName = "BattleGame/Passives/SwiftAssault")]
public class PassiveSwiftAssault : PassiveSkillData
{
    [UnityEngine.Header("속전속결 설정")]
    [UnityEngine.Tooltip("이동속도 증가분 대비 공격속도 전환 비율 (1.0 = 100% 동일 적용)")]
    [UnityEngine.Range(0f, 2f)]
    public float Ratio = 1.0f;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        var stat = em.GetComponentData<StatComponent>(ctx.GeneralEntity);

        float moveDelta = stat.Final[StatType.MoveSpeed] - stat.Base[StatType.MoveSpeed];
        if (moveDelta <= 0f) return;

        float atkSpdBonus = moveDelta * Ratio;
        stat.Base[StatType.AttackSpeed]  += atkSpdBonus;
        stat.Final[StatType.AttackSpeed] += atkSpdBonus;
        em.SetComponentData(ctx.GeneralEntity, stat);
    }
}
