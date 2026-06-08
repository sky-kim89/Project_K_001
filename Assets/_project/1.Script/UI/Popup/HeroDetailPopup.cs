using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroDetailPopup.cs
//  배치된 장수 상세 정보 팝업.
//  구조: 초상화 + 탭[스탯/장비/스킬] + 우측 상단 X 닫기 버튼
//
//  Inspector 연결 (HeroDetailPopupCreator 자동 와이어):
//    _gradeBorder      : 좌측 등급 컬러 바 Image
//    _portraitBg       : 초상화 배경 Image
//    _portraitImage    : 초상화 Image
//    _portraitBridge   : UnitAppearanceBridge
//    _nameText         : 이름 TMP
//    _levelText        : 레벨 TMP
//    _jobText          : 직업 TMP
//    _gradeBadge       : 등급 배지 Image
//    _gradeText        : 등급 TMP
//    _tabButtons       : Button[3] — 스탯/장비/스킬
//    _tabPanels        : GameObject[3] — StatPanel/EquipPanel/SkillPanel
//    [스탯] _hpText / _atkText / _defText / _spdText / _atkSpdText
//           _rangeText / _soldierCountText / _cmdPwrText
//    [성장] _expText / _expBarFill / _levelUpBtn / _levelUpCostText / _levelUpCostIcon
//           _soldierUpBtn / _soldierUpCostText / _soldierUpCostIcon
//    [장비] _equip0Btn~_equip1Btn, NameText/GradeBar/Icon/StatText/LockBadge
//           EnhanceBtn/EnhanceCostText/EnhanceCostIcon (×2슬롯)
//    [스킬] _activeSkillIcon / _activeSkillText / _activeSkillDescText
//           _passive0Text~_passive2Text / _passive0DescText~_passive2DescText
//    _closeBtn
// ============================================================

public class HeroDetailPopup : PopupBase
{
    [Header("등급 컬러 바")]
    [SerializeField] Image _gradeBorder;

    [Header("초상화")]
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;

    [Header("기본 정보")]
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] TextMeshProUGUI _jobText;
    [SerializeField] Image           _gradeBadge;
    [SerializeField] TextMeshProUGUI _gradeText;

    [Header("탭")]
    [SerializeField] Button[]     _tabButtons;
    [SerializeField] GameObject[] _tabPanels;

    [Header("스탯")]
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] TextMeshProUGUI _atkText;
    [SerializeField] TextMeshProUGUI _defText;
    [SerializeField] TextMeshProUGUI _spdText;
    [SerializeField] TextMeshProUGUI _atkSpdText;
    [SerializeField] TextMeshProUGUI _rangeText;
    [SerializeField] TextMeshProUGUI _soldierCountText;
    [SerializeField] TextMeshProUGUI _cmdPwrText;
    [SerializeField] TextMeshProUGUI _cooldownText;

    [Header("EXP · 성장")]
    [SerializeField] TextMeshProUGUI _expText;
    [SerializeField] Image           _expBarFill;
    [SerializeField] Button          _levelUpBtn;
    [SerializeField] TextMeshProUGUI _levelUpCostText;
    [SerializeField] Image           _levelUpCostIcon;
    [SerializeField] Button          _soldierUpBtn;
    [SerializeField] TextMeshProUGUI _soldierUpCostText;
    [SerializeField] Image           _soldierUpCostIcon;

    [Header("장비 슬롯 0")]
    [SerializeField] Button          _equip0Btn;
    [SerializeField] TextMeshProUGUI _equip0NameText;
    [SerializeField] Image           _equip0GradeBar;
    [SerializeField] Image           _equip0Icon;
    [SerializeField] TextMeshProUGUI _equip0StatText;
    [SerializeField] GameObject      _equip0LockBadge;
    [SerializeField] Button          _equip0EnhanceBtn;
    [SerializeField] TextMeshProUGUI _equip0EnhanceCostText;
    [SerializeField] Image           _equip0EnhanceCostIcon;

    [Header("장비 슬롯 1")]
    [SerializeField] Button          _equip1Btn;
    [SerializeField] TextMeshProUGUI _equip1NameText;
    [SerializeField] Image           _equip1GradeBar;
    [SerializeField] Image           _equip1Icon;
    [SerializeField] TextMeshProUGUI _equip1StatText;
    [SerializeField] GameObject      _equip1LockBadge;
    [SerializeField] Button          _equip1EnhanceBtn;
    [SerializeField] TextMeshProUGUI _equip1EnhanceCostText;
    [SerializeField] Image           _equip1EnhanceCostIcon;

    [Header("스킬")]
    [SerializeField] Image           _activeSkillIcon;
    [SerializeField] TextMeshProUGUI _activeSkillText;
    [SerializeField] TextMeshProUGUI _activeSkillDescText;
    [SerializeField] TextMeshProUGUI _passive0Text;
    [SerializeField] TextMeshProUGUI _passive1Text;
    [SerializeField] TextMeshProUGUI _passive2Text;
    [SerializeField] TextMeshProUGUI _passive0DescText;
    [SerializeField] TextMeshProUGUI _passive1DescText;
    [SerializeField] TextMeshProUGUI _passive2DescText;

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    [Header("스탯 분해")]
    [SerializeField] Transform _statListContainer;

    [Header("프리뷰 모드 (성장행 컨테이너)")]
    [SerializeField] GameObject _growthRow;

    UnitEntry _entry;
    Texture2D _portraitTexture;

    HeroStatResult _statResult;
    int            _expandedStatIndex = -1;

    struct StatRowEntry
    {
        public TextMeshProUGUI ValueTmp;
        public LayoutElement   LayoutEl;
        public StatType        Type;
    }
    StatRowEntry[] _statRowEntries;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(UnitEntry entry)
    {
        _entry             = entry;
        _expandedStatIndex = -1;

        // 보유 영웅 모드 — SetupPreview에서 숨겼을 수 있으므로 복원
        _growthRow?.SetActive(true);
        _levelText?.gameObject.SetActive(true);
        if (_tabButtons != null && _tabButtons.Length > 1)
            _tabButtons[1]?.gameObject.SetActive(true);

        RefreshUI();
    }

    // 용병 상점에서 호출 — EXP/레벨업/장비 탭 숨김
    public void SetupPreview(UnitEntry entry)
    {
        Setup(entry);

        _growthRow?.SetActive(false);
        _levelText?.gameObject.SetActive(false);

        if (_tabButtons != null && _tabButtons.Length > 1)
            _tabButtons[1]?.gameObject.SetActive(false);
        if (_tabPanels != null && _tabPanels.Length > 1)
            _tabPanels[1]?.SetActive(false);

        SwitchTab(0);
    }

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(OnCloseClick);

        if (_tabButtons != null)
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int idx = i;
                _tabButtons[i]?.onClick.AddListener(() => SwitchTab(idx));
            }

        _levelUpBtn  ?.onClick.AddListener(OnLevelUpClick);
        _soldierUpBtn?.onClick.AddListener(OnSoldierUpClick);
        _equip0Btn   ?.onClick.AddListener(() => OnEquipSlotClick(0));
        _equip1Btn   ?.onClick.AddListener(() => OnEquipSlotClick(1));
        _equip0EnhanceBtn?.onClick.AddListener(() => OnEnhanceClick(0));
        _equip1EnhanceBtn?.onClick.AddListener(() => OnEnhanceClick(1));
        SetupStatClickHandlers();
    }

    protected override void OnAfterOpen()
    {
        ApplyCostIcon(_levelUpCostIcon,    eItem.Gold);
        ApplyCostIcon(_soldierUpCostIcon,  eItem.SoldierShard);
        ApplyCostIcon(_equip0EnhanceCostIcon, eItem.EquipUpgradeStone);
        ApplyCostIcon(_equip1EnhanceCostIcon, eItem.EquipUpgradeStone);

        if (_entry != null) RefreshUI();
        SwitchTab(0);
    }

    static void ApplyCostIcon(Image img, eItem item)
    {
        if (img == null) return;
        var sm = SpriteManager.Instance;
        if (sm == null) return;
        var sprite = sm.Get(item.IconKey());
        if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
    }

    // ── 탭 전환 ───────────────────────────────────────────────

    void SwitchTab(int index)
    {
        if (_tabPanels != null)
            for (int i = 0; i < _tabPanels.Length; i++)
                _tabPanels[i]?.SetActive(i == index);

        if (_tabButtons == null) return;
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            var btn = _tabButtons[i];
            if (btn == null) continue;
            bool active = i == index;

            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
            {
                lbl.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                lbl.color     = active ? new Color(0.40f, 0.72f, 1.00f)
                                       : new Color(0.72f, 0.72f, 0.78f);
            }
            var bar = btn.transform.Find("ActiveBar");
            if (bar != null) bar.gameObject.SetActive(active);
        }
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    void RefreshUI()
    {
        UnitJob job = UnitJobRoller.GetJob(_entry.UnitName);
        _statResult = HeroStatResolver.Resolve(_entry);
        Color gc    = GradeStyle.GetColor(_entry.Grade);

        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeBadge  != null) _gradeBadge.color  = gc;
        if (_gradeText   != null) { _gradeText.text  = GradeStyle.GetLabel(_entry.Grade); _gradeText.color = Color.white; }
        if (_nameText    != null) _nameText.text  = _entry.UnitName;
        if (_levelText   != null) _levelText.text = $"Lv.{_entry.Level}";
        if (_jobText     != null) _jobText.text   = JobStyle.GetLabel(job);

        RefreshAllStatTexts();

        FillSkills(job, _entry);
        RefreshEquipSlots();
        RefreshLevelUpDisplay();
        RefreshExpDisplay();
        RefreshSoldierUpDisplay();

        UnitPortraitHelper.Render(_entry.UnitName, job, _entry.Grade,
            _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
    }

    void FillSkills(UnitJob job, UnitEntry entry)
    {
        var activeDb  = ActiveSkillDatabase.Current;
        var passiveDb = PassiveSkillDatabase.Current;

        if (activeDb != null)
        {
            var activeId   = ActiveSkillRoller.Roll(entry.UnitName, job, activeDb, entry.Grade);
            var activeData = activeDb.Get(activeId);
            if (_activeSkillText     != null) _activeSkillText.text     = activeData?.SkillName   ?? "-";
            if (_activeSkillDescText != null) _activeSkillDescText.text = activeData?.Description ?? "";

            if (_activeSkillIcon != null)
            {
                var key = activeId.IconKey();
                var sm  = SpriteManager.Instance;
                var sp  = (key != null && sm != null) ? sm.Get(key) : null;
                _activeSkillIcon.sprite = sp;
                _activeSkillIcon.color  = sp != null ? Color.white : new Color(0.25f, 0.30f, 0.48f);
            }
        }

        var (s0, s1, s2) = PassiveSkillRoller.Roll(entry.UnitName);
        int slotCount    = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
        PassiveSkillType[]  passives  = { s0, s1, s2 };
        TextMeshProUGUI[]   nameTexts = { _passive0Text,     _passive1Text,     _passive2Text };
        TextMeshProUGUI[]   descTexts = { _passive0DescText, _passive1DescText, _passive2DescText };

        for (int i = 0; i < nameTexts.Length; i++)
        {
            bool show = i < slotCount && passiveDb != null;
            if (nameTexts[i] != null) nameTexts[i].gameObject.SetActive(show);
            if (descTexts[i] != null) descTexts[i].gameObject.SetActive(show);

            if (!show || nameTexts[i] == null) continue;
            var pd = passiveDb.Get(passives[i]);
            nameTexts[i].text = pd?.SkillName   ?? "-";
            if (descTexts[i] != null) descTexts[i].text = pd?.Description ?? "";
        }
    }

    // ── 레벨업 ────────────────────────────────────────────────

    void OnLevelUpClick()
    {
        if (_entry == null) return;
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        int cost = GetLevelUpCost(_entry.Level);
        if (!itemData.CanSpend(eItem.Gold, cost)) return;

        itemData.Spend(eItem.Gold, cost);
        unitData.SetUnitLevel(_entry.UnitName, _entry.Level + 1);
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        if (_entry != null) RefreshUI();
    }

    static int GetLevelUpCost(int currentLevel) => currentLevel * 100;

    // ── 용병 수 증가 ──────────────────────────────────────────

    void OnSoldierUpClick()
    {
        if (_entry == null) return;
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        int cost = GetSoldierUpCost(_entry.SoldierBonus);
        if (!itemData.CanSpend(eItem.SoldierShard, cost)) return;

        itemData.Spend(eItem.SoldierShard, cost);
        unitData.AddSoldierBonus(_entry.UnitName, 1);
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        if (_entry != null) RefreshUI();
    }

    static int GetSoldierUpCost(int currentBonus) => (currentBonus + 1) * 10;

    void RefreshSoldierUpDisplay()
    {
        if (_soldierUpCostText == null || _entry == null) return;
        int shards = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.SoldierShard) ?? 0;
        int cost   = GetSoldierUpCost(_entry.SoldierBonus);
        _soldierUpCostText.text  = $"{cost}";
        _soldierUpCostText.color = shards >= cost
            ? new Color(0.85f, 0.90f, 1.0f)
            : new Color(0.9f, 0.35f, 0.35f);
    }

    // ── 장비 슬롯 ─────────────────────────────────────────────

    void OnEquipSlotClick(int slot)
    {
        if (_entry == null) return;

        EquipComparePopup popup;
        if (PopupManager.Instance != null && PopupManager.Instance.IsOpen(PopupType.EquipCompare))
            popup = PopupManager.Instance.Get<EquipComparePopup>(PopupType.EquipCompare);
        else
            popup = PopupManager.Instance?.Open<EquipComparePopup>(PopupType.EquipCompare);

        if (popup == null) return;

        var entry = _entry;
        popup.Setup(entry, slot, () =>
        {
            _entry = UserDataManager.Instance?.Get<UnitData>()?.GetUnit(entry.UnitName);
            if (_entry != null) RefreshUI();
        });
    }

    void OnEnhanceClick(int slot)
    {
        if (_entry == null) return;
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (itemData == null || unitData == null) return;

        string id = (_entry.RunEquipSlots != null && slot < _entry.RunEquipSlots.Length)
                    ? _entry.RunEquipSlots[slot] : "";
        if (string.IsNullOrEmpty(id)) return;

        int enhance = (_entry.RunEquipEnhance != null && slot < _entry.RunEquipEnhance.Length)
                      ? _entry.RunEquipEnhance[slot] : 0;
        int cost = GetEnhanceCost(enhance);

        if (!itemData.CanSpend(eItem.EquipUpgradeStone, cost)) return;

        itemData.Spend(eItem.EquipUpgradeStone, cost);
        unitData.SetEquipment(_entry.UnitName, slot, id, enhance + 1);
        UserDataManager.Instance.RequestSave();

        _entry = unitData.GetUnit(_entry.UnitName);
        if (_entry != null) RefreshUI();
    }

    static int GetEnhanceCost(int currentEnhance) => (currentEnhance + 1) * 2;

    void RefreshEquipSlots()
    {
        var db = EquipmentDatabase.Current;
        RefreshEquipSlot(0, db, _equip0NameText, _equip0GradeBar, _equip0Icon, _equip0StatText, _equip0LockBadge, _equip0EnhanceBtn, _equip0EnhanceCostText);
        RefreshEquipSlot(1, db, _equip1NameText, _equip1GradeBar, _equip1Icon, _equip1StatText, _equip1LockBadge, _equip1EnhanceBtn, _equip1EnhanceCostText);
    }

    void RefreshEquipSlot(int slot, EquipmentDatabase db,
        TextMeshProUGUI nameText, Image gradeBar, Image iconImage,
        TextMeshProUGUI statText, GameObject lockBadge,
        Button enhanceBtn, TextMeshProUGUI enhanceCostText)
    {
        string id    = (_entry.RunEquipSlots != null && slot < _entry.RunEquipSlots.Length)
                       ? _entry.RunEquipSlots[slot] : "";
        var    equip = db?.Get(id);

        if (equip == null)
        {
            if (nameText   != null) nameText.text  = "없음";
            if (gradeBar   != null) gradeBar.color = new Color(0.25f, 0.25f, 0.30f);
            if (iconImage  != null) { iconImage.sprite = null; iconImage.color = new Color(0.18f, 0.18f, 0.22f); }
            if (statText   != null) statText.text  = "";
            if (lockBadge  != null) lockBadge.SetActive(false);
            if (enhanceBtn != null) enhanceBtn.gameObject.SetActive(false);
            return;
        }

        int enhance = (_entry.RunEquipEnhance != null && slot < _entry.RunEquipEnhance.Length)
                      ? _entry.RunEquipEnhance[slot] : 0;

        if (nameText != null)
            nameText.text = enhance > 0 ? $"{equip.EquipmentName} +{enhance}" : equip.EquipmentName;
        if (gradeBar  != null) gradeBar.color = GradeStyle.GetColor(equip.Grade);
        if (iconImage != null)
        {
            iconImage.sprite = equip.Icon;
            iconImage.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade);
        }

        if (statText != null && equip.StatEntries != null)
        {
            var sb  = new StringBuilder();
            var loc = LocalizationManager.Instance;
            foreach (var e in equip.StatEntries)
            {
                float val = equip.GetStatValue(e, enhance);
                sb.Append(loc.Get(e.Stat.ToString())).Append(" +");
                sb.AppendLine(StatDisplayHelper.FormatStat(e.Stat, val));
            }
            if (equip.TriggerType != EquipmentTrigger.None)
                sb.AppendLine(EquipmentData.FormatTriggerLine(equip,
                    loc.Get(equip.TriggerType.ToString()),
                    loc.Get(equip.TriggerStat.ToString())));
            statText.text = sb.ToString().TrimEnd();
        }

        if (lockBadge  != null) lockBadge.SetActive(true);
        if (enhanceBtn != null)
        {
            enhanceBtn.gameObject.SetActive(true);
            if (enhanceCostText != null)
            {
                int cost   = GetEnhanceCost(enhance);
                int stones = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.EquipUpgradeStone) ?? 0;
                enhanceCostText.text  = $"{cost}";
                enhanceCostText.color = stones >= cost
                    ? new Color(0.80f, 0.90f, 1.0f)
                    : new Color(0.9f, 0.35f, 0.35f);
            }
        }
    }

    void RefreshLevelUpDisplay()
    {
        if (_levelUpCostText == null || _entry == null) return;
        int cost = GetLevelUpCost(_entry.Level);
        int gold = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
        _levelUpCostText.text  = $"{cost:N0}";
        _levelUpCostText.color = gold >= cost
            ? new Color(1.0f, 0.85f, 0.20f)
            : new Color(0.9f, 0.35f, 0.35f);
    }

    void RefreshExpDisplay()
    {
        if (_entry == null) return;
        int expPerLevel = GameplayConfig.Current != null ? GameplayConfig.Current.ExpPerLevel : 100;
        int expNeeded   = _entry.Level * expPerLevel;
        if (_expText    != null) _expText.text = $"{_entry.Exp:N0} / {expNeeded:N0} EXP";
        if (_expBarFill != null)
            _expBarFill.rectTransform.anchorMax = new Vector2(
                expNeeded > 0 ? Mathf.Clamp01((float)_entry.Exp / expNeeded) : 0f, 1f);
    }

    // ── 닫기 ─────────────────────────────────────────────────

    void OnCloseClick()
    {
        var pm = PopupManager.Instance;
        if (pm != null && pm.IsOpen(PopupType.EquipCompare))
        {
            var ecp = pm.Get<EquipComparePopup>(PopupType.EquipCompare);
            if (ecp != null) ecp.Close();
        }
        Close();
    }

    // ── 스탯 클릭 분해 (HeroPanelUI 동일 패턴) ──────────────────

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
            (_cooldownText,     StatType.SkillCooldownReduce),
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

        float baseVal    = _statResult?.Base.Get(row.Type)   ?? 0f;
        float equipVal   = _statResult?.GetEquip(row.Type)   ?? 0f;
        float passiveVal = _statResult?.GetPassive(row.Type) ?? 0f;
        float abilityVal = _statResult?.GetAbility(row.Type) ?? 0f;
        float relicVal   = _statResult?.GetRelic(row.Type)   ?? 0f;
        float total      = baseVal + equipVal + passiveVal + abilityVal + relicVal;
        bool  expanded   = index == _expandedStatIndex;
        bool  hasBonus   = equipVal != 0f || passiveVal != 0f || abilityVal != 0f || relicVal != 0f;

        if (expanded && hasBonus)
        {
            row.ValueTmp.overflowMode     = TextOverflowModes.Overflow;
            row.ValueTmp.textWrappingMode = TextWrappingModes.NoWrap;
            row.ValueTmp.text = StatDisplayHelper.BuildBreakdown(row.Type, baseVal, equipVal, passiveVal, abilityVal, relicVal);
            if (row.LayoutEl != null) row.LayoutEl.preferredHeight = 52f;
        }
        else
        {
            row.ValueTmp.overflowMode     = TextOverflowModes.Ellipsis;
            row.ValueTmp.textWrappingMode = TextWrappingModes.NoWrap;
            row.ValueTmp.text = StatDisplayHelper.FormatStat(row.Type, total, isFinal: true);
            if (row.LayoutEl != null) row.LayoutEl.preferredHeight = 52f;
        }
    }
}
