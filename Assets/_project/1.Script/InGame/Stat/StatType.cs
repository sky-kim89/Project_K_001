using System;
using Unity.Collections;

// ============================================================
//  StatType.cs
//  스텟 시스템 관련 enum / 구조체 정의
// ============================================================

// ── 스텟 종류 ─────────────────────────────────────────────────
public enum StatType
{
    MaxHp        = 0,
    Defense      = 1,   // 방어율 0~1
    Attack       = 2,
    AttackRange  = 3,
    AttackSpeed  = 4,   // 초당 공격 횟수
    MoveSpeed    = 5,
    CritChance    = 6,   // 크리티컬 확률 0~1
    CritDamage    = 7,   // 크리티컬 배율 (기본 1.5)
    SoldierCount  = 8,   // 장군이 지휘하는 병사 수
    CommandPower  = 9,   // 병사 지휘력 — 1포인트당 병사 스텟 1% 증가
    // 새 스텟은 여기에 순서대로 추가 — 다른 코드 수정 불필요 (최대 127개)
}

// ── 레이어 간 결합 방식 ────────────────────────────────────────
public enum CombineMode
{
    Add,
    Multiply,
    Max,
}

// ── 단일 스텟 수정자 ───────────────────────────────────────────
[Serializable]
public struct StatModifier
{
    public StatType Type;
    public float    Value;

    [UnityEngine.Tooltip("레이어 키 — 비워두면 base 레이어에 추가됩니다.\n" +
                         "예) equip_sword / buff_rage / skill_passive")]
    public string Key;
}

// ── StatBlock ─────────────────────────────────────────────────
/// <summary>
/// StatType 을 인덱스로 사용하는 가변 길이 float 배열.
/// Burst·ECS 호환 비관리형(unmanaged) 구조체.
///
/// 사용법:
///   float atk = stat.Final[StatType.Attack];
///   stat.Final[StatType.MoveSpeed] *= 1.3f;
///
/// 내부 저장소: FixedList512Bytes&lt;float&gt; = 최대 127 슬롯
/// → StatType 이 수십 개로 늘어나도 코드 수정 불필요
/// → 127개 초과 시 FixedList4096Bytes&lt;float&gt; 로 교체 (최대 1023개)
/// </summary>
public struct StatBlock
{
    // FixedList512Bytes<float> = (512 - 2) / 4 = 127 슬롯
    // unsafe 없이 Burst / ECS 에서 안전하게 사용 가능
    FixedList512Bytes<float> _data;

    // ── 인덱서 ────────────────────────────────────────────────

    public float this[StatType stat]
    {
        // 아직 설정되지 않은 슬롯은 0 반환 (안전 기본값)
        get
        {
            int i = (int)stat;
            return i < _data.Length ? _data[i] : 0f;
        }
        // 슬롯이 부족하면 0으로 자동 확장 후 저장
        set
        {
            int i = (int)stat;
            while (_data.Length <= i)
                _data.Add(0f);
            _data[i] = value;
        }
    }

    // ── 유틸리티 ──────────────────────────────────────────────

    /// <summary>UnitStat 계산 결과를 StatBlock 으로 변환 (Baker / 아웃게임 전용).</summary>
    public static StatBlock FromUnitStat(UnitStat unitStat)
    {
        var block = new StatBlock();
        foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            block[stat] = unitStat.Get(stat);
        return block;
    }
}
