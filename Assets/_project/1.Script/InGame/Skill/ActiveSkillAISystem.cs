using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ============================================================
//  ActiveSkillAISystem.cs
//  액티브 스킬 쿨다운이 찼을 때 자동으로 UseActiveSkillTag 를 붙인다.
//
//  ■ 발동 조건
//    - GeneralActiveSkillComponent.IsReady (CooldownRemaining <= 0)
//    - SkillUsePolicy.CanUse — 타겟이 있고, 공격 스킬이면 사거리 안
//    - DeadTag 없음 / 경직(Hit) 아님 / UseActiveSkillTag 중복 아님
//    - **아군 장수는 AUTO 토글이 켜져 있을 때만** (상단바 AUTO 버튼)
//
//  ■ 아군만 토글에 걸린다
//    이 시스템은 적 엘리트도 함께 돈다 (EnemyRuntimeBridge 가 같은 컴포넌트를 붙인다).
//    팀을 안 보고 막으면 AUTO 를 끄는 순간 적 엘리트 스킬까지 멎어
//    전투가 통째로 쉬워진다.
//
//  ■ Burst 를 쓰지 않는다
//    BattleSettingsData.AutoSkillEnabled 는 managed 정적 필드다.
//    대상이 장수·엘리트 몇 기뿐이라 Burst 로 얻을 이득이 없다.
//
//  ■ 흐름
//    이 시스템 → UseActiveSkillTag 추가
//    → ActiveSkillCooldownSystem → ActiveSkillExecuteEvent 버퍼 추가
//    → ActiveSkillExecuteSystem  → Execute(context) 호출
//    (수동 사용은 GeneralPanelUI 가 같은 태그를 직접 붙인다)
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ActiveSkillCooldownSystem))]
    public partial struct ActiveSkillAISystem : ISystem
    {
        ComponentLookup<LocalTransform> _transformLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GeneralActiveSkillComponent>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);

            bool autoAlly = BattleSettingsData.AutoSkillEnabled;
            var  pending  = new NativeList<Entity>(8, Allocator.Temp);

            foreach (var (skill, attack, stat, unitState, transform, identity, entity)
                     in SystemAPI.Query<
                            RefRO<GeneralActiveSkillComponent>,
                            RefRO<AttackComponent>,
                            RefRO<StatComponent>,
                            RefRO<UnitStateComponent>,
                            RefRO<LocalTransform>,
                            RefRO<UnitIdentityComponent>>()
                        .WithNone<DeadTag, UseActiveSkillTag, SkillCastLock>()
                        .WithEntityAccess())
            {
                // 아군 자동 사용이 꺼져 있으면 수동(장수 카드 클릭)으로만 나간다
                if (!autoAlly && identity.ValueRO.Team == TeamType.Ally) continue;

                if (!skill.ValueRO.IsReady) continue;                       // 쿨다운 미완료
                if (unitState.ValueRO.Current == UnitState.Hit) continue;   // 속박/스턴 중

                if (!SkillUsePolicy.CanUse(skill.ValueRO.SkillId,
                                           attack.ValueRO,
                                           stat.ValueRO,
                                           transform.ValueRO.Position,
                                           _transformLookup))
                    continue;

                pending.Add(entity);
            }

            // ⚠ 구조 변경(AddComponent)은 반드시 모든 조회가 끝난 뒤에 한다
            //   AddComponent 는 아키타입을 바꾸므로 그 순간 청크가 재배치되고
            //   _transformLookup 을 비롯한 모든 핸들이 무효가 된다.
            //   예전엔 여기서 태그를 붙인 뒤 FireExtraSlots 가 같은 lookup 을
            //   그대로 다시 썼다 — 무효 핸들 접근이라 Burst 잡에서
            //   NullReferenceException 이 터졌다.
            //   그래서 추가 슬롯 판정을 '먼저' 끝내고, 구조 변경을 맨 마지막에 모은다.
            var fired = CollectExtraSlotFires(ref state);

            var em = state.EntityManager;
            for (int i = 0; i < pending.Length; i++)
                em.AddComponent<UseActiveSkillTag>(pending[i]);

            for (int i = 0; i < fired.Length; i++)
            {
                var f = fired[i];
                if (!em.HasBuffer<ActiveSkillExecuteEvent>(f.Caster)) continue;

                em.GetBuffer<ActiveSkillExecuteEvent>(f.Caster).Add(new ActiveSkillExecuteEvent
                {
                    SkillId        = f.SkillId,
                    TargetEntity   = f.Target,
                    TargetPosition = f.TargetPos,
                });
            }

            pending.Dispose();
            fired.Dispose();
        }

        // ── 추가 슬롯 (보스 돌진·분쇄 강타 등) ────────────────────
        //
        //  대표 스킬과 달리 태그를 거치지 않고 여기서 바로 실행 이벤트를 넣는다.
        //  AI 전용이라 수동 발동 경로가 없어 태그를 왕복시킬 이유가 없다.
        //
        //  ⚠ 한 프레임에 슬롯 하나만 발동시킨다
        //    돌진과 강타가 같은 프레임에 터지면 무슨 일이 났는지 안 읽히고,
        //    돌진 이동 중에 강타가 겹쳐 위치가 꼬인다.
        //  ⚠ 여기서는 '무엇을 쏠지' 만 모은다 — 쓰기는 호출한 쪽에서 한다
        //    SystemAPI.Query foreach 안에서 EntityManager.GetBuffer 를 부르면
        //    그 타입의 잡 의존성이 완료되면서 순회 중인 핸들이 무효화될 수 있다.
        //    (같은 이유로 CompleteAllTrackedJobs / CreateEntityQuery 도 금지)
        //
        //  쿨다운 갱신만 여기서 한다 — 순회 대상인 버퍼라 안전하다.
        NativeList<PendingFire> CollectExtraSlotFires(ref SystemState state)
        {
            var fired = new NativeList<PendingFire>(8, Allocator.Temp);

            foreach (var (slotsRO, attack, stat, unitState, transform, entity)
                     in SystemAPI.Query<
                            DynamicBuffer<ActiveSkillSlot>,
                            RefRO<AttackComponent>,
                            RefRO<StatComponent>,
                            RefRO<UnitStateComponent>,
                            RefRO<LocalTransform>>()
                        .WithNone<DeadTag, SkillCastLock>()
                        .WithEntityAccess())
            {
                // ⚠ foreach 분해 변수에는 쓸 수 없다 (CS1654)
                //   DynamicBuffer 는 내부 포인터를 공유하므로 복사해도 같은 메모리다.
                var slots = slotsRO;

                if (slots.Length == 0) continue;
                if (unitState.ValueRO.Current == UnitState.Hit) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].IsReady) continue;

                    if (!SkillUsePolicy.CanUse(slots[i].SkillId,
                                               attack.ValueRO,
                                               stat.ValueRO,
                                               transform.ValueRO.Position,
                                               _transformLookup))
                        continue;

                    Entity target    = attack.ValueRO.TargetEntity;
                    float3 targetPos = _transformLookup.TryGetComponent(target, out LocalTransform lt)
                        ? lt.Position
                        : transform.ValueRO.Position;

                    // 쿨다운은 버퍼(순회 대상)라 여기서 바로 써도 안전하다
                    var s = slots[i];
                    s.CooldownRemaining = s.Cooldown;
                    slots[i] = s;

                    fired.Add(new PendingFire
                    {
                        Caster    = entity,
                        SkillId   = s.SkillId,
                        Target    = target,
                        TargetPos = targetPos,
                    });
                    break;                       // 이 프레임은 여기까지
                }
            }

            return fired;
        }

        struct PendingFire
        {
            public Entity Caster;
            public int    SkillId;
            public Entity Target;
            public float3 TargetPos;
        }
    }
}
