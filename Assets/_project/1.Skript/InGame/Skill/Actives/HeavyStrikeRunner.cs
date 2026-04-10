using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  HeavyStrikeRunner.cs
//  강타(HeavyStrike) 스킬의 돌진·타격·복귀 시퀀스를 실행하는 MonoBehaviour.
//
//  ActiveHeavyStrike.Execute() 가 이 컴포넌트를 통해 코루틴을 구동한다.
//  GetComponent 가 없을 때 AddComponent 로 동적 추가되므로
//  프리팹에 미리 붙이지 않아도 된다.
//
//  ■ 시퀀스
//    1. 돌진: 타겟 방향으로 DashSpeed 로 이동
//    2. 타격: HitEventBufferElement 추가 (ECS 데미지 처리)
//    3. 복귀: 원래 위치로 ReturnSpeed 로 이동
// ============================================================

public class HeavyStrikeRunner : MonoBehaviour
{
    Coroutine _current;

    // 풀 반납(SetActive false) 시 Coroutine 레퍼런스 초기화 — 재사용 시 StopCoroutine 오류 방지
    void OnDisable()
    {
        _current = null;
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void Run(
        Transform   casterTransform,
        Vector3     targetPos,
        Entity      targetEntity,
        Entity      casterEntity,
        StatComponent casterStat,
        EntityManager em,
        float       damageMultiplier,
        float       dashSpeed,
        float       returnSpeed,
        float       knockbackMult)
    {
        // 이미 실행 중이면 중단하고 재시작
        if (_current != null)
            StopCoroutine(_current);

        _current = StartCoroutine(Sequence(
            casterTransform, targetPos,
            targetEntity, casterEntity, casterStat, em,
            damageMultiplier, dashSpeed, returnSpeed, knockbackMult));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator Sequence(
        Transform   casterTransform,
        Vector3     targetPos,
        Entity      targetEntity,
        Entity      casterEntity,
        StatComponent casterStat,
        EntityManager em,
        float       damageMultiplier,
        float       dashSpeed,
        float       returnSpeed,
        float       knockbackMult)
    {
        Vector3 originPos = casterTransform.position;

        // ── ① 돌진 ────────────────────────────────────────────
        yield return MoveToward(casterTransform, targetPos, dashSpeed, stopDistance: 0.8f);

        // ── ② 타격 (ECS) ──────────────────────────────────────
        if (em.Exists(targetEntity) && em.HasBuffer<HitEventBufferElement>(targetEntity))
        {
            float damage     = casterStat.Final[StatType.Attack] * damageMultiplier;
            float3 hitDir    = GetHitDirection(casterTransform.position, targetPos);

            em.GetBuffer<HitEventBufferElement>(targetEntity).Add(new HitEventBufferElement
            {
                Damage        = damage,
                HitDirection  = hitDir * knockbackMult,   // 강한 넉백
                AttackerEntity = casterEntity,
            });
        }

        // ── ③ 복귀 ────────────────────────────────────────────
        yield return MoveToward(casterTransform, originPos, returnSpeed, stopDistance: 0.1f);

        _current = null;
    }

    /// <summary>지정 위치까지 일정 속도로 이동하는 코루틴.</summary>
    IEnumerator MoveToward(Transform t, Vector3 destination, float speed, float stopDistance)
    {
        while (Vector3.Distance(t.position, destination) > stopDistance)
        {
            t.position = Vector3.MoveTowards(t.position, destination, speed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>타격 방향 계산 (공격자 → 타겟 방향으로 넉백).</summary>
    static float3 GetHitDirection(Vector3 attackerPos, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - attackerPos);
        float   mag = dir.magnitude;
        if (mag < 0.001f) return new float3(1f, 0f, 0f);
        return new float3(dir.x / mag, dir.y / mag, dir.z / mag);
    }
}
