# 이미지 작업 지시서 — PixelTheme UI 아이콘 (로비 메인 패널)

대상: 이미지를 만드는 에이전트(Codex) · 작성일 2026-10-02

**당신이 하는 일은 하나다 — 아래 표의 PNG 8장을 도트(픽셀아트)로 새로 그려 지정 경로에 넣는 것.**
유니티 코드·프리팹·씬·`.meta` 는 **건드리지 않는다** (7장 '범위 밖').

| 문서 / 경로 | 무엇 |
|---|---|
| **이 문서** | 무엇을 · 어떤 규격으로 · 바로 붙여 쓸 프롬프트 · 검수 · 제출 |
| `Assets/_project/3.Textures/UI/PixelTheme/` | 당신이 지난번에 만든 스킨 10장 + `README.md` — **이번 그림의 기준** |
| 로비 메인 패널 시안 (사용자가 준 스크린샷) | 아이콘이 어디에 어떤 크기로 앉는지 |

---

## 1. 지금 상태와 목표

로비 메인 패널(장수 선택 · 난이도 · 게임 시작)을 PixelTheme 스킨으로 다시 짰다.
프레임·버튼·배너는 당신의 스프라이트를 그대로 쓰고 있고, **시안에 있는데 그림이 없는 아이콘 8개**만
코드 도형(톱니 대신 슬라이더, 별 대신 마름모, 칼 대신 X 두 획 …)으로 임시로 그려 두었다.

- 만들 것: **8장** — 경로 `Assets/_project/3.Textures/UI/PixelTheme/<파일>.png`
- 방식: **새 파일.** 파일명은 3장 표 그대로 (코드가 이 이름으로 찾는다 — 철자 하나 틀리면 안 뜬다).
- 그림을 넣으면 유니티 쪽은 프리팹만 다시 구우면 바뀐다. 코드 수정은 없다.

---

## 2. 절대 규칙

1. **전부 도트로 그린다.** 매끈한 일러스트·3D 렌더·사진풍 금지. PixelTheme 10장 옆에 놓아 이질감이 없어야 한다.
2. **그림 안에 글자·숫자·기호 문자를 넣지 않는다** (알파벳·한글·`+`·`!`·`?` 전부). 라벨은 게임 UI 가 옆에 따로 쓴다.
3. **배경은 완전 투명.** 판·테두리·버튼 바탕을 그리지 않는다 — 아이콘은 이미 있는 버튼·칸 **위에 얹힌다.**
4. **워터마크 금지 — 특히 오른쪽 아래 반짝이(✦) 마크.** 한 장씩 오른쪽 아래를 확대해 확인하고 지운다.
5. **가장자리 번짐 금지.** 외곽선은 또렷하게, 반투명 픽셀은 거의 없게 (그림 픽셀의 10% 이하).
6. **`.meta` 를 만들거나 고치지 않는다.** 임포트 설정은 유니티가 자동으로 잡는다 (`PixelThemeImporter`).
7. **기존 PixelTheme 10장은 건드리지 않는다.** 특히 `ui_side_banner.png.meta` 의 9-slice 값은 Claude 가 잡아 둔 것이다.
8. 크기·파일명은 3장 표를 따른다. 임의로 바꾸지 않는다.

---

## 3. 아트 디렉션

### 3.1 기준 — 지난번 PixelTheme 10장과 한 벌로 보여야 한다

| 무엇 | 기준 |
|---|---|
| 외곽선 | 1 논리px, 짙은 남흑 `≈#0A0F2A` (순흑 금지) — 버튼·프레임 외곽과 같은 색 |
| 금속 금 | 밝은 금 `≈#F0C024` · 금 `≈#E49C00` · 그림자 `≈#A85400` · 깊은 그림자 `≈#602400` · 하이라이트 `≈#F0E478` |
| 남색·파랑 | 패널 남색 `≈#08163A` · 왕실 파랑 `≈#1E3A9C` · 밝은 파랑 선 `≈#3C78D8` |
| 상아 (글자색과 같은 흰 기호) | `≈#F5E8C2` · 그림자 `≈#B8A57C` |
| 광원 | **좌상단 하나.** 한 화면에 아이콘 여러 개가 같이 뜬다 — 광원이 흔들리면 잡동사니가 된다 |
| 셰이딩 | 2~3단 셀 셰이딩, 디더링은 넓은 면에만 최소로 |
| 색 수 | 한 장 16색 이하 |

### 3.2 제작 방식 — 논리 격자 ×4

모델에 128px 을 바로 요구하면 뭉갠다. **작은 격자로 설계하고 정수배로 키운다.**

| 종류 | 최종 크기 | 논리 격자 | 확대 |
|---|---|---|---|
| 아이콘 (`ui_icon_*`) | 128×128 | 32×32 | ×4 nearest |
| 배너 걸이 (`ui_banner_rod`) | 424×48 | 106×12 | ×4 nearest |

- 생성 → 논리 격자로 다운샘플(팔레트 양자화) → 손질 → ×4 nearest. 형태는 **논리 격자 위에서** 읽혀야 한다.
- 여백: 사방 논리 2~3px — 주제가 가장자리에 닿지 않게 (시트 자를 때 잘린다).
- 아이콘은 게임에서 **30~76px 로 줄여** 쓴다 → 굵은 실루엣 하나, 내부 디테일 최소. **30px 에서 무엇인지 읽히면 합격.**

### 3.3 이 화면에서 틀리기 쉬운 것

- **올라앉는 바탕이 장마다 다르다** (3장 표 '바탕' 열). 금 버튼 위의 칼은 **남색**, 파란 버튼 위의 톱니·새로고침은 **상아/흰색**이어야 보인다. 같은 색으로 통일하지 않는다.
- **유물 버튼 아이콘은 이번 대상이 아니다** (`Icons/LobbyBtns/btn_relic.png` 를 이미 쓴다). 시안의 투구는 유물 버튼이 아니라 **'적용 제한 사항' 칸**에 쓴다.
- **'적용 제한 사항' 투구는 적의 투구다** — 난이도가 올라 적에게 붙는 제약(흉포·군세 …)을 뜻한다. 아군 장군처럼 금빛 영웅 투구로 그리지 않는다. 차갑고 무거운 **강철 회색 전투 투구.**
- **새로고침은 '이 장수를 다른 장수로 다시 뽑기'** 다. 시계·모래시계로 그리면 '대기 시간'으로 읽힌다 — **원을 그리며 서로를 쫓는 화살표 두 개.**

---

## 4. 만들 것 — 8장

| 파일 | 크기 | 화면 표시 | 바탕 (위에 얹히는 곳) | 주제 |
|---|---|---|---|---|
| `ui_icon_gear.png` | 128 | 60 | 파란 버튼 (어둡게 칠한 판) | 설정 — 톱니바퀴 |
| `ui_icon_refresh.png` | 128 | 36 | 파란 버튼 (작다) | 새로고침 — 원형 화살표 두 개 |
| `ui_icon_swords.png` | 128 | 76 | **금 버튼** (게임 시작) | 교차한 두 장검 — **남색 실루엣** |
| `ui_icon_star.png` | 128 | 30 | 남색 칩 (등급 표시) | 금 오각별 |
| `ui_icon_book.png` | 128 | 44 | 파란 버튼 (자세히 보기) | 펼친 책 |
| `ui_icon_helmet.png` | 128 | 56 | 남색 정보 칸 (적용 제한 사항) | 강철 전투 투구 (적) |
| `ui_icon_chest.png` | 128 | 56 | 남색 정보 칸 (보상 배율) | 금테 나무 보물 상자 |
| `ui_banner_rod.png` | 424×48 | 424×48 (원본 그대로) | 왼쪽 세로 배너의 윗변 | 배너를 거는 금 막대 + 양끝 장식 |

### 시트 PT01 — 버튼 속 기호 (2×2, 한 장에 같이 그린다)

- `ui_icon_gear` — **톱니 8개짜리 톱니바퀴, 가운데 둥근 구멍.** 상아색 단색 + 좌상단 하이라이트 + 남흑 외곽선. 정면, 기울이지 않는다.
- `ui_icon_refresh` — **원을 그리며 서로의 꼬리를 쫓는 굵은 화살표 두 개** (시계 방향). 상아색 단색. 36px 로 작게 쓰이므로 획을 논리 3px 이상 굵게.
- `ui_icon_swords` — **X 자로 교차한 장검 두 자루, 칼끝이 위.** 칼날·손잡이 전부 **짙은 남색(`≈#0A1838`) 실루엣** + 칼날에만 한 단계 밝은 남색 하이라이트. 금 버튼(`≈#F0C024`) 위에서 또렷해야 한다 — 금색·은색으로 칠하면 버튼에 묻힌다.
- `ui_icon_star` — **통통한 금 오각별**, 좌상단 하이라이트 + 남흑 외곽선. 30px 에서도 별로 읽히게 끝을 뾰족하게 하지 말고 살짝 뭉툭하게.

### 시트 PT02 — 정보 칸 사물 (3장, 2×2 중 한 칸은 비운다)

- `ui_icon_book` — **펼친 책을 정면에서**, 금빛 가죽 표지 + 상아색 종이 + 가운데 접힌 골. 시안의 '자세히 보기' 버튼 왼쪽 책과 같은 느낌.
- `ui_icon_helmet` — **정면을 본 강철 전투 투구**, 눈구멍 가로 틈 + 코 보호대. 강철 청회(`≈#8A96AA` · 그림자 `≈#4A5468`), 장식 깃·금테 없음. 시안 '적용 제한 사항' 칸의 투구.
- `ui_icon_chest` — **정면에서 약간 내려다본 보물 상자**, 갈색 나무 + 금 테두리·자물쇠. 뚜껑은 닫혀 있다 (열려서 금화가 튀어나오면 '보상 획득' 으로 읽힌다 — 여기는 '보상 배율' 이다).

### 단독 — `ui_banner_rod`

- **가로로 긴 금 막대 + 양 끝의 둥근 금 장식(작은 구슬 위 뾰족한 촉).** 왼쪽 세로 배너(`ui_side_banner`) 윗변에 걸려 배너를 매단 것처럼 보인다.
- 막대 두께 논리 3px, 금 셰이딩은 `ui_divider_gold` 와 같은 톤. 막대 가운데에 작은 마름모 장식 하나.
- 좌우 대칭. 줄·고리는 그리지 않는다 (배너 위에 겹친다).

---

## 5. 바로 붙여 쓸 프롬프트

### 5.1 공통 (모든 시트 앞에 붙인다)

```
Pixel art game UI icon sheet, front-facing, flat orthographic view.
Style must match an existing pixel UI kit: deep navy (#08163A) panels, royal blue (#1E3A9C),
antique gold trim (#F0C024 / #E49C00 / shadow #A85400), ivory (#F5E8C2) symbols.
1-pixel dark navy-black outline (#0A0F2A, never pure black), single light source from the top-left,
2-3 step cel shading, minimal dithering, at most 16 colors per icon.
Designed on a 32x32 logical pixel grid, crisp hard pixel edges, no anti-aliasing, no blur, no glow.
Fully transparent background. No text, no letters, no numbers, no watermark, no signature,
no sparkle mark in any corner. Each subject centered with 2-3 logical pixels of empty margin.
Layout: 2x2 grid of separate icons on one 1024x1024 transparent canvas, each icon inside its own
512x512 quadrant, nothing crossing the quadrant borders, no dividing lines or frames between them.
```

### 5.2 시트 PT01

```
Top-left: a gear / cogwheel with 8 teeth and a round hole in the center, solid ivory color with
top-left highlight. Top-right: two thick curved arrows chasing each other in a clockwise circle
(refresh / reroll symbol), solid ivory, bold strokes readable at 36 px.
Bottom-left: two long swords crossed in an X, blades pointing up, painted entirely in very dark
navy (#0A1838) with a slightly lighter navy highlight on the blades only — it will sit on a bright
gold button, so no gold or silver on the swords.
Bottom-right: a chunky golden five-pointed star with slightly rounded tips, top-left highlight.
```

### 5.3 시트 PT02

```
Top-left: an open book seen from the front, golden-brown leather cover, ivory pages, visible
center fold. Top-right: a front-facing steel battle helmet of an enemy soldier, horizontal eye
slit and nose guard, cold blue-grey steel (#8A96AA, shadow #4A5468), no plume, no gold trim.
Bottom-left: a closed treasure chest seen slightly from above, brown wood planks, gold metal
bands and a gold lock plate, lid shut (no coins spilling out).
Bottom-right: leave completely empty and transparent.
```

### 5.4 단독 — 배너 걸이

```
Pixel art game UI ornament, front view, fully transparent background.
A long horizontal antique gold rod used to hang a vertical heraldic banner, with a small round
gold finial (ball with a short spike) at each end and a tiny gold diamond ornament at the center.
Rod 3 logical pixels thick, 1-pixel dark navy-black outline (#0A0F2A), top-left light,
gold palette #F0E478 / #F0C024 / #E49C00 / #A85400. Perfectly left-right symmetric.
Designed on a 106x12 logical pixel grid, crisp hard pixel edges, no anti-aliasing, no text,
no watermark, no strings or rings.
```

---

## 6. 작업 순서와 검수

1. **PT01 을 먼저** 만들어 사용자에게 보여 준다 — 외곽선·광원·상아색 톤을 여기서 고정한다. **확인 뒤 PT02 · 걸이.**
2. 시트는 정확히 1/2 지점에서 자른다 → 각 칸을 논리 32×32 로 줄여 손질 → ×4 nearest = 128×128.
3. **넷 중 하나만 틀렸으면 그 칸만** 다시 만든다. 통과한 칸을 다시 굴리지 않는다.
4. 시트 원본(1024)은 저장소에 넣지 않는다. 잘라 낸 개별 PNG 만.

**검수 — 제출 전에 한 장씩**

- [ ] 크기: 아이콘 128×128, 걸이 424×48, RGBA
- [ ] 네 모서리 픽셀이 완전 투명 (판·바탕을 그리지 않았다)
- [ ] 오른쪽 아래 모서리를 확대해 반짝이·서명 없음
- [ ] 글자·숫자 없음
- [ ] **표시 크기로 줄여서** 본다 — 별 30px · 새로고침 36px 에서 무엇인지 읽히나
- [ ] **바탕 위에 얹어서** 본다 — 칼은 금 버튼 위, 톱니·새로고침·책은 파란 버튼 위, 투구·상자·별은 남색 칸 위
      (PixelTheme 의 `ui_button_gold_9slice` · `ui_button_blue_9slice` · `ui_inset_panel_9slice` 를 깔고 확인)
- [ ] 8장의 외곽선 굵기·광원 방향이 같다

---

## 7. 범위 밖 — 당신이 하지 않는 일 (사용자·Claude 가 유니티에서 처리)

| 무엇 | 누가 / 어떻게 |
|---|---|
| 임포트 설정 (Sprite · 압축 없음 · 아이콘 Bilinear / 걸이 Point) | 자동 — `Editor/PixelThemeImporter.cs` 가 새 파일에 건다 |
| 프리팹에 반영 | `Tools > Project K > 프리팹 생성 > 로비 > MainPanel` 다시 굽기 (용병 고용 팝업도 같은 카드를 쓴다 → `팝업 > Mercenary` 도) |
| 코드 연결 | 이미 되어 있다 — 파일명만 맞으면 `MainPanelCreator.IconOr()` 가 도형 대신 그림을 쓴다 |

---

## 7-A. 추가 의뢰 — 스탯 아이콘 7장 (2026-10-02)

**왜:** 화면에서 스탯 이름 글자를 빼고 **아이콘만** 보여 준다 (장수 카드·상점 카드 등).
글자는 언어마다 길이가 달라 칸을 넘었다. 아이콘의 뜻은 장수 상세 창의 스탯 목록이 `[아이콘] 이름 · 값` 으로 알려 준다.
그래서 **아이콘 하나가 스탯 하나와 1:1** 이어야 하고, 서로 닮으면 안 된다.

- 경로: `Assets/_project/3.Textures/Icons/Stats/<파일>.png` — **PixelTheme 폴더가 아니다**
- 규격: 128×128 RGBA, 투명 배경, 논리 32×32 → ×4 nearest (3.2 와 같음). `.meta` 는 만들지 않는다.
- 표시 크기: **34 ~ 52px** — 34px 에서 무엇인지 읽히면 합격.

### 기준 — 이미 있는 4장과 한 벌 (같은 폴더)

| 파일 | 그림 | 색 |
|---|---|---|
| `stat_hp.png` | 하트 | 초록 |
| `stat_attack.png` | 장검 한 자루 (사선) | 붉은 칼날 + 금 자루 |
| `stat_defense.png` | 방패 | 파랑 + 금테 |
| `stat_soldier_count.png` | 금 투구 셋 (가운데가 앞) | 금 |

공통 규칙: **사물 하나**, 굵은 짙은 외곽선, 좌상단 광원, 2~3단 셀 셰이딩, 판·테두리 없음.
⚠ 새 7장은 위 4장의 그림(하트·장검·방패·투구)과 **겹치지 않게** — 특히 공격속도가 장검처럼 보이면 안 된다.

### 시트 PT03 — 움직임 · 공격 (2×2)

| 파일 | 스탯 | 주제 | 색 | 구분 |
|---|---|---|---|---|
| `stat_move_speed.png` | 이동속도 | **날개 달린 가죽 장화** 한 짝, 뒤로 흰 속도선 2줄 | 하늘색 날개 + 갈색 장화 | — |
| `stat_attack_speed.png` | 공격속도 | **짧은 단검 두 자루가 겹쳐 연속으로 휘두르는 모습** + 단검 뒤 주황 궤적 호 2줄 | 주황·노랑 궤적 + 은 칼날 | `stat_attack`(붉은 장검 하나)과 다르게 — **짧은 칼 둘 + 궤적** |
| `stat_attack_range.png` | 사거리 | **과녁(동심원 3겹) 한가운데 꽂힌 화살** | 청록 과녁 + 흰 화살 깃 | — |
| `stat_command_power.png` | 지휘력 | **금 깃대에 매단 파란 군기(작은 왕관 문양)** 가 펄럭이는 모습 | 파란 깃발 + 금 깃대 | `stat_soldier_count`(투구 셋)과 다르게 — **깃발 하나** |

### 시트 PT04 — 스킬 · 치명 (3장, 한 칸 비움)

| 파일 | 스탯 | 주제 | 색 | 구분 |
|---|---|---|---|---|
| `stat_skill_cooldown.png` | 스킬 쿨타임 감소 | **모래시계** — 위 칸이 거의 비었고 아래에 푸른 모래 | 금 틀 + 푸른 빛 모래 | 이 게임에서 모래시계는 '쿨타임' 에만 쓴다 |
| `stat_crit_chance.png` | 치명 확률 | **노란 번개 모양의 날카로운 섬광(4갈래 별빛)** 하나 | 노랑·흰 | 아래 치명 피해와 **짝** — 이쪽은 '확률' = 가볍고 날카로운 반짝임 |
| `stat_crit_damage.png` | 치명 피해 | **붉은 폭발 파편(8갈래 터짐)** 한가운데 칼끝이 찍힌 모습 | 진홍·주황 | 치명 확률보다 **크고 무겁게** — 같은 별 모양으로 그리지 않는다 |

### 바로 붙여 쓸 프롬프트

5.1 공통 프롬프트를 쓰되, 아래 문장으로 **교체**한다 (스탯 아이콘은 버튼 위가 아니라 남색 칸 위에 단독으로 뜬다):

```
Pixel art game stat icon sheet matching an existing set: a green pixel heart, a red longsword with
gold hilt, a blue shield with gold rim, three gold helmets. Same style: one object per icon, thick dark
navy-black outline (#0A0F2A), single top-left light, 2-3 step cel shading, fully transparent background,
no frame, no plate. 32x32 logical pixel grid, crisp hard edges, no anti-aliasing, no text, no numbers,
no watermark, no sparkle mark in any corner. 2x2 grid on a 1024x1024 transparent canvas, one icon per
512x512 quadrant, nothing crossing quadrant borders.
```

PT03:
```
Top-left: a single brown leather boot with small light-blue wings at the ankle and two white speed lines
behind it. Top-right: two short silver daggers overlapping mid-swing with two orange-yellow motion arcs
behind them (fast repeated attacks; must not look like a single longsword). Bottom-left: a teal archery
target with three concentric rings and a white-fletched arrow stuck in the center. Bottom-right: a blue war
banner with a small gold crown emblem, waving on a gold flagpole.
```

PT04:
```
Top-left: an hourglass with a gold frame, the top bulb almost empty, glowing blue sand gathered at the
bottom. Top-right: a single sharp four-pointed yellow-white flash shaped like a lightning spark, light and
crisp. Bottom-left: a heavy crimson-orange eight-pointed explosion burst with fragments flying out and a
blade tip striking its center; bigger and heavier than the yellow flash. Bottom-right: leave empty.
```

### 검수 추가

- [ ] 34px 로 줄여 **11장(기존 4 + 새 7) 을 한 줄에** 놓고 본다 — 서로 헷갈리는 짝이 없나 (장검↔단검 둘, 투구↔깃발, 섬광↔폭발)
- [ ] 남색 칸(`ui_info_panel_9slice`) 위에서 본다

유니티 쪽(Claude·사용자): 넣은 뒤 `데이터 생성 > SpriteManager` 를 다시 돌려 아틀라스에 넣고, 해당 프리팹을 다시 굽는다.

---

## 8. 제출

- 커밋에는 **PNG 8장 + `README.md` 표에 8줄 추가**만 넣는다 (`.meta` · `.cs` · `.prefab` 금지).
  커밋 메시지: `art: PixelTheme 메인 패널 아이콘 8장`
- 보고에 넣을 것: ① 넣은 파일 목록 ② 검수 체크 결과 ③ 표시 크기·바탕 위에 얹은 확인 이미지 경로
  ④ 이 문서와 다르게 그린 것이 있으면 그 이유 ⑤ 사용자가 유니티에서 해 줘야 하는 것(7장).
- **주제가 서로 닮아 보이거나 시안과 어긋나 보이면 혼자 바꾸지 말고 먼저 묻는다.**
