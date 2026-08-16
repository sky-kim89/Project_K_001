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

    // 인게임 캔버스는 화면 비율에 따라 크기가 달라진다 (ScaleWithScreenSize, match 0.5).
    //   세로 1080×1920 / 가로 1920×1080 — **가로일 때 세로 여유가 1080 뿐**이다.
    //   HUD 가 그 절반을 먹으면 전투가 안 보인다. 상·하단 합계를 300 아래로 유지할 것.
    //
    // ⚠ 카드 내용은 반드시 좌우 스트레치로 잡는다 (SetTopStretch)
    //   패널 폭은 HorizontalLayoutGroup 이 균등 분배해 3명이면 넓고 5명이면 좁다.
    //   고정 픽셀 폭으로 두면 넓어진 만큼 오른쪽이 통째로 빈다 — 실제로 그랬다.
    const float GpMinW    = 200f;
    const float GpPad     =   8f;
    const float GpGradeW  =   6f;
    // ⚠ 초상화를 키우면 그만큼 바(HP·병사) 폭이 줄어든다.
    //   세로 화면 5명일 때 카드 폭이 208 밖에 안 돼 80 이 한계다.
    const float GpPortSz  =  80f;
    const float GpSkillSz =  48f;

    static GameObject CreateGeneralPanelPrefab()
    {
        //   [버][버][버][버][버]        ← 카드 **밖** 위쪽 (배경 없는 전장 위에 떠 있다)
        // ┌──────────────────────────────────────────────────────────┐
        // │ ┃ ┌────────┐ 이름                        희귀           │
        // │ ┃ │초상화 80│ ▬▬▬▬▬▬▬ 1694/1693  (HP)              │
        // │ ┃ │ [마법사] │ ▬▬▬▬▬▬▬ 24/29      (병사)            │
        // │ ┃ └[스킬48]─┘                                          │
        // └──────────────────────────────────────────────────────────┘
        //
        // ⚠ 바 높이 44 는 FontSm(34) 한 줄(Line=43)을 담기 위한 값이다 (UI 규칙 5).
        float nameH = UIScale.RowSm;          // 43
        float barH  = UIScale.RowSm + 1f;     // 44
        float buffH = 36f;

        float innerL = GpGradeW + GpPad;               // 14 — 등급 바 오른쪽
        float textL  = innerL + GpPortSz + 6f;         // 100 — 텍스트 열 시작

        // 텍스트 열(이름+HP+병사)이 초상화보다 길다 — 그쪽이 행 높이를 정한다
        float textColH = nameH + 4f + barH + 4f + barH;   // 139

        float rowAY  = GpPad;                             // 8
        float nameY  = rowAY;
        float hpY    = nameY + nameH + 4f;                // 55
        float solY   = hpY   + barH  + 4f;                // 103
        float skillY = rowAY + GpPortSz + 4f;             //  92 — 초상화 아래 빈 자리
        float PH     = rowAY + textColH + GpPad;          // 155 — 버프는 카드 밖이라 안 센다

        // 버프는 카드 **위쪽 바깥**에 띄운다 (y 음수 = 카드 상단보다 위).
        // 카드 배경이 없는 자리라 전장 위에 아이콘만 얹힌 것처럼 보인다.
        float buffY  = -(buffH + 6f);                     // -42

        var root = new GameObject("GeneralPanel");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(GpMinW, PH);
        root.AddComponent<CanvasGroup>();

        // 폭은 레이아웃 그룹이 나눠 준다 — 고정하지 않고 최소치만 건다
        var le = root.AddComponent<LayoutElement>();
        le.minWidth        = GpMinW;
        le.preferredHeight = PH;
        le.flexibleWidth   = 1f;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.14f, 0.94f);   // 로비 카드와 같은 남색

        var panelUI = root.AddComponent<GeneralPanelUI>();

        // ── 등급 색 바 (좌측 세로 전체) ───────────────────────
        var gradeBorder = MakeImg(root, "GradeBorder", new Color(0.55f, 0.55f, 0.60f));
        {
            var rt = gradeBorder.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(GpGradeW, 0f);
        }
        gradeBorder.raycastTarget = false;

        // ── 초상화 + 직업 칩 ──────────────────────────────────
        var portraitBg = MakeImg(root, "Portrait", new Color(0.8f, 0.22f, 0.22f));
        SetTL(portraitBg.gameObject, innerL, rowAY, GpPortSz, GpPortSz);

        var portraitIconGo = MakeRect(portraitBg.gameObject, "PortraitIcon");
        Stretch(portraitIconGo);
        var portraitIcon = portraitIconGo.AddComponent<Image>();
        portraitIcon.color          = Color.white;
        portraitIcon.preserveAspect = true;

        var jobChip = MakeImg(portraitBg.gameObject, "JobChip", new Color(0.03f, 0.035f, 0.06f, 0.86f));
        {
            var rt = jobChip.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, UIScale.RowSm - 8f);
        }
        jobChip.raycastTarget = false;
        var jobChipText = MakeTMP(jobChip.gameObject, "Label", "기사", (int)(UIScale.FontSm * 0.85f), FontStyle.Bold);
        Stretch(jobChipText.gameObject);
        jobChipText.color            = new Color(0.86f, 0.90f, 1.00f);
        jobChipText.alignment        = TextAlignmentOptions.Center;
        jobChipText.textWrappingMode = TextWrappingModes.NoWrap;
        jobChipText.overflowMode     = TextOverflowModes.Overflow;

        // ── 스킬 슬롯 (초상화 아래 남는 자리) ─────────────────
        //  텍스트 열(139)이 초상화(88)보다 길어 그 아래가 비어 있다. 거기에 넣는다.
        var skillSlotGo = MakeRect(root, "SkillSlot");
        SetTL(skillSlotGo, innerL + (GpPortSz - GpSkillSz) * 0.5f, skillY, GpSkillSz, GpSkillSz);
        var skillSlot = skillSlotGo.AddComponent<SkillSlotUI>();

        var skillBg = MakeImg(skillSlotGo, "SkillBg", new Color(0.12f, 0.13f, 0.20f));
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

        var cdText = MakeTMP(skillSlotGo, "CooldownText", "", (int)UIScale.FontSm, FontStyle.Bold);
        Stretch(cdText.gameObject);
        cdText.outlineWidth = 0.25f;
        cdText.outlineColor = Color.black;
        cdText.gameObject.SetActive(false);

        var readyGlow = MakeRect(skillSlotGo, "ReadyGlow");
        readyGlow.AddComponent<Image>().color = new Color(1f, 0.9f, 0.2f, 0.45f);
        Stretch(readyGlow);
        readyGlow.SetActive(false);

        // ── 이름 + 등급 (한 줄, 이름 좌 / 등급 우) ────────────
        //  ⚠ 여기부터는 전부 좌우 스트레치다. 패널이 넓어지면 같이 늘어난다.
        //  긴 이름은 접히지 않고 줄어들게 한다 — 칸보다 한 줄이 크면 통째로 사라진다.
        const float GradeW = 88f;

        var nameText = MakeTMP(root, "NameText", "장군 이름", (int)UIScale.FontSm, FontStyle.Bold);
        SetTopStretch(nameText.gameObject, textL, GpPad + GradeW, nameY, nameH);
        nameText.alignment          = TextAlignmentOptions.MidlineLeft;
        nameText.textWrappingMode   = TextWrappingModes.NoWrap;
        nameText.overflowMode       = TextOverflowModes.Overflow;
        nameText.enableAutoSizing   = true;
        nameText.fontSizeMin        = UIScale.FontSm * 0.72f;
        nameText.fontSizeMax        = UIScale.FontSm;

        var gradeText = MakeTMP(root, "GradeText", "일반", (int)UIScale.FontSm, FontStyle.Bold);
        SetTopStretch(gradeText.gameObject, 0f, GpPad, nameY, nameH);
        gradeText.alignment          = TextAlignmentOptions.MidlineRight;
        gradeText.textWrappingMode   = TextWrappingModes.NoWrap;
        gradeText.overflowMode       = TextOverflowModes.Overflow;
        gradeText.color              = new Color(1f, 0.85f, 0.3f);

        // ── HP 바 (색은 GeneralPanelUI 가 비율에 따라 바꾼다) ──
        var hpBg = MakeImg(root, "HpBarBg", new Color(0.10f, 0.05f, 0.06f));
        SetTopStretch(hpBg.gameObject, textL, GpPad, hpY, barH);
        var hpFill = MakeFilledH(hpBg.gameObject, "HpFill", new Color(0.35f, 0.90f, 0.45f));
        var hpText = MakeBarText(hpBg.gameObject, "HpText", "100/100");

        // ── 병사 바 ───────────────────────────────────────────
        var solBg = MakeImg(root, "SoldierBarBg", new Color(0.05f, 0.07f, 0.13f));
        SetTopStretch(solBg.gameObject, textL, GpPad, solY, barH);
        var soldierFill = MakeFilledH(solBg.gameObject, "SoldierFill", StatColors.Soldier);
        var soldierText = MakeBarText(solBg.gameObject, "SoldierText", "5/5");

        // ── 버프 슬롯 (카드 위쪽 바깥, 초상화 머리 위부터 오른쪽으로) ──
        //  ⚠ 예전엔 28px 짜리 14칸(2행×7열)이 카드 안 맨 아래에 있었다.
        //    작아서 무엇이 걸렸는지 분간이 안 됐고, 카드 높이만 40 더 먹었다.
        //  카드 밖(배경 없는 전장 위)으로 올리면 카드가 그만큼 낮아지고
        //  아이콘도 전장 배경 위라 오히려 눈에 잘 띈다.
        const int BuffCount = 5;
        float buffSz   = buffH;
        float buffStep = buffSz + 6f;

        var buffSlots      = new Image[BuffCount];
        var buffStackTexts = new TextMeshProUGUI[BuffCount];

        for (int i = 0; i < BuffCount; i++)
        {
            var slotGo = MakeRect(root, $"BuffSlot{i}");
            SetTL(slotGo, innerL + i * buffStep, buffY, buffSz, buffSz);

            buffSlots[i] = slotGo.AddComponent<Image>();
            buffSlots[i].color          = Color.white;
            buffSlots[i].preserveAspect = true;

            var stackTxt = MakeTMP(slotGo, "StackCount", "2", (int)(UIScale.FontSm * 0.8f), FontStyle.Bold);
            stackTxt.color        = Color.yellow;
            stackTxt.alignment    = TextAlignmentOptions.BottomRight;
            stackTxt.outlineWidth = 0.3f;
            stackTxt.outlineColor = Color.black;
            Stretch(stackTxt.gameObject);
            stackTxt.gameObject.SetActive(false);
            buffStackTexts[i] = stackTxt;

            slotGo.SetActive(false);
        }

        // ── 전사 배지 (패널 전체 위에 겹침, 기본 숨김) ────────
        var deadBadge = MakeImg(root, "DeadBadge", new Color(0.06f, 0.02f, 0.03f, 0.72f));
        Stretch(deadBadge.gameObject);
        var deadTmp = MakeTMP(deadBadge.gameObject, "Label", "전 사", (int)UIScale.FontLg, FontStyle.Bold);
        Stretch(deadTmp.gameObject);
        deadTmp.color        = new Color(0.92f, 0.30f, 0.30f);
        deadTmp.alignment    = TextAlignmentOptions.Center;
        deadTmp.outlineWidth = 0.25f;
        deadTmp.outlineColor = Color.black;
        deadBadge.gameObject.SetActive(false);

        // ── SerializedField 연결 ──────────────────────────────
        var pso = new SerializedObject(panelUI);
        pso.FindProperty("_gradeBorder").objectReferenceValue  = gradeBorder;
        pso.FindProperty("_portraitBg").objectReferenceValue   = portraitBg;
        pso.FindProperty("_portraitIcon").objectReferenceValue = portraitIcon;
        pso.FindProperty("_nameText").objectReferenceValue     = nameText;
        pso.FindProperty("_jobChipText").objectReferenceValue  = jobChipText;
        pso.FindProperty("_gradeText").objectReferenceValue    = gradeText;
        pso.FindProperty("_deadBadge").objectReferenceValue    = deadBadge.gameObject;
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
        //  ⚠ 모든 글자는 UIScale 상수를 쓴다. 예전엔 11~24px 을 직접 박아
        //    실기에서 읽히지 않았다 (UI 규칙 4).
        //
        //  ⚠ 높이 예산
        //    가로 화면(1920×1080 참조단위)에서 세로 여유가 1080 뿐이다.
        //    상단 78 + 하단 203 = 281 (26%). 예전 180+340=520 은 화면의 48% 를 먹어
        //    전투가 거의 안 보였다. 늘릴 때는 이 합계를 먼저 계산할 것.
        //
        //  정보는 한 줄에 몰아넣는다: [웨이브] [처치] [타이머] ......... [배속][퍼즈]
        const float TopBarH  = 78f;
        const float CornerSz = 66f;    // 손가락으로 누르기엔 작지만 66×66 은 실기 최소선
        const float CornerY  = -6f;
        const float InfoY    = -8f;

        var topBarGo = MakeRect(hudGo, "TopBar");
        var topBarRT = topBarGo.GetComponent<RectTransform>();
        topBarRT.anchorMin        = new Vector2(0f, 1f);
        topBarRT.anchorMax        = new Vector2(1f, 1f);
        topBarRT.pivot            = new Vector2(0.5f, 1f);
        topBarRT.anchoredPosition = Vector2.zero;
        topBarRT.sizeDelta        = new Vector2(0f, TopBarH);

        var topBarBg = topBarGo.AddComponent<Image>();
        topBarBg.color = new Color(0.05f, 0.055f, 0.10f, 0.92f);

        // 하단 강조선 — 로비 팝업 헤더와 같은 언어
        var topAccent = MakeImg(topBarGo, "AccentLine", new Color(0.40f, 0.72f, 1.00f, 0.75f));
        SetRT(topAccent, new Vector2(0f, 0f), new Vector2(1f, 0f),
              new Vector2(0f, 0f), new Vector2(0f, 3f));

        var topBar = topBarGo.AddComponent<TopBarUI>();

        // 웨이브 · 처치 · 타이머 — 한 줄에 나란히
        var waveText = MakeTMP(topBarGo, "WaveText", "웨이브 1 / 10", (int)UIScale.FontMd, FontStyle.Bold);
        SetRT(waveText.gameObject,
              new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(20f, InfoY), new Vector2(330f, UIScale.RowMd));
        waveText.alignment = TextAlignmentOptions.MidlineLeft;

        var killCountText = MakeTMP(topBarGo, "KillCountText", "처치 0", (int)UIScale.FontSm, FontStyle.Normal);
        SetRT(killCountText.gameObject,
              new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(360f, InfoY), new Vector2(200f, UIScale.RowMd));
        killCountText.alignment = TextAlignmentOptions.MidlineLeft;
        killCountText.color     = new Color(0.70f, 0.76f, 0.90f);

        var waveTimerText = MakeTMP(topBarGo, "WaveTimerText", "0s", (int)UIScale.FontSm, FontStyle.Normal);
        SetRT(waveTimerText.gameObject,
              new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(570f, InfoY), new Vector2(170f, UIScale.RowMd));
        waveTimerText.alignment = TextAlignmentOptions.MidlineLeft;
        waveTimerText.color     = new Color(0.70f, 0.76f, 0.90f);

        // Wave 진행 바 (상단바 최하단 전체 폭) — 강조선 위에 얹는다
        var waveBarBg = MakeImg(topBarGo, "WaveProgressBg", new Color(0.14f, 0.15f, 0.22f));
        SetRT(waveBarBg, new Vector2(0f, 0f), new Vector2(1f, 0f),
              new Vector2(0f, 3f), new Vector2(0f, 7f));
        var waveProgressFill = MakeFilledH(waveBarBg.gameObject, "WaveProgressFill",
                                           new Color(0.40f, 0.72f, 1.00f));

        // 보스 HP — 상단바 **아래**로 내렸다. 상단바 안에 있으면
        // 웨이브 진행바와 겹쳐 어느 쪽 바인지 읽히지 않았다.
        var bossHpRoot = MakeRect(hudGo, "BossHpRoot");
        SetRT(bossHpRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -(TopBarH + 10f)), new Vector2(820f, UIScale.RowSm + 8f));
        var bossHpBg = bossHpRoot.AddComponent<Image>();
        bossHpBg.color = new Color(0.14f, 0.04f, 0.05f, 0.94f);
        var bossHpFill = MakeFilledH(bossHpRoot, "BossHpFill", new Color(0.90f, 0.20f, 0.18f));
        var bossHpText = MakeTMP(bossHpRoot, "BossHpText", "보스  1000 / 1000",
                                 (int)UIScale.FontSm, FontStyle.Bold);
        Stretch(bossHpText.gameObject);
        bossHpText.alignment    = TextAlignmentOptions.Center;
        bossHpText.outlineWidth = 0.22f;
        bossHpText.outlineColor = Color.black;
        bossHpRoot.SetActive(false);

        // ── 우상단 코너 버튼: [배속 토글] [일시 정지] ─────────
        //  배속은 버튼 하나짜리 토글이다 (1× → 2× → 3×).
        //  UI 규칙 1 — 누를 수 있는 버튼은 음각(RaisedBtn).
        var pauseBtn = EditorUIBuilder.RaisedBtn(topBarGo, "PauseButton",
                                                 new Color(0.30f, 0.16f, 0.18f), out var pauseBody);
        SetCorner(pauseBtn.gameObject, -12f, CornerY, CornerSz);
        // "||" 대신 세로 막대 2개 — Bar 는 (length=가로, thickness=세로) 라
        // 세로 막대는 length 를 얇게, thickness 를 길게 준다.
        EditorUIBuilder.Bar(pauseBody, "BarL", 9f, 28f, 0f, new Vector2(-9f, 0f), Color.white);
        EditorUIBuilder.Bar(pauseBody, "BarR", 9f, 28f, 0f, new Vector2( 9f, 0f), Color.white);

        var speedBtn = EditorUIBuilder.RaisedBtn(topBarGo, "SpeedButton",
                                                 new Color(0.18f, 0.30f, 0.46f), out var speedBody);
        SetCorner(speedBtn.gameObject, -12f - CornerSz - 10f, CornerY, CornerSz);
        var speedLabel = MakeTMP(speedBody, "Label", "1×", (int)UIScale.FontSm, FontStyle.Bold);
        Stretch(speedLabel.gameObject);
        speedLabel.alignment     = TextAlignmentOptions.Center;
        speedLabel.color         = Color.white;
        speedLabel.raycastTarget = false;

        // 배속 표시용 색 띠 — 버튼 본체(targetGraphic)를 직접 물들이지 않는다.
        // ⚠ Button.colors 는 targetGraphic 색에 **곱해지고**, 눌림/강조 색은
        //   생성 시점의 face 색 기준으로 역산돼 있다 (EditorUIBuilder.TintFor).
        //   본체 색을 런타임에 바꾸면 그 역산이 어긋나 눌린 색이 이상해진다.
        var speedAccent = MakeImg(speedBody, "SpeedAccent", new Color(0.18f, 0.30f, 0.46f));
        {
            var rt = speedAccent.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 3f);
            rt.sizeDelta        = new Vector2(-14f, 6f);
        }
        speedAccent.raycastTarget = false;

        // TopBarUI 필드 연결
        var tso = new SerializedObject(topBar);
        tso.FindProperty("_waveText").objectReferenceValue         = waveText;
        tso.FindProperty("_waveProgressFill").objectReferenceValue = waveProgressFill;
        tso.FindProperty("_waveTimerText").objectReferenceValue    = waveTimerText;
        tso.FindProperty("_bossHpRoot").objectReferenceValue       = bossHpRoot;
        tso.FindProperty("_bossHpFill").objectReferenceValue       = bossHpFill;
        tso.FindProperty("_bossHpText").objectReferenceValue       = bossHpText;
        tso.FindProperty("_killCountText").objectReferenceValue    = killCountText;
        tso.FindProperty("_speedButton").objectReferenceValue      = speedBtn;
        tso.FindProperty("_speedLabel").objectReferenceValue       = speedLabel;
        tso.FindProperty("_speedFace").objectReferenceValue        = speedAccent;
        tso.FindProperty("_pauseButton").objectReferenceValue      = pauseBtn;

        tso.ApplyModifiedPropertiesWithoutUndo();

        // ── GeneralPanelContainer ──────────────────────────────
        //  화면 맨 아래에 딱 붙인다 (anchoredPosition.y = 0) — 전투 화면을 최대한 남긴다.
        //  폭은 **항상 5칸 기준 고정**이다 — 인원수에 따라 카드 크기가 바뀌지 않는다.
        //  실제 폭 지정은 InGameHUD.ApplyFixedSlotWidth() 가 카드마다 LayoutElement 로 넣는다
        //  (컨테이너 폭은 캔버스에 따라 달라지므로 런타임에서만 알 수 있다).
        //  ⚠ childForceExpandWidth 를 켜면 그 지정이 무시되고 다시 인원수 균등 분배가 된다.
        //  ⚠ 카드 내부가 SetTopStretch 로 잡혀 있어야 지정된 폭을 실제로 채운다.
        //  ⚠ 버프 아이콘은 카드 **위쪽 바깥**에 뜬다 — 컨테이너 높이에 넣지 않는다.
        //    넣으면 그만큼 카드가 다시 올라와 버프를 밖으로 뺀 의미가 없어진다.
        const float ContainerH = 155f + 8f;   // 카드 높이 + 위 여백
        var container = MakeRect(hudGo, "GeneralPanelContainer");
        var contRT    = container.GetComponent<RectTransform>();
        contRT.anchorMin        = new Vector2(0f, 0f);
        contRT.anchorMax        = new Vector2(1f, 0f);
        contRT.pivot            = new Vector2(0.5f, 0f);
        contRT.anchoredPosition = Vector2.zero;
        contRT.sizeDelta        = new Vector2(0f, ContainerH);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6;
        hlg.padding              = new RectOffset(8, 8, 8, 0);
        hlg.childAlignment       = TextAnchor.LowerLeft;   // 슬롯 위치가 늘 같아야 한다
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;               // 5칸 고정 폭 (위 주석 참고)
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

    /// <summary>
    /// 바 안에 겹쳐 놓는 수치 텍스트.
    /// 채워진 부분·빈 부분 어디에 걸쳐도 읽히도록 검은 외곽선을 두른다.
    /// </summary>
    /// <summary>
    /// 좌우로 늘어나는 배치 — 부모 폭이 바뀌면 같이 늘어난다.
    /// left/right 는 부모 안쪽 여백, yFromTop 은 위에서부터의 거리.
    ///
    /// ⚠ 장군 카드는 반드시 이걸 쓴다. SetTL(고정 폭)로 두면
    ///   레이아웃 그룹이 패널을 넓혔을 때 오른쪽이 통째로 빈다.
    /// </summary>
    static void SetTopStretch(GameObject go, float left, float right, float yFromTop, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left,   -(yFromTop + h));
        rt.offsetMax = new Vector2(-right, -yFromTop);
    }

    /// <summary>상단바 우측 코너 정사각형 버튼 배치 (x 는 우측 가장자리 기준 음수).</summary>
    static void SetCorner(GameObject go, float x, float y, float size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(size, size);
    }

    static TextMeshProUGUI MakeBarText(GameObject barBg, string name, string text)
    {
        var tmp = MakeTMP(barBg, name, text, (int)UIScale.FontSm, FontStyle.Bold);
        Stretch(tmp.gameObject);
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.color              = Color.white;
        tmp.textWrappingMode   = TextWrappingModes.NoWrap;
        tmp.overflowMode       = TextOverflowModes.Overflow;
        tmp.enableAutoSizing   = true;
        tmp.fontSizeMin        = UIScale.FontSm * 0.72f;
        tmp.fontSizeMax        = UIScale.FontSm;
        tmp.outlineWidth       = 0.22f;
        tmp.outlineColor       = Color.black;
        tmp.raycastTarget      = false;
        return tmp;
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
