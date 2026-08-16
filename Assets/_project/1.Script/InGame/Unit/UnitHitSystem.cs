using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;


// ============================================================
//  UnitHitSystem.cs
//  피격 처리 시스템
//  - HitEventBuffer 에 쌓인 이벤트를 읽어 HP 차감
//  - Defense 는 StatComponent.Final[StatType.Defense] 에서 읽음
//  - 사망 시 DeadTag 부착 (HealthComponent.IsDead 필드 제거됨)
// ============================================================

namespace BattleGame.Units
{
    /// <summary>
    /// 방어율 → 실제 피해 환산 공식. Burst 안에서도 쓸 수 있도록 순수 static.
    ///
    /// ⚠ 예약 피해(ProjectileIncomingDamageSystem)와 실제 피해(ProjectileHitJob)가
    ///   반드시 같은 값을 내야 한다 — 예약이 실제보다 크면 죽지 않은 적이 영영
    ///   타겟에서 제외되어 전투가 멈춘다. 두 곳 모두 이 함수만 쓸 것.
    /// </summary>
    public static class DamageMath
    {
        public static float AfterDefense(float rawDamage, float rawDefense,
                                         float softCap, float overflowRate, float effectiveCap)
        {
            float eff = rawDefense <= softCap
                ? rawDefense
                : softCap + (rawDefense - softCap) * overflowRate;
            float defense = math.min(eff, effectiveCap);
            return math.max(rawDamage * (1f - defense), 1f);
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct UnitHitSystem : ISystem
    {
        ComponentLookup<BossComponent>        _bossLookup;
        ComponentLookup<EliteComponent>       _eliteLookup;
        ComponentLookup<MirrorArmorComponent> _mirrorArmorLookup;
        ComponentLookup<KnockbackImmuneTag>   _knockbackImmuneLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bossLookup            = state.GetComponentLookup<BossComponent>(isReadOnly: true);
            _eliteLookup           = state.GetComponentLookup<EliteComponent>(isReadOnly: true);
            _mirrorArmorLookup     = state.GetComponentLookup<MirrorArmorComponent>(isReadOnly: true);
            _knockbackImmuneLookup = state.GetComponentLookup<KnockbackImmuneTag>(isReadOnly: true);
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bossLookup.Update(ref state);
            _eliteLookup.Update(ref state);
            _mirrorArmorLookup.Update(ref state);
            _knockbackImmuneLookup.Update(ref state);

            var cfg = GameplayConfig.Current;
            if (cfg == null) return;

            float softCap      = cfg.DefenseMax;
            float overflowRate = cfg.DefenseOverflowRate;
            float effectiveCap = cfg.DefenseEffectiveCap;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessHitEventsJob
            {
                Ecb                   = ecb,
                BossLookup            = _bossLookup,
                EliteLookup           = _eliteLookup,
                MirrorArmorLookup     = _mirrorArmorLookup,
                KnockbackImmuneLookup = _knockbackImmuneLookup,
                DefenseSoftCap        = softCap,
                DefenseOverflowRate   = overflowRate,
                DefenseEffectiveCap   = effectiveCap,
            }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // 피격 이벤트 처리 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct ProcessHitEventsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter               Ecb;
        [ReadOnly] public ComponentLookup<BossComponent>        BossLookup;
        [ReadOnly] public ComponentLookup<EliteComponent>       EliteLookup;
        [ReadOnly] public ComponentLookup<MirrorArmorComponent> MirrorArmorLookup;
        [ReadOnly] public ComponentLookup<KnockbackImmuneTag>   KnockbackImmuneLookup;
        public float DefenseSoftCap;
        public float DefenseOverflowRate;
        public float DefenseEffectiveCap;

        // ── 일반 공격 넉백 ────────────────────────────────────
        /// <summary>최대 체력 100% 를 한 방에 깎았을 때의 넉백 세기.</summary>
        const float KnockbackPerHpRatio = 10f;
        /// <summary>단일 타격 넉백 상한 (여러 타격 합산 상한은 MaxKnockbackMag).</summary>
        const float MaxSingleKnockback  = 6f;

        public void Execute(
            [ChunkIndexInQuery] int                    chunkIndex,
            Entity                                     entity,
            ref HealthComponent                        health,
            in  StatComponent                          stat,       // Defense → StatFinal
            ref HitReactionComponent                   hitReaction,
            ref UnitStateComponent                     unitState,
            ref DynamicBuffer<HitEventBufferElement>   hitBuffer,
            ref DynamicBuffer<DamageResultElement>     resultBuffer)
        {
            if (hitBuffer.Length == 0) return;

            float  totalDamage      = 0f;
            float3 totalKnockback   = float3.zero;
            float  maxStun          = 0f;
            float  maxNormalKbMag   = 0f;   // 일반 공격: 프레임 내 최대 단일 타격 넉백
            float3 maxNormalKbVec   = float3.zero;

            float rawDef      = stat.Final[StatType.Defense];

            // 넉백은 "절대 피해량" 이 아니라 "최대 체력 대비 비율" 로 정한다.
            //   100 체력에 50 피해  → 0.5   → 크게 밀린다
            //   10000 체력에 50 피해 → 0.005 → 거의 밀리지 않는다
            // 절대값 기준이면 스테이지가 올라가 공격력이 커질수록 체력 만 단위 보스도
            // 잡몹처럼 날아가고, 반대로 초반 저공격력은 종잇장 적조차 못 밀어낸다.
            float maxHp = math.max(1f, stat.Final[StatType.MaxHp]);

            bool  hasMirror   = MirrorArmorLookup.HasComponent(entity);
            float mirrorRatio = hasMirror ? MirrorArmorLookup[entity].ReflectRatio : 0f;

            bool hasSkillKnockback = false;

            for (int i = 0; i < hitBuffer.Length; i++)
            {
                HitEventBufferElement hit = hitBuffer[i];

                // 방어율 적용 (공식은 DamageMath 가 소유 — 예약 피해 계산과 반드시 동일)
                float rawDamage    = hit.Damage;
                float actualDamage = DamageMath.AfterDefense(
                    rawDamage, rawDef, DefenseSoftCap, DefenseOverflowRate, DefenseEffectiveCap);
                float absorbed     = rawDamage - actualDamage;
                totalDamage       += actualDamage;

                if (hit.Type == HitType.Skill)
                {
                    totalKnockback += hit.HitDirection;
                    if (math.lengthsq(hit.HitDirection) > 0f)
                        hasSkillKnockback = true;
                }
                else
                {
                    // 체력의 100% 를 한 방에 날리면 KnockbackPerHpRatio 만큼 밀린다.
                    float kbMag = math.min(actualDamage / maxHp * KnockbackPerHpRatio, MaxSingleKnockback);
                    if (kbMag > maxNormalKbMag)
                    {
                        maxNormalKbMag = kbMag;
                        maxNormalKbVec = hit.HitDirection * kbMag;
                    }
                }

                float stunTime = CalculateStunDuration(actualDamage);
                maxStun        = math.max(maxStun, stunTime);

                // 거울 방어 반사 — 실제로 받은 피해(방어 적용 후) 기준으로 반사. 반사된 피해는 재반사 불가.
                if (hasMirror && hit.Type != HitType.Reflected && hit.AttackerEntity != Entity.Null)
                {
                    Ecb.AppendToBuffer(chunkIndex, hit.AttackerEntity, new HitEventBufferElement
                    {
                        Damage         = actualDamage * mirrorRatio,
                        AttackerEntity = entity,
                        Type           = HitType.Reflected,
                    });
                }

                resultBuffer.Add(new DamageResultElement
                {
                    AttackerEntity = hit.AttackerEntity,
                    ActualDamage   = actualDamage,
                    AbsorbedDamage = absorbed,
                    IsKill         = false,
                    Type           = hit.Type,
                });
            }

            totalKnockback   += maxNormalKbVec;

            // 스킬 넉백이 있으면 KnockbackJob이 적용할 수 있도록 최소 경직 시간 보장
            // (KnockbackJob은 IsStunned=true 일 때만 KnockbackVelocity를 적용함)
            if (hasSkillKnockback)
                maxStun = math.max(maxStun, 0.3f);

            health.CurrentHp -= totalDamage;
            hitBuffer.Clear();

            hitReaction.NeedsFlash = true;

            // ── 사망 판정 ──
            if (health.CurrentHp <= 0f)
            {
                health.CurrentHp = 0f;
                ChangeState(ref unitState, UnitState.Dead);
                Ecb.AddComponent<DeadTag>(chunkIndex, entity);

                // 마지막 히트를 날린 공격자에게 킬 표시 (버퍼 마지막 항목 기준)
                if (resultBuffer.Length > 0)
                {
                    int last = resultBuffer.Length - 1;
                    var r = resultBuffer[last];
                    r.IsKill = true;
                    resultBuffer[last] = r;
                }
                return;
            }

            // ── 방패병 달인: 넉백 완전 무시 ──
            if (KnockbackImmuneLookup.HasComponent(entity))
            {
                totalKnockback = float3.zero;
                maxStun        = 0f;
            }
            else
            {
                // ── 내성 적용 ──
                if (BossLookup.HasComponent(entity))
                {
                    var boss = BossLookup[entity];
                    totalKnockback *= (1f - boss.KnockbackResistance);
                    maxStun        *= (1f - boss.CCResistance);
                }
                else if (EliteLookup.HasComponent(entity))
                {
                    totalKnockback *= (1f - EliteLookup[entity].KnockbackResistance);
                }
            }

            // ── 넉백 / 경직 적용 (누적 상한 8) ──
            const float MaxKnockbackMag = 8f;
            float kbMagSq = math.lengthsq(totalKnockback);
            if (kbMagSq > MaxKnockbackMag * MaxKnockbackMag)
                totalKnockback = math.normalize(totalKnockback) * MaxKnockbackMag;

            hitReaction.KnockbackVelocity = totalKnockback;

            hitReaction.StunDuration = math.max(hitReaction.StunDuration, maxStun);
            hitReaction.StunTimer    = math.max(hitReaction.StunTimer,    maxStun);
            hitReaction.IsStunned    = hitReaction.IsStunned || maxStun > 0f;

            if (maxStun > 0f)
                ChangeState(ref unitState, UnitState.Hit);
        }

        static float CalculateStunDuration(float damage)
        {
            if (damage >= 100f) return 0.6f;
            if (damage >= 50f)  return 0.35f;
            if (damage >= 20f)  return 0.15f;
            return 0f;
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        {
            s.Previous   = s.Current;
            s.Current    = next;
            s.StateTimer = 0f;
        }
    }

    // ──────────────────────────────────────────
    // 상태 타이머 갱신 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    public partial struct StateTimerJob : IJobEntity
    {
        public readonly float DeltaTime;

        public void Execute(ref UnitStateComponent unitState)
        {
            unitState.StateTimer += DeltaTime;
        }
    }
}
