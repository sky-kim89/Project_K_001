using UnityEngine;

// ============================================================
//  InGameManager.cs
//  인게임 씬의 진입점 — 씬 로드 시 자동으로 배틀을 시작한다.
//
//  실행 순서:
//    Awake: PoolController, BattleManager 준비 확인
//    Start: BattleModeBase 생성 → BattleManager.StartBattle() 호출
//
//  Inspector 설정:
//    - WaveSetup: WaveSetupData SO 할당
//    - BattleMode: 어떤 모드로 시작할지 (기본 Normal)
//
//  배틀 종료 콜백:
//    BattleManager 가 OnBattleEnd 이벤트를 발생시키면
//    InGameManager 가 결과 화면 전환 등을 처리한다.
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

    // ── Unity 생명주기 ────────────────────────────────────────

    void Awake()
    {
        ValidateDependencies();
    }

    void Start()
    {
        if (AutoStartDelay > 0f)
            Invoke(nameof(StartBattle), AutoStartDelay);
        else
            StartBattle();
    }

    // ── 배틀 시작 ────────────────────────────────────────────

    void StartBattle()
    {
        if (WaveSetup == null || WaveSetup.Waves.Count == 0)
        {
            Debug.LogError("[InGameManager] WaveSetup 이 비어있습니다. Inspector 에서 WaveSetupData 를 할당하세요.");
            return;
        }

        BattleModeBase mode = CreateMode(StartMode);
        if (mode == null) return;

        BattleManager.Instance.StartBattle(mode);
        Debug.Log($"[InGameManager] 배틀 시작 — 모드: {StartMode}, 총 웨이브: {WaveSetup.Waves.Count}");
    }

    // ── 모드 생성 ─────────────────────────────────────────────

    BattleModeBase CreateMode(BattleMode mode)
    {
        switch (mode)
        {
            case BattleMode.Normal:
                return new NormalMode(WaveSetup.Waves);

            // 추후 골드 던전, 특수 던전 추가 시 여기에 case 추가
            // case BattleMode.GoldDungeon:
            //     return new GoldDungeonMode(WaveSetup.Waves);

            default:
                Debug.LogError($"[InGameManager] 구현되지 않은 배틀 모드: {mode}");
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
