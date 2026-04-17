using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  BattleResultPopup.cs
//  전투 결과(승리 / 패배) 팝업.
//
//  InGameManager 가 BattleManager.OnVictory / OnDefeat 이벤트에서
//  Open<BattleResultPopup>() 후 Setup() 을 호출해 내용을 설정한다.
//
//  Hierarchy 예시:
//    BattleResultPopup (PopupBase, CanvasGroup)
//      ├ BgPanel       (Image)
//      ├ ResultText    (TMP — _resultText)
//      ├ SubText       (TMP — _subText)
//      ├ StatsText     (TMP — _statsText)
//      └ ConfirmButton (Button — _confirmButton)
// ============================================================

public class BattleResultPopup : PopupBase
{
    [SerializeField] TextMeshProUGUI _resultText;
    [SerializeField] TextMeshProUGUI _subText;
    [SerializeField] TextMeshProUGUI _statsText;
    [SerializeField] Button          _confirmButton;

    protected override void OnBeforeOpen()
    {
        _confirmButton?.onClick.AddListener(() =>
            Close(() => { if (LobbyManager.Instance != null) LobbyManager.Instance.ReturnToLobby(); }));
    }

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// Open() 직후 호출해 승패·통계를 설정한다.
    /// isVictory : true = 승리, false = 패배.
    /// context   : BattleManager.Instance.Context.
    /// killCount : 처치한 적 수.
    /// </summary>
    public void Setup(bool isVictory, BattleContext context, int killCount)
    {
        if (_resultText != null)
        {
            _resultText.text  = isVictory ? "승리!" : "패배";
            _resultText.color = isVictory
                ? new Color(1.00f, 0.85f, 0.10f, 1f)   // 금색
                : new Color(0.65f, 0.65f, 0.65f, 1f);  // 회색
        }

        if (_subText != null)
            _subText.text = isVictory
                ? "모든 적을 물리쳤습니다!"
                : "아군이 전멸했습니다...";

        if (_statsText != null && context != null)
            _statsText.text = $"처치  {killCount}\n웨이브  {context.CurrentWave} / {context.TotalWaves}";
    }
}
