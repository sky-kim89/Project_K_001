using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityShieldMastery.cs  [Mastery / OnBattleStart]
//  D04 방패병 달인 — 넉백 완전 무시 + 자기 최대 체력에 비례한 광역 오라 피해.
//  장군·소속 병사(ShieldBearer 직업) 모두에
//  KnockbackImmuneTag + DamageAuraComponent 부착.
//
//  ■ 피해 반사에서 오라로 바꿨다 (2026-08-21)
//    반사(MirrorArmorComponent)는 "맞아야만" 값이 나온다. 그런데 방패병은
//    애초에 잘 안 맞게 만드는 직업이고(방어율·넉백 무시), 원거리 적이 많은
//    구간에서는 아예 놀았다. 오라는 서 있기만 해도 값이 나오므로
//    "앞에서 버티는 것 자체가 공격" 이라는 그림이 성립한다.
//
//  ⚠ 피해 기준이 공격력이 아니라 자기 최대 체력이다
//    방패병은 공격력이 가장 낮고 체력이 가장 높다. 공격력 계수로 만들면
//    달인 어빌리티인데도 있으나 마나 한 수치가 나온다.
//
//  ⚠ 넉백 무시는 그대로 둔다
//    이 달인의 정체성이고, 바꿔 달라고 한 것은 반사 쪽이다.
// ============================================================

[UnityEngine.CreateAssetMenu(
    fileName = "Ability_D04_ShieldMastery",
    menuName  = "ProjectK/Ability/Mastery/ShieldMastery")]
public class AbilityShieldMastery : AbilityData
{
    [UnityEngine.Header("방패병 달인 설정")]
    [UnityEngine.Range(0f, 0.2f)]
    [UnityEngine.Tooltip("1회당 피해 = 자기 최대 체력 × 이 비율 (0.02 = 2%)")]
    public float AuraHpRatio = 0.02f;

    [UnityEngine.Range(0.5f, 6f)]
    [UnityEngine.Tooltip("오라 반경. 유닛 몸집에 비례해 넓어진다.")]
    public float AuraRadius = 2.0f;

    [UnityEngine.Range(0.2f, 5f)]
    [UnityEngine.Tooltip("발동 간격 (초)")]
    public float AuraInterval = 1.0f;

    public override string Description
        => $"넉백 완전 무시\n{AuraInterval:0.#}초마다 주변 적에게 자신의 최대 체력 " +
           $"{AuraHpRatio * 100f:0.#}% 피해 (장군·방패병 병사 모두 적용)";

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

        // 첫 발동도 Interval 만큼 기다린다 — 전투 시작 순간 전원이 한 번에
        // 터지면 무슨 일이 났는지 안 읽힌다.
        var aura = new DamageAuraComponent
        {
            Radius   = AuraRadius,
            HpRatio  = AuraHpRatio,
            Interval = AuraInterval,
            Timer    = AuraInterval,
        };

        if (em.HasComponent<DamageAuraComponent>(entity))
            em.SetComponentData(entity, aura);
        else
            em.AddComponentData(entity, aura);
    }
}
