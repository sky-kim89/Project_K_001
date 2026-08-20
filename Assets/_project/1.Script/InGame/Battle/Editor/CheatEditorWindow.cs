using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ============================================================
//  CheatEditorWindow.cs
//  Tools > Project K > Cheat Editor
//
//  플레이 모드 전용 치트 에디터.
//  어빌리티 / 특성 / 장비 / 장수 를 즉시 획득하고,
//  난이도 해금 제한을 풀 수 있다.
// ============================================================

public class CheatEditorWindow : EditorWindow
{
    // ── 탭 ──────────────────────────────────────────────────────
    int _tab;
    static readonly string[] kTabs = { "어빌리티", "특성", "장비", "장수", "도감", "난이도", "튜토리얼" };

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
    bool      _autoDeploy   = true;   // 보유만 하고 배치를 안 하면 전투에 안 나온다

    // ────────────────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Tool + "Cheat Editor", priority = ProjectKMenu.ToolPrio)]
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
            case 4: DrawCodexTab();    break;
            case 5: DrawDifficultyTab(); break;
            case 6: DrawTutorialTab();   break;
        }
    }

    // ── 튜토리얼 탭 ───────────────────────────────────────────────
    //
    //  ⚠ 튜토리얼은 정상 플레이로 두 번 볼 수 없다
    //    첫 판에 한 번 뜨고 완료로 기록되면 끝이다. 연출을 고칠 때마다
    //    세이브를 지우는 것 말고 방법이 없으면 아무도 안 고친다.

    Vector2 _tutorialScroll;

    void DrawTutorialTab()
    {
        var data = UserDataManager.Instance?.Get<TutorialData>();
        if (data == null)
        {
            EditorGUILayout.HelpBox("TutorialData 를 불러올 수 없습니다.", MessageType.Warning);
            return;
        }

        var mgr = TutorialManager.Instance;

        EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  재생 중", mgr != null && mgr.IsPlaying
            ? $"{mgr.Current.Id}  ({mgr.Current.CurrentStep}/{mgr.Current.StepCount})"
            : "없음");
        EditorGUILayout.LabelField("  끊긴 튜토리얼", data.InProgress == TutorialId.None
            ? "없음"
            : $"{data.InProgress}  (스텝 {data.InProgressStep})");

        EditorGUILayout.Space(8);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("전체 초기화 (다시 보기)", GUILayout.Height(32)))
            {
                data.ResetAll();
                UserDataManager.Instance.RequestSave();
                Debug.Log("[Cheat] 튜토리얼 기록 초기화 — 조건이 맞으면 처음부터 다시 뜹니다");
            }
            if (GUILayout.Button("전부 봤음 처리", GUILayout.Height(32)))
            {
                data.CompleteAllForced();
                UserDataManager.Instance.RequestSave();
                Debug.Log("[Cheat] 강제 진행 튜토리얼 전부 완료 처리");
            }
        }

        if (mgr != null && mgr.IsPlaying && GUILayout.Button("지금 것 건너뛰기", GUILayout.Height(26)))
            mgr.Skip();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("개별", EditorStyles.boldLabel);

        _tutorialScroll = EditorGUILayout.BeginScrollView(_tutorialScroll);
        foreach (TutorialId id in System.Enum.GetValues(typeof(TutorialId)))
        {
            if (id == TutorialId.None) continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool done = data.IsCompleted(id);
                EditorGUILayout.LabelField($"{(done ? "[본것]" : "[미시청]")} {id}",
                                           GUILayout.Width(220f));
                EditorGUILayout.LabelField(id.IsForced() ? "강제" : "도움말",
                                           EditorStyles.miniLabel, GUILayout.Width(50f));

                bool has = mgr != null && mgr.Has(id);
                using (new EditorGUI.DisabledScope(!done))
                {
                    if (GUILayout.Button("초기화", GUILayout.Width(60f)))
                    {
                        data.ResetOne(id);
                        UserDataManager.Instance.RequestSave();
                    }
                }
                using (new EditorGUI.DisabledScope(!has || (mgr != null && mgr.IsPlaying)))
                {
                    if (GUILayout.Button("재생", GUILayout.Width(60f)))
                        mgr.Replay(id);
                }
                if (!has) EditorGUILayout.LabelField("시나리오 없음", EditorStyles.miniLabel);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    // ── 난이도 탭 ─────────────────────────────────────────────────
    //
    //  ⚠ 해금은 '완주' 기록이라 정상 플레이로는 되돌릴 수 없다
    //    테스트할 때마다 30스테이지를 다섯 번 깰 수는 없으므로 여기서 연다.
    //    ClearedTier 를 직접 올리는 것이라 세이브에 그대로 남는다 —
    //    되돌리려면 '해금 초기화' 를 쓸 것.

    void DrawDifficultyTab()
    {
        var data = UserDataManager.Instance?.Get<DifficultyData>();
        if (data == null)
        {
            EditorGUILayout.HelpBox("DifficultyData 를 불러올 수 없습니다.", MessageType.Warning);
            return;
        }

        var cfg = DifficultyConfig.Current;
        if (cfg == null)
            EditorGUILayout.HelpBox(
                "DifficultyConfig.asset 이 없습니다 — 디버프가 전부 0 으로 동작합니다.\n" +
                "Tools > Project K > 데이터 생성 > 난이도 를 실행하세요.", MessageType.Warning);

        // ── 현재 상태 ────────────────────────────────────────
        EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  선택된 난이도", $"{data.SelectedTier.Label()} ({(int)data.SelectedTier})");
        EditorGUILayout.LabelField("  완주한 최고 등급",
            data.ClearedTierIndex < 0 ? "없음" : ((DifficultyTier)data.ClearedTierIndex).Label());
        EditorGUILayout.LabelField("  선택 가능 상한",
            ((DifficultyTier)data.MaxSelectableIndex).Label());

        bool runInProgress = UserDataManager.Instance?.Get<StageProgressData>()?.RunInProgress ?? false;
        if (runInProgress)
            EditorGUILayout.HelpBox("런 진행 중입니다. 난이도를 바꾸면 이번 판 중간부터 적용됩니다.",
                                    MessageType.Info);

        EditorGUILayout.Space(10);

        // ── 해금 ─────────────────────────────────────────────
        EditorGUILayout.LabelField("해금", EditorStyles.boldLabel);

        if (GUILayout.Button("전 난이도 해금", GUILayout.Height(32)))
        {
            int last = System.Enum.GetValues(typeof(DifficultyTier)).Length - 1;
            data.RecordClear((DifficultyTier)last);
            UserDataManager.Instance.RequestSave();
            Debug.Log("[Cheat] 전 난이도 해금 완료");
        }

        if (GUILayout.Button("해금 초기화 (출정만)", GUILayout.Height(26)))
        {
            data.SetDefaults();
            UserDataManager.Instance.RequestSave();
            Debug.Log("[Cheat] 난이도 해금 초기화 — 선택도 출정으로 되돌림");
        }

        EditorGUILayout.Space(10);

        // ── 직접 선택 ────────────────────────────────────────
        //  해금 여부와 무관하게 바로 꽂는다 — 특정 난이도만 확인하고 싶을 때 쓴다.
        EditorGUILayout.LabelField("바로 적용 (해금 무시)", EditorStyles.boldLabel);

        foreach (DifficultyTier t in System.Enum.GetValues(typeof(DifficultyTier)))
        {
            var tier  = t;
            var entry = cfg?.Get(tier);

            string debuffs = "제약 없음";
            if (entry != null)
            {
                var list = entry.ActiveDebuffs();
                if (list.Length > 0)
                    debuffs = string.Join(" · ", list.Select(d => d.Label()));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool cur = data.SelectedTier == tier;
                if (GUILayout.Button(cur ? $"▶ {tier.Label()}" : tier.Label(), GUILayout.Width(90)))
                {
                    // Select() 는 해금 검사를 하므로 우회해서 기록부터 올린다
                    data.RecordClear(tier);
                    data.Select(tier);
                    UserDataManager.Instance.RequestSave();
                    Debug.Log($"[Cheat] 난이도 → {tier.Label()}");
                }
                EditorGUILayout.LabelField(debuffs, EditorStyles.miniLabel);
            }

            if (entry != null)
                EditorGUILayout.LabelField(
                    $"    적 공·체 +{entry.EnemyStatBonus * 100f:0}%   " +
                    $"적 수 +{entry.EnemyCountBonus * 100f:0}%   " +
                    $"우두머리 쿨 -{entry.BossCooldownCut * 100f:0}%   " +
                    $"환생 ×{entry.ReincarnationMultiplier:0.0#}",
                    EditorStyles.miniLabel);
        }
    }

    // ── 도감 탭 ───────────────────────────────────────────────────
    //
    //  ⚠ 도감만 채운다 — 실제 보유는 건드리지 않는다
    //    도감은 "만나 본 적 있나" 의 기록이라 지금 들고 있는 것과 별개다.
    //    여기서 장비·장수까지 인벤토리에 넣으면 도감 버프 확인이 아니라
    //    전혀 다른 상태의 세이브가 되어 무엇 때문에 세진 건지 알 수 없어진다.
    //    (실제 획득이 필요하면 각 탭에서 따로 준다)

    void DrawCodexTab()
    {
        var codex = UserDataManager.Instance?.Get<CodexData>();
        if (codex == null)
        {
            EditorGUILayout.HelpBox("CodexData 를 불러올 수 없습니다.", MessageType.Warning);
            return;
        }

        // 현재 진행률 — 분류별 + 합계
        EditorGUILayout.LabelField("현재 수집", EditorStyles.boldLabel);
        foreach (CodexCategory c in System.Enum.GetValues(typeof(CodexCategory)))
        {
            var (owned, total) = CodexCatalog.Progress(c);
            EditorGUILayout.LabelField($"  {CodexCatalog.Label(c)}", $"{owned} / {total}");
        }

        var (allOwned, allTotal) = CodexCatalog.TotalProgress();
        EditorGUILayout.LabelField("  합계", $"{allOwned} / {allTotal}");
        EditorGUILayout.LabelField("  공/체 보너스", $"+{CodexApplier.BonusRatio * 100f:F1}%");

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("분류별 완성", EditorStyles.boldLabel);

        foreach (CodexCategory c in System.Enum.GetValues(typeof(CodexCategory)))
        {
            var cat = c;   // 클로저가 마지막 값을 물지 않게 복사
            if (GUILayout.Button($"{CodexCatalog.Label(cat)} 도감 완성", GUILayout.Height(26)))
                FillCodex(codex, cat);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("전체", EditorStyles.boldLabel);

        if (GUILayout.Button("모든 도감 완성", GUILayout.Height(34)))
        {
            int added = 0;
            foreach (CodexCategory c in System.Enum.GetValues(typeof(CodexCategory)))
                added += FillCodex(codex, c, silent: true);

            RequestSave();
            Debug.Log($"[치트] 도감 전체 완성 — {added}종 추가 " +
                      $"(공/체 +{CodexApplier.BonusRatio * 100f:F1}%)");
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("도감 초기화", GUILayout.Height(26)))
        {
            codex.SetDefaults();
            RequestSave();
            Debug.Log("[치트] 도감 초기화 — 수집 기록을 모두 지웠다.");
        }
    }

    /// <summary>
    /// 한 분류를 전부 수집 상태로 만든다. 반환값 = 새로 추가된 수.
    ///
    /// ⚠ 목록의 출처는 도감 화면과 같아야 한다
    ///   여기서 DB 를 다르게 훑으면 "완성" 을 눌렀는데 화면은 399/400 이 된다.
    ///   기록에 필요한 ID 는 CodexCatalog 가 주지 않으므로 DB 를 직접 돌되,
    ///   반드시 CodexCatalog.Build 와 같은 컬렉션을 봐야 한다.
    /// </summary>
    static int FillCodex(CodexData codex, CodexCategory category, bool silent = false)
    {
        int before = OwnedCount(category);

        switch (category)
        {
            case CodexCategory.Equipment:
                var edb = EquipmentDatabase.Current;
                if (edb != null)
                    foreach (var e in edb.Equipments)
                        if (e != null) codex.AddEquip(e.EquipmentId);
                break;

            case CodexCategory.Ability:
                var adb = AbilityDatabase.Current;
                if (adb != null)
                    foreach (var a in adb.GetAll())
                        if (a != null) codex.RecordAbility(a.Id);
                break;

            case CodexCategory.Trait:
                var tdb = TraitDatabase.Current;
                if (tdb != null)
                    foreach (var t in tdb.GetAll())
                        if (t != null) codex.RecordTrait(t.TraitType);
                break;

            case CodexCategory.General:
                foreach (var name in UnitData.AllNames)
                    codex.AddGeneral(name);
                break;
        }

        int added = OwnedCount(category) - before;

        if (!silent)
        {
            RequestSave();
            Debug.Log($"[치트] {CodexCatalog.Label(category)} 도감 완성 — {added}종 추가 " +
                      $"(공/체 +{CodexApplier.BonusRatio * 100f:F1}%)");
        }

        return added;
    }

    static int OwnedCount(CodexCategory category) => CodexCatalog.Progress(category).owned;

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

            // 이 이름이 쓰는 액티브 스킬 — 희귀 스킬 주인인지 여기서 바로 보인다
            var db = ActiveSkillDatabase.Current;
            if (db != null)
            {
                var skillId = RareSkillArbiter.Resolve(_generalName, job, db, birthGrade);
                var data    = db.Get(skillId);
                string tag  = data != null && data.IsRare ? "  ★ 희귀" : "";
                EditorGUILayout.LabelField("액티브 스킬", $"{data?.SkillName ?? skillId.ToString()}{tag}");
            }

            // 보유·배치는 별개다. 보유만 하고 배치를 안 하면 전투에 나오지 않는다
            // — "치트로 받았는데 왜 안 나오냐" 의 원인이 대부분 이것이다.
            var unitData   = UserDataManager.Instance?.Get<UnitData>();
            var deployData = UserDataManager.Instance?.Get<DeploymentData>();
            if (unitData != null && deployData != null)
            {
                bool owned    = unitData.HasUnit(_generalName);
                bool deployed = deployData.GetDeployedUnits().Contains(_generalName);
                EditorGUILayout.LabelField("현재 상태",
                    owned ? (deployed ? "보유 O · 배치 O" : "보유 O · 배치 X (전투에 안 나옴)")
                          : "미보유");
            }
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

        _autoDeploy = EditorGUILayout.Toggle("획득 후 바로 배치", _autoDeploy);

        EditorGUILayout.Space(4);

        GUI.enabled = nameEntered;

        if (GUILayout.Button("장수 획득", GUILayout.Height(30)))
        {
            var unitData = UserDataManager.Instance?.Get<UnitData>();
            if (unitData == null) { LogNoManager(); GUI.enabled = true; return; }

            // 이미 보유 중이면 획득은 건너뛰되, 배치는 시도한다
            // (보유만 하고 배치가 안 돼 "왜 전투에 안 나오냐" 가 되는 경우가 대부분이다)
            if (unitData.HasUnit(_generalName))
            {
                Debug.LogWarning($"[치트] 장수 [{_generalName}] 이미 보유 중 — 획득은 건너뜀.");
                if (_autoDeploy) TryDeploy(_generalName);
                RequestSave();
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
            if (_autoDeploy) TryDeploy(_generalName);
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

    /// <summary>
    /// 빈 배치 슬롯에 넣는다. 빈 칸이 없으면 마지막 칸을 밀어내고 그 자리에 넣는다
    /// — 치트로 부른 장수를 확인하려는 것이므로 "자리가 없어서 안 됨" 은 쓸모가 없다.
    /// </summary>
    static void TryDeploy(string unitName)
    {
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        if (deployData == null) { LogNoManager(); return; }

        if (deployData.GetDeployedUnits().Contains(unitName))
        {
            Debug.Log($"[치트] 장수 [{unitName}] 이미 배치됨.");
            return;
        }

        // 유물·특성으로 열린 칸까지만 실제 슬롯이다 — 잠긴 칸에 넣으면 전투에 안 나온다
        int activeSlots = Mathf.Min(5, RelicApplier.GetTotalActiveGeneralSlots());

        int slot = -1;
        for (int i = 0; i < activeSlots; i++)
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) { slot = i; break; }

        bool pushedOut = false;
        if (slot < 0)
        {
            slot = activeSlots - 1;
            pushedOut = true;
        }

        string before = deployData.GetUnitAt(slot);
        deployData.Deploy(unitName, slot);
        JobSynergyEvaluator.Recalculate();

        Debug.Log(pushedOut
            ? $"[치트] 장수 [{unitName}] 배치 — {slot + 1}번 칸 (기존 [{before}] 밀어냄)"
            : $"[치트] 장수 [{unitName}] 배치 — {slot + 1}번 칸");
    }

    static void RequestSave() => UserDataManager.Instance.RequestSave();

    static void LogNoManager() =>
        Debug.LogError("[치트] UserDataManager 를 찾을 수 없습니다.");
}
