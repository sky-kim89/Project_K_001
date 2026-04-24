using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  UserDataManager.cs
//  데이터 저장 시스템의 중심 관리자. PureSingleton 기반.
//
//  주요 역할:
//  - ISaveSection 구현체를 등록·보관
//  - 게임 시작 시 전체 섹션 로드 (LoadAll)
//  - 변경 발생 시 1프레임 지연 일괄 저장 (RequestSave → SaveCoordinator)
//
//  사용법:
//    // 섹션 접근
//    UserData user = UserDataManager.Instance.Get<UserData>();
//
//    // 데이터 변경 후 저장 예약
//    user.AddGold(100);
//    UserDataManager.Instance.RequestSave();
//
//  새 섹션 추가 방법:
//  1. ISaveSection 구현 클래스 작성 (SaveKey 추가 포함)
//  2. OnInitialize() 안에서 RegisterSection(new YourSection()) 호출
// ============================================================

public class UserDataManager : PureSingleton<UserDataManager>
{
    // ── 섹션 저장소 ──────────────────────────────────────────

    readonly Dictionary<SaveKey, ISaveSection> _sections = new();

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// 타입으로 섹션을 가져온다.
    /// 등록되지 않은 타입이면 null 을 반환한다.
    /// </summary>
    public T Get<T>() where T : class, ISaveSection
    {
        foreach (ISaveSection section in _sections.Values)
        {
            if (section is T typed)
                return typed;
        }
        return null;
    }

    /// <summary>
    /// 저장 키로 섹션을 가져온다.
    /// </summary>
    public ISaveSection Get(SaveKey key)
    {
        _sections.TryGetValue(key, out ISaveSection section);
        return section;
    }

    /// <summary>
    /// 다음 프레임에 전체 섹션을 일괄 저장하도록 예약한다.
    /// 같은 프레임에 여러 번 호출해도 저장은 1회만 실행된다.
    /// </summary>
    public void RequestSave()
    {
        SaveCoordinator.Request(SaveAll);
    }

    /// <summary>전체 섹션을 즉시 저장한다. 일반적으로 RequestSave 를 사용할 것.</summary>
    public void SaveAll()
    {
        foreach (ISaveSection section in _sections.Values)
        {
            string json = section.Serialize();
            PlayerPrefs.SetString(GetPrefKey(section.SaveKey), json);
        }
        PlayerPrefs.Save();
        Debug.Log("[UserDataManager] 저장 완료");
    }

    /// <summary>전체 섹션의 데이터를 디스크에서 로드한다.</summary>
    public void LoadAll()
    {
        foreach (ISaveSection section in _sections.Values)
        {
            string prefKey = GetPrefKey(section.SaveKey);
            if (PlayerPrefs.HasKey(prefKey))
            {
                section.Deserialize(PlayerPrefs.GetString(prefKey));
            }
            else
            {
                section.SetDefaults();
            }
        }
        Debug.Log("[UserDataManager] 로드 완료");
    }

    /// <summary>특정 섹션만 저장한다.</summary>
    public void SaveSection(SaveKey key)
    {
        if (!_sections.TryGetValue(key, out ISaveSection section)) return;

        string json = section.Serialize();
        PlayerPrefs.SetString(GetPrefKey(key), json);
        PlayerPrefs.Save();
    }

    // ── 초기화 ───────────────────────────────────────────────

    protected override void OnInitialize()
    {
        RegisterSection(new UserData());
        RegisterSection(new UnitData());
        RegisterSection(new ItemData());
        RegisterSection(new StageProgressData());

        LoadAll();
    }

    // ── 내부 ─────────────────────────────────────────────────

    void RegisterSection(ISaveSection section)
    {
        if (_sections.ContainsKey(section.SaveKey))
        {
            Debug.LogWarning($"[UserDataManager] 중복 등록 시도: {section.SaveKey}");
            return;
        }
        _sections[section.SaveKey] = section;
    }

    static string GetPrefKey(SaveKey key) => $"Save_{(int)key}";
}
