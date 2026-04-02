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
        // Grid 셀 → 유닛 목록 맵
        // NativeParallelMultiHashMap: 멀티코어 Job에서 안전하게 읽기 가능
        NativeParallelMultiHashMap<int2, UnitGridEntry> _gridMap;
        uint _frameIndex;  // 프레임 카운터 (3프레임 간격 실행용)

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 초기 용량 1024 — 런타임에 자동 확장됨
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
            // 3프레임마다 타겟 탐색 실행 (매 프레임 불필요)
            _frameIndex++;
            if (_frameIndex % 3 != 0) return;

            _gridMap.Clear();

            // 현재 유닛 수에 맞게 용량 조정
            int unitCount = SystemAPI.QueryBuilder()
                .WithAll<UnitIdentityComponent, LocalTransform>()
                .WithNone<DeadTag>()
                .Build()
                .CalculateEntityCount();

            if (_gridMap.Capacity < unitCount)
                _gridMap.Capacity = unitCount * 2;

            // ① Grid 맵 빌드 (모든 살아있는 유닛 등록)
            var gridWriter = _gridMap.AsParallelWriter();
            new BuildGridMapJob { GridWriter = gridWriter }.ScheduleParallel();

            state.Dependency.Complete(); // Grid 빌드 완료 후 탐색 시작

            // ② 타겟 탐색 (Grid 기반)
            new FindNearestTargetJob
            {
                GridMap   = _gridMap,
                CellSize  = UnitGridConstants.CellSize
            }.ScheduleParallel();
        }
    }

    // Grid 셀에 저장되는 유닛 정보 (최소한의 데이터만)
    public struct UnitGridEntry
    {
        public Entity Entity;
        public float3 Position;
        public int    TeamId;
    }

    public static class UnitGridConstants
    {
        public const float CellSize = 3f; // 3유닛 크기 단위 셀
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
            Entity                    entity,
            in LocalTransform         transform,
            in UnitIdentityComponent  identity,
            ref GridCellComponent     gridCell)
        {
            int2 cell = WorldToCell(transform.Position);

            // 이전 셀과 다른 경우만 기록 (변경 감지)
            gridCell.PrevCell = gridCell.Cell;
            gridCell.Cell     = cell;

            GridWriter.Add(cell, new UnitGridEntry
            {
                Entity   = entity,
                Position = transform.Position,
                TeamId   = identity.TeamId
            });
        }

        static int2 WorldToCell(float3 pos)
        {
            return new int2(
                (int)math.floor(pos.x / UnitGridConstants.CellSize),
                (int)math.floor(pos.y / UnitGridConstants.CellSize)
            );
        }
    }

    // ──────────────────────────────────────────
    // 타겟 탐색 Job
    // ──────────────────────────────────────────

    /// <summary>
    /// 자신의 Grid 셀과 인접 셀만 탐색해 가장 가까운 적팀 유닛을 타겟으로 설정
    /// 탐색 범위 = CellSize * (SearchRadius * 2 + 1)
    /// </summary>
    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct FindNearestTargetJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, UnitGridEntry> GridMap;
        public float CellSize;

        // 탐색 반경 (셀 단위) — AttackRange에 맞게 조정
        const int SearchRadius = 3;

        public void Execute(
            in  LocalTransform        transform,
            in  UnitIdentityComponent identity,
            in  GridCellComponent     gridCell,
            ref AttackComponent       attack)
        {
            float closestDistSq = float.MaxValue;
            Entity closestEntity = Entity.Null;

            // 인접 셀 탐색
            for (int dx = -SearchRadius; dx <= SearchRadius; dx++)
            for (int dy = -SearchRadius; dy <= SearchRadius; dy++)
            {
                int2 checkCell = gridCell.Cell + new int2(dx, dy);

                if (!GridMap.TryGetFirstValue(checkCell,
                    out UnitGridEntry entry, out var it)) continue;

                do
                {
                    // 같은 팀이면 스킵
                    if (entry.TeamId == identity.TeamId) continue;

                    float distSq = math.distancesq(transform.Position, entry.Position);
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestEntity = entry.Entity;
                    }
                }
                while (GridMap.TryGetNextValue(out entry, ref it));
            }

            // 타겟 업데이트
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
