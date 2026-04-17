using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  EnemyRuntimeBridge.cs
//  적 프리팹(Enemy / Elite / Boss) 전용 RuntimeBridge.
//
//  EnemySpawner 가 스폰 직후 Initialize(unitName, unitType) 를 호출하면
//  EnemyStatRoller 로 스텟을 생성하고, Start() 에서 ECS Entity 를 만든다.
//  (Entity 생성 공통 로직은 UnitRuntimeBridge 베이스가 담당)
// ============================================================

public class EnemyRuntimeBridge : UnitRuntimeBridge
{
    SpawnUnitType _unitType;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>EnemySpawner 가 스폰 직후 호출.</summary>
    public void Initialize(string unitName, SpawnUnitType unitType, EnemyRace race)
    {
        _unitName = unitName;
        _unitType = unitType;
        _stat     = EnemyStatRoller.Roll(unitName, unitType);

        // 외형 적용 (ECS Entity 생성과 독립적으로 실행)
        GetComponent<UnitAppearanceBridge>()?.ApplyEnemy(race, unitName);

        SpawnEntity();
    }

    // ── UnitRuntimeBridge 구현 ───────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        _unitType = default;
    }

    protected override TeamType GetTeam() => TeamType.Enemy;

    protected override UnitType GetUnitType() => _unitType switch
    {
        SpawnUnitType.Elite => UnitType.Elite,
        SpawnUnitType.Boss  => UnitType.Boss,
        _                   => UnitType.Enemy,
    };

    // ── 타입 전용 컴포넌트 추가 ──────────────────────────────
    // BossComponent / EliteComponent 은 Authoring 없이 런타임 스폰되므로
    // 여기서 직접 추가해야 저항 등 타입별 로직이 동작한다.

    protected override void AddComponents(EntityManager em, Entity entity)
    {
        switch (_unitType)
        {
            case SpawnUnitType.Boss:
                em.AddComponentData(entity, new BossComponent
                {
                    PhaseCount          = 1,
                    CurrentPhase        = 1,
                    Phase2HpRatio       = 0.5f,
                    Phase3HpRatio       = 0.25f,
                    CCResistance        = 1f,
                    KnockbackResistance = 1f,
                });
                break;

            case SpawnUnitType.Elite:
                em.AddComponentData(entity, new EliteComponent
                {
                    HasSkill            = false,
                    KnockbackResistance = 0.5f,
                });
                break;
        }
    }
}
