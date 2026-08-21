using UnityEngine;

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
//
//    // 특성·어빌리티 — SpecificId 에 enum 이름을 넣는다
//    RewardOpener.Commit(new ItemAmount {
//        Item = eItem.Trait, Amount = 1, SpecificId = nameof(TraitType.Event_BattleWill) }, 0);
//
//  ※ Special(900~) 아이템은 ItemData 가 수량을 저장하지 않는다.
//    여기서 각 데이터 섹션에 직접 넣어야 실제로 지급된다.
// ============================================================

public static class RewardOpener
{
    /// <summary>
    /// 보상 하나를 처리하고 결과를 반환한다.
    /// - 직접 아이템: 즉시 ItemData 에 추가
    /// - 박스 아이템: 랜덤 개봉 후 결과를 인벤토리에 추가
    /// - 특성·어빌리티: 런 데이터에 추가
    /// </summary>
    /// <param name="minEquipGrade">
    /// 장비 박스에서 나올 수 있는 최저 등급.
    ///
    /// ⚠ 기대를 세워 놓고 최하위를 주면 보상이 아니라 실망이 된다
    ///   이벤트는 "귀한 것을 준다" 는 연출을 깔고 카드를 뒤집는다. 거기서 일반 등급이
    ///   나오면 안 준 것만 못하다. 그런 자리는 하한을 올려서 부른다
    ///   (EventRewardHandler.EventEquipMinGrade).
    /// </param>
    public static OpenedReward Commit(ItemAmount reward, int stageLevel,
                                      UnitGrade minEquipGrade = UnitGrade.Normal)
    {
        switch (reward.Item)
        {
            case eItem.EquipBox:
                return OpenEquipBox(reward, stageLevel, minEquipGrade);

            case eItem.Equipment:
                return GrantEquipment(reward);

            case eItem.Trait:
                return GrantTrait(reward);

            case eItem.Ability:
                return GrantAbility(reward);

            // 추후 추가: case eItem.HeroBox: return OpenHeroBox(...);
            // 추후 추가: case eItem.RelicBox: return OpenRelicBox(...);

            default:
                return CommitDirect(reward);
        }
    }

    /// <summary>UI 가 보상 카드로 그릴 수 있는 서술자로 변환한다.</summary>
    public static RewardView ToView(OpenedReward result)
    {
        if (result.Equipment != null)  return RewardView.OfEquipment(result.Equipment.EquipmentId);
        if (result.Trait != TraitType.None) return RewardView.OfTrait(result.Trait);
        if (result.HasAbility)         return RewardView.OfAbility(result.Ability);
        return RewardView.OfItem(result.Source.Item, result.Source.Amount);
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

    static OpenedReward OpenEquipBox(ItemAmount reward, int stageLevel,
                                     UnitGrade minGrade = UnitGrade.Normal)
    {
        var db    = EquipmentDatabase.Current;
        var equip = db?.PickRandom(stageLevel > 0 ? stageLevel : 1, minGrade);

        if (equip != null)
        {
            var inv = UserDataManager.Instance?.Get<EquipInventoryData>();
            inv?.Add(equip.EquipmentId);
            UserDataManager.Instance?.RequestSave();
        }

        return new OpenedReward { Source = reward, Equipment = equip };
    }

    // ── Special 지급 ─────────────────────────────────────────

    static OpenedReward GrantEquipment(ItemAmount reward)
    {
        var equip = EquipmentDatabase.Current?.Get(reward.SpecificId);
        if (equip == null)
        {
            Debug.LogWarning($"[RewardOpener] EquipmentId '{reward.SpecificId}' 를 찾을 수 없습니다.");
            return new OpenedReward { Source = reward };
        }

        UserDataManager.Instance?.Get<EquipInventoryData>()?.Add(equip.EquipmentId);
        UserDataManager.Instance?.RequestSave();
        return new OpenedReward { Source = reward, Equipment = equip };
    }

    static OpenedReward GrantTrait(ItemAmount reward)
    {
        if (!System.Enum.TryParse(reward.SpecificId, out TraitType trait) || trait == TraitType.None)
        {
            Debug.LogWarning($"[RewardOpener] TraitType '{reward.SpecificId}' 를 해석할 수 없습니다.");
            return new OpenedReward { Source = reward };
        }

        UserDataManager.Instance?.Get<RunTraitData>()?.AddTrait(trait);
        UserDataManager.Instance?.RequestSave();
        return new OpenedReward { Source = reward, Trait = trait };
    }

    static OpenedReward GrantAbility(ItemAmount reward)
    {
        if (!System.Enum.TryParse(reward.SpecificId, out AbilityId ability))
        {
            Debug.LogWarning($"[RewardOpener] AbilityId '{reward.SpecificId}' 를 해석할 수 없습니다.");
            return new OpenedReward { Source = reward };
        }

        UserDataManager.Instance?.Get<RunAbilityData>()?.AddAbility(ability);
        UserDataManager.Instance?.RequestSave();
        return new OpenedReward { Source = reward, Ability = ability, HasAbility = true };
    }
}

// ── 개봉 결과 ─────────────────────────────────────────────────

public struct OpenedReward
{
    public ItemAmount    Source;
    public EquipmentData Equipment;   // 장비 박스 개봉 / 장비 지급 결과. 없으면 null.
    public TraitType     Trait;       // 특성 지급 결과. 없으면 None.
    public AbilityId     Ability;     // 어빌리티 지급 결과. HasAbility 로 유효 여부 판단.
    public bool          HasAbility;  // AbilityId 는 0 이 유효값일 수 있어 별도 플래그를 둔다.
}
