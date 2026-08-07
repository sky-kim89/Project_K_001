using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  EventRewardHandler.cs
//  이벤트 보상을 런 데이터에 실제로 적용하는 정적 유틸리티.
//
//  호출 순서:
//    1. CanApply(rewards) — SpendItem 비용 충족 여부 사전 확인
//    2. Apply(rewards, onAbilitySelectNeeded) — 실제 적용
//       ㄴ OpenAbilitySelect 보상이 있으면 콜백으로 횟수 전달
//       ㄴ 반환값 = 실제로 지급된 목록 (EventPopup 이 보상 카드로 표시)
//
//  RandomTraitBuff / RandomTraitDebuff 풀:
//    이미 보유한 특성은 제외하고 추첨. 모두 보유 시 중복 허용.
//    무엇이 뽑혔는지는 반환 목록으로만 알 수 있다.
// ============================================================

public static class EventRewardHandler
{
    // ── 랜덤 버프/디버프 특성 풀 ─────────────────────────────

    static readonly TraitType[] BuffTraitPool =
    {
        TraitType.Event_BattleWill,
        TraitType.Event_PotionBuff,
        TraitType.Event_ExecutionMorale,
        TraitType.Event_VeteranHeritage,
    };

    static readonly TraitType[] DebuffTraitPool =
    {
        TraitType.Event_PotionDebuff,
        TraitType.Event_BloodPact,
        TraitType.Event_AltarCurse,
    };

    // ── 비용 사전 확인 ────────────────────────────────────────

    /// <summary>SpendItem 항목이 모두 충족 가능한지 검사.</summary>
    public static bool CanApply(EventReward[] rewards)
    {
        if (rewards == null) return true;
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null) return true;

        foreach (var r in rewards)
            if (r.Type == EventRewardType.SpendItem && r.Item != eItem.None)
                if (!items.CanSpend(r.Item, r.IntValue)) return false;

        return true;
    }

    // ── 보상 적용 ─────────────────────────────────────────────

    /// <param name="rewards">적용할 보상 배열.</param>
    /// <param name="onAbilitySelectNeeded">OpenAbilitySelect 보상이 있을 때 횟수를 전달하는 콜백.</param>
    /// <returns>실제로 지급된 항목 목록 (소비·스테이지 변경 등 표시할 게 없는 항목은 제외).</returns>
    public static List<RewardView> Apply(EventReward[] rewards, Action<int> onAbilitySelectNeeded = null)
    {
        var granted = new List<RewardView>();
        if (rewards == null) return granted;

        var items      = UserDataManager.Instance?.Get<ItemData>();
        var traits     = UserDataManager.Instance?.Get<RunTraitData>();
        var stages     = UserDataManager.Instance?.Get<StageProgressData>();
        var eventBonus = UserDataManager.Instance?.Get<RunEventBonusData>();

        int abilitySelectCount = 0;

        foreach (var r in rewards)
        {
            switch (r.Type)
            {
                // ── 특성 ──────────────────────────────────────
                case EventRewardType.AddTrait:
                {
                    var t = (TraitType)r.IntValue;
                    traits?.AddTrait(t);
                    granted.Add(RewardView.OfTrait(t));
                    break;
                }
                case EventRewardType.RandomTraitBuff:
                    AddRandomTrait(traits, BuffTraitPool, granted);
                    break;
                case EventRewardType.RandomTraitDebuff:
                    AddRandomTrait(traits, DebuffTraitPool, granted);
                    break;

                // ── 아이템 획득 / 소비 ─────────────────────────
                case EventRewardType.AddItem:
                    if (r.Item != eItem.None)
                    {
                        items?.Add(r.Item, r.IntValue);
                        granted.Add(RewardView.OfItem(r.Item, r.IntValue));
                    }
                    break;
                case EventRewardType.SpendItem:
                    // 소비는 "얻은 것" 이 아니므로 카드로 보여주지 않는다.
                    if (r.Item != eItem.None) items?.Spend(r.Item, r.IntValue);
                    break;

                // ── 병사 수 ───────────────────────────────────
                case EventRewardType.AddSoldier:
                    eventBonus?.AddSoldiers(r.IntValue);
                    granted.Add(RewardView.OfSoldier(r.IntValue));
                    break;
                case EventRewardType.RemoveSoldier:
                    eventBonus?.RemoveSoldiers(r.IntValue);
                    granted.Add(RewardView.OfSoldier(-r.IntValue));
                    break;

                // ── 체력 회복 (인게임 전용 — 로비에서는 무시) ──
                case EventRewardType.HealHpPercent:
                    TryHealCurrentGeneral(r.FloatValue);
                    break;

                // ── 스테이지 변경 ─────────────────────────────
                case EventRewardType.NextStageElite:
                    stages?.ForceNextStageElite();
                    break;

                // ── 어빌리티 선택 (횟수 누산 후 콜백) ─────────
                // 실제 획득은 AbilitySelectPopup 에서 결정되므로 여기서는 목록에 넣지 않는다.
                case EventRewardType.OpenAbilitySelect:
                    abilitySelectCount += Mathf.Max(1, r.IntValue);
                    break;

                // ── 런 상점 열기 ──────────────────────────────
                // 데이터에 적용할 것이 없다. EventPopup 이 결과 표시 후
                // RunShopPopup 을 이어 여는 것으로 처리한다
                // (EventPopup.HasRunShopReward 참고).
                case EventRewardType.OpenRunShop:
                    break;
            }
        }

        UserDataManager.Instance?.RequestSave();

        if (abilitySelectCount > 0)
            onAbilitySelectNeeded?.Invoke(abilitySelectCount);

        return granted;
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    static void AddRandomTrait(RunTraitData traits, TraitType[] pool, List<RewardView> granted)
    {
        if (traits == null || pool == null || pool.Length == 0) return;

        var available = System.Array.FindAll(pool, t => !traits.HasTrait(t));
        if (available.Length == 0) available = pool;

        var picked = available[UnityEngine.Random.Range(0, available.Length)];
        traits.AddTrait(picked);
        granted.Add(RewardView.OfTrait(picked));
    }

    static void TryHealCurrentGeneral(float percent)
    {
        // 인게임(전투 중) 상태에서만 유효.
        // TODO: InGameManager.Instance?.HealActiveGeneral(percent)
    }
}
