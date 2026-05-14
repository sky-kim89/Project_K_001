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
//    LeftPanel (450px)
//      PortraitSection (200px)  — 초상화 + 이름·레벨·직업·등급
//      TabBar          (44px)   — [스탯] [장비] [스킬] 버튼
//      TabContentArea  (나머지) — StatPanel / EquipPanel / SkillPanel (SetActive 전환)
//    VertDivider (2px)
//    RightPanel (나머지 너비)  — 헤더 + 카드 ScrollView
// ============================================================

public static class HeroPanelCreator
{
    const string PanelPrefabPath          = "Assets/_project/2.Prefabs/UI/Lobby/HeroPanel.prefab";
    const string CardPrefabPath           = "Assets/_project/2.Prefabs/UI/Lobby/HeroCard.prefab";
    const string EquipCardPrefabPath      = "Assets/_project/2.Prefabs/UI/Lobby/EquipCard.prefab";
    const string EquipComparePrefabPath   = "Assets/_project/2.Prefabs/UI/EquipComparePopup.prefab";
    const string DisassemblePrefabPath    = "Assets/_project/2.Prefabs/UI/DisassemblePopup.prefab";

    const float LeftWidth     = 450f;
    const float PortraitH     = 240f;
    const float DeployRowH    = 52f;   // 배치 슬롯 행 높이
    const float TabBarH       = 44f;
    const float CurrencyBarH  = 60f;   // 재화 바 높이

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
    static readonly Color LevelUpBtnColor   = new Color(0.16f, 0.32f, 0.58f, 1f);
    static readonly Color SoldierUpBtnColor = new Color(0.14f, 0.32f, 0.20f, 1f);
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
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI");
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

        var compareGo = BuildEquipComparePopupPrefab();
        PrefabUtility.SaveAsPrefabAsset(compareGo, EquipComparePrefabPath);
        Object.DestroyImmediate(compareGo);

        var disassembleGo = BuildDisassemblePopupPrefab();
        PrefabUtility.SaveAsPrefabAsset(disassembleGo, DisassemblePrefabPath);
        Object.DestroyImmediate(disassembleGo);

        AssetDatabase.Refresh();
        Debug.Log("[HeroPanelCreator] HeroPanel + HeroCard + EquipCard + EquipComparePopup + DisassemblePopup 프리팹 생성 완료");
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
            "Assets/Resources/ActiveSkillDatabase.asset");
        if (activeDb != null) SetObj(so, "_activeSkillDatabase", activeDb);

        var passiveDb = AssetDatabase.LoadAssetAtPath<PassiveSkillDatabase>(
            "Assets/Resources/PassiveSkillDatabase.asset");
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
        const float BtnH     = 42f;
        const float BtnPad   = 7f;
        const float ExpBarH  = 8f;
        const float ExpTextH = 22f;
        const float ExpGap   = 4f;

        // 하단에서 위로: 패딩 → 레벨업 버튼 → 패딩 → EXP 바 → 갭 → EXP 텍스트 → 패딩 → 스탯 목록
        float expBarBottom = BtnPad + BtnH + BtnPad;               // 56
        float expBarTop    = expBarBottom + ExpBarH;                // 64
        float expTxtBottom = expBarTop + ExpGap;                    // 68
        float expTxtTop    = expTxtBottom + ExpTextH;               // 90
        float listBottom   = expTxtTop + BtnPad;                    // 97

        var panel = CreatePanel(contentArea, "StatPanel", SectionColor);
        Stretch(panel);

        // 스탯 행 컨테이너 — 하단 레벨업·EXP 영역 제외
        var listContainer = new GameObject("StatListContainer", typeof(RectTransform));
        listContainer.transform.SetParent(panel.transform, false);
        {
            var rt = listContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, listBottom);
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

        // ── EXP 표시 (레벨업 버튼 위) ───────────────────────────
        // EXP 텍스트
        var expText = CreateTMP(panel, "ExpText", "0 / 100 EXP", FntSub, FontStyles.Normal);
        {
            var rt = expText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(BtnPad, expTxtBottom);
            rt.offsetMax = new Vector2(-BtnPad, expTxtTop);
        }
        expText.alignment = TextAlignmentOptions.Center;
        expText.color     = LabelColor;
        SetObj(so, "_expText", expText);

        // EXP 바 배경
        var expBarBg = CreatePanel(panel, "ExpBarBg", new Color(0.08f, 0.08f, 0.14f));
        {
            var rt = expBarBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(BtnPad, expBarBottom);
            rt.offsetMax = new Vector2(-BtnPad, expBarTop);
        }

        // EXP 바 채움 — anchorMax.x 를 런타임에 0~1 로 갱신
        var expFill = CreatePanel(expBarBg, "ExpBarFill", new Color(0.25f, 0.65f, 1.00f));
        {
            var rt = expFill.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        SetObj(so, "_expBarFill", expFill.GetComponent<Image>());

        // ── 레벨업 버튼 (왼쪽 절반) ────────────────────────────
        var lvBtnGo = CreatePanel(panel, "LevelUpButton", LevelUpBtnColor);
        {
            var rt = lvBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,    0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(BtnPad, BtnPad);
            rt.offsetMax = new Vector2(-2f,    BtnPad + BtnH);
        }
        var levelUpBtn = lvBtnGo.AddComponent<Button>();
        levelUpBtn.targetGraphic = lvBtnGo.GetComponent<Image>();
        SetObj(so, "_levelUpBtn", levelUpBtn);

        var lvLabel = CreateTMP(lvBtnGo, "Label", "레벨업", FntMain, FontStyles.Bold);
        {
            var rt = lvLabel.rectTransform;
            rt.anchorMin = new Vector2(0,     0);
            rt.anchorMax = new Vector2(0.52f, 1);
            rt.offsetMin = new Vector2(4, 0);
            rt.offsetMax = Vector2.zero;
        }
        lvLabel.alignment = TextAlignmentOptions.Right;

        var lvCostRow = new GameObject("CostRow", typeof(RectTransform));
        lvCostRow.transform.SetParent(lvBtnGo.transform, false);
        {
            var rt = lvCostRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.52f, 0);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 0);
            rt.offsetMax = new Vector2(-4, 0);
        }
        BuildCostHlg(lvCostRow);
        var lvCostIcon = CreateImage(lvCostRow, "CostIcon", new Color(0.8f, 0.8f, 0.8f));
        lvCostIcon.preserveAspect = true;
        AddIconLE(lvCostIcon, 20f);
        SetObj(so, "_levelUpCostIcon", lvCostIcon);
        var costText = CreateTMP(lvCostRow, "CostText", "0", FntSub, FontStyles.Normal);
        costText.alignment = TextAlignmentOptions.Left;
        costText.color     = new Color(1.0f, 0.85f, 0.20f);
        costText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        SetObj(so, "_levelUpCostText", costText);

        // ── 용병 수 증가 버튼 (오른쪽 절반) ─────────────────────
        var soldBtnGo = CreatePanel(panel, "SoldierUpButton", SoldierUpBtnColor);
        {
            var rt = soldBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(1f,   0);
            rt.offsetMin = new Vector2(2f,      BtnPad);
            rt.offsetMax = new Vector2(-BtnPad, BtnPad + BtnH);
        }
        var soldierUpBtn = soldBtnGo.AddComponent<Button>();
        soldierUpBtn.targetGraphic = soldBtnGo.GetComponent<Image>();
        SetObj(so, "_soldierUpBtn", soldierUpBtn);

        var soldLabel = CreateTMP(soldBtnGo, "Label", "용병 +", FntMain, FontStyles.Bold);
        {
            var rt = soldLabel.rectTransform;
            rt.anchorMin = new Vector2(0,     0);
            rt.anchorMax = new Vector2(0.52f, 1);
            rt.offsetMin = new Vector2(4, 0);
            rt.offsetMax = Vector2.zero;
        }
        soldLabel.alignment = TextAlignmentOptions.Right;

        var soldCostRow = new GameObject("CostRow", typeof(RectTransform));
        soldCostRow.transform.SetParent(soldBtnGo.transform, false);
        {
            var rt = soldCostRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.52f, 0);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 0);
            rt.offsetMax = new Vector2(-4, 0);
        }
        BuildCostHlg(soldCostRow);
        var soldCostIcon = CreateImage(soldCostRow, "CostIcon", new Color(0.8f, 0.8f, 0.8f));
        soldCostIcon.preserveAspect = true;
        AddIconLE(soldCostIcon, 20f);
        SetObj(so, "_soldierUpCostIcon", soldCostIcon);
        var soldCostText = CreateTMP(soldCostRow, "CostText", "10", FntSub, FontStyles.Normal);
        soldCostText.alignment = TextAlignmentOptions.Left;
        soldCostText.color     = new Color(0.85f, 0.90f, 1.0f);
        soldCostText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        SetObj(so, "_soldierUpCostText", soldCostText);

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
        hlg.padding                = new RectOffset(8, 4, 0, 0);  // 좌8 우4 — 값 영역 최대화

        // 레이블 (고정 너비 90px — 최대 3글자 한글 수용)
        var lbl = CreateTMP(row, "Label", label, FntSub, FontStyles.Normal);
        lbl.alignment        = TextAlignmentOptions.Left;
        lbl.color            = LabelColor;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Ellipsis;
        var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 90f;
        lblLE.flexibleWidth  = 0f;

        // 값 (나머지 너비 전부 — 450-90-8-4 = 348px)
        var val = CreateTMP(row, "Value", "—", FntMain, FontStyles.Bold);
        val.alignment          = TextAlignmentOptions.Right;
        val.textWrappingMode   = TextWrappingModes.NoWrap;
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

        var (btn0, name0, bar0, icon0, stat0, lock0, enh0, enhCost0, enhIcon0) = BuildEquipSlot(panel, "EquipSlot0", 10f, 150f);
        SetObj(so, "_equip0Btn",           btn0);
        SetObj(so, "_equip0NameText",      name0);
        SetObj(so, "_equip0GradeBar",      bar0);
        SetObj(so, "_equip0Icon",          icon0);
        SetObj(so, "_equip0StatText",      stat0);
        SetObj(so, "_equip0LockBadge",     lock0);
        SetObj(so, "_equip0EnhanceBtn",    enh0);
        SetObj(so, "_equip0EnhanceCostText", enhCost0);
        SetObj(so, "_equip0EnhanceCostIcon", enhIcon0);

        var (btn1, name1, bar1, icon1, stat1, lock1, enh1, enhCost1, enhIcon1) = BuildEquipSlot(panel, "EquipSlot1", 172f, 150f);
        SetObj(so, "_equip1Btn",           btn1);
        SetObj(so, "_equip1NameText",      name1);
        SetObj(so, "_equip1GradeBar",      bar1);
        SetObj(so, "_equip1Icon",          icon1);
        SetObj(so, "_equip1StatText",      stat1);
        SetObj(so, "_equip1LockBadge",     lock1);
        SetObj(so, "_equip1EnhanceBtn",    enh1);
        SetObj(so, "_equip1EnhanceCostText", enhCost1);
        SetObj(so, "_equip1EnhanceCostIcon", enhIcon1);

        return panel;
    }

    // 장비 슬롯: [등급바] [아이콘] [이름 / 옵션] [귀속 배지] [강화 버튼]
    static (Button btn, TextMeshProUGUI nameText, Image gradeBar, Image iconImage,
            TextMeshProUGUI statText, GameObject lockBadge,
            Button enhanceBtn, TextMeshProUGUI enhanceCostText, Image enhanceCostIcon)
        BuildEquipSlot(GameObject parent, string name, float offsetFromTop, float slotH)
    {
        var slotBg = CreatePanel(parent, name, SlotColor);
        {
            var rt = slotBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(4, -(offsetFromTop + slotH));
            rt.offsetMax = new Vector2(-4, -offsetFromTop);
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

        // 강화 버튼 크기 (nameText offsetMax에서 먼저 참조)
        const float EnhBtnW = 148f;
        const float EnhBtnH = 40f;

        // 아이콘 박스 (좌측 정사각형, 최대 86px)
        float iconSize = Mathf.Min(slotH - 14f, 86f);
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
            rt.offsetMin = new Vector2(14,               -iconSize * 0.5f + 4f);
            rt.offsetMax = new Vector2(14 + iconSize - 8f, iconSize * 0.5f - 4f);
        }
        iconImg.preserveAspect = true;

        float textLeft = 10f + iconSize + 12f;

        // 장비 이름 (상단 35%) — 우측은 강화 버튼 영역 피함
        var nameText = CreateTMP(slotBg, "EquipNameText", "없음", FntMain, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.60f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(textLeft, 2);
            rt.offsetMax = new Vector2(-8f, -4);
        }
        nameText.alignment = TextAlignmentOptions.Left;

        // 옵션 텍스트 (하단 65%)
        var statText = CreateTMP(slotBg, "StatText", "", FntSub, FontStyles.Normal);
        {
            var rt = statText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.65f);
            rt.offsetMin = new Vector2(textLeft, 2);
            rt.offsetMax = new Vector2(-10, 0);
        }
        statText.alignment        = TextAlignmentOptions.TopLeft;
        statText.color            = LabelColor;
        statText.textWrappingMode = TextWrappingModes.Normal;
        statText.overflowMode     = TextOverflowModes.Overflow;

        // 귀속 배지 (장비 장착 시 HeroPanelUI 가 활성화)
        var lockBadge = new GameObject("LockBadge", typeof(RectTransform), typeof(Image));
        lockBadge.transform.SetParent(slotBg.transform, false);
        lockBadge.GetComponent<Image>().color = new Color(0.70f, 0.18f, 0.10f, 0.90f);
        {
            var rt = lockBadge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(1, 0);
            rt.offsetMin = new Vector2(-54, 3);
            rt.offsetMax = new Vector2(-4,  19);
        }
        var lockTmp = CreateTMP(lockBadge, "Label", "귀속", FntMini, FontStyles.Bold);
        {
            var rt = lockTmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(3, 0);
            rt.offsetMax = Vector2.zero;
        }
        lockTmp.alignment = TextAlignmentOptions.Center;
        lockBadge.SetActive(false);

        // 강화 버튼 (우측 상단, BG 상단 기준)
        var enhGo = new GameObject("EnhanceBtn", typeof(RectTransform), typeof(Image));
        enhGo.transform.SetParent(slotBg.transform, false);
        enhGo.GetComponent<Image>().color = new Color(0.20f, 0.28f, 0.50f, 1f);
        {
            var rt = enhGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-EnhBtnW - 6f, -62f - EnhBtnH);
            rt.offsetMax = new Vector2(-6f, -62f);
        }
        var enhBtn = enhGo.AddComponent<Button>();
        enhBtn.targetGraphic = enhGo.GetComponent<Image>();

        BuildCostHlg(enhGo);

        var enhLbl = CreateTMP(enhGo, "Label", "강화", FntSub, FontStyles.Bold);
        {
            var le = enhLbl.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 48f; le.minWidth = 48f;
        }
        enhLbl.alignment = TextAlignmentOptions.Right;
        enhLbl.color = new Color(0.80f, 0.85f, 1.0f);

        var enhIconGo = new GameObject("CostIcon", typeof(RectTransform), typeof(Image));
        enhIconGo.transform.SetParent(enhGo.transform, false);
        var enhCostIcon = enhIconGo.GetComponent<Image>();
        enhCostIcon.color = Color.white;
        AddIconLE(enhCostIcon, 22f);

        var enhCostText = CreateTMP(enhGo, "CostText", "2", FntSub, FontStyles.Bold);
        {
            var le = enhCostText.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 36f; le.minWidth = 24f;
        }
        enhCostText.alignment = TextAlignmentOptions.Left;
        enhCostText.color = new Color(0.80f, 0.90f, 1.0f);

        return (btn, nameText, gradeBar, iconImg, statText, lockBadge, enhBtn, enhCostText, enhCostIcon);
    }

    // ── EquipComparePopup 독립 프리팹 ────────────────────────
    //   구조 (600×700px 중앙 팝업):
    //     Header (48px)    — 제목
    //     CompareRow (200px) — [현재 카드] [VS] [선택 카드]
    //     ActionRow (60px) — [장착 버튼] [경고 텍스트]
    //     ListScroll (나머지) — 인벤토리 장비 목록

    static GameObject BuildEquipComparePopupPrefab()
    {
        const float W        = 720f;
        const float H        = 860f;
        const float HeaderH  = 60f;
        const float LabelH   = 30f;
        const float CompareH = 290f;
        const float ActionH  = 72f;

        // 루트 — CanvasGroup 은 PopupBase [RequireComponent] 로 자동 추가
        var root = CreatePanel(null, "EquipComparePopup", new Color(0.04f, 0.04f, 0.09f, 0.97f));
        root.AddComponent<CanvasGroup>();
        {
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(W, H);
        }

        var popupComp = root.AddComponent<EquipComparePopup>();
        var pso       = new SerializedObject(popupComp);

        // PopupType 설정
        var typeProp = pso.FindProperty("_popupType");
        if (typeProp != null) typeProp.intValue = (int)PopupType.EquipCompare;

        // ── 헤더 ────────────────────────────────────────────────
        var header = CreatePanel(root, "Header", new Color(0.08f, 0.10f, 0.20f));
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -HeaderH);
            rt.offsetMax = Vector2.zero;
        }

        var title = CreateTMP(header, "TitleText", "슬롯 1 장비 교체", FntHero, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14, 0);
            rt.offsetMax = Vector2.zero;
        }
        title.alignment = TextAlignmentOptions.Left;
        SetObj(pso, "_titleText", title);

        // ── 카드 레이블 행 ──────────────────────────────────────
        var cardLabelRow = new GameObject("CardLabelRow", typeof(RectTransform));
        cardLabelRow.transform.SetParent(root.transform, false);
        {
            var rt = cardLabelRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(HeaderH + LabelH));
            rt.offsetMax = new Vector2(0, -HeaderH);
        }

        var curLabel = CreateTMP(cardLabelRow, "CurLabel", "장착 중", FntSub, FontStyles.Normal);
        {
            var rt = curLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.48f, 1);
            rt.offsetMin = new Vector2(12, 0);
            rt.offsetMax = Vector2.zero;
        }
        curLabel.alignment = TextAlignmentOptions.BottomLeft;
        curLabel.color     = new Color(0.55f, 0.55f, 0.65f, 0.85f);

        var selLabel = CreateTMP(cardLabelRow, "SelLabel", "선택됨", FntSub, FontStyles.Normal);
        {
            var rt = selLabel.rectTransform;
            rt.anchorMin = new Vector2(0.52f, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, 0);
            rt.offsetMax = new Vector2(-8, 0);
        }
        selLabel.alignment = TextAlignmentOptions.BottomLeft;
        selLabel.color     = new Color(0.40f, 0.75f, 0.40f, 0.85f);

        // ── 비교 카드 행 ────────────────────────────────────────
        var compareRow = new GameObject("CompareRow", typeof(RectTransform));
        compareRow.transform.SetParent(root.transform, false);
        {
            var rt = compareRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(HeaderH + LabelH + CompareH));
            rt.offsetMax = new Vector2(0, -(HeaderH + LabelH));
        }

        var (curBar, curIcon, curName, curStat, curBind) =
            BuildCompareCard(compareRow, "CurCard", new Vector2(0f, 0f), new Vector2(0.48f, 1f));
        SetObj(pso, "_curGradeBar",  curBar);
        SetObj(pso, "_curIcon",      curIcon);
        SetObj(pso, "_curName",      curName);
        SetObj(pso, "_curStat",      curStat);
        SetObj(pso, "_curBindBadge", curBind);

        var vsLabel = CreateTMP(compareRow, "VS", "VS", FntSub, FontStyles.Bold);
        {
            var rt = vsLabel.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(40f, 40f);
            rt.anchoredPosition = Vector2.zero;
        }
        vsLabel.alignment = TextAlignmentOptions.Center;
        vsLabel.color     = new Color(0.55f, 0.55f, 0.65f, 0.70f);

        var (selBar, selIcon, selName, selStat, _) =
            BuildCompareCard(compareRow, "SelCard", new Vector2(0.52f, 0f), new Vector2(1f, 1f));
        SetObj(pso, "_selGradeBar", selBar);
        SetObj(pso, "_selIcon",     selIcon);
        SetObj(pso, "_selName",     selName);
        SetObj(pso, "_selStat",     selStat);

        // ── 액션 행 ─────────────────────────────────────────────
        var actionRow = new GameObject("ActionRow", typeof(RectTransform));
        actionRow.transform.SetParent(root.transform, false);
        {
            var rt = actionRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(HeaderH + LabelH + CompareH + ActionH));
            rt.offsetMax = new Vector2(0, -(HeaderH + LabelH + CompareH));
        }

        var equipBtnGo = CreatePanel(actionRow, "EquipButton", new Color(0.16f, 0.40f, 0.20f));
        {
            var rt = equipBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.15f);
            rt.anchorMax = new Vector2(0.42f, 0.85f);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-4, 0);
        }
        var equipBtn = equipBtnGo.AddComponent<Button>();
        equipBtn.targetGraphic = equipBtnGo.GetComponent<Image>();
        var equipLbl = CreateTMP(equipBtnGo, "Label", "장착", FntMain, FontStyles.Bold);
        equipLbl.rectTransform.anchorMin = Vector2.zero;
        equipLbl.rectTransform.anchorMax = Vector2.one;
        equipLbl.rectTransform.offsetMin = Vector2.zero;
        equipLbl.rectTransform.offsetMax = Vector2.zero;
        equipLbl.alignment = TextAlignmentOptions.Center;
        SetObj(pso, "_equipBtn", equipBtn);

        var warningText = CreateTMP(actionRow, "WarningText",
            "교체 시 기존 장비가 소멸됩니다", FntSub, FontStyles.Bold);
        {
            var rt = warningText.rectTransform;
            rt.anchorMin = new Vector2(0.44f, 0);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(4, 4);
            rt.offsetMax = new Vector2(-8, -4);
        }
        warningText.alignment        = TextAlignmentOptions.Left;
        warningText.color            = new Color(1.0f, 0.55f, 0.15f);
        warningText.textWrappingMode = TextWrappingModes.Normal;
        warningText.gameObject.SetActive(false);
        SetObj(pso, "_warningText", warningText);

        // ── 인벤토리 스크롤 ────────────────────────────────────
        float listTop = HeaderH + LabelH + CompareH + ActionH;
        var scrollGo = new GameObject("ListScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(root.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -listTop);
        }

        var scroll        = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;

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
        grid.cellSize        = new Vector2(80f, 80f);
        grid.spacing         = new Vector2(8f, 8f);
        grid.padding         = new RectOffset(8, 8, 8, 8);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content  = content.GetComponent<RectTransform>();

        SetObj(pso, "_listContent", content.transform);

        pso.ApplyModifiedProperties();
        return root;
    }

    // 비교 카드 공통 빌더
    static (Image gradeBar, Image icon, TextMeshProUGUI name, TextMeshProUGUI stat, GameObject bindBadge)
        BuildCompareCard(GameObject parent, string cardName, Vector2 anchorMin, Vector2 anchorMax)
    {
        var card = CreatePanel(parent, cardName, new Color(0.10f, 0.10f, 0.18f));
        {
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4, 6);
            rt.offsetMax = new Vector2(-4, -6);
        }

        var gradeBar = CreateImage(card, "GradeBar", new Color(0.30f, 0.30f, 0.35f));
        {
            var rt = gradeBar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 4);
            rt.offsetMax = new Vector2(5, -4);
        }

        const float iconSz = 80f;
        var iconBg = CreateImage(card, "IconBg", new Color(0.08f, 0.08f, 0.14f));
        {
            var rt = iconBg.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(12, -iconSz * 0.5f);
            rt.offsetMax = new Vector2(12 + iconSz, iconSz * 0.5f);
        }
        var icon = CreateImage(card, "Icon", new Color(0.25f, 0.25f, 0.30f));
        {
            var rt = icon.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(16, -iconSz * 0.5f + 6f);
            rt.offsetMax = new Vector2(16 + iconSz - 12f, iconSz * 0.5f - 6f);
        }
        icon.preserveAspect = true;

        float tx = 12f + iconSz + 12f;

        var nameText = CreateTMP(card, "NameText", "없음", FntMain, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.65f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(tx, 2);
            rt.offsetMax = new Vector2(-6, -6);
        }
        nameText.alignment        = TextAlignmentOptions.Left;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode     = TextOverflowModes.Ellipsis;

        var statText = CreateTMP(card, "StatText", "", FntSub, FontStyles.Normal);
        {
            var rt = statText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.65f);
            rt.offsetMin = new Vector2(tx, 2);
            rt.offsetMax = new Vector2(-6, 0);
        }
        statText.alignment        = TextAlignmentOptions.TopLeft;
        statText.color            = new Color(0.60f, 0.60f, 0.70f);
        statText.textWrappingMode = TextWrappingModes.Normal;
        statText.overflowMode     = TextOverflowModes.Overflow;

        var bindBadge = new GameObject("BindBadge", typeof(RectTransform), typeof(Image));
        bindBadge.transform.SetParent(card.transform, false);
        bindBadge.GetComponent<Image>().color = new Color(0.70f, 0.18f, 0.10f, 0.90f);
        {
            var rt = bindBadge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-54, -18);
            rt.offsetMax = new Vector2(-4,  -4);
        }
        var bindLbl = CreateTMP(bindBadge, "Label", "귀속", FntMini, FontStyles.Bold);
        {
            var rt = bindLbl.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 0);
            rt.offsetMax = Vector2.zero;
        }
        bindLbl.alignment = TextAlignmentOptions.Center;
        bindBadge.SetActive(false);

        return (gradeBar, icon, nameText, statText, bindBadge);
    }

    // ── DisassemblePopup 독립 프리팹 ─────────────────────────
    //   구조 (580×700px 중앙 팝업):
    //     Header  (48px)  — 제목 + X 버튼
    //     TabBar  (44px)  — [장수 분해] [장비 분해]
    //     Content (나머지) — HeroTabPanel / EquipTabPanel (ScrollView)

    static GameObject BuildDisassemblePopupPrefab()
    {
        const float W       = 800f;
        const float H       = 960f;
        const float HeaderH = 60f;
        const float TabH    = 54f;

        var root = CreatePanel(null, "DisassemblePopup", new Color(0.04f, 0.04f, 0.09f, 0.97f));
        root.AddComponent<CanvasGroup>();
        {
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(W, H);
        }

        var popupComp = root.AddComponent<DisassemblePopup>();
        var pso       = new SerializedObject(popupComp);

        var typeProp = pso.FindProperty("_popupType");
        if (typeProp != null) typeProp.intValue = (int)PopupType.Disassemble;

        // ── 헤더 ──────────────────────────────────────────────
        var header = CreatePanel(root, "Header", new Color(0.08f, 0.10f, 0.20f));
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -HeaderH); rt.offsetMax = Vector2.zero;
        }

        var title = CreateTMP(header, "TitleText", "분해", FntHero, FontStyles.Bold);
        {
            var rt = title.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16, 0); rt.offsetMax = new Vector2(-60, 0);
        }
        title.alignment = TextAlignmentOptions.Left;

        // X 닫기 버튼
        var closeBtnGo = CreatePanel(header, "CloseButton", new Color(0.22f, 0.22f, 0.30f));
        {
            var rt = closeBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-52, -18); rt.offsetMax = new Vector2(-8, 18);
        }
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
        SetObj(pso, "_closeBtn", closeBtn);
        var closeLbl = CreateTMP(closeBtnGo, "Label", "✕", FntSub, FontStyles.Bold);
        {
            var rt = closeLbl.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        }
        closeLbl.alignment = TextAlignmentOptions.Center;

        // ── 탭 바 ─────────────────────────────────────────────
        var tabBar = CreatePanel(root, "TabBar", new Color(0.07f, 0.09f, 0.18f));
        {
            var rt = tabBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(HeaderH + TabH)); rt.offsetMax = new Vector2(0, -HeaderH);
        }
        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.childAlignment         = TextAnchor.MiddleLeft;
        tabHlg.childControlWidth      = true;
        tabHlg.childControlHeight     = true;
        tabHlg.childForceExpandWidth  = true;
        tabHlg.childForceExpandHeight = true;
        tabHlg.spacing                = 2f;
        tabHlg.padding                = new RectOffset(4, 4, 4, 4);

        var tabButtons = new Button[2];
        string[] tabLabels = { "장수 분해", "장비 분해" };
        for (int i = 0; i < 2; i++)
        {
            var tGo  = CreatePanel(tabBar, $"Tab{i}", new Color(0.12f, 0.14f, 0.24f));
            var tBtn = tGo.AddComponent<Button>();
            tBtn.targetGraphic = tGo.GetComponent<Image>();
            var tLbl = CreateTMP(tGo, "Label", tabLabels[i], FntSub, FontStyles.Bold);
            {
                var rt = tLbl.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            }
            tLbl.alignment = TextAlignmentOptions.Center;
            tLbl.color     = i == 0 ? new Color(0.40f, 0.72f, 1.00f) : new Color(0.55f, 0.55f, 0.60f);
            tabButtons[i]  = tBtn;
        }

        var tabBtnsProp = pso.FindProperty("_tabBtns");
        if (tabBtnsProp != null)
        {
            tabBtnsProp.arraySize = 2;
            tabBtnsProp.GetArrayElementAtIndex(0).objectReferenceValue = tabButtons[0];
            tabBtnsProp.GetArrayElementAtIndex(1).objectReferenceValue = tabButtons[1];
        }

        // ── 콘텐츠 영역 ───────────────────────────────────────
        var contentArea = new GameObject("ContentArea", typeof(RectTransform));
        contentArea.transform.SetParent(root.transform, false);
        {
            var rt = contentArea.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0); rt.offsetMax = new Vector2(0, -(HeaderH + TabH));
        }

        var tabPanels = new GameObject[2];
        string[] panelNames = { "HeroTabPanel", "EquipTabPanel" };
        string[] contentFieldNames = { "_heroContent", "_equipContent" };

        for (int i = 0; i < 2; i++)
        {
            var panel = new GameObject(panelNames[i], typeof(RectTransform));
            panel.transform.SetParent(contentArea.transform, false);
            {
                var rt = panel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }

            // ScrollView
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            {
                var rt = scrollGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal   = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            {
                var rt = viewport.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            scroll.viewport = viewport.GetComponent<RectTransform>();

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            {
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing                = 2f;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content.GetComponent<RectTransform>();

            SetObj(pso, contentFieldNames[i], content.transform);

            panel.SetActive(i == 0);
            tabPanels[i] = panel;
        }

        var tabPanelsProp = pso.FindProperty("_tabPanels");
        if (tabPanelsProp != null)
        {
            tabPanelsProp.arraySize = 2;
            tabPanelsProp.GetArrayElementAtIndex(0).objectReferenceValue = tabPanels[0];
            tabPanelsProp.GetArrayElementAtIndex(1).objectReferenceValue = tabPanels[1];
        }

        // ── 행 템플릿 (비활성 자식 오브젝트) ─────────────────
        var heroTemplate  = BuildHeroRowTemplate(root);
        var equipTemplate = BuildEquipRowTemplate(root);
        SetObj(pso, "_heroRowTemplate",  heroTemplate);
        SetObj(pso, "_equipRowTemplate", equipTemplate);

        pso.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // HeroRowTemplate: GradeBar / PortraitBox(PortraitBg+PortraitImage+Bridge) / InfoBlock(NameRow+StatRow) / RewardBlock(Icon+TMP) / DisBtn
    static GameObject BuildHeroRowTemplate(GameObject parent)
    {
        const float RowH     = 130f;
        const float IconSize = 52f;
        var go = new GameObject("HeroRowTemplate", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = RowH; le.minHeight = RowH;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing                = 0f;
        hlg.padding                = new RectOffset(0, 8, 0, 0);

        // 등급 바
        var bar = new GameObject("GradeBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(go.transform, false);
        bar.AddComponent<LayoutElement>().minWidth = 5f;

        // 초상화 박스 (100px, Mask)
        var portraitBox = new GameObject("PortraitBox", typeof(RectTransform), typeof(Image), typeof(Mask));
        portraitBox.transform.SetParent(go.transform, false);
        var pbImg = portraitBox.GetComponent<Image>();
        pbImg.color = new Color(0.08f, 0.08f, 0.14f);
        portraitBox.GetComponent<Mask>().showMaskGraphic = false;
        var pbLe = portraitBox.AddComponent<LayoutElement>();
        pbLe.minWidth = 100f; pbLe.preferredWidth = 100f;

        var portraitImgGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
        portraitImgGo.transform.SetParent(portraitBox.transform, false);
        var piImg = portraitImgGo.GetComponent<Image>();
        piImg.preserveAspect = true;
        var piRt = portraitImgGo.GetComponent<RectTransform>();
        piRt.anchorMin = Vector2.zero; piRt.anchorMax = Vector2.one;
        piRt.offsetMin = Vector2.zero; piRt.offsetMax = Vector2.zero;

        var bridgeGo = new GameObject("PortraitBridge", typeof(RectTransform));
        bridgeGo.transform.SetParent(portraitBox.transform, false);
        var bridge = bridgeGo.AddComponent<UnitAppearanceBridge>();
        bridgeGo.SetActive(false);

        // 정보 블록 (flex=1, VLG)
        var infoBlock = new GameObject("InfoBlock", typeof(RectTransform));
        infoBlock.transform.SetParent(go.transform, false);
        infoBlock.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var infoVlg = infoBlock.AddComponent<VerticalLayoutGroup>();
        infoVlg.childAlignment         = TextAnchor.MiddleLeft;
        infoVlg.childControlWidth      = true;
        infoVlg.childForceExpandWidth  = true;
        infoVlg.childControlHeight     = true;
        infoVlg.childForceExpandHeight = false;
        infoVlg.padding                = new RectOffset(12, 4, 8, 8);
        infoVlg.spacing                = 4f;

        // NameRow (HLG: NameTMP + GradeTMP)
        var nameRow = new GameObject("NameRow", typeof(RectTransform));
        nameRow.transform.SetParent(infoBlock.transform, false);
        var nameRowLe = nameRow.AddComponent<LayoutElement>();
        nameRowLe.preferredHeight = FntMain * 1.4f;
        var nameHlg = nameRow.AddComponent<HorizontalLayoutGroup>();
        nameHlg.childAlignment        = TextAnchor.MiddleLeft;
        nameHlg.childControlWidth     = true;  nameHlg.childControlHeight     = true;
        nameHlg.childForceExpandWidth = false; nameHlg.childForceExpandHeight = true;
        nameHlg.spacing = 6f;

        var nameTmpGo = new GameObject("NameTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        nameTmpGo.transform.SetParent(nameRow.transform, false);
        var nameTmp = nameTmpGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = "—"; nameTmp.fontSize = FntMain; nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white; nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmpGo.GetComponent<LayoutElement>().flexibleWidth = 1f;

        var gradeTmpGo = new GameObject("GradeTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        gradeTmpGo.transform.SetParent(nameRow.transform, false);
        var gradeTmp = gradeTmpGo.GetComponent<TextMeshProUGUI>();
        gradeTmp.text = "일반"; gradeTmp.fontSize = FntSub; gradeTmp.fontStyle = FontStyles.Bold;
        gradeTmp.color = Color.white; gradeTmp.alignment = TextAlignmentOptions.Center;
        var gradeLe = gradeTmpGo.GetComponent<LayoutElement>();
        gradeLe.preferredWidth = 66f; gradeLe.minWidth = 66f;

        // StatRow (HLG: HpTMP + AtkTMP)
        var statRow = new GameObject("StatRow", typeof(RectTransform));
        statRow.transform.SetParent(infoBlock.transform, false);
        statRow.AddComponent<LayoutElement>().preferredHeight = 54f; // 24(직업) + 2 + 28(공격)
        var statHlg = statRow.AddComponent<HorizontalLayoutGroup>();
        statHlg.childAlignment        = TextAnchor.MiddleLeft;
        statHlg.childControlWidth     = true;  statHlg.childControlHeight     = true;
        statHlg.childForceExpandWidth = true;  statHlg.childForceExpandHeight = true;
        statHlg.spacing = 8f;

        var hpGo = new GameObject("HpTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        hpGo.transform.SetParent(statRow.transform, false);
        var hpTmp = hpGo.GetComponent<TextMeshProUGUI>();
        hpTmp.text = "체력 —"; hpTmp.fontSize = FntSub; hpTmp.color = new Color(0.55f, 0.90f, 0.55f);
        hpTmp.alignment = TextAlignmentOptions.MidlineLeft;
        hpGo.GetComponent<LayoutElement>().flexibleWidth = 1f;

        // 직업(위) + 공격(아래) 블록
        var atkBlock = new GameObject("AtkBlock", typeof(RectTransform));
        atkBlock.transform.SetParent(statRow.transform, false);
        atkBlock.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var atkVlg = atkBlock.AddComponent<VerticalLayoutGroup>();
        atkVlg.childAlignment         = TextAnchor.UpperLeft;
        atkVlg.childControlWidth      = true;  atkVlg.childForceExpandWidth  = true;
        atkVlg.childControlHeight     = true;  atkVlg.childForceExpandHeight = false;
        atkVlg.spacing                = 2f;

        var jobGo = new GameObject("JobTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        jobGo.transform.SetParent(atkBlock.transform, false);
        var jobTmp = jobGo.GetComponent<TextMeshProUGUI>();
        jobTmp.text = "기사"; jobTmp.fontSize = FntMini; jobTmp.color = new Color(0.60f, 0.78f, 0.95f);
        jobGo.GetComponent<LayoutElement>().preferredHeight = 24f;

        var atkGo = new GameObject("AtkTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        atkGo.transform.SetParent(atkBlock.transform, false);
        var atkTmp = atkGo.GetComponent<TextMeshProUGUI>();
        atkTmp.text = "공격 —"; atkTmp.fontSize = FntSub; atkTmp.color = new Color(1.00f, 0.75f, 0.40f);
        atkGo.GetComponent<LayoutElement>().preferredHeight = 28f;

        // 보상 블록 (VLG, 130px: RewardIcon + RewardTMP 세로 배치)
        var rewBlock = new GameObject("RewardBlock", typeof(RectTransform));
        rewBlock.transform.SetParent(go.transform, false);
        var rewLe = rewBlock.AddComponent<LayoutElement>();
        rewLe.preferredWidth = 130f; rewLe.minWidth = 130f;
        var rewVlg = rewBlock.AddComponent<VerticalLayoutGroup>();
        rewVlg.childAlignment         = TextAnchor.MiddleCenter;
        rewVlg.childControlWidth      = false; rewVlg.childControlHeight     = false;
        rewVlg.childForceExpandWidth  = false; rewVlg.childForceExpandHeight = false;
        rewVlg.spacing = 4f;
        rewVlg.padding = new RectOffset(0, 0, 8, 8);

        var rewIconGo = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
        rewIconGo.transform.SetParent(rewBlock.transform, false);
        rewIconGo.GetComponent<Image>().preserveAspect = true;
        var rewIconRt = rewIconGo.GetComponent<RectTransform>();
        rewIconRt.sizeDelta = new Vector2(IconSize, IconSize);

        var rewTmpGo = new GameObject("RewardTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewTmpGo.transform.SetParent(rewBlock.transform, false);
        var rewTmp = rewTmpGo.GetComponent<TextMeshProUGUI>();
        rewTmp.text = "5조각"; rewTmp.fontSize = FntSub; rewTmp.color = new Color(0.85f, 0.90f, 1.0f);
        rewTmp.alignment = TextAlignmentOptions.Center;
        rewTmpGo.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, FntSub * 1.4f);

        // 분해 버튼
        BuildDisassembleBtn(go, 84f);

        // DisHeroRowUI 직렬화 필드 연결
        var rowSo = new SerializedObject(go.AddComponent<DisHeroRowUI>());
        rowSo.FindProperty("_portraitBg").objectReferenceValue     = pbImg;
        rowSo.FindProperty("_portraitImage").objectReferenceValue  = piImg;
        rowSo.FindProperty("_portraitBridge").objectReferenceValue = bridge;
        rowSo.FindProperty("_nameTmp").objectReferenceValue        = nameTmp;
        rowSo.FindProperty("_gradeTmp").objectReferenceValue       = gradeTmp;
        rowSo.FindProperty("_hpTmp").objectReferenceValue          = hpTmp;
        rowSo.FindProperty("_jobTmp").objectReferenceValue         = jobTmp;
        rowSo.FindProperty("_atkTmp").objectReferenceValue         = atkTmp;
        rowSo.FindProperty("_rewardIcon").objectReferenceValue     = rewIconGo.GetComponent<Image>();
        rowSo.FindProperty("_rewardTmp").objectReferenceValue      = rewTmp;
        rowSo.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);
        return go;
    }

    // EquipRowTemplate: GradeBar / IconBg(Icon) / NameBlock(NameTMP+OwnerTMP) / RewardTMP / DisBtn
    static GameObject BuildEquipRowTemplate(GameObject parent)
    {
        const float RowH     = 120f;
        const float IconSize = 52f;
        var go = new GameObject("EquipRowTemplate", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.18f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = RowH; le.minHeight = RowH;

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing                = 0f;
        hlg.padding                = new RectOffset(0, 8, 0, 0);

        // 등급 바
        var bar2 = new GameObject("GradeBar", typeof(RectTransform), typeof(Image));
        bar2.transform.SetParent(go.transform, false);
        bar2.AddComponent<LayoutElement>().minWidth = 5f;

        // 아이콘 박스 (100px)
        var iconBg = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        iconBg.transform.SetParent(go.transform, false);
        iconBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f);
        var iconBgLe = iconBg.AddComponent<LayoutElement>();
        iconBgLe.minWidth = 100f; iconBgLe.preferredWidth = 100f;

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(iconBg.transform, false);
        iconGo.GetComponent<Image>().preserveAspect = true;
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.1f, 0.1f);
        iconRt.anchorMax = new Vector2(0.9f, 0.9f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        // 정보 블록 (flex=1, VLG)
        var infoBlock2 = new GameObject("InfoBlock", typeof(RectTransform));
        infoBlock2.transform.SetParent(go.transform, false);
        infoBlock2.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var infoVlg2 = infoBlock2.AddComponent<VerticalLayoutGroup>();
        infoVlg2.childAlignment         = TextAnchor.MiddleLeft;
        infoVlg2.childControlWidth      = true;
        infoVlg2.childForceExpandWidth  = true;
        infoVlg2.childControlHeight     = true;
        infoVlg2.childForceExpandHeight = false;
        infoVlg2.padding                = new RectOffset(12, 4, 8, 8);
        infoVlg2.spacing                = 4f;

        // NameRow (HLG: NameTMP + GradeTMP)
        var nameRow2 = new GameObject("NameRow", typeof(RectTransform));
        nameRow2.transform.SetParent(infoBlock2.transform, false);
        var nameRow2Le = nameRow2.AddComponent<LayoutElement>();
        nameRow2Le.preferredHeight = FntMain * 1.4f;
        var nameHlg2 = nameRow2.AddComponent<HorizontalLayoutGroup>();
        nameHlg2.childAlignment        = TextAnchor.MiddleLeft;
        nameHlg2.childControlWidth     = true;  nameHlg2.childControlHeight     = true;
        nameHlg2.childForceExpandWidth = false; nameHlg2.childForceExpandHeight = true;
        nameHlg2.spacing = 6f;

        var nameTmpGo2 = new GameObject("NameTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        nameTmpGo2.transform.SetParent(nameRow2.transform, false);
        var nameTmp2 = nameTmpGo2.GetComponent<TextMeshProUGUI>();
        nameTmp2.text = "—"; nameTmp2.fontSize = FntMain; nameTmp2.fontStyle = FontStyles.Bold;
        nameTmp2.color = Color.white; nameTmp2.overflowMode = TextOverflowModes.Ellipsis;
        nameTmpGo2.GetComponent<LayoutElement>().flexibleWidth = 1f;

        var gradeTmpGo2 = new GameObject("GradeTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        gradeTmpGo2.transform.SetParent(nameRow2.transform, false);
        var gradeTmp2 = gradeTmpGo2.GetComponent<TextMeshProUGUI>();
        gradeTmp2.text = "일반"; gradeTmp2.fontSize = FntSub; gradeTmp2.fontStyle = FontStyles.Bold;
        gradeTmp2.color = Color.white; gradeTmp2.alignment = TextAlignmentOptions.Center;
        var gradeLe2 = gradeTmpGo2.GetComponent<LayoutElement>();
        gradeLe2.preferredWidth = 66f; gradeLe2.minWidth = 66f;

        // LevelTMP (아이템 레벨)
        var lvlGo = new GameObject("LevelTMP", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        lvlGo.transform.SetParent(infoBlock2.transform, false);
        var lvlTmp = lvlGo.GetComponent<TextMeshProUGUI>();
        lvlTmp.text = "아이템 Lv.1"; lvlTmp.fontSize = FntSub; lvlTmp.color = new Color(0.60f, 0.60f, 0.70f);
        lvlGo.GetComponent<LayoutElement>().preferredHeight = FntSub * 1.4f;

        // 보상 블록 (VLG, 130px: RewardIcon + RewardTMP 세로 배치)
        var rewBlock2 = new GameObject("RewardBlock", typeof(RectTransform));
        rewBlock2.transform.SetParent(go.transform, false);
        var rewLe2 = rewBlock2.AddComponent<LayoutElement>();
        rewLe2.preferredWidth = 130f; rewLe2.minWidth = 130f;
        var rewVlg2 = rewBlock2.AddComponent<VerticalLayoutGroup>();
        rewVlg2.childAlignment         = TextAnchor.MiddleCenter;
        rewVlg2.childControlWidth      = false; rewVlg2.childControlHeight     = false;
        rewVlg2.childForceExpandWidth  = false; rewVlg2.childForceExpandHeight = false;
        rewVlg2.spacing = 4f;
        rewVlg2.padding = new RectOffset(0, 0, 8, 8);

        var rewIconGo2 = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
        rewIconGo2.transform.SetParent(rewBlock2.transform, false);
        rewIconGo2.GetComponent<Image>().preserveAspect = true;
        rewIconGo2.GetComponent<RectTransform>().sizeDelta = new Vector2(IconSize, IconSize);

        var rewTmpGo2 = new GameObject("RewardTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        rewTmpGo2.transform.SetParent(rewBlock2.transform, false);
        var rewTmp2 = rewTmpGo2.GetComponent<TextMeshProUGUI>();
        rewTmp2.text = "1석"; rewTmp2.fontSize = FntSub; rewTmp2.color = new Color(0.85f, 0.90f, 1.0f);
        rewTmp2.alignment = TextAlignmentOptions.Center;
        rewTmpGo2.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, FntSub * 1.4f);

        // 분해 버튼
        BuildDisassembleBtn(go, 84f);

        // DisEquipRowUI 직렬화 필드 연결
        var equipRowSo = new SerializedObject(go.AddComponent<DisEquipRowUI>());
        equipRowSo.FindProperty("_icon").objectReferenceValue        = iconGo.GetComponent<Image>();
        equipRowSo.FindProperty("_nameTmp").objectReferenceValue     = nameTmp2;
        equipRowSo.FindProperty("_gradeTmp").objectReferenceValue    = gradeTmp2;
        equipRowSo.FindProperty("_levelTmp").objectReferenceValue    = lvlTmp;
        equipRowSo.FindProperty("_rewardIcon").objectReferenceValue  = rewIconGo2.GetComponent<Image>();
        equipRowSo.FindProperty("_rewardTmp").objectReferenceValue   = rewTmp2;
        equipRowSo.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);
        return go;
    }

    // 템플릿 공용 — TMP 생성 (LayoutElement 포함)
    static TextMeshProUGUI BuildRowTMP(GameObject parent, string objName, string text,
                                       int size, FontStyles style, Color color)
    {
        var go  = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI),
                                  typeof(LayoutElement));
        go.transform.SetParent(parent.transform, false);
        var tmp          = go.GetComponent<TextMeshProUGUI>();
        tmp.text         = text;
        tmp.fontSize     = size;
        tmp.fontStyle    = style;
        tmp.color        = color;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        var lel = go.GetComponent<LayoutElement>();
        lel.preferredHeight = size * 1.4f;
        return tmp;
    }

    // 템플릿 공용 — 분해 버튼 (리스너 없음, 런타임에 AddListener)
    static void BuildDisassembleBtn(GameObject parent, float width)
    {
        var btnGo = new GameObject("DisBtn", typeof(RectTransform), typeof(Image));
        btnGo.transform.SetParent(parent.transform, false);
        btnGo.GetComponent<Image>().color = new Color(0.50f, 0.18f, 0.12f, 1f);
        var btnLe = btnGo.AddComponent<LayoutElement>();
        btnLe.preferredWidth = width; btnLe.minWidth = width;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnGo.GetComponent<Image>();

        var lbl = CreateTMP(btnGo, "DisLabel", "분해", FntSub, FontStyles.Bold);
        var rt  = lbl.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        lbl.alignment = TextAlignmentOptions.Center;
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

        // ── 액티브 스킬 박스 ──────────────────────────────────
        const float abTop = 8f;
        const float abH   = 172f;

        var activeBg = CreateImage(panel, "ActiveBg", new Color(0.10f, 0.11f, 0.20f));
        {
            var rt = activeBg.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8,  -(abTop + abH));
            rt.offsetMax = new Vector2(-8, -abTop);
        }

        var activeLabel = CreateTMP(panel, "ActiveLabel", "액티브", FntSub, FontStyles.Normal);
        {
            var rt = activeLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, -(abTop + 28));
            rt.offsetMax = new Vector2(-16, -(abTop + 8));
        }
        activeLabel.alignment = TextAlignmentOptions.Left;
        activeLabel.color     = LabelColor;

        var iconBg = CreateImage(panel, "ActiveSkillIconBg", new Color(0.07f, 0.08f, 0.15f));
        {
            var rt = iconBg.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(16, -(abTop + 114));
            rt.offsetMax = new Vector2(96,  -(abTop + 34));
        }

        var iconImg = CreateImage(panel, "ActiveSkillIcon", new Color(0.28f, 0.32f, 0.52f));
        {
            var rt = iconImg.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(20, -(abTop + 110));
            rt.offsetMax = new Vector2(92,  -(abTop + 38));
        }
        iconImg.preserveAspect = true;
        SetObj(so, "_activeSkillIcon", iconImg);

        var skillText = CreateTMP(panel, "ActiveSkillText", "—", FntMain, FontStyles.Bold);
        {
            var rt = skillText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(104, -(abTop + 70));
            rt.offsetMax = new Vector2(-16, -(abTop + 34));
        }
        skillText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_activeSkillText", skillText);

        var descText = CreateTMP(panel, "ActiveSkillDescText", "", FntSub, FontStyles.Normal);
        {
            var rt = descText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, -(abTop + abH - 6));
            rt.offsetMax = new Vector2(-16, -(abTop + 120));
        }
        descText.alignment        = TextAlignmentOptions.TopLeft;
        descText.color            = LabelColor;
        descText.textWrappingMode = TextWrappingModes.Normal;
        SetObj(so, "_activeSkillDescText", descText);

        // ── 패시브 섹션 ───────────────────────────────────────
        const float gap1    = 28f;
        const float pLblH   = 22f;
        const float gap2    = 6f;
        const float pBoxGap = 10f;

        float pSecY  = abTop + abH + gap1;
        float pFirst = pSecY + pLblH + gap2;

        var passiveLabel = CreateTMP(panel, "PassiveLabel", "패시브", FntSub, FontStyles.Normal);
        {
            var rt = passiveLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, -(pSecY + pLblH));
            rt.offsetMax = new Vector2(-16, -pSecY);
        }
        passiveLabel.alignment = TextAlignmentOptions.Left;
        passiveLabel.color     = LabelColor;

        // 패시브 박스 컨테이너 — VLG+CSF 로 내용에 따라 높이 자동 조절
        var passiveCont = new GameObject("PassiveContainer", typeof(RectTransform));
        passiveCont.transform.SetParent(panel.transform, false);
        {
            var rt = passiveCont.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(-16f, 0f);
            rt.anchoredPosition = new Vector2(0f, -pFirst);
        }
        var cVlg = passiveCont.AddComponent<VerticalLayoutGroup>();
        cVlg.spacing              = pBoxGap;
        cVlg.padding              = new RectOffset(0, 0, 0, 0);
        cVlg.childForceExpandWidth  = true;
        cVlg.childForceExpandHeight = false;
        cVlg.childControlWidth      = true;
        cVlg.childControlHeight     = true;
        cVlg.childAlignment         = TextAnchor.UpperLeft;
        passiveCont.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var (p0Name, p0Desc) = BuildPassiveBox(passiveCont, "Passive0");
        var (p1Name, p1Desc) = BuildPassiveBox(passiveCont, "Passive1");
        var (p2Name, p2Desc) = BuildPassiveBox(passiveCont, "Passive2");

        SetObj(so, "_passive0Text",     p0Name);
        SetObj(so, "_passive1Text",     p1Name);
        SetObj(so, "_passive2Text",     p2Name);
        SetObj(so, "_passive0DescText", p0Desc);
        SetObj(so, "_passive1DescText", p1Desc);
        SetObj(so, "_passive2DescText", p2Desc);

        return panel;
    }

    // 패시브 박스 — 내용 길이에 따라 높이 자동 조절 (ContentSizeFitter)
    static (TextMeshProUGUI name, TextMeshProUGUI desc) BuildPassiveBox(
        GameObject container, string id)
    {
        // 박스 배경: VLG + CSF 로 내용에 맞게 높이 자동 결정
        var box = new GameObject($"{id}Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(container.transform, false);
        box.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.20f);

        var vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding              = new RectOffset(12, 12, 8, 10);
        vlg.spacing              = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        box.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // 이름 (고정 높이)
        var nameGo = new GameObject($"{id}Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(box.transform, false);
        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text             = "—";
        nameTmp.fontSize         = FntSub;
        nameTmp.fontStyle        = FontStyles.Bold;
        nameTmp.alignment        = TextAlignmentOptions.Left;
        nameTmp.color            = Color.white;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameGo.AddComponent<LayoutElement>().preferredHeight = FntSub + 8f;

        // 설명 (텍스트 줄 수에 맞게 높이 자동 결정)
        var descGo = new GameObject($"{id}Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(box.transform, false);
        var descTmp = descGo.GetComponent<TextMeshProUGUI>();
        descTmp.text             = "";
        descTmp.fontSize         = FntSub;
        descTmp.fontStyle        = FontStyles.Normal;
        descTmp.alignment        = TextAlignmentOptions.TopLeft;
        descTmp.color            = LabelColor;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode     = TextOverflowModes.Overflow;
        descGo.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        return (nameTmp, descTmp);
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
            rt.offsetMin = new Vector2(0, -88);
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

        // ── 분해 버튼 (헤더 우측 끝에서 두 번째) ────────────
        var disBtnGo = CreatePanel(header, "DisassembleButton", new Color(0.40f, 0.18f, 0.12f));
        {
            var rt = disBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-322, 5);
            rt.offsetMax = new Vector2(-228, -5);
        }
        var disBtn = disBtnGo.AddComponent<Button>();
        disBtn.targetGraphic = disBtnGo.GetComponent<Image>();
        SetObj(so, "_disassembleBtn", disBtn);
        var disLbl = CreateTMP(disBtnGo, "Label", "분해", FntSub, FontStyles.Bold);
        {
            var rt = disLbl.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        }
        disLbl.alignment = TextAlignmentOptions.Center;

        // ── 용병 고용 버튼 (헤더 우측) ──────────────────────
        var hireBtnGo = CreatePanel(header, "HireButton", new Color(0.14f, 0.38f, 0.18f));
        {
            var rt = hireBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-222, 5);
            rt.offsetMax = new Vector2(-8,  -5);
        }
        var hireBtn = hireBtnGo.AddComponent<Button>();
        hireBtn.targetGraphic = hireBtnGo.GetComponent<Image>();
        SetObj(so, "_hireBtn", hireBtn);

        var hireLbl = CreateTMP(hireBtnGo, "Label", "용병 고용", FntMain, FontStyles.Bold);
        {
            var rt = hireLbl.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0.55f, 1);
            rt.offsetMin = new Vector2(8, 0);
            rt.offsetMax = new Vector2(-2, 0);
        }
        hireLbl.alignment = TextAlignmentOptions.Center;

        var hireCostRow = new GameObject("CostRow", typeof(RectTransform));
        hireCostRow.transform.SetParent(hireBtnGo.transform, false);
        {
            var rt = hireCostRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.55f, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-6, -2);
        }
        BuildCostHlg(hireCostRow);
        var hireCostIcon = CreateImage(hireCostRow, "CostIcon", new Color(0.8f, 0.8f, 0.8f));
        hireCostIcon.preserveAspect = true;
        AddIconLE(hireCostIcon, 18f);
        SetObj(so, "_hireCostIcon", hireCostIcon);
        var hireCostText = CreateTMP(hireCostRow, "CostText", "500", FntSub, FontStyles.Normal);
        hireCostText.alignment = TextAlignmentOptions.Left;
        hireCostText.color     = new Color(1.0f, 0.85f, 0.20f);
        hireCostText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        SetObj(so, "_hireCostText", hireCostText);

        // ── 재화 바 (헤더 바로 아래) ─────────────────────────
        var currencyBar = CreatePanel(right, "CurrencyBar", new Color(0.06f, 0.07f, 0.12f));
        {
            var rt = currencyBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(88 + CurrencyBarH));
            rt.offsetMax = new Vector2(0, -88);
        }
        var cBarHlg = currencyBar.AddComponent<HorizontalLayoutGroup>();
        cBarHlg.childAlignment         = TextAnchor.MiddleLeft;
        cBarHlg.childForceExpandWidth  = false;
        cBarHlg.childForceExpandHeight = true;
        cBarHlg.spacing                = 0;
        cBarHlg.padding                = new RectOffset(12, 12, 4, 4);

        BuildCurrencyWidget(currencyBar, eItem.SoldierShard);
        BuildCurrencyWidget(currencyBar, eItem.GeneralUpgradeStone);
        BuildCurrencyWidget(currencyBar, eItem.EquipUpgradeStone);
        BuildCurrencyWidget(currencyBar, eItem.BattleStone);

        // ── ScrollView ────────────────────────────────────────
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(right.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -(88 + CurrencyBarH));
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
        grid.cellSize        = new Vector2(356, 170);
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
    //  HeroCard 프리팹 (356×170px)
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

        // 텍스트 영역 y 오프셋 (상단 앵커 기준, 4px 갭)
        // Row1 이름/등급:  top=4,   bot=40  (36px)
        // Row2 레벨/직업:  top=44,  bot=72  (28px)
        // Divider:          top=76,  bot=78
        // Row3 HP/공격:    top=82,  bot=110 (28px)
        // Row4 방어/용병:  top=114, bot=142 (28px)
        // DeployBadge: 하단 앵커 y=0~20

        // ── 이름 (상단 기준, 36px 높이, NoWrap + Ellipsis) ───
        var nameText = CreateTMP(card, "NameText", "이름", FntSub, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(ix,    1f);
            rt.anchorMax = new Vector2(0.78f, 1f);
            rt.offsetMin = new Vector2(0,  -40);
            rt.offsetMax = new Vector2(0,   -4);
        }
        nameText.alignment        = TextAlignmentOptions.MidlineLeft;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode     = TextOverflowModes.Ellipsis;

        // ── 등급 뱃지 배경 (상단 기준, 우측, 행 내 2px 인셋) ─
        var gradeBadge = CreateImage(card, "GradeBadge", new Color(0.55f, 0.55f, 0.55f));
        {
            var rt = gradeBadge.rectTransform;
            rt.anchorMin = new Vector2(0.78f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(2,  -38);
            rt.offsetMax = new Vector2(-4,  -6);
        }

        // ── 등급 텍스트 (뱃지 위 오버레이) ───────────────────
        var gradeText = CreateTMP(card, "GradeText", "일반", FntSub, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(0.78f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(2,  -38);
            rt.offsetMax = new Vector2(-4,  -6);
        }
        gradeText.alignment        = TextAlignmentOptions.Center;
        gradeText.textWrappingMode = TextWrappingModes.NoWrap;
        gradeText.color            = Color.white;

        // ── 레벨 (상단 기준, 좌측) ────────────────────────────
        var levelText = CreateTMP(card, "LevelText", "Lv.1", FntSub, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(ix,    1f);
            rt.anchorMax = new Vector2(0.52f, 1f);
            rt.offsetMin = new Vector2(0, -72);
            rt.offsetMax = new Vector2(0, -44);
        }
        levelText.alignment = TextAlignmentOptions.MidlineLeft;
        levelText.color     = LabelColor;

        // ── 직업 (상단 기준, 우측) ────────────────────────────
        var jobText = CreateTMP(card, "JobText", "기사", FntSub, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(0.52f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(0,  -72);
            rt.offsetMax = new Vector2(-6, -44);
        }
        jobText.alignment = TextAlignmentOptions.MidlineRight;
        jobText.color     = LabelColor;

        // ── 구분선 (상단 기준) ────────────────────────────────
        var statDiv = CreateImage(card, "StatDivider", DividerColor);
        {
            var rt = statDiv.rectTransform;
            rt.anchorMin = new Vector2(ix,    1f);
            rt.anchorMax = new Vector2(0.97f, 1f);
            rt.offsetMin = new Vector2(0, -78);
            rt.offsetMax = new Vector2(0, -76);
        }

        // ── 스탯 행 1: HP / 공격 (상단 기준) ─────────────────
        var hpLbl = CreateTMP(card, "HpLabel", "체력", FntSub, FontStyles.Normal);
        {
            var rt = hpLbl.rectTransform;
            rt.anchorMin = new Vector2(ix,    1f);
            rt.anchorMax = new Vector2(0.43f, 1f);
            rt.offsetMin = new Vector2(0,  -110);
            rt.offsetMax = new Vector2(0,   -82);
        }
        hpLbl.alignment = TextAlignmentOptions.MidlineLeft;
        hpLbl.color     = LabelColor;

        var hpText = CreateTMP(card, "HpText", "—", FntSub, FontStyles.Bold);
        {
            var rt = hpText.rectTransform;
            rt.anchorMin = new Vector2(0.43f, 1f);
            rt.anchorMax = new Vector2(0.63f, 1f);
            rt.offsetMin = new Vector2(-2, -110);
            rt.offsetMax = new Vector2(0,   -82);
        }
        hpText.alignment = TextAlignmentOptions.MidlineRight;

        var atkLbl = CreateTMP(card, "AtkLabel", "공격", FntSub, FontStyles.Normal);
        {
            var rt = atkLbl.rectTransform;
            rt.anchorMin = new Vector2(0.65f, 1f);
            rt.anchorMax = new Vector2(0.80f, 1f);
            rt.offsetMin = new Vector2(0,  -110);
            rt.offsetMax = new Vector2(0,   -82);
        }
        atkLbl.alignment = TextAlignmentOptions.MidlineLeft;
        atkLbl.color     = LabelColor;

        var atkText = CreateTMP(card, "AtkText", "—", FntSub, FontStyles.Bold);
        {
            var rt = atkText.rectTransform;
            rt.anchorMin = new Vector2(0.80f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(0,   -110);
            rt.offsetMax = new Vector2(-6,   -82);
        }
        atkText.alignment = TextAlignmentOptions.MidlineRight;

        // ── 스탯 행 2: 방어율 / 용병수 (상단 기준) ───────────
        var defLbl = CreateTMP(card, "DefLabel", "방어", FntSub, FontStyles.Normal);
        {
            var rt = defLbl.rectTransform;
            rt.anchorMin = new Vector2(ix,    1f);
            rt.anchorMax = new Vector2(0.43f, 1f);
            rt.offsetMin = new Vector2(0,  -142);
            rt.offsetMax = new Vector2(0,  -114);
        }
        defLbl.alignment = TextAlignmentOptions.MidlineLeft;
        defLbl.color     = LabelColor;

        var defText = CreateTMP(card, "DefText", "—", FntSub, FontStyles.Bold);
        {
            var rt = defText.rectTransform;
            rt.anchorMin = new Vector2(0.43f, 1f);
            rt.anchorMax = new Vector2(0.63f, 1f);
            rt.offsetMin = new Vector2(-2, -142);
            rt.offsetMax = new Vector2(0,  -114);
        }
        defText.alignment = TextAlignmentOptions.MidlineRight;

        var soldLbl = CreateTMP(card, "SoldierLabel", "용병", FntSub, FontStyles.Normal);
        {
            var rt = soldLbl.rectTransform;
            rt.anchorMin = new Vector2(0.65f, 1f);
            rt.anchorMax = new Vector2(0.80f, 1f);
            rt.offsetMin = new Vector2(0,  -142);
            rt.offsetMax = new Vector2(0,  -114);
        }
        soldLbl.alignment = TextAlignmentOptions.MidlineLeft;
        soldLbl.color     = LabelColor;

        var soldText = CreateTMP(card, "SoldierText", "—", FntSub, FontStyles.Bold);
        {
            var rt = soldText.rectTransform;
            rt.anchorMin = new Vector2(0.80f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(0,   -142);
            rt.offsetMax = new Vector2(-6,  -114);
        }
        soldText.alignment = TextAlignmentOptions.MidlineRight;

        // ── 배치 배지 (초상화 우상단 — 번호만 표시, 33×33) ──────
        const float bdgSz = 33f;
        // 초상화: x=4..104, top y=-4.  배지는 초상화 우상단 모서리에 맞춤
        var deployBadge = new GameObject("DeployBadge", typeof(RectTransform), typeof(Image));
        deployBadge.transform.SetParent(card.transform, false);
        deployBadge.GetComponent<Image>().color = new Color(0.14f, 0.42f, 0.82f, 0.92f);
        {
            var rt = deployBadge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(104f - bdgSz, -(4f + bdgSz));  // bottom-left
            rt.offsetMax = new Vector2(104f,          -4f);            // top-right
        }
        deployBadge.SetActive(false);

        var deployText = CreateTMP(deployBadge, "DeployText", "1", 36, FontStyles.Bold);
        {
            var rt = deployText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        deployText.alignment = TextAlignmentOptions.Center;
        deployText.color     = Color.white;

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
        SetObj(cSo, "_gradeBadge",     gradeBadge);
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
        SetObj(cSo, "_deployBadge",    deployBadge);
        SetObj(cSo, "_deployText",     deployText);
        cSo.ApplyModifiedProperties();

        return card;
    }

    // ── 버튼 비용 행 헬퍼 ────────────────────────────────────

    static void BuildCostHlg(GameObject go)
    {
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing                = 3f;
    }

    static void AddIconLE(Image img, float size)
    {
        img.rectTransform.sizeDelta = new Vector2(size, size);
        var le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = size;
        le.preferredHeight = size;
        le.minWidth        = size;
        le.minHeight       = size;
    }

    // ── CurrencyWidget 아이템 (아이콘 + 수량 텍스트) ─────────

    static void BuildCurrencyWidget(GameObject parent, eItem item)
    {
        var container = new GameObject($"CW_{item}", typeof(RectTransform));
        container.transform.SetParent(parent.transform, false);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 4f;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(4, 8, 2, 2);

        var cle = container.AddComponent<LayoutElement>();
        cle.preferredWidth = 140f;
        cle.minWidth       = 140f;
        cle.flexibleWidth  = 0f;

        // 아이콘 36×36
        var iconImg = CreateImage(container, "Icon", new Color(0.55f, 0.55f, 0.60f));
        iconImg.rectTransform.sizeDelta = new Vector2(36f, 36f);
        var ile = iconImg.gameObject.AddComponent<LayoutElement>();
        ile.preferredWidth  = 36f;
        ile.preferredHeight = 36f;
        ile.minWidth        = 36f;
        ile.minHeight       = 36f;

        // 수량 텍스트
        var amtTmp = CreateTMP(container, "Amount", "0", FntSub, FontStyles.Normal);
        amtTmp.alignment        = TextAlignmentOptions.Left;
        amtTmp.textWrappingMode = TextWrappingModes.NoWrap;
        amtTmp.color            = new Color(0.85f, 0.85f, 0.90f);
        var ale = amtTmp.gameObject.AddComponent<LayoutElement>();
        ale.preferredWidth = 90f;
        ale.minWidth       = 70f;

        var widget = container.AddComponent<CurrencyWidget>();
        var wSo    = new SerializedObject(widget);
        wSo.FindProperty("_item").intValue                   = (int)item;
        wSo.FindProperty("_amountText").objectReferenceValue = amtTmp;
        wSo.FindProperty("_icon").objectReferenceValue       = iconImg;
        wSo.ApplyModifiedProperties();
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
