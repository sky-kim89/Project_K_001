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
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct UnitHitSystem : ISystem
    {
        ComponentLookup<BossComponent>  _bossLookup;
        ComponentLookup<EliteComponent> _eliteLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bossLookup  = state.GetComponentLookup<BossComponent>(isReadOnly: true);
            _eliteLookup = state.GetComponentLookup<EliteComponent>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bossLookup.Update(ref state);
            _eliteLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessHitEventsJob { Ecb = ecb, BossLookup = _bossLookup, EliteLookup = _eliteLookup }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // 피격 이벤트 처리 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct ProcessHitEventsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public ComponentLookup<BossComponent>  BossLookup;
        [ReadOnly] public ComponentLookup<EliteComponent> EliteLookup;

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

            float  totalDamage    = 0f;
            float3 totalKnockback = float3.zero;
            float  maxStun        = 0f;

            float defense = math.min(stat.Final[StatType.Defense], 0.99f); // 99% 상한

            for (int i = 0; i < hitBuffer.Length; i++)
            {
                HitEventBufferElement hit = hitBuffer[i];

                // 방어율 적용
                float rawDamage    = hit.Damage;
                float actualDamage = rawDamage * (1f - defense);
                actualDamage       = math.max(actualDamage, 1f);
                float absorbed     = rawDamage - actualDamage;
                totalDamage       += actualDamage;

                float knockbackMag = math.min(actualDamage * 0.05f, 6f);
                totalKnockback    += hit.HitDirection * knockbackMag;

                float stunTime = CalculateStunDuration(actualDamage);
                maxStun        = math.max(maxStun, stunTime);

                // 통계 결과 기록 (BattleStatCollectorSystem 이 매 프레임 읽어 귀속)
                resultBuffer.Add(new DamageResultElement
                {
                    AttackerEntity = hit.AttackerEntity,
                    ActualDamage   = actualDamage,
                    AbsorbedDamage = absorbed,
                    IsKill         = false,   // 사망 여부는 HP 판정 후 업데이트
                    IsSkillHit     = hit.IsSkillHit,
                });
            }

            health.CurrentHp -= totalDamage;
            hitBuffer.Clear();

            // ── 피격 플래시 요청 (스턴 여부와 무관하게 데미지가 발생하면 항상) ──
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

            // ── 넉백 / 경직 적용 (누적 상한 8) ──
            const float MaxKnockbackMag = 8f;
            float kbMagSq = math.lengthsq(totalKnockback);
            if (kbMagSq > MaxKnockbackMag * MaxKnockbackMag)
                totalKnockback = math.normalize(totalKnockback) * MaxKnockbackMag;

            hitReaction.KnockbackVelocity = totalKnockback;
            hitReaction.StunDuration      = maxStun;
            hitReaction.StunTimer         = maxStun;
            hitReaction.IsStunned         = maxStun > 0f;

            if (hitReaction.IsStunned)
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
