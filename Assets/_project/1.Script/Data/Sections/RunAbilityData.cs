using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RunAbilityData.cs
//  런(스테이지 회귀) 내 보유 어빌리티 레벨 목록을 저장하는 세이브 섹션.
//
//  ■ 동작 규칙
//    - Normal/Advanced: 최대 MaxLevel(=3)번 중복 보유 가능, 레벨마다 효과 누적
//    - Special/Mastery: MaxLevel=1, 1번만 보유 가능
//    - 달인 해금 조건: 해당 직업 전용 어빌리티(예: 기사 → A06·B05)가
//                     모두 MaxLevel 도달 시 선택지에 100% 등장
//    - 런 종료(회귀) 시 SetDefaults() 로 초기화
// ============================================================

[Serializable]
class AbilityLevelEntry { public int id; public int lv; }

[Serializable]
class RunAbilityDataJson
{
    public List<AbilityLevelEntry> levels       = new();
    public List<int>               ids          = new();   // v1 마이그레이션용
    public int                     soldierDeaths = 0;
}

public class RunAbilityData : ISaveSection
{
    readonly Dictionary<AbilityId, int> _levels = new();
    int _totalSoldierDeaths;

    public SaveKey SaveKey            => SaveKey.RunAbility;
    public int     TotalSoldierDeaths => _totalSoldierDeaths;

    /// 해당 어빌리티의 현재 레벨 (0 = 미보유)
    public int  GetLevel(AbilityId id)   => _levels.GetValueOrDefault(id, 0);
    public bool HasAbility(AbilityId id) => _levels.ContainsKey(id);

    /// 레벨 1 증가. 최대 레벨 제한은 호출 측(AbilitySelectPopup)에서 관리.
    public void AddAbility(AbilityId id)
    {
        CodexData.Record(id);   // 도감 — 회귀해도 남는다
        _levels[id] = _levels.GetValueOrDefault(id, 0) + 1;
    }

    /// UI용 — 고유 ID 열거
    public IEnumerable<AbilityId> OwnedAbilityIds => _levels.Keys;

    /// AbilityApplier 호환 — ID를 레벨 횟수만큼 중복 반환 (Lv3 → [id, id, id])
    public IReadOnlyList<AbilityId> HeldAbilities
    {
        get
        {
            var list = new List<AbilityId>(_levels.Count * 3);
            foreach (var (id, lv) in _levels)
                for (int i = 0; i < lv; i++) list.Add(id);
            return list;
        }
    }

    public void Clear()                  => _levels.Clear();
    public void IncrementSoldierDeaths() => _totalSoldierDeaths++;

    public string Serialize()
    {
        var dto = new RunAbilityDataJson { soldierDeaths = _totalSoldierDeaths };
        foreach (var (id, lv) in _levels)
            dto.levels.Add(new AbilityLevelEntry { id = (int)id, lv = lv });
        return JsonUtility.ToJson(dto);
    }

    public void Deserialize(string json)
    {
        _levels.Clear();
        _totalSoldierDeaths = 0;
        if (string.IsNullOrEmpty(json)) return;

        var dto = JsonUtility.FromJson<RunAbilityDataJson>(json);
        if (dto == null) return;

        _totalSoldierDeaths = dto.soldierDeaths;

        if (dto.levels?.Count > 0)
        {
            foreach (var e in dto.levels)
                _levels[(AbilityId)e.id] = e.lv;
        }
        else if (dto.ids?.Count > 0)
        {
            // v1 포맷 마이그레이션: 중복 목록 → 레벨 카운트로 변환
            foreach (var i in dto.ids)
                _levels[(AbilityId)i] = _levels.GetValueOrDefault((AbilityId)i, 0) + 1;
        }
    }

    public void SetDefaults() { _levels.Clear(); _totalSoldierDeaths = 0; }
}
