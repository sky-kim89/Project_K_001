using System.Collections;
using UnityEngine;

// ============================================================
//  SkillEffectHelper.cs
//  액티브 스킬 이펙트 풀 스폰 유틸리티.
//
//  ■ SkillEffectHelper (static)
//    - Spawn(key, position, ...)   : PoolType.Effect 풀에서 이펙트를 꺼내 위치에 배치
//    - Spawn(key, from, to, ...)   : LineRenderer 포함 이펙트를 from→to 사이에 배치
//    - 스폰된 GO 에 EffectAutoReturn 을 붙여 지정 시간 후 자동 반납
//
//  ■ EffectAutoReturn (MonoBehaviour)
//    - 스폰된 이펙트 GO 에 동적으로 부착
//    - delay 초 후 PoolController.Despawn() 으로 풀에 반납
//    - OnDisable 에서 코루틴 중단 → 풀 재사용 시 안전
//
//  ■ SkillEffectConfig (struct)
//    - Runner 에 이펙트 설정을 한 번에 넘기기 위한 편의 구조체
//    - TargetEffectKey  : 피격 대상 이펙트
//    - BaseEffectKey    : 기본/범위 이펙트
//    - DespawnDelay     : 자동 반납 딜레이 (초)
// ============================================================

public static class SkillEffectHelper
{
    /// <summary>
    /// PoolType.Effect 에서 key 에 해당하는 이펙트를 스폰한다.
    /// rotation=default 이면 Quaternion.identity 사용.
    /// scale=1 이면 원본 크기.
    /// </summary>
    public static GameObject Spawn(string key, Vector3 position, float despawnDelay,
        Quaternion rotation = default, float scale = 1f)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (PoolController.Instance == null) return null;

        Quaternion rot = rotation.Equals(default) ? Quaternion.identity : rotation;
        var go = PoolController.Instance.Spawn(PoolType.Effect, key, position, rot);
        if (go == null) return null;

        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

        if (!go.TryGetComponent<EffectAutoReturn>(out var ret))
            ret = go.AddComponent<EffectAutoReturn>();
        ret.StartReturn(despawnDelay);

        return go;
    }

    /// <summary>
    /// from 위치에 스폰한 뒤 LineRenderer 를 from→to 로 설정한다.
    /// "ImpactSparks", "RedGlow" 이름의 자식 GO 는 to 위치로 이동한다.
    /// </summary>
    public static GameObject Spawn(string key, Vector3 from, Vector3 to, float despawnDelay)
    {
        var go = Spawn(key, from, despawnDelay);
        if (go == null) return null;

        if (go.TryGetComponent<LineRenderer>(out var lr))
        {
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }

        var sparks = go.transform.Find("ImpactSparks");
        if (sparks != null) sparks.position = to;

        var glow = go.transform.Find("RedGlow");
        if (glow != null) glow.position = to;

        return go;
    }
}

// ─────────────────────────────────────────────────────────────
// ■ 이펙트 자동 반납 컴포넌트
// ─────────────────────────────────────────────────────────────

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

public struct SkillEffectConfig
{
    public string CasterEffectKey;   // 시전자 이펙트 풀 키
    public string TargetEffectKey;   // 피격 대상 이펙트 풀 키
    public string BaseEffectKey;     // 기본/범위 이펙트 풀 키
    public float  DespawnDelay;      // 자동 반납 딜레이 (초)
}
