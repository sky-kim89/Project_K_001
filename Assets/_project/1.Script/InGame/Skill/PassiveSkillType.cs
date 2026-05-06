// ============================================================
//  PassiveSkillType.cs
//  장군 패시브 스킬 종류 열거형
//
//  등급별 활성 슬롯 수 (PassiveSkillRoller.GetActiveSlotCount):
//    Normal / Uncommon = 1슬롯
//    Rare   / Unique   = 2슬롯
//    Epic              = 3슬롯
// ============================================================

public enum PassiveSkillType : byte
{
    None = 0,

    // ── 병사 강화 ──────────────────────────────────────────────
    ExtraSoldiers            = 1,   // 병사 수 +N명
    SoldierCombatBoost       = 2,   // 병사 공격력·이동속도 +X%
    SoldierHorde             = 3,   // 병사 수 +N명, 병사 공체 -X%
    VanguardAura             = 4,   // 병사 방어율 +X%

    // ── 교환 ──────────────────────────────────────────────────
    WeakGeneralStrongSoldier = 5,   // 제너럴 공체 -X%, 병사 공체 +Y%
    StrongGeneralWeakSoldier = 6,   // 병사 공체 -X%, 제너럴 공체 +Y%
    WeakGeneralMoreSoldiers  = 7,   // 제너럴 공체 -X%, 병사 수 +N명
    BerserkerPact            = 8,   // 전체 공이속 +X%, 방어율 -Y%

    // ── 제너럴 강화 ────────────────────────────────────────────
    GeneralCombatBoost       = 9,   // 제너럴 공격력·이동속도 +X%
    TitanGeneral             = 10,  // 제너럴 크기·공체 +X%, 공이속 -Y%
    CommanderFury            = 11,  // 제너럴 크리티컬 확률 +X%, 배율 +Y%

    // ── 시너지 ────────────────────────────────────────────────
    SoldierEmpowerGeneral    = 12,  // 병사 수 × X% → 제너럴 공체 증가
    UnityStrength            = 13,  // 병사 수 × X% → 제너럴 공체 추가 (SoldierEmpower와 수치 다름)
    SoldierDeathEmpower      = 14,  // 병사 사망 시 제너럴 공체 +X%, 공이속 +Y%
    SacrificeRitual          = 15,  // 병사 N명 희생 → 제너럴 공체 +X%

    // ── 조건부 ────────────────────────────────────────────────
    BloodPact                = 16,  // 제너럴 체력 낮을수록 공격력 최대 +X%
    IronWill                 = 17,  // 제너럴 체력 Y% 이하 시 공체 +X% (1회)
    LastStand                = 18,  // 병사 수 초기의 Y% 이하 시 남은 병사 공체 +X% (1회)
}
