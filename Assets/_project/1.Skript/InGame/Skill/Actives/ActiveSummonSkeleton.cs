using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveSummonSkeleton.cs — 스켈레톤 소환 (공통)
//
//  시전자 위치 근처에 스켈레톤 유닛을 소환한다.
//  스켈레톤 수 = 1 (EffectValue 로 추가 소환 가능, 소수점 버림).
//  스켈레톤 능력치 = 시전자 스텟 × StatRatio.
//
//  SkeletonPoolKey: PoolController 에 등록된 풀 키 (Inspector 설정).
//  스켈레톤 프리팹에는 SoldierRuntimeBridge 가 붙어 있어야 한다.
// ============================================================

[CreateAssetMenu(fileName = "Active_SummonSkeleton", menuName = "BattleGame/Actives/SummonSkeleton")]
public class ActiveSummonSkeleton : ActiveSkillData
{
    [Header("스켈레톤 소환 설정")]
    [Tooltip("PoolController 에 등록된 스켈레톤 풀 키")]
    public string SkeletonPoolKey = "Skeleton";

    [Tooltip("스켈레톤 스텟 비율 (시전자 스텟 대비). 예: 0.5 → 50%")]
    [Range(0.1f, 1f)]
    public float StatRatio = 0.4f;

    [Tooltip("소환 스폰 오프셋 반경")]
    public float SpawnRadius = 1.5f;

    public override void Execute(ActiveSkillContext ctx)
    {
        if (string.IsNullOrEmpty(SkeletonPoolKey)) return;
        if (PoolController.Instance == null) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        // 시전자 GO 위치
        Vector3 casterPos = ctx.CasterTransform != null
            ? ctx.CasterTransform.position
            : Vector3.zero;

        // 장군 RuntimeBridge 에서 UnitStat 획득
        if (!ctx.CasterObject.TryGetComponent<GeneralRuntimeBridge>(out var generalBridge)) return;
        UnitStat generalStat = generalBridge.GetRolledStat();
        if (generalStat == null) return;

        // 장군 직업 확인
        UnitJob generalJob = UnitJob.Warrior;
        if (em.HasComponent<UnitJobComponent>(ctx.CasterEntity))
            generalJob = em.GetComponentData<UnitJobComponent>(ctx.CasterEntity).Job;

        int count = Mathf.Max(1, Mathf.FloorToInt(EffectValue));

        for (int i = 0; i < count; i++)
        {
            float angle      = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 offset   = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * SpawnRadius;
            Vector3 spawnPos = casterPos + offset;

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
