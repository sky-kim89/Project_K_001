#if UNITY_EDITOR
using System.IO;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroDetailPopupCreator.cs
//  Tools > Project K > Popup > Create HeroDetail Popup Prefab
//
//  생성 에셋:
//    Assets/_project/2.Prefabs/UI/HeroDetailPopup.prefab
//
//  레이아웃 (HeroPanel LeftPanel 동일 구조):
//    GradeBorder (6px 좌측 등급 컬러 바)
//    PortraitSection  (PortraitH px)  — 초상화 + 이름·레벨·직업·등급
//    DeploySlotRow    (DeployRowH px) — 배치 슬롯 5개
//    TabBar           (TabBarH px)    — [스탯] [장비] [스킬]
//    TabContentArea   (나머지)        — StatPanel / EquipPanel / SkillPanel
//    Footer           (FooterH px)    — 해고 버튼 + 닫기 버튼
// ============================================================

public static class HeroDetailPopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/HeroDetailPopup.prefab";

    const float PopupW    = 576f;
    const float PopupH    = 940f;
    const float PortraitH = 240f;
    const float DeployRowH = 52f;
    const float TabBarH   = 44f;
    const float FooterH   = 90f;

    // ── 색상 (HeroPanelCreator 와 동일) ───────────────────────
    static readonly Color BgColor           = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color SectionColor      = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color TabBarColor       = new Color(0.10f, 0.12f, 0.22f, 1f);
    static readonly Color DividerColor      = new Color(0.18f, 0.18f, 0.26f, 1f);
    static readonly Color SlotColor         = new Color(0.14f, 0.14f, 0.22f, 1f);
    static readonly Color LabelColor        = new Color(0.60f, 0.60f, 0.70f, 1f);
    static readonly Color TabActiveColor    = new Color(0.40f, 0.72f, 1.00f, 1f);
    static readonly Color TabInactiveColor  = new Color(0.72f, 0.72f, 0.78f, 1f);
    static readonly Color LevelUpBtnColor   = new Color(0.16f, 0.32f, 0.58f, 1f);
    static readonly Color SoldierUpBtnColor = new Color(0.14f, 0.32f, 0.20f, 1f);
    static readonly Color DeploySlotEmpty   = new Color(0.20f, 0.20f, 0.23f, 1f);
    static readonly Color FireBtnColor      = new Color(0.48f, 0.10f, 0.10f, 1f);
    static readonly Color CloseBtnColor     = new Color(0.18f, 0.18f, 0.26f, 1f);

    // 폰트 크기 — UIScale 기준 (HeroPanelCreator 와 동일)
    static readonly int FntHero = (int)UIScale.FontLg;  // 54
    static readonly int FntMain = (int)UIScale.FontMd;  // 40
    static readonly int FntSub  = (int)UIScale.FontSm;  // 30
    static readonly int FntMini = 24;

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create HeroDetail Popup Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI");
        AssetDatabase.Refresh();

        var go = BuildPopup();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log("[HeroDetailPopupCreator] HeroDetailPopup 프리팹 생성 완료");
    }

    // ============================================================
    //  팝업 루트
    // ============================================================

    static GameObject BuildPopup()
    {
        var root = CreatePanel(null, "HeroDetailPopup", new Color(0.04f, 0.04f, 0.09f, 0.97f));
        root.AddComponent<CanvasGroup>();
        {
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-600f, 0f);
            rt.sizeDelta        = new Vector2(PopupW, PopupH);
        }

        var popup = root.AddComponent<HeroDetailPopup>();
        var so    = new SerializedObject(popup);

        var typeProp = so.FindProperty("_popupType");
        if (typeProp != null) typeProp.intValue = (int)PopupType.HeroDetail;

        // 좌측 등급 컬러 바
        var gradeBorder = CreateImage(root, "GradeBorder", DividerColor);
        {
            var rt = gradeBorder.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(6, 0);
        }
        SetObj(so, "_gradeBorder", gradeBorder);

        BuildPortraitSection(root, so);
        BuildDeploySlotRow(root, so);
        BuildTabSection(root, so);
        BuildFooter(root, so);

        so.ApplyModifiedProperties();
        return root;
    }

    // ============================================================
    //  초상화 섹션 (상단 PortraitH px) — HeroPanelCreator 동일
    // ============================================================

    static void BuildPortraitSection(GameObject root, SerializedObject so)
    {
        var section = CreatePanel(root, "PortraitSection", new Color(0.07f, 0.07f, 0.13f));
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

        // PortraitPreview (UnitAppearanceBridge)
        var preview = new GameObject("PortraitPreview", typeof(RectTransform));
        preview.transform.SetParent(section.transform, false);
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

    // ============================================================
    //  배치 슬롯 행 (DeployRowH px) — HeroPanelCreator 동일
    // ============================================================

    static void BuildDeploySlotRow(GameObject root, SerializedObject so)
    {
        var row = CreatePanel(root, "DeploySlotRow", new Color(0.07f, 0.08f, 0.14f));
        {
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(PortraitH + DeployRowH));
            rt.offsetMax = new Vector2(0, -PortraitH);
        }

        // 레이블
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

        // 슬롯 5개
        const float slotPad = 6f;
        float slotsX  = 134f;
        float slotsW  = PopupW - slotsX - 8f;
        float slotW   = (slotsW - slotPad * 4f) / 5f;
        float slotH   = DeployRowH - 10f;

        var slotBtns = new Button[5];
        var slotBgs  = new Image[5];
        var slotNums = new TextMeshProUGUI[5];

        for (int i = 0; i < 5; i++)
        {
            float sx = slotsX + i * (slotW + slotPad);
            var slotGo  = new GameObject($"DeploySlot{i}", typeof(RectTransform), typeof(Image));
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

        var rowUI = row.AddComponent<DeploySlotRowUI>();
        var rso   = new SerializedObject(rowUI);
        SetObjArray(rso, "_slotButtons",  System.Array.ConvertAll(slotBtns, b => (Object)b));
        SetObjArray(rso, "_slotBgs",      System.Array.ConvertAll(slotBgs,  b => (Object)b));
        SetObjArray(rso, "_slotNumTexts", System.Array.ConvertAll(slotNums, t => (Object)t));
        rso.ApplyModifiedProperties();

        SetObj(so, "_deploySlotRow", rowUI);
    }

    // ============================================================
    //  탭 섹션 (TabBar + TabContentArea) — HeroPanelCreator 동일
    // ============================================================

    static void BuildTabSection(GameObject root, SerializedObject so)
    {
        float tabTop = PortraitH + DeployRowH;

        // 탭 바
        var tabBar = CreatePanel(root, "TabBar", TabBarColor);
        {
            var rt = tabBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(tabTop + TabBarH));
            rt.offsetMax = new Vector2(0, -tabTop);
        }

        string[] tabLabels  = { "스탯", "장비", "스킬" };
        var tabButtons = new Button[3];
        for (int i = 0; i < 3; i++)
            tabButtons[i] = BuildTabButton(tabBar, tabLabels[i], i, i == 0);

        // 탭 콘텐츠 영역 (TabBar 아래 ~ Footer 위)
        var contentArea = CreatePanel(root, "TabContentArea", SectionColor);
        {
            var rt = contentArea.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, FooterH);
            rt.offsetMax = new Vector2(0, -(tabTop + TabBarH));
        }

        var statPanel  = BuildStatPanel (contentArea, so);
        var equipPanel = BuildEquipPanel(contentArea, so);
        var skillPanel = BuildSkillPanel(contentArea, so);

        equipPanel.SetActive(false);
        skillPanel.SetActive(false);

        SetObjArray(so, "_tabButtons", new Object[] { tabButtons[0], tabButtons[1], tabButtons[2] });
        SetObjArray(so, "_tabPanels",  new Object[] { statPanel, equipPanel, skillPanel });
    }

    // ── 탭 버튼 ────────────────────────────────────────────────

    static Button BuildTabButton(GameObject tabBar, string label, int index, bool isActive)
    {
        float step = 1f / 3f;
        var go = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Image));
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

    // ============================================================
    //  StatPanel — HeroPanelCreator 동일
    // ============================================================

    static GameObject BuildStatPanel(GameObject contentArea, SerializedObject so)
    {
        const float BtnH     = 42f;
        const float BtnPad   = 7f;
        const float ExpBarH  = 8f;
        const float ExpTextH = 22f;
        const float ExpGap   = 4f;

        float expBarBottom = BtnPad + BtnH + BtnPad;
        float expBarTop    = expBarBottom + ExpBarH;
        float expTxtBottom = expBarTop + ExpGap;
        float expTxtTop    = expTxtBottom + ExpTextH;
        float listBottom   = expTxtTop + BtnPad;

        var panel = CreatePanel(contentArea, "StatPanel", SectionColor);
        Stretch(panel);

        // 스탯 행 컨테이너
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
        vlg.childControlWidth      = true;
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
        SetObj(so, "_statListContainer", listContainer.transform);

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
        var expFill = CreatePanel(expBarBg, "ExpBarFill", new Color(0.25f, 0.65f, 1.00f));
        {
            var rt = expFill.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        SetObj(so, "_expBarFill", expFill.GetComponent<Image>());

        // 레벨업 버튼 (왼쪽 절반)
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
        var lvCostText = CreateTMP(lvCostRow, "CostText", "0", FntSub, FontStyles.Normal);
        lvCostText.alignment = TextAlignmentOptions.Left;
        lvCostText.color     = new Color(1.0f, 0.85f, 0.20f);
        lvCostText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        SetObj(so, "_levelUpCostText", lvCostText);

        // 용병 수 증가 버튼 (오른쪽 절반)
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
        var row = new GameObject($"Stat_{id}", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent.transform, false);
        row.GetComponent<Image>().color = Color.clear;
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(PopupW, 52f);

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 52f;
        le.minHeight       = 44f;

        var btn = row.AddComponent<Button>();
        btn.targetGraphic = row.GetComponent<Image>();

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.spacing                = 0;
        hlg.padding                = new RectOffset(8, 8, 0, 0);

        var lbl = CreateTMP(row, "Label", label, FntMain, FontStyles.Normal);
        lbl.alignment        = TextAlignmentOptions.Left;
        lbl.color            = LabelColor;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Ellipsis;
        var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 90f;
        lblLE.flexibleWidth  = 0f;

        var val = CreateTMP(row, "Value", "—", FntMain, FontStyles.Bold);
        val.alignment        = TextAlignmentOptions.Right;
        val.textWrappingMode = TextWrappingModes.NoWrap;
        val.overflowMode     = TextOverflowModes.Ellipsis;
        val.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

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

    // ============================================================
    //  EquipPanel — HeroPanelCreator 동일
    // ============================================================

    static GameObject BuildEquipPanel(GameObject contentArea, SerializedObject so)
    {
        var panel = CreatePanel(contentArea, "EquipPanel", SectionColor);
        Stretch(panel);

        var (btn0, name0, bar0, icon0, stat0, lock0, enh0, enhCost0, enhIcon0) =
            BuildEquipSlot(panel, "EquipSlot0", 10f, 150f);
        SetObj(so, "_equip0Btn",             btn0);
        SetObj(so, "_equip0NameText",        name0);
        SetObj(so, "_equip0GradeBar",        bar0);
        SetObj(so, "_equip0Icon",            icon0);
        SetObj(so, "_equip0StatText",        stat0);
        SetObj(so, "_equip0LockBadge",       lock0);
        SetObj(so, "_equip0EnhanceBtn",      enh0);
        SetObj(so, "_equip0EnhanceCostText", enhCost0);
        SetObj(so, "_equip0EnhanceCostIcon", enhIcon0);

        var (btn1, name1, bar1, icon1, stat1, lock1, enh1, enhCost1, enhIcon1) =
            BuildEquipSlot(panel, "EquipSlot1", 172f, 150f);
        SetObj(so, "_equip1Btn",             btn1);
        SetObj(so, "_equip1NameText",        name1);
        SetObj(so, "_equip1GradeBar",        bar1);
        SetObj(so, "_equip1Icon",            icon1);
        SetObj(so, "_equip1StatText",        stat1);
        SetObj(so, "_equip1LockBadge",       lock1);
        SetObj(so, "_equip1EnhanceBtn",      enh1);
        SetObj(so, "_equip1EnhanceCostText", enhCost1);
        SetObj(so, "_equip1EnhanceCostIcon", enhIcon1);

        return panel;
    }

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

        var gradeBar = CreateImage(slotBg, "GradeBar", DividerColor);
        {
            var rt = gradeBar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 3);
            rt.offsetMax = new Vector2(5, -3);
        }

        const float EnhBtnW = 148f;
        const float EnhBtnH = 40f;

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
            rt.offsetMin = new Vector2(14,                -iconSize * 0.5f + 4f);
            rt.offsetMax = new Vector2(14 + iconSize - 8f, iconSize * 0.5f - 4f);
        }
        iconImg.preserveAspect = true;

        float textLeft = 10f + iconSize + 12f;

        var nameText = CreateTMP(slotBg, "EquipNameText", "없음", FntMain, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.60f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(textLeft, 2);
            rt.offsetMax = new Vector2(-8f, -4);
        }
        nameText.alignment = TextAlignmentOptions.Left;

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

        var enhIconGo  = new GameObject("CostIcon", typeof(RectTransform), typeof(Image));
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

    // ============================================================
    //  SkillPanel — HeroPanelCreator 동일
    // ============================================================

    static GameObject BuildSkillPanel(GameObject contentArea, SerializedObject so)
    {
        const float abTop = 8f;
        const float abH   = 172f;

        var panel = CreatePanel(contentArea, "SkillPanel", SectionColor);
        Stretch(panel);

        // 액티브 스킬 박스
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

        // 패시브 섹션
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

    static (TextMeshProUGUI name, TextMeshProUGUI desc) BuildPassiveBox(
        GameObject container, string id)
    {
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

    // ============================================================
    //  Footer: 해고 버튼 + 닫기 버튼
    // ============================================================

    static void BuildFooter(GameObject root, SerializedObject so)
    {
        const float CloseBtnW = 110f;
        const float BtnPad    = 8f;

        var footer = CreatePanel(root, "Footer", new Color(0.04f, 0.04f, 0.08f, 1f));
        {
            var rt = footer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, FooterH);
        }

        // 상단 구분선
        var divider = CreateImage(footer, "TopDivider", DividerColor);
        {
            var rt = divider.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -1);
            rt.offsetMax = new Vector2(0,  0);
        }

        // 닫기 버튼 (오른쪽)
        var closeBtnGo = CreatePanel(footer, "CloseButton", CloseBtnColor);
        {
            var rt = closeBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-(CloseBtnW + BtnPad), BtnPad);
            rt.offsetMax = new Vector2(-BtnPad, -BtnPad);
        }
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
        SetObj(so, "_closeBtn", closeBtn);

        var closeLbl = CreateTMP(closeBtnGo, "Label", "닫기", FntMain, FontStyles.Normal);
        closeLbl.rectTransform.anchorMin = Vector2.zero;
        closeLbl.rectTransform.anchorMax = Vector2.one;
        closeLbl.alignment = TextAlignmentOptions.Center;

        // 해고 버튼 (나머지 전체)
        var fireBtnGo = CreatePanel(footer, "FireButton", FireBtnColor);
        {
            var rt = fireBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(BtnPad, BtnPad);
            rt.offsetMax = new Vector2(-(CloseBtnW + BtnPad * 2f), -BtnPad);
        }
        var fireBtn = fireBtnGo.AddComponent<Button>();
        fireBtn.targetGraphic = fireBtnGo.GetComponent<Image>();
        SetObj(so, "_fireBtn", fireBtn);

        // 해고 텍스트 (왼쪽)
        var fireLbl = CreateTMP(fireBtnGo, "Label", "해고", FntMain, FontStyles.Bold);
        {
            var rt = fireLbl.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.45f, 1);
            rt.offsetMin = new Vector2(12, 0);
            rt.offsetMax = Vector2.zero;
        }
        fireLbl.alignment = TextAlignmentOptions.Right;

        // 획득 조각 수 텍스트 (오른쪽)
        var shardText = CreateTMP(fireBtnGo, "ShardText", "0 조각", FntSub, FontStyles.Normal);
        {
            var rt = shardText.rectTransform;
            rt.anchorMin = new Vector2(0.47f, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(-8, 0);
        }
        shardText.alignment = TextAlignmentOptions.Left;
        shardText.color     = new Color(0.85f, 0.65f, 0.65f);
        SetObj(so, "_fireShardText", shardText);
    }

    // ============================================================
    //  UI 헬퍼 (HeroPanelCreator 복사)
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

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
        else Debug.LogWarning($"[HeroDetailPopupCreator] 필드 없음: {field}");
    }

    static void SetObjArray(SerializedObject so, string field, Object[] objs)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"[HeroDetailPopupCreator] 배열 필드 없음: {field}"); return; }
        prop.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
    }
}
#endif
