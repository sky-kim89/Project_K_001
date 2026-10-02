using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  AchievementData.cs
//  업적 달성 기록 세이브 섹션.
//
//  ■ 1회성 업적만 있다 — 진행도(카운터)를 저장하지 않는다
//    조건이 한 번 참이 되는 순간 해제하고 끝이다. 그래서 "해제한 ID 목록" 하나면 된다.
//    판정은 전부 AchievementTracker 가 한다 — 여기는 기록만 한다.
//
//  ⚠ 환생으로 초기화하지 않는다
//    UserDataManager.Reincarnate() 의 초기화 목록에 넣지 말 것 —
//    넣으면 환생할 때마다 업적이 전부 풀린다.
// ============================================================

[Serializable]
class AchievementDataJson
{
    public List<int> unlocked = new();
}

public class AchievementData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.Achievement;

    readonly HashSet<AchievementId> _unlocked = new();

    /// <summary>새 업적이 해제될 때 발행. 해제 알림·업적 화면이 구독한다.</summary>
    public static event Action<AchievementId> OnUnlocked;

    public bool IsUnlocked(AchievementId id) => _unlocked.Contains(id);

    public int UnlockedCount => _unlocked.Count;

    /// <summary>해제한다. 이미 해제된 업적이면 false.</summary>
    public bool Unlock(AchievementId id)
    {
        if (id == AchievementId.None || !_unlocked.Add(id)) return false;

        UserDataManager.Instance.RequestSave();
        Debug.Log($"[Achievement] 업적 달성 — {AchievementCatalog.Get(id).Name} ({id})");
        OnUnlocked?.Invoke(id);
        return true;
    }

    /// <summary>치트·디버그용 — 전부 미달성으로 되돌린다.</summary>
    public void ResetAll() => _unlocked.Clear();

    // ── ISaveSection ────────────────────────────────────────────

    public string Serialize()
    {
        var dto = new AchievementDataJson();
        foreach (var id in _unlocked) dto.unlocked.Add((int)id);
        return JsonUtility.ToJson(dto);
    }

    public void Deserialize(string json)
    {
        _unlocked.Clear();
        if (string.IsNullOrEmpty(json)) return;

        var dto = JsonUtility.FromJson<AchievementDataJson>(json);
        if (dto?.unlocked == null) return;

        // ⚠ enum 에서 사라진 번호는 버린다 — 나중에 그 번호를 다시 쓰면 "이미 달성" 으로 오인된다.
        foreach (int raw in dto.unlocked)
        {
            var id = (AchievementId)raw;
            if (id != AchievementId.None && Enum.IsDefined(typeof(AchievementId), id))
                _unlocked.Add(id);
        }
    }

    public void SetDefaults() => _unlocked.Clear();
}
