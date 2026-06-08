using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  BattlePanelCreator.cs
//  Tools > Project K > 로비 UI > Create BattlePanel Prefab
//  또는 LobbyPrefabCreator.CreateLobby() 에서 Build(root) 로 호출.
//
//  저장: Assets/_project/2.Prefabs/UI/Lobby/BattlePanel.prefab
//
//  생성 구조:
//    BattlePanel
//    ├── DeployArea  (좌측 420px — 5개 배치 슬롯)
//    │   ├── Slot_0 ... Slot_4  (DeploySlotUI)
//    └── ActionArea (나머지 공간)
//        ├── StageText        ("스테이지 N 도전")
//        ├── ProgressText     ("N 스테이지 클리어")
//        ├── AbilityListBtn
//        ├── HireBtn
//        └── BattleStartBtn
// ============================================================

public static class BattlePanelCreator
{
    const string SavePath             = "Assets/_project/2.Prefabs/UI/Lobby/BattlePanel.prefab";
    const string StageNodePrefabPath  = "Assets/_project/2.Prefabs/UI/Lobby/StageNodeUI.prefab";
    const string StageArrowPrefabPath = "Assets/_project/2.Prefabs/UI/Lobby/StageArrowUI.prefab";

    const float TopBarH       = 180f;
    const float NavBarH       = 160f;
    const float DeployW       = 420f;
    const float SlotGap       = 8f;
    const int   SlotCount     = 5;
    const float HireBtnH      = 80f;
    const float TraitBarH     = 110f;  // 상단 특성 아이콘 바 높이
    const float ProgressBarH  = 76f;   // 스테이지 진행바 높이
    const int   TraitSlotMax  = 8;     // 특성 최대 슬롯 수
    const float TraitIconSize = 90f;   // 특성 아이콘 크기

    static readonly Color BgDark      = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color SlotBgEmpty = new Color(0.12f, 0.12f, 0.20f, 1f);
    static readonly Color ActionBg    = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color BattleBtnC     = new Color(0.11f, 0.72f, 0.58f, 1f);
    static readonly Color AbilityBtnC    = new Color(0.18f, 0.25f, 0.45f, 1f);
    static readonly Color HireBtnC       = new Color(0.45f, 0.25f, 0.18f, 1f);
    static readonly Color DisassembleBtnC = new Color(0.30f, 0.16f, 0.08f, 1f);
    static readonly Color MutedText    = new Color(0.55f, 0.55f, 0.60f);

    [MenuItem("Tools/Project K/로비 UI/Create BattlePanel Prefab")]
    static void CreateStandalone()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta =
            new Vector2(UIScale.RefWidth, UIScale.RefHeight);

        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattlePanelCreator] BattlePanel.prefab 생성 완료");
    }

    public static GameObject Build(GameObject parent)
    {
        // ── BattlePanel 루트 ──────────────────────────────────
        var panel = new GameObject("BattlePanel", typeof(RectTransform));
        panel.transform.SetParent(parent.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(0,  0);
        panelRt.offsetMax = new Vector2(0, -TopBarH);

        var ui = panel.AddComponent<StageSelectUI>();

        // ── DeployArea (좌측, 전체 높이) ──────────────────────
        var deployArea = MakeGo("DeployArea", panel);
        var deployRt   = deployArea.GetComponent<RectTransform>();
        deployRt.anchorMin = new Vector2(0, 0);
        deployRt.anchorMax = new Vector2(0, 1);
        deployRt.offsetMin = Vector2.zero;
        deployRt.offsetMax = new Vector2(DeployW, 0);

        var deployBg = deployArea.AddComponent<Image>();
        deployBg.color = BgDark;

        var vlg = deployArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.spacing                = SlotGap;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = true;
        vlg.padding                = new RectOffset(4, 4, 4, 4);

        float slotH = (UIScale.RefHeight - TopBarH - SlotGap * (SlotCount - 1) - 8f) / SlotCount;

        var slots = new DeploySlotUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var slot = BuildSlot(deployArea, i, slotH);
            slots[i] = slot.GetComponent<DeploySlotUI>();
        }

        // ── ActionArea (우측, 전체 높이) ──────────────────────
        var actionArea = MakeGo("ActionArea", panel);
        var actionRt   = actionArea.GetComponent<RectTransform>();
        actionRt.anchorMin = new Vector2(0, 0);
        actionRt.anchorMax = new Vector2(1, 1);
        actionRt.offsetMin = new Vector2(DeployW, 0);
        actionRt.offsetMax = Vector2.zero;

        var actionBg = actionArea.AddComponent<Image>();
        actionBg.color = ActionBg;

        // ── TraitBar (ActionArea 상단 — 빨간 박스 영역) ───────
        var traitBar   = MakeGo("TraitBar", actionArea);
        var traitBarRt = traitBar.GetComponent<RectTransform>();
        traitBarRt.anchorMin = new Vector2(0f, 1f);
        traitBarRt.anchorMax = new Vector2(1f, 1f);
        traitBarRt.offsetMin = new Vector2(0f, -TraitBarH);
        traitBarRt.offsetMax = Vector2.zero;
        traitBar.AddComponent<Image>().color = new Color(0.10f, 0.06f, 0.10f, 0.95f);

        var traitHlg = traitBar.AddComponent<HorizontalLayoutGroup>();
        traitHlg.childAlignment        = TextAnchor.MiddleLeft;
        traitHlg.spacing               = 12f;
        traitHlg.padding               = new RectOffset(16, 16, 10, 10);
        traitHlg.childForceExpandWidth  = false;
        traitHlg.childForceExpandHeight = true;

        var traitIcons = new TraitIconUI[TraitSlotMax];
        for (int i = 0; i < TraitSlotMax; i++)
            traitIcons[i] = BuildTraitIconSlot(traitBar, i);

        // ── StageTypeIndicator (ActionArea 상단, TraitBar 바로 아래 — 초록 박스) ──
        var progressBarGo = MakeGo("StageProgressBar", actionArea);
        var progressBarUi = progressBarGo.AddComponent<StageProgressBarUI>();
        {
            var rt = progressBarGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0.80f, 1f);
            rt.offsetMin = new Vector2(12f, -(TraitBarH + ProgressBarH));
            rt.offsetMax = new Vector2(-12f, -TraitBarH);
        }
        // 배경 (반투명 패널)
        progressBarGo.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.14f, 0.80f);

        // 노드 컨테이너 (HorizontalLayoutGroup — 아이콘 노드들을 수평 정렬)
        var pbNodeContainer = MakeGo("Nodes", progressBarGo);
        FullStretch(pbNodeContainer);
        var pbHlg = pbNodeContainer.AddComponent<HorizontalLayoutGroup>();
        pbHlg.childAlignment         = TextAnchor.MiddleCenter;
        pbHlg.spacing                = 0f;
        pbHlg.padding                = new RectOffset(16, 16, 8, 8);
        pbHlg.childControlWidth      = true;   // LayoutElement.preferredWidth 적용
        pbHlg.childControlHeight     = true;   // LayoutElement.preferredHeight 적용
        pbHlg.childForceExpandWidth  = false;
        pbHlg.childForceExpandHeight = false;

        // 노드·화살표 프리팹 생성 (없으면 새로 저장)
        var stageNodePrefab  = BuildStageNodePrefab();
        var stageArrowPrefab = BuildStageArrowPrefab();

        var pbSo = new SerializedObject(progressBarUi);
        pbSo.Update();
        SetObj(pbSo, "_nodeContainer", pbNodeContainer.transform);
        SetObj(pbSo, "_nodePrefab",    stageNodePrefab);
        SetObj(pbSo, "_arrowPrefab",   stageArrowPrefab);

        pbSo.ApplyModifiedProperties();

        // ── 스테이지 텍스트 (TraitBar + Indicator 아래) ───────
        var stageText = CreateTMP(actionArea, "StageText", "스테이지 1 도전",
            UIScale.FontLg, FontStyles.Bold);
        SetRect(stageText.GetComponent<RectTransform>(),
            new Vector2(0, 80), new Vector2(1100, 80));

        var stageTypeText = CreateTMP(actionArea, "StageTypeText", "일반",
            UIScale.FontMd, FontStyles.Bold);
        SetRect(stageTypeText.GetComponent<RectTransform>(),
            new Vector2(0, 10), new Vector2(600, 48));
        stageTypeText.color = new Color(0.6f, 0.7f, 0.85f);

        var progressText = CreateTMP(actionArea, "ProgressText", "0 / 30 스테이지",
            UIScale.FontMd, FontStyles.Normal);
        SetRect(progressText.GetComponent<RectTransform>(),
            new Vector2(0, -55), new Vector2(1000, 46));
        progressText.color = MutedText;

        // ── 우측 아이콘 버튼 컬럼 ────────────────────────────
        const float IBtn = 170f;
        const float IGap =  20f;
        float iBtnStep = IBtn + IGap;

        // 상점/이벤트 스테이지 타입 아이콘 (어빌리티 버튼 위)
        var shopIcon  = CreateIconButton(actionArea, "ShopIcon",  "상점",   new Color(0.15f, 0.50f, 0.75f),
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_ability.png");
        SetRightColRect(shopIcon.GetComponent<RectTransform>(), iBtnStep * 1.5f, IBtn);
        shopIcon.SetActive(false);

        var eventIcon = CreateIconButton(actionArea, "EventIcon", "이벤트", new Color(0.50f, 0.20f, 0.65f),
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_ability.png");
        SetRightColRect(eventIcon.GetComponent<RectTransform>(), iBtnStep * 1.5f, IBtn);
        eventIcon.SetActive(false);

        var abilityBtn = CreateIconButton(actionArea, "AbilityListBtn", "어빌리티", AbilityBtnC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_ability.png");
        SetRightColRect(abilityBtn.GetComponent<RectTransform>(), iBtnStep / 2f, IBtn);

        var disassembleBtn = CreateIconButton(actionArea, "DisassembleBtn", "장비 분해", DisassembleBtnC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_disassemble.png");
        SetRightColRect(disassembleBtn.GetComponent<RectTransform>(), -iBtnStep / 2f, IBtn);

        // 전투 시작 버튼 (하단 — NavBar 제거로 80px 여유, 하단 마진 120px)
        var battleBtn   = CreateButton(actionArea, "BattleStartBtn", "전투 시작", BattleBtnC, UIScale.FontLg);
        var battleBtnRt = battleBtn.GetComponent<RectTransform>();
        battleBtnRt.anchorMin        = new Vector2(0.17f, 0f);
        battleBtnRt.anchorMax        = new Vector2(0.83f, 0f);
        battleBtnRt.anchoredPosition = new Vector2(0, UIScale.BtnLg / 2f + 120f);
        battleBtnRt.sizeDelta        = new Vector2(0, UIScale.BtnLg);
        battleBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // ── 용병 고용 버튼 (BattlePanel 루트 자식, Slot 1 바로 위 TopBar 영역) ──
        var hireBtn = new GameObject("HireBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        hireBtn.transform.SetParent(panel.transform, false);
        hireBtn.GetComponent<Image>().color = HireBtnC;
        var hireBtnRt = hireBtn.GetComponent<RectTransform>();
        hireBtnRt.anchorMin        = new Vector2(0f, 1f);
        hireBtnRt.anchorMax        = new Vector2(0f, 1f);
        hireBtnRt.pivot            = new Vector2(0f, 1f);
        hireBtnRt.anchoredPosition = new Vector2(20f, 100f);
        hireBtnRt.sizeDelta        = new Vector2(380f, HireBtnH);
        hireBtn.SetActive(false);

        // 내용 행 (HLG — 이름 + 골드 아이콘 + 비용)
        var hireRow = new GameObject("ContentRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hireRow.transform.SetParent(hireBtn.transform, false);
        var hireRowRt = hireRow.GetComponent<RectTransform>();
        hireRowRt.anchorMin = Vector2.zero;
        hireRowRt.anchorMax = Vector2.one;
        hireRowRt.offsetMin = hireRowRt.offsetMax = Vector2.zero;
        var hlg = hireRow.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.spacing               = 10f;
        hlg.padding               = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // "용병 고용" 라벨
        var hireNameGo  = new GameObject("NameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        hireNameGo.transform.SetParent(hireRow.transform, false);
        hireNameGo.AddComponent<LayoutElement>().preferredWidth = 200f;
        var hireNameTmp = hireNameGo.GetComponent<TextMeshProUGUI>();
        hireNameTmp.text      = "용병 고용";
        hireNameTmp.fontSize  = UIScale.FontMd;
        hireNameTmp.alignment = TextAlignmentOptions.Right;
        hireNameTmp.color     = Color.white;

        // 골드 아이콘
        var hireIconGo  = new GameObject("GoldIcon", typeof(RectTransform), typeof(Image));
        hireIconGo.transform.SetParent(hireRow.transform, false);
        hireIconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 44f);
        var hireIconLe  = hireIconGo.AddComponent<LayoutElement>();
        hireIconLe.minWidth  = hireIconLe.preferredWidth  = 44f;
        hireIconLe.minHeight = hireIconLe.preferredHeight = 44f;
        var hireIconImg = hireIconGo.GetComponent<Image>();
        hireIconImg.preserveAspect = true;
        var goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_project/3.Textures/Icons/Items/item_gold.png");
        if (goldSprite != null) hireIconImg.sprite = goldSprite;

        // "+500" 비용 텍스트
        var hireCostGo  = new GameObject("CostLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        hireCostGo.transform.SetParent(hireRow.transform, false);
        hireCostGo.AddComponent<LayoutElement>().preferredWidth = 120f;
        var hireCostTmp = hireCostGo.GetComponent<TextMeshProUGUI>();
        hireCostTmp.text      = "+500";
        hireCostTmp.fontSize  = UIScale.FontMd;
        hireCostTmp.alignment = TextAlignmentOptions.Left;
        hireCostTmp.color     = new Color(1f, 0.85f, 0.20f);

        // ── StageSelectUI 필드 연결 ───────────────────────────
        var so = new SerializedObject(ui);
        so.Update();

        var slotsProp = so.FindProperty("_deploySlots");
        if (slotsProp != null)
        {
            slotsProp.arraySize = SlotCount;
            for (int i = 0; i < SlotCount; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }

        var traitsProp = so.FindProperty("_traitIcons");
        if (traitsProp != null)
        {
            traitsProp.arraySize = traitIcons.Length;
            for (int i = 0; i < traitIcons.Length; i++)
                traitsProp.GetArrayElementAtIndex(i).objectReferenceValue = traitIcons[i];
        }

        SetObj(so, "_stageText",      stageText);
        SetObj(so, "_stageTypeText",  stageTypeText);
        SetObj(so, "_progressText",   progressText);
        SetObj(so, "_progressBar",    progressBarUi);
        SetObj(so, "_shopIcon",       shopIcon);
        SetObj(so, "_eventIcon",      eventIcon);
        SetObj(so, "_abilityListBtn", abilityBtn.GetComponent<Button>());
        SetObj(so, "_hireBtn",        hireBtn.GetComponent<Button>());
        SetObj(so, "_hireCostText",   hireCostTmp);
        SetObj(so, "_disassembleBtn", disassembleBtn.GetComponent<Button>());
        SetObj(so, "_battleStartBtn", battleBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        return panel;
    }

    // ── 특성 아이콘 슬롯 생성 ────────────────────────────────

    public static TraitIconUI BuildTraitIconSlot(GameObject parent, int index)
    {
        var go = new GameObject($"TraitSlot_{index}", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = TraitIconSize;

        var ui = go.AddComponent<TraitIconUI>();

        // 아이콘 버튼 (배경)
        var btnGo = new GameObject("IconBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(go.transform, false);
        FullStretch(btnGo);
        btnGo.GetComponent<Image>().color = new Color(0.15f, 0.10f, 0.18f);

        // 아이콘 이미지 (inset)
        var imgGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(btnGo.transform, false);
        var imgRt = imgGo.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.1f, 0.1f);
        imgRt.anchorMax = new Vector2(0.9f, 0.9f);
        imgRt.offsetMin = imgRt.offsetMax = Vector2.zero;
        var iconImg = imgGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.color = new Color(0.25f, 0.25f, 0.38f);

        // 툴팁 패널 (비활성)
        var tooltipGo = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
        tooltipGo.transform.SetParent(go.transform, false);
        tooltipGo.SetActive(false);
        tooltipGo.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.12f, 0.96f);
        {
            var rt = tooltipGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -4f);
            rt.sizeDelta = new Vector2(300f, 0f);
        }
        var tvlg = tooltipGo.AddComponent<VerticalLayoutGroup>();
        tvlg.padding = new RectOffset(10, 10, 10, 10); tvlg.spacing = 4f;
        tvlg.childControlWidth = tvlg.childControlHeight = true;
        tvlg.childForceExpandWidth = true; tvlg.childForceExpandHeight = false;
        var tcsf = tooltipGo.AddComponent<ContentSizeFitter>();
        tcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var tName = new GameObject("TooltipName", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        tName.transform.SetParent(tooltipGo.transform, false);
        var tNameTmp = tName.GetComponent<TMPro.TextMeshProUGUI>();
        tNameTmp.text = ""; tNameTmp.fontSize = UIScale.FontSm;
        tNameTmp.fontStyle = FontStyles.Bold; tNameTmp.color = Color.white;
        tNameTmp.alignment = TMPro.TextAlignmentOptions.Left;
        tNameTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

        var tDesc = new GameObject("TooltipDesc", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        tDesc.transform.SetParent(tooltipGo.transform, false);
        var tDescTmp = tDesc.GetComponent<TMPro.TextMeshProUGUI>();
        tDescTmp.text = ""; tDescTmp.fontSize = UIScale.FontSm;
        tDescTmp.color = new Color(0.55f, 0.57f, 0.72f);
        tDescTmp.alignment = TMPro.TextAlignmentOptions.Left;
        tDescTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

        var tStat = new GameObject("TooltipStat", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        tStat.transform.SetParent(tooltipGo.transform, false);
        tStat.SetActive(false);
        var tStatTmp = tStat.GetComponent<TMPro.TextMeshProUGUI>();
        tStatTmp.text = ""; tStatTmp.fontSize = UIScale.FontSm;
        tStatTmp.color = new Color(0.55f, 0.90f, 0.65f);
        tStatTmp.alignment = TMPro.TextAlignmentOptions.Left;
        tStatTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // TraitIconUI 필드 연결
        var tso = new SerializedObject(ui);
        tso.Update();
        SetObj(tso, "_iconImage",   iconImg);
        SetObj(tso, "_iconBtn",     btnGo.GetComponent<Button>());
        SetObj(tso, "_tooltip",     tooltipGo);
        SetObj(tso, "_tooltipName", tNameTmp);
        SetObj(tso, "_tooltipDesc", tDescTmp);
        SetObj(tso, "_tooltipStat", tStatTmp);
        tso.ApplyModifiedProperties();

        go.SetActive(false);  // 처음엔 숨김 — RefreshTraitIcons에서 활성화
        return ui;
    }

    // ── 슬롯 구성 ────────────────────────────────────────────

    static T FindChild<T>(Transform root, string name) where T : Component
        => root.Find(name)?.GetComponent<T>();

    static GameObject BuildSlot(GameObject parent, int index, float height)
    {
        var go   = MakeGo($"Slot_{index}", parent);
        var goRt = go.GetComponent<RectTransform>();
        goRt.sizeDelta = new Vector2(380f, 180f);   // 에디터 기본값 명시 (VLG가 런타임에 재계산)

        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = 110f;
        le.preferredHeight = height;

        var slotUi = go.AddComponent<DeploySlotUI>();

        // 슬롯 배경 + 버튼 (항상 활성 — 빈/점유 모두 처리)
        var slotImg = go.AddComponent<Image>();
        slotImg.color = SlotBgEmpty;
        var slotBtn = go.AddComponent<Button>();
        slotBtn.targetGraphic = slotImg;
        ColorBlock cb = slotBtn.colors;
        cb.highlightedColor = new Color(0.20f, 0.25f, 0.38f, 1f);
        cb.pressedColor     = new Color(0.12f, 0.15f, 0.24f, 1f);
        slotBtn.colors = cb;

        // ── EmptyGroup — "용병 고용 (골드아이콘) +500" ──────────
        var emptyGo = MakeGo("EmptyGroup", go);
        FullStretch(emptyGo);

        // 중앙 정렬 콘텐츠 행
        var emptyRow = MakeGo("EmptyContentRow", emptyGo);
        {
            var rt = emptyRow.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        var eHlg = emptyRow.AddComponent<HorizontalLayoutGroup>();
        eHlg.childAlignment        = TextAnchor.MiddleCenter;
        eHlg.spacing               = 8f;
        eHlg.childForceExpandWidth  = false;
        eHlg.childForceExpandHeight = false;
        eHlg.padding               = new RectOffset(8, 8, 0, 0);

        // "용병 고용" 라벨
        var emptyLabel = CreateTMP(emptyRow, "EmptyLabel", "용병 고용", UIScale.FontMd, FontStyles.Normal);
        emptyLabel.color = MutedText;
        emptyLabel.alignment = TextAlignmentOptions.Right;
        emptyLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;

        // 골드 아이콘
        var eIconGo = new GameObject("GoldIcon", typeof(RectTransform), typeof(Image));
        eIconGo.transform.SetParent(emptyRow.transform, false);
        eIconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);
        var eIconLe = eIconGo.AddComponent<LayoutElement>();
        eIconLe.minWidth = eIconLe.preferredWidth = 34f;
        eIconLe.minHeight = eIconLe.preferredHeight = 34f;
        var eIconImg = eIconGo.GetComponent<Image>();
        eIconImg.preserveAspect = true;
        var eGoldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_project/3.Textures/Icons/Items/item_gold.png");
        if (eGoldSprite != null) eIconImg.sprite = eGoldSprite;

        // "+500" 비용 라벨
        var emptyCost = CreateTMP(emptyRow, "CostLabel", "+500", UIScale.FontMd, FontStyles.Normal);
        emptyCost.color = new Color(1f, 0.85f, 0.20f);
        emptyCost.alignment = TextAlignmentOptions.Left;
        emptyCost.gameObject.AddComponent<LayoutElement>().preferredWidth = 75f;

        // ── OccupiedGroup = HeroCard 프리팹 구조 ─────────────
        var cardGo = HeroPanelCreator.BuildCardPrefab();
        cardGo.name = "OccupiedGroup";
        cardGo.transform.SetParent(go.transform, false);
        cardGo.SetActive(false);

        // 슬롯 크기에 full-stretch (HeroCard 내부 레이아웃은 비율 앵커 사용이므로 자동 대응)
        {
            var rt = cardGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // HeroCardUI 제거 — DeploySlotUI가 대신 관리
        var heroCardUi = cardGo.GetComponent<HeroCardUI>();
        if (heroCardUi != null) Object.DestroyImmediate(heroCardUi);

        // 카드의 Button 제거 — 슬롯 루트 버튼으로 통일
        var cardBtn = cardGo.GetComponent<Button>();
        if (cardBtn != null) Object.DestroyImmediate(cardBtn);

        // 모든 자식 Image·TMP 의 raycastTarget = false → 클릭이 슬롯 버튼으로 통과
        foreach (var img in cardGo.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
        foreach (var tmp in cardGo.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.raycastTarget = false;

        // ── DeploySlotUI 직렬화 필드 연결 ────────────────────
        var so = new SerializedObject(slotUi);
        so.Update();
        SetObj(so, "_button",         slotBtn);
        SetObj(so, "_emptyGroup",     emptyGo);
        SetObj(so, "_emptyLabel",     emptyLabel);
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
        so.ApplyModifiedProperties();

        return go;
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

    static GameObject MakeGo(string name, GameObject parent, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (components != null)
            foreach (var c in components) go.AddComponent(c);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI CreateTMP(GameObject parent, string name, string text,
        float size, FontStyles style)
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

    static GameObject CreateIconButton(GameObject parent, string name, string label,
        Color bgColor, string iconAssetPath)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        ColorBlock cb = go.GetComponent<Button>().colors;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.25f);
        go.GetComponent<Button>().colors = cb;

        // 아이콘 이미지 (위쪽 70%)
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.12f, 0.28f);
        iconRt.anchorMax = new Vector2(0.88f, 0.94f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
        if (sprite != null) iconImg.sprite = sprite;

        // 라벨 텍스트 (아래쪽 28%)
        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0, 0);
        lRt.anchorMax = new Vector2(1, 0.30f);
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = UIScale.FontSm;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        return go;
    }

    // 우측 세로 배치 — 오른쪽 가장자리 기준, yOffset=0 이 ActionArea 수직 중앙
    static void SetRightColRect(RectTransform rt, float yOffset, float size)
    {
        rt.anchorMin        = new Vector2(1f, 0.5f);
        rt.anchorMax        = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-16f, yOffset);
        rt.sizeDelta        = new Vector2(size, size);
    }

    static GameObject CreateButton(GameObject parent, string name, string label,
        Color bgColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    // ── 스테이지 노드 프리팹 생성 ────────────────────────────

    static StageNodeUI BuildStageNodePrefab()
    {
        var go = new GameObject("StageNodeUI", typeof(RectTransform));
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = StageNodeUI.NodeSize;
        le.preferredHeight = StageNodeUI.NodeSize;
        le.flexibleWidth   = 0f;
        var nodeUi = go.AddComponent<StageNodeUI>();

        // 배경 이미지 (노드 크기를 채움)
        var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        FullStretch(bgGo);
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.30f, 0.33f, 0.46f);

        // 스테이지 타입 아이콘 이미지 (내부 80%)
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.10f, 0.10f);
        iconRt.anchorMax = new Vector2(0.90f, 0.90f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.enabled        = false;   // 아이콘 없을 때 숨김

        // 타입 라벨 (노드 바로 아래)
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin        = labelRt.anchorMax = new Vector2(0.5f, 0f);
        labelRt.pivot            = new Vector2(0.5f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, -4f);
        labelRt.sizeDelta        = new Vector2(120f, 36f);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text          = "";
        labelTmp.fontSize      = UIScale.FontMd;
        labelTmp.alignment     = TextAlignmentOptions.Center;
        labelTmp.color         = new Color(0.55f, 0.58f, 0.70f);
        labelTmp.raycastTarget = false;
        labelGo.SetActive(false);

        // ▲ 현재 위치 마커 (노드 바로 위)
        var markerGo = new GameObject("Marker", typeof(RectTransform), typeof(TextMeshProUGUI));
        markerGo.transform.SetParent(go.transform, false);
        var markerRt = markerGo.GetComponent<RectTransform>();
        markerRt.anchorMin        = markerRt.anchorMax = new Vector2(0.5f, 1f);
        markerRt.pivot            = new Vector2(0.5f, 0f);
        markerRt.anchoredPosition = new Vector2(0f, 4f);
        markerRt.sizeDelta        = new Vector2(52f, 36f);
        var markerTmp = markerGo.GetComponent<TextMeshProUGUI>();
        markerTmp.text          = "▲";
        markerTmp.fontSize      = UIScale.FontMd;
        markerTmp.alignment     = TextAlignmentOptions.Center;
        markerTmp.color         = new Color(1.00f, 0.85f, 0.15f);
        markerTmp.raycastTarget = false;
        markerGo.SetActive(false);

        // 필드 연결
        var so = new SerializedObject(nodeUi);
        so.Update();
        SetObj(so, "_bg",     bgImg);
        SetObj(so, "_icon",   iconImg);
        SetObj(so, "_label",  labelTmp);
        SetObj(so, "_marker", markerGo);
        SetObj(so, "_le",     le);
        so.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, StageNodePrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        return prefab.GetComponent<StageNodeUI>();
    }

    static GameObject BuildStageArrowPrefab()
    {
        var go = new GameObject("StageArrowUI", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        var le  = go.GetComponent<LayoutElement>();
        le.preferredWidth  = 26f;
        le.preferredHeight = StageNodeUI.NodeSize;
        le.flexibleWidth   = 0f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text          = "▶";
        tmp.fontSize      = UIScale.FontMd;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = new Color(0.30f, 0.32f, 0.42f);
        tmp.raycastTarget = false;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, StageArrowPrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        return prefab;
    }

    // ── RectTransform 헬퍼 ───────────────────────────────────

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
    }
}
