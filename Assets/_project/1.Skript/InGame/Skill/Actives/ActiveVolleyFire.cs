using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using BattleGame.Units;

// ============================================================
//  ActiveVolleyFire.cs — 일제 사격 (궁수·법사)
//
//  제너럴과 소속 병사 전체가 현재 타겟에 즉시 일반 공격을 발동한다.
//  공격력은 각 유닛의 StatFinal.Attack × EffectValue(배율).
//  이후 AttackCooldown 도 초기화해 연속 공격이 가능하게 한다.
// ============================================================

[UnityEngine.CreateAssetMenu(
    fileName = "Active_VolleyFire",
    menuName = "BattleGame/Actives/VolleyFire")]
public class ActiveVolleyFire : ActiveSkillData
{
    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        // 제너럴 공격
        ForceAttack(em, ctx.CasterEntity, EffectValue);

        // 소속 병사 전체 공격
        var query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<SoldierComponent>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var soldiers = query.ToComponentDataArray<SoldierComponent>(Unity.Collections.Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
            if (soldiers[i].GeneralEntity == ctx.CasterEntity)
                ForceAttack(em, entities[i], EffectValue);

        entities.Dispose();
        soldiers.Dispose();
        query.Dispose();
    }

    // ── 즉시 공격 ──────────────────────────────────────────────

    static void ForceAttack(EntityManager em, Entity attacker, float damageMult)
    {
        if (!em.HasComponent<AttackComponent>(attacker)) return;
        var atk = em.GetComponentData<AttackComponent>(attacker);

        // 타겟이 없으면 건너뜀
        if (!atk.HasTarget || !em.Exists(atk.TargetEntity)) return;
        if (!em.HasBuffer<HitEventBufferElement>(atk.TargetEntity)) return;

        var stat        = em.GetComponentData<StatComponent>(attacker);
        var selfTf      = em.GetComponentData<LocalTransform>(attacker);
        var targetTf    = em.GetComponentData<LocalTransform>(atk.TargetEntity);
        float3 dir      = math.normalizesafe(targetTf.Position - selfTf.Position);
        float  damage   = stat.Final[StatType.Attack] * damageMult;

        em.GetBuffer<HitEventBufferElement>(atk.TargetEntity).Add(new HitEventBufferElement
        {
            Damage         = damage,
            HitDirection   = float3.zero,   // 일반 공격 수준 넉백 없음
            AttackerEntity = attacker,
        });

        // 쿨다운 리셋 — 스킬 직후 바로 또 공격 가능
        atk.AttackCooldown = 0f;
        em.SetComponentData(attacker, atk);
    }
}
