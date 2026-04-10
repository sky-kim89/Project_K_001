using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  SuicideSoldierRunner.cs — 자폭 병사 투사·폭발 처리기
//
//  제너럴 염력으로 병사를 포물선 궤도로 던지는 연출:
//    t=0   → 병사 현재 위치 (startPos)
//    t=0.5 → 포물선 정점 (arcHeight 만큼 위)
//    t=1   → 타겟 위치 착탄 + 폭발
//
//  OnDisable 에서 코루틴 정리 → 풀 재사용 시 안전.
// ============================================================

public class SuicideSoldierRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    // ── 공개 API ─────────────────────────────────────────────

    public void Run(
        Transform     soldierTransform,
        Entity        soldierEntity,
        Vector3       targetPos,
        StatComponent soldierStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         aoeRadius,
        float         flightDuration,
        float         arcHeight,
        float         knockbackMult)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            soldierTransform, soldierEntity, targetPos, soldierStat, em,
            casterTeam, damageMultiplier, aoeRadius, flightDuration, arcHeight, knockbackMult));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator Sequence(
        Transform     soldierTransform,
        Entity        soldierEntity,
        Vector3       targetPos,
        StatComponent soldierStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         aoeRadius,
        float         flightDuration,
        float         arcHeight,
        float         knockbackMult)
    {
        Vector3 startPos = soldierTransform.position;
        float   elapsed  = 0f;
        float   duration = flightDuration > 0f ? flightDuration : 0.5f;

        // ── ① 포물선 비행 ────────────────────────────────────
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);

            // 선형 보간 위치
            Vector3 flatPos = Vector3.Lerp(startPos, targetPos, t);

            // 포물선 높이: sin(π·t) 로 자연스러운 아치
            float yArc = arcHeight * Mathf.Sin(Mathf.PI * t);

            soldierTransform.position = new Vector3(flatPos.x, flatPos.y + yArc, flatPos.z);

            // ECS LocalTransform 동기화
            if (em.Exists(soldierEntity))
            {
                em.SetComponentData(soldierEntity, LocalTransform.FromPosition(
                    new float3(soldierTransform.position.x,
                               soldierTransform.position.y,
                               soldierTransform.position.z)));
            }

            yield return null;
        }

        // 착탄 위치로 스냅
        soldierTransform.position = targetPos;

        // ── ② 폭발 AoE ───────────────────────────────────────
        em.CompleteAllTrackedJobs();

        float3 center = new float3(targetPos.x, targetPos.y, 0f);

        var query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<UnitIdentityComponent>(),
                                         ComponentType.ReadOnly<LocalTransform>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });

        NativeArray<Entity>         entities   = query.ToEntityArray(Allocator.Temp);
        NativeArray<LocalTransform> transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            var id = em.GetComponentData<UnitIdentityComponent>(entities[i]);
            if (id.Team == casterTeam) continue;

            float dist = math.distance(center, new float3(transforms[i].Position.x, transforms[i].Position.y, 0f));
            if (dist > aoeRadius) continue;

            if (!em.HasBuffer<HitEventBufferElement>(entities[i])) continue;

            float  damage   = soldierStat.Final[StatType.Attack] * damageMultiplier;
            float3 knockDir = dist > 0.01f
                ? math.normalizesafe(new float3(transforms[i].Position.x, transforms[i].Position.y, 0f) - center)
                : new float3(1f, 0f, 0f);

            em.GetBuffer<HitEventBufferElement>(entities[i]).Add(new HitEventBufferElement
            {
                Damage         = damage,
                HitDirection   = knockDir * knockbackMult,
                AttackerEntity = soldierEntity,
            });
        }

        entities.Dispose();
        transforms.Dispose();
        query.Dispose();

        // ── ③ 병사 즉사 ──────────────────────────────────────
        if (em.Exists(soldierEntity) && em.HasBuffer<HitEventBufferElement>(soldierEntity))
        {
            em.GetBuffer<HitEventBufferElement>(soldierEntity).Add(new HitEventBufferElement
            {
                Damage         = 999999f,
                HitDirection   = float3.zero,
                AttackerEntity = soldierEntity,
            });
        }

        _current = null;
    }
}
