using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

// ============================================================
//  BossChargeHitSystem.cs
//  보스 돌진 중 경로상 적 타격 처리.
//
//  ■ 목적
//    BossAttackSystem 은 HasTarget + 쿨다운 기반 일반 공격.
//    돌진(IsCharging)은 별도 스킬 타격 — 경로상 모든 적을 1회씩 피격.
//
//  ■ 동작
//    IsCharging = true 인 보스 주변 AoeRadius 내 적팀 유닛에 HitEvent 추가.
//    _hitThisCharge 로 돌진 1회당 적 1체 최대 1번 타격 보장.
//    돌진 시작 프레임(wasCharging false → true)에 피격 목록 초기화.
//
//  ■ 실행 순서
//    UnitMovementSystem (돌진 이동) → BossChargeHitSystem → UnitHitSystem
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMovementSystem))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial class BossChargeHitSystem : SystemBase
    {
        NativeHashSet<Entity> _hitThisCharge;
        bool                  _bossWasCharging;

        EntityQuery _unitQuery;

        protected override void OnCreate()
        {
            _hitThisCharge   = new NativeHashSet<Entity>(32, Allocator.Persistent);
            _bossWasCharging = false;

            _unitQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitIdentityComponent>(),
                ComponentType.Exclude<DeadTag>());
        }

        protected override void OnDestroy()
        {
            if (_hitThisCharge.IsCreated) _hitThisCharge.Dispose();
        }

        protected override void OnUpdate()
        {
            // 살아있는 모든 유닛 위치·팀·엔티티 미리 수집
            var positions  = _unitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var identities = _unitQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);
            var entities   = _unitQuery.ToEntityArray(Allocator.Temp);

            bool bossIsCharging = false;

            // ── 돌진 시작 감지 (피격 목록 리셋 타이밍 결정) ──────────
            Entities
                .WithoutBurst()
                .WithAll<BossComponent>()
                .WithNone<DeadTag>()
                .ForEach((in BossComponent boss) =>
                {
                    if (boss.IsCharging) bossIsCharging = true;
                })
                .Run();

            // 돌진 시작 프레임: 이전 돌진의 피격 기록 초기화
            if (bossIsCharging && !_bossWasCharging)
                _hitThisCharge.Clear();

            _bossWasCharging = bossIsCharging;

            if (!bossIsCharging)
            {
                positions.Dispose();
                identities.Dispose();
                entities.Dispose();
                return;
            }

            // ── 돌진 타격 처리 ────────────────────────────────────────
            Entities
                .WithoutBurst()
                .WithAll<BossComponent>()
                .WithNone<DeadTag>()
                .ForEach((Entity bossEntity,
                          in BossComponent boss,
                          in LocalTransform bossTransform,
                          in StatComponent  stat,
                          in UnitIdentityComponent identity) =>
                {
                    if (!boss.IsCharging) return;

                    float radiusSq = boss.AoeRadius * boss.AoeRadius;
                    float damage   = stat.Final[StatType.Attack] * 0.3f * (1f + boss.ChargeDamageBonus);

                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (identities[i].Team == identity.Team) continue; // 아군 제외
                        Entity target = entities[i];
                        if (_hitThisCharge.Contains(target)) continue;     // 이미 피격
                        if (math.distancesq(bossTransform.Position, positions[i].Position) > radiusSq) continue;
                        if (!EntityManager.HasBuffer<HitEventBufferElement>(target)) continue;

                        float3 dir = math.normalizesafe(
                            positions[i].Position - bossTransform.Position,
                            new float3(1f, 0f, 0f));

                        EntityManager.GetBuffer<HitEventBufferElement>(target).Add(new HitEventBufferElement
                        {
                            Damage         = damage,
                            HitDirection   = dir * boss.AttackKnockbackForce,
                            AttackerEntity = bossEntity,
                            Type           = HitType.Skill,
                        });

                        _hitThisCharge.Add(target);
                    }
                })
                .Run();

            positions.Dispose();
            identities.Dispose();
            entities.Dispose();
        }
    }
}
