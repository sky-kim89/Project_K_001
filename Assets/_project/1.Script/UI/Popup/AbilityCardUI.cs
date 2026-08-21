using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilityCardUI.cs
//  AbilitySelectPopup 내 어빌리티 1장 카드 UI.
// ============================================================

public class AbilityCardUI : MonoBehaviour
{
    [SerializeField] Image            _gradeBar;
    [SerializeField] Image            _icon;
    [SerializeField] TextMeshProUGUI  _gradeTmp;
    [SerializeField] TextMeshProUGUI  _nameTmp;
    [SerializeField] TextMeshProUGUI  _targetTmp;
    [SerializeField] TextMeshProUGUI  _descTmp;
    [SerializeField] Button           _selectBtn;
    [SerializeField] TextMeshProUGUI  _levelTmp;

    // ── 트리거 표기 ───────────────────────────────────────────
    //
    //  ⚠ 카드 한 칸이 좁다 (328px 중 절반 ≈ 150px)
    //    예전엔 "발동: 피격 시" 처럼 접두어를 붙여 8글자가 되면서 두 줄로 접혔다.
    //    이 줄은 이미 [등급][대상] 자리라 "발동:" 을 안 붙여도 뜻이 통한다.
    //
    //  ⚠ 긴 트리거는 짧은 말로 바꿔 쓴다
    //    "스테이지 클리어 시"(9글자)는 접두어를 떼도 안 들어간다.
    //    툴팁·설명에는 원래 문구가 그대로 나가고, 카드에서만 줄인다.
    static string TriggerLabel(PassiveTrigger t) => t switch
    {
        PassiveTrigger.StageClear    => "클리어 시",
        PassiveTrigger.OnBattleStart => "전투 시작",
        _                            => LocalizationManager.Instance.Get(t.ToString()),
    };

    /// <param name="currentLevel">현재 보유 레벨 (0=미보유). 선택 후 currentLevel+1 이 됨.</param>
    public void Setup(AbilityData data, Action<AbilityData> onClicked, int currentLevel = 0)
    {
        Color gradeColor = AbilityUIHelper.GradeColor(data.Grade);

        if (_gradeBar  != null) _gradeBar.color  = gradeColor;
        if (_icon      != null) _icon.sprite      = data.Icon;

        if (_gradeTmp != null)
        {
            _gradeTmp.text  = LocalizationManager.Instance.Get(data.Grade.ToString());
            _gradeTmp.color = gradeColor;
        }

        if (_nameTmp   != null) _nameTmp.text   = data.AbilityName;
        if (_targetTmp != null)
            _targetTmp.text = (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery)
                ? TriggerLabel(data.GetTriggerType())
                : LocalizationManager.Instance.Get(data.Target.ToString());
        if (_descTmp   != null) _descTmp.text   = BuildDesc(data);

        // 레벨 표시 — MaxLevel > 1 인 어빌리티만 (Normal/Advanced)
        if (_levelTmp != null)
        {
            int maxLv = data.MaxLevel;
            if (maxLv <= 1)
            {
                _levelTmp.gameObject.SetActive(false);
            }
            else
            {
                _levelTmp.gameObject.SetActive(true);
                int nextLv = currentLevel + 1;
                _levelTmp.text  = currentLevel > 0
                    ? $"Lv {currentLevel} → {nextLv} / {maxLv}"
                    : $"Lv {nextLv} / {maxLv}";
                _levelTmp.color = nextLv >= maxLv ? new Color(1f, 0.85f, 0.2f) : gradeColor;
            }
        }

        if (_selectBtn != null)
        {
            _selectBtn.onClick.RemoveAllListeners();
            var captured = data;
            _selectBtn.onClick.AddListener(() => onClicked?.Invoke(captured));
        }
    }

    static string BuildDesc(AbilityData data)
    {
        if (data.Grade == AbilityGrade.Special || data.Grade == AbilityGrade.Mastery)
            return data.Description;

        // 스탯 창의 '어빌리티' 색과 같은 주황 — 색만 보고 출처를 알 수 있게 한다
        string d = StatBonusColors.Wrap(StatSource.Ability,
            $"{LocalizationManager.Instance.Get(data.Stat1.ToString())} {AbilityUIHelper.FormatStatValue(data.Stat1, data.Value1)}");
        if (data.HasStat2) d += "\n" + StatBonusColors.Wrap(StatSource.Ability,
            $"{LocalizationManager.Instance.Get(data.Stat2.ToString())} {AbilityUIHelper.FormatStatValue(data.Stat2, data.Value2)}");
        return d;
    }
}
