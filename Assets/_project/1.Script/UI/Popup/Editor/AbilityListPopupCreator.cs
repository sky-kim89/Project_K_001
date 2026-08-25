#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilityListPopupCreator.cs
//  Tools > Project K > 프리팹 생성 > 팝업 > AbilityList
//
//  저장: Assets/_project/2.Prefabs/UI/AbilityListPopup.prefab
//
//  ■ 왜 PopupPrefabCreator 에서 떼어냈나
//    거기는 BattleResult · Pause · Loading · AbilitySelect · ExpRow 까지
//    다섯 프리팹의 정본이다. 어빌리티 목록만 크게 손보려다 그 파일을 건드리면
//    나머지 다섯이 같이 위험해진다. EquipCompare · Disassemble 처럼 분리한다.
//
//  ■ 왜 다시 짰나 (이전 레이아웃의 문제)
//    · 좌측에 "선택 상세 + 총합" 을 세로로 눌러 담아 둘 다 좁았다.
//      어빌리티 설명은 두세 줄인데 칸이 모자라 스크롤을 또 굴려야 했다.
//    · 목록이 우측 한 줄짜리라 10개만 넘어도 스크롤이 길어졌다.
//    · 보유 개수가 헤더 구석 작은 글씨라 "몇 개 모았나" 가 안 읽혔다.
//
//  ■ 새 레이아웃 (HeroDetailPopup 과 같은 전체화면 톤)
//    전체 1840 × (캔버스높이-32), 2단
//    Header  H=136   ◆ 어빌리티 | 보유 어빌리티   [N개 보유]        [X]
//    Body
//      Left  1080   보유 목록 (2열 그리드 스크롤)
//      Right  720   선택 상세(아이콘·이름·대상·설명) + 합산 스탯
//
//  ⚠ 목록 아이템의 자식 이름을 바꾸지 말 것
//    AbilityListPopup 이 이름으로 찾는다:
//      "Icon" / "NameText" / "GradeText" / "TargetText" / "CountBadge"
//    바꾸면 조용히 null 이 되어 그 칸만 빈 채로 뜬다.
// ============================================================

public static class AbilityListPopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/AbilityListPopup.prefab";

    // ── 치수 ─────────────────────────────────────────────────
    const float PW       = 1840f;
    const float PVMargin =   16f;
    const float HeaderH  =  136f;
    const float BodyTop  =  156f;
    const float BodyBtm  =   26f;
    const float SidePad  =   40f;

    const float ColGap   =   20f;
    const float RightW   =  720f;
    const float LeftW    = PW - SidePad * 2f - RightW - ColGap;   // 1020

    const float ItemH    =  104f;
    const int   ListCols =    2;

    // ── 색상 (HeroDetailPopup 팔레트 — 어빌리티는 보라 계열) ──
    static readonly Color BgOverlay   = new Color(0f,     0f,     0f,     0.80f);
    static readonly Color PanelBg     = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder = new Color(0.44f,  0.32f,  0.72f,  1f);
    static readonly Color HeaderBg    = new Color(0.11f,  0.07f,  0.19f,  1f);
    static readonly Color AccentPurple= new Color(0.66f,  0.48f,  1.00f,  1f);
    static readonly Color TagColor    = new Color(0.80f,  0.68f,  1.00f,  1f);
    static readonly Color TitleColor  = new Color(1.00f,  0.94f,  0.86f,  1f);
    static readonly Color TitleShadow = new Color(0.03f,  0.02f,  0.05f,  0.85f);

    static readonly Color SectionBg   = new Color(0.10f,  0.105f, 0.175f, 1f);
    static readonly Color ListBg      = new Color(0.055f, 0.06f,  0.105f, 1f);
    static readonly Color ItemBg      = new Color(0.115f, 0.12f,  0.195f, 1f);
    static readonly Color DividerC    = new Color(0.28f,  0.26f,  0.40f,  0.85f);
    static readonly Color DividerLbl  = new Color(0.74f,  0.70f,  0.90f,  1f);
    static readonly Color LabelColor  = new Color(0.60f,  0.62f,  0.74f,  1f);
    static readonly Color DescColor   = new Color(0.78f,  0.82f,  0.92f,  1f);
    static readonly Color CountColor  = new Color(0.86f,  0.76f,  1.00f,  1f);
    static readonly Color TotalStatColor = new Color(1.00f, 0.86f, 0.42f, 1f);   // 합산 수치 — 금빛
    static readonly Color CloseBtnC   = new Color(0.50f,  0.14f,  0.14f,  1f);
    static readonly Color BadgeBg     = new Color(0.20f,  0.16f,  0.30f,  1f);
    // 선택 카드는 목록보다 한 단계 밝게 — 어느 쪽을 보고 있는지 구분된다
    static readonly Color CardBg      = new Color(0.155f, 0.145f, 0.255f, 1f);
    static readonly Color IconPadBg   = new Color(0.24f,  0.21f,  0.36f,  1f);
    static readonly Color TotalLblColor = new Color(0.95f, 0.88f, 1.00f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "AbilityList", priority = ProjectKMenu.PrefabPrio + 35)]
    public static void Create()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Build();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AbilityListPopupCreator] 저장: {PrefabPath} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    static GameObject Build()
    {
        var root = EditorUIBuilder.Panel(null, "AbilityListPopup", BgOverlay);
        EditorUIBuilder.Stretch(root);

        var popup = root.AddComponent<AbilityListPopup>();
        var so    = new SerializedObject(popup);
        EditorUIBuilder.SetEnum(so, "_popupType", (int)PopupType.AbilityList, "AbilityListPopupCreator");

        // 테두리는 패널의 앞 형제 (자식이면 팝업을 덮는다, UI 규칙 3)
        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        StretchV(border.GetComponent<RectTransform>(), PW + 6f, PVMargin - 3f);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        StretchV(panel.GetComponent<RectTransform>(), PW, PVMargin);

        BuildHeader(panel, so);
        BuildList(panel, so);
        BuildDetail(panel, so);

        so.ApplyModifiedProperties();
        return root;
    }

    // ══════════════════════════════════════════════════════════
    //  헤더
    // ══════════════════════════════════════════════════════════

    static void BuildHeader(GameObject panel, SerializedObject so)
    {
        var header = Go("Header", panel);
        header.AddComponent<Image>().color = HeaderBg;
        EditorUIBuilder.AnchorTop(header.GetComponent<RectTransform>(), 0f, HeaderH);

        var tagRoot = Go("AbilityTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(340f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "어 빌 리 티", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        MakeTitle(header, "TitleShadow", TitleShadow, 3f);
        MakeTitle(header, "TitleText",   TitleColor,  0f);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentPurple;
        EditorUIBuilder.AnchorTop(accent.GetComponent<RectTransform>(), HeaderH, 3f);

        var closeBtn = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseBtnC, out var body);
        var cRt = closeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(1f, 0.5f);
        cRt.pivot     = new Vector2(1f, 0.5f);
        cRt.anchoredPosition = new Vector2(-24f, 0f);
        cRt.sizeDelta        = new Vector2(76f, 76f);
        Center(EditorUIBuilder.XMark(body, "Mark", UIScale.FontMd, Color.white));
        SetObj(so, "_closeBtn", closeBtn);

        // 도움말 — 닫기 버튼 왼쪽
        EditorUIBuilder.InfoBtn(header, TutorialId.HelpAbility, 76f, -24f);

        // 보유 개수 — 이 팝업의 핵심 정보라 크게 박는다
        var countBg = Go("CountBg", header);
        countBg.AddComponent<Image>().color = BadgeBg;
        var cbRt = countBg.GetComponent<RectTransform>();
        cbRt.anchorMin = cbRt.anchorMax = new Vector2(1f, 0.5f);
        cbRt.pivot     = new Vector2(1f, 0.5f);
        cbRt.anchoredPosition = new Vector2(-(EditorUIBuilder.HeaderRightBlock(76f, 24f) + 20f), 0f);
        cbRt.sizeDelta        = new Vector2(240f, UIScale.BtnFor(UIScale.FontMd));

        var countTmp = TMP(countBg, "CountText", "0개 보유", UIScale.FontMd, FontStyles.Bold);
        countTmp.color            = CountColor;
        countTmp.alignment        = TextAlignmentOptions.Center;
        countTmp.raycastTarget    = false;
        countTmp.textWrappingMode = TextWrappingModes.NoWrap;
        EditorUIBuilder.Stretch(countTmp.gameObject);
        SetObj(so, "_headerCountTmp", countTmp);
    }

    static void MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "보유 어빌리티", UIScale.FontLg, FontStyles.Bold);
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f + dy, -52f - dy);
        rt.sizeDelta        = new Vector2(900f, UIScale.RowLg);
    }

    // ══════════════════════════════════════════════════════════
    //  좌측 — 보유 목록 (2열 그리드)
    // ══════════════════════════════════════════════════════════

    static void BuildList(GameObject panel, SerializedObject so)
    {
        var col = Column(panel, "ListColumn", SidePad, LeftW);

        Divider(col, "ListDivider", 0f, "보유 목록");

        var listBg = Go("ListBg", col);
        listBg.AddComponent<Image>().color = ListBg;
        StretchFrom(listBg.GetComponent<RectTransform>(), 46f, 0f);

        var scroll = Go("Scroll", listBg);
        EditorUIBuilder.Stretch(scroll);
        var sRt = scroll.GetComponent<RectTransform>();
        sRt.offsetMin = new Vector2(12f, 12f);
        sRt.offsetMax = new Vector2(-12f, -12f);

        var sr = scroll.AddComponent<ScrollRect>();
        scroll.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);   // Mask 는 Graphic 필요
        scroll.AddComponent<Mask>().showMaskGraphic = false;

        var content = Go("Content", scroll);
        var coRt = content.GetComponent<RectTransform>();
        coRt.anchorMin = new Vector2(0f, 1f);
        coRt.anchorMax = new Vector2(1f, 1f);
        coRt.pivot     = new Vector2(0.5f, 1f);
        coRt.anchoredPosition = Vector2.zero;
        coRt.sizeDelta        = Vector2.zero;

        // 2열 — 한 화면에 두 배로 보인다. 셀 폭은 부모 폭에서 계산한다
        float cellW = (LeftW - 24f - 8f - 10f) / ListCols;

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(cellW, ItemH);
        grid.spacing         = new Vector2(10f, 10f);
        grid.padding         = new RectOffset(4, 4, 4, 4);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = ListCols;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content           = coRt;
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;

        SetObj(so, "_listContent", content.transform);
        SetObj(so, "_listItemTemplate", BuildItemTemplate(content, cellW));
    }

    /// <summary>
    /// 목록 아이템 템플릿 (비활성).
    /// 자식 이름은 AbilityListPopup 이 찾으므로 고정이다.
    /// </summary>
    static GameObject BuildItemTemplate(GameObject parent, float width)
    {
        var item = Go("ItemTemplate", parent);
        item.AddComponent<Image>().color = ItemBg;   // 런타임이 등급색으로 덮어쓴다
        item.AddComponent<Button>();

        const float IconSz = 72f;

        var icon = EditorUIBuilder.Img(item, "Icon", new Color(0.3f, 0.28f, 0.42f));
        var icRt = icon.rectTransform;
        icRt.anchorMin = icRt.anchorMax = new Vector2(0f, 0.5f);
        icRt.pivot     = new Vector2(0f, 0.5f);
        icRt.anchoredPosition = new Vector2(16f, 0f);
        icRt.sizeDelta        = new Vector2(IconSz, IconSz);
        icon.preserveAspect = true;

        float textLeft = 16f + IconSz + 16f;

        // 이름 — 위쪽 한 줄
        var nameTmp = TMP(item, "NameText", "어빌리티", UIScale.FontMd, FontStyles.Bold);
        nameTmp.color            = Color.white;
        nameTmp.alignment        = TextAlignmentOptions.BottomLeft;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Ellipsis;
        var nRt = nameTmp.rectTransform;
        nRt.anchorMin = new Vector2(0f, 0.5f); nRt.anchorMax = new Vector2(1f, 1f);
        nRt.offsetMin = new Vector2(textLeft, 0f);
        nRt.offsetMax = new Vector2(-130f, -14f);

        // 등급 · 대상 — 아래쪽 한 줄에 나란히
        var gradeTmp = TMP(item, "GradeText", "등급", UIScale.FontSm, FontStyles.Bold);
        gradeTmp.color            = AccentPurple;
        gradeTmp.alignment        = TextAlignmentOptions.TopLeft;
        gradeTmp.raycastTarget    = false;
        gradeTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var gRt = gradeTmp.rectTransform;
        gRt.anchorMin = new Vector2(0f, 0f); gRt.anchorMax = new Vector2(0f, 0.5f);
        gRt.pivot     = new Vector2(0f, 1f);
        gRt.anchoredPosition = new Vector2(textLeft, -2f);
        gRt.sizeDelta        = new Vector2(120f, UIScale.RowSm);

        var targetTmp = TMP(item, "TargetText", "대상", UIScale.FontSm, FontStyles.Normal);
        targetTmp.color            = LabelColor;
        targetTmp.alignment        = TextAlignmentOptions.TopLeft;
        targetTmp.raycastTarget    = false;
        targetTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var tRt = targetTmp.rectTransform;
        tRt.anchorMin = new Vector2(0f, 0f); tRt.anchorMax = new Vector2(0f, 0.5f);
        tRt.pivot     = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(textLeft + 130f, -2f);
        tRt.sizeDelta        = new Vector2(200f, UIScale.RowSm);

        // 레벨 뱃지 — 우측. TMP 가 이 오브젝트에 직접 붙어야 한다
        // (런타임이 CountBadge 에서 곧바로 TextMeshProUGUI 를 가져간다)
        var badge = TMP(item, "CountBadge", "Lv 1/3", UIScale.FontSm, FontStyles.Bold);
        badge.color            = Color.white;
        badge.alignment        = TextAlignmentOptions.Center;
        badge.raycastTarget    = false;
        badge.textWrappingMode = TextWrappingModes.NoWrap;
        var bRt = badge.rectTransform;
        bRt.anchorMin = bRt.anchorMax = new Vector2(1f, 0.5f);
        bRt.pivot     = new Vector2(1f, 0.5f);
        bRt.anchoredPosition = new Vector2(-16f, 0f);
        bRt.sizeDelta        = new Vector2(110f, UIScale.RowSm);

        item.SetActive(false);
        return item;
    }

    // ══════════════════════════════════════════════════════════
    //  우측 — 선택 상세 + 합산 스탯
    // ══════════════════════════════════════════════════════════

    static void BuildDetail(GameObject panel, SerializedObject so)
    {
        var col = Column(panel, "DetailColumn", SidePad + LeftW + ColGap, RightW);

        Divider(col, "DetailDivider", 0f, "선택한 어빌리티");

        // ── 상단 카드: 아이콘 + 이름/등급/대상 ────────────────
        // ⚠ 칸 높이는 폰트 한 줄(UIScale.Line)보다 작으면 안 된다 (UI 규칙 5)
        //   예전엔 이름 칸이 65px 인데 FontLg 한 줄이 70px 이라 통째로 잘려 안 보였다.
        // ── 상세 카드 높이는 '효과' 칸에서 빌려 온 것이다 ─────
        //
        //  ⚠ 효과 설명이 잘려 있었다 (2026-08-21)
        //    설명 칸(DescBg)이 204 뿐이라 "발동 조건" 한 줄을 쓰고 나면
        //    설명에 121 — 2.3줄밖에 안 남았다. 두 줄짜리 설명이 문장 중간에서
        //    끊겨, 화면에는 마치 효과가 아예 없는 것처럼 보였다.
        //
        //  ── 필요한 높이 계산 (RowMd = 53) ──
        //    발동 조건 1줄 53 + 줄간격 6 + 설명 4줄 212 = 271
        //    + Viewport 위아래 여백 24                   = 295
        //
        //  그래서 ① 카드 178 → 158 (아이콘 132 → 96)
        //         ② 카드·구분선 사이 여백 16 → 10, 구분선·설명칸 46 → 40
        //         ③ 하단 합산 칸 360 → 294
        //  로 296 을 만들었다. ③ 은 스크롤 목록이라 한 행쯤 덜 보여도 되지만,
        //  설명은 짧고 고정이라 잘리면 정보가 통째로 사라진다 — 그쪽을 살렸다.
        const float NameH   = 74f;                      // FontLg 한 줄(70) + 여유
        const float SubH    = 56f;                      // 등급·대상 한 줄 (RowMd 53 이상)
        const float IconSz  = 96f;                      // UIScale.IconMd — 132 에서 줄였다
        const float CardPad = 12f;                      // 카드 위·아래 여백
        const float CardH   = CardPad * 2f + NameH + 4f + SubH;   // 158

        var card = Go("DetailCard", col);
        card.AddComponent<Image>().color = CardBg;
        EditorUIBuilder.AnchorTop(card.GetComponent<RectTransform>(), 46f, CardH, padH: 4f);

        // 등급 바 — 좌측 세로. 색만으로 등급이 읽힌다
        var gradeBar = EditorUIBuilder.Img(card, "GradeBar", AccentPurple);
        var gbRt = gradeBar.rectTransform;
        gbRt.anchorMin = new Vector2(0f, 0f); gbRt.anchorMax = new Vector2(0f, 1f);
        gbRt.pivot     = new Vector2(0f, 0.5f);
        gbRt.anchoredPosition = Vector2.zero;
        gbRt.sizeDelta        = new Vector2(8f, 0f);
        SetObj(so, "_infoGradeBar", gradeBar);

        // 아이콘 뒤판 — 회색 아이콘이 어두운 배경에 묻히지 않게 받쳐 준다
        var iconPad = EditorUIBuilder.Img(card, "IconPad", IconPadBg);
        var ipRt = iconPad.rectTransform;
        ipRt.anchorMin = ipRt.anchorMax = new Vector2(0f, 0.5f);
        ipRt.pivot     = new Vector2(0f, 0.5f);
        ipRt.anchoredPosition = new Vector2(26f, 0f);
        ipRt.sizeDelta        = new Vector2(IconSz + 12f, IconSz + 12f);

        var icon = EditorUIBuilder.Img(card, "InfoIcon", Color.white);
        var icRt = icon.rectTransform;
        icRt.anchorMin = icRt.anchorMax = new Vector2(0f, 0.5f);
        icRt.pivot     = new Vector2(0f, 0.5f);
        icRt.anchoredPosition = new Vector2(30f, 0f);
        icRt.sizeDelta        = new Vector2(IconSz, IconSz);
        icon.preserveAspect = true;
        SetObj(so, "_infoIcon", icon);

        float tx = 30f + IconSz + 22f;

        var nameTmp = TMP(card, "InfoName", "어빌리티를 선택하세요", UIScale.FontLg, FontStyles.Bold);
        nameTmp.color            = Color.white;
        nameTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Ellipsis;
        // 자동 축소 — 이름이 길어도 잘리지 않고 폰트가 줄어든다
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontMd;
        nameTmp.fontSizeMax      = UIScale.FontLg;
        var nRt = nameTmp.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot     = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(tx, -CardPad);
        nRt.sizeDelta        = new Vector2(-(tx + 24f), NameH);
        SetObj(so, "_infoNameTmp", nameTmp);

        var gradeTmp = TMP(card, "InfoGrade", "", UIScale.FontMd, FontStyles.Bold);
        gradeTmp.color         = AccentPurple;
        gradeTmp.alignment     = TextAlignmentOptions.TopLeft;
        gradeTmp.raycastTarget = false;
        var grRt = gradeTmp.rectTransform;
        grRt.anchorMin = grRt.anchorMax = new Vector2(0f, 1f);
        grRt.pivot     = new Vector2(0f, 1f);
        grRt.anchoredPosition = new Vector2(tx, -(CardPad + NameH + 4f));
        grRt.sizeDelta        = new Vector2(160f, SubH);
        SetObj(so, "_infoGradeTmp", gradeTmp);

        var targetTmp = TMP(card, "InfoTarget", "", UIScale.FontMd, FontStyles.Normal);
        targetTmp.color         = LabelColor;
        targetTmp.alignment     = TextAlignmentOptions.TopLeft;
        targetTmp.raycastTarget = false;
        var tgRt = targetTmp.rectTransform;
        tgRt.anchorMin = tgRt.anchorMax = new Vector2(0f, 1f);
        tgRt.pivot     = new Vector2(0f, 1f);
        tgRt.anchoredPosition = new Vector2(tx + 170f, -(CardPad + NameH + 4f));
        tgRt.sizeDelta        = new Vector2(340f, SubH);
        SetObj(so, "_infoTargetTmp", targetTmp);

        // ── 설명·효과 (스크롤) ───────────────────────────────
        // 카드 아래 여백 16 → 10, 구분선 아래 여백 46 → 40 (카드 상수 주석의 계산 참고)
        float descTop = 46f + CardH + 10f;
        Divider(col, "EffectDivider", descTop, "효  과");

        var descBg = Go("DescBg", col);
        descBg.AddComponent<Image>().color = SectionBg;
        var dbRt = descBg.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f); dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.pivot     = new Vector2(0.5f, 1f);
        // 866(칼럼) - 254(위) - 310(아래) = 302 → Viewport 278
        //   발동 조건 53 + 간격 6 + 설명 4줄 212 = 271 ≤ 278 ✓ (여유 7)
        // ⚠ 이 세 숫자를 따로 만지지 말 것 — 하나만 바꾸면 설명이 다시 잘린다
        dbRt.offsetMin = new Vector2(4f, 310f);           // 아래 합산 스탯 자리를 남긴다
        dbRt.offsetMax = new Vector2(-4f, -(descTop + 40f));

        var descScroll = MakeScroll(descBg, out var descContent);
        SetObj(so, "_infoStatContent", descContent.transform);
        SetObj(so, "_infoStatTemplate", MakeStatTemplate(descContent, "StatTemplate", DescColor));

        // ── 합산 스탯 (하단 고정) ────────────────────────────
        var totalBg = Go("TotalBg", col);
        totalBg.AddComponent<Image>().color = SectionBg;
        var tbRt = totalBg.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 0f); tbRt.anchorMax = new Vector2(1f, 0f);
        tbRt.pivot     = new Vector2(0.5f, 0f);
        tbRt.offsetMin = new Vector2(4f, 0f);
        // 360 → 294. 설명 칸에 66 을 넘겼다 — 여기는 스크롤 목록이라 한 행쯤
        // 덜 보여도 굴리면 되지만, 설명은 짧고 고정이라 잘리면 정보가 통째로 사라진다.
        // (DescBg 의 아래 여백 310 과 16 만큼 벌어진다)
        tbRt.offsetMax = new Vector2(-4f, 294f);

        var totalLbl = TMP(totalBg, "TotalLabel", "보유 어빌리티 합산 효과", UIScale.FontMd, FontStyles.Bold);
        totalLbl.color         = TotalLblColor;
        totalLbl.alignment     = TextAlignmentOptions.MidlineLeft;
        totalLbl.raycastTarget = false;
        var tlRt = totalLbl.rectTransform;
        tlRt.anchorMin = new Vector2(0f, 1f); tlRt.anchorMax = new Vector2(1f, 1f);
        tlRt.pivot     = new Vector2(0.5f, 1f);
        tlRt.anchoredPosition = new Vector2(0f, -10f);
        tlRt.sizeDelta        = new Vector2(-40f, UIScale.RowMd);

        var totalArea = Go("TotalArea", totalBg);
        var taRt = totalArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(10f, 10f);
        taRt.offsetMax = new Vector2(-10f, -(10f + UIScale.RowMd + 8f));

        var totalScroll = MakeScroll(totalArea, out var totalContent);
        SetObj(so, "_totalStatContent", totalContent.transform);
        SetObj(so, "_totalStatTemplate", MakeStatTemplate(totalContent, "TotalStatTemplate", TotalStatColor));
    }

    // ══════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════

    /// <summary>세로 스크롤 영역을 만들고 Content 를 돌려준다 (세로 레이아웃 + 자동 높이).</summary>
    static ScrollRect MakeScroll(GameObject parent, out GameObject content)
    {
        var view = Go("Viewport", parent);
        EditorUIBuilder.Stretch(view);
        var vRt = view.GetComponent<RectTransform>();
        vRt.offsetMin = new Vector2(16f, 12f);
        vRt.offsetMax = new Vector2(-16f, -12f);

        var sr = view.AddComponent<ScrollRect>();
        view.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        view.AddComponent<Mask>().showMaskGraphic = false;

        content = Go("Content", view);
        var cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot     = new Vector2(0.5f, 1f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta        = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 6f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        var fit = content.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content           = cRt;
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 36f;
        return sr;
    }

    /// <summary>스탯 한 줄 템플릿 (비활성). 런타임이 Instantiate 해서 채운다.</summary>
    static TextMeshProUGUI MakeStatTemplate(GameObject parent, string name, Color color)
    {
        var tmp = TMP(parent, name, "효과", UIScale.FontMd, FontStyles.Normal);
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        // ⚠ Ellipsis 로 두면 두 줄짜리 설명이 통째로 "..." 이 된다
        tmp.overflowMode     = TextOverflowModes.Overflow;

        // 줄 높이는 손으로 적지 않는다 — 폰트 상수가 바뀌면 같이 커져야 한다 (UI 규칙 5)
        var le = tmp.gameObject.AddComponent<LayoutElement>();
        le.minHeight = UIScale.RowMd;

        tmp.gameObject.SetActive(false);
        return tmp;
    }

    static GameObject Column(GameObject panel, string name, float x, float width)
    {
        var col = Go(name, panel);
        var rt = col.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(x, BodyBtm);
        rt.offsetMax = new Vector2(x + width, -BodyTop);
        return col;
    }

    static void Divider(GameObject parent, string name, float top, string label)
    {
        var div = Go(name, parent);
        EditorUIBuilder.AnchorTop(div.GetComponent<RectTransform>(), top, 36f);

        DividerLine(div, "LineL", 0f,   0.5f,   0f, -100f);
        DividerLine(div, "LineR", 0.5f, 1f,   100f,    0f);

        var lbl = TMP(div, "Label", label, UIScale.FontSm, FontStyles.Bold);
        lbl.color         = DividerLbl;
        lbl.alignment     = TextAlignmentOptions.Center;
        lbl.raycastTarget = false;
        EditorUIBuilder.Stretch(lbl.gameObject);
    }

    static void DividerLine(GameObject parent, string name,
                            float aMinX, float aMaxX, float offMinX, float offMaxX)
    {
        var go = Go(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = DividerC;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 0.5f);
        rt.anchorMax = new Vector2(aMaxX, 0.5f);
        rt.offsetMin = new Vector2(offMinX, -1f);
        rt.offsetMax = new Vector2(offMaxX,  1f);
    }

    static void StretchFrom(RectTransform rt, float top, float bottom)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0f, bottom);
        rt.offsetMax = new Vector2(0f, -top);
    }

    static void StretchV(RectTransform rt, float width, float vMargin)
    {
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(width, -vMargin * 2f);
    }

    static void Center(GameObject mark)
    {
        var rt = mark.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    static GameObject Go(string name, GameObject parent) => EditorUIBuilder.Go(name, parent);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text,
                               float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "AbilityListPopupCreator");
}
#endif
