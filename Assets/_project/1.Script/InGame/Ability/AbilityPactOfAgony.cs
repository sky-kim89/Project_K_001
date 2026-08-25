using System.Collections.Generic;
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

    /// <summary>
    /// 전투 시작 시 <b>반드시</b> 걸리는 효과라 플레이어에겐 상시 강화다 — 합산 화면에 신고한다.
    ///
    /// ⚠ 체력 손실(AgonyCostRatio)은 신고하지 않는다
    ///   깎는 것은 최대 체력이 아니라 현재 체력이다. MaxHp 를 줄이는 것처럼 적으면
    ///   "체력 -70%" 가 총합에 섞여 장수 상세의 체력 줄과 어긋난다.
    ///   그 대가는 Description 이 글로 설명한다.
    ///
    /// ⚠ 방어율은 비율이 아니라 그대로 더한다
    ///   Defense 는 AbilityApplier.IsAbsoluteStat 목록에 있는 절대 가산 스탯이다
    ///   (0~1 자체가 값). 0.20 을 넣으면 화면에 +20%p 로 뜬다.
    /// </summary>
    public override void CollectPreviewStats(Dictionary<StatType, float> ratios)
    {
        ratios[StatType.Attack] =
            ratios.GetValueOrDefault(StatType.Attack) + AttackBonus;
        ratios[StatType.AttackSpeed] =
            ratios.GetValueOrDefault(StatType.AttackSpeed) + AtkSpdBonus;
        ratios[StatType.Defense] =
            ratios.GetValueOrDefault(StatType.Defense) + DefenseBonus;
    }

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
