using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityMageSkillProc.cs  [Mastery / OnBattleStart]
//  D03 마법사 달인 — 일반 공격 시 1% 확률로 보유 스킬 즉시 발동.
//  장군에게만 MageSkillProcTag 부착 (스킬은 장군만 보유).
//  MageSkillProcSystem 이 AttackedThisFrame 감지 후 UseActiveSkillTag 추가.
// ============================================================

[UnityEngine.CreateAssetMenu(
    fileName = "Ability_D03_MageSkillProc",
    menuName  = "ProjectK/Ability/Mastery/MageSkillProc")]
public class AbilityMageSkillProc : AbilityData
{
    public override string Description
        => "일반 공격 시 1% 확률로 보유 스킬 즉시 발동 (마법사 장군·병사 전체 공격력 영향)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnBattleStart;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasComponent<MageSkillProcTag>(ctx.GeneralEntity))
            em.AddComponent<MageSkillProcTag>(ctx.GeneralEntity);
    }
}
