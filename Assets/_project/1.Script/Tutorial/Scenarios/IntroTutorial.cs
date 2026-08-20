using System;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  IntroTutorial.cs
//  게임 접속 — 첫 인사. 전투가 시작되기 직전에 딱 한 번 뜬다.
//
//  ■ 짧아야 한다
//    설치하고 처음 켠 사람은 아직 아무것도 안 봤다. 여기서 시스템을
//    설명하면 읽을 근거가 없어 그냥 넘긴다. "무슨 게임인지" 한 마디만
//    하고 바로 전장으로 넘긴다 — 나머지는 InGameTutorial 이 화면을
//    보여주면서 말한다.
//
//  ■ InGameTutorial 과 나눠 둔 이유
//    최초 실행은 로비를 건너뛰고 곧장 전투로 들어가지만(AutoStartFirstRun),
//    나중에 "다시 보기" 를 넣거나 접속 시점 안내를 추가할 때 인게임 설명과
//    수명이 다르다. 화면이 다르면 시나리오도 나눈다.
// ============================================================

public class IntroTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.Intro;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(Greeting);
        steps.Add(WhatIsThis);
    }

    IEnumerator Greeting()
    {
        yield return Show(TutorialStep.Say(
            "환영합니다, 지휘관님.\n" +
            "무너진 전선을 다시 세울 사람은 당신뿐입니다."));
    }

    IEnumerator WhatIsThis()
    {
        yield return Show(TutorialStep.Say(
            "장수를 모아 부대를 꾸리고, 스테이지를 하나씩 밀어냅니다.\n" +
            "먼저 한 판 치러 가시죠."));
    }
}
