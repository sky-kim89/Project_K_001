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

    /// <summary>
    /// 선택한 **칸 번호** (-1 = 없음).
    ///
    /// ⚠ 장비 ID 로 잡지 말 것
    ///   인벤토리는 같은 ID 를 여러 개 들고 있다(똑같은 장비 3개 = 같은 문자열 3줄).
    ///   ID 로 선택을 표시하면 하나를 눌러도 같은 장비 칸이 **전부** 테두리를 두른다.
    ///   칸 번호는 _cells / _cellEquipIds 와 같은 순서라 중복이 없다.
    /// </summary>
    int _selectedCell = -1;

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
        RefreshBulkBtn();
        ClearSelection();
    }

    protected override void OnAfterClose()
    {
        ClearCells();
        _bulkSelected.Clear();
        _selectedCell = -1;
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
            FillCell(cell, equip, _cells.Count);
            _cells.Add(cell);
            _cellEquipIds.Add(id);
        }
        RefreshCellHighlights();
    }

    void FillCell(GameObject cell, EquipmentData equip, int cellIndex)
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
            int captured = cellIndex;   // ID 가 아니라 칸 번호 — 중복 장비를 구분하는 유일한 키
            btn.onClick.AddListener(() => SelectCell(captured));
        }
    }

    // ── 단일 선택 ─────────────────────────────────────────────

    void SelectCell(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= _cellEquipIds.Count) { ClearSelection(); return; }

        _selectedCell = cellIndex;

        var equip = EquipmentDatabase.Current?.Get(_cellEquipIds[cellIndex]);
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
                string name = LocalizationManager.Instance.Get(entry.Stat.ToString());
                sb.AppendLine(StatBonusColors.Wrap(StatSource.Equip,
                    $"{name}  +{StatDisplayHelper.FormatStat(entry.Stat, val)}"));
            }
            _selectedStatsText.text = sb.ToString().TrimEnd();
        }

        if (_rewardText != null) _rewardText.text = $"+{equip.ItemLevel}";
        if (_disassembleBtn != null) _disassembleBtn.interactable = true;

        RefreshCellHighlights();
    }

    void ClearSelection()
    {
        _selectedCell = -1;
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
        RebuildBulkSelection();
        RefreshCellHighlights();
        RefreshBulkBtn();
    }

    /// <summary>
    /// 켜져 있는 등급 토글로부터 일괄 선택 목록을 다시 만든다.
    ///
    /// ⚠ 토글 하나가 바뀔 때 그 등급만 넣고 빼면 안 된다
    ///   분해로 장비가 사라진 뒤에도 목록에 ID 가 남고, 반대로 같은 ID 를 여럿
    ///   들고 있을 때 하나만 분해하면 나머지까지 선택이 풀렸다.
    ///   목록의 정본은 언제나 "지금 켜진 토글 + 지금 보유한 장비" 다.
    /// </summary>
    void RebuildBulkSelection()
    {
        _bulkSelected.Clear();

        var inv     = UserDataManager.Instance?.Get<EquipInventoryData>();
        var equipDb = EquipmentDatabase.Current;
        if (inv == null || equipDb == null || _gradeToggles == null) return;

        foreach (var id in inv.OwnedIds)
        {
            var equip = equipDb.Get(id);
            if (equip == null) continue;

            int gi = (int)equip.Grade;
            if (gi < 0 || gi >= _gradeToggles.Length) continue;
            if (_gradeToggles[gi] != null && _gradeToggles[gi].isOn) _bulkSelected.Add(id);
        }
    }

    void RefreshBulkBtn()
    {
        if (_bulkDisassembleBtn != null)
            _bulkDisassembleBtn.interactable = _bulkSelected.Count > 0;
    }

    // ── 셀 하이라이트 ─────────────────────────────────────────

    /// <summary>
    /// 칸의 상태 표시. 세 가지가 서로 자리를 뺏지 않게 층을 나눠 쓴다.
    ///
    ///   등급          → GradeBorder (항상 등급색. 선택했다고 덮어쓰면 등급을 못 읽는다)
    ///   단일 선택     → SelectionOutline (칸 바깥으로 8px 나가는 금색 링)
    ///   일괄 선택     → Inner 를 호박색으로 (링과 색·자리가 겹치지 않는다)
    ///
    /// ⚠ 예전엔 셋 다 GradeBorder 색 하나로 표현했다
    ///   일괄이 단일을 덮고, 둘 다 등급색을 덮어써서 무엇이 왜 켜졌는지 알 수 없었다.
    /// </summary>
    void RefreshCellHighlights()
    {
        var normalInner = new Color(0.13f, 0.135f, 0.21f);
        var bulkInner   = new Color(0.34f, 0.23f, 0.09f);

        for (int i = 0; i < _cells.Count; i++)
        {
            if (i >= _cellEquipIds.Count) break;
            string id       = _cellEquipIds[i];
            bool   isSingle = i == _selectedCell;
            bool   isBulk   = _bulkSelected.Contains(id);

            var border = _cells[i].transform.Find("GradeBorder")?.GetComponent<Image>();
            if (border != null)
            {
                var equip = EquipmentDatabase.Current?.Get(id);
                border.color = equip != null
                    ? GradeStyle.GetColor(equip.Grade)
                    : new Color(0.28f, 0.28f, 0.45f);
            }

            var inner = _cells[i].transform.Find("Inner")?.GetComponent<Image>();
            if (inner != null) inner.color = isBulk ? bulkInner : normalInner;

            _cells[i].transform.Find("SelectionOutline")?.gameObject.SetActive(isSingle);
        }
    }

    // ── 분해 로직 ─────────────────────────────────────────────

    void ConfirmDisassemble()
    {
        if (_selectedCell < 0 || _selectedCell >= _cellEquipIds.Count) return;

        var invData  = UserDataManager.Instance?.Get<EquipInventoryData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        if (invData == null || itemData == null) return;

        string id    = _cellEquipIds[_selectedCell];
        var    equip = EquipmentDatabase.Current?.Get(id);
        if (equip == null) return;

        itemData.Add(eItem.EquipUpgradeStone, equip.ItemLevel);
        // 같은 ID 가 여러 개면 그중 하나만 빠진다 — 사본끼리는 완전히 같은 장비라 어느 것이든 같다.
        invData.Remove(id);
        UserDataManager.Instance.RequestSave();

        _selectedCell = -1;
        BuildGrid();
        RebuildBulkSelection();   // 방금 사라진 장비를 목록에서 걷어낸다
        RefreshBulkBtn();
        ClearSelection();
    }

    void BulkDisassemble()
    {
        if (_bulkSelected.Count == 0) return;

        var invData  = UserDataManager.Instance?.Get<EquipInventoryData>();
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        var equipDb  = EquipmentDatabase.Current;
        if (invData == null || itemData == null || equipDb == null) return;

        // OwnedIds(List)에서 선택 등급 ID를 중복 포함해 모두 수집
        // _bulkSelected(HashSet)으로 순회하면 같은 ID가 여러 개 있을 때 1개만 제거됨
        var toRemove = new List<string>();
        foreach (var id in invData.OwnedIds)
        {
            if (_bulkSelected.Contains(id))
                toRemove.Add(id);
        }

        int totalStones = 0;
        foreach (var id in toRemove)
        {
            var equip = equipDb.Get(id);
            if (equip != null)
                totalStones += equip.ItemLevel;
            invData.Remove(id);
        }
        itemData.Add(eItem.EquipUpgradeStone, totalStones);
        UserDataManager.Instance.RequestSave();

        if (_gradeToggles != null)
            foreach (var t in _gradeToggles)
                if (t != null) t.isOn = false;
        _bulkSelected.Clear();
        _selectedCell = -1;
        BuildGrid();
        RefreshBulkBtn();
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

}
