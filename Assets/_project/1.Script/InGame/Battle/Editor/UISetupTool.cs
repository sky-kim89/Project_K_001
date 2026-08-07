// ============================================================
//  UISetupTool.cs  [Editor Only]
//  Tools > Project K > 씬 셋업 > InGame 씬 구성 에서 실행.
//
//  담당 범위
//    - 인게임 전용 프리팹: GeneralPanel, RewardCard
//    - 현재 씬의 Canvas > InGameHUD 계층
//    - PopupManager 루트 오브젝트
//
//  팝업 프리팹은 만들지 않는다.
//    BattleResult / Pause / Loading 은 PopupPrefabCreator 가 정본이며
//    이 파일은 호출만 한다. 예전에는 양쪽이 같은 경로에 서로 다른
//    레이아웃을 덮어써서, 실행 순서에 따라 결과가 달라지는 버그가 있었다.
//
//  주의
//    - "InGameHUD" 오브젝트가 이미 있으면 삭제 후 재생성한다.
//    - 실행 후 씬을 저장(Ctrl+S)할 것.
// ============================================================
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class UISetupTool
{
    const string PANEL_PREFAB       = "Assets/_project/2.Prefabs/UI/GeneralPanel.prefab";
    const string REWARD_CARD_PREFAB = "Assets/_project/2.Prefabs/UI/RewardCard.prefab";


    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Setup + "InGame 씬 구성", priority = ProjectKMenu.SetupPrio + 1)]
    public static void SetupInGameUI()
    {
        // 1. 인게임 프리팹 (RewardCard 는 BattleResultPopup 이 참조하므로 먼저)
        var panelPrefab = CreateGeneralPanelPrefab();
        CreateRewardCardPrefab();

        // 2. 팝업 프리팹 — PopupPrefabCreator 가 정본이므로 위임
        PopupPrefabCreator.CreateBattleResultPopup();
        PopupPrefabCreator.CreatePausePopup();
        PopupPrefabCreator.CreateLoadingPopup();

        // 3. Canvas + InGameHUD 계층 생성
        var canvasGo = CreateCanvasHierarchy(panelPrefab);

        // 4. PopupManager 루트 오브젝트 생성/업데이트
        CreateOrUpdatePopupManager(canvasGo);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[UISetupTool] ✓ InGame UI 셋업 완료 — 씬을 저장하세요 (Ctrl+S)");
    }

    // ══════════════════════════════════════════════════════════
    //  개별 프리팹 재생성 메뉴
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.InGame + "GeneralPanel", priority = ProjectKMenu.PrefabPrio + 45)]
    public static void MenuCreateGeneralPanel()
    {
        CreateGeneralPanelPrefab();
        AssetDatabase.SaveAssets();
    }

    [MenuItem(ProjectKMenu.InGame + "RewardCard", priority = ProjectKMenu.PrefabPrio + 46)]
    public static void MenuCreateRewardCard()
    {
        CreateRewardCardPrefab();
        AssetDatabase.SaveAssets();
    }

    // ══════════════════════════════════════════════════════════
    //  GeneralPanel 프리팹
    // ══════════════════════════════════════════════════════════

    static GameObject CreateGeneralPanelPrefab()
    {
        // ┌─────────────────────────────────────────────────────────┐
        // │ 레이아웃 (좌상단 원점, y ↓)                             │
        // │  Portrait : x=4  y=4  w=80  h=80                       │
        // │  NameText : x=88 y=4  w=228 h=36   font=UIScale.FontSm │
        // │  GradeText: x=88 y=44 w=228 h=28   font=22             │
        // │  HpBarBg  : x=88 y=76 w=228 h=26                       │
        // │  SolBarBg : x=88 y=106 w=228 h=26                      │
        // │  SkillSlot: x=4  y=136 w=80  h=62                      │
        // │  BuffSlot : 2행×7열=14개, 28×28, step 32               │
        // └─────────────────────────────────────────────────────────┘
        const float PW      = 320f;
        const float PH      = 210f;
        const float PortW   = 80f;
        const float PortH   = 80f;
        const float TextX   = 88f;    // PortW(80) + margin(4) + left-pad(4) = 88
        const float TextW   = 228f;   // PW(320) - TextX(88) - rightPad(4) = 228
        const float BarH    = 26f;
        const int   BarFont = 18;

        var root = new GameObject("GeneralPanel");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(PW, PH);
        root.AddComponent<CanvasGroup>();

        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth  = PW;
        le.preferredHeight = PH;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.10f, 0.90f);

        var panelUI = root.AddComponent<GeneralPanelUI>();

        // ── Portrait (80×80) ─────────────────────────────────
        var portraitBg = MakeImg(root, "Portrait", new Color(0.8f, 0.22f, 0.22f));
        SetTL(portraitBg.gameObject, 4, 4, PortW, PortH);

        var portraitIconGo = MakeRect(portraitBg.gameObject, "PortraitIcon");
        Stretch(portraitIconGo);
        var portraitIcon = portraitIconGo.AddComponent<Image>();
        portraitIcon.color          = Color.white;
        portraitIcon.preserveAspect = false;

        // ── Name / Grade ──────────────────────────────────────
        float nameH  = Mathf.Round(UIScale.FontSm) + 6f;   // 36
        float gradeH = 28f;

        var nameText = MakeTMP(root, "NameText", "장군 이름", (int)UIScale.FontSm, FontStyle.Bold);
        SetTL(nameText.gameObject, TextX, 4, TextW, nameH);

        var gradeText = MakeTMP(root, "GradeText", "", 22, FontStyle.Normal);
        gradeText.color = new Color(1f, 0.85f, 0.3f);
        SetTL(gradeText.gameObject, TextX, 4 + nameH + 4, TextW, gradeH);

        // ── HP Bar ────────────────────────────────────────────
        float barY1 = 4 + nameH + 4 + gradeH + 4;   // 76

        var hpBg = MakeImg(root, "HpBarBg", new Color(0.12f, 0.04f, 0.04f));
        SetTL(hpBg.gameObject, TextX, barY1, TextW, BarH);

        var hpFill = MakeFilledH(hpBg.gameObject, "HpFill", new Color(0.85f, 0.15f, 0.15f));

        var hpText = MakeTMP(hpBg.gameObject, "HpText", "100/100", BarFont, FontStyle.Normal);
        Stretch(hpText.gameObject);

        // ── Soldier Bar ───────────────────────────────────────
        float barY2 = barY1 + BarH + 4;   // 106

        var solBg = MakeImg(root, "SoldierBarBg", new Color(0.04f, 0.08f, 0.16f));
        SetTL(solBg.gameObject, TextX, barY2, TextW, BarH);

        var soldierFill = MakeFilledH(solBg.gameObject, "SoldierFill", new Color(0.2f, 0.5f, 0.9f));

        var soldierText = MakeTMP(solBg.gameObject, "SoldierText", "5/5", BarFont, FontStyle.Normal);
        Stretch(soldierText.gameObject);

        // ── SkillSlot (80×62) ────────────────────────────────
        float skillY = barY2 + BarH + 4;   // 136

        var skillSlotGo = MakeRect(root, "SkillSlot");
        SetTL(skillSlotGo, 4, skillY, PortW, 62);
        var skillSlot = skillSlotGo.AddComponent<SkillSlotUI>();

        var skillBg = MakeImg(skillSlotGo, "SkillBg", new Color(0.12f, 0.12f, 0.12f));
        Stretch(skillBg.gameObject);

        var skillIcon = MakeImg(skillSlotGo, "Icon", Color.white);
        Stretch(skillIcon.gameObject);
        skillIcon.preserveAspect = true;

        var cdOverlay = MakeImg(skillSlotGo, "CooldownOverlay", new Color(0f, 0f, 0f, 0.78f));
        Stretch(cdOverlay.gameObject);
        {
            var imgSO = new SerializedObject(cdOverlay);
            imgSO.FindProperty("m_Type").intValue           = (int)Image.Type.Filled;
            imgSO.FindProperty("m_FillMethod").intValue     = (int)Image.FillMethod.Radial360;
            imgSO.FindProperty("m_FillClockwise").boolValue = false;
            imgSO.FindProperty("m_FillAmount").floatValue   = 0f;
            imgSO.ApplyModifiedPropertiesWithoutUndo();
        }

        var cdText = MakeTMP(skillSlotGo, "CooldownText", "", 24, FontStyle.Bold);
        Stretch(cdText.gameObject);
        cdText.outlineWidth = 0.25f;
        cdText.outlineColor = Color.black;
        cdText.gameObject.SetActive(false);

        var readyGlow = MakeRect(skillSlotGo, "ReadyGlow");
        readyGlow.AddComponent<Image>().color = new Color(1f, 0.9f, 0.2f, 0.45f);
        Stretch(readyGlow);
        readyGlow.SetActive(false);

        // ── Buff Slots (14개, 28×28, 2행×7열) ────────────────
        // Row 1: y = skillY+4, Row 2: y = skillY+4+32
        // x = TextX + (col * 32), col = i % 7
        const int   BuffCount  = 14;
        const int   BuffCols   = 7;
        const float BuffSlotSz = 28f;
        const float BuffStep   = 32f;

        var buffSlots      = new Image[BuffCount];
        var buffStackTexts = new TextMeshProUGUI[BuffCount];

        for (int i = 0; i < BuffCount; i++)
        {
            int   col  = i % BuffCols;
            int   row  = i / BuffCols;
            float bx   = TextX + col * BuffStep;
            float by   = skillY + 4 + row * BuffStep;

            // 슬롯 컨테이너 (색상 없음 — 아이콘 스프라이트만 표시)
            var slotGo = MakeRect(root, $"BuffSlot{i}");
            SetTL(slotGo, bx, by, BuffSlotSz, BuffSlotSz);

            // 아이콘 이미지 (Color.white — 스프라이트 원본 색 그대로)
            buffSlots[i] = slotGo.AddComponent<Image>();
            buffSlots[i].color          = Color.white;
            buffSlots[i].preserveAspect = true;

            // 스택 카운트 텍스트 (우하단, 기본 비활성)
            var stackTxt = MakeTMP(slotGo, "StackCount", "2", 12, FontStyle.Bold);
            stackTxt.color     = Color.yellow;
            stackTxt.alignment = TextAlignmentOptions.BottomRight;
            stackTxt.outlineWidth = 0.3f;
            stackTxt.outlineColor = Color.black;
            {
                var srt = stackTxt.GetComponent<RectTransform>();
                srt.anchorMin        = Vector2.zero;
                srt.anchorMax        = Vector2.one;
                srt.offsetMin        = Vector2.zero;
                srt.offsetMax        = Vector2.zero;
            }
            stackTxt.gameObject.SetActive(false);
            buffStackTexts[i] = stackTxt;

            slotGo.SetActive(false);
        }

        // ── SerializedField 연결 ──────────────────────────────
        var pso = new SerializedObject(panelUI);
        pso.FindProperty("_portraitBg").objectReferenceValue   = portraitBg;
        pso.FindProperty("_portraitIcon").objectReferenceValue = portraitIcon;
        pso.FindProperty("_nameText").objectReferenceValue     = nameText;
        pso.FindProperty("_gradeText").objectReferenceValue    = gradeText;
        pso.FindProperty("_hpFill").objectReferenceValue       = hpFill;
        pso.FindProperty("_hpText").objectReferenceValue       = hpText;
        pso.FindProperty("_soldierFill").objectReferenceValue  = soldierFill;
        pso.FindProperty("_soldierText").objectReferenceValue  = soldierText;
        pso.FindProperty("_skillSlot").objectReferenceValue    = skillSlot;

        var buffArr = pso.FindProperty("_buffSlots");
        buffArr.arraySize = BuffCount;
        for (int i = 0; i < BuffCount; i++)
            buffArr.GetArrayElementAtIndex(i).objectReferenceValue = buffSlots[i];

        var stackArr = pso.FindProperty("_buffStackTexts");
        stackArr.arraySize = BuffCount;
        for (int i = 0; i < BuffCount; i++)
            stackArr.GetArrayElementAtIndex(i).objectReferenceValue = buffStackTexts[i];

        pso.ApplyModifiedPropertiesWithoutUndo();

        var sso = new SerializedObject(skillSlot);
        sso.FindProperty("_iconImage").objectReferenceValue       = skillIcon;
        sso.FindProperty("_cooldownOverlay").objectReferenceValue = cdOverlay;
        sso.FindProperty("_cooldownText").objectReferenceValue    = cdText;
        sso.FindProperty("_readyGlow").objectReferenceValue       = readyGlow;
        sso.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PANEL_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[UISetupTool] GeneralPanel 프리팹 저장 → {PANEL_PREFAB}");
        return prefab;
    }

    // ══════════════════════════════════════════════════════════
    //  RewardCard 프리팹
    // ══════════════════════════════════════════════════════════

    //  아이콘이 카드 전면을 채우고, 수량은 우하단 배지로만 얹는다.
    //  이름·설명·스탯은 카드에 적지 않고 눌렀을 때 툴팁으로 보여준다
    //  (특성 아이콘과 동일한 동작 — InfoTooltipUI 공용).
    //
    //    RewardCard (Button)
    //    ├─ Frame         종류·등급 색 테두리
    //    ├─ IconImage     카드를 가득 채움
    //    ├─ AmountBadge   우하단 수량 (수량 없는 보상이면 비활성)
    //    │   └─ AmountText
    //    ├─ RevealOverlay "?" — 미개봉 박스 전용
    //    └─ Tooltip       InfoTooltipUI (기본 비활성)

    static GameObject CreateRewardCardPrefab()
    {
        const float CW = 128f, CH = 128f;
        const float FrameW = 3f;      // 테두리 두께
        const float BadgeH = 40f;

        var root = new GameObject("RewardCard");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(CW, CH);
        root.AddComponent<RewardCardUI>();

        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth  = CW;
        le.preferredHeight = CH;

        // 테두리 = 루트 Image. 종류·등급 색으로 런타임에 교체된다.
        var frame = root.AddComponent<Image>();
        frame.color = new Color(0.30f, 0.32f, 0.44f, 1f);

        // 어두운 안쪽 바탕 — 아이콘이 비어도 카드로 읽히게
        var inner = MakeImg(root, "Inner", new Color(0.07f, 0.08f, 0.13f, 1f));
        SetRT(inner.gameObject, Vector2.zero, Vector2.one,
              Vector2.zero, new Vector2(-FrameW * 2f, -FrameW * 2f));
        inner.raycastTarget = false;

        // 아이콘 — 카드 전면
        var iconGo  = MakeRect(root, "IconImage");
        SetRT(iconGo, Vector2.zero, Vector2.one,
              Vector2.zero, new Vector2(-FrameW * 2f - 8f, -FrameW * 2f - 8f));
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.color          = Color.white;

        // 수량 배지 (우하단)
        var badge = MakeImg(root, "AmountBadge", new Color(0.04f, 0.05f, 0.09f, 0.88f));
        badge.raycastTarget = false;
        {
            var brt = badge.rectTransform;
            brt.anchorMin        = new Vector2(1f, 0f);
            brt.anchorMax        = new Vector2(1f, 0f);
            brt.pivot            = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(-FrameW - 2f, FrameW + 2f);
            brt.sizeDelta        = new Vector2(CW * 0.62f, BadgeH);
        }

        var amountTmp = MakeTMP(badge.gameObject, "AmountText", "×1", (int)UIScale.FontSm, FontStyle.Bold);
        amountTmp.color            = Color.white;
        amountTmp.alignment        = TextAlignmentOptions.Right;
        amountTmp.raycastTarget    = false;
        amountTmp.textWrappingMode = TextWrappingModes.NoWrap;
        amountTmp.overflowMode     = TextOverflowModes.Overflow;
        amountTmp.enableAutoSizing = true;
        amountTmp.fontSizeMin      = UIScale.FontSm - 10;
        amountTmp.fontSizeMax      = UIScale.FontSm;
        SetRT(amountTmp.gameObject, Vector2.zero, Vector2.one,
              Vector2.zero, new Vector2(-12f, 0f));

        // 미개봉 오버레이 ("?" 상태)
        var overlay = MakeRect(root, "RevealOverlay");
        Stretch(overlay);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color         = new Color(0.08f, 0.08f, 0.14f, 0.92f);
        overlayImg.raycastTarget = false;

        var qMark = MakeTMP(overlay, "QuestionMark", "?", (int)UIScale.FontXl, FontStyle.Bold);
        qMark.color         = new Color(0.9f, 0.85f, 0.5f);
        qMark.raycastTarget = false;
        Stretch(qMark.gameObject);

        // 탭 버튼 (전체 크기 투명 — 박스 개봉 / 상세 툴팁)
        var btnGo = MakeRect(root, "CardButton");
        Stretch(btnGo);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0f, 0f, 0f, 0f);
        var btn = btnGo.AddComponent<Button>();
        var nav = btn.navigation;
        nav.mode   = Navigation.Mode.None;
        btn.navigation = nav;

        // 상세 툴팁 (특성 아이콘과 같은 공용 컴포넌트)
        var tooltip = BuildInfoTooltip(root);

        // 필드 연결
        var so = new SerializedObject(root.GetComponent<RewardCardUI>());
        so.FindProperty("_frame").objectReferenceValue         = frame;
        so.FindProperty("_icon").objectReferenceValue          = iconImg;
        so.FindProperty("_amountBadge").objectReferenceValue   = badge.gameObject;
        so.FindProperty("_amountText").objectReferenceValue    = amountTmp;
        so.FindProperty("_revealOverlay").objectReferenceValue = overlay;
        so.FindProperty("_cardButton").objectReferenceValue    = btn;
        so.FindProperty("_tooltip").objectReferenceValue       = tooltip;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, REWARD_CARD_PREFAB);
        Object.DestroyImmediate(root);
        Debug.Log($"[UISetupTool] RewardCard 프리팹 저장 → {REWARD_CARD_PREFAB}");
        return prefab;
    }

    // 카드 아래로 펼쳐지는 상세 툴팁. 열릴 때 루트 캔버스로 옮겨진다.
    static InfoTooltipUI BuildInfoTooltip(GameObject parent)
        => InfoTooltipBuilder.Build(parent, 340f);

    // ══════════════════════════════════════════════════════════
    //  Canvas + InGameHUD 계층
    // ══════════════════════════════════════════════════════════

    static GameObject CreateCanvasHierarchy(GameObject panelPrefab)
    {
        // 기존 InGameHUD 루트 제거
        var existing = GameObject.Find("InGameHUD");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.transform.root.gameObject);
            Debug.Log("[UISetupTool] 기존 Canvas(InGameHUD 포함) 제거 후 재생성");
        }

        // ── Canvas ─────────────────────────────────────────────
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── InGameHUD ─────────────────────────────────────────
        var hudGo = MakeRect(canvasGo, "InGameHUD");
        Stretch(hudGo);
        var hud = hudGo.AddComponent<InGameHUD>();

        // ── TopBar ─────────────────────────────────────────────
        var topBarGo = MakeRect(hudGo, "TopBar");
        var topBarRT = topBarGo.GetComponent<RectTransform>();
        topBarRT.anchorMin        = new Vector2(0f, 1f);
        topBarRT.anchorMax        = new Vector2(1f, 1f);
        topBarRT.pivot            = new Vector2(0.5f, 1f);
        topBarRT.anchoredPosition = Vector2.zero;
        topBarRT.sizeDelta        = new Vector2(0f, 110f);

        var topBarBg = topBarGo.AddComponent<Image>();
        topBarBg.color = new Color(0.04f, 0.04f, 0.08f, 0.90f);

        var topBar = topBarGo.AddComponent<TopBarUI>();

        // Wave 텍스트
        var waveText = MakeTMP(topBarGo, "WaveText", "Wave 1 / 10", 22, FontStyle.Bold);
        SetRT(waveText.gameObject,
              new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -10f), new Vector2(300f, 28f));

        // Wave 진행 바
        var waveBarBg = MakeImg(topBarGo, "WaveProgressBg", new Color(0.2f, 0.2f, 0.2f));
        SetRT(waveBarBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -42f), new Vector2(500f, 10f));
        var waveProgressFill = MakeFilledH(waveBarBg.gameObject, "WaveProgressFill",
                                           new Color(0.25f, 0.85f, 0.25f));

        // 타이머
        var waveTimerText = MakeTMP(topBarGo, "WaveTimerText", "0s", 14, FontStyle.Normal);
        SetRT(waveTimerText.gameObject,
              new Vector2(1f, 1f), new Vector2(1f, 1f),
              new Vector2(-74f, -10f), new Vector2(80f, 20f));
        waveTimerText.alignment = TextAlignmentOptions.Right;

        // 킬 카운트
        var killCountText = MakeTMP(topBarGo, "KillCountText", "Kills: 0", 14, FontStyle.Normal);
        SetRT(killCountText.gameObject,
              new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(10f, -10f), new Vector2(100f, 20f));
        killCountText.alignment = TextAlignmentOptions.Left;

        // 보스 HP
        var bossHpRoot = MakeRect(topBarGo, "BossHpRoot");
        SetRT(bossHpRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -56f), new Vector2(400f, 22f));
        var bossHpBg = bossHpRoot.AddComponent<Image>();
        bossHpBg.color = new Color(0.15f, 0.04f, 0.04f);
        var bossHpFill = MakeFilledH(bossHpRoot, "BossHpFill", new Color(0.9f, 0.1f, 0.1f));
        var bossHpText = MakeTMP(bossHpRoot, "BossHpText", "BOSS  1000 / 1000", 11, FontStyle.Bold);
        Stretch(bossHpText.gameObject);
        bossHpRoot.SetActive(false);

        // ── 하단 버튼 행 (배속 + 일시 정지) ──────────────────
        var bottomRow = MakeRect(topBarGo, "BottomButtons");
        SetRT(bottomRow, new Vector2(1f, 0f), new Vector2(1f, 0f),
              new Vector2(-4f, 4f), new Vector2(256f, 36f));
        var bottomHLG = bottomRow.AddComponent<HorizontalLayoutGroup>();
        bottomHLG.spacing              = 4;
        bottomHLG.childAlignment       = TextAnchor.MiddleRight;
        bottomHLG.childForceExpandWidth  = false;
        bottomHLG.childForceExpandHeight = false;

        var btn1     = MakeButton(bottomRow, "Speed1xButton", "1×");
        var btn2     = MakeButton(bottomRow, "Speed2xButton", "2×");
        var btn3     = MakeButton(bottomRow, "Speed3xButton", "3×");
        var pauseBtn = MakePauseButton(bottomRow, "PauseButton", "||",
                           new Color(0.28f, 0.28f, 0.28f), 0f, 60f, 36f);

        // TopBarUI 필드 연결
        var tso = new SerializedObject(topBar);
        tso.FindProperty("_waveText").objectReferenceValue         = waveText;
        tso.FindProperty("_waveProgressFill").objectReferenceValue = waveProgressFill;
        tso.FindProperty("_waveTimerText").objectReferenceValue    = waveTimerText;
        tso.FindProperty("_bossHpRoot").objectReferenceValue       = bossHpRoot;
        tso.FindProperty("_bossHpFill").objectReferenceValue       = bossHpFill;
        tso.FindProperty("_bossHpText").objectReferenceValue       = bossHpText;
        tso.FindProperty("_killCountText").objectReferenceValue    = killCountText;
        tso.FindProperty("_speed1xButton").objectReferenceValue    = btn1;
        tso.FindProperty("_speed2xButton").objectReferenceValue    = btn2;
        tso.FindProperty("_speed3xButton").objectReferenceValue    = btn3;
        tso.FindProperty("_pauseButton").objectReferenceValue      = pauseBtn;

        tso.ApplyModifiedPropertiesWithoutUndo();

        // ── GeneralPanelContainer ──────────────────────────────
        var container = MakeRect(hudGo, "GeneralPanelContainer");
        var contRT    = container.GetComponent<RectTransform>();
        contRT.anchorMin        = new Vector2(0f, 0f);
        contRT.anchorMax        = new Vector2(1f, 0f);
        contRT.pivot            = new Vector2(0.5f, 0f);
        contRT.anchoredPosition = new Vector2(0f, 10f);
        contRT.sizeDelta        = new Vector2(0f, 226f);  // PH(210) + padding top(8) + bottom(8)

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 8;
        hlg.padding              = new RectOffset(10, 10, 8, 8);
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        // ── InGameHUD 필드 연결 ───────────────────────────────
        var hso = new SerializedObject(hud);
        hso.FindProperty("_topBar").objectReferenceValue                = topBar;
        hso.FindProperty("_generalPanelPrefab").objectReferenceValue    = panelPrefab;
        hso.FindProperty("_generalPanelContainer").objectReferenceValue = container.transform;
        hso.FindProperty("_maxGeneralPanels").intValue                  = 5;

        hso.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[UISetupTool] Canvas > InGameHUD 계층 생성 완료");
        return canvasGo;
    }

    // ══════════════════════════════════════════════════════════
    //  PopupManager 씬 오브젝트
    // ══════════════════════════════════════════════════════════

    static void CreateOrUpdatePopupManager(GameObject canvasGo)
    {
        var pm = Object.FindObjectOfType<PopupManager>();
        if (pm == null)
        {
            var go = new GameObject("PopupManager");
            pm = go.AddComponent<PopupManager>();
            Debug.Log("[UISetupTool] PopupManager 생성");
        }

        // _popupRoot 를 Canvas 로 설정
        var pmSO = new SerializedObject(pm);
        pmSO.FindProperty("_popupRoot").objectReferenceValue = canvasGo.transform;
        pmSO.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[UISetupTool] PopupManager._popupRoot → Canvas");
    }

    // ══════════════════════════════════════════════════════════
    //  InGameManager 참조 연결
    // ══════════════════════════════════════════════════════════


    // ══════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════

    /// <summary>HUD 상단바 일시정지 버튼 (LayoutElement 없음, 중앙 정렬).</summary>
    static Button MakePauseButton(GameObject parent, string name, string label,
                                  Color bgColor, float anchoredY, float w, float h)
    {
        var go  = MakeRect(parent, name);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta        = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var labelGo = MakeRect(go, "Label");
        Stretch(labelGo);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    static GameObject MakeRect(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Image MakeImg(GameObject parent, string name, Color color)
    {
        var go  = MakeRect(parent, name);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Image MakeFilledH(GameObject parent, string name, Color color)
    {
        var img = MakeImg(parent, name, color);
        var so = new SerializedObject(img);
        so.FindProperty("m_Sprite").objectReferenceValue = GetOrCreateWhiteBarSprite();
        so.FindProperty("m_Type").intValue       = (int)Image.Type.Filled;
        so.FindProperty("m_FillMethod").intValue = (int)Image.FillMethod.Horizontal;
        so.FindProperty("m_FillAmount").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
        Stretch(img.gameObject);
        return img;
    }

    static Sprite GetOrCreateWhiteBarSprite()
    {
        const string assetPath = "Assets/_project/3.Textures/UI/FillBar.png";

        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (existing != null) return existing;

        string dir = System.IO.Path.GetDirectoryName(
            System.IO.Path.Combine(Application.dataPath, "..", assetPath));
        System.IO.Directory.CreateDirectory(dir);

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();

        string fullPath = System.IO.Path.Combine(Application.dataPath,
            assetPath.Substring("Assets/".Length));
        System.IO.File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType      = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode       = FilterMode.Point;
        importer.mipmapEnabled    = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static void SetTL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name,
                                   string text, int size, FontStyle style)
    {
        var go  = MakeRect(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static Button MakeButton(GameObject parent, string name, string label)
    {
        var go  = MakeRect(parent, name);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth  = 60;
        le.preferredHeight = 36;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.35f, 0.35f, 0.35f);
        var btn = go.AddComponent<Button>();

        var labelGo = MakeRect(go, "Label");
        Stretch(labelGo);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    // ── RectTransform 헬퍼 ────────────────────────────────────

    static void SetRT(Component c, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var rt = c.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    static void SetRT(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                      Vector2 anchoredPos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = (anchorMin + anchorMax) * 0.5f;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    static void SetRT(Image img, Vector2 anchorMin, Vector2 anchorMax,
                      Vector2 anchoredPos, Vector2 size)
        => SetRT(img.gameObject, anchorMin, anchorMax, anchoredPos, size);

    static void Stretch(GameObject go) => EditorUIBuilder.Stretch(go);
}
