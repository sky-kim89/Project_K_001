using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using BattleGame.Units;

// ============================================================
//  ActiveHealAura.cs — 치유 오라 (공통)
//
//  시전자와 소속 병사 전체의 체력을 즉시 회복한다.
//  회복량 = 각 유닛 MaxHp × EffectValue (비율).
//  예) EffectValue = 0.2 → 최대 체력의 20% 회복
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_HealAura", menuName = "BattleGame/Actives/HealAura")]
public class ActiveHealAura : ActiveSkillData
{
    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        // 시전자 치유
        HealUnit(em, ctx.CasterEntity);

        // 소속 병사 전체 치유
        var query    = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<SoldierComponent>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });
        NativeArray<Entity>          entities = query.ToEntityArray(Allocator.Temp);
        NativeArray<SoldierComponent> soldiers = query.ToComponentDataArray<SoldierComponent>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
            if (soldiers[i].GeneralEntity == ctx.CasterEntity)
                HealUnit(em, entities[i]);

        entities.Dispose();
        soldiers.Dispose();
        query.Dispose();
    }

    // ── 즉시 치유 ──────────────────────────────────────────────

    void HealUnit(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<HealthComponent>(entity)) return;
        if (!em.HasComponent<StatComponent>(entity))  return;

        var health = em.GetComponentData<HealthComponent>(entity);
        var stat   = em.GetComponentData<StatComponent>(entity);

        float maxHp  = stat.Final[StatType.MaxHp];
        float amount = maxHp * EffectValue;

        health.CurrentHp = UnityEngine.Mathf.Min(health.CurrentHp + amount, maxHp);
        em.SetComponentData(entity, health);
    }
}
