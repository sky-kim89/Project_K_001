using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

// ============================================================
//  TraitRainFireSoldierSystem.cs
//  폭우 사격 — 병사 전담 처리 시스템.
//
//  ■ 왜 별도 시스템인가
//    장군은 CombatTriggerSystem 이 GeneralTriggerSetComponent(관리형)에 담긴
//    핸들러를 불러 처리한다. 병사는 그 컴포넌트가 없어서 그 경로를 탈 수 없다.
//    그렇다고 병사에게 트리거셋을 붙이면 병사 한 명당 관리형 객체가 생겨
//    수십 명 규모에서 낭비가 크다.
//
//  ■ 프레임당 적 배열은 한 번만 만든다
//    핸들러 방식은 발동할 때마다 ToEntityArray 를 뜬다. 장군 5명이면 괜찮지만
//    병사는 수십 명이라 그대로 두면 프레임마다 수십 번 복사하게 된다.
//    여기서는 때린 병사가 하나라도 있을 때만, 딱 한 번 떠서 전부가 공유한다.
//
//  ■ 스플래시 규칙은 RainFireSplash 가 소유한다 (장군과 완전히 동일)
//
//  실행 순서: CombatTriggerSystem(장군 처리) → 이 시스템 → UnitHitSystem(피해 정산)
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CombatTriggerSystem))]
    [UpdateBefore(typeof(UnitHitSystem))]
    public partial class TraitRainFireSoldierSystem : SystemBase
    {
        EntityQuery _enemyQuery;
        EntityQuery _soldierQuery;

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All  = new ComponentType[]
                {
                    ComponentType.ReadOnly<UnitIdentityComponent>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                },
                None = new ComponentType[] { typeof(DeadTag) },
            });

            _soldierQuery = GetEntityQuery(new EntityQueryDesc
            {
                All  = new ComponentType[]
                {
                    ComponentType.ReadOnly<SoldierComponent>(),
                    ComponentType.ReadOnly<TraitRainFireTag>(),
                    ComponentType.ReadOnly<AttackHitEvent>(),
                },
                None = new ComponentType[] { typeof(DeadTag) },
            });

            // 폭우 사격을 보유하지 않은 런에서는 아예 돌지 않는다.
            RequireForUpdate(_soldierQuery);
        }

        protected override void OnUpdate()
        {
            var soldiers = _soldierQuery.ToEntityArray(Allocator.Temp);

            // 이번 프레임에 착탄한 병사가 하나도 없으면 적 배열도 만들지 않는다.
            bool anyHit = false;
            for (int i = 0; i < soldiers.Length; i++)
            {
                if (EntityManager.GetBuffer<AttackHitEvent>(soldiers[i]).Length > 0)
                {
                    anyHit = true;
                    break;
                }
            }
            if (!anyHit)
            {
                soldiers.Dispose();
                return;
            }

            var targets    = _enemyQuery.ToEntityArray(Allocator.Temp);
            var transforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var identities = _enemyQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);

            foreach (var soldier in soldiers)
            {
                // 앞선 병사의 스플래시(이펙트 GO 활성화 포함)로 구조가 바뀌었을 수 있다.
                if (!EntityManager.Exists(soldier)) continue;
                if (!EntityManager.HasBuffer<AttackHitEvent>(soldier)) continue;

                var buf = EntityManager.GetBuffer<AttackHitEvent>(soldier);
                if (buf.Length == 0) continue;

                // 버퍼 핸들을 들고 순회하지 않는다 (RainFireSplash 가 이펙트를 스폰한다)
                var events = buf.ToNativeArray(Allocator.Temp);
                foreach (var ev in events)
                    RainFireSplash.ApplyOne(EntityManager, soldier,
                                            ev.TargetEntity, ev.TargetPos, ev.Damage,
                                            targets, transforms, identities);
                events.Dispose();

                // 소비 완료 → 클리어. 반드시 다시 가져온다 (위 핸들은 이미 무효일 수 있다).
                if (EntityManager.Exists(soldier) && EntityManager.HasBuffer<AttackHitEvent>(soldier))
                    EntityManager.GetBuffer<AttackHitEvent>(soldier).Clear();
            }

            targets.Dispose();
            transforms.Dispose();
            identities.Dispose();
            soldiers.Dispose();
        }
    }
}
