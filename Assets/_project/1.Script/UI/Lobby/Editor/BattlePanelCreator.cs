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
    const string SavePath = "Assets/_project/2.Prefabs/UI/Lobby/BattlePanel.prefab";

    const float TopBarH     = 180f;
    const float NavBarH     = 160f;
    const float DeployW     = 420f;   // 좌측 배치 슬롯 너비
    const float SlotGap     = 8f;
    const int   SlotCount   = 5;

    static readonly Color BgDark       = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color SlotBgEmpty  = new Color(0.12f, 0.12f, 0.20f, 1f);
    static readonly Color SlotBgOccup  = new Color(0.12f, 0.15f, 0.23f, 1f);
    static readonly Color GradePlaceholder = new Color(0.25f, 0.25f, 0.35f, 1f);
    static readonly Color PortraitBg   = new Color(0.12f, 0.12f, 0.20f, 1f);
    static readonly Color ActionBg     = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color BattleBtnC   = new Color(0.11f, 0.72f, 0.58f, 1f);
    static readonly Color AbilityBtnC  = new Color(0.18f, 0.25f, 0.45f, 1f);
    static readonly Color HireBtnC     = new Color(0.45f, 0.25f, 0.18f, 1f);
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
        panelRt.offsetMin = new Vector2(0,  NavBarH);
        panelRt.offsetMax = new Vector2(0, -TopBarH);

        var ui = panel.AddComponent<StageSelectUI>();

        // ── DeployArea (좌측) ─────────────────────────────────
        var deployArea = MakeGo("DeployArea", panel);
        var deployRt   = deployArea.GetComponent<RectTransform>();
        deployRt.anchorMin = new Vector2(0, 0);
        deployRt.anchorMax = new Vector2(0, 1);
        deployRt.offsetMin = Vector2.zero;
        deployRt.offsetMax = new Vector2(DeployW, 0);

        var deployBg = deployArea.AddComponent<Image>();
        deployBg.color = BgDark;

        // VerticalLayoutGroup으로 5 슬롯을 균등 분할
        var vlg = deployArea.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.spacing               = SlotGap;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.padding               = new RectOffset(4, 4, 4, 4);

        float slotH = (UIScale.RefHeight - TopBarH - NavBarH
                       - SlotGap * (SlotCount - 1) - 8f) / SlotCount;

        var slots = new DeploySlotUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var slot = BuildSlot(deployArea, i, slotH);
            slots[i] = slot.GetComponent<DeploySlotUI>();
        }

        // ── ActionArea (우측) ─────────────────────────────────
        var actionArea = MakeGo("ActionArea", panel);
        var actionRt   = actionArea.GetComponent<RectTransform>();
        actionRt.anchorMin = new Vector2(0, 0);
        actionRt.anchorMax = new Vector2(1, 1);
        actionRt.offsetMin = new Vector2(DeployW, 0);
        actionRt.offsetMax = Vector2.zero;

        var actionBg = actionArea.AddComponent<Image>();
        actionBg.color = ActionBg;

        // 스테이지 텍스트 (상단 중앙)
        var stageText = CreateTMP(actionArea, "StageText", "스테이지 1 도전",
            UIScale.FontLg, FontStyles.Bold);
        SetRect(stageText.GetComponent<RectTransform>(),
            new Vector2(0, 220), new Vector2(1100, 80));

        var progressText = CreateTMP(actionArea, "ProgressText", "첫 번째 스테이지",
            UIScale.FontMd, FontStyles.Normal);
        SetRect(progressText.GetComponent<RectTransform>(),
            new Vector2(0, 150), new Vector2(1000, 56));
        progressText.color = MutedText;

        // 어빌리티·용병 버튼 (중간)
        var abilityBtn = CreateButton(actionArea, "AbilityListBtn", "어빌리티 목록", AbilityBtnC, UIScale.FontMd);
        SetRect(abilityBtn.GetComponent<RectTransform>(),
            new Vector2(-220, 20), new Vector2(380, UIScale.BtnSm));

        var hireBtn = CreateButton(actionArea, "HireBtn", "용병 구매", HireBtnC, UIScale.FontMd);
        SetRect(hireBtn.GetComponent<RectTransform>(),
            new Vector2( 220, 20), new Vector2(380, UIScale.BtnSm));

        // 전투 시작 버튼 (하단)
        var battleBtn   = CreateButton(actionArea, "BattleStartBtn", "전투 시작", BattleBtnC, UIScale.FontLg);
        var battleBtnRt = battleBtn.GetComponent<RectTransform>();
        battleBtnRt.anchorMin        = new Vector2(0.08f, 0f);
        battleBtnRt.anchorMax        = new Vector2(0.92f, 0f);
        battleBtnRt.anchoredPosition = new Vector2(0, UIScale.BtnLg / 2f + 40f);
        battleBtnRt.sizeDelta        = new Vector2(0, UIScale.BtnLg);
        battleBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

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
        SetObj(so, "_progressText",   progressText);
        SetObj(so, "_abilityListBtn", abilityBtn.GetComponent<Button>());
        SetObj(so, "_hireBtn",        hireBtn.GetComponent<Button>());
        SetObj(so, "_battleStartBtn", battleBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        return panel;
    }

    // ── 슬롯 구성 ────────────────────────────────────────────

    static T FindChild<T>(Transform root, string name) where T : Component
        => root.Find(name)?.GetComponent<T>();

    static GameObject BuildSlot(GameObject parent, int index, float height)
    {
        var go   = MakeGo($"Slot_{index}", parent);
        var goRt = go.GetComponent<RectTransform>();
        goRt.sizeDelta = new Vector2(360f, 160f);   // 에디터 기본값 명시 (VLG가 런타임에 재계산)

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

        // ── EmptyGroup ───────────────────────────────────────
        var emptyGo = MakeGo("EmptyGroup", go);
        FullStretch(emptyGo);
        var emptyLabel = CreateTMP(emptyGo, "EmptyLabel", $"+ 슬롯 {index + 1} 비어 있음",
            UIScale.FontMd, FontStyles.Normal);
        {
            var rt = emptyLabel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        emptyLabel.color = MutedText;

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

    // 중앙 앵커 기반 TMP (OccupiedGroup 내 정보 텍스트용)
    // position.x = 중앙에서 오른쪽 offset, position.y = 중앙에서 위/아래 offset
    static TextMeshProUGUI CreateTMPAt(GameObject parent, string name, string text,
        float size, FontStyles style, Vector2 position, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0.5f);
        rt.anchorMax        = new Vector2(0, 0.5f);
        rt.pivot            = new Vector2(0, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta        = sizeDelta;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = size;
        tmp.fontStyle        = style;
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.color            = Color.white;
        tmp.overflowMode     = TextOverflowModes.Ellipsis;
        return tmp;
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
