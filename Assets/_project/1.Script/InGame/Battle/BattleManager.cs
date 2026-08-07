using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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
        // 이전 배틀 잔여 유닛이 있을 경우 정리
        if (_context != null)
            DespawnAllUnits();

        _context             = new BattleContext { Mode = mode.Mode };
        _mode                = mode;
        _wave1AlliesSpawned  = false;
        _enemyKillCount      = 0;
        _mode.Initialize(_context, AllySpawner, EnemySpawner);
        BattleStatsTracker.Instance?.Reset();

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

        // OnBattleStart 트리거 어빌리티 일괄 발동
        FireBattleStartTriggers();

        // ── 웨이브 1 적군만 프리웜 (즉시 스폰을 위한 최소 준비) ──
        List<SpawnEntry> wave1Enemies = _mode.GetEnemySpawnEntries(1);
        if (wave1Enemies is { Count: > 0 })
            yield return StartCoroutine(EnemySpawner.Prewarm(wave1Enemies));

        // 웨이브 2+ 프리웜은 배틀 진행 중에 백그라운드로 처리
        StartCoroutine(PrewarmRemainingWaves());

        yield return StartCoroutine(BattleRoutine());
    }

    // 웨이브 2 이후 적군을 배틀 진행 중에 백그라운드로 프리웜한다.
    IEnumerator PrewarmRemainingWaves()
    {
        for (int w = 2; w <= _context.TotalWaves; w++)
        {
            List<SpawnEntry> waveEnemies = _mode.GetEnemySpawnEntries(w);
            if (waveEnemies is { Count: > 0 })
                yield return StartCoroutine(EnemySpawner.Prewarm(waveEnemies));
        }
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
            bool isLastWave = _context.IsLastWave;
            yield return StartCoroutine(RunWave(_context.CurrentWave, awaitSpawnOnly: !isLastWave));

            // 패배 시 루틴 종료
            if (_context.State == BattleState.BattleDefeat)
                yield break;

            // 웨이브 클리어 처리
            _context.State = BattleState.WaveClear;
            _mode.OnWaveClear(_context.CurrentWave);

            // 마지막 웨이브면 승리 + 스테이지 클리어 보상 산정
            // 실제 지급은 BattleResultPopup 에서 RewardOpener 를 통해 처리
            if (isLastWave)
            {
                ApplyStageClearTraitStacks();
                _mode.ApplyStageClearReward();
                _context.State = BattleState.BattleVictory;
                LogBattleStats("승리");
                _mode.OnBattleVictory();
                OnVictory?.Invoke();
                yield break;
            }

            _context.CurrentWave++;
        }
    }

    // awaitSpawnOnly=true : 스폰 완료 시 즉시 다음 웨이브로 (적이 살아 있어도 진행)
    // awaitSpawnOnly=false: 마지막 웨이브 — 적 전멸까지 대기
    IEnumerator RunWave(int wave, bool awaitSpawnOnly)
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

        // ── 웨이브 종료 대기 ──────────────────────────────────
        if (awaitSpawnOnly)
        {
            // 스폰 완료 시 즉시 리턴 — 잔존 적은 다음 웨이브와 병행 처리
            yield return new WaitUntil(() =>
                !EnemySpawner.IsSpawning ||
                _context.State == BattleState.BattleDefeat);
        }
        else
        {
            // 마지막 웨이브: 모든 웨이브 통산 적 전멸까지 대기
            yield return new WaitUntil(() =>
                _context.AliveEnemyCount <= 0 ||
                _context.State == BattleState.BattleDefeat);
        }
    }

    // ── 내부 ─────────────────────────────────────────────────

    // StageClear 트리거를 가진 특성의 스택을 장군별 UnitEntry 에 기록한다.
    // 실제 StatusEffect 적용은 다음 스테이지 시작 시 GeneralRuntimeBridge.OnEntityReset 에서 처리.
    void ApplyStageClearTraitStacks()
    {
        var runData = UserDataManager.Instance?.Get<RunTraitData>();
        if (runData == null) return;

        var traitDb = TraitDatabase.Current;
        var unitData = UserDataManager.Instance.Get<UnitData>();

        // StageClear 트리거 특성 목록 수집
        var stageClearTraits = new System.Collections.Generic.List<(TraitType type, int maxStacks)>();
        foreach (var t in runData.AcquiredTraits)
        {
            var td = traitDb?.Get(t);
            if (td?.StackTrigger == PassiveTrigger.StageClear)
                stageClearTraits.Add((t, td.MaxStacks));
        }
        if (stageClearTraits.Count == 0) return;

        // 배치된 각 장군의 UnitEntry 에 스택 추가
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        var em    = world.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<BattleGame.Units.GeneralComponent>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        query.Dispose();

        bool changed = false;
        foreach (var entity in entities)
        {
            if (!em.HasComponent<BattleGame.Units.UnitPoolLinkComponent>(entity)) continue;
            var link = em.GetComponentObject<BattleGame.Units.UnitPoolLinkComponent>(entity);
            var entry = unitData?.GetUnit(link?.PoolKey);
            if (entry == null) continue;

            foreach (var (t, maxStacks) in stageClearTraits)
                changed |= entry.IncrementTraitStack(t, 1, maxStacks) > 0;
        }

        if (changed) UserDataManager.Instance.RequestSave();
    }

    // OnBattleStart 트리거를 보유한 모든 장군의 어빌리티·패시브를 일괄 발동한다.
    // 특정 타입을 직접 참조하지 않으므로 새 OnBattleStart 스킬 추가 시 자동 지원.
    void FireBattleStartTriggers()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        var em    = world.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<BattleGame.Units.GeneralComponent>());

        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var entity in entities)
        {
            var ctx = new PassiveTriggerContext { GeneralEntity = entity, EntityManager = em };

            // ── 어빌리티 (GeneralTriggerSetComponent) ───────────
            if (em.HasComponent<BattleGame.Units.GeneralTriggerSetComponent>(entity))
            {
                var trigSet = em.GetComponentObject<BattleGame.Units.GeneralTriggerSetComponent>(entity);
                if (trigSet?.TriggerAbilities != null)
                {
                    foreach (var ability in trigSet.TriggerAbilities)
                    {
                        if (ability == null) continue;
                        if (ability.GetTriggerType() == PassiveTrigger.OnBattleStart)
                            ability.OnTrigger(ctx);
                    }
                }
            }

            // ── 패시브 스킬 (GeneralPassiveSetComponent) ────────
            if (em.HasComponent<BattleGame.Units.GeneralPassiveSetComponent>(entity))
            {
                var ps = em.GetComponentData<BattleGame.Units.GeneralPassiveSetComponent>(entity);
                var db = PassiveSkillDatabase.Current;
                if (db != null)
                {
                    TryFirePassiveOnBattleStart(db.Get(ps.Slot0), ctx, ps.ActiveSlotCount >= 1);
                    TryFirePassiveOnBattleStart(db.Get(ps.Slot1), ctx, ps.ActiveSlotCount >= 2);
                    TryFirePassiveOnBattleStart(db.Get(ps.Slot2), ctx, ps.ActiveSlotCount >= 3);
                }
            }
        }
        query.Dispose();
    }

    static void TryFirePassiveOnBattleStart(PassiveSkillData data, PassiveTriggerContext ctx, bool active)
    {
        if (!active || data == null) return;
        if (data.TriggerType != PassiveTrigger.OnBattleStart) return;
        data.OnTrigger(ctx);
    }

    /// <summary>
    /// 즉시 패배 처리 — 일시 정지 팝업의 "즉시 환생하기" 가 부른다.
    /// 아군 전멸과 같은 경로를 타므로 결과 팝업·통계·보상 처리가 전부 동일하다.
    /// 이미 승패가 갈렸으면 무시한다 (결과 팝업이 두 번 뜨는 것을 막는다).
    /// </summary>
    public void Surrender()
    {
        if (_context == null) return;
        if (_context.State == BattleState.BattleDefeat ||
            _context.State == BattleState.BattleVictory) return;

        _context.State = BattleState.BattleDefeat;
        Debug.Log($"[BattleManager] 포기 — 웨이브 {_context.CurrentWave}/{_context.TotalWaves}");
        LogBattleStats("포기");
        _mode?.OnBattleDefeat();
        OnDefeat?.Invoke();
    }

    /// <summary>아군 전멸 시 패배를 판정한다. 웨이브 클리어는 BattleRoutine 이 처리.</summary>
    void EvaluateBattleState()
    {
        if (_context.State != BattleState.InWave) return;

        if (_context.IsAllyDefeated)
        {
            _context.State = BattleState.BattleDefeat;
            Debug.Log($"[BattleManager] 패배 — 웨이브 {_context.CurrentWave}/{_context.TotalWaves}" +
                      $"  아군 생존: {_context.AliveAllyCount}" +
                      $"  적군 잔존: {_context.AliveEnemyCount}");
            LogBattleStats("패배");
            _mode.OnBattleDefeat();
            OnDefeat?.Invoke();
        }
    }

    // ── 전투 통계 로그 ────────────────────────────────────────

    static void LogBattleStats(string result)
    {
        var tracker = BattleStatsTracker.Instance;
        if (tracker == null) return;

        var entries = tracker.GetAllEntries();
        if (entries == null || entries.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[BattleStats] ===== 전투 결과 [{result}] =====");

        foreach (var e in entries)
        {
            sb.AppendLine($"  ▶ {e.GeneralName}");
            sb.AppendLine($"    딜량   총:{e.TotalDamageDealt:F0}" +
                          $"  장군:{e.GeneralDamageDealt:F0}" +
                          $"  병사:{e.SoldierDamageDealt:F0}" +
                          $"  스킬:{e.SkillDamageDealt:F0}");
            sb.AppendLine($"    피해량 장군:{e.DamageTaken:F0}" +
                          $"  병사:{e.SoldierDamageTaken:F0}" +
                          $"  방어감소:{e.DamageAbsorbed:F0}");
            sb.AppendLine($"    처치:{e.KillCount}  힐(가한):{e.HealingDone:F0}  힐(받은):{e.HealingReceived:F0}");
        }

        sb.Append("=========================================");
        Debug.Log(sb.ToString());
    }

    // ── 유닛 전체 정리 ────────────────────────────────────────

    /// <summary>
    /// 진행 중인 배틀을 즉시 종료하고 모든 유닛을 풀로 반납한 뒤 ECS 엔티티를 파괴한다.
    /// 로비 복귀 시 LobbyManager.ReturnToLobby() 에서 호출.
    /// </summary>
    public void DespawnAllUnits()
    {
        StopAllCoroutines();
        _context = null;
        _mode    = null;

        // ① ECS UnitPoolLinkComponent 를 통해 활성 유닛 GO 풀 반납
        var world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            var em    = world.EntityManager;
            em.CompleteAllTrackedJobs();  // 실행 중인 Burst Job 완료 대기
            var query = em.CreateEntityQuery(
                new ComponentType[] { ComponentType.ReadOnly<BattleGame.Units.UnitPoolLinkComponent>() });

            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var link = em.GetComponentObject<BattleGame.Units.UnitPoolLinkComponent>(entity);
                if (link?.LinkedObject != null)
                    PoolController.Instance?.Despawn(link.LinkedObject);
            }
            query.Dispose();

            // ② 모든 유닛 ECS 엔티티 파괴 (UnitIdentityComponent 기준)
            var unitQuery = em.CreateEntityQuery(
                new ComponentType[] { ComponentType.ReadOnly<BattleGame.Units.UnitIdentityComponent>() });
            em.DestroyEntity(unitQuery);
            unitQuery.Dispose();
        }

        // ③ 풀에서 놓친 유닛 브릿지 — 안전망으로 활성 브릿지 전부 회수
        foreach (var bridge in Object.FindObjectsByType<UnitRuntimeBridge>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            PoolController.Instance?.Despawn(bridge.gameObject);
        }
    }

    void CommitPendingRewards()
    {
        if (_context.PendingRewards.Count == 0) return;
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null) return;
        items.AddBatch(_context.PendingRewards);
        UserDataManager.Instance.RequestSave();
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
