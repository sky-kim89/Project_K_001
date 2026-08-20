using System;
using System.Collections;
using System.Collections.Generic;

// ============================================================
//  HelpTutorials.cs
//  팝업별 도움말 — 헤더의 'i' 버튼으로만 열린다 (강제로 안 뜬다).
//
//  ■ 강제 진행과 규칙이 다르다
//    · 조건 대기가 없다 — 이미 그 팝업이 열려 있을 때만 눌리는 버튼이다
//    · 클릭 유도가 없다 — 도움말을 보러 온 사람에게 조작을 시키지 않는다
//
//  ■ 화면에 있는 것을 짚는다
//    "이 화면이 무엇인가" 만 말하면 이미 열어 본 사람에게는 새 정보가 없다.
//    도움말을 누르는 순간은 대개 "이 버튼이 뭘 하는지 모르겠다" 는 순간이므로,
//    그 화면에만 있는 기능(일괄 분해·중복 표시·교체 소멸 등)을 실제 위치를
//    가리키며 설명한다.
//
//  ■ 되돌릴 수 없는 것은 반드시 경고한다
//    장비 교체(기존 소멸)·분해·이벤트 선택은 취소가 없다.
//    도움말에서 안 짚으면 처음 겪는 사람은 잃고 나서야 안다.
//
//  ■ 한 파일에 모아 둔 이유
//    각자 서너 스텝뿐이라 파일을 쪼개면 찾기만 번거로워진다.
//    스텝이 길어지는 것이 생기면 그때 따로 뺀다 (InGameTutorial 처럼).
//
//  ⚠ 타겟을 가리킬 땐 InPopup 으로 그 팝업 안에서 찾는다
//    ByName 은 씬 전체를 훑어 같은 이름의 다른 UI 를 잡을 수 있다.
//    로비 패널(유물·난이도)은 팝업이 아니라 ByName 을 쓴다.
//
//  ⚠ 타겟 이름은 Creator 가 만드는 이름과 같아야 한다
//    틀리면 하이라이트만 사라지고 말풍선은 화면 중앙에 뜬다 — 튜토리얼이
//    멈추지는 않으므로, 이름을 바꿀 때 여기도 같이 고쳐야 조용히 어긋나지 않는다.
// ============================================================

// ── 유물 · 환생 ─────────────────────────────────────────────

public class RelicHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpRelic;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Points);
        steps.Add(Cost);
        steps.Add(Reincarnate);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "<b>유물</b>은 환생해도 사라지지 않는 영구 성장입니다.\n" +
            "장수·장비·특성이 전부 초기화돼도 여기 쌓인 것은 남습니다.\n" +
            "여정을 거듭할수록 출발선이 앞당겨지는 부분이 이곳입니다."));
    }

    IEnumerator Points()
    {
        yield return Show(TutorialStep.Point(
            ByName("PointGroup"),
            "강화에 쓰는 <b>환생 포인트</b>입니다.\n" +
            "환생할 때 도달한 스테이지와 난이도에 따라 받습니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Cost()
    {
        yield return Show(TutorialStep.Say(
            "레벨이 오를수록 다음 레벨 비용이 가파르게 오릅니다.\n" +
            "여럿을 낮게 펼칠지, 하나를 끝까지 올릴지 고르셔야 합니다.\n" +
            "출전 슬롯처럼 <b>부대 자체를 늘리는 유물</b>은 먼저 봐 두는 편이 낫습니다."));
    }

    IEnumerator Reincarnate()
    {
        yield return Show(TutorialStep.Say(
            "환생하면 이번 여정의 장수·장비·특성·어빌리티가 모두 사라지고\n" +
            "그 대가로 포인트를 받습니다.\n" +
            "더 나아갈 수 없을 때 다시 시작하는 수단입니다."));
    }
}

// ── 어빌리티 ────────────────────────────────────────────────

public class AbilityHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpAbility;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(OwnedList);
        steps.Add(Stacking);
        steps.Add(Detail);
        steps.Add(TotalStats);
        steps.Add(Targets);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "<b>어빌리티</b>는 이번 여정 동안만 유지되는 강화입니다.\n" +
            "전투에서 이길 때마다 셋 중 하나를 골라 쌓아 갑니다.\n" +
            "환생하면 전부 사라집니다 — 유물과 다른 점입니다."));
    }

    IEnumerator OwnedList()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.AbilityList, "ListBg"),
            "지금까지 <b>고른 어빌리티 전부</b>가 여기 쌓입니다.\n" +
            "줄을 누르면 왼쪽에 그 어빌리티의 상세가 뜹니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Stacking()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.AbilityList, "ListBg"),
            "같은 어빌리티를 또 고르면 새 줄이 생기지 않고\n" +
            "그 줄에 <b>×2 · ×3</b> 표시가 붙습니다.\n" +
            "겹칠수록 효과도 그만큼 곱해집니다 — 몰아 주는 것이 손해가 아닙니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Detail()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.AbilityList, "DetailCard"),
            "고른 어빌리티의 <b>등급·적용 대상·효과</b>가 여기 나옵니다.\n" +
            "등급이 높을수록 같은 항목이라도 오르는 폭이 큽니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator TotalStats()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.AbilityList, "TotalBg"),
            "아래는 보유한 어빌리티를 <b>전부 합친 값</b>입니다.\n" +
            "한 줄씩 볼 때는 작아 보여도 합치면 얼마나 됐는지 여기서 확인합니다.\n" +
            "다음 선택에서 무엇이 부족한지 정할 때 보시면 됩니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Targets()
    {
        yield return Show(TutorialStep.Say(
            "적용 대상을 꼭 보세요.\n" +
            "<b>전체·직업</b>은 장수와 병사 모두에게,\n" +
            "<b>장군</b>은 장수에게만, <b>병사</b>는 병사에게만 붙습니다.\n" +
            "부대 위력의 대부분이 병사에게서 나오므로 대상이 곧 체감 차이입니다."));
    }
}

// ── 장비 (비교 · 교체) ──────────────────────────────────────

public class EquipmentHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpEquipment;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Compare);
        steps.Add(Inventory);
        steps.Add(ReplaceWarning);
        steps.Add(Enhance);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "<b>장비</b>는 장수에게 끼우는 물건입니다.\n" +
            "등급이 높을수록 붙는 옵션의 수치가 큽니다."));
    }

    IEnumerator Compare()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.EquipCompare, "CardRow"),
            "왼쪽이 <b>지금 낀 장비</b>, 오른쪽이 <b>고른 장비</b>입니다.\n" +
            "수치가 나란히 놓이니 어느 쪽이 나은지 보고 정하시면 됩니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Inventory()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.EquipCompare, "InventoryScroll"),
            "아래는 이 칸에 낄 수 있는 <b>보유 장비</b>입니다.\n" +
            "누를 때마다 위쪽 비교가 그 장비로 바뀝니다.",
            TutorialAnchor.Above));
    }

    IEnumerator ReplaceWarning()
    {
        yield return Show(TutorialStep.Say(
            "<b>교체하면 원래 끼고 있던 장비는 사라집니다.</b>\n" +
            "가방으로 돌아오지 않으니 되돌릴 수 없습니다.\n" +
            "아깝다면 교체 전에 분해해 전투석으로 바꿔 두세요."));
    }

    IEnumerator Enhance()
    {
        yield return Show(TutorialStep.Say(
            "<b>전투석</b>으로 강화하면 수치가 더 오릅니다.\n" +
            "강화 수치는 그 장비에 붙으므로, 교체하면 함께 사라집니다.\n" +
            "오래 쓸 장비를 정하고 나서 올리는 편이 낫습니다."));
    }
}

// ── 상점 ────────────────────────────────────────────────────

public class ShopHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpShop;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Goods);
        steps.Add(Mercenary);
        steps.Add(Refresh);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "골드로 <b>장비·특성·장수</b>를 사는 곳입니다.\n" +
            "상점은 몇 스테이지마다 한 번씩만 열리니\n" +
            "다음 허들 전에 정비한다고 생각하시면 됩니다."));
    }

    IEnumerator Goods()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.RunShop, "GoodsArea"),
            "왼쪽은 <b>장비</b>, 오른쪽은 <b>특성</b>입니다.\n" +
            "특성은 이미 가진 개수가 늘수록 값이 올라가니,\n" +
            "같은 특성을 계속 쌓을지 값이 쌀 때 폭을 넓힐지 고르셔야 합니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Mercenary()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.RunShop, "MercRow"),
            "아래는 <b>새 장수</b>입니다.\n" +
            "장수 하나가 병사까지 통째로 데려오므로\n" +
            "빈 출전 칸이 있다면 장비보다 먼저 보실 만합니다.",
            TutorialAnchor.Above));
    }

    IEnumerator Refresh()
    {
        yield return Show(TutorialStep.Say(
            "마음에 드는 게 없으면 <b>새로고침</b>으로 물건을 다시 뽑습니다.\n" +
            "비용은 100골드에서 시작해 새로 뽑을 때마다 100씩 올라갑니다.\n" +
            "물건은 스테이지를 넘길 때도 새로 채워집니다."));
    }
}

// ── 이벤트 ──────────────────────────────────────────────────

public class EventHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpEvent;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Choice);
        steps.Add(Shop);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "길에서 마주치는 <b>사건</b>입니다.\n" +
            "특성·장수·재화처럼 상점에서 못 사는 것을 얻는 자리이기도 합니다."));
    }

    IEnumerator Choice()
    {
        yield return Show(TutorialStep.Say(
            "선택지마다 얻는 것과 잃는 것이 다릅니다.\n" +
            "<b>고르고 나면 되돌릴 수 없고</b>, 같은 사건이 다시 나와도\n" +
            "결과가 그때 정해지므로 미리 알 수 없습니다."));
    }

    IEnumerator Shop()
    {
        yield return Show(TutorialStep.Say(
            "행상인을 만나면 여기서 <b>상점</b>이 열립니다.\n" +
            "상점 스테이지는 이렇게 이벤트를 거쳐 들어갑니다."));
    }
}

// ── 도감 ────────────────────────────────────────────────────

public class CodexHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpCodex;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Tabs);
        steps.Add(Bonus);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "<b>도감</b>은 지금까지 한 번이라도 얻어 본 것들의 기록입니다.\n" +
            "잃거나 분해해도 기록은 지워지지 않고,\n" +
            "환생해도 그대로 남습니다."));
    }

    IEnumerator Tabs()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.Codex, "TabBar"),
            "장비·특성·어빌리티·장수로 나뉘어 있습니다.\n" +
            "아직 못 얻은 칸은 비어 있으니 무엇이 남았는지 여기서 봅니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Bonus()
    {
        yield return Show(TutorialStep.Say(
            "수집한 <b>종류 1개당 모든 장수의 공격력·체력 +0.5%</b>입니다.\n" +
            "같은 것을 여러 개 모아도 오르지 않고, 새 종류를 채울 때만 오릅니다.\n" +
            "여정을 이어 갈수록 저절로 쌓이는 영구 성장입니다."));
    }
}

// ── 장수 상세 ───────────────────────────────────────────────
//
//  강제 진행의 HeroStatTutorial 과 내용이 겹치지만 일부러 따로 둔다.
//  그쪽은 "눌러서 열어 보세요" 로 조작을 시키고, 이쪽은 이미 열어 둔
//  사람에게 읽을 것만 준다. 같은 시나리오를 재사용하면 도움말을 눌렀는데
//  배치 칸을 누르라고 하는 꼴이 된다.

public class HeroDetailHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpHeroDetail;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(Stats);
        steps.Add(Breakdown);
        steps.Add(SoldierTab);
        steps.Add(Equip);
        steps.Add(Skills);
        steps.Add(Grow);
    }

    IEnumerator Stats()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "StatListContainer"),
            "이 장수의 <b>최종 스탯</b>입니다.\n" +
            "장비·패시브·특성·유물·도감이 전부 반영된 뒤의 값이라\n" +
            "전투에서 쓰이는 수치와 같습니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Breakdown()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "Stat_ATK"),
            "스탯 줄을 누르면 <b>어디서 온 수치인지</b> 펼쳐집니다.\n" +
            "기본값에 무엇이 얼마씩 얹혔는지 출처별로 나뉘어 보이니,\n" +
            "장비를 바꿀지 특성을 더 살지 여기서 판단하시면 됩니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator SoldierTab()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "SoldierTab"),
            "<b>용병</b> 탭은 이 장수가 이끄는 병사의 스탯입니다.\n" +
            "부대 위력의 대부분이 병사에게서 나오므로\n" +
            "장수 숫자만 보고 강해졌다고 판단하면 실제 전투력과 어긋납니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Equip()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "EquipStrip"),
            "장비 칸입니다. 칸을 누르면 교체 화면이 열립니다.\n" +
            "쓸 수 있는 칸 수는 장수 등급이 오를수록 늘어납니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Skills()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.HeroDetail, "SkillColumn"),
            "<b>액티브 스킬</b>은 전투 중 직접 눌러 쓰고,\n" +
            "<b>패시브</b>는 조건이 맞으면 저절로 발동합니다.\n" +
            "패시브 슬롯 수도 등급을 따라 늘어납니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Grow()
    {
        yield return Show(TutorialStep.Say(
            "레벨업은 전투에서 얻는 <b>경험치</b>로,\n" +
            "등급업은 <b>장군 강화석</b>으로 합니다.\n" +
            "둘 다 이번 여정 동안만 유지되며 환생하면 초기화됩니다."));
    }
}

// ── 용병 고용 ───────────────────────────────────────────────

public class MercenaryHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpMercenary;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Candidates);
        steps.Add(Squad);
        steps.Add(Synergy);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "새 <b>장수</b>를 부대에 들이는 곳입니다.\n" +
            "장수 하나가 병사까지 통째로 데려오므로\n" +
            "출전 칸이 비어 있다면 가장 큰 전력 증가입니다."));
    }

    IEnumerator Candidates()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.MercenaryShop, "CardColumn"),
            "고용할 수 있는 <b>후보</b>입니다.\n" +
            "직업과 등급에 따라 값이 다르며,\n" +
            "등급이 높으면 병사 수와 스탯이 함께 올라갑니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Squad()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.MercenaryShop, "SquadRow"),
            "지금 <b>부대에 있는 장수</b>입니다.\n" +
            "칸이 다 찼다면 새로 들이기 전에 누구를 뺄지 정해야 합니다.",
            TutorialAnchor.Above));
    }

    IEnumerator Synergy()
    {
        yield return Show(TutorialStep.Say(
            "직업 조합이 맞으면 <b>시너지 특성</b>이 저절로 붙습니다.\n" +
            "같은 직업만 모으는 것보다 없는 직업을 채우는 쪽이\n" +
            "같은 골드로 더 많이 오르는 경우가 많습니다."));
    }
}

// ── 장비 분해 ───────────────────────────────────────────────

public class DisassembleHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpDisassemble;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Grid);
        steps.Add(Reward);
        steps.Add(GradeFilter);
        steps.Add(BulkButton);
        steps.Add(Safety);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "쓰지 않는 <b>장비</b>를 전투석으로 바꾸는 곳입니다.\n" +
            "전투석은 장비 강화에 쓰는 재화라,\n" +
            "안 쓰는 장비를 쌓아 두는 것보다 녹이는 편이 낫습니다."));
    }

    IEnumerator Grid()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.Disassemble, "GridBg"),
            "분해할 수 있는 <b>보유 장비</b>입니다.\n" +
            "칸을 누르면 왼쪽에 그 장비의 옵션과 돌려받을 양이 나옵니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator Reward()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.Disassemble, "RewardRow"),
            "이 장비를 녹였을 때 <b>돌려받는 전투석</b>입니다.\n" +
            "등급이 높은 장비일수록 많이 나옵니다.",
            TutorialAnchor.Auto));
    }

    IEnumerator GradeFilter()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.Disassemble, "FilterRow"),
            "여기서 <b>등급을 체크</b>하면 그 등급 장비가 한꺼번에 선택됩니다.\n" +
            "여러 등급을 동시에 켤 수도 있습니다.\n" +
            "하나씩 누를 필요 없이 \"일반·고급은 전부 녹인다\" 를 한 번에 정하는 자리입니다.",
            TutorialAnchor.Below));
    }

    IEnumerator BulkButton()
    {
        yield return Show(TutorialStep.Point(
            InPopup(PopupType.Disassemble, "BulkBtn"),
            "<b>선택 등급 일괄 분해</b>를 누르면\n" +
            "체크한 등급의 장비가 한 번에 전부 전투석이 됩니다.\n" +
            "장비가 쌓였을 때 정리하는 가장 빠른 방법입니다.",
            TutorialAnchor.Below));
    }

    IEnumerator Safety()
    {
        yield return Show(TutorialStep.Say(
            "<b>장수가 끼고 있는 장비는 이 목록에 없습니다.</b>\n" +
            "일괄 분해로 실수로 녹일 걱정은 하지 않으셔도 됩니다.\n" +
            "다만 한 번 녹인 장비는 되돌릴 수 없습니다."));
    }
}

// ── 난이도 ──────────────────────────────────────────────────

public class DifficultyHelpTutorial : TutorialScenario
{
    public override TutorialId Id => TutorialId.HelpDifficulty;

    protected override void Build(List<Func<IEnumerator>> steps)
    {
        steps.Add(What);
        steps.Add(Debuff);
        steps.Add(Unlock);
        steps.Add(Locked);
    }

    IEnumerator What()
    {
        yield return Show(TutorialStep.Say(
            "난이도를 올리면 적이 강해지는 대신\n" +
            "환생할 때 받는 <b>환생 포인트</b>가 늘어납니다.\n" +
            "유물을 빨리 모으려면 결국 올려야 하는 값입니다."));
    }

    IEnumerator Debuff()
    {
        yield return Show(TutorialStep.Say(
            "등급마다 붙는 것이 다릅니다.\n" +
            "적이 세지는 <b>광포</b>, 수가 늘어나는 <b>물량</b>,\n" +
            "우두머리가 스킬을 더 자주 쓰는 <b>각성·폭주</b>가 차례로 더해집니다."));
    }

    IEnumerator Unlock()
    {
        yield return Show(TutorialStep.Say(
            "지금 등급으로 <b>20스테이지</b>까지 나아가면\n" +
            "다음 등급이 열립니다. 끝까지 깰 필요는 없습니다."));
    }

    IEnumerator Locked()
    {
        yield return Show(TutorialStep.Say(
            "여정이 시작되면 난이도는 <b>바꿀 수 없습니다.</b>\n" +
            "쉬운 등급으로 앞을 깔고 마지막만 올리는 것을 막기 위해서입니다.\n" +
            "고르는 것은 다음 여정을 시작하기 전입니다."));
    }
}
