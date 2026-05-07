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

    public int AddUnitExp(string unitId, int amount)
    {
        UnitEntry entry = GetUnit(unitId);
        if (entry == null) return 0;

        entry.Exp += amount;

        int levelsGained = 0;
        int expPerLevel  = GameplayConfig.Current.ExpPerLevel;
        while (entry.Exp >= entry.Level * expPerLevel)
        {
            entry.Exp -= entry.Level * expPerLevel;
            entry.Level++;
            levelsGained++;
        }
        return levelsGained;
    }

    // ── 런 장비 슬롯 관리 (회귀 시 ClearAllEquipments 호출) ──

    public void SetEquipment(string unitId, int slot, string equipId, int enhanceLevel)
    {
        if (slot < 0 || slot >= 2) return;
        var entry = GetUnit(unitId);
        if (entry == null) return;
        entry.EnsureEquipArrays();
        entry.RunEquipSlots[slot]   = equipId ?? "";
        entry.RunEquipEnhance[slot] = enhanceLevel;
    }

    public void AddSoldierBonus(string unitId, int amount)
    {
        var entry = GetUnit(unitId);
        if (entry != null) entry.SoldierBonus += amount;
    }

    public string PickAvailableName()
    {
        var available = new List<string>(s_namePool.Length);
        foreach (var n in s_namePool)
            if (!HasUnit(n)) available.Add(n);
        if (available.Count == 0) return null;
        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    public void RemoveEquipment(string unitId, int slot)
    {
        if (slot < 0 || slot >= 2) return;
        var entry = GetUnit(unitId);
        if (entry == null) return;
        entry.EnsureEquipArrays();
        entry.RunEquipSlots[slot]   = "";
        entry.RunEquipEnhance[slot] = 0;
    }

    /// <summary>회귀(런 종료) 시 모든 장군의 런 장비를 초기화.</summary>
    public void ClearAllEquipments()
    {
        foreach (var entry in _raw.Units)
        {
            entry.RunEquipSlots   = new string[2];
            entry.RunEquipEnhance = new int[2];
        }
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()
    {
        return JsonUtility.ToJson(_raw);
    }

    public void Deserialize(string json)
    {
        _raw = JsonUtility.FromJson<UnitRawData>(json) ?? new UnitRawData();
    }

    public void SetDefaults()
    {
        _raw = new UnitRawData();
        var name = s_namePool[UnityEngine.Random.Range(0, s_namePool.Length)];
        _raw.Units.Add(new UnitEntry { UnitName = name, Level = 1, Exp = 0 });
    }

    static readonly string[] s_namePool =
    {
        "아서",   "드레이크", "마커스",  "알드릭",  "레온",    "가레스",  "트리스탄", "이반",   "로한",   "케인",
        "오웬",   "덱스터",  "그레이엄", "페닉스",  "레이번",  "시리우스", "막시무스", "아론",   "브렌던", "도미닉",
        "에릭",   "펠릭스",  "가브리엘", "헥터",   "재스퍼",  "카일",   "말콤",   "나단",   "오스카",  "패트릭",
        "로드릭",  "솔로몬",  "티모시",  "빅터",   "월터",   "요나스",  "아킬레스", "발리안",  "다미안",  "에드워드",
        "프레드릭", "길버트",  "해롤드",  "야코브",  "킬리안",  "로렌즈",  "마그누스", "니콜라스", "피어스",  "레이먼드",
        "시그프리드","발데마르", "크레이그", "라이덴",  "에반",   "코너",   "알렉산더", "버나드",  "다리우스", "에이든",
        "핀리",   "그리핀",  "헌터",   "아이작",  "자렛",   "라이언",  "모건",   "오리온",  "퀸시",   "리드",
        "샘슨",   "타이터스", "빈센트",  "윌리엄",  "자비에르", "아드리안", "블레이즈", "세드릭",  "더글라스", "엘리엇",
        "플린",   "가빈",   "허큘리스", "이고르",  "줄리안",  "케리건",  "레오",   "매슈",   "올리버",  "필립",
        "랄프",   "스탠리",  "토마스",  "울리히",  "발렌틴",  "웨슬리",  "젤드리스", "알폰스",  "베르트랑", "시그마",
    };

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
    public string UnitName;         // PoolController 풀 키와 동일하게 저장 (스폰 시 PoolKey 로 사용)
    public int    Level        = 1;
    public int    Exp          = 0;
    public int    GradeUpCount = 0;   // 등급 업그레이드 횟수 (태생 등급은 UnitName 시드로 결정)

    // 태생 등급은 이름 시드로 결정적 계산 — 저장 불필요
    public UnitGrade BirthGrade => UnitJobRoller.GetBirthGrade(UnitName);
    // 현재 등급 = 태생 등급 + 업그레이드 횟수 (최대 Epic)
    public UnitGrade Grade      => (UnitGrade)Mathf.Min((int)BirthGrade + GradeUpCount, (int)UnitGrade.Epic);

    // 직업(UnitJob)은 UnitName 시드로 결정적 배정 — UnitJobRoller.GetJob(UnitName) 으로 조회

    // ── 용병 보너스 (용병조각으로 영구 증가) ─────────────────
    public int SoldierBonus = 0;

    // ── 런 장비 슬롯 (회귀 시 초기화) ─────────────────────────
    /// <summary>슬롯 0~1 장착 중인 장비 ID. 비어있으면 "".</summary>
    public string[] RunEquipSlots   = new string[2];
    /// <summary>슬롯별 강화 레벨 (0 = 미강화).</summary>
    public int[]    RunEquipEnhance = new int[2];

    internal void EnsureEquipArrays()
    {
        if (RunEquipSlots == null || RunEquipSlots.Length < 2)
            RunEquipSlots = new string[2];
        if (RunEquipEnhance == null || RunEquipEnhance.Length < 2)
            RunEquipEnhance = new int[2];
    }
}
