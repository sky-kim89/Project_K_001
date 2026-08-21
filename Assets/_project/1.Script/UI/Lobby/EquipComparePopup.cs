using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EquipComparePopup.cs
//  장비 비교·교체 팝업.
//
//  흐름:
//    HeroDetailPopup 의 장비 칸 클릭
//      → PopupManager.Open<EquipComparePopup>(PopupType.EquipCompare)
//      → popup.Setup(entry, slot, onEquipped)
//    팝업 내:
//      아래 격자에서 장비를 고르면 위쪽 [현재 장착 | 선택 장비] 카드가 갱신되고,
//      선택 카드 쪽에는 현재 대비 증감(+/-)이 색으로 함께 표시된다.
//      [장 착] → 교체 후 onEquipped 콜백 → 닫기
//
//  ■ "비교" 팝업의 핵심은 증감이다
//    예전에는 두 장비의 스탯을 나란히 적어 두기만 해서, 어느 쪽이 나은지
//    플레이어가 직접 뺄셈을 해야 했다. 지금은 선택 카드에 델타를 적는다.
//    ⚠ ▲▼ 글리프는 기본 폰트에 없어 □ 로 렌더된다 — 부호(+/-)와 색으로만 구분한다.
// ============================================================

public class EquipComparePopup : PopupBase
{
    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _titleText;
    [SerializeField] Button          _closeBtn;

    [Header("현재 장착 카드 (좌측)")]
    [SerializeField] Image           _curGradeBar;
    [SerializeField] Image           _curIcon;
    [SerializeField] TextMeshProUGUI _curName;
    [SerializeField] TextMeshProUGUI _curGrade;
    [SerializeField] TextMeshProUGUI _curStat;
    [SerializeField] GameObject      _curBindBadge;
    [SerializeField] GameObject      _curEmptyMark;

    [Header("선택 장비 카드 (우측)")]
    [SerializeField] Image           _selGradeBar;
    [SerializeField] Image           _selIcon;
    [SerializeField] TextMeshProUGUI _selName;
    [SerializeField] TextMeshProUGUI _selGrade;
    [SerializeField] TextMeshProUGUI _selStat;
    [SerializeField] GameObject      _selEmptyMark;

    [Header("장착 / 분해")]
    [SerializeField] Button          _equipBtn;
    [SerializeField] TextMeshProUGUI _equipBtnLabel;
    [SerializeField] Button          _disassembleBtn;
    [SerializeField] TextMeshProUGUI _disassembleGainText;
    [SerializeField] TextMeshProUGUI _warningText;

    [Header("인벤토리 격자")]
    [SerializeField] Transform       _listContent;
    [SerializeField] EquipPickSlotUI _pickTemplate;
    [SerializeField] TextMeshProUGUI _emptyText;

    // 증감 색 — 초록(상승) / 빨강(하락). 리치텍스트로 끼워 넣는다.
    const string UpHex   = "55EE88";
    const string DownHex = "EE7766";

    static readonly Color EmptyName = new(0.45f, 0.47f, 0.58f);

    UnitEntry     _entry;
    int           _slot;
    Action        _onEquipped;
    EquipmentData _selectedEquip;
    int           _selectedIndex = -1;

    readonly List<EquipPickSlotUI> _slots = new();

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>팝업 열기 직후 호출. Open&lt;T&gt;() 반환값에서 바로 Setup().</summary>
    public void Setup(UnitEntry entry, int slot, Action onEquipped)
    {
        _entry      = entry;
        _slot       = slot;
        _onEquipped = onEquipped;

        _titleText.text = $"슬롯 {slot + 1} 교체";

        _selectedEquip = null;
        _selectedIndex = -1;

        RefreshCurrentCard();
        RefreshSelectedCard();
        BuildInventoryList();
    }

    // ── 라이프사이클 ──────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _equipBtn.onClick.AddListener(OnEquipClick);
        _disassembleBtn.onClick.AddListener(OnDisassembleClick);
        _closeBtn.onClick.AddListener(() => Close());
        _pickTemplate.gameObject.SetActive(false);
    }

    protected override void OnAfterClose()
    {
        ClearList();
        _selectedEquip = null;
        _entry         = null;
        _onEquipped    = null;
    }

    // ── 카드 ─────────────────────────────────────────────────

    EquipmentData CurrentEquip()
    {
        string id = _entry?.RunEquipSlots != null && _slot < _entry.RunEquipSlots.Length
                    ? _entry.RunEquipSlots[_slot] : "";
        return EquipmentDatabase.Current.Get(id);
    }

    int CurrentEnhance()
        => _entry?.RunEquipEnhance != null && _slot < _entry.RunEquipEnhance.Length
            ? _entry.RunEquipEnhance[_slot] : 0;

    void RefreshCurrentCard()
    {
        var equip   = CurrentEquip();
        int enhance = CurrentEnhance();

        FillCard(equip, enhance, _curGradeBar, _curIcon, _curName, _curGrade,
                 _curStat, _curEmptyMark, BuildStatText(equip, enhance));

        _curBindBadge.SetActive(equip != null);
        // 경고는 "덮어써서 사라질 게 있을 때"만 띄운다 — 빈 슬롯이면 잃을 것이 없다.
        _warningText.gameObject.SetActive(equip != null);
    }

    void RefreshSelectedCard()
    {
        FillCard(_selectedEquip, 0, _selGradeBar, _selIcon, _selName, _selGrade,
                 _selStat, _selEmptyMark,
                 BuildCompareText(_selectedEquip, CurrentEquip(), CurrentEnhance()));

        bool ready = _selectedEquip != null;
        _equipBtn.interactable = ready;
        _equipBtnLabel.text    = ready ? "장  착" : "장비를 고르세요";

        // 분해도 "고른 장비" 를 대상으로 한다 — 장착 중인 장비는 귀속이라 분해할 수 없다.
        _disassembleBtn.interactable = ready;
        _disassembleGainText.text    = ready ? $"+{DisassembleGain(_selectedEquip)}" : "-";
    }

    /// <summary>분해 보상 — DisassemblePopup 과 같은 규칙(아이템 레벨만큼 강화석).</summary>
    static int DisassembleGain(EquipmentData equip) => equip.ItemLevel;

    static void FillCard(EquipmentData equip, int enhance,
        Image gradeBar, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI gradeText,
        TextMeshProUGUI statText, GameObject emptyMark, string statBody)
    {
        if (equip == null)
        {
            nameText.text  = "없음";
            nameText.color = EmptyName;
            gradeText.text = "";
            statText.text  = "";
            gradeBar.color = new Color(0.24f, 0.25f, 0.34f);
            icon.enabled   = false;
            emptyMark.SetActive(true);
            return;
        }

        var gc = GradeStyle.GetColor(equip.Grade);

        nameText.text  = enhance > 0 ? $"{equip.EquipmentName} +{enhance}" : equip.EquipmentName;
        nameText.color = Color.white;
        gradeText.text  = GradeStyle.GetLabel(equip.Grade);
        gradeText.color = gc;
        gradeBar.color  = gc;

        icon.enabled = true;
        icon.sprite  = equip.Icon;
        icon.color   = equip.Icon != null ? Color.white : gc;

        emptyMark.SetActive(false);
        statText.text = statBody;
    }

    // ── 스탯 문구 ─────────────────────────────────────────────

    static string BuildStatText(EquipmentData equip, int enhance)
    {
        if (equip == null) return "";

        var loc = LocalizationManager.Instance;
        var sb  = new StringBuilder();

        foreach (var e in equip.StatEntries)
        {
            if (sb.Length > 0) sb.Append('\n');
            // 스탯 창의 '장비' 색과 같은 파랑 — 색만 보고 출처를 알 수 있게 한다
            sb.Append(StatBonusColors.Wrap(StatSource.Equip,
                $"{loc.Get(e.Stat.ToString())} +{StatDisplayHelper.FormatStat(e.Stat, equip.GetStatValue(e, enhance))}"));
        }
        AppendTrigger(sb, equip, loc);
        return sb.ToString();
    }

    /// <summary>선택 장비 스탯 + 현재 장비 대비 증감. 증감이 0이면 적지 않는다.</summary>
    static string BuildCompareText(EquipmentData sel, EquipmentData cur, int curEnhance)
    {
        if (sel == null) return "";

        var loc     = LocalizationManager.Instance;
        var curVals = CollectStats(cur, curEnhance);
        var selVals = CollectStats(sel, 0);
        var sb      = new StringBuilder();

        // 선택 장비가 가진 스탯 먼저
        foreach (var kv in selVals)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(StatBonusColors.Wrap(StatSource.Equip,
                $"{loc.Get(kv.Key.ToString())} +{StatDisplayHelper.FormatStat(kv.Key, kv.Value)}"));

            curVals.TryGetValue(kv.Key, out float before);
            AppendDelta(sb, kv.Key, kv.Value - before);
        }

        // 현재 장비에만 있던 스탯 = 교체하면 통째로 잃는다
        foreach (var kv in curVals)
        {
            if (selVals.ContainsKey(kv.Key)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(StatBonusColors.Wrap(StatSource.Equip, $"{loc.Get(kv.Key.ToString())} +0"));
            AppendDelta(sb, kv.Key, -kv.Value);
        }

        AppendTrigger(sb, sel, loc);
        return sb.ToString();
    }

    static Dictionary<StatType, float> CollectStats(EquipmentData equip, int enhance)
    {
        var map = new Dictionary<StatType, float>();
        if (equip == null) return map;

        foreach (var e in equip.StatEntries)
        {
            map.TryGetValue(e.Stat, out float acc);
            map[e.Stat] = acc + equip.GetStatValue(e, enhance);
        }
        return map;
    }

    // ▲▼ 는 폰트에 없다 — 부호와 색으로만 방향을 보여 준다.
    static void AppendDelta(StringBuilder sb, StatType stat, float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;

        string hex  = delta > 0f ? UpHex : DownHex;
        string sign = delta > 0f ? "+" : "-";
        sb.Append($"  <color=#{hex}>({sign}{StatDisplayHelper.FormatStat(stat, Mathf.Abs(delta))})</color>");
    }

    static void AppendTrigger(StringBuilder sb, EquipmentData equip, LocalizationManager loc)
    {
        if (equip.TriggerType == EquipmentTrigger.None) return;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append(EquipmentData.FormatTriggerLine(equip,
            loc.Get(equip.TriggerType.ToString()),
            loc.Get(equip.TriggerStat.ToString())));
    }

    // ── 인벤토리 격자 ─────────────────────────────────────────

    void BuildInventoryList()
    {
        ClearList();

        var inv = UserDataManager.Instance.Get<EquipInventoryData>();
        var db  = EquipmentDatabase.Current;

        foreach (var id in inv.OwnedIds)
        {
            var equip = db.Get(id);
            if (equip == null) continue;

            var slot  = Instantiate(_pickTemplate, _listContent);
            slot.gameObject.SetActive(true);

            int index = _slots.Count;
            slot.Setup(equip, () => OnPick(equip, index));
            _slots.Add(slot);
        }

        // 빈 인벤토리에 아무 안내도 없으면 "고장난 화면"으로 읽힌다.
        _emptyText.gameObject.SetActive(_slots.Count == 0);
    }

    void ClearList()
    {
        foreach (var s in _slots)
            if (s != null) Destroy(s.gameObject);
        _slots.Clear();
        _selectedIndex = -1;
    }

    void OnPick(EquipmentData equip, int index)
    {
        // 고른 칸을 격자에서도 표시한다 — 위쪽 카드만 바뀌면 어디를 골랐는지 모른다.
        if (_selectedIndex >= 0 && _selectedIndex < _slots.Count)
            _slots[_selectedIndex].SetSelected(false);

        _selectedIndex = index;
        _slots[index].SetSelected(true);

        _selectedEquip = equip;
        RefreshSelectedCard();
    }

    // ── 장착 ─────────────────────────────────────────────────

    void OnEquipClick()
    {
        if (_entry == null || _slot < 0 || _selectedEquip == null) return;

        var unitData = UserDataManager.Instance.Get<UnitData>();
        var invData  = UserDataManager.Instance.Get<EquipInventoryData>();

        invData.Remove(_selectedEquip.EquipmentId);
        unitData.SetEquipment(_entry.UnitName, _slot, _selectedEquip.EquipmentId, 0);
        UserDataManager.Instance.RequestSave();

        _onEquipped?.Invoke();
        Close();
    }

    // ── 분해 ─────────────────────────────────────────────────
    //  DisassemblePopup 을 열지 않아도 여기서 바로 정리할 수 있게 한다.
    //  창을 닫지 않는 이유: 여러 개를 연달아 분해하는 흐름이 자연스럽다.

    void OnDisassembleClick()
    {
        if (_selectedEquip == null) return;

        var inv   = UserDataManager.Instance.Get<EquipInventoryData>();
        var items = UserDataManager.Instance.Get<ItemData>();

        items.Add(eItem.EquipUpgradeStone, DisassembleGain(_selectedEquip));
        inv.Remove(_selectedEquip.EquipmentId);
        UserDataManager.Instance.RequestSave();

        _selectedEquip = null;
        RefreshSelectedCard();
        BuildInventoryList();
    }
}
