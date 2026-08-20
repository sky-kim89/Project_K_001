using UnityEngine;

// ============================================================
//  StageConfig.cs
//  스테이지 난이도 수치를 정의하는 ScriptableObject.
//  StageGenerator 가 이 값을 읽어 WaveData 목록을 생성한다.
//
//  ■ 난이도 설계
//    BlockStatMultipliers[N] = 스테이지 N*5+1 의 스텟 배율 앵커.
//    앵커 사이는 스테이지 단위로 기하 보간한다 (약 1.17배/스테이지).
//    5의 배수 스테이지(5, 10, 15…)는 허들로 추가 강화.
//
//    ⚠ 스텟 배율은 스테이지가 올라가면 절대 떨어지지 않아야 한다
//      허들 배율이 스테이지당 성장(1.17)보다 크면 허들 바로 다음 스테이지가
//      더 쉬워진다. 허들은 배율이 아니라 물량·웨이브로 벌리는 쪽이 안전하다.
//
//  생성: 우클릭 > Create > Project K > Stage Config
// ============================================================

[CreateAssetMenu(fileName = "StageConfig", menuName = "Project K/Stage Config")]
public class StageConfig : ScriptableObject
{
    static StageConfig _current;
    public static StageConfig Current
    {
        get => _current != null ? _current : (_current = Resources.Load<StageConfig>("StageConfig"));
        internal set => _current = value;
    }

    [Header("스테이지 수")]
    public int NormalStageCount = 30;
    public int EliteStageCount  = 15;

    [Header("잠금 해제 조건")]
    [Tooltip("엘리트 탭 해제에 필요한 일반 스테이지 클리어 수")]
    public int EliteUnlockStage    = 5;
    [Tooltip("환생 가능 최소 일반 스테이지 클리어 수. 이 스테이지부터 환생 포인트가 쌓인다.")]
    public int ReincarnateMinStage = 2;
    [Tooltip("이 일반 스테이지를 클리어하면 다음 난이도 등급이 해금된다.")]
    public int DifficultyUnlockStage = 20;

    [Header("에너지 비용")]
    public int EnergyCostNormal = 5;
    public int EnergyCostElite  = 10;

    [Header("웨이브 수 (스테이지 진행도 0→1 에 따라 Min→Max 선형 보간)")]
    public int WaveCountMin = 2;
    public int WaveCountMax = 8;

    [Header("웨이브당 일반 적 수 (스테이지 진행도 0→1 에 따라 Min→Max 선형 보간)")]
    [Tooltip("Stage 30 허들 기준 10웨이브 × 108적 ≈ 1080마리")]
    public int EnemyCountMin = 6;
    public int EnemyCountMax = 100;

    [Header("적 레벨 (스테이지 번호 1→NormalStageCount 에 따라 Min→Max 선형 보간)")]
    public int EnemyLevelMin = 1;
    public int EnemyLevelMax = 50;

    // ──────────────────────────────────────────────────────────
    // ■ 지수 성장 난이도 설정
    // ──────────────────────────────────────────────────────────

    [Header("스텟 배율 앵커 — 5스테이지 간격 (인덱스 0=스테이지1, 1=스테이지6, …)")]
    [Tooltip("앵커 사이는 스테이지 단위로 기하 보간된다 (GetBlockMultiplier 참고). " +
             "마지막 값은 26~30 구간을 이어 갈 꼬리 앵커라 그 자체로는 등장하지 않는다.")]
    public float[] BlockStatMultipliers = { 1.0f, 2.2f, 5.0f, 11f, 25f, 55f, 85f };

    [Header("초반 난이도 완화")]
    [Tooltip("스테이지 1의 적 스텟 배율. EarlyGameEndStage 까지 1.0 으로 선형 복귀한다.")]
    [Range(0.1f, 1f)]
    public float EarlyGameStatStart = 0.55f;

    [Tooltip("완화가 끝나는 스테이지. 이 스테이지부터는 배율이 그대로 적용된다.")]
    public int EarlyGameEndStage = 8;

    [Header("허들 스테이지 (5의 배수) 추가 배율")]
    [Tooltip("5·10·15… 스테이지는 앵커 배율에 이 값을 추가로 곱한다. " +
             "⚠ 스테이지당 기본 성장(약 1.17배)보다 크게 잡으면 허들 다음 스테이지가 " +
             "오히려 쉬워진다 — 1.15 이하로 유지할 것.")]
    [Range(1f, 3f)]
    public float HurdleStatMultiplier = 1.15f;

    [Tooltip("허들 스테이지 웨이브당 추가 일반 적 수")]
    public int HurdleExtraEnemies = 8;

    [Tooltip("허들 스테이지 추가 웨이브 수")]
    public int HurdleExtraWaves = 2;

    [Header("용병 조각 보상")]
    [Tooltip("스테이지 1 기본 지급량")]
    public int ShardRewardBase     = 5;
    [Tooltip("5스테이지 블록마다 추가 지급량 (블록 0=+0, 블록 1=+2, …)")]
    public int ShardRewardPerBlock = 2;
    [Tooltip("허들 스테이지 추가 지급량")]
    public int ShardHurdleBonus    = 5;

    [Header("장군 강화석 보상 (등급업 재화)")]
    [Tooltip("스테이지 1 기본 지급량")]
    public int GeneralStoneRewardBase     = 2;
    [Tooltip("5스테이지 블록마다 추가 지급량 (블록 0=+0, 블록 1=+1, …)")]
    public int GeneralStoneRewardPerBlock = 1;
    [Tooltip("허들 스테이지 추가 지급량")]
    public int GeneralStoneHurdleBonus    = 3;

    [Header("장비 박스 보상")]
    [Tooltip("이 스테이지부터 장비 박스 2개 지급")]
    public int EquipBoxThreshold2 = 10;
    [Tooltip("이 스테이지부터 장비 박스 3개 지급")]
    public int EquipBoxThreshold3 = 20;

    // ──────────────────────────────────────────────────────────
    // ■ 보상
    // ──────────────────────────────────────────────────────────

    [Header("스테이지 클리어 골드 보상 — 스테이지 N = Base + (N-1) × PerStage")]
    [Tooltip("1스테이지 클리어 골드")]
    public int GoldRewardBase     = 500;
    [Tooltip("스테이지 1당 증가분. 기본 50 → 500 / 550 / 600 / 650 …")]
    public int GoldRewardPerStage = 50;

    [Header("스테이지 클리어 전투석 보상 (진행도 0→1 에 따라 Min→Max 선형 보간)")]
    public int EquipStoneRewardMin = 1;
    public int EquipStoneRewardMax = 20;

    [Header("스테이지 클리어 경험치 보상 (진행도 0→1 에 따라 Min→Max 선형 보간)")]
    public int ExpRewardMin = 200;
    public int ExpRewardMax = 3000;

    // ── 헬퍼 ─────────────────────────────────────────────────

    /// <summary>
    /// 스테이지 번호(1~)의 스텟 배율. 앵커 사이를 기하 보간한다.
    ///
    /// ⚠ 예전엔 블록 안에서 값이 고정이었다 (계단형)
    ///   스테이지 1~5 가 전부 ×1.0 이라 다섯 판 내리 같은 세기의 적이 나왔고,
    ///   그 사이 플레이어는 레벨·장비·어빌리티로 계속 강해져 초반이 통째로 허무해졌다.
    ///   그러다 6스테이지에서 갑자기 2.5배가 튀어나와 벽처럼 느껴졌다.
    ///   지금은 앵커(5스테이지 간격)만 남기고 그 사이를 스테이지마다 약 1.17배씩
    ///   기하 보간한다 — 같은 지점을 지나면서 계단이 사라진다.
    ///
    /// ⚠ 마지막 앵커는 꼬리다
    ///   26~30 구간도 보간하려면 그 너머의 값이 하나 더 필요하다.
    ///   배열 끝 값은 그 용도이며 실제 스테이지에 그대로 적용되지는 않는다.
    /// </summary>
    public float GetBlockMultiplier(int stageNumber)
    {
        if (BlockStatMultipliers == null || BlockStatMultipliers.Length == 0) return 1f;

        int block = Mathf.Max(0, (stageNumber - 1) / 5);
        if (block >= BlockStatMultipliers.Length - 1)
            return BlockStatMultipliers[BlockStatMultipliers.Length - 1];

        float from = BlockStatMultipliers[block];
        float to   = BlockStatMultipliers[block + 1];
        if (from <= 0f || to <= 0f) return Mathf.Max(from, 0.01f);

        float t = ((stageNumber - 1) % 5) / 5f;
        return from * Mathf.Pow(to / from, t);
    }

    /// <summary>허들 스테이지(5의 배수) 여부를 반환한다.</summary>
    public bool IsHurdle(int stageNumber) => stageNumber % 5 == 0;
}
