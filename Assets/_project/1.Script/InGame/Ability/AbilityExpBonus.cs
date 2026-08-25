// ⚠ GetValueOrDefault 는 확장 메서드다 — 타입을 풀네임으로 써도 이 using 없이는 안 잡힌다
using System.Collections.Generic;

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

    /// <summary>
    /// 트리거가 아예 없는 상시 효과다 — 합산 화면에 신고한다.
    /// ExpGainBonus 는 특성(TraitApplier)이 쓰는 것과 같은 스탯이라
    /// 특성으로 얻은 경험치 보너스와 한 줄에 합쳐진다.
    /// </summary>
    public override void CollectPreviewStats(Dictionary<StatType, float> ratios)
        => ratios[StatType.ExpGainBonus] = ratios.GetValueOrDefault(StatType.ExpGainBonus) + ExpBonusRatio;
}
