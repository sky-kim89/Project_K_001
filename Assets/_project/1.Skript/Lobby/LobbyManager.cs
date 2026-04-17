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
    [SerializeField] string _inGameSceneName = "InGame";

    // ── 런타임 데이터 ─────────────────────────────────────────

    List<StageData> _normalStages;
    List<StageData> _eliteStages;

    BattleMode _currentTab   = BattleMode.Normal;
    int        _currentIndex = 0;

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
        OnStageChanged?.Invoke(CurrentStage);
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void SetTab(BattleMode mode)
    {
        if (_currentTab == mode) return;
        _currentTab   = mode;
        _currentIndex = 0;
        OnStageChanged?.Invoke(CurrentStage);
    }

    public void Navigate(int delta)
    {
        var list = GetList(_currentTab);
        if (list == null || list.Count == 0) return;

        int next = _currentIndex + delta;
        if (next < 0 || next >= list.Count) return;

        _currentIndex = next;
        OnStageChanged?.Invoke(CurrentStage);
    }

    public bool CanNavigate(int delta)
    {
        var list = GetList(_currentTab);
        if (list == null) return false;
        int next = _currentIndex + delta;
        return next >= 0 && next < list.Count;
    }

    public void StartBattle()
    {
        var stage = CurrentStage;
        if (stage == null)
        {
            Debug.LogWarning("[LobbyManager] 스테이지 데이터가 없습니다.");
            return;
        }

        GameSession.Instance.CurrentStage = stage;
        Debug.Log($"[LobbyManager] 전투 시작 → {stage.DisplayName} (웨이브 {stage.Waves.Count}개)");

        if (ScenePreloader.IsInGameReady)
        {
            // Splash 에서 사전 로딩된 InGame 씬을 활성화한다.
            // LobbyManager 는 DontDestroyOnLoad 에 있으므로 씬 전환 중에도 코루틴이 안전하게 실행된다.
            StartCoroutine(TransitionToInGame());
        }
        else
        {
            // 폴백: 일반 Single 로드 — Lobby 포함 현재 씬을 모두 교체
            SceneManager.LoadScene(_inGameSceneName);
        }
    }

    // ── InGame 씬 전환 코루틴 ─────────────────────────────────
    // 흐름:
    //   1. 전환 전 현재 로드된 씬 목록 수집
    //   2. InGame 활성화 (allowSceneActivation = true)
    //   3. InGame 이 완전히 로드될 때까지 대기
    //   4. InGame 을 활성 씬으로 설정
    //   5. 이전 씬(Lobby 등) 언로드
    // ※ LobbyManager 는 DontDestroyOnLoad 에 있으므로 씬 언로드와 무관하게 동작한다.

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

        // ② InGame 씬 활성화
        ScenePreloader.ActivateInGame();

        // ③ InGame 씬이 완전히 로드될 때까지 대기
        while (true)
        {
            Scene inGame = SceneManager.GetSceneByName(_inGameSceneName);
            if (inGame.IsValid() && inGame.isLoaded)
            {
                // ④ InGame 을 활성 씬으로 설정
                SceneManager.SetActiveScene(inGame);
                break;
            }
            yield return null;
        }

        // ⑤ 이전 씬 언로드 (LobbyCanvas 등 Lobby 씬 오브젝트 정리)
        foreach (Scene s in toUnload)
        {
            if (s.IsValid())
                SceneManager.UnloadSceneAsync(s);
        }
    }

    // ── 내부 ─────────────────────────────────────────────────

    List<StageData> GetList(BattleMode mode)
        => mode == BattleMode.Normal ? _normalStages : _eliteStages;
}
