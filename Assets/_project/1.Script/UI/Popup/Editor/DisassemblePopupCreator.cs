#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DisassemblePopupCreator.cs
//  Tools > Project K > 프리팹 생성 > 팝업 > Disassemble
//
//  저장: Assets/_project/2.Prefabs/UI/DisassemblePopup.prefab
//
//  ■ 왜 다시 짰나 (이전 레이아웃의 문제)
//    · 960×860 짜리 작은 창이라 장비 그리드가 5열 × 몇 줄뿐이었다.
//      장비가 30개만 넘어가도 스크롤을 한참 굴려야 해서 "뭘 분해할지" 를
//      한눈에 못 골랐다. 분해는 **많은 것 중에 고르는** 화면이다.
//    · 선택 정보가 좌측 300px 에 눌려 있어 스탯이 두 줄로 접혔다.
//    · 등급 필터가 그냥 네모라 켜짐/꺼짐이 구분되지 않았다.
//
//  ■ 새 레이아웃 (HeroDetailPopup 과 같은 전체화면 톤)
//    전체 1840 × (캔버스높이-32), 2단
//    Header  H=136   ◆ 분 해 | 장비 분해     [강화석 보유]        [X]
//    Body    H=814
//      Left  1140   등급 필터 5칸 + 장비 그리드 (8열, 스크롤)
//      Right  660   선택 장비 크게 + 스탯 + 분해 보상 + 실행 버튼
//
//  ⚠ 셀·템플릿의 자식 이름을 바꾸지 말 것
//    DisassemblePopup 이 이름으로 찾는다:
//      셀 = "GradeBorder" / "IconImage" / "SelectionOutline"
//    바꾸면 조용히 null 이 되어 등급 테두리·선택 표시가 사라진다.
// ============================================================

public static class DisassemblePopupCreator
{
    const string PrefabPath    = "Assets/_project/2.Prefabs/UI/DisassemblePopup.prefab";
    const string StoneIconPath = "Assets/_project/3.Textures/Icons/Items/item_equip_upgrade_stone.png";

    // ── 치수 ─────────────────────────────────────────────────
    const float PW       = 1840f;
    const float PVMargin =   16f;
    const float HeaderH  =  136f;
    const float BodyTop  =  156f;
    const float BodyBtm  =   26f;
    const float SidePad  =   40f;

    const float ColGap   =   20f;
    const float RightW   =  660f;
    const float LeftW    = PW - SidePad * 2f - RightW - ColGap;   // 1080

    const float FilterH  =   88f;
    const float CellSize =  128f;
    const float CellGap  =   10f;
    const int   GridCols =    7;

    // ── 색상 (HeroDetailPopup 과 같은 팔레트) ─────────────────
    static readonly Color BgOverlay   = new Color(0f,     0f,     0f,     0.80f);
    static readonly Color PanelBg     = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder = new Color(0.62f,  0.30f,  0.26f,  1f);   // 분해 = 적동색
    static readonly Color HeaderBg    = new Color(0.16f,  0.07f,  0.06f,  1f);
    static readonly Color AccentRed   = new Color(0.86f,  0.36f,  0.28f,  1f);
    static readonly Color TagColor    = new Color(0.96f,  0.62f,  0.54f,  1f);
    static readonly Color TitleColor  = new Color(1.00f,  0.94f,  0.86f,  1f);
    static readonly Color TitleShadow = new Color(0.05f,  0.02f,  0.02f,  0.85f);

    static readonly Color SectionBg   = new Color(0.10f,  0.105f, 0.175f, 1f);
    static readonly Color GridBg      = new Color(0.055f, 0.06f,  0.105f, 1f);
    static readonly Color CellBg      = new Color(0.13f,  0.135f, 0.21f,  1f);
    static readonly Color DividerC    = new Color(0.30f,  0.24f,  0.28f,  0.85f);
    static readonly Color DividerLbl  = new Color(0.82f,  0.70f,  0.66f,  1f);
    static readonly Color LabelColor  = new Color(0.62f,  0.60f,  0.70f,  1f);

    static readonly Color DisBtnC     = new Color(0.60f,  0.20f,  0.14f,  1f);
    // 일괄 분해 — 회보라(0.30,0.22,0.32)는 패널 배경과 거의 같아 버튼으로 안 보였다.
    // 위험한 동작이므로 눈에 먼저 들어와야 한다 → 호박색.
    static readonly Color BulkBtnC    = new Color(0.72f,  0.45f,  0.12f,  1f);
    static readonly Color CloseBtnC   = new Color(0.50f,  0.14f,  0.14f,  1f);
    static readonly Color FilterOnC   = new Color(0.34f,  0.26f,  0.34f,  1f);
    // 선택 표시 — 등급 테두리(최대 5색)와 겹쳐도 이기도록 밝고 두껍게
    static readonly Color SelectOuter = new Color(1.00f,  0.86f,  0.25f,  1f);
    static readonly Color SelectInner = new Color(1.00f,  1.00f,  0.92f,  1f);
    static readonly Color SelectTint  = new Color(1.00f,  0.90f,  0.45f,  0.22f);
    static readonly Color StoneColor  = new Color(0.60f,  0.85f,  1.00f,  1f);
    static readonly Color MutedText   = new Color(0.52f,  0.52f,  0.60f,  1f);

    static readonly Color[] GradeColors =
    {
        new Color(0.55f, 0.55f, 0.55f),  // Normal
        new Color(0.25f, 0.80f, 0.35f),  // Uncommon
        new Color(0.20f, 0.50f, 1.00f),  // Rare
        new Color(0.70f, 0.30f, 1.00f),  // Unique
        new Color(1.00f, 0.60f, 0.10f),  // Epic
    };
    static readonly string[] GradeLabels = { "일반", "비범", "희귀", "고유", "영웅" };

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "Disassemble", priority = ProjectKMenu.PrefabPrio + 37)]
    public static void Create()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Build();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DisassemblePopupCreator] 저장: {PrefabPath} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    static GameObject Build()
    {
        var root = EditorUIBuilder.Panel(null, "DisassemblePopup", BgOverlay);
        EditorUIBuilder.Stretch(root);

        var popup = root.AddComponent<DisassemblePopup>();
        var so    = new SerializedObject(popup);
        EditorUIBuilder.SetEnum(so, "_popupType", (int)PopupType.Disassemble, "DisassemblePopupCreator");

        // 테두리 — 패널의 앞 형제 (자식으로 두면 팝업을 덮는다, UI 규칙 3)
        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        StretchV(border.GetComponent<RectTransform>(), PW + 6f, PVMargin - 3f);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        StretchV(panel.GetComponent<RectTransform>(), PW, PVMargin);

        BuildHeader(panel, so);
        BuildLeft(panel, so);
        BuildRight(panel, so);

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

        // ◆ 태그 — ★ 같은 글리프는 폰트에 없어 도형으로 그린다 (UI 규칙 2)
        var tagRoot = Go("DisTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(300f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "분 해", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        MakeTitle(header, "TitleShadow", TitleShadow, 3f);
        MakeTitle(header, "TitleText",   TitleColor,  0f);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentRed;
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
        EditorUIBuilder.InfoBtn(header, TutorialId.HelpDisassemble, 76f, -24f);

        // 보유 강화석 — 분해로 얻는 재화라 헤더에 상시 노출한다
        BuildStoneWidget(header);
    }

    static void MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "장비 분해", UIScale.FontLg, FontStyles.Bold);
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

    static void BuildStoneWidget(GameObject header)
    {
        const float W = 220f, IconSz = 44f;

        var group = Go("StoneGroup", header);
        var rt = group.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);
        // 닫기 + 도움말 묶음 왼쪽
        rt.anchoredPosition = new Vector2(-(EditorUIBuilder.HeaderRightBlock(76f, 24f) + 20f), 0f);
        rt.sizeDelta        = new Vector2(W, UIScale.RowMd);

        var icon = EditorUIBuilder.Img(group, "Icon", StoneColor);
        icon.sprite = LoadSprite(StoneIconPath);
        if (icon.sprite != null) icon.color = Color.white;
        var iRt = icon.rectTransform;
        iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot     = new Vector2(0f, 0.5f);
        iRt.anchoredPosition = Vector2.zero;
        iRt.sizeDelta        = new Vector2(IconSz, IconSz);

        var amt = TMP(group, "Value", "0", UIScale.FontMd, FontStyles.Bold);
        amt.color            = StoneColor;
        amt.alignment        = TextAlignmentOptions.MidlineLeft;
        amt.raycastTarget    = false;
        amt.textWrappingMode = TextWrappingModes.NoWrap;
        var aRt = amt.rectTransform;
        aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
        aRt.offsetMin = new Vector2(IconSz + 10f, 0f);
        aRt.offsetMax = Vector2.zero;

        var widget = group.AddComponent<CurrencyWidget>();
        var wSo    = new SerializedObject(widget);
        wSo.FindProperty("_item").intValue                   = (int)eItem.EquipUpgradeStone;
        wSo.FindProperty("_amountText").objectReferenceValue = amt;
        wSo.FindProperty("_icon").objectReferenceValue       = icon;
        wSo.ApplyModifiedProperties();
    }

    // ══════════════════════════════════════════════════════════
    //  좌측 — 등급 필터 + 장비 그리드
    // ══════════════════════════════════════════════════════════

    static void BuildLeft(GameObject panel, SerializedObject so)
    {
        var col = Column(panel, "GridColumn", SidePad, LeftW);

        // ── 등급 필터 ────────────────────────────────────────
        Divider(col, "FilterDivider", 0f, "등  급");

        var filterRow = Go("FilterRow", col);
        EditorUIBuilder.AnchorTop(filterRow.GetComponent<RectTransform>(), 44f, FilterH, padH: 4f);

        var toggles = new Toggle[5];
        for (int i = 0; i < 5; i++)
            toggles[i] = BuildGradeToggle(filterRow, i);

        SetObjArray(so, "_gradeToggles", toggles);

        // 일괄 분해 — 필터로 고른 등급을 통째로 넘긴다
        var bulk = EditorUIBuilder.RaisedBtn(col, "BulkBtn", BulkBtnC, out var bulkBody);
        var bRt = bulk.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 1f); bRt.anchorMax = new Vector2(1f, 1f);
        bRt.pivot     = new Vector2(1f, 1f);
        bRt.anchoredPosition = new Vector2(-4f, -(44f + FilterH + 12f));
        bRt.sizeDelta        = new Vector2(260f, UIScale.BtnFor(UIScale.FontMd));

        var bulkLbl = TMP(bulkBody, "Label", "선택 등급 일괄 분해", UIScale.FontSm, FontStyles.Bold);
        bulkLbl.color         = new Color(1f, 0.97f, 0.88f);
        bulkLbl.alignment     = TextAlignmentOptions.Center;
        bulkLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(bulkLbl.gameObject);
        SetObj(so, "_bulkDisassembleBtn", bulk);

        // ── 그리드 ───────────────────────────────────────────
        float gridTop = 44f + FilterH + 12f + UIScale.BtnFor(UIScale.FontMd) + 14f;

        var gridBg = Go("GridBg", col);
        gridBg.AddComponent<Image>().color = GridBg;
        StretchFrom(gridBg.GetComponent<RectTransform>(), gridTop, 0f);

        var scroll = Go("Scroll", gridBg);
        EditorUIBuilder.Stretch(scroll);
        var scrollRt = scroll.GetComponent<RectTransform>();
        scrollRt.offsetMin = new Vector2(14f, 14f);
        scrollRt.offsetMax = new Vector2(-14f, -14f);

        var sr   = scroll.AddComponent<ScrollRect>();
        var mask = scroll.AddComponent<Image>();
        mask.color = new Color(0f, 0f, 0f, 0.01f);   // Mask 는 Graphic 이 있어야 동작한다
        scroll.AddComponent<Mask>().showMaskGraphic = false;

        var content = Go("Content", scroll);
        var cRt2 = content.GetComponent<RectTransform>();
        cRt2.anchorMin = new Vector2(0f, 1f);
        cRt2.anchorMax = new Vector2(1f, 1f);
        cRt2.pivot     = new Vector2(0.5f, 1f);
        cRt2.anchoredPosition = Vector2.zero;
        cRt2.sizeDelta        = new Vector2(0f, 0f);

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(CellSize, CellSize);
        grid.spacing         = new Vector2(CellGap, CellGap);
        grid.padding         = new RectOffset(4, 4, 4, 4);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = GridCols;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content     = cRt2;
        sr.horizontal  = false;
        sr.vertical    = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;

        SetObj(so, "_gridContent", content.transform);
        SetObj(so, "_iconCellTemplate", BuildCellTemplate(content));
    }

    /// <summary>
    /// 장비 셀 템플릿 (비활성). 자식 이름은 DisassemblePopup 이 찾으므로 고정이다.
    ///   GradeBorder / IconImage / SelectionOutline
    /// </summary>
    static GameObject BuildCellTemplate(GameObject parent)
    {
        var cell = Go("CellTemplate", parent);
        cell.AddComponent<Image>().color = CellBg;
        cell.AddComponent<Button>();

        // 등급 테두리 — 셀 뒤가 아니라 살짝 안쪽에 깔아 색이 또렷하게 보이게 한다
        var borderGo = Go("GradeBorder", cell);
        borderGo.AddComponent<Image>().color = GradeColors[0];
        var brRt = borderGo.GetComponent<RectTransform>();
        brRt.anchorMin = Vector2.zero; brRt.anchorMax = Vector2.one;
        brRt.offsetMin = new Vector2(3f, 3f); brRt.offsetMax = new Vector2(-3f, -3f);

        var inner = Go("Inner", cell);
        inner.AddComponent<Image>().color = CellBg;
        var inRt = inner.GetComponent<RectTransform>();
        inRt.anchorMin = Vector2.zero; inRt.anchorMax = Vector2.one;
        inRt.offsetMin = new Vector2(7f, 7f); inRt.offsetMax = new Vector2(-7f, -7f);

        var icon = EditorUIBuilder.Img(cell, "IconImage", Color.white);
        var icRt = icon.rectTransform;
        icRt.anchorMin = Vector2.zero; icRt.anchorMax = Vector2.one;
        icRt.offsetMin = new Vector2(16f, 16f); icRt.offsetMax = new Vector2(-16f, -16f);
        icon.preserveAspect = true;

        // ── 선택 표시 ────────────────────────────────────────
        //  ⚠ 얇은 노란 테두리(±3)는 등급 테두리에 묻혀 안 보였다.
        //    두께를 키우고(±8) 흰 안쪽 선을 덧대 **두 겹**으로 만든다.
        //    셀 전체를 덮는 밝은 틴트도 같이 깔아 "이게 선택됨" 이 멀리서도 읽히게 한다.
        var sel = Go("SelectionOutline", cell);
        sel.AddComponent<Image>().color = SelectOuter;
        var slRt = sel.GetComponent<RectTransform>();
        slRt.anchorMin = Vector2.zero; slRt.anchorMax = Vector2.one;
        slRt.offsetMin = new Vector2(-8f, -8f); slRt.offsetMax = new Vector2(8f, 8f);

        var selInner = EditorUIBuilder.Img(sel, "InnerLine", SelectInner);
        var siRt2 = selInner.rectTransform;
        siRt2.anchorMin = Vector2.zero; siRt2.anchorMax = Vector2.one;
        siRt2.offsetMin = new Vector2(5f, 5f); siRt2.offsetMax = new Vector2(-5f, -5f);

        var selTint = EditorUIBuilder.Img(sel, "Tint", SelectTint);
        var stRt2 = selTint.rectTransform;
        stRt2.anchorMin = Vector2.zero; stRt2.anchorMax = Vector2.one;
        stRt2.offsetMin = new Vector2(8f, 8f); stRt2.offsetMax = new Vector2(-8f, -8f);

        sel.transform.SetAsFirstSibling();   // 테두리는 앞 형제로 (UI 규칙 3)
        sel.SetActive(false);

        cell.SetActive(false);
        return cell;
    }

    /// <summary>등급 필터 토글 — 켜짐이 한눈에 보이도록 등급색 바 + 체크를 같이 쓴다.</summary>
    static Toggle BuildGradeToggle(GameObject row, int index)
    {
        var go = Go($"Grade{index}", row);
        var bg = go.AddComponent<Image>();
        bg.color = SectionBg;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(index       / 5f, 0f);
        rt.anchorMax = new Vector2((index + 1) / 5f, 1f);
        rt.offsetMin = new Vector2(5f, 0f);
        rt.offsetMax = new Vector2(-5f, 0f);

        // ── 켜짐 표시 (Toggle.graphic) ───────────────────────
        //  ⚠ 어두운 회보라 한 장으로는 켜짐/꺼짐이 구분되지 않았다.
        //    등급색 프레임 + 그 등급색 옅은 채움으로 바꾼다 — 어느 등급이 켜졌는지도 같이 읽힌다.
        var on = Go("OnMark", go);
        var onImg = on.AddComponent<Image>();
        onImg.color = GradeColors[index];
        var onRt = on.GetComponent<RectTransform>();
        onRt.anchorMin = Vector2.zero; onRt.anchorMax = Vector2.one;
        onRt.offsetMin = Vector2.zero; onRt.offsetMax = Vector2.zero;

        var onFill = EditorUIBuilder.Img(on, "Fill",
            new Color(GradeColors[index].r, GradeColors[index].g, GradeColors[index].b, 0.28f));
        var ofRt = onFill.rectTransform;
        ofRt.anchorMin = Vector2.zero; ofRt.anchorMax = Vector2.one;
        ofRt.offsetMin = new Vector2(4f, 4f); ofRt.offsetMax = new Vector2(-4f, -4f);

        // 등급색 바 — 좌측 세로. 색만으로 어느 등급인지 읽힌다
        var bar = EditorUIBuilder.Img(go, "GradeBar", GradeColors[index]);
        var barRt = bar.rectTransform;
        barRt.anchorMin = new Vector2(0f, 0f); barRt.anchorMax = new Vector2(0f, 1f);
        barRt.pivot     = new Vector2(0f, 0.5f);
        barRt.anchoredPosition = new Vector2(8f, 0f);
        barRt.sizeDelta        = new Vector2(6f, -20f);

        var lbl = TMP(go, "Label", GradeLabels[index], UIScale.FontSm, FontStyles.Bold);
        // 등급색 채움 위에 같은 등급색 글자를 얹으면 묻힌다 — 거의 흰색으로 뽑는다
        lbl.color            = Color.Lerp(GradeColors[index], Color.white, 0.75f);
        lbl.alignment        = TextAlignmentOptions.Center;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        var lRt = lbl.rectTransform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(20f, 0f); lRt.offsetMax = Vector2.zero;

        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic       = on.GetComponent<Image>();
        toggle.isOn          = true;
        return toggle;
    }

    // ══════════════════════════════════════════════════════════
    //  우측 — 선택 장비 상세 + 분해
    // ══════════════════════════════════════════════════════════

    static void BuildRight(GameObject panel, SerializedObject so)
    {
        var col = Column(panel, "DetailColumn", SidePad + LeftW + ColGap, RightW);

        Divider(col, "DetailDivider", 0f, "선택한 장비");

        // ── 큰 아이콘 ────────────────────────────────────────
        const float IconBox = 260f;

        var iconWrap = Go("IconWrap", col);
        iconWrap.AddComponent<Image>().color = SectionBg;
        var iwRt = iconWrap.GetComponent<RectTransform>();
        iwRt.anchorMin = iwRt.anchorMax = new Vector2(0.5f, 1f);
        iwRt.pivot     = new Vector2(0.5f, 1f);
        iwRt.anchoredPosition = new Vector2(0f, -52f);
        iwRt.sizeDelta        = new Vector2(IconBox, IconBox);

        var gradeBorder = EditorUIBuilder.Img(iconWrap, "GradeBorder", GradeColors[0]);
        var gbRt = gradeBorder.rectTransform;
        gbRt.anchorMin = Vector2.zero; gbRt.anchorMax = Vector2.one;
        gbRt.offsetMin = new Vector2(-4f, -4f); gbRt.offsetMax = new Vector2(4f, 4f);
        gradeBorder.transform.SetAsFirstSibling();   // 테두리는 앞 형제 (UI 규칙 3)
        SetObj(so, "_selectedGradeBorder", gradeBorder);

        var selIcon = EditorUIBuilder.Img(iconWrap, "SelectedIcon", Color.white);
        var siRt = selIcon.rectTransform;
        siRt.anchorMin = Vector2.zero; siRt.anchorMax = Vector2.one;
        siRt.offsetMin = new Vector2(26f, 26f); siRt.offsetMax = new Vector2(-26f, -26f);
        selIcon.preserveAspect = true;
        SetObj(so, "_selectedIcon", selIcon);

        // ── 이름 · 등급 ──────────────────────────────────────
        float y = 52f + IconBox + 18f;

        var nameTmp = TMP(col, "NameText", "장비를 선택하세요", UIScale.FontLg, FontStyles.Bold);
        nameTmp.color            = Color.white;
        nameTmp.alignment        = TextAlignmentOptions.Center;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Ellipsis;
        EditorUIBuilder.AnchorTop(nameTmp.rectTransform, y, UIScale.Line(UIScale.FontLg), padH: 8f);
        SetObj(so, "_selectedNameText", nameTmp);

        y += UIScale.Line(UIScale.FontLg) + 4f;

        var gradeTmp = TMP(col, "GradeText", "", UIScale.FontMd, FontStyles.Bold);
        gradeTmp.color         = MutedText;
        gradeTmp.alignment     = TextAlignmentOptions.Center;
        gradeTmp.raycastTarget = false;
        EditorUIBuilder.AnchorTop(gradeTmp.rectTransform, y, UIScale.RowMd, padH: 8f);
        SetObj(so, "_selectedGradeText", gradeTmp);

        y += UIScale.RowMd + 14f;

        // ── 스탯 ─────────────────────────────────────────────
        // ⚠ 높이를 고정하지 않는다
        //   스탯 2개짜리 장비는 190px 로 충분하지만, 특수 옵션이 붙으면 넘친다.
        //   위(y)와 아래(보상행+버튼)에 동시에 물려 남는 높이를 전부 쓰게 한다.
        float rewardBlockH = UIScale.BtnFor(UIScale.FontLg)          // 분해 버튼
                           + 12f + UIScale.BtnFor(UIScale.FontMd)    // 보상 행
                           + 16f;                                    // 여백

        var statBg = Go("StatBg", col);
        statBg.AddComponent<Image>().color = SectionBg;
        var sbRt = statBg.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0f); sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot     = new Vector2(0.5f, 1f);
        sbRt.offsetMin = new Vector2(4f, rewardBlockH);
        sbRt.offsetMax = new Vector2(-4f, -y);

        var statTmp = TMP(statBg, "StatsText", "", UIScale.FontMd, FontStyles.Normal);
        statTmp.color            = new Color(0.80f, 0.84f, 0.94f);
        statTmp.alignment        = TextAlignmentOptions.TopLeft;
        statTmp.raycastTarget    = false;
        statTmp.textWrappingMode = TextWrappingModes.Normal;
        statTmp.lineSpacing      = 14f;
        // ⚠ Ellipsis 로 두면 긴 스탯 문자열이 통째로 "..." 이 된다 (이전 버그)
        statTmp.overflowMode     = TextOverflowModes.Overflow;
        // 특수 옵션까지 붙어 줄이 늘어나면 폰트를 줄여 칸 안에 담는다
        statTmp.enableAutoSizing = true;
        statTmp.fontSizeMin      = 28f;
        statTmp.fontSizeMax      = UIScale.FontMd;
        var stRt = statTmp.rectTransform;
        stRt.anchorMin = Vector2.zero; stRt.anchorMax = Vector2.one;
        stRt.offsetMin = new Vector2(20f, 16f); stRt.offsetMax = new Vector2(-20f, -16f);
        SetObj(so, "_selectedStatsText", statTmp);

        // ── 분해 보상 + 실행 버튼 (하단 고정) ────────────────
        float btnH = UIScale.BtnFor(UIScale.FontLg);

        var disBtn = EditorUIBuilder.RaisedBtn(col, "DisassembleBtn", DisBtnC, out var disBody);
        var dbRt = disBtn.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f); dbRt.anchorMax = new Vector2(1f, 0f);
        dbRt.pivot     = new Vector2(0.5f, 0f);
        dbRt.offsetMin = new Vector2(4f, 0f);
        dbRt.offsetMax = new Vector2(-4f, btnH);

        var disLbl = TMP(disBody, "Label", "분  해", UIScale.FontLg, FontStyles.Bold);
        disLbl.alignment     = TextAlignmentOptions.Center;
        disLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(disLbl.gameObject);
        SetObj(so, "_disassembleBtn", disBtn);

        // 보상 표시 — 버튼 바로 위. "얼마 받는지" 를 누르기 전에 본다
        var rewardRow = Go("RewardRow", col);
        rewardRow.AddComponent<Image>().color = SectionBg;
        var rrRt = rewardRow.GetComponent<RectTransform>();
        rrRt.anchorMin = new Vector2(0f, 0f); rrRt.anchorMax = new Vector2(1f, 0f);
        rrRt.pivot     = new Vector2(0.5f, 0f);
        rrRt.offsetMin = new Vector2(4f, btnH + 12f);
        rrRt.offsetMax = new Vector2(-4f, btnH + 12f + UIScale.BtnFor(UIScale.FontMd));

        var rewardLbl = TMP(rewardRow, "Label", "분해 보상", UIScale.FontSm, FontStyles.Normal);
        rewardLbl.color         = LabelColor;
        rewardLbl.alignment     = TextAlignmentOptions.MidlineLeft;
        rewardLbl.raycastTarget = false;
        var rlRt = rewardLbl.rectTransform;
        rlRt.anchorMin = Vector2.zero; rlRt.anchorMax = new Vector2(0.5f, 1f);
        rlRt.offsetMin = new Vector2(20f, 0f); rlRt.offsetMax = Vector2.zero;

        var rewardIcon = EditorUIBuilder.Img(rewardRow, "RewardIcon", StoneColor);
        rewardIcon.sprite = LoadSprite(StoneIconPath);
        if (rewardIcon.sprite != null) rewardIcon.color = Color.white;
        var riRt = rewardIcon.rectTransform;
        riRt.anchorMin = riRt.anchorMax = new Vector2(1f, 0.5f);
        riRt.pivot     = new Vector2(1f, 0.5f);
        riRt.anchoredPosition = new Vector2(-110f, 0f);
        riRt.sizeDelta        = new Vector2(40f, 40f);
        SetObj(so, "_rewardIcon", rewardIcon);

        var rewardTmp = TMP(rewardRow, "RewardText", "0", UIScale.FontMd, FontStyles.Bold);
        rewardTmp.color            = StoneColor;
        rewardTmp.alignment        = TextAlignmentOptions.MidlineRight;
        rewardTmp.raycastTarget    = false;
        rewardTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var rtRt = rewardTmp.rectTransform;
        rtRt.anchorMin = new Vector2(0.5f, 0f); rtRt.anchorMax = Vector2.one;
        rtRt.offsetMin = Vector2.zero; rtRt.offsetMax = new Vector2(-20f, 0f);
        SetObj(so, "_rewardText", rewardTmp);
    }

    // ══════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════

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

    /// <summary>"— 라벨 —" 구분선. 섹션 머리로 쓴다.</summary>
    static void Divider(GameObject parent, string name, float top, string label)
    {
        var div = Go(name, parent);
        EditorUIBuilder.AnchorTop(div.GetComponent<RectTransform>(), top, 36f);

        DividerLine(div, "LineL", 0f,   0.5f,   0f, -90f);
        DividerLine(div, "LineR", 0.5f, 1f,    90f,   0f);

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

    // 위 top 만큼 띄우고 아래 bottom 까지 늘리는 세로 스트레치
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

    static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static GameObject Go(string name, GameObject parent) => EditorUIBuilder.Go(name, parent);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text,
                               float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "DisassemblePopupCreator");

    static void SetObjArray(SerializedObject so, string field, Object[] items)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"[DisassemblePopupCreator] 필드 없음: {field}");
            return;
        }
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
#endif
