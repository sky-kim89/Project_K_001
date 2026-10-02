// ============================================================
//  SteamAchievementBackend.cs
//  Steamworks.NET 로 스팀 업적을 푼다.
//
//  ⚠ 지금은 컴파일되지 않는다 (의도) — Steamworks.NET 패키지를 넣고
//    Standalone 의 Scripting Define 에 PROJECTK_STEAM 을 추가해야 켜진다.
//    순서: Docs/Achievement_Platform.md
//
//  ■ 업적 API 이름 = AchievementId 의 enum 이름 그대로 (예: FirstStage, RareHeroBisect)
//    Steamworks 관리 화면에 업적을 등록할 때 API 이름을 이 이름으로 넣는다.
//    ⚠ enum 이름을 바꾸면 스팀 쪽 API 이름도 같이 바꿔야 한다 — 안 그러면 조용히 안 풀린다.
//
//  ■ SDK 1.61+ 는 로그인 시 통계가 자동으로 올라온다 — RequestCurrentStats 를 부르지 않는다.
// ============================================================

#if PROJECTK_STEAM && UNITY_STANDALONE
using System;
using Steamworks;
using UnityEngine;

public sealed class SteamAchievementBackend : IPlatformAchievements
{
    /// <summary>
    /// 스팀 App ID. ⚠ 발급받으면 채울 것 — 0 이면 재시작 검사(RestartAppIfNecessary)를 건너뛴다.
    /// 개발 중에는 프로젝트 루트의 steam_appid.txt 가 같은 값을 가져야 한다.
    /// </summary>
    const uint AppId = 0;

    public string Name    => "Steam";
    public bool   IsReady { get; private set; }

    public void Initialize(Action onReady)
    {
        // 스팀 클라이언트 밖에서 실행됐으면 스팀으로 다시 띄운다 (출시 빌드 전용)
        if (AppId != 0 && !Application.isEditor &&
            SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
        {
            Application.Quit();
            return;
        }

        if (!SteamAPI.Init())
        {
            Debug.LogWarning("[SteamAchievementBackend] SteamAPI.Init 실패 — 스팀 클라이언트가 켜져 있는지, " +
                             "steam_appid.txt 가 있는지 확인할 것. 이번 실행은 로컬에만 기록된다.");
            return;
        }

        // 콜백 펌프 — 씬이 바뀌어도 살아 있어야 한다
        var go = new GameObject("[SteamCallbacks]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<SteamCallbackRunner>();

        IsReady = true;
        onReady();
    }

    public void Unlock(AchievementId id)
    {
        if (!SteamUserStats.SetAchievement(id.ToString()))
            Debug.LogWarning($"[SteamAchievementBackend] SetAchievement 실패 — \"{id}\" 가 Steamworks 에 " +
                             "등록·Publish 됐는지 확인할 것.");
    }

    public void Flush() => SteamUserStats.StoreStats();

    sealed class SteamCallbackRunner : MonoBehaviour
    {
        void Update()            => SteamAPI.RunCallbacks();
        void OnApplicationQuit() => SteamAPI.Shutdown();
    }
}
#endif
