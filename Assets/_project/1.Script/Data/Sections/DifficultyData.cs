using System;
using UnityEngine;

// ============================================================
//  DifficultyData.cs
//  난이도 선택 상태 저장 섹션.
//
//  SelectedTier  : 지금 고른 등급. 런 시작 시점에 고정된다.
//  ClearedTier   : 해금 기준을 넘겨 본 최고 등급. 이 값 +1 까지 선택할 수 있다.
//                  기록 시점은 InGameManager.TryUnlockNextDifficulty —
//                  StageConfig.DifficultyUnlockStage(기본 20) 도달이다.
//
//  ⚠ 환생으로 초기화하지 않는다
//    런 데이터가 아니라 계정 진행도다. 해금 기록이 날아가면
//    다시 처음 등급부터 완주해야 한다.
//
//  ⚠ 런 도중에는 바꿀 수 없다
//    30스테이지를 쉬운 등급으로 깔고 마지막만 올리는 악용을 막는다.
//    StageProgressData.RunInProgress 를 보고 UI 에서 잠근다.
// ============================================================

public class DifficultyData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.Difficulty;

    RawData _raw = new();

    /// <summary>선택 변경 알림 — 상단 배지·난이도 팝업이 구독한다.</summary>
    public static event Action OnChanged;

    // ── 읽기 ────────────────────────────────────────────────────

    public DifficultyTier SelectedTier => (DifficultyTier)_raw.SelectedTier;

    /// <summary>완주해 본 최고 등급. 아직 하나도 못 깼으면 -1.</summary>
    public int ClearedTierIndex => _raw.ClearedTier;

    /// <summary>선택 가능한 최고 등급 인덱스 — 완주한 다음 등급까지.</summary>
    public int MaxSelectableIndex =>
        Mathf.Min(_raw.ClearedTier + 1, Enum.GetValues(typeof(DifficultyTier)).Length - 1);

    public bool IsUnlocked(DifficultyTier tier) => (int)tier <= MaxSelectableIndex;

    // ── 쓰기 ────────────────────────────────────────────────────

    public void Select(DifficultyTier tier)
    {
        if (!IsUnlocked(tier)) return;
        if (_raw.SelectedTier == (int)tier) return;

        _raw.SelectedTier = (int)tier;
        OnChanged?.Invoke();
    }

    /// <summary>런 완주 시 호출. 더 높은 등급을 깼을 때만 기록이 올라간다.</summary>
    public void RecordClear(DifficultyTier tier)
    {
        if ((int)tier <= _raw.ClearedTier) return;
        _raw.ClearedTier = (int)tier;
        OnChanged?.Invoke();
    }

    // ── ISaveSection ────────────────────────────────────────────

    public string Serialize() => JsonUtility.ToJson(_raw);

    public void Deserialize(string json)
    {
        _raw = JsonUtility.FromJson<RawData>(json) ?? new RawData();
        OnChanged?.Invoke();
    }

    public void SetDefaults()
    {
        _raw = new RawData();
        OnChanged?.Invoke();
    }

    [Serializable]
    class RawData
    {
        public int SelectedTier = 0;    // 출정
        public int ClearedTier  = -1;   // 아직 아무것도 완주하지 않음
    }
}
