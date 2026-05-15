using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercenaryPopupCreator.cs
//  Tools > Project K > Popup > Create Mercenary Popups
//
//  생성 대상:
//    MercenaryShopPopup.prefab  (용병 상점)
//    MercCandidateCard.prefab   (용병 상점 후보 카드)
//    MercFullRow.prefab         (슬롯 꽉 찼을 때 목록 행)
//
//  저장 경로: Assets/_project/2.Prefabs/UI/
// ============================================================

public static class MercenaryPopupCreator
{
    const string SaveDir = "Assets/_project/2.Prefabs/UI";

    // ── 색상 상수 ─────────────────────────────────────────────

    static readonly Color BgColor       = new Color(0.08f, 0.10f, 0.16f, 0.97f);
    static readonly Color SectionBg     = new Color(0.10f, 0.12f, 0.20f, 1f);
    static readonly Color DividerColor  = new Color(0.25f, 0.25f, 0.35f, 0.80f);
    static readonly Color MutedText     = new Color(0.60f, 0.60f, 0.65f);
    static readonly Color HpColor       = new Color(0.35f, 1.00f, 0.45f);
    static readonly Color AtkColor      = new Color(1.00f, 0.45f, 0.35f);
    static readonly Color DefColor      = new Color(0.40f, 0.70f, 1.00f);
    static readonly Color SpdColor      = new Color(1.00f, 0.85f, 0.35f);
static readonly Color FireBtnColor  = new Color(0.70f, 0.20f, 0.18f, 1f);
    static readonly Color HireBtnColor  = new Color(0.15f, 0.65f, 0.40f, 1f);
    static readonly Color PassBtnColor  = new Color(0.35f, 0.35f, 0.42f, 1f);
    static readonly Color CloseBtnColor = new Color(0.55f, 0.18f, 0.18f, 1f);
    static readonly Color CardBg        = new Color(0.11f, 0.13f, 0.22f, 1f);
    static readonly Color PortraitBg    = new Color(0.14f, 0.14f, 0.22f, 1f);

    // ── 메뉴 ─────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Popup/Create Mercenary Popups")]
    static void CreateAll()
    {
        CreateMercCandidateCard();
        CreateMercFullRow();
        CreateMercenaryShopPopup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MercenaryPopupCreator] 팝업 프리팹 생성 완료");
    }

    // ══════════════════════════════════════════════════════════
    //  MercCandidateCard 프리팹 (310 × 520)
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/Popup/Create MercCandidateCard Prefab")]
    public static void CreateMercCandidateCard()
    {
        const float CW = 310f, CH = 520f;

        var root = new GameObject("MercCandidateCard", typeof(RectTransform), typeof(Image));
        root.AddComponent<MercCandidateCardUI>();
        root.GetComponent<Image>().color = CardBg;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(CW, CH);

        // 상단 등급 바
        var topBar = AddImg(root, "GradeTopBar", new Color(0.3f, 0.3f, 0.4f));
        var tbRt = topBar.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0f, 1f); tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.anchoredPosition = Vector2.zero; tbRt.sizeDelta = new Vector2(0f, 8f);

        // 초상화 배경
        const float portSz = 140f;
        var portBg  = AddImg(root, "PortraitBg", PortraitBg);
        var pbRt = portBg.GetComponent<RectTransform>();
        pbRt.anchorMin = pbRt.anchorMax = new Vector2(0.5f, 1f);
        pbRt.pivot = new Vector2(0.5f, 1f);
        pbRt.anchoredPosition = new Vector2(0f, -24f);
        pbRt.sizeDelta = new Vector2(portSz, portSz);

        var portImg = AddImg(portBg, "PortraitImage", Color.white);
        FullStretch(portImg.gameObject);
        var portBridge = portBg.gameObject.AddComponent<UnitAppearanceBridge>();

        // 이름 / 등급 / 직업 (초상화 아래)
        var nameText  = AddCardTMP(root, "NameText",  "이름",   UIScale.FontMd, FontStyles.Bold,  new Vector2(0f, -185f), new Vector2(CW - 20f, 46f));
        var gradeText = AddCardTMP(root, "GradeText", "일반",   UIScale.FontSm, FontStyles.Bold,  new Vector2(0f, -240f), new Vector2(CW - 20f, 36f));
        var jobText   = AddCardTMP(root, "JobText",   "직업",   UIScale.FontSm, FontStyles.Normal, new Vector2(0f, -280f), new Vector2(CW - 20f, 34f));
        gradeText.color = new Color(0.80f, 0.80f, 0.90f);
        jobText.color   = MutedText;

        // 구분선
        var div = AddImg(root, "Divider", DividerColor);
        var divRt = div.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 1f); divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -324f);
        divRt.sizeDelta = new Vector2(CW - 32f, 2f);

        // HP / ATK
        var hpText  = AddCardTMP(root, "HpText",  "HP 0",   UIScale.FontSm, FontStyles.Normal, new Vector2(0f, -358f), new Vector2(CW - 20f, 36f));
        var atkText = AddCardTMP(root, "AtkText", "공격 0", UIScale.FontSm, FontStyles.Normal, new Vector2(0f, -400f), new Vector2(CW - 20f, 36f));
        hpText.color  = HpColor;
        atkText.color = AtkColor;

        // 선택 버튼 (하단)
        var selBtn = AddBtn(root, "SelectBtn", "상세 보기", new Color(0.22f, 0.45f, 0.72f), UIScale.FontSm);
        var sbRt = selBtn.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0f); sbRt.anchorMax = new Vector2(1f, 0f);
        sbRt.pivot = new Vector2(0.5f, 0f);
        sbRt.anchoredPosition = new Vector2(0f, 12f);
        sbRt.sizeDelta = new Vector2(-16f, UIScale.BtnSm);

        // 직렬화 필드 연결
        var so = new SerializedObject(root.GetComponent<MercCandidateCardUI>());
        SetObj(so, "_gradeBorder",   topBar.GetComponent<Image>());
        SetObj(so, "_portraitBg",    portBg.GetComponent<Image>());
        SetObj(so, "_portraitImg",   portImg.GetComponent<Image>());
        SetObj(so, "_portraitBridge",portBridge);
        SetObj(so, "_nameText",      nameText);
        SetObj(so, "_gradeText",     gradeText);
        SetObj(so, "_jobText",       jobText);
        SetObj(so, "_hpText",        hpText);
        SetObj(so, "_atkText",       atkText);
        SetObj(so, "_button",        selBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "MercCandidateCard");
    }

    // ══════════════════════════════════════════════════════════
    //  MercFullRow 프리팹 (슬롯 꽉 찼을 때 목록 행, 760 × 100)
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/Popup/Create MercFullRow Prefab")]
    public static void CreateMercFullRow()
    {
        const float RW = 760f, RH = 100f;

        var root = new GameObject("MercFullRow", typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.22f, 1f);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(RW, RH);

        // 등급 좌측 바
        var gradeBar = AddImg(root, "GradeBar", new Color(0.3f, 0.3f, 0.4f));
        var gbRt = gradeBar.GetComponent<RectTransform>();
        gbRt.anchorMin = new Vector2(0f, 0f); gbRt.anchorMax = new Vector2(0f, 1f);
        gbRt.offsetMin = new Vector2(0f, 4f); gbRt.offsetMax = new Vector2(8f, -4f);

        // 이름 (좌)
        var nameT = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameT.transform.SetParent(root.transform, false);
        var ntTmp = nameT.GetComponent<TextMeshProUGUI>();
        ntTmp.text = "장군 이름"; ntTmp.fontSize = UIScale.FontMd;
        ntTmp.fontStyle = FontStyles.Bold; ntTmp.alignment = TextAlignmentOptions.Left;
        ntTmp.color = Color.white;
        var ntRt = nameT.GetComponent<RectTransform>();
        ntRt.anchorMin = new Vector2(0f, 0.5f); ntRt.anchorMax = new Vector2(0f, 0.5f);
        ntRt.pivot = new Vector2(0f, 0.5f);
        ntRt.anchoredPosition = new Vector2(18f, 12f); ntRt.sizeDelta = new Vector2(280f, 42f);

        // 직업
        var jobT = new GameObject("JobText", typeof(RectTransform), typeof(TextMeshProUGUI));
        jobT.transform.SetParent(root.transform, false);
        var jtTmp = jobT.GetComponent<TextMeshProUGUI>();
        jtTmp.text = "직업"; jtTmp.fontSize = UIScale.FontSm;
        jtTmp.alignment = TextAlignmentOptions.Left; jtTmp.color = MutedText;
        var jtRt = jobT.GetComponent<RectTransform>();
        jtRt.anchorMin = new Vector2(0f, 0.5f); jtRt.anchorMax = new Vector2(0f, 0.5f);
        jtRt.pivot = new Vector2(0f, 0.5f);
        jtRt.anchoredPosition = new Vector2(18f, -16f); jtRt.sizeDelta = new Vector2(200f, 36f);

        // 해고 버튼 (우측)
        var fireBtn = AddBtn(root, "FireBtn", "해고", FireBtnColor, UIScale.FontSm);
        var fbRt = fireBtn.GetComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(1f, 0.5f); fbRt.anchorMax = new Vector2(1f, 0.5f);
        fbRt.pivot = new Vector2(1f, 0.5f);
        fbRt.anchoredPosition = new Vector2(-12f, 0f); fbRt.sizeDelta = new Vector2(180f, 72f);

        Save(root, "MercFullRow");
    }

    // ══════════════════════════════════════════════════════════
    //  MercenaryShopPopup (1140 × 840)
    // ══════════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/Popup/Create MercenaryShop Popup")]
    public static void CreateMercenaryShopPopup()
    {
        const float W = 1140f, H = 840f;
        const float halfW = W / 2f, halfH = H / 2f;
        const float HeaderH = 80f;

        var root  = MakeRoot<MercenaryShopPopup>("MercenaryShopPopup", W, H);
        var popup = root.GetComponent<MercenaryShopPopup>();
        AddBg(root, BgColor);

        // ── 헤더 ──────────────────────────────────────────────
        var headerGo = AddImg(root, "Header", new Color(0.10f, 0.12f, 0.22f, 1f));
        var hRt = headerGo.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f); hRt.anchorMax = new Vector2(1f, 1f);
        hRt.offsetMin = new Vector2(0f, -HeaderH); hRt.offsetMax = Vector2.zero;

        var titleTmp = AddTMP(headerGo.gameObject, "TitleText", "용병 상점", UIScale.FontMd, FontStyles.Bold);
        SetCenterRect(titleTmp.rectTransform, new Vector2(-60f, 0f), new Vector2(W - 200f, HeaderH - 10f));

        var closeBtn = AddBtn(headerGo.gameObject, "CloseBtn", "✕", CloseBtnColor, UIScale.FontMd);
        SetCenterRect(closeBtn.GetComponent<RectTransform>(),
            new Vector2(halfW - 44f, 0f), new Vector2(72f, 60f));

        // ── 본문 영역 (헤더 아래) ─────────────────────────────
        // 세 가지 뷰가 이 공간을 공유 (한 번에 하나만 활성)
        const float bodyTop = -HeaderH;  // 헤더 바로 아래부터 (root 기준 offset)

        // ── CandidateView ─────────────────────────────────────
        var candidateView = new GameObject("CandidateView", typeof(RectTransform));
        candidateView.transform.SetParent(root.transform, false);
        var cvRt = candidateView.GetComponent<RectTransform>();
        cvRt.anchorMin = Vector2.zero; cvRt.anchorMax = Vector2.one;
        cvRt.offsetMin = Vector2.zero; cvRt.offsetMax = new Vector2(0f, bodyTop);

        // 카드 3개 — HorizontalLayoutGroup
        var cardArea = new GameObject("CardArea", typeof(RectTransform));
        cardArea.transform.SetParent(candidateView.transform, false);
        var caRt = cardArea.GetComponent<RectTransform>();
        caRt.anchorMin = new Vector2(0.5f, 0.5f); caRt.anchorMax = new Vector2(0.5f, 0.5f);
        caRt.anchoredPosition = new Vector2(0f, 0f);
        caRt.sizeDelta = new Vector2(W - 40f, H - HeaderH - 20f);
        var hlg = cardArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        // 카드 프리팹 로드
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SaveDir}/MercCandidateCard.prefab");
        var cards = new MercCandidateCardUI[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject cardGo;
            if (cardPrefab != null)
            {
                cardGo = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, cardArea.transform);
                cardGo.name = $"Card{i}";
            }
            else
            {
                cardGo = new GameObject($"Card{i}", typeof(RectTransform), typeof(Image));
                cardGo.transform.SetParent(cardArea.transform, false);
                cardGo.GetComponent<Image>().color = CardBg;
                cardGo.AddComponent<MercCandidateCardUI>();
                cardGo.GetComponent<RectTransform>().sizeDelta = new Vector2(310f, (H - HeaderH - 50f));
            }
            cards[i] = cardGo.GetComponent<MercCandidateCardUI>();
        }

        // ── DetailView ────────────────────────────────────────
        var detailView = new GameObject("DetailView", typeof(RectTransform));
        detailView.transform.SetParent(root.transform, false);
        var dvRt = detailView.GetComponent<RectTransform>();
        dvRt.anchorMin = Vector2.zero; dvRt.anchorMax = Vector2.one;
        dvRt.offsetMin = Vector2.zero; dvRt.offsetMax = new Vector2(0f, bodyTop);
        detailView.SetActive(false);

        // DetailView 내부 — 좌 초상화(380px) + 우 스탯/스킬
        const float detLeftW  = 380f;
        const float detBodyH  = H - HeaderH;

        // 좌측 패널 (초상화 + 기본 정보)
        var detLeftPnl = AddImg(detailView, "DetailLeft", SectionBg);
        var dlRt = detLeftPnl.GetComponent<RectTransform>();
        dlRt.anchorMin = new Vector2(0f, 0f); dlRt.anchorMax = new Vector2(0f, 1f);
        dlRt.offsetMin = Vector2.zero; dlRt.offsetMax = new Vector2(detLeftW, 0f);

        const float dPortSz = 160f;
        var dPortBg  = AddImg(detLeftPnl, "PortraitBg", PortraitBg);
        var dpbRt = dPortBg.GetComponent<RectTransform>();
        dpbRt.anchorMin = dpbRt.anchorMax = new Vector2(0.5f, 1f);
        dpbRt.pivot = new Vector2(0.5f, 1f);
        dpbRt.anchoredPosition = new Vector2(0f, -30f);
        dpbRt.sizeDelta = new Vector2(dPortSz, dPortSz);
        var dPortImg = AddImg(dPortBg, "PortraitImage", Color.white);
        FullStretch(dPortImg.gameObject);
        var dPortBridge = dPortBg.gameObject.AddComponent<UnitAppearanceBridge>();

        var dGradeBorder = AddImg(detLeftPnl, "GradeBorder", new Color(0.3f, 0.3f, 0.4f));
        var dgbRt = dGradeBorder.GetComponent<RectTransform>();
        dgbRt.anchorMin = new Vector2(0f, 0f); dgbRt.anchorMax = new Vector2(0f, 1f);
        dgbRt.offsetMin = Vector2.zero; dgbRt.offsetMax = new Vector2(8f, 0f);

        var dNameT  = AddTMP(detLeftPnl.gameObject, "DetailNameText",  "이름", UIScale.FontMd, FontStyles.Bold);
        var dGradeT = AddTMP(detLeftPnl.gameObject, "DetailGradeText", "일반", UIScale.FontSm, FontStyles.Bold);
        var dJobT   = AddTMP(detLeftPnl.gameObject, "DetailJobText",   "직업", UIScale.FontSm, FontStyles.Normal);
        float dInfoY = -dPortSz - 40f;
        PinTopLocal(dNameT .rectTransform, detLeftPnl.gameObject, dInfoY,        50f);
        PinTopLocal(dGradeT.rectTransform, detLeftPnl.gameObject, dInfoY - 56f,  38f);
        PinTopLocal(dJobT  .rectTransform, detLeftPnl.gameObject, dInfoY - 100f, 36f);
        dGradeT.color = new Color(0.80f, 0.80f, 0.90f);
        dJobT.color   = MutedText;

        // 우측 패널 (스탯 + 스킬 + 버튼)
        var detRightPnl = new GameObject("DetailRight", typeof(RectTransform));
        detRightPnl.transform.SetParent(detailView.transform, false);
        var drRt = detRightPnl.GetComponent<RectTransform>();
        drRt.anchorMin = new Vector2(0f, 0f); drRt.anchorMax = new Vector2(1f, 1f);
        drRt.offsetMin = new Vector2(detLeftW + 1f, 0f); drRt.offsetMax = Vector2.zero;

        float drHalf  = (W - detLeftW) / 2f;
        float drHalfH = detBodyH / 2f;

        // 스탯 (4개: HP, ATK, DEF, SPD)
        string[] dStatLbls   = { "체력", "공격력", "방어율", "이동속도" };
        Color[]  dStatColors = { HpColor, AtkColor, DefColor, SpdColor };
        var dStatTmps = new TextMeshProUGUI[4];
        const float dStatStartY = 100f;
        const float dStatRowH   = 56f;
        for (int i = 0; i < 4; i++)
        {
            float sy = drHalfH - 60f - i * dStatRowH;
            var lbl = AddTMP(detRightPnl, $"DStatLbl{i}", dStatLbls[i], UIScale.FontSm, FontStyles.Normal);
            SetCenterRect(lbl.rectTransform, new Vector2(-drHalf + 80f, sy), new Vector2(120f, 40f));
            lbl.color = MutedText; lbl.alignment = TextAlignmentOptions.Left;

            var val = AddTMP(detRightPnl, $"DStatVal{i}", "0", UIScale.FontSm, FontStyles.Bold);
            SetCenterRect(val.rectTransform, new Vector2(-drHalf + 220f, sy), new Vector2(200f, 40f));
            val.color = dStatColors[i]; val.alignment = TextAlignmentOptions.Left;
            dStatTmps[i] = val;
        }

        AddDivider(detRightPnl, drHalfH - 60f - 4 * dStatRowH - 6f, W - detLeftW - 40f);

        // 스킬
        float dSkillY = drHalfH - 60f - 4 * dStatRowH - 40f;
        var dActLbl = AddTMP(detRightPnl, "DActiveLbl", "액티브", UIScale.FontSm, FontStyles.Normal);
        SetCenterRect(dActLbl.rectTransform, new Vector2(-drHalf + 80f, dSkillY), new Vector2(100f, 38f));
        dActLbl.color = MutedText; dActLbl.alignment = TextAlignmentOptions.Left;
        var dActSkillText = AddTMP(detRightPnl, "DetailActiveSkillText", "스킬", UIScale.FontSm, FontStyles.Bold);
        SetCenterRect(dActSkillText.rectTransform, new Vector2(-drHalf + 200f, dSkillY), new Vector2(360f, 38f));
        dActSkillText.color = new Color(0.75f, 0.85f, 1.0f); dActSkillText.alignment = TextAlignmentOptions.Left;

        float dPasY = dSkillY - 48f;
        var dPasLbl = AddTMP(detRightPnl, "DPassiveLbl", "패시브", UIScale.FontSm, FontStyles.Normal);
        SetCenterRect(dPasLbl.rectTransform, new Vector2(-drHalf + 80f, dPasY), new Vector2(100f, 38f));
        dPasLbl.color = MutedText; dPasLbl.alignment = TextAlignmentOptions.Left;
        var dPas0 = AddTMP(detRightPnl, "DetailPassive0Text", "패시브1", UIScale.FontSm, FontStyles.Normal);
        var dPas1 = AddTMP(detRightPnl, "DetailPassive1Text", "패시브2", UIScale.FontSm, FontStyles.Normal);
        var dPas2 = AddTMP(detRightPnl, "DetailPassive2Text", "패시브3", UIScale.FontSm, FontStyles.Normal);
        float dpx = -drHalf + 200f;
        SetCenterRect(dPas0.rectTransform, new Vector2(dpx,        dPasY), new Vector2(170f, 38f));
        SetCenterRect(dPas1.rectTransform, new Vector2(dpx + 180f, dPasY), new Vector2(170f, 38f));
        SetCenterRect(dPas2.rectTransform, new Vector2(dpx + 360f, dPasY), new Vector2(170f, 38f));
        dPas0.color = dPas1.color = dPas2.color = new Color(0.65f, 0.90f, 0.65f);
        dPas0.alignment = dPas1.alignment = dPas2.alignment = TextAlignmentOptions.Left;

        AddDivider(detRightPnl, dPasY - 34f, W - detLeftW - 40f);

        // 고용 / 포기 버튼 (하단)
        const float btnSecY = -(detBodyH / 2f) + 60f;
        var passShardText = AddTMP(detRightPnl, "PassShardText", "0 조각", UIScale.FontSm, FontStyles.Normal);
        SetCenterRect(passShardText.rectTransform, new Vector2(-drHalf + 120f, btnSecY), new Vector2(200f, 48f));
        passShardText.color = new Color(0.85f, 0.85f, 0.35f); passShardText.alignment = TextAlignmentOptions.Left;

        var hireBtn = AddBtn(detRightPnl, "HireBtn", "고용", HireBtnColor, UIScale.FontMd);
        SetCenterRect(hireBtn.GetComponent<RectTransform>(), new Vector2(80f, btnSecY), new Vector2(220f, UIScale.BtnSm));

        var passBtn = AddBtn(detRightPnl, "PassBtn", "포기", PassBtnColor, UIScale.FontSm);
        SetCenterRect(passBtn.GetComponent<RectTransform>(), new Vector2(320f, btnSecY), new Vector2(160f, UIScale.BtnSm));

        // ── SlotFullView ──────────────────────────────────────
        var slotFullView = new GameObject("SlotFullView", typeof(RectTransform));
        slotFullView.transform.SetParent(root.transform, false);
        var sfvRt = slotFullView.GetComponent<RectTransform>();
        sfvRt.anchorMin = Vector2.zero; sfvRt.anchorMax = Vector2.one;
        sfvRt.offsetMin = Vector2.zero; sfvRt.offsetMax = new Vector2(0f, bodyTop);
        slotFullView.SetActive(false);

        // "슬롯이 꽉 찼습니다" 레이블
        var fullHintText = AddTMP(slotFullView, "FullHintText",
            "모든 슬롯이 채워져 있습니다. 해고하면 바로 새 용병을 고용할 수 있습니다.",
            UIScale.FontSm, FontStyles.Normal);
        fullHintText.color = MutedText;
        PinTopInFull(fullHintText.rectTransform, slotFullView, 0f, 60f);

        // 스크롤뷰
        var scrollGo = new GameObject("FullListScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(slotFullView.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(40f, 20f); scrollRt.offsetMax = new Vector2(-40f, -70f);

        var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        var vpRt = vpGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;

        var contentGo = new GameObject("FullList", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f); contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f; vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.content = contentRt; sr.viewport = vpRt;
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        // FullRow 템플릿 로드
        var fullRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SaveDir}/MercFullRow.prefab");
        GameObject fullRowTemplate;
        if (fullRowPrefab != null)
        {
            fullRowTemplate = (GameObject)PrefabUtility.InstantiatePrefab(fullRowPrefab, root.transform);
            fullRowTemplate.name = "FullRowTemplate";
            fullRowTemplate.SetActive(false);
        }
        else
        {
            fullRowTemplate = new GameObject("FullRowTemplate", typeof(RectTransform));
            fullRowTemplate.transform.SetParent(root.transform, false);
            fullRowTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 100f);
            fullRowTemplate.SetActive(false);
        }

        // ── 직렬화 필드 연결 ──────────────────────────────────
        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",            (int)PopupType.MercenaryShop);
        SetObj (so, "_candidateView",        candidateView);
        SetObj (so, "_detailView",           detailView);
        SetObj (so, "_slotFullView",         slotFullView);
        SetObj (so, "_fullList",             contentGo.transform);
        SetObj (so, "_fullRowTemplate",      fullRowTemplate);
        SetObj (so, "_detailGradeBorder",    dGradeBorder.GetComponent<Image>());
        SetObj (so, "_detailPortraitBg",     dPortBg.GetComponent<Image>());
        SetObj (so, "_detailPortraitImg",    dPortImg.GetComponent<Image>());
        SetObj (so, "_detailPortraitBridge", dPortBridge);
        SetObj (so, "_detailNameText",       dNameT);
        SetObj (so, "_detailGradeText",      dGradeT);
        SetObj (so, "_detailJobText",        dJobT);
        SetObj (so, "_detailHpText",         dStatTmps[0]);
        SetObj (so, "_detailAtkText",        dStatTmps[1]);
        SetObj (so, "_detailDefText",        dStatTmps[2]);
        SetObj (so, "_detailSpdText",        dStatTmps[3]);
        SetObj (so, "_detailActiveSkillText",dActSkillText);
        SetObj (so, "_detailPassive0Text",   dPas0);
        SetObj (so, "_detailPassive1Text",   dPas1);
        SetObj (so, "_detailPassive2Text",   dPas2);
        SetObj (so, "_passShardText",        passShardText);
        SetObj (so, "_hireBtn",              hireBtn.GetComponent<Button>());
        SetObj (so, "_passBtn",              passBtn.GetComponent<Button>());
        SetObj (so, "_closeBtn",             closeBtn.GetComponent<Button>());

        var cardsProp = so.FindProperty("_candidateCards");
        cardsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];

        so.ApplyModifiedProperties();

        Save(root, "MercenaryShopPopup");
    }

    // ══════════════════════════════════════════════════════════
    //  UI 생성 헬퍼
    // ══════════════════════════════════════════════════════════

    static GameObject MakeRoot<T>(string name, float w, float h) where T : PopupBase
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<CanvasGroup>();
        go.AddComponent<T>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    static void AddBg(GameObject parent, Color color)
    {
        var go = new GameObject("BgPanel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.transform.SetAsFirstSibling();
        FullStretch(go);
        go.GetComponent<Image>().color = color;
    }

    static Image AddImg(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<Image>();
    }

    static Image AddImg(Image parent, string name, Color color)
        => AddImg(parent.gameObject, name, color);

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text,
        float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return tmp;
    }

    static TextMeshProUGUI AddCardTMP(GameObject parent, string name, string text,
        float size, FontStyles style, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var tmp = AddTMP(parent, name, text, size, style);
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        return tmp;
    }

    static GameObject AddBtn(GameObject parent, string name, string label,
        Color bgColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;
        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return go;
    }

    static void AddDivider(GameObject parent, float anchoredY, float width)
    {
        var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = DividerColor;
        SetCenterRect(go.GetComponent<RectTransform>(), new Vector2(0f, anchoredY), new Vector2(width, 2f));
    }

    // center anchor (0.5, 0.5)
    static void SetCenterRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    // left-center anchor (0, 0.5) — OccupiedGroup 텍스트용
    static void SetLeftRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    // 전체 스트레치
    static void FullStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // parent 기준 상단에서 anchoredY(음수)만큼 내려온 위치에 텍스트 배치
    static void PinTopLocal(RectTransform rt, GameObject parent, float anchoredY, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY); rt.sizeDelta = new Vector2(-20f, height);
    }

    // SlotFullView 내 상단 고정 텍스트
    static void PinTopInFull(RectTransform rt, GameObject parent, float anchoredY, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, anchoredY); rt.sizeDelta = new Vector2(-40f, height);
    }

    static void SetEnum(SerializedObject so, string field, int value)
    {
        var prop = so.FindProperty(field); if (prop != null) prop.intValue = value;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field); if (prop != null) prop.objectReferenceValue = obj;
    }

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SaveDir}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[MercenaryPopupCreator] 저장: {path}");
    }
}
