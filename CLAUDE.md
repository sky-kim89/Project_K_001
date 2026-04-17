# Project K — Claude Code 컨텍스트

> 이 파일은 Claude Code가 매 세션 자동으로 읽습니다.
> 프로젝트 파악 시간을 줄이기 위해 핵심 정보를 여기에 유지합니다.

---

## 프로젝트 개요

- **장르**: 2D 전술 배틀 / 자동 전투 (오토배틀)
- **엔진**: Unity 6 (URP 17.0.4) + **Unity ECS (Entities 1.4.5)**
- **언어**: C# — 네임스페이스 `BattleGame.*`
- **작업 디렉터리**: `d:\project\Project_K_001`
- **스크립트 루트**: `Assets/_project/1.Skript/`

---

## 아키텍처 — ECS + Managed Bridge 혼합

```
ECS (Jobs/Burst)                    Managed (MonoBehaviour)
────────────────                    ────────────────────────
UnitComponents.cs                   UnitRuntimeBridge.cs
UnitAttackSystem.cs                 GeneralRuntimeBridge.cs
UnitMovementSystem.cs               SoldierRuntimeBridge.cs
UnitTargetSearchSystem.cs           EnemyRuntimeBridge.cs
UnitHitSystem.cs                    UnitAppearanceBridge.cs
UnitStatusEffectSystem.cs
GeneralSkillSystem.cs
ActiveSkillAISystem.cs  →  ActiveSkillExecuteSystem.cs (managed)
ProjectileSystem.cs     →  ProjectileView.cs (managed)
```

**핵심 패턴**: ECS 시스템이 로직 처리 → Managed Bridge가 시각/애니메이션 동기화.  
스킬 실행은 `ActiveSkillExecuteSystem`에서 `ActiveSkillData.Execute(context)`를 호출 (managed).

---

## 유닛 계층

| 타입 | ECS Component | Managed Bridge | 설명 |
|------|--------------|----------------|------|
| General (장군) | `GeneralComponent` | `GeneralRuntimeBridge` | 플레이어 유닛, 병사 지휘 |
| Soldier (병사) | `SoldierComponent` | `SoldierRuntimeBridge` | 장군 소속 자동 전투원 |
| Enemy | `EnemyComponent` | `EnemyRuntimeBridge` | 일반 적 |
| Elite | `EliteComponent` | - | 강화된 적 |
| Boss | `BossComponent` | - | 웨이브 보스 |

---

## 직업 시스템 (UnitJob)

```csharp
Knight       = 0  // 균형 스텟, 이동속도 최고
Archer       = 1  // 사거리 최고, 낮은 체력
Mage         = 2  // 공격력 최고, 낮은 체력·연사
ShieldBearer = 3  // 방어율·체력 최고
```

등급: `Normal(×1.0) → Uncommon(×1.1) → Rare(×1.2) → Unique(×1.3) → Epic(×1.4)`

---

## 스킬 시스템

### 액티브 스킬 (ActiveSkillId)
총 20종 — `ActiveSkillData` ScriptableObject + 각 `Active*.cs` 구현체

| ID | 이름 | 직업 |
|----|------|------|
| 1 | HeavyStrike 강타 | 방패·전사 |
| 2 | VolleyFire 일제사격 | 궁수·법사 |
| 3 | LeapStrike 도약강타 | 방패·전사 |
| 4 | HealAura 치유오라 | 공통 |
| 5 | TargetHeal 집중치유 | 공통 |
| 6 | ChargeSoldier 돌격병사 | 방패 |
| 7 | SummonSkeleton 스켈레톤소환 | 공통 |
| 8 | PoisonZone 독성지대 | 법사·궁수 |
| 9 | Meteor 메테오 | 법사 |
| 10 | Blizzard 블리자드 | 법사 |
| 11 | SacrificeSoldier 병사희생 | 공통 |
| 12 | Bind 속박 | 공통 |
| 13 | SuicideSoldier 자폭병사 | 법사 |
| 14 | Berserker 광전사 | 전사 |
| 15 | IronShield 철벽방어 | 방패 |
| 16 | ArrowRain 화살비 | 궁수 |
| 17 | BattleCry 전투함성 | 전사·방패 |
| 18 | Shockwave 충격파 | 전사 |
| 19 | SwiftStrike 신속연격 | 궁수 |
| 20 | SummonElite 정예소환 | 법사 |

### 패시브 스킬 (PassiveSkillType)
18종 — `PassiveSkillData` SO + `PassiveSkillRuntimeSystem`  
등급별 슬롯: Normal/Uncommon=1, Rare/Unique=2, Epic=3

---

## 핵심 파일 위치

```
Assets/_project/
├── 1.Skript/
│   ├── GameEnums.cs                         # GameState, PopupType, PoolType 등
│   ├── Data/
│   │   ├── Core/  UserDataManager, SaveCoordinator, ISaveSection
│   │   └── Sections/  UserData.cs, UnitData.cs
│   └── InGame/
│       ├── Authoring/   *Authoring.cs + Baker (ECS 씬 설정)
│       ├── Battle/
│       │   ├── BattleManager.cs             # 전투 총괄
│       │   ├── InGameManager.cs             # 인게임 상태 관리
│       │   ├── BattleEnums.cs               # BattleState, SpawnUnitType
│       │   ├── Spawner/  AllySpawner, EnemySpawner
│       │   └── Editor/  GameAssetCreator, ActiveSkillCreator, IconGenerator, WaveSetupDataEditor
│       ├── Skill/
│       │   ├── ActiveSkillData.cs           # SO 베이스, ActiveSkillId enum
│       │   ├── ActiveSkillExecuteSystem.cs  # Execute() 호출 (managed)
│       │   ├── ActiveSkillAISystem.cs       # 쿨다운·AI 판단 (ECS)
│       │   ├── ActiveSkillDatabase.cs       # SO 컬렉션
│       │   ├── Actives/  Active*.cs (20종)
│       │   ├── PassiveSkillType.cs          # enum 18종
│       │   ├── PassiveSkillRuntimeSystem.cs # ECS 패시브 적용
│       │   └── Editor/  EffectTextureGenerator, EffectPrefabGenerator, EffectKeyLinker
│       ├── Unit/
│       │   ├── UnitComponents.cs            # ECS 컴포넌트 정의
│       │   ├── UnitJob.cs                   # UnitJob enum, UnitGrade enum
│       │   ├── UnitAttackSystem.cs
│       │   ├── UnitMovementSystem.cs
│       │   ├── UnitTargetSearchSystem.cs
│       │   ├── UnitHitSystem.cs
│       │   ├── UnitStatusEffectSystem.cs
│       │   └── *RuntimeBridge.cs (General/Soldier/Enemy)
│       ├── Projectile/  ProjectileSystem.cs, ProjectileView.cs
│       ├── Appearance/  UnitAppearanceBridge, UnitAnimationSync
│       ├── Stat/  StatType.cs, UnitStat.cs
│       └── GameplayConfig.cs
├── 2.Prefabs/
│   └── Effect/  FX_*.prefab (22개 — 이펙트 프리팹)
├── 3.Textures/
│   ├── FX/     이펙트 텍스처
│   └── Icons/  Classes/(4종), Skills/(20종) PNG 스프라이트
├── 4.Materials/
│   └── FX/     MAT_FX_*.mat (15종 URP Add머티리얼)
├── ActiveSkillDatabase.asset
├── PassiveSkillDatabase.asset
└── GameplayConfig.asset
```

---

## 에디터 툴 (Tools > BattleGame / Project K 메뉴)

| 메뉴 | 스크립트 | 역할 |
|------|---------|------|
| BattleGame > Generate Effect Textures | EffectTextureGenerator.cs | 이펙트 텍스처 14종 + 머티리얼 15종 생성 |
| BattleGame > Generate Effect Prefabs | EffectPrefabGenerator.cs | 이펙트 프리팹 22종 생성 |
| BattleGame > Link Effect Keys to Skills | EffectKeyLinker.cs | SO에 이펙트 풀 키 자동 연결 |
| Tools > Project K > Generate Icons | IconGenerator.cs | 직업·스킬 PNG 아이콘 24장 생성 |

---

## 오브젝트 풀 (PoolType)

```csharp
UI=0, Unit=1, Effect=2, Projectile=3
```
`PoolController` 싱글턴 → `ObjectPool<T>` 관리.

---

## 코딩 규칙

- **ECS Component 구조체**: `I ComponentData` 또는 `IBufferElementData`
- **Baker 클래스**: Authoring 파일 하단에 인라인으로 작성
- **스킬 추가 순서**: ① `ActiveSkillId` enum 추가 → ② `Active*.cs` 생성 → ③ SO 생성 → ④ DB 등록
- **이펙트 키**: `"FX_스킬이름"` 형식 (예: `FX_Meteor_Explosion`)
- **네임스페이스**: 유닛 관련은 `BattleGame.Units`, 나머지는 전역 또는 미사용
- **프리팹 풀 반납**: `EffectDespawnDelay` 초 후 자동 반납 (`SkillEffectHelper`)

---

## 현재 완료된 주요 작업 (2026-04)

- [x] ECS 배틀 시스템 기본 구조 (공격·이동·타겟팅·피격)
- [x] 액티브 스킬 20종 + 이펙트 22종 + 이펙트 파이프라인
- [x] 패시브 스킬 18종 ECS 구현
- [x] 발사체(Projectile) 포물선 비행 + 넉백 시스템
- [x] 유닛 외형·애니메이션 Managed Bridge
- [x] 직업·등급 아이콘 PNG 24장 (IconGenerator)
- [x] 세이브 시스템 (UserDataManager + ISaveSection)
- [x] 인게임 UI HTML 목업 (UI_Mockup.html)

---

## 기억해야 할 사항

- Unity 6 + ECS 1.4.5 — DOTS API 최신 버전 사용 (`SystemAPI`, `IAspect` 등)
- `com.unity.vectorgraphics` 패키지 없음 — SVG 직접 임포트 불가, PNG 필요
- 오브젝트 풀은 항상 사용 — `new` 대신 `PoolController.Get()`
- 스킬 Execute()는 메인 스레드에서 실행됨 (Burst 불가)
