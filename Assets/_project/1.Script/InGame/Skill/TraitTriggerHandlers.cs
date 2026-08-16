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
//  순교          — OnSoldierDeath → 병사 사망 지점 폭발 (장군 공격력 80%, 반경 2)
//  파쇄          — OnAttackLanded → 대상 최대 체력 비례 추가 피해 (공속 빌드 축)
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
//  착탄한 적을 기점으로 "주변" 적 2명이 함께 맞는다.
//
//  ■ 대상 선정은 반드시 착탄 지점 기준이어야 한다
//    예전에는 장군 위치에서 "공격 사거리 × 1.5" 안의 적을 훑었다. 그래서
//      · 궁수(사거리 9) 는 반경 13.5 = 사실상 전장 전체가 후보였고
//      · 그중 거리순도 아닌 쿼리(청크) 순서대로 앞의 2명을 집었다
//    결과적으로 착탄한 적과 아무 상관없는 적이 맞고, 전기 체인이
//    화면을 가로지르는 긴 작대기로 그려졌다 (근접 장군은 자기 발밑에서 선이 뻗어 나감).
//
//  ■ 지금은 착탄 지점에서 SplashRadius 안의 "가장 가까운" 2명을 고른다.
//    번개가 짧게 튀고, 아군이 많아 화면이 붐벼도 어디서 어디로 튀었는지 읽힌다.

//  ■ 장군과 병사가 같은 코드를 쓴다
//    장군은 CombatTriggerSystem → TraitRainFireHandler 경로,
//    병사는 GeneralTriggerSetComponent 가 없어 그 경로를 못 타므로
//    TraitRainFireSoldierSystem 이 따로 돈다. 스플래시 규칙은 아래 RainFireSplash 하나뿐이다.

/// <summary>폭우 사격 스플래시 규칙 — 장군·병사 공용 진입점.</summary>
public static class RainFireSplash
{
    public const int    MaxSplash    = 2;      // 추가로 맞는 적 수
    public const float  SplashRatio  = 0.7f;   // 원 피해 대비
    public const float  SplashRadius = 3f;     // 착탄 지점 기준 반경
    public const string EffectKey    = "FX_RedLightning_Chain";
    // 동시에 떠 있는 체인 수는 수명에 정비례한다.
    // 40명이 초당 5발이면 착탄 200/초 → 체인 최대 400/초 → 동시 = 400 × 수명.
    // 0.28 이면 약 112개로 상한(160) 안에 들어온다. 번개는 원래 순간적으로
    // 번쩍이는 게 자연스러워서 짧은 편이 연출도 또렷하다.
    public const float  EffectLife   = 0.28f;

    /// <summary>
    /// 전투 시작 전 준비.
    ///
    /// 체인은 풀에서 GameObject 를 꺼내지 않는다 — LightningChainRenderer 가
    /// 활성 체인 전부를 메시 하나에 모아 그리므로 몇 발이 뜨든 드로우콜은 1 이다.
    /// 따라서 인스턴스 프리웜은 필요 없고, 렌더러만 미리 깨워
    /// 첫 발에서 머티리얼 조회·메시 할당이 몰리지 않게 한다.
    /// </summary>
    public static void Prewarm(int count)
    {
        if (count <= 0) return;
        LightningChainRenderer.Prepare();
    }

    /// <summary>
    /// 착탄 1건 처리 — 착탄 지점 주변에서 가장 가까운 적 MaxSplash 명에게 피해 + 이펙트.
    /// 적 배열은 호출자가 프레임당 한 번만 만들어 넘긴다 (병사 수십 명이 매번 뜨면 감당이 안 된다).
    /// </summary>
    public static void ApplyOne(EntityManager em, Entity attacker,
                                Entity mainTarget, float3 impact, float damage,
                                NativeArray<Entity>                targets,
                                NativeArray<LocalTransform>        transforms,
                                NativeArray<UnitIdentityComponent> identities)
    {
        if (damage <= 0f) return;

        float splashDmg = damage * SplashRatio;

        // 착탄 지점에서 가장 가까운 적 2명 (1등·2등을 한 번의 순회로 추린다)
        Entity e0 = Entity.Null, e1 = Entity.Null;
        float3 p0 = default,     p1 = default;
        float  d0 = float.MaxValue, d1 = float.MaxValue;

        for (int i = 0; i < targets.Length; i++)
        {
            if (identities[i].Team != TeamType.Enemy) continue;
            if (targets[i] == mainTarget)             continue;

            float3 pos  = transforms[i].Position;
            float  dist = math.distance(impact.xy, pos.xy);
            if (dist > SplashRadius) continue;

            if (dist < d0)
            {
                d1 = d0;   e1 = e0;         p1 = p0;
                d0 = dist; e0 = targets[i]; p0 = pos;
            }
            else if (dist < d1)
            {
                d1 = dist; e1 = targets[i]; p1 = pos;
            }
        }

        Apply(em, attacker, e0, p0, impact, splashDmg);
        Apply(em, attacker, e1, p1, impact, splashDmg);
    }

    // 착탄 지점(A) → 튄 대상(B) 으로 피해 + 전기 체인.
    // 넉백 방향도 공격자가 아니라 착탄 지점에서 바깥으로 퍼진다.
    static void Apply(EntityManager em, Entity attacker,
                      Entity target, float3 targetPos, float3 impact, float damage)
    {
        if (target == Entity.Null) return;
        if (!em.Exists(target))    return;   // 앞선 스플래시로 이미 사라졌을 수 있다
        if (!em.HasBuffer<HitEventBufferElement>(target)) return;

        em.GetBuffer<HitEventBufferElement>(target).Add(new HitEventBufferElement
        {
            Damage         = damage,
            AttackerEntity = attacker,
            HitDirection   = math.normalizesafe(targetPos - impact),
            Type           = HitType.Normal,
        });

        // 선은 풀 스폰이 아니라 배치 렌더러에 등록한다 (전체 합쳐 드로우콜 1)
        LightningChainRenderer.Instance.Add((Vector3)impact, (Vector3)targetPos, EffectLife);
    }
}

public class TraitRainFireHandler : ITraitTriggerHandler
{
    // OnAttackLanded: 근거리·원거리 모두 실제 타격 착탄 시 발동 (ECB → 다음 프레임)
    public PassiveTrigger GetTriggerType() => PassiveTrigger.OnAttackLanded;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;

        if (!em.HasBuffer<AttackHitEvent>(entity)) return;

        // ⚠ DynamicBuffer 를 들고 순회하지 않는다 — 아래에서 이펙트 GO 를 활성화하는데,
        //   그 과정이 구조적 변경을 일으키면 핸들이 무효화돼 예외가 난다.
        var hitBuf = em.GetBuffer<AttackHitEvent>(entity);
        if (hitBuf.Length == 0) return;
        var hitEvents = hitBuf.ToNativeArray(Allocator.Temp);

        var query      = ctx.EnemyQuery;
        var targets    = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var identities = query.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);

        // 착탄 이벤트마다 스플래시 처리 (더블 스트라이크 등 복수 착탄 지원)
        foreach (var ev in hitEvents)
            RainFireSplash.ApplyOne(em, entity, ev.TargetEntity, ev.TargetPos, ev.Damage,
                                    targets, transforms, identities);

        hitEvents.Dispose();
        targets.Dispose();
        transforms.Dispose();
        identities.Dispose();
    }
}

// ── 순교 ──────────────────────────────────────────────────────
//  병사가 쓰러진 자리에서 폭발이 일어나 주변 적을 함께 태운다.
//  피해는 "쓰러진 병사"가 아니라 장군의 공격력 기준 — 병사 스탯은
//  지휘력에 따라 들쭉날쭉해서 특성 위력이 예측 불가능해지기 때문이다.

public class TraitMartyrHandler : ITraitTriggerHandler
{
    const float  DamageRatio = 0.8f;                   // 장군 공격력 대비
    const float  Radius      = 2f;                     // 폭발 반경
    const string EffectKey   = "FX_Martyr_Explosion";
    const float  EffectLife  = 1.2f;                   // 이펙트 자동 반납까지

    public PassiveTrigger GetTriggerType() => PassiveTrigger.OnSoldierDeath;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;

        if (!em.HasBuffer<SoldierDeathEvent>(entity)) return;
        if (!em.HasComponent<StatComponent>(entity))  return;

        float damage = em.GetComponentData<StatComponent>(entity).Final[StatType.Attack] * DamageRatio;
        if (damage <= 0f) return;

        // 폭발 지점을 먼저 복사해 둔다 — 아래에서 이펙트 스폰(managed)이
        // 끼어들므로 DynamicBuffer 를 계속 들고 있지 않는다.
        var deathBuf = em.GetBuffer<SoldierDeathEvent>(entity);
        if (deathBuf.Length == 0) return;

        var origins = new NativeArray<float3>(deathBuf.Length, Allocator.Temp);
        for (int i = 0; i < deathBuf.Length; i++)
            origins[i] = deathBuf[i].Position;

        var query      = ctx.EnemyQuery;
        var targets    = query.ToEntityArray(Allocator.Temp);
        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var identities = query.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);

        foreach (var origin in origins)
        {
            SkillEffectHelper.Spawn(EffectKey, (Vector3)origin, EffectLife);

            for (int i = 0; i < targets.Length; i++)
            {
                if (identities[i].Team != TeamType.Enemy) continue;
                if (math.distance(origin.xy, transforms[i].Position.xy) > Radius) continue;
                // 이펙트 스폰이 낀 뒤라 엔티티가 사라졌을 수 있다
                if (!em.Exists(targets[i])) continue;
                if (!em.HasBuffer<HitEventBufferElement>(targets[i])) continue;

                em.GetBuffer<HitEventBufferElement>(targets[i]).Add(new HitEventBufferElement
                {
                    Damage         = damage,
                    AttackerEntity = entity,
                    HitDirection   = math.normalizesafe(transforms[i].Position - origin),
                    Type           = HitType.Normal,
                });
            }
        }

        origins.Dispose();
        targets.Dispose();
        transforms.Dispose();
        identities.Dispose();
    }
}

// ── 파쇄 ──────────────────────────────────────────────────────
//  착탄마다 대상 최대 체력의 일정 비율을 추가로 깎는다.
//
//  ■ 왜 "장군 공격력 비례" 가 아닌가
//    공격력에 비례하면 공격력을 올리든 공속을 올리든 결과가 같아서 빌드가 안 갈린다.
//    이 특성은 오직 "때린 횟수" 에만 비례하므로 공속을 올릴수록 값이 커진다
//    — 공격속도 축이 성립하는 유일한 지점이다.
//
//  ■ 보스에게 비율을 깎는 이유
//    체력 비례 피해는 대상이 누구든 "고정 타수 처형" 이 된다 — 2% 면 50대에 죽는다.
//    잡몹은 어차피 50대 전에 평타로 죽으니 문제가 없지만, 보스는 원래 오래 버티라고
//    체력을 크게 잡아 둔 상대라 그 설계가 통째로 무의미해진다.
//    보스에게만 BossRatio 를 곱해 실질 2% → 0.66%(약 150대)로 늦춘다.
//
//  ■ 상한도 함께 거는 이유
//    비율을 깎아도 보스 체력이 수만이면 한 방이 여전히 크다.
//    장군 공격력의 CapMult 배로 잘라 "평타 몇 대 값" 을 넘지 않게 한다.

public class TraitRendHandler : ITraitTriggerHandler
{
    const float HpRatio   = 0.02f;   // 대상 최대 체력의 2%
    const float BossRatio = 0.33f;   // 보스에게는 그중 33% 만 (실질 0.66%)
    const float CapMult   = 3f;      // 장군 공격력 × 3 상한

    public PassiveTrigger GetTriggerType() => PassiveTrigger.OnAttackLanded;

    public void OnTrigger(PassiveTriggerContext ctx)
    {
        var em     = ctx.EntityManager;
        var entity = ctx.GeneralEntity;

        if (!em.HasBuffer<AttackHitEvent>(entity))   return;
        if (!em.HasComponent<StatComponent>(entity)) return;

        float cap = em.GetComponentData<StatComponent>(entity).Final[StatType.Attack] * CapMult;
        if (cap <= 0f) return;

        // 버퍼 핸들을 들고 순회하지 않는다 (TraitRainFireHandler 주석 참고)
        var hitEvents = em.GetBuffer<AttackHitEvent>(entity).ToNativeArray(Allocator.Temp);

        foreach (var ev in hitEvents)
        {
            var target = ev.TargetEntity;
            if (target == Entity.Null || !em.Exists(target))         continue;
            if (!em.HasComponent<StatComponent>(target))             continue;
            if (!em.HasBuffer<HitEventBufferElement>(target))        continue;

            float targetMaxHp = em.GetComponentData<StatComponent>(target).Final[StatType.MaxHp];
            float ratio       = em.HasComponent<BossComponent>(target) ? HpRatio * BossRatio : HpRatio;
            float bonus       = math.min(targetMaxHp * ratio, cap);
            if (bonus <= 0f) continue;

            em.GetBuffer<HitEventBufferElement>(target).Add(new HitEventBufferElement
            {
                Damage         = bonus,
                AttackerEntity = entity,
                HitDirection   = float3.zero,   // 넉백 없음 — 순수 추가 피해
                Type           = HitType.Normal,
            });
        }

        hitEvents.Dispose();
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

    /// <summary>재발동까지의 텀 (초). 0 이면 같은 프레임에 겹쳐 한 번 쓴 것처럼 보인다.</summary>
    const float EchoDelay = 0.5f;

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

        // ── 0.5초 뒤 재발동 ──────────────────────────────────
        //  같은 프레임에 두 번 터지면 한 번 쓴 것처럼 보이고,
        //  러너 기반 스킬은 원본 시전이 통째로 취소된다 (EchoSkillRunner 주석 참고).
        if (context.CasterObject != null)
        {
            var echo = context.CasterObject.GetComponent<EchoSkillRunner>();
            if (echo == null) echo = context.CasterObject.AddComponent<EchoSkillRunner>();

            echo.Echo(skillData, context, EchoDelay, EchoEffectScale);
        }
        else
        {
            // 시전자 GameObject 가 없으면 코루틴을 돌릴 곳이 없다 — 즉시 실행으로 대체
            float orig = skillData.EffectValue;
            skillData.EffectValue = orig * EchoEffectScale;
            skillData.Execute(context);
            skillData.EffectValue = orig;
        }

        // SkillUseEvent 를 추가하지 않으므로 에코의 에코는 발동되지 않음
    }
}
