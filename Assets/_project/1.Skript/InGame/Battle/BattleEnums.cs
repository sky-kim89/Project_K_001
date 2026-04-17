// ============================================================
//  BattleEnums.cs
//  배틀 시스템 전용 enum 모음
// ============================================================

// ── 배틀 모드 ─────────────────────────────────────────────────
public enum BattleMode
{
    Normal         = 0,   // 일반 스테이지
    Elite          = 1,   // 엘리트 스테이지
    GoldDungeon    = 2,   // 골드 던전 (추후 구현)
    SpecialDungeon = 3,   // 특수 던전 (추후 구현)
}

// ── 배틀 상태 ─────────────────────────────────────────────────
public enum BattleState
{
    None         = 0,
    Preparing    = 1,   // 웨이브 시작 전 준비 (카운트다운 등)
    InWave       = 2,   // 웨이브 진행 중
    WaveClear    = 3,   // 웨이브 클리어 (보상 창 대기)
    BattleVictory = 4,  // 전체 클리어
    BattleDefeat  = 5,  // 패배
}

// ── 스폰 유닛 종류 (스폰 테이블 항목용) ───────────────────────
public enum SpawnUnitType
{
    Soldier = 0,
    General = 1,
    Enemy   = 2,
    Elite   = 3,
    Boss    = 4,
}
