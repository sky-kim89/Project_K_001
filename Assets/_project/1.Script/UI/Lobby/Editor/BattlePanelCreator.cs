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
//    ├── DeployArea  (좌측 DeployW — 5개 배치 슬롯, 잠긴 슬롯은 숨김)
//    │   ├── Slot_0 ... Slot_4  (DeploySlotUI)
//    └── ActionArea (나머지 공간)
//        ├── TraitBar          (일반 특성 2줄 + 시너지 1줄)
//        ├── StageProgressBar  (좌우 대칭 = 아래 텍스트와 같은 중심)
//        ├── StageText         ("스테이지 N 도전")
//        ├── ProgressText      ("N 스테이지 클리어")
//        ├── AbilityListBtn · DisassembleBtn
//        └── BattleStartBtn
//
//  상점·이벤트 버튼은 없다 — 해당 스테이지 도착 시 StageSelectUI 가 자동으로 띄운다.
// ============================================================

public static class BattlePanelCreator
{
    const string SavePath             = "Assets/_project/2.Prefabs/UI/Lobby/BattlePanel.prefab";
    const string StageNodePrefabPath  = "Assets/_project/2.Prefabs/UI/Lobby/StageNodeUI.prefab";
    const string StageArrowPrefabPath = "Assets/_project/2.Prefabs/UI/Lobby/StageArrowUI.prefab";

    const float TopBarH       = 180f;
    const float DeployW       = 560f;   // 좌측 배치 슬롯 열 — 카드 스탯이 4자리까지 들어가는 폭
    const float SlotGap       = 8f;
    const int   SlotCount     = 5;

    // ── 스테이지 진행바 ───────────────────────────────────────
    //  ProgressBarTop 은 ActionArea 상단에서 내려오는 거리.
    //  현재 노드 위에 붙는 마커가 바 밖으로 삐져나오므로 여유를 둔다.
    const float ProgressBarH    = 76f;
    const float ProgressBarTop  = 48f;
    const float StageNodeGap    = 26f;  // 노드 사이 간격 — 아래 타입 라벨이 겹치지 않을 만큼

    static readonly Color BgDark      = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color SlotBgEmpty = new Color(0.12f, 0.12f, 0.20f, 1f);
    // ⚠ ActionArea 는 투명하다 (a=0)
    //   이 칸 뒤에는 출전 대기 중인 장수들이 실제로 서 있는 전장이 비친다.
    //   불투명하게 칠하면 배치한 부대가 통째로 가려진다.
    //   Image 자체는 남겨 둔다 — 빈 곳 클릭이 뒤로 새는 것을 막아 준다.
    static readonly Color ActionBg    = new Color(0.05f, 0.05f, 0.10f, 0f);
    static readonly Color BattleBtnC     = new Color(0.11f, 0.72f, 0.58f, 1f);
    static readonly Color AbilityBtnC    = new Color(0.18f, 0.25f, 0.45f, 1f);
    static readonly Color DisassembleBtnC = new Color(0.30f, 0.16f, 0.08f, 1f);
    static readonly Color ShopBtnC        = new Color(0.42f, 0.30f, 0.10f, 1f);
    static readonly Color MutedText    = new Color(0.55f, 0.55f, 0.60f);

    [MenuItem(ProjectKMenu.Lobby + "BattlePanel", priority = ProjectKMenu.PrefabPrio + 13)]
    public static void CreateStandalone()
    {
        // BattlePanel 은 로비(1920×1080 가로) 전용이다.
        // UIScale.RefWidth/Height(1080×1920 세로) 로 잡으면 프리팹이 세로로 저장된다.
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta =
            new Vector2(UIScale.LobbyCanvasH / 9f * 16f, UIScale.LobbyCanvasH);

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
        vlg.childForceExpandHeight = false;   // 잠긴 슬롯을 숨겨도 남은 카드가 늘어나지 않는다
        vlg.padding                = new RectOffset(4, 4, 4, 4);

        // 로비 캔버스는 1920×1080 (가로) — 세로 여유는 1080 뿐이다.
        // UIScale.RefHeight(1920) 로 계산하면 슬롯이 화면보다 커진다.
        float slotH = (UIScale.LobbyCanvasH - TopBarH - SlotGap * (SlotCount - 1) - 8f) / SlotCount;

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

        // 특성 바는 여기 없다 — TopBar(TopBarCreator.BuildTraitStrip) 로 옮겼다.
        // 어느 패널을 보고 있든 현재 런의 특성이 계속 보여야 하기 때문이다.

        // ── 스테이지 진행바 (ActionArea 상단) ─────────────────
        //  0~0.80 으로 잡으면 노드는 그 안에서 가운데 정렬돼도
        //  아래 스테이지 텍스트(ActionArea 중앙 기준)보다 왼쪽으로 밀린다.
        //  → 좌우 대칭으로 잡아 두 기준을 일치시킨다.
        var progressBarGo = MakeGo("StageProgressBar", actionArea);
        var progressBarUi = progressBarGo.AddComponent<StageProgressBarUI>();
        {
            var rt = progressBarGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(12f,  -(ProgressBarTop + ProgressBarH));
            rt.offsetMax = new Vector2(-12f, -ProgressBarTop);
        }
        // 배경 (반투명 패널)
        progressBarGo.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.14f, 0.80f);

        // 노드 컨테이너 (HorizontalLayoutGroup — 아이콘 노드들을 수평 정렬)
        var pbNodeContainer = MakeGo("Nodes", progressBarGo);
        FullStretch(pbNodeContainer);
        var pbHlg = pbNodeContainer.AddComponent<HorizontalLayoutGroup>();
        pbHlg.childAlignment         = TextAnchor.MiddleCenter;
        // 노드(52) + 화살표(26) 만으로는 간격이 78px 이라 노드 아래 타입 라벨
        // ("이벤트"·"엘리트")이 서로 겹쳤다. 간격을 벌려 라벨 폭을 확보한다.
        pbHlg.spacing                = StageNodeGap;
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

        // ── 스테이지 텍스트 (진행바 아래) ─────────────────────
        //  y 는 ActionArea 세로 중앙(높이 900 → 중앙 450) 기준.
        //  진행바는 상단 48~124, 노드 아래 타입 라벨이 ~160 까지 내려온다.
        //  폭은 우측 아이콘 버튼 컬럼(170 + 여백)을 피해 900 으로 잡는다.
        var stageText = CreateTMP(actionArea, "StageText", "스테이지 1 도전",
            UIScale.FontLg, FontStyles.Bold);
        SetRect(stageText.GetComponent<RectTransform>(),
            new Vector2(0, 150), new Vector2(900, UIScale.Line(UIScale.FontLg) + 10f));

        var stageTypeText = CreateTMP(actionArea, "StageTypeText", "일반",
            UIScale.FontMd, FontStyles.Bold);
        SetRect(stageTypeText.GetComponent<RectTransform>(),
            new Vector2(0, 80), new Vector2(600, UIScale.RowMd));
        stageTypeText.color = new Color(0.6f, 0.7f, 0.85f);

        var progressText = CreateTMP(actionArea, "ProgressText", "0 / 30 스테이지",
            UIScale.FontMd, FontStyles.Normal);
        SetRect(progressText.GetComponent<RectTransform>(),
            new Vector2(0, 25), new Vector2(900, UIScale.RowMd));
        progressText.color = MutedText;

        // ── 우측 아이콘 버튼 컬럼 ────────────────────────────
        //  이벤트 버튼은 없다 — 해당 스테이지에 도착하면 StageSelectUI 가 자동으로 띄운다.
        //  상점 버튼은 되살렸다: 자동으로 뜬 상점을 닫으면 다시 들어갈 길이 없었는데,
        //  상품은 ShopSeed + RefreshCount 로 고정이라 재입장이 아무것도 바꾸지 않는다.
        //  (StageSelectUI 가 상점 스테이지에서만 SetActive(true) 한다)
        const float IBtn = 170f;
        const float IGap =  20f;
        float iBtnStep = IBtn + IGap;

        var abilityBtn = CreateIconButton(actionArea, "AbilityListBtn", "어빌리티", AbilityBtnC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_ability.png");
        SetRightColRect(abilityBtn.GetComponent<RectTransform>(), iBtnStep, IBtn);

        var shopBtn = CreateIconButton(actionArea, "ShopBtn", "상점", ShopBtnC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_shop.png");
        SetRightColRect(shopBtn.GetComponent<RectTransform>(), 0f, IBtn);

        var disassembleBtn = CreateIconButton(actionArea, "DisassembleBtn", "장비 분해", DisassembleBtnC,
            "Assets/_project/3.Textures/Icons/LobbyBtns/btn_disassemble.png");
        SetRightColRect(disassembleBtn.GetComponent<RectTransform>(), -iBtnStep, IBtn);

        // 전투 시작 버튼 (하단) — 빈 슬롯 카드가 잘 보이도록 폭·높이를 줄였다.
        var battleBtn   = CreateButton(actionArea, "BattleStartBtn", "전투 시작", BattleBtnC, UIScale.FontMd);
        var battleBtnRt = battleBtn.GetComponent<RectTransform>();
        battleBtnRt.anchorMin        = new Vector2(0.28f, 0f);
        battleBtnRt.anchorMax        = new Vector2(0.72f, 0f);
        battleBtnRt.anchoredPosition = new Vector2(0, UIScale.BtnMd / 2f + 90f);
        battleBtnRt.sizeDelta        = new Vector2(0, UIScale.BtnMd);
        battleBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // 용병 고용 버튼(HireBtn)은 삭제했다 — 고용은 좌측 빈 배치 슬롯을 눌러서 한다.

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

        SetObj(so, "_stageText",      stageText);
        SetObj(so, "_stageTypeText",  stageTypeText);
        SetObj(so, "_progressText",   progressText);
        SetObj(so, "_progressBar",    progressBarUi);
        SetObj(so, "_abilityListBtn", abilityBtn.GetComponent<Button>());
        SetObj(so, "_shopBtn",        shopBtn.GetComponent<Button>());
        SetObj(so, "_disassembleBtn", disassembleBtn.GetComponent<Button>());
        SetObj(so, "_battleStartBtn", battleBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        return panel;
    }

    // ── 슬롯 구성 ────────────────────────────────────────────

    // HeroCard 의 스탯 텍스트는 TextArea 아래에 있으므로 깊이 탐색이 필요하다.
    static T FindChild<T>(Transform root, string name) where T : Component
        => EditorUIBuilder.FindDeep<T>(root, name);

    static GameObject BuildSlot(GameObject parent, int index, float height)
    {
        var go   = MakeGo($"Slot_{index}", parent);
        var goRt = go.GetComponent<RectTransform>();
        goRt.sizeDelta = new Vector2(DeployW - 8f, height);   // 에디터 기본값 명시 (VLG가 런타임에 재계산)

        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = height;
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
        // 빈 자리는 DeploySlotUI 가 interactable 을 끈다.
        // 기본 disabledColor 는 알파 0.5 라 칸이 반투명해진다 — 흰색(=원본색)으로 둔다.
        cb.disabledColor    = Color.white;
        slotBtn.colors = cb;

        // ── EmptyGroup — "빈 자리" 안내만 ──────────────────────
        //  예전엔 "용병 고용 🪙+500" 버튼이었지만 고용 경로는 런 상점으로 일원화했다.
        //  누를 수 없는 칸이므로 버튼처럼 보이는 요소(아이콘·가격)를 두지 않는다.
        var emptyGo = MakeGo("EmptyGroup", go);
        FullStretch(emptyGo);

        var emptyLabel = CreateTMP(emptyGo, "EmptyLabel", "빈 자리", UIScale.FontMd, FontStyles.Normal);
        emptyLabel.color             = MutedText;
        emptyLabel.alignment         = TextAlignmentOptions.Center;
        emptyLabel.raycastTarget     = false;
        emptyLabel.textWrappingMode  = TMPro.TextWrappingModes.NoWrap;
        FullStretch(emptyLabel.gameObject);

        // 잠긴 슬롯은 "슬롯 잠금" 안내를 띄우지 않고 슬롯 자체를 숨긴다
        // (DeploySlotUI.Setup 이 gameObject.SetActive(false)) — LockedGroup 없음.

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
        => EditorUIBuilder.Go(name, parent, components);

    static void FullStretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static TextMeshProUGUI CreateTMP(GameObject parent, string name, string text,
        float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static GameObject CreateIconButton(GameObject parent, string name, string label,
        Color bgColor, string iconAssetPath)
    {
        // UI 규칙: 누를 수 있는 버튼은 음각 처리.
        // 내용은 반드시 body 아래에 넣어야 눌릴 때 같이 내려간다.
        var go = MakeGo(name, parent);
        EditorUIBuilder.RaisedBtnOn(go, bgColor, out var body);

        // 아이콘 이미지 (위쪽 70%)
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(body.transform, false);
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
        lGo.transform.SetParent(body.transform, false);
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

    // UI 규칙: 누를 수 있는 버튼은 음각 처리 (EditorUIBuilder.RaisedTextBtn)
    static GameObject CreateButton(GameObject parent, string name, string label,
        Color bgColor, float fontSize)
        => EditorUIBuilder.RaisedTextBtn(parent, name, label, fontSize, bgColor).gameObject;

    // ── 스테이지 노드 프리팹 생성 ────────────────────────────

    static StageNodeUI BuildStageNodePrefab()
    {
        var go = new GameObject("StageNodeUI", typeof(RectTransform));
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = StageNodeUI.NodeSize;
        le.preferredHeight = StageNodeUI.NodeSize;
        le.flexibleWidth   = 0f;
        var nodeUi = go.AddComponent<StageNodeUI>();

        // ── 테두리 → 판 ──────────────────────────────────────
        //  ⚠ 테두리는 판의 **앞 형제**로 만든다 (UI 규칙 3)
        //    자식으로 두면 부모 Image 보다 앞에 그려져 판을 덮는다.
        //    먼저 만든 형제가 뒤에 깔리므로, 노드를 꽉 채운 테두리 위에
        //    BorderThick 만큼 작은 판을 얹으면 테가 그만큼 드러난다.
        //
        //  ⚠ 속성 색은 테두리에만 들어간다 — 판은 전부 같은 색이다
        //    (이유는 StageNodeUI.ColBg 주석 참고)
        var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(go.transform, false);
        FullStretch(borderGo);
        var borderImg = borderGo.GetComponent<Image>();
        borderImg.color         = new Color(0.42f, 0.46f, 0.60f);
        borderImg.raycastTarget = false;

        var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        {
            const float T = StageNodeUI.BorderThick;
            var rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2( T,  T);
            rt.offsetMax = new Vector2(-T, -T);
        }
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = StageNodeUI.ColBg;

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

        // ── 타입 라벨 (노드 바로 아래) ───────────────────────
        //  ⚠ 글자 뒤에 판을 깐다
        //    "이벤트" 는 보라색이라 로비 배경에 묻혀 거의 안 보였다.
        //    판 폭은 글자에 맞춰 흐르게 둔다 — 라벨 칸 폭(104)으로 고정하면
        //    노드 간격(78)보다 넓어 옆 노드의 판과 겹친다.
        var plateGo  = MakeGo("LabelPlate", go);
        var plateImg = plateGo.AddComponent<Image>();
        plateImg.color         = new Color(0.05f, 0.055f, 0.10f, 0.88f);
        plateImg.raycastTarget = false;
        var plateRt = plateGo.GetComponent<RectTransform>();
        plateRt.anchorMin        = plateRt.anchorMax = new Vector2(0.5f, 0f);
        plateRt.pivot            = new Vector2(0.5f, 1f);
        plateRt.anchoredPosition = new Vector2(0f, -6f);
        plateRt.sizeDelta        = new Vector2(0f, UIScale.RowSm);

        var plateHlg = plateGo.AddComponent<HorizontalLayoutGroup>();
        plateHlg.childAlignment         = TextAnchor.MiddleCenter;
        plateHlg.padding                = new RectOffset(10, 10, 0, 0);
        plateHlg.childControlWidth      = true;
        plateHlg.childControlHeight     = true;
        plateHlg.childForceExpandWidth  = false;
        plateHlg.childForceExpandHeight = true;

        var plateFit = plateGo.AddComponent<ContentSizeFitter>();
        plateFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        plateFit.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(plateGo.transform, false);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text             = "";
        labelTmp.fontSize         = UIScale.FontSm;
        labelTmp.alignment        = TextAlignmentOptions.Center;
        labelTmp.color            = new Color(0.55f, 0.58f, 0.70f);
        labelTmp.raycastTarget    = false;
        labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
        labelTmp.overflowMode     = TextOverflowModes.Overflow;
        plateGo.SetActive(false);

        // 현재 위치 마커 — 노드 위에 떠서 노드를 "가리키므로" 아래를 향해야 한다.
        // 예전엔 ▲ 글리프였는데 방향이 반대인 데다 기본 폰트에 없는 문자다(UI 규칙 2).
        // EditorUIBuilder.Chevron 으로 그린다. dirDeg = -90 → 아래 방향.
        var markerGo = MakeGo("Marker", go);
        var markerRt = markerGo.GetComponent<RectTransform>();
        markerRt.anchorMin        = markerRt.anchorMax = new Vector2(0.5f, 1f);
        markerRt.pivot            = new Vector2(0.5f, 0f);
        markerRt.anchoredPosition = new Vector2(0f, 10f);
        markerRt.sizeDelta        = new Vector2(28f, 28f);
        EditorUIBuilder.Chevron(markerGo, "Arrow", 28f, -90f, new Color(1.00f, 0.85f, 0.15f));
        markerGo.SetActive(false);

        // 필드 연결
        var so = new SerializedObject(nodeUi);
        so.Update();
        SetObj(so, "_bg",        bgImg);
        SetObj(so, "_border",    borderImg);
        SetObj(so, "_icon",      iconImg);
        SetObj(so, "_label",     labelTmp);
        SetObj(so, "_labelRoot", plateGo);   // 켜고 끄는 대상 = 판 (라벨은 그 자식)
        SetObj(so, "_marker",    markerGo);
        SetObj(so, "_le",        le);
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
        tmp.text          = "›";   // ▶ 는 기본 폰트에 없어 □ 로 나온다 (UI 규칙 2)
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
        => EditorUIBuilder.Center(rt, pos, size);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "BattlePanelCreator");
}
