using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// ============================================================
//  GeneralSkillSystem.cs
//  장군 관련 시스템 3종
//
//  ① SoldierSpawnSystem          [InitializationSystemGroup]
//     - SpawnSoldiersRequest 가 붙은 장군 Entity 를 찾아
//       지정 프리팹을 Count 만큼 인스턴스화
//     - 각 병사에 GeneralEntity / StatScaleRatio 주입 후 스탯 스케일 적용
//     - 처리 완료 후 SpawnSoldiersRequest 제거
//
//  ② PassiveSkillAuraSystem       [SimulationSystemGroup]
//     - 1초마다 실행
//     - GeneralPassiveSkillComponent 를 가진 장군의
//       CommandRadius 내 소속 병사에게 StatusEffectBufferElement 갱신
//     - 버프 Duration = RefreshDuration(2초), 1초마다 갱신 → 만료 없이 유지
//       장군 사망 / 범위 이탈 시 2초 후 자연 만료
//
//  ③ ActiveSkillCooldownSystem    [SimulationSystemGroup]
//     - 매 프레임 쿨다운 감소
//     - UseActiveSkillTag 발동 시: 쿨다운 리셋 + 태그 제거
//       실제 스킬 실행(트윈/이동/공격 제어)은 별도 스킬 실행기에서 수행
// ============================================================

namespace BattleGame.Units
{
    // ══════════════════════════════════════════
    // ① 병사 스폰 시스템
    // ══════════════════════════════════════════

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SoldierSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnSoldiersRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (request, identity, health, attack, movement, generalEntity)
                     in SystemAPI.Query<
                            RefRO<SpawnSoldiersRequest>,
                            RefRO<UnitIdentityComponent>,
                            RefRO<HealthComponent>,
                            RefRO<AttackComponent>,
                            RefRO<MovementComponent>>()
                        .WithEntityAccess())
            {
                // 장군 스탯 스냅샷
                float genMaxHp        = health.ValueRO.MaxHp;
                float genDefense      = health.ValueRO.Defense;
                float genAttackDamage = attack.ValueRO.AttackDamage;
                float genAttackRange  = attack.ValueRO.AttackRange;
                float genAttackSpeed  = attack.ValueRO.AttackSpeed;
                float genMoveSpeed    = movement.ValueRO.MoveSpeed;

                float ratio  = request.ValueRO.StatScaleRatio;
                int   count  = request.ValueRO.Count;
                Entity prefab = request.ValueRO.SoldierPrefab;
                TeamType team = identity.ValueRO.Team;

                // 병사 스폰
                for (int i = 0; i < count; i++)
                {
                    Entity soldier = ecb.Instantiate(prefab);

                    // 소속 정보 갱신
                    ecb.SetComponent(soldier, new UnitIdentityComponent
                    {
                        UnitId = i,
                        Team   = team,
                        Type   = UnitType.Soldier,
                    });

                    // 장군 스탯 × 비율로 스탯 덮어쓰기
                    float soldierMaxHp = genMaxHp * ratio;
                    ecb.SetComponent(soldier, new HealthComponent
                    {
                        CurrentHp = soldierMaxHp,
                        MaxHp     = soldierMaxHp,
                        Defense   = genDefense * ratio,
                        IsDead    = false,
                    });
                    ecb.SetComponent(soldier, new AttackComponent
                    {
                        AttackDamage   = genAttackDamage * ratio,
                        AttackRange    = genAttackRange,      // 사거리는 비율 적용 제외
                        AttackSpeed    = genAttackSpeed * ratio,
                        AttackCooldown = 0f,
                        HasTarget      = false,
                    });
                    ecb.SetComponent(soldier, new MovementComponent
                    {
                        MoveSpeed        = genMoveSpeed * ratio,
                        StoppingDistance = 0.5f,
                        Destination      = float3.zero,
                        IsMoving         = false,
                    });

                    // 소속 장군 링크
                    ecb.SetComponent(soldier, new SoldierComponent
                    {
                        GeneralEntity  = generalEntity,
                        StatScaleRatio = ratio,
                        IsInitialized  = true,
                    });
                }

                // 요청 제거 (1회성)
                ecb.RemoveComponent<SpawnSoldiersRequest>(generalEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    // ══════════════════════════════════════════
    // ② 패시브 버프 오라 시스템
    // ══════════════════════════════════════════

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PassiveSkillAuraSystem : ISystem
    {
        // 1초마다 버프를 갱신하고, 버프 지속시간은 2초로 설정한다.
        // → 갱신이 한 번 누락되면 2초 후 자연 만료 (장군 사망·범위 이탈 처리)
        const float RefreshInterval   = 1f;
        const float BuffDuration      = 2f;

        float _timer;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GeneralPassiveSkillComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _timer += SystemAPI.Time.DeltaTime;
            if (_timer < RefreshInterval) return;
            _timer = 0f;

            // ── Step 1. 살아 있는 장군의 오라 정보를 수집 ────────────
            var auraList = new NativeList<AuraInfo>(8, Allocator.Temp);

            foreach (var (identity, passive, general, transform)
                     in SystemAPI.Query<
                            RefRO<UnitIdentityComponent>,
                            RefRO<GeneralPassiveSkillComponent>,
                            RefRO<GeneralComponent>,
                            RefRO<LocalTransform>>()
                        .WithNone<DeadTag>())
            {
                // AuraRadius 가 0 이면 지휘 반경(CommandRadius) 전체를 적용
                float radius = passive.ValueRO.AuraRadius > 0f
                    ? passive.ValueRO.AuraRadius
                    : general.ValueRO.CommandRadius;

                auraList.Add(new AuraInfo
                {
                    Team       = identity.ValueRO.Team,
                    Position   = transform.ValueRO.Position,
                    Radius     = radius,
                    EffectType = StatToStatusEffect(passive.ValueRO.BuffStat),
                    BuffValue  = passive.ValueRO.BuffValue,
                });
            }

            if (auraList.Length == 0)
            {
                auraList.Dispose();
                return;
            }

            // ── Step 2. 살아 있는 병사마다 오라 범위 안에 있는지 확인 ──
            foreach (var (identity, transform, buffersRO)
                     in SystemAPI.Query<
                            RefRO<UnitIdentityComponent>,
                            RefRO<LocalTransform>,
                            DynamicBuffer<StatusEffectBufferElement>>()
                        .WithAll<SoldierComponent>()
                        .WithNone<DeadTag>())
            {
                // DynamicBuffer 는 내부 포인터를 공유하므로 복사해도 같은 메모리를 가리킨다.
                // foreach 분해식 변수는 C# 컴파일러가 쓰기를 막으므로 로컬 변수로 받는다.
                var activeBuffs = buffersRO;

                for (int i = 0; i < auraList.Length; i++)
                {
                    AuraInfo aura = auraList[i];

                    // 다른 팀 장군의 오라는 무시
                    if (aura.Team != identity.ValueRO.Team)
                        continue;

                    // 오라 반경 밖에 있으면 무시
                    float distSq = math.distancesq(transform.ValueRO.Position, aura.Position);
                    if (aura.Radius > 0f && distSq > aura.Radius * aura.Radius)
                        continue;

                    // ── Step 3. 이미 같은 버프가 있으면 지속시간만 갱신,
                    //           없으면 새로 추가 ──────────────────────
                    bool alreadyHasBuff = false;

                    for (int j = 0; j < activeBuffs.Length; j++)
                    {
                        StatusEffectBufferElement existingBuff = activeBuffs[j];

                        bool sameType  = existingBuff.EffectType == aura.EffectType;
                        bool sameValue = math.abs(existingBuff.Magnitude - aura.BuffValue) < 0.01f;

                        if (sameType && sameValue)
                        {
                            existingBuff.Remaining  = BuffDuration;
                            existingBuff.Duration   = BuffDuration;
                            activeBuffs[j]          = existingBuff;
                            alreadyHasBuff          = true;
                            break;
                        }
                    }

                    if (!alreadyHasBuff)
                    {
                        activeBuffs.Add(new StatusEffectBufferElement
                        {
                            EffectType = aura.EffectType,
                            Magnitude  = aura.BuffValue,
                            Duration   = BuffDuration,
                            Remaining  = BuffDuration,
                        });
                    }
                }
            }

            auraList.Dispose();
        }

        static StatusEffectType StatToStatusEffect(StatType stat) => stat switch
        {
            StatType.Attack    => StatusEffectType.AttackBuff,
            StatType.Defense   => StatusEffectType.DefenseBuff,
            StatType.MoveSpeed => StatusEffectType.SpeedBuff,
            _                  => StatusEffectType.AttackBuff,
        };
    }

    internal struct AuraInfo
    {
        public TeamType          Team;
        public float3            Position;
        public float             Radius;
        public StatusEffectType  EffectType; // BuffStat 을 미리 변환해 저장 (루프 내 변환 제거)
        public float             BuffValue;
    }

    // ══════════════════════════════════════════
    // ③ 액티브 스킬 쿨다운 시스템
    // ══════════════════════════════════════════

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ActiveSkillCooldownSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GeneralActiveSkillComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            // 쿨다운 감소
            foreach (var skill in SystemAPI.Query<RefRW<GeneralActiveSkillComponent>>()
                                           .WithNone<DeadTag>())
            {
                if (skill.ValueRO.CooldownRemaining > 0f)
                    skill.ValueRW.CooldownRemaining -= dt;
            }

            // UseActiveSkillTag 처리 — 쿨다운 리셋 + 태그 제거
            // 실제 스킬 실행(트윈/이동/공격 제어)은 외부 스킬 실행기에서 수행
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (skill, entity)
                     in SystemAPI.Query<RefRW<GeneralActiveSkillComponent>>()
                        .WithAll<UseActiveSkillTag>()
                        .WithNone<DeadTag>()
                        .WithEntityAccess())
            {
                if (!skill.ValueRO.IsReady)
                {
                    // 쿨다운 미완료 — 요청 무시
                    ecb.RemoveComponent<UseActiveSkillTag>(entity);
                    continue;
                }

                skill.ValueRW.CooldownRemaining = skill.ValueRO.Cooldown;
                ecb.RemoveComponent<UseActiveSkillTag>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
