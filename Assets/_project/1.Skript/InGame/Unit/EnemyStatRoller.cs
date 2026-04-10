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
    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// unitName 을 시드로, unitType 에 맞는 범위의 스텟을 생성해 반환한다.
    /// 같은 이름+타입은 항상 같은 값을 반환한다.
    /// 스텟 범위는 GameplayConfig.Current 에서 읽는다.
    /// </summary>
    public static UnitStat Roll(string unitName, SpawnUnitType unitType)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Random(seed);

        var cfg   = GameplayConfig.Current;
        EnemyGradeStatRange range = cfg != null
            ? cfg.GetEnemyRange(unitType)
            : FallbackRange(unitType);

        var stat = new UnitStat();
        stat.Set(StatType.MaxHp,       range.Hp.Lerp(rng.NextFloat()));
        stat.Set(StatType.Attack,      range.Attack.Lerp(rng.NextFloat()));
        stat.Set(StatType.Defense,     range.Defense.Lerp(rng.NextFloat()));
        stat.Set(StatType.AttackRange, range.AttackRange.Lerp(rng.NextFloat()));
        stat.Set(StatType.AttackSpeed, range.AttackSpeed.Lerp(rng.NextFloat()));
        stat.Set(StatType.MoveSpeed,   range.MoveSpeed.Lerp(rng.NextFloat()));
        stat.Set(StatType.CritChance,  range.CritChance);
        stat.Set(StatType.CritDamage,  range.CritDamage);

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

    /// <summary>GameplayConfig 미할당 시 하드코딩 폴백 (에디터 테스트 등 예외 상황용).</summary>
    static EnemyGradeStatRange FallbackRange(SpawnUnitType type) => type switch
    {
        SpawnUnitType.Elite => new EnemyGradeStatRange
        {
            Hp = new FloatRange(400f, 1200f), Attack = new FloatRange(50f, 150f),
            Defense = new FloatRange(0.10f, 0.30f), AttackRange = new FloatRange(1.5f, 3.0f),
            AttackSpeed = new FloatRange(0.8f, 2.0f), MoveSpeed = new FloatRange(2.5f, 4.5f),
            CritChance = 0.05f, CritDamage = 1.50f,
        },
        SpawnUnitType.Boss => new EnemyGradeStatRange
        {
            Hp = new FloatRange(2000f, 8000f), Attack = new FloatRange(150f, 400f),
            Defense = new FloatRange(0.20f, 0.45f), AttackRange = new FloatRange(2.0f, 4.0f),
            AttackSpeed = new FloatRange(0.4f, 1.2f), MoveSpeed = new FloatRange(1.5f, 3.0f),
            CritChance = 0.05f, CritDamage = 1.50f,
        },
        _ => new EnemyGradeStatRange
        {
            Hp = new FloatRange(100f, 400f), Attack = new FloatRange(10f, 50f),
            Defense = new FloatRange(0.05f, 0.15f), AttackRange = new FloatRange(1.2f, 2.5f),
            AttackSpeed = new FloatRange(0.5f, 1.5f), MoveSpeed = new FloatRange(2.0f, 4.0f),
            CritChance = 0.05f, CritDamage = 1.50f,
        },
    };
}
