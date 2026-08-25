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
    /// <summary>
    /// 이벤트로 여는 장비 박스의 최저 등급.
    ///
    /// ⚠ 이벤트는 기대를 세워 놓고 카드를 뒤집는다
    ///   "귀한 것을 내주겠다" 는 문장을 읽고 고른 선택지에서 일반 등급이 나오면
    ///   보상이 아니라 실망이 된다. 일상 드랍(스테이지 클리어)과 달리 이쪽은
    ///   하한을 올려 둔다. 초반 스테이지라 상위 등급 풀이 비어 있으면
    ///   EquipmentDatabase.GetDropPool 이 하한 등급만은 열어 준다.
    /// </summary>
    public const UnitGrade EventEquipMinGrade = UnitGrade.Uncommon;

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

    // ── 이벤트로 얻을 수 있는 특성 목록 ───────────────────────

    /// <summary>
    /// 이벤트를 통해 얻을 수 있는 특성 전부.
    /// EventDatabase 의 모든 AddTrait 보상 + 랜덤 버프/디버프 풀을 훑는다.
    ///
    /// 상점(RunShopPopup)이 이 목록을 빼고 상품을 뽑는다 —
    /// "처형의 사기" 처럼 특정 이벤트에서 특정 선택지를 골라야 얻는 특성이
    /// 골드로도 사지면 그 선택의 의미가 사라진다.
    ///
    /// enum 번호대(500~)로 거르지 않는 이유: 새 이벤트가 기존 특성을 주도록
    /// 바뀌어도 여기 손댈 필요 없이 상점에서 자동으로 빠진다.
    /// </summary>
    public static HashSet<TraitType> CollectEventTraits()
    {
        var set = new HashSet<TraitType>(BuffTraitPool);
        set.UnionWith(DebuffTraitPool);

        var db = EventDatabase.Current;
        if (db == null) return set;

        foreach (var evt in db.GetAll())
        {
            CollectTraitRewards(evt.InstantRewards, set);
            if (evt.Choices == null) continue;
            foreach (var choice in evt.Choices)
            {
                CollectTraitRewards(choice.SuccessRewards, set);
                CollectTraitRewards(choice.FailRewards, set);
            }
        }
        return set;
    }

    static void CollectTraitRewards(EventReward[] rewards, HashSet<TraitType> set)
    {
        if (rewards == null) return;
        foreach (var r in rewards)
            if (r.Type == EventRewardType.AddTrait)
                set.Add((TraitType)r.IntValue);
    }

    // ── 비용 사전 확인 ────────────────────────────────────────

    /// <summary>SpendItem 항목이 모두 충족 가능한지 검사.</summary>
    public static bool CanApply(EventReward[] rewards)
    {
        if (rewards == null) return true;
        var items = UserDataManager.Instance?.Get<ItemData>();
        if (items == null) return true;

        foreach (var r in rewards)
            if (r.Type == EventRewardType.SpendItem && r.Item != eItem.None)
                if (!items.CanSpend(r.Item, ResolveAmount(r))) return false;

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
                {
                    if (r.Item == eItem.None) break;

                    // ⚠ 상자는 받는 즉시 깐다
                    //   이 게임에는 인벤토리 화면이 없다. 상자를 그대로 주면 플레이어가
                    //   나중에 열 방법이 없고, 보상 카드도 "장비 박스 ×1" 이라 뭘 얻었는지
                    //   알 수 없다. RewardOpener 가 개봉까지 처리하므로 그 결과를 보여 준다.
                    if (r.Item.IsBoxType())
                    {
                        int count = Mathf.Max(1, ResolveAmount(r));
                        for (int i = 0; i < count; i++)
                        {
                            var opened = RewardOpener.Commit(
                                new ItemAmount { Item = r.Item, Amount = 1 }, CurrentStageLevel(),
                                EventEquipMinGrade);
                            granted.Add(RewardOpener.ToView(opened));
                        }
                        break;
                    }

                    int amount = ResolveAmount(r);
                    items?.Add(r.Item, amount);
                    granted.Add(RewardView.OfItem(r.Item, amount));
                    break;
                }
                case EventRewardType.SpendItem:
                    // 소비는 "얻은 것" 이 아니므로 카드로 보여주지 않는다.
                    if (r.Item != eItem.None) items?.Spend(r.Item, ResolveAmount(r));
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

                // ── 용병 고용 열기 ────────────────────────────
                // 상점과 같다. 고용 여부는 팝업에서 정해지므로 여기서 줄 것이 없다.
                // 대가를 받으려면 이 보상 앞에 SpendItem 을 같이 걸면 된다
                // (EventPopup.HasMercenaryReward 참고).
                case EventRewardType.OpenMercenary:
                    break;
            }
        }

        UserDataManager.Instance?.RequestSave();

        if (abilitySelectCount > 0)
            onAbilitySelectNeeded?.Invoke(abilitySelectCount);

        return granted;
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    /// <summary>
    /// 보상 수량을 확정한다.
    /// ScaleByStageReward 면 IntValue 를 "그 스테이지 클리어 보상 대비 %" 로 해석한다.
    ///
    /// ⚠ 스테이지 번호를 곱하면 안 된다
    ///   클리어 보상은 500(1스테이지) → 1,950(30스테이지) 으로 3.9배만 는다.
    ///   번호를 곱하면 30배가 되어 후반 비용이 수입을 짓눌러 버린다.
    ///   수입 곡선에 직접 묶어야 언제 만나도 체감이 같다.
    ///
    /// ⚠ 지급·차감·표시가 전부 이 함수를 거쳐야 한다
    ///   한 곳이라도 IntValue 를 직접 쓰면 표시와 실제가 어긋난다.
    /// </summary>
    public static int ResolveAmount(EventReward r)
    {
        if (!r.ScaleByStageReward) return r.IntValue;
        return Mathf.Max(1, Mathf.RoundToInt(StageGoldReward() * r.IntValue / 100f));
    }

    /// <summary>현재 스테이지의 클리어 보상 골드 — 이벤트 금액의 기준선.</summary>
    static float StageGoldReward()
    {
        var cfg = StageConfig.Current;
        int stage = CurrentStageLevel();

        float baseGold = cfg != null ? cfg.GoldRewardBase     : 500f;
        float perStage = cfg != null ? cfg.GoldRewardPerStage : 50f;
        return baseGold + Mathf.Max(0, stage - 1) * perStage;
    }

    /// <summary>상자 개봉 등급·스테이지 비례 보상의 기준이 되는 현재 스테이지 (1부터).</summary>
    public static int CurrentStageLevel()
    {
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        return progress != null ? progress.CurrentRunStage + 1 : 1;
    }

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
        // 이벤트 팝업은 로비(StageSelectUI)에서만 열린다 — 회복시킬 전투 중 장수가 없다.
        // 지금 조용히 넘기면 "체력 회복" 보상이 카드도 없이 사라지므로 로그는 남긴다.
        // 쓰려면 인게임 이벤트 진입점부터 만들어야 한다.
        Debug.LogWarning($"[EventRewardHandler] HealHpPercent({percent:P0}) — 지급 경로가 없다(이벤트는 로비 전용).");
    }
}
