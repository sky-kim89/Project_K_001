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

    // 활성 인스턴스 ID → (풀, 오브젝트 이름)  —  Despawn 역방향 조회용
    readonly Dictionary<int, (ObjectPool pool, string name)> _active = new();

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
            _active[obj.GetInstanceID()] = (pool, name);

        return obj;
    }

    /// <summary>컴포넌트까지 한 번에 반환하는 편의 메서드</summary>
    public T Spawn<T>(PoolType type, string name,
                      Vector3 position = default,
                      Quaternion rotation = default) where T : Component
        => Spawn(type, name, position, rotation)?.GetComponent<T>();

    // ── 디스폰 ────────────────────────────────────────────────
    /// <summary>오브젝트를 풀로 반납 — 어디서든 gameObject 하나만 넘기면 됨</summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        int id = obj.GetInstanceID();
        if (_active.TryGetValue(id, out var entry))
        {
            entry.pool.Release(entry.name, obj);
            _active.Remove(id);
        }
        else
        {
            Debug.LogWarning($"[PoolController] 풀에서 꺼낸 오브젝트가 아님: {obj.name}");
            Destroy(obj);
        }
    }

    // ── 정리 ──────────────────────────────────────────────────
    protected override void OnDestroy()
    {
        foreach (var pool in Pools)
            pool?.Clear();

        base.OnDestroy();
    }
}
