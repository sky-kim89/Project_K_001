using Unity.Mathematics;
using Unity.Transforms;
using BattleGame.Units;

// ============================================================
//  ActivePoisonZone.cs — 독성 지대 (법사·궁수)
//
//  타겟 위치에 독성 지대를 생성한다.
//  지대 내 적에게 매 틱 EffectValue 피해 + 이동속도 감소.
//  지속시간 = EffectDuration, 반경 = EffectRadius.
//
//  이동속도 감소 배율은 MoveSlowMultiplier Inspector 에서 설정.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_PoisonZone", menuName = "BattleGame/Actives/PoisonZone")]
public class ActivePoisonZone : ActiveSkillData
{
    [UnityEngine.Header("독성 지대 설정")]
    [UnityEngine.Tooltip("이동속도 감소 배율 (예: 0.5 → 이동속도 50%)")]
    [UnityEngine.Range(0.1f, 0.9f)]
    public float MoveSlowMultiplier = 0.5f;

    [UnityEngine.Tooltip("틱 간격 (초)")]
    public float TickInterval = 0.5f;

    public override void Execute(ActiveSkillContext ctx)
    {
        if (!ctx.HasTarget) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        // 타겟 위치를 지대 중심으로 사용
        float3 center = float3.zero;
        if (em.HasComponent<LocalTransform>(ctx.TargetEntity))
        {
            var lt = em.GetComponentData<LocalTransform>(ctx.TargetEntity);
            center = lt.Position;
        }

        var casterIdentity = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);

        // 동시 발동 지원: 매번 새 인스턴스 추가 (완료 시 자동 Destroy)
        var runner = ctx.CasterObject.AddComponent<SkillZoneRunner>();

        runner.Setup(new SkillZoneRunner.ZoneConfig
        {
            Center        = center,
            Radius        = EffectRadius > 0f ? EffectRadius : 2.5f,
            Duration      = EffectDuration > 0f ? EffectDuration : 6f,
            TickInterval  = TickInterval,
            DamagePerTick = EffectValue,
            CasterTeam    = casterIdentity.Team,
            CasterEntity  = ctx.CasterEntity,

            HasDebuff1    = true,
            Debuff1Stat   = StatType.MoveSpeed,
            Debuff1Delta  = MoveSlowMultiplier,
            Debuff1Mode   = EffectMode.Multiply,

            HasDebuff2    = false,
        });
    }
}
