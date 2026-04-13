using System.Collections;
using UnityEngine;

// ============================================================
//  SkillEffectHelper.cs
//  액티브 스킬 이펙트 풀 스폰 유틸리티.
//
//  ■ SkillEffectHelper (static)
//    - Spawn() : PoolType.Effect 풀에서 이펙트를 꺼내 위치에 배치
//    - 스폰된 GO 에 EffectAutoReturn 을 붙여 지정 시간 후 자동 반납
//
//  ■ EffectAutoReturn (MonoBehaviour)
//    - 스폰된 이펙트 GO 에 동적으로 부착
//    - delay 초 후 PoolController.Despawn() 으로 풀에 반납
//    - OnDisable 에서 코루틴 중단 → 풀 재사용 시 안전
//
//  ■ SkillEffectConfig (struct)
//    - Runner 에 이펙트 설정을 한 번에 넘기기 위한 편의 구조체
//    - CasterEffectKey  : 사용자(시전자) 이펙트
//    - TargetEffectKey  : 피격 대상 이펙트
//    - BaseEffectKey    : 기본/범위 이펙트
//    - DespawnDelay     : 자동 반납 딜레이 (초)
// ============================================================

/// <summary>
/// 이펙트 풀(PoolType.Effect) 에서 꺼내 배치하고, 시간이 지나면 자동 반납한다.
/// </summary>
public static class SkillEffectHelper
{
    /// <summary>사용자 이펙트 (시전자 위치)</summary>
    public static void SpawnCaster(string key, Vector3 position, float despawnDelay)
        => Spawn(key, position, despawnDelay);

    /// <summary>피격 대상 이펙트</summary>
    public static void SpawnTarget(string key, Vector3 position, float despawnDelay)
        => Spawn(key, position, despawnDelay);

    /// <summary>기본/범위 이펙트</summary>
    public static void SpawnBase(string key, Vector3 position, float despawnDelay)
        => Spawn(key, position, despawnDelay);

    /// <summary>
    /// PoolType.Effect 에서 key 에 해당하는 이펙트를 스폰한다.
    /// 반환 값: 스폰된 GO (null 이면 실패)
    /// </summary>
    public static GameObject Spawn(string key, Vector3 position, float despawnDelay)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (PoolController.Instance == null) return null;

        var go = PoolController.Instance.Spawn(PoolType.Effect, key, position, Quaternion.identity);
        if (go == null) return null;

        var ret = go.GetComponent<EffectAutoReturn>() ?? go.AddComponent<EffectAutoReturn>();
        ret.StartReturn(despawnDelay);

        return go;
    }
}

// ─────────────────────────────────────────────────────────────
// ■ 이펙트 자동 반납 컴포넌트
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 이펙트 GO 에 동적으로 부착되어, 지정 딜레이 후 풀에 자동 반납한다.
/// </summary>
public class EffectAutoReturn : MonoBehaviour
{
    Coroutine _cr;

    void OnDisable()
    {
        if (_cr != null) { StopCoroutine(_cr); _cr = null; }
    }

    public void StartReturn(float delay)
    {
        if (_cr != null) StopCoroutine(_cr);
        _cr = StartCoroutine(ReturnAfter(delay));
    }

    IEnumerator ReturnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _cr = null;

        if (PoolController.Instance != null)
            PoolController.Instance.Despawn(gameObject);
        else
            gameObject.SetActive(false);
    }
}

// ─────────────────────────────────────────────────────────────
// ■ 이펙트 설정 구조체 (Runner 전달용)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Runner 에 이펙트 설정을 한 번에 전달하기 위한 구조체.
/// </summary>
public struct SkillEffectConfig
{
    public string CasterEffectKey;   // 사용자(시전자) 이펙트 풀 키
    public string TargetEffectKey;   // 피격 대상 이펙트 풀 키
    public string BaseEffectKey;     // 기본/범위 이펙트 풀 키
    public float  DespawnDelay;      // 자동 반납 딜레이 (초)
}
