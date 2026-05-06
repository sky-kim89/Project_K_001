using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  ActiveIronShield.cs — 철벽 방어 (방패)
//
//  시전자의 방어율을 EffectDuration 초 동안 EffectValue 만큼 추가한다.
//  (예: EffectValue = 0.3 → 방어율 +30%)
//  StatType.Defense 는 0~1 범위, UnitStatusEffectSystem 에서 DefenseMax 로 클램프됨.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_IronShield", menuName = "BattleGame/Actives/IronShield")]
public class ActiveIronShield : ActiveSkillData
{
    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        if (!em.HasBuffer<StatusEffectBufferElement>(ctx.CasterEntity)) return;

        float duration = EffectDuration > 0f ? EffectDuration : 8f;

        // 사용자 이펙트 (방어막 연출)
        if (ctx.CasterTransform != null)
            SkillEffectHelper.SpawnCaster(CasterEffectKey, ctx.CasterTransform.position, EffectDespawnDelay);

        em.GetBuffer<StatusEffectBufferElement>(ctx.CasterEntity).Add(new StatusEffectBufferElement
        {
            Stat      = StatType.Defense,
            Delta     = EffectValue,   // Add: 방어율 + EffectValue
            Mode      = EffectMode.Add,
            Duration  = duration,
            Remaining = duration,
        });
    }
}
