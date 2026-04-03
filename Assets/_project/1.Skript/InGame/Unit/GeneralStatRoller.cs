using Unity.Mathematics;

// ============================================================
//  GeneralStatRoller.cs
//  장군 스텟 결정적(Deterministic) 랜덤 생성기.
//
//  같은 UnitName → 항상 같은 스텟 (시드 기반).
//  시드 알고리즘: FNV-1a 32bit — 플랫폼/실행마다 일치 보장.
//
//  랜덤 스텟: 공, 체, 방, 공격속도, 이동속도, 병사수, 지휘력
//  고정  스텟: 크리티컬 확률(10%), 크리티컬 데미지(×1.5)
//
//  사용:
//    UnitStat stat = GeneralStatRoller.Roll("Knight_A");
// ============================================================

public static class GeneralStatRoller
{
    // ── 랜덤 스텟 범위 (min, max) ────────────────────────────

    static readonly (float min, float max) RangeHp           = (500f,  2000f);
    static readonly (float min, float max) RangeAttack       = (50f,   200f);
    static readonly (float min, float max) RangeDefense      = (0.05f, 0.30f);  // 5~30%
    static readonly (float min, float max) RangeAttackSpeed  = (0.5f,  2.0f);   // 초당 공격 횟수
    static readonly (float min, float max) RangeMoveSpeed    = (2.0f,  5.0f);
    static readonly (float min, float max) RangeSoldierCount = (5f,    20f);
    static readonly (float min, float max) RangeCommandPower = (1f,    30f);    // 1포인트 = 병사 스텟 1%

    // ── 고정 스텟 ─────────────────────────────────────────────

    const float FixedCritChance = 0.10f;   // 10%
    const float FixedCritDamage = 1.50f;   // 기본 배율

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// unitName 을 시드로 장군 스텟을 생성해 반환한다.
    /// 같은 이름은 항상 같은 값을 반환한다.
    /// </summary>
    public static UnitStat Roll(string unitName)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Random(seed);

        var stat = new UnitStat();

        stat.Set(StatType.MaxHp,        Lerp(RangeHp,           rng.NextFloat()));
        stat.Set(StatType.Attack,       Lerp(RangeAttack,       rng.NextFloat()));
        stat.Set(StatType.Defense,      Lerp(RangeDefense,      rng.NextFloat()));
        stat.Set(StatType.AttackSpeed,  Lerp(RangeAttackSpeed,  rng.NextFloat()));
        stat.Set(StatType.MoveSpeed,    Lerp(RangeMoveSpeed,    rng.NextFloat()));
        stat.Set(StatType.SoldierCount, math.round(Lerp(RangeSoldierCount, rng.NextFloat())));
        stat.Set(StatType.CommandPower, math.round(Lerp(RangeCommandPower, rng.NextFloat())));

        stat.Set(StatType.CritChance, FixedCritChance);
        stat.Set(StatType.CritDamage, FixedCritDamage);

        return stat;
    }

    // ── 내부 ─────────────────────────────────────────────────

    /// <summary>
    /// FNV-1a 32bit 해시 — string.GetHashCode() 와 달리 실행마다 일치 보장.
    /// Unity.Mathematics.Random 의 seed 는 0 이 허용되지 않으므로 0이면 1로 대체.
    /// </summary>
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
}
