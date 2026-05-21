using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityTwinStrike.cs  [Special / OnBattleStart]
//  C11 쌍신 공격 — 장군·소속 병사 모두에 DoubleStrikeTag 부착,
//  공격력 -AttackPenalty%.
//  UnitAttackSystem 이 매 공격마다 HitEvent 를 2번 추가한다.
//  DPS: (1 - penalty) × 2 — 예) 60% × 2 = 1.2× 기본 DPS
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C11_TwinStrike", menuName = "ProjectK/Ability/Special/TwinStrike")]
public class AbilityTwinStrike : AbilityData
{
    [UnityEngine.Header("쌍신 공격 설정")]
    [UnityEngine.Range(0f, 0.9f)]
    [UnityEngine.Tooltip("공격력 감소 비율 (0.40 = -40%)")]
    public float AttackPenalty = 0.40f;

    public override string Description
        => $"공격력 -{AttackPenalty * 100f:0}%  |  장군·병사 모두 2회 연타\n(DPS ×{(1f - AttackPenalty) * 2f:F2})";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;

        ApplyToUnit(em, ctx.GeneralEntity);

        // 소속 병사 모두에 적용
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<SoldierComponent>());
        using var soldiers = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        foreach (var s in soldiers)
        {
            if (!em.HasComponent<SoldierComponent>(s)) continue;
            if (em.GetComponentData<SoldierComponent>(s).GeneralEntity != ctx.GeneralEntity) continue;
            ApplyToUnit(em, s);
        }
    }

    void ApplyToUnit(EntityManager em, Entity entity)
    {
        if (em.HasComponent<StatComponent>(entity))
        {
            var stat    = em.GetComponentData<StatComponent>(entity);
            float pen   = stat.Base[StatType.Attack] * AttackPenalty;
            stat.Base[StatType.Attack]  -= pen;
            stat.Final[StatType.Attack] -= pen;
            em.SetComponentData(entity, stat);
        }

        if (!em.HasComponent<DoubleStrikeTag>(entity))
            em.AddComponent<DoubleStrikeTag>(entity);
    }
}
