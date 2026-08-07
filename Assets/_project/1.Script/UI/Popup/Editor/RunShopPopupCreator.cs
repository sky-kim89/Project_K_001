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
//  ■ 왜 다시 짰나 (이전 레이아웃의 문제)
//    · 장비 4칸이 2×2 로 접혀 특성 블록 옆이 통째로 비었다.
//    · 장비 스탯을 칸 안에 다 적으려다 폭이 모자라 글자가 접혔다.
//      (추가 옵션 트리거까지 붙으면 어떤 폭으로도 안 들어간다)
//    · 구매/새로고침 버튼의 preferredWidth 가 글자 폭보다 작아
//      "구매" → "구/매", "100" → "10/0" 처럼 두 줄로 깨졌다.
//    · 평평한 사각형 버튼이라 누를 수 있는지 읽히지 않았다 (UI 규칙 1 위반).
//
//  ■ 새 레이아웃 — 한 줄에 한 종류, 상세는 카드 툴팁으로
//    Header        H=136  ◆ 상 점 / 타이틀 | 보유골드 · 새로고침 · 닫기
//    AccentLine    H=3
//    GoodsDivider  H=36   "상  품"
//    GroupTags     H=34   [장비 4칸] [특성 2칸] 구간 라벨
//    GoodsRow      H=368  RunShopGoodsSlot × 6 (HLG 균등 분배)
//    MercDivider   H=36   "용 병 고 용"
//    MercRow       H=272  RunShopGeneralSlot × 5 (HLG 균등 분배)
//
//    상품 칸은 RewardCard(전투 결과·이벤트와 같은 카드) + 이름 + 가격뿐이다.
//    스탯·설명·추가 옵션은 카드를 누르면 InfoTooltipUI 로 뜬다.
// ============================================================

public static class RunShopPopupCreator
{
    const string SavePath       = "Assets/_project/2.Prefabs/UI/RunShopPopup.prefab";
    const string RewardCardPath = "Assets/_project/2.Prefabs/UI/RewardCard.prefab";
    const string GoldIconPath   = "Assets/_project/3.Textures/Icons/Items/item_gold.png";

    // ── 치수 ─────────────────────────────────────────────────────
    const float PW      = 1840f;   // 패널 폭 (캔버스 1920 - 좌우 40)
    const float PH      = 970f;    // 패널 높이 (PopupMaxH 1000 이내)
    const float SidePad = 40f;     // 콘텐츠 좌우 여백 → 실제 폭 1760

    const float HeaderH   = 136f;
    const float GoodsDivY = 156f;
    const float TagY      = 198f;
    const float TagH      = 34f;
    const float GoodsY    = 240f;
    const float GoodsH    = 368f;
    const float MercDivY  = 624f;
    const float MercY     = 668f;
    const float MercH     = 272f;
    const float DivH      = 36f;
    const float CellGap   = 16f;

    // 상품 칸 내부 (위에서부터)
    const float CardSize  = 160f;
    const float CardTop   = 14f;
    const float NameY     = 182f;
    const float KindY     = 229f;
    const float BuyBtmPad = 16f;

    // 용병 칸 내부
    const float HeroCardH = 170f;
    const float HeroPad   = 6f;
    const float HireBtmPad = 12f;

    static readonly float BuyBtnH  = UIScale.BtnFor(UIScale.FontMd);   // 72 — 라벨이 안 눌리는 최소 높이
    static readonly float HeadBtnH = 76f;

    // ── 색상 팔레트 — 행상인(금빛) 계열, EventPopup 과 같은 어두운 남색 바탕 ──
    static readonly Color BgOverlay    = new Color(0f,     0f,     0f,     0.78f);
    static readonly Color PanelBg      = new Color(0.07f,  0.075f, 0.13f,  1f);
    static readonly Color PanelBorder  = new Color(0.72f,  0.54f,  0.22f,  1f);
    static readonly Color HeaderBg     = new Color(0.15f,  0.115f, 0.06f,  1f);
    static readonly Color AccentGold   = new Color(0.90f,  0.70f,  0.30f,  1f);
    static readonly Color TagColor     = new Color(1.00f,  0.85f,  0.45f,  1f);
    static readonly Color TitleColor   = new Color(1.00f,  0.90f,  0.62f,  1f);
    static readonly Color TitleShadow  = new Color(0.05f,  0.03f,  0.01f,  0.85f);

    static readonly Color DividerLine  = new Color(0.30f,  0.27f,  0.22f,  0.85f);
    static readonly Color DividerLabel = new Color(0.78f,  0.72f,  0.58f,  1f);

    static readonly Color SlotBg       = new Color(0.105f, 0.115f, 0.185f, 1f);
    static readonly Color CardPit      = new Color(0.055f, 0.06f,  0.10f,  1f);
    static readonly Color NameColor    = new Color(0.98f,  0.98f,  1.00f,  1f);
    static readonly Color GoldColor    = new Color(1.00f,  0.86f,  0.30f,  1f);
    static readonly Color GoldPillBg   = new Color(0.05f,  0.045f, 0.03f,  1f);

    static readonly Color BuyBtnC      = new Color(0.12f,  0.52f,  0.40f,  1f);
    static readonly Color HireBtnC     = new Color(0.52f,  0.30f,  0.14f,  1f);
    static readonly Color RefreshBtnC  = new Color(0.38f,  0.22f,  0.60f,  1f);
    static readonly Color CloseBtnC    = new Color(0.50f,  0.14f,  0.14f,  1f);

    static readonly Color SoldPlateBg  = new Color(0.16f,  0.17f,  0.24f,  1f);
    static readonly Color SoldTextC    = new Color(0.58f,  0.62f,  0.74f,  1f);

    static readonly Color EquipTagC    = new Color(0.62f,  0.78f,  1.00f,  1f);
    static readonly Color TraitTagC    = new Color(0.82f,  0.64f,  1.00f,  1f);

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

        // ── 루트 (전체화면 오버레이) ──────────────────────────
        var root = new GameObject("RunShopPopup", typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = BgOverlay;
        Stretch(root);
        var popup = root.AddComponent<RunShopPopup>();

        // ── 테두리 (Panel 의 앞 형제 — 자식으로 두면 팝업을 덮는다) ──
        var border = Go("Border", root);
        border.AddComponent<Image>().color = PanelBorder;
        CenterBox(border, PW + 6f, PH + 6f);

        var panel = Go("Panel", root);
        panel.AddComponent<Image>().color = PanelBg;
        CenterBox(panel, PW, PH);

        // ══ 헤더 ══════════════════════════════════════════════
        var (goldTmp, refreshBtn, refreshCostTmp, closeBtn) = BuildHeader(panel);

        // ══ 상품 ══════════════════════════════════════════════
        BuildDivider(panel, GoodsDivY, "상  품");
        BuildGroupTags(panel);

        var goodsRow = Go("GoodsRow", panel);
        AnchorTop(goodsRow, GoodsY, GoodsH, SidePad);
        EvenRow(goodsRow, CellGap);

        var goodsSlots = new RunShopGoodsSlot[RunShopData.EquipSlots + RunShopData.TraitSlots];
        for (int i = 0; i < goodsSlots.Length; i++)
            goodsSlots[i] = BuildGoodsSlot(goodsRow, $"GoodsSlot_{i}", cardUi);

        // ══ 용병 ══════════════════════════════════════════════
        BuildDivider(panel, MercDivY, "용 병 고 용");

        var mercRow = Go("MercRow", panel);
        AnchorTop(mercRow, MercY, MercH, SidePad);
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
    //  구간 라벨 — [장비 4칸] [특성 2칸]
    // ══════════════════════════════════════════════════════════
    //  칸 폭은 HLG 가 균등 분배하므로 여기서도 같은 식으로 나눈다.

    static void BuildGroupTags(GameObject panel)
    {
        var row = Go("GroupTags", panel);
        AnchorTop(row, TagY, TagH, SidePad);

        const int Total = RunShopData.EquipSlots + RunShopData.TraitSlots;   // 6
        float content   = PW - SidePad;
        float cell      = (content - CellGap * (Total - 1)) / Total;

        float equipW = cell * RunShopData.EquipSlots + CellGap * (RunShopData.EquipSlots - 1);
        float traitW = cell * RunShopData.TraitSlots + CellGap * (RunShopData.TraitSlots - 1);

        BuildGroupTag(row, "EquipTag", "장비",  0f,                 equipW, EquipTagC);
        BuildGroupTag(row, "TraitTag", "특성",  equipW + CellGap,   traitW, TraitTagC);
    }

    static void BuildGroupTag(GameObject parent, string name, string label,
                              float x, float w, Color color)
    {
        var go = Go(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta        = new Vector2(w, TagH);

        var bar = Go("Bar", go);
        var barImg = bar.AddComponent<Image>();
        barImg.color         = color;
        barImg.raycastTarget = false;
        var bRt = bar.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(0f, 1f);
        bRt.offsetMin = new Vector2(0f, 4f); bRt.offsetMax = new Vector2(5f, -4f);

        var tmp = TMP(go, "Label", label, UIScale.FontSm, FontStyles.Bold);
        tmp.color         = color;
        tmp.alignment     = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        var lRt = tmp.rectTransform;
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(16f, 0f); lRt.offsetMax = Vector2.zero;
    }

    // ══════════════════════════════════════════════════════════
    //  상품 칸 — RewardCard + 이름 + 종류 + 가격 버튼
    // ══════════════════════════════════════════════════════════

    static RunShopGoodsSlot BuildGoodsSlot(GameObject parent, string name, RewardCardUI cardPrefab)
    {
        var go = Go(name, parent, typeof(Image));
        go.GetComponent<Image>().color = SlotBg;
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
        BuildCostRow(body, "구매", UIScale.FontMd);

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

    static RunShopGeneralSlot BuildGeneralSlot(GameObject parent, string name)
    {
        var go = Go(name, parent, typeof(Image));
        go.GetComponent<Image>().color = SlotBg;
        var slot = go.AddComponent<RunShopGeneralSlot>();
        Flex(go);

        // HeroCard 는 360×170 설계값 — 칸 폭이 좁으면 스탯 글자가 접힌다.
        // 이 레이아웃에서 칸 폭은 (1760 - 4×16) / 5 ≈ 339 로 설계값에 근접한다.
        var cardGo = HeroPanelCreator.BuildCardPrefab();
        cardGo.name = "CardArea";
        cardGo.transform.SetParent(go.transform, false);
        AnchorTop(cardGo, HeroPad, HeroCardH, HeroPad * 2f);

        if (cardGo.TryGetComponent<HeroCardUI>(out var heroCardUi))
            Object.DestroyImmediate(heroCardUi);

        // 카드 전체가 상세 보기 버튼 — 고용 후에도 계속 눌린다
        if (!cardGo.TryGetComponent<Button>(out var cardBtn))
            cardBtn = cardGo.AddComponent<Button>();
        cardGo.TryGetComponent<Image>(out var cardImg);
        cardBtn.targetGraphic = cardImg;
        var cb = cardBtn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.10f, 1.10f, 1.15f, 1f);
        cb.pressedColor     = new Color(0.82f, 0.82f, 0.88f, 1f);
        cb.fadeDuration     = 0.08f;
        cardBtn.colors = cb;

        foreach (var img in cardGo.GetComponentsInChildren<Image>(true))
            img.raycastTarget = false;
        foreach (var tmp in cardGo.GetComponentsInChildren<TextMeshProUGUI>(true))
            tmp.raycastTarget = false;
        cardImg.raycastTarget = true;

        // ── 고용 버튼 (입체) ─────────────────────────────────
        var hireBtn = EditorUIBuilder.RaisedBtn(go, "HireBtn", HireBtnC, out var body);
        AnchorBottom(hireBtn.gameObject, HireBtmPad, BuyBtnH, 24f);
        BuildCostRow(body, "고용", UIScale.FontMd);

        var (plate, soldTmp) = BuildSoldPlate(go, HireBtmPad, BuyBtnH, 24f);

        var sSo = new SerializedObject(slot);
        sSo.Update();
        SetObj(sSo, "_portraitBg",     FindChild<Image>(cardGo.transform, "PortraitBg"));
        SetObj(sSo, "_portraitImg",    FindChild<Image>(cardGo.transform, "PortraitImage"));
        SetObj(sSo, "_portraitBridge", FindChild<UnitAppearanceBridge>(cardGo.transform, "PortraitPreview"));
        SetObj(sSo, "_nameText",       FindChild<TextMeshProUGUI>(cardGo.transform, "NameText"));
        SetObj(sSo, "_jobText",        FindChild<TextMeshProUGUI>(cardGo.transform, "JobText"));
        SetObj(sSo, "_gradeText",      FindChild<TextMeshProUGUI>(cardGo.transform, "GradeText"));
        SetObj(sSo, "_hpText",         FindChild<TextMeshProUGUI>(cardGo.transform, "HpText"));
        SetObj(sSo, "_atkText",        FindChild<TextMeshProUGUI>(cardGo.transform, "AtkText"));
        SetObj(sSo, "_defText",        FindChild<TextMeshProUGUI>(cardGo.transform, "DefText"));
        SetObj(sSo, "_soldierText",    FindChild<TextMeshProUGUI>(cardGo.transform, "SoldierText"));
        SetObj(sSo, "_costText",       FindTMP(body, "CostText"));
        SetObj(sSo, "_buyBtn",         hireBtn);
        SetObj(sSo, "_cardBtn",        cardBtn);
        SetObj(sSo, "_soldOut",        plate);
        SetObj(sSo, "_soldText",       soldTmp);
        sSo.ApplyModifiedProperties();
        return slot;
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

    static TextMeshProUGUI TMP(GameObject parent, string name, string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    static void Stretch(GameObject go) => EditorUIBuilder.Stretch(go);

    static T FindChild<T>(Transform root, string cname) where T : Component
        => EditorUIBuilder.FindDeep<T>(root, cname);

    static TextMeshProUGUI FindTMP(GameObject root, string cname)
        => EditorUIBuilder.FindDeep<TextMeshProUGUI>(root.transform, cname);

    static Sprite LoadGold() => AssetDatabase.LoadAssetAtPath<Sprite>(GoldIconPath);

    static void SetObj(SerializedObject so, string field, Object obj)
        => EditorUIBuilder.SetObj(so, field, obj, "RunShopPopupCreator");

    static void SetEnum(SerializedObject so, string field, int value)
        => EditorUIBuilder.SetEnum(so, field, value, "RunShopPopupCreator");
}
