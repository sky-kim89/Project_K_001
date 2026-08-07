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

public class LobbyManager : Singleton<LobbyManager>
{
    [Header("스테이지 설정")]
    [SerializeField] StageConfig _stageConfig;

    [Header("씬 이름")]
    [SerializeField] string _lobbySceneName  = "Lobby";
    [SerializeField] string _inGameSceneName = "InGame";

    // ── 런타임 데이터 ─────────────────────────────────────────

    List<StageData> _normalStages;
    List<StageData> _eliteStages;

    BattleMode _currentTab       = BattleMode.Normal;
    int        _currentIndex     = 0;
    bool       _isBattleStarting = false;

    // ── 이벤트 ────────────────────────────────────────────────

    public static event Action<StageData> OnStageChanged;

    // ── 프로퍼티 ──────────────────────────────────────────────

    public BattleMode CurrentTab   => _currentTab;
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
    }

    // ── 초기 패널 결정 ────────────────────────────────────────

    /// <summary>
    /// 표시할 패널을 결정한다.
    ///   - RunInProgress = true  → BattlePanel (index 2, 이어하기)
    ///   - RunInProgress = false → MainPanel   (index 5, 장수 선택)
    /// LobbyNavUI.Start()가 같은 프레임에 Switch(_defaultTab)을 호출하므로
    /// 한 프레임 뒤에 실행해 덮어씌워지지 않도록 한다.
    /// </summary>
    IEnumerator SelectInitialPanelNextFrame()
    {
        yield return null;
        SelectInitialPanel();
    }

    void SelectInitialPanel()
    {
        var navUI    = FindObjectOfType<LobbyNavUI>();
        bool runActive = UserDataManager.Instance?.Get<StageProgressData>()?.RunInProgress ?? false;
        navUI?.Switch(runActive ? 2 : 5);
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

    public void StartBattle()
    {
        if (_isBattleStarting) return;

        var progress  = UserDataManager.Instance?.Get<StageProgressData>();

        // 런 시퀀스가 없으면 최초 진입 — 시퀀스 생성
        if (progress != null && progress.EnsureRunSequence())
            UserDataManager.Instance.RequestSave();

        var stageType = progress?.CurrentStageType ?? RunStageType.Normal;

        var stage = CurrentStage;
        if (stage == null)
        {
            Debug.LogWarning("[LobbyManager] 스테이지 데이터가 없습니다.");
            return;
        }

        // 엘리트 스테이지: 스테이지 데이터에 엘리트 플래그 전달
        if (stageType == RunStageType.Elite)
            stage = StageData.AsElite(stage);

        _isBattleStarting = true;
        GameSession.Instance.CurrentStage = stage;
        Debug.Log($"[LobbyManager] 전투 시작 → {stage.DisplayName} [{stageType}] (웨이브 {stage.Waves.Count}개)");

        if (ScenePreloader.IsInGameReady)
            StartCoroutine(TransitionToInGame());
        else
            StartCoroutine(LoadInGameFallback());
    }

    /// <summary>전투 클리어 후 호출. 런 스테이지를 다음으로 진행.</summary>
    public void AdvanceRunStage()
    {
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        progress?.AdvanceRunStage();
        UserDataManager.Instance?.Get<RunShopData>()?.NewStage();
        UserDataManager.Instance?.RequestSave();
    }

    /// <summary>
    /// 전투 종료 후 로비로 복귀한다.
    /// BattleResultPopup 확인 버튼에서 호출.
    /// </summary>
    public void ReturnToLobby()
    {
        _isBattleStarting = false;
        BattleManager.Instance?.DespawnAllUnits();
        _currentIndex = GetLatestAvailableIndex(_currentTab);

        var ids = UserDataManager.Instance.Get<EquipInventoryData>().OwnedIds;
        Debug.Log($"[LobbyManager] 장비 인벤토리 ({ids.Count}개): {string.Join(", ", ids)}");

        SceneManager.sceneLoaded += OnLobbySceneLoaded;
        SceneManager.LoadScene(_lobbySceneName);
    }

    void OnLobbySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLobbySceneLoaded;
        StartCoroutine(SelectInitialPanelNextFrame());
    }

    // ── InGame 씬 전환 코루틴 ─────────────────────────────────
    // 흐름:
    //   1. ScenePreloader 에서 InGameManager 루트 오브젝트 탐색
    //   2. InGame 씬을 활성 씬으로 설정
    //   3. InGameManager 루트 SetActive(true) → Awake/Start 실행 → 배틀 시작
    //   4. 이전 씬(Lobby 등) 언로드
    // ※ InGame 씬의 InGameManager 루트는 Inspector 에서 inactive 로 배치되어야 한다.

    IEnumerator TransitionToInGame()
    {
        // ① 현재 로드된 씬 수집 — InGame 활성화 전에 캡처
        var toUnload = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.isLoaded)
                toUnload.Add(s);
        }

        // ② InGame 씬 활성화 (allowSceneActivation = true)
        ScenePreloader.ActivateInGame();

        // ③ InGame 씬이 완전히 로드될 때까지 대기
        while (true)
        {
            Scene inGame = SceneManager.GetSceneByName(_inGameSceneName);
            if (inGame.IsValid() && inGame.isLoaded)
            {
                SceneManager.SetActiveScene(inGame);
                break;
            }
            yield return null;
        }

        // ④ InGameManager 루트 오브젝트를 찾아 활성화 → Awake/Start 실행 → 배틀 시작
        GameObject inGameRoot = ScenePreloader.FindInGameManagerRoot(_inGameSceneName);
        if (inGameRoot != null)
            inGameRoot.SetActive(true);
        else
            Debug.LogWarning("[LobbyManager] InGameManager 루트를 찾지 못했습니다. Inspector 에서 InGameManager 루트를 inactive 로 설정했는지 확인하세요.");

        yield return null;

        // ⑤ 이전 씬(Lobby 등) 언로드
        foreach (Scene s in toUnload)
        {
            if (s.IsValid())
                SceneManager.UnloadSceneAsync(s);
        }
    }

    // ── InGame 폴백 로드 (ScenePreloader 미준비 시) ───────────
    // ScenePreloader 를 사용하지 않고 InGame 씬을 비동기로 직접 로드한다.
    // 로드 완료 후 InGameManager 루트를 활성화해 배틀을 시작한다.

    IEnumerator LoadInGameFallback()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(_inGameSceneName);
        while (!op.isDone)
            yield return null;

        Scene inGame = SceneManager.GetSceneByName(_inGameSceneName);
        if (inGame.IsValid() && inGame.isLoaded)
            SceneManager.SetActiveScene(inGame);

        GameObject inGameRoot = ScenePreloader.FindInGameManagerRoot(_inGameSceneName);
        if (inGameRoot != null)
            inGameRoot.SetActive(true);
        else
            Debug.LogWarning("[LobbyManager] InGameManager 루트를 찾지 못했습니다 (폴백 경로).");
    }

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
