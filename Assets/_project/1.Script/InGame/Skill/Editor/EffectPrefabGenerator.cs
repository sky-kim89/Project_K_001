using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EffectPrefabGenerator.cs  —  1부: FX #01~#11 구현 / #12~#22 스텁
// ============================================================

public static class EffectPrefabGenerator
{
    const string kSavePath = "Assets/_project/2.Prefabs/Effect";
    const string kMatPath  = "Assets/_project/4.Materials/FX";

    static readonly Dictionary<string, string[]> kEffectMaterials = new()
    {
        { "FX_Slash_Impact",     new[] { "MAT_FX_Slash_Add",    "MAT_FX_Spark_Add",     "MAT_FX_Soft_Add"      } },
        { "FX_Leap_Land",        new[] { "MAT_FX_Ring_Add",     "MAT_FX_Smoke_Alpha",   "MAT_FX_Soft_Add"      } },
        { "FX_Dust_Dash",        new[] { "MAT_FX_Smoke_Alpha"                                                   } },
        { "FX_Shockwave",        new[] { "MAT_FX_Spark_Add",    "MAT_FX_Lightning_Add", "MAT_FX_Ring_Add"      } },
        { "FX_Meteor_Warning",   new[] { "MAT_FX_Flame_Add",    "MAT_FX_Ring_Add"                              } },
        { "FX_Meteor_Explosion", new[] { "MAT_FX_Flame_Add",    "MAT_FX_Smoke_Alpha",   "MAT_FX_Ring_Add",    "MAT_FX_Shard_Add"   } },
        { "FX_Arrow_Volley",     new[] { "MAT_FX_Arrow_Add",    "MAT_FX_Spark_Add"                             } },
        { "FX_Arrow_Rain_Zone",  new[] { "MAT_FX_Arrow_Add",    "MAT_FX_Star_Add",      "MAT_FX_Soft_Add"      } },
        { "FX_Charge_Impact",    new[] { "MAT_FX_Star_Add",     "MAT_FX_Ring_Add",      "MAT_FX_Spark_Add"     } },
        { "FX_Explosion",        new[] { "MAT_FX_Flame_Add",    "MAT_FX_Shard_Add",     "MAT_FX_Smoke_Alpha",  "MAT_FX_Ring_Add"   } },
        { "FX_Summon_Circle",    new[] { "MAT_FX_Rune_Add",     "MAT_FX_Wisp_Add",      "MAT_FX_Soft_Add"      } },
        { "FX_Sacrifice",        new[] { "MAT_FX_Wisp_Add",     "MAT_FX_Soft_Add",      "MAT_FX_Smoke_Alpha"   } },
        { "FX_Absorb",           new[] { "MAT_FX_Soft_Add",     "MAT_FX_Ring_Add",      "MAT_FX_Spark_Add"     } },
        { "FX_Battle_Cry",       new[] { "MAT_FX_Star_Add",     "MAT_FX_Ring_Add",      "MAT_FX_Spark_Add"     } },
        { "FX_Berserk",          new[] { "MAT_FX_Flame_Add",    "MAT_FX_Line_Add",      "MAT_FX_Lightning_Add" } },
        { "FX_Shield_Up",        new[] { "MAT_FX_Diamond_Add",  "MAT_FX_Crystal_Add",   "MAT_FX_Soft_Add"      } },
        { "FX_Speed_Up",         new[] { "MAT_FX_Spark_Add",    "MAT_FX_Wisp_Add",      "MAT_FX_Ring_Add"      } },
        { "FX_Heal_Aura",        new[] { "MAT_FX_Petal_Add",    "MAT_FX_Soft_Add",      "MAT_FX_Ring_Add"      } },
        { "FX_Heal_Target",      new[] { "MAT_FX_Cross_Add",    "MAT_FX_Wisp_Add",      "MAT_FX_Soft_Add"      } },
        { "FX_Bind",             new[] { "MAT_FX_Spiral_Add",   "MAT_FX_Ring_Add",      "MAT_FX_Smoke_Alpha"   } },
        { "FX_Poison_Zone",      new[] { "MAT_FX_Smoke_Alpha",  "MAT_FX_Poison_Alpha",  "MAT_FX_Ring_Add"      } },
        { "FX_Blizzard",             new[] { "MAT_FX_Snowflake_Add","MAT_FX_Crystal_Add",   "MAT_FX_Smoke_Alpha"   } },
        // c1=ImpactSparks, c2=RedGlow — Root LineRenderer 는 BuildRedLightningChain 에서 직접 설정
        { "FX_RedLightning_Chain",   new[] { "MAT_FX_Spark_Add",   "MAT_FX_Soft_Add"                                } },
    };

    [MenuItem("BattleGame/Generate Effect Prefabs")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "_project/2.Prefabs/Effect"));
        AssetDatabase.Refresh();

        int n = 0;
        n += Save("FX_Slash_Impact",    BuildSlashImpact());
        n += Save("FX_Leap_Land",       BuildLeapLand());
        n += Save("FX_Dust_Dash",       BuildDustDash());
        n += Save("FX_Shockwave",       BuildShockwave());
        n += Save("FX_Meteor_Warning",  BuildMeteorWarning());
        n += Save("FX_Meteor_Explosion",BuildMeteorExplosion());
        n += Save("FX_Arrow_Volley",    BuildArrowVolley());
        n += Save("FX_Arrow_Rain_Zone", BuildArrowRainZone());
        n += Save("FX_Charge_Impact",   BuildChargeImpact());
        n += Save("FX_Explosion",       BuildExplosion());
        n += Save("FX_Summon_Circle",   BuildSummonCircle());
        n += Save("FX_Sacrifice",       BuildSacrifice());
        n += Save("FX_Absorb",          BuildAbsorb());
        n += Save("FX_Battle_Cry",      BuildBattleCry());
        n += Save("FX_Berserk",         BuildBerserk());
        n += Save("FX_Shield_Up",       BuildShieldUp());
        n += Save("FX_Speed_Up",        BuildSpeedUp());
        n += Save("FX_Heal_Aura",       BuildHealAura());
        n += Save("FX_Heal_Target",     BuildHealTarget());
        n += Save("FX_Bind",            BuildBind());
        n += Save("FX_Poison_Zone",     BuildPoisonZone());
        n += Save("FX_Blizzard",           BuildBlizzard());
        n += Save("FX_RedLightning_Chain", BuildRedLightningChain());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EffectPrefabGenerator] ✓ {n} effect prefabs generated → {kSavePath}");
    }

    // ── 헬퍼 ────────────────────────────────────────────────

    static int Save(string key, GameObject go)
    {
        go.name = key;
        if (kEffectMaterials.TryGetValue(key, out var mats)) ApplyMaterials(go, mats);
        PrefabUtility.SaveAsPrefabAsset(go, $"{kSavePath}/{key}.prefab");
        Object.DestroyImmediate(go);
        return 1;
    }

    static void ApplyMaterials(GameObject root, string[] mats)
    {
        var rs = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < Mathf.Min(rs.Length, mats.Length); i++)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{kMatPath}/{mats[i]}.mat");
            if (mat != null) rs[i].material = mat;
            else Debug.LogWarning($"[EffectPrefabGenerator] 머티리얼 없음: {mats[i]}");
        }
    }

    static GameObject NewGO() => new GameObject("FX");

    static ParticleSystem AddPS(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var rd = go.GetComponent<ParticleSystemRenderer>();
        rd.renderMode = ParticleSystemRenderMode.Billboard;
        rd.sortingLayerName = "Effect";
        rd.sortingOrder = 5;
        var def = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        if (def != null) rd.material = def;
        return ps;
    }

    static Color32 C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

    static AnimationCurve AC3(float v0, float t1, float v1, float v2) =>
        new AnimationCurve(new Keyframe(0f, v0), new Keyframe(t1, v1), new Keyframe(1f, v2));

    static Gradient MakeGrad(
        (float t, Color c)[] cols, (float t, float a)[] alps)
    {
        var g = new Gradient();
        var ck = new GradientColorKey[cols.Length];
        var ak = new GradientAlphaKey[alps.Length];
        for (int i = 0; i < cols.Length; i++) ck[i] = new GradientColorKey(cols[i].c, cols[i].t);
        for (int i = 0; i < alps.Length; i++) ak[i] = new GradientAlphaKey(alps[i].a, alps[i].t);
        g.SetKeys(ck, ak);
        return g;
    }

    // ── #01  FX_Slash_Impact ─────────────────────────────────
    // Root:Slash_Add  c1:Spark_Add  c2:Soft_Add
    static GameObject BuildSlashImpact()
    {
        var go = NewGO();

        // Root — 칼날 파편 (주황→적)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.3f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 18f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,210), C(255,100,15));
            m.startRotation  = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 28;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 38f; sh.radius = 0.05f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.35f, new Color(1f,0.55f,0f)), (1f, new Color(1f,0.08f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.85f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.5f, 0.2f, 1.4f, 0f));
        }

        // c1 — 금속 스파크 (흰→금)
        var c1 = new GameObject("Sparks"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.25f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(8f, 24f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.03f, 0.11f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,255), C(255,220,70));
            m.gravityModifier = 0.5f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 42;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 30) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55f; sh.radius = 0.05f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, new Color(1f,0.8f,0.1f)) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        // c2 — 백색 코어 플래시 (Soft_Add)
        var c2 = new GameObject("Flash"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.08f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 0.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,255), C(255,240,170));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 3;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, Color.white) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        return go;
    }

    // ── #02  FX_Leap_Land ────────────────────────────────────
    // Root:Ring_Add  c1:Smoke_Alpha  c2:Soft_Add
    static GameObject BuildLeapLand()
    {
        var go = NewGO();

        // Root — 방사 링 (청백)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(7f, 20f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.28f, 0.75f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(220,240,255), C(55,155,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 38) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.5f, new Color(0.3f,0.7f,1f)), (1f, new Color(0.1f,0.35f,1f)) },
                new[] { (0f, 1f), (0.5f, 0.7f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.25f, 1.35f, 0f));
        }

        // c1 — 지면 먼지 (Smoke_Alpha)
        var c1 = new GameObject("Dust"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(1.5f, 5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.3f, 0.85f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(200,175,130), C(245,220,165));
            m.gravityModifier = 0.2f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 22;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.8f,0.7f,0.5f)), (1f, new Color(0.5f,0.44f,0.32f)) },
                new[] { (0f, 0.7f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0f, 0.3f, 1.5f, 0.3f));
        }

        // c2 — 착지 섬광 (Soft_Add)
        var c2 = new GameObject("Flash"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.1f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 1f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,255), C(200,230,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 3;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, Color.white) },
                new[] { (0f, 0.9f), (1f, 0f) }));
        }

        return go;
    }

    // ── #03  FX_Dust_Dash ────────────────────────────────────
    // Root:Smoke_Alpha
    static GameObject BuildDustDash()
    {
        var go = NewGO();
        var ps = AddPS(go);
        var m = ps.main;
        m.duration = 0.4f; m.loop = false;
        m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 7f);
        m.startSize      = new ParticleSystem.MinMaxCurve(0.28f, 0.7f);
        m.startColor     = new ParticleSystem.MinMaxGradient(C(215,195,148), C(255,235,182));
        m.gravityModifier = 0.25f;
        m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
        var em = ps.emission; em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 65f; sh.radius = 0.25f;
        var col = ps.colorOverLifetime; col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
            new[] { (0f, new Color(0.9f,0.85f,0.68f)), (1f, new Color(0.62f,0.58f,0.42f)) },
            new[] { (0f, 0.75f), (1f, 0f) }));
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.4f, 0.3f, 1.2f, 0.2f));
        return go;
    }

    // ── #04  FX_Shockwave ────────────────────────────────────
    // Root:Spark_Add  c1:Lightning_Add  c2:Ring_Add
    // 모든 emitter 가 Circle arc(부채꼴) 형태 → ActiveShockwave 가 GO rotation 으로 방향 제어.
    // 프리팹 기준 arc = 120°, 반경 = 3 (SkillEffectHelper scale 로 조정).
    static GameObject BuildShockwave()
    {
        var go = NewGO();

        // Root — 전방 부채꼴 에너지 입자 (Spark_Add) — Circle arc 120°
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.7f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.25f, 0.60f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 16f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.2f, 0.65f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(220,250,255), C(28,175,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            // Circle arc: XY 평면에서 전방 120° 부채꼴로 방출
            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.2f; sh.radiusThickness = 1f;
            sh.arc = 120f; sh.arcMode = ParticleSystemShapeMultiModeValue.Random;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.2f,0.7f,1f)), (1f, new Color(0f,0.28f,1f)) },
                new[] { (0f, 1f), (0.4f, 0.7f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.4f, 0.5f, 1.15f, 0.05f));
        }

        // c1 — 번개 파편 (Lightning_Add) — 좁은 90° 집중
        var c1 = new GameObject("Lightning"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(10f, 28f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,255), C(140,225,255));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.04f, 22) });
            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.3f; sh.radiusThickness = 1f;
            sh.arc = 90f; sh.arcMode = ParticleSystemShapeMultiModeValue.Random;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.5f, new Color(0.4f,0.8f,1f)), (1f, new Color(0.1f,0.3f,1f)) },
                new[] { (0f, 1f), (0.4f, 0.6f), (1f, 0f) }));
        }

        // c2 — 퍼져나가는 충격파 (Ring_Add) — 넓은 140° 부채꼴 파형
        var c2 = new GameObject("Ring"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.65f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(7f, 18f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(200,240,255), C(75,195,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.03f, 48) });
            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.35f; sh.radiusThickness = 1f;
            sh.arc = 140f; sh.arcMode = ParticleSystemShapeMultiModeValue.Random;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.5f, new Color(0.3f,0.7f,1f)), (1f, new Color(0f,0.2f,0.8f)) },
                new[] { (0f, 0.9f), (0.5f, 0.5f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.5f, 1.4f, 0.1f));
        }

        return go;
    }

    // ── #05  FX_Meteor_Warning ───────────────────────────────
    // Root:Flame_Add(loop)  c1:Ring_Add(loop)
    static GameObject BuildMeteorWarning()
    {
        var go = NewGO();

        // Root — 불꽃 원형 경고 (loop)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.2f, 1.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,200,28), C(255,45,8));
            m.gravityModifier = -0.1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 35f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.4f; sh.radiusThickness = 0.06f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.85f,0.18f)), (0.5f, new Color(1f,0.22f,0f)), (1f, new Color(0.4f,0f,0f)) },
                new[] { (0f, 1f), (0.5f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0f, 0.4f, 1.3f, 0.2f));
        }

        // c1 — 맥박 링 (Ring_Add, loop)
        var c1 = new GameObject("PulseRing"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 1f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(3f, 9f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,170,0), C(255,70,0));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40), new ParticleSystem.Burst(0.5f, 40) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.4f,0f)), (1f, new Color(1f,0f,0f)) },
                new[] { (0f, 0.8f), (0.5f, 0.4f), (1f, 0f) }));
        }

        return go;
    }

    // ── #06  FX_Meteor_Explosion ─────────────────────────────
    // Root:Flame_Add  c1:Smoke_Alpha  c2:Ring_Add  c3:Shard_Add
    static GameObject BuildMeteorExplosion()
    {
        var go = NewGO();

        // Root — 거대 화염 (Flame_Add)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 22f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,240,95), C(255,65,8));
            m.gravityModifier = -0.3f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 70;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 55) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.3f, new Color(1f,0.5f,0f)), (1f, new Color(0.22f,0.04f,0f)) },
                new[] { (0f, 1f), (0.35f, 0.85f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.2f, 1.55f, 0.1f));
        }

        // c1 — 검은 연기 (Smoke_Alpha)
        var c1 = new GameObject("Smoke"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 7f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1f, 2.8f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(55,38,28), C(115,85,55));
            m.gravityModifier = -0.15f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 25;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.1f, 18) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.28f,0.19f,0.13f)), (1f, new Color(0.13f,0.09f,0.06f)) },
                new[] { (0f, 0.8f), (0.4f, 0.5f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.4f, 1.5f, 0.4f));
        }

        // c2 — 바닥 충격 링 (Ring_Add)
        var c2 = new GameObject("ShockRing"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(12f, 32f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.28f, 0.8f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,220,95), C(255,90,0));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 52) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.2f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.5f,0f)), (1f, new Color(0.5f,0.08f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.6f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.3f, 1.45f, 0f));
        }

        // c3 — 불꽃 파편 (Shard_Add)
        var c3 = new GameObject("Debris"); c3.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c3);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 20f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.42f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,175,55), C(175,55,8));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = 1.1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 35;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.68f,0.2f)), (1f, new Color(0.45f,0.08f,0f)) },
                new[] { (0f, 1f), (0.5f, 0.7f), (1f, 0f) }));
        }

        return go;
    }

    // ── #07  FX_Arrow_Volley ─────────────────────────────────
    // Root:Arrow_Add  c1:Spark_Add
    static GameObject BuildArrowVolley()
    {
        var go = NewGO();

        // Root — 화살 궤적 (황금)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(10f, 26f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.28f, 0.7f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,220,75), C(195,135,18));
            m.startRotation  = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18f; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.95f,0.58f)), (0.5f, new Color(1f,0.68f,0.1f)), (1f, new Color(0.68f,0.28f,0f)) },
                new[] { (0f, 0.9f), (0.5f, 0.7f), (1f, 0f) }));
        }

        // c1 — 충격 스파크 (Spark_Add)
        var c1 = new GameObject("ImpactSparks"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.25f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 16f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,200), C(255,195,45));
            m.gravityModifier = 0.5f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 30) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.9f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, new Color(1f,0.75f,0.1f)) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        return go;
    }

    // ── #08  FX_Arrow_Rain_Zone ──────────────────────────────
    // Root:Arrow_Add(loop)  c1:Star_Add(loop)  c2:Soft_Add(loop)
    static GameObject BuildArrowRainZone()
    {
        var go = NewGO();

        // Root — 낙하 화살 (loop)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(8f, 20f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.28f, 0.7f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,215,75), C(200,128,18));
            m.gravityModifier = 1.5f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 25f;
            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Rectangle; sh.scale = new Vector3(3f, 3f, 0f);
            sh.position  = new Vector3(0f, 4f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.9f,0.5f)), (1f, new Color(0.8f,0.5f,0.1f)) },
                new[] { (0f, 0.9f), (0.7f, 0.6f), (1f, 0f) }));
        }

        // c1 — 지면 충격 별 (Star_Add, loop)
        var c1 = new GameObject("GroundStars"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.14f, 0.42f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,240,148), C(255,175,28));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 20f;
            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Rectangle; sh.scale = new Vector3(3f, 3f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.5f, new Color(1f,0.75f,0.1f)), (1f, new Color(1f,0.38f,0f)) },
                new[] { (0f, 1f), (0.5f, 0.5f), (1f, 0f) }));
        }

        // c2 — 황금 글로우 (Soft_Add, loop)
        var c2 = new GameObject("GoldGlow"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 0.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,220,95,175), C(255,145,28,115));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 18;
            var em = ps.emission; em.rateOverTime = 8f;
            var sh = ps.shape; sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Rectangle; sh.scale = new Vector3(3f, 3f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.85f,0.28f)), (1f, new Color(1f,0.58f,0.1f)) },
                new[] { (0f, 0.5f), (1f, 0f) }));
        }

        return go;
    }

    // ── #09  FX_Charge_Impact ────────────────────────────────
    // Root:Star_Add  c1:Ring_Add  c2:Spark_Add
    static GameObject BuildChargeImpact()
    {
        var go = NewGO();

        // Root — 별 폭발 (황금백)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 16f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,215), C(255,195,55));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.85f,0.2f)), (1f, new Color(0.6f,0.28f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.15f, 1.4f, 0f));
        }

        // c1 — 충격 링 (Ring_Add)
        var c1 = new GameObject("Ring"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(9f, 22f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.18f, 0.52f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,240,175), C(200,148,45));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 52;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 44) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.75f,0.15f)), (1f, new Color(0.6f,0.24f,0f)) },
                new[] { (0f, 0.9f), (0.5f, 0.5f), (1f, 0f) }));
        }

        // c2 — 산란 스파크 (Spark_Add)
        var c2 = new GameObject("Sparks"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.3f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 20f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,195), C(255,188,38));
            m.gravityModifier = 0.6f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 45;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 34) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, new Color(1f,0.7f,0.1f)) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        return go;
    }

    // ── #10  FX_Explosion ────────────────────────────────────
    // Root:Flame_Add  c1:Shard_Add  c2:Smoke_Alpha  c3:Ring_Add
    static GameObject BuildExplosion()
    {
        var go = NewGO();

        // Root — 화염 폭발
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 18f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.8f, 2.6f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,228,88), C(255,55,8));
            m.gravityModifier = -0.2f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 55;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 42) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.3f, new Color(1f,0.44f,0f)), (1f, new Color(0.2f,0.04f,0f)) },
                new[] { (0f, 1f), (0.35f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.2f, 1.45f, 0.1f));
        }

        // c1 — 파편 (Shard_Add)
        var c1 = new GameObject("Debris"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.28f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 18f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.42f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,158,38), C(158,48,8));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = 1.2f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.58f,0.14f)), (1f, new Color(0.42f,0.09f,0f)) },
                new[] { (0f, 1f), (0.5f, 0.7f), (1f, 0f) }));
        }

        // c2 — 검은 연기 (Smoke_Alpha)
        var c2 = new GameObject("Smoke"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(1.5f, 5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.7f, 2.1f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(48,33,22), C(98,68,42));
            m.gravityModifier = -0.1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.1f, 14) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.24f,0.17f,0.11f)), (1f, new Color(0.11f,0.07f,0.04f)) },
                new[] { (0f, 0.75f), (0.4f, 0.42f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.4f, 1.5f, 0.4f));
        }

        // c3 — 폭발 링 (Ring_Add)
        var c3 = new GameObject("ExpRing"); c3.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c3);
            var m = ps.main;
            m.duration = 0.3f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(10f, 25f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.18f, 0.6f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,198,55), C(255,75,8));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 55;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 48) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.15f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.44f,0f)), (1f, new Color(0.4f,0.04f,0f)) },
                new[] { (0f, 1f), (0.45f, 0.55f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.3f, 1.42f, 0f));
        }

        return go;
    }

    // ── #11  FX_Summon_Circle ────────────────────────────────
    // Root:Rune_Add  c1:Wisp_Add  c2:Soft_Add  (단발 0.5초)
    static GameObject BuildSummonCircle()
    {
        var go = NewGO();

        // Root — 룬 원진 폭발 (자주색, 단발)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.3f, 0.85f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(200,98,255), C(115,38,255));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f; sh.radiusThickness = 0.08f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.9f,0.58f,1f)), (0.5f, new Color(0.58f,0.2f,1f)), (1f, new Color(0.28f,0f,0.58f)) },
                new[] { (0f, 1f), (0.5f, 0.7f), (1f, 0f) }));
        }

        // c1 — 위습 분출 (Wisp_Add, 단발)
        var c1 = new GameObject("Wisps"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(3f, 7f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.14f, 0.45f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(218,155,255), C(175,75,255));
            m.gravityModifier = -0.3f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.0f; sh.radiusThickness = 1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.78f,1f)), (0.5f, new Color(0.7f,0.28f,1f)), (1f, new Color(0.3f,0f,0.5f)) },
                new[] { (0f, 0.9f), (0.5f, 0.6f), (1f, 0f) }));
        }

        // c2 — 보라빛 플래시 (Soft_Add, 단발)
        var c2 = new GameObject("PurpleGlow"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.25f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 0.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(175,75,255,200), C(95,18,195,140));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 4;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.78f,0.38f,1f)), (1f, new Color(0.38f,0.08f,0.78f)) },
                new[] { (0f, 0.8f), (1f, 0f) }));
        }

        return go;
    }

    // ── #12  FX_Sacrifice ────────────────────────────────────
    // Root:Wisp_Add  c1:Soft_Add  c2:Smoke_Alpha
    static GameObject BuildSacrifice()
    {
        var go = NewGO();

        // Root — 영혼 위습 상승 (흰→금, 위로)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.8f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,220), C(255,215,80));
            m.gravityModifier = -0.5f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 35;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.9f,0.5f)), (1f, new Color(1f,0.7f,0.1f)) },
                new[] { (0f, 1f), (0.5f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.4f, 1.2f, 0.1f));
        }

        // c1 — 황금 글로우 (Soft_Add)
        var c1 = new GameObject("GoldGlow"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 0.8f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,240,140,200), C(255,185,40,140));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 8;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.95f,0.55f)), (1f, new Color(1f,0.65f,0.1f)) },
                new[] { (0f, 0.7f), (1f, 0f) }));
        }

        // c2 — 소멸 연기 (Smoke_Alpha)
        var c2 = new GameObject("Dissolve"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(180,155,100), C(130,108,65));
            m.gravityModifier = -0.08f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 15;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.1f, 10) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.72f,0.62f,0.4f)), (1f, new Color(0.38f,0.32f,0.2f)) },
                new[] { (0f, 0.55f), (1f, 0f) }));
        }

        return go;
    }

    // ── #13  FX_Absorb ───────────────────────────────────────
    // Root:Soft_Add  c1:Ring_Add  c2:Spark_Add
    static GameObject BuildAbsorb()
    {
        var go = NewGO();

        // Root — 흡수 글로우 (청록, 내향)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 2f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(80,255,210,200), C(30,195,255,150));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 15;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.8f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.3f,1f,0.82f)), (0.5f, new Color(0.12f,0.76f,1f)), (1f, new Color(0.05f,0.4f,0.8f)) },
                new[] { (0f, 0.8f), (0.5f, 0.5f), (1f, 0f) }));
        }

        // c1 — 수축 링 (Ring_Add)
        var c1 = new GameObject("ContrRing"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 14f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(100,255,220), C(40,200,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.2f,0.9f,1f)), (1f, new Color(0f,0.4f,0.9f)) },
                new[] { (0f, 0.9f), (0.5f, 0.5f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0f, 0.5f, 0.8f, 0f));
        }

        // c2 — 에너지 스파크 (Spark_Add)
        var c2 = new GameObject("EnergySparks"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(4f, 12f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(180,255,235), C(55,215,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 35;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 26) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.9f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, new Color(0.2f,0.85f,1f)) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        return go;
    }

    // ── #14  FX_Battle_Cry ───────────────────────────────────
    // Root:Star_Add  c1:Ring_Add  c2:Spark_Add
    static GameObject BuildBattleCry()
    {
        var go = NewGO();

        // Root — 황금 별 폭발
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(4f, 14f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,245,140), C(255,175,18));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = -0.1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.35f, new Color(1f,0.88f,0.2f)), (1f, new Color(1f,0.5f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.85f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.25f, 1.3f, 0f));
        }

        // c1 — 확장 링 (Ring_Add)
        var c1 = new GameObject("Ring"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(8f, 22f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,235,100), C(255,155,20));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 55;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 46) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(1f,0.8f,0.15f)), (1f, new Color(1f,0.42f,0f)) },
                new[] { (0f, 0.9f), (0.5f, 0.5f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.35f, 1.4f, 0f));
        }

        // c2 — 금빛 스파크 (Spark_Add)
        var c2 = new GameObject("GoldSparks"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(5f, 16f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.04f, 0.13f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,255,180), C(255,195,35));
            m.gravityModifier = 0.4f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 45;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.03f, 35) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (1f, new Color(1f,0.72f,0.08f)) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        return go;
    }

    // ── #15  FX_Berserk ──────────────────────────────────────
    // Root:Flame_Add  c1:Line_Add  c2:Lightning_Add
    static GameObject BuildBerserk()
    {
        var go = NewGO();

        // Root — 진홍 화염 (상승)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.8f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 7f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,60,20), C(200,10,10));
            m.gravityModifier = -0.4f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 45;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22f; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.9f,0.3f)), (0.3f, new Color(1f,0.2f,0.02f)), (1f, new Color(0.35f,0f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.35f, 1.3f, 0.05f));
        }

        // c1 — 에너지 슬래시 선 (Line_Add)
        var c1 = new GameObject("EnergyLines"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(8f, 20f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,120,50), C(255,30,10));
            m.startRotation  = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 45f; sh.radius = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.3f, new Color(1f,0.38f,0.05f)), (1f, new Color(0.6f,0f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.7f), (1f, 0f) }));
        }

        // c2 — 붉은 번개 (Lightning_Add)
        var c2 = new GameObject("RedLightning"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(10f, 28f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.48f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(255,180,140), C(255,30,20));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 22;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 15) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f,0.8f,0.7f)), (0.5f, new Color(1f,0.15f,0.05f)), (1f, new Color(0.4f,0f,0f)) },
                new[] { (0f, 1f), (0.4f, 0.6f), (1f, 0f) }));
        }

        return go;
    }

    // ── #16  FX_Shield_Up ────────────────────────────────────
    // Root:Diamond_Add  c1:Crystal_Add  c2:Soft_Add
    static GameObject BuildShieldUp()
    {
        var go = NewGO();

        // Root — 다이아몬드 방패 파편 (시안/빙설)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(3f, 10f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.2f, 0.65f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(210,245,255), C(80,200,255));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 28;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.5f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.5f,0.88f,1f)), (1f, new Color(0.15f,0.55f,1f)) },
                new[] { (0f, 1f), (0.5f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.3f, 1.3f, 0f));
        }

        // c1 — 얼음 결정 파편 (Crystal_Add)
        var c1 = new GameObject("IceCrystals"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 8f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.38f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(220,248,255), C(100,210,255));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = 0.15f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 22) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.6f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.6f,0.92f,1f)), (1f, new Color(0.2f,0.6f,1f)) },
                new[] { (0f, 1f), (0.5f, 0.7f), (1f, 0f) }));
        }

        // c2 — 빙청 글로우 (Soft_Add)
        var c2 = new GameObject("IceGlow"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 1f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1f, 2.8f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(200,240,255,210), C(80,195,255,155));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 6;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.78f,0.94f,1f)), (1f, new Color(0.3f,0.68f,1f)) },
                new[] { (0f, 0.8f), (1f, 0f) }));
        }

        return go;
    }

    // ── #17  FX_Speed_Up ─────────────────────────────────────
    // Root:Spark_Add  c1:Wisp_Add  c2:Ring_Add
    static GameObject BuildSpeedUp()
    {
        var go = NewGO();

        // Root — 라임 스파크 스트림
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 18f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(220,255,100), C(100,255,50));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 38) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 55f; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.3f, new Color(0.82f,1f,0.28f)), (1f, new Color(0.22f,0.8f,0.05f)) },
                new[] { (0f, 1f), (0.4f, 0.7f), (1f, 0f) }));
        }

        // c1 — 속도 위습 트레일 (Wisp_Add)
        var c1 = new GameObject("SpeedWisps"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(3f, 10f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(180,255,120), C(80,220,40));
            m.gravityModifier = -0.1f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 22;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.03f, 16) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.85f,1f,0.55f)), (0.5f, new Color(0.4f,1f,0.18f)), (1f, new Color(0.1f,0.55f,0f)) },
                new[] { (0f, 0.9f), (0.5f, 0.6f), (1f, 0f) }));
        }

        // c2 — 링 펄스 (Ring_Add)
        var c2 = new GameObject("RingPulse"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.4f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(8f, 20f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(200,255,120), C(85,220,30));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 48;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.04f, 40) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.65f,1f,0.18f)), (1f, new Color(0.18f,0.65f,0f)) },
                new[] { (0f, 0.9f), (0.5f, 0.5f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.35f, 1.4f, 0f));
        }

        return go;
    }

    // ── #18  FX_Heal_Aura ────────────────────────────────────
    // Root:Petal_Add  c1:Soft_Add  c2:Ring_Add
    static GameObject BuildHealAura()
    {
        var go = NewGO();

        // Root — 상승 꽃잎 (밝은 녹색/청록)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.8f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(1f, 4f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.48f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(140,255,180), C(40,220,130));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = -0.35f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 35;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.8f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.35f, new Color(0.48f,1f,0.62f)), (1f, new Color(0.1f,0.75f,0.4f)) },
                new[] { (0f, 1f), (0.5f, 0.8f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.4f, 1.2f, 0.1f));
        }

        // c1 — 치유 글로우 (Soft_Add)
        var c1 = new GameObject("HealGlow"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 1f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(155,255,195,200), C(55,210,130,145));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 8;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.6f,1f,0.72f)), (1f, new Color(0.18f,0.8f,0.45f)) },
                new[] { (0f, 0.75f), (1f, 0f) }));
        }

        // c2 — 치유 링 (Ring_Add)
        var c2 = new GameObject("HealRing"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.22f, 0.4f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(6f, 16f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(175,255,205), C(55,215,138));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 50;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.03f, 42) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.42f,1f,0.6f)), (1f, new Color(0.1f,0.7f,0.38f)) },
                new[] { (0f, 0.9f), (0.5f, 0.5f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.1f, 0.35f, 1.38f, 0f));
        }

        return go;
    }

    // ── #19  FX_Heal_Target ──────────────────────────────────
    // Root:Cross_Add  c1:Wisp_Add  c2:Soft_Add
    static GameObject BuildHealTarget()
    {
        var go = NewGO();

        // Root — 치유 십자 (Cross_Add)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 0.5f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 8f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(175,255,205), C(55,220,140));
            m.startRotation  = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            m.gravityModifier = -0.2f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 18;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.35f, new Color(0.5f,1f,0.65f)), (1f, new Color(0.12f,0.78f,0.45f)) },
                new[] { (0f, 1f), (0.4f, 0.85f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.25f, 1.25f, 0f));
        }

        // c1 — 치유 위습 (Wisp_Add)
        var c1 = new GameObject("HealWisps"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 0.6f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(190,255,215), C(65,225,148));
            m.gravityModifier = -0.25f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 15) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.75f,1f,0.84f)), (0.5f, new Color(0.3f,1f,0.58f)), (1f, new Color(0.08f,0.62f,0.3f)) },
                new[] { (0f, 0.9f), (0.5f, 0.6f), (1f, 0f) }));
        }

        // c2 — 치유 섬광 (Soft_Add)
        var c2 = new GameObject("HealFlash"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 0.12f; m.loop = false;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0f, 0.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(200,255,220,230), C(100,240,175,180));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 3;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.78f,1f,0.86f)), (1f, new Color(0.38f,1f,0.6f)) },
                new[] { (0f, 0.9f), (1f, 0f) }));
        }

        return go;
    }

    // ── #20  FX_Bind ─────────────────────────────────────────
    // 속박 지속 이펙트 (3초 loop) — EffectDespawnDelay=4로 자동 소멸
    // Root:Spiral_Add(loop)  c1:Ring_Add(loop)  c2:Smoke_Alpha(loop)
    static GameObject BuildBind()
    {
        var go = NewGO();

        // Root — 나선 속박 파티클 (보라, loop)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(180,80,255), C(100,20,200));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 60;
            var em = ps.emission; em.rateOverTime = 18f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.6f; sh.radiusThickness = 0f;  // 가장자리에서만 발생 → 링처럼 보임
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.85f,0.55f,1f)), (0.5f, new Color(0.58f,0.12f,1f)), (1f, new Color(0.25f,0f,0.55f)) },
                new[] { (0f, 1f), (0.6f, 0.65f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.5f, 1.0f, 0.0f));
        }

        // c1 — 맥동하는 링 (Ring_Add, loop)
        var c1 = new GameObject("BindRings"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(210,140,255), C(130,30,255));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 10f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.75f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.72f,0.28f,1f)), (1f, new Color(0.3f,0f,0.62f)) },
                new[] { (0f, 0.85f), (0.5f, 0.45f), (1f, 0f) }));
        }

        // c2 — 어둠 안개 (Smoke_Alpha, loop)
        var c2 = new GameObject("DarkMist"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(55,20,80), C(30,8,50));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 15;
            var em = ps.emission; em.rateOverTime = 4f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.22f,0.08f,0.32f)), (1f, new Color(0.1f,0.03f,0.18f)) },
                new[] { (0f, 0.55f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.4f, 0.6f, 1.2f, 0.1f));
        }

        return go;
    }

    // ── #21  FX_Poison_Zone ──────────────────────────────────
    // Root:Smoke_Alpha(loop)  c1:Poison_Alpha(loop)  c2:Ring_Add
    static GameObject BuildPoisonZone()
    {
        var go = NewGO();

        // Root — 짙은 독 연기 (Smoke_Alpha, loop)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(28,55,18), C(50,80,25));
            m.gravityModifier = -0.05f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 14f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f; sh.radiusThickness = 1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.16f,0.28f,0.08f)), (1f, new Color(0.08f,0.14f,0.04f)) },
                new[] { (0f, 0.7f), (0.5f, 0.45f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.3f, 0.4f, 1.4f, 0.4f));
        }

        // c1 — 독성 구름 (Poison_Alpha, loop)
        var c1 = new GameObject("ToxicCloud"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 3f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(100,200,30,200), C(160,240,40,170));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 10f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.0f; sh.radiusThickness = 1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.42f,0.88f,0.1f)), (0.5f, new Color(0.58f,0.95f,0.12f)), (1f, new Color(0.22f,0.5f,0.05f)) },
                new[] { (0f, 0.65f), (0.5f, 0.45f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.4f, 1.3f, 0.35f));
        }

        // c2 — 독 존 링 마커 (Ring_Add)
        var c2 = new GameObject("PoisonRing"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 1f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 6f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(120,220,35), C(80,180,20));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 55;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 45), new ParticleSystem.Burst(0.5f, 45) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.1f; sh.radiusThickness = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.58f,1f,0.18f)), (0.5f, new Color(0.38f,0.8f,0.1f)), (1f, new Color(0.18f,0.45f,0.05f)) },
                new[] { (0f, 0.8f), (0.5f, 0.4f), (1f, 0f) }));
        }

        return go;
    }

    // ── #22  FX_Blizzard ─────────────────────────────────────
    // Root:Snowflake_Add  c1:Crystal_Add  c2:Smoke_Alpha  (지속 장판)
    static GameObject BuildBlizzard()
    {
        var go = NewGO();

        // Root — 낙설 (Snowflake_Add, loop 지속)
        {
            var ps = AddPS(go);
            var m = ps.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(240,250,255), C(160,220,255));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.gravityModifier = 0.15f;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 80;
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 3f; sh.radiusThickness = 1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.5f, new Color(0.72f,0.9f,1f)), (1f, new Color(0.35f,0.65f,1f)) },
                new[] { (0f, 1f), (0.6f, 0.75f), (1f, 0f) }));
        }

        // c1 — 빙결 결정 부유 (Crystal_Add, loop)
        var c1 = new GameObject("IceCrystals"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.38f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(210,240,255), C(85,185,255));
            m.startRotation  = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 40;
            var em = ps.emission; em.rateOverTime = 18f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 2.5f; sh.radiusThickness = 1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, Color.white), (0.4f, new Color(0.58f,0.88f,1f)), (1f, new Color(0.18f,0.55f,1f)) },
                new[] { (0f, 0.85f), (0.6f, 0.5f), (1f, 0f) }));
        }

        // c2 — 빙설 안개 (Smoke_Alpha, loop 지속)
        var c2 = new GameObject("IcyMist"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration = 2f; m.loop = true;
            m.startLifetime  = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            m.startSpeed     = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
            m.startSize      = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
            m.startColor     = new ParticleSystem.MinMaxGradient(C(195,230,255,140), C(130,195,255,95));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 25;
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 2.8f; sh.radiusThickness = 1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(0.78f,0.9f,1f)), (1f, new Color(0.52f,0.76f,1f)) },
                new[] { (0f, 0.45f), (0.5f, 0.3f), (1f, 0f) }));
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AC3(0.2f, 0.5f, 1.4f, 0.4f));
        }

        return go;
    }

    // ── FX_RedLightning_Chain ────────────────────────────────────────
    // 폭우 사격 특성 — 주 타겟 A → 스플래시 타겟 B 붉은 전기 체인
    // Root : LineRenderer + MAT_FX_ElectricBeam_Add (붉은 전기 선)
    // c1   : ImpactSparks — B 위치 붉은 스파크 버스트   (MAT_FX_Spark_Add)
    // c2   : RedGlow      — B 위치 붉은 플래시 글로우   (MAT_FX_Soft_Add)
    // SpawnLine() 이 런타임에 LineRenderer 양 끝점과 c1/c2 월드 위치를 설정.
    static GameObject BuildRedLightningChain()
    {
        var go = NewGO();

        // Root — LineRenderer: A→B 전기 체인 선
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount       = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, Vector3.right * 2f);  // SpawnLine() 이 런타임에 덮어씀
        lr.startWidth          = 0.18f;
        lr.endWidth            = 0.06f;
        lr.useWorldSpace       = true;
        lr.textureMode         = LineTextureMode.Tile;
        lr.sortingLayerName    = "Effect";
        lr.sortingOrder        = 5;
        lr.generateLightingData = false;
        var lrMat = AssetDatabase.LoadAssetAtPath<Material>($"{kMatPath}/MAT_FX_ElectricBeam_Add.mat");
        if (lrMat != null)
        {
            lr.material   = lrMat;
            lr.startColor = new Color(1f, 0.08f, 0.08f, 1f);
            lr.endColor   = new Color(0.6f, 0f, 0f, 0f);
        }

        // c1 — B 위치 충격 스파크 (MAT_FX_Spark_Add, 단발)
        var c1 = new GameObject("ImpactSparks"); c1.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c1);
            var m = ps.main;
            m.duration      = 0.2f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            m.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 9f);
            m.startSize     = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
            m.startColor    = new ParticleSystem.MinMaxGradient(C(255, 80, 80), C(180, 0, 0));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 20;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.12f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f, 0.5f, 0.5f)), (1f, new Color(0.5f, 0f, 0f)) },
                new[] { (0f, 1f), (1f, 0f) }));
        }

        // c2 — B 위치 붉은 플래시 글로우 (MAT_FX_Soft_Add, 단발)
        var c2 = new GameObject("RedGlow"); c2.transform.SetParent(go.transform, false);
        {
            var ps = AddPS(c2);
            var m = ps.main;
            m.duration      = 0.15f; m.loop = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            m.startSpeed    = new ParticleSystem.MinMaxCurve(0f, 0.5f);
            m.startSize     = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            m.startColor    = new ParticleSystem.MinMaxGradient(C(255, 60, 60, 200), C(200, 0, 0, 120));
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 4;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.08f;
            var col = ps.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(MakeGrad(
                new[] { (0f, new Color(1f, 0.3f, 0.3f)), (1f, new Color(0.5f, 0f, 0f)) },
                new[] { (0f, 0.75f), (1f, 0f) }));
        }

        return go;
    }
}
