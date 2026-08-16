using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ObjectPool (MonoBehaviour)
//  PoolType 하나를 담당하며 여러 종류의 프리팹을 함께 관리
//  키 = 오브젝트 이름 (prefab.name)
// ============================================================

public class ObjectPool : MonoBehaviour
{
    [Header("풀 설정")]
    public PoolType Type;

    [Header("관리 프리팹 목록 (에디터 버튼으로 자동 로드 가능)")]
    public List<GameObject> Prefabs = new();

    // 오브젝트 이름 → 비활성 인스턴스 목록
    readonly Dictionary<string, List<GameObject>> _inactive   = new();
    // 오브젝트 이름 → 원본 프리팹 (신규 생성 시 사용)
    readonly Dictionary<string, GameObject>        _prefabMap  = new();

    // ── 초기화 (PoolController가 Awake에서 호출) ───────────────
    public void Initialize()
    {
        foreach (var prefab in Prefabs)
        {
            if (prefab == null) continue;

            string key = prefab.name;
            _inactive[key]  = new List<GameObject>();
            _prefabMap[key] = prefab;
        }
    }

    // ── 꺼내기 ────────────────────────────────────────────────
    public GameObject Get(string name, Vector3 position = default, Quaternion rotation = default)
    {
        if (!_inactive.TryGetValue(name, out var pool))
        {
            Debug.LogWarning($"[ObjectPool:{Type}] 등록되지 않은 이름: {name}");
            return null;
        }

        // 파괴된 GO 건너뜀 (씬 리로드·직접 Destroy 호출 시 stale 참조 발생)
        GameObject obj = null;
        while (pool.Count > 0)
        {
            int last      = pool.Count - 1;
            var candidate = pool[last];
            pool.RemoveAt(last);
            if (candidate != null) { obj = candidate; break; }
        }
        if (obj == null)
            obj = CreateInstance(name);

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 등록된 원본 프리팹을 이름으로 조회한다 (인스턴스 아님).
    /// 프리팹이 들고 있는 머티리얼·설정을 런타임에 읽고 싶을 때 쓴다
    /// — 같은 에셋을 Resources 에 또 복사해 두는 것보다 출처가 하나로 유지된다.
    /// </summary>
    public GameObject GetPrefab(string name)
        => _prefabMap.TryGetValue(name, out var prefab) ? prefab : null;

    // ── 미리 만들어 두기 ──────────────────────────────────────
    /// <summary>
    /// name 프리팹의 비활성 인스턴스를 count 개까지 미리 채워 둔다.
    /// 이미 그만큼 있으면 아무것도 하지 않으므로 여러 번 불러도 안전하다.
    ///
    /// 전투 중 Instantiate 가 몰리면 프레임이 끊긴다 — 이펙트처럼
    /// 짧은 시간에 여러 개가 동시에 필요한 오브젝트는 전투 시작 전에 채운다.
    /// </summary>
    public void Prewarm(string name, int count)
    {
        if (!_inactive.TryGetValue(name, out var pool))
        {
            Debug.LogWarning($"[ObjectPool:{Type}] 등록되지 않은 이름: {name}");
            return;
        }

        for (int i = pool.Count; i < count; i++)
            pool.Add(CreateInstance(name));
    }

    // ── 반납 ──────────────────────────────────────────────────
    public void Release(string name, GameObject instance)
    {
        if (instance == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(transform);

        if (_inactive.TryGetValue(name, out var pool))
            pool.Add(instance);
    }

    // ── 전체 정리 ──────────────────────────────────────────────
    public void Clear()
    {
        foreach (var list in _inactive.Values)
        {
            foreach (var obj in list)
                if (obj != null) Destroy(obj);
            list.Clear();
        }
    }

    // ── 내부 생성 ──────────────────────────────────────────────
    GameObject CreateInstance(string name)
    {
        var obj = Instantiate(_prefabMap[name], transform);
        obj.SetActive(false);
        return obj;
    }
}
