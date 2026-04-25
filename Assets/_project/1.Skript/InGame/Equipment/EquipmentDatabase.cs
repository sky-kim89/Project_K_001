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
    public static EquipmentDatabase Current { get; private set; }

    void OnEnable()  => Current = this;
    void OnDisable() { if (Current == this) Current = null; }

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
    public List<EquipmentData> GetDropPool(int stageLevel)
        => Equipments.Where(e => e != null && e.ItemLevel <= stageLevel).ToList();

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
}
