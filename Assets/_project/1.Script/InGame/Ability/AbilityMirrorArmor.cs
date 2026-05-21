using BattleGame.Units;

// ============================================================
//  AbilityMirrorArmor.cs  [Special / OnBattleStart]
//  C06 거울 방어 — 전투 시작 시 장군에게 MirrorArmorComponent 를 부착.
//  이후 UnitHitSystem 이 매 피격마다 공격자에게 반사 피해를 돌려준다.
//  반사된 피해는 공격자의 방어율 적용을 받으며, 재반사는 불가능하다.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C06_MirrorArmor", menuName = "ProjectK/Ability/Special/MirrorArmor")]
public class AbilityMirrorArmor : AbilityData
{
    [UnityEngine.Header("거울 방어 설정")]
    [UnityEngine.Range(0f, 1f)]
    [UnityEngine.Tooltip("피격 피해(방어 적용 전)의 반사 비율 (0.25 = 25%)")]
    public float ReflectRatio = 0.25f;

    public override string Description
        => $"피격마다 원본 피해의 {ReflectRatio * 100f:0}%를 공격자에게 반사\n(공격자 방어율 적용, 무한 반사 불가)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<MirrorArmorComponent>(ctx.GeneralEntity))
            em.AddComponentData(ctx.GeneralEntity, new MirrorArmorComponent { ReflectRatio = ReflectRatio });
    }
}
