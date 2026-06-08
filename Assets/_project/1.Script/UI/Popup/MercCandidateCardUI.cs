using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercCandidateCardUI.cs
//  용병 상점 후보 카드 1장.
//  이름·등급/직업·체력/공격·방어/용병수 + 액티브 아이콘 + 패시브(이름+설명) 표시.
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
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;

    [Header("스킬 정보")]
    [SerializeField] Image                _activeSkillIcon;
    [SerializeField] TextMeshProUGUI      _activeSkillNameText;
    [SerializeField] TextMeshProUGUI      _passive0Text;
    [SerializeField] TextMeshProUGUI      _passive0DescText;
    [SerializeField] TextMeshProUGUI      _passive1Text;
    [SerializeField] TextMeshProUGUI      _passive1DescText;
    [SerializeField] TextMeshProUGUI      _passive2Text;
    [SerializeField] TextMeshProUGUI      _passive2DescText;

    [SerializeField] Button               _button;
    [SerializeField] Button               _hireBtn;
    [SerializeField] Image                _highlightBorder;

    Texture2D _portraitTexture;

    public void Setup(UnitEntry entry, Action onSelect, Action onHire = null)
    {
        UnitJob        job    = UnitJobRoller.GetJob(entry.UnitName);
        HeroStatResult result = HeroStatResolver.Resolve(entry);
        Color          gc     = GradeStyle.GetColor(entry.Grade);

        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeText   != null) { _gradeText.text  = GradeStyle.GetLabel(entry.Grade); _gradeText.color = gc; }
        if (_nameText    != null) _nameText.text    = entry.UnitName;
        if (_jobText     != null) _jobText.text     = JobStyle.GetLabel(job);
        if (_hpText      != null) _hpText.text      = $"체력 {result.Total(StatType.MaxHp):N0}";
        if (_atkText     != null) _atkText.text     = $"공격 {result.Total(StatType.Attack):N0}";
        if (_defText     != null) _defText.text     = $"방어 {StatDisplayHelper.EffectiveDefensePct(result.Total(StatType.Defense)):F1}%";
        if (_soldierText != null) _soldierText.text = $"용병 {Mathf.Max(0, Mathf.RoundToInt(result.Total(StatType.SoldierCount)))}명";

        FillSkills(job, entry);

        _button?.onClick.RemoveAllListeners();
        _button?.onClick.AddListener(() => onSelect?.Invoke());

        _hireBtn?.onClick.RemoveAllListeners();
        _hireBtn?.onClick.AddListener(() => onHire?.Invoke());
        if (_hireBtn != null) _hireBtn.gameObject.SetActive(onHire != null);

        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImg, ref _portraitTexture);
    }

    public void SetHighlighted(bool on)
    {
        if (_highlightBorder == null) return;
        _highlightBorder.color = on
            ? new Color(0.80f, 0.75f, 1.00f, 0.30f)
            : new Color(0.80f, 0.75f, 1.00f, 0.00f);
    }

    void FillSkills(UnitJob job, UnitEntry entry)
    {
        var activeDb = ActiveSkillDatabase.Current;
        if (activeDb != null)
        {
            var activeId   = ActiveSkillRoller.Roll(entry.UnitName, job, activeDb, entry.Grade);
            var activeData = activeDb.Get(activeId);

            if (_activeSkillIcon != null)
            {
                var key = activeId.IconKey();
                var sm  = SpriteManager.Instance;
                var sp  = (key != null && sm != null) ? sm.Get(key) : null;
                _activeSkillIcon.sprite = sp;
                _activeSkillIcon.color  = sp != null ? Color.white : new Color(0.25f, 0.30f, 0.48f);
            }

            if (_activeSkillNameText != null)
                _activeSkillNameText.text = activeData?.SkillName ?? "-";
        }

        var passiveDb = PassiveSkillDatabase.Current;
        var (s0, s1, s2) = PassiveSkillRoller.Roll(entry.UnitName);
        int slotCount    = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);

        PassiveSkillType[]  passives   = { s0, s1, s2 };
        TextMeshProUGUI[]   nameTexts  = { _passive0Text,     _passive1Text,     _passive2Text };
        TextMeshProUGUI[]   descTexts  = { _passive0DescText, _passive1DescText, _passive2DescText };

        for (int i = 0; i < nameTexts.Length; i++)
        {
            bool show = i < slotCount && passiveDb != null;

            if (nameTexts[i] != null) nameTexts[i].gameObject.SetActive(show);
            if (descTexts[i] != null) descTexts[i].gameObject.SetActive(show);

            if (!show) continue;

            var pd = passiveDb.Get(passives[i]);
            if (nameTexts[i] != null) nameTexts[i].text = pd?.SkillName    ?? "-";
            if (descTexts[i] != null) descTexts[i].text = pd?.Description  ?? "";
        }
    }
}
