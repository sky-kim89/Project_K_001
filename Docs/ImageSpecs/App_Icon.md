# 이미지 작업 지시서 — 앱 아이콘 (Pixel General)

대상: 이미지를 만드는 에이전트(Codex) · 작성일 2026-10-02

**할 일: 스토어·홈 화면에 뜨는 게임 아이콘 1장을 도트로 새로 그려 `Assets/Resources/Icon/Icon.png` 를 교체한다.**
같은 경로·같은 파일명으로 **PNG 바이트만** 덮어쓴다. `.meta` 는 건드리지 않는다
(PlayerSettings 가 그 GUID 로 아이콘을 물고 있다 — 지웠다 만들면 빌드 아이콘이 빈다).

---

## 1. 지금 아이콘의 문제 (되풀이 금지)

| 문제 | 왜 안 되나 |
|---|---|
| 제목이 "픽셀 히어로 / PIXEL WARS" | 게임 이름은 **Pixel General** 이다. 그림 안에 제목을 쓰지 않는 것으로 해결한다 |
| 오른쪽 아래 반짝이(✦) 워터마크 | 스토어 심사·인상 모두 감점 |
| 둥근 모서리 액자를 그림에 구웠다 | 스토어·OS 가 자기 모양(둥근 사각·원)으로 또 깎는다 → 액자가 이중으로 잘린다 |
| 글자가 화면의 1/3 | 홈 화면 48px 에서 글자는 회색 얼룩이다. 그 자리를 주인공이 써야 한다 |
| 영웅 vs 오크 1:1 결투 | 우리 게임의 핵심은 **장수 한 명이 병사 대군을 지휘하는 오토배틀**이다. 1:1 결투는 액션 게임으로 읽힌다 |

---

## 2. 무엇을 그리나 — 콘셉트

**"군기를 든 장수가 칼을 치켜들고, 뒤로 병사 대열이 끝없이 이어진다."**

- **주인공 = 장수 한 명.** 화면 아래 2/3 를 차지하는 상반신~무릎 구도, 정면에서 살짝 비스듬히(3/4).
  - 은빛 판금 갑옷 + **금테**, **왕실 파랑 망토**가 바람에 날린다. 투구는 쓰지 않거나 얼굴이 보이는 열린 투구 — 표정(결의)이 보여야 사람이 끌린다.
  - 오른손에 장검을 하늘로 치켜든다 (지휘·돌격 신호). 칼끝에 작은 금빛 번쩍임 하나.
  - 게임 속 장수(PixelFantasy 도트 히어로)와 같은 **2~3등신 귀여운 비율** — 리얼 비율 금지.
- **배경 = 병사 대군.** 장수 뒤 좌우로 창·방패를 든 병사들이 **작은 실루엣으로 빽빽이** 늘어서고, 대열 사이로 파란 군기 2~3개.
  "수백 명이 같이 싸운다" 가 한눈에 보여야 한다 — 이게 다른 오토배틀 아이콘과 구별되는 지점이다.
- **하늘 = 짙은 남색 밤하늘 → 지평선 쪽 황혼빛.** 로비 배경(`3.Textures/UI/Splash/background_pixel_general.png`)의 야영지 밤 분위기와 같게.
- **상단 장식(선택):** 장수 머리 위에 작은 금 왕관 문장 — 로고(`title_pixel_general_splash.png`)의 왕관과 같은 모양. 크게 하지 않는다.

**적(오크·몬스터)은 넣지 않는다.** 넣으면 1:1 결투 구도로 돌아간다.

---

## 3. 규격

| 항목 | 값 |
|---|---|
| 최종 크기 | **1024×1024** RGBA PNG (모든 플랫폼 아이콘을 유니티가 여기서 줄인다) |
| 논리 격자 | **128×128 → ×8 nearest** (아이콘은 줄여 보므로 32 격자는 너무 거칠다) |
| 바탕 | **화면 끝까지 꽉 채운다 (풀블리드).** 투명·여백·액자·둥근 모서리 없음 |
| 안전 영역 | 장수 얼굴·칼·손은 **가운데 지름 66% 원 안** — 안드로이드 적응형 아이콘이 원으로 깎는다 |
| 글자 | **없음.** 제목·로고·숫자 전부 금지 |
| 워터마크 | 없음 — 특히 오른쪽 아래 모서리를 확대해 확인 |

### 팔레트 (PixelTheme 과 한 벌)

외곽선 짙은 남흑 `≈#0A0F2A` · 하늘 남색 `≈#08163A`~`≈#1E3A9C` · 망토 왕실 파랑 `≈#1E3A9C` / 밝은 파랑 `≈#3C78D8` ·
금 `≈#F0C024` / `≈#E49C00` / 그림자 `≈#A85400` · 갑옷 은 `≈#C8D0DC` / `≈#8A96AA` · 황혼 지평선 주황 `≈#E8853A` (얇게).
**광원: 좌상단** + 칼끝 번쩍임. 한 장 32색 이하.

---

## 4. 바로 붙여 쓸 프롬프트

```
Pixel art mobile game app icon, square 1:1, full-bleed artwork filling the entire canvas edge to edge
(no border, no frame, no rounded corners, no transparent margin).

Subject: a heroic young general in the center foreground, three-quarter view, shown from the knees up,
cute chibi proportions (2.5 heads tall) like classic 16-bit fantasy RPG sprites.
Silver plate armor with antique gold trim, a royal blue cape billowing in the wind,
face visible with a determined expression (open-faced or no helmet).
He raises a longsword high above his head with his right hand, giving the charge signal;
a small golden glint at the sword tip.

Behind him, to the left and right, a dense army of small soldier silhouettes in formation
with spears, shields and two or three blue war banners, stretching to the horizon,
conveying "one commander leading hundreds of troops".
Background: deep navy night sky (#08163A to #1E3A9C) fading to a thin warm dusk glow on the horizon.
Optional: a small golden crown emblem floating just above the general's head.

Palette: dark navy-black outline (#0A0F2A, never pure black), royal blue (#1E3A9C, #3C78D8),
antique gold (#F0C024, #E49C00, shadow #A85400), silver armor (#C8D0DC, #8A96AA).
Single light source from the top-left, 2-3 step cel shading, minimal dithering, max 32 colors.
Designed on a 128x128 logical pixel grid, crisp hard pixel edges, no anti-aliasing, no blur.
Keep the general's face, hands and sword inside the central 66% circle (safe zone for circular masks).
Must stay readable as a tiny 48x48 home-screen icon: one strong silhouette, high contrast against the sky.

No text, no letters, no title, no logo lettering, no numbers, no watermark, no signature,
no sparkle mark in any corner, no monsters or enemies.
```

### 변형 (첫 안이 약하면 하나씩 시도)

- **B — 얼굴 클로즈업:** 위 프롬프트에서 구도를 `shown from the chest up, face and raised sword fill the upper half` 로 바꾼다. 48px 에서 가장 잘 읽힌다.
- **C — 군기 중심:** 장수가 칼 대신 **커다란 파란 군기(금 왕관 문양)** 를 들어 올린다. "지휘" 가 더 직접적으로 읽힌다.

세 안을 만들어 나란히 보여 주고 사용자가 고른다.

---

## 5. 검수

- [ ] 1024×1024, 네 모서리까지 그림이 차 있다 (투명 0%)
- [ ] 글자·워터마크 없음 (오른쪽 아래 확대 확인)
- [ ] **48px · 96px 로 줄여서** 본다 — 장수와 치켜든 칼이 한눈에 읽히나, 대군이 "많다" 로 보이나
- [ ] **원형 마스크(지름 100%) · 둥근 사각 마스크를 씌워서** 본다 — 얼굴·칼끝이 잘리지 않나
- [ ] 로비 배경·PixelTheme 버튼 옆에 놓아 색이 같은 게임으로 보이나

## 6. 범위 밖 (사용자·Claude 가 처리)

| 무엇 | 어떻게 |
|---|---|
| 플랫폼별 아이콘 크기 | 유니티 PlayerSettings 가 이 한 장에서 만든다 — 따로 만들지 않는다 |
| 안드로이드 적응형(전경/배경 분리) | 필요하면 따로 요청한다. 이번엔 한 장 |
| 스토어 그래픽(피처 그래픽·스팀 캡슐) | 이번 범위 아님 |

## 7. 제출

- 커밋: **`Assets/Resources/Icon/Icon.png` 한 파일만** (`art: 앱 아이콘 교체 — Pixel General`). `.meta` 금지.
- 보고: 세 안(A·B·C)의 미리보기, 고른 안, 48px 축소본 경로, 마스크 씌운 확인 이미지 경로.
