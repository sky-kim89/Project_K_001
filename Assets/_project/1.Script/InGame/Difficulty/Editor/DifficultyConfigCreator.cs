using UnityEditor;
using UnityEngine;

// ============================================================
//  DifficultyConfigCreator.cs  [Editor Only]
//  Tools > Project K > 데이터 생성 > 난이도
//
//  난이도 5단계 수치 테이블을 만든다. Assets/Resources/DifficultyConfig.asset
//
//  ■ 수치 근거 — 보통부터 계단이 점점 커진다
//    광포(적 공·체)는 보통 이후 한 단계마다 대략 두 배씩 뛴다.
//      보통 +50% → 어려움 +120% → 지옥 +250% → 불지옥 +500%
//    앞 단계는 완만해서 난이도를 올려볼 마음이 들고, 뒷 단계는
//    한 칸 올릴 때마다 확실히 다른 게임이 된다.
//
//    상한이 있는 값은 이 곡선을 따르지 않는다 —
//    물량은 +80% 가 상한이다. 후반 웨이브가 이미 1,000마리라
//    그 이상은 프레임이 먼저 무너진다.
//    각성(우두머리 쿨감)은 -55% 가 한계선이다. 더 줄이면 보스 연출이
//    끝나기도 전에 다음 스킬이 나가 겹친다.
//
//  ■ 환생 포인트 배율이 유일한 보상이다
//    난이도를 올릴 이유가 없으면 아무도 안 올린다.
//    광포가 500% 까지 가는 만큼 보상도 ×3 까지 올렸다.
//    다만 보상은 점진적이다 — 증가폭이 +0.3 → +0.4 → +0.55 → +0.75 로
//    완만하게 커진다. 광포처럼 배로 뛰면 낮은 난이도를 아무도 안 돈다.
//      쉬움 ×1.0 → 보통 ×1.3 → 어려움 ×1.7 → 지옥 ×2.25 → 불지옥 ×3.0
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
            //        가장 단순한 디버프여야 한다. 계단의 출발점이므로 여기는 그대로 둔다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Normal,
                EnemyStatBonus          = 0.5f,   // +50%
                ReincarnationMultiplier = 1.3f,
            },
            // 사지 — 물량 추가. 여기서부터 광역 스킬의 가치가 뛴다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Hard,
                EnemyStatBonus          = 1.2f,   // +120% (직전 대비 +70%p)
                EnemyCountBonus         = 0.2f,
                ReincarnationMultiplier = 1.7f,
            },
            // 초열 — 각성 추가. 우두머리가 스킬을 두 배 가까이 쏟는다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Hell,
                EnemyStatBonus          = 2.5f,   // +250% (직전 대비 +130%p)
                EnemyCountBonus         = 0.45f,
                BossCooldownCut         = 0.4f,
                ReincarnationMultiplier = 2.25f,
            },
            // 무간 — 폭주 추가. 엘리트가 돌진을 배우고 보스가 분쇄 강타를 쓴다.
            //        마지막 단계는 '더 큰 숫자' 가 아니라 '다른 게임' 이어야 한다.
            new DifficultyConfig.TierEntry
            {
                Tier                    = DifficultyTier.Inferno,
                EnemyStatBonus          = 5.0f,   // +500% (직전 대비 +250%p — 상한)
                EnemyCountBonus         = 0.8f,   // 상한 (1,000마리 프레임 한계)
                BossCooldownCut         = 0.55f,  // 상한 (보스 연출 길이 한계)
                FrenzyPatterns          = true,
                ReincarnationMultiplier = 3.0f,
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
