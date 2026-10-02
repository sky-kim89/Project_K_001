using UnityEngine;

// ============================================================
//  PlatformAchievements.cs
//  로컬 업적(AchievementData) → 플랫폼 업적(Steam · Google Play) 다리.
//
//  ■ 흐름
//    ① 앱 시작(BeforeSceneLoad)  — 빌드 타겟에 맞는 백엔드를 만들고 SDK 를 켠다
//    ② 로비 시작(LobbyManager)    — AchievementTracker.EvaluateAll 뒤에 SyncAll()
//                                   → 로컬에 달성된 업적 전체를 다시 보낸다 (재동기화)
//    ③ 플레이 중                  — AchievementData.OnUnlocked 를 받아 즉시 전송
//
//  ■ 왜 매번 전부 다시 보내나
//    오프라인에서 달성한 것, 로그인 실패로 빠진 것, 소급 판정으로 풀린 것이
//    전부 여기서 메워진다. 두 플랫폼 모두 이미 달성된 업적을 다시 보내도 무해하다.
//
//  ⚠ 개발 빌드·에디터에서는 전송하지 않는다 (로그만 남긴다)
//    치트로 푼 업적이 실제 계정에 올라가면 되돌리기 어렵다 — 특히 Google 은 출시 후 초기화 불가.
//    연동 테스트 때만 Scripting Define 에 PROJECTK_PLATFORM_ACH_TEST 를 넣어 연다.
//
//  ■ 연동 순서는 Docs/Achievement_Platform.md 에 있다.
// ============================================================

public static class PlatformAchievements
{
    static IPlatformAchievements _backend;
    static bool                  _dataReady;   // SyncAll 이 한 번 불렸다 = 세이브를 읽어도 된다

    /// <summary>지금 실제로 플랫폼에 보내는가. 개발 빌드·에디터는 막는다.</summary>
    static bool CanSend
    {
        get
        {
#if PROJECTK_PLATFORM_ACH_TEST
            return true;
#else
            return !Debug.isDebugBuild;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        _backend   = CreateBackend();
        _dataReady = false;

        AchievementData.OnUnlocked -= HandleUnlocked;
        AchievementData.OnUnlocked += HandleUnlocked;

        // 로그인이 로비보다 늦게 끝나면(GPGS) 그때 재동기화한다
        _backend.Initialize(() =>
        {
            Debug.Log($"[PlatformAchievements] {_backend.Name} 준비 완료");
            if (_dataReady) SendAllUnlocked();
        });
    }

    static IPlatformAchievements CreateBackend()
    {
#if PROJECTK_STEAM && UNITY_STANDALONE
        return new SteamAchievementBackend();
#elif PROJECTK_GPGS && UNITY_ANDROID
        return new GooglePlayAchievementBackend();
#else
        return new NullAchievementBackend();
#endif
    }

    /// <summary>
    /// 로컬에 달성된 업적 전체를 플랫폼에 다시 보낸다.
    /// LobbyManager.Start 가 AchievementTracker.EvaluateAll 직후에 부른다 (세이브·DB 가 준비된 뒤).
    /// </summary>
    public static void SyncAll()
    {
        _dataReady = true;
        if (_backend.IsReady) SendAllUnlocked();
    }

    static void SendAllUnlocked()
    {
        var data = UserDataManager.Instance.Get<AchievementData>();
        int sent = 0;

        foreach (var def in AchievementCatalog.All)
        {
            if (!data.IsUnlocked(def.Id)) continue;
            if (Send(def.Id)) sent++;
        }

        if (sent > 0) _backend.Flush();
        Debug.Log($"[PlatformAchievements] {_backend.Name} 재동기화 — {sent}개");
    }

    static void HandleUnlocked(AchievementId id)
    {
        if (!_backend.IsReady) return;   // 준비 전 달성분은 준비되면 SendAllUnlocked 가 보낸다
        if (Send(id)) _backend.Flush();
    }

    static bool Send(AchievementId id)
    {
        if (!CanSend)
        {
            Debug.Log($"[PlatformAchievements] (개발 빌드 — 전송 생략) {_backend.Name} ← {id}");
            return false;
        }

        _backend.Unlock(id);
        return true;
    }
}
