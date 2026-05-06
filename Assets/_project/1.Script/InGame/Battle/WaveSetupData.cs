using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  WaveSetupData.cs
//  웨이브 구성을 에디터에서 설정하는 ScriptableObject.
//
//  생성: Assets 우클릭 → Create → BattleGame → WaveSetupData
//  InGameManager 의 WaveSetup 슬롯에 할당한다.
//
//  추후 서버 테이블로 전환 시 이 파일 대신
//  테이블 데이터를 List<WaveData> 로 변환해 NormalMode 에 넘기면 된다.
// ============================================================

[CreateAssetMenu(fileName = "WaveSetupData", menuName = "BattleGame/WaveSetupData")]
public class WaveSetupData : ScriptableObject
{
    [Tooltip("웨이브 목록. 인덱스 0 = 1웨이브.")]
    public List<WaveData> Waves = new();
}
