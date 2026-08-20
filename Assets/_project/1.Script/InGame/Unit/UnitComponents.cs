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
        public bool  HasSkill;           // GeneralActiveSkillComponent 공유 여부
        /// <summary>넉백 내성. 0 = 없음, 1 = 완전 면역. 넉백 벡터를 (1-값)배로 감소.</summary>
        public float KnockbackResistance;
    }

    // ──────────────────────────────────────────
    // 보스 전용 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>보스 유닛 마커 + 페이즈 데이터 + AoE 공격 + 돌진 패턴.</summary>
    public struct BossComponent : IComponentData
    {
        // ── 페이즈 ────────────────────────────────────────────
        public int   PhaseCount;
        public int   CurrentPhase;    // 현재 페이즈 (1부터 시작)
        public float Phase2HpRatio;   // 2페이즈 전환 체력 비율 (예: 0.5 = 50%)
        public float Phase3HpRatio;   // 3페이즈 전환 체력 비율 (PhaseCount < 3 이면 무시)

        /// <summary>행동불능(스턴) 내성. 0 = 없음, 1 = 완전 면역.</summary>
        public float CCResistance;
        /// <summary>넉백 내성. 0 = 없음, 1 = 완전 면역.</summary>
        public float KnockbackResistance;

        // ── AoE 기본 공격 ─────────────────────────────────────
        /// <summary>AoE 공격 반경. 타겟 주변 이 범위 내 아군도 피해를 받는다.</summary>
        public float AoeRadius;
        /// <summary>AoE 범위 피해 비율. 1.0 = 100%, 0.6 = 60%.</summary>
        public float AoeSplashRatio;
        /// <summary>공격 시 적용되는 넉백 강도 (이동 속도 단위).</summary>
        public float AttackKnockbackForce;
        /// <summary>넉백 지속 시간 (초).</summary>
        public float AttackKnockbackDuration;

        // ⚠ 돌진 필드는 전부 없앴다
        //   돌진이 ActiveSkillId.BossCharge 스킬로 옮겨가면서
        //   쿨다운·타겟·이동을 ActiveSkillSlot + BossChargeRunner 가 갖는다.
        //   패턴을 추가할 때마다 이 컴포넌트에 필드를 늘리던 구조를 끝냈다.
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

    /// <summary>
    /// 성장만으로 얻은 스탯 — 등급·레벨 롤(GeneralStatRoller.Roll)까지다.
    /// 장비·패시브·어빌리티·유물·특성·도감이 붙기 **전** 값.
    ///
    /// ■ 왜 따로 들고 있나 — "외부로 오른 증가분" 은 StatComponent 만으로 못 센다
    ///   StatComponent.Base 는 이미 모든 출처가 합쳐진 값이고, Final - Base 는
    ///   전투 중 버프뿐이다. 그래서 '속전속결' 처럼 성장분과 외부분을 갈라야 하는
    ///   패시브가 전투 시작 시점에 기준을 잡을 방법이 없었다
    ///   (Final - Base 로 읽어 늘 0 이 나왔다).
    ///
    ///   증가분 = StatComponent.Base[s] - Roll[s]
    ///
    /// ⚠ 장군에게만 붙인다
    ///   병사는 장수 스탯을 환산해 받으므로 자기 롤이라는 것이 없다.
    /// </summary>
    public struct BaseRollStatComponent : IComponentData
    {
        public StatBlock Roll;
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

        /// <summary>
        /// 비행 중인 발사체가 이미 확정지은 피해량 (방어율 적용 후).
        /// ProjectileIncomingDamageSystem 이 매 프레임 살아있는 발사체로부터 다시 계산한다
        /// — 누적 필드가 아니므로 환불·정합성 관리가 필요 없다.
        /// </summary>
        public float IncomingDamage;

        /// <summary>날아오는 발사체까지 감안한 실효 체력. 0 이하 = 이미 죽은 목숨.</summary>
        public float EffectiveHp => CurrentHp - IncomingDamage;

        /// <summary>
        /// 아직 살아 있지만(이동·피격 판정 유지) 도착 예정 피해로 이미 죽은 것이 확정된 상태.
        /// 새 공격의 타겟에서 제외해 오버킬 낭비를 막는다.
        /// </summary>
        public bool IsDoomed => CurrentHp > 0f && CurrentHp - IncomingDamage <= 0f;
    }

    /// <summary>
    /// 공격 런타임 상태만 보유.
    /// AttackDamage / AttackRange / AttackSpeed → StatComponent.Final
    /// </summary>
    public struct AttackComponent : IComponentData
    {
        public float  AttackCooldown;      // 다음 공격까지 남은 시간
        public Entity TargetEntity;
        public float3 TargetPosition;      // 타겟 마지막 위치 캐시 (Chasing 이동용, 3프레임마다 갱신)
        public bool   HasTarget;
        public uint   RandomSeed;          // 크리티컬 판정용 per-entity 랜덤 시드
        public bool   AttackedThisFrame;   // 이번 프레임에 공격 발생 — CooldownTickJob이 매 프레임 초기화
        public float  LastDamageDealt;     // 마지막 공격으로 가한 피해 — OnAttack 트리거용
    }

    /// <summary>
    /// 피격 이벤트 타입.
    /// Normal    — 일반 공격 (근거리·원거리 기본 공격)
    /// Skill     — 스킬 직접 타격 (HitDirection 을 넉백으로 그대로 사용)
    /// Reflected — 거울 방어 반사 피해 (다시 반사 불가)
    /// </summary>
    public enum HitType : byte
    {
        Normal    = 0,
        Skill     = 1,
        Reflected = 2,
    }

    /// <summary>
    /// 피격 이벤트 버퍼 (한 프레임에 여러 번 맞을 수 있음)
    /// DynamicBuffer 로 선언해 GC 없이 가변 크기 처리
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct HitEventBufferElement : IBufferElementData
    {
        public float   Damage;
        public float3  HitDirection;   // Normal: 방향벡터, Skill: 방향×힘(직접 사용)
        public Entity  AttackerEntity;
        public HitType Type;
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

    /// <summary>버프/디버프 출처 종류. GeneralPanelUI 아이콘 표시에 사용.</summary>
    public enum BuffSourceType : byte
    {
        None        = 0,
        ActiveSkill = 1,  // ActiveSkillId 값
        Passive     = 2,  // PassiveSkillType 값
        Ability     = 3,  // AbilityId 값
        Equipment   = 4,  // 장비 슬롯 인덱스 (0 또는 1)
    }

    /// <summary>
    /// 활성 버프/디버프 하나.
    /// StatType 기반이므로 새 스텟 추가 시 이 구조체는 수정하지 않아도 된다.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct StatusEffectBufferElement : IBufferElementData
    {
        public StatType        Stat;        // 영향받는 스텟 종류 (Dot 이면 무시)
        public float           Delta;       // 효과 수치 (양수 = 강화, 음수 = 약화)
        public EffectMode      Mode;        // Add / Multiply / Dot
        public float           Duration;    // 전체 지속 시간 (-1 이면 영구)
        public float           Remaining;   // 남은 시간
        public BuffSourceType  SourceType;  // 출처 종류
        public int             SourceId;    // 출처 내 ID (종류별 의미는 BuffSourceType 주석 참고)
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
        Charging  = 6,  // 기사 달인 돌진 중
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
        /// <summary>
        /// 데미지를 받은 프레임에 true 로 설정된다.
        /// 스턴 여부와 무관하게 피격 플래시를 발동시키기 위해 사용.
        /// UnitAnimationSync 가 읽은 뒤 false 로 리셋한다.
        /// </summary>
        public bool   NeedsFlash;
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

    /// <summary>
    /// 무적 유닛 — 피격 연출(플래시·넉백·경직)은 그대로 받되 체력이 깎이지 않는다.
    ///
    /// ■ 왜 태그인가
    ///   로비 데모는 예전에 0.2초마다 아군 체력을 가득 채우는 방식으로 버텼다.
    ///   장군은 체력 통이 커서 버텼지만 **병사는 그 0.2초 안에 한 방에 죽었다**.
    ///   죽음은 UnitHitSystem 이 피해를 넣는 그 자리에서 확정되므로,
    ///   밖에서 아무리 자주 채워도 타이밍 싸움을 이길 수 없다.
    ///
    /// ⚠ 예약 피해(IncomingDamage)도 함께 막아야 한다
    ///   실효 체력이 0 이하면 IsDoomed 가 서고, UnitAttackSystem 은 그 유닛을
    ///   '사망 확정' 으로 보고 공격을 멈춘다 — 안 죽는데 싸우지도 않게 된다.
    ///   ProjectileIncomingDamageSystem 이 이 태그를 건너뛰는 이유다.
    /// </summary>
    public struct InvulnerableTag : IComponentData { }

    /// <summary>
    /// 도발 태그 — 이 태그를 가진 유닛은 적의 우선 타겟이 된다.
    /// 철벽 방어 스킬 시전 시 EffectDuration 동안 부여된다.
    /// </summary>
    public struct TauntTag : IComponentData
    {
        public float Remaining;  // 남은 지속시간 (초)
    }

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
        public Entity   AttackerEntity;  // 통계 귀속용
        public float3   AttackerPos;
        public float3   TargetPos;
        public float    Damage;
        public float    Speed;
        public TeamType Team;
    }

    // ──────────────────────────────────────────
    // 전투 통계 버퍼 / 태그
    // ──────────────────────────────────────────

    /// <summary>
    /// 피격 처리 결과 버퍼. ProcessHitEventsJob 이 TARGET 엔티티에 기록.
    /// BattleStatCollectorSystem 이 매 프레임 읽고 BattleStatsTracker 에 귀속 후 Clear.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct DamageResultElement : IBufferElementData
    {
        public Entity  AttackerEntity;   // 공격자 (딜 귀속용)
        public float   ActualDamage;    // 방어 적용 후 실제 피해
        public float   AbsorbedDamage;  // 방어로 감소된 피해
        public bool    IsKill;          // 이 히트로 대상이 사망했는가
        public HitType Type;            // 피격 종류 (통계 분류·반사 여부 판단용)
    }

    /// <summary>
    /// 힐 이벤트 버퍼. 모든 힐 소스가 TARGET 엔티티에 Append.
    /// UnitHealSystem 이 매 프레임 읽어 HP 회복 + BattleStatsTracker 기록 후 Clear.
    /// </summary>
    [InternalBufferCapacity(4)]
    public struct HealEventBufferElement : IBufferElementData
    {
        public float  Amount;        // 회복량 (최대 체력 클램프 전)
        public Entity SourceEntity;  // 회복 주체 (미래 확장용)
    }

    /// <summary>
    /// 소환 스킬로 생성된 유닛 마커 (SummonSkeleton / SummonElite 등).
    /// 딜 귀속 시 SoldierDmg 가 아닌 SkillDmg 로 분류하기 위해 사용.
    /// </summary>
    public struct SummonedTag : IComponentData { }

    // ──────────────────────────────────────────
    // 어빌리티 전투 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// 거울 방어 (AbilityMirrorArmor) 어빌리티가 붙은 유닛 마커.
    /// ProcessHitEventsJob 이 이 컴포넌트를 확인해 공격자에게 반사 HitEvent 를 보낸다.
    /// 반사된 피해(HitType.Reflected)는 다시 반사되지 않는다.
    /// </summary>
    public struct MirrorArmorComponent : IComponentData
    {
        public float ReflectRatio;  // 반사 비율 (0.25 = 25%)
    }

    /// <summary>
    /// 쌍신 공격 (AbilityTwinStrike) 어빌리티가 붙은 유닛 마커.
    /// MeleeAttackJob / RangedAttackJob 이 이 태그를 확인해 1회 공격마다 HitEvent 를 2번 추가한다.
    /// </summary>
    public struct DoubleStrikeTag : IComponentData { }

    // ──────────────────────────────────────────
    // 달인(Mastery) 어빌리티 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// 기사 달인 — 주기적 돌진 공격 상태 관리.
    /// CooldownTimer <= 0 이 되면 KnightChargeJob 이 IsCharging = true 로 돌진을 개시한다.
    /// 타겟에 도달하면 MeleeAttackJob 이 300% 피해를 적용하고 타이머를 리셋한다.
    /// </summary>
    public struct KnightChargeComponent : IComponentData
    {
        public float  CooldownTimer;    // 남은 대기 시간 (0 이하 = 돌진 준비)
        public float  CooldownMax;      // 재충전 시간 (기본 6초)
        public bool   IsCharging;       // 현재 돌진 이동 중
        public Entity ChargeTarget;     // 돌진 타겟 엔티티
        public float3 ChargeTargetPos;  // InitiateJob 이 매 프레임 갱신 → MoveJob 이 lookup 없이 사용
    }

    /// <summary>
    /// 궁수 달인 — 일반 공격 시 50% 확률로 인접 2번째 적에게도 공격.
    /// ArcherMultiShotSystem 이 AttackedThisFrame 프레임에 추가 발사체를 생성한다.
    /// </summary>
    public struct ArcherMultiShotTag : IComponentData { }

    /// <summary>
    /// 마법사 달인 — 일반 공격 시 1% 확률로 보유 스킬 즉시 발동.
    /// MageSkillProcSystem 이 AttackedThisFrame 프레임에 UseActiveSkillTag 를 추가한다.
    /// </summary>
    public struct MageSkillProcTag : IComponentData { }

    /// <summary>
    /// 방패병 달인 — 넉백 완전 무시.
    /// ProcessHitEventsJob 이 이 태그를 확인해 KnockbackVelocity / StunDuration 적용을 건너뛴다.
    /// </summary>
    public struct KnockbackImmuneTag : IComponentData { }

    // ──────────────────────────────────────────
    // 특성(Trait) 런타임 컴포넌트
    // ──────────────────────────────────────────

    /// <summary>
    /// K4 전우의 분노 — 병사 사망마다 장군 MaxHp +1% 누적.
    /// TraitSoldierRageSystem 이 SoldierDeathEvent 를 읽어 DeathCount 를 증가.
    /// TraitFinalApplySystem 이 Base[MaxHp] × 0.01 × DeathCount 를 Final[MaxHp] 에 가산.
    /// </summary>
    public struct TraitSoldierRageComponent : IComponentData
    {
        public int DeathCount;
    }

    /// <summary>
    /// K12 영웅의 귀환 — 최초 사망 시 1HP 생존 + 전체 병사 즉시 소환.
    /// ProcessHitEventsJob 이 사망 직전에 HasActivated=false 이면 발동,
    /// TraitRuntimeSystem 이 ShouldRespawnSoldiers=true 를 감지해 병사를 소환한다.
    /// </summary>
    public struct TraitHeroReturnComponent : IComponentData
    {
        public bool HasActivated;
        public bool ShouldRespawnSoldiers;
    }

    /// <summary>
    /// 퇴각 사격 — 적이 공격 사거리 절반 이내로 붙으면 뒤로 물러나며 사격을 유지한다.
    /// MoveToDestinationJob 이 이 태그를 감지해 후퇴 이동을 적용한다.
    ///
    /// ⚠ 특성이 아니라 **궁수의 기본 행동**이다 (GeneralRuntimeBridge 가 붙인다)
    ///   예전엔 TraitType.ArcherRetreatFire 특성이었다. 궁수의 정체성 자체가
    ///   "거리를 유지하며 평타로 딜한다" 인데, 그 정체성을 고를 수 있는 옵션으로
    ///   두니 안 고르면 궁수가 근접 유닛처럼 굴었다. 특성 슬롯 하나를 쓸 만한
    ///   선택지도 아니어서 기본 행동으로 내렸다.
    /// </summary>
    public struct ArcherRetreatFireTag : IComponentData { }

    /// <summary>
    /// A4 폭우 사격 — 공격이 실제로 타겟에 닿았을 때(OnAttackLanded) 주변 적 2명 스플래시.
    /// CombatTriggerSystem 이 AttackHitEvent 버퍼를 감지해 TraitRainFireHandler 를 발동한다.
    /// </summary>
    public struct TraitRainFireTag : IComponentData { }

    /// <summary>
    /// 장군의 기본 공격이 실제로 타겟에 닿았을 때 기록되는 이벤트.
    /// 근거리: MeleeAttackJob(ECB) → 다음 프레임 CombatTriggerSystem 에서 처리.
    /// 원거리: ProjectileHitJob(ECB) → 다음 프레임 CombatTriggerSystem 에서 처리.
    /// OnAttackLanded 트리거 핸들러가 이 버퍼를 읽어 동작한다.
    /// </summary>
    public struct AttackHitEvent : IBufferElementData
    {
        public Entity TargetEntity; // 피격된 주 타겟
        public float3 TargetPos;    // 착탄 위치 (이펙트 출발점 등에 사용)
        public float  Damage;       // 가해진 피해량
    }

    /// <summary>
    /// S2 반격의 달인 — 방어율로 흡수한 피해만큼 공격자에게 반사.
    /// ProcessHitEventsJob 이 absorbed 값을 공격자 HitEventBuffer 에 Reflected 로 추가한다.
    /// </summary>
    public struct TraitCounterBlowTag : IComponentData { }

    /// <summary>
    /// S6 분노 축적 — 피격마다 공격력 +3% 누적 (최대 10스택 = +30%).
    /// ProcessHitEventsJob 이 피해를 받은 프레임에 Stacks 를 증가.
    /// TraitFinalApplySystem 이 Base[Attack] × 0.03 × Stacks 를 Final[Attack] 에 가산.
    /// </summary>
    public struct TraitRageBuildComponent : IComponentData
    {
        public int Stacks;
    }

    /// <summary>
    /// M_new1 마법 집중 — 일반 공격 성공 시 스킬 쿨타임 1초 감소.
    /// TraitAttackCdrSystem 이 AttackedThisFrame 감지 후 CooldownRemaining -= 1 적용.
    /// </summary>
    public struct TraitAttackCdrTag : IComponentData { }

    /// <summary>
    /// M_new2 연속 시전 — 스킬 사용 후 쿨타임 없이 1회 즉시 재발동, 재발동 피해 -40%.
    /// TraitRuntimeSystem 이 SkillUseEvent 를 감지해 에코 발동 상태를 관리한다.
    /// </summary>
    public struct TraitEchoSkillComponent : IComponentData
    {
        public bool  IsEchoQueued;
        public float SavedEffectValue;
    }
}
