using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  StageGenerator.cs
//  StageConfig 의 커브를 바탕으로 StageData 목록을 절차적으로 생성.
//
//  ■ 난이도 곡선 (기본 설정 기준 — 레벨 배율 제외한 statMult)
//    스테이지마다 약 1.24배씩 오르고, 5의 배수에서 허들이 한 번 더 곱해진다.
//    Stage  1: ×0.55   | 웨이브 2 | 적 ~9/wave   → ~18마리
//    Stage  5: ×2.02 허들| 웨이브 5 | 적 ~30/wave  → ~150마리
//    Stage 10: ×6.45 허들| 웨이브 6 | 적 ~45/wave → ~270마리
//    Stage 15: ×16.0 허들| 웨이브 7 | 적 ~61/wave → ~427마리
//    Stage 20: ×40.0 허들| 웨이브 8 | 적 ~77/wave  → ~616마리
//    Stage 25: ×100  허들| 웨이브 9 | 적 ~92/wave  → ~828마리
//    Stage 30: ×175  허들| 웨이브10 | 적 ~108/wave → ~1080마리
//
//  ■ 환생·유물 시스템 고려
//    후반 블록(26~30) 100× 기준 — 환생 보너스가 없으면 클리어 불가 설계
//
//  ⚠ 예전 곡선은 초·중반이 통째로 헐거웠다
//    블록 안에서 배율이 고정(계단형)인데다 초반 완화가 20스테이지까지 걸려 있어,
//    스테이지 1~9 의 실효 배율이 0.3~2.3 에 머물렀다. 그 사이 플레이어는
//    레벨·장비·어빌리티·유물로 계속 강해지므로 적이 일방적으로 밀렸다.
//    지금은 앵커 사이를 기하 보간하고 완화를 8스테이지에서 끝낸다.
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
        bool  isHurdle   = config.IsHurdle(stageNumber);

        // 허들 스테이지는 웨이브 + 1
        int baseWaves = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(config.WaveCountMin, config.WaveCountMax, progress)));
        int waveCount = isHurdle ? baseWaves + config.HurdleExtraWaves : baseWaves;

        int energyCost = mode == BattleMode.Normal ? config.EnergyCostNormal : config.EnergyCostElite;

        // 스텟 배율 계산
        float blockMult  = config.GetBlockMultiplier(stageNumber);
        float hurdleMult = isHurdle ? config.HurdleStatMultiplier : 1f;

        // 초반 완화 — 스테이지 1 에서 EarlyGameStatStart, EarlyGameEndStage 에서 1.0.
        // ⚠ 완화 구간을 길게 끌면 안 된다
        //   예전엔 20스테이지까지 걸려 있어서, 앵커 배율이 오르는 만큼을 완화가
        //   계속 깎아먹었다. 그 사이 플레이어는 레벨·장비·어빌리티·유물로 계속
        //   강해지므로 중반까지 적이 일방적으로 밀렸다.
        //   완화는 "조작을 익히는 몇 판" 만 덮으면 된다.
        int   earlyEnd   = Mathf.Max(2, config.EarlyGameEndStage);
        float earlyMult  = stageNumber < earlyEnd
            ? Mathf.Lerp(config.EarlyGameStatStart, 1f, (stageNumber - 1) / (float)(earlyEnd - 1))
            : 1f;

        float statMult   = blockMult * hurdleMult * earlyMult;

        var rng   = new System.Random(HashSeed(mode, stageNumber));
        var waves = new List<WaveData>(waveCount);

        for (int w = 1; w <= waveCount; w++)
        {
            float waveT = waveCount > 1 ? (float)(w - 1) / (waveCount - 1) : 1f;
            waves.Add(GenerateWave(config, mode, stageNumber, w, waveCount,
                                   progress, waveT, statMult, isHurdle, rng));
        }

        // 골드만 선형 등차 — 진행도 보간(지수적 상승)에서 내려온 값
        int goldReward  = Mathf.Max(10,  config.GoldRewardBase + (stageNumber - 1) * config.GoldRewardPerStage);
        int stoneReward = Mathf.Max(1,   Mathf.RoundToInt(Mathf.Lerp(config.EquipStoneRewardMin, config.EquipStoneRewardMax, progress)));
        int expReward   = Mathf.Max(1,   Mathf.RoundToInt(Mathf.Lerp(config.ExpRewardMin,   config.ExpRewardMax,   progress)));
        int block        = (stageNumber - 1) / 5;
        int shardReward  = config.ShardRewardBase + block * config.ShardRewardPerBlock
                           + (isHurdle ? config.ShardHurdleBonus : 0);
        // 장군 강화석 — 등급업 전용 재화. 30스테이지 완주 시 누적 약 150개.
        int generalStone = config.GeneralStoneRewardBase + block * config.GeneralStoneRewardPerBlock
                           + (isHurdle ? config.GeneralStoneHurdleBonus : 0);
        int equipBoxReward = stageNumber >= config.EquipBoxThreshold3 ? 3
                           : stageNumber >= config.EquipBoxThreshold2 ? 2
                           : 1;

        // 허들 보상 추가 — 일반 보상의 2배 + 전투석 추가
        if (isHurdle)
        {
            goldReward  = Mathf.RoundToInt(goldReward * 2.0f);
            stoneReward = stoneReward + 5;
        }

        return new StageData
        {
            Mode        = mode,
            StageNumber = stageNumber,
            EnergyCost  = energyCost,
            GoldReward  = goldReward,
            EquipStoneReward = stoneReward,
            ExpReward   = expReward,
            ShardReward    = shardReward,
            GeneralStoneReward = generalStone,
            EquipBoxReward = equipBoxReward,
            Waves          = waves,
        };
    }

    // ── 웨이브 1개 생성 ──────────────────────────────────────

    static WaveData GenerateWave(
        StageConfig config, BattleMode mode,
        int stageNumber, int wave, int totalWaves,
        float stageProgress, float waveProgress,
        float statMult, bool isHurdle,
        System.Random rng)
    {
        int   total      = mode == BattleMode.Normal ? config.NormalStageCount : config.EliteStageCount;
        float levelT     = total > 1 ? Mathf.Clamp01((float)(stageNumber - 1) / (total - 1)) : 1f;
        int   enemyLevel = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(config.EnemyLevelMin, config.EnemyLevelMax, levelT)));

        // 허들 스테이지 적 수 추가
        // 난이도 '물량' — 적 등장 수를 늘린다.
        // ⚠ +80% 가 상한이다 — 후반 웨이브가 이미 1,000마리라
        //   그 이상은 난이도가 아니라 프레임이 먼저 무너진다.
        float countMul = 1f + Mathf.Max(0f, DifficultyConfig.CurrentTier()?.EnemyCountBonus ?? 0f);
        int baseCount  = Mathf.Max(1, Mathf.RoundToInt(
                             Mathf.Lerp(config.EnemyCountMin, config.EnemyCountMax, stageProgress) * countMul));
        int enemyCount = isHurdle ? baseCount + config.HurdleExtraEnemies : baseCount;

        EnemyRace race = AllRaces[rng.Next(AllRaces.Length)];

        bool isLastWave = wave == totalWaves;
        bool hasElite   = mode == BattleMode.Elite && waveProgress >= 0.5f;
        // 허들 스테이지는 모든 웨이브에 엘리트 추가, 일반은 마지막 웨이브 보스만
        bool hasEliteNormal = isHurdle && stageNumber >= 5 && !isLastWave;
        bool hasBoss        = isLastWave;

        var entries = new List<SpawnEntry>();

        int halfStages  = Mathf.Max(1, config.NormalStageCount / 2);
        int eliteSlots  = (hasElite || hasEliteNormal) && !hasBoss ? 1 + (isHurdle ? stageNumber / halfStages : 0) : 0;
        int normalCount = hasBoss ? Mathf.Max(1, enemyCount - 2)
                        : eliteSlots > 0 ? Mathf.Max(1, enemyCount - eliteSlots)
                        : enemyCount;

        // ── 엘리트 호위 부대 ─────────────────────────────────────
        //  엘리트는 혼자 걸어 나오지 않고 호위 병사를 데리고 대형으로 진입한다.
        //  ⚠ 호위는 "추가" 가 아니라 일반 적 트리클에서 떼어 온다.
        //    그냥 얹으면 웨이브 총 물량이 통째로 늘어 난이도가 따로 논다.
        //    같은 수의 적이 한 명씩 흘러나오느냐, 부대로 몰려오느냐의 차이다.
        int escortPerElite = 0;
        if (eliteSlots > 0)
        {
            int want     = 4 + stageNumber / 6;                        // 1스테이지 4기 → 30스테이지 9기
            int borrowed = Mathf.Min(normalCount - 1, eliteSlots * want);

            // 떼어 올 여유가 없으면 호위를 붙이지 않는다 (총 물량 유지가 우선)
            escortPerElite = borrowed > 0 ? borrowed / eliteSlots : 0;
            normalCount   -= escortPerElite * eliteSlots;
        }

        entries.Add(new SpawnEntry
        {
            Name           = $"S{stageNumber}W{wave}E",
            Level          = enemyLevel,
            UnitType       = SpawnUnitType.Enemy,
            Count          = normalCount,
            DelayBetween   = 0.3f,
            DelayBefore    = 0f,
            EnemyRace      = race,
            StatMultiplier = statMult,
        });

        // 엘리트 유닛 추가 (엘리트 스테이지 중반 or 허들 스테이지 중간 웨이브)
        if ((hasElite || hasEliteNormal) && !hasBoss)
        {
            // 후반 허들일수록 엘리트 수 증가 (15스테이지마다 +1)
            int eliteCount = 1 + (isHurdle ? stageNumber / halfStages : 0);
            entries.Add(new SpawnEntry
            {
                Name           = $"S{stageNumber}W{wave}El",
                Level          = enemyLevel + 3,
                UnitType       = SpawnUnitType.Elite,
                Count          = eliteCount,
                DelayBefore    = 1.5f,
                DelayBetween   = 1.0f,
                EnemyRace      = race,
                StatMultiplier = statMult,
                EscortCount    = escortPerElite,   // 부대 대형으로 함께 진입
            });
        }

        // 마지막 웨이브 보스
        if (hasBoss)
        {
            entries.Add(new SpawnEntry
            {
                Name           = $"S{stageNumber}Boss",
                Level          = enemyLevel + 5,
                UnitType       = SpawnUnitType.Boss,
                Count          = 1,
                DelayBefore    = 2f,
                DelayBetween   = 0f,
                EnemyRace      = race,
                StatMultiplier = statMult,
            });
        }

        return new WaveData
        {
            DefaultRace  = race,
            EnemyEntries = entries,
        };
    }

    // ── 시드 계산 ────────────────────────────────────────────

    static int HashSeed(BattleMode mode, int stageNumber)
        => (int)mode * 100000 + stageNumber;
}
