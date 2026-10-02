using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroDetailPopup.cs
//  장수 상세 정보 팝업 — 전체 화면 3단 구성.
//
//  ■ 왜 탭을 없앴나
//    스탯·장비·스킬이 탭으로 나뉘어 있어 한 번에 한 가지만 볼 수 있었다.
//    장수를 고르는 판단은 "이 스탯에 이 스킬"을 같이 봐야 서는데,
//    탭을 오가며 기억해야 했다. 셋 다 한 화면에 편다.
//
//  ■ 레이아웃 (HeroDetailPopupCreator)
//    Header   ◆ 장 수 | 이름            [재화 4종]              [X]
//    ├ Left   초상화 + 장비 3칸(아이콘만) / [등급업][해고] / Lv·직업·등급
//    │        / EXP / 레벨업·용병
//    ├ Mid    스 탯   — 9행 전부 노출. 행을 누르면 출처별 분해
//    └ Right  스 킬   — 액티브 1 + 패시브 최대 3, 설명을 크게
//
//    장비 칸을 누르면 EquipComparePopup 이 우측(스킬 열) 위에 겹쳐 열린다
//    — 예전 "장비" 탭을 눌렀을 때와 같은 창이다.
//
//  ■ 등급업 · 해고 행 (초상화 아래)
//    장비 스트립(520)이 초상화(400)보다 길어 비어 있던 400×140 자리를 쓴다.
//    등급업 : 장군 강화석 소모, Epic 이면 "MAX" 로 잠긴다.
//    해고   : 배치 장수가 1명뿐이면 잠긴다 — 전부 해고하면 전투를 시작할 수 없다.
//    둘 다 프리뷰 모드(상점 미리보기)에서는 숨긴다 — 아직 내 장수가 아니다.
//
//  Inspector 연결은 전부 HeroDetailPopupCreator 가 자동으로 한다.
// ============================================================

public class HeroDetailPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _nameText;
    // 이름 뒤에 깔리는 그림자 사본. 같이 갱신하지 않으면 프리팹의 플레이스홀더
    // ("영웅 이름") 가 실제 이름 옆에 검은 글씨로 그대로 남는다.
    [SerializeField] TextMeshProUGUI _nameShadowText;
    [SerializeField] Button          _closeBtn;

    [Header("초상화")]
    [SerializeField] Image                _gradeBorder;
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;

    [Header("기본 정보")]
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] TextMeshProUGUI _jobText;
    [SerializeField] Image           _gradeBadge;
    [SerializeField] TextMeshProUGUI _gradeText;

    [Tooltip("등급 배지를 눌렀을 때 뜨는 등급·품질 설명. 배지 아래로 펼쳐진다.")]
    [SerializeField] Button          _gradeInfoBtn;
    [SerializeField] InfoTooltipUI   _gradeTooltip;

    [Tooltip("헤더 이름 옆 지휘력 안내. 수치는 GameplayConfig 에서 읽어 채운다.")]
    [SerializeField] TextMeshProUGUI _commandHintText;

    [Header("장비 (아이콘만 — 누르면 EquipComparePopup)")]
    [SerializeField] GameObject       _equipRoot;    // 프리뷰 모드에서 통째로 숨김
    [SerializeField] HeroEquipSlotUI[] _equipSlots;  // 3칸 — 2번 칸은 특성으로 해금

    [Header("스탯 — 장수 / 용병 토글")]
    [SerializeField] Button       _generalTabBtn;
    [SerializeField] Button       _soldierTabBtn;
    [SerializeField] GameObject[] _generalOnlyRows;   // 용병 수·지휘력·스킬 쿨타임

    [Header("스탯")]
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] TextMeshProUGUI _atkText;
    [SerializeField] TextMeshProUGUI _defText;
    [SerializeField] TextMeshProUGUI _spdText;
    [SerializeField] TextMeshProUGUI _atkSpdText;
    [SerializeField] TextMeshProUGUI _rangeText;
    [SerializeField] TextMeshProUGUI _critChanceText;
    [SerializeField] TextMeshProUGUI _critDmgText;
    [SerializeField] TextMeshProUGUI _soldierCountText;
    [SerializeField] TextMeshProUGUI _cmdPwrText;
    [SerializeField] TextMeshProUGUI _cooldownText;

    [Header("스킬")]
    [SerializeField] Image           _activeSkillIcon;
    [SerializeField] TextMeshProUGUI _activeSkillText;
    [SerializeField] TextMeshProUGUI _activeSkillDescText;
    [SerializeField] GameObject[]    _passiveBoxes;
    [SerializeField] Image[]         _passiveIcons;
    [SerializeField] TextMeshProUGUI[] _passiveNameTexts;
    [SerializeField] TextMeshProUGUI[] _passiveDescTexts;

    [Header("등급업 · 해고 (초상화 아래)")]
    [SerializeField] GameObject      _rankRow;          // 프리뷰 모드에서 통째로 숨김
    [SerializeField] Button          _gradeUpBtn;
    [SerializeField] TextMeshProUGUI _gradeUpCostText;
    [SerializeField] Image           _gradeUpCostIcon;
    [SerializeField] Button          _fireBtn;

    [Header("성장 (EXP · 레벨업 · 용병)")]
    [SerializeField] GameObject      _growthRow;
    [SerializeField] TextMeshProUGUI _expText;
    [SerializeField] Image           _expBarFill;
    [SerializeField] Button          _levelUpBtn;
    [SerializeField] TextMeshProUGUI _levelUpCostText;
    [SerializeField] Image           _levelUpCostIcon;
    [SerializeField] Button          _soldierUpBtn;
    [SerializeField] TextMeshProUGUI _soldierUpCostText;
    [SerializeField] Image           _soldierUpCostIcon;

    const int EquipSlotCount = 3;

    UnitEntry _entry;
    Texture2D _portraitTexture;

    HeroStatResult _statResult;
    int            _expandedStatIndex = -1;
    bool           _showSoldier;          // false = 장수(기본), true = 용병

    struct StatRowEntry
    {
        public TextMeshProUGUI ValueTmp;
        public StatType        Type;
    }
    StatRowEntry[] _statRowEntries;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(UnitEntry entry)
    {
        _entry             = entry;
        _expandedStatIndex = -1;

        // 프리뷰 모드에서 껐을 수 있으므로 복원
        _growthRow.SetActive(true);
        _equipRoot.SetActive(true);
        _rankRow.SetActive(true);
        _levelText.gameObject.SetActive(true);

        SetStatTarget(soldier: false);   // 열 때는 항상 장수부터
        RefreshUI();
    }

    /// <summary>상점 미리보기 — 아직 내 장수가 아니므로 성장·장비·등급업을 숨긴다.</summary>
    public void SetupPreview(UnitEntry entry)
    {
        Setup(entry);

        _growthRow.SetActive(false);
        _equipRoot.SetActive(false);
        _rankRow.SetActive(false);
        _levelText.gameObject.SetActive(false);
    }

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn.onClick.AddListener(OnCloseClick);
        HoldRepeatButton.Bind(_levelUpBtn, OnLevelUpClick);
        HoldRepeatButton.Bind(_soldierUpBtn, OnSoldierUpClick);
        HoldRepeatButton.Bind(_gradeUpBtn, OnGradeUpClick);
        _fireBtn.onClick.AddListener(OnFireClick);
        _gradeInfoBtn?.onClick.AddListener(ShowGradeTooltip);

        _generalTabBtn.onClick.AddListener(() => SetStatTarget(soldier: false));
        _soldierTabBtn.onClick.AddListener(() => SetStatTarget(soldier: true));

        for (int i = 0; i < EquipSlotCount; i++)
        {
            int slot = i;
            _equipSlots[i].Bind(() => OnEquipSlotClick(slot), () => OnEnhanceClick(slot));
        }

        SetupStatClickHandlers();
        ConfigureLocalizedLayout();
    }

    RectTransform _localizedStats;
    RectTransform _localizedSkills;

    // Text height depends on the selected language, so size these regions at runtime.
    void ConfigureLocalizedLayout()
    {
        // ⚠ 스탯 목록은 스크롤로 감싸지 않는다 (사용자 지시, 2026-10-02)
        //   한 장에 다 보여야 장수끼리 비교가 된다. 높이는 LayoutStats 가 칸에 맞춰 나눈다.
        var statViewport = (RectTransform)_hpText.transform.parent.parent;
        _localizedStats = FillContent(statViewport);
        foreach (var entry in _statRowEntries)
        {
            var row = (RectTransform)entry.ValueTmp.transform.parent;
            row.SetParent(_localizedStats, false);
            row.GetComponent<HorizontalLayoutGroup>().enabled = false;
        }

        var activeBox = (RectTransform)_activeSkillText.transform.parent;
        var skillViewport = new GameObject("SkillViewport", typeof(RectTransform)).GetComponent<RectTransform>();
        skillViewport.SetParent(activeBox.parent, false);
        skillViewport.anchorMin = Vector2.zero;
        skillViewport.anchorMax = Vector2.one;
        skillViewport.offsetMin = new Vector2(12f, 12f);
        skillViewport.offsetMax = new Vector2(-12f, -60f);
        _localizedSkills = ScrollContent(skillViewport);
        activeBox.SetParent(_localizedSkills, false);
        foreach (var box in _passiveBoxes) box.transform.SetParent(_localizedSkills, false);

        foreach (var button in new[] { _generalTabBtn, _soldierTabBtn })
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = UIScale.FontSm;
            label.fontSizeMax = UIScale.FontMd;
        }

        _gradeBadge.rectTransform.sizeDelta = new Vector2(220f, UIScale.RowMd);
        _gradeText.enableAutoSizing = true;
        _gradeText.fontSizeMin = _gradeText.fontSizeMax = UIScale.FontSm;
        _gradeText.textWrappingMode = TextWrappingModes.NoWrap;
        _jobText.rectTransform.offsetMin = new Vector2(110f, _jobText.rectTransform.offsetMin.y);
        _jobText.rectTransform.offsetMax = new Vector2(-240f, _jobText.rectTransform.offsetMax.y);
        _jobText.enableAutoSizing = true;
        _jobText.fontSizeMin = UIScale.FontSm;
        _jobText.fontSizeMax = UIScale.FontMd;

        // ── 헤더 한 줄: [이름] ··· [지휘력 안내] ··· [재화] [i][X] ──
        //  ⚠ 지휘력 안내를 이름 '아래 줄' 로 내리지 않는다 (2026-10-02)
        //    헤더(136) 맨 아래 y=110 에 전체 폭으로 깔았더니 헤더 경계와 겹쳐
        //    본문 위로 글자가 걸쳤다. 처음 설계대로 이름 오른쪽 빈칸(HintX ~ 재화 바)에 둔다.
        //    이름은 그 앞에서 끝나고, 둘 다 넘치면 줄어든다(긴 언어).
        const float HintX = 480f, HintRight = 932f;   // HeroDetailPopupCreator: 재화 바 왼쪽 끝 = 932
        foreach (var title in new[] { _nameText, _nameShadowText })
        {
            title.fontSize = UIScale.FontMd;
            title.rectTransform.sizeDelta = new Vector2(HintX - 30f - 16f, UIScale.RowMd);
            LocalizedText.FitLabel(title);
        }
        _commandHintText.transform.parent.Find("CommandHintMark").gameObject.SetActive(false);
        var hint = _commandHintText.rectTransform;
        hint.anchorMin = hint.anchorMax = new Vector2(0f, 1f);
        hint.pivot     = new Vector2(0f, 1f);
        hint.anchoredPosition = new Vector2(HintX, _nameText.rectTransform.anchoredPosition.y);
        hint.sizeDelta = new Vector2(HintRight - HintX - 16f, UIScale.RowMd);
        _commandHintText.overflowMode     = TextOverflowModes.Overflow;
        _commandHintText.textWrappingMode = TextWrappingModes.NoWrap;
        _commandHintText.enableAutoSizing = true;
        _commandHintText.fontSizeMax = UIScale.FontSm;
        _commandHintText.fontSizeMin = UIScale.FontSm * 0.6f;
    }

    /// <summary>스크롤 없이 칸을 그대로 채우는 컨테이너.</summary>
    static RectTransform FillContent(RectTransform parent)
    {
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(parent, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = content.offsetMax = Vector2.zero;
        return content;
    }

    static RectTransform ScrollContent(RectTransform viewport)
    {
        viewport.gameObject.AddComponent<RectMask2D>();
        var background = viewport.gameObject.AddComponent<Image>();
        background.color = Color.clear;
        var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = UIScale.RowMd;
        return content;
    }

    void LateUpdate()
    {
        LayoutStats();

        float y = LayoutSkill(_activeSkillText, _activeSkillDescText, 0f, UIScale.FontLg);
        for (int i = 0; i < _passiveBoxes.Length; i++)
            if (_passiveBoxes[i].activeSelf)
                y = LayoutSkill(_passiveNameTexts[i], _passiveDescTexts[i], y, UIScale.FontMd);
        _localizedSkills.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
    }

    /// <summary>
    /// 스탯 행을 칸 높이 안에 나눠 놓는다 — 스크롤 없음.
    ///
    ///   접힌 행    : 남는 높이를 똑같이 나눈다. 라벨·값은 한 줄, 긴 언어는 글자가 줄어든다.
    ///   펼친 행    : 출처 내역(여러 줄)이 필요한 높이를 먼저 가져간다.
    ///                나머지는 RowSm 까지만 줄고, 그래도 모자라면 내역 글자가 줄어든다.
    ///
    /// ⚠ 예전엔 행마다 글자를 줄바꿈해 높이를 늘리고 스크롤로 감쌌다 —
    ///   언어에 따라 11줄이 한 화면에 안 들어와 아래 스탯을 보려면 내려야 했다.
    /// </summary>
    void LayoutStats()
    {
        float width  = _localizedStats.rect.width;
        float avail  = _localizedStats.rect.height;
        float labelW = width * 0.52f;
        float valueX = labelW + 20f;
        float valueW = width - valueX - 12f;

        int active = 0;
        RectTransform expandedRow = null;
        for (int i = 0; i < _statRowEntries.Length; i++)
        {
            var row = (RectTransform)_statRowEntries[i].ValueTmp.transform.parent;
            if (!row.gameObject.activeSelf) continue;
            active++;
            if (i == _expandedStatIndex) expandedRow = row;
        }
        if (active == 0) return;

        float minRow = UIScale.RowSm;
        float rowH   = Mathf.Max(minRow, avail / active);
        float openH  = 0f;
        if (expandedRow != null)
        {
            var v = _statRowEntries[_expandedStatIndex].ValueTmp;
            float want = v.GetPreferredValues(v.text, valueW, Mathf.Infinity).y + 16f;
            openH = Mathf.Clamp(want, rowH, avail - (active - 1) * minRow);
            rowH  = Mathf.Max(minRow, (avail - openH) / (active - 1));
        }

        float y = 0f;
        foreach (var entry in _statRowEntries)
        {
            var row = (RectTransform)entry.ValueTmp.transform.parent;
            if (!row.gameObject.activeSelf) continue;

            bool open = row == expandedRow;
            float h = open ? openH : rowH;
            PlaceRow(row, y, h);
            y += h;

            // [아이콘] 이름 ········ 값 — 아이콘은 접힌 행 높이에 맞춘 정사각, 행 위쪽 줄에 둔다
            var icon  = (RectTransform)row.Find("Icon");
            float iconSz = Mathf.Min(rowH - 8f, UIScale.RowMd);
            icon.anchorMin = icon.anchorMax = new Vector2(0f, 1f);
            icon.pivot     = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(12f, -rowH * 0.5f);
            icon.sizeDelta = new Vector2(iconSz, iconSz);
            float labelX = icon.GetComponent<Image>().enabled ? 12f + iconSz + 10f : 12f;

            FitLine(row.Find("Label").GetComponent<TextMeshProUGUI>(), labelX, labelW - (labelX - 12f), wrap: false);
            FitLine(entry.ValueTmp, valueX, valueW, wrap: open);
        }
    }

    // 행 높이를 채우는 글자 칸 — 넘치면 줄어든다 (FontSm 의 60% 까지)
    static void FitLine(TextMeshProUGUI text, float x, float width, bool wrap)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = UIScale.FontSm;
        text.fontSizeMin = UIScale.FontSm * 0.6f;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot     = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(x, 4f);
        rect.offsetMax = new Vector2(x + width, -4f);
    }

    float LayoutSkill(TextMeshProUGUI title, TextMeshProUGUI description, float y, float fontSize)
    {
        float left = title.rectTransform.offsetMin.x;
        float width = _localizedSkills.rect.width - left - 20f;
        float titleHeight = PlaceText(title, left, 12f, width, UIScale.Line(fontSize), fontSize);
        float descHeight = PlaceText(description, left, 20f + titleHeight, width, UIScale.RowSm, UIScale.FontSm);
        float height = Mathf.Max(152f, titleHeight + descHeight + 36f);
        PlaceRow((RectTransform)title.transform.parent, y, height);
        return y + height + 12f;
    }

    static void PlaceRow(RectTransform row, float y, float height)
    {
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = Vector2.one;
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -y);
        row.sizeDelta = new Vector2(0f, height);
    }

    static float PlaceText(TextMeshProUGUI text, float x, float y, float width, float minHeight, float fontSize)
    {
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        float height = Mathf.Max(minHeight, text.GetPreferredValues(text.text, Mathf.Max(1f, width), Mathf.Infinity).y);
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return height;
    }

    protected override void OnAfterOpen()
    {
        ApplyCostIcon(_levelUpCostIcon,   eItem.Gold);
        ApplyCostIcon(_soldierUpCostIcon, eItem.SoldierShard);
        ApplyCostIcon(_gradeUpCostIcon,   eItem.GeneralUpgradeStone);
        foreach (var slot in _equipSlots)
            ApplyCostIcon(slot.EnhanceCostIcon, eItem.EquipUpgradeStone);

        if (_entry != null) RefreshUI();
    }

    static void ApplyCostIcon(Image img, eItem item)
    {
        var sprite = SpriteManager.Instance?.Get(item.IconKey());
        if (sprite == null) return;
        img.sprite = sprite;
        img.color  = Color.white;
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    void RefreshUI()
    {
        UnitJob job = UnitJobRoller.GetJob(_entry.UnitName);
        _statResult = HeroStatResolver.Resolve(_entry);
        Color gc    = GradeStyle.GetColor(_entry.Grade);

        _gradeBorder.color = gc;
        _gradeBadge.color  = gc;
        // ⚠ '?' 같은 표시를 라벨에 이어 붙이지 않는다
        //   배지가 120×56 뿐이라 글자가 늘면 두 줄로 접히고, 등급과 품질이
        //   아래위로 갈라져 하나의 값으로 안 읽힌다.
        //   "누를 수 있다" 는 신호는 배지 모서리의 ⓘ 배지가 맡는다 (Creator 참고).
        _gradeText.text    = GradeStyle.GetLabelWithQuality(_entry.Grade, _entry.UnitName);
        _gradeText.color   = Color.white;
        RefreshCommandHint();
        _nameText.text     = CodexMark.ForGeneral(_entry.UnitName);
        if (_nameShadowText != null) _nameShadowText.text = LocalizationManager.Instance.Get(_entry.UnitName);
        _levelText.text    = $"Lv.{_entry.Level}";
        _jobText.text      = JobStyle.GetLabel(job);

        RefreshAllStatTexts();
        FillSkills(job, _entry);
        RefreshEquipSlots();
        RefreshGrowthDisplay();
        RefreshRankRow();

        UnitPortraitHelper.Render(_entry.UnitName, job, _entry.Grade,
            _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
    }

    void FillSkills(UnitJob job, UnitEntry entry)
    {
        var activeDb  = ActiveSkillDatabase.Current;
        var passiveDb = PassiveSkillDatabase.Current;

        var activeId   = RareSkillArbiter.Resolve(entry.UnitName, job, activeDb, entry.Grade);
        var activeData = activeDb.Get(activeId);
        _activeSkillText.text     = activeData != null ? LocalizationManager.Instance.Get(activeData.SkillName) : "-";
        _activeSkillDescText.text = activeData != null ? LocalizationManager.Instance.LocalizeText(activeData.Description) : "";

        // ⚠ 그림이 없을 때 어두운 색을 칠하지 않는다 (2026-08-26)
        //   아이콘 홈이 흰색 계열로 바뀌면서 그 색이 '어두운 사각형' 으로 또렷하게 보인다.
        //   빈 칸은 홈만 남아야 빈 칸으로 읽힌다.
        var sp = SpriteManager.Instance?.Get(activeId.IconKey());
        _activeSkillIcon.sprite  = sp;
        _activeSkillIcon.color   = Color.white;
        _activeSkillIcon.enabled = sp != null;

        var (s0, s1, s2)          = PassiveSkillRoller.Roll(entry.UnitName);
        int slotCount             = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
        PassiveSkillType[] passives = { s0, s1, s2 };

        for (int i = 0; i < _passiveBoxes.Length; i++)
        {
            bool show = i < slotCount;
            _passiveBoxes[i].SetActive(show);
            if (!show) continue;

            var pd = passiveDb.Get(passives[i]);
            _passiveNameTexts[i].text = pd != null ? LocalizationManager.Instance.Get(pd.SkillName) : "-";
            _passiveDescTexts[i].text = pd != null ? LocalizationManager.Instance.LocalizeText(pd.Description) : "";

            // 이름을 '패시브' 색(초록)으로 — 스탯 창에서 초록으로 뜬 수치가
            // 어느 패시브에서 왔는지 색으로 이어진다.
            // ⚠ 액티브 스킬 이름은 칠하지 않는다 — 스탯을 올리는 출처가 아니다.
            _passiveNameTexts[i].color = StatBonusColors.PassiveColor;

            if (_passiveIcons != null && i < _passiveIcons.Length && _passiveIcons[i] != null)
            {
                var pic = pd?.Icon;
                _passiveIcons[i].sprite  = pic;
                _passiveIcons[i].color   = Color.white;
                _passiveIcons[i].enabled = pic != null;   // 위 액티브와 같은 이유
            }
        }
    }

    // ── 장비 ─────────────────────────────────────────────────

    void RefreshEquipSlots()
    {
        var db     = EquipmentDatabase.Current;
        int stones = UserDataManager.Instance.Get<ItemData>().Get(eItem.EquipUpgradeStone);

        // 열린 슬롯 수는 EquipmentApplier 가 소유한다 (3번째 칸은 특성으로 해금)
        int openSlots = EquipmentApplier.ActiveSlotCount;

        for (int i = 0; i < EquipSlotCount; i++)
        {
            var slot = _equipSlots[i];
            slot.gameObject.SetActive(i < openSlots);
            if (!slot.gameObject.activeSelf) continue;

            string id    = GetEquipId(i);
            var    equip = db.Get(id);

            if (equip == null) { slot.SetEmpty(); continue; }

            int enhance = GetEnhanceLevel(i);
            slot.SetEquipment(equip, enhance, GetEnhanceCost(enhance), stones);
        }
    }

    string GetEquipId(int slot)
        => _entry.RunEquipSlots != null && slot < _entry.RunEquipSlots.Length
            ? _entry.RunEquipSlots[slot] : "";

    int GetEnhanceLevel(int slot)
        => _entry.RunEquipEnhance != null && slot < _entry.RunEquipEnhance.Length
            ? _entry.RunEquipEnhance[slot] : 0;

    static int GetEnhanceCost(int currentEnhance) => (currentEnhance + 1) * 2;

    // 예전 "장비" 탭과 같은 창을 연다. 팝업 위치는 스킬 열 위로 맞춰 놨다.
    void OnEquipSlotClick(int slot)
    {
        var pm = PopupManager.Instance;
        var popup = pm.IsOpen(PopupType.EquipCompare)
            ? pm.Get<EquipComparePopup>(PopupType.EquipCompare)
            : pm.Open<EquipComparePopup>(PopupType.EquipCompare);

        var entry = _entry;
        popup.Setup(entry, slot, () =>
        {
            _entry = UserDataManager.Instance.Get<UnitData>().GetUnit(entry.UnitName);
            if (_entry != null) RefreshUI();
        });
    }

    void OnEnhanceClick(int slot)
    {
        string id = GetEquipId(slot);
        if (string.IsNullOrEmpty(id)) return;

        var items    = UserDataManager.Instance.Get<ItemData>();
        var unitData = UserDataManager.Instance.Get<UnitData>();

        int enhance = GetEnhanceLevel(slot);
        if (!items.Spend(eItem.EquipUpgradeStone, GetEnhanceCost(enhance))) return;

        unitData.SetEquipment(_entry.UnitName, slot, id, enhance + 1);
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        RefreshUI();

        // 칸 전체를 기준으로 준다 — 강화 버튼이 그 안에 있으니 터지는 자리는 손가락이 되고,
        // 펀치는 칸 전체에 걸려 3칸 중 어디가 올랐는지 한눈에 보인다
        UIJuice.EquipEnhance(_equipSlots[slot].transform as RectTransform, enhance + 1);
    }

    // ── 성장 (레벨업 · 용병) ──────────────────────────────────

    void OnLevelUpClick()
    {
        var items    = UserDataManager.Instance.Get<ItemData>();
        var unitData = UserDataManager.Instance.Get<UnitData>();

        if (!items.Spend(eItem.Gold, GetLevelUpCost(_entry.Level))) return;

        unitData.SetUnitLevel(_entry.UnitName, _entry.Level + 1);
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        RefreshUI();

        // ⚠ RefreshUI 뒤에 터뜨린다 — 숫자가 새 값으로 바뀐 뒤라야 "오르면서 터진다" 로 읽힌다
        // 기준은 누른 버튼이다 — 실제로는 그 안의 손가락 자리에서 터진다 (UIJuiceLayer.ResolveOrigin)
        UIJuice.LevelUp(_levelUpBtn.transform as RectTransform, _entry.Level);
    }

    void OnSoldierUpClick()
    {
        var items    = UserDataManager.Instance.Get<ItemData>();
        var unitData = UserDataManager.Instance.Get<UnitData>();

        if (!items.Spend(eItem.SoldierShard, GetSoldierUpCost(_entry.SoldierBonus))) return;

        unitData.AddSoldierBonus(_entry.UnitName, 1);
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        RefreshUI();

        UIJuice.SoldierUp(_soldierUpBtn.transform as RectTransform);
    }

    // 비용 공식은 GameplayConfig 가 소유한다 — 여기서 따로 계산하지 말 것
    static int GetLevelUpCost(int currentLevel)  => GameplayConfig.HeroLevelUpCost(currentLevel);
    static int GetSoldierUpCost(int currentBonus) => (currentBonus + 1) * 10;

    // ── 등급업 · 해고 ────────────────────────────────────────

    void OnGradeUpClick()
    {
        if (_entry.Grade >= UnitGrade.Epic) return;

        var items    = UserDataManager.Instance.Get<ItemData>();
        var unitData = UserDataManager.Instance.Get<UnitData>();

        int cost = GameplayConfig.GradeUpCost(_entry.Grade);
        if (!items.Spend(eItem.GeneralUpgradeStone, cost)) return;

        unitData.GradeUp(_entry.UnitName);
        // 등급이 바뀌면 직업 시너지 판정 대상(등급별 슬롯 수)도 달라진다
        JobSynergyEvaluator.Recalculate();
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        RefreshUI();

        // 이 게임에서 가장 비싼 성장 — 유일하게 화면이 한 번 번쩍인다
        UIJuice.GradeUp(_gradeUpBtn.transform as RectTransform, GradeStyle.GetLabel(_entry.Grade));
    }

    // ── 등급·품질 설명 ───────────────────────────────────────
    //
    //  배지에 뜨는 "영웅 5" 는 서로 다른 두 값이 붙어 있는 것이다.
    //    영웅 : 등급 (UnitGrade) — 뽑을 때 정해지고 등급업으로 올린다
    //    5    : 품질 (0~10)      — 이름 시드가 정한 굴림, 영영 바뀌지 않는다
    //  둘 다 "높을수록 세다" 인데 한 칸에 붙어 있어 하나의 값처럼 읽힌다.

    void ShowGradeTooltip()
    {
        if (_gradeTooltip == null || _gradeInfoBtn == null) return;

        // ⚠ 등급 이름·색을 여기 문자열로 적지 않는다
        //   GradeStyle 이 정본이다. 손으로 적으면 이 툴팁만 다른 이름·색을 쓰게 되고,
        //   화면마다 같은 등급이 다른 말과 다른 색으로 불린다.
        //   구분자 '>' 는 ASCII 라 폰트에 있다 (UI 규칙 2).
        var sb    = new System.Text.StringBuilder();
        var order = (UnitGrade[])System.Enum.GetValues(typeof(UnitGrade));
        for (int i = 0; i < order.Length; i++)
        {
            if (i > 0) sb.Append("<color=#808080> > </color>");

            string hex = ColorUtility.ToHtmlStringRGB(GradeStyle.GetColor(order[i]));
            sb.Append($"<color=#{hex}>{LocalizationManager.Instance.Get(GradeStyle.GetLabel(order[i]))}</color>");
        }

        _gradeTooltip.ShowAnchored(
            _gradeInfoBtn.transform as RectTransform,
            "등급과 품질",
            LocalizationManager.Instance.Format("{0}\n오른쪽으로 갈수록 기본 스탯이 높다.", sb.ToString()),
            "옆의 숫자는 품질(1~9)이다.\n같은 등급이라도 숫자가 클수록 스탯이 높다.");
    }

    // ── 지휘력 안내 (헤더) ───────────────────────────────────

    /// <summary>
    /// "지휘력 +1 › 용병 스탯 +N%" (앞의 ※ 는 창을 만들 때 도형으로 그려 둔다).
    ///
    /// ⚠ 수치를 문자열에 박지 않는다
    ///   환산율의 정본은 SoldierRuntimeBridge.StatRatio 이고 계수는
    ///   GameplayConfig.SoldierRatioPerCommandPower 다. 여기에 1% 를 적어 두면
    ///   기획이 그 값을 만지는 순간 이 문장만 거짓말이 된다.
    /// </summary>
    void RefreshCommandHint()
    {
        if (_commandHintText == null) return;

        var   cfg    = GameplayConfig.Current;
        float perCmd = cfg != null ? cfg.SoldierRatioPerCommandPower : 0.01f;

        // ⚠ 화살표(→ U+2192)를 쓰지 않는다 — 폰트에 없어서 □ 로 뜬다 (UI 규칙 2)
        //   폰트에 있는 › (U+203A) 로 대신한다. 앞의 ※ 는 도형으로 그려 뒀다.
        _commandHintText.text = LocalizationManager.Instance.Format(
            "지휘력 +1 › 용병 스탯 +{0:0.#}%", perCmd * 100f);
    }

    /// <summary>
    /// 배치 장수를 해고한다 — 마지막 1명은 해고할 수 없다(전투 불가).
    /// 실제 처리는 GeneralRoster 가 소유한다 (용병 고용 팝업도 같은 문을 쓴다).
    /// </summary>
    void OnFireClick()
    {
        if (!GeneralRoster.Fire(_entry.UnitName)) return;

        Close();   // 해고한 장수의 상세를 계속 띄워 둘 수 없다
    }

    void RefreshRankRow()
    {
        bool isMax = _entry.Grade >= UnitGrade.Epic;
        int  cost  = GameplayConfig.GradeUpCost(_entry.Grade);
        int  owned = UserDataManager.Instance.Get<ItemData>().Get(eItem.GeneralUpgradeStone);

        _gradeUpBtn.interactable  = !isMax && owned >= cost;
        _gradeUpCostText.text     = isMax ? "MAX" : $"{cost}";
        _gradeUpCostText.color    = isMax          ? new Color(0.70f, 0.72f, 0.80f)
                                  : owned >= cost  ? new Color(1.00f, 0.85f, 0.20f)
                                                   : new Color(0.90f, 0.35f, 0.35f);
        // MAX 면 강화석 아이콘은 의미가 없다
        _gradeUpCostIcon.gameObject.SetActive(!isMax);

        _fireBtn.interactable = GeneralRoster.CanFire();
    }

    void RefreshGrowthDisplay()
    {
        var items = UserDataManager.Instance.Get<ItemData>();

        int lvCost = GetLevelUpCost(_entry.Level);
        _levelUpCostText.text  = $"{lvCost:N0}";
        _levelUpCostText.color = items.Get(eItem.Gold) >= lvCost
            ? new Color(1.00f, 0.85f, 0.20f) : new Color(0.90f, 0.35f, 0.35f);

        int sdCost = GetSoldierUpCost(_entry.SoldierBonus);
        _soldierUpCostText.text  = $"{sdCost}";
        _soldierUpCostText.color = items.Get(eItem.SoldierShard) >= sdCost
            ? new Color(0.85f, 0.90f, 1.00f) : new Color(0.90f, 0.35f, 0.35f);

        int expPerLevel = GameplayConfig.Current != null ? GameplayConfig.Current.ExpPerLevel : 100;
        int expNeeded   = _entry.Level * expPerLevel;
        _expText.text   = $"{_entry.Exp:N0} / {expNeeded:N0} EXP";
        _expBarFill.rectTransform.anchorMax = new Vector2(
            expNeeded > 0 ? Mathf.Clamp01((float)_entry.Exp / expNeeded) : 0f, 1f);
    }

    // ── 닫기 ─────────────────────────────────────────────────

    void OnCloseClick()
    {
        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.EquipCompare))
            pm.Get<EquipComparePopup>(PopupType.EquipCompare)?.Close();
        Close();
    }

    // ── 스탯 행 (클릭 → 출처별 분해) ──────────────────────────

    void SetupStatClickHandlers()
    {
        var defs = new (TextMeshProUGUI tmp, StatType type)[]
        {
            // 순서는 화면에 놓인 행 순서와 맞춘다 (HeroDetailPopupCreator.BuildStatColumn).
            // 동작상 필수는 아니지만, 어긋나면 나중에 행을 옮길 때 대조가 안 된다.
            (_hpText,           StatType.MaxHp),
            (_atkText,          StatType.Attack),
            (_defText,          StatType.Defense),
            (_soldierCountText, StatType.SoldierCount),
            (_spdText,          StatType.MoveSpeed),
            (_atkSpdText,       StatType.AttackSpeed),
            (_rangeText,        StatType.AttackRange),
            (_cmdPwrText,       StatType.CommandPower),
            (_cooldownText,     StatType.SkillCooldownReduce),
            (_critChanceText,   StatType.CritChance),
            (_critDmgText,      StatType.CritDamage),
        };

        _statRowEntries = new StatRowEntry[defs.Length];

        for (int i = 0; i < defs.Length; i++)
        {
            var (tmp, type) = defs[i];
            var rowGo = tmp.transform.parent.gameObject;

            int idx = i;
            rowGo.GetComponent<Button>().onClick.AddListener(() => ToggleStatRow(idx));

            // 스탯 아이콘 — 이 목록이 다른 화면의 '아이콘만 있는 스탯' 의 범례다.
            //  그림이 아직 없는 스탯(Codex 대기)은 아이콘 칸을 끄고 라벨만 보인다.
            var icon = rowGo.transform.Find("Icon").GetComponent<Image>();
            icon.sprite  = SpriteManager.Instance.Get(StatIcon.Key(type));
            icon.enabled = icon.sprite != null;

            _statRowEntries[i] = new StatRowEntry { ValueTmp = tmp, Type = type };
        }
    }

    void ToggleStatRow(int index)
    {
        _expandedStatIndex = (_expandedStatIndex == index) ? -1 : index;
        RefreshAllStatTexts();
    }

    // ── 장수 / 용병 전환 ──────────────────────────────────────
    //  용병은 장수 스탯을 그대로 물려받아 배율만 곱한 값이라 같은 행을 다시 쓴다.
    //  용병에게 의미가 없는 세 줄(용병 수·지휘력·스킬 쿨타임)만 감춘다.

    void SetStatTarget(bool soldier)
    {
        _showSoldier       = soldier;
        _expandedStatIndex = -1;

        foreach (var row in _generalOnlyRows)
            row.SetActive(!soldier);

        StyleStatTab(_generalTabBtn, !soldier);
        StyleStatTab(_soldierTabBtn,  soldier);

        if (_statResult != null) RefreshAllStatTexts();
    }

    // 탭 바탕(Body Image) · 라벨 · 밑줄을 한꺼번에 바꾼다.
    // Body 는 입체 버튼의 targetGraphic 이라 여기서 색만 갈아 끼우면 된다.
    static readonly Color TabFaceOn  = new(0.20f, 0.38f, 0.62f);
    static readonly Color TabFaceOff = new(0.15f, 0.16f, 0.25f);
    static readonly Color TabTextOn  = new(0.90f, 0.96f, 1.00f);
    static readonly Color TabTextOff = new(0.58f, 0.60f, 0.72f);

    static void StyleStatTab(Button btn, bool active)
    {
        if (btn.targetGraphic != null)
            btn.targetGraphic.color = active ? TabFaceOn : TabFaceOff;

        var lbl = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        lbl.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        lbl.color     = active ? TabTextOn : TabTextOff;

        var bar = btn.transform.Find("Body/ActiveBar");
        if (bar != null) bar.gameObject.SetActive(active);
    }

    /// <summary>
    /// 용병 탭일 때 이 스탯에 곱할 배율.
    /// 공식은 SoldierRuntimeBridge 가 소유한다 — 실제 전투 병사와 같은 값이다.
    /// </summary>
    float SoldierScale(StatType type)
    {
        if (!_showSoldier || SoldierRuntimeBridge.IsUnscaled(type)) return 1f;
        return SoldierRuntimeBridge.StatRatio(_statResult.Total(StatType.CommandPower));
    }

    void RefreshAllStatTexts()
    {
        if (_statRowEntries == null) return;
        for (int i = 0; i < _statRowEntries.Length; i++)
            RefreshStatRow(i);
    }

    // ⚠ overflowMode 를 Ellipsis 로 두면 안 된다.
    //   분해 문자열("1,200 +300 +157")이 칸보다 길면 TMP 가 통째로 "..." 으로 바꿔
    //   숫자가 아예 안 보였다. 대신 AutoSize 로 줄여서 담는다 (칸 높이는 그대로).
    void RefreshStatRow(int index)
    {
        var row = _statRowEntries[index];

        // 용병 배율은 출처별 값에 그대로 곱해도 된다 — 선형이라 분해가 그대로 성립한다.
        float k          = SoldierScale(row.Type);
        float baseVal    = _statResult.Base.Get(row.Type)   * k;
        float equipVal   = _statResult.GetEquip(row.Type)   * k;
        float traitVal   = _statResult.GetTrait(row.Type)   * k;
        float codexVal   = _statResult.GetCodex(row.Type)   * k;

        // ⚠ 어빌리티·유물은 장수 전용 몫이 섞여 있다
        //   "장군의 위엄"(Unit_General) 같은 옵션은 병사에게 가지 않는데,
        //   출처 총합을 그대로 환산하면 용병 탭에 그 몫까지 얹혀 보였다.
        //   병사 탭에서는 장수 전용을 걷어낸 사본에서 읽는다 (전투와 같은 값).
        float abilityVal = (_showSoldier
            ? _statResult.GetForSoldier(HeroStatPipeline.AbilityKey, row.Type)
            : _statResult.GetAbility(row.Type)) * k;
        float relicVal   = (_showSoldier
            ? _statResult.GetForSoldier(HeroStatPipeline.RelicKey, row.Type)
            : _statResult.GetRelic(row.Type)) * k;

        // ── 패시브 칸은 탭마다 출처가 다르다 ──────────────────
        //  장수 : Target.General 몫
        //  용병 : Target.Soldier 몫 (장수 몫은 장수 전용이라 병사에게 안 간다)
        //
        //  ⚠ 예전엔 장수 몫에 배율만 곱해 보여 줬다
        //    "강한 장군, 약한 병사"(장군 +30% / 병사 -20%)의 용병 탭에 +30% 가
        //    환산돼 뜨고 정작 -20% 는 어디에도 없었다 — 부호가 반대로 보였다.
        //
        //  ⚠ 비율은 '환산된 병사 스탯' 에 곱한다
        //    전투(SoldierStatApplier)가 환산 직후 Base 스냅샷에 곱하므로 같은 기준이어야 한다.
        //    절대값(Flat)은 환산 없이 그대로 더한다 — 이것도 전투와 같다.
        float passiveVal;
        if (_showSoldier)
        {
            float inherited = baseVal + equipVal + abilityVal + relicVal + traitVal + codexVal;
            passiveVal = inherited * _statResult.GetSoldierPassiveRatio(row.Type)
                       + _statResult.GetSoldierPassiveFlat(row.Type);
        }
        else
        {
            passiveVal = _statResult.GetPassive(row.Type);
        }

        float total = baseVal + equipVal + passiveVal + abilityVal + relicVal + traitVal + codexVal;

        bool hasBonus = equipVal != 0f || passiveVal != 0f || abilityVal != 0f
                     || relicVal != 0f || traitVal   != 0f || codexVal   != 0f;

        row.ValueTmp.text = (index == _expandedStatIndex && hasBonus)
            ? StatDisplayHelper.BuildBreakdown(row.Type, baseVal, equipVal, passiveVal, abilityVal, relicVal, traitVal, codexVal)
            : StatDisplayHelper.FormatStat(row.Type, total, isFinal: true);
    }
}
