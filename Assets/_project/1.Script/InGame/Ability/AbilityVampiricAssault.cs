using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  AbilityVampiricAssault.cs  [Special / OnAttack]
//  C01 흡혈 강습 — 공격 시 가한 피해의 VampireRatio% 즉시 체력 회복.
// ============================================================

[CreateAssetMenu(fileName = "Ability_C01_VampiricAssault", menuName = "ProjectK/Ability/Special/VampiricAssault")]
public class AbilityVampiricAssault : AbilityData
{
    [Header("흡혈 강습 설정")]
    [Range(0f, 0.5f)]
    public float VampireRatio = 0.15f;

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnAttack;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (ctx.DamageDealt <= 0f) return;
        var em = ctx.EntityManager;
        if (!em.HasComponent<HealthComponent>(ctx.GeneralEntity)) return;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        float maxHp  = em.GetComponentData<StatComponent>(ctx.GeneralEntity).Final[StatType.MaxHp];
        float heal   = ctx.DamageDealt * VampireRatio;
        var   health = em.GetComponentData<HealthComponent>(ctx.GeneralEntity);
        health.CurrentHp = Mathf.Min(health.CurrentHp + heal, maxHp);
        em.SetComponentData(ctx.GeneralEntity, health);
    }
}
