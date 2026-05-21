using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ChargeSoldierRunner.cs — 돌격 병사 이동·충돌 처리기
//
//  ■ 동작 순서
//    1. 스폰 위치에서 BaseEffect 재생
//    2. chargeDir 방향으로 chargeSpeed 로 이동
//    3. 매 프레임 hitRadius 안의 적에게 스킬 피해 + 넉백 (적 1체당 1회)
//    4. maxDistance 도달 시 자기 자신(GO) 파괴
// ============================================================

public class ChargeSoldierRunner : MonoBehaviour
{
    EntityQuery _query;

    void OnDestroy()
    {
        if (_query != default(EntityQuery))
            _query.Dispose();
    }

    public void Launch(
        Vector3           spawnPos,
        Vector3           chargeDir,
        float             chargeSpeed,
        float             hitRadius,
        float             maxDistance,
        float             damage,
        float             knockbackForce,
        TeamType          casterTeam,
        Entity            casterEntity,
        EntityManager     em,
        SkillEffectConfig fx)
    {
        _query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[]
            {
                ComponentType.ReadOnly<UnitIdentityComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
            },
            None = new ComponentType[] { typeof(DeadTag) },
        });

        StartCoroutine(Sequence(
            spawnPos, chargeDir, chargeSpeed, hitRadius, maxDistance,
            damage, knockbackForce, casterTeam, casterEntity, em, fx));
    }

    IEnumerator Sequence(
        Vector3           startPos,
        Vector3           chargeDir,
        float             chargeSpeed,
        float             hitRadius,
        float             maxDistance,
        float             damage,
        float             knockbackForce,
        TeamType          casterTeam,
        Entity            casterEntity,
        EntityManager     em,
        SkillEffectConfig fx)
    {
        SkillEffectHelper.SpawnBase(fx.BaseEffectKey, startPos, fx.DespawnDelay);

        var   hitSet    = new HashSet<Entity>();
        float traveled  = 0f;
        float radiusSq  = hitRadius * hitRadius;
        var   knockDir3 = new float3(chargeDir.x, chargeDir.y, 0f);

        while (traveled < maxDistance)
        {
            float step = chargeSpeed * Time.deltaTime;
            transform.position += chargeDir * step;
            traveled           += step;

            em.CompleteAllTrackedJobs();

            // 반경 내 적 탐색
            var entities   = _query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var transforms = _query.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
            var identities = _query.ToComponentDataArray<UnitIdentityComponent>(Unity.Collections.Allocator.Temp);

            Vector3 myPos = transform.position;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];

                if (identities[i].Team == casterTeam) continue;  // 아군 제외
                if (hitSet.Contains(e))                continue;  // 이미 피격

                float dx = transforms[i].Position.x - myPos.x;
                float dy = transforms[i].Position.y - myPos.y;
                if (dx * dx + dy * dy > radiusSq)     continue;

                if (!em.HasBuffer<HitEventBufferElement>(e)) continue;

                em.GetBuffer<HitEventBufferElement>(e).Add(new HitEventBufferElement
                {
                    Damage         = damage,
                    HitDirection   = knockDir3 * knockbackForce,
                    AttackerEntity = casterEntity,
                    Type           = HitType.Skill,
                });

                SkillEffectHelper.SpawnTarget(
                    fx.TargetEffectKey,
                    new Vector3(transforms[i].Position.x, transforms[i].Position.y, 0f),
                    fx.DespawnDelay);

                hitSet.Add(e);
            }

            entities.Dispose();
            transforms.Dispose();
            identities.Dispose();

            yield return null;
        }

        Destroy(gameObject);
    }
}
