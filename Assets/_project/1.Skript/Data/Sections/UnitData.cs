using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  UnitData.cs
//  보유 유닛 목록 저장 섹션.
//
//  보유 데이터:
//  - 보유 유닛 목록 (유닛ID, 레벨, 경험치, 장착 스킬)
//
//  새 유닛 필드 추가 시: UnitEntry 내부에 필드만 추가하면 됨.
// ============================================================

public class UnitData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.UnitData;

    // ── 런타임 접근용 프로퍼티 ───────────────────────────────

    public IReadOnlyList<UnitEntry> Units => _raw.Units;

    // ── 내부 직렬화 데이터 ───────────────────────────────────

    UnitRawData _raw = new();

    // ── 데이터 갱신 메서드 ───────────────────────────────────

    public void AddUnit(UnitEntry entry)
    {
        _raw.Units.Add(entry);
    }

    public void RemoveUnit(string unitId)
    {
        _raw.Units.RemoveAll(u => u.UnitName == unitId);
    }

    public UnitEntry GetUnit(string unitId)
    {
        return _raw.Units.Find(u => u.UnitName == unitId);
    }

    public bool HasUnit(string unitId)
    {
        return _raw.Units.Exists(u => u.UnitName == unitId);
    }

    public void SetUnitLevel(string unitId, int level)
    {
        UnitEntry entry = GetUnit(unitId);
        if (entry != null) entry.Level = level;
    }

    public void AddUnitExp(string unitId, int amount)
    {
        UnitEntry entry = GetUnit(unitId);
        if (entry != null) entry.Exp += amount;
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()              => JsonUtility.ToJson(_raw);
    public void   Deserialize(string json) => _raw = JsonUtility.FromJson<UnitRawData>(json) ?? new UnitRawData();

    public void SetDefaults()
    {
        _raw = new UnitRawData();
        _raw.Units.Add(new UnitEntry { UnitName = "General", Level = 100, Exp = 0, SkillId = -1 });
        _raw.Units.Add(new UnitEntry { UnitName = "General1123", Level = 100, Exp = 0, SkillId = -1 });
        _raw.Units.Add(new UnitEntry { UnitName = "General2123", Level = 100, Exp = 0, SkillId = -1 });
        _raw.Units.Add(new UnitEntry { UnitName = "General3123", Level = 100, Exp = 0, SkillId = -1 });
        _raw.Units.Add(new UnitEntry { UnitName = "General4123", Level = 100, Exp = 0, SkillId = -1 });
    }

    // ── 직렬화 전용 내부 클래스 ──────────────────────────────

    [Serializable]
    class UnitRawData
    {
        public List<UnitEntry> Units = new();
    }
}

// ── 유닛 항목 ─────────────────────────────────────────────────

[Serializable]
public class UnitEntry
{
    public string    UnitName;         // PoolController 풀 키와 동일하게 저장 (스폰 시 PoolKey 로 사용)
    public int       Level   = 1;
    public int       Exp     = 0;
    public int       SkillId = -1;     // -1 = 장착 없음
    public UnitGrade Grade   = UnitGrade.Normal;
    // 직업(UnitJob)은 UnitName 시드로 결정적 배정 — UnitJobRoller.GetJob(UnitName) 으로 조회
}
