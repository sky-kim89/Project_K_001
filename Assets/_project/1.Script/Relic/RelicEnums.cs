// ============================================================
//  RelicEnums.cs
//  유물(Relic) 시스템 열거형 정의
// ============================================================

// ── 유물 희귀도 ───────────────────────────────────────────────
// 색상은 UIConstants.RelicStyle.GetColor() 참조
// UnitGrade 색상 체계와 1:1 매핑
public enum RelicRarity
{
    Common    = 0,  // 일반   — 회색  (UnitGrade.Normal   과 동일)
    Uncommon  = 1,  // 언커먼 — 초록  (UnitGrade.Uncommon 과 동일)
    Rare      = 2,  // 희귀   — 파랑  (UnitGrade.Rare     과 동일)
    Epic      = 3,  // 영웅   — 보라  (UnitGrade.Unique   과 동일)
    Legendary = 4,  // 전설   — 주황  (UnitGrade.Epic     과 동일)
}

// ── 유물 표시 카테고리 (정렬 순서: 스탯→재화→어빌리티) ──────────
public enum RelicCategory
{
    Stat     = 0,  // 장수·병사 스탯 유물 (IDs 2xx, 3xx)
    Currency = 1,  // 보상·재화 관련 시스템 유물 (IDs 104~108)
    Ability  = 2,  // 어빌리티 관련 시스템 유물 (IDs 101~103)
}

// ── 유물 효과 분류 ────────────────────────────────────────────
public enum RelicEffectType
{
    Stat   = 0,  // 스텟 증가 — 장수·병사 UnitStat 에 직접 반영
    System = 1,  // 시스템 효과 — 게임 규칙 변경 (어빌리티, 보상, 적 약화 등)
}

// ── 시스템 효과 종류 ──────────────────────────────────────────
public enum RelicSystemEffect
{
    None = 0,

    AbilityRefreshCount    = 1,  // 어빌리티 선택 새로고침 횟수 +N
    AbilityChoiceCount     = 2,  // 어빌리티 선택지 수 +N (기본 3)
    AbilityAdvancedChance  = 3,  // 고급 이상 어빌리티 등장 확률 +N%p (가중치 조정)

    GoldGainBonus          = 4,  // 골드 획득량 +N%
    SoldierSoulGainBonus   = 5,  // 병사 소울 획득량 +N%
    ExpGainBonus           = 6,  // 장수 경험치 획득량 +N%

    EnemyMaxHpReduction    = 7,  // 적 최대 체력 -N%
    EnemyAttackReduction   = 8,  // 적 공격력 -N%

    GeneralSlotBonus       = 9,  // 장수 배치 슬롯 +N칸 (기본은 RelicApplier.BaseGeneralSlots)
    BattleSpeedUnlock      = 10, // 전투 배속 해금 +N단계 (기본 1× 만, Lv1=2×, Lv2=3×)
}

// ── 유물 ID ───────────────────────────────────────────────────
public enum RelicId
{
    None = 0,

    // ── 시스템 유물 (1xx) ─────────────────────────────────────
    // 어빌리티 관련
    R_AbilityRefresh       = 101,  // 어빌리티 재편성  — 새로고침 +1/레벨,         Lv3, Rare
    R_AbilityChoicePlus    = 102,  // 선택의 축복      — 선택지 +1/레벨,           Lv2, Epic
    R_AbilityAdvanced      = 103,  // 고급 비전        — 고급이상 확률 +10%p/레벨, Lv3, Rare

    // 보상 증가
    R_GoldBonus            = 104,  // 황금의 가호      — 골드 +15%/레벨,           Lv5, Common
    R_SoulBonus            = 105,  // 전사의 유산      — 소울 +15%/레벨,           Lv5, Common
    R_ExpBonus             = 106,  // 성장의 증표      — 경험치 +10%/레벨,         Lv5, Uncommon

    // 적 약화
    R_EnemyHpDown          = 107,  // 시련의 세례      — 적 체력 -5%/레벨,         Lv4, Rare
    R_EnemyAtkDown         = 108,  // 공포의 각인      — 적 공격력 -4%/레벨,       Lv4, Rare
    R_GeneralSlotExpand    = 109,  // 출병 명령        — 장수 배치 슬롯 +1칸,      Lv1, Epic
    R_BattleSpeed          = 110,  // 시간의 고삐      — 전투 배속 +1단계/레벨,    Lv2, Epic

    // ── 장수 스텟 유물 (2xx) ─────────────────────────────────
    // 전체 장수 (All)
    R_GeneralHp            = 201,  // 장군의 심장      — All 최대 체력 +5%/레벨,       Lv5, Common
    R_GeneralAtk           = 202,  // 전투의 기억      — All 공격력 +4%/레벨,           Lv5, Common
    R_GeneralDef           = 203,  // 철의 의지        — All 방어율 +3%/레벨,           Lv5, Common
    R_GeneralSpd           = 204,  // 질풍의 발걸음    — All 이동속도 +4%/레벨,         Lv5, Common
    R_GeneralRange         = 205,  // 사냥꾼의 눈      — All 공격 사거리 +4%/레벨,      Lv5, Common
    R_GeneralCrit          = 206,  // 비수의 날        — All 치명타 확률 +3%/레벨,      Lv5, Rare
    R_GeneralCooldown      = 207,  // 시간의 압축      — All 스킬 쿨감 +3%p/레벨,       Lv5, Rare  (절대값)
    R_GeneralSoldierCount  = 208,  // 지휘의 깃발      — All 기본 병사 수 +1/레벨,      Lv5, Rare  (절대값)

    // 직업 특화 장수 유물
    R_KnightArmor          = 211,  // 기사왕의 갑주    — Knight 체력 +8%, 방어율 +5%/레벨, Lv3, Rare
    R_ArcherSwift          = 212,  // 활의 정령        — Archer 공속 +8%, 사거리 +6%/레벨, Lv3, Rare
    R_MagePower            = 213,  // 마력의 결정      — Mage 공격력 +10%, 쿨감 +6%p/레벨, Lv3, Epic (절대값)
    R_ShieldFortress       = 214,  // 방벽의 군주      — Shield 방어율 +10%, 체력 +8%/레벨,Lv3, Epic

    // ── 병사 스텟 유물 (3xx) ─────────────────────────────────
    //  Target = Unit_Soldier — 병사에게만 붙는다. 장수는 그대로다.
    R_SoldierAtk           = 301,  // 병사의 혼        — Soldier 공격력 +5%/레벨,     Lv5, Common
    R_SoldierHp            = 302,  // 병사의 생명력    — Soldier 최대 체력 +5%/레벨,  Lv5, Common

    // ── 장수 전용 유물 (4xx) ─────────────────────────────────
    //  Target = Unit_General — 장수에게만 붙고 병사 환산을 타지 않는다.
    //
    //  ⚠ 2xx 의 R_General* 과 이름이 겹치지 않게 OnlyXxx 를 붙였다
    //    2xx 는 이름만 'General' 이지 실제 Target 은 All 이다 — 장수와 병사를
    //    함께 올린다. 여기 4xx 만이 진짜 '장수 전용' 이다.
    //    둘을 같은 이름으로 두면 어느 쪽이 병사에게 가는지 코드에서 구분이 안 된다.
    //
    //  ⚠ 공통(All) 유물과 역할이 다르다
    //    All 은 부대 전체를 조금씩 올린다. 이쪽은 장수 한 명에게 몰아준다.
    //    병사에게 안 가는 대신 배율을 크게 잡아야 고를 이유가 생긴다 —
    //    2xx 공통 유물(+4~5%/레벨)보다 높게 둔 이유다.
    R_OnlyGeneralAtk       = 401,  // 장군의 검        — General 공격력 +9%/레벨,       Lv5, Uncommon
    R_OnlyGeneralHp        = 402,  // 장군의 투구      — General 최대 체력 +9%/레벨,    Lv5, Uncommon
    R_OnlyGeneralDef       = 403,  // 장군의 흉갑      — General 방어율 +3%p/레벨,      Lv3, Rare (절대값)
    R_OnlyGeneralCrit      = 404,  // 장군의 안목      — General 치명확률 +4%p/레벨,    Lv3, Rare (절대값)
    R_OnlyGeneralMight     = 405,  // 영웅의 증표      — General 공격력·체력 +12%/레벨, Lv3, Epic
}
