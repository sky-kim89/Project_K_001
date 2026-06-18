using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using BattleGame.Units;

// ============================================================
//  ActiveHealAura.cs — 치유 오라 (공통)
//
//  피해를 입은 아군 장군 중 랜덤 1명과 그 휘하 병사 전체를 즉시 회복한다.
//  피해를 입은 장군이 없으면 시전자 자신과 소속 병사를 회복한다.
//  회복량 = 각 유닛 MaxHp × EffectValue (비율).
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Active_HealAura", menuName = "BattleGame/Actives/HealAura")]
public class ActiveHealAura : ActiveSkillData
{
    public override void Execute(ActiveSkillContext ctx)
    {
        var em = ctx.EntityManager;
        em.CompleteAllTrackedJobs();

        // ── 피해 입은 아군 장군 목록 수집 ───────────────────────
        var generalQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<GeneralComponent>(),
                                         ComponentType.ReadOnly<UnitIdentityComponent>(),
                                         ComponentType.ReadOnly<HealthComponent>(),
                                         ComponentType.ReadOnly<StatComponent>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });

        NativeArray<Entity>              generals  = generalQuery.ToEntityArray(Allocator.Temp);
        NativeArray<UnitIdentityComponent> gIds    = generalQuery.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);
        NativeArray<HealthComponent>     gHp       = generalQuery.ToComponentDataArray<HealthComponent>(Allocator.Temp);
        NativeArray<StatComponent>       gStat     = generalQuery.ToComponentDataArray<StatComponent>(Allocator.Temp);

        // 피해를 입은(HP < MaxHP) 아군 장군 인덱스만 모음
        var damagedIndices = new NativeList<int>(generals.Length, Allocator.Temp);
        for (int i = 0; i < generals.Length; i++)
        {
            if (gIds[i].Team != TeamType.Ally) continue;
            float maxHp = gStat[i].Final[StatType.MaxHp];
            if (gHp[i].CurrentHp < maxHp - 0.01f)
                damagedIndices.Add(i);
        }

        // 랜덤 선택 (Unity.Mathematics.Random — 시간 기반 시드)
        Entity targetGeneral;
        if (damagedIndices.Length > 0)
        {
            var rng = new Random((uint)UnityEngine.Time.frameCount * 1664525u + 1013904223u);
            int pick = rng.NextInt(0, damagedIndices.Length);
            targetGeneral = generals[damagedIndices[pick]];
        }
        else
        {
            // 피해 입은 장군 없음 → 시전자 대상
            targetGeneral = ctx.CasterEntity;
        }

        damagedIndices.Dispose();
        generals.Dispose();
        gIds.Dispose();
        gHp.Dispose();
        gStat.Dispose();
        generalQuery.Dispose();

        // ── 이펙트 (타겟 장군 위치) ──────────────────────────────
        if (ctx.CasterTransform != null)
            SkillEffectHelper.Spawn(CasterEffectKey, ctx.CasterTransform.position, EffectDespawnDelay);

        // ── 장군 치유 ─────────────────────────────────────────────
        HealUnit(em, targetGeneral, ctx.CasterEntity);

        // ── 장군의 휘하 병사 전체 치유 ──────────────────────────
        var soldierQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All  = new ComponentType[] { ComponentType.ReadOnly<SoldierComponent>() },
            None = new ComponentType[] { typeof(DeadTag) },
        });

        NativeArray<Entity>          soldiers = soldierQuery.ToEntityArray(Allocator.Temp);
        NativeArray<SoldierComponent> sComps  = soldierQuery.ToComponentDataArray<SoldierComponent>(Allocator.Temp);

        for (int i = 0; i < soldiers.Length; i++)
            if (sComps[i].GeneralEntity == targetGeneral)
                HealUnit(em, soldiers[i], ctx.CasterEntity);

        soldiers.Dispose();
        sComps.Dispose();
        soldierQuery.Dispose();
    }

    void HealUnit(EntityManager em, Entity entity, Entity source)
    {
        if (!em.HasComponent<StatComponent>(entity))       return;
        if (!em.HasBuffer<HealEventBufferElement>(entity)) return;

        float maxHp  = em.GetComponentData<StatComponent>(entity).Final[StatType.MaxHp];
        float amount = maxHp * EffectValue;
        em.GetBuffer<HealEventBufferElement>(entity).Add(
            new HealEventBufferElement { Amount = amount, SourceEntity = source });
    }
}
