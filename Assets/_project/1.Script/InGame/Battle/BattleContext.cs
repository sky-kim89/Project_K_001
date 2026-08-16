//  BattleContext.cs
//  현재 배틀의 진행 상태를 담는 순수 데이터 클래스.
//  BattleModeBase 와 BattleManager 가 공유해서 읽고 쓴다.
// ============================================================

public class BattleContext
{
    // ── 웨이브 정보 ───────────────────────────────────────────
    public int TotalWaves  { get; set; }   // 이번 배틀의 총 웨이브 수
    public int CurrentWave { get; set; }   // 현재 웨이브 (1부터 시작)

    // ── 무한 보스 (최종 스테이지) ─────────────────────────────
    /// <summary>현재 상대 중인 무한 보스 번호 (1부터). 0 이면 무한 구간이 아니다.</summary>
    public int EndlessBossIndex { get; set; }

    // ── 진행 상태 ─────────────────────────────────────────────
    public BattleState State       { get; set; } = BattleState.None;
    public BattleMode  Mode        { get; set; } = BattleMode.Normal;

    // ── 생존 카운트 (웨이브 클리어 판정용) ────────────────────
    public int AliveEnemyCount { get; set; }
    public int AliveAllyCount  { get; set; }

    // ── 스테이지 레벨 (장비 박스 개봉 시 EquipmentDatabase.PickRandom 에 전달) ──
    public int StageLevel { get; set; }

    // ── 누적 보상 (스테이지 클리어 시 ApplyStageClearReward 가 채움) ──
    public System.Collections.Generic.List<ItemAmount> PendingRewards { get; } = new();

    // ── 영웅 EXP 획득 내역 (결과 팝업 표시용) ────────────────
    public struct UnitExpGain
    {
        public string UnitName;
        public int    ExpGained;
        public int    LevelsGained;
        public int    NewLevel;
    }
    public System.Collections.Generic.List<UnitExpGain> ExpGains { get; } = new();

    // ── 전투 통계 스냅샷 (결과 팝업 통계 탭용) ───────────────
    public System.Collections.Generic.List<GeneralStatEntry> CombatStats { get; } = new();

    // ── 전투 경과 시간 (환생 팝업 DPS 계산용) ────────────────
    public float BattleElapsedSeconds { get; set; }

    // ── 편의 프로퍼티 ─────────────────────────────────────────
    public bool IsLastWave      => CurrentWave >= TotalWaves;
    public bool IsEnemyClear    => AliveEnemyCount <= 0;
    public bool IsAllyDefeated  => AliveAllyCount  <= 0;
}
