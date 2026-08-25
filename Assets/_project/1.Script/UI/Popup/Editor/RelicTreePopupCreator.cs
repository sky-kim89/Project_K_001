using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RelicTreePopupCreator.cs
//  Tools > Project K > 프리팹 생성 > 팝업 > RelicTree
//
//  저장: Assets/_project/2.Prefabs/UI/RelicTreePopup.prefab
//
//  ■ 구 RelicPopup(카드 그리드)을 대체한다
//    PopupType 은 그대로 Relic 이다 — 부르는 쪽(MainPanelUI·StageSelectUI)은
//    타입으로 열기 때문에 두 프리팹이 같이 있으면 어느 쪽이 열릴지 알 수 없다.
//    구 RelicPopup.prefab 은 반드시 지울 것.
//
//  ■ 화면
//    Header  H=136   ◆ 유물 전승도    [찍은 노드 · 투자]    [보유 pt]  [닫기]
//    Body            트리 캔버스 (드래그 이동 · 휠/핀치 확대)
//      └ Tooltip     좌하단 — 노드를 누르면 뜬다
//    Footer  H=100   트리 초기화 (좌)                        환생 (우, 조건 미달이면 숨김)
//
//  ⚠ 자식 이름을 바꾸지 말 것 — RelicTreePopup 이 이름으로 찾는다
//    노드: "Face" / "Face/Icon" / "Name" / "Pips" / "BuyBtn" / "BuyBtn/Body/Cost"
//
//  ⚠ 노드 카드는 그리드 한 칸(RelicTreePopup.Spacing=200)보다 좁아야 한다
//    가로로 붙은 노드끼리 겹치면 강화 버튼이 옆 노드에 가린다.
//    세로는 넘쳐도 된다 — 표의 좌표 규칙상 같은 x 면 두 칸 이상 떨어져 있다.
// ============================================================

public static class RelicTreePopupCreator
{
    const string SavePath  = "Assets/_project/2.Prefabs/UI/RelicTreePopup.prefab";
    const string ReincIcon = "Assets/_project/3.Textures/Icons/Items/item_reincarnation_point.png";

    // ── 치수 ─────────────────────────────────────────────────
    const float HeaderH = 136f;
    const float FooterH = 100f;
    const float SidePad =  32f;

    // 노드 카드 — 가로는 RelicTreePopup.Spacing(280) 미만이어야 한다
    //
    // ⚠ 아이콘은 판(Face)을 거의 꽉 채운다
    //   예전엔 104 판에 52 아이콘이라 그림이 판 한가운데 점처럼 떠 있었다.
    //   트리는 축소해서 보는 화면이라, 판 대비 그림이 작으면 무엇인지 안 읽힌다.
    //
    // ⚠ 여기 숫자는 화면에 그대로 나오는 크기가 아니다 (2026-08-25 확대)
    //   트리는 _zoomInit 배율로 열린다. 예전엔 84 아이콘 × 0.50 = 실효 42px,
    //   강화 버튼 140×72 × 0.50 = 70×36 으로 터치 최소 크기에도 못 미쳤다.
    //   카드 치수와 Spacing·_zoomInit 을 같이 올려야 체감이 바뀐다.
    //   지금: 아이콘 140 × 0.65 = 실효 91px, 버튼 196×104 → 127×68.
    const float NodeW   = 264f;
    const float NodeH   = 392f;
    const float FaceSz  = 168f;
    const float IconSz  = 140f;

    // ── 색 ───────────────────────────────────────────────────
    static readonly Color PanelBg    = new Color(0.055f, 0.060f, 0.105f, 1f);
    static readonly Color HeaderBg   = new Color(0.060f, 0.140f, 0.160f, 1f);
    static readonly Color CanvasBg   = new Color(0.035f, 0.040f, 0.075f, 1f);
    static readonly Color FooterBg   = new Color(0.075f, 0.080f, 0.135f, 1f);
    static readonly Color TagColor   = new Color(0.62f,  0.94f,  0.96f,  1f);
    static readonly Color TitleColor = new Color(1.00f,  0.94f,  0.86f,  1f);
    static readonly Color SubColor   = new Color(0.62f,  0.66f,  0.76f,  1f);
    static readonly Color PointColor = new Color(0.62f,  0.94f,  0.96f,  1f);

    static readonly Color TipBg      = new Color(0.085f, 0.095f, 0.160f, 0.96f);
    static readonly Color TipBorder  = new Color(0.26f,  0.34f,  0.52f,  1f);
    static readonly Color NodeFaceBg = new Color(0.10f,  0.11f,  0.17f,  1f);
    static readonly Color EdgeColor  = new Color(0.22f,  0.24f,  0.34f,  1f);
    static readonly Color GhostColor = new Color(0.30f,  0.34f,  0.42f,  1f);

    // 닫기는 팝업 공통색이다 — 이 팝업만 다르면 같은 자리에 있어도 다른 버튼으로 읽힌다
    static readonly Color CloseBtnC  = new Color(0.50f,  0.14f,  0.14f,  1f);
    static readonly Color ResetBtnC  = new Color(0.44f,  0.16f,  0.16f,  1f);
    static readonly Color ReincBtnC  = new Color(0.16f,  0.44f,  0.42f,  1f);
    static readonly Color BuyBtnC    = new Color(0.14f,  0.20f,  0.30f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "RelicTree", priority = ProjectKMenu.PrefabPrio + 44)]
    public static void Create()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);

        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RelicTreePopupCreator] 저장: {SavePath}" +
                  "\nPopupManager 인스펙터의 'Load Popup Prefabs From Folder' 로 등록할 것.");
    }

    // ══════════════════════════════════════════════════════════
    //  빌더
    // ══════════════════════════════════════════════════════════

    static GameObject Build(GameObject parent)
    {
        var panel = EditorUIBuilder.Go("RelicTreePopup", parent);
        EditorUIBuilder.Stretch(panel);

        // 전체 화면을 불투명하게 덮는다 — 아래 MainPanel 은 켜진 채로 남는다.
        // raycastTarget 이 살아 있어야 RelicTreePopup 의 IDragHandler 가 드래그를 받는다.
        panel.AddComponent<Image>().color = PanelBg;
        panel.AddComponent<CanvasGroup>();

        var ui = panel.AddComponent<RelicTreePopup>();
        var so = new SerializedObject(ui);
        EditorUIBuilder.SetEnum(so, "_popupType", (int)PopupType.Relic, "RelicTreePopupCreator");

        BuildHeader(panel, so);
        BuildCanvas(panel, so);
        BuildTooltip(panel, so);
        BuildFooter(panel, so);

        so.ApplyModifiedProperties();
        return panel;
    }

    // ── 헤더 ─────────────────────────────────────────────────

    static void BuildHeader(GameObject panel, SerializedObject so)
    {
        var header = EditorUIBuilder.Panel(panel, "HeaderBar", HeaderBg);
        header.transform.SetParent(panel.transform, false);
        EditorUIBuilder.AnchorTop(header.GetComponent<RectTransform>(), 0f, HeaderH);

        // ◆ 태그 — 글리프 대신 도형 (UI 규칙 2)
        var tag = EditorUIBuilder.Diamond(header, "Tag", 22f, TagColor);
        var tagRt = tag.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 0.5f);
        tagRt.pivot     = new Vector2(0.5f, 0.5f);
        tagRt.anchoredPosition = new Vector2(SidePad + 14f, 0f);

        // ⚠ 조작 안내(드래그·휠·핀치)를 헤더에 적지 않는다
        //   한 번 읽고 나면 매번 자리만 차지한다. 같은 내용은 도움말 튜토리얼
        //   (RelicHelpTutorial)이 들고 있다 — 필요할 때만 꺼내 보면 된다.
        var title = EditorUIBuilder.TMP(header, "Title", "유물 전승도", UIScale.FontLg, FontStyles.Bold, false);
        title.color     = TitleColor;
        title.alignment = TextAlignmentOptions.Left;
        var tRt = title.rectTransform;
        tRt.anchorMin = tRt.anchorMax = new Vector2(0f, 0.5f);
        tRt.pivot     = new Vector2(0f, 0.5f);
        tRt.anchoredPosition = new Vector2(SidePad + 36f, 0f);
        tRt.sizeDelta        = new Vector2(560f, UIScale.RowLg);

        // 진행 요약 — 헤더 한가운데. 포인트 옆에 붙이면 두 수치가 한 덩어리로 읽힌다.
        var summary = EditorUIBuilder.TMP(header, "SummaryText", "찍은 노드 0 / 0", UIScale.FontMd,
                                          FontStyles.Normal);
        summary.color = SubColor;
        var sRt = summary.rectTransform;
        sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
        sRt.pivot     = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = Vector2.zero;
        sRt.sizeDelta        = new Vector2(760f, UIScale.RowMd);
        EditorUIBuilder.SetObj(so, "_summaryText", summary, "RelicTreePopupCreator");

        // 보유 포인트 — 아이콘 + 숫자
        // ⚠ 이름을 바꾸지 말 것 — 튜토리얼(FirstRelic·HelpRelic)이 "PointGroup" 으로 찾는다
        var purse = EditorUIBuilder.Go("PointGroup", header);
        var pRt = purse.GetComponent<RectTransform>();
        pRt.anchorMin = pRt.anchorMax = new Vector2(1f, 0.5f);
        pRt.pivot     = new Vector2(1f, 0.5f);
        pRt.anchoredPosition = new Vector2(-(SidePad + 120f), 0f);
        pRt.sizeDelta        = new Vector2(320f, 64f);

        var icon = EditorUIBuilder.Img(purse, "PtIcon", Color.white);
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ReincIcon);
        if (icon.sprite == null) icon.color = PointColor;
        var iRt = icon.rectTransform;
        iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot     = new Vector2(0f, 0.5f);
        iRt.anchoredPosition = new Vector2(0f, 0f);
        iRt.sizeDelta        = new Vector2(UIScale.IconSm, UIScale.IconSm);

        var pts = EditorUIBuilder.TMP(purse, "PointText", "0", UIScale.FontLg, FontStyles.Bold, false);
        pts.color     = PointColor;
        pts.alignment = TextAlignmentOptions.Left;
        var ptRt = pts.rectTransform;
        ptRt.anchorMin = ptRt.anchorMax = new Vector2(0f, 0.5f);
        ptRt.pivot     = new Vector2(0f, 0.5f);
        ptRt.anchoredPosition = new Vector2(UIScale.IconSm + 12f, 0f);
        ptRt.sizeDelta        = new Vector2(240f, UIScale.RowLg);
        EditorUIBuilder.SetObj(so, "_pointText", pts, "RelicTreePopupCreator");

        // 닫기
        var close = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseBtnC, out var closeBody);
        var cRt = close.GetComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(1f, 0.5f);
        cRt.pivot     = new Vector2(1f, 0.5f);
        cRt.anchoredPosition = new Vector2(-SidePad, 0f);
        cRt.sizeDelta        = new Vector2(76f, 76f);
        EditorUIBuilder.XMark(closeBody, "X", 30f, Color.white);
        EditorUIBuilder.SetObj(so, "_closeBtn", close, "RelicTreePopupCreator");
    }

    // ── 트리 캔버스 ──────────────────────────────────────────

    static void BuildCanvas(GameObject panel, SerializedObject so)
    {
        var viewport = EditorUIBuilder.Panel(panel, "Viewport", CanvasBg);
        viewport.transform.SetParent(panel.transform, false);
        var vRt = viewport.GetComponent<RectTransform>();
        vRt.anchorMin = Vector2.zero;
        vRt.anchorMax = Vector2.one;
        vRt.offsetMin = new Vector2(0f, FooterH);
        vRt.offsetMax = new Vector2(0f, -HeaderH);
        viewport.AddComponent<RectMask2D>();
        EditorUIBuilder.SetObj(so, "_viewport", vRt, "RelicTreePopupCreator");

        // 실제로 움직이고 확대되는 판. 앵커·피벗이 중앙이라 뿌리(0,0)가 화면 중앙에 온다.
        var content = EditorUIBuilder.Go("Content", viewport);
        var cRt = content.GetComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot     = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = Vector2.zero;
        cRt.sizeDelta        = new Vector2(4000f, 3000f);   // 런타임에 표를 보고 다시 잡는다
        EditorUIBuilder.SetObj(so, "_content", cRt, "RelicTreePopupCreator");

        // ⚠ 원본 세 개는 Content 의 첫 자식들이다 (비활성)
        //   런타임이 Instantiate(원본, _content) 로 복제하므로 여기 있어야
        //   앵커·피벗이 그대로 따라간다.
        EditorUIBuilder.SetObj(so, "_edgeTemplate",  BuildEdgeTemplate(content),  "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_nodeTemplate",  BuildNodeTemplate(content),  "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_ghostTemplate", BuildGhostTemplate(content), "RelicTreePopupCreator");
    }

    static GameObject BuildEdgeTemplate(GameObject content)
    {
        var edge = EditorUIBuilder.Panel(content, "EdgeTemplate", EdgeColor);
        edge.transform.SetParent(content.transform, false);
        var rt = edge.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(100f, 9f);   // 길이는 런타임이 다시 잡는다 (두께만 의미 있다)
        edge.GetComponent<Image>().raycastTarget = false;
        edge.SetActive(false);
        return edge;
    }

    static GameObject BuildNodeTemplate(GameObject content)
    {
        var node = EditorUIBuilder.Go("NodeTemplate", content);
        var nRt = node.GetComponent<RectTransform>();
        nRt.anchorMin = nRt.anchorMax = new Vector2(0.5f, 0.5f);
        nRt.pivot     = new Vector2(0.5f, 0.5f);
        nRt.sizeDelta = new Vector2(NodeW, NodeH);

        // 아이콘 판 — 이 자체가 버튼이라 누르면 툴팁이 뜬다
        var face = EditorUIBuilder.Panel(node, "Face", NodeFaceBg);
        face.transform.SetParent(node.transform, false);
        var fRt = face.GetComponent<RectTransform>();
        fRt.anchorMin = fRt.anchorMax = new Vector2(0.5f, 0.5f);
        fRt.pivot     = new Vector2(0.5f, 0.5f);
        fRt.anchoredPosition = new Vector2(0f, RelicTreePopup.FaceOffsetY);
        fRt.sizeDelta        = new Vector2(FaceSz, FaceSz);
        face.AddComponent<Button>().targetGraphic = face.GetComponent<Image>();

        // ⚠ 아이콘은 Face 의 자식이지만 회전은 따라가지 않는다
        //   특수 노드는 Face 를 45° 돌려 마름모로 만든다 — 아이콘까지 같이 돌면
        //   그림이 기울어 보인다. RelicTreePopup 이 아이콘 회전을 0 으로 되돌린다.
        var icon = EditorUIBuilder.Img(face, "Icon", Color.white);
        var icRt = icon.rectTransform;
        icRt.anchorMin = icRt.anchorMax = new Vector2(0.5f, 0.5f);
        icRt.pivot     = new Vector2(0.5f, 0.5f);
        icRt.anchoredPosition = Vector2.zero;
        icRt.sizeDelta        = new Vector2(IconSz, IconSz);
        icon.raycastTarget = false;

        var name = EditorUIBuilder.TMP(node, "Name", "노드", UIScale.FontMd, FontStyles.Bold);
        name.raycastTarget = false;
        var nmRt = name.rectTransform;
        nmRt.anchorMin = nmRt.anchorMax = new Vector2(0.5f, 0.5f);
        nmRt.pivot     = new Vector2(0.5f, 0.5f);
        nmRt.anchoredPosition = new Vector2(0f, -14f);
        nmRt.sizeDelta        = new Vector2(NodeW, UIScale.RowMd * 2f);

        // 레벨 칩 — 최대 레벨이 5라 5개를 만들고 런타임이 필요한 만큼만 켠다
        var pips = EditorUIBuilder.Go("Pips", node);
        var pRt = pips.GetComponent<RectTransform>();
        pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f);
        pRt.pivot     = new Vector2(0.5f, 0.5f);
        pRt.anchoredPosition = new Vector2(0f, -88f);
        pRt.sizeDelta        = new Vector2(NodeW, 20f);
        var hlg = pips.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.spacing             = 5f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth   = false;
        hlg.childControlHeight  = false;
        for (int i = 0; i < 5; i++)
        {
            var pip = EditorUIBuilder.Img(pips, $"Pip{i}", new Color(1f, 1f, 1f, 0.18f));
            pip.raycastTarget = false;
            pip.rectTransform.sizeDelta = new Vector2(36f, 14f);
        }

        // 강화 버튼 — 아이콘 아래 (요청 사양: ▲ + 필요 포인트)
        var buyRoot = EditorUIBuilder.Go("BuyBtn", node);
        var bRt = buyRoot.GetComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.pivot     = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(0f, -168f);
        bRt.sizeDelta        = new Vector2(196f, 104f);
        EditorUIBuilder.RaisedBtnOn(buyRoot, BuyBtnC, out var buyBody);

        // 라벨·아이콘은 반드시 Body 아래 (UI 규칙 1) — 루트에 넣으면 눌려도 안 내려간다
        var arrow = EditorUIBuilder.TriangleUp(buyBody, "Arrow", 30f, new Color(1f, 0.86f, 0.40f, 1f));
        arrow.GetComponent<RectTransform>().anchoredPosition = new Vector2(-56f, 0f);

        var cost = EditorUIBuilder.TMP(buyBody, "Cost", "0", UIScale.FontMd, FontStyles.Bold);
        cost.color         = new Color(1f, 0.86f, 0.40f, 1f);
        cost.raycastTarget = false;
        var coRt = cost.rectTransform;
        coRt.anchorMin = coRt.anchorMax = new Vector2(0.5f, 0.5f);
        coRt.pivot     = new Vector2(0.5f, 0.5f);
        coRt.anchoredPosition = new Vector2(20f, 0f);
        coRt.sizeDelta        = new Vector2(120f, UIScale.RowMd);

        node.SetActive(false);
        return node;
    }

    static GameObject BuildGhostTemplate(GameObject content)
    {
        var ghost = EditorUIBuilder.Go("GhostTemplate", content);
        var rt = ghost.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;   // 실제 자리는 런타임(IconPosOf)이 잡는다
        rt.sizeDelta        = new Vector2(104f, 104f);

        var box = EditorUIBuilder.Img(ghost, "Box", new Color(0.10f, 0.11f, 0.17f, 1f));
        EditorUIBuilder.Stretch(box.gameObject);
        box.raycastTarget = false;
        box.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var q = EditorUIBuilder.TMP(ghost, "Q", "?", UIScale.FontLg, FontStyles.Bold);
        q.color         = GhostColor;
        q.raycastTarget = false;
        EditorUIBuilder.Stretch(q.gameObject);

        ghost.SetActive(false);
        return ghost;
    }

    // ── 툴팁 ─────────────────────────────────────────────────

    static void BuildTooltip(GameObject panel, SerializedObject so)
    {
        // ⚠ Viewport 의 자식이 아니라 형제다
        //   Viewport 는 RectMask2D 로 잘리고 Content 와 함께 확대된다.
        //   툴팁이 그 안에 있으면 줌을 당길 때 같이 커지고 가장자리에서 잘린다.
        //
        // ⚠ 껍데기(root) → 테두리 → 본체 순서다 (UI 규칙 3)
        //   Unity UI 는 자기 Graphic 을 먼저 그리고 자식을 그린다. 테두리를 본체의
        //   자식으로 두면 SetAsFirstSibling 을 해도 본체 위에 얹힌다.
        //   그래서 Image 가 없는 껍데기를 하나 두고 그 안에 테두리·본체를 형제로 넣는다.
        //   껍데기를 켜고 끄면 둘이 같이 움직인다.
        var root = EditorUIBuilder.Go("Tooltip", panel);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0f, 0f);
        rootRt.pivot     = new Vector2(0f, 0f);
        rootRt.anchoredPosition = new Vector2(SidePad, FooterH + 24f);
        // ⚠ 높이를 줄이지 말 것 — 효과가 세 줄까지 나온다
        //   300 이었을 때 '무쌍'·'일기당천' 같은 다중 스탯 노드의 마지막 줄이
        //   상자 밖으로 잘려 나갔다. 이름+선행+효과 3줄+비용이 들어갈 높이다.
        rootRt.sizeDelta        = new Vector2(760f, 380f);

        var border = EditorUIBuilder.Img(root, "Border", TipBorder);
        border.raycastTarget = false;
        var brt = border.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(-3f, -3f);
        brt.offsetMax = new Vector2( 3f,  3f);

        var tip = EditorUIBuilder.Panel(root, "Body", TipBg);
        EditorUIBuilder.Stretch(tip);

        var name = EditorUIBuilder.TMP(tip, "TipName", "노드 이름", UIScale.FontLg, FontStyles.Bold, false);
        name.alignment = TextAlignmentOptions.Left;
        Place(name.rectTransform, 20f, 20f, 720f, UIScale.RowLg);

        var sub = EditorUIBuilder.TMP(tip, "TipSub", "티어 · 레벨 · 선행", UIScale.FontSm,
                                      FontStyles.Normal, false);
        sub.color     = SubColor;
        sub.alignment = TextAlignmentOptions.Left;
        Place(sub.rectTransform, 20f, 20f + UIScale.RowLg + 4f, 720f, UIScale.RowSm);

        // 효과 칸은 남는 세로를 전부 먹는다 (고정 높이 금지 — 상자 높이를 바꾸면 같이 따라온다)
        var eff = EditorUIBuilder.TMP(tip, "TipEffect", "효과", UIScale.FontMd, FontStyles.Normal, false);
        eff.alignment        = TextAlignmentOptions.TopLeft;
        eff.textWrappingMode = TextWrappingModes.Normal;
        var eRt = eff.rectTransform;
        eRt.anchorMin = Vector2.zero; eRt.anchorMax = Vector2.one;
        eRt.offsetMin = new Vector2(20f, 18f + UIScale.RowSm + 12f);
        eRt.offsetMax = new Vector2(-20f, -(20f + UIScale.RowLg + UIScale.RowSm + 14f));

        var cost = EditorUIBuilder.TMP(tip, "TipCost", "다음 레벨 0pt", UIScale.FontSm,
                                       FontStyles.Bold, false);
        cost.color     = PointColor;
        cost.alignment = TextAlignmentOptions.Left;
        var crt = cost.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0f, 0f);
        crt.pivot     = new Vector2(0f, 0f);
        crt.anchoredPosition = new Vector2(20f, 18f);
        crt.sizeDelta        = new Vector2(720f, UIScale.RowSm);

        EditorUIBuilder.SetObj(so, "_tooltipRoot", root, "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_tipName",     name, "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_tipSub",      sub,  "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_tipEffect",   eff,  "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_tipCost",     cost, "RelicTreePopupCreator");

        root.SetActive(false);
    }

    static void Place(RectTransform rt, float left, float top, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(left, -top);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // ── 푸터 ─────────────────────────────────────────────────

    static void BuildFooter(GameObject panel, SerializedObject so)
    {
        var footer = EditorUIBuilder.Panel(panel, "FooterBar", FooterBg);
        footer.transform.SetParent(panel.transform, false);
        var fRt = footer.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f);
        fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot     = new Vector2(0.5f, 0f);
        fRt.anchoredPosition = Vector2.zero;
        fRt.sizeDelta        = new Vector2(0f, FooterH);

        var reset = EditorUIBuilder.RaisedTextBtn(footer, "ResetBtn", "트리 초기화",
                                                  UIScale.FontMd, ResetBtnC);
        var rRt = reset.GetComponent<RectTransform>();
        rRt.anchorMin = rRt.anchorMax = new Vector2(0f, 0.5f);
        rRt.pivot     = new Vector2(0f, 0.5f);
        rRt.anchoredPosition = new Vector2(SidePad, 0f);
        rRt.sizeDelta        = new Vector2(360f, UIScale.BtnFor(UIScale.FontMd));
        EditorUIBuilder.SetObj(so, "_resetBtn", reset, "RelicTreePopupCreator");

        var reinc = EditorUIBuilder.RaisedBtn(footer, "ReincarnateBtn", ReincBtnC, out var reincBody);
        var reRt = reinc.GetComponent<RectTransform>();
        reRt.anchorMin = reRt.anchorMax = new Vector2(1f, 0.5f);
        reRt.pivot     = new Vector2(1f, 0.5f);
        reRt.anchoredPosition = new Vector2(-SidePad, 0f);
        reRt.sizeDelta        = new Vector2(560f, UIScale.BtnFor(UIScale.FontMd));

        var label = EditorUIBuilder.TMP(reincBody, "ReincLabel", "환생", UIScale.FontMd, FontStyles.Bold);
        label.raycastTarget = false;
        EditorUIBuilder.Stretch(label.gameObject);
        EditorUIBuilder.SetObj(so, "_reincarnateBtn", reinc, "RelicTreePopupCreator");
        EditorUIBuilder.SetObj(so, "_reincLabel",     label, "RelicTreePopupCreator");
    }
}
