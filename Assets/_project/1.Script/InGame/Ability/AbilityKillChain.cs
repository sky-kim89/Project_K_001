using BattleGame.Units;

// ============================================================
//  AbilityKillChain.cs  [Special / OnEnemyKill]
//  C03 처치 연쇄 — 처치마다 공격력 +AttackBonusRatio% 누적.
//  버프는 전투 내내 유지 (Duration -1), 스테이지 종료 시 엔티티 파괴로 자동 초기화.
//  버퍼 항목 폭증 방지: 기존 영구 Attack Add 항목에 수치를 합산.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C03_KillChain", menuName = "ProjectK/Ability/Special/KillChain")]
public class AbilityKillChain : AbilityData
{
    [UnityEngine.Header("처치 연쇄 설정")]
    [UnityEngine.Range(0f, 1f)]
    public float AttackBonusRatio = 0.05f;

    public override string Description
        => $"처치마다 공격력 +{AttackBonusRatio * 100f:0}%  (전투 내 누적)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnEnemyKill;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasBuffer<StatusEffectBufferElement>(ctx.GeneralEntity)) return;
        if (!em.HasComponent<StatComponent>(ctx.GeneralEntity)) return;

        float bonus = em.GetComponentData<StatComponent>(ctx.GeneralEntity).Base[StatType.Attack] * AttackBonusRatio;
        var   buf   = em.GetBuffer<StatusEffectBufferElement>(ctx.GeneralEntity);

        // 기존 영구 공격력 Add 항목에 수치 합산 (버퍼 항목 증가 방지)
        for (int i = 0; i < buf.Length; i++)
        {
            var b = buf[i];
            if (b.Stat == StatType.Attack && b.Mode == EffectMode.Add && b.Duration < 0f)
            {
                b.Delta += bonus;
                buf[i]   = b;
                return;
            }
        }

        // 최초 처치 — 신규 항목 추가 (Duration -1 = 전투 종료까지 유지)
        buf.Add(new StatusEffectBufferElement
        {
            Stat      = StatType.Attack,
            Delta     = bonus,
            Mode      = EffectMode.Add,
            Duration  = -1f,
            Remaining = float.MaxValue,
        });
    }
}
