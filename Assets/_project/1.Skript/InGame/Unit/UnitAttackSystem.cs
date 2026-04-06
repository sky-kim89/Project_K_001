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
            float deltaTime = SystemAPI.Time.DeltaTime;

            _transformLookup.Update(ref state);
            _healthLookup.Update(ref state);

            // ① 쿨다운 감소 (병렬, 근거리 + 원거리 공통)
            new CooldownTickJob { DeltaTime = deltaTime }.ScheduleParallel();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // ② 근거리 공격 — 타겟 HitEventBuffer 에 직접 추가
            new MeleeAttackJob
            {
                TransformLookup = _transformLookup,
                HealthLookup    = _healthLookup,
                Ecb             = ecb,
            }.ScheduleParallel();

            // ③ 원거리 공격 — 자신의 ProjectileLaunchRequest 버퍼에 추가
            new RangedAttackJob
            {
                TransformLookup = _transformLookup,
                HealthLookup    = _healthLookup,
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
    // 근거리 공격 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// RangedTag 없는 유닛(Knight, ShieldBearer, 일반 적)만 처리.
    /// 사거리 내 타겟이 있고 쿨다운 0 이면 HitEventBuffer 에 직접 추가.
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag), typeof(RangedTag))]
    public partial struct MeleeAttackJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform>  TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
        public EntityCommandBuffer.ParallelWriter          Ecb;

        public void Execute(
            [ChunkIndexInQuery] int chunkIndex,
            Entity                  entity,
            ref AttackComponent     attack,
            ref UnitStateComponent  unitState,
            in  LocalTransform      transform,
            in  StatComponent       stat)
        {
            if (!attack.HasTarget || attack.AttackCooldown > 0f) return;
            if (!TransformLookup.HasComponent(attack.TargetEntity)) return;
            if (!HealthLookup.HasComponent(attack.TargetEntity))    return;
            if (HealthLookup[attack.TargetEntity].CurrentHp <= 0f) { attack.HasTarget = false; return; }

            float3 targetPos   = TransformLookup[attack.TargetEntity].Position;
            float  attackRange = stat.Final[StatType.AttackRange];

            if (math.distancesq(transform.Position, targetPos) > attackRange * attackRange)
            {
                if (unitState.Current != UnitState.Chasing) ChangeState(ref unitState, UnitState.Chasing);
                return;
            }

            attack.AttackCooldown = 1f / stat.Final[StatType.AttackSpeed];
            ChangeState(ref unitState, UnitState.Attacking);

            float finalDamage = RollDamage(ref attack, in stat);
            float3 hitDir     = math.normalize(targetPos - transform.Position);

            Ecb.AppendToBuffer(chunkIndex, attack.TargetEntity, new HitEventBufferElement
            {
                Damage         = finalDamage,
                HitDirection   = hitDir,
                AttackerEntity = entity,
            });
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        { s.Previous = s.Current; s.Current = next; s.StateTimer = 0f; }

        static float RollDamage(ref AttackComponent attack, in StatComponent stat)
        {
            var   rng   = new Random(attack.RandomSeed == 0u ? 1u : attack.RandomSeed);
            float roll  = rng.NextFloat();
            attack.RandomSeed = rng.state;
            float base_ = stat.Final[StatType.Attack];
            return roll < stat.Final[StatType.CritChance] ? base_ * stat.Final[StatType.CritDamage] : base_;
        }
    }

    // ──────────────────────────────────────────
    // 원거리 공격 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// RangedTag 유닛(Archer, Mage)만 처리.
    /// 사거리 내 타겟이 있고 쿨다운 0 이면 자신의 ProjectileLaunchRequest 버퍼에 추가.
    /// ProjectileSpawnSystem 이 같은 프레임에 버퍼를 읽어 발사체를 스폰한다.
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(RangedTag))]
    [WithNone(typeof(DeadTag))]
    public partial struct RangedAttackJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform>  TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;

        const float ProjectileSpeed = 10f;

        public void Execute(
            ref AttackComponent                    attack,
            ref UnitStateComponent                 unitState,
            ref DynamicBuffer<ProjectileLaunchRequest> launchBuffer,
            in  LocalTransform                     transform,
            in  StatComponent                      stat,
            in  UnitIdentityComponent              identity)
        {
            if (!attack.HasTarget || attack.AttackCooldown > 0f) return;
            if (!TransformLookup.HasComponent(attack.TargetEntity)) return;
            if (!HealthLookup.HasComponent(attack.TargetEntity))    return;
            if (HealthLookup[attack.TargetEntity].CurrentHp <= 0f) { attack.HasTarget = false; return; }

            float3 targetPos   = TransformLookup[attack.TargetEntity].Position;
            float  attackRange = stat.Final[StatType.AttackRange];

            if (math.distancesq(transform.Position, targetPos) > attackRange * attackRange)
            {
                if (unitState.Current != UnitState.Chasing) ChangeState(ref unitState, UnitState.Chasing);
                return;
            }

            attack.AttackCooldown = 1f / stat.Final[StatType.AttackSpeed];
            ChangeState(ref unitState, UnitState.Attacking);

            float finalDamage = RollDamage(ref attack, in stat);

            launchBuffer.Add(new ProjectileLaunchRequest
            {
                TargetEntity = attack.TargetEntity,
                AttackerPos  = transform.Position,
                TargetPos    = targetPos,
                Damage       = finalDamage,
                Speed        = ProjectileSpeed,
                Team         = identity.Team,
            });
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        { s.Previous = s.Current; s.Current = next; s.StateTimer = 0f; }

        static float RollDamage(ref AttackComponent attack, in StatComponent stat)
        {
            var   rng   = new Random(attack.RandomSeed == 0u ? 1u : attack.RandomSeed);
            float roll  = rng.NextFloat();
            attack.RandomSeed = rng.state;
            float base_ = stat.Final[StatType.Attack];
            return roll < stat.Final[StatType.CritChance] ? base_ * stat.Final[StatType.CritDamage] : base_;
        }
    }
}
