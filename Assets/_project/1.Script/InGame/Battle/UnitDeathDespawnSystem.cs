using Unity.Entities;
using UnityEngine;

// ============================================================
//  UnitDeathDespawnSystem.cs
//  DeadTag 가 붙은 Entity 를 감지해 풀로 반납하는 관리형 시스템.
//
//  흐름:
//    ProcessHitEventsJob (Burst) → ECB 로 DeadTag 추가
//    → UnitDeathDespawnSystem (managed) → PoolController.Despawn() 호출
//                                       → BattleManager.OnUnitDead() 호출
//                                       → UnitPoolLinkComponent 제거 (중복 처리 방지)
//
//  왜 Burst 가 아닌 managed 시스템인가:
//    PoolController / BattleManager 는 MonoBehaviour(managed) 이므로
//    Burst Job 내부에서 직접 호출할 수 없다.
//    ProcessHitEventsJob 이 ECB 로 DeadTag 를 추가한 직후 이 시스템이 처리한다.
//
//  UnitPoolLinkComponent:
//    - 스포너가 풀에서 유닛을 꺼낸 뒤 EntityManager 로 추가
//    - PoolKey: 풀 반납 시 사용하는 문자열 키
//    - LinkedObject: 반납할 GameObject 참조
// ============================================================

namespace BattleGame.Units
{
    // ── 유닛 GO ↔ Entity 연결 컴포넌트 (managed) ─────────────
    /// <summary>
    /// 풀에서 꺼낸 유닛 GameObject 와 Entity 를 연결한다.
    /// 스포너가 풀 스폰 직후 EntityManager.AddComponentObject 로 추가.
    /// </summary>
    public class UnitPoolLinkComponent : IComponentData
    {
        public string     PoolKey;       // PoolController 에 등록된 풀 키
        public GameObject LinkedObject;  // 반납할 GameObject
    }

    // ── 사망 감지 + 디스폰 시스템 ────────────────────────────

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitHitSystem))]
    public partial class UnitDeathDespawnSystem : SystemBase
    {
        // GO 반납 목록 — ForEach 외부에서 처리하기 위해 캐싱
        // generalEntity: 병사 사망 시 소속 장군 알림용 (병사 아니면 Entity.Null)
        readonly System.Collections.Generic.List<(GameObject obj, TeamType team, Entity generalEntity)> _pending = new();

        protected override void OnUpdate()
        {
            _pending.Clear();

            // ── ① 사망 유닛 수집 + 링크 컴포넌트 제거 예약 ─────
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            Entities
                .WithAll<DeadTag>()
                .WithoutBurst()
                .ForEach((Entity entity,
                          UnitPoolLinkComponent link,
                          in UnitIdentityComponent identity) =>
                {
                    // ForEach 안에서는 GO 반납 금지 (SetActive → EntityLink.OnDisable → AddComponent 구조적 변경 오류)
                    // 대신 목록에 담아 두고 ForEach 완료 후 처리

                    // 병사 사망 시 소속 장군 Entity 캡처
                    Entity generalEntity = Entity.Null;
                    if (identity.Type == UnitType.Soldier
                        && EntityManager.HasComponent<SoldierComponent>(entity))
                    {
                        generalEntity = EntityManager
                            .GetComponentData<SoldierComponent>(entity).GeneralEntity;
                    }

                    _pending.Add((link.LinkedObject, identity.Team, generalEntity));
                    ecb.RemoveComponent<UnitPoolLinkComponent>(entity);
                })
                .Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();

            // ── ② 병사 사망 이벤트 → 소속 장군에게 알림 ─────────
            // SoldierDeathEvent 버퍼가 있는 장군에게 사망 이벤트를 추가한다.
            // PassiveSkillRuntimeSystem 이 다음 프레임에 이 버퍼를 처리한다.
            foreach (var (_, _, generalEntity) in _pending)
            {
                if (generalEntity == Entity.Null) continue;
                if (!EntityManager.Exists(generalEntity)) continue;
                if (!EntityManager.HasBuffer<SoldierDeathEvent>(generalEntity)) continue;

                EntityManager.GetBuffer<SoldierDeathEvent>(generalEntity).Add(default);
            }

            // ── ③ ForEach 완료 후 GO 반납 (이 시점은 Entity 순회 밖이므로 안전) ──
            foreach (var (obj, team, _) in _pending)
            {
                // 생존 카운트 즉시 갱신 (승패 판정은 연출과 무관하게 바로 처리)
                BattleManager.Instance?.OnUnitDead(team);

                if (obj == null) continue;

                // UnitAnimationSync 가 있으면 사망 연출(날아가기 + 대기) 후 자체 디스폰.
                // 없으면 즉시 디스폰.
                var animSync = obj.GetComponent<UnitAnimationSync>();
                if (animSync != null)
                    animSync.TriggerDeath();
                else
                    PoolController.Instance?.Despawn(obj);
            }
        }
    }
}
