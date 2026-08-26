using System;
using UnityEngine;

// ============================================================
//  RelicTreeTypes.cs
//  유물 테크트리 — 타입 정의 (노드 정의는 RelicTreeCatalog.cs)
//
//  ■ 왜 SO 가 아니라 코드 테이블인가
//    노드가 71개다. SO 로 만들면 부모 참조가 끊긴 에셋을 눈으로 찾아야 하고
//    가지 구조를 한눈에 볼 수 없다. 트리는 "표 하나"로 있을 때만 관리된다.
//    (구 RelicData SO + RelicPopup 은 2026-08-25 에 제거됐다)
//
//  ■ 좌표계
//    정수 그리드. +Y = 위. 뿌리에서 **사방 4갈래**만 뻗는다.
//      위    = 공격력 (벼려진 칼날)   → 본선·공속쿨감·직업 세 갈래로 벌어진다
//      아래  = 체력   (굳은 살갗)     → 본선·방패병 두 갈래
//      왼쪽  = 병사 수 (지휘의 깃발)  → 정공·역분기 두 갈래
//      오른쪽 = 경험치 (성장의 증표)  → 자원·편의·적약화 세 갈래
//
//    ⚠ 뿌리에 갈래를 더 붙이지 말 것
//      첫 화면에서 여러 갈래가 한꺼번에 열리면 무엇부터 찍을지 판단이 안 선다.
//      8방향 분기 자체는 살아 있다 — 아래쪽 노드가 쓴다.
//
//    ⚠ 좌표 간격 규칙 (노드 칸이 겹친다)
//      한 칸 = 118px 인데 노드 카드는 세로 130px 쯤 된다.
//      같은 x 면 dy ≥ 2, 같은 y 면 dx ≥ 2. 대각선은 자유.
//
//  ■ 해금·시야 규칙
//    부모 레벨 ≥ 1 이어야 자식이 열린다(Unlocked).
//    열리기 전까지는 존재 자체가 보이지 않는다(Visible == false) — 안개.
//    단, 보이는 노드의 자식은 "미지의 노드"로 실루엣만 그린다 (트리 끝을 알 수 있게).
// ============================================================

// ── 계열 ──────────────────────────────────────────────────────
public enum RelicBranch
{
    Root    = 0,
    Attack  = 1,   // 공격력·공속·쿨감·치명 + 직업 3종(기사·궁수·마법사) — 뿌리 위쪽
    Defense = 2,   // 체력·방어율 + 방패병 — 뿌리 아래쪽
    Soldier = 3,   // 병사 수·지휘력 (+ 병사를 깎아 장수를 올리는 역분기) — 뿌리 왼쪽
    Utility = 4,   // 경험치·골드·소울·배속·어빌리티·특성 + 적 약화 — 뿌리 오른쪽
}

// ── 노드 ID ───────────────────────────────────────────────────
//  1xx = 공격 / 2xx = 체력 / 3xx = 병사 / 40x·41x = 유틸 / 42x = 적 약화
//  ⚠ 번호는 계열 표시일 뿐 트리 순서가 아니다 — 부모는 RelicTreeCatalog 가 정한다
public enum RelicNodeId
{
    None = 0,

    /// <summary>뿌리 — 공격력·체력 +5%. 1레벨뿐이고 여기서 사방 4갈래가 뻗는다.</summary>
    N_Origin           = 1,  // 근원의 각인      — 공격력 +5%, 체력 +5%                    t0 Lv1  1pt

    // ── 공격 트렁크 · 본선 (위) ──────────────────────────────
    N_Blade            = 101,  // 벼려진 칼날      — 공격력 +4%                              t1 Lv5  30pt
    N_Destruction      = 102,  // 파괴의 의지      — 공격력 +5%                              t2 Lv5  45pt
    N_CritSense        = 103,  // 치명의 감각      — 치명 +2.5%p                             t3 Lv5  75pt
    N_PierceLance      = 104,  // 관통의 창        — 방어관통 +3%p                           t3 Lv5  75pt
    N_Executioner      = 105,  // 처형자           — 치명피해 +12%                           t4 Lv4  80pt
    N_SlayerSeal       = 106,  // 학살자의 인장    — 공격력 +8%                              t4 Lv4  80pt
    N_OneStrike        = 107,  // 일격필살         — 치명피해 +50%, 치명 +10%p               t5 특수 12pt

    // ── 공격 트렁크 · 공속/쿨감 (오른쪽 위) ──────────────────
    N_SwiftHand        = 111,  // 빠른 손놀림      — 공속 +4%                                t2 Lv5  45pt
    N_ChainRhythm      = 112,  // 연격의 리듬      — 공속 +5%                                t3 Lv5  75pt
    N_TimeCompress     = 113,  // 시간의 압축      — 쿨감 +2.5%p                             t3 Lv5  75pt
    N_HawkEye          = 114,  // 매의 시야        — 사거리 +6%                              t4 Lv4  80pt
    N_StormConcert     = 115,  // 폭풍의 연주      — 공속 +8%                                t4 Lv4  80pt
    N_SpellHaste       = 116,  // 주문 가속        — 쿨감 +3%p                               t4 Lv4  80pt
    N_EndlessChain     = 117,  // 무한 연쇄        — 쿨감 +12%p, 공속 +10%                   t5 특수 12pt

    // ── 공격 트렁크 · 직업 (왼쪽 위) ─────────────────────────
    N_GeneralSword     = 121,  // 장군의 검        — 장수 공격력 +6%                         t2 Lv5  45pt
    N_HeroToken        = 122,  // 영웅의 증표      — 장수 공격력 +7%, 체력 +7%               t3 Lv5  75pt
    N_MartialLegacy    = 123,  // 무예의 전승      — 공격력 +5%                              t2 특수 3pt
    N_KnightOath       = 124,  // 기사단의 맹세    — 기사 공격력 +8%, 체력 +6%               t3 Lv5  75pt
    N_ArcherSpirit     = 125,  // 활의 정령        — 궁수 공속 +8%, 사거리 +6%               t3 Lv5  75pt
    N_MageCrystal      = 126,  // 마력의 결정      — 마법사 공격력 +9%, 쿨감 +4%p            t3 Lv5  75pt
    N_KingsBlade       = 127,  // 왕의 검          — 기사 공격력 +12%, 방어율 +4%p, 병사 +1명 t5 Lv3  72pt
    N_StormBow         = 128,  // 폭풍의 궁        — 궁수 공격력 +12%, 치명 +5%p, 사거리 +6% t5 Lv3  72pt
    N_ArchmageLegacy   = 129,  // 대마법사의 유산  — 마법사 공격력 +14%, 쿨감 +5%p, 사거리 +6% t5 Lv3  72pt
    N_HeroAwaken       = 130,  // 영웅의 각성      — 장수 공격력 +9%, 치명 +4%p              t4 Lv4  80pt

    // ── 체력 트렁크 · 본선 (아래) ────────────────────────────
    N_ToughSkin        = 201,  // 굳은 살갗        — 체력 +5%                                t1 Lv5  30pt
    N_IronWill         = 202,  // 철의 의지        — 방어율 +2%p                             t2 Lv5  45pt
    N_UnyieldingHeart  = 203,  // 불굴의 심장      — 체력 +6%                                t3 Lv5  75pt
    N_RampartOath      = 204,  // 성벽의 맹세      — 방어율 +2.5%p                           t3 Lv5  75pt
    N_Regeneration     = 205,  // 재생의 축복      — 체력 +10%                               t4 Lv4  80pt
    N_ImmortalVow      = 206,  // 불멸의 서약      — 방어율 +10%p                            t5 특수 12pt

    // ── 체력 트렁크 · 방패병/장수 (왼쪽 아래) ────────────────
    N_GeneralPlate     = 211,  // 장군의 흉갑      — 장수 방어율 +2.5%p                      t2 Lv5  45pt
    N_GuardianOath     = 212,  // 수호자의 맹세    — 장수 체력 +9%                           t3 Lv5  75pt
    N_BulwarkLord      = 213,  // 방벽의 군주      — 방패 방어율 +5%p, 체력 +8%              t3 Lv5  75pt
    N_CounterArmor     = 214,  // 역전의 갑주      — 장수 방어율 +2.5%p, 체력 +9%            t4 Lv4  80pt
    N_FortressAvatar   = 215,  // 요새의 화신      — 방패 체력 +12%, 방어율 +3%p, 병사 +1명  t4 Lv4  80pt
    N_SteelCitadel     = 216,  // 강철 성채        — 방패 방어율 +12%p, 체력 +30%            t5 특수 12pt

    // ── 병사 트렁크 · 정공 (왼쪽) ────────────────────────────
    N_CommandBanner    = 301,  // 지휘의 깃발      — 병사 +1명                               t1 Lv5  30pt
    N_CommandBasics    = 302,  // 지휘의 기초      — 병사 공격력 +5%                         t2 Lv5  45pt
    N_LineDrill        = 303,  // 전열의 훈련      — 병사 체력 +5%                           t2 Lv5  45pt
    N_GrandAdvance     = 304,  // 대군의 진격      — 병사 +2명                               t3 Lv5  75pt
    N_EliteDrill       = 305,  // 정예 훈련        — 병사 공격력 +4%, 체력 +4%               t3 Lv5  75pt
    N_LegionMight      = 306,  // 군단의 위세      — 지휘력 +2                               t3 Lv5  75pt
    N_UndefeatedLegion = 307,  // 불패의 군단      — 병사 공격력 +8%, 체력 +8%               t4 Lv4  80pt
    N_VeteranCommander = 308,  // 백전노장         — 지휘력 +3                               t4 Lv4  80pt
    N_ThousandHorse    = 309,  // 천군만마         — 병사 +2명                               t4 Lv4  80pt
    N_WarGodMajesty    = 310,  // 군신의 위엄      — 지휘력 +4                               t5 Lv3  72pt

    // ── 병사 트렁크 · 역분기 (왼쪽 아래) ─────────────────────
    N_LoneGeneral      = 311,  // 고독한 장수      — 장수 병사 -5명, 공격력 +12%, 이속 +5%   t3 Lv3  30pt
    N_OneVsThousand    = 312,  // 일기당천         — 장수 병사 -5명, 지휘력 -5, 공격력 +10%, 체력 +10%, 방어율 +3%p t4 Lv3  48pt
    N_Peerless         = 313,  // 무쌍             — 장수 병사 -10명, 공격력 +15%, 치명 +8%p, 치명피해 +20%, 공속 +10% t5 Lv2  36pt
    N_ForsakenStand    = 314,  // 고립무원         — 깎은 병사당 장수 공·체 +0.4%            t4 Lv3  48pt

    // ── 유틸 트렁크 · 자원 (오른쪽 위) ───────────────────────
    N_GrowthMark       = 401,  // 성장의 증표      — 경험치 +12%                             t1 Lv5  30pt
    N_GoldenGrace      = 402,  // 황금의 가호      — 골드 +12%                               t2 Lv5  45pt
    N_WarriorLegacy    = 403,  // 전사의 유산      — 소울 +12%                               t3 Lv5  75pt
    N_SageTome         = 404,  // 현자의 서        — 경험치 +10%                             t4 Lv4  80pt
    N_GoldenBounty     = 405,  // 만금의 축복      — 골드 +10%                               t4 Lv4  80pt
    N_SoulUrn          = 406,  // 영혼의 항아리    — 소울 +10%                               t4 Lv4  80pt
    N_TravelFund       = 407,  // 여정의 노잣돈    — 시작 골드 +150                          t2 Lv5  45pt
    N_WarChest         = 408,  // 여정의 군자금    — 시작 골드 +600                          t5 Lv3  72pt

    // ── 유틸 트렁크 · 편의 (오른쪽 아래) ─────────────────────
    N_TimeReins        = 411,  // 시간의 고삐      — 배속 1.5x 해금                          t2 특수 3pt
    N_MomentMastery    = 412,  // 찰나의 지배      — 배속 2x 해금                            t4 특수 8pt
    N_MarchOrder       = 413,  // 출병 명령        — 장수 슬롯 +1칸                          t3 특수 5pt
    N_AbilityReform    = 414,  // 어빌리티 재편성  — 새로고침 +1회                           t3 Lv3  30pt
    N_AdvancedArcana   = 415,  // 고급 비전        — 고급확률 +10%p                          t4 Lv2  24pt
    N_BlessedChoice    = 416,  // 선택의 축복      — 선택지 +1개                             t4 Lv2  24pt
    N_FateDice         = 417,  // 운명의 주사위    — 무작위 특성 +1개                        t3 Lv2  15pt

    // ── 유틸 트렁크 · 적 약화 (아래) ─────────────────────────
    N_TrialBaptism     = 421,  // 시련의 세례      — 적 체력 -2.5%                           t2 Lv5  45pt
    N_FearBrand        = 422,  // 공포의 각인      — 적 공격력 -2.5%                         t3 Lv5  75pt
    N_WitherCurse      = 423,  // 쇠약의 저주      — 적 체력 -4%                             t4 Lv4  80pt
    N_Disarm           = 424,  // 무력화           — 적 공격력 -4%                           t4 Lv4  80pt
    N_DoomProphecy     = 425,  // 몰락의 예언      — 적 체력 -12%                            t5 특수 12pt
}

// ── 노드가 주는 스탯 한 줄 ────────────────────────────────────
[Serializable]
public struct RelicNodeStat
{
    public StatType Stat;
    public float    PerLevel;

    /// <summary>
    /// true = 절대값 가산 (방어율·치명확률 %p, 병사 수 '명', 지휘력 포인트).
    /// false = 기저값 대비 비율 (%).
    ///
    /// ⚠ 스탯마다 따로 잡는다 — 구 RelicData 는 노드 전체에 하나였다
    ///   '치명피해 +50%(비율) + 치명확률 +10%p(절대)' 같은 노드를 만들 수 없었다.
    /// </summary>
    public bool Absolute;
}

// ── 노드 정의 ─────────────────────────────────────────────────
public sealed class RelicNodeDef
{
    public RelicNodeId Id;
    public RelicNodeId Parent;      // None = 뿌리
    public string      Name;
    public RelicBranch Branch;
    public int         Tier;        // 0=뿌리 … 5=말단. 비용이 여기서 나온다
    public int         X;           // 그리드 좌표 (+Y = 위)
    public int         Y;
    public int         MaxLevel;    // 1 = 단일 습득
    public int         CostBase;    // 레벨업 비용 = CostBase × (현재레벨+1)

    public AbilityTarget    Target;
    public RelicNodeStat[]  Stats;          // EffectType == Stat
    public RelicSystemEffect System;        // EffectType == System (None 이면 스탯 노드)
    public float            SystemPerLevel;

    /// <summary>레벨이 없는 한 방 노드 — 아이콘 테두리와 툴팁 표기가 다르다.</summary>
    public bool Special => MaxLevel == 1 && Branch != RelicBranch.Root;

    public bool IsSystem => System != RelicSystemEffect.None;

    // ── 비용 ──────────────────────────────────────────────────

    /// <summary>현재 레벨 → 다음 레벨 비용. 만렙이면 0.</summary>
    public int LevelUpCost(int currentLevel)
        => currentLevel >= MaxLevel ? 0 : CostBase * (currentLevel + 1);

    /// <summary>0 → 만렙까지 총 비용.</summary>
    public int TotalCost
    {
        get
        {
            int sum = 0;
            for (int lv = 0; lv < MaxLevel; lv++) sum += LevelUpCost(lv);
            return sum;
        }
    }

    // ── 설명 ──────────────────────────────────────────────────

    /// <summary>
    /// 툴팁 본문. level 0 이면 "1레벨을 찍었을 때" 값을 보여 준다 —
    /// 아직 없는 노드의 툴팁이 전부 +0% 로 나오면 살지 말지 판단할 수가 없다.
    /// </summary>
    public string GetDescription(int level)
    {
        int shown = Mathf.Max(1, level);

        if (IsSystem)
            return StatBonusColors.Wrap(StatSource.Relic, SystemLine(SystemPerLevel * shown));

        string body = string.Empty;
        for (int i = 0; i < Stats.Length; i++)
        {
            if (i > 0) body += "\n";
            body += StatLine(Stats[i], shown);
        }
        return $"[{LocalizationManager.Instance.Get(Target.ToString())}]\n" +
               StatBonusColors.Wrap(StatSource.Relic, body);
    }

    string StatLine(RelicNodeStat s, int level)
    {
        float total = s.PerLevel * level;
        string label = LocalizationManager.Instance.Get(s.Stat.ToString());
        string sign  = total < 0f ? "" : "+";

        if (!s.Absolute) return $"{label} {sign}{total * 100f:0.#}%";

        if (s.Stat == StatType.SoldierCount)  return $"{label} {sign}{Mathf.RoundToInt(total)}명";
        if (s.Stat == StatType.CommandPower)  return $"{label} {sign}{Mathf.RoundToInt(total)}";
        return $"{label} {sign}{total * 100f:0.#}%p";
    }

    string SystemLine(float v) => System switch
    {
        RelicSystemEffect.AbilityRefreshCount   => $"어빌리티 새로고침 +{Mathf.RoundToInt(v)}회",
        RelicSystemEffect.AbilityChoiceCount    => $"어빌리티 선택지 +{Mathf.RoundToInt(v)}개",
        RelicSystemEffect.AbilityAdvancedChance => $"고급 이상 어빌리티 확률 +{v * 100f:0.#}%p",
        RelicSystemEffect.GoldGainBonus         => $"골드 획득량 +{v * 100f:0.#}%",
        RelicSystemEffect.StartGoldBonus        => $"여정 시작 시 골드 +{Mathf.RoundToInt(v):#,0}",
        RelicSystemEffect.SoldierSoulGainBonus  => $"병사 소울 획득량 +{v * 100f:0.#}%",
        RelicSystemEffect.ExpGainBonus          => $"경험치 획득량 +{v * 100f:0.#}%",
        RelicSystemEffect.EnemyMaxHpReduction   => $"적 최대 체력 -{v * 100f:0.#}%",
        RelicSystemEffect.EnemyAttackReduction  => $"적 공격력 -{v * 100f:0.#}%",
        RelicSystemEffect.GeneralSlotBonus      => $"장수 배치 슬롯 +{Mathf.RoundToInt(v)}칸",
        // 배속 값의 정본은 TopBarUI.SpeedSteps 다. 여기에 숫자를 박으면 둘이 갈라진다.
        // ⚠ v 는 '이 노드가 주는 양'(둘 다 1)이라 노드만으로는 몇 번째 단계인지 모른다.
        //   두 번째 해금 노드(찰나의 지배)만 짚어 준다 — 아니면 둘 다 같은 배속을 말한다.
        RelicSystemEffect.BattleSpeedUnlock     =>
            $"전투 배속 {TopBarUI.SpeedAtStep(Id == RelicNodeId.N_MomentMastery ? 2 : 1):0.##}× 해금",
        RelicSystemEffect.RandomTraitOnStart    => $"여정 시작 시 무작위 특성 +{Mathf.RoundToInt(v)}개",
        RelicSystemEffect.LoneWolfBonus         => $"줄인 병사 1명당 장수 공격력·체력 +{v * 100f:0.##}%",
        _                                       => string.Empty,
    };
}

// ── 트리 전역 규칙 ────────────────────────────────────────────
public static class RelicTreeRules
{
    /// <summary>
    /// 지휘력이 음수로 내려가도 병사 스탯은 이 비율 아래로 떨어지지 않는다.
    ///
    /// ⚠ 하한이 없으면 역분기가 '병사 삭제 버튼' 이 된다
    ///   지휘력 1포인트당 병사 스탯 1% 라서 일기당천(-15)까지 타면 지휘력이
    ///   0 밑으로 한참 내려간다. 하한 없이 곱하면 병사가 종잇장이 되어
    ///   "장수 하나로 간다" 가 아니라 "병사가 없다" 가 되어 버린다.
    ///   최소 20% 는 남겨야 병사가 몸빵 역할이라도 한다.
    ///
    ///   적용 지점은 SoldierRuntimeBridge.StatRatio 하나다 — 로비 표시와 전투가
    ///   같은 값을 쓰게 하려면 거기서 한 번만 걸어야 한다.
    /// </summary>
    public const float MinSoldierStatRatio = 0.20f;

    /// <summary>정공 노드로 올릴 수 있는 병사 수 총합.</summary>
    public const int MaxSoldierGain = 30;

    /// <summary>역분기로 깎을 수 있는 병사 수 총합 (고독한 장수+일기당천+무쌍).</summary>
    public const int MaxSoldierCut = 50;
}
