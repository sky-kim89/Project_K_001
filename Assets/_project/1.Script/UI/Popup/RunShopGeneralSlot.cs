using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopGeneralSlot.cs
//  런 상점의 "용병 한 칸" — HeroCard + 고용 버튼.
//
//  카드는 고용 후에도 계속 눌러 상세를 볼 수 있다.
//  고용 버튼 자리에는 판(_soldOut)이 덮인다.
// ============================================================

public class RunShopGeneralSlot : MonoBehaviour
{
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImg;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _jobText;
    [SerializeField] TextMeshProUGUI      _gradeText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;   // SoldierCount 용병수
    [SerializeField] TextMeshProUGUI      _passiveSlotsText;
    [SerializeField] TextMeshProUGUI      _costText;
    [SerializeField] Button               _buyBtn;
    [SerializeField] Button               _cardBtn;       // 카드 영역 클릭 → HeroDetail
    [SerializeField] GameObject           _soldOut;
    [SerializeField] TextMeshProUGUI      _soldText;

    Func<UnitEntry, int, bool> _onHire;   // 반환값 = 실제로 고용했는지
    UnitEntry              _entry;
    int                    _cost;
    Texture2D              _portraitTexture;
    bool                   _sold;

    public void SetSoldOut()
    {
        _sold = true;
        if (_soldOut  != null) _soldOut.SetActive(true);
        if (_soldText != null) _soldText.text = "고용 완료";
        if (_buyBtn   != null) _buyBtn.gameObject.SetActive(false);
    }

    /// <summary>골드가 모자라면 고용 버튼을 잠근다. 카드(상세 보기)는 계속 열린다.</summary>
    public void SetAffordable(bool canAfford)
    {
        if (_sold || _entry == null) return;
        if (_buyBtn != null) _buyBtn.interactable = canAfford;
    }

    public void Setup(UnitEntry entry, int cost, Func<UnitEntry, int, bool> onHire)
    {
        _entry = entry; _cost = cost; _onHire = onHire;
        bool valid = entry != null;
        _sold = !valid;

        if (_buyBtn   != null) { _buyBtn.gameObject.SetActive(valid); _buyBtn.interactable = valid; }
        if (_soldOut  != null) _soldOut.SetActive(!valid);
        if (_soldText != null) _soldText.text = "고용 불가";
        if (_cardBtn  != null) _cardBtn.interactable = valid;
        if (_costText != null) _costText.text = valid ? $"{cost:N0}" : "";

        if (valid)
        {
            var job    = UnitJobRoller.GetJob(entry.UnitName);
            var result = HeroStatResolver.Resolve(entry);
            var cfg    = GameplayConfig.Current;
            var gc     = GradeStyle.GetColor(entry.Grade);

            if (_nameText  != null) _nameText.text  = entry.UnitName;
            if (_jobText   != null) _jobText.text   = JobStyle.GetLabel(job);
            if (_gradeText != null) { _gradeText.text = GradeStyle.GetLabel(entry.Grade); _gradeText.color = gc; }

            if (_hpText      != null) { _hpText.text      = $"{result.Total(StatType.MaxHp):N0}"; _hpText.color = StatColors.Hp; }
            if (_atkText     != null) { _atkText.text     = $"{result.Total(StatType.Attack):N0}"; _atkText.color = StatColors.Atk; }
            if (_defText     != null) { _defText.text     = $"{StatDisplayHelper.EffectiveDefensePct(result.Total(StatType.Defense)):F1}%"; _defText.color = StatColors.Def; }
            if (_soldierText != null) { _soldierText.text = $"{Mathf.RoundToInt(result.Total(StatType.SoldierCount))}명"; _soldierText.color = StatColors.Soldier; }
            if (_passiveSlotsText != null) _passiveSlotsText.text = $"패시브 {cfg.GetPassiveSlotCount(entry.Grade)}슬롯";

            UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
                _portraitBridge, _portraitBg, _portraitImg, ref _portraitTexture);
        }
        else
        {
            if (_nameText         != null) _nameText.text         = "—";
            if (_jobText          != null) _jobText.text          = "";
            if (_gradeText        != null) _gradeText.text        = "";
            if (_hpText           != null) _hpText.text           = "";
            if (_atkText          != null) _atkText.text          = "";
            if (_defText          != null) _defText.text          = "";
            if (_soldierText      != null) _soldierText.text      = "";
            if (_passiveSlotsText != null) _passiveSlotsText.text = "";
        }

        if (_buyBtn != null)
        {
            _buyBtn.onClick.RemoveAllListeners();
            // 실제로 고용된 경우에만 품절 처리 (골드 부족·배치 슬롯 없음 → 실패)
            _buyBtn.onClick.AddListener(() =>
            {
                if (_onHire(_entry, _cost)) SetSoldOut();
            });
        }

        if (_cardBtn != null)
        {
            _cardBtn.onClick.RemoveAllListeners();
            if (valid)
                _cardBtn.onClick.AddListener(() =>
                {
                    var popup = PopupManager.Instance?.Open<HeroDetailPopup>(PopupType.HeroDetail);
                    popup?.SetupPreview(_entry);
                });
        }
    }
}
