using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercSlotCardUI.cs
//  용병 상점 SlotFullView 각 슬롯.
//  ShowOccupied / ShowEmpty 로 상태 전환.
// ============================================================

public class MercSlotCardUI : MonoBehaviour
{
    [SerializeField] Image                _gradeBorder;
    [SerializeField] Image                _gradeBadge;
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImg;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _levelText;
    [SerializeField] TextMeshProUGUI      _gradeText;
    [SerializeField] TextMeshProUGUI      _jobText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;
    [SerializeField] Button               _fireButton;
    [SerializeField] GameObject           _occupiedGroup;
    [SerializeField] GameObject           _emptyGroup;

    Texture2D _portraitTexture;

    public void ShowOccupied(UnitEntry entry, System.Action onFire, bool canFire = true)
    {
        _occupiedGroup.SetActive(true);
        _emptyGroup.SetActive(false);

        UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
        Color   gc  = GradeStyle.GetColor(entry.Grade);

        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeBadge  != null) _gradeBadge.color  = gc;
        if (_gradeText   != null) { _gradeText.text = GradeStyle.GetLabelWithQuality(entry.Grade, entry.UnitName); _gradeText.color = Color.white; }
        if (_nameText    != null) _nameText.text  = entry.UnitName;
        if (_levelText   != null) _levelText.text = $"Lv.{entry.Level}";
        if (_jobText     != null) _jobText.text   = JobStyle.GetLabel(job);

        var stat = HeroStatResolver.Resolve(entry);
        if (_hpText      != null) _hpText.text      = $"{stat.Total(StatType.MaxHp):N0}";
        if (_atkText     != null) _atkText.text     = $"{stat.Total(StatType.Attack):N0}";
        if (_defText     != null) _defText.text     = $"{StatDisplayHelper.EffectiveDefensePct(stat.Total(StatType.Defense)):F1}%";
        if (_soldierText != null) _soldierText.text = $"{Mathf.RoundToInt(stat.Total(StatType.SoldierCount))}명";

        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImg, ref _portraitTexture);

        if (_fireButton != null)
        {
            _fireButton.onClick.RemoveAllListeners();
            _fireButton.onClick.AddListener(() => onFire?.Invoke());
            _fireButton.gameObject.SetActive(true);
            _fireButton.interactable = canFire;
        }
    }

    public void ShowEmpty()
    {
        _occupiedGroup.SetActive(false);
        _emptyGroup.SetActive(true);
        if (_fireButton != null) _fireButton.gameObject.SetActive(false);
    }
}
