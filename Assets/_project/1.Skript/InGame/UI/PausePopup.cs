using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  PausePopup.cs
//  일시 정지 팝업.
//
//  열릴 때: Time.timeScale = 0 (게임 정지)
//  닫힐 때: Time.timeScale 원복 (PopupBase 는 unscaledDeltaTime 사용)
//
//  버튼 동작:
//    계속하기 → Close() (timeScale 복원)
//    다시 시작 → 미구현
//    종료     → 미구현
//
//  Hierarchy 예시:
//    PausePopup (PopupBase, CanvasGroup)
//      ├ BgPanel        (Image)
//      ├ TitleText      (TMP)
//      ├ ResumeButton   (Button — _resumeButton)
//      ├ RestartButton  (Button — _restartButton)
//      └ QuitButton     (Button — _quitButton)
// ============================================================

public class PausePopup : PopupBase
{
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _restartButton;  // 미구현
    [SerializeField] Button _quitButton;     // 미구현

    float _prevTimeScale = 1f;

    protected override void OnBeforeOpen()
    {
        // 현재 배속 저장 후 정지
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        _resumeButton? .onClick.AddListener(() => Close());
        _restartButton?.onClick.AddListener(OnRestartClicked);
        _quitButton?   .onClick.AddListener(OnQuitClicked);
    }

    protected override void OnAfterClose()
    {
        // 이전 배속 복원 (1× / 2× / 3×)
        Time.timeScale = _prevTimeScale;
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    void OnRestartClicked()
    {
        // TODO: 씬 재시작 또는 BattleManager 리셋
        Debug.Log("[PausePopup] 다시 시작 — 미구현");
    }

    void OnQuitClicked()
    {
        // TODO: 로비 씬 전환 또는 Application.Quit
        Debug.Log("[PausePopup] 종료 — 미구현");
    }
}
