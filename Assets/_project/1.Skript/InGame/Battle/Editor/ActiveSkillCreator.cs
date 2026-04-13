using UnityEngine;
using UnityEditor;

// ============================================================
//  ActiveSkillCreator.cs  [Editor Only]
//  액티브 스킬 SO 20종 + ActiveSkillDatabase 자동 생성 도구.
//
//  사용법:
//    Unity 메뉴 → BattleGame → 데이터 생성 → 액티브 스킬 전체 생성
//
//  생성 위치:
//    Assets/_project/Data/Actives/  ← 개별 액티브 SO
//    Assets/_project/ActiveSkillDatabase.asset  ← 기존 파일 갱신
// ============================================================

public static class ActiveSkillCreator
{
    const string DataRoot   = "Assets/_project/Data";
    const string ActiveDir  = "Assets/_project/Data/Actives";
    const string DBPath     = "Assets/_project/ActiveSkillDatabase.asset";

    [MenuItem("BattleGame/데이터 생성/액티브 스킬 전체 생성")]
    public static void CreateAllActiveSkills()
    {
        // ── 폴더 준비 ─────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(DataRoot))
            AssetDatabase.CreateFolder("Assets/_project", "Data");
        if (!AssetDatabase.IsValidFolder(ActiveDir))
            AssetDatabase.CreateFolder(DataRoot, "Actives");

        // ── Database 로드 또는 생성 ───────────────────────────
        var db = AssetDatabase.LoadAssetAtPath<ActiveSkillDatabase>(DBPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ActiveSkillDatabase>();
            AssetDatabase.CreateAsset(db, DBPath);
        }
        db.Entries.Clear();

        // ── ① 강타 (전사·방패) ────────────────────────────────
        var heavyStrike = Make<ActiveHeavyStrike>(db,
            id          : ActiveSkillId.HeavyStrike,
            fileName    : "Active_HeavyStrike",
            skillName   : "강타",
            description : "타겟 방향으로 돌진하여 강력한 단일 타격 + 넉백.",
            cooldown    : 15f,
            effectValue : 1f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new[] { UnitJob.Knight, UnitJob.ShieldBearer });
        heavyStrike.DamageMultiplier = 3f;
        heavyStrike.DashSpeed        = 20f;
        heavyStrike.ReturnSpeed      = 12f;
        heavyStrike.KnockbackMult    = 5f;
        EditorUtility.SetDirty(heavyStrike);

        // ── ② 일제 사격 (궁수·법사) ──────────────────────────
        Make<ActiveVolleyFire>(db,
            id          : ActiveSkillId.VolleyFire,
            fileName    : "Active_VolleyFire",
            skillName   : "일제 사격",
            description : "제너럴과 소속 병사 전체가 현재 타겟에 즉시 공격을 발동한다.",
            cooldown    : 12f,
            effectValue : 1.5f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new[] { UnitJob.Archer, UnitJob.Mage });

        // ── ③ 도약 강타 (전사·방패) ──────────────────────────
        var leapStrike = Make<ActiveLeapStrike>(db,
            id          : ActiveSkillId.LeapStrike,
            fileName    : "Active_LeapStrike",
            skillName   : "도약 강타",
            description : "전방으로 도약하여 착지 반경 내 모든 적을 강타 + 넉백.",
            cooldown    : 18f,
            effectValue : 1f,
            radius      : 2.5f,
            duration    : 0f,
            jobs        : new[] { UnitJob.Knight, UnitJob.ShieldBearer });
        leapStrike.DamageMultiplier = 2.5f;
        leapStrike.LeapSpeed        = 18f;
        leapStrike.ReturnSpeed      = 10f;
        leapStrike.KnockbackMult    = 4f;
        EditorUtility.SetDirty(leapStrike);

        // ── ④ 치유 오라 (공통) ───────────────────────────────
        Make<ActiveHealAura>(db,
            id          : ActiveSkillId.HealAura,
            fileName    : "Active_HealAura",
            skillName   : "치유 오라",
            description : "제너럴과 소속 병사 전체의 체력을 최대 HP의 25% 즉시 회복.",
            cooldown    : 20f,
            effectValue : 0.25f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new UnitJob[0]);

        // ── ⑤ 집중 치유 (공통) ──────────────────────────────
        Make<ActiveTargetHeal>(db,
            id          : ActiveSkillId.TargetHeal,
            fileName    : "Active_TargetHeal",
            skillName   : "집중 치유",
            description : "소속 유닛 중 체력 비율이 가장 낮은 유닛을 최대 HP의 40% 집중 치유.",
            cooldown    : 25f,
            effectValue : 0.4f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new UnitJob[0]);

        // ── ⑥ 돌격 병사 (방패) ──────────────────────────────
        var chargeSoldier = Make<ActiveChargeSoldier>(db,
            id          : ActiveSkillId.ChargeSoldier,
            fileName    : "Active_ChargeSoldier",
            skillName   : "돌격 병사",
            description : "체력이 가장 높은 병사를 타겟으로 돌격시켜 피해 + 강한 넉백.",
            cooldown    : 20f,
            effectValue : 2f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new[] { UnitJob.ShieldBearer });
        chargeSoldier.ChargeSpeed    = 20f;
        chargeSoldier.KnockbackMult  = 5f;
        EditorUtility.SetDirty(chargeSoldier);

        // ── ⑦ 스켈레톤 소환 (공통) ──────────────────────────
        var summonSkeleton = Make<ActiveSummonSkeleton>(db,
            id          : ActiveSkillId.SummonSkeleton,
            fileName    : "Active_SummonSkeleton",
            skillName   : "스켈레톤 소환",
            description : "시전자 스텟 40% 수준의 스켈레톤 2기를 소환한다.",
            cooldown    : 30f,
            effectValue : 2f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new UnitJob[0]);
        summonSkeleton.SkeletonPoolKey = "Soldier";
        summonSkeleton.StatRatio       = 0.4f;
        summonSkeleton.SpawnRadius     = 1.5f;
        EditorUtility.SetDirty(summonSkeleton);

        // ── ⑧ 독성 지대 (법사·궁수) ─────────────────────────
        var poisonZone = Make<ActivePoisonZone>(db,
            id          : ActiveSkillId.PoisonZone,
            fileName    : "Active_PoisonZone",
            skillName   : "독성 지대",
            description : "타겟 위치에 독성 지대 생성. 이동속도 50% 감소 + 지속 피해.",
            cooldown    : 18f,
            effectValue : 15f,
            radius      : 2.5f,
            duration    : 6f,
            jobs        : new[] { UnitJob.Mage, UnitJob.Archer });
        poisonZone.MoveSlowMultiplier = 0.5f;
        poisonZone.TickInterval       = 0.5f;
        EditorUtility.SetDirty(poisonZone);

        // ── ⑨ 메테오 (법사) ─────────────────────────────────
        var meteor = Make<ActiveMeteor>(db,
            id          : ActiveSkillId.Meteor,
            fileName    : "Active_Meteor",
            skillName   : "메테오",
            description : "1.5초 후 타겟 위치에 메테오 낙하. 강력한 AoE 피해 + 강한 넉백.",
            cooldown    : 25f,
            effectValue : 1f,
            radius      : 3.5f,
            duration    : 1.5f,
            jobs        : new[] { UnitJob.Mage });
        meteor.DamageMultiplier = 5f;
        meteor.KnockbackMult    = 8f;
        EditorUtility.SetDirty(meteor);

        // ── ⑩ 블리자드 (법사) ───────────────────────────────
        var blizzard = Make<ActiveBlizzard>(db,
            id          : ActiveSkillId.Blizzard,
            fileName    : "Active_Blizzard",
            skillName   : "블리자드",
            description : "타겟 위치에 블리자드 지대. 이동속도·공격속도 감소 + 지속 피해.",
            cooldown    : 22f,
            effectValue : 10f,
            radius      : 3f,
            duration    : 8f,
            jobs        : new[] { UnitJob.Mage });
        blizzard.MoveSlowMultiplier   = 0.4f;
        blizzard.AttackSlowMultiplier = 0.5f;
        blizzard.TickInterval         = 0.5f;
        EditorUtility.SetDirty(blizzard);

        // ── ⑪ 병사 희생 (공통) ──────────────────────────────
        Make<ActiveSacrificeSoldier>(db,
            id          : ActiveSkillId.SacrificeSoldier,
            fileName    : "Active_SacrificeSoldier",
            skillName   : "병사 희생",
            description : "체력 최저 병사를 즉사시키고, 그 공격력의 80%를 시전자 공격력 버프로 흡수.",
            cooldown    : 30f,
            effectValue : 0.8f,
            radius      : 0f,
            duration    : 12f,
            jobs        : new UnitJob[0]);

        // ── ⑫ 속박 (공통) ───────────────────────────────────
        Make<ActiveBind>(db,
            id          : ActiveSkillId.Bind,
            fileName    : "Active_Bind",
            skillName   : "속박",
            description : "현재 타겟을 3초 동안 행동불능으로 만들고 지속 피해를 가한다.",
            cooldown    : 20f,
            effectValue : 0.3f,
            radius      : 0f,
            duration    : 3f,
            jobs        : new UnitJob[0]);

        // ── ⑬ 자폭 병사 (법사) ──────────────────────────────
        var suicideSoldier = Make<ActiveSuicideSoldier>(db,
            id          : ActiveSkillId.SuicideSoldier,
            fileName    : "Active_SuicideSoldier",
            skillName   : "자폭 병사",
            description : "병사를 포물선 궤도로 던져 착탄 시 범위 폭발 피해 + 넉백.",
            cooldown    : 25f,
            effectValue : 3f,
            radius      : 2.5f,
            duration    : 0f,
            jobs        : new[] { UnitJob.Mage });
        suicideSoldier.FlightDuration = 0.5f;
        suicideSoldier.ArcHeight      = 2f;
        suicideSoldier.KnockbackMult  = 7f;
        EditorUtility.SetDirty(suicideSoldier);

        // ── ⑭ 광전사 (전사) ─────────────────────────────────
        Make<ActiveBerserker>(db,
            id          : ActiveSkillId.Berserker,
            fileName    : "Active_Berserker",
            skillName   : "광전사",
            description : "시전자와 소속 병사 전체의 공격속도를 8초 동안 1.8배로 증가.",
            cooldown    : 20f,
            effectValue : 1.8f,
            radius      : 0f,
            duration    : 8f,
            jobs        : new[] { UnitJob.Knight });

        // ── ⑮ 철벽 방어 (방패) ──────────────────────────────
        Make<ActiveIronShield>(db,
            id          : ActiveSkillId.IronShield,
            fileName    : "Active_IronShield",
            skillName   : "철벽 방어",
            description : "시전자의 방어율을 8초 동안 +30% 증가.",
            cooldown    : 20f,
            effectValue : 0.3f,
            radius      : 0f,
            duration    : 8f,
            jobs        : new[] { UnitJob.ShieldBearer });

        // ── ⑯ 화살 비 (궁수) ────────────────────────────────
        var arrowRain = Make<ActiveArrowRain>(db,
            id          : ActiveSkillId.ArrowRain,
            fileName    : "Active_ArrowRain",
            skillName   : "화살 비",
            description : "타겟 위치에 5초 동안 화살 비를 내려 범위 지속 피해.",
            cooldown    : 18f,
            effectValue : 20f,
            radius      : 2f,
            duration    : 5f,
            jobs        : new[] { UnitJob.Archer });
        arrowRain.TickInterval = 0.4f;
        EditorUtility.SetDirty(arrowRain);

        // ── ⑰ 전투 함성 (전사·방패) ─────────────────────────
        Make<ActiveBattleCry>(db,
            id          : ActiveSkillId.BattleCry,
            fileName    : "Active_BattleCry",
            skillName   : "전투 함성",
            description : "반경 5m 내 모든 아군의 공격력을 8초 동안 1.3배로 증가.",
            cooldown    : 20f,
            effectValue : 1.3f,
            radius      : 5f,
            duration    : 8f,
            jobs        : new[] { UnitJob.Knight, UnitJob.ShieldBearer });

        // ── ⑱ 충격파 (전사) ─────────────────────────────────
        var shockwave = Make<ActiveShockwave>(db,
            id          : ActiveSkillId.Shockwave,
            fileName    : "Active_Shockwave",
            skillName   : "충격파",
            description : "전방 120도 부채꼴 범위의 모든 적에게 피해 + 강한 넉백.",
            cooldown    : 18f,
            effectValue : 1.5f,
            radius      : 4f,
            duration    : 0f,
            jobs        : new[] { UnitJob.Knight });
        shockwave.ConeAngleDegrees = 120f;
        shockwave.KnockbackMult    = 6f;
        EditorUtility.SetDirty(shockwave);

        // ── ⑲ 신속 연격 (궁수) ──────────────────────────────
        Make<ActiveSwiftStrike>(db,
            id          : ActiveSkillId.SwiftStrike,
            fileName    : "Active_SwiftStrike",
            skillName   : "신속 연격",
            description : "시전자와 소속 병사 전체의 공격속도를 6초 동안 2배로 증가.",
            cooldown    : 20f,
            effectValue : 2f,
            radius      : 0f,
            duration    : 6f,
            jobs        : new[] { UnitJob.Archer });

        // ── ⑳ 정예 소환 (법사) ──────────────────────────────
        var summonElite = Make<ActiveSummonElite>(db,
            id          : ActiveSkillId.SummonElite,
            fileName    : "Active_SummonElite",
            skillName   : "정예 소환",
            description : "시전자 스텟 70% 수준의 정예 병사 3기를 소환한다.",
            cooldown    : 35f,
            effectValue : 3f,
            radius      : 0f,
            duration    : 0f,
            jobs        : new[] { UnitJob.Mage });
        summonElite.ElitePoolKey = "Soldier";
        summonElite.StatRatio    = 0.7f;
        summonElite.SpawnRadius  = 1.5f;
        EditorUtility.SetDirty(summonElite);

        // ── 저장 ─────────────────────────────────────────────
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ActiveSkillCreator] 완료 — 액티브 {db.Entries.Count}종 생성, Database 갱신: {DBPath}");
        EditorGUIUtility.PingObject(db);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────

    /// <summary>SO 생성 또는 기존 파일 로드 후 공통 필드 설정. Database Entries 에 등록.</summary>
    static T Make<T>(
        ActiveSkillDatabase db,
        ActiveSkillId id,
        string fileName,
        string skillName,
        string description,
        float cooldown,
        float effectValue,
        float radius,
        float duration,
        UnitJob[] jobs) where T : ActiveSkillData
    {
        string path = $"{ActiveDir}/{fileName}.asset";
        var so = AssetDatabase.LoadAssetAtPath<T>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
        }

        so.SkillId        = id;
        so.SkillName      = skillName;
        so.Description    = description;
        so.Cooldown       = cooldown;
        so.EffectValue    = effectValue;
        so.EffectRadius   = radius;
        so.EffectDuration = duration;
        so.AllowedJobs    = jobs;

        EditorUtility.SetDirty(so);
        db.Entries.Add(so);
        return so;
    }
}
