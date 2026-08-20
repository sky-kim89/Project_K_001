using System;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  LobbyTutorial.cs
//  로비(출전 화면) 설명 — 다음 판에 나가기까지 무엇을 할 수 있는지.
//
//  ■ BattlePanel 이 로비의 본체다
//    로비에는 패널이 여럿 있지만 런 중에 실제로 쓰는 곳은 출전 화면 하나다.
//    여기서 부대를 손보고 상점에 들렀다가 전투로 나간다.
//    나머지 패널(유물·도감 등)은 i 버튼 도움말이 맡는다.
//
//  ⚠ 전투 시작 버튼은 마지막에만 가리킨다
//    먼저 가리키면 설명을 끝까지 안 듣고 눌러 버린다. 부대·상점을 다
//    보여준 뒤에 "이제 나가면 된다" 로 닫는다.
//
//  ⚠ 버튼을 직접 누르게 하지 않는다 (ClickTarget 없음)
//    전투 시작은 되돌릴 수 없는 행동이다. 튜토리얼이 등을 떠밀면
//    아직 둘러보고 싶은 사람도 끌려 나간다. 여기서는 알려만 준다.
// ============================================================

public class LobbyTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.Lobby;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(WaitForPanel);
        steps.Add(Welcome);
        steps.Add(PointDeploy);
        steps.Add(PointStageProgress);
        steps.Add(PointShop);
        steps.Add(PointBattleStart);
    }

    /// <summary>
    /// 출전 화면이 실제로 떠 있을 때까지 기다린다.
    ///
    /// ⚠ 승리 팝업이 닫히는 것만으로는 부족하다
    ///   팝업이 닫힌 뒤 LobbyManager 가 Returning → Preparing → Standby 를
    ///   거치며 부대를 다시 세운다. 그 사이에 말을 걸면 아직 안 그려진
    ///   배치 칸을 가리키게 된다.
    /// </summary>
    IEnumerator WaitForPanel()
    {
        yield return WaitUntilFree(
            () => ByName("DeployArea")() != null && ByName("BattleStartBtn")() != null,
            timeout: 30f);

        yield return WaitSeconds(0.4f, dim: false);
    }

    IEnumerator Welcome()
    {
        yield return Show(TutorialStep.Say(
            "여기가 <b>출전 화면</b>입니다.\n" +
            "다음 전투에 나가기 전에 부대를 손보는 곳이죠."));
    }

    IEnumerator PointDeploy()
    {
        yield return Show(TutorialStep.Point(
            ByName("DeployArea"),
            "출전할 <b>장수</b>를 배치하는 칸입니다.\n" +
            "칸을 누르면 그 장수의 상세 정보가 열립니다.",
            TutorialAnchor.Above));
    }

    IEnumerator PointStageProgress()
    {
        yield return Show(TutorialStep.Point(
            ByName("StageProgressBar"),
            "이번 여정의 <b>진행도</b>입니다.\n" +
            "스테이지는 순서대로만 진행되며, 패배하면 환생으로 넘어갑니다.",
            TutorialAnchor.Below));
    }

    IEnumerator PointShop()
    {
        yield return Show(TutorialStep.Point(
            ByName("ShopBtn"),
            "모은 골드로 장비와 특성, 새 장수를 삽니다.\n" +
            "스테이지를 넘길 때마다 물건이 바뀝니다.",
            TutorialAnchor.Above));
    }

    IEnumerator PointBattleStart()
    {
        yield return Show(TutorialStep.Point(
            ByName("BattleStartBtn"),
            "준비가 되면 여기로 다음 스테이지에 나갑니다.",
            TutorialAnchor.Above));
    }
}
