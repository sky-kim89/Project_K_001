using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EventPopupCreator.cs
//  Tools > Project K > Popup > Create EventPopup Prefab
//
//  레이아웃 (880 × 1160, center 기준)
//  ─────────────────────────────────────────────────────────
//  TitleText             Y=  540  H=  80  타이틀
//  Illustration          Y=  420  H= 240  선택적 삽화 (Image)
//  BodyScrollView        Y=  140  H= 280  이벤트 본문 스크롤
//  Divider               Y=  -10  H=   2
//  ChoiceRoot (Vertical) Y= -160  H= 280  선택지 버튼들
//    ChoiceButtonTemplate H=  80           (inactive 기본)
//  ResultPanel (hidden)  Full   (absolute)
//    ResultText           Center
//  ConfirmButton (hidden) Y= -540  H= 100
// ============================================================

public static class EventPopupCreator
{
    const string SaveDir = "Assets/_project/2.Prefabs/UI";

    static readonly Color BgOverlay    = new Color(0f,    0f,    0f,    0.72f);
    static readonly Color PanelBg      = new Color(0.09f, 0.10f, 0.17f, 0.98f);
    static readonly Color PanelBorder  = new Color(0.25f, 0.28f, 0.45f, 1f);
    static readonly Color TitleColor   = new Color(1.00f, 0.95f, 0.75f, 1f);
    static readonly Color BodyColor    = new Color(0.82f, 0.82f, 0.90f, 1f);
    static readonly Color DividerColor = new Color(0.25f, 0.28f, 0.45f, 0.70f);
    static readonly Color ChoiceBg     = new Color(0.16f, 0.18f, 0.30f, 1f);
    static readonly Color ChoiceHover  = new Color(0.22f, 0.25f, 0.40f, 1f);
    static readonly Color ResultBg     = new Color(0.07f, 0.08f, 0.14f, 0.97f);
    static readonly Color ResultText   = new Color(0.88f, 0.95f, 0.80f, 1f);
    static readonly Color ConfirmColor = new Color(0.20f, 0.60f, 0.35f, 1f);

    [MenuItem("Tools/Project K/Popup/Create EventPopup Prefab")]
    public static void Create()
    {
        const float PW = 880f;
        const float PH = 1160f;

        // ── 루트 (전체화면 오버레이) ──────────────────────────
        var root = new GameObject("EventPopup", typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = BgOverlay;
        FullStretch(root);
        var popup = root.AddComponent<EventPopup>();

        // ── 패널 ─────────────────────────────────────────────
        var panel = AddImg(root, "Panel", PanelBg);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(PW, PH);

        // 패널 테두리 (Outline 대신 자식 Image stretch)
        var border = AddImg(panel.gameObject, "Border", PanelBorder);
        var bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-3f, -3f); bRt.offsetMax = new Vector2(3f, 3f);
        border.transform.SetAsFirstSibling();

        // ── 타이틀 ────────────────────────────────────────────
        var titleTmp = AddTMP(panel.gameObject, "TitleText", "이벤트 제목", UIScale.FontLg, FontStyles.Bold);
        titleTmp.color = TitleColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        var tRt = titleTmp.rectTransform;
        tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -24f);
        tRt.sizeDelta = new Vector2(PW - 48f, 80f);

        // ── 삽화 (Image) ─────────────────────────────────────
        var illustGo = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
        illustGo.transform.SetParent(panel.transform, false);
        illustGo.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.26f, 1f);
        var iRt = illustGo.GetComponent<RectTransform>();
        iRt.anchorMin = iRt.anchorMax = new Vector2(0.5f, 1f);
        iRt.pivot = new Vector2(0.5f, 1f);
        iRt.anchoredPosition = new Vector2(0f, -116f);
        iRt.sizeDelta = new Vector2(PW - 48f, 240f);

        // ── 본문 스크롤뷰 ─────────────────────────────────────
        var bodyScroll = new GameObject("BodyScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        bodyScroll.transform.SetParent(panel.transform, false);
        bodyScroll.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.14f, 0.60f);
        var bsRt = bodyScroll.GetComponent<RectTransform>();
        bsRt.anchorMin = bsRt.anchorMax = new Vector2(0.5f, 1f);
        bsRt.pivot = new Vector2(0.5f, 1f);
        bsRt.anchoredPosition = new Vector2(0f, -368f);
        bsRt.sizeDelta = new Vector2(PW - 48f, 280f);

        var bodyViewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        bodyViewport.transform.SetParent(bodyScroll.transform, false);
        bodyViewport.GetComponent<Image>().color = Color.clear;
        bodyViewport.GetComponent<Mask>().showMaskGraphic = false;
        FullStretch(bodyViewport);

        var bodyContent = new GameObject("Content", typeof(RectTransform));
        bodyContent.transform.SetParent(bodyViewport.transform, false);
        var bcRt = bodyContent.GetComponent<RectTransform>();
        bcRt.anchorMin = new Vector2(0f, 1f); bcRt.anchorMax = new Vector2(1f, 1f);
        bcRt.pivot = new Vector2(0.5f, 1f);
        bcRt.anchoredPosition = Vector2.zero;
        bcRt.sizeDelta = new Vector2(0f, 280f);
        var bodyCsf = bodyContent.AddComponent<ContentSizeFitter>();
        bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bodyTmp = AddTMP(bodyContent, "BodyText", "이벤트 본문입니다.\n내용을 여기에 작성합니다.", UIScale.FontMd, FontStyles.Normal);
        bodyTmp.color = BodyColor;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.enableWordWrapping = true;
        var btRt = bodyTmp.rectTransform;
        FullStretch(bodyTmp.gameObject);
        btRt.offsetMin = new Vector2(16f, 8f); btRt.offsetMax = new Vector2(-16f, -8f);

        var bodyScrollRect = bodyScroll.GetComponent<ScrollRect>();
        bodyScrollRect.viewport = bodyViewport.GetComponent<RectTransform>();
        bodyScrollRect.content  = bcRt;
        bodyScrollRect.horizontal = false;
        bodyScrollRect.vertical   = true;
        bodyScrollRect.scrollSensitivity = 30f;

        // ── 구분선 ────────────────────────────────────────────
        var divGo = AddImg(panel.gameObject, "Divider", DividerColor);
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0f, 1f); divRt.anchorMax = new Vector2(1f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -660f);
        divRt.sizeDelta = new Vector2(-48f, 2f);

        // ── 선택지 영역 (VerticalLayoutGroup) ─────────────────
        var choiceRoot = new GameObject("ChoiceRoot", typeof(RectTransform));
        choiceRoot.transform.SetParent(panel.transform, false);
        var crRt = choiceRoot.GetComponent<RectTransform>();
        crRt.anchorMin = crRt.anchorMax = new Vector2(0.5f, 1f);
        crRt.pivot = new Vector2(0.5f, 1f);
        crRt.anchoredPosition = new Vector2(0f, -676f);
        crRt.sizeDelta = new Vector2(PW - 48f, 360f);
        var vlg = choiceRoot.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 12, 0);

        // ── 선택지 버튼 템플릿 (비활성) ──────────────────────
        var choiceBtn = new GameObject("ChoiceButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
        choiceBtn.transform.SetParent(choiceRoot.transform, false);
        choiceBtn.GetComponent<Image>().color = ChoiceBg;
        var cbRt = choiceBtn.GetComponent<RectTransform>();
        cbRt.sizeDelta = new Vector2(PW - 48f, 80f);
        var cbColors = choiceBtn.GetComponent<Button>().colors;
        cbColors.highlightedColor = ChoiceHover;
        cbColors.pressedColor     = new Color(0.28f, 0.32f, 0.50f, 1f);
        cbColors.disabledColor    = new Color(0.30f, 0.30f, 0.35f, 0.60f);
        choiceBtn.GetComponent<Button>().colors = cbColors;

        var choiceTmp = AddTMP(choiceBtn, "Label", "선택지 텍스트", UIScale.FontMd, FontStyles.Bold);
        choiceTmp.color = Color.white;
        choiceTmp.alignment = TextAlignmentOptions.Center;
        FullStretch(choiceTmp.gameObject);
        choiceTmp.rectTransform.offsetMin = new Vector2(12f, 4f);
        choiceTmp.rectTransform.offsetMax = new Vector2(-12f, -4f);

        choiceBtn.SetActive(false);  // 런타임에 Instantiate 됨

        // ── 결과 패널 (숨김) ─────────────────────────────────
        var resultPanel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
        resultPanel.transform.SetParent(panel.transform, false);
        resultPanel.GetComponent<Image>().color = ResultBg;
        var rpRt = resultPanel.GetComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0f, 0f); rpRt.anchorMax = new Vector2(1f, 1f);
        rpRt.offsetMin = new Vector2(0f, 160f); rpRt.offsetMax = new Vector2(0f, -104f);
        resultPanel.SetActive(false);

        var resultTmp = AddTMP(resultPanel, "ResultText", "이벤트 결과 텍스트", UIScale.FontMd, FontStyles.Normal);
        resultTmp.color = ResultText;
        resultTmp.alignment = TextAlignmentOptions.Center;
        resultTmp.enableWordWrapping = true;
        FullStretch(resultTmp.gameObject);
        resultTmp.rectTransform.offsetMin = new Vector2(24f, 24f);
        resultTmp.rectTransform.offsetMax = new Vector2(-24f, -24f);

        // ── 확인 버튼 (숨김) ─────────────────────────────────
        var confirmBtn = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
        confirmBtn.transform.SetParent(panel.transform, false);
        confirmBtn.GetComponent<Image>().color = ConfirmColor;
        var conRt = confirmBtn.GetComponent<RectTransform>();
        conRt.anchorMin = conRt.anchorMax = new Vector2(0.5f, 0f);
        conRt.pivot = new Vector2(0.5f, 0f);
        conRt.anchoredPosition = new Vector2(0f, 24f);
        conRt.sizeDelta = new Vector2(380f, UIScale.BtnMd);

        var conTmp = AddTMP(confirmBtn, "Label", "확인", UIScale.FontLg, FontStyles.Bold);
        conTmp.color = Color.white;
        conTmp.alignment = TextAlignmentOptions.Center;
        FullStretch(conTmp.gameObject);
        confirmBtn.SetActive(false);

        // ── 직렬화 필드 연결 ─────────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",           (int)PopupType.Event);
        SetObj (so, "_illustration",        illustGo.GetComponent<Image>());
        SetObj (so, "_titleTmp",            titleTmp);
        SetObj (so, "_bodyTmp",             bodyTmp);
        SetObj (so, "_choiceRoot",          choiceRoot.GetComponent<RectTransform>());
        SetObj (so, "_choiceButtonTemplate",choiceBtn.GetComponent<Button>());
        SetObj (so, "_resultPanel",         resultPanel);
        SetObj (so, "_resultTmp",           resultTmp);
        SetObj (so, "_confirmBtn",          confirmBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "EventPopup");
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static Image AddImg(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<Image>();
    }

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return tmp;
    }

    static TextMeshProUGUI AddTMP(Transform parent, string name, string text, float size, FontStyles style)
        => AddTMP(parent.gameObject, name, text, size, style);

    static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetEnum(SerializedObject so, string field, int value)
    {
        var prop = so.FindProperty(field); if (prop != null) prop.intValue = value;
    }

    static void SetObj(SerializedObject so, string field, UnityEngine.Object obj)
    {
        var prop = so.FindProperty(field); if (prop != null) prop.objectReferenceValue = obj;
    }

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SaveDir}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[EventPopupCreator] 저장: {path}");
    }
}
