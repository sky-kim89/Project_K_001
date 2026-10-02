using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MainPanelCreator.cs
//  Tools > Project K > 프리팹 생성 > 로비 > MainPanel
//
//   ┌배너┐  ┌───────── 장수 카드 620 ─────────┐  ┌──── 난이도 560 ────┐
//   │PIXEL│  │ [직업] 기사            ★ 영웅 4 │  │      난이도      i │
//   │GENER│  │ ‹        [ 초상화 270 ]       › │  │ ‹ [아이콘 쉬움] ›  │
//   │[유물]│  │ [ 이름 NEW         ][새로고침]  │  │ ▬▬▬▬▬            │
//   │[도감]│  │ [♥ 체력 3,923 │ ⛊ 방어 16.6%]  │  │ 요약 설명 2줄      │
//   │[설정]│  │ [⚔ 공격   116 │ 👥 병사    8]  │  │[투구 적용 제한 사항]│
//   │      │  │ [특][액][패][패][패]           │  │[상자 보상 배율 ×1.0]│
//   │  ▽   │  │      [책 자세히 보기 ›]        │  │[칼   게임 시작    ] │
//   └──────┘  └────────────────────────────────┘  └────────────────────┘
//
//  ■ 스킨 — UI/PixelTheme (Codex 제작). 프레임·버튼·배너는 전부 그 스프라이트다.
//    버튼 그림에 음각이 이미 들어 있어 RaisedBtn 을 덧대지 않는다 (UI 규칙 1 충족).
//
//  ■ 정보가 먼저다 (2026-10-02 2차 정리)
//    1차는 시안을 그대로 옮기느라 장식(패널 위 탭·왕관·금 구분선·마름모·등급 칩 박스)이
//    많았고, 9-slice 모서리를 원본(44~52px) 그대로 써서 테두리가 내용보다 두꺼웠다.
//    글자·아이콘이 테두리 위에 겹쳐 그려졌다.
//    → 장식은 뺐고, 테두리는 *Scale 로 줄였고, 칸 안쪽 여백은 '테두리 두께 + 6' 으로 잡는다.
//    ⚠ 칸 안에 무엇을 넣을 땐 그 칸 스프라이트의 *Pad 상수부터 본다.
//
//  ■ 절대 되돌리면 안 되는 것 (예전 화면에서 밟은 것)
//    · 모든 TMP 는 NoWrap. Ellipsis/Truncate 는 칸보다 한 줄이 크면
//      그 줄을 통째로 버린다 ("PIXE / L", "1,98 / 8", "새로고 / 침").
//    · 칸 폭이 정해진 글자는 Fit() (= NoWrap + 넘치면 축소). Overflow 만 걸면
//      다른 언어에서 칸 밖으로 샌다 — 스페인어에서 "Multiplicador de recompensa" 가
//      배율 숫자를, "Iniciar juego" 가 버튼 테두리를 덮었다 (2026-10-02).
//      한국어는 칸 안에 들어가므로 줄지 않는다. 칸끼리 겹치지 않게 경계부터 잡을 것.
//    · CardW 620 — 440 에서 스탯 값이 두 줄로 접혔다. 용병 팝업도 이 카드를 쓴다.
//    · 스탯은 [라벨 좌][값 우] + StatColors — 인게임·상점 용병 카드와 같은 규칙.
//    · 화살표는 초상화 바로 옆 (카드 안). 밖에 두면 무엇을 넘기는지 안 읽혔다.
//    · _portraitBg 를 물리지 않는다 — 직업색이 배경에 곱해져 검붉게 죽는다.
// ============================================================

public static class MainPanelCreator
{
    const string SavePath        = "Assets/_project/2.Prefabs/UI/Lobby/MainPanel.prefab";
    const string TitleImgPath    = "Assets/_project/3.Textures/UI/Splash/title_pixel_general_splash.png";
    const string BackdropImgPath = "Assets/_project/3.Textures/UI/Splash/background_pixel_general.png";
    const string StatIconDir     = "Assets/_project/3.Textures/Icons/Stats/";
    const string LobbyBtnDir     = "Assets/_project/3.Textures/Icons/LobbyBtns/";
    const string DefaultJobIcon  = "Assets/_project/3.Textures/Icons/Classes/knight_icon.png";

    // ── PixelTheme 스프라이트 (README.md 의 파일 목록) ──────────
    const string FramePx   = "ui_panel_frame_9slice";
    const string BlueBtnPx = "ui_button_blue_9slice";
    const string GoldBtnPx = "ui_button_gold_9slice";
    const string TealPx    = "ui_button_teal_9slice";
    const string SquarePx  = "ui_button_square_9slice";
    const string InfoPx    = "ui_info_panel_9slice";   // 정보 칸 (명판·스탯·제한·보상)
    const string HeadRowPx = "ui_header_row";          // 좌우 금 꺾쇠 머리띠 (가로 9-slice)
    const string BannerPx  = "ui_side_banner";
    const string RodPx     = "ui_banner_rod";

    const string IcoGear    = "ui_icon_gear";
    const string IcoRefresh = "ui_icon_refresh";
    const string IcoBook    = "ui_icon_book";
    const string IcoStar    = "ui_icon_star";
    const string IcoSwords  = "ui_icon_swords";
    const string IcoHelmet  = "ui_icon_helmet";
    const string IcoChest   = "ui_icon_chest";

    // ── 테두리 축척 · 안쪽 여백 ────────────────────────────────
    //  9-slice 모서리를 몇 배로 그릴지 (EditorUIBuilder.PixelImage 의 borderScale).
    //  *Pad = 그 칸 안에 내용을 놓기 시작하는 거리 = 보이는 테두리 + 6.
    const float FrameScale = 0.7f;   const float Pad     = 24f;   // 큰 패널 (금테 ≈ 11)
    const float InfoScale  = 0.6f;   const float InfoPad = 18f;   // 정보 칸 (파란 이중선 ≈ 12)
    const float HeadScale  = 0.7f;   const float HeadPad = 26f;   // 머리띠 (금 꺾쇠 세로획 ≈ 18)
    const float BtnScale   = 0.55f;  const float BtnPad  = 22f;   // 파란 버튼 (≈ 16)
    const float SqScale    = 0.5f;                                // 사각 화살표
    const float TealScale  = 0.6f;
    const float GoldScale  = 0.7f;

    // ── 3열 배치: [배너] [카드] [난이도] — 묶음 전체를 화면 가운데에 ──
    const float SideW      = 378f;   // ui_side_banner 원본 폭 — 도트가 늘어나지 않게 그대로
    const float ColGap1    =  32f;
    public const float CardW = 620f;
    const float ColGap2    =  24f;
    const float OperationW = 600f;
    const float ContentW   = SideW + ColGap1 + CardW + ColGap2 + OperationW;   // 1654
    const float FitMargin  = 120f;   // 배너 걸이·꼬리가 칸 밖으로 나가는 몫

    // ── 카드 내부 Y (card 상단에서 몇 px) ──────────────────────
    //  ⚠ 글자·아이콘 크기를 줄여서 칸을 맞추지 않는다 (2026-10-02)
    //    2차 정리 때 테두리를 얇게 하면서 내용까지 줄였더니 "작아서 안 보인다" 가 됐다.
    //    내용은 원래 크기(FontLg 머리줄·96 아이콘)를 지키고, 모자라면 칸 높이를 늘린다.
    const float HeadY    = 22f;
    const float HeadH    = 72f;                         // ui_header_row 원본 높이 = FontLg 한 줄
    const float JobIconSz = 52f;
    // ⚠ PortPad 는 '초상화 위 끝' 이다 — MercenaryPopupCreator 가 화살표 높이를 여기서 잰다
    public const float PortPad = HeadY + HeadH + 10f;   // 104
    public const float PortH   = 280f;
    const float DotsY    = PortPad + PortH - 22f;       // 초상화 아래쪽 안
    const float DotsH    = 14f;
    const float NameY    = PortPad + PortH + 12f;       // 396
    const float NameH    = 72f;
    const float RefreshW = 220f;   // 테두리 22 + 아이콘 36 + 8 + "새로고침"(≈130) + 22
    const float StatY    = NameY + NameH + 12f;         // 480
    const float StatH    = 136f;                        // 2행 × 56 + 위아래 테두리
    const float StIconSz = 48f;   // 라벨 글자가 없으니 아이콘이 곧 라벨 — 크게
    const float IconY    = StatY + StatH + 14f;         // 630
    const float SkillSz  = 96f;
    const float DetY     = IconY + SkillSz + 16f;       // 742
    static readonly float DetH = UIScale.BtnFor(UIScale.FontMd);   // 72
    // ⚠ static readonly 는 적힌 순서대로 초기화된다 — DetH 보다 위로 올리면 CardH 가 틀린다
    public static readonly float CardH = DetY + DetH + 22f;        // 836

    // ── 화살표 ──
    public const float ArrSize = 80f;
    // ArrGap — 카드 '바깥' 간격. MercenaryPopupCreator 가 그대로 쓴다.
    public const float ArrGap  = 18f;
    const float ArrInset = Pad + 8f;   // MainPanel 은 카드 안 초상화 위에 얹는다

    // ── 사이드 배너 ──
    const float BannerRise = 8f;
    const float BannerTail = 30f;
    const float TitleY     = 36f;
    const float TitleH     = 200f;
    const float SideBtnW   = 290f;   // 배너 안쪽 폭(≈318) 안
    const float SideBtnH   = 100f;
    const float SideBtnGap = 16f;

    // ── 색상 ──────────────────────────────────────────────────
    static readonly Color GoldC     = new Color(0.84f, 0.61f, 0.19f, 1.00f);
    static readonly Color IvoryC    = new Color(0.96f, 0.91f, 0.76f, 1.00f);
    static readonly Color NavyInk   = new Color(0.025f, 0.09f, 0.22f, 1.00f);  // 금 버튼 위 글자
    static readonly Color SubBtnC   = new Color(0.66f, 0.70f, 0.86f, 1.00f);   // 파란 버튼을 한 톤 죽인다 (곱)
    // 야영지 배경을 살짝 식힌 정도 — 어둡게 덮으면 장수 실루엣이 배경에 묻힌다
    static readonly Color PortTint  = new Color(0.82f, 0.88f, 1.00f, 1.00f);
    static readonly Color SlotC     = new Color(0.025f, 0.07f, 0.17f, 1.00f);
    static readonly Color TraitFrameC = new Color(0.30f, 0.62f, 0.90f, 1.00f);
    static readonly Color SelectC   = new Color(1.00f, 0.85f, 0.20f, 0.22f);
    static readonly Color Muted     = new Color(0.66f, 0.70f, 0.81f);
    static readonly Color DivC      = new Color(0.25f, 0.39f, 0.67f, 0.62f);

    // =========================================================

    [MenuItem(ProjectKMenu.Lobby + "MainPanel", priority = ProjectKMenu.PrefabPrio + 12)]
    public static void Run()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta =
            new Vector2(UIScale.LobbyCanvasH / 9f * 16f, UIScale.LobbyCanvasH);
        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RevertSceneInstances();
        Debug.Log("[MainPanelCreator] 생성 완료 → " + SavePath);
    }

    /// <summary>
    /// 열린 씬의 MainPanel 인스턴스에 남은 오버라이드를 걷어 낸다.
    ///
    /// ⚠ 다시 구워도 같은 이름의 오브젝트는 옛 fileID 를 물려받는다 (2026-10-02)
    ///   VerticalLayoutGroup 시절 Lobby 씬에 기록된 "앵커 0 · 크기 0" 이 새 RelicBtn·CodexBtn 에
    ///   그대로 다시 꽂혀, 두 버튼이 배너 왼쪽 아래 모서리에 0×0 으로 찌그러졌다.
    ///   이 프리팹은 Creator 산출물이라 씬에서 바꿀 값이 없다 — 통째로 되돌린다.
    ///   (Lobby 씬이 열려 있지 않으면 아무것도 안 한다 — 그땐 씬에서 Overrides > Revert All)
    /// </summary>
    static void RevertSceneInstances()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(SavePath);
        foreach (var ui in Object.FindObjectsByType<MainPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var inst = PrefabUtility.GetNearestPrefabInstanceRoot(ui.gameObject);
            if (inst == null || PrefabUtility.GetCorrespondingObjectFromSource(inst) != asset) continue;
            PrefabUtility.RevertPrefabInstance(inst, InteractionMode.AutomatedAction);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(inst.scene);
            Debug.Log($"[MainPanelCreator] 씬 오버라이드 되돌림 → {inst.scene.name}/{inst.name}");
        }
    }

    public static GameObject Build(GameObject parent)
    {
        // 루트 (투명 — 배경 투과)
        var root = new GameObject("MainPanel", typeof(RectTransform));
        root.transform.SetParent(parent.transform, false);
        var ui = root.AddComponent<MainPanelUI>();
        Stretch(root.GetComponent<RectTransform>());

        // 배경 이미지 (최하위 — 모든 UI 뒤)
        var bgImgComp = MakeImg("BackgroundImage", root, Color.white).GetComponent<Image>();
        Stretch(bgImgComp.rectTransform);
        bgImgComp.sprite         = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropImgPath);
        bgImgComp.type           = Image.Type.Simple;
        bgImgComp.preserveAspect = false;
        bgImgComp.raycastTarget  = false;

        // ── 고정 크기 묶음을 화면 가운데에 ─────────────────────
        //  ⚠ 16:9 보다 넓은 화면은 캔버스 '세로' 가 1080 아래로 내려간다 (match 0.5).
        //    2560×1080 이면 935 — 묶음을 통째로 줄여 아래가 잘리지 않게 한다.
        var fit = EditorUIBuilder.Go("ContentFit", root);
        Stretch(fit.GetComponent<RectTransform>());
        var content = EditorUIBuilder.Go("Content", fit);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.pivot     = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(ContentW, CardH);
        var fitter = fit.AddComponent<ScaleToFitHeight>();
        var fso = new SerializedObject(fitter);
        fso.FindProperty("_content").objectReferenceValue = contentRt;
        fso.FindProperty("_designHeight").floatValue      = CardH + FitMargin;
        fso.ApplyModifiedPropertiesWithoutUndo();

        var (relicBtn, codexBtn, settingsBtn) = BuildSide(content);

        var card = BuildHeroCard(content, withDots: true, withRefresh: true, withTrait: true,
                                 out var cardUI, out var dots, out var refreshBtnGo);
        PlaceColumn(card, SideW + ColGap1, CardW);

        // 화살표 — 카드 **안쪽** 초상화 양옆.
        //  밖으로 내보내면 왼쪽은 배너를 침범하고 오른쪽은 허공에 떠서
        //  무엇을 넘기는 버튼인지 안 읽혔다.
        float arrY = -(PortPad + PortH * 0.5f);
        var arrL = BuildArrow(card, "LeftArrowBtn",  180f);
        var arrR = BuildArrow(card, "RightArrowBtn",   0f);
        PinTopSide(arrL, left: true,  ArrInset, arrY, ArrSize);
        PinTopSide(arrR, left: false, ArrInset, arrY, ArrSize);

        var (opPanel, startBtn) = BuildOperationPanel(content);
        PlaceColumn(opPanel, SideW + ColGap1 + CardW + ColGap2, OperationW);

        // MainPanelUI 연결
        var so = new SerializedObject(ui);
        so.Update();
        SetRef(so, "_backgroundImage", bgImgComp);
        SetRef(so, "_card",        cardUI);
        SetRef(so, "_relicBtn",    relicBtn.GetComponent<Button>());
        SetRef(so, "_codexBtn",    codexBtn.GetComponent<Button>());
        SetRef(so, "_settingsBtn", settingsBtn.GetComponent<Button>());
        SetRef(so, "_startBtn",    startBtn.GetComponent<Button>());
        SetRef(so, "_prevBtn",     arrL.GetComponent<Button>());
        SetRef(so, "_nextBtn",     arrR.GetComponent<Button>());
        SetRef(so, "_refreshBtn",  refreshBtnGo.GetComponent<Button>());
        SetObjArrayLocal(so, "_pageDots", dots);
        so.ApplyModifiedProperties();
        return root;
    }

    /// <summary>Content 안 세로 열 — 왼쪽 x 에서 w 폭, 높이는 Content 전체.</summary>
    static void PlaceColumn(GameObject go, float x, float w)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(x, 0f);
        rt.offsetMax = new Vector2(x + w, 0f);
    }

    // ── 장수 카드 (공유 팩토리) ──────────────────────────────

    /// <summary>
    /// 장수 카드 1장(620×CardH)을 만들어 붙인다 — MainPanel 과 용병 고용 팝업이 공유한다.
    /// 위치는 호출한 쪽이 정한다 (여기서는 크기만 잡는다).
    ///
    /// ⚠ 이 함수가 카드의 정본이다. 다른 Creator 에서 레이아웃을 복사하지 말 것 —
    ///   복사본이 생기면 스탯 표기·스킬 아이콘 규칙이 화면마다 갈라진다.
    /// </summary>
    /// <param name="withDots">페이지 도트 4개 (초상화 아래쪽 안). false 면 dots 는 빈 배열.</param>
    /// <param name="withRefresh">이름 옆 새로고침 버튼 (MainPanel 전용).</param>
    /// <param name="withTrait">
    /// 특성 칸. 특성은 **게임 시작 시 직업별로 하나** 받는 것이라 MainPanel 에만 있다.
    /// 용병 고용은 특성을 주지 않으므로 false — 칸 자체를 빼고 스킬만 가운데 정렬한다.
    /// </param>
    public static GameObject BuildHeroCard(GameObject parent,
                                           bool withDots, bool withRefresh, bool withTrait,
                                           out GeneralCandidateCardUI cardUI,
                                           out Image[] dots,
                                           out GameObject refreshBtnGo)
    {
        var card = EditorUIBuilder.PixelImage(parent, "CardContainer", FramePx, FrameScale).gameObject;
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

        // ── 머리 줄: [직업 아이콘] 직업명 ········· ★ 등급 ──
        //  등급은 박스 없이 글자만 — 칩 테두리가 글자를 덮었다.
        var head = EditorUIBuilder.PixelImage(card, "HeaderRow", HeadRowPx, HeadScale);
        TAF(head.rectTransform, HeadY, HeadH, Pad, Pad);

        var jobIcon = EditorUIBuilder.Img(head.gameObject, "JobIcon", Color.white);
        jobIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultJobIcon);   // 런타임이 직업별로 바꾼다
        jobIcon.preserveAspect = true;
        jobIcon.raycastTarget  = false;
        PinLeft(jobIcon.gameObject, HeadPad, JobIconSz);

        var jobChipTmp = MakeTMP(head.gameObject, "JobChipText", "기사", UIScale.FontLg, FontStyles.Bold);
        jobChipTmp.color = IvoryC; jobChipTmp.alignment = TextAlignmentOptions.MidlineLeft;
        jobChipTmp.raycastTarget = false;
        //  ⚠ 직업명 칸과 등급 칸을 고정 폭으로 나눈다 (2026-10-02)
        //    예전엔 등급을 HorizontalLayoutGroup 으로 오른쪽에 쌓았는데, 레이아웃은 글자의
        //    '원래 폭' 을 그대로 잡아 축소가 안 먹는다 → "Caballero" 와 "★ Épico 4" 가 겹쳤다.
        const float GradeW = 210f;
        Fit(jobChipTmp);
        {
            var rt = jobChipTmp.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(HeadPad + JobIconSz + 10f, 0f);
            rt.offsetMax = new Vector2(-(HeadPad + GradeW + 10f), 0f);
        }

        //  [★ 영웅 4] — 오른쪽 끝 GradeW 칸. 별은 칸 왼쪽, 글자는 그 뒤에서 남는 폭 안.
        var star = EditorUIBuilder.Img(head.gameObject, "StarIcon", Color.white);
        star.sprite = EditorUIBuilder.PixelSprite(IcoStar);
        star.preserveAspect = true; star.raycastTarget = false;
        {
            var rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(-(HeadPad + GradeW), 0f);
            rt.sizeDelta = new Vector2(36f, 36f);
        }
        // 색은 런타임이 등급색으로 칠한다
        var gradeChipTmp = MakeTMP(head.gameObject, "GradeChipText", "일반", UIScale.FontMd, FontStyles.Bold);
        gradeChipTmp.alignment = TextAlignmentOptions.MidlineLeft; gradeChipTmp.raycastTarget = false;
        Fit(gradeChipTmp);
        {
            var rt = gradeChipTmp.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-(HeadPad + GradeW - 44f), 0f);
            rt.offsetMax = new Vector2(-HeadPad, 0f);
        }

        // ── 초상화 — 테두리 없이 카드 바탕에 얹는다 ──
        var pb = MakeImg("PortraitBg", card, PortTint);
        TAF(pb.GetComponent<RectTransform>(), PortPad, PortH, Pad, Pad);
        var portraitBg = pb.GetComponent<Image>();
        portraitBg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropImgPath);
        portraitBg.type = Image.Type.Simple;
        portraitBg.preserveAspect = false;
        portraitBg.raycastTarget = false;

        var pi = MakeImg("PortraitImage", pb, Color.white);
        pi.GetComponent<Image>().preserveAspect = true;
        pi.GetComponent<Image>().raycastTarget  = false;
        Stretch(pi.GetComponent<RectTransform>());

        var pp = new GameObject("PortraitPreview", typeof(RectTransform));
        pp.transform.SetParent(pb.transform, false);
        pp.AddComponent<UnitAppearanceBridge>();
        Stretch(pp.GetComponent<RectTransform>());
        pp.SetActive(false);

        var dots = withDots ? BuildPageDots(card) : System.Array.Empty<Image>();

        // ── 이름 명판 + 새로고침 ──
        //  새로고침은 '이 장수를 다른 장수로' 라 이름 바로 옆이 가장 잘 읽힌다.
        float nameRight = Pad + (withRefresh ? RefreshW + 10f : 0f);
        var plate = EditorUIBuilder.PixelImage(card, "NamePlate", InfoPx, InfoScale);
        TAF(plate.rectTransform, NameY, NameH, Pad, nameRight);

        //  긴 이름 + "NEW" 는 접히지 않고 줄어든다 — 칸보다 한 줄이 크면 통째로 사라진다.
        var nameTmp = MakeTMP(plate.gameObject, "NameText", "이름", UIScale.FontLg, FontStyles.Bold);
        nameTmp.color = Color.white; nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.raycastTarget = false;
        AutoFit(nameTmp, UIScale.FontSm, UIScale.FontLg);
        Inset(nameTmp.rectTransform, InfoPad, 4f);

        refreshBtnGo = null;
        if (withRefresh)
        {
            refreshBtnGo = EditorUIBuilder.Go("RefreshBtn", card);
            EditorUIBuilder.PixelBtnOn(refreshBtnGo, BlueBtnPx, out var rBody, BtnScale);
            var rt = refreshBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-Pad, -NameY);
            rt.sizeDelta        = new Vector2(RefreshW, NameH);
            BtnContent(rBody, IconOr(rBody, "Icon", IcoRefresh, 36f, () => null),
                       "새로고침", UIScale.FontSm, Color.white, chevron: false);
        }

        BuildCardBody(card, cUI, pb, pi, pp, sel, jobIcon, jobChipTmp, gradeChipTmp, nameTmp, withTrait);
        return dots;
    }

    // 페이지 도트 (소형) — 초상화 아래쪽 안에 얹는다
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
            d.GetComponent<RectTransform>().sizeDelta = new Vector2(ds, ds);
            dots[i] = d.GetComponent<Image>();
            dots[i].color = i == 0 ? Color.white : new Color(0.35f, 0.35f, 0.55f, 0.8f);
            dots[i].raycastTarget = false;
            var le = d.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = ds;
        }
        return dots;
    }

    // 카드 본문 — 스탯 2×2 · 특성/스킬 아이콘 · 자세히 보기 + 필드 와이어링
    static void BuildCardBody(GameObject card, GeneralCandidateCardUI cUI,
                              GameObject pb, GameObject pi, GameObject pp, GameObject sel,
                              Image jobIcon, TextMeshProUGUI jobChipTmp, TextMeshProUGUI gradeChipTmp,
                              TextMeshProUGUI nameTmp, bool withTrait)
    {
        // ── 스탯 2×2 — 한 칸 안을 세로선으로 가른다 ──
        var statBox = EditorUIBuilder.PixelImage(card, "StatsBox", InfoPx, InfoScale);
        TAF(statBox.rectTransform, StatY, StatH, Pad, Pad);
        var vDiv = MakeImg("ColDivider", statBox.gameObject, DivC);
        vDiv.GetComponent<Image>().raycastTarget = false;
        {
            var rt = vDiv.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(-1f, InfoPad);
            rt.offsetMax = new Vector2( 1f, -InfoPad);
        }
        var hpTmp  = BuildStatCell(statBox.gameObject, "HpCell",      "stat_hp",            StatColors.Hp,      0, 0);
        var atkTmp = BuildStatCell(statBox.gameObject, "AtkCell",     "stat_attack",        StatColors.Atk,     0, 1);
        var defTmp = BuildStatCell(statBox.gameObject, "DefCell",     "stat_defense",       StatColors.Def,     1, 0);
        var solTmp = BuildStatCell(statBox.gameObject, "SoldierCell", "stat_soldier_count", StatColors.Soldier, 1, 1);

        // ── 특성 · 스킬 아이콘 줄 — 칸 수에 맞춰 고르게 ──
        //  ⚠ withTrait=false (용병 고용) 면 특성 칸을 통째로 뺀다.
        //    특성은 게임 시작 시 직업별로 하나 받는 것이고 용병 고용에는 안 붙는다.
        //  테두리 색으로 특성(하늘)/액티브(금)/패시브(파랑)/잠김(회색)을 가른다.
        var iconRow = new GameObject("SkillIconRow", typeof(RectTransform));
        iconRow.transform.SetParent(card.transform, false);
        TAF(iconRow.GetComponent<RectTransform>(), IconY, SkillSz, Pad, Pad);

        int   count = withTrait ? 5 : 4;
        float rowW  = CardW - Pad * 2f;
        float gap   = Mathf.Min(30f, (rowW - count * SkillSz) / (count - 1));
        float x     = (rowW - (count * SkillSz + (count - 1) * gap)) * 0.5f;

        TraitIconUI traitIconUI = null;
        if (withTrait)
        {
            traitIconUI = BuildTraitIconUI(iconRow, x, SkillSz);
            x += SkillSz + gap;
        }

        var actIcon = BuildSkillIconUI(iconRow, "ActiveSkillIcon", x, SkillSz,
                                       SkillIconUI.ActiveFrame, SkillIconUI.ActiveSlotBg);
        x += SkillSz + gap;
        var pasIcons = new SkillIconUI[3];
        for (int i = 0; i < 3; i++)
        {
            // 패시브는 잠김 상태로 굽는다 — 등급에 따라 런타임이 연다.
            pasIcons[i] = BuildSkillIconUI(iconRow, $"PassiveSkillIcon{i}", x, SkillSz,
                                           SkillIconUI.LockedFrame, SkillIconUI.LockedSlotBg);
            x += SkillSz + gap;
        }

        // ── 자세히 보기 ──
        var detBtn = EditorUIBuilder.Go("DetailBtn", card);
        EditorUIBuilder.PixelBtnOn(detBtn, BlueBtnPx, out var dBody, BtnScale);
        TAF(detBtn.GetComponent<RectTransform>(), DetY, DetH, Pad + 80f, Pad + 80f);
        BtnContent(dBody, IconOr(dBody, "Icon", IcoBook, 48f,
                       () => EditorUIBuilder.ReferenceMark(dBody, "Icon", 40f, IvoryC)),
                   "자세히 보기", UIScale.FontMd, Color.white, chevron: true);

        var cso = new SerializedObject(cUI);
        cso.Update();
        SetRef(cso, "_selectionOverlay", sel.GetComponent<Image>());
        // ⚠ _portraitBg 를 물리지 않는다 (2026-10-02)
        //   물리면 UnitPortraitHelper 가 배경 그림에 직업색(기사 = 짙은 빨강)을 '곱해서'
        //   야영지 그림이 검붉게 죽는다. 직업은 머리줄 아이콘·이름이 이미 보여 준다.
        SetRef(cso, "_portraitBg",       null);
        SetRef(cso, "_portraitImage",    pi.GetComponent<Image>());
        SetRef(cso, "_portraitBridge",   pp.GetComponent<UnitAppearanceBridge>());
        SetRef(cso, "_nameText",         nameTmp);
        SetRef(cso, "_jobIcon",          jobIcon);
        SetRef(cso, "_jobChipText",      jobChipTmp);
        SetRef(cso, "_gradeChipText",    gradeChipTmp);
        SetRef(cso, "_detailBtn",        detBtn.GetComponent<Button>());
        SetRef(cso, "_hpText",      hpTmp);
        SetRef(cso, "_atkText",     atkTmp);
        SetRef(cso, "_defText",     defTmp);
        SetRef(cso, "_soldierText", solTmp);
        SetRef(cso, "_traitIconUI", traitIconUI);
        SetRef(cso, "_activeSkillIcon", actIcon);
        SetObjArrayLocal(cso, "_passiveSkillIcons", pasIcons);
        cso.ApplyModifiedProperties();
    }

    // ── 사이드 배너 ──────────────────────────────────────────

    static (GameObject relic, GameObject codex, GameObject settings) BuildSide(GameObject content)
    {
        // ⚠ 배너는 세로 9-slice 다 (meta border: 위 100 · 아래 200, 좌우 0)
        //   폭을 원본(SideW)과 다르게 주면 도트가 가로로 늘어난다.
        var banner = EditorUIBuilder.PixelImage(content, "SideColumn", BannerPx);
        {
            var rt = banner.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(0f, -BannerTail);
            rt.offsetMax = new Vector2(SideW, BannerRise);
        }
        var side = banner.gameObject;
        BuildBannerRod(side);

        // 스플래시 타이틀 원본을 그대로 쓴다 (왕관·밑줄 포함). TMP 로 다시 그리지 않는다.
        var title = MakeImg("TitleArea", side, Color.white).GetComponent<Image>();
        title.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleImgPath);
        title.type = Image.Type.Simple;
        title.preserveAspect = true;
        title.raycastTarget = false;
        TAF(title.rectTransform, TitleY, TitleH, 24f, 24f);

        float y = TitleY + TitleH + 16f;
        var relicBtn = BuildSideBtn(side, "RelicBtn", "유물", Color.white, y,
            body => SpriteIcon(body, LobbyBtnDir + "btn_relic.png", 60f));
        y += SideBtnH + SideBtnGap;
        var codexBtn = BuildSideBtn(side, "CodexBtn", "도감", SubBtnC, y,
            body => SpriteIcon(body, LobbyBtnDir + "btn_codex.png", 60f));
        y += SideBtnH + SideBtnGap;
        // "설 정" — 두 글자 라벨이 유물·도감과 폭이 맞게 띄운다 (번역 테이블 키도 이 문자열)
        var settingsBtn = BuildSideBtn(side, "SettingsBtn", "설 정", SubBtnC, y,
            body => IconOr(body, "Icon", IcoGear, 56f,
                () => EditorUIBuilder.SlidersMark(body, "Icon", 52f, Color.white)));

        return (relicBtn, codexBtn, settingsBtn);
    }

    // 배너를 거는 금 막대 — 배너 스프라이트에는 없다. 그림이 없으면 안 그린다.
    static void BuildBannerRod(GameObject side)
    {
        var art = EditorUIBuilder.FindSprite(EditorUIBuilder.PixelThemeDir + RodPx + ".png");
        if (art == null) return;

        var img = EditorUIBuilder.Img(side, "Rod", Color.white);
        img.sprite = art; img.preserveAspect = true; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        rt.sizeDelta = new Vector2(SideW + 46f, 48f);   // ui_banner_rod 424×48 원본 그대로
    }

    // 사이드 버튼 — [아이콘] 라벨. tint 는 파란 판에 곱해진다 (주 버튼만 white).
    static GameObject BuildSideBtn(GameObject side, string name, string label, Color tint, float y,
                                   System.Func<GameObject, GameObject> makeIcon)
    {
        var go = EditorUIBuilder.Go(name, side);
        EditorUIBuilder.PixelBtnOn(go, BlueBtnPx, out var body, BtnScale);
        body.GetComponent<Image>().color = tint;
        PlaceTopCenter(go.GetComponent<RectTransform>(), y, SideBtnW, SideBtnH);
        BtnContent(body, makeIcon(body), label, UIScale.FontMd, Color.white, chevron: false);
        return go;
    }

    /// <summary>
    /// 버튼 안 내용 — [아이콘] 라벨 [›]. 전부 버튼 테두리(BtnPad) 안쪽에 놓는다.
    /// 라벨은 아이콘 오른쪽 남는 폭의 가운데. icon 이 null 이면 라벨이 버튼 전체 가운데.
    /// </summary>
    static void BtnContent(GameObject body, GameObject icon, string label, float fontSize,
                           Color color, bool chevron)
    {
        float left = BtnPad, right = BtnPad;
        if (icon != null)
        {
            PinLeft(icon, BtnPad);
            left = BtnPad + icon.GetComponent<RectTransform>().sizeDelta.x + 8f;
        }
        if (chevron)
        {
            PinRight(EditorUIBuilder.Chevron(body, "Chevron", 22f, 0f, IvoryC), -BtnPad);
            right = BtnPad + 30f;
        }

        var lt = MakeTMP(body, "Label", label, fontSize, FontStyles.Bold);
        lt.color = color; lt.raycastTarget = false;
        Fit(lt);   // "Actualizar"·"Reliquia"·"AJUSTES" 가 버튼 테두리를 넘었다
        Stretch(lt.rectTransform);
        lt.rectTransform.offsetMin = new Vector2(left, 0f);
        lt.rectTransform.offsetMax = new Vector2(-right, 0f);
    }

    // ── 화살표 ────────────────────────────────────────────────
    // ◀ ▶ 글리프는 폰트에 없다 (□ 로 렌더됨) → 꺾쇠 도형으로 그린다.
    // dirDeg: 0 = 오른쪽, 180 = 왼쪽
    static GameObject BuildArrow(GameObject parent, string name, float dirDeg)
    {
        var go = EditorUIBuilder.Go(name, parent);
        EditorUIBuilder.PixelBtnOn(go, SquarePx, out var body, SqScale);
        var mark = EditorUIBuilder.Chevron(body, "Mark", 36f, dirDeg, Color.white);
        var rt = mark.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
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

        // 스킬 칸과 같은 '바깥 판 테두리' — 없으면 어두운 칸이 카드 바탕에 녹아
        // 특성만 테두리 없이 떠 보였다 (UI 규칙 3: 테두리는 뒤 판으로)
        var frame = MakeImg("Frame", root, TraitFrameC).GetComponent<Image>();
        Stretch(frame.rectTransform);
        frame.raycastTarget = false;

        var (btn, img) = BuildIconSlot(root, SlotC, inset: 3f);

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

    static SkillIconUI BuildSkillIconUI(GameObject row, string name, float x, float size,
                                        Color frame, Color slotBg)
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

        // ⚠ 색의 정본은 SkillIconUI 다 — 런타임이 잠김 여부에 따라 갈아 끼우므로
        //   여기 굽는 색과 반드시 같은 값을 써야 첫 프레임이 깜빡이지 않는다.
        var (btn, img) = BuildIconSlot(root, slotBg, inset: 3f);
        var tooltip = InfoTooltipBuilder.Build(root, TipW);

        var so = new SerializedObject(skillUI);
        so.Update();
        SetRef(so, "_frame",     frameImg);
        SetRef(so, "_slotBg",    btn.GetComponent<Image>());
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

    //  아이콘 줄 안에서 왼쪽 x 부터 size 정사각, 세로 중앙
    static void AnchorIcon(GameObject go, float x, float size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta        = new Vector2(size, size);
    }

    // ── 스탯 칸 — [아이콘] 값 ────────────────────────────────
    //  ⚠ 라벨 글자를 두지 않는다 (사용자 지시, 2026-10-02)
    //    아이콘(하트·칼·방패·사람들)이 곧 라벨이다. 글자가 있으면 긴 언어에서
    //    라벨이 값 칸을 먹고("Defensa"·"Gesundheit"), 값이 작아졌다.
    //    그 자리를 값이 쓴다 — 아이콘 바로 뒤에 붙여 크게.
    //  값 색은 StatColors (인게임·상점 용병 카드와 같은 색 규칙).
    //  칸은 정보 칸 테두리(InfoPad) 안쪽만 쓴다 — 테두리 위에 글자가 겹치지 않게.

    static TextMeshProUGUI BuildStatCell(GameObject box, string id, string iconFile,
                                         Color valueColor, int col, int row)
    {
        var cell = EditorUIBuilder.Go(id, box);
        {
            //  상자 안쪽(InfoPad)을 2×2 로 나눈다. 가운데 세로선 양옆은 10 씩 띄운다.
            var rt = cell.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(col * 0.5f,       1f - (row + 1) * 0.5f);
            rt.anchorMax = new Vector2((col + 1) * 0.5f, 1f - row * 0.5f);
            float l = col == 0 ? InfoPad : 10f, r = col == 0 ? 10f : InfoPad;
            float b = row == 1 ? InfoPad - 6f : 0f, t = row == 0 ? InfoPad - 6f : 0f;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        var icon = EditorUIBuilder.Img(cell, "Icon", Color.white);
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StatIconDir + iconFile + ".png");
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        PinLeft(icon.gameObject, 4f, StIconSz);

        var vt = MakeTMP(cell, "ValueText", "0", UIScale.FontLg, FontStyles.Bold);
        vt.color = valueColor; vt.alignment = TextAlignmentOptions.MidlineLeft; vt.raycastTarget = false;
        Fit(vt);
        {
            var rt = vt.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f + StIconSz + 14f, 0f);
            rt.offsetMax = Vector2.zero;
        }
        return vt;
    }

    // ── 난이도 패널 + 게임 시작 (우측 열) ─────────────────────
    //
    //  ■ 출전 직전에 "무엇을 상대하는가" 를 판단하는 화면이다.
    //    작은 배지로 두면 아무도 안 보고 기본값으로 시작한다.
    //  ■ 게임 시작은 같은 프레임 안 맨 아래 — "뭘 상대할지 → 시작" 이 한 덩어리로 읽힌다.
    //  ■ 정보 칸의 아이콘은 제목 줄 앞에 작게 붙인다
    //    1차엔 칸 왼쪽에 크게 세워 두어 그만큼 글자 폭이 줄었고, 설명이 테두리를 넘었다.
    //
    //  화살표는 폰트에 없는 '‹ ›' 대신 EditorUIBuilder.Chevron 으로 그린다 (UI 규칙 2).
    static (GameObject panel, GameObject startBtn) BuildOperationPanel(GameObject content)
    {
        const float SelH     = 96f;
        const float TierIcon = 60f;
        const float BoxIcon  = 44f;
        const float BoxTextX = InfoPad + BoxIcon + 10f;  // 72
        const float DebuffSz = 64f;
        const int   DebuffMax = 4;
        const int   TierCount = 5;
        const float StartH   = 120f;
        const float StartBot = 22f;
        float rowMd = UIScale.RowMd;   // 정보 칸 제목 줄 (FontMd)

        var panel = EditorUIBuilder.PixelImage(content, "DifficultyPanel", FramePx, FrameScale).gameObject;
        var ui = panel.AddComponent<DifficultySelectorUI>();

        float y = HeadY;

        // ── 머리띠 "난이도" — 카드 머리줄과 같은 금 꺾쇠 띠 ──
        var strip = EditorUIBuilder.PixelImage(panel, "HeadStrip", HeadRowPx, HeadScale);
        PlaceTop(strip.rectTransform, Pad, y, HeadH);
        var head = MakeTMP(strip.gameObject, "Head", "난이도", UIScale.FontLg, FontStyles.Bold);
        // 도움말 — 머리띠 오른쪽 금 꺾쇠 안쪽, 머리띠 세로 가운데.
        //  PixelTheme 사각 판 — 평면 InfoBtn 은 주변 금테 사이에서 혼자 구식으로 튀었다.
        const float InfoSz = 52f;
        var info = EditorUIBuilder.PixelInfoBtn(strip.gameObject, TutorialId.HelpDifficulty, InfoSz);
        PinRight(info.gameObject, -HeadPad);

        //  제목은 양쪽을 i 버튼 폭만큼 똑같이 비워 가운데를 지키고, 그 안에서만 줄어든다
        head.color = IvoryC; head.raycastTarget = false;
        Fit(head);
        Inset(head.rectTransform, HeadPad + InfoSz + 8f, 0f);
        y += HeadH + 14f;

        // ── 선택 줄: [‹] [아이콘 등급명] [›] ──
        var prev = EditorUIBuilder.PixelBtn(panel, "PrevBtn", SquarePx, out var pBody, SqScale);
        var next = EditorUIBuilder.PixelBtn(panel, "NextBtn", SquarePx, out var nBody, SqScale);
        EditorUIBuilder.Chevron(pBody, "Ico", 36f, 180f, Color.white);
        EditorUIBuilder.Chevron(nBody, "Ico", 36f,   0f, Color.white);
        float arrTop = y + (SelH - ArrSize) * 0.5f;
        SetCornerLike(prev.gameObject, new Vector2(0f, 1f), new Vector2( Pad, -arrTop), ArrSize);
        SetCornerLike(next.gameObject, new Vector2(1f, 1f), new Vector2(-Pad, -arrTop), ArrSize);

        // 등급 명판 — 버튼 그림을 판으로만 쓴다 (누르는 건 양옆 화살표)
        var tierPlate = EditorUIBuilder.PixelImage(panel, "TierPlate", TealPx, TealScale);
        PlaceTop(tierPlate.rectTransform, Pad + ArrSize + 12f, y, SelH);

        //  [아이콘] 은 명판 왼쪽 고정, 등급명은 나머지 폭의 가운데에서 줄어든다.
        //  ⚠ HorizontalLayoutGroup 으로 묶지 않는다 — 레이아웃은 글자의 원래 폭을 잡아
        //    축소가 안 먹고, 긴 등급명이 명판 밖으로 나간다.
        const float PlateIn = 16f;
        var iconImg = EditorUIBuilder.Img(tierPlate.gameObject, "TierIcon", Color.white);
        iconImg.preserveAspect = true; iconImg.raycastTarget = false;
        PinLeft(iconImg.gameObject, PlateIn, TierIcon);

        var label = MakeTMP(tierPlate.gameObject, "TierLabel", "보통", UIScale.FontXl, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Midline; label.raycastTarget = false;
        Fit(label);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(PlateIn + TierIcon + 8f, 6f);
        label.rectTransform.offsetMax = new Vector2(-PlateIn, -6f);
        y += SelH + 12f;

        // ── 단계 게이지 — 5칸 중 몇 번째인지 + 어디까지 열렸는지 ──
        var gaugeRow = new GameObject("StepGauge", typeof(RectTransform));
        gaugeRow.transform.SetParent(panel.transform, false);
        PlaceTop(gaugeRow.GetComponent<RectTransform>(), Pad, y, 10f);
        var gh = gaugeRow.AddComponent<HorizontalLayoutGroup>();
        gh.spacing = 10; gh.childAlignment = TextAnchor.MiddleCenter;
        gh.childControlWidth = gh.childControlHeight = false;
        gh.childForceExpandWidth = gh.childForceExpandHeight = false;

        var steps = new Image[TierCount];
        for (int i = 0; i < TierCount; i++)
        {
            var stepImg = MakeImg($"Step{i}", gaugeRow, new Color(0.2f, 0.22f, 0.30f)).GetComponent<Image>();
            stepImg.rectTransform.sizeDelta = new Vector2(60f, 10f);
            stepImg.raycastTarget = false;
            steps[i] = stepImg;
        }
        y += 10f + 10f;

        // ── 요약 설명 (2줄) ──
        float sumH = UIScale.Line(UIScale.FontSm) * 2f;
        var summary = MakeTMP(panel, "SummaryLabel", "", UIScale.FontSm, FontStyles.Normal);
        summary.alignment = TextAlignmentOptions.Top; summary.raycastTarget = false;
        summary.textWrappingMode = TextWrappingModes.Normal;
        // 두 줄 안에 못 들어가는 언어만 줄인다 (줄바꿈은 유지)
        summary.enableAutoSizing = true;
        summary.fontSizeMin = UIScale.FontSm * 0.7f;
        summary.fontSizeMax = UIScale.FontSm;
        PlaceTop(summary.rectTransform, Pad + 8f, y, sumH);
        y += sumH + 12f;

        // ── 적용 제한 사항 ──
        //  [투구] 적용 제한 사항
        //  [디버프][디버프]…   (없으면 "특별한 제한 사항이 없습니다")
        float boxH = InfoPad - 6f + rowMd + 4f + DebuffSz + InfoPad - 6f;   // 145
        var cb = EditorUIBuilder.PixelImage(panel, "ConstraintBox", InfoPx, InfoScale).gameObject;
        PlaceTop(cb.GetComponent<RectTransform>(), Pad, y, boxH);
        BoxTitle(cb, IcoHelmet, () => EditorUIBuilder.PadLock(cb, "Icon", 36f, Muted),
                 "DebuffHead", "적용 제한 사항", BoxIcon, BoxTextX, rowMd);

        float rowY = InfoPad - 6f + rowMd + 4f;
        var debuffRow = new GameObject("DebuffRow", typeof(RectTransform));
        debuffRow.transform.SetParent(cb.transform, false);
        TAF(debuffRow.GetComponent<RectTransform>(), rowY, DebuffSz, InfoPad, InfoPad);
        var hlg = debuffRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = hlg.childControlHeight = false;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

        var debuffIcons = new TraitIconUI[DebuffMax];
        for (int i = 0; i < DebuffMax; i++)
            debuffIcons[i] = TraitIconSlotBuilder.Build(debuffRow, i, DebuffSz);

        // 제약이 없을 때 자리를 채우는 문구 (아이콘 0개면 빈칸만 남는다)
        var noneLbl = MakeTMP(cb, "NoDebuffLabel", "특별한 제한 사항이 없습니다", UIScale.FontSm, FontStyles.Normal);
        noneLbl.color = Muted;
        noneLbl.alignment = TextAlignmentOptions.MidlineLeft; noneLbl.raycastTarget = false;
        Fit(noneLbl);
        TAF(noneLbl.rectTransform, rowY + (DebuffSz - UIScale.RowSm) * 0.5f, UIScale.RowSm, InfoPad, InfoPad);
        y += boxH + 10f;

        // ── 보상 배율 ──
        //  [상자] 보상 배율 ············ ×3.0
        //  환생 포인트 200% 추가 획득        ← 칸 전체 폭을 쓴다
        //  제목 줄은 배율 숫자(FontLg)에 맞춰 RowLg — 숫자가 이 칸의 주인공이다
        float rowLg   = UIScale.RowLg;
        float rewardH = InfoPad - 6f + rowLg + UIScale.RowSm + InfoPad - 6f;   // 137
        //  ⚠ 제목 칸은 배율 숫자 칸(ValueW) 앞에서 끝난다 — 겹쳐 두면
        //    "Multiplicador de recompensa" 가 "×1.0" 을 덮는다 (2026-10-02).
        const float ValueW = 150f;
        var rb = EditorUIBuilder.PixelImage(panel, "RewardBox", InfoPx, InfoScale).gameObject;
        PlaceTop(rb.GetComponent<RectTransform>(), Pad, y, rewardH);
        BoxTitle(rb, IcoChest, () => EditorUIBuilder.Diamond(rb, "Icon", 30f, GoldC),
                 "RewardTitle", "보상 배율", BoxIcon, BoxTextX, rowLg, rightPad: InfoPad + ValueW + 8f);

        var reward = MakeTMP(rb, "RewardLabel", "×1.0", UIScale.FontLg, FontStyles.Bold);
        reward.color = GoldC; reward.alignment = TextAlignmentOptions.MidlineRight; reward.raycastTarget = false;
        Fit(reward);
        {
            var rt = reward.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-InfoPad, -(InfoPad - 6f));
            rt.sizeDelta = new Vector2(ValueW, rowLg);
        }

        var rDesc = MakeTMP(rb, "RewardDescLabel", "기본 보상을 획득합니다", UIScale.FontSm, FontStyles.Normal);
        rDesc.color = Muted; rDesc.alignment = TextAlignmentOptions.MidlineLeft; rDesc.raycastTarget = false;
        Fit(rDesc);
        TAF(rDesc.rectTransform, InfoPad - 6f + rowLg, UIScale.RowSm, InfoPad, InfoPad);
        y += rewardH + 6f;

        // ── 잠금 안내 — 왜 못 올리는지 ──
        var lockLbl = MakeTMP(panel, "LockLabel", "", UIScale.FontSm, FontStyles.Normal);
        lockLbl.color = new Color(0.88f, 0.66f, 0.32f);
        lockLbl.alignment = TextAlignmentOptions.Center; lockLbl.raycastTarget = false;
        AutoFit(lockLbl, UIScale.FontSm * 0.75f, UIScale.FontSm);
        PlaceTop(lockLbl.rectTransform, Pad, y, UIScale.RowSm);

        // ── 게임 시작 — 프레임 맨 아래 ──
        var startBtn = EditorUIBuilder.Go("StartBtn", panel);
        EditorUIBuilder.PixelBtnOn(startBtn, GoldBtnPx, out var sBody, GoldScale);
        PlaceBottom(startBtn.GetComponent<RectTransform>(), Pad, StartBot, StartH);
        var swords = IconOr(sBody, "Swords", IcoSwords, 76f,
            () => EditorUIBuilder.XMark(sBody, "Swords", 62f, NavyInk));
        PinLeft(swords, 40f);
        var startLabel = MakeTMP(sBody, "Label", "게임 시작", UIScale.FontXl, FontStyles.Bold);
        startLabel.color = NavyInk; startLabel.raycastTarget = false;
        Fit(startLabel);   // "Iniciar juego" 가 버튼 밖으로 나갔다
        Stretch(startLabel.rectTransform);
        startLabel.rectTransform.offsetMin = new Vector2(40f + 76f + 8f, 0f);
        startLabel.rectTransform.offsetMax = new Vector2(-BtnPad, 0f);

        var so = new SerializedObject(ui);
        so.Update();
        SetRef(so, "_tierIcon",        iconImg);
        SetRef(so, "_tierLabel",       label);
        SetRef(so, "_summaryLabel",    summary);
        SetRef(so, "_rewardLabel",     reward);
        SetRef(so, "_rewardDescLabel", rDesc);
        SetRef(so, "_lockLabel",       lockLbl);
        SetRef(so, "_noDebuffLabel",   noneLbl);
        SetRef(so, "_prevBtn",         prev);
        SetRef(so, "_nextBtn",         next);
        SetObjArrayLocal(so, "_debuffIcons", debuffIcons);
        SetObjArrayLocal(so, "_stepMarks",   steps);
        so.ApplyModifiedProperties();

        return (panel, startBtn);
    }

    // 정보 칸 제목 줄 — [아이콘] 제목(FontMd). 칸 테두리(InfoPad) 안쪽 첫 줄, 높이 rowH.
    static void BoxTitle(GameObject box, string iconFile, System.Func<GameObject> fallback,
                         string name, string text, float iconSz, float textX, float rowH,
                         float rightPad = InfoPad)
    {
        float top = InfoPad - 6f;
        var icon = IconOr(box, "Icon", iconFile, iconSz, fallback);
        var irt = icon.GetComponent<RectTransform>();
        irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
        irt.pivot     = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(InfoPad, -(top + rowH * 0.5f));

        var t = MakeTMP(box, name, text, UIScale.FontMd, FontStyles.Bold);
        t.color = IvoryC; t.alignment = TextAlignmentOptions.MidlineLeft; t.raycastTarget = false;
        Fit(t);
        TAF(t.rectTransform, top, rowH, textX, rightPad);
    }

    // ── 아이콘 ────────────────────────────────────────────────

    /// <summary>
    /// Codex 아이콘 (PixelTheme/ui_icon_*.png). 있으면 그 그림, 없으면 fallback 도형.
    /// fallback 이 null 을 돌려주면 아이콘 없이 간다 (호출부가 라벨을 가운데로 민다).
    /// </summary>
    static GameObject IconOr(GameObject parent, string name, string file, float size,
                             System.Func<GameObject> fallback)
    {
        var art = EditorUIBuilder.FindSprite(EditorUIBuilder.PixelThemeDir + file + ".png");
        if (art == null) return fallback();

        var img = EditorUIBuilder.Img(parent, name, Color.white);
        img.sprite = art; img.preserveAspect = true; img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(size, size);
        return img.gameObject;
    }

    static GameObject SpriteIcon(GameObject parent, string path, float size)
    {
        var img = EditorUIBuilder.Img(parent, "Icon", Color.white);
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        img.preserveAspect = true; img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(size, size);
        return img.gameObject;
    }

    // ── 배치 헬퍼 ────────────────────────────────────────────

    static void Stretch(RectTransform rt) => EditorUIBuilder.Stretch(rt);

    /// <summary>부모를 채우되 가로 h · 세로 v 만큼 안쪽으로.</summary>
    static void Inset(RectTransform rt, float h, float v)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(h, v);
        rt.offsetMax = new Vector2(-h, -v);
    }

    /// <summary>부모 왼쪽 x 에서 세로 중앙. size 가 0 이면 크기는 그대로 둔다.</summary>
    static void PinLeft(GameObject go, float x, float size = 0f)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        if (size > 0f) rt.sizeDelta = new Vector2(size, size);
    }

    /// <summary>부모 오른쪽 x(음수) 에서 세로 중앙.</summary>
    static void PinRight(GameObject go, float x)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
    }

    /// <summary>부모 위 기준 — 왼쪽/오른쪽 끝에서 inset 안쪽, 세로는 y 에 '가운데' 를 맞춘다.</summary>
    static void PinTopSide(GameObject go, bool left, float inset, float y, float size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
        rt.pivot     = new Vector2(left ? 0f : 1f, 0.5f);
        rt.anchoredPosition = new Vector2(left ? inset : -inset, y);
        rt.sizeDelta = new Vector2(size, size);
    }

    /// <summary>부모 상단에서 y 아래, 가로 가운데, 고정 크기.</summary>
    static void PlaceTopCenter(RectTransform rt, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    /// <summary>Top-Anchor Fill: 부모 상단 기준 y 위치, h 높이, 좌우 여백.</summary>
    static void TAF(RectTransform rt, float y, float h, float lp = 0f, float rp = 0f)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2( lp, -(y + h));
        rt.offsetMax = new Vector2(-rp,  -y);
    }

    /// <summary>패널 상단 기준 — 좌우 padX 만큼 띄우고 스트레치.</summary>
    static void PlaceTop(RectTransform rt, float padX, float y, float h) => TAF(rt, y, h, padX, padX);

    /// <summary>패널 하단 기준 배치 — 좌우 스트레치.</summary>
    static void PlaceBottom(RectTransform rt, float padX, float y, float h)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.offsetMin = new Vector2(padX, y);
        rt.offsetMax = new Vector2(-padX, y + h);
    }

    static void SetCornerLike(GameObject go, Vector2 anchor, Vector2 pos, float size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
    }

    // ── 텍스트 ────────────────────────────────────────────────

    /// <summary>
    /// 줄바꿈 금지 + 넘침 허용.
    /// ⚠ 칸보다 한 줄이 크면 Ellipsis/Truncate 는 그 줄을 통째로 버린다 —
    ///   접히거나 사라지는 사고는 전부 이 두 줄로 막는다.
    /// </summary>
    static void NoWrap(TextMeshProUGUI tmp)
    {
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
    }

    /// <summary>
    /// 한 줄 · 칸에 맞춰 축소 — 지금 크기가 상한, 그 60% 가 하한.
    /// 긴 언어(스페인어·독일어 …)에서만 줄어든다.
    /// </summary>
    static void Fit(TextMeshProUGUI tmp) => AutoFit(tmp, tmp.fontSize * 0.6f, tmp.fontSize);

    /// <summary>NoWrap + 칸에 맞춰 축소 (넘치면 잘리는 대신 작아진다).</summary>
    static void AutoFit(TextMeshProUGUI tmp, float min, float max)
    {
        NoWrap(tmp);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = min;
        tmp.fontSizeMax      = max;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    // ── 직렬화 ────────────────────────────────────────────────

    static void SetRef(SerializedObject so, string field, Object obj)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = obj;
    }

    static void SetObjArrayLocal(SerializedObject so, string field, Object[] items)
    {
        var p = so.FindProperty(field);
        if (p == null) return;
        p.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    static GameObject MakeImg(string name, GameObject parent, Color color)
        => EditorUIBuilder.Panel(parent, name, color);
}
