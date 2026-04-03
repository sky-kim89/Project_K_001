using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  EnemySpawner.cs
//  적 스포너 — 화면 오른쪽 밖 랜덤 Y 위치에서 풀로 유닛을 꺼낸다.
//
//  스폰: PoolController.Instance.Spawn(PoolType.Unit, entry.PoolKey, pos)
//  디스폰: UnitDespawnMonitor(유닛에 붙는 컴포넌트)가 사망 감지 후 자동 반납
//
//  좌표계: 가로형 화면, 적군은 오른쪽 밖에서 진입
// ============================================================

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 X 위치")]
    [Tooltip("화면 오른쪽 밖 X 좌표. 0 이면 카메라 뷰포트 기준 자동 계산.")]
    public float SpawnX = 0f;

    [Tooltip("카메라 자동 계산 시 화면 밖 여백 (월드 단위)")]
    public float OffscreenMargin = 2f;

    [Header("스폰 Y 범위 (랜덤)")]
    public float YMin = -3f;
    public float YMax =  3f;

    // 외부(BattleManager)에서 스폰 중 여부 확인용
    public bool IsSpawning { get; private set; }

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>entries 목록을 순서대로 풀에서 꺼내 스폰한다.</summary>
    public void Spawn(List<SpawnEntry> entries)
    {
        if (IsSpawning)
        {
            Debug.LogWarning("[EnemySpawner] 이미 스폰 중입니다.");
            return;
        }
        StartCoroutine(SpawnRoutine(entries));
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator SpawnRoutine(List<SpawnEntry> entries)
    {
        IsSpawning = true;
        float spawnX = GetSpawnX();

        foreach (SpawnEntry entry in entries)
        {
            if (entry.DelayBefore > 0f)
                yield return new WaitForSeconds(entry.DelayBefore);

            for (int i = 0; i < entry.Count; i++)
            {
                float spawnY  = Random.Range(YMin, YMax);
                var   spawnPos = new Vector3(spawnX, spawnY, 0f);

                // 풀에서 꺼내기 — PoolKey 로 유닛 종류 구분
                GameObject unit = PoolController.Instance.Spawn(
                    PoolType.Unit, entry.PoolKey, spawnPos, Quaternion.identity);

                if (unit == null)
                {
                    Debug.LogWarning($"[EnemySpawner] 풀 스폰 실패: '{entry.PoolKey}'");
                }
                else
                {
                    // PoolKey(= UnitName)를 시드로 적 스텟 초기화
                    unit.GetComponent<EnemyRuntimeBridge>()?.Initialize(entry.PoolKey, entry.UnitType);
                }

                if (i < entry.Count - 1 && entry.DelayBetween > 0f)
                    yield return new WaitForSeconds(entry.DelayBetween);
            }
        }

        IsSpawning = false;
    }

    float GetSpawnX()
    {
        if (SpawnX != 0f) return SpawnX;

        Camera cam = Camera.main;
        if (cam == null) return 12f;

        Vector3 rightEdge = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, cam.nearClipPlane));
        return rightEdge.x + OffscreenMargin;
    }

    // ── 에디터 기즈모 ────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        float x = SpawnX != 0f ? SpawnX
            : (Camera.main != null
                ? Camera.main.ViewportToWorldPoint(
                    new Vector3(1f, 0.5f, Camera.main.nearClipPlane)).x + OffscreenMargin
                : 12f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(x, YMin, 0f), new Vector3(x, YMax, 0f));
        Gizmos.DrawWireSphere(new Vector3(x, YMin, 0f), 0.2f);
        Gizmos.DrawWireSphere(new Vector3(x, YMax, 0f), 0.2f);
    }
}
