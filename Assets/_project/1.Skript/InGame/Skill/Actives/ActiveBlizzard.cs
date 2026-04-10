using Unity.Mathematics;
using Unity.Transforms;
using BattleGame.Units;

// ============================================================
//  ActiveBlizzard.cs — 블리자드 (법사)
//
//  타겟 위치에 블리자드 지대를 생성한다.
//  지대 내 적에게 매 틱 EffectValue 피해 + 이동속도 + 공격속도 감소.
//  지속시간 = EffectDuration, 반경 = EffectRadius.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_Blizzard", menuName = "BattleGame/Actives/Blizzard")]
public class ActiveBlizzard : ActiveSkillData
{
    [UnityEngine.Header("블리자드 설정")]
    [UnityEngine.Tooltip("이동속도 감소 배율 (예: 0.4 → 이동속도 40%)")]
    [UnityEngine.Range(0.1f, 0.9f)]
    public float MoveSlowMultiplier = 0.4f;

    [UnityEngine.Tooltip("공격속도 감소 배율 (예: 0.5 → 공격속도 50%)")]
    [UnityEngine.Range(0.1f, 0.9f)]
    public float AttackSlowMultiplier = 0.5f;

    [UnityEngine.Tooltip("틱 간격 (초)")]
    public float TickInterval = 0.5f;

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
            Radius        = EffectRadius > 0f ? EffectRadius : 3f,
            Duration      = EffectDuration > 0f ? EffectDuration : 8f,
            TickInterval  = TickInterval,
            DamagePerTick = EffectValue,
            CasterTeam    = casterIdentity.Team,
            CasterEntity  = ctx.CasterEntity,

            HasDebuff1    = true,
            Debuff1Stat   = StatType.MoveSpeed,
            Debuff1Delta  = MoveSlowMultiplier,
            Debuff1Mode   = EffectMode.Multiply,

            HasDebuff2    = true,
            Debuff2Stat   = StatType.AttackSpeed,
            Debuff2Delta  = AttackSlowMultiplier,
            Debuff2Mode   = EffectMode.Multiply,
        });
    }
}
