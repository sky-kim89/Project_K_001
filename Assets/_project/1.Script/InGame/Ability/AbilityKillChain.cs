using BattleGame.Units;

// ============================================================
//  AbilityKillChain.cs  [Special / OnEnemyKill]
//  C03 처치 연쇄 — 처치 시 공격력 +AttackBonusRatio% 를 BuffDuration 초 버프.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C03_KillChain", menuName = "ProjectK/Ability/Special/KillChain")]
public class AbilityKillChain : AbilityData
{
    [UnityEngine.Header("처치 연쇄 설정")]
    [UnityEngine.Range(0f, 1f)]
    public float AttackBonusRatio = 0.20f;
    public float BuffDuration     = 5f;

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnEnemyKill;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasBuffer<StatusEffectBufferElement>(ctx.GeneralEntity)) return;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        float bonus = em.GetComponentData<StatComponent>(ctx.GeneralEntity).Base[StatType.Attack] * AttackBonusRatio;

        em.GetBuffer<StatusEffectBufferElement>(ctx.GeneralEntity).Add(new StatusEffectBufferElement
        {
            Stat      = StatType.Attack,
            Delta     = bonus,
            Mode      = EffectMode.Add,
            Duration  = BuffDuration,
            Remaining = BuffDuration,
        });
    }
}
