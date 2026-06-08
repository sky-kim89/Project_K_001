using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunShopEquipSlot : MonoBehaviour
{
    [SerializeField] Image           _iconImage;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _gradeText;
    [SerializeField] TextMeshProUGUI _statText;
    [SerializeField] TextMeshProUGUI _costText;
    [SerializeField] Button          _buyBtn;
    [SerializeField] GameObject      _soldOut;

    Action<EquipmentData, int> _onBuy;
    EquipmentData              _data;
    int                        _cost;

    public void Setup(EquipmentData data, int cost, Action<EquipmentData, int> onBuy)
    {
        _data = data; _cost = cost; _onBuy = onBuy;
        bool valid = data != null;

        if (_soldOut != null) _soldOut.SetActive(!valid);
        if (_buyBtn  != null) _buyBtn.interactable = valid;

        if (_iconImage != null)
        {
            _iconImage.sprite = data?.Icon;
            _iconImage.color  = (data != null && data.Icon != null) ? Color.white : new Color(0.3f, 0.3f, 0.4f);
        }
        if (_nameText  != null) _nameText.text  = data != null ? data.EquipmentName : "—";
        if (_gradeText != null)
        {
            _gradeText.text  = data != null ? GradeStyle.GetLabel(data.Grade) : "";
            _gradeText.color = data != null ? GradeStyle.GetColor(data.Grade) : Color.white;
        }
        if (_costText != null) _costText.text = valid ? $"{cost}" : "";

        if (_statText != null)
        {
            if (data != null && data.StatEntries.Count > 0)
            {
                var loc = LocalizationManager.Instance;
                var sb  = new System.Text.StringBuilder();
                foreach (var e in data.StatEntries)
                    sb.AppendLine($"{loc.Get(e.Stat.ToString())}  {StatDisplayHelper.FormatStat(e.Stat, data.GetStatValue(e, 0))}");
                _statText.text = sb.ToString().TrimEnd();
            }
            else _statText.text = "";
        }

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
}
