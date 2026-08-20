using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  LobbyManager.cs
//  로비 전체를 관리하는 Singleton.
//
//  Inspector 설정:
//    StageConfig     : StageConfig SO 할당 필수
//    InGameSceneName : 인게임 씬 이름 (기본 "InGame")
// ============================================================

/// <summary>
/// 로비 → 전투 → 로비 흐름의 상태. 진입 판단은 전부 이 값 하나로 한다.
///
/// ⚠ 예전엔 bool 네 개(_isBattleStarting·_standbyReady·_preparingStandby·…)로 나눠 있었다
///   조합이 16가지라 "지금 준비해도 되나?" 를 매번 다르게 따졌고, 한 조합만 어긋나도
///   대기 부대가 안 서거나 두 번 서는 식으로 조용히 깨졌다.
///
///   Boot ─(첫 패널 결정)→ Idle
///   Idle ─(MainPanel)→ Demo ─(BattlePanel)→ Preparing → Standby
///   Idle ─(BattlePanel)→ Preparing → Standby
///   Standby ─(전투 시작)→ Intro ─(웨이브)→ Battle ─(종료)→ Returning → Preparing → Standby
/// </summary>
public enum LobbyFlow
{
    Boot      = 0,   // 첫 패널이 아직 안 정해졌다 — 아무것도 준비하지 않는다
    Idle      = 1,   // 로비에 있고 전장에는 아무것도 없다
    Demo      = 2,   // MainPanel 배경 데모가 돌고 있다 (더미 부대)
    Preparing = 3,   // 출전 대기를 세우는 중
    Standby   = 4,   // 출전 대기 완료 (배치 부대가 서 있다)
    Intro     = 5,   // 전투 진입 연출 중
    Battle    = 6,   // 웨이브 진행 중
    Returning = 7,   // 전투 종료 후 복귀 처리 중
}

public class LobbyManager : Singleton<LobbyManager>
{
    [Header("스테이지 설정")]
    [SerializeField] StageConfig _stageConfig;

    [Header("씬 이름")]
    [SerializeField] string _lobbySceneName  = "Lobby";
    [SerializeField] string _inGameSceneName = "InGame";
    [Tooltip("최초 실행 자동 진입 시, 이 씬이 내려간 뒤에 전투로 넘어간다.")]
    [SerializeField] string _splashSceneName = "Splash";

    [Header("출전 대기 화면")]
    [Tooltip("아군 슬롯 기준으로 카메라를 오른쪽으로 얼마나 밀지. 클수록 장수가 화면 왼쪽에 붙는다.")]
    [SerializeField] float _standbyCamOffsetX = 5f;
    [Tooltip("대기 뷰 직교 크기. 0 이면 전투 뷰와 같은 크기를 쓴다.")]
    [SerializeField] float _standbyCamSize = 0f;
    [Tooltip("아군 스폰 신호를 받은 뒤 추가로 기다릴 시간 (초).")]
    [SerializeField] float _standbySettleSeconds = 0.2f;
    [Tooltip("스폰 신호가 오지 않을 때 포기하고 진행할 시간 (초). 배치가 비어 있으면 신호가 없다.")]
    [SerializeField] float _standbyReadyTimeout = 3f;

    [Header("전투 진입 연출")]
    [Tooltip("대기 뷰 → 전투 뷰 카메라 이동 시간 (초).")]
    [SerializeField] float _cameraMoveSeconds = 1.0f;
    [Tooltip("카메라가 자리를 잡은 뒤 웨이브 개시까지의 호흡 (초).")]
    [SerializeField] float _battleStartDelay = 1.2f;

    // ── 런타임 데이터 ─────────────────────────────────────────

    List<StageData> _normalStages;
    List<StageData> _eliteStages;

    BattleMode _currentTab       = BattleMode.Normal;
    int        _currentIndex     = 0;

    LobbyFlow  _flow = LobbyFlow.Boot;
    LobbyFlow  _afterClean = LobbyFlow.Idle;   // 청소(Returning) 뒤에 갈 상태
    bool       _startAfterStandby;             // 대기가 서면 곧바로 전투로
    bool       _skipIntro;                     // 카메라 무빙 없이 (최초 실행)

    // ── 이벤트 ────────────────────────────────────────────────

    public static event Action<StageData> OnStageChanged;

    // ── 프로퍼티 ──────────────────────────────────────────────

    public BattleMode CurrentTab   => _currentTab;

    /// <summary>지금 흐름 상태. 모든 진입 판단은 이 값 하나로 한다.</summary>
    public LobbyFlow  Flow => _flow;

    /// <summary>전투로 들어가는 중인가 — 로비 배경 데모가 끼어들면 안 되는 구간.</summary>
    public bool       IsBattleStarting => _flow is LobbyFlow.Intro or LobbyFlow.Battle;
    public int        CurrentIndex => _currentIndex;

    public StageData CurrentStage
    {
        get
        {
            var list = GetList(_currentTab);
            return list != null && _currentIndex < list.Count ? list[_currentIndex] : null;
        }
    }

    // ── Unity 생명주기 ────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        if (_stageConfig == null)
        {
            Debug.LogError("[LobbyManager] StageConfig 가 할당되지 않았습니다.");
            return;
        }

        StageConfig.Current = _stageConfig;

        _normalStages = StageGenerator.GenerateAll(_stageConfig, BattleMode.Normal);
        _eliteStages  = StageGenerator.GenerateAll(_stageConfig, BattleMode.Elite);

        Debug.Log($"[LobbyManager] 스테이지 생성 완료 — 일반 {_normalStages.Count}개, 엘리트 {_eliteStages.Count}개");
    }

    void Start()
    {
        _currentIndex = GetLatestAvailableIndex(BattleMode.Normal);

        // 진행바 표시를 위해 런 시퀀스를 OnStageChanged 발화 전에 미리 확보
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        if (progress != null && progress.EnsureRunSequence())
            UserDataManager.Instance.RequestSave();

        OnStageChanged?.Invoke(CurrentStage);
        StartCoroutine(SelectInitialPanelNextFrame());

        DeploymentData.OnChanged += InvalidateStandby;
    }

    // Singleton<T> 가 OnDestroy 로 인스턴스를 정리한다 — 반드시 base 를 부를 것
    protected override void OnDestroy()
    {
        DeploymentData.OnChanged -= InvalidateStandby;
        base.OnDestroy();
    }

    // ── 초기 패널 결정 ────────────────────────────────────────

    /// <summary>
    /// 표시할 패널을 결정한다.
    ///   - 설치 후 첫 실행       → 패널 없이 자동으로 전투 진입 (AutoStartFirstRun)
    ///   - RunInProgress = true  → BattlePanel (index 2, 이어하기)
    ///   - RunInProgress = false → MainPanel   (index 4, 장수 선택)
    /// LobbyNavUI.Start()가 같은 프레임에 Switch(_defaultTab)을 호출하므로
    /// 한 프레임 뒤에 실행해 덮어씌워지지 않도록 한다.
    /// </summary>
    IEnumerator SelectInitialPanelNextFrame()
    {
        yield return null;

        // 설치 후 첫 실행이면 장수 선택·출전 화면을 건너뛰고 곧장 전투로 들어간다
        if (UserDataManager.Instance.ConsumeFirstLaunch() && CanAutoStartFirstRun())
        {
            yield return StartCoroutine(AutoStartFirstRun());
            yield break;
        }

        SelectInitialPanel();
    }

    void SelectInitialPanel()
    {
        DiscardUnstartedRun();
        if (_flow == LobbyFlow.Boot) SetFlow(LobbyFlow.Idle);

        var navUI    = FindObjectOfType<LobbyNavUI>();
        bool runActive = UserDataManager.Instance?.Get<StageProgressData>()?.RunInProgress ?? false;
        // 유물 탭이 빠지면서 MainPanel 이 5 → 4 로 앞당겨졌다 (LobbyScenePatcher 참조)
        navUI?.Switch(runActive ? 2 : 4);

        // ⚠ 이미 켜져 있던 패널은 OnEnable 이 다시 돌지 않는다
        //   LobbyNavUI 는 기본 탭(BattlePanel)을 첫 프레임에 이미 켠다.
        //   그 패널로 확정되면 여기서 직접 대기 화면을 요청해야 한다.
        if (runActive) EnterStandby();
    }

    /// <summary>
    /// 진행 기록이 있어도 아직 1스테이지에 머물러 있으면 시작 전으로 되돌린다.
    ///
    /// ⚠ 1스테이지는 "아무것도 하지 않은 상태" 와 구분되지 않는다
    ///   장수만 고르고 나갔거나 첫 전투에서 되돌아온 기록을 이어하기로 취급하면,
    ///   플레이어는 고를 기회를 잃은 채 BattlePanel 로 떨어진다.
    ///
    /// ⚠ 배치도 함께 비운다
    ///   RunStarter 는 '첫 빈 슬롯' 에 배치한다. 예전 장수가 슬롯 0 에 남아 있으면
    ///   새로 고른 장수가 슬롯 1 로 들어가 둘이 함께 출전한다.
    /// </summary>
    void DiscardUnstartedRun()
    {
        var udm      = UserDataManager.Instance;
        var progress = udm?.Get<StageProgressData>();
        if (progress == null || !progress.RunInProgress) return;
        if (progress.CurrentRunStage > 0) return;   // 진짜로 진행 중인 런

        progress.RunInProgress = false;
        udm.Get<DeploymentData>()?.SetDefaults();
        udm.SaveAll();

        // 편성이 비었으니 서 있던 대기 화면도 다시 세워야 한다
        InvalidateStandby();

        Debug.Log("[LobbyManager] 1스테이지에 머문 기록 — 시작 전으로 되돌리고 장수 선택부터 시작합니다.");
    }

    // ── 최초 실행 자동 진입 ───────────────────────────────────
    //
    //  설치 직후 첫 화면은 "무엇을 고를지" 가 아니라 "이 게임이 뭔지" 여야 한다.
    //  장수 하나를 자동으로 골라 1스테이지까지 밀어 넣고, 조작은 튜토리얼이 가르친다.
    //
    //  ⚠ 직업이 아니라 이름을 박는다
    //    예전엔 RollCandidate(UnitJob.Knight) 로 "기사 아무나" 를 뽑았다.
    //    그러면 첫 판의 장수가 매번 달라져 튜토리얼 문구·연출을 그 사람에게
    //    맞출 수가 없다. 직업은 이름 해시가 정하므로(UnitJobRoller) 이름을
    //    고정하면 직업·스탯·스킬까지 전부 같은 사람이 나온다.
    //
    //  ⚠ 세이브가 이미 있으면 절대 타지 않는 경로다
    //    ConsumeFirstLaunch() 가 한 번만 true 를 주고, 아래 조건이 한 번 더 거른다.

    /// <summary>
    /// 최초 실행에서 자동으로 주는 장수. UnitData 의 이름 풀에 있는 이름이어야 한다.
    /// 직업은 이 이름이 정한다 — 바꾸면 첫 판의 직업도 같이 바뀐다.
    /// </summary>
    const string FirstRunHeroName = "아르투어";

    bool CanAutoStartFirstRun()
    {
        var udm = UserDataManager.Instance;
        if (udm.Get<UnitData>().Units.Count > 0) return false;              // 이미 장수가 있다
        if (udm.Get<StageProgressData>().CurrentRunStage > 0) return false; // 진행 중인 런이 있다
        return true;
    }

    IEnumerator AutoStartFirstRun()
    {
        // ⚠ 스플래시가 완전히 사라진 뒤에 전투로 넘어가야 한다
        //   SplashBootstrap 은 '로딩 팝업 닫기 → 자기 씬 언로드' 로 끝난다.
        //   그 전에 우리가 로딩 팝업을 열면 스플래시가 그것을 닫아 버려
        //   전환 도중에 로비가 한 장면 드러난다.
        float timeout = Time.realtimeSinceStartup + 3f;
        while (SceneManager.GetSceneByName(_splashSceneName).isLoaded
               && Time.realtimeSinceStartup < timeout)
            yield return null;

        // 로비가 한 장면도 노출되지 않도록 로딩 팝업으로 덮은 뒤 전환한다
        PopupManager.Instance?.Open(PopupType.Loading);

        // ⚠ Boot 를 여기서 푼다 — 안 풀면 첫 실행이 무한 로딩으로 멈춘다
        //   Boot 를 Idle 로 바꾸는 곳은 SelectInitialPanel() 한 곳뿐인데,
        //   자동 진입 경로는 그 함수를 건너뛴다(위 yield break).
        //   그래서 아래 StartBattle → BeginBattleFromStandby 가 Boot 를 만나
        //   "전투 시작 무시" 경고만 남기고 아무 일도 하지 않았다.
        //   Preparing 에 못 들어가니 PrepareRoutine 이 열어 둔 로딩 팝업을
        //   닫아 줄 사람이 없어 그대로 화면이 덮인 채 멈춘다.
        //
        //   Boot 의 뜻은 "첫 패널이 아직 안 정해졌다" 다. 자동 진입은 패널을
        //   쓰지 않기로 이미 정한 것이므로 여기가 Boot 를 벗어날 자리가 맞다.
        //   화면은 방금 연 로딩 팝업이 덮고 있어 로비가 드러나지 않는다.
        if (_flow == LobbyFlow.Boot) SetFlow(LobbyFlow.Idle);

        RunStarter.BeginRun(RunStarter.CandidateNamed(FirstRunHeroName));

        Debug.Log($"[LobbyManager] 최초 실행 — {FirstRunHeroName} 자동 선택 후 1스테이지로 바로 진입");
        StartBattle(withIntro: false);
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Comma))  NavigateEditor(-1);
        if (Input.GetKeyDown(KeyCode.Period)) NavigateEditor(1);
    }

    void NavigateEditor(int delta)
    {
        var list = GetList(_currentTab);
        if (list == null || list.Count == 0) return;
        int next = _currentIndex + delta;
        if (next < 0 || next >= list.Count) return;
        _currentIndex = next;
        OnStageChanged?.Invoke(CurrentStage);
        Debug.Log($"[Editor] 스테이지 이동 → {CurrentStage?.DisplayName}");
    }
#endif

    // ── 공개 API ─────────────────────────────────────────────

    public void SetTab(BattleMode mode)
    {
        if (_currentTab == mode) return;
        if (mode == BattleMode.Elite && !IsEliteUnlocked())
        {
            Debug.Log("[LobbyManager] 엘리트 탭 잠금 — 일반 스테이지 5 클리어 필요");
            return;
        }
        _currentTab   = mode;
        _currentIndex = GetLatestAvailableIndex(mode);
        OnStageChanged?.Invoke(CurrentStage);
    }

    /// <summary>환생 후 1스테이지로 초기화. OnStageChanged 이벤트를 발행해 StageSelectUI를 갱신한다.</summary>
    public void ResetToFirstStage()
    {
        _currentIndex = 0;
        OnStageChanged?.Invoke(CurrentStage);
    }

    /// <summary>
    /// 지금 도전할 스테이지를 확정해 GameSession 에 올린다.
    ///
    /// ⚠ 전투를 누르는 순간이 아니라 '대기 화면을 세울 때' 부터 필요하다
    ///   InGameManager 는 GameSession.HasStage 를 보고 배틀 모드를 만든다.
    ///   비어 있으면 에디터 테스트용 분기로 빠져 WaveSetup 을 찾다가 실패하고,
    ///   아무 일도 일어나지 않는다 — 출전 화면에 장수가 한 명도 안 서던 이유다.
    /// </summary>
    StageData ResolveStageIntoSession()
    {
        var progress = UserDataManager.Instance?.Get<StageProgressData>();

        // 런 시퀀스가 없으면 최초 진입 — 시퀀스 생성
        if (progress != null && progress.EnsureRunSequence())
            UserDataManager.Instance.RequestSave();

        var stage = CurrentStage;
        if (stage == null)
        {
            Debug.LogWarning("[LobbyManager] 스테이지 데이터가 없습니다.");
            return null;
        }

        // 엘리트 스테이지: 스테이지 데이터에 엘리트 플래그 전달
        if ((progress?.CurrentStageType ?? RunStageType.Normal) == RunStageType.Elite)
            stage = StageData.AsElite(stage);

        GameSession.Instance.CurrentStage = stage;
        return stage;
    }

    /// <summary>
    /// 전투를 시작한다.
    ///
    /// withIntro = true  : 출전 대기 화면 → 카메라 무빙 → 웨이브 (장수 선택 직후 · 출전 화면)
    /// withIntro = false : 곧장 전투 (최초 실행 자동 진입 — 튜토리얼까지 최단 경로)
    /// </summary>
    public void StartBattle(bool withIntro = true)
    {
        var stage = ResolveStageIntoSession();
        if (stage == null) return;

        Debug.Log($"[LobbyManager] 전투 시작 → {stage.DisplayName} (웨이브 {stage.Waves.Count}개)");

        // 대기를 세운 뒤 이어서 들어간다. 상태 전이는 BeginBattleFromStandby 가 맡는다.
        _skipIntro = !withIntro;
        BeginBattleFromStandby();
    }

    // ══════════════════════════════════════════════════════════
    //  흐름 (LobbyFlow) — 이 구역이 로비↔전투 전환의 전부다
    //
    //  ■ 상태 하나가 세 가지를 함께 정한다
    //      LobbyFlow  : 지금 어느 단계인가
    //      BattleArena: 전장에 무엇이 살아 있는가 (None / Demo / Real)
    //      Present    : 화면을 누가 그리는가 (LobbyOnly / ArenaBehindUI / Battle)
    //    셋을 호출부마다 따로 세팅하던 것이 문제였다. 이제 ApplyWorld() 한 곳에서
    //    상태에 맞는 조합을 통째로 적용한다 — 조합이 어긋날 여지를 없앤다.
    //
    //  ■ 허용 전이
    //      Boot ──────────────► Idle            첫 패널 결정
    //      Idle ◄────────────► Demo            MainPanel 켜짐/꺼짐
    //      Idle·Demo ─────────► Preparing ──► Standby
    //      Standby ───────────► Intro ──► Battle ──► Returning ──► Preparing
    //      Standby ───────────► Idle            편성 변경(무효화)
    //    그 외 요청은 경고를 남기고 무시한다. 조용히 통과시키면 두 번 스폰되거나
    //    방금 세운 부대가 지워진다.
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 상태를 바꾼다. 화면·전장·코루틴은 전부 OnEnterFlow 가 처리한다.
    /// ⚠ 상태 밖에서 Present / Arena.Open / Arena.Close 를 직접 부르지 말 것.
    /// </summary>
    void SetFlow(LobbyFlow next)
    {
        if (_flow == next) return;
        Debug.Log($"[LobbyFlow] {_flow} → {next}");
        _flow = next;
        OnEnterFlow(next);
    }

    /// <summary>
    /// 각 상태가 '자기 일' 을 한다. 상태마다 책임은 정확히 하나다.
    ///
    ///   Idle      화면만 로비로
    ///   Demo      데모 판 열기 (더미 스폰은 LobbyDemoBattle 이)
    ///   Preparing 아군 스폰 — 그것만
    ///   Standby   대기 화면 표시 (카메라·배경 정리)
    ///   Intro     카메라 무빙
    ///   Battle    웨이브 개시
    ///   Returning 전장 청소 — 아군·적군·이펙트를 지우는 곳은 여기뿐
    /// </summary>
    void OnEnterFlow(LobbyFlow flow)
    {
        var director = SceneDirector.Instance;

        // 배경음이 바뀌는 지점은 "로비 ↔ 전투" 하나뿐이다.
        // 흐름이 그 경계를 이미 알고 있으니 여기 한 줄로 끝낸다 —
        // 화면마다 PlayBgm 을 뿌리면 로비 안에서 곡이 계속 처음부터 다시 돈다.
        // (Boot 은 아직 아무것도 안 정해진 상태라 건드리지 않는다)
        if (flow != LobbyFlow.Boot)
            AudioManager.Instance?.PlayBgm(
                flow is LobbyFlow.Intro or LobbyFlow.Battle ? BgmKey.InGame : BgmKey.Lobby);

        Debug.Log($"[LobbyFlow] OnEnterFlow({flow})");
        switch (flow)
        {
            case LobbyFlow.Boot:
                break;

            case LobbyFlow.Idle:
                director?.Present(PresentMode.LobbyOnly);
                break;

            case LobbyFlow.Demo:
                director?.Present(PresentMode.ArenaBehindUI);
                BattleArena.Ensure().Open(ArenaKind.Demo);
                break;

            case LobbyFlow.Preparing:
                director?.Present(PresentMode.ArenaBehindUI);
                StartCoroutine(PrepareRoutine());
                break;

            case LobbyFlow.Standby:
                director?.Present(PresentMode.ArenaBehindUI);
                ShowStandbyView();
                break;

            case LobbyFlow.Intro:
                director?.Present(PresentMode.Battle);
                StartCoroutine(IntroRoutine());
                break;

            case LobbyFlow.Battle:
                director?.Present(PresentMode.Battle);
                SceneDirector.Instance?.InGame?.BeginWaves();
                break;

            case LobbyFlow.Returning:
                director?.Present(PresentMode.ArenaBehindUI);
                Clean();          // 동기 — 이 상태는 프레임을 넘기지 않는다
                break;
        }
    }

    // ── 데모 아레나 (LobbyDemoBattle 이 알려 준다) ────────────

    /// <summary>데모를 돌려도 되는 상태인가. 준비·전투·복귀 중에는 끼어들면 안 된다.</summary>
    public bool CanRunDemo
        => _flow is LobbyFlow.Boot or LobbyFlow.Idle or LobbyFlow.Demo or LobbyFlow.Standby;

    /// <summary>배경 데모 시작/종료를 흐름에 반영한다.</summary>
    public void NotifyDemoRunning(bool running)
    {
        if (running)
        {
            if (CanRunDemo) SetFlow(LobbyFlow.Demo);
            return;
        }

        // 데모가 멈췄다 — 더미를 치우고 Idle 로. 청소는 Returning 이 한다.
        // (이미 다음 단계로 넘어갔다면 그쪽이 알아서 하므로 건드리지 않는다)
        if (_flow != LobbyFlow.Demo) return;

        _afterClean = LobbyFlow.Idle;
        SetFlow(LobbyFlow.Returning);
    }

    // ── 외부 요청 (상태만 바꾼다) ─────────────────────────────

    /// <summary>출전 대기를 요청한다. 실제 작업은 Preparing 상태가 한다.</summary>
    public void EnterStandby()
    {
        //   Boot    — 첫 패널이 아직 안 정해졌다. 여기서 세우면 다음 프레임에 버려진다.
        //   Standby — 이미 서 있다. 탭을 오갈 때마다 세우면 장수가 재소환된다.
        //   그 외    — 준비·전투·복귀 중이라 끼어들면 안 된다.
        if (_flow is not (LobbyFlow.Idle or LobbyFlow.Demo)) return;

        // 데모가 돌고 있었다면 전장을 비우고 나서 세운다 (청소는 Returning 이 한다)
        if (_flow == LobbyFlow.Demo) { _afterClean = LobbyFlow.Preparing; SetFlow(LobbyFlow.Returning); return; }

        SetFlow(LobbyFlow.Preparing);
    }

    /// <summary>출전 대기 → 전투. BattlePanel 의 전투 시작 버튼이 부른다.</summary>
    public void BeginBattleFromStandby()
    {
        if (_flow == LobbyFlow.Standby) { SetFlow(LobbyFlow.Intro); return; }

        // 아직 안 서 있으면 세우고 이어서 들어간다
        if (_flow is LobbyFlow.Idle or LobbyFlow.Demo)
        {
            _startAfterStandby = true;
            EnterStandby();
            return;
        }

        Debug.LogWarning($"[LobbyFlow] 전투 시작 무시 — 지금 상태 {_flow}");
    }

    /// <summary>
    /// 서 있던 대기 부대가 낡았다고 표시하고 다시 세운다 (편성 변경 등).
    /// ⚠ 여기서 전장을 지우지 않는다 — 지우는 곳은 Returning 하나뿐이다.
    /// </summary>
    public void InvalidateStandby()
    {
        if (_flow != LobbyFlow.Standby) return;

        _flow = LobbyFlow.Idle;   // 화면은 그대로 두고 표시만 낡음으로
        SetFlow(LobbyFlow.Preparing);
    }

    /// <summary>
    /// 전투가 끝났다. 결과·환생 팝업, 용병 상점이 부른다 (호출 지점이 네 곳이라 중복이 잦다).
    /// 청소는 Returning 상태가 하고, 그 뒤 패널 결정으로 이어진다.
    /// </summary>
    public void ReturnToLobby()
    {
        if (_flow is not (LobbyFlow.Intro or LobbyFlow.Battle))
        {
            Debug.Log($"[LobbyFlow] ReturnToLobby 무시 — 이미 {_flow}");
            return;
        }

        _afterClean = LobbyFlow.Idle;   // 청소 후 패널 결정으로
        SetFlow(LobbyFlow.Returning);
    }

    // ── 상태별 작업 ───────────────────────────────────────────

    /// <summary>
    /// Preparing — 아군 스폰. 그것만 한다.
    ///
    /// 화면에 적이 없으면 UnitMovementSystem 이 아군을 Idle 로 세워 두므로,
    /// 스폰만 해 두면 장수들이 제자리에 서 있는 그림이 그대로 나온다.
    /// </summary>
    IEnumerator PrepareRoutine()
    {
        var director = SceneDirector.Ensure();

        // 로딩 팝업 프리팹은 InGame 씬의 PopupManager 가 들고 있다 — 상주 먼저.
        yield return director.EnsureInGameResident();

        // ⚠ 판이 이미 열려 있으면 로딩 팝업을 띄우지 않는다
        //   Open() 은 새로 연 경우에만 true 다. 장비 교체·등급업·용병 고용처럼
        //   HeroDetailPopup 에서 무언가를 바꾸면 DeploymentData.OnChanged →
        //   InvalidateStandby → 여기로 돌아오는데, 그때는 전장이 그대로 서 있어
        //   덮을 것이 없다. 그런데도 로딩을 띄우면 팝업을 닫을 때마다
        //   "로딩 → 사라짐" 이 번쩍여 마치 렉이 걸린 것처럼 보인다.
        //   빈 벌판이 드러나는 경우(첫 진입·데모 정리 직후)에만 덮는다.
        bool cover = BattleArena.Ensure().Open(ArenaKind.Real);
        if (cover) PopupManager.Instance?.Open(PopupType.Loading);

        ResolveStageIntoSession();
        director.InGame?.PrepareStandby();

        yield return StartCoroutine(WaitForAlliesStanding());

        if (cover) PopupManager.Instance?.Close(PopupType.Loading);

        SetFlow(LobbyFlow.Standby);
    }

    /// <summary>Standby — 대기 화면 표시. 스폰은 이미 끝났고 여기서는 보여 주기만 한다.</summary>
    void ShowStandbyView()
    {
        var director = SceneDirector.Ensure();

        // 대기 중에는 아무도 카메라를 건드리면 안 된다
        BattleScrollManager.Instance?.StopScroll();
        ArenaCameraRig.Snap(director.InGameCam, StandbyCamX(), StandbyCamSize());
        BattleScrollManager.Instance?.ResetBackgroundToStart();

        WarnIfStandbyEmpty();

        // 대기를 거치지 않고 곧장 들어가야 하는 요청이 있었으면 여기서 이어 간다
        if (!_startAfterStandby) return;
        _startAfterStandby = false;
        SetFlow(_skipIntro ? LobbyFlow.Battle : LobbyFlow.Intro);
    }

    /// <summary>Intro — 카메라 무빙. 끝나면 Battle 로 넘긴다.</summary>
    IEnumerator IntroRoutine()
    {
        var director = SceneDirector.Ensure();

        // ⚠ 배경은 건드리지 않는다 — 대기에서 시작 위치에 깔아 뒀고
        //   StartScroll 도 같은 자리를 쓰므로 웨이브가 시작돼도 움직이지 않는다.
        yield return ArenaCameraRig.MoveTo(
            director.InGameCam, ArenaCameraRig.HomeX, ArenaCameraRig.HomeSize, _cameraMoveSeconds);

        yield return new WaitForSecondsRealtime(_battleStartDelay);

        SetFlow(LobbyFlow.Battle);
    }

    /// <summary>
    /// Returning — 전장 청소. 아군·적군·발사체·이펙트를 지우는 곳은 여기 하나뿐이다.
    /// 청소가 끝나면 _afterClean 이 가리키는 상태로 넘어간다.
    /// </summary>
    /// <summary>
    /// Returning — 전장 청소. 아군·적군·발사체·이펙트를 지우는 곳은 여기 하나뿐이다.
    /// 청소가 끝나면 _afterClean 이 가리키는 상태로 곧바로 넘어간다.
    ///
    /// ⚠ 코루틴이 아니라 동기다
    ///   디스폰은 SetActive(false) / DestroyEntity 라 그 자리에서 끝난다.
    ///   프레임을 넘길 이유가 없는데 넘기면, 그 한 프레임 동안 상태가 Returning 이라
    ///   그 사이 들어온 요청(패널 OnEnable 의 EnterStandby 등)이 거부된 뒤 사라진다.
    ///   요청은 큐에 쌓이지 않으므로 그대로 "대기가 안 서는" 버그가 된다.
    /// </summary>
    void Clean()
    {
        Debug.Log("[LobbyFlow] Returning — 전장 청소");
        LobbyDemoBattle.Instance?.End();
        BattleArena.Ensure().Close();

        _currentIndex = GetLatestAvailableIndex(_currentTab);

        if (_afterClean == LobbyFlow.Preparing)
        {
            _afterClean = LobbyFlow.Idle;
            SetFlow(LobbyFlow.Preparing);
            return;
        }

        // 전투 후 복귀 — 어느 패널로 갈지는 진행 데이터가 정한다 (환생했으면 MainPanel)
        SetFlow(LobbyFlow.Idle);
        OnStageChanged?.Invoke(CurrentStage);
        SelectInitialPanel();
    }

    /// <summary>
    /// 아군이 전장에 설 때까지 기다린다.
    ///
    /// ⚠ 고정 시간으로 기다리면 안 된다
    ///   준비 루틴은 외형 캐시를 비우고 Resources.UnloadUnusedAssets 를 돌린다.
    ///   기기·스테이지에 따라 시간이 들쭉날쭉해서, 짧게 잡으면 가림막이 먼저 걷히며
    ///   빈 벌판이 보인다. 스폰 완료 신호를 기다리고 시간은 안전망으로만 쓴다.
    /// </summary>
    IEnumerator WaitForAlliesStanding()
    {
        bool ready = false;
        void OnReady() => ready = true;

        BattleManager.OnAlliesReady += OnReady;

        float deadline = Time.realtimeSinceStartup + _standbyReadyTimeout;
        while (!ready && Time.realtimeSinceStartup < deadline)
            yield return null;

        BattleManager.OnAlliesReady -= OnReady;

        // 스폰 직후 한 박자 — 외형 조립이 끝난 뒤에 화면을 열어 준다
        yield return new WaitForSecondsRealtime(_standbySettleSeconds);
    }

    /// <summary>전장에 서 있는 아군 수.</summary>
    static int CountStandingAllies()
    {
        int n = 0;
        foreach (var b in FindObjectsByType<UnitRuntimeBridge>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (b is GeneralRuntimeBridge) n++;
        return n;
    }

    void WarnIfStandbyEmpty()
    {
        if (CountStandingAllies() > 0) return;

        var deploy = UserDataManager.Instance?.Get<DeploymentData>()?.GetDeployedUnits();
        Debug.LogWarning(
            "[LobbyManager] 출전 대기에 선 장수가 없습니다 — " +
            $"배치 {deploy?.Count ?? 0}명 / 스테이지 {(GameSession.Instance.HasStage ? GameSession.Instance.CurrentStage.DisplayName : "없음")} / " +
            $"InGameManager {(SceneDirector.Instance?.InGame != null ? "연결됨" : "없음")}");
    }

    // ── 런 진행 ───────────────────────────────────────────────

    /// <summary>전투 클리어 후 호출. 런 스테이지를 다음으로 진행.</summary>
    public void AdvanceRunStage()
    {
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        progress?.AdvanceRunStage();
        UserDataManager.Instance?.Get<RunShopData>()?.NewStage();
        UserDataManager.Instance?.RequestSave();
    }

    // ── 대기 뷰 카메라 ────────────────────────────────────────

    /// <summary>대기 뷰 카메라 X — 아군 슬롯이 화면 왼쪽(출전 카드 자리)에 오도록 민다.</summary>
    float StandbyCamX()
    {
        var spawner = BattleManager.Instance?.AllySpawner;
        if (spawner == null || spawner.SpawnPoints.Count == 0)
            return ArenaCameraRig.HomeX + _standbyCamOffsetX;

        float sum = 0f;
        int   n   = 0;
        foreach (var t in spawner.SpawnPoints)
        {
            if (t == null) continue;
            sum += t.position.x;
            n++;
        }
        if (n == 0) return ArenaCameraRig.HomeX + _standbyCamOffsetX;

        return sum / n + _standbyCamOffsetX;
    }

    float StandbyCamSize()
        => _standbyCamSize > 0f ? _standbyCamSize : ArenaCameraRig.HomeSize;

    // ── 내부 ─────────────────────────────────────────────────

    /// <summary>해당 모드에서 진입 가능한 최신 스테이지의 인덱스를 반환한다.
    /// 클리어 수 = 다음 스테이지 인덱스 (예: 8클리어 → index 8 = 9스테이지).</summary>
    int GetLatestAvailableIndex(BattleMode mode)
    {
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        int cleared  = mode == BattleMode.Normal
            ? (progress?.ClearedNormalStages ?? 0)
            : (progress?.ClearedEliteStages  ?? 0);
        var list = GetList(mode);
        return Mathf.Clamp(cleared, 0, list != null ? list.Count - 1 : 0);
    }

    bool IsStageUnlocked(BattleMode mode, int stageNumber)
    {
        var p = UserDataManager.Instance?.Get<StageProgressData>();
        return p?.IsUnlocked(mode, stageNumber) ?? stageNumber == 1;
    }

    bool IsEliteUnlocked()
    {
        var p = UserDataManager.Instance?.Get<StageProgressData>();
        return p?.IsEliteUnlocked ?? false;
    }

    List<StageData> GetList(BattleMode mode)
        => mode == BattleMode.Normal ? _normalStages : _eliteStages;
}
