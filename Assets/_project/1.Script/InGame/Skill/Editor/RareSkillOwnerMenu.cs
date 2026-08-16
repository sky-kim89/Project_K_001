using UnityEditor;

// ============================================================
//  RareSkillOwnerMenu.cs  [Editor Only]
//  희귀 스킬 주인 목록을 수동으로 찍는 메뉴.
//
//  로직은 전부 런타임 쪽 RareSkillOwnerLog 에 있다 — 여기는 진입점일 뿐이다.
//  (게임을 실행하면 RareSkillOwnerLog 가 알아서 한 번 찍는다)
// ============================================================

public static class RareSkillOwnerMenu
{
    [MenuItem(ProjectKMenu.Tool + "희귀 스킬 주인 확인", priority = ProjectKMenu.ToolPrio + 2)]
    static void LogOwners() => RareSkillOwnerLog.LogOwners();

    [MenuItem(ProjectKMenu.Tool + "희귀 스킬 주인 확인 (직업별 이름 전체)",
              priority = ProjectKMenu.ToolPrio + 3)]
    static void LogOwnersWithNames()
    {
        RareSkillOwnerLog.LogOwners();
        RareSkillOwnerLog.LogNamesByJob();
    }
}
