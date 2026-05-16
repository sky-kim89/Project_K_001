using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercenaryPopupCreator.cs
//  Tools > Project K > Popup > Create Mercenary Popups
//
//  생성 대상:
//    MercenaryShopPopup.prefab  (용병 상점 — 전체 화면 오버레이)
//    MercCandidateCard.prefab   (후보 카드 1장)
//
//  전체 화면 레이아웃:
//    SlotFullView  — 좌측, BattlePanel DeployArea와 동일 위치·구조
//    CandidatePanel— 우측 플로팅 패널 (1300×1120)
//
//  저장 경로: Assets/_project/2.Prefabs/UI/
// ============================================================

public static class MercenaryPopupCreator
{
    const string SaveDir = "Assets/_project/2.Prefabs/UI";

    // ── BattlePanel.DeployArea와 동일한 레이아웃 상수 ────────
    const float TopBarH  = 180f;
    const float NavBarH  = 160f;
    const float DeployW  = 420f;
    const int   SlotCount = 5;
    const float SlotGap  = 8f;

    // ── 색상 ─────────────────────────────────────────────────

    static readonly Color BgColor        = new Color(0.08f, 0.10f, 0.16f, 0.97f);
    static readonly Color BgDark         = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color SlotBgEmpty    = new Color(0.12f, 0.12f, 0.20f, 1f);
    static readonly Color DividerColor   = new Color(0.25f, 0.25f, 0.35f, 0.80f);
    static readonly Color MutedText      = new Color(0.60f, 0.60f, 0.65f);
    static Color HpColor      => StatColors.Hp;
    static Color AtkColor     => StatColors.Atk;
    static Color DefColor     => StatColors.Def;
    static Color SoldierColor => StatColors.Soldier;
    static readonly Color FireBtnColor   = new Color(0.70f, 0.20f, 0.18f, 1f);
    static readonly Color HireBtnColor   = new Color(0.15f, 0.65f, 0.40f, 1f);
    static readonly Color PassBtnColor   = new Color(0.35f, 0.35f, 0.42f, 1f);
    static readonly Color CloseBtnColor  = new Color(0.55f, 0.18f, 0.18f, 1f);
    static readonly Color CardBg         = new Color(0.11f, 0.13f, 0.22f, 1f);
    static readonly Color PortraitBg     = new Color(0.14f, 0.14f, 0.22f, 1f);
    static readonly Color PassiveNameColor = new Color(0.90f, 0.85f, 1.00f);
    static readonly Color PassiveDescColor = new Color(0.65f, 0.63f, 0.80f);
    static readonly Color ActiveNameColor  = new Color(1.00f, 0.95f, 0.75f);

    // ── 메뉴 ─────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create Mercenary Popups")]
    static void CreateAll()
    {
        CreateMercCandidateCard();
        CreateMercenaryShopPopup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MercenaryPopupCreator] 팝업 프리팹 생성 완료");
    }

    // ══════════════════════════════════════════════════════════
    //  MercCandidateCard 프리팹 (380 × 840)
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/Popup/Create MercCandidateCard Prefab")]
    public static void CreateMercCandidateCard()
    {
        const float CW = 380f, CH = 840f;

        var root = new GameObject("MercCandidateCard", typeof(RectTransform), typeof(Image));
        root.AddComponent<MercCandidateCardUI>();
        root.GetComponent<Image>().color = CardBg;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(CW, CH);

        // 등급 상단 바
        var topBar = AddImg(root, "GradeTopBar", new Color(0.3f, 0.3f, 0.4f));
        SetTopStretch(topBar.GetComponent<RectTransform>(), 8f);

        // 초상화
        const float portSz = 120f;
        var portBg = AddImg(root, "PortraitBg", PortraitBg);
        SetTopCenter(portBg.GetComponent<RectTransform>(), -14f, portSz, portSz);
        var portImg = AddImg(portBg, "PortraitImage", Color.white);
        FullStretch(portImg.gameObject);
        var portBridge = portBg.gameObject.AddComponent<UnitAppearanceBridge>();

        // 이름
        var nameText = AddCardTMP(root, "NameText", "이름", UIScale.FontLg, FontStyles.Bold, -148f, CW - 16f, 48f);

        // 등급(좌) / 직업(우)
        var gradeText = AddHalfTMP(root, "GradeText", "일반", UIScale.FontMd, FontStyles.Bold, -202f, CW, isLeft: true);
        gradeText.color = new Color(0.80f, 0.80f, 0.90f);
        var jobText = AddHalfTMP(root, "JobText", "직업", UIScale.FontMd, FontStyles.Normal, -202f, CW, isLeft: false);
        jobText.color = MutedText;
        jobText.alignment = TextAlignmentOptions.Right;

        AddCardDivider(root, -246f, CW);

        // 스탯 행 1: 체력(좌) / 공격(우)
        var hpText  = AddStatHalf(root, "HpText",  "체력 0",  HpColor,     -260f, CW, isLeft: true);
        var atkText = AddStatHalf(root, "AtkText",  "공격 0",  AtkColor,    -260f, CW, isLeft: false);

        // 스탯 행 2: 방어(좌) / 용병(우)
        var defText     = AddStatHalf(root, "DefText",     "방어 0%",  DefColor,    -318f, CW, isLeft: true);
        var soldierText = AddStatHalf(root, "SoldierText", "용병 0명", SoldierColor,-318f, CW, isLeft: false);

        AddCardDivider(root, -376f, CW);

        // 액티브 스킬 행: 아이콘(72×72) + 이름
        const float activeIconSz = 72f;
        const float activeRowY   = -392f;

        var activeIcon = AddImg(root, "ActiveSkillIcon", new Color(0.25f, 0.30f, 0.48f));
        var aiRt = activeIcon.GetComponent<RectTransform>();
        aiRt.anchorMin = aiRt.anchorMax = new Vector2(0f, 1f);
        aiRt.pivot = new Vector2(0f, 1f);
        aiRt.anchoredPosition = new Vector2(14f, activeRowY);
        aiRt.sizeDelta = new Vector2(activeIconSz, activeIconSz);

        var activeNameText = AddTMP(root, "ActiveSkillNameText", "스킬명", UIScale.FontMd, FontStyles.Bold);
        activeNameText.color = ActiveNameColor;
        activeNameText.alignment = TextAlignmentOptions.Left;
        var anRt = activeNameText.rectTransform;
        anRt.anchorMin = anRt.anchorMax = new Vector2(0f, 1f);
        anRt.pivot = new Vector2(0f, 0.5f);
        anRt.anchoredPosition = new Vector2(14f + activeIconSz + 10f, activeRowY - activeIconSz * 0.5f);
        anRt.sizeDelta = new Vector2(CW - 14f - activeIconSz - 10f - 14f, activeIconSz);

        AddCardDivider(root, -472f, CW);

        // 패시브 스킬 (이름 + 설명)
        var passive0     = AddPassiveNameTMP(root, "Passive0Text",     -486f, CW);
        var passive0Desc = AddPassiveDescTMP(root, "Passive0DescText", -526f, CW);
        var passive1     = AddPassiveNameTMP(root, "Passive1Text",     -562f, CW);
        var passive1Desc = AddPassiveDescTMP(root, "Passive1DescText", -602f, CW);
        var passive2     = AddPassiveNameTMP(root, "Passive2Text",     -638f, CW);
        var passive2Desc = AddPassiveDescTMP(root, "Passive2DescText", -678f, CW);

        // 선택 버튼
        var selBtn = AddBtn(root, "SelectBtn", "자세히 보기", new Color(0.22f, 0.45f, 0.72f), UIScale.FontMd);
        var sbRt = selBtn.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0f); sbRt.anchorMax = new Vector2(1f, 0f);
        sbRt.pivot = new Vector2(0.5f, 0f);
        sbRt.anchoredPosition = new Vector2(0f, 12f);
        sbRt.sizeDelta = new Vector2(-16f, UIScale.BtnSm);

        // 선택 하이라이트 테두리 (전체 덮는 Image — 기본 투명, SetHighlighted()로 제어)
        var highlightBorder = AddImg(root, "HighlightBorder", new Color(0.80f, 0.75f, 1.00f, 0.00f));
        highlightBorder.raycastTarget = false;
        FullStretch(highlightBorder.gameObject);

        // 직렬화 필드 연결
        var so = new SerializedObject(root.GetComponent<MercCandidateCardUI>());
        SetObj(so, "_gradeBorder",       topBar.GetComponent<Image>());
        SetObj(so, "_portraitBg",        portBg.GetComponent<Image>());
        SetObj(so, "_portraitImg",       portImg.GetComponent<Image>());
        SetObj(so, "_portraitBridge",    portBridge);
        SetObj(so, "_nameText",          nameText);
        SetObj(so, "_gradeText",         gradeText);
        SetObj(so, "_jobText",           jobText);
        SetObj(so, "_hpText",            hpText);
        SetObj(so, "_atkText",           atkText);
        SetObj(so, "_defText",           defText);
        SetObj(so, "_soldierText",       soldierText);
        SetObj(so, "_activeSkillIcon",   activeIcon.GetComponent<Image>());
        SetObj(so, "_activeSkillNameText", activeNameText);
        SetObj(so, "_passive0Text",      passive0);
        SetObj(so, "_passive0DescText",  passive0Desc);
        SetObj(so, "_passive1Text",      passive1);
        SetObj(so, "_passive1DescText",  passive1Desc);
        SetObj(so, "_passive2Text",      passive2);
        SetObj(so, "_passive2DescText",  passive2Desc);
        SetObj(so, "_button",            selBtn.GetComponent<Button>());
        SetObj(so, "_highlightBorder",   highlightBorder);
        so.ApplyModifiedProperties();

        Save(root, "MercCandidateCard");
    }

    // ══════════════════════════════════════════════════════════
    //  MercenaryShopPopup — 전체 화면 오버레이
    //
    //  SlotFullView : BattlePanel DeployArea와 완전히 동일한 위치·구조
    //  CandidatePanel : 우측 플로팅 패널 (1300 × 1120)
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/Popup/Create MercenaryShop Popup")]
    public static void CreateMercenaryShopPopup()
    {
        const float PW       = 1300f;
        const float PH       = 1120f;
        const float HeaderH  = 100f;
        const float HireBarH = 140f;

        // ── 루트 — 전체 화면 스트레치 ─────────────────────────
        var root  = MakeFullScreenRoot<MercenaryShopPopup>("MercenaryShopPopup");
        var popup = root.GetComponent<MercenaryShopPopup>();

        // FadeOnly 애니메이션 (전체화면 팝업에 FadeScale은 어색)
        var animSo = new SerializedObject(popup);
        SetEnum(animSo, "_openAnimation",  (int)PopupAnimation.FadeOnly);
        SetEnum(animSo, "_closeAnimation", (int)PopupAnimation.FadeOnly);
        animSo.ApplyModifiedProperties();

        // ── InputBlocker — 배경 클릭 차단 (로비 UI 보호) ──────
        var blocker = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image));
        blocker.transform.SetParent(root.transform, false);
        FullStretch(blocker);
        var blockerImg = blocker.GetComponent<Image>();
        blockerImg.color         = new Color(0f, 0f, 0f, 0f);
        blockerImg.raycastTarget = true;

        // ══════════════════════════════════════════════════════
        //  SlotFullView — BattlePanel DeployArea와 동일 위치
        //  anchorMin=(0,0) anchorMax=(0,1)
        //  offsetMin=(0, NavBarH) offsetMax=(DeployW, -TopBarH)
        // ══════════════════════════════════════════════════════

        var slotFullView = new GameObject("SlotFullView", typeof(RectTransform));
        slotFullView.transform.SetParent(root.transform, false);
        var sfvRt = slotFullView.GetComponent<RectTransform>();
        sfvRt.anchorMin = new Vector2(0f, 0f);
        sfvRt.anchorMax = new Vector2(0f, 1f);
        sfvRt.offsetMin = new Vector2(0f,      0f);
        sfvRt.offsetMax = new Vector2(DeployW, -TopBarH);

        var sfvImg = slotFullView.AddComponent<Image>();
        sfvImg.color = BgDark;

        var sfvVlg = slotFullView.AddComponent<VerticalLayoutGroup>();
        sfvVlg.childAlignment         = TextAnchor.UpperCenter;
        sfvVlg.spacing                = SlotGap;
        sfvVlg.childForceExpandWidth  = true;
        sfvVlg.childForceExpandHeight = true;
        sfvVlg.padding                = new RectOffset(4, 4, 4, 4);

        float slotH = (UIScale.RefHeight - TopBarH
                       - SlotGap * (SlotCount - 1) - 8f) / SlotCount;

        var slots = new MercSlotCardUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var slotGo = BuildMercSlot(slotFullView, i, slotH);
            slots[i] = slotGo.GetComponent<MercSlotCardUI>();
        }

        // ══════════════════════════════════════════════════════
        //  CandidatePanel — 우측 플로팅 패널
        //  화면 우측 [DeployW, RefWidth] 영역 중앙에 배치
        // ══════════════════════════════════════════════════════

        var candidatePanel = new GameObject("CandidatePanel", typeof(RectTransform));
        candidatePanel.transform.SetParent(root.transform, false);
        var cpRt = candidatePanel.GetComponent<RectTransform>();
        cpRt.anchorMin = cpRt.anchorMax = new Vector2(0.5f, 0.5f);
        cpRt.pivot     = new Vector2(0.5f, 0.5f);
        cpRt.anchoredPosition = new Vector2(300f, 0f);
        cpRt.sizeDelta        = new Vector2(PW, PH);

        var cpImg = candidatePanel.AddComponent<Image>();
        cpImg.color = BgColor;

        // ── CandidatePanel > Header ────────────────────────────
        var header = AddImg(candidatePanel, "Header", new Color(0.10f, 0.12f, 0.22f, 1f));
        var hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f); hRt.anchorMax = new Vector2(1f, 1f);
        hRt.offsetMin = new Vector2(0f, -HeaderH); hRt.offsetMax = Vector2.zero;

        var titleTmp = AddTMP(header.gameObject, "TitleText", "용병 상점", UIScale.FontXl, FontStyles.Bold);
        SetCenterRect(titleTmp.rectTransform, new Vector2(0f, 0f), new Vector2(PW - 40f, HeaderH - 10f));

        // ── CandidatePanel > CardArea ─────────────────────────
        var cardArea = new GameObject("CardArea", typeof(RectTransform));
        cardArea.transform.SetParent(candidatePanel.transform, false);
        var caRt = cardArea.GetComponent<RectTransform>();
        caRt.anchorMin = Vector2.zero; caRt.anchorMax = Vector2.one;
        caRt.offsetMin = new Vector2(0f, HireBarH);
        caRt.offsetMax = new Vector2(0f, -HeaderH);

        var hlg = cardArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 24f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = false; hlg.childControlHeight     = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SaveDir}/MercCandidateCard.prefab");
        var cards = new MercCandidateCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject cardGo;
            if (cardPrefab != null)
            {
                cardGo = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, cardArea.transform);
                cardGo.name = $"Card{i}";
            }
            else
            {
                cardGo = new GameObject($"Card{i}", typeof(RectTransform), typeof(Image));
                cardGo.transform.SetParent(cardArea.transform, false);
                cardGo.GetComponent<Image>().color = CardBg;
                cardGo.AddComponent<MercCandidateCardUI>();
                cardGo.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 840f);
            }
            cards[i] = cardGo.GetComponent<MercCandidateCardUI>();
        }

        // ── CandidatePanel > HireBar ──────────────────────────
        var hireBar = AddImg(candidatePanel, "HireBar", new Color(0.08f, 0.09f, 0.16f, 1f));
        var hbRt = hireBar.GetComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(0f, 0f); hbRt.anchorMax = new Vector2(1f, 0f);
        hbRt.offsetMin = Vector2.zero; hbRt.offsetMax = new Vector2(0f, HireBarH);

        // 고용 버튼
        var hireBtn = AddBtn(hireBar.gameObject, "HireBtn", "고용", HireBtnColor, UIScale.FontLg);
        var hbtnRt = hireBtn.GetComponent<RectTransform>();
        hbtnRt.anchorMin = new Vector2(1f, 0.5f); hbtnRt.anchorMax = new Vector2(1f, 0.5f);
        hbtnRt.pivot = new Vector2(1f, 0.5f);
        hbtnRt.anchoredPosition = new Vector2(-420f, 0f);
        hbtnRt.sizeDelta = new Vector2(220f, UIScale.BtnMd);

        // 분해 버튼 [분해][아이콘][+N]
        var passBtn = new GameObject("PassBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        passBtn.transform.SetParent(hireBar.gameObject.transform, false);
        passBtn.GetComponent<Image>().color = PassBtnColor;
        var pbtnRt = passBtn.GetComponent<RectTransform>();
        pbtnRt.anchorMin = new Vector2(1f, 0.5f); pbtnRt.anchorMax = new Vector2(1f, 0.5f);
        pbtnRt.pivot = new Vector2(1f, 0.5f);
        pbtnRt.anchoredPosition = new Vector2(-16f, 0f);
        pbtnRt.sizeDelta = new Vector2(384f, UIScale.BtnMd);

        var passHlg = passBtn.AddComponent<HorizontalLayoutGroup>();
        passHlg.childAlignment        = TextAnchor.MiddleCenter;
        passHlg.spacing               = 8f;
        passHlg.padding               = new RectOffset(16, 16, 0, 0);
        passHlg.childControlWidth     = false; passHlg.childControlHeight     = false;
        passHlg.childForceExpandWidth = false; passHlg.childForceExpandHeight = false;

        var passStaticGo = new GameObject("StaticLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        passStaticGo.transform.SetParent(passBtn.transform, false);
        var psTmp = passStaticGo.GetComponent<TextMeshProUGUI>();
        psTmp.text = "분해"; psTmp.fontSize = UIScale.FontLg;
        psTmp.fontStyle = FontStyles.Bold; psTmp.alignment = TextAlignmentOptions.Center;
        psTmp.color = Color.white;
        passStaticGo.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, UIScale.BtnMd);

        var passIconGo = new GameObject("ShardIcon", typeof(RectTransform), typeof(Image));
        passIconGo.transform.SetParent(passBtn.transform, false);
        var passShardIcon = passIconGo.GetComponent<Image>();
        passShardIcon.color = new Color(0.45f, 0.70f, 1.00f);
        passIconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(72f, 72f);

        var passCountGo = new GameObject("CountLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        passCountGo.transform.SetParent(passBtn.transform, false);
        var pcTmp = passCountGo.GetComponent<TextMeshProUGUI>();
        pcTmp.text = "+?"; pcTmp.fontSize = UIScale.FontLg;
        pcTmp.fontStyle = FontStyles.Bold; pcTmp.alignment = TextAlignmentOptions.Center;
        pcTmp.color = new Color(1f, 0.90f, 0.50f);
        passCountGo.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, UIScale.BtnMd);

        // ── 직렬화 필드 연결 ──────────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",      (int)PopupType.MercenaryShop);
        SetObj (so, "_candidateView",  candidatePanel);
        SetObj (so, "_slotFullView",   slotFullView);
        SetObj (so, "_hireBtn",        hireBtn.GetComponent<Button>());
        SetObj (so, "_passBtn",        passBtn.GetComponent<Button>());
        SetObj (so, "_passBtnLabel",   pcTmp);
        SetObj (so, "_passShardIcon",  passShardIcon);
        // _closeBtn 미연결 — 닫기 버튼 없음 (고용/분해 버튼으로만 닫힘)

        var cardsProp = so.FindProperty("_candidateCards");
        cardsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];

        var slotsProp = so.FindProperty("_slotCards");
        slotsProp.arraySize = SlotCount;
        for (int i = 0; i < SlotCount; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

        so.ApplyModifiedProperties();

        Save(root, "MercenaryShopPopup");
    }

    // ══════════════════════════════════════════════════════════
    //  BuildMercSlot — SlotFullView 내 슬롯 1개
    //  BattlePanelCreator.BuildSlot()과 동일한 카드 구조,
    //  DeploySlotUI 대신 MercSlotCardUI + FireBtn 추가.
    // ══════════════════════════════════════════════════════════

    static GameObject BuildMercSlot(GameObject parent, int index, float height)
    {
        var go = new GameObject($"Slot_{index}", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(DeployW, 160f);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = 110f;
        le.preferredHeight = height;

        var slotUi  = go.AddComponent<MercSlotCardUI>();
        var slotImg = go.AddComponent<Image>();
        slotImg.color = SlotBgEmpty;

        // ── EmptyGroup ────────────────────────────────────────
        var emptyGo = new GameObject("EmptyGroup", typeof(RectTransform));
        emptyGo.transform.SetParent(go.transform, false);
        FullStretch(emptyGo);

        var emptyLbl = AddTMP(emptyGo, "EmptyLabel", "빈 슬롯", UIScale.FontMd, FontStyles.Normal);
        emptyLbl.color     = MutedText;
        emptyLbl.alignment = TextAlignmentOptions.Center;
        FullStretch(emptyLbl.gameObject);

        // ── OccupiedGroup = HeroPanelCreator.BuildCardPrefab() ─
        var cardGo = HeroPanelCreator.BuildCardPrefab();
        cardGo.name = "OccupiedGroup";
        cardGo.transform.SetParent(go.transform, false);
        cardGo.SetActive(false);
        FullStretch(cardGo);

        var heroCardUi = cardGo.GetComponent<HeroCardUI>();
        if (heroCardUi != null) Object.DestroyImmediate(heroCardUi);
        var cardBtn = cardGo.GetComponent<Button>();
        if (cardBtn != null) Object.DestroyImmediate(cardBtn);

        // 카드 내부 요소는 raycast 차단 (해고 버튼은 카드 밖에 있으므로 영향 없음)
        foreach (var img in cardGo.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
        foreach (var tmp in cardGo.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.raycastTarget = false;


        // 해고 버튼 — 슬롯 루트의 직접 자식, 슬롯 우측(SlotFullView–CandidatePanel 사이 공간)에 배치
        var fireBtn = AddBtn(go, "FireBtn", "해고", FireBtnColor, UIScale.FontSm);
        var fbRt = fireBtn.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(1f, 0.5f);
        fbRt.anchorMax = new Vector2(1f, 0.5f);
        fbRt.pivot     = new Vector2(0f, 0.5f);
        fbRt.anchoredPosition = new Vector2(12f, 0f);  // 슬롯 우측 경계 + 12px 여백
        fbRt.sizeDelta = new Vector2(160f, 80f);

        // ── MercSlotCardUI 필드 연결 ──────────────────────────
        var so = new SerializedObject(slotUi);
        SetObj(so, "_emptyGroup",     emptyGo);
        SetObj(so, "_occupiedGroup",  cardGo);
        SetObj(so, "_gradeBorder",    FindChild<Image>(cardGo.transform, "GradeBorder"));
        SetObj(so, "_gradeBadge",     FindChild<Image>(cardGo.transform, "GradeBadge"));
        SetObj(so, "_portraitBg",     FindChild<Image>(cardGo.transform, "PortraitBg"));
        SetObj(so, "_portraitImg",    FindChild<Image>(cardGo.transform, "PortraitImage"));
        SetObj(so, "_portraitBridge", FindChild<UnitAppearanceBridge>(cardGo.transform, "PortraitPreview"));
        SetObj(so, "_nameText",       FindChild<TextMeshProUGUI>(cardGo.transform, "NameText"));
        SetObj(so, "_levelText",      FindChild<TextMeshProUGUI>(cardGo.transform, "LevelText"));
        SetObj(so, "_gradeText",      FindChild<TextMeshProUGUI>(cardGo.transform, "GradeText"));
        SetObj(so, "_jobText",        FindChild<TextMeshProUGUI>(cardGo.transform, "JobText"));
        SetObj(so, "_hpText",         FindChild<TextMeshProUGUI>(cardGo.transform, "HpText"));
        SetObj(so, "_atkText",        FindChild<TextMeshProUGUI>(cardGo.transform, "AtkText"));
        SetObj(so, "_defText",        FindChild<TextMeshProUGUI>(cardGo.transform, "DefText"));
        SetObj(so, "_soldierText",    FindChild<TextMeshProUGUI>(cardGo.transform, "SoldierText"));
        SetObj(so, "_fireButton",     fireBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        return go;
    }

    // ══════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════

    // 전체 화면 스트레치 루트 (MercenaryShopPopup 전용)
    static GameObject MakeFullScreenRoot<T>(string name) where T : PopupBase
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<CanvasGroup>();
        go.AddComponent<T>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    // 기존 팝업 루트 (고정 크기 — MercCandidateCard 등에 미사용이지만 보존)
    static GameObject MakeRoot<T>(string name, float w, float h) where T : PopupBase
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<CanvasGroup>();
        go.AddComponent<T>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    static Image AddImg(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<Image>();
    }

    static Image AddImg(Image parent, string name, Color color)
        => AddImg(parent.gameObject, name, color);

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return tmp;
    }

    // 카드 상단 앵커, 중앙 X
    static TextMeshProUGUI AddCardTMP(GameObject parent, string name, string text,
        float size, FontStyles style, float anchoredY, float width, float height)
    {
        var tmp = AddTMP(parent, name, text, size, style);
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta = new Vector2(width, height);
        return tmp;
    }

    // 좌/우 반쪽 배치
    static TextMeshProUGUI AddHalfTMP(GameObject parent, string name, string text,
        float size, FontStyles style, float anchoredY, float cardW, bool isLeft)
    {
        var tmp = AddTMP(parent, name, text, size, style);
        tmp.alignment = isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        var rt = tmp.rectTransform;
        if (isLeft)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, anchoredY);
            rt.sizeDelta = new Vector2(cardW / 2f - 20f, 36f);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, anchoredY);
            rt.sizeDelta = new Vector2(cardW / 2f - 20f, 36f);
        }
        return tmp;
    }

    // 스탯 텍스트 (좌/우 반쪽, FontMd, Bold)
    static TextMeshProUGUI AddStatHalf(GameObject parent, string name, string text,
        Color color, float anchoredY, float cardW, bool isLeft)
    {
        var tmp = AddTMP(parent, name, text, UIScale.FontMd, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = color;
        var rt = tmp.rectTransform;
        if (isLeft)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, anchoredY);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(8f, anchoredY);
        }
        rt.sizeDelta = new Vector2(cardW / 2f - 28f, 50f);
        return tmp;
    }

    static TextMeshProUGUI AddPassiveNameTMP(GameObject parent, string name, float anchoredY, float cardW)
    {
        var tmp = AddTMP(parent, name, "-", UIScale.FontMd, FontStyles.Bold);
        var rt  = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(16f, anchoredY);
        rt.sizeDelta = new Vector2(cardW - 32f, 38f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = PassiveNameColor;
        return tmp;
    }

    static TextMeshProUGUI AddPassiveDescTMP(GameObject parent, string name, float anchoredY, float cardW)
    {
        var tmp = AddTMP(parent, name, "", UIScale.FontSm, FontStyles.Normal);
        var rt  = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(16f, anchoredY);
        rt.sizeDelta = new Vector2(cardW - 32f, 32f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = PassiveDescColor;
        return tmp;
    }

    static void AddCardDivider(GameObject parent, float anchoredY, float cardW)
    {
        var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = DividerColor;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta = new Vector2(cardW - 32f, 2f);
    }

    static void SetTopStretch(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0f, height);
    }

    static void SetTopCenter(RectTransform rt, float anchoredY, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY);
        rt.sizeDelta = new Vector2(w, h);
    }

    static GameObject AddBtn(GameObject parent, string name, string label, Color bgColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;
        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return go;
    }

    static void SetCenterRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static T FindChild<T>(Transform root, string name) where T : Component
        => root.Find(name)?.GetComponent<T>();

    static void SetEnum(SerializedObject so, string field, int value)
    {
        var prop = so.FindProperty(field); if (prop != null) prop.intValue = value;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field); if (prop != null) prop.objectReferenceValue = obj;
    }

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SaveDir}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[MercenaryPopupCreator] 저장: {path}");
    }
}
