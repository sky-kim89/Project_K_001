using UnityEngine;

// ============================================================
//  UIConstants.cs
//  등급·직업 색상과 레이블을 한곳에 모은 공유 상수.
//  UI 파일 어디서든 GradeStyle / JobStyle 로 접근.
// ============================================================

public static class GradeStyle
{
    // ── 색상 ──────────────────────────────────────────────────
    public static Color GetColor(UnitGrade grade) => grade switch
    {
        UnitGrade.Normal   => new Color(0.55f, 0.55f, 0.55f),
        UnitGrade.Uncommon => new Color(0.25f, 0.80f, 0.35f),
        UnitGrade.Rare     => new Color(0.20f, 0.50f, 1.00f),
        UnitGrade.Unique   => new Color(0.70f, 0.30f, 1.00f),
        UnitGrade.Epic     => new Color(1.00f, 0.60f, 0.10f),
        _                  => Color.white,
    };

    // ── 레이블 ────────────────────────────────────────────────
    public static string GetLabel(UnitGrade grade) => grade switch
    {
        UnitGrade.Normal   => "일반",
        UnitGrade.Uncommon => "비범",
        UnitGrade.Rare     => "희귀",
        UnitGrade.Unique   => "고유",
        UnitGrade.Epic     => "영웅",
        _                  => "?",
    };
}

public static class JobStyle
{
    public static string GetLabel(UnitJob job) => job switch
    {
        UnitJob.Knight       => "기사",
        UnitJob.Archer       => "궁수",
        UnitJob.Mage         => "마법사",
        UnitJob.ShieldBearer => "방패병",
        _                    => "?",
    };
}
