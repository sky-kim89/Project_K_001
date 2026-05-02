// ============================================================
//  eItem.cs
//  게임 내 모든 재화·아이템 타입 정의.
//
//  범위 규칙:
//    0~99    Currency  — 기본 재화 (골드, 잼, 에너지 등)
//    100~199 Material  — 성장 재료 (경험서, 전투석, 강화석 등)
//    200~299 Box       — 랜덤 박스 (개봉 시 eItem 타입으로 분기 처리)
//    900~    Special   — 별도 매니저 위임 (장군, 장비)
//                         → ItemData.Add() 호출 시 수량 저장 안 하고 이벤트만 발생
//
//  새 재화 추가: 해당 범위 안에 항목 추가 → ItemDatabase.asset 에 메타 등록.
// ============================================================

public enum eItem
{
    None = -1,

    // ── 기본 재화 (Currency) ─────────────────── 0~99
    Gold    = 0,
    Gem     = 1,
    Energy  = 2,
    Stamina = 3,
    Honor   = 4,   // 명예 — PvP 콘텐츠용

    // ── 성장 재료 (Material) ────────────────── 100~199
    ExpBook             = 100,  // 경험서         — 유닛 경험치
    BattleStone         = 101,  // 전투석         — 스테이지 클리어 보상
    SkillScroll         = 102,  // 스킬 서적      — 스킬 잠금 해제
    GeneralUpgradeStone = 103,  // 장군 강화석    — 장군 강화에 사용
    EquipUpgradeStone   = 104,  // 장비 강화석    — 장비 강화에 사용

    // ── 랜덤 박스 (Box) ─────────────────────── 200~299
    EquipBox = 200,  // 장비 랜덤 박스 — 개봉 시 EquipmentDatabase.PickRandom()

    // ── 특수 위임 (Special) ─────────────────── 900~
    General   = 900,   // 장군 지급 → GeneralManager 위임
    Equipment = 901,   // 장비 지급 → EquipmentManager 위임
}

// ── 재화 카테고리 ─────────────────────────────────────────────

public enum ItemCategory
{
    Currency = 0,
    Material = 1,
    Box      = 3,   // 랜덤 박스 — 개봉 후 결과 아이템 획득
    Special  = 2,   // 별도 매니저 위임
}

// ── 표시 이름 / 박스 여부 확장 ────────────────────────────────

public static class ItemExtensions
{
    public static string DisplayName(this eItem item) => item switch
    {
        eItem.Gold                => "골드",
        eItem.Gem                 => "잼",
        eItem.Energy              => "에너지",
        eItem.Stamina             => "스태미나",
        eItem.Honor               => "명예",
        eItem.ExpBook             => "경험서",
        eItem.BattleStone         => "전투석",
        eItem.SkillScroll         => "스킬 서적",
        eItem.GeneralUpgradeStone => "장군 강화석",
        eItem.EquipUpgradeStone   => "장비 강화석",
        eItem.EquipBox            => "장비 박스",
        eItem.General             => "장군",
        eItem.Equipment           => "장비",
        _                         => item.ToString(),
    };

    /// <summary>랜덤 박스 타입 여부. RewardOpener 분기·팝업 UI 에 사용.</summary>
    public static bool IsBoxType(this eItem item) => (int)item >= 200 && (int)item <= 299;

    /// <summary>SpriteManager 조회용 스프라이트 이름 (PNG 파일명 기준).</summary>
    public static string IconKey(this eItem item) => item switch
    {
        eItem.Gold                => "item_gold",
        eItem.Gem                 => "item_gem",
        eItem.Energy              => "item_energy",
        eItem.Stamina             => "item_stamina",
        eItem.Honor               => "item_honor",
        eItem.ExpBook             => "item_expbook",
        eItem.BattleStone         => "item_battlestone",
        eItem.SkillScroll         => "item_skillscroll",
        eItem.GeneralUpgradeStone => "item_general_upgrade_stone",
        eItem.EquipUpgradeStone   => "item_equip_upgrade_stone",
        eItem.EquipBox            => "item_equipbox",
        _                         => "",
    };
}
