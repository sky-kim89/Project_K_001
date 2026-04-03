using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

// ============================================================
//  UnitAttackSystem.cs
//  공격 처리 시스템
//  - AttackDamage / AttackRange / AttackSpeed 는 StatComponent.Final 에서 읽음
//  - 타겟 생존 확인: HealthComponent.CurrentHp <= 0 체크
// ============================================================

namespace BattleGame.Units
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitAttackSystem : ISystem
    {
        ComponentLookup<LocalTransform> _transformLookup;
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
            float deltaTime = SystemAPI.Time.DeltaTime;

            _transformLookup.Update(ref state);
            _healthLookup.Update(ref state);

            // ① 쿨다운 감소 (병렬)
            new CooldownTickJob { DeltaTime = deltaTime }.ScheduleParallel();

            // ② 공격 판정 (병렬)
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new AttackExecuteJob
            {
                TransformLookup = _transformLookup,
                HealthLookup    = _healthLookup,
                Ecb             = ecb
            }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // 쿨다운 감소 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct CooldownTickJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref AttackComponent attack)
        {
            if (attack.AttackCooldown > 0f)
                attack.AttackCooldown -= DeltaTime;
        }
    }

    // ──────────────────────────────────────────
    // 공격 실행 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// 사거리 내 타겟이 있고 쿨다운이 0 이면 타겟의 HitEventBuffer 에 이벤트를 예약한다.
    /// 공격력 / 사거리 / 공격속도 는 StatComponent.Final 에서 읽는다.
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct AttackExecuteJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform>  TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(
            [ChunkIndexInQuery] int  chunkIndex,
            Entity                   entity,
            ref AttackComponent      attack,
            ref UnitStateComponent   unitState,
            in  LocalTransform       transform,
            in  StatComponent        stat)           // ← 스텟은 StatFinal 에서 읽음
        {
            if (!attack.HasTarget)          return;
            if (attack.AttackCooldown > 0f) return;

            if (!TransformLookup.HasComponent(attack.TargetEntity)) return;
            if (!HealthLookup.HasComponent(attack.TargetEntity))    return;

            // 타겟 생존 확인 (CurrentHp 로 직접 체크)
            HealthComponent targetHealth = HealthLookup[attack.TargetEntity];
            if (targetHealth.CurrentHp <= 0f)
            {
                attack.HasTarget = false;
                return;
            }

            // 사거리 체크 (AttackRange → StatFinal)
            float3 targetPos = TransformLookup[attack.TargetEntity].Position;
            float  distSq    = math.distancesq(transform.Position, targetPos);
            float  attackRange = stat.Final[StatType.AttackRange];

            if (distSq > attackRange * attackRange)
            {
                if (unitState.Current == UnitState.Attacking)
                    ChangeState(ref unitState, UnitState.Chasing);
                return;
            }

            // 공격 실행 (AttackSpeed / AttackDamage → StatFinal)
            attack.AttackCooldown = 1f / stat.Final[StatType.AttackSpeed];
            ChangeState(ref unitState, UnitState.Attacking);

            // 크리티컬 판정 (CritChance / CritDamage → StatFinal)
            var   rng       = new Random(attack.RandomSeed == 0u ? 1u : attack.RandomSeed);
            float roll      = rng.NextFloat();
            attack.RandomSeed = rng.state; // 다음 공격을 위해 시드 갱신

            float baseDamage = stat.Final[StatType.Attack];
            float finalDamage = roll < stat.Final[StatType.CritChance]
                ? baseDamage * stat.Final[StatType.CritDamage]
                : baseDamage;

            float3 toTarget = targetPos - transform.Position;
            float3 hitDir   = math.lengthsq(toTarget) > 0f ? math.normalize(toTarget) : float3.zero;

            Ecb.AppendToBuffer(chunkIndex, attack.TargetEntity, new HitEventBufferElement
            {
                Damage         = finalDamage,
                HitDirection   = hitDir,
                AttackerEntity = entity,
            });
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        {
            s.Previous   = s.Current;
            s.Current    = next;
            s.StateTimer = 0f;
        }
    }
}
