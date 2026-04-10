using Unity.Mathematics;
using Unity.Transforms;
using BattleGame.Units;

// ============================================================
//  ActiveArrowRain.cs — 화살 비 (궁수)
//
//  타겟 위치에 화살 비 지대를 생성한다.
//  지대 내 적에게 매 틱 EffectValue 피해.
//  지속시간 = EffectDuration, 반경 = EffectRadius.
//  (디버프 없이 순수 피해만 가하는 고밀도 공격)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_ArrowRain", menuName = "BattleGame/Actives/ArrowRain")]
public class ActiveArrowRain : ActiveSkillData
{
    [UnityEngine.Header("화살 비 설정")]
    [UnityEngine.Tooltip("틱 간격 (초)")]
    public float TickInterval = 0.4f;

    public override void Execute(ActiveSkillContext ctx)
    {
        if (!ctx.HasTarget) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        float3 center = float3.zero;
        if (em.HasComponent<LocalTransform>(ctx.TargetEntity))
        {
            var lt = em.GetComponentData<LocalTransform>(ctx.TargetEntity);
            center = lt.Position;
        }

        var casterIdentity = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);

        var runner = ctx.CasterObject.AddComponent<SkillZoneRunner>();

        runner.Setup(new SkillZoneRunner.ZoneConfig
        {
            Center        = center,
            Radius        = EffectRadius > 0f ? EffectRadius : 2f,
            Duration      = EffectDuration > 0f ? EffectDuration : 5f,
            TickInterval  = TickInterval,
            DamagePerTick = EffectValue,
            CasterTeam    = casterIdentity.Team,
            CasterEntity  = ctx.CasterEntity,

            HasDebuff1    = false,
            HasDebuff2    = false,
        });
    }
}
