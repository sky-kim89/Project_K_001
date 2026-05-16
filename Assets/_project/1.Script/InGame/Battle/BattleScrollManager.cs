using UnityEngine;

// ============================================================
//  BattleScrollManager.cs
//  선두 아군을 기준으로 카메라를 오른쪽으로만 이동시키고
//  배경 스프라이트(BGSprite1/2)를 무한 루프한다.
//
//  규칙:
//    - 스크롤은 선두 아군에 의해서만 발생 (적 무관)
//    - 카메라는 오른쪽으로만 이동 (왼쪽으로 되돌아가지 않음)
//    - EnemySpawner / ScreenClampSystem 은 Camera.main 을 직접 참조하므로
//      이 스크립트가 카메라만 이동하면 나머지는 자동으로 따라감
//
//  씬 설정:
//    Inspector 에서 _cam, _bg1, _bg2 연결
//    두 스프라이트는 초기에 나란히 배치되어야 함 (합산 폭 >= 화면 폭 × 2)
// ============================================================

public class BattleScrollManager : Singleton<BattleScrollManager>
{
    [Header("인게임 카메라")]
    [SerializeField] Camera _cam;

    [Header("배경 스프라이트")]
    [SerializeField] Transform _bg1;
    [SerializeField] Transform _bg2;

    [Header("카메라 스크롤")]
    [Tooltip("선두 유닛을 화면 중앙 기준으로 얼마나 왼쪽에 고정할지 (0 = 중앙, 양수 = 중앙보다 왼쪽)")]
    [SerializeField] float _leadOffset = 0f;
    [Tooltip("카메라 추종 스무딩 시간 (초). 값이 클수록 느리게 따라감")]
    [SerializeField] float _smoothTime = 0.4f;

    float _bgWidth;        // 스프라이트 1장의 월드 폭
    float _initCamX;       // 배틀 시작 전 카메라 초기 X
    float _targetCamX;     // 목표 카메라 X (오른쪽으로만 증가)
    float _camVelX;        // SmoothDamp 내부 속도
    bool  _active;

    // ── Unity 생명주기 ────────────────────────────────────────

    void Start()
    {
        _initCamX = _cam.transform.position.x;
        _bgWidth  = CalcBgWidth();
        ResetBackground(_initCamX);
    }

    void OnEnable()
    {
        BattleManager.OnAlliesReady += OnBattleStart;
        BattleManager.OnVictory     += OnBattleEnd;
        BattleManager.OnDefeat      += OnBattleEnd;
    }

    void OnDisable()
    {
        BattleManager.OnAlliesReady -= OnBattleStart;
        BattleManager.OnVictory     -= OnBattleEnd;
        BattleManager.OnDefeat      -= OnBattleEnd;
    }

    // ── 배틀 이벤트 ───────────────────────────────────────────

    void OnBattleStart()
    {
        Vector3 camPos = _cam.transform.position;
        camPos.x                = _initCamX;
        _cam.transform.position = camPos;

        _targetCamX = _initCamX;
        _camVelX    = 0f;
        _active     = true;

        ResetBackground(_initCamX);
    }

    void OnBattleEnd() => _active = false;

    // ── 메인 루프 ─────────────────────────────────────────────

    void LateUpdate()
    {
        if (!_active) return;

        // ── 선두 아군 추종 ────────────────────────────────────
        float leadX = LeadAllyTrackerSystem.LeadX;
        if (leadX > -9999f)
        {
            float desired = leadX - _leadOffset;
            if (desired > _targetCamX)
                _targetCamX = desired;
        }

        // ── 카메라 X 스무딩 이동 ─────────────────────────────
        Vector3 pos = _cam.transform.position;
        pos.x = Mathf.SmoothDamp(pos.x, _targetCamX, ref _camVelX, _smoothTime);
        _cam.transform.position = pos;

        // ── 배경 루프 ─────────────────────────────────────────
        LoopBackground();
    }

    // ── 배경 처리 ─────────────────────────────────────────────

    void ResetBackground(float camCenterX)
    {
        if (_bgWidth <= 0f) return;
        SetBgX(_bg1, camCenterX);
        SetBgX(_bg2, camCenterX + _bgWidth);
    }

    void LoopBackground()
    {
        if (_bgWidth <= 0f) return;

        float halfW    = _cam.orthographicSize * _cam.aspect;
        float leftEdge = _cam.transform.position.x - halfW;

        // 센터 피벗 기준: right edge = position.x + bgWidth * 0.5f
        if (_bg1.position.x + _bgWidth * 0.5f < leftEdge)
            SetBgX(_bg1, _bg2.position.x + _bgWidth);
        else if (_bg2.position.x + _bgWidth * 0.5f < leftEdge)
            SetBgX(_bg2, _bg1.position.x + _bgWidth);
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static void SetBgX(Transform t, float x)
    {
        Vector3 p = t.position;
        p.x = x;
        t.position = p;
    }

    float CalcBgWidth()
    {
        if (_bg1 != null && _bg1.TryGetComponent<SpriteRenderer>(out var sr))
            return sr.bounds.size.x;

        Debug.LogWarning("[BattleScrollManager] _bg1에 SpriteRenderer가 없습니다. bgWidth = 20f 기본값 사용.");
        return 20f;
    }
}
