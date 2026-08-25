// ============================================================
//  RelicEnums.cs
//  유물 시스템 효과 열거형.
//
//  ⚠ 구 유물(RelicId·RelicRarity·RelicCategory·RelicEffectType)은 제거됐다
//    카드 그리드 시절의 SO 기반 유물이 쓰던 것들이다. 지금은 테크트리
//    (RelicTreeCatalog)가 대신하고, 트리는 아래 RelicSystemEffect 만 쓴다.
//    희귀도·카테고리는 트리에 없다 — 세기는 Tier 가 말한다.
// ============================================================

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

    GeneralSlotBonus       = 9,  // 장수 배치 슬롯 +N칸 (기본은 RelicTreeApplier.BaseGeneralSlots)
    BattleSpeedUnlock      = 10, // 전투 배속 해금 +N단계 (기본 1× 만) — 트리에서 2×·3× 노드가 나뉘어 있다

    /// <summary>여정 시작 시 무작위 특성을 N개 들고 시작한다. TraitDatabase 에서 추첨.</summary>
    RandomTraitOnStart     = 11,

    /// <summary>
    /// 독고다이 — 트리로 <b>깎아낸</b> 병사 1명당 장수 공격력·체력 +N%.
    ///
    /// ⚠ '현재 병사가 적을수록' 이 아니라 '트리로 줄인 만큼' 이다
    ///   전투 중 병사가 죽었다고 장수가 강해지면 전멸 직전이 가장 센 판이 된다.
    ///   기준은 역분기 노드가 깎은 고정 수치(RelicTreeCatalog 의 음수 SoldierCount 합)다.
    /// </summary>
    LoneWolfBonus          = 12,
}
