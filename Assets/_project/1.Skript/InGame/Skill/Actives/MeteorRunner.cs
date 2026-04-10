using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  MeteorRunner.cs — 메테오 착탄 처리기
//
//  delay 초 대기 후 impactPos 반경 내 적 전체에 피해 + 넉백을 가한다.
//  OnDisable 에서 코루틴 정리 → 풀 재사용 시 안전.
// ============================================================

public class MeteorRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    // ── 공개 API ─────────────────────────────────────────────

    public void Run(
        Vector3       impactPos,
        Entity        casterEntity,
        StatComponent casterStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         aoeRadius,
        float         delay,
        float         knockbackMult)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            impactPos, casterEntity, casterStat, em,
            casterTeam, damageMultiplier, aoeRadius, delay, knockbackMult));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator Sequence(
        Vector3       impactPos,
        Entity        casterEntity,
        StatComponent casterStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         aoeRadius,
        float         delay,
        float         knockbackMult)
    {
        // ── ① 착탄 대기 ───────────────────────────────────────
        yield return new WaitForSeconds(delay);

        // ── ② AoE 피해 ────────────────────────────────────────
        em.CompleteAllTrackedJobs();

        float3 center = new float3(impactPos.x, impactPos.y, 0f);

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

            float  damage   = casterStat.Final[StatType.Attack] * damageMultiplier;
            float3 knockDir = dist > 0.01f
                ? math.normalizesafe(new float3(transforms[i].Position.x, transforms[i].Position.y, 0f) - center)
                : new float3(1f, 0f, 0f);

            em.GetBuffer<HitEventBufferElement>(entities[i]).Add(new HitEventBufferElement
            {
                Damage         = damage,
                HitDirection   = knockDir * knockbackMult,
                AttackerEntity = casterEntity,
            });
        }

        entities.Dispose();
        transforms.Dispose();
        query.Dispose();

        _current = null;
    }
}
