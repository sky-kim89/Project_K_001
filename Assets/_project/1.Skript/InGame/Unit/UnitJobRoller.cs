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
    // ── 직업별 스텟 범위 정의 ─────────────────────────────────

    static readonly JobRanges KnightRanges = new()
    {
        Hp           = (700f,   1500f),
        Attack       = (60f,    150f),
        Defense      = (0.08f,  0.22f),
        AttackRange  = (0.8f,   1.2f),  // 명확한 근접
        AttackSpeed  = (0.8f,   1.8f),
        MoveSpeed    = (2.5f,   3.0f),  // 이동속도 최고
        SoldierCount = (5f,     20f),
        CommandPower = (1f,     30f),
        CritChance   = 0.10f,
        CritDamage   = 1.50f,
    };

    static readonly JobRanges ArcherRanges = new()
    {
        Hp           = (300f,   700f),   // 낮은 체력
        Attack       = (50f,    130f),   // 중간 공격
        Defense      = (0.03f,  0.10f),
        AttackRange  = (5.0f,   9.0f),  // 사거리 최고 — 명확한 원거리
        AttackSpeed  = (0.8f,   1.8f),
        MoveSpeed    = (2.0f,   2.5f),  // 느려서 후방 유지
        SoldierCount = (5f,     20f),
        CommandPower = (1f,     30f),
        CritChance   = 0.15f,            // 크리티컬 확률 높음
        CritDamage   = 1.80f,
    };

    static readonly JobRanges MageRanges = new()
    {
        Hp           = (250f,   600f),   // 체력 최저
        Attack       = (120f,   350f),  // 공격력 최고
        Defense      = (0.02f,  0.08f),
        AttackRange  = (4.0f,   7.0f),  // 원거리 — 명확한 후방 포격
        AttackSpeed  = (0.3f,   0.7f),  // 연사속도 최저
        MoveSpeed    = (1.5f,   2.0f),  // 이동속도 최저 — 자연스럽게 후방 대기
        SoldierCount = (5f,     20f),
        CommandPower = (1f,     30f),
        CritChance   = 0.10f,
        CritDamage   = 2.00f,            // 크리티컬 데미지 최고
    };

    static readonly JobRanges ShieldBearerRanges = new()
    {
        Hp           = (1500f,  4000f), // 체력 최고
        Attack       = (30f,    80f),
        Defense      = (0.25f,  0.50f), // 방어율 최고
        AttackRange  = (0.7f,   1.0f),  // 명확한 근접
        AttackSpeed  = (0.5f,   1.2f),
        MoveSpeed    = (2.0f,   2.5f),  // 느린 탱커
        SoldierCount = (5f,     20f),
        CommandPower = (1f,     30f),
        CritChance   = 0.05f,
        CritDamage   = 1.50f,
    };

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// unitName 시드로 직업·스텟을 결정하고, 레벨·등급 보너스를 적용해 반환.
    /// </summary>
    public static UnitStat Roll(string unitName, int level = 1, UnitGrade grade = UnitGrade.Normal)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Unity.Mathematics.Random(seed);

        UnitJob   job    = (UnitJob)rng.NextInt(0, 4);
        JobRanges ranges = GetRanges(job);

        var stat = new UnitStat();

        // ── 직업 기반 랜덤 스텟 ───────────────────────────────
        float hp           = Lerp(ranges.Hp,           rng.NextFloat());
        float attack       = Lerp(ranges.Attack,       rng.NextFloat());
        float defense      = Lerp(ranges.Defense,      rng.NextFloat());
        float attackRange  = Lerp(ranges.AttackRange,  rng.NextFloat());
        float attackSpeed  = Lerp(ranges.AttackSpeed,  rng.NextFloat());
        float moveSpeed    = Lerp(ranges.MoveSpeed,    rng.NextFloat());
        float soldierCount = math.round(Lerp(ranges.SoldierCount, rng.NextFloat()));
        float commandPower = math.round(Lerp(ranges.CommandPower, rng.NextFloat()));

        // ── 레벨·등급 배율 계산 ──────────────────────────────
        float levelMult = 1f + Mathf.Max(0, level - 1) * 0.01f;
        float gradeMult = 1f + (int)grade * 0.10f;
        float totalMult = levelMult * gradeMult;

        // ── 배율 적용 ─────────────────────────────────────────
        stat.Set(StatType.MaxHp,        hp           * totalMult);
        stat.Set(StatType.Attack,       attack       * totalMult);
        stat.Set(StatType.Defense,      Mathf.Min(defense * totalMult, 0.80f)); // 최대 80% 상한
        stat.Set(StatType.AttackRange,  attackRange);                           // 배율 미적용
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
    ///   Normal 50% / Uncommon 25% / Rare 15% / Unique 7% / Epic 3%
    /// </summary>
    public static UnitGrade RollGrade()
    {
        float r = UnityEngine.Random.value;
        if (r < 0.03f) return UnitGrade.Epic;
        if (r < 0.10f) return UnitGrade.Unique;
        if (r < 0.25f) return UnitGrade.Rare;
        if (r < 0.50f) return UnitGrade.Uncommon;
        return UnitGrade.Normal;
    }

    // ── 내부 ─────────────────────────────────────────────────

    static JobRanges GetRanges(UnitJob job) => job switch
    {
        UnitJob.Archer       => ArcherRanges,
        UnitJob.Mage         => MageRanges,
        UnitJob.ShieldBearer => ShieldBearerRanges,
        _                    => KnightRanges,
    };

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

    static float Lerp((float min, float max) range, float t)
        => range.min + (range.max - range.min) * t;

    // ── 내부 데이터 구조 ──────────────────────────────────────

    struct JobRanges
    {
        public (float min, float max) Hp;
        public (float min, float max) Attack;
        public (float min, float max) Defense;
        public (float min, float max) AttackRange;
        public (float min, float max) AttackSpeed;
        public (float min, float max) MoveSpeed;
        public (float min, float max) SoldierCount;
        public (float min, float max) CommandPower;
        public float CritChance;
        public float CritDamage;
    }
}
