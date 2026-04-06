// ============================================================
//  GeneralStatRoller.cs
//  장군 스텟 롤러 — UnitJobRoller 에 위임.
//
//  직업·등급·레벨 보너스는 UnitJobRoller 에서 통합 처리.
//  이 클래스는 호환성 유지를 위한 진입점.
//
//  사용:
//    UnitStat stat = GeneralStatRoller.Roll("Knight_A");
//    UnitStat stat = GeneralStatRoller.Roll("Knight_A", level: 5, grade: UnitGrade.Rare);
// ============================================================

public static class GeneralStatRoller
{
    /// <summary>
    /// unitName 을 시드로 장군 스텟을 생성해 반환한다.
    /// 직업은 시드에서 결정적으로 배정. 레벨·등급 보너스 적용.
    /// </summary>
    public static UnitStat Roll(string unitName, int level = 1, UnitGrade grade = UnitGrade.Normal)
        => UnitJobRoller.Roll(unitName, level, grade);
}
