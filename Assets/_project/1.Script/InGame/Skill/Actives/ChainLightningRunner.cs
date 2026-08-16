using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ChainLightningRunner.cs
//  연쇄 번개(ChainLightning)의 튀는 시퀀스.
//
//    ① 시전자 → 첫 대상으로 번개를 잇는다
//    ② 대상마다: 피해 + 감전(이동속도 격감) + 착탄 이펙트
//    ③ 방금 맞은 적 기준 jumpRange 안에서 **아직 안 맞은** 가장 가까운 적으로 튄다
//    ④ 튈 때마다 피해가 누적 증가한다
//
//  ⚠ 이미 맞은 적은 후보에서 뺀다
//    안 빼면 두 적 사이를 왕복하며 같은 둘만 8번 때린다 — 연쇄로 안 보인다.
//
//  ⚠ 진행 중인 시전을 끊지 않는다 (연속 시전·중복 시전 대응)
//    시퀀스마다 자기 방문 목록을 들고 있어 병행 실행해도 간섭하지 않는다.
// ============================================================

public class ChainLightningRunner : MonoBehaviour
{
    public void Run(EntityManager em, Entity casterEntity, TeamType casterTeam,
                    Transform casterTf, Entity firstTarget, float jumpRange,
                    float baseDamage, int maxChains, float damageGrowth, float chainInterval,
                    float slowRatio, float slowDuration, float knockMult,
                    SkillEffectConfig fx)
    {
        StartCoroutine(Sequence(em, casterEntity, casterTeam, casterTf, firstTarget, jumpRange,
                                baseDamage, maxChains, damageGrowth, chainInterval,
                                slowRatio, slowDuration, knockMult, fx));
    }

    IEnumerator Sequence(EntityManager em, Entity casterEntity, TeamType casterTeam,
                         Transform casterTf, Entity firstTarget, float jumpRange,
                         float baseDamage, int maxChains, float damageGrowth, float chainInterval,
                         float slowRatio, float slowDuration, float knockMult,
                         SkillEffectConfig fx)
    {
        var visited = new HashSet<Entity>();

        Vector3 fromPos = casterTf != null
            ? casterTf.position
            : SkillCrowdControl.PositionOf(em, casterEntity);

        // 시전자에게 방전 연출
        SkillEffectHelper.Spawn(fx.CasterEffectKey, fromPos, fx.DespawnDelay);

        em.CompleteAllTrackedJobs();

        Entity current = em.Exists(firstTarget)
            ? firstTarget
            : FindNearest(em, new float3(fromPos.x, fromPos.y, 0f), jumpRange * 2f, casterTeam, visited);

        float damage = baseDamage;

        for (int hop = 0; hop < maxChains; hop++)
        {
            if (current == Entity.Null || !em.Exists(current)) break;

            visited.Add(current);

            Vector3 toPos = SkillCrowdControl.PositionOf(em, current);

            // 번개 줄기 — from → to 로 LineRenderer 를 잇는다 (ImpactSparks 자식이 to 로 이동)
            SkillEffectHelper.Spawn(fx.BaseEffectKey, fromPos, toPos, fx.DespawnDelay);
            SkillEffectHelper.Spawn(fx.TargetEffectKey, toPos, fx.DespawnDelay);

            float3 dir = new float3(toPos.x - fromPos.x, toPos.y - fromPos.y, 0f);
            SkillCrowdControl.DealDamage(em, current, damage, dir, knockMult, casterEntity);

            // 감전 — 번개에 맞은 적은 발이 묶인다
            if (slowRatio > 0f)
                SkillCrowdControl.AddEffect(em, current, StatType.MoveSpeed, 1f - slowRatio,
                                            EffectMode.Multiply, slowDuration,
                                            ActiveSkillId.ChainLightning);

            // 다음 대상 — 방금 맞은 적을 기준으로 찾는다
            fromPos = toPos;
            damage *= 1f + damageGrowth;

            if (chainInterval > 0f) yield return new WaitForSeconds(chainInterval);

            em.CompleteAllTrackedJobs();
            current = FindNearest(em, new float3(toPos.x, toPos.y, 0f), jumpRange, casterTeam, visited);
        }
    }

    /// <summary>origin 반경 안에서 아직 안 맞은 가장 가까운 적. 없으면 Entity.Null.</summary>
    static Entity FindNearest(EntityManager em, float3 origin, float range,
                              TeamType casterTeam, HashSet<Entity> visited)
    {
        var candidates = SkillCrowdControl.CollectEnemiesInRadius(em, origin, range, casterTeam);

        Entity best     = Entity.Null;
        float  bestDist = float.MaxValue;

        foreach (var e in candidates)
        {
            if (visited.Contains(e)) continue;

            Vector3 p = SkillCrowdControl.PositionOf(em, e);
            float d = math.lengthsq(new float3(p.x - origin.x, p.y - origin.y, 0f));
            if (d >= bestDist) continue;

            best     = e;
            bestDist = d;
        }

        return best;
    }
}
