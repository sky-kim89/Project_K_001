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
    Honor               = 4,   // 명예 — PvP 콘텐츠용
    ReincarnationPoint  = 5,   // 환생 포인트 — 유물 강화에 사용

    // ── 성장 재료 (Material) ────────────────── 100~199
    ExpBook             = 100,  // 경험서         — 유닛 경험치
    // ⚠ 전투석은 폐지됐다 — 주는 곳도 쓰는 곳도 없다
    //   소비처가 이벤트 선택지 하나뿐이라 쌓이기만 하는 재화였다.
    //   스테이지 클리어·전쟁 유물 보상은 전부 EquipUpgradeStone 으로 넘어갔다.
    //   enum 값은 남긴다 — 옛 세이브에 수량이 들어 있어 지우면 로드가 깨진다.
    //   새로 지급하는 코드를 추가하지 말 것.
    BattleStone         = 101,  // 전투석         — (폐지) 지급처 없음
    SkillScroll         = 102,  // 스킬 서적      — 스킬 잠금 해제
    GeneralUpgradeStone = 103,  // 장군 강화석    — 장수 등급업에 사용 (HeroDetailPopup)
    EquipUpgradeStone   = 104,  // 장비 강화석    — 장비 강화에 사용
    SoldierShard        = 105,  // 용병조각       — 영웅 용병 수 증가에 사용

    // ── 랜덤 박스 (Box) ─────────────────────── 200~299
    EquipBox = 200,  // 장비 랜덤 박스 — 개봉 시 EquipmentDatabase.PickRandom()

    // ── 특수 위임 (Special) ─────────────────── 900~
    //  수량을 저장하지 않는다. SpecificId 로 "무엇을" 주는지 지정하고
    //  RewardOpener 가 해당 데이터 섹션에 직접 넣는다.
    General   = 900,   // 장군 지급 → GeneralManager 위임
    Equipment = 901,   // 장비 지급 → SpecificId = EquipmentId
    Trait     = 902,   // 특성 지급 → SpecificId = TraitType 이름 (예: "Event_BattleWill")
    Ability   = 903,   // 어빌리티 지급 → SpecificId = AbilityId 이름
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
        eItem.ReincarnationPoint  => "환생 포인트",
        eItem.ExpBook             => "경험서",
        eItem.BattleStone         => "전투석",
        eItem.SkillScroll         => "스킬 서적",
        eItem.GeneralUpgradeStone => "장군 강화석",
        eItem.EquipUpgradeStone   => "장비 강화석",
        eItem.SoldierShard        => "용병조각",
        eItem.EquipBox            => "장비 박스",
        eItem.General             => "장군",
        eItem.Equipment           => "장비",
        eItem.Trait               => "특성",
        eItem.Ability             => "어빌리티",
        _                         => item.ToString(),
    };

    /// <summary>랜덤 박스 타입 여부. RewardOpener 분기·팝업 UI 에 사용.</summary>
    public static bool IsBoxType(this eItem item) => (int)item >= 200 && (int)item <= 299;

    /// <summary>
    /// Special 아이템 여부 (900~). 수량을 저장하지 않고 SpecificId 로 대상을 지정한다.
    /// 지급 처리는 RewardOpener 가 담당한다.
    /// </summary>
    public static bool IsSpecialType(this eItem item) => (int)item >= 900;

    /// <summary>
    /// "이 재화가 어디에 쓰이는가" 한 줄 설명. 보상 카드 툴팁에 표시된다.
    /// 실제 소비처 기준으로 적을 것 — 새 소비처가 생기면 여기도 갱신한다.
    /// </summary>
    public static string UsageDesc(this eItem item) => item switch
    {
        eItem.Gold                => "용병 고용, 장수 레벨업, 장비 강화, 환생에 쓰인다.",
        eItem.Gem                 => "상점의 특별 상품을 구매할 때 쓰인다.",
        eItem.Energy              => "스테이지에 입장할 때 소모된다.",
        eItem.Stamina             => "반복 콘텐츠 입장에 소모된다.",
        eItem.Honor               => "PvP 콘텐츠 보상 교환에 쓰인다.",
        eItem.ReincarnationPoint  => "환생 후 유물을 강화할 때 쓰인다.",
        eItem.ExpBook             => "장수에게 사용해 경험치를 올린다.",
        eItem.BattleStone         => "더 이상 쓰이지 않는 재화다.",
        eItem.SkillScroll         => "장수의 잠긴 스킬을 해제할 때 쓰인다.",
        eItem.GeneralUpgradeStone => "장수의 등급을 한 단계 올릴 때 쓰인다.",
        eItem.EquipUpgradeStone   => "장비를 강화할 때 쓰인다.",
        eItem.SoldierShard        => "장수가 지휘하는 용병 수를 늘릴 때 쓰인다.",
        eItem.EquipBox            => "열면 무작위 장비가 하나 나온다.",
        eItem.General             => "새 장수가 부대에 합류한다.",
        eItem.Equipment           => "장비 인벤토리에 추가된다.",
        eItem.Trait               => "이번 런 동안 유지되는 특성이다.",
        eItem.Ability             => "이번 런 동안 유지되는 어빌리티다.",
        _                         => "",
    };

    /// <summary>SpriteManager 조회용 스프라이트 이름 (PNG 파일명 기준).</summary>
    public static string IconKey(this eItem item) => item switch
    {
        eItem.Gold                => "item_gold",
        eItem.Gem                 => "item_gem",
        eItem.Energy              => "item_energy",
        eItem.Stamina             => "item_stamina",
        eItem.Honor               => "item_honor",
        eItem.ReincarnationPoint  => "item_reincarnation_point",
        eItem.ExpBook             => "item_expbook",
        eItem.BattleStone         => "item_battlestone",
        eItem.SkillScroll         => "item_skillscroll",
        eItem.GeneralUpgradeStone => "item_general_upgrade_stone",
        eItem.EquipUpgradeStone   => "item_equip_upgrade_stone",
        eItem.SoldierShard        => "item_soldier_shard",
        eItem.EquipBox            => "item_equipbox",
        _                         => "",
    };
}
