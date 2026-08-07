#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ============================================================
//  EquipmentCreator.cs  [Editor Only]
//  장비 SO 20종 + EquipmentDatabase 자동 생성 도구.
//
//  사용법:
//    Unity 메뉴 → BattleGame → 데이터 생성 → 장비 전체 생성
//
//  생성 위치:
//    Assets/_project/Data/Equipments/  ← 개별 장비 SO
//    Assets/_project/EquipmentDatabase.asset  ← 기존 파일 갱신
//
//  장비 구성 (DESIGN_GrowthSystems.md 기준):
//    [A] 갑옷 계열  MaxHp + Defense        ×5등급 = 5개
//    [B] 무기 계열  Attack + AttackSpeed   ×5등급 = 5개
//    [C] 지휘 계열  SoldierCount + Cmd     ×5등급 = 5개
//    [D] 특수 장비  Rare 1 / Unique 2 / Epic 2  = 5개
//    ───────────────────────────────────────── 합계 20개
//
//  등급별 트리거 규칙:
//    Normal / Uncommon / Rare → 스탯 절대값만
//    Unique / Epic            → 스탯 + OnAttack / OnHit 조건부 효과
// ============================================================

public static class EquipmentCreator
{
    const string DataRoot = "Assets/_project/Data";
    const string EquipDir = "Assets/_project/Data/Equipments";
    const string DBPath   = "Assets/Resources/EquipmentDatabase.asset";

    [MenuItem(ProjectKMenu.Data + "장비", priority = ProjectKMenu.DataPrio + 17)]
    public static void CreateAllEquipments()
    {
        // ── 폴더 준비 ─────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(DataRoot))
            AssetDatabase.CreateFolder("Assets/_project", "Data");
        if (!AssetDatabase.IsValidFolder(EquipDir))
            AssetDatabase.CreateFolder(DataRoot, "Equipments");

        // ── 아이콘 먼저 생성 ──────────────────────────────────
        EquipmentIconGenerator.GenerateEquipmentIcons();
        AssetDatabase.Refresh();

        // ── Database 로드 ─────────────────────────────────────
        var db = AssetDatabase.LoadAssetAtPath<EquipmentDatabase>(DBPath);
        if (db == null)
        {
            Debug.LogError($"[EquipmentCreator] EquipmentDatabase 없음: {DBPath}");
            return;
        }
        db.Equipments.Clear();

        // ══════════════════════════════════════════════════════
        // [A] 갑옷 계열 — MaxHp + Defense  (Normal ~ Epic)
        // ══════════════════════════════════════════════════════

        Make(db, "equip_armor_normal",
            "낡은 철판", UnitGrade.Normal, itemLv: 1, stone: 1, gold: 100,
            "낡고 녹슨 철판. 미약하지만 몸을 지켜준다.",
            S(StatType.MaxHp,   50f),
            S(StatType.Defense,  0.02f));

        Make(db, "equip_armor_uncommon",
            "철제 흉갑", UnitGrade.Uncommon, itemLv: 2, stone: 2, gold: 200,
            "견고하게 단조된 철제 흉갑.",
            S(StatType.MaxHp,  110f),
            S(StatType.Defense,  0.04f));

        Make(db, "equip_armor_rare",
            "강철 흉갑", UnitGrade.Rare, itemLv: 3, stone: 3, gold: 400,
            "숙련된 대장장이가 만든 강철 흉갑.",
            S(StatType.MaxHp,  220f),
            S(StatType.Defense,  0.07f));

        Make(db, "equip_armor_unique",
            "황금 흉갑", UnitGrade.Unique, itemLv: 4, stone: 5, gold: 800,
            "황금으로 장식된 귀족의 흉갑. 수호의 기운이 깃들어 있다.",
            S(StatType.MaxHp,  380f),
            S(StatType.Defense,  0.10f));

        Make(db, "equip_armor_epic",
            "신성한 갑옷", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "신들이 축복한 전설의 갑옷. 착용자를 죽음으로부터 지켜준다.",
            S(StatType.MaxHp,  600f),
            S(StatType.Defense,  0.14f));

        // ══════════════════════════════════════════════════════
        // [B] 무기 계열 — Attack + AttackSpeed  (Normal ~ Epic)
        // ══════════════════════════════════════════════════════

        Make(db, "equip_sword_normal",
            "낡은 단검", UnitGrade.Normal, itemLv: 1, stone: 1, gold: 100,
            "날이 무뎌진 낡은 단검.",
            S(StatType.Attack,       15f),
            S(StatType.AttackSpeed,   0.10f));

        Make(db, "equip_sword_uncommon",
            "강철 단검", UnitGrade.Uncommon, itemLv: 2, stone: 2, gold: 200,
            "잘 벼려진 강철 단검. 빠른 연격에 적합하다.",
            S(StatType.Attack,       30f),
            S(StatType.AttackSpeed,   0.18f));

        Make(db, "equip_sword_rare",
            "세공된 장검", UnitGrade.Rare, itemLv: 3, stone: 3, gold: 400,
            "보석이 박힌 세공된 장검. 타격감과 속도를 겸비했다.",
            S(StatType.Attack,       60f),
            S(StatType.AttackSpeed,   0.28f));

        Make(db, "equip_sword_unique",
            "명품 장검", UnitGrade.Unique, itemLv: 4, stone: 5, gold: 800,
            "명장이 평생을 바쳐 만든 검. 날카로움이 극에 달했다.",
            S(StatType.Attack,      105f),
            S(StatType.AttackSpeed,   0.40f));

        Make(db, "equip_sword_epic",
            "전설의 검", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "영웅들의 피를 마신 전설의 검. 들어 올리는 것만으로도 전의가 타오른다.",
            S(StatType.Attack,      165f),
            S(StatType.AttackSpeed,   0.55f));

        // ══════════════════════════════════════════════════════
        // [C] 지휘 계열 — SoldierCount + CommandPower  (Normal ~ Epic)
        // ══════════════════════════════════════════════════════

        Make(db, "equip_cmd_normal",
            "낡은 지휘봉", UnitGrade.Normal, itemLv: 1, stone: 1, gold: 100,
            "오래된 나무 지휘봉. 병사들이 마지못해 따른다.",
            S(StatType.SoldierCount,  1f),
            S(StatType.CommandPower,  5f));

        Make(db, "equip_cmd_uncommon",
            "청동 지휘봉", UnitGrade.Uncommon, itemLv: 2, stone: 2, gold: 200,
            "청동으로 만든 지휘봉. 병사들의 사기를 높인다.",
            S(StatType.SoldierCount,  2f),
            S(StatType.CommandPower, 10f));

        Make(db, "equip_cmd_rare",
            "황금 지휘봉", UnitGrade.Rare, itemLv: 3, stone: 3, gold: 400,
            "황금빛 지휘봉. 소지자의 카리스마를 크게 높여준다.",
            S(StatType.SoldierCount,  3f),
            S(StatType.CommandPower, 18f));

        Make(db, "equip_cmd_unique",
            "영주의 깃발", UnitGrade.Unique, itemLv: 4, stone: 5, gold: 800,
            "영주의 문장이 새겨진 깃발. 병사들이 충성을 다해 따른다.",
            S(StatType.SoldierCount,  4f),
            S(StatType.CommandPower, 28f));

        Make(db, "equip_cmd_epic",
            "왕의 깃발", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "왕실의 깃발. 이 깃발 아래 모인 병사는 두려움을 잊는다.",
            S(StatType.SoldierCount,  6f),
            S(StatType.CommandPower, 42f));

        // ══════════════════════════════════════════════════════
        // [D] 특수 장비 — 해당 등급에만 존재
        // ══════════════════════════════════════════════════════

        // Rare: 크리티컬 + 사거리 + 공격력 복합 (트리거 없음)
        Make(db, "equip_sniper_rare",
            "저격수의 장갑", UnitGrade.Rare, itemLv: 3, stone: 4, gold: 500,
            "원거리 저격에 최적화된 장갑. 급소를 노리는 법을 알고 있다.",
            S(StatType.Attack,      40f),
            S(StatType.CritChance,   0.12f),
            S(StatType.AttackRange,  0.4f));

        // Unique: 흡혈 반지 — 공격 시 30% 확률로 준 피해의 10% 체력 회복
        var vamp = Make(db, "equip_vamp_unique",
            "흡혈 반지", UnitGrade.Unique, itemLv: 4, stone: 6, gold: 1000,
            "피를 빨아들이는 저주받은 반지. 공격 시 30% 확률로 준 피해의 10%를 체력으로 흡수한다.",
            S(StatType.Attack, 80f));
        vamp.TriggerType        = EquipmentTrigger.OnAttack;
        vamp.TriggerStat        = StatType.MaxHp;
        vamp.TriggerValue       = 0.10f;          // 준 피해량의 10%
        vamp.TriggerIsPercent   = true;
        vamp.TriggerPercentBase = EquipTriggerPercentBase.OfDamage;
        vamp.TriggerChance      = 0.30f;
        vamp.TriggerDuration    = 0f;             // 즉시 적용
        EditorUtility.SetDirty(vamp);

        // Unique: 복수의 갑옷 — 피격 시 40% 확률로 공격력 +250 (2초)
        var revenge = Make(db, "equip_revenge_unique",
            "복수의 갑옷", UnitGrade.Unique, itemLv: 4, stone: 6, gold: 1000,
            "피를 보면 분노가 끓어오르는 갑옷. 피격될 때마다 전투력이 폭발한다.",
            S(StatType.Defense, 0.12f));
        revenge.TriggerType     = EquipmentTrigger.OnHit;
        revenge.TriggerStat     = StatType.Attack;
        revenge.TriggerValue    = 250f;
        revenge.TriggerChance   = 0.40f;
        revenge.TriggerDuration = 2f;
        EditorUtility.SetDirty(revenge);

        // Epic: 광기의 검 — 공격 시 25% 확률로 공격력 +400 (2초)
        var berserk = Make(db, "equip_berserk_epic",
            "광기의 검", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "광기가 깃든 검. 피를 볼수록 더욱 날카로워진다.",
            S(StatType.Attack,      130f),
            S(StatType.AttackSpeed,   0.40f));
        berserk.TriggerType     = EquipmentTrigger.OnAttack;
        berserk.TriggerStat     = StatType.Attack;
        berserk.TriggerValue    = 400f;
        berserk.TriggerChance   = 0.25f;
        berserk.TriggerDuration = 2f;
        EditorUtility.SetDirty(berserk);

        // Epic: 불사의 갑옷 — 피격 시 20% 확률로 최대 체력의 1% 즉시 회복
        var immortal = Make(db, "equip_immortal_epic",
            "불사의 갑옷", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "죽음을 거부하는 자의 갑옷. 피격 시 20% 확률로 최대 체력의 1%를 즉시 회복한다.",
            S(StatType.MaxHp,   400f),
            S(StatType.Defense,   0.10f));
        immortal.TriggerType        = EquipmentTrigger.OnHit;
        immortal.TriggerStat        = StatType.MaxHp;
        immortal.TriggerValue       = 0.01f;      // 최대 체력의 1%
        immortal.TriggerIsPercent   = true;
        immortal.TriggerPercentBase = EquipTriggerPercentBase.OfMaxHp;
        immortal.TriggerChance      = 0.20f;
        immortal.TriggerDuration    = 0f;         // 즉시 적용
        EditorUtility.SetDirty(immortal);

        // ── 아이콘 자동 할당 ──────────────────────────────────
        foreach (var equip in db.Equipments)
        {
            string iconPath = EquipmentIconGenerator.GetIconPath(equip.EquipmentId);
            if (iconPath == null) continue;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite != null)
            {
                equip.Icon = sprite;
                EditorUtility.SetDirty(equip);
            }
        }

        // ── 저장 ─────────────────────────────────────────────
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EquipmentCreator] 완료 — 장비 {db.Equipments.Count}종 생성, Database 갱신: {DBPath}");
        EditorGUIUtility.PingObject(db);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    static EquipmentData Make(
        EquipmentDatabase db,
        string id, string equipName, UnitGrade grade,
        int itemLv, int stone, int gold,
        string description,
        params EquipStatEntry[] stats)
    {
        string path = $"{EquipDir}/{id}.asset";
        var so = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<EquipmentData>();
            AssetDatabase.CreateAsset(so, path);
        }

        so.EquipmentId          = id;
        so.EquipmentName        = equipName;
        so.Grade                = grade;
        so.Description          = description;
        so.ItemLevel            = itemLv;
        so.BaseEnhanceStoneCost = stone;
        so.BaseGoldCost         = gold;
        so.ValuePerLevel        = 0.10f;
        so.TriggerType          = EquipmentTrigger.None;
        so.TriggerValue         = 0f;
        so.TriggerIsPercent     = false;
        so.TriggerPercentBase   = EquipTriggerPercentBase.Absolute;
        so.TriggerChance        = 0f;
        so.TriggerDuration      = 0f;
        so.StatEntries          = new List<EquipStatEntry>(stats);

        EditorUtility.SetDirty(so);
        db.Equipments.Add(so);
        return so;
    }

    static EquipStatEntry S(StatType type, float delta)
        => new EquipStatEntry { Stat = type, Delta = delta };
}
#endif
