using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  AllySpawner.cs
//  아군 스포너 — Inspector 에서 받은 위치에 풀로 유닛을 배치한다.
//
//  스폰: PoolController.Instance.Spawn(PoolType.Unit, entry.PoolKey, pos)
//  디스폰: UnitDespawnMonitor(유닛에 붙는 컴포넌트)가 사망 감지 후 자동 반납
//
//  SpawnPoints: 0번 = 장군 위치, 이후 = 병사 슬롯
//  BattleModeBase.SetupSpawners() 에서 SpawnEntry 목록을 주입받는다.
// ============================================================

public class AllySpawner : MonoBehaviour
{
    [Header("아군 스폰 위치 목록 (왼쪽 진형)")]
    [Tooltip("0번 = 장군 위치. 이후 인덱스 = 병사 슬롯.")]
    public List<Transform> SpawnPoints = new();

    // 외부(BattleManager)에서 스폰 중 여부 확인용
    public bool IsSpawning { get; private set; }

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>entries 목록을 순서대로 풀에서 꺼내 슬롯 위치에 배치한다.</summary>
    public void Spawn(List<SpawnEntry> entries)
    {
        if (IsSpawning)
        {
            Debug.LogWarning("[AllySpawner] 이미 스폰 중입니다.");
            return;
        }
        StartCoroutine(SpawnRoutine(entries));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator SpawnRoutine(List<SpawnEntry> entries)
    {
        IsSpawning = true;
        int slotIndex = 0;

        foreach (SpawnEntry entry in entries)
        {
            if (entry.DelayBefore > 0f)
                yield return new WaitForSeconds(entry.DelayBefore);

            for (int i = 0; i < entry.Count; i++)
            {
                if (slotIndex >= SpawnPoints.Count)
                {
                    Debug.LogWarning("[AllySpawner] SpawnPoints 슬롯이 부족합니다.");
                    break;
                }

                Transform slot = SpawnPoints[slotIndex];

                // 풀에서 꺼내기
                GameObject unit = PoolController.Instance.Spawn(
                    PoolType.Unit, entry.PoolKey, slot.position, slot.rotation);

                if (unit == null)
                {
                    Debug.LogWarning($"[AllySpawner] 풀 스폰 실패: '{entry.PoolKey}'");
                }
                else
                {
                    string unitName = string.IsNullOrEmpty(entry.UnitName) ? entry.PoolKey : entry.UnitName;
                    if (unit.TryGetComponent<GeneralRuntimeBridge>(out var bridge))
                        bridge.Initialize(unitName, entry.Level);
                }

                slotIndex++;

                if (i < entry.Count - 1 && entry.DelayBetween > 0f)
                    yield return new WaitForSeconds(entry.DelayBetween);
            }
        }

        IsSpawning = false;
    }

    // ── 에디터 기즈모 (배치 확인용) ──────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < SpawnPoints.Count; i++)
        {
            if (SpawnPoints[i] == null) continue;
            Gizmos.DrawWireSphere(SpawnPoints[i].position, 0.3f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(SpawnPoints[i].position + Vector3.up * 0.4f,
                i == 0 ? "General" : $"Soldier {i}");
#endif
        }
    }
}
