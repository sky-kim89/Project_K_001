// ============================================================
//  ItemIconGenerator.cs  [Editor Only]
//  Tools > Project K > 에셋 > Generate Item Icons
//
//  eItem 열거형 항목별 48×48 PNG 아이콘 생성.
//  저장 경로: Assets/_project/3.Textures/Icons/Items/
// ============================================================
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ItemIconGenerator
{
    const string ITEM_PATH = "Assets/_project/3.Textures/Icons/Items";

    // ── 팔레트 ────────────────────────────────────────────────
    static Color32 H(string h)
    {
        h = h.TrimStart('#');
        return new Color32(
            Convert.ToByte(h.Substring(0, 2), 16),
            Convert.ToByte(h.Substring(2, 2), 16),
            Convert.ToByte(h.Substring(4, 2), 16), 255);
    }
    static Color32 HA(string h, byte a)
    {
        var c = H(h); c.a = a; return c;
    }
    static Color32 Lerp(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);

    // ═══════════════════════════════════════════════════════
    //  진입점
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/에셋/Generate Item Icons")]
    public static void GenerateAll()
    {
        EnsureDir(ITEM_PATH);

        Save("item_gold.png",                 DrawGold);
        Save("item_gem.png",                  DrawGem);
        Save("item_energy.png",               DrawEnergy);
        Save("item_stamina.png",              DrawStamina);
        Save("item_honor.png",                DrawHonor);
        Save("item_expbook.png",              DrawExpBook);
        Save("item_battlestone.png",          DrawBattleStone);
        Save("item_skillscroll.png",          DrawSkillScroll);
        Save("item_general_upgrade_stone.png",DrawGeneralUpgradeStone);
        Save("item_equip_upgrade_stone.png",  DrawEquipUpgradeStone);
        Save("item_equipbox.png",             DrawEquipBox);
        Save("item_soldier_shard.png",        DrawSoldierShard);
        Save("item_reincarnation_point.png",  DrawReincarnationPoint);

        AssetDatabase.Refresh();
        ApplySpriteImport(ITEM_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[ItemIconGenerator] 아이템 아이콘 13종 생성 완료.");
    }

    // ═══════════════════════════════════════════════════════
    //  ■ 아이콘 드로잉
    // ═══════════════════════════════════════════════════════

    // ── Gold (금화) ───────────────────────────────────────
    static void DrawGold(P p)
    {
        p.BgGrad(H("1A0E00"), H("3D2200"));
        p.RoundedBorder(10, 2, H("8B6000"));
        // 바깥 금테
        p.FillCircle(24, 24, 16, H("C8920A"));
        p.FillCircle(24, 24, 14, H("F0B800"));
        // 안쪽 릴리프 (동심원)
        p.DrawCircle(24, 24, 11, 2, H("D4A000"));
        // 코인 면
        p.FillCircle(24, 24, 9, H("E8CC44"));
        // "G" 각인 느낌 (수평+수직 줄)
        p.FillRect(20, 22, 8, 4, H("C8920A"));
        p.FillRect(24, 20, 4, 8, H("C8920A"));
        p.FillCircle(24, 24, 3, H("E8CC44"));
        // 광택
        p.FillCircleAlpha(20, 18, 4, HA("FFFFFF", 90));
        p.FillCircle(20, 18, 2, HA("FFFFFF", 160));
    }

    // ── Gem (보석) ────────────────────────────────────────
    static void DrawGem(P p)
    {
        p.BgGrad(H("050826"), H("0D1A50"));
        p.RoundedBorder(10, 2, H("2244AA"));
        // 다이아몬드 형태 (팔각형 근사)
        int cx = 24, cy = 26;
        // 다이아몬드 채우기
        FillDiamond(p, cx, cy, 16, 12, H("2266DD"));
        // 면 하이라이트
        FillDiamond(p, cx, cy - 2, 14, 6, H("4488FF"));
        p.FillTri(cx - 8, cy - 6, cx + 8, cy - 6, cx, cy - 14, H("88BBFF"));
        // 상단 크라운 면
        p.FillTri(cx - 10, cy - 6, cx - 6, cy - 14, cx, cy - 10, HA("AACCFF", 180));
        p.FillTri(cx + 10, cy - 6, cx + 6, cy - 14, cx, cy - 10, HA("AACCFF", 120));
        // 광택
        p.FillCircleAlpha(cx - 4, cy - 10, 3, HA("FFFFFF", 200));
        p.BlendPixel(cx - 3, cy - 11, HA("FFFFFF", 255));
    }

    // ── Energy (에너지) ──────────────────────────────────
    static void DrawEnergy(P p)
    {
        p.BgGrad(H("080520"), H("1A0C44"));
        p.RoundedBorder(10, 2, H("5533AA"));
        // 번개 글로우
        p.FillCircleAlpha(24, 24, 18, HA("FFAA00", 35));
        p.FillCircleAlpha(24, 24, 12, HA("FFCC44", 45));
        // 번개 형태 (지그재그)
        // 상단 → 중앙 → 하단
        int[] bx = { 28, 22, 30, 18, 26 };
        int[] by = { 8,  20, 26, 36, 42 };
        for (int i = 0; i < bx.Length - 1; i++)
            p.DrawLine(bx[i], by[i], bx[i + 1], by[i + 1], H("FFCC44"), 4);
        for (int i = 0; i < bx.Length - 1; i++)
            p.DrawLine(bx[i], by[i], bx[i + 1], by[i + 1], H("FFF0AA"), 2);
        // 스파크 파티클
        p.FillCircle(32, 14, 2, H("FFAA22"));
        p.FillCircle(16, 30, 2, H("FFAA22"));
        p.FillCircle(34, 30, 2, HA("FFCC44", 150));
    }

    // ── Stamina (스태미나) ────────────────────────────────
    static void DrawStamina(P p)
    {
        p.BgGrad(H("02100E"), H("063028"));
        p.RoundedBorder(10, 2, H("1A7A5A"));
        // 물방울 (하단 원 + 상단 삼각)
        int cx = 24, bot = 36;
        p.FillCircle(cx, bot, 11, H("1A88CC"));
        p.FillCircle(cx, bot, 9,  H("33AAEE"));
        // 물방울 상단 뾰족 부분
        p.FillTri(cx - 7, bot - 8, cx + 7, bot - 8, cx, 10, H("1A88CC"));
        p.FillTri(cx - 5, bot - 8, cx + 5, bot - 8, cx, 13, H("33AAEE"));
        // 광택 (왼쪽 하이라이트)
        p.FillCircleAlpha(cx - 4, bot - 2, 4, HA("AADDFF", 180));
        p.FillCircle(cx - 4, bot - 2, 2,      HA("FFFFFF", 200));
        // 물결 라인
        p.DrawLine(cx - 4, bot + 3, cx + 4, bot + 3, HA("FFFFFF", 60), 1);
    }

    // ── Honor (명예) ─────────────────────────────────────
    static void DrawHonor(P p)
    {
        p.BgGrad(H("0C0520"), H("200A44"));
        p.RoundedBorder(10, 2, H("5522AA"));
        // 방패 형태
        FillShield(p, 24, 10, 22, 28, H("3A1A66"));
        DrawShieldOutline(p, 24, 10, 22, 28, H("AA55FF"), 2);
        FillShield(p, 24, 12, 18, 24, H("4A2288"));
        // 별
        DrawStar(p, 24, 26, 10, 5, H("FFD700"), H("FFA500"));
        // 상단 월계관 느낌
        p.FillCircle(24, 10, 3, H("AA55FF"));
        p.FillCircle(24, 10, 1, H("FFFFFF"));
    }

    // ── ExpBook (경험서) ──────────────────────────────────
    static void DrawExpBook(P p)
    {
        p.BgGrad(H("120A04"), H("2A1608"));
        p.RoundedBorder(10, 2, H("664422"));
        // 책 표지 (약간 열린 상태)
        // 왼쪽 표지
        p.FillRRect(8,  10, 16, 30, 2, H("5A2E0A"));
        p.FillRRect(10, 12, 12, 26, 1, H("7A4020"));
        // 오른쪽 표지
        p.FillRRect(26, 10, 14, 30, 2, H("5A2E0A"));
        // 내지 (오른쪽)
        p.FillRect(27, 12, 11, 26, H("F0E8D0"));
        // 줄 (텍스트 느낌)
        for (int y = 15; y <= 35; y += 4)
            p.DrawLine(29, y, 36, y, HA("886644", 100), 1);
        // 책 등 (중앙)
        p.FillRect(23, 10, 4, 30, H("3A1A08"));
        p.DrawLine(24, 10, 24, 40, H("8B6040"), 1);
        // 금 장식 테두리
        p.DrawLine(9, 10, 9, 40, HA("CC9944", 120), 1);
        // 글로우
        p.FillCircleAlpha(28, 24, 8, HA("FFCC88", 30));
        // 경험치 느낌 (별 파티클)
        p.FillCircle(34, 16, 2, H("FFDD44"));
        p.FillCircle(32, 36, 2, HA("FFDD44", 180));
    }

    // ── BattleStone (전투석) ─────────────────────────────
    static void DrawBattleStone(P p)
    {
        p.BgGrad(H("050A0E"), H("0E1E2A"));
        p.RoundedBorder(10, 2, H("225577"));
        // 결정 형태 (6각형 기둥)
        int cx = 24, cy = 26;
        // 기둥 몸체
        p.FillRect(cx - 7, cy - 14, 14, 22, H("2255AA"));
        // 상단 육각 뾰족
        p.FillTri(cx - 7, cy - 14, cx + 7, cy - 14, cx, cy - 22, H("4477CC"));
        // 하단 육각 뾰족
        p.FillTri(cx - 7, cy + 8, cx + 7, cy + 8, cx, cy + 16, H("1A3A7A"));
        // 면 하이라이트 (왼쪽 밝음)
        p.FillRect(cx - 7, cy - 14, 4, 22, HA("88BBFF", 100));
        p.FillTri(cx - 7, cy - 14, cx - 3, cy - 14, cx - 7, cy - 10, HA("88BBFF", 80));
        // 내부 광택
        p.FillRect(cx - 3, cy - 12, 2, 18, HA("FFFFFF", 60));
        p.BlendPixel(cx - 2, cy - 6, HA("FFFFFF", 180));
    }

    // ── SkillScroll (스킬 서적) ───────────────────────────
    static void DrawSkillScroll(P p)
    {
        p.BgGrad(H("120A02"), H("2E1804"));
        p.RoundedBorder(10, 2, H("774422"));
        // 두루마리 몸통 (양쪽 말린 부분)
        p.FillRRect(10, 16, 28, 18, 2, H("D4B878"));
        // 두루마리 내지
        p.FillRect(12, 17, 24, 16, H("EED898"));
        // 텍스트 줄
        p.DrawLine(15, 20, 33, 20, HA("886644", 150), 1);
        p.DrawLine(15, 24, 33, 24, HA("886644", 130), 1);
        p.DrawLine(15, 28, 28, 28, HA("886644", 110), 1);
        // 왼쪽 말린 봉
        p.FillRRect(7,  14, 6, 22, 3, H("8B5522"));
        p.DrawLine(8, 14, 8, 36, H("AA7744"), 1);
        // 오른쪽 말린 봉
        p.FillRRect(35, 14, 6, 22, 3, H("8B5522"));
        p.DrawLine(36, 14, 36, 36, H("AA7744"), 1);
        // 상단/하단 봉 끝 장식
        p.FillCircle(10, 14, 4, H("AA7744"));
        p.FillCircle(10, 36, 4, H("AA7744"));
        p.FillCircle(38, 14, 4, H("AA7744"));
        p.FillCircle(38, 36, 4, H("AA7744"));
        // 마법 룬 느낌 (중앙)
        p.FillCircle(24, 24, 3, HA("CC88FF", 180));
        p.FillCircle(24, 24, 1, H("FFFFFF"));
    }

    // ── GeneralUpgradeStone (장군 강화석) ────────────────
    static void DrawGeneralUpgradeStone(P p)
    {
        p.BgGrad(H("0A0A0A"), H("1E1E1E"));
        p.RoundedBorder(10, 2, H("666666"));
        // 돌 베이스
        p.FillCircle(24, 28, 14, H("555555"));
        p.FillCircle(24, 28, 12, H("6A6A6A"));
        // 돌 표면 질감
        p.DrawLine(18, 24, 22, 20, HA("888888", 80), 1);
        p.DrawLine(26, 22, 30, 26, HA("888888", 60), 1);
        // 금 별 (상단)
        DrawStar(p, 24, 16, 9, 4, H("FFD700"), H("CC8800"));
        // 강화 상향 화살표 (돌 위)
        p.FillTri(24, 22, 20, 28, 28, 28, H("FFCC22"));
        p.FillRect(22, 28, 4, 6, H("FFCC22"));
        // 파티클
        p.FillCircle(14, 20, 2, HA("FFD700", 180));
        p.FillCircle(34, 22, 2, HA("FFD700", 150));
        p.FillCircle(18, 36, 2, HA("FFD700", 120));
    }

    // ── EquipUpgradeStone (장비 강화석) ──────────────────
    static void DrawEquipUpgradeStone(P p)
    {
        p.BgGrad(H("020810"), H("061828"));
        p.RoundedBorder(10, 2, H("334466"));
        // 결정 (작게, 하단)
        p.FillRect(20, 28, 8, 10, H("2255AA"));
        p.FillTri(20, 28, 28, 28, 24, 21, H("4477CC"));
        p.FillTri(20, 38, 28, 38, 24, 43, H("1A3A7A"));
        p.FillRect(20, 29, 2, 8, HA("88BBFF", 80));
        // 망치 (상단)
        // 자루
        p.DrawLine(20, 26, 32, 14, H("996633"), 3);
        p.DrawLine(20, 26, 32, 14, H("BB8844"), 1);
        // 망치 헤드
        p.FillRRect(28, 9, 12, 8, 2, H("AAAAAA"));
        p.FillRect(29, 10, 10, 3, HA("DDDDDD", 180));
        p.FillRect(29, 13, 10, 1, HA("777777", 150));
        // 충격 파티클
        p.FillCircleAlpha(22, 26, 5, HA("AADDFF", 80));
        p.FillCircle(16, 30, 2, HA("88BBFF", 150));
        p.FillCircle(30, 26, 2, HA("88BBFF", 130));
    }

    // ── SoldierShard (용병조각) ───────────────────────────
    static void DrawSoldierShard(P p)
    {
        p.BgGrad(H("050C18"), H("0A1E38"));
        p.RoundedBorder(10, 2, H("1A5A88"));
        // 글로우 배경
        p.FillCircleAlpha(24, 24, 20, HA("1177AA", 55));
        p.FillCircleAlpha(24, 24, 13, HA("22AACC", 45));
        // 마름모 조각 몸체
        FillDiamond(p, 24, 26, 14, 17, H("0D3A5A"));
        FillDiamond(p, 24, 26, 12, 15, H("1A5888"));
        // 마름모 테두리 (빛나는 엣지)
        p.DrawLine(24,  9, 10, 26, H("33BBDD"), 1);
        p.DrawLine(10, 26, 24, 43, H("33BBDD"), 1);
        p.DrawLine(24,  9, 38, 26, H("22AACC"), 1);
        p.DrawLine(38, 26, 24, 43, H("22AACC"), 1);
        // 병사 실루엣 (머리 + 몸통)
        p.FillCircle(24, 17, 5, H("88DDFF")); // 머리
        p.FillTri(17, 35, 31, 35, 24, 22, H("66BBDD")); // 몸 삼각형
        // 균열 라인 (조각 느낌)
        p.DrawLine(22, 10, 20, 22, HA("AAEEFF", 130), 1);
        p.DrawLine(26, 11, 28, 22, HA("AAEEFF", 100), 1);
        // 파티클
        p.FillCircle(13, 13, 2, HA("44DDFF", 200));
        p.FillCircle(35, 13, 2, HA("44DDFF", 160));
        p.FillCircle(13, 38, 2, HA("44DDFF", 120));
        // 광택 하이라이트
        p.FillCircleAlpha(16, 14, 4, HA("BBEEFF", 110));
        p.FillCircle(16, 14, 2, HA("FFFFFF", 180));
    }

    // ── Reincarnation Point (환생 포인트) ─────────────────
    static void DrawReincarnationPoint(P p)
    {
        p.BgGrad(H("100804"), H("2C1804"));
        p.RoundedBorder(10, 2, H("CC8822"));
        // 외곽 글로우
        p.FillCircleAlpha(24, 24, 18, HA("FF9900", 35));
        p.FillCircleAlpha(24, 24, 12, HA("FFBB44", 30));
        // 원형 화살표 호 (30° ~ 320°, 시계 방향)
        var gold  = H("D4A840");
        var ltGold = H("FFE080");
        for (int i = 30; i <= 320; i += 5)
        {
            float a = i * Mathf.Deg2Rad;
            int x = 24 + Mathf.RoundToInt(Mathf.Cos(a) * 14);
            int y = 24 + Mathf.RoundToInt(Mathf.Sin(a) * 14);
            p.FillCircle(x, y, 3, gold);
            int xi = 24 + Mathf.RoundToInt(Mathf.Cos(a) * 12);
            int yi = 24 + Mathf.RoundToInt(Mathf.Sin(a) * 12);
            p.FillCircle(xi, yi, 1, ltGold);
        }
        // 화살표 머리 (320° 근처)
        float ah = 320 * Mathf.Deg2Rad;
        int ax = 24 + Mathf.RoundToInt(Mathf.Cos(ah) * 14);
        int ay = 24 + Mathf.RoundToInt(Mathf.Sin(ah) * 14);
        p.FillTri(ax, ay, ax + 7, ay - 3, ax + 3, ay + 8, gold);
        // 중앙 별
        DrawStar(p, 24, 24, 7, 3, H("FFD060"), H("D4A840"));
        p.FillCircle(24, 24, 2, H("FFFFAA"));
    }

    // ── EquipBox (장비 박스) ──────────────────────────────
    static void DrawEquipBox(P p)
    {
        p.BgGrad(H("0E0802"), H("241504"));
        p.RoundedBorder(10, 2, H("8B5A22"));
        // 상자 몸통
        p.FillRRect(8, 22, 32, 20, 2, H("8B4A18"));
        p.FillRect(9, 23, 30, 18, H("A05A22"));
        // 상자 뚜껑
        p.FillRRect(8, 14, 32, 10, 2, H("7A3A10"));
        p.FillRect(9, 15, 30, 8,  H("8B4416"));
        // 금 장식 테두리
        p.FillRect(8, 22, 32, 3, H("CC8822"));
        p.DrawLine(9, 22, 38, 22, H("EEA833"), 1);
        // 경첩 (뚜껑-몸통 연결)
        p.FillCircle(14, 22, 3, H("AA7722"));
        p.FillCircle(34, 22, 3, H("AA7722"));
        // 중앙 자물쇠
        p.FillRRect(20, 19, 8, 6, 2, H("CC9922"));
        p.FillRect(22, 22, 4, 5,    H("886600"));
        // 자물쇠 고리
        DrawArc(p, 22, 19, 26, 19, H("CC9922"), 2);
        // 반짝임 (열려는 기운)
        p.FillCircleAlpha(24, 18, 6, HA("FFCC44", 50));
        p.FillCircle(30, 14, 2, HA("FFCC44", 180));
        p.FillCircle(18, 14, 2, HA("FFCC44", 150));
        // 목재 결 느낌
        p.DrawLine(10, 27, 38, 27, HA("6A3010", 80), 1);
        p.DrawLine(10, 32, 38, 32, HA("6A3010", 60), 1);
        p.DrawLine(10, 37, 38, 37, HA("6A3010", 50), 1);
    }

    // ═══════════════════════════════════════════════════════
    //  ■ 도형 헬퍼
    // ═══════════════════════════════════════════════════════

    static void FillDiamond(P p, int cx, int cy, int hw, int hh, Color32 c)
    {
        for (int y = cy - hh; y <= cy + hh; y++)
        {
            float t = 1f - Mathf.Abs(y - cy) / (float)hh;
            int w = Mathf.RoundToInt(hw * t);
            p.FillRect(cx - w, y, w * 2 + 1, 1, c);
        }
    }

    static void FillShield(P p, int cx, int top, int hw, int h, Color32 c)
    {
        int bot = top + h;
        for (int y = top; y <= bot; y++)
        {
            float t = (float)(y - top) / h;
            int w = t < 0.5f
                ? hw
                : Mathf.RoundToInt(hw * (1f - (t - 0.5f) * 2f));
            p.FillRect(cx - w, y, w * 2 + 1, 1, c);
        }
    }

    static void DrawShieldOutline(P p, int cx, int top, int hw, int h, Color32 c, int thick)
    {
        int bot = top + h;
        for (int y = top; y <= bot; y++)
        {
            float t = (float)(y - top) / h;
            int w = t < 0.5f
                ? hw
                : Mathf.RoundToInt(hw * (1f - (t - 0.5f) * 2f));
            for (int d = 0; d < thick; d++)
            {
                p.BlendPixel(cx - w - d, y, c);
                p.BlendPixel(cx + w + d, y, c);
            }
        }
        p.DrawLine(cx - hw, top, cx + hw, top, c, thick);
    }

    static void DrawStar(P p, int cx, int cy, int outerR, int innerR, Color32 outer, Color32 inner)
    {
        // 5-pointed star via triangle fan
        for (int i = 0; i < 5; i++)
        {
            float a1 = (i * 72 - 90) * Mathf.Deg2Rad;
            float a2 = ((i * 72) + 36 - 90) * Mathf.Deg2Rad;
            float a3 = ((i + 1) * 72 - 90) * Mathf.Deg2Rad;
            int ox1 = cx + Mathf.RoundToInt(Mathf.Cos(a1) * outerR);
            int oy1 = cy + Mathf.RoundToInt(Mathf.Sin(a1) * outerR);
            int ix  = cx + Mathf.RoundToInt(Mathf.Cos(a2) * innerR);
            int iy  = cy + Mathf.RoundToInt(Mathf.Sin(a2) * innerR);
            int ox2 = cx + Mathf.RoundToInt(Mathf.Cos(a3) * outerR);
            int oy2 = cy + Mathf.RoundToInt(Mathf.Sin(a3) * outerR);
            p.FillTri(ox1, oy1, ix, iy, cx, cy, outer);
            p.FillTri(ix,  iy, ox2, oy2, cx, cy, inner);
        }
    }

    static void DrawArc(P p, int x1, int y1, int x2, int y2, Color32 c, int thick)
    {
        int mcx = (x1 + x2) / 2;
        int mcy = Mathf.Min(y1, y2) - 5;
        int steps = 20, px = x1, py = y1;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps, mt = 1 - t;
            int nx = Mathf.RoundToInt(mt * mt * x1 + 2 * mt * t * mcx + t * t * x2);
            int ny = Mathf.RoundToInt(mt * mt * y1 + 2 * mt * t * mcy + t * t * y2);
            p.DrawLine(px, py, nx, ny, c, thick);
            px = nx; py = ny;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ■ 유틸
    // ═══════════════════════════════════════════════════════

    static void Save(string filename, Action<P> draw)
    {
        var painter = new P(48, 48);
        draw(painter);
        painter.Save(Path.Combine(ITEM_PATH, filename));
    }

    static void EnsureDir(string assetPath)
    {
        string full = Path.Combine(Application.dataPath, "..", assetPath);
        Directory.CreateDirectory(full);
    }

    static void ApplySpriteImport(string folder, int size)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.spritePivot         = new Vector2(0.5f, 0.5f);
            importer.filterMode          = FilterMode.Bilinear;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize      = 128;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ■ Painter (픽셀 그리기 헬퍼) — IconGenerator 와 동일 구조
    // ═══════════════════════════════════════════════════════
    class P
    {
        public readonly int W, H;
        readonly Color32[] _px;

        public P(int w, int h) { W = w; H = h; _px = new Color32[w * h]; }

        int Idx(int x, int y) => (H - 1 - y) * W + x;

        public void BlendPixel(int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            int i = Idx(x, y);
            if (c.a == 255) { _px[i] = c; return; }
            float a = c.a / 255f, ea = _px[i].a / 255f;
            float oa = a + ea * (1 - a);
            if (oa < 0.001f) { _px[i] = default; return; }
            _px[i] = new Color32(
                (byte)Mathf.RoundToInt((c.r * a + _px[i].r * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt((c.g * a + _px[i].g * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt((c.b * a + _px[i].b * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt(oa * 255));
        }

        public void BgGrad(Color32 dark, Color32 mid)
        {
            int cx = W / 2, cy = H / 2;
            float maxD = Mathf.Sqrt(cx * cx + cy * cy);
            const int R = 10;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int dx = 0, dy = 0;
                if (x < R && y < R)             { dx = R - x; dy = R - y; }
                else if (x >= W - R && y < R)   { dx = x - (W - R - 1); dy = R - y; }
                else if (x < R && y >= H - R)   { dx = R - x; dy = y - (H - R - 1); }
                else if (x >= W - R && y >= H - R) { dx = x - (W - R - 1); dy = y - (H - R - 1); }
                if (dx * dx + dy * dy > R * R && (dx > 0 || dy > 0)) continue;
                float t = Mathf.Clamp01(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxD);
                BlendPixel(x, y, Color32.Lerp(mid, dark, t * t));
            }
        }

        public void RoundedBorder(int radius, int thick, Color32 c)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int dx = 0, dy = 0;
                if (x < radius && y < radius)               { dx = radius - x - 1; dy = radius - y - 1; }
                else if (x >= W - radius && y < radius)     { dx = x - (W - radius); dy = radius - y - 1; }
                else if (x < radius && y >= H - radius)     { dx = radius - x - 1; dy = y - (H - radius); }
                else if (x >= W - radius && y >= H - radius){ dx = x - (W - radius); dy = y - (H - radius); }

                bool onEdge = (dx > 0 || dy > 0)
                    ? (dx * dx + dy * dy >= (radius - thick) * (radius - thick) && dx * dx + dy * dy <= radius * radius)
                    : (x < thick || x >= W - thick || y < thick || y >= H - thick);
                if (onEdge) BlendPixel(x, y, c);
            }
        }

        public void FillRect(int x, int y, int w, int h, Color32 c)
        {
            for (int fy = y; fy < y + h; fy++)
            for (int fx = x; fx < x + w; fx++)
                BlendPixel(fx, fy, c);
        }

        public void FillRRect(int x, int y, int w, int h, int r, Color32 c)
        {
            for (int fy = y; fy < y + h; fy++)
            for (int fx = x; fx < x + w; fx++)
            {
                int dx = 0, dy = 0;
                if (fx < x + r && fy < y + r)           { dx = fx - (x + r); dy = fy - (y + r); }
                else if (fx >= x + w - r && fy < y + r) { dx = fx - (x + w - r - 1); dy = fy - (y + r); }
                else if (fx < x + r && fy >= y + h - r) { dx = fx - (x + r); dy = fy - (y + h - r - 1); }
                else if (fx >= x+w-r && fy >= y+h-r)    { dx = fx - (x+w-r-1); dy = fy - (y+h-r-1); }
                if (dx * dx + dy * dy <= r * r || (dx == 0 && dy == 0))
                    BlendPixel(fx, fy, c);
            }
        }

        public void FillCircle(int cx, int cy, int r, Color32 c)
        {
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    BlendPixel(x, y, c);
        }

        public void FillCircleAlpha(int cx, int cy, int r, Color32 c)
            => FillCircle(cx, cy, r, c);

        public void DrawCircle(int cx, int cy, int r, int thick, Color32 c)
        {
            for (int y = cy - r - thick; y <= cy + r + thick; y++)
            for (int x = cx - r - thick; x <= cx + r + thick; x++)
            {
                int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                int r0 = r - thick, r1 = r + thick;
                if (d2 >= r0 * r0 && d2 <= r1 * r1) BlendPixel(x, y, c);
            }
        }

        public void DrawLine(int x0, int y0, int x1, int y1, Color32 c, int thick)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy, x = x0, y = y0;
            while (true)
            {
                for (int ty = -thick / 2; ty <= thick / 2; ty++)
                for (int tx = -thick / 2; tx <= thick / 2; tx++)
                    BlendPixel(x + tx, y + ty, c);
                if (x == x1 && y == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 <  dx) { err += dx; y += sy; }
            }
        }

        public void FillTri(int x0, int y0, int x1, int y1, int x2, int y2, Color32 c)
        {
            int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
            int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
            int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
            int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d0 = Sign(x, y, x0, y0, x1, y1);
                float d1 = Sign(x, y, x1, y1, x2, y2);
                float d2 = Sign(x, y, x2, y2, x0, y0);
                bool hasNeg = d0 < 0 || d1 < 0 || d2 < 0;
                bool hasPos = d0 > 0 || d1 > 0 || d2 > 0;
                if (!(hasNeg && hasPos)) BlendPixel(x, y, c);
            }
        }
        static float Sign(int px, int py, int ax, int ay, int bx, int by)
            => (px - bx) * (ay - by) - (ax - bx) * (py - by);

        public void Save(string assetPath)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(_px);
            tex.Apply();
            string full = Path.Combine(Application.dataPath, "..", assetPath);
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
