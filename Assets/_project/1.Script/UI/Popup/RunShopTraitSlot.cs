using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunShopTraitSlot : MonoBehaviour
{
    [SerializeField] TraitIconUI     _traitIcon;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _descText;
    [SerializeField] TextMeshProUGUI _statsText;
    [SerializeField] TextMeshProUGUI _costText;
    [SerializeField] Button          _buyBtn;
    [SerializeField] GameObject      _soldOut;

    Action<TraitData, int> _onBuy;
    TraitData              _data;
    int                    _cost;

    public void Setup(TraitData data, int cost, Action<TraitData, int> onBuy)
    {
        _data = data; _cost = cost; _onBuy = onBuy;
        bool valid = data != null;

        if (_traitIcon != null) _traitIcon.Setup(data);
        if (_nameText  != null) _nameText.text = data != null ? data.TraitName    : "";
        if (_descText  != null) _descText.text = data != null ? data.Description : "";
        if (_statsText != null) _statsText.text = BuildEffectsText(data);
        if (_costText  != null) _costText.text = valid ? $"{cost}" : "";
        if (_soldOut   != null) _soldOut.SetActive(!valid);
        if (_buyBtn    != null) _buyBtn.interactable = valid;

        if (_buyBtn != null)
        {
            _buyBtn.onClick.RemoveAllListeners();
            _buyBtn.onClick.AddListener(() =>
            {
                _onBuy?.Invoke(_data, _cost);
                if (_soldOut != null) _soldOut.SetActive(true);
                if (_buyBtn  != null) _buyBtn.interactable = false;
            });
        }
    }

    static string BuildEffectsText(TraitData data)
    {
        if (data == null || data.Effects.Length == 0) return "";
        var loc = LocalizationManager.Instance;
        var sb  = new System.Text.StringBuilder();
        foreach (var e in data.Effects)
        {
            string val = e.IsPercent
                ? $"+{e.Value * 100f:F0}%"
                : StatDisplayHelper.FormatStat(e.Stat, e.Value, withSign: true);
            sb.AppendLine($"{loc.Get(e.Stat.ToString())} {val}");
        }
        return sb.ToString().TrimEnd();
    }
}
