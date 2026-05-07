#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// ============================================================
//  AbilityCreator.cs
//  일반(15종) + 고급(12종) 어빌리티 SO 자산을 자동 생성하는 에디터 툴.
//
//  메뉴: Tools > Project K > 어빌리티 > Create Ability Assets
//  출력:
//    Assets/Resources/Abilities/Ability_{id}.asset  ×27
//    Assets/Resources/AbilityDatabase.asset
// ============================================================

public static class AbilityCreator
{
    const string SaveDir      = "Assets/Resources/Abilities";
    const string DatabasePath = "Assets/Resources/AbilityDatabase.asset";

    struct Def
    {
        public AbilityId     Id;
        public string        Name;
        public AbilityGrade  Grade;
        public AbilityTarget Target;
        public StatType      Stat1;
        public float         Value1;
        public bool          HasStat2;
        public StatType      Stat2;
        public float         Value2;
    }

    static readonly Def[] Defs =
    {
        // ── 일반 (Normal) ─────────────────────────────────────
        new Def { Id=AbilityId.A01, Name="강인한 체력",   Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.MaxHp,    Value1=0.08f },
        new Def { Id=AbilityId.A02, Name="예리한 검격",   Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.Attack,   Value1=0.08f },
        new Def { Id=AbilityId.A03, Name="신속한 연격",   Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.AttackSpeed, Value1=0.08f },
        new Def { Id=AbilityId.A04, Name="민첩한 기동",   Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.MoveSpeed, Value1=0.08f },
        new Def { Id=AbilityId.A05, Name="단단한 방어",   Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.Defense,  Value1=0.06f },

        new Def { Id=AbilityId.A06, Name="기사의 용맹",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Job_Knight,
                  Stat1=StatType.Attack,   Value1=0.08f, HasStat2=true, Stat2=StatType.MaxHp,              Value2=0.06f },
        new Def { Id=AbilityId.A07, Name="명궁의 직관",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Job_Archer,
                  Stat1=StatType.AttackRange, Value1=0.10f, HasStat2=true, Stat2=StatType.AttackSpeed,     Value2=0.06f },
        new Def { Id=AbilityId.A08, Name="마법사의 집중", Grade=AbilityGrade.Normal, Target=AbilityTarget.Job_Mage,
                  Stat1=StatType.Attack,   Value1=0.10f, HasStat2=true, Stat2=StatType.SkillCooldownReduce, Value2=0.08f },
        new Def { Id=AbilityId.A09, Name="방패병의 수호", Grade=AbilityGrade.Normal, Target=AbilityTarget.Job_ShieldBearer,
                  Stat1=StatType.Defense,  Value1=0.12f, HasStat2=true, Stat2=StatType.MaxHp,              Value2=0.06f },

        new Def { Id=AbilityId.A10, Name="전사의 돌격",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Range_Melee,
                  Stat1=StatType.Attack,   Value1=0.08f, HasStat2=true, Stat2=StatType.MoveSpeed,          Value2=0.06f },
        new Def { Id=AbilityId.A11, Name="원거리 집중",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Range_Ranged,
                  Stat1=StatType.AttackRange, Value1=0.08f, HasStat2=true, Stat2=StatType.Attack,          Value2=0.06f },

        new Def { Id=AbilityId.A12, Name="장군의 위엄",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Unit_General,
                  Stat1=StatType.MaxHp,    Value1=0.10f, HasStat2=true, Stat2=StatType.Defense,            Value2=0.06f },
        new Def { Id=AbilityId.A13, Name="병사의 투지",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Unit_Soldier,
                  Stat1=StatType.Attack,   Value1=0.10f, HasStat2=true, Stat2=StatType.MoveSpeed,          Value2=0.06f },

        new Def { Id=AbilityId.A14, Name="치명의 감각",   Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.CritChance, Value1=0.06f },
        new Def { Id=AbilityId.A15, Name="넓은 시야",     Grade=AbilityGrade.Normal, Target=AbilityTarget.All,
                  Stat1=StatType.AttackRange, Value1=0.08f },

        // ── 고급 (Advanced) ───────────────────────────────────
        new Def { Id=AbilityId.B01, Name="철벽 체력",     Grade=AbilityGrade.Advanced, Target=AbilityTarget.All,
                  Stat1=StatType.MaxHp,    Value1=0.15f },
        new Def { Id=AbilityId.B02, Name="강철 검격",     Grade=AbilityGrade.Advanced, Target=AbilityTarget.All,
                  Stat1=StatType.Attack,   Value1=0.15f },
        new Def { Id=AbilityId.B03, Name="폭풍 연격",     Grade=AbilityGrade.Advanced, Target=AbilityTarget.All,
                  Stat1=StatType.AttackSpeed, Value1=0.12f },
        new Def { Id=AbilityId.B04, Name="질풍 기동",     Grade=AbilityGrade.Advanced, Target=AbilityTarget.All,
                  Stat1=StatType.MoveSpeed, Value1=0.12f },

        new Def { Id=AbilityId.B05, Name="기사의 분노",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Job_Knight,
                  Stat1=StatType.Attack,   Value1=0.15f, HasStat2=true, Stat2=StatType.MaxHp,              Value2=0.10f },
        new Def { Id=AbilityId.B06, Name="독수리의 눈",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Job_Archer,
                  Stat1=StatType.AttackRange, Value1=0.15f, HasStat2=true, Stat2=StatType.AttackSpeed,     Value2=0.12f },
        new Def { Id=AbilityId.B07, Name="마법의 각성",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Job_Mage,
                  Stat1=StatType.Attack,   Value1=0.18f, HasStat2=true, Stat2=StatType.SkillCooldownReduce, Value2=0.15f },
        new Def { Id=AbilityId.B08, Name="철옹성",        Grade=AbilityGrade.Advanced, Target=AbilityTarget.Job_ShieldBearer,
                  Stat1=StatType.Defense,  Value1=0.20f, HasStat2=true, Stat2=StatType.MaxHp,              Value2=0.12f },

        new Def { Id=AbilityId.B09, Name="광전사의 기세", Grade=AbilityGrade.Advanced, Target=AbilityTarget.Range_Melee,
                  Stat1=StatType.Attack,   Value1=0.15f, HasStat2=true, Stat2=StatType.MoveSpeed,          Value2=0.10f },
        new Def { Id=AbilityId.B10, Name="정밀 사격",     Grade=AbilityGrade.Advanced, Target=AbilityTarget.Range_Ranged,
                  Stat1=StatType.AttackRange, Value1=0.12f, HasStat2=true, Stat2=StatType.Attack,          Value2=0.12f },

        new Def { Id=AbilityId.B11, Name="영웅의 기상",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Unit_General,
                  Stat1=StatType.MaxHp,    Value1=0.18f, HasStat2=true, Stat2=StatType.Defense,            Value2=0.12f },
        new Def { Id=AbilityId.B12, Name="병사의 맹세",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Unit_Soldier,
                  Stat1=StatType.Attack,   Value1=0.18f, HasStat2=true, Stat2=StatType.MoveSpeed,          Value2=0.12f },
    };

    const string IconDir = "Assets/_project/3.Textures/Icons/Abilities";

    [MenuItem("Tools/Project K/어빌리티/Create Ability Assets")]
    public static void Create()
    {
        // 폴더 보장
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(SaveDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Abilities");

        var assets = new AbilityData[Defs.Length];

        for (int i = 0; i < Defs.Length; i++)
        {
            var def  = Defs[i];
            var path = $"{SaveDir}/Ability_{def.Id}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<AbilityData>(path);
            var so       = existing != null ? existing : ScriptableObject.CreateInstance<AbilityData>();

            so.Id          = def.Id;
            so.AbilityName = def.Name;
            so.Grade       = def.Grade;
            so.Target      = def.Target;
            so.Stat1       = def.Stat1;
            so.Value1      = def.Value1;
            so.HasStat2    = def.HasStat2;
            so.Stat2       = def.Stat2;
            so.Value2      = def.Value2;

            // 아이콘 자동 연결
            string idStr   = def.Id.ToString().ToLower();
            string iconPath = $"{IconDir}/ability_{idStr}.png";
            var    sprite   = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null) so.Icon = sprite;

            if (existing == null) AssetDatabase.CreateAsset(so, path);
            else                  EditorUtility.SetDirty(so);

            assets[i] = so;
        }

        // Database 생성/업데이트
        var dbExisting = AssetDatabase.LoadAssetAtPath<AbilityDatabase>(DatabasePath);
        var db         = dbExisting != null ? dbExisting : ScriptableObject.CreateInstance<AbilityDatabase>();

        var serialized = new SerializedObject(db);
        var prop       = serialized.FindProperty("_abilities");
        prop.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        serialized.ApplyModifiedProperties();

        if (dbExisting == null) AssetDatabase.CreateAsset(db, DatabasePath);
        else                    EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AbilityCreator] 어빌리티 {assets.Length}종 (Normal 15 + Advanced 12) + AbilityDatabase 생성 완료");
    }
}
#endif
