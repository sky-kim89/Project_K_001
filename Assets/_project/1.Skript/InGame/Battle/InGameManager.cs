using UnityEngine;

// ============================================================
//  InGameManager.cs
//  인게임 씬의 진입점 — 씬 로드 시 자동으로 배틀을 시작한다.
//
//  실행 순서:
//    Awake : PoolController, BattleManager 준비 확인
//    Start : 로딩 팝업 오픈 → BattleManager 이벤트 구독 → StartBattle()
//
//  팝업 흐름:
//    1. 로딩 팝업    : Start() 에서 오픈, OnAlliesReady 이벤트에서 클로즈
//    2. 결과 팝업    : OnVictory / OnDefeat 이벤트에서 오픈
//    3. 일시 정지 팝업: TopBarUI 의 일시 정지 버튼에서 오픈 (PausePopup 참조 전달)
//
//  Inspector 설정:
//    - WaveSetup              : WaveSetupData SO 할당
//    - BattleMode             : 어떤 모드로 시작할지 (기본 Normal)
//    - LoadingPopupPrefab     : LoadingPopup 프리팹 할당
//    - BattleResultPopupPrefab: BattleResultPopup 프리팹 할당
//    - PausePopupPrefab       : PausePopup 프리팹 할당 (TopBarUI 가 공유 참조)
// ============================================================

public class InGameManager : MonoBehaviour
{
    [Header("배틀 설정")]
    [Tooltip("웨이브 구성 데이터 (WaveSetupData SO 할당)")]
    public WaveSetupData WaveSetup;

    [Tooltip("시작할 배틀 모드")]
    public BattleMode StartMode = BattleMode.Normal;

    [Header("자동 시작 딜레이 (초) — 씬 로드 연출 대기용")]
    [Min(0f)]
    public float AutoStartDelay = 0f;

    [Header("스킬 데이터베이스")]
    [Tooltip("패시브 스킬 전체 목록 (PassiveSkillDatabase SO 할당)")]
    [SerializeField] PassiveSkillDatabase _passiveSkillDatabase;

    [Tooltip("액티브 스킬 전체 목록 (ActiveSkillDatabase SO 할당)")]
    [SerializeField] ActiveSkillDatabase _activeSkillDatabase;

    [Header("게임플레이 설정")]
    [Tooltip("인게임 밸런스 수치 중앙 저장소 (GameplayConfig SO 할당)")]
    [SerializeField] GameplayConfig _gameplayConfig;


    // ── Unity 생명주기 ────────────────────────────────────────

    void Awake()
    {
        // 전역 참조 주입 — ValidateDependencies 보다 먼저 실행
        PassiveSkillDatabase.Current = _passiveSkillDatabase;
        ActiveSkillDatabase.Current  = _activeSkillDatabase;
        GameplayConfig.Current       = _gameplayConfig;

        ValidateDependencies();
    }

    void Start()
    {
        // 로딩 팝업 오픈 (장군 스폰 전 화면 가리기)
        if (PopupManager.Instance != null)
            PopupManager.Instance.Open(PopupType.Loading);

        // 배틀 이벤트 구독
        BattleManager.OnAlliesReady += HandleAlliesReady;
        BattleManager.OnVictory     += HandleVictory;
        BattleManager.OnDefeat      += HandleDefeat;

        if (AutoStartDelay > 0f)
            Invoke(nameof(StartBattle), AutoStartDelay);
        else
            StartBattle();
    }

    void OnDestroy()
    {
        BattleManager.OnAlliesReady -= HandleAlliesReady;
        BattleManager.OnVictory     -= HandleVictory;
        BattleManager.OnDefeat      -= HandleDefeat;
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    /// <summary>장군 스폰 완료 → 로딩 팝업 닫기.</summary>
    void HandleAlliesReady()
    {
        if (PopupManager.Instance != null)
            PopupManager.Instance.Close(PopupType.Loading);
    }

    /// <summary>전투 승리 → 스테이지 클리어 기록 후 결과 팝업 오픈.</summary>
    void HandleVictory()
    {
        // 로비에서 선택한 스테이지인 경우에만 진행 기록
        if (GameSession.Instance.HasStage)
        {
            var stage    = GameSession.Instance.CurrentStage;
            var progress = UserDataManager.Instance?.Get<StageProgressData>();
            if (progress != null)
            {
                progress.RecordClear(stage.Mode, stage.StageNumber);
                UserDataManager.Instance.RequestSave();
            }
        }

        if (PopupManager.Instance == null) return;
        var popup = PopupManager.Instance.Open<BattleResultPopup>(PopupType.BattleResult);
        popup?.Setup(true,
            BattleManager.Instance?.Context,
            BattleManager.Instance?.EnemyKillCount ?? 0);
    }

    /// <summary>전투 패배 → 결과 팝업 오픈.</summary>
    void HandleDefeat()
    {
        if (PopupManager.Instance == null) return;
        var popup = PopupManager.Instance.Open<BattleResultPopup>(PopupType.BattleResult);
        popup?.Setup(false,
            BattleManager.Instance?.Context,
            BattleManager.Instance?.EnemyKillCount ?? 0);
    }

    // ── 배틀 시작 ────────────────────────────────────────────

    void StartBattle()
    {
        BattleModeBase mode;

        // 로비에서 스테이지를 선택해서 넘어온 경우
        if (GameSession.Instance.HasStage)
        {
            var stage = GameSession.Instance.CurrentStage;
            mode = CreateMode(stage);
            Debug.Log($"[InGameManager] 배틀 시작 — {stage.DisplayName}, 웨이브 {stage.Waves.Count}개");
        }
        // 에디터에서 직접 WaveSetup 을 할당해 테스트하는 경우
        else
        {
            if (WaveSetup == null || WaveSetup.Waves.Count == 0)
            {
                Debug.LogError("[InGameManager] WaveSetup 이 비어있습니다.");
                return;
            }
            var editorStage = new StageData
            {
                Mode        = StartMode,
                StageNumber = 1,
                Waves       = WaveSetup.Waves,
                GoldReward  = 500,
                StoneReward = 2,
            };
            mode = CreateMode(editorStage);
            Debug.Log($"[InGameManager] 배틀 시작 (에디터 직접) — 모드: {StartMode}, 웨이브: {WaveSetup.Waves.Count}개");
        }

        if (mode == null) return;
        BattleManager.Instance.StartBattle(mode);
    }

    // ── 모드 생성 ─────────────────────────────────────────────

    BattleModeBase CreateMode(StageData stage)
    {
        switch (stage.Mode)
        {
            case BattleMode.Normal:
            case BattleMode.Elite:
                return new NormalMode(stage);

            default:
                Debug.LogError($"[InGameManager] 구현되지 않은 배틀 모드: {stage.Mode}");
                return null;
        }
    }

    // ── 유효성 검사 ───────────────────────────────────────────

    void ValidateDependencies()
    {
        if (BattleManager.Instance == null)
            Debug.LogError("[InGameManager] BattleManager 를 찾을 수 없습니다. Hierarchy 에 배치되어 있는지 확인하세요.");

        if (PoolController.Instance == null)
            Debug.LogError("[InGameManager] PoolController 를 찾을 수 없습니다. Hierarchy 에 배치되어 있는지 확인하세요.");
    }
}
