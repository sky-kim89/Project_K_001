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
//    ① BuildSepGridJob          — 전체 유닛 위치를 셀 맵에 등록 (병렬)
//       Complete()              — 맵 완성 보장
//    ② SeparationJob           — 겹친 유닛끼리 서로 밀어냄 (병렬)
//    ③ KnightChargeInitiateJob — 돌진 개시/취소/사거리 판정 (병렬, LocalTransform read-only)
//    ④ KnightChargeMoveJob     — 돌진 고속 이동 (병렬, LocalTransform writeable)
//    ⑤ MoveToDestinationJob    — 목적지로 이동 (병렬)
//    ⑥ KnockbackJob            — 넉백 처리 (병렬)
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
    [UpdateAfter(typeof(UnitTargetSearchSystem))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct UnitMovementSystem : ISystem
    {
        NativeParallelMultiHashMap<int2, SeparationEntry> _sepGrid;

        const float SepCellSize     = 1.0f;  // 그리드 셀 크기
        const float SepStrength     = 3.0f;  // 밀어내는 힘
        const float ChargeSpeedMult = 6.0f;  // 돌진 이동속도 배율

        public void OnCreate(ref SystemState state)
        {
            _sepGrid = new NativeParallelMultiHashMap<int2, SeparationEntry>(1024, Allocator.Persistent);
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

            // 화면에 진입한 적이 있는지 확인 — 아군 전진 개시 조건
            bool anyEnemyOnScreen = false;
            foreach (var (identity, screen) in
                SystemAPI.Query<RefRO<UnitIdentityComponent>, RefRO<ScreenStateComponent>>()
                         .WithNone<DeadTag>())
            {
                if (identity.ValueRO.Team == TeamType.Enemy && screen.ValueRO.HasEnteredScreen)
                {
                    anyEnemyOnScreen = true;
                    break;
                }
            }

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

            // ③ 기사 달인 돌진 개시/판정 (LocalTransform read-only → lookup aliasing 없음)
            new KnightChargeInitiateJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
                HealthLookup    = SystemAPI.GetComponentLookup<HealthComponent>(isReadOnly: true),
            }.ScheduleParallel();

            // ④ 기사 달인 돌진 이동 (LocalTransform writeable → lookup 없이 ChargeTargetPos 사용)
            new KnightChargeMoveJob
            {
                DeltaTime       = deltaTime,
                ChargeSpeedMult = ChargeSpeedMult,
            }.ScheduleParallel();

            // ⑤ 목적지 이동 ────────────────────────────────────────
            var retreatFireLookup  = SystemAPI.GetComponentLookup<TraitRetreatFireTag>(isReadOnly: true);
            new MoveToDestinationJob
            {
                DeltaTime          = deltaTime,
                AllyDefeated       = allyDefeated,
                EnemyDefeated      = enemyDefeated,
                AnyEnemyOnScreen   = anyEnemyOnScreen,
                RetreatFireLookup  = retreatFireLookup,
            }.ScheduleParallel();

            // ⑥ 넉백 ─────────────────────────────────────────────
            new KnockbackJob { DeltaTime = deltaTime }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // 기사 달인 돌진 — ① 개시/취소/사거리 판정 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// LocalTransform 을 읽기 전용(in)으로만 사용하므로 ComponentLookup&lt;LocalTransform&gt; 과 aliasing 없음.
    /// 타겟 위치를 ChargeTargetPos 에 기록 → KnightChargeMoveJob 이 lookup 없이 이동에 활용.
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct KnightChargeInitiateJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform>  TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;

        public void Execute(
            ref KnightChargeComponent charge,
            ref AttackComponent       attack,
            ref MovementComponent     movement,
            ref UnitStateComponent    unitState,
            in  LocalTransform        transform,   // read-only → aliasing 없음
            in  StatComponent         stat)
        {
            // 피격/사망 중 돌진 억제
            if (unitState.Current == UnitState.Hit || unitState.Current == UnitState.Dead)
            {
                if (charge.IsCharging)
                    CancelCharge(ref charge, ref unitState, ref movement);
                return;
            }

            // ① 돌진 개시
            if (!charge.IsCharging && charge.CooldownTimer <= 0f
                && attack.HasTarget
                && HealthLookup.HasComponent(attack.TargetEntity)
                && HealthLookup[attack.TargetEntity].CurrentHp > 0f)
            {
                charge.IsCharging   = true;
                charge.ChargeTarget = attack.TargetEntity;
                ChangeState(ref unitState, UnitState.Charging);
            }

            if (!charge.IsCharging) return;

            // ② 타겟 생존 + 위치 갱신 (MoveJob 이 lookup 없이 읽는다)
            if (!HealthLookup.HasComponent(charge.ChargeTarget)
                || HealthLookup[charge.ChargeTarget].CurrentHp <= 0f
                || !TransformLookup.HasComponent(charge.ChargeTarget))
            {
                CancelCharge(ref charge, ref unitState, ref movement);
                return;
            }

            charge.ChargeTargetPos = TransformLookup[charge.ChargeTarget].Position;

            // ③ 사거리 도달 → 돌진 완료, MeleeAttackJob 이 3배 피해 적용
            float3 toTarget = charge.ChargeTargetPos - transform.Position;
            float  dist     = math.length(toTarget);
            float  atkRange = stat.Final[StatType.AttackRange];

            if (dist <= atkRange)
            {
                charge.IsCharging     = false;
                attack.AttackCooldown = 0f;  // 즉시 공격 강제 — 남은 쿨다운이 있으면 재돌진이 먼저 들어가 3배 피해 누락됨
                ChangeState(ref unitState, UnitState.Attacking);
                movement.Velocity = float3.zero;
                movement.IsMoving = false;
            }
        }

        static void CancelCharge(
            ref KnightChargeComponent charge,
            ref UnitStateComponent    unitState,
            ref MovementComponent     movement)
        {
            charge.IsCharging    = false;
            charge.ChargeTarget  = Entity.Null;
            charge.CooldownTimer = charge.CooldownMax;
            ChangeState(ref unitState, UnitState.Idle);
            movement.Velocity = float3.zero;
            movement.IsMoving = false;
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        { s.Previous = s.Current; s.Current = next; s.StateTimer = 0f; }
    }

    // ──────────────────────────────────────────
    // 기사 달인 돌진 — ② 고속 이동 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// LocalTransform 을 쓰기(ref)로 사용하므로 ComponentLookup&lt;LocalTransform&gt; 보유 불가.
    /// InitiateJob 이 기록한 ChargeTargetPos 를 읽어 lookup 없이 이동한다.
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct KnightChargeMoveJob : IJobEntity
    {
        public float DeltaTime;
        public float ChargeSpeedMult;

        public void Execute(
            in  KnightChargeComponent charge,
            ref MovementComponent     movement,
            ref LocalTransform        transform,   // writeable → ComponentLookup<LocalTransform> 보유 불가
            in  StatComponent         stat)
        {
            if (!charge.IsCharging) return;

            float3 toTarget = charge.ChargeTargetPos - transform.Position;
            float  dist     = math.length(toTarget);
            if (dist < 0.01f) return;

            float  speed = stat.Final[StatType.MoveSpeed] * ChargeSpeedMult;
            float3 dir   = toTarget / dist;
            movement.Velocity   = dir * speed;
            transform.Position += movement.Velocity * DeltaTime;
            movement.IsMoving   = true;
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
    [WithNone(typeof(DeadTag), typeof(SkillCastLock))]
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
                // 공격 중·돌진 중 밀림 감쇠 — 돌진 궤도 유지
                bool reduceSep = unitState.Current == UnitState.Attacking
                              || unitState.Current == UnitState.Charging;
                float scale = reduceSep ? AttackingSepScale : 1f;
                transform.Position += push * (DeltaTime * scale);
            }
        }
    }

    // ──────────────────────────────────────────
    // ③ 목적지 이동 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag), typeof(SkillCastLock))]
    public partial struct MoveToDestinationJob : IJobEntity
    {
        public float DeltaTime;
        public bool  AllyDefeated;
        public bool  EnemyDefeated;
        public bool  AnyEnemyOnScreen;
        [ReadOnly] public ComponentLookup<TraitRetreatFireTag> RetreatFireLookup;

        public void Execute(
            Entity                     entity,
            ref LocalTransform         transform,
            ref MovementComponent      movement,
            ref UnitStateComponent     unitState,
            in  FormationSlotComponent slot,
            in  AttackComponent        attack,
            in  UnitIdentityComponent  identity,
            in  StatComponent          stat)
        {
            // 스폰 대기 중 — 이동 억제
            if (movement.MoveDelay > 0f)
            {
                movement.MoveDelay -= DeltaTime;
                movement.Velocity   = float3.zero;
                movement.IsMoving   = false;
                return;
            }

            if (unitState.Current == UnitState.Hit  ||
                unitState.Current == UnitState.Dead)
            {
                movement.Velocity = float3.zero;
                movement.IsMoving = false;
                return;
            }

            // 돌진 중 — KnightChargeJob 이 이동·속도 담당
            if (unitState.Current == UnitState.Charging) return;

            // ⚠ 보스 돌진은 여기서 처리하지 않는다
            //   돌진이 ActiveSkillId.BossCharge 스킬로 옮겨가면서
            //   BossChargeRunner 가 transform 을 직접 제어한다.
            //   시전 중에는 SkillCastLock 이 붙어 이 잡 자체가 안 돈다.

            // 공격 상태 전환 처리
            if (unitState.Current == UnitState.Attacking)
            {
                if (attack.HasTarget)
                {
                    if (attack.AttackCooldown > 0f)
                    {
                        // A7 퇴각 사격: 타겟이 사거리 절반 이내면 후퇴 이동 (공격 상태 유지)
                        if (RetreatFireLookup.HasComponent(entity))
                        {
                            float r     = stat.Final[StatType.AttackRange];
                            float halfSq = r * r * 0.25f;
                            if (math.distancesq(transform.Position, attack.TargetPosition) < halfSq)
                            {
                                float3 retreatDir   = math.normalizesafe(transform.Position - attack.TargetPosition);
                                movement.Velocity   = retreatDir * stat.Final[StatType.MoveSpeed];
                                transform.Position += movement.Velocity * DeltaTime;
                                movement.IsMoving   = true;
                                return;
                            }
                        }
                        movement.Velocity = float3.zero;
                        movement.IsMoving = false;
                        return;
                    }
                    // 쿨다운 만료: 사거리 밖이면 즉시 추격 전환 (타겟 사망 후 새 타겟 배정 시 멈춤 방지)
                    float atkRng      = stat.Final[StatType.AttackRange];
                    float toTargetSq  = math.distancesq(transform.Position, attack.TargetPosition);
                    if (toTargetSq > atkRng * atkRng)
                        ChangeState(ref unitState, UnitState.Chasing);
                    else
                    {
                        movement.Velocity = float3.zero;
                        movement.IsMoving = false;
                        return;
                    }
                }
                else
                {
                    ChangeState(ref unitState, UnitState.Idle);
                }
            }

            float moveSpeed = stat.Final[StatType.MoveSpeed];

            // 아군 + 적 전멸 → 제자리 정지 (승리 후 몰림 방지)
            if (identity.Team == TeamType.Ally && EnemyDefeated)
            {
                movement.Velocity = float3.zero;
                movement.IsMoving = false;
                if (unitState.Current != UnitState.Idle)
                    ChangeState(ref unitState, UnitState.Idle);
                return;
            }

            // 아군 + 타겟 없음 → 적이 화면에 진입한 후에만 +X 전진
            if (identity.Team == TeamType.Ally && !attack.HasTarget)
            {
                if (!AnyEnemyOnScreen)
                {
                    movement.Velocity = float3.zero;
                    movement.IsMoving = false;
                    if (unitState.Current != UnitState.Idle)
                        ChangeState(ref unitState, UnitState.Idle);
                    return;
                }
                movement.Velocity  = new float3(1f, 0f, 0f) * moveSpeed;
                transform.Position += movement.Velocity * DeltaTime;
                movement.IsMoving  = true;
                if (unitState.Current != UnitState.Moving)
                    ChangeState(ref unitState, UnitState.Moving);
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

            bool isChasing = unitState.Current == UnitState.Chasing && attack.HasTarget;

            // 타겟이 있지만 추격 상태가 아닌 경우 (피격 후 Idle 복귀 직후 등)
            // SlotPosition(0,0,0) 쪽으로 이동하면 Velocity.x < 0 이 되어
            // UnitAnimationSync 의 _lastFacingX 가 뒤집히므로 제자리 대기한다.
            if (attack.HasTarget && !isChasing)
            {
                movement.Velocity = float3.zero;
                movement.IsMoving = false;
                return;
            }

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
    /// 미진입 상태에서 화면 외곽 4칸 이상 벗어나면 즉시 사망 처리한다.
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

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ScreenClampJob
            {
                Min = new float2(camX - w, camY - h),
                Max = new float2(camX + w, camY + h),
                Ecb = ecb,

                // ⚠ 화면 밖 사망 판정은 웨이브가 도는 동안에만 켠다
                //   출전 대기 화면은 카메라를 옆으로 밀어 두므로 스폰 지점이 화면 밖으로
                //   빠질 수 있다. 그 상태에서 이 규칙이 살아 있으면 방금 세운 부대가
                //   미진입(HasEnteredScreen=false) 상태로 걸려 즉시 사망 → 1초 뒤 풀 반납된다.
                //   이 규칙의 목적은 '전투 중 넉백으로 화면 밖에 영구 방치되는 유닛 정리' 다.
                KillOutOfBounds = BattleManager.Instance != null
                               && BattleManager.Instance.IsWaveRunning,
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct ScreenClampJob : IJobEntity
    {
        public float2 Min;
        public float2 Max;
        public EntityCommandBuffer.ParallelWriter Ecb;

        /// <summary>화면 밖 사망 판정 사용 여부. 웨이브 진행 중에만 true.</summary>
        public bool KillOutOfBounds;

        // 스폰 후 화면 미진입 유닛이 이 거리 이상 벗어나면 즉시 사망 처리
        // — 대형 넉백으로 타겟 탐색 범위 밖에 영구 방치되는 버그 방지
        const float OutOfBoundsKillDist = 4f;

        public void Execute(
            [ChunkIndexInQuery] int    chunkIndex,
            Entity                     entity,
            ref LocalTransform         transform,
            ref ScreenStateComponent   screen,
            ref HealthComponent        health)
        {
            float x = transform.Position.x;
            float y = transform.Position.y;

            // 화면 진입 감지
            if (!screen.HasEnteredScreen &&
                x >= Min.x && x <= Max.x &&
                y >= Min.y && y <= Max.y)
            {
                screen.HasEnteredScreen = true;
            }

            if (screen.HasEnteredScreen)
            {
                // 진입 후: 화면 밖으로 나가지 않도록 엄격히 클램프
                transform.Position.x = math.clamp(x, Min.x, Max.x);
                transform.Position.y = math.clamp(y, Min.y, Max.y);
            }
            else if (KillOutOfBounds)
            {
                // 미진입 상태에서 허용 범위 초과 시 즉시 사망 처리 (웨이브 중에만)
                bool outX = x < Min.x - OutOfBoundsKillDist || x > Max.x + OutOfBoundsKillDist;
                bool outY = y < Min.y - OutOfBoundsKillDist || y > Max.y + OutOfBoundsKillDist;
                if (outX || outY)
                {
                    health.CurrentHp = 0f;
                    Ecb.AddComponent<DeadTag>(chunkIndex, entity);
                }
            }
        }
    }
}
