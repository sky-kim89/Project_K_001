// ⚠ GetValueOrDefault 는 확장 메서드다 — 타입을 풀네임으로 써도 이 using 없이는 안 잡힌다
using System.Collections.Generic;
using BattleGame.Units;

// ============================================================
//  AbilityTimeWarp.cs  [Special / OnBattleStart]
//  C10 시간 왜곡 — 스킬 쿨타임 -CooldownBonus(비율),
//  공격력 -AttackPenalty%.
//  스킬 의존 빌드용 고위험 어빌리티.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C10_TimeWarp", menuName = "ProjectK/Ability/Special/TimeWarp")]
public class AbilityTimeWarp : AbilityData
{
    [UnityEngine.Header("시간 왜곡 설정")]
    [UnityEngine.Range(0f, 1f)]
    [UnityEngine.Tooltip("스킬 쿨타임 감소 비율 (0.35 = 쿨타임 35% 감소)")]
    public float CooldownBonus = 0.35f;

    [UnityEngine.Range(0f, 0.5f)]
    [UnityEngine.Tooltip("공격력 감소 비율 (0.10 = -10%)")]
    public float AttackPenalty = 0.10f;

    // 다른 쿨감소 표기(마법의 각성 "+15%")와 단위를 맞춘다.
    // "+0.35" 로 적으면 15% 보다 작아 보이지만 실제로는 두 배 넘게 크다.
    public override string Description
        => $"스킬 쿨타임 -{CooldownBonus * 100f:0}%  |  공격력 -{AttackPenalty * 100f:0}%";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    // 전투 시작 시 조건 없이 무조건 걸린다 = 플레이어에겐 상시 효과다.
    // 신고하지 않으면 로비 스탯 화면에서 쿨타임 -35% 가 통째로 안 보인다.
    public override void CollectPreviewStats(Dictionary<StatType, float> ratios)
    {
        ratios[StatType.SkillCooldownReduce] =
            ratios.GetValueOrDefault(StatType.SkillCooldownReduce) + CooldownBonus;

        // 페널티는 음수로 — 합산 화면에 "공격력 -10%" 로 정직하게 뜬다
        ratios[StatType.Attack] =
            ratios.GetValueOrDefault(StatType.Attack) - AttackPenalty;
    }

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        var stat = em.GetComponentData<StatComponent>(ctx.GeneralEntity);

        stat.Base[StatType.SkillCooldownReduce]  += CooldownBonus;
        stat.Final[StatType.SkillCooldownReduce] += CooldownBonus;

        float atkPenalty = stat.Base[StatType.Attack] * AttackPenalty;
        stat.Base[StatType.Attack]  -= atkPenalty;
        stat.Final[StatType.Attack] -= atkPenalty;

        em.SetComponentData(ctx.GeneralEntity, stat);

        ApplyCooldown(em, ctx.GeneralEntity);
    }

    /// <summary>
    /// 실제 쿨타임을 줄인다.
    ///
    /// ⚠ StatComponent 에 쿨감소를 더하는 것만으로는 아무 일도 일어나지 않는다
    ///   장군의 스킬 쿨타임은 **스폰 시점에 한 번 계산되어 값으로 굳는다**
    ///   (GeneralRuntimeBridge: Cooldown = base × (1 - cdr)).
    ///   전투 시작 시 발동하는 이 어빌리티는 그보다 늦으므로, 굳어 있는
    ///   GeneralActiveSkillComponent.Cooldown 을 직접 다시 계산해야 한다.
    ///   그 전까지 이 어빌리티는 공격력 -10% 페널티만 있는 순수 하향이었다.
    ///
    /// ⚠ 합연산이 아니라 잔여 곱연산이다
    ///   SkillCooldownReduce 는 CombineMode.MultiplyResidual 로 합쳐진다
    ///   (UnitJobRoller). 여기서만 단순 가산하면 다른 쿨감소와 규칙이 어긋난다.
    ///   기존 쿨감소는 SO 의 원래 쿨타임과 현재 값을 비교해 역산한다.
    /// </summary>
    void ApplyCooldown(Unity.Entities.EntityManager em, Unity.Entities.Entity general)
    {
        if (!em.HasComponent<GeneralActiveSkillComponent>(general)) return;

        var skill = em.GetComponentData<GeneralActiveSkillComponent>(general);

        var db     = ActiveSkillDatabase.Current;
        var data   = db?.Get((ActiveSkillId)skill.SkillId);
        float baseCd = data != null ? data.Cooldown : skill.Cooldown;
        if (baseCd <= 0f) return;

        float currentCdr = 1f - skill.Cooldown / baseCd;
        float mergedCdr  = 1f - (1f - currentCdr) * (1f - CooldownBonus);

        float maxCdr = UnityEngine.Application.isPlaying && GameplayConfig.Current != null
                     ? GameplayConfig.Current.CooldownReduceMax
                     : 0.8f;

        skill.Cooldown = baseCd * (1f - GeneralRuntimeBridge.ClampCDR(mergedCdr, maxCdr));

        // 이미 돌고 있는 쿨타임이 새 값을 넘지 않게 같이 당겨준다
        if (skill.CooldownRemaining > skill.Cooldown)
            skill.CooldownRemaining = skill.Cooldown;

        em.SetComponentData(general, skill);
    }
}
