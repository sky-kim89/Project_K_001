#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EquipComparePopupCreator.cs
//  Tools > Project K > Popup > Create EquipCompare Popup Prefab
//
//  생성 에셋:
//    Assets/_project/2.Prefabs/UI/EquipComparePopup.prefab
//
//  레이아웃 (위 → 아래):
//    Header     (HeaderH px) — 제목 + 우측 상단 X 닫기 버튼
//    Cards      (CardH  px)  — 현재 장착(좌) / 선택 장비(우)
//    Warning    (WarnH  px)  — 경고 텍스트
//    EquipBtn   (BtnH   px)  — 장착 버튼
//    Divider    (1 px)
//    Inventory  (나머지)     — 인벤토리 스크롤 목록
// ============================================================

public static class EquipComparePopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/EquipComparePopup.prefab";

    const float PopupW   = 720f;
    const float PopupH   = 900f;
    const float HeaderH  = 64f;
    const float CardH    = 270f;
    const float WarnH    = 44f;
    const float BtnH     = 64f;
    const float DividerH = 1f;

    static readonly Color BgColor       = new Color(0.06f, 0.06f, 0.12f, 0.97f);
    static readonly Color HeaderColor   = new Color(0.09f, 0.09f, 0.17f, 1f);
    static readonly Color CardColor     = new Color(0.10f, 0.10f, 0.18f, 1f);
    static readonly Color SlotEmptyCol  = new Color(0.12f, 0.12f, 0.20f, 1f);
    static readonly Color DividerColor  = new Color(0.20f, 0.20f, 0.30f, 1f);
    static readonly Color LabelColor    = new Color(0.60f, 0.60f, 0.70f, 1f);
    static readonly Color EquipBtnColor = new Color(0.18f, 0.42f, 0.18f, 1f);

    static readonly int FntMain = (int)UIScale.FontMd;
    static readonly int FntSub  = (int)UIScale.FontSm;

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create EquipCompare Popup Prefab")]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI");
        AssetDatabase.Refresh();

        var go = BuildPopup();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log("[EquipComparePopupCreator] EquipComparePopup 프리팹 생성 완료");
    }

    // ============================================================
    //  팝업 루트
    // ============================================================

    static GameObject BuildPopup()
    {
        var root = MakePanel(null, "EquipComparePopup", BgColor);
        root.AddComponent<CanvasGroup>();
        {
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(200f, 0f);
            rt.sizeDelta        = new Vector2(PopupW, PopupH);
        }

        var popup = root.AddComponent<EquipComparePopup>();
        var so    = new SerializedObject(popup);

        var typeProp = so.FindProperty("_popupType");
        if (typeProp != null) typeProp.intValue = (int)PopupType.EquipCompare;

        BuildHeader(root, so);
        BuildCards(root, so);
        BuildWarningAndEquipBtn(root, so);
        BuildInventoryScroll(root, so);

        so.ApplyModifiedProperties();
        return root;
    }

    // ============================================================
    //  헤더 (제목 + X 버튼)
    // ============================================================

    static void BuildHeader(GameObject root, SerializedObject so)
    {
        var header = MakePanel(root, "Header", HeaderColor);
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -HeaderH);
            rt.offsetMax = new Vector2(0, 0);
        }

        // 하단 구분선
        var div = MakeImage(header, "BottomDivider", DividerColor);
        {
            var rt = div.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 1);
        }

        // 제목
        var titleTmp = MakeTMP(header, "TitleText", "슬롯 장비 교체", FntMain, FontStyles.Bold);
        {
            var rt = titleTmp.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(16, 0);
            rt.offsetMax = new Vector2(-68, 0);
        }
        titleTmp.alignment = TextAlignmentOptions.Left;
        SetObj(so, "_titleText", titleTmp);

        // X 닫기 버튼 (우측)
        var xBtnGo = MakePanel(header, "CloseButton", new Color(0.30f, 0.14f, 0.14f, 0.90f));
        {
            var rt = xBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-58f, -24f);
            rt.offsetMax = new Vector2(-8f,  24f);
        }
        var xBtn = xBtnGo.AddComponent<Button>();
        xBtn.targetGraphic = xBtnGo.GetComponent<Image>();
        SetObj(so, "_closeBtn", xBtn);

        var xLbl = MakeTMP(xBtnGo, "Label", "✕", FntMain, FontStyles.Bold);
        xLbl.rectTransform.anchorMin = Vector2.zero;
        xLbl.rectTransform.anchorMax = Vector2.one;
        xLbl.alignment = TextAlignmentOptions.Center;
        xLbl.color     = new Color(0.92f, 0.80f, 0.80f);
    }

    // ============================================================
    //  장비 카드 2열 (현재 / 선택)
    // ============================================================

    static void BuildCards(GameObject root, SerializedObject so)
    {
        float top = HeaderH;

        var cardRow = MakePanel(root, "CardRow", new Color(0.07f, 0.07f, 0.13f, 1f));
        {
            var rt = cardRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(top + CardH));
            rt.offsetMax = new Vector2(0, -top);
        }

        // 중앙 구분선
        var midDiv = MakeImage(cardRow, "MiddleDivider", DividerColor);
        {
            var rt = midDiv.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.1f);
            rt.anchorMax = new Vector2(0.5f, 0.9f);
            rt.offsetMin = new Vector2(-1, 0);
            rt.offsetMax = new Vector2( 1, 0);
        }

        // 현재 장착 (좌)
        var curCard = MakePanel(cardRow, "CurrentCard", CardColor);
        {
            var rt = curCard.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(6, 6);
            rt.offsetMax = new Vector2(-3, -6);
        }

        var curLabel = MakeTMP(curCard, "Label", "현재 장착", FntSub, FontStyles.Normal);
        {
            var rt = curLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8, -26);
            rt.offsetMax = new Vector2(-8, 0);
        }
        curLabel.alignment = TextAlignmentOptions.Left;
        curLabel.color     = LabelColor;

        var (curBar, curIcon, curName, curStat) = BuildEquipCard(curCard, "Cur");
        SetObj(so, "_curGradeBar", curBar);
        SetObj(so, "_curIcon",     curIcon);
        SetObj(so, "_curName",     curName);
        SetObj(so, "_curStat",     curStat);

        // 귀속 배지
        var bindBadge = new GameObject("CurBindBadge", typeof(RectTransform), typeof(Image));
        bindBadge.transform.SetParent(curCard.transform, false);
        bindBadge.GetComponent<Image>().color = new Color(0.70f, 0.18f, 0.10f, 0.90f);
        {
            var rt = bindBadge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot     = new Vector2(1, 0);
            rt.offsetMin = new Vector2(-58, 6);
            rt.offsetMax = new Vector2(-6, 22);
        }
        var bindTmp = MakeTMP(bindBadge, "Label", "귀속", 22f, FontStyles.Bold);
        bindTmp.rectTransform.anchorMin = Vector2.zero;
        bindTmp.rectTransform.anchorMax = Vector2.one;
        bindTmp.alignment = TextAlignmentOptions.Center;
        bindBadge.SetActive(false);
        SetObj(so, "_curBindBadge", bindBadge);

        // 선택 장비 (우)
        var selCard = MakePanel(cardRow, "SelectedCard", CardColor);
        {
            var rt = selCard.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(1,    1);
            rt.offsetMin = new Vector2(3, 6);
            rt.offsetMax = new Vector2(-6, -6);
        }

        var selLabel = MakeTMP(selCard, "Label", "선택 장비", FntSub, FontStyles.Normal);
        {
            var rt = selLabel.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8, -26);
            rt.offsetMax = new Vector2(-8, 0);
        }
        selLabel.alignment = TextAlignmentOptions.Left;
        selLabel.color     = LabelColor;

        var (selBar, selIcon, selName, selStat) = BuildEquipCard(selCard, "Sel");
        SetObj(so, "_selGradeBar", selBar);
        SetObj(so, "_selIcon",     selIcon);
        SetObj(so, "_selName",     selName);
        SetObj(so, "_selStat",     selStat);
    }

    static (Image gradeBar, Image icon, TextMeshProUGUI name, TextMeshProUGUI stat)
        BuildEquipCard(GameObject card, string prefix)
    {
        // 등급 바 (좌측 세로)
        var gradeBar = MakeImage(card, $"{prefix}GradeBar", new Color(0.40f, 0.40f, 0.48f));
        {
            var rt = gradeBar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.offsetMin = new Vector2(0, 4);
            rt.offsetMax = new Vector2(5, -4);
        }

        // 아이콘 배경
        const float iconSz = 88f;
        var iconBg = MakeImage(card, $"{prefix}IconBg", new Color(0.08f, 0.08f, 0.16f));
        {
            var rt = iconBg.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(10, -iconSz * 0.5f);
            rt.offsetMax = new Vector2(10 + iconSz, iconSz * 0.5f);
        }

        // 아이콘
        var iconImg = MakeImage(card, $"{prefix}Icon", new Color(0.22f, 0.22f, 0.28f));
        {
            var rt = iconImg.rectTransform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(14,                 -iconSz * 0.5f + 4f);
            rt.offsetMax = new Vector2(14 + iconSz - 8f,   iconSz * 0.5f - 4f);
        }
        iconImg.preserveAspect = true;

        float textLeft = 10f + iconSz + 12f;

        // 이름
        var nameText = MakeTMP(card, $"{prefix}Name", "없음", FntMain, FontStyles.Bold);
        {
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 0.58f);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(textLeft, 4);
            rt.offsetMax = new Vector2(-8, -30);
        }
        nameText.alignment = TextAlignmentOptions.Left;

        // 스탯 텍스트
        var statText = MakeTMP(card, $"{prefix}Stat", "", FntSub, FontStyles.Normal);
        {
            var rt = statText.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0.62f);
            rt.offsetMin = new Vector2(textLeft, 6);
            rt.offsetMax = new Vector2(-8, 0);
        }
        statText.alignment        = TextAlignmentOptions.TopLeft;
        statText.color            = LabelColor;
        statText.textWrappingMode = TextWrappingModes.Normal;
        statText.overflowMode     = TextOverflowModes.Overflow;

        return (gradeBar, iconImg, nameText, statText);
    }

    // ============================================================
    //  경고 텍스트 + 장착 버튼
    // ============================================================

    static void BuildWarningAndEquipBtn(GameObject root, SerializedObject so)
    {
        float warnTop = HeaderH + CardH;
        float btnTop  = warnTop + WarnH;

        // 경고 텍스트
        var warnGo = MakeTMP(root, "WarningText",
            "현재 장비를 교체하면 기존 장비는 사라집니다.", FntSub, FontStyles.Normal);
        {
            var rt = warnGo.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -(warnTop + WarnH));
            rt.offsetMax = new Vector2(-12, -warnTop);
        }
        warnGo.alignment = TextAlignmentOptions.Center;
        warnGo.color     = new Color(0.88f, 0.65f, 0.20f);
        warnGo.gameObject.SetActive(false);
        SetObj(so, "_warningText", warnGo);

        // 장착 버튼
        var equipBtnGo = MakePanel(root, "EquipButton", EquipBtnColor);
        {
            var rt = equipBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8, -(btnTop + BtnH));
            rt.offsetMax = new Vector2(-8, -btnTop);
        }
        var equipBtn = equipBtnGo.AddComponent<Button>();
        equipBtn.targetGraphic = equipBtnGo.GetComponent<Image>();
        equipBtn.interactable  = false;
        SetObj(so, "_equipBtn", equipBtn);

        var equipLbl = MakeTMP(equipBtnGo, "Label", "장착", FntMain, FontStyles.Bold);
        equipLbl.rectTransform.anchorMin = Vector2.zero;
        equipLbl.rectTransform.anchorMax = Vector2.one;
        equipLbl.alignment = TextAlignmentOptions.Center;

        // 구분선
        var div = MakeImage(root, "EquipDivider", DividerColor);
        {
            var rt = div.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, -(btnTop + BtnH + DividerH));
            rt.offsetMax = new Vector2(0, -(btnTop + BtnH));
        }
    }

    // ============================================================
    //  인벤토리 스크롤 목록
    // ============================================================

    static void BuildInventoryScroll(GameObject root, SerializedObject so)
    {
        float scrollTop = HeaderH + CardH + WarnH + BtnH + DividerH;

        // 라벨
        const float lblH = 36f;
        var listLbl = MakeTMP(root, "InventoryLabel", "보유 장비", FntSub, FontStyles.Normal);
        {
            var rt = listLbl.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(12, -(scrollTop + lblH));
            rt.offsetMax = new Vector2(-12, -scrollTop);
        }
        listLbl.alignment = TextAlignmentOptions.Left;
        listLbl.color     = LabelColor;

        float innerTop = scrollTop + lblH;

        // ScrollRect
        var scrollGo = new GameObject("InventoryScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(root.transform, false);
        {
            var rt = scrollGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, -innerTop);
        }

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        {
            var rt = viewport.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Content (GridLayoutGroup — 아이콘 격자)
        var content = new GameObject("ListContent", typeof(RectTransform), typeof(GridLayoutGroup));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(80f, 80f);
        grid.spacing         = new Vector2(8f, 8f);
        grid.padding         = new RectOffset(8, 8, 8, 8);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperLeft;
        grid.constraint      = GridLayoutGroup.Constraint.Flexible;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.content      = contentRt;
        sr.viewport     = viewport.GetComponent<RectTransform>();
        sr.horizontal   = false;
        sr.vertical     = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        SetObj(so, "_listContent", content.transform);
    }

    // ============================================================
    //  UI 헬퍼
    // ============================================================

    static GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Image MakeImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name,
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

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
        else Debug.LogWarning($"[EquipComparePopupCreator] 필드 없음: {field}");
    }
}
#endif
