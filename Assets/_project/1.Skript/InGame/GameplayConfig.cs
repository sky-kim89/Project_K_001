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
    /// <summary>InGameManager.Awake() 에서 주입. null 체크 후 사용.</summary>
    public static GameplayConfig Current;

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

    [Header("전투 공통")]
    [Tooltip("방어율 최대 상한 (0~1). 스텟 재계산 후 이 값으로 클램프됩니다.")]
    [Range(0f, 1f)]
    public float DefenseMax = 0.95f;

    // ──────────────────────────────────────────────────────────
    // ■ 스텟 성장 배율
    // ──────────────────────────────────────────────────────────

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
        // 적 스텟 범위
        EnemyRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(100f,  400f),
            Attack      = new FloatRange(10f,   50f),
            Defense     = new FloatRange(0.05f, 0.15f),
            AttackRange = new FloatRange(1.2f,  2.5f),
            AttackSpeed = new FloatRange(0.5f,  1.5f),
            MoveSpeed   = new FloatRange(2.0f,  4.0f),
            CritChance  = 0.05f,
            CritDamage  = 1.50f,
        };
        EliteRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(400f,  1200f),
            Attack      = new FloatRange(50f,   150f),
            Defense     = new FloatRange(0.10f, 0.30f),
            AttackRange = new FloatRange(1.5f,  3.0f),
            AttackSpeed = new FloatRange(0.8f,  2.0f),
            MoveSpeed   = new FloatRange(2.5f,  4.5f),
            CritChance  = 0.05f,
            CritDamage  = 1.50f,
        };
        BossRange = new EnemyGradeStatRange
        {
            Hp          = new FloatRange(2000f, 8000f),
            Attack      = new FloatRange(150f,  400f),
            Defense     = new FloatRange(0.20f, 0.45f),
            AttackRange = new FloatRange(2.0f,  4.0f),
            AttackSpeed = new FloatRange(0.4f,  1.2f),
            MoveSpeed   = new FloatRange(1.5f,  3.0f),
            CritChance  = 0.05f,
            CritDamage  = 1.50f,
        };

        // 아군 직업별 스텟 범위
        KnightRange = new JobStatRange
        {
            Hp           = new FloatRange(700f,   1500f),
            Attack       = new FloatRange(60f,    150f),
            Defense      = new FloatRange(0.08f,  0.22f),
            AttackRange  = new FloatRange(0.8f,   1.2f),
            AttackSpeed  = new FloatRange(0.8f,   1.8f),
            MoveSpeed    = new FloatRange(2.5f,   3.0f),
            SoldierCount = new FloatRange(5f,     20f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.10f,
            CritDamage   = 1.50f,
        };
        ArcherRange = new JobStatRange
        {
            Hp           = new FloatRange(300f,   700f),
            Attack       = new FloatRange(50f,    130f),
            Defense      = new FloatRange(0.03f,  0.10f),
            AttackRange  = new FloatRange(5.0f,   9.0f),
            AttackSpeed  = new FloatRange(0.8f,   1.8f),
            MoveSpeed    = new FloatRange(2.0f,   2.5f),
            SoldierCount = new FloatRange(5f,     20f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.15f,
            CritDamage   = 1.80f,
        };
        MageRange = new JobStatRange
        {
            Hp           = new FloatRange(250f,   600f),
            Attack       = new FloatRange(120f,   350f),
            Defense      = new FloatRange(0.02f,  0.08f),
            AttackRange  = new FloatRange(4.0f,   7.0f),
            AttackSpeed  = new FloatRange(0.3f,   0.7f),
            MoveSpeed    = new FloatRange(1.5f,   2.0f),
            SoldierCount = new FloatRange(5f,     20f),
            CommandPower = new FloatRange(1f,     30f),
            CritChance   = 0.10f,
            CritDamage   = 2.00f,
        };
        ShieldBearerRange = new JobStatRange
        {
            Hp           = new FloatRange(1500f,  4000f),
            Attack       = new FloatRange(30f,    80f),
            Defense      = new FloatRange(0.25f,  0.50f),
            AttackRange  = new FloatRange(0.7f,   1.0f),
            AttackSpeed  = new FloatRange(0.5f,   1.2f),
            MoveSpeed    = new FloatRange(2.0f,   2.5f),
            SoldierCount = new FloatRange(5f,     20f),
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
