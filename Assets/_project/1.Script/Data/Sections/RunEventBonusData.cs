using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RunEventBonusData.cs
//  이벤트로 발생한 런 스코프 보너스 저장 섹션.
//
//  ■ 보관 데이터
//    - ExtraSoldiers : 병사 추가/감소 누계
//    - StatBonuses   : StatType별 직접 보너스 (% 가산)
//                      예) StatType.Attack → 0.08f = 공격력 +8%
//
//  ■ 특성(TraitType)을 통한 스탯 효과는 RunTraitData 에서 관리.
//    이 섹션은 특성 없이 숫자만으로 직접 적용하는 보너스 전용.
//
//  ■ 환생(SetDefaults) 시 전부 초기화된다.
// ============================================================

[Serializable]
class RunEventBonusRaw
{
    public int          ExtraSoldiers = 0;
    public List<int>    StatKeys      = new();   // (int)StatType
    public List<float>  StatValues    = new();   // 대응하는 % 보너스 합산값
    public List<string> SeenEventIds  = new();   // 이번 런에 이미 등장한 이벤트 ID
}

public class RunEventBonusData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.RunEventBonus;

    RunEventBonusRaw _raw = new();

    // ── 병사 수 ───────────────────────────────────────────────

    public int ExtraSoldiers => _raw.ExtraSoldiers;

    public void AddSoldiers(int count)
        => _raw.ExtraSoldiers += count;

    /// <summary>
    /// 병사 감소. 누계는 음수로 내려간다.
    ///
    /// ⚠ 여기서 0 으로 막지 말 것
    ///   이벤트의 "병사 -1 (영구)" 는 대가로 붙는 항목인데, 0 에서 잘라 버리면
    ///   보너스를 받은 적 없는 플레이어에게는 아무 일도 일어나지 않아 공짜가 된다.
    ///   실제 병사 수 하한은 스폰 시점이 잡는다 (GeneralSkillSystem: max(0, SoldierCount)).
    /// </summary>
    public void RemoveSoldiers(int count)
        => _raw.ExtraSoldiers -= Mathf.Max(0, count);

    // ── 스탯 보너스 ───────────────────────────────────────────

    /// <summary>StatType 에 percent 만큼 보너스를 추가한다 (0.1 = +10%).</summary>
    public void AddStatBonus(StatType stat, float percent)
    {
        int key = (int)stat;
        int idx = _raw.StatKeys.IndexOf(key);
        if (idx < 0)
        {
            _raw.StatKeys.Add(key);
            _raw.StatValues.Add(percent);
        }
        else
        {
            _raw.StatValues[idx] += percent;
        }
    }

    /// <summary>StatType 에 누산된 % 보너스 합계를 반환한다 (없으면 0).</summary>
    public float GetStatBonus(StatType stat)
    {
        int idx = _raw.StatKeys.IndexOf((int)stat);
        return idx < 0 ? 0f : _raw.StatValues[idx];
    }

    /// <summary>이벤트 보너스가 하나라도 있으면 true.</summary>
    public bool HasAnyBonus => _raw.ExtraSoldiers != 0 || _raw.StatKeys.Count > 0;

    // ── 등장 이력 (런 내 중복 방지) ───────────────────────────

    /// <summary>이번 런에 이미 등장한 이벤트 ID 목록.</summary>
    public IEnumerable<string> SeenEventIds => _raw.SeenEventIds;

    /// <summary>이벤트가 열릴 때 호출 — 같은 런에서 다시 뽑히지 않게 기록한다.</summary>
    public void MarkEventSeen(string id)
    {
        if (string.IsNullOrEmpty(id) || _raw.SeenEventIds.Contains(id)) return;
        _raw.SeenEventIds.Add(id);
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()              => JsonUtility.ToJson(_raw);
    public void   Deserialize(string json) => _raw = JsonUtility.FromJson<RunEventBonusRaw>(json) ?? new RunEventBonusRaw();
    public void   SetDefaults()            => _raw = new RunEventBonusRaw();
}
