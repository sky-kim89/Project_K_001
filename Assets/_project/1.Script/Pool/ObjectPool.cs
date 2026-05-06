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

        GameObject obj;
        if (pool.Count > 0)
        {
            int last = pool.Count - 1;
            obj = pool[last];
            pool.RemoveAt(last);
        }
        else
        {
            obj = CreateInstance(name);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
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
