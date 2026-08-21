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

    // ── 추출 ──────────────────────────────────────────────────
    //
    //  ■ 등급을 먼저 뽑고, 그 등급 안에서 균등하게 고른다
    //    예전엔 개체마다 1/아이템레벨 가중치를 매겨 한 번에 뽑았다. 그러면
    //    **등급별 종수가 곧 그 등급의 출현 확률**이 된다 — Epic 장비를 5종에서
    //    12종으로 늘렸더니 Epic 드랍이 12% → 22% 로 저절로 올라갔다.
    //    데이터를 추가했을 뿐인데 드랍 테이블이 바뀌는 구조라 밸런스를 잡을 수 없다.
    //
    //    지금은 등급 확률(GradeWeight)이 종수와 무관하게 고정이고, 그 안에서만
    //    균등 추첨한다. 장비를 몇 개 더 만들어도 등급 분포는 그대로다.
    //
    //  ⚠ 등급 가중치는 예전 공식(1/아이템레벨)을 그대로 옮긴 값이다
    //    Normal 1 / Uncommon 1/2 / Rare 1/3 / Unique 1/4 / Epic 1/5.
    //    체감 드랍률을 바꾸려면 이 표만 고치면 된다 — 종수는 건드릴 필요가 없다.

    static float GradeWeight(UnitGrade grade) => 1f / (1 + (int)grade);

    /// <summary>
    /// 스테이지 레벨에 따라 장비 1개를 추출한다.
    /// 등급을 먼저 뽑고 그 등급 안에서 균등 선택 — 종수가 확률을 바꾸지 않는다.
    /// </summary>
    public EquipmentData PickRandom(int stageLevel, UnitGrade minGrade = UnitGrade.Normal)
        => PickFromPool(GetDropPool(stageLevel, minGrade), () => Random.value);

    /// <summary>결정론적 시드 기반 추출 (System.Random 사용). minGrade = 등급 하한.</summary>
    public EquipmentData PickRandom(int stageLevel, System.Random rng,
                                    UnitGrade minGrade = UnitGrade.Normal)
        => PickFromPool(GetDropPool(stageLevel, minGrade), () => (float)rng.NextDouble());

    static EquipmentData PickFromPool(List<EquipmentData> pool, System.Func<float> roll01)
    {
        if (pool == null || pool.Count == 0) return null;

        // 풀에 실제로 들어 있는 등급만 후보로 둔다 — 초반 스테이지는 상위 등급이 아예 없다
        var byGrade = new Dictionary<UnitGrade, List<EquipmentData>>();
        foreach (var e in pool)
        {
            if (!byGrade.TryGetValue(e.Grade, out var list))
                byGrade[e.Grade] = list = new List<EquipmentData>();
            list.Add(e);
        }

        float total = 0f;
        foreach (var kv in byGrade) total += GradeWeight(kv.Key);

        float pick       = roll01() * total;
        float cumulative = 0f;
        foreach (var kv in byGrade)
        {
            cumulative += GradeWeight(kv.Key);
            if (pick <= cumulative)
            {
                var list = kv.Value;
                int idx  = Mathf.Clamp((int)(roll01() * list.Count), 0, list.Count - 1);
                return list[idx];
            }
        }

        return pool[^1];
    }
}
