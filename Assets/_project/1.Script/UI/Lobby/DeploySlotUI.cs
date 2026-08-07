using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DeploySlotUI.cs
//  BattlePanel 좌측 배치 슬롯 1칸.
//
//  빈 슬롯: EmptyGroup("빈 자리") 표시. 누를 수 없다.
//  점유 슬롯: OccupiedGroup 표시 (초상화·이름·직업·HP·ATK),
//            클릭 → onOccupied(entry, slotIndex) 콜백
//  잠긴 슬롯: 슬롯 자체를 비활성화 (안내 문구를 띄우지 않는다)
//
//  ⚠ 빈 슬롯을 눌러 용병을 고용하던 경로는 없앴다.
//    용병 고용은 런 상점(RunShopPopup)에서만 한다.
// ============================================================

public class DeploySlotUI : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] Button _button;

    [Header("빈 슬롯")]
    [SerializeField] GameObject _emptyGroup;

    [Header("점유 슬롯")]
    [SerializeField] GameObject           _occupiedGroup;
    [SerializeField] Image                _gradeBorder;
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImg;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _jobText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;
    [SerializeField] TextMeshProUGUI      _levelText;
    [SerializeField] Image                _gradeBadge;
    [SerializeField] TextMeshProUGUI      _gradeText;

    int                    _slotIndex;
    UnitEntry              _entry;
    Action<UnitEntry, int> _onOccupied;
    Texture2D              _portraitTexture;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(int slotIndex, bool locked, Action<UnitEntry, int> onOccupied)
    {
        _slotIndex  = slotIndex;
        _onOccupied = onOccupied;

        _button?.onClick.RemoveAllListeners();
        _button?.onClick.AddListener(OnClicked);

        // 잠긴 슬롯은 통째로 숨긴다 — 빈 칸도 남기지 않는다.
        gameObject.SetActive(!locked);
        if (locked) return;

        Refresh();
    }

    public void Refresh()
    {
        var deploy   = UserDataManager.Instance?.Get<DeploymentData>();
        var unitData = UserDataManager.Instance?.Get<UnitData>();

        string unitName = deploy?.GetUnitAt(_slotIndex) ?? "";
        _entry = string.IsNullOrEmpty(unitName) ? null : unitData?.GetUnit(unitName);

        bool occupied = _entry != null;
        if (_emptyGroup    != null) _emptyGroup   .SetActive(!occupied);
        if (_occupiedGroup != null) _occupiedGroup.SetActive(occupied);

        // 빈 자리는 누를 것이 없다 — 호버·클릭 반응까지 꺼서 버튼처럼 보이지 않게 한다
        if (_button != null) _button.interactable = occupied;

        if (occupied) FillOccupied(_entry);
    }

    // ── 내부 ─────────────────────────────────────────────────

    void FillOccupied(UnitEntry entry)
    {
        UnitJob        job    = UnitJobRoller.GetJob(entry.UnitName);
        HeroStatResult result = HeroStatResolver.Resolve(entry);
        Color          gc     = GradeStyle.GetColor(entry.Grade);

        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeBadge  != null) _gradeBadge.color  = gc;
        if (_nameText    != null) _nameText.text      = entry.UnitName;
        if (_levelText   != null) _levelText.text     = $"Lv.{entry.Level}";
        if (_gradeText   != null) { _gradeText.text   = GradeStyle.GetLabel(entry.Grade); _gradeText.color = Color.white; }
        if (_jobText     != null) _jobText.text        = JobStyle.GetLabel(job);
        if (_hpText      != null) _hpText.text         = $"{result.Total(StatType.MaxHp):N0}";
        if (_atkText     != null) _atkText.text        = $"{result.Total(StatType.Attack):N0}";
        if (_defText     != null) _defText.text        = $"{StatDisplayHelper.EffectiveDefensePct(result.Total(StatType.Defense)):F1}%";
        if (_soldierText != null) _soldierText.text    = $"{Mathf.RoundToInt(result.Total(StatType.SoldierCount))}명";

        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImg, ref _portraitTexture);
    }

    void OnClicked()
    {
        if (_entry != null) _onOccupied?.Invoke(_entry, _slotIndex);
    }
}
