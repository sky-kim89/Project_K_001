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
//  레이아웃 (1000px):
//    GradeBorder      (6px 좌측 등급 컬러 바)
//    CloseButton      (52px 우측 상단 오버레이 X 버튼)
//    PortraitSection  (PortraitH=280px)  — 초상화·이름·레벨·직업 (텍스트 BG 오버레이)
//    GrowthRow        (GrowthRowH=112px) — EXP바 + 레벨업·용병 버튼
//    TabBar           (TabBarH=60px)     — [스탯] [장비] [스킬]
//    TabContentArea   (나머지 548px)     — StatPanel / EquipPanel / SkillPanel
// ============================================================

public static class HeroDetailPopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/HeroDetailPopup.prefab";

    const float PopupW        = 576f;
    const float PopupH        = 1000f;
    const float PortraitH     = 280f;
    const float GrowthRowH    = 122f;  // 버튼 높이 +10 반영
    const float GrowthTabGap  = 4f;    // GrowthRow ~ TabBar 사이 간격
    const float TabBarH       = 60f;

    // ── 색상 ──────────────────────────────────────────────────
    static readonly Color SectionColor      = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color TabBarColor       = new Color(0.10f, 0.12f, 0.22f, 1f);
    static readonly Color DividerColor      = new Color(0.18f, 0.18f, 0.26f, 1f);
    static readonly Color SlotColor         = new Color(0.14f, 0.14f, 0.22f, 1f);
    static readonly Color LabelColor        = new Color(0.60f, 0.60f, 0.70f, 1f);
    static readonly Color TabActiveColor    = new Color(0.40f, 0.72f, 1.00f, 1f);
    static readonly Color TabInactiveColor  = new Color(0.72f, 0.72f, 0.78f, 1f);
    static readonly Color LevelUpBtnColor   = new Color(0.16f, 0.32f, 0.58f, 1f);
    static readonly Color SoldierUpBtnColor = new Color(0.14f, 0.32f, 0.20f, 1f);
    static readonly Color GrowthBgColor     = new Color(0.07f, 0.08f, 0.14f, 1f);

    // 폰트 크기
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
        var growthRowGo = BuildGrowthRow(root, so);
        SetObj(so, "_growthRow", growthRowGo);
        BuildTabSection(root, so);

        // X 닫기 버튼 (우측 상단 오버레이)
        var xBtnGo = CreatePanel(root, "CloseButton", new Color(0.22f, 0.12f, 0.22f, 0.88f));
        {
            var rt = xBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-52f, -52f);
            rt.offsetMax = new Vector2(0f, 0f);
        }
        var xBtn = xBtnGo.AddComponent<Button>();
        xBtn.targetGraphic = xBtnGo.GetComponent<Image>();
        SetObj(so, "_closeBtn", xBtn);

        var xLbl = CreateTMP(xBtnGo, "Label", "✕", FntMain, FontStyles.Bold);
        xLbl.rectTransform.anchorMin = Vector2.zero;
        xLbl.rectTransform.anchorMax = Vector2.one;
        xLbl.alignment = TextAlignmentOptions.Center;
        xLbl.color     = new Color(0.90f, 0.80f, 0.80f);

        so.ApplyModifiedProperties();
        return root;
    }

    // ============================================================
    //  초상화 섹션 (상단 PortraitH px)
    //  이름·레벨·직업 텍스트를 PortraitBg 하단에 오버레이
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

        // 초상화 배경 — 섹션 전체 커버
        var portraitBg = CreateImage(section, "PortraitBg", new Color(0.16f, 0.27f, 0.56f));
        {
            var rt = portraitBg.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2);
            rt.offsetMax = new Vector2(-8, -8);
        }
        SetObj(so, "_portraitBg", portraitBg);

        // 초상화 이미지 — 위쪽으로 치우치게 배치
        var portraitImg = CreateImage(section, "PortraitImage", Color.clear);
        {
            var rt = portraitImg.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 48f);
            rt.sizeDelta        = new Vector2(165, 165);
        }
        portraitImg.preserveAspect = true;
        SetObj(so, "_portraitImage", portraitImg);

        // 텍스트 가독성 오버레이 (BG 하단 반투명 검정)
        var textOverlay = CreateImage(section, "TextOverlay", new Color(0f, 0f, 0f, 0.52f));
        {
            var rt = textOverlay.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(8, 2);
            rt.offsetMax = new Vector2(-8, 102);
        }

        // 이름 (BG 하단 오버레이 — 위쪽)
        var nameText = CreateTMP(section, "NameText", "영웅 이름", FntHero, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(14, 56);
            rt.offsetMax = new Vector2(-14, 96);
        }
        nameText.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_nameText", nameText);

        // 레벨 (BG 하단 오버레이 — 아래 왼쪽)
        var levelText = CreateTMP(section, "LevelText", "Lv.1", FntSub, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(14, 12);
            rt.offsetMax = new Vector2(0, 48);
        }
        levelText.alignment = TextAlignmentOptions.Left;
        levelText.color     = new Color(0.85f, 0.85f, 0.92f);
        SetObj(so, "_levelText", levelText);

        // 직업 (BG 하단 오버레이 — 아래 오른쪽)
        var jobText = CreateTMP(section, "JobText", "기사", FntSub, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(1,    0);
            rt.offsetMin = new Vector2(0, 12);
            rt.offsetMax = new Vector2(-14, 48);
        }
        jobText.alignment = TextAlignmentOptions.Right;
        jobText.color     = Color.white;   // BG 색상과 구분되도록 흰색
        SetObj(so, "_jobText", jobText);

        // 등급 배지
        var gradeBadge = CreateImage(section, "GradeBadge", new Color(0.55f, 0.55f, 0.55f));
        {
            var rt = gradeBadge.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.offsetMin = new Vector2(-90, -42);
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
            rt.offsetMin = new Vector2(-90, -42);
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
    //  성장 행 (PortraitH ~ PortraitH+GrowthRowH)
    //  EXP바 + 레벨업 버튼 + 용병 버튼
    // ============================================================

    static GameObject BuildGrowthRow(GameObject root, SerializedObject so)
    {
        const float BtnH   = 58f;   // 48 → 58 (+10)
        const float BtnPad = 8f;

        var row = CreatePanel(root, "GrowthRow", GrowthBgColor);
        {
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(PortraitH + GrowthRowH));
            rt.offsetMax = new Vector2(0, -PortraitH);
        }

        // 상단 구분선
        var topDiv = CreateImage(row, "TopDivider", DividerColor);
        {
            var rt = topDiv.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -1);
            rt.offsetMax = new Vector2(0,  0);
        }

        // 하단 구분선
        var botDiv = CreateImage(row, "BotDivider", DividerColor);
        {
            var rt = botDiv.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 1);
        }

        // ── 레벨업 버튼 (왼쪽 절반) ──
        // 위치: 하단에서 8~56px (BtnPad ~ BtnPad+BtnH)
        var lvBtnGo = CreatePanel(row, "LevelUpButton", LevelUpBtnColor);
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

        // ── 용병 수 증가 버튼 (오른쪽 절반) ──
        var soldBtnGo = CreatePanel(row, "SoldierUpButton", SoldierUpBtnColor);
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

        // ── EXP 바 (버튼 바로 위) ──
        // BtnPad(8) + BtnH(58) + gap(8) = 74, height=10 → 74~84
        var expBarBg = CreatePanel(row, "ExpBarBg", new Color(0.08f, 0.08f, 0.14f));
        {
            var rt = expBarBg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(BtnPad, 74f);
            rt.offsetMax = new Vector2(-BtnPad, 84f);
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

        // ── EXP 텍스트 (EXP 바 위) ──
        // expBarTop(84) + gap(4) = 88, height=26 → 88~114
        var expText = CreateTMP(row, "ExpText", "0 / 100 EXP", FntSub, FontStyles.Normal);
        {
            var rt = expText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(BtnPad, 88f);
            rt.offsetMax = new Vector2(-BtnPad, 114f);
        }
        expText.alignment = TextAlignmentOptions.Center;
        expText.color     = LabelColor;
        SetObj(so, "_expText", expText);

        return row;
    }

    // ============================================================
    //  탭 섹션 (TabBar + TabContentArea)
    // ============================================================

    static void BuildTabSection(GameObject root, SerializedObject so)
    {
        float tabTop = PortraitH + GrowthRowH + GrowthTabGap;  // 280 + 122 + 4 = 406

        // 탭 바
        var tabBar = CreatePanel(root, "TabBar", TabBarColor);
        {
            var rt = tabBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(tabTop + TabBarH));
            rt.offsetMax = new Vector2(0, -tabTop);
        }

        string[] tabLabels = { "스탯", "장비", "스킬" };
        var tabButtons = new Button[3];
        for (int i = 0; i < 3; i++)
            tabButtons[i] = BuildTabButton(tabBar, tabLabels[i], i, i == 0);

        // 탭 콘텐츠 영역 (TabBar 아래 ~ 하단)
        var contentArea = CreatePanel(root, "TabContentArea", SectionColor);
        {
            var rt = contentArea.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 0);
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

    // ── 탭 버튼 (FontLg) ───────────────────────────────────────

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

        // 탭 텍스트 — FontLg(54) 사용
        var tmp = CreateTMP(go, "Label", label, FntHero,
            isActive ? FontStyles.Bold : FontStyles.Normal);
        {
            var trt = tmp.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(0, 4);
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
            brt.offsetMax = new Vector2(0, 4);
            if (!isActive) bar.gameObject.SetActive(false);
        }

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        return btn;
    }

    // ============================================================
    //  StatPanel — 스탯 목록만 (성장 버튼은 GrowthRow로 이동)
    // ============================================================

    static GameObject BuildStatPanel(GameObject contentArea, SerializedObject so)
    {
        var panel = CreatePanel(contentArea, "StatPanel", SectionColor);
        Stretch(panel);

        // 스탯 행 컨테이너 — 패널 전체 채움
        var listContainer = new GameObject("StatListContainer", typeof(RectTransform));
        listContainer.transform.SetParent(panel.transform, false);
        {
            var rt = listContainer.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        var vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.padding                = new RectOffset(0, 0, 4, 4);
        vlg.spacing                = 0;

        SetObj(so, "_hpText",           ColorStat(BuildStatRow(listContainer, "HP",   "체력"),    StatColors.Hp));
        SetObj(so, "_atkText",          ColorStat(BuildStatRow(listContainer, "ATK",  "공격"),    StatColors.Atk));
        SetObj(so, "_defText",          ColorStat(BuildStatRow(listContainer, "DEF",  "방어율"),  StatColors.Def));
        SetObj(so, "_spdText",          BuildStatRow(listContainer, "SPD",  "이속"));
        SetObj(so, "_atkSpdText",       BuildStatRow(listContainer, "ASPD", "공속"));
        SetObj(so, "_rangeText",        BuildStatRow(listContainer, "RNG",  "사거리"));
        SetObj(so, "_soldierCountText", ColorStat(BuildStatRow(listContainer, "SOLD", "용병수"), StatColors.Soldier));
        SetObj(so, "_cmdPwrText",       BuildStatRow(listContainer, "CMD",  "지휘력"));
        SetObj(so, "_statListContainer", listContainer.transform);

        return panel;
    }

    // 스탯 행 — height 64px
    static TextMeshProUGUI BuildStatRow(GameObject parent, string id, string label)
    {
        var row = new GameObject($"Stat_{id}", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent.transform, false);
        row.GetComponent<Image>().color = Color.clear;
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(PopupW, 64f);

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 64f;
        le.minHeight       = 56f;

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
    //  EquipPanel
    // ============================================================

    static GameObject BuildEquipPanel(GameObject contentArea, SerializedObject so)
    {
        var panel = CreatePanel(contentArea, "EquipPanel", SectionColor);
        Stretch(panel);

        var (btn0, name0, bar0, icon0, stat0, lock0, enh0, enhCost0, enhIcon0) =
            BuildEquipSlot(panel, "EquipSlot0", 10f, 165f);
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
            BuildEquipSlot(panel, "EquipSlot1", 187f, 165f);
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

        float iconSize = Mathf.Min(slotH - 14f, 106f);
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
    //  SkillPanel
    // ============================================================

    static GameObject BuildSkillPanel(GameObject contentArea, SerializedObject so)
    {
        const float abTop  = 12f;
        const float iconSz = 88f;
        const float abH    = iconSz + 24f;   // 아이콘 크기 기반

        var panel = CreatePanel(contentArea, "SkillPanel", SectionColor);
        Stretch(panel);

        // ── 액티브 스킬 영역 (라벨 없음, 아이콘 + 이름 + 설명) ──
        var activeBg = CreateImage(panel, "ActiveBg", new Color(0.10f, 0.11f, 0.20f));
        {
            var rt = activeBg.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8,  -(abTop + abH));
            rt.offsetMax = new Vector2(-8, -abTop);
        }

        // 좌측 강조 바
        var activeAccent = CreateImage(panel, "ActiveAccentBar", new Color(0.45f, 0.65f, 1.00f));
        {
            var rt = activeAccent.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(8, -(abTop + abH));
            rt.offsetMax = new Vector2(12, -abTop);
        }

        // 아이콘 배경
        var iconBg = CreateImage(panel, "ActiveSkillIconBg", new Color(0.07f, 0.08f, 0.15f));
        {
            var rt = iconBg.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(18,         -(abTop + iconSz + 8f));
            rt.offsetMax = new Vector2(18 + iconSz, -abTop - 8f);
        }

        var iconImg = CreateImage(panel, "ActiveSkillIcon", new Color(0.28f, 0.32f, 0.52f));
        {
            float pad = 4f;
            var rt = iconImg.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(18 + pad,         -(abTop + iconSz + 8f - pad));
            rt.offsetMax = new Vector2(18 + iconSz - pad, -abTop - 8f - pad);
        }
        iconImg.preserveAspect = true;
        SetObj(so, "_activeSkillIcon", iconImg);

        float textLeft = 18f + iconSz + 12f;
        var skillText = CreateTMP(panel, "ActiveSkillText", "—", FntMain, FontStyles.Bold);
        {
            var rt = skillText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(textLeft, -(abTop + 54f));
            rt.offsetMax = new Vector2(-16, -(abTop + 12f));
        }
        skillText.alignment = TextAlignmentOptions.Left;
        skillText.color     = new Color(1.00f, 0.95f, 0.75f);
        SetObj(so, "_activeSkillText", skillText);

        var descText = CreateTMP(panel, "ActiveSkillDescText", "", FntSub, FontStyles.Normal);
        {
            var rt = descText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(textLeft, -(abTop + abH - 6f));
            rt.offsetMax = new Vector2(-16, -(abTop + 58f));
        }
        descText.alignment        = TextAlignmentOptions.TopLeft;
        descText.color            = LabelColor;
        descText.textWrappingMode = TextWrappingModes.Normal;
        SetObj(so, "_activeSkillDescText", descText);

        // ── 구분선 ──────────────────────────────────────────────
        const float divY   = abTop + abH + 10f;
        var divider = CreateImage(panel, "SkillDivider", new Color(0.25f, 0.25f, 0.35f));
        {
            var rt = divider.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8,  -(divY + 2f));
            rt.offsetMax = new Vector2(-8, -divY);
        }

        // ── 패시브 섹션 (라벨 없음, 바로 박스 나열) ──────────────
        const float pBoxGap = 10f;
        float pFirst = divY + 14f;

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
        // 박스 루트 (HLG: 좌측 강조 바 + 내용)
        var box = new GameObject($"{id}Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(container.transform, false);
        box.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.20f);
        var boxHlg = box.AddComponent<HorizontalLayoutGroup>();
        boxHlg.padding              = new RectOffset(0, 12, 10, 12);
        boxHlg.spacing              = 10f;
        boxHlg.childForceExpandWidth  = false;
        boxHlg.childForceExpandHeight = false;
        boxHlg.childControlWidth      = true;
        boxHlg.childControlHeight     = true;
        boxHlg.childAlignment         = TextAnchor.UpperLeft;
        box.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // 좌측 강조 바
        var accentBar = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
        accentBar.transform.SetParent(box.transform, false);
        accentBar.GetComponent<Image>().color = new Color(0.55f, 0.40f, 0.85f);
        var abLe = accentBar.AddComponent<LayoutElement>();
        abLe.minWidth       = 4f;
        abLe.preferredWidth = 4f;
        abLe.flexibleHeight = 1f;

        // 텍스트 컨테이너 (VLG)
        var textCont = new GameObject("TextCont", typeof(RectTransform));
        textCont.transform.SetParent(box.transform, false);
        var tcLe = textCont.AddComponent<LayoutElement>();
        tcLe.flexibleWidth = 1f;
        var tcVlg = textCont.AddComponent<VerticalLayoutGroup>();
        tcVlg.spacing              = 4f;
        tcVlg.childForceExpandWidth  = true;
        tcVlg.childForceExpandHeight = false;
        tcVlg.childControlWidth      = true;
        tcVlg.childControlHeight     = true;
        textCont.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        var nameGo = new GameObject($"{id}Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(textCont.transform, false);
        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text             = "—";
        nameTmp.fontSize         = FntMain;
        nameTmp.fontStyle        = FontStyles.Bold;
        nameTmp.alignment        = TextAlignmentOptions.Left;
        nameTmp.color            = new Color(0.90f, 0.85f, 1.00f);
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameGo.AddComponent<LayoutElement>().preferredHeight = FntMain + 8f;

        var descGo = new GameObject($"{id}Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGo.transform.SetParent(textCont.transform, false);
        var descTmp = descGo.GetComponent<TextMeshProUGUI>();
        descTmp.text             = "";
        descTmp.fontSize         = FntSub;
        descTmp.fontStyle        = FontStyles.Normal;
        descTmp.alignment        = TextAlignmentOptions.TopLeft;
        descTmp.color            = new Color(0.65f, 0.63f, 0.80f);
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode     = TextOverflowModes.Overflow;
        descGo.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        return (nameTmp, descTmp);
    }

    static TextMeshProUGUI ColorStat(TextMeshProUGUI tmp, Color color)
    {
        tmp.color = color;
        return tmp;
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
