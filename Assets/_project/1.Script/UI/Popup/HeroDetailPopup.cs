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
        _levelUpBtn.onClick.AddListener(OnLevelUpClick);
        _soldierUpBtn.onClick.AddListener(OnSoldierUpClick);
        _gradeUpBtn.onClick.AddListener(OnGradeUpClick);
        _fireBtn.onClick.AddListener(OnFireClick);

        _generalTabBtn.onClick.AddListener(() => SetStatTarget(soldier: false));
        _soldierTabBtn.onClick.AddListener(() => SetStatTarget(soldier: true));

        for (int i = 0; i < EquipSlotCount; i++)
        {
            int slot = i;
            _equipSlots[i].Bind(() => OnEquipSlotClick(slot), () => OnEnhanceClick(slot));
        }

        SetupStatClickHandlers();
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
        _gradeText.text    = GradeStyle.GetLabelWithQuality(_entry.Grade, _entry.UnitName);
        _gradeText.color   = Color.white;
        _nameText.text     = CodexMark.ForGeneral(_entry.UnitName);
        if (_nameShadowText != null) _nameShadowText.text = _entry.UnitName;
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
        _activeSkillText.text     = activeData?.SkillName   ?? "-";
        _activeSkillDescText.text = activeData?.Description ?? "";

        var sp = SpriteManager.Instance?.Get(activeId.IconKey());
        _activeSkillIcon.sprite = sp;
        _activeSkillIcon.color  = sp != null ? Color.white : new Color(0.25f, 0.30f, 0.48f);

        var (s0, s1, s2)          = PassiveSkillRoller.Roll(entry.UnitName);
        int slotCount             = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
        PassiveSkillType[] passives = { s0, s1, s2 };

        for (int i = 0; i < _passiveBoxes.Length; i++)
        {
            bool show = i < slotCount;
            _passiveBoxes[i].SetActive(show);
            if (!show) continue;

            var pd = passiveDb.Get(passives[i]);
            _passiveNameTexts[i].text = pd?.SkillName   ?? "-";
            _passiveDescTexts[i].text = pd?.Description ?? "";

            if (_passiveIcons != null && i < _passiveIcons.Length && _passiveIcons[i] != null)
            {
                var pic = pd?.Icon;
                _passiveIcons[i].sprite = pic;
                _passiveIcons[i].color  = pic != null ? Color.white : new Color(0.25f, 0.24f, 0.40f);
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

    /// <summary>배치 장수를 해고한다 — 마지막 1명은 해고할 수 없다(전투 불가).</summary>
    void OnFireClick()
    {
        if (!CanFire()) return;

        var unitData   = UserDataManager.Instance.Get<UnitData>();
        var deployData = UserDataManager.Instance.Get<DeploymentData>();

        deployData.Undeploy(_entry.UnitName);
        unitData.RemoveUnit(_entry.UnitName);
        JobSynergyEvaluator.Recalculate();
        UserDataManager.Instance.RequestSave();

        Close();   // 해고한 장수의 상세를 계속 띄워 둘 수 없다
    }

    /// <summary>배치된 장수가 2명 이상이어야 해고 가능.</summary>
    static bool CanFire()
    {
        var deployData = UserDataManager.Instance.Get<DeploymentData>();
        return deployData.GetDeployedUnits().Count > 1;
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

        _fireBtn.interactable = CanFire();
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
        float abilityVal = _statResult.GetAbility(row.Type) * k;
        float relicVal   = _statResult.GetRelic(row.Type)   * k;
        float traitVal   = _statResult.GetTrait(row.Type)   * k;
        float codexVal   = _statResult.GetCodex(row.Type)   * k;

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
