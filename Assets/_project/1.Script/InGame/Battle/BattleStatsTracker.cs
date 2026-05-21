using System.Collections.Generic;
using Unity.Entities;

// ============================================================
//  BattleStatsTracker.cs
//  전투 통계 저장 매니저 (SingletonPure).
//
//  BattleStatCollectorSystem 이 매 프레임 DamageResultBuffer 를 읽어
//  해당 장군의 GeneralStatEntry 에 귀속한다.
//  UnitHealSystem 이 HealEventBuffer 처리 후 힐량을 기록한다.
//
//  딜 분류 (DamageCategory):
//    General — 장군 일반 공격
//    Soldier — 병사 일반 공격
//    Skill   — 스킬 직접 타격(HitType.Skill) + 소환 유닛(SummonedTag)
// ============================================================

public class BattleStatsTracker : SingletonPure<BattleStatsTracker>
{
    readonly Dictionary<Entity, GeneralStatEntry> _entries = new();

    // ── 등록 / 초기화 ─────────────────────────────────────────

    /// <summary>전투 시작 시 장군 Entity 를 등록한다.</summary>
    public void RegisterGeneral(Entity generalEntity, string generalName)
    {
        if (!_entries.ContainsKey(generalEntity))
            _entries[generalEntity] = new GeneralStatEntry { GeneralName = generalName };
    }

    /// <summary>전투 종료 또는 재시작 전 통계 초기화.</summary>
    public void Reset() => _entries.Clear();

    // ── 조회 ─────────────────────────────────────────────────

    public GeneralStatEntry GetEntry(Entity generalEntity)
        => _entries.TryGetValue(generalEntity, out var e) ? e : null;

    public IReadOnlyCollection<GeneralStatEntry> GetAllEntries() => _entries.Values;

    // ── 기록 (BattleStatCollectorSystem / UnitHealSystem 에서 호출) ──

    public void RecordDamage(Entity generalEntity, float amount, DamageCategory category)
    {
        if (!_entries.TryGetValue(generalEntity, out var e)) return;
        switch (category)
        {
            case DamageCategory.General: e.GeneralDamageDealt += amount; break;
            case DamageCategory.Soldier: e.SoldierDamageDealt += amount; break;
            case DamageCategory.Skill:   e.SkillDamageDealt   += amount; break;
        }
        e.TotalDamageDealt = e.GeneralDamageDealt + e.SoldierDamageDealt + e.SkillDamageDealt;
    }

    public void RecordAbsorbed(Entity generalEntity, float amount)
    {
        if (_entries.TryGetValue(generalEntity, out var e))
            e.DamageAbsorbed += amount;
    }

    public void RecordDamageTaken(Entity generalEntity, float amount, bool isSoldierHit)
    {
        if (!_entries.TryGetValue(generalEntity, out var e)) return;
        if (isSoldierHit) e.SoldierDamageTaken += amount;
        else              e.DamageTaken        += amount;
    }

    public void RecordKill(Entity generalEntity)
    {
        if (_entries.TryGetValue(generalEntity, out var e))
            e.KillCount++;
    }

    public void RecordHealingDone(Entity generalEntity, float amount)
    {
        if (_entries.TryGetValue(generalEntity, out var e))
            e.HealingDone += amount;
    }

    public void RecordHealingReceived(Entity generalEntity, float amount)
    {
        if (_entries.TryGetValue(generalEntity, out var e))
            e.HealingReceived += amount;
    }
}

// ── 딜 분류 ──────────────────────────────────────────────────

public enum DamageCategory : byte
{
    General = 0,  // 장군 일반 공격
    Soldier = 1,  // 병사 일반 공격
    Skill   = 2,  // 스킬 직접 타격 + 소환 유닛
}

// ── 장군 1명 분 통계 항목 ─────────────────────────────────────

public class GeneralStatEntry
{
    public string GeneralName;

    // 딜량
    public float GeneralDamageDealt;   // 장군 일반 공격
    public float SoldierDamageDealt;   // 병사 일반 공격
    public float SkillDamageDealt;     // 스킬 + 소환 유닛
    public float TotalDamageDealt;     // 위 세 항목 합산 (캐시)

    // 피해량
    public float DamageTaken;          // 장군 본인이 받은 피해
    public float SoldierDamageTaken;   // 병사가 받은 피해
    public float DamageAbsorbed;       // 방어로 감소된 피해 합산

    // 처치
    public int KillCount;

    // 힐량
    public float HealingDone;      // 장군 팀이 가한 힐량
    public float HealingReceived;  // 장군 팀이 받은 힐량
}
