using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  GravestoneRunner.cs
//  비석 강림(Gravestone)의 낙하 → 착탄 → 소환 시퀀스.
//
//  비석마다 독립된 코루틴이 dropInterval 씩 늦게 시작한다.
//  → 예고와 착탄이 겹치면서 "우수수 떨어지는" 그림이 된다.
//    전부 동시에 떨어뜨리면 한 번의 폭발로 뭉개지고,
//    완전히 순차로 처리하면(하나 끝나고 다음) 12기에 10초가 넘어 늘어진다.
//
//  ⚠ 소환 스켈레톤은 SkeletonSpawner 가 만든다
//    스켈레톤 소환(ActiveSummonSkeleton)과 외형·태그 규칙을 공유해야
//    "같은 스켈레톤" 으로 보인다.
// ============================================================

public class GravestoneRunner : MonoBehaviour
{
    public void Run(EntityManager em, Entity casterEntity, TeamType casterTeam,
                    float3 center, int stoneCount, float scatterRadius, float hitRadius,
                    float warningTime, float dropInterval, float damage, float knockMult,
                    UnitStat generalStat, UnitJob generalJob, float statRatio, string poolKey,
                    SkillEffectConfig fx)
    {
        // ── 낙하 지점 산개 ───────────────────────────────────
        //  황금각(137.5°)으로 돌리면 12기가 겹치지 않고 고르게 퍼진다.
        //  같은 각도 간격으로 돌리면 개수에 따라 줄이 서 보인다.
        for (int i = 0; i < stoneCount; i++)
        {
            float ang = i * 137.5f * Mathf.Deg2Rad + UnityEngine.Random.Range(-0.2f, 0.2f);
            float r   = stoneCount == 1
                ? 0f
                : scatterRadius * Mathf.Sqrt((i + 0.5f) / stoneCount);   // 안쪽부터 바깥으로

            var spot = new Vector3(center.x + Mathf.Cos(ang) * r,
                                   center.y + Mathf.Sin(ang) * r, 0f);

            StartCoroutine(DropOne(em, casterEntity, casterTeam, spot, hitRadius,
                                   warningTime, dropInterval * i, damage, knockMult,
                                   generalStat, generalJob, statRatio, poolKey, fx));
        }
    }

    IEnumerator DropOne(EntityManager em, Entity casterEntity, TeamType casterTeam,
                        Vector3 spot, float hitRadius, float warningTime, float startDelay,
                        float damage, float knockMult,
                        UnitStat generalStat, UnitJob generalJob, float statRatio, string poolKey,
                        SkillEffectConfig fx)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        // ── ① 낙하 예고 ──────────────────────────────────────
        SkillEffectHelper.Spawn(fx.BaseEffectKey, spot, warningTime + fx.DespawnDelay);

        if (warningTime > 0f) yield return new WaitForSeconds(warningTime);

        // ── ② 착탄 ───────────────────────────────────────────
        SkillEffectHelper.Spawn(fx.TargetEffectKey, spot, fx.DespawnDelay);

        // "딱" — 착탄마다 화면을 때린다.
        //  ⚠ 세기를 키우지 말 것. 12기가 0.12초 간격이라 트라우마가 계속 쌓인다.
        //    한 발은 가볍게(0.16), 대신 연타로 지속되는 진동이 무게를 만든다.
        //    (CameraShaker 가 1.0 으로 클램프하므로 화면이 터지진 않는다)
        CameraShaker.Impulse(0.16f, spot);

        em.CompleteAllTrackedJobs();
        var hit = SkillCrowdControl.CollectEnemiesInRadius(
            em, new float3(spot.x, spot.y, 0f), hitRadius, casterTeam);

        foreach (var t in hit)
        {
            if (!em.Exists(t)) continue;

            Vector3 tp      = SkillCrowdControl.PositionOf(em, t);
            float3  outward = new float3(tp.x - spot.x, tp.y - spot.y, 0f);
            SkillCrowdControl.DealDamage(em, t, damage, outward, knockMult, casterEntity);
        }

        // ── ③ 망자가 일어난다 ────────────────────────────────
        SkillEffectHelper.Spawn(fx.CasterEffectKey, spot, fx.DespawnDelay);
        SkeletonSpawner.Spawn(em, poolKey, spot, generalStat, statRatio, casterEntity, generalJob);
    }
}
