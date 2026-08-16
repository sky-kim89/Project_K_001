using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  CombatTriggerSystem.cs
//  장비·특수 어빌리티 트리거 디스패치 시스템.
//
//  GeneralTriggerSetComponent 는 managed class IComponentData 이므로
//  SystemAPI.Query 에 포함할 수 없고 EntityManager.GetComponentObject 로 획득.
//
//  실행 순서: PassiveSkillRuntimeSystem → CombatTriggerSystem → UnitHitSystem
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PassiveSkillRuntimeSystem))]
    [UpdateBefore(typeof(UnitHitSystem))]
    public partial class CombatTriggerSystem : SystemBase
    {
        EntityQuery _enemyQuery;
        EntityQuery _generalQuery;

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All  = new ComponentType[] { ComponentType.ReadOnly<UnitIdentityComponent>(), ComponentType.ReadOnly<LocalTransform>() },
                None = new ComponentType[] { typeof(DeadTag) },
            });

            _generalQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<GeneralComponent>(),
                    ComponentType.ReadOnly<GeneralTriggerSetComponent>(),
                    ComponentType.ReadOnly<AttackComponent>(),
                    ComponentType.ReadOnly<HealthComponent>(),
                },
                None = new ComponentType[] { typeof(DeadTag) },
            });
        }

        // ⚠ SystemAPI.Query 로 순회하지 않는다.
        //   여기서 부르는 핸들러(연속 시전의 스킬 재시전, 소환, 이펙트 GO 활성화 등)는
        //   구조적 변경을 일으킬 수 있고, 그 순간 순회의 TypeHandle 과
        //   미리 잡아 둔 DynamicBuffer 가 통째로 무효화된다.
        //   → 대상 엔티티를 배열로 떠 놓고 순회 밖에서 처리하며,
        //     버퍼 핸들은 절대 핸들러 호출을 가로질러 들고 있지 않는다.
        protected override void OnUpdate()
        {
            var generals = _generalQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in generals)
            {
                if (!EntityManager.Exists(entity)) continue;
                if (!EntityManager.HasComponent<GeneralTriggerSetComponent>(entity)) continue;

                var trigSet = EntityManager.GetComponentObject<GeneralTriggerSetComponent>(entity);
                if (trigSet == null) continue;

                var attack = EntityManager.GetComponentData<AttackComponent>(entity);
                var health = EntityManager.GetComponentData<HealthComponent>(entity);

                // 이벤트 유무·개수만 읽고 버퍼 핸들은 버린다.
                int soldierDeaths = EntityManager.GetBuffer<SoldierDeathEvent>(entity).Length;

                bool doAttack        = attack.AttackedThisFrame;
                bool doHit           = EntityManager.GetBuffer<HitEventBufferElement>(entity).Length > 0;
                bool doKill          = EntityManager.GetBuffer<EnemyKillEvent>(entity).Length        > 0;
                bool doSoldierDeath  = soldierDeaths                                                 > 0;
                bool doSkill         = EntityManager.GetBuffer<SkillUseEvent>(entity).Length         > 0;
                bool doAttackLanded  = EntityManager.GetBuffer<AttackHitEvent>(entity).Length        > 0;

                if (!doAttack && !doHit && !doKill && !doSoldierDeath && !doSkill && !doAttackLanded)
                    continue;

                var ctx = new PassiveTriggerContext
                {
                    GeneralEntity     = entity,
                    EntityManager     = EntityManager,
                    EnemyQuery        = _enemyQuery,
                    Health            = health,
                    DamageDealt       = attack.LastDamageDealt,
                    SoldierDeathCount = soldierDeaths,
                };

                // ── 장비 트리거 디스패치 ─────────────────────────
                for (int s = 0; s < trigSet.ActiveEquipSlots; s++)
                {
                    var equip   = trigSet.EquipSlots[s];
                    int enhance = trigSet.EnhanceLevels[s];
                    if (equip == null || equip.TriggerType == EquipmentTrigger.None) continue;

                    bool fire = equip.TriggerType switch
                    {
                        EquipmentTrigger.OnAttack       => doAttack,
                        EquipmentTrigger.OnHit          => doHit,
                        EquipmentTrigger.OnEnemyKill    => doKill,
                        EquipmentTrigger.OnSoldierDeath => doSoldierDeath,
                        EquipmentTrigger.OnSkillUse     => doSkill,
                        _                               => false,
                    };
                    if (!fire) continue;
                    if (Random.value > equip.TriggerChance) continue;

                    ApplyEquipTrigger(equip, enhance, s, ctx);
                }

                // ── 특수 어빌리티 트리거 디스패치 ────────────────
                foreach (var ability in trigSet.TriggerAbilities)
                {
                    if (ability == null) continue;
                    PassiveTrigger pt = ability.GetTriggerType();

                    bool fire = pt switch
                    {
                        PassiveTrigger.OnAttack        => doAttack,
                        PassiveTrigger.OnHit           => doHit,
                        PassiveTrigger.OnEnemyKill     => doKill,
                        PassiveTrigger.OnSoldierDeath  => doSoldierDeath,
                        PassiveTrigger.OnSkillUse      => doSkill,
                        PassiveTrigger.OnAttackLanded  => doAttackLanded,
                        _                              => false,
                    };
                    if (!fire) continue;
                    ability.OnTrigger(ctx);
                }

                // ── 특성 트리거 디스패치 ──────────────────────────
                foreach (var handler in trigSet.TraitTriggers)
                {
                    if (handler == null) continue;
                    bool fire = handler.GetTriggerType() switch
                    {
                        PassiveTrigger.OnAttack        => doAttack,
                        PassiveTrigger.OnHit           => doHit,
                        PassiveTrigger.OnEnemyKill     => doKill,
                        PassiveTrigger.OnSoldierDeath  => doSoldierDeath,
                        PassiveTrigger.OnSkillUse      => doSkill,
                        PassiveTrigger.OnAttackLanded  => doAttackLanded,
                        _                              => false,
                    };
                    if (!fire) continue;
                    handler.OnTrigger(ctx);
                }

                // ── 이벤트 버퍼 소비 완료 → 클리어 ──────────────────
                // 이 시스템이 이벤트 버퍼의 마지막 소비자다.
                // PassiveSkillRuntimeSystem(먼저 도는 시스템)이 지우면 여기까지
                // 이벤트가 오지 않아 장비·어빌리티·특성 트리거가 통째로 죽는다.
                //
                // ⚠ 반드시 여기서 버퍼를 다시 가져온다. 위에서 잡아 둔 핸들은
                //   핸들러의 구조적 변경으로 이미 무효일 수 있고, 그 상태로 Clear 하면
                //   ObjectDisposedException 이 나면서 OnUpdate 가 중단된다
                //   → 버퍼가 안 비워져 이벤트가 쌓이고, 다음 프레임에 몰아서 터진다.
                if (!EntityManager.Exists(entity)) continue;

                if (doAttackLanded)  EntityManager.GetBuffer<AttackHitEvent>(entity).Clear();
                if (doSoldierDeath)  EntityManager.GetBuffer<SoldierDeathEvent>(entity).Clear();
                if (doKill)          EntityManager.GetBuffer<EnemyKillEvent>(entity).Clear();
                if (doSkill)         EntityManager.GetBuffer<SkillUseEvent>(entity).Clear();
            }

            generals.Dispose();
        }

        // ── 장비 트리거 효과 적용 ────────────────────────────────

        static void ApplyEquipTrigger(EquipmentData equip, int enhance, int slotIndex, PassiveTriggerContext ctx)
        {
            var   em    = ctx.EntityManager;
            float value = CalcValue(equip, enhance, ctx);

            // MaxHp 스탯 = 즉시 체력 회복 특수 처리
            if (equip.TriggerStat == StatType.MaxHp)
            {
                if (!em.HasBuffer<HealEventBufferElement>(ctx.GeneralEntity)) return;
                em.GetBuffer<HealEventBufferElement>(ctx.GeneralEntity).Add(
                    new HealEventBufferElement { Amount = value, SourceEntity = ctx.GeneralEntity });
                return;
            }

            // 그 외 스탯 = StatusEffect 버프
            if (!em.HasBuffer<StatusEffectBufferElement>(ctx.GeneralEntity)) return;
            float dur = equip.TriggerDuration > 0f ? equip.TriggerDuration : 0.1f;
            em.GetBuffer<StatusEffectBufferElement>(ctx.GeneralEntity).Add(new StatusEffectBufferElement
            {
                Stat       = equip.TriggerStat,
                Delta      = value,
                Mode       = EffectMode.Add,
                Duration   = dur,
                Remaining  = dur,
                SourceType = BuffSourceType.Equipment,
                SourceId   = slotIndex,
            });
        }

        static float CalcValue(EquipmentData equip, int enhance, PassiveTriggerContext ctx)
        {
            float v = equip.TriggerValue;
            if (equip.TriggerIsPercent)
            {
                float baseRef = equip.TriggerPercentBase switch
                {
                    EquipTriggerPercentBase.OfDamage => ctx.DamageDealt,
                    EquipTriggerPercentBase.OfMaxHp  =>
                        ctx.EntityManager.HasComponent<StatComponent>(ctx.GeneralEntity)
                            ? ctx.EntityManager.GetComponentData<StatComponent>(ctx.GeneralEntity).Final[StatType.MaxHp]
                            : 0f,
                    _ => 1f,
                };
                v *= baseRef;
            }
            v *= 1f + enhance * equip.ValuePerLevel;
            return v;
        }
    }
}
