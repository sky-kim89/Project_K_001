using BattleGame.Units;

// ============================================================
//  AbilityArmorReaction.cs  [Special / OnHit]
//  C02 철갑 반응 — 피격 시 방어율 +DefenseBuff 를 BuffDuration 초 동안 적용.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C02_ArmorReaction", menuName = "ProjectK/Ability/Special/ArmorReaction")]
public class AbilityArmorReaction : AbilityData
{
    [UnityEngine.Header("철갑 반응 설정")]
    [UnityEngine.Range(0f, 0.5f)]
    public float DefenseBuff  = 0.15f;
    public float BuffDuration = 5f;

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnHit;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasBuffer<StatusEffectBufferElement>(ctx.GeneralEntity)) return;

        em.GetBuffer<StatusEffectBufferElement>(ctx.GeneralEntity).Add(new StatusEffectBufferElement
        {
            Stat      = StatType.Defense,
            Delta     = DefenseBuff,
            Mode      = EffectMode.Add,
            Duration  = BuffDuration,
            Remaining = BuffDuration,
        });
    }
}
