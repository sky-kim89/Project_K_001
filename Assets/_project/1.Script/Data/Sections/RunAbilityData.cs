using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RunAbilityData.cs
//  런(스테이지 회귀) 내 보유 어빌리티 목록을 저장하는 세이브 섹션.
//
//  ■ 동작 규칙
//    - 일반/고급 어빌리티: 중복 보유 가능 (합연산 누적)
//    - 특수 어빌리티: AddAbility 호출 전 HasAbility() 로 중복 확인 필요
//    - 런 종료(회귀) 시 SetDefaults() 호출로 초기화
// ============================================================

[Serializable]
class RunAbilityDataJson
{
    public List<int> ids = new();
}

public class RunAbilityData : ISaveSection
{
    readonly List<AbilityId> _held = new();

    public SaveKey SaveKey => SaveKey.RunAbility;
    public IReadOnlyList<AbilityId> HeldAbilities => _held;

    public void AddAbility(AbilityId id) => _held.Add(id);
    public bool HasAbility(AbilityId id) => _held.Contains(id);
    public void Clear()                  => _held.Clear();

    public string Serialize()
    {
        var dto = new RunAbilityDataJson();
        foreach (var id in _held) dto.ids.Add((int)id);
        return JsonUtility.ToJson(dto);
    }

    public void Deserialize(string json)
    {
        _held.Clear();
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<RunAbilityDataJson>(json);
        if (dto?.ids == null) return;
        foreach (var i in dto.ids) _held.Add((AbilityId)i);
    }

    public void SetDefaults() => _held.Clear();
}
