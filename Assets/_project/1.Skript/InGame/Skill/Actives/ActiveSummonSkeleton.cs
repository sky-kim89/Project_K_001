using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveSummonSkeleton.cs — 스켈레톤 소환 (공통)
//
//  동작:
//    1. 소속 장군의 DeadSoldierSpawnPointBuffer 에서 최근 사망 위치 읽기
//    2. 사망 위치마다 BaseEffect + 스켈레톤 소환
//    3. 버퍼 소비(Clear) → 다음 사용 전까지 재누적
//    4. 버퍼가 비어 있으면 시전자 주변 원형 위치에 소환 (fallback)
// ============================================================

[CreateAssetMenu(fileName = "Active_SummonSkeleton", menuName = "BattleGame/Actives/SummonSkeleton")]
public class ActiveSummonSkeleton : ActiveSkillData
{
    [Header("스켈레톤 소환 설정")]
    [Tooltip("PoolController 에 등록된 스켈레톤 풀 키")]
    public string SkeletonPoolKey = "Soldier";

    [Tooltip("스켈레톤 스텟 비율 (시전자 스텟 대비). 예: 0.5 → 50%")]
    [Range(0.1f, 1f)]
    public float StatRatio = 0.4f;

    [Tooltip("fallback 소환 스폰 오프셋 반경 (사망 위치 없을 때)")]
    public float SpawnRadius = 1.5f;

    public override void Execute(ActiveSkillContext ctx)
    {
        if (string.IsNullOrEmpty(SkeletonPoolKey)) return;
        if (PoolController.Instance == null) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        Vector3 casterPos = ctx.CasterTransform != null ? ctx.CasterTransform.position : Vector3.zero;

        if (!ctx.CasterObject.TryGetComponent<GeneralRuntimeBridge>(out var generalBridge)) return;
        UnitStat generalStat = generalBridge.GetRolledStat();
        if (generalStat == null) return;

        if (!em.HasComponent<UnitJobComponent>(ctx.CasterEntity)) return;
        UnitJob generalJob = em.GetComponentData<UnitJobComponent>(ctx.CasterEntity).Job;

        int count = Mathf.Max(1, Mathf.FloorToInt(EffectValue));

        // ── 소환 위치 목록 결정 ──────────────────────────────
        // 우선: 장군의 DeadSoldierSpawnPointBuffer (최근 사망 위치)
        // fallback: 시전자 주변 원형
        System.Collections.Generic.List<Vector3> spawnPositions = new(count);

        if (em.HasBuffer<DeadSoldierSpawnPointBuffer>(ctx.CasterEntity))
        {
            var buf = em.GetBuffer<DeadSoldierSpawnPointBuffer>(ctx.CasterEntity);

            // 최신 위치부터 역순으로 count 개 사용
            for (int i = buf.Length - 1; i >= 0 && spawnPositions.Count < count; i--)
            {
                float3 p = buf[i].Position;
                spawnPositions.Add(new Vector3(p.x, p.y, p.z));
            }

            buf.Clear();  // 소비
        }

        // fallback: 남은 슬롯을 원형 위치로 채움
        int filled = spawnPositions.Count;
        for (int i = filled; i < count; i++)
        {
            float   angle  = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * SpawnRadius;
            spawnPositions.Add(casterPos + offset);
        }

        // ── 소환 실행 ────────────────────────────────────────
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            Vector3 spawnPos = spawnPositions[i];

            // 사망 위치에 이펙트
            SkillEffectHelper.SpawnBase(BaseEffectKey, spawnPos, EffectDespawnDelay);

            GameObject go = PoolController.Instance.Spawn(
                PoolType.Unit, SkeletonPoolKey, spawnPos, Quaternion.identity);

            if (go == null)
            {
                Debug.LogWarning($"[ActiveSummonSkeleton] 풀 스폰 실패: '{SkeletonPoolKey}'");
                continue;
            }

            BattleManager.Instance?.OnUnitSpawned(TeamType.Ally);

            if (go.TryGetComponent<SoldierRuntimeBridge>(out var bridge))
                bridge.Initialize(SkeletonPoolKey, generalStat, StatRatio, ctx.CasterEntity,
                    generalJob, "Skeleton", UnitGrade.Normal);
        }
    }
}
