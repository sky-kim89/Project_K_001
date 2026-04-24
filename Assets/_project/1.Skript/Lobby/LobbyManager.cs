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
        // 마지막 클리어 스테이지로 초기 인덱스 설정
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        if (progress != null)
            _currentIndex = Mathf.Clamp(progress.ClearedNormalStages, 0, _normalStages.Count - 1);

        OnStageChanged?.Invoke(CurrentStage);
    }

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
        if (list == null || list.Count == 0) return false;
        int next = _currentIndex + delta;
        if (next < 0 || next >= list.Count) return false;
        return IsStageUnlocked(_currentTab, next + 1);  // stageNumber = index+1
    }

    public void StartBattle()
    {
        if (_isBattleStarting) return;

        var stage = CurrentStage;
        if (stage == null)
        {
            Debug.LogWarning("[LobbyManager] 스테이지 데이터가 없습니다.");
            return;
        }

        // 에너지 체크
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || !items.CanSpend(eItem.Energy, stage.EnergyCost))
        {
            int have = items?.Get(eItem.Energy) ?? 0;
            Debug.LogWarning($"[LobbyManager] 에너지 부족 — 필요: {stage.EnergyCost}, 보유: {have}");
            // TODO: 에너지 부족 팝업 표시
            return;
        }
        items.Spend(eItem.Energy, stage.EnergyCost);
        UserDataManager.Instance.RequestSave();

        _isBattleStarting = true;
        GameSession.Instance.CurrentStage = stage;
        Debug.Log($"[LobbyManager] 전투 시작 → {stage.DisplayName} (에너지 -{stage.EnergyCost}, 웨이브 {stage.Waves.Count}개)");

        if (ScenePreloader.IsInGameReady)
            StartCoroutine(TransitionToInGame());
        else
            StartCoroutine(LoadInGameFallback());
    }

    /// <summary>
    /// 전투 종료 후 로비로 복귀한다.
    /// BattleResultPopup 확인 버튼에서 호출.
    /// </summary>
    public void ReturnToLobby()
    {
        _isBattleStarting = false;
        BattleManager.Instance?.DespawnAllUnits();
        SceneManager.LoadScene(_lobbySceneName);
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
