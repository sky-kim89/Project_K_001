using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  GeneralRuntimeBridge.cs
//  장군 프리팹 전용 RuntimeBridge.
//
//  병사 스탯 비율:
//    statScaleRatio = 0.4f + CommandPower * 0.01f
//    CommandPower 1~30 기준 → 41%~70%
//
//  병사 진형 (세로 열):
//    전체 높이를 고정(FormationHeight)하고 병사 수로 나눠 간격 산출.
//    병사가 많을수록 자동으로 좁아짐 (최소 0.15, 최대 1.5).
// ============================================================

public class GeneralRuntimeBridge : UnitRuntimeBridge
{
    [Header("병사 설정")]
    [Tooltip("PoolController 에 등록된 병사 풀 키")]
    [SerializeField] string _soldierPoolKey = "Soldier";

    // 진형 최대 높이 — 이 안에 병사를 꽉 채움
    const float FormationHeight  = 15f;
    const float MaxSpacing       = 1.5f;
    const float MinSpacing       = 0.15f;

    int       _level;
    UnitGrade _grade;
    UnitJob   _job;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>AllySpawner 가 스폰 직후 호출. 등급은 가중치 랜덤으로 자동 결정.</summary>
    public void Initialize(string unitName, int level = 1)
    {
        _unitName = unitName;
        _level    = level;
        _grade    = UnitJobRoller.RollGrade();
        _job      = UnitJobRoller.GetJob(unitName);
        _stat     = GeneralStatRoller.Roll(unitName, _level, _grade);
        SpawnEntity();
        SpawnSoldiers();
    }

    /// <summary>외부에서 롤된 스탯을 읽을 때 사용.</summary>
    public UnitStat GetRolledStat() => _stat;

    // ── UnitRuntimeBridge 구현 ───────────────────────────────

    protected override TeamType GetTeam()     => TeamType.Ally;
    protected override UnitType GetUnitType() => UnitType.General;

    /// <summary>장군 전용 ECS 컴포넌트 추가 — 직업, 원거리 태그, 발사 요청 버퍼.</summary>
    protected override void AddComponents(EntityManager em, Entity entity)
    {
        em.AddComponentData(entity, new UnitJobComponent { Job = _job });

        if (_job == UnitJob.Archer || _job == UnitJob.Mage)
        {
            em.AddComponent<RangedTag>(entity);
            em.AddBuffer<ProjectileLaunchRequest>(entity);
        }
    }

    /// <summary>풀 재사용 시 발사 요청 버퍼 초기화.</summary>
    protected override void OnEntityReset(EntityManager em, Entity entity)
    {
        if (em.HasBuffer<ProjectileLaunchRequest>(entity))
            em.GetBuffer<ProjectileLaunchRequest>(entity).Clear();
    }

    // ── 병사 스폰 ─────────────────────────────────────────────

    void SpawnSoldiers()
    {
        if (string.IsNullOrEmpty(_soldierPoolKey)) return;

        int soldierCount = Mathf.RoundToInt(_stat.Get(StatType.SoldierCount));
        if (soldierCount <= 0) return;

        if (!TryGetComponent<EntityLink>(out var link) || link.Entity == Entity.Null)
        {
            Debug.LogWarning("[GeneralRuntimeBridge] 장군 Entity 없음 — 병사 스폰 취소");
            return;
        }

        // 병사 스탯 비율: 기본 40% + CommandPower 1포인트당 1%
        float commandPower    = _stat.Get(StatType.CommandPower);
        float statScaleRatio  = Mathf.Clamp01(0.4f + commandPower * 0.01f);

        // 병사 수에 따라 간격 자동 조정
        float spacing = soldierCount > 1
            ? Mathf.Clamp(FormationHeight / (soldierCount - 1), MinSpacing, MaxSpacing)
            : 0f;

        float startY = transform.position.y - (soldierCount - 1) * spacing * 0.5f;

        for (int i = 0; i < soldierCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                transform.position.x,
                startY + i * spacing,
                transform.position.z);

            GameObject soldierGO = PoolController.Instance?.Spawn(
                PoolType.Unit, _soldierPoolKey, spawnPos, Quaternion.identity);

            if (soldierGO == null)
            {
                Debug.LogWarning($"[GeneralRuntimeBridge] 병사 스폰 실패 (풀 키: '{_soldierPoolKey}')");
                continue;
            }

            if (soldierGO.TryGetComponent<SoldierRuntimeBridge>(out var soldier))
                soldier.Initialize(_soldierPoolKey, _stat, statScaleRatio, link.Entity, _job);
        }

        Debug.Log($"[GeneralRuntimeBridge] '{_unitName}' 스폰 " +
                  $"| Lv:{_level}  등급:{_grade}  직업:{_job}  " +
                  $"HP:{_stat.Get(StatType.MaxHp):F0}  ATK:{_stat.Get(StatType.Attack):F0}  " +
                  $"병사:{soldierCount}명  스탯비율:{statScaleRatio:P0}");
    }
}
