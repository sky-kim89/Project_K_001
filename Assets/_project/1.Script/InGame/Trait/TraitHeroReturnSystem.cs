using Unity.Entities;
using UnityEngine;
using BattleGame.Units;
using System.Collections.Generic;

// ============================================================
//  TraitHeroReturnSystem.cs
//  K12 영웅의 귀환: 장군 사망 시 병사를 주변에 새로 소환한다.
//
//  DeadTag 가 붙은 장군 중 TraitHeroReturnComponent 가 있는 유닛을
//  UnitDeathDespawnSystem 처리 이전에 감지하여 병사를 스폰한다.
//  SpawnSoldiers() 는 내부에서 em.RemoveComponent 등 구조적 변경을 수행하므로
//  ForEach 완료 후 호출해야 한다 (UnitDeathDespawnSystem 의 _pending 패턴 동일).
// ============================================================

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitHitSystem))]
[UpdateBefore(typeof(UnitDeathDespawnSystem))]
public partial class TraitHeroReturnSystem : SystemBase
{
    readonly List<GeneralRuntimeBridge> _pending = new();

    protected override void OnUpdate()
    {
        _pending.Clear();
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        Entities
            .WithAll<DeadTag, TraitHeroReturnComponent, GeneralComponent>()
            .WithoutBurst()
            .ForEach((Entity entity, UnitPoolLinkComponent link) =>
            {
                if (link.LinkedObject == null) return;
                if (!link.LinkedObject.TryGetComponent<GeneralRuntimeBridge>(out var bridge)) return;

                _pending.Add(bridge);
                ecb.RemoveComponent<TraitHeroReturnComponent>(entity);
            })
            .Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();

        foreach (var bridge in _pending)
            bridge.SpawnSoldiers();
    }
}
