#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EquipmentIconGenerator.cs  [Editor Only]
//  장비 아이콘 (48×48) 17종 PNG 생성.
//
//  메뉴: BattleGame > 데이터 생성 > 장비 아이콘 생성
//  출력: Assets/_project/3.Textures/Icons/Equipments/
//
//  5가지 형태 × 등급:
//    Armor normal~epic (5), Sword normal~epic (5),
//    Cmd   normal~epic (5), Glove rare (1), Ring unique (1)
//  equip_revenge_unique → armor_unique 공유
//  equip_berserk_epic   → sword_epic   공유
//  equip_immortal_epic  → armor_epic   공유
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

        Save("equip_icon_glove_rare",     p => DrawGlove(p, Theme(UnitGrade.Rare)));
        Save("equip_icon_ring_unique",    p => DrawRing(p, Theme(UnitGrade.Unique)));

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(EQUIP_PATH);
        AssetDatabase.SaveAssets();

        Debug.Log("[EquipmentIconGenerator] 장비 아이콘 17장 생성 완료.");
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
        "equip_cmd_normal"      => $"{EQUIP_PATH}/equip_icon_cmd_normal.png",
        "equip_cmd_uncommon"    => $"{EQUIP_PATH}/equip_icon_cmd_uncommon.png",
        "equip_cmd_rare"        => $"{EQUIP_PATH}/equip_icon_cmd_rare.png",
        "equip_cmd_unique"      => $"{EQUIP_PATH}/equip_icon_cmd_unique.png",
        "equip_cmd_epic"        => $"{EQUIP_PATH}/equip_icon_cmd_epic.png",
        "equip_sniper_rare"     => $"{EQUIP_PATH}/equip_icon_glove_rare.png",
        "equip_vamp_unique"     => $"{EQUIP_PATH}/equip_icon_ring_unique.png",
        "equip_revenge_unique"  => $"{EQUIP_PATH}/equip_icon_armor_unique.png",
        "equip_berserk_epic"    => $"{EQUIP_PATH}/equip_icon_sword_epic.png",
        "equip_immortal_epic"   => $"{EQUIP_PATH}/equip_icon_armor_epic.png",
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
