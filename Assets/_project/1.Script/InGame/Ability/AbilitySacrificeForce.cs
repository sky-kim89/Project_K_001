using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilitySacrificeForce.cs  [Special / OnSoldierDeath]
//  C04 희생의 힘 — 병사 사망 수 × BonusPerDeath 만큼 공격력·최대체력 영구 증가 (1회 제한).
//  PassiveConditionState 를 재사용해 1회 발동을 추적한다.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C04_SacrificeForce", menuName = "ProjectK/Ability/Special/SacrificeForce")]
public class AbilitySacrificeForce : AbilityData
{
    [UnityEngine.Header("희생의 힘 설정")]
    public float AttackBonusPerDeath = 20f;
    public float MaxHpBonusPerDeath  = 50f;

    // PassiveConditionState 에 IronWill 플래그를 재사용 (SacrificeForce 1회 발동 추적)
    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnSoldierDeath;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (ctx.SoldierDeathCount <= 0) return;
        var em = ctx.EntityManager;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        // 1회 발동 추적 — PassiveConditionState 가 없으면 조건 없이 항상 발동
        if (em.HasComponent<PassiveConditionState>(ctx.GeneralEntity))
        {
            var cond = em.GetComponentData<PassiveConditionState>(ctx.GeneralEntity);
            if (cond.IronWillTriggered) return;  // 이미 발동했으면 종료
            cond.IronWillTriggered = true;
            em.SetComponentData(ctx.GeneralEntity, cond);
        }

        float atkBonus = AttackBonusPerDeath * ctx.SoldierDeathCount;
        float hpBonus  = MaxHpBonusPerDeath  * ctx.SoldierDeathCount;

        var stat = em.GetComponentData<StatComponent>(ctx.GeneralEntity);
        stat.Base[StatType.Attack]  += atkBonus;
        stat.Base[StatType.MaxHp]   += hpBonus;
        stat.Final[StatType.Attack] += atkBonus;
        stat.Final[StatType.MaxHp]  += hpBonus;
        em.SetComponentData(ctx.GeneralEntity, stat);
    }
}
