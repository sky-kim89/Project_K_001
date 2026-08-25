using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  CodexData.cs
//  도감 — 한 번이라도 획득한 장비·어빌리티·특성·장수를 기록하는 세이브 섹션.
//
//  ■ 런 초기화 없음 — 영구 저장 (RelicTreeData 와 같은 성격)
//    회귀해도 지워지지 않는다. 그게 도감의 전부다 —
//    "이번 런에 뭘 들고 있나" 가 아니라 "지금까지 뭘 만나 봤나" 를 센다.
//
//  ■ 버프
//    수집 1종당 공격력·체력 +BonusPerEntry(0.5%).
//    장군 스탯에만 건다 — 병사는 장군 스탯 × 비율로 파생되므로
//    (SoldierRuntimeBridge.StatRatio) 자동으로 같이 올라간다.
//    ⚠ 병사에도 따로 걸면 이중 적용이다.
//
//  ■ 버프는 여정 시작 시점에 고정된다 (LockForRun)
//    여정 도중에 새로 채운 도감은 이번 여정의 스탯에 즉시 반영되지 않고,
//    다음 여정을 시작할 때 한꺼번에 들어온다.
//    수집은 여정 중반에 몰려서 일어나는데(장비·특성·어빌리티를 그때 만난다)
//    그걸 즉시 얹으면 "지금 조금씩 세지는" 잡음이 되고, 정작 다음 회차
//    출발선이 올라간 체감은 사라진다. 성장은 회차 경계에서 한 번에 느껴야 한다.
//
//    잠금 : RunStarter.BeginRun  (여정 시작 — 이 시점의 수집 수를 박는다)
//    해제 : UserDataManager.Reincarnate (환생 — 다음 여정까지는 실시간 값)
//    ⚠ 잠긴 수는 세이브에 남는다 — 여정 도중에 앱을 껐다 켜도 그대로여야 한다.
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

    // 이번 여정에 적용 중인 수집 수. -1 = 잠기지 않음(실시간 값을 쓴다).
    public int lockedCount = -1;

    // 아직 플레이어에게 보여 주지 않은 수확 (환생 후 메인 화면에서 한 번에 뿌린다).
    // 세이브에 남긴다 — 여정 도중에 앱을 껐다 켜도 수확이 사라지면 안 된다.
    public List<string> gainEquips    = new();
    public List<int>    gainAbilities = new();
    public List<int>    gainTraits    = new();
    public List<string> gainGenerals  = new();

    // 수확을 보여 줄 때가 됐는가 — 환생이 끝난 순간에만 켜진다.
    // 이게 없으면 여정 도중에 쌓인 수확이 메인 화면에 들를 때마다 튀어나온다.
    public bool gainsReady = false;
}

/// <summary>
/// 아직 보여 주지 않은 도감 수확 한 묶음.
/// 환생 뒤 메인 화면이 CodexData.TakeRunGains() 로 가져가 한 번에 뿌리고 비운다.
/// </summary>
public class CodexRunGains
{
    public readonly List<string>    Equips    = new();
    public readonly List<AbilityId> Abilities = new();
    public readonly List<TraitType> Traits    = new();
    public readonly List<string>    Generals  = new();

    public int Count => Equips.Count + Abilities.Count + Traits.Count + Generals.Count;
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

    // 이번 여정에 박힌 수집 수. -1 = 아직 안 잠김 (여정 밖 · 옛 세이브).
    int _lockedCount = -1;

    /// <summary>지금 스탯에 실제로 걸리는 수집 수. 여정 중이면 시작 시점 값.</summary>
    public int AppliedCount => _lockedCount >= 0 ? _lockedCount : TotalCollected;

    /// <summary>도감 버프 배율. 0.12 = 공격력·체력 +12%.</summary>
    public float StatBonusRatio => AppliedCount * BonusPerEntry;

    /// <summary>다음 여정부터 걸릴 배율 — 지금까지 채운 것 전부.</summary>
    public float PendingStatBonusRatio => TotalCollected * BonusPerEntry;

    // ── 여정 잠금 ────────────────────────────────────────────

    /// <summary>여정 시작 — 지금 수집 수를 이번 여정 값으로 박는다.</summary>
    public void LockForRun()
    {
        _lockedCount = TotalCollected;
        Changed();
    }

    /// <summary>환생 — 잠금을 푼다. 다음 여정을 시작할 때까지는 실시간 값.</summary>
    public void UnlockForNextRun()
    {
        if (_lockedCount < 0) return;
        _lockedCount = -1;
        Changed();
    }

    // ── 기록 ─────────────────────────────────────────────────
    //  호출부가 짧아지도록 정적 진입점을 둔다.
    //  세이브가 아직 없는 시점(스플래시 등)에 불려도 조용히 무시된다.

    public static CodexData Current => UserDataManager.Instance?.Get<CodexData>();

    public static void Record(AbilityId id) => Current?.RecordAbility(id);
    public static void Record(TraitType t)  => Current?.RecordTrait(t);

    /// <summary>장비 ID 기록. 장수 이름과 헷갈리지 않게 이름을 나눠 둔다.</summary>
    public static void RecordEquip(string equipId)     => Current?.AddEquip(equipId);
    public static void RecordGeneral(string unitName)  => Current?.AddGeneral(unitName);

    public void RecordAbility(AbilityId id)          { if (_abilities.Add(id)) { _gains.Abilities.Add(id); Changed(); } }
    public void RecordTrait(TraitType t)             { if (_traits.Add(t))     { _gains.Traits.Add(t);     Changed(); } }
    public void AddEquip(string equipId)             { if (!string.IsNullOrEmpty(equipId)  && _equips.Add(equipId))    { _gains.Equips.Add(equipId);    Changed(); } }
    public void AddGeneral(string unitName)          { if (!string.IsNullOrEmpty(unitName) && _generals.Add(unitName)) { _gains.Generals.Add(unitName); Changed(); } }

    // ── 수확 (아직 안 보여 준 것) ─────────────────────────────
    //
    //  ⚠ 도감 버프는 여정 경계에서 한 번에 들어온다 (위 LockForRun 주석)
    //    그런데 "무엇을 채웠고 그래서 얼마나 세졌는지" 를 알려 주는 자리가 없어서,
    //    올라간 공격력·체력이 어디서 왔는지 알 수 없었다.
    //    환생 뒤 메인 화면(MainPanelUI)이 이 묶음을 가져가 한 번에 뿌린다.

    CodexRunGains _gains = new();

    // 수확을 보여 줄 때가 됐는가. 환생이 끝나야 켜진다.
    //
    // ⚠ "수확이 비어 있지 않다" 를 조건으로 쓰지 말 것
    //   수확은 여정 도중 도감에 뭔가 등록될 때마다(RecordAbility 등) 쌓이고 세이브된다.
    //   그래서 그것만 보면 장수를 고른 직후나 여정 중 앱을 껐다 켠 직후처럼
    //   MainPanelUI.OnEnable 이 도는 모든 자리에서 수확 화면이 튀어나왔다.
    //   여정이 끝나고 환생한 순간에만 보여 주는 화면이므로 신호를 따로 둔다.
    bool _gainsReady;

    /// <summary>보여 줄 수확이 쌓여 있고, 보여 줄 때(환생 직후)가 됐는가.</summary>
    public bool HasRunGains => _gainsReady && _gains.Count > 0;

    /// <summary>
    /// 환생이 끝났다 — 지금까지 쌓인 수확을 다음 메인 화면에서 보여 준다.
    /// UserDataManager.Reincarnate() 만 부른다.
    /// </summary>
    public void MarkGainsReady()
    {
        _gainsReady = true;
        UserDataManager.Instance?.RequestSave();
    }

    /// <summary>수확을 가져가고 비운다 — 같은 목록이 두 번 뜨지 않게 꺼내는 즉시 소비된다.</summary>
    public CodexRunGains TakeRunGains()
    {
        var taken = _gains;
        _gains = new CodexRunGains();
        _gainsReady = false;
        UserDataManager.Instance?.RequestSave();
        return taken;
    }

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
        json.lockedCount = _lockedCount;

        json.gainEquips.AddRange(_gains.Equips);
        json.gainGenerals.AddRange(_gains.Generals);
        foreach (var a in _gains.Abilities) json.gainAbilities.Add((int)a);
        foreach (var t in _gains.Traits)    json.gainTraits.Add((int)t);
        json.gainsReady = _gainsReady;

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

        if (json.gainEquips    != null) _gains.Equips.AddRange(json.gainEquips);
        if (json.gainGenerals  != null) _gains.Generals.AddRange(json.gainGenerals);
        if (json.gainAbilities != null) foreach (var a in json.gainAbilities) _gains.Abilities.Add((AbilityId)a);
        if (json.gainTraits    != null) foreach (var t in json.gainTraits)    _gains.Traits.Add((TraitType)t);

        // 필드가 없던 옛 세이브는 false 로 들어온다 — 그게 맞는 기본값이다.
        // (여정 도중에 쌓여 있던 수확이 업데이트 직후 한 번 튀어나오는 것을 막는다)
        _gainsReady = json.gainsReady;

        // 잠금 필드가 없던 옛 세이브는 -1(미잠금)로 본다 — 진행 중이던 여정에서
        // 도감 버프가 통째로 사라지지 않게. JsonUtility 는 없는 필드를 0 으로
        // 채울 수 있으므로 필드 존재 여부를 문자열로 직접 본다.
        _lockedCount = jsonStr.Contains("\"lockedCount\"") ? json.lockedCount : -1;
    }

    public void SetDefaults()
    {
        _equips.Clear();
        _abilities.Clear();
        _traits.Clear();
        _generals.Clear();
        _lockedCount = -1;
        _gains = new CodexRunGains();
        _gainsReady = false;
    }
}
