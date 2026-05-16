using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using BattleGame.Units;

// ============================================================
//  LeadAllyTrackerSystem.cs
//  매 프레임 아군 중 가장 오른쪽에 있는 유닛의 X 좌표를 추적한다.
//  BattleScrollManager 가 이 값을 읽어 카메라 스크롤에 사용한다.
// ============================================================

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitMovementSystem))]
public partial class LeadAllyTrackerSystem : SystemBase
{
    const float NoAlly = -99999f;

    /// <summary>현재 프레임 기준 가장 오른쪽 아군의 월드 X 좌표. 아군이 없으면 -99999.</summary>
    public static float LeadX { get; private set; } = NoAlly;

    protected override void OnUpdate()
    {
        float maxX = NoAlly;
        foreach (var (identity, tf) in
            SystemAPI.Query<RefRO<UnitIdentityComponent>, RefRO<LocalTransform>>()
                     .WithNone<DeadTag>())
        {
            if (identity.ValueRO.Team == TeamType.Ally)
            {
                float x = tf.ValueRO.Position.x;
                if (x > maxX) maxX = x;
            }
        }
        LeadX = maxX;
    }
}
