using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  BattleManager.cs
//  배틀 전체 흐름 관리 Singleton.
//
//  담당 역할:
//  - BattleModeBase 를 통해 웨이브 진행
//  - 아군/적군 생존 카운트 추적 → 웨이브 클리어 / 패배 판정
//  - 웨이브 클리어 시 보상 창(PopupType.WaveReward) 오픈
//  - 스포너(AllySpawner / EnemySpawner) 참조 보유
//
//  외부에서의 사용:
//    BattleManager.Instance.StartBattle(BattleMode.Normal, waveList);
//    BattleManager.Instance.OnUnitDead(TeamType.Enemy);  ← UnitDeathDespawnSystem 호출
//    BattleManager.Instance.OnWaveRewardClosed();        ← 보상 창 닫기 완료 콜백
// ============================================================

public class BattleManager : Singleton<BattleManager>
{
    // ── Inspector 연결 ────────────────────────────────────────

    [Header("스포너")]
    public AllySpawner  AllySpawner;
    public EnemySpawner EnemySpawner;


    // ── 내부 상태 ─────────────────────────────────────────────

    BattleContext  _context;
    BattleModeBase _mode;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>아군이 전멸했는지 여부. ECS 시스템에서 프레임마다 읽는다.</summary>
    public bool IsAllyDefeated => _context?.IsAllyDefeated ?? false;

    /// <summary>적군이 전멸했는지 여부(웨이브 클리어). ECS 시스템에서 프레임마다 읽는다.</summary>
    public bool IsEnemyDefeated => _context?.IsEnemyClear ?? false;

    // ── Unity 생명주기 ─────────────────────────────────────────

    /// <summary>배틀을 시작한다.</summary>
    public void StartBattle(BattleModeBase mode)
    {
        _context = new BattleContext { Mode = mode.Mode };
        _mode    = mode;
        _mode.Initialize(_context, AllySpawner, EnemySpawner);

        StartCoroutine(StartBattleRoutine());
    }

    // 1웨이브 적군을 미리 풀에 준비한 뒤 배틀 루틴을 시작한다.
    IEnumerator StartBattleRoutine()
    {
        List<SpawnEntry> wave1Enemies = _mode.GetEnemySpawnEntries(1);
        if (wave1Enemies is { Count: > 0 })
            yield return StartCoroutine(EnemySpawner.Prewarm(wave1Enemies));

        yield return StartCoroutine(BattleRoutine());
    }

    /// <summary>
    /// 병사처럼 스포너 외부에서 추가 스폰되는 유닛을 카운트에 반영한다.
    /// GeneralRuntimeBridge.SpawnSoldiers() 에서 병사 스폰 성공 시 호출.
    /// </summary>
    public void OnUnitSpawned(TeamType team)
    {
        if (_context == null) return;

        if (team == TeamType.Ally)
            _context.AliveAllyCount++;
        else
            _context.AliveEnemyCount++;
    }

    /// <summary>
    /// UnitDeathDespawnSystem 이 유닛 사망 시 호출.
    /// 생존 카운트를 갱신하고 웨이브 클리어 / 패배 여부를 확인한다.
    /// </summary>
    public void OnUnitDead(TeamType team)
    {
        if (_context == null) return;

        if (team == TeamType.Enemy)
            _context.AliveEnemyCount = Mathf.Max(0, _context.AliveEnemyCount - 1);
        else
            _context.AliveAllyCount  = Mathf.Max(0, _context.AliveAllyCount  - 1);

        EvaluateBattleState();
    }

    // ── 배틀 메인 루틴 ────────────────────────────────────────

    IEnumerator BattleRoutine()
    {
        while (_context.CurrentWave < _context.TotalWaves)
        {
            _context.CurrentWave++;
            yield return StartCoroutine(RunWave(_context.CurrentWave));

            // 패배 시 루틴 종료
            if (_context.State == BattleState.BattleDefeat)
                yield break;

            // 웨이브 클리어 처리
            _context.State = BattleState.WaveClear;
            _mode.OnWaveClear(_context.CurrentWave);

            // 마지막 웨이브면 승리 + 스테이지 클리어 보상 지급
            if (_context.IsLastWave)
            {
                _mode.ApplyStageClearReward();
                _context.State = BattleState.BattleVictory;
                _mode.OnBattleVictory();
                yield break;
            }
        }
    }

    IEnumerator RunWave(int wave)
    {
        _context.State = BattleState.Preparing;
        _mode.OnWaveStart(wave);

        // ── 스폰 ──────────────────────────────────────────────

        // 아군 스폰 (항목이 있을 때만)
        List<SpawnEntry> allyEntries = _mode.GetAllySpawnEntries(wave);
        if (allyEntries is { Count: > 0 })
        {
            AllySpawner.Spawn(allyEntries);
            _context.AliveAllyCount += CountUnits(allyEntries);
        }

        // 적군 스폰
        List<SpawnEntry> enemyEntries = _mode.GetEnemySpawnEntries(wave);
        if (enemyEntries is { Count: > 0 })
        {
            EnemySpawner.Spawn(enemyEntries);
            _context.AliveEnemyCount += CountUnits(enemyEntries);
        }

        _context.State = BattleState.InWave;

        // ── 웨이브 종료 대기 (적 전멸 or 아군 전멸) ─────────────
        yield return new WaitUntil(() =>
            _context.State == BattleState.WaveClear ||
            _context.State == BattleState.BattleDefeat);
    }

    // ── 내부 ─────────────────────────────────────────────────

    /// <summary>생존 카운트 기반으로 웨이브 클리어 / 패배를 판정한다.</summary>
    void EvaluateBattleState()
    {
        if (_context.State != BattleState.InWave) return;

        if (_context.IsAllyDefeated)
        {
            _context.State = BattleState.BattleDefeat;
            Debug.Log($"[BattleManager] 패배 — 웨이브 {_context.CurrentWave}/{_context.TotalWaves}" +
                      $"  아군 생존: {_context.AliveAllyCount}" +
                      $"  적군 잔존: {_context.AliveEnemyCount}");
            _mode.OnBattleDefeat();
        }
        else if (_context.IsEnemyClear)
        {
            _context.State = BattleState.WaveClear;
        }
    }

    /// <summary>SpawnEntry 목록의 총 유닛 수를 계산한다.</summary>
    static int CountUnits(List<SpawnEntry> entries)
    {
        int total = 0;
        foreach (SpawnEntry entry in entries)
            total += entry.Count;
        return total;
    }


}
