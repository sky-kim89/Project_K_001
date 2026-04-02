using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ============================================================
//  UnitAuthoring.cs
//  모든 유닛 Authoring 의 공통 베이스 클래스 + 공통 Baker
//
//  파생 클래스 목록 (Authoring/ 폴더):
//    GeneralAuthoring  — 장군 (병사 스폰, 스킬)
//    SoldierAuthoring  — 병사 (장군 소속, 스탯은 스폰 시 확정)
//    EnemyAuthoring    — 일반 적 (독립 전투)
//    EliteAuthoring    — 엘리트 (독립 강화, 스킬)
//    BossAuthoring     — 보스 (페이즈, 스킬 목록)
//
//  Baker 구조:
//    UnitBakerBase<T>  — 공통 ECS 컴포넌트 추가 (BakeCommon)
//    각 파생 Baker     — 타입 전용 컴포넌트 추가
// ============================================================

namespace BattleGame.Units
{
    public abstract class UnitAuthoring : MonoBehaviour
    {
        [Header("소속")]
        public int      UnitId = 0;
        public TeamType Team   = TeamType.Ally;

        [Header("이동 설정")]
        public float StoppingDistance = 0.5f;

        [Header("진형 슬롯")]
        public int Row    = 0;
        public int Column = 0;

        [Header("기본 스텟 (Key 비우면 base 레이어)")]
        public List<StatModifier> BaseStats = new()
        {
            new StatModifier { Type = StatType.MaxHp,       Value = 100f },
            new StatModifier { Type = StatType.Defense,     Value = 0.1f },
            new StatModifier { Type = StatType.Attack,      Value = 15f  },
            new StatModifier { Type = StatType.AttackRange, Value = 1.5f },
            new StatModifier { Type = StatType.AttackSpeed, Value = 1f   },
            new StatModifier { Type = StatType.MoveSpeed,   Value = 3f   },
        };

        public UnitStat BuildStat()
        {
            var stat = new UnitStat();
            stat.Apply(BaseStats);
            return stat;
        }
    }

    // ──────────────────────────────────────────
    // 공통 Baker 베이스
    // ──────────────────────────────────────────

    /// <summary>
    /// 모든 유닛 Baker 의 공통 베이스.
    /// BakeCommon() 을 호출하면 공통 ECS 컴포넌트를 일괄 추가한다.
    /// </summary>
    public abstract class UnitBakerBase<T> : Baker<T> where T : UnitAuthoring
    {
        protected void BakeCommon(T authoring, Entity entity, UnitType unitType)
        {
            UnitStat stat = authoring.BuildStat();

            AddComponent(entity, new UnitIdentityComponent
            {
                UnitId = authoring.UnitId,
                Team   = authoring.Team,
                Type   = unitType,
            });

            AddComponent(entity, new MovementComponent
            {
                MoveSpeed        = stat.Get(StatType.MoveSpeed),
                StoppingDistance = authoring.StoppingDistance,
                Destination      = float3.zero,
                IsMoving         = false,
            });
            AddComponent(entity, new VelocityComponent { Value = float3.zero });
            AddComponent(entity, new FormationSlotComponent
            {
                Row          = authoring.Row,
                Column       = authoring.Column,
                SlotPosition = float3.zero,
            });

            float maxHp = stat.Get(StatType.MaxHp);
            AddComponent(entity, new HealthComponent
            {
                CurrentHp = maxHp,
                MaxHp     = maxHp,
                Defense   = stat.GetClamped(StatType.Defense, 0f, 1f),
                IsDead    = false,
            });

            AddComponent(entity, new AttackComponent
            {
                AttackDamage   = stat.Get(StatType.Attack),
                AttackRange    = stat.Get(StatType.AttackRange),
                AttackSpeed    = stat.Get(StatType.AttackSpeed),
                AttackCooldown = 0f,
                HasTarget      = false,
            });

            AddComponent(entity, new UnitStateComponent
            {
                Current    = UnitState.Idle,
                Previous   = UnitState.Idle,
                StateTimer = 0f,
            });
            AddComponent(entity, new HitReactionComponent
            {
                KnockbackVelocity = float3.zero,
                StunDuration      = 0f,
                StunTimer         = 0f,
                IsStunned         = false,
            });
            AddComponent(entity, new GridCellComponent
            {
                Cell     = int2.zero,
                PrevCell = int2.zero,
            });

            AddBuffer<HitEventBufferElement>(entity);
            AddBuffer<StatusEffectBufferElement>(entity);
        }
    }
}
