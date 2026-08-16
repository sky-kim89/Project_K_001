using UnityEngine;

// ============================================================
//  StageConfig.cs
//  스테이지 난이도 수치를 정의하는 ScriptableObject.
//  StageGenerator 가 이 값을 읽어 WaveData 목록을 생성한다.
//
//  ■ 난이도 설계
//    스테이지를 5개 단위 블록으로 나누어 지수 성장 적용.
//    BlockStatMultipliers[N] = N번 블록(스테이지 N*5+1 ~ N*5+5)의 스텟 배율.
//    5의 배수 스테이지(5, 10, 15…)는 허들로 추가 강화.
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
    [Tooltip("환생 가능 최소 일반 스테이지 클리어 수")]
    public int ReincarnateMinStage = 5;

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

    [Header("블록별 스텟 배율 — 5스테이지 단위, 인덱스 0=스테이지1~5")]
    [Tooltip("블록당 약 2.5배 지수 성장. 환생·유물 시스템을 고려한 후반부 100배 스케일.")]
    public float[] BlockStatMultipliers = { 1.0f, 2.5f, 6.5f, 16f, 40f, 100f };

    [Header("초반 난이도 완화 — 스테이지 1~4 한정")]
    [Tooltip("스테이지 1의 적 스텟 배율. 0.3 = 블록 배율 대비 -70%. 5스테이지부터는 블록 배율 그대로.")]
    [Range(0.1f, 1f)]
    public float EarlyGameStatStart = 0.3f;

    [Header("허들 스테이지 (5의 배수) 추가 배율")]
    [Tooltip("5·10·15… 스테이지는 블록 배율에 이 값을 추가로 곱한다. 1.5 = 블록 배율의 50% 추가.")]
    [Range(1f, 3f)]
    public float HurdleStatMultiplier = 1.5f;

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

    /// <summary>스테이지 번호(1~)로 블록 스텟 배율을 반환한다.</summary>
    public float GetBlockMultiplier(int stageNumber)
    {
        if (BlockStatMultipliers == null || BlockStatMultipliers.Length == 0) return 1f;
        int idx = Mathf.Clamp((stageNumber - 1) / 5, 0, BlockStatMultipliers.Length - 1);
        return BlockStatMultipliers[idx];
    }

    /// <summary>허들 스테이지(5의 배수) 여부를 반환한다.</summary>
    public bool IsHurdle(int stageNumber) => stageNumber % 5 == 0;
}
