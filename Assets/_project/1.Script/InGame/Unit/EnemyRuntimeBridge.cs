using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  EnemyRuntimeBridge.cs
//  적 프리팹(Enemy / Elite / Boss) 전용 RuntimeBridge.
//
//  EnemySpawner 가 스폰 직후 Initialize(unitName, unitType, race, level, statMult) 를
//  호출하면 EnemyStatRoller 로 스텟을 생성하고 ECS Entity 를 만든다.
//
//  엘리트: 스킬 ID를 unitName 시드로 결정, GeneralActiveSkillComponent 추가
//  보스:   AoE 반경·넉백·돌진 패턴 필드 초기화
// ============================================================

public class EnemyRuntimeBridge : UnitRuntimeBridge
{
    // 엘리트가 사용할 수 있는 스킬 ID 목록 (단순 공격계 스킬만 선택)
    static readonly int[] EliteSkillPool =
    {
        (int)ActiveSkillId.HeavyStrike,   // 1  강타
        (int)ActiveSkillId.LeapStrike,    // 3  도약강타
        (int)ActiveSkillId.Bind,          // 12 속박
        (int)ActiveSkillId.Berserker,     // 14 광전사
        (int)ActiveSkillId.IronShield,    // 15 철벽방어
        (int)ActiveSkillId.Shockwave,     // 18 충격파
    };

    SpawnUnitType _unitType;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>EnemySpawner 가 스폰 직후 호출.</summary>
    public void Initialize(string unitName, SpawnUnitType unitType, EnemyRace race,
                           int level = 1, float statMultiplier = 1f)
    {
        _unitName = unitName;
        _unitType = unitType;
        _stat     = EnemyStatRoller.Roll(unitName, unitType, level, statMultiplier);

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

    protected override void AddComponents(EntityManager em, Entity entity)
    {
        switch (_unitType)
        {
            case SpawnUnitType.Boss:
                em.AddComponentData(entity, new BossComponent
                {
                    PhaseCount             = 1,
                    CurrentPhase           = 1,
                    Phase2HpRatio          = 0.5f,
                    Phase3HpRatio          = 0.25f,
                    CCResistance           = 1f,
                    KnockbackResistance    = 0.8f,  // 완전 면역 → 약간 허용
                    // AoE 공격 설정
                    AoeRadius              = 2.5f,
                    AoeSplashRatio         = 0.6f,  // 범위 내 60% 피해
                    AttackKnockbackForce   = 4.0f,
                    AttackKnockbackDuration= 0.25f,
                    // 돌진 패턴 설정
                    ChargePatternCooldown  = 8f,
                    ChargePatternTimer     = 4f,    // 첫 돌진은 4초 후
                    ChargeSpeedMult        = 2.5f,
                    ChargeDuration         = 1.2f,
                    ChargeDamageBonus      = 0.5f,  // 돌진 중 공격력 +50%
                });
                break;

            case SpawnUnitType.Elite:
                // unitName 시드로 스킬 결정 (결정적 랜덤 — 같은 이름은 같은 스킬)
                int skillIndex = Mathf.Abs(_unitName.GetHashCode()) % EliteSkillPool.Length;
                int skillId    = EliteSkillPool[skillIndex];

                em.AddComponentData(entity, new EliteComponent
                {
                    HasSkill            = true,
                    KnockbackResistance = 0.5f,
                });
                em.AddComponentData(entity, new GeneralActiveSkillComponent
                {
                    SkillId           = skillId,
                    EffectValue       = 1.5f,   // 스킬 효과 수치 (스킬마다 의미가 다름)
                    EffectRadius      = 2.0f,
                    EffectDuration    = 3.0f,
                    Cooldown          = 12f,    // 12초마다 스킬 사용
                    CooldownRemaining = 5f,     // 처음 5초 후 첫 발동
                });
                // ActiveSkillCooldownSystem 이 AppendToBuffer 하므로 버퍼 미리 추가
                em.AddBuffer<ActiveSkillExecuteEvent>(entity);
                break;
        }
    }
}
