using System.Collections;
using System.Collections.Generic;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
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
    bool           _prepared;            // PrepareRoutine 완료 여부 — 웨이브 개시 대기 조건
    bool           _wavesStarted;        // 웨이브 루프 중복 개시 방지
    bool           _wave1AlliesSpawned;  // PrepareRoutine 에서 웨이브1 아군 선스폰 여부

    // ── UI 이벤트 ─────────────────────────────────────────────

    /// <summary>유닛 사망 시 팀 정보를 전달하는 이벤트. TopBarUI 킬 카운터 등에서 구독.</summary>
    public static event System.Action<TeamType> OnUnitKilled;

    /// <summary>웨이브 1 아군(장군) 스폰 완료. InGameManager 가 로딩 팝업 닫기에 사용.</summary>
    public static event System.Action OnAlliesReady;

    /// <summary>
    /// 웨이브가 실제로 시작될 때 발행.
    /// ⚠ OnAlliesReady 와 구분해야 한다 — 출전 대기 화면에서도 아군은 서므로
    ///   그 신호로 전투 연출(카메라 추종 등)을 켜면 대기 중에 카메라를 빼앗긴다.
    /// </summary>
    public static event System.Action OnWavesStarted;

    /// <summary>
    /// 새 전투 준비 직전에 발행 — 아군이 스폰되기 전이다.
    /// ⚠ 씬이 상주하면서 필요해졌다
    ///   예전엔 전투가 끝날 때마다 InGame 씬이 통째로 내려가 HUD 도 새로 만들어졌다.
    ///   지금은 씬이 계속 살아 있으므로, 지난 전투의 흔적을 여기서 스스로 지워야 한다.
    /// </summary>
    public static event System.Action OnBattlePrepared;

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

    /// <summary>
    /// 웨이브가 실제로 돌고 있는가 (적이 나오는 구간).
    ///
    /// ⚠ 출전 대기와 전투를 가르는 값이다
    ///   대기 중에는 아군만 서 있고 카메라도 옆으로 밀려 있다. 전투용 규칙
    ///   (화면 밖 사망 판정 등)을 그대로 적용하면 방금 세운 부대가 정리돼 버린다.
    /// </summary>
    public bool IsWaveRunning { get; private set; }

    /// <summary>적군이 전멸했는지 여부(웨이브 클리어). ECS 시스템에서 프레임마다 읽는다.</summary>
    public bool IsEnemyDefeated => _context?.IsEnemyClear ?? false;

    // ── Unity 생명주기 ─────────────────────────────────────────

    /// <summary>배틀을 시작한다 — 대기 준비와 웨이브 개시를 이어서 실행한다.</summary>
    public void StartBattle(BattleModeBase mode)
    {
        PrepareBattle(mode);
        StartCoroutine(BeginWavesWhenReady());
    }

    /// <summary>
    /// 아군만 세워 두는 '출전 대기' 상태까지 준비한다. 적은 나오지 않는다.
    ///
    /// ⚠ 여기서 멈출 수 있어야 출전 화면이 성립한다
    ///   화면에 적이 없으면 UnitMovementSystem 이 아군을 Idle 로 세워 두므로
    ///   (아군 + 타겟 없음 + 적 미등장 → 정지), 준비만 해 두고 기다려도
    ///   장수들이 제자리에 서 있는 그림이 그대로 나온다.
    /// </summary>
    public void PrepareBattle(BattleModeBase mode)
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

        _prepared     = false;
        _wavesStarted = false;

        // 아군을 스폰하기 전에 알린다 — 듣는 쪽이 지난 전투 잔재를 지울 기회
        OnBattlePrepared?.Invoke();

        StartCoroutine(PrepareRoutine());
    }

    /// <summary>대기 중이던 전투의 웨이브를 시작한다. PrepareBattle 이 끝난 뒤에 부를 것.</summary>
    public void BeginWaves()
    {
        if (_context == null)
        {
            Debug.LogWarning("[BattleManager] 준비된 전투가 없습니다 — PrepareBattle 을 먼저 호출하세요.");
            return;
        }
        StartCoroutine(BeginWavesWhenReady());
    }

    IEnumerator BeginWavesWhenReady()
    {
        // ⚠ 두 번 들어오면 웨이브가 두 겹으로 돈다
        //   StartBattle(직접 진입)과 BeginWaves(대기 → 시작)가 같은 문을 쓰므로
        //   여기서 한 번만 통과시킨다.
        if (_wavesStarted) yield break;
        _wavesStarted = true;

        while (!_prepared) yield return null;
        yield return StartCoroutine(WaveRoutine());
    }

    // 웨이브1 아군을 즉시 스폰해 '대기' 상태를 만든다. 적은 아직 없다.
    IEnumerator PrepareRoutine()
    {
        // ── 지난 스테이지 외형 캐시 회수 ────────────────────────
        // 적 외형 키는 스테이지 번호(S{n}W{w}E)를 물고 있어 스테이지가 바뀌면 전부 갈린다.
        // 로딩 팝업이 떠 있는 지금 놓아줘야 전투 중에 언로드 스파이크가 안 생긴다.
        CharacterBuilder.ClearSharedCache();
        yield return Resources.UnloadUnusedAssets();

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

        // 특성 이펙트 프리웜 — 적군 프리웜과 같은 이유로 전투 시작 전에 채운다
        PrewarmTraitEffects(ally1 is { Count: > 0 } ? CountUnits(ally1) : 0);

        _prepared = true;
    }

    // 적군 프리웜 → 웨이브 루프.
    IEnumerator WaveRoutine()
    {
        IsWaveRunning = true;
        OnWavesStarted?.Invoke();

        // ⚠ 전투 시작 트리거는 '웨이브가 시작될 때' 터져야 한다
        //   출전 대기 화면에서 미리 터뜨리면 버프 지속시간이 대기 중에 흘러
        //   정작 첫 웨이브에서는 절반만 남는다.
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
                // 최종 스테이지는 클리어되지 않는다 — 보스를 잡을 때마다 더 강한 보스가 나온다
                if (_mode.IsEndless)
                {
                    yield return StartCoroutine(EndlessBossRoutine());
                    yield break;
                }

                ApplyStageClearTraitStacks();
                _mode.ApplyStageClearReward();
                _context.State = BattleState.BattleVictory;
                IsWaveRunning  = false;
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

    // ── 무한 보스 루틴 (최종 스테이지) ────────────────────────
    //
    //  보스를 잡을 때마다 스텟 ×10, 크기 ×2 인 다음 보스가 다시 나온다.
    //  승리 조건이 없으므로 아군 전멸(또는 포기)로만 끝난다.
    //
    //  ⚠ State 를 InWave 로 되돌려야 한다 — EvaluateBattleState 의 패배 판정이
    //    InWave 상태에서만 돌기 때문에, WaveClear 인 채로 두면 전멸해도 패배가 안 뜬다.
    IEnumerator EndlessBossRoutine()
    {
        int bossIndex = 0;

        while (true)
        {
            bossIndex++;
            _context.EndlessBossIndex = bossIndex;

            List<SpawnEntry> entries = _mode.GetEndlessBossEntries(bossIndex);
            EnemySpawner.Spawn(entries);
            _context.AliveEnemyCount += CountUnits(entries);
            _context.State = BattleState.InWave;

            Debug.Log($"[BattleManager] 무한 보스 {bossIndex} 등장 " +
                      $"(스텟 ×{entries[0].StatMultiplier:G3}, 크기 ×{entries[0].ScaleMultiplier})");

            // 잔존 적까지 전부 정리해야 다음 보스가 나온다
            yield return new WaitUntil(() =>
                (!EnemySpawner.IsSpawning && _context.AliveEnemyCount <= 0) ||
                _context.State == BattleState.BattleDefeat);

            if (_context.State == BattleState.BattleDefeat)
                yield break;
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
            IsWaveRunning  = false;
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
        // 유닛을 다 치우는 시점 = 웨이브는 끝났다.
        // 승리·패배·판 닫기 어느 경로로 왔든 여기를 지나므로 한 곳에서 내린다.
        IsWaveRunning = false;

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
    // 호위 병사(EscortCount)까지 포함한 실제 스폰 수.
    // 생존 카운트는 웨이브 시작 시 여기서 한 번에 더해지므로, 호위를 빼먹으면
    // 호위가 아직 살아 있는데 카운트가 0 이 되어 웨이브가 먼저 끝난다.
    static int CountUnits(List<SpawnEntry> entries)
    {
        int total = 0;
        foreach (SpawnEntry entry in entries)
            total += entry.Count * (1 + entry.EscortCount);
        return total;
    }

    // ── 특성 이펙트 프리웜 ────────────────────────────────────
    //  트리거 특성의 이펙트는 전투가 시작되자마자 여러 개가 동시에 필요하다.
    //  첫 발동 때 Instantiate 가 몰려 프레임이 튀지 않도록 미리 채워 둔다.
    //  새 특성 이펙트가 늘어나면 여기에 한 줄씩 추가한다.
    static void PrewarmTraitEffects(int generalCount)
    {
        if (generalCount <= 0) return;

        var traits = UserDataManager.Instance?.Get<RunTraitData>();
        if (traits == null) return;

        // 폭우 사격은 장군뿐 아니라 병사도 발동한다 → 아군 유닛 총량 기준으로 채운다.
        if (traits.HasTrait(TraitType.ArcherRainFire))
            RainFireSplash.Prewarm(CountAllyUnits(generalCount));
    }

    /// <summary>장군 + 그 장군들이 데리고 나가는 병사 수. 전투 시작 시 1회만 계산한다.</summary>
    static int CountAllyUnits(int generalCount)
    {
        int total = generalCount;

        var deploy = UserDataManager.Instance?.Get<DeploymentData>();
        var units  = UserDataManager.Instance?.Get<UnitData>();
        if (deploy == null || units == null) return total;

        foreach (string name in deploy.GetDeployedUnits())
        {
            var entry = units.GetUnit(name);
            if (entry == null) continue;
            total += Mathf.Max(0, Mathf.RoundToInt(
                HeroStatResolver.Resolve(entry).Total(StatType.SoldierCount)));
        }
        return total;
    }


}
