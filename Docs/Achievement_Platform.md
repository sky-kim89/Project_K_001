# 업적 시스템 — 진행 기록 & 플랫폼 연동 가이드

> 최종 갱신: 2026-09-20
> 대상: 이 프로젝트를 이어서 작업하는 사람 (Steam·Google 계정이 생긴 뒤 연동할 때 이 문서만 보면 된다)

---

## 1. 현재 상태 한눈에

| 단계 | 상태 | 비고 |
|---|---|---|
| 업적 정의 (34개, 전부 1회성) | ✅ 완료 | `InGame/Achievement/AchievementCatalog.cs` |
| 판정 로직 | ✅ 완료 | `InGame/Achievement/AchievementTracker.cs` |
| 로컬 저장 | ✅ 완료 | `Data/Sections/AchievementData.cs` (`SaveKey.Achievement = 17`, 환생 무관) |
| 기존 세이브 소급 판정 | ✅ 완료 | `LobbyManager.Start` → `AchievementTracker.EvaluateAll()` |
| 플랫폼 연동 구조 (다리·재동기화·개발 빌드 차단) | ✅ 완료 | `InGame/Achievement/Platform/` |
| 릴리스 점검 경고 | ✅ 완료 | `ReleasePlayerSettings.Validate()` 가 미연동을 경고 |
| **Steam 연동** | ⏳ 대기 | 계정 없음 — §4 |
| **Google Play 연동** | ⏳ 대기 | 계정 없음 — §5 |
| 게임 내 업적 목록 팝업 | ❌ 미착수 | 버튼 위치 미정 (TopBar 메뉴 / 일시정지 / 메인 패널 사이드 중) |
| 달성 알림 (토스트) | ❌ 미착수 | `AchievementData.OnUnlocked` 구독만 하면 된다 |
| 업적 아이콘 34종 | ❌ 미착수 | 512×512 로 굽고 Steam 용 64×64 축소 |
| 이름·설명 번역 (10개 언어) | ❌ 미착수 | `LocalizationTable` 에 추가 → 콘솔 입력에 재사용 |
| 보상 | — | 없음 (결정됨) |

⚠ **아직 유니티에서 실제 플레이로 검증하지 않았다.** 컴파일(`dotnet build`)만 확인했다.
Steam·Google 백엔드 파일은 SDK 가 없어서 **컴파일 대상에서 빠져 있다** — SDK 를 넣고 심볼을 켠 뒤 처음 컴파일된다.

---

## 2. 업적 목록 (34개)

- **API 이름** = `AchievementId` enum 이름. Steam 관리 화면의 "API Name" 에 그대로 넣는다.
- **숨김** = 달성 전 "???" 표시. 두 콘솔 모두 같은 값으로 등록할 것.
- ⚠ 번호는 세이브에 남는다 — 지운 번호를 재사용하지 말 것.

| 번호 | API 이름 | 이름 | 조건 | 숨김 |
|---|---|---|---|---|
| 1 | FirstStage | 첫 출정 | 1스테이지 클리어 | |
| 2 | FirstHurdle | 첫 관문 | 5스테이지 클리어 | |
| 3 | HalfWay | 반환점 | 15스테이지 클리어 | |
| 4 | JourneyEnd | 난세의 끝 | 30스테이지 클리어 (무한 보스 진입) | |
| 5 | NewTrial | 새로운 시련 | 새 난이도 해금 | |
| 6 | HellClear | 지옥 불 | 지옥 난이도로 30스테이지 클리어 | |
| 7 | InfernoClear | 초열 정복 | 불지옥 난이도로 30스테이지 클리어 | |
| 8 | FirstReincarnation | 첫 윤회 | 첫 환생 | |
| 101 | MiracleSurvivor | 기적의 생환 | 병사 단 1명만 살아남은 채 승리 | ✔ |
| 102 | LastGeneral | 최후의 장수 | 장수 2명 이상 편성, 단 1명만 살아남은 채 승리 | ✔ |
| 201 | KnightOrder | 기사단 | 장수 5명 전원 기사로 편성해 클리어 | |
| 202 | ArcherCorps | 궁병대 | 〃 궁수 | |
| 203 | MageCorps | 마도 군단 | 〃 마법사 | |
| 204 | ShieldWall | 철벽 방진 | 〃 방패병 | |
| 205 | LoneWolf | 외로운 늑대 | 병사 0명인 장수를 편성해 클리어 | ✔ |
| 301 | LegendaryGeneral | 전설의 장수 | 장수를 에픽으로 등급업 | |
| 321 | RareHeroBisect | 일도양단의 주인 | 해당 희귀 스킬 주인 장수 고용 | |
| 322 | RareHeroArrowStorm | 화살 폭풍의 주인 | 〃 | |
| 323 | RareHeroGravityCollapse | 중력 붕괴의 주인 | 〃 | |
| 324 | RareHeroBulwark | 불멸의 방벽의 주인 | 〃 | |
| 325 | RareHeroChainLightning | 연쇄 번개의 주인 | 〃 | |
| 326 | RareHeroDeathSentence | 사형 선고의 주인 | 〃 | |
| 327 | RareHeroBloodPrice | 피의 대가의 주인 | 〃 | |
| 328 | RareHeroPiercingDash | 관통 돌진의 주인 | 〃 | |
| 329 | RareHeroWarBanner | 군기 강림의 주인 | 〃 | |
| 330 | RareHeroGravestone | 비석 강림의 주인 | 〃 | |
| 401 | KnightMaster | 기사 달인 | 기사 달인 어빌리티 획득 | |
| 402 | ArcherMaster | 궁수 달인 | 궁수 달인 어빌리티 획득 | |
| 403 | MageMaster | 마법사 달인 | 마법사 달인 어빌리티 획득 | |
| 404 | ShieldMaster | 방패병 달인 | 방패병 달인 어빌리티 획득 | |
| 501 | Collector | 수집가 | 도감 한 분류 완성 | |
| 502 | CodexComplete | 만물 도감 | 도감 전 분류 완성 | |
| 503 | RelicRoot | 뿌리 내림 | 유물 첫 습득 | |
| 504 | RelicComplete | 유물의 정점 | 모든 유물 최대 레벨 | |

### 판정 규칙에서 알아둘 것
- **30스테이지에는 승리가 없다** (무한 보스). "30스테이지 클리어" = 웨이브를 다 깨고 무한 보스에 들어선 순간
  (`BattleManager.OnEndlessStarted`).
- 기적의 생환·최후의 장수는 **소환물(스켈레톤 등, `SummonedTag`)을 세지 않는다.**
- 직업 편성 업적은 장수 칸 5개가 모두 열려야 가능하다 (기본 2칸 + 유물).
- 희귀 장수는 `UnitData.AddUnit` 을 지나는 모든 획득(고용·상점·이벤트·시작 선택)을 센다.
- 여정 진행·편성·전투 업적은 소급하지 않는다 — "그 전투를 이겼다" 는 기록이 세이브에 없다.

---

## 3. 플랫폼 연동 구조 (이미 만들어 둔 것)

```
AchievementTracker ──Unlock──▶ AchievementData (로컬 정본, 세이브)
                                   │ OnUnlocked
                                   ▼
                         PlatformAchievements (다리)
                                   │ 빌드 타겟별 하나만 컴파일
          ┌────────────────────────┼─────────────────────────┐
  SteamAchievementBackend   GooglePlayAchievementBackend   NullAchievementBackend
  (PROJECTK_STEAM)          (PROJECTK_GPGS)                (기본 — 지금 이것)
```

| 파일 (`InGame/Achievement/Platform/`) | 역할 |
|---|---|
| `IPlatformAchievements.cs` | 인터페이스 + `NullAchievementBackend` |
| `PlatformAchievements.cs` | 백엔드 생성(BeforeSceneLoad), 달성 즉시 전송, 로비 시작 시 전체 재동기화, 개발 빌드 차단 |
| `SteamAchievementBackend.cs` | Steamworks.NET — `SetAchievement(enum이름)` + `StoreStats` |
| `GooglePlayAchievementBackend.cs` | GPGS v2 — 자동 로그인 후 `UnlockAchievement(콘솔ID)` |
| `GooglePlayAchievementIds.cs` | `AchievementId → Play Console ID` 매핑 표 (지금 전부 빈 칸) |

**동작 원칙**
- 로컬 세이브가 정본이다. 플랫폼은 전달받기만 한다.
- 로비 시작 때마다 로컬 달성분 **전체를 다시 보낸다** → 오프라인 달성·로그인 실패·소급 판정이 자동으로 메워진다.
- **개발 빌드·에디터는 전송하지 않는다** (로그만). 연동 테스트 때만 define `PROJECTK_PLATFORM_ACH_TEST` 를 넣는다.
  ⚠ 치트로 푼 업적이 실제 계정에 올라가면 되돌리기 어렵다 (Google 은 출시 후 초기화 불가).

---

## 4. Steam 연동 순서 (계정이 생기면)

1. **Steamworks 파트너 가입 + 앱 등록비($100)** → App ID 발급
2. **업적 등록** (Steamworks 관리 → Stats & Achievements)
   - API Name = §2 표의 "API 이름" 그대로 (대소문자 포함)
   - 표시 이름·설명 (언어별), Hidden, 아이콘 2장(달성/미달성, 64×64)
   - ⚠ **Publish 를 눌러야 반영된다** (가장 많이 놓치는 단계)
3. **Steamworks.NET 설치** — Package Manager → git URL
   `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net`
4. `SteamAchievementBackend.cs` 의 `AppId` 상수 채우기
5. 프로젝트 루트에 `steam_appid.txt` (내용 = App ID) — 개발 실행용. **버전 관리에는 넣지 말 것**
6. Player Settings → Standalone → Scripting Define Symbols 에 `PROJECTK_STEAM` 추가
   (Android 에는 넣지 않는다 — Steamworks.NET 은 PC 전용)
7. 테스트: 스팀 클라이언트를 켜고 `PROJECTK_PLATFORM_ACH_TEST` 를 잠시 추가해 에디터에서 확인
   → 확인 후 **반드시 제거** (Validate 가 경고한다)
   - 테스트로 푼 업적 초기화: `SteamUserStats.ResetAllStats(true)` 를 한 번 호출 (개발 중에만)

## 5. Google Play 연동 순서 (계정이 생기면)

1. **Play Console 개발자 계정($25)** + 앱 등록
2. **Play Games Services 설정** → 게임 프로젝트 생성, OAuth 사용자 인증 정보 연결
   - ⚠ **SHA-1 을 둘 다 등록**: Play 앱 서명 키 + 업로드 키. 빠지면 로그인이 조용히 실패한다
3. **업적 등록** (Play Games Services → 업적)
   - 이름·설명 (언어별), 아이콘 512×512, 숨김 여부
   - **점수**: 5점 단위, 게임 전체 합 **1,000점 이하** → 34개 배분표를 먼저 정할 것
   - 테스터 계정 추가 (출시 전에는 테스터에게만 보인다)
   - 출시 요건(최소 업적 수 등)은 그 시점 콘솔 안내를 확인할 것
4. **Google Play Games Plugin for Unity (v2 이상)** 설치
5. 콘솔 "리소스 가져오기" XML → `Window > Google Play Games > Setup > Android Setup` 에 붙여 넣기
6. 콘솔이 만든 ID(`CgkI...`)를 `GooglePlayAchievementIds.cs` 의 빈 칸에 채우기 (34줄)
7. Player Settings → Android → Scripting Define Symbols 에 `PROJECTK_GPGS` 추가
8. 테스트: 테스터 계정 기기 + `PROJECTK_PLATFORM_ACH_TEST` → 확인 후 **반드시 제거**

## 6. 업적을 추가할 때

1. `AchievementCatalog.cs` — enum 에 새 번호 + `Add(...)` 한 줄
2. `AchievementTracker.cs` — 판정 한 줄 (호출 지점 목록도 갱신)
3. `GooglePlayAchievementIds.cs` — 매핑 한 줄 (빈 칸이면 Validate 가 경고)
4. Steam·Google 콘솔에 같은 업적 등록 (Steam 은 Publish 까지)
5. 이 문서 §2 표에 한 줄

## 7. 릴리스 직전 체크

`ReleasePlayerSettings.Validate()` 가 아래를 경고한다:
- Standalone 에 `PROJECTK_STEAM` 없음
- Android 에 `PROJECTK_GPGS` 없음
- `GooglePlayAchievementIds.cs` 빈 칸 남음
- `PROJECTK_PLATFORM_ACH_TEST` 가 남아 있음
