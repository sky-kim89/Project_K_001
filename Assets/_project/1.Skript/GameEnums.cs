// ============================================================
//  GameEnums.cs
//  게임 전반에 걸쳐 사용되는 enum 모음 (네임스페이스 없음)
//  특정 시스템에 종속된 enum은 해당 파일에 보관할 것
// ============================================================

// ── 팀 소속 ───────────────────────────────────────────────────
public enum TeamType : byte
{
    Ally  = 0,  // 아군
    Enemy = 1,  // 적군
}

// ── 오브젝트 풀 ──────────────────────────────────────────────
public enum PoolType
{
    UI         = 0,
    Unit       = 1,
    Effect     = 2,
    Projectile = 3,   // 기본 공격 발사체 (Arrow, MagicBolt 등)
}

// ── 게임 상태 ─────────────────────────────────────────────────
public enum GameState
{
    None    = 0,
    Lobby   = 1,
    Loading = 2,
    InGame  = 3,
    Result  = 4,
    Paused  = 5
}

// ── 팝업 타입 ─────────────────────────────────────────────────
public enum PopupType
{
    None         = 0,
    Alert        = 1,   // 확인만 있는 알림
    Confirm      = 2,   // 확인/취소
    Settings     = 3,
    BattleResult = 4,   // 전투 결과 (승리/패배)
    Pause        = 5,   // 일시 정지
    Loading      = 6,   // 로딩
}

// ── 로그인 상태 ───────────────────────────────────────────────
public enum LoginState
{
    None        = 0,
    Connecting  = 1,
    Success     = 2,
    Failed      = 3,
    TokenExpired = 4
}
