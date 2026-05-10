using Unity.Mathematics;
using UnityEngine;

// ============================================================
//  EnemyStatRoller.cs
//  적 유닛 스텟 결정적(Deterministic) 랜덤 생성기.
//
//  같은 UnitName → 항상 같은 기본 스텟 (FNV-1a 시드 기반).
//  SpawnUnitType 별로 스텟 범위가 다르게 적용된다.
//
//  성장 요소 (level, statMultiplier):
//    level         — 레벨당 LevelMultPerLevel(기본 1%) 스텟 증가 (HP·ATK만 적용)
//    statMultiplier — 스테이지 진행도 기반 배율 (HP·ATK에 곱함, 지수 성장)
//
//  사용:
//    UnitStat stat = EnemyStatRoller.Roll("S5W1E", SpawnUnitType.Enemy, level: 4, statMultiplier: 1.2f);
// ============================================================

public static class EnemyStatRoller
{
    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// unitName 을 시드로, unitType 에 맞는 범위의 스텟을 생성해 반환한다.
    /// level 과 statMultiplier 가 HP·ATK에 추가로 곱해진다 (DEF·Range·Speed 제외).
    /// </summary>
    public static UnitStat Roll(string unitName, SpawnUnitType unitType,
                                int level = 1, float statMultiplier = 1f)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Unity.Mathematics.Random(seed);

        var cfg   = GameplayConfig.Current;
        EnemyGradeStatRange range = cfg != null
            ? cfg.GetEnemyRange(unitType)
            : FallbackRange(unitType);

        // 레벨 배율: 레벨당 1% (GameplayConfig.LevelMultPerLevel)
        float levelMultPerLevel = cfg != null ? cfg.LevelMultPerLevel : 0.01f;
        float levelMult = 1f + Mathf.Max(0, level - 1) * levelMultPerLevel;

        // HP·ATK에만 레벨 배율 + 스테이지 배율을 곱한다
        float hpAtkMult = levelMult * Mathf.Max(0.1f, statMultiplier);

        var stat = new UnitStat();
        stat.Set(StatType.MaxHp,       range.Hp.Lerp(rng.NextFloat())     * hpAtkMult);
        stat.Set(StatType.Attack,      range.Attack.Lerp(rng.NextFloat())  * hpAtkMult);
        stat.Set(StatType.Defense,     Mathf.Min(range.Defense.Lerp(rng.NextFloat()), 0.85f));
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
            Hp = new FloatRange(700f, 2000f), Attack = new FloatRange(70f, 210f),
            Defense = new FloatRange(0.12f, 0.32f), AttackRange = new FloatRange(1.5f, 3.0f),
            AttackSpeed = new FloatRange(0.8f, 2.0f), MoveSpeed = new FloatRange(2.5f, 4.5f),
            CritChance = 0.08f, CritDamage = 1.60f,
        },
        SpawnUnitType.Boss => new EnemyGradeStatRange
        {
            Hp = new FloatRange(5000f, 14000f), Attack = new FloatRange(220f, 550f),
            Defense = new FloatRange(0.25f, 0.50f), AttackRange = new FloatRange(2.5f, 4.5f),
            AttackSpeed = new FloatRange(0.40f, 0.90f), MoveSpeed = new FloatRange(2.0f, 3.5f),
            CritChance = 0.08f, CritDamage = 1.60f,
        },
        _ => new EnemyGradeStatRange
        {
            Hp = new FloatRange(200f, 600f), Attack = new FloatRange(18f, 65f),
            Defense = new FloatRange(0.05f, 0.15f), AttackRange = new FloatRange(1.2f, 2.5f),
            AttackSpeed = new FloatRange(0.6f, 1.6f), MoveSpeed = new FloatRange(2.0f, 4.0f),
            CritChance = 0.05f, CritDamage = 1.50f,
        },
    };
}
