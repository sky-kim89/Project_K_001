using System;
using UnityEngine;

// ============================================================
//  ReincarnationData.cs
//  환생 포인트 + 어빌리티 새로고침 누적 횟수 영구 저장 섹션.
//
//  ■ 환생 포인트 (ReincarnationPoints)
//    RelicPanelUI 에서 유물 강화/획득 시 차감.
//    즉시 환생(RelicPanelUI.Reincarnate) 시 EarnPoints() 로 적립.
//
//  ■ 어빌리티 새로고침 (UsedRefreshCount)
//    AbilitySelectPopup 에서 새로고침 시 UseRefresh() 로 증가.
//    환생 시 ResetOnReincarnation() 으로 초기화 — 환생 전까지 누적 유지.
//
//  ■ 환생 포인트 획득 공식 (구간별 누적)
//    5~9구간 +1pt/스테이지, 10~14 +2pt, 15~19 +3pt, 20~24 +4pt, 25+ +(stage-20)pt
//    → 스테이지 30 최대: 95pt 누적
//
//  ■ 레벨업 비용 공식 (GameplayConfig.RelicLevelUpCostExponent 사용)
//    기본 지수 2: (currentLevel+1)²  →  0→1: 1pt, 1→2: 4pt, 4→5: 25pt
// ============================================================

[Serializable]
class ReincarnationJson
{
    public int points;
    public int usedRefreshCount;
}

public class ReincarnationData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.Reincarnation;

    public int ReincarnationPoints { get; private set; }
    public int UsedRefreshCount    { get; private set; }

    // 환생 가능 최소 스테이지 — StageConfig 에서 읽어 단일 진실 소스 유지
    public static int ReincarnateMinStage => StageConfig.Current?.ReincarnateMinStage ?? 5;

    // ── 환생 포인트 공식 ──────────────────────────────────────

    /// <summary>
    /// 클리어한 일반 스테이지 수에 따른 환생 포인트 (누적).
    /// 구간별 획득량: 5~9구간 +1pt, 10~14 +2pt, 15~19 +3pt, 20~24 +4pt, 25+ +(stage-20)pt.
    /// 예) st30 = 1×5 + 2×5 + 3×5 + 4×5 + 5+6+7+8+9+10 = 5+10+15+20+45 = 95pt.
    /// </summary>
    public static int CalculateReincarnationPoints(int clearedNormalStage)
    {
        if (clearedNormalStage < ReincarnateMinStage) return 0;
        int total = 0;
        for (int s = ReincarnateMinStage; s <= clearedNormalStage; s++)
            total += StagePointIncrement(s);
        return total;
    }

    static int StagePointIncrement(int stage)
    {
        if (stage < 10) return 1;
        if (stage < 15) return 2;
        if (stage < 20) return 3;
        if (stage < 25) return 4;
        return stage - 20;
    }

    public static bool CanReincarnate(int clearedNormalStage)
        => clearedNormalStage >= ReincarnateMinStage;

    // ── 레벨업 비용 ───────────────────────────────────────────

    /// <summary>
    /// 현재 레벨 → 다음 레벨 강화 비용.
    /// 공식: (currentLevel+1)^GameplayConfig.RelicLevelUpCostExponent (기본 지수 2).
    /// 0→1: 1pt, 1→2: 4pt, 2→3: 9pt, 4→5: 25pt.
    /// </summary>
    public static int LevelUpCost(int currentLevel)
    {
        float exp = GameplayConfig.Current != null
            ? GameplayConfig.Current.RelicLevelUpCostExponent
            : 2f;
        return Mathf.Max(1, Mathf.RoundToInt(Mathf.Pow(currentLevel + 1, exp)));
    }

    // ⚠ AcquireCost(희귀도별 첫 획득 비용) 는 삭제됐다.
    //   유물에 "획득" 단계는 없다 — 모든 유물이 0레벨로 존재하고 전부 강화일 뿐이다.
    //   비용은 LevelUpCost(현재레벨) 하나로 끝난다.

    // ── 포인트 조작 ───────────────────────────────────────────

    /// <summary>포인트가 충분하면 차감하고 true 반환, 아니면 false.</summary>
    public bool TrySpendPoints(int cost)
    {
        if (cost <= 0 || ReincarnationPoints < cost) return false;
        ReincarnationPoints -= cost;
        return true;
    }

    /// <summary>스테이지 기반 포인트 자동 계산 후 적립.</summary>
    public void EarnPointsByStage(int clearedNormalStage)
    {
        // 난이도 배율 — 올릴 이유가 없으면 아무도 안 올린다.
        float mul = DifficultyConfig.CurrentTier()?.ReincarnationMultiplier ?? 1f;
        EarnPoints(Mathf.RoundToInt(
            CalculateReincarnationPoints(clearedNormalStage) * Mathf.Max(1f, mul)));
    }

    public void EarnPoints(int amount)
        => ReincarnationPoints += Mathf.Max(0, amount);

    // ── 어빌리티 새로고침 ─────────────────────────────────────

    public void UseRefresh() => UsedRefreshCount++;

    /// <summary>환생 시 새로고침 횟수 초기화.</summary>
    public void ResetOnReincarnation() => UsedRefreshCount = 0;

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()
        => JsonUtility.ToJson(new ReincarnationJson
        {
            points           = ReincarnationPoints,
            usedRefreshCount = UsedRefreshCount,
        });

    public void Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<ReincarnationJson>(json);
        if (dto == null) return;
        ReincarnationPoints = dto.points;
        UsedRefreshCount    = dto.usedRefreshCount;
    }

    public void SetDefaults()
    {
        ReincarnationPoints = 0;
        UsedRefreshCount    = 0;
    }
}
