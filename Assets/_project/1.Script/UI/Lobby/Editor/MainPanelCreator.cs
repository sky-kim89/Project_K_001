using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MainPanelCreator.cs
//  Tools > Project K > 로비 UI > Create MainPanel Prefab
//
//  ┌──────────────────────────────────────────────────────────┐
//  │  [타이틀]   │        ◀  [초상화 280 · 새로고침]  ▶        │
//  │  [▣ 유물 ]  │              ● ○ ○ ○                       │
//  │  [▣ 도감 ]  │               이름                          │
//  │  [▣ 기타 ]  │            기사  ·  영웅                     │
//  │             │   체력 1,988 │ 공격    99                   │
//  │             │   방어 80.0% │ 병사    7명                  │
//  │             │   특성 │ 스킬                               │
//  │             │   [특] │ [액티브][패][패][패]                │
//  │             │            [자세히 보기]                     │
//  │             │            [ 게임 시작 ]                     │
//  └──────────────────────────────────────────────────────────┘
//
//  ■ 이전 화면에서 고친 것
//    · 타이틀 "PIXEL"(112pt)이 340px 칸을 넘겨 "PIXE / L" 로 접혔다.
//      → NoWrap + AutoSize. 글자가 접히는 대신 줄어든다.
//    · 스탯 값 칸이 120px 뿐이라 "1,988" 이 "1,98 / 8" 로 접혔다.
//      → 카드를 440 → 620 으로 넓히고 값 칸을 160px 로. NoWrap 고정.
//    · 스탯 표기가 인게임과 달랐다 (라벨 우측정렬 + 값 좌측정렬).
//      → 상점 용병 카드와 같은 [라벨 좌][값 우] 한 줄 + StatColors 로 통일.
//    · 새로고침 버튼이 평평한 84×42 였고 라벨도 "새로고 / 침" 으로 접혔다.
//      → 초상화 우하단에 얹는 180×60 입체 버튼.
//    · 화살표가 56×56 검정 45% 라 배경에 묻혔다. → 88×88 청색 입체.
//    · 유물·도감·기타가 정사각형에 가까웠다. → 가로형 행 (아이콘 좌 + 라벨 우).
//
//  ■ 새로 넣은 것 — 스킬 아이콘
//    선택 단계에서 가장 궁금한 건 "이 장수가 뭘 쓰는가" 다.
//    액티브 1칸(금테) + 패시브 3칸(파란테, 등급만큼만 열림)을 특성 옆에 둔다.
//    아이콘을 누르면 이름·설명 툴팁 (특성 아이콘과 같은 동작).
// ============================================================

public static class MainPanelCreator
{
    const string SavePath     = "Assets/_project/2.Prefabs/UI/Lobby/MainPanel.prefab";
    const string TitleImgPath = "Assets/_project/3.Textures/UI/Lobby/title_pixel_general.png";

    // ── 레이아웃 상수 ─────────────────────────────────────────
    const float SideW     = 380f;
    const float SidePad   =  24f;
    const float SideBtnW  = SideW - SidePad * 2f;    // 332
    const float TitleH    = 300f;                    // 타이틀 PNG 원본 높이와 동일

    // 카드 (우측 오프셋 = 배경 더 많이 노출)
    // ⚠ CardW 620 은 스탯 값이 두 줄로 접히지 않는 최소 폭이다 (440 에서 접혔다).
    //   MercenaryPopupCreator 도 이 카드를 그대로 쓴다 — 줄이면 양쪽이 같이 깨진다.
    public const float CardW = 620f;
    const float CardOffX  =  70f;   // 카드 중심 오른쪽으로 이동
    const float CardTopY  =  46f;
    const int   Lp        =  16;    // 카드 내부 좌 여백 (GradeW 포함)
    const int   Rp        =  16;

    // 초상화
    public const float PortPad = 14f;
    public const float PortH   = 320f;   // 선택 화면의 주인공 — 260 → 320 으로 키웠다
    const float GradeW    =   6f;
    const float ChipW     = 118f;
    const float ChipH     =  40f;
    const float RefreshW  = 180f;
    const float RefreshH  =  60f;

    // 화살표 (CardContainer 기준 — 카드 밖 좌우, 초상화 수직 중심)
    public const float ArrSize = 88f;
    public const float ArrGap  = 18f;

    // 카드 내부 Y (top-anchor 기준 = card 상단에서 몇 px)
    //  직업·등급은 초상화 위 배지로 올려서 별도 행을 없앴다.
    const float DotsY     = PortPad + PortH + 12f;   // 346
    const float DotsH     =  14f;
    const float NameY     = DotsY + DotsH + 10f;     // 370
    const float NameH     =  70f;                    // FontLg(56) 한 줄
    const float D1Y       = NameY + NameH + 10f;     // 450
    const float St1Y      = D1Y + 2f + 10f;          // 462
    const float StH       =  56f;
    const float StGap     =   8f;
    const float St2Y      = St1Y + StH + StGap;      // 526
    const float D2Y       = St2Y + StH + 12f;        // 594
    const float SkHY      = D2Y + 2f + 10f;          // 606
    const float SkHH      =  38f;
    const float SkRY      = SkHY + SkHH + 4f;        // 648
    const float SkRH      =  96f;
    const float DetY      = SkRY + SkRH + 16f;       // 760
    static readonly float DetH = UIScale.BtnFor(UIScale.FontMd);   // 72 — 라벨이 안 눌리는 최소 높이
    public static readonly float CardH = DetY + DetH + 22f;   // 862

    // 아이콘 줄 내부 x (카드 콘텐츠 폭 = CardW - Lp - Rp = 588)
    const float TraitIconSz = 96f;
    const float ActIconSz   = 96f;
    const float PasIconSz   = 84f;
    const float GroupDivX   = 116f;                  // 특성 ┃ 스킬 구분선
    const float SkillX      = 132f;

    // 시작 버튼
    static readonly float SBtnTopY = CardTopY + CardH + 30f;   // 938
    const float SBtnH     =  88f;
    const float SBtnW     = 520f;

    // ── 색상 ──────────────────────────────────────────────────
    static readonly Color SideBg   = new Color(0.05f, 0.06f, 0.11f, 0.92f);
    static readonly Color CardBg   = new Color(0.07f, 0.08f, 0.14f, 0.94f);
    static readonly Color PortBg   = new Color(0.03f, 0.04f, 0.08f, 1.00f);
    static readonly Color StatRowC = new Color(0.10f, 0.11f, 0.19f, 1.00f);
    static readonly Color ChipC    = new Color(0.03f, 0.035f, 0.06f, 0.86f);
    static readonly Color StartC   = new Color(0.11f, 0.72f, 0.58f, 1.00f);
    static readonly Color RelicC   = new Color(0.35f, 0.18f, 0.50f, 1.00f);
    static readonly Color DetailC  = new Color(0.18f, 0.25f, 0.42f, 1.00f);
    static readonly Color ArrowC   = new Color(0.20f, 0.30f, 0.52f, 1.00f);
    static readonly Color RefreshC = new Color(0.16f, 0.42f, 0.62f, 1.00f);
    static readonly Color SlotC    = new Color(0.07f, 0.08f, 0.16f, 1.00f);
    static readonly Color SelectC  = new Color(1.00f, 0.85f, 0.20f, 0.22f);
    static readonly Color Muted    = new Color(0.55f, 0.57f, 0.72f);
    static readonly Color JobChipC = new Color(0.86f, 0.90f, 1.00f, 1.00f);
    static readonly Color DivC     = new Color(0.22f, 0.24f, 0.34f, 0.70f);
    static readonly Color SideDivC = new Color(0.15f, 0.17f, 0.26f, 1.00f);
    static readonly Color LockedC  = new Color(0.09f, 0.10f, 0.17f, 0.90f);

    // =========================================================

    [MenuItem(ProjectKMenu.Lobby + "MainPanel", priority = ProjectKMenu.PrefabPrio + 12)]
    public static void Run()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta =
            new Vector2(UIScale.RefWidth, UIScale.RefHeight);
        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MainPanelCreator] 생성 완료 → " + SavePath);
    }

    public static GameObject Build(GameObject parent)
    {
        // 루트 (투명 — 배경 투과)
        var root = new GameObject("MainPanel", typeof(RectTransform));
        root.transform.SetParent(parent.transform, false);
        var ui = root.AddComponent<MainPanelUI>();
        Stretch(root.GetComponent<RectTransform>());

        // 배경 이미지 (최하위 — 모든 UI 뒤)
        var bgImg = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image));
        bgImg.transform.SetParent(root.transform, false);
        Stretch(bgImg.GetComponent<RectTransform>());
        var bgImgComp = bgImg.GetComponent<Image>();
        bgImgComp.color         = Color.white;
        bgImgComp.preserveAspect = false;
        bgImgComp.raycastTarget  = false;

        // 좌측 사이드
        var relicBtn = BuildSide(root);

        // 우측 영역 (투명)
        var right = new GameObject("RightArea", typeof(RectTransform));
        right.transform.SetParent(root.transform, false);
        {
            var rt = right.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(SideW, 0f);
            rt.offsetMax = Vector2.zero;
        }

        // 카드 컨테이너 (center-top anchor, CardOffX 오른쪽 이동)
        var card = MakeImg("CardContainer", right, CardBg);
        {
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(CardOffX, -CardTopY);
            rt.sizeDelta = new Vector2(CardW, CardH);
        }
        var cardUI = card.AddComponent<GeneralCandidateCardUI>();
        var dots   = BuildCardContent(card, cardUI, withDots: true, withRefresh: true,
                                      withTrait: true, out var refreshBtnGo);

        // 화살표 (소형 56×56, CardContainer 자식 — 카드 기준 배치)
        // anchor(0,1)+pivot(1,0.5) → 오른쪽 엣지가 카드 왼쪽에 ArrGap 간격
        // anchor(1,1)+pivot(0,0.5) → 왼쪽 엣지가 카드 오른쪽에 ArrGap 간격
        // Y: -(PortPad + PortH*0.5) = 초상화 수직 중심
        var arrL = BuildArrow(card, "LeftArrowBtn",  180f);
        var arrR = BuildArrow(card, "RightArrowBtn",   0f);
        {
            var rt = arrL.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);  // 카드 좌상단 앵커
            rt.pivot     = new Vector2(1f, 0.5f);               // 오른쪽 중앙 피벗
            rt.anchoredPosition = new Vector2(-ArrGap, -(PortPad + PortH * 0.5f));
            rt.sizeDelta = new Vector2(ArrSize, ArrSize);
        }
        {
            var rt = arrR.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);  // 카드 우상단 앵커
            rt.pivot     = new Vector2(0f, 0.5f);               // 왼쪽 중앙 피벗
            rt.anchoredPosition = new Vector2(ArrGap, -(PortPad + PortH * 0.5f));
            rt.sizeDelta = new Vector2(ArrSize, ArrSize);
        }

        // 게임 시작 버튼
        var startBtn = MakeBtn(right, "StartBtn", "게 임 시 작", StartC, UIScale.FontLg);
        {
            var lbl = startBtn.GetComponentInChildren<TextMeshProUGUI>();
            lbl.fontStyle = FontStyles.Bold;
            NoWrap(lbl);
        }
        {
            var rt = startBtn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(CardOffX, -SBtnTopY);
            rt.sizeDelta = new Vector2(SBtnW, SBtnH);
        }

        // MainPanelUI 연결
        var so = new SerializedObject(ui);
        so.Update();
        SetRef(so, "_backgroundImage", bgImgComp);
        SetRef(so, "_card",       cardUI);
        SetRef(so, "_relicBtn",   relicBtn != null ? relicBtn.GetComponent<Button>() : null);
        SetRef(so, "_startBtn",   startBtn.GetComponent<Button>());
        SetRef(so, "_prevBtn",    arrL.GetComponent<Button>());
        SetRef(so, "_nextBtn",    arrR.GetComponent<Button>());
        SetRef(so, "_refreshBtn", refreshBtnGo != null ? refreshBtnGo.GetComponent<Button>() : null);
        var dp = so.FindProperty("_pageDots");
        if (dp != null)
        {
            dp.arraySize = 4;
            for (int i = 0; i < 4; i++)
                dp.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];
        }
        so.ApplyModifiedProperties();
        return root;
    }

    // ── 장수 카드 (공유 팩토리) ──────────────────────────────

    /// <summary>
    /// 장수 카드 1장(620×862)을 만들어 붙인다 — MainPanel 과 용병 고용 팝업이 공유한다.
    /// 위치는 호출한 쪽이 정한다 (여기서는 크기만 잡는다).
    ///
    /// ⚠ 이 함수가 카드의 정본이다. 다른 Creator 에서 레이아웃을 복사하지 말 것 —
    ///   복사본이 생기면 스탯 표기·스킬 아이콘 규칙이 화면마다 갈라진다.
    /// </summary>
    /// <param name="withDots">페이지 도트 4개 (MainPanel 전용). false 면 dots 는 빈 배열.</param>
    /// <param name="withRefresh">초상화 우하단 새로고침 버튼 (MainPanel 전용).</param>
    /// <param name="withTrait">
    /// 특성 칸. 특성은 **게임 시작 시 직업별로 하나** 받는 것이라 MainPanel 에만 있다.
    /// 용병 고용은 특성을 주지 않으므로 false — 칸 자체를 빼고 스킬을 왼쪽 끝부터 놓는다.
    /// </param>
    public static GameObject BuildHeroCard(GameObject parent,
                                           bool withDots, bool withRefresh, bool withTrait,
                                           out GeneralCandidateCardUI cardUI,
                                           out Image[] dots,
                                           out GameObject refreshBtnGo)
    {
        var card = MakeImg("CardContainer", parent, CardBg);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(CardW, CardH);
        cardUI = card.AddComponent<GeneralCandidateCardUI>();
        dots   = BuildCardContent(card, cardUI, withDots, withRefresh, withTrait, out refreshBtnGo);
        return card;
    }

    /// <summary>카드 좌우 페이지 화살표. 방향은 dirDeg(0=오른쪽, 180=왼쪽).</summary>
    public static GameObject BuildPageArrow(GameObject parent, string name, float dirDeg)
        => BuildArrow(parent, name, dirDeg);

    // ── 카드 컨텐츠 ──────────────────────────────────────────

    static Image[] BuildCardContent(GameObject card, GeneralCandidateCardUI cUI,
                                    bool withDots, bool withRefresh, bool withTrait,
                                    out GameObject refreshBtnGo)
    {
        var sel = MakeImg("SelectionOverlay", card, SelectC);
        Stretch(sel.GetComponent<RectTransform>());
        sel.GetComponent<Image>().raycastTarget = false;
        sel.SetActive(false);

        var gb = MakeImg("GradeBorder", card, Color.gray);
        {
            var rt = gb.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(GradeW, 0f);
        }
        gb.GetComponent<Image>().raycastTarget = false;

        var pb = MakeImg("PortraitBg", card, PortBg);
        {
            var rt = pb.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(GradeW, -(PortPad + PortH));
            rt.offsetMax = new Vector2(0f, -PortPad);
        }
        pb.GetComponent<Image>().raycastTarget = false;

        var pi = MakeImg("PortraitImage", pb, Color.white);
        pi.GetComponent<Image>().preserveAspect = true;
        pi.GetComponent<Image>().raycastTarget  = false;
        Stretch(pi.GetComponent<RectTransform>());

        var pp = new GameObject("PortraitPreview", typeof(RectTransform));
        pp.transform.SetParent(pb.transform, false);
        pp.AddComponent<UnitAppearanceBridge>();
        Stretch(pp.GetComponent<RectTransform>());
        pp.SetActive(false);

        // 직업 / 등급 배지 — 초상화 모서리에 얹어 별도 행을 없앴다
        var jobChipTmp   = BuildPortraitChip(pb, "JobChip",   "JobChipText",   "기사", JobChipC,   left: true);
        var gradeChipTmp = BuildPortraitChip(pb, "GradeChip", "GradeChipText", "일반", Color.white, left: false);

        // 새로 고침 — 초상화 우하단에 얹는 입체 버튼.
        // 예전엔 카드 우측의 평평한 84×42 라 눈에 안 띄고 라벨도 두 줄로 접혔다.
        refreshBtnGo = null;
        if (withRefresh)
        {
            refreshBtnGo = EditorUIBuilder
                .RaisedTextBtn(pb, "RefreshBtn", "새로고침", UIScale.FontSm, RefreshC).gameObject;
            var rt = refreshBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-12f, 12f);
            rt.sizeDelta        = new Vector2(RefreshW, RefreshH);
            NoWrap(refreshBtnGo.GetComponentInChildren<TextMeshProUGUI>());
        }

        var dots = withDots ? BuildPageDots(card) : System.Array.Empty<Image>();

        BuildCardBody(card, cUI, pb, pi, pp, gb, sel, jobChipTmp, gradeChipTmp, withTrait);
        return dots;
    }

    // 페이지 도트 (소형) — MainPanel 전용
    static Image[] BuildPageDots(GameObject card)
    {
        var dr = new GameObject("PageDotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        dr.transform.SetParent(card.transform, false);
        {
            var rt = dr.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -DotsY);
            rt.sizeDelta = new Vector2(80f, DotsH);
        }
        var dhlg = dr.GetComponent<HorizontalLayoutGroup>();
        dhlg.childAlignment        = TextAnchor.MiddleCenter;
        dhlg.childForceExpandWidth  = false;
        dhlg.childForceExpandHeight = false;
        dhlg.spacing = 10f;

        var dots = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            float ds = i == 0 ? 14f : 10f;
            var d = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
            d.transform.SetParent(dr.transform, false);
            var drt = d.GetComponent<RectTransform>();
            drt.sizeDelta = new Vector2(ds, ds);
            dots[i] = d.GetComponent<Image>();
            dots[i].color = i == 0 ? Color.white : new Color(0.35f, 0.35f, 0.55f, 0.8f);
            var le = d.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = ds;
        }
        return dots;
    }

    // 카드 본문 — 이름 · 스탯 2행 · 특성┃스킬 아이콘 · 자세히 보기 + 필드 와이어링
    static void BuildCardBody(GameObject card, GeneralCandidateCardUI cUI,
                              GameObject pb, GameObject pi, GameObject pp,
                              GameObject gb, GameObject sel,
                              TextMeshProUGUI jobChipTmp, TextMeshProUGUI gradeChipTmp,
                              bool withTrait)
    {
        const int lp = Lp, rp = Rp;

        // 이름 텍스트 (카드 전체 폭, 가운데 정렬)
        //  긴 이름은 접히지 않고 줄어들게 한다 — 칸보다 한 줄이 크면 통째로 사라진다.
        var nameTmp = MakeTMP(card, "NameText", "이름", UIScale.FontLg, FontStyles.Bold);
        nameTmp.color = Color.white; nameTmp.alignment = TextAlignmentOptions.Center;
        NoWrap(nameTmp);
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontMd;
        nameTmp.fontSizeMax      = UIScale.FontLg;
        TAF(nameTmp.rectTransform, NameY, NameH, lp, rp);

        var d1 = MakeImg("Div1", card, DivC);
        d1.GetComponent<Image>().raycastTarget = false;
        TAF(d1.GetComponent<RectTransform>(), D1Y, 2f, lp, rp);

        var r1 = BuildStatRow(card, "StatsRow1",
            ("HpCell", "체력", StatColors.Hp), ("AtkCell", "공격", StatColors.Atk));
        TAF(r1.GetComponent<RectTransform>(), St1Y, StH, lp, rp);

        var r2 = BuildStatRow(card, "StatsRow2",
            ("DefCell", "방어", StatColors.Def), ("SoldierCell", "병사", StatColors.Soldier));
        TAF(r2.GetComponent<RectTransform>(), St2Y, StH, lp, rp);

        var d2 = MakeImg("Div2", card, DivC);
        d2.GetComponent<Image>().raycastTarget = false;
        TAF(d2.GetComponent<RectTransform>(), D2Y, 2f, lp, rp);

        // ── 특성 ┃ 스킬 ─────────────────────────────────────
        //  두 종류를 한 줄에 두되 세로선으로 갈라 놓는다.
        //  라벨 줄(SkHY)과 아이콘 줄(SkRY)의 x 를 맞춰 어느 쪽 라벨인지 읽히게 한다.
        //
        //  ⚠ withTrait=false (용병 고용) 면 특성 칸을 통째로 뺀다.
        //    특성은 게임 시작 시 직업별로 하나 받는 것이고 용병 고용에는 안 붙는다.
        //    칸만 비워 두면 "언젠가 열리는 자리" 로 읽혀 오해를 부른다.
        float skillX = withTrait ? SkillX : 0f;

        var hdr = new GameObject("SkillHeaderRow", typeof(RectTransform));
        hdr.transform.SetParent(card.transform, false);
        TAF(hdr.GetComponent<RectTransform>(), SkHY, SkHH, lp, rp);
        if (withTrait) BuildGroupLabel(hdr, "TraitHeader", "특성", 0f, TraitIconSz);
        BuildGroupLabel(hdr, "SkillHeader", "스킬", skillX, 260f);

        // 어두운 패시브 칸이 왜 비었는지 알려 준다 (등급이 오르면 열린다)
        var slotTmp = MakeTMP(hdr, "PassiveSlotText", "패시브 1/3", UIScale.FontSm, FontStyles.Normal);
        slotTmp.color = Muted; slotTmp.alignment = TextAlignmentOptions.MidlineRight;
        slotTmp.raycastTarget = false;
        NoWrap(slotTmp);
        Stretch(slotTmp.rectTransform);

        var iconRow = new GameObject("SkillIconRow", typeof(RectTransform));
        iconRow.transform.SetParent(card.transform, false);
        TAF(iconRow.GetComponent<RectTransform>(), SkRY, SkRH, lp, rp);

        TraitIconUI traitIconUI = null;
        if (withTrait)
        {
            traitIconUI = BuildTraitIconUI(iconRow, 0f, TraitIconSz);

            var groupDiv = MakeImg("GroupDivider", iconRow, DivC);
            groupDiv.GetComponent<Image>().raycastTarget = false;
            var rt = groupDiv.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(GroupDivX, 8f);
            rt.offsetMax = new Vector2(GroupDivX + 2f, -8f);
        }

        var actIcon = BuildSkillIconUI(iconRow, "ActiveSkillIcon", skillX, ActIconSz,
                                       SkillIconUI.ActiveFrame);
        var pasIcons = new SkillIconUI[3];
        float px = skillX + ActIconSz + 14f;
        for (int i = 0; i < 3; i++)
        {
            pasIcons[i] = BuildSkillIconUI(iconRow, $"PassiveSkillIcon{i}", px, PasIconSz,
                                           SkillIconUI.LockedFrame);
            px += PasIconSz + 12f;
        }

        var detBtn = MakeBtn(card, "DetailBtn", "자세히 보기", DetailC, UIScale.FontMd);
        NoWrap(detBtn.GetComponentInChildren<TextMeshProUGUI>());
        TAF(detBtn.GetComponent<RectTransform>(), DetY, DetH, lp + 8, rp + 8);

        var cso = new SerializedObject(cUI);
        cso.Update();
        SetRef(cso, "_selectionOverlay", sel.GetComponent<Image>());
        SetRef(cso, "_gradeBorder",      gb.GetComponent<Image>());
        SetRef(cso, "_portraitBg",       pb.GetComponent<Image>());
        SetRef(cso, "_portraitImage",    pi.GetComponent<Image>());
        SetRef(cso, "_portraitBridge",   pp.GetComponent<UnitAppearanceBridge>());
        SetRef(cso, "_nameText",         nameTmp);
        SetRef(cso, "_jobChipText",      jobChipTmp);
        SetRef(cso, "_gradeChipText",    gradeChipTmp);
        SetRef(cso, "_detailBtn",        detBtn.GetComponent<Button>());
        SetRef(cso, "_hpText",      GetValTMP(r1.transform, "HpCell"));
        SetRef(cso, "_atkText",     GetValTMP(r1.transform, "AtkCell"));
        SetRef(cso, "_defText",     GetValTMP(r2.transform, "DefCell"));
        SetRef(cso, "_soldierText", GetValTMP(r2.transform, "SoldierCell"));
        SetRef(cso, "_traitIconUI", traitIconUI);
        SetRef(cso, "_activeSkillIcon", actIcon);
        SetRef(cso, "_passiveSlotText", slotTmp);
        var pp2 = cso.FindProperty("_passiveSkillIcons");
        if (pp2 != null)
        {
            pp2.arraySize = 3;
            for (int i = 0; i < 3; i++)
                pp2.GetArrayElementAtIndex(i).objectReferenceValue = pasIcons[i];
        }
        cso.ApplyModifiedProperties();
    }

    // ── 초상화 위 직업·등급 배지 ─────────────────────────────

    static TextMeshProUGUI BuildPortraitChip(GameObject portrait, string chipName, string tmpName,
                                             string text, Color color, bool left)
    {
        var chip = MakeImg(chipName, portrait, ChipC);
        {
            var rt = chip.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rt.pivot     = new Vector2(left ? 0f : 1f, 1f);
            rt.anchoredPosition = new Vector2(left ? 12f : -12f, -12f);
            rt.sizeDelta        = new Vector2(ChipW, ChipH);
        }
        chip.GetComponent<Image>().raycastTarget = false;

        var tmp = MakeTMP(chip, tmpName, text, UIScale.FontSm, FontStyles.Bold);
        tmp.color = color; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        NoWrap(tmp);
        Stretch(tmp.rectTransform);
        return tmp;
    }

    // ── 그룹 라벨 (특성 / 스킬) ──────────────────────────────

    static void BuildGroupLabel(GameObject parent, string name, string text, float x, float w)
    {
        var tmp = MakeTMP(parent, name, text, UIScale.FontSm, FontStyles.Bold);
        tmp.color = Muted; tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.raycastTarget = false;
        NoWrap(tmp);
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(x,     0f);
        rt.offsetMax = new Vector2(x + w, 0f);
    }

    // ── 사이드바 ─────────────────────────────────────────────

    static GameObject BuildSide(GameObject parent)
    {
        var side = MakeImg("SideColumn", parent, SideBg);
        {
            var rt = side.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(SideW, 0f);
        }

        var border = MakeImg("Border", side, SideDivC);
        {
            var rt = border.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-2f, 0f); rt.offsetMax = Vector2.zero;
        }
        border.GetComponent<Image>().raycastTarget = false;

        // 타이틀 이미지
        var ta = MakeImg("TitleArea", side, Color.clear);
        {
            var rt = ta.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0f, -TitleH); rt.offsetMax = Vector2.zero;
        }
        var sp = GetOrCreateTitleSprite();
        if (sp != null)
        {
            var img = ta.GetComponent<Image>();
            img.sprite = sp; img.type = Image.Type.Simple;
            img.preserveAspect = false; img.raycastTarget = false;
        }

        // 타이틀 텍스트
        //  ⚠ "PIXEL" 은 112pt × 5자 ≈ 347px 로 340px 칸을 넘겨 "PIXE / L" 로 접혔었다.
        //    NoWrap + AutoSize — 접히는 대신 칸에 맞게 줄어든다.
        var px = MakeTMP(ta, "Pixel", "PIXEL", 112f, FontStyles.Bold);
        px.color = Color.white; px.alignment = TextAlignmentOptions.Center; px.raycastTarget = false;
        AutoFit(px, 78f, 112f);
        { var rt = px.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, -16f); rt.sizeDelta = new Vector2(SideBtnW, 104f); }

        var gn = MakeTMP(ta, "General", "GENERAL", 68f, FontStyles.Bold);
        gn.color = new Color(0.35f, 0.65f, 1f); gn.alignment = TextAlignmentOptions.Center; gn.raycastTarget = false;
        AutoFit(gn, 48f, 68f);
        { var rt = gn.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, -124f); rt.sizeDelta = new Vector2(SideBtnW, 84f); }

        var sub = MakeTMP(ta, "SubTitle", "픽셀 제너럴", UIScale.FontSm, FontStyles.Normal);
        sub.color = new Color(0.40f, 0.52f, 0.80f); sub.alignment = TextAlignmentOptions.Center; sub.raycastTarget = false;
        NoWrap(sub);
        { var rt = sub.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 18f); rt.sizeDelta = new Vector2(SideBtnW, UIScale.RowSm); }

        // 구분선
        var tDiv = MakeImg("TitleDiv", side, DivC);
        { var rt = tDiv.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.offsetMin = new Vector2(20f, -(TitleH + 2f)); rt.offsetMax = new Vector2(-20f, -TitleH); }
        tDiv.GetComponent<Image>().raycastTarget = false;

        // 버튼 영역 — 가로형 행 (정사각형이면 무엇을 누르는 건지 덜 읽힌다)
        var btnArea = new GameObject("BtnArea", typeof(RectTransform));
        btnArea.transform.SetParent(side.transform, false);
        { var rt = btnArea.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 1f); rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0f, -(TitleH + 8f)); }
        var vlg = btnArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        // ⚠ childControl* 를 끄면 LayoutElement 의 preferred 크기를 레이아웃이 아예 보지 않는다.
        //   그러면 버튼은 RectTransform 기본값 100×100 정사각형으로 남는다 —
        //   "가로로 길게" 가 반영되지 않던 원인이 이것이었다.
        vlg.childControlWidth = true;  vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        vlg.spacing = 16f; vlg.padding = new RectOffset((int)SidePad, (int)SidePad, 24, 24);

        var relicBtn = BuildWideBtn(btnArea, "RelicBtn", "유물", RelicC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_relic.png");
        var rle = relicBtn.AddComponent<LayoutElement>();
        rle.preferredWidth = SideBtnW; rle.preferredHeight = 116f;

        var dogsanBtn = BuildLockedBtn(btnArea, "DogsanBtn", "도감");
        var dle = dogsanBtn.AddComponent<LayoutElement>();
        dle.preferredWidth = SideBtnW; dle.preferredHeight = 100f;

        var etcBtn = BuildLockedBtn(btnArea, "EtcBtn", "기타");
        var ele = etcBtn.AddComponent<LayoutElement>();
        ele.preferredWidth = SideBtnW; ele.preferredHeight = 100f;

        return relicBtn;
    }

    // ── 화살표 ────────────────────────────────────────────────

    // ◀ ▶ 글리프는 폰트에 없다 (□ 로 렌더됨) → 꺾쇠 도형으로 그린다.
    // dirDeg: 0 = 오른쪽, 180 = 왼쪽
    // 예전엔 56×56 에 검정 45% 라 흙 배경에 그대로 묻혔다 →
    // 88×88 청색 입체 + 밝은 꺾쇠 + 뒤에 어두운 판을 깔아 대비를 만든다.
    static GameObject BuildArrow(GameObject parent, string name, float dirDeg)
    {
        var go = EditorUIBuilder.Go(name, parent);
        EditorUIBuilder.RaisedBtnOn(go, ArrowC, out var body);
        var mark = EditorUIBuilder.Chevron(body, "Mark", UIScale.FontLg, dirDeg, Color.white);
        var rt = mark.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    // ── 사이드 버튼 (가로형) ──────────────────────────────────
    //  [아이콘][라벨 ─────────] › 형태. 정사각형보다 무엇을 누르는지 잘 읽힌다.

    static GameObject BuildWideBtn(GameObject parent, string name, string label, Color bg, string iconPath)
    {
        // UI 규칙 1: 누를 수 있는 버튼은 음각 처리 (내용은 body 아래로)
        var go = EditorUIBuilder.Go(name, parent);
        EditorUIBuilder.RaisedBtnOn(go, bg, out var body);

        var iGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iGo.transform.SetParent(body.transform, false);
        {
            var rt = iGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(14f, 12f);
            rt.offsetMax = new Vector2(14f + 76f, -12f);
        }
        var ii = iGo.GetComponent<Image>(); ii.preserveAspect = true; ii.raycastTarget = false;
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath); if (sp != null) ii.sprite = sp;

        var lt = MakeTMP(body, "Label", label, UIScale.FontMd, FontStyles.Bold);
        lt.alignment = TextAlignmentOptions.MidlineLeft; lt.color = Color.white; lt.raycastTarget = false;
        NoWrap(lt);
        {
            var rt = lt.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(104f, 0f);
            rt.offsetMax = new Vector2(-46f, 0f);
        }

        // › 표식 — 눌러서 들어가는 화면이라는 신호
        var chev = EditorUIBuilder.Chevron(body, "Chevron", 26f, 0f, new Color(1f, 1f, 1f, 0.65f));
        {
            var rt = chev.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-18f, 0f);
        }
        return go;
    }

    static GameObject BuildLockedBtn(GameObject parent, string name, string label)
    {
        var go = MakeImg(name, parent, LockedC);
        var dim = new Color(0.30f, 0.32f, 0.48f);

        // 🔒 이모지는 폰트에 없다 (□ 로 렌더됨) → 자물쇠 도형으로 그린다
        var lockGo = EditorUIBuilder.PadLock(go, "Lock", 52f, dim);
        {
            var rt = lockGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(28f, 0f);
        }

        var tmp = MakeTMP(go, "Label", label, UIScale.FontMd, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.color = dim; tmp.raycastTarget = false;
        NoWrap(tmp);
        {
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(104f, 0f);
            rt.offsetMax = new Vector2(-46f, 0f);
        }

        var soon = MakeTMP(go, "SoonLabel", "준비 중", UIScale.FontSm, FontStyles.Normal);
        soon.alignment = TextAlignmentOptions.MidlineRight;
        soon.color = new Color(0.24f, 0.26f, 0.40f); soon.raycastTarget = false;
        NoWrap(soon);
        {
            var rt = soon.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(104f, 0f);
            rt.offsetMax = new Vector2(-20f, 0f);
        }
        return go;
    }

    // ── 특성 아이콘 UI ────────────────────────────────────────

    static TraitIconUI BuildTraitIconUI(GameObject row, float x, float size)
    {
        const float TipW = 420f;

        var root = new GameObject("TraitIconUI", typeof(RectTransform));
        root.transform.SetParent(row.transform, false);
        var traitUI = root.AddComponent<TraitIconUI>();
        AnchorIcon(root, x, size);

        var (btn, img) = BuildIconSlot(root, SlotC);

        // 상세 툴팁 — 보상 카드·특성 슬롯과 같은 공용 컴포넌트
        var tooltip = InfoTooltipBuilder.Build(root, TipW);

        var so = new SerializedObject(traitUI);
        so.Update();
        SetRef(so, "_iconImage", img);
        SetRef(so, "_iconBtn",   btn);
        SetRef(so, "_tooltip",   tooltip);
        so.ApplyModifiedProperties();
        return traitUI;
    }

    // ── 스킬 아이콘 (액티브 1 + 패시브 3) ────────────────────
    //  테두리 색으로 액티브(금)/패시브(파랑)/잠김(회색)을 구분한다.
    //  SkillIconUI 가 런타임에 _frame 색을 갈아 끼운다.

    static SkillIconUI BuildSkillIconUI(GameObject row, string name, float x, float size, Color frame)
    {
        const float TipW = 420f;

        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(row.transform, false);
        var skillUI = root.AddComponent<SkillIconUI>();
        AnchorIcon(root, x, size);

        // 테두리는 아이콘 판의 "뒤 형제" 가 아니라 바깥 판으로 둔다 (UI 규칙 3)
        var frameImg = MakeImg("Frame", root, frame).GetComponent<Image>();
        Stretch(frameImg.rectTransform);
        frameImg.raycastTarget = false;

        var (btn, img) = BuildIconSlot(root, SlotC, inset: 3f);
        var tooltip = InfoTooltipBuilder.Build(root, TipW);

        var so = new SerializedObject(skillUI);
        so.Update();
        SetRef(so, "_frame",     frameImg);
        SetRef(so, "_iconImage", img);
        SetRef(so, "_iconBtn",   btn);
        SetRef(so, "_tooltip",   tooltip);
        so.ApplyModifiedProperties();
        return skillUI;
    }

    //  아이콘 칸 공통 — 누를 수 있는 판 + 그 안의 아이콘 이미지
    static (Button btn, Image img) BuildIconSlot(GameObject root, Color bg, float inset = 0f)
    {
        var btnGo = new GameObject("IconBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(root.transform, false);
        {
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
        btnGo.GetComponent<Image>().color = bg;

        var imgGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(btnGo.transform, false);
        {
            var rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.12f);
            rt.anchorMax = new Vector2(0.88f, 0.88f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        var img = imgGo.GetComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget  = false;
        img.color          = new Color(0.25f, 0.25f, 0.38f);

        return (btnGo.GetComponent<Button>(), img);
    }

    //  아이콘 줄(높이 SkRH) 안에서 왼쪽 x 부터 size 정사각, 세로 중앙
    static void AnchorIcon(GameObject go, float x, float size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta        = new Vector2(size, size);
    }

    // ── 스탯 행 (앵커 기반 — LayoutGroup 미사용) ──────────────
    //  인게임·상점 용병 카드와 같은 표기로 맞춘다:
    //    [라벨 좌측][값 우측], 값은 StatColors, 둘 다 NoWrap 고정 크기.
    //  예전엔 라벨 우측정렬 + 값 좌측정렬에 값 칸이 120px 뿐이라
    //  "1,988" 이 두 줄로 접혔다.

    static GameObject BuildStatRow(GameObject parent, string rowName,
        (string id, string label, Color color) c0, (string id, string label, Color color) c1)
    {
        var row = new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        BuildStatCell(row, c0.id, c0.label, c0.color, 0f,   6f);
        BuildStatCell(row, c1.id, c1.label, c1.color, 0.5f, -6f);
        return row;
    }

    static void BuildStatCell(GameObject parent, string id, string label, Color valueColor,
                              float xStart, float inset)
    {
        const float LabelW = 92f;

        var cell = MakeImg(id, parent, StatRowC);
        {
            var rt = cell.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xStart,        0f);
            rt.anchorMax = new Vector2(xStart + 0.5f, 1f);
            rt.offsetMin = new Vector2(inset > 0f ? 0f : -inset, 0f);
            rt.offsetMax = new Vector2(inset > 0f ? -inset : 0f, 0f);
        }
        cell.GetComponent<Image>().raycastTarget = false;

        var lt = MakeTMP(cell, "LabelText", label, UIScale.FontSm, FontStyles.Normal);
        lt.color = Muted; lt.alignment = TextAlignmentOptions.MidlineLeft; lt.raycastTarget = false;
        NoWrap(lt);
        {
            var rt = lt.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(16f, 0f);
            rt.offsetMax = new Vector2(16f + LabelW, 0f);
        }

        var vt = MakeTMP(cell, "ValueText", "0", UIScale.FontMd, FontStyles.Bold);
        vt.color = valueColor; vt.alignment = TextAlignmentOptions.MidlineRight; vt.raycastTarget = false;
        NoWrap(vt);
        {
            var rt = vt.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f + LabelW + 8f, 0f);
            rt.offsetMax = new Vector2(-16f, 0f);
        }
    }

    // ── 타이틀 이미지 생성 ────────────────────────────────────

    static Sprite GetOrCreateTitleSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(TitleImgPath);
        if (existing != null) return existing;

        const int W = 380, H = 300;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float fx = (float)x / W - 0.5f;
            float fy = (float)y / H;
            float glow  = Mathf.Max(0f, 1f - (fx * fx * 3.5f + (fy - 0.55f) * (fy - 0.55f) * 5f) * 4.5f) * 0.24f;
            float alpha = Mathf.Clamp01(fy * 1.4f);
            tex.SetPixel(x, y, new Color(0.04f + glow * 0.18f, 0.05f + glow * 0.32f, 0.12f + glow * 0.88f, alpha * 0.90f));
        }
        for (int x = 0; x < W; x++)
        {
            tex.SetPixel(x, H - 1, new Color(0.30f, 0.55f, 1f, 1f));
            tex.SetPixel(x, H - 2, new Color(0.22f, 0.44f, 0.88f, 0.8f));
            tex.SetPixel(x, H - 3, new Color(0.16f, 0.34f, 0.72f, 0.5f));
        }
        for (int y = 20; y < H - 20; y++)
        {
            float a = Mathf.Sin((float)(y - 20) / (H - 40) * Mathf.PI) * 0.55f;
            tex.SetPixel(0, y, new Color(0.28f, 0.52f, 1f, a));
            tex.SetPixel(1, y, new Color(0.22f, 0.44f, 0.85f, a * 0.5f));
        }
        tex.Apply();

        var dir = System.IO.Path.GetDirectoryName(
            System.IO.Path.Combine(Application.dataPath, "..", TitleImgPath));
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "..", TitleImgPath),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(TitleImgPath);
        var imp = AssetDatabase.LoadAssetAtPath<TextureImporter>(TitleImgPath);
        if (imp != null)
        {
            imp.textureType        = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.spritePivot        = new Vector2(0.5f, 0.5f);
            imp.filterMode         = FilterMode.Bilinear;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(TitleImgPath);
    }

    // ── 헬퍼 ──────────────────────────────────────────────────

    static void Stretch(RectTransform rt) => EditorUIBuilder.Stretch(rt);

    /// <summary>
    /// 줄바꿈 금지 + 넘침 허용.
    /// ⚠ 칸보다 한 줄이 크면 Ellipsis/Truncate 는 그 줄을 통째로 버린다 —
    ///   접히거나 사라지는 사고는 전부 이 두 줄로 막는다.
    /// </summary>
    static void NoWrap(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
    }

    /// <summary>NoWrap + 칸에 맞춰 축소 (넘치면 잘리는 대신 작아진다).</summary>
    static void AutoFit(TextMeshProUGUI tmp, float min, float max)
    {
        NoWrap(tmp);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = min;
        tmp.fontSizeMax      = max;
    }

    /// Top-Anchor Fill: card 상단 기준으로 y 위치, h 높이, 좌우 패딩
    static void TAF(RectTransform rt, float y, float h, int lp = 0, int rp = 0)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2( lp, -(y + h));
        rt.offsetMax = new Vector2(-rp,  -y);
    }

    static void SetRef(SerializedObject so, string field, Object obj)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = obj;
    }

    static GameObject MakeImg(string name, GameObject parent, Color color)
        => EditorUIBuilder.Panel(parent, name, color);

    // UI 규칙: 누를 수 있는 버튼은 음각 처리 (EditorUIBuilder.RaisedTextBtn)
    static GameObject MakeBtn(GameObject parent, string name, string label, Color bg, float fontSize)
        => EditorUIBuilder.RaisedTextBtn(parent, name, label, fontSize, bg).gameObject;

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static TextMeshProUGUI GetValTMP(Transform row, string cellName)
    {
        var cell = row.Find(cellName);
        if (cell == null) return null;
        var val = cell.Find("ValueText");
        if (val == null) return null;
        return val.GetComponent<TextMeshProUGUI>();
    }
}
