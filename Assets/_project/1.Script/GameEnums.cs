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
    KnightSoldierRage   = 5,   // 기사: 전우의 분노 (병사 사망마다 체력 +1%)
    KnightHeroReturn    = 6,   // 기사: 영웅의 귀환 (사망 시 1HP + 병사 즉시 소환)
    ArcherRetreatFire   = 7,   // 궁수: 퇴각 사격 (반사정 접근 시 후퇴하며 사격)
    ArcherRainFire      = 8,   // 궁수: 폭우 사격 (주변 적 2명 추가 타격 70%)
    ShieldCounterBlow   = 9,   // 방패병: 반격의 달인 (흡수 피해 반사)
    ShieldRageBuild     = 10,  // 방패병: 분노 축적 (피격마다 공격력 +3%, 최대 +30%)
    MageAttackCdr       = 11,  // 법사: 마법 집중 (공격 시 스킬 쿨타임 -1초)
    MageEchoSkill       = 12,  // 법사: 연속 시전 (스킬 사용 시 즉시 재발동, 피해 -40%)

    // ── 공통 장수 배치 슬롯 확장 특성 ───────────────────────────
    CommonExpedition    = 13,  // 원정 편성    (슬롯 +1)
    CommonMassMobilize  = 14,  // 대규모 동원  (슬롯 +2, 전 능력치 -10%)
    CommonSoldierSupply = 15,  // 병사 지원령  (슬롯 +1, 병사 +5)
    CommonForcedLevy    = 16,  // 무리한 징집  (슬롯 +1, 이동속도 -15%)
    CommonEquipExpand   = 17,  // 장비 확장    (장비 슬롯 +1)

    // ── 직업 시너지 특성 (1000~) ─────────────────────────────────
    // 상점·이벤트에 등장하지 않음. JobSynergyEvaluator 가 배치 구성에 따라 자동 부여·제거.
    // 조합형 (1~4) ─────────────────────────
    Synergy_VanguardCross = 1001, // 선봉대: 기사1+궁수1 → 공격력 +10%
    Synergy_MagicShield   = 1002, // 마법 방패: 기사1+법사1 → 공격력 +5%, 최대체력 +5%
    Synergy_IronWallLine  = 1003, // 철벽진: 기사1+방패병1 → 방어율 +5%, 최대체력 +10%
    Synergy_BalancedHost  = 1004, // 균형의 군세: 전 직업 각 1+ → 전 스탯 +5%
    // 5-스택 (1011~1014) ────────────────────
    Synergy_KnightOrder   = 1011, // 기사단: 기사5 → 공격력 +30%, 이동속도 +20%
    Synergy_ArrowLegion   = 1012, // 화살의 군단: 궁수5 → 공격력 +30%, 공격속도 +20%
    Synergy_GreatMageCorp = 1013, // 대법사단: 법사5 → 공격력 +35%, 쿨타임 -15%
    Synergy_Ironclad      = 1014, // 철옹성: 방패병5 → 최대체력 +40%, 방어율 +15%
    // 복합형 (1021~1022) ────────────────────
    Synergy_RangedFirenet = 1021, // 원거리 화망: 궁수2+법사2 → 공격력 +20%, 사거리 +10%
    Synergy_IronVanguard  = 1022, // 철벽 전위대: 기사2+방패병2 → 최대체력 +30%, 방어율 +10%
    // 열화 (1031~1034, 3-스택) — 5-스택 활성 시 자동 제거 ──
    Synergy_KnightSquad   = 1031, // 기사 소대: 기사3 → 공격력 +10%, 이동속도 +6%
    Synergy_ArcherSquad   = 1032, // 궁수 소대: 궁수3 → 공격력 +10%, 공격속도 +6%
    Synergy_MageSquad     = 1033, // 법사 소대: 법사3 → 공격력 +12%, 쿨타임 -5%
    Synergy_ShieldSquad   = 1034, // 방패병 소대: 방패병3 → 최대체력 +13%, 방어율 +5%
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
