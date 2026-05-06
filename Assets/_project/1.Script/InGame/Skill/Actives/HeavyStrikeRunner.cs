using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  HeavyStrikeRunner.cs
//  강타(HeavyStrike) 스킬의 돌진·타격·복귀 시퀀스를 실행하는 MonoBehaviour.
//
//  ■ 이펙트 타이밍
//    - BaseEffect  : 돌진 시작 시 시전자 위치 (출발 연출)
//    - CasterEffect: 타겟 도착 시 시전자 위치 (타격 임팩트)
//    - TargetEffect: 타격 시 타겟 위치
// ============================================================

public class HeavyStrikeRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    public void Run(
        Transform       casterTransform,
        Vector3         targetPos,
        Entity          targetEntity,
        Entity          casterEntity,
        StatComponent   casterStat,
        EntityManager   em,
        float           damageMultiplier,
        float           dashSpeed,
        float           returnSpeed,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            casterTransform, targetPos, targetEntity, casterEntity, casterStat, em,
            damageMultiplier, dashSpeed, returnSpeed, knockbackMult, fx));
    }

    IEnumerator Sequence(
        Transform       casterTransform,
        Vector3         targetPos,
        Entity          targetEntity,
        Entity          casterEntity,
        StatComponent   casterStat,
        EntityManager   em,
        float           damageMultiplier,
        float           dashSpeed,
        float           returnSpeed,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        Vector3 originPos = casterTransform.position;

        // ── ① 출발 이펙트 ─────────────────────────────────────
        SkillEffectHelper.SpawnBase(fx.BaseEffectKey, casterTransform.position, fx.DespawnDelay);

        // ── ② 돌진 ────────────────────────────────────────────
        yield return MoveToward(casterTransform, targetPos, dashSpeed, stopDistance: 0.8f);

        // ── ③ 타격 + 임팩트 이펙트 ───────────────────────────
        Vector3 hitPos = casterTransform.position;
        SkillEffectHelper.SpawnCaster(fx.CasterEffectKey, hitPos, fx.DespawnDelay);
        SkillEffectHelper.SpawnTarget(fx.TargetEffectKey, targetPos, fx.DespawnDelay);

        if (em.Exists(targetEntity) && em.HasBuffer<HitEventBufferElement>(targetEntity))
        {
            float  damage  = casterStat.Final[StatType.Attack] * damageMultiplier;
            float3 hitDir  = GetHitDirection(hitPos, targetPos);

            em.GetBuffer<HitEventBufferElement>(targetEntity).Add(new HitEventBufferElement
            {
                Damage         = damage,
                HitDirection   = hitDir * knockbackMult,
                AttackerEntity = casterEntity,
            });
        }

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

    static float3 GetHitDirection(Vector3 attackerPos, Vector3 targetPos)
    {
        Vector3 dir = targetPos - attackerPos;
        float   mag = dir.magnitude;
        if (mag < 0.001f) return new float3(1f, 0f, 0f);
        return new float3(dir.x / mag, dir.y / mag, dir.z / mag);
    }
}
