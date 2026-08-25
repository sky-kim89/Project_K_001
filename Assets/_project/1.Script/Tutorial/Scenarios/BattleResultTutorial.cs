using System;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  BattleResultTutorial.cs
//  승리 팝업 설명 — 무엇을 받았고 이제 뭘 하면 되는지.
//
//  ■ 팝업이 뜬 뒤에 시작한다
//    이 시나리오는 스스로 팝업을 열지 않는다. InGameManager 가 승리 처리로
//    BattleResultPopup 을 열면 그때 붙는다. 첫 스텝이 팝업을 기다리는 이유다.
//
//  ⚠ 확인 버튼을 튜토리얼이 대신 눌러 주지 않는다
//    마지막 스텝에서 플레이어가 직접 누르게 한다. 자동으로 닫으면
//    "이 버튼을 누르면 다음으로 간다" 를 배우지 못한 채 화면만 바뀐다.
//
//  ⚠ 팝업이 닫히는 것으로 시나리오가 끝난다
//    확인 버튼은 팝업을 닫으므로, 그 뒤에 스텝을 더 두면 사라진 UI 를
//    가리키게 된다. 마무리 인사는 로비 튜토리얼이 이어받는다.
// ============================================================

public class BattleResultTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.BattleResult;

    // 이 시나리오만은 팝업 위가 무대다 — 결과 팝업이 떠 있어도 시작한다.
    public override PopupType StagePopup => PopupType.BattleResult;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(WaitForPopupOpen);
        steps.Add(Congratulate);
        steps.Add(PointRewards);
        steps.Add(PointExp);
        steps.Add(PressConfirm);
    }

    IEnumerator WaitForPopupOpen()
    {
        yield return WaitForPopup(PopupType.BattleResult);

        // 팝업 열기 애니메이션(FadeScale)이 끝난 뒤에 말을 건다.
        // 도중에 띄우면 하이라이트 구멍이 커지는 중인 팝업을 잘못 잡는다.
        yield return WaitSeconds(0.35f, dim: false);
    }

    IEnumerator Congratulate()
    {
        yield return Show(TutorialStep.Say("첫 승리입니다. 전리품을 확인하시죠."));
    }

    IEnumerator PointRewards()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.BattleResult, "RewardArea"),
            "이번 판에서 얻은 <b>보상</b>입니다.\n" +
            "장비와 재화는 다음 스테이지를 준비하는 데 씁니다.",
            TutorialAnchor.Below));
    }

    IEnumerator PointExp()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.BattleResult, "ExpArea"),
            "출전한 장수는 <b>경험치</b>를 받습니다.\n" +
            "레벨이 오르면 체력과 공격력이 함께 올라갑니다.",
            TutorialAnchor.Above));
    }

    IEnumerator PressConfirm()
    {
        yield return Show(TutorialStep.Point(
                InPopup(PopupType.BattleResult, "ConfirmButton"),
                "확인을 누르면 로비로 돌아갑니다.",
                TutorialAnchor.Above)
            .ClickTarget()
            // 팝업이 닫힌 것으로도 넘어간다 — 확인 말고 다른 경로로 닫힐 수 있다.
            .Until(() => PopupManager.Instance == null
                      || !PopupManager.Instance.IsOpen(PopupType.BattleResult)));
    }
}
