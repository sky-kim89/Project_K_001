#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EquipmentIconGenerator.cs  [Editor Only]
//  장비 아이콘 (48×48) 36종 PNG 생성.
//
//  메뉴: Tools > Project K > 아이콘·텍스처 > 장비 아이콘
//  출력: Assets/_project/3.Textures/Icons/Equipments/
//
//  8가지 형태 × 등급:
//    Armor  normal~epic (5), Sword normal~epic (5),
//    Dagger normal~epic (5), Cmd   normal~epic (5),
//    Glove  rare·epic   (2), Ring  unique·epic (2),
//    Crown  epic        (1), Orb   epic        (1),
//    ArmorSpiked·ArmorHoly·SwordJagged·Banner (각 epic 1)
//    Tome·Sigil·Brand (소환 3), Horn·Drum·Oath (병사 버프 3)
//
//  ⚠ 트리거 장비도 자기 도안을 갖는다 — 아이콘을 공유하지 말 것
//    Epic 갑옷이 셋(신성한/복수의/불사의)인데 그림이 같으면 인벤토리에서
//    무엇을 집는지 이름을 읽기 전까지 알 수 없다. 등급색까지 같아서 더 심하다.
//      복수의 갑옷     → 가시 갑옷 (ArmorSpiked)
//      불사의 갑옷     → 후광 갑옷 (ArmorHoly)
//      광기의 검       → 톱니 검   (SwordJagged)
//      망자의 군기     → 해골 군기 (Banner)
//      망자의 소환서   → 금서     (Tome)
//      해골 군주의 인장 → 육각 인장 (Sigil)
//      처형자의 낙인   → 낙인 인두 (Brand)
//      군단의 뿔피리   → 뿔피리   (Horn)
//      선봉의 북       → 북       (Drum)
//      수호의 서약     → 회복 방패 (Oath)
// ============================================================

public static class EquipmentIconGenerator
{
    const string EQUIP_PATH = "Assets/_project/3.Textures/Icons/Equipments";

    struct GradeTheme { public Color32 BgDark, BgMid, Border, Accent; }

    static GradeTheme Theme(UnitGrade grade) => grade switch
    {
        UnitGrade.Normal   => new GradeTheme { BgDark=Hex("0A0A0A"), BgMid=Hex("2E2E2E"), Border=Hex("888888"), Accent=Hex("CCCCCC") },
        UnitGrade.Uncommon => new GradeTheme { BgDark=Hex("061206"), BgMid=Hex("1A401A"), Border=Hex("44BB44"), Accent=Hex("88EE88") },
        UnitGrade.Rare     => new GradeTheme { BgDark=Hex("040A18"), BgMid=Hex("0E2455"), Border=Hex("2266CC"), Accent=Hex("6699FF") },
        UnitGrade.Unique   => new GradeTheme { BgDark=Hex("0A0415"), BgMid=Hex("2A1055"), Border=Hex("8833CC"), Accent=Hex("BB77FF") },
        UnitGrade.Epic     => new GradeTheme { BgDark=Hex("120602"), BgMid=Hex("401804"), Border=Hex("CC6600"), Accent=Hex("FFAA44") },
        _                  => new GradeTheme { BgDark=Hex("0A0A0A"), BgMid=Hex("2E2E2E"), Border=Hex("888888"), Accent=Hex("CCCCCC") },
    };

    [MenuItem(ProjectKMenu.Icon + "장비 아이콘", priority = ProjectKMenu.IconPrio + 16)]
    public static void GenerateEquipmentIcons()
    {
        EnsureDir(EQUIP_PATH);

        Save("equip_icon_armor_normal",   p => DrawArmor(p, Theme(UnitGrade.Normal)));
        Save("equip_icon_armor_uncommon", p => DrawArmor(p, Theme(UnitGrade.Uncommon)));
        Save("equip_icon_armor_rare",     p => DrawArmor(p, Theme(UnitGrade.Rare)));
        Save("equip_icon_armor_unique",   p => DrawArmor(p, Theme(UnitGrade.Unique)));
        Save("equip_icon_armor_epic",     p => DrawArmor(p, Theme(UnitGrade.Epic)));

        Save("equip_icon_sword_normal",   p => DrawSword(p, Theme(UnitGrade.Normal)));
        Save("equip_icon_sword_uncommon", p => DrawSword(p, Theme(UnitGrade.Uncommon)));
        Save("equip_icon_sword_rare",     p => DrawSword(p, Theme(UnitGrade.Rare)));
        Save("equip_icon_sword_unique",   p => DrawSword(p, Theme(UnitGrade.Unique)));
        Save("equip_icon_sword_epic",     p => DrawSword(p, Theme(UnitGrade.Epic)));

        Save("equip_icon_cmd_normal",     p => DrawCmd(p, Theme(UnitGrade.Normal)));
        Save("equip_icon_cmd_uncommon",   p => DrawCmd(p, Theme(UnitGrade.Uncommon)));
        Save("equip_icon_cmd_rare",       p => DrawCmd(p, Theme(UnitGrade.Rare)));
        Save("equip_icon_cmd_unique",     p => DrawCmd(p, Theme(UnitGrade.Unique)));
        Save("equip_icon_cmd_epic",       p => DrawCmd(p, Theme(UnitGrade.Epic)));

        Save("equip_icon_dagger_normal",   p => DrawDagger(p, Theme(UnitGrade.Normal)));
        Save("equip_icon_dagger_uncommon", p => DrawDagger(p, Theme(UnitGrade.Uncommon)));
        Save("equip_icon_dagger_rare",     p => DrawDagger(p, Theme(UnitGrade.Rare)));
        Save("equip_icon_dagger_unique",   p => DrawDagger(p, Theme(UnitGrade.Unique)));
        Save("equip_icon_dagger_epic",     p => DrawDagger(p, Theme(UnitGrade.Epic)));

        Save("equip_icon_glove_rare",     p => DrawGlove(p, Theme(UnitGrade.Rare)));
        Save("equip_icon_glove_epic",     p => DrawGlove(p, Theme(UnitGrade.Epic)));
        Save("equip_icon_ring_unique",    p => DrawRing(p, Theme(UnitGrade.Unique)));
        Save("equip_icon_ring_epic",      p => DrawRing(p, Theme(UnitGrade.Epic)));
        Save("equip_icon_crown_epic",     p => DrawCrown(p, Theme(UnitGrade.Epic)));
        Save("equip_icon_orb_epic",       p => DrawOrb(p, Theme(UnitGrade.Epic)));

        // 트리거 Epic 전용 도안 — 같은 계열이라도 그림이 겹치면 안 된다
        Save("equip_icon_armor_spiked_epic", p => DrawArmorSpiked(p, Theme(UnitGrade.Epic)));
        Save("equip_icon_armor_holy_epic",   p => DrawArmorHoly(p,   Theme(UnitGrade.Epic)));
        Save("equip_icon_sword_jagged_epic", p => DrawSwordJagged(p, Theme(UnitGrade.Epic)));
        Save("equip_icon_banner_epic",       p => DrawBanner(p,      Theme(UnitGrade.Epic)));

        // 소환 계열
        Save("equip_icon_tome_epic",   p => DrawTome(p,  Theme(UnitGrade.Epic)));
        Save("equip_icon_sigil_epic",  p => DrawSigil(p, Theme(UnitGrade.Epic)));
        Save("equip_icon_brand_epic",  p => DrawBrand(p, Theme(UnitGrade.Epic)));

        // 병사 버프 계열
        Save("equip_icon_horn_epic",   p => DrawHorn(p,  Theme(UnitGrade.Epic)));
        Save("equip_icon_drum_epic",   p => DrawDrum(p,  Theme(UnitGrade.Epic)));
        Save("equip_icon_oath_epic",   p => DrawOath(p,  Theme(UnitGrade.Epic)));

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(EQUIP_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log("[EquipmentIconGenerator] 장비 아이콘 36장 생성 완료.");
    }

    // ── 장비 ID → 아이콘 경로 (EquipmentCreator 에서 사용) ───────
    public static string GetIconPath(string equipmentId) => equipmentId switch
    {
        "equip_armor_normal"    => $"{EQUIP_PATH}/equip_icon_armor_normal.png",
        "equip_armor_uncommon"  => $"{EQUIP_PATH}/equip_icon_armor_uncommon.png",
        "equip_armor_rare"      => $"{EQUIP_PATH}/equip_icon_armor_rare.png",
        "equip_armor_unique"    => $"{EQUIP_PATH}/equip_icon_armor_unique.png",
        "equip_armor_epic"      => $"{EQUIP_PATH}/equip_icon_armor_epic.png",
        "equip_sword_normal"    => $"{EQUIP_PATH}/equip_icon_sword_normal.png",
        "equip_sword_uncommon"  => $"{EQUIP_PATH}/equip_icon_sword_uncommon.png",
        "equip_sword_rare"      => $"{EQUIP_PATH}/equip_icon_sword_rare.png",
        "equip_sword_unique"    => $"{EQUIP_PATH}/equip_icon_sword_unique.png",
        "equip_sword_epic"      => $"{EQUIP_PATH}/equip_icon_sword_epic.png",
        "equip_dagger_normal"   => $"{EQUIP_PATH}/equip_icon_dagger_normal.png",
        "equip_dagger_uncommon" => $"{EQUIP_PATH}/equip_icon_dagger_uncommon.png",
        "equip_dagger_rare"     => $"{EQUIP_PATH}/equip_icon_dagger_rare.png",
        "equip_dagger_unique"   => $"{EQUIP_PATH}/equip_icon_dagger_unique.png",
        "equip_dagger_epic"     => $"{EQUIP_PATH}/equip_icon_dagger_epic.png",
        "equip_cmd_normal"      => $"{EQUIP_PATH}/equip_icon_cmd_normal.png",
        "equip_cmd_uncommon"    => $"{EQUIP_PATH}/equip_icon_cmd_uncommon.png",
        "equip_cmd_rare"        => $"{EQUIP_PATH}/equip_icon_cmd_rare.png",
        "equip_cmd_unique"      => $"{EQUIP_PATH}/equip_icon_cmd_unique.png",
        "equip_cmd_epic"        => $"{EQUIP_PATH}/equip_icon_cmd_epic.png",
        "equip_sniper_rare"     => $"{EQUIP_PATH}/equip_icon_glove_rare.png",
        "equip_chrono_epic"     => $"{EQUIP_PATH}/equip_icon_crown_epic.png",
        "equip_vamp_epic"       => $"{EQUIP_PATH}/equip_icon_ring_epic.png",
        "equip_hunter_epic"     => $"{EQUIP_PATH}/equip_icon_glove_epic.png",
        "equip_arcane_epic"     => $"{EQUIP_PATH}/equip_icon_orb_epic.png",
        "equip_revenge_epic"    => $"{EQUIP_PATH}/equip_icon_armor_spiked_epic.png",
        "equip_immortal_epic"   => $"{EQUIP_PATH}/equip_icon_armor_holy_epic.png",
        "equip_berserk_epic"    => $"{EQUIP_PATH}/equip_icon_sword_jagged_epic.png",
        "equip_requiem_epic"    => $"{EQUIP_PATH}/equip_icon_banner_epic.png",
        "equip_necro_epic"      => $"{EQUIP_PATH}/equip_icon_tome_epic.png",
        "equip_lich_epic"       => $"{EQUIP_PATH}/equip_icon_sigil_epic.png",
        "equip_executioner_epic"=> $"{EQUIP_PATH}/equip_icon_brand_epic.png",
        "equip_horn_epic"       => $"{EQUIP_PATH}/equip_icon_horn_epic.png",
        "equip_drum_epic"       => $"{EQUIP_PATH}/equip_icon_drum_epic.png",
        "equip_oath_epic"       => $"{EQUIP_PATH}/equip_icon_oath_epic.png",
        _                       => null,
    };

    // ── 형태 그리기 ───────────────────────────────────────────────

    static void DrawArmor(P p, GradeTheme g)
    {
        var steel   = Hex("AAAAAA");
        var steelDk = Hex("666666");
        var steelLt = Hex("DDDDDD");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 어깨 패드
        p.FillEllipse(13, 15, 8, 6, steelDk);
        p.FillEllipse(35, 15, 8, 6, steelDk);

        // 흉갑 본체
        p.FillRRect(11, 14, 26, 22, 3, steel);
        p.FillRect(11, 14, 26, 7, steelLt);

        // 봉합선
        p.DrawLine(cx, 15, cx, 35, new Color32(80, 80, 80, 80), 1);
        p.DrawLine(12, 24, 36, 24, new Color32(80, 80, 80, 60), 1);

        // 중앙 엠블럼 (등급 색)
        p.FillCircle(cx, 26, 5, g.BgMid);
        p.DrawCircle(cx, 26, 5, 1, g.Accent);
        p.FillCircle(cx, 26, 2, g.Accent);

        // 허리 벨트
        p.FillRect(11, 36, 26, 3, steelDk);
        p.DrawLine(11, 36, 37, 36, g.Border, 1);

        // 버클 (등급 색)
        p.FillRRect(cx - 4, 35, 8, 5, 1, g.Accent);
    }

    static void DrawSword(P p, GradeTheme g)
    {
        var silver   = Hex("D8D8D8");
        var silverDk = Hex("888888");
        var wood     = Hex("6A3818");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 검날 끝
        p.FillTri(cx - 4, 8, cx + 4, 8, cx, 4, silver);
        // 검날 본체
        p.FillRect(cx - 4, 8, 8, 24, silver);
        // 능선 하이라이트
        p.FillRect(cx - 1, 9, 2, 22, Hex("FFFFFF"));
        // 날 그늘
        p.DrawLine(cx - 4, 8, cx - 4, 32, silverDk, 1);
        p.DrawLine(cx + 4, 8, cx + 4, 32, silverDk, 1);

        // 크로스가드 (등급 색)
        p.FillRRect(cx - 12, 30, 24, 5, 2, g.Accent);
        p.FillRect(cx - 12, 30, 24, 2, new Color32(255, 255, 255, 50));
        p.FillCircle(cx, 33, 3, g.BgMid);
        p.DrawCircle(cx, 33, 3, 1, g.Accent);
        p.FillCircle(cx, 33, 1, Hex("FFFFFF"));

        // 손잡이
        p.FillRRect(cx - 3, 35, 6, 8, 1, wood);
        p.DrawLine(cx - 3, 38, cx + 3, 38, Hex("3A1A08"), 1);
        p.DrawLine(cx - 3, 41, cx + 3, 41, Hex("3A1A08"), 1);

        // 폼멜 (등급 색)
        p.FillCircle(cx, 46, 4, g.Accent);
        p.FillCircle(cx - 1, 45, 2, new Color32(255, 255, 255, 100));
    }

    static void DrawCmd(P p, GradeTheme g)
    {
        var wood   = Hex("7A3A14");
        var woodLt = Hex("A85A24");

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 깃대
        p.DrawLine(10, 44, 32, 8, wood, 4);
        p.DrawLine(11, 43, 33, 8, woodLt, 2);

        // 깃발 천 (페넌트)
        p.FillTri(32, 8, 44, 16, 32, 28, g.BgMid);
        p.DrawLine(32, 8,  44, 16, g.Border, 2);
        p.DrawLine(44, 16, 32, 28, g.Border, 2);
        p.DrawLine(32, 8,  32, 28, new Color32(g.Border.r, g.Border.g, g.Border.b, 80), 1);

        // 깃발 다이아몬드 문양 (등급 색)
        p.FillTri(38, 13, 43, 17, 38, 21, g.Accent);
        p.FillTri(38, 13, 33, 17, 38, 21, new Color32(g.Accent.r, g.Accent.g, g.Accent.b, 140));

        // 깃대 끝 장식
        p.FillCircle(32, 8, 3, g.Accent);
        p.FillCircle(31, 7, 1, new Color32(255, 255, 255, 180));
    }

    static void DrawGlove(P p, GradeTheme g)
    {
        var leather   = Hex("5A3820");
        var leatherLt = Hex("8B5830");
        var leatherDk = Hex("3A2210");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 손가락 (4개)
        int[] fxs = { 12, 18, 24, 30 };
        foreach (int fx in fxs)
        {
            p.FillRRect(fx, 11, 5, 13, 2, leather);
            p.DrawLine(fx, 11, fx + 5, 11, leatherLt, 1);
        }

        // 손바닥
        p.FillRRect(10, 21, 28, 20, 3, leather);
        p.FillRect(10, 21, 28, 6, leatherLt);

        // 손목
        p.FillRRect(12, 38, 24, 6, 2, leatherDk);
        p.DrawLine(12, 38, 36, 38, g.Border, 1);

        // 조준선 크로스헤어 (등급 색)
        p.DrawCircle(cx, 29, 5, 1, g.Accent);
        p.DrawLine(cx - 9, 29, cx - 6, 29, g.Accent, 1);
        p.DrawLine(cx + 6, 29, cx + 9, 29, g.Accent, 1);
        p.DrawLine(cx, 21, cx, 24, g.Accent, 1);
        p.DrawLine(cx, 34, cx, 37, g.Accent, 1);
        p.FillCircle(cx, 29, 2, g.Accent);
    }

    // 단검 — 검보다 날이 짧고 넓다. 등급 색은 코등이·폼멜에 들어간다.
    static void DrawDagger(P p, GradeTheme g)
    {
        var silver   = Hex("D8D8D8");
        var silverDk = Hex("888888");
        var wrap     = Hex("2A2A32");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 날 끝 — 검보다 짧고 각이 급하다
        p.FillTri(cx - 6, 16, cx + 6, 16, cx, 6, silver);
        // 날 본체
        p.FillRect(cx - 6, 16, 12, 14, silver);
        // 능선
        p.FillRect(cx - 1, 8, 2, 21, Hex("FFFFFF"));
        // 날 그늘
        p.DrawLine(cx - 6, 16, cx - 6, 30, silverDk, 1);
        p.DrawLine(cx + 6, 16, cx + 6, 30, silverDk, 1);

        // 코등이 (등급 색) — 좁게
        p.FillRRect(cx - 9, 29, 18, 4, 1, g.Accent);
        p.FillRect(cx - 9, 29, 18, 1, new Color32(255, 255, 255, 60));

        // 감아 쥔 손잡이
        p.FillRRect(cx - 3, 33, 6, 10, 1, wrap);
        p.DrawLine(cx - 3, 35, cx + 3, 35, g.Border, 1);
        p.DrawLine(cx - 3, 38, cx + 3, 38, g.Border, 1);
        p.DrawLine(cx - 3, 41, cx + 3, 41, g.Border, 1);

        // 폼멜
        p.FillCircle(cx, 45, 3, g.Accent);
        p.BlendPixel(cx - 1, 44, new Color32(255, 255, 255, 180));
    }

    // 왕관 — 쿨다운 장비. 가운데 보석이 시간을 상징한다.
    static void DrawCrown(P p, GradeTheme g)
    {
        var gold   = Hex("C89020");
        var goldLt = Hex("E8C040");
        var goldDk = Hex("7A5010");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 뿔 세 개
        p.FillTri(10, 32, 18, 32, 14, 12, gold);
        p.FillTri(18, 32, 30, 32, cx,  8, gold);
        p.FillTri(30, 32, 38, 32, 34, 12, gold);

        // 뿔 하이라이트
        p.DrawLine(14, 13, 12, 31, goldLt, 1);
        p.DrawLine(cx, 9,  20, 31, goldLt, 1);
        p.DrawLine(34, 13, 32, 31, goldLt, 1);

        // 뿔 끝 보석 (등급 색)
        p.FillCircle(14, 12, 2, g.Accent);
        p.FillCircle(cx, 8,  3, g.Accent);
        p.FillCircle(34, 12, 2, g.Accent);

        // 관테
        p.FillRRect(9, 31, 30, 9, 2, gold);
        p.FillRect(9, 31, 30, 2, goldLt);
        p.FillRect(9, 38, 30, 2, goldDk);

        // 중앙 보석
        p.FillCircleGrad(cx, 35, 4, Hex("FFFFFF"), g.Accent, g.BgMid);
        p.DrawCircle(cx, 35, 4, 1, goldDk);

        // 시곗바늘 — 이 왕관이 무엇을 줄이는지 읽히게
        p.DrawLine(cx, 35, cx,     32, Hex("FFFFFF"), 1);
        p.DrawLine(cx, 35, cx + 3, 36, Hex("FFFFFF"), 1);
    }

    // 오브 — 스킬 계열 장비. 구슬 안에서 빛이 돈다.
    static void DrawOrb(P p, GradeTheme g)
    {
        var standDk = Hex("3A2A18");
        var stand   = Hex("6A4A28");
        int cx = 24, cy = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 구슬
        p.FillCircleGrad(cx, cy, 13, Hex("FFFFFF"), g.Accent, g.BgDark);
        p.DrawCircle(cx, cy, 13, 1, g.Border);

        // 내부 소용돌이
        p.DrawCircle(cx, cy, 8, 1, new Color32(255, 255, 255, 90));
        p.DrawLine(cx - 6, cy + 4, cx + 5, cy - 5, new Color32(255, 255, 255, 70), 1);

        // 하이라이트
        p.FillCircle(cx - 5, cy - 6, 3, new Color32(255, 255, 255, 170));

        // 받침대
        p.FillRRect(cx - 10, 38, 20, 5, 2, stand);
        p.FillRect(cx - 10, 38, 20, 1, new Color32(255, 255, 255, 50));
        p.FillRRect(cx - 6, 34, 12, 5, 1, standDk);
    }

    // 복수의 갑옷 — 가시가 돋은 검붉은 갑옷. 맞을수록 세지는 물건이라 날이 밖을 향한다.
    static void DrawArmorSpiked(P p, GradeTheme g)
    {
        var steel   = Hex("7A5A5A");
        var steelDk = Hex("4A3232");
        var steelLt = Hex("A88888");
        var blood   = Hex("B02020");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 어깨 가시 (좌우 3개씩)
        for (int i = 0; i < 3; i++)
        {
            int lx = 8 + i * 4, rx = 32 + i * 4;
            p.FillTri(lx, 16, lx + 4, 16, lx + 2, 16 - (6 - i * 2), steelLt);
            p.FillTri(rx, 16, rx + 4, 16, rx + 2, 16 - (2 + i * 2), steelLt);
        }

        // 어깨 패드
        p.FillEllipse(13, 17, 8, 5, steelDk);
        p.FillEllipse(35, 17, 8, 5, steelDk);

        // 흉갑 본체 — 아래로 좁아지는 역사다리꼴
        p.FillRRect(11, 16, 26, 20, 3, steel);
        p.FillRect(11, 16, 26, 5, steelLt);
        p.FillTri(11, 36, 37, 36, cx, 42, steel);

        // 갈라진 균열 (피가 밴 자국)
        p.DrawLine(cx - 6, 20, cx - 2, 28, blood, 1);
        p.DrawLine(cx - 2, 28, cx - 7, 34, blood, 1);
        p.DrawLine(cx + 5, 22, cx + 2, 30, blood, 1);

        // 중앙 가시 (등급 색)
        p.FillTri(cx - 5, 30, cx + 5, 30, cx, 20, g.Accent);
        p.DrawLine(cx, 20, cx, 30, Hex("FFFFFF"), 1);

        // 벨트
        p.FillRect(11, 33, 26, 3, steelDk);
        p.FillRRect(cx - 4, 32, 8, 5, 1, g.Accent);
    }

    // 불사의 갑옷 — 후광이 걸린 흰 갑옷. 회복 쪽이라 날카로운 선을 쓰지 않는다.
    static void DrawArmorHoly(P p, GradeTheme g)
    {
        var steel   = Hex("D8D4C4");
        var steelDk = Hex("9A9484");
        var steelLt = Hex("FFFCF0");
        var halo    = Hex("FFE08A");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 후광
        p.DrawCircle(cx, 12, 9, 2, halo);
        p.DrawCircle(cx, 12, 12, 1, new Color32(halo.r, halo.g, halo.b, 90));

        // 어깨 패드 (둥글게)
        p.FillEllipse(13, 20, 8, 6, steelDk);
        p.FillEllipse(35, 20, 8, 6, steelDk);

        // 흉갑 본체
        p.FillRRect(11, 19, 26, 21, 5, steel);
        p.FillRect(11, 19, 26, 6, steelLt);

        // 성흔 십자 (등급 색)
        p.FillRect(cx - 2, 24, 4, 14, g.Accent);
        p.FillRect(cx - 7, 28, 14, 4, g.Accent);
        p.FillCircle(cx, 30, 2, steelLt);

        // 아래 테두리
        p.FillRect(11, 39, 26, 3, steelDk);
        p.DrawLine(11, 39, 37, 39, halo, 1);

        // 반짝임
        p.BlendPixel(cx - 9, 22, new Color32(255, 255, 255, 200));
        p.BlendPixel(cx + 9, 26, new Color32(255, 255, 255, 160));
    }

    // 광기의 검 — 날이 톱니처럼 뜯긴 검. 검 계열이지만 실루엣이 확실히 다르다.
    static void DrawSwordJagged(P p, GradeTheme g)
    {
        var blade   = Hex("9A8898");
        var bladeDk = Hex("5A4A58");
        var glow    = Hex("D02828");
        var wrap    = Hex("2A1A1A");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 날 본체 (끝이 비스듬한 넓은 날)
        p.FillTri(cx - 5, 10, cx + 5, 10, cx + 2, 3, blade);
        p.FillRect(cx - 5, 10, 10, 20, blade);

        // 톱니 — 왼쪽으로 뜯긴 자국
        for (int i = 0; i < 4; i++)
        {
            int y = 12 + i * 5;
            p.FillTri(cx - 5, y, cx - 5, y + 4, cx - 9, y + 2, blade);
            p.DrawLine(cx - 5, y, cx - 9, y + 2, bladeDk, 1);
        }

        // 핏빛 능선
        p.FillRect(cx - 1, 5, 2, 24, glow);
        p.DrawLine(cx + 5, 10, cx + 5, 30, bladeDk, 1);

        // 코등이 — 아래로 처진 뿔
        p.FillRRect(cx - 11, 29, 22, 4, 1, g.Accent);
        p.FillTri(cx - 11, 33, cx - 5, 33, cx - 9, 38, g.Accent);
        p.FillTri(cx + 5, 33, cx + 11, 33, cx + 9, 38, g.Accent);

        // 손잡이
        p.FillRRect(cx - 3, 33, 6, 9, 1, wrap);
        p.DrawLine(cx - 3, 36, cx + 3, 36, glow, 1);
        p.DrawLine(cx - 3, 39, cx + 3, 39, glow, 1);

        // 폼멜 (붉은 눈)
        p.FillCircle(cx, 44, 4, wrap);
        p.FillCircle(cx, 44, 2, glow);
    }

    // 망자의 군기 — 사각 깃발에 해골. 지휘봉(페넌트)과 실루엣이 겹치지 않는다.
    static void DrawBanner(P p, GradeTheme g)
    {
        var pole   = Hex("4A3A2A");
        var poleLt = Hex("6A5A42");
        var cloth  = Hex("2A2230");
        var bone   = Hex("E8E4D8");
        int px = 13;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 깃대 (수직)
        p.FillRect(px - 2, 6, 4, 38, pole);
        p.FillRect(px - 2, 6, 1, 38, poleLt);

        // 깃대 끝 창날
        p.FillTri(px - 3, 8, px + 3, 8, px, 2, g.Accent);

        // 깃발 천 (아래가 제비꼬리)
        p.FillRect(px + 2, 9, 22, 22, cloth);
        p.FillTri(px + 2, 31, px + 12, 31, px + 7, 38, cloth);
        p.FillTri(px + 12, 31, px + 24, 31, px + 18, 38, cloth);
        p.DrawLine(px + 2, 9, px + 24, 9, g.Accent, 1);
        p.DrawLine(px + 24, 9, px + 24, 31, g.Border, 1);

        // 해골 문양
        int sx = px + 13, sy = 18;
        p.FillCircle(sx, sy, 6, bone);
        p.FillRect(sx - 4, sy + 4, 8, 5, bone);
        p.FillCircle(sx - 2, sy - 1, 2, cloth);   // 눈
        p.FillCircle(sx + 2, sy - 1, 2, cloth);
        p.FillRect(sx - 1, sy + 3, 2, 3, cloth);  // 코
        p.DrawLine(sx - 3, sy + 7, sx + 3, sy + 7, cloth, 1);

        // 깃발 아래 술 (등급 색)
        p.DrawLine(px + 4, 31, px + 4, 34, g.Accent, 1);
        p.DrawLine(px + 22, 31, px + 22, 34, g.Accent, 1);
    }

    // 망자의 소환서 — 펼쳐진 금서. 페이지 위로 혼불이 떠오른다.
    static void DrawTome(P p, GradeTheme g)
    {
        var cover   = Hex("3A1E2A");
        var coverLt = Hex("5C3242");
        var page    = Hex("E8E0CC");
        var pageDk  = Hex("BCB49C");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 표지 (펼친 상태)
        p.FillRRect(6, 24, 36, 16, 2, cover);
        p.FillRect(6, 24, 36, 2, coverLt);

        // 좌우 페이지
        p.FillTri(8, 26, 23, 26, 8, 36, page);
        p.FillTri(23, 26, 40, 26, 40, 36, page);
        p.FillRect(8, 26, 15, 10, page);
        p.FillRect(25, 26, 15, 10, page);
        p.DrawLine(8, 36, 23, 36, pageDk, 1);
        p.DrawLine(25, 36, 40, 36, pageDk, 1);

        // 책등
        p.FillRect(cx - 1, 24, 2, 16, cover);

        // 글줄
        for (int i = 0; i < 3; i++)
        {
            p.DrawLine(10, 29 + i * 3, 21, 29 + i * 3, pageDk, 1);
            p.DrawLine(27, 29 + i * 3, 38, 29 + i * 3, pageDk, 1);
        }

        // 떠오르는 혼불 (등급 색)
        p.FillCircleGrad(cx, 14, 6, Hex("FFFFFF"), g.Accent, g.BgDark);
        p.FillCircle(cx - 7, 20, 2, g.Accent);
        p.FillCircle(cx + 7, 19, 2, new Color32(g.Accent.r, g.Accent.g, g.Accent.b, 170));

        // 혼불 속 해골 눈
        p.FillCircle(cx - 2, 13, 1, cover);
        p.FillCircle(cx + 2, 13, 1, cover);
    }

    // 해골 군주의 인장 — 육각 인장 위의 해골. 반지·오브와 겹치지 않게 각진 형태.
    static void DrawSigil(P p, GradeTheme g)
    {
        var metal   = Hex("8A8A96");
        var metalDk = Hex("4A4A56");
        var bone     = Hex("E8E4D8");
        int cx = 24, cy = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 육각 판 (삼각 두 개로)
        p.FillTri(cx - 14, cy - 6, cx + 14, cy - 6, cx, cy - 18, metal);
        p.FillTri(cx - 14, cy + 6, cx + 14, cy + 6, cx, cy + 18, metal);
        p.FillRect(cx - 14, cy - 7, 28, 14, metal);
        p.DrawLine(cx - 14, cy - 6, cx, cy - 18, g.Accent, 1);
        p.DrawLine(cx + 14, cy - 6, cx, cy - 18, g.Accent, 1);
        p.DrawLine(cx - 14, cy + 6, cx, cy + 18, g.Accent, 1);
        p.DrawLine(cx + 14, cy + 6, cx, cy + 18, g.Accent, 1);
        p.DrawLine(cx - 14, cy - 6, cx - 14, cy + 6, g.Accent, 1);
        p.DrawLine(cx + 14, cy - 6, cx + 14, cy + 6, g.Accent, 1);

        // 안쪽 음각
        p.FillRRect(cx - 9, cy - 9, 18, 18, 3, metalDk);

        // 해골
        p.FillCircle(cx, cy - 2, 6, bone);
        p.FillRect(cx - 4, cy + 2, 8, 5, bone);
        p.FillCircle(cx - 2, cy - 3, 2, metalDk);
        p.FillCircle(cx + 2, cy - 3, 2, metalDk);
        p.DrawLine(cx - 3, cy + 5, cx + 3, cy + 5, metalDk, 1);

        // 왕관 뿔 (등급 색)
        p.FillTri(cx - 7, cy - 7, cx - 3, cy - 7, cx - 5, cy - 12, g.Accent);
        p.FillTri(cx + 3, cy - 7, cx + 7, cy - 7, cx + 5, cy - 12, g.Accent);
    }

    // 처형자의 낙인 — 달군 낙인 인두. 손잡이 + 벌겋게 달아오른 인장부.
    static void DrawBrand(P p, GradeTheme g)
    {
        var iron   = Hex("6A6A72");
        var ironDk = Hex("3A3A42");
        var wood   = Hex("6A3818");
        var hot    = Hex("FF6420");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 손잡이 (아래)
        p.FillRRect(cx - 3, 30, 6, 14, 2, wood);
        p.DrawLine(cx - 3, 34, cx + 3, 34, Hex("3A1A08"), 1);
        p.DrawLine(cx - 3, 38, cx + 3, 38, Hex("3A1A08"), 1);

        // 자루
        p.FillRect(cx - 2, 20, 4, 11, iron);
        p.DrawLine(cx - 2, 20, cx - 2, 31, ironDk, 1);

        // 인장부 (달아오른 사각 틀)
        p.FillRRect(cx - 10, 8, 20, 13, 3, ironDk);
        p.FillRRect(cx - 8, 10, 16, 9, 2, Hex("2A1208"));
        p.DrawLine(cx - 10, 8, cx + 10, 8, hot, 1);
        p.DrawLine(cx - 10, 20, cx + 10, 20, hot, 1);

        // 낙인 문양 — 처형 표식 (교차선)
        p.DrawLine(cx - 5, 12, cx + 5, 18, hot, 2);
        p.DrawLine(cx + 5, 12, cx - 5, 18, hot, 2);

        // 열기 불티
        p.FillCircle(cx - 12, 6, 1, g.Accent);
        p.FillCircle(cx + 12, 5, 1, g.Accent);
        p.FillCircle(cx + 9, 3, 1, new Color32(g.Accent.r, g.Accent.g, g.Accent.b, 150));
    }

    // 군단의 뿔피리 — 굽은 뿔 + 울림선.
    static void DrawHorn(P p, GradeTheme g)
    {
        var horn   = Hex("D8C8A8");
        var hornDk = Hex("9A8A6A");
        var band   = Hex("C89020");

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 굽은 몸통 — 점점 굵어지는 원을 이어 그린다
        for (int i = 0; i <= 20; i++)
        {
            float t  = i / 20f;
            int   x  = 10 + Mathf.RoundToInt(t * 26);
            int   y  = 34 - Mathf.RoundToInt(Mathf.Sin(t * 2.2f) * 16);
            int   r  = 2 + Mathf.RoundToInt(t * 5);
            p.FillCircle(x, y, r, i < 4 ? hornDk : horn);
        }

        // 나팔 입구
        p.FillEllipse(37, 15, 6, 8, horn);
        p.DrawCircle(37, 15, 6, 1, hornDk);
        p.FillEllipse(37, 15, 4, 6, g.BgDark);

        // 금테 두 줄
        p.DrawLine(17, 30, 21, 26, band, 2);
        p.DrawLine(26, 22, 30, 19, band, 2);

        // 울림선 (등급 색)
        p.DrawCircle(37, 15, 10, 1, new Color32(g.Accent.r, g.Accent.g, g.Accent.b, 150));
        p.DrawCircle(37, 15, 13, 1, new Color32(g.Accent.r, g.Accent.g, g.Accent.b, 80));
    }

    // 선봉의 북 — 정면 북 + 채 두 자루.
    static void DrawDrum(P p, GradeTheme g)
    {
        var skin   = Hex("E4D8C0");
        var skinDk = Hex("B0A488");
        var body   = Hex("8A3A22");
        var rope   = Hex("D8C060");
        int cx = 24, cy = 26;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 북통
        p.FillRRect(cx - 13, cy - 10, 26, 20, 4, body);
        // 가죽 면
        p.FillEllipse(cx, cy, 13, 10, skin);
        p.DrawCircle(cx, cy, 10, 1, skinDk);

        // 조임줄 (지그재그)
        for (int i = -2; i <= 2; i++)
        {
            p.DrawLine(cx + i * 6, cy - 9, cx + i * 6 + 3, cy + 9, rope, 1);
            p.DrawLine(cx + i * 6 + 3, cy + 9, cx + i * 6 + 6, cy - 9, rope, 1);
        }

        // 중앙 문양 (등급 색)
        p.FillCircle(cx, cy, 4, g.Accent);
        p.FillCircle(cx, cy, 2, skin);

        // 북채 두 자루
        p.DrawLine(cx - 16, 40, cx - 4, 30, Hex("6A4A28"), 2);
        p.FillCircle(cx - 3, 29, 2, skinDk);
        p.DrawLine(cx + 16, 40, cx + 4, 30, Hex("6A4A28"), 2);
        p.FillCircle(cx + 3, 29, 2, skinDk);
    }

    // 수호의 서약 — 방패 + 회복을 뜻하는 십자. 갑옷과 실루엣이 다르다.
    static void DrawOath(P p, GradeTheme g)
    {
        var steel   = Hex("C8CCD8");
        var steelDk = Hex("7A8090");
        var steelLt = Hex("F0F4FF");
        var heal    = Hex("6ADC8A");
        int cx = 24;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 방패 — 위는 사각, 아래는 뾰족
        p.FillRRect(cx - 13, 8, 26, 20, 3, steel);
        p.FillTri(cx - 13, 28, cx + 13, 28, cx, 42, steel);
        p.FillRect(cx - 13, 8, 26, 4, steelLt);

        // 테두리
        p.DrawLine(cx - 13, 8, cx - 13, 28, steelDk, 1);
        p.DrawLine(cx + 13, 8, cx + 13, 28, steelDk, 1);
        p.DrawLine(cx - 13, 28, cx, 42, steelDk, 1);
        p.DrawLine(cx + 13, 28, cx, 42, steelDk, 1);

        // 등급 띠
        p.FillRect(cx - 13, 24, 26, 3, g.Accent);

        // 회복 십자 (초록)
        p.FillRect(cx - 3, 12, 6, 18, heal);
        p.FillRect(cx - 9, 17, 18, 6, heal);
        p.FillRect(cx - 2, 13, 2, 16, new Color32(255, 255, 255, 120));

        // 아래 끝 장식
        p.FillCircle(cx, 40, 2, g.Accent);
    }

    static void DrawRing(P p, GradeTheme g)
    {
        var gold   = Hex("C89020");
        var goldLt = Hex("E8C040");
        var goldDk = Hex("7A5010");
        int cx = 24, cy = 32;

        p.BgGradient(g.BgDark, g.BgMid);
        p.RoundedBorder(8, 1, g.Border);

        // 밴드
        p.DrawCircle(cx, cy, 11, 2, gold);
        // 상단 하이라이트
        for (int a = -70; a <= 70; a += 2)
        {
            float rad = a * Mathf.Deg2Rad;
            int bx = cx + Mathf.RoundToInt(Mathf.Cos(rad) * 11);
            int by = cy - Mathf.RoundToInt(Mathf.Sin(rad) * 11);
            p.BlendPixel(bx, by,     goldLt);
            p.BlendPixel(bx, by - 1, goldLt);
        }
        // 하단 그림자
        for (int a = 110; a <= 250; a += 2)
        {
            float rad = a * Mathf.Deg2Rad;
            int bx = cx + Mathf.RoundToInt(Mathf.Cos(rad) * 11);
            int by = cy - Mathf.RoundToInt(Mathf.Sin(rad) * 11);
            p.BlendPixel(bx, by, goldDk);
        }

        // 보석 받침대
        p.FillCircle(cx, cy - 11, 5, goldDk);
        p.DrawCircle(cx, cy - 11, 5, 1, gold);

        // 보석 (등급 색)
        p.FillCircleGrad(cx, cy - 11, 4, Hex("FFFFFF"), g.Accent, g.BgMid);
        p.BlendPixel(cx - 1, cy - 13, new Color32(255, 255, 255, 200));

        // 반짝임
        p.DrawLine(cx,     cy - 17, cx,     cy - 21, g.Accent, 1);
        p.DrawLine(cx - 3, cy - 14, cx - 6, cy - 17, g.Accent, 1);
        p.DrawLine(cx + 3, cy - 14, cx + 6, cy - 17, g.Accent, 1);
    }

    // ── 유틸 ──────────────────────────────────────────────────────

    static void Save(string name, Action<P> draw)
    {
        var painter = new P(48, 48);
        draw(painter);
        painter.Save($"{EQUIP_PATH}/{name}.png");
    }

    static void EnsureDir(string assetPath)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", assetPath));
    }

    static void ApplySpriteImportSettings(string folder)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.spritePivot         = new Vector2(0.5f, 0.5f);
            imp.filterMode          = FilterMode.Bilinear;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize      = 128;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
    }

    static Color32 Hex(string h)
    {
        h = h.TrimStart('#');
        return new Color32(
            Convert.ToByte(h.Substring(0, 2), 16),
            Convert.ToByte(h.Substring(2, 2), 16),
            Convert.ToByte(h.Substring(4, 2), 16), 255);
    }

    // ═══════════════════════════════════════════════════════
    //  ■ Painter (IconGenerator.P 복사)
    // ═══════════════════════════════════════════════════════
    class P
    {
        public int W, H;
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

        public void BgGradient(Color32 dark, Color32 mid)
        {
            int cx = W / 2, cy = H / 2;
            float maxD = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                const int R = 8;
                int dx = 0, dy = 0;
                if      (x < R   && y < R)   { dx = R - x;       dy = R - y; }
                else if (x>=W-R  && y < R)   { dx = x-(W-R-1);   dy = R - y; }
                else if (x < R   && y>=H-R)  { dx = R - x;       dy = y-(H-R-1); }
                else if (x>=W-R  && y>=H-R)  { dx = x-(W-R-1);   dy = y-(H-R-1); }
                if (dx*dx + dy*dy > R*R && (dx > 0 || dy > 0)) continue;
                float t = Mathf.Clamp01(Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy)) / maxD);
                BlendPixel(x, y, Color32.Lerp(mid, dark, t * t));
            }
        }

        public void RoundedBorder(int r, int thick, Color32 col)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int dx = 0, dy = 0; bool corner = false;
                if      (x < r   && y < r)   { dx=r-x;       dy=r-y;       corner=true; }
                else if (x>=W-r  && y < r)   { dx=x-(W-r-1); dy=r-y;       corner=true; }
                else if (x < r   && y>=H-r)  { dx=r-x;       dy=y-(H-r-1); corner=true; }
                else if (x>=W-r  && y>=H-r)  { dx=x-(W-r-1); dy=y-(H-r-1); corner=true; }
                bool onEdge;
                if (corner) { float d = Mathf.Sqrt(dx*dx+dy*dy); onEdge = d >= r-thick && d <= r; }
                else         onEdge = x < thick || x >= W-thick || y < thick || y >= H-thick;
                if (onEdge) BlendPixel(x, y, col);
            }
        }

        public void FillRect(int x, int y, int w, int h, Color32 c)
        { for (int py=y; py<y+h; py++) for (int px2=x; px2<x+w; px2++) BlendPixel(px2,py,c); }

        public void FillRRect(int x, int y, int w, int h, int rad, Color32 c)
        {
            for (int py=y; py<y+h; py++)
            for (int px2=x; px2<x+w; px2++)
            {
                int dx=0, dy=0;
                if      (px2<x+rad   && py<y+rad)   { dx=x+rad-px2;       dy=y+rad-py; }
                else if (px2>=x+w-rad && py<y+rad)  { dx=px2-(x+w-rad-1); dy=y+rad-py; }
                else if (px2<x+rad   && py>=y+h-rad){ dx=x+rad-px2;       dy=py-(y+h-rad-1); }
                else if (px2>=x+w-rad && py>=y+h-rad){ dx=px2-(x+w-rad-1); dy=py-(y+h-rad-1); }
                if (dx*dx+dy*dy <= rad*rad || (dx==0&&dy==0)) BlendPixel(px2,py,c);
            }
        }

        public void FillCircle(int cx, int cy, int r, Color32 c)
        {
            for (int y=cy-r; y<=cy+r; y++)
            for (int x=cx-r; x<=cx+r; x++)
                if ((x-cx)*(x-cx)+(y-cy)*(y-cy) <= r*r) BlendPixel(x,y,c);
        }

        public void FillCircleGrad(int cx, int cy, int r, Color32 inner, Color32 mid, Color32 outer)
        {
            for (int y=cy-r; y<=cy+r; y++)
            for (int x=cx-r; x<=cx+r; x++)
            {
                int d2 = (x-cx)*(x-cx)+(y-cy)*(y-cy);
                if (d2 > r*r) continue;
                float t = Mathf.Sqrt(d2) / r;
                BlendPixel(x, y, t < 0.5f
                    ? Color32.Lerp(inner, mid, t*2)
                    : Color32.Lerp(mid, outer, (t-0.5f)*2));
            }
        }

        public void DrawCircle(int cx, int cy, int r, int thick, Color32 c)
        {
            for (int y=cy-r-thick; y<=cy+r+thick; y++)
            for (int x=cx-r-thick; x<=cx+r+thick; x++)
            {
                int d2 = (x-cx)*(x-cx)+(y-cy)*(y-cy);
                if (d2 >= (r-thick)*(r-thick) && d2 <= (r+thick)*(r+thick)) BlendPixel(x,y,c);
            }
        }

        public void FillEllipse(int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y=cy-ry; y<=cy+ry; y++)
            for (int x=cx-rx; x<=cx+rx; x++)
            {
                float fx=(float)(x-cx)/rx, fy=(float)(y-cy)/ry;
                if (fx*fx+fy*fy <= 1f) BlendPixel(x,y,c);
            }
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color32 c, int thick=1)
        {
            int dx=Mathf.Abs(x2-x1), dy=Mathf.Abs(y2-y1);
            int sx=x1<x2?1:-1, sy=y1<y2?1:-1;
            int err=dx-dy, x=x1, y=y1, h2=thick/2;
            while (true)
            {
                for (int py=y-h2; py<=y+h2; py++)
                for (int px2=x-h2; px2<=x+h2; px2++)
                    BlendPixel(px2, py, c);
                if (x==x2 && y==y2) break;
                int e2 = 2*err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 <  dx) { err += dx; y += sy; }
            }
        }

        public void FillTri(int x1,int y1, int x2,int y2, int x3,int y3, Color32 c)
        {
            int minX=Mathf.Min(x1,Mathf.Min(x2,x3)), maxX=Mathf.Max(x1,Mathf.Max(x2,x3));
            int minY=Mathf.Min(y1,Mathf.Min(y2,y3)), maxY=Mathf.Max(y1,Mathf.Max(y2,y3));
            for (int py=minY; py<=maxY; py++)
            for (int px2=minX; px2<=maxX; px2++)
            {
                float d1=Sign(px2,py,x1,y1,x2,y2);
                float d2=Sign(px2,py,x2,y2,x3,y3);
                float d3=Sign(px2,py,x3,y3,x1,y1);
                if (!((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0))) BlendPixel(px2,py,c);
            }
        }
        float Sign(int px,int py,int x1,int y1,int x2,int y2)
            => (px-x2)*(y1-y2) - (float)(x1-x2)*(py-y2);

        public void Save(string assetPath)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(_px);
            tex.Apply();
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
#endif
