using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ============================================================
//  SkillSfxGenerator.cs  [Editor Only]
//
//  액티브 스킬 효과음을 결정적으로 합성한다.
//  실제 재료의 질감과 체감 음량을 기준으로 스킬마다 다른 팔레트를 사용한다.
// ============================================================

public static class SkillSfxGenerator
{
    const int SampleRate = 44100;
    const string SfxFolder = "Assets/_project/5.Audio/SFX";
    const string SplashScene = "Assets/Scenes/Splash.unity";
    static readonly string[] BisectMetalSources =
    {
        "Assets/_project/5.Audio/Source/StarNinjasSwordClash/sword_clash.6.ogg",
        "Assets/_project/5.Audio/Source/StarNinjasSwordClash/sword_clash.3.ogg",
    };

    readonly struct Profile
    {
        public readonly SfxKey Key;
        public readonly ActiveSkillId Id;
        public readonly float Seconds;

        public Profile(ActiveSkillId id, float seconds)
        {
            Key = (SfxKey)(100 + (int)id);
            Id = id;
            Seconds = seconds;
        }

        public Profile(SfxKey key, ActiveSkillId id, float seconds)
        {
            Key = key;
            Id = id;
            Seconds = seconds;
        }
    }

    static readonly Profile[] Profiles =
    {
        new(ActiveSkillId.HeavyStrike,        0.52f),
        new(ActiveSkillId.VolleyFire,         0.48f),
        new(ActiveSkillId.LeapStrike,         0.72f),
        new(ActiveSkillId.HealAura,           0.78f),
        new(ActiveSkillId.TargetHeal,         0.64f),
        new(ActiveSkillId.ChargeSoldier,      0.72f),
        new(ActiveSkillId.SummonSkeleton,     0.86f),
        new(ActiveSkillId.PoisonZone,         0.72f),
        new(ActiveSkillId.Meteor,             1.12f),
        new(ActiveSkillId.Blizzard,           1.35f),
        new(ActiveSkillId.SacrificeSoldier,   0.72f),
        new(ActiveSkillId.Bind,               0.58f),
        new(ActiveSkillId.SuicideSoldier,     1.02f),
        new(ActiveSkillId.Berserker,          0.82f),
        new(ActiveSkillId.IronShield,         0.68f),
        new(ActiveSkillId.ArrowRain,          1.02f),
        new(ActiveSkillId.BattleCry,          0.88f),
        new(ActiveSkillId.Shockwave,          0.78f),
        new(ActiveSkillId.SwiftStrike,        0.64f),
        new(ActiveSkillId.SummonElite,        1.02f),

        new(ActiveSkillId.Bisect,             0.74f),
        new(ActiveSkillId.ArrowStorm,         1.28f),
        new(ActiveSkillId.GravityCollapse,    1.05f),
        new(ActiveSkillId.Bulwark,            1.02f),
        new(ActiveSkillId.ChainLightning,     1.08f),
        new(ActiveSkillId.DeathSentence,      0.62f),
        new(ActiveSkillId.BloodPrice,         1.02f),
        new(ActiveSkillId.PiercingDash,       0.72f),
        new(ActiveSkillId.WarBanner,          1.02f),
        new(ActiveSkillId.Gravestone,         1.28f),

        new(ActiveSkillId.BossCharge,         0.96f),
        new(ActiveSkillId.BossSlam,           1.14f),
        new(ActiveSkillId.BossEnrage,         1.16f),
        new(ActiveSkillId.BossJumpShockwave,  0.76f),

        // 선행음을 한 파일에 넣으면 착탄 지연 동안 시전음도 같이 늦어진다.
        new(SfxKey.SKILL_Bisect_Cast,          ActiveSkillId.Bisect,          0.86f),
        new(SfxKey.SKILL_GravityCollapse_Cast, ActiveSkillId.GravityCollapse, 1.75f),
    };

    public static void GenerateAll()
    {
        string fullFolder = Path.Combine(Application.dataPath, "_project/5.Audio/SFX");
        Directory.CreateDirectory(fullFolder);
        PrepareBisectSources();

        foreach (var profile in Profiles)
            Write(profile, fullFolder);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[SkillSfxGenerator] ✓ 액티브 스킬 효과음 {Profiles.Length}종 생성 → {SfxFolder}");
    }

    public static void GenerateBisectCast()
    {
        string fullFolder = Path.Combine(Application.dataPath, "_project/5.Audio/SFX");
        Directory.CreateDirectory(fullFolder);
        PrepareBisectSources();

        foreach (var profile in Profiles)
        {
            if (profile.Key != SfxKey.SKILL_Bisect_Cast) continue;
            Write(profile, fullFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[SkillSfxGenerator] ✓ 일도 양단 시전 효과음 재생성");
            return;
        }

        throw new InvalidOperationException("SKILL_Bisect_Cast 프로필이 없습니다.");
    }

    static void PrepareBisectSources()
    {
        foreach (string path in BisectMetalSources)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = SampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }
    }

    [MenuItem(ProjectKMenu.Fx + "스킬 효과음 (36종)", priority = ProjectKMenu.PrefabPrio + 54)]
    public static void GenerateAllAndWireSplash()
    {
        GenerateAll();
        var scene = EditorSceneManager.OpenScene(SplashScene, OpenSceneMode.Single);
        var manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (manager == null)
            throw new InvalidOperationException($"{SplashScene} 에 AudioManager 가 없습니다.");

        AudioManagerEditor.LoadClips(manager);
        VerifyWiring(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SkillSfxGenerator] ✓ Splash AudioManager 연결 완료");
    }

    static void VerifyWiring(AudioManager manager)
    {
        var clips = new SerializedObject(manager).FindProperty("_clips");
        foreach (var profile in Profiles)
        {
            bool found = false;
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = (AudioClip)clips.GetArrayElementAtIndex(i).objectReferenceValue;
                if (clip.name != profile.Key.ToString()) continue;
                found = true;
                break;
            }

            if (!found)
                throw new InvalidOperationException(
                    $"Splash AudioManager 에 {profile.Key} 클립이 연결되지 않았습니다.");
        }
    }

    static void Write(Profile p, string fullFolder)
    {
        var samples = new float[Mathf.CeilToInt(SampleRate * p.Seconds)];
        uint seed = 0x9E3779B9u ^ ((uint)p.Key * 0x85EBCA6Bu);

        Compose(samples, p, seed);

        Master(samples, p.Key);
        ValidateLoudness(samples, p.Key);
        WriteWave(Path.Combine(fullFolder, p.Key + ".wav"), samples);
    }

    static void Compose(float[] d, Profile p, uint s)
    {
        switch (p.Key)
        {
            case SfxKey.SKILL_HeavyStrike:
                DullThud(d, 0.015f, 1f, 0.72f, s);
                break;
            case SfxKey.SKILL_VolleyFire:
                for (int i = 0; i < 5; i++)
                    AirSlice(d, i * 0.018f, 0.32f, 0.28f, s + (uint)i);
                break;
            case SfxKey.SKILL_LeapStrike:
                AirSlice(d, 0f, 0.34f, 0.44f, s);
                DullThud(d, 0.29f, 0.95f, 0.78f, s + 10);
                Dirt(d, 0.31f, 0.36f, 0.22f, s + 11);
                break;
            case SfxKey.SKILL_HealAura:
                BuffPulse(d, 0f, 0.76f, 0.58f, s);
                break;
            case SfxKey.SKILL_TargetHeal:
                BuffPulse(d, 0f, 0.62f, 0.5f, s);
                break;
            case SfxKey.SKILL_ChargeSoldier:
                AirSlice(d, 0f, 0.35f, 0.42f, s);
                DullThud(d, 0.3f, 0.78f, 0.66f, s + 10);
                ArmorShift(d, 0.31f, 0.4f, s + 11);
                break;
            case SfxKey.SKILL_SummonSkeleton:
                SummonPulse(d, 0f, 0.48f, s);
                SoilCrumble(d, 0.12f, 0.68f, 0.44f, s + 10);
                BoneRattle(d, 0.22f, 0.5f, 0.32f, s + 20);
                break;
            case SfxKey.SKILL_PoisonZone:
                LiquidBubble(d, 0f, 0.68f, 0.72f, s);
                break;
            case SfxKey.SKILL_Meteor:
                Explosion(d, 0f, 1f, 1.05f, s);
                Dirt(d, 0.04f, 0.86f, 0.35f, s + 10);
                break;
            case SfxKey.SKILL_Blizzard:
                WhistlingWind(d, 0f, 1.3f, 0.72f, s);
                break;
            case SfxKey.SKILL_SacrificeSoldier:
                DullThud(d, 0.02f, 1.18f, 0.82f, s);
                break;
            case SfxKey.SKILL_Bind:
                RopeSnap(d, 0.04f, 0.76f, s);
                RopeSnap(d, 0.18f, 0.54f, s + 1);
                BandSweep(d, 0f, 0.46f, 0.24f, 140f, 260f, 920f, 1450f, s + 2);
                break;
            case SfxKey.SKILL_SuicideSoldier:
                Fuse(d, 0f, 0.32f, 0.44f, s);
                Explosion(d, 0.3f, 0.9f, 0.88f, s + 20);
                break;
            case SfxKey.SKILL_Berserker:
                BuffPulse(d, 0f, 0.82f, 0.82f, s);
                break;
            case SfxKey.SKILL_IronShield:
                ShieldRaise(d, 0f, 0.82f, s);
                break;
            case SfxKey.SKILL_ArrowRain:
                for (int i = 0; i < 12; i++)
                    AirSlice(d, i * 0.068f, 0.22f, 0.2f, s + (uint)i);
                break;
            case SfxKey.SKILL_BattleCry:
                BuffPulse(d, 0f, 0.84f, 0.76f, s);
                DullThud(d, 0.08f, 0.3f, 0.62f, s + 1);
                break;
            case SfxKey.SKILL_Shockwave:
                GroundHit(d, 0f, 0.94f, 0.82f, s);
                break;
            case SfxKey.SKILL_SwiftStrike:
                BuffPulse(d, 0f, 0.62f, 0.62f, s);
                break;
            case SfxKey.SKILL_SummonElite:
                SummonPulse(d, 0f, 0.72f, s);
                ArmorShift(d, 0.42f, 0.56f, s + 2);
                break;
            case SfxKey.SKILL_Bisect_Cast:
                SwordDrawAndSlash(d, 0f, 0.82f, s);
                break;
            case SfxKey.SKILL_Bisect:
                GlassBreak(d, 0f, 0.98f, s);
                break;
            case SfxKey.SKILL_ArrowStorm:
                Shotgun(d, 0f,    0.8f, s);
                Shotgun(d, 0.45f, 0.86f, s + 10);
                Shotgun(d, 0.90f, 1f, s + 20);
                break;
            case SfxKey.SKILL_GravityCollapse_Cast:
                Suction(d, 0f, 1.7f, 0.48f, s);
                break;
            case SfxKey.SKILL_GravityCollapse:
                PressureCollapse(d, 0f, 1f, s);
                break;
            case SfxKey.SKILL_Bulwark:
                ShieldRaise(d, 0f, 1.05f, s);
                BuffPulse(d, 0.28f, 0.66f, 0.46f, s + 3);
                break;
            case SfxKey.SKILL_ChainLightning:
                Lightning(d, 0f, 1.02f, 8, s);
                break;
            case SfxKey.SKILL_DeathSentence:
                GunChamber(d, 0.03f, 1f, s);
                break;
            case SfxKey.SKILL_BloodPrice:
                WetSplatter(d, 0f, 0.9f, s);
                DullThud(d, 0.08f, 0.48f, 0.78f, s + 4);
                break;
            case SfxKey.SKILL_PiercingDash:
                AirSlice(d, 0f,    0.28f, 0.5f, s);
                AirSlice(d, 0.18f, 0.28f, 0.55f, s + 1);
                AirSlice(d, 0.36f, 0.3f,  0.62f, s + 2);
                break;
            case SfxKey.SKILL_WarBanner:
                BuffPulse(d, 0f, 0.92f, 0.74f, s);
                GroundHit(d, 0.5f, 0.48f, 0.62f, s + 2);
                break;
            case SfxKey.SKILL_Gravestone:
                StoneImpact(d, 0f,    0.72f, s);
                StoneImpact(d, 0.11f, 0.82f, s + 10);
                StoneImpact(d, 0.24f, 0.92f, s + 20);
                StoneImpact(d, 0.39f, 1.05f, s + 30);
                break;
            case SfxKey.SKILL_BossCharge:
                AirSlice(d, 0f, 0.48f, 0.5f, s);
                ArmorShift(d, 0.17f, 0.7f, s + 1);
                DullThud(d, 0.43f, 1.05f, 0.9f, s + 2);
                break;
            case SfxKey.SKILL_BossSlam:
                GroundHit(d, 0f, 1.2f, 1f, s);
                StoneImpact(d, 0.12f, 0.72f, s + 10);
                Dirt(d, 0.03f, 0.9f, 0.46f, s + 20);
                break;
            case SfxKey.SKILL_BossEnrage:
                Roar(d, 0f, 1.12f, 0.9f, s);
                ArmorShift(d, 0.28f, 0.46f, s + 1);
                break;
            case SfxKey.SKILL_BossJumpShockwave:
                GroundHit(d, 0f, 1.12f, 0.96f, s);
                Dirt(d, 0.02f, 0.66f, 0.4f, s + 1);
                break;
        }
    }

    // 칼날·화살이 통과하는 중역 중심의 짧은 마찰음.
    // 6 kHz 위 백색 노이즈를 주성분으로 쓰면 실제 음량보다 훨씬 거슬린다.
    static void AirSlice(float[] d, float start, float duration, float amp, uint seed)
    {
        BandSweep(d, start, duration, amp * 0.72f,
                  260f, 920f, 1800f, 4800f, seed);
        Noise(d, start + duration * 0.42f, duration * 0.28f,
              amp * 0.055f, 0.62f, 12f, seed + 1);
    }

    // 버프·소환 공통 팔레트: 낮은 압력 펄스 + 짧은 입자 세 번.
    // 지속 바람과 음계 진행은 넣지 않는다.
    static void BuffPulse(float[] d, float start, float duration, float amp, uint seed)
    {
        DrumBody(d, start, 0.28f, 92f, 58f, amp * 0.48f);
        Noise(d, start, 0.08f, amp * 0.28f, 0.2f, 18f, seed);

        for (int i = 0; i < 3; i++)
        {
            float at = start + 0.13f + i * 0.13f;
            float brightness = 0.42f + i * 0.16f;
            Noise(d, at, 0.065f, amp * (0.2f + i * 0.035f),
                  brightness, 22f, seed + (uint)(i + 1));
            GrainCluster(d, at, 0.1f, amp * 0.16f, brightness, seed + (uint)(10 + i));
        }
    }

    static void SummonPulse(float[] d, float start, float amp, uint seed)
    {
        BuffPulse(d, start, 0.7f, amp, seed);
        DrumBody(d, start + 0.34f, 0.3f, 76f, 44f, amp * 0.34f);
    }

    static void GrainCluster(float[] d, float start, float duration,
                             float amp, float brightness, uint seed)
    {
        for (int i = 0; i < 7; i++)
        {
            float at = start + Random01(ref seed) * duration;
            Noise(d, at, 0.009f + Random01(ref seed) * 0.014f,
                  amp * (0.35f + Random01(ref seed) * 0.45f),
                  brightness, 55f, seed + (uint)i);
        }
    }

    static void SoilCrumble(float[] d, float start, float duration, float amp, uint seed)
    {
        Noise(d, start, duration, amp * 0.36f, 0.06f, 3.8f, seed);
        for (int i = 0; i < 12; i++)
        {
            float at = start + Random01(ref seed) * duration * 0.82f;
            Noise(d, at, 0.025f + Random01(ref seed) * 0.035f,
                  amp * (0.12f + Random01(ref seed) * 0.13f),
                  0.22f, 28f, seed + (uint)(i + 1));
        }
    }

    static void BoneRattle(float[] d, float start, float duration, float amp, uint seed)
    {
        // 클릭 사이를 불규칙하게 두고 어택을 낮춰 총성처럼 연속되지 않게 한다.
        for (int i = 0; i < 7; i++)
        {
            float at = start + Random01(ref seed) * duration * 0.82f;
            float a = amp * (0.22f + Random01(ref seed) * 0.2f);
            Noise(d, at, 0.055f, a, 0.34f, 24f, seed + (uint)i);
            Resonance(d, at, 0.05f, 310f + Random01(ref seed) * 280f, a * 0.12f, 28f);
        }
    }

    static void ArmorShift(float[] d, float start, float amp, uint seed)
    {
        BandSweep(d, start, 0.2f, amp * 0.34f,
                  180f, 340f, 1700f, 1100f, seed);
        Noise(d, start + 0.12f, 0.08f, amp * 0.26f, 0.46f, 24f, seed + 1);
        Resonance(d, start + 0.13f, 0.06f, 520f, amp * 0.08f, 30f);
    }

    static void WhistlingWind(float[] d, float start, float duration, float amp, uint seed)
    {
        WindBand(d, start, duration, amp * 0.58f, 620f, 980f, 82f, 0.14f, seed);
        WindBand(d, start + 0.14f, duration * 0.82f, amp * 0.3f,
                 960f, 1380f, 105f, 0.2f, seed + 1);
        Noise(d, start, duration, amp * 0.018f, 0.48f, 0.7f, seed + 2);
    }

    static void WindBand(float[] d, float start, float duration, float amp,
                         float f0, float f1, float width, float wobble, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float wide = 0f, narrow = 0f;
        uint state = seed;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float center = Mathf.Lerp(f0, f1, u)
                         * (1f + Mathf.Sin(u * Mathf.PI * 4f) * wobble);
            float highHz = Mathf.Clamp(center + width, 80f, 9000f);
            float lowHz = Mathf.Clamp(center - width, 40f, highHz - 20f);
            float aHigh = 1f - Mathf.Exp(-2f * Mathf.PI * highHz / SampleRate);
            float aLow = 1f - Mathf.Exp(-2f * Mathf.PI * lowHz / SampleRate);
            float white = White(ref state);
            wide += (white - wide) * aHigh;
            narrow += (white - narrow) * aLow;
            float envelope = Mathf.Sin(Mathf.PI * u);
            envelope *= envelope * (0.72f + 0.28f * Mathf.Sin(u * Mathf.PI * 5f));
            d[i] += (wide - narrow) * envelope * amp * 3.2f;
        }
    }

    static void SwordDrawAndSlash(float[] d, float start, float amp, uint seed)
    {
        // 실제 칼날 대 칼날 충돌 녹음을 주음으로 사용한다.
        MixSourceClip(d, BisectMetalSources[0], start,         amp,         1.02f);
        MixSourceClip(d, BisectMetalSources[1], start + 0.008f, amp * 0.35f, 0.96f);

        // 칼날의 선명한 초기 접촉음과 게임용 타격 무게만 최소한으로 보강.
        Noise(d, start, 0.012f, amp * 0.14f, 0.94f, 140f, seed);
        DrumBody(d, start, 0.08f, 165f, 108f, amp * 0.08f);
        Resonance(d, start, 0.11f, 790f,  amp * 0.06f, 14f);
        Resonance(d, start, 0.09f, 1430f, amp * 0.04f, 16f);
    }

    static void MixSourceClip(float[] d, string path, float start,
                              float amp, float playbackRate)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            throw new InvalidOperationException($"실제 금속 충돌 소스를 불러오지 못했습니다: {path}");

        var source = new float[clip.samples * clip.channels];
        if (!clip.GetData(source, 0))
            throw new InvalidOperationException($"금속 충돌 소스를 PCM으로 읽지 못했습니다: {path}");

        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        for (int i = from; i < d.Length; i++)
        {
            float sourceFrame = (i - from) * clip.frequency * playbackRate / SampleRate;
            int frame0 = Mathf.FloorToInt(sourceFrame);
            if (frame0 >= clip.samples - 1) break;

            float sample0 = 0f;
            float sample1 = 0f;
            for (int channel = 0; channel < clip.channels; channel++)
            {
                sample0 += source[frame0 * clip.channels + channel];
                sample1 += source[(frame0 + 1) * clip.channels + channel];
            }

            sample0 /= clip.channels;
            sample1 /= clip.channels;
            d[i] += Mathf.Lerp(sample0, sample1, sourceFrame - frame0) * amp;
        }
    }

    static void SheathScrape(float[] d, float start, float duration,
                             float amp, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float upper = 0f;
        float lower = 0f;
        float roughness = 0f;
        uint state = seed;
        float upperAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 3400f / SampleRate);
        float lowerAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 620f / SampleRate);

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float white = White(ref state);
            upper += (white - upper) * upperAlpha;
            lower += (white - lower) * lowerAlpha;
            roughness += (Mathf.Abs(white) - roughness) * 0.006f;

            float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 18f));
            float release = 1f - Mathf.SmoothStep(0.74f, 1f, u);
            float contact = 0.42f + roughness * 0.9f
                          + 0.16f * Mathf.Sin(u * Mathf.PI * 23f);
            d[i] += (upper - lower) * attack * release * contact * amp;
        }

        Resonance(d, start + 0.015f, 0.07f, 1320f, amp * 0.07f, 24f);
        Resonance(d, start + 0.31f, 0.055f, 1840f, amp * 0.045f, 27f);
    }

    static void BladeCut(float[] d, float start, float duration,
                         float amp, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float upper = 0f;
        float lower = 0f;
        uint state = seed;
        float upperAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 2900f / SampleRate);
        float lowerAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 430f / SampleRate);

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float white = White(ref state);
            upper += (white - upper) * upperAlpha;
            lower += (white - lower) * lowerAlpha;

            float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.16f));
            float release = 1f - Mathf.SmoothStep(0.16f, 1f, u);
            d[i] += (upper - lower) * attack * release * amp;
        }

        Noise(d, start + 0.025f, 0.055f, amp * 0.2f, 0.45f, 30f, seed + 1);
        Resonance(d, start + 0.035f, 0.075f, 1560f, amp * 0.055f, 24f);
    }

    static void BandSweep(float[] d, float start, float duration, float amp,
                          float low0, float low1, float high0, float high1, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float wide = 0f, narrow = 0f;
        uint state = seed;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float lowHz = Mathf.Lerp(low0, low1, u);
            float highHz = Mathf.Lerp(high0, high1, u);
            float aHigh = 1f - Mathf.Exp(-2f * Mathf.PI * highHz / SampleRate);
            float aLow = 1f - Mathf.Exp(-2f * Mathf.PI * lowHz / SampleRate);
            float white = White(ref state);
            wide += (white - wide) * aHigh;
            narrow += (white - narrow) * aLow;
            float envelope = Mathf.Sin(Mathf.PI * u);
            d[i] += (wide - narrow) * envelope * envelope * amp;
        }
    }

    static void SweepResonance(float[] d, float start, float duration,
                               float f0, float f1, float amp, float decay)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        double phase = 0.0;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float hz = Mathf.Lerp(f0, f1, u);
            phase += Math.PI * 2.0 * hz / SampleRate;
            float scrape = 0.68f + 0.32f * Mathf.Sin(u * Mathf.PI * 31f);
            d[i] += Mathf.Sin((float)phase) * Mathf.Exp(-u * decay) * scrape * amp;
        }
    }

    static void DullThud(float[] d, float start, float amp, float depth, uint seed)
    {
        // 50~80 Hz 서브만 키우면 휴대폰·작은 스피커에서 사라진다.
        // 120~280 Hz 몸통을 함께 넣어 '둥'의 크기가 실제로 들리게 한다.
        DrumBody(d, start, 0.38f,
                 Mathf.Lerp(158f, 126f, depth), Mathf.Lerp(84f, 68f, depth), amp * 0.58f);
        DrumBody(d, start + 0.002f, 0.22f,
                 Mathf.Lerp(430f, 360f, depth), Mathf.Lerp(225f, 190f, depth), amp * 0.78f);
        BandSweep(d, start, 0.13f, amp * 0.3f,
                  120f, 180f, 1150f, 760f, seed);
        Noise(d, start + 0.012f, 0.3f, amp * 0.13f, 0.04f, 5.6f, seed + 1);
    }

    static void GroundHit(float[] d, float start, float amp, float depth, uint seed)
    {
        DullThud(d, start, amp, depth, seed);
        SoilCrumble(d, start + 0.012f, 0.58f, amp * 0.34f, seed + 3);
    }

    static void Explosion(float[] d, float start, float amp, float depth, uint seed)
    {
        DullThud(d, start + 0.006f, amp * 1.08f, depth, seed + 1);
        BandSweep(d, start, 0.085f, amp * 0.42f,
                  120f, 180f, 2600f, 1500f, seed);
        Noise(d, start + 0.025f, 0.92f, amp * 0.4f, 0.08f, 4.1f, seed + 2);
        Noise(d, start + 0.08f, 0.5f, amp * 0.18f, 0.38f, 6f, seed + 3);
    }

    static void Dirt(float[] d, float start, float duration, float amp, uint seed)
    {
        Noise(d, start, duration, amp, 0.09f, 4.8f, seed);
        for (int i = 0; i < 6; i++)
        {
            float at = start + 0.025f + Random01(ref seed) * duration * 0.62f;
            Noise(d, at, 0.035f, amp * (0.2f + Random01(ref seed) * 0.18f), 0.42f, 28f, seed + (uint)i);
        }
    }

    static void ArmorHit(float[] d, float start, float amp, uint seed)
    {
        Noise(d, start, 0.055f, amp * 0.7f, 0.76f, 30f, seed);
        Resonance(d, start, 0.085f, 540f, amp * 0.22f, 16f);
        Resonance(d, start + 0.006f, 0.07f, 910f, amp * 0.13f, 19f);
    }

    static void BoneClick(float[] d, float start, float amp, uint seed)
    {
        Noise(d, start, 0.045f, amp, 0.62f, 34f, seed);
        Resonance(d, start, 0.045f, 430f, amp * 0.22f, 24f);
    }

    static void RopeSnap(float[] d, float start, float amp, uint seed)
    {
        Noise(d, start, 0.038f, amp, 0.68f, 40f, seed);
        Noise(d, start + 0.018f, 0.22f, amp * 0.24f, 0.3f, 9f, seed + 1);
    }

    static void ShieldRaise(float[] d, float start, float amp, uint seed)
    {
        // 가죽 손잡이가 당겨지고 금속판이 올라와 걸리는 순서.
        BandSweep(d, start, 0.24f, amp * 0.34f,
                  140f, 260f, 1200f, 1900f, seed);
        ArmorShift(d, start + 0.08f, amp * 0.56f, seed + 1);
        MetalClick(d, start + 0.26f, amp * 0.58f, seed + 2);
        DullThud(d, start + 0.275f, amp * 0.34f, 0.62f, seed + 3);
    }

    static void Shotgun(float[] d, float start, float amp, uint seed)
    {
        Noise(d, start, 0.048f, amp, 0.96f, 42f, seed);
        DullThud(d, start + 0.004f, amp * 0.68f, 0.6f, seed + 1);
        Noise(d, start + 0.025f, 0.24f, amp * 0.28f, 0.36f, 8f, seed + 2);
    }

    static void StoneImpact(float[] d, float start, float amp, uint seed)
    {
        GroundHit(d, start, amp, 0.88f, seed);
        Resonance(d, start + 0.004f, 0.12f, 185f, amp * 0.2f, 18f);
        Resonance(d, start + 0.006f, 0.09f, 330f, amp * 0.12f, 22f);
        for (int i = 0; i < 4; i++)
            Noise(d, start + 0.035f + i * 0.035f, 0.045f,
                  amp * 0.15f, 0.24f, 24f, seed + (uint)(10 + i));
    }

    static void GlassBreak(float[] d, float start, float amp, uint seed)
    {
        BandSweep(d, start, 0.075f, amp * 0.72f,
                  700f, 1200f, 4600f, 3200f, seed);
        Resonance(d, start, 0.11f, 1850f, amp * 0.18f, 20f);
        Resonance(d, start + 0.003f, 0.09f, 3150f, amp * 0.1f, 24f);
        for (int i = 0; i < 10; i++)
        {
            float at = start + 0.018f + Random01(ref seed) * 0.43f;
            float a = amp * (0.13f + Random01(ref seed) * 0.2f);
            Noise(d, at, 0.025f + Random01(ref seed) * 0.035f,
                  a, 0.62f, 38f, seed + (uint)i);
        }
        Noise(d, start + 0.04f, 0.5f, amp * 0.12f, 0.42f, 8f, seed + 20);
    }

    static void LiquidBubble(float[] d, float start, float duration, float amp, uint seed)
    {
        Noise(d, start, duration, amp * 0.24f, 0.04f, 2.8f, seed);
        for (int i = 0; i < 6; i++)
        {
            float at = start + 0.04f + Random01(ref seed) * duration * 0.76f;
            WetPop(d, at, amp * (0.22f + Random01(ref seed) * 0.16f), seed + (uint)(i + 1));
        }
    }

    static void GunChamber(float[] d, float start, float amp, uint seed)
    {
        MetalClick(d, start, amp * 0.62f, seed);
        BandSweep(d, start + 0.045f, 0.2f, amp * 0.28f,
                  280f, 520f, 1800f, 1250f, seed + 1);
        MetalClick(d, start + 0.24f, amp * 0.78f, seed + 2);
    }

    static void MetalClick(float[] d, float start, float amp, uint seed)
    {
        Noise(d, start, 0.032f, amp * 0.85f, 0.86f, 48f, seed);
        Resonance(d, start, 0.07f, 760f, amp * 0.2f, 22f);
        Resonance(d, start + 0.003f, 0.055f, 1270f, amp * 0.1f, 27f);
    }

    static void WetPop(float[] d, float start, float amp, uint seed)
    {
        Noise(d, start, 0.075f, amp, 0.12f, 20f, seed);
        DrumBody(d, start, 0.09f, 92f, 58f, amp * 0.22f);
    }

    static void WetSplatter(float[] d, float start, float amp, uint seed)
    {
        for (int i = 0; i < 7; i++)
            WetPop(d, start + 0.06f + i * 0.065f, amp * (0.42f - i * 0.025f), seed + (uint)(i + 1));
        Noise(d, start + 0.04f, 0.52f, amp * 0.2f, 0.05f, 7f, seed + 20);
    }

    static void PressureCollapse(float[] d, float start, float amp, uint seed)
    {
        Whoosh(d, start, 0.22f, amp * 0.42f, 0.52f, seed);
        Explosion(d, start + 0.12f, amp, 1f, seed + 1);
        Noise(d, start + 0.18f, 0.75f, amp * 0.3f, 0.04f, 3.6f, seed + 2);
    }

    static void Lightning(float[] d, float start, float duration, int count, uint seed)
    {
        float at = start;
        int branches = Mathf.Min(5, count);
        for (int i = 0; i < branches; i++)
        {
            at += i == 0 ? 0f : 0.018f + Random01(ref seed) * 0.052f;
            float a = 0.78f - i * 0.09f;
            Noise(d, at, 0.018f, a, 1f, 58f, seed + (uint)i);
            Noise(d, at + 0.004f, 0.07f, a * 0.28f, 0.86f, 24f, seed + (uint)(20 + i));
        }
        DullThud(d, start + 0.12f, 0.52f, 0.86f, seed + 50);
        Noise(d, start + 0.14f, duration * 0.7f, 0.18f, 0.12f, 5.2f, seed + 51);
    }

    static void BladeWhoosh(float[] d, float start, float duration, float amp, uint seed)
    {
        Whoosh(d, start, duration, amp, 1.55f, seed);
        Noise(d, start + duration * 0.35f, duration * 0.42f, amp * 0.22f, 0.94f, 7f, seed + 1);
    }

    static void HealingAir(float[] d, float start, float duration, float amp, int pulses, uint seed)
    {
        Whoosh(d, start, duration, amp, 0.82f, seed);
        for (int i = 0; i < pulses; i++)
            Noise(d, start + 0.1f + i * 0.16f, 0.13f, amp * 0.2f, 0.7f, 11f, seed + (uint)(i + 1));
    }

    static void Wind(float[] d, float start, float duration, float amp, uint seed)
    {
        Whoosh(d, start, duration, amp, 1.15f, seed);
        Whoosh(d, start + 0.12f, duration * 0.72f, amp * 0.54f, 1.55f, seed + 1);
        Whoosh(d, start + 0.34f, duration * 0.58f, amp * 0.38f, 0.86f, seed + 2);
        Noise(d, start, duration, amp * 0.15f, 0.72f, 1.3f, seed + 3);
    }

    static void Hiss(float[] d, float start, float duration, float amp, uint seed)
    {
        Noise(d, start, duration, amp, 0.9f, 1.8f, seed);
        Noise(d, start + 0.08f, duration * 0.78f, amp * 0.28f, 0.62f, 2.6f, seed + 1);
    }

    static void Fuse(float[] d, float start, float duration, float amp, uint seed)
    {
        Hiss(d, start, duration, amp, seed);
        for (int i = 0; i < 5; i++)
            Noise(d, start + 0.04f + i * 0.05f, 0.018f, amp * 0.3f, 1f, 55f, seed + (uint)(10 + i));
    }

    static void Roar(float[] d, float start, float duration, float amp, uint seed)
    {
        Noise(d, start, duration, amp, 0.18f, 2.6f, seed);
        Noise(d, start + 0.03f, duration * 0.88f, amp * 0.42f, 0.48f, 3.2f, seed + 1);
        Whoosh(d, start, duration, amp * 0.34f, 0.52f, seed + 2);
    }

    static void Suction(float[] d, float start, float duration, float amp, uint seed)
    {
        ReverseBand(d, start, duration, amp, 180f, 2200f, seed);
        ReverseBand(d, start + 0.25f, duration * 0.72f, amp * 0.34f, 520f, 4200f, seed + 1);
    }

    static void FlagSnap(float[] d, float start, float amp, uint seed)
    {
        Whoosh(d, start, 0.42f, amp * 0.42f, 0.92f, seed);
        Noise(d, start + 0.17f, 0.045f, amp, 0.7f, 42f, seed + 1);
        Noise(d, start + 0.22f, 0.24f, amp * 0.24f, 0.28f, 10f, seed + 2);
    }

    static void Whoosh(float[] d, float start, float duration, float amp, float pitch, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float low = 0f;
        uint state = seed;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float white = White(ref state);
            float follow = Mathf.Lerp(0.012f, 0.2f, u) * Mathf.Clamp(pitch, 0.4f, 1.9f);
            low += (white - low) * follow;
            float envelope = Mathf.Sin(Mathf.PI * u);
            d[i] += low * envelope * envelope * amp;
        }
    }

    static void ReverseWhoosh(float[] d, float start, float duration, float amp, float pitch, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float low = 0f;
        uint state = seed;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float white = White(ref state);
            float follow = Mathf.Lerp(0.01f, 0.2f, u) * pitch;
            low += (white - low) * follow;
            float envelope = u * u * (1f - Mathf.SmoothStep(0.82f, 1f, u));
            d[i] += low * envelope * amp;
        }
    }

    static void ReverseBand(float[] d, float start, float duration, float amp,
                            float lowHz, float highHz, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float wide = 0f, narrow = 0f;
        uint state = seed;
        float aHigh = 1f - Mathf.Exp(-2f * Mathf.PI * highHz / SampleRate);
        float aLow = 1f - Mathf.Exp(-2f * Mathf.PI * lowHz / SampleRate);

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float white = White(ref state);
            wide += (white - wide) * aHigh;
            narrow += (white - narrow) * aLow;
            float envelope = u * u * (1f - Mathf.SmoothStep(0.86f, 1f, u));
            d[i] += (wide - narrow) * envelope * amp;
        }
    }

    static void Noise(float[] d, float start, float duration, float amp, float brightness, float decay, uint seed)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        float low = 0f;
        float top = 0f;
        uint state = seed;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float white = White(ref state);
            low += (white - low) * 0.075f;
            top += (white - top) * 0.46f;
            float colored = Mathf.Lerp(low, top - low, brightness);
            float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 80f));
            d[i] += colored * attack * Mathf.Exp(-u * decay) * amp;
        }
    }

    static void DrumBody(float[] d, float start, float duration, float f0, float f1, float amp)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        double phase = 0.0;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            float hz = Mathf.Lerp(f0, f1, Mathf.Sqrt(u));
            phase += Math.PI * 2.0 * hz / SampleRate;
            float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 120f)) * Mathf.Exp(-u * 7.5f);
            d[i] += Mathf.Sin((float)phase) * envelope * amp;
        }
    }

    static void Resonance(float[] d, float start, float duration, float frequency, float amp, float decay)
    {
        int from = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int len = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        int to = Mathf.Min(d.Length, from + len);
        double phase = 0.0;

        for (int i = from; i < to; i++)
        {
            float u = (i - from) / (float)len;
            phase += Math.PI * 2.0 * frequency / SampleRate;
            d[i] += Mathf.Sin((float)phase) * Mathf.Exp(-u * decay) * amp;
        }
    }

    static float White(ref uint state)
    {
        state = state * 1664525u + 1013904223u;
        return ((state >> 8) / 16777215f) * 2f - 1f;
    }

    static float Random01(ref uint state)
    {
        state = state * 1664525u + 1013904223u;
        return (state >> 8) / 16777215f;
    }

    const float PeakCeiling = 0.88f;

    // 원시 RMS가 같아도 고역 노이즈는 훨씬 크게, 100 Hz 아래 충격음은 훨씬
    // 작게 들린다. 재생 체감에 가까운 가중 RMS로 각 팔레트의 목표치를 나눈다.
    static float TargetPerceivedRms(SfxKey key) => key switch
    {
        SfxKey.SKILL_VolleyFire or
        SfxKey.SKILL_Blizzard or
        SfxKey.SKILL_ArrowRain or
        SfxKey.SKILL_PiercingDash              => 0.058f, // 바람·연속 통과음: 약 -24.7 dB

        SfxKey.SKILL_HealAura or
        SfxKey.SKILL_TargetHeal or
        SfxKey.SKILL_SummonSkeleton or
        SfxKey.SKILL_Berserker or
        SfxKey.SKILL_SwiftStrike or
        SfxKey.SKILL_SummonElite or
        SfxKey.SKILL_Bulwark or
        SfxKey.SKILL_WarBanner                 => 0.072f, // 버프·소환: 약 -22.9 dB

        SfxKey.SKILL_HeavyStrike or
        SfxKey.SKILL_LeapStrike or
        SfxKey.SKILL_ChargeSoldier or
        SfxKey.SKILL_Meteor or
        SfxKey.SKILL_SacrificeSoldier or
        SfxKey.SKILL_SuicideSoldier or
        SfxKey.SKILL_Shockwave or
        SfxKey.SKILL_ArrowStorm or
        SfxKey.SKILL_GravityCollapse or
        SfxKey.SKILL_BloodPrice or
        SfxKey.SKILL_Gravestone or
        SfxKey.SKILL_BossCharge or
        SfxKey.SKILL_BossSlam or
        SfxKey.SKILL_BossJumpShockwave         => 0.12f,  // 충격·낙석: 약 -18.4 dB

        SfxKey.SKILL_Bisect                    => 0.09f,  // 유리 파열은 고역이라 한 단계 낮춤
        SfxKey.SKILL_Bisect_Cast               => 0.12f,
        SfxKey.SKILL_GravityCollapse_Cast      => 0.068f,
        _                                      => 0.085f,
    };

    static void Master(float[] d, SfxKey key)
    {
        if (key == SfxKey.SKILL_Bisect_Cast)
            BladeBalance(d);
        else
            WarmBalance(d);

        for (int i = 0; i < d.Length; i++)
            d[i] = (float)Math.Tanh(d[i] * 1.04f);

        float target = TargetPerceivedRms(key);
        float gain = target / Mathf.Max(0.001f, PerceivedGatedRms(d));
        int fade = Mathf.Min(d.Length, Mathf.RoundToInt(SampleRate * 0.025f));
        float peak = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float end = i >= d.Length - fade ? (d.Length - i - 1) / (float)fade : 1f;
            d[i] *= gain * Mathf.Clamp01(end);
            peak = Mathf.Max(peak, Mathf.Abs(d[i]));
        }

        if (peak > PeakCeiling)
        {
            float ceilingGain = PeakCeiling / peak;
            for (int i = 0; i < d.Length; i++) d[i] *= ceilingGain;
        }

        // 게이트 경계에 있던 꼬리 블록이 1차 증폭 뒤 포함되면 측정값이 조금
        // 달라진다. 최종 파일 자체를 다시 측정해 두 번 수렴시킨다.
        for (int pass = 0; pass < 2; pass++)
        {
            float correction = target / Mathf.Max(0.001f, PerceivedGatedRms(d));
            peak = 0f;
            for (int i = 0; i < d.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            if (correction > 1f)
                correction = Mathf.Min(correction, PeakCeiling / Mathf.Max(0.001f, peak));

            for (int i = 0; i < d.Length; i++) d[i] *= correction;
        }
    }

    static void BladeBalance(float[] d)
    {
        float bass = 0f;
        float bodyStage = 0f;
        float body = 0f;
        float bassAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 280f / SampleRate);
        float bodyAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 3400f / SampleRate);

        for (int i = 0; i < d.Length; i++)
        {
            float sample = d[i];
            bass += (sample - bass) * bassAlpha;
            bodyStage += (sample - bodyStage) * bodyAlpha;
            body += (bodyStage - body) * bodyAlpha;
            d[i] = bass + (body - bass) * 1.45f + (sample - body) * 0.16f;
        }
    }

    static void WarmBalance(float[] d)
    {
        float bass = 0f;
        float body = 0f;
        float bassAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 300f / SampleRate);
        float bodyAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 3200f / SampleRate);

        for (int i = 0; i < d.Length; i++)
        {
            float sample = d[i];
            bass += (sample - bass) * bassAlpha;
            body += (sample - body) * bodyAlpha;
            float mid = body - bass;
            float high = sample - body;
            d[i] = bass * 1.14f + mid + high * 0.42f;
        }
    }

    static float PerceivedGatedRms(float[] d)
    {
        int block = Mathf.Max(1, Mathf.RoundToInt(SampleRate * 0.02f));
        double energy = 0.0;
        int samples = 0;
        float bass = 0f;
        float body = 0f;
        float bassAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 180f / SampleRate);
        float bodyAlpha = 1f - Mathf.Exp(-2f * Mathf.PI * 3500f / SampleRate);

        for (int from = 0; from < d.Length; from += block)
        {
            int to = Mathf.Min(d.Length, from + block);
            double blockEnergy = 0.0;
            for (int i = from; i < to; i++)
            {
                float sample = d[i];
                bass += (sample - bass) * bassAlpha;
                body += (sample - body) * bodyAlpha;
                float weighted = bass * 0.52f + (body - bass) + (sample - body) * 1.45f;
                blockEnergy += weighted * weighted;
            }

            int count = to - from;
            float blockRms = Mathf.Sqrt((float)(blockEnergy / count));
            if (blockRms < 0.01f) continue;   // 약 -40 dBFS 이하의 무음 꼬리는 제외

            energy += blockEnergy;
            samples += count;
        }

        return samples > 0 ? Mathf.Sqrt((float)(energy / samples)) : 0f;
    }

    static void ValidateLoudness(float[] d, SfxKey key)
    {
        float targetDb = 20f * Mathf.Log10(TargetPerceivedRms(key));
        float rmsDb = 20f * Mathf.Log10(Mathf.Max(0.000001f, PerceivedGatedRms(d)));
        float peak = 0f;
        for (int i = 0; i < d.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));

        if (rmsDb < targetDb - 4f || rmsDb > targetDb + 1f || peak > PeakCeiling + 0.001f)
            throw new InvalidOperationException(
                $"{key} 음량 정규화 실패: perceived RMS {rmsDb:0.0} dBFS " +
                $"(목표 {targetDb:0.0}) / peak {peak:0.000}");
    }

    static void WriteWave(string path, float[] samples)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        int dataBytes = samples.Length * sizeof(short);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataBytes);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataBytes);

        foreach (float sample in samples)
            writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
    }
}
