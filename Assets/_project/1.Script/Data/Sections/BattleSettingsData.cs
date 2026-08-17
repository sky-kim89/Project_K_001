using System;
using UnityEngine;

// ============================================================
//  BattleSettingsData.cs
//  전투 조작 설정 저장 섹션.
//
//  AutoSkill  : 액티브 스킬 자동 사용 여부 (상단바 AUTO 토글).
//               꺼져 있으면 장수 카드를 눌러야만 스킬이 나간다.
//  SpeedIndex : 배속 토글 단계 인덱스 (TopBarUI.SpeedSteps 의 인덱스).
//
//  ⚠ 환생으로 초기화하지 않는다
//    런 데이터가 아니라 조작 취향이다. UserDataManager.Reincarnate() 목록에
//    넣으면 환생할 때마다 배속과 자동 설정이 풀린다.
// ============================================================

public class BattleSettingsData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.BattleSettings;

    RawData _raw = new();

    /// <summary>
    /// 자동 스킬 사용 여부의 정적 사본.
    ///
    /// ActiveSkillAISystem(ECS) 이 매 프레임 읽는 값이다.
    /// 그때마다 UserDataManager 의 섹션 딕셔너리를 훑지 않도록 여기에 복사해 둔다.
    /// 쓰기는 이 클래스 안에서만 한다 — 저장값과 어긋나지 않게 하려는 것이다.
    /// </summary>
    public static bool AutoSkillEnabled { get; private set; }

    // ── 읽기 ────────────────────────────────────────────────────

    public bool AutoSkill  => _raw.AutoSkill;
    public int  SpeedIndex => _raw.SpeedIndex;

    // ── 쓰기 ────────────────────────────────────────────────────

    public void SetAutoSkill(bool on)
    {
        _raw.AutoSkill   = on;
        AutoSkillEnabled = on;
    }

    public void SetSpeedIndex(int index) => _raw.SpeedIndex = index;

    // ── ISaveSection ────────────────────────────────────────────

    public string Serialize() => JsonUtility.ToJson(_raw);

    public void Deserialize(string json)
    {
        _raw = JsonUtility.FromJson<RawData>(json) ?? new RawData();
        AutoSkillEnabled = _raw.AutoSkill;
    }

    public void SetDefaults()
    {
        _raw = new RawData();
        AutoSkillEnabled = _raw.AutoSkill;
    }

    [Serializable]
    class RawData
    {
        public bool AutoSkill  = false;   // 기본은 수동 — 장수 카드를 눌러 쓴다
        public int  SpeedIndex = 0;       // 1×
    }
}
