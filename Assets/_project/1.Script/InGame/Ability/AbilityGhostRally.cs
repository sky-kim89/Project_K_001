using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityGhostRally.cs  [Special / OnBattleStart]
//  C07 혼령 집결 — 이번 런에서 사망한 아군 병사 누적 수 × BonusPerDeath%
//  만큼 장군 및 소속 병사 모두의 공격력·최대체력을 강화한다.
//  (RunAbilityData.TotalSoldierDeaths 참조)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C07_GhostRally", menuName = "ProjectK/Ability/Special/GhostRally")]
public class AbilityGhostRally : AbilityData
{
    [UnityEngine.Header("혼령 집결 설정")]
    [UnityEngine.Range(0f, 0.2f)]
    [UnityEngine.Tooltip("사망 병사 1명당 공격력·체력 증가 비율 (0.05 = 5%)")]
    public float BonusPerDeath = 0.05f;

    public override string Description
        => $"이번 런 사망 병사 1명당 아군 전체 공격력·체력 +{BonusPerDeath * 100f:0}%";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var runData = UserDataManager.Instance?.Get<RunAbilityData>();
        if (runData == null) return;
        int deaths = runData.TotalSoldierDeaths;
        if (deaths <= 0) return;

        float bonus = deaths * BonusPerDeath;
        var   em    = ctx.EntityManager;

        // 장군 강화
        ApplyBonus(em, ctx.GeneralEntity, bonus);

        // 소속 병사 모두 강화
        var query   = em.CreateEntityQuery(ComponentType.ReadOnly<SoldierComponent>());
        using var soldiers = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        foreach (var s in soldiers)
        {
            if (!em.HasComponent<SoldierComponent>(s)) continue;
            if (em.GetComponentData<SoldierComponent>(s).GeneralEntity != ctx.GeneralEntity) continue;
            ApplyBonus(em, s, bonus);
        }
    }

    static void ApplyBonus(EntityManager em, Entity entity, float bonus)
    {
        if (!em.HasComponent<StatComponent>(entity)) return;
        var stat = em.GetComponentData<StatComponent>(entity);

        stat.Base[StatType.Attack]  *= (1f + bonus);
        stat.Final[StatType.Attack] *= (1f + bonus);
        stat.Base[StatType.MaxHp]   *= (1f + bonus);
        stat.Final[StatType.MaxHp]  *= (1f + bonus);
        em.SetComponentData(entity, stat);

        if (em.HasComponent<HealthComponent>(entity))
        {
            var h = em.GetComponentData<HealthComponent>(entity);
            h.CurrentHp *= (1f + bonus);
            em.SetComponentData(entity, h);
        }
    }
}
