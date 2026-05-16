using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  UnitHealSystem.cs
//  HealEventBuffer 를 매 프레임 처리해 HP 를 회복시킨다.
//
//  실행 순서: BattleStatCollectorSystem 이후
//
//  힐 흐름:
//    모든 힐 소스 → HealEventBufferElement.Add(amount, sourceEntity)
//    → 이 시스템 → HP 클램프 회복
//    → BattleStatsTracker.RecordHealingDone(sourceGeneral, rawAmount)
//    → BattleStatsTracker.RecordHealingReceived(targetGeneral, actualHeal)
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BattleStatCollectorSystem))]
    public partial class UnitHealSystem : SystemBase
    {
        ComponentLookup<GeneralComponent>      _generalLookup;
        ComponentLookup<SoldierComponent>      _soldierLookup;
        ComponentLookup<UnitIdentityComponent> _identityLookup;

        protected override void OnCreate()
        {
            _generalLookup  = GetComponentLookup<GeneralComponent>(isReadOnly: true);
            _soldierLookup  = GetComponentLookup<SoldierComponent>(isReadOnly: true);
            _identityLookup = GetComponentLookup<UnitIdentityComponent>(isReadOnly: true);
        }

        protected override void OnUpdate()
        {
            _generalLookup.Update(this);
            _soldierLookup.Update(this);
            _identityLookup.Update(this);

            var tracker = BattleStatsTracker.Instance;

            foreach (var (healBuffer, healthRW, statRO, entity) in
                     SystemAPI.Query<DynamicBuffer<HealEventBufferElement>,
                                    RefRW<HealthComponent>,
                                    RefRO<StatComponent>>()
                              .WithNone<DeadTag>()
                              .WithEntityAccess())
            {
                if (healBuffer.IsEmpty) continue;

                float maxHp    = statRO.ValueRO.Final[StatType.MaxHp];
                float rawTotal = 0f;

                for (int i = 0; i < healBuffer.Length; i++)
                {
                    float amount = healBuffer[i].Amount;
                    rawTotal += amount;

                    if (tracker != null && healBuffer[i].SourceEntity != Entity.Null)
                    {
                        Entity srcGeneral = ResolveGeneral(
                            healBuffer[i].SourceEntity, _generalLookup, _soldierLookup);
                        if (srcGeneral != Entity.Null)
                            tracker.RecordHealingDone(srcGeneral, amount);
                    }
                }

                healBuffer.Clear();

                float prevHp = healthRW.ValueRO.CurrentHp;
                healthRW.ValueRW.CurrentHp = Mathf.Min(prevHp + rawTotal, maxHp);
                float actualHeal = healthRW.ValueRO.CurrentHp - prevHp;

                if (tracker == null || actualHeal <= 0f) continue;

                Entity targetGeneral = ResolveTargetGeneral(entity, _identityLookup, _soldierLookup);
                if (targetGeneral != Entity.Null)
                    tracker.RecordHealingReceived(targetGeneral, actualHeal);
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────────

        static Entity ResolveGeneral(Entity source,
            ComponentLookup<GeneralComponent> generalLookup,
            ComponentLookup<SoldierComponent> soldierLookup)
        {
            if (generalLookup.HasComponent(source)) return source;
            if (soldierLookup.HasComponent(source)) return soldierLookup[source].GeneralEntity;
            return Entity.Null;
        }

        static Entity ResolveTargetGeneral(Entity target,
            ComponentLookup<UnitIdentityComponent> identLookup,
            ComponentLookup<SoldierComponent>      soldierLookup)
        {
            if (!identLookup.HasComponent(target)) return Entity.Null;
            var id = identLookup[target];
            if (id.Type == UnitType.General) return target;
            if (id.Type == UnitType.Soldier && soldierLookup.HasComponent(target))
                return soldierLookup[target].GeneralEntity;
            return Entity.Null;
        }
    }
}
