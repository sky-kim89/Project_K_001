using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  AbilityPactOfAgony.cs  [Special / OnBattleStart]
//  C05 고통의 계약 — 전투 시작 시 최대HP의 AgonyCostRatio 만큼 즉시 피해를 받는 대신
//  공격력·공격속도·방어율을 영구 강화한다.
//
//  BloodPact 패시브와 시너지: 낮은 HP 구간에서 공격력 추가 증가.
//  초반 생존력을 의도적으로 포기하는 고위험 고보상 빌드 전용.
// ============================================================

[CreateAssetMenu(fileName = "Ability_C05_PactOfAgony", menuName = "ProjectK/Ability/Special/PactOfAgony")]
public class AbilityPactOfAgony : AbilityData
{
    [Header("고통의 계약 설정")]
    [Range(0.1f, 0.9f)]
    [Tooltip("전투 시작 시 최대HP에서 차감되는 비율 (기본 0.7 = 70%)")]
    public float AgonyCostRatio = 0.70f;

    [Range(0f, 1f)]
    [Tooltip("공격력 강화 비율 (기본 0.35 = +35%)")]
    public float AttackBonus = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("공격속도 강화 비율 (기본 0.20 = +20%)")]
    public float AtkSpdBonus = 0.20f;

    [Range(0f, 1f)]
    [Tooltip("방어율 강화 비율 (기본 0.20 = +20%p)")]
    public float DefenseBonus = 0.20f;

    public override string Description
        => $"전투 시작 시 최대HP {AgonyCostRatio * 100f:0}% 손실\n" +
           $"공격력 +{AttackBonus * 100f:0}%  공격속도 +{AtkSpdBonus * 100f:0}%  방어율 +{DefenseBonus * 100f:0}%p";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
        => Apply(ctx.GeneralEntity, ctx.EntityManager);

    void Apply(Entity generalEntity, EntityManager em)
    {
        if (!em.Exists(generalEntity)) return;

        // ── HP 손실 ─────────────────────────────────────────────
        if (em.HasComponent<HealthComponent>(generalEntity) &&
            em.HasComponent<StatComponent>(generalEntity))
        {
            var stat   = em.GetComponentData<StatComponent>(generalEntity);
            var health = em.GetComponentData<HealthComponent>(generalEntity);
            float maxHp    = stat.Final[StatType.MaxHp];
            float cost     = maxHp * AgonyCostRatio;
            health.CurrentHp = Mathf.Max(1f, health.CurrentHp - cost);
            em.SetComponentData(generalEntity, health);
        }

        // ── 공격력·공격속도·방어율 영구 강화 (StatusEffect 무기한) ──
        if (!em.HasBuffer<StatusEffectBufferElement>(generalEntity)) return;
        var buf = em.GetBuffer<StatusEffectBufferElement>(generalEntity);

        const float InfiniteDuration = 99999f;
        const int   SourceId         = (int)AbilityId.C05;

        if (AttackBonus > 0f)
            buf.Add(new StatusEffectBufferElement
            {
                Stat       = StatType.Attack,
                Delta      = 1f + AttackBonus,   // Multiply: Final *= Delta → 1.35 = +35%
                Mode       = EffectMode.Multiply,
                Duration   = InfiniteDuration,
                Remaining  = InfiniteDuration,
                SourceType = BuffSourceType.ActiveSkill,
                SourceId   = SourceId,
            });

        if (AtkSpdBonus > 0f)
            buf.Add(new StatusEffectBufferElement
            {
                Stat       = StatType.AttackSpeed,
                Delta      = 1f + AtkSpdBonus,   // Multiply: Final *= Delta → 1.20 = +20%
                Mode       = EffectMode.Multiply,
                Duration   = InfiniteDuration,
                Remaining  = InfiniteDuration,
                SourceType = BuffSourceType.ActiveSkill,
                SourceId   = SourceId,
            });

        if (DefenseBonus > 0f)
            buf.Add(new StatusEffectBufferElement
            {
                Stat       = StatType.Defense,
                Delta      = DefenseBonus,
                Mode       = EffectMode.Add,
                Duration   = InfiniteDuration,
                Remaining  = InfiniteDuration,
                SourceType = BuffSourceType.ActiveSkill,
                SourceId   = SourceId,
            });
    }
}
