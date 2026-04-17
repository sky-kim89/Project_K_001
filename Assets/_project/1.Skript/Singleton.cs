using UnityEngine;

// ============================================================
//  Singleton<T>          — MonoBehaviour 싱글톤 (씬에 배치 필요)
//  SingletonPure<T>      — 순수 C# 싱글톤 (씬 독립, 자동 생성)
//
//  사용법:
//  public class PoolController : Singleton<PoolController> { }
//  public class GameSession    : SingletonPure<GameSession> { }
// ============================================================

// ── 순수 C# 싱글톤 ────────────────────────────────────────────

public abstract class SingletonPure<T> where T : SingletonPure<T>, new()
{
    static T _instance;
    public static T Instance => _instance ??= new T();
}

// ── MonoBehaviour 싱글톤 ──────────────────────────────────────

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<T>();
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this as T)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this as T;
        // DontDestroyOnLoad 는 루트 오브젝트만 지원한다.
        // 씬 셋업 툴에서 계층 아래에 배치된 경우에도 올바르게 동작하도록 먼저 분리.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
            _instance = null;
    }
}
