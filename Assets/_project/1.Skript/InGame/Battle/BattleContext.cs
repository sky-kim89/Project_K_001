// ============================================================
//  BattleContext.cs
//  현재 배틀의 진행 상태를 담는 순수 데이터 클래스.
//  BattleModeBase 와 BattleManager 가 공유해서 읽고 쓴다.
// ============================================================

public class BattleContext
{
    // ── 웨이브 정보 ───────────────────────────────────────────
    public int TotalWaves  { get; set; }   // 이번 배틀의 총 웨이브 수
    public int CurrentWave { get; set; }   // 현재 웨이브 (1부터 시작)

    // ── 진행 상태 ─────────────────────────────────────────────
    public BattleState State       { get; set; } = BattleState.None;
    public BattleMode  Mode        { get; set; } = BattleMode.Normal;

    // ── 생존 카운트 (웨이브 클리어 판정용) ────────────────────
    public int AliveEnemyCount { get; set; }
    public int AliveAllyCount  { get; set; }

    // ── 누적 보상 (스테이지 클리어 시 ApplyStageClearReward 가 채움) ──
    public System.Collections.Generic.List<ItemAmount> PendingRewards { get; } = new();

    // ── 편의 프로퍼티 ─────────────────────────────────────────
    public bool IsLastWave      => CurrentWave >= TotalWaves;
    public bool IsEnemyClear    => AliveEnemyCount <= 0;
    public bool IsAllyDefeated  => AliveAllyCount  <= 0;
}
