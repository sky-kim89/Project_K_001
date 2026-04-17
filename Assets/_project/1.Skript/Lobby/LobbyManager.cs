using System;
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

        // 스플래시에서 사전 로딩된 씬이 있으면 즉시 전환, 없으면 일반 로드
        if (ScenePreloader.IsInGameReady)
            ScenePreloader.ActivateInGame();
        else
            SceneManager.LoadScene(_inGameSceneName);
    }

    // ── 내부 ─────────────────────────────────────────────────

    List<StageData> GetList(BattleMode mode)
        => mode == BattleMode.Normal ? _normalStages : _eliteStages;
}
