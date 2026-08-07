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

        new Def { Id=AbilityId.A16, Name="병사 추가",     Grade=AbilityGrade.Normal, Target=AbilityTarget.Unit_General,
                  Stat1=StatType.SoldierCount, Value1=1f },
        new Def { Id=AbilityId.A17, Name="지휘력 강화",   Grade=AbilityGrade.Normal, Target=AbilityTarget.Unit_General,
                  Stat1=StatType.CommandPower, Value1=10f },

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

        new Def { Id=AbilityId.B13, Name="병사 대규모",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Unit_General,
                  Stat1=StatType.SoldierCount, Value1=2f },
        new Def { Id=AbilityId.B14, Name="완벽한 지휘",   Grade=AbilityGrade.Advanced, Target=AbilityTarget.Unit_General,
                  Stat1=StatType.CommandPower, Value1=20f },
    };

    const string IconDir = "Assets/_project/3.Textures/Icons/Abilities";

    [MenuItem(ProjectKMenu.Data + "어빌리티", priority = ProjectKMenu.DataPrio + 14)]
    public static void Create()
    {
        // 폴더 보장
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(SaveDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Abilities");

        CreateSpecialAbilities();
        CreateMasteryAbilities();

        var normalAdvanced = new AbilityData[Defs.Length];

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
            string idStr    = def.Id.ToString().ToLower();
            string iconPath = $"{IconDir}/ability_{idStr}.png";
            var    sprite   = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null) so.Icon = sprite;

            if (existing == null) AssetDatabase.CreateAsset(so, path);
            else                  EditorUtility.SetDirty(so);

            normalAdvanced[i] = so;
        }

        // 특수 어빌리티 로드 (CreateSpecialAbilities 에서 이미 저장됨)
        var specials = new AbilityData[]
        {
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C01.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C02.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C03.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C04.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C05.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C06.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C07.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C08.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C09.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C10.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_C11.asset"),
        };

        // 달인 어빌리티 로드 (CreateMasteryAbilities 에서 이미 저장됨)
        var masteries = new AbilityData[]
        {
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_D01.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_D02.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_D03.asset"),
            AssetDatabase.LoadAssetAtPath<AbilityData>($"{SaveDir}/Ability_D04.asset"),
        };

        var allAssets = new AbilityData[normalAdvanced.Length + specials.Length + masteries.Length];
        normalAdvanced.CopyTo(allAssets, 0);
        specials.CopyTo(allAssets, normalAdvanced.Length);
        masteries.CopyTo(allAssets, normalAdvanced.Length + specials.Length);

        // Database 생성/업데이트
        var dbExisting = AssetDatabase.LoadAssetAtPath<AbilityDatabase>(DatabasePath);
        var db         = dbExisting != null ? dbExisting : ScriptableObject.CreateInstance<AbilityDatabase>();

        var serialized = new SerializedObject(db);
        var prop       = serialized.FindProperty("_abilities");
        prop.arraySize = allAssets.Length;
        for (int i = 0; i < allAssets.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = allAssets[i];
        serialized.ApplyModifiedProperties();

        if (dbExisting == null) AssetDatabase.CreateAsset(db, DatabasePath);
        else                    EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AbilityCreator] 어빌리티 {allAssets.Length}종 (Normal 17 + Advanced 14 + Special 11 + Mastery 4) + AbilityDatabase 생성 완료");
    }

    // ── 특수(Special) 어빌리티 C01-C04 생성 ─────────────────

    static void CreateSpecialAbilities()
    {
        // C01 — 흡혈 강습 (OnAttack)
        var c01 = MakeSpecial<AbilityVampiricAssault>(AbilityId.C01, "Ability_C01", "흡혈 강습");
        c01.VampireRatio = 0.15f;
        EditorUtility.SetDirty(c01);

        // C02 — 철갑 반응 (OnHit)
        var c02 = MakeSpecial<AbilityArmorReaction>(AbilityId.C02, "Ability_C02", "철갑 반응");
        c02.DefenseBuff = 0.001f;
        EditorUtility.SetDirty(c02);

        // C03 — 처치 연쇄 (OnEnemyKill)
        var c03 = MakeSpecial<AbilityKillChain>(AbilityId.C03, "Ability_C03", "처치 연쇄");
        c03.AttackBonusRatio = 0.05f;
        EditorUtility.SetDirty(c03);

        // C04 — 희생의 힘 (OnSoldierDeath)
        var c04 = MakeSpecial<AbilitySacrificeForce>(AbilityId.C04, "Ability_C04", "희생의 힘");
        c04.AttackBonusRatio = 0.05f;
        c04.MaxHpBonusRatio  = 0.05f;
        EditorUtility.SetDirty(c04);

        // C05 — 고통의 계약 (OnBattleStart)
        var c05 = MakeSpecial<AbilityPactOfAgony>(AbilityId.C05, "Ability_C05", "고통의 계약");
        c05.AgonyCostRatio = 0.70f;
        c05.AttackBonus    = 0.35f;
        c05.AtkSpdBonus    = 0.20f;
        c05.DefenseBonus   = 0.20f;
        EditorUtility.SetDirty(c05);

        // C06 — 거울 방어 (OnBattleStart)
        var c06 = MakeSpecial<AbilityMirrorArmor>(AbilityId.C06, "Ability_C06", "거울 방어");
        c06.ReflectRatio = 0.25f;
        EditorUtility.SetDirty(c06);

        // C07 — 혼령 집결 (OnBattleStart)
        var c07 = MakeSpecial<AbilityGhostRally>(AbilityId.C07, "Ability_C07", "혼령 집결");
        c07.BonusPerDeath = 0.05f;
        EditorUtility.SetDirty(c07);

        // C08 — 황금 탐욕 (시스템)
        var c08 = MakeSpecial<AbilityGoldBonus>(AbilityId.C08, "Ability_C08", "황금 탐욕");
        c08.GoldBonusRatio = 0.30f;
        EditorUtility.SetDirty(c08);

        // C09 — 성장 촉진 (시스템)
        var c09 = MakeSpecial<AbilityExpBonus>(AbilityId.C09, "Ability_C09", "성장 촉진");
        c09.ExpBonusRatio = 0.30f;
        EditorUtility.SetDirty(c09);

        // C10 — 시간 왜곡 (OnBattleStart)
        var c10 = MakeSpecial<AbilityTimeWarp>(AbilityId.C10, "Ability_C10", "시간 왜곡");
        c10.CooldownBonus  = 0.35f;
        c10.AttackPenalty  = 0.10f;
        EditorUtility.SetDirty(c10);

        // C11 — 쌍신 공격 (OnBattleStart)
        var c11 = MakeSpecial<AbilityTwinStrike>(AbilityId.C11, "Ability_C11", "쌍신 공격");
        c11.AttackPenalty = 0.40f;
        EditorUtility.SetDirty(c11);
    }

    // ── 달인(Mastery) 어빌리티 D01-D04 생성 ──────────────────

    static void CreateMasteryAbilities()
    {
        // D01 — 기사 달인 (OnBattleStart)
        var d01 = MakeMastery<AbilityKnightCharge>(AbilityId.D01, "Ability_D01", "기사 달인");
        d01.ChargeCooldown = 6f;
        EditorUtility.SetDirty(d01);

        // D02 — 궁수 달인 (OnBattleStart)
        MakeMastery<AbilityArcherMultiShot>(AbilityId.D02, "Ability_D02", "궁수 달인");

        // D03 — 마법사 달인 (OnBattleStart)
        MakeMastery<AbilityMageSkillProc>(AbilityId.D03, "Ability_D03", "마법사 달인");

        // D04 — 방패병 달인 (OnBattleStart)
        var d04 = MakeMastery<AbilityShieldMastery>(AbilityId.D04, "Ability_D04", "방패병 달인");
        d04.ReflectRatio = 0.25f;
        EditorUtility.SetDirty(d04);
    }

    static T MakeMastery<T>(AbilityId id, string fileName, string name) where T : AbilityData
    {
        string path     = $"{SaveDir}/{fileName}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<T>(path);
        var    so       = existing != null ? existing : ScriptableObject.CreateInstance<T>();

        so.Id          = id;
        so.AbilityName = name;
        so.Grade       = AbilityGrade.Mastery;
        so.Target      = AbilityTarget.All;
        so.MaxLevel    = 1;

        string idStr    = id.ToString().ToLower();
        string iconPath = $"{IconDir}/ability_{idStr}.png";
        var    sprite   = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite != null) so.Icon = sprite;

        if (existing == null) AssetDatabase.CreateAsset(so, path);
        else                  EditorUtility.SetDirty(so);

        return so;
    }

    static T MakeSpecial<T>(AbilityId id, string fileName, string name) where T : AbilityData
    {
        string path     = $"{SaveDir}/{fileName}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<T>(path);
        var    so       = existing != null ? existing : ScriptableObject.CreateInstance<T>();

        so.Id          = id;
        so.AbilityName = name;
        so.Grade       = AbilityGrade.Special;
        so.Target      = AbilityTarget.All;
        so.MaxLevel    = 1;

        // 아이콘 자동 연결 (ability_c01.png 등)
        string idStr    = id.ToString().ToLower();
        string iconPath = $"{IconDir}/ability_{idStr}.png";
        var    sprite   = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite != null) so.Icon = sprite;

        if (existing == null) AssetDatabase.CreateAsset(so, path);
        else                  EditorUtility.SetDirty(so);

        return so;
    }
}
#endif
