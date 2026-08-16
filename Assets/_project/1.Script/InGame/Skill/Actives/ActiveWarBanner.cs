using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveWarBanner.cs — 군기 강림 (공통 · 희귀)
//
//  시전자 자리에 군기를 세워 **주변 아군**을 강화한다.
//    · 반경 안 아군 전원 공격력·공격속도 대폭 증가 (지속 시간 동안)
//    · 버프는 발동 순간에 범위 안에 있던 대상에게만 걸린다
//
//  ⚠ 부술 수 있는 오브젝트가 아니다
//    처음엔 군기를 실제 유닛으로 세우려 했지만, 그러면 적 타겟팅·생존 카운트·
//    사망 처리에 전부 예외가 생긴다. 지금은 시전자 주변 즉발 버프다.
//
//  EffectRadius   : 버프 반경
//  EffectDuration : 버프 지속 시간
// ============================================================

[CreateAssetMenu(fileName = "Active_WarBanner", menuName = "BattleGame/Actives/WarBanner")]
public class ActiveWarBanner : ActiveSkillData
{
    [Header("군기 강림 설정")]
    [Tooltip("공격력 증가 배율 (1.4 = +40%)")]
    public float AttackMultiplier = 1.4f;

    [Tooltip("공격속도 증가 배율 (1.4 = +40%)")]
    public float AttackSpeedMultiplier = 1.4f;

    [Tooltip("이동속도 증가 배율 (1.15 = +15%)")]
    public float MoveSpeedMultiplier = 1.15f;

    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        if (!em.HasComponent<LocalTransform>(ctx.CasterEntity)) return;

        var    casterTf  = em.GetComponentData<LocalTransform>(ctx.CasterEntity);
        float3 casterPos = new float3(casterTf.Position.x, casterTf.Position.y, 0f);
        var    center    = new Vector3(casterPos.x, casterPos.y, 0f);

        float radius   = EffectRadius   > 0f ? EffectRadius   : 6f;
        float duration = EffectDuration > 0f ? EffectDuration : 8f;

        // 군기 — 반경에 맞춰 깔고 지속 시간만큼 유지한다 (프리팹 기준 반경 3)
        SkillEffectHelper.Spawn(BaseEffectKey, center, duration + EffectDespawnDelay,
                                Quaternion.identity, radius / 3f);
        SkillEffectHelper.Spawn(CasterEffectKey, center, EffectDespawnDelay);

        var identity = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);

        // 반경 안 아군만 — CollectAllies 는 전장 전체라 거리로 다시 거른다
        float radiusSq = radius * radius;
        int   buffed   = 0;

        foreach (var a in SkillCrowdControl.CollectAllies(em, identity.Team))
        {
            if (!em.Exists(a)) continue;

            Vector3 p = SkillCrowdControl.PositionOf(em, a);
            if (math.lengthsq(new float3(p.x - casterPos.x, p.y - casterPos.y, 0f)) > radiusSq) continue;

            SkillCrowdControl.AddEffect(em, a, StatType.Attack, AttackMultiplier,
                                        EffectMode.Multiply, duration, ActiveSkillId.WarBanner);
            SkillCrowdControl.AddEffect(em, a, StatType.AttackSpeed, AttackSpeedMultiplier,
                                        EffectMode.Multiply, duration, ActiveSkillId.WarBanner);
            SkillCrowdControl.AddEffect(em, a, StatType.MoveSpeed, MoveSpeedMultiplier,
                                        EffectMode.Multiply, duration, ActiveSkillId.WarBanner);

            SkillEffectHelper.Spawn(TargetEffectKey, p, EffectDespawnDelay);
            buffed++;
        }

        if (buffed == 0)
            Debug.LogWarning("[WarBanner] 반경 안에 아군이 없다 — 반경이 너무 작지 않은지 확인할 것.");
    }
}
