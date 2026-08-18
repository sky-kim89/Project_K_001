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
//    Inspector 에서 _bg1 = BGSprite1, _bg2 = BGSprite2 연결
//    두 스프라이트는 초기에 나란히 배치되어야 함 (합산 폭 >= 화면 폭 × 2)
// ============================================================

public class BattleScrollManager : Singleton<BattleScrollManager>
{
    [Header("배경 스프라이트")]
    [SerializeField] Transform _bg1;
    [SerializeField] Transform _bg2;

    [Header("카메라 스크롤")]
    [Tooltip("선두 유닛을 화면 중앙 기준으로 얼마나 왼쪽에 고정할지 (0 = 중앙, 양수 = 중앙보다 왼쪽)")]
    [SerializeField] float _leadOffset = 0f;
    [Tooltip("카메라 추종 스무딩 시간 (초). 값이 클수록 느리게 따라감")]
    [SerializeField] float _smoothTime = 0.4f;

    Camera _cam;
    float  _bgWidth;
    float  _initCamX;
    float  _targetCamX;
    float  _camVelX;
    bool   _active;

    // ── Unity 생명주기 ────────────────────────────────────────

    void Start()
    {
        // ⚠ Camera.main 을 쓰지 않는다 — 로비 카메라가 함께 떠 있으면 그쪽이 잡힌다
        _cam      = ResolveCamera();
        _initCamX = _cam.transform.position.x;
        _bgWidth  = CalcBgWidth();
        ResetBackground(_initCamX);
    }

    void OnEnable()
    {
        // ⚠ OnAlliesReady 가 아니라 OnWavesStarted 다
        //   출전 대기 화면에서도 아군은 스폰된다. 그 신호로 추종을 켜면
        //   대기 화면이 잡아 둔 카메라 위치를 이 스크립트가 곧바로 되돌린다
        //   (카메라 X 가 저절로 0 으로 돌아가던 증상).
        BattleManager.OnWavesStarted += OnBattleStart;
        BattleManager.OnVictory      += OnBattleEnd;
        BattleManager.OnDefeat       += OnBattleEnd;
    }

    void OnDisable()
    {
        BattleManager.OnWavesStarted -= OnBattleStart;
        BattleManager.OnVictory      -= OnBattleEnd;
        BattleManager.OnDefeat       -= OnBattleEnd;
    }

    // ── 배틀 이벤트 ───────────────────────────────────────────

    void OnBattleStart() => StartScroll();
    void OnBattleEnd()   => StopScroll();

    // ── 공개 제어 (실전·로비 데모 공용) ───────────────────────

    /// <summary>
    /// 선두 아군 추종을 시작한다. 카메라를 시작 위치로 되돌리고 배경도 다시 깐다.
    ///
    /// ⚠ 실전 진입뿐 아니라 로비 배경 데모도 이 문으로 들어온다
    ///   데모가 BattleManager 이벤트를 흉내 내면 그쪽 상태 기계가 오염된다.
    /// </summary>
    public void StartScroll()
    {
        _cam = ResolveCamera();
        if (_cam == null) return;

        Vector3 camPos = _cam.transform.position;
        camPos.x                = _initCamX;
        _cam.transform.position = camPos;

        _targetCamX = _initCamX;
        _camVelX    = 0f;
        _active     = true;

        ResetBackground(_initCamX);
    }

    public void StopScroll() => _active = false;

    /// <summary>
    /// 배경을 시작 위치(_initCamX)로 되돌린다. BattlePanel 로 돌아올 때만 부른다.
    ///
    /// ⚠ 전투가 끝나도 배경은 전진한 자리에 남는다
    ///   스크롤 중 BGSprite1/2 는 카메라를 따라 계속 오른쪽으로 옮겨진다.
    ///   되돌리지 않으면 대기 화면 카메라가 텅 빈 허공을 잡는다.
    ///
    /// ⚠ 카메라 위치가 아니라 시작 위치에 맞춘다
    ///   전투가 시작되면 StartScroll 이 배경을 _initCamX 에 다시 깐다.
    ///   대기 때 다른 자리에 깔아 두면 그 순간 배경이 한 번 덜컹한다.
    ///   같은 자리에 두면 전투 시작 때 아무것도 움직이지 않는다.
    /// </summary>
    public void ResetBackgroundToStart()
    {
        if (_bgWidth <= 0f) _bgWidth = CalcBgWidth();
        ResetBackground(_initCamX);
    }

    /// <summary>
    /// 스크롤이 쓸 카메라. 로비·인게임 카메라가 동시에 있을 수 있으므로
    /// Camera.main 대신 SceneDirector 가 지목한 인게임 카메라를 쓴다.
    /// </summary>
    static Camera ResolveCamera()
    {
        var director = SceneDirector.Instance;
        Camera cam = director != null ? director.InGameCam : null;
        return cam != null ? cam : Camera.main;
    }

    // ── 메인 루프 ─────────────────────────────────────────────

    void LateUpdate()
    {
        if (!_active) return;
        if (_cam == null) _cam = ResolveCamera();
        if (_cam == null) return;

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
