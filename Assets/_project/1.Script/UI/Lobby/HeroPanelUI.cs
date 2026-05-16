using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ============================================================
//  HeroPanelUI.cs
//  영웅(장군) 탭 패널
//
//  장비 비교: 슬롯 클릭 → PopupManager.Open<EquipComparePopup>().Setup()
// ============================================================

public class HeroPanelUI : MonoBehaviour
{
    // ── 초상화 ────────────────────────────────────────────────
    [Header("초상화")]
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;

    // ── 기본 정보 ─────────────────────────────────────────────
    [Header("기본 정보")]
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] Image           _gradeBadge;
    [SerializeField] TextMeshProUGUI _gradeText;
    [SerializeField] TextMeshProUGUI _jobText;

    // ── 스탯 ──────────────────────────────────────────────────
    [Header("스탯")]
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] TextMeshProUGUI _atkText;
    [SerializeField] TextMeshProUGUI _defText;
    [SerializeField] TextMeshProUGUI _spdText;
    [SerializeField] TextMeshProUGUI _atkSpdText;
    [SerializeField] TextMeshProUGUI _rangeText;
    [SerializeField] TextMeshProUGUI _soldierCountText;
    [SerializeField] TextMeshProUGUI _cmdPwrText;

    // ── 탭 ────────────────────────────────────────────────────
    [Header("탭")]
    [SerializeField] Button[]     _tabButtons;
    [SerializeField] GameObject[] _tabPanels;
    [SerializeField] Color        _tabActiveColor   = new Color(0.35f, 0.62f, 1.00f);
    [SerializeField] Color        _tabInactiveColor = new Color(0.40f, 0.40f, 0.45f);

    // ── 장비 슬롯 ─────────────────────────────────────────────
    [Header("장비 슬롯")]
    [SerializeField] Button          _equip0Btn;
    [SerializeField] Image           _equip0Icon;
    [SerializeField] TextMeshProUGUI _equip0NameText;
    [SerializeField] TextMeshProUGUI _equip0StatText;
    [SerializeField] Image           _equip0GradeBar;
    [SerializeField] GameObject      _equip0LockBadge;
    [SerializeField] Button          _equip0EnhanceBtn;
    [SerializeField] TextMeshProUGUI _equip0EnhanceCostText;
    [SerializeField] Image           _equip0EnhanceCostIcon;
    [SerializeField] Button          _equip1Btn;
    [SerializeField] Image           _equip1Icon;
    [SerializeField] TextMeshProUGUI _equip1NameText;
    [SerializeField] TextMeshProUGUI _equip1StatText;
    [SerializeField] Image           _equip1GradeBar;
    [SerializeField] GameObject      _equip1LockBadge;
    [SerializeField] Button          _equip1EnhanceBtn;
    [SerializeField] TextMeshProUGUI _equip1EnhanceCostText;
    [SerializeField] Image           _equip1EnhanceCostIcon;

    // ── 스킬 ──────────────────────────────────────────────────
    [Header("스킬")]
    [SerializeField] Image            _activeSkillIcon;
    [SerializeField] TextMeshProUGUI  _activeSkillText;
    [SerializeField] TextMeshProUGUI  _activeSkillDescText;

    // ── 패시브 스킬 ───────────────────────────────────────────
    [Header("패시브 스킬")]
    [SerializeField] TextMeshProUGUI _passive0Text;
    [SerializeField] TextMeshProUGUI _passive1Text;
    [SerializeField] TextMeshProUGUI _passive2Text;
    [SerializeField] TextMeshProUGUI _passive0DescText;
    [SerializeField] TextMeshProUGUI _passive1DescText;
    [SerializeField] TextMeshProUGUI _passive2DescText;

    // ── 스킬 DB ───────────────────────────────────────────────
    [Header("스킬 DB")]
    [SerializeField] ActiveSkillDatabase  _activeSkillDatabase;
    [SerializeField] PassiveSkillDatabase _passiveSkillDatabase;

    // ── 배치 슬롯 ────────────────────────────────────────────
    [Header("배치 슬롯")]
    [SerializeField] DeploySlotRowUI _deploySlotRow;

    // ── 레벨업 / EXP ──────────────────────────────────────────
    [Header("레벨업")]
    [SerializeField] Button          _levelUpBtn;
    [SerializeField] TextMeshProUGUI _levelUpCostText;
    [SerializeField] Image           _levelUpCostIcon;
    [SerializeField] TextMeshProUGUI _expText;
    [SerializeField] Image           _expBarFill;
    [SerializeField] Button          _soldierUpBtn;
    [SerializeField] TextMeshProUGUI _soldierUpCostText;
    [SerializeField] Image           _soldierUpCostIcon;

    // ── 영웅 목록 ─────────────────────────────────────────────
    [Header("영웅 목록")]
    [SerializeField] Transform  _listContent;
    [SerializeField] HeroCardUI _cardPrefab;

    // ── 분해 ──────────────────────────────────────────────────
    [Header("분해")]
    [SerializeField] Button _disassembleBtn;

    // ── 용병 고용 ─────────────────────────────────────────────
    [Header("용병 고용")]
    [SerializeField] Button          _hireBtn;
    [SerializeField] TextMeshProUGUI _hireCostText;
    [SerializeField] Image           _hireCostIcon;

    const int HireCost = 500;

    // ── 런타임 ────────────────────────────────────────────────
    readonly List<HeroCardUI> _cards = new();
    UnitEntry _selected;
    Texture2D _portraitTexture;

    // ── 스탯 클릭 상세 ────────────────────────────────────────
    HeroStatResult _statResult;
    int            _expandedStatIndex = -1;
    Transform      _statListContainer;

    struct StatRowEntry
    {
        public TextMeshProUGUI ValueTmp;
        public LayoutElement   LayoutEl;
        public StatType        Type;
    }
    StatRowEntry[] _statRowEntries;

    // 색상·포맷은 UIConstants.cs 의 StatBonusColors / StatDisplayHelper 공통 사용

    // ── 라이프사이클 ──────────────────────────────────────────

    void Start()
    {
        ApplyCostIcon(_levelUpCostIcon,      eItem.Gold);
        ApplyCostIcon(_soldierUpCostIcon,    eItem.SoldierShard);
        ApplyCostIcon(_hireCostIcon,         eItem.Gold);
        ApplyCostIcon(_equip0EnhanceCostIcon, eItem.EquipUpgradeStone);
        ApplyCostIcon(_equip1EnhanceCostIcon, eItem.EquipUpgradeStone);
    }

    static void ApplyCostIcon(Image img, eItem item)
    {
        if (img == null) return;
        var sprite = SpriteManager.Instance?.GetItem(item.IconKey());
        if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
    }

    void Awake()
    {
        AutoWireFallback();

        _equip0Btn?.onClick.AddListener(() => OnEquipSlotClick(0));
        _equip1Btn?.onClick.AddListener(() => OnEquipSlotClick(1));
        _equip0EnhanceBtn?.onClick.AddListener(() => OnEnhanceClick(0));
        _equip1EnhanceBtn?.onClick.AddListener(() => OnEnhanceClick(1));

        FixStatLabelAlignment();

        if (_tabButtons != null)
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int idx = i;
                _tabButtons[i]?.onClick.AddListener(() => SwitchTab(idx));
            }
        _levelUpBtn?.onClick.AddListener(OnLevelUpClick);
        _soldierUpBtn?.onClick.AddListener(OnSoldierUpClick);
        _hireBtn?.onClick.AddListener(OnHireClick);
        _disassembleBtn?.onClick.AddListener(OnDisassembleClick);
        _deploySlotRow.OnDeployChanged += RefreshCardDeployBadges;
        SetupStatClickHandlers();
        SwitchTab(0);
    }

    void OnEnable() => Refresh();

    // ── 공개 API ──────────────────────────────────────────────

    public void SwitchTab(int index)
    {
        if (_tabPanels != null)
            for (int i = 0; i < _tabPanels.Length; i++)
                _tabPanels[i]?.SetActive(i == index);

        if (_tabButtons != null)
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var activeBar = _tabButtons[i].transform.Find("ActiveBar");
                if (activeBar != null) activeBar.gameObject.SetActive(i == index);
                var tmp = _tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.color = (i == index) ? _tabActiveColor : _tabInactiveColor;
            }
    }

    public void Refresh()
    {
        var units = UserDataManager.Instance?.Get<UnitData>()?.Units;
        if (units == null) return;
        BuildCardList(units);
        if (_cards.Count > 0)
            SelectHero(_cards[0].Entry);
        RefreshHireCostDisplay();
    }

    // ── 내부 ──────────────────────────────────────────────────

    void BuildCardList(IReadOnlyList<UnitEntry> units)
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        foreach (var entry in units)
        {
            var card = Instantiate(_cardPrefab, _listContent);
            card.Setup(entry, SelectHero);
            _cards.Add(card);
        }
    }

    void SelectHero(UnitEntry entry)
    {
        _selected = entry;
        foreach (var c in _cards)
            c.SetSelected(c.Entry == entry);
        UpdateDetail(entry);
        _deploySlotRow.Setup(_selected, UserDataManager.Instance.Get<DeploymentData>());
    }

    void UpdateDetail(UnitEntry entry)
    {
        UnitJob job = UnitJobRoller.GetJob(entry.UnitName);

        UpdatePortrait(entry, job);

        _nameText.text  = entry.UnitName;
        _levelText.text = $"Lv.{entry.Level}";
        _jobText.text   = JobStyle.GetLabel(job);
        _gradeText.text = GradeStyle.GetLabel(entry.Grade);

        Color gc          = GradeStyle.GetColor(entry.Grade);
        _gradeBadge.color = gc;
        _gradeText.color  = gc;

        // 스탯 (기본 + 패시브 + 장비 일괄 계산)
        _statResult        = HeroStatResolver.Resolve(entry);
        _expandedStatIndex = -1;
        RefreshAllStatTexts();

        // 장비 슬롯
        var equipDb = EquipmentDatabase.Current;
        RefreshEquipSlot(0, entry, equipDb, _equip0NameText, _equip0GradeBar, _equip0Icon, _equip0StatText, _equip0LockBadge);
        RefreshEquipSlot(1, entry, equipDb, _equip1NameText, _equip1GradeBar, _equip1Icon, _equip1StatText, _equip1LockBadge);

        // 스킬
        var activeDb  = _activeSkillDatabase != null ? _activeSkillDatabase : ActiveSkillDatabase.Current;
        var rolledId  = ActiveSkillRoller.Roll(entry.UnitName, job, activeDb);
        var skillData = activeDb?.Get(rolledId);
        _activeSkillText.text = LocalizationManager.Instance.Get(rolledId.ToString());
        if (_activeSkillDescText != null)
            _activeSkillDescText.text = skillData != null ? skillData.Description : "";
        if (_activeSkillIcon != null)
        {
            var key = rolledId.IconKey();
            var sp  = key != null ? SpriteManager.Instance?.GetGeneral(key) : null;
            _activeSkillIcon.sprite = sp;
            _activeSkillIcon.color  = sp != null ? Color.white : new Color(0.25f, 0.30f, 0.48f);
        }

        // 패시브 표시
        var passiveDb = _passiveSkillDatabase != null ? _passiveSkillDatabase : PassiveSkillDatabase.Current;
        var (p0, p1, p2) = PassiveSkillRoller.Roll(entry.UnitName);
        byte slots = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
        RefreshPassiveSlot(_passive0Text, _passive0DescText, passiveDb, p0, slots >= 1);
        RefreshPassiveSlot(_passive1Text, _passive1DescText, passiveDb, p1, slots >= 2);
        RefreshPassiveSlot(_passive2Text, _passive2DescText, passiveDb, p2, slots >= 3);

        // 레벨업 비용
        if (_levelUpCostText != null)
        {
            int cost = GetLevelUpCost(entry.Level);
            int gold = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
            _levelUpCostText.text  = $"{cost:N0}";
            _levelUpCostText.color = gold >= cost
                ? new Color(1.0f, 0.85f, 0.20f)
                : new Color(0.9f, 0.35f, 0.35f);
        }

        // EXP
        {
            int expPerLevel = GameplayConfig.Current != null ? GameplayConfig.Current.ExpPerLevel : 100;
            int expNeeded   = entry.Level * expPerLevel;
            if (_expText != null)
                _expText.text = $"{entry.Exp:N0} / {expNeeded:N0} EXP";
            if (_expBarFill != null)
                _expBarFill.rectTransform.anchorMax = new Vector2(
                    expNeeded > 0 ? Mathf.Clamp01((float)entry.Exp / expNeeded) : 0f, 1f);
        }

        RefreshSoldierUpDisplay(entry);
        RefreshEnhanceBtns(entry);
    }

    void UpdatePortrait(UnitEntry entry, UnitJob job)
    {
        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
    }

    // ── 장비 슬롯 표시 ────────────────────────────────────────

    void RefreshEquipSlot(int slot, UnitEntry entry, EquipmentDatabase db,
                          TextMeshProUGUI nameText, Image gradeBar,
                          Image iconImage, TextMeshProUGUI statText,
                          GameObject lockBadge = null)
    {
        string id    = (entry.RunEquipSlots != null && slot < entry.RunEquipSlots.Length)
                       ? entry.RunEquipSlots[slot] : "";
        var    equip = db?.Get(id);

        if (equip == null)
        {
            nameText.text = "없음";
            if (gradeBar  != null) gradeBar.color = new Color(0.25f, 0.25f, 0.30f);
            if (iconImage != null) { iconImage.sprite = null; iconImage.color = new Color(0.18f, 0.18f, 0.22f); }
            if (statText  != null) statText.text = "";
            if (lockBadge != null) lockBadge.SetActive(false);
            return;
        }

        int enhance = (entry.RunEquipEnhance != null && slot < entry.RunEquipEnhance.Length)
                      ? entry.RunEquipEnhance[slot] : 0;
        nameText.text = enhance > 0 ? $"{equip.EquipmentName} +{enhance}" : equip.EquipmentName;
        if (gradeBar != null) gradeBar.color = GradeStyle.GetColor(equip.Grade);

        if (iconImage != null)
        {
            iconImage.sprite = equip.Icon;
            iconImage.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade);
        }

        if (statText != null && equip.StatEntries != null)
        {
            var sb  = new System.Text.StringBuilder();
            var loc = LocalizationManager.Instance;
            foreach (var e in equip.StatEntries)
            {
                float val = equip.GetStatValue(e, enhance);
                sb.Append(loc.Get(e.Stat.ToString())).Append(" +");
                sb.AppendLine(EquipmentData.FormatStat(e.Stat, val));
            }
            if (equip.TriggerType != EquipmentTrigger.None)
                sb.AppendLine(EquipmentData.FormatTriggerLine(equip,
                    loc.Get(equip.TriggerType.ToString()),
                    loc.Get(equip.TriggerStat.ToString())));
            statText.text = sb.ToString().TrimEnd();
        }

        // "귀속" 배지 — 장착 중임을 표시
        if (lockBadge != null) lockBadge.SetActive(true);
    }

    // ── 장비 슬롯 클릭 → 비교 팝업 ──────────────────────────────

    void OnEquipSlotClick(int slot)
    {
        if (_selected == null) return;

        var popup = PopupManager.Instance?.Open<EquipComparePopup>(PopupType.EquipCompare);
        if (popup == null) return;

        var entry = _selected;
        popup.Setup(entry, slot, () =>
        {
            var units = UserDataManager.Instance?.Get<UnitData>()?.Units;
            if (units != null) BuildCardList(units);
            _selected = UserDataManager.Instance?.Get<UnitData>()?.GetUnit(entry.UnitName);
            if (_selected != null) UpdateDetail(_selected);
        });
    }

    // ── 장비 강화 ─────────────────────────────────────────────

    static int GetEnhanceCost(int currentEnhance) => (currentEnhance + 1) * 2;

    void RefreshEnhanceBtns(UnitEntry entry)
    {
        RefreshEnhanceBtn(0, entry, _equip0EnhanceBtn, _equip0EnhanceCostText);
        RefreshEnhanceBtn(1, entry, _equip1EnhanceBtn, _equip1EnhanceCostText);
    }

    void RefreshEnhanceBtn(int slot, UnitEntry entry, Button btn, TextMeshProUGUI costText)
    {
        if (btn == null) return;
        string id = (entry?.RunEquipSlots != null && slot < entry.RunEquipSlots.Length)
                    ? entry.RunEquipSlots[slot] : "";
        bool hasEquip = !string.IsNullOrEmpty(id);
        btn.gameObject.SetActive(hasEquip);
        if (!hasEquip || costText == null) return;

        int enhance = (entry.RunEquipEnhance != null && slot < entry.RunEquipEnhance.Length)
                      ? entry.RunEquipEnhance[slot] : 0;
        int cost    = GetEnhanceCost(enhance);
        int stones  = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.EquipUpgradeStone) ?? 0;
        costText.text  = $"{cost}";
        costText.color = stones >= cost
            ? new Color(0.80f, 0.90f, 1.0f)
            : new Color(0.9f, 0.35f, 0.35f);
    }

    void OnEnhanceClick(int slot)
    {
        if (_selected == null) return;
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        string id = (_selected.RunEquipSlots != null && slot < _selected.RunEquipSlots.Length)
                    ? _selected.RunEquipSlots[slot] : "";
        if (string.IsNullOrEmpty(id)) return;

        int enhance = (_selected.RunEquipEnhance != null && slot < _selected.RunEquipEnhance.Length)
                      ? _selected.RunEquipEnhance[slot] : 0;
        int cost = GetEnhanceCost(enhance);

        if (!itemData.CanSpend(eItem.EquipUpgradeStone, cost))
        {
            Debug.Log($"[HeroPanelUI] 장비 강화석 부족 — 필요: {cost}, 보유: {itemData.Get(eItem.EquipUpgradeStone)}");
            return;
        }

        itemData.Spend(eItem.EquipUpgradeStone, cost);
        unitData.SetEquipment(_selected.UnitName, slot, id, enhance + 1);
        UserDataManager.Instance.RequestSave();

        _selected = unitData.GetUnit(_selected.UnitName);
        if (_selected != null) UpdateDetail(_selected);
    }

    // ── 레벨업 ────────────────────────────────────────────────

    void OnLevelUpClick()
    {
        if (_selected == null) return;

        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        int cost = GetLevelUpCost(_selected.Level);
        if (!itemData.CanSpend(eItem.Gold, cost))
        {
            Debug.Log($"[HeroPanelUI] 골드 부족 — 필요: {cost}, 보유: {itemData.Get(eItem.Gold)}");
            return;
        }

        itemData.Spend(eItem.Gold, cost);
        unitData.SetUnitLevel(_selected.UnitName, _selected.Level + 1);
        UserDataManager.Instance.RequestSave();

        _selected = unitData.GetUnit(_selected.UnitName);
        foreach (var c in _cards)
            if (c != null && c.Entry?.UnitName == _selected?.UnitName)
                c.Setup(_selected, SelectHero);
        if (_selected != null) UpdateDetail(_selected);
    }

    static int GetLevelUpCost(int currentLevel) => currentLevel * 100;

    // ── 용병 수 증가 ──────────────────────────────────────────

    static int GetSoldierUpCost(int currentBonus) => (currentBonus + 1) * 10;

    void RefreshSoldierUpDisplay(UnitEntry entry)
    {
        if (_soldierUpCostText == null) return;
        int shards = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.SoldierShard) ?? 0;
        int cost   = GetSoldierUpCost(entry?.SoldierBonus ?? 0);
        _soldierUpCostText.text  = $"{cost}";
        _soldierUpCostText.color = shards >= cost
            ? new Color(0.85f, 0.90f, 1.0f)
            : new Color(0.9f, 0.35f, 0.35f);
    }

    void OnSoldierUpClick()
    {
        if (_selected == null) return;
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        int cost = GetSoldierUpCost(_selected.SoldierBonus);
        if (!itemData.CanSpend(eItem.SoldierShard, cost))
        {
            Debug.Log($"[HeroPanelUI] 용병조각 부족 — 필요: {cost}, 보유: {itemData.Get(eItem.SoldierShard)}");
            return;
        }

        itemData.Spend(eItem.SoldierShard, cost);
        unitData.AddSoldierBonus(_selected.UnitName, 1);
        UserDataManager.Instance.RequestSave();

        _selected = unitData.GetUnit(_selected.UnitName);
        if (_selected != null) UpdateDetail(_selected);
    }

    // ── 분해 ─────────────────────────────────────────────────

    void OnDisassembleClick()
    {
        PopupManager.Instance?.Open<DisassemblePopup>(PopupType.Disassemble);
    }

    // ── 배치 ─────────────────────────────────────────────────

    void RefreshCardDeployBadges()
    {
        var data = UserDataManager.Instance.Get<DeploymentData>();
        foreach (var c in _cards)
            c.RefreshDeploy(data);
    }

    // ── 고용 ─────────────────────────────────────────────────

    void RefreshHireCostDisplay()
    {
        if (_hireCostText == null) return;
        int gold = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
        _hireCostText.text  = $"{HireCost:N0}";
        _hireCostText.color = gold >= HireCost
            ? new Color(1.0f, 0.85f, 0.20f)
            : new Color(0.9f, 0.35f, 0.35f);
    }

    void OnHireClick()
    {
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        if (!itemData.CanSpend(eItem.Gold, HireCost))
        {
            Debug.Log($"[HeroPanelUI] 골드 부족 — 필요: {HireCost}, 보유: {itemData.Get(eItem.Gold)}");
            return;
        }

        itemData.Spend(eItem.Gold, HireCost);
        string hireName = unitData.PickAvailableName() ?? GenerateMercName(unitData);
        unitData.AddUnit(new UnitEntry { UnitName = hireName, Level = 1, Exp = 0 });
        UserDataManager.Instance.RequestSave();
        Refresh();
    }

    static string GenerateMercName(UnitData unitData)
    {
        string name;
        do { name = "용병_" + UnityEngine.Random.Range(10000, 99999); }
        while (unitData.HasUnit(name));
        return name;
    }

    // ── 자동 와이어 (fallback) ────────────────────────────────

    void AutoWireFallback()
    {
        if (_equip0LockBadge == null)
        {
            var s = FindChildRecursive(transform, "EquipSlot0")?.Find("LockBadge");
            if (s != null) _equip0LockBadge = s.gameObject;
        }
        if (_equip1LockBadge == null)
        {
            var s = FindChildRecursive(transform, "EquipSlot1")?.Find("LockBadge");
            if (s != null) _equip1LockBadge = s.gameObject;
        }
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ── 스탯 레이블 정렬 보정 ─────────────────────────────────

    void FixStatLabelAlignment()
    {
        FixStatLabel(_hpText);
        FixStatLabel(_atkText);
        FixStatLabel(_defText);
        FixStatLabel(_spdText);
        FixStatLabel(_atkSpdText);
        FixStatLabel(_rangeText);
        FixStatLabel(_soldierCountText);
        FixStatLabel(_cmdPwrText);
    }

    static void FixStatLabel(TextMeshProUGUI valueText)
    {
        if (valueText == null) return;
        foreach (Transform child in valueText.transform.parent)
        {
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null && tmp != valueText)
            {
                tmp.alignment = TextAlignmentOptions.Left;
                break;
            }
        }
    }

    // ── 패시브 슬롯 ───────────────────────────────────────────

    void RefreshPassiveSlot(TextMeshProUGUI nameText, TextMeshProUGUI descText,
                            PassiveSkillDatabase db, PassiveSkillType type, bool active)
    {
        if (nameText == null) return;
        if (!active)
        {
            nameText.text  = "—";
            nameText.color = new Color(0.40f, 0.40f, 0.40f);
            if (descText != null) descText.text = "";
            return;
        }
        var data       = db != null ? db.Get(type) : null;
        nameText.text  = data != null ? data.SkillName : type.ToString();
        nameText.color = Color.white;
        if (descText != null)
            descText.text = data != null ? data.Description : "";
    }

    // ============================================================
    //  스탯 클릭 상세 표시
    // ============================================================

    void SetupStatClickHandlers()
    {
        var defs = new (TextMeshProUGUI tmp, StatType type)[]
        {
            (_hpText,           StatType.MaxHp),
            (_atkText,          StatType.Attack),
            (_defText,          StatType.Defense),
            (_spdText,          StatType.MoveSpeed),
            (_atkSpdText,       StatType.AttackSpeed),
            (_rangeText,        StatType.AttackRange),
            (_soldierCountText, StatType.SoldierCount),
            (_cmdPwrText,       StatType.CommandPower),
        };

        _statRowEntries = new StatRowEntry[defs.Length];

        for (int i = 0; i < defs.Length; i++)
        {
            var (tmp, type) = defs[i];
            if (tmp == null) continue;

            var rowGo = tmp.transform.parent.gameObject;

            if (!rowGo.TryGetComponent<Image>(out var img))
            { img = rowGo.AddComponent<Image>(); img.color = Color.clear; }

            if (!rowGo.TryGetComponent<Button>(out var btn))
            {
                btn = rowGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.transition    = Selectable.Transition.None;
            }

            int idx = i;
            btn.onClick.AddListener(() => ToggleStatRow(idx));

            if (_statListContainer == null && rowGo.transform.parent != null)
                _statListContainer = rowGo.transform.parent;

            // 래핑 모드는 RefreshStatRow 에서 확장 상태에 따라 제어
            _statRowEntries[i] = new StatRowEntry
            {
                ValueTmp = tmp,
                LayoutEl = rowGo.GetComponent<LayoutElement>(),
                Type     = type,
            };
        }
    }

    void ToggleStatRow(int index)
    {
        _expandedStatIndex = (_expandedStatIndex == index) ? -1 : index;
        RefreshAllStatTexts();
    }

    void RefreshAllStatTexts()
    {
        if (_statRowEntries == null) return;
        for (int i = 0; i < _statRowEntries.Length; i++)
            RefreshStatRow(i);
        if (_statListContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_statListContainer);
    }

    void RefreshStatRow(int index)
    {
        var row = _statRowEntries[index];
        if (row.ValueTmp == null) return;

        float baseVal    = _statResult?.Base.Get(row.Type)    ?? 0f;
        float equipVal   = _statResult?.GetEquip(row.Type)    ?? 0f;
        float passiveVal = _statResult?.GetPassive(row.Type)  ?? 0f;
        float abilityVal = _statResult?.GetAbility(row.Type)  ?? 0f;
        float relicVal   = _statResult?.GetRelic(row.Type)    ?? 0f;
        float total      = baseVal + equipVal + passiveVal + abilityVal + relicVal;
        bool  expanded   = index == _expandedStatIndex;
        bool  hasBonus   = equipVal != 0f || passiveVal != 0f || abilityVal != 0f || relicVal != 0f;

        if (expanded && hasBonus)
        {
            // 합계 숨기고 합산 과정만 표시: "기본  +장비  +패시브  +어빌리티  +유물"
            row.ValueTmp.overflowMode     = TextOverflowModes.Overflow;
            row.ValueTmp.textWrappingMode = TextWrappingModes.NoWrap;
            row.ValueTmp.text = StatDisplayHelper.BuildBreakdown(row.Type, baseVal, equipVal, passiveVal, abilityVal, relicVal);
            if (row.LayoutEl != null) row.LayoutEl.preferredHeight = 50f;
        }
        else
        {
            row.ValueTmp.overflowMode     = TextOverflowModes.Ellipsis;
            row.ValueTmp.textWrappingMode = TextWrappingModes.NoWrap;
            row.ValueTmp.text = StatDisplayHelper.FormatTotal(row.Type, total);
            if (row.LayoutEl != null) row.LayoutEl.preferredHeight = 50f;
        }
    }

}
