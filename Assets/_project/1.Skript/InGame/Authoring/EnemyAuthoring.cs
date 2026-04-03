using Unity.Entities;

// ============================================================
//  EnemyAuthoring.cs
//  일반 적 유닛 전용 Authoring
//
//  - 장군-병사 계층 없이 독립적으로 전투하는 기본 적 유닛
//  - 추가 전용 컴포넌트 없음 (필요 시 EnemyComponent 를 추가할 것)
// ============================================================

namespace BattleGame.Units
{
    public class EnemyAuthoring : UnitAuthoring { }

    public class EnemyBaker : UnitBakerBase<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            BakeCommon(authoring, entity, UnitType.Enemy);
        }
    }
}
