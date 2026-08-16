using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveDeathSentence.cs — 사형 선고 (공통 · 희귀)
//
//  넓은 범위의 적에게 낙인을 찍고, 잠시 뒤 일제히 처형한다.
//    ① 범위 내 적 전원에게 낙인 (2초간 붉게 맥동)
//    ② 처형 순간 — 체력 비율이 기준 이하인 적은 **즉사**
//    ③ 기준을 넘긴 적은 큰 피해만 받는다
//    ④ 처형한 수만큼 시전자 공격력이 영구(이번 전투) 누적
//
//  ⚠ 즉사도 피해 파이프라인을 거친다
//    DeadTag 를 직접 붙이면 킬 수·전투 통계·처치 트리거가 전부 누락된다.
//    현재 체력을 뚫을 만큼의 피해를 넣어 UnitHitSystem 이 죽이게 한다.
//
//  EffectRadius   : 낙인 반경
//  EffectDuration : 낙인 → 처형까지의 시간
// ============================================================

[CreateAssetMenu(fileName = "Active_DeathSentence", menuName = "BattleGame/Actives/DeathSentence")]
public class ActiveDeathSentence : ActiveSkillData
{
    [Header("사형 선고 설정")]
    [Tooltip("즉사 기준 체력 비율. 0.35 = 체력 35% 이하면 처형.")]
    [Range(0f, 1f)]
    public float ExecuteHpRatio = 0.35f;

    [Tooltip("처형을 피한 적에게 주는 공격력 배율")]
    public float DamageMultiplier = 4f;

    [Tooltip("처형 1명당 시전자 공격력 증가량 (전투 내 영구 누적)")]
    public float AttackPerExecute = 6f;

    [Tooltip("보스에게도 즉사를 적용할지. 꺼두면 보스는 피해만 받는다.")]
    public bool ExecuteBosses = false;

    [Tooltip("넉백 배율")]
    public float KnockbackMult = 3f;

    public override void Execute(ActiveSkillContext ctx)
    {
        if (ctx.CasterObject == null) return;

        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        Vector3 center = ctx.HasTarget
            ? ctx.TargetPosition
            : ctx.CasterTransform.position + Vector3.right * 4f;

        var identity = em.GetComponentData<UnitIdentityComponent>(ctx.CasterEntity);

        var runner = ctx.CasterObject.GetComponent<DeathSentenceRunner>();
        if (runner == null) runner = ctx.CasterObject.AddComponent<DeathSentenceRunner>();

        runner.Run(
            em            : em,
            casterEntity  : ctx.CasterEntity,
            casterTeam    : identity.Team,
            center        : new float3(center.x, center.y, 0f),
            radius        : EffectRadius   > 0f ? EffectRadius   : 6f,
            markDuration  : EffectDuration > 0f ? EffectDuration : 2f,
            executeRatio  : ExecuteHpRatio,
            damage        : ctx.CasterStat.Final[StatType.Attack] * DamageMultiplier * EffectValue,
            attackPerKill : AttackPerExecute,
            executeBosses : ExecuteBosses,
            knockMult     : KnockbackMult,
            fx            : new SkillEffectConfig
            {
                CasterEffectKey = CasterEffectKey,
                TargetEffectKey = TargetEffectKey,
                BaseEffectKey   = BaseEffectKey,
                DespawnDelay    = EffectDespawnDelay,
            });
    }
}
