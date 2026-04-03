using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  EnemyRuntimeBridge.cs
//  적 프리팹(Enemy / Elite / Boss)에 붙는 런타임 초기화 브릿지.
//
//  역할:
//  1. EnemySpawner 가 스폰 직후 Initialize(unitName, unitType) 호출
//  2. EnemyStatRoller 로 UnitName 기반 결정적 랜덤 스텟 생성
//  3. Start() 에서 EntityLink 를 통해 Entity 를 찾아 ECS 스텟 적용
//
//  부착 위치:
//    Enemy / Elite / Boss 프리팹 루트 (EntityLink 와 함께)
// ============================================================

public class EnemyRuntimeBridge : MonoBehaviour
{
    string        _unitName;
    SpawnUnitType _unitType;
    UnitStat      _rolledStat;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>EnemySpawner 가 스폰 직후 호출.</summary>
    public void Initialize(string unitName, SpawnUnitType unitType)
    {
        _unitName   = unitName;
        _unitType   = unitType;
        _rolledStat = EnemyStatRoller.Roll(unitName, unitType);
    }

    // ── Unity 생명주기 ────────────────────────────────────────

    void OnEnable()
    {
        _unitName   = null;
        _rolledStat = null;
    }

    void Start()
    {
        if (string.IsNullOrEmpty(_unitName)) return;

        if (!TryGetComponent<EntityLink>(out var entityLink))
        {
            Debug.LogWarning($"[EnemyRuntimeBridge:{_unitName}] EntityLink 없음. 프리팹에 추가하세요.");
            return;
        }

        ApplyToEntity(entityLink.Entity);
    }

    // ── ECS 적용 ─────────────────────────────────────────────

    void ApplyToEntity(Entity entity)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        EntityManager em = world.EntityManager;
        if (entity == Entity.Null || !em.Exists(entity)) return;

        if (em.HasComponent<StatComponent>(entity))
        {
            StatBlock block = StatBlock.FromUnitStat(_rolledStat);
            em.SetComponentData(entity, new StatComponent { Base = block, Final = block });
        }

        if (em.HasComponent<HealthComponent>(entity))
        {
            em.SetComponentData(entity, new HealthComponent
            {
                CurrentHp = _rolledStat.Get(StatType.MaxHp),
            });
        }

        Debug.Log($"[EnemyRuntimeBridge] '{_unitName}'({_unitType}) 스텟 적용 " +
                  $"| HP:{_rolledStat.Get(StatType.MaxHp):F0} " +
                  $"ATK:{_rolledStat.Get(StatType.Attack):F0} " +
                  $"DEF:{_rolledStat.Get(StatType.Defense):P0}");
    }
}
