using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  HeroStatTutorial.cs
//  장수 상세 팝업 — 스탯 읽는 법. 강제 진행 튜토리얼의 마지막이다.
//
//  ■ 여기서 강제 진행이 끝난다
//    이 시나리오가 완료되면 더는 플레이어를 붙잡지 않는다.
//    유물·어빌리티·도감 같은 나머지는 각 팝업의 i 버튼이 맡는다.
//    그래서 마지막 스텝이 "이제 알아서 하시면 된다" 로 닫힌다.
//
//  ■ 배치 칸을 눌러 팝업을 열게 한다
//    로비 튜토리얼에서 "칸을 누르면 상세가 열린다" 고 말해 뒀다.
//    여기서 실제로 눌러 보게 해 그 말을 확인시킨다.
//
//  ⚠ 장수·용병 탭의 차이가 이 화면의 핵심이다
//    부대 위력의 대부분은 병사에게서 나온다. 장수 숫자만 보고 강해졌다고
//    판단하면 실제 전투력과 어긋나므로, 탭이 있다는 것을 반드시 짚는다.
// ============================================================

public class HeroStatTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HeroStat;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(AskToOpen);
        steps.Add(WaitForPopupOpen);
        steps.Add(PointStatList);
        steps.Add(ExplainStatBreakdown);
        steps.Add(PointSoldierTab);
        steps.Add(PointEquipStrip);
        steps.Add(PointSkills);
        steps.Add(Closing);
    }

    // ── 팝업 열기 ────────────────────────────────────────────

    /// <summary>
    /// ⚠ DeployArea 를 가리키면 안 된다 — 화면 세로 전체를 차지하는 열이다
    ///   구멍이 화면 높이만큼 뚫려 말풍선이 그 옆으로 밀려나고, 정작 눌러야 할
    ///   장수가 어느 칸인지 가리켜지지 않는다. 실제로 서 있는 칸 하나만 판다.
    ///
    /// ⚠ 눌린 것을 Button.onClick 으로 잡지 않는다
    ///   칸이 비었거나 잠겼을 수도 있어 확실하지 않다.
    ///   "장수 상세가 열렸다" 를 완료 조건으로 준다.
    /// </summary>
    IEnumerator AskToOpen()
    {
        yield return Show(TutorialStep.Point(
                FirstOccupiedSlot,
                "배치된 장수를 눌러 보세요.",
                TutorialAnchor.Above)
            .ClickTarget()
            .Until(() => PopupManager.Instance != null
                      && PopupManager.Instance.IsOpen(PopupType.HeroDetail)));
    }

    /// <summary>
    /// 장수가 서 있는 첫 배치 칸. 하나도 없으면 열 전체로 물러선다.
    ///
    /// ⚠ ByName("Slot_0") 을 쓰지 않는다
    ///   0번 칸이 비어 있을 수 있다. 빈 칸은 버튼이 꺼져 있어(interactable=false)
    ///   눌러도 아무 일이 없고, 튜토리얼은 시간 초과까지 멈춰 선다.
    /// </summary>
    static RectTransform FirstOccupiedSlot()
    {
        var area = ByName("DeployArea")();
        if (area == null) return null;

        for (int i = 0; i < area.childCount; i++)
        {
            var child = area.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;

            var slot = child.GetComponent<DeploySlotUI>();
            if (slot != null && slot.IsOccupied) return child as RectTransform;
        }

        return area;   // 배치된 장수가 없다 — 적어도 어디를 보는지는 알려 준다
    }

    IEnumerator WaitForPopupOpen()
    {
        yield return WaitForPopup(PopupType.HeroDetail);
        yield return WaitSeconds(0.35f, dim: false);   // 열기 애니메이션 대기
    }

    // ── 스탯 ─────────────────────────────────────────────────

    IEnumerator PointStatList()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "StatListContainer"),
            "이 장수의 <b>스탯</b>입니다.\n" +
            "체력·공격력부터 치명타까지 전투에 쓰이는 값이 전부 여기 있습니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator ExplainStatBreakdown()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "Stat_ATK"),
            "스탯 줄을 누르면 <b>어디서 온 수치인지</b> 펼쳐집니다.\n" +
            "기본값에 장비·패시브·특성·유물이 얼마씩 얹혔는지 색으로 나뉩니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator PointSoldierTab()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "SoldierTab"),
            "<b>용병</b> 탭은 이 장수가 이끄는 병사의 스탯입니다.\n" +
            "부대 위력의 대부분은 병사에게서 나오니 함께 보셔야 합니다.",
            TutorialAnchor.Below));
    }

    // ── 장비 · 스킬 ──────────────────────────────────────────

    IEnumerator PointEquipStrip()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "EquipStrip"),
            "장비 칸입니다. 상점과 전투 보상으로 얻어 끼우고,\n" +
            "전투석으로 강화하면 수치가 더 오릅니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator PointSkills()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "SkillColumn"),
            "장수가 가진 <b>액티브 스킬</b>과 <b>패시브</b>입니다.\n" +
            "등급이 오르면 패시브 슬롯이 늘어납니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Closing()
    {
        yield return Show(TutorialStep.Say(
            "여기까지입니다.\n" +
            "더 궁금한 화면은 오른쪽 위 <b>i</b> 버튼을 누르면 다시 알려드립니다."));
    }
}
