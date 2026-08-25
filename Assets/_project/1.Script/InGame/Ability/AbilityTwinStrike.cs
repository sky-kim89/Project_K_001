// ⚠ GetValueOrDefault 는 확장 메서드다 — 타입을 풀네임으로 써도 이 using 없이는 안 잡힌다
using System.Collections.Generic;
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

    /// <summary>
    /// 공격력 감소는 전투 시작 시 반드시 걸린다 — 상시 효과로 신고한다.
    ///
    /// ⚠ 페널티만 신고하는 게 이상해 보여도 이게 맞다
    ///   2회 연타는 StatType 으로 표현할 수 없다(DoubleStrikeTag). 그렇다고
    ///   공격력 감소를 숨기면 화면의 합산 공격력이 실제보다 높게 뜬다 —
    ///   총합은 '틀린 것보다 불완전한 편' 이 낫다. 연타는 Description 이 설명한다.
    /// </summary>
    public override void CollectPreviewStats(Dictionary<StatType, float> ratios)
        => ratios[StatType.Attack] = ratios.GetValueOrDefault(StatType.Attack) - AttackPenalty;

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
