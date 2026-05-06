using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  PassiveSkillData.cs
//  패시브 스킬 베이스 ScriptableObject.
//
//  ■ 일반 패시브 (스폰 시 즉시 스텟 적용)
//    TriggerType = None.
//    StatModifiers 에 수치를 입력하면 스폰 시 자동 적용.
//    서브클래스 없이 PassiveSkillData 를 직접 사용한다.
//
//  ■ 조건부 / 이벤트 반응 패시브
//    TriggerType 에 반응할 이벤트를 지정.
//    PassiveSkillRuntimeSystem 이 해당 이벤트 발생 시 OnTrigger() 를 호출.
//    서브클래스에서 OnTrigger() 를 오버라이드해 구체적인 처리를 구현.
//
//  ■ 추가 패시브 시 작업 범위
//    - 스텟만 변경: SO 생성 → Inspector 에서 StatModifiers 설정 → DB 등록
//    - 이벤트 반응: SO 생성 + 서브클래스 작성 → TriggerType 설정 → DB 등록
//    - 완전히 새로운 TriggerType 필요 시: enum 에 추가 + 시스템에 감지 코드 추가
// ============================================================

/// <summary>
/// 패시브 스킬이 반응하는 게임 이벤트 종류.
/// PassiveSkillData.TriggerType 에 설정한다.
/// </summary>
public enum PassiveTrigger : byte
{
    None           = 0,  // 이벤트 반응 없음 (스폰 시 즉시 적용 패시브)
    OnHit          = 1,  // 제너럴이 피격될 때 (매 피격마다 호출)
    OnAttack       = 2,  // 제너럴이 공격할 때
    OnEnemyKill    = 3,  // 제너럴 또는 소속 병사가 적을 처치할 때
    OnSoldierDeath = 4,  // 소속 병사가 사망할 때
}

[CreateAssetMenu(fileName = "PassiveSkillData", menuName = "BattleGame/PassiveSkillData")]
public class PassiveSkillData : ScriptableObject
{
    // ─────────────────────────────────────────────────────────
    // ■ 식별
    // ─────────────────────────────────────────────────────────

    [Header("식별")]
    [Tooltip("이 데이터가 대응하는 패시브 종류")]
    public PassiveSkillType Type;

    [Tooltip("패시브 이름 (에디터·UI 표시용)")]
    public string SkillName;

    [TextArea(2, 4)]
    [Tooltip("패시브 설명 (UI 표시용)")]
    public string Description;

    // ─────────────────────────────────────────────────────────
    // ■ 스폰 시 즉시 적용 스텟 목록
    // ─────────────────────────────────────────────────────────

    [Header("스폰 시 즉시 적용 스텟 (TriggerType = None 패시브)")]
    [Tooltip("스폰 시 한 번 적용되는 스텟 변경 목록.\n" +
             "이벤트 반응 패시브는 비워두고 TriggerType 과 서브클래스로 처리한다.")]
    public List<StatModifierEntry> StatModifiers = new();

    // ─────────────────────────────────────────────────────────
    // ■ 런타임 이벤트 트리거
    // ─────────────────────────────────────────────────────────

    [Header("런타임 이벤트 트리거")]
    [Tooltip("어떤 게임 이벤트에 반응할지. None 이면 스폰 시 즉시 적용 패시브.")]
    public PassiveTrigger TriggerType = PassiveTrigger.None;

    // ─────────────────────────────────────────────────────────
    // ■ 특수 플래그
    // ─────────────────────────────────────────────────────────

    [Header("특수 플래그")]
    [Tooltip("TitanGeneral: 제너럴 크기 배율 추가값 (0 = 변화 없음)")]
    public float GeneralScaleBonusAdd = 0f;

    // ─────────────────────────────────────────────────────────
    // ■ 런타임 트리거 콜백 — 서브클래스에서 필요할 때만 오버라이드
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// TriggerType 에 해당하는 게임 이벤트 발생 시 호출된다.
    /// 기본 구현은 아무것도 하지 않음. 서브클래스에서 오버라이드.
    /// </summary>
    public virtual void OnTrigger(PassiveTriggerContext context) { }

    // ─────────────────────────────────────────────────────────
    // ■ 단일 스텟 수정자 항목
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct StatModifierEntry
    {
        [Tooltip("변경할 스텟 종류")]
        public StatType Stat;

        [Tooltip("수치 (IsPercent=true 이면 기존 스텟의 N% 증감)")]
        public float Delta;

        [Tooltip("true  → 기존 스텟 × Delta 를 더함 (0.2 = +20%)\n" +
                 "false → Delta 를 절대값으로 더함")]
        public bool IsPercent;

        [Tooltip("어디에 적용할지:\n" +
                 "General = 제너럴 UnitStat (SpawnEntity 전)\n" +
                 "Soldier = 병사 StatComponent (Initialize 후)")]
        public PassiveSkillApplier.ApplyTarget Target;
    }
}

// ─────────────────────────────────────────────────────────────
// ■ 트리거 컨텍스트
// ─────────────────────────────────────────────────────────────

/// <summary>
/// OnTrigger() 에 전달되는 이벤트 컨텍스트.
/// 모든 트리거 타입이 공통으로 사용한다.
/// </summary>
public struct PassiveTriggerContext
{
    public Entity         GeneralEntity;
    public EntityManager  EntityManager;

    // TriggerType 별 부가 데이터
    public HealthComponent Health;       // OnHit: 피격 시점 체력 스냅샷
    public int             SoldierDeathCount; // OnSoldierDeath: 이번 프레임 사망 병사 수
}
