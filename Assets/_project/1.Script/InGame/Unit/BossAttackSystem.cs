using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

// ============================================================
//  BossAttackSystem.cs
//  보스 전용 공격 시스템 (MeleeAttackJob 에서 제외됨).
//
//  ■ 기능
//    1. 쿨다운 0 + 타겟 있음 → 타겟에 직접 피해
//    2. AoeRadius * 보스 크기 비율 범위 내 적팀 유닛에 스플래시 피해
//    3. 피격 유닛에게 넉백 적용
//    4. 돌진 중(IsCharging)이면 ChargeDamageBonus 만큼 공격력 증가
//
//  ■ AoE 반경 스케일
//    effectiveAoeRadius = boss.AoeRadius * (bossRadius / ReferenceRadius)
//    ReferenceRadius = 0.5f (기준 크기 — 일반 유닛 반경)
//
//  ■ 실행 순서
//    UnitAttackSystem(CooldownTickJob) → 쿨다운 감소
//    BossAttackSystem                  → 보스 공격 (이후)
//    UnitHitSystem                     → HitEventBuffer 처리
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial class BossAttackSystem : SystemBase
    {
        // 일반 유닛 기준 반경 — 보스 크기 배율 계산 기준값
        const float ReferenceRadius = 0.5f;

        struct BossAttackInfo
        {
            public float3   BossPosition;
            public float3   TargetPosition;
            public Entity   TargetEntity;
            public float    Damage;
            public float    EffectiveAoeRadius;   // 크기 보정된 AoE 반경
            public float    AoeSplashRatio;
            public float    KnockbackForce;
            public float    KnockbackDuration;
            public TeamType AttackerTeam;
        }

        // AoE 대상 후보 (ForEach 밖에서 미리 수집)
        struct AoeCandidate
        {
            public Entity   Entity;
            public float3   Position;
            public TeamType Team;
        }

        EntityQuery _aoeQuery;

        protected override void OnCreate()
        {
            _aoeQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitIdentityComponent>(),
                ComponentType.Exclude<DeadTag>());
        }

        protected override void OnUpdate()
        {
            var pendingAttacks = new NativeList<BossAttackInfo>(Allocator.Temp);

            // ── AoE 후보를 ForEach 이전에 미리 수집 ──────────────
            var aoePositions = _aoeQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var aoeIds       = _aoeQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);
            var aoeEntities  = _aoeQuery.ToEntityArray(Allocator.Temp);

            var aoeCandidates = new NativeArray<AoeCandidate>(aoePositions.Length, Allocator.Temp);
            for (int i = 0; i < aoePositions.Length; i++)
                aoeCandidates[i] = new AoeCandidate
                {
                    Entity   = aoeEntities[i],
                    Position = aoePositions[i].Position,
                    Team     = aoeIds[i].Team,
                };

            aoePositions.Dispose();
            aoeIds.Dispose();
            aoeEntities.Dispose();

            // ── 보스 공격 이벤트 수집 ────────────────────────────
            Entities
                .WithoutBurst()
                .WithAll<BossComponent>()
                .WithNone<DeadTag, RangedTag>()
                .ForEach((Entity entity,
                          ref AttackComponent    attack,
                          ref UnitStateComponent unitState,
                          in  BossComponent      boss,
                          in  LocalTransform     transform,
                          in  StatComponent      stat,
                          in  UnitSizeComponent  size,
                          in  UnitIdentityComponent identity) =>
                {
                    if (!attack.HasTarget || attack.AttackCooldown > 0f) return;
                    if (!EntityManager.Exists(attack.TargetEntity)) { attack.HasTarget = false; return; }
                    if (!EntityManager.HasComponent<HealthComponent>(attack.TargetEntity)) return;
                    if (EntityManager.GetComponentData<HealthComponent>(attack.TargetEntity).CurrentHp <= 0f)
                    { attack.HasTarget = false; return; }

                    float3 targetPos   = EntityManager.GetComponentData<LocalTransform>(attack.TargetEntity).Position;
                    float  attackRange = stat.Final[StatType.AttackRange];

                    if (math.distancesq(transform.Position, targetPos) > attackRange * attackRange)
                    {
                        if (unitState.Current != UnitState.Chasing)
                        { unitState.Previous = unitState.Current; unitState.Current = UnitState.Chasing; unitState.StateTimer = 0f; }
                        return;
                    }

                    // 공격 발동 — 쿨다운 리셋
                    attack.AttackCooldown = 1f / stat.Final[StatType.AttackSpeed];
                    unitState.Previous    = unitState.Current;
                    unitState.Current     = UnitState.Attacking;
                    unitState.StateTimer  = 0f;

                    // 크리티컬 판정
                    var   rng    = new Random(attack.RandomSeed == 0u ? 1u : attack.RandomSeed);
                    float roll   = rng.NextFloat();
                    attack.RandomSeed = rng.state;

                    float baseAtk = stat.Final[StatType.Attack];
                    if (boss.IsCharging) baseAtk *= (1f + boss.ChargeDamageBonus);
                    float damage = roll < stat.Final[StatType.CritChance]
                        ? baseAtk * stat.Final[StatType.CritDamage]
                        : baseAtk;

                    // 보스 크기에 비례한 AoE 반경
                    float radiusScale       = math.max(1f, size.Radius / ReferenceRadius);
                    float effectiveAoeRadius = boss.AoeRadius * radiusScale;

                    pendingAttacks.Add(new BossAttackInfo
                    {
                        BossPosition        = transform.Position,
                        TargetPosition      = targetPos,
                        TargetEntity        = attack.TargetEntity,
                        Damage              = damage,
                        EffectiveAoeRadius  = effectiveAoeRadius,
                        AoeSplashRatio      = boss.AoeSplashRatio,
                        KnockbackForce      = boss.AttackKnockbackForce,
                        KnockbackDuration   = boss.AttackKnockbackDuration,
                        AttackerTeam        = identity.Team,
                    });
                })
                .Run();

            // ── 직접 피해 + AoE 처리 ─────────────────────────────
            for (int i = 0; i < pendingAttacks.Length; i++)
            {
                BossAttackInfo info = pendingAttacks[i];

                // 타겟 직접 피해
                ApplyHit(info.TargetEntity, info.Damage, info.BossPosition, info.TargetPosition,
                         info.KnockbackForce, info.KnockbackDuration);

                // AoE — 미리 수집된 후보 배열을 순회
                if (info.EffectiveAoeRadius > 0f)
                {
                    float sqr       = info.EffectiveAoeRadius * info.EffectiveAoeRadius;
                    float aoeDamage = info.Damage * info.AoeSplashRatio;

                    for (int j = 0; j < aoeCandidates.Length; j++)
                    {
                        AoeCandidate c = aoeCandidates[j];
                        if (c.Team == info.AttackerTeam) continue;
                        if (c.Entity == info.TargetEntity) continue;
                        if (math.distancesq(c.Position, info.TargetPosition) > sqr) continue;

                        ApplyHit(c.Entity, aoeDamage, info.BossPosition, c.Position,
                                 info.KnockbackForce * 0.7f, info.KnockbackDuration * 0.7f);
                    }
                }
            }

            pendingAttacks.Dispose();
            aoeCandidates.Dispose();
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────

        void ApplyHit(Entity target, float damage, float3 attackerPos, float3 targetPos,
                      float knockbackForce, float knockbackDuration)
        {
            if (!EntityManager.Exists(target)) return;
            if (!EntityManager.HasComponent<HitEventBufferElement>(target)) return;

            float3 dir = math.normalizesafe(targetPos - attackerPos, new float3(-1f, 0f, 0f));

            EntityManager.GetBuffer<HitEventBufferElement>(target).Add(new HitEventBufferElement
            {
                Damage         = damage,
                HitDirection   = dir,
                AttackerEntity = Entity.Null,
            });

            if (knockbackForce <= 0f) return;
            if (!EntityManager.HasComponent<HitReactionComponent>(target)) return;

            float resistance = 0f;
            if (EntityManager.HasComponent<BossComponent>(target))
                resistance = EntityManager.GetComponentData<BossComponent>(target).KnockbackResistance;
            else if (EntityManager.HasComponent<EliteComponent>(target))
                resistance = EntityManager.GetComponentData<EliteComponent>(target).KnockbackResistance;

            float effectiveDuration = knockbackDuration * (1f - resistance);
            if (effectiveDuration <= 0f) return;

            var reaction = EntityManager.GetComponentData<HitReactionComponent>(target);
            reaction.KnockbackVelocity = dir * knockbackForce * (1f - resistance);
            reaction.StunDuration      = effectiveDuration;
            reaction.StunTimer         = effectiveDuration;
            reaction.IsStunned         = true;
            reaction.NeedsFlash        = true;
            EntityManager.SetComponentData(target, reaction);
        }
    }
}
