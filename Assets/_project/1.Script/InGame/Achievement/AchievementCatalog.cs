using System.Collections.Generic;

// ============================================================
//  AchievementCatalog.cs
//  업적 ID 와 표시 정보(이름·설명·숨김 여부)의 정본.
//
//  ■ 전부 1회성이다 — 단계형(10마리 → 100마리 → …) 업적은 두지 않는다.
//  ■ 판정 로직은 AchievementTracker 에 있다. 여기는 "무엇이 있는가" 만 적는다.
//
//  ⚠ 번호는 세이브에 그대로 남는다 — 지운 번호를 다른 업적에 재사용하지 말 것.
//  ⚠ 번호 규칙이 곧 매핑이다
//    희귀 장수 = 300 + ActiveSkillId (Bisect 21 → 321)
//    직업 달인 = AbilityId 와 같은 번호 (D01 401 → KnightMaster 401)
//    직업 편성 = 201 + UnitJob
// ============================================================

public enum AchievementId
{
    None = 0,

    // ── 여정 진행 (1~) ─────────────────────────────────────
    FirstStage          = 1,   // 1스테이지 클리어
    FirstHurdle         = 2,   // 5스테이지(첫 엘리트) 클리어
    HalfWay             = 3,   // 15스테이지 클리어
    JourneyEnd          = 4,   // 30스테이지 클리어
    NewTrial            = 5,   // 난이도 해금
    HellClear           = 6,   // 지옥 난이도 30스테이지 클리어
    InfernoClear        = 7,   // 불지옥 난이도 30스테이지 클리어
    FirstReincarnation  = 8,   // 처음으로 환생

    // ── 전투 (101~) ────────────────────────────────────────
    MiracleSurvivor     = 101, // 병사 단 1명만 살아남은 채 승리 (장수는 세지 않는다)
    LastGeneral         = 102, // 장수 단 1명만 살아남은 채 승리 (병사는 세지 않는다, 2명 이상 편성)

    // ── 부대 편성 (201~) — 201 + UnitJob ───────────────────
    KnightOrder         = 201, // 장수 5명 전원 기사로 편성해 클리어
    ArcherCorps         = 202, // 〃 궁수
    MageCorps           = 203, // 〃 마법사
    ShieldWall          = 204, // 〃 방패병
    LoneWolf            = 205, // 병사가 0명인 장수를 편성해 클리어

    // ── 장수 (301~) ────────────────────────────────────────
    LegendaryGeneral    = 301, // 장수를 최고 등급(에픽)으로 등급업

    // 희귀 장수 고용 — 300 + ActiveSkillId
    RareHeroBisect          = 321,
    RareHeroArrowStorm      = 322,
    RareHeroGravityCollapse = 323,
    RareHeroBulwark         = 324,
    RareHeroChainLightning  = 325,
    RareHeroDeathSentence   = 326,
    RareHeroBloodPrice      = 327,
    RareHeroPiercingDash    = 328,
    RareHeroWarBanner       = 329,
    RareHeroGravestone      = 330,

    // ── 직업 달인 어빌리티 (401~) — AbilityId.D01~D04 와 같은 번호 ──
    KnightMaster        = 401,
    ArcherMaster        = 402,
    MageMaster          = 403,
    ShieldMaster        = 404,

    // ── 수집 (501~) ────────────────────────────────────────
    Collector           = 501, // 도감 한 분류 완성
    CodexComplete       = 502, // 도감 전 분류 완성
    RelicRoot           = 503, // 유물 노드 처음 습득
    RelicComplete       = 504, // 모든 유물 노드 최대 레벨
}

public readonly struct AchievementDef
{
    public readonly AchievementId Id;
    public readonly string        Name;
    public readonly string        Description;

    /// <summary>달성 전에는 이름·설명을 "???" 로 가린다 — 발견하는 재미가 있는 업적.</summary>
    public readonly bool          Hidden;

    public AchievementDef(AchievementId id, string name, string description, bool hidden = false)
    {
        Id = id; Name = name; Description = description; Hidden = hidden;
    }
}

public static class AchievementCatalog
{
    public const int RareHeroBase = 300;

    static readonly (ActiveSkillId skill, string skillName)[] RareSkills =
    {
        (ActiveSkillId.Bisect,          "일도양단"),
        (ActiveSkillId.ArrowStorm,      "화살 폭풍"),
        (ActiveSkillId.GravityCollapse, "중력 붕괴"),
        (ActiveSkillId.Bulwark,         "불멸의 방벽"),
        (ActiveSkillId.ChainLightning,  "연쇄 번개"),
        (ActiveSkillId.DeathSentence,   "사형 선고"),
        (ActiveSkillId.BloodPrice,      "피의 대가"),
        (ActiveSkillId.PiercingDash,    "관통 돌진"),
        (ActiveSkillId.WarBanner,       "군기 강림"),
        (ActiveSkillId.Gravestone,      "비석 강림"),
    };

    static readonly Dictionary<AchievementId, AchievementDef> _defs = new();
    static readonly List<AchievementDef>                      _ordered = new();

    /// <summary>화면에 늘어놓는 순서 (enum 선언 순서).</summary>
    public static IReadOnlyList<AchievementDef> All => _ordered;

    public static AchievementDef Get(AchievementId id) => _defs[id];

    /// <summary>희귀 스킬 → 그 주인을 고용하는 업적.</summary>
    public static AchievementId RareHeroOf(ActiveSkillId skill) => (AchievementId)(RareHeroBase + (int)skill);

    public static IEnumerable<ActiveSkillId> RareSkillIds
    {
        get { foreach (var (skill, _) in RareSkills) yield return skill; }
    }

    static AchievementCatalog()
    {
        // 여정 진행
        Add(AchievementId.FirstStage,         "첫 출정",   "1스테이지를 클리어한다.");
        Add(AchievementId.FirstHurdle,        "첫 관문",   "5스테이지를 클리어한다.");
        Add(AchievementId.HalfWay,            "반환점",    "15스테이지를 클리어한다.");
        Add(AchievementId.JourneyEnd,         "난세의 끝", "30스테이지를 클리어한다.");
        Add(AchievementId.NewTrial,           "새로운 시련", "새로운 난이도를 해금한다.");
        Add(AchievementId.HellClear,          "지옥 불",   $"{DifficultyTier.Hell.Label()} 난이도로 30스테이지를 클리어한다.");
        Add(AchievementId.InfernoClear,       "초열 정복", $"{DifficultyTier.Inferno.Label()} 난이도로 30스테이지를 클리어한다.");
        Add(AchievementId.FirstReincarnation, "첫 윤회",   "처음으로 환생한다.");

        // 전투
        Add(AchievementId.MiracleSurvivor, "기적의 생환", "병사가 단 1명만 살아남은 채 전투에서 승리한다.", hidden: true);
        Add(AchievementId.LastGeneral,     "최후의 장수", "장수를 2명 이상 편성하고, 단 1명만 살아남은 채 전투에서 승리한다.", hidden: true);

        // 부대 편성
        Add(AchievementId.KnightOrder, "기사단",    "장수 5명을 모두 기사로 편성해 클리어한다.");
        Add(AchievementId.ArcherCorps, "궁병대",    "장수 5명을 모두 궁수로 편성해 클리어한다.");
        Add(AchievementId.MageCorps,   "마도 군단", "장수 5명을 모두 마법사로 편성해 클리어한다.");
        Add(AchievementId.ShieldWall,  "철벽 방진", "장수 5명을 모두 방패병으로 편성해 클리어한다.");
        Add(AchievementId.LoneWolf,    "외로운 늑대", "병사가 0명인 장수를 편성해 클리어한다.", hidden: true);

        // 장수
        Add(AchievementId.LegendaryGeneral, "전설의 장수", "장수를 에픽 등급으로 등급업한다.");
        foreach (var (skill, skillName) in RareSkills)
            Add(RareHeroOf(skill), $"{skillName}의 주인", $"희귀 스킬 [{skillName}]을 쓰는 장수를 고용한다.");

        // 직업 달인
        Add(AchievementId.KnightMaster, "기사 달인",   "기사 달인 어빌리티를 획득한다.");
        Add(AchievementId.ArcherMaster, "궁수 달인",   "궁수 달인 어빌리티를 획득한다.");
        Add(AchievementId.MageMaster,   "마법사 달인", "마법사 달인 어빌리티를 획득한다.");
        Add(AchievementId.ShieldMaster, "방패병 달인", "방패병 달인 어빌리티를 획득한다.");

        // 수집
        Add(AchievementId.Collector,     "수집가",      "도감 한 분류를 완성한다.");
        Add(AchievementId.CodexComplete, "만물 도감",   "도감의 모든 분류를 완성한다.");
        Add(AchievementId.RelicRoot,     "뿌리 내림",   "유물을 처음으로 습득한다.");
        Add(AchievementId.RelicComplete, "유물의 정점", "모든 유물을 최대 레벨까지 올린다.");
    }

    static void Add(AchievementId id, string name, string description, bool hidden = false)
    {
        var def = new AchievementDef(id, name, description, hidden);
        _defs.Add(id, def);
        _ordered.Add(def);
    }
}
