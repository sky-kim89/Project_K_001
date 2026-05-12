// ============================================================
//  IconGenerator.cs
//  Tools > Project K > Generate Icons 메뉴에서 실행.
//  직업 아이콘(64×64) 4장, 액티브 스킬 아이콘(48×48) 20장을
//  PNG로 생성 → Assets/_project/3.Textures/Icons/ 에 저장.
// ============================================================
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class IconGenerator
{
    const string CLASS_PATH   = "Assets/_project/3.Textures/Icons/Classes";
    const string SKILL_PATH   = "Assets/_project/3.Textures/Icons/Skills";
    const string ABILITY_PATH = "Assets/_project/3.Textures/Icons/Abilities";

    // ── 컬러 팔레트 ───────────────────────────────────────────
    static readonly Color32 Knight_BgDark  = Hex("1A0606"); static readonly Color32 Knight_BgMid   = Hex("4A1010");
    static readonly Color32 Knight_Rim     = Hex("8B4444");
    static readonly Color32 Archer_BgDark  = Hex("060E02"); static readonly Color32 Archer_BgMid   = Hex("1A4A1A");
    static readonly Color32 Archer_Rim     = Hex("448B44");
    static readonly Color32 Mage_BgDark    = Hex("080520"); static readonly Color32 Mage_BgMid     = Hex("251A6A");
    static readonly Color32 Mage_Rim       = Hex("6644CC");
    static readonly Color32 Shield_BgDark  = Hex("040E0E"); static readonly Color32 Shield_BgMid   = Hex("104040");
    static readonly Color32 Shield_Rim     = Hex("448B8B");

    static readonly Color32 Silver  = Hex("D0D0D0");
    static readonly Color32 Gold    = Hex("D4A840");
    static readonly Color32 DkGold  = Hex("8B6820");
    static readonly Color32 Wood    = Hex("7A4020");
    static readonly Color32 White   = Hex("FFFFFF");
    static readonly Color32 Green   = Hex("2ECC71");
    static readonly Color32 Purple  = Hex("9933FF");
    static readonly Color32 Red     = Hex("E74C3C");
    static readonly Color32 Teal    = Hex("22CCDD");
    static readonly Color32 Orange  = Hex("FF8833");
    static readonly Color32 Yellow  = Hex("FFCC44");

    // ═══════════════════════════════════════════════════════
    //  메뉴 진입점
    // ═══════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════
    //  어빌리티 아이콘 메뉴
    // ═══════════════════════════════════════════════════════

    [MenuItem("Tools/Project K/에셋/Generate Ability Icons")]
    public static void GenerateAbilityIcons()
    {
        EnsureDir(ABILITY_PATH);

        // Normal (A01~A15) — dark-blue bg, grade dot absent
        Save(48, 48, ABILITY_PATH + "/ability_a01.png", p => DrawAbilIcon(p, DrawSymHp,     TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a02.png", p => DrawAbilIcon(p, DrawSymAtk,    TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a03.png", p => DrawAbilIcon(p, DrawSymASpd,   TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a04.png", p => DrawAbilIcon(p, DrawSymMSpd,   TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a05.png", p => DrawAbilIcon(p, DrawSymDef,    TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a06.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymHpSm, TargetCol(1), false));
        Save(48, 48, ABILITY_PATH + "/ability_a07.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymASpdSm, TargetCol(2), false));
        Save(48, 48, ABILITY_PATH + "/ability_a08.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymCdrSm,  TargetCol(3), false));
        Save(48, 48, ABILITY_PATH + "/ability_a09.png", p => DrawAbilIconDual(p, DrawSymDef,  DrawSymHpSm, TargetCol(4), false));
        Save(48, 48, ABILITY_PATH + "/ability_a10.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(5), false));
        Save(48, 48, ABILITY_PATH + "/ability_a11.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymAtkSm, TargetCol(6), false));
        Save(48, 48, ABILITY_PATH + "/ability_a12.png", p => DrawAbilIconDual(p, DrawSymCrown, DrawSymDefSm, TargetCol(7), false));
        Save(48, 48, ABILITY_PATH + "/ability_a13.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(8), false));
        Save(48, 48, ABILITY_PATH + "/ability_a14.png", p => DrawAbilIcon(p, DrawSymCrit,   TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a15.png", p => DrawAbilIcon(p, DrawSymRange,  TargetCol(0), false));

        // Advanced (B01~B12) — amber bg, gold star present
        Save(48, 48, ABILITY_PATH + "/ability_b01.png", p => DrawAbilIcon(p, DrawSymHp,     TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b02.png", p => DrawAbilIcon(p, DrawSymAtk,    TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b03.png", p => DrawAbilIcon(p, DrawSymASpd,   TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b04.png", p => DrawAbilIcon(p, DrawSymMSpd,   TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b05.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymHpSm, TargetCol(1), true));
        Save(48, 48, ABILITY_PATH + "/ability_b06.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymASpdSm, TargetCol(2), true));
        Save(48, 48, ABILITY_PATH + "/ability_b07.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymCdrSm,  TargetCol(3), true));
        Save(48, 48, ABILITY_PATH + "/ability_b08.png", p => DrawAbilIconDual(p, DrawSymDef,  DrawSymHpSm, TargetCol(4), true));
        Save(48, 48, ABILITY_PATH + "/ability_b09.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(5), true));
        Save(48, 48, ABILITY_PATH + "/ability_b10.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymAtkSm, TargetCol(6), true));
        Save(48, 48, ABILITY_PATH + "/ability_b11.png", p => DrawAbilIconDual(p, DrawSymCrown, DrawSymDefSm, TargetCol(7), true));
        Save(48, 48, ABILITY_PATH + "/ability_b12.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(8), true));

        // Special (C01~C04) — dark crimson bg, diamond marker
        Save(48, 48, ABILITY_PATH + "/ability_c01.png", DrawSpecialC01);
        Save(48, 48, ABILITY_PATH + "/ability_c02.png", DrawSpecialC02);
        Save(48, 48, ABILITY_PATH + "/ability_c03.png", DrawSpecialC03);
        Save(48, 48, ABILITY_PATH + "/ability_c04.png", DrawSpecialC04);

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(ABILITY_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 어빌리티 아이콘 31장 생성 완료.");
    }

    // ─────────────────────────────────────────────────────
    //  어빌리티 아이콘 공용 프레임
    // ─────────────────────────────────────────────────────

    // target index → border color: 0=All(white) 1=Knight(red) 2=Archer(green) 3=Mage(purple)
    //   4=Shield(teal) 5=Melee(orange) 6=Ranged(cyan) 7=General(gold) 8=Soldier(steel)
    static Color32 TargetCol(int idx)
    {
        switch (idx)
        {
            case 1: return Hex("CC3333");
            case 2: return Hex("33BB44");
            case 3: return Hex("9933FF");
            case 4: return Hex("22BBCC");
            case 5: return Hex("FF8833");
            case 6: return Hex("33CCFF");
            case 7: return Hex("D4A840");
            case 8: return Hex("7799BB");
            default: return Hex("CCCCDD");
        }
    }

    static void DrawAbilIcon(P p, Action<P, Color32, bool> symDraw, Color32 borderCol, bool advanced)
    {
        AbilBg(p, advanced);
        p.RoundedBorder(8, 2, borderCol);
        symDraw(p, borderCol, advanced);
        if (advanced) DrawAdvancedStar(p);
    }

    static void DrawAbilIconDual(P p, Action<P, Color32, bool> symMain, Action<P> symSub,
                                   Color32 borderCol, bool advanced)
    {
        AbilBg(p, advanced);
        p.RoundedBorder(8, 2, borderCol);
        symMain(p, borderCol, advanced);
        symSub(p);
        if (advanced) DrawAdvancedStar(p);
    }

    static void AbilBg(P p, bool advanced)
    {
        if (advanced) p.BgGradient(Hex("0E0902"), Hex("2A1E06"));
        else          p.BgGradient(Hex("060810"), Hex("0E1830"));
    }

    // 고급 등급 표시 — 우상단 금별
    static void DrawAdvancedStar(P p)
    {
        int x = 40, y = 8;
        p.FillCircle(x, y, 5, Hex("2A1E06"));
        // 별 5각
        for (int i = 0; i < 5; i++)
        {
            float a1 = (i * 72 - 90) * Mathf.Deg2Rad;
            float a2 = (i * 72 - 90 + 36) * Mathf.Deg2Rad;
            int ox = x + Mathf.RoundToInt(Mathf.Cos(a1) * 4);
            int oy = y + Mathf.RoundToInt(Mathf.Sin(a1) * 4);
            int ix = x + Mathf.RoundToInt(Mathf.Cos(a2) * 2);
            int iy = y + Mathf.RoundToInt(Mathf.Sin(a2) * 2);
            p.DrawLine(ox, oy, ix, iy, Gold, 1);
        }
        p.FillCircle(x, y, 2, Gold);
    }

    // ─────────────────────────────────────────────────────
    //  특수(Special) 어빌리티 아이콘
    // ─────────────────────────────────────────────────────

    static void SpecialBg(P p) => p.BgGradient(Hex("120008"), Hex("320020"));

    // 특수 등급 마커 — 우상단 보라 다이아몬드
    static void DrawSpecialDiamond(P p)
    {
        int x = 40, y = 8;
        p.FillCircle(x, y, 5, Hex("120008"));
        p.FillTri(x, y - 5, x - 4, y, x + 4, y, Hex("DD44FF"));
        p.FillTri(x, y + 5, x - 4, y, x + 4, y, Hex("AA22CC"));
        p.DrawLine(x, y - 5, x - 4, y, Hex("EE88FF"), 1);
        p.DrawLine(x, y - 5, x + 4, y, Hex("EE88FF"), 1);
    }

    // C01 — 흡혈 강습 (OnAttack: 검 + 핏방울)
    static void DrawSpecialC01(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("CC2244"));
        // 검
        p.FillTri(24, 6, 20, 11, 28, 11, Silver);
        p.FillRect(22, 11, 5, 20, Silver);
        p.FillRect(21, 11, 3, 18, Tint(Silver, White, 0.4f));
        p.FillRRect(16, 31, 16, 5, 2, Hex("AA2222"));
        p.FillRRect(22, 36, 5, 6, 2, Wood);
        // 핏방울 오른쪽 하단
        var blood = Hex("EE1133");
        p.FillCircle(37, 38, 7, blood);
        p.FillTri(37, 25, 32, 38, 42, 38, blood);
        p.FillCircleAlpha(34, 30, 2, new Color32(255, 120, 140, 120));
        DrawSpecialDiamond(p);
    }

    // C02 — 철갑 반응 (OnHit: 방패 + 번개)
    static void DrawSpecialC02(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("2266CC"));
        var sc = Hex("3388EE");
        // 방패
        FillShieldShape(p, 22, 7, 44, 10, Hex("0A1A2A"), Hex("123050"));
        DrawShieldOutline(p, 22, 7, 44, 10, sc, 2);
        // 번개 (방패 위)
        p.FillTri(24, 13, 20, 26, 25, 26, Hex("FFEE33"));
        p.FillTri(23, 26, 28, 26, 24, 38, Hex("FFEE33"));
        p.DrawLine(24, 13, 20, 26, Hex("FFFFFF"), 1);
        p.DrawLine(28, 26, 24, 38, Hex("FFFFAA"), 1);
        // 충격 스파크
        p.DrawLine(10, 34, 16, 30, sc, 2);
        p.DrawLine(10, 40, 17, 38, sc, 1);
        p.DrawLine(36, 34, 42, 30, sc, 2);
        DrawSpecialDiamond(p);
    }

    // C03 — 처치 연쇄 (OnEnemyKill: 검 + 연쇄 링)
    static void DrawSpecialC03(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("CC8800"));
        var gc = Hex("FFB800");
        // 검 (대각)
        p.DrawLine(10, 42, 38, 10, Silver, 5);
        p.DrawLine(10, 42, 38, 10, Tint(Silver, White, 0.5f), 2);
        p.FillTri(38, 10, 32, 12, 36, 16, Silver);
        p.FillRRect(8, 40, 8, 4, 2, Gold);
        // 연쇄 링 오른쪽 하단 (두 개)
        p.DrawCircle(34, 36, 6, 2, gc);
        p.DrawCircle(42, 38, 5, 2, Tint(gc, Hex("000000"), 0.3f));
        p.DrawLine(34, 30, 34, 34, gc, 2);
        p.DrawLine(42, 33, 42, 36, Tint(gc, Hex("000000"), 0.3f), 2);
        // 처치 X 표시
        p.DrawLine(16, 28, 24, 20, Red, 2);
        p.DrawLine(24, 28, 16, 20, Red, 2);
        DrawSpecialDiamond(p);
    }

    // C04 — 희생의 힘 (OnSoldierDeath: 병사 실루엣 + 상승 화살표)
    static void DrawSpecialC04(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("8833CC"));
        var pc = Hex("BB44EE");
        // 병사 실루엣 (검은 작은 사람 형태, 중앙 하단)
        p.FillCircle(24, 30, 5, Hex("221133"));
        p.FillRRect(20, 35, 8, 10, 2, Hex("221133"));
        p.DrawCircle(24, 30, 5, 1, Hex("553366"));
        p.DrawCircle(24, 40, 5, 1, Hex("553366"));
        // 상승 화살표 (중앙 → 위)
        p.DrawLine(24, 26, 24, 8, pc, 3);
        p.FillTri(24, 5, 18, 14, 30, 14, pc);
        // 강화 광선 (방사형)
        var glow = new Color32(187, 68, 238, 100);
        p.DrawLine(24, 10, 14, 4,  glow, 1);
        p.DrawLine(24, 10, 34, 4,  glow, 1);
        p.DrawLine(24, 10, 8,  14, glow, 1);
        p.DrawLine(24, 10, 40, 14, glow, 1);
        DrawSpecialDiamond(p);
    }

    // ─────────────────────────────────────────────────────
    //  스텟 심볼 그리기
    // ─────────────────────────────────────────────────────

    // 체력 — 하트
    static void DrawSymHp(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("FF5577") : Hex("DD3355");
        FillHeart(p, 24, 23, 13, c);
        p.FillCircleAlpha(19, 17, 4, new Color32(255, 255, 255, 60));
    }
    static void DrawSymHpSm(P p) => FillHeart(p, 38, 36, 6, Hex("DD3355"));

    // 공격 — 검
    static void DrawSymAtk(P p, Color32 accent, bool adv)
    {
        var blade = adv ? Silver : Hex("AAAAAA");
        p.FillTri(24, 6, 20, 10, 28, 10, blade);
        p.FillRect(22, 10, 5, 22, blade);
        p.FillRect(21, 11, 3, 20, Tint(blade, White, 0.4f));
        p.FillRRect(16, 32, 16, 5, 2, Gold);
        p.FillRRect(22, 37, 5, 7, 2, Wood);
        p.FillCircle(24, 46, 4, DkGold);
    }
    static void DrawSymAtkSm(P p)
    {
        p.DrawLine(34, 30, 44, 20, Hex("AAAAAA"), 3);
        p.FillTri(34, 30, 30, 34, 37, 37, Hex("AAAAAA"));
    }

    // 공격속도 — 검 + 속도선
    static void DrawSymASpd(P p, Color32 accent, bool adv)
    {
        var lc = new Color32(accent.r, accent.g, accent.b, 120);
        p.DrawLine(6, 18, 22, 18, lc, 3);
        p.DrawLine(6, 24, 20, 24, lc, 2);
        p.DrawLine(6, 30, 22, 30, lc, 3);
        var blade = adv ? Silver : Hex("AAAAAA");
        p.DrawLine(24, 8, 42, 40, blade, 5);
        p.DrawLine(23, 8, 41, 40, Tint(blade, White, 0.5f), 2);
        p.FillTri(42, 40, 36, 38, 40, 34, blade);
        p.FillRRect(22, 6, 5, 3, 1, Gold);
    }
    static void DrawSymASpdSm(P p)
    {
        p.DrawLine(32, 34, 44, 28, Hex("CCCCCC"), 2);
        p.DrawLine(32, 40, 44, 34, new Color32(204, 204, 204, 120), 1);
    }

    // 이동속도 — 발자국 + 화살표
    static void DrawSymMSpd(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("88EEFF") : Hex("44AADD");
        p.DrawLine(10, 40, 38, 12, c, 3);
        p.FillTri(38, 12, 30, 12, 38, 20, c);
        p.DrawLine(4,  38, 14, 38, new Color32(c.r, c.g, c.b, 120), 2);
        p.DrawLine(8,  44, 18, 44, new Color32(c.r, c.g, c.b, 80),  2);
        p.DrawLine(6,  32, 16, 32, new Color32(c.r, c.g, c.b, 100), 1);
        // 발자국
        p.FillEllipse(18, 36, 4, 3, Tint(c, Hex("000000"), 0.4f));
        p.FillEllipse(28, 26, 4, 3, Tint(c, Hex("000000"), 0.4f));
    }
    static void DrawSymMSpdSm(P p)
    {
        p.DrawLine(34, 40, 44, 30, Hex("44AADD"), 2);
        p.FillTri(44, 30, 38, 30, 44, 36, Hex("44AADD"));
    }

    // 방어 — 방패
    static void DrawSymDef(P p, Color32 accent, bool adv)
    {
        var dark = adv ? Hex("1A2A1A") : Hex("1A2030");
        var mid  = adv ? Hex("2A4A2A") : Hex("1A4050");
        FillShieldShape(p, 24, 6, 46, 10, dark, mid);
        DrawShieldOutline(p, 24, 6, 46, 10, accent, 2);
        p.FillCircle(24, 28, 7, dark);
        p.DrawCircle(24, 28, 7, 1, accent);
        p.FillRRect(23, 22, 3, 12, 1, accent);
        p.FillRRect(18, 27, 12, 3, 1, accent);
    }
    static void DrawSymDefSm(P p)
    {
        FillShieldShape(p, 38, 30, 44, 28, Hex("1A2030"), Hex("1A4050"));
        DrawShieldOutline(p, 38, 30, 44, 28, Hex("22BBCC"), 1);
    }

    // 사거리 — 조준선
    static void DrawSymRange(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("FFEE88") : Hex("DDCC44");
        p.DrawCircle(24, 24, 14, 2, c);
        p.DrawCircle(24, 24, 8,  1, new Color32(c.r, c.g, c.b, 130));
        p.DrawLine(24, 6,  24, 14, c, 2);
        p.DrawLine(24, 34, 24, 42, c, 2);
        p.DrawLine(6,  24, 14, 24, c, 2);
        p.DrawLine(34, 24, 42, 24, c, 2);
        p.FillCircle(24, 24, 3, c);
        // 화살
        p.DrawLine(32, 32, 44, 44, Hex("C8A840"), 3);
        p.FillTri(44, 44, 38, 44, 44, 38, Hex("C8A840"));
    }

    // 치명 — 별+조준
    static void DrawSymCrit(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("FFDD33") : Hex("FFBB22");
        p.DrawCircle(24, 24, 16, 1, new Color32(c.r, c.g, c.b, 80));
        p.DrawLine(24, 8,  24, 40, c, 1);
        p.DrawLine(8,  24, 40, 24, c, 1);
        p.DrawLine(11, 11, 37, 37, c, 1);
        p.DrawLine(37, 11, 11, 37, c, 1);
        p.FillCircle(24, 24, 5, Hex("332200"));
        p.DrawCircle(24, 24, 5, 1, c);
        p.FillCircle(24, 24, 2, c);
        // 번쩍임
        p.FillTri(24, 5, 22, 10, 26, 10, c);
        p.FillTri(24, 43, 22, 38, 26, 38, c);
    }

    // 스킬 쿨다운 감소 — 시계 화살표 (소형, 우하단)
    static void DrawSymCdrSm(P p)
    {
        var c = Hex("9966DD");
        p.DrawCircle(38, 36, 6, 1, c);
        p.DrawLine(38, 36, 38, 30, c, 1);
        p.DrawLine(38, 36, 42, 38, c, 1);
    }

    // 왕관 — 장군 대상
    static void DrawSymCrown(P p, Color32 accent, bool adv)
    {
        var base1 = adv ? Hex("C89030") : Hex("A06820");
        var gemC  = adv ? Hex("88CCFF") : Red;
        p.FillRRect(10, 28, 28, 9, 1, base1);
        p.FillTri(10, 28, 13, 14, 17, 28, Gold);
        p.FillTri(21, 28, 24, 10, 27, 28, Gold);
        p.FillTri(31, 28, 35, 14, 38, 28, Gold);
        p.FillRect(10, 28, 28, 3, new Color32(255, 255, 255, 30));
        p.FillCircle(13, 14, 3, gemC);
        p.FillCircle(24, 10, 4, gemC);
        p.FillCircle(35, 14, 3, gemC);
        p.FillCircleAlpha(23, 9, 2, new Color32(255, 255, 255, 150));
        // 광택
        p.FillCircleAlpha(13, 13, 1, new Color32(255, 255, 255, 130));
    }

    [MenuItem("Tools/Project K/에셋/Generate Icons")]
    public static void GenerateAllIcons()
    {
        EnsureDir(CLASS_PATH);
        EnsureDir(SKILL_PATH);

        // 직업 아이콘 (64×64)
        Save(64, 64, CLASS_PATH + "/knight_icon.png",      DrawKnight);
        Save(64, 64, CLASS_PATH + "/archer_icon.png",       DrawArcher);
        Save(64, 64, CLASS_PATH + "/mage_icon.png",         DrawMage);
        Save(64, 64, CLASS_PATH + "/shieldbearer_icon.png", DrawShieldBearer);

        // 스킬 아이콘 (48×48)
        Save(48, 48, SKILL_PATH + "/skill_heavy_strike.png",      DrawHeavyStrike);
        Save(48, 48, SKILL_PATH + "/skill_volley_fire.png",        DrawVolleyFire);
        Save(48, 48, SKILL_PATH + "/skill_leap_strike.png",        DrawLeapStrike);
        Save(48, 48, SKILL_PATH + "/skill_heal_aura.png",          DrawHealAura);
        Save(48, 48, SKILL_PATH + "/skill_target_heal.png",        DrawTargetHeal);
        Save(48, 48, SKILL_PATH + "/skill_charge_soldier.png",     DrawChargeSoldier);
        Save(48, 48, SKILL_PATH + "/skill_summon_skeleton.png",    DrawSummonSkeleton);
        Save(48, 48, SKILL_PATH + "/skill_poison_zone.png",        DrawPoisonZone);
        Save(48, 48, SKILL_PATH + "/skill_meteor.png",             DrawMeteor);
        Save(48, 48, SKILL_PATH + "/skill_blizzard.png",           DrawBlizzard);
        Save(48, 48, SKILL_PATH + "/skill_sacrifice_soldier.png",  DrawSacrificeSoldier);
        Save(48, 48, SKILL_PATH + "/skill_bind.png",               DrawBind);
        Save(48, 48, SKILL_PATH + "/skill_suicide_soldier.png",    DrawSuicideSoldier);
        Save(48, 48, SKILL_PATH + "/skill_berserker.png",          DrawBerserker);
        Save(48, 48, SKILL_PATH + "/skill_iron_shield.png",        DrawIronShield);
        Save(48, 48, SKILL_PATH + "/skill_arrow_rain.png",         DrawArrowRain);
        Save(48, 48, SKILL_PATH + "/skill_battle_cry.png",         DrawBattleCry);
        Save(48, 48, SKILL_PATH + "/skill_shockwave.png",          DrawShockwave);
        Save(48, 48, SKILL_PATH + "/skill_swift_strike.png",       DrawSwiftStrike);
        Save(48, 48, SKILL_PATH + "/skill_summon_elite.png",       DrawSummonElite);

        AssetDatabase.Refresh();
        // Sprite 임포트 설정 적용
        ApplySpriteImportSettings(CLASS_PATH, 64);
        ApplySpriteImportSettings(SKILL_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 아이콘 24장 생성 완료.");
    }

    // ─────────────────────────────────────────────────────
    //  ■ 직업 아이콘
    // ─────────────────────────────────────────────────────

    static void DrawKnight(P p)
    {
        int W = p.W, H = p.H, cx = W / 2;
        p.BgGradient(Knight_BgDark, Knight_BgMid);
        p.RoundedBorder(10, 2, Knight_Rim);
        // 검 날 (넓고 듬직하게)
        p.FillTri(cx - 7, 10, cx + 7, 10, cx, 4, Silver);        // 칼끝 삼각형
        p.FillRect(cx - 7, 10, 14, 34, Silver);                   // 날 몸체
        p.FillRect(cx - 3, 11, 6, 32, Tint(Silver, White, 0.5f)); // 능선
        // 가드
        p.FillRRect(cx - 14, 44, 28, 6, 3, Gold);
        p.FillCircle(cx - 14, 47, 3, Red);
        p.FillCircle(cx + 14, 47, 3, Red);
        // 손잡이
        p.FillRRect(cx - 4, 50, 8, 10, 2, Wood);
        p.DrawLine(cx - 4, 53, cx + 4, 53, Tint(Wood, Hex("000000"), 0.35f), 1);
        p.DrawLine(cx - 4, 56, cx + 4, 56, Tint(Wood, Hex("000000"), 0.35f), 1);
        // 폼멜
        p.FillCircle(cx, 62, 5, Gold);
        p.FillCircle(cx, 62, 2, Yellow);
    }

    static void DrawArcher(P p)
    {
        int W = p.W, H = p.H;
        p.BgGradient(Archer_BgDark, Archer_BgMid);
        p.RoundedBorder(10, 2, Archer_Rim);

        // 활 — 당긴 상태 (활 몸체: 왼쪽 C자 곡선)
        var bowCol  = Hex("A07830");
        var bowHigh = Hex("D4A840");
        // 활 커브 (여러 선분으로 근사)
        DrawBowCurve(p, 22, 8, 56, bowCol, 5);   // 두꺼운 갈색
        DrawBowCurve(p, 22, 8, 56, bowHigh, 2);   // 하이라이트
        // 활 팁
        p.FillCircle(22, 8,  4, Gold);
        p.FillCircle(22, 56, 4, Gold);
        // 손잡이
        p.FillRRect(19, 26, 8, 12, 3, Hex("5A3010"));
        // 시위 — V자 당겨진 형태 (시위 당긴 지점 x=46, y=32)
        p.DrawLine(22, 9,  46, 32, Hex("E8E8CC"), 2);
        p.DrawLine(22, 55, 46, 32, Hex("E8E8CC"), 2);
        p.FillCircle(46, 32, 3, Hex("E8E8CC"));
        // 화살 샤프트
        p.DrawLine(10, 32, 46, 32, Gold, 3);
        // 화살촉 (왼쪽 방향)
        p.FillTri(10, 32, 20, 27, 20, 37, Silver);
        // 깃털
        p.FillTri(44, 32, 36, 27, 38, 32, Green);
        p.FillTri(44, 32, 36, 37, 38, 32, Tint(Green, Hex("000000"), 0.3f));
    }

    // 활 커브 근사 (cubic bezier M22,8 C10,18 10,46 22,56)
    static void DrawBowCurve(P p, int x0, int y0, int y1, Color32 col, int thickness)
    {
        // 조절점: (10,18) (10,46) → 단순 근사
        int steps = 40;
        float[] bx = { x0, 10, 10, x0 };
        float[] by = { y0, y0 + (y1 - y0) * 0.22f, y0 + (y1 - y0) * 0.78f, y1 };
        int px = x0, py = y0;
        for (int i = 1; i <= steps; i++)
        {
            float t  = i / (float)steps;
            float t2 = t * t, t3 = t2 * t;
            float mt = 1 - t, mt2 = mt * mt, mt3 = mt2 * mt;
            int nx = Mathf.RoundToInt(mt3*bx[0] + 3*mt2*t*bx[1] + 3*mt*t2*bx[2] + t3*bx[3]);
            int ny = Mathf.RoundToInt(mt3*by[0] + 3*mt2*t*by[1] + 3*mt*t2*by[2] + t3*by[3]);
            p.DrawLine(px, py, nx, ny, col, thickness);
            px = nx; py = ny;
        }
    }

    static void DrawMage(P p)
    {
        int W = p.W, H = p.H;
        p.BgGradient(Mage_BgDark, Mage_BgMid);
        p.RoundedBorder(10, 2, Mage_Rim);

        var orbInner = Hex("AACCFF");
        var orbOuter = Hex("2211AA");
        var orbMid   = Hex("6644FF");

        // 구슬 글로우
        p.FillCircleAlpha(36, 18, 14, new Color32(102, 68, 255, 40));
        // 구슬 본체
        p.FillCircleGrad(36, 18, 11, orbInner, orbMid, orbOuter);
        // 구슬 하이라이트
        p.FillCircleAlpha(32, 13, 5, new Color32(255, 255, 255, 90));
        // 구슬 내부 마법 선
        p.DrawCircle(36, 18, 6, 1, new Color32(200, 170, 255, 100));
        p.DrawLine(36, 12, 36, 24, new Color32(200, 170, 255, 80), 1);
        p.DrawLine(30, 18, 42, 18, new Color32(200, 170, 255, 80), 1);
        // 지팡이 본체 (하단 좌방향)
        var staffCol  = Hex("8B6820");
        var staffHigh = Hex("C8A040");
        p.DrawLine(36, 28, 24, 58, staffCol,  6);
        p.DrawLine(35, 29, 23, 57, staffHigh, 2);
        // 구슬-지팡이 연결 장식
        p.DrawLine(30, 27, 36, 27, Gold, 2);
        p.DrawLine(42, 27, 36, 27, Gold, 2);
        // 지팡이 끝 장식
        p.FillCircle(23, 59, 4, Gold);
        p.FillCircle(23, 59, 2, Yellow);
        // 마법 파티클
        p.FillCircle(18, 28, 2, new Color32(170, 136, 255, 180));
        p.FillCircle(50, 32, 2, new Color32(136, 170, 255, 160));
        p.FillCircle(14, 40, 2, new Color32(204, 136, 255, 140));
        p.FillCircle(52, 14, 2, new Color32(255, 170, 255, 140));
    }

    static void DrawShieldBearer(P p)
    {
        int W = p.W, H = p.H, cx = W / 2;
        p.BgGradient(Shield_BgDark, Shield_BgMid);
        p.RoundedBorder(10, 2, Shield_Rim);

        var steelDk = Hex("334455");
        var steelMd = Hex("1A4A5A");
        var steelLt = Hex("2A6A7A");
        var rimCol  = Teal;

        // 방패 본체 (히터 방패) — 다각형 근사
        FillShieldShape(p, cx, 7, 52, 14, steelDk, steelMd);
        // 테두리
        DrawShieldOutline(p, cx, 7, 52, 14, rimCol, 2);
        // 내부 선 (장식)
        DrawShieldOutline(p, cx, 11, 47, 18, new Color32(68, 204, 221, 70), 1);
        // 철판 질감
        p.DrawLine(20, 16, 20, 52, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(cx, 10, cx, 56, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(44, 16, 44, 52, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(10, 24, 54, 24, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(10, 36, 54, 36, new Color32(153, 170, 204, 40), 1);
        // 리벳
        p.FillCircle(15, 16, 3, Teal); p.FillCircle(49, 16, 3, Teal);
        p.FillCircle(11, 30, 3, Teal); p.FillCircle(53, 30, 3, Teal);
        p.FillCircle(15, 44, 3, Teal); p.FillCircle(49, 44, 3, Teal);
        // 중앙 엠블럼
        p.FillCircle(cx, 33, 9, steelDk);
        p.DrawCircle(cx, 33, 9, 2, Teal);
        // 십자
        p.FillRRect(cx - 1, 25, 3, 16, 1, Teal);
        p.FillRRect(cx - 8, 32, 16, 3, 1, Teal);
        p.FillCircle(cx, 33, 3, Hex("AACCDD"));
        p.FillCircle(cx - 1, 32, 2, new Color32(255,255,255,70));
    }

    // 방패 채우기 (대략적인 히터 방패 모양)
    static void FillShieldShape(P p, int cx, int top, int bottom, int topY,
                                 Color32 dark, Color32 mid)
    {
        for (int y = topY; y <= bottom; y++)
        {
            float t  = (float)(y - topY) / (bottom - topY);
            float halfW;
            if (t < 0.6f)  halfW = Mathf.Lerp(24, 22, t / 0.6f);
            else           halfW = Mathf.Lerp(22, 0,  (t - 0.6f) / 0.4f);
            int x0 = cx - Mathf.RoundToInt(halfW);
            int x1 = cx + Mathf.RoundToInt(halfW);
            for (int x = x0; x <= x1; x++)
            {
                float blend = (float)(y - topY) / (bottom - topY);
                p.BlendPixel(x, y, Color32.Lerp(mid, dark, blend * blend));
            }
        }
    }

    static void DrawShieldOutline(P p, int cx, int top, int bottom, int topY, Color32 col, int w)
    {
        for (int y = topY; y <= bottom; y++)
        {
            float t = (float)(y - topY) / (bottom - topY);
            float halfW;
            if (t < 0.6f)  halfW = Mathf.Lerp(24, 22, t / 0.6f);
            else           halfW = Mathf.Lerp(22, 0,  (t - 0.6f) / 0.4f);
            int x0 = cx - Mathf.RoundToInt(halfW);
            int x1 = cx + Mathf.RoundToInt(halfW);
            for (int i = 0; i < w; i++)
            {
                p.BlendPixel(x0 + i, y, col);
                p.BlendPixel(x1 - i, y, col);
            }
        }
        // 상단 가로선
        for (int x = cx - 24; x <= cx + 24; x++)
            for (int i = 0; i < w; i++) p.BlendPixel(x, topY + i, col);
    }

    // ─────────────────────────────────────────────────────
    //  ■ 스킬 아이콘 (48×48)
    // ─────────────────────────────────────────────────────

    static void DrawHeavyStrike(P p)
    {
        int W = p.W, H = p.H, cx = W / 2;
        p.BgGradient(Hex("0E0604"), Hex("4A2010"));
        p.RoundedBorder(8, 1, Hex("CC5520"));
        // 해머 머리
        p.FillRRect(cx - 11, 8, 22, 14, 3, Hex("888888"));
        p.FillRect(cx - 11, 8, 22, 6, Hex("AAAAAA")); // 상단 하이라이트
        // 해머 손잡이
        p.FillRRect(cx - 3, 22, 6, 14, 2, Wood);
        p.DrawLine(cx - 3, 26, cx + 3, 26, Tint(Wood, Hex("000000"), 0.4f), 1);
        p.DrawLine(cx - 3, 30, cx + 3, 30, Tint(Wood, Hex("000000"), 0.4f), 1);
        // 충격 이펙트 라인 (아래)
        p.DrawLine(10, 42, 20, 34, Orange, 2);
        p.DrawLine(cx, 44, cx, 36, Orange, 2);
        p.DrawLine(38, 42, 28, 34, Orange, 2);
        // 충격 글로우
        p.FillCircleAlpha(cx, 36, 10, new Color32(255, 100, 50, 30));
    }

    static void DrawVolleyFire(P p)
    {
        int W = p.W;
        p.BgGradient(Hex("060E02"), Hex("1A3A0A"));
        p.RoundedBorder(8, 1, Hex("44AA22"));
        var arrowCol = Hex("C8A840");
        for (int i = 0; i < 3; i++)
        {
            int y = 14 + i * 10;
            p.DrawLine(8, y, 38, y, arrowCol, 3);        // 샤프트
            p.FillTri(38, y, 30, y - 5, 30, y + 5, Silver); // 화살촉
            p.FillTri(8, y, 16, y - 4, 14, y, Green);     // 깃 위
            p.FillTri(8, y, 16, y + 4, 14, y, Tint(Green, Hex("000000"), 0.25f)); // 깃 아래
        }
        // 속도선
        p.DrawLine(36, 10, 44, 10, Hex("88FF44"), 1);
        p.DrawLine(38, 24, 46, 24, Hex("88FF44"), 1);
        p.DrawLine(36, 38, 44, 38, Hex("88FF44"), 1);
    }

    static void DrawLeapStrike(P p)
    {
        p.BgGradient(Hex("0E0502"), Hex("3A1A04"));
        p.RoundedBorder(8, 1, Hex("CC7722"));
        // 착지 충격
        p.FillCircleAlpha(22, 40, 14, new Color32(255, 136, 50, 35));
        p.DrawCircle(22, 40, 8, 1, new Color32(255, 136, 50, 130));
        // 도약 궤적 (포물선 점선)
        DrawArc(p, 8, 38, 32, 12, Hex("FFAA44"), 1, true);
        // 인물 실루엣 — 점프 포즈
        p.FillCircle(34, 11, 4, Hex("DDDDDD")); // 머리
        p.DrawLine(34, 15, 34, 24, Hex("DDDDDD"), 3);  // 몸통
        p.DrawLine(34, 18, 40, 12, Hex("DDDDDD"), 2);  // 팔 (칼 올림)
        p.DrawLine(40, 12, 44,  8, Silver, 2);          // 검
        p.FillTri(44, 8, 41, 11, 43, 13, Silver);      // 검끝
        p.DrawLine(34, 24, 30, 32, Hex("DDDDDD"), 2);  // 다리1
        p.DrawLine(34, 24, 38, 30, Hex("DDDDDD"), 2);  // 다리2
    }

    static void DrawHealAura(P p)
    {
        int cx = p.W / 2, cy = p.H / 2;
        p.BgGradient(Hex("020A02"), Hex("0A2E0A"));
        p.RoundedBorder(8, 1, Hex("22AA44"));
        // 오라 원
        p.FillCircleAlpha(cx, cy, 22, new Color32(34, 255, 102, 25));
        p.DrawCircle(cx, cy, 18, 1, new Color32(34, 204, 85,  110));
        p.DrawCircle(cx, cy, 14, 1, new Color32(51, 221, 102, 90));
        // 십자 (치유)
        p.FillRRect(cx - 3, cy - 12, 6, 24, 3, Hex("44FF88"));
        p.FillRRect(cx - 12, cy - 3, 24, 6, 3, Hex("44FF88"));
        p.FillCircleAlpha(cx, cy, 6, new Color32(136, 255, 170, 180));
        p.FillCircle(cx, cy, 4, Hex("88FFAA"));
        p.FillCircleAlpha(cx - 2, cy - 2, 2, new Color32(255, 255, 255, 140));
        // 파티클
        p.FillCircle(10, 14, 2, new Color32(68, 255, 136, 180));
        p.FillCircle(38, 12, 2, new Color32(68, 255, 136, 150));
        p.FillCircle(8,  34, 2, new Color32(68, 255, 136, 130));
        p.FillCircle(40, 36, 2, new Color32(68, 255, 136, 150));
    }

    static void DrawTargetHeal(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("030803"), Hex("0E2A0E"));
        p.RoundedBorder(8, 1, Hex("33BB44"));
        // 하트
        FillHeart(p, cx, 22, 14, Red);
        // 하트 하이라이트
        p.FillCircleAlpha(cx - 5, 15, 4, new Color32(255, 255, 255, 60));
        // 십자 (치유)
        p.FillRRect(cx - 2, 16, 4, 12, 1, new Color32(255, 255, 255, 180));
        p.FillRRect(cx - 6, 20, 12, 4, 1, new Color32(255, 255, 255, 180));
        // 조준선
        p.DrawLine(cx, 8, cx, 13, Hex("33BB44"), 1);
        p.DrawLine(cx, 33, cx, 38, Hex("33BB44"), 1);
        p.DrawLine(10, 22, 15, 22, Hex("33BB44"), 1);
        p.DrawLine(33, 22, 38, 22, Hex("33BB44"), 1);
        p.DrawCircle(cx, 22, 14, 1, new Color32(51, 187, 68, 80));
        // 힐 화살표
        p.FillTri(10, 34, 7, 40, 13, 40, Hex("44FF88"));
        p.DrawLine(10, 40, 10, 46, Hex("44FF88"), 2);
        p.FillTri(38, 34, 35, 40, 41, 40, Hex("44FF88"));
        p.DrawLine(38, 40, 38, 46, Hex("44FF88"), 2);
    }

    static void FillHeart(P p, int cx, int cy, int r, Color32 c)
    {
        for (int y = cy - r; y <= cy + r * 2; y++)
        {
            for (int x = cx - r - 2; x <= cx + r + 2; x++)
            {
                float nx = (x - cx) / (float)r;
                float ny = (y - cy) / (float)r;
                // 하트 방정식 근사
                float dx1 = nx + 0.5f, dy1 = ny + 0.6f;
                float dx2 = nx - 0.5f, dy2 = ny + 0.6f;
                bool in1 = dx1 * dx1 + dy1 * dy1 <= 0.9f;
                bool in2 = dx2 * dx2 + dy2 * dy2 <= 0.9f;
                bool inLow = (nx * nx * 0.6f + (ny - 0.4f) * (ny - 0.4f)) <= 1.0f && ny > -0.3f;
                if ((in1 || in2) && y <= cy + r) p.BlendPixel(x, y, c);
                else if (inLow) p.BlendPixel(x, y, c);
            }
        }
    }

    static void DrawChargeSoldier(P p)
    {
        p.BgGradient(Hex("03090E"), Hex("10303A"));
        p.RoundedBorder(8, 1, Hex("22AACC"));
        // 속도선
        p.DrawLine(4, 20, 18, 20, new Color32(34, 170, 204, 100), 2);
        p.DrawLine(4, 24, 14, 24, new Color32(34, 170, 204, 80),  2);
        p.DrawLine(4, 28, 18, 28, new Color32(34, 170, 204, 100), 2);
        // 방패
        FillShieldShape(p, 28, 18, 44, 14, Hex("1A7A9A"), Hex("1A4A5A"));
        DrawShieldOutline(p, 28, 18, 44, 14, Teal, 2);
        // 방패 엠블럼
        p.FillCircle(28, 31, 5, Hex("0A4A5A"));
        p.FillRRect(27, 26, 3, 10, 1, Teal);
        p.FillRRect(22, 30, 12, 3, 1, Teal);
        // 돌격 화살표
        p.FillTri(44, 24, 36, 19, 36, 29, Teal);
        p.FillRect(28, 22, 8, 4, Teal);
        // 글로우
        p.FillCircleAlpha(44, 24, 8, new Color32(34, 204, 255, 40));
    }

    static void DrawSummonSkeleton(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("060310"), Hex("1A1030"));
        p.RoundedBorder(8, 1, Hex("8855CC"));
        // 소환진
        p.DrawCircle(cx, 40, 10, 1, new Color32(136, 85, 204, 80));
        p.FillCircleAlpha(cx, 24, 18, new Color32(136, 85, 204, 20));
        // 해골 두개골
        p.FillEllipse(cx, 16, 11, 9, Hex("CCCCAA"));
        p.FillCircleAlpha(cx - 3, 13, 4, new Color32(255, 255, 255, 50));
        // 눈 소켓
        p.FillEllipse(cx - 4, 15, 3, 4, Hex("220033"));
        p.FillEllipse(cx + 4, 15, 3, 4, Hex("220033"));
        p.FillCircleAlpha(cx - 4, 15, 2, new Color32(153, 51, 255, 200));
        p.FillCircleAlpha(cx + 4, 15, 2, new Color32(153, 51, 255, 200));
        // 코
        p.DrawLine(cx, 19, cx - 1, 21, Hex("888866"), 1);
        p.DrawLine(cx, 19, cx + 1, 21, Hex("888866"), 1);
        // 이빨
        p.FillRRect(cx - 7, 23, 14, 3, 1, Hex("888866"));
        for (int i = 0; i < 4; i++) p.DrawLine(cx - 6 + i*4, 23, cx - 6 + i*4, 26, Hex("CCCCAA"), 1);
        // 상승선
        p.DrawLine(cx, 44, cx, 28, new Color32(136, 85, 204, 100), 1);
        // 파티클
        p.FillCircle(10, 32, 2, new Color32(153, 51, 255, 180));
        p.FillCircle(38, 30, 2, new Color32(153, 51, 255, 160));
        p.FillCircle(14, 42, 2, new Color32(153, 51, 255, 120));
        p.FillCircle(36, 44, 2, new Color32(153, 51, 255, 120));
    }

    static void DrawPoisonZone(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("030802"), Hex("0E2A06"));
        p.RoundedBorder(8, 1, Hex("66AA11"));
        // 바닥 독 지대
        p.FillEllipse(cx, 40, 18, 5, new Color32(34, 85, 0, 150));
        p.FillCircleAlpha(cx, 40, 14, new Color32(136, 238, 34, 20));
        // 독 구름 (여러 원)
        p.FillCircleAlpha(18, 28, 9,  new Color32(136, 238, 34, 80));
        p.FillCircleAlpha(28, 26, 10, new Color32(100, 200, 20, 90));
        p.FillCircleAlpha(22, 24, 8,  new Color32(120, 220, 25, 70));
        // 해골 심볼
        p.FillCircle(cx, 24, 7, Hex("1A3A00"));
        p.DrawCircle(cx, 24, 7, 1, Hex("88EE22"));
        p.FillCircle(cx - 3, 22, 2, Hex("88EE22"));
        p.FillCircle(cx + 3, 22, 2, Hex("88EE22"));
        p.DrawLine(cx - 3, 27, cx + 3, 27, Hex("88EE22"), 2);
        p.DrawLine(cx - 2, 26, cx - 2, 29, Hex("88EE22"), 1);
        p.DrawLine(cx + 2, 26, cx + 2, 29, Hex("88EE22"), 1);
        // 독 방울
        p.FillEllipse(12, 17, 3, 4, Hex("88EE22"));
        p.FillCircle(12, 14, 2, new Color32(170, 255, 170, 150));
        p.FillEllipse(36, 20, 2, 3, Hex("88EE22"));
    }

    static void DrawMeteor(P p)
    {
        p.BgGradient(Hex("0A0302"), Hex("2A0E04"));
        p.RoundedBorder(8, 1, Hex("CC4400"));
        // 화염 꼬리
        p.DrawLine(40, 6, 22, 36, new Color32(255, 102, 0, 50),  10);
        p.DrawLine(40, 6, 22, 36, new Color32(255, 170, 68, 120), 5);
        p.DrawLine(40, 6, 22, 36, new Color32(255, 221, 136, 200),2);
        // 착지 폭발
        p.FillCircleAlpha(22, 40, 10, new Color32(255, 102, 0, 50));
        p.DrawLine(10, 46, 14, 38, Orange, 2);
        p.DrawLine(22, 47, 22, 40, Orange, 2);
        p.DrawLine(34, 46, 30, 38, Orange, 2);
        p.DrawLine(8,  38, 14, 36, Orange, 2);
        p.DrawLine(36, 38, 30, 36, Orange, 2);
        // 운석 본체
        p.FillCircleGrad(22, 34, 9, Hex("FFCC44"), Hex("FF6600"), Hex("882200"));
        // 운석 크레이터
        p.FillCircleAlpha(18, 31, 3, new Color32(0, 0, 0, 80));
        p.FillCircleAlpha(25, 36, 2, new Color32(0, 0, 0, 70));
        // 파편
        p.FillCircle(38, 20, 2, Orange);
        p.FillCircle(42, 28, 2, Yellow);
    }

    static void DrawBlizzard(P p)
    {
        int cx = p.W / 2, cy = p.H / 2;
        p.BgGradient(Hex("020509"), Hex("081830"));
        p.RoundedBorder(8, 1, Hex("4488CC"));
        var iceCol = Hex("AADDFF");
        // 글로우
        p.FillCircleAlpha(cx, cy, 20, new Color32(136, 204, 255, 20));
        // 6축 눈결정
        p.DrawLine(cx, 4,  cx, 44, iceCol, 2);
        p.DrawLine(7,  13, 41, 35, iceCol, 2);
        p.DrawLine(7,  35, 41, 13, iceCol, 2);
        // 각 가지 측면 돌기
        p.DrawLine(cx, 14, cx - 4, 18, iceCol, 1); p.DrawLine(cx, 14, cx + 4, 18, iceCol, 1);
        p.DrawLine(cx, 34, cx - 4, 30, iceCol, 1); p.DrawLine(cx, 34, cx + 4, 30, iceCol, 1);
        p.DrawLine(31, 11, 29, 16, iceCol, 1);     p.DrawLine(31, 11, 33, 15, iceCol, 1);
        p.DrawLine(17, 37, 19, 32, iceCol, 1);     p.DrawLine(17, 37, 15, 33, iceCol, 1);
        p.DrawLine(37, 31, 32, 30, iceCol, 1);     p.DrawLine(37, 31, 35, 26, iceCol, 1);
        p.DrawLine(11, 17, 16, 18, iceCol, 1);     p.DrawLine(11, 17, 13, 22, iceCol, 1);
        // 중심 원
        p.FillCircle(cx, cy, 4, iceCol);
        p.FillCircleAlpha(cx, cy, 6, new Color32(170, 221, 255, 100));
        // 파티클
        p.FillCircle(8,  10, 2, new Color32(170, 221, 255, 150));
        p.FillCircle(40,  8, 2, new Color32(170, 221, 255, 130));
        p.FillCircle(6,  38, 2, new Color32(170, 221, 255, 130));
        p.FillCircle(42, 40, 2, new Color32(170, 221, 255, 150));
    }

    static void DrawSacrificeSoldier(P p)
    {
        p.BgGradient(Hex("080502"), Hex("2A1A04"));
        p.RoundedBorder(8, 1, Hex("CC8822"));
        // 병사 실루엣 (희미하게)
        p.FillCircleAlpha(16, 28, 4, new Color32(139, 96, 32, 130));
        p.DrawLine(16, 32, 16, 42, new Color32(139, 96, 32, 100), 3);
        // 에너지 흡수 선
        p.DrawLine(16, 27, 32, 10, new Color32(255, 170, 34, 180), 2);
        // 에너지 구슬
        p.FillCircleAlpha(32, 10, 6, new Color32(255, 204, 68, 80));
        p.FillCircle(32, 10, 4, Hex("FFAA22"));
        p.FillCircle(30, 8,  2, new Color32(255, 255, 255, 150));
        // 장군 강화 이펙트
        p.FillCircleAlpha(36, 30, 7, new Color32(255, 170, 34, 50));
        p.FillCircle(36, 24, 4, Hex("DDDDDD"));
        p.DrawLine(36, 28, 36, 36, Hex("DDDDDD"), 3);
        // 강화 화살표
        p.FillTri(36, 14, 33, 20, 39, 20, Yellow);
        p.FillTri(36, 10, 33, 16, 39, 16, new Color32(255, 170, 34, 180));
        // 파티클
        p.FillCircle(22, 20, 2, new Color32(255, 204, 68, 150));
        p.FillCircle(26, 16, 2, new Color32(255, 204, 68, 130));
    }

    static void DrawBind(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("060402"), Hex("1E100A"));
        p.RoundedBorder(8, 1, Hex("886633"));
        // 마법 봉인 원
        p.DrawCircle(cx, cx, 14, 1, new Color32(204, 136, 51, 100));
        p.FillCircleAlpha(cx, cx, 14, new Color32(204, 136, 51, 20));
        // 쇠사슬 (타원 링크들)
        DrawChainLink(p, 14, 11, -30, Hex("AAAAAA"), Hex("CCCCCC"));
        DrawChainLink(p, 22,  9,  30, Hex("BBBBBB"), Hex("DDDDDD"));
        DrawChainLink(p, 30, 12, -30, Hex("AAAAAA"), Hex("CCCCCC"));
        DrawChainLink(p, 10, 23,  60, Hex("AAAAAA"), Hex("CCCCCC"));
        DrawChainLink(p, 38, 23, -60, Hex("AAAAAA"), Hex("CCCCCC"));
        // 중앙 타겟 (속박된 형태)
        p.FillCircle(cx, 24, 6, Hex("331A0A"));
        p.DrawLine(cx - 4, 21, cx + 4, 21, Hex("777777"), 2);
        p.DrawLine(cx - 4, 24, cx + 4, 24, Hex("777777"), 2);
        p.DrawLine(cx - 4, 27, cx + 4, 27, Hex("777777"), 2);
        // 자물쇠
        p.FillRRect(cx - 4, 34, 8, 7, 2, Hex("8B7040"));
        DrawArcPath(p, cx, 34, 4, 180, 360, Hex("8B7040"), 3);
        p.FillCircle(cx, 37, 2, Hex("5A3A10"));
    }

    static void DrawChainLink(P p, int cx, int cy, int rotDeg, Color32 outer, Color32 inner)
    {
        // 타원 링크 근사
        float rad = rotDeg * Mathf.Deg2Rad;
        for (int a = 0; a < 360; a += 4)
        {
            float ar = a * Mathf.Deg2Rad;
            float lx = Mathf.Cos(ar) * 5, ly = Mathf.Sin(ar) * 3.5f;
            float rx = lx * Mathf.Cos(rad) - ly * Mathf.Sin(rad);
            float ry = lx * Mathf.Sin(rad) + ly * Mathf.Cos(rad);
            int px = cx + Mathf.RoundToInt(rx);
            int py = cy + Mathf.RoundToInt(ry);
            p.BlendPixel(px, py, outer);
            p.BlendPixel(px + 1, py, outer);
        }
    }

    static void DrawSuicideSoldier(P p)
    {
        p.BgGradient(Hex("0A0302"), Hex("2A1004"));
        p.RoundedBorder(8, 1, Hex("CC4400"));
        // 폭발 글로우
        p.FillCircleAlpha(28, 26, 16, new Color32(255, 102, 0, 40));
        // 폭발 방사선
        foreach (var pair in new[]{(28,8),(40,12),(46,26),(40,40),(28,46),(16,40),(10,26),(16,12)})
            p.DrawLine(28, 26, pair.Item1, pair.Item2, new Color32(255, 170, 68, 180), 2);
        // 폭발 중심
        p.FillCircleGrad(28, 26, 10, White, Hex("FFEE44"), Hex("FF6600"));
        p.FillCircleAlpha(28, 26, 6, new Color32(255, 255, 255, 120));
        // 병사 실루엣 (달려가는)
        p.FillCircleAlpha(13, 22, 4, new Color32(136, 136, 136, 180));
        p.DrawLine(13, 26, 13, 33, new Color32(136, 136, 136, 160), 3);
        p.DrawLine(13, 28, 9,  25, new Color32(136, 136, 136, 140), 2);
        p.DrawLine(13, 28, 17, 26, new Color32(136, 136, 136, 140), 2);
        p.DrawLine(13, 33, 10, 39, new Color32(136, 136, 136, 140), 2);
        p.DrawLine(13, 33, 16, 38, new Color32(136, 136, 136, 140), 2);
        // 방향 화살표
        p.DrawLine(18, 26, 22, 26, new Color32(255, 136, 51, 150), 1);
    }

    static void DrawBerserker(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("0C0202"), Hex("3A0808"));
        p.RoundedBorder(8, 1, Hex("CC2222"));
        // 화염 배경
        p.FillCircleAlpha(cx, 32, 16, new Color32(255, 68, 0, 30));
        // 불꽃 (좌)
        for (int y = 44; y > 14; y -= 4)
        {
            float t = (44 - y) / 30f;
            int x = (int)(cx - 10 + Mathf.Sin(t * 8) * 3);
            p.FillCircleAlpha(x, y, 4 + (int)(t * 3), new Color32(255, 100, 0, (byte)(80 * (1 - t))));
        }
        // 불꽃 (우)
        for (int y = 44; y > 14; y -= 4)
        {
            float t = (44 - y) / 30f;
            int x = (int)(cx + 10 - Mathf.Sin(t * 8) * 3);
            p.FillCircleAlpha(x, y, 4 + (int)(t * 3), new Color32(255, 68, 0, (byte)(80 * (1 - t))));
        }
        // 교차 쌍검
        FillRRectRotated(p, cx, cx, 32, 5, -35, Hex("D0D0D0"), Hex("888888")); // 검1
        FillRRectRotated(p, cx, cx, 32, 5,  35, Hex("D0D0D0"), Hex("888888")); // 검2
        // 교차점 분노 글로우
        p.FillCircleAlpha(cx, cx, 5, new Color32(255, 34, 0, 180));
        p.FillCircle(cx, cx, 3, Hex("FFAA44"));
    }

    static void DrawIronShield(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("03080E"), Hex("0E2030"));
        p.RoundedBorder(8, 1, Hex("4488BB"));
        // 방어 글로우
        FillShieldShape(p, cx, 5, 46, 10, Hex("334455"), Hex("1A4A5A"));
        DrawShieldOutline(p, cx, 5, 46, 10, Teal, 2);
        // 철판 질감
        p.DrawLine(17, 12, 17, 42, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(cx, 8,  cx, 44, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(31, 12, 31, 42, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(9,  20, 39, 20, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(9,  30, 39, 30, new Color32(153, 170, 204, 50), 1);
        // 리벳
        p.FillCircle(11, 13, 2, Teal); p.FillCircle(37, 13, 2, Teal);
        p.FillCircle(8,  25, 2, Teal); p.FillCircle(40, 25, 2, Teal);
        p.FillCircle(11, 38, 2, Teal); p.FillCircle(37, 38, 2, Teal);
        // 중앙 엠블럼
        p.FillCircle(cx, 28, 8, Hex("334455"));
        p.DrawCircle(cx, 28, 8, 2, Teal);
        p.FillRRect(cx - 1, 21, 3, 14, 1, Teal);
        p.FillRRect(cx - 7, 27, 14, 3, 1, Teal);
        p.FillCircle(cx, 28, 3, Hex("AACCDD"));
        p.FillCircleAlpha(cx - 1, 27, 2, new Color32(255,255,255,70));
    }

    static void DrawArrowRain(P p)
    {
        p.BgGradient(Hex("020602"), Hex("0A1E0A"));
        p.RoundedBorder(8, 1, Hex("228833"));
        // 구름
        p.FillEllipse(12, 10, 7, 5, Hex("334433"));
        p.FillEllipse(22,  8, 9, 6, Hex("334433"));
        p.FillEllipse(34, 10, 8, 5, Hex("334433"));
        p.FillEllipse(24, 12, 18, 6, Hex("2A3A2A"));
        // 화살 비 (5개)
        int[] xs = { 11, 18, 24, 31, 38 };
        int[] ys = { 16, 14, 14, 16, 18 };
        int[] ye = { 38, 42, 44, 42, 38 };
        for (int i = 0; i < 5; i++)
        {
            p.DrawLine(xs[i], ys[i], xs[i], ye[i], Hex("C8A840"), 2);  // 샤프트
            p.FillTri(xs[i], ye[i], xs[i] - 3, ye[i] - 6, xs[i] + 3, ye[i] - 6, Silver); // 촉
            p.FillTri(xs[i], ys[i], xs[i] - 3, ys[i] + 5, xs[i], ys[i] + 4, Green);      // 깃 왼
            p.FillTri(xs[i], ys[i], xs[i] + 3, ys[i] + 5, xs[i], ys[i] + 4, Tint(Green, Hex("000000"), 0.25f)); // 깃 오른
        }
        // 착지 글로우
        p.FillEllipse(24, 46, 16, 2, new Color32(34, 170, 51, 60));
    }

    static void DrawBattleCry(P p)
    {
        p.BgGradient(Hex("080602"), Hex("2A1E04"));
        p.RoundedBorder(8, 1, Hex("CC9922"));
        // 함성 음파 (동심 호)
        DrawSoundWave(p, 28, 24, 12, Hex("FFCC44"), 2, 0.8f);
        DrawSoundWave(p, 28, 24, 18, Hex("FFAA22"), 2, 0.55f);
        DrawSoundWave(p, 28, 24, 24, Hex("FF8800"), 1, 0.35f);
        // 인물
        p.FillCircle(16, 16, 5, Hex("DDDDDD"));
        p.FillCircleAlpha(14, 14, 2, new Color32(255, 255, 255, 60));
        // 입 (함성)
        p.FillEllipse(16, 19, 3, 2, Hex("CC3333"));
        // 몸통
        p.DrawLine(16, 21, 16, 32, Hex("DDDDDD"), 4);
        // 팔 (올린 포즈)
        p.DrawLine(16, 24, 8, 18, Hex("DDDDDD"), 3);
        p.DrawLine(16, 24, 24, 21, Hex("DDDDDD"), 3);
        // 다리
        p.DrawLine(16, 32, 11, 42, Hex("DDDDDD"), 3);
        p.DrawLine(16, 32, 21, 42, Hex("DDDDDD"), 3);
        // 강화 화살표
        p.FillTri(8, 42, 5, 36, 11, 36, Yellow);
        p.DrawLine(8, 36, 8, 28, Yellow, 2);
    }

    static void DrawSoundWave(P p, int cx, int cy, int r, Color32 col, int thick, float alpha)
    {
        var c = new Color32(col.r, col.g, col.b, (byte)(col.a * alpha));
        for (int a = -70; a <= 70; a++)
        {
            float rad = a * Mathf.Deg2Rad;
            int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * r);
            int y = cy - Mathf.RoundToInt(Mathf.Sin(rad) * r);
            for (int t = 0; t < thick; t++)
            {
                float nr = (r + t) / (float)r;
                int nx = cx + Mathf.RoundToInt(Mathf.Cos(rad) * (r + t));
                int ny = cy - Mathf.RoundToInt(Mathf.Sin(rad) * (r + t));
                p.BlendPixel(nx, ny, c);
            }
        }
    }

    static void DrawShockwave(P p)
    {
        p.BgGradient(Hex("060402"), Hex("1A100A"));
        p.RoundedBorder(8, 1, Hex("CC6622"));
        // 충격파 호
        DrawShockArc(p, 8, 34, Hex("FF8833"), 4, 0.7f);
        DrawShockArc(p, 6, 40, Hex("FFAA55"), 3, 0.5f);
        DrawShockArc(p, 4, 46, Hex("FFCC77"), 2, 0.35f);
        // 발원 주먹
        p.FillCircleAlpha(8, 36, 7, new Color32(255, 102, 34, 50));
        p.FillCircle(8, 36, 5, Hex("3A1400"));
        p.DrawCircle(8, 36, 5, 1, new Color32(255, 136, 51, 180));
        p.FillRRect(5, 33, 7, 5, 2, Hex("DDDDCC"));
        p.FillRRect(4, 37, 9, 3, 1, Hex("CCCCBB"));
        // 파편
        p.FillCircle(36, 12, 2, Orange);
        p.FillCircle(42, 20, 2, Yellow);
        p.FillCircle(38,  8, 2, Yellow);
    }

    static void DrawShockArc(P p, int ofsX, int ofsY, Color32 col, int thick, float alpha)
    {
        var c = new Color32(col.r, col.g, col.b, (byte)(col.a * alpha));
        // 부채꼴 호 (오른쪽 상단 방향 ~ Q16,8 40,24 형태)
        int steps = 40;
        float x0 = ofsX, y0 = ofsY;
        float cx2 = 16, cy2 = 8;
        float x2 = 44, y2 = 26;
        int ppx = (int)x0, ppy = (int)y0;
        for (int i = 1; i <= steps; i++)
        {
            float t  = i / (float)steps;
            float mt = 1 - t;
            int nx = Mathf.RoundToInt(mt * mt * x0 + 2 * mt * t * cx2 + t * t * x2);
            int ny = Mathf.RoundToInt(mt * mt * y0 + 2 * mt * t * cy2 + t * t * y2);
            for (int w = 0; w < thick; w++) p.DrawLine(ppx, ppy + w, nx, ny + w, c, 1);
            ppx = nx; ppy = ny;
        }
    }

    static void DrawSwiftStrike(P p)
    {
        p.BgGradient(Hex("060502"), Hex("1E1A04"));
        p.RoundedBorder(8, 1, Hex("CCBB11"));
        // 속도 잔상
        p.DrawLine(4, 20, 22, 20, new Color32(255, 238, 68, 40), 5);
        p.DrawLine(4, 24, 18, 24, new Color32(255, 238, 68, 30), 4);
        p.DrawLine(4, 28, 22, 28, new Color32(255, 238, 68, 25), 3);
        p.DrawLine(6, 18, 24, 18, new Color32(255, 238, 68, 100), 1);
        p.DrawLine(4, 22, 22, 22, new Color32(255, 238, 68, 100), 1);
        p.DrawLine(6, 26, 20, 26, new Color32(255, 238, 68, 90),  1);
        // 검 (25도 기울기)
        FillRotatedRect(p, 20, 20, 30, 6, 25, Hex("D8D8D8"), Hex("888888"));
        // 검 가드
        FillRotatedRect(p, 20, 24, 18, 4, 25, Gold, DkGold);
        // 베기 이펙트
        p.FillCircleAlpha(40, 16, 10, new Color32(255, 238, 68, 50));
        p.DrawLine(30, 10, 46, 30, Hex("FFEE44"), 3);
        p.DrawLine(34,  8, 48, 24, new Color32(255, 204, 34, 150), 2);
        p.DrawLine(28, 12, 44, 32, new Color32(255, 204, 34, 80),  1);
    }

    static void DrawSummonElite(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("060402"), Hex("1E140A"));
        p.RoundedBorder(8, 1, Hex("CCAA22"));
        // 소환진
        p.DrawCircle(cx, 38, 10, 1, new Color32(204, 153, 34, 100));
        p.FillCircleAlpha(cx, 38, 8, new Color32(204, 153, 34, 25));
        // 왕관 베이스
        p.FillRRect(10, 22, 28, 10, 1, Hex("C8922A"));
        // 왕관 포인트 3개
        p.FillTri(10, 22, 14, 10, 18, 22, Hex("D4A030"));
        p.FillTri(20, 22, 24,  8, 28, 22, Hex("D4A030"));
        p.FillTri(30, 22, 34, 10, 38, 22, Hex("D4A030"));
        p.FillRect(10, 22, 28, 3, new Color32(255, 255, 255, 30)); // 하이라이트
        // 왕관 보석
        p.FillCircle(14, 10, 3, Red);
        p.FillCircle(24,  8, 3, Hex("4488FF"));
        p.FillCircle(34, 10, 3, Red);
        // 측면 보석
        p.FillCircle(10, 27, 2, Hex("22CCAA"));
        p.FillCircle(38, 27, 2, Hex("22CCAA"));
        // 중앙 보석
        p.FillCircle(cx, 27, 4, Hex("4488FF"));
        p.FillCircleAlpha(cx - 2, 25, 2, new Color32(255, 255, 255, 140));
        // 소환 파티클
        p.FillCircle(12, 40, 2, new Color32(255, 204, 68, 150));
        p.FillCircle(36, 42, 2, new Color32(255, 204, 68, 130));
        p.FillCircle(20, 44, 2, new Color32(255, 170, 34, 120));
        p.FillCircle(30, 44, 2, new Color32(255, 170, 34, 110));
    }

    // ─────────────────────────────────────────────────────
    //  ■ 도우미 — 회전 사각형 그리기
    // ─────────────────────────────────────────────────────
    static void FillRotatedRect(P p, int cx, int cy, int length, int height,
                                 float angleDeg, Color32 mainCol, Color32 edgeCol)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        float hL = length / 2f, hH = height / 2f;
        for (int y = cy - length; y <= cy + length; y++)
        for (int x = cx - length; x <= cx + length; x++)
        {
            float lx =  (x - cx) * cos + (y - cy) * sin;
            float ly = -(x - cx) * sin + (y - cy) * cos;
            if (Mathf.Abs(lx) <= hL && Mathf.Abs(ly) <= hH)
            {
                bool edge = Mathf.Abs(ly) > hH - 1.5f;
                p.BlendPixel(x, y, edge ? edgeCol : mainCol);
            }
        }
    }

    static void FillRRectRotated(P p, int cx, int cy, int length, int height,
                                   float angleDeg, Color32 mainCol, Color32 edgeCol)
        => FillRotatedRect(p, cx, cy, length, height, angleDeg, mainCol, edgeCol);

    static void DrawArc(P p, int x0, int y0, int x2, int y2, Color32 col, int thick, bool dashed)
    {
        // 2차 베지어 호 (포물선)
        int cx2 = (x0 + x2) / 2, cy2 = Mathf.Min(y0, y2) - 20;
        int steps = 30, ppx = x0, ppy = y0;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps, mt = 1 - t;
            int nx = Mathf.RoundToInt(mt*mt*x0 + 2*mt*t*cx2 + t*t*x2);
            int ny = Mathf.RoundToInt(mt*mt*y0 + 2*mt*t*cy2 + t*t*y2);
            if (!dashed || (i % 4 < 2)) p.DrawLine(ppx, ppy, nx, ny, col, thick);
            ppx = nx; ppy = ny;
        }
    }

    static void DrawArcPath(P p, int cx, int cy, int r, int startDeg, int endDeg, Color32 col, int thick)
    {
        int ppx = cx + Mathf.RoundToInt(Mathf.Cos(startDeg * Mathf.Deg2Rad) * r);
        int ppy = cy - Mathf.RoundToInt(Mathf.Sin(startDeg * Mathf.Deg2Rad) * r);
        for (int a = startDeg + 5; a <= endDeg; a += 5)
        {
            int nx = cx + Mathf.RoundToInt(Mathf.Cos(a * Mathf.Deg2Rad) * r);
            int ny = cy - Mathf.RoundToInt(Mathf.Sin(a * Mathf.Deg2Rad) * r);
            p.DrawLine(ppx, ppy, nx, ny, col, thick);
            ppx = nx; ppy = ny;
        }
    }

    // ─────────────────────────────────────────────────────
    //  ■ 유틸
    // ─────────────────────────────────────────────────────
    static Color32 Tint(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);

    static Color32 Hex(string h)
    {
        h = h.TrimStart('#');
        byte r = Convert.ToByte(h.Substring(0, 2), 16);
        byte g = Convert.ToByte(h.Substring(2, 2), 16);
        byte b = Convert.ToByte(h.Substring(4, 2), 16);
        return new Color32(r, g, b, 255);
    }

    static void Save(int w, int h, string assetPath, Action<P> draw)
    {
        var painter = new P(w, h);
        draw(painter);
        painter.Save(assetPath);
    }

    static void EnsureDir(string assetPath)
    {
        string full = Path.Combine(Application.dataPath, "..", assetPath);
        Directory.CreateDirectory(full);
    }

    static void ApplySpriteImportSettings(string folder, int size)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType          = TextureImporterType.Sprite;
            importer.spriteImportMode     = SpriteImportMode.Single;
            importer.spritePivot          = new Vector2(0.5f, 0.5f);
            importer.filterMode           = FilterMode.Bilinear;
            importer.textureCompression   = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize       = 128;
            importer.alphaIsTransparency  = true;
            importer.SaveAndReimport();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ■ Painter — 픽셀 그리기 헬퍼
    // ═══════════════════════════════════════════════════════
    class P
    {
        public int W, H;
        readonly Color32[] px;

        public P(int w, int h)
        {
            W = w; H = h;
            px = new Color32[w * h];
        }

        int Idx(int x, int y) => (H - 1 - y) * W + x;

        public void BlendPixel(int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            int i = Idx(x, y);
            if (c.a == 255) { px[i] = c; return; }
            float a = c.a / 255f, ea = px[i].a / 255f;
            float oa = a + ea * (1 - a);
            if (oa < 0.001f) { px[i] = default; return; }
            px[i] = new Color32(
                (byte)Mathf.RoundToInt((c.r * a + px[i].r * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt((c.g * a + px[i].g * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt((c.b * a + px[i].b * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt(oa * 255));
        }

        // ── 배경 ──────────────────────────────────────────
        public void BgGradient(Color32 dark, Color32 mid)
        {
            int cx = W / 2, cy = H / 2;
            float maxD = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                // 둥근 모서리 마스크 (r=10)
                const int R = 10;
                int dx = 0, dy = 0;
                if (x < R && y < R)       { dx = R-x; dy = R-y; }
                else if (x>=W-R && y<R)   { dx = x-(W-R-1); dy = R-y; }
                else if (x<R && y>=H-R)   { dx = R-x; dy = y-(H-R-1); }
                else if (x>=W-R && y>=H-R){ dx = x-(W-R-1); dy = y-(H-R-1); }
                if (dx*dx + dy*dy > R*R && (dx>0||dy>0)) continue;

                float t = Mathf.Clamp01(Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy)) / maxD);
                BlendPixel(x, y, Color32.Lerp(mid, dark, t * t));
            }
        }

        // ── 테두리 ────────────────────────────────────────
        public void RoundedBorder(int r, int thick, Color32 col)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int dx = 0, dy = 0;
                bool corner = false;
                if (x < r && y < r)         { dx=r-x;   dy=r-y;   corner=true; }
                else if(x>=W-r && y<r)      { dx=x-(W-r-1); dy=r-y; corner=true; }
                else if(x<r && y>=H-r)      { dx=r-x;   dy=y-(H-r-1); corner=true; }
                else if(x>=W-r && y>=H-r)   { dx=x-(W-r-1); dy=y-(H-r-1); corner=true; }

                bool onEdge;
                if (corner)
                {
                    float d = Mathf.Sqrt(dx*dx + dy*dy);
                    onEdge = d >= r - thick && d <= r;
                }
                else
                {
                    onEdge = x < thick || x >= W - thick || y < thick || y >= H - thick;
                }
                if (onEdge) BlendPixel(x, y, col);
            }
        }

        // ── 도형 ──────────────────────────────────────────
        public void FillRect(int x, int y, int w, int h, Color32 c)
        {
            for (int py=y; py<y+h; py++) for (int px2=x; px2<x+w; px2++) BlendPixel(px2,py,c);
        }

        public void FillRRect(int x, int y, int w, int h, int rad, Color32 c)
        {
            for (int py=y; py<y+h; py++)
            for (int px2=x; px2<x+w; px2++)
            {
                int dx=0, dy=0;
                if (px2<x+rad && py<y+rad)     { dx=x+rad-px2; dy=y+rad-py; }
                else if(px2>=x+w-rad && py<y+rad){ dx=px2-(x+w-rad-1); dy=y+rad-py; }
                else if(px2<x+rad && py>=y+h-rad){ dx=x+rad-px2; dy=py-(y+h-rad-1); }
                else if(px2>=x+w-rad && py>=y+h-rad){ dx=px2-(x+w-rad-1); dy=py-(y+h-rad-1); }
                if (dx*dx+dy*dy <= rad*rad || (dx==0&&dy==0)) BlendPixel(px2,py,c);
            }
        }

        public void FillCircle(int cx, int cy, int r, Color32 c)
        {
            for (int y=cy-r; y<=cy+r; y++) for (int x=cx-r; x<=cx+r; x++)
                if ((x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r) BlendPixel(x,y,c);
        }

        public void FillCircleAlpha(int cx, int cy, int r, Color32 c)
            => FillCircle(cx, cy, r, c);

        public void DrawCircle(int cx, int cy, int r, int thick, Color32 c)
        {
            for (int y=cy-r-thick; y<=cy+r+thick; y++) for (int x=cx-r-thick; x<=cx+r+thick; x++)
            {
                int d2 = (x-cx)*(x-cx)+(y-cy)*(y-cy);
                if (d2>=(r-thick)*(r-thick) && d2<=(r+thick)*(r+thick)) BlendPixel(x,y,c);
            }
        }

        public void FillCircleGrad(int cx, int cy, int r, Color32 inner, Color32 mid, Color32 outer)
        {
            for (int y=cy-r; y<=cy+r; y++) for (int x=cx-r; x<=cx+r; x++)
            {
                int d2 = (x-cx)*(x-cx)+(y-cy)*(y-cy);
                if (d2 > r*r) continue;
                float t = Mathf.Sqrt(d2) / r;
                Color32 c = t < 0.5f ? Color32.Lerp(inner, mid, t*2) : Color32.Lerp(mid, outer, (t-0.5f)*2);
                BlendPixel(x, y, c);
            }
        }

        public void FillEllipse(int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y=cy-ry; y<=cy+ry; y++) for (int x=cx-rx; x<=cx+rx; x++)
            {
                float dx = (float)(x-cx)/rx, dy2 = (float)(y-cy)/ry;
                if (dx*dx+dy2*dy2 <= 1f) BlendPixel(x,y,c);
            }
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color32 c, int thick=1)
        {
            int dx = Mathf.Abs(x2-x1), dy = Mathf.Abs(y2-y1);
            int sx = x1<x2?1:-1, sy = y1<y2?1:-1;
            int err = dx-dy, x=x1, y=y1;
            int h2 = thick/2;
            while (true)
            {
                for (int py=y-h2; py<=y+h2; py++) for (int px2=x-h2; px2<=x+h2; px2++) BlendPixel(px2,py,c);
                if (x==x2&&y==y2) break;
                int e2=2*err;
                if (e2>-dy){err-=dy;x+=sx;}
                if (e2< dx){err+=dx;y+=sy;}
            }
        }

        public void FillTri(int x1,int y1, int x2,int y2, int x3,int y3, Color32 c)
        {
            int minX=Mathf.Min(x1,Mathf.Min(x2,x3)), maxX=Mathf.Max(x1,Mathf.Max(x2,x3));
            int minY=Mathf.Min(y1,Mathf.Min(y2,y3)), maxY=Mathf.Max(y1,Mathf.Max(y2,y3));
            for (int py=minY; py<=maxY; py++) for (int px2=minX; px2<=maxX; px2++)
            {
                float d1=Sign(px2,py,x1,y1,x2,y2), d2=Sign(px2,py,x2,y2,x3,y3), d3=Sign(px2,py,x3,y3,x1,y1);
                if (!((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0))) BlendPixel(px2,py,c);
            }
        }
        float Sign(int px,int py,int x1,int y1,int x2,int y2) => (px-x2)*(y1-y2)-(float)(x1-x2)*(py-y2);

        public void Save(string assetPath)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
