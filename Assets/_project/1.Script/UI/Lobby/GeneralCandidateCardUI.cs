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
    [SerializeField] TextMeshProUGUI      _jobGradeText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;
    [SerializeField] Button               _selectBtn;
    [SerializeField] Button               _detailBtn;

    [Header("특성")]
    [SerializeField] TraitIconUI _traitIconUI;

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
        string         gcHex  = ColorUtility.ToHtmlStringRGB(gc);

        if (_nameText     != null) _nameText.text     = entry.UnitName;
        if (_jobGradeText != null) _jobGradeText.text =
            $"{JobStyle.GetLabel(job)}  ·  <color=#{gcHex}>{GradeStyle.GetLabel(entry.Grade)}</color>";
        if (_gradeBorder  != null) _gradeBorder.color = gc;
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

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOverlay != null)
            _selectionOverlay.gameObject.SetActive(selected);

        if (_gradeBorder != null && _entry != null)
            _gradeBorder.color = selected ? Color.white : GradeStyle.GetColor(_entry.Grade);
    }
}
