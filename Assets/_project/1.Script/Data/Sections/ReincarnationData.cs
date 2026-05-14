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
//  ■ 레벨업 비용 공식
//    LevelUpCost(currentLevel) = (currentLevel + 1)²
//    0→1 : 1pt, 1→2 : 4pt, 2→3 : 9pt, 3→4 : 16pt, 4→5 : 25pt
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

    // 환생 가능 최소 스테이지
    public const int ReincarnateMinStage = 5;

    // ── 환생 포인트 공식 ──────────────────────────────────────

    /// <summary>
    /// 클리어한 일반 스테이지 수에 따른 환생 포인트.
    /// stage² — 스테이지 5=25pt, 10=100pt, 20=400pt.
    /// 스테이지 5 미만이면 0.
    /// </summary>
    public static int CalculateReincarnationPoints(int clearedNormalStage)
        => clearedNormalStage < ReincarnateMinStage ? 0
           : clearedNormalStage * clearedNormalStage;

    public static bool CanReincarnate(int clearedNormalStage)
        => clearedNormalStage >= ReincarnateMinStage;

    // ── 레벨업 비용 ───────────────────────────────────────────

    /// <summary>현재 레벨 → 다음 레벨 강화 비용. 0→1 : 1pt, N→N+1 : (N+1)²pt.</summary>
    public static int LevelUpCost(int currentLevel)
        => (currentLevel + 1) * (currentLevel + 1);

    /// <summary>희귀도별 유물 첫 획득 포인트 비용.</summary>
    public static int AcquireCost(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common    =>  5,
        RelicRarity.Uncommon  => 10,
        RelicRarity.Rare      => 20,
        RelicRarity.Epic      => 40,
        RelicRarity.Legendary => 80,
        _ => 10,
    };

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
        => EarnPoints(CalculateReincarnationPoints(clearedNormalStage));

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
