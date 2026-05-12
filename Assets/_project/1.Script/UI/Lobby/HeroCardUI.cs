using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroCardUI.cs
//  영웅 목록에서 장군 1명을 표시하는 카드 컴포넌트.
//  초상화는 PortraitPreview GO 의 CharacterBuilder 로 생성.
// ============================================================

public class HeroCardUI : MonoBehaviour
{
    [SerializeField] Image                _gradeBorder;
    [SerializeField] Image                _gradeBadge;
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _levelText;
    [SerializeField] TextMeshProUGUI      _gradeText;
    [SerializeField] TextMeshProUGUI      _jobText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;
    [SerializeField] Button               _button;
    [SerializeField] GameObject           _deployBadge;  // 배치 배지 컨테이너 (초상화 우상단)
    [SerializeField] TextMeshProUGUI      _deployText;   // 배치 슬롯 번호

    public UnitEntry Entry { get; private set; }

    Action<UnitEntry> _onSelect;
    Texture2D         _portraitTexture;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(UnitEntry entry, Action<UnitEntry> onSelect)
    {
        Entry     = entry;
        _onSelect = onSelect;

        if (_nameText  != null) _nameText.text  = entry.UnitName;
        if (_levelText != null) _levelText.text = $"Lv.{entry.Level}";
        if (_gradeText != null) _gradeText.text = GradeStyle.GetLabel(entry.Grade);

        Color gc = GradeStyle.GetColor(entry.Grade);
        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeBadge  != null) _gradeBadge.color  = gc;
        if (_gradeText   != null) _gradeText.color   = Color.white;

        UnitJob        job    = UnitJobRoller.GetJob(entry.UnitName);
        HeroStatResult result = HeroStatResolver.Resolve(entry);

        if (_jobText     != null) _jobText.text     = JobStyle.GetLabel(job);
        if (_hpText      != null) _hpText.text      = $"{result.Total(StatType.MaxHp):N0}";
        if (_atkText     != null) _atkText.text     = $"{result.Total(StatType.Attack):N0}";
        if (_defText     != null) _defText.text     = $"{result.Total(StatType.Defense) * 100f:F0}%";
        if (_soldierText != null) _soldierText.text = $"{Mathf.RoundToInt(result.Total(StatType.SoldierCount))}명";

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelect?.Invoke(Entry));
        }

        var deployData = UserDataManager.Instance.Get<DeploymentData>();
        RefreshDeploy(deployData);

        UpdatePortrait(entry);
    }

    public void RefreshDeploy(DeploymentData data)
    {
        int slot = data.GetSlotOf(Entry.UnitName);
        if (_deployBadge != null) _deployBadge.SetActive(slot >= 0);
        if (_deployText  != null && slot >= 0) _deployText.text = $"{slot + 1}";
    }

    public void SetSelected(bool selected)
    {
        if (_gradeBorder == null) return;
        _gradeBorder.color = selected
            ? Color.white
            : GradeStyle.GetColor(Entry?.Grade ?? UnitGrade.Normal);
    }

    // ── 초상화 ───────────────────────────────────────────────

    void UpdatePortrait(UnitEntry entry)
    {
        UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
    }
}
