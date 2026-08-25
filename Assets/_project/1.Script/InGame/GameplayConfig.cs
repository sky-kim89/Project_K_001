using System;
using UnityEngine;

// ============================================================
//  GameplayConfig.cs
//  인게임 밸런스 수치 중앙 저장소 (ScriptableObject).
//
//  ■ 사용법
//    - Assets → Create → BattleGame → GameplayConfig 로 생성
//    - InGameManager.Inspector 에서 _gameplayConfig 에 할당
//    - 코드에서 GameplayConfig.Current.XXX 로 접근
//
//  ■ 포함 범위
//    디버그 토글 / 패시브 슬롯 / 방어율 상한
//    적·아군 스텟 랜덤 범위 / 레벨·등급 배율 / 등급 뽑기 확률
//
//  ■ 포함하지 않는 범위
//    개별 스킬 수치 (각 스킬 SO 에서 관리)
// ============================================================

[CreateAssetMenu(fileName = "GameplayConfig", menuName = "BattleGame/GameplayConfig")]
public class GameplayConfig : ScriptableObject
{
    // ── 전역 참조 ──────────────────────────────────────────────
    static GameplayConfig _current;
    public static GameplayConfig Current
        => _current != null ? _current : (_current = Resources.Load<GameplayConfig>("GameplayConfig"));

    // ──────────────────────────────────────────────────────────
    // ■ 디버그
    // ──────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────
    // ■ 로비 비용
    // ──────────────────────────────────────────────────────────

    // ── 장수 고용 비용 (등급별) ──────────────────────────────
    //  등급은 스탯 배율 + 패시브 슬롯을 동시에 올린다.
    //  단일 가격이면 낮은 등급 매물은 아무도 안 사고, 높은 등급은 거저 얻는다.
    //  → 등급 차이를 가격으로 드러내 "지금 이 값을 낼 만한가" 를 묻게 한다.

    [Header("장수 고용 비용 — 등급별 (골드)")]
    [Tooltip("일반 등급 고용가")]
    public int HireCostNormal   = 400;
    [Tooltip("고급 등급 고용가")]
    public int HireCostUncommon = 650;
    [Tooltip("희귀 등급 고용가")]
    public int HireCostRare     = 1000;
    [Tooltip("유니크 등급 고용가")]
    public int HireCostUnique   = 1500;
    [Tooltip("영웅 등급 고용가")]
    public int HireCostEpic     = 2200;

    public int GetHireCost(UnitGrade grade) => grade switch
    {
        UnitGrade.Uncommon => HireCostUncommon,
        UnitGrade.Rare     => HireCostRare,
        UnitGrade.Unique   => HireCostUnique,
        UnitGrade.Epic     => HireCostEpic,
        _                  => HireCostNormal,
    };

    /// <summary>Config 미할당 시에도 안전한 정적 진입점 — 상점·용병 팝업 공용.</summary>
    public static int HireCost(UnitGrade grade)
    {
        if (Current != null) return Current.GetHireCost(grade);
        return grade switch
        {
            UnitGrade.Uncommon => 650,
            UnitGrade.Rare     => 1000,
            UnitGrade.Unique   => 1500,
            UnitGrade.Epic     => 2200,
            _                  => 400,
        };
    }

    [Header("장수 레벨업 비용 — Lv N → N+1 = Base + (N-1) × PerLevel")]
    [Tooltip("Lv1 → Lv2 비용 (골드)")]
    public int HeroLevelUpCostBase     = 100;

    [Tooltip("레벨 1당 비용 증가분 (골드). 기본 10 → 100 / 110 / 120 / 130 …")]
    public int HeroLevelUpCostPerLevel = 10;

    /// <summary>Lv currentLevel → currentLevel+1 에 필요한 골드.</summary>
    public int GetHeroLevelUpCost(int currentLevel)
        => HeroLevelUpCostBase + Mathf.Max(0, currentLevel - 1) * HeroLevelUpCostPerLevel;

    /// <summary>Config 미할당 시에도 안전하게 쓸 수 있는 정적 진입점 — UI·로직 공용.</summary>
    public static int HeroLevelUpCost(int currentLevel)
        => Current != null ? Current.GetHeroLevelUpCost(currentLevel)
                           : 100 + Mathf.Max(0, currentLevel - 1) * 10;

    // ──────────────────────────────────────────────────────────
    // ■ 장수 등급업 비용 (장군 강화석)
    // ──────────────────────────────────────────────────────────
    //  등급 1단계 = 전 스탯 ×GradeMultPerTier + 패시브 슬롯 증가(1→2→3).
    //  스테이지 클리어로 강화석이 30스테이지 누적 약 150개 들어온다
    //  → 한 런에 장수 하나를 Normal→Epic(누적 97) 까지 올릴 수 있는 수준.

    [Header("장수 등급업 비용 — 장군 강화석 (현재 등급 → 다음 등급)")]
    [Tooltip("일반 → 고급")]
    public int GradeUpCostToUncommon = 8;
    [Tooltip("고급 → 희귀")]
    public int GradeUpCostToRare     = 16;
    [Tooltip("희귀 → 유니크")]
    public int GradeUpCostToUnique   = 28;
    [Tooltip("유니크 → 에픽")]
    public int GradeUpCostToEpic     = 45;

    /// <summary>현재 등급에서 다음 등급으로 올리는 비용. Epic 이면 0 (더 못 올림).</summary>
    public int GetGradeUpCost(UnitGrade current) => current switch
    {
        UnitGrade.Normal   => GradeUpCostToUncommon,
        UnitGrade.Uncommon => GradeUpCostToRare,
        UnitGrade.Rare     => GradeUpCostToUnique,
        UnitGrade.Unique   => GradeUpCostToEpic,
        _                  => 0,   // Epic = 최대 등급
    };

    /// <summary>Config 미할당 시에도 안전한 정적 진입점 — UI·로직 공용.</summary>
    public static int GradeUpCost(UnitGrade current)
    {
        if (Current != null) return Current.GetGradeUpCost(current);
        return current switch
        {
            UnitGrade.Normal   => 8,
            UnitGrade.Uncommon => 16,
            UnitGrade.Rare     => 28,
            UnitGrade.Unique   => 45,
            _                  => 0,
        };
    }

    // ──────────────────────────────────────────────────────────
    // ■ 유물 비용
    // ──────────────────────────────────────────────────────────

    // ⚠ 희귀도별 "첫 획득 비용" 은 삭제됐다.
    //   유물은 전부 0레벨로 존재하며 획득 단계가 없다. 비용은 강화 비용 하나뿐.

    // ──────────────────────────────────────────────────────────
    // ■ 디버그
    // ──────────────────────────────────────────────────────────

    [Header("디버그 — 에디터 전용")]
    [Tooltip("패시브 스킬 발동 시 Console 에 상세 로그 출력 (에디터 전용)")]
    public bool EnablePassiveLog = true;

    [Tooltip("액티브 스킬 발동 시 Console 에 상세 로그 출력 (에디터 전용)")]
    public bool EnableActiveLog = true;

    // ──────────────────────────────────────────────────────────
    // ■ 패시브 슬롯 (등급별 활성 슬롯 수)
    // ──────────────────────────────────────────────────────────

    [Header("패시브 슬롯 — 등급별 활성 슬롯 수")]
    [Tooltip("Epic 등급 활성 패시브 슬롯 수")]
    public byte EpicSlots     = 3;

    [Tooltip("Unique 등급 활성 패시브 슬롯 수")]
    public byte UniqueSlots   = 2;

    [Tooltip("Rare 등급 활성 패시브 슬롯 수")]
    public byte RareSlots     = 2;

    [Tooltip("Uncommon 등급 활성 패시브 슬롯 수")]
    public byte UncommonSlots = 1;

    [Tooltip("Normal 등급 활성 패시브 슬롯 수")]
    public byte NormalSlots   = 1;

    // ──────────────────────────────────────────────────────────
    // ■ 전투 공통
    // ──────────────────────────────────────────────────────────

    [Header("스테이지 설정")]
    [Tooltip("게임 최대 스테이지 수. StageConfig.NormalStageCount 를 읽는다 — StageConfig 가 없으면 30.")]
    public int MaxStage => StageConfig.Current != null ? StageConfig.Current.NormalStageCount : 30;

    [Header("전투 공통")]
    [Tooltip("방어율 소프트캡 임계값 (0~1). 이 값 초과분은 DefenseOverflowRate 로 성장.")]
    [Range(0f, 1f)]
    public float DefenseMax = 0.95f;

    [Tooltip("소프트캡 초과분에 곱하는 성장 계수. 0.1 = 1/10 성장.")]
    [Range(0.001f, 1f)]
    public float DefenseOverflowRate = 0.1f;

    [Tooltip("스킬 쿨다운 감소율 상한 (0~1). 이 값을 초과하는 쿨감은 무시된다.")]
    [Range(0f, 1f)]
    public float CooldownReduceMax = DefaultCooldownReduceMax;

    /// <summary>
    /// 쿨감 상한 기본값 (0.8 → 0.9, 2026-08-26 상향).
    ///
    /// ⚠ 이 상수만 고치면 안 된다 — 실제로 쓰이는 값은 에셋에 구워져 있다
    ///   `Assets/Resources/GameplayConfig.asset` 의 `CooldownReduceMax` 를 같이 바꿔야
    ///   런타임 동작이 바뀐다. 여기 값은 에셋을 새로 만들 때만 쓰인다.
    /// </summary>
    public const float DefaultCooldownReduceMax = 0.9f;

    /// <summary>
    /// 지금 걸리는 쿨감 상한. **쿨감을 클램프하는 모든 곳이 이것만 쓴다.**
    ///
    /// ⚠ 호출부마다 `?? 0.8f` 를 적지 말 것
    ///   예전엔 폴백 숫자가 네 곳(전투 2 · UI 표시 2)에 흩어져 있었다.
    ///   상한을 올릴 때 한 곳이라도 빠지면 전투와 화면에 뜨는 값이 어긋난다.
    /// </summary>
    public static float CooldownCap =>
        Current != null ? Current.CooldownReduceMax : DefaultCooldownReduceMax;

    [Tooltip("방어율 실효 최대치 (0~1). 소프트캡 공식 적용 후 이 값으로 상한 클램프.")]
    [Range(0f, 1f)]
    public float DefenseEffectiveCap = 0.999f;

    // ──────────────────────────────────────────────────────────
    // ■ 스텟 성장 배율
    // ──────────────────────────────────────────────────────────

    [Header("레벨업 경험치")]
    [Tooltip("Lv N → N+1 에 필요한 EXP = N × ExpPerLevel")]
    public int ExpPerLevel = 100;

    [Header("스텟 성장 배율")]
    [Tooltip("레벨 1포인트당 스텟 배율 증가량.\n기본 0.01 → Lv1=×1.0, Lv100=×1.99")]
    public float LevelMultPerLevel = 0.01f;

    [Tooltip("등급 1단계당 스텟 배율 증가량.\n기본 0.10 → Normal=×1.0, Epic(4단계)=×1.4")]
    public float GradeMultPerTier  = 0.10f;

    [Tooltip("등급 1단계당 '해당 스텟 최댓값의 N%' 를 고정 가산한다.\n" +
             "기본 0.05 → Epic(4단계) = 최댓값의 20% 추가.\n\n" +
             "배율(GradeMultPerTier)만으로는 공격속도·이동속도·사거리처럼\n" +
             "배율이 안 붙는 스텟에 등급이 전혀 반영되지 않는다.\n" +
             "이 값은 모든 굴림 스텟에 동일하게 더해진다.")]
    public float GradeFlatMaxRatio = 0.05f;

    [Header("레벨업 고정 성장 (배율과 별개로 매 레벨 그대로 더해진다)")]
    [Tooltip("레벨 1당 기본 최대체력 가산량")]
    public float LevelFlatHpPerLevel     = 10f;

    [Tooltip("레벨 1당 기본 공격력 가산량")]
    public float LevelFlatAttackPerLevel = 1f;

    // ──────────────────────────────────────────────────────────
    // ■ 용병(병사) 스탯 환산
    // ──────────────────────────────────────────────────────────

    [Header("용병 스탯 비율 — 실제 계산은 SoldierRuntimeBridge.StatRatio 가 소유")]
    [Tooltip("지휘력 0 일 때의 병사 스탯 비율 (장군 대비). 기본 0.2 = 20%")]
    public float SoldierBaseStatRatio = 0.2f;

    [Tooltip("지휘력 1포인트당 병사 스탯 비율 증가량. 기본 0.01 = +1%p")]
    public float SoldierRatioPerCommandPower = 0.01f;

    [Tooltip("병사 스탯 비율 상한. 0 이하면 상한 없음 — 지휘력으로 100% 를 넘길 수 있다.")]
    public float SoldierStatRatioMax = 0f;

    // ──────────────────────────────────────────────────────────
    // ■ 등급 뽑기 확률 (RollGrade)
    // ──────────────────────────────────────────────────────────

    [Header("등급 뽑기 확률 — 합계가 1.0 이 되도록 설정")]
    [Range(0f, 1f)] public float GradeChanceEpic     = 0.03f;   //  3%
    [Range(0f, 1f)] public float GradeChanceUnique   = 0.07f;   //  7%  (누적 10%)
    [Range(0f, 1f)] public float GradeChanceRare     = 0.15f;   // 15%  (누적 25%)
    [Range(0f, 1f)] public float GradeChanceUncommon = 0.25f;   // 25%  (누적 50%)
    // Normal = 나머지 50%

    // ──────────────────────────────────────────────────────────
    // ■ 메인 패널 후보 장수
    // ──────────────────────────────────────────────────────────

    [Header("메인 패널 후보 장수 (슬롯 0=Knight · 1=Archer · 2=Mage · 3=ShieldBearer)")]
    [Tooltip("이름을 비워두면 해당 슬롯은 직업별 랜덤 선택으로 대체됩니다.")]
    public CandidatePreset[] MainPanelCandidates = new CandidatePreset[4];

    // ──────────────────────────────────────────────────────────
    // ■ 적 스텟 범위
    // ──────────────────────────────────────────────────────────

    [Header("적 스텟 범위")]
    public EnemyGradeStatRange EnemyRange;
    public EnemyGradeStatRange EliteRange;
    public EnemyGradeStatRange BossRange;

    // ──────────────────────────────────────────────────────────
    // ■ 아군 직업별 스텟 범위
    // ──────────────────────────────────────────────────────────

    [Header("아군 직업별 스텟 범위")]
    public JobStatRange KnightRange;
    public JobStatRange ArcherRange;
    public JobStatRange MageRange;
    public JobStatRange ShieldBearerRange;

    // ──────────────────────────────────────────────────────────
    // ■ 조회 헬퍼
    // ──────────────────────────────────────────────────────────

    public byte GetPassiveSlotCount(UnitGrade grade) => grade switch
    {
        UnitGrade.Epic     => EpicSlots,
        UnitGrade.Unique   => UniqueSlots,
        UnitGrade.Rare     => RareSlots,
        UnitGrade.Uncommon => UncommonSlots,
        _                  => NormalSlots,
    };

    public JobStatRange GetJobRange(UnitJob job) => job switch
    {
        UnitJob.Archer       => ArcherRange,
        UnitJob.Mage         => MageRange,
        UnitJob.ShieldBearer => ShieldBearerRange,
        _                    => KnightRange,
    };

    public EnemyGradeStatRange GetEnemyRange(SpawnUnitType type) => type switch
    {
        SpawnUnitType.Elite => EliteRange,
        SpawnUnitType.Boss  => BossRange,
        _                   => EnemyRange,
    };

    // ──────────────────────────────────────────────────────────
    // ■ 에디터 — 신규 생성 시 디폴트 값 주입
    // ──────────────────────────────────────────────────────────

    void Reset()
    {
        // 적 스텟 범위 — 스테이지 배율(StatMultiplier)·레벨 배율이 별도 적용되므로
        // 여기는 스테이지 1 기준값만 정의한다.
        EnemyRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(240f,  720f),   // ×2 상향 후 +20% (아군 대비 격차 해소)
            Attack      = new FloatRange(9.9f,  35.75f), // 전투 지속시간 확보(×0.5) 후 +10%
            Defense     = new FloatRange(0.05f, 0.15f),
            AttackRange = new FloatRange(1.2f,  2.5f),
            AttackSpeed = new FloatRange(0.6f,  1.6f),
            MoveSpeed   = new FloatRange(2.0f,  4.0f),
            CritChance  = 0.05f,
            CritDamage  = 1.50f,
        };
        EliteRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(720f,  2160f),  // ×1.5 상향 후 +20% — 엘리트 체감이 약했다
            Attack      = new FloatRange(41.25f, 123.75f), // 전투 지속시간 확보(×0.5) 후 +10%
            Defense     = new FloatRange(0.10f, 0.55f),  // 에셋 실값과 동기화
            AttackRange = new FloatRange(1.5f,  3.0f),
            AttackSpeed = new FloatRange(0.8f,  2.0f),
            MoveSpeed   = new FloatRange(2.5f,  4.5f),
            CritChance  = 0.08f,
            CritDamage  = 1.60f,
        };
        // ⚠ 보스는 '느리고 무겁게' — 공격속도 1/3, 평타 피해 3배 (DPS 는 그대로)
        //   초당 여러 번 깨작거리면 1,000마리 난전 속에서 보스가 안 보인다.
        //   한 대씩 크게 때려야 "보스한테 맞았다" 가 화면과 숫자로 읽힌다.
        //   AoE·넉백이 이 한 방에 얹히므로 체감 차이가 더 벌어진다.
        //   단, ×3 은 여기 Attack 이 아니라 BossAttackSystem.BasicAttackMultiplier 가 갖는다.
        //   스텟을 올리면 보스 스킬(슬램·돌진·사형선고) 피해까지 3배가 되기 때문이다.
        BossRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(3750f, 10500f), // ×1.8 상향 후 -25% (보스전이 너무 길었다)
            Attack      = new FloatRange(110f,  275f),   // 스킬 계수의 기준값 — 평타 ×3 은 시스템이 얹는다 (전원 ×0.5)
            Defense     = new FloatRange(0.25f, 0.50f),
            AttackRange = new FloatRange(2.5f,  4.5f),
            AttackSpeed = new FloatRange(0.133f, 0.30f), // ÷3 — 3~7.5초에 한 번
            MoveSpeed   = new FloatRange(2.0f,  3.5f),
            CritChance  = 0.08f,
            CritDamage  = 1.60f,
        };

        // 아군 직업별 스텟 범위 — 직업 간 DPS 균형 조정
        // 평균 DPS(크리 포함): Knight ~170, Archer ~158, Mage ~155, ShieldBearer ~90(탱킹 보정)
        KnightRange = new JobStatRange
        {
            Hp           = new FloatRange(960f,   1920f),  // +20% 상향
            Attack       = new FloatRange(45f,    100f),   // 근접 고위험 고딜 정체성 (전원 ×0.5)
            Defense      = new FloatRange(0.06f,  0.15f),   // ↓ 하향 — 방패병과 격차 확보 (기사는 체력·공격으로 버틴다)
            AttackRange  = new FloatRange(0.8f,   1.2f),
            AttackSpeed  = new FloatRange(0.9f,   1.9f),
            MoveSpeed    = new FloatRange(2.5f,   3.0f),
            SoldierCount = new FloatRange(3f,     7f),     // ↑ 병사 특화 — 타 직업(1~4) 대비 상향
            CommandPower = new FloatRange(10f,    45f),    // ↑ 병사 특화 — 타 직업(1~30) 대비 상향
            CritChance   = 0.13f,                          // ↑ 10% → 13%
            CritDamage   = 1.50f,
        };
        ArcherRange = new JobStatRange
        {
            Hp           = new FloatRange(577.5f, 1237.5f), // +50% 상향 후 +10% — 넉백으로 버는 시간을 살리려면
                                                           //   한 번 붙였을 때 한두 대는 버텨야 한다
            Attack       = new FloatRange(30f,    70f),    // 평타 위주 정체성 (전원 ×0.5)
            Defense      = new FloatRange(0.03f,  0.10f),
            AttackRange  = new FloatRange(5.5f,   9.9f),   // +10% 상향 — 화력을 뺀 만큼 거리로 돌려준다
            AttackSpeed  = new FloatRange(0.9f,   1.9f),   // 소폭 상향
            MoveSpeed    = new FloatRange(2.0f,   2.5f),
            SoldierCount = new FloatRange(1f,     2f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.15f,
            CritDamage   = 1.80f,
        };
        MageRange = new JobStatRange
        {
            Hp           = new FloatRange(445.5f, 1023f),  // +50% 상향 후 +10%
            Attack       = new FloatRange(85f,    185f),   // 한 방이 굵은 것이 법사의 정체성 (전원 ×0.5)
                                                          // ⚠ 하한을 올린 이유 — 못 뽑힌 법사가 게임에서 제일 약했다
                                                          //   공속이 궁수의 1/3 이라 공격력 하한이 낮으면 DPS 바닥이
                                                          //   궁수·기사보다도 아래로 내려간다. 상한은 그대로 두어
                                                          //   '한 방이 굵다' 는 정체성과 뽑기의 진폭은 유지한다.
            Defense      = new FloatRange(0.02f,  0.08f),
            AttackRange  = new FloatRange(4.0f,   7.0f),
            AttackSpeed  = new FloatRange(0.315f, 0.675f), // -10% 하향 — DPS 는 공속으로만 깎는다
            MoveSpeed    = new FloatRange(1.5f,   2.0f),
            SoldierCount = new FloatRange(1f,     2f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.10f,
            CritDamage   = 2.00f,
        };
        ShieldBearerRange = new JobStatRange
        {
            Hp           = new FloatRange(1200f,  3000f),  // -25% 하향 (과도한 HP 조정)
            Attack       = new FloatRange(25f,    60f),    // (전원 ×0.5)
            Defense      = new FloatRange(0.22f,  0.32f),  // 상한만 조인다 — 하한이 낮으면 "방패병인데 물렁한"
                                                           //   개체가 나온다. 폭을 좁혀 항상 단단하되
                                                           //   장비·유물을 얹어도 실효 90% 에는 쉽게 닿지 않게.
            AttackRange  = new FloatRange(0.7f,   1.0f),
            AttackSpeed  = new FloatRange(0.6f,   1.4f),   // 소폭 상향
            MoveSpeed    = new FloatRange(2.0f,   2.5f),
            SoldierCount = new FloatRange(1f,     2f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.05f,
            CritDamage   = 1.50f,
        };
    }
}

// ──────────────────────────────────────────────────────────────
// ■ 공유 구조체
// ──────────────────────────────────────────────────────────────

/// <summary>스텟 랜덤 범위 (Min~Max). Inspector 에서 x=Min, y=Max 로 표시.</summary>
[Serializable]
public struct FloatRange
{
    [Tooltip("최솟값")]
    public float Min;

    [Tooltip("최댓값")]
    public float Max;

    public FloatRange(float min, float max) { Min = min; Max = max; }

    /// <summary>t(0~1) 로 Min~Max 선형 보간.</summary>
    public float Lerp(float t) => Min + (Max - Min) * t;
}

/// <summary>적 등급별 스텟 범위.</summary>
[Serializable]
public struct EnemyGradeStatRange
{
    public FloatRange Hp;
    public FloatRange Attack;
    public FloatRange Defense;
    public FloatRange AttackRange;
    public FloatRange AttackSpeed;
    public FloatRange MoveSpeed;

    [Tooltip("고정 크리티컬 확률 (등급 내 동일)")]
    public float CritChance;

    [Tooltip("고정 크리티컬 데미지 배율 (등급 내 동일)")]
    public float CritDamage;
}

/// <summary>메인 패널 후보 장수 프리셋 1개.</summary>
[Serializable]
public struct CandidatePreset
{
    [Tooltip("후보 장수 이름 (직업은 이름 시드로 자동 결정, 비우면 직업별 랜덤)")]
    public string    Name;
    [Tooltip("표시 등급 — 이름의 태생 등급보다 낮으면 태생 등급이 유지됩니다")]
    public UnitGrade Grade;
}

/// <summary>아군 직업별 스텟 범위.</summary>
[Serializable]
public struct JobStatRange
{
    public FloatRange Hp;
    public FloatRange Attack;
    public FloatRange Defense;
    public FloatRange AttackRange;
    public FloatRange AttackSpeed;
    public FloatRange MoveSpeed;
    public FloatRange SoldierCount;
    public FloatRange CommandPower;

    [Tooltip("고정 크리티컬 확률 (직업 내 동일)")]
    public float CritChance;

    [Tooltip("고정 크리티컬 데미지 배율 (직업 내 동일)")]
    public float CritDamage;
}
