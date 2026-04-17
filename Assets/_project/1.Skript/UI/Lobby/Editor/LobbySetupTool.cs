using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ============================================================
//  LobbySetupTool.cs
//  Tools > Project K > Setup Lobby Scene
//
//  현재 열린 씬에 Lobby 에 필요한 모든 오브젝트/컴포넌트를
//  하나의 루트(LobbyRoot) 아래 생성하고 각 필드를 자동 연결한다.
//
//  ※ Singleton 매니저들(LobbyManager · PoolController · PopupManager)은
//    런타임 Awake 에서 transform.SetParent(null) 후 DontDestroyOnLoad 처리된다.
//    초기 배치는 LobbyRoot 아래지만 실행 시점에 루트로 분리되는 것이 정상.
//
//  생성 계층:
//    LobbyRoot
//    ├── LobbyManager       [LobbyManager]
//    ├── PoolController     [PoolController]
//    │   ├── UnitPool       [ObjectPool  Type=Unit]
//    │   ├── EffectPool     [ObjectPool  Type=Effect]
//    │   └── ProjectilePool [ObjectPool  Type=Projectile]
//    ├── PopupManager       [PopupManager]
//    └── LobbyCanvas        [Canvas / CanvasScaler 1080×1920 / GraphicRaycaster]
//        ├── Background
//        ├── TopBar
//        │   ├── PlayerIcon
//        │   ├── GoldGroup  / GemGroup / EnergyGroup
//        │   └── SettingsBtn
//        ├── StageSelectPanel  [StageSelectUI — 필드 전체 자동 연결]
//        │   ├── TabRow        NormalTab / EliteTab
//        │   ├── StageInfo     StageNameText / BestRecordText
//        │   ├── PreviewArea   PreviewBg / PreviewImage / PrevBtn / NextBtn
//        │   └── BattleArea    ProgressText / BattleStartBtn / EnergyCostText
//        ├── NavBar            홈 / 영웅 / 전투 / 상점 / 프로필
//        └── PopupRoot         (PopupManager._popupRoot 연결)
//    EventSystem (씬에 없으면 자동 생성)
// ============================================================

public static class LobbySetupTool
{
    // ── 색상 팔레트 ───────────────────────────────────────────
    static readonly Color BgColor          = new Color(0.05f,  0.05f,  0.10f,  1f);
    static readonly Color BarColor         = new Color(0.07f,  0.07f,  0.13f,  1f);
    static readonly Color PanelColor       = new Color(0.09f,  0.09f,  0.16f,  1f);
    static readonly Color TabActiveColor   = new Color(0.20f,  0.70f,  0.90f,  1f);
    static readonly Color TabInactiveColor = new Color(0.22f,  0.22f,  0.28f,  1f);
    static readonly Color BattleBtnColor   = new Color(0.11f,  0.72f,  0.58f,  1f);
    static readonly Color ArrowBtnColor    = new Color(0.25f,  0.25f,  0.35f,  0.70f);
    static readonly Color PreviewBgColor   = new Color(0.04f,  0.04f,  0.09f,  1f);
    static readonly Color IconBgColor      = new Color(0.22f,  0.22f,  0.32f,  1f);
    static readonly Color GoldColor        = new Color(1.00f,  0.80f,  0.20f,  1f);
    static readonly Color GemColor         = new Color(0.60f,  0.40f,  1.00f,  1f);
    static readonly Color EnergyColor      = new Color(0.30f,  0.90f,  1.00f,  1f);
    static readonly Color SettingsBtnColor = new Color(0.18f,  0.18f,  0.26f,  1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Setup Lobby Scene")]
    static void Setup()
    {
        if (GameObject.Find("LobbyRoot") != null)
        {
            EditorUtility.DisplayDialog("Setup Lobby Scene",
                "LobbyRoot 가 이미 씬에 존재합니다.\n기존 오브젝트를 삭제한 뒤 다시 실행하세요.", "확인");
            return;
        }

        var root = BuildRoot();
        EnsureEventSystem();

        Undo.RegisterCreatedObjectUndo(root, "Setup Lobby Scene");
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log("[LobbySetupTool] LobbyRoot 생성 완료 — StageConfig 를 Inspector 에서 확인하세요.");
    }

    // ── 루트 ─────────────────────────────────────────────────

    static GameObject BuildRoot()
    {
        var root = new GameObject("LobbyRoot");

        // ── 매니저들 ─────────────────────────────────────────
        var lobbyMgr  = BuildLobbyManager(root);
        var poolCtrl  = BuildPoolController(root);
        var popupMgr  = BuildPopupManager(root);

        // ── UI ───────────────────────────────────────────────
        Transform popupRoot;
        StageSelectUI stageUI;
        BuildCanvas(root, out stageUI, out popupRoot);

        // ── 필드 연결 ────────────────────────────────────────
        WireLobbyManager(lobbyMgr);
        WirePopupManager(popupMgr, popupRoot);

        return root;
    }

    // ============================================================
    //  매니저 생성
    // ============================================================

    static LobbyManager BuildLobbyManager(GameObject parent)
    {
        var go = CreateEmpty(parent, "LobbyManager");
        return go.AddComponent<LobbyManager>();
    }

    static PoolController BuildPoolController(GameObject parent)
    {
        var go = CreateEmpty(parent, "PoolController");
        var pc = go.AddComponent<PoolController>();

        var unitPool  = CreateEmpty(go, "UnitPool");
        var effectPool = CreateEmpty(go, "EffectPool");
        var projPool  = CreateEmpty(go, "ProjectilePool");

        var up = unitPool.AddComponent<ObjectPool>();   up.Type = PoolType.Unit;
        var ep = effectPool.AddComponent<ObjectPool>(); ep.Type = PoolType.Effect;
        var pp = projPool.AddComponent<ObjectPool>();   pp.Type = PoolType.Projectile;

        // PoolController.Pools 리스트에 세 풀 연결
        var so   = new SerializedObject(pc);
        var prop = so.FindProperty("Pools");
        prop.arraySize = 3;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = up;
        prop.GetArrayElementAtIndex(1).objectReferenceValue = ep;
        prop.GetArrayElementAtIndex(2).objectReferenceValue = pp;
        so.ApplyModifiedProperties();

        return pc;
    }

    static PopupManager BuildPopupManager(GameObject parent)
    {
        var go = CreateEmpty(parent, "PopupManager");
        return go.AddComponent<PopupManager>();
    }

    // ============================================================
    //  Canvas 전체 빌드
    // ============================================================

    static void BuildCanvas(GameObject root,
                            out StageSelectUI stageUI,
                            out Transform     popupRoot)
    {
        var go = CreateEmpty(root, "LobbyCanvas");

        // Canvas
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        // CanvasScaler
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // ── 배경 ─────────────────────────────────────────────
        var bg = CreatePanel(go, "Background", BgColor);
        Stretch(bg);

        // ── TopBar ────────────────────────────────────────────
        BuildTopBar(go);

        // ── StageSelectPanel ──────────────────────────────────
        stageUI = BuildStageSelectPanel(go);

        // ── NavBar ────────────────────────────────────────────
        BuildNavBar(go);

        // ── PopupRoot (PopupManager._popupRoot 연결용) ────────
        var pr = CreateEmpty(go, "PopupRoot");
        Stretch(pr);
        popupRoot = pr.transform;
    }

    // ============================================================
    //  TopBar  (상단 130px 고정)
    // ============================================================

    static void BuildTopBar(GameObject canvas)
    {
        var bar = CreatePanel(canvas, "TopBar", BarColor);
        AnchorTop(bar, 130);

        // 플레이어 아이콘 (왼쪽)
        var icon = CreateImage(bar, "PlayerIcon", IconBgColor);
        SetRect(icon.GetComponent<RectTransform>(), new Vector2(-460, 0), new Vector2(88, 88));

        // 레벨 배지 (아이콘 위에 겹쳐서 표시)
        var lvText = CreateTMP(bar, "LevelText", "Lv.1", 18, FontStyles.Bold);
        SetRect(lvText.rectTransform, new Vector2(-460, -52), new Vector2(88, 28));

        // 통화 그룹 (오른쪽 세 묶음)
        CreateCurrencyGroup(bar, "GoldGroup",   GoldColor,   "0",       new Vector2(130, 0));
        CreateCurrencyGroup(bar, "GemGroup",    GemColor,    "0",       new Vector2(300, 0));
        CreateCurrencyGroup(bar, "EnergyGroup", EnergyColor, "30 / 30", new Vector2(460, 0));

        // 설정 버튼 (우상단 모서리)
        var settingsBtn = CreateButton(bar, "SettingsBtn", "⚙", SettingsBtnColor, 28);
        {
            var rt = settingsBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12, -12);
            rt.sizeDelta        = new Vector2(68, 68);
        }
    }

    static void CreateCurrencyGroup(GameObject parent, string name, Color iconColor,
                                    string defaultVal, Vector2 pos)
    {
        var group = CreateEmpty(parent, name);
        SetRect(group.GetComponent<RectTransform>(), pos, new Vector2(140, 52));

        var icon = CreateImage(group, "Icon", iconColor);
        SetRect(icon.GetComponent<RectTransform>(), new Vector2(-42, 0), new Vector2(36, 36));

        var txt = CreateTMP(group, "Value", defaultVal, 22, FontStyles.Bold);
        SetRect(txt.rectTransform, new Vector2(28, 0), new Vector2(90, 40));
        txt.alignment = TextAlignmentOptions.Left;
    }

    // ============================================================
    //  StageSelectPanel  (TopBar ~ NavBar 사이 전체)
    // ============================================================

    static StageSelectUI BuildStageSelectPanel(GameObject canvas)
    {
        var panel = CreateEmpty(canvas, "StageSelectPanel");
        {
            // TopBar(130) 아래 ~ NavBar(110) 위
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0,  110);
            rt.offsetMax = new Vector2(0, -130);
        }
        var ui = panel.AddComponent<StageSelectUI>();

        // ── TabRow (상단 80px) ───────────────────────────────
        var tabRow = CreateEmpty(panel, "TabRow");
        AnchorTopInside(tabRow, 80, 0);

        var normalTab = CreateButton(tabRow, "NormalTab", "일반",   TabActiveColor,   26);
        SetRect(normalTab.GetComponent<RectTransform>(), new Vector2(-205, 0), new Vector2(380, 62));

        var eliteTab  = CreateButton(tabRow, "EliteTab",  "엘리트", TabInactiveColor, 26);
        SetRect(eliteTab.GetComponent<RectTransform>(),  new Vector2( 205, 0), new Vector2(380, 62));

        // ── StageInfo (탭 아래 130px) ────────────────────────
        var infoArea = CreateEmpty(panel, "StageInfo");
        AnchorTopInside(infoArea, 130, 80);

        var stageName = CreateTMP(infoArea, "StageNameText", "일반 스테이지 1", 38, FontStyles.Bold);
        SetRect(stageName.rectTransform, new Vector2(0, 26), new Vector2(900, 54));

        var bestRecord = CreateTMP(infoArea, "BestRecordText", "최고 기록  --:--", 22, FontStyles.Normal);
        SetRect(bestRecord.rectTransform, new Vector2(0, -28), new Vector2(900, 38));
        bestRecord.color = new Color(0.65f, 0.65f, 0.70f);

        // ── PreviewArea (중앙 가변 영역) ─────────────────────
        var previewArea = CreateEmpty(panel, "PreviewArea");
        {
            var rt = previewArea.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0,  220);
            rt.offsetMax = new Vector2(0, -210);
        }

        var previewBg = CreateImage(previewArea, "PreviewBg", PreviewBgColor);
        {
            var rt = previewBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.05f);
            rt.anchorMax = new Vector2(0.92f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var previewImg = CreateImage(previewArea, "PreviewImage", new Color(1, 1, 1, 0));
        {
            var rt = previewImg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.10f);
            rt.anchorMax = new Vector2(0.85f, 0.90f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            previewImg.preserveAspect = true;
        }

        var prevBtn = CreateButton(previewArea, "PrevBtn", "‹", ArrowBtnColor, 48);
        SetRect(prevBtn.GetComponent<RectTransform>(), new Vector2(-460, 0), new Vector2(88, 88));
        prevBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var nextBtn = CreateButton(previewArea, "NextBtn", "›", ArrowBtnColor, 48);
        SetRect(nextBtn.GetComponent<RectTransform>(), new Vector2( 460, 0), new Vector2(88, 88));
        nextBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // ── BattleArea (하단 220px) ───────────────────────────
        var battleArea = CreateEmpty(panel, "BattleArea");
        AnchorBottomInside(battleArea, 220, 0);

        var progressText = CreateTMP(battleArea, "ProgressText", "일반 스테이지 1 클리어  0 / 1", 20, FontStyles.Normal);
        SetRect(progressText.rectTransform, new Vector2(0, 165), new Vector2(840, 38));
        progressText.color = new Color(0.60f, 0.60f, 0.65f);

        var battleBtn = CreateButton(battleArea, "BattleStartBtn", "전투 시작", BattleBtnColor, 34);
        {
            var rt = battleBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.08f, 0f);
            rt.anchorMax        = new Vector2(0.92f, 0f);
            rt.pivot            = new Vector2(0.5f,  0f);
            rt.anchoredPosition = new Vector2(0, 70);
            rt.sizeDelta        = new Vector2(0, 94);
        }
        battleBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var energyText = CreateTMP(battleArea, "EnergyCostText", "⚡  5", 22, FontStyles.Normal);
        SetRect(energyText.rectTransform, new Vector2(0, 26), new Vector2(300, 38));
        energyText.color = EnergyColor;

        // ── StageSelectUI 필드 연결 ───────────────────────────
        var so = new SerializedObject(ui);
        SetObj(so, "_normalTabBtn",   normalTab.GetComponent<Button>());
        SetObj(so, "_eliteTabBtn",    eliteTab.GetComponent<Button>());
        SetObj(so, "_stageNameText",  stageName);
        SetObj(so, "_bestRecordText", bestRecord);
        SetObj(so, "_previewImage",   previewImg);
        SetObj(so, "_prevBtn",        prevBtn.GetComponent<Button>());
        SetObj(so, "_nextBtn",        nextBtn.GetComponent<Button>());
        SetObj(so, "_battleStartBtn", battleBtn.GetComponent<Button>());
        SetObj(so, "_energyCostText", energyText);
        SetObj(so, "_progressText",   progressText);
        so.ApplyModifiedProperties();

        return ui;
    }

    // ============================================================
    //  NavBar  (하단 110px 고정)
    // ============================================================

    static void BuildNavBar(GameObject canvas)
    {
        var bar = CreatePanel(canvas, "NavBar", BarColor);
        AnchorBottom(bar, 110);

        string[] labels = { "홈", "영웅", "전투", "상점", "프로필" };
        float startX    = -400f;
        float step      = 200f;

        for (int i = 0; i < labels.Length; i++)
        {
            var btn = CreateButton(bar, $"NavBtn_{labels[i]}", labels[i], PanelColor, 20);
            SetRect(btn.GetComponent<RectTransform>(),
                new Vector2(startX + step * i, 0), new Vector2(170, 84));

            // 전투 탭 활성 색상
            if (i == 2)
                btn.GetComponent<Image>().color = TabActiveColor;
        }
    }

    // ============================================================
    //  매니저 필드 연결
    // ============================================================

    static void WireLobbyManager(LobbyManager lm)
    {
        var so = new SerializedObject(lm);

        // StageConfig SO 검색 (프로젝트 전체 탐색)
        var guids = AssetDatabase.FindAssets("t:StageConfig");
        if (guids.Length > 0)
        {
            var cfg = AssetDatabase.LoadAssetAtPath<StageConfig>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            SetObj(so, "_stageConfig", cfg);
            Debug.Log($"[LobbySetupTool] StageConfig 연결: {AssetDatabase.GUIDToAssetPath(guids[0])}");
        }
        else
        {
            Debug.LogWarning("[LobbySetupTool] StageConfig SO 를 찾지 못했습니다. Inspector 에서 직접 할당하세요.");
        }

        so.ApplyModifiedProperties();
    }

    static void WirePopupManager(PopupManager pm, Transform popupRoot)
    {
        var so = new SerializedObject(pm);

        // _popupRoot
        SetObj(so, "_popupRoot", popupRoot);

        // 팝업 프리팹 로드 — PopupPrefabCreator 로 미리 생성해야 함
        string[] paths =
        {
            "Assets/_project/2.Prefabs/UI/BattleResultPopup.prefab",
            "Assets/_project/2.Prefabs/UI/PausePopup.prefab",
            "Assets/_project/2.Prefabs/UI/LoadingPopup.prefab",
        };

        var loaded = new List<PopupBase>();
        foreach (var path in paths)
        {
            var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabGo != null)
            {
                var pb = prefabGo.GetComponent<PopupBase>();
                if (pb != null) { loaded.Add(pb); continue; }
            }
            Debug.LogWarning($"[LobbySetupTool] 팝업 프리팹 없음: {path}\n" +
                              "→ Tools > Project K > Create Popup Prefabs 를 먼저 실행하세요.");
        }

        var prefabsProp = so.FindProperty("_prefabs");
        prefabsProp.arraySize = loaded.Count;
        for (int i = 0; i < loaded.Count; i++)
            prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = loaded[i];

        so.ApplyModifiedProperties();
    }

    // ============================================================
    //  EventSystem
    // ============================================================

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(es, "Setup Lobby Scene - EventSystem");
        Debug.Log("[LobbySetupTool] EventSystem 생성 완료");
    }

    // ============================================================
    //  UI 생성 헬퍼
    // ============================================================

    static GameObject CreateEmpty(GameObject parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Image CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CreateTMP(GameObject parent, string name,
                                     string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static GameObject CreateButton(GameObject parent, string name,
                                   string label, Color bgColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lRt = labelGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = Vector2.zero;
        lRt.offsetMax = Vector2.zero;
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }

    // ============================================================
    //  RectTransform 헬퍼
    // ============================================================

    /// <summary>부모 전체 스트레치</summary>
    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>부모 상단 고정, 전체 너비</summary>
    static void AnchorTop(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -height);
        rt.offsetMax = new Vector2(0,  0);
    }

    /// <summary>부모 하단 고정, 전체 너비</summary>
    static void AnchorBottom(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, height);
    }

    /// <summary>부모 내부 상단에서 offsetFromTop 아래, height 만큼</summary>
    static void AnchorTopInside(GameObject go, float height, float offsetFromTop)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -(offsetFromTop + height));
        rt.offsetMax = new Vector2(0, -offsetFromTop);
    }

    /// <summary>부모 내부 하단에서 offsetFromBottom 위, height 만큼</summary>
    static void AnchorBottomInside(GameObject go, float height, float offsetFromBottom)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = new Vector2(0, offsetFromBottom);
        rt.offsetMax = new Vector2(0, offsetFromBottom + height);
    }

    /// <summary>중앙 앵커, 고정 크기</summary>
    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
        else Debug.LogWarning($"[LobbySetupTool] 필드를 찾을 수 없음: {field}");
    }
}
