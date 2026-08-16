using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  CodexPopupCreator.cs
//  Tools > Project K > 프리팹 생성 > 팝업 > Codex
//
//  ┌──────────────────────────────────────────────────────────┐  ← 전체 화면
//  │ ◆ 도감   수집 84 / 439        공격력·체력 +42.0%    [ × ] │
//  ├──────────────────────────────────────────────────────────┤
//  │ [장비 3/20][어빌리티 12/46][특성 9/53][장수 60/320]        │
//  ├──────────────────────────────────────────────────────────┤
//  │  ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐                 │
//  │  │▨ │ │▨ │ │ ?│ │ ?│ │▨ │ │ ?│ │ ?│ │ ?│   (스크롤)      │
//  │  └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘                 │
//  └──────────────────────────────────────────────────────────┘
//
//  ⚠ 셀 구조는 Frame > Fill > (Icon, Name) 이다
//    CodexPopup 이 이 경로로 Find 한다. 이름을 바꾸면 색·아이콘이 통째로 안 채워진다.
//    Frame(테두리) 안에 Fill(안쪽)을 조금 작게 깔아 테두리를 만든다 —
//    UI 규칙 3 대로 반투명 테두리를 자식으로 얹지 않는다. 여기서는 Fill 이 불투명이라
//    Frame 이 가장자리로만 보이는 방식이라 렌더 순서 문제가 없다.
//
//  ⚠ 전체 화면이다
//    항목이 439칸이라 창으로 띄우면 스크롤만 길어진다. 캔버스를 꽉 채운다.
//
//  ⚠ ◆ 는 폰트에 있는 글자다 (UI 규칙 2 표에서 허용)
//    ★ ✔ ✕ ▶ 같은 건 □ 로 뜬다 — 닫기 X 는 EditorUIBuilder.XMark 로 그린다.
// ============================================================

public static class CodexPopupCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI/CodexPopup.prefab";

    const float HeaderH = 118f;
    const float TabH    = 96f;
    const float Pad     = 26f;

    // 격자 — 셀 폭은 가장 긴 장비 이름이 두 줄로 접히지 않는 크기
    const float CellW   = 176f;
    const float CellH   = 200f;   // 이름 + 직업·등급 두 줄이 들어간다
    const float CellGap = 16f;
    const float Border  = 3f;     // Frame 이 Fill 밖으로 드러나는 두께 = 테두리

    // 아랫줄(직업 · 등급). UI 규칙 4 — 폰트는 UIScale 상수에서만 파생시킨다.
    static readonly float SubFont = UIScale.FontSm * 0.82f;
    static readonly float SubH    = UIScale.Line(SubFont);

    static readonly Color CodexC   = new Color(0.27f, 0.87f, 0.80f, 1f);
    static readonly Color ScrimBg  = new Color(0.035f, 0.040f, 0.070f, 0.97f);
    static readonly Color GridBg   = new Color(0.055f, 0.062f, 0.105f, 1f);
    static readonly Color CloseC   = new Color(0.62f, 0.20f, 0.24f, 1f);
    static readonly Color CellFill = new Color(0.125f, 0.140f, 0.215f, 1f);
    static readonly Color CellEdge = new Color(0.32f, 0.36f, 0.52f, 1f);

    [MenuItem(ProjectKMenu.Popup + "Codex", priority = ProjectKMenu.PrefabPrio + 43)]
    public static void Run()
    {
        var root = new GameObject("CodexPopup", typeof(RectTransform));
        root.AddComponent<CanvasGroup>();
        var popup = root.AddComponent<CodexPopup>();

        // 전체 화면 — 부모(캔버스)에 꽉 채운다
        EditorUIBuilder.Stretch(root);

        var scrim = EditorUIBuilder.Img(root, "Scrim", ScrimBg);
        EditorUIBuilder.Stretch(scrim.gameObject);

        var progressTmp = default(TextMeshProUGUI);
        var bonusTmp    = default(TextMeshProUGUI);
        var closeBtn    = default(Button);
        BuildHeader(root, out progressTmp, out bonusTmp, out closeBtn);

        BuildTabs(root, out var tabButtons, out var tabLabels, out var tabBodies);
        var grid    = BuildGrid(root);
        var tooltip = BuildTooltip(root);

        var so = new SerializedObject(popup);
        EditorUIBuilder.SetEnum(so, "_popupType", (int)PopupType.Codex, Tag);
        EditorUIBuilder.SetObj(so, "_progressTmp",  progressTmp,  Tag);
        EditorUIBuilder.SetObj(so, "_bonusTmp",     bonusTmp,     Tag);
        EditorUIBuilder.SetObj(so, "_closeBtn",     closeBtn,     Tag);
        EditorUIBuilder.SetObj(so, "_grid",         grid,         Tag);
        EditorUIBuilder.SetObj(so, "_tooltip",      tooltip,      Tag);
        EditorUIBuilder.SetObjArray(so, "_tabButtons", tabButtons, Tag);
        EditorUIBuilder.SetObjArray(so, "_tabLabels",  tabLabels,  Tag);
        EditorUIBuilder.SetObjArray(so, "_tabBodies",  tabBodies,  Tag);
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, SavePath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CodexPopupCreator] 생성 완료 → " + SavePath +
                  "\nPopupManager 의 _prefabs 배열에 등록해야 열린다.");
    }

    const string Tag = "CodexPopupCreator";

    // ── 헤더 ─────────────────────────────────────────────────

    static void BuildHeader(GameObject root, out TextMeshProUGUI progress,
                            out TextMeshProUGUI bonus, out Button close)
    {
        var header = EditorUIBuilder.Img(root, "Header", EditorUIBuilder.Pop.HeaderBg).gameObject;
        {
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -HeaderH);
            rt.offsetMax = Vector2.zero;
        }

        // 아래 테두리 — 헤더와 탭을 눈으로 가른다
        var edge = EditorUIBuilder.Img(header, "BottomEdge", EditorUIBuilder.Pop.Divider);
        {
            var rt = edge.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0f, 3f);
        }
        edge.raycastTarget = false;

        // ◆ 마름모는 도형으로 그린다 (UI 규칙 2 — 장식 기호에 글리프를 쓰지 않는다)
        var dia = EditorUIBuilder.Diamond(header, "Diamond", 26f, CodexC);
        {
            var rt = dia.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(Pad + 6f, 0f);
        }

        var title = EditorUIBuilder.TMP(header, "Title", "도감", UIScale.FontLg, FontStyles.Bold);
        title.color     = CodexC;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        {
            var rt = title.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(Pad + 44f, 0f);
            rt.sizeDelta        = new Vector2(280f, UIScale.RowLg);
        }

        progress = EditorUIBuilder.TMP(header, "Progress", "수집 0 / 0", UIScale.FontMd, FontStyles.Bold);
        progress.color     = EditorUIBuilder.Pop.SubText;
        progress.alignment = TextAlignmentOptions.MidlineLeft;
        {
            var rt = progress.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(Pad + 340f, 0f);
            rt.sizeDelta        = new Vector2(420f, UIScale.RowMd);
        }

        bonus = EditorUIBuilder.TMP(header, "Bonus", "공격력·체력 +0.0%", UIScale.FontMd, FontStyles.Bold);
        bonus.color     = EditorUIBuilder.Pop.SubText;
        bonus.alignment = TextAlignmentOptions.MidlineRight;
        {
            var rt = bonus.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-(Pad + 130f), 0f);
            rt.sizeDelta        = new Vector2(560f, UIScale.RowMd);
        }

        // UI 규칙 1 — 누를 수 있는 버튼은 음각. 라벨은 body 아래에 넣는다.
        close = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseC, out var closeBody);
        {
            var rt = close.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-Pad, 4f);
            rt.sizeDelta        = new Vector2(88f, 78f);
        }
        var x = EditorUIBuilder.XMark(closeBody, "X", 34f, Color.white);
        {
            var rt = x.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    // ── 탭 ───────────────────────────────────────────────────

    static void BuildTabs(GameObject root, out Object[] buttons, out Object[] labels, out Object[] bodies)
    {
        var bar = EditorUIBuilder.Go("TabBar", root);
        {
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(Pad, -(HeaderH + TabH));
            rt.offsetMax = new Vector2(-Pad, -HeaderH);
        }

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = true;  hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(0, 0, 12, 10);

        // 배열 순서 = CodexCategory 순서. CodexPopup 이 인덱스로 매핑한다.
        var names  = new[] { "장비", "어빌리티", "특성", "장수" };
        var btnArr = new Object[names.Length];
        var lblArr = new Object[names.Length];
        var bodArr = new Object[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            var go  = EditorUIBuilder.Go($"Tab{i}", bar);
            var btn = EditorUIBuilder.RaisedBtnOn(go, EditorUIBuilder.Pop.TabInactive, out var body);

            var tmp = EditorUIBuilder.TMP(body, "Label", $"{names[i]}  <size=80%>0/0</size>",
                                          UIScale.FontMd, FontStyles.Bold);
            tmp.alignment = TextAlignmentOptions.Center;
            EditorUIBuilder.Stretch(tmp.gameObject);

            btnArr[i] = btn;
            lblArr[i] = tmp;
            bodArr[i] = body.GetComponent<Image>();
        }

        buttons = btnArr;
        labels  = lblArr;
        bodies  = bodArr;
    }

    // ── 격자 ─────────────────────────────────────────────────

    static RecycleGridScroll BuildGrid(GameObject root)
    {
        var box = EditorUIBuilder.Img(root, "GridBox", GridBg).gameObject;
        {
            var rt = box.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(Pad, Pad);
            rt.offsetMax = new Vector2(-Pad, -(HeaderH + TabH + 6f));
        }
        box.AddComponent<RectMask2D>();

        var scroll = box.AddComponent<ScrollRect>();
        scroll.horizontal   = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 46f;

        var content = EditorUIBuilder.Go("Content", box);
        {
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        // ⚠ GridLayoutGroup·ContentSizeFitter 를 붙이지 않는다
        //   위치와 Content 높이는 RecycleGridScroll 이 직접 계산한다.
        //   레이아웃 그룹이 같이 있으면 매 프레임 서로 값을 덮어써 칸이 떨린다.
        scroll.viewport = box.GetComponent<RectTransform>();
        scroll.content  = content.GetComponent<RectTransform>();

        var cellTemplate = BuildCell(content);

        var grid = box.AddComponent<RecycleGridScroll>();
        var gso  = new SerializedObject(grid);
        EditorUIBuilder.SetObj(gso, "_scroll",       scroll,                          Tag);
        EditorUIBuilder.SetObj(gso, "_viewport",     box.GetComponent<RectTransform>(), Tag);
        EditorUIBuilder.SetObj(gso, "_content",      content.GetComponent<RectTransform>(), Tag);
        EditorUIBuilder.SetObj(gso, "_cellTemplate", cellTemplate,                    Tag);
        gso.FindProperty("_cellSize").vector2Value = new Vector2(CellW, CellH);
        gso.FindProperty("_spacing").vector2Value  = new Vector2(CellGap, CellGap);
        gso.FindProperty("_padLeft").floatValue    = 18f;
        gso.FindProperty("_padRight").floatValue   = 18f;
        gso.FindProperty("_padTop").floatValue     = 18f;
        gso.FindProperty("_padBottom").floatValue  = 18f;
        gso.ApplyModifiedProperties();

        return grid;
    }

    // 셀 — Frame(테두리) > Fill(안쪽) > Icon + Name.
    // Fill 을 Border 만큼 안쪽으로 넣어 Frame 이 가장자리로만 드러나게 한다.
    static GameObject BuildCell(GameObject parent)
    {
        var cell = EditorUIBuilder.Go("CellTemplate", parent);
        var btn  = cell.AddComponent<Button>();

        var frame = EditorUIBuilder.Img(cell, "Frame", CellEdge);
        EditorUIBuilder.Stretch(frame.gameObject);
        btn.targetGraphic = frame;
        // 누름 표시는 테두리 밝기로 준다 (targetGraphic 색에 곱해진다 — UI 규칙 1)
        EditorUIBuilder.TintTransition(cell, CellEdge);

        var fill = EditorUIBuilder.Img(frame.gameObject, "Fill", CellFill);
        {
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(Border, Border);
            rt.offsetMax = new Vector2(-Border, -Border);
        }
        fill.raycastTarget = false;

        var icon = EditorUIBuilder.Img(fill.gameObject, "Icon", Color.white);
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        {
            var rt = icon.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -10f);
            rt.sizeDelta        = new Vector2(86f, 86f);
        }

        // 이름 — 아래에서 두 번째 줄
        var name = EditorUIBuilder.TMP(fill.gameObject, "Name", "?", UIScale.FontSm, FontStyles.Bold);
        name.alignment     = TextAlignmentOptions.Top;
        name.raycastTarget = false;
        // 이름이 길면 줄여서 담는다 — 두 줄이 되면 칸 높이를 넘긴다 (UI 규칙 5)
        name.enableAutoSizing = true;
        name.fontSizeMin      = UIScale.FontSm * 0.60f;
        name.fontSizeMax      = UIScale.FontSm;
        {
            float h = UIScale.Line(UIScale.FontSm);
            var rt = name.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(6f, SubH + 8f);
            rt.offsetMax = new Vector2(-6f, SubH + 8f + h);
        }

        // 직업 · 등급 — 맨 아랫줄. 이름보다 작고 흐리게 둬야 이름이 먼저 읽힌다.
        var sub = EditorUIBuilder.TMP(fill.gameObject, "Sub", "", SubFont, FontStyles.Normal);
        sub.alignment     = TextAlignmentOptions.Top;
        sub.raycastTarget = false;
        sub.enableAutoSizing = true;
        sub.fontSizeMin      = SubFont * 0.62f;
        sub.fontSizeMax      = SubFont;
        {
            var rt = sub.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(6f, 6f);
            rt.offsetMax = new Vector2(-6f, 6f + SubH);
        }

        cell.SetActive(false);
        return cell;
    }

    // ── 정보 툴팁 ────────────────────────────────────────────
    //  장비·어빌리티·특성을 눌렀을 때 뜬다. 장수는 HeroDetailPopup 을 쓴다.
    //
    //  ⚠ 직접 만들지 말고 InfoTooltipBuilder 를 쓴다
    //    특성 아이콘·보상 카드와 같은 모양·같은 동작이어야 한다.
    //    예전에 여기서 따로 만들었더니 앵커 규칙이 달라 툴팁이 엉뚱한 곳에 떴다.
    //
    //  ⚠ 여기서는 팝업 루트에 하나만 만든다
    //    칸이 400개라 칸마다 붙일 수 없다. 실제 위치는 런타임에
    //    InfoTooltipUI.ShowFrom(누른 칸) 이 다시 잡는다.
    static InfoTooltipUI BuildTooltip(GameObject root)
        => InfoTooltipBuilder.Build(root, 420f);
}
