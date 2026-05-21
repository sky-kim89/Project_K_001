using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityShieldMastery.cs  [Mastery / OnBattleStart]
//  D04 방패병 달인 — 넉백 완전 무시 + 실제 받은 피해(방어 적용 후)의 25% 반사.
//  장군·소속 병사(ShieldBearer 직업) 모두에
//  KnockbackImmuneTag + MirrorArmorComponent(ReflectRatio=0.25) 부착.
// ============================================================

[UnityEngine.CreateAssetMenu(
    fileName = "Ability_D04_ShieldMastery",
    menuName  = "ProjectK/Ability/Mastery/ShieldMastery")]
public class AbilityShieldMastery : AbilityData
{
    [UnityEngine.Header("방패병 달인 설정")]
    [UnityEngine.Range(0f, 1f)]
    public float ReflectRatio = 0.25f;

    public override string Description
        => $"넉백 완전 무시 + 실제 받은 피해(방어 적용 후)의 {ReflectRatio * 100f:0}% 반사 (장군·방패병 병사 모두 적용)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;

        ApplyToUnit(em, ctx.GeneralEntity);

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<SoldierComponent>(),
            ComponentType.ReadOnly<UnitJobComponent>());
        using var soldiers = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        foreach (var s in soldiers)
        {
            if (!em.HasComponent<SoldierComponent>(s)) continue;
            if (em.GetComponentData<SoldierComponent>(s).GeneralEntity != ctx.GeneralEntity) continue;
            if (!em.HasComponent<UnitJobComponent>(s)) continue;
            if (em.GetComponentData<UnitJobComponent>(s).Job != UnitJob.ShieldBearer) continue;
            ApplyToUnit(em, s);
        }
    }

    void ApplyToUnit(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<KnockbackImmuneTag>(entity))
            em.AddComponent<KnockbackImmuneTag>(entity);

        if (em.HasComponent<MirrorArmorComponent>(entity))
            em.SetComponentData(entity, new MirrorArmorComponent { ReflectRatio = ReflectRatio });
        else
            em.AddComponentData(entity, new MirrorArmorComponent { ReflectRatio = ReflectRatio });
    }
}
