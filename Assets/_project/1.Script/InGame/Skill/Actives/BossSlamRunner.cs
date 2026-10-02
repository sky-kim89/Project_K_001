using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  BossSlamRunner.cs
//  분쇄 강타·도약 충격파 연출. 예고 → 포물선 도약 → 착지 → 경직.
//
//  ■ 같은 실행기로 이동형·제자리형을 나눈다
//    BossSlam 은 타겟 위치까지 높게 날아가고, BossJumpShockwave 는
//    시작 위치에서 낮게 뛴다. 착지 위치와 높이는 SO 가 결정한다.
//
//  ■ 피해는 착지 프레임에 한 번만
//    돌진과 달리 경로가 없어 프레임마다 훑을 이유가 없다.
//
//  ⚠ EntityLink.SyncPosition 을 꺼야 도약이 보인다
//    평소엔 EntityLink 가 매 프레임 ECS → GameObject 로 위치를 덮어쓴다.
//    도약 중 transform 을 직접 움직이므로 반드시 꺼야 한다.
// ============================================================

public class BossSlamRunner : MonoBehaviour
{
    Coroutine  _current;
    EntityLink _held;

    // 연출 중 죽거나 풀로 반납되면 꺼진 채로 남는다 — 여기서 반드시 되돌린다.
    void OnDisable()
    {
        if (_held != null) _held.SyncPosition = true;
        _held    = null;
        _current = null;
    }

    public void Run(ActiveBossSlam data, ActiveSkillContext ctx)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(Sequence(data, ctx));
    }

    IEnumerator Sequence(ActiveBossSlam d, ActiveSkillContext ctx)
    {
        Transform t  = ctx.CasterTransform;
        var       em = ctx.EntityManager;
        if (t == null) yield break;

        Vector3 start   = t.position;
        Vector3 landing = d.LeapToTarget ? ctx.TargetPosition : start;
        landing.z = start.z;

        Vector3 toLanding = landing - start;
        float   distance  = toLanding.magnitude;
        if (distance > d.MaxLeapDistance && distance > 0.001f)
            landing = start + toLanding / distance * d.MaxLeapDistance;

        // 도약 중에는 transform 을 직접 움직인다.
        _held = t.GetComponent<EntityLink>();
        if (_held != null) _held.SyncPosition = false;

        // ── ① 예고 — 발밑에 장판, 몸은 살짝 웅크렸다 편다 ──────
        //  ⚠ 반경을 곱해서 띄운다
        //    예고 장판은 "어디까지 맞는가" 를 말하는 그림이다. 프리팹 기본 크기로
        //    띄우면 반경 3 짜리로 보이는데 실제로는 7 까지 때린다 —
        //    피했다고 생각한 자리에서 맞으면 그건 예고가 아니다.
        //    (프리팹 기준 반경 3 — RareSkillEffectGenerator 의 스케일 연동 규칙)
        float fxScale = d.SlamRadius / 3f;
        SkillEffectHelper.Spawn(d.BaseEffectKey, landing, d.WindupTime + d.SlamTime + 0.3f,
                                default, fxScale);

        float e = 0f;
        while (e < d.WindupTime)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / d.WindupTime);
            // 출발 전에 살짝 가라앉아 도약을 읽게 한다.
            float dip = Mathf.Sin(k * Mathf.PI) * -0.35f;
            t.position = start + new Vector3(0f, dip, 0f);
            yield return null;
        }

        // ── ② 포물선 도약 ───────────────────────────────────────
        e = 0f;
        while (e < d.SlamTime)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / d.SlamTime);
            Vector3 p = Vector3.Lerp(start, landing, Mathf.SmoothStep(0f, 1f, k));
            p.y += Mathf.Sin(k * Mathf.PI) * d.JumpHeight;
            t.position = p;
            yield return null;
        }
        t.position = landing;

        // ── ③ 착탄 ─────────────────────────────────────────────
        // 착탄도 같은 배율 — 예고와 크기가 다르면 "예고보다 더 넓게 터졌다" 로 읽힌다
        SkillEffectHelper.Spawn(d.CasterEffectKey, landing, d.EffectDespawnDelay,
                                default, fxScale);
        CameraShaker.Impulse(0.6f, landing);

        Explode(em, ctx, landing, d);

        // 이동형 분쇄 강타는 착지 좌표를 ECS 에 넘겨야 동기화를 켠 뒤 되돌아가지 않는다.
        if (d.LeapToTarget && em.Exists(ctx.CasterEntity)
                           && em.HasComponent<LocalTransform>(ctx.CasterEntity))
        {
            var lt = em.GetComponentData<LocalTransform>(ctx.CasterEntity);
            lt.Position = landing;
            em.SetComponentData(ctx.CasterEntity, lt);
        }

        if (_held != null) _held.SyncPosition = true;
        _held = null;

        yield return new WaitForSeconds(d.RecoverTime);
        _current = null;
    }

    void Explode(EntityManager em, ActiveSkillContext ctx, Vector3 center, ActiveBossSlam d)
    {
        if (!em.Exists(ctx.CasterEntity)) return;
        if (!em.HasComponent<UnitIdentityComponent>(ctx.CasterEntity)) return;

        TeamType myTeam = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity).Team;
        float    damage = ctx.CasterStat.Final[StatType.Attack]
                        * d.DamageMultiplier * Mathf.Max(0.1f, d.EffectValue);

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<UnitIdentityComponent>(),
            ComponentType.ReadOnly<HitEventBufferElement>(),
            ComponentType.Exclude<DeadTag>());

        var ents = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var trs  = query.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
        var ids  = query.ToComponentDataArray<UnitIdentityComponent>(Unity.Collections.Allocator.Temp);

        float  r2 = d.SlamRadius * d.SlamRadius;
        float3 c  = center;

        for (int i = 0; i < ents.Length; i++)
        {
            if (ids[i].Team == myTeam) continue;

            float3 to     = trs[i].Position - c;
            float  distSq = math.lengthsq(to);
            if (distSq > r2) continue;

            // 중심에서 바깥으로 밀어낸다. 정확히 겹쳐 있으면 임의 방향.
            float3 dir = distSq > 0.0001f ? math.normalize(to) : new float3(1f, 0f, 0f);

            // 가장자리는 피해를 덜 받는다 — 반경이 넓어 전멸을 막는 완충
            float falloff = Mathf.Lerp(1f, 0.55f, Mathf.Sqrt(distSq) / d.SlamRadius);

            SkillEffectHelper.Spawn(d.TargetEffectKey, (Vector3)trs[i].Position, d.EffectDespawnDelay);

            em.GetBuffer<HitEventBufferElement>(ents[i]).Add(new HitEventBufferElement
            {
                Damage         = damage * falloff,
                HitDirection   = dir * d.KnockbackMult,
                AttackerEntity = ctx.CasterEntity,
                Type           = HitType.Skill,
            });
        }

        ents.Dispose();
        trs.Dispose();
        ids.Dispose();
        query.Dispose();
    }
}
