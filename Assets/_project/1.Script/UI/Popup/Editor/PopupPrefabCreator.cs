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

    /// <summary>
    /// 팝업 프리팹 전체 생성.
    ///
    /// ⚠ "이 파일이 만드는 것" 이 아니라 "팝업 메뉴에 있는 것 전부" 다
    ///   예전엔 이 파일이 직접 만드는 6개(ExpRow·BattleResult·Pause·Loading·
    ///   Ability×2)만 돌렸다. 그런데 메뉴에는 HeroDetail·EquipCompare·Disassemble
    ///   같은 팝업이 바로 아래 나란히 붙어 있어서, "▶ 팝업 전체" 를 누르면
    ///   그것들도 만들어진 줄 알게 된다. 실제로는 손도 안 대므로 Creator 를
    ///   고쳐도 프리팹은 옛날 것 그대로 남는다 — i 버튼이 안 생긴 원인이었다.
    ///   메뉴 이름이 "전체" 면 전체를 만들어야 한다.
    ///
    /// ⚠ Creator 를 추가하면 여기에도 한 줄 넣을 것
    ///   ProjectKBatch 는 이 메서드만 부른다. 여기 빠지면 전체 생성에서도 빠진다.
    /// </summary>
    [MenuItem(ProjectKMenu.Popup + "▶ 팝업 전체", priority = ProjectKMenu.PrefabPrio + 20)]
    public static void CreateAll()
    {
        // 이 파일이 직접 만드는 것 — BattleResult 가 ExpRow/RewardCard 를 참조하므로 순서 유지
        CreateExpRowPrefab();
        CreateBattleResultPopup();
        CreatePausePopup();
        CreateLoadingPopup();
        AbilitySelectPopupCreator.Create();
        AbilityListPopupCreator.Create();

        // 나머지 팝업 — 각자 정본 Creator 가 따로 있다
        EquipComparePopupCreator.Create();
        DisassemblePopupCreator.Create();
        HeroDetailPopupCreator.Create();
        RunShopPopupCreator.Create();
        ReincarnationPopupCreator.Create();
        MercenaryPopupCreator.Create();
        EventPopupCreator.Create();
        CodexPopupCreator.Run();
        RelicPopupCreator.Create();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopupPrefabCreator] ✓ 팝업 프리팹 전체 생성 완료 (15종)");
    }

    // ── ExpRow 프리팹 ─────────────────────────────────────────
    //
    //  레이아웃 (H=100) — 세 개의 열이 겹치지 않게 x 를 못 박는다
    //    [초상화] │ [이름          ] │ [■■■░░ 막대   ] │ [   22.9K]
    //     6..78   │ [Lv.11  Exp 1040] │ [■장군 ■병사 ■스킬] │ [DPS 513]
    //             90 ............ 400  416 ......... W-186   우측 170
    //
    //  ⚠ 열 경계는 반드시 아래 상수로만 잡는다
    //    예전엔 ExpText(178~328)가 범례·막대 시작선(310)을 18px 파고들어
    //    "Exp 1040" 위에 "■ 장군 ■ 병사" 가 겹쳐 찍혔다. 레벨업 프레임에는
    //    UP! 까지 끼어 세 글자가 한 자리에서 뭉갰다.
    //    가운데 열은 stretch 라 폭이 변해도, 좌우 열은 **고정 픽셀**이다 —
    //    팝업을 넓혀도 이 겹침은 안 풀린다. 여기서 벌려야 한다.
    static void CreateExpRowPrefab()
    {
        const float rowH      = 100f;
        const float portSz    = 72f;
        const float nameX     = 90f;    // portrait 우측 끝(78) + 12 gap
        const float lvW       = 118f;   // "Lv.11" / "UP!" 이 들어가는 폭
        const float expX      = nameX + lvW + 10f;   // 218
        const float expW      = 172f;                // "Exp 1040" — 네 자리까지
        const float leftEnd   = 400f;   // 막대·범례가 시작하는 x (Exp 끝 390 보다 뒤)
        const float rightSize = 186f;   // 우측 총량 영역 ("DPS 4,154" + 여백)
        float stretchX = (leftEnd - rightSize) / 2f;

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
        nameRt.anchoredPosition = new Vector2(nameX, 24f);
        // 아랫줄(Lv + Exp) 과 같은 폭을 쓴다 — 이름이 길어도 막대까지 침범하지 않는다
        nameRt.sizeDelta = new Vector2(leftEnd - nameX - 12f, 46f);

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
        lvupRt.anchoredPosition = new Vector2(nameX, -22f);  // LevelText와 동일 위치(교대 표시)
        lvupRt.sizeDelta = new Vector2(lvW, 36f);
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
        levelRt.sizeDelta = new Vector2(lvW, 36f);

        // ── Exp (레벨 오른쪽) ─────────────────────────────────────
        var expText = AddTMP(root, "ExpText", "Exp 0", UIScale.FontSm, FontStyles.Normal);
        expText.alignment = TextAlignmentOptions.MidlineLeft;
        expText.color     = new Color(0.45f, 0.80f, 0.50f);
        var expRt = expText.rectTransform;
        expRt.anchorMin = new Vector2(0f, 0.5f); expRt.anchorMax = new Vector2(0f, 0.5f);
        expRt.pivot = new Vector2(0f, 0.5f);
        expRt.anchoredPosition = new Vector2(expX, -22f);
        expRt.sizeDelta = new Vector2(expW, 34f);
        // ⚠ 넘치면 줄이고, 절대 옆 칸을 침범하지 않는다
        expText.textWrappingMode = TextWrappingModes.NoWrap;
        expText.overflowMode     = TextOverflowModes.Ellipsis;

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
        totalRt.anchoredPosition = new Vector2(-14f, 24f);
        totalRt.sizeDelta = new Vector2(rightSize - 24f, 34f);

        // ── DPS 텍스트 (우측 하단, 딜탭만 표시) ──────────────────
        var dpsText = AddTMP(root, "DPSText", "", UIScale.FontSm - 2f, FontStyles.Normal);
        dpsText.alignment          = TextAlignmentOptions.MidlineRight;
        dpsText.enableWordWrapping = false;
        dpsText.color              = new Color(0.55f, 0.65f, 0.85f);
        var dpsRt = dpsText.rectTransform;
        dpsRt.anchorMin = new Vector2(1f, 0.5f); dpsRt.anchorMax = new Vector2(1f, 0.5f);
        dpsRt.pivot = new Vector2(1f, 0.5f);
        dpsRt.anchoredPosition = new Vector2(-14f, -22f);
        dpsRt.sizeDelta = new Vector2(rightSize - 24f, 30f);
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
    //  레이아웃 (W=1240, H=1000 / 위에서 아래로)
    //    Header        Y=  0  H=136   승패 배지 + 타이틀
    //    AccentLine    Y=136  H=  3   승패 색 (런타임 교체)
    //    RewardLabel   Y=150  H= 40   "획득 보상"
    //    RewardBox     Y=194  H=168   카드 가로 스크롤
    //    HintText      Y=366  H= 43
    //    StatLabel     Y=412  H= 43   "전투 기록"
    //    TabBar        Y=456  H= 84   딜 / 탱 / 힐
    //    ExpBox        Y=546  → 확인 버튼 위까지 (314px = 3행)
    //    ConfirmButton 360×100, 하단 22
    //
    //  ⚠ 폭은 ExpRow 가 정한다
    //    행 좌우 열(초상화+이름 400 / 총량 186)이 고정이라 가운데 막대가
    //    쓸 폭이 곧 남는 값이다. 1240 이면 막대가 574 — 세그먼트 3개가 읽힌다.
    //    더 늘리면 막대만 길어지고 양쪽 열은 그대로라 화면이 비어 보인다.

    [MenuItem(ProjectKMenu.Popup + "BattleResult", priority = ProjectKMenu.PrefabPrio + 31)]
    public static void CreateBattleResultPopup()
    {
        // ExpRow 프리팹이 없으면 먼저 생성
        if (AssetDatabase.LoadAssetAtPath<GameObject>($"{SavePath}/ExpRow.prefab") == null)
            CreateExpRowPrefab();

        // ⚠ 팝업 캔버스는 1920×1080 (CanvasPopup) 이다 — 인게임 캔버스가 아니다
        //   1000 폭은 화면의 절반뿐이라 좌우가 휑했다. 좌우 140 여백만 남기고 채운다.
        //   세로는 PopupMaxH 를 넘기지 않는다 (UI 규칙 6) — 대신 아래 버튼을 줄여
        //   전투 기록 목록이 쓸 높이를 돌려준다.
        const float PW      = 1240f;
        const float PH      = UIScale.PopupMaxH;
        const float SidePad = 40f;
        const float ContentW = PW - SidePad * 2f;

        // 확인 버튼 — 예전엔 520×132 로 하단을 통째로 먹어 목록이 3행에서 잘렸다.
        // 누르는 데 문제 없는 최소 크기로 줄이고 그 차이를 목록에 준다.
        const float ConfirmW      = 360f;
        const float ConfirmH      = UIScale.BtnSm;
        const float ConfirmBottom = 22f;

        // 위 섹션을 조금씩 죄어 전투 기록이 3행을 온전히 담게 한다.
        // ⚠ 반쯤 잘린 행이 남으면 "삐져나왔다" 로 보인다 — 행 높이(100)+간격(6)의
        //   배수로 떨어지게 ExpY 를 맞춘다: 1000 - 542 - 140 = 318 ≥ 3행(312).
        const float HeaderH   = 136f;
        const float AccentH   = 3f;
        const float RewardY   = 150f;
        const float RewardBoxY = 194f;
        const float RewardBoxH = 168f;
        const float HintY     = 366f;
        const float HintH     = 43f;    // UIScale.RowSm — FontSm 한 줄 (UI 규칙 5)
        const float StatY     = 412f;
        const float TabY      = 456f;
        const float TabH      = 84f;
        const float ExpY      = 546f;

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
        BuildSectionLabel(panel, "획득 보상", RewardY, ContentW, SidePad);

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
        // ⚠ 가로 중앙 기준으로 매단다 (예전엔 왼쪽 고정)
        //   박스가 넓어지면서 카드 5장이 왼쪽 구석에 몰려 붙었다.
        //   중앙 앵커 + ContentSizeFitter 조합이면 카드가 적을 때는 가운데,
        //   많아서 넘칠 때는 그대로 가로 스크롤이 된다.
        rewardRt.anchorMin        = new Vector2(0.5f, 0f);
        rewardRt.anchorMax        = new Vector2(0.5f, 1f);
        rewardRt.pivot            = new Vector2(0.5f, 0.5f);
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
        AnchorTopBand(hintText.gameObject, HintY, HintH, SidePad);
        hintText.gameObject.SetActive(false);

        // ── "전투 기록" 섹션 ─────────────────────────────────
        BuildSectionLabel(panel, "전투 기록", StatY, ContentW, SidePad);

        var tabBar = MakeGo("TabBar", panel);
        // 탭 3개를 패널 폭 전체로 늘리면 한 칸이 500 을 넘어 글자만 덩그러니 남는다.
        // 가운데 900 폭으로 묶는다.
        AnchorTopBand(tabBar, TabY, TabH, (PW - 900f) * 0.5f);

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
            rt.offsetMin = new Vector2(SidePad, ConfirmBottom + ConfirmH + 18f);
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
            .RaisedTextBtn(panel, "ConfirmButton", "확  인", UIScale.FontMd, BrConfirm)
            .gameObject;
        {
            var rt = confirmBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0f);
            rt.anchorMax        = new Vector2(0.5f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, ConfirmBottom);
            rt.sizeDelta        = new Vector2(ConfirmW, ConfirmH);
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
    /// <summary>
    /// 섹션 라벨 (가운데 글자 + 좌우 라인).
    ///
    /// ⚠ 여백은 반드시 sidePad 를 그대로 받는다
    ///   예전엔 `(1000 - contentW) * 0.5` 로 패널 폭 1000 을 상수로 박아 뒀다.
    ///   팝업을 넓히자 이 값이 **음수**가 되어 구분선이 패널 밖으로 뻗어 나갔다.
    ///   폭을 바꿀 때마다 조용히 깨지는 종류의 계산이라 인자로 받는다.
    /// </summary>
    static void BuildSectionLabel(GameObject panel, string text, float yFromTop,
                                  float contentW, float sidePad)
        => EditorUIBuilder.SectionLabel(panel, text, yFromTop, contentW, sidePad);

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
        const float Outset  =   6f;   // 테두리가 패널 밖으로 드러나는 두께 (PausePopup 과 동일)

        float btnH = UIScale.BtnFor(UIScale.FontMd) + 20f;   // 92 — 인게임은 손가락으로 누른다
        // 사운드 토글은 설명 줄이 없다 — 두 줄짜리 선택지보다 낮게 잡아 목록을 압축한다.
        float togH = UIScale.BtnFor(UIScale.FontMd);

        // 행 순서: 계속하기 → 효과음 → 배경음악 → 즉시 환생하기
        //   되돌릴 수 없는 항목을 맨 아래에 둔다. 마침 로비에서 접는 행도 이것이라
        //   접었을 때 목록 중간에 구멍이 나지 않는다.
        float yResume = HeaderH + 43f;
        float ySfx    = yResume + btnH + BtnGap;
        float yBgm    = ySfx    + togH + BtnGap;
        float yReinc  = yBgm    + togH + BtnGap;

        float surrenderRowH = BtnGap + btnH;                  // 로비에서 접는 높이
        float popupH        = yReinc + btnH + 48f;

        // 루트는 전체화면 오버레이 — 뒤 전투 화면을 어둡게 깐다
        var root = new GameObject("PausePopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        StretchRT(root);
        var popup = root.AddComponent<PausePopup>();

        // 테두리는 Panel 의 **앞 형제** — 자식으로 두면 팝업 전체를 덮는다 (UI 규칙 3)
        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(root.transform, false);
        border.GetComponent<Image>().color = new Color(0.26f, 0.44f, 0.72f, 1f);
        SetRect(border.GetComponent<RectTransform>(), Vector2.zero, new Vector2(PW + Outset, popupH + Outset));

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

        // ── 선택지 ────────────────────────────────────────────
        //  "즉시 환생하기" 는 되돌릴 수 없다 — 붉은 계열로 구분한다.
        var resumeBtn = MakePauseChoice(panel, "ResumeButton", "계 속 하 기",
                                        "전투로 돌아간다",
                                        new Color(0.13f, 0.52f, 0.38f, 1f),
                                        yResume, btnH, SidePad);

        var sfxBtn = MakeSoundToggle(panel, "SfxButton", "효 과 음",
                                     ySfx, togH, SidePad,
                                     out var sfxPill, out var sfxState);

        var bgmBtn = MakeSoundToggle(panel, "BgmButton", "배 경 음 악",
                                     yBgm, togH, SidePad,
                                     out var bgmPill, out var bgmState);

        var reincBtn  = MakePauseChoice(panel, "ReincarnateButton", "즉시 환생하기",
                                        "이번 런을 포기하고 환생한다",
                                        new Color(0.50f, 0.16f, 0.18f, 1f),
                                        yReinc, btnH, SidePad);

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",          (int)PopupType.Pause);
        SetObj (so, "_resumeButton",       resumeBtn);
        SetObj (so, "_reincarnateButton",  reincBtn);
        SetObj (so, "_sfxButton",          sfxBtn);
        SetObj (so, "_sfxPill",            sfxPill);
        SetObj (so, "_sfxState",           sfxState);
        SetObj (so, "_bgmButton",          bgmBtn);
        SetObj (so, "_bgmPill",            bgmPill);
        SetObj (so, "_bgmState",           bgmState);
        SetObj (so, "_panelRect",          panel.GetComponent<RectTransform>());
        SetObj (so, "_borderRect",         border.GetComponent<RectTransform>());
        so.FindProperty("_panelFullH").floatValue    = popupH;
        so.FindProperty("_surrenderRowH").floatValue = surrenderRowH;
        so.ApplyModifiedProperties();

        Save(root, "PausePopup");
    }

    /// <summary>
    /// [라벨] ────── [상태 알약] 한 줄짜리 사운드 토글 버튼.
    ///
    /// ⚠ 상태를 버튼 본체 색으로 나타내지 않는다
    ///   Body 는 Button.targetGraphic 이라 눌림 색이 그 색에 곱해진다 (UI 규칙 1).
    ///   런타임에 Body 를 물들이면 TintFor 로 역산해 둔 눌림 색이 어긋나므로,
    ///   상태는 그 위에 얹은 알약(Image + TMP)이 맡는다.
    /// </summary>
    static Button MakeSoundToggle(GameObject panel, string name, string label,
                                  float yFromTop, float h, float sidePad,
                                  out Image pill, out TextMeshProUGUI state)
    {
        var btn = EditorUIBuilder.RaisedBtn(panel, name, new Color(0.19f, 0.24f, 0.38f, 1f), out var body);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta        = new Vector2(-sidePad * 2f, h);

        const float PillW = 170f;

        var lbl = AddTMP(body, "Label", label, UIScale.FontMd, FontStyles.Bold);
        lbl.color            = Color.white;
        lbl.alignment        = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        var lRt = lbl.rectTransform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(28f, 0f);
        lRt.offsetMax = new Vector2(-(PillW + 40f), 0f);

        // 알약 — 우측. 높이는 UIScale.RowSm (글자가 잘리지 않는 최소 한 줄, UI 규칙 5)
        pill = EditorUIBuilder.Img(body, "StatePill", new Color(0.16f, 0.50f, 0.34f, 1f));
        var pRt = pill.rectTransform;
        pRt.anchorMin = pRt.anchorMax = new Vector2(1f, 0.5f);
        pRt.pivot     = new Vector2(1f, 0.5f);
        pRt.anchoredPosition = new Vector2(-28f, 0f);
        pRt.sizeDelta        = new Vector2(PillW, UIScale.RowSm);

        state = AddTMP(pill.gameObject, "State", "켜짐", UIScale.FontSm, FontStyles.Bold);
        state.color            = Color.white;
        state.alignment        = TextAlignmentOptions.Center;
        state.raycastTarget    = false;
        state.textWrappingMode = TextWrappingModes.NoWrap;
        StretchRT(state.gameObject);

        return btn;
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

    // ── AbilitySelectPopup 은 여기서 만들지 않는다 ─────────────
    //  AbilitySelectPopupCreator.cs 가 정본이다 (전체화면 + 입체 카드).
    //  ⚠ 한 프리팹은 한 Creator 만 — 두 곳에서 만들면 결과가 갈린다.
    // ── AbilityListPopup 은 여기서 만들지 않는다 ──────────────
    //  AbilityListPopupCreator.cs 가 정본이다 (전체화면 2단 레이아웃).
    //  ⚠ 한 프리팹은 한 Creator 만 — 두 곳에서 만들면 메뉴를 어느 쪽으로
    //    돌렸느냐에 따라 결과가 달라진다.


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
