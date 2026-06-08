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

    [Header("로비 비용")]
    [Tooltip("용병 고용 시 소모 골드")]
    public int HireMercenaryCost = 500;

    // ──────────────────────────────────────────────────────────
    // ■ 유물 비용
    // ──────────────────────────────────────────────────────────

    [Header("유물 비용 — 획득")]
    [Tooltip("Common 유물 첫 획득 비용 (환생 포인트)")]
    public int RelicAcquireCostCommon    = 1;
    [Tooltip("Uncommon 유물 첫 획득 비용 (환생 포인트)")]
    public int RelicAcquireCostUncommon  = 2;
    [Tooltip("Rare 유물 첫 획득 비용 (환생 포인트)")]
    public int RelicAcquireCostRare      = 3;
    [Tooltip("Epic 유물 첫 획득 비용 (환생 포인트)")]
    public int RelicAcquireCostEpic      = 4;
    [Tooltip("Legendary 유물 첫 획득 비용 (환생 포인트)")]
    public int RelicAcquireCostLegendary = 5;

    [Header("유물 비용 — 레벨업")]
    [Tooltip("레벨업 비용 지수. 기본 2 → (현재레벨+1)^지수 pt.\n예) 지수 2: 0→1: 1pt, 1→2: 4pt, 4→5: 25pt")]
    public float RelicLevelUpCostExponent = 2f;

    public int GetRelicAcquireCost(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common    => RelicAcquireCostCommon,
        RelicRarity.Uncommon  => RelicAcquireCostUncommon,
        RelicRarity.Rare      => RelicAcquireCostRare,
        RelicRarity.Epic      => RelicAcquireCostEpic,
        RelicRarity.Legendary => RelicAcquireCostLegendary,
        _ => RelicAcquireCostUncommon,
    };

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
    public float CooldownReduceMax = 0.9f;

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
            Hp          = new FloatRange(200f,  600f),   // ×2 상향 (아군 대비 격차 해소)
            Attack      = new FloatRange(18f,   65f),    // ×1.7 상향
            Defense     = new FloatRange(0.05f, 0.15f),
            AttackRange = new FloatRange(1.2f,  2.5f),
            AttackSpeed = new FloatRange(0.6f,  1.6f),
            MoveSpeed   = new FloatRange(2.0f,  4.0f),
            CritChance  = 0.05f,
            CritDamage  = 1.50f,
        };
        EliteRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(700f,  2000f),  // ×1.7 상향
            Attack      = new FloatRange(70f,   210f),   // ×1.5 상향
            Defense     = new FloatRange(0.12f, 0.32f),
            AttackRange = new FloatRange(1.5f,  3.0f),
            AttackSpeed = new FloatRange(0.8f,  2.0f),
            MoveSpeed   = new FloatRange(2.5f,  4.5f),
            CritChance  = 0.08f,
            CritDamage  = 1.60f,
        };
        BossRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(5000f, 14000f), // ×1.8 상향 (AoE 공격 보정)
            Attack      = new FloatRange(220f,  550f),   // AoE 기준 단일 피해
            Defense     = new FloatRange(0.25f, 0.50f),
            AttackRange = new FloatRange(2.5f,  4.5f),
            AttackSpeed = new FloatRange(0.40f, 0.90f),  // AoE라 느리게 유지
            MoveSpeed   = new FloatRange(2.0f,  3.5f),
            CritChance  = 0.08f,
            CritDamage  = 1.60f,
        };

        // 아군 직업별 스텟 범위 — 직업 간 DPS 균형 조정
        // 평균 DPS(크리 포함): Knight ~170, Archer ~158, Mage ~155, ShieldBearer ~90(탱킹 보정)
        KnightRange = new JobStatRange
        {
            Hp           = new FloatRange(800f,   1600f),
            Attack       = new FloatRange(90f,    200f),   // ↑ 근접 고위험 고딜 정체성
            Defense      = new FloatRange(0.08f,  0.20f),
            AttackRange  = new FloatRange(0.8f,   1.2f),
            AttackSpeed  = new FloatRange(0.9f,   1.9f),
            MoveSpeed    = new FloatRange(2.5f,   3.0f),
            SoldierCount = new FloatRange(1f,     2f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.13f,                          // ↑ 10% → 13%
            CritDamage   = 1.50f,
        };
        ArcherRange = new JobStatRange
        {
            Hp           = new FloatRange(350f,   750f),   // +10% 상향
            Attack       = new FloatRange(60f,    140f),   // +8% 상향
            Defense      = new FloatRange(0.03f,  0.10f),
            AttackRange  = new FloatRange(5.0f,   9.0f),
            AttackSpeed  = new FloatRange(0.9f,   1.9f),   // 소폭 상향
            MoveSpeed    = new FloatRange(2.0f,   2.5f),
            SoldierCount = new FloatRange(1f,     2f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.15f,
            CritDamage   = 1.80f,
        };
        MageRange = new JobStatRange
        {
            Hp           = new FloatRange(270f,   620f),   // +4% 상향
            Attack       = new FloatRange(130f,   370f),   // +6% 상향
            Defense      = new FloatRange(0.02f,  0.08f),
            AttackRange  = new FloatRange(4.0f,   7.0f),
            AttackSpeed  = new FloatRange(0.35f,  0.75f),
            MoveSpeed    = new FloatRange(1.5f,   2.0f),
            SoldierCount = new FloatRange(1f,     2f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.10f,
            CritDamage   = 2.00f,
        };
        ShieldBearerRange = new JobStatRange
        {
            Hp           = new FloatRange(1200f,  3000f),  // -25% 하향 (과도한 HP 조정)
            Attack       = new FloatRange(50f,    120f),   // +50% 상향 (DPS 보정)
            Defense      = new FloatRange(0.30f,  0.55f),  // +5% 상향 (탱킹 특화)
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
