using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================
//  NormalMode.cs
//  일반 배틀 모드 — BattleModeBase 구현체.
//
//  - 웨이브 구성: WaveDataList (Inspector 에서 직접 설정)
//  - 웨이브 클리어 보상: 웨이브당 고정 골드 지급
//  - 추후 ScriptableObject 테이블로 WaveDataList 를 대체 예정
// ============================================================

public class NormalMode : BattleModeBase
{
    readonly StageData _stage;

    public NormalMode(StageData stage)
    {
        _stage = stage;
    }

    // ── BattleModeBase 구현 ───────────────────────────────────

    public override BattleMode Mode => BattleMode.Normal;

    protected override int GetTotalWaves() => _stage.Waves.Count;

    public override List<SpawnEntry> GetAllySpawnEntries(int wave)
    {
        // 아군은 1웨이브에만 스폰 (이후 웨이브에선 유지)
        if (wave != 1) return null;

        UnitData       unitData   = UserDataManager.Instance.Get<UnitData>();
        DeploymentData deployData = UserDataManager.Instance.Get<DeploymentData>();
        if (unitData == null || unitData.Units.Count == 0) return null;

        int activeSlots = RelicTreeApplier.GetTotalActiveGeneralSlots();

        // 배치 설정이 있으면 배치된 유닛만, 없으면 전체 유닛 스폰
        List<string> names;
        if (deployData != null && deployData.HasAnyDeployed())
        {
            names = deployData.GetDeployedUnits()
                .Where(n => unitData.GetUnit(n) != null)
                .Take(activeSlots)
                .ToList();
        }
        else
        {
            names = unitData.Units
                .Take(activeSlots)
                .Select(u => u.UnitName)
                .ToList();
        }

        if (names.Count == 0) return null;

        var entries = new List<SpawnEntry>(names.Count);
        foreach (string name in names)
        {
            UnitEntry unit = unitData.GetUnit(name);
            entries.Add(new SpawnEntry
            {
                Name         = unit.UnitName,
                Level        = unit.Level,
                UnitType     = SpawnUnitType.General,
                Count        = 1,
                DelayBetween = 0f,
                DelayBefore  = 0f,
            });
        }
        return entries;
    }

    public override List<SpawnEntry> GetEnemySpawnEntries(int wave)
    {
        var entries = GetWaveData(wave)?.EnemyEntries;
        if (entries == null) return null;

        int   maxStage = GameplayConfig.Current.MaxStage;
        float bias     = Mathf.Clamp01((float)_stage.StageNumber / maxStage);
        foreach (var e in entries) e.StageBias = bias;
        return entries;
    }

    // ── 무한 보스 (최종 스테이지 전용) ────────────────────────
    //
    //  최종 스테이지(기본 30)는 클리어할 수 없다.
    //  마지막 웨이브 보스를 잡으면 스텟 ×10, 크기 ×2 인 보스가 다시 나오고,
    //  그 보스를 잡으면 또 ×10 (누적) 보스가 나오는 식으로 무한히 반복된다.

    /// <summary>보스 1기마다 곱해지는 스텟 배율.</summary>
    public const float EndlessStatStep = 10f;

    /// <summary>무한 보스 크기 — 기본 보스 프리팹의 2배 (보스마다 동일).</summary>
    public const float EndlessScaleMult = 2f;

    // float 무한대(≈3.4e38)로 넘어가면 HP 가 Infinity 가 되어 피해 계산이 전부 NaN 이 된다.
    const float EndlessStatCap = 1e30f;

    public override bool IsEndless => _stage.StageNumber >= GameplayConfig.Current.MaxStage;

    public override List<SpawnEntry> GetEndlessBossEntries(int bossIndex)
    {
        // 마지막 웨이브 보스를 원본으로 삼는다 (레벨·종족·스테이지 배율 승계)
        SpawnEntry origin = _stage.Waves[^1].EnemyEntries.First(e => e.UnitType == SpawnUnitType.Boss);

        float statMult = Mathf.Min(EndlessStatCap,
                                   origin.StatMultiplier * Mathf.Pow(EndlessStatStep, bossIndex));

        return new List<SpawnEntry>
        {
            new()
            {
                // 이름이 바뀌면 외형·기본 스텟 시드도 바뀐다 — 매번 다른 보스로 보인다
                Name            = $"S{_stage.StageNumber}EndlessBoss{bossIndex}",
                Level           = origin.Level,
                UnitType        = SpawnUnitType.Boss,
                Count           = 1,
                DelayBefore     = 2f,
                DelayBetween    = 0f,
                EnemyRace       = origin.EnemyRace,
                StatMultiplier  = statMult,
                StageBias       = 1f,   // 최종 스테이지 — 스텟 범위 최댓값
                ScaleMultiplier = EndlessScaleMult,
                KnockbackImmune = true,
            }
        };
    }

    public override void ApplyStageClearReward()
    {
        Context.StageLevel = _stage.StageNumber;

        var   runData   = UserDataManager.Instance?.Get<RunAbilityData>();
        float goldBonus = AbilityApplier.GetGoldBonusRatio(runData?.HeldAbilities, AbilityDatabase.Current);
        int   totalGold = Mathf.RoundToInt(_stage.GoldReward * (1f + goldBonus));

        if (totalGold > 0)
            Context.PendingRewards.Add(new ItemAmount { Item = eItem.Gold, Amount = totalGold });

        // 장비 강화석 — 장비 강화(HeroDetailPopup)에 쓰인다.
        //
        // ⚠ 예전엔 여기서 전투석(BattleStone)을 줬다
        //   전투석은 소비처가 이벤트 선택지 하나뿐이라 사실상 쌓이기만 했고,
        //   반대로 장비 강화석은 소비처만 있고 획득처가 '장비 분해' 뿐이었다.
        //   스테이지 보상을 강화석으로 돌려 두 문제를 같이 없앴다.
        if (_stage.EquipStoneReward > 0)
            Context.PendingRewards.Add(new ItemAmount
                { Item = eItem.EquipUpgradeStone, Amount = _stage.EquipStoneReward });

        for (int i = 0; i < _stage.EquipBoxReward; i++)
            Context.PendingRewards.Add(new ItemAmount { Item = eItem.EquipBox, Amount = 1 });
        Context.PendingRewards.Add(new ItemAmount { Item = eItem.SoldierShard, Amount = _stage.ShardReward });

        // 장군 강화석 — HeroDetailPopup 의 등급업에 쓰인다
        if (_stage.GeneralStoneReward > 0)
            Context.PendingRewards.Add(new ItemAmount
                { Item = eItem.GeneralUpgradeStone, Amount = _stage.GeneralStoneReward });

        Debug.Log($"[NormalMode] 클리어 보상: 골드 +{totalGold}(×{1f + goldBonus:F2}), 장비 강화석 +{_stage.EquipStoneReward}, " +
                  $"장비 박스 ×{_stage.EquipBoxReward}, 용병 조각 ×{_stage.ShardReward}, 장군 강화석 +{_stage.GeneralStoneReward}");
    }

    // ── 훅 오버라이드 ─────────────────────────────────────────

    public override void OnWaveStart(int wave)
    {
        Debug.Log($"[NormalMode] 웨이브 {wave} 시작");
    }

    public override void OnWaveClear(int wave)
    {
        Debug.Log($"[NormalMode] 웨이브 {wave} 클리어");
    }

    public override void OnBattleVictory()
    {
        Debug.Log("[NormalMode] 배틀 승리");
    }

    public override void OnBattleDefeat()
    {
        Debug.Log("[NormalMode] 배틀 패배");
    }

    // ── 내부 ─────────────────────────────────────────────────

    // 2차 곡선: 1 − ((stage−1) / 19)²
    // Stage  5 → ×0.956   Stage  9 → ×0.823   Stage 15 → ×0.457
    // Stage 20+ → ×0.05 (최소)
    protected override float GetSpawnDelayMultiplier()
    {
        float t = (_stage.StageNumber - 1) / 19f;
        return Mathf.Max(0.05f, 1.0f - t * t);
    }

    WaveData GetWaveData(int wave)
    {
        int index = wave - 1;
        if (index < 0 || index >= _stage.Waves.Count)
        {
            Debug.LogWarning($"[NormalMode] 웨이브 데이터 없음: {wave}");
            return null;
        }
        return _stage.Waves[index];
    }
}

// ── 웨이브 데이터 구조 ────────────────────────────────────────

/// <summary>
/// 웨이브 하나의 구성 데이터.
/// 추후 ScriptableObject 로 전환해 테이블 관리 예정.
/// </summary>
[System.Serializable]
public class WaveData
{
    [Tooltip("이 웨이브 적군의 기본 종족.\n" +
             "항목을 새로 추가하면 이 종족으로 초기화된다.\n" +
             "항목별로 개별 변경도 가능하다.")]
    public EnemyRace DefaultRace = EnemyRace.Orc;

    [Tooltip("이 웨이브에서 스폰할 적군 목록")]
    public List<SpawnEntry> EnemyEntries = new();
}
