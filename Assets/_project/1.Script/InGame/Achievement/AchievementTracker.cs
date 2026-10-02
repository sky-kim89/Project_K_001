using BattleGame.Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ============================================================
//  AchievementTracker.cs
//  업적 판정 — 게임 곳곳의 "그 일이 일어난 자리" 가 여기 한 줄씩 부른다.
//
//  ■ 호출 지점 (새 판정을 넣으면 여기에도 적을 것)
//    OnStageCleared       InGameManager.RecordStageClear      여정 진행 · 편성 · 기적의 생환
//    OnDifficultyUnlocked InGameManager.TryUnlockNextDifficulty
//    OnReincarnated       UserDataManager.Reincarnate
//    OnUnitAcquired       UnitData.AddUnit                    희귀 장수 고용
//    OnGradeUp            UnitData.GradeUp                    전설의 장수
//    OnAbilityAcquired    RunAbilityData.AddAbility           직업 달인
//    OnCodexChanged       CodexData.OnCodexChanged 구독        수집가 · 만물 도감
//    OnRelicLevelUp       RelicTreeData.LevelUp               뿌리 내림 · 유물의 정점
//    EvaluateAll          LobbyManager.Start                  업적 추가 전 세이브 소급
//
//  ■ 판정은 조건이 참인 순간 Unlock 한 번이다 — 누적 카운터가 없다.
//    이미 해제된 업적은 AchievementData.Unlock 이 걸러 준다.
// ============================================================

public static class AchievementTracker
{
    const int FullFormation = 5;   // 편성 업적의 "5명 모두"
    const int HurdleStage   = 5;
    const int HalfWayStage  = 15;

    // 최종 스테이지 — 무한 보스가 나오는 칸. 승리가 없어서 무한 보스 진입이 곧 클리어다.
    static int FinalStage => GameplayConfig.Current.MaxStage;

    static AchievementData Data => UserDataManager.Instance.Get<AchievementData>();

    static void Unlock(AchievementId id) => Data.Unlock(id);

    // ── 여정 진행 · 전투 · 편성 ─────────────────────────────

    /// <summary>
    /// 전투 스테이지를 클리어했다. clearedStage = 방금 깬 스테이지 번호(1~30).
    ///
    /// ⚠ 승리 직후(결과 팝업 전)에 부를 것 — 병사 생존 수를 ECS 에서 바로 센다.
    ///   로비로 돌아가 유닛이 회수된 뒤에는 셀 것이 없다.
    /// </summary>
    public static void OnStageCleared(int clearedStage)
    {
        if (clearedStage >= 1)            Unlock(AchievementId.FirstStage);
        if (clearedStage >= HurdleStage)  Unlock(AchievementId.FirstHurdle);
        if (clearedStage >= HalfWayStage) Unlock(AchievementId.HalfWay);

        if (clearedStage >= FinalStage)
        {
            Unlock(AchievementId.JourneyEnd);

            var tier = UserDataManager.Instance.Get<DifficultyData>().SelectedTier;
            if (tier == DifficultyTier.Hell)    Unlock(AchievementId.HellClear);
            if (tier == DifficultyTier.Inferno) Unlock(AchievementId.InfernoClear);
        }

        CheckFormation();

        if (CountAlive<SoldierComponent>() == 1) Unlock(AchievementId.MiracleSurvivor);

        // ⚠ 1명만 편성했으면 "살아남은 1명" 이 공짜로 채워진다 — 2명 이상 나갔을 때만 센다
        if (UserDataManager.Instance.Get<DeploymentData>().GetDeployedUnits().Count >= 2 &&
            CountAlive<GeneralComponent>() == 1)
            Unlock(AchievementId.LastGeneral);
    }

    /// <summary>편성 업적 — 직업 통일 5인 · 병사 0명 장수.</summary>
    static void CheckFormation()
    {
        var units    = UserDataManager.Instance.Get<UnitData>();
        var deployed = UserDataManager.Instance.Get<DeploymentData>().GetDeployedUnits();

        bool     sameJob  = deployed.Count == FullFormation;
        UnitJob? firstJob = null;
        bool     loneWolf = false;

        foreach (string name in deployed)
        {
            UnitJob job = UnitJobRoller.GetJob(name);
            firstJob ??= job;
            if (job != firstJob) sameJob = false;

            // 병사 수는 로비 표시·전투 스폰과 같은 진입점에서 읽는다 (HeroStatResolver)
            float soldiers = HeroStatResolver.Resolve(units.GetUnit(name)).Total(StatType.SoldierCount);
            if (Mathf.RoundToInt(soldiers) <= 0) loneWolf = true;
        }

        if (sameJob && firstJob.HasValue)
            Unlock((AchievementId)((int)AchievementId.KnightOrder + (int)firstJob.Value));

        if (loneWolf) Unlock(AchievementId.LoneWolf);
    }

    /// <summary>
    /// 지금 살아 있는 아군 수 — T 로 병사(SoldierComponent)/장수(GeneralComponent)를 고른다.
    /// 소환물(스켈레톤 등)은 세지 않는다.
    /// </summary>
    static int CountAlive<T>() where T : unmanaged, IComponentData
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<T>(),
            ComponentType.ReadOnly<UnitIdentityComponent>(),
            ComponentType.ReadOnly<HealthComponent>(),
            ComponentType.Exclude<DeadTag>(),
            ComponentType.Exclude<SummonedTag>());

        using var ids    = query.ToComponentDataArray<UnitIdentityComponent>(Allocator.Temp);
        using var health = query.ToComponentDataArray<HealthComponent>(Allocator.Temp);
        query.Dispose();

        int alive = 0;
        for (int i = 0; i < ids.Length; i++)
            if (ids[i].Team == TeamType.Ally && health[i].CurrentHp > 0f) alive++;
        return alive;
    }

    public static void OnDifficultyUnlocked() => Unlock(AchievementId.NewTrial);

    public static void OnReincarnated() => Unlock(AchievementId.FirstReincarnation);

    // ── 장수 ────────────────────────────────────────────────

    /// <summary>장수를 얻었다 (고용·상점·이벤트·시작 선택 전부 UnitData.AddUnit 을 지난다).</summary>
    public static void OnUnitAcquired(string unitName)
    {
        var db = ActiveSkillDatabase.Current;
        foreach (var skill in AchievementCatalog.RareSkillIds)
            if (RareSkillArbiter.OwnerOf(skill, db) == unitName)
                Unlock(AchievementCatalog.RareHeroOf(skill));
    }

    public static void OnGradeUp(UnitEntry entry)
    {
        if (entry.Grade == UnitGrade.Epic) Unlock(AchievementId.LegendaryGeneral);
    }

    // ── 어빌리티 ────────────────────────────────────────────

    public static void OnAbilityAcquired(AbilityId id)
    {
        switch (id)
        {
            case AbilityId.D01: Unlock(AchievementId.KnightMaster); break;
            case AbilityId.D02: Unlock(AchievementId.ArcherMaster); break;
            case AbilityId.D03: Unlock(AchievementId.MageMaster);   break;
            case AbilityId.D04: Unlock(AchievementId.ShieldMaster); break;
        }
    }

    // ── 도감 · 유물 ─────────────────────────────────────────

    public static void OnCodexChanged()
    {
        bool any = false, all = true;
        foreach (CodexCategory c in System.Enum.GetValues(typeof(CodexCategory)))
        {
            var (owned, total) = CodexCatalog.Progress(c);

            // ⚠ total 0 은 "완성" 이 아니다 — DB 가 아직 안 올라온 것이다
            bool complete = total > 0 && owned >= total;
            any |= complete;
            all &= complete;
        }

        if (any) Unlock(AchievementId.Collector);
        if (all) Unlock(AchievementId.CodexComplete);
    }

    public static void OnRelicLevelUp()
    {
        var tree = UserDataManager.Instance.Get<RelicTreeData>();
        if (tree.TakenCount > 0) Unlock(AchievementId.RelicRoot);

        foreach (var def in RelicTreeCatalog.All)
            if (tree.GetLevel(def.Id) < def.MaxLevel) return;
        Unlock(AchievementId.RelicComplete);
    }

    // ── 소급 판정 ───────────────────────────────────────────

    static bool _subscribed;

    /// <summary>
    /// 이미 조건을 채운 세이브를 소급해 해제하고, 도감 변경을 구독한다.
    /// 업적 시스템이 생기기 전의 세이브(환생·유물·도감을 이미 해 둔)를 위해 로비 시작 때 한 번 부른다.
    ///
    /// ⚠ 여정 진행·편성 업적은 소급하지 않는다 — "그 전투를 이겼다" 는 기록이 세이브에 없다.
    /// </summary>
    public static void EvaluateAll()
    {
        if (!_subscribed)
        {
            CodexData.OnCodexChanged += OnCodexChanged;
            _subscribed = true;
        }

        var udm = UserDataManager.Instance;
        if (udm.Get<ReincarnationData>().TotalCount > 0)       OnReincarnated();
        if (udm.Get<DifficultyData>().ClearedTierIndex >= 0)  OnDifficultyUnlocked();

        foreach (var unit in udm.Get<UnitData>().Units)
        {
            OnUnitAcquired(unit.UnitName);
            if (unit.GradeUpCount > 0) OnGradeUp(unit);
        }

        foreach (var id in udm.Get<RunAbilityData>().OwnedAbilityIds)
            OnAbilityAcquired(id);

        OnCodexChanged();
        OnRelicLevelUp();
    }
}
