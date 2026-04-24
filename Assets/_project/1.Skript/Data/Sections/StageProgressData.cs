using System;
using UnityEngine;

// ============================================================
//  StageProgressData.cs
//  스테이지 클리어 진행 저장 섹션 (ISaveSection).
//
//  보유 데이터:
//  - 일반 스테이지 최대 클리어 번호
//  - 엘리트 스테이지 최대 클리어 번호
//
//  잠금 규칙:
//  - 일반  N 접근 : N == 1  ||  ClearedNormal >= N-1
//  - 엘리트 탭    : ClearedNormal >= 5
//  - 엘리트 N 접근: 엘리트 탭 잠금 해제 + (N == 1 || ClearedElite >= N-1)
// ============================================================

public class StageProgressData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.StageProgress;

    // ── 읽기 전용 프로퍼티 ───────────────────────────────────

    public int  ClearedNormalStages => _raw.ClearedNormal;
    public int  ClearedEliteStages  => _raw.ClearedElite;
    public bool IsEliteUnlocked     => _raw.ClearedNormal >= 5;

    // ── 내부 직렬화 데이터 ───────────────────────────────────

    RawData _raw = new();

    // ── 진행 기록 ────────────────────────────────────────────

    public void RecordClear(BattleMode mode, int stageNumber)
    {
        if (mode == BattleMode.Normal)
            _raw.ClearedNormal = Mathf.Max(_raw.ClearedNormal, stageNumber);
        else
            _raw.ClearedElite = Mathf.Max(_raw.ClearedElite, stageNumber);
    }

    // ── 잠금 판정 ────────────────────────────────────────────

    public bool IsUnlocked(BattleMode mode, int stageNumber)
    {
        if (mode == BattleMode.Normal)
            return stageNumber == 1 || _raw.ClearedNormal >= stageNumber - 1;

        // 엘리트 — 일반 5 클리어(탭 해제) + 일반 N 클리어 + 엘리트 N-1 클리어
        if (!IsEliteUnlocked) return false;
        if (_raw.ClearedNormal < stageNumber) return false;
        return stageNumber == 1 || _raw.ClearedElite >= stageNumber - 1;
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()              => JsonUtility.ToJson(_raw);
    public void   Deserialize(string json) => _raw = JsonUtility.FromJson<RawData>(json) ?? new RawData();
    public void   SetDefaults()            => _raw = new RawData();

    // ── 직렬화 전용 내부 클래스 ──────────────────────────────

    [Serializable]
    class RawData
    {
        public int ClearedNormal = 0;
        public int ClearedElite  = 0;
    }
}
