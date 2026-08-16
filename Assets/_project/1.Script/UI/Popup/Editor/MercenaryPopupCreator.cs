using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercenaryPopupCreator.cs  [Editor Only]
//  Tools > Project K > 프리팹 생성 > 팝업 > Mercenary
//
//  생성물: MercenaryShopPopup.prefab
//
//  ⚠ 캔버스 기준
//    PopupManager 는 Splash 에서 살아남는 싱글턴이라 팝업 캔버스가 씬과 무관하다.
//    HeroDetailPopup·EventPopup 과 같은 가로 기준(1920×1080)이며 세로 여유가 1080 뿐이다.
//    → 패널은 세로 스트레치(위아래 16 여백)로 캔버스에 맞춘다.
//
//  ── 레이아웃 (PW=1840, H=캔버스높이-32 / 1080 캔버스 기준 1048) ──
//  Header      Y=  0  H=136  ◆ 용 병 태그 + 타이틀(그림자) + 닫기
//  AccentLine  Y=136  H=  3
//  Body        Y=156  → 하단 26
//    Left  832  ◀ [장수 카드 620×862] ▶     ← MainPanelCreator.BuildHeroCard
//    Right 888  "선 택" 구분선 → 안내 문구 → 현재 부대 5칸 → [고용] / [돌려보내기]
//
//  ⚠ 카드 높이(862)가 Body 높이를 결정한다
//    Body = H - 156 - 26. 1080 캔버스에서 866 → 862 가 겨우 들어간다.
//    헤더를 키우거나 여백을 늘리면 카드 아래가 잘린다 — 반드시 다시 계산할 것.
//
//  ⚠ 카드 레이아웃을 여기서 다시 짜지 않는다
//    MainPanelCreator.BuildHeroCard() 가 장수 카드의 정본이다.
//    복사본을 만들면 스탯 표기·스킬 아이콘 규칙이 화면마다 갈라진다.
//
//  ⚠ 선택 버튼은 EventPopup 과 같은 입체 버튼
//    Shadow(하단 6px 노출) → Body → TopEdge/BottomEdge → 라벨 + 힌트.
//    평평한 사각형은 버튼인지 라벨인지 구분이 안 된다 (UI 규칙 1).
// ============================================================

public static class MercenaryPopupCreator
{
    const string SaveDir = "Assets/_project/2.Prefabs/UI";

    // ── 치수 ─────────────────────────────────────────────────
    const float PW       = 1840f;
    const float PVMargin =   16f;   // 캔버스 위·아래 여백
    const float HeaderH  =  136f;
    const float BodyTop  =  156f;
    const float BodyBtm  =   26f;
    const float SidePad  =   40f;

    const float LeftW    =  832f;   // 카드 620 + 화살표 2×(88+18)
    const float ColGap   =   40f;
    const float RightW   = PW - SidePad * 2f - LeftW - ColGap;   // 888

    const float DivH     =   36f;
    const float HintH    =  120f;
    const float BtnH     =  112f;   // FontMd 라벨 + FontSm 힌트 2줄
    const float BtnGap   =   16f;

    // 현재 부대 행 — 안내 문구 아래, 선택 버튼 위
    const float SquadTop  = DivH + 24f + HintH + 20f;   // 200
    const float SquadRowH =  124f;                      // 이름 한 줄 + Lv 한 줄
    const float SquadGap  =    6f;                      // 칸 사이 좌우 여백

    // ── 색상 (EventPopup 톤 — 용병은 청록 계열로 구분) ────────
    static readonly Color BgOverlay    = new Color(0f,     0f,     0f,     0.78f);
    static readonly Color PanelBg      = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder  = new Color(0.20f,  0.52f,  0.56f,  1f);
    static readonly Color HeaderBg     = new Color(0.06f,  0.14f,  0.16f,  1f);
    static readonly Color AccentTeal   = new Color(0.26f,  0.78f,  0.80f,  1f);
    static readonly Color TagColor     = new Color(0.58f,  0.92f,  0.94f,  1f);
    static readonly Color TitleColor   = new Color(1.00f,  0.94f,  0.78f,  1f);
    static readonly Color TitleShadow  = new Color(0.02f,  0.03f,  0.06f,  0.85f);

    static readonly Color DividerLineC = new Color(0.26f,  0.34f,  0.40f,  0.85f);
    static readonly Color DividerLbl   = new Color(0.64f,  0.76f,  0.82f,  1f);
    static readonly Color HintColor    = new Color(0.72f,  0.78f,  0.86f,  1f);

    static readonly Color HireBtnC     = new Color(0.13f,  0.52f,  0.38f,  1f);
    static readonly Color PassBtnC     = new Color(0.26f,  0.28f,  0.36f,  1f);
    static readonly Color SquadSlotC   = new Color(0.17f,  0.20f,  0.28f,  1f);
    static readonly Color SquadLblC    = new Color(0.64f,  0.76f,  0.82f,  1f);
    static readonly Color CloseBtnC    = new Color(0.50f,  0.14f,  0.14f,  1f);
    static readonly Color LabelWhite   = new Color(0.98f,  0.99f,  1.00f,  1f);
    static readonly Color HintGreen    = new Color(0.50f,  0.94f,  0.66f,  1f);
    static readonly Color CostGold     = new Color(1.00f,  0.85f,  0.20f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "Mercenary", priority = ProjectKMenu.PrefabPrio + 41)]
    public static void Create()
    {
        System.IO.Directory.CreateDirectory(SaveDir);

        var root = Build();
        string path = $"{SaveDir}/MercenaryShopPopup.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MercenaryPopupCreator] 저장: {path} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    static GameObject Build()
    {
        // ── 루트 (전체화면 오버레이) ─────────────────────────
        var root = EditorUIBuilder.Panel(null, "MercenaryShopPopup", BgOverlay);
        EditorUIBuilder.Stretch(root);
        var popup = root.AddComponent<MercenaryShopPopup>();
        var so    = new SerializedObject(popup);
        SetEnum(so, "_popupType", (int)PopupType.MercenaryShop);

        // ── 테두리 (Panel 의 앞 형제 — 자식으로 두면 팝업을 덮는다) ──
        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        StretchV(border.GetComponent<RectTransform>(), PW + 6f, PVMargin - 3f);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        StretchV(panel.GetComponent<RectTransform>(), PW, PVMargin);

        BuildHeader(panel, so);
        BuildLeftColumn(panel, so);
        BuildRightColumn(panel, so);

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

        // ★ 는 폰트에 없다 (□ 로 렌더됨) → 마름모 도형으로 대체 (UI 규칙 2)
        var tagRoot = Go("MercTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(300f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "용 병", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        // 타이틀 — 그림자 사본을 먼저 깔아 어떤 배경에서도 읽히게 한다
        MakeTitle(header, "TitleShadow", TitleShadow, 3f);
        MakeTitle(header, "TitleText",   TitleColor,  0f);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentTeal;
        EditorUIBuilder.AnchorTop(accent.GetComponent<RectTransform>(), HeaderH, 3f);

        var closeBtn = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseBtnC, out var body);
        var cRt = closeBtn.GetComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(1f, 0.5f);
        cRt.pivot     = new Vector2(1f, 0.5f);
        cRt.anchoredPosition = new Vector2(-24f, 0f);
        cRt.sizeDelta        = new Vector2(76f, 76f);
        CenterMark(EditorUIBuilder.XMark(body, "Mark", UIScale.FontMd, Color.white));
        SetObj(so, "_closeBtn", closeBtn);

        // 보유 골드 — 닫기 버튼 왼쪽. 고용에 골드가 나가므로 잔액이 보여야 한다.
        // 로비 TopBar 와 같은 CurrencyWidget 이라 값이 자동으로 갱신된다.
        BuildGoldWidget(header);
    }

    // ── 보유 골드 위젯 ───────────────────────────────────────

    static void BuildGoldWidget(GameObject header)
    {
        const float W = 200f;
        const float IconSz = 44f;

        var group = Go("GoldGroup", header);
        var rt = group.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-(24f + 76f + 20f), 0f);   // 닫기 버튼 왼쪽
        rt.sizeDelta        = new Vector2(W, UIScale.RowMd);

        var icon = EditorUIBuilder.Img(group, "Icon", CostGold);
        var iRt = icon.rectTransform;
        iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot     = new Vector2(0f, 0.5f);
        iRt.anchoredPosition = new Vector2(0f, 0f);
        iRt.sizeDelta        = new Vector2(IconSz, IconSz);

        // 자릿수가 늘어나도 줄바꿈 대신 폰트를 줄인다 (UI 규칙 5 — 칸 높이 유지)
        var amt = TMP(group, "Value", "0", UIScale.FontMd, FontStyles.Bold);
        amt.color            = CostGold;
        amt.alignment        = TextAlignmentOptions.MidlineLeft;
        amt.raycastTarget    = false;
        amt.textWrappingMode = TextWrappingModes.NoWrap;
        amt.overflowMode     = TextOverflowModes.Overflow;
        amt.enableAutoSizing = true;
        amt.fontSizeMin      = UIScale.FontSm;
        amt.fontSizeMax      = UIScale.FontMd;
        var aRt = amt.rectTransform;
        aRt.anchorMin = new Vector2(0f, 0f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.offsetMin = new Vector2(IconSz + 10f, 0f);
        aRt.offsetMax = Vector2.zero;

        var widget = group.AddComponent<CurrencyWidget>();
        var wSo    = new SerializedObject(widget);
        wSo.FindProperty("_item").intValue                   = (int)eItem.Gold;
        wSo.FindProperty("_amountText").objectReferenceValue = amt;
        wSo.FindProperty("_icon").objectReferenceValue       = icon;
        wSo.ApplyModifiedProperties();
    }

    static void MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "용병 고용", UIScale.FontLg, FontStyles.Bold);
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
    //  좌측 — 장수 카드 + 페이지 화살표
    // ══════════════════════════════════════════════════════════

    static void BuildLeftColumn(GameObject panel, SerializedObject so)
    {
        var col = Go("CardColumn", panel);
        var rt = col.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(SidePad, BodyBtm);
        rt.offsetMax = new Vector2(SidePad + LeftW, -BodyTop);

        // 카드는 MainPanelCreator 가 정본 — 여기서 레이아웃을 다시 짜지 않는다.
        //  withRefresh : MainPanel 전용 (상점 후보는 다시 뽑을 수 없다)
        //  withTrait   : 특성은 게임 시작 시 직업별로 하나 받는 것이다.
        //                용병 고용은 특성을 주지 않으므로 칸 자체를 뺀다.
        var card = MainPanelCreator.BuildHeroCard(col, withDots: true, withRefresh: false,
                                                  withTrait: false,
                                                  out var cardUI, out var dots, out _);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 1f);
        cardRt.pivot     = new Vector2(0.5f, 1f);
        cardRt.anchoredPosition = Vector2.zero;

        // 화살표 — 카드 좌우 바깥, 초상화 수직 중심에 맞춘다 (MainPanel 과 동일)
        float arrY = -(MainPanelCreator.PortPad + MainPanelCreator.PortH * 0.5f);

        var arrL = MainPanelCreator.BuildPageArrow(card, "PrevBtn", 180f);
        {
            var a = arrL.GetComponent<RectTransform>();
            a.anchorMin = a.anchorMax = new Vector2(0f, 1f);
            a.pivot     = new Vector2(1f, 0.5f);
            a.anchoredPosition = new Vector2(-MainPanelCreator.ArrGap, arrY);
            a.sizeDelta = new Vector2(MainPanelCreator.ArrSize, MainPanelCreator.ArrSize);
        }

        var arrR = MainPanelCreator.BuildPageArrow(card, "NextBtn", 0f);
        {
            var a = arrR.GetComponent<RectTransform>();
            a.anchorMin = a.anchorMax = new Vector2(1f, 1f);
            a.pivot     = new Vector2(0f, 0.5f);
            a.anchoredPosition = new Vector2(MainPanelCreator.ArrGap, arrY);
            a.sizeDelta = new Vector2(MainPanelCreator.ArrSize, MainPanelCreator.ArrSize);
        }

        SetObj(so, "_card",    cardUI);
        SetObj(so, "_prevBtn", arrL.GetComponent<Button>());
        SetObj(so, "_nextBtn", arrR.GetComponent<Button>());

        var dp = so.FindProperty("_pageDots");
        if (dp != null)
        {
            dp.arraySize = dots.Length;
            for (int i = 0; i < dots.Length; i++)
                dp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
        }
    }

    // ══════════════════════════════════════════════════════════
    //  우측 — "선 택" 구분선 + 안내 + 고용/돌려보내기
    // ══════════════════════════════════════════════════════════

    static void BuildRightColumn(GameObject panel, SerializedObject so)
    {
        var col = Go("ChoiceColumn", panel);
        var rt = col.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(SidePad + LeftW + ColGap, BodyBtm);
        rt.offsetMax = new Vector2(SidePad + LeftW + ColGap + RightW, -BodyTop);

        // ── "선 택" 구분선 ───────────────────────────────────
        var divider = Go("ChoiceDivider", col);
        EditorUIBuilder.AnchorTop(divider.GetComponent<RectTransform>(), 0f, DivH);
        DividerLine(divider, "LineL", 0f,   0.5f,   0f, -66f);
        DividerLine(divider, "LineR", 0.5f, 1f,   66f,   0f);

        var divLbl = TMP(divider, "Label", "선  택", UIScale.FontSm, FontStyles.Bold);
        divLbl.color         = DividerLbl;
        divLbl.alignment     = TextAlignmentOptions.Center;
        divLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(divLbl.gameObject);

        // ── 안내 문구 ────────────────────────────────────────
        var hint = TMP(col, "HintText",
                       "한 명을 고용하거나, 전부 돌려보내 용병 조각을 받는다.",
                       UIScale.FontMd, FontStyles.Normal);
        hint.color            = HintColor;
        hint.alignment        = TextAlignmentOptions.TopLeft;
        hint.raycastTarget    = false;
        hint.textWrappingMode = TextWrappingModes.Normal;
        hint.lineSpacing      = 12f;   // 줄간격이 좁으면 한글 본문이 뭉친다
        EditorUIBuilder.AnchorTop(hint.rectTransform, DivH + 24f, HintH, padH: 8f);
        SetObj(so, "_hintText", hint);

        BuildSquadSection(col, so);

        // ── 선택 버튼 2개 (하단 정렬) ────────────────────────
        //  아래에서 위로 쌓는다 — 카드 높이가 캔버스에 따라 달라져도 붙어 있다.
        var passBtn = BuildChoiceButton(col, "PassButton", "돌 려 보 내 기", PassBtnC,
                                        bottomY: 0f,
                                        out var passHint, out var passIcon);
        SetObj(so, "_passBtn",       passBtn);
        SetObj(so, "_passBtnLabel",  passHint);
        SetObj(so, "_passShardIcon", passIcon);

        var hireBtn = BuildChoiceButton(col, "HireButton", "고     용", HireBtnC,
                                        bottomY: BtnH + BtnGap,
                                        out var hireCost, out var hireIcon);
        SetObj(so, "_hireBtn",      hireBtn);
        SetObj(so, "_hireCostText", hireCost);
        SetObj(so, "_hireCostIcon", hireIcon);
    }

    // ══════════════════════════════════════════════════════════
    //  현재 부대 (5칸) — 해고 진입점
    // ══════════════════════════════════════════════════════════
    //
    //  ⚠ 해고 UI 를 여기에 만들지 않는다
    //    칸을 누르면 HeroDetailPopup 이 정식 모드로 열리고, [해고] 버튼과
    //    "마지막 1명은 해고 불가" 규칙이 거기 이미 있다.
    //    두 곳에 두면 보호 규칙이 갈라진다 — 여기는 진입점일 뿐이다.
    //
    //  ⚠ 칸 배경을 등급색으로 칠하지 않는다
    //    RaisedBtn 의 눌림·비활성 색은 targetGraphic(Body) 색에 곱해지는 값을
    //    빌드 시점 face 색 기준으로 역산해 둔 것이다 (UI 규칙 1).
    //    런타임에 Body 색을 갈아끼우면 그 계산이 어긋난다 → 등급은 이름 색으로 표시한다.

    static void BuildSquadSection(GameObject col, SerializedObject so)
    {
        var lbl = TMP(col, "SquadLabel", "현재 부대 — 칸을 누르면 상세·해고", UIScale.FontSm, FontStyles.Bold);
        lbl.color            = SquadLblC;
        lbl.alignment        = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        EditorUIBuilder.AnchorTop(lbl.rectTransform, SquadTop, UIScale.RowSm, padH: 8f);

        var row = Go("SquadRow", col);
        EditorUIBuilder.AnchorTop(row.GetComponent<RectTransform>(),
                                  SquadTop + UIScale.RowSm + 8f, SquadRowH, padH: 8f);

        const int SlotCount = 5;
        var btns   = new Button[SlotCount];
        var names  = new TextMeshProUGUI[SlotCount];
        var levels = new TextMeshProUGUI[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            var btn = EditorUIBuilder.RaisedBtn(row, $"Slot{i}", SquadSlotC, out var body);

            // 폭을 숫자로 박지 않는다 — 앵커로 5등분해야 패널 폭이 바뀌어도 따라간다
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(i       / (float)SlotCount, 0f);
            rt.anchorMax = new Vector2((i + 1) / (float)SlotCount, 1f);
            rt.offsetMin = new Vector2(SquadGap,  0f);
            rt.offsetMax = new Vector2(-SquadGap, 0f);

            // ⚠ 라벨은 body 아래 (UI 규칙 1) — 루트에 붙이면 눌려도 안 내려간다
            var nameTmp = TMP(body, "NameText", "비어 있음", UIScale.FontSm, FontStyles.Bold);
            nameTmp.alignment        = TextAlignmentOptions.Center;
            nameTmp.raycastTarget    = false;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode     = TextOverflowModes.Overflow;
            // 이름 길이가 칸을 넘으면 줄바꿈 대신 폰트를 줄인다 (UI 규칙 5 — 칸 높이 유지)
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin      = 24f;
            nameTmp.fontSizeMax      = UIScale.FontSm;
            var nRt = nameTmp.rectTransform;
            nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot     = new Vector2(0.5f, 1f);
            nRt.anchoredPosition = new Vector2(0f, -14f);
            nRt.sizeDelta        = new Vector2(-12f, UIScale.RowSm);

            var lvTmp = TMP(body, "LevelText", "—", UIScale.FontSm, FontStyles.Normal);
            lvTmp.alignment     = TextAlignmentOptions.Center;
            lvTmp.raycastTarget = false;
            var vRt = lvTmp.rectTransform;
            vRt.anchorMin = new Vector2(0f, 0f); vRt.anchorMax = new Vector2(1f, 0f);
            vRt.pivot     = new Vector2(0.5f, 0f);
            vRt.anchoredPosition = new Vector2(0f, 14f);
            vRt.sizeDelta        = new Vector2(-12f, UIScale.RowSm);

            btns[i]   = btn;
            names[i]  = nameTmp;
            levels[i] = lvTmp;
        }

        SetArray(so, "_squadBtns",   btns);
        SetArray(so, "_squadNames",  names);
        SetArray(so, "_squadLevels", levels);
    }

    static void SetArray(SerializedObject so, string field, Object[] items)
    {
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"[MercenaryPopupCreator] 필드 없음: {field}");
            return;
        }
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    /// <summary>
    /// EventPopup 과 같은 입체 선택 버튼.
    /// 좌측에 라벨(FontMd), 우측에 [아이콘][수량] 힌트.
    /// </summary>
    static Button BuildChoiceButton(GameObject parent, string name, string label, Color face,
                                    float bottomY, out TextMeshProUGUI hintTmp, out Image hintIcon)
    {
        var btn = EditorUIBuilder.RaisedBtn(parent, name, face, out var body);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(8f,  bottomY);
        rt.offsetMax = new Vector2(-8f, bottomY + BtnH);

        // ⚠ 라벨·아이콘은 반드시 body 아래 — 루트에 넣으면 눌려도 안 내려간다 (UI 규칙 1)
        var lbl = TMP(body, "Label", label, UIScale.FontMd, FontStyles.Bold);
        lbl.color            = LabelWhite;
        lbl.alignment        = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        var lRt = lbl.rectTransform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(28f, 0f);
        lRt.offsetMax = new Vector2(-200f, 0f);

        // 우측 [아이콘][수량] — 고용은 골드 비용, 돌려보내기는 획득 조각 수
        var hintRoot = Go("Hint", body);
        var hRt = hintRoot.GetComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = new Vector2(1f, 0.5f);
        hRt.pivot     = new Vector2(1f, 0.5f);
        hRt.anchoredPosition = new Vector2(-24f, 0f);
        hRt.sizeDelta        = new Vector2(180f, UIScale.RowMd);
        EditorUIBuilder.CostHlg(hintRoot);

        hintIcon = EditorUIBuilder.Img(hintRoot, "Icon", new Color(0.55f, 0.58f, 0.66f));
        EditorUIBuilder.IconLE(hintIcon, 40f);

        hintTmp = TMP(hintRoot, "Value", "+0", UIScale.FontMd, FontStyles.Bold);
        hintTmp.color            = HintGreen;
        hintTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        hintTmp.raycastTarget    = false;
        hintTmp.textWrappingMode = TextWrappingModes.NoWrap;
        EditorUIBuilder.LE(hintTmp.gameObject, 110f, UIScale.RowMd);

        return btn;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    static GameObject Go(string name, GameObject parent) => EditorUIBuilder.Go(name, parent);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text,
                               float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    // 폭 고정 + 세로 스트레치 (위아래 vMargin 여백)
    static void StretchV(RectTransform rt, float width, float vMargin)
    {
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(width, -vMargin * 2f);
    }

    static void DividerLine(GameObject parent, string name,
                            float aMinX, float aMaxX, float offMinX, float offMaxX)
    {
        var go = Go(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = DividerLineC;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 0.5f);
        rt.anchorMax = new Vector2(aMaxX, 0.5f);
        rt.offsetMin = new Vector2(offMinX, -1f);
        rt.offsetMax = new Vector2(offMaxX,  1f);
    }

    static void CenterMark(GameObject mark)
    {
        var rt = mark.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "MercenaryPopupCreator");

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "MercenaryPopupCreator");
}
