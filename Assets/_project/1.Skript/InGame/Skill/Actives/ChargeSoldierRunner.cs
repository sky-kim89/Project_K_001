using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ChargeSoldierRunner.cs — 돌격 병사 이동·타격 처리기
//
//  ■ 이펙트 타이밍
//    - BaseEffect  : 돌격 시작 시 병사 위치 (돌진 연출)
//    - TargetEffect: 타격 시 타겟 위치 (충돌 이펙트)
// ============================================================

public class ChargeSoldierRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    public void Run(
        Transform       soldierTransform,
        Entity          soldierEntity,
        Entity          targetEntity,
        Vector3         targetPos,
        StatComponent   soldierStat,
        EntityManager   em,
        TeamType        casterTeam,
        float           damageMultiplier,
        float           chargeSpeed,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            soldierTransform, soldierEntity, targetEntity, targetPos,
            soldierStat, em, casterTeam, damageMultiplier, chargeSpeed, knockbackMult, fx));
    }

    IEnumerator Sequence(
        Transform       soldierTransform,
        Entity          soldierEntity,
        Entity          targetEntity,
        Vector3         targetPos,
        StatComponent   soldierStat,
        EntityManager   em,
        TeamType        casterTeam,
        float           damageMultiplier,
        float           chargeSpeed,
        float           knockbackMult,
        SkillEffectConfig fx)
    {
        // ── ① 돌진 시작 이펙트 ────────────────────────────────
        SkillEffectHelper.SpawnBase(fx.BaseEffectKey, soldierTransform.position, fx.DespawnDelay);

        // ── ② 돌격 이동 ───────────────────────────────────────
        while (Vector3.Distance(soldierTransform.position, targetPos) > 0.8f)
        {
            soldierTransform.position = Vector3.MoveTowards(
                soldierTransform.position, targetPos, chargeSpeed * Time.deltaTime);

            if (em.Exists(soldierEntity))
            {
                em.SetComponentData(soldierEntity, LocalTransform.FromPosition(
                    new float3(soldierTransform.position.x,
                               soldierTransform.position.y,
                               soldierTransform.position.z)));
            }

            yield return null;
        }

        // ── ③ 타격 + 충돌 이펙트 ────────────────────────────
        SkillEffectHelper.SpawnTarget(fx.TargetEffectKey, targetPos, fx.DespawnDelay);

        em.CompleteAllTrackedJobs();

        if (em.Exists(targetEntity) && em.HasBuffer<HitEventBufferElement>(targetEntity))
        {
            float  damage     = soldierStat.Final[StatType.Attack] * damageMultiplier;
            float3 soldierPos = new float3(soldierTransform.position.x, soldierTransform.position.y, 0f);
            float3 targetPos3 = new float3(targetPos.x, targetPos.y, 0f);
            float3 knockDir   = math.normalizesafe(targetPos3 - soldierPos);

            em.GetBuffer<HitEventBufferElement>(targetEntity).Add(new HitEventBufferElement
            {
                Damage         = damage,
                HitDirection   = knockDir * knockbackMult,
                AttackerEntity = soldierEntity,
            });
        }

        _current = null;
    }
}
