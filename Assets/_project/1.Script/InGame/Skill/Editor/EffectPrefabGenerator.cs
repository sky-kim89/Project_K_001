using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EffectPrefabGenerator.cs
//  Unity 메뉴 BattleGame > Generate Effect Prefabs 실행 시
//  22개 액티브 스킬 이펙트 프리팹을 자동 생성한다.
//
//  저장 경로: Assets/_project/2.Prefabs/Effect/
//  파티클 머티리얼: EffectTextureGenerator 가 생성한 MAT_FX_* 사용
//
//  ■ 사용법
//    1. BattleGame → Generate Effect Textures & Materials  ← 반드시 먼저
//    2. BattleGame → Generate Effect Prefabs
//    3. Console 에 "✓ 22 effect prefabs generated." 확인
//    4. PoolController → Effect Pool 에 생성된 프리팹 등록
//    5. 각 ActiveSkillData SO 에 FX_ 키 입력
// ============================================================

public static class EffectPrefabGenerator
{
    const string kSavePath = "Assets/_project/2.Prefabs/Effect";
    const string kMatPath  = "Assets/_project/4.Materials/FX";

    // 이펙트 키 → [루트 머티리얼, 자식1 머티리얼, ...] 매핑
    // GetComponentsInChildren 순서(루트 → 자식 DFS)와 일치
    static readonly Dictionary<string, string[]> kEffectMaterials =
        new Dictionary<string, string[]>
    {
        { "FX_Slash_Impact",     new[] { "MAT_FX_Slash_Add",    "MAT_FX_Soft_Add"                       } },
        { "FX_Leap_Land",        new[] { "MAT_FX_Ring_Add",      "MAT_FX_Smoke_Alpha"                    } },
        { "FX_Dust_Dash",        new[] { "MAT_FX_Smoke_Alpha"                                            } },
        { "FX_Shockwave",        new[] { "MAT_FX_Spark_Add",     "MAT_FX_Spark_Add"                      } },
        { "FX_Meteor_Warning",   new[] { "MAT_FX_Flame_Add"                                              } },
        { "FX_Meteor_Explosion", new[] { "MAT_FX_Flame_Add",     "MAT_FX_Smoke_Alpha",  "MAT_FX_Ring_Add" } },
        { "FX_Arrow_Volley",     new[] { "MAT_FX_Spark_Add"                                              } },
        { "FX_Arrow_Rain_Zone",  new[] { "MAT_FX_Arrow_Add",     "MAT_FX_Star_Add",     "MAT_FX_Soft_Add" } },
        { "FX_Charge_Impact",    new[] { "MAT_FX_Star_Add"                              } },
        { "FX_Explosion",        new[] { "MAT_FX_Flame_Add",     "MAT_FX_Spark_Add"     } },
        { "FX_Summon_Circle",    new[] { "MAT_FX_Rune_Add",      "MAT_FX_Soft_Add"      } },
        { "FX_Sacrifice",        new[] { "MAT_FX_Soft_Add"                              } },
        { "FX_Absorb",           new[] { "MAT_FX_Soft_Add"                              } },
        { "FX_Battle_Cry",       new[] { "MAT_FX_Star_Add"                              } },
        { "FX_Berserk",          new[] { "MAT_FX_Flame_Add",    "MAT_FX_Line_Add"                       } },
        { "FX_Shield_Up",        new[] { "MAT_FX_Diamond_Add"                                            } },
        { "FX_Speed_Up",         new[] { "MAT_FX_Spark_Add"                                              } },
        { "FX_Heal_Aura",        new[] { "MAT_FX_Soft_Add"                                               } },
        { "FX_Heal_Target",      new[] { "MAT_FX_Cross_Add"                                              } },
        { "FX_Bind",             new[] { "MAT_FX_Ring_Add"                                               } },
        { "FX_Poison_Zone",      new[] { "MAT_FX_Smoke_Alpha",  "MAT_FX_Ring_Add"                        } },
        { "FX_Blizzard",         new[] { "MAT_FX_Snowflake_Add", "MAT_FX_Smoke_Alpha"   } },
    };

    // ── 공개 진입점 ─────────────────────────────────────────

    [MenuItem("BattleGame/Generate Effect Prefabs")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath,
            "_project/2.Prefabs/Effect"));
        AssetDatabase.Refresh();

        int count = 0;
        count += Save("FX_Slash_Impact",      BuildSlashImpact());
        count += Save("FX_Leap_Land",          BuildLeapLand());
        count += Save("FX_Dust_Dash",          BuildDustDash());
        count += Save("FX_Shockwave",          BuildShockwave());
        count += Save("FX_Meteor_Warning",     BuildMeteorWarning());
        count += Save("FX_Meteor_Explosion",   BuildMeteorExplosion());
        count += Save("FX_Arrow_Volley",       BuildArrowVolley());
        count += Save("FX_Arrow_Rain_Zone",    BuildArrowRainZone());
        count += Save("FX_Charge_Impact",      BuildChargeImpact());
        count += Save("FX_Explosion",          BuildExplosion());
        count += Save("FX_Summon_Circle",      BuildSummonCircle());
        count += Save("FX_Sacrifice",          BuildSacrifice());
        count += Save("FX_Absorb",             BuildAbsorb());
        count += Save("FX_Battle_Cry",         BuildBattleCry());
        count += Save("FX_Berserk",            BuildBerserk());
        count += Save("FX_Shield_Up",          BuildShieldUp());
        count += Save("FX_Speed_Up",           BuildSpeedUp());
        count += Save("FX_Heal_Aura",          BuildHealAura());
        count += Save("FX_Heal_Target",        BuildHealTarget());
        count += Save("FX_Bind",               BuildBind());
        count += Save("FX_Poison_Zone",        BuildPoisonZone());
        count += Save("FX_Blizzard",           BuildBlizzard());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EffectPrefabGenerator] ✓ {count} effect prefabs generated → {kSavePath}");
    }

    // ── 저장 헬퍼 ───────────────────────────────────────────

    static int Save(string fxKey, GameObject go)
    {
        go.name = fxKey;
        string path = $"{kSavePath}/{fxKey}.prefab";

        // 이펙트에 맞는 머티리얼 적용 (텍스처 생성 후 실행 필요)
        if (kEffectMaterials.TryGetValue(fxKey, out var matNames))
            ApplyMaterials(go, matNames);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return 1;
    }

    // 루트 GO 의 ParticleSystemRenderer 를 DFS 순서로 순회하며 머티리얼 할당
    static void ApplyMaterials(GameObject root, string[] matNames)
    {
        var renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < Mathf.Min(renderers.Length, matNames.Length); i++)
        {
            string p   = $"{kMatPath}/{matNames[i]}.mat";
            var    mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat != null)
                renderers[i].material = mat;
            else
                Debug.LogWarning(
                    $"[EffectPrefabGenerator] 머티리얼 없음: {p}\n" +
                    "→ 먼저 'BattleGame/Generate Effect Textures & Materials' 를 실행하세요.");
        }
    }

    // ── 공통 헬퍼 ───────────────────────────────────────────

    static GameObject NewGO(string name = "FX")
    {
        var go = new GameObject(name);
        return go;
    }

    /// <summary>GO에 ParticleSystem을 추가하고 기본 Renderer 설정 후 반환한다.</summary>
    static ParticleSystem AddPS(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var rd = go.GetComponent<ParticleSystemRenderer>();
        rd.renderMode       = ParticleSystemRenderMode.Billboard;
        rd.sortingLayerName = "Effect";          // 없으면 Default 로 fallback
        rd.sortingOrder     = 5;

        // 빌트인 Default-Particle 머티리얼 할당 (없으면 핑크색으로 표시됨)
        var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        if (mat != null) rd.material = mat;

        return ps;
    }

    static Color32 C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

    // ─────────────────────────────────────────────────────────────────
    // ① FX_Slash_Impact  — 칼날 충격 섬광
    //    HeavyStrike Caster/Target · LeapStrike Target
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildSlashImpact()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.3f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(4f, 11f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.38f, 0.90f);   // +30%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,255,220), C(255,100,20));
        main.startRotation      = new ParticleSystem.MinMaxCurve(-60f * Mathf.Deg2Rad, 60f * Mathf.Deg2Rad);
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 26;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Cone;
        sh.angle        = 40f;
        sh.radius       = 0.05f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var grad        = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white,           0f),
                    new GradientColorKey(new Color(1f,0.6f,0f), 0.4f),
                    new GradientColorKey(new Color(1f,0.1f,0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.4f),
                    new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(grad);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,1f), new Keyframe(0.4f,1.3f), new Keyframe(1f,0f)));

        // Layer 2: 흰 코어 플래시 — 순간적인 강렬한 백색 점멸
        var coreGO = new GameObject("CoreFlash");
        coreGO.transform.SetParent(go.transform, false);
        var psCore = AddPS(coreGO);
        {
            var m = psCore.main;
            m.duration        = 0.12f;
            m.loop            = false;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(0f, 1.5f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(255,255,255), C(255,230,160));
            m.gravityModifier = 0f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 4;

            var e = psCore.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

            var s = psCore.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Sphere;
            s.radius    = 0.05f;

            var c = psCore.colorOverLifetime;
            c.enabled   = true;
            var gCore   = new Gradient();
            gCore.SetKeys(
                new[] { new GradientColorKey(Color.white,             0f),
                        new GradientColorKey(new Color(1f,0.9f,0.5f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            c.color = new ParticleSystem.MinMaxGradient(gCore);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ② FX_Leap_Land  — 착지 충격파 (LeapStrike Caster)
    //    슬래시 효과 통합 포함
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildLeapLand()
    {
        var go = NewGO();

        // Layer 1: 방사형 충격파 링
        var ps1 = AddPS(go);
        {
            var main     = ps1.main;
            main.duration        = 0.5f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(6f, 15f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.25f, 0.63f);  // +25%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(210,235,255), C(80,170,255));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 38;

            var em       = ps1.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var sh       = ps1.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.1f;

            var col      = ps1.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white,              0f),
                        new GradientColorKey(new Color(0.3f,0.7f,1f),  0.5f),
                        new GradientColorKey(new Color(0.1f,0.4f,1f),  1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f),
                        new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);

            var sz       = ps1.sizeOverLifetime;
            sz.enabled   = true;
            sz.size      = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.5f), new Keyframe(0.3f,1.2f), new Keyframe(1f,0f)));
        }

        // Layer 2: 먼지/흙 파편
        var child2 = new GameObject("Dust");
        child2.transform.SetParent(go.transform, false);
        var ps2 = AddPS(child2);
        {
            var main     = ps2.main;
            main.duration        = 0.5f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.13f, 0.38f);  // +25%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(200,175,130), C(240,215,165));
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 20;

            var em       = ps2.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            var sh       = ps2.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.3f;

            var col      = ps2.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.8f,0.7f,0.5f), 0f),
                        new GradientColorKey(new Color(0.6f,0.5f,0.3f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ③ FX_Dust_Dash  — 돌진·도약 출발 먼지
    //    HeavyStrike/LeapStrike/ChargeSoldier Base
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildDustDash()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.4f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.19f, 0.44f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(220,200,155), C(255,235,185));
        main.gravityModifier    = 0.3f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 25;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Cone;
        sh.angle        = 70f;
        sh.radius       = 0.2f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.9f,0.85f,0.7f), 0f),
                    new GradientColorKey(new Color(0.7f,0.65f,0.5f), 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.2f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ④ FX_Shockwave  — 전방 부채꼴 충격파
    //    Shockwave Caster
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildShockwave()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.5f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(7f, 17f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.25f, 0.63f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(210,245,255), C(50,170,255));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 50;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 38) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Cone;
        sh.angle        = 45f;
        sh.radius       = 0.1f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,             0f),
                    new GradientColorKey(new Color(0.2f,0.65f,1f),0.4f),
                    new GradientColorKey(new Color(0f,0.3f,1f),   1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,0.4f), new Keyframe(0.5f,1f), new Keyframe(1f,0f)));

        // Layer 2: 크랙 스파크 — 충격파 끝자락에 흩어지는 전기 파편
        var crackGO = new GameObject("CrackSparks");
        crackGO.transform.SetParent(go.transform, false);
        var psCrack = AddPS(crackGO);
        {
            var m = psCrack.main;
            m.duration        = 0.4f;
            m.loop            = false;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(10f, 22f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(255,255,255), C(150,220,255));
            m.gravityModifier = 0f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 30;

            var e = psCrack.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 20) });

            var s = psCrack.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Cone;
            s.angle     = 50f;
            s.radius    = 0.5f;

            var c = psCrack.colorOverLifetime;
            c.enabled = true;
            var gCrack = new Gradient();
            gCrack.SetKeys(
                new[] { new GradientColorKey(Color.white,              0f),
                        new GradientColorKey(new Color(0.3f,0.7f,1f),  0.5f),
                        new GradientColorKey(new Color(0f,0.2f,0.8f),  1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
            c.color = new ParticleSystem.MinMaxGradient(gCrack);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑤ FX_Meteor_Warning  — 착탄 경고 마커
    //    Meteor Base  (EffectDespawnDelay = delay + 여유)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildMeteorWarning()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 3f;      // SO의 EffectDespawnDelay 로 실제 despawn 제어
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0f, 0.5f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.38f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,60,10), C(255,210,30));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 75;

        var em          = ps.emission;
        em.rateOverTime = 25f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 1.5f;
        sh.radiusThickness = 0.1f;         // 원 테두리에만 스폰

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f,0.8f,0.1f), 0f),
                    new GradientColorKey(new Color(1f,0.2f,0f),  0.6f),
                    new GradientColorKey(new Color(0.4f,0f,0f),  1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑥ FX_Meteor_Explosion  — 메테오 착탄 폭발
    //    Meteor Target
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildMeteorExplosion()
    {
        var go = NewGO();

        // Layer 1: 핵심 폭발
        var ps1 = AddPS(go);
        {
            var main     = ps1.main;
            main.duration        = 0.6f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(5f, 16f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startColor      = new ParticleSystem.MinMaxGradient(C(255,220,80), C(255,80,20));
            main.gravityModifier = -0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 60;

            var em       = ps1.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var sh       = ps1.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius    = 0.2f;

            var col      = ps1.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f,0.95f,0.5f), 0f),
                        new GradientColorKey(new Color(1f,0.4f,0f),   0.4f),
                        new GradientColorKey(new Color(0.3f,0.1f,0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);

            var sz       = ps1.sizeOverLifetime;
            sz.enabled   = true;
            sz.size      = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.2f,1.2f), new Keyframe(1f,0f)));
        }

        // Layer 2: 연기
        var child2 = new GameObject("Smoke");
        child2.transform.SetParent(go.transform, false);
        var ps2 = AddPS(child2);
        {
            var main     = ps2.main;
            main.duration        = 0.6f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);   // +20%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(90,78,65,200), C(55,50,43,180));
            main.gravityModifier = -0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 24;

            var em       = ps2.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 16) });

            var sh       = ps2.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius    = 0.5f;

            var col      = ps2.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.45f,0.38f,0.3f), 0f),
                        new GradientColorKey(new Color(0.2f,0.17f,0.13f), 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);

            var sz       = ps2.sizeOverLifetime;
            sz.enabled   = true;
            sz.size      = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.5f), new Keyframe(0.5f,1.5f), new Keyframe(1f,2f)));
        }

        // Layer 3: 충격파 링 — 착탄 순간 방사형으로 퍼지는 링
        var child3 = new GameObject("ShockRing");
        child3.transform.SetParent(go.transform, false);
        var ps3 = AddPS(child3);
        {
            var m = ps3.main;
            m.duration        = 0.3f;
            m.loop            = false;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(8f, 18f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(255,200,80), C(255,120,20));
            m.gravityModifier = 0f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 30;

            var e = ps3.emission;
            e.rateOverTime = 0f;
            e.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var s = ps3.shape;
            s.enabled          = true;
            s.shapeType        = ParticleSystemShapeType.Circle;
            s.radius           = 0.1f;
            s.radiusThickness  = 0f;   // 링 외곽에서만 스폰

            var c = ps3.colorOverLifetime;
            c.enabled = true;
            var gRing = new Gradient();
            gRing.SetKeys(
                new[] { new GradientColorKey(new Color(1f,0.9f,0.5f), 0f),
                        new GradientColorKey(new Color(1f,0.5f,0f),   0.4f),
                        new GradientColorKey(new Color(0.6f,0.1f,0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            c.color = new ParticleSystem.MinMaxGradient(gRing);

            var sz = ps3.sizeOverLifetime;
            sz.enabled = true;
            sz.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.3f,1.2f), new Keyframe(1f,0f)));
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑦ FX_Arrow_Volley  — 일제 사격 발사
    //    VolleyFire Caster
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildArrowVolley()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.3f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(10f, 22f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.32f);  // +28%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,245,180), C(255,190,30));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 26;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Cone;
        sh.angle        = 25f;
        sh.radius       = 0.05f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,              0f),
                    new GradientColorKey(new Color(1f,0.85f,0.2f), 0.3f),
                    new GradientColorKey(new Color(1f,0.55f,0f),   1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑧ FX_Arrow_Rain_Zone  — 화살비 낙하 영역  (3레이어)
    //    ArrowRain Base  (지속 재생)
    //    Layer 1 (root)  : Stretch 렌더 — 낙하하는 화살 실루엣
    //    Layer 2 Impact  : 지면 착탄 스파크 버스트
    //    Layer 3 AoeGlow : 영역 표시 글로우
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildArrowRainZone()
    {
        var go = NewGO();

        // ── Layer 1: 낙하 화살 (Stretch renderMode로 화살 실루엣 구현)
        var ps1 = AddPS(go);
        {
            var rd = go.GetComponent<ParticleSystemRenderer>();
            rd.renderMode       = ParticleSystemRenderMode.Stretch;
            rd.velocityScale    = 0.12f;   // 속도에 비례한 늘임 정도
            rd.lengthScale      = 1.8f;    // 추가 길이 배율
            rd.sortingLayerName = "Effect";
            rd.sortingOrder     = 5;

            var m = ps1.main;
            m.duration        = 5f;
            m.loop            = true;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(10f, 16f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(230,200,100), C(180,145,60));
            m.gravityModifier = 2.0f;
            m.startRotation   = new ParticleSystem.MinMaxCurve(170f * Mathf.Deg2Rad, 190f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 100;

            var e = ps1.emission;
            e.rateOverTime = 35f;

            var s = ps1.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius    = 2f;

            // 위에서 아래로 낙하하도록 시작 위치를 위로 오프셋
            go.transform.localPosition = new Vector3(0f, 3.5f, 0f);

            var col = ps1.colorOverLifetime;
            col.enabled = true;
            var g1 = new Gradient();
            g1.SetKeys(
                new[] { new GradientColorKey(new Color(1f,0.92f,0.5f), 0f),
                        new GradientColorKey(new Color(0.75f,0.58f,0.2f), 0.7f),
                        new GradientColorKey(new Color(0.5f,0.35f,0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g1);
        }

        // ── Layer 2: 착탄 스파크 — 화살이 지면에 꽂히는 임팩트 이펙트
        var impactGO = new GameObject("Impact");
        impactGO.transform.SetParent(go.transform, false);
        impactGO.transform.localPosition = new Vector3(0f, -3.5f, 0f);  // 지면 높이
        var ps2 = AddPS(impactGO);
        {
            var m = ps2.main;
            m.duration        = 5f;
            m.loop            = true;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(255,245,180), C(255,180,50));
            m.gravityModifier = 0.8f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 80;

            var e = ps2.emission;
            e.rateOverTime = 25f;

            var s = ps2.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius    = 2f;

            var col = ps2.colorOverLifetime;
            col.enabled = true;
            var g2 = new Gradient();
            g2.SetKeys(
                new[] { new GradientColorKey(Color.white,             0f),
                        new GradientColorKey(new Color(1f,0.7f,0.1f), 0.4f),
                        new GradientColorKey(new Color(0.6f,0.3f,0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g2);
        }

        // ── Layer 3: AoeGlow — 영역을 표시하는 지면 글로우
        var glowGO = new GameObject("AoeGlow");
        glowGO.transform.SetParent(go.transform, false);
        glowGO.transform.localPosition = new Vector3(0f, -3.5f, 0f);
        var ps3 = AddPS(glowGO);
        {
            var m = ps3.main;
            m.duration        = 5f;
            m.loop            = true;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(0f, 0.3f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.2f, 0.4f),
                new Color(1f, 0.6f, 0.1f, 0.25f));
            m.gravityModifier = 0f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 40;

            var e = ps3.emission;
            e.rateOverTime = 12f;

            var s = ps3.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius    = 2f;

            var col = ps3.colorOverLifetime;
            col.enabled = true;
            var g3 = new Gradient();
            g3.SetKeys(
                new[] { new GradientColorKey(new Color(1f,0.9f,0.4f), 0f),
                        new GradientColorKey(new Color(1f,0.65f,0.1f),0.5f),
                        new GradientColorKey(new Color(0.8f,0.4f,0f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.5f, 0.2f),
                        new GradientAlphaKey(0.35f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g3);

            var sz = ps3.sizeOverLifetime;
            sz.enabled = true;
            sz.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.4f,1f), new Keyframe(1f,1.3f)));
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑨ FX_Charge_Impact  — 병사 충돌 임팩트
    //    ChargeSoldier Target
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildChargeImpact()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.4f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(5f, 13f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.19f, 0.50f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,215,120), C(255,140,20));
        main.gravityModifier    = 0.2f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 32;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Cone;
        sh.angle        = 55f;
        sh.radius       = 0.15f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,             0f),
                    new GradientColorKey(new Color(1f,0.7f,0.2f), 0.4f),
                    new GradientColorKey(new Color(0.8f,0.4f,0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1.1f), new Keyframe(1f, 0f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑩ FX_Explosion  — 자폭 착탄 폭발
    //    SuicideSoldier Target
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildExplosion()
    {
        var go = NewGO();

        // Layer 1: 주 폭발
        var ps1 = AddPS(go);
        {
            var main     = ps1.main;
            main.duration        = 0.5f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(5f, 17f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.38f, 1.25f);  // +25%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(255,240,80), C(255,60,5));
            main.gravityModifier = -0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 62;

            var em       = ps1.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 44) });

            var sh       = ps1.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius    = 0.15f;

            var col      = ps1.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f,0.95f,0.5f), 0f),
                        new GradientColorKey(new Color(1f,0.25f,0f),   0.3f),
                        new GradientColorKey(new Color(0.15f,0.05f,0f),1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);

            var sz       = ps1.sizeOverLifetime;
            sz.enabled   = true;
            sz.size      = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.2f), new Keyframe(0.15f,1.3f), new Keyframe(1f,0f)));
        }

        // Layer 2: 불꽃 파편
        var child2 = new GameObject("Sparks");
        child2.transform.SetParent(go.transform, false);
        var ps2 = AddPS(child2);
        {
            var main     = ps2.main;
            main.duration        = 0.5f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(7f, 22f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.19f);  // +25%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(255,255,180), C(255,110,0));
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 40;

            var em       = ps2.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var sh       = ps2.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius    = 0.1f;

            var col      = ps2.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white,          0f),
                        new GradientColorKey(new Color(1f,0.5f,0f),0.5f),
                        new GradientColorKey(new Color(0.5f,0.1f,0f),1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑪ FX_Summon_Circle  — 소환진 마법진
    //    SummonSkeleton/SummonElite Base
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildSummonCircle()
    {
        var go = NewGO();

        // Layer 1: 링 파티클
        var ps1 = AddPS(go);
        {
            var main     = ps1.main;
            main.duration        = 1.0f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0f, 1.5f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.13f, 0.32f);  // +28%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(200,120,255), C(110,50,255));
            main.gravityModifier = -0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 52;

            var em       = ps1.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] {
                new ParticleSystem.Burst(0f,   26),
                new ParticleSystem.Burst(0.3f, 18),
            });

            var sh       = ps1.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.8f;
            sh.radiusThickness = 0.15f;

            var col      = ps1.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white,              0f),
                        new GradientColorKey(new Color(0.9f,0.3f,1f),  0.3f),
                        new GradientColorKey(new Color(0.3f,0f,0.9f),  1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);
        }

        // Layer 2: 중앙에서 위로 솟아오르는 마나 파편
        var child2 = new GameObject("Rise");
        child2.transform.SetParent(go.transform, false);
        var ps2 = AddPS(child2);
        {
            var main     = ps2.main;
            main.duration        = 1.0f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.10f, 0.26f);  // +28%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(245,190,255), C(195,60,255));
            main.gravityModifier = -1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 25;

            var em       = ps2.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.1f, 20) });

            var sh       = ps2.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 0.6f;

            var col      = ps2.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white,               0f),
                        new GradientColorKey(new Color(0.9f,0.6f,1f),   0.4f),
                        new GradientColorKey(new Color(0.5f,0.1f,0.9f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑫ FX_Sacrifice  — 희생 병사 사망
    //    SacrificeSoldier Target
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildSacrifice()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.5f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(3f, 9f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.19f, 0.50f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,40,40), C(160,5,5));
        main.gravityModifier    = -0.3f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 38;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Sphere;
        sh.radius       = 0.2f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f,0.85f,0.85f), 0f),
                    new GradientColorKey(new Color(1f,0.05f,0.05f), 0.25f),
                    new GradientColorKey(new Color(0.4f,0f,0f),     1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,0.5f), new Keyframe(0.2f,1.2f), new Keyframe(1f,0f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑬ FX_Absorb  — 시전자 공격력 흡수
    //    SacrificeSoldier Caster
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildAbsorb()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.8f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(-6f, -2f);   // 음수 = 안쪽으로 수렴
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.38f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,215,40), C(255,130,10));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 44;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Sphere;
        sh.radius       = 1.5f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f,0.95f,0.2f), 0f),
                    new GradientColorKey(new Color(1f,0.55f,0f),   0.5f),
                    new GradientColorKey(Color.white,              1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,1f), new Keyframe(0.8f,0.4f), new Keyframe(1f,0f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑭ FX_Battle_Cry  — 전투 함성 오라
    //    BattleCry Caster
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildBattleCry()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.8f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(4f, 11f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.19f, 0.50f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,235,60), C(255,130,5));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 64;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] {
            new ParticleSystem.Burst(0f,   38),
            new ParticleSystem.Burst(0.15f,25),
        });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 0.2f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,              0f),
                    new GradientColorKey(new Color(1f,0.85f,0.1f), 0.25f),
                    new GradientColorKey(new Color(1f,0.45f,0f),   1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.4f,1.1f), new Keyframe(1f,0f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑮ FX_Berserk  — 분노 오라 (Berserker Caster)
    //    Layer 1 (root)   : 불꽃 파티클 오라
    //    Layer 2 EnergyLine: 붉은 에너지 섬광 라인
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildBerserk()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 1.5f;
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.32f);  // +28%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(255,30,10), C(220,10,0));
        main.gravityModifier    = -0.3f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 52;

        var em          = ps.emission;
        em.rateOverTime = 26f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 0.4f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f,0.9f,0.1f), 0f),
                    new GradientColorKey(new Color(1f,0.15f,0f),  0.35f),
                    new GradientColorKey(new Color(0.6f,0f,0f),   1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        // Layer 2: 붉은 에너지 라인 — 짧은 섬광이 사방으로 번쩍임
        var lineGO = new GameObject("EnergyLines");
        lineGO.transform.SetParent(go.transform, false);
        var psLine = AddPS(lineGO);
        {
            var m = psLine.main;
            m.duration        = 1.5f;
            m.loop            = true;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(255,200,180), C(255,40,10));
            m.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = 0f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 25;

            var e = psLine.emission;
            e.rateOverTime = 18f;

            var s = psLine.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius    = 0.35f;

            var c = psLine.colorOverLifetime;
            c.enabled = true;
            var gLine = new Gradient();
            gLine.SetKeys(
                new[] { new GradientColorKey(Color.white,           0f),
                        new GradientColorKey(new Color(1f,0.3f,0f), 0.3f),
                        new GradientColorKey(new Color(0.7f,0f,0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
            c.color = new ParticleSystem.MinMaxGradient(gLine);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑯ FX_Shield_Up  — 방어막 생성 (IronShield Caster)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildShieldUp()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 1.2f;
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.38f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(160,215,255), C(40,140,255));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 52;

        var em          = ps.emission;
        em.rateOverTime = 24f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 0.6f;
        sh.radiusThickness = 0.3f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,              0f),
                    new GradientColorKey(new Color(0.4f,0.75f,1f), 0.35f),
                    new GradientColorKey(new Color(0.15f,0.4f,1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.4f,1f), new Keyframe(1f,0.5f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑰ FX_Speed_Up  — 속도 오라 / 바람 잔상 (SwiftStrike Caster)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildSpeedUp()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 1.0f;
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.06f, 0.25f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(120,255,200), C(20,200,255));
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 65;

        var em          = ps.emission;
        em.rateOverTime = 38f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 0.3f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,               0f),
                    new GradientColorKey(new Color(0.2f,1f,0.75f),  0.25f),
                    new GradientColorKey(new Color(0f,0.6f,1f),     1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑱ FX_Heal_Aura  — 치유 오라 (HealAura Caster)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildHealAura()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 2.0f;
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.38f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(80,255,120), C(20,220,60));
        main.gravityModifier    = -0.8f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 76;

        var em          = ps.emission;
        em.rateOverTime = 26f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 1.8f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,              0f),
                    new GradientColorKey(new Color(0.2f,1f,0.4f),  0.25f),
                    new GradientColorKey(new Color(0f,0.85f,0.15f),1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.65f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,0.4f), new Keyframe(0.5f,1f), new Keyframe(1f,0.3f)));

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑲ FX_Heal_Target  — 집중 치유 (TargetHeal Target)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildHealTarget()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 0.8f;
        main.loop               = false;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(-2f, -0.5f);  // 안쪽 수렴 후 위로
        main.startSize          = new ParticleSystem.MinMaxCurve(0.13f, 0.32f);  // +28%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(150,255,160), C(50,255,80));
        main.gravityModifier    = -1.2f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 38;

        var em          = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] {
            new ParticleSystem.Burst(0f,   19),
            new ParticleSystem.Burst(0.2f, 13),
        });

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 0.8f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,              0f),
                    new GradientColorKey(new Color(0.3f,1f,0.5f),  0.25f),
                    new GradientColorKey(new Color(0.1f,0.95f,0.25f),1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ⑳ FX_Bind  — 속박 (Bind Target)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildBind()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 2.0f;
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.10f, 0.25f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(C(60,80,255), C(20,40,220));
        main.startRotation      = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier    = 0f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 52;

        var em          = ps.emission;
        em.rateOverTime = 20f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 0.5f;
        sh.radiusThickness = 0.2f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white,              0f),
                    new GradientColorKey(new Color(0.3f,0.4f,1f),  0.25f),
                    new GradientColorKey(new Color(0f,0.1f,0.8f),  1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.4f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var rot         = ps.rotationOverLifetime;
        rot.enabled     = true;
        rot.z           = new ParticleSystem.MinMaxCurve(90f * Mathf.Deg2Rad);

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ㉑ FX_Poison_Zone  — 독 안개 영역 (PoisonZone Base)
    //    Layer 1 (root) : 독 안개 연기
    //    Layer 2 Bubble : 독 버블 팝 링
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildPoisonZone()
    {
        var go = NewGO();
        var ps = AddPS(go);

        var main        = ps.main;
        main.duration           = 5f;
        main.loop               = true;
        main.startLifetime      = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed         = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
        main.startSize          = new ParticleSystem.MinMaxCurve(0.63f, 1.88f);  // +25%
        main.startColor         = new ParticleSystem.MinMaxGradient(
            new Color(0.15f, 0.85f, 0.05f, 0.55f),
            new Color(0.05f, 0.6f,  0.02f, 0.45f));
        main.gravityModifier    = -0.1f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 50;

        var em          = ps.emission;
        em.rateOverTime = 10f;

        var sh          = ps.shape;
        sh.enabled      = true;
        sh.shapeType    = ParticleSystemShapeType.Circle;
        sh.radius       = 1.5f;

        var col         = ps.colorOverLifetime;
        col.enabled     = true;
        var g           = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.2f,1f,0.05f),  0f),
                    new GradientColorKey(new Color(0.1f,0.7f,0.03f), 0.5f),
                    new GradientColorKey(new Color(0.05f,0.45f,0f),  1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.3f),
                    new GradientAlphaKey(0.45f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color       = new ParticleSystem.MinMaxGradient(g);

        var sz          = ps.sizeOverLifetime;
        sz.enabled      = true;
        sz.size         = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.5f,1f), new Keyframe(1f,1.5f)));

        // Layer 2: 독 버블 팝 — 주기적으로 터지는 링 파티클
        var bubbleGO = new GameObject("BubblePop");
        bubbleGO.transform.SetParent(go.transform, false);
        var psBubble = AddPS(bubbleGO);
        {
            var m = psBubble.main;
            m.duration        = 5f;
            m.loop            = true;
            m.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            m.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            m.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            m.startColor      = new ParticleSystem.MinMaxGradient(C(160,255,80), C(80,220,20));
            m.gravityModifier = -0.2f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles    = 30;

            var e = psBubble.emission;
            e.rateOverTime = 8f;

            var s = psBubble.shape;
            s.enabled   = true;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius    = 1.4f;

            var c = psBubble.colorOverLifetime;
            c.enabled = true;
            var gBub  = new Gradient();
            gBub.SetKeys(
                new[] { new GradientColorKey(new Color(0.7f,1f,0.3f), 0f),
                        new GradientColorKey(new Color(0.3f,0.9f,0.1f),0.5f),
                        new GradientColorKey(new Color(0.1f,0.5f,0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            c.color = new ParticleSystem.MinMaxGradient(gBub);

            var sz2 = psBubble.sizeOverLifetime;
            sz2.enabled = true;
            sz2.size    = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.2f), new Keyframe(0.4f,1.3f), new Keyframe(1f,0f)));
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    // ㉒ FX_Blizzard  — 눈보라 영역 (Blizzard Base)
    // ─────────────────────────────────────────────────────────────────
    static GameObject BuildBlizzard()
    {
        var go = NewGO();

        // Layer 1: 눈송이
        var ps1 = AddPS(go);
        {
            var main     = ps1.main;
            main.duration        = 5f;
            main.loop            = true;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.23f);  // +25%
            main.startColor      = new ParticleSystem.MinMaxGradient(C(210,240,255), C(160,205,255));
            main.gravityModifier = 0.3f;
            main.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 100;

            var em       = ps1.emission;
            em.rateOverTime = 38f;

            var sh       = ps1.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 2f;

            // 위에서 아래로 낙하 + 가로 흐름
            var vel      = ps1.velocityOverLifetime;
            vel.enabled  = true;
            vel.x        = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            vel.y        = new ParticleSystem.MinMaxCurve(-2f, -0.5f);
            vel.z        = new ParticleSystem.MinMaxCurve(0f, 0f);   // 모든 축 TwoConstants 모드 통일

            var col      = ps1.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white,              0f),
                        new GradientColorKey(new Color(0.75f,0.88f,1f),1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f),
                        new GradientAlphaKey(0.9f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);
        }

        // Layer 2: 눈보라 안개
        var child2 = new GameObject("Fog");
        child2.transform.SetParent(go.transform, false);
        var ps2 = AddPS(child2);
        {
            var main     = ps2.main;
            main.duration        = 5f;
            main.loop            = true;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1f);
            main.startSize       = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);   // +25%
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(0.88f, 0.94f, 1f, 0.28f),
                new Color(0.75f, 0.87f, 1f, 0.22f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 25;

            var em       = ps2.emission;
            em.rateOverTime = 6f;

            var sh       = ps2.shape;
            sh.enabled   = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius    = 1.8f;

            var vel      = ps2.velocityOverLifetime;
            vel.enabled  = true;
            vel.x        = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            vel.y        = new ParticleSystem.MinMaxCurve(0f, 0f);   // 모든 축 TwoConstants 모드 통일
            vel.z        = new ParticleSystem.MinMaxCurve(0f, 0f);

            var col      = ps2.colorOverLifetime;
            col.enabled  = true;
            var g        = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white,              0f),
                        new GradientColorKey(new Color(0.85f,0.93f,1f),1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.3f),
                        new GradientAlphaKey(0.28f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color    = new ParticleSystem.MinMaxGradient(g);

            var sz       = ps2.sizeOverLifetime;
            sz.enabled   = true;
            sz.size      = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f,0.3f), new Keyframe(0.5f,1f), new Keyframe(1f,1.3f)));
        }

        return go;
    }
}
