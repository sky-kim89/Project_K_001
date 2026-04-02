using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

// ============================================================
//  UnitMovementSystem.cs
//  이동 처리 시스템
//  - 목적지(진형 슬롯)를 향한 이동
//  - 넉백 처리
//  - MoveSpeed 는 StatComponent.Final[StatType.MoveSpeed] 에서 읽음
//  - Velocity 는 MovementComponent.Velocity 에 저장 (VelocityComponent 제거됨)
// ============================================================

namespace BattleGame.Units
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct UnitMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            new MoveToDestinationJob { DeltaTime = deltaTime }.ScheduleParallel();
            new KnockbackJob         { DeltaTime = deltaTime }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // 목적지 이동 Job
    // ──────────────────────────────────────────

    /// <summary>각 유닛을 진형 슬롯 위치로 이동시킨다.</summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct MoveToDestinationJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            ref LocalTransform      transform,
            ref MovementComponent   movement,
            ref UnitStateComponent  unitState,
            in  FormationSlotComponent slot,
            in  StatComponent       stat)
        {
            // 경직·사망 상태면 이동 불가
            if (unitState.Current == UnitState.Hit ||
                unitState.Current == UnitState.Dead)
            {
                movement.Velocity = float3.zero;
                return;
            }

            float3 destination   = slot.SlotPosition;
            float3 toDestination = destination - transform.Position;
            float  distSq        = math.lengthsq(toDestination);
            float  stoppingDistSq = movement.StoppingDistance * movement.StoppingDistance;

            if (distSq <= stoppingDistSq)
            {
                // 목적지 도착 — 이동 중지
                movement.Velocity = float3.zero;
                movement.IsMoving = false;

                if (unitState.Current == UnitState.Moving)
                    ChangeState(ref unitState, UnitState.Idle);
                return;
            }

            // MoveSpeed 는 StatFinal 에서 읽음
            float moveSpeed = stat.Final[StatType.MoveSpeed];

            float3 direction  = math.normalize(toDestination);
            movement.Velocity = direction * moveSpeed;

            transform.Position += movement.Velocity * DeltaTime;
            movement.IsMoving   = true;

            // 이동 방향으로 회전 (2D)
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
    // 넉백 처리 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct KnockbackJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
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

            // 넉백 이동 (감속 적용)
            const float KnockbackDrag = 8f;
            hitReaction.KnockbackVelocity = math.lerp(
                hitReaction.KnockbackVelocity,
                float3.zero,
                DeltaTime * KnockbackDrag);

            transform.Position += hitReaction.KnockbackVelocity * DeltaTime;
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        {
            s.Previous   = s.Current;
            s.Current    = next;
            s.StateTimer = 0f;
        }
    }
}
