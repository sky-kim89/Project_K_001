using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ChargeSoldierRunner.cs — 돌격 병사 이동·타격 처리기
//
//  선택된 병사 GO 를 타겟 방향으로 이동시키고 도착 시 피해를 가한다.
//  병사는 타격 후 정상 전투 상태로 복귀한다.
//  OnDisable 에서 코루틴 정리 → 풀 재사용 시 안전.
// ============================================================

public class ChargeSoldierRunner : MonoBehaviour
{
    Coroutine _current;

    void OnDisable() { _current = null; }

    // ── 공개 API ─────────────────────────────────────────────

    public void Run(
        Transform     soldierTransform,
        Entity        soldierEntity,
        Entity        targetEntity,
        Vector3       targetPos,
        StatComponent soldierStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         chargeSpeed,
        float         knockbackMult)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(
            soldierTransform, soldierEntity, targetEntity, targetPos,
            soldierStat, em, casterTeam, damageMultiplier, chargeSpeed, knockbackMult));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator Sequence(
        Transform     soldierTransform,
        Entity        soldierEntity,
        Entity        targetEntity,
        Vector3       targetPos,
        StatComponent soldierStat,
        EntityManager em,
        TeamType      casterTeam,
        float         damageMultiplier,
        float         chargeSpeed,
        float         knockbackMult)
    {
        // ── ① 돌격 이동 ───────────────────────────────────────
        while (Vector3.Distance(soldierTransform.position, targetPos) > 0.8f)
        {
            soldierTransform.position = Vector3.MoveTowards(
                soldierTransform.position, targetPos, chargeSpeed * Time.deltaTime);

            // ECS LocalTransform 도 동기화
            if (em.Exists(soldierEntity))
            {
                em.SetComponentData(soldierEntity, LocalTransform.FromPosition(
                    new float3(soldierTransform.position.x,
                               soldierTransform.position.y,
                               soldierTransform.position.z)));
            }

            yield return null;
        }

        // ── ② 타격 ────────────────────────────────────────────
        em.CompleteAllTrackedJobs();

        if (em.Exists(targetEntity) && em.HasBuffer<HitEventBufferElement>(targetEntity))
        {
            float  damage   = soldierStat.Final[StatType.Attack] * damageMultiplier;
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
