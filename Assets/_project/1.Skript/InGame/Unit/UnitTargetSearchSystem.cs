using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

// ============================================================
//  UnitTargetSearchSystem.cs
//  Grid 기반 타겟 탐색 시스템
//  - 전체 유닛 O(n²) 순회 없이 인접 Grid 셀만 탐색
//  - 적팀 유닛 중 가장 가까운 유닛을 타겟으로 설정
//  - 3프레임마다 실행 (매 프레임 불필요 — 성능 최적화)
// ============================================================

namespace BattleGame.Units
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct UnitTargetSearchSystem : ISystem
    {
        NativeParallelMultiHashMap<int2, UnitGridEntry> _gridMap;
        uint _frameIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _gridMap = new NativeParallelMultiHashMap<int2, UnitGridEntry>(
                1024, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _gridMap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _frameIndex++;
            if (_frameIndex % 3 != 0) return;

            _gridMap.Clear();

            int unitCount = SystemAPI.QueryBuilder()
                .WithAll<UnitIdentityComponent, LocalTransform>()
                .WithNone<DeadTag>()
                .Build()
                .CalculateEntityCount();

            if (_gridMap.Capacity < unitCount)
                _gridMap.Capacity = unitCount * 2;

            // ① Grid 맵 빌드
            var gridWriter = _gridMap.AsParallelWriter();
            new BuildGridMapJob { GridWriter = gridWriter }.ScheduleParallel();

            state.Dependency.Complete();

            // ② 타겟 탐색
            new FindNearestTargetJob
            {
                GridMap  = _gridMap,
                CellSize = UnitGridConstants.CellSize
            }.ScheduleParallel();
        }
    }

    /// <summary>Grid 셀에 저장되는 유닛 정보 (최소한의 데이터만)</summary>
    public struct UnitGridEntry
    {
        public Entity   Entity;
        public float3   Position;
        public TeamType Team;     // TeamId(int) → Team(TeamType) 으로 변경
    }

    public static class UnitGridConstants
    {
        public const float CellSize = 3f;
    }

    // ──────────────────────────────────────────
    // Grid 맵 빌드 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct BuildGridMapJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, UnitGridEntry>.ParallelWriter GridWriter;

        public void Execute(
            Entity                   entity,
            in LocalTransform        transform,
            in UnitIdentityComponent identity,
            ref GridCellComponent    gridCell)
        {
            int2 cell = WorldToCell(transform.Position);

            gridCell.PrevCell = gridCell.Cell;
            gridCell.Cell     = cell;

            GridWriter.Add(cell, new UnitGridEntry
            {
                Entity   = entity,
                Position = transform.Position,
                Team     = identity.Team,   // TeamId → Team
            });
        }

        static int2 WorldToCell(float3 pos) => (int2)math.floor(pos.xy / UnitGridConstants.CellSize);
    }

    // ──────────────────────────────────────────
    // 타겟 탐색 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// 자신의 Grid 셀과 인접 셀만 탐색해 가장 가까운 적팀 유닛을 타겟으로 설정.
    /// 탐색 범위 = CellSize × (SearchRadius × 2 + 1)
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct FindNearestTargetJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, UnitGridEntry> GridMap;
        public float CellSize;

        const int SearchRadius = 3;

        public void Execute(
            in  LocalTransform        transform,
            in  UnitIdentityComponent identity,
            in  GridCellComponent     gridCell,
            ref AttackComponent       attack)
        {
            float  closestDistSq  = float.MaxValue;
            Entity closestEntity  = Entity.Null;

            for (int dx = -SearchRadius; dx <= SearchRadius; dx++)
            for (int dy = -SearchRadius; dy <= SearchRadius; dy++)
            {
                int2 checkCell = gridCell.Cell + new int2(dx, dy);

                if (!GridMap.TryGetFirstValue(checkCell, out UnitGridEntry entry, out var it))
                    continue;

                do
                {
                    // 같은 팀이면 스킵
                    if (entry.Team == identity.Team) continue;

                    float distSq = math.distancesq(transform.Position, entry.Position);
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestEntity = entry.Entity;
                    }
                }
                while (GridMap.TryGetNextValue(out entry, ref it));
            }

            if (closestEntity != Entity.Null)
            {
                attack.TargetEntity = closestEntity;
                attack.HasTarget    = true;
            }
            else
            {
                attack.HasTarget = false;
            }
        }
    }
}
