using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  TraitTriggerHandlers.cs
//  직업별 특성 중 CombatTriggerSystem 이벤트 기반으로 동작하는 핸들러 구현.
//
//  GeneralRuntimeBridge.AddComponents() 에서 HasTrait 체크 후
//  GeneralTriggerSetComponent.TraitTriggers 에 등록한다.
//
//  [범용 스택 핸들러]
//  TraitStackHandler   — TraitData.StackTrigger 설정에 따라 자동 등록.
//                        트리거 발동 시 RunTraitData 스택 +1 (SoldierDeath 는 사망 수만큼).
//                        누적 스택 × StackStatBonuses 를 StatusEffect 로 즉시 추가.
//
//  [행동 기반 핸들러 (스택 없음)]
//  폭우 사격     — OnAttack       → 주변 적 2명 추가 타격 (70%)
//  마법 집중     — OnAttack       → 스킬 쿨타임 -1초
//  연속 시전     — OnSkillUse     → 동일 스킬 즉시 재발동 (피해 -40%)
// ============================================================

// ── 범용 스택 누적 핸들러 ─────────────────────────────────────
// TraitData.StackTrigger 에 설정된 패시브 트리거로 동작.
// GeneralRuntimeBridge 가 스택 트리거가 있는 특성마다 자동 생성·등록한다.

public class TraitStackHandler : ITraitTriggerHandler
{
    readonly TraitType      _type;
    readonly PassiveTrigger _trigger;

    public TraitStackHandler(TraitType type, PassiveTrigger trigger)
    {
        _type    = type;
        _trigger = trigger;
    }

    public PassiveTrigger GetTriggerType() => _trigger;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var tData = TraitDatabase.Current?.Get(_type);
        if (tData == null || tData.StackStatBonuses == null || tData.StackStatBonuses.Length == 0) return;

        // 이 장군의 UnitEntry 에서 스택 관리 (장군별 독립)
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;
        string unitName = em.HasComponent<UnitPoolLinkComponent>(entity)
            ? em.GetComponentObject<UnitPoolLinkComponent>(entity)?.PoolKey : null;
        if (string.IsNullOrEmpty(unitName)) return;

        var unitEntry = UserDataManager.Instance?.Get<UnitData>()?.GetUnit(unitName);
        if (unitEntry == null) return;

        int delta  = _trigger == PassiveTrigger.OnSoldierDeath ? ctx.SoldierDeathCount : 1;
        int actual = unitEntry.IncrementTraitStack(_type, delta, tData.MaxStacks);
        if (actual <= 0) return;

        UserDataManager.Instance.RequestSave();

        if (!em.HasBuffer<StatusEffectBufferElement>(entity)) return;
        if (!em.HasComponent<StatComponent>(entity)) return;

        var stat = em.GetComponentData<StatComponent>(entity);
        var buf  = em.GetBuffer<StatusEffectBufferElement>(entity);

        foreach (var entry in tData.StackStatBonuses)
        {
            float bonusPer = entry.IsPercent ? stat.Base[entry.Stat] * entry.Value : entry.Value;
            buf.Add(new StatusEffectBufferElement
            {
                Stat       = entry.Stat,
                Delta      = bonusPer * actual,
                Mode       = EffectMode.Add,
                Duration   = -1f,
                Remaining  = -1f,
                SourceType = BuffSourceType.Passive,
                SourceId   = (int)_type,
            });
        }
    }
}

// ── 폭우 사격 ─────────────────────────────────────────────────

public class TraitRainFireHandler : ITraitTriggerHandler
{
    // OnAttackLanded: 근거리·원거리 모두 실제 타격 착탄 시 발동 (ECB → 다음 프레임)
    public PassiveTrigger GetTriggerType() => PassiveTrigger.OnAttackLanded;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;

        if (!em.HasBuffer<AttackHitEvent>(entity))  return;
        if (!em.HasComponent<LocalTransform>(entity)) return;
        if (!em.HasComponent<StatComponent>(entity))  return;

        var  selfPos  = em.GetComponentData<LocalTransform>(entity).Position;
        float atkRange = em.GetComponentData<StatComponent>(entity).Final[StatType.AttackRange];
        float maxRange = atkRange * 1.5f;

        var hitEvents = em.GetBuffer<AttackHitEvent>(entity);

        var query      = ctx.EnemyQuery;
        var targets    = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var identities = query.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);

        // 착탄 이벤트마다 스플래시 처리 (더블 스트라이크 등 복수 착탄 지원)
        foreach (var ev in hitEvents)
        {
            if (ev.Damage <= 0f) continue;

            float  splashDmg      = ev.Damage * 0.7f;
            Vector3 mainTargetPos = (Vector3)ev.TargetPos;

            int hits = 0;
            for (int i = 0; i < targets.Length && hits < 2; i++)
            {
                if (identities[i].Team != TeamType.Enemy) continue;
                if (targets[i] == ev.TargetEntity) continue;

                float dist = math.distance(selfPos.xy, transforms[i].Position.xy);
                if (dist > maxRange) continue;

                if (!em.HasBuffer<HitEventBufferElement>(targets[i])) continue;

                em.GetBuffer<HitEventBufferElement>(targets[i]).Add(new HitEventBufferElement
                {
                    Damage         = splashDmg,
                    AttackerEntity = entity,
                    HitDirection   = math.normalizesafe(transforms[i].Position - selfPos),
                    Type           = HitType.Normal,
                });

                // 착탄 위치(A) → 스플래시 타겟(B) 붉은 전기 체인 이펙트
                SkillEffectHelper.Spawn("FX_RedLightning_Chain",
                    mainTargetPos, (Vector3)transforms[i].Position, 0.35f);

                hits++;
            }
        }

        targets.Dispose();
        transforms.Dispose();
        identities.Dispose();
    }
}

// ── 마법 집중 ─────────────────────────────────────────────────

public class TraitAttackCdrHandler : ITraitTriggerHandler
{
    public PassiveTrigger GetTriggerType() => PassiveTrigger.OnAttack;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;

        if (!em.HasComponent<GeneralActiveSkillComponent>(entity)) return;

        var skill = em.GetComponentData<GeneralActiveSkillComponent>(entity);
        float before = skill.CooldownRemaining;
        skill.CooldownRemaining = math.max(0f, skill.CooldownRemaining - 1f);
        em.SetComponentData(entity, skill);
        UnityEngine.Debug.Log($"[MageAttackCdr] 쿨타임 감소: {before:F1} → {skill.CooldownRemaining:F1}");
    }
}

// ── 연속 시전 ─────────────────────────────────────────────────

public class TraitEchoSkillHandler : ITraitTriggerHandler
{
    const float EchoEffectScale = 0.6f;   // 피해 -40%

    public PassiveTrigger GetTriggerType() => PassiveTrigger.OnSkillUse;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;

        var db = ActiveSkillDatabase.Current;
        if (db == null) return;
        if (!em.HasComponent<GeneralActiveSkillComponent>(entity)) return;
        if (!em.HasComponent<AttackComponent>(entity)) return;

        var skillComp = em.GetComponentData<GeneralActiveSkillComponent>(entity);
        var skillData = db.Get((ActiveSkillId)skillComp.SkillId);
        if (skillData == null) return;

        var attack = em.GetComponentData<AttackComponent>(entity);

        var context = new ActiveSkillContext
        {
            CasterEntity   = entity,
            TargetEntity   = attack.TargetEntity,
            TargetPosition = new Vector3(attack.TargetPosition.x, attack.TargetPosition.y, attack.TargetPosition.z),
            EntityManager  = em,
        };

        if (em.HasComponent<StatComponent>(entity))
            context.CasterStat = em.GetComponentData<StatComponent>(entity);

        if (em.HasComponent<UnitPoolLinkComponent>(entity))
        {
            var link = em.GetComponentObject<UnitPoolLinkComponent>(entity);
            if (link != null && link.LinkedObject != null)
            {
                context.CasterObject    = link.LinkedObject;
                context.CasterTransform = link.LinkedObject.transform;
            }
        }

        // 피해 -40%: EffectValue 를 일시적으로 줄인 뒤 실행
        // Execute() 는 동기로 실행되므로 복원 전 다른 코드 미개입
        float orig = skillData.EffectValue;
        skillData.EffectValue = orig * EchoEffectScale;
        skillData.Execute(context);
        skillData.EffectValue = orig;

        // SkillUseEvent 를 추가하지 않으므로 에코의 에코는 발동되지 않음
    }
}
