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
//  장비 구성:
//    [A] 갑옷 계열  MaxHp + Defense          ×5등급 = 5개
//    [B] 검   계열  Attack + CritDamage      ×5등급 = 5개
//    [C] 단검 계열  AttackSpeed + CritChance ×5등급 = 5개
//    [D] 지휘 계열  SoldierCount + Cmd       ×5등급 = 5개
//    [E] 특수 장비  트리거 없음 (Rare 1 / Epic 1)    = 2개
//    [F] 특수 장비  트리거 Epic (스탯 버프·회복)       = 7개
//    [G] 특수 장비  트리거 Epic (스켈레톤 소환)       = 3개
//    [H] 특수 장비  트리거 Epic (병사 전원 버프)      = 3개
//    ───────────────────────────────────────── 합계 35개
//
//  ■ 무기가 두 계열로 갈라져 있다 (검 / 단검)
//    예전엔 무기 하나가 공격력과 공격속도를 함께 올렸다. DPS = 공격력 × 공격속도라
//    한 칸에서 두 배율이 곱해져 다른 계열의 세 배가 됐다.
//    지금은 축을 나눈다 —
//      검   = 크게 때린다  (공격력 + 치명타 피해)
//      단검 = 자주 때린다  (공격속도 + 치명타 확률)
//    둘 다 DPS 를 올리지만 한 칸 안에서 곱해지지 않는다. 직업 궁합도 갈린다:
//    기본 공격력이 큰 법사는 단검이, 공격속도가 빠른 기사는 검이 더 크게 붙는다.
//
//  ■ 트리거(특수 효과) 장비는 전부 Epic 이다
//    "가끔 터지는 한 줄" 은 등급을 읽는 재미의 정점이라 최상위에만 둔다.
//    Epic 안에서도 트리거 장비는 기본 스탯을 조금 낮추는 대신 발동 효과를 크게 준다.
//
//  등급별 트리거 규칙:
//    Normal ~ Unique → 스탯 절대값만
//    Epic            → 스탯만 있는 것 / 스탯 + 트리거인 것이 섞여 있다
//
//  ■ 수치 기준 — 네 계열이 같은 무게여야 한다
//    한 칸에 뭘 끼우든 비슷한 값이라야 "고민" 이 생긴다.
//    Epic 기준 (장수 Lv10·Epic 가정, 직업별 최소~최대):
//      갑옷 = 유효 체력 +74~99%
//      검   = 개인 DPS  +55~195%   (법사 최저 · 방패병 최고)
//      단검 = 개인 DPS  +46~131%   (기사 최저 · 법사 최고)
//      지휘 = 부대 DPS  +124~208%  (기사 최저 · 원거리 최고)
//    등급이 한 단계 오를 때마다 대략 두 배씩 커진다 (Normal ≈ +10%).
//
//    지휘 계열만 폭이 큰 것은 의도다 — 병사 수는 패시브(+5명)와 같은 저울에
//    올라가므로 등급값을 후하게 준다. 대신 부대 DPS 는 장군 하나가 아니라
//    부대 전체 합이라 단일 대상 화력·생존과 1:1 로 비교되는 값이 아니다.
//
//  ■ 스탯은 절대값 가산이다 — 의도된 설계다
//    슬롯이 두 칸뿐이라 한 칸의 무게가 커야 한다. 절대값이므로 기본 수치가
//    낮은 직업일수록 상승률이 크게 잡힌다 (방패병 공격력 +195% 등).
//    이건 버그가 아니라 "부족한 축을 장비로 메운다" 는 규칙이다.
//
//    ⚠ 다만 한 칸 안에서 곱해지는 조합은 피한다
//      공격력 × 공격속도처럼 곱으로 붙는 두 스탯을 한 장비에 같이 크게 주면
//      (1+a)(1+b) 로 터진다. 검·단검이 갈라진 이유다.
//
//    ⚠ 공격속도 절대값은 느린 직업에게 몇 배로 꽂힌다
//      법사 기본 공속 0.55, 기사 1.6 — 같은 +0.5 가 법사에겐 +90%, 기사에겐 +31%.
//      단검 계열의 상승폭이 직업마다 갈리는 이유이며, 그래서 값 자체를 작게 잡는다.
//
//    ⚠ 지휘력은 병사 스탯 배율에 선형으로 들어간다 (상한 없음)
//      병사 환산율 = 0.2 + 지휘력 × 0.01 (SoldierRuntimeBridge.StatRatio).
//      지휘력 +20 이면 병사 전원이 장군 스탯의 20%p 를 더 받는다.
//
//    ⚠ 트리거 수치도 절대값이다 — 상수로 크게 박지 말 것
//      예전 광기의 검은 발동 시 공격력 +400 이었다. 장수 기본 공격력이
//      65~250 인 구간에서 그 한 줄이 본체 스탯보다 몇 배 셌다.
//      지금 기준은 "발동 중에는 그 등급 무기 한 자루를 더 든 정도" 다.
//
//    ⚠ 발동 빈도는 확률 하나로만 잡는다 (재발동 대기를 두지 않는다)
//      확률과 대기를 같이 걸면 실제 주기가 둘의 곱이 되어 표시된 확률이 거짓말이 된다.
//
//      트리거는 전부 '이 부대' 기준이다 — 공격·피격·스킬은 장군 본인,
//      처치·병사 사망은 그 장군의 부대(병사·소환수 포함). 남의 부대 일로는 안 터진다.
//      다만 판정이 프레임 단위라, 피격처럼 초당 여러 번 참이 되는 조건은
//      확률을 크게 잡으면 사실상 상시 발동이 된다.
//
//    ⚠ 표에 적는 값은 최종 수치가 아니다
//      finalDelta = delta × (1 + (ItemLevel - 1 + 강화) × ValuePerLevel) 이라
//      ItemLevel 5 인 Epic 은 강화 0 에서도 이미 ×1.4 로 들어간다.
//
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
        // [B] 검 계열 — Attack + CritDamage  (크게 때린다)
        // ══════════════════════════════════════════════════════
        //  공격력이 주력, 치명타 피해가 양념이다. 치명타 확률은 단검이 담당하므로
        //  검만 끼면 치명타 피해가 놀고, 단검만 끼면 터뜨릴 원판이 작다 — 둘을 섞을 이유.

        Make(db, "equip_sword_normal",
            "무딘 검", UnitGrade.Normal, itemLv: 1, stone: 1, gold: 100,
            "날이 무뎌진 낡은 검. 그래도 없는 것보단 낫다.",
            S(StatType.Attack,      10f),
            S(StatType.CritDamage,   0.05f));

        Make(db, "equip_sword_uncommon",
            "강철 검", UnitGrade.Uncommon, itemLv: 2, stone: 2, gold: 200,
            "잘 벼려진 강철 검. 무게가 손에 익는다.",
            S(StatType.Attack,      15f),
            S(StatType.CritDamage,   0.10f));

        Make(db, "equip_sword_rare",
            "세공된 장검", UnitGrade.Rare, itemLv: 3, stone: 3, gold: 400,
            "보석이 박힌 세공된 장검. 급소를 파고들면 뼈까지 갈라놓는다.",
            S(StatType.Attack,      30f),
            S(StatType.CritDamage,   0.15f));

        Make(db, "equip_sword_unique",
            "명품 장검", UnitGrade.Unique, itemLv: 4, stone: 5, gold: 800,
            "명장이 평생을 바쳐 만든 검. 날카로움이 극에 달했다.",
            S(StatType.Attack,      55f),
            S(StatType.CritDamage,   0.25f));

        Make(db, "equip_sword_epic",
            "전설의 검", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "영웅들의 피를 마신 전설의 검. 들어 올리는 것만으로도 전의가 타오른다.",
            S(StatType.Attack,      85f),
            S(StatType.CritDamage,   0.40f));

        // ══════════════════════════════════════════════════════
        // [C] 단검 계열 — AttackSpeed + CritChance  (자주 때린다)
        // ══════════════════════════════════════════════════════
        //  ⚠ 공격속도는 절대 가산이다 — 값을 크게 잡지 말 것
        //    기본 공속이 0.55 인 법사에게 +0.5 는 +90%, 1.6 인 기사에게는 +31% 다.
        //    같은 장비가 직업에 따라 세 배 다르게 붙으므로 폭을 좁게 유지한다.

        Make(db, "equip_dagger_normal",
            "녹슨 단검", UnitGrade.Normal, itemLv: 1, stone: 1, gold: 100,
            "손에 익은 녹슨 단검. 가벼워서 손이 빠르다.",
            S(StatType.AttackSpeed,  0.06f),
            S(StatType.CritChance,   0.03f));

        Make(db, "equip_dagger_uncommon",
            "강철 단검", UnitGrade.Uncommon, itemLv: 2, stone: 2, gold: 200,
            "균형이 잘 잡힌 강철 단검. 빠른 연격에 적합하다.",
            S(StatType.AttackSpeed,  0.11f),
            S(StatType.CritChance,   0.05f));

        Make(db, "equip_dagger_rare",
            "쌍날 단검", UnitGrade.Rare, itemLv: 3, stone: 3, gold: 400,
            "양쪽에 날을 세운 단검. 스치기만 해도 상처가 벌어진다.",
            S(StatType.AttackSpeed,  0.18f),
            S(StatType.CritChance,   0.08f));

        Make(db, "equip_dagger_unique",
            "명인의 단검", UnitGrade.Unique, itemLv: 4, stone: 5, gold: 800,
            "암살자의 손을 거쳐 온 단검. 급소를 스스로 찾아간다.",
            S(StatType.AttackSpeed,  0.26f),
            S(StatType.CritChance,   0.12f));

        Make(db, "equip_dagger_epic",
            "그림자 단검", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "그림자에서 벼려낸 단검. 휘두른 뒤에야 베인 것을 안다.",
            S(StatType.AttackSpeed,  0.36f),
            S(StatType.CritChance,   0.16f));

        // ══════════════════════════════════════════════════════
        // [D] 지휘 계열 — SoldierCount + CommandPower  (부대를 키운다)
        // ══════════════════════════════════════════════════════
        //  ⚠ 병사 수는 패시브·유물과 같은 무대에서 비교된다
        //    병사를 5명씩 늘리는 패시브가 있다. Epic 장비가 그보다 적게 주면
        //    "최고 등급인데 패시브 한 줄보다 못한" 칸이 된다 — 병사 수는 후하게 준다.
        //
        //  ⚠ 지휘력은 병사 스탯 배율에 선형으로 들어간다 (상한 없음)
        //    환산율 = 0.2 + 지휘력 × 0.01. 예전 왕의 깃발(+42)은 그것만으로 병사
        //    스탯을 두 배로 만들었다. 병사 수와 달리 이쪽은 조심해서 올린다.

        Make(db, "equip_cmd_normal",
            "낡은 지휘봉", UnitGrade.Normal, itemLv: 1, stone: 1, gold: 100,
            "오래된 나무 지휘봉. 병사들이 마지못해 따른다.",
            S(StatType.SoldierCount,  1f),
            S(StatType.CommandPower,  2f));

        Make(db, "equip_cmd_uncommon",
            "청동 지휘봉", UnitGrade.Uncommon, itemLv: 2, stone: 2, gold: 200,
            "청동으로 만든 지휘봉. 병사들의 사기를 높인다.",
            S(StatType.SoldierCount,  2f),
            S(StatType.CommandPower,  6f));

        Make(db, "equip_cmd_rare",
            "황금 지휘봉", UnitGrade.Rare, itemLv: 3, stone: 3, gold: 400,
            "황금빛 지휘봉. 소지자의 카리스마를 크게 높여준다.",
            S(StatType.SoldierCount,  3f),
            S(StatType.CommandPower,  9f));

        Make(db, "equip_cmd_unique",
            "영주의 깃발", UnitGrade.Unique, itemLv: 4, stone: 5, gold: 800,
            "영주의 문장이 새겨진 깃발. 병사들이 충성을 다해 따른다.",
            S(StatType.SoldierCount,  4f),
            S(StatType.CommandPower, 12f));

        Make(db, "equip_cmd_epic",
            "왕의 깃발", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "왕실의 깃발. 이 깃발 아래 모인 병사는 두려움을 잊는다.",
            S(StatType.SoldierCount,  6f),
            S(StatType.CommandPower, 16f));

        // ══════════════════════════════════════════════════════
        // [E] 특수 장비 — 트리거 없음
        // ══════════════════════════════════════════════════════

        // Rare: 크리티컬 + 사거리 + 공격력 복합
        Make(db, "equip_sniper_rare",
            "저격수의 장갑", UnitGrade.Rare, itemLv: 3, stone: 4, gold: 500,
            "원거리 저격에 최적화된 장갑. 급소를 노리는 법을 알고 있다.",
            S(StatType.Attack,      20f),
            S(StatType.CritChance,   0.10f),
            S(StatType.AttackRange,  0.4f));

        // Epic: 스킬 쿨다운 전용
        //  ⚠ 쿨감은 출처끼리 곱연산으로 겹치고 상한이 0.8 이다 (GameplayConfig)
        //    유물·특성과 겹쳐도 80% 를 넘지 않으므로 한 자리에 크게 몰아줘도 된다.
        Make(db, "equip_chrono_epic",
            "시간의 왕관", UnitGrade.Epic, itemLv: 5, stone: 8, gold: 1500,
            "시간의 흐름을 비트는 왕관. 방금 쓴 힘이 벌써 다시 차오른다.",
            S(StatType.SkillCooldownReduce, 0.20f),
            S(StatType.Attack,             30f));

        // ══════════════════════════════════════════════════════
        // [F] 특수 장비 — 트리거 (전부 Epic)
        // ══════════════════════════════════════════════════════
        //  기본 스탯은 같은 등급의 일반 장비보다 낮게, 대신 발동 효과를 크게 준다.
        //  발동 수치 기준: "발동 중에는 그 등급 무기 한 자루를 더 든 정도".

        // OnAttack — 흡혈
        var vamp = Make(db, "equip_vamp_epic",
            "흡혈의 반지", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "피를 빨아들이는 저주받은 반지. 공격할 때마다 남의 피로 제 상처를 메운다.",
            S(StatType.Attack, 55f));
        vamp.TriggerType        = EquipmentTrigger.OnAttack;
        vamp.TriggerStat        = StatType.MaxHp;      // MaxHp = 즉시 회복 (CombatTriggerSystem)
        vamp.TriggerValue       = 0.15f;               // 준 피해량의 15%
        vamp.TriggerIsPercent   = true;
        vamp.TriggerPercentBase = EquipTriggerPercentBase.OfDamage;
        vamp.TriggerChance      = 0.35f;
        vamp.TriggerDuration    = 0f;                  // 즉시 적용
        EditorUtility.SetDirty(vamp);

        // OnHit — 맞을수록 세진다
        var revenge = Make(db, "equip_revenge_epic",
            "복수의 갑옷", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "피를 보면 분노가 끓어오르는 갑옷. 피격될 때마다 전투력이 폭발한다.",
            S(StatType.Defense, 0.16f),
            S(StatType.MaxHp, 250f));
        revenge.TriggerType     = EquipmentTrigger.OnHit;
        revenge.TriggerStat     = StatType.Attack;
        revenge.TriggerValue    = 150f;
        revenge.TriggerChance   = 0.40f;
        revenge.TriggerDuration = 2f;
        EditorUtility.SetDirty(revenge);

        // OnAttack — 때릴수록 세진다 (검 계열 트리거판)
        var berserk = Make(db, "equip_berserk_epic",
            "광기의 검", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "광기가 깃든 검. 피를 볼수록 더욱 날카로워진다.",
            S(StatType.Attack,     60f),
            S(StatType.CritDamage,  0.25f));
        berserk.TriggerType     = EquipmentTrigger.OnAttack;
        berserk.TriggerStat     = StatType.Attack;
        berserk.TriggerValue    = 150f;
        berserk.TriggerChance   = 0.25f;
        berserk.TriggerDuration = 2f;
        EditorUtility.SetDirty(berserk);

        // OnHit — 버티는 쪽
        var immortal = Make(db, "equip_immortal_epic",
            "불사의 갑옷", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "죽음을 거부하는 자의 갑옷. 상처를 입는 순간 그 자리가 아물기 시작한다.",
            S(StatType.MaxHp,   450f),
            S(StatType.Defense,   0.12f));
        immortal.TriggerType        = EquipmentTrigger.OnHit;
        immortal.TriggerStat        = StatType.MaxHp;
        immortal.TriggerValue       = 0.02f;      // 최대 체력의 2%
        immortal.TriggerIsPercent   = true;
        immortal.TriggerPercentBase = EquipTriggerPercentBase.OfMaxHp;
        immortal.TriggerChance      = 0.25f;
        immortal.TriggerDuration    = 0f;         // 즉시 적용
        EditorUtility.SetDirty(immortal);

        // OnEnemyKill — 처치가 처치를 부른다
        var hunter = Make(db, "equip_hunter_epic",
            "사냥꾼의 장갑", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "사냥에 미친 자의 장갑. 하나를 눕히면 손이 다음 목을 찾는다.",
            S(StatType.Attack,     45f),
            S(StatType.CritChance,  0.10f));
        hunter.TriggerType     = EquipmentTrigger.OnEnemyKill;
        hunter.TriggerStat     = StatType.Attack;
        hunter.TriggerValue    = 120f;
        hunter.TriggerChance   = 0.40f;
        hunter.TriggerDuration = 3f;
        EditorUtility.SetDirty(hunter);

        // OnSoldierDeath — 병사의 죽음을 연료로 쓴다
        var requiem = Make(db, "equip_requiem_epic",
            "망자의 군기", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "쓰러진 병사의 이름이 새겨지는 군기. 부하의 죽음이 장군의 검을 무겁게 한다.",
            S(StatType.SoldierCount,  5f),
            S(StatType.CommandPower, 10f));
        requiem.TriggerType     = EquipmentTrigger.OnSoldierDeath;
        requiem.TriggerStat     = StatType.Attack;
        requiem.TriggerValue    = 100f;
        requiem.TriggerChance   = 0.50f;
        requiem.TriggerDuration = 4f;
        EditorUtility.SetDirty(requiem);

        // OnSkillUse — 스킬을 쓸수록 세진다
        var arcane = Make(db, "equip_arcane_epic",
            "주문술사의 오브", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "주문의 잔향이 맴도는 구슬. 한 번 시전할 때마다 다음 한 방이 무거워진다.",
            S(StatType.Attack,             40f),
            S(StatType.SkillCooldownReduce, 0.10f));
        arcane.TriggerType     = EquipmentTrigger.OnSkillUse;
        arcane.TriggerStat     = StatType.Attack;
        arcane.TriggerValue    = 180f;
        arcane.TriggerChance   = 0.50f;
        arcane.TriggerDuration = 3f;
        EditorUtility.SetDirty(arcane);

        // ══════════════════════════════════════════════════════
        // [G] 특수 장비 — 소환 (전부 Epic)
        // ══════════════════════════════════════════════════════
        //  ⚠ 기본 스탯은 일부러 얇다
        //    소환체는 그 자체로 부대 화력이자 방패다. 기본 옵션까지 같은 등급으로
        //    주면 한 칸이 두 칸 몫을 한다. "왜 이걸 끼나" 가 발동 효과로만 설명돼야 한다.

        // 병사 사망 시 — 그 자리를 스켈레톤이 메운다
        var necro = Make(db, "equip_necro_epic",
            "망자의 소환서", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "죽은 자를 다시 세우는 금서. 쓰러진 병사가 뼈만 남아 다시 일어선다.",
            S(StatType.SoldierCount, 2f),
            S(StatType.MaxHp,      150f));
        necro.TriggerType     = EquipmentTrigger.OnSoldierDeath;
        necro.EffectKind      = EquipTriggerEffect.Summon;
        necro.TriggerValue    = 1f;      // 1기
        necro.TriggerChance   = 0.45f;
        necro.SummonStatRatio = 0.40f;
        EditorUtility.SetDirty(necro);

        // 스킬 사용 시 — 쿨감과 묶으면 소환사 빌드가 된다
        var lich = Make(db, "equip_lich_epic",
            "해골 군주의 인장", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "해골 군주의 문장이 새겨진 인장. 주문을 외울 때마다 무덤이 열린다.",
            S(StatType.SkillCooldownReduce, 0.06f),
            S(StatType.Attack,             20f));
        lich.TriggerType     = EquipmentTrigger.OnSkillUse;
        lich.EffectKind      = EquipTriggerEffect.Summon;
        lich.TriggerValue    = 2f;      // 2기
        lich.TriggerChance   = 0.40f;    // 스킬 사용은 원래 드물어 그대로 둔다
        lich.SummonStatRatio = 0.40f;
        EditorUtility.SetDirty(lich);

        // 적 처치 시 — 이 부대(장군 본인 · 그 휘하 병사 · 소환수)가 잡은 적만 센다.
        //  UnitDeathDespawnSystem.ResolveKillCredit 가 마지막 일격의 주인을 되짚어
        //  그 장군에게만 이벤트를 넣는다. 남의 부대 전과로는 터지지 않는다.
        var executioner = Make(db, "equip_executioner_epic",
            "처형자의 낙인", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "처형대에서 건져 온 낙인. 장군의 손에 죽은 자는 그의 병사로 다시 선다.",
            S(StatType.Attack,     25f),
            S(StatType.CritDamage,  0.10f));
        executioner.TriggerType     = EquipmentTrigger.OnEnemyKill;
        executioner.EffectKind      = EquipTriggerEffect.Summon;
        executioner.TriggerValue    = 1f;      // 1기
        executioner.TriggerChance   = 0.25f;
        executioner.SummonStatRatio = 0.40f;
        EditorUtility.SetDirty(executioner);

        // ══════════════════════════════════════════════════════
        // [H] 특수 장비 — 병사 버프 (전부 Epic)
        // ══════════════════════════════════════════════════════
        //  ⚠ 병사 버프는 비율(RatioBuff)로 준다
        //    병사 스탯은 장군 스탯 × 환산율이라 장군마다·직업마다 크기가 다르다.
        //    절대값을 뿌리면 지휘력 낮은 부대엔 두 배가 되고 높은 부대엔 티도 안 난다.

        // 피격 시 — 장군이 맞을수록 부대가 달려든다
        var horn = Make(db, "equip_horn_epic",
            "군단의 뿔피리", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "전장 끝까지 울리는 뿔피리. 장군이 피를 흘릴 때 부대가 미친 듯이 달려든다.",
            S(StatType.CommandPower, 6f),
            S(StatType.MoveSpeed,    0.20f));
        horn.TriggerType     = EquipmentTrigger.OnHit;
        horn.EffectKind      = EquipTriggerEffect.RatioBuff;
        horn.TriggerTarget   = EquipTriggerTarget.Soldiers;
        horn.TriggerStat     = StatType.Attack;
        horn.TriggerValue    = 0.35f;    // ×1.35
        horn.TriggerChance   = 0.35f;
        horn.TriggerDuration = 4f;
        EditorUtility.SetDirty(horn);

        // 공격 시 — 자주 터지므로 확률을 낮게, 지속을 짧게
        var drum = Make(db, "equip_drum_epic",
            "선봉의 북", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "선봉대의 북. 북소리가 빨라지면 병사들의 창끝도 함께 빨라진다.",
            S(StatType.SoldierCount, 2f),
            S(StatType.AttackSpeed,  0.06f));
        drum.TriggerType     = EquipmentTrigger.OnAttack;
        drum.EffectKind      = EquipTriggerEffect.RatioBuff;
        drum.TriggerTarget   = EquipTriggerTarget.Soldiers;
        drum.TriggerStat     = StatType.AttackSpeed;
        drum.TriggerValue    = 0.30f;    // ×1.30
        drum.TriggerChance   = 0.10f;
        drum.TriggerDuration = 3f;
        EditorUtility.SetDirty(drum);

        // 피격 시 — 게임에서 유일한 병사 회복 수단
        //  MaxHp + IsPercent = 즉시 회복 (CombatTriggerSystem), 기준은 받는 병사 자신의 체력
        var oath = Make(db, "equip_oath_epic",
            "수호의 서약", UnitGrade.Epic, itemLv: 5, stone: 10, gold: 2000,
            "부하를 먼저 지키겠다는 서약. 장군이 맞을 때마다 병사들의 상처가 아문다.",
            S(StatType.Defense, 0.08f),
            S(StatType.MaxHp, 200f));
        oath.TriggerType        = EquipmentTrigger.OnHit;
        oath.EffectKind         = EquipTriggerEffect.StatBuff;
        oath.TriggerTarget      = EquipTriggerTarget.Soldiers;
        oath.TriggerStat        = StatType.MaxHp;      // MaxHp = 즉시 회복
        oath.TriggerValue       = 0.12f;               // 병사 최대 체력의 12%
        oath.TriggerIsPercent   = true;
        oath.TriggerPercentBase = EquipTriggerPercentBase.OfMaxHp;
        oath.TriggerChance      = 0.10f;
        oath.TriggerDuration    = 0f;                  // 즉시
        EditorUtility.SetDirty(oath);

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
        so.EffectKind           = EquipTriggerEffect.StatBuff;
        so.TriggerTarget        = EquipTriggerTarget.General;
        so.SummonPoolKey        = "Soldier";
        so.SummonStatRatio      = 0.4f;
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
