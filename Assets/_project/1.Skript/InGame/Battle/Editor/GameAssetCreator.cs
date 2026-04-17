using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Target = PassiveSkillApplier.ApplyTarget;

// ============================================================
//  GameAssetCreator.cs  [Editor Only]
//  패시브 스킬 SO 18종 + Database 자동 생성 도구.
//
//  사용법:
//    Unity 메뉴 → BattleGame → 데이터 생성 → 패시브 스킬 전체 생성
//
//  생성 위치:
//    Assets/_project/Data/Passives/  ← 개별 패시브 SO
//    Assets/_project/PassiveSkillDatabase.asset  ← 기존 파일 갱신
// ============================================================

public static class GameAssetCreator
{
    const string DataRoot    = "Assets/_project/Data";
    const string PassiveDir  = "Assets/_project/Data/Passives";
    const string DBPath      = "Assets/_project/PassiveSkillDatabase.asset";
    const string StageCfgPath = "Assets/_project/StageConfig.asset";

    // ── StageConfig 생성 ──────────────────────────────────────

    [MenuItem("BattleGame/데이터 생성/StageConfig 생성")]
    static void CreateStageConfig()
    {
        // 이미 존재하면 선택만 하고 종료
        var existing = AssetDatabase.LoadAssetAtPath<StageConfig>(StageCfgPath);
        if (existing != null)
        {
            Debug.Log($"[GameAssetCreator] StageConfig 이미 존재합니다: {StageCfgPath}");
            EditorGUIUtility.PingObject(existing);
            Selection.activeObject = existing;
            return;
        }

        // 생성 (필드 기본값은 클래스 초기값 그대로 사용)
        var cfg = ScriptableObject.CreateInstance<StageConfig>();
        AssetDatabase.CreateAsset(cfg, StageCfgPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 씬의 LobbyManager 에 자동 할당 (씬이 열려 있을 경우)
        var lobbyMgr = Object.FindAnyObjectByType<LobbyManager>();
        if (lobbyMgr != null)
        {
            var so   = new SerializedObject(lobbyMgr);
            var prop = so.FindProperty("_stageConfig");
            if (prop != null)
            {
                prop.objectReferenceValue = cfg;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(lobbyMgr);
                Debug.Log("[GameAssetCreator] LobbyManager._stageConfig 자동 할당 완료");
            }
        }
        else
        {
            Debug.Log("[GameAssetCreator] 씬에 LobbyManager 없음 — Inspector 에서 직접 할당하세요.");
        }

        EditorGUIUtility.PingObject(cfg);
        Selection.activeObject = cfg;
        Debug.Log($"[GameAssetCreator] StageConfig 생성 완료: {StageCfgPath}");
    }

    // ── 패시브 스킬 생성 ─────────────────────────────────────

    [MenuItem("BattleGame/데이터 생성/패시브 스킬 전체 생성")]
    public static void CreateAllPassiveSkills()
    {
        // ── 폴더 준비 ─────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(DataRoot))
            AssetDatabase.CreateFolder("Assets/_project", "Data");
        if (!AssetDatabase.IsValidFolder(PassiveDir))
            AssetDatabase.CreateFolder(DataRoot, "Passives");

        // ── Database 로드 ─────────────────────────────────────
        var db = AssetDatabase.LoadAssetAtPath<PassiveSkillDatabase>(DBPath);
        if (db == null)
        {
            Debug.LogError($"[GameAssetCreator] PassiveSkillDatabase 없음: {DBPath}\n" +
                           "Assets/_project/ 에 PassiveSkillDatabase.asset 이 있는지 확인하세요.");
            return;
        }

        db.Entries.Clear();

        // ── ① 병사 강화 ───────────────────────────────────────

        Make<PassiveSkillData>(db, PassiveSkillType.ExtraSoldiers, "ExtraSoldiers",
            "병사 수 +5명",
            "장군 소속 병사 수를 5명 추가합니다.",
            PassiveTrigger.None,
            Mod(StatType.SoldierCount, 5f,    false, Target.General));

        Make<PassiveSkillData>(db, PassiveSkillType.SoldierCombatBoost, "SoldierCombatBoost",
            "병사 전투 강화",
            "병사의 공격력과 이동속도를 각각 20%·10% 증가시킵니다.",
            PassiveTrigger.None,
            Mod(StatType.Attack,    0.20f, true, Target.Soldier),
            Mod(StatType.MoveSpeed, 0.10f, true, Target.Soldier));

        Make<PassiveSkillData>(db, PassiveSkillType.SoldierHorde, "SoldierHorde",
            "군중 전술",
            "병사 수 +10명. 단, 병사 공격력·체력 -10%.",
            PassiveTrigger.None,
            Mod(StatType.SoldierCount, 10f,   false, Target.General),
            Mod(StatType.Attack,      -0.10f, true,  Target.Soldier),
            Mod(StatType.MaxHp,       -0.10f, true,  Target.Soldier));

        Make<PassiveSkillData>(db, PassiveSkillType.VanguardAura, "VanguardAura",
            "선봉 오라",
            "병사 방어율 +10%.",
            PassiveTrigger.None,
            Mod(StatType.Defense, 0.10f, false, Target.Soldier));

        // ── ② 교환 ────────────────────────────────────────────

        Make<PassiveSkillData>(db, PassiveSkillType.WeakGeneralStrongSoldier, "WeakGeneralStrongSoldier",
            "약한 장군, 강한 병사",
            "장군 공격력·체력 -20%. 병사 공격력·체력 +30%.",
            PassiveTrigger.None,
            Mod(StatType.MaxHp,  -0.20f, true, Target.General),
            Mod(StatType.Attack, -0.20f, true, Target.General),
            Mod(StatType.MaxHp,   0.30f, true, Target.Soldier),
            Mod(StatType.Attack,  0.30f, true, Target.Soldier));

        Make<PassiveSkillData>(db, PassiveSkillType.StrongGeneralWeakSoldier, "StrongGeneralWeakSoldier",
            "강한 장군, 약한 병사",
            "병사 공격력·체력 -20%. 장군 공격력·체력 +30%.",
            PassiveTrigger.None,
            Mod(StatType.MaxHp,  -0.20f, true, Target.Soldier),
            Mod(StatType.Attack, -0.20f, true, Target.Soldier),
            Mod(StatType.MaxHp,   0.30f, true, Target.General),
            Mod(StatType.Attack,  0.30f, true, Target.General));

        Make<PassiveSkillData>(db, PassiveSkillType.WeakGeneralMoreSoldiers, "WeakGeneralMoreSoldiers",
            "희생의 지휘",
            "장군 공격력·체력 -15%. 병사 수 +8명.",
            PassiveTrigger.None,
            Mod(StatType.MaxHp,        -0.15f, true,  Target.General),
            Mod(StatType.Attack,       -0.15f, true,  Target.General),
            Mod(StatType.SoldierCount,  8f,    false, Target.General));

        Make<PassiveSkillData>(db, PassiveSkillType.BerserkerPact, "BerserkerPact",
            "광전사의 맹약",
            "전체 공격력·공격속도 +25%. 방어율 -0.15.",
            PassiveTrigger.None,
            Mod(StatType.Attack,      0.25f,  true,  Target.General),
            Mod(StatType.AttackSpeed, 0.25f,  true,  Target.General),
            Mod(StatType.Defense,    -0.15f,  false, Target.General),
            Mod(StatType.Attack,      0.25f,  true,  Target.Soldier),
            Mod(StatType.AttackSpeed, 0.25f,  true,  Target.Soldier),
            Mod(StatType.Defense,    -0.15f,  false, Target.Soldier));

        // ── ③ 제너럴 강화 ─────────────────────────────────────

        Make<PassiveSkillData>(db, PassiveSkillType.GeneralCombatBoost, "GeneralCombatBoost",
            "장군 전투 강화",
            "장군 공격력·이동속도 +15%.",
            PassiveTrigger.None,
            Mod(StatType.Attack,    0.15f, true, Target.General),
            Mod(StatType.MoveSpeed, 0.15f, true, Target.General));

        var titan = Make<PassiveSkillData>(db, PassiveSkillType.TitanGeneral, "TitanGeneral",
            "거인 장군",
            "장군 체력·공격력 +30%·+20%. 공격·이동속도 -15%. 체격 +30%.",
            PassiveTrigger.None,
            Mod(StatType.MaxHp,       0.30f,  true, Target.General),
            Mod(StatType.Attack,      0.20f,  true, Target.General),
            Mod(StatType.AttackSpeed, -0.15f, true, Target.General),
            Mod(StatType.MoveSpeed,   -0.15f, true, Target.General));
        titan.GeneralScaleBonusAdd = 0.3f;
        EditorUtility.SetDirty(titan);

        Make<PassiveSkillData>(db, PassiveSkillType.CommanderFury, "CommanderFury",
            "지휘관의 분노",
            "장군 크리티컬 확률 +15%, 크리티컬 배율 +0.5.",
            PassiveTrigger.None,
            Mod(StatType.CritChance, 0.15f, false, Target.General),
            Mod(StatType.CritDamage, 0.50f, false, Target.General));

        // ── ④ 시너지 ──────────────────────────────────────────

        Make<PassiveSkillData>(db, PassiveSkillType.SoldierEmpowerGeneral, "SoldierEmpowerGeneral",
            "병력의 힘",
            "병사 1명당 장군 공격력·체력 +1%.",
            PassiveTrigger.None,
            Mod(StatType.Attack, 0.01f, true, Target.General),
            Mod(StatType.MaxHp,  0.01f, true, Target.General));

        Make<PassiveSkillData>(db, PassiveSkillType.UnityStrength, "UnityStrength",
            "결속의 힘",
            "병사 1명당 장군 공격력·체력 +1.5%.",
            PassiveTrigger.None,
            Mod(StatType.Attack, 0.015f, true, Target.General),
            Mod(StatType.MaxHp,  0.015f, true, Target.General));

        Make<PassiveSoldierDeathEmpower>(db, PassiveSkillType.SoldierDeathEmpower, "SoldierDeathEmpower",
            "병사의 유산",
            "병사 사망 1명당 장군 공격력 +2%, 체력 +1%.",
            PassiveTrigger.OnSoldierDeath,
            Mod(StatType.Attack, 0.02f, true, Target.General),
            Mod(StatType.MaxHp,  0.01f, true, Target.General));

        Make<PassiveSkillData>(db, PassiveSkillType.SacrificeRitual, "SacrificeRitual",
            "희생 의식",
            "병사 5명 희생 → 장군 공격력·체력 +20%.",
            PassiveTrigger.None,
            Mod(StatType.Attack, 0.20f, true, Target.General),
            Mod(StatType.MaxHp,  0.20f, true, Target.General));

        // ── ⑤ 조건부 ─────────────────────────────────────────

        Make<PassiveBloodPact>(db, PassiveSkillType.BloodPact, "BloodPact",
            "피의 계약",
            "장군 체력이 낮을수록 공격력 최대 +50%.",
            PassiveTrigger.OnHit,
            Mod(StatType.Attack, 0.50f, true, Target.General));

        var ironWill = Make<PassiveIronWill>(db, PassiveSkillType.IronWill, "IronWill",
            "강철 의지",
            "장군 HP 50% 이하 시 공격력·체력 +20%·+10% (1회).",
            PassiveTrigger.OnHit,
            Mod(StatType.Attack, 0.20f, true, Target.General),
            Mod(StatType.MaxHp,  0.10f, true, Target.General));
        ironWill.HpThreshold = 0.5f;
        EditorUtility.SetDirty(ironWill);

        var lastStand = Make<PassiveLastStand>(db, PassiveSkillType.LastStand, "LastStand",
            "최후의 항전",
            "병사 수가 초기의 50% 이하 시 남은 병사 공격력·체력 +30%·+20% (1회).",
            PassiveTrigger.OnSoldierDeath,
            Mod(StatType.Attack, 0.30f, true, Target.Soldier),
            Mod(StatType.MaxHp,  0.20f, true, Target.Soldier));
        lastStand.SoldierThreshold = 0.5f;
        EditorUtility.SetDirty(lastStand);

        // ── 저장 ─────────────────────────────────────────────
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GameAssetCreator] 완료 — 패시브 {db.Entries.Count}종 생성, Database 갱신: {DBPath}");
        EditorGUIUtility.PingObject(db);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────

    /// <summary>SO 생성 또는 로드 후 기본값 설정. Database Entries 에 등록.</summary>
    static T Make<T>(
        PassiveSkillDatabase db,
        PassiveSkillType type,
        string fileName,
        string skillName,
        string description,
        PassiveTrigger trigger,
        params PassiveSkillData.StatModifierEntry[] mods) where T : PassiveSkillData
    {
        string path = $"{PassiveDir}/{fileName}.asset";
        var so = AssetDatabase.LoadAssetAtPath<T>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
        }

        so.Type          = type;
        so.SkillName     = skillName;
        so.Description   = description;
        so.TriggerType   = trigger;
        so.StatModifiers = new List<PassiveSkillData.StatModifierEntry>(mods);

        EditorUtility.SetDirty(so);
        db.Entries.Add(so);
        return so;
    }

    static PassiveSkillData.StatModifierEntry Mod(
        StatType stat, float delta, bool isPercent, Target target)
        => new PassiveSkillData.StatModifierEntry
        {
            Stat      = stat,
            Delta     = delta,
            IsPercent = isPercent,
            Target    = target,
        };
}
