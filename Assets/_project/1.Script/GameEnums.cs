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
    EquipCompare    = 7,   // 장비 비교/교체
    Disassemble     = 8,   // 장수·장비 분해
    AbilitySelect   = 9,   // 어빌리티 3택 선택
    AbilityList     = 10,  // 어빌리티 전체 목록 확인
    MercenaryShop   = 11,  // 용병 상점 (신규 고용 / 해고)
    HeroDetail      = 12,  // 배치 장수 상세 + 해고
    Reincarnation   = 13,  // 환생 전용 팝업 (패배 시)
    RunShop         = 14,  // 런 중간 상점 (장비·특성·장수 구매)
}

// ── 런 스테이지 타입 ──────────────────────────────────────────
public enum RunStageType
{
    Normal = 0,  // 일반 전투
    Elite  = 1,  // 엘리트 (몬스터 ×1.1, 엘리트 출현 ×2)
    Shop   = 2,  // 상점
    Event  = 3,  // 이벤트 (추후 구현)
}

// ── 특성 종류 ─────────────────────────────────────────────────
public enum TraitType
{
    None             = 0,

    // ── 초기 선택 특성 (직업별 1종) ─────────────────────────
    KnightCommand    = 1,   // 기사: 지휘관의 기질
    ArcherPrecision  = 2,   // 궁수: 정밀 사수
    MageArcane       = 3,   // 마법사: 마력 집중
    ShieldFortress   = 4,   // 방패병: 강철 요새
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
