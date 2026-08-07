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

    // ── BattleResultPopup 팔레트 ──────────────────────────────
    //  구조색은 EditorUIBuilder.Pop 공용 (Reincarnation 등과 공유).
    //  이 팝업에만 쓰는 강조색(승패·힌트·확인)만 여기서 정의한다.
    static readonly Color BrPanelBg      = EditorUIBuilder.Pop.PanelBg;
    static readonly Color BrPanelBorder  = EditorUIBuilder.Pop.PanelBorder;
    static readonly Color BrHeaderBg     = EditorUIBuilder.Pop.HeaderBg;
    static readonly Color BrTitleShadow  = EditorUIBuilder.Pop.TitleShadow;
    static readonly Color BrSlotBg       = EditorUIBuilder.Pop.SlotBg;
    static readonly Color BrTabActive    = EditorUIBuilder.Pop.TabActive;
    static readonly Color BrTabInactive  = EditorUIBuilder.Pop.TabInactive;

    static readonly Color BrVictory      = new Color(1.00f,  0.82f,  0.22f,  1f);
    static readonly Color BrDefeat       = new Color(0.62f,  0.64f,  0.72f,  1f);
    static readonly Color BrHint         = new Color(0.82f,  0.74f,  0.44f,  1f);
    static readonly Color BrConfirm      = new Color(0.16f,  0.58f,  0.36f,  1f);

    [MenuItem(ProjectKMenu.Popup + "▶ 팝업 전체", priority = ProjectKMenu.PrefabPrio + 20)]
    public static void CreateAll()
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
    // 새 레이아웃 (H=96):
    //   LEFT  : [Portrait] [이름  ▲UP!] / [Lv.x  Exp N]
    //   RIGHT : [StatBar ████░░] [Total] / [Legend...] [DPS]

    static void CreateExpRowPrefab()
    {
        const float rowH      = 100f;
        const float portSz    = 72f;
        const float nameX     = 84f;    // portrait 우측 끝 + 6 gap
        const float leftEnd   = 310f;   // 좌측 섹션 끝 (portrait + name + levelup)
        const float rightSize = 140f;   // 우측 총량 영역 (TotalText 120 + 20 margin)
        float stretchX = (leftEnd - rightSize) / 2f;  // = 85

        var root    = new GameObject("ExpRow", typeof(RectTransform));
        var rowComp = root.AddComponent<ExpRowUI>();
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, rowH);
        root.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.19f, 0.90f);

        // ── 초상화 ──────────────────────────────────────────────
        var portBg = new GameObject("PortraitBg", typeof(RectTransform), typeof(Image));
        portBg.transform.SetParent(root.transform, false);
        portBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.35f, 1f);
        var portRt = portBg.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0f, 0.5f); portRt.anchorMax = new Vector2(0f, 0.5f);
        portRt.pivot = new Vector2(0f, 0.5f);
        portRt.anchoredPosition = new Vector2(6f, 0f);
        portRt.sizeDelta = new Vector2(portSz, portSz);

        var portImg = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portImg.transform.SetParent(portBg.transform, false);
        portImg.GetComponent<Image>().color = Color.white;
        StretchRT(portImg);

        var bridgeGo = new GameObject("PortraitBridge", typeof(RectTransform));
        bridgeGo.transform.SetParent(portBg.transform, false);
        bridgeGo.SetActive(false);
        bridgeGo.AddComponent<UnitAppearanceBridge>();
        bridgeGo.AddComponent<Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts.CharacterBuilder>();

        // ── 이름 (좌상) ──────────────────────────────────────────
        var nameText = AddTMP(root, "NameText", "영웅 이름", UIScale.FontMd, FontStyles.Bold);
        nameText.alignment        = TextAlignmentOptions.MidlineLeft;
        nameText.overflowMode     = TextOverflowModes.Ellipsis;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin      = 16f;
        nameText.fontSizeMax      = 38f;
        var nameRt = nameText.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 0.5f); nameRt.anchorMax = new Vector2(0f, 0.5f);
        nameRt.pivot = new Vector2(0f, 0.5f);
        nameRt.anchoredPosition = new Vector2(nameX, 22f);
        nameRt.sizeDelta = new Vector2(210f, 46f);

        // ── 레벨업 뱃지 (LevelText 동일 위치, 기본 비활성) ──────
        // 레벨업 시 LevelText를 대체하여 먼저 표시, 1초 후 LevelText로 교체
        // ▲ 는 폰트에 없다 (□ 로 렌더됨)
        var lvupText = AddTMP(root, "LevelUpText", "UP!", UIScale.FontSm, FontStyles.Bold);
        lvupText.alignment          = TextAlignmentOptions.MidlineLeft;
        lvupText.color              = new Color(1.00f, 0.85f, 0.15f);
        lvupText.enableWordWrapping = false;
        var lvupRt = lvupText.rectTransform;
        lvupRt.anchorMin = new Vector2(0f, 0.5f); lvupRt.anchorMax = new Vector2(0f, 0.5f);
        lvupRt.pivot = new Vector2(0f, 0.5f);
        lvupRt.anchoredPosition = new Vector2(nameX, -22f);  // LevelText와 동일 위치
        lvupRt.sizeDelta = new Vector2(90f, 36f);
        lvupText.gameObject.SetActive(false);

        // ── 레벨 (좌하) ──────────────────────────────────────────
        var levelText = AddTMP(root, "LevelText", "Lv.1", UIScale.FontSm, FontStyles.Normal);
        levelText.alignment          = TextAlignmentOptions.MidlineLeft;
        levelText.color              = new Color(0.55f, 0.57f, 0.72f);
        levelText.textWrappingMode   = TextWrappingModes.NoWrap;
        levelText.overflowMode       = TextOverflowModes.Overflow;
        var levelRt = levelText.rectTransform;
        levelRt.anchorMin = new Vector2(0f, 0.5f); levelRt.anchorMax = new Vector2(0f, 0.5f);
        levelRt.pivot = new Vector2(0f, 0.5f);
        levelRt.anchoredPosition = new Vector2(nameX, -22f);
        levelRt.sizeDelta = new Vector2(90f, 36f);

        // ── Exp (레벨 오른쪽) ─────────────────────────────────────
        var expText = AddTMP(root, "ExpText", "Exp 0", UIScale.FontSm, FontStyles.Normal);
        expText.alignment = TextAlignmentOptions.MidlineLeft;
        expText.color     = new Color(0.45f, 0.80f, 0.50f);
        var expRt = expText.rectTransform;
        expRt.anchorMin = new Vector2(0f, 0.5f); expRt.anchorMax = new Vector2(0f, 0.5f);
        expRt.pivot = new Vector2(0f, 0.5f);
        expRt.anchoredPosition = new Vector2(nameX + 90f + 4f, -22f);  // 178
        expRt.sizeDelta = new Vector2(150f, 34f);

        // ── StatBar (우측 stretch, H=22) ──────────────────────────
        var barGo = new GameObject("StatBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(root.transform, false);
        barGo.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.28f, 1f);
        var barRt = barGo.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0.5f); barRt.anchorMax = new Vector2(1f, 0.5f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.anchoredPosition = new Vector2(stretchX, 22f);
        barRt.sizeDelta = new Vector2(-(leftEnd + rightSize), 22f);  // -450

        var statBarComp = barGo.AddComponent<StatBarUI>();
        var segs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var seg = new GameObject($"Seg{i}", typeof(RectTransform), typeof(Image));
            seg.transform.SetParent(barGo.transform, false);
            var segRt = seg.GetComponent<RectTransform>();
            segRt.anchorMin = new Vector2(0f, 0f); segRt.anchorMax = new Vector2(0f, 1f);
            segRt.offsetMin = Vector2.zero; segRt.offsetMax = Vector2.zero;
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

        // ── 범례 (StatBar 아래, stretch) ──────────────────────────
        var legendText = AddTMP(root, "LegendText",
            "<color=#4D8CF2>■</color> 장군  <color=#59CC74>■</color> 병사  <color=#F28C33>■</color> 스킬",
            28f, FontStyles.Normal);
        legendText.alignment = TextAlignmentOptions.MidlineLeft;
        legendText.color     = new Color(0.55f, 0.57f, 0.68f);
        var legendRt = legendText.rectTransform;
        legendRt.anchorMin = new Vector2(0f, 0.5f); legendRt.anchorMax = new Vector2(1f, 0.5f);
        legendRt.pivot = new Vector2(0.5f, 0.5f);
        legendRt.anchoredPosition = new Vector2(stretchX, -22f);
        legendRt.sizeDelta = new Vector2(-(leftEnd + rightSize), 26f);

        // ── TotalText (우측 상단, 우측정렬) ──────────────────────
        var totalText = AddTMP(root, "TotalText", "", UIScale.FontSm, FontStyles.Bold);
        totalText.alignment          = TextAlignmentOptions.MidlineRight;
        totalText.enableWordWrapping = false;
        totalText.color              = new Color(0.78f, 0.82f, 1.00f, 1f);
        var totalRt = totalText.rectTransform;
        totalRt.anchorMin = new Vector2(1f, 0.5f); totalRt.anchorMax = new Vector2(1f, 0.5f);
        totalRt.pivot = new Vector2(1f, 0.5f);
        totalRt.anchoredPosition = new Vector2(-10f, 22f);
        totalRt.sizeDelta = new Vector2(120f, 34f);

        // ── DPS 텍스트 (우측 하단, 딜탭만 표시) ──────────────────
        var dpsText = AddTMP(root, "DPSText", "", UIScale.FontSm - 2f, FontStyles.Normal);
        dpsText.alignment          = TextAlignmentOptions.MidlineRight;
        dpsText.enableWordWrapping = false;
        dpsText.color              = new Color(0.55f, 0.65f, 0.85f);
        var dpsRt = dpsText.rectTransform;
        dpsRt.anchorMin = new Vector2(1f, 0.5f); dpsRt.anchorMax = new Vector2(1f, 0.5f);
        dpsRt.pivot = new Vector2(1f, 0.5f);
        dpsRt.anchoredPosition = new Vector2(-10f, -22f);
        dpsRt.sizeDelta = new Vector2(120f, 30f);
        dpsText.gameObject.SetActive(false);

        // ── 필드 연결 ─────────────────────────────────────────────
        var so = new SerializedObject(rowComp);
        SetObj(so, "_portraitBg",     portBg.GetComponent<Image>());
        SetObj(so, "_portraitImage",  portImg.GetComponent<Image>());
        SetObj(so, "_portraitBridge", bridgeGo.GetComponent<UnitAppearanceBridge>());
        SetObj(so, "_nameText",       nameText);
        SetObj(so, "_levelText",      levelText);
        SetObj(so, "_expText",        expText);
        SetObj(so, "_legendText",     legendText);
        SetObj(so, "_dpsText",        dpsText);
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

    static void StretchRT(GameObject go) => EditorUIBuilder.Stretch(go);

    // ── BattleResultPopup ─────────────────────────────────────
    //  EventPopup 과 같은 시각 언어로 맞췄다.
    //    · 헤더 밴드 + 다이아 태그 + 그림자 깔린 타이틀
    //    · 강조선 → 섹션 라벨(좌우 라인) → 내용 카드
    //    · 탭·확인 버튼은 음각 입체 버튼 (UI 규칙 1)
    //
    //  레이아웃 (W=1000, H=1000 / 위에서 아래로)
    //    Header        Y=  0  H=136   승패 배지 + 타이틀
    //    AccentLine    Y=136  H=  3   승패 색 (런타임 교체)
    //    RewardLabel   Y=158  H= 40   "획득 보상"
    //    RewardBox     Y=204  H=176   카드 가로 스크롤
    //    HintText      Y=384  H= 34
    //    StatLabel     Y=424  H= 40   "전투 기록"
    //    TabBar        Y=470  H= 92   딜 / 탱 / 힐
    //    ExpBox        Y=574  → 확인 버튼 위까지 (242px, 세로 스크롤)
    //    ConfirmButton 하단 28

    [MenuItem(ProjectKMenu.Popup + "BattleResult", priority = ProjectKMenu.PrefabPrio + 31)]
    public static void CreateBattleResultPopup()
    {
        // ExpRow 프리팹이 없으면 먼저 생성
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/ExpRow.prefab") == null)
            CreateExpRowPrefab();

        const float PW      = 1000f;
        const float PH      = UIScale.PopupMaxH;
        const float SidePad = 40f;
        const float ContentW = PW - SidePad * 2f;

        const float HeaderH   = 136f;
        const float AccentH   = 3f;
        const float RewardY   = 158f;
        const float RewardBoxY = 204f;
        const float RewardBoxH = 176f;
        const float HintY     = 384f;
        const float StatY     = 424f;
        const float TabY      = 470f;
        const float TabH      = 92f;
        const float ExpY      = 574f;

        var root  = CreateRoot<BattleResultPopup>("BattleResultPopup", PW, PH);
        var popup = root.GetComponent<BattleResultPopup>();

        // 테두리는 패널의 "앞 형제" 로 둔다 (UI 규칙 3 — 자식으로 두면 위에 덮인다)
        var border = MakeGo("Border", root);
        border.AddComponent<Image>().color = BrPanelBorder;
        StretchWith(border, -3f);

        var panel = MakeGo("Panel", root);
        panel.AddComponent<Image>().color = BrPanelBg;
        StretchWith(panel, 0f);

        // ── 헤더 밴드 ────────────────────────────────────────
        var header = MakeGo("Header", panel);
        var headerImg = header.AddComponent<Image>();
        headerImg.color = BrHeaderBg;
        AnchorTopBand(header, 0f, HeaderH);

        // 승패를 상징하는 다이아 태그 (색은 런타임 교체)
        var badge = EditorUIBuilder.Diamond(header, "Badge", 30f, BrVictory);
        {
            var rt = badge.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 38f);
        }
        var badgeImg = badge.GetComponent<Image>();

        // 타이틀 — 그림자 사본을 먼저 깔고 그 위에 본문
        var titleShadow = AddTMP(header, "TitleShadow", "승  리", UIScale.FontXl, FontStyles.Bold);
        titleShadow.color         = BrTitleShadow;
        titleShadow.alignment     = TextAlignmentOptions.Center;
        titleShadow.raycastTarget = false;
        SetRect(titleShadow.rectTransform, new Vector2(3f, -18f), new Vector2(ContentW, 84f));

        var resultText = AddTMP(header, "ResultText", "승  리", UIScale.FontXl, FontStyles.Bold);
        resultText.color         = BrVictory;
        resultText.alignment     = TextAlignmentOptions.Center;
        resultText.raycastTarget = false;
        SetRect(resultText.rectTransform, new Vector2(0f, -15f), new Vector2(ContentW, 84f));

        // 헤더 아래 강조선 (승패 색)
        var accentLine = MakeGo("AccentLine", panel);
        var accentImg  = accentLine.AddComponent<Image>();
        accentImg.color         = BrVictory;
        accentImg.raycastTarget = false;
        AnchorTopBand(accentLine, HeaderH, AccentH);

        // ── "획득 보상" 섹션 ─────────────────────────────────
        BuildSectionLabel(panel, "획득 보상", RewardY, ContentW);

        // 보상 카드 가로 스크롤 (카드가 많아도 잘리지 않게)
        var rewardBox = MakeGo("RewardBox", panel);
        var rewardBoxImg = rewardBox.AddComponent<Image>();
        rewardBoxImg.color = BrSlotBg;
        AnchorTopBand(rewardBox, RewardBoxY, RewardBoxH, SidePad);

        var rewardScroll = rewardBox.AddComponent<ScrollRect>();

        var rewardVp = MakeGo("Viewport", rewardBox);
        var rewardVpImg = rewardVp.AddComponent<Image>();
        rewardVpImg.color = new Color(0f, 0f, 0f, 0.01f);
        rewardVp.AddComponent<Mask>().showMaskGraphic = false;
        var rewardVpRt = rewardVp.GetComponent<RectTransform>();
        rewardVpRt.anchorMin = Vector2.zero; rewardVpRt.anchorMax = Vector2.one;
        rewardVpRt.offsetMin = new Vector2(8f, 8f); rewardVpRt.offsetMax = new Vector2(-8f, -8f);

        var rewardArea = MakeGo("RewardArea", rewardVp);
        var rewardRt = rewardArea.GetComponent<RectTransform>();
        rewardRt.anchorMin        = new Vector2(0f, 0f);
        rewardRt.anchorMax        = new Vector2(0f, 1f);
        rewardRt.pivot            = new Vector2(0f, 0.5f);
        rewardRt.anchoredPosition = Vector2.zero;
        rewardRt.sizeDelta        = Vector2.zero;

        var rewardHlg = rewardArea.AddComponent<HorizontalLayoutGroup>();
        rewardHlg.spacing                = 14f;
        rewardHlg.childAlignment         = TextAnchor.MiddleLeft;
        rewardHlg.childControlWidth      = false;
        rewardHlg.childControlHeight     = false;
        rewardHlg.childForceExpandWidth  = false;
        rewardHlg.childForceExpandHeight = false;
        rewardHlg.padding                = new RectOffset(8, 8, 0, 0);
        rewardArea.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        rewardScroll.horizontal   = true;
        rewardScroll.vertical     = false;
        rewardScroll.movementType = ScrollRect.MovementType.Elastic;
        rewardScroll.viewport     = rewardVpRt;
        rewardScroll.content      = rewardRt;

        // 박스 개봉 안내
        var hintText = AddTMP(panel, "HintText", "카드를 눌러 개봉하세요", UIScale.FontSm, FontStyles.Normal);
        hintText.color         = BrHint;
        hintText.alignment     = TextAlignmentOptions.Center;
        hintText.raycastTarget = false;
        AnchorTopBand(hintText.gameObject, HintY, 34f, SidePad);
        hintText.gameObject.SetActive(false);

        // ── "전투 기록" 섹션 ─────────────────────────────────
        BuildSectionLabel(panel, "전투 기록", StatY, ContentW);

        var tabBar = MakeGo("TabBar", panel);
        AnchorTopBand(tabBar, TabY, TabH, SidePad);

        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing                = 10f;
        tabHlg.childAlignment         = TextAnchor.MiddleCenter;
        tabHlg.childControlWidth      = true;
        tabHlg.childControlHeight     = true;
        tabHlg.childForceExpandWidth  = true;
        tabHlg.childForceExpandHeight = true;

        string[] tabLabels = { "딜", "탱", "힐" };
        var tabButtons   = new Button[3];
        var tabButtonBgs = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            // UI 규칙 1 — 누를 수 있는 요소는 음각 입체 버튼으로.
            //
            // 세 탭 모두 같은 면 색으로 만든다. RaisedBtnOn 이 TopEdge/BottomEdge
            // 색을 면 색에서 구워 넣기 때문에, 런타임에 Body 색만 바꾸면
            // 모서리 색이 따라오지 않아 어긋난다.
            // → 활성 표시는 하단 강조바(ActiveBar)로 한다.
            var tabGo = MakeGo($"TabBtn{i}", tabBar);
            var btn   = EditorUIBuilder.RaisedBtnOn(tabGo, BrTabInactive, out var body);

            var label = AddTMP(body, "Label", tabLabels[i], UIScale.FontMd, FontStyles.Bold);
            label.alignment     = TextAlignmentOptions.Center;
            label.color         = Color.white;
            label.raycastTarget = false;
            EditorUIBuilder.Stretch(label.gameObject);

            var bar    = MakeGo("ActiveBar", body);
            var barImg = bar.AddComponent<Image>();
            barImg.color         = i == 0 ? BrTabActive : Color.clear;
            barImg.raycastTarget = false;
            {
                var rt = bar.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.08f, 0f);
                rt.anchorMax        = new Vector2(0.92f, 0f);
                rt.pivot            = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 5f);   // BottomEdge(4px) 바로 위
                rt.sizeDelta        = new Vector2(0f, 5f);
            }

            tabButtons[i]   = btn;
            tabButtonBgs[i] = barImg;   // 활성 표시용 그래픽
        }

        // ── EXP 행 목록 ──────────────────────────────────────
        // 배치 슬롯이 5칸이라 행이 최대 5개(100px * 5 + spacing = 524px)까지 온다.
        // 남는 세로는 242px 뿐이므로 3행부터 팝업 밖으로 삐져나온다 → 세로 스크롤로 감싼다.
        var expBox = MakeGo("ExpBox", panel);
        {
            var rt = expBox.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(SidePad, UIScale.BtnMd + 52f);
            rt.offsetMax = new Vector2(-SidePad, -ExpY);
        }
        var expScroll = expBox.AddComponent<ScrollRect>();

        var expVp = MakeGo("Viewport", expBox);
        expVp.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);  // Mask 는 Graphic 이 있어야 동작
        expVp.AddComponent<Mask>().showMaskGraphic = false;
        var expVpRt = expVp.GetComponent<RectTransform>();
        expVpRt.anchorMin = Vector2.zero; expVpRt.anchorMax = Vector2.one;
        expVpRt.offsetMin = Vector2.zero; expVpRt.offsetMax = Vector2.zero;

        // Content — 위에서 아래로 자라고 높이는 ContentSizeFitter 가 잰다
        var expArea = MakeGo("ExpArea", expVp);
        {
            var rt = expArea.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
        }
        expArea.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        expScroll.horizontal   = false;
        expScroll.vertical     = true;
        expScroll.movementType = ScrollRect.MovementType.Elastic;
        expScroll.viewport     = expVpRt;
        expScroll.content      = expArea.GetComponent<RectTransform>();

        var expVlg = expArea.AddComponent<VerticalLayoutGroup>();
        expVlg.spacing                = 6f;
        expVlg.padding                = new RectOffset(0, 0, 0, 0);
        expVlg.childControlWidth      = true;
        expVlg.childControlHeight     = false;
        expVlg.childForceExpandWidth  = true;
        expVlg.childForceExpandHeight = false;
        expVlg.childAlignment         = TextAnchor.UpperCenter;

        // 확인 버튼 (하단 고정)
        var confirmBtn = EditorUIBuilder
            .RaisedTextBtn(panel, "ConfirmButton", "확  인", UIScale.FontLg, BrConfirm)
            .gameObject;
        {
            var rt = confirmBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 28f);
            rt.sizeDelta        = new Vector2(520f, UIScale.BtnMd);
        }

        // ── 직렬화 필드 연결 ─────────────────────────────────
        var rewardCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/RewardCard.prefab");
        var expRowPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/ExpRow.prefab");

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",       (int)PopupType.BattleResult);
        SetObj (so, "_resultText",      resultText);
        SetObj (so, "_titleShadowText", titleShadow);
        SetObj (so, "_headerBadge",     badgeImg);
        SetObj (so, "_accentLine",      accentImg);
        SetObj (so, "_rewardArea",      rewardArea.transform);
        SetObj (so, "_hintText",        hintText);
        SetObj (so, "_expArea",         expArea.transform);
        SetObj (so, "_confirmButton",   confirmBtn.GetComponent<Button>());

        if (rewardCardPrefab != null)
            SetObj(so, "_rewardCardPrefab", rewardCardPrefab.GetComponent<RewardCardUI>());
        else
            Debug.LogWarning("[PopupPrefabCreator] RewardCard.prefab 이 없습니다 — " +
                             "Tools > Project K > 프리팹 생성 > 인게임 > RewardCard 를 먼저 실행하세요.");

        if (expRowPrefab != null)
            SetObj(so, "_expRowPrefab", expRowPrefab.GetComponent<ExpRowUI>());

        SetObjArray(so, "_tabButtons",   System.Array.ConvertAll(tabButtons,   b => (Object)b));
        SetObjArray(so, "_tabButtonBgs", System.Array.ConvertAll(tabButtonBgs, b => (Object)b));

        so.ApplyModifiedProperties();

        Save(root, "BattleResultPopup");
    }

    // 섹션 라벨 — 가운데 글자 + 좌우 라인 (EventPopup 의 "선 택" 과 같은 형태)
    static void BuildSectionLabel(GameObject panel, string text, float yFromTop, float contentW)
        => EditorUIBuilder.SectionLabel(panel, text, yFromTop, contentW, (1000f - contentW) * 0.5f);

    // 상단에서 yFromTop 만큼 내려온 높이 h 의 가로 밴드
    static void AnchorTopBand(GameObject go, float yFromTop, float h, float sidePad = 0f)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(sidePad,  -(yFromTop + h));
        rt.offsetMax = new Vector2(-sidePad, -yFromTop);
    }

    static void StretchWith(GameObject go, float outset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(outset, outset);
        rt.offsetMax = new Vector2(-outset, -outset);
    }

    // ── PausePopup ────────────────────────────────────────────

    //  로비 팝업(EventPopup·HeroDetail)과 같은 톤으로 맞췄다:
    //    전체화면 오버레이 → 테두리(패널 앞 형제) → 패널 → ◆ 태그 헤더 + 강조선 → 입체 버튼.
    //  ⚠ 이 팝업만 인게임 캔버스(1080×1920 세로) 위에 뜬다 — 가로 여유가 1080 뿐이다.
    [MenuItem(ProjectKMenu.Popup + "Pause", priority = ProjectKMenu.PrefabPrio + 32)]
    public static void CreatePausePopup()
    {
        const float PW      = 840f;
        const float HeaderH = 136f;
        const float SidePad =  48f;
        const float BtnGap  =  24f;
        float btnH   = UIScale.BtnFor(UIScale.FontMd) + 20f;   // 92 — 인게임은 손가락으로 누른다
        float popupH = HeaderH + 3f + 40f + btnH * 2 + BtnGap + 48f;

        // 루트는 전체화면 오버레이 — 뒤 전투 화면을 어둡게 깐다
        var root = new GameObject("PausePopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        StretchRT(root);
        var popup = root.AddComponent<PausePopup>();

        // 테두리는 Panel 의 **앞 형제** — 자식으로 두면 팝업 전체를 덮는다 (UI 규칙 3)
        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(root.transform, false);
        border.GetComponent<Image>().color = new Color(0.26f, 0.44f, 0.72f, 1f);
        SetRect(border.GetComponent<RectTransform>(), Vector2.zero, new Vector2(PW + 6f, popupH + 6f));

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        panel.GetComponent<Image>().color = new Color(0.07f, 0.075f, 0.13f, 1f);
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(PW, popupH));

        // ── 헤더 ──────────────────────────────────────────────
        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        header.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.18f, 1f);
        EditorUIBuilder.AnchorTop(header.GetComponent<RectTransform>(), 0f, HeaderH);

        // ★ 는 폰트에 없다 (□ 로 렌더됨) → 마름모 도형 (UI 규칙 2)
        var tagRoot = EditorUIBuilder.Go("PauseTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(300f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, new Color(0.62f, 0.82f, 1.00f));
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = AddTMP(tagRoot, "Label", "전 투", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = new Color(0.62f, 0.82f, 1.00f);
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        // 타이틀 — 그림자 사본을 먼저 깔아 어떤 배경에서도 읽히게 한다
        MakePauseTitle(header, "TitleShadow", new Color(0.02f, 0.03f, 0.06f, 0.85f), 3f);
        MakePauseTitle(header, "TitleText",   new Color(1.00f, 0.94f, 0.78f, 1f),    0f);

        var accent = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(panel.transform, false);
        accent.GetComponent<Image>().color = new Color(0.40f, 0.72f, 1.00f, 1f);
        EditorUIBuilder.AnchorTop(accent.GetComponent<RectTransform>(), HeaderH, 3f);

        // ── 버튼 2개 (계속하기 / 즉시 환생하기) ───────────────
        //  "즉시 환생하기" 는 되돌릴 수 없다 — 붉은 계열로 구분한다.
        var resumeBtn = MakePauseChoice(panel, "ResumeButton", "계 속 하 기",
                                        "전투로 돌아간다",
                                        new Color(0.13f, 0.52f, 0.38f, 1f),
                                        HeaderH + 43f, btnH, SidePad);

        var reincBtn  = MakePauseChoice(panel, "ReincarnateButton", "즉시 환생하기",
                                        "이번 런을 포기하고 환생한다",
                                        new Color(0.50f, 0.16f, 0.18f, 1f),
                                        HeaderH + 43f + btnH + BtnGap, btnH, SidePad);

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",          (int)PopupType.Pause);
        SetObj (so, "_resumeButton",       resumeBtn);
        SetObj (so, "_reincarnateButton",  reincBtn);
        so.ApplyModifiedProperties();

        Save(root, "PausePopup");
    }

    static void MakePauseTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = AddTMP(header, name, "일시 정지", UIScale.FontLg, FontStyles.Bold);
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f + dy, -52f - dy);
        rt.sizeDelta        = new Vector2(700f, UIScale.RowLg);
    }

    /// <summary>[라벨(FontMd)] 위 / [설명(FontSm)] 아래 2줄짜리 입체 선택 버튼.</summary>
    static Button MakePauseChoice(GameObject panel, string name, string label, string hint,
                                  Color face, float yFromTop, float h, float sidePad)
    {
        // UI 규칙 1 — 누를 수 있는 버튼은 음각. 내용은 반드시 body 아래.
        var btn = EditorUIBuilder.RaisedBtn(panel, name, face, out var body);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(sidePad,  0f);
        rt.offsetMax = new Vector2(-sidePad, 0f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta        = new Vector2(-sidePad * 2f, h);

        var lbl = AddTMP(body, "Label", label, UIScale.FontMd, FontStyles.Bold);
        lbl.color            = Color.white;
        lbl.alignment        = TextAlignmentOptions.Center;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        var lRt = lbl.rectTransform;
        lRt.anchorMin = new Vector2(0f, 0.5f); lRt.anchorMax = new Vector2(1f, 1f);
        lRt.offsetMin = new Vector2(16f, 0f);  lRt.offsetMax = new Vector2(-16f, -6f);

        var hintTmp = AddTMP(body, "Hint", hint, UIScale.FontSm, FontStyles.Normal);
        hintTmp.color            = new Color(1f, 1f, 1f, 0.72f);
        hintTmp.alignment        = TextAlignmentOptions.Center;
        hintTmp.raycastTarget    = false;
        hintTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var hRt = hintTmp.rectTransform;
        hRt.anchorMin = new Vector2(0f, 0f);   hRt.anchorMax = new Vector2(1f, 0.5f);
        hRt.offsetMin = new Vector2(16f, 8f);  hRt.offsetMax = new Vector2(-16f, 0f);

        return btn;
    }

    // ── LoadingPopup ──────────────────────────────────────────

    [MenuItem(ProjectKMenu.Popup + "Loading", priority = ProjectKMenu.PrefabPrio + 33)]
    public static void CreateLoadingPopup()
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
        => EditorUIBuilder.BgPanel(parent, color);

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static GameObject AddButton(GameObject parent, string objName, string label, Color bgColor, float fontSize)
        => EditorUIBuilder.Btn(parent, objName, label, bgColor, fontSize, boldLabel: true);

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        => EditorUIBuilder.Center(rt, pos, size);

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "PopupPrefabCreator");

    static GameObject MakeGo(string name, GameObject parent)
        => EditorUIBuilder.Go(name, parent);

    static void SetObjArray(SerializedObject so, string field, Object[] objs)
        => EditorUIBuilder.SetObjArray(so, field, objs, "PopupPrefabCreator");

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "PopupPrefabCreator");

    // ── AbilitySelectPopup ────────────────────────────────────
    // 카드 최대 5장을 스크롤 없이 한 번에 표시.
    // 팝업 너비는 5장 기준으로 고정; 3장 이하일 때는 HLG MiddleCenter 가 중앙 정렬.

    [MenuItem(ProjectKMenu.Popup + "AbilitySelect", priority = ProjectKMenu.PrefabPrio + 34)]
    static void CreateAbilitySelectPopup()
    {
        const int   maxCards     = 5;
        const float cardW        = 272f;
        const float cardH        = 580f;
        const float cardSpacing  = 16f;
        const float cardPadding  = 8f;   // HLG left+right 합계
        const float sideMargin   = 40f;
        float refreshRowH = UIScale.BtnFor(UIScale.FontSm);   // 64 → 58
        const float popupH       = 840f;

        // 팝업 너비 = 5장 전체 콘텐츠 + 좌우 여백
        float contentW = maxCards * cardW + (maxCards - 1) * cardSpacing + cardPadding;
        float popupW   = contentW + sideMargin;   // 1472f

        var root  = CreateRoot<AbilitySelectPopup>("AbilitySelectPopup", popupW, popupH);
        var popup = root.GetComponent<AbilitySelectPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        // 제목
        var titleTmp = AddTMP(root, "TitleText", "어빌리티 선택", UIScale.FontLg, FontStyles.Bold);
        SetRect(titleTmp.rectTransform, new Vector2(0f, popupH / 2f - 60f), new Vector2(popupW - 80f, 70f));

        // ── 새로고침 행 ───────────────────────────────────────
        var refreshRow = new GameObject("RefreshRow", typeof(RectTransform));
        refreshRow.transform.SetParent(root.transform, false);
        SetRect(refreshRow.GetComponent<RectTransform>(),
            new Vector2(0f, popupH / 2f - 130f), new Vector2(contentW, refreshRowH));

        var refreshBtn = AddButton(refreshRow, "RefreshBtn", "새로고침",
            new Color(0.15f, 0.40f, 0.55f), UIScale.FontSm);
        SetRect(refreshBtn.GetComponent<RectTransform>(), new Vector2(-200f, 0f), new Vector2(220f, 56f));

        var refreshCountTmp = AddTMP(refreshRow, "RefreshCountText",
            "새로고침  0회 남음", UIScale.FontSm, FontStyles.Normal);
        refreshCountTmp.color = new Color(0.60f, 0.85f, 1.0f);
        refreshCountTmp.alignment = TextAlignmentOptions.Left;
        SetRect(refreshCountTmp.rectTransform, new Vector2(130f, 0f), new Vector2(380f, refreshRowH));

        // ── 카드 영역 (스크롤 없음, 최대 5장 한 번에 표시) ────
        var cardArea = new GameObject("CardArea", typeof(RectTransform));
        cardArea.transform.SetParent(root.transform, false);
        var cardAreaRt = cardArea.GetComponent<RectTransform>();
        cardAreaRt.anchorMin        = new Vector2(0.5f, 0.5f);
        cardAreaRt.anchorMax        = new Vector2(0.5f, 0.5f);
        cardAreaRt.pivot            = new Vector2(0.5f, 0.5f);
        cardAreaRt.anchoredPosition = new Vector2(0f, -60f);
        cardAreaRt.sizeDelta        = new Vector2(contentW, cardH);

        var hlg = cardArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = cardSpacing;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding               = new RectOffset(4, 4, 0, 0);

        // 카드 5장 생성
        var cards = new AbilityCardUI[maxCards];
        for (int i = 0; i < maxCards; i++)
        {
            var card   = BuildAbilityCard($"Card{i}", cardW, cardH);
            card.transform.SetParent(cardArea.transform, false);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(cardW, cardH);
            cards[i] = card.GetComponent<AbilityCardUI>();
        }

        // ── SerializedObject 연결 ─────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",       (int)PopupType.AbilitySelect);
        SetObj (so, "_titleTmp",        titleTmp);
        SetObj (so, "_refreshBtn",      refreshBtn.GetComponent<Button>());
        SetObj (so, "_refreshCountTmp", refreshCountTmp);

        var cardsProp = so.FindProperty("_cards");
        cardsProp.arraySize = maxCards;
        for (int i = 0; i < maxCards; i++)
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

        // 스탯 설명 텍스트 (높이 160 — Special/Mastery 긴 설명 수용)
        var descTmp = AddCardTMP(card, "DescTmp", "+0%", UIScale.FontSm, FontStyles.Normal);
        SetRect(descTmp.rectTransform, new Vector2(0f, iconTopY - iconSz / 2f - 228f), new Vector2(w - 24f, 160f));
        descTmp.color = new Color(0.85f, 0.85f, 0.90f);
        descTmp.alignment = TextAlignmentOptions.Top;
        descTmp.textWrappingMode = TextWrappingModes.Normal;

        // 레벨 텍스트 (descTmp 아래, MaxLevel>1 어빌리티에만 표시)
        var levelTmp = AddCardTMP(card, "LevelTmp", "Lv 1/3", UIScale.FontSm - 2f, FontStyles.Normal);
        SetRect(levelTmp.rectTransform, new Vector2(0f, iconTopY - iconSz / 2f - 334f), new Vector2(w - 24f, 36f));
        levelTmp.color = new Color(0.55f, 0.80f, 1.00f);
        levelTmp.gameObject.SetActive(false);

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
        SetObj(cardSo, "_levelTmp",  levelTmp);
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

    [MenuItem(ProjectKMenu.Popup + "AbilityList", priority = ProjectKMenu.PrefabPrio + 35)]
    static void CreateAbilityListPopup()
    {
        const float popupW   = 980f;
        float popupH = UIScale.PopupMaxH;   // 1060 → 1000 (위아래 40 여백 확보)
        float headerH = UIScale.BtnFor(UIScale.FontLg);   // 100 → 96
        const float leftW    = 360f;
        const float detailH  = 560f;   // body=960 → detailH 560 + totalBox 400

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

        var closeBtn = AddButton(header, "CloseBtn", "", new Color(0.55f, 0.18f, 0.18f, 1f), UIScale.FontMd);
        EditorUIBuilder.XMark(closeBtn, "Mark", UIScale.FontMd, Color.white);
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

        // InfoStatScrollView (ScrollRect — 긴 설명 텍스트 스크롤 지원)
        float statY = divY + 8f;
        var infoStatScroll = new GameObject("InfoStatScrollView", typeof(RectTransform), typeof(ScrollRect));
        infoStatScroll.transform.SetParent(detailBox.transform, false);
        var issRt = infoStatScroll.GetComponent<RectTransform>();
        issRt.anchorMin = Vector2.zero; issRt.anchorMax = Vector2.one;
        issRt.offsetMin = new Vector2(10f, 0f); issRt.offsetMax = new Vector2(-10f, -statY);

        var infoStatVp = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        infoStatVp.transform.SetParent(infoStatScroll.transform, false);
        infoStatVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        infoStatVp.GetComponent<Mask>().showMaskGraphic = false;
        var isvpRt = infoStatVp.GetComponent<RectTransform>();
        isvpRt.anchorMin = Vector2.zero; isvpRt.anchorMax = Vector2.one;
        isvpRt.offsetMin = isvpRt.offsetMax = Vector2.zero;

        var infoStatContent = new GameObject("InfoStatContent",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        infoStatContent.transform.SetParent(infoStatVp.transform, false);
        var iscRt = infoStatContent.GetComponent<RectTransform>();
        iscRt.anchorMin = new Vector2(0f, 1f); iscRt.anchorMax = new Vector2(1f, 1f);
        iscRt.pivot = new Vector2(0.5f, 1f); iscRt.offsetMin = iscRt.offsetMax = Vector2.zero;
        var iscVlg = infoStatContent.GetComponent<VerticalLayoutGroup>();
        iscVlg.padding = new RectOffset(4, 4, 4, 4); iscVlg.spacing = 6f;
        iscVlg.childControlWidth = true; iscVlg.childForceExpandWidth = true;
        iscVlg.childControlHeight = true; iscVlg.childForceExpandHeight = false;
        infoStatContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var isScrollRect = infoStatScroll.GetComponent<ScrollRect>();
        isScrollRect.content = iscRt; isScrollRect.viewport = isvpRt;
        isScrollRect.horizontal = false; isScrollRect.vertical = true;
        isScrollRect.movementType = ScrollRect.MovementType.Elastic;

        // InfoStatTemplate (팝업 루트에 배치, 비활성)
        var infoStatRow = new GameObject("InfoStatTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoStatRow.transform.SetParent(root.transform, false);
        var infoRowTmp = infoStatRow.GetComponent<TextMeshProUGUI>();
        infoRowTmp.text = "<color=#AAAAAA>스탯</color>  +0%";
        infoRowTmp.fontSize = UIScale.FontSm; infoRowTmp.alignment = TextAlignmentOptions.Left;
        infoRowTmp.color = Color.white; infoRowTmp.textWrappingMode = TextWrappingModes.Normal;
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
