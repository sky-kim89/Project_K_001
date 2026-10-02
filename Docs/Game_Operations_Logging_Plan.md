# Project K 게임 운영 지표 및 로그 설계 계획서

작성일: 2026-10-02  
대상: Unity 6 / ECS 기반 2D 오토배틀 로그라이트 `Project K`

---

## 1. 결론

Project K의 초기 운영에서 반드시 확인해야 할 것은 다음 7가지다.

1. 게임이 정상 실행되고 저장되는가
2. 신규 사용자가 튜토리얼과 첫 전투를 통과하는가
3. 사용자가 한 여정을 어디까지 진행하고 어디서 이탈하는가
4. 스테이지와 난이도가 의도한 승률·소요 시간을 보이는가
5. 장수·직업·스킬·특성·어빌리티·장비 조합이 편중되지 않는가
6. 골드·강화석·용병 조각·환생 포인트의 공급과 소비가 균형을 이루는가
7. 환생과 도감·유물 성장에 장기 플레이 동기가 생기는가

현재 프로젝트에는 `GameAnalytics 8.2.0` 패키지와 설정 에셋이 있지만, 게임 코드에서 운영 이벤트를 보내는 연결부는 확인되지 않았다. 반면 `BattleStatsTracker`, `StageProgressData`, `ItemData` 등 분석에 필요한 원천 데이터는 이미 상당 부분 존재한다.

따라서 첫 구현은 새로운 대규모 분석 시스템보다 다음 구조가 적합하다.

- 게임 코드에서는 공급자와 무관한 `Telemetry` 파사드만 호출한다.
- `GameAnalytics` 어댑터가 실제 SDK 이벤트로 변환한다.
- 타격·프레임 단위 로그는 전송하지 않는다.
- 전투 중에는 메모리에 집계하고 `battle_end`에서 한 번 전송한다.
- 모든 주요 이벤트는 `session_id`, `run_id`, `battle_id`로 연결한다.
- 재화 변화는 반드시 `reason`과 변화 전후 잔액을 기록한다.

---

## 2. 현재 프로젝트 진단

### 이미 활용할 수 있는 기반

| 기반 | 현재 위치 | 활용 방법 |
|---|---|---|
| 분석 SDK | `Packages/manifest.json`, `Assets/Resources/GameAnalytics/Settings.asset` | 실제 운영 이벤트 전송 어댑터로 사용 |
| 전투 시작·종료 | `InGame/Battle/BattleManager.cs`, `InGame/Battle/InGameManager.cs` | `battle_start`, `battle_end`, `endless_start`의 정본 |
| 전투 통계 | `InGame/Battle/BattleStatsTracker.cs` | 장수별 피해·피격·흡수·처치·회복을 종료 시 집계 전송 |
| 여정 시작 | `Lobby/RunStarter.cs:71` | `run_start`의 정본 |
| 여정 진행 | `Data/Sections/StageProgressData.cs` | 현재 런 스테이지, 스테이지 타입, 클리어 기록 |
| 재화 | `Data/Item/ItemData.cs` | 보유량과 실제 획득·소비 성공 여부 |
| 환생 | `Data/Core/UserDataManager.cs:134` | `reincarnation`의 정본 |
| 이벤트 선택 | `UI/Popup/EventPopup.cs:193` | 노출·선택·성공·실패 분석 |
| 런 상점 | `UI/Popup/RunShopPopup.cs` | 상품 노출·구매·새로고침 분석 |
| 용병 상점 | `UI/Popup/MercenaryShopPopup.cs` | 후보 노출·고용·분해 분석 |
| 튜토리얼 | `Tutorial/TutorialManager.cs` | 시작·단계 진행·완료·건너뛰기 |
| 도감 | `Data/Sections/CodexData.cs` | 최초 획득과 여정별 수확량 |
| 난이도 | `Data/Sections/DifficultyData.cs` | 선택·해금·난이도별 성과 |

### 현재 부족한 부분

- `Debug.Log`와 전투 결과 문자열은 개발 진단용이며 사용자·세션·여정 간 연결이 불가능하다.
- 재화 `ItemData.Add/Spend`에는 변화 사유가 없어 현재 상태 그대로 중앙 로깅하면 “얼마나 변했는지”만 알고 “왜 변했는지”는 알 수 없다.
- 앱 강제 종료나 백그라운드 이탈 시 열린 여정·전투가 종료 이벤트 없이 남을 수 있다.
- 콘텐츠는 선택 결과뿐 아니라 “무엇이 제시되었는지”도 기록해야 선택률을 계산할 수 있다.
- 저장 성공·실패, 예외, 로딩 시간, 프레임 성능을 구조화해 수집하는 경로가 없다.
- 현재 코드 검색 기준 IAP·광고 구현은 없다. 매출 지표는 실제 기능 도입 전까지 범위에서 제외한다.

---

## 3. 운영에 필요한 정보 목록

## 3.1 서비스 안정성

운영 질문:

- 버전별 실행 성공률과 크래시 없는 사용자 비율은 얼마인가?
- Splash에서 Lobby, Lobby에서 전투 진입까지 얼마나 걸리는가?
- 저장 로드·저장·마이그레이션 실패가 발생하는가?
- 특정 OS·기기 등급에서 프레임 저하나 메모리 문제가 집중되는가?

필요 지표:

- 앱 실행 수, 세션 수, 세션 길이
- 정상 종료·백그라운드·강제 종료 추정 비율
- crash-free users / sessions
- 초기화·씬 준비·전투 준비 시간의 p50, p95
- 저장 로드·저장 성공률과 실패 섹션
- 기기 등급별 평균 FPS, 저프레임 비율, 메모리 경고 횟수

## 3.2 유입·활성·리텐션

운영 질문:

- DAU/WAU/MAU와 신규·복귀 사용자 비율은 어떻게 변하는가?
- D1/D7/D30 리텐션은 얼마인가?
- 한 사용자가 하루에 몇 번, 얼마나 오래 플레이하는가?

필요 지표:

- 익명 설치 ID 기준 활성 사용자
- 첫 실행 일자, 마지막 실행 일자
- 세션 수와 세션 길이
- 일별 신규·복귀·휴면 복귀 사용자
- 코호트별 D1/D7/D30 재방문

주의: 리텐션은 클라이언트 화면에서 즉석 계산하지 않고 수집 서버의 UTC 날짜 기준으로 계산한다.

## 3.3 온보딩·튜토리얼 퍼널

운영 질문:

- 신규 사용자가 어느 단계에서 가장 많이 이탈하는가?
- 튜토리얼을 완료하는 데 얼마나 걸리는가?
- 건너뛴 사용자와 완료 사용자의 첫 전투 성과가 다른가?

권장 퍼널:

`first_open → tutorial_start → hero_selected → run_start → first_battle_start → first_battle_end → tutorial_complete`

필요 지표:

- 튜토리얼 ID·단계별 진입, 완료, 체류 시간
- 강제/선택 튜토리얼 여부
- skip/abort와 당시 화면·단계
- 첫 장수, 첫 직업, 첫 전투 결과와 소요 시간

## 3.4 여정 진행과 이탈

운영 질문:

- 여정 시작 대비 환생·완주·중도 이탈 비율은 얼마인가?
- 어느 스테이지 타입에서 진행이 멈추는가?
- 재접속 후 이어하기 비율은 얼마인가?

필요 지표:

- 여정 시작·이어하기·종료·환생
- 최고 도달 스테이지와 마지막 스테이지 타입
- 여정 총 플레이 시간, 전투 수, 승·패 수
- 일반/엘리트/상점/이벤트 노드 진입·완료율
- 다음 실행에서 열린 여정이 남아 있으면 `abandoned` 또는 `resumed`로 복구 판정

## 3.5 전투와 난이도 밸런스

운영 질문:

- 스테이지·난이도별 승률과 전투 시간은 적절한가?
- 특정 웨이브·보스에서 실패가 집중되는가?
- 장수·직업·빌드별 성능 차이가 과도한가?
- 무한 보스 진입률과 보스 처치 분포는 어떠한가?

필요 지표:

- 전투 시작·승리·패배·중단
- 난이도, 스테이지 번호·타입, 총 웨이브, 도달 웨이브
- 전투 시간, 적 처치 수, 생존 장수·병사 수
- 장수별 일반 공격 피해, 병사 피해, 스킬 피해, 받은 피해, 흡수량, 처치, 회복량
- 장수 직업·등급·레벨·병사 수·최종 계산 스탯
- 장착 장비·강화 수치, 액티브·패시브, 어빌리티, 특성, 적용 유물/도감 수준
- 무한 보스 진입과 최종 처치 보스 인덱스

승률은 전체 평균만 보면 안 된다. 최소한 `difficulty + stage + account_age_bucket + power_bucket`으로 나눠야 한다.

## 3.6 콘텐츠 선택과 조합 효율

운영 질문:

- 어떤 콘텐츠가 자주 제시되고, 선택되고, 실제로 승리에 기여하는가?
- 사용되지 않는 스킬·특성·어빌리티·장비가 있는가?
- 특정 조합이 사실상 필수 선택이 되었는가?

필요 지표:

- 장수/어빌리티/특성/장비/상점 상품의 노출 횟수
- 노출 대비 선택률, 구매율, 장착률
- 선택 후 다음 1·3·5 전투 승률
- 콘텐츠별 사용 여정 수, 최고 도달 스테이지, 평균 전투 기여도
- 선택 당시 대안 목록과 선택한 항목

핵심 원칙: `content_selected`만 기록하면 선택률을 계산할 수 없다. 동일한 `offer_id`로 `content_offer`를 먼저 기록해야 한다.

## 3.7 경제 밸런스

운영 질문:

- 재화별 공급원과 소비처 비중은 어떠한가?
- 사용자가 재화 부족으로 어느 행동을 포기하는가?
- 잔액이 계속 쌓이거나 특정 지점에서 고갈되는가?
- 상점 새로고침과 강화 비용 곡선이 의도대로 작동하는가?

대상 재화:

- 골드
- 잼
- 에너지
- 스태미나
- 장비 강화석
- 용병 조각
- 환생 포인트
- 그 밖의 `eItem` 항목은 동일한 규칙으로 확장

필요 지표:

- 재화별 source/sink 금액과 발생 건수
- 변화 전·후 잔액
- 획득·소비 사유와 연관 콘텐츠 ID
- 구매·강화 실패 시 필요량과 부족량
- 스테이지·환생 횟수별 잔액 분포
- 아이템·장비 획득, 장착, 교체, 강화, 분해

## 3.8 메타 성장과 장기 목표

운영 질문:

- 사용자가 언제 처음 환생하고 이후 주기는 어떻게 변하는가?
- 환생 포인트를 어떤 유물에 투자하는가?
- 도감 수집과 영구 성장 속도가 적절한가?
- 난이도 해금 후 실제로 상위 난이도를 시도하는가?

필요 지표:

- 환생 시점, 도달 스테이지, 획득 포인트, 보유 포인트, 누적 환생 수
- 유물 노드별 해금·레벨업·비용
- 도감 카테고리별 신규 등록과 총 수집률
- 업적 달성
- 난이도 선택·해금·첫 시도·첫 클리어

## 3.9 이벤트·상점·용병 UX

운영 질문:

- 이벤트 선택지가 공평하게 노출되고 선택되는가?
- 확률 선택의 성공·실패 체감이 실제 확률과 일치하는가?
- 상점에서 구매하지 않는 이유가 가격인지 제안 품질인지 알 수 있는가?
- 용병 고용과 분해 중 어떤 행동이 우세한가?

필요 지표:

- 이벤트 노출, 선택지 목록, 선택, 성공/실패, 실제 지급 결과
- 상점 진입, 상품 목록, 가격, 구매, 새로고침, 잔액 부족
- 용병 후보 목록, 가격, 고용, 해고/분해, 패스

## 3.10 설정·알림

운영 질문:

- 언어와 사운드 설정 분포는 어떠한가?
- 알림 권한과 복귀 효과가 있는가?

필요 지표:

- 언어 변경, BGM/SFX 음소거 및 볼륨 구간
- 알림 권한 결과, 예약 성공, 알림을 통한 앱 실행

모든 일반 버튼 클릭을 기록하지 않는다. 운영 의사결정과 직접 연결되는 설정·퍼널 행동만 기록한다.

---

## 4. 이벤트 명세

## 4.1 공통 필드

모든 이벤트에 가능한 한 아래 문맥을 자동 부착한다.

| 필드 | 의미 | 비고 |
|---|---|---|
| `schema_version` | 로그 스키마 버전 | 처음은 `1` |
| `event_id` | 이벤트 고유 ID | 중복 제거용 UUID |
| `occurred_at_utc` | 발생 시각 | UTC ISO-8601 |
| `environment` | `dev`, `qa`, `prod` | 개발 데이터 오염 방지 |
| `app_version` | 앱 표시 버전 | `Application.version` 사용 |
| `build_number` | Android/iOS/PC 빌드 번호 | 릴리스별 회귀 추적 |
| `platform` | 플랫폼 | Android, Windows 등 |
| `language` | 현재 언어 | 번역·UX 분석 |
| `install_id` | 익명 설치 식별자 | 이름·광고 ID 등 개인정보 금지 |
| `session_id` | 앱 활성 세션 ID | 앱 포그라운드 단위 |
| `run_id` | 현재 여정 ID | 여정 시작 때 생성·저장 |
| `battle_id` | 현재 전투 ID | 전투 시작 때 생성 |
| `difficulty` | 선택 난이도 | 해당 시점 값 |
| `run_stage_index` | 여정 내 노드 인덱스 | 0-based임을 문서에 고정 |
| `stage_type` | normal/elite/shop/event | 비전투 노드 포함 |

`run_id`는 앱 종료 후 이어하기에서도 유지되어야 하므로 `StageProgressData`와 함께 저장하는 것이 안전하다. `battle_id`는 전투마다 새로 만들고, 시작된 전투의 종료 여부도 저장해 다음 실행 시 비정상 종료를 복구 판정한다.

## 4.2 P0 — 출시 전 필수 이벤트

| 이벤트 | 발생 시점 | 핵심 속성 | 권장 연결 지점 |
|---|---|---|---|
| `app_open` | 앱 초기화 완료 | first_open, install_age_days | `Splash/SplashBootstrap.cs` |
| `session_start` | 포그라운드 세션 시작 | entry_reason, previous_session_end | 신규 수명주기 브리지 |
| `session_end` | 정상 종료/백그라운드 | duration_sec, end_reason | 신규 수명주기 브리지 |
| `save_result` | 전체/섹션 로드·저장 후 | operation, success, section, duration_ms, error_code | `Data/Core/UserDataManager.cs` |
| `run_start` | 여정 데이터 확정 후 | hero_id, job, difficulty, start_gold, codex_applied_count | `Lobby/RunStarter.cs:71` |
| `run_resume` | 저장된 여정으로 로비 진입 | stage_index, offline_sec | `Lobby/LobbyManager.cs` |
| `run_end` | 환생·완주·포기·복구 이탈 | reason, max_stage, duration_sec, battles, wins, losses | 환생/포기 처리 정본 |
| `battle_start` | 실제 웨이브 시작 | stage, wave_count, squad_snapshot, power_bucket | `BattleManager.OnWavesStarted` |
| `battle_end` | 승리·패배·중단 확정 | result, duration_sec, reached_wave, kills, squad_survivors | `InGameManager.HandleVictory/HandleDefeat` |
| `battle_member_result` | 전투 종료 시 장수별 1건 | general_id, damage_*, damage_taken, absorbed, kills, healing | `BattleStatsTracker.GetAllEntries()` |
| `endless_start` | 최종 스테이지 무한 보스 진입 | stage, elapsed_sec | `BattleManager.OnEndlessStarted` |
| `economy_flow` | 재화 변화 성공 직후 | flow, currency, amount, balance_before, balance_after, reason, content_id | `ItemData`를 통한 공통 경로 |
| `reward_granted` | 보상 확정 후 | reward_type, reward_id, amount, source, source_id | 보상 지급 공통 경로 |
| `tutorial_progress` | 시작·단계·완료·스킵 | tutorial_id, step, status, elapsed_sec | `TutorialManager.cs` |
| `client_error` | 처리 가능한 중요 오류 | error_code, system, stage, dedupe_key | 중앙 오류 수집기 |

### `battle_end` 스쿼드 스냅샷

한 이벤트에 모든 상세 정보를 문자열로 무제한 결합하지 않는다.

- `battle_end`: 전투 전체 결과와 요약
- `battle_member_result`: 배치 장수 1명당 1건
- 장비·스킬 목록은 정렬된 ID 목록 또는 안정적인 `build_hash`로 기록
- 상세 빌드가 필요하면 `battle_build_member`를 별도 이벤트로 제한적으로 전송

전투 중 발생하는 모든 타격을 원격 로그로 보내지 않는다. 기존 `BattleStatsTracker`가 메모리에서 집계한 값을 전투 종료 시점에만 전송한다.

## 4.3 P1 — 밸런스 운영 필수 이벤트

| 이벤트 | 핵심 속성 | 연결 지점 |
|---|---|---|
| `content_offer` | offer_id, content_type, candidate_ids, prices, reroll_index | 어빌리티 선택·런 상점·용병 후보 생성 직후 |
| `content_selected` | offer_id, content_type, selected_id, slot, price | 선택/구매 성공 후 |
| `content_declined` | offer_id, content_type, reason | 패스·팝업 종료 시 |
| `purchase_failed` | content_type, content_id, currency, price, balance, reason | 잔액/슬롯 부족으로 실패 시 |
| `shop_refresh` | shop_type, cost, balance_after, refresh_index | `RunShopPopup.OnRefresh` |
| `event_exposed` | event_id, choice_ids, success_rates | `EventPopup` 내용 표시 완료 시 |
| `event_choice` | event_id, choice_id, success, granted_ids, cost | `EventPopup.OnChoiceSelected` |
| `equipment_changed` | general_id, slot, before_id, after_id | `EquipComparePopup.OnEquipClick` |
| `equipment_enhanced` | equipment_id, before_level, after_level, cost | 장비 강화 성공 지점 |
| `equipment_disassembled` | equipment_id, grade, item_level, reward_amount, batch_count | 두 분해 UI의 성공 지점 |
| `mercenary_action` | action, general_id, job, grade, cost_or_reward | 고용·해고·분해·패스 |
| `progression_upgrade` | system, target_id, before_level, after_level, cost | 장수/병사/유물 성장 성공 후 |
| `codex_discovered` | category, content_id, total_collected | `CodexData`에서 최초 등록 성공 시 |
| `difficulty_changed` | before, after, max_unlocked | `DifficultyData.Select` 성공 후 |
| `difficulty_unlocked` | tier, reached_stage | `InGameManager.TryUnlockNextDifficulty` |
| `reincarnation` | reached_stage, points_earned, points_before, points_after, total_count | `UserDataManager.Reincarnate` |
| `achievement_unlocked` | achievement_id, platform_sync_state | 업적 정본 |

## 4.4 P2 — 최적화·UX 진단 이벤트

| 이벤트 | 핵심 속성 | 전송 정책 |
|---|---|---|
| `loading_measure` | phase, duration_ms, success | 주요 전환만 100% |
| `performance_sample` | scene, avg_fps, low_frame_ratio, memory_mb, device_tier | 30~60초 집계, 사용자 10~20% 샘플링 |
| `low_memory` | scene, stage, memory_mb | 100%, 세션 내 횟수 제한 |
| `notification_result` | action, permission, schedule_count | 변경·오픈 시만 |
| `settings_changed` | setting, before_bucket, after_bucket | 중요 설정만 |

---

## 5. 재화 로그 설계

`ItemData.Add/Spend`에 단순히 로그를 넣는 것만으로는 부족하다. 호출 지점의 문맥이 사라지기 때문이다. 다음과 같이 사유를 강제하는 경로가 필요하다.

```csharp
public enum EconomyReason
{
    StageClear,
    EventReward,
    EventCost,
    RunShopPurchase,
    RunShopRefresh,
    MercenaryHire,
    EquipmentEnhance,
    EquipmentDisassemble,
    SoldierUpgrade,
    ReincarnationReward,
    RelicUpgrade,
    StartBonus,
    Cheat,
}
```

권장 API 개념:

```csharp
items.Add(item, amount, EconomyReason.StageClear, contentId);
items.Spend(item, amount, EconomyReason.RunShopPurchase, contentId);
```

구현 원칙:

- 실제 상태 변경이 성공한 뒤에만 `economy_flow`를 전송한다.
- `amount`는 항상 양수이며 `flow`가 `source` 또는 `sink`를 구분한다.
- `balance_before`, `balance_after`를 함께 보내 데이터 이상을 검증한다.
- 치트·개발자 보상은 `environment` 또는 `reason=Cheat`로 운영 데이터에서 제외한다.
- `AddBatch`도 항목별 이벤트를 만들되 동일한 `transaction_id`로 묶는다.
- 환생 포인트는 실제 정본인 `ReincarnationData`의 변화 전후 값을 사용한다.
- 부족으로 실패한 소비는 `economy_flow`가 아니라 `purchase_failed`로 보낸다.

---

## 6. 구현 구조

## 6.1 권장 파일 구성

```text
Assets/_project/1.Script/Analytics/
├── Telemetry.cs                 # 게임 코드가 호출하는 타입 안전 파사드
├── TelemetryContext.cs          # 공통 필드·세션/run/battle ID 관리
├── TelemetryEvent.cs            # 내부 이벤트 DTO
├── ITelemetrySink.cs            # 전송 대상 인터페이스
├── GameAnalyticsSink.cs         # GA SDK 매핑
├── DebugTelemetrySink.cs        # 에디터 검증용 구조화 로그
├── TelemetryLifecycle.cs        # pause/focus/quit/lowMemory 처리
└── TelemetrySchema.cs           # 이벤트명·필수 필드 상수
```

문자열 이벤트 이름을 게임 곳곳에서 직접 만들지 않는다. 예를 들어 게임 코드는 `Telemetry.BattleEnded(summary)`처럼 타입이 있는 메서드를 호출하고, 실제 SDK 형식은 Sink만 안다.

## 6.2 GameAnalytics 매핑

- 세션은 SDK 기본 세션 수집과 내부 `session_id`를 함께 사용한다.
- 스테이지 시작·완료·실패는 Progression 이벤트에 매핑한다.
- 골드·강화석·조각·환생 포인트는 Resource 이벤트에 매핑한다.
- 선택·상점·이벤트·튜토리얼은 Design 이벤트에 매핑한다.
- 오류는 Error 이벤트에 매핑한다.
- 프로젝트 내부 표준 이벤트는 공급자 중립으로 유지해 향후 다른 분석 도구를 병행해도 게임 코드는 바꾸지 않는다.

SDK 버전별 호출 시그니처와 커스텀 필드 지원 범위는 실제 구현 시 설치된 8.2.0 API를 기준으로 확정한다.

## 6.3 성능 원칙

- ECS Job/Burst 코드 안에서 SDK를 호출하지 않는다.
- Managed 진입점에서만 이벤트를 만들고 전송한다.
- 매 프레임·매 타격·매 발사체 로그는 금지한다.
- 전투 상세 수치는 기존 `BattleStatsTracker`에 누적한다.
- JSON 직렬화와 문자열 목록 생성은 이벤트 종료 시 한 번만 수행한다.
- SDK가 제공하는 오프라인 큐를 우선 사용하고 별도 큐는 실제 유실이 확인될 때만 추가한다.
- 비즈니스 이벤트는 100%, 성능 표본은 10~20%만 전송한다.
- 동일 오류는 `dedupe_key` 기준으로 세션당 전송 횟수를 제한한다.

## 6.4 생명주기와 유실 복구

- `session_start`: 앱이 포그라운드에서 플레이 가능한 상태가 된 시점
- `session_end`: 정상 종료와 장시간 백그라운드 진입 시점
- 전투 시작 시 열린 전투 마커를 로컬 저장한다.
- 정상 `battle_end`에서 마커를 제거한다.
- 다음 실행에 마커가 남아 있으면 `battle_end(result=abandoned, inferred=true)`를 1회 생성한다.
- 같은 방식으로 열린 여정은 `run_resume` 또는 `run_end(reason=abandoned)`로 정합성을 맞춘다.
- `event_id`와 종료 마커를 이용해 중복 전송을 방지한다.

---

## 7. 코드 삽입 지점

| 우선순위 | 파일/메서드 | 심을 로그 | 비고 |
|---|---|---|---|
| P0 | `Splash/SplashBootstrap.cs` | app_open, 초기화 loading_measure | 분석 SDK 초기화도 이 구간 |
| P0 | `Data/Core/UserDataManager.LoadAll/SaveAll/SaveSection` | save_result | JSON 내용 자체는 전송 금지 |
| P0 | `Lobby/RunStarter.BeginRun` | run_start | 데이터 확정·저장 직전에 1회 |
| P0 | `Lobby/LobbyManager`의 이어하기 분기 | run_resume | 기존 RunInProgress 사용 |
| P0 | `BattleManager.OnWavesStarted` 시점 | battle_start | 대기 화면 시간 제외 |
| P0 | `InGameManager.HandleVictory/HandleDefeat` | battle_end, battle_member_result | 통계 스냅샷 뒤, context 파기 전 |
| P0 | `Data/Core/UserDataManager.Reincarnate` | run_end, reincarnation | 초기화 전에 before 값을 캡처 |
| P0 | `Data/Item/ItemData.Add/Spend` | economy_flow | reason 파라미터 도입 필요 |
| P0 | `Tutorial/TutorialManager` | tutorial_progress | TryPlay/단계/완료/Skip/Abort |
| P1 | `UI/Popup/AbilitySelectPopup.cs` | content_offer/selected | 노출 후보 전체 필요 |
| P1 | `UI/Popup/RunShopPopup.cs` | offer/purchase/refresh/failure | 장비·특성·장수 구분 |
| P1 | `UI/Popup/MercenaryShopPopup.cs` | offer/hire/decompose/pass | 후보와 실제 행동 연결 |
| P1 | `UI/Popup/EventPopup.cs` | event_exposed/event_choice | 성공 판정과 지급 결과 포함 |
| P1 | `UI/Lobby/EquipComparePopup.cs` | equipment_changed/disassembled | 실제 변경 성공 후 |
| P1 | `UI/Popup/DisassemblePopup.cs` | equipment_disassembled | 일괄 분해 transaction_id 필요 |
| P1 | `Data/Sections/CodexData.cs` | codex_discovered | HashSet에 새로 추가된 경우만 |
| P1 | `Data/Sections/DifficultyData.Select` | difficulty_changed | 실제 값이 변한 경우만 |
| P1 | `InGameManager.TryUnlockNextDifficulty` | difficulty_unlocked | 해금이 실제 발생한 경우만 |
| P2 | 신규 `TelemetryLifecycle` | session, low_memory, performance | DontDestroyOnLoad 또는 상주 부트스트랩 |

`battle_end`는 `BattleManager.LogBattleStats`의 콘솔 출력과 별개로 다룬다. 실제 전송은 `InGameManager`가 `BattleElapsedSeconds`와 `CombatStats`를 확정한 뒤 수행해야 한다.

---

## 8. 데이터 품질 규칙

1. 이벤트 이름과 속성 키는 영문 `snake_case`로 고정한다.
2. ID 필드에는 번역된 표시 이름이 아니라 enum 값 또는 안정적인 콘텐츠 ID를 보낸다.
3. 이벤트 이름에 장수명·스테이지 번호를 붙이지 않는다. 값은 속성으로 분리한다.
4. 자유 입력 문자열, 사용자 이름, 원문 저장 JSON, 광고 ID는 수집하지 않는다.
5. 전투 시작 1건에는 종료 이벤트가 최대 1건만 대응해야 한다.
6. `economy_flow`의 `balance_after - balance_before`는 source면 `amount`, sink면 `-amount`여야 한다.
7. 선택 이벤트는 반드시 존재하는 `offer_id`를 참조해야 한다.
8. 개발·QA·운영 환경 데이터를 대시보드에서 섞지 않는다.
9. 시간은 UTC, 소요 시간은 가능하면 monotonic time으로 계산한다.
10. 스키마 변경 시 `schema_version`을 올리고 기존 필드 의미를 조용히 바꾸지 않는다.

운영 빌드에서는 로그 실패가 게임 진행을 막아서는 안 된다. 단, 에디터와 QA 빌드에서는 필수 필드 누락을 명확한 오류로 드러내야 한다.

---

## 9. 대시보드와 경보

## 9.1 출시 대시보드

1. 서비스 건강도
   - DAU, 세션, 평균 세션 길이
   - crash-free users/sessions
   - 저장 실패율
   - 로딩 p50/p95

2. 신규 사용자 퍼널
   - first_open → 튜토리얼 시작 → 여정 시작 → 첫 전투 시작 → 첫 승리/패배 → 튜토리얼 완료
   - 각 단계 전환율과 소요 시간

3. 여정 퍼널
   - 스테이지 인덱스별 도달 사용자
   - 타입별 진입·완료율
   - 이어하기·환생·이탈 비율

4. 전투 밸런스
   - 난이도·스테이지별 승률, 중앙 전투 시간, p90 전투 시간
   - 직업·장수·파워 구간별 승률
   - 실패 집중 웨이브와 무한 보스 도달 분포

5. 경제
   - 재화별 일간 source/sink
   - 사유별 비중과 사용자 잔액 분포
   - 구매 실패율과 부족량

6. 콘텐츠
   - 노출 대비 선택률
   - 선택 후 성과
   - 지나치게 낮은/높은 픽률과 승률

7. 메타 성장
   - 첫 환생까지 걸린 시간·세션·스테이지
   - 환생 주기와 포인트 사용처
   - 유물·도감·난이도 해금 진척

## 9.2 초기 경보 기준

실측 기준선이 생기기 전에는 절대 수치보다 직전 안정 버전 대비 변화율로 경보를 잡는다.

- 새 버전 crash-free sessions가 이전 버전보다 1%p 이상 하락
- 저장 실패율 0.5% 초과
- Splash → Lobby 성공률 98% 미만
- 첫 전투 시작 전환율이 이전 버전보다 10% 이상 하락
- 특정 스테이지 승률이 이전 버전보다 15%p 이상 변동
- 재화 balance invariant 위반 발생
- battle_start 대비 terminal battle_end 누락률 2% 초과

---

## 10. 단계별 실행 계획

### 0단계 — 스키마 확정 및 개발 데이터 분리

- 이벤트·필드 이름과 enum 값을 확정한다.
- dev/qa/prod 빌드 구분을 만든다.
- GameAnalytics 프로젝트도 가능하면 환경별로 분리한다.
- 개인정보·보존 기간·동의 정책을 릴리스 대상 국가 기준으로 확정한다.

완료 조건: 이벤트 명세가 코드 상수와 문서에서 1:1로 일치한다.

### 1단계 — P0 최소 운영 로그

- Telemetry 파사드와 GameAnalytics Sink를 만든다.
- app/session/save/run/battle/tutorial/economy/reincarnation을 연결한다.
- 전투 중에는 기존 집계기를 재사용하고 종료 시만 전송한다.
- 에디터 Debug Sink로 페이로드를 검증한다.

완료 조건: 한 신규 사용자의 첫 실행부터 첫 환생까지 이벤트가 ID로 끊김 없이 연결된다.

### 2단계 — 콘텐츠·상점·이벤트 분석

- offer/selected 쌍을 어빌리티·특성·장비·장수에 적용한다.
- 상점, 용병, 장비, 이벤트 선택 로그를 연결한다.
- 도감·난이도·업적 로그를 연결한다.

완료 조건: 각 콘텐츠의 노출 대비 선택률과 선택 후 승률을 계산할 수 있다.

### 3단계 — 성능과 데이터 품질

- 주요 로딩 구간을 계측한다.
- 기기 등급과 저프레임 표본을 추가한다.
- 열린 전투/여정의 비정상 종료 복구를 추가한다.
- 중복·필수 필드·잔액 invariant 자동 검사를 만든다.

완료 조건: 버전별 성능 회귀와 이벤트 누락을 대시보드에서 탐지할 수 있다.

### 4단계 — 운영 루틴

- 매일: 크래시, 저장 실패, 첫 전투 퍼널, 이상 재화 확인
- 매주: 스테이지 승률, 콘텐츠 픽률, 경제 source/sink, 환생 주기 검토
- 릴리스 전후: 버전 코호트 비교, 이벤트 스키마 검증, QA 테스트 계정 제외 확인

---

## 11. QA 검증 체크리스트

- 신규 설치 → 첫 여정 → 승리 → 상점 → 이벤트 → 패배 → 환생 흐름을 한 번 완주한다.
- 모든 이벤트의 `session_id`, `run_id`, `battle_id` 연결을 확인한다.
- 동일 버튼을 빠르게 여러 번 눌러도 구매·보상이 한 번만 기록되는지 확인한다.
- 앱을 전투 중 강제 종료한 뒤 다음 실행에서 abandoned 전투가 한 번만 보정되는지 확인한다.
- 오프라인 플레이 후 재접속했을 때 이벤트 순서와 중복 여부를 확인한다.
- 재화 source/sink의 전후 잔액과 실제 세이브 값을 대조한다.
- 개발 치트 데이터가 prod 대시보드에 들어오지 않는지 확인한다.
- 저사양 기기에서 계측 전후 프레임과 GC Alloc 차이를 비교한다.
- 다국어 환경에서도 표시 문자열이 아닌 안정적인 콘텐츠 ID가 전송되는지 확인한다.
- 이벤트 전송 실패가 저장·전투·보상 지급을 막지 않는지 확인한다.

---

## 12. 추천 우선순위 요약

가장 먼저 심을 로그는 다음 10개다.

1. `app_open`
2. `session_start`, `session_end`
3. `run_start`, `run_resume`, `run_end`
4. `battle_start`, `battle_end`
5. `battle_member_result`
6. `economy_flow`
7. `reward_granted`
8. `tutorial_progress`
9. `reincarnation`
10. `save_result`, `client_error`

이 집합만 먼저 적용해도 안정성, 신규 사용자 퍼널, 여정 이탈, 전투 밸런스, 경제 흐름, 환생 주기를 운영할 수 있다. 이후 `content_offer/content_selected`를 추가하면 스킬·특성·장비·장수 밸런스까지 분석 범위가 확장된다.
