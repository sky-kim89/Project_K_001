using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveSkillData.cs
//  액티브 스킬 베이스 ScriptableObject.
//
//  ■ 일반 구조
//    - 쿨다운 / 효과 수치는 여기에 정의
//    - 실제 실행 로직(이동 트윈, 타격, 넉백 연출 등)은 서브클래스에서 Execute() 오버라이드
//
//  ■ 실행 흐름
//    1. AI 또는 입력 → Entity 에 UseActiveSkillTag 추가
//    2. ActiveSkillCooldownSystem → 쿨다운 확인 후 UseActiveSkillTag 제거 + 쿨다운 리셋
//       동시에 ActiveSkillExecuteEvent 버퍼에 이벤트 추가
//    3. ActiveSkillExecuteSystem(managed) → Execute(context) 호출
//    4. Execute() 내부에서 트윈/이동/ECS 이벤트 처리
//
//  ■ 추가 액티브 스킬 시 작업
//    1. ActiveSkillData 를 상속한 클래스 생성 → Execute() 오버라이드
//    2. SO 생성 + GeneralRuntimeBridge 의 ActiveSkill 슬롯에 할당
//    3. ActiveSkillId 에 새 ID 추가
//
//  ■ ActiveSkillContext
//    Execute() 에 전달되는 실행 컨텍스트.
//    ECS Entity/StatComponent 와 Unity GO/Transform 에 동시 접근 가능.
// ============================================================

[CreateAssetMenu(fileName = "ActiveSkillData", menuName = "BattleGame/ActiveSkillData")]
public class ActiveSkillData : ScriptableObject
{
    // ─────────────────────────────────────────────────────────
    // ■ 식별
    // ─────────────────────────────────────────────────────────

    [Header("식별")]
    [Tooltip("스킬 고유 ID. GeneralActiveSkillComponent.SkillId 에 저장된다.")]
    public ActiveSkillId SkillId;

    [Tooltip("스킬 이름 (에디터·UI 표시용)")]
    public string SkillName;

    [TextArea(2, 4)]
    [Tooltip("스킬 설명 (UI 표시용)")]
    public string Description;

    // ─────────────────────────────────────────────────────────
    // ■ 기본 수치
    // ─────────────────────────────────────────────────────────

    [Header("기본 수치")]
    [Min(0f)]
    [Tooltip("쿨다운 (초)")]
    public float Cooldown = 15f;

    [Tooltip("효과 수치 기본값 (데미지 배율, 버프량 등 — 스킬별 해석이 다름)")]
    public float EffectValue = 1f;

    [Tooltip("효과 반경 (0 이면 단일 대상)")]
    public float EffectRadius = 0f;

    [Tooltip("효과 지속 시간 (0 이면 즉발)")]
    public float EffectDuration = 0f;

    // ─────────────────────────────────────────────────────────
    // ■ 직업 제한 (참조용 — 스킬 배정 로직에서 외부에서 활용)
    // ─────────────────────────────────────────────────────────

    [Header("직업 제한 (비워두면 모든 직업 가능)")]
    [Tooltip("이 스킬을 사용할 수 있는 직업 목록. 스킬 배정 시 외부에서 참조.")]
    public UnitJob[] AllowedJobs = new UnitJob[0];

    // ─────────────────────────────────────────────────────────
    // ■ 이펙트 풀 키 (PoolType.Effect)
    // ─────────────────────────────────────────────────────────

    [Header("이펙트 풀 키 (PoolType.Effect)")]
    [Tooltip("사용자(시전자) 이펙트 풀 키. 비워두면 미사용.")]
    public string CasterEffectKey = "";

    [Tooltip("피격 대상 이펙트 풀 키. 비워두면 미사용.")]
    public string TargetEffectKey = "";

    [Tooltip("기본/범위 이펙트 풀 키 (존 중심, 낙하 예고 등). 비워두면 미사용.")]
    public string BaseEffectKey = "";

    [Min(0.1f)]
    [Tooltip("이펙트 자동 반납 딜레이 (초). 파티클 재생 시간에 맞게 조절.")]
    public float EffectDespawnDelay = 2f;


    // ─────────────────────────────────────────────────────────
    // ■ 실행 진입점 — 서브클래스에서 오버라이드
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 스킬을 실행한다. ActiveSkillExecuteSystem 이 이벤트 발생 시 호출.
    /// context 를 통해 ECS Entity / StatComponent 와 Unity GO / Transform 에 접근 가능.
    /// </summary>
    public virtual void Execute(ActiveSkillContext context) { }
}

// ─────────────────────────────────────────────────────────────
// ■ 실행 컨텍스트
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 스킬 실행 시 전달되는 컨텍스트.
/// ECS 데이터와 Unity MonoBehaviour 양쪽에 동시 접근할 수 있다.
/// </summary>
public struct ActiveSkillContext
{
    // ECS
    public Entity         CasterEntity;    // 스킬 사용자(제너럴) Entity
    public Entity         TargetEntity;    // 현재 공격 타겟 Entity (없으면 Entity.Null)
    public StatComponent  CasterStat;      // 사용자 StatComponent 스냅샷
    public EntityManager  EntityManager;

    // Unity
    public GameObject     CasterObject;    // 사용자 GameObject (트윈, 애니메이션 등)
    public UnityEngine.Transform CasterTransform;

    // 편의 프로퍼티
    public bool HasTarget => TargetEntity != Entity.Null;
}

// ─────────────────────────────────────────────────────────────
// ■ 액티브 스킬 ID
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 액티브 스킬 고유 ID.
/// GeneralActiveSkillComponent.SkillId 에 저장되며
/// 스킬별 실행 시스템이 이 값으로 분기한다.
/// </summary>
public enum ActiveSkillId : int
{
    None             = 0,
    HeavyStrike      = 1,   // 강타          — 단일 돌진 타격 + 넉백         (방패·전사)
    VolleyFire       = 2,   // 일제 사격      — 전체 즉시 일반 공격           (궁수·법사)
    LeapStrike       = 3,   // 도약 강타      — 전방 도약 + AoE 타격 + 넉백   (방패·전사)
    HealAura         = 4,   // 치유 오라      — 주변 아군 체력 회복           (공통)
    TargetHeal       = 5,   // 집중 치유      — 가장 체력 낮은 장군+병사 회복  (공통)
    ChargeSoldier    = 6,   // 돌격 병사 소환 — 적 밀치며 데미지, 전투 참여   (방패)
    SummonSkeleton   = 7,   // 스켈레톤 소환  — 사망 병사 공·체 일부로 소환   (공통)
    PoisonZone       = 8,   // 독성 지대      — 이속 감소 + 지속 피해 영역    (법사·궁수)
    Meteor           = 9,   // 메테오         — 강력한 AoE 피해 + 넉백        (법사)
    Blizzard         = 10,  // 블리자드       — 공·이속 감소 + 지속 피해 영역  (법사)
    SacrificeSoldier = 11,  // 병사 희생      — 병사 즉사, 그 공·체 일부 흡수  (공통)
    Bind             = 12,  // 속박           — 단일 완전 행동불능 + 지속 피해  (공통)
    SuicideSoldier   = 13,  // 자폭 병사      — 병사가 적에게 달려 폭발        (법사)
    Berserker        = 14,  // 광전사         — 공격속도 대폭 증가 (일시)      (전사)
    IronShield       = 15,  // 철벽 방어      — 방어율 대폭 증가 (일시)        (방패)
    ArrowRain        = 16,  // 화살 비        — 범위 지속 피해                 (궁수)
    BattleCry        = 17,  // 전투 함성      — 주변 아군 공격력 증가 (일시)   (전사·방패)
    Shockwave        = 18,  // 충격파         — 전방 부채꼴 넉백               (전사)
    SwiftStrike      = 19,  // 신속 연격      — 자신·병사 공격속도 대폭 증가   (궁수)
    SummonElite      = 20,  // 정예 소환      — 강화된 병사 분대 소환          (법사)
}
