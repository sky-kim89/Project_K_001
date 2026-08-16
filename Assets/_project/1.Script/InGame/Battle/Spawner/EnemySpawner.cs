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
//
//  프리웜:
//    Prewarm(entries) 를 웨이브 시작 전에 호출하면 사용할 유닛들을
//    한 프레임에 하나씩 스폰→즉시 디스폰해 CharacterBuilder.Rebuild() 를
//    게임플레이 전에 완료한다. 이후 실제 스폰 시 Rebuild 가 스킵된다.
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

    [Header("스폰 딜레이 배율")]
    [Tooltip("DelayBefore / DelayBetween 에 곱한다. 0.5 = 절반 시간에 스폰.")]
    public float SpawnDelayMultiplier = 0.5f;

    [Header("호위 대형 (SpawnEntry.EscortCount > 0)")]
    [Tooltip("대열 간 X 간격. 호위는 본대 뒤(화면 바깥쪽)로 늘어선다.")]
    public float SquadRowSpacing = 0.8f;

    [Tooltip("같은 대열 안 병사 간 Y 간격")]
    public float SquadColSpacing = 0.7f;

    // 외부(BattleManager)에서 스폰 중 여부 확인용
    public bool IsSpawning { get; private set; }

    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

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

    /// <summary>
    /// 웨이브 시작 전 외형 프리웜 — 한 프레임에 유닛 하나씩 스폰 후 즉시 디스폰.
    /// CharacterBuilder.Rebuild() 비용을 게임플레이 전에 분산 처리한다.
    /// 완료될 때까지 yield 로 기다린다.
    /// </summary>
    public IEnumerator Prewarm(List<SpawnEntry> entries)
    {
        float spawnX = GetSpawnX();
        var   pos    = new Vector3(spawnX, 0f, 0f);

        foreach (SpawnEntry entry in entries)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                GameObject unit = PoolController.Instance.Spawn(
                    PoolType.Unit, entry.PoolKey, pos, Quaternion.identity);

                if (unit != null)
                {
                    if (unit.TryGetComponent<EnemyRuntimeBridge>(out var bridge))
                        bridge.Initialize(entry.Name, entry.UnitType, entry.EnemyRace,
                                          entry.Level, entry.StatMultiplier, entry.StageBias,
                                          entry.ScaleMultiplier, entry.KnockbackImmune);
                    PoolController.Instance.Despawn(unit);
                }

                yield return null; // 프레임당 1유닛 — 스터터 방지

                // 호위대도 데워 둔다. 부대 하나는 이름이 같으므로 1기만 돌리면 된다
                // — 빼먹으면 엘리트가 등장하는 순간 CharacterBuilder.Rebuild 가 몰린다.
                if (entry.EscortCount <= 0) continue;

                GameObject escort = PoolController.Instance.Spawn(
                    PoolType.Unit, SpawnUnitType.Enemy.ToString(), pos, Quaternion.identity);

                if (escort != null)
                {
                    if (escort.TryGetComponent<EnemyRuntimeBridge>(out var eb))
                        eb.Initialize(EscortName(entry, i), SpawnUnitType.Enemy, entry.EnemyRace,
                                      entry.Level, entry.StatMultiplier, entry.StageBias);
                    PoolController.Instance.Despawn(escort);
                }

                yield return null;
            }
        }
    }

    // ── 내부 ─────────────────────────────────────────────────

    IEnumerator SpawnRoutine(List<SpawnEntry> entries)
    {
        IsSpawning = true;

        foreach (SpawnEntry entry in entries)
        {
            if (entry.DelayBefore > 0f)
                yield return new WaitForSeconds(entry.DelayBefore * SpawnDelayMultiplier);

            for (int i = 0; i < entry.Count; i++)
            {
                // 매 스폰마다 현재 카메라 위치 기준으로 실시간 계산
                float spawnX   = GetSpawnX();
                float spawnY   = GetSpawnY();
                var   spawnPos = new Vector3(spawnX, spawnY, 0f);

                // 풀에서 꺼내기 — UnitType 으로 풀 키 자동 결정
                GameObject unit = PoolController.Instance.Spawn(
                    PoolType.Unit, entry.PoolKey, spawnPos, Quaternion.identity);

                if (unit == null)
                {
                    Debug.LogWarning($"[EnemySpawner] 풀 스폰 실패: '{entry.PoolKey}' (UnitType={entry.UnitType})");
                }
                else
                {
                    // Name 시드 + 레벨 + 스테이지 배율로 스텟 초기화
                    if (unit.TryGetComponent<EnemyRuntimeBridge>(out var bridge))
                        bridge.Initialize(entry.Name, entry.UnitType, entry.EnemyRace,
                                          entry.Level, entry.StatMultiplier, entry.StageBias,
                                          entry.ScaleMultiplier, entry.KnockbackImmune);
                }

                // 호위 대형 — 본대(엘리트) 뒤에 격자로 붙여 한 덩어리로 진입시킨다
                if (entry.EscortCount > 0)
                    yield return StartCoroutine(SpawnEscorts(entry, spawnPos, i));

                if (i < entry.Count - 1)
                {
                    if (entry.DelayBetween > 0f)
                        yield return new WaitForSeconds(entry.DelayBetween * SpawnDelayMultiplier);
                    else
                        yield return null; // DelayBetween 없어도 프레임 분산
                }
            }
        }

        IsSpawning = false;
    }

    // ── 호위 대형 ────────────────────────────────────────────
    //
    //  아군(장군 + 병사) 대형과 같은 격자 계산을 쓴다 — 열 수 = ceil(sqrt(N)),
    //  각 열은 Y 중앙 정렬, 행이 늘어날수록 X 로 밀린다.
    //
    //  ■ 왜 본대 "뒤"(+X) 인가
    //    적군은 화면 오른쪽 밖에서 나와 왼쪽으로 진군한다. 호위를 앞(-X)에 두면
    //    화면 안에서 갑자기 튀어나온다. 뒤에 붙여야 부대 전체가 화면 밖에 머물다가
    //    대형을 유지한 채 함께 걸어 들어온다.
    //
    //  ■ 스폰을 프레임에 나눠도 대형은 안 깨진다
    //    자리를 leaderPos 기준 절대 좌표로 미리 정하기 때문이다.
    //    (몇 프레임 차이로 앞쪽 유닛이 조금 더 전진하지만 간격 대비 무시할 수준)
    IEnumerator SpawnEscorts(SpawnEntry entry, Vector3 leaderPos, int squadIndex)
    {
        int count = entry.EscortCount;
        int colsY = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(count)));

        // 한 부대는 같은 이름 = 같은 스텟·외형. 오합지졸이 아니라 "편성된 부대" 로 보인다.
        // (프리웜도 이 이름 하나만 데워 두면 된다)
        string escortName = EscortName(entry, squadIndex);

        for (int k = 0; k < count; k++)
        {
            int row      = k / colsY;
            int col      = k % colsY;
            int inThisRow = Mathf.Min(colsY, count - row * colsY);

            float yOffset = (col - (inThisRow - 1) * 0.5f) * SquadColSpacing;
            float xOffset = (row + 1) * SquadRowSpacing;   // 본대 뒤(+X)

            var pos = new Vector3(leaderPos.x + xOffset,
                                  leaderPos.y + yOffset,
                                  leaderPos.z);

            GameObject unit = PoolController.Instance.Spawn(
                PoolType.Unit, SpawnUnitType.Enemy.ToString(), pos, Quaternion.identity);

            if (unit == null)
            {
                Debug.LogWarning("[EnemySpawner] 호위 스폰 실패: 'Enemy' 풀");
                continue;
            }

            if (unit.TryGetComponent<EnemyRuntimeBridge>(out var bridge))
                bridge.Initialize(escortName, SpawnUnitType.Enemy, entry.EnemyRace,
                                  entry.Level, entry.StatMultiplier, entry.StageBias);

            // 생존 카운트는 BattleManager.CountUnits 가 EscortCount 까지 미리 더해 둔다
            // — 여기서 OnUnitSpawned 를 부르면 이중 계산이 된다.

            yield return null;   // 프레임당 1기 — 스터터 방지 (화면 밖이라 티가 안 난다)
        }
    }

    /// <summary>squadIndex 번째 호위대가 공유하는 유닛 이름 (스텟·외형 시드).</summary>
    static string EscortName(SpawnEntry entry, int squadIndex)
        => $"{entry.Name}_G{squadIndex}";

    float GetSpawnX()
    {
        if (SpawnX != 0f) return SpawnX;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return 12f;

        Vector3 rightEdge = _cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, _cam.nearClipPlane));
        return rightEdge.x + OffscreenMargin;
    }

    // YMin / YMax 는 카메라 중앙 Y 기준 오프셋
    float GetSpawnY()
    {
        if (_cam == null) _cam = Camera.main;
        float centerY = _cam != null ? _cam.transform.position.y : 0f;
        return centerY + Random.Range(YMin, YMax);
    }

    // ── 에디터 기즈모 ────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Camera cam = Camera.main;
        float x = SpawnX != 0f ? SpawnX
            : (cam != null
                ? cam.ViewportToWorldPoint(
                    new Vector3(1f, 0.5f, cam.nearClipPlane)).x + OffscreenMargin
                : 12f);
        float cy = cam != null ? cam.transform.position.y : 0f;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(x, cy + YMin, 0f), new Vector3(x, cy + YMax, 0f));
        Gizmos.DrawWireSphere(new Vector3(x, cy + YMin, 0f), 0.2f);
        Gizmos.DrawWireSphere(new Vector3(x, cy + YMax, 0f), 0.2f);
    }
}
