using UnityEngine;

// ============================================================
//  StageConfig.cs
//  스테이지 난이도 수치를 정의하는 ScriptableObject.
//  StageGenerator 가 이 값을 읽어 WaveData 목록을 생성한다.
//  각 항목은 스테이지 진행도(0→1)에 따라 Min→Max 로 선형 보간된다.
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

    [Header("웨이브 수 (스테이지 진행도 0→1 에 따라 Min→Max 선형 보간)")]
    public int WaveCountMin = 1;
    public int WaveCountMax = 5;

    [Header("웨이브당 일반 적 수 (스테이지 진행도 0→1 에 따라 Min→Max 선형 보간)")]
    public int EnemyCountMin = 3;
    public int EnemyCountMax = 15;

    [Header("적 레벨 (스테이지 번호 1→NormalStageCount 에 따라 Min→Max 선형 보간)")]
    public int EnemyLevelMin = 1;
    public int EnemyLevelMax = 30;

    [Header("웨이브당 골드 보상 (스테이지 진행도 0→1 에 따라 Min→Max 선형 보간)")]
    public int GoldRewardMin = 50;
    public int GoldRewardMax = 600;
}
