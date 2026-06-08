using UnityEngine;
using UnityEditor;

// ============================================================
//  TraitCreator.cs  [Editor Only]
//  직업별 초기 특성 SO 4종 + TraitDatabase 자동 생성 도구.
//
//  사용법:
//    Unity 메뉴 → BattleGame → 데이터 생성 → 특성 전체 생성
//
//  생성 위치:
//    Assets/_project/Data/Traits/  ← 개별 TraitData SO
//    Assets/Resources/TraitDatabase.asset  ← 데이터베이스
// ============================================================

public static class TraitCreator
{
    const string TraitDir = "Assets/_project/Data/Traits";
    const string DBPath   = "Assets/Resources/TraitDatabase.asset";

    [MenuItem("BattleGame/데이터 생성/특성 전체 생성")]
    public static void CreateAllTraits()
    {
        EnsureFolder();

        var defs = BuildDefinitions();
        var assets = new TraitData[defs.Length];

        for (int i = 0; i < defs.Length; i++)
        {
            var def  = defs[i];
            string path = $"{TraitDir}/Trait_{def.type}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<TraitData>(path);
            if (existing != null)
            {
                ApplyDefinition(existing, def);
                EditorUtility.SetDirty(existing);
                assets[i] = existing;
            }
            else
            {
                var so = ScriptableObject.CreateInstance<TraitData>();
                ApplyDefinition(so, def);
                AssetDatabase.CreateAsset(so, path);
                assets[i] = so;
            }
        }

        // ── TraitDatabase 생성/갱신 ───────────────────────────
        var db = AssetDatabase.LoadAssetAtPath<TraitDatabase>(DBPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<TraitDatabase>();
            AssetDatabase.CreateAsset(db, DBPath);
        }

        var dbSO    = new SerializedObject(db);
        var arrProp = dbSO.FindProperty("_traits");
        arrProp.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++)
            arrProp.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        dbSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = db;
        EditorGUIUtility.PingObject(db);
        Debug.Log($"[TraitCreator] 특성 {assets.Length}종 + TraitDatabase 생성 완료.");
    }

    // ── 정의 ─────────────────────────────────────────────────

    struct Def
    {
        public TraitType type;
        public string    name;
        public string    desc;
        public UnitJob   job;
        public (StatType stat, float value, bool isPct)[] fx;
    }

    static Def[] BuildDefinitions() => new Def[]
    {
        new Def
        {
            type = TraitType.KnightCommand,
            name = "지휘관의 기질",
            desc = "전장을 호령하는 기사의 타고난 통솔력. 체력과 병사 지휘력이 강화된다.",
            job  = UnitJob.Knight,
            fx   = new[]
            {
                (StatType.MaxHp,       0.10f, true ),   // 체력 +10%
                (StatType.SoldierCount, 2f,   false),   // 병사 수 +2
                (StatType.CommandPower, 2f,   false),   // 지휘력 +2
            },
        },
        new Def
        {
            type = TraitType.ArcherPrecision,
            name = "정밀 사수",
            desc = "숨을 죽이고 조준하는 궁수의 집중력. 공격력·사거리·공격속도가 강화된다.",
            job  = UnitJob.Archer,
            fx   = new[]
            {
                (StatType.Attack,      0.15f, true ),   // 공격력 +15%
                (StatType.AttackRange,  1f,   false),   // 사거리 +1
                (StatType.AttackSpeed,  0.15f, false),  // 공격속도 +0.15
            },
        },
        new Def
        {
            type = TraitType.MageArcane,
            name = "마력 집중",
            desc = "내면의 마력을 극한까지 끌어올린 마법사의 각성. 공격력과 스킬 쿨다운이 강화된다.",
            job  = UnitJob.Mage,
            fx   = new[]
            {
                (StatType.Attack,             0.20f, true ),   // 공격력 +20%
                (StatType.SkillCooldownReduce, 0.10f, false),  // 쿨다운 감소 +10%
            },
        },
        new Def
        {
            type = TraitType.ShieldFortress,
            name = "강철 요새",
            desc = "흔들리지 않는 방패병의 육체. 체력과 방어율이 대폭 강화된다.",
            job  = UnitJob.ShieldBearer,
            fx   = new[]
            {
                (StatType.MaxHp,  0.20f, true ),   // 체력 +20%
                (StatType.Defense, 0.08f, false),  // 방어율 +8%
            },
        },
    };

    // ── 헬퍼 ──────────────────────────────────────────────────

    static readonly (TraitType type, string iconPath)[] IconPaths = new[]
    {
        (TraitType.KnightCommand,   "Assets/_project/3.Textures/Icons/Traits/trait_knight_command.png"),
        (TraitType.ArcherPrecision, "Assets/_project/3.Textures/Icons/Traits/trait_archer_precision.png"),
        (TraitType.MageArcane,      "Assets/_project/3.Textures/Icons/Traits/trait_mage_arcane.png"),
        (TraitType.ShieldFortress,  "Assets/_project/3.Textures/Icons/Traits/trait_shield_fortress.png"),
    };

    static void ApplyDefinition(TraitData so, Def def)
    {
        so.TraitType    = def.type;
        so.TraitName    = def.name;
        so.Description  = def.desc;
        so.RequiredJob  = def.job;

        // 아이콘 연결 (Generate Trait Icons 이후에 실행하면 자동 링크)
        foreach (var (t, path) in IconPaths)
        {
            if (t != def.type) continue;
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) so.Icon = sp;
            break;
        }

        so.Effects = new TraitData.TraitStatEntry[def.fx.Length];
        for (int i = 0; i < def.fx.Length; i++)
        {
            so.Effects[i] = new TraitData.TraitStatEntry
            {
                Stat      = def.fx[i].stat,
                Value     = def.fx[i].value,
                IsPercent = def.fx[i].isPct,
            };
        }
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_project/Data"))
            AssetDatabase.CreateFolder("Assets/_project", "Data");
        if (!AssetDatabase.IsValidFolder(TraitDir))
            AssetDatabase.CreateFolder("Assets/_project/Data", "Traits");
    }
}
