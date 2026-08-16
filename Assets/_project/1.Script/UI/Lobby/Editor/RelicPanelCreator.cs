using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RelicPanelCreator.cs
//  Tools > Project K > 프리팹 생성 > 로비 > RelicPanel
//
//  저장: Assets/_project/2.Prefabs/UI/Lobby/RelicPanel.prefab
//
//  ■ 왜 다시 짰나
//    · 카드 260×280 에 4열이라 설명이 3~4줄로 접혀 읽히지 않았다.
//      유물은 "무슨 효과인지" 를 보고 고르는 화면인데 그게 가장 안 보였다.
//    · 하단 200px 을 즉시 환생 버튼이 통째로 먹었다. 대부분의 시간 동안
//      "스테이지 5 이상 시 활성화" 라는 **누를 수 없는 버튼**이 자리만 차지했다.
//      → 제거하고 그 높이를 카드 영역에 돌려줬다.
//    · 팝업(HeroDetail·Disassemble·AbilityList)과 색·구분선·버튼 규칙이 달라
//      같은 게임 화면처럼 안 보였다.
//
//  ■ 새 레이아웃 (팝업 계열과 통일)
//    Header  H=136   ◆ 유 물 | 유물     [환생 포인트]        [뒤로]
//    Body            카드 그리드 5열 (스크롤)
//    Footer  H=84    유물 초기화 (우측)
//
//  ⚠ 카드·버튼의 자식 이름을 바꾸지 말 것
//    RelicPanelUI 가 이름으로 찾는다:
//      "RarityBorder" / "IconBg/IconImage" / "IconBg/LevelBadge"
//      "NameText" / "DescText" / "CostText" / "UpgradeBtn"
// ============================================================

public static class RelicPanelCreator
{
    const string SavePath  = "Assets/_project/2.Prefabs/UI/Lobby/RelicPanel.prefab";
    const string ReincIcon = "Assets/_project/3.Textures/Icons/Items/item_reincarnation_point.png";

    // ── 치수 ─────────────────────────────────────────────────
    const float HeaderH = 136f;
    const float FooterH =  84f;
    const float SidePad =  32f;

    // 카드 — 1920 기준 5열. 설명을 두 줄 이상 담으려면 세로를 키워야 한다.
    const int   GridCols    = 5;
    const int   GridSpacing = 12;
    const int   GridPad     = 10;
    const float CardW       = 344f;   // (1920 - 64(좌우) - 20(패딩) - 48(간격)) / 5
    const float CardH       = 330f;

    // ── 색상 (팝업 계열과 같은 팔레트 — 유물은 청록) ──────────
    static readonly Color PanelBg     = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color HeaderBg    = new Color(0.06f,  0.14f,  0.16f,  1f);
    static readonly Color AccentTeal  = new Color(0.30f,  0.82f,  0.84f,  1f);
    static readonly Color TagColor    = new Color(0.62f,  0.94f,  0.96f,  1f);
    static readonly Color TitleColor  = new Color(1.00f,  0.94f,  0.86f,  1f);
    static readonly Color TitleShadow = new Color(0.02f,  0.04f,  0.05f,  0.85f);

    static readonly Color GridBg      = new Color(0.055f, 0.06f,  0.105f, 1f);
    static readonly Color CardBg      = new Color(0.125f, 0.13f,  0.205f, 1f);
    static readonly Color CardInner   = new Color(0.08f,  0.085f, 0.145f, 1f);
    static readonly Color IconPadBg   = new Color(0.20f,  0.26f,  0.32f,  1f);
    static readonly Color DividerC    = new Color(0.24f,  0.32f,  0.36f,  0.85f);
    static readonly Color LabelColor  = new Color(0.62f,  0.66f,  0.74f,  1f);
    static readonly Color DescColor   = new Color(0.80f,  0.85f,  0.92f,  1f);

    static readonly Color BackBtnC    = new Color(0.20f,  0.22f,  0.30f,  1f);
    static readonly Color ResetBtnC   = new Color(0.52f,  0.14f,  0.14f,  1f);
    static readonly Color UpgradeBtnC = new Color(0.16f,  0.50f,  0.34f,  1f);
    static readonly Color PointColor  = new Color(0.62f,  0.94f,  0.96f,  1f);
    static readonly Color LevelColor  = new Color(1.00f,  0.86f,  0.42f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Lobby + "RelicPanel", priority = ProjectKMenu.PrefabPrio + 15)]
    public static void CreateStandalone()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(UIScale.RefWidth, UIScale.RefHeight);

        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RelicPanelCreator] 저장: {SavePath}");
    }

    // ══════════════════════════════════════════════════════════
    //  빌더
    // ══════════════════════════════════════════════════════════

    public static GameObject Build(GameObject parent)
    {
        var panel = Go("RelicPanel", parent);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = PanelBg;

        var ui = panel.AddComponent<RelicPanelUI>();
        var so = new SerializedObject(ui);

        BuildHeader(panel, so);
        BuildGrid(panel, so);
        BuildFooter(panel, so);

        so.ApplyModifiedProperties();
        return panel;
    }

    // ── 헤더 ─────────────────────────────────────────────────

    static void BuildHeader(GameObject panel, SerializedObject so)
    {
        var header = Go("HeaderBar", panel);
        header.AddComponent<Image>().color = HeaderBg;
        EditorUIBuilder.AnchorTop(header.GetComponent<RectTransform>(), 0f, HeaderH);

        // ◆ 태그 (글리프 대신 도형 — UI 규칙 2)
        var tagRoot = Go("RelicTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(SidePad, -14f);
        tagRt.sizeDelta        = new Vector2(300f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "유 물", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        MakeTitle(header, "TitleShadow", TitleShadow, 3f);
        MakeTitle(header, "TitleText",   TitleColor,  0f);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentTeal;
        EditorUIBuilder.AnchorTop(accent.GetComponent<RectTransform>(), HeaderH, 3f);

        // 뒤로가기 — 우측 끝
        var backBtn = EditorUIBuilder.RaisedBtn(header, "BackBtn", BackBtnC, out var backBody);
        var bRt = backBtn.GetComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = new Vector2(1f, 0.5f);
        bRt.pivot     = new Vector2(1f, 0.5f);
        bRt.anchoredPosition = new Vector2(-SidePad, 0f);
        bRt.sizeDelta        = new Vector2(180f, UIScale.BtnFor(UIScale.FontMd));

        var backLbl = TMP(backBody, "Label", "뒤로", UIScale.FontMd, FontStyles.Bold);
        backLbl.color         = Color.white;
        backLbl.alignment     = TextAlignmentOptions.Center;
        backLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(backLbl.gameObject);
        SetObj(so, "_backBtn", backBtn);

        // 환생 포인트 — 유물을 사는 재화라 항상 보여야 한다
        var ptGroup = Go("PointGroup", header);
        var pgRt = ptGroup.GetComponent<RectTransform>();
        pgRt.anchorMin = pgRt.anchorMax = new Vector2(1f, 0.5f);
        pgRt.pivot     = new Vector2(1f, 0.5f);
        pgRt.anchoredPosition = new Vector2(-(SidePad + 180f + 24f), 0f);
        pgRt.sizeDelta        = new Vector2(260f, UIScale.RowMd);

        var ptIcon = EditorUIBuilder.Img(ptGroup, "PointIcon", PointColor);
        ptIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ReincIcon);
        if (ptIcon.sprite != null) ptIcon.color = Color.white;
        var piRt = ptIcon.rectTransform;
        piRt.anchorMin = piRt.anchorMax = new Vector2(0f, 0.5f);
        piRt.pivot     = new Vector2(0f, 0.5f);
        piRt.anchoredPosition = Vector2.zero;
        piRt.sizeDelta        = new Vector2(46f, 46f);

        var ptTmp = TMP(ptGroup, "PointText", "0", UIScale.FontMd, FontStyles.Bold);
        ptTmp.color            = PointColor;
        ptTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        ptTmp.raycastTarget    = false;
        ptTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var ptRt = ptTmp.rectTransform;
        ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one;
        ptRt.offsetMin = new Vector2(56f, 0f); ptRt.offsetMax = Vector2.zero;
        SetObj(so, "_pointText", ptTmp);
    }

    static void MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "유물", UIScale.FontLg, FontStyles.Bold);
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(SidePad + dy, -52f - dy);
        rt.sizeDelta        = new Vector2(700f, UIScale.RowLg);
    }

    // ── 카드 그리드 ──────────────────────────────────────────

    static void BuildGrid(GameObject panel, SerializedObject so)
    {
        var area = Go("ScrollArea", panel);
        area.AddComponent<Image>().color = GridBg;
        var aRt = area.GetComponent<RectTransform>();
        aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
        aRt.offsetMin = new Vector2(SidePad, FooterH);
        aRt.offsetMax = new Vector2(-SidePad, -(HeaderH + 14f));

        var viewport = Go("Viewport", area);
        EditorUIBuilder.Stretch(viewport);
        var vRt = viewport.GetComponent<RectTransform>();
        vRt.offsetMin = new Vector2(10f, 10f);
        vRt.offsetMax = new Vector2(-10f, -10f);

        var sr = viewport.AddComponent<ScrollRect>();
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);   // Mask 는 Graphic 필요
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = Go("Content", viewport);
        var cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot     = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta        = Vector2.zero;

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(CardW, CardH);
        grid.spacing         = new Vector2(GridSpacing, GridSpacing);
        grid.padding         = new RectOffset(GridPad, GridPad, GridPad, GridPad);
        grid.childAlignment  = TextAnchor.UpperCenter;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridCols;

        var fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content           = cRt;
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 42f;

        SetObj(so, "_scrollContent", content.transform);
        SetObj(so, "_cardTemplate",  BuildCardTemplate(content));
    }

    /// <summary>
    /// 유물 카드 템플릿 (비활성).
    /// 자식 이름은 RelicPanelUI 가 찾으므로 고정이다.
    /// </summary>
    static GameObject BuildCardTemplate(GameObject parent)
    {
        var card = Go("RelicCardTemplate", parent);
        card.AddComponent<Image>().color = CardBg;

        // 희귀도 띠 — 카드 최상단
        var rarity = EditorUIBuilder.Img(card, "RarityBorder", AccentTeal);
        var raRt = rarity.rectTransform;
        raRt.anchorMin = new Vector2(0f, 1f); raRt.anchorMax = new Vector2(1f, 1f);
        raRt.pivot     = new Vector2(0.5f, 1f);
        raRt.anchoredPosition = Vector2.zero;
        raRt.sizeDelta        = new Vector2(0f, 10f);

        const float IconSz = 80f;
        float y = 10f + 16f;

        // ── 이름 — 카드 폭 전체를 쓰는 첫 줄 ─────────────────
        //  ⚠ 아이콘 오른쪽에 두면 폭이 200px 뿐이라 한글 7자부터 넘친다.
        //    거기서 줄바꿈을 허용하면 단어 중간이 잘리고("질풍의 발걸/음"),
        //    안 넘치게 하려면 폰트를 26 까지 줄여야 해서 규칙 4 위반이다.
        //    → 이름을 맨 윗줄 전체 폭(312px)으로 올린다. FontSm 에서 한글 9자.
        var nameTmp = TMP(card, "NameText", "유물 이름", UIScale.FontMd, FontStyles.Bold);
        nameTmp.color            = Color.white;
        nameTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Overflow;
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontSm;
        nameTmp.fontSizeMax      = UIScale.FontMd;
        var nRt = nameTmp.rectTransform;
        nRt.anchorMin = nRt.anchorMax = new Vector2(0f, 1f);
        nRt.pivot     = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(16f, -y);
        nRt.sizeDelta        = new Vector2(CardW - 32f, UIScale.RowMd);

        y += UIScale.RowMd + 8f;

        // 아이콘 — 이름 아래 좌측
        var iconBg = Go("IconBg", card);
        iconBg.AddComponent<Image>().color = IconPadBg;
        var ibRt = iconBg.GetComponent<RectTransform>();
        ibRt.anchorMin = ibRt.anchorMax = new Vector2(0f, 1f);
        ibRt.pivot     = new Vector2(0f, 1f);
        ibRt.anchoredPosition = new Vector2(16f, -y);
        ibRt.sizeDelta        = new Vector2(IconSz, IconSz);

        var icon = EditorUIBuilder.Img(iconBg, "IconImage", Color.white);
        var icRt = icon.rectTransform;
        icRt.anchorMin = Vector2.zero; icRt.anchorMax = Vector2.one;
        icRt.offsetMin = new Vector2(8f, 8f); icRt.offsetMax = new Vector2(-8f, -8f);
        icon.preserveAspect = true;

        // ── 레벨 뱃지 — 아이콘 "바로 아래", 겹치지 않게 ──────
        //  ⚠ 예전엔 아이콘 위에 겹쳐 올려서 그림을 가리고 카드 밖으로 삐져나왔다.
        //    아이콘과 같은 폭으로 아래에 붙여 아이콘 열을 하나로 만든다.
        //    (RelicPanelUI 가 "IconBg/LevelBadge" 로 찾으므로 부모는 IconBg 유지)
        var badge = Go("LevelBadge", iconBg);
        badge.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.95f);
        var bgRt = badge.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0f);
        bgRt.pivot     = new Vector2(0.5f, 1f);
        bgRt.anchoredPosition = new Vector2(0f, -6f);
        bgRt.sizeDelta        = new Vector2(IconSz, UIScale.RowSm);

        var lvTmp = TMP(badge, "LevelText", "Lv.0", UIScale.FontSm, FontStyles.Bold);
        lvTmp.color            = LevelColor;
        lvTmp.alignment        = TextAlignmentOptions.Center;
        lvTmp.raycastTarget    = false;
        lvTmp.textWrappingMode = TextWrappingModes.NoWrap;
        // "Lv.10" 처럼 자릿수가 늘면 80px 을 넘는다 — 줄바꿈 대신 축소
        lvTmp.enableAutoSizing = true;
        lvTmp.fontSizeMin      = 28f;
        lvTmp.fontSizeMax      = UIScale.FontSm;
        EditorUIBuilder.Stretch(lvTmp.gameObject);

        // ── 설명 — 아이콘 오른쪽의 남는 폭을 쓴다 ────────────
        //  ⚠ 예전엔 아이콘 오른쪽이 통째로 비어 있었다(뱃지 한 줄뿐).
        //    설명을 그 자리로 올려 카드를 키우지 않고도 줄 수를 확보한다.
        //  ⚠ 예전엔 카드가 낮아 설명이 3~4줄로 접혔다.
        //    유물을 고르는 판단 근거가 설명이므로 여기가 가장 넓어야 한다.
        float btnH     = UIScale.BtnFor(UIScale.FontSm);
        float descLeft = 16f + IconSz + 16f;

        var descBg = Go("DescBg", card);
        descBg.AddComponent<Image>().color = CardInner;
        var dbRt = descBg.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f); dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.offsetMin = new Vector2(descLeft, btnH + 24f);
        dbRt.offsetMax = new Vector2(-16f, -y);

        var descTmp = TMP(card, "DescText", "", UIScale.FontSm, FontStyles.Normal);
        descTmp.color            = DescColor;
        descTmp.alignment        = TextAlignmentOptions.TopLeft;
        descTmp.raycastTarget    = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.lineSpacing      = 8f;
        // ⚠ Ellipsis 로 두면 긴 설명이 통째로 "..." 이 된다
        descTmp.overflowMode     = TextOverflowModes.Overflow;
        descTmp.enableAutoSizing = true;
        descTmp.fontSizeMin      = 28f;
        descTmp.fontSizeMax      = UIScale.FontSm;
        var dtRt = descTmp.rectTransform;
        dtRt.anchorMin = new Vector2(0f, 0f); dtRt.anchorMax = new Vector2(1f, 1f);
        dtRt.offsetMin = new Vector2(descLeft + 14f, btnH + 34f);
        dtRt.offsetMax = new Vector2(-30f, -(y + 10f));

        // ── 강화 버튼 (하단) ─────────────────────────────────
        var upBtn = EditorUIBuilder.RaisedBtn(card, "UpgradeBtn", UpgradeBtnC, out var upBody);
        var ubRt = upBtn.GetComponent<RectTransform>();
        ubRt.anchorMin = new Vector2(0f, 0f); ubRt.anchorMax = new Vector2(1f, 0f);
        ubRt.pivot     = new Vector2(0.5f, 0f);
        ubRt.offsetMin = new Vector2(16f, 14f);
        ubRt.offsetMax = new Vector2(-16f, 14f + btnH);

        var upLbl = TMP(upBody, "Label", "강화", UIScale.FontSm, FontStyles.Bold);
        upLbl.color         = Color.white;
        upLbl.alignment     = TextAlignmentOptions.MidlineLeft;
        upLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(upLbl.gameObject);
        upLbl.rectTransform.offsetMin = new Vector2(24f, 0f);

        // ── 강화 비용 — 버튼 우측 ────────────────────────────
        //  ⚠ 반드시 UpgradeBtn *다음에* 만든다.
        //    예전엔 아이콘 아래에 놓였는데 DescBg 가 나중에 그려지면서
        //    통째로 가려 비용이 화면에 아예 안 보였다.
        //    RelicPanelUI 는 card 의 직계 자식에서 "CostText" 를 찾으므로
        //    버튼 안에 넣지 못한다 — 버튼 위에 겹쳐 올린다.
        var costTmp = TMP(card, "CostText", "1 pt", UIScale.FontSm, FontStyles.Bold);
        costTmp.color            = PointColor;
        costTmp.alignment        = TextAlignmentOptions.MidlineRight;
        costTmp.raycastTarget    = false;
        costTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var coRt = costTmp.rectTransform;
        coRt.anchorMin = new Vector2(1f, 0f); coRt.anchorMax = new Vector2(1f, 0f);
        coRt.pivot     = new Vector2(1f, 0f);
        //  버튼은 아래로 눌리는 두께(lift)만큼 Body 가 떠 있다 — 라벨과 같은 높이에 맞춘다
        coRt.anchoredPosition = new Vector2(-40f, 14f + EditorUIBuilder.BtnLift);
        coRt.sizeDelta        = new Vector2(150f, btnH - EditorUIBuilder.BtnLift);

        card.SetActive(false);
        return card;
    }

    // ── 푸터 ─────────────────────────────────────────────────

    static void BuildFooter(GameObject panel, SerializedObject so)
    {
        // ⚠ 즉시 환생 버튼은 두지 않는다
        //   대부분의 시간 동안 "스테이지 5 이상 시 활성화" 라는 **누를 수 없는 버튼**이
        //   하단 200px 을 차지했다. 환생은 패배 시 결과 흐름에서 처리되므로
        //   여기 상시 버튼으로 둘 이유가 없다. 그 높이는 카드 영역이 가져갔다.
        //   (RelicPanelUI 는 _reincarnateBtn 이 null 이어도 안전하게 동작한다)

        var footer = Go("FooterBar", panel);
        var fRt = footer.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f); fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot     = new Vector2(0.5f, 0f);
        fRt.anchoredPosition = Vector2.zero;
        fRt.sizeDelta        = new Vector2(0f, FooterH);

        // 안내 — 유물이 영구 보상이라는 점을 좌측에 적어 둔다
        // 초기화 버튼 바로 옆이 이 문구의 유일한 설명 자리다 — 무엇이 사라지고
        // 무엇이 남는지 여기서 말해주지 않으면 누르기 전에 알 방법이 없다.
        var hint = TMP(footer, "Hint",
            "초기화하면 모든 유물이 0레벨로 돌아가고 강화에 쓴 포인트를 전액 돌려받습니다.",
            UIScale.FontSm, FontStyles.Normal);
        hint.color            = LabelColor;
        hint.alignment        = TextAlignmentOptions.MidlineLeft;
        hint.raycastTarget    = false;
        hint.textWrappingMode = TextWrappingModes.NoWrap;
        var hRt = hint.rectTransform;
        hRt.anchorMin = new Vector2(0f, 0f); hRt.anchorMax = new Vector2(0.6f, 1f);
        hRt.offsetMin = new Vector2(SidePad, 0f); hRt.offsetMax = Vector2.zero;

        var resetBtn = EditorUIBuilder.RaisedBtn(footer, "ResetBtn", ResetBtnC, out var rBody);
        var rbRt = resetBtn.GetComponent<RectTransform>();
        rbRt.anchorMin = rbRt.anchorMax = new Vector2(1f, 0.5f);
        rbRt.pivot     = new Vector2(1f, 0.5f);
        rbRt.anchoredPosition = new Vector2(-SidePad, 0f);
        rbRt.sizeDelta        = new Vector2(260f, UIScale.BtnFor(UIScale.FontMd));

        var rLbl = TMP(rBody, "Label", "유물 초기화", UIScale.FontMd, FontStyles.Bold);
        rLbl.color         = Color.white;
        rLbl.alignment     = TextAlignmentOptions.Center;
        rLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(rLbl.gameObject);
        SetObj(so, "_resetBtn", resetBtn);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    static GameObject Go(string name, GameObject parent) => EditorUIBuilder.Go(name, parent);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text,
                               float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "RelicPanelCreator");
}
