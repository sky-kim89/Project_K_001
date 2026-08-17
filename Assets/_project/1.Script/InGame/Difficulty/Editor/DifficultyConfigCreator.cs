using UnityEditor;
using UnityEngine;

// ============================================================
//  DifficultyConfigCreator.cs  [Editor Only]
//  Tools > Project K > 데이터 생성 > 난이도
//
//  난이도 5단계 수치 테이블을 만든다. Assets/Resources/DifficultyConfig.asset
//
//  ■ 수치 근거
//    광포(적 공·체)는 배로 늘리지만, 물량은 +80% 가 상한이다 —
//    후반 웨이브가 이미 1,000마리라 그 이상은 프레임이 먼저 무너진다.
//    각성(우두머리 쿨감)은 -55% 가 한계선이다. 더 줄이면 보스 연출이
//    끝나기도 전에 다음 스킬이 나가 겹친다.
//
//  ■ 환생 포인트 배율이 유일한 보상이다
//    난이도를 올릴 이유가 없으면 아무도 안 올린다. 단계당 대략 +20%.
// ============================================================

public static class DifficultyConfigCreator
{
    const string Path = "Assets/Resources/DifficultyConfig.asset";

    [MenuItem(ProjectKMenu.Data + "난이도", priority = ProjectKMenu.DataPrio + 19)]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var cfg = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(Path);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<DifficultyConfig>();
            AssetDatabase.CreateAsset(cfg, Path);
        }

        cfg.Tiers = new[]
        {
            // 출정 — 기준선. 디버프 없음.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Easy,
                ReincarnationMultiplier = 1.0f,
            },
            // 혈전 — 광포 하나만. 플레이어가 난이도 체감을 보정하는 기준점이라
            //        가장 단순한 디버프여야 한다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Normal,
                EnemyStatBonus          = 0.5f,
                ReincarnationMultiplier = 1.2f,
            },
            // 사지 — 물량 추가. 여기서부터 광역 스킬의 가치가 뛴다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Hard,
                EnemyStatBonus          = 1.0f,
                EnemyCountBonus         = 0.2f,
                ReincarnationMultiplier = 1.4f,
            },
            // 초열 — 각성 추가. 우두머리가 스킬을 두 배 가까이 쏟는다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Hell,
                EnemyStatBonus          = 2.0f,
                EnemyCountBonus         = 0.5f,
                BossCooldownCut         = 0.4f,
                ReincarnationMultiplier = 1.7f,
            },
            // 무간 — 폭주 추가. 엘리트가 돌진을 배우고 보스가 분쇄 강타를 쓴다.
            //        마지막 단계는 '더 큰 숫자' 가 아니라 '다른 게임' 이어야 한다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Inferno,
                EnemyStatBonus          = 3.5f,
                EnemyCountBonus         = 0.8f,
                BossCooldownCut         = 0.55f,
                FrenzyPatterns          = true,
                ReincarnationMultiplier = 2.0f,
            },
        };

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 플레이 중에 다시 만들 수 있다 — 캐시를 버려야 새 수치가 먹는다.
        DifficultyConfig.Invalidate();

        Debug.Log($"[DifficultyConfigCreator] 난이도 {cfg.Tiers.Length}단계 생성 → {Path}");
        EditorGUIUtility.PingObject(cfg);
    }
}
