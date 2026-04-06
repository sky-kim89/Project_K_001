using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

// ============================================================
//  UnitMovementSystem.cs
//  이동 + 유닛 간 분리(Separation) 처리 시스템
//
//  분리 대상: 팀 무관 — 아군/적군 모두 포함 (공격 중 겹침 방지)
//
//  실행 순서 (매 프레임):
//    ① BuildSepGridJob      — 전체 유닛 위치를 셀 맵에 등록 (병렬)
//       Complete()          — 맵 완성 보장
//    ② SeparationJob       — 겹친 유닛끼리 서로 밀어냄 (병렬)
//    ③ MoveToDestinationJob — 목적지로 이동 (병렬)
//    ④ KnockbackJob        — 넉백 처리 (병렬)
//
//  분리 성능:
//    셀 크기 1.0f, 3×3 인접 셀 탐색 → 유닛당 평균 비교 4~8회
//    Burst 병렬 처리 → 200유닛 기준 무시 가능한 오버헤드
// ============================================================

namespace BattleGame.Units
{
    public struct SeparationEntry
    {
        public Entity Entity;
        public float3 Position;
        public float  Radius;   // GameObject.transform.localScale 기반 반경
        public float  Mass;     // 분리 질량 (General = 5, 나머지 = 1)
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct UnitMovementSystem : ISystem
    {
        NativeParallelMultiHashMap<int2, SeparationEntry> _sepGrid;

        const float SepCellSize = 1.0f;  // 그리드 셀 크기
        const float SepStrength = 3.0f;  // 밀어내는 힘

        public void OnCreate(ref SystemState state)
        {
            _sepGrid = new NativeParallelMultiHashMap<int2, SeparationEntry>(
                1024, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            _sepGrid.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime     = SystemAPI.Time.DeltaTime;
            bool  allyDefeated  = BattleManager.Instance != null && BattleManager.Instance.IsAllyDefeated;
            bool  enemyDefeated = BattleManager.Instance != null && BattleManager.Instance.IsEnemyDefeated;

            // ① 분리 그리드 빌드 (아군 + 적군 전체) ─────────────
            _sepGrid.Clear();

            int unitCount = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithNone<DeadTag>()
                .Build()
                .CalculateEntityCount();

            if (_sepGrid.Capacity < unitCount * 2)
                _sepGrid.Capacity = unitCount * 2;

            new BuildSepGridJob
            {
                GridWriter = _sepGrid.AsParallelWriter(),
                CellSize   = SepCellSize,
            }.ScheduleParallel();

            state.Dependency.Complete(); // 그리드 완성 대기

            // ② 분리 (팀 무관 — 공격 중 겹침 포함) ──────────────
            new SeparationJob
            {
                Grid      = _sepGrid,
                DeltaTime = deltaTime,
                CellSize  = SepCellSize,
                Strength  = SepStrength,
            }.ScheduleParallel();

            // ③ 목적지 이동 ────────────────────────────────────────
            new MoveToDestinationJob
            {
                DeltaTime     = deltaTime,
                AllyDefeated  = allyDefeated,
                EnemyDefeated = enemyDefeated,
            }.ScheduleParallel();

            // ④ 넉백 ─────────────────────────────────────────────
            new KnockbackJob { DeltaTime = deltaTime }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // ① 분리 그리드 빌드 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct BuildSepGridJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, SeparationEntry>.ParallelWriter GridWriter;
        public float CellSize;

        public void Execute(Entity entity, in LocalTransform transform, in UnitSizeComponent size)
        {
            int2 cell = (int2)math.floor(transform.Position.xy / CellSize);
            GridWriter.Add(cell, new SeparationEntry
            {
                Entity   = entity,
                Position = transform.Position,
                Radius   = size.Radius,
                Mass     = size.Mass,
            });
        }
    }

    // ──────────────────────────────────────────
    // ② 분리 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct SeparationJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, SeparationEntry> Grid;
        public float DeltaTime;
        public float CellSize;
        public float Strength;

        // 공격 중 밀림 감쇠 — 대규모 전투에서 어택 무빙 방지
        const float AttackingSepScale = 0.1f;

        public void Execute(Entity entity, ref LocalTransform transform,
                            in UnitSizeComponent size, in UnitStateComponent unitState)
        {
            float  myRadius = size.Radius;
            float  myMass   = math.max(size.Mass, 0.01f);
            int2   myCell   = (int2)math.floor(transform.Position.xy / CellSize);
            float3 push     = float3.zero;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int2 cell = myCell + new int2(dx, dy);
                if (!Grid.TryGetFirstValue(cell, out SeparationEntry entry, out var it))
                    continue;

                do
                {
                    if (entry.Entity == entity) continue;

                    float  pushDist = myRadius + entry.Radius;
                    float3 diff     = transform.Position - entry.Position;
                    float  distSq   = math.lengthsq(diff);

                    if (distSq > 0.0001f && distSq < pushDist * pushDist)
                    {
                        float dist    = math.sqrt(distSq);
                        float overlap = pushDist - dist;

                        // 질량 기반 분리: 상대 질량이 클수록 나는 더 많이 밀림
                        // push 비율 = otherMass / (myMass + otherMass)
                        float otherMass  = math.max(entry.Mass, 0.01f);
                        float massRatio  = otherMass / (myMass + otherMass);
                        push += diff / dist * overlap * Strength * massRatio;
                    }
                }
                while (Grid.TryGetNextValue(out entry, ref it));
            }

            if (math.lengthsq(push) > 0f)
            {
                float scale = unitState.Current == UnitState.Attacking ? AttackingSepScale : 1f;
                transform.Position += push * (DeltaTime * scale);
            }
        }
    }

    // ──────────────────────────────────────────
    // ③ 목적지 이동 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct MoveToDestinationJob : IJobEntity
    {
        public float DeltaTime;
        public bool  AllyDefeated;   // 아군 전멸 시 적 진군 중지용
        public bool  EnemyDefeated;  // 적 전멸(웨이브 클리어) 시 아군 이동 중지용

        public void Execute(
            ref LocalTransform         transform,
            ref MovementComponent      movement,
            ref UnitStateComponent     unitState,
            in  FormationSlotComponent slot,
            in  AttackComponent        attack,
            in  UnitIdentityComponent  identity,
            in  StatComponent          stat)
        {
            if (unitState.Current == UnitState.Hit      ||
                unitState.Current == UnitState.Dead     ||
                unitState.Current == UnitState.Attacking)
            {
                movement.Velocity = float3.zero;
                movement.IsMoving = false;
                return;
            }

            float moveSpeed = stat.Final[StatType.MoveSpeed];

            // 아군 + 적 전멸 → 제자리 정지 (승리 후 (0,0) 몰림 방지)
            if (identity.Team == TeamType.Ally && EnemyDefeated)
            {
                movement.Velocity = float3.zero;
                movement.IsMoving = false;
                if (unitState.Current != UnitState.Idle)
                    ChangeState(ref unitState, UnitState.Idle);
                return;
            }

            // 적팀 + 타겟 없음 → 진군 or 정지
            if (identity.Team == TeamType.Enemy && !attack.HasTarget)
            {
                // 아군 전멸 시 제자리 정지
                if (AllyDefeated)
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = false;
                    if (unitState.Current != UnitState.Idle)
                        ChangeState(ref unitState, UnitState.Idle);
                    return;
                }

                movement.Velocity  = new float3(-1f, 0f, 0f) * moveSpeed;
                transform.Position += movement.Velocity * DeltaTime;
                movement.IsMoving  = true;

                if (unitState.Current != UnitState.Moving)
                    ChangeState(ref unitState, UnitState.Moving);
                return;
            }

            bool   isChasing  = unitState.Current == UnitState.Chasing && attack.HasTarget;
            float3 destination = isChasing ? attack.TargetPosition : slot.SlotPosition;

            // 추격 중에는 공격 사거리를 정지 거리로 사용 (Archer·Mage 근접 방지)
            float stoppingDist    = isChasing ? stat.Final[StatType.AttackRange] : movement.StoppingDistance;
            float3 toDestination  = destination - transform.Position;
            float  distSq         = math.lengthsq(toDestination);
            float  stoppingDistSq = stoppingDist * stoppingDist;

            if (distSq <= stoppingDistSq)
            {
                movement.Velocity = float3.zero;
                movement.IsMoving = false;

                if (unitState.Current == UnitState.Moving)
                    ChangeState(ref unitState, UnitState.Idle);
                return;
            }

            float3 direction  = math.normalize(toDestination);
            movement.Velocity = direction * moveSpeed;
            transform.Position += movement.Velocity * DeltaTime;
            movement.IsMoving   = true;

            if (math.lengthsq(movement.Velocity) > 0.001f)
            {
                float angle = math.atan2(movement.Velocity.y, movement.Velocity.x);
                transform.Rotation = quaternion.RotateZ(angle);
            }

            if (unitState.Current == UnitState.Idle)
                ChangeState(ref unitState, UnitState.Moving);
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        {
            s.Previous   = s.Current;
            s.Current    = next;
            s.StateTimer = 0f;
        }
    }

    // ──────────────────────────────────────────
    // ④ 넉백 처리 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct KnockbackJob : IJobEntity
    {
        public float DeltaTime;

        public readonly void Execute(
            ref LocalTransform       transform,
            ref HitReactionComponent hitReaction,
            ref UnitStateComponent   unitState)
        {
            if (!hitReaction.IsStunned) return;

            hitReaction.StunTimer -= DeltaTime;

            if (hitReaction.StunTimer <= 0f)
            {
                hitReaction.IsStunned         = false;
                hitReaction.KnockbackVelocity = float3.zero;
                ChangeState(ref unitState, UnitState.Idle);
                return;
            }

            const float KnockbackDrag = 8f;
            hitReaction.KnockbackVelocity = math.lerp(
                hitReaction.KnockbackVelocity, float3.zero, DeltaTime * KnockbackDrag);

            transform.Position += hitReaction.KnockbackVelocity * DeltaTime;
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        {
            s.Previous   = s.Current;
            s.Current    = next;
            s.StateTimer = 0f;
        }
    }

    // ──────────────────────────────────────────
    // ⑤ 화면 경계 클램프 System + Job
    // ──────────────────────────────────────────

    /// <summary>
    /// 이동·분리·넉백이 모두 끝난 뒤 실행.
    /// 유닛이 화면에 한 번이라도 진입하면 이후로는 화면 밖으로 밀리지 않는다.
    /// Camera.main 이 없거나 Perspective 카메라면 동작하지 않는다.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMovementSystem))]
    public partial struct ScreenClampSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic) return;

            float h    = cam.orthographicSize;
            float w    = h * cam.aspect;
            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            new ScreenClampJob
            {
                Min = new float2(camX - w, camY - h),
                Max = new float2(camX + w, camY + h),
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct ScreenClampJob : IJobEntity
    {
        public float2 Min;
        public float2 Max;

        public void Execute(ref LocalTransform transform, ref ScreenStateComponent screen)
        {
            float x = transform.Position.x;
            float y = transform.Position.y;

            // 화면 안에 들어왔는지 확인
            if (!screen.HasEnteredScreen &&
                x >= Min.x && x <= Max.x &&
                y >= Min.y && y <= Max.y)
            {
                screen.HasEnteredScreen = true;
            }

            // 진입 후에는 화면 밖으로 나가지 않도록 클램프
            if (screen.HasEnteredScreen)
            {
                transform.Position.x = math.clamp(x, Min.x, Max.x);
                transform.Position.y = math.clamp(y, Min.y, Max.y);
            }
        }
    }
}
