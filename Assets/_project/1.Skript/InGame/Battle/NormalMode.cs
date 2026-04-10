using System.Collections.Generic;
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
    // ── 웨이브 데이터 (추후 SO 테이블로 교체 예정) ───────────

    readonly List<WaveData> _waves;

    public NormalMode(List<WaveData> waves)
    {
        _waves = waves;
    }

    // ── BattleModeBase 구현 ───────────────────────────────────

    public override BattleMode Mode => BattleMode.Normal;

    protected override int GetTotalWaves() => _waves.Count;

    public override List<SpawnEntry> GetAllySpawnEntries(int wave)
    {
        // 아군은 1웨이브에만 스폰 (이후 웨이브에선 유지)
        if (wave != 1) return null;

        UnitData unitData = UserDataManager.Instance.Get<UnitData>();
        if (unitData == null || unitData.Units.Count == 0) return null;

        var entries = new List<SpawnEntry>(unitData.Units.Count);
        foreach (UnitEntry unit in unitData.Units)
        {
            entries.Add(new SpawnEntry
            {
                Name         = unit.UnitName,   // 직업·스텟 시드
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
        WaveData data = GetWaveData(wave);
        return data?.EnemyEntries;
    }

    public override void ApplyStageClearReward()
    {
        int total = 0;
        for (int w = 1; w <= _waves.Count; w++)
        {
            WaveData data = GetWaveData(w);
            if (data != null) total += data.GoldReward;
        }

        Context.PendingGold += total;
        Debug.Log($"[NormalMode] 스테이지 클리어 보상: 골드 +{total}");
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
        int index = wave - 1; // wave 는 1부터 시작
        if (index < 0 || index >= _waves.Count)
        {
            Debug.LogWarning($"[NormalMode] 웨이브 데이터 없음: {wave}");
            return null;
        }
        return _waves[index];
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

    [Tooltip("이 웨이브 클리어 시 지급할 골드")]
    public int GoldReward = 100;
}
