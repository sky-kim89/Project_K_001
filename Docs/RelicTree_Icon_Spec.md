# 유물 트리 아이콘 생성 명세 (69종)

작성일: 2026-08-25  
대상: `Assets/_project/3.Textures/Icons/RelicTree/` — 신규 폴더, 69개 PNG  
해상도: **128×128** (프로젝트 전체 아이콘을 128로 올리는 중이다)  
짝 문서: `Docs/Icon_Regeneration_Spec.md` — 그쪽이 "유물은 개편 중이라 제외"로 남겨 둔 부분이 이 문서다.

## 왜 새로 필요한가

유물 습득이 카드 그리드에서 **테크트리**로 교체됐다(`Relic/Tree/RelicTreeCatalog.cs`, 69노드).
구 유물 아이콘 `Icons/Relics/` 29장은 구 `RelicId` 기준이라 트리 노드와 1:1로 맞지 않는다.
현재 `RelicTreePopup` 은 아이콘이 없어 계열 색 타일만 그리고 있다.

> ⚠ `Icons/Relics/` 29장을 재사용하거나 덮어쓰지 말 것  
> 구 `RelicData` SO 가 아직 그 스프라이트를 참조한다. 트리는 **새 폴더**를 쓴다.

## 파일 규칙

| 항목 | 값 |
|---|---|
| 저장 경로 | `Assets/_project/3.Textures/Icons/RelicTree/` |
| 파일명 | `node_<enum 이름 snake_case>.png` (예: `N_Blade` → `node_blade.png`) |
| 해상도 | 128×128 |
| 형식 | PNG, 알파 채널 포함, 배경 완전 투명 |
| 아틀라스 | 신규 `Atlas_RelicTree.spriteatlas` 에 폴더째 등록 |

파일명은 `RelicNodeId` enum 이름에서 `N_` 를 떼고 snake_case 로 바꾼 것이다.

> **변환 규칙의 정본은 `Relic/Tree/RelicIconKey.cs` 다.**
> 런타임 조회·자리표시 생성·이 문서 셋이 같은 규칙을 써야 한다.
> 한 곳이라도 어긋나면 그림이 **에러 없이 조용히 안 붙는다.**

## 공통 시각 언어

| 요소 | 방향 | 의도 |
|---|---|---|
| 배경 | 완전 투명. 원형·사각 배경판을 그리지 말 것 | 노드 Face 가 계열 색 판을 이미 깔고 그 위에 아이콘만 얹는다 |
| 주제 | 한 아이콘에 하나의 읽히는 도구·행위·상태 | 트리에서 52px 로 축소돼 표시된다 — 두 개를 넣으면 뭉갠다 |
| 여백 | 중앙 주제 88~104px, 외곽 여백 12~20px | 마름모(특수 노드)로 45° 회전해도 잘리지 않는다 |
| 외곽선 | 어두운 2~3px 실루엣 외곽선 | 어떤 계열 색 판 위에서도 형태가 분리된다 |
| 광원 | 좌상단 단일 광원, 플랫 셰이딩 | 69장이 한 화면에 같이 보인다 — 광원이 흔들리면 잡동사니가 된다 |
| 색 | 계열 색을 주조로, 강조 1색만 추가 | 계열을 색으로 먼저 읽고 형태로 확인한다 |
| 금지 | 문자·숫자·워터마크·프레임·테두리 장식·드롭섀도 | 프레임은 노드 Face 가 그린다. 겹치면 두 겹이 된다 |

### 계열 색 팔레트

`RelicTreePopup.ColorOf()` 의 값 그대로다. 아이콘 주조색을 여기에 맞춘다.

| 계열 | HEX | 영문 색 이름 (프롬프트에 넣을 것) | 노드 수 |
|---|---|---|---:|
| 뿌리 | `#D9B238` | warm gold | 1 |
| 공격 | `#E0693D` | ember orange-red | 24 |
| 체력 | `#4F99C4` | steel blue | 12 |
| 병사 | `#C79438` | bronze | 14 |
| 유틸 | `#9978D1` | amethyst violet | 18 |

### 특수 노드 (마름모) — 9종

`MaxLevel == 1` 인 노드는 팝업이 Face 를 45° 회전시켜 마름모로 그린다.
**아이콘은 회전하지 않는다**(`RelicTreePopup` 이 반대로 되돌린다). 다만 마름모 안에 들어가므로
네 모서리로 뻗는 형태는 피하고 원형에 가까운 실루엣을 쓴다.

대상: `node_one_strike`, `node_endless_chain`, `node_martial_legacy`, `node_immortal_vow`, `node_steel_citadel`, `node_time_reins`, `node_march_order`, `node_moment_mastery`, `node_doom_prophecy`

## 공통 프롬프트

각 노드 프롬프트 앞에 그대로 붙인다. `{BRANCH}` 자리에 계열 영문 색 이름을 넣는다.

```
128x128 game icon, dark fantasy, single centered subject,
flat shading with one light source from the upper left,
crisp dark outline, {BRANCH} as the dominant color with one accent hue,
fully transparent background, no background plate, no frame, no border,
no text, no numbers, no watermark, no signature,
centered composition with even margin on all four sides
```

### 네거티브 프롬프트

```
text, letters, numbers, watermark, signature, logo,
multiple subjects, cluttered composition, busy background,
background plate, circular badge, frame, border, UI chrome,
photorealistic, 3D render, depth of field, drop shadow on background,
cropped subject, subject touching the edge
```

### 생성 해상도 — 표시 크기가 기준이다

이 아이콘이 실제로 보이는 곳은 **트리 노드 한 곳뿐이고, 그 칸은 52px 이다.**
그래서 최종 파일은 128×128 이면 충분하다 (노출 52 × 2, 2의 거듭제곱 올림).

모델에 128×128 를 직접 요구하지 말 것 — 뭉개진다.
**512×512 로 생성한 뒤 128×128 로 축소**하고, 축소 후 알파 가장자리를 정리한다.
1024 원본을 남길 이유는 없다 — 52px 로 줄어들 그림이라 디테일을 더 넣을수록 축소에서 뭉개지기만 한다.
축소 필터는 Lanczos/Area 를 쓰고, 반투명으로 번진 외곽 픽셀은 알파 임계값 0.35 로 자른다.

**검수는 52px 축소본으로 한다.** 128px 에서 예쁜지가 아니라 52px 에서 계열과 주제가
읽히는지가 합격 기준이다.

## 노드별 프롬프트 (69종)

`주제` 는 검수용 한국어 설명이고, `Prompt` 가 공통 프롬프트 뒤에 붙는 본문이다.
`효과` 는 아이콘이 무엇을 뜻해야 하는지 판단하는 근거다 — 그림이 효과와 어긋나면 다시 만든다.

### 뿌리

주조색 `#D9B238` (warm gold)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_origin.png` | **근원의 각인** | 0 | 공격력 +5%, 최대체력 +5% | 네 갈래로 뻗는 금빛 룬 각인 | an engraved golden rune sigil pulsing with light, four faint branch lines radiating outward |

### 공격 · 본선 (위)

주조색 `#E0693D` (ember orange-red)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_blade.png` | **벼려진 칼날** | 1 | 공격력 +4% | 갓 벼려진 장검 날 | a freshly honed longsword blade, a single sharp highlight running down the edge |
| `node_destruction.png` | **파괴의 의지** | 2 | 공격력 +5% | 갈라진 바위를 부수는 강철 주먹 | a gauntleted fist crushing a cracked boulder, shards flying outward |
| `node_crit_sense.png` | **치명의 감각** | 3 | 치명확률 +2.5%p | 동공에 조준선이 비친 맹수의 눈 | a narrowed predatory eye with a thin crosshair reflected in the pupil |
| `node_pierce_lance.png` | **관통의 창** | 3 | 방어관통 +3%p | 방패를 꿰뚫는 창끝 | a spear tip punching clean through a splintering shield |
| `node_executioner.png` | **처형자** | 4 | 치명피해 +12% | 핏빛 날의 처형 도끼 | a broad executioner axe with a blood-red edge and a notched haft |
| `node_slayer_seal.png` | **학살자의 인장** | 4 | 공격력 +8% | 송곳니 해골이 찍힌 봉랍 인장 | a wax seal stamped with a fanged skull, the wax still dripping |
| `node_one_strike.png` | **일격필살** ◆ | 5 | 치명피해 +50%, 치명확률 +10%p | 화면을 가르는 한 줄기 참격 | a single vertical slash of blinding light cleaving the frame, cut edges glowing |

### 공격 · 공속/쿨감 (오른쪽 위)

주조색 `#E0693D` (ember orange-red)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_swift_hand.png` | **빠른 손놀림** | 2 | 공속 +4% | 잔상이 남은 단검 뽑기 | a blurred hand drawing a dagger, motion streaks trailing behind it |
| `node_chain_rhythm.png` | **연격의 리듬** | 3 | 공속 +5% | 겹쳐지는 세 번의 초승달 참격 | three overlapping crescent slash arcs in sequence, brightest at the front |
| `node_time_compress.png` | **시간의 압축** | 3 | 쿨감 +2.5%p | 허리가 눌린 모래시계 | an hourglass squeezed narrow at the waist, sand rushing through fast |
| `node_storm_concert.png` | **폭풍의 연주** | 4 | 공속 +8% | 칼날이 소용돌이치는 폭풍 | a whirlwind of spinning blades forming a storm funnel |
| `node_hawk_eye.png` | **매의 시야** | 4 | 사거리 +6% | 시위 너머로 보이는 매의 눈 | a hawk eye framed between a drawn bowstring and an arrow nock |
| `node_spell_haste.png` | **주문 가속** | 4 | 쿨감 +3%p | 앞으로 튕기는 시계바늘의 마법진 | a spinning arcane circle with clock hands flicking forward, motion arcs behind |
| `node_endless_chain.png` | **무한 연쇄** ◆ | 5 | 쿨감 +12%p, 공속 +10% | 사슬로 된 우로보로스 | an ouroboros made of interlocking chain links, glowing at the seam |

### 공격 · 직업 (왼쪽 위)

주조색 `#E0693D` (ember orange-red)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_general_sword.png` | **장군의 검** | 2 | 장수 공격력 +6% | 땅에 꽂힌 장군의 장식검 | an ornate commander sword planted point-down in the earth |
| `node_hero_token.png` | **영웅의 증표** | 3 | 장수 공격력 +7%, 최대체력 +7% | 월계관이 새겨진 영웅의 메달 | a hero medallion on a chain, a laurel wreath embossed on its face |
| `node_hero_awaken.png` | **영웅의 각성** | 4 | 장수 공격력 +9%, 치명확률 +4%p | 빛이 터져 나오는 갈라진 메달 | a medallion cracking open, light bursting out through the fracture |
| `node_martial_legacy.png` | **무예의 전승** ◆ | 2 | 공격력 +5% | 교차한 무기 위에 펼쳐진 무예서 | an old martial technique scroll unrolled over crossed weapons |
| `node_knight_oath.png` | **기사단의 맹세** | 3 | 기사 공격력 +8%, 최대체력 +6% | 검자루에 얹힌 기사의 투구 | a kneeling knight helm resting on a sword hilt, an oath ribbon tied around it |
| `node_archer_spirit.png` | **활의 정령** | 3 | 궁수 공속 +8%, 사거리 +6% | 초록 정령빛이 감긴 장궁 | a longbow wreathed in a wisp of green spirit light |
| `node_mage_crystal.png` | **마력의 결정** | 3 | 마법사 공격력 +9%, 쿨감 +4%p | 룬 위에 떠 있는 보랏빛 마력 결정 | a faceted violet mana crystal hovering above a glowing rune |
| `node_kings_blade.png` | **왕의 검** | 5 | 기사 공격력 +12%, 방어율 +4%p, 병사 수 +1명 | 왕관을 두른 대검과 군기 | a crowned greatsword with a small banner tied to the crossguard |
| `node_storm_bow.png` | **폭풍의 궁** | 5 | 궁수 공격력 +12%, 치명확률 +5%p, 사거리 +6% | 번개를 감은 화살 세 발을 쏘는 활 | a bow loosing three lightning-wreathed arrows at once |
| `node_archmage_legacy.png` | **대마법사의 유산** | 5 | 마법사 공격력 +14%, 쿨감 +5%p, 사거리 +6% | 구슬이 도는 지팡이와 펼쳐진 마도서 | an archmage staff with an orbiting orb, an open grimoire beneath it |

### 체력 · 본선 (아래)

주조색 `#4F99C4` (steel blue)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_tough_skin.png` | **굳은 살갗** | 1 | 최대체력 +5% | 두껍게 굳은 비늘 가죽 | a patch of thick scarred hide, plated and leathery, one deep scar across it |
| `node_iron_will.png` | **철의 의지** | 2 | 방어율 +2%p | 리벳으로 조인 강철 심장 | an iron heart bound in riveted steel bands |
| `node_unyielding_heart.png` | **불굴의 심장** | 3 | 최대체력 +6% | 강철 갈비뼈에 감싸인 빛나는 심장 | a glowing heart caged inside a lattice of steel ribs |
| `node_rampart_oath.png` | **성벽의 맹세** | 3 | 방어율 +2.5%p | 맹세의 손이 새겨진 성벽 | a battlement wall section with an oath hand carved into the stone |
| `node_regeneration.png` | **재생의 축복** | 4 | 최대체력 +10% | 갈라진 돌 심장을 뚫고 나온 새싹 | a green shoot breaking through a cracked stone heart |
| `node_immortal_vow.png` | **불멸의 서약** ◆ | 5 | 방어율 +10%p | 끊을 수 없는 강철 고리로 봉한 서약서 | a sealed vow scroll bound with an unbreakable steel ring |

### 체력 · 방패병/장수 (왼쪽 아래)

주조색 `#4F99C4` (steel blue)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_general_plate.png` | **장군의 흉갑** | 2 | 장수 방어율 +2.5%p | 문장이 새겨진 장교 흉갑 | an officer breastplate with a crest engraved on the chest |
| `node_guardian_oath.png` | **수호자의 맹세** | 3 | 장수 최대체력 +9% | 수호자의 손이 얹힌 타워 실드 | a tower shield with a guardian gauntlet pressed flat against it |
| `node_counter_armor.png` | **역전의 갑주** | 4 | 장수 방어율 +2.5%p, 최대체력 +9% | 칼날을 부러뜨리는 가시 갑주 | spiked armor deflecting a blade that shatters on impact |
| `node_bulwark_lord.png` | **방벽의 군주** | 3 | 방패병 방어율 +5%p, 최대체력 +8% | 왕관을 쓴 거대 카이트 실드 | an immense crowned kite shield braced into the ground |
| `node_fortress_avatar.png` | **요새의 화신** | 4 | 방패병 최대체력 +12%, 방어율 +3%p, 병사 수 +1명 | 성문과 하나가 된 전사 | a warrior fused with a fortress gate, stone shoulders and iron bracing |
| `node_steel_citadel.png` | **강철 성채** ◆ | 5 | 방패병 방어율 +12%p, 최대체력 +30% | 내려진 쇠창살문의 강철 성채 | a steel citadel tower with its portcullis lowered and locked |

### 병사 · 정공 (왼쪽)

주조색 `#C79438` (bronze)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_command_banner.png` | **지휘의 깃발** | 1 | 병사 수 +1명 | 땅에 꽂혀 펄럭이는 군기 | a war banner planted in the ground, its pennant snapping in the wind |
| `node_command_basics.png` | **지휘의 기초** | 2 | 병사 공격력 +5% | 나란히 치켜든 창 세 자루 | three spears raised in unison by a small squad, tips aligned |
| `node_line_drill.png` | **전열의 훈련** | 2 | 병사 최대체력 +5% | 겹쳐 세운 방패 벽 | a locked shield wall of three overlapping shields seen head on |
| `node_grand_advance.png` | **대군의 진격** | 3 | 병사 수 +2명 | 안개 속으로 이어지는 진격 대열 | a mass of marching silhouettes with raised spears receding into fog |
| `node_elite_drill.png` | **정예 훈련** | 3 | 병사 공격력 +4%, 최대체력 +4% | 훈련목 앞에 놓인 정예병 투구 | a decorated soldier helm in front of a battered training post |
| `node_legion_might.png` | **군단의 위세** | 3 | 지휘력 +2 | 독수리를 얹은 군단 깃대 | a legion standard topped with a bronze eagle |
| `node_undefeated_legion.png` | **불패의 군단** | 4 | 병사 공격력 +8%, 최대체력 +8% | 흠집 없는 승리의 군단기 | an unbroken legion standard with victory laurels, no chips or tears |
| `node_veteran_commander.png` | **백전노장** | 4 | 지휘력 +3 | 전투 흠집이 가득한 노장의 투구 | a scarred veteran helm covered in tally notches from many battles |
| `node_thousand_horse.png` | **천군만마** | 4 | 병사 수 +2명 | 돌격하는 기병 쐐기 대형 | a charging cavalry wedge of horse silhouettes, dust trailing |
| `node_war_god_majesty.png` | **군신의 위엄** | 5 | 지휘력 +4 | 군림하는 기운을 뿜는 군신의 관 투구 | a war god crowned helm radiating a commanding aura |

### 병사 · 역분기 (왼쪽 아래)

주조색 `#C79438` (bronze)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_lone_general.png` | **고독한 장수** | 3 | 장수 병사 수 −5명, 공격력 +12%, 이동속도 +5% | 뒤에서 병사 그림자가 사라지는 장수 | a lone general standing forward while faded troop silhouettes dissolve behind |
| `node_one_vs_thousand.png` | **일기당천** | 4 | 장수 병사 수 −5명, 지휘력 −5, 공격력 +10%, 최대체력 +10%, 방어율 +3%p | 적 창벽을 홀로 마주한 무장 | one armored figure facing a solid wall of enemy spears |
| `node_peerless.png` | **무쌍** | 5 | 장수 병사 수 −10명, 공격력 +15%, 치명확률 +8%p, 치명피해 +20%, 공속 +10% | 베어낸 무기에 둘러싸여 도는 전사 | a lone warrior mid-spin inside a ring of cut-down weapons |
| `node_forsaken_stand.png` | **고립무원** | 4 | 깎은 병사당 장수 공·체 +0.4% | 무너지는 발판 위에 홀로 선 전사 | a warrior on a shrinking island of ground, broken banners falling away |

### 유틸 · 자원 (오른쪽 위)

주조색 `#9978D1` (amethyst violet)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_growth_mark.png` | **성장의 증표** | 1 | 경험치 +12% | 빛 화살표가 솟는 펼친 책 | an open book with a rising arrow of light lifting off the page |
| `node_golden_grace.png` | **황금의 가호** | 2 | 골드 +12% | 금화가 쏟아지는 축복받은 돈주머니 | a coin purse spilling gold coins, a soft blessing glow above |
| `node_warrior_legacy.png` | **전사의 유산** | 3 | 소울 +12% | 쓰러진 투구에서 오르는 푸른 혼불 | a soul-blue ember rising out of a fallen helm |
| `node_sage_tome.png` | **현자의 서** | 4 | 경험치 +10% | 빛나는 서표가 꽂힌 두꺼운 현자의 서 | a thick sage tome with a glowing bookmark ribbon hanging out |
| `node_golden_bounty.png` | **만금의 축복** | 4 | 골드 +10% | 금화와 보석이 넘치는 보물 상자 | an overflowing treasure chest of coins and gemstones |
| `node_soul_urn.png` | **영혼의 항아리** | 4 | 소울 +10% | 푸른 혼불이 새어 나오는 항아리 | a ceramic urn with blue soul flames curling out of its mouth |

### 유틸 · 편의 (오른쪽 아래)

주조색 `#9978D1` (amethyst violet)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_time_reins.png` | **시간의 고삐** ◆ | 2 | 배속 2× 해금 | 시계 문자판에 물린 가죽 고삐 | a leather rein bridling a clock face like a horse bit |
| `node_moment_mastery.png` | **찰나의 지배** ◆ | 4 | 배속 2× 해금 | 멈춘 순간을 움켜쥔 깨진 시계 | a shattered clock face with a hand grasping the frozen instant |
| `node_march_order.png` | **출병 명령** ◆ | 3 | 장수 슬롯 +1칸 | 봉랍이 찍힌 출병 명령서와 군기 | a sealed war order scroll with a wax seal, a marching banner behind it |
| `node_ability_reform.png` | **어빌리티 재편성** | 3 | 새로고침 +1회 | 화살표가 도는 재배치되는 카드 세 장 | three cards being reshuffled, circular arrows around them |
| `node_advanced_arcana.png` | **고급 비전** | 4 | 고급확률 +10%p | 중앙에 희귀 보석이 박힌 정교한 마법 문양 | an ornate arcane sigil with a rare gemstone set at its center |
| `node_blessed_choice.png` | **선택의 축복** | 4 | 선택지 +1개 | 하나만 후광이 도는 네 장의 선택 카드 | a fan of four glowing choice cards, one haloed brighter than the rest |
| `node_fate_dice.png` | **운명의 주사위** | 3 | 무작위 특성 +1개 | 룬이 새겨진 채 구르는 주사위 | a rune-carved die caught mid-tumble, its faces glowing faintly |

### 유틸 · 적 약화 (아래)

주조색 `#9978D1` (amethyst violet)

| 파일명 | 노드 | T | 효과 (레벨당) | 주제 | Prompt |
|---|---|:-:|---|---|---|
| `node_trial_baptism.png` | **시련의 세례** | 2 | 적 체력 −2.5% | 갈라진 적 해골에 검은 물을 붓는 성배 | a chalice pouring dark water over a cracked enemy skull |
| `node_fear_brand.png` | **공포의 각인** | 3 | 적 공격력 −2.5% | 방패에 공포의 룬을 지지는 낙인쇠 | a branding iron burning a fear rune into an enemy shield |
| `node_wither_curse.png` | **쇠약의 저주** | 4 | 적 체력 −4% | 쪼그라드는 심장을 말리는 저주의 손 | a withering skeletal hand draining a shriveling heart |
| `node_disarm.png` | **무력화** | 4 | 적 공격력 −4% | 힘 빠진 손에서 떨어지는 부러진 검 | a snapped sword falling from a limp gauntlet |
| `node_doom_prophecy.png` | **몰락의 예언** ◆ | 5 | 적 체력 −12% | 왕관이 떨어지는 갈라진 예언 석판 | a cracked prophecy tablet with a crown tumbling off it |

◆ = 특수 노드(마름모). 원형에 가까운 실루엣을 쓸 것.

## 작업 절차 (2026-08-25 개정 — 연동 완료 기준)

### 그림을 만드는 쪽

1. 512×512 로 69장 생성 → **256×256** 으로 축소 → 알파 임계값 0.35 로 정리.
2. `Assets/_project/3.Textures/Icons/RelicTree/` 의 같은 이름 파일에 **덮어쓴다.**
   (69장 전부 자리표시 PNG 가 이미 그 이름으로 깔려 있다 — 새 파일을 만들지 말고 덮어쓸 것)
3. Unity 를 켠다. **임포트 설정은 자동으로 붙는다** (`RelicTreeIconImporter`).
4. `Tools > Project K > 데이터 생성 > SpriteManager + 아틀라스` 실행 → 아틀라스에 반영.
5. 유물 화면을 열어 확인한다.

> ⚠ **해상도가 128 → 256 으로 바뀌었다**
> 이 명세를 쓸 당시 노드 아이콘 노출 크기는 52px 이었다. 그 뒤 노드를 키우면서
> 실효 노출이 **약 91px** 이 됐다(아이콘 140 캔버스px × `_zoomInit` 0.65).
> 128 은 확대 배율을 조금만 올려도 부족해진다. 2배 여유를 두어 256 으로 만들 것.
> 임포터의 `maxTextureSize` 도 256 이다.

### 파일명이 어긋나면 그림이 조용히 안 붙는다

에러가 나지 않는다 — 아틀라스에서 못 찾으면 **자리표시가 그대로 남는다.**
헷갈리면 `Tools > Project K > 아이콘·텍스처 > 유물 트리 아이콘 누락 점검` 을 실행한다
(파일이 아예 없는 노드를 콘솔에 나열한다).

### 관련 도구

| 메뉴 | 하는 일 |
|---|---|
| `아이콘·텍스처 > 유물 트리 자리표시 생성 (빈 자리만)` | 파일이 없는 노드만 자리표시로 채운다. **진짜 그림은 안 건드린다** |
| `아이콘·텍스처 > 유물 트리 자리표시 강제 재생성` | 69장 전부 자리표시로 덮어쓴다 (확인 대화상자 있음) |
| `아이콘·텍스처 > 유물 트리 아이콘 임포트 재적용` | 이미 임포트된 파일에 설정을 다시 얹는다 |
| `아이콘·텍스처 > 유물 트리 아이콘 누락 점검` | 파일이 없는 노드를 콘솔에 나열 |
| `데이터 생성 > SpriteManager + 아틀라스` | `Atlas_RelicTree` 재패킹 + SpriteManager 갱신 |

### 자동 적용되는 임포트 설정

`Texture Type = Sprite (2D and UI)` · `Alpha Is Transparency = ON` ·
`Mesh Type = Full Rect` · `Filter Mode = Bilinear` · `Compression = None` ·
`Max Size = 256` · `Mipmap = OFF` · `Wrap = Clamp`

> `Mesh Type = Full Rect` 가 중요하다. `Tight` 는 알파를 따라 불규칙 다각형으로 잘려,
> 특수 노드(마름모)에서 Face 를 45° 돌릴 때 모서리가 날아간다.

> ✅ **연동은 끝났다 (2026-08-25).** 이 폴더에 규칙에 맞는 이름으로 PNG 를 넣으면 그대로 붙는다.
> `RelicTreePopup.BuildNode()` 가 `SpriteManager.Instance.Get(RelicIconKey.Of(id))` 로 찾는다.
> 지금은 69장 전부 **자리표시(계열 색 원반 + 티어 눈금)** 로 채워져 있다 — 그 위에 덮어쓰면 된다.
> 자세한 절차는 아래 '작업 절차' 참고.

## 검수 체크리스트

- [ ] 69장 전부 있고 파일명이 `RelicNodeId` snake_case 와 일치한다
- [ ] 배경이 완전 투명하다 (배경판·원형 뱃지가 없다)
- [ ] 128px 에서 주제가 하나로 읽힌다 — 52px 로 줄여도 형태가 남는다
- [ ] 문자·숫자·워터마크가 없다
- [ ] 계열 주조색이 팔레트와 맞다 — 같은 계열끼리 나란히 놓고 확인한다
- [ ] 특수 노드 9종이 마름모 안에서 잘리지 않는다 (45° 회전 마스크로 확인)
- [ ] 효과와 그림이 어긋나는 노드가 없다 (위 표의 `효과` 열 대조)

## 참조

- 노드 표 정본: `Assets/_project/1.Script/Relic/Tree/RelicTreeCatalog.cs`
- 노드 타입·enum: `Assets/_project/1.Script/Relic/Tree/RelicTreeTypes.cs`
- 계열 색·표시 규칙: `Assets/_project/1.Script/UI/Popup/RelicTreePopup.cs`
- 나머지 아이콘 명세: `Docs/Icon_Regeneration_Spec.md`
