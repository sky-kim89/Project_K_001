#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EquipComparePopupCreator.cs  [Editor Only]
//  Tools > Project K > 프리팹 생성 > 팝업 > EquipCompare
//
//  ⚠ 위치는 HeroDetailPopup 과 짝을 이룬다
//    HeroDetail 이 전체 화면 3단이 되면서 우측(스킬) 열 중심이 x=+540 이다.
//    장비 칸을 누르면 "스킬 자리에 장비 창이 들어선" 것으로 읽혀야 하므로
//    다만 증감 표시로 줄이 길어져 왼쪽으로 넓혔다 — 오른쪽 끝만 그 열에 맞춘다.
//    한쪽 폭·위치를 바꾸면 다른 쪽도 같이 고칠 것.
//
//  ■ 왜 다시 짰나 (이전 레이아웃의 문제)
//    · 두 장비 스탯을 나란히 적어 두기만 해서 어느 쪽이 나은지 직접 뺄셈해야 했다.
//    · 인벤토리 칸이 맨 Image 라 등급을 알 수 없고, 눌러도 선택 표시가 없었다.
//    · 장착·닫기가 평평한 사각형이라 버튼으로 안 읽혔다 (UI 규칙 1 위반).
//    · 보유 장비가 없으면 아무 안내도 없이 빈 화면이었다.
//
//  ■ 레이아웃 (1040×940, x=+380 — 오른쪽 끝을 스킬 열에 맞추고 왼쪽으로 확장)
//    Header      96   ◆ 장 비 | 슬롯 N 교체                    [X]
//    AccentLine   3
//    "비  교"    32   구분선
//    CardRow    340   현재 장착 | 선택 장비 (선택 쪽에 증감 표시)
//    Warning     43   교체 시 기존 장비 소멸 경고 (장착 중일 때만)
//    ActionRow   72   [장 착] [분 해 🔧+N] — 둘 다 "고른 장비" 대상
//    "보유 장비" 32   구분선
//    Grid       나머지 — 등급 테두리 + 선택 표시가 있는 격자
// ============================================================

public static class EquipComparePopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/EquipComparePopup.prefab";

    // ── 치수 ─────────────────────────────────────────────────
    // 스탯 증감까지 적으면 한 줄이 길어져 720 폭으로는 경고문과 겹쳤다.
    // 오른쪽 끝(=HeroDetail 스킬 열 오른쪽 +900)은 그대로 두고 왼쪽으로만 넓힌다.
    //   왼쪽 -140 … 오른쪽 +900  →  폭 1040, 중심 +380
    const float PW   = 1040f;
    const float PH   = 940f;
    const float PosX = 380f;

    const float HeaderH = 96f;
    const float DivH    = 32f;

    const float CmpDivY  = 112f;
    const float CardY    = 152f;
    const float CardH    = 340f;
    const float WarnY    = 502f;
    const float BtnY     = 553f;
    const float InvDivY  = 641f;
    const float GridY    = 681f;
    const float GridBtm  = 14f;

    const float CardIcon = 96f;
    const float CellSize = 96f;

    // 장착 / 분해 버튼 분할 지점 — 주 동작(장착)에 더 넓게 준다
    const float ActionSplit = 0.64f;

    static readonly float WarnH = UIScale.RowSm;                      // 43
    static readonly float BtnH  = UIScale.BtnFor(UIScale.FontMd);     // 72

    // ── 색상 (HeroDetail·RunShop 과 같은 계열) ───────────────
    static readonly Color BgOverlay    = new Color(0f,     0f,     0f,     0.72f);
    static readonly Color PanelBg      = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder  = new Color(0.30f,  0.52f,  0.42f,  1f);
    static readonly Color HeaderBg     = new Color(0.07f,  0.13f,  0.11f,  1f);
    static readonly Color AccentGreen  = new Color(0.36f,  0.82f,  0.60f,  1f);
    static readonly Color TagColor     = new Color(0.60f,  0.95f,  0.78f,  1f);
    static readonly Color TitleColor   = new Color(1.00f,  0.96f,  0.84f,  1f);

    static readonly Color CardBg       = new Color(0.105f, 0.115f, 0.185f, 1f);
    static readonly Color PitBg        = new Color(0.055f, 0.06f,  0.10f,  1f);
    static readonly Color DividerLine  = new Color(0.26f,  0.30f,  0.28f,  0.85f);
    static readonly Color DividerLabel = new Color(0.72f,  0.78f,  0.74f,  1f);
    static readonly Color LabelColor   = new Color(0.58f,  0.60f,  0.70f,  1f);
    static readonly Color StatColor    = new Color(0.82f,  0.86f,  0.94f,  1f);

    static readonly Color EquipBtnC    = new Color(0.14f,  0.48f,  0.30f,  1f);
    static readonly Color DisBtnC      = new Color(0.42f,  0.24f,  0.20f,  1f);
    static readonly Color CloseBtnC    = new Color(0.50f,  0.14f,  0.14f,  1f);
    static readonly Color BindBadgeC   = new Color(0.62f,  0.20f,  0.14f,  0.92f);
    static readonly Color SelectMarkC  = new Color(1.00f,  0.92f,  0.45f,  1f);
    static readonly Color WarnColor    = new Color(0.94f,  0.72f,  0.28f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "EquipCompare", priority = ProjectKMenu.PrefabPrio + 36)]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI");
        AssetDatabase.Refresh();

        var go = BuildPopup();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log($"[EquipComparePopupCreator] 저장: {PrefabPath} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    static GameObject BuildPopup()
    {
        // 루트는 전체 화면이지만 배경을 아주 옅게만 깐다 —
        // 아래에 깔린 HeroDetail 의 스탯을 보면서 고를 수 있어야 한다.
        var root = Panel(null, "EquipComparePopup", BgOverlay);
        Stretch(root);
        var popup = root.AddComponent<EquipComparePopup>();
        var so    = new SerializedObject(popup);
        SetEnum(so, "_popupType", (int)PopupType.EquipCompare);

        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        CenterBox(border, PW + 6f, PH + 6f, PosX);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        CenterBox(panel, PW, PH, PosX);

        BuildHeader(panel, so);

        BuildDivider(panel, CmpDivY, "비  교");
        BuildCards(panel, so);
        BuildWarningAndButton(panel, so);

        BuildDivider(panel, InvDivY, "보유 장비");
        BuildGrid(panel, so);

        so.ApplyModifiedProperties();
        return root;
    }

    // ══════════════════════════════════════════════════════════
    //  헤더
    // ══════════════════════════════════════════════════════════

    static void BuildHeader(GameObject panel, SerializedObject so)
    {
        var header = Go("Header", panel);
        header.AddComponent<Image>().color = HeaderBg;
        AnchorTop(header, 0f, HeaderH);

        // ★ 는 폰트에 없다 (□ 로 렌더됨) → 마름모 도형으로 대체
        var tagRoot = Go("EquipTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(24f, -10f);
        tagRt.sizeDelta        = new Vector2(240f, 30f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 14f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(8f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "장 비", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(26f, 0f); tlRt.offsetMax = Vector2.zero;

        var title = TMP(header, "TitleText", "슬롯 1 교체", UIScale.FontMd, FontStyles.Bold);
        title.color            = TitleColor;
        title.alignment        = TextAlignmentOptions.MidlineLeft;
        title.raycastTarget    = false;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode     = TextOverflowModes.Overflow;
        var ttRt = title.rectTransform;
        ttRt.anchorMin = ttRt.anchorMax = new Vector2(0f, 1f);
        ttRt.pivot     = new Vector2(0f, 1f);
        ttRt.anchoredPosition = new Vector2(24f, -44f);
        ttRt.sizeDelta        = new Vector2(460f, UIScale.RowMd);
        SetObj(so, "_titleText", title);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentGreen;
        AnchorTop(accent, HeaderH, 3f);

        var closeBtn = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseBtnC, out var body);
        AnchorRight(closeBtn.gameObject, -16f, 64f, 64f);
        Center(EditorUIBuilder.XMark(body, "Mark", UIScale.FontSm, Color.white));
        SetObj(so, "_closeBtn", closeBtn);
    }

    // ══════════════════════════════════════════════════════════
    //  비교 카드 2장
    // ══════════════════════════════════════════════════════════

    static void BuildCards(GameObject panel, SerializedObject so)
    {
        var row = Go("CardRow", panel);
        AnchorTop(row, CardY, CardH, 24f);

        BuildCard(row, so, "Cur", "현재 장착", 0f,    0.5f, -6f, hasBind: true);
        BuildCard(row, so, "Sel", "선택 장비", 0.5f, 1f,     6f, hasBind: false);
    }

    static void BuildCard(GameObject row, SerializedObject so, string prefix, string label,
                          float aMinX, float aMaxX, float inset, bool hasBind)
    {
        var card = Go($"{prefix}Card", row);
        card.AddComponent<Image>().color = CardBg;
        var rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 0f);
        rt.anchorMax = new Vector2(aMaxX, 1f);
        rt.offsetMin = new Vector2(inset > 0 ? inset : 0f, 0f);
        rt.offsetMax = new Vector2(inset < 0 ? inset : 0f, 0f);

        // 좌측 등급 바
        var gradeBar = Go("GradeBar", card, typeof(Image)).GetComponent<Image>();
        gradeBar.color         = new Color(0.24f, 0.25f, 0.34f);
        gradeBar.raycastTarget = false;
        var gbRt = gradeBar.rectTransform;
        gbRt.anchorMin = new Vector2(0f, 0f); gbRt.anchorMax = new Vector2(0f, 1f);
        gbRt.offsetMin = Vector2.zero;        gbRt.offsetMax = new Vector2(6f, 0f);

        // 어느 쪽 카드인지
        var lbl = TMP(card, "Label", label, UIScale.FontSm, FontStyles.Normal);
        lbl.color         = LabelColor;
        lbl.alignment     = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget = false;
        AnchorTop(lbl.gameObject, 10f, UIScale.RowSm, 36f);

        // 아이콘
        var pit = Go("IconPit", card);
        pit.AddComponent<Image>().color = PitBg;
        var pitRt = pit.GetComponent<RectTransform>();
        pitRt.anchorMin = pitRt.anchorMax = new Vector2(0f, 1f);
        pitRt.pivot     = new Vector2(0f, 1f);
        pitRt.anchoredPosition = new Vector2(18f, -58f);
        pitRt.sizeDelta        = new Vector2(CardIcon, CardIcon);

        var icon = Go("Icon", pit, typeof(Image)).GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        var iRt = icon.rectTransform;
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(8f, 8f); iRt.offsetMax = new Vector2(-8f, -8f);

        // 빈 칸 표시 (＋ 도형 — 글리프 대신)
        var emptyMark = Go("EmptyMark", pit);
        EditorUIBuilder.Bar(emptyMark, "H", 28f, 4f, 0f,  Vector2.zero, new Color(0.32f, 0.34f, 0.46f));
        EditorUIBuilder.Bar(emptyMark, "V", 28f, 4f, 90f, Vector2.zero, new Color(0.32f, 0.34f, 0.46f));
        Center(emptyMark);
        emptyMark.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);

        float textLeft = 18f + CardIcon + 14f;

        var nameTmp = TMP(card, "NameText", "없음", UIScale.FontMd, FontStyles.Bold);
        nameTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Overflow;
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontSm - 6f;
        nameTmp.fontSizeMax      = UIScale.FontMd;
        var nRt = nameTmp.rectTransform;
        nRt.anchorMin = nRt.anchorMax = new Vector2(0f, 1f);
        nRt.pivot     = new Vector2(0f, 1f);
        nRt.anchoredPosition = new Vector2(textLeft, -60f);
        nRt.sizeDelta        = new Vector2(300f, UIScale.RowMd);

        var gradeTmp = TMP(card, "GradeText", "", UIScale.FontSm, FontStyles.Bold);
        gradeTmp.alignment     = TextAlignmentOptions.MidlineLeft;
        gradeTmp.raycastTarget = false;
        var grRt = gradeTmp.rectTransform;
        grRt.anchorMin = grRt.anchorMax = new Vector2(0f, 1f);
        grRt.pivot     = new Vector2(0f, 1f);
        grRt.anchoredPosition = new Vector2(textLeft, -116f);
        grRt.sizeDelta        = new Vector2(300f, UIScale.RowSm);

        // 스탯 — 아이콘 아래 전체 폭. 증감이 붙으므로 줄이 길어진다.
        var statTmp = TMP(card, "StatText", "", UIScale.FontSm, FontStyles.Normal);
        statTmp.color            = StatColor;
        statTmp.alignment        = TextAlignmentOptions.TopLeft;
        statTmp.raycastTarget    = false;
        statTmp.textWrappingMode = TextWrappingModes.Normal;
        statTmp.overflowMode     = TextOverflowModes.Overflow;
        statTmp.lineSpacing      = 8f;
        var sRt = statTmp.rectTransform;
        sRt.anchorMin = new Vector2(0f, 0f); sRt.anchorMax = new Vector2(1f, 1f);
        sRt.offsetMin = new Vector2(18f, 10f);
        sRt.offsetMax = new Vector2(-16f, -(58f + CardIcon + 8f));

        SetObj(so, $"_{prefix.ToLower()}GradeBar", gradeBar);
        SetObj(so, $"_{prefix.ToLower()}Icon",     icon);
        SetObj(so, $"_{prefix.ToLower()}Name",     nameTmp);
        SetObj(so, $"_{prefix.ToLower()}Grade",    gradeTmp);
        SetObj(so, $"_{prefix.ToLower()}Stat",     statTmp);
        SetObj(so, $"_{prefix.ToLower()}EmptyMark", emptyMark);

        if (!hasBind) return;

        // 귀속 배지 — 장착 중인 장비는 회수되지 않는다는 표시
        var badge = Go("CurBindBadge", card);
        badge.AddComponent<Image>().color = BindBadgeC;
        var bRt = badge.GetComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = new Vector2(1f, 1f);
        bRt.pivot     = new Vector2(1f, 1f);
        bRt.anchoredPosition = new Vector2(-10f, -10f);
        bRt.sizeDelta        = new Vector2(84f, UIScale.RowSm);

        var bTmp = TMP(badge, "Label", "귀속", UIScale.FontSm - 6f, FontStyles.Bold);
        bTmp.alignment     = TextAlignmentOptions.Center;
        bTmp.raycastTarget = false;
        Stretch(bTmp.gameObject);

        badge.SetActive(false);
        SetObj(so, "_curBindBadge", badge);
    }

    // ══════════════════════════════════════════════════════════
    //  경고 + 장착 버튼
    // ══════════════════════════════════════════════════════════

    static void BuildWarningAndButton(GameObject panel, SerializedObject so)
    {
        var warn = TMP(panel, "WarningText",
            "교체하면 지금 장착한 장비는 사라집니다.", UIScale.FontSm, FontStyles.Normal);
        warn.color            = WarnColor;
        warn.alignment        = TextAlignmentOptions.Center;
        warn.raycastTarget    = false;
        warn.textWrappingMode = TextWrappingModes.NoWrap;
        warn.overflowMode     = TextOverflowModes.Overflow;
        AnchorTop(warn.gameObject, WarnY, WarnH, 24f);
        warn.gameObject.SetActive(false);
        SetObj(so, "_warningText", warn);

        // ── 장착 (좌) ────────────────────────────────────
        var btn = EditorUIBuilder.RaisedBtn(panel, "EquipButton", EquipBtnC, out var body);
        ActionRect(btn.gameObject, 0f, ActionSplit, 24f, -6f);
        btn.interactable = false;

        // 비활성 이유를 라벨로 말해 준다 — 그냥 안 눌리면 고장으로 읽힌다.
        var lbl = TMP(body, "Label", "장비를 고르세요", UIScale.FontMd, FontStyles.Bold);
        lbl.alignment        = TextAlignmentOptions.Center;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Overflow;
        Stretch(lbl.gameObject);

        SetObj(so, "_equipBtn",      btn);
        SetObj(so, "_equipBtnLabel", lbl);

        // ── 분해 (우) ────────────────────────────────────
        //  DisassemblePopup 을 따로 열지 않아도 여기서 정리할 수 있게 한다.
        //  보상(강화석)을 버튼에 미리 적어 둔다 — 눌러 보고 알게 하면 안 된다.
        var disBtn = EditorUIBuilder.RaisedBtn(panel, "DisassembleButton", DisBtnC, out var disBody);
        ActionRect(disBtn.gameObject, ActionSplit, 1f, 24f, 6f);
        disBtn.interactable = false;

        var content = Go("Content", disBody);
        Stretch(content);
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 6f;
        hlg.padding                = new RectOffset(10, 10, 0, 0);
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        var disLbl = TMP(content, "Label", "분 해", UIScale.FontMd, FontStyles.Bold);
        disLbl.alignment        = TextAlignmentOptions.Right;
        disLbl.raycastTarget    = false;
        disLbl.textWrappingMode = TextWrappingModes.NoWrap;
        disLbl.overflowMode     = TextOverflowModes.Overflow;

        var stoneIcon = Go("GainIcon", content, typeof(Image)).GetComponent<Image>();
        stoneIcon.sprite         = LoadStoneIcon();
        stoneIcon.preserveAspect = true;
        stoneIcon.raycastTarget  = false;
        EditorUIBuilder.IconLE(stoneIcon, 30f);

        var gain = TMP(content, "GainText", "-", UIScale.FontSm, FontStyles.Bold);
        gain.color            = new Color(0.80f, 0.92f, 1.00f);
        gain.alignment        = TextAlignmentOptions.Left;
        gain.raycastTarget    = false;
        gain.textWrappingMode = TextWrappingModes.NoWrap;
        gain.overflowMode     = TextOverflowModes.Overflow;

        SetObj(so, "_disassembleBtn",      disBtn);
        SetObj(so, "_disassembleGainText", gain);
    }

    //  하단 액션 행에서 좌우로 나눠 앉히는 배치 (비율 + 바깥 여백 + 안쪽 간격)
    static void ActionRect(GameObject go, float aMinX, float aMaxX, float padH, float inset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 1f);
        rt.anchorMax = new Vector2(aMaxX, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(aMinX > 0f ? inset : padH * 0.5f,  -(BtnY + BtnH));
        rt.offsetMax = new Vector2(aMaxX < 1f ? inset : -padH * 0.5f, -BtnY);
    }

    // ══════════════════════════════════════════════════════════
    //  인벤토리 격자
    // ══════════════════════════════════════════════════════════

    static void BuildGrid(GameObject panel, SerializedObject so)
    {
        var scrollGo = Go("InventoryScroll", panel, typeof(ScrollRect));
        var sRt = scrollGo.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f); sRt.anchorMax = new Vector2(1f, 1f);
        sRt.offsetMin = new Vector2(12f, GridBtm);
        sRt.offsetMax = new Vector2(-12f, -GridY);

        var viewport = Go("Viewport", scrollGo, typeof(Image), typeof(Mask));
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport);

        var content = Go("ListContent", viewport, typeof(GridLayoutGroup));
        var cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 1f); cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot     = new Vector2(0.5f, 1f);
        cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize       = new Vector2(CellSize, CellSize);
        grid.spacing        = new Vector2(8f, 8f);
        grid.padding        = new RectOffset(4, 4, 4, 4);
        grid.startCorner    = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis      = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint     = GridLayoutGroup.Constraint.Flexible;

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.content      = cRt;
        sr.viewport     = viewport.GetComponent<RectTransform>();
        sr.horizontal   = false;
        sr.vertical     = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        SetObj(so, "_listContent", content.transform);
        SetObj(so, "_pickTemplate", BuildPickTemplate(content));

        // 비어 있을 때 안내 — 격자 위에 겹쳐 둔다 (스크롤 밖이라 레이아웃에 안 낀다)
        var empty = TMP(panel, "EmptyText", "보유한 장비가 없습니다.", UIScale.FontSm, FontStyles.Normal);
        empty.color         = LabelColor;
        empty.alignment     = TextAlignmentOptions.Center;
        empty.raycastTarget = false;
        AnchorTop(empty.gameObject, GridY + 40f, UIScale.RowSm, 24f);
        empty.gameObject.SetActive(false);
        SetObj(so, "_emptyText", empty);
    }

    //  격자 칸 템플릿 — 런타임에 Instantiate 된다. 프리팹에서는 꺼 둔다.
    static EquipPickSlotUI BuildPickTemplate(GameObject content)
    {
        var go = Go("PickTemplate", content, typeof(Image), typeof(Button));
        var frame = go.GetComponent<Image>();
        frame.color = new Color(0.24f, 0.25f, 0.34f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = frame;

        var slot = go.AddComponent<EquipPickSlotUI>();

        var pit = Go("Pit", go);
        var pitImg = pit.AddComponent<Image>();
        pitImg.color         = PitBg;
        pitImg.raycastTarget = false;
        var pRt = pit.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
        pRt.offsetMin = new Vector2(3f, 3f); pRt.offsetMax = new Vector2(-3f, -3f);

        var icon = Go("Icon", pit, typeof(Image)).GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        var iRt = icon.rectTransform;
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(8f, 8f); iRt.offsetMax = new Vector2(-8f, -8f);

        // 선택 표시 — 네 변 막대. 자식으로 두면 아이콘 위에 그려진다.
        var mark = Go("SelectMark", go);
        Stretch(mark);
        EdgeBar(mark, "T", 0f, 1f, 1f, 1f,  0f, -4f);
        EdgeBar(mark, "B", 0f, 0f, 1f, 0f,  4f,  0f);
        SideBar(mark, "L", 0f, 0f, 0f, 1f,  0f,  4f);
        SideBar(mark, "R", 1f, 0f, 1f, 1f, -4f,  0f);
        mark.SetActive(false);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_frame",      frame);
        SetObj(sSo, "_icon",       icon);
        SetObj(sSo, "_selectMark", mark);
        SetObj(sSo, "_btn",        btn);
        sSo.ApplyModifiedProperties();

        go.SetActive(false);
        return slot;
    }

    static void EdgeBar(GameObject parent, string name,
                        float aMinX, float aMinY, float aMaxX, float aMaxY,
                        float offMinY, float offMaxY)
    {
        var go = Go(name, parent, typeof(Image));
        var img = go.GetComponent<Image>();
        img.color         = SelectMarkC;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.offsetMin = new Vector2(0f, offMinY);
        rt.offsetMax = new Vector2(0f, offMaxY);
    }

    static void SideBar(GameObject parent, string name,
                        float aMinX, float aMinY, float aMaxX, float aMaxY,
                        float offMinX, float offMaxX)
    {
        var go = Go(name, parent, typeof(Image));
        var img = go.GetComponent<Image>();
        img.color         = SelectMarkC;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.offsetMin = new Vector2(offMinX, 0f);
        rt.offsetMax = new Vector2(offMaxX, 0f);
    }

    // ══════════════════════════════════════════════════════════
    //  공통 조각
    // ══════════════════════════════════════════════════════════

    //  섹션 구분선 — 가운데 글자 + 좌우 라인 (EventPopup 의 "선 택" 과 같은 형태)
    static void BuildDivider(GameObject panel, float y, string label)
    {
        var div = Go($"Divider_{label}", panel);
        AnchorTop(div, y, DivH, 40f);

        DivLine(div, "LineL", 0f,   0.5f,   0f, -80f);
        DivLine(div, "LineR", 0.5f, 1f,  80f,    0f);

        var tmp = TMP(div, "Label", label, UIScale.FontSm, FontStyles.Bold);
        tmp.color         = DividerLabel;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        Stretch(tmp.gameObject);
    }

    static void DivLine(GameObject parent, string name,
                        float aMinX, float aMaxX, float offMinX, float offMaxX)
    {
        var go = Go(name, parent, typeof(Image));
        var img = go.GetComponent<Image>();
        img.color         = DividerLine;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 0.5f);
        rt.anchorMax = new Vector2(aMaxX, 0.5f);
        rt.offsetMin = new Vector2(offMinX, -1f);
        rt.offsetMax = new Vector2(offMaxX,  1f);
    }

    static Sprite LoadStoneIcon()
        => AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/_project/3.Textures/Icons/Items/{eItem.EquipUpgradeStone.IconKey()}.png");

    static void CenterBox(GameObject go, float w, float h, float x)
        => EditorUIBuilder.Center(go.GetComponent<RectTransform>(), new Vector2(x, 0f), new Vector2(w, h));

    static void AnchorTop(GameObject go, float yFromTop, float height, float padH = 0f)
        => EditorUIBuilder.AnchorTop(go.GetComponent<RectTransform>(), yFromTop, height, padH);

    static void AnchorRight(GameObject go, float rightX, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(rightX, 0f);
        rt.sizeDelta        = new Vector2(w, h);
    }

    static void Center(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    // ── 잡 헬퍼 ──────────────────────────────────────────────

    static GameObject Go(string name, GameObject parent, params System.Type[] extra)
        => EditorUIBuilder.Go(name, parent, extra);

    static GameObject Panel(GameObject parent, string name, Color color)
        => EditorUIBuilder.Panel(parent, name, color);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void Stretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "EquipComparePopupCreator");

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "EquipComparePopupCreator");
}
#endif
