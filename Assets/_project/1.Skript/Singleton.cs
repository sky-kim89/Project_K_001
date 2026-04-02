using UnityEngine;

// ============================================================
//  Singleton<T>
//  MonoBehaviour 싱글톤 베이스 클래스
//
//  사용법:
//  public class PoolController : Singleton<PoolController> { }
// ============================================================

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
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
            _instance = null;
    }
}
