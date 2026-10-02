using UnityEditor;
using Unity.Notifications;
using UnityEngine;

public static class ProjectKReleaseMenu
{
    [MenuItem(ProjectKMenu.Release + "Steam + Google Play 권장값 적용",
        priority = ProjectKMenu.ReleasePrio)]
    static void ApplyAll()
    {
        ReleasePlayerSettings.ApplyAll();
        ApplyAndroidNotificationDefaults();
    }

    [MenuItem(ProjectKMenu.Release + "Steam 권장값 적용",
        priority = ProjectKMenu.ReleasePrio + 11)]
    static void ApplySteam() => ReleasePlayerSettings.ApplySteam();

    [MenuItem(ProjectKMenu.Release + "Google Play 권장값 적용",
        priority = ProjectKMenu.ReleasePrio + 12)]
    static void ApplyGooglePlay()
    {
        ReleasePlayerSettings.ApplyGooglePlay();
        ApplyAndroidNotificationDefaults();
    }

    [MenuItem(ProjectKMenu.Release + "출시 설정 점검",
        priority = ProjectKMenu.ReleasePrio + 30)]
    static void Validate() => ReleasePlayerSettings.Validate();

    public static void ApplyAndroidNotificationDefaults()
    {
        NotificationSettings.AndroidSettings.RescheduleOnDeviceRestart = true;
        NotificationSettings.AndroidSettings.ExactSchedulingOption =
            AndroidExactSchedulingOption.ExactWhenAvailable;
        Debug.Log("[Release Settings] Applied Android local notification defaults.");
    }
}
