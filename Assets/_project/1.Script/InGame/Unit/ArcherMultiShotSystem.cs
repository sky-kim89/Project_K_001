using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

// ============================================================
//  ArcherMultiShotSystem.cs
//  D02 궁수 달인 — 일반 공격 시 50% 확률로 현재 타겟이 아닌
//  가장 가까운 적에게 추가 발사체를 발사한다.
//  ArcherMultiShotTag 를 가진 RangedTag 유닛이 AttackedThisFrame 이면 발동.
// ============================================================

namespace BattleGame.Units
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct ArcherMultiShotSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<UnitIdentityComponent, LocalTransform>()
                .WithNone<DeadTag>()
                .Build();

            var allEntities   = query.ToEntityArray(Allocator.TempJob);
            var allTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var allIdentities = query.ToComponentDataArray<UnitIdentityComponent>(Allocator.TempJob);

            new ArcherMultiShotJob
            {
                AllEntities   = allEntities,
                AllTransforms = allTransforms,
                AllIdentities = allIdentities,
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(ArcherMultiShotTag), typeof(RangedTag))]
    [WithNone(typeof(DeadTag))]
    public partial struct ArcherMultiShotJob : IJobEntity
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity>                AllEntities;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<LocalTransform>        AllTransforms;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<UnitIdentityComponent> AllIdentities;

        const float ArrowSpeed     = 15f;
        const float MagicBoltSpeed = 10f;

        public void Execute(
            Entity                                     entity,
            ref AttackComponent                        attack,
            ref DynamicBuffer<ProjectileLaunchRequest> launchBuffer,
            in  LocalTransform                         transform,
            in  UnitIdentityComponent                  identity,
            in  UnitJobComponent                       jobComp,
            in  StatComponent                          stat)
        {
            if (!attack.AttackedThisFrame) return;

            var  rng = new Random(attack.RandomSeed == 0u ? 1u : attack.RandomSeed);
            bool proc = rng.NextFloat() < 0.5f;
            attack.RandomSeed = rng.state;
            if (!proc) return;

            // 현재 타겟을 제외한 가장 가까운 적팀 유닛 탐색
            float3 pos        = transform.Position;
            Entity target2    = Entity.Null;
            float3 target2Pos = float3.zero;
            float  minDistSq  = float.MaxValue;

            for (int i = 0; i < AllEntities.Length; i++)
            {
                if (AllIdentities[i].Team == identity.Team)      continue;
                if (AllEntities[i]        == attack.TargetEntity) continue;

                float distSq = math.distancesq(pos, AllTransforms[i].Position);
                if (distSq < minDistSq)
                {
                    minDistSq  = distSq;
                    target2    = AllEntities[i];
                    target2Pos = AllTransforms[i].Position;
                }
            }

            if (target2 == Entity.Null) return;

            launchBuffer.Add(new ProjectileLaunchRequest
            {
                TargetEntity   = target2,
                AttackerEntity = entity,
                AttackerPos    = pos,
                TargetPos      = target2Pos,
                Damage         = attack.LastDamageDealt,
                Speed          = jobComp.Job == UnitJob.Archer ? ArrowSpeed : MagicBoltSpeed,
                Team           = identity.Team,
                DefensePierce  = stat.Final[StatType.DefensePenetration],
            });
        }
    }
}
