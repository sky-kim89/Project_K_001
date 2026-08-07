using System;
using UnityEngine;

// ============================================================
//  StageProgressData.cs
//  스테이지 클리어 진행 저장 섹션 (ISaveSection).
//
//  보유 데이터:
//  - 일반 스테이지 최대 클리어 번호
//  - 엘리트 스테이지 최대 클리어 번호
//  - RunInProgress : MainPanel에서 런을 시작했으면 true
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
    public bool IsEliteUnlocked     => _raw.ClearedNormal >= (StageConfig.Current?.EliteUnlockStage ?? 5);

    /// <summary>MainPanel에서 "게임 시작"을 눌러 런을 시작했으면 true.
    /// 환생(SetDefaults)으로 false로 초기화된다.</summary>
    public bool RunInProgress
    {
        get => _raw.RunInProgress;
        set => _raw.RunInProgress = value;
    }

    /// <summary>현재 런에서 진행 중인 스테이지 인덱스 (0-based).</summary>
    public int CurrentRunStage => _raw.CurrentRunStage;

    /// <summary>현재 스테이지의 타입.</summary>
    public RunStageType CurrentStageType => GetStageType(_raw.CurrentRunStage);

    public RunStageType GetStageType(int index)
    {
        if (_raw.RunSequence == null || index >= _raw.RunSequence.Length) return RunStageType.Normal;
        return (RunStageType)_raw.RunSequence[index];
    }

    public RunStageType[] GetRunSequence()
    {
        if (_raw.RunSequence == null || _raw.RunSequence.Length == 0)
            return System.Array.Empty<RunStageType>();
        return System.Array.ConvertAll(_raw.RunSequence, x => (RunStageType)x);
    }

    /// <summary>런 시작 시 시퀀스를 설정하고 진행 인덱스를 0으로 초기화.</summary>
    public void SetRunSequence(RunStageType[] seq)
    {
        _raw.RunSequence         = System.Array.ConvertAll(seq, s => (int)s);
        _raw.CurrentRunStage     = 0;
        // 새 시퀀스 = 새 런. 팝업 표시 기록도 같이 지운다 —
        // 남겨 두면 새 런의 첫 상점·이벤트가 "이미 봤다" 로 판정돼 건너뛴다.
        _raw.AutoPopupShownStage = -1;
    }

    /// <summary>
    /// 런 시퀀스를 보장한다. 없거나 현재 규칙에 어긋나면 새로 뽑는다.
    ///
    /// 시퀀스는 세이브에 남으므로, 생성 규칙을 바꿔도 옛 세이브는 그대로 남는다.
    /// (1스테이지가 상점으로 뜨던 원인 — 규칙 수정 전에 만들어진 시퀀스였다)
    /// 단 이미 진행 중인 런(CurrentRunStage > 0)은 건드리지 않는다 —
    /// SetRunSequence 가 진행 인덱스를 0으로 되돌리기 때문에 진행이 날아간다.
    /// </summary>
    /// <returns>새로 뽑았으면 true — 호출한 쪽이 저장 여부를 판단한다.</returns>
    public bool EnsureRunSequence()
    {
        var seq = GetRunSequence();
        if (seq.Length > 0 && (_raw.CurrentRunStage > 0 || RunSequenceGenerator.IsValid(seq)))
            return false;

        SetRunSequence(RunSequenceGenerator.Generate());
        return true;
    }

    /// <summary>다음 스테이지를 Elite 로 강제 변경 (갈림길의 첩자 — 풀어준다 선택).</summary>
    public void ForceNextStageElite()
    {
        if (_raw.RunSequence == null) return;
        int next = _raw.CurrentRunStage + 1;
        if (next < _raw.RunSequence.Length)
            _raw.RunSequence[next] = (int)RunStageType.Elite;
    }

    /// <summary>
    /// 상점·이벤트 팝업을 자동으로 띄운 스테이지 인덱스 (-1 = 아직 없음).
    /// 로비에 도착할 때마다 다시 뜨는 것을 막는다. 씬을 다시 로드해도 유지돼야 하므로
    /// 메모리 플래그가 아니라 세이브에 남긴다.
    /// </summary>
    public int AutoPopupShownStage
    {
        get => _raw.AutoPopupShownStage;
        set => _raw.AutoPopupShownStage = value;
    }

    /// <summary>스테이지가 클리어됐으면 true — 다음 상점 오픈 시 새 상품을 생성한다.</summary>
    public bool IsStageCleared
    {
        get => _raw.IsStageCleared;
        set => _raw.IsStageCleared = value;
    }

    /// <summary>스테이지 클리어 후 다음 스테이지로 진행.</summary>
    public void AdvanceRunStage()
    {
        _raw.CurrentRunStage++;
        _raw.IsStageCleared = true;
    }

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
        public int  ClearedNormal    = 0;
        public int  ClearedElite     = 0;
        public bool RunInProgress    = false;
        public int[] RunSequence     = System.Array.Empty<int>();
        public int   CurrentRunStage = 0;
        public bool  IsStageCleared  = true;  // 새 런 = 클리어 상태로 시작 → 첫 상점에서 새 상품 생성
        public int   AutoPopupShownStage = -1;
    }
}
