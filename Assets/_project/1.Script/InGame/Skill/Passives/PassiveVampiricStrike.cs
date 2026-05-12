using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  PassiveVampiricStrike.cs  [OnAttack]
//  흡혈 — 공격 시 가한 피해의 X% 를 즉시 체력 회복.
//
//  Inspector:
//    TriggerType = OnAttack
//    VampireRatio: 0.10 = 피해의 10% 회복
// ============================================================

[CreateAssetMenu(fileName = "Passive_VampiricStrike", menuName = "BattleGame/Passives/VampiricStrike")]
public class PassiveVampiricStrike : PassiveSkillData
{
    [Header("흡혈 설정")]
    [Range(0f, 0.5f)]
    [Tooltip("가한 피해 대비 회복 비율 (0.1 = 10%)")]
    public float VampireRatio = 0.10f;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (ctx.DamageDealt <= 0f) return;
        var em = ctx.EntityManager;
        if (!em.HasComponent<HealthComponent>(ctx.GeneralEntity)) return;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        float heal   = ctx.DamageDealt * VampireRatio;
        float maxHp  = em.GetComponentData<StatComponent>(ctx.GeneralEntity).Final[StatType.MaxHp];
        var   health = em.GetComponentData<HealthComponent>(ctx.GeneralEntity);
        health.CurrentHp = Mathf.Min(health.CurrentHp + heal, maxHp);
        em.SetComponentData(ctx.GeneralEntity, health);
    }
}
