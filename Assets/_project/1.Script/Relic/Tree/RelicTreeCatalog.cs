using System.Collections.Generic;

// ============================================================
//  RelicTreeCatalog.cs
//  유물 테크트리 노드 표 — 69개. 이 파일이 트리의 정본이다.
//
//  ■ 읽는 법
//    Stat(...) = 스탯 노드, Sys(...) = 시스템 노드.
//    P(스탯, 값) = 비율(%) · A(스탯, 값) = 절대값(%p·명·포인트).
//    좌표는 (x, y), +Y 가 위다. 같은 x 면 dy ≥ 2, 같은 y 면 dx ≥ 2 (칸이 겹친다).
//
//  ■ 비용 (TierCost)
//    tier     0   1   2   3   4   5
//    CostBase 1   2   3   5   8  12    → 레벨업 비용 = CostBase × (레벨+1)
//    첫 레벨만 보면 2 / 3 / 5 / 8 / 12pt — 한 판 벌이(292pt)로 여러 갈래를 동시에 건드릴 수 있다.
//    만렙: t1 Lv5 = 30 · t2 Lv5 = 45 · t3 Lv5 = 75 · t4 Lv4 = 80 · t5 Lv3 = 72 · 특수 = CostBase
//
//    ⚠ 레벨을 늘리고 레벨당 수치를 낮췄다 (2026-08-25)
//      예전엔 t3 첫 레벨이 10pt, t4 가 20pt 라 한 번의 환생으로 노드 서너 개밖에
//      못 건드렸다. 트리는 "이번엔 어디를 넓힐까" 가 매 판 생겨야 재미가 있다.
//      만렙 총합은 거의 그대로 두고 계단만 잘게 쪼갠 것이다.
//
//  ■ 뿌리는 사방 4갈래뿐이다
//        위    공격력   (벼려진 칼날)
//        아래  체력     (굳은 살갗)
//        왼쪽  병사 수  (지휘의 깃발)
//        오른쪽 경험치  (성장의 증표)
//
//  ■ 병사 수 총량 (RelicTreeRules 참고)
//    정공으로 +30명 — 지휘의 깃발 5 · 대군의 진격 10 · 천군만마 8 · 왕의 검 3 · 요새의 화신 4
//    역분기로 -50명 — 고독한 장수 15 · 일기당천 15 · 무쌍 20
//    지휘력은 정공 +34 / 역분기 -15.
//
//    ⚠ 이 숫자를 바꿀 땐 RelicTreeRules 의 상수도 같이 고칠 것
//      MaxSoldierGain / MaxSoldierCut 은 밸런스 문서가 아니라 코드가 읽는 값이다.
// ============================================================

public static class RelicTreeCatalog
{
    /// <summary>티어별 기본 비용. 인덱스 = Tier.</summary>
    public static readonly int[] TierCost = { 1, 2, 3, 5, 8, 12 };

    static RelicNodeDef[] _all;
    static Dictionary<RelicNodeId, RelicNodeDef>       _byId;
    static Dictionary<RelicNodeId, List<RelicNodeDef>> _children;

    public static RelicNodeDef[] All { get { Ensure(); return _all; } }

    public static RelicNodeDef Get(RelicNodeId id) { Ensure(); return _byId[id]; }

    /// <summary>자식 목록. 없으면 빈 리스트 (트리 말단).</summary>
    public static List<RelicNodeDef> ChildrenOf(RelicNodeId id)
    {
        Ensure();
        return _children.TryGetValue(id, out var list) ? list : Empty;
    }

    static readonly List<RelicNodeDef> Empty = new();

    // ── 해금 · 시야 ───────────────────────────────────────────

    /// <summary>부모를 1레벨 이상 찍었으면 이 노드를 살 수 있다. 뿌리는 항상 열려 있다.</summary>
    public static bool IsUnlocked(RelicNodeId id, IReadOnlyDictionary<RelicNodeId, int> levels)
    {
        var def = Get(id);
        if (def.Parent == RelicNodeId.None) return true;
        return levels.TryGetValue(def.Parent, out int lv) && lv >= 1;
    }

    /// <summary>
    /// 화면에 그릴지 여부. 해금됐거나 이미 찍은 노드만 보인다.
    ///
    /// ⚠ 해금되지 않은 노드는 이름도 효과도 노출하지 않는다
    ///   "다음에 뭐가 나올지 모른다" 가 이 트리의 설계 의도다.
    /// </summary>
    public static bool IsVisible(RelicNodeId id, IReadOnlyDictionary<RelicNodeId, int> levels)
        => IsUnlocked(id, levels) || levels.ContainsKey(id);

    /// <summary>안개 너머 실루엣 — 보이는 노드의 자식만 "뭔가 있다"까지 알려 준다.</summary>
    public static bool IsSilhouette(RelicNodeId id, IReadOnlyDictionary<RelicNodeId, int> levels)
    {
        var def = Get(id);
        if (def.Parent == RelicNodeId.None) return false;
        return !IsVisible(id, levels) && IsVisible(def.Parent, levels);
    }

    /// <summary>전 노드를 만렙까지 올리는 총 환생 포인트.</summary>
    public static int GrandTotalCost()
    {
        Ensure();
        int sum = 0;
        foreach (var d in _all) sum += d.TotalCost;
        return sum;
    }

    /// <summary>
    /// 현재 레벨 기준으로 트리가 깎아낸 병사 수 (양수).
    /// 고립무원(LoneWolfBonus)이 이 값을 곱해 장수 보너스를 만든다.
    /// </summary>
    public static int SoldierCutTotal(IReadOnlyDictionary<RelicNodeId, int> levels)
    {
        Ensure();
        float cut = 0f;
        foreach (var d in _all)
        {
            if (!levels.TryGetValue(d.Id, out int lv) || lv <= 0) continue;
            lv = UnityEngine.Mathf.Min(lv, d.MaxLevel);   // 옛 세이브가 상한을 넘겨 저장돼 있을 수 있다
            foreach (var s in d.Stats)
                if (s.Stat == StatType.SoldierCount && s.PerLevel < 0f)
                    cut -= s.PerLevel * lv;
        }
        return UnityEngine.Mathf.RoundToInt(cut);
    }

    // ── 표 ────────────────────────────────────────────────────

    static void Ensure()
    {
        if (_all != null) return;
        var t = new List<RelicNodeDef>();
        _build = t;

        // ══ 뿌리 ════════════════════════════════════════════════
        Stat(RelicNodeId.N_Origin, RelicNodeId.None, "근원의 각인", RelicBranch.Root, 0,
            0, 0, 1, AbilityTarget.All, P(StatType.Attack, 0.05f), P(StatType.MaxHp, 0.05f));

        // ══════════════════════════════════════════════════════════
        //  공격 트렁크 (위) — 뿌리 직속은 '벼려진 칼날' 하나뿐이다
        // ══════════════════════════════════════════════════════════
        Stat(RelicNodeId.N_Blade, RelicNodeId.N_Origin, "벼려진 칼날", RelicBranch.Attack, 1,
            0, 2, 5, AbilityTarget.All, P(StatType.Attack, 0.04f));

        // ── 본선 (위) ────────────────────────────────────────────
        Stat(RelicNodeId.N_Destruction, RelicNodeId.N_Blade, "파괴의 의지", RelicBranch.Attack, 2,
            0, 4, 5, AbilityTarget.All, P(StatType.Attack, 0.05f));
        Stat(RelicNodeId.N_CritSense, RelicNodeId.N_Destruction, "치명의 감각", RelicBranch.Attack, 3,
            -1, 6, 5, AbilityTarget.All, A(StatType.CritChance, 0.025f));
        Stat(RelicNodeId.N_PierceLance, RelicNodeId.N_Destruction, "관통의 창", RelicBranch.Attack, 3,
            1, 6, 5, AbilityTarget.All, A(StatType.DefensePenetration, 0.03f));
        Stat(RelicNodeId.N_Executioner, RelicNodeId.N_CritSense, "처형자", RelicBranch.Attack, 4,
            -1, 8, 4, AbilityTarget.All, P(StatType.CritDamage, 0.12f));
        Stat(RelicNodeId.N_SlayerSeal, RelicNodeId.N_PierceLance, "학살자의 인장", RelicBranch.Attack, 4,
            1, 8, 4, AbilityTarget.All, P(StatType.Attack, 0.08f));
        Stat(RelicNodeId.N_OneStrike, RelicNodeId.N_SlayerSeal, "일격필살", RelicBranch.Attack, 5,
            0, 10, 1, AbilityTarget.All,
            P(StatType.CritDamage, 0.50f), A(StatType.CritChance, 0.10f));

        // ── 공속·쿨감 (오른쪽 위) ────────────────────────────────
        Stat(RelicNodeId.N_SwiftHand, RelicNodeId.N_Blade, "빠른 손놀림", RelicBranch.Attack, 2,
            3, 3, 5, AbilityTarget.All, P(StatType.AttackSpeed, 0.04f));
        Stat(RelicNodeId.N_ChainRhythm, RelicNodeId.N_SwiftHand, "연격의 리듬", RelicBranch.Attack, 3,
            2, 5, 5, AbilityTarget.All, P(StatType.AttackSpeed, 0.05f));
        Stat(RelicNodeId.N_TimeCompress, RelicNodeId.N_SwiftHand, "시간의 압축", RelicBranch.Attack, 3,
            4, 5, 5, AbilityTarget.All, A(StatType.SkillCooldownReduce, 0.025f));
        Stat(RelicNodeId.N_StormConcert, RelicNodeId.N_ChainRhythm, "폭풍의 연주", RelicBranch.Attack, 4,
            2, 7, 4, AbilityTarget.All, P(StatType.AttackSpeed, 0.08f));
        Stat(RelicNodeId.N_HawkEye, RelicNodeId.N_ChainRhythm, "매의 시야", RelicBranch.Attack, 4,
            4, 7, 4, AbilityTarget.All, P(StatType.AttackRange, 0.06f));
        Stat(RelicNodeId.N_SpellHaste, RelicNodeId.N_TimeCompress, "주문 가속", RelicBranch.Attack, 4,
            6, 6, 4, AbilityTarget.All, A(StatType.SkillCooldownReduce, 0.03f));
        Stat(RelicNodeId.N_EndlessChain, RelicNodeId.N_StormConcert, "무한 연쇄", RelicBranch.Attack, 5,
            2, 9, 1, AbilityTarget.All,
            A(StatType.SkillCooldownReduce, 0.12f), P(StatType.AttackSpeed, 0.10f));

        // ── 직업 (왼쪽 위) ───────────────────────────────────────
        // ⚠ 직업 노드는 전부 공격 트렁크다. 방패병만 예외로 체력 트렁크에 있다.
        //   직업 전용은 '그 직업만' 받는 대신 공통 노드보다 배율이 크다 —
        //   같은 값이면 부대 전체를 올리는 공통 노드를 안 고를 이유가 없다.
        Stat(RelicNodeId.N_GeneralSword, RelicNodeId.N_Blade, "장군의 검", RelicBranch.Attack, 2,
            -3, 3, 5, AbilityTarget.Unit_General, P(StatType.Attack, 0.06f));
        Stat(RelicNodeId.N_HeroToken, RelicNodeId.N_GeneralSword, "영웅의 증표", RelicBranch.Attack, 3,
            -3, 5, 5, AbilityTarget.Unit_General,
            P(StatType.Attack, 0.07f), P(StatType.MaxHp, 0.07f));
        Stat(RelicNodeId.N_HeroAwaken, RelicNodeId.N_HeroToken, "영웅의 각성", RelicBranch.Attack, 4,
            -2, 7, 4, AbilityTarget.Unit_General,
            P(StatType.Attack, 0.09f), A(StatType.CritChance, 0.04f));
        // 직업 허브 — 여기를 찍어야 3직업 가지가 열린다
        Stat(RelicNodeId.N_MartialLegacy, RelicNodeId.N_GeneralSword, "무예의 전승", RelicBranch.Attack, 2,
            -5, 4, 1, AbilityTarget.All, P(StatType.Attack, 0.05f));
        Stat(RelicNodeId.N_KnightOath, RelicNodeId.N_MartialLegacy, "기사단의 맹세", RelicBranch.Attack, 3,
            -7, 5, 5, AbilityTarget.Job_Knight,
            P(StatType.Attack, 0.08f), P(StatType.MaxHp, 0.06f));
        Stat(RelicNodeId.N_ArcherSpirit, RelicNodeId.N_MartialLegacy, "활의 정령", RelicBranch.Attack, 3,
            -6, 7, 5, AbilityTarget.Job_Archer,
            P(StatType.AttackSpeed, 0.08f), P(StatType.AttackRange, 0.06f));
        Stat(RelicNodeId.N_MageCrystal, RelicNodeId.N_MartialLegacy, "마력의 결정", RelicBranch.Attack, 3,
            -4, 7, 5, AbilityTarget.Job_Mage,
            P(StatType.Attack, 0.09f), A(StatType.SkillCooldownReduce, 0.04f));
        Stat(RelicNodeId.N_KingsBlade, RelicNodeId.N_KnightOath, "왕의 검", RelicBranch.Attack, 5,
            -8, 7, 3, AbilityTarget.Job_Knight,
            P(StatType.Attack, 0.12f), A(StatType.Defense, 0.04f), A(StatType.SoldierCount, 1f));
        Stat(RelicNodeId.N_StormBow, RelicNodeId.N_ArcherSpirit, "폭풍의 궁", RelicBranch.Attack, 5,
            -6, 9, 3, AbilityTarget.Job_Archer,
            P(StatType.Attack, 0.12f), A(StatType.CritChance, 0.05f), P(StatType.AttackRange, 0.06f));
        Stat(RelicNodeId.N_ArchmageLegacy, RelicNodeId.N_MageCrystal, "대마법사의 유산", RelicBranch.Attack, 5,
            -4, 9, 3, AbilityTarget.Job_Mage,
            P(StatType.Attack, 0.14f), A(StatType.SkillCooldownReduce, 0.05f), P(StatType.AttackRange, 0.06f));

        // ══════════════════════════════════════════════════════════
        //  체력 트렁크 (아래) — 뿌리 직속은 '굳은 살갗' 하나뿐이다
        // ══════════════════════════════════════════════════════════
        Stat(RelicNodeId.N_ToughSkin, RelicNodeId.N_Origin, "굳은 살갗", RelicBranch.Defense, 1,
            0, -2, 5, AbilityTarget.All, P(StatType.MaxHp, 0.05f));

        // ── 본선 (아래) ──────────────────────────────────────────
        Stat(RelicNodeId.N_IronWill, RelicNodeId.N_ToughSkin, "철의 의지", RelicBranch.Defense, 2,
            0, -4, 5, AbilityTarget.All, A(StatType.Defense, 0.02f));
        Stat(RelicNodeId.N_UnyieldingHeart, RelicNodeId.N_IronWill, "불굴의 심장", RelicBranch.Defense, 3,
            -1, -6, 5, AbilityTarget.All, P(StatType.MaxHp, 0.06f));
        Stat(RelicNodeId.N_RampartOath, RelicNodeId.N_IronWill, "성벽의 맹세", RelicBranch.Defense, 3,
            1, -6, 5, AbilityTarget.All, A(StatType.Defense, 0.025f));
        Stat(RelicNodeId.N_Regeneration, RelicNodeId.N_UnyieldingHeart, "재생의 축복", RelicBranch.Defense, 4,
            -1, -8, 4, AbilityTarget.All, P(StatType.MaxHp, 0.10f));
        Stat(RelicNodeId.N_ImmortalVow, RelicNodeId.N_RampartOath, "불멸의 서약", RelicBranch.Defense, 5,
            1, -8, 1, AbilityTarget.All, A(StatType.Defense, 0.10f));

        // ── 방패병·장수 방어 (왼쪽 아래) ─────────────────────────
        Stat(RelicNodeId.N_GeneralPlate, RelicNodeId.N_ToughSkin, "장군의 흉갑", RelicBranch.Defense, 2,
            -3, -3, 5, AbilityTarget.Unit_General, A(StatType.Defense, 0.025f));
        Stat(RelicNodeId.N_GuardianOath, RelicNodeId.N_GeneralPlate, "수호자의 맹세", RelicBranch.Defense, 3,
            -3, -5, 5, AbilityTarget.Unit_General, P(StatType.MaxHp, 0.09f));
        Stat(RelicNodeId.N_CounterArmor, RelicNodeId.N_GuardianOath, "역전의 갑주", RelicBranch.Defense, 4,
            -3, -7, 4, AbilityTarget.Unit_General,
            A(StatType.Defense, 0.025f), P(StatType.MaxHp, 0.09f));
        Stat(RelicNodeId.N_BulwarkLord, RelicNodeId.N_GeneralPlate, "방벽의 군주", RelicBranch.Defense, 3,
            -5, -4, 5, AbilityTarget.Job_ShieldBearer,
            A(StatType.Defense, 0.05f), P(StatType.MaxHp, 0.08f));
        Stat(RelicNodeId.N_FortressAvatar, RelicNodeId.N_BulwarkLord, "요새의 화신", RelicBranch.Defense, 4,
            -6, -6, 4, AbilityTarget.Job_ShieldBearer,
            P(StatType.MaxHp, 0.12f), A(StatType.Defense, 0.03f), A(StatType.SoldierCount, 1f));
        Stat(RelicNodeId.N_SteelCitadel, RelicNodeId.N_FortressAvatar, "강철 성채", RelicBranch.Defense, 5,
            -6, -8, 1, AbilityTarget.Job_ShieldBearer,
            A(StatType.Defense, 0.12f), P(StatType.MaxHp, 0.30f));

        // ══════════════════════════════════════════════════════════
        //  병사 트렁크 (왼쪽) — 뿌리 직속은 '지휘의 깃발' 하나뿐이다
        // ══════════════════════════════════════════════════════════
        Stat(RelicNodeId.N_CommandBanner, RelicNodeId.N_Origin, "지휘의 깃발", RelicBranch.Soldier, 1,
            -3, 0, 5, AbilityTarget.All, A(StatType.SoldierCount, 1f));

        // ── 정공 (왼쪽) ──────────────────────────────────────────
        // ⚠ 병사 공격력과 체력은 같은 티어의 형제다
        //   한쪽이 먼저 열리면 "체력을 올리려고 공격력을 찍는" 순서가 강제된다.
        //   둘 다 지휘의 깃발 직속으로 두고 어느 쪽부터 갈지는 플레이어가 고른다.
        Stat(RelicNodeId.N_CommandBasics, RelicNodeId.N_CommandBanner, "지휘의 기초", RelicBranch.Soldier, 2,
            -5, 2, 5, AbilityTarget.Unit_Soldier, P(StatType.Attack, 0.05f));
        Stat(RelicNodeId.N_LineDrill, RelicNodeId.N_CommandBanner, "전열의 훈련", RelicBranch.Soldier, 2,
            -5, 0, 5, AbilityTarget.Unit_Soldier, P(StatType.MaxHp, 0.05f));
        Stat(RelicNodeId.N_GrandAdvance, RelicNodeId.N_CommandBanner, "대군의 진격", RelicBranch.Soldier, 3,
            -5, -2, 5, AbilityTarget.All, A(StatType.SoldierCount, 2f));

        Stat(RelicNodeId.N_EliteDrill, RelicNodeId.N_CommandBasics, "정예 훈련", RelicBranch.Soldier, 3,
            -7, 3, 5, AbilityTarget.Unit_Soldier,
            P(StatType.Attack, 0.04f), P(StatType.MaxHp, 0.04f));
        Stat(RelicNodeId.N_UndefeatedLegion, RelicNodeId.N_EliteDrill, "불패의 군단", RelicBranch.Soldier, 4,
            -9, 4, 4, AbilityTarget.Unit_Soldier,
            P(StatType.Attack, 0.08f), P(StatType.MaxHp, 0.08f));

        Stat(RelicNodeId.N_LegionMight, RelicNodeId.N_LineDrill, "군단의 위세", RelicBranch.Soldier, 3,
            -7, 1, 5, AbilityTarget.All, A(StatType.CommandPower, 2f));
        Stat(RelicNodeId.N_VeteranCommander, RelicNodeId.N_LegionMight, "백전노장", RelicBranch.Soldier, 4,
            -9, 2, 4, AbilityTarget.All, A(StatType.CommandPower, 3f));
        Stat(RelicNodeId.N_ThousandHorse, RelicNodeId.N_LegionMight, "천군만마", RelicBranch.Soldier, 4,
            -9, 0, 4, AbilityTarget.All, A(StatType.SoldierCount, 2f));
        Stat(RelicNodeId.N_WarGodMajesty, RelicNodeId.N_VeteranCommander, "군신의 위엄", RelicBranch.Soldier, 5,
            -11, 1, 3, AbilityTarget.All, A(StatType.CommandPower, 4f));

        // ── 역분기 (왼쪽 아래) — 병사를 깎아 장수 하나에 몰아준다 ──
        // ⚠ 병사 트렁크 안에 두는 게 맞다
        //   "병사를 키울 것인가, 병사를 버릴 것인가" 가 같은 가지 위에서 갈려야 선택이 된다.
        //   공격 트렁크에 두면 그냥 공짜 공격력 노드가 되어 버린다.
        //
        // ⚠ 깎는 값이 커서 지휘력이 음수로 내려간다
        //   병사 스탯 하한은 RelicTreeRules.MinSoldierStatRatio(20%) 가 잡는다.
        Stat(RelicNodeId.N_LoneGeneral, RelicNodeId.N_GrandAdvance, "고독한 장수", RelicBranch.Soldier, 3,
            -7, -3, 3, AbilityTarget.Unit_General,
            A(StatType.SoldierCount, -5f), P(StatType.Attack, 0.12f), P(StatType.MoveSpeed, 0.05f));
        Stat(RelicNodeId.N_OneVsThousand, RelicNodeId.N_LoneGeneral, "일기당천", RelicBranch.Soldier, 4,
            -9, -4, 3, AbilityTarget.Unit_General,
            A(StatType.SoldierCount, -5f), A(StatType.CommandPower, -5f),
            P(StatType.Attack, 0.10f), P(StatType.MaxHp, 0.10f), A(StatType.Defense, 0.03f));
        Stat(RelicNodeId.N_Peerless, RelicNodeId.N_OneVsThousand, "무쌍", RelicBranch.Soldier, 5,
            -11, -3, 2, AbilityTarget.Unit_General,
            A(StatType.SoldierCount, -10f), P(StatType.Attack, 0.15f),
            A(StatType.CritChance, 0.08f), P(StatType.CritDamage, 0.20f), P(StatType.AttackSpeed, 0.10f));
        // 독고다이 — 깎아낸 병사 수에 비례해 장수가 강해진다 (역분기를 깊게 탈수록 이득)
        Sys(RelicNodeId.N_ForsakenStand, RelicNodeId.N_LoneGeneral, "고립무원", 4,
            -7, -5, 3, RelicSystemEffect.LoneWolfBonus, 0.004f, RelicBranch.Soldier);

        // ══════════════════════════════════════════════════════════
        //  유틸 트렁크 (오른쪽) — 뿌리 직속은 '성장의 증표' 하나뿐이다
        // ══════════════════════════════════════════════════════════
        Sys(RelicNodeId.N_GrowthMark, RelicNodeId.N_Origin, "성장의 증표", 1,
            3, 0, 5, RelicSystemEffect.ExpGainBonus, 0.12f);

        // ── 자원 (오른쪽 위) ─────────────────────────────────────
        Sys(RelicNodeId.N_GoldenGrace, RelicNodeId.N_GrowthMark, "황금의 가호", 2,
            5, 2, 5, RelicSystemEffect.GoldGainBonus, 0.12f);
        Sys(RelicNodeId.N_WarriorLegacy, RelicNodeId.N_GoldenGrace, "전사의 유산", 3,
            7, 3, 5, RelicSystemEffect.SoldierSoulGainBonus, 0.12f);
        Sys(RelicNodeId.N_SageTome, RelicNodeId.N_GoldenGrace, "현자의 서", 4,
            5, 4, 4, RelicSystemEffect.ExpGainBonus, 0.10f);
        Sys(RelicNodeId.N_GoldenBounty, RelicNodeId.N_WarriorLegacy, "만금의 축복", 4,
            9, 4, 4, RelicSystemEffect.GoldGainBonus, 0.10f);
        Sys(RelicNodeId.N_SoulUrn, RelicNodeId.N_WarriorLegacy, "영혼의 항아리", 4,
            7, 5, 4, RelicSystemEffect.SoldierSoulGainBonus, 0.10f);

        // ── 편의 (오른쪽 아래) ───────────────────────────────────
        // ⚠ 배속은 두 노드로 나뉜다 — 저티어에서 2×, 고티어에서 3×
        //   하나에 몰아 두면 첫 환생에 3배속이 열려 트리를 더 볼 이유가 사라진다.
        Sys(RelicNodeId.N_TimeReins, RelicNodeId.N_GrowthMark, "시간의 고삐", 2,
            5, 0, 1, RelicSystemEffect.BattleSpeedUnlock, 1f);
        // 장수 슬롯은 최종 티어가 아니라 t3 다 — 부대 편성이 바뀌는 재미는 일찍 줘야 한다
        Sys(RelicNodeId.N_MarchOrder, RelicNodeId.N_TimeReins, "출병 명령", 3,
            7, 1, 1, RelicSystemEffect.GeneralSlotBonus, 1f);
        Sys(RelicNodeId.N_AbilityReform, RelicNodeId.N_TimeReins, "어빌리티 재편성", 3,
            7, -1, 3, RelicSystemEffect.AbilityRefreshCount, 1f);
        Sys(RelicNodeId.N_MomentMastery, RelicNodeId.N_MarchOrder, "찰나의 지배", 4,
            9, 0, 1, RelicSystemEffect.BattleSpeedUnlock, 1f);
        Sys(RelicNodeId.N_FateDice, RelicNodeId.N_MarchOrder, "운명의 주사위", 3,
            9, 2, 2, RelicSystemEffect.RandomTraitOnStart, 1f);
        Sys(RelicNodeId.N_AdvancedArcana, RelicNodeId.N_AbilityReform, "고급 비전", 4,
            9, -2, 2, RelicSystemEffect.AbilityAdvancedChance, 0.10f);
        Sys(RelicNodeId.N_BlessedChoice, RelicNodeId.N_AbilityReform, "선택의 축복", 4,
            11, -1, 2, RelicSystemEffect.AbilityChoiceCount, 1f);

        // ── 적 약화 (아래 오른쪽) ────────────────────────────────
        Sys(RelicNodeId.N_TrialBaptism, RelicNodeId.N_GrowthMark, "시련의 세례", 2,
            4, -3, 5, RelicSystemEffect.EnemyMaxHpReduction, 0.025f);
        Sys(RelicNodeId.N_FearBrand, RelicNodeId.N_TrialBaptism, "공포의 각인", 3,
            5, -5, 5, RelicSystemEffect.EnemyAttackReduction, 0.025f);
        Sys(RelicNodeId.N_WitherCurse, RelicNodeId.N_FearBrand, "쇠약의 저주", 4,
            4, -7, 4, RelicSystemEffect.EnemyMaxHpReduction, 0.04f);
        Sys(RelicNodeId.N_Disarm, RelicNodeId.N_FearBrand, "무력화", 4,
            6, -7, 4, RelicSystemEffect.EnemyAttackReduction, 0.04f);
        Sys(RelicNodeId.N_DoomProphecy, RelicNodeId.N_Disarm, "몰락의 예언", 5,
            5, -9, 1, RelicSystemEffect.EnemyMaxHpReduction, 0.12f);

        // ── 색인 ─────────────────────────────────────────────
        _all      = t.ToArray();
        _byId     = new Dictionary<RelicNodeId, RelicNodeDef>(_all.Length);
        _children = new Dictionary<RelicNodeId, List<RelicNodeDef>>();
        foreach (var d in _all)
        {
            _byId[d.Id] = d;
            if (d.Parent == RelicNodeId.None) continue;
            if (!_children.TryGetValue(d.Parent, out var list))
                _children[d.Parent] = list = new List<RelicNodeDef>();
            list.Add(d);
        }
        _build = null;
    }

    // ── 표 작성 헬퍼 ──────────────────────────────────────────

    static List<RelicNodeDef> _build;

    /// <summary>비율(%) 스탯 한 줄.</summary>
    static RelicNodeStat P(StatType s, float v) => new() { Stat = s, PerLevel = v, Absolute = false };

    /// <summary>절대값(%p·명·포인트) 스탯 한 줄.</summary>
    static RelicNodeStat A(StatType s, float v) => new() { Stat = s, PerLevel = v, Absolute = true };

    static void Stat(RelicNodeId id, RelicNodeId parent, string name, RelicBranch branch, int tier,
                     int x, int y, int maxLevel, AbilityTarget target, params RelicNodeStat[] stats)
        => _build.Add(new RelicNodeDef
        {
            Id = id, Parent = parent, Name = name, Branch = branch, Tier = tier,
            X = x, Y = y, MaxLevel = maxLevel, CostBase = TierCost[tier],
            Target = target, Stats = stats, System = RelicSystemEffect.None,
        });

    /// <summary>시스템 노드. 거의 전부 유틸이라 branch 는 기본값을 둔다 (고립무원만 병사).</summary>
    static void Sys(RelicNodeId id, RelicNodeId parent, string name, int tier,
                    int x, int y, int maxLevel, RelicSystemEffect effect, float perLevel,
                    RelicBranch branch = RelicBranch.Utility)
        => _build.Add(new RelicNodeDef
        {
            Id = id, Parent = parent, Name = name, Branch = branch, Tier = tier,
            X = x, Y = y, MaxLevel = maxLevel, CostBase = TierCost[tier],
            Target = AbilityTarget.All, Stats = new RelicNodeStat[0],
            System = effect, SystemPerLevel = perLevel,
        });
}
