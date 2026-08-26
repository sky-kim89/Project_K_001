using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

// ============================================================
//  DamageAuraSystem.cs
//  DamageAuraComponent 를 가진 유닛이 주변 적을 주기적으로 태운다.
//
//  피해 = 자기 최대 체력 × HpRatio, Interval 초마다 1회.
//  현재 소유자: 방패병 달인(D04) — 장군 + 소속 방패병 병사.
//
//  ■ 실행 순서
//    UnitAttackSystem → DamageAuraSystem → UnitHitSystem
//    같은 프레임에 넣은 HitEvent 가 그대로 처리된다.
//
//  ■ 왜 BossAttackSystem 과 같은 '모아서 나중에' 꼴인가
//    남의 버퍼(HitEventBufferElement)에 써야 하는데, 쿼리를 순회하는 도중에
//    EntityManager 로 남을 건드리면 순회 핸들이 무효가 될 수 있다.
//    후보를 먼저 배열로 뜨고, 순회가 끝난 뒤에 때린다.
//
//  ⚠ 넉백을 만들지 않는다
//    HitType.Skill + HitDirection = 0 으로 넣는다. Normal 로 넣으면
//    ProcessHitEventsJob 이 피해량으로 넉백 벡터를 만들어, 초당 한 번씩
//    주변 적이 들썩인다. 오라는 밀어내는 기술이 아니다.
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitHitSystem))]
    public partial class DamageAuraSystem : SystemBase
    {
        /// <summary>일반 유닛 기준 반경 — 몸집 배율 계산의 기준값 (BossAttackSystem 과 동일).</summary>
        const float ReferenceRadius = 0.5f;

        struct AuraHit
        {
            public float3   Center;
            public float    RadiusSq;
            public float    Damage;
            public TeamType Team;
            public Entity   Owner;
        }

        struct Candidate
        {
            public Entity   Entity;
            public float3   Position;
            public TeamType Team;
        }

        EntityQuery _targetQuery;

        protected override void OnCreate()
        {
            _targetQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<UnitIdentityComponent>(),
                ComponentType.ReadOnly<HealthComponent>(),
                ComponentType.Exclude<DeadTag>());

            RequireForUpdate<DamageAuraComponent>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime <= 0f) return;   // 일시정지 중에는 타지 않는다

            var pending = new NativeList<AuraHit>(Allocator.Temp);

            // ── ① 타이머를 굴려 이번 프레임에 터질 오라를 모은다 ──
            Entities
                .WithoutBurst()
                .WithNone<DeadTag>()
                .ForEach((Entity entity,
                          ref DamageAuraComponent aura,
                          in  StatComponent         stat,
                          in  LocalTransform        transform,
                          in  UnitIdentityComponent identity,
                          in  UnitSizeComponent     size) =>
                {
                    if (aura.Interval <= 0f || aura.HpRatio <= 0f) return;

                    aura.Timer -= deltaTime;
                    if (aura.Timer > 0f) return;

                    // ⚠ Timer = Interval 로 덮어쓰지 않고 더한다
                    //   배속(2×)에서 한 프레임이 간격을 넘기면 그 초과분이 사라져
                    //   실제 발동이 설정보다 느려진다.
                    aura.Timer += aura.Interval;
                    if (aura.Timer <= 0f) aura.Timer = aura.Interval;   // 프레임이 크게 튄 경우

                    // 몸집이 커지면 오라도 넓어진다 (보스 AoE 와 같은 규칙)
                    float scale  = math.max(1f, size.Radius / ReferenceRadius);
                    float radius = aura.Radius * scale;

                    pending.Add(new AuraHit
                    {
                        Center   = transform.Position,
                        RadiusSq = radius * radius,
                        Damage   = stat.Final[StatType.MaxHp] * aura.HpRatio,
                        Team     = identity.Team,
                        Owner    = entity,
                    });
                })
                .Run();

            if (pending.Length == 0) { pending.Dispose(); return; }

            // ── ② 대상 후보를 한 번만 뜬다 ────────────────────
            var xforms   = _targetQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var ids      = _targetQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);
            var entities = _targetQuery.ToEntityArray(Allocator.Temp);

            var candidates = new NativeArray<Candidate>(entities.Length, Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                candidates[i] = new Candidate
                {
                    Entity   = entities[i],
                    Position = xforms[i].Position,
                    Team     = ids[i].Team,
                };

            xforms.Dispose();
            ids.Dispose();
            entities.Dispose();

            // ── ③ 때린다 ──────────────────────────────────────
            for (int a = 0; a < pending.Length; a++)
            {
                AuraHit hit = pending[a];
                if (hit.Damage <= 0f) continue;

                for (int c = 0; c < candidates.Length; c++)
                {
                    Candidate t = candidates[c];
                    if (t.Team == hit.Team)                                 continue;
                    if (math.distancesq(t.Position, hit.Center) > hit.RadiusSq) continue;
                    if (!EntityManager.Exists(t.Entity))                    continue;
                    if (!EntityManager.HasComponent<HitEventBufferElement>(t.Entity)) continue;

                    EntityManager.GetBuffer<HitEventBufferElement>(t.Entity).Add(
                        new HitEventBufferElement
                        {
                            Damage         = hit.Damage,
                            HitDirection   = float3.zero,   // 밀지 않는다 (파일 상단 주석 참고)
                            AttackerEntity = hit.Owner,     // 처치 귀속 — 이 부대의 전과다
                            Type           = HitType.Skill,
                        });
                }
            }

            candidates.Dispose();
            pending.Dispose();
        }
    }
}
