using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RunShopPopup.cs
//  런 중간 상점 팝업.
//
//  레이아웃:
//    상단: 장비 카드 4종 + 특성 카드 2종
//    중단: 장수 카드 5종 (고용 가능 후보)
//    하단: 새로고침 버튼
//
//  Inspector 연결 (RunShopCreator 자동):
//    _equipSlots[0~3]   : RunShopEquipSlot
//    _traitSlots[0~1]   : RunShopTraitSlot
//    _generalSlots[0~4] : RunShopGeneralSlot
//    _refreshBtn        : 새로고침 버튼
//    _refreshCostText   : 새로고침 비용 TMP
//    _closeBtn          : 닫기 버튼
// ============================================================

public class RunShopPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [Header("장비 슬롯 (4종)")]
    [SerializeField] RunShopEquipSlot[] _equipSlots;

    [Header("특성 슬롯 (2종)")]
    [SerializeField] RunShopTraitSlot[] _traitSlots;

    [Header("장수 슬롯 (5종)")]
    [SerializeField] RunShopGeneralSlot[] _generalSlots;

    [Header("UI")]
    [SerializeField] Button          _refreshBtn;
    [SerializeField] TextMeshProUGUI _refreshCostText;
    [SerializeField] Button          _closeBtn;

    const int RefreshBaseCost = 100;
    int _refreshCount;

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        _refreshBtn?.onClick.AddListener(OnRefresh);
    }

    protected override void OnAfterOpen()
    {
        _refreshCount = 0;
        GenerateShop();
    }

    // ── 상점 생성 ─────────────────────────────────────────────

    void GenerateShop()
    {
        RefreshEquip();
        RefreshTraits();
        RefreshGenerals();
        RefreshRefreshBtn();
    }

    void RefreshEquip()
    {
        var db = EquipmentDatabase.Current;
        if (db == null || _equipSlots == null) return;

        int stageLevel = UserDataManager.Instance?.Get<StageProgressData>()?.CurrentRunStage + 1 ?? 1;
        var picked  = new List<EquipmentData>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < _equipSlots.Length; i++)
        {
            EquipmentData data = null;
            for (int retry = 0; retry < 30; retry++)
            {
                var candidate = db.PickRandom(stageLevel);
                if (candidate == null) break;
                if (usedIds.Add(candidate.EquipmentId)) { data = candidate; break; }
            }
            // 풀이 작아 유니크 아이템을 못 찾은 경우 중복 허용 fallback
            if (data == null) data = db.PickRandom(stageLevel);
            picked.Add(data);
        }

        for (int i = 0; i < _equipSlots.Length; i++)
        {
            var data = picked[i];
            int cost = data != null ? CalcEquipCost(data) : 0;
            _equipSlots[i]?.Setup(data, cost, OnBuyEquip);
        }
    }

    void RefreshTraits()
    {
        var db = TraitDatabase.Current;
        if (db == null || _traitSlots == null) return;

        var owned = UserDataManager.Instance?.Get<RunTraitData>();
        var pool  = new List<TraitData>();
        foreach (TraitType t in Enum.GetValues(typeof(TraitType)))
        {
            if (t == TraitType.None) continue;
            if (owned != null && owned.HasTrait(t)) continue;
            var td = db.Get(t);
            if (td != null) pool.Add(td);
        }
        Shuffle(pool);

        for (int i = 0; i < _traitSlots.Length; i++)
        {
            var data = i < pool.Count ? pool[i] : null;
            if (_traitSlots[i] != null) _traitSlots[i].Setup(data, 400, OnBuyTrait);
        }
    }

    void RefreshGenerals()
    {
        if (_generalSlots == null) return;

        var allNames = UserDataManager.Instance?.Get<UnitData>()?.GetAvailableNames();
        if (allNames == null || allNames.Count == 0) return;

        var candidates = new List<UnitEntry>();
        var used       = new HashSet<string>();
        var rng        = new System.Random();
        for (int i = 0; i < _generalSlots.Length; i++)
        {
            string chosen = null;
            for (int retry = 0; retry < 20; retry++)
            {
                string nm = allNames[rng.Next(allNames.Count)];
                if (used.Add(nm)) { chosen = nm; break; }
            }
            if (chosen == null) chosen = allNames[i % allNames.Count];

            candidates.Add(new UnitEntry
            {
                UnitName     = chosen,
                Level        = 1,
                Exp          = 0,
                GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)UnitJobRoller.GetBirthGrade(chosen)),
            });
        }

        int cost = GameplayConfig.Current.HireMercenaryCost;
        for (int i = 0; i < _generalSlots.Length; i++)
        {
            if (_generalSlots[i] != null)
                _generalSlots[i].Setup(i < candidates.Count ? candidates[i] : null, cost, OnHireGeneral);
        }
    }

    void RefreshRefreshBtn()
    {
        int cost = RefreshBaseCost * (_refreshCount + 1);
        if (_refreshCostText != null) _refreshCostText.text = $"{cost}";

        int gold = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
        if (_refreshBtn != null) _refreshBtn.interactable = gold >= cost;
    }

    // ── 구매 처리 ─────────────────────────────────────────────

    void OnBuyEquip(EquipmentData data, int cost)
    {
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || items.Get(eItem.Gold) < cost) return;

        items.Add(eItem.Gold, -cost);
        UserDataManager.Instance?.Get<EquipInventoryData>()?.Add(data.EquipmentId);
        UserDataManager.Instance?.RequestSave();
        RefreshRefreshBtn();
    }

    void OnBuyTrait(TraitData data, int cost)
    {
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || items.Get(eItem.Gold) < cost) return;

        items.Add(eItem.Gold, -cost);
        UserDataManager.Instance?.Get<RunTraitData>()?.AddTrait(data.TraitType);
        UserDataManager.Instance?.RequestSave();
        RefreshTraits();
        RefreshRefreshBtn();
    }

    void OnHireGeneral(UnitEntry entry, int cost)
    {
        var items  = UserDataManager.Instance?.Get<ItemData>();
        var deploy = UserDataManager.Instance?.Get<DeploymentData>();
        var units  = UserDataManager.Instance?.Get<UnitData>();
        if (items == null || deploy == null || items.Get(eItem.Gold) < cost) return;

        int slot = -1;
        for (int i = 0; i < 5; i++)
        {
            if (string.IsNullOrEmpty(deploy.GetUnitAt(i))) { slot = i; break; }
        }
        if (slot < 0) return;

        items.Add(eItem.Gold, -cost);
        if (!units.HasUnit(entry.UnitName))
            units.AddUnit(new UnitEntry { UnitName = entry.UnitName, Level = 1, GradeUpCount = entry.GradeUpCount });
        deploy.Deploy(entry.UnitName, slot);
        UserDataManager.Instance?.RequestSave();
        RefreshGenerals();
        RefreshRefreshBtn();
    }

    void OnRefresh()
    {
        int cost  = RefreshBaseCost * (_refreshCount + 1);
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null || items.Get(eItem.Gold) < cost) return;

        items.Add(eItem.Gold, -cost);
        _refreshCount++;
        GenerateShop();
        UserDataManager.Instance?.RequestSave();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static int CalcEquipCost(EquipmentData data) => data.Grade switch
    {
        UnitGrade.Normal   => 200,
        UnitGrade.Uncommon => 350,
        UnitGrade.Rare     => 550,
        UnitGrade.Unique   => 800,
        UnitGrade.Epic     => 1200,
        _                  => 300,
    };

    static void Shuffle<T>(List<T> list)
    {
        var rng = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// 슬롯 컴포넌트는 각 별도 파일로 분리:
//   RunShopEquipSlot.cs / RunShopTraitSlot.cs / RunShopGeneralSlot.cs
