#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilitySelectPopupCreator.cs
//  Tools > Project K > 프리팹 생성 > 팝업 > AbilitySelect
//
//  저장: Assets/_project/2.Prefabs/UI/AbilitySelectPopup.prefab
//
//  ■ 왜 다시 짰나 (이전 레이아웃의 문제)
//    · 1472×840 창에 272×580 카드 5장이라 카드 하나가 좁고 길었다.
//      설명이 4~5줄로 접혀 "무슨 효과인지" 를 읽기 전에 눈이 지쳤다.
//    · 카드가 평평한 사각형이라 **누를 수 있는 것처럼 안 보였다** (UI 규칙 1 위반).
//    · 새로고침이 제목 아래 작은 버튼이라 남은 횟수가 눈에 안 들어왔다.
//
//  ■ 새 레이아웃 (AbilityList·HeroDetail 과 같은 전체화면 톤)
//    전체 1840 × (캔버스높이-32)
//    Header  H=136   ◆ 어빌리티 | 어빌리티 선택        [새로고침 N회 남음]
//    Body            카드 5장 가로 정렬 (한 장 328×640, 입체 카드)
//    Footer          안내 문구
//
//  ⚠ 닫기 버튼이 없다
//    이 팝업은 "골라야 넘어가는" 화면이다. 닫기를 주면 보상을 건너뛰게 된다.
//
//  ⚠ 카드 5장을 미리 만들어 둔다
//    AbilitySelectPopup 은 선택지가 5개보다 많으면 Card0 을 복제해 늘린다.
//    그래서 0번 카드는 반드시 완성된 형태여야 한다.
// ============================================================

public static class AbilitySelectPopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/AbilitySelectPopup.prefab";

    // ── 치수 ─────────────────────────────────────────────────
    const float PW       = 1840f;
    const float PVMargin =   16f;
    const float HeaderH  =  136f;
    const float BodyTop  =  156f;

    const int   MaxCards =    5;
    const float CardW    =  328f;
    const float CardH    =  640f;
    const float CardGap  =   18f;

    const float IconSz   =  148f;
    const float FooterH  =   64f;

    // ── 색상 (AbilityList 와 같은 보라 계열) ──────────────────
    static readonly Color BgOverlay    = new Color(0f,     0f,     0f,     0.86f);
    static readonly Color PanelBg      = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder  = new Color(0.44f,  0.32f,  0.72f,  1f);
    static readonly Color HeaderBg     = new Color(0.11f,  0.07f,  0.19f,  1f);
    static readonly Color AccentPurple = new Color(0.66f,  0.48f,  1.00f,  1f);
    static readonly Color TagColor     = new Color(0.80f,  0.68f,  1.00f,  1f);
    static readonly Color TitleColor   = new Color(1.00f,  0.94f,  0.86f,  1f);
    static readonly Color TitleShadow  = new Color(0.03f,  0.02f,  0.05f,  0.85f);

    static readonly Color CardFace     = new Color(0.155f, 0.145f, 0.255f, 1f);
    static readonly Color CardInner    = new Color(0.10f,  0.10f,  0.175f, 1f);
    static readonly Color IconPadBg    = new Color(0.24f,  0.21f,  0.36f,  1f);
    static readonly Color DescColor    = new Color(0.82f,  0.86f,  0.96f,  1f);
    static readonly Color LabelColor   = new Color(0.64f,  0.66f,  0.78f,  1f);
    static readonly Color LevelColor   = new Color(1.00f,  0.86f,  0.42f,  1f);
    static readonly Color SelectBtnC   = new Color(0.36f,  0.24f,  0.62f,  1f);
    static readonly Color RefreshBtnC  = new Color(0.14f,  0.38f,  0.52f,  1f);
    static readonly Color RefreshTxt   = new Color(0.62f,  0.88f,  1.00f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "AbilitySelect", priority = ProjectKMenu.PrefabPrio + 34)]
    public static void Create()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        var root = Build();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AbilitySelectPopupCreator] 저장: {PrefabPath} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    static GameObject Build()
    {
        var root = EditorUIBuilder.Panel(null, "AbilitySelectPopup", BgOverlay);
        EditorUIBuilder.Stretch(root);

        var popup = root.AddComponent<AbilitySelectPopup>();
        var so    = new SerializedObject(popup);
        EditorUIBuilder.SetEnum(so, "_popupType", (int)PopupType.AbilitySelect, "AbilitySelectPopupCreator");

        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        StretchV(border.GetComponent<RectTransform>(), PW + 6f, PVMargin - 3f);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        StretchV(panel.GetComponent<RectTransform>(), PW, PVMargin);

        BuildHeader(panel, so);
        BuildCards(panel, so);
        BuildFooter(panel);

        so.ApplyModifiedProperties();
        return root;
    }

    // ══════════════════════════════════════════════════════════
    //  헤더 — 제목 + 새로고침
    // ══════════════════════════════════════════════════════════

    static void BuildHeader(GameObject panel, SerializedObject so)
    {
        var header = Go("Header", panel);
        header.AddComponent<Image>().color = HeaderBg;
        EditorUIBuilder.AnchorTop(header.GetComponent<RectTransform>(), 0f, HeaderH);

        var tagRoot = Go("AbilityTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(340f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "어 빌 리 티", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        MakeTitle(header, "TitleShadow", TitleShadow, 3f);
        var titleTmp = MakeTitle(header, "TitleText", TitleColor, 0f);
        SetObj(so, "_titleTmp", titleTmp);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentPurple;
        EditorUIBuilder.AnchorTop(accent.GetComponent<RectTransform>(), HeaderH, 3f);

        // ── 새로고침 (버튼 + 남은 횟수) ──────────────────────
        //  남은 횟수가 버튼 안이 아니라 옆에 크게 붙는다 —
        //  "몇 번 더 돌릴 수 있나" 가 누르기 전에 보여야 판단이 선다.
        float btnH = UIScale.BtnFor(UIScale.FontMd);

        var countTmp = TMP(header, "RefreshCountText", "새로고침 0회 남음", UIScale.FontMd, FontStyles.Bold);
        countTmp.color            = RefreshTxt;
        countTmp.alignment        = TextAlignmentOptions.MidlineRight;
        countTmp.raycastTarget    = false;
        countTmp.textWrappingMode = TextWrappingModes.NoWrap;
        var ctRt = countTmp.rectTransform;
        ctRt.anchorMin = ctRt.anchorMax = new Vector2(1f, 0.5f);
        ctRt.pivot     = new Vector2(1f, 0.5f);
        ctRt.anchoredPosition = new Vector2(-(30f + 240f + 20f), 0f);
        ctRt.sizeDelta        = new Vector2(420f, UIScale.RowMd);
        SetObj(so, "_refreshCountTmp", countTmp);

        var refreshBtn = EditorUIBuilder.RaisedBtn(header, "RefreshBtn", RefreshBtnC, out var rBody);
        var rbRt = refreshBtn.GetComponent<RectTransform>();
        rbRt.anchorMin = rbRt.anchorMax = new Vector2(1f, 0.5f);
        rbRt.pivot     = new Vector2(1f, 0.5f);
        rbRt.anchoredPosition = new Vector2(-30f, 0f);
        rbRt.sizeDelta        = new Vector2(240f, btnH);

        var rLbl = TMP(rBody, "Label", "새로고침", UIScale.FontMd, FontStyles.Bold);
        rLbl.color         = Color.white;
        rLbl.alignment     = TextAlignmentOptions.Center;
        rLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(rLbl.gameObject);
        SetObj(so, "_refreshBtn", refreshBtn);
    }

    static TextMeshProUGUI MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "어빌리티 선택", UIScale.FontLg, FontStyles.Bold);
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Left;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f + dy, -52f - dy);
        rt.sizeDelta        = new Vector2(900f, UIScale.RowLg);
        return tmp;
    }

    // ══════════════════════════════════════════════════════════
    //  카드 영역
    // ══════════════════════════════════════════════════════════

    static void BuildCards(GameObject panel, SerializedObject so)
    {
        var area = Go("CardArea", panel);
        var aRt = area.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.5f, 0.5f);
        aRt.anchorMax = new Vector2(0.5f, 0.5f);
        aRt.pivot     = new Vector2(0.5f, 0.5f);
        aRt.anchoredPosition = new Vector2(0f, -(BodyTop - HeaderH) * 0.5f);
        aRt.sizeDelta        = new Vector2(MaxCards * CardW + (MaxCards - 1) * CardGap, CardH);

        var hlg = area.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = CardGap;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        var cards = new Object[MaxCards];
        for (int i = 0; i < MaxCards; i++)
            cards[i] = BuildCard(area, $"Card{i}");

        var prop = so.FindProperty("_cards");
        if (prop == null)
        {
            Debug.LogError("[AbilitySelectPopupCreator] 필드 없음: _cards");
            return;
        }
        prop.arraySize = MaxCards;
        for (int i = 0; i < MaxCards; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
    }

    /// <summary>
    /// 어빌리티 카드 한 장. 카드 전체가 눌리는 입체 버튼이다 (UI 규칙 1).
    ///
    /// ⚠ 내용물은 전부 Body 아래에 넣는다
    ///   루트에 넣으면 눌러도 같이 안 내려가 "판이 눌린" 느낌이 안 난다.
    /// </summary>
    static AbilityCardUI BuildCard(GameObject parent, string name)
    {
        var card = Go(name, parent);
        var cRt = card.GetComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(CardW, CardH);

        var selectBtn = EditorUIBuilder.RaisedBtnOn(card, CardFace, out var body);
        var cardUI    = card.AddComponent<AbilityCardUI>();

        // ── 등급 바 (카드 최상단 굵은 띠) ────────────────────
        var gradeBar = EditorUIBuilder.Img(body, "GradeBar", AccentPurple);
        var gbRt = gradeBar.rectTransform;
        gbRt.anchorMin = new Vector2(0f, 1f); gbRt.anchorMax = new Vector2(1f, 1f);
        gbRt.pivot     = new Vector2(0.5f, 1f);
        gbRt.anchoredPosition = Vector2.zero;
        gbRt.sizeDelta        = new Vector2(0f, 12f);

        float y = 12f + 22f;

        // ── 아이콘 ───────────────────────────────────────────
        var iconPad = EditorUIBuilder.Img(body, "IconPad", IconPadBg);
        var ipRt = iconPad.rectTransform;
        ipRt.anchorMin = ipRt.anchorMax = new Vector2(0.5f, 1f);
        ipRt.pivot     = new Vector2(0.5f, 1f);
        ipRt.anchoredPosition = new Vector2(0f, -y);
        ipRt.sizeDelta        = new Vector2(IconSz + 14f, IconSz + 14f);

        var icon = EditorUIBuilder.Img(body, "Icon", Color.white);
        var icRt = icon.rectTransform;
        icRt.anchorMin = icRt.anchorMax = new Vector2(0.5f, 1f);
        icRt.pivot     = new Vector2(0.5f, 1f);
        icRt.anchoredPosition = new Vector2(0f, -(y + 7f));
        icRt.sizeDelta        = new Vector2(IconSz, IconSz);
        icon.preserveAspect = true;

        y += IconSz + 14f + 16f;

        // ── 이름 ─────────────────────────────────────────────
        //  칸 높이는 폰트 한 줄 이상 (UI 규칙 5). 길면 두 줄까지 접고 자동 축소.
        float nameH = UIScale.Line(UIScale.FontMd) * 2f;

        var nameTmp = TMP(body, "NameText", "어빌리티", UIScale.FontMd, FontStyles.Bold);
        nameTmp.color            = Color.white;
        nameTmp.alignment        = TextAlignmentOptions.Center;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.Normal;
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontSm;
        nameTmp.fontSizeMax      = UIScale.FontMd;
        PinTop(nameTmp.rectTransform, y, nameH, 14f);

        y += nameH + 6f;

        // ── 등급 · 대상 (한 줄에 나란히) ─────────────────────
        var gradeTmp = TMP(body, "GradeText", "등급", UIScale.FontSm, FontStyles.Bold);
        gradeTmp.color         = AccentPurple;
        gradeTmp.alignment     = TextAlignmentOptions.MidlineRight;
        gradeTmp.raycastTarget = false;
        var grRt = gradeTmp.rectTransform;
        grRt.anchorMin = new Vector2(0f, 1f); grRt.anchorMax = new Vector2(0.5f, 1f);
        grRt.pivot     = new Vector2(0.5f, 1f);
        grRt.anchoredPosition = new Vector2(0f, -y);
        grRt.sizeDelta        = new Vector2(-14f, UIScale.RowSm);

        var targetTmp = TMP(body, "TargetText", "대상", UIScale.FontSm, FontStyles.Normal);
        targetTmp.color         = LabelColor;
        targetTmp.alignment     = TextAlignmentOptions.MidlineLeft;
        targetTmp.raycastTarget = false;
        var tgRt = targetTmp.rectTransform;
        tgRt.anchorMin = new Vector2(0.5f, 1f); tgRt.anchorMax = new Vector2(1f, 1f);
        tgRt.pivot     = new Vector2(0.5f, 1f);
        tgRt.anchoredPosition = new Vector2(0f, -y);
        tgRt.sizeDelta        = new Vector2(-14f, UIScale.RowSm);

        y += UIScale.RowSm + 4f;

        // ── 레벨 ─────────────────────────────────────────────
        var levelTmp = TMP(body, "LevelText", "Lv 1/3", UIScale.FontSm, FontStyles.Bold);
        levelTmp.color         = LevelColor;
        levelTmp.alignment     = TextAlignmentOptions.Center;
        levelTmp.raycastTarget = false;
        PinTop(levelTmp.rectTransform, y, UIScale.RowSm, 14f);

        y += UIScale.RowSm + 12f;

        // ── 설명 (남는 높이를 전부 쓴다) ─────────────────────
        float btnH   = UIScale.BtnFor(UIScale.FontMd);
        float descBt = btnH + 20f;

        var descBg = Go("DescBg", body);
        descBg.AddComponent<Image>().color = CardInner;
        var dbRt = descBg.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(0f, 0f); dbRt.anchorMax = new Vector2(1f, 1f);
        dbRt.offsetMin = new Vector2(14f, descBt);
        dbRt.offsetMax = new Vector2(-14f, -y);

        var descTmp = TMP(descBg, "DescText", "", UIScale.FontSm, FontStyles.Normal);
        descTmp.color            = DescColor;
        descTmp.alignment        = TextAlignmentOptions.TopLeft;
        descTmp.raycastTarget    = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.lineSpacing      = 10f;
        // ⚠ Ellipsis 로 두면 긴 설명이 통째로 "..." 이 된다
        descTmp.overflowMode     = TextOverflowModes.Overflow;
        descTmp.enableAutoSizing = true;
        descTmp.fontSizeMin      = 26f;
        descTmp.fontSizeMax      = UIScale.FontSm;
        var dtRt = descTmp.rectTransform;
        dtRt.anchorMin = Vector2.zero; dtRt.anchorMax = Vector2.one;
        dtRt.offsetMin = new Vector2(14f, 12f);
        dtRt.offsetMax = new Vector2(-14f, -12f);

        // ── 선택 표시 (카드 하단 띠) ─────────────────────────
        //  카드 전체가 버튼이지만, "여기를 누르면 고른다" 를 글자로 못 박는다.
        var pick = Go("PickBar", body);
        pick.AddComponent<Image>().color = SelectBtnC;
        var pkRt = pick.GetComponent<RectTransform>();
        pkRt.anchorMin = new Vector2(0f, 0f); pkRt.anchorMax = new Vector2(1f, 0f);
        pkRt.pivot     = new Vector2(0.5f, 0f);
        pkRt.offsetMin = new Vector2(14f, 12f);
        pkRt.offsetMax = new Vector2(-14f, 12f + btnH);

        var pickLbl = TMP(pick, "Label", "선  택", UIScale.FontMd, FontStyles.Bold);
        pickLbl.color         = Color.white;
        pickLbl.alignment     = TextAlignmentOptions.Center;
        pickLbl.raycastTarget = false;
        EditorUIBuilder.Stretch(pickLbl.gameObject);

        // ── AbilityCardUI 필드 연결 ──────────────────────────
        var cardSo = new SerializedObject(cardUI);
        SetObjOn(cardSo, "_gradeBar",  gradeBar);
        SetObjOn(cardSo, "_icon",      icon);
        SetObjOn(cardSo, "_gradeTmp",  gradeTmp);
        SetObjOn(cardSo, "_nameTmp",   nameTmp);
        SetObjOn(cardSo, "_targetTmp", targetTmp);
        SetObjOn(cardSo, "_descTmp",   descTmp);
        SetObjOn(cardSo, "_levelTmp",  levelTmp);
        SetObjOn(cardSo, "_selectBtn", selectBtn);
        cardSo.ApplyModifiedProperties();

        return cardUI;
    }

    // ══════════════════════════════════════════════════════════
    //  푸터
    // ══════════════════════════════════════════════════════════

    static void BuildFooter(GameObject panel)
    {
        var tmp = TMP(panel, "FooterHint", "카드를 눌러 하나를 고르세요 — 고른 어빌리티는 이번 런 동안 유지됩니다.",
                      UIScale.FontSm, FontStyles.Normal);
        tmp.color         = LabelColor;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 18f);
        rt.sizeDelta        = new Vector2(-80f, FooterH);
    }

    // ══════════════════════════════════════════════════════════
    //  헬퍼
    // ══════════════════════════════════════════════════════════

    // 부모 위쪽에서 yFromTop 만큼 내려 가로 전체(좌우 padH)로 붙인다
    static void PinTop(RectTransform rt, float yFromTop, float height, float padH)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta        = new Vector2(-padH * 2f, height);
    }

    static void StretchV(RectTransform rt, float width, float vMargin)
    {
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(width, -vMargin * 2f);
    }

    static GameObject Go(string name, GameObject parent) => EditorUIBuilder.Go(name, parent);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text,
                               float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "AbilitySelectPopupCreator");

    static void SetObjOn(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "AbilitySelectPopupCreator(Card)");
}
#endif
