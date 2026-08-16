using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  CodexData.cs
//  도감 — 한 번이라도 획득한 장비·어빌리티·특성·장수를 기록하는 세이브 섹션.
//
//  ■ 런 초기화 없음 — 영구 저장 (RelicInventoryData 와 같은 성격)
//    회귀해도 지워지지 않는다. 그게 도감의 전부다 —
//    "이번 런에 뭘 들고 있나" 가 아니라 "지금까지 뭘 만나 봤나" 를 센다.
//
//  ■ 버프
//    수집 1종당 공격력·체력 +BonusPerEntry(0.5%).
//    장군 스탯에만 건다 — 병사는 장군 스탯 × 비율로 파생되므로
//    (SoldierRuntimeBridge.StatRatio) 자동으로 같이 올라간다.
//    ⚠ 병사에도 따로 걸면 이중 적용이다.
//
//  ■ 기록 지점 (전부 Record 한 줄만 추가돼 있다)
//    장비     : EquipInventoryData.Add
//    어빌리티 : RunAbilityData.AddAbility
//    특성     : RunTraitData.AddTrait
//    장수     : UnitData.AddUnit
//
//  ⚠ 전체 종수는 DB 에서 동적으로 읽는다 (CodexCatalog)
//    여기에 총 개수를 상수로 박아 두면 장수 이름이나 장비가 늘 때마다
//    도감 진행률이 조용히 틀려진다.
// ============================================================

[Serializable]
class CodexJson
{
    public List<string> equips    = new();
    public List<int>    abilities = new();
    public List<int>    traits    = new();
    public List<string> generals  = new();
}

public class CodexData : ISaveSection
{
    /// <summary>수집 1종당 공격력·체력 증가율.</summary>
    public const float BonusPerEntry = 0.005f;   // 0.5%

    public SaveKey SaveKey => SaveKey.Codex;

    readonly HashSet<string>    _equips    = new();
    readonly HashSet<AbilityId> _abilities = new();
    readonly HashSet<TraitType> _traits    = new();
    readonly HashSet<string>    _generals  = new();

    /// <summary>새 항목이 등록될 때 발행. 도감 화면·스탯 표시가 구독한다.</summary>
    public static event Action OnCodexChanged;

    // ── 조회 ─────────────────────────────────────────────────

    public bool HasEquip(string id)      => !string.IsNullOrEmpty(id) && _equips.Contains(id);
    public bool HasAbility(AbilityId id) => _abilities.Contains(id);
    public bool HasTrait(TraitType t)    => _traits.Contains(t);
    public bool HasGeneral(string name)  => !string.IsNullOrEmpty(name) && _generals.Contains(name);

    public int EquipCount    => _equips.Count;
    public int AbilityCount  => _abilities.Count;
    public int TraitCount    => _traits.Count;
    public int GeneralCount  => _generals.Count;

    public int TotalCollected => _equips.Count + _abilities.Count + _traits.Count + _generals.Count;

    /// <summary>도감 버프 배율. 0.12 = 공격력·체력 +12%.</summary>
    public float StatBonusRatio => TotalCollected * BonusPerEntry;

    // ── 기록 ─────────────────────────────────────────────────
    //  호출부가 짧아지도록 정적 진입점을 둔다.
    //  세이브가 아직 없는 시점(스플래시 등)에 불려도 조용히 무시된다.

    static CodexData Current => UserDataManager.Instance?.Get<CodexData>();

    public static void Record(AbilityId id) => Current?.RecordAbility(id);
    public static void Record(TraitType t)  => Current?.RecordTrait(t);

    /// <summary>장비 ID 기록. 장수 이름과 헷갈리지 않게 이름을 나눠 둔다.</summary>
    public static void RecordEquip(string equipId)     => Current?.AddEquip(equipId);
    public static void RecordGeneral(string unitName)  => Current?.AddGeneral(unitName);

    public void RecordAbility(AbilityId id)          { if (_abilities.Add(id)) Changed(); }
    public void RecordTrait(TraitType t)             { if (_traits.Add(t))     Changed(); }
    public void AddEquip(string equipId)             { if (!string.IsNullOrEmpty(equipId) && _equips.Add(equipId))     Changed(); }
    public void AddGeneral(string unitName)          { if (!string.IsNullOrEmpty(unitName) && _generals.Add(unitName)) Changed(); }

    void Changed()
    {
        UserDataManager.Instance?.RequestSave();
        OnCodexChanged?.Invoke();
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()
    {
        var json = new CodexJson();
        json.equips.AddRange(_equips);
        json.generals.AddRange(_generals);
        foreach (var a in _abilities) json.abilities.Add((int)a);
        foreach (var t in _traits)    json.traits.Add((int)t);
        return JsonUtility.ToJson(json);
    }

    public void Deserialize(string jsonStr)
    {
        SetDefaults();
        if (string.IsNullOrEmpty(jsonStr)) return;

        var json = JsonUtility.FromJson<CodexJson>(jsonStr);
        if (json == null) return;

        foreach (var e in json.equips)    _equips.Add(e);
        foreach (var g in json.generals)  _generals.Add(g);
        foreach (var a in json.abilities) _abilities.Add((AbilityId)a);
        foreach (var t in json.traits)    _traits.Add((TraitType)t);
    }

    public void SetDefaults()
    {
        _equips.Clear();
        _abilities.Clear();
        _traits.Clear();
        _generals.Clear();
    }
}
