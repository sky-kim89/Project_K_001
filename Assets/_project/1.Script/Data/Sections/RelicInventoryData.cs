using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RelicInventoryData.cs
//  영구 보유 유물 목록 세이브 섹션.
//
//  ■ 구조
//    Dictionary<RelicId, int> 로 유물 ID → 보유 레벨 저장.
//    중복 획득 시 LevelUp() → 레벨 증가 (최대 MaxLevel 까지).
//    JsonUtility 직렬화를 위해 두 개의 병렬 리스트로 변환.
//
//  ■ 런 초기화 없음 — 영구 저장 (RunAbilityData 와 다름)
// ============================================================

[Serializable]
class RelicInventoryJson
{
    public List<int> ids    = new();
    public List<int> levels = new();
}

public class RelicInventoryData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.RelicInventory;

    readonly Dictionary<RelicId, int> _owned = new();

    // ── 런타임 접근 ──────────────────────────────────────────

    /// <summary>보유 중인 유물 목록 (ID → 레벨). 읽기 전용.</summary>
    public IEnumerable<(RelicId id, int level)> OwnedRelics
    {
        get
        {
            foreach (var kvp in _owned)
                yield return (kvp.Key, kvp.Value);
        }
    }

    /// <summary>해당 유물 보유 여부.</summary>
    public bool HasRelic(RelicId id) => _owned.ContainsKey(id) && _owned[id] > 0;

    /// <summary>해당 유물의 현재 레벨 (미보유 시 0).</summary>
    public int GetLevel(RelicId id) => _owned.TryGetValue(id, out int lv) ? lv : 0;

    /// <summary>보유 유물 종류 수.</summary>
    public int RelicCount => _owned.Count;

    // ── 데이터 조작 ───────────────────────────────────────────

    /// <summary>
    /// 유물을 획득한다. 처음 획득 시 Lv1 로 추가.
    /// 이미 보유 중이면 레벨을 1 올린다 (maxLevel 을 넘지 않음).
    /// </summary>
    public bool AddOrLevelUp(RelicId id, int maxLevel = 5)
    {
        if (_owned.TryGetValue(id, out int current))
        {
            if (current >= maxLevel) return false;
            _owned[id] = current + 1;
        }
        else
        {
            _owned[id] = 1;
        }
        return true;
    }

    /// <summary>테스트·에디터용 — 레벨을 직접 설정한다.</summary>
    public void SetLevel(RelicId id, int level)
    {
        if (level <= 0) _owned.Remove(id);
        else            _owned[id] = level;
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()
    {
        var dto = new RelicInventoryJson();
        foreach (var kvp in _owned)
        {
            dto.ids.Add((int)kvp.Key);
            dto.levels.Add(kvp.Value);
        }
        return JsonUtility.ToJson(dto);
    }

    public void Deserialize(string json)
    {
        _owned.Clear();
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<RelicInventoryJson>(json);
        if (dto?.ids == null) return;

        int count = Mathf.Min(dto.ids.Count, dto.levels.Count);
        for (int i = 0; i < count; i++)
            _owned[(RelicId)dto.ids[i]] = dto.levels[i];
    }

    public void SetDefaults() => _owned.Clear();
}
