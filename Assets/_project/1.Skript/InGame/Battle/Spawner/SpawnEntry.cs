using UnityEngine;

// ============================================================
//  SpawnEntry.cs
//  웨이브 하나에 등록하는 스폰 항목.
//
//  ■ Name
//    유닛 고유 이름. EnemyStatRoller / EnemyAppearanceRoller 의 랜덤 시드로 사용.
//    같은 Name 은 항상 같은 스텟·외형을 가진다.
//
//  ■ UnitType
//    PoolController 풀 키로 자동 변환된다. (Enemy / Elite / Boss / General)
//    별도의 PoolKey 필드는 없으며 UnitType.ToString() 이 키가 된다.
//
//  ■ EnemyRace
//    이 항목의 종족. WaveData.DefaultRace 를 기본값으로 사용하고
//    항목별로 다르게 설정할 수 있다.
// ============================================================

[System.Serializable]
public class SpawnEntry
{
    [Tooltip("유닛 고유 이름. 스텟·외형 랜덤 시드 및 식별자로 사용.\n" +
             "같은 이름은 항상 같은 스텟·외형을 가진다.")]
    public string Name;

    [Tooltip("유닛 레벨 — 스텟 배율에 반영")]
    public int Level = 1;

    [Tooltip("유닛 종류. PoolController 풀 키로 자동 변환된다.\n" +
             "(Enemy → \"Enemy\", Elite → \"Elite\", Boss → \"Boss\", General → \"General\")")]
    public SpawnUnitType UnitType;

    [Tooltip("이 항목에서 스폰할 수")]
    public int Count = 1;

    [Tooltip("이 항목 시작 전 대기 시간 (초)")]
    public float DelayBefore = 0f;

    [Tooltip("같은 항목 내 유닛 간 스폰 간격 (초)")]
    public float DelayBetween = 0.5f;

    [Tooltip("적군 종족. WaveData.DefaultRace 를 기본값으로 사용하되 항목별로 덮어쓸 수 있다.")]
    public EnemyRace EnemyRace = EnemyRace.Orc;

    /// <summary>PoolController 에 전달할 풀 키. UnitType 에서 자동 파생.</summary>
    public string PoolKey => UnitType.ToString();
}
