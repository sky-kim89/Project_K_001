using Unity.Mathematics;
using Unity.Transforms;
using BattleGame.Units;

// ============================================================
//  ActiveSkillZoneBase.cs
//  지속 효과 영역(Zone) 스킬의 공통 Execute 로직 추상 베이스.
//
//  ■ 상속 스킬: ActivePoisonZone / ActiveBlizzard / ActiveArrowRain
//
//  ■ 서브클래스 작성 규칙
//    - DefaultRadius   : 기본 반경 (EffectRadius 가 0 일 때 사용)
//    - DefaultDuration : 기본 지속 시간 (EffectDuration 이 0 일 때 사용)
//    - ConfigureDebuffs(ref ZoneConfig) : 디버프 설정 오버라이드 (선택)
// ============================================================

public abstract class ActiveSkillZoneBase : ActiveSkillData
{
    [UnityEngine.Header("존 공통 설정")]
    [UnityEngine.Tooltip("틱 간격 (초)")]
    public float TickInterval = 0.5f;

    protected abstract float DefaultRadius   { get; }
    protected abstract float DefaultDuration { get; }

    /// <summary>디버프 설정. 기본값은 디버프 없음.</summary>
    protected virtual void ConfigureDebuffs(ref SkillZoneRunner.ZoneConfig config) { }

    public override void Execute(ActiveSkillContext ctx)
    {
        if (!ctx.HasTarget) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        float3 center = float3.zero;
        if (em.HasComponent<LocalTransform>(ctx.TargetEntity))
            center = em.GetComponentData<LocalTransform>(ctx.TargetEntity).Position;

        var identity = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);
        var runner   = ctx.CasterObject.AddComponent<SkillZoneRunner>();

        var config = new SkillZoneRunner.ZoneConfig
        {
            Center             = center,
            Radius             = EffectRadius   > 0f ? EffectRadius   : DefaultRadius,
            Duration           = EffectDuration > 0f ? EffectDuration : DefaultDuration,
            TickInterval       = TickInterval,
            DamagePerTick      = EffectValue,
            CasterTeam         = identity.Team,
            CasterEntity       = ctx.CasterEntity,
            BaseEffectKey      = BaseEffectKey,
            EffectDespawnDelay = EffectDespawnDelay,
        };

        ConfigureDebuffs(ref config);
        runner.Setup(config);

#if UNITY_EDITOR
        ActiveSkillDebugOverlay.RegisterZone(
            new UnityEngine.Vector3(center.x, center.y, 0f),
            config.Radius,
            $"{SkillName} r={config.Radius:F1}");
#endif
    }
}
