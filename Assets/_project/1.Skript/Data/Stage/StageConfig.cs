using UnityEngine;

// ============================================================
//  StageConfig.cs
//  스테이지 난이도 커브를 정의하는 ScriptableObject.
//  StageGenerator 가 이 값을 읽어 WaveData 목록을 생성한다.
//
//  생성: 우클릭 > Create > Project K > Stage Config
// ============================================================

[CreateAssetMenu(fileName = "StageConfig", menuName = "Project K/Stage Config")]
public class StageConfig : ScriptableObject
{
    public static StageConfig Current { get; internal set; }

    [Header("스테이지 수")]
    public int NormalStageCount = 30;
    public int EliteStageCount  = 15;

    [Header("에너지 비용")]
    public int EnergyCostNormal = 5;
    public int EnergyCostElite  = 10;

    [Header("웨이브 수 (x=스테이지 진행도 0→1, y=웨이브 수)")]
    public AnimationCurve WaveCountCurve  = AnimationCurve.Linear(0, 1, 1, 5);

    [Header("웨이브당 일반 적 수 (x=스테이지 진행도, y=적 수)")]
    public AnimationCurve EnemyCountCurve = AnimationCurve.Linear(0, 3, 1, 15);

    [Header("적 레벨 (x=스테이지 번호, y=레벨)")]
    public AnimationCurve EnemyLevelCurve = AnimationCurve.Linear(0, 1, 30, 30);

    [Header("웨이브당 골드 보상 (x=스테이지 진행도, y=골드)")]
    public AnimationCurve GoldRewardCurve = AnimationCurve.Linear(0, 50, 1, 600);
}
