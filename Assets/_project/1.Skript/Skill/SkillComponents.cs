using Unity.Entities;

// ============================================================
//  SkillComponents.cs
//  스킬 관련 ECS 컴포넌트 정의 (장군 전용)
//
//  ■ GeneralPassiveSkillComponent
//    - 장군에게 붙음
//    - PassiveSkillAuraSystem이 주기적으로 읽어
//      CommandRadius 내 아군 병사에게 StatusEffectBufferElement 추가
//
//  ■ GeneralActiveSkillComponent
//    - 장군에게 붙음
//    - ActiveSkillCooldownSystem이 쿨다운을 감소시키고
//      UseActiveSkillTag 가 붙으면 발동 처리
//
//  ■ UseActiveSkillTag
//    - 스킬 발동 요청 시 장군 Entity에 AddComponent
//    - 시스템이 처리 후 즉시 제거
// ============================================================

namespace BattleGame.Units
{
    // ──────────────────────────────────────────
    // 패시브 스킬
    // ──────────────────────────────────────────

    /// <summary>
    /// 장군의 패시브 버프 스킬.
    /// GeneralComponent.CommandRadius 범위 내 아군 병사에게 스탯 버프를 지속 적용한다.
    /// </summary>
    public struct GeneralPassiveSkillComponent : IComponentData
    {
        public StatType BuffStat;       // 버프할 스탯 종류 (Attack, Defense …)
        public float    BuffValue;      // 버프 수치 (고정값)
        public float    AuraRadius;     // 적용 반경 (0 = CommandRadius 전체 사용)

        /// <summary>버프를 StatusEffectBufferElement로 변환할 때 사용할 Duration</summary>
        public const float RefreshDuration = 2f; // 2초마다 갱신되며 만료 전 재적용
    }

    // ──────────────────────────────────────────
    // 액티브 스킬
    // ──────────────────────────────────────────

    /// <summary>
    /// 장군의 액티브 스킬 상태.
    /// CooldownRemaining 이 0 이하면 사용 가능 (UseActiveSkillTag 로 발동).
    /// </summary>
    public struct GeneralActiveSkillComponent : IComponentData
    {
        public int                  SkillId;
        public ActiveSkillEffectType EffectType;
        public float                EffectValue;
        public float                EffectRadius;
        public float                EffectDuration;
        public float                Cooldown;
        public float                CooldownRemaining;

        public bool IsReady => CooldownRemaining <= 0f;
    }

    // ──────────────────────────────────────────
    // 발동 요청 태그
    // ──────────────────────────────────────────

    /// <summary>
    /// 장군 Entity에 붙이면 다음 프레임에 액티브 스킬을 발동한다.
    /// ActiveSkillSystem이 처리 후 자동 제거.
    /// </summary>
    public struct UseActiveSkillTag : IComponentData { }
}
