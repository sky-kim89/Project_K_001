using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  GeneralCandidateCardUI.cs
//  MainPanel에서 장수 후보 1명을 표시하는 카드.
//  카드 배경 Button 클릭 → 선택 / 자세히 보기 Button → HeroDetailPopup
// ============================================================

public class GeneralCandidateCardUI : MonoBehaviour
{
    [SerializeField] Image                _selectionOverlay; // 선택 시 밝아지는 오버레이
    [SerializeField] Image                _gradeBorder;      // 좌측 등급 색 바
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _jobChipText;    // 초상화 좌상단 배지
    [SerializeField] TextMeshProUGUI      _gradeChipText;  // 초상화 우상단 배지
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;
    [SerializeField] Button               _selectBtn;
    [SerializeField] Button               _detailBtn;

    [Header("특성")]
    [SerializeField] TraitIconUI _traitIconUI;

    [Header("스킬")]
    [SerializeField] SkillIconUI   _activeSkillIcon;
    [SerializeField] SkillIconUI[] _passiveSkillIcons;   // 3칸 — 등급에 따라 1~3칸만 열린다
    [SerializeField] TextMeshProUGUI _passiveSlotText;   // "패시브 2/3"

    UnitEntry         _entry;
    Texture2D         _portraitTexture;
    Action<int>       _onSelect;
    Action<UnitEntry> _onDetail;
    int               _index;

    public UnitEntry Entry => _entry;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(int index, UnitEntry entry,
        Action<int>       onSelect,
        Action<UnitEntry> onDetail)
    {
        _index    = index;
        _entry    = entry;
        _onSelect = onSelect;
        _onDetail = onDetail;

        UnitJob        job    = UnitJobRoller.GetJob(entry.UnitName);
        HeroStatResult result = HeroStatResolver.Resolve(entry);
        Color          gc     = GradeStyle.GetColor(entry.Grade);

        if (_nameText      != null) _nameText.text      = entry.UnitName;
        if (_jobChipText   != null) _jobChipText.text   = JobStyle.GetLabel(job);
        if (_gradeChipText != null)
        {
            _gradeChipText.text  = GradeStyle.GetLabelWithQuality(entry.Grade, entry.UnitName);
            _gradeChipText.color = gc;
        }
        if (_gradeBorder   != null) _gradeBorder.color  = gc;
        if (_hpText      != null) { _hpText.text      = $"{result.Total(StatType.MaxHp):N0}";                                           _hpText.color      = StatColors.Hp;      }
        if (_atkText     != null) { _atkText.text      = $"{result.Total(StatType.Attack):N0}";                                          _atkText.color     = StatColors.Atk;     }
        if (_defText     != null) { _defText.text      = $"{StatDisplayHelper.EffectiveDefensePct(result.Total(StatType.Defense)):F1}%"; _defText.color     = StatColors.Def;     }
        if (_soldierText != null) { _soldierText.text  = $"{Mathf.RoundToInt(result.Total(StatType.SoldierCount))}명";                  _soldierText.color = StatColors.Soldier; }

        if (_selectBtn != null)
        {
            _selectBtn.onClick.RemoveAllListeners();
            _selectBtn.onClick.AddListener(() => _onSelect?.Invoke(_index));
        }

        if (_detailBtn != null)
        {
            _detailBtn.onClick.RemoveAllListeners();
            _detailBtn.onClick.AddListener(() => _onDetail?.Invoke(_entry));
        }

        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);

        // 직업 특성 표시
        if (_traitIconUI != null)
        {
            var traitDb   = TraitDatabase.Current;
            var traitData = traitDb != null
                ? System.Array.Find(traitDb.GetAll(), t => t != null && t.RequiredJob == job)
                : null;
            _traitIconUI.Setup(traitData);
        }

        RefreshSkills(entry, job);
        SetSelected(false);
    }

    // ── 스킬 아이콘 ──────────────────────────────────────────
    //  액티브·패시브 모두 이름 기반 결정론적 추첨이라, 여기서 보여주는 것과
    //  실제 전투에서 쓰는 것이 항상 같다 (GeneralRuntimeBridge 와 동일 호출).
    void RefreshSkills(UnitEntry entry, UnitJob job)
    {
        if (_activeSkillIcon != null)
        {
            var activeDb = ActiveSkillDatabase.Current;
            var rolledId = RareSkillArbiter.Resolve(entry.UnitName, job, activeDb, entry.Grade);
            _activeSkillIcon.SetActiveSkill(rolledId, activeDb?.Get(rolledId));
        }

        if (_passiveSkillIcons == null) return;

        var passiveDb = PassiveSkillDatabase.Current;
        var (p0, p1, p2) = PassiveSkillRoller.Roll(entry.UnitName);
        var rolled = new[] { p0, p1, p2 };
        int slots  = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);

        for (int i = 0; i < _passiveSkillIcons.Length; i++)
        {
            if (_passiveSkillIcons[i] == null) continue;
            if (i < slots && i < rolled.Length)
                _passiveSkillIcons[i].SetPassiveSkill(passiveDb?.Get(rolled[i]));
            else
                _passiveSkillIcons[i].SetLocked();
        }

        if (_passiveSlotText != null)
            _passiveSlotText.text = $"패시브 {slots}/{_passiveSkillIcons.Length}";
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOverlay != null)
            _selectionOverlay.gameObject.SetActive(selected);

        if (_gradeBorder != null && _entry != null)
            _gradeBorder.color = selected ? Color.white : GradeStyle.GetColor(_entry.Grade);
    }
}
