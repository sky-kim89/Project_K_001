using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  FirstRelicTutorial.cs
//  첫 환생 직후 메인 화면 — 유물 강화를 한 번 거치게 만든다.
//
//  ■ 왜 여기인가
//    앱 첫 실행은 메인 화면을 건너뛰고 곧장 전투로 들어간다. 유물 화면을
//    처음 만나는 순간은 '첫 환생 뒤 메인 화면으로 돌아온 지금' 뿐이다.
//    그리고 이때가 처음으로 쓸 포인트를 손에 쥔 시점이다 —
//    설명과 사용이 같은 자리에서 붙어야 기억에 남는다.
//
//  ■ 유물 버튼은 직접 누르게 한다 (ClickTarget)
//    이 화면에서 유물을 안 열면 다음 여정은 지난번과 똑같은 세기로 시작한다.
//    "저기 있습니다" 로 끝내면 대부분 그냥 출전을 누른다 — 여정을 한 번 더
//    통째로 날린 뒤에야 유물 화면을 열어 본다.
//
//  ⚠ 강화까지 강요하지는 않는다
//    어느 유물을 올릴지는 취향이고, 포인트가 1pt 뿐일 수도 있다.
//    카드를 가리켜 "여기서 올린다" 까지만 하고 선택은 남겨 둔다.
//
//  ⚠ 전투 시작(출전) 버튼은 가리키지 않는다
//    LobbyTutorial 과 같은 이유다 — 되돌릴 수 없는 행동은 등을 떠밀지 않는다.
//
//  트리거: TutorialManager.HandleMainPanelShown (환생 1회 이상 + 아직 안 봤을 때)
// ============================================================

public class FirstRelicTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.FirstRelic;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(WaitForPanel);
        steps.Add(Welcome);
        steps.Add(OpenRelic);
        steps.Add(PointPoints);
        steps.Add(PointCard);
        steps.Add(Closing);
    }

    /// <summary>
    /// 메인 화면이 실제로 떠 있을 때까지 기다린다.
    ///
    /// ⚠ 환생 처리가 끝나는 것만으로는 부족하다
    ///   팝업이 닫히고 로비가 메인 패널을 다시 세우는 사이에 말을 걸면
    ///   아직 없는 유물 버튼을 가리키게 된다.
    /// </summary>
    IEnumerator WaitForPanel()
    {
        yield return WaitUntilFree(() => RelicBtn() != null, timeout: 30f);
        yield return WaitSeconds(0.4f, dim: false);
    }

    IEnumerator Welcome()
    {
        yield return Show(TutorialStep.Say(
            "여정이 끝나고 <b>환생</b>했습니다.\n" +
            "장수·장비·특성은 사라졌지만, 그 대가로 <b>환생 포인트</b>를 받았습니다.\n" +
            "이 포인트로 다음 여정의 출발선을 끌어올립니다."));
    }

    IEnumerator OpenRelic()
    {
        yield return Show(TutorialStep.Point(
                RelicBtn,
                "<b>유물</b>을 열어 보겠습니다.\n" +
                "여기서 쓰지 않으면 다음 여정도 방금과 같은 세기로 시작합니다.",
                TutorialAnchor.Above)
            .ClickTarget()
            .Until(() => PopupManager.Instance != null &&
                         PopupManager.Instance.IsOpen(PopupType.Relic)));

        yield return WaitForPopup(PopupType.Relic, timeout: 10f);
        yield return WaitSeconds(0.3f, dim: false);
    }

    IEnumerator PointPoints()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.Relic, "PointGroup"),
            "지금 가진 <b>환생 포인트</b>입니다.\n" +
            "더 멀리 간 여정일수록, 더 높은 난이도일수록 많이 받습니다.",
            TutorialAnchor.Below));
    }

    IEnumerator PointCard()
    {
        yield return Show(TutorialStep.Point(
            UpgradeBtn,
            "유물마다 이 버튼으로 레벨을 올립니다.\n" +
            "유물은 환생해도 사라지지 않습니다 — 여기 쌓은 만큼이 영구 성장입니다.",
            TutorialAnchor.Above));
    }

    IEnumerator Closing()
    {
        yield return Show(TutorialStep.Say(
            "포인트를 다 쓰고 나면 창을 닫고 다시 출전하세요.\n" +
            "이번에는 지난 여정보다 앞에서 출발합니다."));
    }

    // ── 타겟 ─────────────────────────────────────────────────
    //  MainPanelCreator 가 만드는 버튼 이름이다 (BuildWideBtn "RelicBtn").
    //
    //  ⚠ 강화 버튼은 ByName 으로 찾는다 (InPopup 이 아니라)
    //    카드 템플릿이 팝업 안에 꺼진 채로 들어 있어 InPopup 의 깊이 탐색이
    //    그 템플릿을 먼저 집을 수 있다. ByName 은 꺼진 오브젝트를 제외하므로
    //    실제로 화면에 깔린 카드의 버튼이 잡힌다.
    static readonly Func<RectTransform> RelicBtn   = ByName("RelicBtn");
    static readonly Func<RectTransform> UpgradeBtn = ByName("UpgradeBtn");
}
