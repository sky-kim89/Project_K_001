using Unity.Entities;

// ============================================================
//  SkillComponents.cs
//  스킬 관련 ECS 컴포넌트 정의 (장군·엘리트 공용)
//
//  ■ GeneralPassiveSkillComponent
//    - 장군에게 붙음
//    - PassiveSkillAuraSystem 이 주기적으로 읽어
//      CommandRadius 내 소속 병사에게 StatusEffectBufferElement 추가
//    - 버프는 StatType + EffectMode(Add/Multiply) 기반
//
//  ■ GeneralActiveSkillComponent
//    - 장군·엘리트에게 붙음
//    - ActiveSkillCooldownSystem 이 쿨다운을 감소시키고
//      UseActiveSkillTag 가 붙으면 쿨다운 리셋 + 태그 제거
//    - 실제 스킬 실행(트윈·이동·공격 제어)은 별도 스킬 실행기에서 수행
//
//  ■ UseActiveSkillTag
//    - 스킬 발동 요청 시 Entity 에 AddComponent
//    - ActiveSkillCooldownSystem 이 처리 후 즉시 제거
// ============================================================

namespace BattleGame.Units
{
    // ──────────────────────────────────────────
    // 패시브 스킬
    // ──────────────────────────────────────────

    /// <summary>
    /// 장군의 패시브 버프 스킬.
    /// CommandRadius 범위 내 소속 병사의 StatusEffectBuffer 에 버프를 지속 유지한다.
    /// </summary>
    public struct GeneralPassiveSkillComponent : IComponentData
    {
        public StatType   BuffStat;     // 버프할 스텟 종류
        public float      BuffValue;    // 버프 수치
        public EffectMode BuffMode;     // Add(절대값) / Multiply(배율)
        public float      AuraRadius;   // 적용 반경 (0 이면 CommandRadius 전체 사용)
    }

    // ──────────────────────────────────────────
    // 액티브 스킬
    // ──────────────────────────────────────────

    /// <summary>
    /// 장군·엘리트의 액티브 스킬 상태.
    /// CooldownRemaining 이 0 이하면 사용 가능 (UseActiveSkillTag 로 발동).
    /// </summary>
    public struct GeneralActiveSkillComponent : IComponentData
    {
        public int   SkillId;
        public float EffectValue;
        public float EffectRadius;
        public float EffectDuration;
        public float Cooldown;
        public float CooldownRemaining;

        public bool IsReady => CooldownRemaining <= 0f;
    }

    // ──────────────────────────────────────────
    // 발동 요청 태그
    // ──────────────────────────────────────────

    /// <summary>
    /// Entity 에 붙이면 다음 프레임에 액티브 스킬을 발동한다.
    /// ActiveSkillCooldownSystem 이 처리 후 자동 제거.
    /// </summary>
    public struct UseActiveSkillTag : IComponentData { }

    // ──────────────────────────────────────────
    // 스킬 실행 이벤트 버퍼
    // ──────────────────────────────────────────

    /// <summary>
    /// ActiveSkillCooldownSystem 이 스킬 발동 조건이 충족될 때 이 버퍼에 이벤트를 추가.
    /// ActiveSkillExecuteSystem(managed) 이 같은 프레임에 읽어 Execute() 를 호출 후 Clear.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ActiveSkillExecuteEvent : IBufferElementData
    {
        public int   SkillId;       // 발동할 스킬 ID (ActiveSkillId enum)
        public Entity TargetEntity; // 현재 공격 타겟 (없으면 Entity.Null)
    }
}
