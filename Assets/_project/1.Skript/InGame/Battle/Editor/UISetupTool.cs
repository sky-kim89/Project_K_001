// ============================================================
//  UISetupTool.cs  [Editor Only]
//  Tools > Project K > Setup InGame UI 메뉴에서 실행.
//
//  수행 내용:
//    1. Assets/_project/2.Prefabs/UI/GeneralPanel.prefab 생성
//    2. 팝업 프리팹 3종 생성
//       - LoadingPopup.prefab    (게임 시작 로딩)
//       - BattleResultPopup.prefab (승리/패배 결과)
//       - PausePopup.prefab      (일시 정지)
//    3. 현재 씬에 Canvas > InGameHUD 계층 생성
//       (TopBarUI + 일시 정지 버튼, GeneralPanelContainer 포함)
//    4. PopupManager 루트 오브젝트 생성 (없을 경우)
//    5. InGameManager 참조 자동 연결 (씬에 존재할 경우)
//    6. InGameHUD._skillIcons 에 PNG 스프라이트 자동 연결
//
//  주의:
//    - 이미 "InGameHUD" 이름의 오브젝트가 씬에 있으면 삭제 후 재생성합니다.
//    - 실행 후 씬을 저장(Ctrl+S)하세요.
// ============================================================
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class UISetupTool
{
    const string ICON_SKILL_ROOT         = "Assets/_project/3.Textures/Icons/Skills";
    const string PANEL_PREFAB            = "Assets/_project/2.Prefabs/UI/GeneralPanel.prefab";
    const string LOADING_POPUP_PREFAB    = "Assets/_project/2.Prefabs/UI/LoadingPopup.prefab";
    const string RESULT_POPUP_PREFAB     = "Assets/_project/2.Prefabs/UI/BattleResultPopup.prefab";
    const string PAUSE_POPUP_PREFAB      = "Assets/_project/2.Prefabs/UI/PausePopup.prefab";

    // ActiveSkillId(1~20) 순서에 맞춘 파일명 배열 (인덱스 0 = None/빈칸용)
    static readonly string[] s_SkillFileNames =
    {
        null,                       // 0 — None
        "skill_heavy_strike",       // 1
        "skill_volley_fire",        // 2
        "skill_leap_strike",        // 3
        "skill_heal_aura",          // 4
        "skill_target_heal",        // 5
        "skill_charge_soldier",     // 6
        "skill_summon_skeleton",    // 7
        "skill_poison_zone",        // 8
        "skill_meteor",             // 9
        "skill_blizzard",           // 10
        "skill_sacrifice_soldier",  // 11
        "skill_bind",               // 12
        "skill_suicide_soldier",    // 13
        "skill_berserker",          // 14
        "skill_iron_shield",        // 15
        "skill_arrow_rain",         // 16
        "skill_battle_cry",         // 17
        "skill_shockwave",          // 18
        "skill_swift_strike",       // 19
        "skill_summon_elite",       // 20
    };

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/Setup InGame UI")]
    public static void SetupInGameUI()
    {
        // 1. GeneralPanel 프리팹 생성
        var panelPrefab = CreateGeneralPanelPrefab();
        if (panelPrefab == null)
        {
            Debug.LogError("[UISetupTool] GeneralPanel 프리팹 생성 실패 — 중단");
            return;
        }

        // 2. 팝업 프리팹 3종 생성
        var loadingPrefab = CreateLoadingPopupPrefab();
        var resultPrefab  = CreateBattleResultPopupPrefab();
        var pausePrefab   = CreatePausePopupPrefab();

        // 3. Canvas + InGameHUD 계층 생성
        var canvasGo = CreateCanvasHierarchy(panelPrefab, pausePrefab);

        // 4. PopupManager 루트 오브젝트 생성/업데이트
        CreateOrUpdatePopupManager(canvasGo);

        // 5. InGameManager 참조 연결
        WireInGameManager(loadingPrefab, resultPrefab, pausePrefab);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[UISetupTool] ✓ InGame UI 셋업 완료 — 씬을 저장하세요 (Ctrl+S)");
    }

    // ══════════════════════════════════════════════════════════
    //  GeneralPanel 프리팹
    // ══════════════════════════════════════════════════════════

    static GameObject CreateGeneralPanelPrefab()
    {
        // ┌──────────────────────────────────────────────────────┐
        // │ 레이아웃 (절대 좌표, 좌상단 원점, y ↓)               │
        // │  Portrait : x=4   y=4   w=120 h=120  (전신 초상화)  │
        // │  Name     : x=128 y=4   w=88  h=18                  │
        // │  Grade    : x=128 y=24  w=88  h=14                  │
        // │  HpBg     : x=128 y=42  w=88  h=16  (Fill 내부)     │
        // │  SolBg    : x=128 y=62  w=88  h=16  (Fill 내부)     │
        // │  SkillSlot: x=4   y=128 w=52  h=38                  │
        // │  BuffSlot : x=60+ y=132 w=24  h=24  (가로 4개)      │
        // └──────────────────────────────────────────────────────┘
        const float PW = 220f, PH = 170f;

        var root = new GameObject("GeneralPanel");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(PW, PH);
        root.AddComponent<CanvasGroup>();

        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth  = PW;
        le.preferredHeight = PH;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.10f, 0.90f);

        var panelUI = root.AddComponent<GeneralPanelUI>();

        // ── Portrait (120×120) ───────────────────────────────
        var portraitBg = MakeImg(root, "Portrait", new Color(0.8f, 0.22f, 0.22f));
        SetTL(portraitBg.gameObject, 4, 4, 120, 120);

        var portraitIconGo = MakeRect(portraitBg.gameObject, "PortraitIcon");
        Stretch(portraitIconGo);
        var portraitIcon = portraitIconGo.AddComponent<Image>();
        portraitIcon.color          = Color.white;
        portraitIcon.preserveAspect = false;

        // ── Name / Grade ──────────────────────────────────────
        var nameText = MakeTMP(root, "NameText", "장군 이름", 11, FontStyle.Bold);
        SetTL(nameText.gameObject, 128, 4, 88, 18);

        var gradeText = MakeTMP(root, "GradeText", "", 9, FontStyle.Normal);
        gradeText.color = new Color(1f, 0.85f, 0.3f);
        SetTL(gradeText.gameObject, 128, 24, 88, 14);

        // ── HP Bar ────────────────────────────────────────────
        var hpBg = MakeImg(root, "HpBarBg", new Color(0.12f, 0.04f, 0.04f));
        SetTL(hpBg.gameObject, 128, 42, 88, 16);

        var hpFill = MakeFilledH(hpBg.gameObject, "HpFill", new Color(0.85f, 0.15f, 0.15f));

        var hpText = MakeTMP(hpBg.gameObject, "HpText", "100/100", 8, FontStyle.Normal);
        Stretch(hpText.gameObject);

        // ── Soldier Bar ───────────────────────────────────────
        var solBg = MakeImg(root, "SoldierBarBg", new Color(0.04f, 0.08f, 0.16f));
        SetTL(solBg.gameObject, 128, 62, 88, 16);

        var soldierFill = MakeFilledH(solBg.gameObject, "SoldierFill", new Color(0.2f, 0.5f, 0.9f));

        var soldierText = MakeTMP(solBg.gameObject, "SoldierText", "5/5", 8, FontStyle.Normal);
        Stretch(soldierText.gameObject);

        // ── SkillSlot (52×38) ─────────────────────────────────
        var skillSlotGo = MakeRect(root, "SkillSlot");
        SetTL(skillSlotGo, 4, 128, 52, 38);
        var skillSlot = skillSlotGo.AddComponent<SkillSlotUI>();

        var skillBg = MakeImg(skillSlotGo, "SkillBg", new Color(0.12f, 0.12f, 0.12f));
        Stretch(skillBg.gameObject);

        var skillIcon = MakeImg(skillSlotGo, "Icon", Color.white);
        Stretch(skillIcon.gameObject);
        skillIcon.preserveAspect = true;

        var cdOverlay = MakeImg(skillSlotGo, "CooldownOverlay", new Color(0f, 0f, 0f, 0.78f));
        Stretch(cdOverlay.gameObject);
        {
            var imgSO = new SerializedObject(cdOverlay);
            imgSO.FindProperty("m_Type").intValue           = (int)Image.Type.Filled;
            imgSO.FindProperty("m_FillMethod").intValue     = (int)Image.FillMethod.Radial360;
            imgSO.FindProperty("m_FillClockwise").boolValue = false;
            imgSO.FindProperty("m_FillAmount").floatValue   = 0f;
            imgSO.ApplyModifiedPropertiesWithoutUndo();
        }

        var cdText = MakeTMP(skillSlotGo, "CooldownText", "", 16, FontStyle.Bold);
        Stretch(cdText.gameObject);
        cdText.outlineWidth = 0.25f;
        cdText.outlineColor = Color.black;
        cdText.gameObject.SetActive(false);

        var readyGlow = MakeRect(skillSlotGo, "ReadyGlow");
        readyGlow.AddComponent<Image>().color = new Color(1f, 0.9f, 0.2f, 0.45f);
        Stretch(readyGlow);
        readyGlow.SetActive(false);

        // ── Buff Slots ────────────────────────────────────────
        var buffSlots = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            buffSlots[i] = MakeImg(root, $"BuffSlot{i}", new Color(0.5f, 0.5f, 0.8f, 0.8f));
            SetTL(buffSlots[i].gameObject, 60 + i * 26, 132, 24, 24);
            buffSlots[i].gameObject.SetActive(false);
        }

        // ── SerializedField 연결 ──────────────────────────────
        var pso = new SerializedObject(panelUI);
        pso.FindProperty("_portraitBg").objectReferenceValue   = portraitBg;
        pso.FindProperty("_portraitIcon").objectReferenceValue = portraitIcon;
        pso.FindProperty("_nameText").objectReferenceValue     = nameText;
        pso.FindProperty("_gradeText").objectReferenceValue    = gradeText;
        pso.FindProperty("_hpFill").objectReferenceValue       = hpFill;
        pso.FindProperty("_hpText").objectReferenceValue       = hpText;
        pso.FindProperty("_soldierFill").objectReferenceValue  = soldierFill;
        pso.FindProperty("_soldierText").objectReferenceValue  = soldierText;
        pso.FindProperty("_skillSlot").objectReferenceValue    = skillSlot;
        var buffArr = pso.FindProperty("_buffSlots");
        buffArr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            buffArr.GetArrayElementAtIndex(i).objectReferenceValue = buffSlots[i];
        pso.ApplyModifiedPropertiesWithoutUndo();

        var sso = new SerializedObject(skillSlot);
        sso.FindProperty("_iconImage").objectReferenceValue       = skillIcon;
        sso.FindProperty("_cooldownOverlay").objectReferenceValue = cdOverlay;
        sso.FindProperty("_cooldownText").objectReferenceValue    = cdText;
        sso.FindProperty("_readyGlow").objectReferenceValue       = readyGlow;
        sso.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PANEL_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[UISetupTool] GeneralPanel 프리팹 저장 → {PANEL_PREFAB}");
        return prefab;
    }

    // ══════════════════════════════════════════════════════════
    //  팝업 프리팹 — LoadingPopup
    // ══════════════════════════════════════════════════════════

    static GameObject CreateLoadingPopupPrefab()
    {
        // 블로커(PopupManager 가 생성)가 배경을 가리므로
        // 팝업 자체는 텍스트만 있는 중앙 패널
        var root = new GameObject("LoadingPopup");
        SetupPopupRoot(root, 600f, 200f);
        var popup = root.AddComponent<LoadingPopup>();

        // 제목
        var title = MakeTMP(root, "TitleText", "배틀 준비 중", 36, FontStyle.Bold);
        SetRT(title.gameObject,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -40f), new Vector2(0f, 50f));

        // 상태 (점 애니메이션)
        var status = MakeTMP(root, "StatusText", "장군 소환 중...", 20, FontStyle.Normal);
        status.color = new Color(0.75f, 0.75f, 0.75f);
        SetRT(status.gameObject,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -100f), new Vector2(0f, 32f));

        // 필드 연결
        var so = new SerializedObject(popup);
        so.FindProperty("_titleText").objectReferenceValue  = title;
        so.FindProperty("_statusText").objectReferenceValue = status;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, LOADING_POPUP_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[UISetupTool] LoadingPopup 프리팹 저장 → {LOADING_POPUP_PREFAB}");
        return prefab;
    }

    // ══════════════════════════════════════════════════════════
    //  팝업 프리팹 — BattleResultPopup
    // ══════════════════════════════════════════════════════════

    static GameObject CreateBattleResultPopupPrefab()
    {
        var root = new GameObject("BattleResultPopup");
        SetupPopupRoot(root, 500f, 380f);
        var popup = root.AddComponent<BattleResultPopup>();

        // 패널 배경
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.08f, 0.96f);

        // 결과 텍스트 (승리! / 패배)
        var resultText = MakeTMP(root, "ResultText", "승리!", 52, FontStyle.Bold);
        resultText.color = new Color(1f, 0.85f, 0.1f);
        SetRT(resultText.gameObject,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -60f), new Vector2(0f, 70f));

        // 부제 텍스트
        var subText = MakeTMP(root, "SubText", "모든 적을 물리쳤습니다!", 16, FontStyle.Normal);
        subText.color = new Color(0.85f, 0.85f, 0.85f);
        SetRT(subText.gameObject,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -138f), new Vector2(0f, 30f));

        // 통계 텍스트
        var statsText = MakeTMP(root, "StatsText", "처치  0\n웨이브  1 / 5", 15, FontStyle.Normal);
        statsText.lineSpacing = 8f;
        SetRT(statsText.gameObject,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -180f), new Vector2(0f, 60f));

        // 구분선
        var divider = MakeImg(root, "Divider", new Color(0.3f, 0.3f, 0.3f, 0.6f));
        SetRT(divider.gameObject,
            new Vector2(0.1f, 1f), new Vector2(0.9f, 1f),
            new Vector2(0f, -252f), new Vector2(0f, 2f));

        // 확인 버튼
        var confirmBtn = MakeRect(root, "ConfirmButton");
        SetRT(confirmBtn,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(200f, 52f));
        var confirmBtnImg = confirmBtn.AddComponent<Image>();
        confirmBtnImg.color = new Color(0.18f, 0.42f, 0.72f);
        var confirmButton = confirmBtn.AddComponent<Button>();
        var confirmLabel  = MakeRect(confirmBtn, "Label");
        Stretch(confirmLabel);
        var confirmTmp = confirmLabel.AddComponent<TextMeshProUGUI>();
        confirmTmp.text      = "확인";
        confirmTmp.fontSize  = 20;
        confirmTmp.fontStyle = FontStyles.Bold;
        confirmTmp.alignment = TextAlignmentOptions.Center;
        confirmTmp.color     = Color.white;

        // 필드 연결
        var so = new SerializedObject(popup);
        so.FindProperty("_resultText").objectReferenceValue    = resultText;
        so.FindProperty("_subText").objectReferenceValue       = subText;
        so.FindProperty("_statsText").objectReferenceValue     = statsText;
        so.FindProperty("_confirmButton").objectReferenceValue = confirmButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, RESULT_POPUP_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[UISetupTool] BattleResultPopup 프리팹 저장 → {RESULT_POPUP_PREFAB}");
        return prefab;
    }

    // ══════════════════════════════════════════════════════════
    //  팝업 프리팹 — PausePopup
    // ══════════════════════════════════════════════════════════

    static GameObject CreatePausePopupPrefab()
    {
        var root = new GameObject("PausePopup");
        SetupPopupRoot(root, 360f, 320f);
        var popup = root.AddComponent<PausePopup>();

        // 패널 배경
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.08f, 0.97f);

        // 제목
        var title = MakeTMP(root, "TitleText", "일시 정지", 28, FontStyle.Bold);
        SetTL(title.gameObject, 0, 20, 360, 40);
        title.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
        title.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
        {
            var rt = title.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(0f, 40f);
        }

        // 버튼 레이아웃: 중앙 세로 정렬
        const float btnW = 280f, btnH = 52f, btnGap = 14f;
        const float startY = -88f;  // 제목 아래에서 시작

        var resumeBtn  = MakePauseButton(root, "ResumeButton",  "계속하기",
            new Color(0.18f, 0.55f, 0.28f), startY,              btnW, btnH);
        var restartBtn = MakePauseButton(root, "RestartButton", "다시 시작",
            new Color(0.30f, 0.30f, 0.30f), startY - (btnH + btnGap),    btnW, btnH);
        var quitBtn    = MakePauseButton(root, "QuitButton",    "종료",
            new Color(0.38f, 0.14f, 0.14f), startY - (btnH + btnGap) * 2, btnW, btnH);

        // 필드 연결
        var so = new SerializedObject(popup);
        so.FindProperty("_resumeButton").objectReferenceValue  = resumeBtn;
        so.FindProperty("_restartButton").objectReferenceValue = restartBtn;
        so.FindProperty("_quitButton").objectReferenceValue    = quitBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PAUSE_POPUP_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[UISetupTool] PausePopup 프리팹 저장 → {PAUSE_POPUP_PREFAB}");
        return prefab;
    }

    // ══════════════════════════════════════════════════════════
    //  Canvas + InGameHUD 계층
    // ══════════════════════════════════════════════════════════

    static GameObject CreateCanvasHierarchy(GameObject panelPrefab, GameObject pausePrefab)
    {
        // 기존 InGameHUD 루트 제거
        var existing = GameObject.Find("InGameHUD");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.transform.root.gameObject);
            Debug.Log("[UISetupTool] 기존 Canvas(InGameHUD 포함) 제거 후 재생성");
        }

        // ── Canvas ─────────────────────────────────────────────
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── InGameHUD ─────────────────────────────────────────
        var hudGo = MakeRect(canvasGo, "InGameHUD");
        Stretch(hudGo);
        var hud = hudGo.AddComponent<InGameHUD>();

        // ── TopBar ─────────────────────────────────────────────
        var topBarGo = MakeRect(hudGo, "TopBar");
        var topBarRT = topBarGo.GetComponent<RectTransform>();
        topBarRT.anchorMin        = new Vector2(0f, 1f);
        topBarRT.anchorMax        = new Vector2(1f, 1f);
        topBarRT.pivot            = new Vector2(0.5f, 1f);
        topBarRT.anchoredPosition = Vector2.zero;
        topBarRT.sizeDelta        = new Vector2(0f, 110f);

        var topBarBg = topBarGo.AddComponent<Image>();
        topBarBg.color = new Color(0.04f, 0.04f, 0.08f, 0.90f);

        var topBar = topBarGo.AddComponent<TopBarUI>();

        // Wave 텍스트
        var waveText = MakeTMP(topBarGo, "WaveText", "Wave 1 / 10", 22, FontStyle.Bold);
        SetRT(waveText.gameObject,
              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -10f), new Vector2(300f, 28f));

        // Wave 진행 바
        var waveBarBg = MakeImg(topBarGo, "WaveProgressBg", new Color(0.2f, 0.2f, 0.2f));
        SetRT(waveBarBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -42f), new Vector2(500f, 10f));
        var waveProgressFill = MakeFilledH(waveBarBg.gameObject, "WaveProgressFill",
                                           new Color(0.25f, 0.85f, 0.25f));

        // 타이머
        var waveTimerText = MakeTMP(topBarGo, "WaveTimerText", "0s", 14, FontStyle.Normal);
        SetRT(waveTimerText.gameObject,
              new Vector2(1f, 1f), new Vector2(1f, 1f),
              new Vector2(-74f, -10f), new Vector2(80f, 20f));
        waveTimerText.alignment = TextAlignmentOptions.Right;

        // 킬 카운트
        var killCountText = MakeTMP(topBarGo, "KillCountText", "Kills: 0", 14, FontStyle.Normal);
        SetRT(killCountText.gameObject,
              new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(10f, -10f), new Vector2(100f, 20f));
        killCountText.alignment = TextAlignmentOptions.Left;

        // 보스 HP
        var bossHpRoot = MakeRect(topBarGo, "BossHpRoot");
        SetRT(bossHpRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -56f), new Vector2(400f, 22f));
        var bossHpBg = bossHpRoot.AddComponent<Image>();
        bossHpBg.color = new Color(0.15f, 0.04f, 0.04f);
        var bossHpFill = MakeFilledH(bossHpRoot, "BossHpFill", new Color(0.9f, 0.1f, 0.1f));
        var bossHpText = MakeTMP(bossHpRoot, "BossHpText", "BOSS  1000 / 1000", 11, FontStyle.Bold);
        Stretch(bossHpText.gameObject);
        bossHpRoot.SetActive(false);

        // ── 하단 버튼 행 (배속 + 일시 정지) ──────────────────
        var bottomRow = MakeRect(topBarGo, "BottomButtons");
        SetRT(bottomRow, new Vector2(1f, 0f), new Vector2(1f, 0f),
              new Vector2(-4f, 4f), new Vector2(256f, 36f));
        var bottomHLG = bottomRow.AddComponent<HorizontalLayoutGroup>();
        bottomHLG.spacing              = 4;
        bottomHLG.childAlignment       = TextAnchor.MiddleRight;
        bottomHLG.childForceExpandWidth  = false;
        bottomHLG.childForceExpandHeight = false;

        var btn1     = MakeButton(bottomRow, "Speed1xButton", "1×");
        var btn2     = MakeButton(bottomRow, "Speed2xButton", "2×");
        var btn3     = MakeButton(bottomRow, "Speed3xButton", "3×");
        var pauseBtn = MakePauseButton(bottomRow, "PauseButton", "||",
                           new Color(0.28f, 0.28f, 0.28f), 0f, 60f, 36f);

        // TopBarUI 필드 연결
        var tso = new SerializedObject(topBar);
        tso.FindProperty("_waveText").objectReferenceValue         = waveText;
        tso.FindProperty("_waveProgressFill").objectReferenceValue = waveProgressFill;
        tso.FindProperty("_waveTimerText").objectReferenceValue    = waveTimerText;
        tso.FindProperty("_bossHpRoot").objectReferenceValue       = bossHpRoot;
        tso.FindProperty("_bossHpFill").objectReferenceValue       = bossHpFill;
        tso.FindProperty("_bossHpText").objectReferenceValue       = bossHpText;
        tso.FindProperty("_killCountText").objectReferenceValue    = killCountText;
        tso.FindProperty("_speed1xButton").objectReferenceValue    = btn1;
        tso.FindProperty("_speed2xButton").objectReferenceValue    = btn2;
        tso.FindProperty("_speed3xButton").objectReferenceValue    = btn3;
        tso.FindProperty("_pauseButton").objectReferenceValue      = pauseBtn;

        // PausePopup 프리팹을 TopBarUI 에도 연결
        if (pausePrefab != null)
        {
            var pausePopupAsset = AssetDatabase.LoadAssetAtPath<PausePopup>(PAUSE_POPUP_PREFAB);
            if (pausePopupAsset != null)
                tso.FindProperty("_pausePopupPrefab").objectReferenceValue = pausePopupAsset;
        }
        tso.ApplyModifiedPropertiesWithoutUndo();

        // ── GeneralPanelContainer ──────────────────────────────
        var container = MakeRect(hudGo, "GeneralPanelContainer");
        var contRT    = container.GetComponent<RectTransform>();
        contRT.anchorMin        = new Vector2(0f, 0f);
        contRT.anchorMax        = new Vector2(1f, 0f);
        contRT.pivot            = new Vector2(0.5f, 0f);
        contRT.anchoredPosition = new Vector2(0f, 10f);
        contRT.sizeDelta        = new Vector2(0f, 180f);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 8;
        hlg.padding              = new RectOffset(10, 10, 8, 8);
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        // ── InGameHUD 필드 연결 ───────────────────────────────
        var hso = new SerializedObject(hud);
        hso.FindProperty("_topBar").objectReferenceValue                = topBar;
        hso.FindProperty("_generalPanelPrefab").objectReferenceValue    = panelPrefab;
        hso.FindProperty("_generalPanelContainer").objectReferenceValue = container.transform;
        hso.FindProperty("_maxGeneralPanels").intValue                  = 5;

        var skillIconsProp = hso.FindProperty("_skillIcons");
        skillIconsProp.arraySize = s_SkillFileNames.Length;
        for (int i = 0; i < s_SkillFileNames.Length; i++)
        {
            Sprite sp = null;
            if (s_SkillFileNames[i] != null)
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(
                    $"{ICON_SKILL_ROOT}/{s_SkillFileNames[i]}.png");
            skillIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = sp;
        }
        hso.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[UISetupTool] Canvas > InGameHUD 계층 생성 완료");
        return canvasGo;
    }

    // ══════════════════════════════════════════════════════════
    //  PopupManager 씬 오브젝트
    // ══════════════════════════════════════════════════════════

    static void CreateOrUpdatePopupManager(GameObject canvasGo)
    {
        var pm = Object.FindObjectOfType<PopupManager>();
        if (pm == null)
        {
            var go = new GameObject("PopupManager");
            pm = go.AddComponent<PopupManager>();
            Debug.Log("[UISetupTool] PopupManager 생성");
        }

        // _popupRoot 를 Canvas 로 설정
        var pmSO = new SerializedObject(pm);
        pmSO.FindProperty("_popupRoot").objectReferenceValue = canvasGo.transform;
        pmSO.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupTool] PopupManager._popupRoot → Canvas");
    }

    // ══════════════════════════════════════════════════════════
    //  InGameManager 참조 연결
    // ══════════════════════════════════════════════════════════

    static void WireInGameManager(GameObject loadingPrefab, GameObject resultPrefab, GameObject pausePrefab)
    {
        var igm = Object.FindObjectOfType<InGameManager>();
        if (igm == null)
        {
            Debug.Log("[UISetupTool] InGameManager 를 씬에서 찾지 못했습니다. 수동으로 팝업 프리팹을 연결하세요.");
            return;
        }

        var so = new SerializedObject(igm);

        if (loadingPrefab != null)
        {
            var lp = AssetDatabase.LoadAssetAtPath<LoadingPopup>(LOADING_POPUP_PREFAB);
            so.FindProperty("_loadingPopupPrefab").objectReferenceValue = lp;
        }
        if (resultPrefab != null)
        {
            var rp = AssetDatabase.LoadAssetAtPath<BattleResultPopup>(RESULT_POPUP_PREFAB);
            so.FindProperty("_battleResultPopupPrefab").objectReferenceValue = rp;
        }
        if (pausePrefab != null)
        {
            var pp = AssetDatabase.LoadAssetAtPath<PausePopup>(PAUSE_POPUP_PREFAB);
            so.FindProperty("_pausePopupPrefab").objectReferenceValue = pp;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupTool] InGameManager 팝업 프리팹 참조 연결 완료");
    }

    // ══════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════

    /// <summary>PopupBase 루트에 공통 컴포넌트(CanvasGroup, centered anchor)를 설정한다.</summary>
    static void SetupPopupRoot(GameObject go, float w, float h)
    {
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(w, h);
        go.AddComponent<CanvasGroup>();
    }

    /// <summary>일시 정지 팝업 내 버튼을 생성한다 (LayoutElement 없음, 중앙 정렬).</summary>
    static Button MakePauseButton(GameObject parent, string name, string label,
                                  Color bgColor, float anchoredY, float w, float h)
    {
        var go  = MakeRect(parent, name);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta        = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var labelGo = MakeRect(go, "Label");
        Stretch(labelGo);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    static GameObject MakeRect(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Image MakeImg(GameObject parent, string name, Color color)
    {
        var go  = MakeRect(parent, name);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Image MakeFilledH(GameObject parent, string name, Color color)
    {
        var img = MakeImg(parent, name, color);
        var so = new SerializedObject(img);
        so.FindProperty("m_Type").intValue       = (int)Image.Type.Filled;
        so.FindProperty("m_FillMethod").intValue = (int)Image.FillMethod.Horizontal;
        so.FindProperty("m_FillAmount").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
        Stretch(img.gameObject);
        return img;
    }

    static void SetTL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name,
                                   string text, int size, FontStyle style)
    {
        var go  = MakeRect(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static Button MakeButton(GameObject parent, string name, string label)
    {
        var go  = MakeRect(parent, name);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 60;
        le.preferredHeight = 36;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.35f, 0.35f, 0.35f);
        var btn = go.AddComponent<Button>();

        var labelGo = MakeRect(go, "Label");
        Stretch(labelGo);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    // ── RectTransform 헬퍼 ────────────────────────────────────

    static void SetRT(Component c, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = c.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                      Vector2 anchoredPos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = (anchorMin + anchorMax) * 0.5f;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    static void SetRT(Image img, Vector2 anchorMin, Vector2 anchorMax,
                      Vector2 anchoredPos, Vector2 size)
        => SetRT(img.gameObject, anchorMin, anchorMax, anchoredPos, size);

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
