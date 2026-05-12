using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  PassiveQuickRecovery.cs  [OnHit]
//  긴급 회복 — 피격 시 최대체력의 X% 즉시 회복.
//
//  Inspector:
//    TriggerType = OnHit
//    HealRatio: 최대체력 대비 회복 비율 (0.05 = 5%)
// ============================================================

[CreateAssetMenu(fileName = "Passive_QuickRecovery", menuName = "BattleGame/Passives/QuickRecovery")]
public class PassiveQuickRecovery : PassiveSkillData
{
    [Header("긴급 회복 설정")]
    [Range(0f, 0.3f)]
    [Tooltip("최대체력 대비 즉시 회복 비율 (0.05 = 5%)")]
    public float HealRatio = 0.05f;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<HealthComponent>(ctx.GeneralEntity)) return;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        float maxHp  = em.GetComponentData<StatComponent>(ctx.GeneralEntity).Final[StatType.MaxHp];
        float heal   = maxHp * HealRatio;
        var   health = em.GetComponentData<HealthComponent>(ctx.GeneralEntity);
        health.CurrentHp = Mathf.Min(health.CurrentHp + heal, maxHp);
        em.SetComponentData(ctx.GeneralEntity, health);
    }
}
