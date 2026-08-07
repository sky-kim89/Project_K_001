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
//  Tools > Project K > 프리팹 생성 > 로비 > HeroPanel
//
//  생성 에셋:
//    Assets/_project/2.Prefabs/UI/Lobby/HeroPanel.prefab
//    Assets/_project/2.Prefabs/UI/Lobby/HeroCard.prefab
//    Assets/_project/2.Prefabs/UI/Lobby/EquipCard.prefab
//
//  BuildCardPrefab() 은 BattlePanel · MercenaryShop · RunShop 이
//  공유하는 장수 카드 팩토리다 (public 유지 필수).
//
//  EquipComparePopup / DisassemblePopup 은 여기서 만들지 않는다.
//    각각 EquipComparePopupCreator / DisassemblePopupCreator 가 정본.
//    예전에는 이 파일에도 사본이 있어 같은 경로를 덮어썼고,
//    DisassemblePopup 쪽 사본은 이미 사라진 필드(_heroRowTemplate 등)를
//    연결하고 있어 실행하면 빈 프리팹이 나왔다.
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
    const string PanelPrefabPath     = "Assets/_project/2.Prefabs/UI/Lobby/HeroPanel.prefab";
    const string CardPrefabPath      = "Assets/_project/2.Prefabs/UI/Lobby/HeroCard.prefab";
    const string EquipCardPrefabPath = "Assets/_project/2.Prefabs/UI/Lobby/EquipCard.prefab";

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
    static readonly int FntMini = (int)UIScale.FontSm - 6;   // 24 → 28 — 배지 등 극소 텍스트

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Lobby + "HeroPanel", priority = ProjectKMenu.PrefabPrio + 14)]
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

        // ── 용병 고용 버튼: 삭제 ─────────────────────────────
        //  용병 고용은 런 상점(RunShopPopup)에서만 한다.
        //  HeroPanelUI._hireBtn / _hireCostText / _hireCostIcon 은
        //  비워 둔다 — 전부 `?.` 로 접근하는 선택적 UI 참조다.

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

    public static GameObject BuildCardPrefab()
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

        // ── 텍스트 영역 ───────────────────────────────────────
        //  카드는 쓰이는 곳마다 폭이 다르다 (영웅 목록 356 / 배치 슬롯 550+).
        //  초상화는 절대 좌표(4~104)인데 글자까지 카드 폭 비율로 잡으면
        //  칸 하나가 초상화 몫까지 나눠 갖게 되어 좁아진다 — "체력 2,262" 가
        //  두 줄로 깨지던 원인. 텍스트는 초상화 오른쪽 영역을 따로 잡고,
        //  그 안에서만 비율로 나눈다.
        //  ※ 자식 이름으로 찾는 쪽(BattlePanel·Mercenary·RunShop Creator)은
        //    EditorUIBuilder.FindDeep 을 쓰므로 한 단계 들어가도 안전하다.
        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(card.transform, false);
        {
            var rt = textArea.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(112, 0);
            rt.offsetMax = new Vector2(-8,  0);
        }

        const float MidX = 0.5f;    // 2열 분할 지점 (텍스트 영역 기준)

        // 텍스트 영역 y 오프셋 (상단 앵커 기준, 4px 갭)
        // FntSub(34) 한 줄은 43px 을 쓴다. 카드가 170px 뿐이라 5행에 43 씩은 못 준다.
        //
        // ⚠ 칸이 한 줄보다 낮은데 overflowMode = Ellipsis/Truncate 면
        //   TMP 가 그 줄을 통째로 버린다 (잘리는 게 아니라 아예 안 그려진다).
        //   실제로 이름이 사라졌던 원인이다. 칸을 낮게 잡을 거면
        //   AutoSize 를 켜서 줄어들게 하거나 overflowMode 를 Overflow 로 둘 것.
        //
        // Row1 이름/등급:  top=4,   bot=44  (40px)
        // Row2 레벨/직업:  top=48,  bot=82  (34px)
        // Divider:          top=86,  bot=88
        // Row3 HP/공격:    top=92,  bot=128 (36px)
        // Row4 방어/용병:  top=132, bot=168 (36px)
        // DeployBadge: 초상화 우상단

        // ── 이름 (상단 기준, 40px 높이) ──────────────────────
        //  우측 InfoBadge 자리(InfoSize + 여백)를 비워 둔다.
        const float InfoSize = 30f;
        var nameText = CreateTMP(textArea, "NameText", "이름", FntSub, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0f,    1f);
            rt.anchorMax = new Vector2(0.70f, 1f);
            rt.offsetMin = new Vector2(0,                 -44);
            rt.offsetMax = new Vector2(-(InfoSize + 12f),   -4);
        }
        nameText.alignment        = TextAlignmentOptions.MidlineLeft;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode     = TextOverflowModes.Ellipsis;
        // AutoSize 없이 Ellipsis 를 쓰면 칸(40)보다 한 줄(43)이 커서 통째로 사라진다.
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin      = FntSub - 10;
        nameText.fontSizeMax      = FntSub;

        // ── 상세 보기 힌트 (i) — 등급 배지 왼쪽 ───────────────
        //  카드를 누르면 상세 정보가 뜬다는 안내 표시. 기능은 없다(장식).
        var infoBadge = CreateImage(textArea, "InfoBadge", new Color(0.32f, 0.45f, 0.66f, 0.95f));
        {
            var rt = infoBadge.rectTransform;
            rt.anchorMin        = rt.anchorMax = new Vector2(0.70f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -9f);
            rt.sizeDelta        = new Vector2(InfoSize, InfoSize);
        }
        // 동그란 정보 아이콘 — 내장 Knob 스프라이트를 원으로 쓴다.
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (knob != null) infoBadge.sprite = knob;

        var infoLabel = CreateTMP(infoBadge.gameObject, "InfoLabel", "i", InfoSize * 0.72f, FontStyles.Bold);
        {
            var rt = infoLabel.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 1);
            rt.offsetMax = Vector2.zero;
        }
        infoLabel.alignment        = TextAlignmentOptions.Center;
        infoLabel.textWrappingMode = TextWrappingModes.NoWrap;
        infoLabel.color            = Color.white;

        // ── 등급 뱃지 배경 (상단 기준, 우측, 행 내 2px 인셋) ─
        var gradeBadge = CreateImage(textArea, "GradeBadge", new Color(0.55f, 0.55f, 0.55f));
        {
            var rt = gradeBadge.rectTransform;
            rt.anchorMin = new Vector2(0.70f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(6, -42);
            rt.offsetMax = new Vector2(0,  -6);
        }

        // ── 등급 텍스트 (뱃지 위 오버레이) ───────────────────
        var gradeText = CreateTMP(textArea, "GradeText", "일반", FntSub, FontStyles.Bold);
        {
            var rt = gradeText.rectTransform;
            rt.anchorMin = new Vector2(0.70f, 1f);
            rt.anchorMax = new Vector2(1f,    1f);
            rt.offsetMin = new Vector2(6, -42);
            rt.offsetMax = new Vector2(0,  -6);
        }
        gradeText.alignment        = TextAlignmentOptions.Center;
        gradeText.textWrappingMode = TextWrappingModes.NoWrap;
        gradeText.color            = Color.white;
        gradeText.enableAutoSizing = true;
        gradeText.fontSizeMin      = FntSub - 10;
        gradeText.fontSizeMax      = FntSub;

        // ── 레벨 (상단 기준, 좌측) ────────────────────────────
        var levelText = CreateTMP(textArea, "LevelText", "Lv.1", FntSub, FontStyles.Normal);
        {
            var rt = levelText.rectTransform;
            rt.anchorMin = new Vector2(0f,   1f);
            rt.anchorMax = new Vector2(MidX, 1f);
            rt.offsetMin = new Vector2(0, -82);
            rt.offsetMax = new Vector2(0, -48);
        }
        levelText.alignment = TextAlignmentOptions.MidlineLeft;
        levelText.color     = LabelColor;

        // ── 직업 (상단 기준, 우측) ────────────────────────────
        var jobText = CreateTMP(textArea, "JobText", "기사", FntSub, FontStyles.Normal);
        {
            var rt = jobText.rectTransform;
            rt.anchorMin = new Vector2(MidX, 1f);
            rt.anchorMax = new Vector2(1f,   1f);
            rt.offsetMin = new Vector2(0, -82);
            rt.offsetMax = new Vector2(0, -48);
        }
        jobText.alignment = TextAlignmentOptions.MidlineRight;
        jobText.color     = LabelColor;

        // ── 구분선 (상단 기준) ────────────────────────────────
        var statDiv = CreateImage(textArea, "StatDivider", DividerColor);
        {
            var rt = statDiv.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0, -88);
            rt.offsetMax = new Vector2(0, -86);
        }

        // ── 스탯 2×2 (텍스트 영역을 반씩 나눠 씀) ────────────
        var hpText   = BuildCardStat(textArea, "Hp",      "체력", StatColors.Hp,
                                     0f,   MidX, -92f,  -128f);
        var atkText  = BuildCardStat(textArea, "Atk",     "공격", StatColors.Atk,
                                     MidX, 1f,   -92f,  -128f);
        var defText  = BuildCardStat(textArea, "Def",     "방어", StatColors.Def,
                                     0f,   MidX, -132f, -168f);
        var soldText = BuildCardStat(textArea, "Soldier", "용병", StatColors.Soldier,
                                     MidX, 1f,   -132f, -168f);

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

        var deployText = CreateTMP(deployBadge, "DeployText", "1", (int)UIScale.FontMd, FontStyles.Bold);
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

    // ── HeroCard 스탯 칸 ─────────────────────────────────────
    //  [x0..x1] 구간(텍스트 영역 기준 비율)을 레이블 38% / 값 62% 로 나눈다.
    //  둘 다 NoWrap 이고 값은 AutoSize — 자릿수가 늘어나도 두 줄로 깨지거나
    //  잘리지 않고 폰트만 작아진다 (공격 4자리, 체력 6자리 대응).
    static TextMeshProUGUI BuildCardStat(GameObject textArea, string id, string label,
                                         Color valueColor, float x0, float x1,
                                         float top, float bottom)
    {
        const float LabelRatio = 0.38f;
        float split = x0 + (x1 - x0) * LabelRatio;

        var lbl = CreateTMP(textArea, $"{id}Label", label, FntSub, FontStyles.Normal);
        {
            var rt = lbl.rectTransform;
            rt.anchorMin = new Vector2(x0,    1f);
            rt.anchorMax = new Vector2(split, 1f);
            rt.offsetMin = new Vector2(0, bottom);
            rt.offsetMax = new Vector2(0, top);
        }
        lbl.alignment         = TextAlignmentOptions.MidlineLeft;
        lbl.color             = LabelColor;
        lbl.textWrappingMode  = TextWrappingModes.NoWrap;
        // Ellipsis 는 칸보다 한 줄이 높으면 줄을 통째로 버린다 → Overflow + AutoSize.
        // 값과 같은 규칙이라 레이블·값의 글자 크기가 어긋나지 않는다.
        lbl.overflowMode      = TextOverflowModes.Overflow;
        lbl.enableAutoSizing  = true;   // 좁은 카드(영웅 목록 356px)에서는 줄여서라도 다 보인다
        lbl.fontSizeMin       = FntSub - 10;
        lbl.fontSizeMax       = FntSub;

        var val = CreateTMP(textArea, $"{id}Text", "—", FntSub, FontStyles.Bold);
        {
            var rt = val.rectTransform;
            rt.anchorMin = new Vector2(split, 1f);
            rt.anchorMax = new Vector2(x1,    1f);
            rt.offsetMin = new Vector2(0,  bottom);
            rt.offsetMax = new Vector2(-6, top);
        }
        val.alignment        = TextAlignmentOptions.MidlineRight;
        val.color            = valueColor;
        val.textWrappingMode = TextWrappingModes.NoWrap;
        val.overflowMode     = TextOverflowModes.Overflow;
        val.enableAutoSizing = true;
        val.fontSizeMin      = FntSub - 10;
        val.fontSizeMax      = FntSub;

        return val;
    }

    // ── 버튼 비용 행 헬퍼 ────────────────────────────────────

    static void BuildCostHlg(GameObject go) => EditorUIBuilder.CostHlg(go);

    static void AddIconLE(Image img, float size) => EditorUIBuilder.IconLE(img, size);

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
        => EditorUIBuilder.Panel(parent, name, color);

    static Image CreateImage(GameObject parent, string name, Color color)
        => EditorUIBuilder.Img(parent, name, color);

    static TextMeshProUGUI CreateTMP(GameObject parent, string name,
                                     string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void Stretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        => EditorUIBuilder.Center(rt, pos, size);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "HeroPanelCreator");

    static void SetObjArray(SerializedObject so, string field, Object[] objs)
        => EditorUIBuilder.SetObjArray(so, field, objs, "HeroPanelCreator");
}
#endif
