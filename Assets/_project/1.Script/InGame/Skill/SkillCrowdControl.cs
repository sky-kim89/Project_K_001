using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  SkillCrowdControl.cs
//  희귀 스킬들이 공유하는 상태이상·범위 판정 유틸리티.
//
//  ■ 스턴은 반드시 여기를 거친다
//    HitReactionComponent 를 직접 쓰면 보스의 CCResistance 를 건너뛴다.
//    (UnitHitSystem 은 히트 이벤트로 들어온 스턴에만 내성을 곱한다)
//    보스가 안 굳는 건 의도된 설계이므로, 여기서 내성을 곱해 존중한다.
//    → 보스에게는 스턴이 0 이 되고 피해·넉백만 들어간다.
//
//  ■ 떨림 연출 (Tremble)
//    EntityLink.SyncPosition 을 잠시 꺼야 transform 을 직접 흔들 수 있다.
//    (LateUpdate 가 매 프레임 ECS 위치로 덮어쓰기 때문)
//    끝나면 반드시 되돌린다 — 안 되돌리면 그 유닛은 제자리에 붙어버린다.
// ============================================================

public static class SkillCrowdControl
{
    /// <summary>대상을 경직시킨다. 보스·엘리트의 CC 내성만큼 지속시간이 줄어든다.</summary>
    public static void Stun(EntityManager em, Entity target, float duration)
    {
        if (duration <= 0f || !em.Exists(target)) return;

        // 보스 CC 내성 적용 — 내성 1 이면 스턴이 통째로 사라진다
        if (em.HasComponent<BossComponent>(target))
            duration *= 1f - em.GetComponentData<BossComponent>(target).CCResistance;

        if (duration <= 0.01f) return;

        if (em.HasComponent<HitReactionComponent>(target))
        {
            var reaction = em.GetComponentData<HitReactionComponent>(target);
            reaction.IsStunned    = true;
            reaction.StunDuration = math.max(reaction.StunDuration, duration);
            reaction.StunTimer    = math.max(reaction.StunTimer,    duration);
            em.SetComponentData(target, reaction);
        }

        if (em.HasComponent<UnitStateComponent>(target))
        {
            var state = em.GetComponentData<UnitStateComponent>(target);
            state.Previous   = state.Current;
            state.Current    = UnitState.Hit;
            state.StateTimer = 0f;
            em.SetComponentData(target, state);
        }
    }

    // ── 버프 / 디버프 ────────────────────────────────────────

    public static void AddEffect(EntityManager em, Entity target, StatType stat, float delta,
                                 EffectMode mode, float duration, ActiveSkillId sourceSkill)
    {
        if (!em.Exists(target) || !em.HasBuffer<StatusEffectBufferElement>(target)) return;

        em.GetBuffer<StatusEffectBufferElement>(target).Add(new StatusEffectBufferElement
        {
            Stat       = stat,
            Delta      = delta,
            Mode       = mode,
            Duration   = duration,
            Remaining  = duration,
            SourceType = BuffSourceType.ActiveSkill,
            SourceId   = (int)sourceSkill,
        });
    }

    // ── 범위 수집 ────────────────────────────────────────────

    /// <summary>center 반경 안의 적(= casterTeam 과 다른 팀)을 모은다.</summary>
    public static List<Entity> CollectEnemiesInRadius(EntityManager em, float3 center,
                                                      float radius, TeamType casterTeam)
    {
        var result   = new List<Entity>();
        float radSq  = radius * radius;
        float3 c     = new float3(center.x, center.y, 0f);

        var query = MakeUnitQuery(em);
        var entities   = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (em.GetComponentData<UnitIdentityComponent>(entities[i]).Team == casterTeam) continue;

            float3 p = new float3(transforms[i].Position.x, transforms[i].Position.y, 0f);
            if (math.lengthsq(p - c) > radSq) continue;

            result.Add(entities[i]);
        }

        entities.Dispose();
        transforms.Dispose();
        query.Dispose();
        return result;
    }

    /// <summary>
    /// origin 에서 forward 방향으로 뻗은 길이 length·폭 width 의 직선 띠 안의 적을 모은다.
    /// (점-선분 거리 판정 — 일도양단의 참격선)
    /// </summary>
    public static List<Entity> CollectEnemiesInLine(EntityManager em, float3 origin, float3 forward,
                                                    float length, float width, TeamType casterTeam)
    {
        var result  = new List<Entity>();
        float halfW = width * 0.5f;
        float3 o    = new float3(origin.x, origin.y, 0f);
        float3 dir  = math.normalizesafe(new float3(forward.x, forward.y, 0f), new float3(1f, 0f, 0f));

        var query = MakeUnitQuery(em);
        var entities   = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (em.GetComponentData<UnitIdentityComponent>(entities[i]).Team == casterTeam) continue;

            float3 p    = new float3(transforms[i].Position.x, transforms[i].Position.y, 0f) - o;
            float  proj = math.dot(p, dir);                  // 참격선을 따라 얼마나 갔나
            if (proj < 0f || proj > length) continue;

            if (math.length(p - dir * proj) > halfW) continue;  // 참격선에서 벗어난 거리

            result.Add(entities[i]);
        }

        entities.Dispose();
        transforms.Dispose();
        query.Dispose();
        return result;
    }

    /// <summary>
    /// origin 에서 forward 방향으로 퍼지는 부채꼴(반경 range, 반각 halfAngleDeg) 안의 적을 모은다.
    /// 산탄처럼 "앞으로 퍼져 나가는" 판정에 쓴다.
    /// </summary>
    public static List<Entity> CollectEnemiesInCone(EntityManager em, float3 origin, float3 forward,
                                                    float range, float halfAngleDeg, TeamType casterTeam)
    {
        var result   = new List<Entity>();
        float rangeSq = range * range;
        float cosHalf = math.cos(math.radians(halfAngleDeg));
        float3 o      = new float3(origin.x, origin.y, 0f);
        float3 dir    = math.normalizesafe(new float3(forward.x, forward.y, 0f), new float3(1f, 0f, 0f));

        var query = MakeUnitQuery(em);
        var entities   = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            if (em.GetComponentData<UnitIdentityComponent>(entities[i]).Team == casterTeam) continue;

            float3 to     = new float3(transforms[i].Position.x, transforms[i].Position.y, 0f) - o;
            float  distSq = math.lengthsq(to);
            if (distSq > rangeSq || distSq < 0.0001f) continue;

            // 부채꼴 안쪽인지 — 코사인 비교가 acos 보다 싸다
            if (math.dot(dir, to * math.rsqrt(distSq)) < cosHalf) continue;

            result.Add(entities[i]);
        }

        entities.Dispose();
        transforms.Dispose();
        query.Dispose();
        return result;
    }

    /// <summary>casterTeam 과 같은 팀의 살아 있는 유닛 (버프·치유 대상).</summary>
    public static List<Entity> CollectAllies(EntityManager em, TeamType casterTeam)
    {
        var result = new List<Entity>();

        var query = MakeUnitQuery(em);
        var entities = query.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
            if (em.GetComponentData<UnitIdentityComponent>(entities[i]).Team == casterTeam)
                result.Add(entities[i]);

        entities.Dispose();
        query.Dispose();
        return result;
    }

    static EntityQuery MakeUnitQuery(EntityManager em) => em.CreateEntityQuery(new EntityQueryDesc
    {
        All  = new ComponentType[] { ComponentType.ReadOnly<UnitIdentityComponent>(),
                                     ComponentType.ReadOnly<LocalTransform>() },
        None = new ComponentType[] { typeof(DeadTag) },
    });

    // ── 타격 / 치유 ──────────────────────────────────────────

    public static void DealDamage(EntityManager em, Entity target, float damage,
                                  float3 knockDir, float knockForce, Entity attacker)
    {
        if (!em.Exists(target) || !em.HasBuffer<HitEventBufferElement>(target)) return;

        em.GetBuffer<HitEventBufferElement>(target).Add(new HitEventBufferElement
        {
            Damage         = damage,
            HitDirection   = math.normalizesafe(knockDir, new float3(1f, 0f, 0f)) * knockForce,
            AttackerEntity = attacker,
            Type           = HitType.Skill,
        });
    }

    public static void Heal(EntityManager em, Entity target, float amount, Entity source)
    {
        if (amount <= 0f || !em.Exists(target) || !em.HasBuffer<HealEventBufferElement>(target)) return;

        em.GetBuffer<HealEventBufferElement>(target).Add(new HealEventBufferElement
        {
            Amount       = amount,
            SourceEntity = source,
        });
    }

    // ── 위치 / 오브젝트 ──────────────────────────────────────

    public static Vector3 PositionOf(EntityManager em, Entity e)
    {
        if (!em.Exists(e) || !em.HasComponent<LocalTransform>(e)) return Vector3.zero;
        var p = em.GetComponentData<LocalTransform>(e).Position;
        return new Vector3(p.x, p.y, p.z);
    }

    /// <summary>
    /// 이펙트를 붙일 몸통 중심.
    ///
    /// ⚠ 낙인·착탄 이펙트를 PositionOf 에 붙이면 안 된다
    ///   LocalTransform 위치는 유닛의 **발밑(피벗)** 이다. 거기에 이펙트를 띄우면
    ///   적 몸에 찍힌 게 아니라 바닥에 깔린 것처럼 보인다 — "타겟에 정확히 안 붙는"
    ///   증상의 정체다. 스프라이트 경계 중앙을 쓰면 유닛 크기가 달라도 항상 몸통에 맞는다.
    ///   (연결된 GameObject 가 없는 엔티티는 발밑으로 되돌아간다)
    /// </summary>
    public static Vector3 BodyCenterOf(EntityManager em, Entity e)
    {
        var go = ObjectOf(em, e);
        if (go != null)
        {
            var r = go.GetComponentInChildren<Renderer>();
            if (r != null) return r.bounds.center;
        }

        return PositionOf(em, e);
    }

    /// <summary>엔티티에 연결된 GameObject (떨림 연출용). 풀 링크가 없으면 null.</summary>
    public static GameObject ObjectOf(EntityManager em, Entity e)
    {
        if (!em.Exists(e) || !em.HasComponent<UnitPoolLinkComponent>(e)) return null;
        return em.GetComponentObject<UnitPoolLinkComponent>(e)?.LinkedObject;
    }
}
