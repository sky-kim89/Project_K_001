using UnityEngine;

// ============================================================
//  SpawnEntry.cs
//  웨이브 하나에 등록하는 스폰 항목.
//  추후 ScriptableObject 테이블로 전환 예정 — 구조만 미리 정의.
//
//  사용 예:
//    new SpawnEntry { PoolKey = "Enemy_Goblin", Count = 3, DelayBetween = 0.5f }
// ============================================================

[System.Serializable]
public class SpawnEntry
{
    [Tooltip("PoolController 에 등록된 유닛 풀 키 (프리팹 이름과 동일)")]
    public string PoolKey;

    [Tooltip("직업·스텟 결정에 사용할 고유 유닛 이름 (비어있으면 PoolKey 로 대체)")]
    public string UnitName;

    [Tooltip("유닛 레벨 — 스텟 배율에 반영")]
    public int Level = 1;

    [Tooltip("스폰할 유닛 종류 — 스포너 분류 및 추후 테이블용")]
    public SpawnUnitType UnitType;

    [Tooltip("이 항목에서 스폰할 수")]
    public int Count = 1;

    [Tooltip("같은 항목 내 유닛 간 스폰 간격 (초)")]
    public float DelayBetween = 0.5f;

    [Tooltip("이 항목 시작 전 대기 시간 (초)")]
    public float DelayBefore = 0f;
}
