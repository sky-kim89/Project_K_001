using BattleGame.Units;

// ============================================================
//  AbilitySacrificeForce.cs  [Special / OnSoldierDeath]
//  C04 희생의 힘 — 병사 사망마다 장군 공격력·체력 누적 증가.
//  전투 내내 유지 (Duration -1), 스테이지 종료 시 초기화.
//  Attack / MaxHp 각각 영구 Add 항목으로 버퍼에 합산 관리.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C04_SacrificeForce", menuName = "ProjectK/Ability/Special/SacrificeForce")]
public class AbilitySacrificeForce : AbilityData
{
    [UnityEngine.Header("희생의 힘 설정")]
    [UnityEngine.Range(0f, 1f)]
    public float AttackBonusRatio = 0.05f;
    [UnityEngine.Range(0f, 1f)]
    public float MaxHpBonusRatio  = 0.05f;

    public override string Description
        => $"병사 사망마다 공격력 +{AttackBonusRatio * 100f:0}%, 체력 +{MaxHpBonusRatio * 100f:0}%  (전투 내 누적)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnSoldierDeath;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (ctx.SoldierDeathCount <= 0) return;
        var em = ctx.EntityManager;
        if (!em.HasBuffer<StatusEffectBufferElement>(ctx.GeneralEntity)) return;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        var  stat   = em.GetComponentData<StatComponent>(ctx.GeneralEntity);
        var  buf    = em.GetBuffer<StatusEffectBufferElement>(ctx.GeneralEntity);
        int  deaths = ctx.SoldierDeathCount;

        float atkBonus = stat.Base[StatType.Attack] * AttackBonusRatio * deaths;
        float hpBonus  = stat.Base[StatType.MaxHp]  * MaxHpBonusRatio  * deaths;

        AddOrMerge(buf, StatType.Attack, atkBonus);
        AddOrMerge(buf, StatType.MaxHp,  hpBonus);
    }

    static void AddOrMerge(
        Unity.Entities.DynamicBuffer<StatusEffectBufferElement> buf,
        StatType stat, float delta)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            var b = buf[i];
            if (b.Stat == stat && b.Mode == EffectMode.Add && b.Duration < 0f)
            {
                b.Delta += delta;
                buf[i]   = b;
                return;
            }
        }
        buf.Add(new StatusEffectBufferElement
        {
            Stat      = stat,
            Delta     = delta,
            Mode      = EffectMode.Add,
            Duration  = -1f,
            Remaining = float.MaxValue,
        });
    }
}
