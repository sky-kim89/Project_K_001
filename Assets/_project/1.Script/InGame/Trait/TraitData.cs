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
}
