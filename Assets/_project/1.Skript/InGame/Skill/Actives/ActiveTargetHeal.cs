using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using BattleGame.Units;

// ============================================================
//  ActiveTargetHeal.cs — 집중 치유 (공통)
//
//  소속 유닛(시전자 + 병사) 중 현재 체력 비율이 가장 낮은 유닛 하나를 집중 치유한다.
//  회복량 = 대상 MaxHp × EffectValue (비율).
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_TargetHeal", menuName = "BattleGame/Actives/TargetHeal")]
public class ActiveTargetHeal : ActiveSkillData
{
    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        // ── 가장 체력 비율이 낮은 유닛 탐색 ──────────────────────
        Entity lowestEntity   = ctx.CasterEntity;
        float  lowestHpRatio  = GetHpRatio(em, ctx.CasterEntity);

        var query    = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<SoldierComponent>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });
        NativeArray<Entity>           entities = query.ToEntityArray(Allocator.Temp);
        NativeArray<SoldierComponent> soldiers = query.ToComponentDataArray<SoldierComponent>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (soldiers[i].GeneralEntity != ctx.CasterEntity) continue;

            float ratio = GetHpRatio(em, entities[i]);
            if (ratio < lowestHpRatio)
            {
                lowestHpRatio  = ratio;
                lowestEntity   = entities[i];
            }
        }

        entities.Dispose();
        soldiers.Dispose();
        query.Dispose();

        // ── 집중 치유 ──────────────────────────────────────────
        if (!em.HasComponent<HealthComponent>(lowestEntity)) return;
        if (!em.HasComponent<StatComponent>(lowestEntity))   return;

        var health = em.GetComponentData<HealthComponent>(lowestEntity);
        var stat   = em.GetComponentData<StatComponent>(lowestEntity);

        float maxHp  = stat.Final[StatType.MaxHp];
        float amount = maxHp * EffectValue;

        health.CurrentHp = UnityEngine.Mathf.Min(health.CurrentHp + amount, maxHp);
        em.SetComponentData(lowestEntity, health);
    }

    // ── 내부 ─────────────────────────────────────────────────

    static float GetHpRatio(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<HealthComponent>(entity)) return 1f;
        if (!em.HasComponent<StatComponent>(entity))   return 1f;

        float maxHp = em.GetComponentData<StatComponent>(entity).Final[StatType.MaxHp];
        if (maxHp <= 0f) return 1f;

        return em.GetComponentData<HealthComponent>(entity).CurrentHp / maxHp;
    }
}
