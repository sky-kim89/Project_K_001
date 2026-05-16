using Unity.Entities;
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
        protected override void OnUpdate()
        {
            foreach (var (attack, health, entity) in
                SystemAPI.Query<RefRO<AttackComponent>, RefRO<HealthComponent>>()
                    .WithAll<GeneralComponent, GeneralTriggerSetComponent>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                var trigSet = EntityManager.GetComponentObject<GeneralTriggerSetComponent>(entity);
                if (trigSet == null) continue;

                var hitBuf   = EntityManager.GetBuffer<HitEventBufferElement>(entity);
                var killBuf  = EntityManager.GetBuffer<EnemyKillEvent>(entity);
                var deathBuf = EntityManager.GetBuffer<SoldierDeathEvent>(entity);
                var skillBuf = EntityManager.GetBuffer<SkillUseEvent>(entity);

                var ctx = new PassiveTriggerContext
                {
                    GeneralEntity     = entity,
                    EntityManager     = EntityManager,
                    Health            = health.ValueRO,
                    DamageDealt       = attack.ValueRO.LastDamageDealt,
                    SoldierDeathCount = deathBuf.Length,
                };

                bool doAttack       = attack.ValueRO.AttackedThisFrame;
                bool doHit          = hitBuf.Length   > 0;
                bool doKill         = killBuf.Length  > 0;
                bool doSoldierDeath = deathBuf.Length > 0;
                bool doSkill        = skillBuf.Length > 0;

                // ── 장비 트리거 디스패치 ─────────────────────────
                for (int s = 0; s < 2; s++)
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

                    ApplyEquipTrigger(equip, enhance, ctx);
                }

                // ── 특수 어빌리티 트리거 디스패치 ────────────────
                foreach (var ability in trigSet.TriggerAbilities)
                {
                    if (ability == null) continue;
                    PassiveTrigger pt = ability.GetTriggerType();

                    bool fire = pt switch
                    {
                        PassiveTrigger.OnAttack       => doAttack,
                        PassiveTrigger.OnHit          => doHit,
                        PassiveTrigger.OnEnemyKill    => doKill,
                        PassiveTrigger.OnSoldierDeath => doSoldierDeath,
                        PassiveTrigger.OnSkillUse     => doSkill,
                        _                             => false,
                    };
                    if (!fire) continue;
                    ability.OnTrigger(ctx);
                }
            }
        }

        // ── 장비 트리거 효과 적용 ────────────────────────────────

        static void ApplyEquipTrigger(EquipmentData equip, int enhance, PassiveTriggerContext ctx)
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
                Stat      = equip.TriggerStat,
                Delta     = value,
                Mode      = EffectMode.Add,
                Duration  = dur,
                Remaining = dur,
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
