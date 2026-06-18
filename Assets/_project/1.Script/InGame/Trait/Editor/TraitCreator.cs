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
        public TraitType    type;
        public string       name;
        public string       desc;
        public UnitJob      job;
        public (StatType stat, float value, bool isPct)[] fx;
        // 스택 누적 보너스 (StackTrigger = None 이면 사용 안 함)
        public PassiveTrigger stackTrigger;
        public int            maxStacks;
        public (StatType stat, float value, bool isPct)[] stackFx;
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
                (StatType.MaxHp,       0.10f, true ),
                (StatType.SoldierCount, 2f,   false),
                (StatType.CommandPower, 2f,   false),
            },
        },
        new Def
        {
            type         = TraitType.KnightSoldierRage,
            name         = "전우의 분노",
            desc         = "병사가 쓰러질 때마다 장군의 최대 체력이 영구적으로 1% 증가한다.",
            job          = UnitJob.Knight,
            fx           = System.Array.Empty<(StatType, float, bool)>(),
            stackTrigger = PassiveTrigger.OnSoldierDeath,
            maxStacks    = 0,  // 무제한
            stackFx      = new[] { (StatType.MaxHp, 0.01f, true) },
        },
        new Def
        {
            type = TraitType.KnightHeroReturn,
            name = "영웅의 귀환",
            desc = "장군이 쓰러지는 순간, 마지막 기개로 병사들을 전장에 새롭게 소환한다.",
            job  = UnitJob.Knight,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type = TraitType.ArcherPrecision,
            name = "정밀 사수",
            desc = "숨을 죽이고 조준하는 궁수의 집중력. 공격력·사거리·공격속도가 강화된다.",
            job  = UnitJob.Archer,
            fx   = new[]
            {
                (StatType.Attack,      0.15f, true ),
                (StatType.AttackRange,  1f,   false),
                (StatType.AttackSpeed,  0.15f, false),
            },
        },
        new Def
        {
            type = TraitType.ArcherRetreatFire,
            name = "퇴각 사격",
            desc = "적이 사거리 절반 이내로 접근하면 자동으로 후퇴하며 사격을 멈추지 않는다.",
            job  = UnitJob.Archer,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type = TraitType.ArcherRainFire,
            name = "폭우 사격",
            desc = "공격할 때마다 주변의 적 2명을 추가로 타격한다. 추가 피해는 70%.",
            job  = UnitJob.Archer,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type = TraitType.MageArcane,
            name = "마력 집중",
            desc = "내면의 마력을 극한까지 끌어올린 마법사의 각성. 공격력과 스킬 쿨다운이 강화된다.",
            job  = UnitJob.Mage,
            fx   = new[]
            {
                (StatType.Attack,             0.20f, true ),
                (StatType.SkillCooldownReduce, 0.10f, false),
            },
        },
        new Def
        {
            type = TraitType.MageAttackCdr,
            name = "마법 집중",
            desc = "공격할 때마다 액티브 스킬 쿨타임이 1초 감소한다.",
            job  = UnitJob.Mage,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type = TraitType.MageEchoSkill,
            name = "연속 시전",
            desc = "스킬을 사용하면 즉시 동일한 스킬이 40% 위력으로 재발동된다.",
            job  = UnitJob.Mage,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type = TraitType.ShieldFortress,
            name = "강철 요새",
            desc = "흔들리지 않는 방패병의 육체. 체력과 방어율이 대폭 강화된다.",
            job  = UnitJob.ShieldBearer,
            fx   = new[]
            {
                (StatType.MaxHp,  0.20f, true ),
                (StatType.Defense, 0.08f, false),
            },
        },
        new Def
        {
            type = TraitType.ShieldCounterBlow,
            name = "반격의 달인",
            desc = "피격 시 받은 피해를 그대로 공격자에게 반사한다.",
            job  = UnitJob.ShieldBearer,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type         = TraitType.ShieldRageBuild,
            name         = "분노 축적",
            desc         = "스테이지를 클리어할 때마다 공격력이 3% 증가한다. 최대 10회 중첩.",
            job          = UnitJob.ShieldBearer,
            fx           = System.Array.Empty<(StatType, float, bool)>(),
            stackTrigger = PassiveTrigger.StageClear,
            maxStacks    = 10,
            stackFx      = new[] { (StatType.Attack, 0.03f, true) },
        },

        // ── 공통 장수 배치 슬롯 확장 특성 ───────────────────────
        new Def
        {
            type = TraitType.CommonExpedition,
            name = "원정 편성",
            desc = "장수 배치 슬롯이 1칸 증가한다. 최대 5칸까지 늘어난다.",
            job  = (UnitJob)255,  // 공통 — 직업 자동 배정 없음
            fx   = new[] { (StatType.GeneralSlotBonus, 1f, false) },
        },
        new Def
        {
            type = TraitType.CommonMassMobilize,
            name = "대규모 동원",
            desc = "장수 배치 슬롯이 2칸 증가하지만, 모든 장수의 능력치가 10% 감소한다. 최대 5칸까지 늘어난다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.GeneralSlotBonus, 2f,   false),
                (StatType.AllStatPenalty,   0.10f, false),
            },
        },
        new Def
        {
            type = TraitType.CommonSoldierSupply,
            name = "병사 지원령",
            desc = "장수 배치 슬롯이 1칸 증가하고, 모든 장수의 병사 수가 5명 증가한다. 최대 5칸까지 늘어난다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.GeneralSlotBonus, 1f, false),
                (StatType.SoldierCount,     5f, false),
            },
        },
        new Def
        {
            type = TraitType.CommonForcedLevy,
            name = "무리한 징집",
            desc = "장수 배치 슬롯이 1칸 증가하지만, 모든 장수의 이동속도가 15% 감소한다. 최대 5칸까지 늘어난다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.GeneralSlotBonus, 1f,     false),
                (StatType.MoveSpeed,       -0.15f,  true),
            },
        },
        new Def
        {
            type = TraitType.CommonEquipExpand,
            name = "중무장 편성",
            desc = "모든 장수의 장비 슬롯이 1칸 증가한다. 더 많은 장비를 착용할 수 있다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.EquipSlotBonus, 1f, false) },
        },

        // ── 직업 시너지 (자동 부여 — 상점·이벤트 비등장) ─────────
        new Def
        {
            type = TraitType.Synergy_VanguardCross,
            name = "선봉대",
            desc = "기사와 궁수가 함께하면 전진 속도와 화력이 맞물려 공격력이 상승한다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.10f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_MagicShield,
            name = "마법 방패",
            desc = "기사의 방어선 뒤에서 법사가 마법을 펼치며 공격력과 체력이 상승한다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.05f, true), (StatType.MaxHp, 0.05f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_IronWallLine,
            name = "철벽진",
            desc = "기사와 방패병이 이중 방어선을 형성해 방어율과 체력이 상승한다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.10f, true), (StatType.Defense, 0.05f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_BalancedHost,
            name = "균형의 군세",
            desc = "네 직업이 모두 갖춰진 완전한 군대. 모든 스탯이 5% 상승한다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.Attack,      0.05f, true ),
                (StatType.MaxHp,       0.05f, true ),
                (StatType.Defense,     0.05f, false),
                (StatType.MoveSpeed,   0.05f, true ),
                (StatType.AttackSpeed, 0.05f, true ),
            },
        },
        new Def
        {
            type = TraitType.Synergy_KnightOrder,
            name = "기사단",
            desc = "다섯 기사의 창이 하나로 모인다. 공격력 +30%, 이동속도 +20%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.30f, true), (StatType.MoveSpeed, 0.20f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_ArrowLegion,
            name = "화살의 군단",
            desc = "하늘을 가리는 화살의 비. 공격력 +30%, 공격속도 +20%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.30f, true), (StatType.AttackSpeed, 0.20f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_GreatMageCorp,
            name = "대법사단",
            desc = "다섯 법사의 마력이 공명한다. 공격력 +35%, 스킬 쿨타임 -15%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.35f, true), (StatType.SkillCooldownReduce, 0.15f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_Ironclad,
            name = "철옹성",
            desc = "무너지지 않는 철벽. 최대체력 +40%, 방어율 +15%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.40f, true), (StatType.Defense, 0.15f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_RangedFirenet,
            name = "원거리 화망",
            desc = "궁수와 법사가 원거리 화망을 펼친다. 공격력 +20%, 사거리 +10%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.20f, true), (StatType.AttackRange, 0.10f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_IronVanguard,
            name = "철벽 전위대",
            desc = "기사와 방패병이 함께 전선을 지킨다. 최대체력 +30%, 방어율 +10%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.30f, true), (StatType.Defense, 0.10f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_KnightSquad,
            name = "기사 소대",
            desc = "세 기사의 연대. 공격력 +10%, 이동속도 +6%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.10f, true), (StatType.MoveSpeed, 0.06f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_ArcherSquad,
            name = "궁수 소대",
            desc = "세 궁수의 일제 사격. 공격력 +10%, 공격속도 +6%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.10f, true), (StatType.AttackSpeed, 0.06f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_MageSquad,
            name = "법사 소대",
            desc = "세 법사의 마력 집중. 공격력 +12%, 스킬 쿨타임 -5%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.12f, true), (StatType.SkillCooldownReduce, 0.05f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_ShieldSquad,
            name = "방패병 소대",
            desc = "세 방패병의 굳건한 방어. 최대체력 +13%, 방어율 +5%.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.13f, true), (StatType.Defense, 0.05f, false) },
        },
    };

    // ── 헬퍼 ──────────────────────────────────────────────────

    static readonly (TraitType type, string iconPath)[] IconPaths = new[]
    {
        (TraitType.KnightCommand,    "Assets/_project/3.Textures/Icons/Traits/trait_knight_command.png"),
        (TraitType.KnightSoldierRage,"Assets/_project/3.Textures/Icons/Traits/trait_knight_soldier_rage.png"),
        (TraitType.KnightHeroReturn, "Assets/_project/3.Textures/Icons/Traits/trait_knight_hero_return.png"),
        (TraitType.ArcherPrecision,  "Assets/_project/3.Textures/Icons/Traits/trait_archer_precision.png"),
        (TraitType.ArcherRetreatFire,"Assets/_project/3.Textures/Icons/Traits/trait_archer_retreat_fire.png"),
        (TraitType.ArcherRainFire,   "Assets/_project/3.Textures/Icons/Traits/trait_archer_rain_fire.png"),
        (TraitType.MageArcane,       "Assets/_project/3.Textures/Icons/Traits/trait_mage_arcane.png"),
        (TraitType.MageAttackCdr,    "Assets/_project/3.Textures/Icons/Traits/trait_mage_attack_cdr.png"),
        (TraitType.MageEchoSkill,    "Assets/_project/3.Textures/Icons/Traits/trait_mage_echo_skill.png"),
        (TraitType.ShieldFortress,   "Assets/_project/3.Textures/Icons/Traits/trait_shield_fortress.png"),
        (TraitType.ShieldCounterBlow,    "Assets/_project/3.Textures/Icons/Traits/trait_shield_counter_blow.png"),
        (TraitType.ShieldRageBuild,      "Assets/_project/3.Textures/Icons/Traits/trait_shield_rage_build.png"),
        (TraitType.CommonExpedition,     "Assets/_project/3.Textures/Icons/Traits/trait_common_expedition.png"),
        (TraitType.CommonMassMobilize,   "Assets/_project/3.Textures/Icons/Traits/trait_common_mass_mobilize.png"),
        (TraitType.CommonSoldierSupply,  "Assets/_project/3.Textures/Icons/Traits/trait_common_soldier_supply.png"),
        (TraitType.CommonForcedLevy,     "Assets/_project/3.Textures/Icons/Traits/trait_common_forced_levy.png"),
        (TraitType.CommonEquipExpand,    "Assets/_project/3.Textures/Icons/Traits/trait_common_equip_expand.png"),
        // 시너지
        (TraitType.Synergy_VanguardCross, "Assets/_project/3.Textures/Icons/Traits/trait_synergy_vanguard.png"),
        (TraitType.Synergy_MagicShield,   "Assets/_project/3.Textures/Icons/Traits/trait_synergy_magic_shield.png"),
        (TraitType.Synergy_IronWallLine,  "Assets/_project/3.Textures/Icons/Traits/trait_synergy_iron_wall.png"),
        (TraitType.Synergy_BalancedHost,  "Assets/_project/3.Textures/Icons/Traits/trait_synergy_balanced.png"),
        (TraitType.Synergy_KnightOrder,   "Assets/_project/3.Textures/Icons/Traits/trait_synergy_knight_order.png"),
        (TraitType.Synergy_ArrowLegion,   "Assets/_project/3.Textures/Icons/Traits/trait_synergy_arrow_legion.png"),
        (TraitType.Synergy_GreatMageCorp, "Assets/_project/3.Textures/Icons/Traits/trait_synergy_mage_corp.png"),
        (TraitType.Synergy_Ironclad,      "Assets/_project/3.Textures/Icons/Traits/trait_synergy_ironclad.png"),
        (TraitType.Synergy_RangedFirenet, "Assets/_project/3.Textures/Icons/Traits/trait_synergy_ranged_firenet.png"),
        (TraitType.Synergy_IronVanguard,  "Assets/_project/3.Textures/Icons/Traits/trait_synergy_iron_vanguard.png"),
        (TraitType.Synergy_KnightSquad,   "Assets/_project/3.Textures/Icons/Traits/trait_synergy_knight_squad.png"),
        (TraitType.Synergy_ArcherSquad,   "Assets/_project/3.Textures/Icons/Traits/trait_synergy_archer_squad.png"),
        (TraitType.Synergy_MageSquad,     "Assets/_project/3.Textures/Icons/Traits/trait_synergy_mage_squad.png"),
        (TraitType.Synergy_ShieldSquad,   "Assets/_project/3.Textures/Icons/Traits/trait_synergy_shield_squad.png"),
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

        so.StackTrigger = def.stackTrigger;
        so.MaxStacks    = def.maxStacks;
        var sfx = def.stackFx ?? System.Array.Empty<(StatType, float, bool)>();
        so.StackStatBonuses = new TraitData.TraitStatEntry[sfx.Length];
        for (int i = 0; i < sfx.Length; i++)
        {
            so.StackStatBonuses[i] = new TraitData.TraitStatEntry
            {
                Stat      = sfx[i].stat,
                Value     = sfx[i].value,
                IsPercent = sfx[i].isPct,
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
