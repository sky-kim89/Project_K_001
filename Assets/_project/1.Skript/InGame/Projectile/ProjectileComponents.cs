using Unity.Entities;
using Unity.Mathematics;

// ============================================================
//  ProjectileComponents.cs
//  기본 공격 발사체 전용 ECS 컴포넌트 정의.
//
//  흐름:
//    ① RangedAttackJob         — 원거리 공격 발생 시 자신의 ProjectileLaunchRequest 버퍼에 추가
//    ② ProjectileSpawnSystem   — 버퍼를 읽어 GO 풀에서 꺼내고 ProjectileView.Launch() 호출
//    ③ ProjectileMoveJob       — LocalTransform 갱신, 타겟 위치 추적 (Burst 병렬)
//    ④ ProjectileHitJob        — 거리 체크 → HitEventBuffer append + DestroyTag 추가 (Burst 병렬)
//    ⑤ ProjectileDestroySystem — DestroyTag 감지 → PoolController 반납
//                                → EntityLink.OnDisable → entity Disabled (기존 패턴 재사용)
// ============================================================

namespace BattleGame.Projectiles
{
    // ── 비행 중 발사체 데이터 ─────────────────────────────────────
    // 발사 요청 버퍼(ProjectileLaunchRequest)는 BattleGame.Units(UnitComponents.cs) 에 정의.
    public struct ProjectileComponent : IComponentData
    {
        public Entity   TargetEntity;
        public float3   TargetPos;   // 타겟 마지막 위치 캐시 (사망 후에도 계속 날아감)
        public float    Damage;
        public float    Speed;
        public float    Lifetime;    // 남은 유효 시간 — 0 이하 시 소멸
        public TeamType Team;
    }

    // ── GO 링크 (managed component) ───────────────────────────────
    // ProjectileDestroySystem 이 읽어서 PoolController.Despawn() 호출.
    public class ProjectileGoLink : IComponentData
    {
        public UnityEngine.GameObject Go;
        public string                 PoolKey;
    }

    // ── 소멸 요청 태그 ────────────────────────────────────────────
    // ProjectileHitJob(타격) 또는 ProjectileMoveJob(Lifetime 만료) 이 추가.
    // ProjectileDestroySystem 이 프레임 내에 처리.
    public struct ProjectileDestroyTag : IComponentData { }
}
