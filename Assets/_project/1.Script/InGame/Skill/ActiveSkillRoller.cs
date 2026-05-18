// ============================================================
//  ActiveSkillRoller.cs
//  장군 이름 + 직업 + 등급을 시드로 액티브 스킬 1종을 결정하는 정적 클래스.
//
//  알고리즘:
//    FNV-1a 해시로 unitName 을 uint 시드로 변환.
//    직업(UnitJob) 값을 시드에 XOR 하여 같은 이름이라도 직업에 따라 다른 결과 보장.
//    AllowedJobs 에 해당 직업이 포함된(또는 비어있는) 스킬 목록을 필터링한 뒤
//    가중치를 적용하여 LCG 난수로 1개 선택.
//
//  가중치 규칙:
//    공통 스킬(AllowedJobs 비어 있음) = weight 1
//    전용 스킬(AllowedJobs 지정)      = weight (1 + (int)grade)
//      → Normal=2, Uncommon=3, Rare=4, Unique=5, Epic=6
//    등급이 높을수록 전용 스킬이 훨씬 더 자주 등장.
//
//  결정론적 보장:
//    동일 unitName + job + grade 는 항상 동일한 스킬을 반환.
//
//  fallback:
//    ActiveSkillDatabase 가 null 이거나 해당 직업 스킬이 없으면 HeavyStrike 반환.
// ============================================================

public static class ActiveSkillRoller
{
    /// <summary>
    /// unitName, job, grade 를 시드로 액티브 스킬 1종을 결정한다.
    /// db 가 null 이거나 해당 직업에 맞는 스킬이 없으면 HeavyStrike 를 반환.
    /// </summary>
    public static ActiveSkillId Roll(string unitName, UnitJob job, ActiveSkillDatabase db,
        UnitGrade grade = UnitGrade.Normal)
    {
        if (db == null || db.Entries == null || db.Entries.Count == 0)
            return ActiveSkillId.HeavyStrike;

        // 전용 스킬 가중치: 등급이 높을수록 전용 스킬이 더 자주 등장
        int specificWeight = 1 + (int)grade;  // Normal=2, Uncommon=3, Rare=4, Unique=5, Epic=6

        // 직업에 맞는 스킬 풀 구성 (공통=1회, 전용=weight회 반복)
        var pool = new System.Collections.Generic.List<ActiveSkillId>(db.Entries.Count * specificWeight);
        foreach (var entry in db.Entries)
        {
            if (entry == null || entry.SkillId == ActiveSkillId.None) continue;

            bool isCommon = entry.AllowedJobs == null || entry.AllowedJobs.Length == 0;
            if (isCommon)
            {
                pool.Add(entry.SkillId);  // 공통: 가중치 1
                continue;
            }

            foreach (var allowed in entry.AllowedJobs)
            {
                if (allowed == job)
                {
                    for (int w = 0; w < specificWeight; w++)
                        pool.Add(entry.SkillId);  // 전용: 가중치 N
                    break;
                }
            }
        }

        if (pool.Count == 0)
            return ActiveSkillId.HeavyStrike;

        // unitName + job + grade 으로 결정론적 선택
        uint seed = Fnv1aHash(unitName);
        seed ^= (uint)job  * 2654435761u;
        seed ^= (uint)grade * 1597334677u;
        seed  = LcgNext(seed);

        int index = (int)(seed % (uint)pool.Count);
        return pool[index];
    }

    // ── 내부 유틸 ─────────────────────────────────────────────

    static uint Fnv1aHash(string text)
    {
        uint hash = 2166136261u;
        if (string.IsNullOrEmpty(text)) return hash;
        foreach (char c in text)
        {
            hash ^= (uint)c;
            hash *= 16777619u;
        }
        return hash;
    }

    static uint LcgNext(uint seed)
        => seed * 1664525u + 1013904223u;
}
