using System.Collections.Generic;
using UnityEngine;
using BattleGame.Units;

// ============================================================
//  RelicTreeApplier.cs
//  유물 테크트리에서 찍은 노드를 스텟·시스템 보너스에 반영한다.
//  구 RelicApplier(RelicData SO + RelicInventoryData)를 대체한다.
//
//  ■ 부르는 쪽이 데이터를 들고 다니지 않는다
//    구 API 는 (inventory, db) 를 매번 넘겨받았고, 호출처 10곳이 각자
//    UserDataManager 에서 꺼내 오느라 같은 코드가 흩어져 있었다.
//    트리는 세이브가 하나뿐이라 여기서 직접 읽는다.
//
//  ■ 적용 지점
//    장수 공통  : HeroStatPipeline (ApplyToGeneralStat)
//    장수 전용  : HeroStatPipeline 맨 끝 (ApplyGeneralOnly)
//    병사       : SoldierStatApplier (CollectSoldier)
//    적 약화    : EnemyRuntimeBridge (ApplyEnemyWeaken)
//    시스템 값  : 장수 슬롯·배속·어빌리티 (GetSystemInt / GetSystemValue)
//
//  ⚠ 저장된 레벨을 그대로 믿지 않는다
//    밸런스로 MaxLevel 을 내리면 이미 그 위로 저장된 세이브가 초과 효과를 낸다.
//    LevelOf() 한 곳에서 상한을 다시 건다 — 구 RelicApplier 가 겪은 문제다.
// ============================================================

public static class RelicTreeApplier
{
    /// <summary>장수 전용 노드가 들어가는 레이어 — 병사 환산에서 걷힌다.</summary>
    public const string GeneralLayerKey = HeroStatPipeline.RelicKey + UnitStat.GeneralOnlySuffix;

    /// <summary>기본으로 열려 있는 장수 배치 슬롯 수.</summary>
    public const int BaseGeneralSlots = 2;

    /// <summary>장수 배치 슬롯 상한 — 트리·특성을 다 합쳐도 이 위로는 안 간다.</summary>
    public const int MaxGeneralSlots = 5;

    static RelicTreeData Data => UserDataManager.Instance?.Get<RelicTreeData>();

    /// <summary>세이브 레벨에 MaxLevel 상한을 건 값.</summary>
    static int LevelOf(RelicTreeData data, RelicNodeDef def)
        => Mathf.Min(data.GetLevel(def.Id), def.MaxLevel);

    // ══════════════════════════════════════════════════════════
    //  장수 스텟
    // ══════════════════════════════════════════════════════════

    public static void ApplyToGeneralStat(UnitStat stat, UnitJob job)
        => ApplyStatNodes(stat, job, generalOnly: false);

    /// <summary>
    /// 장수 전용(Unit_General) 노드만 적용한다.
    ///
    /// ⚠ 공통 출처가 전부 끝난 뒤에 부른다
    ///   먼저 붙이면 뒤에 오는 공통 % 옵션이 장수 전용으로 부풀려진 값을 기준으로
    ///   계산되고, 그 몫은 병사가 물려받는 층에 담긴다 —
    ///   결국 장수 전용 보너스의 일부가 병사에게 새어 들어간다.
    /// </summary>
    public static void ApplyGeneralOnly(UnitStat stat, UnitJob job)
    {
        ApplyStatNodes(stat, job, generalOnly: true);
        ApplyLoneWolf(stat);
    }

    static void ApplyStatNodes(UnitStat stat, UnitJob job, bool generalOnly)
    {
        var data = Data;
        if (stat == null || data == null) return;

        foreach (var def in RelicTreeCatalog.All)
        {
            if (def.IsSystem) continue;
            if (def.Target == AbilityTarget.Unit_Soldier) continue;
            if (!AbilityApplier.MatchesGeneralTarget(def.Target, job)) continue;

            bool isGeneralOnly = def.Target == AbilityTarget.Unit_General;
            if (isGeneralOnly != generalOnly) continue;

            int level = LevelOf(data, def);
            if (level <= 0) continue;

            string layer = isGeneralOnly ? GeneralLayerKey : HeroStatPipeline.RelicKey;
            foreach (var line in def.Stats)
            {
                float delta = line.Absolute
                    ? line.PerLevel * level
                    : stat.Get(line.Stat) * line.PerLevel * level;
                stat.Add(line.Stat, delta, layer);
            }
        }
    }

    /// <summary>
    /// 고립무원 — 트리로 깎아낸 병사 1명당 장수 공격력·체력이 오른다.
    ///
    /// ⚠ '현재 병사가 적을수록' 이 아니다
    ///   전투 중 병사가 죽었다고 장수가 강해지면 전멸 직전이 가장 센 판이 된다.
    ///   기준은 역분기 노드가 고정으로 깎은 수(SoldierCutTotal)다.
    /// </summary>
    static void ApplyLoneWolf(UnitStat stat)
    {
        float perSoldier = GetSystemValue(RelicSystemEffect.LoneWolfBonus);
        if (perSoldier <= 0f) return;

        int cut = RelicTreeCatalog.SoldierCutTotal(Data.Levels);
        if (cut <= 0) return;

        float ratio = perSoldier * cut;
        stat.Add(StatType.Attack, stat.Get(StatType.Attack) * ratio, GeneralLayerKey);
        stat.Add(StatType.MaxHp,  stat.Get(StatType.MaxHp)  * ratio, GeneralLayerKey);
    }

    // ══════════════════════════════════════════════════════════
    //  병사 스텟 (SoldierStatApplier 가 모아서 적용)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 병사 전용(Unit_Soldier) 노드를 비율·절대값 통에 담는다.
    ///
    /// ⚠ 공통(전체·직업) 노드를 여기 담지 말 것 — 이중 적용이다
    ///   공통은 이미 장수 스텟에 들어가 있고 병사는 그 값을 환산해 받는다.
    /// </summary>
    public static void CollectSoldier(UnitJob job,
                                      Dictionary<StatType, float> ratios,
                                      Dictionary<StatType, float> flats)
    {
        var data = Data;
        if (data == null) return;

        foreach (var def in RelicTreeCatalog.All)
        {
            if (def.IsSystem) continue;
            if (def.Target != AbilityTarget.Unit_Soldier) continue;

            int level = LevelOf(data, def);
            if (level <= 0) continue;

            foreach (var line in def.Stats)
            {
                var bucket = line.Absolute ? flats : ratios;
                float v = line.PerLevel * level;
                if (v == 0f) continue;
                bucket[line.Stat] = bucket.TryGetValue(line.Stat, out float cur) ? cur + v : v;
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  적 약화
    // ══════════════════════════════════════════════════════════

    /// <summary>EnemyRuntimeBridge.Initialize() 에서 SpawnEntity() 직전에 호출.</summary>
    public static void ApplyEnemyWeaken(UnitStat stat)
    {
        if (stat == null) return;

        float hpRatio  = GetSystemValue(RelicSystemEffect.EnemyMaxHpReduction);
        float atkRatio = GetSystemValue(RelicSystemEffect.EnemyAttackReduction);

        if (hpRatio > 0f)
            stat.Add(StatType.MaxHp,  -stat.Get(StatType.MaxHp)  * Mathf.Clamp01(hpRatio),  "relic_weaken");
        if (atkRatio > 0f)
            stat.Add(StatType.Attack, -stat.Get(StatType.Attack) * Mathf.Clamp01(atkRatio), "relic_weaken");
    }

    // ══════════════════════════════════════════════════════════
    //  시스템 값
    // ══════════════════════════════════════════════════════════

    /// <summary>같은 시스템 효과를 가진 노드의 합 (0.15 = 15%).</summary>
    public static float GetSystemValue(RelicSystemEffect effect)
    {
        var data = Data;
        if (data == null) return 0f;

        float total = 0f;
        foreach (var def in RelicTreeCatalog.All)
        {
            if (def.System != effect) continue;
            total += def.SystemPerLevel * LevelOf(data, def);
        }
        return total;
    }

    public static int GetSystemInt(RelicSystemEffect effect)
        => Mathf.RoundToInt(GetSystemValue(effect));

    /// <summary>트리만 계산한 장수 배치 슬롯 수 (기본 2칸 + 출병 명령).</summary>
    public static int GetActiveGeneralSlots()
        => BaseGeneralSlots + GetSystemInt(RelicSystemEffect.GeneralSlotBonus);

    /// <summary>
    /// 트리 + 특성 보너스를 합산한 최종 장수 슬롯 수.
    /// ⚠ 슬롯을 묻는 곳은 전부 여기 하나만 부를 것.
    /// </summary>
    public static int GetTotalActiveGeneralSlots()
    {
        var udm = UserDataManager.Instance;
        return Mathf.Min(
            GetActiveGeneralSlots()
            + TraitApplier.GetGeneralSlotBonus(udm?.Get<RunTraitData>(), TraitDatabase.Current),
            MaxGeneralSlots);
    }

    /// <summary>
    /// 전투 배속으로 쓸 수 있는 단계 수. 기본 1단계(1× 뿐) + 해금 노드.
    /// 시간의 고삐 → 2단계, 찰나의 지배까지 → 3단계.
    /// ⚠ 배속을 묻는 곳은 전부 여기를 거친다 — TopBarUI 가 직접 세면 트리가 무시된다.
    /// </summary>
    public static int GetBattleSpeedStepCount()
        => 1 + GetSystemInt(RelicSystemEffect.BattleSpeedUnlock);

    /// <summary>여정 시작 시 무작위로 받는 특성 수 (운명의 주사위).</summary>
    public static int GetRandomTraitCount()
        => GetSystemInt(RelicSystemEffect.RandomTraitOnStart);

    /// <summary>
    /// 여정 시작 시 얹어 주는 골드 (여정의 군자금).
    ///
    /// ⚠ 지급은 RunStarter.BeginRun 한 곳에서만 한다
    ///   ItemData.SetDefaults 에 넣으면 안 된다 — 그 자리는 환생 처리 도중이라,
    ///   방금 번 포인트로 이 노드를 찍어도 다음 여정이 아니라 그 다음에야 반영된다.
    /// </summary>
    public static int GetStartGoldBonus()
        => GetSystemInt(RelicSystemEffect.StartGoldBonus);

    /// <summary>
    /// 그 효과를 주는 노드 중 <b>아직 만렙이 아닌 첫 노드</b>의 이름.
    /// 잠긴 기능을 안내할 때 "무엇을 찍으면 되는지" 를 짚는 데 쓴다.
    ///
    /// ⚠ 이름을 문자열로 박아 두지 말 것
    ///   표에서 노드 이름을 고치면 안내만 옛 이름으로 남는다.
    ///   한 효과에 노드가 여럿인 경우(배속 2단계)도 여기서 걸러진다.
    /// </summary>
    public static string NextNodeNameFor(RelicSystemEffect effect)
    {
        var data = Data;
        if (data == null) return null;

        foreach (var def in RelicTreeCatalog.All)
        {
            if (def.System != effect) continue;
            if (LevelOf(data, def) >= def.MaxLevel) continue;
            return def.Name;
        }
        return null;
    }
}
