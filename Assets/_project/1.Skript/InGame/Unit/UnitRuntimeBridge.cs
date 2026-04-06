using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  UnitRuntimeBridge.cs
//  모든 유닛 RuntimeBridge 의 공통 베이스 클래스.
//
//  Pool 은 Instantiate 기반이므로 Baker 가 실행되지 않는다.
//  이 클래스가 Start() 에서 ECS Entity 를 직접 생성하고 EntityLink 에 등록한다.
//
//  파생 클래스 구현 사항:
//    - GetTeam()          : 팀 타입 반환 (Ally / Enemy)
//    - GetUnitType()      : 유닛 타입 반환 (General / Enemy / Elite ...)
//    - AddComponents()    : (선택) 타입 전용 ECS 컴포넌트 추가
//    - Initialize(...)    : 스폰 직후 Spawner 에서 호출 — _unitName, _stat 설정
//
//  라이프사이클:
//    OnEnable  → 상태 초기화 (풀 재사용 대비)
//    Start     → Entity 생성 후 EntityLink 에 등록
//    (EntityLink.OnDisable 이 풀 반납 시 Entity 파괴 처리)
// ============================================================

public abstract class UnitRuntimeBridge : MonoBehaviour
{
    // 파생 클래스가 Initialize() 에서 설정
    protected string   _unitName;
    protected UnitStat _stat;

    // ── 파생 클래스가 반드시 구현 ────────────────────────────

    protected abstract TeamType GetTeam();
    protected abstract UnitType GetUnitType();

    /// <summary>기본 컴포넌트 추가 후 호출 — 타입 전용 컴포넌트를 여기서 추가.</summary>
    protected virtual void AddComponents(EntityManager em, Entity entity) { }

    /// <summary>Entity 재사용 시 호출 — 타입 전용 컴포넌트 값을 리셋할 때 오버라이드.</summary>
    protected virtual void OnEntityReset(EntityManager em, Entity entity) { }

    // ── Unity 생명주기 ────────────────────────────────────────

    protected virtual void OnEnable()
    {
        _unitName = null;
        _stat     = null;
    }

    /// <summary>
    /// 파생 클래스의 Initialize() 마지막에 호출.
    /// Entity 가 없으면 최초 생성, 있으면 상태값만 리셋해서 재사용한다.
    /// </summary>
    protected void SpawnEntity()
    {
        if (!TryGetComponent<EntityLink>(out var link))
        {
            Debug.LogWarning($"[{GetType().Name}:{_unitName}] EntityLink 없음. 프리팹에 추가하세요.");
            return;
        }

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null)
        {
            Debug.LogWarning($"[{GetType().Name}:{_unitName}] ECS World 없음.");
            return;
        }

        EntityManager em = world.EntityManager;
        em.CompleteAllTrackedJobs();

        if (link.Entity != Entity.Null && em.Exists(link.Entity))
        {
            // ── 재사용: 상태값 리셋 후 Disabled 제거 ─────────
            ResetEntity(em, link.Entity);
            if (em.HasComponent<Disabled>(link.Entity))
                em.RemoveComponent<Disabled>(link.Entity);
        }
        else
        {
            // ── 최초 생성 ─────────────────────────────────────
            Entity entity = CreateBaseEntity(em);
            AddComponents(em, entity);
            link.Entity = entity;
        }

    }

    // ── Entity 재사용 시 상태 리셋 ───────────────────────────

    void ResetEntity(EntityManager em, Entity entity)
    {
        Vector3   pos       = transform.position;
        StatBlock statBlock = StatBlock.FromUnitStat(_stat);

        em.SetComponentData(entity, LocalTransform.FromPosition(
            new float3(pos.x, pos.y, pos.z)));
        em.SetComponentData(entity, new UnitIdentityComponent
        {
            UnitId = 0,
            Team   = GetTeam(),
            Type   = GetUnitType(),
        });
        em.SetComponentData(entity, new StatComponent { Base = statBlock, Final = statBlock });
        em.SetComponentData(entity, new HealthComponent
        {
            CurrentHp = _stat.Get(StatType.MaxHp),
        });
        em.SetComponentData(entity, new MovementComponent
        {
            Velocity         = float3.zero,
            Destination      = float3.zero,
            StoppingDistance = 0.5f,
            IsMoving         = false,
        });
        em.SetComponentData(entity, new FormationSlotComponent
        {
            Row = 0, Column = 0, SlotPosition = float3.zero,
        });
        em.SetComponentData(entity, new AttackComponent
        {
            AttackCooldown = 0f,
            HasTarget      = false,
            RandomSeed     = (uint)UnityEngine.Random.Range(1, int.MaxValue),
        });
        em.SetComponentData(entity, new UnitStateComponent
        {
            Current = UnitState.Idle, Previous = UnitState.Idle, StateTimer = 0f,
        });
        em.SetComponentData(entity, new HitReactionComponent
        {
            KnockbackVelocity = float3.zero, StunDuration = 0f,
            StunTimer = 0f, IsStunned = false,
        });
        em.SetComponentData(entity, new GridCellComponent
        {
            Cell = int2.zero, PrevCell = int2.zero,
        });

        // 유닛 크기 갱신 (풀 재사용 시 스케일이 달라질 수 있음)
        Vector3 scale  = transform.localScale;
        float   radius = Mathf.Max(scale.x, scale.y) * 0.5f;
        float   mass   = GetUnitType() == UnitType.General ? 5f : 1f;
        em.SetComponentData(entity, new UnitSizeComponent { Radius = radius, Mass = mass });

        // 화면 진입 상태 초기화 (재스폰 시 다시 진입 판정)
        em.SetComponentData(entity, new ScreenStateComponent { HasEnteredScreen = false });

        // DeadTag 제거 (사망 상태로 반납된 경우 대비)
        if (em.HasComponent<DeadTag>(entity))
            em.RemoveComponent<DeadTag>(entity);

        // 풀 링크 갱신 — 사망 처리 시 UnitDeathDespawnSystem 이 제거하므로 없으면 재추가
        if (em.HasComponent<BattleGame.Units.UnitPoolLinkComponent>(entity))
        {
            var poolLink = em.GetComponentObject<BattleGame.Units.UnitPoolLinkComponent>(entity);
            poolLink.PoolKey      = _unitName;
            poolLink.LinkedObject = gameObject;
        }
        else
        {
            em.AddComponentObject(entity, new BattleGame.Units.UnitPoolLinkComponent
            {
                PoolKey      = _unitName,
                LinkedObject = gameObject,
            });
        }

        // 버퍼 클리어
        em.GetBuffer<HitEventBufferElement>(entity).Clear();
        em.GetBuffer<StatusEffectBufferElement>(entity).Clear();

        // 파생 클래스 전용 컴포넌트 리셋
        OnEntityReset(em, entity);
    }

    // ── 공통 Entity 생성 ─────────────────────────────────────

    Entity CreateBaseEntity(EntityManager em)
    {
        Entity entity = em.CreateEntity();

        // ── Transform ─────────────────────────────────────────
        Vector3 pos = transform.position;
        em.AddComponentData(entity, LocalTransform.FromPosition(
            new float3(pos.x, pos.y, pos.z)));

        // ── 식별 ──────────────────────────────────────────────
        em.AddComponentData(entity, new UnitIdentityComponent
        {
            UnitId = 0,
            Team   = GetTeam(),
            Type   = GetUnitType(),
        });

        // ── 스탯 ──────────────────────────────────────────────
        StatBlock statBlock = StatBlock.FromUnitStat(_stat);
        em.AddComponentData(entity, new StatComponent { Base = statBlock, Final = statBlock });

        // ── 체력 ──────────────────────────────────────────────
        em.AddComponentData(entity, new HealthComponent
        {
            CurrentHp = _stat.Get(StatType.MaxHp),
        });

        // ── 이동 ──────────────────────────────────────────────
        em.AddComponentData(entity, new MovementComponent
        {
            Velocity         = float3.zero,
            Destination      = float3.zero,
            StoppingDistance = 0.5f,
            IsMoving         = false,
        });
        em.AddComponentData(entity, new FormationSlotComponent
        {
            Row          = 0,
            Column       = 0,
            SlotPosition = float3.zero,
        });

        // ── 전투 ──────────────────────────────────────────────
        em.AddComponentData(entity, new AttackComponent
        {
            AttackCooldown = 0f,
            HasTarget      = false,
            RandomSeed     = (uint)UnityEngine.Random.Range(1, int.MaxValue),
        });
        em.AddComponentData(entity, new UnitStateComponent
        {
            Current    = UnitState.Idle,
            Previous   = UnitState.Idle,
            StateTimer = 0f,
        });
        em.AddComponentData(entity, new HitReactionComponent
        {
            KnockbackVelocity = float3.zero,
            StunDuration      = 0f,
            StunTimer         = 0f,
            IsStunned         = false,
        });
        em.AddComponentData(entity, new GridCellComponent
        {
            Cell     = int2.zero,
            PrevCell = int2.zero,
        });

        // ── 유닛 크기 (분리 반경 + 질량) ──────────────────────────────
        Vector3 scale  = transform.localScale;
        float   radius = Mathf.Max(scale.x, scale.y) * 0.5f;
        float   mass   = GetUnitType() == UnitType.General ? 5f : 1f;
        em.AddComponentData(entity, new UnitSizeComponent { Radius = radius, Mass = mass });

        // ── 화면 경계 ─────────────────────────────────────────
        em.AddComponentData(entity, new ScreenStateComponent { HasEnteredScreen = false });

        // ── 동적 버퍼 ─────────────────────────────────────────
        em.AddBuffer<HitEventBufferElement>(entity);
        em.AddBuffer<StatusEffectBufferElement>(entity);

        // ── 풀 반납 링크 (managed component) ──────────────────
        // UnitDeathDespawnSystem 이 DeadTag 감지 후 이 컴포넌트로 풀 반납
        em.AddComponentObject(entity, new BattleGame.Units.UnitPoolLinkComponent
        {
            PoolKey      = _unitName,
            LinkedObject = gameObject,
        });

        return entity;
    }
}
