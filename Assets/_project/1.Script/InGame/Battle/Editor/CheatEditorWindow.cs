using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ============================================================
//  CheatEditorWindow.cs
//  Tools > Project K > Cheat Editor
//
//  플레이 모드 전용 치트 에디터.
//  어빌리티 / 특성 / 장비 / 장수 를 즉시 획득할 수 있다.
// ============================================================

public class CheatEditorWindow : EditorWindow
{
    // ── 탭 ──────────────────────────────────────────────────────
    int _tab;
    static readonly string[] kTabs = { "어빌리티", "특성", "장비", "장수" };

    // ── 어빌리티 ─────────────────────────────────────────────────
    AbilityData[] _allAbilities;
    string[]      _abilityLabels;
    int           _abilityIdx;
    int           _abilityLevel = 1;

    // ── 특성 ─────────────────────────────────────────────────────
    TraitData[] _allTraits;
    string[]    _traitLabels;
    int         _traitIdx;

    // ── 장비 ─────────────────────────────────────────────────────
    EquipmentData[] _allEquips;
    string[]        _equipLabels;
    int             _equipIdx;

    // ── 장수 ─────────────────────────────────────────────────────
    string    _generalName  = "";
    int       _generalLevel = 1;
    UnitGrade _generalGrade = UnitGrade.Normal;
    bool      _forceGrade   = false;

    // ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Cheat Editor")]
    static void Open() => GetWindow<CheatEditorWindow>("치트 에디터");

    void OnEnable()
    {
        RefreshAbilityList();
        RefreshTraitList();
        RefreshEquipList();
    }

    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서만 사용할 수 있습니다.", MessageType.Warning);
            return;
        }

        _tab = GUILayout.Toolbar(_tab, kTabs);
        EditorGUILayout.Space(6);

        switch (_tab)
        {
            case 0: DrawAbilityTab();  break;
            case 1: DrawTraitTab();    break;
            case 2: DrawEquipTab();    break;
            case 3: DrawGeneralTab();  break;
        }
    }

    // ── 어빌리티 탭 ───────────────────────────────────────────────

    void DrawAbilityTab()
    {
        if (_allAbilities == null || _allAbilities.Length == 0)
        {
            EditorGUILayout.HelpBox("AbilityDatabase 를 불러올 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button("새로고침")) RefreshAbilityList();
            return;
        }

        _abilityIdx   = EditorGUILayout.Popup("어빌리티", Mathf.Clamp(_abilityIdx, 0, _abilityLabels.Length - 1), _abilityLabels);
        _abilityLevel = EditorGUILayout.IntSlider("추가 레벨", _abilityLevel, 1, 3);

        var data = UserDataManager.Instance?.Get<RunAbilityData>();
        if (data != null)
        {
            int cur = data.GetLevel(_allAbilities[_abilityIdx].Id);
            EditorGUILayout.LabelField("현재 레벨", cur == 0 ? "미보유" : $"Lv.{cur}");
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button($"추가 (+{_abilityLevel})", GUILayout.Height(30)))
        {
            if (data == null) { LogNoManager(); return; }
            var ability = _allAbilities[_abilityIdx];
            for (int i = 0; i < _abilityLevel; i++) data.AddAbility(ability.Id);
            RequestSave();
            Debug.Log($"[치트] 어빌리티 [{ability.AbilityName}] +{_abilityLevel} (현재 Lv.{data.GetLevel(ability.Id)})");
        }
    }

    // ── 특성 탭 ───────────────────────────────────────────────────

    void DrawTraitTab()
    {
        if (_allTraits == null || _allTraits.Length == 0)
        {
            EditorGUILayout.HelpBox("TraitDatabase 를 불러올 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button("새로고침")) RefreshTraitList();
            return;
        }

        _traitIdx = EditorGUILayout.Popup("특성", Mathf.Clamp(_traitIdx, 0, _traitLabels.Length - 1), _traitLabels);

        var data = UserDataManager.Instance?.Get<RunTraitData>();
        if (data != null)
        {
            bool has = data.HasTrait(_allTraits[_traitIdx].TraitType);
            EditorGUILayout.LabelField("현재 상태", has ? "보유" : "미보유");
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("특성 추가", GUILayout.Height(30)))
        {
            if (data == null) { LogNoManager(); return; }
            var trait = _allTraits[_traitIdx];
            data.AddTrait(trait.TraitType);
            RequestSave();
            Debug.Log($"[치트] 특성 [{trait.TraitName}] 추가");
        }
    }

    // ── 장비 탭 ───────────────────────────────────────────────────

    void DrawEquipTab()
    {
        if (_allEquips == null || _allEquips.Length == 0)
        {
            EditorGUILayout.HelpBox("EquipmentDatabase 를 불러올 수 없습니다.", MessageType.Warning);
            if (GUILayout.Button("새로고침")) RefreshEquipList();
            return;
        }

        _equipIdx = EditorGUILayout.Popup("장비 선택", Mathf.Clamp(_equipIdx, 0, _equipLabels.Length - 1), _equipLabels);

        var selected = _allEquips[_equipIdx];
        EditorGUILayout.LabelField("ID",   selected.EquipmentId);
        EditorGUILayout.LabelField("등급", selected.Grade.ToString());

        EditorGUILayout.Space(4);

        if (GUILayout.Button("인벤토리에 추가", GUILayout.Height(30)))
        {
            var inv = UserDataManager.Instance?.Get<EquipInventoryData>();
            if (inv == null) { LogNoManager(); return; }
            inv.Add(selected.EquipmentId);
            RequestSave();
            Debug.Log($"[치트] 장비 획득 — [{selected.Grade}] {selected.EquipmentName}");
        }
    }

    // ── 장수 탭 ───────────────────────────────────────────────────

    void DrawGeneralTab()
    {
        _generalName  = EditorGUILayout.TextField("장수 이름", _generalName);
        _generalLevel = EditorGUILayout.IntField("레벨", Mathf.Max(1, _generalLevel));

        // 이름 입력 시 직업/태생 등급 미리보기
        bool nameEntered = !string.IsNullOrWhiteSpace(_generalName);
        if (nameEntered)
        {
            var job        = UnitJobRoller.GetJob(_generalName);
            var birthGrade = UnitJobRoller.GetBirthGrade(_generalName);
            EditorGUILayout.LabelField("직업 (이름 기반)", job.ToString());
            EditorGUILayout.LabelField("태생 등급",        birthGrade.ToString());
        }

        EditorGUILayout.Space(4);

        // 등급 강제 지정
        _forceGrade = EditorGUILayout.Toggle("등급 강제", _forceGrade);
        if (_forceGrade)
        {
            _generalGrade = (UnitGrade)EditorGUILayout.EnumPopup("목표 등급", _generalGrade);
            if (nameEntered)
            {
                var birth = UnitJobRoller.GetBirthGrade(_generalName);
                if (_generalGrade < birth)
                    EditorGUILayout.HelpBox($"태생 등급({birth}) 보다 낮음 — 태생 등급으로 획득됩니다.", MessageType.Info);
            }
        }

        EditorGUILayout.Space(4);

        GUI.enabled = nameEntered;

        if (GUILayout.Button("장수 획득", GUILayout.Height(30)))
        {
            var unitData = UserDataManager.Instance?.Get<UnitData>();
            if (unitData == null) { LogNoManager(); GUI.enabled = true; return; }

            if (unitData.HasUnit(_generalName))
            {
                Debug.LogWarning($"[치트] 장수 [{_generalName}] 이미 보유 중.");
                GUI.enabled = true;
                return;
            }

            var entry = new UnitEntry { UnitName = _generalName, Level = _generalLevel };

            if (_forceGrade)
            {
                var birth = UnitJobRoller.GetBirthGrade(_generalName);
                int upgrades = (int)_generalGrade - (int)birth;
                if (upgrades > 0) entry.GradeUpCount = upgrades;
                // upgrades <= 0 이면 다운그레이드이므로 무시 (태생 등급 유지)
            }

            unitData.AddUnit(entry);
            RequestSave();
            Debug.Log($"[치트] 장수 획득 — {_generalName}  Lv.{_generalLevel}  [{entry.Grade}]  [{UnitJobRoller.GetJob(_generalName)}]");
        }

        GUI.enabled = true;

        if (!nameEntered)
            EditorGUILayout.HelpBox("이름을 입력하세요.", MessageType.Info);
    }

    // ── 데이터 로드 ───────────────────────────────────────────────

    void RefreshAbilityList()
    {
        var db = AbilityDatabase.Current;
        if (db == null) return;
        _allAbilities  = db.GetAll().Where(a => a != null).ToArray();
        _abilityLabels = _allAbilities
            .Select(a => $"{a.AbilityName}  [{a.Grade}]")
            .ToArray();
    }

    void RefreshTraitList()
    {
        var db = TraitDatabase.Current;
        if (db == null) return;
        _allTraits   = db.GetAll().Where(t => t != null).ToArray();
        _traitLabels = _allTraits
            .Select(t => t.TraitName)
            .ToArray();
    }

    void RefreshEquipList()
    {
        var db = EquipmentDatabase.Current;
        if (db == null) return;
        _allEquips   = db.Equipments.Where(e => e != null).ToArray();
        _equipLabels = _allEquips
            .Select(e => $"[{e.Grade}] {e.EquipmentName}")
            .ToArray();
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────

    static void RequestSave() => UserDataManager.Instance.RequestSave();

    static void LogNoManager() =>
        Debug.LogError("[치트] UserDataManager 를 찾을 수 없습니다.");
}
