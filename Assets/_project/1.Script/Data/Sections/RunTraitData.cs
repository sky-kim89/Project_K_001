using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RunTraitData.cs
//  런(스테이지 회귀) 내 보유 특성 목록 + 특성별 누적 스택을 저장하는 세이브 섹션.
//
//  ■ 구조
//    _data : Dictionary<TraitType, int>
//      - 키 존재 여부 = 특성 보유 여부  (HasTrait)
//      - 값              = 누적 스택 수  (GetStacks)
//    스택의 의미는 특성마다 다름 (TraitData.StackTrigger 참조):
//      KnightSoldierRage → 병사 사망 누적 수
//      ShieldRageBuild   → 스테이지 클리어 누적 수
//      그 외 스택 없는 특성 → 0
//
//  ■ 동작 규칙
//    - 캐릭터 선택 시 직업 특성 1종 자동 배정 (AddTrait)
//    - 런 진행 중 추가 특성 획득 가능
//    - 회귀 시 SetDefaults() 로 전체 초기화
// ============================================================

[Serializable]
class TraitSaveEntry
{
    public int traitType;
    public int stackCount;
}

[Serializable]
class RunTraitDataJson { public List<TraitSaveEntry> entries = new(); }

public class RunTraitData : ISaveSection
{
    readonly Dictionary<TraitType, int> _data = new();

    public SaveKey SaveKey => SaveKey.RunTrait;

    // ── 보유 여부 ───────────────────────────────────────────────

    public bool                   HasTrait(TraitType t)  => _data.ContainsKey(t);
    public void                   AddTrait(TraitType t)  { if (!_data.ContainsKey(t)) _data[t] = 0; }
    public IEnumerable<TraitType> AcquiredTraits         => _data.Keys;
    public int                    Count                  => _data.Count;

    // ── 스택 ────────────────────────────────────────────────────

    public int GetStacks(TraitType t) => _data.TryGetValue(t, out var v) ? v : 0;

    public void SetStacks(TraitType t, int value)
    {
        if (_data.ContainsKey(t)) _data[t] = value;
    }

    // delta 만큼 스택 증가. maxStacks <= 0 이면 무제한.
    // 실제 증가된 양을 반환 (0이면 변화 없음).
    public int IncrementStack(TraitType t, int delta, int maxStacks)
    {
        if (!_data.TryGetValue(t, out var cur)) return 0;
        int cap    = maxStacks > 0 ? maxStacks : int.MaxValue;
        int after  = Mathf.Min(cur + delta, cap);
        int actual = after - cur;
        if (actual <= 0) return 0;
        _data[t] = after;
        return actual;
    }

    public void RemoveTrait(TraitType t) => _data.Remove(t);

    // 직업 시너지(TraitType >= 1000)를 전부 제거한다. JobSynergyEvaluator 가 재계산 전에 호출.
    public void RemoveSynergies()
    {
        var toRemove = new List<TraitType>();
        foreach (var t in _data.Keys)
            if ((int)t >= 1000) toRemove.Add(t);
        foreach (var t in toRemove) _data.Remove(t);
    }

    // ── 직렬화 ──────────────────────────────────────────────────

    public string Serialize()
    {
        var dto = new RunTraitDataJson();
        foreach (var kv in _data)
            dto.entries.Add(new TraitSaveEntry { traitType = (int)kv.Key, stackCount = kv.Value });
        return JsonUtility.ToJson(dto);
    }

    public void Deserialize(string json)
    {
        _data.Clear();
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<RunTraitDataJson>(json);
        if (dto?.entries == null) return;
        foreach (var e in dto.entries)
            _data[(TraitType)e.traitType] = e.stackCount;
    }

    public void SetDefaults() => _data.Clear();
}
