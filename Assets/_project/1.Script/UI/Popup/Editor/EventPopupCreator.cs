using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EventPopupCreator.cs
//  Tools > Project K > 프리팹 생성 > 팝업 > Event
//
//  ⚠ 캔버스 기준
//    이 팝업은 StageSelectUI 에서만 열리므로 항상 **로비 캔버스** 위에 뜬다.
//    로비 캔버스는 가로 1920×1080 기준 (InGame 캔버스만 세로 1080×1920).
//    세로 여유가 1080 뿐이라 패널 높이를 고정하면 잘린다.
//    → 패널은 세로 스트레치(위아래 40 여백)로 캔버스에 맞추고,
//      ChoiceRoot 가 남는 높이를 흡수한다. 위쪽 섹션만 고정 높이.
//
//  ⚠ 테두리는 Panel 의 자식이면 안 된다
//    Unity UI 는 자기 Graphic 을 먼저 그리고 그 다음 자식을 그린다.
//    자식에 SetAsFirstSibling() 을 해도 부모 Image 보다 뒤로 갈 수 없다.
//    반투명 테두리를 자식으로 두면 팝업 전체가 그 색으로 물든다.
//    → Border 는 root 아래 Panel 의 "앞 형제"로 둔다.
//
//  ⚠ 장식 기호에 폰트 글리프를 쓰지 않는다
//    기본 폰트에 ★ ✔ ✕ ▶ ◀ ▲ ⚙ 가 없어 □(두부)로 렌더된다.
//    EditorUIBuilder.Diamond / CheckMark / Chevron 으로 그린다.
//
//  ── 레이아웃 (W=880 고정, H=캔버스높이-80 / 1080 캔버스 기준 H=1000) ──
//  Header        Y=  0  H=136  다이아 태그 + 타이틀(그림자 포함)
//  AccentLine    Y=136  H=  3
//  Illustration  Y=150  H=200  삽화 832×200 원본 비율 + 하단 페이드
//  BodyPanel     Y=362  H=190  본문 카드 (좌측 강조바 + 여백)
//  ChoiceDivider Y=562  H= 36  좌우 라인 + 가운데 "선 택"
//  ChoiceRoot    Y=606  → 하단 24까지 (가변). 1080 캔버스에서 370 확보
//    ChoiceButtonTemplate H=112
//      Shadow(하단 6px 노출) → Body → TopEdge/BottomEdge/Accent/Label/Hint/Arrow
//      ※ 그림자 위에 본체를 얹어 "떠 있는 버튼" 으로 읽히게 한다
//      ※ 3개 × 112 + 간격 2 × 12 = 360 → 370 안에 들어간다
//  ResultPanel (hidden)  BodyY 부터 확인 버튼 위까지 덮는다.
//    헤더와 삽화는 남겨서 "어떤 이벤트의 결과인지" 맥락을 유지한다.
//    내부는 세로 흐름: [결  과 구분선] → [보상 카드 줄] → [결과 문구]
//    문구가 남는 높이를 전부 가져가고, 보상이 없으면 그 줄이 통째로 빠진다.
//    (예전엔 전부 절대 좌표였고 문구 칸이 58px 라 보상 카드와 글자가 겹쳤다)
//  ConfirmButton (hidden) 하단 28
// ============================================================

public static class EventPopupCreator
{
    const string SaveDir = "Assets/_project/2.Prefabs/UI";

    // ── 색상 팔레트 ──────────────────────────────────────────────
    //  가독성 기준: 본문/라벨은 배경과 명도차를 크게, 보조정보만 채도로 구분.
    static readonly Color BgOverlay      = new Color(0f,    0f,    0f,    0.78f);
    static readonly Color PanelBg        = new Color(0.07f, 0.075f, 0.13f, 1f);
    static readonly Color PanelBorder    = new Color(0.46f, 0.24f, 0.72f, 1f);
    static readonly Color HeaderBg       = new Color(0.13f, 0.08f, 0.22f, 1f);
    static readonly Color AccentPurple   = new Color(0.58f, 0.26f, 0.86f, 1f);
    static readonly Color TagColor       = new Color(0.80f, 0.60f, 1.00f, 1f);
    static readonly Color TitleColor     = new Color(1.00f, 0.90f, 0.62f, 1f);
    static readonly Color TitleShadow    = new Color(0.04f, 0.02f, 0.08f, 0.85f);
    static readonly Color IllustBg       = new Color(0.10f, 0.07f, 0.16f, 1f);

    static readonly Color BodyBg         = new Color(0.105f, 0.115f, 0.185f, 1f);
    static readonly Color BodyBarColor   = new Color(0.58f, 0.26f, 0.86f, 0.85f);
    static readonly Color BodyTextColor  = new Color(0.92f, 0.92f, 0.97f, 1f);

    static readonly Color DividerLine    = new Color(0.30f, 0.26f, 0.44f, 0.85f);
    static readonly Color DividerLabel   = new Color(0.66f, 0.64f, 0.82f, 1f);

    static readonly Color ChoiceBg       = new Color(0.175f, 0.195f, 0.315f, 1f);
    static readonly Color ChoiceLabel    = new Color(0.98f, 0.98f, 1.00f, 1f);
    static readonly Color ArrowColor     = new Color(0.70f, 0.66f, 0.90f, 1f);
    static readonly Color HintGreen      = new Color(0.50f, 0.94f, 0.66f, 1f);

    static readonly Color ResultBg       = new Color(0.055f, 0.06f, 0.115f, 1f);
    static readonly Color ResultAccent   = new Color(0.34f, 0.86f, 0.52f, 1f);
    static readonly Color ResultBadgeBg  = new Color(0.34f, 0.86f, 0.52f, 0.16f);
    static readonly Color ResultLabelC   = new Color(0.56f, 0.72f, 0.62f, 1f);
    static readonly Color ResultTextC    = new Color(0.94f, 0.97f, 0.92f, 1f);

    static readonly Color ConfirmGreen   = new Color(0.16f, 0.58f, 0.33f, 1f);

    // ── 치수 ─────────────────────────────────────────────────────
    const float PW        = 880f;
    const float PVMargin  = 40f;    // 캔버스 위·아래 여백 (패널 높이를 결정)
    const float SidePad   = 48f;    // 콘텐츠 좌우 여백 → 실제 폭 832 (= 삽화 PNG 폭)
    const float HeaderH   = 136f;
    const float IllustY   = 150f;
    const float IllustH   = 200f;   // evt_*.png 원본 높이
    const float BodyY     = 362f;
    const float BodyH     = 190f;
    const float DivY      = 562f;
    const float DivH      = 36f;
    const float ChoiceY   = 606f;
    const float ChoiceBtm = 24f;    // ChoiceRoot 하단 여백 (높이는 가변)

    const float BtnH      = 112f;   // 선택지 버튼 — FontMd 라벨 + FontSm 힌트 2줄
    const float BtnGap    = 12f;
    const float BtnLift   = 6f;     // 그림자 노출 폭 = 떠 있는 정도
    const float RewardRowH = 128f;  // RewardCard 한 변

    [MenuItem(ProjectKMenu.Popup + "Event", priority = ProjectKMenu.PrefabPrio + 42)]
    public static void Create()
    {
        // ── 루트 (전체화면 오버레이) ──────────────────────────────
        var root = new GameObject("EventPopup", typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = BgOverlay;
        FullStretch(root);
        var popup = root.AddComponent<EventPopup>();

        // ── 테두리 (Panel 의 앞 형제 — 자식으로 두면 팝업을 덮는다) ──
        var border = MakeGo("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        StretchV(border.GetComponent<RectTransform>(), PW + 6f, PVMargin - 3f);

        // ── 패널 ─────────────────────────────────────────────────
        var panel = MakeGo("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        StretchV(panel.GetComponent<RectTransform>(), PW, PVMargin);

        // ══ 헤더 ══════════════════════════════════════════════════
        var header = MakeGo("Header", panel);
        header.AddComponent<Image>().color = HeaderBg;
        AnchorTopH(header, 0f, HeaderH);

        // 도움말 — 이 팝업엔 닫기 버튼이 없다 (선택지로만 닫힌다).
        // 그래서 닫기 자리를 비켜 줄 필요 없이 오른쪽 끝에 그대로 붙인다.
        EditorUIBuilder.InfoBtn(header, TutorialId.HelpEvent, 0f, -24f, gap: 0f);

        // "이벤트" 태그 — ★ 는 폰트에 없으므로 마름모 도형으로 대체
        var tagRoot = MakeGo("EventTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin        = new Vector2(0f, 1f);
        tagRt.anchorMax        = new Vector2(0f, 1f);
        tagRt.pivot            = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(26f, -14f);
        tagRt.sizeDelta        = new Vector2(300f, 40f);

        var tagDiamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var tdRt = tagDiamond.GetComponent<RectTransform>();
        tdRt.anchorMin = new Vector2(0f, 0.5f);
        tdRt.anchorMax = new Vector2(0f, 0.5f);
        tdRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = AddTMP(tagRoot, "Label", "이 벤 트", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(28f, 0f); tlRt.offsetMax = Vector2.zero;

        // 타이틀 — 그림자 사본을 먼저 깔아 어떤 배경에서도 읽히게 한다
        var titleShadow = AddTMP(header, "TitleShadow", "이벤트 제목", UIScale.FontLg, FontStyles.Bold);
        titleShadow.color         = TitleShadow;
        titleShadow.alignment     = TextAlignmentOptions.Center;
        titleShadow.raycastTarget = false;
        SetTitleRect(titleShadow.rectTransform, 3f);

        var titleTmp = AddTMP(header, "TitleText", "이벤트 제목", UIScale.FontLg, FontStyles.Bold);
        titleTmp.color         = TitleColor;
        titleTmp.alignment     = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;
        SetTitleRect(titleTmp.rectTransform, 0f);

        var accentLine = MakeGo("AccentLine", panel);
        accentLine.AddComponent<Image>().color = AccentPurple;
        AnchorTopH(accentLine, HeaderH, 3f);

        // ══ 삽화 ══════════════════════════════════════════════════
        var illustGo = MakeGo("Illustration", panel);
        var illustImg = illustGo.AddComponent<Image>();
        illustImg.color          = IllustBg;
        illustImg.preserveAspect = true;   // 832×200 PNG 가 눌리지 않도록
        illustImg.raycastTarget  = false;
        AnchorTopH(illustGo, IllustY, IllustH, padH: SidePad);

        var illustFade = MakeGo("BottomFade", illustGo);
        var fadeImg = illustFade.AddComponent<Image>();
        fadeImg.color         = new Color(PanelBg.r, PanelBg.g, PanelBg.b, 0.55f);
        fadeImg.raycastTarget = false;
        var fRt = illustFade.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0f, 0f);
        fRt.anchorMax = new Vector2(1f, 0f);
        fRt.pivot     = new Vector2(0.5f, 0f);
        fRt.anchoredPosition = Vector2.zero;
        fRt.sizeDelta        = new Vector2(0f, 28f);

        // ══ 본문 카드 ══════════════════════════════════════════════
        var bodyPanel = MakeGo("BodyPanel", panel);
        bodyPanel.AddComponent<Image>().color = BodyBg;
        AnchorTopH(bodyPanel, BodyY, BodyH, padH: SidePad);

        var bodyBar = MakeGo("AccentBar", bodyPanel);
        var barImg = bodyBar.AddComponent<Image>();
        barImg.color         = BodyBarColor;
        barImg.raycastTarget = false;
        var bbRt = bodyBar.GetComponent<RectTransform>();
        bbRt.anchorMin = new Vector2(0f, 0f); bbRt.anchorMax = new Vector2(0f, 1f);
        bbRt.offsetMin = Vector2.zero;        bbRt.offsetMax = new Vector2(6f, 0f);

        var bodyTmp = AddTMP(bodyPanel, "BodyText", "이벤트 본문입니다.",
                             UIScale.FontMd, FontStyles.Normal);
        bodyTmp.color              = BodyTextColor;
        bodyTmp.alignment          = TextAlignmentOptions.TopLeft;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.overflowMode       = TextOverflowModes.Truncate;
        bodyTmp.lineSpacing        = 16f;    // 줄간격이 좁으면 한글 본문이 뭉친다
        bodyTmp.raycastTarget      = false;
        var btRt = bodyTmp.rectTransform;
        btRt.anchorMin = Vector2.zero;  btRt.anchorMax = Vector2.one;
        btRt.offsetMin = new Vector2(30f, 18f);
        btRt.offsetMax = new Vector2(-24f, -18f);

        // ══ "선 택" 구분선 ════════════════════════════════════════
        var divider = MakeGo("ChoiceDivider", panel);
        AnchorTopH(divider, DivY, DivH, padH: SidePad);
        DividerLine_(divider, "LineL", 0f, 0.5f, 0f, -66f);
        DividerLine_(divider, "LineR", 0.5f, 1f, 66f, 0f);

        var divLabel = AddTMP(divider, "Label", "선  택", UIScale.FontSm, FontStyles.Bold);
        divLabel.color         = DividerLabel;
        divLabel.alignment     = TextAlignmentOptions.Center;
        divLabel.raycastTarget = false;
        FullStretch(divLabel.gameObject);

        // ══ 선택지 ════════════════════════════════════════════════
        var choiceRoot = MakeGo("ChoiceRoot", panel);
        AnchorTopToBottom(choiceRoot, ChoiceY, ChoiceBtm, padH: SidePad);
        var vlg = choiceRoot.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = BtnGap;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childControlWidth      = true;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandHeight = false;
        vlg.padding                = new RectOffset(0, 0, 2, 0);

        var choiceBtn = BuildChoiceButton(choiceRoot);
        choiceBtn.SetActive(false);

        // ══ 결과 오버레이 (숨김) ═══════════════════════════════════
        var resultPanel = BuildResultPanel(panel, out var resultTmp, out var rewardArea);
        resultPanel.SetActive(false);

        // ══ 확인 버튼 (숨김) ═══════════════════════════════════════
        var confirmBtn = EditorUIBuilder
            .RaisedTextBtn(panel, "ConfirmButton", "확  인", UIScale.FontLg, ConfirmGreen)
            .gameObject;
        var conRt = confirmBtn.GetComponent<RectTransform>();
        conRt.anchorMin        = new Vector2(0.5f, 0f);
        conRt.anchorMax        = new Vector2(0.5f, 0f);
        conRt.pivot            = new Vector2(0.5f, 0f);
        conRt.anchoredPosition = new Vector2(0f, 28f);
        conRt.sizeDelta        = new Vector2(480f, UIScale.BtnMd);
        confirmBtn.SetActive(false);

        // ── 직렬화 필드 연결 ─────────────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",            (int)PopupType.Event);
        SetObj (so, "_illustration",         illustImg);
        SetObj (so, "_titleTmp",             titleTmp);
        SetObj (so, "_titleShadowTmp",       titleShadow);
        SetObj (so, "_bodyTmp",              bodyTmp);
        SetObj (so, "_choiceRoot",           choiceRoot.GetComponent<RectTransform>());
        SetObj (so, "_choiceButtonTemplate", choiceBtn.GetComponent<Button>());
        SetObj (so, "_choiceDivider",        divider);
        SetObj (so, "_resultPanel",          resultPanel);
        SetObj (so, "_resultTmp",            resultTmp);
        SetObj (so, "_confirmBtn",           confirmBtn.GetComponent<Button>());
        SetObj (so, "_rewardArea",           rewardArea.GetComponent<RectTransform>());

        // 전투 결과 팝업과 같은 카드 프리팹을 쓴다 (UISetupTool 이 먼저 만들어 둔다)
        var rewardCard = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_project/2.Prefabs/UI/RewardCard.prefab");
        if (rewardCard != null)
            SetObj(so, "_rewardCardPrefab", rewardCard.GetComponent<RewardCardUI>());
        else
            Debug.LogWarning("[EventPopupCreator] RewardCard.prefab 이 없습니다 — " +
                             "Tools > Project K > 프리팹 생성 > 인게임 > RewardCard 를 먼저 실행하세요.");

        so.ApplyModifiedProperties();

        Save(root, "EventPopup");
    }

    // ══════════════════════════════════════════════════════════
    //  결과 오버레이
    // ══════════════════════════════════════════════════════════
    //  본문 카드 위치부터 확인 버튼 위까지만 덮는다.
    //  헤더·삽화를 남겨두면 "무슨 이벤트의 결과"인지 맥락이 유지되고,
    //  결과창만 덩그러니 뜬 미완성 느낌이 사라진다.
    //
    //    TopLine   상단 강조선 (초록)
    //    Badge     마름모 배경 + 체크 도형   ← ✔ 글리프는 폰트에 없다
    //    Label     "결과"
    //    Divider   가는 가로선
    //    Text      결과 문구

    static GameObject BuildResultPanel(GameObject panel, out TextMeshProUGUI resultTmp,
                                       out GameObject rewardArea)
    {
        var rp = MakeGo("ResultPanel", panel);
        rp.AddComponent<Image>().color = ResultBg;   // 불투명 — 반투명이면 본문이 비쳐 지저분하다
        var rpRt = rp.GetComponent<RectTransform>();
        rpRt.anchorMin = Vector2.zero; rpRt.anchorMax = Vector2.one;
        rpRt.offsetMin = new Vector2(0f,  UIScale.BtnMd + 44f);
        rpRt.offsetMax = new Vector2(0f, -BodyY);

        // 상단 강조선
        var topLine = MakeGo("TopLine", rp);
        var tlImg = topLine.AddComponent<Image>();
        tlImg.color         = ResultAccent;
        tlImg.raycastTarget = false;
        AnchorTopH(topLine, 0f, 3f);

        // ── 세로 흐름 배치 ───────────────────────────────────
        //  ⚠ 예전에는 전부 절대 좌표였다. 결과 문구 칸을 58px 로 잡아 놓고
        //    3~4줄짜리 문구를 Overflow 로 흘려 보내는 바람에, 그 아래에
        //    고정 배치된 보상 카드와 글자가 겹쳤다.
        //    이제는 [결과 구분선] → [보상 카드] → [문구] 순서로 쌓고,
        //    문구가 남는 높이를 전부 가져간다. 보상이 없으면 그 줄이 통째로
        //    빠지면서 문구가 그만큼 더 넓게 쓴다.
        var content = MakeGo("Content", rp);
        {
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, -3f);   // 상단 강조선 아래부터
        }
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding                = new RectOffset((int)SidePad, (int)SidePad, 20, 20);
        vlg.spacing                = 14f;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ① "결  과" 구분선 — 선택지의 "선  택" 과 같은 형태
        var divider = MakeGo("ResultDivider", content);
        AddLE(divider, 0f, DivH);
        DividerLine_(divider, "LineL", 0f,   0.5f,   0f, -66f);
        DividerLine_(divider, "LineR", 0.5f, 1f,  66f,    0f);

        var divLabel = AddTMP(divider, "Label", "결  과", UIScale.FontSm, FontStyles.Bold);
        divLabel.color         = ResultAccent;
        divLabel.alignment     = TextAlignmentOptions.Center;
        divLabel.raycastTarget = false;
        FullStretch(divLabel.gameObject);

        // ② 획득 보상 카드 줄 — 전투 결과 팝업과 같은 RewardCard
        rewardArea = MakeGo("RewardArea", content);
        AddLE(rewardArea, 0f, RewardRowH);

        var rHlg = rewardArea.AddComponent<HorizontalLayoutGroup>();
        rHlg.childAlignment         = TextAnchor.MiddleCenter;
        rHlg.spacing                = 12f;
        rHlg.childControlWidth      = true;
        rHlg.childControlHeight     = true;
        rHlg.childForceExpandWidth  = false;
        rHlg.childForceExpandHeight = false;
        rewardArea.SetActive(false);

        // ③ 결과 문구 — 남는 높이를 전부 가져간다
        resultTmp = AddTMP(content, "ResultText", "결과 텍스트", UIScale.FontMd, FontStyles.Normal);
        resultTmp.color              = ResultTextC;
        resultTmp.alignment          = TextAlignmentOptions.Top;
        resultTmp.enableWordWrapping = true;
        resultTmp.overflowMode       = TextOverflowModes.Overflow;
        resultTmp.lineSpacing        = 12f;
        resultTmp.raycastTarget      = false;
        {
            // preferredHeight 를 0 으로 둬야 "남는 높이 전부"가 된다.
            // 비워 두면 TMP 가 글자 길이만큼 요구해 패널 밖으로 밀려난다.
            var le = resultTmp.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 0f;
            le.flexibleHeight  = 1f;
            le.minHeight       = UIScale.Line(UIScale.FontMd) * 2f;
        }

        return rp;
    }

    // ══════════════════════════════════════════════════════════
    //  선택지 버튼 — "눌리는" 입체 버튼
    // ══════════════════════════════════════════════════════════
    //  ChoiceButtonTemplate (Button)
    //  ├─ Shadow    전체를 채움. 아래 BtnLift 만큼이 Body 밖으로 노출된다
    //  └─ Body      상단 정렬, 높이 = H - BtnLift   ← Button.targetGraphic
    //      ├─ TopEdge     상단 2px 밝은 선  (빛 받는 면)
    //      ├─ BottomEdge  하단 4px 어두운 선 (음각)
    //      ├─ Accent      좌측 7px 보라 바
    //      ├─ LabelText / HintText
    //      └─ Arrow       우측 꺾쇠 도형

    static GameObject BuildChoiceButton(GameObject parent)
    {
        var go = MakeGo("ChoiceButtonTemplate", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(PW - SidePad, BtnH);

        // 템플릿 루트가 곧 버튼이어야 하므로 RaisedBtnOn 을 쓴다.
        // (RaisedBtn 은 루트를 새로 만들어 자식 경로가 한 단계 깊어진다)
        EditorUIBuilder.RaisedBtnOn(go, ChoiceBg, out var body, BtnLift);

        var accent = MakeGo("Accent", body);
        var accImg = accent.AddComponent<Image>();
        accImg.color         = AccentPurple;
        accImg.raycastTarget = false;
        var aRt = accent.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 0f); aRt.anchorMax = new Vector2(0f, 1f);
        aRt.offsetMin = new Vector2(0f, 2f); aRt.offsetMax = new Vector2(7f, -2f);

        // 선택지 문구 (상단 56%)
        var label = AddTMP(body, "LabelText", "선택지", UIScale.FontMd, FontStyles.Bold);
        label.color         = ChoiceLabel;
        label.alignment     = TextAlignmentOptions.BottomLeft;
        label.raycastTarget = false;
        var lRt = label.rectTransform;
        lRt.anchorMin = new Vector2(0f, 0.44f); lRt.anchorMax = new Vector2(1f, 1f);
        lRt.offsetMin = new Vector2(28f, 0f);   lRt.offsetMax = new Vector2(-66f, -8f);

        // 비용·보상 힌트 (하단 44%)
        var hint = AddTMP(body, "HintText", "", UIScale.FontSm, FontStyles.Normal);
        hint.color              = HintGreen;
        hint.alignment          = TextAlignmentOptions.TopLeft;
        hint.enableWordWrapping = false;
        hint.overflowMode       = TextOverflowModes.Ellipsis;
        hint.raycastTarget      = false;
        var hRt = hint.rectTransform;
        hRt.anchorMin = new Vector2(0f, 0f);  hRt.anchorMax = new Vector2(1f, 0.44f);
        hRt.offsetMin = new Vector2(28f, 8f); hRt.offsetMax = new Vector2(-66f, 0f);

        // 우측 꺾쇠 — 누를 수 있다는 신호.
        // › 글리프도 있긴 하지만 크기·굵기를 맞출 수 없어 도형으로 그린다.
        var arrow = EditorUIBuilder.Chevron(body, "Arrow", 30f, 0f, ArrowColor);
        var arRt = arrow.GetComponent<RectTransform>();
        arRt.anchorMin        = new Vector2(1f, 0.5f);
        arRt.anchorMax        = new Vector2(1f, 0.5f);
        arRt.pivot            = new Vector2(1f, 0.5f);
        arRt.anchoredPosition = new Vector2(-26f, 0f);

        return go;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────

    static GameObject MakeGo(string name, GameObject parent)
        => EditorUIBuilder.Go(name, parent);

    // 폭 고정 + 세로 스트레치 (위아래 vMargin 여백)
    static void StretchV(RectTransform rt, float width, float vMargin)
    {
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(width, -vMargin * 2f);
    }

    static void AnchorTopH(GameObject go, float yFromTop, float height, float padH = 0f)
        => EditorUIBuilder.AnchorTop(go.GetComponent<RectTransform>(), yFromTop, height, padH);

    // 상단 yFromTop 부터 하단 bottomMargin 까지 채우는 가변 높이 배치
    static void AnchorTopToBottom(GameObject go, float yFromTop, float bottomMargin, float padH = 0f)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(padH * 0.5f, bottomMargin);
        rt.offsetMax = new Vector2(-padH * 0.5f, -yFromTop);
    }

    static void DividerLine_(GameObject parent, string name,
                             float aMinX, float aMaxX, float offMinX, float offMaxX)
    {
        var go = MakeGo(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = DividerLine;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 0.5f);
        rt.anchorMax = new Vector2(aMaxX, 0.5f);
        rt.offsetMin = new Vector2(offMinX, -1f);
        rt.offsetMax = new Vector2(offMaxX,  1f);
    }

    // 타이틀 위치 — 태그 아래 영역의 수직 중앙. dy 만큼 내리면 그림자 사본.
    static void SetTitleRect(RectTransform rt, float dy)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(26f + dy, 10f - dy);
        rt.offsetMax = new Vector2(-26f + dy, -54f - dy);
    }

    static void AddLE(GameObject go, float w, float h) => EditorUIBuilder.LE(go, w, h);

    static void FullStretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text,
                                  float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "EventPopupCreator");

    static void SetObj(SerializedObject so, string field, UnityEngine.Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "EventPopupCreator");

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SaveDir}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[EventPopupCreator] 저장: {path}");
    }
}
