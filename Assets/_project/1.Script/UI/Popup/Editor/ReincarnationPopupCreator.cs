using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  ReincarnationPopupCreator.cs
//  Tools > Project K > Popup > Create ReincarnationPopup Prefab
//
//  레이아웃 (1000 × 1080, center = 0,0)
//  ─────────────────────────────────────────────────────────
//  TitleText "패배"       Y= 488  H= 80
//  SubText   웨이브/처치  Y= 412  H= 44
//  StatsText 총피해/DPS   Y= 360  H= 40
//  AbilityScrollView      Y= 285  H= 80   아이콘 HScroll
//  TopDivider             Y= 222  H=  2
//  TabBar 딜/탱/힐        Y= 199  H= 40
//  GeneralScrollView      Y= -76  H=489   장수 VScroll
//  BottomDivider          Y=-340  H=  2
//  PointsPanel            Y=-370  H= 48
//  ReincarnateButton      Y=-470  H=120
// ============================================================

public static class ReincarnationPopupCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI";

    [MenuItem("Tools/Project K/Popup/Create GeneralStatRow Prefab")]
    public static void CreateGeneralStatRowPrefab()
    {
        const float rowH   = 92f;
        const float portSz = 72f;
        // 좌측 고정 영역 끝 (portrait + name group)
        const float leftEnd   = 230f;
        // 우측 고정 영역 (TotalText 만 존재)
        const float rightSize = 110f;
        float stretchX = (leftEnd - rightSize) / 2f;  // = 60

        var root    = new GameObject("GeneralStatRow", typeof(RectTransform));
        var rowComp = root.AddComponent<GeneralStatRowUI>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, rowH);
        root.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.19f, 0.90f);

        // ── 초상화 ──────────────────────────────────────────────
        var portBg = new GameObject("PortraitBg", typeof(RectTransform), typeof(Image));
        portBg.transform.SetParent(root.transform, false);
        portBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.35f, 1f);
        var portRt = portBg.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0f, 0.5f); portRt.anchorMax = new Vector2(0f, 0.5f);
        portRt.pivot = new Vector2(0f, 0.5f);
        portRt.anchoredPosition = new Vector2(6f, 0f);
        portRt.sizeDelta = new Vector2(portSz, portSz);

        var portImg = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portImg.transform.SetParent(portBg.transform, false);
        portImg.GetComponent<Image>().color = Color.white;
        StretchRT(portImg);

        var bridgeGo = new GameObject("PortraitBridge", typeof(RectTransform));
        bridgeGo.transform.SetParent(portBg.transform, false);
        bridgeGo.SetActive(false);
        bridgeGo.AddComponent<UnitAppearanceBridge>();
        bridgeGo.AddComponent<Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts.CharacterBuilder>();

        // ── 이름 텍스트 ─────────────────────────────────────────
        var nameText = AddTMP(root, "NameText", "장수 이름", 34f, FontStyles.Bold);
        nameText.alignment    = TextAlignmentOptions.MidlineLeft;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        var nameRt = nameText.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 0.5f); nameRt.anchorMax = new Vector2(0f, 0.5f);
        nameRt.pivot = new Vector2(0f, 0.5f);
        nameRt.anchoredPosition = new Vector2(84f, 10f);
        nameRt.sizeDelta = new Vector2(140f, 44f);

        // ── StatBar (수평 stretch) ──────────────────────────────
        var barGo = new GameObject("StatBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(root.transform, false);
        barGo.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.32f, 1f);
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0.5f); barRt.anchorMax = new Vector2(1f, 0.5f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.anchoredPosition = new Vector2(stretchX, 18f);
        barRt.sizeDelta = new Vector2(-(leftEnd + rightSize), 22f);

        var statBarComp = barGo.AddComponent<StatBarUI>();
        var segs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var seg = new GameObject($"Seg{i}", typeof(RectTransform), typeof(Image));
            seg.transform.SetParent(barGo.transform, false);
            var segRt = seg.GetComponent<RectTransform>();
            segRt.anchorMin = new Vector2(0f, 0f); segRt.anchorMax = new Vector2(0f, 1f);
            segRt.offsetMin = Vector2.zero; segRt.offsetMax = Vector2.zero;
            segs[i] = seg.GetComponent<Image>();
            segs[i].color = Color.clear;
            seg.SetActive(false);
        }
        var sbSo = new SerializedObject(statBarComp);
        sbSo.FindProperty("_barBg").objectReferenceValue = barGo.GetComponent<RectTransform>();
        var segsProp = sbSo.FindProperty("_segments");
        segsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            segsProp.GetArrayElementAtIndex(i).objectReferenceValue = segs[i];
        sbSo.ApplyModifiedProperties();

        // ── 범례 텍스트 ─────────────────────────────────────────
        var legendText = AddTMP(root, "LegendText",
            "<color=#4D8CF2>■</color> 장군  <color=#59CC74>■</color> 병사  <color=#F28C33>■</color> 스킬",
            30f, FontStyles.Normal);
        legendText.alignment = TextAlignmentOptions.MidlineLeft;
        legendText.color     = new Color(0.60f, 0.62f, 0.72f);
        var legendRt = legendText.rectTransform;
        legendRt.anchorMin = new Vector2(0f, 0.5f); legendRt.anchorMax = new Vector2(1f, 0.5f);
        legendRt.pivot = new Vector2(0.5f, 0.5f);
        legendRt.anchoredPosition = new Vector2(stretchX, -14f);
        legendRt.sizeDelta = new Vector2(-(leftEnd + rightSize), 30f);

        // ── TotalText (우측 고정) ───────────────────────────────
        var totalText = AddTMP(root, "TotalText", "", UIScale.FontSm, FontStyles.Normal);
        totalText.alignment = TextAlignmentOptions.MidlineRight;
        totalText.color     = new Color(0.75f, 0.80f, 1.00f, 1f);
        var totalRt = totalText.rectTransform;
        totalRt.anchorMin = new Vector2(1f, 0.5f); totalRt.anchorMax = new Vector2(1f, 0.5f);
        totalRt.pivot = new Vector2(1f, 0.5f);
        totalRt.anchoredPosition = new Vector2(-10f, 10f);
        totalRt.sizeDelta = new Vector2(100f, 26f);

        // ── DPS 텍스트 (TotalText 아래, 우측 고정) ──────────────
        var dpsText = AddTMP(root, "DPSText", "", UIScale.FontSm, FontStyles.Normal);
        dpsText.alignment = TextAlignmentOptions.MidlineRight;
        dpsText.color     = new Color(0.55f, 0.65f, 0.85f);
        var dpsRt = dpsText.rectTransform;
        dpsRt.anchorMin = new Vector2(1f, 0.5f); dpsRt.anchorMax = new Vector2(1f, 0.5f);
        dpsRt.pivot = new Vector2(1f, 0.5f);
        dpsRt.anchoredPosition = new Vector2(-10f, -16f);
        dpsRt.sizeDelta = new Vector2(100f, 26f);

        // ── 필드 연결 ────────────────────────────────────────────
        var so = new SerializedObject(rowComp);
        SetObj(so, "_portraitBg",     portBg.GetComponent<Image>());
        SetObj(so, "_portraitImage",  portImg.GetComponent<Image>());
        SetObj(so, "_portraitBridge", bridgeGo.GetComponent<UnitAppearanceBridge>());
        SetObj(so, "_nameText",       nameText);
        SetObj(so, "_statBar",        statBarComp);
        SetObj(so, "_totalText",      totalText);
        SetObj(so, "_legendText",     legendText);
        SetObj(so, "_dpsText",        dpsText);
        so.ApplyModifiedProperties();

        Save(root, "GeneralStatRow");
    }

    // ── ReincarnationPopup ────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create ReincarnationPopup Prefab")]
    public static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/GeneralStatRow.prefab") == null)
            CreateGeneralStatRowPrefab();

        var root  = CreateRoot<ReincarnationPopup>("ReincarnationPopup", 1000f, 1080f);
        var popup = root.GetComponent<ReincarnationPopup>();
        AddBgPanel(root, new Color(0.07f, 0.08f, 0.14f, 0.97f));

        // ── 제목 ─────────────────────────────────────────────
        var titleText = AddTMP(root, "TitleText", "패배", UIScale.FontXl, FontStyles.Bold);
        titleText.color = new Color(1f, 0.30f, 0.30f);
        SetRect(titleText.rectTransform, new Vector2(0f, 488f), new Vector2(900f, 80f));

        // ── 부제 (웨이브/처치) ────────────────────────────────
        var subText = AddTMP(root, "SubText", "웨이브 0 / 0  ·  처치 0명", UIScale.FontSm, FontStyles.Normal);
        subText.color = new Color(0.75f, 0.78f, 0.90f);
        SetRect(subText.rectTransform, new Vector2(0f, 412f), new Vector2(900f, 44f));

        // ── 통계 (총 피해 / DPS) ──────────────────────────────
        var statsText = AddTMP(root, "StatsText", "총 피해  0  |  DPS  0", UIScale.FontSm, FontStyles.Normal);
        statsText.color = new Color(0.85f, 0.88f, 1f);
        SetRect(statsText.rectTransform, new Vector2(0f, 360f), new Vector2(800f, 40f));

        // ── 어빌리티 아이콘 HScrollView ───────────────────────
        var abilityScroll = new GameObject("AbilityScrollView",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        abilityScroll.transform.SetParent(root.transform, false);
        abilityScroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        SetRect(abilityScroll.GetComponent<RectTransform>(), new Vector2(0f, 285f), new Vector2(960f, 80f));

        var abilityVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        abilityVp.transform.SetParent(abilityScroll.transform, false);
        abilityVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        abilityVp.GetComponent<Mask>().showMaskGraphic = false;
        var avpRt = abilityVp.GetComponent<RectTransform>();
        avpRt.anchorMin = Vector2.zero; avpRt.anchorMax = Vector2.one;
        avpRt.offsetMin = avpRt.offsetMax = Vector2.zero;

        var abilityArea = new GameObject("AbilityArea", typeof(RectTransform));
        abilityArea.transform.SetParent(abilityVp.transform, false);
        var aaRt = abilityArea.GetComponent<RectTransform>();
        aaRt.anchorMin = new Vector2(0.5f, 0f); aaRt.anchorMax = new Vector2(0.5f, 1f);
        aaRt.pivot = new Vector2(0.5f, 0.5f);
        aaRt.anchoredPosition = Vector2.zero; aaRt.sizeDelta = Vector2.zero;
        var aaHlg = abilityArea.AddComponent<HorizontalLayoutGroup>();
        aaHlg.spacing = 6f;
        aaHlg.childAlignment = TextAnchor.MiddleCenter;
        aaHlg.childControlWidth  = false; aaHlg.childControlHeight  = false;
        aaHlg.childForceExpandWidth = false; aaHlg.childForceExpandHeight = false;
        aaHlg.padding = new RectOffset(4, 4, 0, 0);
        abilityArea.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        var aaSr = abilityScroll.GetComponent<ScrollRect>();
        aaSr.horizontal = true; aaSr.vertical = false;
        aaSr.movementType = ScrollRect.MovementType.Elastic;
        aaSr.viewport = avpRt; aaSr.content = aaRt;

        // ── 구분선 (상단) ─────────────────────────────────────
        var topDiv = new GameObject("TopDivider", typeof(RectTransform), typeof(Image));
        topDiv.transform.SetParent(root.transform, false);
        topDiv.GetComponent<Image>().color = new Color(0.30f, 0.32f, 0.45f);
        SetRect(topDiv.GetComponent<RectTransform>(), new Vector2(0f, 222f), new Vector2(940f, 2f));

        // ── 탭 바 (딜/탱/힐) ──────────────────────────────────
        var tabBar = new GameObject("TabBar", typeof(RectTransform));
        tabBar.transform.SetParent(root.transform, false);
        SetRect(tabBar.GetComponent<RectTransform>(), new Vector2(0f, 199f), new Vector2(940f, 40f));
        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing = 6f;
        tabHlg.childAlignment = TextAnchor.MiddleCenter;
        tabHlg.childControlWidth  = true; tabHlg.childControlHeight  = true;
        tabHlg.childForceExpandWidth = true; tabHlg.childForceExpandHeight = true;

        string[] tabLabels   = { "딜", "탱", "힐" };
        var tabButtons   = new Button[3];
        var tabButtonBgs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var tbGo = new GameObject($"TabBtn{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            tbGo.transform.SetParent(tabBar.transform, false);
            var tbImg = tbGo.GetComponent<Image>();
            tbImg.color = i == 0
                ? new Color(0.25f, 0.45f, 0.85f)
                : new Color(0.15f, 0.18f, 0.28f);

            var tbLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            tbLabel.transform.SetParent(tbGo.transform, false);
            var tbLabelRt = tbLabel.GetComponent<RectTransform>();
            tbLabelRt.anchorMin = Vector2.zero; tbLabelRt.anchorMax = Vector2.one;
            tbLabelRt.offsetMin = tbLabelRt.offsetMax = Vector2.zero;
            var tbTmp = tbLabel.GetComponent<TextMeshProUGUI>();
            tbTmp.text = tabLabels[i]; tbTmp.fontSize = UIScale.FontSm;
            tbTmp.fontStyle = FontStyles.Bold;
            tbTmp.alignment = TextAlignmentOptions.Center; tbTmp.color = Color.white;

            tabButtons[i]   = tbGo.GetComponent<Button>();
            tabButtonBgs[i] = tbImg;
        }

        // ── 장수 통계 VScrollView ──────────────────────────────
        // top=169 (10px below TabBar bottom 179), bottom=-320
        // center = (169 + (-320))/2 = -75.5 → Y=-76, H=489
        var generalScroll = new GameObject("GeneralScrollView",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        generalScroll.transform.SetParent(root.transform, false);
        generalScroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        SetRect(generalScroll.GetComponent<RectTransform>(), new Vector2(0f, -76f), new Vector2(960f, 489f));

        var generalVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        generalVp.transform.SetParent(generalScroll.transform, false);
        generalVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        generalVp.GetComponent<Mask>().showMaskGraphic = false;
        var gvpRt = generalVp.GetComponent<RectTransform>();
        gvpRt.anchorMin = Vector2.zero; gvpRt.anchorMax = Vector2.one;
        gvpRt.offsetMin = gvpRt.offsetMax = Vector2.zero;

        var generalContent = new GameObject("GeneralContent", typeof(RectTransform));
        generalContent.transform.SetParent(generalVp.transform, false);
        var gcRt = generalContent.GetComponent<RectTransform>();
        gcRt.anchorMin = new Vector2(0f, 1f); gcRt.anchorMax = new Vector2(1f, 1f);
        gcRt.pivot = new Vector2(0.5f, 1f);
        gcRt.anchoredPosition = Vector2.zero; gcRt.sizeDelta = Vector2.zero;
        var gcVlg = generalContent.AddComponent<VerticalLayoutGroup>();
        gcVlg.spacing = 6f;
        gcVlg.padding = new RectOffset(8, 8, 6, 6);
        gcVlg.childControlWidth  = true; gcVlg.childControlHeight  = false;
        gcVlg.childForceExpandWidth = true; gcVlg.childForceExpandHeight = false;
        gcVlg.childAlignment = TextAnchor.UpperCenter;
        generalContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var gSr = generalScroll.GetComponent<ScrollRect>();
        gSr.horizontal = false; gSr.vertical = true;
        gSr.movementType = ScrollRect.MovementType.Elastic;
        gSr.viewport = gvpRt; gSr.content = gcRt;

        // ── 구분선 (하단) ─────────────────────────────────────
        var botDiv = new GameObject("BottomDivider", typeof(RectTransform), typeof(Image));
        botDiv.transform.SetParent(root.transform, false);
        botDiv.GetComponent<Image>().color = new Color(0.30f, 0.32f, 0.45f);
        SetRect(botDiv.GetComponent<RectTransform>(), new Vector2(0f, -340f), new Vector2(940f, 2f));

        // ── 포인트 패널 ───────────────────────────────────────
        var (currentPtsTmp, earnPtsTmp, totalPtsTmp) = BuildPointsPanel(root);

        // ── 환생 버튼 ─────────────────────────────────────────
        var btnGo = AddButton(root, "ReincarnateButton", "환생", new Color(0.55f, 0.18f, 0.78f), UIScale.FontLg);
        SetRect(btnGo.GetComponent<RectTransform>(), new Vector2(0f, -470f), new Vector2(600f, 120f));
        var reincBtn = btnGo.GetComponent<Button>();

        // ── 장수 Row 프리팹 로드 ─────────────────────────────
        var generalRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/GeneralStatRow.prefab");

        // ── SerializedObject 연결 ────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",            (int)PopupType.Reincarnation);
        SetObj (so, "_subText",              subText);
        SetObj (so, "_statsText",            statsText);
        SetObj (so, "_abilityIconContent",   abilityArea.transform);
        SetObj (so, "_generalArea",          generalContent.transform);
        SetObj (so, "_currentPtsText",       currentPtsTmp);
        SetObj (so, "_earnPtsText",          earnPtsTmp);
        SetObj (so, "_totalPtsText",         totalPtsTmp);
        SetObj (so, "_reincarnateBtn",       reincBtn);
        if (generalRowPrefab != null)
            SetObj(so, "_generalRowTemplate", generalRowPrefab.GetComponent<GeneralStatRowUI>());

        var tabBtnsProp = so.FindProperty("_tabButtons");
        if (tabBtnsProp != null)
        {
            tabBtnsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                tabBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtons[i];
        }
        var tabBgsProp = so.FindProperty("_tabButtonBgs");
        if (tabBgsProp != null)
        {
            tabBgsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                tabBgsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtonBgs[i];
        }

        so.ApplyModifiedProperties();

        Save(root, "ReincarnationPopup");
    }

    // ── 포인트 패널 빌드 ──────────────────────────────────────

    static (TextMeshProUGUI current, TextMeshProUGUI earn, TextMeshProUGUI total) BuildPointsPanel(GameObject parent)
    {
        var panel = new GameObject("PointsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent.transform, false);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.20f, 0.90f);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0f, -370f), new Vector2(940f, 48f));

        var currentTmp = AddTMP(panel, "CurrentPtsText", "보유  0 pt", UIScale.FontSm, FontStyles.Normal);
        currentTmp.alignment = TextAlignmentOptions.MidlineLeft;
        currentTmp.color     = new Color(0.75f, 0.78f, 0.90f);
        SetRect(currentTmp.rectTransform, new Vector2(-280f, 0f), new Vector2(240f, 44f));

        var earnTmp = AddTMP(panel, "EarnPtsText", "+0 pt", UIScale.FontSm, FontStyles.Bold);
        earnTmp.alignment = TextAlignmentOptions.Center;
        earnTmp.color     = new Color(0.95f, 0.85f, 0.25f);
        SetRect(earnTmp.rectTransform, new Vector2(0f, 0f), new Vector2(200f, 44f));

        var totalTmp = AddTMP(panel, "TotalPtsText", "환생 후  0 pt", UIScale.FontSm, FontStyles.Bold);
        totalTmp.alignment = TextAlignmentOptions.MidlineRight;
        totalTmp.color     = new Color(0.60f, 0.90f, 0.65f);
        SetRect(totalTmp.rectTransform, new Vector2(280f, 0f), new Vector2(240f, 44f));

        return (currentTmp, earnTmp, totalTmp);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    static GameObject CreateRoot<T>(string name, float w, float h) where T : PopupBase
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<CanvasGroup>();
        go.AddComponent<T>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    static void AddBgPanel(GameObject parent, Color color)
    {
        var go = new GameObject("BgPanel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.transform.SetAsFirstSibling();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
    }

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text, float size, FontStyles style)
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

    static GameObject AddButton(GameObject parent, string objName, string label, Color bgColor, float fontSize)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static void StretchRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void SetEnum(SerializedObject so, string field, int value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.intValue = value;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
    }

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SavePath}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ReincarnationPopupCreator] 저장 완료 → {path}");
    }
}
