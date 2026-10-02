// ============================================================
//  GooglePlayAchievementBackend.cs
//  Google Play Games Plugin for Unity (v2, 자동 로그인) 로 업적을 푼다.
//
//  ⚠ 지금은 컴파일되지 않는다 (의도) — 플러그인을 넣고
//    Android 의 Scripting Define 에 PROJECTK_GPGS 를 추가해야 켜진다.
//    순서: Docs/Achievement_Platform.md
//
//  ■ 업적 ID 는 Play Console 이 만든다 (CgkI... 형태)
//    AchievementId → 콘솔 ID 매핑은 GooglePlayAchievementIds.cs 에 채운다.
//    빈 칸은 전송하지 않고 경고만 남긴다.
// ============================================================

#if PROJECTK_GPGS && UNITY_ANDROID
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public sealed class GooglePlayAchievementBackend : IPlatformAchievements
{
    public string Name    => "GooglePlay";
    public bool   IsReady { get; private set; }

    public void Initialize(Action onReady)
    {
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status != SignInStatus.Success)
            {
                Debug.LogWarning($"[GooglePlayAchievementBackend] 로그인 실패 ({status}) — " +
                                 "SHA-1 등록·테스터 계정을 확인할 것. 이번 실행은 로컬에만 기록된다.");
                return;
            }

            IsReady = true;
            onReady();
        });
    }

    public void Unlock(AchievementId id)
    {
        string gid = GooglePlayAchievementIds.Get(id);
        if (string.IsNullOrEmpty(gid))
        {
            Debug.LogWarning($"[GooglePlayAchievementBackend] \"{id}\" 의 콘솔 ID 가 비어 있다 — " +
                             "GooglePlayAchievementIds.cs 를 채울 것.");
            return;
        }

        PlayGamesPlatform.Instance.UnlockAchievement(gid, ok =>
        {
            if (!ok) Debug.LogWarning($"[GooglePlayAchievementBackend] UnlockAchievement 실패 — {id} ({gid})");
        });
    }

    public void Flush() { }   // GPGS 는 호출마다 바로 보낸다
}
#endif
