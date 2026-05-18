using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  PopupPrefabCreator.cs
//  Tools > Project K > Popup > Create Popup Prefabs
//  BattleResultPopup / PausePopup / LoadingPopup / ExpRow 프리팹 자동 생성.
//  크기·폰트는 UIScale 상수를 참조한다.
// ============================================================

public static class PopupPrefabCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI";

    [MenuItem("Tools/Project K/Popup/Create Popup Prefabs")]
    static void CreateAll()
    {
        CreateExpRowPrefab();
        CreateBattleResultPopup();
        CreatePausePopup();
        CreateLoadingPopup();
        CreateAbilitySelectPopup();
        CreateAbilityListPopup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopupPrefabCreator] 팝업 프리팹 생성 완료");
    }

    // ── ExpRow 프리팹 ─────────────────────────────────────────
    // 한 줄 HLG 레이아웃:
    //   [Portrait 56px] [Name 80px] [StatBar flex] [TotalText 80px]
    //   [ExpText 70px] [LevelText 52px] [LevelUpText 70px]

    [MenuItem("Tools/Project K/Popup/Create ExpRow Prefab")]
    static void CreateExpRowPrefab()
    {
        const float rowH       = 80f;
        const float portraitSz = 64f;

        var root = new GameObject("ExpRow", typeof(RectTransform));
        var rowComp = root.AddComponent<ExpRowUI>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, rowH);
        root.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.19f, 0.90f);

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 4f;
        hlg.padding              = new RectOffset(6, 6, 4, 4);
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childControlHeight   = true;
        hlg.childControlWidth    = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // ── 초상화 영역 ─────────────────────────────────────────
        var portBg = new GameObject("PortraitBg", typeof(RectTransform), typeof(Image));
        portBg.transform.SetParent(root.transform, false);
        portBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.35f, 1f);
        AddLE(portBg, portraitSz, portraitSz);
        portBg.GetComponent<RectTransform>().sizeDelta = new Vector2(portraitSz, portraitSz);

        var portImg = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portImg.transform.SetParent(portBg.transform, false);
        portImg.GetComponent<Image>().color = Color.white;
        StretchRT(portImg);

        var bridgeGo = new GameObject("PortraitBridge", typeof(RectTransform));
        bridgeGo.transform.SetParent(portBg.transform, false);
        bridgeGo.SetActive(false);
        bridgeGo.AddComponent<UnitAppearanceBridge>();
        bridgeGo.AddComponent<Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts.CharacterBuilder>();

        // ── 이름 + 레벨 (VLG 묶음) ───────────────────────────────
        var nameGroup = new GameObject("NameGroup", typeof(RectTransform));
        nameGroup.transform.SetParent(root.transform, false);
        nameGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, rowH);
        AddLE(nameGroup, 100f, 80f);

        var ngVlg = nameGroup.AddComponent<VerticalLayoutGroup>();
        ngVlg.spacing              = 2f;
        ngVlg.childAlignment       = TextAnchor.MiddleLeft;
        ngVlg.childControlWidth    = true;
        ngVlg.childControlHeight   = false;
        ngVlg.childForceExpandWidth  = true;
        ngVlg.childForceExpandHeight = false;

        var nameText = AddTMP(nameGroup, "NameText", "영웅 이름", UIScale.FontMd, FontStyles.Bold);
        nameText.alignment    = TextAlignmentOptions.MidlineLeft;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, UIScale.FontMd + 6f);

        var levelText = AddTMP(nameGroup, "LevelText", "Lv.1", UIScale.FontSm, FontStyles.Normal);
        levelText.alignment = TextAlignmentOptions.MidlineLeft;
        levelText.color     = new Color(0.60f, 0.62f, 0.75f);
        levelText.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, UIScale.FontSm + 4f);

        // ── StatBar (가변 너비 — 나머지 공간 채움) ──────────────
        var barGo = new GameObject("StatBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(root.transform, false);
        barGo.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.32f, 1f);
        barGo.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 22f);
        var barLE = barGo.AddComponent<LayoutElement>();
        barLE.flexibleWidth = 1f;
        barLE.minWidth      = 80f;

        var statBarComp = barGo.AddComponent<StatBarUI>();
        var segs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var seg = new GameObject($"Seg{i}", typeof(RectTransform), typeof(Image));
            seg.transform.SetParent(barGo.transform, false);
            var segRt = seg.GetComponent<RectTransform>();
            segRt.anchorMin = new Vector2(0f, 0f); segRt.anchorMax = new Vector2(0f, 1f);
            segRt.offsetMin = Vector2.zero;         segRt.offsetMax = Vector2.zero;
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

        // ── TotalText (80px 고정) ─────────────────────────────────
        var totalText = AddTMP(root, "TotalText", "", UIScale.FontSm, FontStyles.Normal);
        totalText.alignment = TextAlignmentOptions.MidlineLeft;
        totalText.color     = new Color(0.75f, 0.80f, 1.00f, 1f);
        AddLE(totalText.gameObject, 80f, 50f);

        // ── EXP 텍스트 (70px 고정) ────────────────────────────────
        var expText = AddTMP(root, "ExpText", "+0 EXP", UIScale.FontSm, FontStyles.Normal);
        expText.alignment = TextAlignmentOptions.MidlineLeft;
        expText.color     = new Color(0.60f, 0.90f, 0.60f);
        AddLE(expText.gameObject, 70f, 50f);

        // ── 레벨업 텍스트 (70px, 기본 비활성) ─────────────────────
        var lvupText = AddTMP(root, "LevelUpText", "▲UP!", UIScale.FontSm, FontStyles.Bold);
        lvupText.alignment = TextAlignmentOptions.MidlineLeft;
        lvupText.color     = new Color(1.00f, 0.85f, 0.15f);
        AddLE(lvupText.gameObject, 70f, 50f);
        lvupText.gameObject.SetActive(false);

        // ── 필드 연결 ─────────────────────────────────────────────
        var so = new SerializedObject(rowComp);
        SetObj(so, "_portraitBg",     portBg.GetComponent<Image>());
        SetObj(so, "_portraitImage",  portImg.GetComponent<Image>());
        SetObj(so, "_portraitBridge", bridgeGo.GetComponent<UnitAppearanceBridge>());
        SetObj(so, "_nameText",       nameText);
        SetObj(so, "_levelText",      levelText);
        SetObj(so, "_expText",        expText);
        SetObj(so, "_levelUpText",    lvupText);
        SetObj(so, "_statBar",        statBarComp);
        SetObj(so, "_totalText",      totalText);
        so.ApplyModifiedProperties();

        Save(root, "ExpRow");
    }

    static void AddLE(GameObject go, float preferred, float min)
    {
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = preferred;
        le.minWidth       = min;
        le.flexibleWidth  = 0f;
    }

    static void StretchRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // ── BattleResultPopup ─────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Rebuild BattleResult Popup")]
    public static void CreateBattleResultPopup()
    {
        // ExpRow 프리팹이 없으면 먼저 생성
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/ExpRow.prefab") == null)
            CreateExpRowPrefab();

        // 레이아웃 (위 → 아래):
        //   승리! / 서브 / 스탯 → 보상 카드(스크롤) → 힌트 → 구분선 → 탭바 → EXP 행 목록 → 확인 버튼
        var root  = CreateRoot<BattleResultPopup>("BattleResultPopup", 900, 1100);
        var popup = root.GetComponent<BattleResultPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        var resultText = AddTMP(root, "ResultText", "승리!", UIScale.FontXl, FontStyles.Bold);
        var subText    = AddTMP(root, "SubText", "모든 적을 물리쳤습니다!", UIScale.FontSm, FontStyles.Normal);
        var statsText  = AddTMP(root, "StatsText", "처치  0   |   웨이브  0 / 0", UIScale.FontSm, FontStyles.Normal);

        SetRect(resultText.rectTransform, new Vector2(0,  480), new Vector2(800, 90));
        SetRect(subText.rectTransform,    new Vector2(0,  400), new Vector2(800, 50));
        SetRect(statsText.rectTransform,  new Vector2(0,  342), new Vector2(700, 50));

        // ── 보상 카드 스크롤뷰 (5개 이상 카드를 수평 스크롤로 지원) ──
        var rewardScroll = new GameObject("RewardScrollView",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        rewardScroll.transform.SetParent(root.transform, false);
        rewardScroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        SetRect(rewardScroll.GetComponent<RectTransform>(), new Vector2(0f, 250f), new Vector2(860f, 150f));

        var rewardVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        rewardVp.transform.SetParent(rewardScroll.transform, false);
        rewardVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        rewardVp.GetComponent<Mask>().showMaskGraphic = false;
        var rewardVpRt = rewardVp.GetComponent<RectTransform>();
        rewardVpRt.anchorMin = Vector2.zero; rewardVpRt.anchorMax = Vector2.one;
        rewardVpRt.offsetMin = rewardVpRt.offsetMax = Vector2.zero;

        var rewardArea = new GameObject("RewardArea", typeof(RectTransform));
        rewardArea.transform.SetParent(rewardVp.transform, false);
        var rewardRt = rewardArea.GetComponent<RectTransform>();
        rewardRt.anchorMin        = new Vector2(0f, 0f);
        rewardRt.anchorMax        = new Vector2(0f, 1f);
        rewardRt.pivot            = new Vector2(0f, 0.5f);
        rewardRt.anchoredPosition = Vector2.zero;
        rewardRt.sizeDelta        = new Vector2(0f, 0f);
        var rewardHlg = rewardArea.AddComponent<HorizontalLayoutGroup>();
        rewardHlg.spacing              = 8f;
        rewardHlg.childAlignment       = TextAnchor.MiddleCenter;
        rewardHlg.childControlWidth    = false;
        rewardHlg.childControlHeight   = false;
        rewardHlg.childForceExpandWidth  = false;
        rewardHlg.childForceExpandHeight = false;
        rewardHlg.padding              = new RectOffset(4, 4, 0, 0);
        rewardArea.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var rewardScrollRect = rewardScroll.GetComponent<ScrollRect>();
        rewardScrollRect.horizontal   = true;
        rewardScrollRect.vertical     = false;
        rewardScrollRect.movementType = ScrollRect.MovementType.Elastic;
        rewardScrollRect.viewport     = rewardVpRt;
        rewardScrollRect.content      = rewardRt;

        // 힌트 텍스트
        var hintText = AddTMP(root, "HintText", "카드를 탭하면 개봉합니다", UIScale.FontSm, FontStyles.Italic);
        hintText.color = new Color(0.70f, 0.70f, 0.50f);
        SetRect(hintText.rectTransform, new Vector2(0, 158f), new Vector2(800f, 44f));
        hintText.gameObject.SetActive(false);

        // ── 구분선 ──────────────────────────────────────────────
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(root.transform, false);
        divider.GetComponent<Image>().color = new Color(0.30f, 0.30f, 0.35f, 0.80f);
        SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 122f), new Vector2(840f, 2f));

        // ── 탭 바 (딜 / 탱 / 힐) ──────────────────────────────────
        var tabBar = new GameObject("TabBar", typeof(RectTransform));
        tabBar.transform.SetParent(root.transform, false);
        SetRect(tabBar.GetComponent<RectTransform>(), new Vector2(0f, 91f), new Vector2(840f, 36f));

        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing              = 6f;
        tabHlg.childAlignment       = TextAnchor.MiddleCenter;
        tabHlg.childControlWidth    = true;
        tabHlg.childControlHeight   = true;
        tabHlg.childForceExpandWidth  = true;
        tabHlg.childForceExpandHeight = true;

        string[] tabLabels   = { "딜", "탱", "힐" };
        var tabButtons   = new Button[3];
        var tabButtonBgs = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            var tabBtnGo = new GameObject($"TabBtn{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            tabBtnGo.transform.SetParent(tabBar.transform, false);
            var tabImg = tabBtnGo.GetComponent<Image>();
            tabImg.color = i == 0
                ? new Color(0.25f, 0.45f, 0.85f)
                : new Color(0.15f, 0.18f, 0.28f);

            var tabLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            tabLabelGo.transform.SetParent(tabBtnGo.transform, false);
            var tabLabelRt = tabLabelGo.GetComponent<RectTransform>();
            tabLabelRt.anchorMin = Vector2.zero;
            tabLabelRt.anchorMax = Vector2.one;
            tabLabelRt.offsetMin = Vector2.zero;
            tabLabelRt.offsetMax = Vector2.zero;
            var tabTmp = tabLabelGo.GetComponent<TextMeshProUGUI>();
            tabTmp.text      = tabLabels[i];
            tabTmp.fontSize  = UIScale.FontSm;
            tabTmp.fontStyle = FontStyles.Bold;
            tabTmp.alignment = TextAlignmentOptions.Center;
            tabTmp.color     = Color.white;

            tabButtons[i]   = tabBtnGo.GetComponent<Button>();
            tabButtonBgs[i] = tabImg;
        }

        // ── EXP 행 목록 (승리 시 영웅별 경험치 + 전투 통계 표시) ──────
        var expArea = new GameObject("ExpArea", typeof(RectTransform));
        expArea.transform.SetParent(root.transform, false);
        var expAreaRt = expArea.GetComponent<RectTransform>();
        expAreaRt.anchorMin        = new Vector2(0f, 1f);
        expAreaRt.anchorMax        = new Vector2(1f, 1f);
        expAreaRt.pivot            = new Vector2(0.5f, 1f);
        expAreaRt.anchoredPosition = new Vector2(0f, -488f);
        expAreaRt.sizeDelta        = new Vector2(0f, 440f);
        var expVlg = expArea.AddComponent<VerticalLayoutGroup>();
        expVlg.spacing              = 6f;
        expVlg.padding              = new RectOffset(8, 8, 6, 6);
        expVlg.childControlWidth    = true;
        expVlg.childControlHeight   = false;
        expVlg.childForceExpandWidth  = true;
        expVlg.childForceExpandHeight = false;
        expVlg.childAlignment       = TextAnchor.UpperCenter;
        expArea.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 확인 버튼
        var confirmBtn = AddButton(root, "ConfirmButton", "확인", new Color(0.20f, 0.55f, 0.20f), UIScale.FontMd);
        SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0, -500f), new Vector2(400f, UIScale.BtnMd));

        // 프리팹 로드 및 연결
        var rewardCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/RewardCard.prefab");
        var expRowPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/ExpRow.prefab");

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",       (int)PopupType.BattleResult);
        SetObj (so, "_resultText",      resultText);
        SetObj (so, "_subText",         subText);
        SetObj (so, "_statsText",       statsText);
        SetObj (so, "_rewardArea",      rewardArea.GetComponent<Transform>());
        SetObj (so, "_hintText",        hintText);
        SetObj (so, "_expArea",         expArea.GetComponent<Transform>());
        SetObj (so, "_confirmButton",   confirmBtn.GetComponent<Button>());
        if (rewardCardPrefab != null)
            SetObj(so, "_rewardCardPrefab", rewardCardPrefab.GetComponent<RewardCardUI>());
        if (expRowPrefab != null)
            SetObj(so, "_expRowPrefab",   expRowPrefab.GetComponent<ExpRowUI>());

        var tabButtonsProp = so.FindProperty("_tabButtons");
        if (tabButtonsProp != null)
        {
            tabButtonsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                tabButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtons[i];
        }

        var tabButtonBgsProp = so.FindProperty("_tabButtonBgs");
        if (tabButtonBgsProp != null)
        {
            tabButtonBgsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
                tabButtonBgsProp.GetArrayElementAtIndex(i).objectReferenceValue = tabButtonBgs[i];
        }

        so.ApplyModifiedProperties();

        Save(root, "BattleResultPopup");
    }

    // ── PausePopup ────────────────────────────────────────────

    static void CreatePausePopup()
    {
        // 버튼 3개 + 여백 기준으로 높이 산출
        float btnH   = UIScale.BtnSm;
        float gap    = 24f;
        float totalH = btnH * 3 + gap * 2;           // 3버튼 높이
        float popupH = totalH + UIScale.FontLg + 120; // 제목 + 상하 여백

        var root  = CreateRoot<PausePopup>("PausePopup", 720, popupH);
        var popup = root.GetComponent<PausePopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        var title = AddTMP(root, "TitleText", "일시 정지", UIScale.FontLg, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0, popupH / 2 - 80), new Vector2(640, 70));

        // 버튼 3개: 중앙 기준 위→아래
        float btnStep = btnH + gap;
        float btn1Y   =  btnStep;
        float btn2Y   =  0;
        float btn3Y   = -btnStep;

        var resumeBtn  = AddButton(root, "ResumeButton",  "계속하기",  new Color(0.20f, 0.55f, 0.20f), UIScale.FontMd);
        var restartBtn = AddButton(root, "RestartButton", "다시 시작", new Color(0.55f, 0.45f, 0.10f), UIScale.FontMd);
        var quitBtn    = AddButton(root, "QuitButton",    "종료",      new Color(0.55f, 0.15f, 0.15f), UIScale.FontMd);

        SetRect(resumeBtn .GetComponent<RectTransform>(), new Vector2(0, btn1Y), new Vector2(560, btnH));
        SetRect(restartBtn.GetComponent<RectTransform>(), new Vector2(0, btn2Y), new Vector2(560, btnH));
        SetRect(quitBtn   .GetComponent<RectTransform>(), new Vector2(0, btn3Y), new Vector2(560, btnH));

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",     (int)PopupType.Pause);
        SetObj (so, "_resumeButton",  resumeBtn .GetComponent<Button>());
        SetObj (so, "_restartButton", restartBtn.GetComponent<Button>());
        SetObj (so, "_quitButton",    quitBtn   .GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "PausePopup");
    }

    // ── LoadingPopup ──────────────────────────────────────────

    static void CreateLoadingPopup()
    {
        var root = new GameObject("LoadingPopup", typeof(RectTransform));
        root.AddComponent<CanvasGroup>();
        var popup = root.AddComponent<LoadingPopup>();

        // 전체 화면 스트레치
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        AddBgPanel(root, new Color(0.05f, 0.05f, 0.08f, 1f));

        var titleText  = AddTMP(root, "TitleText",  "배틀 준비 중",    UIScale.FontLg, FontStyles.Bold);
        var statusText = AddTMP(root, "StatusText", "장군 소환 중...", UIScale.FontMd, FontStyles.Normal);
        statusText.color = new Color(0.75f, 0.75f, 0.75f);

        SetRect(titleText .rectTransform, new Vector2(0,  50), new Vector2(800, 80));
        SetRect(statusText.rectTransform, new Vector2(0, -50), new Vector2(700, 60));

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",   (int)PopupType.Loading);
        SetObj (so, "_titleText",   titleText);
        SetObj (so, "_statusText",  statusText);
        so.ApplyModifiedProperties();

        Save(root, "LoadingPopup");
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
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
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

    // ── AbilitySelectPopup ────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create AbilitySelectPopup Prefab")]
    static void CreateAbilitySelectPopup()
    {
        const float popupW       = 920f;
        const float popupH       = 840f;   // 새로고침 행 추가로 높이 증가
        const float cardW        = 272f;
        const float cardH        = 580f;
        const float refreshRowH  = 64f;

        var root  = CreateRoot<AbilitySelectPopup>("AbilitySelectPopup", popupW, popupH);
        var popup = root.GetComponent<AbilitySelectPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        // 제목
        var titleTmp = AddTMP(root, "TitleText", "어빌리티 선택", UIScale.FontLg, FontStyles.Bold);
        SetRect(titleTmp.rectTransform, new Vector2(0f, popupH / 2f - 60f), new Vector2(800f, 70f));

        // ── 새로고침 행 ───────────────────────────────────────
        var refreshRow = new GameObject("RefreshRow", typeof(RectTransform));
        refreshRow.transform.SetParent(root.transform, false);
        SetRect(refreshRow.GetComponent<RectTransform>(),
            new Vector2(0f, popupH / 2f - 130f), new Vector2(popupW - 40f, refreshRowH));

        var refreshBtn = AddButton(refreshRow, "RefreshBtn", "새로고침",
            new Color(0.15f, 0.40f, 0.55f), UIScale.FontSm);
        SetRect(refreshBtn.GetComponent<RectTransform>(), new Vector2(-200f, 0f), new Vector2(220f, 56f));

        var refreshCountTmp = AddTMP(refreshRow, "RefreshCountText",
            "새로고침  0회 남음", UIScale.FontSm, FontStyles.Normal);
        refreshCountTmp.color = new Color(0.60f, 0.85f, 1.0f);
        refreshCountTmp.alignment = TextAlignmentOptions.Left;
        SetRect(refreshCountTmp.rectTransform, new Vector2(130f, 0f), new Vector2(380f, refreshRowH));

        // ── 카드 영역 (HorizontalLayoutGroup) ─────────────────
        var cardArea = new GameObject("CardArea", typeof(RectTransform));
        cardArea.transform.SetParent(root.transform, false);
        var cardAreaRt = cardArea.GetComponent<RectTransform>();
        cardAreaRt.anchorMin        = new Vector2(0.5f, 0.5f);
        cardAreaRt.anchorMax        = new Vector2(0.5f, 0.5f);
        cardAreaRt.pivot            = new Vector2(0.5f, 0.5f);
        cardAreaRt.anchoredPosition = new Vector2(0f, -60f);
        cardAreaRt.sizeDelta        = new Vector2(popupW - 40f, cardH);
        var hlg = cardArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 16f;
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.childControlWidth    = false;
        hlg.childControlHeight   = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        // 카드 3장 생성
        var cards = new AbilityCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            var card = BuildAbilityCard($"Card{i}", cardW, cardH);
            card.transform.SetParent(cardArea.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(cardW, cardH);
            cards[i] = card.GetComponent<AbilityCardUI>();
        }

        // ── SerializedObject 연결 ─────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",       (int)PopupType.AbilitySelect);
        SetObj (so, "_titleTmp",        titleTmp);
        SetObj (so, "_refreshBtn",      refreshBtn.GetComponent<Button>());
        SetObj (so, "_refreshCountTmp", refreshCountTmp);

        var cardsProp = so.FindProperty("_cards");
        cardsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];

        so.ApplyModifiedProperties();

        Save(root, "AbilitySelectPopup");
        Debug.Log("[PopupPrefabCreator] AbilitySelectPopup 저장 완료");
    }

    static GameObject BuildAbilityCard(string name, float w, float h)
    {
        var card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.AddComponent<AbilityCardUI>();
        card.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f, 1f);

        // 등급 바 (상단)
        var gradeBar = new GameObject("GradeBar", typeof(RectTransform), typeof(Image));
        gradeBar.transform.SetParent(card.transform, false);
        var gbRt = gradeBar.GetComponent<RectTransform>();
        gbRt.anchorMin        = new Vector2(0f, 1f);
        gbRt.anchorMax        = new Vector2(1f, 1f);
        gbRt.pivot            = new Vector2(0.5f, 1f);
        gbRt.anchoredPosition = Vector2.zero;
        gbRt.sizeDelta        = new Vector2(0f, 8f);
        gradeBar.GetComponent<Image>().color = new Color(0.70f, 0.70f, 0.75f);

        // 아이콘 배경
        float iconSz   = 90f;
        float iconTopY = h / 2f - 60f;  // 카드 상단에서 60 내려온 위치(중앙 기준)
        var iconBg = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconBg.transform.SetParent(card.transform, false);
        var iconBgRt = iconBg.GetComponent<RectTransform>();
        iconBgRt.anchorMin        = new Vector2(0.5f, 0.5f);
        iconBgRt.anchorMax        = new Vector2(0.5f, 0.5f);
        iconBgRt.pivot            = new Vector2(0.5f, 0.5f);
        iconBgRt.anchoredPosition = new Vector2(0f, iconTopY);
        iconBgRt.sizeDelta        = new Vector2(iconSz + 8f, iconSz + 8f);
        iconBg.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 1f);

        // 아이콘
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(card.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
        iconRt.pivot            = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0f, iconTopY);
        iconRt.sizeDelta        = new Vector2(iconSz, iconSz);
        iconGo.GetComponent<Image>().color = Color.white;

        // 등급 텍스트
        var gradeTmp = AddCardTMP(card, "GradeTmp", "일반", UIScale.FontSm, FontStyles.Normal);
        SetRect(gradeTmp.rectTransform, new Vector2(0f, iconTopY - iconSz / 2f - 22f), new Vector2(w - 20f, 36f));
        gradeTmp.color = new Color(0.70f, 0.70f, 0.75f);

        // 이름 텍스트
        var nameTmp = AddCardTMP(card, "NameTmp", "어빌리티 이름", UIScale.FontMd, FontStyles.Bold);
        SetRect(nameTmp.rectTransform, new Vector2(0f, iconTopY - iconSz / 2f - 68f), new Vector2(w - 20f, 48f));

        // 대상 텍스트
        var targetTmp = AddCardTMP(card, "TargetTmp", "전체", UIScale.FontSm, FontStyles.Normal);
        SetRect(targetTmp.rectTransform, new Vector2(0f, iconTopY - iconSz / 2f - 112f), new Vector2(w - 20f, 36f));
        targetTmp.color = new Color(0.65f, 0.80f, 0.65f);

        // 구분선
        var div = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(card.transform, false);
        div.GetComponent<Image>().color = new Color(0.30f, 0.30f, 0.38f, 0.80f);
        SetRect(div.GetComponent<RectTransform>(), new Vector2(0f, iconTopY - iconSz / 2f - 134f), new Vector2(w - 32f, 2f));

        // 스탯 설명 텍스트
        var descTmp = AddCardTMP(card, "DescTmp", "+0%", UIScale.FontSm, FontStyles.Normal);
        SetRect(descTmp.rectTransform, new Vector2(0f, iconTopY - iconSz / 2f - 188f), new Vector2(w - 24f, 80f));
        descTmp.color = new Color(0.85f, 0.85f, 0.90f);

        // 선택 버튼
        var selBtn = AddButton(card, "SelectBtn", "선택", new Color(0.20f, 0.45f, 0.75f), UIScale.FontSm);
        SetRect(selBtn.GetComponent<RectTransform>(), new Vector2(0f, -(h / 2f - 44f)), new Vector2(w - 24f, UIScale.BtnSm));

        // SerializedObject 로 AbilityCardUI 필드 연결
        var cardSo = new SerializedObject(card.GetComponent<AbilityCardUI>());
        SetObj(cardSo, "_gradeBar",  gradeBar.GetComponent<Image>());
        SetObj(cardSo, "_icon",      iconGo.GetComponent<Image>());
        SetObj(cardSo, "_gradeTmp",  gradeTmp);
        SetObj(cardSo, "_nameTmp",   nameTmp);
        SetObj(cardSo, "_targetTmp", targetTmp);
        SetObj(cardSo, "_descTmp",   descTmp);
        SetObj(cardSo, "_selectBtn", selBtn.GetComponent<Button>());
        cardSo.ApplyModifiedProperties();

        return card;
    }

    static TextMeshProUGUI AddCardTMP(GameObject parent, string name, string text, float size, FontStyles style)
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

    // ── AbilityListPopup ──────────────────────────────────────
    //  레이아웃: 좌(선택 상세 + 총 스탯 합산) / 우(보유 목록 스크롤)

    [MenuItem("Tools/Project K/Popup/Create AbilityList Popup Prefab")]
    static void CreateAbilityListPopup()
    {
        const float popupW   = 980f;
        const float popupH   = 1060f;  // 1080p 화면에 맞게 (헤더·아이콘 잘림 방지)
        const float headerH  = 100f;
        const float leftW    = 360f;
        const float detailH  = 420f;   // body=870 → detailH 420 + totalBox 450

        var root  = CreateRoot<AbilityListPopup>("AbilityListPopup", popupW, popupH);
        var popup = root.GetComponent<AbilityListPopup>();
        AddBgPanel(root, new Color(0.08f, 0.08f, 0.14f, 1f));

        // ── Header ──────────────────────────────────────────
        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(root.transform, false);
        header.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f, 1f);
        AnchorTopLocal(header, headerH, 0);

        var titleTmp = AddTMP(header, "TitleText", "보유 어빌리티", UIScale.FontMd, FontStyles.Bold);
        SetRect(titleTmp.rectTransform, new Vector2(-60f, 0), new Vector2(popupW - 220f, headerH - 20f));

        var headerCountTmp = AddTMP(header, "HeaderCountText", "0개 보유", UIScale.FontSm, FontStyles.Normal);
        headerCountTmp.color = new Color(0.50f, 0.55f, 0.70f);
        SetRect(headerCountTmp.rectTransform, new Vector2(popupW * 0.25f - 60f, 0f), new Vector2(180f, headerH - 30f));

        var closeBtn = AddButton(header, "CloseBtn", "✕", new Color(0.55f, 0.18f, 0.18f, 1f), UIScale.FontMd);
        SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(popupW * 0.5f - 50f, 0f), new Vector2(80f, 70f));

        // ── Body (header 아래 ~ 하단, stretch) ──────────────
        var body = new GameObject("Body", typeof(RectTransform));
        body.transform.SetParent(root.transform, false);
        var bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero; bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(0f, 0f);
        bodyRt.offsetMax = new Vector2(0f, -headerH);

        // ── Left Panel (좌, 고정 너비) ────────────────────────
        var leftPanel = new GameObject("LeftPanel", typeof(RectTransform), typeof(Image));
        leftPanel.transform.SetParent(body.transform, false);
        leftPanel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f, 1f);
        var lpRt = leftPanel.GetComponent<RectTransform>();
        lpRt.anchorMin = new Vector2(0f, 0f); lpRt.anchorMax = new Vector2(0f, 1f);
        lpRt.pivot = new Vector2(0f, 0.5f);
        lpRt.anchoredPosition = Vector2.zero; lpRt.sizeDelta = new Vector2(leftW, 0f);
        // 우측 경계선
        var lBorder = new GameObject("RightBorder", typeof(RectTransform), typeof(Image));
        lBorder.transform.SetParent(leftPanel.transform, false);
        lBorder.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.34f, 1f);
        var lbRt = lBorder.GetComponent<RectTransform>();
        lbRt.anchorMin = new Vector2(1f, 0f); lbRt.anchorMax = new Vector2(1f, 1f);
        lbRt.pivot = new Vector2(1f, 0.5f); lbRt.anchoredPosition = Vector2.zero; lbRt.sizeDelta = new Vector2(1f, 0f);

        // ── DetailBox (좌 상단, 고정 높이) ───────────────────
        var detailBox = new GameObject("DetailBox", typeof(RectTransform), typeof(Image));
        detailBox.transform.SetParent(leftPanel.transform, false);
        detailBox.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.20f, 1f);
        AnchorTopLocal(detailBox, detailH, 0);

        // InfoIcon
        float iconSz = 80f;
        var infoIconGo = new GameObject("InfoIcon", typeof(RectTransform), typeof(Image));
        infoIconGo.transform.SetParent(detailBox.transform, false);
        infoIconGo.GetComponent<Image>().color = Color.white;
        var iiRt = infoIconGo.GetComponent<RectTransform>();
        iiRt.anchorMin = new Vector2(0.5f, 1f); iiRt.anchorMax = new Vector2(0.5f, 1f);
        iiRt.pivot = new Vector2(0.5f, 1f);
        iiRt.anchoredPosition = new Vector2(0f, -16f); iiRt.sizeDelta = new Vector2(iconSz, iconSz);

        // InfoGradeBar
        float gradeBarY = 16f + iconSz + 10f;
        var gradeBarGo = new GameObject("InfoGradeBar", typeof(RectTransform), typeof(Image));
        gradeBarGo.transform.SetParent(detailBox.transform, false);
        gradeBarGo.GetComponent<Image>().color = new Color(0.50f, 0.50f, 0.60f, 1f);
        var gbRt = gradeBarGo.GetComponent<RectTransform>();
        gbRt.anchorMin = new Vector2(0f, 1f); gbRt.anchorMax = new Vector2(1f, 1f);
        gbRt.pivot = new Vector2(0.5f, 1f);
        gbRt.anchoredPosition = new Vector2(0f, -gradeBarY); gbRt.sizeDelta = new Vector2(0f, 6f);

        // InfoGradeTmp, InfoNameTmp, InfoTargetTmp
        float y1 = gradeBarY + 6f + 8f;
        var gradeTmp = AddTMP(detailBox, "InfoGradeTmp", "일반", UIScale.FontSm, FontStyles.Normal);
        PinTop(gradeTmp.rectTransform, -y1, UIScale.FontSm + 8f);

        float y2 = y1 + UIScale.FontSm + 8f + 4f;
        var nameTmp = AddTMP(detailBox, "InfoNameTmp", "어빌리티 이름", UIScale.FontMd, FontStyles.Bold);
        PinTop(nameTmp.rectTransform, -y2, UIScale.FontMd + 8f);

        float y3 = y2 + UIScale.FontMd + 8f + 4f;
        var targetTmp = AddTMP(detailBox, "InfoTargetTmp", "대상: 전체", UIScale.FontSm, FontStyles.Normal);
        targetTmp.color = new Color(0.55f, 0.65f, 0.80f);
        PinTop(targetTmp.rectTransform, -y3, UIScale.FontSm + 8f);

        // 구분선
        float divY = y3 + UIScale.FontSm + 8f + 8f;
        var detDiv = new GameObject("StatDivider", typeof(RectTransform), typeof(Image));
        detDiv.transform.SetParent(detailBox.transform, false);
        detDiv.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.32f, 1f);
        var ddRt = detDiv.GetComponent<RectTransform>();
        ddRt.anchorMin = new Vector2(0f, 1f); ddRt.anchorMax = new Vector2(1f, 1f);
        ddRt.pivot = new Vector2(0.5f, 1f);
        ddRt.anchoredPosition = new Vector2(0f, -divY); ddRt.sizeDelta = new Vector2(-20f, 1f);

        // InfoStatContent (VLG + CSF, 나머지 공간 채움)
        float statY = divY + 8f;
        var infoStatContent = new GameObject("InfoStatContent",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        infoStatContent.transform.SetParent(detailBox.transform, false);
        var iscRt = infoStatContent.GetComponent<RectTransform>();
        iscRt.anchorMin = Vector2.zero; iscRt.anchorMax = Vector2.one;
        iscRt.offsetMin = new Vector2(10f, 0f); iscRt.offsetMax = new Vector2(-10f, -statY);
        var iscVlg = infoStatContent.GetComponent<VerticalLayoutGroup>();
        iscVlg.padding = new RectOffset(0, 0, 4, 4); iscVlg.spacing = 6f;
        iscVlg.childControlWidth = true; iscVlg.childForceExpandWidth = true;
        iscVlg.childControlHeight = false; iscVlg.childForceExpandHeight = false;
        infoStatContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // InfoStatTemplate (팝업 루트에 배치, 비활성)
        var infoStatRow = new GameObject("InfoStatTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoStatRow.transform.SetParent(root.transform, false);
        var infoRowTmp = infoStatRow.GetComponent<TextMeshProUGUI>();
        infoRowTmp.text = "<color=#AAAAAA>스탯</color>  +0%";
        infoRowTmp.fontSize = UIScale.FontSm; infoRowTmp.alignment = TextAlignmentOptions.Left;
        infoRowTmp.color = Color.white;
        infoStatRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, UIScale.FontSm + 10f);
        infoStatRow.SetActive(false);

        // ── TotalBox (좌 하단, detailH 아래부터 채움) ─────────
        var totalBox = new GameObject("TotalBox", typeof(RectTransform), typeof(Image));
        totalBox.transform.SetParent(leftPanel.transform, false);
        totalBox.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.16f, 1f);
        var tbRt = totalBox.GetComponent<RectTransform>();
        tbRt.anchorMin = Vector2.zero; tbRt.anchorMax = Vector2.one;
        tbRt.offsetMin = Vector2.zero; tbRt.offsetMax = new Vector2(0f, -detailH);

        var totalTitle = AddTMP(totalBox, "TotalTitle", "총 스탯 합산", UIScale.FontSm, FontStyles.Bold);
        totalTitle.color = new Color(0.45f, 0.50f, 0.65f);
        PinTop(totalTitle.rectTransform, -10f, UIScale.FontSm + 10f);

        var totDivGo = new GameObject("TotalDivider", typeof(RectTransform), typeof(Image));
        totDivGo.transform.SetParent(totalBox.transform, false);
        totDivGo.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.32f, 1f);
        var tdRt = totDivGo.GetComponent<RectTransform>();
        tdRt.anchorMin = new Vector2(0f, 1f); tdRt.anchorMax = new Vector2(1f, 1f);
        tdRt.pivot = new Vector2(0.5f, 1f);
        tdRt.anchoredPosition = new Vector2(0f, -(UIScale.FontSm + 22f)); tdRt.sizeDelta = new Vector2(-20f, 1f);

        float totalTitleH = UIScale.FontSm + 26f;
        var totalScroll = new GameObject("TotalStatScrollView", typeof(RectTransform), typeof(ScrollRect));
        totalScroll.transform.SetParent(totalBox.transform, false);
        var tsRt = totalScroll.GetComponent<RectTransform>();
        tsRt.anchorMin = Vector2.zero; tsRt.anchorMax = Vector2.one;
        tsRt.offsetMin = Vector2.zero; tsRt.offsetMax = new Vector2(0f, -totalTitleH);

        var totalVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        totalVp.transform.SetParent(totalScroll.transform, false);
        totalVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        totalVp.GetComponent<Mask>().showMaskGraphic = false;
        var tvpRt = totalVp.GetComponent<RectTransform>();
        tvpRt.anchorMin = Vector2.zero; tvpRt.anchorMax = Vector2.one;
        tvpRt.offsetMin = tvpRt.offsetMax = Vector2.zero;

        var totalStatContent = new GameObject("TotalStatContent",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        totalStatContent.transform.SetParent(totalVp.transform, false);
        var tscRt = totalStatContent.GetComponent<RectTransform>();
        tscRt.anchorMin = new Vector2(0f, 1f); tscRt.anchorMax = new Vector2(1f, 1f);
        tscRt.pivot = new Vector2(0.5f, 1f); tscRt.offsetMin = tscRt.offsetMax = Vector2.zero;
        var tscVlg = totalStatContent.GetComponent<VerticalLayoutGroup>();
        tscVlg.padding = new RectOffset(10, 10, 6, 6); tscVlg.spacing = 4f;
        tscVlg.childControlWidth = true; tscVlg.childForceExpandWidth = true;
        tscVlg.childControlHeight = false; tscVlg.childForceExpandHeight = false;
        totalStatContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var totalSr = totalScroll.GetComponent<ScrollRect>();
        totalSr.content = tscRt; totalSr.viewport = tvpRt;
        totalSr.horizontal = false; totalSr.vertical = true;
        totalSr.movementType = ScrollRect.MovementType.Elastic;

        // TotalStatTemplate (팝업 루트에 배치, 비활성)
        var totalStatRow = new GameObject("TotalStatTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
        totalStatRow.transform.SetParent(root.transform, false);
        var totRowTmp = totalStatRow.GetComponent<TextMeshProUGUI>();
        totRowTmp.text = "<color=#AAAAAA>스탯</color>  +0%";
        totRowTmp.fontSize = UIScale.FontSm; totRowTmp.alignment = TextAlignmentOptions.Left;
        totRowTmp.color = Color.white;
        totalStatRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, UIScale.FontSm + 10f);
        totalStatRow.SetActive(false);

        // ── Right Panel (우, 나머지 너비 채움) ────────────────
        var rightPanel = new GameObject("RightPanel", typeof(RectTransform), typeof(Image));
        rightPanel.transform.SetParent(body.transform, false);
        rightPanel.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.12f, 1f);
        var rpRt = rightPanel.GetComponent<RectTransform>();
        rpRt.anchorMin = Vector2.zero; rpRt.anchorMax = Vector2.one;
        rpRt.offsetMin = new Vector2(leftW + 1f, 0f); rpRt.offsetMax = Vector2.zero;

        // 목록 헤더
        float listHeaderH = 44f;
        var listHeaderGo = new GameObject("ListHeader", typeof(RectTransform), typeof(Image));
        listHeaderGo.transform.SetParent(rightPanel.transform, false);
        listHeaderGo.GetComponent<Image>().color = new Color(0.09f, 0.09f, 0.16f, 1f);
        AnchorTopLocal(listHeaderGo, listHeaderH, 0);
        var listHeaderTmp = AddTMP(listHeaderGo, "ListHeaderText", "보유 목록", UIScale.FontSm, FontStyles.Bold);
        SetRect(listHeaderTmp.rectTransform, new Vector2(-10f, 0f), new Vector2(300f, listHeaderH - 8f));
        listHeaderTmp.alignment = TextAlignmentOptions.Left;
        listHeaderTmp.color = new Color(0.50f, 0.55f, 0.70f);

        // ListScrollView
        var listScroll = new GameObject("ListScrollView", typeof(RectTransform), typeof(ScrollRect));
        listScroll.transform.SetParent(rightPanel.transform, false);
        var lsRt = listScroll.GetComponent<RectTransform>();
        lsRt.anchorMin = Vector2.zero; lsRt.anchorMax = Vector2.one;
        lsRt.offsetMin = Vector2.zero; lsRt.offsetMax = new Vector2(0f, -listHeaderH);

        var listVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        listVp.transform.SetParent(listScroll.transform, false);
        listVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        listVp.GetComponent<Mask>().showMaskGraphic = false;
        var lvpRt = listVp.GetComponent<RectTransform>();
        lvpRt.anchorMin = Vector2.zero; lvpRt.anchorMax = Vector2.one;
        lvpRt.offsetMin = lvpRt.offsetMax = Vector2.zero;

        var listContent = new GameObject("ListContent",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listContent.transform.SetParent(listVp.transform, false);
        var lcRt = listContent.GetComponent<RectTransform>();
        lcRt.anchorMin = new Vector2(0f, 1f); lcRt.anchorMax = new Vector2(1f, 1f);
        lcRt.pivot = new Vector2(0.5f, 1f); lcRt.offsetMin = lcRt.offsetMax = Vector2.zero;
        var lcVlg = listContent.GetComponent<VerticalLayoutGroup>();
        lcVlg.padding = new RectOffset(8, 8, 8, 8); lcVlg.spacing = 6f;
        lcVlg.childForceExpandWidth = true; lcVlg.childForceExpandHeight = false;
        lcVlg.childControlWidth = true; lcVlg.childControlHeight = false;
        listContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var listSr = listScroll.GetComponent<ScrollRect>();
        listSr.content = lcRt; listSr.viewport = lvpRt;
        listSr.horizontal = false; listSr.vertical = true;
        listSr.movementType = ScrollRect.MovementType.Elastic;

        // ── List Item Template (팝업 루트에 배치, 비활성) ─────
        float itemH   = 88f;
        float iconSize = 68f;
        float textX   = 10f + iconSize + 12f;

        var itemTemplate = new GameObject("ListItemTemplate",
            typeof(RectTransform), typeof(Image), typeof(Button));
        itemTemplate.transform.SetParent(root.transform, false);
        itemTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, itemH);
        itemTemplate.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.20f, 1f);

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(itemTemplate.transform, false);
        iconGo.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.30f, 1f);
        var icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0f, 0.5f); icRt.anchorMax = new Vector2(0f, 0.5f);
        icRt.pivot = new Vector2(0f, 0.5f);
        icRt.anchoredPosition = new Vector2(10f, 0f); icRt.sizeDelta = new Vector2(iconSize, iconSize);

        var itemNameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemNameGo.transform.SetParent(itemTemplate.transform, false);
        var inTmp = itemNameGo.GetComponent<TextMeshProUGUI>();
        inTmp.text = "어빌리티 이름"; inTmp.fontSize = UIScale.FontSm;
        inTmp.fontStyle = FontStyles.Bold; inTmp.alignment = TextAlignmentOptions.Left; inTmp.color = Color.white;
        var inRt = itemNameGo.GetComponent<RectTransform>();
        inRt.anchorMin = new Vector2(0f, 0.5f); inRt.anchorMax = new Vector2(0f, 0.5f);
        inRt.pivot = new Vector2(0f, 0.5f);
        inRt.anchoredPosition = new Vector2(textX, 12f); inRt.sizeDelta = new Vector2(220f, UIScale.FontSm + 8f);

        var itemTargetGo = new GameObject("TargetText", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemTargetGo.transform.SetParent(itemTemplate.transform, false);
        var itgTmp = itemTargetGo.GetComponent<TextMeshProUGUI>();
        itgTmp.text = "전체"; itgTmp.fontSize = UIScale.FontSm - 4f;
        itgTmp.alignment = TextAlignmentOptions.Left; itgTmp.color = new Color(0.55f, 0.60f, 0.75f);
        var itgRt = itemTargetGo.GetComponent<RectTransform>();
        itgRt.anchorMin = new Vector2(0f, 0.5f); itgRt.anchorMax = new Vector2(0f, 0.5f);
        itgRt.pivot = new Vector2(0f, 0.5f);
        itgRt.anchoredPosition = new Vector2(textX, -12f); itgRt.sizeDelta = new Vector2(200f, UIScale.FontSm + 4f);

        // CountBadge (×N, 기본 비활성)
        var countBadgeGo = new GameObject("CountBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        countBadgeGo.transform.SetParent(itemTemplate.transform, false);
        var cbTmp = countBadgeGo.GetComponent<TextMeshProUGUI>();
        cbTmp.text = "×2"; cbTmp.fontSize = UIScale.FontSm - 2f;
        cbTmp.fontStyle = FontStyles.Bold; cbTmp.alignment = TextAlignmentOptions.Left;
        cbTmp.color = new Color(0.55f, 0.80f, 1.00f);
        var cbRt = countBadgeGo.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0f, 0.5f); cbRt.anchorMax = new Vector2(0f, 0.5f);
        cbRt.pivot = new Vector2(0f, 0.5f);
        cbRt.anchoredPosition = new Vector2(textX + 228f, 12f); cbRt.sizeDelta = new Vector2(60f, UIScale.FontSm + 4f);
        countBadgeGo.SetActive(false);

        // GradeText (우측 정렬)
        var gradeTextGo = new GameObject("GradeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        gradeTextGo.transform.SetParent(itemTemplate.transform, false);
        var gtTmp = gradeTextGo.GetComponent<TextMeshProUGUI>();
        gtTmp.text = "일반"; gtTmp.fontSize = UIScale.FontSm - 4f;
        gtTmp.fontStyle = FontStyles.Bold; gtTmp.alignment = TextAlignmentOptions.Right;
        gtTmp.color = new Color(0.70f, 0.70f, 0.80f);
        var gtRt = gradeTextGo.GetComponent<RectTransform>();
        gtRt.anchorMin = new Vector2(1f, 0.5f); gtRt.anchorMax = new Vector2(1f, 0.5f);
        gtRt.pivot = new Vector2(1f, 0.5f);
        gtRt.anchoredPosition = new Vector2(-10f, 0f); gtRt.sizeDelta = new Vector2(80f, UIScale.FontSm + 8f);

        itemTemplate.SetActive(false);

        // ── 필드 연결 ────────────────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",          (int)PopupType.AbilityList);
        SetObj (so, "_headerCountTmp",     headerCountTmp);
        SetObj (so, "_infoIcon",           infoIconGo.GetComponent<Image>());
        SetObj (so, "_infoGradeBar",       gradeBarGo.GetComponent<Image>());
        SetObj (so, "_infoGradeTmp",       gradeTmp);
        SetObj (so, "_infoNameTmp",        nameTmp);
        SetObj (so, "_infoTargetTmp",      targetTmp);
        SetObj (so, "_infoStatContent",    infoStatContent.transform);
        SetObj (so, "_infoStatTemplate",   infoRowTmp);
        SetObj (so, "_totalStatContent",   totalStatContent.transform);
        SetObj (so, "_totalStatTemplate",  totRowTmp);
        SetObj (so, "_listContent",        listContent.transform);
        SetObj (so, "_listItemTemplate",   itemTemplate);
        SetObj (so, "_closeBtn",           closeBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "AbilityListPopup");
        Debug.Log("[PopupPrefabCreator] AbilityListPopup 저장 완료");
    }

    // 상단 앵커 고정 (full-width)
    static void AnchorTopLocal(GameObject go, float height, float offsetFromTop)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -(offsetFromTop + height));
        rt.offsetMax = new Vector2(0, -offsetFromTop);
    }

    // 하단 앵커 고정 (full-width)
    static void AnchorBottomLocal(GameObject go, float height, float offsetFromBottom)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = new Vector2(0, offsetFromBottom);
        rt.offsetMax = new Vector2(0, offsetFromBottom + height);
    }

    // 상단 앵커 + full-width (InfoPanel 자식용)
    static void PinTop(RectTransform rt, float anchoredY, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, anchoredY);
        rt.sizeDelta        = new Vector2(0, height);
    }

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SavePath}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[PopupPrefabCreator] 저장: {path}");
    }
}
