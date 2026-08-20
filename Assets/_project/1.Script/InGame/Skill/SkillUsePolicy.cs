using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ============================================================
//  SkillUsePolicy.cs
//  "지금 이 액티브 스킬을 써도 되는가" 판정의 정본.
//
//  ■ 규칙
//    ① 타겟이 있어야 한다.
//    ② 공격 스킬(= ActiveSkillId.IsSupport() 가 false)은
//       그 타겟이 **시전자의 공격 사거리 안** 에 들어와 있어야 한다.
//       버프·치유·소환은 ② 를 건너뛴다.
//    ③ 돌진형(ActiveSkillId.IsDash())도 ② 를 건너뛴다.
//       달려가서 때리는 것이 목적인데 사거리 안에서만 나가면 돌진할 거리가
//       남아 있지 않다. ① 의 HasTarget 이 이미 "적이 시야에 있다" 를 뜻하므로
//       (UnitTargetSearchSystem 의 그리드 탐색 반경) 그것으로 충분하다.
//
//    사거리 판정은 UnitAttackSystem 의 평타 판정과 같은 식이다
//    (StatComponent.Final[AttackRange] 기준 거리 제곱 비교).
//    두 곳이 어긋나면 "때리지도 못하는 거리에서 스킬만 나가는" 상태가 된다.
//
//  ■ 두 진입점이 있는 이유
//    Query   — ActiveSkillAISystem(ECS) 이 매 프레임 자동 발동을 판단할 때.
//              쿼리 순회 중이라 ComponentLookup 으로만 남의 엔티티를 읽는다.
//    Now     — 장수 카드를 눌렀을 때 (GeneralPanelUI, 메인 스레드).
//              EntityManager 를 직접 써도 되는 자리다.
//    같은 규칙을 두 번 적으면 반드시 갈라지므로 판정 본문은 InRange 하나로 모은다.
// ============================================================

namespace BattleGame.Units
{
    public static class SkillUsePolicy
    {
        /// <summary>ECS 쿼리 순회 중 판정 — 남의 위치는 lookup 으로만 읽는다.</summary>
        public static bool CanUse(int skillId,
                                  in AttackComponent attack,
                                  in StatComponent   stat,
                                  float3             casterPos,
                                  in ComponentLookup<LocalTransform> transformLookup)
        {
            if (!attack.HasTarget) return false;

            var id = (ActiveSkillId)skillId;
            if (id.IsSupport()) return true;

            // 돌진형은 여기서 끝 — HasTarget 이 곧 "적이 시야에 있다" 다.
            if (id.IsDash()) return true;

            if (!transformLookup.TryGetComponent(attack.TargetEntity, out LocalTransform targetT))
                return false;

            float range = stat.Final[StatType.AttackRange] * id.RangeScale();
            return InRange(casterPos, targetT.Position, range);
        }

        /// <summary>메인 스레드 판정 — 클릭·UI 표시용. 쿨다운·사망·경직까지 함께 본다.</summary>
        public static bool CanUseNow(EntityManager em, Entity general)
        {
            if (!em.Exists(general))                                   return false;
            if (em.HasComponent<DeadTag>(general))                     return false;
            if (em.HasComponent<UseActiveSkillTag>(general))           return false;  // 이미 요청됨
            if (!em.HasComponent<GeneralActiveSkillComponent>(general)) return false;

            if (!em.GetComponentData<GeneralActiveSkillComponent>(general).IsReady) return false;
            if (em.GetComponentData<UnitStateComponent>(general).Current == UnitState.Hit) return false;

            var attack = em.GetComponentData<AttackComponent>(general);
            if (!attack.HasTarget) return false;

            var id = (ActiveSkillId)em.GetComponentData<GeneralActiveSkillComponent>(general).SkillId;
            if (id.IsSupport()) return true;
            if (id.IsDash())    return true;   // 달려가서 때리는 스킬 — 사거리를 보지 않는다

            if (!em.Exists(attack.TargetEntity))                            return false;
            if (!em.HasComponent<LocalTransform>(attack.TargetEntity))      return false;

            float3 casterPos = em.GetComponentData<LocalTransform>(general).Position;
            float3 targetPos = em.GetComponentData<LocalTransform>(attack.TargetEntity).Position;
            float  range     = em.GetComponentData<StatComponent>(general).Final[StatType.AttackRange]
                               * id.RangeScale();

            return InRange(casterPos, targetPos, range);
        }

        static bool InRange(float3 casterPos, float3 targetPos, float range)
            => math.distancesq(casterPos, targetPos) <= range * range;
    }
}
