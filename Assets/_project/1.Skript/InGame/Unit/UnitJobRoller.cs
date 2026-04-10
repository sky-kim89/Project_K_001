using Unity.Mathematics;
using UnityEngine;

// ============================================================
//  UnitJobRoller.cs
//  장군(아군 유닛) 직업별 스텟 결정적 랜덤 생성기.
//
//  ■ 직업 배정
//    unitName → FNV-1a 시드 → rng.NextInt(0,4) → UnitJob
//    같은 이름은 항상 같은 직업.
//
//  ■ 스텟 배율
//    레벨 보너스: × (1 + (level - 1) × 0.01)   → Lv1=×1.0, Lv100=×1.99
//    등급 보너스: × (1 + (int)grade  × 0.10)   → Normal=×1.0, Epic=×1.4
//    두 배율 곱 적용. 고정 스텟(CritChance, CritDamage)은 배율 미적용.
//    Defense 는 최대 0.80 으로 클램프 (80% 데미지 감소 상한).
//
//  ■ 직업별 특징
//    Knight       — 균형 스텟, 이동속도 최고 (4.5~7.0)
//    Archer       — 사거리 최고 (3.5~6.0), 중간 공격, 낮은 체력
//    Mage         — 공격력 최고 (120~350), 낮은 체력·연사속도 (0.3~0.7), 크리뎀 ×2.0
//    ShieldBearer — 체력 최고 (1500~4000), 방어율 최고 (25~50%)
//
//  사용:
//    UnitStat stat = UnitJobRoller.Roll("MyGeneral", level: 5, grade: UnitGrade.Rare);
//    UnitJob  job  = UnitJobRoller.GetJob("MyGeneral");
// ============================================================

public static class UnitJobRoller
{
    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// unitName 시드로 직업·스텟을 결정하고, 레벨·등급 보너스를 적용해 반환.
    /// 스텟 범위·배율 계수는 GameplayConfig.Current 에서 읽는다.
    /// </summary>
    public static UnitStat Roll(string unitName, int level = 1, UnitGrade grade = UnitGrade.Normal)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Unity.Mathematics.Random(seed);

        UnitJob      job    = (UnitJob)rng.NextInt(0, 4);
        var          cfg    = GameplayConfig.Current;
        JobStatRange ranges = cfg != null ? cfg.GetJobRange(job) : FallbackJobRange(job);

        var stat = new UnitStat();

        // ── 직업 기반 랜덤 스텟 ───────────────────────────────
        float hp           = ranges.Hp.Lerp(rng.NextFloat());
        float attack       = ranges.Attack.Lerp(rng.NextFloat());
        float defense      = ranges.Defense.Lerp(rng.NextFloat());
        float attackRange  = ranges.AttackRange.Lerp(rng.NextFloat());
        float attackSpeed  = ranges.AttackSpeed.Lerp(rng.NextFloat());
        float moveSpeed    = ranges.MoveSpeed.Lerp(rng.NextFloat());
        float soldierCount = math.round(ranges.SoldierCount.Lerp(rng.NextFloat()));
        float commandPower = math.round(ranges.CommandPower.Lerp(rng.NextFloat()));

        // ── 레벨·등급 배율 계산 ──────────────────────────────
        float levelCoef = cfg != null ? cfg.LevelMultPerLevel : 0.01f;
        float gradeCoef = cfg != null ? cfg.GradeMultPerTier  : 0.10f;
        float defMax    = cfg != null ? cfg.DefenseMax         : 0.95f;

        float levelMult = 1f + Mathf.Max(0, level - 1) * levelCoef;
        float gradeMult = 1f + (int)grade * gradeCoef;
        float totalMult = levelMult * gradeMult;

        // ── 배율 적용 ─────────────────────────────────────────
        stat.Set(StatType.MaxHp,        hp           * totalMult);
        stat.Set(StatType.Attack,       attack       * totalMult);
        stat.Set(StatType.Defense,      Mathf.Min(defense * totalMult, defMax));
        stat.Set(StatType.AttackRange,  attackRange);   // 배율 미적용
        stat.Set(StatType.AttackSpeed,  attackSpeed);
        stat.Set(StatType.MoveSpeed,    moveSpeed);
        stat.Set(StatType.SoldierCount, math.round(soldierCount * totalMult));
        stat.Set(StatType.CommandPower, math.round(commandPower * totalMult));

        // ── 고정 스텟 (레벨·등급 미적용) ─────────────────────
        stat.Set(StatType.CritChance, ranges.CritChance);
        stat.Set(StatType.CritDamage, ranges.CritDamage);

        return stat;
    }

    /// <summary>unitName 시드에서 직업만 반환 — UI 표시·필터링용.</summary>
    public static UnitJob GetJob(string unitName)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Unity.Mathematics.Random(seed);
        return (UnitJob)rng.NextInt(0, 4);
    }

    /// <summary>
    /// 등급을 가중치 랜덤으로 결정해 반환.
    /// 확률은 GameplayConfig.Current 에서 읽는다.
    /// </summary>
    public static UnitGrade RollGrade()
    {
        var   cfg = GameplayConfig.Current;
        float r   = UnityEngine.Random.value;

        float epic     = cfg != null ? cfg.GradeChanceEpic                                             : 0.03f;
        float unique   = cfg != null ? cfg.GradeChanceEpic + cfg.GradeChanceUnique                     : 0.10f;
        float rare     = cfg != null ? cfg.GradeChanceEpic + cfg.GradeChanceUnique + cfg.GradeChanceRare : 0.25f;
        float uncommon = cfg != null ? rare + cfg.GradeChanceUncommon                                  : 0.50f;

        if (r < epic)     return UnitGrade.Epic;
        if (r < unique)   return UnitGrade.Unique;
        if (r < rare)     return UnitGrade.Rare;
        if (r < uncommon) return UnitGrade.Uncommon;
        return UnitGrade.Normal;
    }

    // ── 내부 ─────────────────────────────────────────────────

    /// <summary>FNV-1a 32bit 해시 — 플랫폼·실행마다 일치 보장.</summary>
    static uint ComputeSeed(string name)
    {
        uint hash = 2166136261u;
        foreach (char c in name)
        {
            hash ^= (byte)c;
            hash *= 16777619u;
        }
        return hash == 0u ? 1u : hash;
    }

    /// <summary>GameplayConfig 미할당 시 하드코딩 폴백.</summary>
    static JobStatRange FallbackJobRange(UnitJob job) => job switch
    {
        UnitJob.Archer => new JobStatRange
        {
            Hp = new FloatRange(300f, 700f), Attack = new FloatRange(50f, 130f),
            Defense = new FloatRange(0.03f, 0.10f), AttackRange = new FloatRange(5.0f, 9.0f),
            AttackSpeed = new FloatRange(0.8f, 1.8f), MoveSpeed = new FloatRange(2.0f, 2.5f),
            SoldierCount = new FloatRange(5f, 20f), CommandPower = new FloatRange(1f, 30f),
            CritChance = 0.15f, CritDamage = 1.80f,
        },
        UnitJob.Mage => new JobStatRange
        {
            Hp = new FloatRange(250f, 600f), Attack = new FloatRange(120f, 350f),
            Defense = new FloatRange(0.02f, 0.08f), AttackRange = new FloatRange(4.0f, 7.0f),
            AttackSpeed = new FloatRange(0.3f, 0.7f), MoveSpeed = new FloatRange(1.5f, 2.0f),
            SoldierCount = new FloatRange(5f, 20f), CommandPower = new FloatRange(1f, 30f),
            CritChance = 0.10f, CritDamage = 2.00f,
        },
        UnitJob.ShieldBearer => new JobStatRange
        {
            Hp = new FloatRange(1500f, 4000f), Attack = new FloatRange(30f, 80f),
            Defense = new FloatRange(0.25f, 0.50f), AttackRange = new FloatRange(0.7f, 1.0f),
            AttackSpeed = new FloatRange(0.5f, 1.2f), MoveSpeed = new FloatRange(2.0f, 2.5f),
            SoldierCount = new FloatRange(5f, 20f), CommandPower = new FloatRange(1f, 30f),
            CritChance = 0.05f, CritDamage = 1.50f,
        },
        _ => new JobStatRange
        {
            Hp = new FloatRange(700f, 1500f), Attack = new FloatRange(60f, 150f),
            Defense = new FloatRange(0.08f, 0.22f), AttackRange = new FloatRange(0.8f, 1.2f),
            AttackSpeed = new FloatRange(0.8f, 1.8f), MoveSpeed = new FloatRange(2.5f, 3.0f),
            SoldierCount = new FloatRange(5f, 20f), CommandPower = new FloatRange(1f, 30f),
            CritChance = 0.10f, CritDamage = 1.50f,
        },
    };
}
