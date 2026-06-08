using System.Collections;
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
    [SerializeField] TextMeshProUGUI      _legendText;
    [SerializeField] TextMeshProUGUI      _dpsText;

    Texture2D        _portraitTexture;
    GeneralStatEntry _stats;
    float            _elapsedSec;

    public void Setup(BattleContext.UnitExpGain gain)
    {
        if (_nameText  != null) _nameText.text  = gain.UnitName;
        if (_levelText != null) _levelText.text = $"Lv.{gain.NewLevel}";
        if (_expText   != null) _expText.text   = $"Exp {gain.ExpGained}";

        StopAllCoroutines();
        bool leveledUp = gain.LevelsGained > 0;
        if (_levelUpText != null)
        {
            _levelUpText.gameObject.SetActive(leveledUp);
            if (leveledUp)
                _levelUpText.text = gain.LevelsGained == 1 ? "▲UP!" : $"▲{gain.LevelsGained}UP!";
        }
        if (_levelText != null) _levelText.gameObject.SetActive(!leveledUp);
        if (leveledUp) StartCoroutine(BlinkLevelUp());

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

    public void SetDPS(float elapsedSec)
    {
        _elapsedSec = elapsedSec;
    }

    /// <summary>탭 전환 시 호출. 바와 총량 텍스트를 갱신한다.</summary>
    public void RefreshTab(CombatStatTab tab, float maxValue)
    {
        UpdateLegend(tab);
        RefreshDpsText(tab);

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
                if (_totalText != null) _totalText.text = FormatTotal(total);
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

    IEnumerator BlinkLevelUp()
    {
        bool showLevelUp = true;
        while (true)
        {
            if (_levelUpText != null) _levelUpText.gameObject.SetActive(showLevelUp);
            if (_levelText   != null) _levelText.gameObject.SetActive(!showLevelUp);
            showLevelUp = !showLevelUp;
            yield return new WaitForSeconds(1f);
        }
    }

    static string FormatTotal(float value)
    {
        if (value >= 1_000_000f) return $"{value / 1_000_000f:0.0}M";
        if (value >= 10_000f)    return $"{value / 1_000f:0.#}K";
        return $"{(int)value:N0}";
    }
}
