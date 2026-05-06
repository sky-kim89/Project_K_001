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
//  ■ 이펙트 타이밍
//    - BaseEffect  : 도약 시작 시 시전자 위치 (발구름 연출)
//    - CasterEffect: 착지 시 시전자 위치 (AoE 충격파)
//    - TargetEffect: 범위 내 각 피격 적 위치
// ============================================================

public class LeapStrikeRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    public void Run(
        Transform       casterTransform,
        Vector3         targetPos,
        Entity          casterEntity,
        StatComponent   casterStat,
        EntityManager   em,
        TeamType        casterTeam,
        float           damageMultiplier,
        float           aoeRadius,
        float           leapSpeed,
        float           returnSpeed,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            casterTransform, targetPos, casterEntity, casterStat, em,
            casterTeam, damageMultiplier, aoeRadius, leapSpeed, returnSpeed, knockbackMult, fx));
    }

    IEnumerator Sequence(
        Transform       casterTransform,
        Vector3         targetPos,
        Entity          casterEntity,
        StatComponent   casterStat,
        EntityManager   em,
        TeamType        casterTeam,
        float           damageMultiplier,
        float           aoeRadius,
        float           leapSpeed,
        float           returnSpeed,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        Vector3 originPos = casterTransform.position;

        // ── ① 도약 시작 이펙트 ────────────────────────────────
        SkillEffectHelper.SpawnBase(fx.BaseEffectKey, casterTransform.position, fx.DespawnDelay);

        // ── ② 도약 ────────────────────────────────────────────
        yield return MoveToward(casterTransform, targetPos, leapSpeed, stopDistance: 1.0f);

        // ── ③ 착지 이펙트 + AoE 타격 ─────────────────────────
        Vector3 landPos3D = casterTransform.position;
        SkillEffectHelper.SpawnCaster(fx.CasterEffectKey, landPos3D, fx.DespawnDelay);

        em.CompleteAllTrackedJobs();
        float3 landPos = new float3(landPos3D.x, landPos3D.y, 0f);

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

            // 피격 적마다 이펙트
            SkillEffectHelper.SpawnTarget(fx.TargetEffectKey,
                new Vector3(transforms[i].Position.x, transforms[i].Position.y, transforms[i].Position.z),
                fx.DespawnDelay);

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

        // ── ④ 복귀 ────────────────────────────────────────────
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
