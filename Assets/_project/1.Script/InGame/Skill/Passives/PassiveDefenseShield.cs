using BattleGame.Units;

// ============================================================
//  PassiveDefenseShield.cs  [OnHit]
//  방어 강화 — 피격 시 방어율 +N% T초 버프.
//
//  Inspector:
//    TriggerType = OnHit
//    DefenseBuff: 방어율 증가 절대값 (0.1 = +10%)
//    BuffDuration: 지속 시간(초)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Passive_DefenseShield", menuName = "BattleGame/Passives/DefenseShield")]
public class PassiveDefenseShield : PassiveSkillData
{
    [UnityEngine.Header("방어 강화 설정")]
    [UnityEngine.Range(0f, 0.5f)]
    public float DefenseBuff  = 0.10f;
    public float BuffDuration = 3f;

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
