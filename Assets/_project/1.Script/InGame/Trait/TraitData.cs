using System;
using UnityEngine;

// ============================================================
//  TraitData.cs
//  특성 1종을 정의하는 ScriptableObject.
//
//  - 캐릭터 선택 시 UnitEntry.Trait 에 배정됨
//  - HeroStatResolver Step 6 에서 TraitBonuses 로 적용됨
//  - 패시브·어빌리티와 달리 레벨 없이 고정 효과
//
//  생성: BattleGame > 데이터 생성 > 특성 전체 생성
// ============================================================

[CreateAssetMenu(fileName = "Trait_", menuName = "ProjectK/TraitData")]
public class TraitData : ScriptableObject
{
    [Header("식별")]
    public TraitType TraitType;
    public string    TraitName;
    [TextArea(2, 4)]
    public string    Description;

    [Tooltip("이 특성을 자동 배정할 직업. 직업 선택 시 OnStartPressed 에서 매핑.")]
    public UnitJob   RequiredJob;

    [Header("아이콘")]
    public Sprite Icon;

    [Header("효과")]
    public TraitStatEntry[] Effects = Array.Empty<TraitStatEntry>();

    [Header("스탯 전환")]
    [Tooltip("한 스탯을 다른 스탯으로 환산한다 (중갑·거인·속공 등).\n" +
             "TraitApplier 가 모든 레이어 합산이 끝난 뒤 마지막에 처리한다.")]
    public StatConversion[] Conversions = Array.Empty<StatConversion>();

    [Header("누적 스택 보너스")]
    [Tooltip("스택을 쌓는 트리거. None = 스택 없음.")]
    public PassiveTrigger StackTrigger;
    [Tooltip("최대 스택 수. 0 = 무제한.")]
    public int MaxStacks;
    [Tooltip("스택 1개당 적용되는 스탯 보너스.")]
    public TraitStatEntry[] StackStatBonuses = Array.Empty<TraitStatEntry>();

    // ── 단일 스탯 수정자 ─────────────────────────────────────

    [Serializable]
    public struct TraitStatEntry
    {
        [Tooltip("변경할 스탯 종류")]
        public StatType Stat;

        [Tooltip("수치. IsPercent=true 이면 기본 스탯의 N% (0.1 = 10%), false 이면 절대값 가산.")]
        public float Value;

        [Tooltip("true → Base 스탯 × Value 가산 / false → Value 를 직접 가산")]
        public bool IsPercent;
    }

    // ── 스탯 전환 ────────────────────────────────────────────
    /// <summary>
    /// From 스탯을 PerUnit 단위로 세어, 그 개수만큼 To 스탯을 Rate 비율로 올린다.
    ///
    ///     증가량 = To의 현재값 × Rate × (From의 현재값 / PerUnit)
    ///
    /// 예) 중갑 = From:Defense, PerUnit:0.01, To:Attack, Rate:0.015
    ///     → 방어율 45% 면 45단위 × 1.5% = 공격력 +67.5%
    ///
    /// "현재값" 은 전환이 시작되기 전 스냅샷이다 — 전환끼리 서로를 증폭시키거나
    /// 특성 획득 순서에 따라 결과가 달라지지 않게 하기 위함.
    /// </summary>
    [Serializable]
    public struct StatConversion
    {
        [Tooltip("환산 재료가 되는 스탯")]
        public StatType From;

        [Tooltip("From 스탯 몇 단위마다 계산할지. 방어율·쿨감·치명확률은 0.01(=1%p) 이 자연스럽다.")]
        public float PerUnit;

        [Tooltip("보상으로 올릴 스탯")]
        public StatType To;

        [Tooltip("1단위당 To 스탯을 올릴 비율 (0.015 = 1.5%)")]
        public float Rate;
    }
}
