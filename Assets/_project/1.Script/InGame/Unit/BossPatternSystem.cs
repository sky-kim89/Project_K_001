using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

// ============================================================
//  BossPatternSystem.cs
//  보스 행동 패턴 시스템.
//
//  ■ 구조
//    BossPatternBase    — 패턴 베이스 클래스 (상속 확장용)
//    BossChargePattern  — 돌진 패턴 구현체
//    BossPatternSystem  — 매 프레임 패턴 실행 조율
//
//  ■ 새 패턴 추가 방법
//    1. BossPatternBase 상속 → Execute() 구현
//    2. BossPatternSystem._patterns 배열에 추가
//
//  ■ 돌진 패턴 흐름
//    ChargePatternTimer 감소 → 0 이하 시 가장 가까운 적으로 ChargeTarget 고정
//    → IsCharging=true, ChargeTimer=ChargeDuration
//    → UnitMovementSystem 이 ChargeSpeedMult 배속으로 이동
//    → ChargeTimer 만료 시 IsCharging=false, 쿨다운 리셋
//
//  ■ 실행 순서
//    BossPatternSystem [Before UnitMovementSystem]
//    → UnitMovementSystem (돌진 이동 처리)
//    → BossAttackSystem  (공격 + AoE)
// ============================================================

namespace BattleGame.Units
{
    // ──────────────────────────────────────────────────────────
    //  패턴 베이스 클래스
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// 보스 행동 패턴 베이스. 새 패턴은 이 클래스를 상속해서 Execute() 를 구현한다.
    /// SystemBase 레퍼런스를 통해 EntityManager / EntityQuery 에 접근할 수 있다.
    /// </summary>
    public abstract class BossPatternBase
    {
        protected SystemBase System;
        protected EntityManager EntityManager => System.EntityManager;

        public void Init(SystemBase system) => System = system;

        /// <summary>보스 한 개체에 대해 패턴 로직을 실행한다.</summary>
        /// <param name="entity">보스 엔티티</param>
        /// <param name="boss">BossComponent (ref — 수정 가능)</param>
        /// <param name="position">현재 위치</param>
        /// <param name="identity">팀 정보</param>
        /// <param name="dt">deltaTime</param>
        /// <param name="candidates">이 프레임에 미리 수집된 타겟 후보 배열</param>
        public abstract void Execute(
            Entity                    entity,
            ref BossComponent         boss,
            in  float3                position,
            in  UnitIdentityComponent identity,
            float                     dt,
            NativeArray<UnitCandidate> candidates);
    }

    // ──────────────────────────────────────────────────────────
    //  타겟 후보 데이터 (ForEach 이전에 미리 수집)
    // ──────────────────────────────────────────────────────────

    public struct UnitCandidate
    {
        public float3   Position;
        public TeamType Team;
    }

    // ──────────────────────────────────────────────────────────
    //  돌진 패턴 구현체
    // ──────────────────────────────────────────────────────────

    public sealed class BossChargePattern : BossPatternBase
    {
        public override void Execute(
            Entity                    entity,
            ref BossComponent         boss,
            in  float3                position,
            in  UnitIdentityComponent identity,
            float                     dt,
            NativeArray<UnitCandidate> candidates)
        {
            if (boss.IsCharging)
            {
                boss.ChargeTimer -= dt;
                if (boss.ChargeTimer <= 0f)
                {
                    boss.IsCharging         = false;
                    boss.ChargeTimer        = 0f;
                    boss.ChargePatternTimer = boss.ChargePatternCooldown;
                }
                return;
            }

            boss.ChargePatternTimer -= dt;
            if (boss.ChargePatternTimer > 0f) return;

            // 가장 가까운 적팀 유닛 탐색 (pre-collected 배열 사용)
            float3 closestPos    = float3.zero;
            float  closestDistSq = float.MaxValue;
            bool   found         = false;

            for (int i = 0; i < candidates.Length; i++)
            {
                UnitCandidate c = candidates[i];
                if (c.Team == identity.Team) continue;

                float distSq = math.distancesq(position, c.Position);
                if (distSq >= closestDistSq) continue;

                closestDistSq = distSq;
                closestPos    = c.Position;
                found         = true;
            }

            if (!found) { boss.ChargePatternTimer = 2f; return; }

            boss.IsCharging   = true;
            boss.ChargeTarget = closestPos;
            boss.ChargeTimer  = boss.ChargeDuration;
        }
    }

    // ──────────────────────────────────────────────────────────
    //  패턴 시스템 (러너)
    // ──────────────────────────────────────────────────────────

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitMovementSystem))]
    public partial class BossPatternSystem : SystemBase
    {
        // 새 패턴 추가 시 여기에 인스턴스를 추가
        readonly BossPatternBase[] _patterns = { new BossChargePattern() };

        EntityQuery _candidateQuery;

        protected override void OnCreate()
        {
            foreach (var p in _patterns) p.Init(this);

            _candidateQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitIdentityComponent>(),
                ComponentType.Exclude<DeadTag>());
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            // ── 타겟 후보를 ForEach 외부에서 미리 수집 ──────────
            var positions = _candidateQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var ids       = _candidateQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);

            var candidates = new NativeArray<UnitCandidate>(positions.Length, Allocator.Temp);
            for (int i = 0; i < positions.Length; i++)
                candidates[i] = new UnitCandidate { Position = positions[i].Position, Team = ids[i].Team };

            positions.Dispose();
            ids.Dispose();

            // ── 보스 순회 ───────────────────────────────────────
            foreach (var (boss, transform, identity, entity) in
                SystemAPI.Query<RefRW<BossComponent>, RefRO<LocalTransform>, RefRO<UnitIdentityComponent>>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                ref BossComponent bossRef = ref boss.ValueRW;
                foreach (var pattern in _patterns)
                    pattern.Execute(entity, ref bossRef, transform.ValueRO.Position, identity.ValueRO, dt, candidates);
            }

            candidates.Dispose();
        }
    }
}
