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

        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        var rowBgA = new Color(0.10f, 0.10f, 0.18f);
        var rowBgB = new Color(0.12f, 0.12f, 0.20f);
        int rowIdx = 0;

        for (int i = 0; i < units.Count; i++)
        {
            if (deployData != null && deployData.GetSlotOf(units[i].UnitName) >= 0) continue;

            var row = Instantiate(_heroRowTemplate, _heroContent);
            row.SetActive(true);
            FillHeroRow(row, units[i], rowIdx % 2 == 0 ? rowBgA : rowBgB);
            _heroRows.Add(row);
            rowIdx++;
        }
    }

    void FillHeroRow(GameObject row, UnitEntry entry, Color bgColor)
    {
        row.GetComponent<Image>().color = bgColor;
        row.transform.Find("GradeBar").GetComponent<Image>().color = GradeStyle.GetColor(entry.Grade);

        row.GetComponent<DisHeroRowUI>()?.Fill(entry);

        var btn = row.transform.Find("DisBtn").GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        var captured = entry;
        btn.onClick.AddListener(() => DisassembleHero(captured));
    }

    // ── 장비 목록 (인벤토리 기반) ─────────────────────────────

    void BuildEquipList()
    {
        if (_equipContent == null || _equipRowTemplate == null) return;
        ClearRows(_equipRows);

        var inv    = UserDataManager.Instance?.Get<EquipInventoryData>();
        var equipDb = EquipmentDatabase.Current;
        if (inv == null || equipDb == null) return;

        var rowBgA = new Color(0.10f, 0.10f, 0.18f);
        var rowBgB = new Color(0.12f, 0.12f, 0.20f);
        int rowIdx = 0;

        foreach (var id in inv.OwnedIds)
        {
            var equip = equipDb.Get(id);
            if (equip == null) continue;

            var row = Instantiate(_equipRowTemplate, _equipContent);
            row.SetActive(true);
            FillEquipRow(row, equip, rowIdx % 2 == 0 ? rowBgA : rowBgB);
            _equipRows.Add(row);
            rowIdx++;
        }
    }

    void FillEquipRow(GameObject row, EquipmentData equip, Color bgColor)
    {
        row.GetComponent<Image>().color = bgColor;
        row.transform.Find("GradeBar").GetComponent<Image>().color = GradeStyle.GetColor(equip.Grade);

        row.GetComponent<DisEquipRowUI>()?.Fill(equip);

        var btn = row.transform.Find("DisBtn").GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        string capturedId = equip.EquipmentId;
        btn.onClick.AddListener(() => DisassembleEquip(capturedId));
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

    void DisassembleEquip(string equipId)
    {
        var invData  = UserDataManager.Instance?.Get<EquipInventoryData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        if (invData == null || itemData == null) return;

        var equip = EquipmentDatabase.Current?.Get(equipId);
        if (equip == null) return;

        int stones = equip.ItemLevel;
        invData.Remove(equipId);
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


    // ── 유틸 ─────────────────────────────────────────────────

    static void ClearRows(List<GameObject> rows)
    {
        foreach (var r in rows)
            if (r != null) Destroy(r);
        rows.Clear();
    }
}
