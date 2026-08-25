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
    float _battleStartTime;

    [Header("배틀 설정")]
    [Tooltip("웨이브 구성 데이터 (WaveSetupData SO 할당)")]
    public WaveSetupData WaveSetup;

    [Tooltip("시작할 배틀 모드")]
    public BattleMode StartMode = BattleMode.Normal;

    [Header("자동 시작 딜레이 (초) — 씬 로드 연출 대기용")]
    [Min(0f)]
    public float AutoStartDelay = 0f;

    // ── Unity 생명주기 ────────────────────────────────────────

    void Awake()
    {
        ValidateDependencies();
    }

    /// <summary>
    /// true 면 Start 에서 스스로 전투를 시작하지 않는다.
    ///
    /// ⚠ 씬 상주 모델에서 필요하다
    ///   Lobby·InGame 이 함께 떠 있으면 이 오브젝트는 '전투 스위치' 가 아니라
    ///   그냥 상주하는 매니저가 된다. 켜지자마자 전투를 시작해 버리면
    ///   출전 대기 화면이 성립하지 않는다. 진행은 LobbyManager 가 잡는다.
    /// </summary>
    public static bool ExternallyDriven;

    void Start()
    {
        // 배틀 이벤트 구독 — 승패 처리·보상은 외부 주도 여부와 무관하게 여기서 한다
        BattleManager.OnAlliesReady += HandleAlliesReady;
        BattleManager.OnVictory     += HandleVictory;
        BattleManager.OnDefeat      += HandleDefeat;

        if (ExternallyDriven) return;

        // 로딩 팝업 오픈 (장군 스폰 전 화면 가리기)
        if (PopupManager.Instance != null)
            PopupManager.Instance.Open(PopupType.Loading);

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

    /// <summary>장군 스폰 완료 → 로딩 팝업 닫기 + 전투 타이머 시작.</summary>
    void HandleAlliesReady()
    {
        // 외부 주도(출전 대기)에서는 아군이 섰다고 가림막을 걷으면 안 된다 —
        // 카메라를 대기 뷰로 옮긴 뒤 LobbyManager 가 닫는다.
        if (!ExternallyDriven && PopupManager.Instance != null)
            PopupManager.Instance.Close(PopupType.Loading);
        _battleStartTime = Time.time;
    }

    /// <summary>전투 승리 → 스테이지 클리어 기록 후 결과 팝업 오픈.</summary>
    void HandleVictory()
    {
        var context = BattleManager.Instance?.Context;

        // 로비에서 선택한 스테이지인 경우에만 진행 기록
        if (GameSession.Instance.HasStage)
        {
            var stage      = GameSession.Instance.CurrentStage;
            var unitData   = UserDataManager.Instance.Get<UnitData>();
            var deployData = UserDataManager.Instance.Get<DeploymentData>();
            var deployed   = deployData?.GetDeployedUnits();

            var   runData  = UserDataManager.Instance?.Get<RunAbilityData>();
            float expBonus = AbilityApplier.GetExpBonusRatio(runData?.HeldAbilities, AbilityDatabase.Current)
                           + TraitApplier.GetExpGainBonus(
                                 UserDataManager.Instance?.Get<RunTraitData>(), TraitDatabase.Current);
            int   expReward = Mathf.RoundToInt(stage.ExpReward * (1f + expBonus));

            foreach (var unit in unitData.Units)
            {
                if (deployed != null && !deployed.Contains(unit.UnitName)) continue;

                int gained = unitData.AddUnitExp(unit.UnitName, expReward);
                if (gained > 0)
                    Debug.Log($"[InGameManager] {unit.UnitName} 레벨 업! → Lv.{unit.Level} (+{gained})");
                context?.ExpGains.Add(new BattleContext.UnitExpGain
                {
                    UnitName     = unit.UnitName,
                    ExpGained    = expReward,
                    LevelsGained = gained,
                    NewLevel     = unit.Level,
                });
            }

            // 비박스 보상은 팝업 표시 여부와 무관하게 즉시 지급
            if (context != null)
            {
                foreach (var reward in context.PendingRewards)
                {
                    if (!reward.Item.IsBoxType())
                        RewardOpener.Commit(reward, context.StageLevel);
                }
            }

            UserDataManager.Instance.RequestSave();
        }

        // 전투 경과 시간 기록
        if (context != null)
            context.BattleElapsedSeconds = Time.time - _battleStartTime;

        // 전투 통계 스냅샷
        if (context != null)
        {
            var tracker = BattleStatsTracker.Instance;
            if (tracker != null)
                foreach (var entry in tracker.GetAllEntries())
                    context.CombatStats.Add(entry);
        }

        if (PopupManager.Instance == null) return;
        var popup = PopupManager.Instance.Open<BattleResultPopup>(PopupType.BattleResult);
        popup?.Setup(true, context, BattleManager.Instance?.EnemyKillCount ?? 0,
            onConfirmed: OpenAbilitySelectOrReturnToLobby);
    }

    /// <summary>결과 팝업 확인 후 — 어빌리티를 뽑을 수 있으면 선택 팝업, 아니면 바로 로비.</summary>
    void OpenAbilitySelectOrReturnToLobby()
    {
        var db                = AbilityDatabase.Current;
        var runAbility        = UserDataManager.Instance?.Get<RunAbilityData>();
        var reincarnationData = UserDataManager.Instance?.Get<ReincarnationData>();

        if (db != null && runAbility != null && PopupManager.Instance != null)
        {
            var choices = AbilityPicker.Pick(db, runAbility);
            if (choices.Length > 0)
            {
                var abilityPopup = PopupManager.Instance.Open<AbilitySelectPopup>(PopupType.AbilitySelect);
                abilityPopup?.Setup(choices, chosen =>
                {
                    runAbility.AddAbility(chosen.Id);
                    FinishStageClear();
                }, db, runAbility, reincarnationData);
                return;
            }
        }

        // 선택지가 없을 때는 선택 없이 클리어 처리
        FinishStageClear();
    }

    /// <summary>
    /// 어빌리티 선택까지 끝난 뒤의 마무리 —
    /// 엘리트 스테이지였으면 용병 고용 팝업을 한 번 띄우고, 그 다음 로비로 돌아간다.
    /// </summary>
    void FinishStageClear()
    {
        // ⚠ 스테이지 타입은 RecordStageClear() 보다 먼저 읽어야 한다.
        //   그 안의 AdvanceRunStage() 가 인덱스를 올려 버리면 "다음" 스테이지 타입이 잡힌다.
        var  progress = UserDataManager.Instance?.Get<StageProgressData>();
        bool wasElite = GameSession.Instance.HasStage
                     && progress != null
                     && progress.CurrentStageType == RunStageType.Elite;

        RecordStageClear();
        UserDataManager.Instance?.RequestSave();

        if (wasElite && PopupManager.Instance != null)
        {
            var merc = PopupManager.Instance.Open<MercenaryShopPopup>(
                PopupType.MercenaryShop, onClose: () => LobbyManager.Instance.ReturnToLobby());
            if (merc != null)
            {
                // 엘리트 보상으로 주는 고용이라 골드를 받지 않는다.
                merc.SetupAsReward();
                return;
            }
        }

        LobbyManager.Instance.ReturnToLobby();
    }

    void RecordStageClear()
    {
        if (!GameSession.Instance.HasStage) return;
        var stage    = GameSession.Instance.CurrentStage;
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        progress?.RecordClear(stage.Mode, stage.StageNumber);
        progress?.AdvanceRunStage();
        UserDataManager.Instance?.Get<RunShopData>()?.NewStage();

        TryUnlockNextDifficulty(stage);
    }

    /// <summary>
    /// 일정 스테이지를 넘기면 지금 등급을 "해 봤다" 로 기록해 다음 난이도를 연다.
    ///
    /// ⚠ 예전엔 아무도 이 기록을 남기지 않았다
    ///   DifficultyData.RecordClear 를 부르는 곳이 치트 에디터뿐이라, 정상 플레이로는
    ///   난이도가 영원히 '출정' 하나에 묶여 있었다. 해금 UI 는 멀쩡히 돌아가고 있어서
    ///   조건을 못 채운 것처럼 보였을 뿐이다.
    ///
    /// ⚠ 30스테이지 완주를 조건으로 두면 안 된다
    ///   후반 블록은 환생 보너스 없이 클리어 불가로 설계돼 있는데, 환생 포인트 배율은
    ///   높은 난이도에서 나온다. 완주를 요구하면 서로가 서로의 전제가 되어 아무도
    ///   첫 해금을 못 한다. 그래서 '완주' 가 아니라 '충분히 갔다'(기본 20)로 끊는다.
    /// </summary>
    static void TryUnlockNextDifficulty(StageData stage)
    {
        if (stage.Mode != BattleMode.Normal) return;

        int unlockStage = StageConfig.Current != null
            ? StageConfig.Current.DifficultyUnlockStage : 20;
        if (stage.StageNumber < unlockStage) return;

        var diff = UserDataManager.Instance?.Get<DifficultyData>();
        if (diff == null) return;

        int before = diff.ClearedTierIndex;
        diff.RecordClear(diff.SelectedTier);
        if (diff.ClearedTierIndex != before)
            Debug.Log($"[InGameManager] 난이도 해금 — {diff.SelectedTier} 등급 {unlockStage}스테이지 도달");
    }

    /// <summary>전투 패배 → 통계 스냅샷 후 유닛 디스폰, 환생 팝업 오픈.</summary>
    void HandleDefeat()
    {
        // context/killCount 를 먼저 캡처 — DespawnAllUnits() 가 _context 를 null 로 초기화하기 때문
        var context   = BattleManager.Instance?.Context;
        int killCount = BattleManager.Instance?.EnemyKillCount ?? 0;

        // 전투 경과 시간 기록
        if (context != null)
            context.BattleElapsedSeconds = Time.time - _battleStartTime;

        // 전투 통계 스냅샷
        if (context != null)
        {
            var tracker = BattleStatsTracker.Instance;
            if (tracker != null)
                foreach (var entry in tracker.GetAllEntries())
                    context.CombatStats.Add(entry);
        }

        BattleManager.Instance?.DespawnAllUnits();

        if (PopupManager.Instance == null) return;
        var popup = PopupManager.Instance.Open<ReincarnationPopup>(PopupType.Reincarnation);
        popup?.Setup(context, killCount);
    }

    // ── 배틀 시작 ────────────────────────────────────────────

    /// <summary>
    /// 아군만 세우는 '출전 대기' 까지 준비한다 (적 없음).
    /// 모드 생성 규칙을 한 곳에 두려고 StartBattle 과 같은 경로를 쓴다.
    /// </summary>
    public void PrepareStandby()
    {
        BattleModeBase mode = BuildMode();
        if (mode == null) return;
        BattleManager.Instance.PrepareBattle(mode);
    }

    /// <summary>대기 중인 전투의 웨이브를 시작한다.</summary>
    public void BeginWaves()
    {
        // 경과 시간은 '실제로 싸운 시간' 이어야 한다 —
        // 출전 대기·카메라 무빙에 머문 시간은 빼고 여기서 다시 잡는다.
        _battleStartTime = Time.time;
        BattleManager.Instance.BeginWaves();
    }

    void StartBattle()
    {
        BattleModeBase mode = BuildMode();
        if (mode == null) return;
        BattleManager.Instance.StartBattle(mode);
    }

    /// <summary>현재 상황에 맞는 배틀 모드를 만든다 (로비 진입 · 에디터 직접 실행 공용).</summary>
    BattleModeBase BuildMode()
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
                return null;
            }
            var editorStage = new StageData
            {
                Mode        = StartMode,
                StageNumber = 1,
                Waves       = WaveSetup.Waves,
                GoldReward  = 500,
                EquipStoneReward = 2,
            };
            mode = CreateMode(editorStage);
            Debug.Log($"[InGameManager] 배틀 시작 (에디터 직접) — 모드: {StartMode}, 웨이브: {WaveSetup.Waves.Count}개");
        }

        return mode;
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
