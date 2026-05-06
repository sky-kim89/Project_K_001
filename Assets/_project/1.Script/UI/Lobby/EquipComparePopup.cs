using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  EquipComparePopup.cs
//  장비 비교·교체 전용 팝업.
//
//  흐름:
//    HeroPanelUI 에서 슬롯 클릭
//      → PopupManager.Open<EquipComparePopup>(PopupType.EquipCompare)
//      → popup.Setup(entry, slot, onEquipped)
//    팝업 내:
//      좌측 = 현재 장착 장비, 우측 = 인벤토리에서 선택한 장비
//      [장착] 버튼 → 교체 후 onEquipped 콜백 → 팝업 닫기
// ============================================================

public class EquipComparePopup : PopupBase
{
    // ── 헤더 ──────────────────────────────────────────────────
    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _titleText;

    // ── 현재 장착 카드 (좌측) ─────────────────────────────────
    [Header("현재 장착 카드")]
    [SerializeField] Image           _curGradeBar;
    [SerializeField] Image           _curIcon;
    [SerializeField] TextMeshProUGUI _curName;
    [SerializeField] TextMeshProUGUI _curStat;
    [SerializeField] GameObject      _curBindBadge;

    // ── 선택 장비 카드 (우측) ─────────────────────────────────
    [Header("선택 장비 카드")]
    [SerializeField] Image           _selGradeBar;
    [SerializeField] Image           _selIcon;
    [SerializeField] TextMeshProUGUI _selName;
    [SerializeField] TextMeshProUGUI _selStat;

    // ── 장착 버튼 / 경고 ──────────────────────────────────────
    [Header("장착 버튼")]
    [SerializeField] Button          _equipBtn;
    [SerializeField] TextMeshProUGUI _warningText;

    // ── 인벤토리 목록 ─────────────────────────────────────────
    [Header("인벤토리 목록")]
    [SerializeField] Transform       _listContent;

    // ── 런타임 ────────────────────────────────────────────────
    UnitEntry     _entry;
    int           _slot;
    Action        _onEquipped;
    EquipmentData _selectedEquip;
    readonly List<GameObject> _cards = new();

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>팝업 열기 직후 호출. Open<T>() 반환값에서 바로 Setup() 호출.</summary>
    public void Setup(UnitEntry entry, int slot, Action onEquipped)
    {
        _entry      = entry;
        _slot       = slot;
        _onEquipped = onEquipped;

        if (_titleText != null)
            _titleText.text = $"슬롯 {slot + 1} 장비 교체";

        _selectedEquip = null;
        RefreshCurrentSlot();
        RefreshSelectedSlot();
        BuildInventoryList();
    }

    // ── 라이프사이클 ──────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _equipBtn?.onClick.AddListener(OnEquipClick);
    }

    protected override void OnAfterClose()
    {
        ClearList();
        _selectedEquip = null;
        _entry         = null;
        _onEquipped    = null;
    }

    // ── 내부 ──────────────────────────────────────────────────

    void RefreshCurrentSlot()
    {
        var db    = EquipmentDatabase.Current;
        string id = (_entry?.RunEquipSlots != null && _slot < _entry.RunEquipSlots.Length)
                    ? _entry.RunEquipSlots[_slot] : "";
        var equip = db?.Get(id);

        int enhance = (_entry?.RunEquipEnhance != null && _slot < _entry.RunEquipEnhance.Length)
                      ? _entry.RunEquipEnhance[_slot] : 0;

        FillCard(equip, enhance, _curGradeBar, _curIcon, _curName, _curStat);
        if (_curBindBadge != null) _curBindBadge.SetActive(equip != null);
        if (_warningText  != null) _warningText.gameObject.SetActive(equip != null);
    }

    void RefreshSelectedSlot()
    {
        FillCard(_selectedEquip, 0, _selGradeBar, _selIcon, _selName, _selStat);
        if (_equipBtn != null) _equipBtn.interactable = _selectedEquip != null;
    }

    void FillCard(EquipmentData equip, int enhance,
        Image gradeBar, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI statText)
    {
        if (equip == null)
        {
            if (nameText != null) nameText.text = "없음";
            if (statText != null) statText.text = "";
            if (gradeBar != null) gradeBar.color = new Color(0.25f, 0.25f, 0.30f);
            if (icon     != null) { icon.sprite = null; icon.color = new Color(0.18f, 0.18f, 0.22f); }
            return;
        }

        if (nameText != null)
            nameText.text = enhance > 0 ? $"{equip.EquipmentName} +{enhance}" : equip.EquipmentName;

        if (gradeBar != null) gradeBar.color = GradeStyle.GetColor(equip.Grade);

        if (icon != null)
        {
            icon.sprite = equip.Icon;
            icon.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade);
        }

        if (statText != null && equip.StatEntries != null)
        {
            statText.overflowMode = TMPro.TextOverflowModes.Overflow;
            var sb  = new System.Text.StringBuilder();
            var loc = LocalizationManager.Instance;
            foreach (var e in equip.StatEntries)
            {
                float val = equip.GetStatValue(e, enhance);
                sb.Append(loc.Get(e.Stat.ToString())).Append(" +");
                sb.AppendLine(FormatEquipStat(e.Stat, val));
            }
            if (equip.TriggerType != EquipmentTrigger.None)
                sb.AppendLine($"{loc.Get(equip.TriggerType.ToString())}: {loc.Get(equip.TriggerStat.ToString())}");
            statText.text = sb.ToString().TrimEnd();
        }
    }

    static string FormatEquipStat(StatType stat, float val) => stat switch
    {
        StatType.Defense      => $"{val * 100f:F1}%",
        StatType.AttackSpeed  => $"{val:F2}",
        StatType.MoveSpeed    => $"{val:F1}",
        StatType.AttackRange  => $"{val:F1}",
        StatType.SoldierCount => $"{Mathf.RoundToInt(val)}",
        _                     => $"{val:N0}",
    };

    void BuildInventoryList()
    {
        ClearList();
        if (_listContent == null) return;

        var inv = UserDataManager.Instance?.Get<EquipInventoryData>();
        var db  = EquipmentDatabase.Current;
        if (inv == null || db == null) return;

        foreach (var id in inv.OwnedIds)
        {
            var equip = db.Get(id);
            if (equip == null) continue;

            var go = new GameObject("EquipIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_listContent, false);

            var img = go.GetComponent<Image>();
            img.sprite         = equip.Icon;
            img.color          = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade);
            img.preserveAspect = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var captured = equip;
            btn.onClick.AddListener(() => OnItemClick(captured));

            _cards.Add(go);
        }
    }

    void ClearList()
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c);
        _cards.Clear();
    }

    void OnItemClick(EquipmentData equip)
    {
        _selectedEquip = equip;
        RefreshSelectedSlot();
    }

    void OnEquipClick()
    {
        if (_entry == null || _slot < 0 || _selectedEquip == null) return;

        var unitData = UserDataManager.Instance?.Get<UnitData>();
        var invData  = UserDataManager.Instance?.Get<EquipInventoryData>();
        if (unitData == null || invData == null) return;

        invData.Remove(_selectedEquip.EquipmentId);
        unitData.SetEquipment(_entry.UnitName, _slot, _selectedEquip.EquipmentId, 0);
        UserDataManager.Instance.RequestSave();

        _onEquipped?.Invoke();
        Close();
    }

}
