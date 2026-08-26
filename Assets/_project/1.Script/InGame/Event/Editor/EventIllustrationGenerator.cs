using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EventIllustrationGenerator.cs  [Editor Only]
//  Tools > Project K > 아이콘·텍스처 > 이벤트 일러스트
//
//  프로젝트에 포함된 이벤트 삽화 PNG 의 Sprite 임포트 설정을 적용한다.
//
//  ── 그리기 규칙 ─────────────────────────────────────────
//  · 좌표는 전부 screenY (0 = 상단, 199 = 하단).
//  · 도형은 SDF + 커버리지 기반 안티에일리어싱으로 그린다.
//    (정수 거리 판정으로 찍으면 원·사선이 계단처럼 보인다)
//  · 픽셀은 float 버퍼에 누적하고 마지막에 한 번만 Texture 로 옮긴다.
//    Get/SetPixel 을 도형마다 호출하면 느리고 정밀도도 깎인다.
//
//  ── 화면 구성 규칙 ───────────────────────────────────────
//  832×200 은 아이콘이 아니라 "가로 배너"다. 가운데 도형 하나로는
//  양옆이 텅 빈다. 모든 삽화를 아래 레이어 순서로 장면처럼 쌓는다.
//
//    ① 하늘 그레이디언트 + fbm 구름/안개
//    ② 원경 실루엣  — 안개색에 가깝게 (공기원근)
//    ③ 중경 실루엣  — 화면 폭 전체에 반복 배치
//    ④ 초점 오브젝트 — 3분할 위치 + 글로우
//    ⑤ 근경 실루엣  — 가장 어둡고 선명하게
//    ⑥ 입자 · 비네트 · 하단 페이드 · 그레인
//
//  하단 페이드는 필수 — 삽화 바로 아래에 본문 패널이 붙으므로
//  아래쪽이 어두워야 팝업이 한 덩어리로 보인다.
//
//  출력: Assets/_project/3.Textures/Events/evt_*.png
// ============================================================

public static class EventIllustrationGenerator
{
    const string OutDir = "Assets/_project/3.Textures/Events";
    const int    W      = 832;
    const int    H      = 200;

    // 하단 기준 픽셀 버퍼 (index = (H-1-screenY) * W + x)
    static Color[] _px;

    [MenuItem(ProjectKMenu.Icon + "이벤트 일러스트", priority = ProjectKMenu.IconPrio + 19)]
    public static void Generate()
    {
        AssetDatabase.Refresh();
        ApplySpriteImportSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"[EventIllustrationGenerator] 이벤트 삽화 Sprite 임포트 설정 적용 → {OutDir}");
    }

    // PNG 를 Sprite(Single) 로 임포트한다.
    // 기본 임포트는 spriteMode=Multiple 이라 서브 스프라이트가 생기고,
    // AssetDatabase.LoadAssetAtPath<Sprite>() 가 null 을 반환한다.
    static void ApplySpriteImportSettings()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { OutDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.spritePivot         = new Vector2(0.5f, 0.5f);
            importer.filterMode          = FilterMode.Point;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize      = 1024;   // 832 폭 유지
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  ① 매복 — 붉은 전장, 교차하는 검
    // ══════════════════════════════════════════════════════════

    static void GenAmbush()
    {
        Begin();
        Sky(C(0.05f, 0.02f, 0.05f), C(0.30f, 0.06f, 0.07f));
        Clouds(C(0.42f, 0.08f, 0.08f), 0.28f, 4.2f, 11);

        // 원경 능선
        Ridge(x => 120f + Noise1(x * 0.006f, 3) * 26f, C(0.16f, 0.05f, 0.07f, 0.92f));
        // 중경 — 창 숲 (기울어진 창대가 화면 전체에 늘어섬)
        var rng = new System.Random(7);
        for (int i = 0; i < 26; i++)
        {
            float x  = 10f + i * 32f + rng.Next(-8, 9);
            float ln = 46f + rng.Next(0, 30);
            float tilt = (rng.Next(0, 2) == 0 ? -1f : 1f) * (5f + rng.Next(0, 8));
            Capsule(x, 152f, x + tilt, 152f - ln, 2.6f, C(0.09f, 0.03f, 0.04f, 0.95f));
            Tri(x + tilt, 152f - ln - 8f, x + tilt - 3.4f, 152f - ln, x + tilt + 3.4f, 152f - ln,
                C(0.09f, 0.03f, 0.04f, 0.95f));
        }
        Ridge(x => 150f + Noise1(x * 0.010f, 5) * 10f, C(0.06f, 0.015f, 0.02f, 1f));

        // 초점 — 교차한 검 (좌측 3분할)
        float cx = W * 0.42f, cy = 96f;
        Glow(cx, cy, 118f, C(1f, 0.30f, 0.16f), 0.55f);
        Sword(cx, cy,  38f, C(0.44f, 0.47f, 0.56f), C(0.28f, 0.15f, 0.08f));
        Sword(cx, cy, -38f, C(0.35f, 0.38f, 0.47f), C(0.22f, 0.11f, 0.06f));
        Glow(cx, cy, 26f, C(1f, 0.84f, 0.56f), 0.85f);
        Disc(cx, cy, 3.6f, C(1f, 0.96f, 0.88f, 0.90f));

        Embers(150, C(1f, 0.52f, 0.20f), 22);
        Vignette(0.62f);
        BottomFade(C(0.05f, 0.01f, 0.02f), 132f, 1.5f);
        Grain(0.020f, 31);
        Save("evt_ambush");
    }

    // ══════════════════════════════════════════════════════════
    //  ② 상인 — 천막 시장, 금화 더미
    // ══════════════════════════════════════════════════════════

    static void GenMerchant()
    {
        Begin();
        Sky(C(0.09f, 0.05f, 0.02f), C(0.30f, 0.17f, 0.05f));
        Clouds(C(0.48f, 0.30f, 0.08f), 0.22f, 3.4f, 23);

        // 원경 — 천막 지붕이 화면 폭을 가로지름
        for (int i = 0; i < 7; i++)
        {
            float bx = 40f + i * 128f;
            Tri(bx, 74f, bx - 68f, 122f, bx + 68f, 122f, C(0.27f, 0.16f, 0.05f, 0.94f));
            Capsule(bx - 62f, 122f, bx - 62f, 160f, 2.4f, C(0.14f, 0.08f, 0.03f, 0.85f));
            Capsule(bx + 62f, 122f, bx + 62f, 160f, 2.4f, C(0.14f, 0.08f, 0.03f, 0.85f));
        }

        // 매달린 랜턴 (좌측) — 따뜻한 광원
        float lx = W * 0.17f, ly = 62f;
        Capsule(lx, 0f, lx, ly - 20f, 1.6f, C(0.16f, 0.09f, 0.03f, 0.9f));
        Glow(lx, ly, 100f, C(1f, 0.66f, 0.22f), 0.68f);
        Tri(lx, ly - 22f, lx - 10f, ly - 12f, lx + 10f, ly - 12f, C(0.30f, 0.18f, 0.05f, 0.95f));
        RoundRect(lx, ly - 11f, 11f, 2.4f, 1f, C(0.36f, 0.22f, 0.07f, 0.95f));  // 갓 테
        RoundRect(lx, ly + 1f,  7.4f, 11f, 2.5f, C(1f, 0.82f, 0.36f, 0.94f));   // 유리·불빛
        Capsule(lx - 7.4f, ly - 9f, lx - 7.4f, ly + 11f, 1.5f, C(0.30f, 0.18f, 0.05f, 0.95f));
        Capsule(lx + 7.4f, ly - 9f, lx + 7.4f, ly + 11f, 1.5f, C(0.30f, 0.18f, 0.05f, 0.95f));
        RoundRect(lx, ly + 13f, 9f, 2.2f, 1f, C(0.32f, 0.19f, 0.06f, 0.95f));   // 바닥

        // 좌판
        Ridge(x => 150f, C(0.17f, 0.10f, 0.04f, 1f));
        RoundRect(W * 0.55f, 152f, 190f, 7f, 2f, C(0.26f, 0.15f, 0.05f, 1f));

        // 초점 — 금화 더미 (우측 3분할)
        float mx = W * 0.58f;
        Glow(mx, 128f, 128f, C(1f, 0.76f, 0.20f), 0.50f);
        CoinStack(mx - 46f, 146f, 4);
        CoinStack(mx + 44f, 146f, 3);
        CoinStack(mx,       146f, 6);
        // 튀어오른 동전
        Coin(mx + 74f, 84f, 13f, 0.55f);
        Coin(mx - 70f, 70f, 10f, 0.30f);

        Motes(180, C(1f, 0.84f, 0.34f), 29, 0.55f);
        Vignette(0.56f);
        BottomFade(C(0.07f, 0.04f, 0.01f), 138f, 1.4f);
        Grain(0.018f, 47);
        Save("evt_merchant");
    }

    // ══════════════════════════════════════════════════════════
    //  ③ 신전 — 기둥 회랑, 빛기둥, 제단
    // ══════════════════════════════════════════════════════════

    static void GenShrine()
    {
        Begin();
        Sky(C(0.03f, 0.04f, 0.13f), C(0.11f, 0.10f, 0.26f));
        Clouds(C(0.30f, 0.34f, 0.62f), 0.20f, 3.0f, 5);

        float cx = W * 0.5f;

        // 천장에서 내려오는 빛기둥
        LightShaft(cx, 0f, 128f, 210f, C(0.62f, 0.74f, 1f), 0.30f);
        LightShaft(cx - 210f, 0f, 54f, 120f, C(0.55f, 0.66f, 1f), 0.13f);
        LightShaft(cx + 210f, 0f, 54f, 120f, C(0.55f, 0.66f, 1f), 0.13f);

        // 기둥 회랑 — 중앙에서 멀수록 크고 어둡게 (원근)
        for (int s = -1; s <= 1; s += 2)
        for (int i = 1; i <= 3; i++)
        {
            float px = cx + s * (78f + i * 92f);
            float top = 30f - i * 6f;
            float w   = 12f + i * 3.5f;
            float dark = 0.30f - i * 0.05f;
            Color col = C(dark, dark * 0.94f, dark * 1.34f, 0.95f);
            RoundRect(px, (top + 172f) * 0.5f, w, (172f - top) * 0.5f, 1.5f, col);
            RoundRect(px, top + 5f,  w + 6f, 6f, 1.5f, col * 1.22f);   // 주두
            RoundRect(px, 170f,      w + 8f, 7f, 1.5f, col * 1.10f);   // 주초
            // 좌측 림라이트
            Capsule(px - w + 1.5f, top + 10f, px - w + 1.5f, 166f, 1.6f,
                    C(0.55f, 0.64f, 0.98f, 0.28f));
        }

        // 바닥
        Ridge(x => 174f, C(0.07f, 0.07f, 0.17f, 1f));
        Capsule(0f, 174f, W, 174f, 1.6f, C(0.42f, 0.50f, 0.86f, 0.30f));

        // 초점 — 제단 + 부유하는 성구
        RoundRect(cx, 166f, 46f, 12f, 3f, C(0.22f, 0.21f, 0.36f, 1f));
        RoundRect(cx, 154f, 54f, 5f,  2f, C(0.30f, 0.29f, 0.46f, 1f));
        Glow(cx, 104f, 74f, C(0.72f, 0.84f, 1f), 1.05f);
        Disc(cx, 104f, 13f, C(0.86f, 0.92f, 1f, 0.94f));
        Disc(cx, 104f,  6f, C(1f, 1f, 1f, 1f));
        RingAA(cx, 104f, 26f, 1.6f, C(0.70f, 0.82f, 1f, 0.42f));

        Motes(70, C(0.80f, 0.88f, 1f), 13, 0.32f);   // 실내 먼지 — 과하면 눈처럼 보인다
        Vignette(0.58f);
        BottomFade(C(0.03f, 0.03f, 0.10f), 150f, 1.5f);
        Grain(0.016f, 59);
        Save("evt_shrine");
    }

    // ══════════════════════════════════════════════════════════
    //  ④ 병사 — 새벽 진영, 방패와 창렬
    // ══════════════════════════════════════════════════════════

    static void GenSoldier()
    {
        Begin();
        Sky(C(0.04f, 0.06f, 0.12f), C(0.24f, 0.26f, 0.34f));
        Clouds(C(0.42f, 0.46f, 0.58f), 0.24f, 3.6f, 71);

        // 원경 산릉
        Ridge(x => 104f + Noise1(x * 0.005f, 2) * 34f, C(0.11f, 0.13f, 0.21f, 0.90f));
        Ridge(x => 132f + Noise1(x * 0.009f, 8) * 18f, C(0.07f, 0.09f, 0.16f, 0.95f));

        // 중경 — 창과 군기 행렬
        var rng = new System.Random(19);
        for (int i = 0; i < 22; i++)
        {
            float x = 16f + i * 38f + rng.Next(-6, 7);
            float ln = 52f + rng.Next(0, 22);
            Capsule(x, 158f, x, 158f - ln, 2.2f, C(0.06f, 0.08f, 0.13f, 0.95f));
            if (i % 4 == 1)   // 네 개마다 깃발
                Tri(x, 158f - ln + 4f, x + 22f, 158f - ln + 13f, x, 158f - ln + 24f,
                    C(0.30f, 0.13f, 0.14f, 0.88f));
            else
                Tri(x, 158f - ln - 7f, x - 2.8f, 158f - ln, x + 2.8f, 158f - ln,
                    C(0.06f, 0.08f, 0.13f, 0.95f));
        }

        Ridge(x => 162f, C(0.04f, 0.05f, 0.09f, 1f));

        // 좌하단 모닥불 — 장면에 온도차를 준다.
        // ⚠ 반드시 지면(Ridge) 뒤에 그린다 — 예전엔 앞에 있어서 불이 통째로
        //   흙에 덮였다. 유일한 난색 광원이 사라진 채 파란 장면만 남았다.
        float fx = W * 0.16f, fy = 168f;
        Glow(fx, fy, 104f, C(1f, 0.54f, 0.16f), 0.75f);
        Capsule(fx - 15f, fy + 2f, fx + 14f, fy - 2f, 2.6f, C(0.22f, 0.15f, 0.10f));   // 장작
        Capsule(fx - 13f, fy - 2f, fx + 16f, fy + 2f, 2.6f, C(0.18f, 0.12f, 0.08f));
        Tri(fx, fy - 26f, fx - 10f, fy, fx + 10f, fy, C(1f, 0.62f, 0.22f, 0.92f));      // 불꽃
        Tri(fx, fy - 16f, fx - 5.5f, fy, fx + 5.5f, fy, C(1f, 0.90f, 0.58f, 0.95f));
        Disc(fx, fy, 6f, C(1f, 0.84f, 0.44f, 0.85f));
        // 불빛이 닿는 땅
        Ellipse(fx, fy + 8f, 62f, 11f, C(0.60f, 0.30f, 0.10f, 0.28f));

        // 초점 — 방패 (우측 3분할). 땅에 세워 둔 것이므로 접지 그림자를 깐다.
        float sx = W * 0.62f, sy = 104f;
        GroundShadow(sx, 166f, 62f, 12f, 0.55f);
        Glow(sx, sy, 104f, C(0.48f, 0.62f, 1f), 0.34f);
        Shield(sx, sy, 46f, 58f);
        // 방패 오른쪽으로 모닥불의 난색 반사 — 두 광원이 만나야 입체가 산다
        Capsule(sx + 42f, sy - 32f, sx + 42f, sy + 22f, 2.4f, C(1f, 0.62f, 0.28f, 0.22f));

        Motes(90, C(1f, 0.72f, 0.40f), 37, 0.45f);
        Vignette(0.60f);
        BottomFade(C(0.03f, 0.04f, 0.08f), 140f, 1.5f);
        Grain(0.018f, 83);
        Save("evt_soldier");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑨ 전술서 — 펼쳐진 고서와 떠오르는 전조
    //
    //  ⚠ evt_mystery(물음표 마법진)와 나눠 쓴다
    //    "어빌리티 발견" 은 무엇을 얻는지가 분명한 사건이다.
    //    물음표를 띄우면 "정체불명" 으로 읽혀 발견의 만족감이 죽는다.
    // ══════════════════════════════════════════════════════════

    static void GenTome()
    {
        Begin();
        Sky(C(0.05f, 0.04f, 0.11f), C(0.13f, 0.09f, 0.20f));
        Clouds(C(0.42f, 0.32f, 0.78f), 0.24f, 3.2f, 211);

        float cx = W * 0.5f, cy = 104f;

        // ── 방을 먼저 만든다 ────────────────────────────────
        // ⚠ 책만 띄우면 832 폭이 통째로 빈다
        //   좌우에 책장을 세우고 아래에 독서대를 놓아야 "서재" 가 된다.
        //   책이 무언가에 얹혀 있어야 크기도 읽힌다.
        const float DeskTop = 112f;   // 독서대 상판 — 책이 여기 얹힌다
        for (int s = -1; s <= 1; s += 2)
        for (int i = 0; i < 3; i++)
        {
            float bx = cx + s * (152f + i * 116f);
            RoundRect(bx, 84f, 50f, 78f, 2f, C(0.068f, 0.056f, 0.112f));
            for (int sh = 0; sh < 3; sh++)
            {
                float shelfY = 32f + sh * 46f;
                Capsule(bx - 50f, shelfY, bx + 50f, shelfY, 3f, C(0.102f, 0.086f, 0.162f));

                var brng = new System.Random((int)bx * 31 + sh * 7 + s);
                for (int b = 0; b < 7; b++)   // 꽂힌 책 — 등만 보인다
                {
                    float bw = 4f + brng.Next(0, 4);
                    float bh = 18f + brng.Next(0, 12);
                    float v  = 0.5f + (float)brng.NextDouble() * 0.9f;
                    RoundRect(bx - 44f + b * 13f, shelfY - bh * 0.5f - 2f, bw * 0.5f, bh * 0.5f, 1f,
                              C(0.16f * v + 0.05f, 0.11f * v + 0.04f, 0.30f * v + 0.06f));
                }
            }
        }
        MistBand(112f, 48f, C(0.24f, 0.18f, 0.42f), 0.28f);   // 공기원근 — 책장을 뒤로 민다

        // 독서대 — 상판을 사다리꼴로 눕혀 원근을 준다
        Ridge(x => DeskTop + 34f, C(0.042f, 0.034f, 0.076f, 1f));
        Tri(cx - 132f, DeskTop + 34f, cx + 132f, DeskTop + 34f, cx + 104f, DeskTop, C(0.112f, 0.092f, 0.174f));
        Tri(cx - 132f, DeskTop + 34f, cx - 104f, DeskTop,       cx + 104f, DeskTop, C(0.112f, 0.092f, 0.174f));
        Capsule(cx - 104f, DeskTop, cx + 104f, DeskTop, 2.4f, C(0.32f, 0.28f, 0.50f, 0.55f));
        GroundShadow(cx, DeskTop + 4f, 106f, 14f, 0.60f);

        // 책 뒤 후광 — 시선을 가운데로 모은다
        Glow(cx, cy + 6f, 128f, C(0.72f, 0.56f, 1f), 0.46f);

        // 펼친 책 — 좌우 페이지를 살짝 기울여 입체를 만든다
        var paper  = C(0.88f, 0.86f, 0.78f);
        var shade  = C(0.62f, 0.60f, 0.55f);
        Tri(cx - 78f, cy - 8f, cx - 4f, cy + 4f, cx - 4f, cy - 26f, paper);
        Tri(cx - 78f, cy - 8f, cx - 78f, cy - 26f, cx - 4f, cy - 26f, paper);
        Tri(cx + 78f, cy - 8f, cx + 4f, cy + 4f, cx + 4f, cy - 26f, paper);
        Tri(cx + 78f, cy - 8f, cx + 78f, cy - 26f, cx + 4f, cy - 26f, paper);

        // 책등 그림자 = 접힌 부분
        Capsule(cx, cy + 4f, cx, cy - 26f, 3.2f, shade);

        // 글줄 — 왼쪽은 빛바래고 오른쪽은 아직 읽히는 느낌
        for (int i = 0; i < 5; i++)
        {
            float y = cy - 4f - i * 4.2f;
            Capsule(cx - 66f, y, cx - 14f, y, 1.1f, C(0.45f, 0.42f, 0.40f, 0.65f));
            Capsule(cx + 14f, y, cx + 66f, y, 1.1f, C(0.45f, 0.42f, 0.40f, 0.45f));
        }

        // 페이지에서 떠오르는 전조 — 마름모 룬 셋.
        // ⚠ 책 위로 올린다 (screenY 가 작을수록 위)
        //   예전엔 cy + 34 라 책 아래로 가라앉아 "떠오른다" 가 안 읽혔다.
        for (int i = 0; i < 3; i++)
        {
            float rx = cx + (i - 1) * 34f;
            float ry = cy - 46f + Mathf.Abs(i - 1) * 11f;
            float s  = 7f;
            Tri(rx, ry + s, rx - s, ry, rx + s, ry, C(0.86f, 0.72f, 1f, 0.92f));
            Tri(rx, ry - s, rx - s, ry, rx + s, ry, C(0.68f, 0.50f, 1f, 0.92f));
            Glow(rx, ry, 26f, C(0.78f, 0.60f, 1f), 0.42f);
        }

        Motes(160, C(0.88f, 0.78f, 1f), 233, 0.55f);
        Vignette(0.62f);
        BottomFade(C(0.03f, 0.02f, 0.08f), 150f, 1.6f);
        Grain(0.020f, 191);
        Save("evt_tome");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑩ 창고 — 반쯤 열린 문으로 빛이 새어 드는 폐창고 내부
    //
    //  ⚠ 예전 판은 검은 화면에 갈색 네모 네 개였다
    //    ① 유일한 광원인 문빛이 LightShaft(topY=200) 로 들어가 한 픽셀도
    //       안 그려졌고, ② 벽은 1.4px 선 아홉 줄이라 배경에 묻혔으며,
    //       ③ 바닥이 없어 상자가 허공에 떠 있었고, ④ 전부 화면 정중앙이라
    //       832 폭의 좌우가 텅 비었다. 넷 다 여기서 고친다.
    //
    //  구성: 좌측 3분할에 열린 문(광원) — 우측 3분할에 상자 더미(초점).
    //        빛이 문에서 상자 쪽으로 비스듬히 깔려 둘을 잇는다.
    // ══════════════════════════════════════════════════════════

    static void GenWarehouse()
    {
        Begin();

        // 실내다 — 하늘이 아니라 어둠이 배경이다
        Sky(C(0.030f, 0.028f, 0.034f), C(0.080f, 0.066f, 0.056f));

        const float FloorY = 146f;
        float doorX  = W * 0.19f;    // 반쯤 열린 문
        float focusX = W * 0.63f;    // 상자 더미

        // ── ① 뒷벽 · 천장 ────────────────────────────────────
        PlankWall(0f, W, 6f, FloorY, C(0.150f, 0.120f, 0.094f), 907, 34f);

        // 널 두 장이 떨어져 나가 그 너머의 어둠이 보인다 (폐허의 신호)
        RoundRect(W * 0.86f, 74f, 13f, 44f, 1f, C(0.012f, 0.012f, 0.016f));
        RoundRect(W * 0.40f, 52f, 12f, 26f, 1f, C(0.012f, 0.012f, 0.016f));

        // 천장 들보 + 매달린 기둥
        Capsule(0f, 22f, W, 28f, 9f, C(0.098f, 0.078f, 0.060f));
        Capsule(0f, 18f, W, 24f, 1.6f, C(0.26f, 0.21f, 0.15f, 0.45f));
        for (int i = 0; i < 6; i++)
        {
            float bx = 76f + i * 140f;
            Capsule(bx, 27f, bx, 50f, 5f, C(0.088f, 0.070f, 0.054f));
        }

        // ── ② 바닥 ──────────────────────────────────────────
        PlankFloor(FloorY, W * 0.42f, C(0.115f, 0.092f, 0.070f), 811);

        // ── ③ 문 — 반쯤 열려 바깥의 낮이 새어 든다 ───────────
        float dTop = 40f, dBot = FloorY;
        float dMid = (dTop + dBot) * 0.5f, dHalf = (dBot - dTop) * 0.5f;

        RoundRect(doorX, dMid, 54f, dHalf, 2f, C(0.010f, 0.010f, 0.014f));   // 문틀 안쪽 어둠
        RoundRect(doorX + 16f, dMid, 30f, dHalf - 3f, 1f, C(0.99f, 0.93f, 0.76f, 0.95f)); // 바깥 빛면
        Glow(doorX + 16f, dMid, 128f, C(1f, 0.88f, 0.60f), 0.60f);

        // 젖혀진 문짝 — 사다리꼴로 그려야 열려 있는 각이 읽힌다
        Tri(doorX - 54f, dTop,  doorX - 104f, dTop + 16f, doorX - 104f, dBot - 4f,
            C(0.115f, 0.090f, 0.068f));
        Tri(doorX - 54f, dTop,  doorX - 54f,  dBot,       doorX - 104f, dBot - 4f,
            C(0.115f, 0.090f, 0.068f));
        Capsule(doorX - 58f, dTop + 8f,  doorX - 100f, dTop + 22f, 2.4f, C(0.055f, 0.044f, 0.034f));
        Capsule(doorX - 58f, dBot - 22f, doorX - 100f, dBot - 12f, 2.4f, C(0.055f, 0.044f, 0.034f));
        Capsule(doorX - 56f, dTop + 2f,  doorX - 56f,  dBot,       2.0f, C(0.34f, 0.28f, 0.20f, 0.75f));
        Disc(doorX - 66f, dMid + 6f, 3.2f, C(0.40f, 0.34f, 0.24f));           // 문고리
        // 문틀 기둥
        Capsule(doorX + 56f, dTop - 6f, doorX + 56f, dBot, 7f, C(0.135f, 0.108f, 0.082f));
        Capsule(doorX - 56f, dTop - 6f, doorX + 56f, dTop - 6f, 8f, C(0.135f, 0.108f, 0.082f));

        // ── ④ 문에서 쏟아지는 빛 — 장면을 잇는 대각선 ────────
        LightBeam(doorX + 16f, dMid - 40f, focusX + 40f, 196f,
                  34f, 96f, C(1f, 0.88f, 0.62f), 0.34f);
        LightBeam(doorX + 16f, dMid + 20f, focusX - 30f, 200f,
                  22f, 70f, C(1f, 0.90f, 0.68f), 0.22f);
        // 빛이 바닥에 닿아 생기는 웅덩이
        Ellipse(W * 0.40f, 176f, 168f, 26f, C(0.62f, 0.50f, 0.30f, 0.20f));

        // ── ⑤ 상자 더미 — 우측 3분할, 아래부터 쌓는다 ────────
        GroundShadow(focusX + 6f, FloorY + 12f, 118f, 22f, 0.72f);

        Crate(focusX - 46f, FloorY + 6f, 34f, 46f, C(0.310f, 0.216f, 0.132f), 3);   // 아래 왼쪽
        Crate(focusX + 34f, FloorY + 8f, 30f, 40f, C(0.276f, 0.192f, 0.118f), 7);   // 아래 오른쪽
        Crate(focusX - 20f, FloorY - 40f, 26f, 34f, C(0.352f, 0.248f, 0.150f), 11); // 위
        Crate(focusX + 96f, FloorY + 4f, 22f, 28f, C(0.248f, 0.172f, 0.106f), 17);  // 곁다리

        // 뚜껑이 열린 상자 — "쓸만한 게 있다" 는 신호는 여기 하나로 몰아 준다
        float oX = focusX - 20f, oTop = FloorY - 74f;
        Tri(oX - 26f, oTop, oX + 4f, oTop - 16f, oX + 30f, oTop - 4f, C(0.190f, 0.132f, 0.080f));
        Tri(oX - 26f, oTop, oX - 2f, oTop + 8f,  oX + 30f, oTop - 4f, C(0.190f, 0.132f, 0.080f));
        Glow(oX, oTop + 2f, 74f, C(1f, 0.82f, 0.42f), 0.85f);
        Coin(oX - 11f, oTop + 4f, 4.6f, 0.30f);
        Coin(oX + 5f,  oTop + 2f, 4.6f, -0.15f);
        Coin(oX - 3f,  oTop + 9f, 4.6f, 0.10f);

        // ── ⑥ 곁 소품 — 좌우가 비지 않게 채운다 ──────────────
        GroundShadow(W * 0.36f, FloorY + 16f, 46f, 12f, 0.55f);
        Barrel(W * 0.36f, FloorY + 14f, 24f, 52f, C(0.240f, 0.170f, 0.108f));

        GroundShadow(W * 0.455f, FloorY + 20f, 40f, 11f, 0.50f);
        Sack(W * 0.452f, FloorY + 20f, 17f, C(0.300f, 0.262f, 0.180f));
        Sack(W * 0.492f, FloorY + 24f, 13f, C(0.266f, 0.232f, 0.158f));

        GroundShadow(W * 0.905f, FloorY + 14f, 44f, 12f, 0.55f);
        Barrel(W * 0.905f, FloorY + 12f, 20f, 42f, C(0.210f, 0.150f, 0.096f));
        // 옆으로 넘어간 통 — 전부 반듯이 세워 두면 '정리된 창고' 가 된다
        Ellipse(W * 0.955f, FloorY + 26f, 26f, 15f, C(0.196f, 0.140f, 0.090f));
        RingAA(W * 0.955f, FloorY + 26f, 15f, 1.4f, C(0.10f, 0.09f, 0.08f, 0.8f));

        // 벽에 기대 둔 판자 · 갈고리
        Capsule(W * 0.775f, FloorY + 12f, W * 0.745f, 62f, 5f, C(0.155f, 0.122f, 0.090f));
        Capsule(W * 0.795f, FloorY + 12f, W * 0.772f, 76f, 4f, C(0.132f, 0.104f, 0.076f));

        // ── ⑦ 방치의 흔적 ───────────────────────────────────
        Cobweb(6f, 30f, 62f, 0.10f, 1.42f, C(0.62f, 0.60f, 0.56f, 0.16f));
        Cobweb(W - 6f, 26f, 54f, 1.72f, 3.04f, C(0.62f, 0.60f, 0.56f, 0.14f));
        // 들보에 걸린 밧줄
        Capsule(W * 0.70f, 28f, W * 0.706f, 66f, 1.6f, C(0.28f, 0.24f, 0.17f, 0.85f));
        Capsule(W * 0.706f, 66f, W * 0.694f, 88f, 1.6f, C(0.24f, 0.20f, 0.14f, 0.80f));

        // ── ⑧ 공기 ─────────────────────────────────────────
        BeamDust(doorX + 16f, dMid - 40f, focusX + 40f, 196f, 34f, 96f,
                 C(1f, 0.92f, 0.74f), 190, 307);
        Motes(70, C(0.80f, 0.74f, 0.62f), 313, 0.26f);

        Vignette(0.66f);
        BottomFade(C(0.020f, 0.016f, 0.014f), 150f, 1.7f);
        Grain(0.024f, 271);
        Save("evt_warehouse");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑪ 유물 — 흙에 반쯤 묻힌 갑옷과 전투석
    // ══════════════════════════════════════════════════════════

    static void GenRelic()
    {
        Begin();
        Sky(C(0.06f, 0.06f, 0.09f), C(0.16f, 0.13f, 0.12f));
        MistBand(96f, 34f, C(0.30f, 0.28f, 0.30f), 0.30f);

        // 흙더미 능선 — 전장터의 파헤쳐진 땅
        Ridge(x => 62f + Mathf.Sin(x * 0.021f) * 9f + Mathf.Sin(x * 0.055f) * 4f,
              C(0.13f, 0.11f, 0.09f));

        float cx = W * 0.5f;

        // 좌우로 이어지는 무기 무덤 — 832 폭을 쓰지 않으면 가운데만 그림이 된다.
        // 뒤로 갈수록 흙색에 가깝게 죽여 공기원근을 만든다.
        var frng = new System.Random(577);
        for (int i = 0; i < 16; i++)
        {
            float x = 24f + i * 52f + frng.Next(-14, 15);
            if (Mathf.Abs(x - cx) < 96f) continue;          // 초점 주변은 비워 둔다
            float far = Mathf.Clamp01(Mathf.Abs(x - cx) / (W * 0.5f));
            float k   = 0.9f - far * 0.42f;
            float ln  = 30f + frng.Next(0, 26);
            float tilt = (frng.Next(0, 2) == 0 ? -1f : 1f) * (6f + frng.Next(0, 14));
            Capsule(x, 66f, x + tilt, 66f - ln, 2.2f,
                    C(0.20f * k, 0.19f * k, 0.20f * k, 0.95f));
            Tri(x + tilt, 66f - ln - 7f, x + tilt - 3f, 66f - ln, x + tilt + 3f, 66f - ln,
                C(0.20f * k, 0.19f * k, 0.20f * k, 0.95f));
            if (i % 3 == 0)   // 흙에 처박힌 방패
                ShieldSilhouette(x + 16f, 60f, 13f, 15f, C(0.17f * k, 0.16f * k, 0.18f * k));
        }

        // 부러진 창·검이 땅에 꽂혀 있다 — 배경 밀도
        Sword(cx - 118f, 66f, 14f,  C(0.34f, 0.34f, 0.38f), C(0.20f, 0.14f, 0.10f));
        Sword(cx + 126f, 62f, -22f, C(0.30f, 0.30f, 0.34f), C(0.18f, 0.13f, 0.09f));

        // 반쯤 묻힌 갑옷 — 실루엣만 보이게 흙 위로 살짝 올린다
        GroundShadow(cx - 8f, 104f, 66f, 14f, 0.55f);
        ShieldSilhouette(cx - 8f, 84f, 40f, 46f, C(0.26f, 0.25f, 0.27f));
        RingAA(cx - 8f, 92f, 16f, 1.6f, C(0.42f, 0.40f, 0.42f, 0.6f));
        // 녹슨 자국
        Capsule(cx - 26f, 96f, cx - 12f, 74f, 2.2f, C(0.42f, 0.24f, 0.12f, 0.55f));
        Capsule(cx + 8f,  100f, cx + 18f, 78f, 1.8f, C(0.40f, 0.22f, 0.11f, 0.45f));

        // 갑옷에 박힌 전투석 — 이 그림의 주인공
        Glow(cx + 26f, 96f, 54f, C(0.40f, 0.72f, 1f), 0.60f);
        Tri(cx + 26f, 108f, cx + 16f, 94f, cx + 36f, 94f, C(0.66f, 0.88f, 1f));
        Tri(cx + 26f, 82f,  cx + 16f, 94f, cx + 36f, 94f, C(0.34f, 0.60f, 0.96f));

        Embers(70, C(0.52f, 0.76f, 1f), 419);
        Vignette(0.68f);
        BottomFade(C(0.03f, 0.02f, 0.02f), 146f, 1.6f);
        Grain(0.024f, 383);
        Save("evt_relic");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑫ 노병 — 모닥불 옆에 홀로 앉은 실루엣
    // ══════════════════════════════════════════════════════════

    static void GenVeteran()
    {
        Begin();
        Sky(C(0.05f, 0.05f, 0.09f), C(0.14f, 0.10f, 0.10f));
        StarField(140, 53);
        Ridge(x => 50f + Mathf.Sin(x * 0.017f) * 6f, C(0.09f, 0.08f, 0.10f));

        float fx = W * 0.5f + 46f, fy = 62f;

        // ── 야영지 — 인물 하나만 두면 좌우가 텅 빈다 ─────────
        // 천막 두 채와 다른 모닥불로 "진영" 을 만들고, 뒤로 갈수록 어둡게 죽인다.
        for (int s = -1; s <= 1; s += 2)
        {
            float tx = W * 0.5f + s * 296f;
            Tri(tx, fy - 34f, tx - 54f, fy + 6f, tx + 54f, fy + 6f, C(0.072f, 0.066f, 0.086f));
            Capsule(tx, fy - 36f, tx, fy + 6f, 1.8f, C(0.11f, 0.10f, 0.13f));
            Capsule(tx - 54f, fy + 6f, tx - 66f, fy + 6f, 1.4f, C(0.09f, 0.085f, 0.11f));
            Capsule(tx + 54f, fy + 6f, tx + 66f, fy + 6f, 1.4f, C(0.09f, 0.085f, 0.11f));
        }
        // 먼 모닥불 — 작고 흐리게. 깊이감은 크기 차이가 만든다.
        float ffx = W * 0.16f;
        Glow(ffx, fy + 4f, 40f, C(1f, 0.58f, 0.22f), 0.32f);
        Disc(ffx, fy + 4f, 2.6f, C(1f, 0.78f, 0.38f, 0.7f));

        // 모닥불
        GroundShadow(fx - 60f, fy + 56f, 76f, 13f, 0.50f);
        Glow(fx, fy + 10f, 96f, C(1f, 0.62f, 0.24f), 0.70f);
        Tri(fx, fy + 30f, fx - 12f, fy, fx + 12f, fy, C(1f, 0.70f, 0.28f));
        Tri(fx, fy + 20f, fx - 7f,  fy, fx + 7f,  fy, C(1f, 0.90f, 0.60f));
        // 장작
        Capsule(fx - 18f, fy + 2f, fx + 16f, fy + 8f, 2.6f, C(0.26f, 0.17f, 0.11f));
        Capsule(fx - 16f, fy + 8f, fx + 18f, fy + 2f, 2.6f, C(0.22f, 0.14f, 0.09f));

        // 앉은 노병 — 굽은 등과 뻗은 한쪽 다리로 "불편한 다리" 를 읽히게 한다
        var body = C(0.10f, 0.09f, 0.12f);
        Ellipse(fx - 66f, fy + 26f, 15f, 18f, body);            // 몸통
        Disc(fx - 70f, fy + 50f, 9f, body);                      // 머리
        Capsule(fx - 56f, fy + 14f, fx - 24f, fy + 6f, 5f, body); // 뻗은 다리
        Capsule(fx - 66f, fy + 16f, fx - 48f, fy + 4f, 5f, body); // 접은 다리
        // 옆에 세워 둔 지팡이
        Capsule(fx - 84f, fy + 4f, fx - 78f, fy + 52f, 2f, C(0.22f, 0.16f, 0.11f));

        // 불빛이 등에 닿는 가장자리
        Capsule(fx - 54f, fy + 40f, fx - 52f, fy + 16f, 2.2f, C(1f, 0.66f, 0.32f, 0.55f));

        Embers(90, C(1f, 0.68f, 0.32f), 457);
        Vignette(0.66f);
        BottomFade(C(0.03f, 0.02f, 0.03f), 142f, 1.6f);
        Grain(0.022f, 431);
        Save("evt_veteran");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑤ 신비 — 성운, 별, 마법진
    // ══════════════════════════════════════════════════════════

    static void GenMystery()
    {
        Begin();
        Sky(C(0.04f, 0.02f, 0.10f), C(0.11f, 0.05f, 0.20f));
        Clouds(C(0.52f, 0.24f, 0.86f), 0.34f, 2.6f, 101);
        Clouds(C(0.20f, 0.42f, 0.90f), 0.20f, 4.4f, 137);

        StarField(220, 61);

        float cx = W * 0.5f, cy = 98f;

        // 마법진 — 이중 링 + 눈금
        Glow(cx, cy, 132f, C(0.66f, 0.36f, 1f), 0.52f);
        RingAA(cx, cy, 66f, 1.8f, C(0.78f, 0.56f, 1f, 0.55f));
        RingAA(cx, cy, 54f, 1.2f, C(0.70f, 0.46f, 1f, 0.38f));
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            float c1 = Mathf.Cos(a), s1 = Mathf.Sin(a);
            Capsule(cx + c1 * 58f, cy + s1 * 58f, cx + c1 * 66f, cy + s1 * 66f,
                    1.6f, C(0.86f, 0.66f, 1f, 0.50f));
        }

        // 물음표 — 굵은 획으로 또렷하게
        QuestionMark(cx, cy, C(0.94f, 0.82f, 1f, 0.96f));
        Glow(cx, cy, 40f, C(0.80f, 0.52f, 1f), 0.60f);

        Motes(200, C(0.86f, 0.72f, 1f), 149, 0.60f);
        Vignette(0.64f);
        BottomFade(C(0.03f, 0.01f, 0.08f), 148f, 1.6f);
        Grain(0.020f, 97);
        Save("evt_mystery");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑥ 숲 — 달밤, 3중 깊이의 침엽수림
    // ══════════════════════════════════════════════════════════

    static void GenForest()
    {
        Begin();
        Sky(C(0.03f, 0.06f, 0.09f), C(0.10f, 0.18f, 0.16f));

        // 달 (우측 3분할)
        float mx = W * 0.76f, my = 50f;
        Glow(mx, my, 130f, C(0.80f, 0.90f, 0.78f), 0.50f);
        Disc(mx, my, 27f, C(0.90f, 0.94f, 0.82f, 0.96f));
        Disc(mx - 8f, my - 5f, 5f,  C(0.78f, 0.83f, 0.70f, 0.55f));
        Disc(mx + 9f, my + 7f, 3.4f, C(0.78f, 0.83f, 0.70f, 0.45f));

        StarField(90, 211);
        Clouds(C(0.30f, 0.44f, 0.40f), 0.18f, 3.2f, 199);

        // 원경 — 안개에 잠긴 나무 (밝고 흐리게)
        for (int i = 0; i < 20; i++)
            Conifer(6f + i * 44f, 150f, 52f + Noise1(i * 1.7f, 3) * 26f,
                    C(0.13f, 0.21f, 0.20f, 0.72f));
        MistBand(140f, 26f, C(0.42f, 0.56f, 0.52f), 0.20f);

        // 중경
        for (int i = 0; i < 13; i++)
            Conifer(2f + i * 68f, 172f, 82f + Noise1(i * 2.9f, 7) * 34f,
                    C(0.05f, 0.11f, 0.09f, 0.92f));
        MistBand(166f, 22f, C(0.34f, 0.48f, 0.44f), 0.16f);

        // 근경 — 완전한 실루엣
        Conifer(W * 0.10f, 208f, 150f, C(0.012f, 0.030f, 0.026f, 1f));
        Conifer(W * 0.93f, 208f, 132f, C(0.012f, 0.030f, 0.026f, 1f));
        Ridge(x => 182f + Noise1(x * 0.014f, 13) * 7f, C(0.015f, 0.035f, 0.030f, 1f));

        Fireflies(46, 233);
        Vignette(0.62f);
        BottomFade(C(0.01f, 0.03f, 0.03f), 152f, 1.5f);
        Grain(0.018f, 113);
        Save("evt_forest");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑦ 물약 — 연금술 공방, 선반과 플라스크
    // ══════════════════════════════════════════════════════════

    static void GenPotion()
    {
        Begin();
        Sky(C(0.03f, 0.07f, 0.10f), C(0.06f, 0.16f, 0.19f));
        Clouds(C(0.16f, 0.60f, 0.52f), 0.20f, 3.8f, 151);

        // 뒷선반 — 역광 받는 병들
        Ridge(x => 96f, C(0.04f, 0.10f, 0.13f, 0.55f));
        var rng = new System.Random(23);
        for (int i = 0; i < 16; i++)
        {
            float x = 22f + i * 53f + rng.Next(-6, 7);
            float h = 26f + rng.Next(0, 18);
            RoundRect(x, 92f - h * 0.5f, 8f, h * 0.5f, 3f, C(0.06f, 0.17f, 0.19f, 0.80f));
            Capsule(x, 92f - h, x, 92f - h - 7f, 3f, C(0.06f, 0.17f, 0.19f, 0.80f));
            Capsule(x - 6.4f, 92f - h * 0.8f, x - 6.4f, 88f, 1.4f,
                    C(0.30f, 0.86f, 0.74f, 0.22f));
        }
        RoundRect(W * 0.5f, 98f, W * 0.5f, 4f, 1f, C(0.09f, 0.20f, 0.22f, 1f));

        // 작업대
        Ridge(x => 172f, C(0.05f, 0.11f, 0.13f, 1f));
        RoundRect(W * 0.5f, 174f, W * 0.5f, 5f, 1f, C(0.11f, 0.23f, 0.25f, 1f));

        // 초점 — 큰 플라스크 (좌측 3분할)
        float fx = W * 0.38f;
        Glow(fx, 138f, 138f, C(0.24f, 1f, 0.68f), 0.62f);
        Flask(fx, 171f, 44f, C(0.10f, 0.72f, 0.48f));

        // 보조 — 작은 플라스크 둘
        Flask(fx + 128f, 172f, 27f, C(0.14f, 0.56f, 0.72f));
        Flask(fx - 118f, 172f, 22f, C(0.60f, 0.40f, 0.78f));

        Motes(160, C(0.44f, 1f, 0.76f), 167, 0.60f);
        Vignette(0.58f);
        BottomFade(C(0.02f, 0.06f, 0.08f), 150f, 1.5f);
        Grain(0.018f, 127);
        Save("evt_potion");
    }

    // ══════════════════════════════════════════════════════════
    //  ⑧ 저주 — 룬 서클과 뻗어나가는 균열
    // ══════════════════════════════════════════════════════════

    static void GenDark()
    {
        Begin();
        Sky(C(0.02f, 0.01f, 0.04f), C(0.09f, 0.02f, 0.13f));
        Clouds(C(0.42f, 0.08f, 0.60f), 0.30f, 3.0f, 181);

        float cx = W * 0.5f, cy = 100f;

        // 바닥을 가로지르는 균열 — 화면 폭을 쓰게 해주는 요소
        var rng = new System.Random(41);
        for (int i = 0; i < 14; i++)
        {
            float a  = i * Mathf.PI * 2f / 14f + 0.2f;
            float len = 150f + rng.Next(0, 240);
            float ex = cx + Mathf.Cos(a) * len;
            float ey = cy + Mathf.Sin(a) * len * 0.42f;
            float mx = (cx + ex) * 0.5f + rng.Next(-24, 25);
            float my = (cy + ey) * 0.5f + rng.Next(-12, 13);
            Capsule(cx, cy, mx, my, 2.4f, C(0.44f, 0.08f, 0.62f, 0.42f));
            Capsule(mx, my, ex, ey, 1.4f, C(0.34f, 0.05f, 0.50f, 0.30f));
        }

        // 룬 서클
        Glow(cx, cy, 150f, C(0.62f, 0.10f, 0.86f), 0.55f);
        RingAA(cx, cy, 72f, 2.2f, C(0.66f, 0.14f, 0.88f, 0.60f));
        RingAA(cx, cy, 60f, 1.2f, C(0.56f, 0.10f, 0.78f, 0.42f));
        RingAA(cx, cy, 38f, 1.6f, C(0.72f, 0.22f, 0.94f, 0.50f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + 0.39f;
            float c1 = Mathf.Cos(a), s1 = Mathf.Sin(a);
            Capsule(cx + c1 * 38f, cy + s1 * 38f, cx + c1 * 60f, cy + s1 * 60f,
                    2.2f, C(0.80f, 0.34f, 1f, 0.55f));
            Disc(cx + c1 * 72f, cy + s1 * 72f, 3.4f, C(0.90f, 0.56f, 1f, 0.70f));
        }

        // 코어
        Glow(cx, cy, 46f, C(0.90f, 0.40f, 1f), 1.20f);
        Disc(cx, cy, 15f, C(0.34f, 0.05f, 0.48f, 0.94f));
        Disc(cx, cy,  9f, C(0.68f, 0.16f, 0.90f, 0.94f));
        Disc(cx, cy,  4f, C(0.96f, 0.80f, 1f, 1f));

        Embers(140, C(0.82f, 0.30f, 1f), 191);
        Vignette(0.70f);
        BottomFade(C(0.02f, 0.005f, 0.05f), 146f, 1.6f);
        Grain(0.022f, 149);
        Save("evt_dark");
    }

    // ══════════════════════════════════════════════════════════
    //  복합 오브젝트
    // ══════════════════════════════════════════════════════════

    // 검 — angle 도 단위 (0 = 우측 수평).
    // 날을 균일 폭 캡슐로 그리면 막대기로 보인다. 뾰족한 칼끝 + 또렷한
    // 십자 가드 + 손잡이·폼멜이 있어야 검으로 읽힌다.
    static void Sword(float cx, float cy, float angleDeg, Color steel, Color grip)
    {
        float a  = angleDeg * Mathf.Deg2Rad;
        float dx = Mathf.Cos(a), dy = Mathf.Sin(a);
        float nx = -dy,          ny = dx;      // 날 폭 방향

        float gX = cx - dx * 30f,  gY = cy - dy * 30f;    // 가드
        float sX = cx + dx * 80f,  sY = cy + dy * 80f;    // 칼끝 시작
        float tX = cx + dx * 106f, tY = cy + dy * 106f;   // 칼끝
        const float hw = 5.2f;

        // 날 — 캡슐 몸통 + 삼각 칼끝 (사각형 두 장으로 이으면 대각 이음선이 보인다)
        Capsule(gX, gY, sX, sY, hw * 2f, steel);
        Tri(tX, tY, sX + nx * hw, sY + ny * hw, sX - nx * hw, sY - ny * hw, steel);
        Capsule(gX, gY, sX, sY, 1.8f, steel * 0.62f);                    // 혈조
        Capsule(gX + nx * (hw - 1f), gY + ny * (hw - 1f),                // 한쪽 날 반사
                tX, tY, 1.5f, steel * 1.85f);

        // 십자 가드
        Capsule(gX - nx * 19f, gY - ny * 19f, gX + nx * 19f, gY + ny * 19f, 5.2f, grip * 2.2f);
        Disc(gX - nx * 19f, gY - ny * 19f, 2.8f, grip * 2.2f);
        Disc(gX + nx * 19f, gY + ny * 19f, 2.8f, grip * 2.2f);

        // 손잡이 + 폼멜
        Capsule(gX - dx * 3f, gY - dy * 3f, gX - dx * 27f, gY - dy * 27f, 4.4f, grip);
        Disc(gX - dx * 31f, gY - dy * 31f, 5.0f, grip * 1.9f);
    }

    // 방패 — 위는 사각, 아래는 뾰족한 형태
    static void Shield(float cx, float cy, float hw, float hh)
    {
        Color body = C(0.19f, 0.26f, 0.44f, 1f);
        Color rim  = C(0.50f, 0.62f, 0.90f, 0.95f);
        Color mark = C(0.72f, 0.82f, 1f, 0.92f);

        // 테두리 = 실루엣을 조금 크게 깔고 그 위에 본체를 얹는다.
        // 원형 Ring 으로 두르면 아래쪽 삼각과 모양이 어긋난다.
        ShieldSilhouette(cx, cy, hw + 3f, hh + 3f, rim);
        ShieldSilhouette(cx, cy, hw, hh, body);

        // 십자 문양
        Capsule(cx, cy - hh * 0.62f, cx, cy + hh * 0.52f, 7f, mark);
        Capsule(cx - hw * 0.62f, cy - hh * 0.16f, cx + hw * 0.62f, cy - hh * 0.16f, 7f, mark);

        // 좌상단 림라이트
        Capsule(cx - hw + 4f, cy - hh * 0.70f, cx - hw + 4f, cy + hh * 0.30f, 2.2f,
                C(0.78f, 0.88f, 1f, 0.45f));
    }

    // 방패 실루엣 — 상단 둥근 사각 + 하단 삼각
    static void ShieldSilhouette(float cx, float cy, float hw, float hh, Color c)
    {
        RoundRect(cx, cy - hh * 0.18f, hw, hh * 0.62f, 7f, c);
        Tri(cx, cy + hh * 0.92f, cx - hw, cy + hh * 0.40f, cx + hw, cy + hh * 0.40f, c);
    }

    // 동전 더미 — 둥근 사각형을 쌓으면 금괴처럼 보인다.
    // 위에서 비스듬히 본 타원(rx≫ry)을 겹쳐야 동전으로 읽힌다.
    static void CoinStack(float cx, float baseY, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float y = baseY - i * 8f;
            Ellipse(cx, y + 2.6f, 21f, 7.4f, C(0.40f, 0.27f, 0.04f, 1f));   // 옆면 두께
            Ellipse(cx, y,        21f, 7.4f, C(0.78f, 0.60f, 0.15f, 1f));   // 윗면
            Ellipse(cx, y - 0.5f, 14f, 4.8f, C(0.97f, 0.83f, 0.31f, 1f));   // 각인 면
            Capsule(cx - 12f, y - 2.8f, cx + 3f, y - 3.6f, 1.3f, C(1f, 0.97f, 0.66f, 0.55f));
        }
    }

    // 낙하하는 동전 (tilt 0=정면, 1=완전히 눕힘)
    static void Coin(float cx, float cy, float r, float tilt)
    {
        Glow(cx, cy, r * 3.4f, C(1f, 0.82f, 0.28f), 0.45f);
        Ellipse(cx, cy, r, r * Mathf.Max(0.18f, 1f - tilt), C(0.60f, 0.43f, 0.08f, 1f));
        Ellipse(cx, cy, r * 0.78f, r * Mathf.Max(0.14f, 1f - tilt) * 0.78f,
                C(0.94f, 0.78f, 0.26f, 1f));
        Ellipse(cx - r * 0.22f, cy - r * 0.18f, r * 0.24f,
                r * Mathf.Max(0.05f, 1f - tilt) * 0.24f, C(1f, 0.98f, 0.80f, 0.85f));
    }

    // 침엽수 — baseY 바닥, h 높이
    static void Conifer(float x, float baseY, float h, Color c)
    {
        Capsule(x, baseY, x, baseY - h * 0.16f, h * 0.030f, c * 0.72f);
        for (int i = 0; i < 4; i++)
        {
            float ty = baseY - h * (0.12f + i * 0.235f);
            float tw = h * (0.30f - i * 0.055f);
            float th = h * 0.36f;
            Tri(x, ty - th, x - tw, ty, x + tw, ty, c);
        }
    }

    // 플라스크 — baseY 바닥, r 몸통 반지름
    static void Flask(float cx, float baseY, float r, Color liquid)
    {
        float bulbY = baseY - r;
        // 유리는 반드시 불투명 — 반투명이면 뒤 선반 판자가 비쳐서 몸통을 가로지른다
        Disc(cx, bulbY, r,          C(0.11f, 0.22f, 0.25f, 1f));       // 유리
        Disc(cx, bulbY + r * 0.09f, r - 2.2f, liquid);                 // 고인 액체
        Disc(cx, bulbY + r * 0.34f, r - 5.0f, liquid * 0.74f);         // 바닥 그림자
        Disc(cx - r * 0.30f, bulbY - r * 0.34f, r * 0.30f,             // 유리 하이라이트
             C(1f, 1f, 1f, 0.20f));

        // 목 + 마개
        RoundRect(cx, bulbY - r - r * 0.42f, r * 0.24f, r * 0.46f, 2f,
                  C(0.12f, 0.24f, 0.28f, 0.88f));
        RoundRect(cx, bulbY - r - r * 0.92f, r * 0.32f, r * 0.16f, 2f,
                  C(0.32f, 0.21f, 0.10f, 0.95f));

        // 유리 하이라이트
        Capsule(cx - r * 0.58f, bulbY - r * 0.34f, cx - r * 0.44f, bulbY + r * 0.30f,
                r * 0.14f, C(0.90f, 1f, 0.98f, 0.34f));

        // 기포
        var rng = new System.Random((int)(cx * 7f) + (int)r);
        for (int i = 0; i < 9; i++)
            Disc(cx + rng.Next((int)-r + 7, (int)r - 6),
                 bulbY + rng.Next(2, (int)r - 5),
                 1.0f + (float)rng.NextDouble() * 1.3f,
                 C(0.92f, 1f, 0.96f, 0.26f));
    }

    // 물음표 — 굵은 획 (마법진 중앙용)
    static void QuestionMark(float cx, float cy, Color c)
    {
        float r = 17f;
        // 상단 갈고리
        for (int i = 0; i <= 26; i++)
        {
            float a0 = Mathf.PI * 1.08f - i * (Mathf.PI * 1.28f / 26f);
            float a1 = Mathf.PI * 1.08f - (i + 1) * (Mathf.PI * 1.28f / 26f);
            Capsule(cx + Mathf.Cos(a0) * r, cy - 16f - Mathf.Sin(a0) * r,
                    cx + Mathf.Cos(a1) * r, cy - 16f - Mathf.Sin(a1) * r, 6.4f, c);
        }
        Capsule(cx + 10f, cy - 6f, cx, cy + 12f, 6.4f, c);   // 목
        Disc(cx, cy + 26f, 5.2f, c);                          // 점
    }

    // ══════════════════════════════════════════════════════════
    //  입체 소품 — "장면" 을 만드는 최소 단위
    //
    //  ⚠ 정면 사각형은 아무리 색을 잘 골라도 상자로 안 보인다
    //    예전 창고 삽화가 허술했던 이유가 이것이다. 앞면만 칠한 정사각형
    //    네 개는 갈색 네모 네 개로 읽힌다. 윗면·옆면을 얹어 면이 세 개
    //    보여야 부피가 생기고, 바닥 그림자가 붙어야 떠 있지 않게 된다.
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 3/4 시점 상자. (cx, baseY) = 앞면 바닥 중앙, 깊이는 우상단으로 뻗는다.
    /// 광원은 항상 왼쪽 위 — 윗면이 가장 밝고 옆면이 가장 어둡다.
    /// </summary>
    static void Box3D(float cx, float baseY, float hw, float h, float depth, Color face)
    {
        float topY = baseY - h;
        float dx   = depth * 0.74f, dy = -depth * 0.50f;

        Color side = Opaque(face, 0.55f);
        Color top  = Opaque(face, 1.30f);

        // 옆면 → 윗면 → 앞면 순서. 뒤에 있는 면부터 칠해야 경계가 안 새어 나온다.
        Tri(cx + hw, baseY, cx + hw, topY, cx + hw + dx, topY + dy, side);
        Tri(cx + hw, baseY, cx + hw + dx, baseY + dy, cx + hw + dx, topY + dy, side);
        Tri(cx - hw, topY, cx + hw, topY, cx + hw + dx, topY + dy, top);
        Tri(cx - hw, topY, cx - hw + dx, topY + dy, cx + hw + dx, topY + dy, top);
        RoundRect(cx, topY + h * 0.5f, hw, h * 0.5f, 1.5f, face);
    }

    /// <summary>나무 상자 — 3/4 상자에 판자 이음새·쇠띠·대각 보강대를 얹는다.</summary>
    static void Crate(float cx, float baseY, float hw, float h, Color wood, int seed)
    {
        Box3D(cx, baseY, hw, h, hw * 0.62f, wood);

        Color dark = Opaque(wood, 0.46f);
        Color lite = Opaque(wood, 1.55f);
        float topY = baseY - h;

        for (int i = 1; i <= 2; i++)                         // 앞면 판자 이음새
        {
            float y = topY + h * i / 3f;
            Capsule(cx - hw + 1f, y, cx + hw - 1f, y, 1.2f, dark);
        }
        Capsule(cx - hw + 3f, baseY - 3f, cx + hw - 3f, topY + 3f, 2.2f, dark);   // 대각 보강대
        Capsule(cx - hw, topY, cx - hw, baseY, 2.4f, dark);                       // 좌우 쇠띠
        Capsule(cx + hw, topY, cx + hw, baseY, 2.4f, dark);
        Capsule(cx - hw + 1.8f, topY + 2f, cx - hw + 1.8f, baseY - 2f, 1.4f, lite); // 좌측 림라이트
        Capsule(cx - hw + 1f, topY, cx + hw - 1f, topY, 1.3f, lite);               // 윗 모서리

        var rng = new System.Random(seed);                    // 못 머리
        int spanX = Mathf.Max(1, (int)(hw * 2f) - 8);
        int spanY = Mathf.Max(1, (int)h - 8);
        for (int i = 0; i < 5; i++)
            Disc(cx - hw + 4f + rng.Next(0, spanX), topY + 4f + rng.Next(0, spanY), 1.0f, lite);
    }

    /// <summary>나무통 — 가운데가 불룩하고 쇠테 두 줄이 둘린다.</summary>
    static void Barrel(float cx, float baseY, float hw, float h, Color wood)
    {
        Color dark = Opaque(wood, 0.44f);
        Color hoop = C(0.26f, 0.24f, 0.22f);
        float topY = baseY - h, midY = (topY + baseY) * 0.5f;

        RoundRect(cx, midY, hw * 0.84f, h * 0.5f, 3f, wood);
        Ellipse(cx, midY, hw, h * 0.44f, wood);                   // 배부른 실루엣
        Ellipse(cx, topY + 2f, hw * 0.80f, hw * 0.26f, Opaque(wood, 1.28f));   // 뚜껑
        RingAA(cx, topY + 2f, hw * 0.80f, 1.1f, dark);

        for (int i = -2; i <= 2; i++)                              // 세로 판자
            Capsule(cx + i * hw * 0.33f, topY + 5f, cx + i * hw * 0.33f, baseY - 3f, 0.9f, dark);
        Capsule(cx - hw * 0.90f, topY + h * 0.28f, cx + hw * 0.90f, topY + h * 0.28f, 2.6f, hoop);
        Capsule(cx - hw * 0.90f, topY + h * 0.76f, cx + hw * 0.90f, topY + h * 0.76f, 2.6f, hoop);
        Capsule(cx - hw * 0.72f, topY + 8f, cx - hw * 0.86f, baseY - 8f, 1.6f,
                Opaque(wood, 1.5f));                               // 좌측 림라이트
    }

    /// <summary>곡물 자루 — 목을 끈으로 묶은 형태.</summary>
    static void Sack(float cx, float baseY, float r, Color cloth)
    {
        Color dark = Opaque(cloth, 0.52f);
        Ellipse(cx, baseY - r * 0.86f, r, r * 0.94f, cloth);
        Ellipse(cx, baseY - r * 0.24f, r * 1.04f, r * 0.44f, cloth);            // 눌린 바닥
        Capsule(cx, baseY - r * 1.62f, cx, baseY - r * 1.94f, r * 0.44f, Opaque(cloth, 0.88f));
        Capsule(cx - r * 0.36f, baseY - r * 1.70f, cx + r * 0.36f, baseY - r * 1.70f, 1.8f, dark);
        Capsule(cx - r * 0.50f, baseY - r * 1.20f, cx - r * 0.22f, baseY - r * 0.40f, 1.2f, dark);
        Capsule(cx + r * 0.44f, baseY - r * 1.28f, cx + r * 0.20f, baseY - r * 0.40f, 1.1f, dark);
        Ellipse(cx - r * 0.34f, baseY - r * 1.10f, r * 0.26f, r * 0.34f, Opaque(cloth, 1.30f));
    }

    /// <summary>
    /// 판자벽 — 세로 널을 이어 붙이고 틈·나뭇결·못을 넣는다.
    ///
    /// ⚠ 얇은 선 몇 개로는 벽이 되지 않는다
    ///   예전 창고 배경은 1.4px 캡슐 아홉 줄이 전부라 검은 화면에 그대로 묻혔다.
    ///   널을 실제로 칠하고 그 사이를 어둡게 파야 면으로 읽힌다.
    /// </summary>
    static void PlankWall(float x0, float x1, float topY, float botY,
                          Color wood, int seed, float boardW = 32f)
    {
        var rng = new System.Random(seed);
        Color gap = Opaque(wood, 0.30f);

        for (float x = x0; x < x1; x += boardW)
        {
            float w = Mathf.Min(boardW, x1 - x);
            if (w < 2f) break;

            float v = 0.80f + (float)rng.NextDouble() * 0.42f;
            Color c = Opaque(wood, v);
            RoundRect(x + w * 0.5f, (topY + botY) * 0.5f,
                      w * 0.5f - 1f, (botY - topY) * 0.5f, 1f, c);
            Capsule(x + w, topY, x + w, botY, 2.2f, gap);

            for (int g = 0; g < 3; g++)   // 나뭇결
            {
                float gy = topY + (float)rng.NextDouble() * (botY - topY);
                Capsule(x + 2f, gy, x + w - 2f, gy + rng.Next(-2, 3), 0.8f,
                        new Color(c.r * 0.66f, c.g * 0.66f, c.b * 0.66f, 0.6f));
            }
            Color nail = new Color(c.r * 1.7f, c.g * 1.7f, c.b * 1.7f, 0.75f);
            Disc(x + w * 0.5f, topY + 6f, 1.0f, nail);
            Disc(x + w * 0.5f, botY - 6f, 1.0f, nail);
        }
    }

    /// <summary>
    /// 원근 바닥. topY 아래를 널빤지로 채우고 이음선을 소실점으로 모은다.
    /// ⚠ 소품보다 먼저 그릴 것 — 나중에 그리면 위에 덮인다.
    /// </summary>
    static void PlankFloor(float topY, float vanishX, Color wood, int seed)
    {
        Ridge(x => topY, wood);

        var rng = new System.Random(seed);
        Color seam = Opaque(wood, 0.52f);

        for (int i = -10; i <= 10; i++)   // 세로 이음선 — 위로 갈수록 모인다
            Capsule(vanishX + i * 9f, topY, vanishX + i * 96f, H + 4f, 1.4f, seam);

        for (int i = 1; i <= 5; i++)      // 가로 이음선 — 아래로 갈수록 간격이 벌어진다
        {
            float t = i / 5f;
            float y = Mathf.Lerp(topY, H + 6f, t * t);
            Capsule(0f, y, W, y, 1.2f, Opaque(wood, 0.62f));
        }
        for (int i = 0; i < 26; i++)      // 얼룩 — 균일한 면은 CG 처럼 보인다
        {
            float x = rng.Next(0, W), y = topY + rng.Next(2, Mathf.Max(3, H - (int)topY));
            Ellipse(x, y, 10f + rng.Next(0, 26), 3f + rng.Next(0, 6),
                    new Color(wood.r * 0.7f, wood.g * 0.7f, wood.b * 0.7f, 0.22f));
        }
    }

    /// <summary>
    /// 바닥 접지 그림자 — 곱연산으로 어둡게 한다.
    /// 소품이 "떠 있지 않게" 만드는 가장 싼 방법이며, 없으면 스티커처럼 보인다.
    /// </summary>
    static void GroundShadow(float cx, float cy, float rx, float ry, float strength)
    {
        int x0 = Mathf.Max(0, (int)(cx - rx)), x1 = Mathf.Min(W, (int)(cx + rx) + 1);
        int y0 = Mathf.Max(0, (int)(cy - ry)), y1 = Mathf.Min(H, (int)(cy + ry) + 1);
        for (int sy = y0; sy < y1; sy++)
        for (int x  = x0; x  < x1; x++)
        {
            float dx = (x + 0.5f - cx) / rx, dy = (sy + 0.5f - cy) / ry;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);
            if (d >= 1f) continue;
            MulPx(x, sy, 1f - (1f - Mathf.Pow(d, 1.6f)) * strength);
        }
    }

    /// <summary>
    /// 비스듬한 빛줄기. (ax,ay) 에서 (bx,by) 로 퍼지며 흐려진다.
    ///
    /// ⚠ LightShaft 는 수직 전용이고 topY 가 화면 밖이면 한 픽셀도 안 그린다
    ///   창고 삽화의 "문틈으로 새는 빛" 이 topY=200(=H) 으로 들어가 있어
    ///   루프가 한 번도 돌지 않았다. 장면의 유일한 광원이 통째로 빠진 채였다.
    ///   문·창문에서 들어오는 빛은 애초에 기울어져 있으니 이쪽을 쓴다.
    /// </summary>
    static void LightBeam(float ax, float ay, float bx, float by,
                          float hwA, float hwB, Color c, float strength)
    {
        float dx = bx - ax, dy = by - ay;
        float len2 = dx * dx + dy * dy;
        if (len2 < 0.001f) return;
        float len = Mathf.Sqrt(len2);

        float pad = Mathf.Max(hwA, hwB) + 2f;
        int x0 = Mathf.Max(0, (int)(Mathf.Min(ax, bx) - pad));
        int x1 = Mathf.Min(W, (int)(Mathf.Max(ax, bx) + pad) + 1);
        int y0 = Mathf.Max(0, (int)(Mathf.Min(ay, by) - pad));
        int y1 = Mathf.Min(H, (int)(Mathf.Max(ay, by) + pad) + 1);

        for (int sy = y0; sy < y1; sy++)
        for (int x  = x0; x  < x1; x++)
        {
            float px = x + 0.5f - ax, py = sy + 0.5f - ay;
            float t  = (px * dx + py * dy) / len2;
            if (t < 0f || t > 1f) continue;

            float hw = Mathf.Lerp(hwA, hwB, t);
            float d  = Mathf.Abs(px * dy - py * dx) / len;
            float e  = 1f - d / hw;
            if (e <= 0f) continue;

            AddPx(x, sy, c, e * e * (1f - t) * (1f - t) * strength);
        }
    }

    /// <summary>빛줄기 속 먼지 — 빛에 걸린 티끌이 보여야 공기가 있는 공간이 된다.</summary>
    static void BeamDust(float ax, float ay, float bx, float by,
                         float hwA, float hwB, Color c, int count, int seed)
    {
        float dx = bx - ax, dy = by - ay;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return;
        float nx = -dy / len, ny = dx / len;

        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            float t = (float)rng.NextDouble();
            float o = (float)rng.NextDouble() * 2f - 1f;
            float hw = Mathf.Lerp(hwA, hwB, t) * 0.92f;
            float a  = (0.25f + (float)rng.NextDouble() * 0.75f)
                       * (1f - t * 0.55f) * (1f - Mathf.Abs(o) * 0.7f);
            Disc(ax + dx * t + nx * o * hw, ay + dy * t + ny * o * hw,
                 0.6f + (float)rng.NextDouble() * 1.1f, new Color(c.r, c.g, c.b, a));
        }
    }

    /// <summary>거미줄 — a0~a1 부채꼴. 구석에 걸어 두면 "방치됨" 이 한 번에 읽힌다.</summary>
    static void Cobweb(float cx, float cy, float r, float a0, float a1, Color c)
    {
        const int Spokes = 5;
        for (int i = 0; i <= Spokes; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)Spokes);
            Capsule(cx, cy, cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, 0.8f, c);
        }
        for (int ring = 1; ring <= 3; ring++)
        {
            float rr = r * ring / 3.3f;
            for (int i = 0; i < Spokes; i++)
            {
                float s = Mathf.Lerp(a0, a1, i / (float)Spokes);
                float e = Mathf.Lerp(a0, a1, (i + 1) / (float)Spokes);
                Capsule(cx + Mathf.Cos(s) * rr, cy + Mathf.Sin(s) * rr,
                        cx + Mathf.Cos(e) * rr, cy + Mathf.Sin(e) * rr, 0.7f, c);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  배경 · 분위기
    // ══════════════════════════════════════════════════════════

    static void Sky(Color top, Color bottom)
    {
        for (int sy = 0; sy < H; sy++)
        {
            float t = (float)sy / (H - 1);
            Color c = Color.Lerp(top, bottom, t * t * 0.72f + t * 0.28f);
            for (int x = 0; x < W; x++) _px[(H - 1 - sy) * W + x] = c;
        }
    }

    // fbm 구름 — 상단에 몰리게 가중치를 준다
    static void Clouds(Color c, float strength, float scale, int seed)
    {
        for (int sy = 0; sy < H; sy++)
        for (int x  = 0; x < W;  x++)
        {
            float n = Fbm(x / (W / scale), sy / (H / (scale * 0.55f)), 4, seed);
            float w = 1f - (float)sy / H;              // 위쪽일수록 진하게
            float a = Mathf.Clamp01((n - 0.45f) * 2.4f) * strength * (0.35f + w * 0.75f);
            if (a > 0.002f) Px(x, sy, new Color(c.r, c.g, c.b, a));
        }
    }

    // 안개 띠 — 원경과 근경을 분리해 깊이를 만든다
    static void MistBand(float centerY, float thickness, Color c, float strength)
    {
        for (int sy = 0; sy < H; sy++)
        {
            float d = Mathf.Abs(sy - centerY) / thickness;
            if (d >= 1f) continue;
            float fall = (1f - d) * (1f - d);
            for (int x = 0; x < W; x++)
            {
                float n = Fbm(x / 90f, sy / 26f, 3, 313);
                float a = fall * strength * (0.45f + n);
                if (a > 0.002f) Px(x, sy, new Color(c.r, c.g, c.b, a));
            }
        }
    }

    // 능선 채우기 — ridgeSy(x) 아래를 전부 칠한다 (상단 경계 AA)
    static void Ridge(Func<float, float> ridgeSy, Color c)
    {
        for (int x = 0; x < W; x++)
        {
            float ry = ridgeSy(x);
            int start = Mathf.Max(0, Mathf.FloorToInt(ry) - 1);
            for (int sy = start; sy < H; sy++)
            {
                float cov = Mathf.Clamp01((sy + 0.5f) - ry);
                if (cov <= 0f) continue;
                Px(x, sy, new Color(c.r, c.g, c.b, c.a * cov));
            }
        }
    }

    // 위에서 내려오는 빛기둥 (사다리꼴 + 아래로 감쇠)
    static void LightShaft(float cx, float topY, float halfW, float length, Color c, float strength)
    {
        for (int sy = (int)topY; sy < Mathf.Min(H, topY + length); sy++)
        {
            float t   = (sy - topY) / length;
            float hw  = halfW * (0.45f + t * 0.90f);
            float fall = (1f - t) * (1f - t) * strength;
            for (int x = (int)(cx - hw); x <= (int)(cx + hw); x++)
            {
                float e = 1f - Mathf.Abs(x - cx) / hw;
                if (e <= 0f) continue;
                AddPx(x, sy, c, fall * e * e);
            }
        }
    }

    static void StarField(int count, int seed)
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(0, W);
            float y = rng.Next(0, (int)(H * 0.80f));
            float b = 0.25f + (float)rng.NextDouble() * 0.75f;
            float r = 0.6f + (float)rng.NextDouble() * 1.5f;
            Disc(x, y, r, C(1f, 0.97f, 1f, b * 0.85f));
            if (b > 0.86f)   // 밝은 별에만 십자 플레어
            {
                Glow(x, y, 9f, C(0.85f, 0.90f, 1f), 0.55f);
                Capsule(x - 5f, y, x + 5f, y, 0.9f, C(1f, 1f, 1f, 0.34f));
                Capsule(x, y - 5f, x, y + 5f, 0.9f, C(1f, 1f, 1f, 0.34f));
            }
        }
    }

    // 위로 갈수록 흐려지는 불티
    static void Embers(int count, Color c, int seed)
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(0, W);
            float y = rng.Next(10, H);
            float t = 1f - y / (float)H;
            float a = (0.20f + (float)rng.NextDouble() * 0.65f) * (1f - t * 0.72f);
            float r = 0.7f + (float)rng.NextDouble() * 1.7f;
            Disc(x, y, r, new Color(c.r, c.g, c.b, a));
            if (r > 1.9f) Glow(x, y, r * 5f, c, a * 0.55f);
        }
    }

    static void Motes(int count, Color c, int seed, float maxAlpha)
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(0, W);
            float y = rng.Next(0, H);
            float a = (float)rng.NextDouble() * maxAlpha;
            Disc(x, y, 0.6f + (float)rng.NextDouble() * 1.2f,
                 new Color(c.r, c.g, c.b, a));
        }
    }

    static void Fireflies(int count, int seed)
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < count; i++)
        {
            float x = rng.Next(0, W);
            float y = rng.Next(70, H - 10);
            float b = 0.35f + (float)rng.NextDouble() * 0.65f;
            Glow(x, y, 8f, C(0.85f, 1f, 0.50f), b * 0.85f);
            Disc(x, y, 1.1f, C(0.92f, 1f, 0.62f, b));
        }
    }

    // ══════════════════════════════════════════════════════════
    //  후처리
    // ══════════════════════════════════════════════════════════

    // 가장자리를 어둡게 — 시선을 중앙으로 모은다.
    // 가로가 길므로 x 가중치를 낮춰야 좌우가 과하게 죽지 않는다.
    static void Vignette(float strength)
    {
        for (int sy = 0; sy < H; sy++)
        for (int x  = 0; x < W;  x++)
        {
            float nx = (x / (float)(W - 1) - 0.5f) * 2f;
            float ny = (sy / (float)(H - 1) - 0.5f) * 2f;
            float d  = Mathf.Sqrt(nx * nx * 0.62f + ny * ny * 0.85f);
            float k  = Mathf.Clamp01((d - 0.42f) / 0.85f);
            float m  = 1f - k * k * strength;
            int i = (H - 1 - sy) * W + x;
            Color c = _px[i];
            _px[i] = new Color(c.r * m, c.g * m, c.b * m, c.a);
        }
    }

    // 하단 페이드 — 삽화 아래에 본문 패널이 붙으므로 경계를 녹인다
    static void BottomFade(Color c, float startY, float power)
    {
        for (int sy = (int)startY; sy < H; sy++)
        {
            float t = (sy - startY) / (H - startY);
            float a = Mathf.Pow(Mathf.Clamp01(t), power) * 0.92f;
            for (int x = 0; x < W; x++)
                Px(x, sy, new Color(c.r, c.g, c.b, a));
        }
    }

    static void Grain(float amount, int seed)
    {
        var rng = new System.Random(seed);
        for (int i = 0; i < _px.Length; i++)
        {
            float n = ((float)rng.NextDouble() - 0.5f) * amount;
            Color c = _px[i];
            _px[i] = new Color(c.r + n, c.g + n, c.b + n, c.a);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  안티에일리어싱 도형 (SDF 커버리지)
    // ══════════════════════════════════════════════════════════

    // sdf(x,y) < 0 이 내부. 경계에서 feather 폭만큼 부드럽게 채운다.
    static void Sdf(int x0, int y0, int x1, int y1, Func<float, float, float> sdf,
                    Color c, float feather)
    {
        x0 = Mathf.Max(0, x0);  y0 = Mathf.Max(0, y0);
        x1 = Mathf.Min(W, x1);  y1 = Mathf.Min(H, y1);
        for (int sy = y0; sy < y1; sy++)
        for (int x  = x0; x  < x1; x++)
        {
            float cov = Mathf.Clamp01(0.5f - sdf(x + 0.5f, sy + 0.5f) / feather);
            if (cov <= 0f) continue;
            Px(x, sy, new Color(c.r, c.g, c.b, c.a * cov));
        }
    }

    static void Disc(float cx, float cy, float r, Color c, float feather = 1.4f)
        => Sdf((int)(cx - r - 2), (int)(cy - r - 2), (int)(cx + r + 3), (int)(cy + r + 3),
               (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, feather);

    static void Ellipse(float cx, float cy, float rx, float ry, Color c, float feather = 1.4f)
        => Sdf((int)(cx - rx - 2), (int)(cy - ry - 2), (int)(cx + rx + 3), (int)(cy + ry + 3),
               (x, y) =>
               {
                   float dx = (x - cx) / Mathf.Max(0.001f, rx);
                   float dy = (y - cy) / Mathf.Max(0.001f, ry);
                   return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
               }, c, feather);

    static void RingAA(float cx, float cy, float r, float thick, Color c, float feather = 1.2f)
    {
        float o = r + thick;
        Sdf((int)(cx - o - 2), (int)(cy - o - 2), (int)(cx + o + 3), (int)(cy + o + 3),
            (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r)
                      - thick * 0.5f, c, feather);
    }

    static void Capsule(float ax, float ay, float bx, float by, float thick,
                        Color c, float feather = 1.2f)
    {
        float pad = thick + 2f;
        Sdf((int)(Mathf.Min(ax, bx) - pad), (int)(Mathf.Min(ay, by) - pad),
            (int)(Mathf.Max(ax, bx) + pad), (int)(Mathf.Max(ay, by) + pad),
            (x, y) =>
            {
                float pax = x - ax,  pay = y - ay;
                float bax = bx - ax, bay = by - ay;
                float len2 = bax * bax + bay * bay;
                float h = len2 < 0.0001f ? 0f
                        : Mathf.Clamp01((pax * bax + pay * bay) / len2);
                float dx = pax - bax * h, dy = pay - bay * h;
                return Mathf.Sqrt(dx * dx + dy * dy) - thick * 0.5f;
            }, c, feather);
    }

    static void RoundRect(float cx, float cy, float hw, float hh, float rad,
                          Color c, float feather = 1.2f)
    {
        rad = Mathf.Min(rad, Mathf.Min(hw, hh));
        Sdf((int)(cx - hw - 2), (int)(cy - hh - 2), (int)(cx + hw + 3), (int)(cy + hh + 3),
            (x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - (hw - rad);
                float qy = Mathf.Abs(y - cy) - (hh - rad);
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - rad;
            }, c, feather);
    }

    static void Tri(float ax, float ay, float bx, float by, float cx2, float cy2,
                    Color c, float feather = 1.2f)
    {
        int x0 = (int)(Mathf.Min(ax, Mathf.Min(bx, cx2)) - 2);
        int y0 = (int)(Mathf.Min(ay, Mathf.Min(by, cy2)) - 2);
        int x1 = (int)(Mathf.Max(ax, Mathf.Max(bx, cx2)) + 3);
        int y1 = (int)(Mathf.Max(ay, Mathf.Max(by, cy2)) + 3);

        // 삼각형 내부 = 세 반평면의 교집합. 최대값이 곧 근사 SDF.
        float sign = Cross(ax, ay, bx, by, cx2, cy2) < 0f ? -1f : 1f;
        Sdf(x0, y0, x1, y1, (x, y) =>
        {
            float e0 = Cross(ax, ay, bx, by, x, y)  * sign;
            float e1 = Cross(bx, by, cx2, cy2, x, y) * sign;
            float e2 = Cross(cx2, cy2, ax, ay, x, y) * sign;
            float l0 = Dist(ax, ay, bx, by), l1 = Dist(bx, by, cx2, cy2), l2 = Dist(cx2, cy2, ax, ay);
            return -Mathf.Min(e0 / Mathf.Max(l0, 0.001f),
                   Mathf.Min(e1 / Mathf.Max(l1, 0.001f),
                             e2 / Mathf.Max(l2, 0.001f)));
        }, c, feather);
    }

    // 가산 광원 — 겹칠수록 밝아진다 (알파 블렌딩으로는 빛 느낌이 안 난다)
    static void Glow(float cx, float cy, float radius, Color c, float power)
    {
        int x0 = Mathf.Max(0, (int)(cx - radius)), x1 = Mathf.Min(W, (int)(cx + radius) + 1);
        int y0 = Mathf.Max(0, (int)(cy - radius)), y1 = Mathf.Min(H, (int)(cy + radius) + 1);
        for (int sy = y0; sy < y1; sy++)
        for (int x  = x0; x  < x1; x++)
        {
            float dx = x + 0.5f - cx, dy = sy + 0.5f - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
            if (d >= 1f) continue;
            AddPx(x, sy, c, Mathf.Pow(1f - d, 2.4f) * power);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  픽셀 · 노이즈 · 저장
    // ══════════════════════════════════════════════════════════

    static void Begin() => _px = new Color[W * H];

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    /// <summary>
    /// 밝기만 k 배 하고 알파는 1 로 고정한다.
    /// ⚠ Color * float 를 쓰지 말 것 — 알파까지 곱해져서 어둡게 만들려던 면이
    ///   반투명해지고 뒤 배경이 비쳐 나온다 (상자 옆면이 벽에 먹히는 원인).
    /// </summary>
    static Color Opaque(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

    static float Cross(float ax, float ay, float bx, float by, float px, float py)
        => (bx - ax) * (py - ay) - (by - ay) * (px - ax);

    static float Dist(float ax, float ay, float bx, float by)
        => Mathf.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));

    // 알파 블렌딩 (sy = screenY)
    static void Px(int x, int sy, Color c)
    {
        if ((uint)x >= W || (uint)sy >= H || c.a <= 0f) return;
        int i = (H - 1 - sy) * W + x;
        Color d = _px[i];
        float a = Mathf.Clamp01(c.a);
        _px[i] = new Color(d.r + (c.r - d.r) * a,
                           d.g + (c.g - d.g) * a,
                           d.b + (c.b - d.b) * a, 1f);
    }

    // 곱연산 — 그림자용. 알파 블렌딩으로 검정을 덮으면 색이 죽는다.
    static void MulPx(int x, int sy, float k)
    {
        if ((uint)x >= W || (uint)sy >= H) return;
        int i = (H - 1 - sy) * W + x;
        Color d = _px[i];
        _px[i] = new Color(d.r * k, d.g * k, d.b * k, 1f);
    }

    static void AddPx(int x, int sy, Color c, float k)
    {
        if ((uint)x >= W || (uint)sy >= H || k <= 0f) return;
        int i = (H - 1 - sy) * W + x;
        Color d = _px[i];
        _px[i] = new Color(d.r + c.r * k, d.g + c.g * k, d.b + c.b * k, 1f);
    }

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    static float ValueNoise(float x, float y, int seed)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3f - 2f * xf);
        float v = yf * yf * (3f - 2f * yf);
        return Mathf.Lerp(
            Mathf.Lerp(Hash(xi, yi, seed),     Hash(xi + 1, yi, seed),     u),
            Mathf.Lerp(Hash(xi, yi + 1, seed), Hash(xi + 1, yi + 1, seed), u), v);
    }

    static float Fbm(float x, float y, int octaves, int seed)
    {
        float sum = 0f, amp = 0.5f, f = 1f;
        for (int i = 0; i < octaves; i++)
        {
            sum += ValueNoise(x * f, y * f, seed + i * 37) * amp;
            amp *= 0.5f; f *= 2f;
        }
        return sum;
    }

    // 능선 흔들기용 1D 노이즈 (-1 ~ 1)
    static float Noise1(float x, int seed) => ValueNoise(x, seed * 0.37f, seed) * 2f - 1f;

    static void Save(string name)
    {
        for (int i = 0; i < _px.Length; i++)
        {
            Color c = _px[i];
            _px[i] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
        }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels(_px);
        tex.Apply();
        File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }
}
