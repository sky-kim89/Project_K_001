// Copy this file to another Unity 6 project to reuse the same release baseline.
// Product name, version and application identifiers intentionally remain project-owned.

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

public static class ReleasePlayerSettings
{
    public static void ApplyAll()
    {
        ApplySteam();
        ApplyGooglePlay();
        Validate();
    }

    public static void ApplySteam()
    {
        ApplyShared();

        NamedBuildTarget target = NamedBuildTarget.Standalone;
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
        PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSpeed);

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { GraphicsDeviceType.Direct3D11 });

        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.allowFullscreenSwitch = true;
        PlayerSettings.forceSingleInstance = true;
        PlayerSettings.visibleInBackground = false;

        Save("Steam");
    }

    public static void ApplyGooglePlay()
    {
        ApplyShared();

        NamedBuildTarget target = NamedBuildTarget.Android;
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
        PlayerSettings.SetIl2CppCompilerConfiguration(target, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSpeed);

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.Android.resizeableActivity = true;
        PlayerSettings.Android.renderOutsideSafeArea = false;
        PlayerSettings.Android.optimizedFramePacing = true;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
        PlayerSettings.Android.blitType = AndroidBlitType.Auto;
        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = false;

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.buildAppBundle = true;
        if (!string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            PlayerSettings.Android.useCustomKeystore = true;

        Save("Google Play");
    }

    public static void Validate()
    {
        int issues = 0;
        Need(PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevel36,
            "Android Target API must be 36.", ref issues);
        Need(PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel26,
            "Android Minimum API must be 26.", ref issues);
        Need(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64,
            "Android architecture must be ARM64 only.", ref issues);
        Need(EditorUserBuildSettings.buildAppBundle,
            "Android release output must be an App Bundle (AAB).", ref issues);
        Need(PlayerSettings.Android.useCustomKeystore &&
             !string.IsNullOrEmpty(PlayerSettings.Android.keystoreName),
            "Configure a private Android upload keystore; passwords must stay outside source control.", ref issues);
        Need(HasAndroidPlatformIcon(),
            "Assign Android adaptive and legacy icons in Player Settings.", ref issues);
        Need(IsReleaseIdentifier(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)),
            "Finalize a lowercase Android package name without underscores.", ref issues);
        Need(!PlayerSettings.productName.Contains("_", StringComparison.Ordinal),
            "Finalize the public product name without underscores.", ref issues);
        Need(PlayerSettings.bundleVersion != "0.0.1",
            "Replace the development bundle version before release.", ref issues);

        // 플랫폼 업적 연동 — 순서는 Docs/Achievement_Platform.md
        Need(HasDefine(NamedBuildTarget.Standalone, "PROJECTK_STEAM"),
            "Steam achievements are not connected (install Steamworks.NET, add PROJECTK_STEAM).", ref issues);
        Need(HasDefine(NamedBuildTarget.Android, "PROJECTK_GPGS"),
            "Google Play achievements are not connected (install GPGS plugin, add PROJECTK_GPGS).", ref issues);
        Need(GooglePlayAchievementIds.MissingCount() == 0,
            $"Fill {GooglePlayAchievementIds.MissingCount()} Play Console achievement ID(s) in GooglePlayAchievementIds.cs.", ref issues);
        Need(!HasDefine(NamedBuildTarget.Standalone, "PROJECTK_PLATFORM_ACH_TEST") &&
             !HasDefine(NamedBuildTarget.Android,    "PROJECTK_PLATFORM_ACH_TEST"),
            "Remove PROJECTK_PLATFORM_ACH_TEST before release.", ref issues);

        if (issues == 0)
            Debug.Log("[Release Settings] Steam and Google Play settings are ready.");
        else
            Debug.LogWarning($"[Release Settings] {issues} manual release item(s) remain.");
    }

    static void ApplyShared()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.gcIncremental = true;
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
        PlayerSettings.runInBackground = false;

        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
    }

    static bool HasDefine(NamedBuildTarget target, string symbol)
    {
        PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
        return Array.IndexOf(defines, symbol) >= 0;
    }

    static bool HasAndroidPlatformIcon()
    {
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        foreach (PlatformIcon icon in PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind))
        foreach (Texture2D texture in icon.GetTextures())
            if (texture != null) return true;

        return false;
    }

    static bool IsReleaseIdentifier(string value)
        => value.IndexOf('.') > 0 &&
           value.IndexOf('_') < 0 &&
           value == value.ToLowerInvariant() &&
           value.IndexOf("defaultcompany", StringComparison.OrdinalIgnoreCase) < 0;

    static void Need(bool condition, string message, ref int issues)
    {
        if (condition) return;
        issues++;
        Debug.LogWarning($"[Release Settings] {message}");
    }

    static void Save(string platform)
    {
        AssetDatabase.SaveAssets();
        Debug.Log($"[Release Settings] Applied {platform} release defaults.");
    }
}
