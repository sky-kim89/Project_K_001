using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  EquipmentData.cs
//  장비 아이템 ScriptableObject
//
//  등급: UnitGrade 공통 사용 (Normal ~ Epic)
//    Normal / Uncommon / Rare — 스탯 절대값만
//    Unique / Epic             — 절대값 + 공격·피격 시 조건부 효과
//
//  수치 공식:
//    finalDelta = baseDelta × (1 + (itemLevel - 1 + enhanceLevel) × ValuePerLevel)
//    아이템 레벨 1 증가 = 강화 1회와 동일한 효과
//
//  스탯은 절대값 (유물·어빌리티의 %와 달리 고정 수치)
//  회귀 시 초기화 (UnitEntry.RunEquipSlots 에 저장)
// ============================================================

[CreateAssetMenu(fileName = "Equipment_", menuName = "BattleGame/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    // ── 식별 ──────────────────────────────────────────────────
    public string    EquipmentId;
    public string    EquipmentName;
    [TextArea(2, 4)]
    public string    Description;
    public UnityEngine.Sprite Icon;

    // ── 등급 (UnitGrade 공통) ─────────────────────────────────
    public UnitGrade Grade = UnitGrade.Normal;

    // ── 스탯 (절대값) ─────────────────────────────────────────
    public List<EquipStatEntry> StatEntries = new();

    // ── 조건부 효과 (Epic 등급용) ─────────────────────────────
    public EquipmentTrigger     TriggerType        = EquipmentTrigger.None;

    /// <summary>
    /// 발동했을 때 무슨 일이 일어나는가.
    ///   StatBuff — TriggerStat 에 절대값을 더한다 (MaxHp 면 즉시 회복)
    ///   RatioBuff— TriggerStat 에 비율을 곱한다 (0.35 = ×1.35). 병사 버프처럼
    ///              대상마다 기본 수치가 다른 곳에 쓴다
    ///   Summon   — 스켈레톤을 TriggerValue 마리 소환한다
    /// </summary>
    public EquipTriggerEffect   EffectKind         = EquipTriggerEffect.StatBuff;

    /// <summary>버프를 누가 받는가. Summon 에는 의미 없다.</summary>
    public EquipTriggerTarget   TriggerTarget      = EquipTriggerTarget.General;

    public StatType             TriggerStat        = StatType.Attack;
    /// <summary>
    /// 트리거 발동 시 적용할 수치.
    ///   TriggerIsPercent = false : 절대값 Delta (예: +120 체력)
    ///   TriggerIsPercent = true  : TriggerPercentBase 기준 비율 (예: 0.10 = 10%)
    /// </summary>
    public float                TriggerValue       = 0f;
    public bool                 TriggerIsPercent   = false;
    /// <summary>IsPercent = true 일 때 비율 계산 기준.</summary>
    public EquipTriggerPercentBase TriggerPercentBase = EquipTriggerPercentBase.Absolute;
    [Range(0f, 1f)]
    public float                TriggerChance      = 0.3f;
    /// <summary>버프 지속 시간 (초). 0 = 즉시 적용형.</summary>
    public float                TriggerDuration    = 0f;

    // ⚠ 재발동 대기(쿨다운)는 두지 않는다 — 빈도는 확률 하나로만 조절한다
    //   확률과 대기를 같이 걸면 실제 발동 주기가 둘의 곱이 되어, 표시된 확률이
    //   아무 의미가 없어진다. 발동이 잦아 문제면 확률을 낮추거나,
    //   판정 조건 자체를 바꾼다 ("N번째 처치마다" 같은 식).

    // ── 아이템 레벨 ───────────────────────────────────────────
    [Min(1)]
    public int   ItemLevel            = 1;

    // ── 소환 설정 (EffectKind == Summon) ──────────────────────
    [Tooltip("PoolController 에 등록된 소환 유닛 풀 키")]
    public string SummonPoolKey   = "Soldier";
    [Tooltip("소환체 스텟 비율 (장군 스텟 대비)")]
    [Range(0.1f, 1f)]
    public float  SummonStatRatio = 0.4f;

    // ── 강화 설정 ─────────────────────────────────────────────
    public float ValuePerLevel        = 0.1f;
    public int   BaseEnhanceStoneCost = 1;
    public int   BaseGoldCost         = 100;

    // ── 유틸 ──────────────────────────────────────────────────

    public float GetStatValue(EquipStatEntry entry, int enhanceLevel)
        => entry.Delta * (1f + (ItemLevel - 1 + enhanceLevel) * ValuePerLevel);

    public int GetEnhanceStoneCost(int currentLevel) => BaseEnhanceStoneCost * (currentLevel + 1);
    public int GetEnhanceGoldCost(int currentLevel)  => BaseGoldCost         * (currentLevel + 1);

    // "공격 시 30% 확률: 체력 +피해량의 10%"  or  "피격 시 40% 확률: 공격 +250 (2초)"
    // 소환:      "병사 사망 시 30% 확률: 스켈레톤 1기 소환"
    // 병사 버프: "피격 시 35% 확률: [병사] 공격력 +35% (4초)"
    public static string FormatTriggerLine(EquipmentData equip, string triggerLabel, string statLabel)
    {
        string chance = $"{equip.TriggerChance * 100f:F0}%";

        if (equip.EffectKind == EquipTriggerEffect.Summon)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(equip.TriggerValue));
            return $"{triggerLabel} {chance} 확률: 스켈레톤 {count}기 소환";
        }

        // 병사 대상이면 누구에게 걸리는지부터 밝힌다 — 장군 버프와 헷갈리면 고를 이유가 사라진다
        string scope = equip.TriggerTarget == EquipTriggerTarget.Soldiers ? "[병사 전원] " : "";

        if (equip.EffectKind == EquipTriggerEffect.RatioBuff)
        {
            string dur = equip.TriggerDuration > 0f ? $" ({equip.TriggerDuration:F0}초)" : "";
            return $"{triggerLabel} {chance} 확률: {scope}{statLabel} +{equip.TriggerValue * 100f:F0}%{dur}";
        }

        string value;
        if (equip.TriggerIsPercent)
        {
            string baseLabel = equip.TriggerPercentBase switch
            {
                EquipTriggerPercentBase.OfDamage => "피해량",
                EquipTriggerPercentBase.OfMaxHp  => "최대 체력",
                _                                => "",
            };
            value = $"+{baseLabel}의 {equip.TriggerValue * 100f:F0}%";
        }
        else
        {
            value = $"+{StatDisplayHelper.FormatStat(equip.TriggerStat, equip.TriggerValue)}";
        }
        string duration = equip.TriggerDuration > 0f ? $" ({equip.TriggerDuration:F0}초)" : "";
        return $"{triggerLabel} {chance} 확률: {scope}{statLabel} {value}{duration}";
    }
}

// ── 트리거 종류 ───────────────────────────────────────────────

public enum EquipmentTrigger
{
    None           = 0,
    OnAttack       = 1,  // 장군 공격 시 발동
    OnHit          = 2,  // 장군 피격 시 발동
    OnEnemyKill    = 3,  // 적 처치 시 발동
    OnSoldierDeath = 4,  // 병사 사망 시 발동
    OnSkillUse     = 5,  // 스킬 사용 시 발동
}

// ── 발동 효과 종류 ───────────────────────────────────────────
/// <summary>
/// 트리거가 발동했을 때 실제로 하는 일.
///
/// ⚠ 비율 버프를 Add 로 흉내내지 말 것
///   병사처럼 대상마다 기본 수치가 다른 곳에 절대값을 뿌리면, 약한 병사에겐
///   두 배가 되고 센 병사에겐 티도 안 난다. RatioBuff 는 EffectMode.Multiply 로
///   들어가고, StatComponent.Final 이 매 프레임 Base 에서 다시 계산되므로
///   같은 버프가 여러 번 겹쳐도 값이 누적 폭주하지 않는다.
/// </summary>
public enum EquipTriggerEffect
{
    StatBuff  = 0,  // TriggerStat 에 절대값 가산 (MaxHp 면 즉시 회복)
    RatioBuff = 1,  // TriggerStat 에 비율 곱연산 (0.35 = ×1.35)
    Summon    = 2,  // 스켈레톤 TriggerValue 마리 소환
}

// ── 버프 대상 ────────────────────────────────────────────────
/// <summary>
/// 발동 효과를 누가 받는가.
///
/// ⚠ 병사는 트리거를 스스로 굴리지 않는다
///   판정은 언제나 장군이 하고(GeneralTriggerSetComponent 가 장군에게만 있다),
///   Soldiers 는 "그 장군 휘하 병사에게 결과를 뿌린다" 는 뜻이다.
/// </summary>
public enum EquipTriggerTarget
{
    General  = 0,
    Soldiers = 1,
}

// ── 퍼센트 기준 ──────────────────────────────────────────────
/// <summary>
/// TriggerIsPercent = true 일 때 비율 계산의 기준이 되는 값.
///   Absolute   — 퍼센트 미사용 (절대값)
///   OfDamage   — 이번 공격으로 준 피해량 기준   (OnAttack 전용)
///   OfMaxHp    — 장군 최대 체력(MaxHp) 기준      (OnHit 전용)
/// </summary>
public enum EquipTriggerPercentBase
{
    Absolute = 0,
    OfDamage = 1,
    OfMaxHp  = 2,
}

// ── 스탯 항목 ─────────────────────────────────────────────────

[Serializable]
public struct EquipStatEntry
{
    public StatType Stat;
    public float    Delta;
}
