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

    // ── 최초 실행 판별 ───────────────────────────────────────

    // ⚠ "세이브가 하나도 없었다" 가 유일하게 믿을 수 있는 신호다
    //   장수 수·클리어 수로 판단하면 환생 직후와 구분이 안 된다 —
    //   환생하면 장수도 스테이지도 0 으로 돌아가지만 그때는 장수를 골라야 한다.
    //   UserData 는 환생해도 남으므로 이 키의 유무가 설치 직후인지를 가른다.
    bool _firstLaunch;

    /// <summary>
    /// 설치 후 첫 실행이면 true 를 한 번만 돌려주고 플래그를 내린다.
    /// ⚠ 반드시 소비형이어야 한다 — 전투를 마치고 로비 씬을 다시 로드해도
    ///   매니저는 살아 있어서, 플래그가 남아 있으면 자동 진입이 무한히 반복된다.
    /// </summary>
    public bool ConsumeFirstLaunch()
    {
        bool was = _firstLaunch;
        _firstLaunch = false;
        return was;
    }

    /// <summary>전체 섹션의 데이터를 디스크에서 로드한다.</summary>
    public void LoadAll()
    {
        _firstLaunch = !PlayerPrefs.HasKey(GetPrefKey(SaveKey.UserData));

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

    // ── 환생 ─────────────────────────────────────────────────

    /// <summary>
    /// 환생: 환생 포인트 적립 후 유물·환생 데이터·유저 정보를 제외한
    /// 모든 런 데이터(장수·장비·어빌리티·재화·스테이지)를 초기화.
    /// </summary>
    public void Reincarnate()
    {
        var stageData = Get<StageProgressData>();
        var reincData = Get<ReincarnationData>();

        int cleared = stageData?.ClearedNormalStages ?? 0;

        // 포인트 적립 (초기화 전에 먼저 계산)
        reincData?.EarnPointsByStage(cleared);
        reincData?.ResetOnReincarnation();

        // 런 데이터 전체 초기화 (UserData·RelicInventory·ReincarnationData·Codex 제외)
        // ⚠ CodexData 를 여기에 넣지 말 것 — 도감은 "지금까지 만나 본 것" 의 영구 기록이다.
        //   회귀로 지워지면 도감 버프가 매 런 0 에서 다시 시작해 존재 의미가 사라진다.
        Get<UnitData>()?.SetDefaults();
        Get<ItemData>()?.SetDefaults();
        Get<StageProgressData>()?.SetDefaults();
        Get<EquipInventoryData>()?.SetDefaults();
        Get<DeploymentData>()?.SetDefaults();
        Get<RunAbilityData>()?.SetDefaults();
        Get<RunTraitData>()?.SetDefaults();
        Get<RunShopData>()?.SetDefaults();
        Get<RunEventBonusData>()?.SetDefaults();

        // 도감은 지우지 않되 '이번 여정 고정치' 잠금만 푼다 —
        // 다음 여정을 시작(RunStarter.BeginRun)할 때 이번 회차에 모은 것까지 얹혀 잠긴다.
        Get<CodexData>()?.UnlockForNextRun();

        // 이번이 몇 번째 환생인지 — 첫 환생 직후에만 뜨는 안내가 이 값을 본다.
        reincData?.CountReincarnation();

        // 초기 장수 자동 배치
        AutoDeployFirstHeroIfNeeded();

        SaveAll();

        Debug.Log($"[UserDataManager] 환생 완료 — +{ReincarnationData.CalculateReincarnationPoints(cleared)}pt, 누적 {reincData?.ReincarnationPoints}pt");
    }

    // ── 초기화 ───────────────────────────────────────────────

    protected override void OnInitialize()
    {
        RegisterSection(new UserData());
        RegisterSection(new UnitData());
        RegisterSection(new ItemData());
        RegisterSection(new StageProgressData());
        RegisterSection(new EquipInventoryData());
        RegisterSection(new DeploymentData());
        RegisterSection(new RunAbilityData());
        RegisterSection(new RunTraitData());
        RegisterSection(new RunShopData());
        RegisterSection(new RunEventBonusData());
        RegisterSection(new RelicInventoryData());
        RegisterSection(new ReincarnationData());
        RegisterSection(new CodexData());
        RegisterSection(new BattleSettingsData());
        RegisterSection(new DifficultyData());
        // ⚠ Reincarnate() 의 초기화 목록에 넣지 말 것 — 환생마다 튜토리얼이 다시 뜬다.
        RegisterSection(new TutorialData());

        LoadAll();
        AutoDeployFirstHeroIfNeeded();
    }

    void AutoDeployFirstHeroIfNeeded()
    {
        var unitData   = Get<UnitData>();
        var deployData = Get<DeploymentData>();
        if (unitData == null || deployData == null) return;
        if (deployData.HasAnyDeployed()) return;
        if (unitData.Units.Count == 0) return;

        deployData.Deploy(unitData.Units[0].UnitName, 0);
        RequestSave();
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
