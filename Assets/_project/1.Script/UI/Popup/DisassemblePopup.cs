using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DisassemblePopup.cs
//  장수·장비 분해 팝업.
//
//  장수 분해 → 용병조각(SoldierShard) 획득
//    보상: 등급 기준 + 레벨 소량 반영
//    Normal=5 / Uncommon=10 / Rare=20 / Unique=35 / Epic=60  +  Level/5
//
//  장비 분해 → 장비강화석(EquipUpgradeStone) 획득
//    보상: itemLevel + enhanceLevel
//
//  행 오브젝트는 프리팹 내 비활성 템플릿을 Instantiate 해서 사용.
// ============================================================

public class DisassemblePopup : PopupBase
{
    [Header("탭")]
    [SerializeField] Button[]     _tabBtns;
    [SerializeField] GameObject[] _tabPanels;

    [Header("목록 컨테이너")]
    [SerializeField] Transform _heroContent;
    [SerializeField] Transform _equipContent;

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    [Header("행 템플릿 (비활성 자식 오브젝트)")]
    [SerializeField] GameObject _heroRowTemplate;
    [SerializeField] GameObject _equipRowTemplate;

    readonly List<GameObject> _heroRows  = new();
    readonly List<GameObject> _equipRows = new();

    // ── 라이프사이클 ──────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        if (_tabBtns != null)
            for (int i = 0; i < _tabBtns.Length; i++)
            { int idx = i; _tabBtns[i]?.onClick.AddListener(() => SwitchTab(idx)); }
    }

    protected override void OnAfterOpen()
    {
        BuildHeroList();
        BuildEquipList();
        SwitchTab(0);
    }

    protected override void OnAfterClose()
    {
        ClearRows(_heroRows);
        ClearRows(_equipRows);
    }

    // ── 탭 ───────────────────────────────────────────────────

    void SwitchTab(int idx)
    {
        if (_tabPanels != null)
            for (int i = 0; i < _tabPanels.Length; i++)
                _tabPanels[i]?.SetActive(i == idx);

        if (_tabBtns != null)
            for (int i = 0; i < _tabBtns.Length; i++)
            {
                var tmp = _tabBtns[i]?.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp == null) continue;
                tmp.color = (i == idx)
                    ? new Color(0.40f, 0.72f, 1.00f)
                    : new Color(0.55f, 0.55f, 0.60f);
            }
    }

    // ── 장수 목록 ─────────────────────────────────────────────

    void BuildHeroList()
    {
        if (_heroContent == null || _heroRowTemplate == null) return;
        ClearRows(_heroRows);

        var units = UserDataManager.Instance?.Get<UnitData>()?.Units;
        if (units == null) return;

        var rowBgA = new Color(0.10f, 0.10f, 0.18f);
        var rowBgB = new Color(0.12f, 0.12f, 0.20f);

        for (int i = 0; i < units.Count; i++)
        {
            var row = Instantiate(_heroRowTemplate, _heroContent);
            row.SetActive(true);
            FillHeroRow(row, units[i], i % 2 == 0 ? rowBgA : rowBgB);
            _heroRows.Add(row);
        }
    }

    void FillHeroRow(GameObject row, UnitEntry entry, Color bgColor)
    {
        row.GetComponent<Image>().color = bgColor;

        row.transform.Find("GradeBar")
            .GetComponent<Image>().color = GradeStyle.GetColor(entry.Grade);

        var nameBlock = row.transform.Find("NameBlock");
        nameBlock.Find("NameTMP").GetComponent<TextMeshProUGUI>().text = entry.UnitName;

        var gradeTmp = nameBlock.Find("GradeTMP").GetComponent<TextMeshProUGUI>();
        gradeTmp.text  = GradeStyle.GetLabel(entry.Grade);
        gradeTmp.color = GradeStyle.GetColor(entry.Grade);

        row.transform.Find("LevelTMP").GetComponent<TextMeshProUGUI>().text
            = $"Lv.{entry.Level}";

        int shards = GetHeroShards(entry);
        row.transform.Find("RewardTMP").GetComponent<TextMeshProUGUI>().text
            = $"→ {shards}조각";

        var btn = row.transform.Find("DisBtn").GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        var captured = entry;
        btn.onClick.AddListener(() => DisassembleHero(captured));
    }

    // ── 장비 목록 ─────────────────────────────────────────────

    void BuildEquipList()
    {
        if (_equipContent == null || _equipRowTemplate == null) return;
        ClearRows(_equipRows);

        var units   = UserDataManager.Instance?.Get<UnitData>()?.Units;
        var equipDb = EquipmentDatabase.Current;
        if (units == null || equipDb == null) return;

        var rowBgA = new Color(0.10f, 0.10f, 0.18f);
        var rowBgB = new Color(0.12f, 0.12f, 0.20f);
        int rowIdx = 0;

        foreach (var entry in units)
        {
            for (int slot = 0; slot < 2; slot++)
            {
                string id = (entry.RunEquipSlots != null && slot < entry.RunEquipSlots.Length)
                            ? entry.RunEquipSlots[slot] : "";
                if (string.IsNullOrEmpty(id)) continue;

                var equip = equipDb.Get(id);
                if (equip == null) continue;

                int enhance = (entry.RunEquipEnhance != null && slot < entry.RunEquipEnhance.Length)
                              ? entry.RunEquipEnhance[slot] : 0;

                var row = Instantiate(_equipRowTemplate, _equipContent);
                row.SetActive(true);
                FillEquipRow(row, entry, slot, equip, enhance, rowIdx % 2 == 0 ? rowBgA : rowBgB);
                _equipRows.Add(row);
                rowIdx++;
            }
        }
    }

    void FillEquipRow(GameObject row, UnitEntry entry, int slot,
                      EquipmentData equip, int enhance, Color bgColor)
    {
        row.GetComponent<Image>().color = bgColor;

        row.transform.Find("GradeBar")
            .GetComponent<Image>().color = GradeStyle.GetColor(equip.Grade);

        var icon = row.transform.Find("IconBg/Icon").GetComponent<Image>();
        icon.sprite = equip.Icon;
        icon.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade);

        var nameBlock = row.transform.Find("NameBlock");
        string displayName = enhance > 0 ? $"{equip.EquipmentName} +{enhance}" : equip.EquipmentName;
        nameBlock.Find("NameTMP").GetComponent<TextMeshProUGUI>().text  = displayName;
        nameBlock.Find("OwnerTMP").GetComponent<TextMeshProUGUI>().text = entry.UnitName;

        int stones = GetEquipStones(equip, enhance);
        row.transform.Find("RewardTMP").GetComponent<TextMeshProUGUI>().text
            = $"→ {stones}석";

        var btn = row.transform.Find("DisBtn").GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        string capturedId    = equip.EquipmentId;
        var    capturedEntry = entry;
        int    capturedSlot  = slot;
        btn.onClick.AddListener(() => DisassembleEquip(capturedEntry, capturedSlot, capturedId, enhance));
    }

    // ── 분해 로직 ─────────────────────────────────────────────

    void DisassembleHero(UnitEntry entry)
    {
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        if (unitData == null || itemData == null) return;

        if (unitData.Units.Count <= 1)
        {
            Debug.Log("[DisassemblePopup] 마지막 장수는 분해할 수 없습니다.");
            return;
        }

        int shards = GetHeroShards(entry);
        unitData.RemoveUnit(entry.UnitName);
        itemData.Add(eItem.SoldierShard, shards);
        UserDataManager.Instance.RequestSave();

        BuildHeroList();
    }

    void DisassembleEquip(UnitEntry entry, int slot, string equipId, int enhance)
    {
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        if (unitData == null || itemData == null) return;

        var equip = EquipmentDatabase.Current?.Get(equipId);
        if (equip == null) return;

        int stones = GetEquipStones(equip, enhance);
        unitData.RemoveEquipment(entry.UnitName, slot);
        itemData.Add(eItem.EquipUpgradeStone, stones);
        UserDataManager.Instance.RequestSave();

        BuildEquipList();
    }

    // ── 보상 공식 ─────────────────────────────────────────────

    static int GetHeroShards(UnitEntry e)
    {
        int[] bases = { 5, 10, 20, 35, 60 };
        int gradeIdx = Mathf.Clamp((int)e.Grade, 0, bases.Length - 1);
        return bases[gradeIdx] + e.Level / 5;
    }

    static int GetEquipStones(EquipmentData equip, int enhance)
        => equip.ItemLevel + enhance;

    // ── 유틸 ─────────────────────────────────────────────────

    static void ClearRows(List<GameObject> rows)
    {
        foreach (var r in rows)
            if (r != null) Destroy(r);
        rows.Clear();
    }
}
