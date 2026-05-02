// ============================================================
//  UIScale.cs
//  UI 사이즈 공통 상수.
//  모든 Creator 스크립트가 이 값을 참조한다.
//
//  레이아웃별 세부 수치(패딩, 패널 높이 등)는
//  각 Creator 안에서 자유롭게 결정한다.
// ============================================================

public static class UIScale
{
    // ── 기준 해상도 (Canvas Scaler) ───────────────────────────
    public const float RefWidth  = 1080f;
    public const float RefHeight = 1920f;
    public const float Match     = 0.5f;   // matchWidthOrHeight

    // ── 폰트 크기 ──────────────────────────────────────────────
    public const float FontSm = 30f;   // 보조 설명, 수치
    public const float FontMd = 40f;   // 일반 텍스트, 탭 레이블
    public const float FontLg = 54f;   // 섹션 제목
    public const float FontXl = 72f;   // 팝업 강조 제목

    // ── 버튼 높이 ──────────────────────────────────────────────
    public const float BtnSm  = 90f;   // 탭, 소형 버튼
    public const float BtnMd  = 120f;  // 일반 버튼
    public const float BtnLg  = 150f;  // 주요 액션 (전투 시작 등)

    // ── 아이콘 크기 (정사각형) ─────────────────────────────────
    public const float IconSm = 64f;   // 재화 아이콘, 인라인 아이콘
    public const float IconMd = 96f;   // 카드 썸네일, 패널 아이콘
}
