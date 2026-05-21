using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveChargeSoldier.cs — 돌격 병사 소환 (방패)
//
//  장수 후방에 임시 병사 3명을 소환해 전방으로 돌격시킨다.
//  돌진 경로 위 반경(HitRadius) 안에 있는 적에게
//  공격력×EffectValue 스킬 피해 + 뒤로 넉백.
//  ChargeSoldierRunner 가 이동·충돌을 처리한다.
// ============================================================

[CreateAssetMenu(fileName = "Active_ChargeSoldier", menuName = "BattleGame/Actives/ChargeSoldier")]
public class ActiveChargeSoldier : ActiveSkillData
{
    [Header("돌격 병사 설정")]
    [Tooltip("돌격 이동 속도 (유닛/초)")]
    public float ChargeSpeed = 18f;

    [Tooltip("피해 판정 반경 (유닛)")]
    public float HitRadius = 0.8f;

    [Tooltip("최대 돌진 거리 (유닛)")]
    public float ChargeDistance = 12f;

    [Tooltip("넉백 힘")]
    public float KnockbackForce = 5f;

    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        Vector3 casterPos = ctx.CasterTransform != null
            ? ctx.CasterTransform.position
            : Vector3.zero;

        // 돌진 방향: 타겟 방향, 없으면 +X (기본 전방)
        Vector3 chargeDir;
        if (ctx.HasTarget && em.Exists(ctx.TargetEntity))
        {
            var tf  = em.GetComponentData<LocalTransform>(ctx.TargetEntity);
            var raw = new Vector3(tf.Position.x - casterPos.x, tf.Position.y - casterPos.y, 0f);
            chargeDir = raw.sqrMagnitude > 0.01f ? raw.normalized : Vector3.right;
        }
        else
        {
            chargeDir = Vector3.right;
        }

        // 수직 방향 (2D)
        Vector3 perp = new Vector3(-chargeDir.y, chargeDir.x, 0f);

        float baseDamage = em.GetComponentData<StatComponent>(ctx.CasterEntity).Final[StatType.Attack] * EffectValue;
        var identity     = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);

        var fx = new SkillEffectConfig
        {
            CasterEffectKey = CasterEffectKey,
            TargetEffectKey = TargetEffectKey,
            BaseEffectKey   = BaseEffectKey,
            DespawnDelay    = EffectDespawnDelay,
        };

        if (ctx.CasterTransform != null)
            SkillEffectHelper.SpawnCaster(CasterEffectKey, casterPos, EffectDespawnDelay);

        // 후방 기준점 + 좌·중·우 3명 배치
        Vector3   behindBase = casterPos - chargeDir * 1.5f;
        Vector3[] offsets    = { Vector3.zero, perp * 0.9f, -perp * 0.9f };

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = behindBase + offsets[i];
            var go           = new GameObject("ChargeSoldier_Temp");
            go.transform.position = spawnPos;
            var runner = go.AddComponent<ChargeSoldierRunner>();
            runner.Launch(
                spawnPos:       spawnPos,
                chargeDir:      chargeDir,
                chargeSpeed:    ChargeSpeed,
                hitRadius:      HitRadius,
                maxDistance:    ChargeDistance,
                damage:         baseDamage,
                knockbackForce: KnockbackForce,
                casterTeam:     identity.Team,
                casterEntity:   ctx.CasterEntity,
                em:             em,
                fx:             fx);
        }
    }
}
