using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EffectTextureGenerator.cs
//  Unity 메뉴 BattleGame > Generate Effect Textures & Materials
//  22개 액티브 스킬 이펙트용 절차적 텍스처(128×128)와
//  머티리얼을 자동 생성한다.
//
//  저장 경로:
//    텍스처  : Assets/_project/3.Textures/FX/
//    머티리얼 : Assets/_project/4.Materials/FX/
//
//  ■ 생성 텍스처 (흰색 RGB, 알파로 형태 표현)
//    TX_FX_Soft      — 부드러운 방사형 글로우 (버프/흡수/치유)
//    TX_FX_Slash     — 45° 대각 섬광 스트릭 (슬래시 임팩트)
//    TX_FX_Spark     — 방향성 눈물방울 불꽃 (화살/속도/충격파)
//    TX_FX_Smoke     — 불규칙 연기 덩어리 (먼지/연기)
//    TX_FX_Ring      — 속이 빈 링 파동 (충격파/속박)
//    TX_FX_Flame     — 위로 좁아지는 불꽃 (메테오/폭발/광전사)
//    TX_FX_Snowflake — 6각 눈결정 (눈보라)
//    TX_FX_Cross     — 십자 치유 심볼 (대상 치유)
//    TX_FX_Star      — 4+4 방향 별빛 (임팩트/전투함성)
//    TX_FX_Diamond   — 마름모 방어막 (방어막)
//    TX_FX_Rune      — 마법진 룬 (소환)
//    TX_FX_Poison    — 독 방울 (독 안개)
//
//  ■ 사용법
//    1. BattleGame > Generate Effect Textures & Materials 실행
//    2. BattleGame > Generate Effect Prefabs 실행
// ============================================================

public static class EffectTextureGenerator
{
    const string kTexPath = "Assets/_project/3.Textures/FX";
    const string kMatPath = "Assets/_project/4.Materials/FX";
    const int    kSz      = 128;

    // ── 진입점 ──────────────────────────────────────────────────────────

    [MenuItem("BattleGame/Generate Effect Textures & Materials")]
    public static void GenerateAll()
    {
        EnsureDir(kTexPath);
        EnsureDir(kMatPath);

        // 텍스처 생성 & PNG 저장
        SavePng("TX_FX_Soft",      GenSoft());
        SavePng("TX_FX_Slash",     GenSlash());
        SavePng("TX_FX_Spark",     GenSpark());
        SavePng("TX_FX_Smoke",     GenSmoke());
        SavePng("TX_FX_Ring",      GenRing());
        SavePng("TX_FX_Flame",     GenFlame());
        SavePng("TX_FX_Snowflake", GenSnowflake());
        SavePng("TX_FX_Cross",     GenCross());
        SavePng("TX_FX_Star",      GenStar());
        SavePng("TX_FX_Diamond",   GenDiamond());
        SavePng("TX_FX_Rune",      GenRune());
        SavePng("TX_FX_Poison",    GenPoison());
        SavePng("TX_FX_Arrow",     GenArrow());
        SavePng("TX_FX_Line",      GenLine());

        AssetDatabase.Refresh();  // PNG 1차 임포트

        // 알파 투명도 임포트 설정 적용 후 재임포트
        ConfigureTextureImports();

        // 머티리얼 생성 (Additive / Alpha Blended 분류)
        MakeMat("MAT_FX_Soft_Add",      "TX_FX_Soft",      additive: true);
        MakeMat("MAT_FX_Soft_Alpha",    "TX_FX_Soft",      additive: false);
        MakeMat("MAT_FX_Slash_Add",     "TX_FX_Slash",     additive: true);
        MakeMat("MAT_FX_Spark_Add",     "TX_FX_Spark",     additive: true);
        MakeMat("MAT_FX_Smoke_Alpha",   "TX_FX_Smoke",     additive: false);
        MakeMat("MAT_FX_Ring_Add",      "TX_FX_Ring",      additive: true);
        MakeMat("MAT_FX_Flame_Add",     "TX_FX_Flame",     additive: true);
        MakeMat("MAT_FX_Snowflake_Add", "TX_FX_Snowflake", additive: true);
        MakeMat("MAT_FX_Cross_Add",     "TX_FX_Cross",     additive: true);
        MakeMat("MAT_FX_Star_Add",      "TX_FX_Star",      additive: true);
        MakeMat("MAT_FX_Diamond_Add",   "TX_FX_Diamond",   additive: true);
        MakeMat("MAT_FX_Rune_Add",      "TX_FX_Rune",      additive: true);
        MakeMat("MAT_FX_Poison_Alpha",  "TX_FX_Poison",    additive: false);
        MakeMat("MAT_FX_Arrow_Add",     "TX_FX_Arrow",     additive: true);
        MakeMat("MAT_FX_Line_Add",      "TX_FX_Line",      additive: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[EffectTextureGenerator] ✓ 14 textures + 15 materials generated.");
    }

    // ── 픽셀 생성 공통 ───────────────────────────────────────────────────

    // cx, cy ∈ [-0.5, 0.5] (중심 = 0,0)
    delegate float PixFn(float cx, float cy);

    static Texture2D BuildTex(PixFn fn)
    {
        var tex = new Texture2D(kSz, kSz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        for (int py = 0; py < kSz; py++)
        for (int px = 0; px < kSz; px++)
        {
            float cx = (px + 0.5f) / kSz - 0.5f;
            float cy = (py + 0.5f) / kSz - 0.5f;
            float a  = Mathf.Clamp01(fn(cx, cy));
            tex.SetPixel(px, py, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    static void SavePng(string name, Texture2D tex)
    {
        byte[] png      = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        string fullPath = Path.Combine(
            Application.dataPath, "_project/3.Textures/FX", name + ".png");
        File.WriteAllBytes(fullPath, png);
    }

    // ── 수학 헬퍼 ────────────────────────────────────────────────────────

    static float Gauss(float x, float mu, float sigma)
    {
        float d = (x - mu) / sigma;
        return Mathf.Exp(-0.5f * d * d);
    }

    // ── 텍스처 생성 ──────────────────────────────────────────────────────

    // 1. Soft radial glow — 부드러운 방사형 빛 (버프/흡수/치유/소환 파편)
    static Texture2D GenSoft() => BuildTex((cx, cy) =>
    {
        float d = Mathf.Sqrt(cx * cx + cy * cy) / 0.5f;  // 0(중심)~1(가장자리)
        return Mathf.Pow(Mathf.Max(0f, 1f - d * d), 1.5f);
    });

    // 2. Slash streak — 45° 대각선 섬광 (슬래시 임팩트)
    static Texture2D GenSlash() => BuildTex((cx, cy) =>
    {
        // 45° 회전
        float rx =  cx * 0.7071f + cy * 0.7071f;
        float ry = -cx * 0.7071f + cy * 0.7071f;
        // 넓은 빛줄기: 긴 축 0.45, 짧은 축 0.07
        float a  = Mathf.Max(0f, 1f - (rx / 0.45f) * (rx / 0.45f)
                                     - (ry / 0.07f) * (ry / 0.07f));
        // 중심 핵심 섬광: 더 가는 라인
        float a2 = Mathf.Max(0f, 1f - (rx / 0.45f) * (rx / 0.45f)
                                     - (ry / 0.02f) * (ry / 0.02f));
        return Mathf.Clamp01(a + a2 * 0.6f);
    });

    // 3. Spark / teardrop — 방향성 불꽃 파편 (화살, 속도, 충격파)
    static Texture2D GenSpark() => BuildTex((cx, cy) =>
    {
        // +y 방향이 뾰족, -y 방향이 둥근 눈물방울
        float tip = Mathf.Max(0f, cy);          // 위로 갈수록 더 좁아짐
        float ex  = cx / 0.10f;
        float ey  = cy / 0.38f;
        float d   = Mathf.Sqrt(ex * ex + ey * ey);
        return Mathf.Max(0f, 1f - d * (1f + tip * 2.5f));
    });

    // 4. Smoke cloud — 불규칙 연기 덩어리 (먼지, 연기, 독 안개)
    static Texture2D GenSmoke() => BuildTex((cx, cy) =>
    {
        float a = 0f;
        a += Gauss(Mathf.Sqrt((cx)         * (cx)         + (cy)         * (cy)),         0f, 0.21f);
        a += Gauss(Mathf.Sqrt((cx + 0.10f) * (cx + 0.10f) + (cy - 0.07f) * (cy - 0.07f)), 0f, 0.15f) * 0.75f;
        a += Gauss(Mathf.Sqrt((cx - 0.11f) * (cx - 0.11f) + (cy + 0.05f) * (cy + 0.05f)), 0f, 0.13f) * 0.65f;
        a += Gauss(Mathf.Sqrt((cx + 0.04f) * (cx + 0.04f) + (cy + 0.09f) * (cy + 0.09f)), 0f, 0.17f) * 0.55f;
        return Mathf.Clamp01(a);
    });

    // 5. Ring / shockwave — 속이 빈 링 파동 (충격파, 착지, 속박)
    static Texture2D GenRing() => BuildTex((cx, cy) =>
    {
        float d = Mathf.Sqrt(cx * cx + cy * cy) / 0.5f;  // 0~1
        return Gauss(d, 0.72f, 0.11f);
    });

    // 6. Flame blob — 위로 좁아지는 불꽃 (메테오, 폭발, 광전사)
    static Texture2D GenFlame() => BuildTex((cx, cy) =>
    {
        // 아래쪽(-y)이 넓고, 위쪽(+y)으로 갈수록 좁아지는 불꽃
        float scaleX = 0.32f * (1f - Mathf.Max(0f, cy) * 0.9f);
        float scaleY = 0.38f;
        float ox     = cx / Mathf.Max(0.01f, scaleX);
        float oy     = (cy + 0.05f) / scaleY;
        float d      = Mathf.Sqrt(ox * ox + oy * oy);
        return Mathf.Pow(Mathf.Max(0f, 1f - d), 0.65f);
    });

    // 7. Snowflake — 6각 눈결정 (눈보라)
    static Texture2D GenSnowflake() => BuildTex((cx, cy) =>
    {
        float d     = Mathf.Sqrt(cx * cx + cy * cy);
        float angle = Mathf.Atan2(cy, cx);

        // 중심 점
        float result = Gauss(d, 0f, 0.055f);

        // 6방향 팔 (60° 간격)
        for (int i = 0; i < 6; i++)
        {
            float a  = angle - i * Mathf.PI / 3f;
            float ax = d * Mathf.Cos(a);
            float ay = d * Mathf.Sin(a);
            // 팔 본체: 긴 축 0.42, 두께 0.025
            float arm = Mathf.Max(0f, 1f - (ax / 0.42f) * (ax / 0.42f)
                                         - (ay / 0.025f) * (ay / 0.025f));
            arm *= Mathf.Max(0f, 1f - d / 0.44f);

            // 보조 가지 (팔 1/3·2/3 지점에서 분기)
            float[] branchDist = { 0.13f, 0.25f };
            foreach (float bd in branchDist)
            {
                float bx   = ax - bd;
                float bArm = Mathf.Max(0f, 1f - (bx  / 0.015f) * (bx  / 0.015f)
                                             - (ay  / 0.065f) * (ay  / 0.065f));
                arm = Mathf.Max(arm, bArm * 0.65f);
            }
            result = Mathf.Max(result, arm);
        }
        return Mathf.Clamp01(result);
    });

    // 8. Cross — 십자 치유 심볼 (대상 집중 치유)
    static Texture2D GenCross() => BuildTex((cx, cy) =>
    {
        float ax = Mathf.Abs(cx), ay = Mathf.Abs(cy);
        float hw = 0.09f;   // 팔 반폭
        float hl = 0.43f;   // 팔 반길이
        float h  = (ax <= hl && ay <= hw) ? Mathf.Pow(1f - ay / hw, 1.5f) : 0f;  // 수평
        float v  = (ay <= hl && ax <= hw) ? Mathf.Pow(1f - ax / hw, 1.5f) : 0f;  // 수직
        return Mathf.Clamp01(Mathf.Max(h, v));
    });

    // 9. Star burst — 4+4 방향 별빛 (충돌 임팩트, 전투함성)
    static Texture2D GenStar() => BuildTex((cx, cy) =>
    {
        float d     = Mathf.Sqrt(cx * cx + cy * cy);
        float angle = Mathf.Atan2(cy, cx);
        float result = Gauss(d, 0f, 0.075f);  // 중심 빛

        // 4개 주 스파이크 (0°, 90°, 180°, 270°)
        for (int i = 0; i < 4; i++)
        {
            float a  = angle - i * Mathf.PI / 2f;
            float sx = d * Mathf.Cos(a);
            float sy = d * Mathf.Sin(a);
            float sp = Mathf.Max(0f, 1f - (sx / 0.44f) * (sx / 0.44f)
                                        - (sy / 0.035f) * (sy / 0.035f));
            sp *= Mathf.Max(0f, 1f - d / 0.46f);
            result = Mathf.Max(result, sp);
        }
        // 4개 보조 스파이크 (45°, 135°, …) — 주 스파이크보다 짧음
        for (int i = 0; i < 4; i++)
        {
            float a  = angle - (i * Mathf.PI / 2f + Mathf.PI / 4f);
            float sx = d * Mathf.Cos(a);
            float sy = d * Mathf.Sin(a);
            float sp = Mathf.Max(0f, 1f - (sx / 0.26f) * (sx / 0.26f)
                                        - (sy / 0.028f) * (sy / 0.028f));
            sp *= Mathf.Max(0f, 1f - d / 0.28f);
            result = Mathf.Max(result, sp);
        }
        return Mathf.Clamp01(result);
    });

    // 10. Diamond — 마름모 방어막 (아이언 실드)
    static Texture2D GenDiamond() => BuildTex((cx, cy) =>
    {
        float d = (Mathf.Abs(cx) + Mathf.Abs(cy)) / 0.44f;  // L1 거리
        return Mathf.Pow(Mathf.Max(0f, 1f - d), 0.75f);
    });

    // 11. Rune circle — 마법진 룬 (소환)
    static Texture2D GenRune() => BuildTex((cx, cy) =>
    {
        float d     = Mathf.Sqrt(cx * cx + cy * cy) / 0.5f;  // 0~1
        float angle = Mathf.Atan2(cy, cx);

        float outer  = Gauss(d, 0.86f, 0.055f);               // 외곽 링
        float inner  = Gauss(d, 0.56f, 0.04f) * 0.65f;        // 내부 링

        // 6개 방사형 연결선 (내/외 링 사이)
        float lines = 0f;
        for (int i = 0; i < 6; i++)
        {
            float a    = angle - i * Mathf.PI / 3f;
            float perp = d * 0.5f * Mathf.Sin(a);
            bool inZone = d >= 0.52f && d <= 0.90f;
            float lineA = Mathf.Max(0f, 1f - (perp / 0.025f) * (perp / 0.025f)) * (inZone ? 1f : 0f);
            lines = Mathf.Max(lines, lineA * 0.45f);
        }
        float center = Gauss(d * 0.5f, 0f, 0.055f) * 0.55f;  // 중앙 점

        return Mathf.Clamp01(outer + inner + lines + center);
    });

    // 12. Poison drip — 독 방울 (독 안개 파티클)
    static Texture2D GenPoison() => BuildTex((cx, cy) =>
    {
        // 위쪽 원
        float circR  = Mathf.Sqrt(cx * cx + (cy - 0.10f) * (cy - 0.10f)) / 0.33f;
        float circle = Mathf.Max(0f, 1f - circR * circR);
        // 아래 꼬리 물방울
        float ex2  = cx / 0.085f;
        float ey2  = (cy + 0.27f) / 0.185f;
        float drip = Mathf.Max(0f, 1f - ex2 * ex2 - ey2 * ey2);
        return Mathf.Clamp01(circle + drip);
    });

    // ── 텍스처 임포트 설정 ───────────────────────────────────────────────

    static readonly string[] kTexNames =
    {
        "TX_FX_Soft", "TX_FX_Slash", "TX_FX_Spark", "TX_FX_Smoke",
        "TX_FX_Ring", "TX_FX_Flame", "TX_FX_Snowflake", "TX_FX_Cross",
        "TX_FX_Star", "TX_FX_Diamond", "TX_FX_Rune", "TX_FX_Poison",
        "TX_FX_Arrow", "TX_FX_Line",
    };

    // PNG 임포트 후 알파 투명도 설정을 적용한다.
    // 기본 임포트로는 alphaIsTransparency = false → 사각형으로 렌더링됨
    static void ConfigureTextureImports()
    {
        foreach (var name in kTexNames)
        {
            string assetPath = $"{kTexPath}/{name}.png";
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            importer.textureType         = TextureImporterType.Default;
            importer.alphaSource         = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;   // ★ 핵심: 알파를 투명도로 사용
            importer.sRGBTexture         = false;  // 파티클 텍스처는 선형 공간
            importer.mipmapEnabled       = false;
            importer.wrapMode            = TextureWrapMode.Clamp;
            importer.filterMode          = FilterMode.Bilinear;
            importer.maxTextureSize      = 128;
            importer.SaveAndReimport();
        }
    }

    // ── 머티리얼 생성 ────────────────────────────────────────────────────

    static void MakeMat(string matName, string texName, bool additive)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{kTexPath}/{texName}.png");
        if (tex == null)
        {
            Debug.LogWarning($"[EffectTextureGenerator] 텍스처를 찾을 수 없음: {texName}.png");
            return;
        }

        // 셰이더 선택: URP → 빌트인 레거시 순으로 폴백
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? (additive
                         ? Shader.Find("Legacy Shaders/Particles/Additive")
                           ?? Shader.Find("Particles/Additive")
                         : Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                           ?? Shader.Find("Particles/Alpha Blended"));

        Material mat;
        if (shader != null)
        {
            mat = new Material(shader) { mainTexture = tex };
            ApplyBlending(mat, additive);
        }
        else
        {
            // 최후 폴백: Default-Particle 복사
            var baseMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            mat = new Material(baseMat) { mainTexture = tex };
            if (additive)
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_ZWrite",   0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }

        string path     = $"{kMatPath}/{matName}.mat";
        var    existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            // 기존 머티리얼 갱신 (GUID 유지) — 블렌딩 설정도 모두 재적용
            existing.shader      = mat.shader;
            existing.mainTexture = tex;
            ApplyBlending(existing, additive);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mat);
        }
        else
        {
            AssetDatabase.CreateAsset(mat, path);
        }
    }

    // 렌더 파이프라인에 맞게 투명도·블렌딩 프로퍼티를 설정한다.
    static void ApplyBlending(Material mat, bool additive)
    {
        bool isUrp = mat.shader.name.Contains("Universal Render Pipeline");

        if (isUrp)
        {
            // URP Particles/Unlit 올바른 프로퍼티
            // _Surface: 0=Opaque, 1=Transparent
            // _Blend  : 0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply
            mat.SetFloat("_Surface", 1f);                    // Transparent
            mat.SetFloat("_Blend",   additive ? 2f : 0f);   // Additive or Alpha
            mat.SetFloat("_ZWrite",  0f);
            mat.SetFloat("_ZWriteControl", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", additive
                ? (int)UnityEngine.Rendering.BlendMode.One
                : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlendAlpha", additive
                ? (int)UnityEngine.Rendering.BlendMode.One
                : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) mat.EnableKeyword("_BLENDMODE_ADD");
            else          mat.DisableKeyword("_BLENDMODE_ADD");
        }
        else
        {
            // 빌트인 레거시 파티클 셰이더
            // Additive 셰이더는 자체적으로 블렌드를 처리하므로 별도 설정 불필요.
            // Alpha Blended 셰이더도 동일.
        }

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;
    }

    // 13. Arrow needle — 세로로 긴 화살 실루엣 (ArrowRainZone Stretch 렌더)
    //     Stretch 렌더모드에서 속도 방향으로 늘어나므로 가로는 매우 좁게
    static Texture2D GenArrow() => BuildTex((cx, cy) =>
    {
        // 세로 길이 0.44, 가로 폭 0.035 — 끝이 뾰족한 바늘 모양
        float tip = Mathf.Max(0f, cy + 0.1f);     // 위로 갈수록 더 좁아짐
        float ex  = cx / 0.035f;
        float ey  = cy / 0.44f;
        float d   = Mathf.Sqrt(ex * ex + ey * ey);
        return Mathf.Max(0f, 1f - d * (1f + tip * 2.5f));
    });

    // 14. Line streak — 얇은 가로 섬광 선 (Berserk 에너지 라인 / 충격파 크랙)
    static Texture2D GenLine() => BuildTex((cx, cy) =>
    {
        // 가로로 긴 섬광 선: 폭 0.45, 높이 0.025, 중심에서 가장자리로 페이드
        float a  = Mathf.Max(0f, 1f - (cx / 0.45f) * (cx / 0.45f)
                                     - (cy / 0.025f) * (cy / 0.025f));
        // 중심 코어 강조
        float a2 = Mathf.Max(0f, 1f - (cx / 0.45f) * (cx / 0.45f)
                                     - (cy / 0.008f) * (cy / 0.008f));
        return Mathf.Clamp01(a + a2 * 0.7f);
    });

    // ── 경로 헬퍼 ────────────────────────────────────────────────────────

    static void EnsureDir(string assetPath)
    {
        string full = Path.Combine(
            Application.dataPath.Replace("Assets", ""), assetPath);
        Directory.CreateDirectory(full);
    }
}
