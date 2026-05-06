using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

// ============================================================
//  UnitStatusEffectSystem.cs
//  버프·디버프 처리 + 스텟 재계산 시스템
//
//  매 프레임 한 번, StatusEffectTickJob 이 다음을 수행한다:
//  1. 버프 타이머 감소 → 만료 시 제거
//  2. Dot(도트 데미지) → HitEventBuffer 에 피해 주입
//  3. StatComponent.Final 재계산:
//     Final = Base, 이후 Add 모드 적용, 마지막 Multiply 모드 적용
//
//  Add 먼저, Multiply 나중 순서 이유:
//    공격력 100 + 50(Add) = 150, × 1.3(Multiply) = 195
//    → 순서를 바꾸면 값이 달라지므로 일관성 유지
// ============================================================

namespace BattleGame.Units
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitHitSystem))]
    public partial struct UnitStatusEffectSystem : ISystem
    {
        // [BurstCompile] — GameplayConfig(관리형 객체) 접근이 필요해 Burst 제외
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime  = SystemAPI.Time.DeltaTime;
            float defenseMax = GameplayConfig.Current != null ? GameplayConfig.Current.DefenseMax : 0.95f;
            new StatusEffectTickJob { DeltaTime = deltaTime, DefenseMax = defenseMax }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct StatusEffectTickJob : IJobEntity
    {
        public float DeltaTime;
        public float DefenseMax;

        public void Execute(
            ref StatComponent                            stat,
            ref DynamicBuffer<StatusEffectBufferElement> buffs,
            ref DynamicBuffer<HitEventBufferElement>     hitBuffer)
        {
            // ── Step 1. 타이머 감소 & 만료 제거 & 도트 데미지 처리 ──
            for (int i = buffs.Length - 1; i >= 0; i--)
            {
                var buff = buffs[i];

                if (buff.Duration >= 0f) // Duration == -1 이면 영구 버프
                    buff.Remaining -= DeltaTime;

                if (buff.Remaining <= 0f)
                {
                    buffs.RemoveAtSwapBack(i);
                    continue;
                }

                // 도트 데미지 — 매 프레임 HitEvent 로 주입
                if (buff.Mode == EffectMode.Dot)
                {
                    hitBuffer.Add(new HitEventBufferElement
                    {
                        Damage         = buff.Delta * DeltaTime,
                        HitDirection   = float3.zero,
                        AttackerEntity = Entity.Null,
                    });
                }

                buffs[i] = buff;
            }

            // ── Step 2. Final = Base 로 초기화 ─────────────────────
            stat.ResetFinalToBase();

            // ── Step 3. Add 모드 버프 적용 ─────────────────────────
            for (int i = 0; i < buffs.Length; i++)
            {
                var buff = buffs[i];
                if (buff.Mode == EffectMode.Add)
                    stat.Final[buff.Stat] += buff.Delta;
            }

            // ── Step 4. Multiply 모드 버프 적용 ────────────────────
            for (int i = 0; i < buffs.Length; i++)
            {
                var buff = buffs[i];
                if (buff.Mode == EffectMode.Multiply)
                    stat.Final[buff.Stat] *= buff.Delta;
            }

            // ── Step 5. 스텟 하한선 보정 ────────────────────────────
            float defense    = stat.Final[StatType.Defense];
            float atkSpeed   = stat.Final[StatType.AttackSpeed];
            float moveSpeed  = stat.Final[StatType.MoveSpeed];
            float attack     = stat.Final[StatType.Attack];

            stat.Final[StatType.Defense]     = math.clamp(defense,   0f, DefenseMax);
            stat.Final[StatType.AttackSpeed] = math.max(atkSpeed,    0.1f);
            stat.Final[StatType.MoveSpeed]   = math.max(moveSpeed,   0.1f);
            stat.Final[StatType.Attack]      = math.max(attack,      0f);
        }
    }
}
