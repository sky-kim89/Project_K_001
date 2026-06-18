using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopPopupCreator.cs  [Editor Only]
//  Tools > Project K > Popup > Create RunShopPopup Prefab
//
//  Canvas 기준: 1920×1080 (Landscape)
//
//  ■ 전체화면 레이아웃 (총 ~850px):
//    TitleBar       80px
//    TopSection    474px  EquipBlock(818px 고정) | TraitBlock(480px 고정)
//    GenSection    300px  장수 5카드
// ============================================================

public static class RunShopPopupCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI/RunShopPopup.prefab";

    // ── 레이아웃 상수 ─────────────────────────────────────────
    const float TitleH       = 80f;
    const float SecTitleH    = 36f;
    // TopSection: EquipBlock(좌) | TraitBlock(우), 세로 꽉 참
    const float TopSectionH  = 694f;
    // 외각 여백 (panel VLG padding 좌우/상하)
    const int   OuterPadH    = 10;
    const int   OuterPadV    = 10;

    // 장비 슬롯: 너비 고정, 높이는 ContentSizeFitter 자동
    const float EquipSlotW   = 400f;
    const float EquipGridGap = 6f;
    const float EquipGridPad = 6f;
    // EquipBlock 고정폭 = 400×2 + 6 + 12 = 818px
    const float EquipBlockW  = EquipSlotW * 2 + EquipGridGap + EquipGridPad * 2;
    // TraitBlock 고정높이 = 36 + 2×200 + 6 + 12 = 454px (슬롯 콘텐츠에 딱 맞춤)
    const float TraitBlockH  = 454f;

    const float TraitBlockW  = 480f;
    // GenRowH: 카드(170) + 구분선(2) + 고용행(26) = 198px
    const float GenRowH      = 198f;

    // ── 색상 팔레트 ───────────────────────────────────────────
    static readonly Color PanelBg    = new Color(0.06f, 0.07f, 0.12f, 1f);
    static readonly Color TitleBg    = new Color(0.04f, 0.05f, 0.10f, 1f);
    static readonly Color EquipSecBg = new Color(0.08f, 0.09f, 0.16f, 1f);
    static readonly Color TraitSecBg = new Color(0.09f, 0.07f, 0.16f, 1f);
    static readonly Color GenSecBg   = new Color(0.06f, 0.07f, 0.13f, 1f);
    static readonly Color SlotEquip  = new Color(0.10f, 0.12f, 0.22f, 1f);
    static readonly Color SlotTrait  = new Color(0.12f, 0.09f, 0.20f, 1f);
    static readonly Color SlotGen    = new Color(0.08f, 0.10f, 0.18f, 1f);
    static readonly Color BuyBtnC    = new Color(0.10f, 0.65f, 0.52f, 1f);
    static readonly Color HireBtnC   = new Color(0.50f, 0.28f, 0.14f, 1f);
    static readonly Color RefBtnC    = new Color(0.38f, 0.22f, 0.60f, 1f);
    static readonly Color CloseBtnC  = new Color(0.50f, 0.12f, 0.12f, 1f);
    static readonly Color SoldOutC   = new Color(0f,    0f,    0f,    0.72f);
    static readonly Color MutedC     = new Color(0.55f, 0.57f, 0.70f);
    static readonly Color GoldC      = new Color(1f,    0.85f, 0.20f);
    static readonly Color StatC      = new Color(0.72f, 0.88f, 1.00f);
    static readonly Color DivC       = new Color(0.22f, 0.26f, 0.38f, 0.7f);
    static readonly Color GradeC     = new Color(0.70f, 0.82f, 0.55f);
    static readonly Color CardHover  = new Color(1f,    1f,    1f,    0.06f);

    [MenuItem("Tools/Project K/Popup/Create RunShopPopup Prefab")]
    public static void Create()
    {
        var root = new GameObject("RunShopPopup", typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = Color.clear;
        FullStretch(root.GetComponent<RectTransform>());
        var popup = root.AddComponent<RunShopPopup>();

        var panel = MakeGo("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        FullStretch(panel.GetComponent<RectTransform>());
        // 외각 여백: 화면 가장자리에서 10px 안쪽, 섹션 간 12px 간격
        SetVLG(panel, new RectOffset(OuterPadH, OuterPadH, OuterPadV, OuterPadV), 12f);

        // ── TitleBar ──────────────────────────────────────────
        var titleBar = MakeGo("TitleBar", panel);
        titleBar.AddComponent<Image>().color = TitleBg;
        AddLE(titleBar, 0f, TitleH);
        var titleTmp = MakeTMP(titleBar, "TitleText", "런 상점", UIScale.FontLg, FontStyles.Bold);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        SetRT(titleTmp.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 0f), new Vector2(-300f, 0f));

        var (refreshBtn, refreshCostTmp) = BuildRefreshBtn(titleBar);
        var closeBtn = BuildCloseBtn(titleBar);

        // ── TopSection: HLG 없이 RectTransform 직접 배치 ─────
        // EquipBlock은 좌측 고정, TraitBlock은 우측 정확한 높이 고정
        var topSection = MakeGo("TopSection", panel);
        AddLE(topSection, 0f, TopSectionH);
        // 자식은 RT로 직접 위치 지정 — 레이아웃 그룹 없음

        // ── 장비 블록: 좌측, 세로 100% ───────────────────────
        var equipBlock = MakeGo("EquipBlock", topSection);
        equipBlock.AddComponent<Image>().color = EquipSecBg;
        var equipRt = equipBlock.GetComponent<RectTransform>();
        equipRt.anchorMin = new Vector2(0f, 0f);
        equipRt.anchorMax = new Vector2(0f, 1f);
        equipRt.offsetMin = Vector2.zero;
        equipRt.offsetMax = new Vector2(EquipBlockW, 0f);  // 818px 폭, 세로 꽉 참
        SetVLG(equipBlock, new RectOffset(0, 0, 0, 0), 0f);

        var equipTitleGo = MakeGo("SectionTitle", equipBlock);
        AddLE(equipTitleGo, 0f, SecTitleH);
        var etmp = MakeTMP(equipTitleGo, "Text", "장비", UIScale.FontMd, FontStyles.Bold);
        etmp.color = MutedC; etmp.alignment = TextAlignmentOptions.MidlineLeft;
        SetRT(etmp.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), Vector2.zero);

        // 장비 그리드: VLG(패딩·간격) → 2 HLG rows, 슬롯 고정 사이즈
        var equipGrid = MakeGo("EquipGrid", equipBlock);
        equipGrid.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var egVlg = equipGrid.AddComponent<VerticalLayoutGroup>();
        egVlg.childControlWidth = egVlg.childControlHeight = true;
        egVlg.childForceExpandWidth = false;
        egVlg.childForceExpandHeight = false;
        egVlg.childAlignment = TextAnchor.UpperLeft;
        egVlg.spacing = EquipGridGap;
        egVlg.padding = new RectOffset((int)EquipGridPad, (int)EquipGridPad, (int)EquipGridPad, (int)EquipGridPad);

        var eRow1 = MakeGo("EquipRow1", equipGrid);
        SetHLGFixed(eRow1, EquipGridGap);
        var eRow2 = MakeGo("EquipRow2", equipGrid);
        SetHLGFixed(eRow2, EquipGridGap);

        var equipSlots = new RunShopEquipSlot[4];
        equipSlots[0] = BuildEquipSlot(eRow1, "EquipSlot_0");
        equipSlots[1] = BuildEquipSlot(eRow1, "EquipSlot_1");
        equipSlots[2] = BuildEquipSlot(eRow2, "EquipSlot_2");
        equipSlots[3] = BuildEquipSlot(eRow2, "EquipSlot_3");

        // ── 특성 블록: EquipBlock 우측, 정확히 454px 높이 (콘텐츠에 딱 맞춤) ─
        var traitBlock = MakeGo("TraitBlock", topSection);
        traitBlock.AddComponent<Image>().color = TraitSecBg;
        var traitRt = traitBlock.GetComponent<RectTransform>();
        traitRt.anchorMin        = new Vector2(0f, 1f);
        traitRt.anchorMax        = new Vector2(0f, 1f);
        traitRt.pivot            = new Vector2(0f, 1f);
        traitRt.anchoredPosition = new Vector2(EquipBlockW + 12f, 0f);  // 바로 옆, 상단 정렬
        traitRt.sizeDelta        = new Vector2(TraitBlockW, TraitBlockH); // 480×454px 고정
        SetVLG(traitBlock, new RectOffset(0, 0, 0, 0), 0f);

        var traitTitleGo = MakeGo("SectionTitle", traitBlock);
        AddLE(traitTitleGo, 0f, SecTitleH);
        var ttmp = MakeTMP(traitTitleGo, "Text", "특성", UIScale.FontMd, FontStyles.Bold);
        ttmp.color = MutedC; ttmp.alignment = TextAlignmentOptions.MidlineLeft;
        SetRT(ttmp.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), Vector2.zero);

        var traitArea = MakeGo("TraitArea", traitBlock);
        traitArea.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var taVlg = traitArea.AddComponent<VerticalLayoutGroup>();
        taVlg.childControlWidth = taVlg.childControlHeight = true;
        taVlg.childForceExpandWidth = true;
        taVlg.childForceExpandHeight = false;  // 슬롯 preferredHeight 존중 (배경 팽창 방지)
        taVlg.childAlignment = TextAnchor.UpperCenter;
        taVlg.spacing = 6f; taVlg.padding = new RectOffset(6, 6, 6, 6);

        var traitSlots = new RunShopTraitSlot[2];
        traitSlots[0] = BuildTraitSlot(traitArea, "TraitSlot_0");
        traitSlots[1] = BuildTraitSlot(traitArea, "TraitSlot_1");

        // ── 용병 섹션 ─────────────────────────────────────────
        var genSection = MakeGo("GeneralSection", panel);
        genSection.AddComponent<Image>().color = GenSecBg;
        AddLE(genSection, 0f, SecTitleH + GenRowH);
        SetVLG(genSection, new RectOffset(0, 0, 0, 0), 0f);

        var genTitleGo = MakeGo("SectionTitle", genSection);
        AddLE(genTitleGo, 0f, SecTitleH);
        var gtmp = MakeTMP(genTitleGo, "Text", "용병 고용", UIScale.FontMd, FontStyles.Bold);
        gtmp.color = MutedC; gtmp.alignment = TextAlignmentOptions.MidlineLeft; gtmp.raycastTarget = false;
        SetRT(gtmp.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), Vector2.zero);

        var genRow = MakeGo("Row", genSection);
        AddLE(genRow, 0f, GenRowH);
        var genHlg = genRow.AddComponent<HorizontalLayoutGroup>();
        genHlg.childAlignment = TextAnchor.MiddleCenter;
        genHlg.spacing = 8f; genHlg.padding = new RectOffset(10, 10, 6, 6);
        genHlg.childControlWidth = true; genHlg.childControlHeight = true;
        genHlg.childForceExpandWidth = true; genHlg.childForceExpandHeight = true;

        var genSlots = new RunShopGeneralSlot[5];
        for (int i = 0; i < 5; i++)
            genSlots[i] = BuildGeneralSlot(genRow, $"GeneralSlot_{i}");

        // ── 필드 연결 ─────────────────────────────────────────
        var so = new SerializedObject(popup);
        so.Update();
        so.FindProperty("_popupType").intValue = (int)PopupType.RunShop;
        SetArr(so, "_equipSlots",   4, i => equipSlots[i]);
        SetArr(so, "_traitSlots",   2, i => traitSlots[i]);
        SetArr(so, "_generalSlots", 5, i => genSlots[i]);
        SetObj(so, "_refreshBtn",      refreshBtn);
        SetObj(so, "_refreshCostText", refreshCostTmp);
        SetObj(so, "_closeBtn",        closeBtn);
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, SavePath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RunShopPopupCreator] 완료. PopupManager → Load Popup Prefabs 실행 필요.");
    }

    // ═══════════════════════════════════════════════════════
    //  장비 슬롯 — 고정 400×210, 아이콘+이름+등급/스텟/우측하단 구매버튼
    // ═══════════════════════════════════════════════════════

    static RunShopEquipSlot BuildEquipSlot(GameObject parent, string name)
    {
        var go = MakeGo(name, parent, typeof(Image));
        go.GetComponent<Image>().color = SlotEquip;
        // 너비 고정, 높이는 콘텐츠에 따라 자동 (CSF)
        go.AddComponent<LayoutElement>().preferredWidth = EquipSlotW;
        var csfSlot = go.AddComponent<ContentSizeFitter>();
        csfSlot.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csfSlot.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        var slot = go.AddComponent<RunShopEquipSlot>();
        SetVLG(go, new RectOffset(8, 8, 8, 8), 5f);

        // ── 헤더: 아이콘 + 이름/등급 ─────────────────────────
        var header = MakeGo("Header", go);
        AddLE(header, 0f, 70f);
        var hhlg = header.AddComponent<HorizontalLayoutGroup>();
        hhlg.childAlignment = TextAnchor.MiddleLeft;
        hhlg.spacing = 8f; hhlg.padding = new RectOffset(0, 0, 0, 0);
        hhlg.childControlWidth = true; hhlg.childControlHeight = true;
        hhlg.childForceExpandWidth = false; hhlg.childForceExpandHeight = true;

        // 아이콘 (70×70 고정)
        var iconBg = MakeGo("IconBg", header, typeof(Image));
        iconBg.GetComponent<Image>().color = new Color(0.07f, 0.08f, 0.16f);
        iconBg.AddComponent<LayoutElement>().preferredWidth = 70f;
        var iconImg = MakeGo("Icon", iconBg, typeof(Image)).GetComponent<Image>();
        iconImg.preserveAspect = true; iconImg.raycastTarget = false;
        iconImg.color = new Color(0.30f, 0.32f, 0.44f);
        SetRT(iconImg.rectTransform, new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.93f));

        // 이름 + 등급 (세로)
        var nameCol = MakeGo("NameCol", header);
        nameCol.AddComponent<LayoutElement>().flexibleWidth = 1f;
        SetVLG(nameCol, new RectOffset(0, 0, 2, 2), 3f);

        var nameTmp = MakeTMP(nameCol, "NameText", "장비 이름", UIScale.FontMd, FontStyles.Bold);
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.raycastTarget = false;
        nameTmp.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        // 등급 뱃지 행 (좌측 등급 텍스트)
        var gradeTmp = MakeTMP(nameCol, "GradeText", "일반", UIScale.FontSm, FontStyles.Bold);
        gradeTmp.color = GradeC; gradeTmp.alignment = TextAlignmentOptions.Left;
        gradeTmp.raycastTarget = false;
        AddLE(gradeTmp.gameObject, 0f, 20f);

        // ── 구분선 ────────────────────────────────────────────
        AddDivider(go);

        // ── 스텟 텍스트 — 줄 넘김 허용, 슬롯 높이가 CSF로 자동 조정됨 ──
        var statTmp = MakeTMP(go, "StatText", "", UIScale.FontSm, FontStyles.Normal);
        statTmp.color = StatC; statTmp.alignment = TextAlignmentOptions.TopLeft;
        statTmp.textWrappingMode = TextWrappingModes.Normal;
        statTmp.overflowMode = TextOverflowModes.Overflow;
        statTmp.raycastTarget = false;

        // ── 구매 버튼 (우측 하단, 소형 고정) — 44px로 키움 ──
        var buyRow = MakeGo("BuyRow", go);
        AddLE(buyRow, 0f, 44f);
        var bhlg = buyRow.AddComponent<HorizontalLayoutGroup>();
        bhlg.childAlignment = TextAnchor.MiddleRight;
        bhlg.childControlWidth = true; bhlg.childControlHeight = true;
        bhlg.childForceExpandWidth = false; bhlg.childForceExpandHeight = true;
        bhlg.padding = new RectOffset(0, 0, 0, 0);

        var (costTmp, buyBtn, soldOut) = BuildBuyBtnFixed(buyRow, "구매", BuyBtnC, 160f, 44f);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_iconImage", iconImg);
        SetObj(sSo, "_nameText",  nameTmp);
        SetObj(sSo, "_gradeText", gradeTmp);
        SetObj(sSo, "_statText",  statTmp);
        SetObj(sSo, "_costText",  costTmp);
        SetObj(sSo, "_buyBtn",    buyBtn);
        SetObj(sSo, "_soldOut",   soldOut);
        sSo.ApplyModifiedProperties();
        return slot;
    }

    // ═══════════════════════════════════════════════════════
    //  특성 슬롯 — 아이콘 좌 | 스텟+구매버튼
    // ═══════════════════════════════════════════════════════

    // 레이아웃: 아이콘(중앙 상단) → 이름(중앙) → spacer → 구매버튼(우측 하단)
    // 슬롯 고정 높이 200px — 배경이 넘치지 않도록 preferredHeight 명시
    static RunShopTraitSlot BuildTraitSlot(GameObject parent, string name)
    {
        var go = MakeGo(name, parent, typeof(Image));
        go.GetComponent<Image>().color = SlotTrait;
        // 고정 높이: icon(100) + name(34) + buy(36) + padding(18) + 여유(12) = 200px
        AddLE(go, 0f, 200f);
        var slot = go.AddComponent<RunShopTraitSlot>();
        SetVLG(go, new RectOffset(8, 8, 10, 8), 0f);

        // ── 아이콘 행: 양쪽 Spacer로 수평 중앙 정렬, minHeight=100 확보 ──
        var iconRow = MakeGo("IconRow", go);
        var iconRowLe = iconRow.AddComponent<LayoutElement>();
        iconRowLe.minHeight = 100f; iconRowLe.flexibleHeight = 1f;
        var irHlg = iconRow.AddComponent<HorizontalLayoutGroup>();
        irHlg.childAlignment = TextAnchor.MiddleCenter;
        irHlg.childControlWidth = true; irHlg.childControlHeight = true;
        irHlg.childForceExpandWidth = false; irHlg.childForceExpandHeight = false;

        MakeGo("SpacerL", iconRow).AddComponent<LayoutElement>().flexibleWidth = 1f;

        // 아이콘 컨테이너 (100×100 고정, 배경 없음)
        var iconContainer = MakeGo("IconContainer", iconRow);
        var icLe = iconContainer.AddComponent<LayoutElement>();
        icLe.preferredWidth = 100f; icLe.preferredHeight = 100f; icLe.flexibleWidth = 0f;

        var traitUi = BattlePanelCreator.BuildTraitIconSlot(iconContainer, 0);
        traitUi.gameObject.SetActive(true);
        if (!traitUi.TryGetComponent<LayoutElement>(out var traitLe))
            traitLe = traitUi.gameObject.AddComponent<LayoutElement>();
        traitLe.ignoreLayout = true;
        FullStretch(traitUi.gameObject.GetComponent<RectTransform>());

        MakeGo("SpacerR", iconRow).AddComponent<LayoutElement>().flexibleWidth = 1f;

        // ── 특성 이름 (아이콘 아래, 중앙 정렬, 배경 없음) ────
        var nameTmp = MakeTMP(go, "NameText", "특성 이름", UIScale.FontMd, FontStyles.Bold);
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.raycastTarget = false;
        AddLE(nameTmp.gameObject, 0f, 34f);

        MakeGo("Spacer", go).AddComponent<LayoutElement>().flexibleHeight = 0.4f;

        // ── 구매 버튼 (우측 하단) ─────────────────────────────
        var buyRow = MakeGo("BuyRow", go);
        AddLE(buyRow, 0f, 36f);
        var bhlg = buyRow.AddComponent<HorizontalLayoutGroup>();
        bhlg.childAlignment = TextAnchor.MiddleRight;
        bhlg.childControlWidth = true; bhlg.childControlHeight = true;
        bhlg.childForceExpandWidth = false; bhlg.childForceExpandHeight = true;

        var (costTmp, buyBtn, soldOut) = BuildBuyBtnFixed(buyRow, "구매", BuyBtnC, 160f, 32f);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_traitIcon",  traitUi);
        SetObj(sSo, "_nameText",   nameTmp);
        SetObj(sSo, "_descText",   null);
        SetObj(sSo, "_statsText",  null);
        SetObj(sSo, "_costText",   costTmp);
        SetObj(sSo, "_buyBtn",     buyBtn);
        SetObj(sSo, "_soldOut",    soldOut);
        sSo.ApplyModifiedProperties();
        return slot;
    }

    // ═══════════════════════════════════════════════════════
    //  장수 슬롯 — HeroCard + 고용버튼 우측 하단
    // ═══════════════════════════════════════════════════════

    static RunShopGeneralSlot BuildGeneralSlot(GameObject parent, string name)
    {
        var go = MakeGo(name, parent, typeof(Image));
        go.GetComponent<Image>().color = SlotGen;
        var slot = go.AddComponent<RunShopGeneralSlot>();
        SetVLG(go, new RectOffset(0, 0, 0, 0), 0f);

        // 카드 영역 (HeroCard)
        var cardGo = HeroPanelCreator.BuildCardPrefab();
        cardGo.name = "CardArea";
        cardGo.transform.SetParent(go.transform, false);

        if (cardGo.TryGetComponent<HeroCardUI>(out var heroCardUi))
            Object.DestroyImmediate(heroCardUi);

        if (!cardGo.TryGetComponent<Button>(out var cardBtn))
            cardBtn = cardGo.AddComponent<Button>();
        if (cardGo.TryGetComponent<Image>(out var cardImg))
        {
            cardBtn.targetGraphic = cardImg;
            var cb = cardBtn.colors;
            cb.highlightedColor = CardHover;
            cb.pressedColor     = new Color(0f, 0f, 0f, 0.10f);
            cb.normalColor      = Color.clear;
            cardBtn.colors = cb;
        }

        if (!cardGo.TryGetComponent<LayoutElement>(out var cardLe))
            cardLe = cardGo.AddComponent<LayoutElement>();
        // HeroCard는 170px 설계값으로 고정 — 늘어나면 콘텐츠가 비어 보임
        cardLe.preferredHeight = 170f;
        cardLe.flexibleHeight  = 0f;

        foreach (var img in cardGo.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
        foreach (var tmp in cardGo.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.raycastTarget = false;
        if (cardImg != null) cardImg.raycastTarget = true;

        // 고용 버튼 (우측 하단) — 26px, 18px 폰트 기준으로 텍스트 한 줄 보장
        AddDivider(go);
        var hireRow = MakeGo("HireRow", go);
        AddLE(hireRow, 0f, 26f);
        var hrHlg = hireRow.AddComponent<HorizontalLayoutGroup>();
        hrHlg.childAlignment = TextAnchor.MiddleRight;
        hrHlg.childControlWidth = true; hrHlg.childControlHeight = true;
        hrHlg.childForceExpandWidth = false; hrHlg.childForceExpandHeight = true;
        hrHlg.padding = new RectOffset(0, 6, 2, 2);

        var (costTmp, buyBtn, soldOut) = BuildHireBtnCompact(hireRow);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_portraitBg",     FindChild<Image>(cardGo.transform, "PortraitBg"));
        SetObj(sSo, "_portraitImg",    FindChild<Image>(cardGo.transform, "PortraitImage"));
        SetObj(sSo, "_portraitBridge", FindChild<UnitAppearanceBridge>(cardGo.transform, "PortraitPreview"));
        SetObj(sSo, "_nameText",       FindChild<TextMeshProUGUI>(cardGo.transform, "NameText"));
        SetObj(sSo, "_jobText",        FindChild<TextMeshProUGUI>(cardGo.transform, "JobText"));
        SetObj(sSo, "_gradeText",      FindChild<TextMeshProUGUI>(cardGo.transform, "GradeText"));
        SetObj(sSo, "_hpText",         FindChild<TextMeshProUGUI>(cardGo.transform, "HpText"));
        SetObj(sSo, "_atkText",        FindChild<TextMeshProUGUI>(cardGo.transform, "AtkText"));
        SetObj(sSo, "_defText",        FindChild<TextMeshProUGUI>(cardGo.transform, "DefText"));
        SetObj(sSo, "_soldierText",    FindChild<TextMeshProUGUI>(cardGo.transform, "SoldierText"));
        SetObj(sSo, "_costText",       costTmp);
        SetObj(sSo, "_buyBtn",         buyBtn);
        SetObj(sSo, "_cardBtn",        cardBtn);
        SetObj(sSo, "_soldOut",        soldOut);
        sSo.ApplyModifiedProperties();
        return slot;
    }

    static T FindChild<T>(Transform root, string cname) where T : Component
    {
        var t = root.Find(cname);
        if (t != null) return t.GetComponent<T>();
        foreach (Transform child in root)
        {
            var result = FindChild<T>(child, cname);
            if (result != null) return result;
        }
        return null;
    }

    // ═══════════════════════════════════════════════════════
    //  고정 사이즈 구매 버튼 — [💰 CostText · Label]
    // ═══════════════════════════════════════════════════════

    // 고용 전용 컴팩트 버튼 125×22px
    // 폰트 18px: goldIcon(14)+cost(46)+label(44)+spacing(4×2=8)+padding(5×2=10) = 122px → 125px 버튼
    static (TextMeshProUGUI costTmp, Button buyBtn, GameObject soldOut) BuildHireBtnCompact(
        GameObject parent)
    {
        var btnGo = MakeGo("BuyBtn", parent, typeof(Image), typeof(Button));
        btnGo.GetComponent<Image>().color = HireBtnC;
        AddLE(btnGo, 125f, 22f);

        var hlg = btnGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 4f; hlg.padding = new RectOffset(5, 5, 0, 0);
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        var goldSp = LoadGold();
        var giGo = MakeGo("GoldIcon", btnGo, typeof(Image));
        if (goldSp != null) giGo.GetComponent<Image>().sprite = goldSp;
        giGo.GetComponent<Image>().preserveAspect = true; giGo.GetComponent<Image>().raycastTarget = false;
        var giLe = giGo.AddComponent<LayoutElement>();
        giLe.preferredWidth = giLe.preferredHeight = 14f; giLe.flexibleWidth = 0f;

        // 18px 폰트: FontSm(24)보다 작아 22px 버튼 안에서 한 줄로 표시됨
        const float HireFont = 18f;
        var costTmp = MakeTMP(btnGo, "CostText", "0", HireFont, FontStyles.Bold);
        costTmp.color = GoldC; costTmp.alignment = TextAlignmentOptions.Right;
        costTmp.raycastTarget = false;
        costTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;

        var labelTmp = MakeTMP(btnGo, "Label", "고용", HireFont, FontStyles.Bold);
        labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.raycastTarget = false;
        labelTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;

        var parentTransform = parent.transform.parent;
        var soldOutParent = parentTransform != null ? parentTransform.gameObject : parent;
        var soldOut = BuildSoldOut(soldOutParent, "고용완료");
        return (costTmp, btnGo.GetComponent<Button>(), soldOut);
    }

    // 내용 너비 계산: goldIcon(18) + costText(60) + label(46) + spacing(4×2) + padding(7×2) = 146px
    // → w 파라미터는 160px 이상 사용할 것
    static (TextMeshProUGUI costTmp, Button buyBtn, GameObject soldOut) BuildBuyBtnFixed(
        GameObject parent, string btnLabel, Color btnColor, float w, float h)
    {
        var btnGo = MakeGo("BuyBtn", parent, typeof(Image), typeof(Button));
        btnGo.GetComponent<Image>().color = btnColor;
        AddLE(btnGo, w, h);

        var hlg = btnGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 4f; hlg.padding = new RectOffset(7, 7, 0, 0);
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        // 골드 아이콘 18px
        var goldSp = LoadGold();
        var giGo = MakeGo("GoldIcon", btnGo, typeof(Image));
        if (goldSp != null) giGo.GetComponent<Image>().sprite = goldSp;
        giGo.GetComponent<Image>().preserveAspect = true; giGo.GetComponent<Image>().raycastTarget = false;
        var giLe = giGo.AddComponent<LayoutElement>();
        giLe.preferredWidth = giLe.preferredHeight = 18f; giLe.flexibleWidth = 0f;

        // 가격 텍스트 60px (최대 "1,200" 4자리 수용)
        var costTmp = MakeTMP(btnGo, "CostText", "0", UIScale.FontSm, FontStyles.Bold);
        costTmp.color = GoldC; costTmp.alignment = TextAlignmentOptions.Right;
        costTmp.raycastTarget = false;
        costTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = 60f;

        // 레이블 46px
        var labelTmp = MakeTMP(btnGo, "Label", btnLabel, UIScale.FontSm, FontStyles.Bold);
        labelTmp.alignment = TextAlignmentOptions.Left; labelTmp.raycastTarget = false;
        labelTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;

        // SoldOut는 slot 루트 부모에 배치
        var parentTransform = parent.transform.parent;
        var soldOutParent = parentTransform != null ? parentTransform.gameObject : parent;
        var soldOut = BuildSoldOut(soldOutParent, btnLabel == "고용" ? "고용완료" : "품절");
        return (costTmp, btnGo.GetComponent<Button>(), soldOut);
    }

    // ═══════════════════════════════════════════════════════
    //  TitleBar 버튼 빌더
    // ═══════════════════════════════════════════════════════

    static (Button refresh, TextMeshProUGUI costTmp) BuildRefreshBtn(GameObject titleBar)
    {
        var go = MakeGo("RefreshBtn", titleBar, typeof(Image), typeof(Button));
        go.GetComponent<Image>().color = RefBtnC;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-90f, 0f);
        rt.sizeDelta = new Vector2(220f, 54f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 6f;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(10, 10, 0, 0);

        var goldSp = LoadGold();
        var giGo = MakeGo("GoldIcon", go, typeof(Image));
        if (goldSp != null) giGo.GetComponent<Image>().sprite = goldSp;
        giGo.GetComponent<Image>().preserveAspect = true; giGo.GetComponent<Image>().raycastTarget = false;
        var giLe = giGo.AddComponent<LayoutElement>();
        giLe.preferredWidth = giLe.preferredHeight = UIScale.IconSm; giLe.flexibleWidth = 0f;

        var costTmp = MakeTMP(go, "CostText", "100", UIScale.FontMd, FontStyles.Bold);
        costTmp.color = GoldC; costTmp.raycastTarget = false;
        costTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;

        var lbl = MakeTMP(go, "Label", "새로고침", UIScale.FontSm, FontStyles.Normal);
        lbl.raycastTarget = false;
        lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = 68f;

        return (go.GetComponent<Button>(), costTmp);
    }

    static Button BuildCloseBtn(GameObject titleBar)
    {
        var go = MakeGo("CloseBtn", titleBar, typeof(Image), typeof(Button));
        go.GetComponent<Image>().color = CloseBtnC;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-12f, 0f);
        rt.sizeDelta = new Vector2(62f, 62f);
        MakeTMP(go, "Label", "✕", UIScale.FontMd, FontStyles.Bold).raycastTarget = false;
        return go.GetComponent<Button>();
    }

    // ═══════════════════════════════════════════════════════
    //  공통 헬퍼
    // ═══════════════════════════════════════════════════════

    static GameObject BuildSoldOut(GameObject parent, string label)
    {
        var go = MakeGo("SoldOut", parent, typeof(Image));
        go.GetComponent<Image>().color = SoldOutC;
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        MakeTMP(go, "Label", label, UIScale.FontLg, FontStyles.Bold).color = new Color(0.9f, 0.5f, 0.5f);
        go.SetActive(false);
        return go;
    }

    static void AddDivider(GameObject parent)
    {
        var go = MakeGo("Divider", parent, typeof(Image));
        go.GetComponent<Image>().color = DivC;
        AddLE(go, 0f, 2f);
    }

    static Sprite LoadGold()
        => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_project/3.Textures/Icons/Items/item_gold.png");

    static void SetVLG(GameObject go, RectOffset padding, float spacing)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.padding = padding; vlg.spacing = spacing;
    }

    // 고정 사이즈용 HLG (force-expand 없음)
    static void SetHLGFixed(GameObject go, float spacing)
    {
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.spacing = spacing;
    }

    static GameObject MakeGo(string name, GameObject parent, params System.Type[] extra)
    {
        var go = new GameObject(name, typeof(RectTransform));
        foreach (var t in extra) go.AddComponent(t);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return tmp;
    }

    static void AddLE(GameObject go, float w, float h)
    {
        if (!go.TryGetComponent<LayoutElement>(out var le))
            le = go.AddComponent<LayoutElement>();
        if (w > 0f) le.preferredWidth  = w;
        if (h > 0f) le.preferredHeight = h;
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetRT(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = obj;
    }

    static void SetArr(SerializedObject so, string field, int count, System.Func<int, Object> getter)
    {
        var p = so.FindProperty(field);
        if (p == null) return;
        p.arraySize = count;
        for (int i = 0; i < count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = getter(i);
    }
}
