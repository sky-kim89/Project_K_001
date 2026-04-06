using Unity.Mathematics;

// ============================================================
//  EnemyStatRoller.cs
//  적 유닛 스텟 결정적(Deterministic) 랜덤 생성기.
//
//  같은 UnitName → 항상 같은 스텟 (FNV-1a 시드 기반).
//  SpawnUnitType 별로 스텟 범위가 다르게 적용된다.
//
//  타입별 스텟 범위:
//    Enemy (일반)  : HP 100~400   ATK 10~50    DEF 5~15%
//    Elite  (엘리트): HP 400~1200  ATK 50~150   DEF 10~30%
//    Boss   (보스)  : HP 2000~8000 ATK 150~400  DEF 20~45%
//
//  사용:
//    UnitStat stat = EnemyStatRoller.Roll("Goblin_A", SpawnUnitType.Enemy);
// ============================================================

public static class EnemyStatRoller
{
    // ── 타입별 스텟 범위 ──────────────────────────────────────

    static readonly StatRanges EnemyRanges = new()
    {
        Hp           = (100f,  400f),
        Attack       = (10f,   50f),
        Defense      = (0.05f, 0.15f),
        AttackRange  = (1.2f,  2.5f),
        AttackSpeed  = (0.5f,  1.5f),
        MoveSpeed    = (2.0f,  4.0f),
    };

    static readonly StatRanges EliteRanges = new()
    {
        Hp           = (400f,  1200f),
        Attack       = (50f,   150f),
        Defense      = (0.10f, 0.30f),
        AttackRange  = (1.5f,  3.0f),
        AttackSpeed  = (0.8f,  2.0f),
        MoveSpeed    = (2.5f,  4.5f),
    };

    static readonly StatRanges BossRanges = new()
    {
        Hp           = (2000f, 8000f),
        Attack       = (150f,  400f),
        Defense      = (0.20f, 0.45f),
        AttackRange  = (2.0f,  4.0f),
        AttackSpeed  = (0.4f,  1.2f),
        MoveSpeed    = (1.5f,  3.0f),
    };

    // 고정 스텟 (전 타입 공통)
    const float FixedCritChance = 0.05f;  // 5%
    const float FixedCritDamage = 1.50f;  // ×1.5

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// unitName 을 시드로, unitType 에 맞는 범위의 스텟을 생성해 반환한다.
    /// 같은 이름+타입은 항상 같은 값을 반환한다.
    /// </summary>
    public static UnitStat Roll(string unitName, SpawnUnitType unitType)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Random(seed);

        StatRanges range = unitType switch
        {
            SpawnUnitType.Elite => EliteRanges,
            SpawnUnitType.Boss  => BossRanges,
            _                   => EnemyRanges,   // Enemy, Soldier, General → 기본 Enemy 범위
        };

        var stat = new UnitStat();
        stat.Set(StatType.MaxHp,       Lerp(range.Hp,          rng.NextFloat()));
        stat.Set(StatType.Attack,      Lerp(range.Attack,      rng.NextFloat()));
        stat.Set(StatType.Defense,     Lerp(range.Defense,     rng.NextFloat()));
        stat.Set(StatType.AttackRange, Lerp(range.AttackRange, rng.NextFloat()));
        stat.Set(StatType.AttackSpeed, Lerp(range.AttackSpeed, rng.NextFloat()));
        stat.Set(StatType.MoveSpeed,   Lerp(range.MoveSpeed,   rng.NextFloat()));
        stat.Set(StatType.CritChance,  FixedCritChance);
        stat.Set(StatType.CritDamage,  FixedCritDamage);

        return stat;
    }

    // ── 내부 ─────────────────────────────────────────────────

    /// <summary>FNV-1a 32bit 해시 — 실행·플랫폼마다 일치 보장.</summary>
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

    struct StatRanges
    {
        public (float min, float max) Hp;
        public (float min, float max) Attack;
        public (float min, float max) Defense;
        public (float min, float max) AttackRange;
        public (float min, float max) AttackSpeed;
        public (float min, float max) MoveSpeed;
    }
}
