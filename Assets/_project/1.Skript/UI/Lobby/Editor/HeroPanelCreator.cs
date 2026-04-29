#if UNITY_EDITOR
using System.IO;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
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
    const float StatH      = 250f;
    const float EquipH     = 140f;

    static readonly Color BgColor        = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color SectionColor   = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color CardColor      = new Color(0.14f, 0.14f, 0.24f, 1f);  // 배경보다 밝게
    static readonly Color CardBorderColor= new Color(0.28f, 0.28f, 0.42f, 1f);  // 카드 테두리
    static readonly Color DividerColor   = new Color(0.18f, 0.18f, 0.26f, 1f);
    static readonly Color SlotColor      = new Color(0.14f, 0.14f, 0.22f, 1f);
    static readonly Color LabelColor     = new Color(0.60f, 0.60f, 0.70f, 1f);

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

        // DB 직접 참조 (로비에서 Current 없이도 동작하도록)
        var activeDb = AssetDatabase.LoadAssetAtPath<ActiveSkillDatabase>(
            "Assets/_project/ActiveSkillDatabase.asset");
        if (activeDb != null) SetObj(so, "_activeSkillDatabase", activeDb);

        var passiveDb = AssetDatabase.LoadAssetAtPath<PassiveSkillDatabase>(
            "Assets/_project/PassiveSkillDatabase.asset");
        if (passiveDb != null) SetObj(so, "_passiveSkillDatabase", passiveDb);

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

        // 이름 (하단에서 24~56px)
        var nameText = CreateTMP(section, "NameText", "영웅 이름", 30, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(12, 24);
            rt.offsetMax = new Vector2(-12, 56);
        }
        nameText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_nameText", nameText);

        // 레벨 (하단에서 0~24px 왼쪽 절반)
        var levelText = CreateTMP(section, "LevelText", "Lv.1", 22, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(12, 1);
            rt.offsetMax = new Vector2(0, 23);
        }
        levelText.alignment = TextAlignmentOptions.Left;
        levelText.color     = LabelColor;
        SetObj(so, "_levelText", levelText);

        // 직업 (하단에서 0~24px 오른쪽 절반)
        var jobText = CreateTMP(section, "JobText", "기사", 22, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(1,    0);
            rt.offsetMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(-12, 23);
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
            rt.offsetMin = new Vector2(-100, -44);
            rt.offsetMax = new Vector2(-8,   -8);
        }
        SetObj(so, "_gradeBadge", gradeBadge);

        // 등급 텍스트 (배지 위)
        var gradeText = CreateTMP(section, "GradeText", "일반", 20, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-100, -44);
            rt.offsetMax = new Vector2(-8,   -8);
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

        // [RequireComponent] 로 자동 추가된 CharacterBuilder 에 SpriteCollection 할당
        // (없으면 런타임에서 Resources.Load 로 폴백되지만, 미리 할당해두면 로드 생략)
        var builder = preview.GetComponent<CharacterBuilder>();
        if (builder != null)
        {
            var sc = AssetDatabase.LoadAssetAtPath<SpriteCollection>(
                "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset");
            if (sc != null) builder.SpriteCollection = sc;
        }

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

        var title = CreateTMP(section, "Title", "스탯", 22, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -32);
            rt.offsetMax = new Vector2(-12, 0);
        }
        title.alignment = TextAlignmentOptions.Left;
        title.color     = LabelColor;

        // 2열 × 4행: 체력·공격 / 방어율·이속 / 공속·사거리 / 용병수·지휘력
        SetObj(so, "_hpText",           BuildStatRow(section, "HP",   "체력",   new Vector2(-100f, +74f)));
        SetObj(so, "_atkText",          BuildStatRow(section, "ATK",  "공격",   new Vector2( 100f, +74f)));
        SetObj(so, "_defText",          BuildStatRow(section, "DEF",  "방어율", new Vector2(-100f, +24f)));
        SetObj(so, "_spdText",          BuildStatRow(section, "SPD",  "이속",   new Vector2( 100f, +24f)));
        SetObj(so, "_atkSpdText",       BuildStatRow(section, "ASPD", "공속",   new Vector2(-100f, -26f)));
        SetObj(so, "_rangeText",        BuildStatRow(section, "RNG",  "사거리", new Vector2( 100f, -26f)));
        SetObj(so, "_soldierCountText", BuildStatRow(section, "SOLD", "용병수", new Vector2(-100f, -76f)));
        SetObj(so, "_cmdPwrText",       BuildStatRow(section, "CMD",  "지휘력", new Vector2( 100f, -76f)));
    }

    static TextMeshProUGUI BuildStatRow(GameObject parent, string id, string label, Vector2 pos)
    {
        var row = new GameObject($"Stat_{id}", typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);
        SetRect(row.GetComponent<RectTransform>(), pos, new Vector2(188f, 30f));

        var lbl = CreateTMP(row, "Label", label, 19, FontStyles.Normal);
        SetRect(lbl.rectTransform, new Vector2(-42f, 0), new Vector2(82f, 32f));
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.color     = LabelColor;

        var val = CreateTMP(row, "Value", "—", 21, FontStyles.Bold);
        SetRect(val.rectTransform, new Vector2(52f, 0), new Vector2(96f, 32f));
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

        var title = CreateTMP(section, "Title", "장비", 22, FontStyles.Bold);
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
        var nameText = CreateTMP(slotBg, "EquipNameText", "없음", 22, FontStyles.Bold);
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

        var title = CreateTMP(section, "Title", "스킬", 22, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -28);
            rt.offsetMax = new Vector2(-12, 0);
        }
        title.alignment = TextAlignmentOptions.Left;
        title.color     = LabelColor;

        // 액티브 레이블
        var activeLabel = CreateTMP(section, "ActiveLabel", "액티브", 17, FontStyles.Normal);
        {
            var rt = activeLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -48);
            rt.offsetMax = new Vector2(-12, -30);
        }
        activeLabel.alignment = TextAlignmentOptions.Left;
        activeLabel.color     = LabelColor;

        // 스킬 이름
        var skillText = CreateTMP(section, "ActiveSkillText", "—", 22, FontStyles.Bold);
        {
            var rt = skillText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -70);
            rt.offsetMax = new Vector2(-12, -48);
        }
        skillText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_activeSkillText", skillText);

        // 스킬 설명
        var descText = CreateTMP(section, "ActiveSkillDescText", "", 16, FontStyles.Normal);
        {
            var rt = descText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -128);
            rt.offsetMax = new Vector2(-12, -74);
        }
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color     = LabelColor;
        SetObj(so, "_activeSkillDescText", descText);

        // 패시브 레이블
        var passiveLabel = CreateTMP(section, "PassiveLabel", "패시브", 17, FontStyles.Normal);
        {
            var rt = passiveLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -150);
            rt.offsetMax = new Vector2(-12, -132);
        }
        passiveLabel.alignment = TextAlignmentOptions.Left;
        passiveLabel.color     = LabelColor;

        // 패시브 0
        var passive0 = CreateTMP(section, "Passive0Text", "—", 20, FontStyles.Bold);
        {
            var rt = passive0.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, -174);
            rt.offsetMax = new Vector2(-12, -152);
        }
        passive0.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_passive0Text", passive0);

        // 패시브 1
        var passive1 = CreateTMP(section, "Passive1Text", "—", 20, FontStyles.Bold);
        {
            var rt = passive1.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, -198);
            rt.offsetMax = new Vector2(-12, -176);
        }
        passive1.alignment = TextAlignmentOptions.Left;
        passive1.color     = new Color(0.40f, 0.40f, 0.40f);
        SetObj(so, "_passive1Text", passive1);

        // 패시브 2
        var passive2 = CreateTMP(section, "Passive2Text", "—", 20, FontStyles.Bold);
        {
            var rt = passive2.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, -222);
            rt.offsetMax = new Vector2(-12, -200);
        }
        passive2.alignment = TextAlignmentOptions.Left;
        passive2.color     = new Color(0.40f, 0.40f, 0.40f);
        SetObj(so, "_passive2Text", passive2);
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
        var headerText = CreateTMP(header, "Title", "영웅 목록", 24, FontStyles.Bold);
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
        viewport.GetComponent<Image>().color             = Color.white;  // clear 이면 스텐실 미기록 → 자식 불가시
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

        // 등급 테두리 (전체 아웃라인) — Simple 타입
        var border = CreateImage(card, "GradeBorder", CardBorderColor);
        {
            var rt = border.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        border.type = Image.Type.Simple;

        // 내부 배경 (테두리 3px 안쪽)
        var inner = CreatePanel(card, "InnerBg", CardColor);
        {
            var rt = inner.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3, 3);
            rt.offsetMax = new Vector2(-3, -3);
        }

        // ── 초상화 영역 (왼쪽 100px) ─────────────────────────
        // 직업 배경색
        var portraitBg = CreateImage(card, "PortraitBg", new Color(0.16f, 0.27f, 0.56f));
        {
            var rt = portraitBg.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(4,   4);
            rt.offsetMax = new Vector2(104, -4);
        }

        // 초상화 이미지
        var portraitImg = CreateImage(card, "PortraitImage", Color.clear);
        {
            var rt = portraitImg.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(4,   4);
            rt.offsetMax = new Vector2(104, -4);
        }
        portraitImg.preserveAspect = true;

        // ── 텍스트 영역 (오른쪽, x: 108~292) ─────────────────
        // 이름 (상단 절반)
        var nameText = CreateTMP(card, "NameText", "이름", 22, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(104f / 300f, 0.5f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(8,  4);
            rt.offsetMax = new Vector2(-8, -4);
        }
        nameText.alignment = TextAlignmentOptions.BottomLeft;

        // 레벨 (하단 왼쪽)
        var levelText = CreateTMP(card, "LevelText", "Lv.1", 19, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(104f / 300f, 0f);
            rt.anchorMax = new Vector2(0.63f, 0.5f);
            rt.offsetMin = new Vector2(8,  6);
            rt.offsetMax = new Vector2(0, -4);
        }
        levelText.alignment = TextAlignmentOptions.Left;
        levelText.color = LabelColor;

        // 등급 (하단 오른쪽)
        var gradeText = CreateTMP(card, "GradeText", "일반", 19, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(0.63f, 0f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(0,  6);
            rt.offsetMax = new Vector2(-8, -4);
        }
        gradeText.alignment = TextAlignmentOptions.Right;
        gradeText.color = new Color(0.55f, 0.55f, 0.55f);

        // 버튼 (전체 투명 오버레이)
        var btn = card.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();

        // ── PortraitPreview GO (CharacterBuilder + SpriteCollection) ──
        var portraitPreview = new GameObject("PortraitPreview", typeof(RectTransform));
        portraitPreview.transform.SetParent(card.transform, false);
        portraitPreview.SetActive(false);

        var portraitBridge  = portraitPreview.AddComponent<UnitAppearanceBridge>();
        var portraitBuilder = portraitPreview.GetComponent<CharacterBuilder>();
        if (portraitBuilder != null)
        {
            var sc = AssetDatabase.LoadAssetAtPath<SpriteCollection>(
                "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset");
            if (sc != null) portraitBuilder.SpriteCollection = sc;
        }

        var cardUI = card.AddComponent<HeroCardUI>();
        var cSo    = new SerializedObject(cardUI);
        SetObj(cSo, "_gradeBorder",    border);
        SetObj(cSo, "_portraitBg",     portraitBg);
        SetObj(cSo, "_portraitImage",  portraitImg);
        SetObj(cSo, "_portraitBridge", portraitBridge);
        SetObj(cSo, "_nameText",       nameText);
        SetObj(cSo, "_levelText",      levelText);
        SetObj(cSo, "_gradeText",      gradeText);
        SetObj(cSo, "_button",         btn);
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
