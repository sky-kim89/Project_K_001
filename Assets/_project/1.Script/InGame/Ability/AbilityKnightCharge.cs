using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityKnightCharge.cs  [Mastery / OnBattleStart]
//  D01 기사 달인 — 6초마다 타겟에게 300% 피해 돌진 공격.
//  장군·소속 병사(Knight 직업) 모두에 KnightChargeComponent 부착.
// ============================================================

[UnityEngine.CreateAssetMenu(
    fileName = "Ability_D01_KnightCharge",
    menuName  = "ProjectK/Ability/Mastery/KnightCharge")]
public class AbilityKnightCharge : AbilityData
{
    [UnityEngine.Header("기사 달인 설정")]
    [UnityEngine.Tooltip("돌진 재충전 시간 (초)")]
    [UnityEngine.Range(2f, 20f)]
    public float ChargeCooldown = 6f;

    public override string Description
        => $"매 {ChargeCooldown:0}초마다 타겟에게 300% 피해 돌진 공격 (장군·기사 병사 모두 적용)";

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
            if (em.GetComponentData<UnitJobComponent>(s).Job != UnitJob.Knight) continue;
            ApplyToUnit(em, s);
        }
    }

    void ApplyToUnit(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<KnightChargeComponent>(entity))
            em.AddComponent<KnightChargeComponent>(entity);

        em.SetComponentData(entity, new KnightChargeComponent
        {
            CooldownTimer = 0f,           // 첫 공격 즉시 발동
            CooldownMax   = ChargeCooldown,
        });
    }
}
