using UnityEngine;
using BattleGame.Units;   // SkillCastLockUtil

// ============================================================
//  ActiveBossSlam.cs
//  분쇄 강타 — 타겟 위치로 도약해 내려찍고, 착지점을 대반경으로 터뜨린다.
//  도약 충격파 — 같은 착지 로직을 쓰되 제자리에서 낮게 뛴다.
//
//  ■ 돌진과 정반대로 설계했다
//    돌진 : 이동한다 / 좁은 경로 / 밀어낸다
//    강타 : 타겟 도약 / 넓은 원 / 착지점에서 밀어낸다
//    충격파: 제자리   / 작은 원 / 낮게 뛰고 밀어낸다
//
//  ■ 예고가 핵심이다
//    오토배틀이라 플레이어가 피할 수는 없다. 그래도 예고를 두는 이유는
//    "무슨 일이 벌어졌는지" 를 알려주기 위해서다. 예고 없이 갑자기 절반이
//    죽으면 버그로 보인다.
//
//  ■ 이펙트 키
//    BaseEffect   : 예고 장판 (착탄 반경 그대로)
//    CasterEffect : 착지 충격파
//    TargetEffect : 피격 대상마다
// ============================================================

[CreateAssetMenu(fileName = "Active_BossSlam", menuName = "BattleGame/Actives/BossSlam")]
public class ActiveBossSlam : ActiveSkillData
{
    [Header("연출")]
    [Tooltip("팔을 들고 예고하는 시간 (초)")]
    public float WindupTime = 0.6f;

    [Tooltip("도약 높이")]
    public float JumpHeight = 3.2f;

    [Tooltip("도약 시작부터 착지까지 걸리는 시간 (초)")]
    public float SlamTime = 0.65f;

    [Tooltip("착지 후 경직 (초)")]
    public float RecoverTime = 0.5f;

    [Tooltip("타겟 위치로 날아가면 켠다. 끄면 제자리 점프한다.")]
    public bool LeapToTarget = true;

    [Tooltip("타겟 도약 최대 거리. 시야 밖까지 따라가지 않게 제한한다.")]
    public float MaxLeapDistance = 9f;

    [Header("피해")]
    [Tooltip("공격력 배율 — 한 방이 무거워야 한다")]
    public float DamageMultiplier = 3.5f;

    [Tooltip("착탄 반경 — 돌진보다 훨씬 넓다")]
    public float SlamRadius = 7f;

    [Tooltip("넉백 배율 — 바깥으로 밀어낸다")]
    public float KnockbackMult = 9f;

    public override void Execute(ActiveSkillContext context)
    {
        if (context.CasterObject == null) return;

        var runner = context.CasterObject.GetComponent<BossSlamRunner>()
                  ?? context.CasterObject.AddComponent<BossSlamRunner>();

        float lockTime = WindupTime + SlamTime + RecoverTime;
        SkillCastLockUtil.Apply(context.EntityManager, context.CasterEntity, lockTime);

        runner.Run(this, context);
    }
}
