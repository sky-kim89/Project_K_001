using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;

// ============================================================
//  AbilityGhostRally.cs  [Special / OnSoldierDeath]
//  C07 혼령 집결 — 병사가 쓰러질 때마다 부대 전체(장군 + 소속 병사)의
//  공격력·최대체력이 누적으로 오른다. 스테이지가 끝나면 사라진다.
//
//  ■ 런 누적이던 것을 전투 내 누적으로 바꿨다 (2026-08-21)
//    예전에는 RunAbilityData.TotalSoldierDeaths(런 내내 쌓이는 카운터)를
//    OnBattleStart 에 읽어 곱했다. 스테이지를 넘길수록 사망자가 계속 쌓여
//    후반에는 배율이 통제 불능이 됐다 — 10스테이지쯤이면 수십 명이라 +100% 를
//    우습게 넘겼다. 지금은 이번 전투에서 죽은 병사만 센다.
//
//  ■ 왜 트리거를 OnSoldierDeath 로 옮겼나
//    OnBattleStart 로는 '스테이지 한정' 이 성립하지 않는다. 전투가 시작되는
//    순간에는 이번 스테이지 사망자가 항상 0 이라 보너스가 영영 0 이다.
//    죽을 때마다 얹어야 이번 판 안에서만 자라고 판이 끝나면 사라진다.
//
//  ■ 희생의 힘(C04)과 무엇이 다른가
//    C04 는 **장군만** 강해진다. 이쪽은 장군과 **소속 병사 전원**이 함께 오른다.
//    쓰러진 동료가 남은 부대에 힘을 보탠다는 그림이라 병사까지 가는 게 맞다.
//
//  ⚠ 스택이 오른 뒤에 새로 나온 병사는 그 몫을 못 받는다
//    버프는 '그 순간 살아 있는 병사' 에게 붙는다. 나중에 보충된 병사에게
//    소급하려면 매 프레임 부대를 훑어야 하는데, 병사가 수십인 오토배틀에서
//    그 비용을 매기느니 새 병사는 그때부터 쌓는 편이 낫다고 봤다.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C07_GhostRally", menuName = "ProjectK/Ability/Special/GhostRally")]
public class AbilityGhostRally : AbilityData
{
    [UnityEngine.Header("혼령 집결 설정")]
    [UnityEngine.Range(0f, 0.2f)]
    [UnityEngine.Tooltip("사망 병사 1명당 공격력·체력 증가 비율 (0.05 = 5%)")]
    public float BonusPerDeath = 0.05f;

    public override string Description
        => $"이번 전투에서 병사가 쓰러질 때마다 부대 전체 공격력·체력 +{BonusPerDeath * 100f:0}%  (전투 내 누적)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnSoldierDeath;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        if (ctx.SoldierDeathCount <= 0) return;

        var em = ctx.EntityManager;

        ApplyBonus(em, ctx.GeneralEntity, ctx.SoldierDeathCount);

        // 소속 병사 전원 — C04 와 갈리는 지점이다
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<SoldierComponent>());
        using var soldiers = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        foreach (var s in soldiers)
        {
            if (!em.HasComponent<SoldierComponent>(s)) continue;
            if (em.GetComponentData<SoldierComponent>(s).GeneralEntity != ctx.GeneralEntity) continue;
            ApplyBonus(em, s, ctx.SoldierDeathCount);
        }
    }

    /// <summary>
    /// 영구(전투 한정) 가산 버프에 이번 사망분을 얹는다.
    ///
    /// ⚠ Base 를 직접 곱하지 않는다
    ///   예전 구현은 stat.Base 와 Final 을 그 자리에서 ×(1+보너스) 했다. 죽을 때마다
    ///   부르면 복리가 되고(1.05 → 1.1025 → …), 무엇보다 Base 를 건드려서
    ///   되돌릴 방법이 없다. StatusEffect 항목 하나에 합산하면 UnitStatusEffectSystem
    ///   이 매 프레임 Base 에서 다시 계산하므로 정확히 선형이고, 전투가 끝나
    ///   버퍼가 비워지면 흔적 없이 사라진다.
    /// </summary>
    void ApplyBonus(EntityManager em, Entity entity, int deaths)
    {
        if (!em.HasBuffer<StatusEffectBufferElement>(entity)) return;
        if (!em.HasComponent<StatComponent>(entity))          return;

        var stat = em.GetComponentData<StatComponent>(entity);
        var buf  = em.GetBuffer<StatusEffectBufferElement>(entity);

        AddOrMerge(buf, StatType.Attack, stat.Base[StatType.Attack] * BonusPerDeath * deaths);
        AddOrMerge(buf, StatType.MaxHp,  stat.Base[StatType.MaxHp]  * BonusPerDeath * deaths);
    }

    void AddOrMerge(DynamicBuffer<StatusEffectBufferElement> buf, StatType stat, float delta)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            var b = buf[i];
            if (b.SourceType != BuffSourceType.Ability) continue;
            if (b.SourceId   != (int)Id)                continue;
            if (b.Stat       != stat)                   continue;

            b.Delta += delta;
            buf[i]   = b;
            return;
        }

        buf.Add(new StatusEffectBufferElement
        {
            Stat       = stat,
            Delta      = delta,
            Mode       = EffectMode.Add,
            // ⚠ Duration = -1 만으로는 영구가 아니다 — Remaining 이 0 이하면 바로 지워진다
            Duration   = -1f,
            Remaining  = float.MaxValue,
            SourceType = BuffSourceType.Ability,
            SourceId   = (int)Id,
        });
    }
}
