using Unity.Mathematics;

// ============================================================
//  EnemyAppearanceRoller.cs
//  적군 유닛 외형을 종족(EnemyRace) + unitName 시드로 생성.
//
//  ■ 규칙
//    Body / Head / Eyes / Ears = 종족 이름 (디폴트 색상 고정)
//    무기만 unitName 시드 기반 랜덤
//    그 외 모든 슬롯(아머/헬멧/헤어/마스크 등) = empty
//
//  ■ Lizard / FireLizard 종족
//    CharacterBuilder.BuildLayers() 에서 Lizard 계열 Head 감지 시
//    Hair / Helmet / Mask 를 자동으로 제거하므로 별도 처리 불필요.
// ============================================================

public static class EnemyAppearanceRoller
{
    // ── 적군 공통 무기 풀 (unitName 시드 기반) ────────────────

    static readonly string[] EnemyWeapons =
    {
        "Sword",       "IronSword",   "Axe",         "BattleAxe",
        "Mace",        "Hammer",      "Pitchfork",   "Scythe",
        "Fork",        "DeathScythe", "LargeScythe", "Sickle",
        "WoodenClub",  "SpikedClub",  "RoundMace",   "BattleHammer",
        "Greataxe",    "Greatsword",  "GiantBlade",  "GiantSword",
    };

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// 종족으로 신체를 결정하고 unitName 시드로 무기를 결정한다.
    /// 신체 색상은 에셋 기본값(디폴트) 유지.
    /// </summary>
    public static UnitAppearanceData Roll(EnemyRace race, string unitName)
    {
        uint seed = ComputeSeed(unitName);
        var  rng  = new Random(seed);

        string raceName = race.ToString();

        return new UnitAppearanceData
        {
            Body   = raceName,
            Head   = raceName,
            Ears   = raceName,
            Eyes   = raceName,
            Weapon = EnemyWeapons[rng.NextInt(0, EnemyWeapons.Length)],
            // 나머지 슬롯: 기본값 empty
        };
    }

    // ── 내부 ─────────────────────────────────────────────────

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
}
