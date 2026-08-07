using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================
//  EquipmentDatabase.cs
//  모든 EquipmentData SO 를 보관하는 컬렉션 에셋.
//
//  드롭 레벨 계산:
//    스테이지 N → 아이템 레벨 1 ~ N 범위에서 가중치 추출
//    (높은 레벨일수록 낮은 가중치 — 고레벨 장비는 희귀)
// ============================================================

[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "BattleGame/EquipmentDatabase")]
public class EquipmentDatabase : ScriptableObject
{
    // ── 싱글턴 참조 ───────────────────────────────────────────
    static EquipmentDatabase _current;
    public static EquipmentDatabase Current
        => _current != null ? _current : (_current = Resources.Load<EquipmentDatabase>("EquipmentDatabase"));

    // ── 데이터 ────────────────────────────────────────────────
    public List<EquipmentData> Equipments = new();

    // ── 조회 ──────────────────────────────────────────────────

    public EquipmentData Get(string id)
        => Equipments.Find(e => e != null && e.EquipmentId == id);

    public List<EquipmentData> GetByGrade(UnitGrade grade)
        => Equipments.Where(e => e != null && e.Grade == grade).ToList();

    /// <summary>
    /// 스테이지 레벨 이하의 아이템 레벨을 가진 장비 풀 반환.
    /// 스테이지 클리어 보상 선택지 생성 시 사용.
    /// </summary>
    /// <param name="minGrade">
    /// 등급 하한. 런 상점처럼 저등급을 취급하지 않는 곳이 쓴다.
    /// ⚠ 등급과 아이템 레벨은 사실상 1:1(Normal=1 … Epic=5)이라
    ///   하한을 걸면 초반 스테이지에서 풀이 통째로 비어 버린다.
    ///   그 경우 하한 등급만은 스테이지 레벨과 무관하게 열어 준다
    ///   — 상점이 빈 칸으로 뜨는 것보다 낫다.
    /// </param>
    public List<EquipmentData> GetDropPool(int stageLevel, UnitGrade minGrade = UnitGrade.Normal)
    {
        var pool = Equipments
            .Where(e => e != null && e.Grade >= minGrade && e.ItemLevel <= stageLevel)
            .ToList();

        if (pool.Count > 0 || minGrade == UnitGrade.Normal) return pool;

        return Equipments.Where(e => e != null && e.Grade == minGrade).ToList();
    }

    /// <summary>
    /// 스테이지 레벨에 따라 장비 1개를 가중치 랜덤으로 추출.
    /// 아이템 레벨이 높을수록 낮은 가중치 (희귀).
    ///   weight = 1 / itemLevel
    /// </summary>
    public EquipmentData PickRandom(int stageLevel)
    {
        var pool = GetDropPool(stageLevel);
        if (pool.Count == 0) return null;

        float totalWeight = pool.Sum(e => 1f / e.ItemLevel);
        float roll        = Random.value * totalWeight;
        float cumulative  = 0f;

        foreach (var equip in pool)
        {
            cumulative += 1f / equip.ItemLevel;
            if (roll <= cumulative) return equip;
        }
        return pool[^1];
    }

    /// <summary>결정론적 시드 기반 추출 (System.Random 사용). minGrade = 등급 하한.</summary>
    public EquipmentData PickRandom(int stageLevel, System.Random rng,
                                    UnitGrade minGrade = UnitGrade.Normal)
    {
        var pool = GetDropPool(stageLevel, minGrade);
        if (pool.Count == 0) return null;

        float totalWeight = pool.Sum(e => 1f / e.ItemLevel);
        float roll        = (float)(rng.NextDouble() * totalWeight);
        float cumulative  = 0f;

        foreach (var equip in pool)
        {
            cumulative += 1f / equip.ItemLevel;
            if (roll <= cumulative) return equip;
        }
        return pool[^1];
    }
}
