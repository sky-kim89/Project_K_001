#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroPanelCreator.cs
//  Tools > Project K > Create HeroPanel Prefab
//
//  생성 에셋:
//    Assets/_project/2.Prefabs/UI/Lobby/HeroPanel.prefab
//    Assets/_project/2.Prefabs/UI/Lobby/HeroCard.prefab
//
//  레이아웃 (TopBar 130 ~ NavBar 110 사이):
//    LeftPanel  (430px) — 초상화(260) + 스탯(175) + 장비(140) + 스킬(나머지)
//    VertDivider (2px)
//    RightPanel (나머지) — 헤더(44) + 2열 카드 ScrollView
// ============================================================

public static class HeroPanelCreator
{
    const string PanelPrefabPath = "Assets/_project/2.Prefabs/UI/Lobby/HeroPanel.prefab";
    const string CardPrefabPath  = "Assets/_project/2.Prefabs/UI/Lobby/HeroCard.prefab";

    const float LeftWidth  = 430f;
    const float PortraitH  = 260f;
    const float StatH      = 175f;
    const float EquipH     = 140f;

    static readonly Color BgColor      = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color SectionColor = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color CardColor    = new Color(0.10f, 0.10f, 0.18f, 1f);
    static readonly Color DividerColor = new Color(0.18f, 0.18f, 0.26f, 1f);
    static readonly Color SlotColor    = new Color(0.14f, 0.14f, 0.22f, 1f);
    static readonly Color LabelColor   = new Color(0.60f, 0.60f, 0.70f, 1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Create HeroPanel Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI/Lobby");
        AssetDatabase.Refresh();

        var cardGo    = BuildCardPrefab();
        var cardAsset = PrefabUtility.SaveAsPrefabAsset(cardGo, CardPrefabPath);
        Object.DestroyImmediate(cardGo);

        var panelGo = BuildHeroPanel(cardAsset.GetComponent<HeroCardUI>());
        PrefabUtility.SaveAsPrefabAsset(panelGo, PanelPrefabPath);
        Object.DestroyImmediate(panelGo);

        AssetDatabase.Refresh();
        Debug.Log("[HeroPanelCreator] HeroPanel + HeroCard 프리팹 생성 완료");
    }

    // ============================================================
    //  HeroPanel 루트
    // ============================================================

    static GameObject BuildHeroPanel(HeroCardUI cardPrefab)
    {
        var panel = CreatePanel(null, "HeroPanel", BgColor);
        var pRt   = panel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = new Vector2(0,  110);
        pRt.offsetMax = new Vector2(0, -130);

        var ui = panel.AddComponent<HeroPanelUI>();
        var so = new SerializedObject(ui);

        // ── 왼쪽 패널 (430px) ─────────────────────────────────
        var leftPanel = CreatePanel(panel, "LeftPanel", BgColor);
        {
            var rt = leftPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(LeftWidth, 0);
        }

        BuildPortraitSection(leftPanel, so);
        BuildStatSection    (leftPanel, so);
        BuildEquipSection   (leftPanel, so);
        BuildSkillSection   (leftPanel, so);
        BuildPortraitPreview(leftPanel, so);

        // ── 세로 구분선 (2px) ─────────────────────────────────
        var vDiv = CreatePanel(panel, "VertDivider", DividerColor);
        {
            var rt = vDiv.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(LeftWidth,      0);
            rt.offsetMax = new Vector2(LeftWidth + 2f, 0);
        }

        // ── 오른쪽 패널 (나머지 너비) ─────────────────────────
        var rightPanel = CreatePanel(panel, "RightPanel", BgColor);
        {
            var rt = rightPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(LeftWidth + 2f, 0);
            rt.offsetMax = new Vector2(0, 0);
        }

        var listContent = BuildCardListSection(rightPanel);
        SetObj(so, "_listContent", listContent);
        SetObj(so, "_cardPrefab",  cardPrefab);

        so.ApplyModifiedProperties();
        return panel;
    }

    // ── 초상화 섹션 (상단 260px) ──────────────────────────────

    static void BuildPortraitSection(GameObject left, SerializedObject so)
    {
        var section = CreatePanel(left, "PortraitSection", new Color(0.07f, 0.07f, 0.13f));
        {
            var rt = section.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -PortraitH);
            rt.offsetMax = new Vector2(0, 0);
        }

        // 직업 배경색
        var portraitBg = CreateImage(section, "PortraitBg", new Color(0.16f, 0.27f, 0.56f));
        {
            var rt = portraitBg.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 44);
            rt.offsetMax = new Vector2(-8, -8);
        }
        SetObj(so, "_portraitBg", portraitBg);

        // 초상화 이미지 (중앙)
        var portraitImg = CreateImage(section, "PortraitImage", Color.clear);
        {
            var rt = portraitImg.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 20f);
            rt.sizeDelta        = new Vector2(190, 190);
        }
        portraitImg.preserveAspect = true;
        SetObj(so, "_portraitImage", portraitImg);

        // ── 하단 오버레이 (이름·레벨·등급·직업) ──────────────

        // 이름 (하단에서 20~42px)
        var nameText = CreateTMP(section, "NameText", "영웅 이름", 22, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(12, 20);
            rt.offsetMax = new Vector2(-12, 42);
        }
        nameText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_nameText", nameText);

        // 레벨 (하단에서 0~20px 왼쪽 절반)
        var levelText = CreateTMP(section, "LevelText", "Lv.1", 17, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(12, 1);
            rt.offsetMax = new Vector2(0, 19);
        }
        levelText.alignment = TextAlignmentOptions.Left;
        levelText.color     = LabelColor;
        SetObj(so, "_levelText", levelText);

        // 직업 (하단에서 0~20px 오른쪽 절반)
        var jobText = CreateTMP(section, "JobText", "기사", 17, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(1,    0);
            rt.offsetMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(-12, 19);
        }
        jobText.alignment = TextAlignmentOptions.Right;
        jobText.color     = LabelColor;
        SetObj(so, "_jobText", jobText);

        // 등급 배지 (우측 상단 모서리)
        var gradeBadge = CreateImage(section, "GradeBadge", new Color(0.55f, 0.55f, 0.55f));
        {
            var rt = gradeBadge.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-88, -38);
            rt.offsetMax = new Vector2(-8,  -8);
        }
        SetObj(so, "_gradeBadge", gradeBadge);

        // 등급 텍스트 (배지 위)
        var gradeText = CreateTMP(section, "GradeText", "일반", 16, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-88, -38);
            rt.offsetMax = new Vector2(-8,  -8);
        }
        SetObj(so, "_gradeText", gradeText);
    }

    // ── PortraitPreview (숨겨진 캐릭터 빌더 렌더링 오브젝트) ──

    static void BuildPortraitPreview(GameObject left, SerializedObject so)
    {
        var preview = new GameObject("PortraitPreview", typeof(RectTransform));
        preview.transform.SetParent(left.transform, false);
        preview.SetActive(false);

        var bridge = preview.AddComponent<UnitAppearanceBridge>();
        SetObj(so, "_portraitBridge", bridge);
    }

    // ── 스탯 섹션 (260~435px) ─────────────────────────────────

    static void BuildStatSection(GameObject left, SerializedObject so)
    {
        float top = PortraitH;
        var section = CreatePanel(left, "StatSection", SectionColor);
        {
            var rt = section.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(top + StatH));
            rt.offsetMax = new Vector2(0, -top);
        }

        var title = CreateTMP(section, "Title", "스탯", 17, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -28);
            rt.offsetMax = new Vector2(-12, 0);
        }
        title.alignment = TextAlignmentOptions.Left;
        title.color     = LabelColor;

        // 2열 배치: 왼쪽(-100) / 오른쪽(+100), 상대 y 위치
        SetObj(so, "_hpText",          BuildStatRow(section, "HP",   "HP",   new Vector2(-100f, -60f)));
        SetObj(so, "_atkText",         BuildStatRow(section, "ATK",  "공격", new Vector2( 100f, -60f)));
        SetObj(so, "_defText",         BuildStatRow(section, "DEF",  "방어", new Vector2(-100f, -98f)));
        SetObj(so, "_spdText",         BuildStatRow(section, "SPD",  "속도", new Vector2( 100f, -98f)));
        SetObj(so, "_soldierCountText", BuildStatRow(section, "SOLD", "병사수", new Vector2(0f, -138f)));
    }

    static TextMeshProUGUI BuildStatRow(GameObject parent, string id, string label, Vector2 pos)
    {
        var row = new GameObject($"Stat_{id}", typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        SetRect(row.GetComponent<RectTransform>(), pos, new Vector2(188f, 30f));

        var lbl = CreateTMP(row, "Label", label, 15, FontStyles.Normal);
        SetRect(lbl.rectTransform, new Vector2(-42f, 0), new Vector2(82f, 28f));
        lbl.alignment = TextAlignmentOptions.Right;
        lbl.color     = LabelColor;

        var val = CreateTMP(row, "Value", "—", 17, FontStyles.Bold);
        SetRect(val.rectTransform, new Vector2(52f, 0), new Vector2(96f, 28f));
        val.alignment = TextAlignmentOptions.Left;

        return val;
    }

    // ── 장비 섹션 (435~575px) ────────────────────────────────

    static void BuildEquipSection(GameObject left, SerializedObject so)
    {
        float top = PortraitH + StatH;
        var section = CreatePanel(left, "EquipSection", SectionColor);
        {
            var rt = section.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(top + EquipH));
            rt.offsetMax = new Vector2(0, -top);
        }

        var title = CreateTMP(section, "Title", "장비", 17, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -28);
            rt.offsetMax = new Vector2(-12, 0);
        }
        title.alignment = TextAlignmentOptions.Left;
        title.color     = LabelColor;

        // 슬롯 0 (top: 34~80px)
        var (btn0, name0, bar0) = BuildEquipSlot(section, "EquipSlot0", 34f);
        SetObj(so, "_equip0Btn",      btn0);
        SetObj(so, "_equip0NameText", name0);
        SetObj(so, "_equip0GradeBar", bar0);

        // 슬롯 1 (top: 86~132px)
        var (btn1, name1, bar1) = BuildEquipSlot(section, "EquipSlot1", 86f);
        SetObj(so, "_equip1Btn",      btn1);
        SetObj(so, "_equip1NameText", name1);
        SetObj(so, "_equip1GradeBar", bar1);
    }

    static (Button btn, TextMeshProUGUI nameText, Image gradeBar)
        BuildEquipSlot(GameObject parent, string name, float offsetFromTop)
    {
        var slotBg = CreatePanel(parent, name, SlotColor);
        {
            var rt = slotBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8,  -(offsetFromTop + 46f));
            rt.offsetMax = new Vector2(-8, -offsetFromTop);
        }

        var btn = slotBg.AddComponent<Button>();
        btn.targetGraphic = slotBg.GetComponent<Image>();

        // 등급 컬러 바 (좌측 6px)
        var gradeBar = CreateImage(slotBg, "GradeBar", DividerColor);
        {
            var rt = gradeBar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 3);
            rt.offsetMax = new Vector2(6, -3);
        }

        // 장비 이름
        var nameText = CreateTMP(slotBg, "EquipNameText", "없음", 18, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(14, -14);
            rt.offsetMax = new Vector2(-8,  14);
        }
        nameText.alignment = TextAlignmentOptions.Left;

        return (btn, nameText, gradeBar);
    }

    // ── 스킬 섹션 (575px ~ 하단) ─────────────────────────────

    static void BuildSkillSection(GameObject left, SerializedObject so)
    {
        float top = PortraitH + StatH + EquipH;
        var section = CreatePanel(left, "SkillSection", SectionColor);
        {
            var rt = section.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -top);
        }

        var title = CreateTMP(section, "Title", "스킬", 17, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -28);
            rt.offsetMax = new Vector2(-12, 0);
        }
        title.alignment = TextAlignmentOptions.Left;
        title.color     = LabelColor;

        var skillText = CreateTMP(section, "ActiveSkillText", "—", 20, FontStyles.Bold);
        {
            var rt = skillText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -66);
            rt.offsetMax = new Vector2(-12, -32);
        }
        skillText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_activeSkillText", skillText);
    }

    // ── 오른쪽 카드 리스트 (2열 ScrollView) ──────────────────

    static Transform BuildCardListSection(GameObject right)
    {
        // 타이틀 헤더 (44px)
        var header = CreatePanel(right, "Header", BgColor);
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -44);
            rt.offsetMax = new Vector2(0, 0);
        }
        var headerText = CreateTMP(header, "Title", "영웅 목록", 20, FontStyles.Bold);
        {
            var rt = headerText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(16, 0);
            rt.offsetMax = new Vector2(200, 0);
        }
        headerText.alignment = TextAlignmentOptions.Left;

        // ScrollRect
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(right.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -44);
        }

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal   = false;
        scroll.vertical     = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport);
        viewport.GetComponent<Image>().color             = Color.clear;
        viewport.GetComponent<Mask>().showMaskGraphic    = false;

        // Content (GridLayoutGroup 2열)
        var content = new GameObject("Content",
            typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        {
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(300, 180);
        grid.spacing         = new Vector2(10, 10);
        grid.padding         = new RectOffset(10, 10, 10, 10);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport                    = viewport.GetComponent<RectTransform>();
        scroll.content                     = content.GetComponent<RectTransform>();
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return content.transform;
    }

    // ============================================================
    //  HeroCard 프리팹 (2열 배치용 — 300×180px)
    // ============================================================

    static GameObject BuildCardPrefab()
    {
        var card = CreatePanel(null, "HeroCard", CardColor);
        SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(300, 180));

        // 등급 테두리 (전체 아웃라인)
        var border = CreateImage(card, "GradeBorder", new Color(0.55f, 0.55f, 0.55f));
        {
            var rt = border.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        border.type = Image.Type.Sliced;

        // 내부 배경 (테두리 3px 안쪽)
        var inner = CreatePanel(card, "InnerBg", CardColor);
        {
            var rt = inner.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3, 3);
            rt.offsetMax = new Vector2(-3, -3);
        }

        // 이름 텍스트
        var nameText = CreateTMP(card, "NameText", "이름", 20, FontStyles.Bold);
        SetRect(nameText.rectTransform, new Vector2(0, 30), new Vector2(280, 32));

        // 레벨 텍스트
        var levelText = CreateTMP(card, "LevelText", "Lv.1", 17, FontStyles.Normal);
        SetRect(levelText.rectTransform, new Vector2(-60, -6), new Vector2(120, 28));
        levelText.color = LabelColor;

        // 등급 텍스트
        var gradeText = CreateTMP(card, "GradeText", "일반", 17, FontStyles.Bold);
        SetRect(gradeText.rectTransform, new Vector2(70, -6), new Vector2(120, 28));
        gradeText.color = new Color(0.55f, 0.55f, 0.55f);

        // 버튼 (전체 투명 오버레이)
        var btn = card.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();

        var cardUI = card.AddComponent<HeroCardUI>();
        var cSo    = new SerializedObject(cardUI);
        SetObj(cSo, "_gradeBorder", border);
        SetObj(cSo, "_nameText",    nameText);
        SetObj(cSo, "_levelText",   levelText);
        SetObj(cSo, "_gradeText",   gradeText);
        SetObj(cSo, "_button",      btn);
        cSo.ApplyModifiedProperties();

        return card;
    }

    // ============================================================
    //  UI 헬퍼
    // ============================================================

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Image CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CreateTMP(GameObject parent, string name,
                                     string text, float size, FontStyles style)
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

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

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
        else Debug.LogWarning($"[HeroPanelCreator] 필드를 찾을 수 없음: {field}");
    }
}
#endif
