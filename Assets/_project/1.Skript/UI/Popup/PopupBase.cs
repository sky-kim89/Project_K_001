using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  PopupBase.cs
//  모든 팝업 UI의 베이스 클래스.
//
//  기능:
//    - 열기/닫기 애니메이션 (FadeScale / FadeOnly / ScaleOnly / SlideUp)
//    - 닫기 콜백 (onClose)
//    - 라이프사이클 훅 (OnBeforeOpen / OnAfterOpen / OnBeforeClose / OnAfterClose)
//    - Time.unscaledDeltaTime 사용 → 게임 일시정지 중에도 애니메이션 동작
//    - Close() 호출 시 PopupManager 에 자동 통보 → 목록 정리 및 블로커 제거
//
//  사용법:
//    public class MyPopup : PopupBase
//    {
//        protected override void OnAfterOpen()  { /* UI 세팅 */ }
//        protected override void OnAfterClose() { /* 정리    */ }
//    }
//
//    // 닫기 취소 예시 (저장 확인 등)
//    protected override bool OnBeforeClose() { return _isSaved; }
// ============================================================

public enum PopupAnimation
{
    Instant,    // 즉시
    FadeScale,  // 페이드 + 스케일 (기본)
    FadeOnly,   // 페이드만
    ScaleOnly,  // 스케일만
    SlideUp,    // 아래서 위로 슬라이드
}

[RequireComponent(typeof(CanvasGroup))]
public abstract class PopupBase : MonoBehaviour
{
    [Header("팝업 타입")]
    [SerializeField] PopupType _popupType;
    public PopupType PopupType => _popupType;

    [Header("애니메이션")]
    [SerializeField] PopupAnimation _openAnimation  = PopupAnimation.FadeScale;
    [SerializeField] PopupAnimation _closeAnimation = PopupAnimation.FadeScale;
    [SerializeField] float          _openDuration   = 0.20f;
    [SerializeField] float          _closeDuration  = 0.15f;

    // ── 컴포넌트 캐시 ─────────────────────────────────────────────
    CanvasGroup   _canvasGroup;
    RectTransform _rectTransform;

    // ── 상태 ─────────────────────────────────────────────────────
    /// <summary>팝업이 열려 있는지 여부.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>열기 애니메이션이 완료됐는지 여부. CloseRoutine 이 이 값을 기다린다.</summary>
    bool _openComplete;

    // ── 콜백 ─────────────────────────────────────────────────────
    Action            _onClose;
    Action<PopupBase> _onClosedToManager;  // PopupManager 내부 정리용

    // ── Unity 생명주기 ────────────────────────────────────────────

    protected virtual void Awake()
    {
        _canvasGroup   = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    // ── 공개 API ─────────────────────────────────────────────────

    /// <summary>
    /// 팝업을 닫는다.
    /// onComplete : 닫기 애니메이션 완료 후 호출.
    /// PopupManager 에 등록된 팝업이면 자동으로 목록에서 제거된다.
    /// </summary>
    public void Close(Action onComplete = null)
    {
        if (!IsOpen) return;
        _onClose = onComplete;
        StartCoroutine(CloseRoutine());
    }

    // ── PopupManager 전용 ─────────────────────────────────────────

    /// <summary>PopupManager 가 팝업 생성 직후 호출한다.</summary>
    internal void OpenInternal(Action<PopupBase> onClosedToManager, Action onClose = null)
    {
        _onClosedToManager = onClosedToManager;
        _onClose           = onClose;
        IsOpen             = true;

        ApplyAnimProgress(_openAnimation, 0f);
        gameObject.SetActive(true);

        OnBeforeOpen();
        StartCoroutine(OpenRoutine());
    }

    // ── 라이프사이클 훅 ───────────────────────────────────────────

    /// <summary>열기 애니메이션 시작 직전.</summary>
    protected virtual void OnBeforeOpen() { }

    /// <summary>열기 애니메이션 완료 후.</summary>
    protected virtual void OnAfterOpen() { }

    /// <summary>
    /// 닫기 직전. false 를 반환하면 닫기를 취소할 수 있다.
    /// (예: 필수 입력값 미입력 시 닫기 방지)
    /// </summary>
    protected virtual bool OnBeforeClose() => true;

    /// <summary>닫기 애니메이션 완료 후, GameObject 파괴 직전.</summary>
    protected virtual void OnAfterClose() { }

    // ── 애니메이션 코루틴 ─────────────────────────────────────────

    IEnumerator OpenRoutine()
    {
        _openComplete = false;
        yield return StartCoroutine(PlayAnim(_openAnimation, opening: true, _openDuration));
        _openComplete = true;
        OnAfterOpen();
    }

    IEnumerator CloseRoutine()
    {
        if (!OnBeforeClose()) yield break;

        // 열기 애니메이션이 끝날 때까지 대기 (열리는 도중 Close() 호출 시 충돌 방지)
        yield return new WaitUntil(() => _openComplete);

        IsOpen = false;
        _canvasGroup.blocksRaycasts = false;

        yield return StartCoroutine(PlayAnim(_closeAnimation, opening: false, _closeDuration));

        OnAfterClose();

        _onClose?.Invoke();
        _onClose = null;

        // PopupManager 에 통보 → 블로커 제거 + 목록 정리 + Destroy 처리
        _onClosedToManager?.Invoke(this);
        _onClosedToManager = null;
    }

    IEnumerator PlayAnim(PopupAnimation anim, bool opening, float duration)
    {
        if (anim == PopupAnimation.Instant || duration <= 0f)
        {
            ApplyAnimProgress(anim, opening ? 1f : 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float raw   = Mathf.Clamp01(elapsed / duration);
            float eased = opening ? EaseOutBack(raw) : EaseInCubic(raw);
            ApplyAnimProgress(anim, opening ? eased : 1f - eased);
            yield return null;
        }

        ApplyAnimProgress(anim, opening ? 1f : 0f);
    }

    void ApplyAnimProgress(PopupAnimation anim, float t)
    {
        switch (anim)
        {
            case PopupAnimation.FadeScale:
                _canvasGroup.alpha = t;
                float fs = Mathf.LerpUnclamped(0.85f, 1f, t);
                _rectTransform.localScale = new Vector3(fs, fs, 1f);
                break;

            case PopupAnimation.FadeOnly:
                _canvasGroup.alpha = t;
                _rectTransform.localScale = Vector3.one;
                break;

            case PopupAnimation.ScaleOnly:
                _canvasGroup.alpha = 1f;
                float ss = Mathf.LerpUnclamped(0.85f, 1f, t);
                _rectTransform.localScale = new Vector3(ss, ss, 1f);
                break;

            case PopupAnimation.SlideUp:
                _canvasGroup.alpha = t;
                float yOff = Mathf.LerpUnclamped(-80f, 0f, t);
                _rectTransform.anchoredPosition =
                    new Vector2(_rectTransform.anchoredPosition.x, yOff);
                break;

            default:  // Instant
                _canvasGroup.alpha = t >= 0.5f ? 1f : 0f;
                _rectTransform.localScale = Vector3.one;
                break;
        }

        _canvasGroup.blocksRaycasts = t > 0.01f;
    }

    // ── 이징 ──────────────────────────────────────────────────────

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    static float EaseInCubic(float t) => t * t * t;
}
