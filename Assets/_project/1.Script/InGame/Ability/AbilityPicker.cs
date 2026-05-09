using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  AbilityPicker.cs
//  스테이지 클리어 후 어빌리티 3택 추첨.
//
//  등급 가중치: Normal 60% / Advanced 30% / Special 10%
//  - 같은 AbilityId 는 1회 선택지에 중복 출현하지 않음
//  - Special 어빌리티를 이미 1개 이상 보유하면 Special 풀 제외
// ============================================================

public static class AbilityPicker
{
    const int PickCount = 3;

    static readonly float[] GradeWeights =
    {
        0.60f,  // Normal
        0.30f,  // Advanced
        0.10f,  // Special
    };

    public static AbilityData[] Pick(AbilityDatabase db, RunAbilityData runData)
    {
        if (db == null) return System.Array.Empty<AbilityData>();

        var normal   = new List<AbilityData>(db.GetByGrade(AbilityGrade.Normal));
        var advanced = new List<AbilityData>(db.GetByGrade(AbilityGrade.Advanced));
        var special  = new List<AbilityData>(db.GetByGrade(AbilityGrade.Special));

        // Special 이미 보유 중이면 전체 제외 (런당 1개 제한)
        if (runData != null)
            special.RemoveAll(a => runData.HasAbility(a.Id));

        var result  = new List<AbilityData>(PickCount);
        var usedIds = new HashSet<AbilityId>();
        int attempts = 0;

        while (result.Count < PickCount && attempts < 200)
        {
            attempts++;
            var data = PickOneWeighted(normal, advanced, special);
            if (data == null) break;
            if (!usedIds.Add(data.Id)) continue;
            result.Add(data);
        }

        return result.ToArray();
    }

    static AbilityData PickOneWeighted(
        List<AbilityData> normal,
        List<AbilityData> advanced,
        List<AbilityData> special)
    {
        float totalWeight = 0f;
        if (normal.Count   > 0) totalWeight += GradeWeights[0];
        if (advanced.Count > 0) totalWeight += GradeWeights[1];
        if (special.Count  > 0) totalWeight += GradeWeights[2];

        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        float acc  = 0f;

        if (normal.Count > 0)
        {
            acc += GradeWeights[0];
            if (roll < acc) return normal[Random.Range(0, normal.Count)];
        }
        if (advanced.Count > 0)
        {
            acc += GradeWeights[1];
            if (roll < acc) return advanced[Random.Range(0, advanced.Count)];
        }
        if (special.Count > 0)
            return special[Random.Range(0, special.Count)];

        return null;
    }
}
