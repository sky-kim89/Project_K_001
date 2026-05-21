using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityArcherMultiShot.cs  [Mastery / OnBattleStart]
//  D02 궁수 달인 — 일반 공격 시 50% 확률로 2번째 적에게도 공격.
//  장군·소속 병사(Archer 직업) 모두에 ArcherMultiShotTag 부착.
//  ArcherMultiShotSystem 이 AttackedThisFrame 감지 후 추가 발사체 생성.
// ============================================================

[UnityEngine.CreateAssetMenu(
    fileName = "Ability_D02_ArcherMultiShot",
    menuName  = "ProjectK/Ability/Mastery/ArcherMultiShot")]
public class AbilityArcherMultiShot : AbilityData
{
    public override string Description
        => "일반 공격 시 50% 확률로 인접 2번째 적에게도 동일 피해 (장군·궁수 병사 모두 적용)";

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
            if (em.GetComponentData<UnitJobComponent>(s).Job != UnitJob.Archer) continue;
            ApplyToUnit(em, s);
        }
    }

    static void ApplyToUnit(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<ArcherMultiShotTag>(entity))
            em.AddComponent<ArcherMultiShotTag>(entity);
    }
}
