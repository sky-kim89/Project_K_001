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
        ComponentLookup<LocalTransform>       _transformLookup;
        ComponentLookup<HealthComponent>      _healthLookup;
        ComponentLookup<DoubleStrikeTag>      _doubleStrikeLookup;
        ComponentLookup<KnightChargeComponent> _chargeLookup;
        ComponentLookup<GeneralComponent>     _generalLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup    = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            _healthLookup       = state.GetComponentLookup<HealthComponent>(isReadOnly: true);
            _doubleStrikeLookup = state.GetComponentLookup<DoubleStrikeTag>(isReadOnly: true);
            _chargeLookup       = state.GetComponentLookup<KnightChargeComponent>(isReadOnly: true);
            _generalLookup      = state.GetComponentLookup<GeneralComponent>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            _transformLookup.Update(ref state);
            _healthLookup.Update(ref state);
            _doubleStrikeLookup.Update(ref state);
            _chargeLookup.Update(ref state);
            _generalLookup.Update(ref state);

            // ① 쿨다운 감소 (병렬, 근거리 + 원거리 + 기사 돌진)
            new CooldownTickJob { DeltaTime = deltaTime }.ScheduleParallel();
            new KnightChargeCooldownJob { DeltaTime = deltaTime }.ScheduleParallel();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // ② 근거리 공격 — 타겟 HitEventBuffer + (장군이면) AttackHitEvent 에 직접 추가
            new MeleeAttackJob
            {
                TransformLookup    = _transformLookup,
                HealthLookup       = _healthLookup,
                DoubleStrikeLookup = _doubleStrikeLookup,
                ChargeLookup       = _chargeLookup,
                GeneralLookup      = _generalLookup,
                Ecb                = ecb,
            }.ScheduleParallel();

            // ③ 원거리 공격 — 자신의 ProjectileLaunchRequest 버퍼에 추가
            new RangedAttackJob
            {
                TransformLookup    = _transformLookup,
                HealthLookup       = _healthLookup,
                DoubleStrikeLookup = _doubleStrikeLookup,
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
            attack.AttackedThisFrame = false;   // 매 프레임 초기화, 이후 AttackJob에서 설정
        }
    }

    // ──────────────────────────────────────────
    // 기사 달인 돌진 쿨다운 감소 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct KnightChargeCooldownJob : IJobEntity
    {
        public float DeltaTime;
        public void Execute(ref KnightChargeComponent charge)
        {
            if (charge.CooldownTimer > 0f)
                charge.CooldownTimer -= DeltaTime;
        }
    }

    // ──────────────────────────────────────────
    // 근거리 공격 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// RangedTag·BossComponent 없는 유닛(Knight, ShieldBearer, 일반 적)만 처리.
    /// 보스는 BossAttackSystem 이 별도로 처리한다 (AoE + 넉백 포함).
    /// 사거리 내 타겟이 있고 쿨다운 0 이면 HitEventBuffer 에 직접 추가.
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag), typeof(RangedTag), typeof(BossComponent))]
    public partial struct MeleeAttackJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform>        TransformLookup;
        [ReadOnly] public ComponentLookup<HealthComponent>       HealthLookup;
        [ReadOnly] public ComponentLookup<DoubleStrikeTag>       DoubleStrikeLookup;
        [ReadOnly] public ComponentLookup<KnightChargeComponent> ChargeLookup;
        [ReadOnly] public ComponentLookup<GeneralComponent>      GeneralLookup;
        public EntityCommandBuffer.ParallelWriter                 Ecb;

        public void Execute(
            [ChunkIndexInQuery] int chunkIndex,
            Entity                  entity,
            ref AttackComponent     attack,
            ref UnitStateComponent  unitState,
            in  LocalTransform      transform,
            in  StatComponent       stat,
            in  HealthComponent     health)
        {
            if (!attack.HasTarget || attack.AttackCooldown > 0f) return;
            if (unitState.Current == UnitState.Hit)      return;
            if (unitState.Current == UnitState.Charging) return;  // KnightChargeJob 이 이동 담당
            if (health.IsDoomed) return;                          // 사망 확정 — 이동만 한다
            if (!TransformLookup.HasComponent(attack.TargetEntity)) return;
            if (!HealthLookup.HasComponent(attack.TargetEntity))    return;

            // 이미 죽었거나(HP 0), 날아오는 발사체로 사망이 확정된 타겟은 놓는다 — 오버킬 방지
            var targetHealth = HealthLookup[attack.TargetEntity];
            if (targetHealth.CurrentHp <= 0f || targetHealth.IsDoomed) { attack.HasTarget = false; return; }

            float3 targetPos   = TransformLookup[attack.TargetEntity].Position;
            attack.TargetPosition = targetPos;
            float  attackRange = stat.Final[StatType.AttackRange];
            float  distSq      = math.distancesq(transform.Position, targetPos);

            if (distSq > attackRange * attackRange)
            {
                if (unitState.Current != UnitState.Chasing) ChangeState(ref unitState, UnitState.Chasing);
                return;
            }

            attack.AttackCooldown = 1f / stat.Final[StatType.AttackSpeed];
            ChangeState(ref unitState, UnitState.Attacking);

            float finalDamage = RollDamage(ref attack, in stat);

            // 기사 달인 — 돌진 완료(타이머=0, 비돌진 상태) 이면 300% 피해, 쿨다운 리셋
            bool chargeReady = ChargeLookup.HasComponent(entity)
                            && ChargeLookup[entity].CooldownTimer <= 0f
                            && !ChargeLookup[entity].IsCharging;
            if (chargeReady)
            {
                finalDamage *= 3f;
                var chargeData = ChargeLookup[entity];
                Ecb.SetComponent(chunkIndex, entity, new KnightChargeComponent
                {
                    CooldownTimer    = chargeData.CooldownMax,
                    CooldownMax      = chargeData.CooldownMax,
                    IsCharging       = false,
                    ChargeTarget     = Entity.Null,
                    ChargeTargetPos  = float3.zero,
                });
            }

            float3 hitDir = math.normalize(targetPos - transform.Position);

            attack.AttackedThisFrame = true;
            attack.LastDamageDealt   = finalDamage;

            int hitCount = DoubleStrikeLookup.HasComponent(entity) ? 2 : 1;
            for (int h = 0; h < hitCount; h++)
                Ecb.AppendToBuffer(chunkIndex, attack.TargetEntity, new HitEventBufferElement
                {
                    Damage         = finalDamage,
                    HitDirection   = hitDir,
                    AttackerEntity = entity,
                });

            // 장군의 근거리 공격 착탄 — OnAttackLanded 트리거용 (ECB → 다음 프레임 CombatTriggerSystem)
            if (GeneralLookup.HasComponent(entity))
                Ecb.AppendToBuffer(chunkIndex, entity, new AttackHitEvent
                {
                    TargetEntity = attack.TargetEntity,
                    TargetPos    = targetPos,
                    Damage       = finalDamage,
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
        [ReadOnly] public ComponentLookup<DoubleStrikeTag> DoubleStrikeLookup;

        const float ArrowSpeed     = 15f;
        const float MagicBoltSpeed = 10f;

        public void Execute(
            Entity                                 entity,
            ref AttackComponent                    attack,
            ref UnitStateComponent                 unitState,
            ref DynamicBuffer<ProjectileLaunchRequest> launchBuffer,
            in  LocalTransform                     transform,
            in  StatComponent                      stat,
            in  UnitIdentityComponent              identity,
            in  UnitJobComponent                   jobComp,
            in  HealthComponent                    health)
        {
            if (!attack.HasTarget || attack.AttackCooldown > 0f) return;
            if (unitState.Current == UnitState.Hit) return;  // 속박/스턴 중 공격 불가
            if (health.IsDoomed) return;                     // 사망 확정 — 이동만 한다
            if (!TransformLookup.HasComponent(attack.TargetEntity)) return;
            if (!HealthLookup.HasComponent(attack.TargetEntity))    return;

            // 이미 죽었거나, 날아가는 발사체로 사망이 확정된 타겟은 놓는다.
            // 한 명에게 화살이 몰려 낭비되는 것을 막는 핵심 분기.
            var targetHealth = HealthLookup[attack.TargetEntity];
            if (targetHealth.CurrentHp <= 0f || targetHealth.IsDoomed) { attack.HasTarget = false; return; }

            float3 targetPos   = TransformLookup[attack.TargetEntity].Position;
            attack.TargetPosition = targetPos;  // 이동 시스템에 항상 최신 위치 전달
            float  attackRange = stat.Final[StatType.AttackRange];
            float  distSq      = math.distancesq(transform.Position, targetPos);

            if (distSq > attackRange * attackRange)
            {
                if (unitState.Current != UnitState.Chasing) ChangeState(ref unitState, UnitState.Chasing);
                return;
            }

            attack.AttackCooldown = 1f / stat.Final[StatType.AttackSpeed];
            ChangeState(ref unitState, UnitState.Attacking);

            float finalDamage = RollDamage(ref attack, in stat);

            attack.AttackedThisFrame = true;
            attack.LastDamageDealt   = finalDamage;

            int launchCount = DoubleStrikeLookup.HasComponent(entity) ? 2 : 1;
            for (int h = 0; h < launchCount; h++)
                launchBuffer.Add(new ProjectileLaunchRequest
                {
                    TargetEntity   = attack.TargetEntity,
                    AttackerEntity = entity,
                    AttackerPos    = transform.Position,
                    TargetPos      = targetPos,
                    Damage         = finalDamage,
                    Speed          = jobComp.Job == UnitJob.Archer ? ArrowSpeed : MagicBoltSpeed,
                    Team           = identity.Team,
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
