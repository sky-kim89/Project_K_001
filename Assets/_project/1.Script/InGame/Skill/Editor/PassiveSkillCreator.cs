#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// ============================================================
//  PassiveSkillCreator.cs  [Editor Only]
//  트리거 기반 패시브 스킬 SO 14종 + PassiveSkillDatabase 자동 생성.
//
//  메뉴: Tools > Project K > 패시브 > Create Trigger Passive Assets
//  출력:
//    Assets/Resources/Passives/Passive_{Type}.asset  ×14
//    Assets/Resources/PassiveSkillDatabase.asset  (기존 항목에 추가)
// ============================================================

public static class PassiveSkillCreator
{
    const string SaveDir      = "Assets/Resources/Passives";
    const string DatabasePath = "Assets/Resources/PassiveSkillDatabase.asset";

    [MenuItem("Tools/Project K/패시브/Create Trigger Passive Assets")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(SaveDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Passives");

        // ── Database 로드 또는 생성 ───────────────────────────
        var db = AssetDatabase.LoadAssetAtPath<PassiveSkillDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<PassiveSkillDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        int created = 0;

        // ── OnAttack 트리거 ──────────────────────────────────

        var vampiric = Make<PassiveVampiricStrike>(
            PassiveSkillType.VampiricStrike,
            "흡혈",
            "공격 시 가한 피해의 10%를 즉시 체력 회복.",
            PassiveTrigger.OnAttack, db);
        vampiric.VampireRatio = 0.10f;
        EditorUtility.SetDirty(vampiric);
        created++;

        var strengthStack = Make<PassiveStrengthStack>(
            PassiveSkillType.StrengthStack,
            "연격 스택",
            "연속 공격마다 공격력 +8 누적 (최대 5스택).",
            PassiveTrigger.OnAttack, db);
        strengthStack.AttackBonusPerStack = 8f;
        strengthStack.MaxStacks           = 5;
        EditorUtility.SetDirty(strengthStack);
        created++;

        var soldierMorale = Make<PassiveSoldierMorale>(
            PassiveSkillType.SoldierMorale,
            "병사 고무",
            "공격 시 30% 확률로 소속 병사 전체의 공격력을 3초 동안 버프.",
            PassiveTrigger.OnAttack, db);
        soldierMorale.TriggerChance   = 0.30f;
        soldierMorale.AttackBuffDelta = 15f;
        soldierMorale.BuffDuration    = 3f;
        EditorUtility.SetDirty(soldierMorale);
        created++;

        // ── OnHit 트리거 ─────────────────────────────────────

        var defenseShield = Make<PassiveDefenseShield>(
            PassiveSkillType.DefenseShield,
            "방어 강화",
            "피격 시 방어율 +10%를 3초 동안 버프.",
            PassiveTrigger.OnHit, db);
        defenseShield.DefenseBuff  = 0.10f;
        defenseShield.BuffDuration = 3f;
        EditorUtility.SetDirty(defenseShield);
        created++;

        var quickRecovery = Make<PassiveQuickRecovery>(
            PassiveSkillType.QuickRecovery,
            "긴급 회복",
            "피격 시 최대 체력의 5%를 즉시 회복.",
            PassiveTrigger.OnHit, db);
        quickRecovery.HealRatio = 0.05f;
        EditorUtility.SetDirty(quickRecovery);
        created++;

        var counterStrike = Make<PassiveCounterStrike>(
            PassiveSkillType.CounterStrike,
            "피격 반격 강화",
            "피격 시 40% 확률로 공격력 +20%를 5초 동안 버프.",
            PassiveTrigger.OnHit, db);
        counterStrike.TriggerChance  = 0.40f;
        counterStrike.AttackBonusRatio  = 0.20f;
        counterStrike.BuffDuration   = 5f;
        EditorUtility.SetDirty(counterStrike);
        created++;

        // ── OnEnemyKill 트리거 ───────────────────────────────

        var killMomentum = Make<PassiveKillMomentum>(
            PassiveSkillType.KillMomentum,
            "처치 가속",
            "처치마다 이동속도 +0.15 누적 (최대 5스택).",
            PassiveTrigger.OnEnemyKill, db);
        killMomentum.SpeedBonusPerKill = 0.15f;
        killMomentum.MaxStacks         = 5;
        EditorUtility.SetDirty(killMomentum);
        created++;

        var killEmpower = Make<PassiveKillEmpower>(
            PassiveSkillType.KillEmpower,
            "처치 강화",
            "처치마다 공격력 +10 누적 (최대 5스택).",
            PassiveTrigger.OnEnemyKill, db);
        killEmpower.AttackBonusPerKill = 10f;
        killEmpower.MaxStacks          = 5;
        EditorUtility.SetDirty(killEmpower);
        created++;

        var killHeal = Make<PassiveKillHeal>(
            PassiveSkillType.KillHeal,
            "처치 회복",
            "처치 시 최대 체력의 5%를 즉시 회복.",
            PassiveTrigger.OnEnemyKill, db);
        killHeal.HealRatio = 0.05f;
        EditorUtility.SetDirty(killHeal);
        created++;

        var soldierVigor = Make<PassiveSoldierVigor>(
            PassiveSkillType.SoldierVigor,
            "병사 결의",
            "처치 시 소속 병사 전체의 공격력을 4초 동안 버프.",
            PassiveTrigger.OnEnemyKill, db);
        soldierVigor.AttackBuffDelta = 20f;
        soldierVigor.BuffDuration    = 4f;
        EditorUtility.SetDirty(soldierVigor);
        created++;

        // ── OnSoldierDeath 트리거 ────────────────────────────

        var sacrificeAbsorb = Make<PassiveSacrificeAbsorb>(
            PassiveSkillType.SacrificeAbsorb,
            "희생 흡수",
            "병사 사망 시 사망 수 × 30만큼 즉시 체력 회복.",
            PassiveTrigger.OnSoldierDeath, db);
        sacrificeAbsorb.HealPerDeath = 30f;
        EditorUtility.SetDirty(sacrificeAbsorb);
        created++;

        // ── OnSkillUse 트리거 ────────────────────────────────

        var skillAdrenaline = Make<PassiveSkillAdrenaline>(
            PassiveSkillType.SkillAdrenaline,
            "스킬 아드레날린",
            "스킬 사용 시 공격력 +20, 공격속도 +0.3을 5초 동안 버프.",
            PassiveTrigger.OnSkillUse, db);
        skillAdrenaline.AttackBuff   = 20f;
        skillAdrenaline.AtkSpeedBuff = 0.3f;
        skillAdrenaline.BuffDuration = 5f;
        EditorUtility.SetDirty(skillAdrenaline);
        created++;

        var skillInstinct = Make<PassiveSkillInstinct>(
            PassiveSkillType.SkillInstinct,
            "생존 본능",
            "스킬 사용 시 50% 확률로 최대 체력의 8%를 즉시 회복.",
            PassiveTrigger.OnSkillUse, db);
        skillInstinct.TriggerChance = 0.50f;
        skillInstinct.HealRatio     = 0.08f;
        EditorUtility.SetDirty(skillInstinct);
        created++;

        var skillRally = Make<PassiveSkillRally>(
            PassiveSkillType.SkillRally,
            "전투 집결",
            "스킬 사용 시 소속 병사 전체의 공격력 +20, 이동속도 +0.5을 6초 동안 버프.",
            PassiveTrigger.OnSkillUse, db);
        skillRally.AttackBuffDelta = 20f;
        skillRally.SpeedBuffDelta  = 0.5f;
        skillRally.BuffDuration    = 6f;
        EditorUtility.SetDirty(skillRally);
        created++;

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PassiveSkillCreator] 트리거 패시브 {created}종 생성 완료. PassiveSkillDatabase 갱신: {DatabasePath}");
        EditorGUIUtility.PingObject(db);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────

    static T Make<T>(
        PassiveSkillType type,
        string skillName,
        string description,
        PassiveTrigger trigger,
        PassiveSkillDatabase db) where T : PassiveSkillData
    {
        string path     = $"{SaveDir}/Passive_{type}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<T>(path);
        var    so       = existing != null ? existing : ScriptableObject.CreateInstance<T>();

        so.Type        = type;
        so.SkillName   = skillName;
        so.Description = description;
        so.TriggerType = trigger;

        if (existing == null)
            AssetDatabase.CreateAsset(so, path);
        else
            EditorUtility.SetDirty(so);

        // 중복 등록 방지
        if (!db.Entries.Contains(so))
            db.Entries.Add(so);

        return so;
    }
}
#endif
