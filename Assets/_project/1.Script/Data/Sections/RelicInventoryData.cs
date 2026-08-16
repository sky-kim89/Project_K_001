using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RelicInventoryData.cs
//  영구 보유 유물 목록 세이브 섹션.
//
//  ■ 구조
//    Dictionary<RelicId, int> 로 유물 ID → 강화 레벨 저장.
//    ⚠ "획득" 이라는 개념은 없다. 모든 유물은 처음부터 0레벨로 존재하며,
//      0레벨 = 효과 없음, 1레벨부터 스텟이 붙는다. 전부 강화일 뿐이다.
//      따라서 딕셔너리에 없는 ID = 0레벨 (GetLevel 이 0 을 반환).
//    강화 비용: ReincarnationData.LevelUpCost(currentLevel) 참조.
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

    /// <summary>
    /// 해당 유물의 현재 강화 레벨. 한 번도 강화하지 않았으면 0.
    /// ⚠ "미보유" 라는 상태는 없다 — 모든 유물은 처음부터 0레벨로 존재한다.
    /// </summary>
    public int GetLevel(RelicId id) => _owned.TryGetValue(id, out int lv) ? lv : 0;

    /// <summary>1레벨 이상 강화한 유물 종류 수.</summary>
    public int EnhancedCount
    {
        get
        {
            int n = 0;
            foreach (var kvp in _owned) if (kvp.Value > 0) n++;
            return n;
        }
    }

    // ── 데이터 조작 ───────────────────────────────────────────

    /// <summary>
    /// 유물 강화 레벨을 1 올린다 (maxLevel 을 넘지 않음).
    /// 0레벨(딕셔너리에 없는 상태) 에서도 정상 동작한다.
    /// </summary>
    public bool LevelUp(RelicId id, int maxLevel)
    {
        int current = GetLevel(id);
        if (current >= maxLevel) return false;
        _owned[id] = current + 1;
        return true;
    }

    /// <summary>테스트·에디터용 — 레벨을 직접 설정한다.</summary>
    public void SetLevel(RelicId id, int level)
    {
        if (level <= 0) _owned.Remove(id);   // 0레벨 = 기록할 것이 없음
        else            _owned[id] = level;
    }

    /// <summary>
    /// 모든 유물을 0레벨로 되돌리고, 강화에 투자한 포인트 전액을 반환한다.
    /// 호출 후 반환값을 ReincarnationData.EarnPoints() 로 적립해야 한다.
    ///
    /// 지불한 것이 레벨업 비용뿐이므로 전액 환불이며, 손해도 이득도 없는
    /// 순수한 재분배다.
    /// </summary>
    public int ResetAll()
    {
        if (_owned.Count == 0) return 0;

        int refund = 0;
        foreach (var kvp in _owned)
            for (int lv = 0; lv < kvp.Value; lv++)
                refund += ReincarnationData.LevelUpCost(lv);

        _owned.Clear();   // 비어 있음 = 전부 0레벨 (GetLevel 이 0 을 돌려준다)
        return refund;
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
