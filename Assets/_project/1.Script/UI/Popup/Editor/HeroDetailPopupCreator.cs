#if UNITY_EDITOR
using System.IO;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroDetailPopupCreator.cs  [Editor Only]
//  Tools > Project K > 프리팹 생성 > 팝업 > HeroDetail
//
//  ⚠ 캔버스 기준: 로비 1920×1080 (가로). 세로 여유는 1080 뿐이다.
//
//  ■ 왜 다시 짰나 (이전 레이아웃의 문제)
//    · 폭 576px 패널이 화면 좌측(-600)에 치우쳐 있어 우측이 통째로 빘다.
//    · 스탯·장비·스킬이 탭으로 나뉘어 한 번에 하나만 볼 수 있었다.
//      장수를 고르는 판단은 "이 스탯에 이 스킬"을 같이 봐야 서는데 탭을 오가야 했다.
//    · 스탯 값 TMP 가 Ellipsis + 칸 높이 52(FontMd 한 줄 53보다 낮음)이라
//      분해 문자열이 통째로 "..." 으로 바뀌어 숫자가 사라졌다.
//
//  ■ 새 레이아웃 (전체 화면 1840×1000, 3단)
//    Header  H=136   ◆ 장 수 | 이름(동적)      [재화 4종]           [X]
//    Body    H=814   Left 580 | Mid 460 | Right 720   (간격 20)
//      Left   초상화 400 + 장비 3칸(144 세로 스트립, 아이콘만)
//             초상화 아래 [등급업][해고] / Lv · 직업 · 등급
//             / (하단) EXP 바 + 레벨업·용병 버튼
//
//    ⚠ 초상화(400) 아래 빈 자리에 대해
//      장비 스트립은 168×3+8×2 = 520 이라 초상화보다 120 길다.
//      그래서 x 12~412 / y 412~552 가 통째로 비어 있었고, 여기에 등급업·해고 행을 넣었다.
//      RankRowH 를 키우면 정보 행(infoY)과 겹치므로 반드시 다시 계산할 것.
//      Mid    "스 탯" + [장 수 | 용 병] 토글 + 9행 — 전부 노출.
//             행을 누르면 출처별 분해. 용병 탭은 배율을 곱한 값 + 3행 숨김
//      Right  "스 킬" + 액티브 1 + 패시브 3 — 설명을 FontMd 로 크게
//
//    "업" 버튼(레벨·용병·장비 강화)에는 ▲ 대신 EditorUIBuilder.TriangleUp 도형을 쓴다
//    — ▲ 글리프는 기본 폰트에 없어 □ 로 렌더된다 (UI 규칙 2).
//
//    EquipComparePopup 은 x=+540(스킬 열 중심)에 뜨도록 맞춰 놨다 — 그 열 위에 겹친다.
//    (EquipComparePopupCreator 의 anchoredPosition 과 짝을 이룬다)
// ============================================================

public static class HeroDetailPopupCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/HeroDetailPopup.prefab";

    // ── 치수 ─────────────────────────────────────────────────
    const float PW      = 1840f;
    const float PH      = 1000f;
    const float SidePad = 40f;     // 좌우 총 여백 → 콘텐츠 1800

    const float HeaderH = 136f;
    const float BodyTop = 156f;
    const float BodyBtm = 30f;
    const float ColGap  = 20f;

    const float LeftW  = 580f;
    const float MidW   = 460f;
    const float RightW = 720f;     // 580 + 460 + 720 + 20×2 = 1800 ✓

    const float BodyH  = PH - BodyTop - BodyBtm;   // 814

    // 좌측 열 내부
    const float PortraitSize = 400f;
    const float EquipStripW  = 144f;   // 12 + 400 + 12 + 144 + 12 = 580 = LeftW ✓
    const float EquipTileH   = 168f;   // 아이콘 104 + 강화 버튼 58 + 간격 6
    const float EquipTileGap = 8f;     // 168×3 + 8×2 = 520
    const float DivH         = 36f;

    // 초상화(400) 아래 ~ 정보 행(infoY) 사이의 빈 높이 = 520 + 20 - 400 = 140.
    // 등급업·해고 행이 이 안에 들어간다.
    const float RankRowGap   = EquipTileH * 3f + EquipTileGap * 2f + 20f - PortraitSize;

    static readonly float InfoRowH = UIScale.RowMd;              // 53
    static readonly float BtnH     = UIScale.BtnFor(UIScale.FontMd);  // 72
    static readonly float StatTabH = UIScale.BtnFor(UIScale.FontMd);  // 72 — 장수/용병 토글

    // ── 스탯 목록 높이 배분 ───────────────────────────────────
    //
    //  ⚠ 행 높이를 손으로 적지 않는다
    //    예전엔 StatRowH = 72f 고정에 "행 수를 바꾸면 다시 계산할 것" 이라는
    //    주석만 있었다. 9행 × 72 = 648 로 목록 영역(664)을 거의 꽉 채워 두어
    //    스탯을 하나만 더 넣어도 아래가 잘렸다.
    //    이제 행 수만 고치면 높이가 따라온다.
    //
    //  ⚠ 최소치는 UIScale.RowMd(53) 다 — FontMd 한 줄 (UI 규칙 5)
    //    이 아래로 내려가면 값 글자 아래가 잘린다. 행을 더 늘리려면
    //    목록 영역 자체를 키우거나 폰트를 내려야 한다.
    const  int   StatRowCount = 11;    // 체력·공격·방어율·용병 수·이속·공속
                                       // ·사거리·지휘력·쿨타임·치명확률·치명피해
    static readonly float StatListTop = 12f + DivH + 10f + StatTabH + 10f;   // 140
    static readonly float StatListH   = BodyH - StatListTop - 10f;           // 664
    static readonly float StatRowH    = Mathf.Floor(StatListH / StatRowCount);
    // 스킬 열 세로 배분 (BodyH 814 안에서)
    //   구분선 12..48 / 액티브 60..300 / 패시브 320..802
    //   패시브 3칸 + 간격 12×2 = 482  → 한 칸 152
    // ⚠ ActiveH 를 키우면 패시브 칸이 줄어든다. 둘의 합을 반드시 다시 계산할 것.
    const float ActiveBoxH  = 240f;
    const float PassiveBoxH = 152f;

    /// <summary>"업(상승)" 표시 세모 크기 — ▲ 글리프는 폰트에 없어 도형으로 그린다.</summary>
    const float UpMarkSize = 26f;

    // 헤더 재화 위젯 — 4종 × 176 = 704. 이름(900)과 닫기(116) 사이에 들어간다.
    const float CurrencyW    = 176f;
    const float CurrencyIcon = 40f;
    static readonly float CurrencyH = UIScale.RowMd;   // 53

    // ── 색상 ─────────────────────────────────────────────────
    static readonly Color BgOverlay    = new Color(0f,     0f,     0f,     0.80f);
    static readonly Color PanelBg      = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder  = new Color(0.26f,  0.44f,  0.72f,  1f);
    static readonly Color HeaderBg     = new Color(0.08f,  0.10f,  0.18f,  1f);
    static readonly Color AccentBlue   = new Color(0.40f,  0.72f,  1.00f,  1f);
    static readonly Color TagColor     = new Color(0.62f,  0.82f,  1.00f,  1f);
    static readonly Color TitleColor   = new Color(1.00f,  0.94f,  0.78f,  1f);
    static readonly Color TitleShadow  = new Color(0.02f,  0.03f,  0.06f,  0.85f);

    static readonly Color ColumnBg     = new Color(0.095f, 0.10f,  0.165f, 1f);
    static readonly Color PitBg        = new Color(0.055f, 0.06f,  0.10f,  1f);
    static readonly Color DividerLine  = new Color(0.26f,  0.28f,  0.40f,  0.85f);
    static readonly Color DividerLabel = new Color(0.70f,  0.74f,  0.88f,  1f);
    static readonly Color LabelColor   = new Color(0.60f,  0.62f,  0.74f,  1f);
    static readonly Color RowLine      = new Color(0.18f,  0.19f,  0.28f,  1f);

    static readonly Color SkillBoxBg   = new Color(0.115f, 0.125f, 0.215f, 1f);
    static readonly Color ActiveAccent = new Color(0.45f,  0.65f,  1.00f,  1f);
    static readonly Color PassAccent   = new Color(0.60f,  0.44f,  0.90f,  1f);
    // 이름 = 채도 높은 강조색(액티브 금 / 패시브 보라), 설명 = 채도 낮은 청회색.
    // 종류(금↔보라)와 역할(이름↔설명)이 색만 보고도 갈린다.
    // 예전엔 액티브 설명(0.80,0.82,0.92)과 패시브 이름(0.92,0.88,1.00)이 거의 같은 색이었다.
    static readonly Color ActNameC     = new Color(1.00f,  0.88f,  0.52f,  1f);   // 금
    static readonly Color ActDescC     = new Color(0.66f,  0.72f,  0.86f,  1f);   // 청회
    static readonly Color PassNameC    = new Color(0.80f,  0.66f,  1.00f,  1f);   // 보라
    static readonly Color PassDescC    = new Color(0.60f,  0.63f,  0.76f,  1f);   // 어두운 청회

    static readonly Color LevelUpBtnC  = new Color(0.16f,  0.34f,  0.62f,  1f);
    static readonly Color SoldierBtnC  = new Color(0.14f,  0.34f,  0.22f,  1f);
    static readonly Color GradeUpBtnC  = new Color(0.44f,  0.30f,  0.10f,  1f);   // 금빛 — 등급 상승
    static readonly Color FireBtnC     = new Color(0.36f,  0.14f,  0.16f,  1f);   // 붉은 — 되돌릴 수 없는 조작
    static readonly Color CloseBtnC    = new Color(0.50f,  0.14f,  0.14f,  1f);
    static readonly Color EnhanceBtnC  = new Color(0.22f,  0.30f,  0.52f,  1f);
    static readonly Color UpMarkC      = Color.white;
    static readonly Color TabActiveC   = new Color(0.42f,  0.74f,  1.00f,  1f);
    static readonly Color TabIdleC     = new Color(0.58f,  0.60f,  0.72f,  1f);
    // 탭 바탕 — 아래 스탯 목록(ColumnBg)보다 밝게 잡아 "여기부터 목록"이 구분되게 한다
    static readonly Color TabFaceOn    = new Color(0.20f,  0.38f,  0.62f,  1f);
    static readonly Color TabFaceOff   = new Color(0.15f,  0.16f,  0.25f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "HeroDetail", priority = ProjectKMenu.PrefabPrio + 38)]
    public static void Create()
    {
        Directory.CreateDirectory("Assets/_project/2.Prefabs/UI");
        AssetDatabase.Refresh();

        var go = BuildPopup();
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log($"[HeroDetailPopupCreator] 저장: {PrefabPath} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    static GameObject BuildPopup()
    {
        // 루트 = 전체 화면 어둡게
        var root = CreatePanel(null, "HeroDetailPopup", BgOverlay);
        Stretch(root);
        var popup = root.AddComponent<HeroDetailPopup>();
        var so    = new SerializedObject(popup);
        SetEnum(so, "_popupType", (int)PopupType.HeroDetail);

        // 테두리는 Panel 의 앞 형제 — 자식으로 두면 팝업 전체를 덮는다
        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        CenterBox(border, PW + 6f, PH + 6f);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        CenterBox(panel, PW, PH);

        BuildHeader(panel, so);

        float x = SidePad * 0.5f;
        BuildLeftColumn (panel, so, x);                       x += LeftW + ColGap;
        BuildStatColumn (panel, so, x);                       x += MidW  + ColGap;
        BuildSkillColumn(panel, so, x);

        so.ApplyModifiedProperties();
        return root;
    }

    // ══════════════════════════════════════════════════════════
    //  헤더 — ◆ 장 수 태그 + 이름(동적) + 닫기
    // ══════════════════════════════════════════════════════════

    static void BuildHeader(GameObject panel, SerializedObject so)
    {
        var header = Go("Header", panel);
        header.AddComponent<Image>().color = HeaderBg;
        AnchorTop(header, 0f, HeaderH);

        // ★ 는 폰트에 없다 (□ 로 렌더됨) → 마름모 도형으로 대체
        var tagRoot = Go("HeroTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(300f, 34f);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "장 수", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        // 이름 — 그림자 사본을 먼저 깔아 어떤 배경에서도 읽히게 한다
        // ⚠ 그림자도 반드시 연결한다. 안 하면 프리팹 플레이스홀더("영웅 이름")가
        //   실제 이름 옆에 검은 글씨로 그대로 보인다.
        var shadowTmp = MakeTitle(header, "NameShadow", TitleShadow, 3f);
        var nameTmp   = MakeTitle(header, "NameText",   TitleColor,  0f);
        SetObj(so, "_nameText",       nameTmp);
        SetObj(so, "_nameShadowText", shadowTmp);

        var accent = Go("AccentLine", panel);
        accent.AddComponent<Image>().color = AccentBlue;
        AnchorTop(accent, HeaderH, 3f);

        // 닫기
        var closeBtn = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseBtnC, out var body);
        AnchorRight(closeBtn.gameObject, -24f, 76f, 76f);
        Center(EditorUIBuilder.XMark(body, "Mark", UIScale.FontMd, Color.white));
        SetObj(so, "_closeBtn", closeBtn);

        // 도움말 — 닫기 버튼 왼쪽
        EditorUIBuilder.InfoBtn(header, TutorialId.HelpHeroDetail, 76f, -24f);

        BuildCurrencyBar(header);
    }

    // ── 헤더 재화 바 (이름 오른쪽 ~ 닫기 버튼 왼쪽) ───────────
    //  이 팝업에서 쓰는 재화만 올린다: 레벨업(골드) · 등급업(장군 강화석)
    //  · 장비 강화(장비 강화석) · 용병 수(용병 조각).
    //  로비 TopBar 와 같은 CurrencyWidget 을 써서 값이 자동으로 갱신된다.

    static void BuildCurrencyBar(GameObject header)
    {
        // 닫기 + 도움말 묶음을 피해 그 왼쪽에 붙인다
        float CloseW = EditorUIBuilder.HeaderRightBlock(76f, 24f) + 16f;

        var bar = Go("CurrencyBar", header);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-CloseW, 0f);
        rt.sizeDelta        = new Vector2(CurrencyW * 4f, CurrencyH);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleRight;
        hlg.spacing                = 8f;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        BuildCurrencyWidget(bar, eItem.Gold);
        BuildCurrencyWidget(bar, eItem.GeneralUpgradeStone);
        BuildCurrencyWidget(bar, eItem.EquipUpgradeStone);
        BuildCurrencyWidget(bar, eItem.SoldierShard);
    }

    static void BuildCurrencyWidget(GameObject parent, eItem item)
    {
        var container = Go($"CW_{item}", parent);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.spacing                = 6f;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(6, 6, 0, 0);

        var cle = container.AddComponent<LayoutElement>();
        cle.preferredWidth = CurrencyW;
        cle.minWidth       = CurrencyW;
        cle.flexibleWidth  = 0f;

        var iconImg = Go("Icon", container).AddComponent<Image>();
        iconImg.color = new Color(0.55f, 0.55f, 0.60f);
        var ile = iconImg.gameObject.AddComponent<LayoutElement>();
        ile.preferredWidth = ile.minWidth  = CurrencyIcon;
        ile.preferredHeight = ile.minHeight = CurrencyIcon;

        // 자릿수가 늘어나도 줄바꿈 대신 폰트를 줄인다 (UI 규칙 5 — 칸 높이 유지)
        var amtTmp = TMP(container, "Amount", "0", UIScale.FontSm, FontStyles.Bold);
        amtTmp.alignment        = TextAlignmentOptions.Left;
        amtTmp.color            = new Color(0.88f, 0.90f, 0.98f);
        amtTmp.raycastTarget    = false;
        amtTmp.textWrappingMode = TextWrappingModes.NoWrap;
        amtTmp.overflowMode     = TextOverflowModes.Overflow;
        amtTmp.enableAutoSizing = true;
        amtTmp.fontSizeMin      = UIScale.FontSm * 0.7f;
        amtTmp.fontSizeMax      = UIScale.FontSm;
        var ale = amtTmp.gameObject.AddComponent<LayoutElement>();
        ale.preferredWidth = CurrencyW - CurrencyIcon - 24f;
        ale.minWidth       = 60f;

        var widget = container.AddComponent<CurrencyWidget>();
        var wSo    = new SerializedObject(widget);
        wSo.FindProperty("_item").intValue                   = (int)item;
        wSo.FindProperty("_amountText").objectReferenceValue = amtTmp;
        wSo.FindProperty("_icon").objectReferenceValue       = iconImg;
        wSo.ApplyModifiedProperties();
    }

    static TextMeshProUGUI MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "영웅 이름", UIScale.FontLg, FontStyles.Bold);
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
    //  좌측 열 — 초상화 + 장비 스트립 / 기본 정보 / 성장
    // ══════════════════════════════════════════════════════════

    static void BuildLeftColumn(GameObject panel, SerializedObject so, float x)
    {
        var col = Column(panel, "LeftColumn", x, LeftW);

        // ── 초상화 (좌) ──────────────────────────────────────
        var portraitPit = Go("PortraitPit", col);
        portraitPit.AddComponent<Image>().color = PitBg;
        var ppRt = portraitPit.GetComponent<RectTransform>();
        ppRt.anchorMin = ppRt.anchorMax = new Vector2(0f, 1f);
        ppRt.pivot     = new Vector2(0f, 1f);
        ppRt.anchoredPosition = new Vector2(12f, -12f);
        ppRt.sizeDelta        = new Vector2(PortraitSize, PortraitSize);

        var gradeBorder = Go("GradeBorder", portraitPit);
        gradeBorder.AddComponent<Image>().color = DividerLine;
        Stretch(gradeBorder);
        SetObj(so, "_gradeBorder", gradeBorder.GetComponent<Image>());

        var portraitBg = Go("PortraitBg", portraitPit);
        portraitBg.AddComponent<Image>().color = new Color(0.16f, 0.27f, 0.56f);
        var pbRt = portraitBg.GetComponent<RectTransform>();
        pbRt.anchorMin = Vector2.zero; pbRt.anchorMax = Vector2.one;
        pbRt.offsetMin = new Vector2(4f, 4f); pbRt.offsetMax = new Vector2(-4f, -4f);
        SetObj(so, "_portraitBg", portraitBg.GetComponent<Image>());

        var portraitImg = Go("PortraitImage", portraitPit, typeof(Image)).GetComponent<Image>();
        portraitImg.color          = Color.clear;
        portraitImg.preserveAspect = true;
        portraitImg.raycastTarget  = false;
        var piRt = portraitImg.rectTransform;
        piRt.anchorMin = piRt.anchorMax = new Vector2(0.5f, 0.5f);
        piRt.sizeDelta = new Vector2(PortraitSize - 60f, PortraitSize - 60f);
        SetObj(so, "_portraitImage", portraitImg);

        var preview = Go("PortraitPreview", portraitPit);
        preview.SetActive(false);
        var bridge = preview.AddComponent<UnitAppearanceBridge>();
        if (preview.TryGetComponent<CharacterBuilder>(out var builder))
        {
            var sc = AssetDatabase.LoadAssetAtPath<SpriteCollection>(
                "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset");
            if (sc != null) builder.SpriteCollection = sc;
        }
        SetObj(so, "_portraitBridge", bridge);

        // ── 장비 스트립 (초상화 오른쪽, 세로 3칸) ────────────
        //  높이가 초상화와 정확히 같도록 128×3 + 8×2 = 400 으로 잡았다.
        var equipRoot = Go("EquipStrip", col);
        var erRt = equipRoot.GetComponent<RectTransform>();
        erRt.anchorMin = erRt.anchorMax = new Vector2(0f, 1f);
        erRt.pivot     = new Vector2(0f, 1f);
        erRt.anchoredPosition = new Vector2(12f + PortraitSize + 12f, -12f);
        erRt.sizeDelta        = new Vector2(EquipStripW, EquipTileH * 3f + EquipTileGap * 2f);
        SetObj(so, "_equipRoot", equipRoot);

        var slots = new Object[3];
        for (int i = 0; i < 3; i++)
            slots[i] = BuildEquipSlot(equipRoot, i);
        SetObjArray(so, "_equipSlots", slots);

        // ── 기본 정보 행: Lv · 직업 · 등급 ───────────────────
        // 장비 스트립(520)이 초상화(400)보다 길다 — 더 긴 쪽 아래에 붙인다
        float infoY = 12f + EquipTileH * 3f + EquipTileGap * 2f + 20f;

        var levelTmp = TMP(col, "LevelText", "Lv.1", UIScale.FontMd, FontStyles.Bold);
        levelTmp.alignment     = TextAlignmentOptions.MidlineLeft;
        levelTmp.color         = new Color(0.88f, 0.90f, 0.98f);
        levelTmp.raycastTarget = false;
        AnchorTop(levelTmp.gameObject, infoY, InfoRowH, 24f);

        var jobTmp = TMP(col, "JobText", "기사", UIScale.FontMd, FontStyles.Normal);
        jobTmp.alignment     = TextAlignmentOptions.Center;
        jobTmp.color         = Color.white;
        jobTmp.raycastTarget = false;
        AnchorTop(jobTmp.gameObject, infoY, InfoRowH, 24f);

        var gradeBadge = Go("GradeBadge", col);
        gradeBadge.AddComponent<Image>().color = new Color(0.55f, 0.55f, 0.55f);
        var gbRt = gradeBadge.GetComponent<RectTransform>();
        gbRt.anchorMin = gbRt.anchorMax = new Vector2(1f, 1f);
        gbRt.pivot     = new Vector2(1f, 1f);
        gbRt.anchoredPosition = new Vector2(-12f, -infoY);
        gbRt.sizeDelta        = new Vector2(120f, InfoRowH);

        var gradeTmp = TMP(gradeBadge, "Label", "일반", UIScale.FontSm, FontStyles.Bold);
        gradeTmp.alignment     = TextAlignmentOptions.Center;
        gradeTmp.raycastTarget = false;
        Stretch(gradeTmp.gameObject);

        SetObj(so, "_levelText",  levelTmp);
        SetObj(so, "_jobText",    jobTmp);
        SetObj(so, "_gradeBadge", gradeBadge.GetComponent<Image>());
        SetObj(so, "_gradeText",  gradeTmp);

        BuildRankRow(col, so);
        BuildGrowthRow(col, so);
    }

    // ── 등급업 · 해고 행 (초상화 아래 빈 자리) ────────────────
    //  초상화 하단 = 12 + 400 = 412, 정보 행 시작 = infoY(552).
    //  그 사이 140 중 위아래 여백을 빼고 버튼(BtnH=72)을 세로 가운데에 놓는다.

    static void BuildRankRow(GameObject col, SerializedObject so)
    {
        float rowTop = 12f + PortraitSize + (RankRowGap - BtnH) * 0.5f;

        var row = Go("RankRow", col);
        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(12f, -rowTop);
        rt.sizeDelta        = new Vector2(PortraitSize, BtnH);
        SetObj(so, "_rankRow", row);

        // 등급업 — 비용(강화석)이 붙으므로 넓게. 해고는 라벨만이라 좁아도 된다.
        var (gradeBtn, gradeCostTmp, gradeCostIcon) =
            BuildCostButton(row, "GradeUpButton", "등급", GradeUpBtnC, 0f, 0.62f, -6f,
                            eItem.GeneralUpgradeStone);
        SetObj(so, "_gradeUpBtn",      gradeBtn);
        SetObj(so, "_gradeUpCostText", gradeCostTmp);
        SetObj(so, "_gradeUpCostIcon", gradeCostIcon);

        var fireBtn = EditorUIBuilder.RaisedBtn(row, "FireButton", FireBtnC, out var fireBody);
        var fRt = fireBtn.GetComponent<RectTransform>();
        fRt.anchorMin = new Vector2(0.62f, 0f);
        fRt.anchorMax = new Vector2(1f,    0f);
        fRt.pivot     = new Vector2(0.5f,  0f);
        fRt.offsetMin = new Vector2(6f, 0f);
        fRt.offsetMax = new Vector2(0f, BtnH);

        // ⚠ 라벨은 반드시 Body 아래에 — 루트에 넣으면 눌려도 같이 안 내려간다 (UI 규칙 1)
        var fireLbl = TMP(fireBody, "Label", "해고", UIScale.FontMd, FontStyles.Bold);
        fireLbl.alignment     = TextAlignmentOptions.Center;
        fireLbl.color         = Color.white;
        fireLbl.raycastTarget = false;
        Stretch(fireLbl.gameObject);

        SetObj(so, "_fireBtn", fireBtn);
    }

    // ── 성장 행: 열 하단에 붙인다 (EXP 텍스트 → 바 → 버튼) ──

    static void BuildGrowthRow(GameObject col, SerializedObject so)
    {
        var row = Go("GrowthRow", col);
        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(12f, 12f);
        rt.offsetMax = new Vector2(-12f, 12f + BtnH + 16f + 14f + 8f + UIScale.RowSm);
        SetObj(so, "_growthRow", row);

        // EXP 텍스트 (맨 위)
        var expTmp = TMP(row, "ExpText", "0 / 100 EXP", UIScale.FontSm, FontStyles.Normal);
        expTmp.alignment     = TextAlignmentOptions.Center;
        expTmp.color         = LabelColor;
        expTmp.raycastTarget = false;
        AnchorTop(expTmp.gameObject, 0f, UIScale.RowSm);
        SetObj(so, "_expText", expTmp);

        // EXP 바
        var barBg = Go("ExpBarBg", row);
        barBg.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.12f);
        AnchorTop(barBg, UIScale.RowSm + 8f, 14f);

        var fill = Go("ExpBarFill", barBg);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.65f, 1.00f);
        var fRt = fill.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero; fRt.anchorMax = new Vector2(0f, 1f);
        fRt.offsetMin = fRt.offsetMax = Vector2.zero;
        SetObj(so, "_expBarFill", fillImg);

        // 레벨업 · 용병 버튼 (하단, 반반)
        var (lvBtn, lvCostTmp, lvCostIcon) =
            BuildCostButton(row, "LevelUpButton", "레벨", LevelUpBtnC, 0f, 0.5f, -6f, eItem.Gold);
        SetObj(so, "_levelUpBtn",      lvBtn);
        SetObj(so, "_levelUpCostText", lvCostTmp);
        SetObj(so, "_levelUpCostIcon", lvCostIcon);

        var (sdBtn, sdCostTmp, sdCostIcon) =
            BuildCostButton(row, "SoldierUpButton", "용병", SoldierBtnC, 0.5f, 1f, 6f, eItem.SoldierShard);
        SetObj(so, "_soldierUpBtn",      sdBtn);
        SetObj(so, "_soldierUpCostText", sdCostTmp);
        SetObj(so, "_soldierUpCostIcon", sdCostIcon);
    }

    // [라벨][아이콘 비용] 입체 버튼. 폭을 못 박지 않고 내용에 맞춰 흐르게 둔다.
    static (Button btn, TextMeshProUGUI cost, Image icon) BuildCostButton(
        GameObject parent, string name, string label, Color face,
        float anchorL, float anchorR, float inset, eItem costItem)
    {
        var btn = EditorUIBuilder.RaisedBtn(parent, name, face, out var btnBody);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorL, 0f);
        rt.anchorMax = new Vector2(anchorR, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(inset > 0 ? inset : 0f, 0f);
        rt.offsetMax = new Vector2(inset < 0 ? inset : 0f, BtnH);

        // ⚠ Body 에는 이미 TopEdge/BottomEdge 가 있다 — 레이아웃 그룹은 한 겹 안에서.
        var body = Go("Content", btnBody);
        Stretch(body);
        var hlg = body.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 8f;
        hlg.padding                = new RectOffset(10, 10, 0, 0);
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        var lbl = TMP(body, "Label", label, UIScale.FontSm, FontStyles.Bold);
        lbl.alignment        = TextAlignmentOptions.Right;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Overflow;

        UpMark(body);

        var icon = Go("CostIcon", body, typeof(Image)).GetComponent<Image>();
        icon.sprite         = LoadItemIcon(costItem);
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        EditorUIBuilder.IconLE(icon, 30f);

        var cost = TMP(body, "CostText", "0", UIScale.FontSm, FontStyles.Bold);
        cost.alignment        = TextAlignmentOptions.Left;
        cost.color            = new Color(1.00f, 0.85f, 0.20f);
        cost.raycastTarget    = false;
        cost.textWrappingMode = TextWrappingModes.NoWrap;
        cost.overflowMode     = TextOverflowModes.Overflow;

        return (btn, cost, icon);
    }

    // ── 장비 칸 (아이콘만 + 강화 버튼) ───────────────────────

    static HeroEquipSlotUI BuildEquipSlot(GameObject strip, int index)
    {
        float enhH = UIScale.BtnFor(UIScale.FontSm);   // 58
        float iconH = EquipTileH - enhH - 6f;          // 64

        var go = Go($"EquipSlot_{index}", strip);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, -index * (EquipTileH + EquipTileGap));
        rt.sizeDelta        = new Vector2(EquipStripW, EquipTileH);
        var slot = go.AddComponent<HeroEquipSlotUI>();

        // 타일 = 등급 색 테두리(Frame) + 안쪽 홈(Pit) + 아이콘
        var frameBtn = Go("Frame", go, typeof(Image), typeof(Button));
        var frameImg = frameBtn.GetComponent<Image>();
        frameImg.color = new Color(0.24f, 0.25f, 0.34f);
        AnchorTop(frameBtn, 0f, iconH);

        var pit = Go("Pit", frameBtn);
        pit.AddComponent<Image>().color = PitBg;
        var pitRt = pit.GetComponent<RectTransform>();
        pitRt.anchorMin = Vector2.zero; pitRt.anchorMax = Vector2.one;
        pitRt.offsetMin = new Vector2(3f, 3f); pitRt.offsetMax = new Vector2(-3f, -3f);
        pit.GetComponent<Image>().raycastTarget = false;

        var icon = Go("Icon", pit, typeof(Image)).GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        var iRt = icon.rectTransform;
        iRt.anchorMin = new Vector2(0.5f, 0.5f); iRt.anchorMax = new Vector2(0.5f, 0.5f);
        iRt.sizeDelta = new Vector2(iconH - 16f, iconH - 16f);

        // 빈 칸 표시 — ＋ 글리프는 폰트에 있지만 굵기를 맞출 수 없어 도형으로 그린다
        var emptyMark = Go("EmptyMark", pit);
        EditorUIBuilder.Bar(emptyMark, "H", 30f, 4f, 0f,  Vector2.zero, new Color(0.34f, 0.36f, 0.48f));
        EditorUIBuilder.Bar(emptyMark, "V", 30f, 4f, 90f, Vector2.zero, new Color(0.34f, 0.36f, 0.48f));
        Center(emptyMark);
        emptyMark.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 30f);

        // 강화 단계 배지 (우상단)
        var badge = TMP(frameBtn, "EnhanceBadge", "+0", UIScale.FontSm, FontStyles.Bold);
        badge.alignment     = TextAlignmentOptions.Center;
        badge.color         = new Color(0.60f, 1.00f, 0.75f);
        badge.raycastTarget = false;
        var bRt = badge.rectTransform;
        bRt.anchorMin = bRt.anchorMax = new Vector2(1f, 1f);
        bRt.pivot     = new Vector2(1f, 1f);
        bRt.anchoredPosition = new Vector2(-4f, -2f);
        bRt.sizeDelta        = new Vector2(70f, UIScale.RowSm);
        badge.gameObject.SetActive(false);

        // 강화 버튼 (타일 아래)
        var enhBtn = EditorUIBuilder.RaisedBtn(go, "EnhanceBtn", EnhanceBtnC, out var enhBody, 4f);
        AnchorBottom(enhBtn.gameObject, 0f, enhH);

        var content = Go("Content", enhBody);
        Stretch(content);
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 4f;
        hlg.padding                = new RectOffset(6, 6, 0, 0);
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        UpMark(content);

        var enhIcon = Go("CostIcon", content, typeof(Image)).GetComponent<Image>();
        enhIcon.sprite         = LoadItemIcon(eItem.EquipUpgradeStone);
        enhIcon.preserveAspect = true;
        enhIcon.raycastTarget  = false;
        EditorUIBuilder.IconLE(enhIcon, 26f);

        var enhCost = TMP(content, "CostText", "2", UIScale.FontSm, FontStyles.Bold);
        enhCost.alignment        = TextAlignmentOptions.Left;
        enhCost.color            = new Color(0.80f, 0.90f, 1.00f);
        enhCost.raycastTarget    = false;
        enhCost.textWrappingMode = TextWrappingModes.NoWrap;
        enhCost.overflowMode     = TextOverflowModes.Overflow;

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_frame",           frameImg);
        SetObj(sSo, "_icon",            icon);
        SetObj(sSo, "_emptyMark",       emptyMark);
        SetObj(sSo, "_enhanceBadge",    badge);
        SetObj(sSo, "_selectBtn",       frameBtn.GetComponent<Button>());
        SetObj(sSo, "_enhanceBtn",      enhBtn);
        SetObj(sSo, "_enhanceCostText", enhCost);
        SetObj(sSo, "_enhanceCostIcon", enhIcon);
        sSo.ApplyModifiedProperties();
        return slot;
    }

    // ══════════════════════════════════════════════════════════
    //  가운데 열 — 스탯 9행
    // ══════════════════════════════════════════════════════════

    static void BuildStatColumn(GameObject panel, SerializedObject so, float x)
    {
        var col = Column(panel, "StatColumn", x, MidW);
        BuildDivider(col, 12f, "스  탯");

        // ── 장수 / 용병 토글 ────────────────────────────────
        //  용병은 장수 스탯 × 배율이라 같은 행을 다시 쓴다 (SoldierRuntimeBridge 공식).
        float tabY = 12f + DivH + 10f;
        var generalTab = BuildStatTab(col, "GeneralTab", "장 수", 0f,    0.5f, tabY, true);
        var soldierTab = BuildStatTab(col, "SoldierTab", "용 병", 0.5f, 1f,   tabY, false);
        SetObj(so, "_generalTabBtn", generalTab);
        SetObj(so, "_soldierTabBtn", soldierTab);

        var list = Go("StatListContainer", col);
        var rt = list.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(10f, 10f);
        rt.offsetMax = new Vector2(-10f, -StatListTop);

        // ⚠ 레이아웃 그룹을 쓰지 않는다.
        //   용병 탭에서 3줄이 빠지면 남은 줄이 재배치돼 "사거리가 어디 갔지" 가 된다.
        //   행마다 index × StatRowH 로 못 박아 두면 어떤 탭에서도 같은 자리에 있다.
        int rowIndex = 0;

        SetObj(so, "_hpText",     Tint(StatRow(list, "HP",   "체력",   rowIndex++), StatColors.Hp));
        SetObj(so, "_atkText",    Tint(StatRow(list, "ATK",  "공격",   rowIndex++), StatColors.Atk));
        SetObj(so, "_defText",    Tint(StatRow(list, "DEF",  "방어율", rowIndex++), StatColors.Def));

        // 용병 수는 네 번째 — 장수를 고르는 기준이 되는 값이라 위쪽에 둔다.
        // ⚠ 장수 전용 행이라 용병 탭에서는 이 자리가 빈다 (아래 _generalOnlyRows 참고)
        var soldierCnt = Tint(StatRow(list, "SOLD", "용병 수",   rowIndex++), StatColors.Soldier);

        SetObj(so, "_spdText",         StatRow(list, "SPD",  "이동속도", rowIndex++));
        SetObj(so, "_atkSpdText",      StatRow(list, "ASPD", "공격속도", rowIndex++));
        SetObj(so, "_rangeText",       StatRow(list, "RNG",  "사거리",   rowIndex++));

        var cmdPwr   = StatRow(list, "CMD", "지휘력", rowIndex++);
        // 라벨은 짧게 — "스킬 쿨타임" 은 라벨 칸을 넘겨 값(3.3%) 위로 밀고 들어왔다
        var cooldown = StatRow(list, "CD",  "쿨타임", rowIndex++);

        // 치명타 2행 — 맨 아래. 용병 탭에서도 보인다.
        //   치확은 환산율이 곱해지고 치피는 그대로다 (SoldierRuntimeBridge.IsUnscaled).
        //   두 값이 한 화면에 붙어 있어야 그 차이가 읽힌다.
        SetObj(so, "_critChanceText", StatRow(list, "CRIT",  "치명확률", rowIndex++));
        SetObj(so, "_critDmgText",    StatRow(list, "CRITD", "치명피해", rowIndex++));

        SetObj(so, "_soldierCountText", soldierCnt);
        SetObj(so, "_cmdPwrText",       cmdPwr);
        SetObj(so, "_cooldownText",     cooldown);
        SetObjArray(so, "_generalOnlyRows", new Object[]
        {
            soldierCnt.transform.parent.gameObject,
            cmdPwr    .transform.parent.gameObject,
            cooldown  .transform.parent.gameObject,
        });

    }

    //  탭 버튼 — 입체(음각)로 만들어 "누를 수 있다"를 먼저 읽히게 한다.
    //  선택 상태는 Body 색 + 밑줄로 표시한다 (HeroDetailPopup.StyleStatTab 이 런타임에 갱신).
    static Button BuildStatTab(GameObject col, string name, string label,
                               float aMinX, float aMaxX, float y, bool active)
    {
        var btn = EditorUIBuilder.RaisedBtn(col, name,
                                            active ? TabFaceOn : TabFaceOff, out var body, 4f);

        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 1f);
        rt.anchorMax = new Vector2(aMaxX, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(aMinX > 0f ? 4f : 10f, -(y + StatTabH));
        rt.offsetMax = new Vector2(aMaxX < 1f ? -4f : -10f, -y);

        var tmp = TMP(body, "Label", label, UIScale.FontMd,
                      active ? FontStyles.Bold : FontStyles.Normal);
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = active ? TabActiveC : TabIdleC;
        tmp.raycastTarget = false;
        Stretch(tmp.gameObject);

        var bar = Go("ActiveBar", body, typeof(Image));
        var barImg = bar.GetComponent<Image>();
        barImg.color         = TabActiveC;
        barImg.raycastTarget = false;
        var bRt = bar.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.14f, 0f); bRt.anchorMax = new Vector2(0.86f, 0f);
        bRt.offsetMin = new Vector2(0f, 4f);    bRt.offsetMax = new Vector2(0f, 8f);
        bar.SetActive(active);

        return btn;
    }

    //  ⚠ 값 TMP 는 Ellipsis 금지 (UI 규칙 5)
    //    칸보다 긴 분해 문자열이 통째로 "..." 으로 바뀌어 숫자가 사라졌다.
    //    AutoSize 로 줄여서 담고, 칸 높이는 FontMd 한 줄보다 넉넉히 잡는다.
    static TextMeshProUGUI StatRow(GameObject parent, string id, string label, int index)
    {
        var row = Go($"Stat_{id}", parent, typeof(Image), typeof(Button));
        AnchorTop(row, index * StatRowH, StatRowH);
        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        var btn = row.GetComponent<Button>();
        btn.targetGraphic = row.GetComponent<Image>();
        btn.transition    = Selectable.Transition.None;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.padding                = new RectOffset(14, 14, 0, 0);
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        var lbl = TMP(row, "Label", label, UIScale.FontMd, FontStyles.Normal);
        lbl.alignment        = TextAlignmentOptions.MidlineLeft;
        lbl.color            = LabelColor;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Overflow;
        var lblLe = lbl.gameObject.AddComponent<LayoutElement>();
        lblLe.preferredWidth = 190f;
        lblLe.flexibleWidth  = 0f;

        var val = TMP(row, "Value", "—", UIScale.FontMd, FontStyles.Bold);
        val.alignment        = TextAlignmentOptions.MidlineRight;
        val.raycastTarget    = false;
        val.textWrappingMode = TextWrappingModes.NoWrap;
        val.overflowMode     = TextOverflowModes.Overflow;
        val.enableAutoSizing = true;
        val.fontSizeMin      = UIScale.FontSm - 8f;
        val.fontSizeMax      = UIScale.FontMd;
        val.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var line = Go("Divider", row, typeof(Image));
        line.GetComponent<Image>().color = RowLine;
        line.GetComponent<Image>().raycastTarget = false;
        line.AddComponent<LayoutElement>().ignoreLayout = true;
        var lRt = line.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0f, 0f); lRt.anchorMax = new Vector2(1f, 0f);
        lRt.offsetMin = new Vector2(8f, 0f); lRt.offsetMax = new Vector2(-8f, 1f);

        return val;
    }

    // ══════════════════════════════════════════════════════════
    //  우측 열 — 스킬 (액티브 1 + 패시브 3)
    // ══════════════════════════════════════════════════════════

    static void BuildSkillColumn(GameObject panel, SerializedObject so, float x)
    {
        var col = Column(panel, "SkillColumn", x, RightW);
        BuildDivider(col, 12f, "스  킬");

        const float ActiveY = 60f;
        const float ActiveH = ActiveBoxH;
        const float IconSz  = 128f;

        // ── 액티브 스킬 ──────────────────────────────────────
        var box = Go("ActiveBox", col);
        box.AddComponent<Image>().color = SkillBoxBg;
        AnchorTop(box, ActiveY, ActiveH, 24f);

        var accent = Go("AccentBar", box);
        var accImg = accent.AddComponent<Image>();
        accImg.color = ActiveAccent;
        accImg.raycastTarget = false;
        var aRt = accent.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 0f); aRt.anchorMax = new Vector2(0f, 1f);
        aRt.offsetMin = Vector2.zero;        aRt.offsetMax = new Vector2(6f, 0f);

        var iconPit = Go("IconPit", box);
        iconPit.AddComponent<Image>().color = PitBg;
        var ipRt = iconPit.GetComponent<RectTransform>();
        ipRt.anchorMin = ipRt.anchorMax = new Vector2(0f, 1f);
        ipRt.pivot     = new Vector2(0f, 1f);
        ipRt.anchoredPosition = new Vector2(20f, -20f);
        ipRt.sizeDelta        = new Vector2(IconSz, IconSz);

        var skillIcon = Go("ActiveSkillIcon", iconPit, typeof(Image)).GetComponent<Image>();
        skillIcon.color          = new Color(0.28f, 0.32f, 0.52f);
        skillIcon.preserveAspect = true;
        skillIcon.raycastTarget  = false;
        var siRt = skillIcon.rectTransform;
        siRt.anchorMin = Vector2.zero; siRt.anchorMax = Vector2.one;
        siRt.offsetMin = new Vector2(6f, 6f); siRt.offsetMax = new Vector2(-6f, -6f);
        SetObj(so, "_activeSkillIcon", skillIcon);

        float textLeft = 20f + IconSz + 18f;

        var nameTmp = TMP(box, "ActiveSkillText", "—", UIScale.FontLg, FontStyles.Bold);
        nameTmp.color            = ActNameC;
        nameTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Overflow;
        var nRt = nameTmp.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot     = new Vector2(0.5f, 1f);
        nRt.offsetMin = new Vector2(textLeft, -(18f + UIScale.RowLg));
        nRt.offsetMax = new Vector2(-20f, -18f);
        SetObj(so, "_activeSkillText", nameTmp);

        // 설명 — 남는 칸에 맞춰 줄어든다.
        // ⚠ 예전엔 FontMd(42) + lineSpacing 12 고정이라 한 줄이 64px 였는데
        //   칸은 108px 뿐이라 1.7줄 밖에 못 담았고, Overflow 라서 넘친 줄이
        //   상자 밖(배경 바깥)에 그대로 그려졌다.
        //   AutoSize 를 켜면 넘치는 대신 글자가 작아진다.
        var descTmp = TMP(box, "ActiveSkillDescText", "", UIScale.FontMd, FontStyles.Normal);
        descTmp.color            = ActDescC;
        descTmp.alignment        = TextAlignmentOptions.TopLeft;
        descTmp.raycastTarget    = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode     = TextOverflowModes.Overflow;
        descTmp.lineSpacing      = 6f;
        descTmp.enableAutoSizing = true;
        descTmp.fontSizeMin      = UIScale.FontSm - 8f;
        descTmp.fontSizeMax      = UIScale.FontMd;
        var dRt = descTmp.rectTransform;
        dRt.anchorMin = new Vector2(0f, 0f); dRt.anchorMax = new Vector2(1f, 1f);
        dRt.offsetMin = new Vector2(textLeft, 16f);
        dRt.offsetMax = new Vector2(-20f, -(18f + UIScale.RowLg + 6f));
        SetObj(so, "_activeSkillDescText", descTmp);

        // ── 패시브 3칸 ───────────────────────────────────────
        float passY = ActiveY + ActiveH + 20f;

        var cont = Go("PassiveContainer", col);
        var cRt = cont.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 0f); cRt.anchorMax = new Vector2(1f, 1f);
        cRt.offsetMin = new Vector2(12f, 12f);
        cRt.offsetMax = new Vector2(-12f, -passY);

        var vlg = cont.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 12f;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        // 패시브 칸 수는 등급마다 다르다(1~3). 늘리게 두면 1칸짜리 노멀 등급에서
        // 상자 하나가 열 전체로 부풀어 빈 공간처럼 보인다 — 고정 높이로 둔다.
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;

        var boxes = new Object[3];
        var icons = new Object[3];
        var names = new Object[3];
        var descs = new Object[3];
        for (int i = 0; i < 3; i++)
        {
            var (boxGo, iconI, nameT, descT) = BuildPassiveBox(cont, i);
            boxes[i] = boxGo; icons[i] = iconI; names[i] = nameT; descs[i] = descT;
        }
        SetObjArray(so, "_passiveBoxes",     boxes);
        SetObjArray(so, "_passiveIcons",     icons);
        SetObjArray(so, "_passiveNameTexts", names);
        SetObjArray(so, "_passiveDescTexts", descs);
    }

    //  [액센트바][아이콘 96][이름 / 설명]
    //  아이콘이 붙으면서 글자 폭이 96+여백 만큼 줄었다 —
    //  이름은 NoWrap, 설명은 AutoSize 라 넘치지 않는다.
    static (GameObject box, Image icon, TextMeshProUGUI name, TextMeshProUGUI desc)
        BuildPassiveBox(GameObject cont, int index)
    {
        const float IconSz  = 96f;
        const float IconX   = 20f;
        float textLeft = IconX + IconSz + 18f;   // 134

        var box = Go($"Passive{index}Box", cont);
        box.AddComponent<Image>().color = SkillBoxBg;
        box.AddComponent<LayoutElement>().preferredHeight = PassiveBoxH;

        var accent = Go("AccentBar", box);
        var accImg = accent.AddComponent<Image>();
        accImg.color = PassAccent;
        accImg.raycastTarget = false;
        var aRt = accent.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 0f); aRt.anchorMax = new Vector2(0f, 1f);
        aRt.offsetMin = Vector2.zero;        aRt.offsetMax = new Vector2(6f, 0f);

        // 아이콘 자리 — 액티브와 같은 "홈에 얹힌 카드" 형태
        var pit = Go("IconPit", box);
        pit.AddComponent<Image>().color = PitBg;
        var pRt = pit.GetComponent<RectTransform>();
        pRt.anchorMin = pRt.anchorMax = new Vector2(0f, 0.5f);
        pRt.pivot     = new Vector2(0f, 0.5f);
        pRt.anchoredPosition = new Vector2(IconX, 0f);
        pRt.sizeDelta        = new Vector2(IconSz, IconSz);

        var icon = Go("PassiveIcon", pit, typeof(Image)).GetComponent<Image>();
        icon.color          = new Color(0.25f, 0.24f, 0.40f);
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        var iRt = icon.rectTransform;
        iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(5f, 5f); iRt.offsetMax = new Vector2(-5f, -5f);

        var nameTmp = TMP(box, "NameText", "—", UIScale.FontMd, FontStyles.Bold);
        nameTmp.color            = PassNameC;
        nameTmp.alignment        = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Overflow;
        var nRt = nameTmp.rectTransform;
        nRt.anchorMin = new Vector2(0f, 1f); nRt.anchorMax = new Vector2(1f, 1f);
        nRt.pivot     = new Vector2(0.5f, 1f);
        nRt.offsetMin = new Vector2(textLeft, -(12f + UIScale.RowMd));
        nRt.offsetMax = new Vector2(-16f, -12f);

        var descTmp = TMP(box, "DescText", "", UIScale.FontSm, FontStyles.Normal);
        descTmp.color            = PassDescC;
        descTmp.alignment        = TextAlignmentOptions.TopLeft;
        descTmp.raycastTarget    = false;
        descTmp.textWrappingMode = TextWrappingModes.Normal;
        descTmp.overflowMode     = TextOverflowModes.Overflow;
        descTmp.lineSpacing      = 4f;
        descTmp.enableAutoSizing = true;
        descTmp.fontSizeMin      = UIScale.FontSm - 12f;
        descTmp.fontSizeMax      = UIScale.FontSm;
        var dRt = descTmp.rectTransform;
        dRt.anchorMin = new Vector2(0f, 0f); dRt.anchorMax = new Vector2(1f, 1f);
        dRt.offsetMin = new Vector2(textLeft, 10f);
        dRt.offsetMax = new Vector2(-16f, -(12f + UIScale.RowMd + 4f));

        return (box, icon, nameTmp, descTmp);
    }

    // ══════════════════════════════════════════════════════════
    //  공통 조각
    // ══════════════════════════════════════════════════════════

    // 본문 3단의 열 하나 — 좌표 x 부터 폭 w, 세로는 본문 전체
    static GameObject Column(GameObject panel, string name, float x, float w)
    {
        var col = Go(name, panel);
        col.AddComponent<Image>().color = ColumnBg;
        var rt = col.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -BodyTop);
        rt.sizeDelta        = new Vector2(w, BodyH);
        return col;
    }

    //  섹션 구분선 — 가운데 글자 + 좌우 라인 (EventPopup 의 "선 택" 과 같은 형태)
    static void BuildDivider(GameObject col, float y, string label)
    {
        var div = Go($"Divider_{label}", col);
        AnchorTop(div, y, DivH, 32f);

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

    /// <summary>
    /// "업(상승)" 세모를 레이아웃 그룹 안에 넣는다.
    /// ▲ 글리프는 기본 폰트에 없어 □ 로 렌더된다 — 도형으로 그려야 한다 (UI 규칙 2).
    /// </summary>
    static void UpMark(GameObject parent)
    {
        var mark = EditorUIBuilder.TriangleUp(parent, "UpMark", UpMarkSize, UpMarkC);
        var le = mark.AddComponent<LayoutElement>();
        le.preferredWidth = le.minWidth  = UpMarkSize;
        le.preferredHeight = le.minHeight = UpMarkSize;
        le.flexibleWidth  = 0f;
    }

    static TextMeshProUGUI Tint(TextMeshProUGUI tmp, Color c) { tmp.color = c; return tmp; }

    static Sprite LoadItemIcon(eItem item)
        => AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/_project/3.Textures/Icons/Items/{item.IconKey()}.png");

    // ── 레이아웃 헬퍼 ────────────────────────────────────────

    static void CenterBox(GameObject go, float w, float h)
        => EditorUIBuilder.Center(go.GetComponent<RectTransform>(), Vector2.zero, new Vector2(w, h));

    static void AnchorTop(GameObject go, float yFromTop, float height, float padH = 0f)
        => EditorUIBuilder.AnchorTop(go.GetComponent<RectTransform>(), yFromTop, height, padH);

    static void AnchorBottom(GameObject go, float yFromBottom, float height, float padH = 0f)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(padH * 0.5f, yFromBottom);
        rt.offsetMax = new Vector2(-padH * 0.5f, yFromBottom + height);
    }

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

    static GameObject CreatePanel(GameObject parent, string name, Color color)
        => EditorUIBuilder.Panel(parent, name, color);

    static TextMeshProUGUI TMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void Stretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "HeroDetailPopupCreator");

    static void SetObjArray(SerializedObject so, string field, Object[] objs)
        => EditorUIBuilder.SetObjArray(so, field, objs, "HeroDetailPopupCreator");

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "HeroDetailPopupCreator");
}
#endif
