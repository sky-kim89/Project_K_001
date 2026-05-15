using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercCandidateCardUI.cs
//  용병 상점 후보 카드 1장.
//  이름·등급·직업·HP·ATK 표시, 클릭 시 onSelect 호출.
// ============================================================

public class MercCandidateCardUI : MonoBehaviour
{
    [SerializeField] Image                _gradeBorder;
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImg;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _gradeText;
    [SerializeField] TextMeshProUGUI      _jobText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] Button               _button;

    Texture2D _portraitTexture;

    public void Setup(UnitEntry entry, Action onSelect)
    {
        UnitJob        job    = UnitJobRoller.GetJob(entry.UnitName);
        HeroStatResult result = HeroStatResolver.Resolve(entry);
        Color          gc     = GradeStyle.GetColor(entry.Grade);

        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeText   != null) { _gradeText.text  = GradeStyle.GetLabel(entry.Grade); _gradeText.color = gc; }
        if (_nameText    != null) _nameText.text    = entry.UnitName;
        if (_jobText     != null) _jobText.text     = JobStyle.GetLabel(job);
        if (_hpText      != null) _hpText.text      = $"HP {result.Total(StatType.MaxHp):N0}";
        if (_atkText     != null) _atkText.text     = $"공격 {result.Total(StatType.Attack):N0}";

        _button?.onClick.RemoveAllListeners();
        _button?.onClick.AddListener(() => onSelect?.Invoke());

        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImg, ref _portraitTexture);
    }
}
