using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  LobbyDemoBattle.cs
//  MainPanel 배경에서 도는 '보여 주기용' 전투.
//
//  ■ 실전과 완전히 무관하다
//    · BattleManager 를 쓰지 않는다 — 그쪽은 웨이브 진행·보상 커밋·승패 팝업·
//      통계·세이브 쓰기를 전부 물고 있어서, 배경 연출이 골드를 주고
//      스테이지를 클리어시키는 사고가 난다. 스포너만 빌려 쓴다.
//    · 아군은 보유 장수가 아니라 이름 풀에서 아무나 뽑은 더미다.
//      UnitData 에 없는 이름이라 GeneralRuntimeBridge 가 장비·강화를 건너뛴다.
//    · 저장 데이터에 한 글자도 쓰지 않는다.
//
//  ■ 그림의 목표
//    아군 한 파티가 끊임없이 밀려오는 적을 잡으면서 오른쪽으로 전진한다.
//    · 적은 항상 8기가 화면에 있도록 채운다 (죽는 즉시 보충)
//    · 아군은 장군·병사 모두 죽지 않는다 — InvulnerableTag 로 사망 자체를 막는다
//    · 전진·배경 루프는 BattleScrollManager 가 이미 하던 일을 그대로 쓴다
//
//  ■ 아군이 왜 저절로 전진하나
//    UnitMovementSystem 규칙: 아군은 타겟이 없어도 '적이 화면에 있으면' +X 로 전진하고,
//    적이 하나도 없으면 제자리에 선다. 적을 계속 채워 주는 것만으로 전진이 유지된다.
//
//  수명: MainPanelUI 가 켜질 때 Begin(), 꺼질 때 End().
// ============================================================

public class LobbyDemoBattle : MonoBehaviour
{
    // ── 연출 수치 ────────────────────────────────────────────

    [Tooltip("화면에 유지할 적 수")]
    [SerializeField] int _enemyCount = 8;

    [Tooltip("적 보충 주기 (초). 너무 짧으면 스폰이 뭉쳐 들어온다.")]
    [SerializeField] float _refillInterval = 1.2f;

    [Tooltip("아군 불사 처리 주기 (초). 새로 나온 병사에 태그를 달고 체력을 채운다.")]
    [SerializeField] float _immortalInterval = 0.2f;

    [Tooltip("데모 적 레벨 — 아군을 위협할 필요가 없으므로 낮게 둔다.")]
    [SerializeField] int _enemyLevel = 1;

    bool      _running;
    Coroutine _beginRoutine;   // 시작 코루틴 — End 가 도중에 끊을 수 있어야 한다

    EntityQuery _unitQuery;
    bool        _queryReady;

    // ── 공개 API ─────────────────────────────────────────────

    public bool IsRunning => _running;

    static LobbyDemoBattle _instance;

    /// <summary>이미 있는 것만 돌려준다 (없으면 null). 정리 경로에서 쓸 것.</summary>
    public static LobbyDemoBattle Instance => _instance;

    /// <summary>씬에 배치하지 않아도 쓸 수 있게 필요할 때 만든다.</summary>
    public static LobbyDemoBattle Ensure()
    {
        if (_instance != null) return _instance;

        var go = new GameObject(nameof(LobbyDemoBattle));
        DontDestroyOnLoad(go);
        return _instance = go.AddComponent<LobbyDemoBattle>();
    }

    void Awake()
    {
        if (_instance == null) _instance = this;
    }

    public void Begin()
    {
        if (_running || _beginRoutine != null) return;

        // ⚠ '전투로 들어가는 중' 일 때만 막는다
        //   최초 실행 자동 진입과 MainPanel 의 OnEnable 이 겹치면 데모 더미가
        //   실전 전장에 남는다 (장수 카드가 세 장이 되던 그 증상).
        //   다만 BattlePanel 의 출전 대기도 Real 판이라, 판 종류로 막으면
        //   BattlePanel 을 한 번 거친 뒤로는 데모가 영영 안 켜진다.
        if (LobbyManager.Instance != null && !LobbyManager.Instance.CanRunDemo) return;

        _beginRoutine = StartCoroutine(BeginRoutine());
    }

    public void End()
    {
        // ⚠ 아직 시작 중인 것도 취소해야 한다
        //   BeginRoutine 은 씬 상주를 기다리며 몇 프레임 떠 있다. 그 사이에 End 가
        //   들어오면 _running 은 아직 false 라 예전 코드는 그냥 돌아갔고,
        //   잠시 뒤 코루틴이 깨어나 더미를 스폰했다.
        if (_beginRoutine != null)
        {
            StopCoroutine(_beginRoutine);
            _beginRoutine = null;
        }

        if (!_running) return;
        _running = false;
        StopAllCoroutines();

        ClearImmortality();
        BattleScrollManager.Instance?.StopScroll();

        // ⚠ 판은 여기서 닫지 않는다
        //   전장을 비우는 곳은 LobbyFlow.Returning 하나뿐이다.
        //   여기서 닫으면 실전 대기 부대까지 쓸어갈 위험이 생긴다.
        LobbyManager.Instance?.NotifyDemoRunning(false);
    }

    void OnDisable() => End();

    // ── 시작 ─────────────────────────────────────────────────

    IEnumerator BeginRoutine()
    {
        var director = SceneDirector.Ensure();

        // 데모는 인게임 씬의 스포너·풀·배경을 빌려 쓴다 — 먼저 상주시킨다.
        yield return director.EnsureInGameResident();

        // 기다리는 동안 전투로 넘어갔을 수 있다 — 그러면 조용히 물러난다
        if (LobbyManager.Instance != null && !LobbyManager.Instance.CanRunDemo)
        { _beginRoutine = null; yield break; }

        // 판 열기와 화면 전환은 흐름이 한다 (Demo 상태가 Arena·Present 를 함께 맞춘다)
        _running = true;
        LobbyManager.Instance?.NotifyDemoRunning(true);

        if (!BattleArena.Ensure().IsDemo) { _running = false; _beginRoutine = null; yield break; }

        SpawnDummyParty();

        // 아군이 자리를 잡은 다음 프레임부터 적을 붓는다
        yield return null;

        BattleScrollManager.Instance?.StartScroll();

        StartCoroutine(RefillLoop());
        StartCoroutine(ImmortalLoop());

        _beginRoutine = null;   // 시작 완료 — 이후 정리는 _running 이 맡는다
    }

    // ── 아군 더미 파티 ───────────────────────────────────────

    void SpawnDummyParty()
    {
        var spawner = BattleManager.Instance?.AllySpawner;
        if (spawner == null)
        {
            Debug.LogWarning("[LobbyDemoBattle] AllySpawner 를 찾지 못했습니다.");
            return;
        }

        // 이름 풀 전체에서 아무나 — 보유 여부와 무관한 고정 목록이라 세이브와 얽히지 않는다.
        var pool = UnitData.AllNames;
        string name = pool[Random.Range(0, pool.Count)];

        spawner.SpawnImmediate(new List<SpawnEntry>
        {
            new SpawnEntry
            {
                Name     = name,
                Level    = Random.Range(8, 15),   // 병사를 여럿 달고 나오도록 적당히 높게
                UnitType = SpawnUnitType.General,
                Count    = 1,
            },
        });
    }

    // ── 적 보충 ──────────────────────────────────────────────

    IEnumerator RefillLoop()
    {
        var wait = new WaitForSeconds(_refillInterval);

        while (_running)
        {
            var spawner = BattleManager.Instance?.EnemySpawner;
            if (spawner != null && !spawner.IsSpawning)
            {
                int missing = _enemyCount - CountAlive(TeamType.Enemy);
                if (missing > 0) spawner.Spawn(BuildEnemyEntries(missing));
            }
            yield return wait;
        }
    }

    List<SpawnEntry> BuildEnemyEntries(int count)
    {
        var list = new List<SpawnEntry>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new SpawnEntry
            {
                // 이름이 외형·스텟 시드라 매번 달라야 같은 얼굴이 줄 서지 않는다
                Name         = $"DemoFoe{Random.Range(0, 99999)}",
                Level        = _enemyLevel,
                UnitType     = SpawnUnitType.Enemy,
                Count        = 1,
                DelayBefore  = 0f,
                DelayBetween = 0.15f,
                EnemyRace    = (EnemyRace)Random.Range(0, System.Enum.GetValues(typeof(EnemyRace)).Length),
            });
        }
        return list;
    }

    // ── 아군 불사 ────────────────────────────────────────────

    //  ⚠ 체력만 채우는 방식으로는 병사가 죽는다
    //    죽음은 UnitHitSystem 이 피해를 넣는 그 자리에서 확정된다. 장군은 체력 통이
    //    커서 0.2초를 버텼지만, 병사는 한 방에 통이 비어 다음 보충이 오기 전에 죽었다.
    //    → 사망 자체를 막는 InvulnerableTag 를 달고, 체력 보충은 '보기 좋으라고' 만 남긴다.
    //
    //  ⚠ 적 공격력을 0 으로 만드는 쪽은 채택하지 않았다
    //    적은 계속 새로 스폰되므로 매번 붙잡아 0 을 박아야 하고,
    //    독·자폭처럼 공격력을 거치지 않는 피해는 그래도 들어온다.
    IEnumerator ImmortalLoop()
    {
        var wait = new WaitForSeconds(_immortalInterval);

        while (_running)
        {
            MakeAlliesImmortal();
            yield return wait;
        }
    }

    /// <summary>
    /// 살아 있는 아군 전원(장군·병사·소환물)에 불사 태그를 달고 체력을 채운다.
    ///
    /// 병사는 장군을 따라 나중에 나오기도 하고 스킬로 새로 소환되기도 하므로
    /// 한 번 달고 끝낼 수 없다 — 매 틱 새로 들어온 아군을 훑는다.
    /// </summary>
    void MakeAlliesImmortal()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        if (!EnsureQuery(world)) return;

        var em = world.EntityManager;
        using var entities = _unitQuery.ToEntityArray(Allocator.Temp);

        // ⚠ 태그 부착은 구조 변경이라 청크를 흔든다 — 먼저 다 모아 두고 한 번에 붙인다.
        //   루프 도중에 붙이면 지금 돌고 있는 배열의 컴포넌트 접근이 어긋난다.
        var newcomers = new NativeList<Entity>(entities.Length, Allocator.Temp);

        foreach (var e in entities)
        {
            if (em.GetComponentData<UnitIdentityComponent>(e).Team != TeamType.Ally) continue;

            if (!em.HasComponent<InvulnerableTag>(e)) newcomers.Add(e);

            var stat   = em.GetComponentData<StatComponent>(e);
            var health = em.GetComponentData<HealthComponent>(e);

            float max = stat.Final[StatType.MaxHp];
            if (health.CurrentHp >= max) continue;

            health.CurrentHp = max;
            em.SetComponentData(e, health);
        }

        if (newcomers.Length > 0)
            em.AddComponent<InvulnerableTag>(newcomers.AsArray());

        newcomers.Dispose();
    }

    /// <summary>
    /// 데모가 끝나면 태그를 전부 걷는다.
    ///
    /// ⚠ 실전에 새어 나가면 아무도 안 죽는 전투가 된다
    ///   데모 더미는 판을 닫을 때 사라지지만, 판을 닫는 주체는 여기가 아니라
    ///   LobbyFlow.Returning 이다. 그 사이에 실전이 시작될 여지를 남기지 않는다.
    /// </summary>
    void ClearImmortality()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;

        var em    = world.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<InvulnerableTag>());
        em.RemoveComponent<InvulnerableTag>(query);
        query.Dispose();
    }

    // ── 조회 ─────────────────────────────────────────────────

    int CountAlive(TeamType team)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return 0;
        if (!EnsureQuery(world)) return 0;

        using var ids = _unitQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);

        int n = 0;
        foreach (var id in ids)
            if (id.Team == team) n++;
        return n;
    }

    /// <summary>
    /// 쿼리는 한 번만 만든다.
    /// ⚠ 매 틱 CreateEntityQuery 를 부르면 핸들이 계속 쌓인다 — 캐시가 규칙이다.
    ///   팀 구분용 태그 컴포넌트는 없다. UnitIdentityComponent.Team 으로 걸러야 한다.
    /// </summary>
    bool EnsureQuery(World world)
    {
        if (_queryReady) return true;

        _unitQuery = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitIdentityComponent>(),
            ComponentType.ReadOnly<StatComponent>(),
            ComponentType.ReadWrite<HealthComponent>(),
            ComponentType.Exclude<DeadTag>());

        _queryReady = true;
        return true;
    }
}
