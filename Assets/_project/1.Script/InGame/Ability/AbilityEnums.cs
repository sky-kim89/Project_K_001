// ============================================================
//  AbilityEnums.cs
//  어빌리티 시스템 열거형 정의
// ============================================================

public enum AbilityId
{
    None = 0,

    // ── 일반 (Normal, 101~115) ──────────────────────────────
    A01 = 101, // 강인한 체력   — All         MaxHp +8%
    A02 = 102, // 예리한 검격   — All         Attack +8%
    A03 = 103, // 신속한 연격   — All         AttackSpeed +8%
    A04 = 104, // 민첩한 기동   — All         MoveSpeed +8%
    A05 = 105, // 단단한 방어   — All         Defense +6%
    A06 = 106, // 기사의 용맹   — Knight      Attack +8%, MaxHp +6%
    A07 = 107, // 명궁의 직관   — Archer      AttackRange +10%, AttackSpeed +6%
    A08 = 108, // 마법사의 집중 — Mage        Attack +10%, SkillCooldownReduce +8%
    A09 = 109, // 방패병의 수호 — ShieldBearer Defense +12%, MaxHp +6%
    A10 = 110, // 전사의 돌격   — Melee       Attack +8%, MoveSpeed +6%
    A11 = 111, // 원거리 집중   — Ranged      AttackRange +8%, Attack +6%
    A12 = 112, // 장군의 위엄   — General     MaxHp +10%, Defense +6%
    A13 = 113, // 병사의 투지   — Soldier     Attack +10%, MoveSpeed +6%
    A14 = 114, // 치명의 감각   — All         CritChance +6%
    A15 = 115, // 넓은 시야     — All         AttackRange +8%

    // ── 고급 (Advanced, 201~212) ─────────────────────────────
    B01 = 201, // 철벽 체력     — All         MaxHp +15%
    B02 = 202, // 강철 검격     — All         Attack +15%
    B03 = 203, // 폭풍 연격     — All         AttackSpeed +12%
    B04 = 204, // 질풍 기동     — All         MoveSpeed +12%
    B05 = 205, // 기사의 분노   — Knight      Attack +15%, MaxHp +10%
    B06 = 206, // 독수리의 눈   — Archer      AttackRange +15%, AttackSpeed +12%
    B07 = 207, // 마법의 각성   — Mage        Attack +18%, SkillCooldownReduce +15%
    B08 = 208, // 철옹성        — ShieldBearer Defense +20%, MaxHp +12%
    B09 = 209, // 광전사의 기세 — Melee       Attack +15%, MoveSpeed +10%
    B10 = 210, // 정밀 사격     — Ranged      AttackRange +12%, Attack +12%
    B11 = 211, // 영웅의 기상   — General     MaxHp +18%, Defense +12%
    B12 = 212, // 병사의 맹세   — Soldier     Attack +27%, MoveSpeed +18%  (병사 전용 ×1.5)

    // ── 특수 (Special, 301~311) ─────────────────────────────
    // Special 등급은 트리거 기반 — AbilityData 서브클래스로 구현
    C01 = 301, // 흡혈 강습     — OnAttack:       공격 시 피해의 15% 즉시 체력 회복
    C02 = 302, // 철갑 반응     — OnHit:          피격마다 방어율 +1% 영구 누적 (최대 95%)
    C03 = 303, // 처치 연쇄     — OnEnemyKill:    처치 시 공격력 +2% 영구 누적
    C04 = 304, // 희생의 힘     — OnSoldierDeath: 병사 사망 시 장군 공격력·체력 대폭 강화(1회)
    C05 = 305, // 고통의 계약   — OnBattleStart:  전투 시작 시 최대HP 70% 손실 + 공/공속/방어 강화
    C06 = 306, // 거울 방어     — OnBattleStart:  피해의 25% 반사 (적 방어율 적용, 무한 반사 불가)
    C07 = 307, // 혼령 집결     — OnSoldierDeath: 이번 전투 사망 병사 1명당 5% 공격력·체력 (장군+소속 병사)
    C08 = 308, // 황금 탐욕     — 시스템:         스테이지 골드 보상 +30%
    C09 = 309, // 성장 촉진     — 시스템:         장군 경험치 획득량 +30%
    C10 = 310, // 시간 왜곡     — OnBattleStart:  스킬 쿨타임 -35%, 공격력 -10%
    C11 = 311, // 쌍신 공격     — OnBattleStart:  공격력 -40%, 장군·병사 모두 2회 연타

    // ── 일반 확장 (Normal, 116~117) ──────────────────────────
    A16 = 116, // 병사 추가     — General         SoldierCount +1 (병사 1명 추가)
    A17 = 117, // 지휘력 강화   — General         CommandPower +10 (병사 스텟 +10%)

    // ── 고급 확장 (Advanced, 213~214) ────────────────────────
    B13 = 213, // 병사 대규모   — General         SoldierCount +2 (병사 2명 추가)
    B14 = 214, // 완벽한 지휘   — General         CommandPower +20 (병사 스텟 +20%)

    // ── 달인 (Mastery, 401~404) — 직업 해금 조건 충족 시 100% 등장
    D01 = 401, // 기사 달인     — Knight          주기적으로 타겟에게 300% 피해 돌진 (6초 쿨타임)
    D02 = 402, // 궁수 달인     — Archer          일반 공격 시 50% 확률로 2번째 적에게도 공격
    D03 = 403, // 마법사 달인   — Mage            일반 공격 시 1% 확률로 보유 스킬 즉시 발동
    D04 = 404, // 방패병 달인   — ShieldBearer    넉백 완전 무시 + 최대체력 비례 광역 오라
}

public enum AbilityGrade
{
    Normal   = 0,   // 일반 (60% 가중치)
    Advanced = 1,   // 고급 (30% 가중치)
    Special  = 2,   // 특수 (10% 가중치, 1개 제한)
    Mastery  = 3,   // 달인 (직업별 해금 조건 충족 시 100% 등장, 1개 제한)
}

public enum AbilityTarget
{
    [UnityEngine.InspectorName("전체")]               All              = 0,
    [UnityEngine.InspectorName("직업 — 기사")]        Job_Knight       = 1,
    [UnityEngine.InspectorName("직업 — 궁수")]        Job_Archer       = 2,
    [UnityEngine.InspectorName("직업 — 마법사")]      Job_Mage         = 3,
    [UnityEngine.InspectorName("직업 — 방패병")]      Job_ShieldBearer = 4,
    [UnityEngine.InspectorName("범위 — 근거리 (기사+방패)")]  Range_Melee      = 5,
    [UnityEngine.InspectorName("범위 — 원거리 (궁수+마법사)")] Range_Ranged     = 6,
    [UnityEngine.InspectorName("유닛 — 장군")]        Unit_General     = 7,
    [UnityEngine.InspectorName("유닛 — 병사")]        Unit_Soldier     = 8,
}
