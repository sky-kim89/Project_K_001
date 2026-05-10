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

        // 배치 설정이 있으면 배치된 유닛만, 없으면 전체 유닛 스폰
        List<string> names;
        if (deployData != null && deployData.HasAnyDeployed())
        {
            names = deployData.GetDeployedUnits()
                .Where(n => unitData.GetUnit(n) != null)
                .ToList();
        }
        else
        {
            names = unitData.Units.Select(u => u.UnitName).ToList();
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

    public override List<SpawnEntry> GetEnemySpawnEntries(int wave) => GetWaveData(wave)?.EnemyEntries;

    public override void ApplyStageClearReward()
    {
        Context.StageLevel = _stage.StageNumber;

        if (_stage.GoldReward  > 0) Context.PendingRewards.Add(new ItemAmount { Item = eItem.Gold,        Amount = _stage.GoldReward  });
        if (_stage.StoneReward > 0) Context.PendingRewards.Add(new ItemAmount { Item = eItem.BattleStone, Amount = _stage.StoneReward });

        for (int i = 0; i < _stage.EquipBoxReward; i++)
            Context.PendingRewards.Add(new ItemAmount { Item = eItem.EquipBox, Amount = 1 });
        Context.PendingRewards.Add(new ItemAmount { Item = eItem.SoldierShard, Amount = _stage.ShardReward });

        Debug.Log($"[NormalMode] 클리어 보상: 골드 +{_stage.GoldReward}, 전투석 +{_stage.StoneReward}, 장비 박스 ×{_stage.EquipBoxReward}, 용병 조각 ×{_stage.ShardReward}");
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
