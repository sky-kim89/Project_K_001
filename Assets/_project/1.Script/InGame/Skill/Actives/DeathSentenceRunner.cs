using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  DeathSentenceRunner.cs
//  사형 선고(DeathSentence)의 낙인 → 처형 시퀀스.
//
//    ① 낙인 : 범위 내 적 전원 머리 위에 표식 (markDuration 동안 유지)
//    ② 처형 : 낙인이 찍힌 적을 다시 훑어
//             체력 비율 ≤ executeRatio → 즉사, 아니면 큰 피해
//    ③ 보상 : 처형한 수만큼 시전자 공격력 누적 (전투 내 영구)
//
//  ⚠ 즉사를 DeadTag 로 직접 처리하지 않는다
//    그러면 킬 수·전투 통계·처치 트리거(KillEmpower 등)가 전부 누락된다.
//    방어율을 역산해 "확실히 죽는 피해" 를 넣어 UnitHitSystem 이 죽이게 한다.
//
//  ⚠ 낙인 시점이 아니라 처형 시점의 체력으로 판정한다
//    2초 사이에 아군이 깎아 놓으면 그것도 처형 대상이 된다 — 마무리 기술의 맛이다.
// ============================================================

public class DeathSentenceRunner : MonoBehaviour
{
    public void Run(EntityManager em, Entity casterEntity, TeamType casterTeam,
                    float3 center, float radius, float markDuration, float executeRatio,
                    float damage, float attackPerKill, bool executeBosses, float knockMult,
                    SkillEffectConfig fx)
    {
        StartCoroutine(Sequence(em, casterEntity, casterTeam, center, radius, markDuration,
                                executeRatio, damage, attackPerKill, executeBosses,
                                knockMult, fx));
    }

    IEnumerator Sequence(EntityManager em, Entity casterEntity, TeamType casterTeam,
                         float3 center, float radius, float markDuration, float executeRatio,
                         float damage, float attackPerKill, bool executeBosses, float knockMult,
                         SkillEffectConfig fx)
    {
        var centerPos = new Vector3(center.x, center.y, 0f);

        // ── ① 낙인 ───────────────────────────────────────────
        // 범위 표시 (프리팹 기준 반경 3)
        SkillEffectHelper.Spawn(fx.BaseEffectKey, centerPos, markDuration + fx.DespawnDelay,
                                Quaternion.identity, radius / 3f);

        em.CompleteAllTrackedJobs();
        var marked = SkillCrowdControl.CollectEnemiesInRadius(em, center, radius, casterTeam);

        foreach (var t in marked)
        {
            if (!em.Exists(t)) continue;
            SkillEffectHelper.Spawn(fx.CasterEffectKey, SkillCrowdControl.PositionOf(em, t),
                                    markDuration + 0.3f);
        }

        if (markDuration > 0f) yield return new WaitForSeconds(markDuration);

        // ── ② 처형 ───────────────────────────────────────────
        em.CompleteAllTrackedJobs();

        int executed = 0;

        foreach (var t in marked)
        {
            if (!em.Exists(t)) continue;
            if (!em.HasComponent<HealthComponent>(t) || !em.HasComponent<StatComponent>(t)) continue;

            var  stat    = em.GetComponentData<StatComponent>(t);
            var  health  = em.GetComponentData<HealthComponent>(t);
            float maxHp  = math.max(1f, stat.Final[StatType.MaxHp]);
            float ratio  = health.CurrentHp / maxHp;

            bool isBoss     = em.HasComponent<BossComponent>(t);
            bool canExecute = ratio <= executeRatio && (executeBosses || !isBoss);

            Vector3 tp  = SkillCrowdControl.PositionOf(em, t);
            float3  dir = new float3(tp.x - center.x, tp.y - center.y, 0f);

            if (canExecute)
            {
                SkillCrowdControl.DealDamage(em, t, LethalDamage(health.CurrentHp, stat),
                                             dir, knockMult, casterEntity);
                executed++;
            }
            else
            {
                SkillCrowdControl.DealDamage(em, t, damage, dir, knockMult, casterEntity);
            }

            SkillEffectHelper.Spawn(fx.TargetEffectKey, tp, fx.DespawnDelay,
                                    Quaternion.identity, canExecute ? 1.4f : 1f);
        }

        // ── ③ 처형 보상 ──────────────────────────────────────
        if (executed > 0 && attackPerKill > 0f)
            SkillCrowdControl.AddEffect(em, casterEntity, StatType.Attack,
                                        attackPerKill * executed, EffectMode.Add,
                                        -1f, ActiveSkillId.DeathSentence);   // -1 = 전투 내 영구
    }

    /// <summary>
    /// 방어율을 역산해 반드시 죽는 피해량을 구한다.
    /// UnitHitSystem 이 (1 - 방어율) 을 곱하므로 그만큼 부풀려야 정확히 죽는다.
    /// 과하게 넣으면 전투 통계의 총 딜량이 부풀어 오른다.
    /// </summary>
    static float LethalDamage(float currentHp, StatComponent stat)
    {
        float defense = math.clamp(stat.Final[StatType.Defense], 0f, 0.95f);
        return currentHp / math.max(0.05f, 1f - defense) + 1f;
    }
}
