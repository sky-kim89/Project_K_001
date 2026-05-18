using BattleGame.Units;

// ============================================================
//  AbilityArmorReaction.cs  [Special / OnHit]
//  C02 철갑 반응 — 피격마다 방어율 +DefenseBuff 영구 누적.
//  전투 내내 유지 (Duration -1), 스테이지 종료 시 초기화.
//  버퍼 항목 폭증 방지: 기존 영구 Defense Add 항목에 수치를 합산.
//  DefenseMax(95%) 클램프는 UnitStatusEffectSystem 에서 처리.
// ============================================================

[UnityEngine.CreateAssetMenu(fileName = "Ability_C02_ArmorReaction", menuName = "ProjectK/Ability/Special/ArmorReaction")]
public class AbilityArmorReaction : AbilityData
{
    [UnityEngine.Header("철갑 반응 설정")]
    [UnityEngine.Range(0f, 0.5f)]
    public float DefenseBuff = 0.01f;

    public override string Description
        => $"피격마다 방어율 +{DefenseBuff * 100f:0}%  (전투 내 누적, 최대 95%)";

    public override PassiveTrigger GetTriggerType() => PassiveTrigger.OnHit;

    public override void OnTrigger(PassiveTriggerContext ctx)
    {
        var em = ctx.EntityManager;
        if (!em.HasBuffer<StatusEffectBufferElement>(ctx.GeneralEntity)) return;

        var buf = em.GetBuffer<StatusEffectBufferElement>(ctx.GeneralEntity);

        // 기존 영구 방어율 Add 항목에 수치 합산 (버퍼 항목 증가 방지)
        for (int i = 0; i < buf.Length; i++)
        {
            var b = buf[i];
            if (b.Stat == StatType.Defense && b.Mode == EffectMode.Add && b.Duration < 0f)
            {
                b.Delta += DefenseBuff;
                buf[i]   = b;
                return;
            }
        }

        // 최초 피격 — 신규 항목 추가 (Duration -1 = 전투 종료까지 유지)
        buf.Add(new StatusEffectBufferElement
        {
            Stat       = StatType.Defense,
            Delta      = DefenseBuff,
            Mode       = EffectMode.Add,
            Duration   = -1f,
            Remaining  = float.MaxValue,
            SourceType = BuffSourceType.Ability,
            SourceId   = (int)Id,
        });
    }
}
