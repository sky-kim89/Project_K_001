using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  BattleResultPopup.cs
//  전투 결과(승리 / 패배) 팝업.
//
//  보상 카드:
//    - 직접 아이템 (골드·전투석 등): 팝업 열릴 때 즉시 지급 + 카드 표시
//    - 박스 아이템 (장비 박스 등): "?" 카드로 표시, 탭하면 개봉
//
//  Hierarchy 예시:
//    BattleResultPopup (PopupBase, CanvasGroup)
//      ├── BgPanel       (Image — 반투명 배경)
//      ├── ContentPanel  (VerticalLayoutGroup)
//      │     ├── ResultText    (TMP — _resultText)
//      │     ├── SubText       (TMP — _subText)
//      │     ├── StatsText     (TMP — _statsText)
//      │     ├── RewardArea    (HorizontalLayoutGroup — _rewardArea)
//      │     ├── HintText      (TMP — _hintText)
//      │     └── ConfirmButton (Button — _confirmButton)
// ============================================================

public class BattleResultPopup : PopupBase
{
    [SerializeField] TextMeshProUGUI _resultText;
    [SerializeField] TextMeshProUGUI _subText;
    [SerializeField] TextMeshProUGUI _statsText;
    [SerializeField] Transform       _rewardArea;
    [SerializeField] RewardCardUI    _rewardCardPrefab;
    [SerializeField] TextMeshProUGUI _hintText;
    [SerializeField] Button          _confirmButton;

    readonly List<RewardCardUI> _cards = new();
    int _unopenedBoxes;

    // ── PopupBase 훅 ────────────────────────────────────────

    protected override void OnBeforeOpen()
    {
        _confirmButton?.onClick.AddListener(OnConfirm);
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(bool isVictory, BattleContext context, int killCount)
    {
        SetHeader(isVictory);
        SetStats(isVictory, context, killCount);
        BuildRewardCards(isVictory, context);
        RefreshConfirmButton();
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
                var result = RewardOpener.Commit(reward, stageLevel);
                var icon   = SpriteManager.Instance?.GetItem(reward.Item.IconKey());
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
        RefreshConfirmButton();
        if (_hintText != null && _unopenedBoxes <= 0)
            _hintText.gameObject.SetActive(false);
    }

    void RefreshConfirmButton()
    {
        if (_confirmButton != null)
            _confirmButton.interactable = _unopenedBoxes <= 0;
    }

    void OnConfirm()
    {
        Close(() => LobbyManager.Instance?.ReturnToLobby());
    }

    static Color GetItemColor(eItem item) => item switch
    {
        eItem.Gold        => new Color(1.00f, 0.80f, 0.20f, 1f),
        eItem.Gem         => new Color(0.40f, 0.80f, 1.00f, 1f),
        eItem.BattleStone => new Color(0.50f, 0.75f, 0.60f, 1f),
        eItem.Energy      => new Color(0.90f, 0.50f, 0.20f, 1f),
        _                 => new Color(0.60f, 0.60f, 0.70f, 1f),
    };
}
