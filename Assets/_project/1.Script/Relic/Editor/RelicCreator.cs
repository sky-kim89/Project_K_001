using UnityEditor;
using UnityEngine;

// ============================================================
//  RelicCreator.cs
//  Tools > Project K > 데이터 생성 > 유물 전체 생성
//
//  실행하면:
//    1. Assets/Resources/Relics/ 에 RelicData SO 30종 생성
//    2. Assets/Resources/RelicDatabase.asset 생성 후 전체 등록
//
//  비용 공식: RelicData.LevelUpCost(현재레벨) = (N+1)² × 희귀도 배율
//    희귀도 배율은 GameplayConfig.RelicRarityCostMultipliers 에 있다.
//    같은 희귀도에서 더 나눠야 하면 RelicData.CostWeight 로 조정한다.
//  이펙트 IsAbsoluteValue:
//    - 대형 스탯 (MaxHp/Attack/Speed/Range) → false (기저값 대비 %)
//    - 소형 스탯 (SoldierCount/CritChance/Defense/SkillCooldownReduce) → true (절대값 가산)
// ============================================================

public static class RelicCreator
{
    const string DbPath    = "Assets/Resources/RelicDatabase.asset";
    const string RelicDir  = "Assets/Resources/Relics";

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Data + "유물", priority = ProjectKMenu.DataPrio + 15)]
    public static void CreateAll()
    {
        EnsureFolder();
        CreateSystemRelics();
        CreateGeneralStatRelics();
        CreateClassRelics();
        CreateSoldierRelics();
        RebuildDatabase();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RelicCreator] 유물 전체 생성 완료");
    }

    [MenuItem(ProjectKMenu.Data + "RelicDatabase 재구성", priority = ProjectKMenu.DataPrio + 16)]
    public static void RebuildDatabase()
    {
        var db = AssetDatabase.LoadAssetAtPath<RelicDatabase>(DbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<RelicDatabase>();
            AssetDatabase.CreateAsset(db, DbPath);
        }

        var guids = AssetDatabase.FindAssets("t:RelicData", new[] { RelicDir });
        var so    = new SerializedObject(db);
        var prop  = so.FindProperty("_relics");
        prop.arraySize = guids.Length;

        for (int i = 0; i < guids.Length; i++)
        {
            var path   = AssetDatabase.GUIDToAssetPath(guids[i]);
            var relic  = AssetDatabase.LoadAssetAtPath<RelicData>(path);
            prop.GetArrayElementAtIndex(i).objectReferenceValue = relic;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
        Debug.Log($"[RelicCreator] RelicDatabase 재구성 완료 ({guids.Length}개)");
    }

    // ── 시스템 유물 (1xx) ─────────────────────────────────────

    static void CreateSystemRelics()
    {
        // 어빌리티 관련
        MakeSys(RelicId.R_AbilityRefresh,    "어빌리티 재편성",  RelicRarity.Rare,      3,
            RelicSystemEffect.AbilityRefreshCount,   1f,   true);  // +1회/레벨
        MakeSys(RelicId.R_AbilityChoicePlus, "선택의 축복",      RelicRarity.Epic,      2,
            RelicSystemEffect.AbilityChoiceCount,    1f,   true);  // +1개/레벨
        MakeSys(RelicId.R_AbilityAdvanced,   "고급 비전",        RelicRarity.Rare,      3,
            RelicSystemEffect.AbilityAdvancedChance, 0.10f, false); // +10%p/레벨

        // 보상 증가
        MakeSys(RelicId.R_GoldBonus,  "황금의 가호",  RelicRarity.Common,   5,
            RelicSystemEffect.GoldGainBonus,        0.15f, false);
        MakeSys(RelicId.R_SoulBonus,  "전사의 유산",  RelicRarity.Common,   5,
            RelicSystemEffect.SoldierSoulGainBonus, 0.15f, false);
        MakeSys(RelicId.R_ExpBonus,   "성장의 증표",  RelicRarity.Uncommon, 5,
            RelicSystemEffect.ExpGainBonus,         0.10f, false);

        // 적 약화
        MakeSys(RelicId.R_EnemyHpDown,  "시련의 세례", RelicRarity.Rare, 4,
            RelicSystemEffect.EnemyMaxHpReduction,  0.05f, false);
        MakeSys(RelicId.R_EnemyAtkDown, "공포의 각인", RelicRarity.Rare, 4,
            RelicSystemEffect.EnemyAttackReduction, 0.04f, false);

        // 장수 배치 슬롯
        MakeSys(RelicId.R_GeneralSlotExpand, "출병 명령", RelicRarity.Epic, 2,
            RelicSystemEffect.GeneralSlotBonus, 1f, true);  // Lv1=+1칸, Lv2=+2칸

        // 전투 배속 해금 — 기본은 1× 뿐이다. 이 유물이 2×·3× 를 연다.
        MakeSys(RelicId.R_BattleSpeed, "시간의 고삐", RelicRarity.Epic, 2,
            RelicSystemEffect.BattleSpeedUnlock, 1f, true);  // Lv1=2×, Lv2=3×
    }

    // ── 장수 스탯 유물 (2xx) ─────────────────────────────────

    static void CreateGeneralStatRelics()
    {
        // 전체 장수 (All)
        MakeStat1(RelicId.R_GeneralHp,           "장군의 심장",   RelicRarity.Common,  5,
            AbilityTarget.All, StatType.MaxHp,               0.05f, false);
        MakeStat1(RelicId.R_GeneralAtk,          "전투의 기억",   RelicRarity.Common,  5,
            AbilityTarget.All, StatType.Attack,               0.04f, false);
        MakeStat1(RelicId.R_GeneralDef,          "철의 의지",     RelicRarity.Common,  5,
            AbilityTarget.All, StatType.Defense,              0.03f, true);  // +3%p
        MakeStat1(RelicId.R_GeneralSpd,          "질풍의 발걸음", RelicRarity.Common,  5,
            AbilityTarget.All, StatType.MoveSpeed,            0.04f, false);
        MakeStat1(RelicId.R_GeneralRange,        "사냥꾼의 눈",   RelicRarity.Common,  5,
            AbilityTarget.All, StatType.AttackRange,          0.04f, false);
        MakeStat1(RelicId.R_GeneralCrit,         "비수의 날",     RelicRarity.Rare,    5,
            AbilityTarget.All, StatType.CritChance,           0.03f, true);  // +3%p
        MakeStat1(RelicId.R_GeneralCooldown,     "시간의 압축",   RelicRarity.Rare,    5,
            AbilityTarget.All, StatType.SkillCooldownReduce,  0.03f, true);  // +3%p
        MakeStat1(RelicId.R_GeneralSoldierCount, "지휘의 깃발",   RelicRarity.Rare,    5,
            AbilityTarget.All, StatType.SoldierCount,         1f,    true);  // +1명
    }

    // ── 직업 특화 장수 유물 (2xx, 211~214) ────────────────────

    static void CreateClassRelics()
    {
        // Knight — MaxHp + Defense (IsAbsolute=false: 비율 적용)
        MakeStat2(RelicId.R_KnightArmor,    "기사왕의 갑주", RelicRarity.Rare, 3,
            AbilityTarget.Job_Knight,
            StatType.MaxHp,    0.08f,
            StatType.Defense,  0.05f,
            false);

        // Archer — AttackSpeed + AttackRange
        MakeStat2(RelicId.R_ArcherSwift,    "활의 정령",     RelicRarity.Rare, 3,
            AbilityTarget.Job_Archer,
            StatType.AttackSpeed, 0.08f,
            StatType.AttackRange, 0.06f,
            false);

        // Mage — Attack + SkillCooldownReduce (IsAbsolute=true: 둘 다 절대값)
        MakeStat2(RelicId.R_MagePower,      "마력의 결정",   RelicRarity.Epic, 3,
            AbilityTarget.Job_Mage,
            StatType.Attack,              0.10f,
            StatType.SkillCooldownReduce, 0.06f,
            false);  // Attack은 비율이 의미있으므로 false

        // ShieldBearer — Defense + MaxHp
        MakeStat2(RelicId.R_ShieldFortress, "방벽의 군주",   RelicRarity.Epic, 3,
            AbilityTarget.Job_ShieldBearer,
            StatType.Defense, 0.10f,
            StatType.MaxHp,   0.08f,
            false);
    }

    // ── 병사 스탯 유물 (3xx) ─────────────────────────────────

    static void CreateSoldierRelics()
    {
        MakeStat1(RelicId.R_SoldierAtk, "병사의 혼",    RelicRarity.Common, 5,
            AbilityTarget.Unit_Soldier, StatType.Attack, 0.05f, false);
        MakeStat1(RelicId.R_SoldierHp,  "병사의 생명력", RelicRarity.Common, 5,
            AbilityTarget.Unit_Soldier, StatType.MaxHp,  0.05f, false);
    }

    // ── SO 생성 헬퍼 ─────────────────────────────────────────

    static void MakeSys(RelicId id, string name, RelicRarity rarity, int maxLevel,
        RelicSystemEffect effect, float valuePerLevel, bool isAbsolute)
    {
        var r = GetOrCreate(id);
        r.Id                  = id;
        r.RelicName           = name;
        r.Rarity              = rarity;
        r.MaxLevel            = maxLevel;
        r.EffectType          = RelicEffectType.System;
        r.SystemEffect        = effect;
        r.SystemValuePerLevel = valuePerLevel;
        r.IsAbsoluteValue     = isAbsolute;
        EditorUtility.SetDirty(r);
    }

    static void MakeStat1(RelicId id, string name, RelicRarity rarity, int maxLevel,
        AbilityTarget target, StatType stat, float valuePerLevel, bool isAbsolute)
    {
        var r = GetOrCreate(id);
        r.Id              = id;
        r.RelicName       = name;
        r.Rarity          = rarity;
        r.MaxLevel        = maxLevel;
        r.EffectType      = RelicEffectType.Stat;
        r.Target          = target;
        r.Stat1           = stat;
        r.Value1PerLevel  = valuePerLevel;
        r.HasStat2        = false;
        r.IsAbsoluteValue = isAbsolute;
        EditorUtility.SetDirty(r);
    }

    static void MakeStat2(RelicId id, string name, RelicRarity rarity, int maxLevel,
        AbilityTarget target,
        StatType stat1, float val1,
        StatType stat2, float val2,
        bool isAbsolute)
    {
        var r = GetOrCreate(id);
        r.Id              = id;
        r.RelicName       = name;
        r.Rarity          = rarity;
        r.MaxLevel        = maxLevel;
        r.EffectType      = RelicEffectType.Stat;
        r.Target          = target;
        r.Stat1           = stat1;
        r.Value1PerLevel  = val1;
        r.HasStat2        = true;
        r.Stat2           = stat2;
        r.Value2PerLevel  = val2;
        r.IsAbsoluteValue = isAbsolute;
        EditorUtility.SetDirty(r);
    }

    static RelicData GetOrCreate(RelicId id)
    {
        string path = $"{RelicDir}/Relic_{(int)id}.asset";
        var    data = AssetDatabase.LoadAssetAtPath<RelicData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<RelicData>();
            AssetDatabase.CreateAsset(data, path);
        }
        return data;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(RelicDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Relics");
    }
}
