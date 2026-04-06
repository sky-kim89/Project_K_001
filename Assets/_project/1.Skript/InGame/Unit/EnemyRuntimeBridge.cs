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
}
