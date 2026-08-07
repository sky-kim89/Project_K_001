#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DisassemblePopupCreator.cs
//  Tools > Project K > Popup > Create Disassemble Popup Prefab
//
//  저장: Assets/_project/2.Prefabs/UI/DisassemblePopup.prefab
//
//  레이아웃:
//    Header    (80px top)   — "장비 분해" 제목 + X 닫기
//    InfoPanel (300px left) — 선택 정보 + 분해 확인 버튼(보상 포함)
//    Divider   (2px)
//    GridPanel (나머지, Mask 적용)
//      ├── BulkHeader (70px) — 등급 Toggle 5개 + 일괄 분해 버튼
//      └── ScrollView        — 114×114 아이콘 그리드
// ============================================================

public static class DisassemblePopupCreator
{
    const string PrefabPath  = "Assets/_project/2.Prefabs/UI/DisassemblePopup.prefab";
    const string StoneIconPath = "Assets/_project/3.Textures/Icons/Items/item_equip_upgrade_stone.png";

    const float PopupW      = 960f;
    const float PopupH      = 860f;
    static readonly float HeaderH = UIScale.BtnFor(UIScale.FontLg);   // 80 → 96
    const float InfoW       = 300f;
    static readonly float BulkHeaderH = UIScale.BtnFor(UIScale.FontMd);   // 70 → 72
    const float CellSize    = 114f;
    const float CellGap     = 8f;
    const int   GridColumns = 5;

    static readonly Color BgColor      = new Color(0.06f, 0.06f, 0.12f, 0.97f);
    static readonly Color HeaderColor  = new Color(0.09f, 0.09f, 0.17f, 1f);
    static readonly Color InfoBg       = new Color(0.08f, 0.08f, 0.15f, 1f);
    static readonly Color GridBg       = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color BulkHeaderBg = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color DividerColor = new Color(0.20f, 0.20f, 0.30f, 1f);

    static readonly Color DisBtnColor  = new Color(0.55f, 0.18f, 0.12f, 1f);

    static readonly Color MutedText    = new Color(0.55f, 0.55f, 0.60f);

    // 등급 색상·레이블 (GradeStyle 미사용 — editor 전용)
    static readonly Color[] GradeColors =
    {
        new Color(0.55f, 0.55f, 0.55f),  // Normal
        new Color(0.25f, 0.80f, 0.35f),  // Uncommon
        new Color(0.20f, 0.50f, 1.00f),  // Rare
        new Color(0.70f, 0.30f, 1.00f),  // Unique
        new Color(1.00f, 0.60f, 0.10f),  // Epic
    };
    static readonly string[] GradeLabels = { "일반", "언커먼", "희귀", "고유", "영웅" };

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Popup + "Disassemble", priority = ProjectKMenu.PrefabPrio + 37)]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI");
        AssetDatabase.Refresh();

        var go = BuildPopup();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log("[DisassemblePopupCreator] DisassemblePopup 프리팹 생성 완료");
    }

    // ── 팝업 루트 ─────────────────────────────────────────────

    static GameObject BuildPopup()
    {
        var root = MakePanel(null, "DisassemblePopup", BgColor);
        root.AddComponent<CanvasGroup>();
        {
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(PopupW, PopupH);
        }

        var popup = root.AddComponent<DisassemblePopup>();
        var so    = new SerializedObject(popup);
        var typeProp = so.FindProperty("_popupType");
        if (typeProp != null) typeProp.intValue = (int)PopupType.Disassemble;

        BuildHeader(root, so);
        BuildContent(root, so);

        so.ApplyModifiedProperties();
        return root;
    }

    // ── 헤더 ─────────────────────────────────────────────────

    static void BuildHeader(GameObject root, SerializedObject so)
    {
        var header = MakePanel(root, "Header", HeaderColor);
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0, HeaderH);
        }

        var title = MakeTMP(header, "TitleText", "장비 분해", UIScale.FontLg, FontStyles.Bold);
        FullStretch(title.transform as RectTransform);
        title.alignment = TextAlignmentOptions.Center;

        var closeGo = MakeButton(header, "CloseBtn", "", new Color(0.55f, 0.15f, 0.15f), UIScale.FontMd);
        EditorUIBuilder.XMark(closeGo, "Mark", UIScale.FontMd, Color.white);
        {
            var rt = closeGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1, 0.5f);
            rt.anchorMax        = new Vector2(1, 0.5f);
            rt.pivot            = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-16, 0);
            rt.sizeDelta        = new Vector2(72, 72);
        }
        SetObj(so, "_closeBtn", closeGo.GetComponent<Button>());
    }

    // ── 콘텐츠 영역 ──────────────────────────────────────────

    static void BuildContent(GameObject root, SerializedObject so)
    {
        // 좌측 InfoPanel
        var infoPanel = MakePanel(root, "InfoPanel", InfoBg);
        {
            var rt = infoPanel.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 0);
            rt.anchorMax        = new Vector2(0, 1);
            rt.pivot            = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(0, -HeaderH / 2f);
            rt.sizeDelta        = new Vector2(InfoW, -HeaderH);
        }
        BuildInfoPanel(infoPanel, so);

        // 구분선
        var divider = MakePanel(root, "Divider", DividerColor);
        {
            var rt = divider.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 0);
            rt.anchorMax        = new Vector2(0, 1);
            rt.pivot            = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(InfoW, -HeaderH / 2f);
            rt.sizeDelta        = new Vector2(2, -HeaderH);
        }

        // 우측 GridPanel — Mask 직접 적용
        var gridPanel = MakePanel(root, "GridPanel", GridBg);
        {
            var rt = gridPanel.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 0);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(InfoW + 2, -HeaderH / 2f);
            rt.sizeDelta        = new Vector2(-(InfoW + 2), -HeaderH);
        }
        var mask = gridPanel.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        BuildBulkHeader(gridPanel, so);
        BuildGridScroll(gridPanel, so);
    }

    // ── InfoPanel ─────────────────────────────────────────────

    static void BuildInfoPanel(GameObject parent, SerializedObject so)
    {
        const float padX   = 20f;
        const float iconSz = 120f;
        float curY = -20f;

        // 아이콘 배경 (등급 테두리 색상으로 활용)
        var iconBg = MakePanel(parent, "IconBg", new Color(0.25f, 0.25f, 0.35f));
        {
            var rt = iconBg.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, curY);
            rt.sizeDelta        = new Vector2(iconSz, iconSz);
        }
        SetObj(so, "_selectedGradeBorder", iconBg.GetComponent<Image>());

        // 실제 아이콘 이미지 (안쪽 여백 4px)
        var iconImg = MakePanel(iconBg, "IconImage", new Color(0.2f, 0.2f, 0.3f));
        {
            var rt = iconImg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4, 4);
            rt.offsetMax = new Vector2(-4, -4);
        }
        iconImg.GetComponent<Image>().raycastTarget = false;
        SetObj(so, "_selectedIcon", iconImg.GetComponent<Image>());
        curY -= iconSz + 14f;

        // 이름
        var nameGo = MakeTMP(parent, "SelectedName", "장비를 선택하세요", UIScale.FontMd, FontStyles.Bold);
        {
            var rt = nameGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, curY);
            rt.sizeDelta        = new Vector2(-padX * 2, 44);
        }
        nameGo.alignment = TextAlignmentOptions.Center;
        SetObj(so, "_selectedNameText", (Object)nameGo);
        curY -= 44f + 6f;

        // 등급
        var gradeGo = MakeTMP(parent, "SelectedGrade", "", UIScale.FontSm, FontStyles.Normal);
        {
            var rt = gradeGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, curY);
            rt.sizeDelta        = new Vector2(-padX * 2, 34);
        }
        gradeGo.alignment = TextAlignmentOptions.Center;
        SetObj(so, "_selectedGradeText", (Object)gradeGo);
        curY -= 34f + 14f;

        // 구분선
        var div = MakePanel(parent, "Divider", DividerColor);
        {
            var rt = div.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, curY);
            rt.sizeDelta        = new Vector2(-padX * 2, 1);
        }
        curY -= 1f + 10f;

        // 스탯 목록
        var statsGo = MakeTMP(parent, "SelectedStats", "", UIScale.FontSm, FontStyles.Normal);
        {
            var rt = statsGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, curY);
            rt.sizeDelta        = new Vector2(-padX * 2, 210);
        }
        statsGo.alignment   = TextAlignmentOptions.TopLeft;
        statsGo.color       = MutedText;
        statsGo.lineSpacing = 8f;
        SetObj(so, "_selectedStatsText", (Object)statsGo);

        // 분해 확인 버튼 (하단 고정) — 보상 아이콘 + "+N" + "분해 확인" 텍스트
        var disBtn = new GameObject("DisassembleBtn",
            typeof(RectTransform), typeof(Image), typeof(Button));
        disBtn.transform.SetParent(parent.transform, false);
        disBtn.GetComponent<Image>().color = DisBtnColor;
        {
            var rt = disBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 0);
            rt.anchorMax        = new Vector2(1, 0);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 24f);
            rt.sizeDelta        = new Vector2(-padX * 2, UIScale.BtnMd);
        }
        disBtn.GetComponent<Button>().interactable = false;
        BuildDisBtnContent(disBtn, so);
        SetObj(so, "_disassembleBtn", disBtn.GetComponent<Button>());
    }

    // 분해 버튼 내부: 보상 아이콘+"+N"(위, 중앙) + "분해 확인"(아래)
    // LayoutGroup 미사용 — sizeDelta 100×100 기본값 버그 방지, 앵커 절대 위치 사용
    static void BuildDisBtnContent(GameObject btn, SerializedObject so)
    {
        // 보상 행 컨테이너 (HLG, 버튼 상단 중앙 고정)
        // 아이콘 32 + 간격 6 + 텍스트 66 = 104px → sizeDelta.x = 104
        var rowGo = new GameObject("RewardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(btn.transform, false);
        {
            var rt = rowGo.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 20f);
            rt.sizeDelta        = new Vector2(104f, 34f);
        }
        var rHlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        rHlg.childAlignment        = TextAnchor.MiddleCenter;
        rHlg.spacing               = 6f;
        rHlg.childForceExpandWidth  = false;
        rHlg.childForceExpandHeight = false;

        // 강화석 아이콘 (32×32, sizeDelta 명시)
        var iconGo = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(rowGo.transform, false);
        iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
        var iconLe = iconGo.AddComponent<LayoutElement>();
        iconLe.minWidth      = iconLe.preferredWidth  = 32f;
        iconLe.minHeight     = iconLe.preferredHeight = 32f;
        iconLe.flexibleWidth = iconLe.flexibleHeight  = 0f;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var stoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StoneIconPath);
        if (stoneSprite != null) iconImg.sprite = stoneSprite;
        else                     iconImg.color  = new Color(0.85f, 0.65f, 0.20f);
        SetObj(so, "_rewardIcon", iconImg);

        // "+N" 텍스트 (66×34, sizeDelta 명시)
        var textGo = new GameObject("RewardText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rowGo.transform, false);
        textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(66f, 34f);
        var textLe = textGo.AddComponent<LayoutElement>();
        textLe.minWidth      = 40f;
        textLe.preferredWidth = 66f;
        textLe.flexibleWidth  = 0f;
        var rewardTmp = textGo.GetComponent<TextMeshProUGUI>();
        rewardTmp.text          = "+0";
        rewardTmp.fontSize      = UIScale.FontMd;
        rewardTmp.alignment     = TextAlignmentOptions.Left;
        rewardTmp.color         = new Color(1f, 0.85f, 0.20f);
        rewardTmp.raycastTarget = false;
        SetObj(so, "_rewardText", rewardTmp);

        // "분해 확인" 라벨 (버튼 하단 42%)
        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(btn.transform, false);
        {
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.42f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text          = "분해 확인";
        labelTmp.fontSize      = UIScale.FontMd;
        labelTmp.alignment     = TextAlignmentOptions.Center;
        labelTmp.color         = Color.white;
        labelTmp.raycastTarget = false;
    }

    // ── BulkHeader ────────────────────────────────────────────

    static void BuildBulkHeader(GameObject gridPanel, SerializedObject so)
    {
        var header = MakePanel(gridPanel, "BulkHeader", BulkHeaderBg);
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0, BulkHeaderH);
        }

        var hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.spacing               = 6f;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.padding               = new RectOffset(8, 8, 6, 6);

        // 5개 등급 토글
        var gradeToggleComponents = new Toggle[5];
        for (int i = 0; i < 5; i++)
        {
            var tGo = BuildGradeToggle(header, (UnitGrade)i);
            gradeToggleComponents[i] = tGo.GetComponent<Toggle>();
        }

        // 구분선 (토글 그룹 / 일괄 분해 버튼 시각 분리)
        // sizeDelta 명시 필수 — 미설정 시 기본 100×100으로 prefab 저장되어 100px 블록으로 렌더됨
        var sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sep.transform.SetParent(header.transform, false);
        sep.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0f);
        sep.GetComponent<Image>().color = new Color(0.28f, 0.28f, 0.42f);
        var sepLe = sep.AddComponent<LayoutElement>();
        sepLe.minWidth      = sepLe.preferredWidth = 2f;
        sepLe.flexibleWidth = 0f;

        // 일괄 분해 버튼 (더 밝은 색 + Bold 텍스트 — 버튼임을 명확히)
        var bulkBtn = new GameObject("BulkDisassembleBtn",
            typeof(RectTransform), typeof(Image), typeof(Button));
        bulkBtn.transform.SetParent(header.transform, false);
        bulkBtn.GetComponent<Image>().color = new Color(0.70f, 0.28f, 0.08f, 1f);
        var bulkLe = bulkBtn.AddComponent<LayoutElement>();
        bulkLe.minWidth       = 170f;
        bulkLe.preferredWidth = 170f;
        bulkLe.flexibleWidth  = 0f;

        var bulkLblGo = MakeTMP(bulkBtn, "Label", "일괄 분해", UIScale.FontSm, FontStyles.Bold);
        FullStretch(bulkLblGo.GetComponent<RectTransform>());
        bulkLblGo.alignment     = TextAlignmentOptions.Center;
        bulkLblGo.color         = new Color(1f, 0.92f, 0.78f);
        bulkLblGo.raycastTarget = false;

        var bulkBtnComp = bulkBtn.GetComponent<Button>();
        bulkBtnComp.interactable = false;
        ColorBlock bcb = bulkBtnComp.colors;
        bcb.disabledColor    = new Color(0.38f, 0.16f, 0.06f, 0.75f);
        bcb.highlightedColor = new Color(0.85f, 0.36f, 0.12f, 1f);
        bulkBtnComp.colors = bcb;

        // 직렬화 연결
        var togglesProp = so.FindProperty("_gradeToggles");
        if (togglesProp != null)
        {
            togglesProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
                togglesProp.GetArrayElementAtIndex(i).objectReferenceValue = gradeToggleComponents[i];
        }
        SetObj(so, "_bulkDisassembleBtn", bulkBtnComp);
    }

    static GameObject BuildGradeToggle(GameObject parent, UnitGrade grade)
    {
        int    idx = (int)grade;
        Color  col = GradeColors[idx];
        string lbl = GradeLabels[idx];

        // 배경 (Toggle의 targetGraphic)
        var go = new GameObject($"Toggle_{grade}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var bgImg = go.GetComponent<Image>();
        bgImg.color = new Color(0.11f, 0.11f, 0.19f);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1f;
        le.flexibleHeight = 1f;
        le.minWidth       = 68f;

        // 등급 색상 바 (하단 3px — 항상 표시)
        var bar = new GameObject("ColorBar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(go.transform, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin        = new Vector2(0f, 0f);
        barRt.anchorMax        = new Vector2(1f, 0f);
        barRt.pivot            = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta        = new Vector2(0f, 3f);
        bar.GetComponent<Image>().color        = col;
        bar.GetComponent<Image>().raycastTarget = false;

        // 체크마크 오버레이 (isOn=true 시 graphic으로 표시)
        var ckGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        ckGo.transform.SetParent(go.transform, false);
        FullStretch(ckGo.GetComponent<RectTransform>());
        var ckImg = ckGo.GetComponent<Image>();
        ckImg.color        = new Color(col.r, col.g, col.b, 0.42f);
        ckImg.raycastTarget = false;

        // 등급명 텍스트
        var lGo = MakeTMP(go, "Label", lbl, UIScale.FontSm, FontStyles.Normal);
        FullStretch(lGo.GetComponent<RectTransform>());
        lGo.alignment     = TextAlignmentOptions.Center;
        lGo.color         = new Color(0.82f, 0.82f, 0.88f);
        lGo.raycastTarget = false;

        // Toggle 컴포넌트
        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic       = ckImg;
        toggle.isOn          = false;

        ColorBlock cb = toggle.colors;
        cb.normalColor      = new Color(0.11f, 0.11f, 0.19f);
        cb.highlightedColor = new Color(0.18f, 0.18f, 0.28f);
        cb.pressedColor     = new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f);
        cb.selectedColor    = Color.white;
        toggle.colors = cb;

        return go;
    }

    // ── GridScroll ────────────────────────────────────────────

    static void BuildGridScroll(GameObject gridPanel, SerializedObject so)
    {
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(gridPanel.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // 상단: BulkHeaderH + 8px 여백 / 나머지: 8px 여백
            rt.offsetMin = new Vector2(8, 8);
            rt.offsetMax = new Vector2(-8, -(BulkHeaderH + 8));
        }
        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;

        // Viewport — Mask 없음 (GridPanel이 처리)
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
        viewport.transform.SetParent(scrollGo.transform, false);
        viewport.GetComponent<Image>().color = Color.clear;
        {
            var rt = viewport.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // GridContent
        var content = new GameObject("GridContent", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        {
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = Vector2.zero;
        }
        var glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(CellSize, CellSize);
        glg.spacing         = new Vector2(CellGap, CellGap);
        glg.padding         = new RectOffset(12, 12, 12, 12);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperLeft;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = GridColumns;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content  = content.GetComponent<RectTransform>();

        SetObj(so, "_gridContent", content.transform);

        // 셀 템플릿 (비활성)
        var template = BuildIconCellTemplate(content);
        template.SetActive(false);
        SetObj(so, "_iconCellTemplate", template);
    }

    // ── 아이콘 셀 템플릿 ─────────────────────────────────────

    static GameObject BuildIconCellTemplate(GameObject parent)
    {
        var cell = new GameObject("IconCellTemplate",
            typeof(RectTransform), typeof(Image), typeof(Button));
        cell.transform.SetParent(parent.transform, false);
        cell.GetComponent<RectTransform>().sizeDelta = new Vector2(CellSize, CellSize);
        cell.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.20f);

        ColorBlock cb = cell.GetComponent<Button>().colors;
        cb.highlightedColor = new Color(0.20f, 0.30f, 0.50f, 1f);
        cb.pressedColor     = new Color(0.14f, 0.20f, 0.35f, 1f);
        cell.GetComponent<Button>().colors = cb;

        // 등급 테두리 (GradeBorder — 아이콘보다 먼저 렌더, 선택 표시로도 활용)
        var border = new GameObject("GradeBorder", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(cell.transform, false);
        FullStretch(border.GetComponent<RectTransform>());
        border.GetComponent<Image>().color        = new Color(0.28f, 0.28f, 0.46f);
        border.GetComponent<Image>().raycastTarget = false;

        // 아이콘 이미지 (5px 인셋 — 선택 시 GradeBorder 색상 변경으로 테두리 효과)
        var icon = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(cell.transform, false);
        {
            var rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(5, 5);
            rt.offsetMax = new Vector2(-5, -5);
        }
        icon.GetComponent<Image>().color        = new Color(0.18f, 0.18f, 0.28f);
        icon.GetComponent<Image>().raycastTarget = false;

        // SelectionOutline (항상 비활성 — 선택 표시는 GradeBorder로 처리)
        var outlineGo = new GameObject("SelectionOutline", typeof(RectTransform), typeof(Image));
        outlineGo.transform.SetParent(cell.transform, false);
        FullStretch(outlineGo.GetComponent<RectTransform>());
        outlineGo.GetComponent<Image>().color        = Color.clear;
        outlineGo.GetComponent<Image>().raycastTarget = false;
        outlineGo.SetActive(false);

        return cell;
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

    static GameObject MakePanel(GameObject parent, string name, Color color)
        => EditorUIBuilder.Panel(parent, name, color);

    // alignment 미지정(TMP 기본 TopLeft)이 이 팝업 레이아웃의 전제라 center:false.
    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text,
        float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style, center: false);

    static GameObject MakeButton(GameObject parent, string name, string label,
        Color bgColor, float fontSize)
        => EditorUIBuilder.Btn(parent, name, label, bgColor, fontSize);

    static void FullStretch(RectTransform rt) => EditorUIBuilder.Stretch(rt);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "DisassemblePopupCreator");
}
#endif
