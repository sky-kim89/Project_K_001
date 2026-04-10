using Unity.Entities;
using Unity.Mathematics;

// ============================================================
//  UnitComponents.cs
//  유닛의 모든 데이터(Component)를 정의하는 파일
//  ECS 원칙: 데이터와 로직을 완전히 분리
//  - Component = 순수 데이터만 보유, 로직 없음
//
//  ■ 스텟 구조 (버프/성장 대응)
//    StatComponent.Base  — 기본 스텟 (성장·장비 확정값, 버프 절대 미적용)
//    StatComponent.Final — 최종 스텟 (버프·디버프 적용 후 캐시)
//    → 접근법:  stat.Final[StatType.Attack]
//    → UnitStatusEffectSystem 이 매 프레임 Final 재계산
//    → 전투 시스템은 모두 StatFinal 에서만 읽는다
//
//  ■ 통합된 컴포넌트
//    MovementComponent  ← VelocityComponent 흡수 (Velocity 필드)
//    HealthComponent    ← CurrentHp 런타임 상태만 (MaxHp → StatFinal)
//    AttackComponent    ← 쿨다운·타겟만 (공격력 등 → StatFinal)
//
//  ■ 제거
//    VelocityComponent  — MovementComponent.Velocity 로 대체
//    StatusEffectType   — StatType + EffectMode 로 대체
// ============================================================

namespace BattleGame.Units
{
    // ──────────────────────────────────────────
    // 유닛 기본 정보
    // ──────────────────────────────────────────

    /// <summary>유닛 고유 식별 및 소속 정보</summary>
    public struct UnitIdentityComponent : IComponentData
    {
        public int      UnitId;
        public TeamType Team;    // 아군 / 적군
        public UnitType Type;    // 병사 / 장군 / 일반적 / 엘리트 / 보스
    }

    public enum UnitType : byte
    {
        Soldier  = 0,   // 장군 소속 병사 (아군/적군)
        General  = 1,   // 장군 — 병사를 지휘 (아군/적군)
        Enemy    = 2,   // 일반 적 — 독립 전투 유닛
        Elite    = 3,   // 엘리트 — 독립 강화 유닛
        Boss     = 4,   // 보스 — 독립 특수 유닛 (페이즈 보유)
    }

    // ──────────────────────────────────────────
    // 장군 전용 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// 장군 유닛에게만 붙는 컴포넌트.
    /// 스킬은 GeneralPassiveSkillComponent / GeneralActiveSkillComponent 로 분리.
    /// </summary>
    public struct GeneralComponent : IComponentData
    {
        public float CommandRadius; // 지휘 반경 — 이 범위 내 소속 병사에게 패시브 버프 적용
    }

    /// <summary>
    /// 장군 생성 시 Baker 가 붙이는 병사 스폰 요청.
    /// SoldierSpawnSystem 이 처리 후 이 컴포넌트를 제거한다.
    /// </summary>
    public struct SpawnSoldiersRequest : IComponentData
    {
        public Entity SoldierPrefab;
        public int    Count;
        public float  StatScaleRatio;  // 병사 스텟 = 장군 스텟 × 이 값
    }

    // ──────────────────────────────────────────
    // 병사 전용 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// 병사 유닛에게만 붙는 컴포넌트.
    /// GeneralEntity 는 SoldierSpawnSystem 이 스폰 시 주입한다.
    /// </summary>
    public struct SoldierComponent : IComponentData
    {
        public Entity GeneralEntity;   // 소속 장군 (스폰 시 채워짐)
        public float  StatScaleRatio;  // 장군 스텟 대비 병사 스텟 비율
        public bool   IsInitialized;
    }

    // ──────────────────────────────────────────
    // 엘리트 전용 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>엘리트 유닛 마커. 독립 전투 유닛으로 장군-병사 계층 없음.</summary>
    public struct EliteComponent : IComponentData
    {
        public bool HasSkill; // GeneralActiveSkillComponent 공유 여부
    }

    // ──────────────────────────────────────────
    // 보스 전용 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>보스 유닛 마커 + 페이즈 데이터.</summary>
    public struct BossComponent : IComponentData
    {
        public int   PhaseCount;
        public int   CurrentPhase;    // 현재 페이즈 (1부터 시작)
        public float Phase2HpRatio;   // 2페이즈 전환 체력 비율 (예: 0.5 = 50%)
        public float Phase3HpRatio;   // 3페이즈 전환 체력 비율 (PhaseCount < 3 이면 무시)

        /// <summary>행동불능(스턴) 내성. 0 = 없음, 1 = 완전 면역. 스턴 지속 시간을 (1-값)배로 감소.</summary>
        public float CCResistance;
        /// <summary>넉백 내성. 0 = 없음, 1 = 완전 면역. 넉백 벡터를 (1-값)배로 감소.</summary>
        public float KnockbackResistance;
    }

    // ──────────────────────────────────────────
    // 스텟 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// 유닛 스텟 컴포넌트.
    ///
    /// Base  — 성장·장비로 확정된 기본값. 인게임 버프는 절대 쓰지 않는다.
    /// Final — Base + 활성 버프/디버프. 전투 시스템은 여기서만 읽는다.
    ///
    /// 읽기:  float atk = stat.Final[StatType.Attack];
    /// 쓰기:  stat.Base[StatType.MaxHp] = 500f;
    /// 복사:  stat.ResetFinalToBase();
    ///
    /// StatType 추가 시 이 파일은 건드리지 않아도 된다 (StatBlock 이 자동 확장).
    /// </summary>
    public struct StatComponent : IComponentData
    {
        public StatBlock Base;   // 기본 스텟 (성장·장비)
        public StatBlock Final;  // 최종 스텟 (버프 적용 후, 매 프레임 재계산)

        /// <summary>Final 을 Base 값으로 초기화. 버프 재계산 직전에 호출.</summary>
        public void ResetFinalToBase() => Final = Base;  // StatBlock 은 값 타입이므로 struct copy
    }

    // ──────────────────────────────────────────
    // 이동 관련 컴포넌트 (VelocityComponent 흡수)
    // ──────────────────────────────────────────

    /// <summary>
    /// 이동 상태 컴포넌트. VelocityComponent 를 흡수해 Velocity 필드를 가진다.
    /// MoveSpeed 는 StatComponent.Final[StatType.MoveSpeed] 에서 읽는다.
    /// </summary>
    public struct MovementComponent : IComponentData
    {
        public float3 Velocity;          // 현재 프레임 이동 벡터 (VelocityComponent 대체)
        public float3 Destination;       // 목적지 (전선 위치 등)
        public float  StoppingDistance;  // 이 거리 안으로 들어오면 이동 중지
        public float  MoveDelay;         // 스폰 후 이동 시작까지 대기 시간 (초). 0이면 즉시 이동.
        public bool   IsMoving;
    }

    /// <summary>포지션 레이어 (전열/중열/후열)</summary>
    public struct FormationSlotComponent : IComponentData
    {
        public int    Row;          // 0 = 전열, 1 = 중열, 2 = 후열
        public int    Column;       // 같은 열 안에서의 가로 인덱스
        public float3 SlotPosition; // 배정된 진형 슬롯 월드 좌표
    }

    // ──────────────────────────────────────────
    // 전투 런타임 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// HP 런타임 상태만 보유.
    /// MaxHp / Defense → StatComponent.Final
    /// 사망 여부 → DeadTag
    /// </summary>
    public struct HealthComponent : IComponentData
    {
        public float CurrentHp;
    }

    /// <summary>
    /// 공격 런타임 상태만 보유.
    /// AttackDamage / AttackRange / AttackSpeed → StatComponent.Final
    /// </summary>
    public struct AttackComponent : IComponentData
    {
        public float  AttackCooldown;   // 다음 공격까지 남은 시간
        public Entity TargetEntity;
        public float3 TargetPosition;   // 타겟 마지막 위치 캐시 (Chasing 이동용, 3프레임마다 갱신)
        public bool   HasTarget;
        public uint   RandomSeed;       // 크리티컬 판정용 per-entity 랜덤 시드
    }

    /// <summary>
    /// 피격 이벤트 버퍼 (한 프레임에 여러 번 맞을 수 있음)
    /// DynamicBuffer 로 선언해 GC 없이 가변 크기 처리
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct HitEventBufferElement : IBufferElementData
    {
        public float  Damage;
        public float3 HitDirection;    // 넉백 방향 계산용
        public Entity AttackerEntity;
    }

    // ──────────────────────────────────────────
    // 버프 / 디버프 버퍼
    // ──────────────────────────────────────────

    /// <summary>
    /// 버프·디버프 적용 방식.
    /// Add      — StatFinal += Delta  (절대값 증감)
    /// Multiply — StatFinal *= Delta  (배율, 1.3 = 30% 증가)
    /// Dot      — 초당 Delta 만큼 CurrentHp 직접 감소 (도트 데미지, Stat 필드 무시)
    /// </summary>
    public enum EffectMode : byte
    {
        Add      = 0,
        Multiply = 1,
        Dot      = 2,
    }

    /// <summary>
    /// 활성 버프/디버프 하나.
    /// StatType 기반이므로 새 스텟 추가 시 이 구조체는 수정하지 않아도 된다.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct StatusEffectBufferElement : IBufferElementData
    {
        public StatType   Stat;       // 영향받는 스텟 종류 (Dot 이면 무시)
        public float      Delta;      // 효과 수치 (양수 = 강화, 음수 = 약화)
        public EffectMode Mode;       // Add / Multiply / Dot
        public float      Duration;   // 전체 지속 시간 (-1 이면 영구)
        public float      Remaining;  // 남은 시간
    }

    // ──────────────────────────────────────────
    // 상태 머신 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>유닛 현재 행동 상태</summary>
    public struct UnitStateComponent : IComponentData
    {
        public UnitState Current;
        public UnitState Previous;
        public float     StateTimer; // 현재 상태 진입 후 경과 시간
    }

    public enum UnitState : byte
    {
        Idle      = 0,  // 대기
        Moving    = 1,  // 이동 중
        Chasing   = 2,  // 적 추격
        Attacking = 3,  // 공격 중
        Hit       = 4,  // 피격 경직
        Dead      = 5,
    }

    // ──────────────────────────────────────────
    // 피격 반응 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>피격 시 넉백, 경직 처리용</summary>
    public struct HitReactionComponent : IComponentData
    {
        public float3 KnockbackVelocity; // 넉백 이동 벡터
        public float  StunDuration;      // 경직 지속 시간 (초)
        public float  StunTimer;         // 경직 잔여 시간
        public bool   IsStunned;
    }

    // ──────────────────────────────────────────
    // 공간 분할용 Grid 컴포넌트
    // ──────────────────────────────────────────

    // ──────────────────────────────────────────
    // 화면 경계 상태
    // ──────────────────────────────────────────

    /// <summary>
    /// 유닛이 한 번이라도 화면 안에 들어왔으면 HasEnteredScreen = true.
    /// ScreenClampSystem 이 이 값을 보고 화면 밖으로 나가지 않게 위치를 클램프한다.
    /// </summary>
    public struct ScreenStateComponent : IComponentData
    {
        public bool HasEnteredScreen;
    }

    /// <summary>
    /// 유닛이 현재 속한 Grid 셀 좌표.
    /// SpatialGridSystem 이 매 프레임 갱신.
    /// </summary>
    public struct GridCellComponent : IComponentData
    {
        public int2 Cell;      // 현재 셀
        public int2 PrevCell;  // 직전 프레임 셀 (변경 감지용)
    }

    /// <summary>
    /// 유닛의 물리적 크기 반경.
    /// GameObject.transform.localScale 에서 계산 (Max(x,y) * 0.5f).
    /// SeparationJob 에서 두 유닛의 반경 합을 밀어낼 거리로 사용한다.
    /// </summary>
    public struct UnitSizeComponent : IComponentData
    {
        public float Radius;
        /// <summary>
        /// 분리 질량. 클수록 다른 유닛에게 밀리지 않고 더 강하게 밀어낸다.
        /// General = 5, Soldier/Enemy = 1
        /// </summary>
        public float Mass;
    }

    // ──────────────────────────────────────────
    // 태그
    // ──────────────────────────────────────────

    /// <summary>Hybrid Renderer 와 연결용 태그. 이 컴포넌트가 붙은 Entity 만 스프라이트 업데이트 수행.</summary>
    public struct NeedsRenderSyncTag : IComponentData { }

    /// <summary>죽은 유닛에 붙이는 태그 — 각 시스템에서 이 태그로 필터링해 연산 제외.</summary>
    public struct DeadTag : IComponentData { }

    // ──────────────────────────────────────────
    // 직업 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// 유닛 직업 컴포넌트.
    /// GeneralRuntimeBridge.AddComponents() 에서 설정.
    /// 적은 직업 시스템 도입 시 EnemyRuntimeBridge.AddComponents() 에서 설정 예정.
    /// </summary>
    public struct UnitJobComponent : IComponentData
    {
        public UnitJob Job;
    }

    // ──────────────────────────────────────────
    // 원거리 공격 태그
    // ──────────────────────────────────────────

    /// <summary>
    /// 원거리 공격(발사체 사용) 유닛 마커.
    /// UnitJob.Archer / UnitJob.Mage 인 경우에만 추가.
    /// RangedAttackJob 필터링 및 ProjectileLaunchRequest 버퍼 추가 여부에 사용.
    /// </summary>
    public struct RangedTag : IComponentData { }

    // ──────────────────────────────────────────
    // 발사체 발사 요청 버퍼
    // ──────────────────────────────────────────

    /// <summary>
    /// 원거리 유닛(RangedTag 보유) entity 에만 추가되는 버퍼.
    /// RangedAttackJob 이 공격마다 추가 → ProjectileSpawnSystem 이 같은 프레임에 처리 후 Clear.
    /// </summary>
    [InternalBufferCapacity(2)]
    public struct ProjectileLaunchRequest : IBufferElementData
    {
        public Entity   TargetEntity;
        public float3   AttackerPos;
        public float3   TargetPos;
        public float    Damage;
        public float    Speed;
        public TeamType Team;
    }
}
