using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RelicPanelCreator.cs
//  Tools > Project K > 로비 UI > Create RelicPanel Prefab
//
//  저장: Assets/_project/2.Prefabs/UI/Lobby/RelicPanel.prefab
//
//  구조:
//    RelicPanel (RelicPanelUI)
//    ├── HeaderBar                (상단 고정 140px)
//    │   ├── TitleText            "유물"
//    │   ├── PointIcon            환생 포인트 아이콘
//    │   └── PointText            숫자 TMP (우측 정렬)
//    ├── ScrollArea               (4열 UpperCenter 그리드)
//    │   └── Viewport
//    │       └── Content          (GridLayoutGroup 4열, 260×280)
//    ├── RelicCardTemplate        (비활성, 260×280)
//    │   ├── RarityBorder         (상단 6px)
//    │   ├── IconBg               (96×96)
//    │   │   ├── IconImage
//    │   │   └── LevelBadge       (우하단 오버레이)
//    │   │       └── LevelText
//    │   ├── NameText
//    │   ├── DescBg
//    │   ├── DescText             (auto-size)
//    │   ├── CostText
//    │   └── UpgradeBtn
//    └── FooterBar                (하단 고정 200px)
//        └── ReincarnateBtn
//            ├── InnerRow (HLG)
//            │   ├── LeftText     "즉시 환생"
//            │   ├── ReincPtIcon  아이콘 (폰트 크기에 맞춤)
//            │   └── RightText    "{pts}pt 획득"
// ============================================================

public static class RelicPanelCreator
{
    const string SavePath  = "Assets/_project/2.Prefabs/UI/Lobby/RelicPanel.prefab";
    const string ReincIcon = "Assets/_project/3.Textures/Icons/Items/item_reincarnation_point.png";

    const float HeaderH  = 140f;
    const float FooterH  = 200f;
    const float BtnH     = 90f;

    // 카드 그리드 — 4열, 1080px 기준: (1080-16-24)/4=260
    const float CardW       = 260f;
    const float CardH       = 280f;
    const int   GridCols    = 4;
    const int   GridPad     = 8;
    const int   GridSpacing = 8;

    static readonly Color PanelBg          = new Color(0.07f, 0.07f, 0.12f, 1f);
    static readonly Color HeaderBg         = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color FooterBg         = new Color(0.07f, 0.07f, 0.12f, 1f);
    static readonly Color CardBg           = new Color(0.11f, 0.11f, 0.18f, 1f);
    static readonly Color ReincarnateColor = new Color(0.70f, 0.35f, 0.10f, 1f);
    static readonly Color UpgradeBtnColor  = new Color(0.18f, 0.50f, 0.28f, 1f);
    static readonly Color GoldColor        = new Color(1f, 0.82f, 0.25f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/로비 UI/Create RelicPanel Prefab")]
    static void CreateStandalone()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(UIScale.RefWidth, UIScale.RefHeight);

        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RelicPanelCreator] RelicPanel.prefab 생성 완료");
    }

    // ── 공개 빌더 ─────────────────────────────────────────────

    public static GameObject Build(GameObject parent)
    {
        // ── 패널 루트 ─────────────────────────────────────────
        var panel = new GameObject("RelicPanel", typeof(RectTransform));
        panel.transform.SetParent(parent.transform, false);
        var ui = panel.AddComponent<RelicPanelUI>();

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, 110f);
        rt.offsetMax = new Vector2(0, 0);
        panel.AddComponent<Image>().color = PanelBg;

        // ── HeaderBar ─────────────────────────────────────────
        var header = MakePanel(panel, "HeaderBar", HeaderBg);
        TopFull(header.GetComponent<RectTransform>(), 0, HeaderH, 0, 0);

        var titleTmp = MakeTMP(header, "TitleText", "유물", UIScale.FontLg, FontStyles.Bold);
        SetRect(titleTmp.rectTransform, new Vector2(-160f, 0f), new Vector2(260f, 70f));

        // 환생 포인트 아이콘 (원래 위치 유지)
        var ptIconGo = new GameObject("PointIcon", typeof(RectTransform), typeof(Image));
        ptIconGo.transform.SetParent(header.transform, false);
        var ptIconImg = ptIconGo.GetComponent<Image>();
        ptIconImg.preserveAspect = true;
        var ptSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ReincIcon);
        if (ptSprite != null) ptIconImg.sprite = ptSprite;
        SetRect(ptIconGo.GetComponent<RectTransform>(), new Vector2(150f, 0f), new Vector2(52f, 52f));

        // 환생 포인트 숫자 (원래 위치 + 우측 정렬)
        var pointTmp = MakeTMP(header, "PointText", "0", UIScale.FontMd, FontStyles.Bold);
        pointTmp.color     = GoldColor;
        pointTmp.alignment = TextAlignmentOptions.Right;
        SetRect(pointTmp.rectTransform, new Vector2(208f, 0f), new Vector2(140f, 54f));

        // ── FooterBar ─────────────────────────────────────────
        var footer = MakePanel(panel, "FooterBar", FooterBg);
        BottomFull(footer.GetComponent<RectTransform>(), 0, FooterH);

        var reincBtn = BuildReincBtn(footer);

        // ── ScrollArea ────────────────────────────────────────
        var scrollGo = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(panel.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(0, FooterH);
        scrollRt.offsetMax = new Vector2(0, -HeaderH);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;

        var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;

        var glg = content.GetComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(CardW, CardH);
        glg.spacing         = new Vector2(GridSpacing, GridSpacing);
        glg.padding         = new RectOffset(GridPad, GridPad, GridPad, GridPad);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = GridCols;
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperCenter;

        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.content      = contentRt;
        sr.viewport     = vpRt;
        sr.horizontal   = false;
        sr.vertical     = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        // ── RelicCardTemplate (비활성) ────────────────────────
        var cardTemplate = BuildCardTemplate("RelicCardTemplate");
        cardTemplate.transform.SetParent(panel.transform, false);
        cardTemplate.SetActive(false);

        // ── SerializedObject 연결 ─────────────────────────────
        TextMeshProUGUI reincLabelTmp = null;
        foreach (var t in reincBtn.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.gameObject.name == "RightText") { reincLabelTmp = t; break; }

        var so = new SerializedObject(ui);
        so.Update();
        SetObj(so, "_pointText",      pointTmp);
        SetObj(so, "_scrollContent",  content.transform);
        SetObj(so, "_cardTemplate",   cardTemplate);
        SetObj(so, "_reincarnateBtn", reincBtn.GetComponent<Button>());
        SetObj(so, "_reincLabel",     reincLabelTmp);
        so.ApplyModifiedProperties();

        return panel;
    }

    // ── 환생 버튼 — 절대 위치 방식 (HLG 제거, 100×100 버그 방지) ──

    static GameObject BuildReincBtn(GameObject footer)
    {
        var btn = new GameObject("ReincarnateBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(footer.transform, false);
        btn.GetComponent<Image>().color = ReincarnateColor;

        var btnRt = btn.GetComponent<RectTransform>();
        btnRt.anchorMin        = new Vector2(0.08f, 0.5f);
        btnRt.anchorMax        = new Vector2(0.92f, 0.5f);
        btnRt.pivot            = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = Vector2.zero;
        btnRt.sizeDelta        = new Vector2(0f, BtnH);

        // LeftText: 왼쪽 절반, 우측 정렬
        var leftGo  = new GameObject("LeftText", typeof(RectTransform), typeof(TextMeshProUGUI));
        leftGo.transform.SetParent(btn.transform, false);
        var leftTmp = leftGo.GetComponent<TextMeshProUGUI>();
        leftTmp.text      = "즉시 환생";
        leftTmp.fontSize  = UIScale.FontMd;
        leftTmp.fontStyle = FontStyles.Bold;
        leftTmp.alignment = TextAlignmentOptions.Right;
        leftTmp.color     = Color.white;
        var leftRt = leftGo.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0.5f, 1f);
        leftRt.offsetMin = new Vector2(16f, 0f);
        leftRt.offsetMax = new Vector2(-24f, 0f);

        // ReincPtIcon: 중앙 고정 32×32
        var iconGo  = new GameObject("ReincPtIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(btn.transform, false);
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        var reincSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ReincIcon);
        if (reincSprite != null) iconImg.sprite = reincSprite;
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
        iconRt.pivot            = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta        = new Vector2(32f, 32f);

        // RightText: 오른쪽 절반, 좌측 정렬
        var rightGo  = new GameObject("RightText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rightGo.transform.SetParent(btn.transform, false);
        var rightTmp = rightGo.GetComponent<TextMeshProUGUI>();
        rightTmp.text      = "25pt 획득";
        rightTmp.fontSize  = UIScale.FontMd;
        rightTmp.fontStyle = FontStyles.Bold;
        rightTmp.alignment = TextAlignmentOptions.Left;
        rightTmp.color     = GoldColor;
        var rightRt = rightGo.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.5f, 0f);
        rightRt.anchorMax = new Vector2(1f, 1f);
        rightRt.offsetMin = new Vector2(24f, 0f);
        rightRt.offsetMax = new Vector2(-16f, 0f);

        // InactiveText: 비활성 시 전체 너비 중앙 표시 (기본 숨김)
        var inactGo  = new GameObject("InactiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        inactGo.transform.SetParent(btn.transform, false);
        var inactTmp = inactGo.GetComponent<TextMeshProUGUI>();
        inactTmp.text      = $"스테이지 {ReincarnationData.ReincarnateMinStage} 이상 시 활성화";
        inactTmp.fontSize  = UIScale.FontMd;
        inactTmp.fontStyle = FontStyles.Bold;
        inactTmp.alignment = TextAlignmentOptions.Center;
        inactTmp.color     = new Color(0.75f, 0.75f, 0.75f);
        var inactRt = inactGo.GetComponent<RectTransform>();
        inactRt.anchorMin = Vector2.zero;
        inactRt.anchorMax = Vector2.one;
        inactRt.offsetMin = new Vector2(16f, 0f);
        inactRt.offsetMax = new Vector2(-16f, 0f);
        inactGo.SetActive(false);

        return btn;
    }

    // ── 카드 템플릿 (260×280) ────────────────────────────────

    static GameObject BuildCardTemplate(string name)
    {
        var card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.GetComponent<Image>().color = CardBg;
        var le = card.AddComponent<LayoutElement>();
        le.minWidth  = CardW;
        le.minHeight = CardH;
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(CardW, CardH);

        // 희귀도 상단 바
        var rarityBorder = MakeImg(card, "RarityBorder", new Color(0.4f, 0.4f, 0.4f));
        TopFull(rarityBorder.rectTransform, 0, 6, 0, 0);

        // 아이콘 배경 96×96
        var iconBg = MakeImg(card, "IconBg", new Color(0.05f, 0.05f, 0.10f));
        TopCenter(iconBg.rectTransform, 8, 96, 96);

        var iconImg = MakeImg(iconBg.gameObject, "IconImage", Color.white);
        iconImg.preserveAspect = true;
        StretchFull(iconImg.rectTransform, 4, 4, 4, 4);

        // 레벨 뱃지 (우하단 오버레이)
        var badge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(iconBg.gameObject.transform, false);
        badge.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin        = new Vector2(1f, 0f);
        badgeRt.anchorMax        = new Vector2(1f, 0f);
        badgeRt.pivot            = new Vector2(1f, 0f);
        badgeRt.anchoredPosition = Vector2.zero;
        badgeRt.sizeDelta        = new Vector2(60f, 26f);

        var levelTmp = MakeTMP(badge, "LevelText", "Lv.1", 24f, FontStyles.Bold);
        levelTmp.color            = new Color(0.7f, 0.9f, 1f);
        levelTmp.textWrappingMode = TextWrappingModes.NoWrap;
        StretchFull(levelTmp.rectTransform, 2, 1, 2, 1);

        // 이름 (top 108, h 32)
        var nameTmp = MakeTMP(card, "NameText", "유물 이름", UIScale.FontSm, FontStyles.Bold);
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = 22f;
        nameTmp.fontSizeMax      = UIScale.FontSm;
        TopFull(nameTmp.rectTransform, 108, 32, 6, 6);

        // 설명 배경
        var descBg = MakeImg(card, "DescBg", new Color(0f, 0f, 0.05f, 0.50f));
        TopFull(descBg.rectTransform, 144, 84, 3, 3);

        // 설명 텍스트 (auto-size, 줄임 없이 축소 방식)
        var descTmp = MakeTMP(card, "DescText", "효과 설명", UIScale.FontSm, FontStyles.Normal);
        descTmp.color            = new Color(0.92f, 0.92f, 0.88f);
        descTmp.enableAutoSizing = true;
        descTmp.fontSizeMin      = 20f;
        descTmp.fontSizeMax      = UIScale.FontSm;
        descTmp.overflowMode     = TextOverflowModes.Truncate;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        TopFull(descTmp.rectTransform, 144, 84, 8, 8);

        // 비용 텍스트 (좌하단)
        var costTmp = MakeTMP(card, "CostText", "1pt", UIScale.FontSm, FontStyles.Bold);
        costTmp.color     = GoldColor;
        costTmp.alignment = TextAlignmentOptions.Center;
        BottomHalf(costTmp.rectTransform, 8, 34, 0f, 0.5f, 6, 3);

        // 강화 버튼 (우하단)
        var upgradeBtn = MakeBtn(card, "UpgradeBtn", "강화", UpgradeBtnColor, UIScale.FontSm);
        BottomHalf(upgradeBtn.GetComponent<RectTransform>(), 8, 34, 0.5f, 1f, 3, 6);

        return card;
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

    static GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Image MakeImg(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text, float size, FontStyles style)
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

    static GameObject MakeBtn(GameObject parent, string name, string label, Color bgColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    // ── RectTransform 헬퍼 ───────────────────────────────────

    static void TopFull(RectTransform rt, float yFromTop, float height, float lPad = 8, float rPad = 8)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(lPad,  -(yFromTop + height));
        rt.offsetMax = new Vector2(-rPad, -yFromTop);
    }

    static void TopCenter(RectTransform rt, float yFromTop, float width, float height)
    {
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta        = new Vector2(width, height);
    }

    static void BottomFull(RectTransform rt, float yFromBottom, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.offsetMin = new Vector2(0, yFromBottom);
        rt.offsetMax = new Vector2(0, yFromBottom + height);
    }

    static void BottomHalf(RectTransform rt, float yFromBottom, float height,
                           float xMin, float xMax, float lPad = 4, float rPad = 4)
    {
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 0f);
        rt.offsetMin = new Vector2(lPad,  yFromBottom);
        rt.offsetMax = new Vector2(-rPad, yFromBottom + height);
    }

    static void StretchFull(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

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
