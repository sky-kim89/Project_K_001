using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  CombatTriggerSystem.cs
//  장비·특수 어빌리티 트리거 디스패치 시스템.
//
//  GeneralTriggerSetComponent 는 managed class IComponentData 이므로
//  SystemAPI.Query 에 포함할 수 없고 EntityManager.GetComponentObject 로 획득.
//
//  실행 순서: PassiveSkillRuntimeSystem → CombatTriggerSystem → UnitHitSystem
// ============================================================

namespace BattleGame.Units
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PassiveSkillRuntimeSystem))]
    [UpdateBefore(typeof(UnitHitSystem))]
    public partial class CombatTriggerSystem : SystemBase
    {
        EntityQuery _enemyQuery;
        EntityQuery _generalQuery;
        EntityQuery _soldierQuery;

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All  = new ComponentType[] { ComponentType.ReadOnly<UnitIdentityComponent>(), ComponentType.ReadOnly<LocalTransform>() },
                None = new ComponentType[] { typeof(DeadTag) },
            });

            // 병사 버프 장비(군단의 뿔피리 등)가 쓰는 쿼리 — 장군별 필터는 순회에서 한다
            _soldierQuery = GetEntityQuery(new EntityQueryDesc
            {
                All  = new ComponentType[] { ComponentType.ReadOnly<SoldierComponent>(), ComponentType.ReadOnly<StatComponent>() },
                None = new ComponentType[] { typeof(DeadTag) },
            });

            _generalQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<GeneralComponent>(),
                    ComponentType.ReadOnly<GeneralTriggerSetComponent>(),
                    ComponentType.ReadOnly<AttackComponent>(),
                    ComponentType.ReadOnly<HealthComponent>(),
                },
                None = new ComponentType[] { typeof(DeadTag) },
            });
        }

        // ⚠ SystemAPI.Query 로 순회하지 않는다.
        //   여기서 부르는 핸들러(연속 시전의 스킬 재시전, 소환, 이펙트 GO 활성화 등)는
        //   구조적 변경을 일으킬 수 있고, 그 순간 순회의 TypeHandle 과
        //   미리 잡아 둔 DynamicBuffer 가 통째로 무효화된다.
        //   → 대상 엔티티를 배열로 떠 놓고 순회 밖에서 처리하며,
        //     버퍼 핸들은 절대 핸들러 호출을 가로질러 들고 있지 않는다.
        protected override void OnUpdate()
        {
            var generals = _generalQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in generals)
            {
                if (!EntityManager.Exists(entity)) continue;
                if (!EntityManager.HasComponent<GeneralTriggerSetComponent>(entity)) continue;

                var trigSet = EntityManager.GetComponentObject<GeneralTriggerSetComponent>(entity);
                if (trigSet == null) continue;

                var attack = EntityManager.GetComponentData<AttackComponent>(entity);
                var health = EntityManager.GetComponentData<HealthComponent>(entity);

                // 이벤트 유무·개수만 읽고 버퍼 핸들은 버린다.
                int soldierDeaths = EntityManager.GetBuffer<SoldierDeathEvent>(entity).Length;

                bool doAttack        = attack.AttackedThisFrame;
                bool doHit           = EntityManager.GetBuffer<HitEventBufferElement>(entity).Length > 0;
                bool doKill          = EntityManager.GetBuffer<EnemyKillEvent>(entity).Length        > 0;
                bool doSoldierDeath  = soldierDeaths                                                 > 0;
                bool doSkill         = EntityManager.GetBuffer<SkillUseEvent>(entity).Length         > 0;
                bool doAttackLanded  = EntityManager.GetBuffer<AttackHitEvent>(entity).Length        > 0;

                if (!doAttack && !doHit && !doKill && !doSoldierDeath && !doSkill && !doAttackLanded)
                    continue;

                var ctx = new PassiveTriggerContext
                {
                    GeneralEntity     = entity,
                    EntityManager     = EntityManager,
                    EnemyQuery        = _enemyQuery,
                    Health            = health,
                    DamageDealt       = attack.LastDamageDealt,
                    SoldierDeathCount = soldierDeaths,
                };

                // 사건이 일어난 자리 — 소환이 "그 자리에서" 일어나게 하려고 같이 넘긴다.
                // 여러 건이면 마지막 것을 쓴다(한 프레임에 한 번만 터지는 판정이라 하나면 충분하다).
                if (doKill)
                {
                    var killBuf = EntityManager.GetBuffer<EnemyKillEvent>(entity);
                    ctx.KillPosition    = killBuf[killBuf.Length - 1].Position;
                    ctx.HasKillPosition = true;
                }
                if (doSoldierDeath)
                {
                    var deathBuf = EntityManager.GetBuffer<SoldierDeathEvent>(entity);
                    ctx.SoldierDeathPosition    = deathBuf[deathBuf.Length - 1].Position;
                    ctx.HasSoldierDeathPosition = true;
                }

                // ── 장비 트리거 디스패치 ─────────────────────────
                //
                //  ⚠ 판정은 프레임 단위다 — 확률을 정할 때 이걸 감안해야 한다
                //    "적 처치 시" 는 누가 죽였든 적이 죽은 프레임마다 참이 된다
                //    (UnitDeathDespawnSystem 이 아군 장군 전원에게 브로드캐스트).
                //    난전에서는 초당 수십 번 굴린다는 뜻이라, 표시된 확률이 곧
                //    체감 빈도가 아니다.
                for (int s = 0; s < trigSet.ActiveEquipSlots; s++)
                {
                    var equip   = trigSet.EquipSlots[s];
                    int enhance = trigSet.EnhanceLevels[s];
                    if (equip == null || equip.TriggerType == EquipmentTrigger.None) continue;

                    bool fire = equip.TriggerType switch
                    {
                        EquipmentTrigger.OnAttack       => doAttack,
                        EquipmentTrigger.OnHit          => doHit,
                        EquipmentTrigger.OnEnemyKill    => doKill,
                        EquipmentTrigger.OnSoldierDeath => doSoldierDeath,
                        EquipmentTrigger.OnSkillUse     => doSkill,
                        _                               => false,
                    };
                    if (!fire) continue;
                    if (Random.value > equip.TriggerChance) continue;

                    ApplyEquipTrigger(equip, enhance, s, ctx);
                }

                // ── 특수 어빌리티 트리거 디스패치 ────────────────
                foreach (var ability in trigSet.TriggerAbilities)
                {
                    if (ability == null) continue;
                    PassiveTrigger pt = ability.GetTriggerType();

                    bool fire = pt switch
                    {
                        PassiveTrigger.OnAttack        => doAttack,
                        PassiveTrigger.OnHit           => doHit,
                        PassiveTrigger.OnEnemyKill     => doKill,
                        PassiveTrigger.OnSoldierDeath  => doSoldierDeath,
                        PassiveTrigger.OnSkillUse      => doSkill,
                        PassiveTrigger.OnAttackLanded  => doAttackLanded,
                        _                              => false,
                    };
                    if (!fire) continue;
                    ability.OnTrigger(ctx);
                }

                // ── 특성 트리거 디스패치 ──────────────────────────
                foreach (var handler in trigSet.TraitTriggers)
                {
                    if (handler == null) continue;
                    bool fire = handler.GetTriggerType() switch
                    {
                        PassiveTrigger.OnAttack        => doAttack,
                        PassiveTrigger.OnHit           => doHit,
                        PassiveTrigger.OnEnemyKill     => doKill,
                        PassiveTrigger.OnSoldierDeath  => doSoldierDeath,
                        PassiveTrigger.OnSkillUse      => doSkill,
                        PassiveTrigger.OnAttackLanded  => doAttackLanded,
                        _                              => false,
                    };
                    if (!fire) continue;
                    handler.OnTrigger(ctx);
                }

                // ── 이벤트 버퍼 소비 완료 → 클리어 ──────────────────
                // 이 시스템이 이벤트 버퍼의 마지막 소비자다.
                // PassiveSkillRuntimeSystem(먼저 도는 시스템)이 지우면 여기까지
                // 이벤트가 오지 않아 장비·어빌리티·특성 트리거가 통째로 죽는다.
                //
                // ⚠ 반드시 여기서 버퍼를 다시 가져온다. 위에서 잡아 둔 핸들은
                //   핸들러의 구조적 변경으로 이미 무효일 수 있고, 그 상태로 Clear 하면
                //   ObjectDisposedException 이 나면서 OnUpdate 가 중단된다
                //   → 버퍼가 안 비워져 이벤트가 쌓이고, 다음 프레임에 몰아서 터진다.
                if (!EntityManager.Exists(entity)) continue;

                if (doAttackLanded)  EntityManager.GetBuffer<AttackHitEvent>(entity).Clear();
                if (doSoldierDeath)  EntityManager.GetBuffer<SoldierDeathEvent>(entity).Clear();
                if (doKill)          EntityManager.GetBuffer<EnemyKillEvent>(entity).Clear();
                if (doSkill)         EntityManager.GetBuffer<SkillUseEvent>(entity).Clear();
            }

            generals.Dispose();
        }

        // ── 장비 트리거 효과 적용 ────────────────────────────────

        void ApplyEquipTrigger(EquipmentData equip, int enhance, int slotIndex, PassiveTriggerContext ctx)
        {
            if (equip.EffectKind == EquipTriggerEffect.Summon)
            {
                SummonForEquip(equip, enhance, ctx);
                return;
            }

            if (equip.TriggerTarget == EquipTriggerTarget.Soldiers)
            {
                ApplyToSoldiers(equip, enhance, slotIndex, ctx);
                return;
            }

            ApplyStatEffect(equip, enhance, slotIndex, ctx, ctx.GeneralEntity,
                            CalcValue(equip, enhance, ctx));
        }

        /// <summary>한 대상에게 회복 또는 버프를 건다.</summary>
        static void ApplyStatEffect(EquipmentData equip, int enhance, int slotIndex,
                                    PassiveTriggerContext ctx, Entity target, float value)
        {
            var em = ctx.EntityManager;
            if (!em.Exists(target)) return;

            // MaxHp 스탯 = 즉시 체력 회복 특수 처리
            if (equip.EffectKind == EquipTriggerEffect.StatBuff && equip.TriggerStat == StatType.MaxHp)
            {
                if (!em.HasBuffer<HealEventBufferElement>(target)) return;
                em.GetBuffer<HealEventBufferElement>(target).Add(
                    new HealEventBufferElement { Amount = value, SourceEntity = ctx.GeneralEntity });
                return;
            }

            // 그 외 스탯 = StatusEffect 버프
            //  RatioBuff 는 Multiply 로 들어간다 — 대상마다 기본 수치가 다른 병사 버프용.
            if (!em.HasBuffer<StatusEffectBufferElement>(target)) return;
            bool  ratio = equip.EffectKind == EquipTriggerEffect.RatioBuff;
            float dur   = equip.TriggerDuration > 0f ? equip.TriggerDuration : 0.1f;
            em.GetBuffer<StatusEffectBufferElement>(target).Add(new StatusEffectBufferElement
            {
                Stat       = equip.TriggerStat,
                Delta      = ratio ? 1f + value : value,
                Mode       = ratio ? EffectMode.Multiply : EffectMode.Add,
                Duration   = dur,
                Remaining  = dur,
                SourceType = BuffSourceType.Equipment,
                SourceId   = slotIndex,
            });
        }

        /// <summary>
        /// 이 장군 휘하 병사 전원에게 효과를 뿌린다.
        ///
        /// ⚠ 캐시한 쿼리로 배열을 떠서 돈다
        ///   SystemAPI.Query 순회 안에서 버퍼를 만지면 구조적 변경 한 번에
        ///   TypeHandle 이 통째로 무효화된다 (이 파일 상단 주석과 같은 이유).
        /// </summary>
        void ApplyToSoldiers(EquipmentData equip, int enhance, int slotIndex, PassiveTriggerContext ctx)
        {
            var em       = ctx.EntityManager;
            var soldiers = _soldierQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < soldiers.Length; i++)
            {
                Entity s = soldiers[i];
                if (!em.Exists(s)) continue;
                if (em.GetComponentData<SoldierComponent>(s).GeneralEntity != ctx.GeneralEntity) continue;

                // 회복·비율 버프는 병사 자신의 수치를 기준으로 잡아야 한다
                float value = CalcValueFor(equip, enhance, ctx, s);
                ApplyStatEffect(equip, enhance, slotIndex, ctx, s, value);
            }

            soldiers.Dispose();
        }

        /// <summary>
        /// 스켈레톤 소환. 스폰·외형·태그는 SkeletonSpawner 가 소유한다 —
        /// 스켈레톤 소환 스킬(ActiveSummonSkeleton)과 같은 모습이어야 한다.
        /// </summary>
        void SummonForEquip(EquipmentData equip, int enhance, PassiveTriggerContext ctx)
        {
            var em = ctx.EntityManager;
            if (PoolController.Instance == null) return;

            // 장군 GameObject 는 UnitPoolLinkComponent 로 찾는다 (ActiveSkillExecuteSystem 과 같은 길)
            if (!em.HasComponent<UnitPoolLinkComponent>(ctx.GeneralEntity)) return;
            var go = em.GetComponentObject<UnitPoolLinkComponent>(ctx.GeneralEntity)?.LinkedObject;
            if (go == null || !go.TryGetComponent<GeneralRuntimeBridge>(out var bridge)) return;

            UnitStat generalStat = bridge.GetRolledStat();
            if (generalStat == null) return;
            if (!em.HasComponent<UnitJobComponent>(ctx.GeneralEntity)) return;

            UnitJob job      = em.GetComponentData<UnitJobComponent>(ctx.GeneralEntity).Job;
            Vector3 basePos  = SummonOrigin(equip, ctx, go.transform.position);
            int     count    = Mathf.Max(1, Mathf.RoundToInt(
                                   equip.TriggerValue * (1f + enhance * equip.ValuePerLevel)));

            for (int i = 0; i < count; i++)
            {
                // 한 기짜리는 그 자리 그대로 — 굳이 반경만큼 밀어내면 "쓰러진 자리" 가 어긋난다
                Vector3 pos = basePos;
                if (count > 1)
                {
                    float angle = (360f / count) * i * Mathf.Deg2Rad;
                    pos += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * SummonRadius;
                }

                SkeletonSpawner.Spawn(em, equip.SummonPoolKey, pos, generalStat,
                                      equip.SummonStatRatio, ctx.GeneralEntity, job);
            }
        }

        /// <summary>
        /// 소환이 일어날 자리.
        ///
        /// ⚠ 사건이 벌어진 곳에서 일어나야 무엇이 일어났는지 읽힌다
        ///   "처형자의 낙인"(적 처치) 은 쓰러진 적 위에서, "망자의 소환서"(병사 사망) 는
        ///   쓰러진 병사 자리에서 일어난다 — 둘 다 설명 문구가 그렇게 약속하고 있다.
        ///   예전엔 전부 장군 발밑이라, 병사 떼 사이에서 하나 더 생겨도 눈에 띄지 않았다.
        ///   위치를 못 받은 경우(스킬 사용 트리거 등)만 장군 자리로 물러선다.
        /// </summary>
        static Vector3 SummonOrigin(EquipmentData equip, PassiveTriggerContext ctx, Vector3 fallback)
        {
            if (equip.TriggerType == EquipmentTrigger.OnEnemyKill && ctx.HasKillPosition)
                return ctx.KillPosition;

            if (equip.TriggerType == EquipmentTrigger.OnSoldierDeath && ctx.HasSoldierDeathPosition)
                return ctx.SoldierDeathPosition;

            return fallback;
        }

        /// <summary>소환 위치 반경 — 소환 지점을 둘러싸고 나온다.</summary>
        const float SummonRadius = 1.5f;

        static float CalcValue(EquipmentData equip, int enhance, PassiveTriggerContext ctx)
            => CalcValueFor(equip, enhance, ctx, ctx.GeneralEntity);

        /// <summary>
        /// 효과 수치를 대상 기준으로 계산한다.
        ///
        /// ⚠ 기준값은 '받는 쪽' 에서 읽는다
        ///   최대 체력 비례 회복을 병사에게 뿌릴 때 장군의 체력을 기준으로 잡으면
        ///   병사가 한 방에 풀피가 된다. 대상이 자기 수치를 기준으로 삼아야 한다.
        ///   (피해량 기준만은 장군이 때린 값이라 대상과 무관하다)
        /// </summary>
        static float CalcValueFor(EquipmentData equip, int enhance, PassiveTriggerContext ctx, Entity target)
        {
            float v = equip.TriggerValue;
            if (equip.TriggerIsPercent)
            {
                float baseRef = equip.TriggerPercentBase switch
                {
                    EquipTriggerPercentBase.OfDamage => ctx.DamageDealt,
                    EquipTriggerPercentBase.OfMaxHp  =>
                        ctx.EntityManager.HasComponent<StatComponent>(target)
                            ? ctx.EntityManager.GetComponentData<StatComponent>(target).Final[StatType.MaxHp]
                            : 0f,
                    _ => 1f,
                };
                v *= baseRef;
            }
            v *= 1f + enhance * equip.ValuePerLevel;
            return v;
        }
    }
}
