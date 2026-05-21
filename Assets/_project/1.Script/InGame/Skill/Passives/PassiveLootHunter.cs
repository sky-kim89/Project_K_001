using BattleGame.Units;

// ============================================================
//  PassiveLootHunter.cs  [OnEnemyKill]
//  전리품 사냥 — 적 처치 시 골드 N 획득.
//
//  Inspector:
//    TriggerType = OnEnemyKill
//    GoldPerKill : 처치당 획득 골드 (기본 50)
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Passive_LootHunter", menuName = "BattleGame/Passives/LootHunter")]
public class PassiveLootHunter : PassiveSkillData
{
    [UnityEngine.Header("전리품 사냥 설정")]
    [UnityEngine.Tooltip("적 1처치당 획득 골드")]
    public int GoldPerKill = 50;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (GoldPerKill <= 0) return;
        var itemData = UserDataManager.Instance?.Get<ItemData>();
        itemData?.Add(eItem.Gold, GoldPerKill);
    }
}
