# PixelTheme UI Assets

시안의 짙은 남색/왕실 파랑/골드 픽셀 UI를 재구성할 수 있도록 만든 투명 PNG 세트입니다.
텍스트와 아이콘은 이미지에 굽지 않았으므로 TMP와 기존 아이콘을 자식으로 올려 사용합니다.

## Unity 사용

- `*_9slice.png`: `Image.Type = Sliced`
- `ui_header_tab.png`, `ui_divider_gold.png`, `ui_side_banner.png`, `ui_crest_gold.png`, `ui_header_brackets.png`, `ui_banner_rod.png`, `ui_icon_*.png`: `Image.Type = Simple`
- 버튼은 기존 규칙대로 라벨/아이콘을 `Body` 아래에 배치합니다.
- 픽셀 선명도를 위해 모든 PNG는 Point 필터, Mipmap 비활성, 무압축으로 설정했습니다.
- 9-slice border 값은 각 `.meta`에 저장했습니다.

## 파일 목록

| 파일 | 크기 | 용도 |
|---|---:|---|
| `ui_panel_frame_9slice.png` | 768×384 | 중앙/우측 대형 패널 프레임 |
| `ui_button_blue_9slice.png` | 512×124 | 일반 메뉴/상세보기 버튼 |
| `ui_button_gold_9slice.png` | 640×148 | 게임 시작 등 주요 CTA |
| `ui_button_teal_9slice.png` | 512×120 | 난이도 선택 등 상태 버튼 |
| `ui_button_square_9slice.png` | 192×188 | 좌우 이동/새로고침 사각 버튼 |
| `ui_header_tab.png` | 512×108 | 패널 상단 제목 탭 |
| `ui_inset_panel_9slice.png` | 512×108 | 제한 사항/보상 등 내부 정보 박스 |
| `ui_divider_gold.png` | 512×44 | 중앙 장식 구분선 |
| `ui_side_banner.png` | 384×764 | 왼쪽 세로 메뉴 배너 |
| `ui_crest_gold.png` | 192×132 | 패널 상단 왕관형 장식 |
| `ui_icon_gear.png` | 128×128 | 설정 버튼 톱니 아이콘 |
| `ui_icon_refresh.png` | 128×128 | 장수 새로고침 아이콘 |
| `ui_icon_swords.png` | 128×128 | 게임 시작 버튼 교차검 아이콘 |
| `ui_icon_star.png` | 128×128 | 장수 등급 금색 별 아이콘 |
| `ui_icon_book.png` | 128×128 | 자세히 보기 버튼 펼친 책 아이콘 |
| `ui_icon_helmet.png` | 128×128 | 적용 제한 사항의 적군 투구 아이콘 |
| `ui_icon_chest.png` | 128×128 | 보상 배율의 닫힌 보물상자 아이콘 |
| `ui_banner_rod.png` | 424×48 | 왼쪽 세로 배너의 금색 걸이 막대 |
| `ui_header_brackets.png` | 564×72 | 장수 카드 헤더의 좌우 금색 장식 오버레이 |
| `ui_info_panel_9slice.png` | 512×128 | 제한 사항·보상 등 내부 정보칸용 청색 테두리 BG |
| `ui_header_row.png` | 564×72 | 청색 BG와 좌우 금장식을 합친 장수 카드 헤더 |

## 생성 기준

- 참고 이미지: 사용자가 제공한 UI 시안(스타일 참고용)
- 방식: OpenAI 기본 이미지 생성 도구
- 공통 프롬프트: 텍스트/아이콘 없는 정면 픽셀 아트 UI, 짙은 남색 내부, 왕실 파랑 하이라이트, 앤티크 골드 테두리, 실제 투명 배경, 안티앨리어싱/워터마크 없음
- 개별 대상: 대형 프레임, 파랑/골드/청록 버튼, 사각 버튼, 헤더 탭, 인셋 패널, 구분선, 세로 배너, 골드 크레스트
