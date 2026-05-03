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
//  레이아웃 (Preset B — 좌측 탭 분리):
//    LeftPanel (430px)
//      PortraitSection (200px)  — 초상화 + 이름·레벨·직업·등급
//      TabBar          (44px)   — [스탯] [장비] [스킬] 버튼
//      TabContentArea  (나머지) — StatPanel / EquipPanel / SkillPanel (SetActive 전환)
//    VertDivider (2px)
//    RightPanel (나머지 너비)  — 헤더 + 카드 ScrollView
// ============================================================

public static class HeroPanelCreator
{
    const string PanelPrefabPath     = "Assets/_project/2.Prefabs/UI/Lobby/HeroPanel.prefab";
    const string CardPrefabPath      = "Assets/_project/2.Prefabs/UI/Lobby/HeroCard.prefab";
    const string EquipCardPrefabPath = "Assets/_project/2.Prefabs/UI/Lobby/EquipCard.prefab";

    const float LeftWidth   = 430f;
    const float PortraitH   = 240f;
    const float DeployRowH  = 52f;   // 배치 슬롯 행 높이
    const float TabBarH     = 44f;

    static readonly Color BgColor          = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color SectionColor    = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color TabBarColor     = new Color(0.10f, 0.12f, 0.22f, 1f);
    static readonly Color CardColor       = new Color(0.14f, 0.14f, 0.24f, 1f);
    static readonly Color CardBorderColor = new Color(0.28f, 0.28f, 0.42f, 1f);
    static readonly Color DividerColor    = new Color(0.18f, 0.18f, 0.26f, 1f);
    static readonly Color SlotColor       = new Color(0.14f, 0.14f, 0.22f, 1f);
    static readonly Color LabelColor      = new Color(0.60f, 0.60f, 0.70f, 1f);
    static readonly Color TabActiveColor  = new Color(0.40f, 0.72f, 1.00f, 1f);
    static readonly Color TabInactiveColor= new Color(0.72f, 0.72f, 0.78f, 1f);
    static readonly Color LevelUpBtnColor = new Color(0.16f, 0.32f, 0.58f, 1f);
    static readonly Color DeploySlotEmpty = new Color(0.20f, 0.20f, 0.23f, 1f);
    static readonly Color DeploySlotMine  = new Color(0.22f, 0.54f, 0.92f, 1f);
    static readonly Color DeployBadgeColor= new Color(0.22f, 0.54f, 0.92f, 0.90f);

    // 폰트 크기 — UIScale 기준 (모바일 1080×1920)
    static readonly int FntHero = (int)UIScale.FontLg;   // 54 — 영웅 이름 헤더
    static readonly int FntMain = (int)UIScale.FontMd;   // 40 — 탭·버튼·스탯값·제목
    static readonly int FntSub  = (int)UIScale.FontSm;   // 30 — 레이블·보조텍스트
    static readonly int FntMini = 24;                    // 24 — 배지 등 극소 텍스트

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/로비 UI/Create HeroPanel Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI/Lobby");
        AssetDatabase.Refresh();

        var equipCardGo    = BuildEquipCardPrefab();
        var equipCardAsset = PrefabUtility.SaveAsPrefabAsset(equipCardGo, EquipCardPrefabPath);
        Object.DestroyImmediate(equipCardGo);

        var cardGo    = BuildCardPrefab();
        var cardAsset = PrefabUtility.SaveAsPrefabAsset(cardGo, CardPrefabPath);
        Object.DestroyImmediate(cardGo);

        var panelGo = BuildHeroPanel(cardAsset.GetComponent<HeroCardUI>(),
                                     equipCardAsset.GetComponent<EquipCardUI>());
        PrefabUtility.SaveAsPrefabAsset(panelGo, PanelPrefabPath);
        Object.DestroyImmediate(panelGo);

        AssetDatabase.Refresh();
        Debug.Log("[HeroPanelCreator] HeroPanel + HeroCard + EquipCard 프리팹 생성 완료");
    }

    // ============================================================
    //  HeroPanel 루트
    // ============================================================

    static GameObject BuildHeroPanel(HeroCardUI cardPrefab, EquipCardUI equipCardPrefab)
    {
        var panel = CreatePanel(null, "HeroPanel", BgColor);
        var pRt   = panel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.offsetMin = new Vector2(0,  110);
        pRt.offsetMax = new Vector2(0, -130);

        var ui = panel.AddComponent<HeroPanelUI>();
        var so = new SerializedObject(ui);

        // ── 왼쪽 패널 ─────────────────────────────────────────
        var leftPanel = CreatePanel(panel, "LeftPanel", BgColor);
        {
            var rt = leftPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(LeftWidth, 0);
        }

        BuildPortraitSection(leftPanel, so);
        BuildDeploySlotRow(leftPanel, so);
        BuildTabSection(leftPanel, so);
        BuildPortraitPreview(leftPanel, so);

        // ── 세로 구분선 ───────────────────────────────────────
        var vDiv = CreatePanel(panel, "VertDivider", DividerColor);
        {
            var rt = vDiv.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(LeftWidth,      0);
            rt.offsetMax = new Vector2(LeftWidth + 2f, 0);
        }

        // ── 오른쪽 패널 ───────────────────────────────────────
        var rightPanel = CreatePanel(panel, "RightPanel", BgColor);
        {
            var rt = rightPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(LeftWidth + 2f, 0);
            rt.offsetMax = new Vector2(0, 0);
        }

        var listContent = BuildCardListSection(rightPanel, so);
        SetObj(so, "_listContent",    listContent);
        SetObj(so, "_cardPrefab",     cardPrefab);
        SetObj(so, "_equipCardPrefab", equipCardPrefab);

        var activeDb = AssetDatabase.LoadAssetAtPath<ActiveSkillDatabase>(
            "Assets/_project/ActiveSkillDatabase.asset");
        if (activeDb != null) SetObj(so, "_activeSkillDatabase", activeDb);

        var passiveDb = AssetDatabase.LoadAssetAtPath<PassiveSkillDatabase>(
            "Assets/_project/PassiveSkillDatabase.asset");
        if (passiveDb != null) SetObj(so, "_passiveSkillDatabase", passiveDb);

        so.ApplyModifiedProperties();
        return panel;
    }

    // ── 초상화 섹션 (상단 PortraitH px) ──────────────────────

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

        // 초상화 이미지
        var portraitImg = CreateImage(section, "PortraitImage", Color.clear);
        {
            var rt = portraitImg.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 16f);
            rt.sizeDelta        = new Vector2(150, 150);
        }
        portraitImg.preserveAspect = true;
        SetObj(so, "_portraitImage", portraitImg);

        // 이름
        var nameText = CreateTMP(section, "NameText", "영웅 이름", FntHero, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(12, 22);
            rt.offsetMax = new Vector2(-12, 50);
        }
        nameText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_nameText", nameText);

        // 레벨 (하단 왼쪽)
        var levelText = CreateTMP(section, "LevelText", "Lv.1", FntSub, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(12, 2);
            rt.offsetMax = new Vector2(0, 22);
        }
        levelText.alignment = TextAlignmentOptions.Left;
        levelText.color     = LabelColor;
        SetObj(so, "_levelText", levelText);

        // 직업 (하단 오른쪽)
        var jobText = CreateTMP(section, "JobText", "기사", FntSub, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(1,    0);
            rt.offsetMin = new Vector2(0, 2);
            rt.offsetMax = new Vector2(-12, 22);
        }
        jobText.alignment = TextAlignmentOptions.Right;
        jobText.color     = LabelColor;
        SetObj(so, "_jobText", jobText);

        // 등급 배지
        var gradeBadge = CreateImage(section, "GradeBadge", new Color(0.55f, 0.55f, 0.55f));
        {
            var rt = gradeBadge.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-90, -38);
            rt.offsetMax = new Vector2(-8,  -8);
        }
        SetObj(so, "_gradeBadge", gradeBadge);

        // 등급 텍스트
        var gradeText = CreateTMP(section, "GradeText", "일반", FntSub, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-90, -38);
            rt.offsetMax = new Vector2(-8,  -8);
        }
        SetObj(so, "_gradeText", gradeText);
    }

    // ── PortraitPreview ───────────────────────────────────────

    static void BuildPortraitPreview(GameObject left, SerializedObject so)
    {
        var preview = new GameObject("PortraitPreview", typeof(RectTransform));
        preview.transform.SetParent(left.transform, false);
        preview.SetActive(false);

        var bridge  = preview.AddComponent<UnitAppearanceBridge>();
        var builder = preview.GetComponent<CharacterBuilder>();
        if (builder != null)
        {
            var sc = AssetDatabase.LoadAssetAtPath<SpriteCollection>(
                "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset");
            if (sc != null) builder.SpriteCollection = sc;
        }
        SetObj(so, "_portraitBridge", bridge);
    }

    // ── 배치 슬롯 행 (PortraitSection 바로 아래) ──────────────

    static void BuildDeploySlotRow(GameObject left, SerializedObject so)
    {
        // 행 배경
        var row = CreatePanel(left, "DeploySlotRow", new Color(0.07f, 0.08f, 0.14f));
        {
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(PortraitH + DeployRowH));
            rt.offsetMax = new Vector2(0, -PortraitH);
        }

        // 레이블 "출전 위치"
        var label = CreateTMP(row, "DeployLabel", "출전 위치", FntSub, FontStyles.Normal);
        {
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.offsetMin = new Vector2(10, -DeployRowH);
            rt.offsetMax = new Vector2(130, 0);
        }
        label.alignment = TextAlignmentOptions.Left;
        label.color     = LabelColor;

        // 슬롯 5개: 균등 배치 (레이블 130px 제외, 우측 패딩 8px)
        const float slotPad = 6f;
        float slotsX   = 134f;
        float slotsW   = LeftWidth - slotsX - 8f;
        float slotW    = (slotsW - slotPad * 4f) / 5f;
        float slotH    = DeployRowH - 10f;

        var slotBtns   = new Button[5];
        var slotBgs    = new Image[5];
        var slotNums   = new TextMeshProUGUI[5];

        for (int i = 0; i < 5; i++)
        {
            float sx = slotsX + i * (slotW + slotPad);

            var slotGo = new GameObject($"DeploySlot{i}", typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(row.transform, false);
            var slotImg = slotGo.GetComponent<Image>();
            slotImg.color = DeploySlotEmpty;
            {
                var rt = slotGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot     = new Vector2(0, 0.5f);
                rt.offsetMin = new Vector2(sx, -slotH * 0.5f);
                rt.offsetMax = new Vector2(sx + slotW, slotH * 0.5f);
            }

            // 슬롯 번호 텍스트
            var numTmp = CreateTMP(slotGo, "Num", (i + 1).ToString(), FntSub, FontStyles.Bold);
            {
                var rt = numTmp.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            numTmp.alignment = TextAlignmentOptions.Center;

            var btn = slotGo.AddComponent<Button>();
            btn.targetGraphic = slotImg;

            slotBtns[i] = btn;
            slotBgs[i]  = slotImg;
            slotNums[i] = numTmp;
        }

        // DeploySlotRowUI 컴포넌트 + 필드 연결
        var rowUI = row.AddComponent<DeploySlotRowUI>();
        var rso   = new SerializedObject(rowUI);
        SetObjArray(rso, "_slotButtons",  System.Array.ConvertAll(slotBtns, b => (Object)b));
        SetObjArray(rso, "_slotBgs",      System.Array.ConvertAll(slotBgs,  b => (Object)b));
        SetObjArray(rso, "_slotNumTexts", System.Array.ConvertAll(slotNums, t => (Object)t));
        rso.ApplyModifiedProperties();

        // HeroPanelUI._deploySlotRow 연결
        SetObj(so, "_deploySlotRow", rowUI);
    }

    // ── 탭 섹션 (초상화 아래 전체) ────────────────────────────

    static void BuildTabSection(GameObject left, SerializedObject so)
    {
        // 탭 바 — PortraitSection + DeploySlotRow 아래에 위치
        var tabBar = CreatePanel(left, "TabBar", TabBarColor);
        {
            var rt = tabBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(PortraitH + DeployRowH + TabBarH));
            rt.offsetMax = new Vector2(0, -(PortraitH + DeployRowH));
        }

        // 탭 버튼 3개
        string[] tabLabels = { "스탯", "장비", "스킬" };
        var tabButtons = new Button[3];
        for (int i = 0; i < 3; i++)
            tabButtons[i] = BuildTabButton(tabBar, tabLabels[i], i, i == 0);

        // 탭 콘텐츠 영역 (탭 바 아래 ~ 하단)
        var contentArea = CreatePanel(left, "TabContentArea", SectionColor);
        {
            var rt = contentArea.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -(PortraitH + DeployRowH + TabBarH));
        }

        // 3개 패널 (동일 영역, SetActive 로 전환)
        var statPanel  = BuildStatPanel (contentArea, so);
        var equipPanel = BuildEquipPanel(contentArea, so);
        var skillPanel = BuildSkillPanel(contentArea, so);

        equipPanel.SetActive(false);
        skillPanel.SetActive(false);

        SetObjArray(so, "_tabButtons", new Object[] { tabButtons[0], tabButtons[1], tabButtons[2] });
        SetObjArray(so, "_tabPanels",  new Object[] { statPanel, equipPanel, skillPanel });
    }

    // index 0~2 → 앵커로 1/3씩 균등 분할, 높이는 TabBar 전체 차지
    static Button BuildTabButton(GameObject tabBar, string label, int index, bool isActive)
    {
        float step = 1f / 3f;

        var go  = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(tabBar.transform, false);
        go.GetComponent<Image>().color = Color.clear;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(step * index,       0);
        rt.anchorMax = new Vector2(step * (index + 1), 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = CreateTMP(go, "Label", label, FntMain,
            isActive ? FontStyles.Bold : FontStyles.Normal);
        {
            var trt = tmp.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(0, 3);
            trt.offsetMax = Vector2.zero;
        }
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = isActive ? TabActiveColor : TabInactiveColor;

        // 모든 탭에 ActiveBar 추가 — 비활성 탭은 숨겨두고 SwitchTab()이 토글
        {
            var bar = CreateImage(go, "ActiveBar", TabActiveColor);
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0.1f, 0);
            brt.anchorMax = new Vector2(0.9f, 0);
            brt.offsetMin = new Vector2(0, 0);
            brt.offsetMax = new Vector2(0, 3);
            if (!isActive) bar.gameObject.SetActive(false);
        }

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        return btn;
    }

    // ── StatPanel ─────────────────────────────────────────────

    static GameObject BuildStatPanel(GameObject contentArea, SerializedObject so)
    {
        const float BtnH  = 42f;
        const float BtnPad = 7f;

        var panel = CreatePanel(contentArea, "StatPanel", SectionColor);
        Stretch(panel);

        // 스탯 행 컨테이너 — 하단 레벨업 버튼 영역 제외
        var listContainer = new GameObject("StatListContainer", typeof(RectTransform));
        listContainer.transform.SetParent(panel.transform, false);
        {
            var rt = listContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, BtnH + BtnPad * 2);
            rt.offsetMax = new Vector2(0, 0);
        }

        var vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.padding                = new RectOffset(0, 0, 2, 2);
        vlg.spacing                = 0;

        SetObj(so, "_hpText",           BuildStatRow(listContainer, "HP",   "체력"));
        SetObj(so, "_atkText",          BuildStatRow(listContainer, "ATK",  "공격"));
        SetObj(so, "_defText",          BuildStatRow(listContainer, "DEF",  "방어율"));
        SetObj(so, "_spdText",          BuildStatRow(listContainer, "SPD",  "이속"));
        SetObj(so, "_atkSpdText",       BuildStatRow(listContainer, "ASPD", "공속"));
        SetObj(so, "_rangeText",        BuildStatRow(listContainer, "RNG",  "사거리"));
        SetObj(so, "_soldierCountText", BuildStatRow(listContainer, "SOLD", "용병수"));
        SetObj(so, "_cmdPwrText",       BuildStatRow(listContainer, "CMD",  "지휘력"));

        // 레벨업 버튼 (하단 고정)
        var btnGo = CreatePanel(panel, "LevelUpButton", LevelUpBtnColor);
        {
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(BtnPad, BtnPad);
            rt.offsetMax = new Vector2(-BtnPad, BtnPad + BtnH);
        }

        var levelUpBtn = btnGo.AddComponent<Button>();
        levelUpBtn.targetGraphic = btnGo.GetComponent<Image>();
        SetObj(so, "_levelUpBtn", levelUpBtn);

        // "레벨업" 텍스트 (상단 55%)
        var btnLabel = CreateTMP(btnGo, "Label", "레벨업", FntMain, FontStyles.Bold);
        {
            var rt = btnLabel.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.8f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        btnLabel.alignment = TextAlignmentOptions.Center;

        // 골드 비용 텍스트 (하단 45%)
        var costText = CreateTMP(btnGo, "CostText", "0 G", FntSub, FontStyles.Normal);
        {
            var rt = costText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1.2f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        costText.alignment = TextAlignmentOptions.Center;
        costText.color     = new Color(1.0f, 0.85f, 0.20f);
        SetObj(so, "_levelUpCostText", costText);

        return panel;
    }

    static TextMeshProUGUI BuildStatRow(GameObject parent, string id, string label)
    {
        var row = new GameObject($"Stat_{id}", typeof(RectTransform));
        row.transform.SetParent(parent.transform, false);

        // RectTransform 높이를 직접 50으로 고정 — VLG가 에디터 생성 시점에 레이아웃 패스를 실행하지 않으므로
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(rowRt.sizeDelta.x, 50f);

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 50f;
        le.minHeight       = 44f;

        // 행 내부 HorizontalLayoutGroup — 텍스트 래핑 방지
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.spacing                = 0;
        hlg.padding                = new RectOffset(16, 16, 0, 0);

        // 레이블 (고정 너비 110px)
        var lbl = CreateTMP(row, "Label", label, FntSub, FontStyles.Normal);
        lbl.alignment        = TextAlignmentOptions.Left;
        lbl.color            = LabelColor;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Ellipsis;
        var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 110f;
        lblLE.flexibleWidth  = 0f;

        // 값 (나머지 너비 전부)
        var val = CreateTMP(row, "Value", "—", FntMain, FontStyles.Bold);
        val.alignment          = TextAlignmentOptions.Right;
        val.textWrappingMode = TextWrappingModes.NoWrap;
        val.overflowMode       = TextOverflowModes.Ellipsis;
        val.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // 행 하단 구분선 (HLG 레이아웃에서 제외)
        var line = CreateImage(row, "Divider", DividerColor);
        line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        {
            var rt = line.rectTransform;
            rt.anchorMin = new Vector2(0.02f, 0);
            rt.anchorMax = new Vector2(0.98f, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 1);
        }

        return val;
    }

    // ── EquipPanel ────────────────────────────────────────────

    static GameObject BuildEquipPanel(GameObject contentArea, SerializedObject so)
    {
        var panel = CreatePanel(contentArea, "EquipPanel", SectionColor);
        Stretch(panel);

        var (btn0, name0, bar0, icon0, stat0, lock0) = BuildEquipSlot(panel, "EquipSlot0", 10f, 74f);
        SetObj(so, "_equip0Btn",       btn0);
        SetObj(so, "_equip0NameText",  name0);
        SetObj(so, "_equip0GradeBar",  bar0);
        SetObj(so, "_equip0Icon",      icon0);
        SetObj(so, "_equip0StatText",  stat0);
        SetObj(so, "_equip0LockBadge", lock0);

        var (btn1, name1, bar1, icon1, stat1, lock1) = BuildEquipSlot(panel, "EquipSlot1", 92f, 74f);
        SetObj(so, "_equip1Btn",       btn1);
        SetObj(so, "_equip1NameText",  name1);
        SetObj(so, "_equip1GradeBar",  bar1);
        SetObj(so, "_equip1Icon",      icon1);
        SetObj(so, "_equip1StatText",  stat1);
        SetObj(so, "_equip1LockBadge", lock1);

        BuildEquipSelectPanel(panel, so);
        return panel;
    }

    // 장비 슬롯: [등급바] [아이콘] [이름 / 옵션] [교체시소멸 배지]
    static (Button btn, TextMeshProUGUI nameText, Image gradeBar, Image iconImage, TextMeshProUGUI statText, GameObject lockBadge)
        BuildEquipSlot(GameObject parent, string name, float offsetFromTop, float slotH)
    {
        var slotBg = CreatePanel(parent, name, SlotColor);
        {
            var rt = slotBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(10, -(offsetFromTop + slotH));
            rt.offsetMax = new Vector2(-10, -offsetFromTop);
        }

        var btn = slotBg.AddComponent<Button>();
        btn.targetGraphic = slotBg.GetComponent<Image>();

        // 등급 컬러 바 (좌측 5px)
        var gradeBar = CreateImage(slotBg, "GradeBar", DividerColor);
        {
            var rt = gradeBar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 3);
            rt.offsetMax = new Vector2(5, -3);
        }

        // 아이콘 박스 (좌측 정사각형)
        float iconSize = slotH - 14f;
        var iconBg = CreateImage(slotBg, "IconBg", new Color(0.10f, 0.10f, 0.18f));
        {
            var rt = iconBg.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(10, -iconSize * 0.5f);
            rt.offsetMax = new Vector2(10 + iconSize, iconSize * 0.5f);
        }

        var iconImg = CreateImage(slotBg, "Icon", new Color(0.25f, 0.25f, 0.30f));
        {
            var rt = iconImg.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(14,            -iconSize * 0.5f + 4f);
            rt.offsetMax = new Vector2(14 + iconSize - 8f,  iconSize * 0.5f - 4f);
        }
        iconImg.preserveAspect = true;

        float textLeft = 10f + iconSize + 12f;

        // 장비 이름 (상단 40%)
        var nameText = CreateTMP(slotBg, "EquipNameText", "없음", FntMain, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(textLeft, 2);
            rt.offsetMax = new Vector2(-10, -4);
        }
        nameText.alignment = TextAlignmentOptions.Left;

        // 옵션 텍스트 (하단 60%)
        var statText = CreateTMP(slotBg, "StatText", "", FntSub, FontStyles.Normal);
        {
            var rt = statText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(textLeft, 2);
            rt.offsetMax = new Vector2(-10, 0);
        }
        statText.alignment = TextAlignmentOptions.TopLeft;
        statText.color     = LabelColor;

        // 교체시 소멸 배지 (장비 장착 시 HeroPanelUI 가 활성화)
        var lockBadge = new GameObject("LockBadge", typeof(RectTransform), typeof(Image));
        lockBadge.transform.SetParent(slotBg.transform, false);
        lockBadge.GetComponent<Image>().color = new Color(0.70f, 0.18f, 0.10f, 0.90f);
        {
            var rt = lockBadge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(1, 0);
            rt.offsetMin = new Vector2(-86, 3);
            rt.offsetMax = new Vector2(-4,  19);
        }
        var lockTmp = CreateTMP(lockBadge, "Label", "교체시 소멸", FntMini, FontStyles.Bold);
        {
            var rt = lockTmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3, 0);
            rt.offsetMax = Vector2.zero;
        }
        lockTmp.alignment = TextAlignmentOptions.Center;
        lockBadge.SetActive(false);

        return (btn, nameText, gradeBar, iconImg, statText, lockBadge);
    }

    // ── EquipSelectPanel (EquipPanel 위에 겹쳐 표시) ─────────

    static void BuildEquipSelectPanel(GameObject equipPanel, SerializedObject so)
    {
        var overlay = CreatePanel(equipPanel, "EquipSelectPanel", new Color(0.04f, 0.04f, 0.09f, 0.97f));
        Stretch(overlay);
        overlay.SetActive(false);

        // 헤더 (40px 상단)
        var header = CreatePanel(overlay, "SelectHeader", new Color(0.08f, 0.10f, 0.18f));
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -40);
            rt.offsetMax = Vector2.zero;
        }

        var title = CreateTMP(header, "SelectTitle", "슬롯 1 장비 선택", FntMain, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, 0);
            rt.offsetMax = new Vector2(-50, 0);
        }
        title.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_equipSelectTitle", title);

        // 닫기 버튼
        var closeBtnGo = CreatePanel(header, "CloseBtn", new Color(0.25f, 0.10f, 0.10f));
        {
            var rt = closeBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-44, 4);
            rt.offsetMax = new Vector2(-4, -4);
        }
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
        var closeTmp = CreateTMP(closeBtnGo, "Label", "✕", FntMain, FontStyles.Bold);
        closeTmp.rectTransform.anchorMin = Vector2.zero;
        closeTmp.rectTransform.anchorMax = Vector2.one;
        closeTmp.rectTransform.offsetMin = Vector2.zero;
        closeTmp.rectTransform.offsetMax = Vector2.zero;
        closeTmp.alignment = TextAlignmentOptions.Center;
        SetObj(so, "_equipSelectCloseBtn", closeBtn);

        // 경고 텍스트 (헤더 바로 아래 28px, 기본 비활성)
        var warning = CreateTMP(overlay, "SelectWarning", "⚠ 교체 시 기존 장비가 소멸됩니다", FntSub, FontStyles.Bold);
        {
            var rt = warning.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -70);
            rt.offsetMax = new Vector2(-12, -42);
        }
        warning.alignment = TextAlignmentOptions.Left;
        warning.color     = new Color(1.0f, 0.55f, 0.15f);
        warning.gameObject.SetActive(false);
        SetObj(so, "_equipSelectWarning", warning);

        // ScrollView (헤더 아래 ~ 하단)
        var scrollGo = new GameObject("SelectScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(overlay.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -40);
        }

        var scroll        = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport);
        viewport.GetComponent<Image>().color          = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("SelectContent", typeof(RectTransform),
                                     typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        {
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding                = new RectOffset(6, 6, 4, 4);
        vlg.spacing                = 4;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content  = content.GetComponent<RectTransform>();

        SetObj(so, "_equipSelectPanel",   overlay);
        SetObj(so, "_equipSelectContent", content.transform);
    }

    // ── EquipCard 프리팹 (64px, VLG 에서 세로 나열) ──────────

    static GameObject BuildEquipCardPrefab()
    {
        var card = CreatePanel(null, "EquipCard", new Color(0.12f, 0.12f, 0.20f));
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(400, 64);

        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 64f;
        le.minHeight       = 64f;

        // 등급 바 (좌측 5px)
        var gradeBar = CreateImage(card, "GradeBar", DividerColor);
        {
            var rt = gradeBar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 3);
            rt.offsetMax = new Vector2(5, -3);
        }

        // 아이콘 박스 (좌측 52×52)
        const float iconSize = 48f;
        var iconBg = CreateImage(card, "IconBg", new Color(0.08f, 0.08f, 0.16f));
        {
            var rt = iconBg.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(9,  -iconSize * 0.5f);
            rt.offsetMax = new Vector2(9 + iconSize, iconSize * 0.5f);
        }

        var icon = CreateImage(card, "Icon", new Color(0.30f, 0.30f, 0.36f));
        {
            var rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(13,  -iconSize * 0.5f + 4f);
            rt.offsetMax = new Vector2(13 + iconSize - 8f, iconSize * 0.5f - 4f);
        }
        icon.preserveAspect = true;

        float tx = 9f + iconSize + 10f;

        // 장비 이름 (상단)
        var nameText = CreateTMP(card, "EquipName", "장비 이름", FntSub, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.48f);
            rt.anchorMax = new Vector2(0.70f, 1f);
            rt.offsetMin = new Vector2(tx, 2);
            rt.offsetMax = new Vector2(0, -4);
        }
        nameText.alignment = TextAlignmentOptions.BottomLeft;

        // 등급 텍스트 (상단 우측)
        var gradeText = CreateTMP(card, "GradeText", "일반", FntSub, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(0.70f, 0.48f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(0, 2);
            rt.offsetMax = new Vector2(-8, -4);
        }
        gradeText.alignment = TextAlignmentOptions.BottomRight;

        // 스탯 텍스트 (하단)
        var statText = CreateTMP(card, "StatText", "", FntSub, FontStyles.Normal);
        {
            var rt = statText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1f, 0.50f);
            rt.offsetMin = new Vector2(tx, 2);
            rt.offsetMax = new Vector2(-8, 0);
        }
        statText.alignment = TextAlignmentOptions.TopLeft;
        statText.color     = LabelColor;

        // 하단 구분선
        var div = CreateImage(card, "Divider", DividerColor);
        {
            var rt = div.rectTransform;
            rt.anchorMin = new Vector2(0.02f, 0);
            rt.anchorMax = new Vector2(0.98f, 0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 1);
        }

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();

        var cardUI = card.AddComponent<EquipCardUI>();
        var cSo    = new SerializedObject(cardUI);
        SetObj(cSo, "_gradeBar",  gradeBar);
        SetObj(cSo, "_icon",      icon);
        SetObj(cSo, "_nameText",  nameText);
        SetObj(cSo, "_gradeText", gradeText);
        SetObj(cSo, "_statText",  statText);
        SetObj(cSo, "_button",    btn);
        cSo.ApplyModifiedProperties();

        return card;
    }

    // ── SkillPanel ────────────────────────────────────────────

    static GameObject BuildSkillPanel(GameObject contentArea, SerializedObject so)
    {
        var panel = CreatePanel(contentArea, "SkillPanel", SectionColor);
        Stretch(panel);

        // 액티브 레이블
        var activeLabel = CreateTMP(panel, "ActiveLabel", "액티브", FntSub, FontStyles.Normal);
        {
            var rt = activeLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(14, -26);
            rt.offsetMax = new Vector2(-14, -4);
        }
        activeLabel.alignment = TextAlignmentOptions.Left;
        activeLabel.color     = LabelColor;

        // 스킬 이름
        var skillText = CreateTMP(panel, "ActiveSkillText", "—", FntMain, FontStyles.Bold);
        {
            var rt = skillText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(14, -54);
            rt.offsetMax = new Vector2(-14, -28);
        }
        skillText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_activeSkillText", skillText);

        // 스킬 설명
        var descText = CreateTMP(panel, "ActiveSkillDescText", "", FntSub, FontStyles.Normal);
        {
            var rt = descText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(14, -114);
            rt.offsetMax = new Vector2(-14, -58);
        }
        descText.alignment = TextAlignmentOptions.TopLeft;
        descText.color     = LabelColor;
        SetObj(so, "_activeSkillDescText", descText);

        // 구분선
        var div = CreateImage(panel, "Divider", DividerColor);
        {
            var rt = div.rectTransform;
            rt.anchorMin = new Vector2(0.03f, 1);
            rt.anchorMax = new Vector2(0.97f, 1);
            rt.offsetMin = new Vector2(0, -120);
            rt.offsetMax = new Vector2(0, -118);
        }

        // 패시브 레이블
        var passiveLabel = CreateTMP(panel, "PassiveLabel", "패시브", FntSub, FontStyles.Normal);
        {
            var rt = passiveLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(14, -142);
            rt.offsetMax = new Vector2(-14, -122);
        }
        passiveLabel.alignment = TextAlignmentOptions.Left;
        passiveLabel.color     = LabelColor;

        SetObj(so, "_passive0Text", BuildPassiveRow(panel, "Passive0Text", -168f, true));
        SetObj(so, "_passive1Text", BuildPassiveRow(panel, "Passive1Text", -196f, false));
        SetObj(so, "_passive2Text", BuildPassiveRow(panel, "Passive2Text", -224f, false));

        return panel;
    }

    static TextMeshProUGUI BuildPassiveRow(GameObject parent, string name,
                                           float topOffset, bool active)
    {
        var tmp = CreateTMP(parent, name, "—", FntMain, FontStyles.Bold);
        {
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(18, topOffset - 22);
            rt.offsetMax = new Vector2(-14, topOffset);
        }
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color     = active ? Color.white : new Color(0.40f, 0.40f, 0.40f);
        return tmp;
    }

    // ── 오른쪽 카드 리스트 (ScrollView) ──────────────────────
    // _hireBtn, _hireCostText 도 so 에 연결.

    static Transform BuildCardListSection(GameObject right, SerializedObject so)
    {
        var header = CreatePanel(right, "Header", BgColor);
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -44);
            rt.offsetMax = new Vector2(0, 0);
        }

        var headerText = CreateTMP(header, "Title", "영웅 목록", FntMain, FontStyles.Bold);
        {
            var rt = headerText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(16, 0);
            rt.offsetMax = new Vector2(200, 0);
        }
        headerText.alignment = TextAlignmentOptions.Left;

        // ── 용병 고용 버튼 (헤더 우측) ──────────────────────
        var hireBtnGo = CreatePanel(header, "HireButton", new Color(0.14f, 0.38f, 0.18f));
        {
            var rt = hireBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-172, 5);
            rt.offsetMax = new Vector2(-8,  -5);
        }
        var hireBtn = hireBtnGo.AddComponent<Button>();
        hireBtn.targetGraphic = hireBtnGo.GetComponent<Image>();
        SetObj(so, "_hireBtn", hireBtn);

        var hireLbl = CreateTMP(hireBtnGo, "Label", "용병 고용", FntMain, FontStyles.Bold);
        {
            var rt = hireLbl.rectTransform;
            rt.anchorMin = new Vector2(0, 0.48f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-8, -2);
        }
        hireLbl.alignment = TextAlignmentOptions.Bottom;

        var hireCostText = CreateTMP(hireBtnGo, "CostText", "500 G", FntSub, FontStyles.Normal);
        {
            var rt = hireCostText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.50f);
            rt.offsetMin = new Vector2(8, 2);
            rt.offsetMax = new Vector2(-8, 0);
        }
        hireCostText.alignment = TextAlignmentOptions.Top;
        hireCostText.color     = new Color(1.0f, 0.85f, 0.20f);
        SetObj(so, "_hireCostText", hireCostText);

        // ── ScrollView ────────────────────────────────────────
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(right.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -44);
        }

        var scroll            = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal     = false;
        scroll.movementType   = ScrollRect.MovementType.Elastic;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport);
        viewport.GetComponent<Image>().color          = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

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
        grid.cellSize        = new Vector2(360, 170);
        grid.spacing         = new Vector2(10, 10);
        grid.padding         = new RectOffset(10, 10, 10, 10);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;

        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content  = content.GetComponent<RectTransform>();
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return content.transform;
    }

    // ============================================================
    //  HeroCard 프리팹 (360×170px)
    //  레이아웃: [초상화 100px] | 이름  등급
    //                            Lv.X  직업
    //                            ──────────
    //                            HP XXXX  공격 XXX
    //                            방어 X%  용병 XX명
    // ============================================================

    static GameObject BuildCardPrefab()
    {
        var card = CreatePanel(null, "HeroCard", CardColor);
        SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(360, 170));

        var border = CreateImage(card, "GradeBorder", CardBorderColor);
        {
            var rt = border.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        border.type = Image.Type.Simple;

        var inner = CreatePanel(card, "InnerBg", CardColor);
        {
            var rt = inner.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3, 3);
            rt.offsetMax = new Vector2(-3, -3);
        }

        // 초상화 (좌측 100px)
        var portraitBg = CreateImage(card, "PortraitBg", new Color(0.16f, 0.27f, 0.56f));
        {
            var rt = portraitBg.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(4,   4);
            rt.offsetMax = new Vector2(104, -4);
        }

        var portraitImg = CreateImage(card, "PortraitImage", Color.clear);
        {
            var rt = portraitImg.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(4,   4);
            rt.offsetMax = new Vector2(104, -4);
        }
        portraitImg.preserveAspect = true;

        // x 앵커 기준점 (초상화 우측 + 패딩)
        const float ix = 112f / 360f;  // ≈ 0.311

        // ── 이름 (상단 좌측) ──────────────────────────────────
        var nameText = CreateTMP(card, "NameText", "이름", FntSub, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(ix,    0.52f);
            rt.anchorMax = new Vector2(0.72f, 0.97f);
            rt.offsetMin = new Vector2(0, 2);
            rt.offsetMax = new Vector2(0, -4);
        }
        nameText.alignment = TextAlignmentOptions.BottomLeft;

        // ── 등급 (상단 우측) ──────────────────────────────────
        var gradeText = CreateTMP(card, "GradeText", "일반", FntSub, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(0.72f, 0.52f);
            rt.anchorMax = new Vector2(1f,    0.97f);
            rt.offsetMin = new Vector2(0, 2);
            rt.offsetMax = new Vector2(-6, -4);
        }
        gradeText.alignment = TextAlignmentOptions.BottomRight;
        gradeText.color     = new Color(0.55f, 0.55f, 0.55f);

        // ── 레벨 (중간 좌측) ──────────────────────────────────
        var levelText = CreateTMP(card, "LevelText", "Lv.1", FntSub, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(ix,    0.40f);
            rt.anchorMax = new Vector2(0.52f, 0.51f);
            rt.offsetMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(0, -1);
        }
        levelText.alignment = TextAlignmentOptions.Left;
        levelText.color     = LabelColor;

        // ── 직업 (중간 우측) ──────────────────────────────────
        var jobText = CreateTMP(card, "JobText", "기사", FntSub, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(0.52f, 0.40f);
            rt.anchorMax = new Vector2(1f,    0.51f);
            rt.offsetMin = new Vector2(0, 1);
            rt.offsetMax = new Vector2(-6, -1);
        }
        jobText.alignment = TextAlignmentOptions.Right;
        jobText.color     = LabelColor;

        // ── 구분선 ────────────────────────────────────────────
        var statDiv = CreateImage(card, "StatDivider", DividerColor);
        {
            var rt = statDiv.rectTransform;
            rt.anchorMin = new Vector2(ix,   0.38f);
            rt.anchorMax = new Vector2(0.97f, 0.38f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 2);
        }

        // ── 스탯 행 1: HP / 공격 ─────────────────────────────
        var hpLbl = CreateTMP(card, "HpLabel", "HP", FntSub, FontStyles.Normal);
        {
            var rt = hpLbl.rectTransform;
            rt.anchorMin = new Vector2(ix,    0.23f);
            rt.anchorMax = new Vector2(0.43f, 0.37f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        hpLbl.alignment = TextAlignmentOptions.Left;
        hpLbl.color     = LabelColor;

        var hpText = CreateTMP(card, "HpText", "—", FntSub, FontStyles.Bold);
        {
            var rt = hpText.rectTransform;
            rt.anchorMin = new Vector2(0.43f, 0.23f);
            rt.anchorMax = new Vector2(0.63f, 0.37f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(-2, 0);
        }
        hpText.alignment = TextAlignmentOptions.Right;

        var atkLbl = CreateTMP(card, "AtkLabel", "공격", FntSub, FontStyles.Normal);
        {
            var rt = atkLbl.rectTransform;
            rt.anchorMin = new Vector2(0.65f, 0.23f);
            rt.anchorMax = new Vector2(0.80f, 0.37f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        atkLbl.alignment = TextAlignmentOptions.Left;
        atkLbl.color     = LabelColor;

        var atkText = CreateTMP(card, "AtkText", "—", FntSub, FontStyles.Bold);
        {
            var rt = atkText.rectTransform;
            rt.anchorMin = new Vector2(0.80f, 0.23f);
            rt.anchorMax = new Vector2(1f,    0.37f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(-6, 0);
        }
        atkText.alignment = TextAlignmentOptions.Right;

        // ── 스탯 행 2: 방어율 / 용병수 ───────────────────────
        var defLbl = CreateTMP(card, "DefLabel", "방어", FntSub, FontStyles.Normal);
        {
            var rt = defLbl.rectTransform;
            rt.anchorMin = new Vector2(ix,    0.05f);
            rt.anchorMax = new Vector2(0.43f, 0.21f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        defLbl.alignment = TextAlignmentOptions.Left;
        defLbl.color     = LabelColor;

        var defText = CreateTMP(card, "DefText", "—", FntSub, FontStyles.Bold);
        {
            var rt = defText.rectTransform;
            rt.anchorMin = new Vector2(0.43f, 0.05f);
            rt.anchorMax = new Vector2(0.63f, 0.21f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(-2, 0);
        }
        defText.alignment = TextAlignmentOptions.Right;

        var soldLbl = CreateTMP(card, "SoldierLabel", "용병", FntSub, FontStyles.Normal);
        {
            var rt = soldLbl.rectTransform;
            rt.anchorMin = new Vector2(0.65f, 0.05f);
            rt.anchorMax = new Vector2(0.80f, 0.21f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        soldLbl.alignment = TextAlignmentOptions.Left;
        soldLbl.color     = LabelColor;

        var soldText = CreateTMP(card, "SoldierText", "—", FntSub, FontStyles.Bold);
        {
            var rt = soldText.rectTransform;
            rt.anchorMin = new Vector2(0.80f, 0.05f);
            rt.anchorMax = new Vector2(1f,    0.21f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(-6, 0);
        }
        soldText.alignment = TextAlignmentOptions.Right;

        // ── 배치 배지 (카드 하단 오버레이) ───────────────────────
        var deployText = CreateTMP(card, "DeployText", "출전 1번", FntMini, FontStyles.Bold);
        {
            var rt = deployText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(4,  0);
            rt.offsetMax = new Vector2(-4, 20);
        }
        deployText.alignment        = TextAlignmentOptions.Center;
        deployText.color            = DeployBadgeColor;
        deployText.gameObject.SetActive(false);

        var btn = card.AddComponent<Button>();
        btn.targetGraphic = card.GetComponent<Image>();

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
        SetObj(cSo, "_jobText",        jobText);
        SetObj(cSo, "_gradeText",      gradeText);
        SetObj(cSo, "_hpText",         hpText);
        SetObj(cSo, "_atkText",        atkText);
        SetObj(cSo, "_defText",        defText);
        SetObj(cSo, "_soldierText",    soldText);
        SetObj(cSo, "_button",         btn);
        SetObj(cSo, "_deployText",     deployText);
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

    static void SetObjArray(SerializedObject so, string field, Object[] objs)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[HeroPanelCreator] 배열 필드 없음: {field}"); return; }
        prop.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
    }
}
#endif
