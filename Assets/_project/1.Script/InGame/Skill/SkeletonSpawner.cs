using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  SkeletonSpawner.cs
//  소환 스켈레톤 생성의 단일 진입점.
//
//  스켈레톤 소환(ActiveSummonSkeleton)과 비석 강림(ActiveGravestone)이
//  같은 함수를 쓴다 — 외형·태그·생존 카운트 규칙이 갈라지면 안 된다.
//
//  ■ 외형
//    SoldierRuntimeBridge.Initialize 는 아군 외형(ApplyAlly)을 입힌다.
//    그대로 두면 소환수가 평범한 병사로 보인다 — 스켈레톤이 아니다.
//    그래서 초기화 뒤에 ApplyEnemy(EnemyRace.Skeleton) 로 외형만 덮어쓴다.
//    (팀·스탯은 아군 그대로다. 외형만 언데드다)
//
//  ■ SummonedTag
//    소환수의 딜은 병사 딜이 아니라 스킬 딜로 집계된다 (BattleStatCollectorSystem).
// ============================================================

public static class SkeletonSpawner
{
    /// <summary>지정 위치에 스켈레톤 1기를 소환한다. 실패하면 null.</summary>
    public static GameObject Spawn(EntityManager em, string poolKey, Vector3 position,
                                   UnitStat generalStat, float statRatio,
                                   Entity casterEntity, UnitJob generalJob)
    {
        if (string.IsNullOrEmpty(poolKey) || PoolController.Instance == null) return null;

        GameObject go = PoolController.Instance.Spawn(
            PoolType.Unit, poolKey, position, Quaternion.identity);

        if (go == null)
        {
            Debug.LogWarning($"[SkeletonSpawner] 풀 스폰 실패: '{poolKey}'");
            return null;
        }

        BattleManager.Instance?.OnUnitSpawned(TeamType.Ally);

        if (!go.TryGetComponent<SoldierRuntimeBridge>(out var bridge))
        {
            Debug.LogWarning($"[SkeletonSpawner] '{poolKey}' 프리팹에 SoldierRuntimeBridge 가 없다.");
            return go;
        }

        bridge.Initialize(poolKey, generalStat, statRatio, casterEntity,
                          generalJob, "Skeleton", UnitGrade.Normal);

        // 외형만 언데드로 덮어쓴다 — Initialize 안의 ApplyAlly 이후여야 한다
        ApplySkeletonLook(go, position);

        // 소환 유닛 마킹 — 딜을 스킬 딜로 귀속
        if (go.TryGetComponent<EntityLink>(out var link) && link.Entity != Entity.Null)
            em.AddComponent<SummonedTag>(link.Entity);

        return go;
    }

    /// <summary>
    /// 소환수에게 스켈레톤 외형을 입힌다.
    /// 위치를 시드로 써서 같은 소환에서도 조금씩 다른 무기를 든다.
    /// </summary>
    static void ApplySkeletonLook(GameObject go, Vector3 position)
    {
        if (!go.TryGetComponent<UnitAppearanceBridge>(out var appearance)) return;

        // ApplyEnemy 는 "같은 조합이면 Rebuild 스킵" 이라, 이름이 매번 같으면
        // 아군 외형에서 스켈레톤으로 갈아입는 첫 호출만 비용이 든다.
        string seed = $"Skeleton_{Mathf.RoundToInt(position.x * 3f)}_{Mathf.RoundToInt(position.y * 3f)}";
        appearance.ApplyEnemy(EnemyRace.Skeleton, seed);
    }
}
