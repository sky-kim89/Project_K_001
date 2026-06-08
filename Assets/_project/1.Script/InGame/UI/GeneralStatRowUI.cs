using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  GeneralStatRowUI.cs
//  환생 팝업에서 장수 1명의 통계 행.
//  ExpRowUI와 동일 구조이지만 경험치 필드 없음.
// ============================================================

public class GeneralStatRowUI : MonoBehaviour
{
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] StatBarUI            _statBar;
    [SerializeField] TextMeshProUGUI      _totalText;
    [SerializeField] TextMeshProUGUI      _legendText;
    [SerializeField] TextMeshProUGUI      _dpsText;

    Texture2D        _portraitTexture;
    GeneralStatEntry _stats;
    float            _elapsedSec;

    public void Setup(string unitName)
    {
        if (_nameText != null) _nameText.text = unitName;

        var unitData = UserDataManager.Instance?.Get<UnitData>();
        var entry    = unitData?.GetUnit(unitName);
        if (entry != null)
        {
            UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
            UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
                _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
        }

        if (_statBar   != null) _statBar.Clear();
        if (_totalText != null) _totalText.text = "";
    }

    public void SetStats(GeneralStatEntry stats) => _stats = stats;

    public void SetDPS(float elapsedSec)
    {
        _elapsedSec = elapsedSec;
    }

    public void RefreshTab(CombatStatTab tab, float maxValue)
    {
        UpdateLegend(tab);

        if (_statBar == null) return;
        if (_stats == null || maxValue <= 0f)
        {
            _statBar.Clear();
            if (_totalText != null) _totalText.text = "";
            return;
        }

        RefreshDpsText(tab);

        switch (tab)
        {
            case CombatStatTab.Damage:
            {
                float total = _stats.TotalDamageDealt;
                _statBar.Setup(
                    new[] { _stats.GeneralDamageDealt, _stats.SoldierDamageDealt, _stats.SkillDamageDealt },
                    StatBarUI.DamageColors,
                    total / maxValue);
                if (_totalText != null) _totalText.text = FormatTotal(total);
                break;
            }
            case CombatStatTab.Tank:
            {
                float taken    = _stats.DamageTaken + _stats.SoldierDamageTaken;
                float absorbed = _stats.DamageAbsorbed;
                _statBar.Setup(
                    new[] { taken, absorbed },
                    StatBarUI.TankColors,
                    (taken + absorbed) / maxValue);
                if (_totalText != null) _totalText.text = FormatTotal(taken);
                break;
            }
            case CombatStatTab.Heal:
            {
                float heal = _stats.HealingDone;
                _statBar.Setup(
                    new[] { heal },
                    StatBarUI.HealColors,
                    heal / maxValue);
                if (_totalText != null) _totalText.text = FormatTotal(heal);
                break;
            }
        }
    }

    void RefreshDpsText(CombatStatTab tab)
    {
        if (_dpsText == null) return;
        if (tab != CombatStatTab.Damage || _stats == null || _elapsedSec <= 0f)
        {
            _dpsText.gameObject.SetActive(false);
            return;
        }
        _dpsText.gameObject.SetActive(true);
        _dpsText.text = $"DPS {FormatTotal(_stats.TotalDamageDealt / _elapsedSec)}";
    }

    void UpdateLegend(CombatStatTab tab)
    {
        if (_legendText == null) return;
        _legendText.text = tab switch
        {
            CombatStatTab.Damage => "<color=#4D8CF2>■</color> 장군  <color=#59CC74>■</color> 병사  <color=#F28C33>■</color> 스킬",
            CombatStatTab.Tank   => "<color=#E64040>■</color> 받은피해  <color=#4D8CF2>■</color> 감소피해",
            CombatStatTab.Heal   => "<color=#59D98C>■</color> 치유",
            _                    => "",
        };
    }

    static string FormatTotal(float value)
    {
        if (value >= 1_000_000f) return $"{value / 1_000_000f:0.0}M";
        if (value >= 10_000f)    return $"{value / 1_000f:0.#}K";
        return $"{(int)value:N0}";
    }
}
