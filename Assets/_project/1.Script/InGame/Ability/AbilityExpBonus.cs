// ============================================================
//  AbilityExpBonus.cs  [Special / 시스템]
//  C09 성장 촉진 — 장군 경험치 획득량 +ExpBonusRatio%.
//  InGameManager.HandleVictory() 에서 AbilityApplier.GetExpBonusRatio() 로 참조.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C09_ExpBonus", menuName = "ProjectK/Ability/Special/ExpBonus")]
public class AbilityExpBonus : AbilityData
{
    [UnityEngine.Header("성장 촉진 설정")]
    [UnityEngine.Range(0f, 1f)]
    [UnityEngine.Tooltip("경험치 보상 추가 비율 (0.30 = +30%)")]
    public float ExpBonusRatio = 0.30f;

    public override string Description
        => $"장군 경험치 획득량 +{ExpBonusRatio * 100f:0}%";
}
