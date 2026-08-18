using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  PoolController
//  Singleton — 모든 ObjectPool을 총괄하는 컨트롤러
//
//  Inspector 세팅:
//  1. PoolController 오브젝트의 자식으로 UnitPool / UIPool / EffectPool 배치
//  2. Pools 리스트에 각 ObjectPool 컴포넌트를 연결
//  3. 에디터 버튼으로 프리팹 자동 로드 후 플레이
//
//  사용법:
//  Spawn   → PoolController.Instance.Spawn(PoolType.Unit, "Ally", position);
//  Despawn → PoolController.Instance.Despawn(gameObject);
// ============================================================

public class PoolController : Singleton<PoolController>
{
    [Header("풀 목록 (ObjectPool 컴포넌트 연결)")]
    public List<ObjectPool> Pools = new();

    // PoolType → ObjectPool
    readonly Dictionary<PoolType, ObjectPool> _byType = new();

    // 활성 인스턴스 ID → (풀, 이름, 오브젝트)  —  Despawn 역방향 조회용
    // ⚠ 오브젝트 참조까지 들고 있는 이유: DespawnAll 이 ID 만으로는 되찾을 수 없다.
    readonly Dictionary<int, (ObjectPool pool, string name, GameObject go)> _active = new();

    // ── 초기화 ────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        foreach (var pool in Pools)
        {
            if (pool == null) continue;
            pool.Initialize();
            _byType[pool.Type] = pool;
        }
    }

    // ── 스폰 ──────────────────────────────────────────────────
    /// <summary>이름으로 오브젝트를 풀에서 꺼낸다</summary>
    public GameObject Spawn(PoolType type, string name,
                            Vector3 position = default,
                            Quaternion rotation = default)
    {
        if (!_byType.TryGetValue(type, out var pool))
        {
            Debug.LogError($"[PoolController] 등록되지 않은 PoolType: {type}");
            return null;
        }

        var obj = pool.Get(name, position, rotation);
        if (obj != null)
            _active[obj.GetInstanceID()] = (pool, name, obj);

        return obj;
    }

    /// <summary>컴포넌트까지 한 번에 반환하는 편의 메서드</summary>
    public T Spawn<T>(PoolType type, string name,
                      Vector3 position = default,
                      Quaternion rotation = default) where T : Component
        => Spawn(type, name, position, rotation)?.GetComponent<T>();

    /// <summary>등록된 원본 프리팹 조회 — 프리팹이 들고 있는 머티리얼 등을 읽을 때.</summary>
    public GameObject GetPrefab(PoolType type, string name)
        => _byType.TryGetValue(type, out var pool) ? pool.GetPrefab(name) : null;

    // ── 미리 만들어 두기 ──────────────────────────────────────
    /// <summary>전투 시작 전 등 미리 인스턴스를 채워 둔다 (런타임 Instantiate 끊김 방지).</summary>
    public void Prewarm(PoolType type, string name, int count)
    {
        if (!_byType.TryGetValue(type, out var pool))
        {
            Debug.LogError($"[PoolController] 등록되지 않은 PoolType: {type}");
            return;
        }

        pool.Prewarm(name, count);
    }

    // ── 디스폰 ────────────────────────────────────────────────
    /// <summary>오브젝트를 풀로 반납 — 어디서든 gameObject 하나만 넘기면 됨</summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        int id = obj.GetInstanceID();
        if (_active.TryGetValue(id, out var entry))
        {
            TraceUnitDespawn(entry, obj);
            entry.pool.Release(entry.name, obj);
            _active.Remove(id);
        }
        else
        {
            Debug.LogWarning($"[PoolController] 풀에서 꺼낸 오브젝트가 아님: {obj.name}");
            Destroy(obj);
        }
    }


    /// <summary>
    /// 해당 풀에서 나가 있는 오브젝트를 전부 반납한다.
    ///
    /// ⚠ 아레나(전투 세션)를 닫을 때 쓰는 청소용이다
    ///   이펙트·발사체는 자기 수명 타이머로 돌아오므로, 전투를 끝낸 순간에는
    ///   아직 공중에 떠 있는 것들이 남는다. 다음 아레나로 넘어가서 터지면
    ///   "로비인데 폭발이 보이는" 그림이 된다.
    /// </summary>
    public void DespawnAll(PoolType type)
    {
        if (!_byType.TryGetValue(type, out var pool)) return;

        // 순회 중 _active 가 바뀌므로 대상을 먼저 모은다
        var targets = new List<int>();
        foreach (var kvp in _active)
            if (kvp.Value.pool == pool) targets.Add(kvp.Key);

        foreach (int id in targets)
        {
            if (!_active.TryGetValue(id, out var entry)) continue;
            _active.Remove(id);

            entry.pool.Release(entry.name, entry.go);
        }
    }

    /// <summary>
    /// 전투가 아닌 상태에서 유닛이 반납되면 호출자를 스택과 함께 남긴다.
    ///
    /// ⚠ 임시 추적용이다 — 원인을 잡으면 지울 것
    ///   출전 대기 중에 장수·병사가 조용히 풀로 돌아가는 문제를 쫓는다.
    ///   Despawn 을 부를 수 있는 곳은 넷뿐이라(아레나 청소·재준비·사망 연출·프리웜)
    ///   스택 한 줄이면 어느 경로인지 바로 판별된다.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    static void TraceUnitDespawn((ObjectPool pool, string name, GameObject go) entry, GameObject obj)
    {
        if (entry.pool == null || entry.pool.Type != PoolType.Unit) return;

        var flow = LobbyManager.Instance != null ? LobbyManager.Instance.Flow : LobbyFlow.Boot;
        if (flow is not (LobbyFlow.Standby or LobbyFlow.Preparing)) return;

        Debug.LogWarning(
            $"[PoolTrace] 대기 중 유닛 반납 — {obj.name} ({entry.name}) / 상태 {flow}\n"
            + System.Environment.StackTrace);
    }

    // ── 정리 ──────────────────────────────────────────────────
    protected override void OnDestroy()
    {
        foreach (var pool in Pools)
            pool?.Clear();

        base.OnDestroy();
    }
}
