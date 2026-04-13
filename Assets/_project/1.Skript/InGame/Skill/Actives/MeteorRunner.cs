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
//  ■ 이펙트 타이밍
//    - BaseEffect  : 즉시 착탄 예정 위치 (낙하 예고 마커)
//    - TargetEffect: delay 후 착탄 시점 (폭발 이펙트)
// ============================================================

public class MeteorRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    public void Run(
        Vector3         impactPos,
        Entity          casterEntity,
        StatComponent   casterStat,
        EntityManager   em,
        TeamType        casterTeam,
        float           damageMultiplier,
        float           aoeRadius,
        float           delay,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            impactPos, casterEntity, casterStat, em,
            casterTeam, damageMultiplier, aoeRadius, delay, knockbackMult, fx));
    }

    IEnumerator Sequence(
        Vector3         impactPos,
        Entity          casterEntity,
        StatComponent   casterStat,
        EntityManager   em,
        TeamType        casterTeam,
        float           damageMultiplier,
        float           aoeRadius,
        float           delay,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        // ── ① 낙하 예고 이펙트 (즉시) ────────────────────────
        SkillEffectHelper.SpawnBase(fx.BaseEffectKey, impactPos, delay + fx.DespawnDelay);

        // ── ② 착탄 대기 ───────────────────────────────────────
        yield return new WaitForSeconds(delay);

        // ── ③ 폭발 이펙트 + AoE 피해 ─────────────────────────
        SkillEffectHelper.SpawnTarget(fx.TargetEffectKey, impactPos, fx.DespawnDelay);

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
