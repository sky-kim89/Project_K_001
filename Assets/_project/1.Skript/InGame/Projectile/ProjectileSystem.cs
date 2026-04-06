using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using BattleGame.Units;      // ProjectileLaunchRequest, RangedTag, UnitJobComponent, HitEventBufferElement
using BattleGame.Projectiles; // ProjectileComponent, ProjectileGoLink, ProjectileDestroyTag

// ============================================================
//  ProjectileSystem.cs
//  발사체 생성 · 이동 · 피격 · 소멸 시스템.
//
//  실행 순서 (SimulationSystemGroup 내):
//    ① ProjectileSpawnSystem   — 발사 요청 버퍼 처리 (non-Burst, managed 접근 필요)
//    ② ProjectileMovementSystem — 이동 + Lifetime 감소 (Burst 병렬)
//    ③ ProjectileHitSystem      — 피격 판정 (Burst 병렬)
//    ④ ProjectileDestroySystem  — GO 반납 (non-Burst, PoolController 접근 필요)
//
//  피격 흐름:
//    ProjectileHitJob → HitEventBuffer append (ECB) + ProjectileDestroyTag 추가
//    → UnitHitSystem 이 HitEvent 처리 (기존 로직 그대로)
// ============================================================

namespace BattleGame.Projectiles
{
    // ══════════════════════════════════════════════════════════
    // ① 발사체 스폰 — 발사 요청 버퍼 처리
    // ══════════════════════════════════════════════════════════

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ProjectileMovementSystem))]
    public partial struct ProjectileSpawnSystem : ISystem
    {
        const string ArrowPoolKey     = "Arrow";
        const string MagicBoltPoolKey = "MagicBolt";

        // ── 직업별 풀 키 ──────────────────────────────────────
        static string GetPoolKey(UnitJob job) => job switch
        {
            UnitJob.Mage => MagicBoltPoolKey,
            _            => ArrowPoolKey,
        };

        public readonly void OnUpdate(ref SystemState state)
        {
            if (PoolController.Instance == null) return;

            // 로컬 변수 — ISystem 은 unmanaged struct 이므로 List 필드 불가
            var pending = new List<(string poolKey, ProjectileLaunchRequest req)>();

            // ── 1단계: 반복 중 데이터만 수집 (구조 변경 없음) ────
            foreach (var (launchBuffer, jobComp) in
                SystemAPI.Query<DynamicBuffer<ProjectileLaunchRequest>, RefRO<UnitJobComponent>>()
                         .WithAll<RangedTag>())
            {
                if (launchBuffer.IsEmpty) continue;

                string poolKey = GetPoolKey(jobComp.ValueRO.Job);
                foreach (var req in launchBuffer)
                    pending.Add((poolKey, req));

                launchBuffer.Clear(); // 길이만 0으로 — 구조 변경 아님, 반복 중 안전
            }

            if (pending.Count == 0) return;

            // ── 2단계: 반복 완료 후 스폰 + ECS 구조 변경 ─────────
            state.EntityManager.CompleteAllTrackedJobs();

            foreach (var (poolKey, req) in pending)
            {
                var go = PoolController.Instance.Spawn(
                    PoolType.Projectile, poolKey,
                    new Vector3(req.AttackerPos.x, req.AttackerPos.y, req.AttackerPos.z),
                    Quaternion.identity);

                if (go == null)
                {
                    Debug.LogWarning($"[ProjectileSpawnSystem] 풀 스폰 실패: '{poolKey}'");
                    continue;
                }

                if (go.TryGetComponent<ProjectileView>(out var view))
                {
                    float arcHeight = poolKey == ArrowPoolKey ? 1.5f : 0f;
                    view.Launch(req, arcHeight);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    // ② 이동 + Lifetime 감소
    // ══════════════════════════════════════════════════════════

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileSpawnSystem))]
    public partial struct ProjectileMovementSystem : ISystem
    {
        ComponentLookup<LocalTransform>  _transformLookup;
        ComponentLookup<HealthComponent> _healthLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            _healthLookup    = state.GetComponentLookup<HealthComponent>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _healthLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProjectileMoveJob
            {
                TransformLookup = _transformLookup,
                HealthLookup    = _healthLookup,
                DeltaTime       = SystemAPI.Time.DeltaTime,
                Ecb             = ecb,
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(ProjectileDestroyTag))]
    public partial struct ProjectileMoveJob : IJobEntity
    {
        // 자신의 LocalTransform(쓰기)과 타겟의 LocalTransform(읽기)이 다른 entity 임을
        // 안전 시스템이 구분 못하므로 명시적으로 억제 — 실제 aliasing 없음
        [ReadOnly, NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform>  TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
        public float                                       DeltaTime;
        public EntityCommandBuffer.ParallelWriter          Ecb;

        public void Execute(
            [ChunkIndexInQuery] int    chunkIndex,
            Entity                     entity,
            ref ProjectileComponent    proj,
            ref LocalTransform         transform)
        {
            // Lifetime 감소 — 만료 시 소멸 요청
            proj.Lifetime -= DeltaTime;
            if (proj.Lifetime <= 0f)
            {
                Ecb.AddComponent<ProjectileDestroyTag>(chunkIndex, entity);
                return;
            }

            // 타겟 생존 확인 — 사망 즉시 Entity.Null 로 무효화
            // 이유: 유닛은 풀 반납 후 같은 Entity 로 재사용되므로,
            //       무효화하지 않으면 재스폰된 유닛을 계속 추적하는 버그 발생
            if (proj.TargetEntity != Entity.Null)
            {
                bool alive = TransformLookup.HasComponent(proj.TargetEntity) &&
                             HealthLookup.HasComponent(proj.TargetEntity)    &&
                             HealthLookup[proj.TargetEntity].CurrentHp > 0f;

                if (alive)
                {
                    proj.TargetPos = TransformLookup[proj.TargetEntity].Position;
                }
                else
                {
                    // TargetPos 는 이미 마지막 위치로 캐시됨 — Entity 만 무효화
                    proj.TargetEntity = Entity.Null;

                    // Arrow: 남은 거리 기반으로 TotalTime 재계산 (속도 일정하게 유지)
                    if (proj.ArcHeight > 0f)
                    {
                        float remaining = math.distance(transform.Position, proj.TargetPos);
                        proj.TotalTime  = proj.ElapsedTime +
                                          (proj.Speed > 0.0001f ? remaining / proj.Speed : 0.1f);
                    }
                }
            }

            if (proj.ArcHeight > 0f)
            {
                // ── Arrow: 포물선 이동 ─────────────────────────────
                // t=0(발사) → t=1(도달), sin 곡선으로 Y 오프셋 추가
                proj.ElapsedTime += DeltaTime;
                float t = proj.TotalTime > 0.0001f
                    ? math.saturate(proj.ElapsedTime / proj.TotalTime)
                    : 1f;

                float3 basePos = math.lerp(proj.StartPos, proj.TargetPos, t);
                basePos.y += proj.ArcHeight * math.sin(t * math.PI);
                transform.Position = basePos;
            }
            else
            {
                // ── MagicBolt: 직선 이동 ──────────────────────────
                float3 diff = proj.TargetPos - transform.Position;
                float  dist = math.length(diff);
                if (dist < 0.01f) return;

                float step = proj.Speed * DeltaTime;
                transform.Position += math.normalize(diff) * math.min(step, dist);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    // ③ 피격 판정
    // ══════════════════════════════════════════════════════════

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial struct ProjectileHitSystem : ISystem
    {
        ComponentLookup<LocalTransform>  _transformLookup;
        ComponentLookup<HealthComponent> _healthLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            _healthLookup    = state.GetComponentLookup<HealthComponent>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _healthLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProjectileHitJob
            {
                TransformLookup = _transformLookup,
                HealthLookup    = _healthLookup,
                Ecb             = ecb,
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(ProjectileDestroyTag))]
    public partial struct ProjectileHitJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform>  TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
        public EntityCommandBuffer.ParallelWriter          Ecb;

        const float HitRadiusSq = 0.5f * 0.5f;  // 타격 판정 반경

        public void Execute(
            [ChunkIndexInQuery] int  chunkIndex,
            Entity                   entity,
            in  ProjectileComponent  proj,
            in  LocalTransform       transform)
        {
            // 타겟 위치 결정 (생존 시 현재 위치, 사망 시 마지막 캐시)
            float3 targetPos;
            bool   targetAlive = false;

            // Entity.Null 체크 — MoveJob 이 사망 감지 시 이미 무효화했으므로
            // 재스폰된 유닛(같은 Entity ID)을 살아있는 타겟으로 오판하지 않음
            if (proj.TargetEntity != Entity.Null          &&
                TransformLookup.HasComponent(proj.TargetEntity) &&
                HealthLookup.HasComponent(proj.TargetEntity)    &&
                HealthLookup[proj.TargetEntity].CurrentHp > 0f)
            {
                targetPos   = TransformLookup[proj.TargetEntity].Position;
                targetAlive = true;
            }
            else
            {
                targetPos = proj.TargetPos;
            }

            // 도달 체크
            if (math.distancesq(transform.Position, targetPos) > HitRadiusSq) return;

            // 타겟 생존 시 피격 이벤트 등록 (기존 HitSystem 이 처리)
            if (targetAlive)
            {
                float3 hitDir = math.lengthsq(transform.Position - targetPos) > 0f
                    ? math.normalize(transform.Position - targetPos)
                    : float3.zero;

                Ecb.AppendToBuffer(chunkIndex, proj.TargetEntity, new HitEventBufferElement
                {
                    Damage         = proj.Damage,
                    HitDirection   = hitDir,
                    AttackerEntity = Entity.Null,
                });
            }

            // 소멸 요청
            Ecb.AddComponent<ProjectileDestroyTag>(chunkIndex, entity);
        }
    }

    // ══════════════════════════════════════════════════════════
    // ④ GO 반납 — ProjectileDestroyTag 처리
    // ══════════════════════════════════════════════════════════

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileHitSystem))]
    public partial struct ProjectileDestroySystem : ISystem
    {
        public readonly void OnUpdate(ref SystemState state)
        {
            if (PoolController.Instance == null) return;

            // ── 1단계: 반복 중 GO 목록만 수집 ────────────────────
            var toDestroy = new List<GameObject>();
            foreach (var goLink in
                SystemAPI.Query<ProjectileGoLink>()
                         .WithAll<ProjectileDestroyTag>())
            {
                if (goLink.Go != null && goLink.Go.activeSelf)
                    toDestroy.Add(goLink.Go);
            }

            if (toDestroy.Count == 0) return;

            // ── 2단계: 반복 완료 후 Despawn (EntityLink.OnDisable → Disabled 추가) ─
            state.EntityManager.CompleteAllTrackedJobs();
            foreach (var go in toDestroy)
                PoolController.Instance.Despawn(go);
        }
    }
}
