using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  PausePopup.cs
//  일시 정지 팝업.
//
//  열릴 때: Time.timeScale = 0 (게임 정지)
//  닫힐 때: Time.timeScale 원복 (PopupBase 는 unscaledDeltaTime 사용)
//
//  ■ 선택지는 둘뿐이다
//    계속하기     → Close() (배속 복원)
//    즉시 환생하기 → BattleManager.Surrender() → 아군 전멸과 같은 경로로 패배 처리
//                   → InGameManager.HandleDefeat() 가 ReincarnationPopup 을 연다
//
//    ⚠ 예전엔 "다시 시작"·"종료" 가 있었지만 둘 다 Debug.Log 만 찍는 빈 껍데기였다.
//      런 도중 나갈 길은 환생 하나뿐이라 선택지를 그대로 둘 이유가 없다.
//
//  ⚠ Surrender() 전에 Time.timeScale 을 되돌려야 한다
//    timeScale=0 인 채로 패배 팝업이 뜨면 그 팝업의 애니메이션·버튼이 멈춘 것처럼 보인다.
//    (PopupBase 는 unscaledDeltaTime 을 쓰지만 그 뒤의 전투 정리 코루틴은 아니다)
//
//  Hierarchy — PopupPrefabCreator.CreatePausePopup() 이 만든다.
// ============================================================

public class PausePopup : PopupBase
{
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _reincarnateButton;

    float _prevTimeScale = 1f;
    bool  _surrendering;

    protected override void OnBeforeOpen()
    {
        // 현재 배속 저장 후 정지
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _surrendering  = false;

        // 팝업은 풀에서 재사용되므로 매번 정리하고 다시 붙인다
        _resumeButton?.onClick.RemoveAllListeners();
        _resumeButton?.onClick.AddListener(() => Close());

        _reincarnateButton?.onClick.RemoveAllListeners();
        _reincarnateButton?.onClick.AddListener(OnReincarnateClicked);
    }

    protected override void OnAfterClose()
    {
        // 배속 복원 — 포기했으면 1× 로 되돌린다 (환생 팝업·로비를 배속으로 볼 이유가 없다)
        Time.timeScale = _surrendering ? 1f : _prevTimeScale;

        if (_surrendering)
            BattleManager.Instance?.Surrender();
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────

    /// <summary>
    /// 즉시 환생 — 이 팝업이 완전히 닫힌 **뒤에** 패배를 처리한다.
    /// 여기서 바로 Surrender() 를 부르면 환생 팝업이 열리는 도중에
    /// 이 팝업의 닫기 애니메이션이 겹쳐 두 팝업이 동시에 보인다.
    /// </summary>
    void OnReincarnateClicked()
    {
        _surrendering = true;
        Close();
    }
}
