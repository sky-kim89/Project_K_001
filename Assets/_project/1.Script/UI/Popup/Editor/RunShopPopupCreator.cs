using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopPopupCreator.cs  [Editor Only]
//  Tools > Project K > 프리팹 생성 > 팝업 > RunShop
//
//  ⚠ 캔버스 기준
//    이 팝업은 EventPopup("행상인의 좌판") 위에 열리므로 항상 로비 캔버스다.
//    로비 캔버스는 1920×1080 가로 — 세로 여유가 1080 뿐이라
//    패널 높이는 UIScale.PopupMaxH(1000)를 넘길 수 없다.
//
//  ■ 왜 또 다시 짰나 (직전 레이아웃의 문제)
//    · 공용 HeroCard(360×170)를 용병 칸에 그대로 썼다. 이 카드의 글자 영역은
//      초상화(104px)를 뺀 나머지를 다시 반씩 쪼개 쓰는 구조라, 상점 칸 폭
//      (≈339)에서는 이름 칸이 103px 밖에 안 됐다 → 5~6글자 이름이 "..." 로 잘렸다.
//    · 같은 이유로 스탯 칸은 [라벨 39px][값 58px] 이 됐다. 라벨·값이 각자
//      AutoSize 로 줄어드니 네 칸의 글자 크기가 제각각이 되고, 자릿수가 커지면
//      Overflow 로 서로 침범했다 ("체력 12,345" 가 라벨 위로 겹침).
//    · 상품 6칸이 같은 크기·같은 간격 한 줄이라 장비와 특성이 구분되지 않았다.
//
//  ■ 새 레이아웃 — 전체화면 + 용병 우선
//    패널을 캔버스 전체(1920×1080)로 덮는다. 세로 110px 을 더 벌어
//    용병 칸을 513px 세로 카드로 키웠다.
//
//    Header       Y=  0  H=120  ◆ 상 점 / 타이틀 | 보유골드 · 새로고침 · 닫기
//    AccentLine   Y=120  H=  3
//    MercDivider  Y=138  H= 38  "용 병 고 용"
//    MercRow      Y=184  H=513  RunShopGeneralSlot × 5 (칸 ≈352×513)
//    GoodsArea    Y=712  H=344  [장비 블록 4칸] ┃ [특성 블록 2칸]
//                                 → 블록마다 배경색·헤더바가 다르고 56px 벌어져 있다
//
//    용병 카드는 이 팝업 전용으로 여기서 만든다 (공용 HeroCard 를 쓰지 않는다).
//    자식 이름(PortraitBg/NameText/HpText…)은 그대로라 RunShopGeneralSlot 은
//    손대지 않아도 된다.
//
//    상품 칸은 RewardCard(전투 결과·이벤트와 같은 카드) + 이름 + 가격뿐이다.
//    스탯·설명·추가 옵션은 카드를 누르면 InfoTooltipUI 로 뜬다.
// ============================================================

public static class RunShopPopupCreator
{
    const string SavePath       = "Assets/_project/2.Prefabs/UI/RunShopPopup.prefab";
    const string RewardCardPath = "Assets/_project/2.Prefabs/UI/RewardCard.prefab";
    const string GoldIconPath   = "Assets/_project/3.Textures/Icons/Items/item_gold.png";
    const string SpriteCollPath =
        "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset";

    // ── 치수 (로비 캔버스 1920×1080 전체) ────────────────────────
    const float SidePad = 40f;     // 콘텐츠 좌우 여백 → 실제 폭 1840

    const float HeaderH   = 120f;
    const float TagH      = 34f;
    const float DivH      = 38f;
    const float CellGap   = 16f;

    // 상품이 위, 용병이 아래.
    //  툴팁은 아이콘 아래로 펼쳐지므로, 눌러 보는 칸(장비·특성)을 위쪽에 둬야
    //  펼칠 자리가 남는다. 아래에 두면 매번 화면 밖으로 나가 뒤집혀야 했다.
    const float GoodsY    = 134f;
    const float GoodsH    = 344f;

    const float MercDivY  = 494f;
    const float MercY     = 540f;
    const float MercH     = 513f;   // 하단 여백 27 (1080 - 540 - 513)
    const float BlockTagH = 40f;
    const float BlockGap  = 56f;    // 장비 ↔ 특성 블록 사이

    // 상품 칸 내부 (블록 헤더 아래 기준, 칸 높이 298)
    const float GoodsSlotTop = BlockTagH + 6f;
    const float GoodsSlotH   = GoodsH - GoodsSlotTop;
    const float CardSize     = 127f;
    const float CardTop      = 10f;
    const float NameY        = 143f;
    const float KindY        = 188f;
    const float BuyBtmPad    = 10f;

    // 용병 칸 내부
    const float MercPad    = 12f;
    const float PortraitH  = 150f;
    const float MercNameY  = 156f;   // CardArea 기준
    const float MercStatY  = 215f;   // CardArea 기준
    const float StatRowH   = 44f;
    const float StatRowGap = 4f;
    const float HireBtmPad = 14f;

    static readonly float BuyBtnH  = UIScale.BtnFor(UIScale.FontSm);   // 58
    static readonly float HireBtnH = UIScale.BtnFor(UIScale.FontMd);   // 72
    static readonly float HeadBtnH = 76f;

    // 용병 카드 본체 높이 — 칸에서 위아래 여백과 고용 버튼 자리를 뺀 나머지
    static readonly float MercCardH = MercH - MercPad - (HireBtmPad + HireBtnH + 12f);

    // ── 색상 팔레트 — 행상인(금빛) 계열, EventPopup 과 같은 어두운 남색 바탕 ──
    static readonly Color BgOverlay    = new Color(0f,     0f,     0f,     0.78f);
    static readonly Color PanelBg      = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color HeaderBg     = new Color(0.15f,  0.115f, 0.06f,  1f);
    static readonly Color AccentGold   = new Color(0.90f,  0.70f,  0.30f,  1f);
    static readonly Color TagColor     = new Color(1.00f,  0.85f,  0.45f,  1f);
    static readonly Color TitleColor   = new Color(1.00f,  0.90f,  0.62f,  1f);
    static readonly Color TitleShadow  = new Color(0.05f,  0.03f,  0.01f,  0.85f);

    static readonly Color DividerLine  = new Color(0.30f,  0.27f,  0.22f,  0.85f);
    static readonly Color DividerLabel = new Color(0.78f,  0.72f,  0.58f,  1f);

    static readonly Color CardPit      = new Color(0.055f, 0.06f,  0.10f,  1f);
    static readonly Color NameColor    = new Color(0.98f,  0.98f,  1.00f,  1f);
    static readonly Color GoldColor    = new Color(1.00f,  0.86f,  0.30f,  1f);
    static readonly Color GoldPillBg   = new Color(0.05f,  0.045f, 0.03f,  1f);

    static readonly Color BuyBtnC      = new Color(0.12f,  0.52f,  0.40f,  1f);
    static readonly Color HireBtnC     = new Color(0.52f,  0.30f,  0.14f,  1f);
    static readonly Color RefreshBtnC  = new Color(0.38f,  0.22f,  0.60f,  1f);
    static readonly Color CloseBtnC    = new Color(0.50f,  0.14f,  0.14f,  1f);

    static readonly Color SoldPlateBg  = new Color(0.16f,  0.17f,  0.24f,  1f);
    static readonly Color SoldTextC    = new Color(0.72f,  0.76f,  0.88f,  1f);
    static readonly Color BlindBg      = new Color(0.035f, 0.040f, 0.075f, 0.86f);

    // 장비 ↔ 특성 — 배경 틴트까지 다르게 줘야 "같은 칸 6개" 로 안 보인다
    static readonly Color EquipTagC    = new Color(0.62f,  0.78f,  1.00f,  1f);
    static readonly Color TraitTagC    = new Color(0.82f,  0.64f,  1.00f,  1f);
    static readonly Color EquipBlockBg = new Color(0.070f, 0.090f, 0.150f, 1f);
    static readonly Color TraitBlockBg = new Color(0.100f, 0.070f, 0.140f, 1f);
    static readonly Color EquipSlotBg  = new Color(0.105f, 0.130f, 0.205f, 1f);
    static readonly Color TraitSlotBg  = new Color(0.145f, 0.100f, 0.190f, 1f);
    static readonly Color EquipTagBar  = new Color(0.145f, 0.215f, 0.350f, 1f);
    static readonly Color TraitTagBar  = new Color(0.215f, 0.145f, 0.320f, 1f);

    // 용병 카드
    static readonly Color MercCardBg   = new Color(0.115f, 0.125f, 0.200f, 1f);
    static readonly Color MercSlotBg   = new Color(0.085f, 0.092f, 0.155f, 1f);
    static readonly Color StatRowBg    = new Color(0.070f, 0.078f, 0.135f, 1f);
    static readonly Color ChipBg       = new Color(0.03f,  0.035f, 0.06f,  0.85f);
    static readonly Color StatLabelC   = new Color(0.62f,  0.65f,  0.78f,  1f);
    static readonly Color JobChipC     = new Color(0.86f,  0.90f,  1.00f,  1f);

    // ══════════════════════════════════════════════════════════
    //  진입점
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Popup + "RunShop", priority = ProjectKMenu.PrefabPrio + 39)]
    public static void Create()
    {
        var rewardCard = AssetDatabase.LoadAssetAtPath<GameObject>(RewardCardPath);
        if (rewardCard == null)
        {
            Debug.LogError("[RunShopPopupCreator] RewardCard.prefab 이 없습니다 — " +
                           "Tools > Project K > 프리팹 생성 > 인게임 > RewardCard 를 먼저 실행하세요.");
            return;
        }
        var cardUi = rewardCard.GetComponent<RewardCardUI>();

        // ── 루트 = 캔버스 전체. 패널도 전체를 덮는다.
        //    (예전엔 1840×970 상자를 가운데 띄웠다 — 세로 110px 을 놀리고 있었다)
        var root = new GameObject("RunShopPopup", typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = BgOverlay;
        Stretch(root);
        // PopupBase.Awake 가 GetComponent<CanvasGroup>() 로 잡아 알파를 건드린다.
        // 없으면 여는 순간 NRE — 다른 팝업 Creator 의 CreateRoot 와 같은 순서로 붙인다.
        root.AddComponent<CanvasGroup>();
        var popup = root.AddComponent<RunShopPopup>();

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        Stretch(panel);

        // ══ 헤더 ══════════════════════════════════════════════
        var (goldTmp, refreshBtn, refreshCostTmp, closeBtn) = BuildHeader(panel);

        // ══ 상품 — 장비 블록 ┃ 특성 블록 ══════════════════════
        var goodsArea = Go("GoodsArea", panel);
        AnchorTop(goodsArea, GoodsY, GoodsH, SidePad * 2f);
        {
            var hlg = goodsArea.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.UpperCenter;
            hlg.spacing                = BlockGap;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
        }

        var goodsSlots = new RunShopGoodsSlot[RunShopData.EquipSlots + RunShopData.TraitSlots];

        var equipRow = BuildGoodsBlock(goodsArea, "EquipBlock", "장 비",
            RunShopData.EquipSlots, EquipBlockBg, EquipTagBar, EquipTagC);
        for (int i = 0; i < RunShopData.EquipSlots; i++)
            goodsSlots[i] = BuildGoodsSlot(equipRow, $"GoodsSlot_{i}", cardUi, EquipSlotBg);

        var traitRow = BuildGoodsBlock(goodsArea, "TraitBlock", "특 성",
            RunShopData.TraitSlots, TraitBlockBg, TraitTagBar, TraitTagC);
        for (int i = 0; i < RunShopData.TraitSlots; i++)
            goodsSlots[RunShopData.EquipSlots + i] =
                BuildGoodsSlot(traitRow, $"GoodsSlot_{RunShopData.EquipSlots + i}", cardUi, TraitSlotBg);

        // ══ 용병 ══════════════════════════════════════════════
        BuildDivider(panel, MercDivY, "용 병 고 용");

        var mercRow = Go("MercRow", panel);
        AnchorTop(mercRow, MercY, MercH, SidePad * 2f);
        EvenRow(mercRow, CellGap);

        var genSlots = new RunShopGeneralSlot[RunShopData.GeneralSlots];
        for (int i = 0; i < genSlots.Length; i++)
            genSlots[i] = BuildGeneralSlot(mercRow, $"GeneralSlot_{i}");

        // ── 필드 연결 ─────────────────────────────────────────
        var so = new SerializedObject(popup);
        so.Update();
        SetEnum(so, "_popupType", (int)PopupType.RunShop);
        EditorUIBuilder.SetObjArray(so, "_goodsSlots",   goodsSlots, "RunShopPopupCreator");
        EditorUIBuilder.SetObjArray(so, "_generalSlots", genSlots,   "RunShopPopupCreator");
        SetObj(so, "_goldText",        goldTmp);
        SetObj(so, "_refreshBtn",      refreshBtn);
        SetObj(so, "_refreshCostText", refreshCostTmp);
        SetObj(so, "_closeBtn",        closeBtn);
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, SavePath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RunShopPopupCreator] 저장: {SavePath} — PopupManager > Load Popup Prefabs 실행 필요.");
    }

    // ══════════════════════════════════════════════════════════
    //  헤더 — 좌: 태그 + 타이틀 / 우: 보유 골드 · 새로고침 · 닫기
    // ══════════════════════════════════════════════════════════

    static (TextMeshProUGUI gold, Button refresh, TextMeshProUGUI refreshCost, Button close)
        BuildHeader(GameObject panel)
    {
        var header = Go("Header", panel);
        header.AddComponent<Image>().color = HeaderBg;
        AnchorTop(header, 0f, HeaderH);

        // "상 점" 태그 — ★ 는 폰트에 없으므로 마름모 도형으로 대체
        var tagRoot = Go("ShopTag", header);
        var tagRt = tagRoot.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot     = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(30f, -14f);
        tagRt.sizeDelta        = new Vector2(300f, TagH);

        var diamond = EditorUIBuilder.Diamond(tagRoot, "Mark", 16f, TagColor);
        var dRt = diamond.GetComponent<RectTransform>();
        dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(10f, 0f);

        var tagTmp = TMP(tagRoot, "Label", "상  점", UIScale.FontSm, FontStyles.Bold);
        tagTmp.color         = TagColor;
        tagTmp.alignment     = TextAlignmentOptions.Left;
        tagTmp.raycastTarget = false;
        var tlRt = tagTmp.rectTransform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = new Vector2(30f, 0f); tlRt.offsetMax = Vector2.zero;

        // 타이틀 — 그림자 사본을 먼저 깔아 어떤 배경에서도 읽히게 한다
        MakeTitle(header, "TitleShadow", TitleShadow, 3f);
        MakeTitle(header, "TitleText",   TitleColor,  0f);

        var accentLine = Go("AccentLine", panel);
        accentLine.AddComponent<Image>().color = AccentGold;
        AnchorTop(accentLine, HeaderH, 3f);

        // ── 우측 버튼 묶음 (오른쪽부터 쌓는다) ────────────────
        float x = -24f;

        var closeBtn = BuildCloseBtn(header, x);
        x -= HeadBtnH + 16f;

        var (refreshBtn, refreshCostTmp) = BuildRefreshBtn(header, x);
        x -= RefreshW + 16f;

        var goldTmp = BuildGoldPill(header, x);

        return (goldTmp, refreshBtn, refreshCostTmp, closeBtn);
    }

    static void MakeTitle(GameObject header, string name, Color color, float dy)
    {
        var tmp = TMP(header, name, "행상인의 좌판", UIScale.FontLg, FontStyles.Bold);
        tmp.color         = color;
        tmp.alignment     = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f + dy, -(TagH + 14f) - dy);
        rt.sizeDelta        = new Vector2(700f, UIScale.RowLg);
    }

    // 보유 골드 표시 — 팝업이 화면을 덮어 TopBar 가 안 보이므로 여기 다시 둔다.
    static TextMeshProUGUI BuildGoldPill(GameObject header, float rightX)
    {
        const float W = 300f, H = 64f;

        var pill = Go("GoldPill", header);
        pill.AddComponent<Image>().color = GoldPillBg;
        AnchorRight(pill, rightX, W, H);
        EditorUIBuilder.CostHlg(pill);

        var icon = Go("GoldIcon", pill, typeof(Image)).GetComponent<Image>();
        icon.sprite         = LoadGold();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        EditorUIBuilder.IconLE(icon, 40f);

        var tmp = TMP(pill, "GoldText", "0", UIScale.FontMd, FontStyles.Bold);
        tmp.color            = GoldColor;
        tmp.alignment        = TextAlignmentOptions.Right;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        tmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        return tmp;
    }

    // 새로고침 버튼 — 예전엔 preferredWidth 가 글자 폭보다 작아
    // "100" 이 "10/0" 으로, "새로고침" 이 두 줄로 접혔다. 폭을 넉넉히 잡는다.
    const float RefreshW    = 380f;
    const float RefCostW    = 140f;   // "1,000" 4~5자리
    const float RefLabelW   = 150f;   // "새로고침" 4자 × FontSm

    static (Button btn, TextMeshProUGUI cost) BuildRefreshBtn(GameObject header, float rightX)
    {
        var btn = EditorUIBuilder.RaisedBtn(header, "RefreshBtn", RefreshBtnC, out var btnBody);
        AnchorRight(btn.gameObject, rightX, RefreshW, HeadBtnH);

        var body = BtnContent(btnBody);
        var hlg = body.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment          = TextAnchor.MiddleCenter;
        hlg.spacing                 = 8f;
        hlg.padding                 = new RectOffset(12, 12, 0, 0);
        hlg.childControlWidth       = true;
        hlg.childControlHeight      = true;
        hlg.childForceExpandWidth   = false;
        hlg.childForceExpandHeight  = true;

        var icon = Go("GoldIcon", body, typeof(Image)).GetComponent<Image>();
        icon.sprite         = LoadGold();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        EditorUIBuilder.IconLE(icon, 34f);

        var cost = TMP(body, "CostText", "100", UIScale.FontMd, FontStyles.Bold);
        cost.color            = GoldColor;
        cost.alignment        = TextAlignmentOptions.Right;
        cost.raycastTarget    = false;
        cost.textWrappingMode = TextWrappingModes.NoWrap;
        cost.overflowMode     = TextOverflowModes.Overflow;
        cost.gameObject.AddComponent<LayoutElement>().preferredWidth = RefCostW;

        var label = TMP(body, "Label", "새로고침", UIScale.FontSm, FontStyles.Bold);
        label.alignment        = TextAlignmentOptions.Left;
        label.raycastTarget    = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode     = TextOverflowModes.Overflow;
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = RefLabelW;

        return (btn, cost);
    }

    static Button BuildCloseBtn(GameObject header, float rightX)
    {
        var btn = EditorUIBuilder.RaisedBtn(header, "CloseBtn", CloseBtnC, out var body);
        AnchorRight(btn.gameObject, rightX, HeadBtnH, HeadBtnH);
        // ✕ 글리프는 폰트에 없다 (□ 로 렌더됨) → 도형으로 그린다
        var mark = EditorUIBuilder.XMark(body, "Mark", UIScale.FontMd, Color.white);
        Center(mark);
        return btn;
    }

    // ══════════════════════════════════════════════════════════
    //  상품 블록 — 장비 / 특성을 통째로 갈라놓는다
    // ══════════════════════════════════════════════════════════
    //  칸 6개를 같은 간격으로 늘어놓으면 어디까지가 장비인지 안 읽힌다.
    //  블록마다 ① 배경 틴트 ② 색 헤더바 ③ 56px 간격 세 가지를 다르게 준다.
    //  블록 폭은 칸 수에 비례(4:2)하게 두어 칸 크기는 6개 모두 같다.
    //  반환값 = 칸을 담을 Row (HLG 균등 분배).

    static GameObject BuildGoodsBlock(GameObject parent, string name, string label,
                                      int cellCount, Color blockBg, Color tagBar, Color tagText)
    {
        var block = Go(name, parent, typeof(Image));
        block.GetComponent<Image>().color = blockBg;
        var le = block.AddComponent<LayoutElement>();
        le.flexibleWidth  = cellCount;   // 장비 4 : 특성 2
        le.flexibleHeight = 1f;

        // ── 헤더 바 ───────────────────────────────────────────
        var tag = Go("BlockTag", block, typeof(Image));
        tag.GetComponent<Image>().color = tagBar;
        AnchorTop(tag, 0f, BlockTagH);

        var mark = Go("Mark", tag, typeof(Image));
        var markImg = mark.GetComponent<Image>();
        markImg.color         = tagText;
        markImg.raycastTarget = false;
        {
            var rt = mark.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(0f, 0f); rt.offsetMax = new Vector2(6f, 0f);
        }

        var tmp = TMP(tag, "Label", $"{label}   {cellCount}", UIScale.FontSm, FontStyles.Bold);
        tmp.color            = tagText;
        tmp.alignment        = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        {
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 0f); rt.offsetMax = new Vector2(-12f, 0f);
        }

        // ── 칸 줄 ─────────────────────────────────────────────
        var row = Go("Row", block);
        AnchorTop(row, GoodsSlotTop, GoodsSlotH, 20f);
        EvenRow(row, CellGap);
        return row;
    }

    // ══════════════════════════════════════════════════════════
    //  상품 칸 — RewardCard + 이름 + 종류 + 가격 버튼
    // ══════════════════════════════════════════════════════════

    static RunShopGoodsSlot BuildGoodsSlot(GameObject parent, string name,
                                           RewardCardUI cardPrefab, Color slotBg)
    {
        var go = Go(name, parent, typeof(Image));
        go.GetComponent<Image>().color = slotBg;
        var slot = go.AddComponent<RunShopGoodsSlot>();
        Flex(go);

        // 카드 자리 — 어두운 홈을 파서 카드가 얹혀 있는 것으로 읽히게 한다
        var pit = Go("CardPit", go);
        pit.AddComponent<Image>().color = CardPit;
        var pitRt = pit.GetComponent<RectTransform>();
        pitRt.anchorMin = pitRt.anchorMax = new Vector2(0.5f, 1f);
        pitRt.pivot     = new Vector2(0.5f, 1f);
        pitRt.anchoredPosition = new Vector2(0f, -CardTop);
        pitRt.sizeDelta        = new Vector2(CardSize, CardSize);

        var holder = Go("CardHolder", pit);
        Stretch(holder);

        // 이름 — 한 줄 고정. 칸(RowSm)보다 한 줄이 커지면 통째로 사라지므로
        // AutoSize 로 줄여서 담는다 (UI 규칙 5).
        var nameTmp = TMP(go, "NameText", "상품 이름", UIScale.FontSm, FontStyles.Bold);
        nameTmp.color            = NameColor;
        nameTmp.alignment        = TextAlignmentOptions.Center;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Overflow;
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontSm - 10f;
        nameTmp.fontSizeMax      = UIScale.FontSm;
        AnchorTop(nameTmp.gameObject, NameY, UIScale.RowSm, 16f);

        var kindTmp = TMP(go, "KindText", "일반 장비", UIScale.FontSm, FontStyles.Normal);
        kindTmp.alignment        = TextAlignmentOptions.Center;
        kindTmp.raycastTarget    = false;
        kindTmp.textWrappingMode = TextWrappingModes.NoWrap;
        kindTmp.overflowMode     = TextOverflowModes.Overflow;
        kindTmp.enableAutoSizing = true;
        kindTmp.fontSizeMin      = UIScale.FontSm - 10f;
        kindTmp.fontSizeMax      = UIScale.FontSm;
        AnchorTop(kindTmp.gameObject, KindY, UIScale.RowSm, 16f);

        // ── 구매 버튼 (입체) ─────────────────────────────────
        var buyBtn = EditorUIBuilder.RaisedBtn(go, "BuyBtn", BuyBtnC, out var body);
        AnchorBottom(buyBtn.gameObject, BuyBtmPad, BuyBtnH, 16f);
        BuildCostRow(body, "구매", UIScale.FontSm);

        // ── 품절 판 (버튼과 같은 자리) ───────────────────────
        var (plate, soldTmp) = BuildSoldPlate(go, BuyBtmPad, BuyBtnH, 16f);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_cardPrefab", cardPrefab);
        SetObj(sSo, "_cardHolder", holder.GetComponent<RectTransform>());
        SetObj(sSo, "_nameText",   nameTmp);
        SetObj(sSo, "_kindText",   kindTmp);
        SetObj(sSo, "_costText",   FindTMP(body, "CostText"));
        SetObj(sSo, "_buyBtn",     buyBtn);
        SetObj(sSo, "_soldPlate",  plate);
        SetObj(sSo, "_soldText",   soldTmp);
        sSo.ApplyModifiedProperties();
        return slot;
    }

    // ══════════════════════════════════════════════════════════
    //  용병 칸 — HeroCard + 고용 버튼
    // ══════════════════════════════════════════════════════════

    //  칸 ≈352×513. 공용 HeroCard(가로형 360×170)를 쓰지 않는 이유는
    //  파일 상단 주석 참고 — 이름 칸 103px, 스탯 값 칸 58px 이 근본 원인이었다.
    //  여기서는 세로로 세워 이름은 카드 폭(≈328)을 통째로, 스탯은 한 행에
    //  하나씩 [라벨 좌][값 우] 로 둔다. 값이 6자리가 되어도 겹칠 자리가 없다.
    static RunShopGeneralSlot BuildGeneralSlot(GameObject parent, string name)
    {
        var go = Go(name, parent, typeof(Image));
        go.GetComponent<Image>().color = MercSlotBg;
        var slot = go.AddComponent<RunShopGeneralSlot>();
        Flex(go);

        // ── 카드 본체 (전체가 상세 보기 버튼) ────────────────
        var cardGo = Go("CardArea", go, typeof(Image));
        var cardImg = cardGo.GetComponent<Image>();
        cardImg.color = MercCardBg;
        AnchorTop(cardGo, MercPad, MercCardH, MercPad * 2f);

        var cardBtn = cardGo.AddComponent<Button>();
        cardBtn.targetGraphic = cardImg;
        var cb = cardBtn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.10f, 1.10f, 1.15f, 1f);
        cb.pressedColor     = new Color(0.82f, 0.82f, 0.88f, 1f);
        cb.fadeDuration     = 0.08f;
        cardBtn.colors = cb;

        // ── 초상화 + 직업·등급 배지 ──────────────────────────
        var portraitBg = Go("PortraitBg", cardGo, typeof(Image)).GetComponent<Image>();
        portraitBg.color = new Color(0.16f, 0.27f, 0.56f, 1f);
        AnchorTop(portraitBg.gameObject, 0f, PortraitH);

        var portraitImg = Go("PortraitImage", portraitBg.gameObject, typeof(Image)).GetComponent<Image>();
        portraitImg.color          = Color.white;
        portraitImg.preserveAspect = true;
        Stretch(portraitImg.gameObject);

        var jobTmp   = BuildPortraitChip(portraitBg.gameObject, "JobChip",   "JobText",
                                         "기사", JobChipC,  left: true);
        var gradeTmp = BuildPortraitChip(portraitBg.gameObject, "GradeChip", "GradeText",
                                         "일반", Color.white, left: false);

        // ── 이름 — 카드 폭을 통째로 쓴다 (잘림의 원인이던 0.70 분할 제거) ──
        var nameTmp = TMP(cardGo, "NameText", "용병 이름", UIScale.FontMd, FontStyles.Bold);
        nameTmp.color            = NameColor;
        nameTmp.alignment        = TextAlignmentOptions.Center;
        nameTmp.raycastTarget    = false;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode     = TextOverflowModes.Overflow;   // Ellipsis 는 줄을 통째로 버린다
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin      = UIScale.FontSm;
        nameTmp.fontSizeMax      = UIScale.FontMd;
        AnchorTop(nameTmp.gameObject, MercNameY, UIScale.RowMd, 20f);

        // ── 스탯 4행 — 한 행에 하나씩, 라벨 좌 / 값 우 ───────
        var hpTmp   = BuildStatRow(cardGo, 0, "Hp",      "체 력", StatColors.Hp);
        var atkTmp  = BuildStatRow(cardGo, 1, "Atk",     "공 격", StatColors.Atk);
        var defTmp  = BuildStatRow(cardGo, 2, "Def",     "방 어", StatColors.Def);
        var sldTmp  = BuildStatRow(cardGo, 3, "Soldier", "병 사", StatColors.Soldier);

        // ── 초상화 렌더용 빌더 (비활성 — 화면에 그리지 않는다) ──
        var preview = Go("PortraitPreview", cardGo);
        preview.SetActive(false);
        var bridge  = preview.AddComponent<UnitAppearanceBridge>();
        var builder = preview.GetComponent<CharacterBuilder>();
        if (builder != null)
        {
            var sc = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollPath);
            if (sc != null) builder.SpriteCollection = sc;
        }

        // 카드 본체만 클릭을 받는다 — 자식이 레이캐스트를 먹으면 상세가 안 열린다
        foreach (var img in cardGo.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
        foreach (var tmp in cardGo.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.raycastTarget = false;
        cardImg.raycastTarget = true;

        // ── 고용 버튼 (입체) ─────────────────────────────────
        var hireBtn = EditorUIBuilder.RaisedBtn(go, "HireBtn", HireBtnC, out var body);
        AnchorBottom(hireBtn.gameObject, HireBtmPad, HireBtnH, 24f);
        BuildCostRow(body, "고용", UIScale.FontMd);

        var (plate, soldTmp) = BuildBlindPlate(go);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_portraitBg",     portraitBg);
        SetObj(sSo, "_portraitImg",    portraitImg);
        SetObj(sSo, "_portraitBridge", bridge);
        SetObj(sSo, "_nameText",       nameTmp);
        SetObj(sSo, "_jobText",        jobTmp);
        SetObj(sSo, "_gradeText",      gradeTmp);
        SetObj(sSo, "_hpText",         hpTmp);
        SetObj(sSo, "_atkText",        atkTmp);
        SetObj(sSo, "_defText",        defTmp);
        SetObj(sSo, "_soldierText",    sldTmp);
        SetObj(sSo, "_costText",       FindTMP(body, "CostText"));
        SetObj(sSo, "_buyBtn",         hireBtn);
        SetObj(sSo, "_cardBtn",        cardBtn);
        SetObj(sSo, "_soldOut",        plate);
        SetObj(sSo, "_soldText",       soldTmp);
        sSo.ApplyModifiedProperties();
        return slot;
    }

    // ── 초상화 위 직업·등급 배지 ─────────────────────────────
    //  카드 세로를 아끼려고 별도 행 대신 초상화 모서리에 얹는다.
    static TextMeshProUGUI BuildPortraitChip(GameObject portrait, string chipName,
                                             string tmpName, string text, Color color, bool left)
    {
        const float W = 108f, H = 38f, Pad = 8f;

        var chip = Go(chipName, portrait, typeof(Image));
        chip.GetComponent<Image>().color = ChipBg;
        var rt = chip.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
        rt.pivot     = new Vector2(left ? 0f : 1f, 1f);
        rt.anchoredPosition = new Vector2(left ? Pad : -Pad, -Pad);
        rt.sizeDelta        = new Vector2(W, H);

        var tmp = TMP(chip, tmpName, text, UIScale.FontSm, FontStyles.Bold);
        tmp.color            = color;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = UIScale.FontSm - 8f;
        tmp.fontSizeMax      = UIScale.FontSm;
        Stretch(tmp.gameObject);
        return tmp;
    }

    // ── 스탯 한 행 ───────────────────────────────────────────
    //  라벨은 왼쪽 고정폭, 값은 남는 폭 전부를 오른쪽 정렬로 쓴다.
    //  둘 다 AutoSize 를 끄고 같은 크기(FontSm)로 못 박는다 —
    //  칸마다 제각각 줄어들어 글자 크기가 어긋나던 문제의 원인이었다.
    //  값 칸이 200px 넘게 남으므로 "999,999" 도 줄이지 않고 들어간다.
    static TextMeshProUGUI BuildStatRow(GameObject card, int index, string id,
                                        string label, Color valueColor)
    {
        const float LabelW = 100f;

        float y = MercStatY + index * (StatRowH + StatRowGap);

        var row = Go($"Stat_{id}", card, typeof(Image));
        var rowImg = row.GetComponent<Image>();
        rowImg.color         = StatRowBg;
        rowImg.raycastTarget = false;
        AnchorTop(row, y, StatRowH, 20f);

        var lbl = TMP(row, $"{id}Label", label, UIScale.FontSm, FontStyles.Normal);
        lbl.color            = StatLabelC;
        lbl.alignment        = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Overflow;
        {
            var rt = lbl.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(14f, 0f);
            rt.offsetMax = new Vector2(14f + LabelW, 0f);
        }

        var val = TMP(row, $"{id}Text", "—", UIScale.FontSm, FontStyles.Bold);
        val.color            = valueColor;
        val.alignment        = TextAlignmentOptions.MidlineRight;
        val.raycastTarget    = false;
        val.textWrappingMode = TextWrappingModes.NoWrap;
        val.overflowMode     = TextOverflowModes.Overflow;
        {
            var rt = val.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(14f + LabelW + 8f, 0f);
            rt.offsetMax = new Vector2(-14f, 0f);
        }
        return val;
    }

    // ══════════════════════════════════════════════════════════
    //  공통 조각
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 입체 버튼의 Body 안에 내용 컨테이너를 만든다.
    /// ⚠ Body 에는 이미 TopEdge/BottomEdge(음각 선)가 자식으로 들어 있다.
    ///   Body 에 직접 레이아웃 그룹을 붙이면 그 선들까지 정렬 대상이 되어 뭉개진다.
    ///   반드시 이 컨테이너를 한 겹 두고 그 안에 배치할 것.
    /// </summary>
    static GameObject BtnContent(GameObject body)
    {
        var content = Go("Content", body);
        Stretch(content);
        return content;
    }

    //  [금화][가격][라벨] — 폭을 preferredWidth 로 못 박지 않고
    //  내용에 맞춰 흐르게 둔다. 글자가 잘리는 대신 가운데 정렬이 살짝 밀릴 뿐이다.
    static void BuildCostRow(GameObject btnBody, string label, float font)
    {
        var body = BtnContent(btnBody);
        var hlg = body.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 8f;
        hlg.padding                = new RectOffset(12, 12, 0, 0);
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        var icon = Go("GoldIcon", body, typeof(Image)).GetComponent<Image>();
        icon.sprite         = LoadGold();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        EditorUIBuilder.IconLE(icon, font * 0.8f);

        var cost = TMP(body, "CostText", "0", font, FontStyles.Bold);
        cost.color            = GoldColor;
        cost.alignment        = TextAlignmentOptions.Right;
        cost.raycastTarget    = false;
        cost.textWrappingMode = TextWrappingModes.NoWrap;
        cost.overflowMode     = TextOverflowModes.Overflow;

        var lbl = TMP(body, "Label", label, UIScale.FontSm, FontStyles.Bold);
        lbl.alignment        = TextAlignmentOptions.Left;
        lbl.raycastTarget    = false;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;
        lbl.overflowMode     = TextOverflowModes.Overflow;
    }

    //  용병 칸 블라인드 — 칸 전체를 반투명하게 덮고 사유를 크게 얹는다.
    //  버튼 자리만 덮던 예전 방식은 배치 슬롯이 가득 찼을 때
    //  빈 카드(흰 상자)가 그대로 남아 무슨 상태인지 읽히지 않았다.
    //  반투명이라 고용 완료 시에는 누구를 뽑았는지 비쳐 보인다.
    static (GameObject plate, TextMeshProUGUI label) BuildBlindPlate(GameObject parent)
    {
        var plate = Go("SoldPlate", parent);
        var img = plate.AddComponent<Image>();
        img.color = BlindBg;
        // 고용 완료 후에도 카드를 눌러 상세를 볼 수 있어야 한다 — 클릭은 통과시킨다.
        // (고용 불가 칸은 Setup 에서 _cardBtn.interactable=false 라 눌러도 안 열린다)
        img.raycastTarget = false;
        Stretch(plate);

        // 라벨은 카드 위쪽(초상화 자리)에 — 아래 버튼 자리가 아니라
        var tmp = TMP(plate, "Label", "고용 불가", UIScale.FontLg, FontStyles.Bold);
        tmp.color            = SoldTextC;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        {
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, MercH * 0.16f);
            rt.sizeDelta        = new Vector2(0f, UIScale.RowLg);
        }

        // 아래 어딘가에 깔리지 않게 항상 맨 위로
        plate.transform.SetAsLastSibling();
        plate.SetActive(false);
        return (plate, tmp);
    }

    //  품절 판 — 버튼과 정확히 같은 자리를 덮는다.
    //  카드까지 덮지 않는 이유: 무엇을 샀는지 눌러서 다시 볼 수 있어야 한다.
    static (GameObject plate, TextMeshProUGUI label) BuildSoldPlate(
        GameObject parent, float bottom, float h, float padH)
    {
        var plate = Go("SoldPlate", parent);
        plate.AddComponent<Image>().color = SoldPlateBg;
        AnchorBottom(plate, bottom, h, padH);

        var tmp = TMP(plate, "Label", "구매 완료", UIScale.FontSm, FontStyles.Bold);
        tmp.color            = SoldTextC;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        Stretch(tmp.gameObject);

        plate.SetActive(false);
        return (plate, tmp);
    }

    //  섹션 구분선 — 가운데 글자 + 좌우 라인 (EventPopup 의 "선 택" 과 같은 형태)
    static void BuildDivider(GameObject panel, float y, string label)
    {
        var div = Go($"Divider_{label}", panel);
        AnchorTop(div, y, DivH, SidePad);

        DivLine(div, "LineL", 0f,   0.5f,   0f, -110f);
        DivLine(div, "LineR", 0.5f, 1f,  110f,    0f);

        var tmp = TMP(div, "Label", label, UIScale.FontSm, FontStyles.Bold);
        tmp.color         = DividerLabel;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        Stretch(tmp.gameObject);
    }

    static void DivLine(GameObject parent, string name,
                        float aMinX, float aMaxX, float offMinX, float offMaxX)
    {
        var go = Go(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = DividerLine;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, 0.5f);
        rt.anchorMax = new Vector2(aMaxX, 0.5f);
        rt.offsetMin = new Vector2(offMinX, -1f);
        rt.offsetMax = new Vector2(offMaxX,  1f);
    }

    // ── 레이아웃 헬퍼 ────────────────────────────────────────

    //  칸을 균등 분배하는 가로 줄. 칸 폭을 손으로 계산하지 않는다.
    static void EvenRow(GameObject go, float spacing)
    {
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = spacing;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
    }

    static void Flex(GameObject go)
    {
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1f;
        le.flexibleHeight = 1f;
    }

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

    static TextMeshProUGUI TMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void Stretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static TextMeshProUGUI FindTMP(GameObject root, string cname)
        => EditorUIBuilder.FindDeep<TextMeshProUGUI>(root.transform, cname);

    static Sprite LoadGold() => AssetDatabase.LoadAssetAtPath<Sprite>(GoldIconPath);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "RunShopPopupCreator");

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "RunShopPopupCreator");
}
