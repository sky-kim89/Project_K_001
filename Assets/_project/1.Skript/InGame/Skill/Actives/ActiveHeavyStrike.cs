using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  ActiveHeavyStrike.cs
//  강타(HeavyStrike) 액티브 스킬 구현.
//
//  동작 순서:
//    1. 사용자 GameObject 가 타겟 방향으로 돌진 (트윈 이동)
//    2. 돌진 도착 콜백 → ECS HitEventBufferElement 추가 (데미지 + 강한 넉백)
//    3. 사용자 GameObject 원위치 복귀 (트윈 이동)
//
//  Inspector 설정:
//    EffectValue : 기본 공격력 배율 (예: 3.0 = 공격력 × 3)
//    DashSpeed   : 돌진 이동 속도 (유닛/초)
//    ReturnSpeed : 복귀 이동 속도 (유닛/초)
//    KnockbackMult: 넉백 배율 (기본 HitSystem 넉백 × 이 값)
//
//  트윈 라이브러리 의존:
//    현재 코루틴 기반으로 구현. DOTween 등 별도 라이브러리 도입 시 교체 용이.
// ============================================================

[CreateAssetMenu(fileName = "Active_HeavyStrike", menuName = "BattleGame/Actives/HeavyStrike")]
public class ActiveHeavyStrike : ActiveSkillData
{
    [Header("강타 설정")]
    [Tooltip("기본 공격력 배율 (예: 3.0 → 공격력 × 3 데미지)")]
    public float DamageMultiplier = 3f;

    [Tooltip("타겟 방향 돌진 속도 (유닛/초)")]
    public float DashSpeed = 20f;

    [Tooltip("원위치 복귀 속도 (유닛/초)")]
    public float ReturnSpeed = 12f;

    [Tooltip("넉백 배율 (UnitHitSystem 의 기본 넉백 × 이 값)")]
    public float KnockbackMult = 5f;

    // ─────────────────────────────────────────────────────────

    public override void Execute(ActiveSkillContext context)
    {
        if (!context.HasTarget) return;
        if (!context.EntityManager.Exists(context.TargetEntity)) return;
        if (context.CasterObject == null) return;

        // 타겟 위치 계산
        UnityEngine.Vector3 targetPos = UnityEngine.Vector3.zero;
        if (context.EntityManager.HasComponent<LocalTransform>(context.TargetEntity))
        {
            var lt = context.EntityManager.GetComponentData<LocalTransform>(context.TargetEntity);
            targetPos = new UnityEngine.Vector3(lt.Position.x, lt.Position.y, lt.Position.z);
        }

        // 코루틴 실행 (MonoBehaviour 없이 코루틴은 불가하므로 HeavyStrikeRunner 를 활용)
        var runner = context.CasterObject.GetComponent<HeavyStrikeRunner>();
        if (runner == null)
            runner = context.CasterObject.AddComponent<HeavyStrikeRunner>();

        runner.Run(
            casterTransform : context.CasterTransform,
            targetPos        : targetPos,
            targetEntity     : context.TargetEntity,
            casterEntity     : context.CasterEntity,
            casterStat       : context.CasterStat,
            em               : context.EntityManager,
            damageMultiplier : DamageMultiplier * EffectValue,
            dashSpeed        : DashSpeed,
            returnSpeed      : ReturnSpeed,
            knockbackMult    : KnockbackMult,
            fx               : new SkillEffectConfig
            {
                CasterEffectKey = CasterEffectKey,
                TargetEffectKey = TargetEffectKey,
                BaseEffectKey   = BaseEffectKey,
                DespawnDelay    = EffectDespawnDelay,
            }
        );
    }
}
