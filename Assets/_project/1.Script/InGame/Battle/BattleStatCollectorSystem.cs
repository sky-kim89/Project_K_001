using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  BattleStatCollectorSystem.cs
//  DamageResultBuffer 를 매 프레임 읽어 BattleStatsTracker 에 귀속.
//
//  실행 순서: UnitHitSystem 이후 → 같은 프레임 내 결과를 즉시 집계
//
//  딜 분류:
//    HitType.Skill                → DamageCategory.Skill
//    SummonedTag 있는 공격자      → DamageCategory.Skill
//    SoldierComponent 있는 공격자 → DamageCategory.Soldier
//    GeneralComponent 있는 공격자 → DamageCategory.General
//
//  피해 수신:
//    피격자가 아군 장군   → DamageTaken
//    피격자가 아군 병사   → SoldierDamageTaken (소속 장군 기준)
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitHitSystem))]
    public partial class BattleStatCollectorSystem : SystemBase
    {
        ComponentLookup<GeneralComponent>       _generalLookup;
        ComponentLookup<SoldierComponent>       _soldierLookup;
        ComponentLookup<SummonedTag>            _summonedLookup;

        protected override void OnCreate()
        {
            _generalLookup  = GetComponentLookup<GeneralComponent>(isReadOnly: true);
            _soldierLookup  = GetComponentLookup<SoldierComponent>(isReadOnly: true);
            _summonedLookup = GetComponentLookup<SummonedTag>(isReadOnly: true);
        }

        protected override void OnUpdate()
        {
            var tracker = BattleStatsTracker.Instance;
            if (tracker == null) return;

            _generalLookup.Update(this);
            _soldierLookup.Update(this);
            _summonedLookup.Update(this);

            foreach (var (results, identityRO, entity) in
                     SystemAPI.Query<DynamicBuffer<DamageResultElement>,
                                    RefRO<UnitIdentityComponent>>()
                              .WithEntityAccess())
            {
                if (results.Length == 0) continue;

                // ── 피격자의 소속 장군 결정 ──────────────────
                Entity victimGeneralEntity = Entity.Null;
                bool   victimIsGeneral     = false;
                var    identity            = identityRO.ValueRO;

                if (identity.Team == TeamType.Ally)
                {
                    if (identity.Type == UnitType.General)
                    {
                        victimGeneralEntity = entity;
                        victimIsGeneral     = true;
                    }
                    else if (identity.Type == UnitType.Soldier &&
                             _soldierLookup.HasComponent(entity))
                    {
                        victimGeneralEntity = _soldierLookup[entity].GeneralEntity;
                    }
                }

                for (int i = 0; i < results.Length; i++)
                {
                    DamageResultElement r = results[i];

                    // ── 피해 수신 기록 (아군 피격 시만) ─────────
                    if (victimGeneralEntity != Entity.Null &&
                        tracker.GetEntry(victimGeneralEntity) != null)
                    {
                        tracker.RecordDamageTaken(victimGeneralEntity, r.ActualDamage, !victimIsGeneral);
                        if (r.AbsorbedDamage > 0f)
                            tracker.RecordAbsorbed(victimGeneralEntity, r.AbsorbedDamage);
                    }

                    // ── 피해 귀속 (공격자 → 소속 장군) ──────────
                    if (r.AttackerEntity == Entity.Null) continue;

                    Entity attackerGeneral = ResolveGeneral(r.AttackerEntity, _generalLookup, _soldierLookup);
                    if (attackerGeneral == Entity.Null) continue;
                    if (tracker.GetEntry(attackerGeneral) == null) continue;

                    DamageCategory cat = ClassifyDamage(
                        r.AttackerEntity, r.Type,
                        _generalLookup, _soldierLookup, _summonedLookup);

                    tracker.RecordDamage(attackerGeneral, r.ActualDamage, cat);

                    if (r.IsKill)
                        tracker.RecordKill(attackerGeneral);
                }

                results.Clear();
            }
        }

        // ── 헬퍼 ─────────────────────────────────────────────────

        static Entity ResolveGeneral(Entity attacker,
            ComponentLookup<GeneralComponent> generalLookup,
            ComponentLookup<SoldierComponent> soldierLookup)
        {
            if (generalLookup.HasComponent(attacker)) return attacker;
            if (soldierLookup.HasComponent(attacker)) return soldierLookup[attacker].GeneralEntity;
            return Entity.Null;
        }

        static DamageCategory ClassifyDamage(Entity attacker, BattleGame.Units.HitType hitType,
            ComponentLookup<GeneralComponent> generalLookup,
            ComponentLookup<SoldierComponent> soldierLookup,
            ComponentLookup<SummonedTag>      summonedLookup)
        {
            if (hitType == BattleGame.Units.HitType.Skill)  return DamageCategory.Skill;
            if (summonedLookup.HasComponent(attacker))      return DamageCategory.Skill;
            if (soldierLookup.HasComponent(attacker))       return DamageCategory.Soldier;
            if (generalLookup.HasComponent(attacker))       return DamageCategory.General;
            return DamageCategory.General;
        }
    }
}
