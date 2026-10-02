using System.Collections.Generic;

// ============================================================
//  GooglePlayAchievementIds.cs
//  AchievementId → Play Console 업적 ID 매핑.
//
//  ⚠ 지금은 전부 빈 칸이다 — Play Console 에 업적을 등록하면 콘솔이 ID(CgkI...)를 만든다.
//    콘솔의 "리소스 가져오기" 로 받은 값을 여기 채운다.
//    (플러그인이 만드는 GPGSIds.cs 의 상수를 넣어도 된다 — 그 파일은 PROJECTK_GPGS 전에는 없으므로
//     여기서는 문자열로 받는다)
//
//  ⚠ 빈 칸인 업적은 전송하지 않고 경고만 남긴다 (GooglePlayAchievementBackend.Unlock).
//  ⚠ AchievementId 를 추가하면 여기에도 한 줄 추가할 것 — Validate() 가 빠진 줄을 잡아 준다.
// ============================================================

public static class GooglePlayAchievementIds
{
    static readonly Dictionary<AchievementId, string> _ids = new()
    {
        { AchievementId.FirstStage,              "" },
        { AchievementId.FirstHurdle,             "" },
        { AchievementId.HalfWay,                 "" },
        { AchievementId.JourneyEnd,              "" },
        { AchievementId.NewTrial,                "" },
        { AchievementId.HellClear,               "" },
        { AchievementId.InfernoClear,            "" },
        { AchievementId.FirstReincarnation,      "" },
        { AchievementId.MiracleSurvivor,         "" },
        { AchievementId.LastGeneral,             "" },
        { AchievementId.KnightOrder,             "" },
        { AchievementId.ArcherCorps,             "" },
        { AchievementId.MageCorps,               "" },
        { AchievementId.ShieldWall,              "" },
        { AchievementId.LoneWolf,                "" },
        { AchievementId.LegendaryGeneral,        "" },
        { AchievementId.RareHeroBisect,          "" },
        { AchievementId.RareHeroArrowStorm,      "" },
        { AchievementId.RareHeroGravityCollapse, "" },
        { AchievementId.RareHeroBulwark,         "" },
        { AchievementId.RareHeroChainLightning,  "" },
        { AchievementId.RareHeroDeathSentence,   "" },
        { AchievementId.RareHeroBloodPrice,      "" },
        { AchievementId.RareHeroPiercingDash,    "" },
        { AchievementId.RareHeroWarBanner,       "" },
        { AchievementId.RareHeroGravestone,      "" },
        { AchievementId.KnightMaster,            "" },
        { AchievementId.ArcherMaster,            "" },
        { AchievementId.MageMaster,              "" },
        { AchievementId.ShieldMaster,            "" },
        { AchievementId.Collector,               "" },
        { AchievementId.CodexComplete,           "" },
        { AchievementId.RelicRoot,               "" },
        { AchievementId.RelicComplete,           "" },
    };

    public static string Get(AchievementId id) => _ids.TryGetValue(id, out var gid) ? gid : null;

    /// <summary>매핑 표에 줄이 없거나 빈 칸인 업적 수 (릴리스 점검용).</summary>
    public static int MissingCount()
    {
        int missing = 0;
        foreach (var def in AchievementCatalog.All)
            if (string.IsNullOrEmpty(Get(def.Id))) missing++;
        return missing;
    }
}
