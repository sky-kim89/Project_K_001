using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  LobbyPrefabCreator.cs
//  Tools > Project K > Create Lobby Prefab
//  로비 Canvas 프리팹을 Assets/_project/2.Prefabs/UI/ 에 생성한다.
//
//  생성 구조:
//    LobbyCanvas (Canvas, CanvasScaler 1080×1920)
//    ├── Background
//    ├── TopBar          (placeholder — 추후 구현)
//    ├── StageSelectPanel (StageSelectUI 컴포넌트 + 모든 필드 자동 연결)
//    │   ├── TabRow       NormalTab / EliteTab
//    │   ├── StageInfo    StageNameText / BestRecordText
//    │   ├── PreviewArea  PrevBtn / PreviewImage / NextBtn
//    │   └── BattleArea   ProgressText / BattleStartBtn / EnergyCostText
//    └── NavBar          (placeholder — 추후 구현)
// ============================================================

public static class LobbyPrefabCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI";

    // ── 색상 팔레트 ───────────────────────────────────────────
    static readonly Color BgColor         = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color BarColor        = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color PanelColor      = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color TabActiveColor  = new Color(0.20f, 0.70f, 0.90f, 1f);
    static readonly Color TabInactiveColor= new Color(0.22f, 0.22f, 0.28f, 1f);
    static readonly Color BattleBtnColor  = new Color(0.11f, 0.72f, 0.58f, 1f);
    static readonly Color ArrowBtnColor   = new Color(0.25f, 0.25f, 0.35f, 0.70f);
    static readonly Color PreviewBgColor  = new Color(0.04f, 0.04f, 0.09f, 1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Create Lobby Prefab")]
    static void CreateLobby()
    {
        var root = CreateCanvas("LobbyCanvas");

        // Background
        var bg = CreatePanel(root, "Background", BgColor);
        Stretch(bg, 0, 0, 0, 0);

        // TopBar (130px, 상단 고정)
        var topBar = CreateTopBar(root);

        // NavBar (110px, 하단 고정)
        var navBar = CreateNavBar(root);

        // StageSelectPanel (TopBar~NavBar 사이 영역)
        var stagePanel = CreateStagePanel(root);

        // LobbyManager 는 씬에 별도 배치 (프리팹에서 제외)

        PrefabUtility.SaveAsPrefabAsset(root, $"{SavePath}/LobbyCanvas.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LobbyPrefabCreator] LobbyCanvas.prefab 생성 완료");
    }

    // ── TopBar ────────────────────────────────────────────────

    static GameObject CreateTopBar(GameObject parent)
    {
        var bar = CreatePanel(parent, "TopBar", BarColor);
        AnchorTop(bar, 130);

        // 플레이어 아이콘 자리 (왼쪽)
        var icon = CreateImage(bar, "PlayerIcon", new Color(0.3f, 0.3f, 0.4f));
        SetRect(icon.GetComponent<RectTransform>(),
            new Vector2(-460, 0), new Vector2(90, 90));

        // 통화 표시 (오른쪽) — 세 묶음
        CreateCurrencyGroup(bar, "Gold",   new Color(1.0f, 0.8f, 0.2f), "0",       new Vector2(120, 0));
        CreateCurrencyGroup(bar, "Gem",    new Color(0.6f, 0.4f, 1.0f), "0",       new Vector2(280, 0));
        CreateCurrencyGroup(bar, "Energy", new Color(0.3f, 0.9f, 1.0f), "30 / 30", new Vector2(450, 0));

        return bar;
    }

    static void CreateCurrencyGroup(GameObject parent, string name, Color iconColor, string defaultVal, Vector2 pos)
    {
        var group = new GameObject(name, typeof(RectTransform));
        group.transform.SetParent(parent.transform, false);
        SetRect(group.GetComponent<RectTransform>(), pos, new Vector2(130, 50));

        var icon = CreateImage(group, "Icon", iconColor);
        SetRect(icon.GetComponent<RectTransform>(), new Vector2(-40, 0), new Vector2(34, 34));

        var text = CreateTMP(group, "Value", defaultVal, 22, FontStyles.Bold);
        SetRect(text.GetComponent<RectTransform>(), new Vector2(25, 0), new Vector2(80, 40));
        text.alignment = TextAlignmentOptions.Left;
    }

    // ── NavBar ────────────────────────────────────────────────

    static GameObject CreateNavBar(GameObject parent)
    {
        var bar = CreatePanel(parent, "NavBar", BarColor);
        AnchorBottom(bar, 110);

        string[] icons = { "홈", "영웅", "전투", "상점", "프로필" };
        float startX = -400f;
        float step   = 200f;

        for (int i = 0; i < icons.Length; i++)
        {
            var btn = CreateButton(bar, $"NavBtn_{icons[i]}", icons[i], PanelColor, 18);
            SetRect(btn.GetComponent<RectTransform>(),
                new Vector2(startX + step * i, 0), new Vector2(160, 80));

            // 전투 탭 강조
            if (i == 2)
                btn.GetComponent<Image>().color = TabActiveColor;
        }

        return bar;
    }

    // ── StageSelectPanel ──────────────────────────────────────

    static GameObject CreateStagePanel(GameObject parent)
    {
        var panel = new GameObject("StageSelectPanel", typeof(RectTransform));
        panel.transform.SetParent(parent.transform, false);

        // TopBar(130) ~ NavBar(110) 사이
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, 110);
        rt.offsetMax = new Vector2(0, -130);

        var ui = panel.AddComponent<StageSelectUI>();

        // TabRow (상단 80px)
        var tabRow = new GameObject("TabRow", typeof(RectTransform));
        tabRow.transform.SetParent(panel.transform, false);
        AnchorTopInside(tabRow, 80, 0);

        var normalTab = CreateButton(tabRow, "NormalTab", "일반", TabActiveColor, 24);
        SetRect(normalTab.GetComponent<RectTransform>(), new Vector2(-200, 0), new Vector2(240, 60));

        var eliteTab = CreateButton(tabRow, "EliteTab", "엘리트", TabInactiveColor, 24);
        SetRect(eliteTab.GetComponent<RectTransform>(), new Vector2(200, 0), new Vector2(240, 60));

        // StageInfo (탭 아래 130px)
        var infoArea = new GameObject("StageInfo", typeof(RectTransform));
        infoArea.transform.SetParent(panel.transform, false);
        AnchorTopInside(infoArea, 130, 80);

        var stageName = CreateTMP(infoArea, "StageNameText", "일반 스테이지 1", 36, FontStyles.Bold);
        SetRect(stageName.GetComponent<RectTransform>(), new Vector2(0, 25), new Vector2(900, 50));

        var bestRecord = CreateTMP(infoArea, "BestRecordText", "최고 기록  --:--", 22, FontStyles.Normal);
        SetRect(bestRecord.GetComponent<RectTransform>(), new Vector2(0, -28), new Vector2(900, 36));
        bestRecord.color = new Color(0.7f, 0.7f, 0.7f);

        // PreviewArea (중앙 나머지 영역, BattleArea 220px 위)
        var previewArea = new GameObject("PreviewArea", typeof(RectTransform));
        previewArea.transform.SetParent(panel.transform, false);
        var previewRt = previewArea.GetComponent<RectTransform>();
        previewRt.anchorMin = Vector2.zero;
        previewRt.anchorMax = Vector2.one;
        previewRt.offsetMin = new Vector2(0, 220);
        previewRt.offsetMax = new Vector2(0, -210);

        // 프리뷰 배경 (어두운 원형 느낌)
        var previewBg = CreateImage(previewArea, "PreviewBg", PreviewBgColor);
        var previewBgRt = previewBg.GetComponent<RectTransform>();
        previewBgRt.anchorMin = new Vector2(0.1f, 0.05f);
        previewBgRt.anchorMax = new Vector2(0.9f, 0.95f);
        previewBgRt.offsetMin = Vector2.zero;
        previewBgRt.offsetMax = Vector2.zero;

        // 스테이지 프리뷰 이미지
        var preview = CreateImage(previewArea, "PreviewImage", new Color(1, 1, 1, 0));
        var previewRt2 = preview.GetComponent<RectTransform>();
        previewRt2.anchorMin = new Vector2(0.15f, 0.1f);
        previewRt2.anchorMax = new Vector2(0.85f, 0.9f);
        previewRt2.offsetMin = Vector2.zero;
        previewRt2.offsetMax = Vector2.zero;
        preview.preserveAspect = true;

        // Prev / Next 버튼
        var prevBtn = CreateButton(previewArea, "PrevBtn", "<", ArrowBtnColor, 36);
        SetRect(prevBtn.GetComponent<RectTransform>(), new Vector2(-460, 0), new Vector2(90, 90));
        prevBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var nextBtn = CreateButton(previewArea, "NextBtn", ">", ArrowBtnColor, 36);
        SetRect(nextBtn.GetComponent<RectTransform>(), new Vector2(460, 0), new Vector2(90, 90));
        nextBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // BattleArea (하단 220px)
        var battleArea = new GameObject("BattleArea", typeof(RectTransform));
        battleArea.transform.SetParent(panel.transform, false);
        AnchorBottomInside(battleArea, 220, 0);

        var progressText = CreateTMP(battleArea, "ProgressText", "스테이지 1 클리어  0 / 1", 20, FontStyles.Normal);
        SetRect(progressText.GetComponent<RectTransform>(), new Vector2(0, 70), new Vector2(800, 36));
        progressText.color = new Color(0.65f, 0.65f, 0.65f);

        var battleBtn = CreateButton(battleArea, "BattleStartBtn", "전투 시작", BattleBtnColor, 32);
        var battleBtnRt = battleBtn.GetComponent<RectTransform>();
        battleBtnRt.anchorMin = new Vector2(0.1f, 0f);
        battleBtnRt.anchorMax = new Vector2(0.9f, 0f);
        battleBtnRt.anchoredPosition = new Vector2(0, 30);
        battleBtnRt.sizeDelta        = new Vector2(0, 90);
        battleBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var energyCostText = CreateTMP(battleArea, "EnergyCostText", "⚡  5", 22, FontStyles.Normal);
        SetRect(energyCostText.GetComponent<RectTransform>(), new Vector2(0, -55), new Vector2(300, 36));
        energyCostText.color = new Color(0.4f, 0.9f, 1.0f);

        // ── StageSelectUI 필드 연결 ───────────────────────────
        var so = new SerializedObject(ui);
        SetObj(so, "_normalTabBtn",   normalTab.GetComponent<Button>());
        SetObj(so, "_eliteTabBtn",    eliteTab.GetComponent<Button>());
        SetObj(so, "_stageNameText",  stageName);
        SetObj(so, "_bestRecordText", bestRecord);
        SetObj(so, "_previewImage",   preview);
        SetObj(so, "_prevBtn",        prevBtn.GetComponent<Button>());
        SetObj(so, "_nextBtn",        nextBtn.GetComponent<Button>());
        SetObj(so, "_battleStartBtn", battleBtn.GetComponent<Button>());
        SetObj(so, "_energyCostText", energyCostText);
        SetObj(so, "_progressText",   progressText);
        so.ApplyModifiedProperties();

        return panel;
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

    static GameObject CreateCanvas(string name)
    {
        var go     = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
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

    static TextMeshProUGUI CreateTMP(GameObject parent, string name, string text, float size, FontStyles style)
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

    static GameObject CreateButton(GameObject parent, string name, string label, Color bgColor, float fontSize)
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

    // ── RectTransform 헬퍼 ───────────────────────────────────

    /// <summary>전체 스트레치 (오프셋 지정)</summary>
    static void Stretch(GameObject go, float left, float bottom, float right, float top)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left,   bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>부모 상단에 고정, 전체 너비</summary>
    static void AnchorTop(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -height);
        rt.offsetMax = new Vector2(0, 0);
    }

    /// <summary>부모 하단에 고정, 전체 너비</summary>
    static void AnchorBottom(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, height);
    }

    /// <summary>부모 내부 상단에서 offsetFromTop 아래에 height 만큼</summary>
    static void AnchorTopInside(GameObject go, float height, float offsetFromTop)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -(offsetFromTop + height));
        rt.offsetMax = new Vector2(0, -offsetFromTop);
    }

    /// <summary>부모 내부 하단에서 offsetFromBottom 위에 height 만큼</summary>
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
    }
}
