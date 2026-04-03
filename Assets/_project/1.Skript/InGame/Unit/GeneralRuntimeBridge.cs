using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  GeneralRuntimeBridge.cs
//  장군 프리팹에 붙는 런타임 초기화 브릿지 MonoBehaviour.
//
//  역할:
//  1. AllySpawner 가 스폰 직후 Initialize(unitName) 호출
//  2. GeneralStatRoller 로 UnitName 기반 결정적 랜덤 스텟 생성
//  3. Start() 에서 EntityReference 를 통해 Entity 를 찾아 ECS 스텟 적용
//
//  부착 위치:
//    General 유닛 프리팹 루트 (EntityReference 와 함께)
// ============================================================

public class GeneralRuntimeBridge : MonoBehaviour
{
    string   _unitName;
    UnitStat _rolledStat;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>AllySpawner 가 스폰 직후 호출.</summary>
    public void Initialize(string unitName)
    {
        _unitName   = unitName;
        _rolledStat = GeneralStatRoller.Roll(unitName);
    }

    public UnitStat GetRolledStat() => _rolledStat;

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
            Debug.LogWarning($"[GeneralRuntimeBridge:{_unitName}] EntityLink 없음. 프리팹에 추가하세요.");
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

        if (em.HasComponent<SpawnSoldiersRequest>(entity))
        {
            SpawnSoldiersRequest req = em.GetComponentData<SpawnSoldiersRequest>(entity);
            req.Count          = (int)_rolledStat.Get(StatType.SoldierCount);
            req.StatScaleRatio *= 1f + _rolledStat.Get(StatType.CommandPower) * 0.01f;
            em.SetComponentData(entity, req);
        }

        Debug.Log($"[GeneralRuntimeBridge] '{_unitName}' 스텟 적용 완료 " +
                  $"| HP:{_rolledStat.Get(StatType.MaxHp):F0} " +
                  $"ATK:{_rolledStat.Get(StatType.Attack):F0} " +
                  $"병사:{_rolledStat.Get(StatType.SoldierCount):F0} " +
                  $"지휘:{_rolledStat.Get(StatType.CommandPower):F0}");
    }
}
