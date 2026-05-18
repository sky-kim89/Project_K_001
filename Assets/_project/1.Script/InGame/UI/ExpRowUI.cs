using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  ExpRowUI.cs
//  전투 결과 팝업에서 영웅 1명의 행.
//
//  한 줄 레이아웃:
//    [Portrait] [NameText] [StatBar] [TotalText] [ExpText] [LevelText] [LevelUpText]
//
//  탭에 따라 StatBar / TotalText 내용이 변경된다.
//  EXP 정보(ExpText/LevelText/LevelUpText)는 오른쪽에 압축해서 항상 표시.
// ============================================================

public class ExpRowUI : MonoBehaviour
{
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _levelText;
    [SerializeField] TextMeshProUGUI      _expText;
    [SerializeField] TextMeshProUGUI      _levelUpText;

    [Header("통계 바")]
    [SerializeField] StatBarUI            _statBar;
    [SerializeField] TextMeshProUGUI      _totalText;

    Texture2D        _portraitTexture;
    GeneralStatEntry _stats;

    public void Setup(BattleContext.UnitExpGain gain)
    {
        if (_nameText  != null) _nameText.text  = gain.UnitName;
        if (_levelText != null) _levelText.text = $"Lv.{gain.NewLevel}";
        if (_expText   != null) _expText.text   = $"+{gain.ExpGained}";

        if (_levelUpText != null)
        {
            bool leveledUp = gain.LevelsGained > 0;
            _levelUpText.gameObject.SetActive(leveledUp);
            if (leveledUp)
                _levelUpText.text = gain.LevelsGained == 1
                    ? "▲ UP!"
                    : $"▲{gain.LevelsGained}UP!";
        }

        var unitData = UserDataManager.Instance?.Get<UnitData>();
        var entry    = unitData?.GetUnit(gain.UnitName);
        if (entry != null)
        {
            UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
            UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
                _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
        }

        if (_statBar != null) _statBar.Clear();
        if (_totalText != null) _totalText.text = "";
    }

    /// <summary>통계 데이터를 연결한다. Setup() 후 BattleResultPopup 에서 호출.</summary>
    public void SetStats(GeneralStatEntry stats)
    {
        _stats = stats;
    }

    /// <summary>탭 전환 시 호출. 바와 총량 텍스트를 갱신한다.</summary>
    public void RefreshTab(CombatStatTab tab, float maxValue)
    {
        if (_statBar == null) return;
        if (_stats == null || maxValue <= 0f)
        {
            _statBar.Clear();
            if (_totalText != null) _totalText.text = "";
            return;
        }

        switch (tab)
        {
            case CombatStatTab.Damage:
            {
                float total = _stats.TotalDamageDealt;
                _statBar.Setup(
                    new[] { _stats.GeneralDamageDealt, _stats.SoldierDamageDealt, _stats.SkillDamageDealt },
                    StatBarUI.DamageColors,
                    total / maxValue);
                if (_totalText != null) _totalText.text = FormatTotal("딜", total);
                break;
            }
            case CombatStatTab.Tank:
            {
                float taken    = _stats.DamageTaken + _stats.SoldierDamageTaken;
                float absorbed = _stats.DamageAbsorbed;
                float total    = taken + absorbed;
                _statBar.Setup(
                    new[] { taken, absorbed },
                    StatBarUI.TankColors,
                    total / maxValue);
                if (_totalText != null) _totalText.text = FormatTotal("수비", taken);
                break;
            }
            case CombatStatTab.Heal:
            {
                float heal = _stats.HealingDone;
                _statBar.Setup(
                    new[] { heal },
                    StatBarUI.HealColors,
                    heal / maxValue);
                if (_totalText != null) _totalText.text = FormatTotal("힐", heal);
                break;
            }
        }
    }

    static string FormatTotal(string label, float value)
    {
        if (value >= 1_000_000f) return $"{value / 1_000_000f:0.0}M";
        if (value >= 10_000f)    return $"{value / 1_000f:0.#}K";
        return $"{(int)value:N0}";
    }
}
