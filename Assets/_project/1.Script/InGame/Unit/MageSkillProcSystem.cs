using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  MageSkillProcSystem.cs
//  D03 마법사 달인 — 일반 공격 시 1% 확률로 보유 스킬 즉시 발동.
//  MageSkillProcTag 가 붙은 장군이 AttackedThisFrame 이고
//  스킬 쿨다운이 준비된 경우 UseActiveSkillTag 를 추가한다.
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial class MageSkillProcSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var em    = EntityManager;
            var queue = new NativeList<Entity>(Allocator.Temp);

            foreach (var (attack, skill, entity) in
                SystemAPI.Query<RefRO<AttackComponent>, RefRO<GeneralActiveSkillComponent>>()
                    .WithAll<MageSkillProcTag>()
                    .WithNone<DeadTag, UseActiveSkillTag>()
                    .WithEntityAccess())
            {
                if (!attack.ValueRO.AttackedThisFrame) continue;
                if (!skill.ValueRO.IsReady)            continue;
                if (UnityEngine.Random.value < 0.01f)
                    queue.Add(entity);
            }

            for (int i = 0; i < queue.Length; i++)
                em.AddComponent<UseActiveSkillTag>(queue[i]);

            queue.Dispose();
        }
    }
}
