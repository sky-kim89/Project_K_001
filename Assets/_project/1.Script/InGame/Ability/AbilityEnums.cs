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
    B12 = 212, // 병사의 맹세   — Soldier     Attack +18%, MoveSpeed +12%

    // ── 특수 (Special, 301~304) ─────────────────────────────
    // Special 등급은 트리거 기반 — AbilityData 서브클래스로 구현
    C01 = 301, // 흡혈 강습     — OnAttack:       공격 시 피해의 15% 즉시 체력 회복
    C02 = 302, // 철갑 반응     — OnHit:          피격 시 방어율 +15% 5초 버프
    C03 = 303, // 처치 연쇄     — OnEnemyKill:    처치 시 공격력 +20% 5초 버프
    C04 = 304, // 희생의 힘     — OnSoldierDeath: 병사 사망 시 장군 공격력·체력 대폭 강화(1회)
}

public enum AbilityGrade
{
    Normal   = 0,   // 일반 (60% 가중치)
    Advanced = 1,   // 고급 (30% 가중치)
    Special  = 2,   // 특수 (10% 가중치, 1개 제한)
}

public enum AbilityTarget
{
    All              = 0,
    Job_Knight       = 1,
    Job_Archer       = 2,
    Job_Mage         = 3,
    Job_ShieldBearer = 4,
    Range_Melee      = 5,   // Knight + ShieldBearer
    Range_Ranged     = 6,   // Archer + Mage
    Unit_General     = 7,   // 장군 스텟 전용
    Unit_Soldier     = 8,   // 병사 스텟 전용
}
