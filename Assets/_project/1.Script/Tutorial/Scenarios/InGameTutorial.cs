using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  InGameTutorial.cs
//  인게임 화면 하나를 통째로 설명하는 시나리오.
//
//  ■ 세 덩어리가 한 시나리오다
//    ① 전투 설명 — 이 게임은 알아서 싸운다
//    ② 스킬 사용 — 플레이어가 개입할 수 있는 유일한 지점
//    ③ 인게임 UI — 웨이브·AUTO·배속·일시정지 읽는 법
//    같은 화면에서 끊기지 않고 이어지는 이야기라 쪼개지 않는다.
//    쪼개면 중간에 끊겼을 때 이어 볼 지점이 시나리오 경계로 뭉개지고,
//    나중에 i 버튼을 달 때도 "이 화면의 설명" 이 셋으로 흩어진다.
//
//  ■ 스킬 사용이 이 시나리오의 심장이다
//    나머지는 읽고 넘기면 되지만 스킬만은 직접 눌러 봐야 한다.
//    오토배틀에서 플레이어가 손을 대는 곳이 여기 하나뿐이라,
//    이걸 안 눌러 보고 넘어가면 "볼 것만 있는 게임" 으로 오해한다.
//
//  ■ 읽는 동안에는 전투를 멈춘다
//    설명을 읽는 사이에도 전투가 굴러가면, 말풍선을 다 읽었을 때 이미
//    스테이지가 끝나 있다. 실제로 그렇게 됐다.
//    Show 로 띄우는 스텝은 TutorialManager 가 timeScale 을 0 으로 잡는다.
//
//  ⚠ 기다리는 스텝까지 멈추면 안 된다
//    쿨타임(WaitForSkillReady)·웨이브(WaitForBattle)는 시간이 흘러야 차는
//    조건이다. 여기서 멈추면 영원히 안 풀린다 — 대기 헬퍼는 전부 게임을
//    다시 굴린다. 스킬을 실제로 누르는 스텝도 연출을 봐야 하므로 안 멈춘다.
//
//  ■ HUD 이름으로 타겟을 잡는다
//    카드·버튼이 전부 private [SerializeField] 라 밖에서 참조를 못 얻는다.
//    UISetupTool 이 만드는 이름은 고정이다 (TopBar/SpeedButton/SkillSlot/…).
//    이름이 바뀌면 타겟만 null 이 되고 말풍선은 화면 중앙에 뜬다 —
//    튜토리얼이 멈추지는 않는다.
// ============================================================

public class InGameTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.InGame;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        // ── ① 전투 설명 ─────────────────────────────────────
        steps.Add(WaitForBattle);
        steps.Add(ExplainAutoBattle);
        steps.Add(PointGeneralCards);

        // ── ② 스킬 사용 ─────────────────────────────────────
        steps.Add(ExplainSkill);
        steps.Add(WaitForSkillReady);
        steps.Add(UseSkill);
        steps.Add(PraiseSkill);

        // ── ③ 인게임 UI ─────────────────────────────────────
        steps.Add(PointWaveBar);
        steps.Add(PointAutoButton);
        steps.Add(PointSpeedButton);
        steps.Add(PointPauseButton);
        steps.Add(Closing);
    }

    // ══════════════════════════════════════════════════════════
    //  ① 전투 설명
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 웨이브가 실제로 시작될 때까지 기다린다.
    ///
    /// ⚠ 첫 실행은 로비를 거치지 않고 곧장 전투로 들어간다
    ///   (LobbyManager.AutoStartFirstRun). 스폰·카메라 무빙이 끝나기 전에
    ///   말풍선을 띄우면 아직 아무것도 없는 화면을 가리키게 된다.
    /// </summary>
    IEnumerator WaitForBattle()
    {
        yield return WaitUntilFree(
            () => BattleManager.Instance != null && BattleManager.Instance.IsWaveRunning,
            timeout: 60f);

        // 스폰 직후는 장수 카드가 아직 채워지는 중이다. 한 박자 쉬고 시작한다.
        yield return WaitSeconds(0.8f);
    }

    IEnumerator ExplainAutoBattle()
    {
        yield return Show(TutorialStep.Say(
            "전투는 <b>자동으로</b> 진행됩니다.\n" +
            "장수와 병사가 알아서 적을 찾아 싸웁니다."));
    }

    IEnumerator PointGeneralCards()
    {
        yield return Show(TutorialStep.Point(
            ByName("GeneralPanelContainer"),
            "출전한 장수입니다.\n체력과 병사 수를 여기서 볼 수 있습니다.",
            TutorialAnchor.Above));
    }

    // ══════════════════════════════════════════════════════════
    //  ② 스킬 사용
    // ══════════════════════════════════════════════════════════

    IEnumerator ExplainSkill()
    {
        yield return Show(TutorialStep.Point(
            FirstSkillSlot,
            "장수마다 <b>스킬</b>이 하나씩 있습니다.\n" +
            "쿨타임이 차면 직접 눌러 쓸 수 있습니다.",
            TutorialAnchor.Above));
    }

    /// <summary>
    /// 쿨타임이 찰 때까지 기다린다.
    ///
    /// ⚠ 이 대기가 없으면 다음 스텝에서 멈춘다
    ///   쿨타임 중에는 SkillSlotUI 가 버튼을 잠근다(SetUsable). 그 상태로
    ///   "눌러 보세요" 를 띄우면 눌리지 않는 버튼을 가리키게 되고,
    ///   플레이어는 튜토리얼이 고장 났다고 생각한다.
    ///
    /// ⚠ 어둡게 덮지 않는다 (dim: false)
    ///   쿨타임은 몇 초씩 걸린다. 그동안 화면을 깔아 두면 설명도 없이
    ///   어두운 화면만 남고, 정작 봐야 할 전투가 가려진다.
    ///   입력은 계속 막아 다른 UI 로 새지 않게 한다.
    /// </summary>
    IEnumerator WaitForSkillReady()
    {
        yield return WaitUntil(IsAnySkillUsable, timeout: 45f, dim: false);
    }

    /// <summary>
    /// 실제로 눌러 보게 한다.
    ///
    /// ⚠ 이 스텝만 게임을 멈추지 않는다 (KeepRunning)
    ///   멈춘 채로 누르면 스킬 연출 코루틴이 그 자리에 얼어붙어, 눌렀는데
    ///   아무 일도 안 일어난 것처럼 보인다. 스킬은 터지는 걸 봐야 배운다.
    /// </summary>
    IEnumerator UseSkill()
    {
        yield return Show(TutorialStep.Point(
                FirstSkillSlot,
                "지금 눌러 보세요.",
                TutorialAnchor.Above)
            .ClickTarget()
            .KeepRunning()
            .Pad(16f));
    }

    IEnumerator PraiseSkill()
    {
        // 스킬 연출이 화면을 채우는 동안은 말을 걸지 않는다.
        // 보여주려고 기다리는 시간이므로 어둡게 덮지 않는다 — 덮으면
        // 방금 누른 스킬이 무엇을 했는지 못 본 채로 지나간다.
        yield return WaitSeconds(1.2f, dim: false);
        yield return Show(TutorialStep.Say(
            "좋습니다.\n스킬은 쿨타임마다 다시 쓸 수 있습니다."));
    }

    // ══════════════════════════════════════════════════════════
    //  ③ 인게임 UI
    // ══════════════════════════════════════════════════════════

    IEnumerator PointWaveBar()
    {
        yield return Show(TutorialStep.Point(
            ByName("WaveProgressBg"),
            "적은 여러 <b>웨이브</b>로 나뉘어 몰려옵니다.\n" +
            "마지막 웨이브의 보스까지 쓰러뜨리면 승리합니다.",
            TutorialAnchor.Below));
    }

    /// <summary>
    /// AUTO 토글 설명.
    ///
    /// 스킬을 직접 눌러 본 바로 다음에 놓는다 — "직접 누른다" 를 겪은 뒤라야
    /// "안 누르고 맡긴다" 가 무슨 말인지 통한다.
    /// ⚠ 기본값은 꺼짐이다 (BattleSettingsData.RawData.AutoSkill = false).
    ///   "켜져 있습니다" 라고 쓰면 안 된다.
    /// </summary>
    IEnumerator PointAutoButton()
    {
        yield return Show(TutorialStep.Point(
            ByName("AutoButton"),
            "<b>AUTO</b> 를 켜면 스킬도 알아서 사용합니다.\n" +
            "직접 골라 쓰고 싶을 때는 꺼 두세요.",
            TutorialAnchor.Below));
    }

    IEnumerator PointSpeedButton()
    {
        yield return Show(TutorialStep.Point(
            ByName("SpeedButton"),
            "전투가 느리게 느껴지면 <b>배속</b>을 올리세요.",
            TutorialAnchor.Below));
    }

    IEnumerator PointPauseButton()
    {
        yield return Show(TutorialStep.Point(
            ByName("PauseButton"),
            "잠시 멈추거나 전투를 포기하려면 여기를 누릅니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Closing()
    {
        yield return Show(TutorialStep.Say("이제 맡기고 지켜보시면 됩니다."));
    }

    // ══════════════════════════════════════════════════════════
    //  타겟 · 조건
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 첫 번째 장수 카드의 스킬 슬롯.
    ///
    /// ⚠ ByName("SkillSlot") 을 쓰지 않는다
    ///   장수가 여럿이면 같은 이름이 5개 나오고, 그중 무엇이 잡힐지는
    ///   씬 순회 순서에 달렸다. 컨테이너의 첫 자식부터 훑어 순서를 고정한다.
    /// </summary>
    static RectTransform FirstSkillSlot()
    {
        var slot = FindFirstSkillSlot();
        return slot != null ? slot.transform as RectTransform : null;
    }

    static SkillSlotUI FindFirstSkillSlot()
    {
        var container = FindContainer();
        if (container == null) return null;

        for (int i = 0; i < container.childCount; i++)
        {
            var card = container.GetChild(i);
            if (!card.gameObject.activeInHierarchy) continue;
            var slot = card.GetComponentInChildren<SkillSlotUI>(includeInactive: false);
            if (slot != null) return slot;
        }
        return null;
    }

    static Transform FindContainer()
    {
        var rt = ByName("GeneralPanelContainer")();
        return rt != null ? rt.transform : null;
    }

    /// <summary>
    /// 지금 누를 수 있는 스킬이 하나라도 있는가.
    /// SkillSlotUI.SetUsable 이 Button.interactable 을 켜고 끄므로 그것만 본다 —
    /// 쿨타임·사거리 판정을 여기서 다시 구현하면 두 곳이 반드시 갈라진다.
    /// </summary>
    static bool IsAnySkillUsable()
    {
        var slot = FindFirstSkillSlot();
        if (slot == null) return false;
        var btn = slot.GetComponentInChildren<Button>(includeInactive: false);
        return btn != null && btn.interactable;
    }
}
