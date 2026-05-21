using BattleGame.Units;

// ============================================================
//  AbilityTimeWarp.cs  [Special / OnBattleStart]
//  C10 시간 왜곡 — 스킬 쿨다운 감소 +CooldownBonus(절대값),
//  공격력 -AttackPenalty%.
//  스킬 의존 빌드용 고위험 어빌리티.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C10_TimeWarp", menuName = "ProjectK/Ability/Special/TimeWarp")]
public class AbilityTimeWarp : AbilityData
{
    [UnityEngine.Header("시간 왜곡 설정")]
    [UnityEngine.Range(0f, 1f)]
    [UnityEngine.Tooltip("스킬 쿨다운 감소 추가량 (절대값, 0.35 = +0.35)")]
    public float CooldownBonus = 0.35f;

    [UnityEngine.Range(0f, 0.5f)]
    [UnityEngine.Tooltip("공격력 감소 비율 (0.10 = -10%)")]
    public float AttackPenalty = 0.10f;

    public override string Description
        => $"스킬 쿨다운 감소 +{CooldownBonus:F2}  |  공격력 -{AttackPenalty * 100f:0}%";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        var stat = em.GetComponentData<StatComponent>(ctx.GeneralEntity);

        stat.Base[StatType.SkillCooldownReduce]  += CooldownBonus;
        stat.Final[StatType.SkillCooldownReduce] += CooldownBonus;

        float atkPenalty = stat.Base[StatType.Attack] * AttackPenalty;
        stat.Base[StatType.Attack]  -= atkPenalty;
        stat.Final[StatType.Attack] -= atkPenalty;

        em.SetComponentData(ctx.GeneralEntity, stat);
    }
}
