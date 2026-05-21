// ============================================================
//  AbilityGoldBonus.cs  [Special / 시스템]
//  C08 황금 탐욕 — 스테이지 골드 보상 +GoldBonusRatio%.
//  NormalMode.ApplyStageClearReward() 에서 AbilityApplier.GetGoldBonusRatio() 로 참조.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C08_GoldBonus", menuName = "ProjectK/Ability/Special/GoldBonus")]
public class AbilityGoldBonus : AbilityData
{
    [UnityEngine.Header("황금 탐욕 설정")]
    [UnityEngine.Range(0f, 1f)]
    [UnityEngine.Tooltip("골드 보상 추가 비율 (0.30 = +30%)")]
    public float GoldBonusRatio = 0.30f;

    public override string Description
        => $"스테이지 골드 보상 +{GoldBonusRatio * 100f:0}%";
}
