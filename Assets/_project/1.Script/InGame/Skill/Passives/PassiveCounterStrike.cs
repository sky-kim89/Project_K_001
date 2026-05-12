using BattleGame.Units;

// ============================================================
//  PassiveCounterStrike.cs  [OnHit]
//  피격 반격 강화 — 피격 시 X% 확률로 공격력 +N% T초 버프.
//
//  Inspector:
//    TriggerType = OnHit
//    TriggerChance: 발동 확률 (0.4 = 40%)
//    AttackBonusRatio: 기본 공격력 대비 버프 비율 (0.2 = +20%)
//    BuffDuration: 지속 시간(초)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Passive_CounterStrike", menuName = "BattleGame/Passives/CounterStrike")]
public class PassiveCounterStrike : PassiveSkillData
{
    [UnityEngine.Header("피격 반격 강화 설정")]
    [UnityEngine.Range(0f, 1f)]
    public float TriggerChance      = 0.40f;
    [UnityEngine.Range(0f, 1f)]
    public float AttackBonusRatio   = 0.20f;
    public float BuffDuration       = 5f;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (UnityEngine.Random.value > TriggerChance) return;
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
