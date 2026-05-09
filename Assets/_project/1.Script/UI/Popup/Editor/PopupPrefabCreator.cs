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
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopupPrefabCreator] 팝업 프리팹 생성 완료");
    }

    // ── ExpRow 프리팹 ─────────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create ExpRow Prefab")]
    static void CreateExpRowPrefab()
    {
        const float rowH       = 80f;
        const float portraitSz = 66f;
        const float padLeft    = 8f;

        var root = new GameObject("ExpRow", typeof(RectTransform));
        root.AddComponent<ExpRowUI>();
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(840f, rowH);

        // 배경
        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.20f, 0.90f);

        // 초상화 배경
        var portBg = new GameObject("PortraitBg", typeof(RectTransform), typeof(Image));
        portBg.transform.SetParent(root.transform, false);
        var portBgRt = portBg.GetComponent<RectTransform>();
        portBgRt.anchorMin        = new Vector2(0f, 0.5f);
        portBgRt.anchorMax        = new Vector2(0f, 0.5f);
        portBgRt.pivot            = new Vector2(0f, 0.5f);
        portBgRt.anchoredPosition = new Vector2(padLeft, 0f);
        portBgRt.sizeDelta        = new Vector2(portraitSz, portraitSz);
        portBg.GetComponent<Image>().color = new Color(0.50f, 0.14f, 0.14f);

        // 초상화 이미지
        var portImg = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portImg.transform.SetParent(root.transform, false);
        var portImgRt = portImg.GetComponent<RectTransform>();
        portImgRt.anchorMin        = new Vector2(0f, 0.5f);
        portImgRt.anchorMax        = new Vector2(0f, 0.5f);
        portImgRt.pivot            = new Vector2(0f, 0.5f);
        portImgRt.anchoredPosition = new Vector2(padLeft, 0f);
        portImgRt.sizeDelta        = new Vector2(portraitSz, portraitSz);
        portImg.GetComponent<Image>().color = Color.white;

        // 초상화 브릿지 (숨김 GO — CharacterBuilder 붙임)
        var bridgeGo = new GameObject("PortraitBridge", typeof(RectTransform));
        bridgeGo.transform.SetParent(root.transform, false);
        bridgeGo.SetActive(false);
        bridgeGo.AddComponent<UnitAppearanceBridge>();
        bridgeGo.AddComponent<Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts.CharacterBuilder>();

        // 이름
        float textX = padLeft + portraitSz + 12f;
        var nameText = AddTMP(root, "NameText", "영웅 이름", UIScale.FontMd, FontStyles.Bold);
        var nameRt   = nameText.rectTransform;
        nameRt.anchorMin        = new Vector2(0f, 0.5f);
        nameRt.anchorMax        = new Vector2(0f, 0.5f);
        nameRt.pivot            = new Vector2(0f, 0.5f);
        nameRt.anchoredPosition = new Vector2(textX, 14f);
        nameRt.sizeDelta        = new Vector2(260f, 40f);
        nameText.alignment      = TextAlignmentOptions.Left;

        // 레벨
        var levelText = AddTMP(root, "LevelText", "Lv.1", UIScale.FontSm, FontStyles.Normal);
        var levelRt   = levelText.rectTransform;
        levelRt.anchorMin        = new Vector2(0f, 0.5f);
        levelRt.anchorMax        = new Vector2(0f, 0.5f);
        levelRt.pivot            = new Vector2(0f, 0.5f);
        levelRt.anchoredPosition = new Vector2(textX, -16f);
        levelRt.sizeDelta        = new Vector2(160f, 36f);
        levelText.alignment      = TextAlignmentOptions.Left;
        levelText.color          = new Color(0.75f, 0.75f, 0.75f);

        // EXP 텍스트
        var expText = AddTMP(root, "ExpText", "+0 EXP", UIScale.FontMd, FontStyles.Normal);
        var expRt   = expText.rectTransform;
        expRt.anchorMin        = new Vector2(1f, 0.5f);
        expRt.anchorMax        = new Vector2(1f, 0.5f);
        expRt.pivot            = new Vector2(1f, 0.5f);
        expRt.anchoredPosition = new Vector2(-180f, 4f);
        expRt.sizeDelta        = new Vector2(240f, 44f);
        expText.alignment      = TextAlignmentOptions.Right;
        expText.color          = new Color(0.60f, 0.90f, 0.60f);

        // 레벨업 텍스트
        var lvupText = AddTMP(root, "LevelUpText", "▲ 레벨업!", UIScale.FontMd, FontStyles.Bold);
        var lvupRt   = lvupText.rectTransform;
        lvupRt.anchorMin        = new Vector2(1f, 0.5f);
        lvupRt.anchorMax        = new Vector2(1f, 0.5f);
        lvupRt.pivot            = new Vector2(1f, 0.5f);
        lvupRt.anchoredPosition = new Vector2(-8f, 4f);
        lvupRt.sizeDelta        = new Vector2(160f, 44f);
        lvupText.alignment      = TextAlignmentOptions.Right;
        lvupText.color          = new Color(1.00f, 0.85f, 0.15f);
        lvupText.gameObject.SetActive(false);

        var so = new SerializedObject(root.GetComponent<ExpRowUI>());
        SetObj(so, "_portraitBg",     portBg .GetComponent<Image>());
        SetObj(so, "_portraitImage",  portImg.GetComponent<Image>());
        SetObj(so, "_portraitBridge", bridgeGo.GetComponent<UnitAppearanceBridge>());
        SetObj(so, "_nameText",       nameText);
        SetObj(so, "_levelText",      levelText);
        SetObj(so, "_expText",        expText);
        SetObj(so, "_levelUpText",    lvupText);
        so.ApplyModifiedProperties();

        Save(root, "ExpRow");
    }

    // ── BattleResultPopup ─────────────────────────────────────

    static void CreateBattleResultPopup()
    {
        // 레이아웃 (위 → 아래):
        //   승리! / 서브 / 스탯 → 보상 카드 → 힌트 → EXP 행 목록 → 확인 버튼
        var root  = CreateRoot<BattleResultPopup>("BattleResultPopup", 900, 1060);
        var popup = root.GetComponent<BattleResultPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        var resultText = AddTMP(root, "ResultText", "승리!", UIScale.FontXl, FontStyles.Bold);
        var subText    = AddTMP(root, "SubText", "모든 적을 물리쳤습니다!", UIScale.FontSm, FontStyles.Normal);
        var statsText  = AddTMP(root, "StatsText", "처치  0   |   웨이브  0 / 0", UIScale.FontSm, FontStyles.Normal);

        SetRect(resultText.rectTransform, new Vector2(0,  480), new Vector2(800, 90));
        SetRect(subText.rectTransform,    new Vector2(0,  400), new Vector2(800, 50));
        SetRect(statsText.rectTransform,  new Vector2(0,  342), new Vector2(700, 50));

        // ── 보상 카드 영역 (기존 — 클리어 보상 아이콘 표시) ─────────
        var rewardArea = new GameObject("RewardArea", typeof(RectTransform));
        rewardArea.transform.SetParent(root.transform, false);
        var rewardRt = rewardArea.GetComponent<RectTransform>();
        rewardRt.anchorMin        = new Vector2(0.5f, 0.5f);
        rewardRt.anchorMax        = new Vector2(0.5f, 0.5f);
        rewardRt.pivot            = new Vector2(0.5f, 0.5f);
        rewardRt.anchoredPosition = new Vector2(0f, 210f);
        rewardRt.sizeDelta        = new Vector2(860f, 140f);
        var rewardHlg = rewardArea.AddComponent<HorizontalLayoutGroup>();
        rewardHlg.spacing              = 16f;
        rewardHlg.childAlignment       = TextAnchor.MiddleCenter;
        rewardHlg.childControlWidth    = false;
        rewardHlg.childControlHeight   = false;
        rewardHlg.childForceExpandWidth  = false;
        rewardHlg.childForceExpandHeight = false;

        // 힌트 텍스트
        var hintText = AddTMP(root, "HintText", "카드를 탭하면 개봉합니다", UIScale.FontSm, FontStyles.Italic);
        hintText.color = new Color(0.70f, 0.70f, 0.50f);
        SetRect(hintText.rectTransform, new Vector2(0, 118f), new Vector2(800f, 44f));
        hintText.gameObject.SetActive(false);

        // ── 구분선 ──────────────────────────────────────────────
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(root.transform, false);
        divider.GetComponent<Image>().color = new Color(0.30f, 0.30f, 0.35f, 0.80f);
        SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 82f), new Vector2(840f, 2f));

        // ── EXP 행 목록 (신규 — 승리 시 영웅별 경험치 표시) ──────────
        var expArea = new GameObject("ExpArea", typeof(RectTransform));
        expArea.transform.SetParent(root.transform, false);
        var expAreaRt = expArea.GetComponent<RectTransform>();
        expAreaRt.anchorMin        = new Vector2(0f, 1f);
        expAreaRt.anchorMax        = new Vector2(1f, 1f);
        expAreaRt.pivot            = new Vector2(0.5f, 1f);
        expAreaRt.anchoredPosition = new Vector2(0f, -490f);  // 팝업 상단 기준 -490
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
        SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0, -476f), new Vector2(400f, UIScale.BtnMd));

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
        const float popupW  = 920f;
        const float popupH  = 760f;
        const float cardW   = 272f;
        const float cardH   = 580f;

        var root  = CreateRoot<AbilitySelectPopup>("AbilitySelectPopup", popupW, popupH);
        var popup = root.GetComponent<AbilitySelectPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        // 제목
        var titleTmp = AddTMP(root, "TitleText", "어빌리티 선택", UIScale.FontLg, FontStyles.Bold);
        SetRect(titleTmp.rectTransform, new Vector2(0f, popupH / 2f - 60f), new Vector2(800f, 70f));

        // 카드 영역 (HorizontalLayoutGroup)
        var cardArea = new GameObject("CardArea", typeof(RectTransform));
        cardArea.transform.SetParent(root.transform, false);
        var cardAreaRt = cardArea.GetComponent<RectTransform>();
        cardAreaRt.anchorMin        = new Vector2(0.5f, 0.5f);
        cardAreaRt.anchorMax        = new Vector2(0.5f, 0.5f);
        cardAreaRt.pivot            = new Vector2(0.5f, 0.5f);
        cardAreaRt.anchoredPosition = new Vector2(0f, -30f);
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

        // SerializedObject 연결
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType", (int)PopupType.AbilitySelect);
        SetObj (so, "_titleTmp",  titleTmp);

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

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SavePath}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[PopupPrefabCreator] 저장: {path}");
    }
}
