using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MainPanelCreator.cs
//  Tools > Project K > 로비 UI > Create MainPanel Prefab
//
//  ┌──────────────────────────────────────────────────────────┐
//  │  [타이틀 이미지]  │  (배경)  ◀ [초상화 260px] ▶  (배경) │
//  │   [유물]          │          ● ○ ○ ○                    │
//  │   (도감)          │           이름                       │
//  │   (기타)          │        직업  ·  등급                  │
//  │                   │        체력  0  공격  0              │
//  │                   │        방어  0  병사  0              │
//  │                   │        [자세히 보기]                  │
//  │                   │  특성 [?][?][?]                      │
//  │                   │        [게임 시작]                    │
//  └──────────────────────────────────────────────────────────┘
//
//  • 패널 루트: 투명 (배경 투과)
//  • 좌측 380px: 반투명 다크
//  • 카드 440×840px: 반투명 다크, 우측으로 CardOffX 이동
//  • 화살표: 56×56 소형 반투명
// ============================================================

public static class MainPanelCreator
{
    const string SavePath     = "Assets/_project/2.Prefabs/UI/Lobby/MainPanel.prefab";
    const string TitleImgPath = "Assets/_project/3.Textures/UI/Lobby/title_pixel_general.png";

    // ── 레이아웃 상수 ─────────────────────────────────────────
    const float SideW     = 380f;

    // 카드 (우측 오프셋 = 배경 더 많이 노출)
    const float CardW     = 440f;
    const float CardOffX  =  70f;   // 카드 중심 오른쪽으로 이동
    const float CardTopY  =  60f;

    // 초상화
    const float PortPad   =  14f;
    const float PortH     = 260f;
    const float GradeW    =   5f;

    // 화살표 (소형, CardContainer 기준)
    // 부모: CardContainer  /  카드 밖 좌우로 ArrGap 떨어져 초상화 수직 중심 정렬
    const float ArrSize   =  56f;
    const float ArrGap    =  10f;
    // card-local Y: -(PortPad + PortH * 0.5) = -144

    // 카드 내부 Y (top-anchor 기준 = card 상단에서 몇 px)
    const float DotsY     = PortPad + PortH + 12f;  // 286
    const float DotsH     =  14f;
    const float NameY     = DotsY + DotsH + 10f;    // 310
    const float NameH     =  52f;
    const float JobY      = NameY + NameH + 8f;     // 370
    const float JobH      =  38f;
    const float D1Y       = JobY + JobH + 14f;       // 422
    const float St1Y      = D1Y + 2f + 10f;          // 434
    const float StH       =  58f;
    const float St2Y      = St1Y + StH + 8f;         // 500
    const float DetY      = St2Y + StH + 14f;        // 572
    const float DetH      =  64f;
    const float D2Y       = DetY + DetH + 16f;       // 652
    const float TrHY      = D2Y + 2f + 10f;          // 664
    const float TrHH      =  44f;
    const float TrSY      = TrHY + TrHH + 10f;       // 718
    const float TrSH      =  96f;
    const float CardH     = TrSY + TrSH + 26f;       // 840

    // 시작 버튼
    const float SBtnTopY  = CardTopY + CardH + 34f;  // 934
    const float SBtnH     =  84f;
    const float SBtnW     = CardW + 40f;              // 480

    // ── 색상 ──────────────────────────────────────────────────
    static readonly Color SideBg   = new Color(0.05f, 0.06f, 0.11f, 0.88f);
    static readonly Color CardBg   = new Color(0.07f, 0.08f, 0.14f, 0.90f);
    static readonly Color PortBg   = new Color(0.03f, 0.04f, 0.08f, 1.00f);
    static readonly Color StartC   = new Color(0.11f, 0.72f, 0.58f, 1.00f);
    static readonly Color RelicC   = new Color(0.35f, 0.18f, 0.50f, 1.00f);
    static readonly Color DetailC  = new Color(0.18f, 0.25f, 0.42f, 1.00f);
    static readonly Color ArrowC   = new Color(0.00f, 0.00f, 0.00f, 0.45f);
    static readonly Color SlotC    = new Color(0.07f, 0.08f, 0.16f, 1.00f);
    static readonly Color SelectC  = new Color(1.00f, 0.85f, 0.20f, 0.22f);
    static readonly Color Muted    = new Color(0.55f, 0.57f, 0.72f);
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
        var dots   = BuildCardContent(card, cardUI, out var refreshBtnGo);

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
        var startBtn = MakeBtn(right, "StartBtn", "게임 시작", StartC, UIScale.FontMd);
        startBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
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

    // ── 카드 컨텐츠 ──────────────────────────────────────────

    static Image[] BuildCardContent(GameObject card, GeneralCandidateCardUI cUI, out GameObject refreshBtnGo)
    {
        refreshBtnGo = null;
        int lp = (int)(GradeW + 10f), rp = 10;

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

        // 페이지 도트 (소형)
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

        // 이름 텍스트 (카드 전체 폭, 가운데 정렬)
        var nameTmp = MakeTMP(card, "NameText", "이름", UIScale.FontLg, FontStyles.Bold);
        nameTmp.color = Color.white; nameTmp.alignment = TextAlignmentOptions.Center;
        TAF(nameTmp.rectTransform, NameY, NameH, lp, rp);

        // 새로 고침 버튼 (카드 우측 고정, 이름 행 수직 중앙)
        refreshBtnGo = new GameObject("RefreshBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        refreshBtnGo.transform.SetParent(card.transform, false);
        refreshBtnGo.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.42f, 0.85f);
        {
            var rt = refreshBtnGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-rp, -(NameY + NameH * 0.5f));
            rt.sizeDelta        = new Vector2(84f, 42f);
        }
        var refreshLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        refreshLabelGo.transform.SetParent(refreshBtnGo.transform, false);
        Stretch(refreshLabelGo.GetComponent<RectTransform>());
        var refreshLabelTmp = refreshLabelGo.GetComponent<TextMeshProUGUI>();
        refreshLabelTmp.text          = "새로고침";
        refreshLabelTmp.fontSize      = UIScale.FontSm - 4f;
        refreshLabelTmp.alignment     = TextAlignmentOptions.Center;
        refreshLabelTmp.color         = new Color(0.75f, 0.88f, 1f);
        refreshLabelTmp.raycastTarget = false;

        var jobTmp = MakeTMP(card, "JobGradeText", "직업  ·  등급", UIScale.FontSm, FontStyles.Normal);
        jobTmp.color = Muted; jobTmp.alignment = TextAlignmentOptions.Center;
        TAF(jobTmp.rectTransform, JobY, JobH, lp, rp);

        var d1 = MakeImg("Div1", card, DivC);
        d1.GetComponent<Image>().raycastTarget = false;
        TAF(d1.GetComponent<RectTransform>(), D1Y, 2f, 14, 14);

        var r1 = BuildStatRow(card, "StatsRow1", ("HpCell", "체력"), ("AtkCell", "공격"));
        TAF(r1.GetComponent<RectTransform>(), St1Y, StH, lp, rp);

        var r2 = BuildStatRow(card, "StatsRow2", ("DefCell", "방어"), ("SoldierCell", "병사"));
        TAF(r2.GetComponent<RectTransform>(), St2Y, StH, lp, rp);

        var detBtn = MakeBtn(card, "DetailBtn", "자세히 보기", DetailC, UIScale.FontSm);
        TAF(detBtn.GetComponent<RectTransform>(), DetY, DetH, lp + 8, rp + 8);

        var d2 = MakeImg("Div2", card, DivC);
        d2.GetComponent<Image>().raycastTarget = false;
        TAF(d2.GetComponent<RectTransform>(), D2Y, 2f, 14, 14);

        var th = MakeTMP(card, "TraitHeader", "특성", UIScale.FontSm, FontStyles.Bold);
        th.color = Muted; th.alignment = TextAlignmentOptions.Left;
        TAF(th.rectTransform, TrHY, TrHH, lp + 4, rp + 4);

        var traitIconUI = BuildTraitIconUI(card);

        var cso = new SerializedObject(cUI);
        cso.Update();
        SetRef(cso, "_selectionOverlay", sel.GetComponent<Image>());
        SetRef(cso, "_gradeBorder",      gb.GetComponent<Image>());
        SetRef(cso, "_portraitBg",       pb.GetComponent<Image>());
        SetRef(cso, "_portraitImage",    pi.GetComponent<Image>());
        SetRef(cso, "_portraitBridge",   pp.GetComponent<UnitAppearanceBridge>());
        SetRef(cso, "_nameText",         nameTmp);
        SetRef(cso, "_jobGradeText",     jobTmp);
        SetRef(cso, "_detailBtn",        detBtn.GetComponent<Button>());
        SetRef(cso, "_hpText",      GetValTMP(r1.transform, "HpCell"));
        SetRef(cso, "_atkText",     GetValTMP(r1.transform, "AtkCell"));
        SetRef(cso, "_defText",     GetValTMP(r2.transform, "DefCell"));
        SetRef(cso, "_soldierText", GetValTMP(r2.transform, "SoldierCell"));
        SetRef(cso, "_traitIconUI", traitIconUI);
        cso.ApplyModifiedProperties();
        return dots;
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
            rt.offsetMin = new Vector2(0f, -300f); rt.offsetMax = Vector2.zero;
        }
        var sp = GetOrCreateTitleSprite();
        if (sp != null)
        {
            var img = ta.GetComponent<Image>();
            img.sprite = sp; img.type = Image.Type.Simple;
            img.preserveAspect = false; img.raycastTarget = false;
        }

        // 타이틀 텍스트
        var px = MakeTMP(ta, "Pixel", "PIXEL", 112f, FontStyles.Bold);
        px.color = Color.white; px.alignment = TextAlignmentOptions.Center; px.raycastTarget = false;
        { var rt = px.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(0f, -12f); rt.sizeDelta = new Vector2(340f, 92f); }

        var gn = MakeTMP(ta, "General", "GENERAL", 68f, FontStyles.Bold);
        gn.color = new Color(0.35f, 0.65f, 1f); gn.alignment = TextAlignmentOptions.Center; gn.raycastTarget = false;
        { var rt = gn.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(0f, 12f); rt.sizeDelta = new Vector2(340f, 60f); }

        var sub = MakeTMP(ta, "SubTitle", "픽셀 제너럴", UIScale.FontSm, FontStyles.Normal);
        sub.color = new Color(0.40f, 0.52f, 0.80f); sub.alignment = TextAlignmentOptions.Center; sub.raycastTarget = false;
        { var rt = sub.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 14f); rt.sizeDelta = new Vector2(340f, 34f); }

        // 구분선
        var tDiv = MakeImg("TitleDiv", side, DivC);
        { var rt = tDiv.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.offsetMin = new Vector2(20f, -303f); rt.offsetMax = new Vector2(-20f, -301f); }
        tDiv.GetComponent<Image>().raycastTarget = false;

        // 버튼 영역
        var btnArea = new GameObject("BtnArea", typeof(RectTransform));
        btnArea.transform.SetParent(side.transform, false);
        { var rt = btnArea.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 1f); rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0f, -308f); }
        var vlg = btnArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
        vlg.spacing = 20f; vlg.padding = new RectOffset(24, 24, 28, 28);

        var relicBtn = BuildIconBtn(btnArea, "RelicBtn", "유물", RelicC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_relic.png");
        var rle = relicBtn.AddComponent<LayoutElement>();
        rle.preferredWidth = 310f; rle.preferredHeight = 190f;

        var dogsanBtn = BuildLockedBtn(btnArea, "DogsanBtn", "도감");
        var dle = dogsanBtn.AddComponent<LayoutElement>();
        dle.preferredWidth = 290f; dle.preferredHeight = 160f;

        var etcBtn = BuildLockedBtn(btnArea, "EtcBtn", "기타");
        var ele = etcBtn.AddComponent<LayoutElement>();
        ele.preferredWidth = 290f; ele.preferredHeight = 160f;

        return relicBtn;
    }

    // ── 화살표 (소형) ─────────────────────────────────────────

    // ◀ ▶ 글리프는 폰트에 없다 (□ 로 렌더됨) → 꺾쇠 도형으로 그린다.
    // dirDeg: 0 = 오른쪽, 180 = 왼쪽
    static GameObject BuildArrow(GameObject parent, string name, float dirDeg)
    {
        var go = EditorUIBuilder.Go(name, parent);
        EditorUIBuilder.RaisedBtnOn(go, ArrowC, out var body);
        EditorUIBuilder.Chevron(body, "Mark", UIScale.FontMd, dirDeg, Color.white);
        return go;
    }

    // ── 아이콘 버튼 ───────────────────────────────────────────

    static GameObject BuildIconBtn(GameObject parent, string name, string label, Color bg, string iconPath)
    {
        // UI 규칙: 누를 수 있는 버튼은 음각 처리 (내용은 body 아래로)
        var go = EditorUIBuilder.Go(name, parent);
        EditorUIBuilder.RaisedBtnOn(go, bg, out var body);
        var iGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iGo.transform.SetParent(body.transform, false);
        { var rt = iGo.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0.1f, 0.24f); rt.anchorMax = new Vector2(0.9f, 0.92f); rt.offsetMin = rt.offsetMax = Vector2.zero; }
        var ii = iGo.GetComponent<Image>(); ii.preserveAspect = true; ii.raycastTarget = false;
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath); if (sp != null) ii.sprite = sp;
        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(body.transform, false);
        { var rt = lGo.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0.26f); rt.offsetMin = rt.offsetMax = Vector2.zero; }
        var lt = lGo.GetComponent<TextMeshProUGUI>(); lt.text = label; lt.fontSize = UIScale.FontSm; lt.alignment = TextAlignmentOptions.Center; lt.color = Color.white; lt.raycastTarget = false;
        return go;
    }

    static GameObject BuildLockedBtn(GameObject parent, string name, string label)
    {
        var go = MakeImg(name, parent, LockedC);
        // 🔒 이모지는 폰트에 없다 (□ 로 렌더됨) → 자물쇠 도형으로 그린다
        EditorUIBuilder.PadLock(go, "Lock", 54f, new Color(0.28f, 0.30f, 0.46f));
        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        { var rt = lGo.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0.30f); rt.offsetMin = rt.offsetMax = Vector2.zero; }
        var tmp = lGo.GetComponent<TextMeshProUGUI>(); tmp.text = label; tmp.fontSize = UIScale.FontSm; tmp.alignment = TextAlignmentOptions.Center; tmp.color = new Color(0.28f, 0.30f, 0.46f); tmp.raycastTarget = false;
        return go;
    }

    // ── 특성 아이콘 UI ────────────────────────────────────────

    static TraitIconUI BuildTraitIconUI(GameObject card)
    {
        const float IconSize = 100f;
        const float TipW    = 400f;
        int         tipLp   = (int)(GradeW + 10f);   // 15

        // Root GO
        var root = new GameObject("TraitIconUI", typeof(RectTransform));
        root.transform.SetParent(card.transform, false);
        var traitUI = root.AddComponent<TraitIconUI>();
        {
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(tipLp, -700f);
            rt.sizeDelta = new Vector2(IconSize, IconSize);
        }

        // Icon button (fills root)
        var iconBtnGo = new GameObject("IconBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        iconBtnGo.transform.SetParent(root.transform, false);
        Stretch(iconBtnGo.GetComponent<RectTransform>());
        iconBtnGo.GetComponent<Image>().color = SlotC;

        // Icon image (inner inset)
        var iconImgGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
        iconImgGo.transform.SetParent(iconBtnGo.transform, false);
        {
            var rt = iconImgGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.12f);
            rt.anchorMax = new Vector2(0.88f, 0.88f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        var iconImg = iconImgGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.color = new Color(0.25f, 0.25f, 0.38f);

        // 상세 툴팁 — 보상 카드·특성 슬롯과 같은 공용 컴포넌트
        var tooltip = InfoTooltipBuilder.Build(root, TipW);

        // Wire TraitIconUI fields
        var so = new SerializedObject(traitUI);
        so.Update();
        SetRef(so, "_iconImage",   iconImg);
        SetRef(so, "_iconBtn",     iconBtnGo.GetComponent<Button>());
        SetRef(so, "_tooltip",     tooltip);
        so.ApplyModifiedProperties();

        return traitUI;
    }

    // ── 스탯 행 (앵커 기반 — LayoutGroup 미사용) ──────────────

    static GameObject BuildStatRow(GameObject parent, string rowName,
        (string id, string label) c0, (string id, string label) c1)
    {
        var row = new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        // 좌측 셀 (0 ~ 0.5), 우측 셀 (0.5 ~ 1.0)
        BuildStatCell(row, c0.id, c0.label, 0f);
        BuildStatCell(row, c1.id, c1.label, 0.5f);
        return row;
    }

    static void BuildStatCell(GameObject parent, string id, string label, float xStart)
    {
        var cell = new GameObject(id, typeof(RectTransform));
        cell.transform.SetParent(parent.transform, false);
        var rt = cell.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xStart,        0f);
        rt.anchorMax = new Vector2(xStart + 0.5f, 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // 레이블 (셀 좌측 40%)
        var lGo = new GameObject("LabelText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(cell.transform, false);
        var lrt = lGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f,    0f); lrt.anchorMax = new Vector2(0.42f, 1f);
        lrt.offsetMin = new Vector2(6f, 2f);    lrt.offsetMax = new Vector2(0f, -2f);
        var lt = lGo.GetComponent<TextMeshProUGUI>();
        lt.text = label; lt.fontSize = UIScale.FontSm; lt.color = Muted;
        lt.alignment = TextAlignmentOptions.Right; lt.raycastTarget = false;

        // 값 (셀 우측 58%)
        var vGo = new GameObject("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        vGo.transform.SetParent(cell.transform, false);
        var vrt = vGo.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0.42f, 0f); vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(4f, 2f);    vrt.offsetMax = new Vector2(-4f, -2f);
        var vt = vGo.GetComponent<TextMeshProUGUI>();
        vt.text = "0"; vt.fontSize = UIScale.FontMd; vt.fontStyle = FontStyles.Bold;
        vt.color = Color.white; vt.alignment = TextAlignmentOptions.Left; vt.raycastTarget = false;
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
