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

## 에디터 툴 — `Tools > Project K` (루트 하나로 통일됨)

> ⚠ `BattleGame/` 루트는 폐기됨. 모든 메뉴는 `Tools/Project K/` 아래에 있다.
> **메뉴 경로를 문자열로 직접 쓰지 말 것** — `Assets/_project/1.Script/Editor/ProjectKMenu.cs`
> 의 상수를 조합한다: `[MenuItem(ProjectKMenu.Popup + "Event", priority = ProjectKMenu.PrefabPrio + 42)]`

```
Tools/Project K/
├─ 씬 이동/          Splash·Lobby·InGame 로드 (Ctrl+Shift+Alt+1/2/3)
├─ 씬 셋업/          Splash 씬 구성 · InGame 씬 구성 · Lobby 씬 패치
├─ 프리팹 생성/      ▶ 전체 생성
│   ├─ 로비/         TopBar · MainPanel · BattlePanel · HeroPanel · RelicPanel
│   ├─ 팝업/         ▶ 팝업 전체 · BattleResult · Pause · Loading · AbilitySelect
│   │                AbilityList · EquipCompare · Disassemble · HeroDetail
│   │                RunShop · Reincarnation · Mercenary · Event
│   ├─ 인게임/       GeneralPanel · RewardCard
│   └─ 이펙트/       Effect 프리팹 (22종)
├─ 데이터 생성/      ▶ 전체 생성 · 액티브/패시브 스킬 · 특성 · 어빌리티 · 유물
│                    장비 · 이벤트 · StageConfig · SpriteManager
├─ 아이콘·텍스처/    ▶ 전체 생성 · 직업·스킬/특성/어빌리티/유물/아이템/장비/
│                    스테이지노드/로비버튼 아이콘 · 이벤트 일러스트 · 이펙트 텍스처
└─ 도구/             Cheat Editor · 스킬 SO 에 이펙트 키 연결
```

**프리팹 정본 (중복 생성자 금지)** — 한 프리팹은 한 Creator 만 만든다.

| 프리팹 | 정본 Creator |
|--------|-------------|
| BattleResult · Pause · Loading · AbilitySelect · AbilityList · ExpRow | `PopupPrefabCreator.cs` |
| EquipComparePopup | `EquipComparePopupCreator.cs` |
| DisassemblePopup | `DisassemblePopupCreator.cs` |
| HeroPanel · HeroCard · EquipCard | `HeroPanelCreator.cs` |
| GeneralPanel · RewardCard | `UISetupTool.cs` |

`UISetupTool` 은 씬 계층 담당이며 팝업 프리팹은 `PopupPrefabCreator` 에 위임한다.
`HeroPanelCreator.BuildCardPrefab()` 은 BattlePanel·Mercenary·RunShop 이 공유하는
장수 카드 팩토리이므로 `public` 유지 필수.

**공용 UI 빌더** — `Assets/_project/1.Script/Editor/EditorUIBuilder.cs`
Creator 들이 각자 복사해 쓰던 `Make*/Create*/Add*` 헬퍼의 본문은 전부 여기 있다.
각 Creator 는 기존 이름을 한 줄 포워더로만 유지한다. 새 헬퍼가 필요하면
로컬에 또 만들지 말고 여기에 추가할 것.

---

## UI 제작 규칙 (Creator 작성 시 필수)

> **⚠ 규칙 1 — 누를 수 있는 버튼에는 반드시 음각을 넣는다**
> 평평한 사각형은 버튼인지 라벨인지 구분이 안 된다.
> `EditorUIBuilder.RaisedBtn()` / `RaisedTextBtn()` / `RaisedBtnOn()` 으로만 만들 것.
> ```csharp
> var btn = EditorUIBuilder.RaisedBtn(parent, "BuyBtn", faceColor, out var body);
> // 라벨·아이콘은 반드시 body 아래에 넣는다 (루트에 넣으면 눌려도 안 내려간다)
> ```
> 구조: `Shadow`(아래 6px 노출 = 두께) → `Body` → `TopEdge`(밝은 2px) + `BottomEdge`(어두운 4px).
> 눌림 색은 `Button.colors` 가 targetGraphic 색에 **곱해지므로** `TintFor()` 로 역산한다.
> 템플릿처럼 루트가 이미 있으면 `RaisedBtnOn(root, ...)` — 자식 경로가 `Body/...` 로 유지된다.

> **⚠ 규칙 2 — 장식 기호에 폰트 글리프를 쓰지 않는다**
> 기본 폰트 `LiberationSans SDF` 는 **문자 250자(ASCII + Latin-1 일부)뿐**이고
> `m_AtlasPopulationMode: 0` = **Static** 이라 런타임에 글리프를 채울 수도 없다.
> 없는 글자는 □(두부)로 그려져 그대로 화면에 노출된다. (한글은 폴백 폰트가 처리)
>
> | 없음 (쓰지 말 것) | 있음 (써도 됨) |
> |---|---|
> | `★ ✔ ✕ ▶ ◀ ▲ ⚙ 🔒` | `› — × € ™ □` |
>
> `EditorUIBuilder.CheckMark / XMark / Chevron / Diamond / PadLock / Bar` 로 그릴 것.
> 새 기호가 필요하면 `Bar()`(회전 막대)를 조합해 헬퍼를 추가한다.

> **⚠ 규칙 3 — 반투명 테두리를 자식으로 두지 않는다**
> Unity UI 는 자기 Graphic 을 먼저 그리고 그 다음 자식을 그린다.
> `SetAsFirstSibling()` 을 해도 자식은 부모 Image 보다 뒤로 갈 수 없다.
> 테두리는 대상의 **앞 형제**로 만들어 뒤에 깔 것.

> **⚠ 규칙 4 — 폰트 크기는 `UIScale` 상수만 쓴다**
> `FontSm(34) / FontMd(42) / FontLg(56) / FontXl(76)`, 버튼 `BtnSm(100) / BtnMd(132) / BtnLg(164)`.
> 하드코딩 금지. 모바일 실기 기준으로 잡힌 값이며 `FontSm` 미만은 읽히지 않는다.

> **⚠ 규칙 5 — 칸 높이를 손으로 적지 않는다 (글자 잘림 방지)**
> TMP 한 줄은 폰트의 **약 1.25배**를 쓴다. 칸을 폰트보다 작게 잡으면 아래가 잘린다.
> ```csharp
> UIScale.RowSm / RowMd / RowLg     // 43 / 53 / 70 — 한 줄짜리 칸
> UIScale.Line(fontSize)            // 임의 폰트의 한 줄 높이
> UIScale.BtnFor(fontSize)          // 라벨이 안 눌리는 최소 버튼 높이 (×1.7)
> ```
> 폰트 상수를 올릴 때 이 값을 쓰는 칸은 자동으로 따라 커진다.

> **⚠ 규칙 6 — 팝업 높이는 `UIScale.PopupMaxH`(1000) 를 넘기지 않는다**
> 로비 캔버스 세로가 1080 뿐이라 그 이상은 위아래가 잘린다.
> 고정 높이 대신 세로 스트레치 + 가변 영역으로 만드는 쪽이 더 안전하다
> (EventPopup 참고: 패널은 캔버스에 맞추고 `ChoiceRoot` 가 남는 높이를 흡수).

**캔버스 기준이 씬마다 다르다 (주의)**
`Lobby.unity` = **1920×1080 가로**, `InGame.unity` = 1080×1920 세로.
팝업 대부분은 로비 위에 뜨므로 **세로 여유가 1080 뿐**이다.
팝업 높이를 고정하면 잘린다 — 세로 스트레치 + 가변 영역으로 만들 것.

---

## 오브젝트 풀 (PoolType)

```csharp
UI=0, Unit=1, Effect=2, Projectile=3
```
`PoolController` 싱글턴 → `ObjectPool<T>` 관리.

---

## 작업 유형별 관련 파일

> 세션 시작 시 아래 목록만 읽으면 탐색 없이 바로 작업 가능.

### 액티브 스킬 추가
1. `GameEnums.cs` — `ActiveSkillId` enum 값 추가
2. `InGame/Skill/Actives/Active{이름}.cs` — 신규 생성 (기존 파일 참고)
3. `InGame/Battle/Editor/ActiveSkillCreator.cs` — SO 자동 생성 에디터 툴
4. `Assets/Resources/ActiveSkillDatabase.asset` — SO 등록 (에디터)
5. `LocalizationManager.cs` — 한국어 이름 추가
6. (이펙트 필요 시) `InGame/Battle/Editor/GameAssetCreator.cs`

### 패시브 스킬 추가
1. `InGame/Skill/PassiveSkillType.cs` — enum 추가
2. `InGame/Skill/PassiveSkillRuntimeSystem.cs` — 효과 로직 추가
3. `Assets/Resources/PassiveSkillDatabase.asset` — 등록 (에디터)

### 로비 UI — 영웅 패널 수정
- **로직**: `UI/Lobby/HeroPanelUI.cs`
- **프리팹 재생성**: `UI/Lobby/Editor/HeroPanelCreator.cs` → `Tools > Project K > 로비 UI > Create HeroPanel Prefab`
- **카드 UI**: `UI/Lobby/HeroCardUI.cs`
- **장비 카드**: `UI/Lobby/EquipCardUI.cs`
- **텍스트 상수**: `UIConstants.cs`, `LocalizationManager.cs`

### 팝업 추가/수정
- **베이스**: `UI/Popup/PopupBase.cs` — 상속 필수, `protected override void Awake()` + `base.Awake()` 호출
- **관리자**: `UI/Popup/PopupManager.cs` — `_prefabs` 배열에 신규 프리팹 등록 필요
- **열기**: `PopupManager.Instance.Open<T>(PopupType.X).Setup(...)`
- **enum**: `GameEnums.cs` — `PopupType` 에 값 추가
- **장비 비교 팝업**: `UI/Lobby/EquipComparePopup.cs`

### 장비 시스템
- **데이터**: `InGame/Equipment/EquipmentData.cs`, `InGame/Equipment/EquipmentDatabase.cs`
- **UI**: `UI/Lobby/EquipComparePopup.cs`, `UI/Lobby/EquipCardUI.cs`
- **영웅 슬롯 표시**: `UI/Lobby/HeroPanelUI.cs` (`RefreshEquipSlot`)
- **세이브**: `Data/Sections/UnitData.cs` (`RunEquipSlots`, `RunEquipEnhance`)

### 세이브 데이터 수정
- **매니저**: `Data/Core/UserDataManager.cs`
- **섹션 추가**: `Data/Core/ISaveSection.cs` 구현 → `UserDataManager` 등록
- **기존 섹션**: `Data/Sections/` 하위 파일들
- **저장 트리거**: `UserDataManager.Instance.RequestSave()`

### 전투 밸런스 / 스탯
- **기본 스탯**: `InGame/GameplayConfig.cs` (SO) — `Assets/Resources/GameplayConfig.asset`
- **스탯 타입**: `InGame/Stat/StatType.cs`
- **유닛 스탯 계산**: `InGame/Stat/UnitStat.cs`
- **직업·등급 배율**: `InGame/Unit/UnitJob.cs`

### 인게임 UI 수정
- **HUD 매니저**: `UI/InGame/InGameUIManager.cs` (또는 `InGameManager.cs`)
- **전투 흐름**: `InGame/Battle/BattleManager.cs`
- **상태 관리**: `InGame/Battle/InGameManager.cs`

### 이펙트 추가
1. `BattleGame > Generate Effect Textures` — 텍스처·머티리얼 생성
2. `BattleGame > Generate Effect Prefabs` — 프리팹 생성
3. `BattleGame > Link Effect Keys to Skills` — SO에 키 연결
- 키 형식: `"FX_스킬이름"` (예: `FX_Meteor_Explosion`)
- 프리팹 위치: `Assets/_project/2.Prefabs/Effect/`

### 싱글톤 생성
- **MonoBehaviour 싱글톤** (씬에 배치 필요): `Singleton<T>` 상속
- **순수 C# 싱글톤** (씬 독립, 자동 생성): `SingletonPure<T>` 상속
- 위치: `Assets/_project/1.Skript/Singleton.cs`

---

## 코딩 규칙

> **⚠ 절대 규칙 — 방어적 null 체크 금지**
> `?.`, `if (x == null) return`, `?? default` 로 로직을 조용히 스킵하지 말 것.
> null이면 예외가 터져야 버그를 즉시 발견할 수 있다.
> 예외: Inspector 에서 선택적으로 연결하는 UI 컴포넌트 (`_button?.onClick` 등).

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
