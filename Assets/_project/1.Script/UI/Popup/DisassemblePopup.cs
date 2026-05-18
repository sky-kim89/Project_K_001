using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DisassemblePopup.cs
//  장비 분해 팝업.
//
//  오른쪽 그리드 → 보유 장비 114×114 아이콘 셀로 표시
//  왼쪽 InfoPanel → 선택 장비 상세 + 분해 확인 버튼 (보상 아이콘 포함)
//  BulkHeader     → 등급별 Toggle 체크박스 + 일괄 분해 버튼
// ============================================================

public class DisassemblePopup : PopupBase
{
    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    [Header("아이콘 그리드")]
    [SerializeField] Transform  _gridContent;
    [SerializeField] GameObject _iconCellTemplate;

    [Header("일괄 분해")]
    [SerializeField] Toggle[] _gradeToggles;       // 0=Normal … 4=Epic
    [SerializeField] Button   _bulkDisassembleBtn;

    [Header("선택 정보 패널")]
    [SerializeField] Image           _selectedIcon;
    [SerializeField] Image           _selectedGradeBorder;
    [SerializeField] TextMeshProUGUI _selectedNameText;
    [SerializeField] TextMeshProUGUI _selectedGradeText;
    [SerializeField] TextMeshProUGUI _selectedStatsText;
    [SerializeField] Image           _rewardIcon;
    [SerializeField] TextMeshProUGUI _rewardText;
    [SerializeField] Button          _disassembleBtn;

    readonly List<GameObject> _cells        = new();
    readonly List<string>     _cellEquipIds = new();
    readonly HashSet<string>  _bulkSelected = new();
    string _selectedEquipId;

    // ── 라이프사이클 ──────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        _disassembleBtn?.onClick.AddListener(() => ConfirmDisassemble());
        _bulkDisassembleBtn?.onClick.AddListener(() => BulkDisassemble());

        if (_gradeToggles != null)
            for (int i = 0; i < _gradeToggles.Length; i++)
            {
                int captured = i;
                _gradeToggles[i]?.onValueChanged.AddListener(v => OnGradeToggleChanged(captured, v));
            }
    }

    protected override void OnAfterOpen()
    {
        if (_gradeToggles != null)
            foreach (var t in _gradeToggles)
                if (t != null) t.isOn = false;
        _bulkSelected.Clear();
        BuildGrid();
        ClearSelection();
    }

    protected override void OnAfterClose()
    {
        ClearCells();
        _bulkSelected.Clear();
        _selectedEquipId = null;
    }

    // ── 그리드 구성 ───────────────────────────────────────────

    void BuildGrid()
    {
        ClearCells();

        var inv    = UserDataManager.Instance?.Get<EquipInventoryData>();
        var equipDb = EquipmentDatabase.Current;
        if (inv == null || equipDb == null) return;

        foreach (var id in inv.OwnedIds)
        {
            var equip = equipDb.Get(id);
            if (equip == null) continue;

            var cell = Instantiate(_iconCellTemplate, _gridContent);
            cell.SetActive(true);
            FillCell(cell, equip);
            _cells.Add(cell);
            _cellEquipIds.Add(id);
        }
        RefreshCellHighlights();
    }

    void FillCell(GameObject cell, EquipmentData equip)
    {
        var border = cell.transform.Find("GradeBorder")?.GetComponent<Image>();
        if (border != null) border.color = GradeStyle.GetColor(equip.Grade);

        var icon = cell.transform.Find("IconImage")?.GetComponent<Image>();
        if (icon != null)
        {
            icon.sprite = equip.Icon;
            icon.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade) * 0.6f;
        }

        var btn = cell.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            string capturedId = equip.EquipmentId;
            btn.onClick.AddListener(() => SelectEquip(capturedId));
        }
    }

    // ── 단일 선택 ─────────────────────────────────────────────

    void SelectEquip(string equipId)
    {
        _selectedEquipId = equipId;

        var equip = EquipmentDatabase.Current?.Get(equipId);
        if (equip == null) { ClearSelection(); return; }

        if (_selectedIcon != null)
        {
            _selectedIcon.sprite = equip.Icon;
            _selectedIcon.color  = equip.Icon != null ? Color.white : GradeStyle.GetColor(equip.Grade) * 0.6f;
        }
        if (_selectedGradeBorder != null)
            _selectedGradeBorder.color = GradeStyle.GetColor(equip.Grade);

        if (_selectedNameText != null) _selectedNameText.text = equip.EquipmentName;
        if (_selectedGradeText != null)
        {
            _selectedGradeText.text  = GradeStyle.GetLabel(equip.Grade);
            _selectedGradeText.color = GradeStyle.GetColor(equip.Grade);
        }

        if (_selectedStatsText != null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var entry in equip.StatEntries)
            {
                float  val  = equip.GetStatValue(entry, 0);
                string name = GetStatLabel(entry.Stat);
                sb.AppendLine($"{name}  +{EquipmentData.FormatStat(entry.Stat, val)}");
            }
            _selectedStatsText.text = sb.ToString().TrimEnd();
        }

        if (_rewardText != null) _rewardText.text = $"+{equip.ItemLevel}";
        if (_disassembleBtn != null) _disassembleBtn.interactable = true;

        RefreshCellHighlights();
    }

    void ClearSelection()
    {
        _selectedEquipId = null;
        if (_selectedNameText    != null) _selectedNameText.text    = "장비를 선택하세요";
        if (_selectedGradeText   != null) _selectedGradeText.text   = "";
        if (_selectedStatsText   != null) _selectedStatsText.text   = "";
        if (_rewardText          != null) _rewardText.text          = "";
        if (_selectedIcon        != null) { _selectedIcon.sprite = null; _selectedIcon.color = new Color(0.2f, 0.2f, 0.3f); }
        if (_selectedGradeBorder != null) _selectedGradeBorder.color = new Color(0.25f, 0.25f, 0.35f);
        if (_disassembleBtn      != null) _disassembleBtn.interactable = false;
        RefreshCellHighlights();
    }

    // ── 일괄 선택 ─────────────────────────────────────────────

    void OnGradeToggleChanged(int gradeIdx, bool isOn)
    {
        var grade   = (UnitGrade)gradeIdx;
        var inv     = UserDataManager.Instance?.Get<EquipInventoryData>();
        var equipDb = EquipmentDatabase.Current;
        if (inv == null || equipDb == null) return;

        foreach (var id in inv.OwnedIds)
        {
            var equip = equipDb.Get(id);
            if (equip == null || equip.Grade != grade) continue;
            if (isOn) _bulkSelected.Add(id);
            else      _bulkSelected.Remove(id);
        }
        RefreshCellHighlights();
        RefreshBulkBtn();
    }

    void RefreshBulkBtn()
    {
        if (_bulkDisassembleBtn != null)
            _bulkDisassembleBtn.interactable = _bulkSelected.Count > 0;
    }

    // ── 셀 하이라이트 ─────────────────────────────────────────

    void RefreshCellHighlights()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            if (i >= _cellEquipIds.Count) break;
            string id       = _cellEquipIds[i];
            bool   isSingle = id == _selectedEquipId;
            bool   isBulk   = _bulkSelected.Contains(id);

            // 셀 배경 (아이콘 뒤 — 단일 선택 시 약한 파란 틴트)
            var bg = _cells[i].GetComponent<Image>();
            if (bg != null)
                bg.color = isSingle
                    ? new Color(0.14f, 0.18f, 0.28f)
                    : new Color(0.12f, 0.12f, 0.20f);

            // GradeBorder: 아이콘보다 먼저 렌더 → 아이콘을 가리지 않음
            // 선택 시 해당 색상으로 덮어쓰고, 해제 시 등급 색상 복원
            var border = _cells[i].transform.Find("GradeBorder")?.GetComponent<Image>();
            if (border != null)
            {
                if (isBulk)
                    border.color = new Color(1.00f, 0.85f, 0.20f);   // 황금
                else if (isSingle)
                    border.color = new Color(0.60f, 0.85f, 1.00f);   // 밝은 청백
                else
                {
                    var equip = EquipmentDatabase.Current?.Get(id);
                    border.color = equip != null
                        ? GradeStyle.GetColor(equip.Grade)
                        : new Color(0.28f, 0.28f, 0.45f);
                }
            }

            // SelectionOutline: full-stretch 오버레이가 아이콘을 가리므로 항상 비활성
            _cells[i].transform.Find("SelectionOutline")?.gameObject.SetActive(false);
        }
    }

    // ── 분해 로직 ─────────────────────────────────────────────

    void ConfirmDisassemble()
    {
        if (string.IsNullOrEmpty(_selectedEquipId)) return;

        var invData  = UserDataManager.Instance?.Get<EquipInventoryData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        if (invData == null || itemData == null) return;

        var equip = EquipmentDatabase.Current?.Get(_selectedEquipId);
        if (equip == null) return;

        itemData.Add(eItem.EquipUpgradeStone, equip.ItemLevel);
        invData.Remove(_selectedEquipId);
        UserDataManager.Instance.RequestSave();

        _bulkSelected.Remove(_selectedEquipId);
        _selectedEquipId = null;
        BuildGrid();
        ClearSelection();
    }

    void BulkDisassemble()
    {
        if (_bulkSelected.Count == 0) return;

        var invData  = UserDataManager.Instance?.Get<EquipInventoryData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var equipDb  = EquipmentDatabase.Current;
        if (invData == null || itemData == null || equipDb == null) return;

        int totalStones = 0;
        foreach (var id in _bulkSelected)
        {
            var equip = equipDb.Get(id);
            if (equip == null) continue;
            totalStones += equip.ItemLevel;
            invData.Remove(id);
        }
        itemData.Add(eItem.EquipUpgradeStone, totalStones);
        UserDataManager.Instance.RequestSave();

        if (_gradeToggles != null)
            foreach (var t in _gradeToggles)
                if (t != null) t.isOn = false;
        _bulkSelected.Clear();
        _selectedEquipId = null;
        BuildGrid();
        ClearSelection();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    void ClearCells()
    {
        foreach (var c in _cells)
            if (c != null) Destroy(c);
        _cells.Clear();
        _cellEquipIds.Clear();
    }

    static string GetStatLabel(StatType stat) => stat switch
    {
        StatType.MaxHp               => "체력",
        StatType.Defense             => "방어율",
        StatType.Attack              => "공격력",
        StatType.AttackRange         => "사거리",
        StatType.AttackSpeed         => "공격속도",
        StatType.MoveSpeed           => "이동속도",
        StatType.CritChance          => "치명타 확률",
        StatType.CritDamage          => "치명타 배율",
        StatType.SoldierCount        => "병사 수",
        StatType.CommandPower        => "지휘력",
        StatType.SkillCooldownReduce => "쿨다운 감소",
        _                            => stat.ToString(),
    };
}
