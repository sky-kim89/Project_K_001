using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  StageGenerator.cs
//  StageConfig 의 커브를 바탕으로 StageData 목록을 절차적으로 생성.
//
//  - 종족은 스테이지 번호를 시드 삼아 고정 (같은 번호 = 같은 종족)
//  - 마지막 웨이브에 보스 추가
//  - 엘리트 스테이지는 중반 이후 웨이브에 Elite 유닛 추가
// ============================================================

public static class StageGenerator
{
    static readonly EnemyRace[] AllRaces = (EnemyRace[])Enum.GetValues(typeof(EnemyRace));

    // ── 전체 생성 ─────────────────────────────────────────────

    public static List<StageData> GenerateAll(StageConfig config, BattleMode mode)
    {
        int count  = mode == BattleMode.Normal ? config.NormalStageCount : config.EliteStageCount;
        var result = new List<StageData>(count);
        for (int i = 1; i <= count; i++)
            result.Add(Generate(config, mode, i));
        return result;
    }

    // ── 스테이지 1개 생성 ─────────────────────────────────────

    public static StageData Generate(StageConfig config, BattleMode mode, int stageNumber)
    {
        int   total      = mode == BattleMode.Normal ? config.NormalStageCount : config.EliteStageCount;
        float progress   = Mathf.Clamp01((float)stageNumber / total);
        int   waveCount  = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(config.WaveCountMin, config.WaveCountMax, progress)));
        int   energyCost = mode == BattleMode.Normal ? config.EnergyCostNormal : config.EnergyCostElite;

        var rng   = new System.Random(HashSeed(mode, stageNumber));
        var waves = new List<WaveData>(waveCount);

        for (int w = 1; w <= waveCount; w++)
        {
            float waveT = waveCount > 1 ? (float)(w - 1) / (waveCount - 1) : 1f;
            waves.Add(GenerateWave(config, mode, stageNumber, w, waveCount, progress, waveT, rng));
        }

        return new StageData
        {
            Mode        = mode,
            StageNumber = stageNumber,
            EnergyCost  = energyCost,
            Waves       = waves,
        };
    }

    // ── 웨이브 1개 생성 ──────────────────────────────────────

    static WaveData GenerateWave(
        StageConfig config, BattleMode mode,
        int stageNumber, int wave, int totalWaves,
        float stageProgress, float waveProgress,
        System.Random rng)
    {
        int       total      = mode == BattleMode.Normal ? config.NormalStageCount : config.EliteStageCount;
        float     levelT     = total > 1 ? Mathf.Clamp01((float)(stageNumber - 1) / (total - 1)) : 1f;
        int       enemyCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(config.EnemyCountMin, config.EnemyCountMax, stageProgress)));
        int       enemyLevel = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(config.EnemyLevelMin, config.EnemyLevelMax, levelT)));
        int       goldReward = Mathf.Max(10, Mathf.RoundToInt(Mathf.Lerp(config.GoldRewardMin, config.GoldRewardMax, stageProgress)));
        EnemyRace race       = AllRaces[rng.Next(AllRaces.Length)];

        bool isLastWave = wave == totalWaves;
        bool hasElite   = mode == BattleMode.Elite && waveProgress >= 0.5f;
        bool hasBoss    = isLastWave;

        var entries = new List<SpawnEntry>();

        int normalCount = hasBoss ? Mathf.Max(1, enemyCount - 2)
                        : hasElite ? Mathf.Max(1, enemyCount - 1)
                        : enemyCount;

        entries.Add(new SpawnEntry
        {
            Name         = $"S{stageNumber}W{wave}E",
            Level        = enemyLevel,
            UnitType     = SpawnUnitType.Enemy,
            Count        = normalCount,
            DelayBetween = 0.3f,
            DelayBefore  = 0f,
            EnemyRace    = race,
        });

        if (hasElite && !hasBoss)
        {
            entries.Add(new SpawnEntry
            {
                Name         = $"S{stageNumber}W{wave}El",
                Level        = enemyLevel + 3,
                UnitType     = SpawnUnitType.Elite,
                Count        = 1,
                DelayBefore  = 1.5f,
                DelayBetween = 0f,
                EnemyRace    = race,
            });
        }

        if (hasBoss)
        {
            entries.Add(new SpawnEntry
            {
                Name         = $"S{stageNumber}Boss",
                Level        = enemyLevel + 5,
                UnitType     = SpawnUnitType.Boss,
                Count        = 1,
                DelayBefore  = 2f,
                DelayBetween = 0f,
                EnemyRace    = race,
            });
        }

        return new WaveData
        {
            DefaultRace  = race,
            EnemyEntries = entries,
            GoldReward   = goldReward,
        };
    }

    // ── 시드 계산 ────────────────────────────────────────────

    static int HashSeed(BattleMode mode, int stageNumber)
        => (int)mode * 100000 + stageNumber;
}
