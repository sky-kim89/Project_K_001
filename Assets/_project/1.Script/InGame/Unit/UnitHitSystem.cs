using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;


// ============================================================
//  UnitHitSystem.cs
//  피격 처리 시스템
//  - HitEventBuffer 에 쌓인 이벤트를 읽어 HP 차감
//  - Defense 는 StatComponent.Final[StatType.Defense] 에서 읽음
//  - 사망 시 DeadTag 부착 (HealthComponent.IsDead 필드 제거됨)
// ============================================================

namespace BattleGame.Units
{
    /// <summary>
    /// 방어율 → 실제 피해 환산 공식. Burst 안에서도 쓸 수 있도록 순수 static.
    ///
    /// ⚠ 예약 피해(ProjectileIncomingDamageSystem)와 실제 피해(ProjectileHitJob)가
    ///   반드시 같은 값을 내야 한다 — 예약이 실제보다 크면 죽지 않은 적이 영영
    ///   타겟에서 제외되어 전투가 멈춘다. 두 곳 모두 이 함수만 쓸 것.
    /// </summary>
    public static class DamageMath
    {
        /// <param name="pierce">
        /// 공격자의 방어율 관통 (0~1). 소프트캡·상한을 모두 적용한 <b>최종</b> 방어율에서
        /// 뺀다 — 관통이 소프트캡 공식을 거꾸로 타고 증폭되지 않게 하려는 것이다.
        /// </param>
        public static float AfterDefense(float rawDamage, float rawDefense, float pierce,
                                         float softCap, float overflowRate, float effectiveCap)
        {
            float eff = rawDefense <= softCap
                ? rawDefense
                : softCap + (rawDefense - softCap) * overflowRate;
            float defense = math.min(eff, effectiveCap);
            defense = math.max(0f, defense - math.saturate(pierce));
            return math.max(rawDamage * (1f - defense), 1f);
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct UnitHitSystem : ISystem
    {
        ComponentLookup<BossComponent>        _bossLookup;
        ComponentLookup<EliteComponent>       _eliteLookup;
        ComponentLookup<MirrorArmorComponent> _mirrorArmorLookup;
        ComponentLookup<KnockbackImmuneTag>   _knockbackImmuneLookup;
        ComponentLookup<InvulnerableTag>      _invulnerableLookup;
        ComponentLookup<SpawnProtection>      _spawnProtectionLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _bossLookup            = state.GetComponentLookup<BossComponent>(isReadOnly: true);
            _eliteLookup           = state.GetComponentLookup<EliteComponent>(isReadOnly: true);
            _mirrorArmorLookup     = state.GetComponentLookup<MirrorArmorComponent>(isReadOnly: true);
            _knockbackImmuneLookup = state.GetComponentLookup<KnockbackImmuneTag>(isReadOnly: true);
            _invulnerableLookup    = state.GetComponentLookup<InvulnerableTag>(isReadOnly: true);
            _spawnProtectionLookup = state.GetComponentLookup<SpawnProtection>(isReadOnly: true);
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _bossLookup.Update(ref state);
            _eliteLookup.Update(ref state);
            _mirrorArmorLookup.Update(ref state);
            _knockbackImmuneLookup.Update(ref state);
            _invulnerableLookup.Update(ref state);
            _spawnProtectionLookup.Update(ref state);

            var cfg = GameplayConfig.Current;
            if (cfg == null) return;

            float softCap      = cfg.DefenseMax;
            float overflowRate = cfg.DefenseOverflowRate;
            float effectiveCap = cfg.DefenseEffectiveCap;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb          = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessHitEventsJob
            {
                Ecb                   = ecb,
                BossLookup            = _bossLookup,
                EliteLookup           = _eliteLookup,
                MirrorArmorLookup     = _mirrorArmorLookup,
                KnockbackImmuneLookup = _knockbackImmuneLookup,
                InvulnerableLookup    = _invulnerableLookup,
                SpawnProtectionLookup = _spawnProtectionLookup,
                DefenseSoftCap        = softCap,
                DefenseOverflowRate   = overflowRate,
                DefenseEffectiveCap   = effectiveCap,
            }.ScheduleParallel();
        }
    }

    // ──────────────────────────────────────────
    // 피격 이벤트 처리 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct ProcessHitEventsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter               Ecb;
        [ReadOnly] public ComponentLookup<BossComponent>        BossLookup;
        [ReadOnly] public ComponentLookup<EliteComponent>       EliteLookup;
        [ReadOnly] public ComponentLookup<MirrorArmorComponent> MirrorArmorLookup;
        [ReadOnly] public ComponentLookup<KnockbackImmuneTag>   KnockbackImmuneLookup;
        [ReadOnly] public ComponentLookup<InvulnerableTag>      InvulnerableLookup;
        // 소환 연출 중(땅에서 일어나는 중)인 유닛 — 잠깐 피해를 받지 않는다
        [ReadOnly] public ComponentLookup<SpawnProtection>      SpawnProtectionLookup;
        public float DefenseSoftCap;
        public float DefenseOverflowRate;
        public float DefenseEffectiveCap;

        // ── 일반 공격 넉백 ────────────────────────────────────
        /// <summary>최대 체력 100% 를 한 방에 깎았을 때의 넉백 세기.</summary>
        const float KnockbackPerHpRatio = 10f;
        /// <summary>단일 타격 넉백 상한 (여러 타격 합산 상한은 MaxKnockbackMag).</summary>
        const float MaxSingleKnockback  = 6f;

        /// <summary>
        /// 경직 발생 문턱 — 한 방에 최대 체력의 이 비율 이상을 날린 타격만 경직을 만든다.
        /// 이 아래는 <b>경직 시간을 계산조차 하지 않고 그냥 무시</b>한다.
        /// </summary>
        const float StunHpRatioThreshold = 0.02f;

        public void Execute(
            [ChunkIndexInQuery] int                    chunkIndex,
            Entity                                     entity,
            ref HealthComponent                        health,
            in  StatComponent                          stat,       // Defense → StatFinal
            ref HitReactionComponent                   hitReaction,
            ref UnitStateComponent                     unitState,
            ref DynamicBuffer<HitEventBufferElement>   hitBuffer,
            ref DynamicBuffer<DamageResultElement>     resultBuffer)
        {
            if (hitBuffer.Length == 0) return;

            float  totalDamage      = 0f;
            float3 totalKnockback   = float3.zero;
            float  maxStun          = 0f;
            float  maxNormalKbMag   = 0f;   // 일반 공격: 프레임 내 최대 단일 타격 넉백
            float3 maxNormalKbVec   = float3.zero;

            float rawDef      = stat.Final[StatType.Defense];

            // 넉백은 "절대 피해량" 이 아니라 "최대 체력 대비 비율" 로 정한다.
            //   100 체력에 50 피해  → 0.5   → 크게 밀린다
            //   10000 체력에 50 피해 → 0.005 → 거의 밀리지 않는다
            // 절대값 기준이면 스테이지가 올라가 공격력이 커질수록 체력 만 단위 보스도
            // 잡몹처럼 날아가고, 반대로 초반 저공격력은 종잇장 적조차 못 밀어낸다.
            float maxHp = math.max(1f, stat.Final[StatType.MaxHp]);

            bool  hasMirror   = MirrorArmorLookup.HasComponent(entity);
            float mirrorRatio = hasMirror ? MirrorArmorLookup[entity].ReflectRatio : 0f;

            bool hasSkillKnockback = false;

            for (int i = 0; i < hitBuffer.Length; i++)
            {
                HitEventBufferElement hit = hitBuffer[i];

                // 방어율 적용 (공식은 DamageMath 가 소유 — 예약 피해 계산과 반드시 동일)
                float rawDamage    = hit.Damage;
                float actualDamage = DamageMath.AfterDefense(
                    rawDamage, rawDef, hit.DefensePierce,
                    DefenseSoftCap, DefenseOverflowRate, DefenseEffectiveCap);
                float absorbed     = rawDamage - actualDamage;
                totalDamage       += actualDamage;

                if (hit.Type == HitType.Skill)
                {
                    totalKnockback += hit.HitDirection;
                    if (math.lengthsq(hit.HitDirection) > 0f)
                        hasSkillKnockback = true;
                }
                else
                {
                    // 체력의 100% 를 한 방에 날리면 KnockbackPerHpRatio 만큼 밀린다.
                    float kbMag = math.min(actualDamage / maxHp * KnockbackPerHpRatio, MaxSingleKnockback);
                    if (kbMag > maxNormalKbMag)
                    {
                        maxNormalKbMag = kbMag;
                        maxNormalKbVec = hit.HitDirection * kbMag;
                    }
                }

                float stunTime = CalculateStunDuration(actualDamage, maxHp);
                maxStun        = math.max(maxStun, stunTime);

                // 거울 방어 반사 — 실제로 받은 피해(방어 적용 후) 기준으로 반사. 반사된 피해는 재반사 불가.
                if (hasMirror && hit.Type != HitType.Reflected && hit.AttackerEntity != Entity.Null)
                {
                    Ecb.AppendToBuffer(chunkIndex, hit.AttackerEntity, new HitEventBufferElement
                    {
                        Damage         = actualDamage * mirrorRatio,
                        AttackerEntity = entity,
                        Type           = HitType.Reflected,
                    });
                }

                resultBuffer.Add(new DamageResultElement
                {
                    AttackerEntity = hit.AttackerEntity,
                    ActualDamage   = actualDamage,
                    AbsorbedDamage = absorbed,
                    IsKill         = false,
                    Type           = hit.Type,
                });
            }

            totalKnockback   += maxNormalKbVec;

            // 스킬 넉백이 있으면 KnockbackJob이 적용할 수 있도록 최소 경직 시간 보장
            // (KnockbackJob은 IsStunned=true 일 때만 KnockbackVelocity를 적용함)
            if (hasSkillKnockback)
                maxStun = math.max(maxStun, 0.3f);

            // ── 무적 ──
            //  피격 연출(플래시·넉백·경직)과 반사·피해 기록은 그대로 두고 체력만 지킨다.
            //  ⚠ 반드시 여기서 막는다 — 죽음은 이 자리에서 확정되므로
            //    밖에서 체력을 주기적으로 채우는 방식은 한 틱 안에 통이 비는
            //    병사를 못 살린다 (로비 데모에서 장군만 살아남던 이유).
            // ⚠ 연출·무적은 '피해만' 막는다
            //   플래시·넉백·경직은 위에서 이미 처리됐다. 체력을 지키는 것이 전부다.
            if (!InvulnerableLookup.HasComponent(entity) &&
                !SpawnProtectionLookup.HasComponent(entity))
                health.CurrentHp -= totalDamage;

            hitBuffer.Clear();

            hitReaction.NeedsFlash = true;

            // ── 사망 판정 ──
            if (health.CurrentHp <= 0f)
            {
                health.CurrentHp = 0f;
                ChangeState(ref unitState, UnitState.Dead);
                Ecb.AddComponent<DeadTag>(chunkIndex, entity);

                // 마지막 히트를 날린 공격자에게 킬 표시 (버퍼 마지막 항목 기준)
                if (resultBuffer.Length > 0)
                {
                    int last = resultBuffer.Length - 1;
                    var r = resultBuffer[last];
                    r.IsKill = true;
                    resultBuffer[last] = r;
                }
                return;
            }

            // ── 방패병 달인: 넉백 완전 무시 ──
            if (KnockbackImmuneLookup.HasComponent(entity))
            {
                totalKnockback = float3.zero;
                maxStun        = 0f;
            }
            else
            {
                // ── 내성 적용 ──
                if (BossLookup.HasComponent(entity))
                {
                    var boss = BossLookup[entity];
                    totalKnockback *= (1f - boss.KnockbackResistance);
                    maxStun        *= (1f - boss.CCResistance);
                }
                else if (EliteLookup.HasComponent(entity))
                {
                    totalKnockback *= (1f - EliteLookup[entity].KnockbackResistance);
                }
            }

            // ── 넉백 / 경직 적용 (누적 상한 8) ──
            const float MaxKnockbackMag = 8f;
            float kbMagSq = math.lengthsq(totalKnockback);
            if (kbMagSq > MaxKnockbackMag * MaxKnockbackMag)
                totalKnockback = math.normalize(totalKnockback) * MaxKnockbackMag;

            hitReaction.KnockbackVelocity = totalKnockback;

            hitReaction.StunDuration = math.max(hitReaction.StunDuration, maxStun);
            hitReaction.StunTimer    = math.max(hitReaction.StunTimer,    maxStun);
            hitReaction.IsStunned    = hitReaction.IsStunned || maxStun > 0f;

            if (maxStun > 0f)
                ChangeState(ref unitState, UnitState.Hit);
        }

        /// <summary>
        /// 경직 시간 — 넉백과 같은 기준(최대 체력 대비 비율)으로 정하고,
        /// <b>문턱 아래의 타격은 경직을 아예 만들지 않는다.</b>
        ///
        /// ⚠ 절대 피해량 기준이던 것을 비율 + 문턱으로 바꿨다
        ///   예전 표는 "피해 20 이상이면 무조건 경직" 이었다. 스테이지가 오르면
        ///   잡몹 평타도 수백 피해라 <b>모든 타격이 경직</b>이 됐다. 그런데 경직은
        ///   매 프레임 max() 로 갱신되고 공격 Job 은 UnitState.Hit 이면 그냥 return 한다.
        ///   적 대여섯이 붙으면 프레임마다 새 경직이 덮여 타이머가 0 에 닿질 못했고,
        ///   유닛은 계속 밀리기만 하며 평생 공격을 못 했다 — 그게 '무한 넉백' 증상이다.
        ///   넉백 상한(MaxKnockbackMag)은 한 프레임의 세기만 막을 뿐 이 누적을 못 막는다.
        ///
        ///   지금은 한 방에 체력을 크게 날린 타격만 경직이다. 잡몹의 잔매는 몇이 붙든
        ///   경직도 넉백도 아니다 (넉백은 IsStunned 일 때만 적용되므로 함께 사라진다).
        /// </summary>
        static float CalculateStunDuration(float damage, float maxHp)
        {
            float ratio = damage / maxHp;
            if (ratio < StunHpRatioThreshold) return 0f;   // 문턱 아래 = 무시
            if (ratio >= 0.40f) return 0.5f;
            if (ratio >= 0.25f) return 0.35f;
            return 0.2f;
        }

        static void ChangeState(ref UnitStateComponent s, UnitState next)
        {
            s.Previous   = s.Current;
            s.Current    = next;
            s.StateTimer = 0f;
        }
    }

    // ──────────────────────────────────────────
    // 상태 타이머 갱신 Job
    // ──────────────────────────────────────────

    [BurstCompile]
    public partial struct StateTimerJob : IJobEntity
    {
        public readonly float DeltaTime;

        public void Execute(ref UnitStateComponent unitState)
        {
            unitState.StateTimer += DeltaTime;
        }
    }
}
