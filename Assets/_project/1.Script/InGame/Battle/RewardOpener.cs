// ============================================================
//  RewardOpener.cs
//  보상 처리 중앙 분기자.
//
//  eItem 타입에 따라 처리를 분기한다.
//  새 콘텐츠(영웅 박스, 유물 박스 등) 추가 시 이 파일의 switch 만 수정한다.
//
//  사용법:
//    // 직접 아이템 (골드 등)
//    OpenedReward result = RewardOpener.Commit(reward, stageLevel);
//
//    // 박스 아이템 (장비 박스 등) — 동일하게 호출
//    OpenedReward result = RewardOpener.Commit(reward, stageLevel);
//    if (result.Equipment != null) Debug.Log(result.Equipment.EquipmentName);
// ============================================================

public static class RewardOpener
{
    /// <summary>
    /// 보상 하나를 처리하고 결과를 반환한다.
    /// - 직접 아이템: 즉시 ItemData 에 추가
    /// - 박스 아이템: 랜덤 개봉 후 결과를 인벤토리에 추가
    /// </summary>
    public static OpenedReward Commit(ItemAmount reward, int stageLevel)
    {
        switch (reward.Item)
        {
            case eItem.EquipBox:
                return OpenEquipBox(reward, stageLevel);

            // 추후 추가: case eItem.HeroBox: return OpenHeroBox(...);
            // 추후 추가: case eItem.RelicBox: return OpenRelicBox(...);

            default:
                return CommitDirect(reward);
        }
    }

    // ── 직접 아이템 ──────────────────────────────────────────

    static OpenedReward CommitDirect(ItemAmount reward)
    {
        var items = UserDataManager.Instance?.Get<ItemData>();
        items?.Add(reward.Item, reward.Amount, reward.SpecificId);
        UserDataManager.Instance?.RequestSave();
        return new OpenedReward { Source = reward };
    }

    // ── 박스 개봉 ────────────────────────────────────────────

    static OpenedReward OpenEquipBox(ItemAmount reward, int stageLevel)
    {
        var db    = EquipmentDatabase.Current;
        var equip = db?.PickRandom(stageLevel > 0 ? stageLevel : 1);

        if (equip != null)
        {
            var inv = UserDataManager.Instance?.Get<EquipInventoryData>();
            inv?.Add(equip.EquipmentId);
            UserDataManager.Instance?.RequestSave();
        }

        return new OpenedReward { Source = reward, Equipment = equip };
    }
}

// ── 개봉 결과 ─────────────────────────────────────────────────

public struct OpenedReward
{
    public ItemAmount    Source;
    public EquipmentData Equipment;  // 장비 박스 개봉 결과. 비박스 보상이면 null.
}
