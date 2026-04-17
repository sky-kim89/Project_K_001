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
    bool           _wave1AlliesSpawned;  // StartBattleRoutine 에서 웨이브1 아군 선스폰 여부

    // ── UI 이벤트 ─────────────────────────────────────────────

    /// <summary>유닛 사망 시 팀 정보를 전달하는 이벤트. TopBarUI 킬 카운터 등에서 구독.</summary>
    public static event System.Action<TeamType> OnUnitKilled;

    /// <summary>웨이브 1 아군(장군) 스폰 완료. InGameManager 가 로딩 팝업 닫기에 사용.</summary>
    public static event System.Action OnAlliesReady;

    /// <summary>전체 웨이브 클리어(승리). InGameManager 가 결과 팝업 오픈에 사용.</summary>
    public static event System.Action OnVictory;

    /// <summary>아군 전멸(패배). InGameManager 가 결과 팝업 오픈에 사용.</summary>
    public static event System.Action OnDefeat;

    // ── 킬 카운트 ─────────────────────────────────────────────
    int _enemyKillCount;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>현재 배틀 컨텍스트. TopBarUI 등 UI 에서 웨이브/상태 정보를 읽을 때 사용.</summary>
    public BattleContext Context => _context;

    /// <summary>이번 배틀에서 처치한 적 수.</summary>
    public int EnemyKillCount => _enemyKillCount;

    /// <summary>아군이 전멸했는지 여부. ECS 시스템에서 프레임마다 읽는다.</summary>
    public bool IsAllyDefeated => _context?.IsAllyDefeated ?? false;

    /// <summary>적군이 전멸했는지 여부(웨이브 클리어). ECS 시스템에서 프레임마다 읽는다.</summary>
    public bool IsEnemyDefeated => _context?.IsEnemyClear ?? false;

    // ── Unity 생명주기 ─────────────────────────────────────────

    /// <summary>배틀을 시작한다.</summary>
    public void StartBattle(BattleModeBase mode)
    {
        _context             = new BattleContext { Mode = mode.Mode };
        _mode                = mode;
        _wave1AlliesSpawned  = false;
        _enemyKillCount      = 0;
        _mode.Initialize(_context, AllySpawner, EnemySpawner);

        StartCoroutine(StartBattleRoutine());
    }

    // 웨이브1 아군을 즉시 스폰한 뒤 적군 프리웜 → 배틀 루틴 시작.
    // 아군 패널이 게임 시작과 동시에 표시된다.
    IEnumerator StartBattleRoutine()
    {
        // ── 웨이브 1 아군 즉시 스폰 (프리웜 대기 없음) ──────────
        _context.CurrentWave = 1;
        List<SpawnEntry> ally1 = _mode.GetAllySpawnEntries(1);
        if (ally1 is { Count: > 0 })
        {
            // 코루틴 없이 동기 스폰 — 첫 yield(프리웜) 이전에 장군 패널이 확실히 생성됨
            AllySpawner.SpawnImmediate(ally1);
            _context.AliveAllyCount += CountUnits(ally1);
            _wave1AlliesSpawned = true;
        }

        // 장군 스폰 완료 통보 → InGameManager 가 로딩 팝업을 닫는다
        OnAlliesReady?.Invoke();

        // ── 적군 프리웜 (아군 스폰 후) ───────────────────────────
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
        {
            _context.AliveEnemyCount = Mathf.Max(0, _context.AliveEnemyCount - 1);
            _enemyKillCount++;
        }
        else
        {
            _context.AliveAllyCount  = Mathf.Max(0, _context.AliveAllyCount  - 1);
        }

        OnUnitKilled?.Invoke(team);
        EvaluateBattleState();
    }

    // ── 배틀 메인 루틴 ────────────────────────────────────────

    // CurrentWave 는 StartBattleRoutine 에서 이미 1로 설정됨.
    // 루프 시작 시 증가가 아닌 끝에서 증가하는 구조로 중복 증가 방지.
    IEnumerator BattleRoutine()
    {
        while (_context.CurrentWave <= _context.TotalWaves)
        {
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
                OnVictory?.Invoke();
                yield break;
            }

            _context.CurrentWave++;
        }
    }

    IEnumerator RunWave(int wave)
    {
        _context.State = BattleState.Preparing;
        _mode.OnWaveStart(wave);

        // ── 스폰 ──────────────────────────────────────────────

        // 아군 스폰 (웨이브1은 StartBattleRoutine에서 이미 스폰됨)
        if (!(wave == 1 && _wave1AlliesSpawned))
        {
            List<SpawnEntry> allyEntries = _mode.GetAllySpawnEntries(wave);
            if (allyEntries is { Count: > 0 })
            {
                AllySpawner.Spawn(allyEntries);
                _context.AliveAllyCount += CountUnits(allyEntries);
            }
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
            OnDefeat?.Invoke();
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
