using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  LeapStrikeRunner.cs — 도약 강타 시퀀스 실행기
//
//  ■ 시퀀스
//    1. 타겟 방향으로 LeapSpeed 로 도약
//    2. 착지 반경(aoeRadius) 내 적 전체에 데미지 + 넉백 주입
//    3. 원위치로 ReturnSpeed 로 복귀
//
//  OnDisable 에서 _current = null 로 초기화 → 풀 재사용 시 StopCoroutine 오류 방지.
// ============================================================

public class LeapStrikeRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    // ── 공개 API ─────────────────────────────────────────────

    public void Run(
        Transform     casterTransform,
        Vector3       targetPos,
        Entity        casterEntity,
        StatComponent casterStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         aoeRadius,
        float         leapSpeed,
        float         returnSpeed,
        float         knockbackMult)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            casterTransform, targetPos, casterEntity, casterStat, em,
            casterTeam, damageMultiplier, aoeRadius, leapSpeed, returnSpeed, knockbackMult));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator Sequence(
        Transform     casterTransform,
        Vector3       targetPos,
        Entity        casterEntity,
        StatComponent casterStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         aoeRadius,
        float         leapSpeed,
        float         returnSpeed,
        float         knockbackMult)
    {
        Vector3 originPos = casterTransform.position;

        // ── ① 도약 ────────────────────────────────────────────
        yield return MoveToward(casterTransform, targetPos, leapSpeed, stopDistance: 1.0f);

        // ── ② 착지 AoE 타격 ───────────────────────────────────
        em.CompleteAllTrackedJobs();

        float3 landPos = new float3(casterTransform.position.x, casterTransform.position.y, 0f);

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

            float dist = math.distance(landPos, new float3(transforms[i].Position.x, transforms[i].Position.y, 0f));
            if (dist > aoeRadius) continue;

            if (!em.HasBuffer<HitEventBufferElement>(entities[i])) continue;

            float  damage   = casterStat.Final[StatType.Attack] * damageMultiplier;
            float3 knockDir = math.normalizesafe(transforms[i].Position - landPos);

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

        // ── ③ 복귀 ────────────────────────────────────────────
        yield return MoveToward(casterTransform, originPos, returnSpeed, stopDistance: 0.1f);

        _current = null;
    }

    IEnumerator MoveToward(Transform t, Vector3 destination, float speed, float stopDistance)
    {
        while (Vector3.Distance(t.position, destination) > stopDistance)
        {
            t.position = Vector3.MoveTowards(t.position, destination, speed * Time.deltaTime);
            yield return null;
        }
    }
}
