using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// ============================================================
//  BattleResultPopup.cs
//  전투 결과(승리 / 패배) 팝업.
//
//  보상 카드:
//    - 직접 아이템 (골드·전투석 등): InGameManager 에서 지급 완료 → 카드는 표시만
//    - 박스 아이템 (장비 박스 등): "?" 카드로 표시, 탭하면 개봉
//
//  통계 탭:
//    - 딜 탭: 장군/병사/스킬 피해 세그먼트 바 + 총 피해량
//    - 탱 탭: 받은 피해 / 감소 피해 세그먼트 바 + 받은 피해량
//    - 힐 탭: 가한 힐 바 + 힐량
//
//  한 줄 ExpRow 레이아웃:
//    [Portrait] [Name] [StatBar] [TotalText] [+EXP] [Lv.X] [▲UP!]
//
//  Hierarchy (위 → 아래):
//    BattleResultPopup
//      ├── ResultText / SubText / StatsText
//      ├── RewardArea   (HorizontalLayoutGroup — 클리어 보상 아이콘)
//      ├── HintText     (박스 안내)
//      ├── Divider
//      ├── TabBar       (딜 / 탱 / 힐 버튼)
//      ├── ExpArea      (VerticalLayoutGroup — 영웅별 통계 행)
//      └── ConfirmButton
// ============================================================

public class BattleResultPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [SerializeField] TextMeshProUGUI _resultText;
    [SerializeField] TextMeshProUGUI _subText;
    [SerializeField] TextMeshProUGUI _statsText;
    [SerializeField] Transform       _rewardArea;
    [SerializeField] RewardCardUI    _rewardCardPrefab;
    [SerializeField] TextMeshProUGUI _hintText;
    [SerializeField] Transform       _expArea;
    [SerializeField] ExpRowUI        _expRowPrefab;
    [SerializeField] Button          _confirmButton;

    [Header("통계 탭")]
    [SerializeField] Button[] _tabButtons;    // 0=딜, 1=탱, 2=힐
    [SerializeField] Image[]  _tabButtonBgs;  // 탭 버튼 배경 이미지 (활성/비활성 색 전환)

    readonly List<RewardCardUI> _cards   = new();
    readonly List<ExpRowUI>     _expRows = new();
    int    _unopenedBoxes;
    Action _onConfirmed;

    CombatStatTab _currentTab = CombatStatTab.Damage;
    BattleContext _context;

    static readonly Color TabActiveColor   = new Color(0.25f, 0.45f, 0.85f);
    static readonly Color TabInactiveColor = new Color(0.15f, 0.18f, 0.28f);

    // ── PopupBase 훅 ────────────────────────────────────────

    protected override void OnBeforeOpen()
    {
        _confirmButton?.onClick.RemoveAllListeners();
        _confirmButton?.onClick.AddListener(OnConfirm);

        if (_tabButtons != null)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                _tabButtons[i].onClick.RemoveAllListeners();
                int captured = i;
                _tabButtons[i].onClick.AddListener(() => OnTabClicked((CombatStatTab)captured));
            }
        }
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(bool isVictory, BattleContext context, int killCount, Action onConfirmed = null)
    {
        _onConfirmed = onConfirmed;
        _context     = context;
        _currentTab  = CombatStatTab.Damage;

        SetHeader(isVictory);
        SetStats(isVictory, context, killCount);
        BuildExpRows(context);
        BuildRewardCards(isVictory, context);
        RefreshTabHighlight();
    }

    // ── 탭 전환 ──────────────────────────────────────────────

    void OnTabClicked(CombatStatTab tab)
    {
        if (_currentTab == tab) return;
        _currentTab = tab;
        RefreshTabHighlight();
        RefreshAllRowBars();
    }

    void RefreshTabHighlight()
    {
        if (_tabButtonBgs == null) return;
        for (int i = 0; i < _tabButtonBgs.Length; i++)
        {
            if (_tabButtonBgs[i] == null) continue;
            _tabButtonBgs[i].color = (int)_currentTab == i ? TabActiveColor : TabInactiveColor;
        }
    }

    void RefreshAllRowBars()
    {
        float maxValue = CalcMaxValue(_currentTab);
        foreach (var row in _expRows)
            if (row != null) row.RefreshTab(_currentTab, maxValue);
    }

    float CalcMaxValue(CombatStatTab tab)
    {
        if (_context?.CombatStats == null || _context.CombatStats.Count == 0) return 1f;

        float max = 0f;
        foreach (var s in _context.CombatStats)
        {
            float v = tab switch
            {
                CombatStatTab.Damage => s.TotalDamageDealt,
                CombatStatTab.Tank   => s.DamageTaken + s.SoldierDamageTaken + s.DamageAbsorbed,
                CombatStatTab.Heal   => s.HealingDone,
                _                   => 0f,
            };
            if (v > max) max = v;
        }
        return max > 0f ? max : 1f;
    }

    // ── 내부 ─────────────────────────────────────────────────

    void SetHeader(bool isVictory)
    {
        if (_resultText != null)
        {
            _resultText.text  = isVictory ? "승리!" : "패배";
            _resultText.color = isVictory
                ? new Color(1.00f, 0.85f, 0.10f, 1f)
                : new Color(0.65f, 0.65f, 0.65f, 1f);
        }
        if (_subText != null)
            _subText.text = isVictory ? "모든 적을 물리쳤습니다!" : "아군이 전멸했습니다...";
    }

    void SetStats(bool isVictory, BattleContext context, int killCount)
    {
        if (_statsText == null || context == null) return;
        _statsText.text = $"처치  {killCount}   |   웨이브  {context.CurrentWave} / {context.TotalWaves}";
    }

    void BuildExpRows(BattleContext context)
    {
        foreach (var row in _expRows)
            if (row != null) Destroy(row.gameObject);
        _expRows.Clear();

        if (_expArea == null || _expRowPrefab == null || context == null) return;

        float maxValue = CalcMaxValue(_currentTab);

        foreach (var gain in context.ExpGains)
        {
            var row = Instantiate(_expRowPrefab, _expArea);
            row.Setup(gain);

            // 이름으로 통계 매칭
            var stats = context.CombatStats.Find(s => s.GeneralName == gain.UnitName);
            if (stats != null)
            {
                row.SetStats(stats);
                row.RefreshTab(_currentTab, maxValue);
            }

            _expRows.Add(row);
        }
    }

    void BuildRewardCards(bool isVictory, BattleContext context)
    {
        foreach (var card in _cards)
            if (card != null) Destroy(card.gameObject);
        _cards.Clear();
        _unopenedBoxes = 0;

        if (_hintText != null) _hintText.gameObject.SetActive(false);

        if (!isVictory || context == null || _rewardCardPrefab == null || _rewardArea == null)
            return;

        int stageLevel = context.StageLevel;

        foreach (var reward in context.PendingRewards)
        {
            var card = Instantiate(_rewardCardPrefab, _rewardArea);
            _cards.Add(card);

            if (reward.Item.IsBoxType())
            {
                _unopenedBoxes++;
                card.SetupBox(reward, stageLevel, _ => { }, OnBoxOpened);
            }
            else
            {
                var icon = SpriteManager.Instance?.GetItem(reward.Item.IconKey());
                card.SetupFixed(icon, GetItemColor(reward.Item),
                                reward.Item.DisplayName(),
                                $"+{reward.Amount}");
            }
        }

        if (_hintText != null)
            _hintText.gameObject.SetActive(_unopenedBoxes > 0);
    }

    void OnBoxOpened()
    {
        _unopenedBoxes = Mathf.Max(0, _unopenedBoxes - 1);
        if (_hintText != null && _unopenedBoxes <= 0)
            _hintText.gameObject.SetActive(false);
    }

    void OnConfirm()
    {
        if (_unopenedBoxes > 0)
        {
            foreach (var card in _cards)
                card.TryOpen();
            return;
        }
        Close(_onConfirmed ?? (() => LobbyManager.Instance.ReturnToLobby()));
    }

    static Color GetItemColor(eItem item) => item switch
    {
        eItem.Gold         => new Color(1.00f, 0.80f, 0.20f, 1f),
        eItem.Gem          => new Color(0.40f, 0.80f, 1.00f, 1f),
        eItem.BattleStone  => new Color(0.50f, 0.75f, 0.60f, 1f),
        eItem.Energy       => new Color(0.90f, 0.50f, 0.20f, 1f),
        eItem.SoldierShard => new Color(0.45f, 0.70f, 1.00f, 1f),
        _                  => new Color(0.60f, 0.60f, 0.70f, 1f),
    };
}
