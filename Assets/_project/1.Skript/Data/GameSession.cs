// ============================================================
//  GameSession.cs
//  씬을 넘나드는 현재 게임 진행 상태를 보관하는 순수 C# 싱글톤.
//  MonoBehaviour 없이 어디서든 접근 가능.
// ============================================================

public class GameSession : SingletonPure<GameSession>
{
    /// <summary>로비에서 선택한 스테이지. InGameManager 가 읽어 배틀을 시작한다.</summary>
    public StageData CurrentStage { get; set; }

    public bool HasStage => CurrentStage != null;
}
