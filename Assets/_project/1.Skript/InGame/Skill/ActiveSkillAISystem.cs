using Unity.Burst;
using Unity.Entities;

// ============================================================
//  ActiveSkillAISystem.cs
//  장군이 액티브 스킬 쿨다운을 채웠을 때 자동으로 UseActiveSkillTag 를 붙인다.
//
//  ■ 발동 조건
//    - GeneralActiveSkillComponent.IsReady (CooldownRemaining <= 0)
//    - AttackComponent.HasTarget (공격 타겟이 있음)
//    - DeadTag 없음
//    - UseActiveSkillTag 가 아직 붙어 있지 않음 (중복 방지)
//
//  ■ 흐름
//    이 시스템 → UseActiveSkillTag 추가
//    → ActiveSkillCooldownSystem → ActiveSkillExecuteEvent 버퍼 추가
//    → ActiveSkillExecuteSystem  → Execute(context) 호출
// ============================================================

namespace BattleGame.Units
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ActiveSkillCooldownSystem))]
    public partial struct ActiveSkillAISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GeneralActiveSkillComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new Unity.Collections.NativeList<Entity>(Unity.Collections.Allocator.Temp);

            foreach (var (skill, attack, entity)
                     in SystemAPI.Query<
                            RefRO<GeneralActiveSkillComponent>,
                            RefRO<AttackComponent>>()
                        .WithNone<DeadTag, UseActiveSkillTag>()
                        .WithEntityAccess())
            {
                if (!skill.ValueRO.IsReady)  continue;  // 쿨다운 미완료
                if (!attack.ValueRO.HasTarget) continue; // 타겟 없음

                ecb.Add(entity);
            }

            var em = state.EntityManager;
            for (int i = 0; i < ecb.Length; i++)
                em.AddComponent<UseActiveSkillTag>(ecb[i]);

            ecb.Dispose();
        }
    }
}
