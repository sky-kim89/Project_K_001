using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  AbilityVampiricAssault.cs  [Special / OnAttack]
//  C01 흡혈 강습 — 공격 시 가한 피해의 VampireRatio% 즉시 체력 회복.
// ============================================================

[CreateAssetMenu(fileName = "Ability_C01_VampiricAssault", menuName = "ProjectK/Ability/Special/VampiricAssault")]
public class AbilityVampiricAssault : AbilityData
{
    [Header("흡혈 강습 설정")]
    [Range(0f, 0.5f)]
    public float VampireRatio = 0.15f;

    public override string Description
        => $"공격마다 가한 피해의 {VampireRatio * 100f:0}% 즉시 체력 회복";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnAttack;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (ctx.DamageDealt <= 0f) return;
        var em = ctx.EntityManager;
        if (!em.HasBuffer<HealEventBufferElement>(ctx.GeneralEntity)) return;

        float heal = ctx.DamageDealt * VampireRatio;
        em.GetBuffer<HealEventBufferElement>(ctx.GeneralEntity).Add(
            new HealEventBufferElement { Amount = heal, SourceEntity = ctx.GeneralEntity });
    }
}
