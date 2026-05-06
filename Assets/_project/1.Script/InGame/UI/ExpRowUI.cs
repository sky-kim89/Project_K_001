using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  ExpRowUI.cs
//  전투 결과 팝업에서 영웅 1명의 EXP 획득 내역을 표시.
//
//  Hierarchy 예시:
//    ExpRow (ExpRowUI)
//      ├── PortraitBg   (Image — 직업 배경색)
//      ├── PortraitImage (Image — 캐릭터 스프라이트)
//      ├── PortraitBridge (UnitAppearanceBridge — 숨김 GO)
//      ├── NameText     (TMP)
//      ├── LevelText    (TMP — "Lv.12")
//      ├── ExpText      (TMP — "+120 EXP")
//      └── LevelUpText  (TMP — "▲2 레벨업!", 없으면 숨김)
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

    Texture2D _portraitTexture;

    public void Setup(BattleContext.UnitExpGain gain)
    {
        if (_nameText  != null) _nameText.text  = gain.UnitName;
        if (_levelText != null) _levelText.text = $"Lv.{gain.NewLevel}";
        if (_expText   != null) _expText.text   = $"+{gain.ExpGained} EXP";

        if (_levelUpText != null)
        {
            bool leveledUp = gain.LevelsGained > 0;
            _levelUpText.gameObject.SetActive(leveledUp);
            if (leveledUp)
                _levelUpText.text = gain.LevelsGained == 1
                    ? "▲ 레벨업!"
                    : $"▲{gain.LevelsGained} 레벨업!";
        }

        var unitData = UserDataManager.Instance?.Get<UnitData>();
        var entry    = unitData?.GetUnit(gain.UnitName);
        if (entry != null)
        {
            UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
            UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
                _portraitBridge, _portraitBg, _portraitImage, ref _portraitTexture);
        }
    }
}
