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

    // ── 등급 (UnitGrade 공통) ─────────────────────────────────
    public UnitGrade Grade = UnitGrade.Normal;

    // ── 스탯 (절대값) ─────────────────────────────────────────
    public List<EquipStatEntry> StatEntries = new();

    // ── 조건부 효과 (Unique 이상 등급용) ─────────────────────
    public EquipmentTrigger TriggerType     = EquipmentTrigger.None;
    public StatType         TriggerStat     = StatType.Attack;
    /// <summary>트리거 발동 시 StatusEffectBuffer 에 추가할 절대값 Delta</summary>
    public float            TriggerValue    = 0f;
    [Range(0f, 1f)]
    public float            TriggerChance   = 0.3f;
    /// <summary>버프 지속 시간 (초). 0 = 즉시 적용형.</summary>
    public float            TriggerDuration = 0f;

    // ── 아이템 레벨 ───────────────────────────────────────────
    [Min(1)]
    public int   ItemLevel            = 1;

    // ── 강화 설정 ─────────────────────────────────────────────
    public float ValuePerLevel        = 0.1f;
    public int   BaseEnhanceStoneCost = 1;
    public int   BaseGoldCost         = 100;

    // ── 유틸 ──────────────────────────────────────────────────

    public float GetStatValue(EquipStatEntry entry, int enhanceLevel)
        => entry.Delta * (1f + (ItemLevel - 1 + enhanceLevel) * ValuePerLevel);

    public int GetEnhanceStoneCost(int currentLevel) => BaseEnhanceStoneCost * (currentLevel + 1);
    public int GetEnhanceGoldCost(int currentLevel)  => BaseGoldCost         * (currentLevel + 1);
}

// ── 트리거 종류 ───────────────────────────────────────────────

public enum EquipmentTrigger
{
    None     = 0,
    OnAttack = 1,
    OnHit    = 2,
}

// ── 스탯 항목 ─────────────────────────────────────────────────

[Serializable]
public struct EquipStatEntry
{
    public StatType Stat;
    public float    Delta;
}
