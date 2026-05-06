// ============================================================
//  ActiveSkillRoller.cs
//  장군 이름 + 직업을 시드로 액티브 스킬 1종을 결정하는 정적 클래스.
//
//  알고리즘:
//    FNV-1a 해시로 unitName 을 uint 시드로 변환.
//    직업(UnitJob) 값을 시드에 XOR 하여 같은 이름이라도 직업에 따라 다른 결과 보장.
//    AllowedJobs 에 해당 직업이 포함된(또는 비어있는) 스킬 목록을 필터링한 뒤
//    LCG 난수로 1개 선택.
//
//  결정론적 보장:
//    동일 unitName + job 은 항상 동일한 스킬을 반환.
//
//  fallback:
//    ActiveSkillDatabase 가 null 이거나 해당 직업 스킬이 없으면 HeavyStrike 반환.
// ============================================================

public static class ActiveSkillRoller
{
    /// <summary>
    /// unitName 과 job 을 시드로 액티브 스킬 1종을 결정한다.
    /// db 가 null 이거나 해당 직업에 맞는 스킬이 없으면 HeavyStrike 를 반환.
    /// </summary>
    public static ActiveSkillId Roll(string unitName, UnitJob job, ActiveSkillDatabase db)
    {
        if (db == null || db.Entries == null || db.Entries.Count == 0)
            return ActiveSkillId.HeavyStrike;

        // 직업에 맞는 스킬 풀 구성
        // AllowedJobs 가 비어 있으면 모든 직업이 사용 가능
        var pool = new System.Collections.Generic.List<ActiveSkillId>(db.Entries.Count);
        foreach (var entry in db.Entries)
        {
            if (entry == null || entry.SkillId == ActiveSkillId.None) continue;

            if (entry.AllowedJobs == null || entry.AllowedJobs.Length == 0)
            {
                pool.Add(entry.SkillId);
                continue;
            }

            foreach (var allowed in entry.AllowedJobs)
            {
                if (allowed == job) { pool.Add(entry.SkillId); break; }
            }
        }

        if (pool.Count == 0)
            return ActiveSkillId.HeavyStrike;

        // unitName + job 으로 결정론적 선택
        uint seed = Fnv1aHash(unitName);
        seed ^= (uint)job * 2654435761u;   // job 값으로 시드 분산
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
