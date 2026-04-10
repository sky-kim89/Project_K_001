using Unity.Entities;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  PassiveSkillApplier.cs
//  패시브 스킬 스폰 시점 즉시 적용 헬퍼.
//
//  역할:
//    PassiveSkillData.StatModifiers 를 순회해
//    ApplyTarget.General / Soldier 항목을 스폰 시 한 번 반영한다.
//
//  ■ 처리 대상
//    ApplyTarget.General → UnitStat.Add("passive") 로 제너럴 스텟 변경
//    ApplyTarget.Soldier → StatComponent.Base 직접 수정
//    ApplyTarget.Runtime → 런타임 이벤트 콜백에서 처리 (여기서는 건너뜀)
//
//  ■ 특수 케이스
//    SoldierEmpower / UnityStrength 는 현재 병사 수에 비례하므로
//    ApplyToGeneralStat() 호출 전 SoldierCount 를 먼저 계산해 전달한다.
// ============================================================

public static class PassiveSkillApplier
{
    // ── 적용 대상 ─────────────────────────────────────────────

    public enum ApplyTarget : byte
    {
        General = 0,    // 제너럴 UnitStat 에 즉시 적용 (SpawnEntity 전)
        Soldier = 1,    // 병사 ECS StatComponent.Base 에 즉시 적용 (Initialize 후)
        Runtime = 2,    // 런타임 이벤트 콜백에서 처리 (여기서는 건너뜀)
    }

    // ── 제너럴 UnitStat 적용 ─────────────────────────────────

    /// <summary>
    /// 활성 패시브 목록을 제너럴 UnitStat 에 즉시 적용한다.
    /// SpawnEntity() 호출 전에 실행해야 한다.
    /// </summary>
    public static void ApplyToGeneralStat(
        UnitStat stat,
        PassiveSkillType[] activePassives,
        PassiveSkillDatabase db)
    {
        if (stat == null || activePassives == null || db == null) return;

        // SoldierEmpower 계열 패시브는 현재 병사 수가 필요하므로 먼저 읽음
        float soldierCountSnapshot = stat.Get(StatType.SoldierCount);

        foreach (var passiveType in activePassives)
        {
            var data = db.Get(passiveType);
            if (data == null) continue;

#if UNITY_EDITOR
            var sbPassive = new System.Text.StringBuilder();
            sbPassive.AppendLine($"[Passive] 스폰 적용 ▶ {data.SkillName} ({data.Type})");
#endif

            foreach (var mod in data.StatModifiers)
            {
                if (mod.Target != ApplyTarget.General) continue;

                float currentValue = stat.Get(mod.Stat);
                float delta        = mod.IsPercent ? currentValue * mod.Delta : mod.Delta;

#if UNITY_EDITOR
                sbPassive.AppendLine($"  {mod.Stat,-14} {currentValue:F1} → {currentValue + delta:F1}  ({(mod.IsPercent ? $"{mod.Delta * 100f:+0.#;-0.#}%" : $"{delta:+0.#;-0.#}")})");
#endif

                stat.Add(mod.Stat, delta, "passive");
            }

#if UNITY_EDITOR
            UnityEngine.Debug.Log(sbPassive.ToString());
#endif
        }
    }

    // ── 병사 ECS Entity 적용 ─────────────────────────────────

    /// <summary>
    /// 활성 패시브 목록을 병사 ECS StatComponent.Base 에 즉시 적용한다.
    /// soldier.Initialize() → SpawnEntity() 이후 시점에 실행해야 한다.
    /// </summary>
    public static void ApplyToSoldierEntity(
        Entity soldierEntity,
        EntityManager em,
        PassiveSkillType[] activePassives,
        PassiveSkillDatabase db)
    {
        if (activePassives == null || db == null) return;
        if (soldierEntity == Entity.Null || !em.Exists(soldierEntity)) return;
        if (!em.HasComponent<StatComponent>(soldierEntity)) return;

        var  stat     = em.GetComponentData<StatComponent>(soldierEntity);
        bool modified = false;

        foreach (var passiveType in activePassives)
        {
            var data = db.Get(passiveType);
            if (data == null) continue;

            foreach (var mod in data.StatModifiers)
            {
                if (mod.Target != ApplyTarget.Soldier) continue;

                float delta = mod.IsPercent
                    ? stat.Base[mod.Stat] * mod.Delta
                    : mod.Delta;

                stat.Base[mod.Stat] += delta;
                modified = true;
            }
        }

        if (!modified) return;

        // Final 도 동기화 (UnitStatusEffectSystem 이 다음 프레임에 재계산)
        stat.Final = stat.Base;
        em.SetComponentData(soldierEntity, stat);

        // MaxHp 가 변경되었을 수 있으므로 체력 리셋
        em.SetComponentData(soldierEntity, new HealthComponent
        {
            CurrentHp = stat.Base[StatType.MaxHp],
        });
    }

    // ── 제너럴 크기 배율 ─────────────────────────────────────

    /// <summary>
    /// 활성 패시브 중 GeneralScaleBonusAdd 합산을 반환.
    /// TitanGeneral 등 크기 변경 패시브에 사용.
    /// 없으면 1.0f 반환.
    /// </summary>
    public static float GetGeneralScaleMultiplier(
        PassiveSkillType[] activePassives,
        PassiveSkillDatabase db)
    {
        if (activePassives == null || db == null) return 1f;

        float totalBonus = 0f;
        foreach (var passiveType in activePassives)
        {
            var data = db.Get(passiveType);
            if (data == null) continue;
            totalBonus += data.GeneralScaleBonusAdd;
        }
        return 1f + totalBonus;
    }

    // ── 내부 유틸리티 ────────────────────────────────────────

    /// <summary>activePassives 배열에 특정 타입이 있는지 확인한다.</summary>
    public static bool HasPassive(PassiveSkillType[] activePassives, PassiveSkillType type)
    {
        if (activePassives == null) return false;
        foreach (var p in activePassives)
            if (p == type) return true;
        return false;
    }
}
