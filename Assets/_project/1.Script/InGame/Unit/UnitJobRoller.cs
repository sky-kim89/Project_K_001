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
//    배율 계산 뒤 레벨업 고정 성장(체력 +10 / 공격력 +1 per 레벨)을 더한다.
//    Defense 는 최대 0.80 으로 클램프 (80% 데미지 감소 상한).
//
//  ■ 직업별 특징
//    Knight       — 병사 특화. 병사 수(3~7)·지휘력(10~45) 최고, 이동속도 최고
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

        // 쿨감은 출처끼리 더하지 않고 곱연산으로 겹친다 (10%+10% = 19%).
        // 출처가 하나면 액면가 그대로 나온다 — 장비 설명과 실제가 어긋나지 않는다.
        stat.SetCombineMode(StatType.SkillCooldownReduce, CombineMode.MultiplyResidual);

        // ── 직업 기반 랜덤 스텟 ───────────────────────────────
        // 희귀 스킬 주인은 모든 굴림이 상위 구간([RareQualityFloor, 1])으로 눌러 담긴다
        float floorT = IsRareOwner(unitName) ? RareQualityFloor : 0f;

        float hp           = ranges.Hp.Lerp(RollT(ref rng, floorT));
        float attack       = ranges.Attack.Lerp(RollT(ref rng, floorT));
        float defense      = ranges.Defense.Lerp(RollT(ref rng, floorT));
        float attackRange  = ranges.AttackRange.Lerp(RollT(ref rng, floorT));
        float attackSpeed  = ranges.AttackSpeed.Lerp(RollT(ref rng, floorT));
        float moveSpeed    = ranges.MoveSpeed.Lerp(RollT(ref rng, floorT));
        float soldierCount = math.round(ranges.SoldierCount.Lerp(RollT(ref rng, floorT)));
        float commandPower = math.round(ranges.CommandPower.Lerp(RollT(ref rng, floorT)));

        // ── 레벨·등급 배율 계산 ──────────────────────────────
        float levelCoef = cfg != null ? cfg.LevelMultPerLevel : 0.01f;
        float gradeCoef = cfg != null ? cfg.GradeMultPerTier  : 0.10f;
        float defMax    = cfg != null ? cfg.DefenseMax         : 0.95f;

        float levelMult = 1f + Mathf.Max(0, level - 1) * levelCoef;
        float gradeMult = 1f + (int)grade * gradeCoef;
        float totalMult = levelMult * gradeMult;

        // ── 레벨업 고정 성장 ─────────────────────────────────
        // 배율과 별개로 레벨 1당 그대로 더해진다 (Lv1 = 0, Lv2 = 1회분).
        // 배율 뒤에 더하므로 "레벨업 1회 = 체력 +10 / 공격력 +1" 이 등급과 무관하게 일정하다.
        int   levelUps  = Mathf.Max(0, level - 1);
        float flatHp    = (cfg != null ? cfg.LevelFlatHpPerLevel     : 10f) * levelUps;
        float flatAtk   = (cfg != null ? cfg.LevelFlatAttackPerLevel : 1f)  * levelUps;

        // ── 등급 고정 가산 ────────────────────────────────────
        //  배율만 쓰면 공격속도·이동속도·사거리처럼 배율이 안 붙는 스텟에
        //  등급이 전혀 반영되지 않는다 (영웅인데 굼뜬 장수가 나온다).
        //  → 등급 1단계마다 "해당 스텟 최댓값의 N%" 를 모든 굴림 스텟에 더한다.
        float flatRatio = (cfg != null ? cfg.GradeFlatMaxRatio : 0.05f) * (int)grade;

        // ── 배율 적용 ─────────────────────────────────────────
        stat.Set(StatType.MaxHp,        hp    * totalMult + flatHp  + ranges.Hp.Max      * flatRatio);
        stat.Set(StatType.Attack,       attack * totalMult + flatAtk + ranges.Attack.Max  * flatRatio);
        stat.Set(StatType.Defense,      defense * totalMult + ranges.Defense.Max * flatRatio); // 소프트캡은 UnitHitSystem에서 처리
        stat.Set(StatType.AttackRange,  attackRange + ranges.AttackRange.Max * flatRatio);   // 배율 미적용
        stat.Set(StatType.AttackSpeed,  attackSpeed + ranges.AttackSpeed.Max * flatRatio);
        stat.Set(StatType.MoveSpeed,    moveSpeed   + ranges.MoveSpeed.Max   * flatRatio);
        stat.Set(StatType.SoldierCount, math.round(soldierCount * totalMult + ranges.SoldierCount.Max * flatRatio));
        stat.Set(StatType.CommandPower, math.round(commandPower * totalMult + ranges.CommandPower.Max * flatRatio));

        // ── 고정 스텟 (레벨·등급 미적용) ─────────────────────
        stat.Set(StatType.CritChance, ranges.CritChance);
        stat.Set(StatType.CritDamage, ranges.CritDamage);

        return stat;
    }

    // ══════════════════════════════════════════════════════════
    //  스탯 품질 (0~1)
    //  Roll() 이 굴리는 t 값 8개의 평균이다.
    //  0 = 모든 스탯이 범위 최솟값, 1 = 전부 최댓값.
    //  같은 순서로 같은 난수를 다시 굴려야 하므로 Roll() 을 고칠 때 여기도 같이 고칠 것.
    // ══════════════════════════════════════════════════════════

    /// <summary>희귀 스킬 주인의 최소 품질. 이 값 아래로는 굴리지 않는다.</summary>
    public const float RareQualityFloor = 0.9f;

    /// <summary>Roll() 이 소비하는 랜덤 스탯 개수.</summary>
    const int StatRollCount = 8;

    /// <summary>unitName 의 스탯 품질 (0~1). 등급 옆에 표시한다.</summary>
    public static float GetQuality(string unitName)
    {
        var rng = new Unity.Mathematics.Random(ComputeSeed(unitName));
        rng.NextInt(0, 4);   // 직업 굴림 — Roll() 과 순서를 맞춘다

        float floorT = IsRareOwner(unitName) ? RareQualityFloor : 0f;

        float sum = 0f;
        for (int i = 0; i < StatRollCount; i++) sum += RollT(ref rng, floorT);
        return sum / StatRollCount;
    }

    // floorT ~ 1 구간으로 눌러 담은 굴림값
    static float RollT(ref Unity.Mathematics.Random rng, float floorT)
        => floorT + (1f - floorT) * rng.NextFloat();

    static bool IsRareOwner(string unitName)
        => RareSkillArbiter.IsRareOwner(unitName);

    /// <summary>unitName 시드에서 직업만 반환 — UI 표시·필터링용.</summary>
    public static UnitJob GetJob(string unitName)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Unity.Mathematics.Random(seed);
        return (UnitJob)rng.NextInt(0, 4);
    }

    /// <summary>
    /// unitName 시드에서 태생 등급을 결정적으로 반환.
    /// 같은 이름은 항상 같은 등급 — 직업 시드(FNV-1a)와 독립된 djb2 해시 사용.
    /// </summary>
    public static UnitGrade GetBirthGrade(string unitName)
    {
        // 희귀 스킬 주인은 태생부터 영웅 등급이다 — 추첨을 타지 않는다
        if (IsRareOwner(unitName)) return UnitGrade.Epic;

        uint seed = ComputeGradeSeed(unitName);
        var  rng  = new Unity.Mathematics.Random(seed);
        float r   = rng.NextFloat();

        var   cfg      = GameplayConfig.Current;
        float epic     = cfg != null ? cfg.GradeChanceEpic                                              : 0.03f;
        float unique   = cfg != null ? cfg.GradeChanceEpic + cfg.GradeChanceUnique                      : 0.10f;
        float rare     = cfg != null ? cfg.GradeChanceEpic + cfg.GradeChanceUnique + cfg.GradeChanceRare : 0.25f;
        float uncommon = cfg != null ? rare + cfg.GradeChanceUncommon                                   : 0.50f;

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

    /// <summary>djb2 해시 — FNV-1a(직업 시드)와 완전히 독립된 등급 시드 생성.</summary>
    static uint ComputeGradeSeed(string name)
    {
        uint hash = 5381u;
        foreach (char c in name)
            hash = hash * 33u ^ (byte)c;
        return hash == 0u ? 1u : hash;
    }

    /// <summary>GameplayConfig 미할당 시 하드코딩 폴백.</summary>
    static JobStatRange FallbackJobRange(UnitJob job) => job switch
    {
        UnitJob.Archer => new JobStatRange
        {
            Hp = new FloatRange(450f, 1050f), Attack = new FloatRange(50f, 130f),
            Defense = new FloatRange(0.03f, 0.10f), AttackRange = new FloatRange(5.5f, 9.9f),
            AttackSpeed = new FloatRange(0.8f, 1.8f), MoveSpeed = new FloatRange(2.0f, 2.5f),
            SoldierCount = new FloatRange(5f, 20f), CommandPower = new FloatRange(1f, 30f),
            CritChance = 0.15f, CritDamage = 1.80f,
        },
        UnitJob.Mage => new JobStatRange
        {
            Hp = new FloatRange(375f, 900f), Attack = new FloatRange(120f, 350f),
            Defense = new FloatRange(0.02f, 0.08f), AttackRange = new FloatRange(4.0f, 7.0f),
            AttackSpeed = new FloatRange(0.27f, 0.63f), MoveSpeed = new FloatRange(1.5f, 2.0f),
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
            Hp = new FloatRange(840f, 1800f), Attack = new FloatRange(60f, 150f),
            Defense = new FloatRange(0.08f, 0.22f), AttackRange = new FloatRange(0.8f, 1.2f),
            AttackSpeed = new FloatRange(0.8f, 1.8f), MoveSpeed = new FloatRange(2.5f, 3.0f),
            SoldierCount = new FloatRange(5f, 20f), CommandPower = new FloatRange(1f, 30f),
            CritChance = 0.10f, CritDamage = 1.50f,
        },
    };
}
