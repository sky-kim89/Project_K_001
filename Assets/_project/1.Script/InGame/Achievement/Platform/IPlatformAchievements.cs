using System;

// ============================================================
//  IPlatformAchievements.cs
//  플랫폼 업적(Steam · Google Play Games) 한 곳을 감싸는 인터페이스.
//
//  ■ 정본은 언제나 로컬 세이브(AchievementData)다
//    플랫폼은 "달성했다" 를 전달받는 쪽일 뿐 판정하지 않는다.
//    그래서 오프라인·로그인 실패로 전송이 빠져도 다음 실행의 재동기화가 메운다.
//
//  ■ 구현체는 빌드 타겟별로 하나만 컴파일된다 (PlatformAchievements.CreateBackend)
//    PROJECTK_STEAM  + Standalone → SteamAchievementBackend
//    PROJECTK_GPGS   + Android    → GooglePlayAchievementBackend
//    그 외(심볼 없음·에디터 등)   → NullAchievementBackend
// ============================================================

public interface IPlatformAchievements
{
    /// <summary>로그 표기용 이름.</summary>
    string Name { get; }

    /// <summary>SDK 초기화·로그인이 끝나 Unlock 을 받을 수 있는가.</summary>
    bool IsReady { get; }

    /// <summary>
    /// SDK 를 켠다. 로그인처럼 비동기인 플랫폼은 끝난 뒤 onReady 를 부른다
    /// (실패하면 부르지 않는다 — 이번 실행은 로컬에만 남고 다음 실행에 재동기화된다).
    /// </summary>
    void Initialize(Action onReady);

    /// <summary>업적 하나를 달성 처리한다. 이미 달성된 업적을 다시 보내도 무해해야 한다.</summary>
    void Unlock(AchievementId id);

    /// <summary>모아 둔 변경을 서버로 보낸다 (Steam 의 StoreStats). 필요 없는 플랫폼은 비워 둔다.</summary>
    void Flush();
}

/// <summary>연동 전 기본값 — 아무것도 보내지 않는다.</summary>
public sealed class NullAchievementBackend : IPlatformAchievements
{
    public string Name    => "None";
    public bool   IsReady => false;

    public void Initialize(Action onReady) { }
    public void Unlock(AchievementId id)   { }
    public void Flush()                    { }
}
