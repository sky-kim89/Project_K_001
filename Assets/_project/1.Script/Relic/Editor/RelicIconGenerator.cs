// ============================================================
//  RelicIconGenerator.cs
//  Tools > Project K > 에셋 > Generate Relic Icons
//
//  유물 24종 아이콘 PNG(64×64)를 생성하고
//  Assets/Resources/Relics/ 의 RelicData SO 에 자동 연결한다.
//
//  저장: Assets/_project/3.Textures/Icons/Relics/
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RelicIconGenerator
{
    const string ICON_PATH  = "Assets/_project/3.Textures/Icons/Relics";
    const string RELIC_DIR  = "Assets/Resources/Relics";
    const int    SIZE       = 64;

    // ── 색상 팔레트 ───────────────────────────────────────────
    static Color32 H(string h)
    {
        byte r = Convert.ToByte(h.Substring(0,2),16);
        byte g = Convert.ToByte(h.Substring(2,2),16);
        byte b = Convert.ToByte(h.Substring(4,2),16);
        return new Color32(r,g,b,255);
    }
    static readonly Color32 Silver = H("C8C8C8"), Gold   = H("D4A840"), DkGold = H("8B6010");
    static readonly Color32 Red    = H("DD3355"), DkRed  = H("991122"), White  = H("FFFFFF");
    static readonly Color32 Blue   = H("4499EE"), LtBlue = H("88BBFF"), DkBlue = H("1A3A6A");
    static readonly Color32 Green  = H("33CC55"), Teal   = H("22CCDD"), Purple = H("9944EE");
    static readonly Color32 Yellow = H("FFCC33"), Orange = H("FF8833"), Brown  = H("7A4020");
    static readonly Color32 LtPurple = H("CC88FF"), DkPurple = H("441166");

    // ── RelicId → 파일명 대응 ─────────────────────────────────
    static readonly (RelicId id, string file, RelicRarity rarity)[] Defs =
    {
        (RelicId.R_AbilityRefresh,    "relic_r101", RelicRarity.Rare),
        (RelicId.R_AbilityChoicePlus, "relic_r102", RelicRarity.Epic),
        (RelicId.R_AbilityAdvanced,   "relic_r103", RelicRarity.Rare),
        (RelicId.R_GoldBonus,         "relic_r104", RelicRarity.Common),
        (RelicId.R_SoulBonus,         "relic_r105", RelicRarity.Common),
        (RelicId.R_ExpBonus,          "relic_r106", RelicRarity.Uncommon),
        (RelicId.R_EnemyHpDown,       "relic_r107", RelicRarity.Rare),
        (RelicId.R_EnemyAtkDown,      "relic_r108", RelicRarity.Rare),
        (RelicId.R_GeneralSlotExpand, "relic_r109", RelicRarity.Epic),
        (RelicId.R_BattleSpeed,       "relic_r110", RelicRarity.Epic),
        (RelicId.R_GeneralHp,         "relic_r201", RelicRarity.Common),
        (RelicId.R_GeneralAtk,        "relic_r202", RelicRarity.Common),
        (RelicId.R_GeneralDef,        "relic_r203", RelicRarity.Common),
        (RelicId.R_GeneralSpd,        "relic_r204", RelicRarity.Common),
        (RelicId.R_GeneralRange,      "relic_r205", RelicRarity.Common),
        (RelicId.R_GeneralCrit,       "relic_r206", RelicRarity.Rare),
        (RelicId.R_GeneralCooldown,   "relic_r207", RelicRarity.Rare),
        (RelicId.R_GeneralSoldierCount,"relic_r208",RelicRarity.Rare),
        (RelicId.R_KnightArmor,       "relic_r211", RelicRarity.Rare),
        (RelicId.R_ArcherSwift,       "relic_r212", RelicRarity.Rare),
        (RelicId.R_MagePower,         "relic_r213", RelicRarity.Epic),
        (RelicId.R_ShieldFortress,    "relic_r214", RelicRarity.Epic),
        (RelicId.R_SoldierAtk,        "relic_r301", RelicRarity.Common),
        (RelicId.R_SoldierHp,         "relic_r302", RelicRarity.Common),

        // 장수 전용 (4xx) — 병사에게 가지 않는 유물. 모두 "장군" 계열 도안이다.
        (RelicId.R_OnlyGeneralAtk,    "relic_r401", RelicRarity.Uncommon),
        (RelicId.R_OnlyGeneralHp,     "relic_r402", RelicRarity.Uncommon),
        (RelicId.R_OnlyGeneralDef,    "relic_r403", RelicRarity.Rare),
        (RelicId.R_OnlyGeneralCrit,   "relic_r404", RelicRarity.Rare),
        (RelicId.R_OnlyGeneralMight,  "relic_r405", RelicRarity.Epic),
    };

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Icon + "유물 아이콘", priority = ProjectKMenu.IconPrio + 14)]
    public static void GenerateAll()
    {
        EnsureDir(ICON_PATH);

        Action<P>[] draws =
        {
            DrawR101, DrawR102, DrawR103,
            DrawR104, DrawR105, DrawR106, DrawR107, DrawR108,
            DrawR109, DrawR110,
            DrawR201, DrawR202, DrawR203, DrawR204, DrawR205,
            DrawR206, DrawR207, DrawR208,
            DrawR211, DrawR212, DrawR213, DrawR214,
            DrawR301, DrawR302,
            DrawR401, DrawR402, DrawR403, DrawR404, DrawR405,
        };

        for (int i = 0; i < Defs.Length; i++)
            Save(SIZE, SIZE, $"{ICON_PATH}/{Defs[i].file}.png", draws[i]);

        AssetDatabase.Refresh();
        ApplySpriteSettings(ICON_PATH, SIZE);
        AssignToSOs();
        AssetDatabase.SaveAssets();
        Debug.Log($"[RelicIconGenerator] 유물 아이콘 {Defs.Length}종 생성·연결 완료");
    }

    // ── SO 자동 연결 ─────────────────────────────────────────

    static void AssignToSOs()
    {
        foreach (var (id, file, _) in Defs)
        {
            string soPath   = $"{RELIC_DIR}/Relic_{(int)id}.asset";
            string iconPath = $"{ICON_PATH}/{file}.png";
            var data   = AssetDatabase.LoadAssetAtPath<RelicData>(soPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (data == null)  { Debug.LogWarning($"[RelicIconGenerator] SO 없음: {soPath}"); continue; }
            if (sprite == null){ Debug.LogWarning($"[RelicIconGenerator] 스프라이트 없음: {iconPath}"); continue; }
            data.Icon = sprite;
            EditorUtility.SetDirty(data);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  공용 배경 / 테두리
    // ═══════════════════════════════════════════════════════════

    static void Bg(P p, RelicRarity r)
    {
        switch (r)
        {
            case RelicRarity.Common:    p.BgGradient(H("0A0A10"), H("1C1C2A")); break;
            case RelicRarity.Uncommon:  p.BgGradient(H("060E06"), H("122212")); break;
            case RelicRarity.Rare:      p.BgGradient(H("06080E"), H("101828")); break;
            case RelicRarity.Epic:      p.BgGradient(H("080510"), H("18103A")); break;
            case RelicRarity.Legendary: p.BgGradient(H("0E0804"), H("2A1A04")); break;
        }
    }

    static void Border(P p, RelicRarity r)
    {
        switch (r)
        {
            case RelicRarity.Common:    p.RoundedBorder(10, 2, H("777788")); break;
            case RelicRarity.Uncommon:  p.RoundedBorder(10, 2, H("33AA44")); break;
            case RelicRarity.Rare:      p.RoundedBorder(10, 2, H("3377CC")); break;
            case RelicRarity.Epic:      p.RoundedBorder(10, 2, H("8833BB")); break;
            case RelicRarity.Legendary: p.RoundedBorder(10, 2, H("BB6611")); break;
        }
    }

    // ── 공용 도형 헬퍼 ────────────────────────────────────────

    static void Heart(P p, int cx, int cy, int sz, Color32 c)
    {
        int br = sz / 2;
        p.FillCircle(cx - br + 1, cy + br / 2, br, c);
        p.FillCircle(cx + br - 1, cy + br / 2, br, c);
        p.FillTri(cx - sz + 2, cy + br / 2, cx + sz - 2, cy + br / 2, cx, cy - sz + 4, c);
    }

    static void Star5(P p, int cx, int cy, int ro, int ri, Color32 c)
    {
        for (int i = 0; i < 5; i++)
        {
            float a1 = (i * 72 - 90) * Mathf.Deg2Rad;
            float a2 = (i * 72 + 36 - 90) * Mathf.Deg2Rad;
            float a3 = ((i + 1) * 72 - 90) * Mathf.Deg2Rad;
            int ox1 = cx + Mathf.RoundToInt(Mathf.Cos(a1) * ro);
            int oy1 = cy + Mathf.RoundToInt(Mathf.Sin(a1) * ro);
            int ix  = cx + Mathf.RoundToInt(Mathf.Cos(a2) * ri);
            int iy  = cy + Mathf.RoundToInt(Mathf.Sin(a2) * ri);
            int ox2 = cx + Mathf.RoundToInt(Mathf.Cos(a3) * ro);
            int oy2 = cy + Mathf.RoundToInt(Mathf.Sin(a3) * ro);
            p.FillTri(cx, cy, ox1, oy1, ix, iy, c);
            p.FillTri(cx, cy, ix, iy, ox2, oy2, c);
        }
    }

    static void Shield(P p, int cx, int cy, int w, int h, Color32 fill, Color32 outline)
    {
        int l = cx - w/2, r2 = cx + w/2, t = cy + h/2, b = cy - h/2, mid = cy;
        p.FillRRect(l, mid, w, h/2, 4, fill);
        p.FillTri(l, mid, r2, mid, cx, b, fill);
        p.DrawLine(l, t, r2, t, outline, 2);
        p.DrawLine(l, t, l, mid, outline, 2);
        p.DrawLine(r2, t, r2, mid, outline, 2);
        p.DrawLine(l, mid, cx, b, outline, 2);
        p.DrawLine(r2, mid, cx, b, outline, 2);
    }

    static void Sword(P p, int cx, int cy, int len, Color32 blade, Color32 guard, Color32 grip)
    {
        // blade
        p.FillRRect(cx - 2, cy, 5, len - 10, 1, blade);
        p.FillTri(cx - 2, cy + len - 10, cx + 3, cy + len - 10, cx, cy + len, blade);
        // highlight
        p.FillRect(cx - 1, cy + 2, 2, len - 14, H("EEEEFF"));
        // crossguard
        p.FillRRect(cx - 10, cy - 2, 21, 4, 2, guard);
        // grip
        p.FillRRect(cx - 2, cy - 16, 5, 14, 1, grip);
        p.FillRRect(cx - 3, cy - 18, 7, 3, 2, guard);
    }

    // ═══════════════════════════════════════════════════════════
    //  어빌리티 유물 (101~103)
    // ═══════════════════════════════════════════════════════════

    // R101 — 어빌리티 재편성 (Rare): 회전 화살표 2개
    static void DrawR101(P p)
    {
        Bg(p, RelicRarity.Rare);
        var c = H("44AAFF");
        // 바깥 반원 호 (위쪽, 오른쪽으로)
        for (int i = 20; i <= 160; i += 3)
        {
            float a = i * Mathf.Deg2Rad;
            int x = 32 + Mathf.RoundToInt(Mathf.Cos(a) * 17);
            int y = 32 + Mathf.RoundToInt(Mathf.Sin(a) * 17);
            p.FillCircle(x, y, 2, c);
        }
        // 화살표 머리 오른쪽
        p.FillTri(49, 38, 44, 29, 54, 31, c);
        // 안쪽 반원 호 (아래쪽, 왼쪽으로)
        var c2 = H("2266AA");
        for (int i = 200; i <= 340; i += 3)
        {
            float a = i * Mathf.Deg2Rad;
            int x = 32 + Mathf.RoundToInt(Mathf.Cos(a) * 11);
            int y = 32 + Mathf.RoundToInt(Mathf.Sin(a) * 11);
            p.FillCircle(x, y, 2, c2);
        }
        p.FillTri(15, 26, 20, 35, 10, 33, c2);
        // 중앙 점
        p.FillCircle(32, 32, 3, c);
        Border(p, RelicRarity.Rare);
    }

    // R102 — 선택의 축복 (Epic): 카드 3장 부채꼴
    static void DrawR102(P p)
    {
        Bg(p, RelicRarity.Epic);
        // 왼쪽 카드
        p.FillRRect(10, 14, 18, 26, 2, H("221133"));
        p.RoundedBorder(2, 1, LtPurple); // border only inside card area — simplified
        p.DrawLine(10, 14, 27, 14, LtPurple, 1);
        p.DrawLine(10, 14, 10, 39, LtPurple, 1);
        p.DrawLine(10, 39, 27, 39, LtPurple, 1);
        p.DrawLine(27, 14, 27, 39, LtPurple, 1);
        // 오른쪽 카드
        p.FillRRect(36, 14, 18, 26, 2, H("221133"));
        p.DrawLine(36, 14, 53, 14, LtPurple, 1);
        p.DrawLine(36, 14, 36, 39, LtPurple, 1);
        p.DrawLine(36, 39, 53, 39, LtPurple, 1);
        p.DrawLine(53, 14, 53, 39, LtPurple, 1);
        // 가운데 카드 (앞에)
        p.FillRRect(22, 10, 20, 28, 2, H("330055"));
        p.DrawLine(22, 10, 41, 10, Purple, 2);
        p.DrawLine(22, 10, 22, 37, Purple, 2);
        p.DrawLine(22, 37, 41, 37, Purple, 2);
        p.DrawLine(41, 10, 41, 37, Purple, 2);
        // 가운데 카드 심볼 (✦)
        Star5(p, 31, 24, 6, 3, Gold);
        // 하단 카드 개수 점
        p.FillCircle(26, 48, 3, Purple);
        p.FillCircle(32, 50, 3, LtPurple);
        p.FillCircle(38, 48, 3, Purple);
        Border(p, RelicRarity.Epic);
    }

    // R103 — 고급 비전 (Rare): 황금별 + 광채
    static void DrawR103(P p)
    {
        Bg(p, RelicRarity.Rare);
        // 광채 광선
        var glow = new Color32(212, 168, 64, 80);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45 * Mathf.Deg2Rad;
            p.DrawLine(32, 32,
                32 + Mathf.RoundToInt(Mathf.Cos(a) * 26),
                32 + Mathf.RoundToInt(Mathf.Sin(a) * 26), glow, 1);
        }
        // 큰 별
        Star5(p, 32, 32, 18, 8, Gold);
        // 별 테두리
        Star5(p, 32, 32, 18, 8, H("FFE090"));
        // 중앙 작은 별
        Star5(p, 32, 32, 7, 3, White);
        Border(p, RelicRarity.Rare);
    }

    // ═══════════════════════════════════════════════════════════
    //  재화 유물 (104~108)
    // ═══════════════════════════════════════════════════════════

    // R104 — 황금의 가호 (Common): 금화 두 개
    static void DrawR104(P p)
    {
        Bg(p, RelicRarity.Common);
        // 뒤 동전
        p.FillCircle(38, 30, 14, DkGold);
        p.DrawCircle(38, 30, 14, 2, Gold);
        // 앞 동전
        p.FillCircle(26, 34, 14, H("B88820"));
        p.DrawCircle(26, 34, 14, 2, Gold);
        // 동전 심볼
        p.FillRect(23, 28, 7, 2, Gold);
        p.FillRect(23, 34, 7, 2, Gold);
        p.FillRect(26, 26, 2, 12, Gold);
        Border(p, RelicRarity.Common);
    }

    // R105 — 전사의 유산 (Common): 빛나는 영혼 구슬
    static void DrawR105(P p)
    {
        Bg(p, RelicRarity.Common);
        // 외곽 글로우
        p.FillCircle(32, 30, 18, new Color32(34, 160, 200, 40));
        p.FillCircle(32, 30, 14, new Color32(34, 200, 220, 70));
        // 구슬 본체
        p.FillCircle(32, 30, 11, Teal);
        p.FillCircle(32, 30, 9,  H("66DDEE"));
        p.FillCircle(31, 32, 5,  H("AAFFFF"));
        // 하이라이트
        p.FillCircle(28, 34, 3, new Color32(255,255,255,200));
        // 영혼 리본 (위)
        p.DrawLine(26, 44, 32, 50, H("44CCDD"), 2);
        p.DrawLine(32, 50, 38, 44, H("44CCDD"), 2);
        Border(p, RelicRarity.Common);
    }

    // R106 — 성장의 증표 (Uncommon): 책 + 별
    static void DrawR106(P p)
    {
        Bg(p, RelicRarity.Uncommon);
        // 책 본체 (두 페이지)
        p.FillRRect(10, 16, 20, 26, 2, H("33AA44"));
        p.FillRRect(34, 16, 20, 26, 2, H("22882F"));
        // 책 등(spine)
        p.FillRect(30, 16, 4, 26, H("185520"));
        p.DrawLine(30, 16, 30, 41, H("66DD88"), 1);
        p.DrawLine(33, 16, 33, 41, H("66DD88"), 1);
        // 페이지 선
        p.DrawLine(14, 24, 26, 24, H("AAFFCC"), 1);
        p.DrawLine(14, 30, 26, 30, H("AAFFCC"), 1);
        p.DrawLine(14, 36, 26, 36, H("AAFFCC"), 1);
        // 별 (우상단)
        Star5(p, 44, 10, 8, 4, Yellow);
        Border(p, RelicRarity.Uncommon);
    }

    // R107 — 시련의 세례 (Rare): 균열 해골
    static void DrawR107(P p)
    {
        Bg(p, RelicRarity.Rare);
        var skc = H("AAAAAA");
        // 해골 둥근 머리
        p.FillCircle(32, 36, 14, skc);
        // 눈 (구멍)
        p.FillCircle(26, 38, 4, H("101820"));
        p.FillCircle(38, 38, 4, H("101820"));
        // 코 구멍
        p.FillCircle(32, 32, 2, H("101820"));
        // 아래 이빨
        p.FillRRect(18, 22, 28, 8, 0, H("101820"));
        for (int i = 0; i < 5; i++)
            p.FillRRect(19 + i*6, 22, 4, 6, 1, skc);
        // 균열 선
        p.DrawLine(32, 50, 30, 42, Red, 2);
        p.DrawLine(30, 42, 34, 36, Red, 2);
        p.DrawLine(34, 36, 32, 28, Red, 1);
        Border(p, RelicRarity.Rare);
    }

    // R108 — 공포의 각인 (Rare): 부러진 검
    static void DrawR108(P p)
    {
        Bg(p, RelicRarity.Rare);
        // 검 손잡이 (하단)
        p.FillRRect(29, 8, 6, 12, 1, Brown);
        p.FillRRect(22, 19, 20, 4, 2, H("886030"));
        // 아래 부분 검날
        p.FillRRect(30, 23, 4, 14, 1, Silver);
        p.FillRect(30, 23, 2, 14, H("EEEEFF"));
        // 균열 부분
        p.FillCircle(32, 37, 4, H("101820"));
        // 위 부분 검날 (기울어짐)
        p.DrawLine(33, 37, 44, 52, Silver, 3);
        p.DrawLine(34, 37, 45, 52, H("EEEEFF"), 1);
        // 균열 효과
        p.DrawLine(28, 37, 36, 37, Red, 2);
        p.DrawLine(30, 34, 35, 40, H("FF6644"), 1);
        Border(p, RelicRarity.Rare);
    }

    // R109 — 출병 명령 (Epic): 장수 슬롯 2개 + 신규 슬롯 (황금 테두리 + +기호)
    static void DrawR109(P p)
    {
        Bg(p, RelicRarity.Epic);
        Color32 slotBg  = H("221133");
        Color32 slotBdr = H("8844CC");
        Color32 newBdr  = H("FFD700");
        Color32 figHead = H("CC99EE");
        Color32 figBody = H("9966CC");

        // 슬롯 1 (배치됨)
        p.FillRRect( 4, 14, 16, 32, 3, slotBg);
        p.DrawLine( 4, 14, 20, 14, slotBdr, 2);
        p.DrawLine( 4, 14,  4, 46, slotBdr, 2);
        p.DrawLine(20, 14, 20, 46, slotBdr, 2);
        p.DrawLine( 4, 46, 20, 46, slotBdr, 2);
        p.FillCircle(12, 38, 4, figHead);
        p.FillRRect( 9, 24, 6, 12, 1, figBody);

        // 슬롯 2 (배치됨)
        p.FillRRect(24, 14, 16, 32, 3, slotBg);
        p.DrawLine(24, 14, 40, 14, slotBdr, 2);
        p.DrawLine(24, 14, 24, 46, slotBdr, 2);
        p.DrawLine(40, 14, 40, 46, slotBdr, 2);
        p.DrawLine(24, 46, 40, 46, slotBdr, 2);
        p.FillCircle(32, 38, 4, figHead);
        p.FillRRect(29, 24, 6, 12, 1, figBody);

        // 슬롯 3 (신규 — 황금 테두리 + 빛 + +기호)
        p.FillRRect(44, 14, 16, 32, 3, H("331144"));
        p.FillCircle(52, 30, 12, new Color32(255, 215, 0, 20));
        p.DrawLine(44, 14, 60, 14, newBdr, 2);
        p.DrawLine(44, 14, 44, 46, newBdr, 2);
        p.DrawLine(60, 14, 60, 46, newBdr, 2);
        p.DrawLine(44, 46, 60, 46, newBdr, 2);
        p.FillRRect(48, 29, 8, 2, 0, newBdr);
        p.FillRRect(51, 26, 2, 8, 0, newBdr);

        // 하단 구분선
        p.DrawLine(4, 10, 60, 10, H("6633AA"), 1);

        Border(p, RelicRarity.Epic);
    }

    // 시간의 고삐 — 모래시계 + 앞으로 튀어나가는 이중 화살표 (배속)
    static void DrawR110(P p)
    {
        Bg(p, RelicRarity.Epic);
        Color32 glass = H("CC99EE");
        Color32 sand  = H("FFCC33");
        Color32 frame = H("8844CC");

        // 모래시계 (좌측) — 위아래 삼각형이 허리에서 만난다
        p.FillTri(10, 48, 26, 48, 18, 33, glass);
        p.FillTri(10, 16, 26, 16, 18, 31, glass);
        p.DrawLine(10, 48, 26, 48, frame, 2);   // 상판
        p.DrawLine(10, 16, 26, 16, frame, 2);   // 하판
        p.FillTri(13, 45, 23, 45, 18, 35, sand);  // 남은 모래
        p.DrawLine(18, 33, 18, 24, sand, 1);      // 떨어지는 줄기
        p.FillTri(15, 19, 21, 19, 18, 25, sand);  // 쌓인 모래

        // 이중 화살표 (우측) — 배속 버튼과 같은 기호
        p.FillTri(32, 42, 32, 22, 44, 32, White);
        p.FillTri(42, 42, 42, 22, 54, 32, sand);

        Border(p, RelicRarity.Epic);
    }

    // ═══════════════════════════════════════════════════════════
    //  장수 스탯 유물 (201~208)
    // ═══════════════════════════════════════════════════════════

    // R201 — 장군의 심장 (Common): 왕관 하트
    static void DrawR201(P p)
    {
        Bg(p, RelicRarity.Common);
        Heart(p, 32, 28, 16, Red);
        // 하이라이트
        p.FillCircle(25, 34, 3, new Color32(255,100,130,160));
        // 작은 왕관 (상단)
        p.FillRect(24, 46, 16, 4, Gold);
        p.FillTri(24, 50, 26, 50, 25, 54, Gold);
        p.FillTri(30, 50, 34, 50, 32, 55, Gold);
        p.FillTri(36, 50, 38, 50, 37, 54, Gold);
        Border(p, RelicRarity.Common);
    }

    // R202 — 전투의 기억 (Common): 검
    static void DrawR202(P p)
    {
        Bg(p, RelicRarity.Common);
        Sword(p, 32, 18, 30, Silver, H("886030"), Brown);
        Border(p, RelicRarity.Common);
    }

    // R203 — 철의 의지 (Common): 방패
    static void DrawR203(P p)
    {
        Bg(p, RelicRarity.Common);
        Shield(p, 32, 30, 28, 28, H("446688"), H("88BBDD"));
        // 방패 십자
        p.FillRRect(30, 20, 4, 18, 1, H("AACCEE"));
        p.FillRRect(22, 28, 20, 4, 1, H("AACCEE"));
        Border(p, RelicRarity.Common);
    }

    // R204 — 질풍의 발걸음 (Common): 번개
    static void DrawR204(P p)
    {
        Bg(p, RelicRarity.Common);
        // 번개 모양
        p.FillTri(38, 10, 26, 10, 28, 30, Yellow);
        p.FillTri(28, 30, 36, 30, 26, 55, Yellow);
        p.FillTri(26, 30, 36, 30, 26, 10, Yellow);
        // 안쪽 밝은 선
        p.DrawLine(34, 12, 30, 29, H("FFFFAA"), 1);
        p.DrawLine(34, 32, 29, 52, H("FFFFAA"), 1);
        // 이동 잔상
        p.DrawLine(14, 22, 22, 20, new Color32(255,200,50,100), 2);
        p.DrawLine(12, 32, 20, 30, new Color32(255,200,50,70),  2);
        p.DrawLine(14, 42, 22, 40, new Color32(255,200,50,50),  1);
        Border(p, RelicRarity.Common);
    }

    // R205 — 사냥꾼의 눈 (Common): 표적 조준경
    static void DrawR205(P p)
    {
        Bg(p, RelicRarity.Common);
        var c = H("66CCAA");
        p.DrawCircle(32, 32, 18, 2, c);
        p.DrawCircle(32, 32, 11, 1, new Color32(102,204,170,140));
        p.DrawCircle(32, 32,  5, 1, c);
        // 조준선
        p.DrawLine(32, 10, 32, 24, c, 2);
        p.DrawLine(32, 40, 32, 54, c, 2);
        p.DrawLine(10, 32, 24, 32, c, 2);
        p.DrawLine(40, 32, 54, 32, c, 2);
        // 중심 점
        p.FillCircle(32, 32, 2, H("FFDDAA"));
        Border(p, RelicRarity.Common);
    }

    // R206 — 비수의 날 (Rare): 단검 + 빛
    static void DrawR206(P p)
    {
        Bg(p, RelicRarity.Rare);
        // 단검 (대각선)
        p.DrawLine(14, 14, 48, 48, Silver, 4);
        p.DrawLine(14, 14, 48, 48, White, 1);
        // 칼끝 삼각형
        p.FillTri(44, 44, 54, 44, 54, 54, Silver);
        p.FillTri(44, 44, 54, 54, 44, 54, H("AAAAAA"));
        // 손잡이
        p.FillRRect(8, 8, 12, 5, 2, Brown);
        p.FillRRect(6, 11, 16, 4, 2, H("886030"));
        // 빛 스파크
        p.FillCircle(38, 20, 2, White);
        p.DrawLine(38, 20, 44, 14, H("FFFFCC"), 1);
        p.DrawLine(38, 20, 32, 14, H("FFFFCC"), 1);
        p.DrawLine(38, 20, 44, 26, H("FFFFCC"), 1);
        Border(p, RelicRarity.Rare);
    }

    // R207 — 시간의 압축 (Rare): 모래시계
    static void DrawR207(P p)
    {
        Bg(p, RelicRarity.Rare);
        var c = H("88AACC");
        // 상단 삼각형
        p.FillTri(14, 52, 50, 52, 32, 34, c);
        // 하단 삼각형
        p.FillTri(14, 12, 50, 12, 32, 30, H("446688"));
        // 중간 모래 (흐르는)
        p.FillCircle(32, 32, 3, H("DDCCAA"));
        p.FillRect(30, 32, 5, 6, H("DDCCAA"));
        // 테두리 프레임
        p.DrawLine(14, 52, 50, 52, H("AACCEE"), 2);
        p.DrawLine(14, 12, 50, 12, H("AACCEE"), 2);
        p.DrawLine(14, 12, 14, 52, H("AACCEE"), 2);
        p.DrawLine(50, 12, 50, 52, H("AACCEE"), 2);
        Border(p, RelicRarity.Rare);
    }

    // R208 — 지휘의 깃발 (Rare): 깃발 + 병사
    static void DrawR208(P p)
    {
        Bg(p, RelicRarity.Rare);
        // 깃대
        p.DrawLine(18, 10, 18, 54, H("886030"), 3);
        // 깃발
        p.FillRRect(18, 38, 28, 18, 2, H("CC3333"));
        p.DrawLine(18, 38, 45, 38, H("FF6644"), 1);
        // 병사 점 3개
        p.FillCircle(26, 22, 4, Silver);
        p.FillCircle(36, 22, 4, Silver);
        p.FillCircle(46, 22, 4, Silver);
        p.FillCircle(26, 14, 3, Silver);
        p.FillCircle(46, 14, 3, Silver);
        Border(p, RelicRarity.Rare);
    }

    // ═══════════════════════════════════════════════════════════
    //  직업 특화 유물 (211~214)
    // ═══════════════════════════════════════════════════════════

    // R211 — 기사왕의 갑주 (Rare): 투구
    static void DrawR211(P p)
    {
        Bg(p, RelicRarity.Rare);
        Color32 mc = H("556677"), rim = H("88AABB");
        // 투구 본체
        p.FillCircle(32, 34, 18, mc);
        p.FillRRect(14, 16, 36, 18, 4, mc);
        // 투구 바이저(visor)
        p.FillRRect(18, 16, 28, 12, 3, H("222E36"));
        // 바이저 슬릿
        p.FillRRect(20, 22, 24, 2, 0, H("111820"));
        p.FillRRect(20, 18, 24, 2, 0, H("111820"));
        // 테두리
        p.DrawLine(14, 16, 50, 16, rim, 2);
        p.DrawLine(14, 16, 14, 28, rim, 2);
        p.DrawLine(50, 16, 50, 28, rim, 2);
        // 깃털 장식
        p.DrawLine(32, 52, 32, 40, H("CC3333"), 3);
        p.FillTri(28, 48, 36, 48, 32, 56, H("CC3333"));
        Border(p, RelicRarity.Rare);
    }

    // R212 — 활의 정령 (Rare): 활 + 화살
    static void DrawR212(P p)
    {
        Bg(p, RelicRarity.Rare);
        Color32 bow = H("7A5010"), str = H("AACC88");
        // 활 (곡선 근사)
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f;
            float x = 16 + t * 6;
            float y = 12 + (1 - (2*t-1)*(2*t-1)) * 20 + t * 28;
            p.FillCircle(Mathf.RoundToInt(x), Mathf.RoundToInt(y), 3, bow);
        }
        // 활시위
        p.DrawLine(16, 12, 22, 52, str, 1);
        // 화살
        p.DrawLine(20, 13, 50, 43, H("D4A840"), 2);
        p.FillTri(48, 41, 52, 45, 44, 47, H("888888")); // 화살촉
        // 깃털
        p.FillTri(20, 13, 14, 8, 24, 8, H("CC6633"));
        p.FillTri(20, 13, 16, 18, 10, 14, H("AA4422"));
        Border(p, RelicRarity.Rare);
    }

    // R213 — 마력의 결정 (Epic): 마법 수정
    static void DrawR213(P p)
    {
        Bg(p, RelicRarity.Epic);
        // 수정 글로우
        p.FillCircle(32, 30, 16, new Color32(120, 50, 200, 50));
        // 수정 본체 (마름모 + 삼각형)
        p.FillTri(32, 54, 16, 30, 48, 30, Purple);             // 아래
        p.FillTri(32, 10, 16, 30, 48, 30, LtPurple);           // 위
        p.FillTri(32, 10, 24, 22, 40, 22, H("DDAAFF"));        // 상단 면
        // 내부 반사
        p.DrawLine(32, 14, 26, 28, new Color32(220,180,255,180), 1);
        p.DrawLine(32, 14, 38, 28, new Color32(200,160,255,140), 1);
        // 마법 파티클
        p.FillCircle(18, 20, 2, H("CC88FF"));
        p.FillCircle(46, 18, 2, H("EE88FF"));
        p.FillCircle(14, 38, 2, H("AA66CC"));
        p.FillCircle(50, 36, 2, H("BB77DD"));
        Border(p, RelicRarity.Epic);
    }

    // R214 — 방벽의 군주 (Epic): 타워 방패
    static void DrawR214(P p)
    {
        Bg(p, RelicRarity.Epic);
        // 방패 본체 (납작한 타워형)
        p.FillRRect(16, 24, 32, 30, 4, H("2A1A4A"));
        p.FillTri(16, 24, 48, 24, 32, 8, H("2A1A4A"));  // 상단 삼각
        p.FillTri(16, 54, 48, 54, 32, 62, H("1A0E30")); // 하단 삼각
        // 방패 테두리
        p.DrawLine(16, 24, 48, 24, LtPurple, 2);
        p.DrawLine(16, 24, 16, 54, LtPurple, 2);
        p.DrawLine(48, 24, 48, 54, LtPurple, 2);
        p.DrawLine(16, 54, 32, 62, LtPurple, 2);
        p.DrawLine(48, 54, 32, 62, LtPurple, 2);
        p.DrawLine(32, 8, 16, 24, LtPurple, 2);
        p.DrawLine(32, 8, 48, 24, LtPurple, 2);
        // 장식 십자
        p.FillRRect(30, 28, 4, 22, 1, Purple);
        p.FillRRect(22, 36, 20, 4, 1, Purple);
        // 보석
        p.FillCircle(32, 32, 3, H("EE88FF"));
        Border(p, RelicRarity.Epic);
    }

    // ═══════════════════════════════════════════════════════════
    //  병사 유물 (301~302)
    // ═══════════════════════════════════════════════════════════

    // ══════════════════════════════════════════════════════════
    //  장수 전용 유물 (4xx)
    //
    //  ⚠ 공통(2xx) 유물과 한눈에 구분돼야 한다
    //    효과 문구를 읽기 전에 "이건 장수만 세지는 것" 이 보여야 고를 때 헷갈리지 않는다.
    //    그래서 다섯 개 모두 **월계관** 을 깔고 그 위에 각자의 상징을 얹는다.
    // ══════════════════════════════════════════════════════════

    /// <summary>장수 전용 표식 — 아래쪽에 깔리는 월계관.</summary>
    static void GeneralWreath(P p, Color32 col)
    {
        // 좌우로 벌어진 잎맥
        for (int i = 0; i < 5; i++)
        {
            int y = 14 + i * 6;
            int w = 7 - i;
            p.FillTri(26, y, 26 - w, y + 3, 26, y + 6, col);
            p.FillTri(38, y, 38 + w, y + 3, 38, y + 6, col);
        }
        p.DrawLine(26, 14, 26, 44, col, 1);
        p.DrawLine(38, 14, 38, 44, col, 1);
    }

    // R401 — 장군의 검 (Uncommon): 월계관 + 검
    static void DrawR401(P p)
    {
        Bg(p, RelicRarity.Uncommon);
        GeneralWreath(p, H("2E6B33"));
        Sword(p, 32, 16, 34, Silver, Gold, Brown);
        Border(p, RelicRarity.Uncommon);
    }

    // R402 — 장군의 투구 (Uncommon): 월계관 + 투구
    static void DrawR402(P p)
    {
        Bg(p, RelicRarity.Uncommon);
        GeneralWreath(p, H("2E6B33"));
        // 투구 — 둥근 머리통 + 얼굴 가리개
        p.FillCircle(32, 34, 13, H("8899AA"));
        p.FillRect(19, 20, 26, 14, H("8899AA"));
        p.FillRect(19, 20, 26, 3, H("55667A"));
        // 얼굴 T자 틈
        p.FillRect(30, 22, 4, 16, H("101018"));
        p.FillRect(23, 30, 18, 4, H("101018"));
        // 볏
        p.FillRRect(30, 44, 5, 10, 2, Red);
        p.FillRRect(31, 46, 2, 7, 1, H("FF7788"));
        Border(p, RelicRarity.Uncommon);
    }

    // R403 — 장군의 흉갑 (Rare): 월계관 + 흉갑
    static void DrawR403(P p)
    {
        Bg(p, RelicRarity.Rare);
        GeneralWreath(p, H("24507A"));
        // 어깨
        p.FillCircle(21, 40, 7, H("6A7A90"));
        p.FillCircle(43, 40, 7, H("6A7A90"));
        // 몸통
        p.FillRRect(22, 20, 21, 22, 4, H("8899AA"));
        p.FillRect(22, 38, 21, 4, H("AABBCC"));
        // 가슴 문양
        p.FillTri(26, 34, 38, 34, 32, 24, LtBlue);
        p.DrawLine(32, 24, 32, 34, White, 1);
        Border(p, RelicRarity.Rare);
    }

    // R404 — 장군의 안목 (Rare): 월계관 + 눈
    static void DrawR404(P p)
    {
        Bg(p, RelicRarity.Rare);
        GeneralWreath(p, H("24507A"));
        // 눈 외곽 (위아래 곡선을 삼각 두 개로)
        p.FillTri(16, 32, 48, 32, 32, 44, H("DDE6F5"));
        p.FillTri(16, 32, 48, 32, 32, 20, H("DDE6F5"));
        // 홍채·동공
        p.FillCircle(32, 32, 8, Blue);
        p.FillCircle(32, 32, 4, H("0A1424"));
        p.FillCircle(30, 34, 2, White);
        // 조준 눈금
        p.DrawLine(12, 32, 17, 32, LtBlue, 1);
        p.DrawLine(47, 32, 52, 32, LtBlue, 1);
        Border(p, RelicRarity.Rare);
    }

    // R405 — 영웅의 증표 (Epic): 월계관 + 훈장
    static void DrawR405(P p)
    {
        Bg(p, RelicRarity.Epic);
        GeneralWreath(p, H("4A2A7A"));
        // 리본
        p.FillTri(26, 46, 32, 36, 32, 52, Red);
        p.FillTri(38, 46, 32, 36, 32, 52, DkRed);
        // 훈장 원판
        p.FillCircle(32, 28, 12, Gold);
        p.DrawCircle(32, 28, 12, 1, DkGold);
        p.FillCircle(32, 28, 8, H("2A1A40"));
        // 별
        p.FillTri(32, 36, 27, 24, 37, 24, Yellow);
        p.FillTri(32, 20, 27, 32, 37, 32, Yellow);
        p.FillCircle(32, 28, 2, White);
        Border(p, RelicRarity.Epic);
    }


    // R301 — 병사의 혼 (Common): 교차 검
    static void DrawR301(P p)
    {
        Bg(p, RelicRarity.Common);
        // 검 1 (좌상→우하)
        p.DrawLine(12, 48, 38, 10, Silver, 4);
        p.DrawLine(12, 48, 38, 10, White, 1);
        p.FillTri(36, 8, 42, 14, 42, 8, Silver);
        p.FillRRect(8, 44, 8, 4, 2, Brown);
        // 검 2 (우상→좌하)
        p.DrawLine(52, 48, 26, 10, H("AAAAAA"), 3);
        p.FillTri(24, 8, 28, 14, 22, 14, H("AAAAAA"));
        p.FillRRect(48, 44, 8, 4, 2, Brown);
        // 교차점 가드
        p.FillCircle(32, 30, 5, Gold);
        Border(p, RelicRarity.Common);
    }

    // R302 — 병사의 생명력 (Common): 작은 병사 + 하트
    static void DrawR302(P p)
    {
        Bg(p, RelicRarity.Common);
        // 병사 실루엣 (심플)
        p.FillCircle(32, 46, 6, Silver);                        // 머리
        p.FillRRect(28, 26, 8, 18, 2, H("778899"));            // 몸
        p.FillRRect(24, 22, 4, 12, 1, H("557788"));            // 왼팔
        p.FillRRect(36, 22, 4, 12, 1, H("557788"));            // 오른팔
        p.FillRRect(28, 12, 4, 14, 1, H("667788"));            // 왼다리
        p.FillRRect(33, 12, 4, 14, 1, H("667788"));            // 오른다리
        // 하트 (우상단)
        Heart(p, 46, 46, 8, Red);
        p.FillCircle(43, 48, 1, new Color32(255,100,130,160));
        Border(p, RelicRarity.Common);
    }

    // ═══════════════════════════════════════════════════════════
    //  저장 / 임포트 / 유틸
    // ═══════════════════════════════════════════════════════════

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

    static void ApplySpriteSettings(string folder, int size)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
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

    // ═══════════════════════════════════════════════════════════
    //  ■ Painter — 픽셀 드로잉 헬퍼 (IconGenerator 와 동일)
    // ═══════════════════════════════════════════════════════════
    class P
    {
        public int W, H;
        readonly Color32[] px;
        public P(int w, int h) { W = w; H = h; px = new Color32[w * h]; }
        int Idx(int x, int y) => (H - 1 - y) * W + x;

        public void BlendPixel(int x, int y, Color32 c)
        {
            if (x<0||x>=W||y<0||y>=H) return;
            int i = Idx(x,y);
            if (c.a==255){px[i]=c;return;}
            float a=c.a/255f,ea=px[i].a/255f,oa=a+ea*(1-a);
            if (oa<0.001f){px[i]=default;return;}
            px[i]=new Color32(
                (byte)Mathf.RoundToInt((c.r*a+px[i].r*ea*(1-a))/oa),
                (byte)Mathf.RoundToInt((c.g*a+px[i].g*ea*(1-a))/oa),
                (byte)Mathf.RoundToInt((c.b*a+px[i].b*ea*(1-a))/oa),
                (byte)Mathf.RoundToInt(oa*255));
        }

        public void BgGradient(Color32 dark, Color32 mid)
        {
            int cx=W/2,cy=H/2; float maxD=Mathf.Sqrt(cx*cx+cy*cy);
            for (int y=0;y<H;y++) for (int x=0;x<W;x++)
            {
                const int R=10; int dx=0,dy=0;
                if(x<R&&y<R){dx=R-x;dy=R-y;}
                else if(x>=W-R&&y<R){dx=x-(W-R-1);dy=R-y;}
                else if(x<R&&y>=H-R){dx=R-x;dy=y-(H-R-1);}
                else if(x>=W-R&&y>=H-R){dx=x-(W-R-1);dy=y-(H-R-1);}
                if(dx*dx+dy*dy>R*R&&(dx>0||dy>0)) continue;
                float t=Mathf.Clamp01(Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy))/maxD);
                BlendPixel(x,y,Color32.Lerp(mid,dark,t*t));
            }
        }

        public void RoundedBorder(int r, int thick, Color32 col)
        {
            for (int y=0;y<H;y++) for (int x=0;x<W;x++)
            {
                int dx=0,dy=0; bool corner=false;
                if(x<r&&y<r){dx=r-x;dy=r-y;corner=true;}
                else if(x>=W-r&&y<r){dx=x-(W-r-1);dy=r-y;corner=true;}
                else if(x<r&&y>=H-r){dx=r-x;dy=y-(H-r-1);corner=true;}
                else if(x>=W-r&&y>=H-r){dx=x-(W-r-1);dy=y-(H-r-1);corner=true;}
                bool onEdge;
                if(corner){float d=Mathf.Sqrt(dx*dx+dy*dy);onEdge=d>=r-thick&&d<=r;}
                else onEdge=x<thick||x>=W-thick||y<thick||y>=H-thick;
                if(onEdge) BlendPixel(x,y,col);
            }
        }

        public void FillRect(int x,int y,int w,int h,Color32 c){for(int py=y;py<y+h;py++)for(int px2=x;px2<x+w;px2++)BlendPixel(px2,py,c);}
        public void FillRRect(int x,int y,int w,int h,int rad,Color32 c){for(int py=y;py<y+h;py++)for(int px2=x;px2<x+w;px2++){int dx=0,dy=0;if(px2<x+rad&&py<y+rad){dx=x+rad-px2;dy=y+rad-py;}else if(px2>=x+w-rad&&py<y+rad){dx=px2-(x+w-rad-1);dy=y+rad-py;}else if(px2<x+rad&&py>=y+h-rad){dx=x+rad-px2;dy=py-(y+h-rad-1);}else if(px2>=x+w-rad&&py>=y+h-rad){dx=px2-(x+w-rad-1);dy=py-(y+h-rad-1);}if(dx*dx+dy*dy<=rad*rad||(dx==0&&dy==0))BlendPixel(px2,py,c);}}
        public void FillCircle(int cx,int cy,int r,Color32 c){for(int y=cy-r;y<=cy+r;y++)for(int x=cx-r;x<=cx+r;x++)if((x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r)BlendPixel(x,y,c);}
        public void DrawCircle(int cx,int cy,int r,int thick,Color32 c){for(int y=cy-r-thick;y<=cy+r+thick;y++)for(int x=cx-r-thick;x<=cx+r+thick;x++){int d2=(x-cx)*(x-cx)+(y-cy)*(y-cy);if(d2>=(r-thick)*(r-thick)&&d2<=(r+thick)*(r+thick))BlendPixel(x,y,c);}}
        public void DrawLine(int x1,int y1,int x2,int y2,Color32 c,int thick=1){int dx=Mathf.Abs(x2-x1),dy=Mathf.Abs(y2-y1),sx=x1<x2?1:-1,sy=y1<y2?1:-1,err=dx-dy,x=x1,y=y1,h2=thick/2;while(true){for(int py=y-h2;py<=y+h2;py++)for(int px2=x-h2;px2<=x+h2;px2++)BlendPixel(px2,py,c);if(x==x2&&y==y2)break;int e2=2*err;if(e2>-dy){err-=dy;x+=sx;}if(e2<dx){err+=dx;y+=sy;}}}
        public void FillTri(int x1,int y1,int x2,int y2,int x3,int y3,Color32 c){int mnX=Mathf.Min(x1,Mathf.Min(x2,x3)),mxX=Mathf.Max(x1,Mathf.Max(x2,x3)),mnY=Mathf.Min(y1,Mathf.Min(y2,y3)),mxY=Mathf.Max(y1,Mathf.Max(y2,y3));for(int py=mnY;py<=mxY;py++)for(int px2=mnX;px2<=mxX;px2++){float d1=Sgn(px2,py,x1,y1,x2,y2),d2=Sgn(px2,py,x2,y2,x3,y3),d3=Sgn(px2,py,x3,y3,x1,y1);if(!((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0)))BlendPixel(px2,py,c);}}
        float Sgn(int px,int py,int x1,int y1,int x2,int y2)=>(px-x2)*(y1-y2)-(float)(x1-x2)*(py-y2);

        public void Save(string assetPath)
        {
            string full=Path.GetFullPath(Path.Combine(Application.dataPath,"..",assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            var tex=new Texture2D(W,H,TextureFormat.RGBA32,false);
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(full,tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
