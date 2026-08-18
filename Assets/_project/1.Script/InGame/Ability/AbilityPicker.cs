using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  AbilityPicker.cs
//  스테이지 클리어 후 어빌리티 N택 추첨.
//
//  기본 동작:
//    - 선택지 수 3 (유물 R_AbilityChoicePlus 로 최대 +2)
//    - 직업 전용(Job_*) 어빌리티는 배치한 장수 1명당 +10% 확률 (달인 해금 보조)
//    - 등급 가중치: Normal 60% / Advanced 30% / Special 10%
//      유물 R_AbilityAdvanced 로 Advanced+Special 가중치 추가 증가
//    - 같은 AbilityId 는 1회 선택지에 중복 출현하지 않음
//    - Special 어빌리티를 이미 1개 이상 보유하면 Special 풀 제외
//
//  새로고침 횟수:
//    AbilitySelectPopup 이 RelicApplier.GetSystemInt(AbilityRefreshCount) 로
//    허용 횟수를 확인하고 재추첨을 요청한다. (이 클래스는 횟수 추적 불필요)
// ============================================================

public static class AbilityPicker
{
    const int BasePickCount = 3;

    static readonly float[] BaseGradeWeights =
    {
        0.68f,  // Normal
        0.30f,  // Advanced
        0.02f,  // Special
    };

    // 달인 해금 조건 — Normal·Advanced 직업 어빌리티 2종을 모두 보유해야 등장
    static readonly Dictionary<AbilityId, AbilityId[]> MasteryPrerequisites =
        new Dictionary<AbilityId, AbilityId[]>
        {
            { AbilityId.D01, new[] { AbilityId.A06, AbilityId.B05 } },  // 기사 달인
            { AbilityId.D02, new[] { AbilityId.A07, AbilityId.B06 } },  // 궁수 달인
            { AbilityId.D03, new[] { AbilityId.A08, AbilityId.B07 } },  // 마법사 달인
            { AbilityId.D04, new[] { AbilityId.A09, AbilityId.B08 } },  // 방패병 달인
        };

    /// <summary>
    /// 어빌리티 선택지를 추첨한다.
    /// relicInventory / relicDb 가 null 이면 유물 보너스 없이 기본값으로 동작.
    /// </summary>
    public static AbilityData[] Pick(
        AbilityDatabase db,
        RunAbilityData runData,
        RelicInventoryData relicInventory = null,
        RelicDatabase relicDb = null)
    {
        if (db == null) return System.Array.Empty<AbilityData>();

        // ── 선택지 수 결정 ──────────────────────────────────────
        int pickCount = BasePickCount
            + RelicApplier.GetSystemInt(RelicSystemEffect.AbilityChoiceCount, relicInventory, relicDb);
        pickCount = Mathf.Max(1, pickCount);

        // ── 등급 가중치 결정 ────────────────────────────────────
        float advancedBonus = RelicApplier.GetSystemValue(
            RelicSystemEffect.AbilityAdvancedChance, relicInventory, relicDb);

        float wNormal   = Mathf.Max(0f, BaseGradeWeights[0] - advancedBonus);
        float wAdvanced = BaseGradeWeights[1] + advancedBonus * 0.7f;  // 증가분의 70%를 Advanced 에
        float wSpecial  = BaseGradeWeights[2] + advancedBonus * 0.3f;  // 나머지 30%를 Special 에

        // ── 풀 준비 ─────────────────────────────────────────────
        var normal   = new List<AbilityData>(db.GetByGrade(AbilityGrade.Normal));
        var advanced = new List<AbilityData>(db.GetByGrade(AbilityGrade.Advanced));
        var special  = new List<AbilityData>(db.GetByGrade(AbilityGrade.Special));

        if (runData != null)
        {
            // 최대 레벨 도달한 어빌리티는 더 이상 등장하지 않음
            normal.RemoveAll(a   => runData.GetLevel(a.Id) >= a.MaxLevel);
            advanced.RemoveAll(a => runData.GetLevel(a.Id) >= a.MaxLevel);
            special.RemoveAll(a  => runData.HasAbility(a.Id));
        }

        // ── 달인 보장 슬롯 ──────────────────────────────────────
        // 해당 직업 전용 어빌리티(Normal+Advanced)가 모두 MaxLevel 도달 시 보장 등장
        AbilityData guaranteedMastery = null;
        if (runData != null)
        {
            foreach (var m in db.GetByGrade(AbilityGrade.Mastery))
            {
                if (runData.HasAbility(m.Id)) continue;
                if (!IsMasteryUnlocked(m.Id, runData, db)) continue;
                guaranteedMastery = m;
                break;
            }
        }

        // 배치된 부대 구성 — 등급 안에서 어느 어빌리티를 뽑을지 기울이는 데 쓴다
        var jobCounts = CountDeployedJobs();

        var result  = new List<AbilityData>(pickCount);
        var usedIds = new HashSet<AbilityId>();
        int attempts = 0;

        if (guaranteedMastery != null)
        {
            result.Add(guaranteedMastery);
            usedIds.Add(guaranteedMastery.Id);
        }

        while (result.Count < pickCount && attempts < 200)
        {
            attempts++;
            var data = PickOneWeighted(normal, advanced, special, wNormal, wAdvanced, wSpecial, jobCounts);
            if (data == null) break;
            if (!usedIds.Add(data.Id)) continue;
            result.Add(data);
        }

        return result.ToArray();
    }

    // ── 직업 친화 가중치 ──────────────────────────────────────
    //
    //  ⚠ 이 보너스는 '달인(Mastery) 해금을 돕는 장치' 다
    //    달인은 그 직업 전용 어빌리티(Normal+Advanced)를 전부 만렙까지
    //    모아야 열린다 — MasteryPrerequisites 참조.
    //    균등 추첨이면 기사만 3명 끌고 다녀도 기사 전용이 뜰 확률이 그대로라
    //    달인까지 가는 길이 순전히 운이었다. 배치한 직업 쪽으로 기울여 준다.
    //
    //  배치된 장수 1명당 +10%. 기사 3명이면 기사 어빌리티 가중치 ×1.3.
    //
    //  ⚠ 보정 대상은 Job_* 넷뿐이다
    //    Range_Melee·Range_Ranged 는 달인 해금 조건이 아니다 — 아무리 모아도
    //    달인이 열리지 않으므로 여기에 얹으면 정작 필요한 Job_* 이 밀린다.
    //    All·Unit_General·Unit_Soldier 도 같은 이유로 보정 없음(×1.0).

    const float AffinityPerGeneral = 0.10f;

    /// <summary>배치된 장수의 직업별 인원수. 배치 정보가 없으면 빈 표(=보정 없음).</summary>
    static Dictionary<UnitJob, int> CountDeployedJobs()
    {
        var counts = new Dictionary<UnitJob, int>();
        var deploy = UserDataManager.Instance?.Get<DeploymentData>();
        if (deploy == null) return counts;

        foreach (string unitName in deploy.GetDeployedUnits())
        {
            var job = UnitJobRoller.GetJob(unitName);
            counts.TryGetValue(job, out int c);
            counts[job] = c + 1;
        }
        return counts;
    }

    static int Count(Dictionary<UnitJob, int> counts, UnitJob job)
        => counts.TryGetValue(job, out int c) ? c : 0;

    static float AffinityWeight(AbilityData data, Dictionary<UnitJob, int> counts)
    {
        int matched = data.Target switch
        {
            AbilityTarget.Job_Knight       => Count(counts, UnitJob.Knight),
            AbilityTarget.Job_Archer       => Count(counts, UnitJob.Archer),
            AbilityTarget.Job_Mage         => Count(counts, UnitJob.Mage),
            AbilityTarget.Job_ShieldBearer => Count(counts, UnitJob.ShieldBearer),
            _                              => 0,   // 근거리·원거리 포함 — 달인 조건이 아니다
        };
        return 1f + matched * AffinityPerGeneral;
    }

    /// <summary>풀 안에서 직업 친화 가중치로 하나 뽑는다.</summary>
    static AbilityData PickFromPool(List<AbilityData> pool, Dictionary<UnitJob, int> counts)
    {
        if (pool.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < pool.Count; i++) total += AffinityWeight(pool[i], counts);

        float roll = Random.value * total;
        float acc  = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += AffinityWeight(pool[i], counts);
            if (roll < acc) return pool[i];
        }
        return pool[pool.Count - 1];   // 부동소수 오차로 끝을 넘겼을 때
    }

    static bool IsMasteryUnlocked(AbilityId id, RunAbilityData runData, AbilityDatabase db)
    {
        if (!MasteryPrerequisites.TryGetValue(id, out var prereqs)) return false;
        foreach (var prereq in prereqs)
        {
            var prereqData = db != null ? db.Get(prereq) : null;
            int maxLv      = prereqData != null ? prereqData.MaxLevel : 1;
            if (runData.GetLevel(prereq) < maxLv) return false;
        }
        return true;
    }

    static AbilityData PickOneWeighted(
        List<AbilityData> normal,
        List<AbilityData> advanced,
        List<AbilityData> special,
        float wNormal, float wAdvanced, float wSpecial,
        Dictionary<UnitJob, int> jobCounts)
    {
        float totalWeight = 0f;
        if (normal.Count   > 0) totalWeight += wNormal;
        if (advanced.Count > 0) totalWeight += wAdvanced;
        if (special.Count  > 0) totalWeight += wSpecial;

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        float acc  = 0f;

        if (normal.Count > 0)
        {
            acc += wNormal;
            if (roll < acc) return PickFromPool(normal, jobCounts);
        }
        if (advanced.Count > 0)
        {
            acc += wAdvanced;
            if (roll < acc) return PickFromPool(advanced, jobCounts);
        }
        if (special.Count > 0)
            return PickFromPool(special, jobCounts);

        return null;
    }
}
