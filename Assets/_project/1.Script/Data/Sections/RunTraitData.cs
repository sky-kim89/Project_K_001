using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RunTraitData.cs
//  런(스테이지 회귀) 내 보유 특성 목록을 저장하는 세이브 섹션.
//
//  ■ 동작 규칙
//    - 캐릭터 선택 시 직업 특성 1종 자동 배정
//    - 런 진행 중 추가 특성 획득 가능
//    - 회귀 시 SetDefaults() 로 전체 초기화
// ============================================================

[Serializable]
class RunTraitDataJson { public List<int> traits = new(); }

public class RunTraitData : ISaveSection
{
    readonly HashSet<TraitType> _traits = new();

    public SaveKey SaveKey => SaveKey.RunTrait;

    public bool                   HasTrait(TraitType t) => _traits.Contains(t);
    public void                   AddTrait(TraitType t) => _traits.Add(t);
    public IEnumerable<TraitType> AcquiredTraits        => _traits;
    public int                    Count                 => _traits.Count;

    public string Serialize()
    {
        var dto = new RunTraitDataJson();
        foreach (var t in _traits) dto.traits.Add((int)t);
        return JsonUtility.ToJson(dto);
    }

    public void Deserialize(string json)
    {
        _traits.Clear();
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<RunTraitDataJson>(json);
        if (dto?.traits == null) return;
        foreach (var i in dto.traits) _traits.Add((TraitType)i);
    }

    public void SetDefaults() => _traits.Clear();
}
