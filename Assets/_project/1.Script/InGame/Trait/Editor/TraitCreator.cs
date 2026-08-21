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

    [MenuItem(ProjectKMenu.Data + "특성", priority = ProjectKMenu.DataPrio + 13)]
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
        // 스탯 전환 (from 스탯을 perUnit 단위로 세어 to 스탯을 rate 만큼 올린다)
        public (StatType from, float perUnit, StatType to, float rate)[] conv;
    }

    static Def[] BuildDefinitions() => new Def[]
    {
        new Def
        {
            type = TraitType.KnightCommand,
            name = "지휘관의 기질",
            // 설명에 수치를 적지 않는다 — 스탯 줄(BuildStatText)이 이미 말한다
            desc = "전장을 호령하는 기사의 타고난 통솔력.",
            job  = UnitJob.Knight,
            fx   = new[]
            {
                (StatType.MaxHp,       0.10f, true ),
                (StatType.SoldierCount, 5f,   false),
                (StatType.CommandPower, 5f,   false),
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
            type = TraitType.KnightMartyr,
            name = "순교",
            desc = "쓰러진 병사가 마지막으로 불타오른다. 병사가 사망한 자리에서 폭발이 일어나 " +
                   "반경 2 안의 적에게 장군 공격력의 80% 피해를 준다.",
            job  = UnitJob.Knight,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },
        new Def
        {
            type = TraitType.ArcherPrecision,
            name = "정밀 사수",
            desc = "숨을 죽이고 조준하는 궁수의 집중력.",
            job  = UnitJob.Archer,
            fx   = new[]
            {
                (StatType.Attack,      0.15f, true ),
                (StatType.AttackRange,  1f,   false),
                (StatType.AttackSpeed,  0.15f, false),
            },
        },
        // 퇴각 사격은 특성에서 뺐다 — 궁수 기본 행동이다 (RetreatFireTag).
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
            desc = "내면의 마력을 극한까지 끌어올린 마법사의 각성.",
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
            desc = "흔들리지 않는 방패병의 육체.",
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
            desc = "원정을 편성해 더 많은 장수를 전장에 내보낸다. 배치 슬롯은 최대 5칸까지 늘어난다.",
            job  = (UnitJob)255,  // 공통 — 직업 자동 배정 없음
            fx   = new[] { (StatType.GeneralSlotBonus, 1f, false) },
        },
        new Def
        {
            type = TraitType.CommonMassMobilize,
            name = "대규모 동원",
            desc = "머릿수로 밀어붙이는 총동원령. 배치 슬롯은 최대 5칸까지 늘어난다.",
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
            desc = "본국에서 병력을 추가로 보내온다. 배치 슬롯은 최대 5칸까지 늘어난다.",
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
            desc = "머릿수는 채웠지만 훈련이 부족하다. 배치 슬롯은 최대 5칸까지 늘어난다.",
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
            desc = "보급 마차를 늘려 장비를 더 챙겨 다닌다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.EquipSlotBonus, 1f, false) },
        },

        // ── 공통 성장형 특성 ─────────────────────────────────────
        new Def
        {
            type         = TraitType.CommonLateBloom,
            name         = "대기만성",
            desc         = "느리게 여무는 그릇. 스테이지를 클리어할 때마다 공격력과 최대 체력이 1%씩 오른다.",
            job          = (UnitJob)255,
            fx           = System.Array.Empty<(StatType, float, bool)>(),
            stackTrigger = PassiveTrigger.StageClear,
            maxStacks    = 30,   // 게임 최대 스테이지 수와 동일 — 사실상 런 내내 성장
            // ⚠ 스택당 5% 는 30스택에서 +150% 였다 — 특성 하나가 빌드를 통째로 이겼다.
            //   1% 로 내려 만렙 +30% 로 맞춘다. 다른 성장형 특성과 같은 눈금이다.
            stackFx      = new[]
            {
                (StatType.Attack, 0.01f, true),
                (StatType.MaxHp,  0.01f, true),
            },
        },

        // ── 스탯 전환 특성 ───────────────────────────────────────
        //  덧셈 옵션만으로는 "몰빵할 이유" 가 없어 빌드가 갈리지 않는다.
        //  한 스탯을 다른 스탯으로 환산해 특화에 초과 보상을 준다.
        //  수치는 "그 스탯에 특화하면 대략 +40~70%" 를 목표로 잡았다.
        new Def
        {
            type = TraitType.ConvHeavyArmor,
            name = "중갑",
            desc = "두꺼운 갑주 자체가 무기가 된다. 방어율 1%p마다 공격력이 1.5% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.Defense, 0.01f, StatType.Attack, 0.015f) },
        },
        new Def
        {
            type = TraitType.ConvTitan,
            name = "거인",
            desc = "거대한 몸집에서 나오는 완력. 최대 체력 1000마다 공격력이 6% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.MaxHp, 1000f, StatType.Attack, 0.06f) },
        },
        new Def
        {
            type = TraitType.ConvSwift,
            name = "속공",
            desc = "발이 빠른 만큼 손도 빠르다. 이동속도 1마다 공격속도가 12% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.MoveSpeed, 1f, StatType.AttackSpeed, 0.12f) },
        },
        new Def
        {
            type = TraitType.ConvSage,
            name = "현자",
            desc = "마력의 회전이 곧 파괴력이다. 스킬 쿨감 1%p마다 공격력이 2% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.SkillCooldownReduce, 0.01f, StatType.Attack, 0.02f) },
        },
        new Def
        {
            type = TraitType.ConvWarlord,
            name = "군단장",
            desc = "등 뒤의 병사가 많을수록 검이 무거워진다. 병사 1명마다 공격력이 3% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.SoldierCount, 1f, StatType.Attack, 0.03f) },
        },
        new Def
        {
            type = TraitType.ConvMarksman,
            name = "명사수",
            desc = "멀리 볼수록 정확히 꽂힌다. 공격 사거리 1마다 공격력이 6% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.AttackRange, 1f, StatType.Attack, 0.06f) },
        },
        new Def
        {
            type = TraitType.ConvBulwark,
            name = "육중",
            desc = "단단한 것은 곧 질기다. 방어율 1%p마다 최대 체력이 2% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.Defense, 0.01f, StatType.MaxHp, 0.02f) },
        },

        // ── 치명타 특성 ──────────────────────────────────────────
        //  치명 기여도 = 치명확률 × (치명배율 - 1).
        //  기존에는 치명배율을 올려주는 성장 옵션이 게임 전체에 패시브 1종뿐이라
        //  확률만 올려도 수익이 납작했다. 확률과 배율을 함께 공급해 곱이 살아나게 한다.
        new Def
        {
            type = TraitType.CritAssassin,
            name = "암살자의 눈",
            desc = "급소만 노리는 눈썰미. 치명타 확률과 배율이 함께 오른다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.CritChance, 0.12f, false),
                (StatType.CritDamage, 0.30f, false),
            },
        },
        new Def
        {
            type = TraitType.CritExecutioner,
            name = "처형인",
            desc = "한 번에 끝낸다. 치명타 배율이 크게 오르는 대신 평타가 무뎌진다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.CritDamage, 0.80f,  false),
                (StatType.Attack,    -0.12f,  true ),
            },
        },
        new Def
        {
            type = TraitType.ConvDeadeye,
            name = "필살",
            desc = "노림수가 쌓일수록 일격이 깊어진다. 치명타 확률 1%p마다 치명타 배율이 1.5% 오른다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
            conv = new[] { (StatType.CritChance, 0.01f, StatType.CritDamage, 0.015f) },
        },

        // ── 공격속도 특성 ────────────────────────────────────────
        new Def
        {
            type = TraitType.HasteFrenzy,
            name = "광란",
            desc = "정확함을 버리고 속도를 택한다. 공격속도가 크게 오르는 대신 공격력이 줄어든다.",
            job  = (UnitJob)255,
            fx   = new[]
            {
                (StatType.AttackSpeed,  0.40f, true),
                (StatType.Attack,      -0.25f, true),
            },
        },
        new Def
        {
            type = TraitType.HasteRend,
            name = "파쇄",
            desc = "때릴 때마다 갑주가 갈라진다. 공격이 적중할 때마다 대상 최대 체력의 2%를 " +
                   "추가로 깎는다. 보스에게는 33%만 적용되며, 추가 피해는 장군 공격력의 3배를 넘지 않는다.",
            job  = (UnitJob)255,
            fx   = System.Array.Empty<(StatType, float, bool)>(),
        },

        // ── 이벤트 전용 특성 (EventRewardHandler 가 부여) ────────
        //  SO 가 없으면 TraitDatabase.Get 이 null 을 돌려주고,
        //  특성 바에 회색 빈칸으로 뜨는 데다 TraitApplier 가 효과를 건너뛴다.
        //  → 이벤트로 얻은 특성이 아무 일도 하지 않는다. 반드시 여기 등록할 것.
        new Def
        {
            type = TraitType.Event_BattleWill,
            name = "전투 의지",
            desc = "구해준 부상병이 전한 각오가 부대에 번진다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.05f, true) },
        },
        new Def
        {
            type = TraitType.Event_PotionBuff,
            name = "활력의 묘약",
            desc = "약장수의 묘약이 몸에 잘 맞았다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.08f, true) },
        },
        new Def
        {
            type = TraitType.Event_PotionDebuff,
            name = "부작용",
            desc = "묘약의 부작용으로 몸이 무겁다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MoveSpeed, -0.08f, true) },
        },
        new Def
        {
            type = TraitType.Event_BloodPact,
            name = "피의 계약",
            desc = "제단에 피를 바쳐 힘을 얻었다. 그 대가는 자신의 생명력이었다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, -0.15f, true) },
        },
        new Def
        {
            type = TraitType.Event_AltarCurse,
            name = "제단의 저주",
            desc = "제단을 부수려다 저주를 뒤집어썼다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Defense, -0.10f, false) },
        },
        new Def
        {
            type = TraitType.Event_ExecutionMorale,
            name = "처형의 사기",
            desc = "첩자를 처형해 군율을 다시 세웠다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.08f, true) },
        },
        new Def
        {
            type = TraitType.Event_SpyInfo,
            name = "첩자 정보",
            desc = "첩자에게서 적진의 정보를 얻어냈다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.ExpGainBonus, 0.20f, false) },
        },
        new Def
        {
            type = TraitType.Event_VeteranHeritage,
            name = "노병의 유산",
            desc = "방랑 노병이 남긴 행군 요령.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MoveSpeed, 0.10f, true) },
        },

        // ── 직업 시너지 (자동 부여 — 상점·이벤트 비등장) ─────────
        new Def
        {
            type = TraitType.Synergy_VanguardCross,
            name = "선봉대",
            desc = "기사와 궁수가 함께 전진하며 화력을 맞물린다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.10f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_MagicShield,
            name = "마법 방패",
            desc = "기사의 방어선 뒤에서 법사가 마법을 펼친다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.05f, true), (StatType.MaxHp, 0.05f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_IronWallLine,
            name = "철벽진",
            desc = "기사와 방패병이 이중 방어선을 형성한다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.10f, true), (StatType.Defense, 0.05f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_BalancedHost,
            name = "균형의 군세",
            desc = "네 직업이 모두 갖춰진 완전한 군대.",
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
            desc = "다섯 기사의 창이 하나로 모인다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.30f, true), (StatType.MoveSpeed, 0.20f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_ArrowLegion,
            name = "화살의 군단",
            desc = "하늘을 가리는 화살의 비.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.30f, true), (StatType.AttackSpeed, 0.20f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_GreatMageCorp,
            name = "대법사단",
            desc = "다섯 법사의 마력이 공명한다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.35f, true), (StatType.SkillCooldownReduce, 0.15f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_Ironclad,
            name = "철옹성",
            desc = "무너지지 않는 철벽.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.40f, true), (StatType.Defense, 0.15f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_RangedFirenet,
            name = "원거리 화망",
            desc = "궁수와 법사가 원거리 화망을 펼친다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.20f, true), (StatType.AttackRange, 0.10f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_IronVanguard,
            name = "철벽 전위대",
            desc = "기사와 방패병이 함께 전선을 지킨다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.MaxHp, 0.30f, true), (StatType.Defense, 0.10f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_KnightSquad,
            name = "기사 소대",
            desc = "세 기사가 대열을 이룬다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.10f, true), (StatType.MoveSpeed, 0.06f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_ArcherSquad,
            name = "궁수 소대",
            desc = "세 궁수가 일제히 사격한다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.10f, true), (StatType.AttackSpeed, 0.06f, true) },
        },
        new Def
        {
            type = TraitType.Synergy_MageSquad,
            name = "법사 소대",
            desc = "세 법사가 마력을 모은다.",
            job  = (UnitJob)255,
            fx   = new[] { (StatType.Attack, 0.12f, true), (StatType.SkillCooldownReduce, 0.05f, false) },
        },
        new Def
        {
            type = TraitType.Synergy_ShieldSquad,
            name = "방패병 소대",
            desc = "세 방패병이 방패를 맞댄다.",
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
        (TraitType.KnightMartyr,     "Assets/_project/3.Textures/Icons/Traits/trait_knight_martyr.png"),
        (TraitType.ArcherPrecision,  "Assets/_project/3.Textures/Icons/Traits/trait_archer_precision.png"),
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
        (TraitType.CommonLateBloom,      "Assets/_project/3.Textures/Icons/Traits/trait_common_late_bloom.png"),
        // 스탯 전환
        (TraitType.ConvHeavyArmor,   "Assets/_project/3.Textures/Icons/Traits/trait_conv_heavy_armor.png"),
        (TraitType.ConvTitan,        "Assets/_project/3.Textures/Icons/Traits/trait_conv_titan.png"),
        (TraitType.ConvSwift,        "Assets/_project/3.Textures/Icons/Traits/trait_conv_swift.png"),
        (TraitType.ConvSage,         "Assets/_project/3.Textures/Icons/Traits/trait_conv_sage.png"),
        (TraitType.ConvWarlord,      "Assets/_project/3.Textures/Icons/Traits/trait_conv_warlord.png"),
        (TraitType.ConvMarksman,     "Assets/_project/3.Textures/Icons/Traits/trait_conv_marksman.png"),
        (TraitType.ConvBulwark,      "Assets/_project/3.Textures/Icons/Traits/trait_conv_bulwark.png"),
        // 치명타
        (TraitType.CritAssassin,     "Assets/_project/3.Textures/Icons/Traits/trait_crit_assassin.png"),
        (TraitType.CritExecutioner,  "Assets/_project/3.Textures/Icons/Traits/trait_crit_executioner.png"),
        (TraitType.ConvDeadeye,      "Assets/_project/3.Textures/Icons/Traits/trait_crit_deadeye.png"),
        // 공격속도
        (TraitType.HasteFrenzy,      "Assets/_project/3.Textures/Icons/Traits/trait_haste_frenzy.png"),
        (TraitType.HasteRend,        "Assets/_project/3.Textures/Icons/Traits/trait_haste_rend.png"),
        // 이벤트 전용
        (TraitType.Event_BattleWill,      "Assets/_project/3.Textures/Icons/Traits/trait_event_battle_will.png"),
        (TraitType.Event_PotionBuff,      "Assets/_project/3.Textures/Icons/Traits/trait_event_potion_buff.png"),
        (TraitType.Event_PotionDebuff,    "Assets/_project/3.Textures/Icons/Traits/trait_event_potion_debuff.png"),
        (TraitType.Event_BloodPact,       "Assets/_project/3.Textures/Icons/Traits/trait_event_blood_pact.png"),
        (TraitType.Event_AltarCurse,      "Assets/_project/3.Textures/Icons/Traits/trait_event_altar_curse.png"),
        (TraitType.Event_ExecutionMorale, "Assets/_project/3.Textures/Icons/Traits/trait_event_execution_morale.png"),
        (TraitType.Event_SpyInfo,         "Assets/_project/3.Textures/Icons/Traits/trait_event_spy_info.png"),
        (TraitType.Event_VeteranHeritage, "Assets/_project/3.Textures/Icons/Traits/trait_event_veteran_heritage.png"),
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

        var conv = def.conv ?? System.Array.Empty<(StatType, float, StatType, float)>();
        so.Conversions = new TraitData.StatConversion[conv.Length];
        for (int i = 0; i < conv.Length; i++)
        {
            so.Conversions[i] = new TraitData.StatConversion
            {
                From    = conv[i].from,
                PerUnit = conv[i].perUnit,
                To      = conv[i].to,
                Rate    = conv[i].rate,
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
