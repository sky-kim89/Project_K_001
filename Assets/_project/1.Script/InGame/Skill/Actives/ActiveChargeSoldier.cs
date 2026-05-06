using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveChargeSoldier.cs — 돌격 병사 소환 (방패)
//
//  소속 병사 중 한 명을 선택해 현재 타겟 방향으로 돌격시킨다.
//  도착 시 EffectValue 배율 피해 + 넉백.
//  ChargeSoldierRunner 가 이동 + 타격 시퀀스를 처리한다.
// ============================================================

[CreateAssetMenu(fileName = "Active_ChargeSoldier", menuName = "BattleGame/Actives/ChargeSoldier")]
public class ActiveChargeSoldier : ActiveSkillData
{
    [Header("돌격 병사 설정")]
    [Tooltip("돌격 이동 속도 (유닛/초)")]
    public float ChargeSpeed = 20f;

    [Tooltip("넉백 배율")]
    public float KnockbackMult = 5f;

    public override void Execute(ActiveSkillContext ctx)
    {
        if (!ctx.HasTarget) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        if (!em.Exists(ctx.TargetEntity)) return;

        // ── 소속 병사 중 한 명 선택 (체력 가장 높은 병사) ────────
        Entity pickedSoldier   = Entity.Null;
        float  highestHp       = -1f;
        GameObject soldierGO   = null;

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
            if (!em.HasComponent<HealthComponent>(entities[i])) continue;

            float hp = em.GetComponentData<HealthComponent>(entities[i]).CurrentHp;
            if (hp > highestHp)
            {
                highestHp     = hp;
                pickedSoldier = entities[i];
            }
        }

        entities.Dispose();
        soldiers.Dispose();
        query.Dispose();

        if (pickedSoldier == Entity.Null) return;

        // ── 병사 GO 획득 ─────────────────────────────────────
        if (em.HasComponent<UnitPoolLinkComponent>(pickedSoldier))
            soldierGO = em.GetComponentObject<UnitPoolLinkComponent>(pickedSoldier).LinkedObject;

        if (soldierGO == null) return;

        // ── 타겟 위치 ─────────────────────────────────────────
        var targetTf  = em.GetComponentData<LocalTransform>(ctx.TargetEntity);
        Vector3 targetPos = new Vector3(targetTf.Position.x, targetTf.Position.y, targetTf.Position.z);

        var soldierStat = em.GetComponentData<StatComponent>(pickedSoldier);
        var casterIdentity = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);

        var runner = soldierGO.GetComponent<ChargeSoldierRunner>();
        if (runner == null) runner = soldierGO.AddComponent<ChargeSoldierRunner>();

        runner.Run(
            soldierTransform  : soldierGO.transform,
            soldierEntity     : pickedSoldier,
            targetEntity      : ctx.TargetEntity,
            targetPos         : targetPos,
            soldierStat       : soldierStat,
            em                : em,
            casterTeam        : casterIdentity.Team,
            damageMultiplier  : EffectValue,
            chargeSpeed       : ChargeSpeed,
            knockbackMult     : KnockbackMult,
            fx                : new SkillEffectConfig
            {
                CasterEffectKey = CasterEffectKey,
                TargetEffectKey = TargetEffectKey,
                BaseEffectKey   = BaseEffectKey,
                DespawnDelay    = EffectDespawnDelay,
            }
        );
    }
}
