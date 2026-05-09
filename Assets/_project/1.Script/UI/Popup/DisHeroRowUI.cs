using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DisHeroRowUI.cs
//  분해 팝업 장수 행 — 초상화·스텟·보상 표시.
//  HeroPanelCreator.BuildHeroRowTemplate() 에서 직렬화 필드 연결.
// ============================================================

public class DisHeroRowUI : MonoBehaviour
{
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameTmp;
    [SerializeField] TextMeshProUGUI      _gradeTmp;
    [SerializeField] TextMeshProUGUI      _hpTmp;
    [SerializeField] TextMeshProUGUI      _jobTmp;
    [SerializeField] TextMeshProUGUI      _atkTmp;
    [SerializeField] Image                _rewardIcon;
    [SerializeField] TextMeshProUGUI      _rewardTmp;

    Texture2D _portraitTexture;

    public void Fill(UnitEntry entry)
    {
        if (_nameTmp != null) _nameTmp.text = entry.UnitName;

        if (_gradeTmp != null)
        {
            _gradeTmp.text  = GradeStyle.GetLabel(entry.Grade);
            _gradeTmp.color = GradeStyle.GetColor(entry.Grade);
        }

        var stat = GeneralStatRoller.Roll(entry.UnitName, entry.Level, entry.Grade);
        if (_hpTmp  != null) _hpTmp.text  = $"체력 {(int)stat.Get(StatType.MaxHp)}";
        if (_jobTmp != null) _jobTmp.text = JobStyle.GetLabel(UnitJobRoller.GetJob(entry.UnitName));
        if (_atkTmp != null) _atkTmp.text = $"공격 {(int)stat.Get(StatType.Attack)}";

        int shards = GetShards(entry);
        if (_rewardTmp  != null) _rewardTmp.text   = $"{shards}조각";
        if (_rewardIcon != null) _rewardIcon.sprite = SpriteManager.Instance?.Get(eItem.SoldierShard.IconKey());

        UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
    }

    static int GetShards(UnitEntry e)
    {
        int[] bases = { 5, 10, 20, 35, 60 };
        return bases[Mathf.Clamp((int)e.Grade, 0, bases.Length - 1)] + e.Level / 5;
    }
}
