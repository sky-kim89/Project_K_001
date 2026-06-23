using System;
using UnityEngine;

// ============================================================
//  EventRewardHandler.cs
//  이벤트 보상을 런 데이터에 실제로 적용하는 정적 유틸리티.
//
//  호출 순서:
//    1. CanApply(rewards) — SpendItem 비용 충족 여부 사전 확인
//    2. Apply(rewards, onAbilitySelectNeeded) — 실제 적용
//       ㄴ OpenAbilitySelect 보상이 있으면 콜백으로 횟수 전달
//
//  RandomTraitBuff / RandomTraitDebuff 풀:
//    이미 보유한 특성은 제외하고 추첨. 모두 보유 시 중복 허용.
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
    public static void Apply(EventReward[] rewards, Action<int> onAbilitySelectNeeded = null)
    {
        if (rewards == null) return;

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
                    traits?.AddTrait((TraitType)r.IntValue);
                    break;
                case EventRewardType.RandomTraitBuff:
                    ApplyRandomTrait(traits, BuffTraitPool);
                    break;
                case EventRewardType.RandomTraitDebuff:
                    ApplyRandomTrait(traits, DebuffTraitPool);
                    break;

                // ── 아이템 획득 / 소비 ─────────────────────────
                case EventRewardType.AddItem:
                    if (r.Item != eItem.None) items?.Add(r.Item, r.IntValue);
                    break;
                case EventRewardType.SpendItem:
                    if (r.Item != eItem.None) items?.Spend(r.Item, r.IntValue);
                    break;

                // ── 병사 수 ───────────────────────────────────
                case EventRewardType.AddSoldier:
                    eventBonus?.AddSoldiers(r.IntValue);
                    break;
                case EventRewardType.RemoveSoldier:
                    eventBonus?.RemoveSoldiers(r.IntValue);
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
                case EventRewardType.OpenAbilitySelect:
                    abilitySelectCount += Mathf.Max(1, r.IntValue);
                    break;
            }
        }

        UserDataManager.Instance?.RequestSave();

        if (abilitySelectCount > 0)
            onAbilitySelectNeeded?.Invoke(abilitySelectCount);
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    static void ApplyRandomTrait(RunTraitData traits, TraitType[] pool)
    {
        if (traits == null || pool == null || pool.Length == 0) return;

        var available = System.Array.FindAll(pool, t => !traits.HasTrait(t));
        if (available.Length == 0) available = pool;
        traits.AddTrait(available[UnityEngine.Random.Range(0, available.Length)]);
    }

    static void TryHealCurrentGeneral(float percent)
    {
        // 인게임(전투 중) 상태에서만 유효.
        // TODO: InGameManager.Instance?.HealActiveGeneral(percent)
    }
}
