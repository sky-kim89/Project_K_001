using System.Collections;
using UnityEngine;

// ============================================================
//  EchoSkillRunner.cs
//  연속 시전(MageEchoSkill) 의 재발동을 지연 실행하는 MonoBehaviour.
//
//  ■ 왜 즉시 실행하면 안 되나
//    ① 같은 프레임에 두 번 터지면 이펙트가 완전히 겹쳐 한 번 쓴 것처럼 보인다.
//    ② 더 나쁜 건 러너 기반 스킬(일도양단·화살 폭풍 등)이다.
//       각 Runner 는 새 시전이 들어오면 StopCoroutine 으로 진행 중인 시퀀스를 끊는다.
//       즉시 에코를 걸면 **원본 시전이 취소되고 에코만 남는다** — 피해 -40% 짜리
//       한 번만 나가는 셈이라 특성이 손해가 된다.
//
//  ■ 지연을 둬도 남는 한계
//    시전 시간이 지연보다 긴 스킬(일도양단 2.35초 등)은 여전히 원본이 끊긴다.
//    지연을 시전 길이만큼 늘리려면 스킬마다 "총 시전 시간" 을 알아야 하므로
//    지금은 고정 지연으로 두고, 필요해지면 ActiveSkillData 에 필드를 추가한다.
// ============================================================

public class EchoSkillRunner : MonoBehaviour
{
    public void Echo(ActiveSkillData data, ActiveSkillContext ctx, float delay, float effectScale)
        => StartCoroutine(Run(data, ctx, delay, effectScale));

    IEnumerator Run(ActiveSkillData data, ActiveSkillContext ctx, float delay, float effectScale)
    {
        yield return new WaitForSeconds(delay);

        // 지연 중에 시전자가 죽었으면 조용히 취소한다
        if (!ctx.EntityManager.Exists(ctx.CasterEntity)) yield break;

        // 피해 -40%: EffectValue 를 잠시 줄인 뒤 실행하고 곧바로 되돌린다
        float orig = data.EffectValue;
        data.EffectValue = orig * effectScale;
        data.Execute(ctx);
        data.EffectValue = orig;
    }
}
