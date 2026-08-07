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

    /// <summary>장군 스폰 완료 → 로딩 팝업 닫기 + 전투 타이머 시작.</summary>
    void HandleAlliesReady()
    {
        if (PopupManager.Instance != null)
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
        var relicInventory    = UserDataManager.Instance?.Get<RelicInventoryData>();
        var relicDb           = RelicDatabase.Current;
        var reincarnationData = UserDataManager.Instance?.Get<ReincarnationData>();

        if (db != null && runAbility != null && PopupManager.Instance != null)
        {
            var choices = AbilityPicker.Pick(db, runAbility, relicInventory, relicDb);
            if (choices.Length > 0)
            {
                var abilityPopup = PopupManager.Instance.Open<AbilitySelectPopup>(PopupType.AbilitySelect);
                abilityPopup?.Setup(choices, chosen =>
                {
                    runAbility.AddAbility(chosen.Id);
                    FinishStageClear();
                }, db, runAbility, relicInventory, relicDb, reincarnationData);
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
