using System.Text;
using UnityEngine;

// ============================================================
//  RewardView.cs
//  "획득한 무언가" 하나를 UI 가 그릴 수 있는 형태로 서술하는 값 타입.
//
//  전투 결과(BattleResultPopup)와 이벤트 결과(EventPopup)가 같은
//  RewardCardUI 로 보상을 보여주기 위한 공통 입력이다.
//
//  타입마다 출처가 다르지만(재화 = eItem, 장비 = SO, 특성 = enum …)
//  카드가 필요로 하는 것은 늘 같다 — 아이콘·색·이름·설명·스탯·수량.
//  RewardInfoResolver 가 그 변환을 한 곳에서 담당한다.
// ============================================================

public enum RewardKind
{
    Item      = 0,   // 재화·재료 (수량 있음)
    Box       = 1,   // 미개봉 랜덤 박스
    Equipment = 2,   // 장비
    Trait     = 3,   // 특성
    Ability   = 4,   // 어빌리티
    Soldier   = 5,   // 병사 수 증감
}

public readonly struct RewardView
{
    public readonly RewardKind Kind;
    public readonly eItem      Item;
    public readonly int        Amount;        // 0 = 수량 개념 없음 → 카운터 숨김
    public readonly TraitType  Trait;
    public readonly AbilityId  Ability;
    public readonly string     EquipmentId;

    RewardView(RewardKind kind, eItem item, int amount,
               TraitType trait, AbilityId ability, string equipmentId)
    {
        Kind        = kind;
        Item        = item;
        Amount      = amount;
        Trait       = trait;
        Ability     = ability;
        EquipmentId = equipmentId;
    }

    public static RewardView OfItem(eItem item, int amount)
        => new(RewardKind.Item, item, amount, TraitType.None, default, null);

    public static RewardView OfBox(eItem item, int amount)
        => new(RewardKind.Box, item, amount, TraitType.None, default, null);

    public static RewardView OfEquipment(string equipmentId)
        => new(RewardKind.Equipment, eItem.Equipment, 0, TraitType.None, default, equipmentId);

    public static RewardView OfTrait(TraitType trait)
        => new(RewardKind.Trait, eItem.None, 0, trait, default, null);

    public static RewardView OfAbility(AbilityId ability)
        => new(RewardKind.Ability, eItem.None, 0, TraitType.None, ability, null);

    /// <param name="delta">양수 = 증가, 음수 = 감소.</param>
    public static RewardView OfSoldier(int delta)
        => new(RewardKind.Soldier, eItem.None, delta, TraitType.None, default, null);

    /// <summary>
    /// ItemAmount 하나를 알맞은 카드 종류로 변환한다.
    /// Special(장비·특성·어빌리티)은 SpecificId 에 대상 식별자가 들어 있다.
    /// </summary>
    public static RewardView From(ItemAmount amount)
    {
        switch (amount.Item)
        {
            case eItem.Equipment:
                return OfEquipment(amount.SpecificId);

            case eItem.Trait:
                return System.Enum.TryParse(amount.SpecificId, out TraitType trait)
                    ? OfTrait(trait)
                    : OfItem(amount.Item, amount.Amount);

            case eItem.Ability:
                return System.Enum.TryParse(amount.SpecificId, out AbilityId ability)
                    ? OfAbility(ability)
                    : OfItem(amount.Item, amount.Amount);

            default:
                return amount.Item.IsBoxType()
                    ? OfBox(amount.Item, amount.Amount)
                    : OfItem(amount.Item, amount.Amount);
        }
    }
}

// ── 카드가 그릴 내용 ──────────────────────────────────────────

public struct RewardInfo
{
    public Sprite Icon;
    public Color  Accent;        // 카드 배경·테두리 색
    public string Name;
    public string Description;
    public string StatText;      // 빈 문자열이면 툴팁에서 숨김
    public string AmountLabel;   // 빈 문자열이면 카운터 배지 비활성
}

// ============================================================
//  RewardInfoResolver
//  RewardView → RewardInfo. 타입별 분기는 전부 여기 모은다.
// ============================================================

public static class RewardInfoResolver
{
    static readonly Color BoxColor     = new(0.25f, 0.25f, 0.35f, 1f);
    static readonly Color TraitColor   = new(0.42f, 0.24f, 0.52f, 1f);
    static readonly Color AbilityColor = new(0.20f, 0.34f, 0.58f, 1f);
    static readonly Color SoldierColor = new(0.22f, 0.42f, 0.30f, 1f);
    static readonly Color MissingColor = new(0.30f, 0.30f, 0.34f, 1f);

    public static RewardInfo Resolve(RewardView view) => view.Kind switch
    {
        RewardKind.Item      => ResolveItem(view),
        RewardKind.Box       => ResolveBox(view),
        RewardKind.Equipment => ResolveEquipment(view),
        RewardKind.Trait     => ResolveTrait(view),
        RewardKind.Ability   => ResolveAbility(view),
        RewardKind.Soldier   => ResolveSoldier(view),
        _                    => new RewardInfo { Accent = MissingColor, Name = "—" },
    };

    // ── 재화·재료 ────────────────────────────────────────────

    static RewardInfo ResolveItem(RewardView v) => new()
    {
        Icon        = SpriteManager.Instance?.Get(v.Item.IconKey()),
        Accent      = ItemColor(v.Item),
        Name        = v.Item.DisplayName(),
        Description = v.Item.UsageDesc(),
        StatText    = "",
        AmountLabel = v.Amount != 0 ? $"×{v.Amount:N0}" : "",
    };

    static Color ItemColor(eItem item) => item switch
    {
        eItem.Gold         => new Color(1.00f, 0.80f, 0.20f, 1f),
        eItem.Gem          => new Color(0.40f, 0.80f, 1.00f, 1f),
        eItem.BattleStone  => new Color(0.50f, 0.75f, 0.60f, 1f),
        eItem.Energy       => new Color(0.90f, 0.50f, 0.20f, 1f),
        eItem.SoldierShard => new Color(0.45f, 0.70f, 1.00f, 1f),
        // 강화석 2종 — 아이콘 색과 맞춘다 (장군=금, 장비=청)
        eItem.GeneralUpgradeStone => new Color(1.00f, 0.84f, 0.25f, 1f),
        eItem.EquipUpgradeStone   => new Color(0.35f, 0.55f, 0.85f, 1f),
        _                  => new Color(0.60f, 0.60f, 0.70f, 1f),
    };

    static RewardInfo ResolveBox(RewardView v) => new()
    {
        Icon        = SpriteManager.Instance?.Get(v.Item.IconKey()),
        Accent      = BoxColor,
        Name        = v.Item.DisplayName(),
        Description = v.Item.UsageDesc(),
        StatText    = "",
        AmountLabel = v.Amount > 1 ? $"×{v.Amount:N0}" : "",
    };

    // ── 장비 ─────────────────────────────────────────────────

    static RewardInfo ResolveEquipment(RewardView v)
    {
        var data = EquipmentDatabase.Current?.Get(v.EquipmentId);
        if (data == null)
            return new RewardInfo { Accent = MissingColor, Name = "알 수 없는 장비" };

        return new RewardInfo
        {
            Icon        = data.Icon,
            Accent      = GradeStyle.GetColor(data.Grade),
            Name        = $"{data.EquipmentName}  <color=#{ColorUtility.ToHtmlStringRGB(GradeStyle.GetColor(data.Grade))}>{GradeStyle.GetLabel(data.Grade)}</color>",
            Description = data.Description,
            StatText    = BuildEquipStats(data),
            AmountLabel = "",   // 장비는 개별 아이템 — 수량 없음
        };
    }

    //  기본 스탯 + 추가 옵션(트리거)까지 한 덩어리로 만든다.
    //  추가 옵션은 "치명타 15% 확률: 공격력 +20%" 처럼 길어서
    //  카드 위에는 절대 들어가지 않는다 — 툴팁에서만 보여준다.
    static string BuildEquipStats(EquipmentData data)
    {
        var loc = LocalizationManager.Instance;
        var sb  = new StringBuilder();

        if (data.StatEntries != null)
            foreach (var e in data.StatEntries)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(loc.Get(e.Stat.ToString()))
                  .Append(" +")
                  .Append(StatDisplayHelper.FormatStat(e.Stat, data.GetStatValue(e, 0)));
            }

        if (data.TriggerType != EquipmentTrigger.None)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(EquipmentData.FormatTriggerLine(data,
                loc.Get(data.TriggerType.ToString()),
                loc.Get(data.TriggerStat.ToString())));
        }

        return sb.ToString();
    }

    // ── 특성 ─────────────────────────────────────────────────
    //  특성 아이콘(TraitIconUI)을 눌렀을 때와 같은 내용을 보여준다.

    static RewardInfo ResolveTrait(RewardView v)
    {
        var data = TraitDatabase.Current?.Get(v.Trait);
        if (data == null)
            return new RewardInfo { Accent = MissingColor, Name = "알 수 없는 특성" };

        return new RewardInfo
        {
            Icon        = data.Icon,
            Accent      = TraitColor,
            Name        = data.TraitName,
            Description = data.Description,
            StatText    = AbilityUIHelper.BuildStatText(data.Effects),
            AmountLabel = "",
        };
    }

    // ── 어빌리티 ─────────────────────────────────────────────

    static RewardInfo ResolveAbility(RewardView v)
    {
        var data = AbilityDatabase.Current?.Get(v.Ability);
        if (data == null)
            return new RewardInfo { Accent = MissingColor, Name = "알 수 없는 어빌리티" };

        return new RewardInfo
        {
            Icon        = data.Icon,
            Accent      = AbilityColor,
            Name        = data.AbilityName,
            Description = data.Description,
            StatText    = BuildAbilityStats(data),
            AmountLabel = "",
        };
    }

    static string BuildAbilityStats(AbilityData data)
    {
        var loc = LocalizationManager.Instance;
        var sb  = new StringBuilder();
        sb.Append(loc.Get(data.Stat1.ToString())).Append(' ')
          .Append(AbilityUIHelper.FormatStatValue(data.Stat1, data.Value1));
        if (data.HasStat2)
            sb.Append('\n').Append(loc.Get(data.Stat2.ToString())).Append(' ')
              .Append(AbilityUIHelper.FormatStatValue(data.Stat2, data.Value2));
        return sb.ToString();
    }

    // ── 병사 수 ──────────────────────────────────────────────

    static RewardInfo ResolveSoldier(RewardView v) => new()
    {
        Icon        = SpriteManager.Instance?.Get(eItem.SoldierShard.IconKey()),
        Accent      = SoldierColor,
        Name        = v.Amount >= 0 ? "병사 합류" : "병사 이탈",
        Description = v.Amount >= 0
            ? "이번 런 동안 모든 장수의 병사 수가 늘어난다."
            : "이번 런 동안 모든 장수의 병사 수가 줄어든다.",
        StatText    = $"{LocalizationManager.Instance.Get(StatType.SoldierCount.ToString())} {(v.Amount >= 0 ? "+" : "")}{v.Amount}",
        AmountLabel = $"{(v.Amount >= 0 ? "+" : "")}{v.Amount}",
    };
}
