using Unity.Entities;
using UnityEngine;

// ============================================================
//  EntityLink.cs
//  GameObject ↔ ECS Entity 연결용 경량 컴포넌트.
//  (Unity.Entities.EntityReference 와 이름 충돌 방지를 위해 EntityLink 사용)
//
//  사용법:
//  1. 유닛 프리팹 루트에 추가한다.
//  2. EntityLinkBaker 가 Bake 시 자동으로 Entity 를 기록한다.
//  3. GeneralRuntimeBridge / EnemyRuntimeBridge 에서 읽는다.
// ============================================================

public class EntityLink : MonoBehaviour
{
    [HideInInspector] public Entity Entity;
}

public class EntityLinkBaker : Baker<EntityLink>
{
    public override void Bake(EntityLink authoring)
    {
        authoring.Entity = GetEntity(TransformUsageFlags.Dynamic);
    }
}
