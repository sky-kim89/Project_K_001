using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  AbilityPicker.cs
//  스테이지 클리어 후 어빌리티 N택 추첨.
//
//  기본 동작:
//    - 선택지 수 3 (유물 R_AbilityChoicePlus 로 최대 +2)
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
        0.60f,  // Normal
        0.30f,  // Advanced
        0.10f,  // Special
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
            var data = PickOneWeighted(normal, advanced, special, wNormal, wAdvanced, wSpecial);
            if (data == null) break;
            if (!usedIds.Add(data.Id)) continue;
            result.Add(data);
        }

        return result.ToArray();
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
        float wNormal, float wAdvanced, float wSpecial)
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
            if (roll < acc) return normal[Random.Range(0, normal.Count)];
        }
        if (advanced.Count > 0)
        {
            acc += wAdvanced;
            if (roll < acc) return advanced[Random.Range(0, advanced.Count)];
        }
        if (special.Count > 0)
            return special[Random.Range(0, special.Count)];

        return null;
    }
}
