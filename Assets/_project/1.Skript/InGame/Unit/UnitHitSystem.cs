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
        ComponentLookup<BossComponent> _bossLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bossLookup = state.GetComponentLookup<BossComponent>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bossLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessHitEventsJob { Ecb = ecb, BossLookup = _bossLookup }.ScheduleParallel();
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
        [ReadOnly] public ComponentLookup<BossComponent> BossLookup;

        public void Execute(
            [ChunkIndexInQuery] int                  chunkIndex,
            Entity                                   entity,
            ref HealthComponent                      health,
            in  StatComponent                        stat,       // Defense → StatFinal
            ref HitReactionComponent                 hitReaction,
            ref UnitStateComponent                   unitState,
            ref DynamicBuffer<HitEventBufferElement> hitBuffer)
        {
            if (hitBuffer.Length == 0) return;

            float  totalDamage    = 0f;
            float3 totalKnockback = float3.zero;
            float  maxStun        = 0f;

            float defense = stat.Final[StatType.Defense]; // ← StatFinal 에서 읽음

            for (int i = 0; i < hitBuffer.Length; i++)
            {
                HitEventBufferElement hit = hitBuffer[i];

                // 방어율 적용
                float actualDamage = hit.Damage * (1f - defense);
                actualDamage       = math.max(actualDamage, 1f);
                totalDamage       += actualDamage;

                float knockbackMag = actualDamage * 0.05f;
                totalKnockback    += hit.HitDirection * knockbackMag;

                float stunTime = CalculateStunDuration(actualDamage);
                maxStun        = math.max(maxStun, stunTime);
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
                Ecb.AddComponent<DeadTag>(chunkIndex, entity); // DeadTag 로 사망 표시
                return;
            }

            // ── 보스 내성 적용 ──
            if (BossLookup.HasComponent(entity))
            {
                var boss = BossLookup[entity];
                totalKnockback *= (1f - boss.KnockbackResistance);
                maxStun        *= (1f - boss.CCResistance);
            }

            // ── 넉백 / 경직 적용 ──
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
