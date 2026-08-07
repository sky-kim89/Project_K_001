// ============================================================
//  IconGenerator.cs
//  Tools > Project K > Generate Icons 메뉴에서 실행.
//  직업 아이콘(64×64) 4장, 액티브 스킬 아이콘(48×48) 20장을
//  PNG로 생성 → Assets/_project/3.Textures/Icons/ 에 저장.
// ============================================================
using System;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class IconGenerator
{
    const string CLASS_PATH      = "Assets/_project/3.Textures/Icons/Classes";
    const string SKILL_PATH      = "Assets/_project/3.Textures/Icons/Skills";
    const string ABILITY_PATH    = "Assets/_project/3.Textures/Icons/Abilities";
    const string LOBBY_BTN_PATH  = "Assets/_project/3.Textures/Icons/LobbyBtns";
    const string TRAIT_PATH      = "Assets/_project/3.Textures/Icons/Traits";
    const string STAGE_NODE_PATH = "Assets/_project/3.Textures/Icons/StageNodes";

    // ── 컬러 팔레트 ───────────────────────────────────────────
    static readonly Color32 Knight_BgDark  = Hex("1A0606"); static readonly Color32 Knight_BgMid   = Hex("4A1010");
    static readonly Color32 Knight_Rim     = Hex("8B4444");
    static readonly Color32 Archer_BgDark  = Hex("060E02"); static readonly Color32 Archer_BgMid   = Hex("1A4A1A");
    static readonly Color32 Archer_Rim     = Hex("448B44");
    static readonly Color32 Mage_BgDark    = Hex("080520"); static readonly Color32 Mage_BgMid     = Hex("251A6A");
    static readonly Color32 Mage_Rim       = Hex("6644CC");
    static readonly Color32 Shield_BgDark  = Hex("040E0E"); static readonly Color32 Shield_BgMid   = Hex("104040");
    static readonly Color32 Shield_Rim     = Hex("448B8B");

    static readonly Color32 Silver  = Hex("D0D0D0");
    static readonly Color32 Gold    = Hex("D4A840");
    static readonly Color32 DkGold  = Hex("8B6820");
    static readonly Color32 Wood    = Hex("7A4020");
    static readonly Color32 White   = Hex("FFFFFF");
    static readonly Color32 Green   = Hex("2ECC71");
    static readonly Color32 Purple  = Hex("9933FF");
    static readonly Color32 Red     = Hex("E74C3C");
    static readonly Color32 Teal    = Hex("22CCDD");
    static readonly Color32 Orange  = Hex("FF8833");
    static readonly Color32 Yellow  = Hex("FFCC44");

    // ═══════════════════════════════════════════════════════
    //  메뉴 진입점
    // ═══════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════
    //  어빌리티 아이콘 메뉴
    // ═══════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════
    //  특성 아이콘 메뉴
    // ═══════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════
    //  스테이지 노드 아이콘 메뉴
    // ═══════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Icon + "스테이지 노드 아이콘", priority = ProjectKMenu.IconPrio + 17)]
    public static void GenerateStageNodeIcons()
    {
        EnsureDir(STAGE_NODE_PATH);
        Save(48, 48, STAGE_NODE_PATH + "/stage_normal.png", DrawStageNormal);
        Save(48, 48, STAGE_NODE_PATH + "/stage_elite.png",  DrawStageElite);
        Save(48, 48, STAGE_NODE_PATH + "/stage_shop.png",   DrawStageShop);
        Save(48, 48, STAGE_NODE_PATH + "/stage_event.png",  DrawStageEvent);
        AssetDatabase.Refresh();
        ApplySpriteImportSettings(STAGE_NODE_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 스테이지 노드 아이콘 4장 생성 완료.");
    }

    // ─────────────────────────────────────────────────────
    //  ■ 스테이지 노드 아이콘 (48×48)
    // ─────────────────────────────────────────────────────

    // Normal — 검 (파란 계열): 가장 기본 전투
    static void DrawStageNormal(P p)
    {
        p.BgGradient(Hex("0A0F1C"), Hex("182040"));
        p.RoundedBorder(8, 2, Hex("3C4E7A"));

        // 손잡이 끝 (pommel)
        p.FillCircle(24, 4, 4, Hex("C8A440"));
        p.FillCircleAlpha(23, 5, 2, new Color32(255, 255, 255, 110));

        // 그립
        p.FillRRect(21, 8, 6, 9, 1, Hex("7A4020"));
        p.FillRect(22, 8, 2, 8, new Color32(255, 255, 255, 28));

        // 가드 (크로스가드)
        p.FillRRect(12, 17, 24, 5, 2, Hex("C8A440"));
        p.FillRect(12, 20, 24, 2, new Color32(255, 255, 255, 40));

        // 검날 몸체
        p.FillRRect(21, 22, 6, 20, 1, Hex("B8C8E0"));
        p.FillRect(22, 23, 2, 18, new Color32(255, 255, 255, 55));

        // 검날 끝 (삼각)
        p.FillTri(21, 42, 27, 42, 24, 47, Hex("B8C8E0"));
    }

    // Elite — 5각 별 (붉은 계열): 강화 전투
    static void DrawStageElite(P p)
    {
        p.BgGradient(Hex("18080A"), Hex("38100C"));
        p.RoundedBorder(8, 2, Hex("BB2222"));
        p.FillCircleAlpha(24, 25, 20, new Color32(220, 120, 40, 32));

        var gold  = Hex("D4A840");
        var goldL = Hex("F0C860");
        var goldD = Hex("8A6010");

        // 5각 별 — outer points: top(24,44), ur(40,31), lr(35,10), ll(13,10), ul(8,31)
        //          inner points: (28,31), (33,22), (24,17), (15,22), (20,31)
        // 꼭짓점 5개
        p.FillTri(24, 44, 20, 31, 28, 31, goldL);   // 위 꼭짓점
        p.FillTri(40, 31, 28, 31, 33, 22, gold);     // 우상
        p.FillTri(35, 10, 33, 22, 24, 17, goldD);    // 우하
        p.FillTri(13, 10, 24, 17, 15, 22, goldD);    // 좌하
        p.FillTri( 8, 31, 15, 22, 20, 31, gold);     // 좌상
        // 중앙 오각형 (중심 24,24 기준 5 삼각형)
        p.FillTri(20, 31, 28, 31, 24, 24, goldL);
        p.FillTri(28, 31, 33, 22, 24, 24, gold);
        p.FillTri(33, 22, 24, 17, 24, 24, goldD);
        p.FillTri(24, 17, 15, 22, 24, 24, goldD);
        p.FillTri(15, 22, 20, 31, 24, 24, gold);
        // 하이라이트
        p.FillCircleAlpha(23, 39, 3, new Color32(255, 248, 200, 130));
    }

    // Shop — 금화 (파란 계열): 상점
    static void DrawStageShop(P p)
    {
        p.BgGradient(Hex("081422"), Hex("0E2040"));
        p.RoundedBorder(8, 2, Hex("1E90C0"));

        // 금화 (다층 원)
        p.FillCircle(24, 24, 17, Hex("9A7010"));
        p.FillCircle(24, 24, 15, Hex("D4A820"));
        p.FillCircle(24, 24, 13, Hex("EAB830"));
        p.FillCircle(24, 24, 11, Hex("F0C840"));

        // 코인 테두리 하이라이트
        p.DrawCircle(24, 24, 15, 1, Hex("F8D860"));
        p.DrawCircle(24, 24, 17, 1, Hex("7A5808"));

        // 코인 반사광
        p.FillCircleAlpha(19, 30, 6, new Color32(255, 248, 200, 90));

        // 코인 면 — "₩" 간략 표현 (세로줄 + 두 가로줄)
        var mk = new Color32(100, 68, 0, 210);
        p.FillRRect(22, 16, 4, 18, 1, mk);   // 세로 기둥
        p.FillRRect(16, 28, 16, 3, 1, mk);   // 위 가로줄
        p.FillRRect(16, 21, 16, 3, 1, mk);   // 아래 가로줄
        // 사선 두 개 (₩ 형태)
        p.DrawLine(16, 16, 22, 31, mk, 2);
        p.DrawLine(32, 16, 26, 31, mk, 2);
    }

    // Event — 물음표 (보라 계열): 이벤트
    static void DrawStageEvent(P p)
    {
        p.BgGradient(Hex("100618"), Hex("281038"));
        p.RoundedBorder(8, 2, Hex("8833BB"));
        p.FillCircleAlpha(24, 26, 18, new Color32(136, 68, 220, 28));

        var c = Hex("DDD0FF");

        // "?" 상단 원호 — 좌변/상변/우변만 그려 C자 + 우측 세로줄
        p.FillRRect(16, 34, 5, 9, 2, c);    // 좌상 세로
        p.FillRRect(16, 40, 16, 5, 2, c);   // 최상단 가로
        p.FillRRect(27, 25, 5, 20, 2, c);   // 우측 세로 (전체)
        p.FillRRect(16, 25, 16, 5, 2, c);   // 중간 연결 가로

        // 꺾임 → 줄기
        p.FillRRect(21, 18, 6, 10, 2, c);   // 줄기

        // 점 (dot)
        p.FillCircle(24, 11, 4, c);
        p.FillCircleAlpha(23, 12, 2, new Color32(255, 255, 255, 140));
    }

    // ═══════════════════════════════════════════════════════
    //  특성 아이콘 메뉴
    // ═══════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Icon + "특성 아이콘", priority = ProjectKMenu.IconPrio + 12)]
    public static void GenerateTraitIcons()
    {
        EnsureDir(TRAIT_PATH);

        Save(48, 48, TRAIT_PATH + "/trait_knight_command.png",           DrawTraitKnight);
        Save(48, 48, TRAIT_PATH + "/trait_knight_soldier_rage.png",     DrawTraitKnightSoldierRage);
        Save(48, 48, TRAIT_PATH + "/trait_knight_hero_return.png",      DrawTraitKnightHeroReturn);
        Save(48, 48, TRAIT_PATH + "/trait_archer_precision.png",        DrawTraitArcher);
        Save(48, 48, TRAIT_PATH + "/trait_archer_retreat_fire.png",     DrawTraitArcherRetreatFire);
        Save(48, 48, TRAIT_PATH + "/trait_archer_rain_fire.png",        DrawTraitArcherRainFire);
        Save(48, 48, TRAIT_PATH + "/trait_mage_arcane.png",             DrawTraitMage);
        Save(48, 48, TRAIT_PATH + "/trait_mage_attack_cdr.png",         DrawTraitMageAttackCdr);
        Save(48, 48, TRAIT_PATH + "/trait_mage_echo_skill.png",         DrawTraitMageEchoSkill);
        Save(48, 48, TRAIT_PATH + "/trait_shield_fortress.png",         DrawTraitShield);
        Save(48, 48, TRAIT_PATH + "/trait_shield_counter_blow.png",     DrawTraitShieldCounterBlow);
        Save(48, 48, TRAIT_PATH + "/trait_shield_rage_build.png",       DrawTraitShieldRageBuild);

        Save(48, 48, TRAIT_PATH + "/trait_common_expedition.png",       DrawTraitCommonExpedition);
        Save(48, 48, TRAIT_PATH + "/trait_common_mass_mobilize.png",    DrawTraitCommonMassMobilize);
        Save(48, 48, TRAIT_PATH + "/trait_common_soldier_supply.png",   DrawTraitCommonSoldierSupply);
        Save(48, 48, TRAIT_PATH + "/trait_common_forced_levy.png",      DrawTraitCommonForcedLevy);
        Save(48, 48, TRAIT_PATH + "/trait_common_equip_expand.png",     DrawTraitCommonEquipExpand);

        // ── 이벤트 전용 특성 아이콘 ─────────────────────────────
        Save(48, 48, TRAIT_PATH + "/trait_event_battle_will.png",       DrawEventBattleWill);
        Save(48, 48, TRAIT_PATH + "/trait_event_potion_buff.png",       DrawEventPotionBuff);
        Save(48, 48, TRAIT_PATH + "/trait_event_potion_debuff.png",     DrawEventPotionDebuff);
        Save(48, 48, TRAIT_PATH + "/trait_event_blood_pact.png",        DrawEventBloodPact);
        Save(48, 48, TRAIT_PATH + "/trait_event_altar_curse.png",       DrawEventAltarCurse);
        Save(48, 48, TRAIT_PATH + "/trait_event_execution_morale.png",  DrawEventExecutionMorale);
        Save(48, 48, TRAIT_PATH + "/trait_event_spy_info.png",          DrawEventSpyInfo);
        Save(48, 48, TRAIT_PATH + "/trait_event_veteran_heritage.png",  DrawEventVeteranHeritage);

        // ── 직업 시너지 아이콘 ──────────────────────────────────
        Save(48, 48, TRAIT_PATH + "/trait_synergy_vanguard.png",        DrawSynergyVanguardCross);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_magic_shield.png",    DrawSynergyMagicShield);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_iron_wall.png",       DrawSynergyIronWallLine);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_balanced.png",        DrawSynergyBalancedHost);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_knight_order.png",    DrawSynergyKnightOrder);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_arrow_legion.png",    DrawSynergyArrowLegion);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_mage_corp.png",       DrawSynergyGreatMageCorp);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_ironclad.png",        DrawSynergyIronclad);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_ranged_firenet.png",  DrawSynergyRangedFirenet);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_iron_vanguard.png",   DrawSynergyIronVanguard);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_knight_squad.png",    DrawSynergyKnightSquad);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_archer_squad.png",    DrawSynergyArcherSquad);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_mage_squad.png",      DrawSynergyMageSquad);
        Save(48, 48, TRAIT_PATH + "/trait_synergy_shield_squad.png",    DrawSynergyShieldSquad);

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(TRAIT_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 특성 아이콘 39장 생성 완료.");
    }

    // ─────────────────────────────────────────────────────
    //  ■ 이벤트 전용 특성 아이콘
    //    버프 = 청록/금 계열, 디버프 = 자주/검붉은 계열로 구분한다.
    // ─────────────────────────────────────────────────────

    // Event_BattleWill — 전투 의지 (불끈 쥔 주먹 + 상승 화살, 주황)
    static void DrawEventBattleWill(P p)
    {
        p.BgGradient(Hex("140A03"), Hex("2A1406"));
        p.RoundedBorder(8, 2, Hex("D2761E"));
        var c = new Color32(240, 160, 60, 235);
        // 주먹 (뭉툭한 사각 + 손가락 마디)
        p.FillRRect(15, 22, 18, 15, 4, new Color32(190, 118, 44, 240));
        for (int i = 0; i < 3; i++)
            p.DrawLine(18 + i * 5, 24, 18 + i * 5, 30, new Color32(120, 70, 24, 200), 1);
        // 상승 화살
        p.DrawLine(24, 20, 24, 9, c, 3);
        p.FillTri(24, 5, 18, 13, 30, 13, c);
    }

    // Event_PotionBuff — 활력의 묘약 (플라스크 + 초록 액체 + 기포)
    static void DrawEventPotionBuff(P p)
    {
        p.BgGradient(Hex("03140A"), Hex("062A16"));
        p.RoundedBorder(8, 2, Hex("2ECC71"));
        // 병목
        p.FillRRect(21, 8, 6, 7, 1, new Color32(190, 215, 205, 200));
        // 병 몸통
        p.FillRRect(14, 15, 20, 24, 7, new Color32(30, 60, 48, 235));
        // 액체
        p.FillRRect(16, 24, 16, 13, 5, new Color32(46, 204, 113, 235));
        // 기포
        p.FillCircle(21, 30, 2, new Color32(180, 255, 210, 200));
        p.FillCircle(27, 33, 1, new Color32(180, 255, 210, 180));
        p.FillCircleAlpha(24, 20, 6, new Color32(46, 204, 113, 60));
    }

    // Event_PotionDebuff — 부작용 (엎어진 플라스크 + 하강 화살, 탁한 보라)
    static void DrawEventPotionDebuff(P p)
    {
        p.BgGradient(Hex("120616"), Hex("24102C"));
        p.RoundedBorder(8, 2, Hex("8E44AD"));
        p.FillRRect(21, 8, 6, 7, 1, new Color32(170, 150, 190, 190));
        p.FillRRect(14, 15, 20, 22, 7, new Color32(52, 34, 62, 235));
        p.FillRRect(16, 24, 16, 11, 5, new Color32(142, 68, 173, 225));
        // 하강 화살
        var c = new Color32(200, 120, 230, 235);
        p.DrawLine(38, 26, 38, 36, c, 3);
        p.FillTri(38, 42, 33, 34, 43, 34, c);
    }

    // Event_BloodPact — 피의 계약 (제단 + 핏방울, 검붉은)
    static void DrawEventBloodPact(P p)
    {
        p.BgGradient(Hex("160404"), Hex("2C0808"));
        p.RoundedBorder(8, 2, Hex("C0392B"));
        // 제단 (사다리꼴 느낌의 2단)
        p.FillRRect(12, 32, 24, 8, 2, new Color32(70, 40, 40, 240));
        p.FillRRect(16, 26, 16, 7, 2, new Color32(96, 54, 54, 240));
        // 핏방울
        var b = new Color32(200, 45, 40, 240);
        p.FillCircle(24, 18, 6, b);
        p.FillTri(24, 6, 19, 18, 29, 18, b);
        p.FillCircle(22, 16, 2, new Color32(255, 150, 140, 180));
    }

    // Event_AltarCurse — 제단의 저주 (금 간 방패 + 저주 기운, 자주)
    static void DrawEventAltarCurse(P p)
    {
        p.BgGradient(Hex("120414"), Hex("240826"));
        p.RoundedBorder(8, 2, Hex("9B59B6"));
        FillShieldShape(p, 24, 28, 42, 10, Hex("2A1030"), Hex("3E1A46"));
        DrawShieldOutline(p, 24, 28, 42, 10, new Color32(155, 89, 182, 230), 2);
        // 균열
        var crack = new Color32(220, 140, 240, 235);
        p.DrawLine(24, 12, 19, 22, crack, 2);
        p.DrawLine(19, 22, 27, 28, crack, 2);
        p.DrawLine(27, 28, 22, 38, crack, 2);
    }

    // Event_ExecutionMorale — 처형의 사기 (도끼날 + 사기 상승 광채, 금-적)
    static void DrawEventExecutionMorale(P p)
    {
        p.BgGradient(Hex("160A03"), Hex("2C1606"));
        p.RoundedBorder(8, 2, Hex("E67E22"));
        // 자루
        p.FillRRect(22, 10, 4, 30, 1, new Color32(120, 78, 40, 240));
        // 도끼날 (좌우 대칭 삼각)
        var blade = new Color32(225, 225, 235, 240);
        p.FillTri(22, 12, 22, 26, 8, 19, blade);
        p.FillTri(26, 12, 26, 26, 40, 19, blade);
        // 사기 광채
        p.FillCircleAlpha(24, 19, 15, new Color32(230, 126, 34, 55));
    }

    // Event_SpyInfo — 첩자 정보 (두루마리 + 눈, 청록)
    static void DrawEventSpyInfo(P p)
    {
        p.BgGradient(Hex("03121A"), Hex("062633"));
        p.RoundedBorder(8, 2, Hex("1ABC9C"));
        // 두루마리
        p.FillRRect(10, 14, 28, 20, 3, new Color32(210, 200, 170, 235));
        p.FillRRect(8,  12, 6, 24, 3, new Color32(150, 140, 115, 240));
        p.FillRRect(34, 12, 6, 24, 3, new Color32(150, 140, 115, 240));
        // 눈 (정보)
        p.FillEllipse(24, 24, 9, 5, new Color32(20, 60, 60, 230));
        p.FillCircle(24, 24, 3, new Color32(26, 188, 156, 245));
        p.FillCircle(23, 23, 1, new Color32(220, 255, 250, 220));
    }

    // Event_VeteranHeritage — 노병의 유산 (군화 + 속도선, 청회)
    static void DrawEventVeteranHeritage(P p)
    {
        p.BgGradient(Hex("0A1016"), Hex("14202C"));
        p.RoundedBorder(8, 2, Hex("5DADE2"));
        // 군화
        p.FillRRect(20, 12, 10, 18, 3, new Color32(90, 110, 130, 240));
        p.FillRRect(16, 28, 20, 8, 3, new Color32(60, 78, 96, 240));
        p.FillRRect(16, 34, 22, 4, 2, new Color32(40, 54, 70, 245));
        // 속도선
        var s = new Color32(93, 173, 226, 210);
        p.DrawLine(4, 16, 15, 16, s, 2);
        p.DrawLine(2, 23, 13, 23, s, 2);
        p.DrawLine(5, 30, 14, 30, s, 2);
    }

    // ─────────────────────────────────────────────────────
    //  ■ 특성 아이콘 (48×48)
    // ─────────────────────────────────────────────────────

    // KnightCommand — 지휘관의 기질 (왕관 + 방패, 금-붉은)
    static void DrawTraitKnight(P p)
    {
        p.BgGradient(Hex("1A0808"), Hex("3C1A10"));
        p.RoundedBorder(8, 2, Hex("D4A840"));

        // 방패 (하단, 배경)
        FillShieldShape(p, 24, 28, 44, 10, Hex("2A1808"), Hex("4A2010"));
        DrawShieldOutline(p, 24, 28, 44, 10, Hex("8B5A20"), 1);
        // 방패 중앙 십자
        p.FillRRect(22, 22, 4, 14, 1, new Color32(139, 90, 32, 160));
        p.FillRRect(17, 27, 14, 4, 1, new Color32(139, 90, 32, 160));

        // 왕관 (중상단, 금색)
        p.FillRRect(12, 18, 24, 7, 1, Gold);      // 왕관 베이스
        p.FillRect(12, 18, 24, 3, new Color32(255, 255, 255, 40));  // 상단 하이라이트
        // 세 꼭짓점
        p.FillTri(12, 18, 12, 8, 16, 18, Gold);
        p.FillTri(21, 18, 24, 6, 27, 18, Hex("FFD060"));
        p.FillTri(32, 18, 36, 8, 36, 18, Gold);
        // 왕관 보석
        p.FillCircle(12, 8,  3, Hex("EE4444"));
        p.FillCircle(24, 6,  4, Hex("FFEE33"));
        p.FillCircle(36, 8,  3, Hex("EE4444"));
        p.FillCircleAlpha(23, 5, 2, new Color32(255, 255, 255, 160));

        // 글로우
        p.FillCircleAlpha(24, 14, 14, new Color32(212, 168, 64, 30));
    }

    // ArcherPrecision — 정밀 사수 (조준선 + 화살, 녹색)
    static void DrawTraitArcher(P p)
    {
        p.BgGradient(Hex("020E02"), Hex("0E2A0E"));
        p.RoundedBorder(8, 2, Hex("44AA22"));

        // 외부 조준 원
        p.DrawCircle(24, 26, 14, 2, Hex("44DD55"));
        p.FillCircleAlpha(24, 26, 14, new Color32(68, 221, 85, 18));
        // 십자선
        p.DrawLine(24, 8,  24, 16, Hex("44DD55"), 2);
        p.DrawLine(24, 36, 24, 44, Hex("44DD55"), 2);
        p.DrawLine(6,  26, 14, 26, Hex("44DD55"), 2);
        p.DrawLine(34, 26, 42, 26, Hex("44DD55"), 2);
        // 내부 소원
        p.DrawCircle(24, 26, 6, 1, new Color32(68, 221, 85, 140));

        // 화살 (우상향 45도, 조준선 위로 강조)
        p.DrawLine(10, 40, 36, 12, Gold, 3);
        p.DrawLine(10, 40, 36, 12, Tint(Gold, White, 0.4f), 1);
        p.FillTri(36, 12, 29, 14, 34, 19, Silver);   // 화살촉
        p.FillTri(10, 40, 16, 38, 12, 44, Green);     // 깃 1
        p.FillTri(10, 40, 14, 44, 8,  44, Tint(Green, Hex("000000"), 0.3f)); // 깃 2

        // 중심 조준점
        p.FillCircle(24, 26, 3, Hex("FFEE22"));
        p.FillCircleAlpha(23, 25, 1, new Color32(255, 255, 255, 200));
    }

    // MageArcane — 마력 집중 (마법 구슬 + 번개, 보라)
    static void DrawTraitMage(P p)
    {
        p.BgGradient(Hex("060220"), Hex("180A50"));
        p.RoundedBorder(8, 2, Hex("9933FF"));

        // 구슬 글로우
        p.FillCircleAlpha(22, 26, 16, new Color32(102, 68, 255, 35));
        // 구슬 본체
        p.FillCircleGrad(22, 26, 11, Hex("AACCFF"), Hex("6644FF"), Hex("2211AA"));
        // 구슬 하이라이트
        p.FillCircleAlpha(17, 21, 4, new Color32(255, 255, 255, 90));
        // 구슬 내부 마법선
        p.DrawCircle(22, 26, 5, 1, new Color32(200, 170, 255, 90));
        p.DrawLine(22, 16, 22, 36, new Color32(200, 170, 255, 70), 1);
        p.DrawLine(12, 26, 32, 26, new Color32(200, 170, 255, 70), 1);

        // 번개 (우상 → 좌하, 강조)
        p.FillTri(36, 8,  30, 22, 38, 22, Hex("FFEE33"));
        p.FillTri(34, 22, 42, 22, 36, 38, Hex("FFEE33"));
        p.DrawLine(36, 8,  30, 22, Hex("FFFFFF"), 1);
        p.DrawLine(42, 22, 36, 38, Hex("FFFFAA"), 1);

        // 마법 파티클
        p.FillCircle(8,  14, 2, new Color32(170, 136, 255, 190));
        p.FillCircle(44, 34, 2, new Color32(136, 170, 255, 170));
        p.FillCircle(6,  36, 2, new Color32(204, 136, 255, 150));
        p.FillCircleAlpha(22, 26, 18, new Color32(102, 68, 255, 18));
    }

    // ShieldFortress — 강철 요새 (성벽 + 방패, 청록)
    static void DrawTraitShield(P p)
    {
        p.BgGradient(Hex("021010"), Hex("083030"));
        p.RoundedBorder(8, 2, Hex("22BBCC"));

        var tc   = Hex("33DDEE");
        var dark = Hex("0E2828");
        var mid  = Hex("1A4A4A");

        // 성벽 (상단 — 흉벽 3칸)
        p.FillRRect(6, 8, 36, 18, 1, mid);
        p.FillRect(6, 8, 36, 5, new Color32(255, 255, 255, 18));
        // 흉벽 (톱니)
        for (int i = 0; i < 3; i++)
        {
            int bx = 8 + i * 12;
            p.FillRect(bx, 4, 8, 6, mid);
            p.FillRect(bx, 4, 8, 2, new Color32(255, 255, 255, 18));
        }
        // 성벽 테두리
        p.DrawLine(6,  26, 42, 26, tc, 1);
        p.DrawLine(6,  8,  6,  26, tc, 1);
        p.DrawLine(42, 8,  42, 26, tc, 1);

        // 성문 (하단 중앙)
        p.FillRRect(18, 18, 12, 10, 2, dark);
        p.DrawLine(18, 18, 30, 18, tc, 1);
        p.DrawLine(18, 18, 18, 28, tc, 1);
        p.DrawLine(30, 18, 30, 28, tc, 1);

        // 방패 (전면, 성벽 위에 겹침)
        FillShieldShape(p, 24, 30, 46, 14, Hex("0E2020"), Hex("1A4040"));
        DrawShieldOutline(p, 24, 30, 46, 14, tc, 2);
        // 방패 내부 선
        DrawShieldOutline(p, 24, 34, 44, 18, new Color32(51, 221, 238, 55), 1);
        // 방패 중앙 십자
        p.FillRRect(23, 26, 3, 16, 1, tc);
        p.FillRRect(17, 32, 15, 3, 1, tc);
        p.FillCircle(24, 34, 3, Hex("AACCDD"));
        p.FillCircleAlpha(23, 33, 1, new Color32(255, 255, 255, 160));
    }

    // KnightSoldierRage — 전우의 분노 (쓰러진 병사 실루엣 + 붉은 분노 오라)
    static void DrawTraitKnightSoldierRage(P p)
    {
        p.BgGradient(Hex("180404"), Hex("3A0A0A"));
        p.RoundedBorder(8, 2, Hex("CC2222"));

        // 분노 오라 (붉은 광환)
        p.FillCircleAlpha(24, 26, 18, new Color32(200, 40, 40, 55));
        p.DrawCircle(24, 26, 16, 1, new Color32(220, 60, 60, 100));

        // 병사 실루엣 (누운 자세, 하단)
        p.FillCircle(14, 36, 4, new Color32(80, 20, 20, 220));
        p.FillRRect(10, 39, 20, 5, 2, new Color32(80, 20, 20, 200));
        p.DrawCircle(14, 36, 4, 1, Hex("CC3333"));

        // 상승 파워 화살표 (붉은)
        var rc = Hex("FF3333");
        p.DrawLine(24, 34, 24, 12, rc, 3);
        p.FillTri(24, 6, 17, 14, 31, 14, rc);
        p.FillCircleAlpha(24, 10, 7, new Color32(255, 60, 60, 70));

        // 방사 스파크
        p.DrawLine(24, 10, 12, 4,  new Color32(255, 80, 80, 120), 1);
        p.DrawLine(24, 10, 36, 4,  new Color32(255, 80, 80, 120), 1);
        p.DrawLine(24, 10, 6,  18, new Color32(255, 80, 80, 80),  1);
        p.DrawLine(24, 10, 42, 18, new Color32(255, 80, 80, 80),  1);

        // 체력 +% 심볼 (우하단 작은 하트)
        FillHeart(p, 38, 40, 5, Hex("CC2233"));
        p.FillCircleAlpha(36, 37, 2, new Color32(255, 160, 160, 120));
    }

    // KnightHeroReturn — 영웅의 귀환 (봉황/부활 날개 + 병사 소환)
    static void DrawTraitKnightHeroReturn(P p)
    {
        p.BgGradient(Hex("140A02"), Hex("382010"));
        p.RoundedBorder(8, 2, Hex("D4A840"));

        // 봉황 날개 왼쪽
        p.FillTri(24, 24, 6, 10, 10, 30,  Hex("AA6010"));
        p.FillTri(24, 24, 6, 10, 16, 14,  Hex("CC8820"));
        p.FillTri(24, 24, 8, 20, 12, 32,  Hex("BB7020"));
        // 봉황 날개 오른쪽
        p.FillTri(24, 24, 42, 10, 38, 30, Hex("AA6010"));
        p.FillTri(24, 24, 42, 10, 32, 14, Hex("CC8820"));
        p.FillTri(24, 24, 40, 20, 36, 32, Hex("BB7020"));

        // 불꽃 중심 글로우
        p.FillCircleAlpha(24, 22, 10, new Color32(255, 180, 40, 60));
        // 봉황 몸체
        p.FillCircle(24, 18, 6, Hex("FFD060"));
        p.FillCircleAlpha(22, 16, 3, new Color32(255, 255, 200, 120));

        // 상승 불꽃
        p.FillTri(21, 18, 27, 18, 24, 6, Hex("FF8820"));
        p.FillTri(22, 14, 26, 14, 24, 6, Hex("FFEE44"));

        // 병사 실루엣 (소환, 하단 좌우)
        var sc = new Color32(180, 130, 40, 160);
        p.FillCircle(12, 40, 3, sc);
        p.FillRRect(9,  42, 6, 5, 1, sc);
        p.FillCircle(36, 40, 3, sc);
        p.FillRRect(33, 42, 6, 5, 1, sc);
    }

    // ArcherRetreatFire — 퇴각 사격 (뒤로 향하는 발자국 + 앞으로 향하는 화살)
    static void DrawTraitArcherRetreatFire(P p)
    {
        p.BgGradient(Hex("020E02"), Hex("0E2808"));
        p.RoundedBorder(8, 2, Hex("44AA22"));

        var gc = Hex("44DD55");

        // 후퇴 화살표 (왼쪽 방향)
        p.DrawLine(38, 26, 18, 26, gc, 2);
        p.FillTri(14, 26, 22, 20, 22, 32, gc);

        // 앞으로 향하는 화살 (오른쪽 위, 대각선)
        p.DrawLine(12, 40, 40, 12, Gold, 3);
        p.DrawLine(12, 40, 40, 12, Tint(Gold, White, 0.4f), 1);
        p.FillTri(40, 12, 33, 14, 38, 19, Silver);  // 화살촉
        p.FillTri(12, 40, 18, 38, 14, 44, Green);    // 깃

        // 분리 강조선
        p.DrawLine(24, 6, 24, 44, new Color32(255, 255, 255, 30), 1);

        // 발자국 (후퇴 방향)
        p.FillEllipse(34, 36, 3, 4, new Color32(68, 221, 85, 110));
        p.FillEllipse(30, 42, 3, 4, new Color32(68, 221, 85, 80));
    }

    // ArcherRainFire — 폭우 사격 (여러 화살 부채꼴)
    static void DrawTraitArcherRainFire(P p)
    {
        p.BgGradient(Hex("020A04"), Hex("082010"));
        p.RoundedBorder(8, 2, Hex("33AA44"));

        var gc = Hex("44DD55");

        // 부채꼴 화살 3발 (아래에서 위 방향)
        // 중앙 화살
        p.DrawLine(24, 44, 24, 10, Silver, 3);
        p.DrawLine(24, 44, 24, 10, Tint(Silver, White, 0.4f), 1);
        p.FillTri(24, 6, 20, 14, 28, 14, Silver);

        // 왼쪽 화살
        p.DrawLine(24, 44, 10, 16, Tint(Silver, Hex("000000"), 0.2f), 2);
        p.FillTri(10, 12, 7, 18, 14, 18, Tint(Silver, Hex("000000"), 0.2f));

        // 오른쪽 화살
        p.DrawLine(24, 44, 38, 16, Tint(Silver, Hex("000000"), 0.2f), 2);
        p.FillTri(38, 12, 34, 18, 41, 18, Tint(Silver, Hex("000000"), 0.2f));

        // 활 (하단)
        p.DrawLine(12, 44, 36, 44, gc, 2);
        p.FillCircle(24, 44, 3, gc);

        // 에너지 방사선
        p.FillCircleAlpha(24, 14, 8, new Color32(68, 221, 85, 40));
    }

    // MageAttackCdr — 마법 집중 (시계 + 마법 감소 화살)
    static void DrawTraitMageAttackCdr(P p)
    {
        p.BgGradient(Hex("060220"), Hex("180A50"));
        p.RoundedBorder(8, 2, Hex("9933FF"));

        var mc = Hex("BB66FF");

        // 시계 외곽
        p.DrawCircle(24, 26, 14, 2, mc);
        p.FillCircleAlpha(24, 26, 14, new Color32(100, 60, 200, 22));
        // 시계 눈금
        p.DrawLine(24, 13, 24, 16, mc, 2);
        p.DrawLine(24, 36, 24, 39, mc, 2);
        p.DrawLine(11, 26, 14, 26, mc, 2);
        p.DrawLine(34, 26, 37, 26, mc, 2);
        // 시계 침 (빠름 - 분침이 앞으로)
        p.DrawLine(24, 26, 24, 15, Hex("CCAAFF"), 2);
        p.DrawLine(24, 26, 33, 22, Hex("CCAAFF"), 2);
        p.FillCircle(24, 26, 2, Hex("CCAAFF"));

        // 마법 공격 화살 (우상, 강조)
        p.DrawLine(24, 26, 44, 8, Hex("FFEE33"), 3);
        p.FillTri(44, 8, 36, 10, 42, 16, Hex("FFEE33"));
        p.FillCircleAlpha(40, 12, 6, new Color32(255, 238, 51, 60));

        // CDR 감소 표시 (하향 화살)
        p.DrawLine(7, 34, 7, 44, mc, 2);
        p.FillTri(7, 47, 3, 41, 11, 41, mc);
    }

    // MageEchoSkill — 연속 시전 (두 개의 겹친 마법 오브)
    static void DrawTraitMageEchoSkill(P p)
    {
        p.BgGradient(Hex("060220"), Hex("180A50"));
        p.RoundedBorder(8, 2, Hex("9933FF"));

        // 오브 1 (뒤, 약함)
        p.FillCircleAlpha(30, 22, 10, new Color32(80, 50, 200, 80));
        p.DrawCircle(30, 22, 9, 1, new Color32(136, 80, 220, 150));
        p.FillCircleAlpha(27, 19, 4, new Color32(180, 150, 255, 80));

        // 오브 2 (앞, 강함)
        p.FillCircleGrad(18, 30, 11, Hex("AACCFF"), Hex("6644FF"), Hex("2211AA"));
        p.FillCircleAlpha(14, 26, 4, new Color32(255, 255, 255, 90));
        p.DrawCircle(18, 30, 5, 1, new Color32(200, 170, 255, 90));

        // 연결 흔적선 (에코)
        p.DrawLine(18, 30, 30, 22, new Color32(180, 140, 255, 100), 2);
        p.FillCircleAlpha(24, 26, 4, new Color32(150, 100, 255, 80));

        // 피해 감소 표시 (-40% — 화살 아래)
        var dc = new Color32(180, 130, 255, 170);
        p.DrawLine(38, 34, 38, 44, dc, 2);
        p.DrawLine(34, 38, 38, 44, dc, 2);
        p.DrawLine(42, 38, 38, 44, dc, 2);
    }

    // ShieldCounterBlow — 반격의 달인 (방패 + 반사 화살)
    static void DrawTraitShieldCounterBlow(P p)
    {
        p.BgGradient(Hex("021010"), Hex("063030"));
        p.RoundedBorder(8, 2, Hex("22BBCC"));

        var tc = Hex("33DDEE");

        // 방패 본체
        FillShieldShape(p, 20, 8, 38, 12, Hex("0A1A2A"), Hex("123050"));
        DrawShieldOutline(p, 20, 8, 38, 12, tc, 2);
        // 방패 내부 거울 광택
        p.FillCircleAlpha(20, 26, 8, new Color32(100, 220, 255, 50));
        p.DrawLine(20, 16, 20, 36, new Color32(200, 240, 255, 100), 1);
        p.DrawLine(10, 26, 30, 26, new Color32(200, 240, 255, 100), 1);

        // 화살 공격 (왼쪽에서 방패 향해)
        p.DrawLine(44, 26, 30, 26, Hex("CC4444"), 3);
        p.FillTri(28, 26, 34, 21, 34, 31, Hex("CC4444"));

        // 반사 화살 (방패에서 되돌아감)
        p.DrawLine(10, 26, 4, 16, tc, 2);
        p.FillTri(4, 14, 1, 20, 8, 20, tc);
        p.DrawLine(10, 26, 4, 36, tc, 2);
        p.FillTri(4, 38, 1, 32, 8, 32, tc);

        // 반사 임팩트
        p.FillCircleAlpha(20, 26, 12, new Color32(51, 221, 238, 40));
    }

    // ShieldRageBuild — 분노 축적 (쌓이는 불꽃 바 + 주먹)
    static void DrawTraitShieldRageBuild(P p)
    {
        p.BgGradient(Hex("020808"), Hex("102020"));
        p.RoundedBorder(8, 2, Hex("22BBCC"));

        // 스택 바 (하단, 5개 — 절반 채움)
        Color32[] barColors =
        {
            Hex("33DDEE"), Hex("33DDEE"), Hex("44BBEE"),
            new Color32(51, 80, 80, 120), new Color32(51, 80, 80, 80),
        };
        for (int i = 0; i < 5; i++)
        {
            int bx = 6 + i * 8;
            p.FillRRect(bx, 38, 6, 6, 1, barColors[i]);
        }

        // 주먹 (중앙, 방패병 상징)
        var fc = Hex("33DDEE");
        p.FillRRect(15, 14, 18, 8, 2, fc);   // 손가락 마디
        p.FillRRect(14, 22, 20, 12, 2, fc);  // 주먹 몸체
        p.FillRect(14, 22, 20, 4, new Color32(255, 255, 255, 25));
        p.DrawLine(19, 22, 19, 34, new Color32(0, 0, 0, 60), 1);
        p.DrawLine(24, 22, 24, 34, new Color32(0, 0, 0, 60), 1);
        p.DrawLine(29, 22, 29, 34, new Color32(0, 0, 0, 60), 1);

        // 분노 에너지 방사 (불꽃)
        var rc = Hex("FF5522");
        p.DrawLine(24, 16, 24, 6,  rc, 2);
        p.DrawLine(24, 16, 14, 8,  new Color32(255, 85, 34, 130), 1);
        p.DrawLine(24, 16, 34, 8,  new Color32(255, 85, 34, 130), 1);
        p.FillCircleAlpha(24, 16, 7, new Color32(255, 80, 30, 50));

        // 공격력 상승 화살 (우상단)
        p.DrawLine(38, 42, 44, 28, rc, 2);
        p.FillTri(44, 25, 40, 30, 47, 30, rc);
    }

    // CommonExpedition — 원정 편성 (군기 + 플러스, 황금 중립)
    static void DrawTraitCommonExpedition(P p)
    {
        p.BgGradient(Hex("0E0A02"), Hex("261C06"));
        p.RoundedBorder(8, 2, Hex("D4A840"));

        // 깃대
        p.FillRRect(15, 6, 3, 38, 1, Hex("7A5010"));
        p.FillRect(15, 6, 1, 36, new Color32(255, 255, 255, 22));

        // 군기 (삼각)
        p.FillTri(18, 6, 18, 28, 44, 17, Hex("C89020"));
        p.DrawLine(18, 6, 44, 17, new Color32(255, 220, 100, 80), 1);
        p.DrawLine(18, 28, 44, 17, new Color32(0, 0, 0, 50), 1);

        // 깃대 상단 구체
        p.FillCircle(16, 5, 4, Gold);
        p.FillCircleAlpha(15, 4, 2, new Color32(255, 255, 200, 140));

        // 플러스 기호 (우하단)
        var pc = Hex("D4A840");
        p.FillRRect(26, 38, 14, 4, 1, pc);
        p.FillRRect(31, 33, 4, 14, 1, pc);

        p.FillCircleAlpha(16, 17, 12, new Color32(212, 168, 64, 22));
    }

    // CommonMassMobilize — 대규모 동원 (쌍 군기 + 하향 화살, 주황-적)
    static void DrawTraitCommonMassMobilize(P p)
    {
        p.BgGradient(Hex("120402"), Hex("2C0A04"));
        p.RoundedBorder(8, 2, Hex("EE6622"));

        // 왼쪽 깃발
        p.FillRRect(7, 6, 3, 28, 1, Hex("7A3010"));
        p.FillTri(10, 6, 10, 24, 26, 15, Hex("EE6622"));
        p.DrawLine(10, 6, 26, 15, new Color32(255, 160, 80, 80), 1);

        // 오른쪽 깃발
        p.FillRRect(25, 6, 3, 28, 1, Hex("7A3010"));
        p.FillTri(28, 6, 28, 24, 44, 15, Hex("CC4A0A"));
        p.DrawLine(28, 6, 44, 15, new Color32(255, 140, 60, 80), 1);

        // 능력치 감소 — 붉은 하향 화살표
        var dc = Hex("FF3333");
        p.FillRRect(18, 34, 12, 3, 1, dc);       // 마이너스 기호
        p.DrawLine(24, 37, 24, 43, dc, 3);         // 줄기
        p.FillTri(18, 41, 30, 41, 24, 47, dc);    // 화살촉

        p.FillCircleAlpha(24, 42, 8, new Color32(255, 60, 30, 40));
    }

    // CommonSoldierSupply — 병사 지원령 (배치 슬롯 프레임 + 병사 실루엣, 강청)
    static void DrawTraitCommonSoldierSupply(P p)
    {
        p.BgGradient(Hex("040C14"), Hex("0C2030"));
        p.RoundedBorder(8, 2, Hex("4488BB"));

        var fc = Hex("4488BB");

        // 배치 슬롯 프레임 (상단)
        p.FillRRect(8, 6, 32, 24, 3, Hex("0A2040"));
        p.DrawLine(8,  6, 40,  6, fc, 1);
        p.DrawLine(8,  6,  8, 30, fc, 1);
        p.DrawLine(40, 6, 40, 30, fc, 1);
        p.DrawLine(8, 30, 40, 30, fc, 1);

        // 슬롯 안 플러스
        p.FillRRect(20, 15, 8, 3, 1, fc);
        p.FillRRect(23, 12, 3, 9, 1, fc);
        p.FillCircleAlpha(24, 18, 6, new Color32(68, 136, 187, 40));

        // 병사 실루엣 3개 (하단)
        var sc = Hex("5599CC");
        p.FillCircle(13, 38, 3, sc);
        p.FillRRect(10, 41, 6, 6, 1, sc);
        p.FillCircle(24, 38, 3, sc);
        p.FillRRect(21, 41, 6, 6, 1, sc);
        p.FillCircle(35, 38, 3, sc);
        p.FillRRect(32, 41, 6, 6, 1, sc);
    }

    // CommonForcedLevy — 무리한 징집 (사슬 + 이동속도 감소, 어두운 회색)
    static void DrawTraitCommonForcedLevy(P p)
    {
        p.BgGradient(Hex("080808"), Hex("181820"));
        p.RoundedBorder(8, 2, Hex("888899"));

        var cc = Hex("9999AA");
        var ccH = new Color32(200, 200, 220, 80);

        // 사슬 링크 1 (좌상, 가로 타원형)
        p.DrawCircle(16, 18, 8, 3, cc);
        p.FillCircleAlpha(16, 18, 8, new Color32(120, 120, 140, 30));
        p.DrawCircle(16, 18, 6, 1, ccH);

        // 사슬 링크 2 (우하, 가로 타원형)
        p.DrawCircle(32, 28, 8, 3, cc);
        p.FillCircleAlpha(32, 28, 8, new Color32(120, 120, 140, 30));
        p.DrawCircle(32, 28, 6, 1, ccH);

        // 연결 사슬 중앙부 (고리가 겹치는 구간)
        p.FillRRect(20, 20, 8, 8, 1, Hex("080808")); // 배경으로 가려 고리 연결 효과

        // 이동속도 감소 (하단, 왼쪽 화살 + 빨간 X)
        var dc = Hex("EE3333");
        p.DrawLine(6, 42, 36, 42, dc, 2);
        p.FillTri(4, 42, 12, 38, 12, 46, dc);         // 왼쪽 화살촉

        // 빨간 X (속도 막힘)
        p.DrawLine(38, 38, 44, 44, dc, 2);
        p.DrawLine(44, 38, 38, 44, dc, 2);

        // 슬롯 +1 표시 (좌상단 작은)
        var pc = Hex("888899");
        p.FillRRect(5, 6, 10, 3, 1, pc);
        p.FillRRect(9, 3, 3, 9, 1, pc);
    }

    // CommonEquipExpand — 중무장 편성 (장비 슬롯 2→3, 강철 청색)
    static void DrawTraitCommonEquipExpand(P p)
    {
        p.BgGradient(Hex("060C18"), Hex("121E34"));
        p.RoundedBorder(8, 2, Hex("5599CC"));

        var fc   = Hex("5599CC");
        var fcH  = Hex("88CCFF");
        var gold = Hex("D4A840");

        // 장비 슬롯 1 (좌, 기존)
        p.FillRRect(4, 8, 16, 18, 2, Hex("0A1828"));
        p.DrawLine(4,  8,  20,  8, fc, 1);
        p.DrawLine(4,  8,   4, 26, fc, 1);
        p.DrawLine(20, 8,  20, 26, fc, 1);
        p.DrawLine(4, 26,  20, 26, fc, 1);
        // 슬롯 1 내부 — 검 심볼
        p.FillRRect(11, 10, 2, 10, 1, new Color32(136, 204, 255, 150));
        p.FillRRect(8,  14, 8,  2,  1, new Color32(136, 204, 255, 150));
        p.FillTri(11, 9, 13, 9, 12, 6, new Color32(200, 230, 255, 180));

        // 장비 슬롯 2 (중앙, 기존)
        p.FillRRect(16, 8, 16, 18, 2, Hex("0A1828"));
        p.DrawLine(16,  8,  32,  8, fc, 1);
        p.DrawLine(16,  8,  16, 26, fc, 1);
        p.DrawLine(32,  8,  32, 26, fc, 1);
        p.DrawLine(16, 26,  32, 26, fc, 1);
        // 슬롯 2 내부 — 방패 심볼
        p.FillRRect(22, 11, 2, 10, 1, new Color32(136, 204, 255, 150));
        p.FillRRect(19, 15, 8,  2,  1, new Color32(136, 204, 255, 150));
        p.FillCircleAlpha(24, 17, 5, new Color32(100, 180, 255, 50));

        // 장비 슬롯 3 (우측, 신규 잠금해제)
        p.FillRRect(28, 8, 16, 18, 2, Hex("0A1E10"));
        p.DrawLine(28,  8, 44,  8, gold, 1);
        p.DrawLine(28,  8, 28, 26, gold, 1);
        p.DrawLine(44,  8, 44, 26, gold, 1);
        p.DrawLine(28, 26, 44, 26, gold, 1);
        // 슬롯 3 내부 — 플러스 (잠금해제)
        p.FillRRect(35, 12, 4,  3, 1, gold);
        p.FillRRect(33, 14, 8,  3, 1, gold);
        p.FillCircleAlpha(36, 16, 5, new Color32(212, 168, 64, 55));

        // 화살표 (슬롯 2→3 사이)
        p.DrawLine(28, 17, 26, 17, fcH, 1);
        p.FillTri(24, 17, 27, 14, 27, 20, fcH);

        // 하단 장갑/갑옷 실루엣
        var ac = new Color32(85, 153, 204, 140);
        // 갑옷 상의 — 역사다리꼴
        p.FillRRect(10, 30, 28, 14, 2, Hex("0C1A2A"));
        p.FillRRect(10, 30, 28, 4, 1, new Color32(85, 153, 204, 50));
        p.DrawLine(10, 30, 38, 30, fc, 1);
        p.DrawLine(10, 44, 38, 44, fc, 1);
        p.DrawLine(10, 30, 10, 44, fc, 1);
        p.DrawLine(38, 30, 38, 44, fc, 1);
        // 갑옷 중앙 장식선
        p.DrawLine(24, 30, 24, 44, new Color32(85, 153, 204, 70), 1);
        p.DrawLine(14, 36, 34, 36, new Color32(85, 153, 204, 70), 1);
        // 어깨 장갑 (좌우)
        p.FillRRect(4,  30, 8, 10, 2, Hex("0C1A2A"));
        p.DrawLine(4, 30, 12, 30, fc, 1);
        p.DrawLine(4, 30, 4,  40, fc, 1);
        p.DrawLine(4, 40, 12, 40, fc, 1);
        p.FillRRect(36, 30, 8, 10, 2, Hex("0C1A2A"));
        p.DrawLine(36, 30, 44, 30, fc, 1);
        p.DrawLine(44, 30, 44, 40, fc, 1);
        p.DrawLine(36, 40, 44, 40, fc, 1);

        // 골드 글로우 (우측 신규 슬롯)
        p.FillCircleAlpha(36, 17, 10, new Color32(212, 168, 64, 22));
    }

    // ─────────────────────────────────────────────────────
    //  ■ 직업 시너지 아이콘 (48×48)
    // ─────────────────────────────────────────────────────

    // 선봉대 (1001) — 기사+궁수: 검과 화살 교차
    static void DrawSynergyVanguardCross(P p)
    {
        p.BgGradient(Hex("0A0804"), Hex("1C1408"));
        p.RoundedBorder(8, 2, Hex("8A7040"));
        // 검 (좌상→우하)
        p.DrawLine(8, 10, 36, 40, Silver, 3);
        p.DrawLine(8, 10, 36, 40, Tint(Silver, White, 0.4f), 1);
        p.FillTri(8, 10, 12, 18, 16, 12, Silver);
        p.FillRRect(30, 36, 7, 3, 1, Gold);
        // 화살 (좌하→우상, 교차)
        p.DrawLine(8, 38, 40, 8, Hex("44DD55"), 3);
        p.FillTri(40, 8, 34, 12, 36, 18, Hex("44DD55"));
        p.FillTri(8, 38, 14, 36, 10, 44, Green);
        // 교차점
        p.FillCircleAlpha(24, 24, 6, new Color32(220, 220, 80, 70));
    }

    // 마법 방패 (1002) — 기사+법사: 검과 마법 오브
    static void DrawSynergyMagicShield(P p)
    {
        p.BgGradient(Hex("090414"), Hex("1A0A30"));
        p.RoundedBorder(8, 2, Hex("882299"));
        // 검 (왼쪽 세로)
        p.FillTri(14, 6, 10, 13, 18, 13, Silver);
        p.FillRRect(12, 13, 4, 16, 1, Silver);
        p.FillRRect(7, 27, 14, 3, 1, Gold);
        p.FillRRect(13, 30, 2, 6, 1, Wood);
        // 법사 오브 (오른쪽)
        p.FillCircleGrad(34, 26, 11, Hex("BBDDFF"), Hex("6644FF"), Hex("2211AA"));
        p.FillCircleAlpha(30, 22, 4, new Color32(255, 255, 255, 90));
        p.FillCircleAlpha(34, 26, 13, new Color32(100, 60, 220, 28));
        // 연결 마법선
        p.DrawLine(18, 24, 24, 22, new Color32(180, 140, 255, 120), 1);
    }

    // 철벽진 (1003) — 기사+방패병: 방패 뒤에서 나온 검
    static void DrawSynergyIronWallLine(P p)
    {
        p.BgGradient(Hex("050C0C"), Hex("0D2020"));
        p.RoundedBorder(8, 2, Hex("3A7088"));
        var tc = Hex("33DDEE");
        // 검 (뒤, 왼쪽 기울어진 투명)
        var sc2 = new Color32(200, 190, 170, 170);
        p.DrawLine(12, 6, 18, 38, sc2, 3);
        p.FillTri(12, 6, 8, 14, 16, 14, sc2);
        // 방패 (앞, 오른쪽)
        FillShieldShape(p, 30, 8, 42, 12, Hex("0A2020"), Hex("163434"));
        DrawShieldOutline(p, 30, 8, 42, 12, tc, 2);
        p.FillRRect(29, 12, 3, 14, 1, tc);
        p.FillRRect(23, 20, 14, 3, 1, tc);
        p.FillCircleAlpha(30, 22, 5, new Color32(51, 221, 238, 35));
        // 기사 붉은 하이라이트
        p.FillCircle(12, 6, 3, Hex("CC3333"));
    }

    // 균형의 군세 (1004) — 전 직업: 4색 사분면 + 중앙 왕관
    static void DrawSynergyBalancedHost(P p)
    {
        // 4색 사분면 배경
        p.BgGradient(Hex("080808"), Hex("101010"));
        p.FillRect(6,  6, 18, 18, Hex("4A1212")); // 좌상 = Knight (빨강)
        p.FillRect(24, 6, 18, 18, Hex("124A12")); // 우상 = Archer (녹색)
        p.FillRect(6, 24, 18, 18, Hex("1A1044")); // 좌하 = Mage (보라)
        p.FillRect(24,24, 18, 18, Hex("104040")); // 우하 = Shield (청록)
        p.RoundedBorder(8, 2, Hex("CCCC88"));
        // 중앙 구분선
        p.DrawLine(24, 6, 24, 42, new Color32(30, 30, 30, 180), 1);
        p.DrawLine(6, 24, 42, 24, new Color32(30, 30, 30, 180), 1);
        // 미니 직업 심볼
        p.DrawLine(10, 10, 18, 18, Silver, 2);        // 기사: 검
        p.DrawLine(38, 10, 30, 18, Hex("44DD55"), 2); // 궁수: 화살
        p.FillCircle(14, 34, 5, Hex("9933FF"));       // 법사: 오브
        FillShieldShape(p, 34, 28, 40, 12, Hex("104040"), Hex("1A5050"));
        DrawShieldOutline(p, 34, 28, 40, 12, Hex("33DDEE"), 1);
        // 중앙 왕관 (균형 심볼)
        p.FillCircle(24, 24, 7, Hex("C89020"));
        p.FillCircleAlpha(22, 22, 3, new Color32(255, 248, 180, 140));
    }

    // 기사단 (1011) — 기사×5: 5개 검 부채꼴
    static void DrawSynergyKnightOrder(P p)
    {
        p.BgGradient(Hex("1C0404"), Hex("400C0C"));
        p.RoundedBorder(8, 2, Hex("CC4444"));
        p.FillCircleAlpha(24, 32, 20, new Color32(180, 40, 40, 30));
        // 5개 검, 부채꼴 (하단 중심에서 방사)
        (int x2, int y2)[] tips = { (10, 6), (17, 4), (24, 6), (31, 4), (38, 6) };
        for (int i = 0; i < 5; i++)
        {
            p.DrawLine(24, 42, tips[i].x2, tips[i].y2, Silver, 2);
            p.FillTri(tips[i].x2, tips[i].y2,
                      tips[i].x2 - 3, tips[i].y2 + 5,
                      tips[i].x2 + 3, tips[i].y2 + 5, Silver);
        }
        // 손잡이 영역 강조
        p.FillRRect(16, 38, 16, 4, 1, Gold);
        p.FillCircleAlpha(24, 42, 8, new Color32(212, 168, 64, 50));
    }

    // 화살의 군단 (1012) — 궁수×5: 5개 화살 부채꼴
    static void DrawSynergyArrowLegion(P p)
    {
        p.BgGradient(Hex("031003"), Hex("0A280A"));
        p.RoundedBorder(8, 2, Hex("44CC44"));
        p.FillCircleAlpha(24, 32, 20, new Color32(40, 180, 40, 25));
        var gc3 = Hex("44DD55");
        // 5개 화살 부채꼴
        (int x2, int y2)[] pts = { (8, 8), (15, 4), (24, 6), (33, 4), (40, 8) };
        for (int i = 0; i < 5; i++)
        {
            p.DrawLine(24, 42, pts[i].x2, pts[i].y2, gc3, 2);
            p.FillTri(pts[i].x2, pts[i].y2,
                      pts[i].x2 - 3, pts[i].y2 + 6,
                      pts[i].x2 + 3, pts[i].y2 + 6, gc3);
        }
        // 활 (하단)
        p.FillRRect(12, 40, 24, 4, 2, Hex("7A4020"));
        p.FillCircle(24, 42, 3, gc3);
    }

    // 대법사단 (1013) — 법사×5: 중앙 오브 + 4 위성 오브
    static void DrawSynergyGreatMageCorp(P p)
    {
        p.BgGradient(Hex("070220"), Hex("160850"));
        p.RoundedBorder(8, 2, Hex("9933FF"));
        p.FillCircleAlpha(24, 24, 22, new Color32(80, 40, 200, 25));
        // 중앙 오브
        p.FillCircleGrad(24, 24, 9, Hex("BBDDFF"), Hex("6644FF"), Hex("2211AA"));
        p.FillCircleAlpha(20, 20, 4, new Color32(255, 255, 255, 90));
        // 4개 위성 오브 (십자 배치)
        int[] ox = { 24, 10, 38, 24 };
        int[] oy = { 8, 24, 24, 40 };
        for (int i = 0; i < 4; i++)
        {
            p.FillCircle(ox[i], oy[i], 5, Hex("4422BB"));
            p.FillCircleAlpha(ox[i] - 1, oy[i] - 1, 2, new Color32(200, 170, 255, 90));
            p.DrawLine(ox[i], oy[i], 24, 24, new Color32(120, 80, 255, 80), 1);
        }
        // 번개 장식 (우하단)
        p.FillTri(38, 30, 34, 40, 40, 40, Hex("FFEE33"));
    }

    // 철옹성 (1014) — 방패병×5: 두꺼운 성곽
    static void DrawSynergyIronclad(P p)
    {
        p.BgGradient(Hex("020C0C"), Hex("062020"));
        p.RoundedBorder(8, 2, Hex("22CCCC"));
        var tc2 = Hex("33DDEE");
        var mid2 = Hex("1A4040");
        // 5개 흉벽 (성벽 상단)
        for (int i = 0; i < 5; i++)
        {
            int bx = 5 + i * 8;
            p.FillRRect(bx, 6, 5, 8, 1, mid2);
            p.DrawLine(bx, 6, bx + 5, 6, tc2, 1);
            p.DrawLine(bx, 6, bx, 14, tc2, 1);
            p.DrawLine(bx + 5, 6, bx + 5, 14, tc2, 1);
        }
        // 성벽 몸체
        p.FillRRect(5, 14, 38, 14, 1, mid2);
        p.DrawLine(5, 14, 43, 14, tc2, 1);
        p.DrawLine(5, 28, 43, 28, tc2, 1);
        p.DrawLine(5, 14, 5, 28, tc2, 1);
        p.DrawLine(43, 14, 43, 28, tc2, 1);
        // 성문
        p.FillRRect(18, 20, 12, 8, 2, Hex("060E0E"));
        p.DrawLine(18, 20, 30, 20, tc2, 1);
        p.DrawLine(18, 20, 18, 28, tc2, 1);
        p.DrawLine(30, 20, 30, 28, tc2, 1);
        // 하단 방패
        FillShieldShape(p, 24, 34, 40, 14, Hex("0A2020"), Hex("163030"));
        DrawShieldOutline(p, 24, 34, 40, 14, tc2, 2);
        p.FillRRect(23, 30, 3, 12, 1, tc2);
        p.FillRRect(17, 36, 15, 3, 1, tc2);
    }

    // 원거리 화망 (1021) — 궁수2+법사2: 화살 + 번개 교차
    static void DrawSynergyRangedFirenet(P p)
    {
        p.BgGradient(Hex("040A12"), Hex("0A1828"));
        p.RoundedBorder(8, 2, Hex("44AACC"));
        // 화살 (좌하→우상)
        p.DrawLine(6, 42, 40, 8, Hex("44DD55"), 3);
        p.FillTri(40, 8, 34, 12, 36, 18, Hex("44DD55"));
        p.FillTri(6, 42, 12, 40, 8, 46, Green);
        // 번개 (우하→좌상, 교차)
        p.FillTri(38, 12, 33, 24, 40, 24, Hex("FFEE33"));
        p.FillTri(36, 24, 44, 24, 38, 40, Hex("FFEE33"));
        p.DrawLine(38, 12, 33, 24, Hex("FFFFFF"), 1);
        p.DrawLine(44, 24, 38, 40, Hex("FFFFAA"), 1);
        // 교차 글로우
        p.FillCircleAlpha(26, 24, 8, new Color32(100, 200, 220, 50));
    }

    // 철벽 전위대 (1022) — 기사2+방패병2: 두 방패 겹침
    static void DrawSynergyIronVanguard(P p)
    {
        p.BgGradient(Hex("060C10"), Hex("0E2028"));
        p.RoundedBorder(8, 2, Hex("5588AA"));
        var tc3 = Hex("33DDEE");
        var rc2 = Hex("CC4444");
        // 방패 1 (왼쪽, 붉은 테두리)
        FillShieldShape(p, 18, 8, 36, 12, Hex("1A0808"), Hex("280E0E"));
        DrawShieldOutline(p, 18, 8, 36, 12, rc2, 2);
        p.FillRRect(17, 12, 3, 12, 1, rc2);
        p.FillRRect(11, 19, 14, 3, 1, rc2);
        // 방패 2 (오른쪽, 청록 테두리)
        FillShieldShape(p, 30, 14, 36, 12, Hex("0A1A20"), Hex("122028"));
        DrawShieldOutline(p, 30, 14, 36, 12, tc3, 2);
        p.FillRRect(29, 18, 3, 12, 1, tc3);
        p.FillRRect(23, 25, 14, 3, 1, tc3);
        // 하단 강조
        p.FillCircleAlpha(24, 38, 10, new Color32(85, 136, 170, 40));
        p.DrawLine(10, 38, 38, 38, new Color32(85, 136, 170, 80), 2);
    }

    // ── 열화 (3-스택) ─────────────────────────────────────────

    // 기사 소대 (1031) — 기사×3: 3개 검 나란히
    static void DrawSynergyKnightSquad(P p)
    {
        p.BgGradient(Hex("120404"), Hex("2A0808"));
        p.RoundedBorder(8, 2, Hex("884444"));
        var sc3 = new Color32(180, 170, 160, 220);
        // 3개 검 (나란히, 약간 기울어진)
        int[] kx = { 13, 24, 35 };
        for (int i = 0; i < 3; i++)
        {
            p.FillTri(kx[i], 8, kx[i] - 3, 14, kx[i] + 3, 14, sc3);
            p.FillRRect(kx[i] - 2, 14, 4, 14, 1, sc3);
            p.FillRRect(kx[i] - 5, 26, 10, 3, 1, new Color32(160, 120, 40, 200));
            p.FillRRect(kx[i] - 2, 29, 4, 4, 1, new Color32(100, 60, 20, 200));
        }
        // 열화 표시 (하단 작은 불꽃)
        p.FillCircleAlpha(24, 42, 6, new Color32(200, 80, 40, 80));
        p.DrawLine(20, 44, 24, 38, Hex("FF6633"), 1);
        p.DrawLine(28, 44, 24, 38, Hex("FF6633"), 1);
    }

    // 궁수 소대 (1032) — 궁수×3: 3개 화살 나란히
    static void DrawSynergyArcherSquad(P p)
    {
        p.BgGradient(Hex("030C03"), Hex("0A1E0A"));
        p.RoundedBorder(8, 2, Hex("448844"));
        var gc4 = new Color32(68, 180, 80, 220);
        // 3개 화살 (나란히, 위로 향함)
        int[] ax = { 13, 24, 35 };
        for (int i = 0; i < 3; i++)
        {
            p.FillTri(ax[i], 8, ax[i] - 3, 14, ax[i] + 3, 14, gc4);
            p.DrawLine(ax[i], 14, ax[i], 36, gc4, 2);
            p.FillTri(ax[i], 42, ax[i] - 4, 36, ax[i] + 4, 36, new Color32(60, 140, 40, 180));
        }
        // 열화 (작은 불꽃)
        p.FillCircleAlpha(24, 44, 5, new Color32(100, 200, 50, 70));
    }

    // 법사 소대 (1033) — 법사×3: 3개 마법 오브
    static void DrawSynergyMageSquad(P p)
    {
        p.BgGradient(Hex("050218"), Hex("100640"));
        p.RoundedBorder(8, 2, Hex("7722BB"));
        // 3개 오브
        int[] mx = { 12, 24, 36 };
        int[] my = { 28, 20, 28 };
        var mc2 = new Color32(100, 70, 200, 220);
        for (int i = 0; i < 3; i++)
        {
            p.FillCircleGrad(mx[i], my[i], 9, Hex("AABBFF"), Hex("5533CC"), Hex("1A0888"));
            p.FillCircleAlpha(mx[i] - 2, my[i] - 2, 3, new Color32(255, 255, 255, 80));
        }
        // 연결선
        p.DrawLine(mx[0], my[0], mx[1], my[1], new Color32(120, 80, 200, 80), 1);
        p.DrawLine(mx[1], my[1], mx[2], my[2], new Color32(120, 80, 200, 80), 1);
        // 열화 (작은 파티클)
        p.FillCircleAlpha(24, 42, 5, new Color32(100, 60, 200, 70));
    }

    // 방패병 소대 (1034) — 방패병×3: 3개 방패 나란히
    static void DrawSynergyShieldSquad(P p)
    {
        p.BgGradient(Hex("020A0A"), Hex("081A1A"));
        p.RoundedBorder(8, 2, Hex("228888"));
        var tc4 = new Color32(50, 180, 180, 220);
        // 3개 미니 방패
        int[] sx = { 10, 24, 38 };
        for (int i = 0; i < 3; i++)
        {
            FillShieldShape(p, sx[i], 10, 22, 12, Hex("061414"), Hex("0E2626"));
            DrawShieldOutline(p, sx[i], 10, 22, 12, tc4, 2);
            p.FillRRect(sx[i] - 2, 14, 3, 10, 1, tc4);
            p.FillRRect(sx[i] - 5, 19, 10, 3, 1, tc4);
        }
        // 열화 (하단)
        p.FillCircleAlpha(24, 42, 5, new Color32(50, 160, 160, 70));
        p.DrawLine(10, 38, 38, 38, new Color32(50, 180, 180, 60), 1);
    }

    [MenuItem(ProjectKMenu.Icon + "어빌리티 아이콘", priority = ProjectKMenu.IconPrio + 13)]
    public static void GenerateAbilityIcons()
    {
        EnsureDir(ABILITY_PATH);

        // Normal (A01~A15) — dark-blue bg, grade dot absent
        Save(48, 48, ABILITY_PATH + "/ability_a01.png", p => DrawAbilIcon(p, DrawSymHp,     TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a02.png", p => DrawAbilIcon(p, DrawSymAtk,    TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a03.png", p => DrawAbilIcon(p, DrawSymASpd,   TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a04.png", p => DrawAbilIcon(p, DrawSymMSpd,   TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a05.png", p => DrawAbilIcon(p, DrawSymDef,    TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a06.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymHpSm, TargetCol(1), false));
        Save(48, 48, ABILITY_PATH + "/ability_a07.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymASpdSm, TargetCol(2), false));
        Save(48, 48, ABILITY_PATH + "/ability_a08.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymCdrSm,  TargetCol(3), false));
        Save(48, 48, ABILITY_PATH + "/ability_a09.png", p => DrawAbilIconDual(p, DrawSymDef,  DrawSymHpSm, TargetCol(4), false));
        Save(48, 48, ABILITY_PATH + "/ability_a10.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(5), false));
        Save(48, 48, ABILITY_PATH + "/ability_a11.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymAtkSm, TargetCol(6), false));
        Save(48, 48, ABILITY_PATH + "/ability_a12.png", p => DrawAbilIconDual(p, DrawSymCrown, DrawSymDefSm, TargetCol(7), false));
        Save(48, 48, ABILITY_PATH + "/ability_a13.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(8), false));
        Save(48, 48, ABILITY_PATH + "/ability_a14.png", p => DrawAbilIcon(p, DrawSymCrit,   TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a15.png", p => DrawAbilIcon(p, DrawSymRange,  TargetCol(0), false));
        Save(48, 48, ABILITY_PATH + "/ability_a16.png", p => DrawAbilIcon(p, DrawSymHp,     TargetCol(8), false));
        Save(48, 48, ABILITY_PATH + "/ability_a17.png", p => DrawAbilIcon(p, DrawSymCrown,  TargetCol(7), false));

        // Advanced (B01~B12) — amber bg, gold star present
        Save(48, 48, ABILITY_PATH + "/ability_b01.png", p => DrawAbilIcon(p, DrawSymHp,     TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b02.png", p => DrawAbilIcon(p, DrawSymAtk,    TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b03.png", p => DrawAbilIcon(p, DrawSymASpd,   TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b04.png", p => DrawAbilIcon(p, DrawSymMSpd,   TargetCol(0), true));
        Save(48, 48, ABILITY_PATH + "/ability_b05.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymHpSm, TargetCol(1), true));
        Save(48, 48, ABILITY_PATH + "/ability_b06.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymASpdSm, TargetCol(2), true));
        Save(48, 48, ABILITY_PATH + "/ability_b07.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymCdrSm,  TargetCol(3), true));
        Save(48, 48, ABILITY_PATH + "/ability_b08.png", p => DrawAbilIconDual(p, DrawSymDef,  DrawSymHpSm, TargetCol(4), true));
        Save(48, 48, ABILITY_PATH + "/ability_b09.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(5), true));
        Save(48, 48, ABILITY_PATH + "/ability_b10.png", p => DrawAbilIconDual(p, DrawSymRange, DrawSymAtkSm, TargetCol(6), true));
        Save(48, 48, ABILITY_PATH + "/ability_b11.png", p => DrawAbilIconDual(p, DrawSymCrown, DrawSymDefSm, TargetCol(7), true));
        Save(48, 48, ABILITY_PATH + "/ability_b12.png", p => DrawAbilIconDual(p, DrawSymAtk,  DrawSymMSpdSm, TargetCol(8), true));
        Save(48, 48, ABILITY_PATH + "/ability_b13.png", p => DrawAbilIcon(p, DrawSymHp,     TargetCol(8), true));
        Save(48, 48, ABILITY_PATH + "/ability_b14.png", p => DrawAbilIcon(p, DrawSymCrown,  TargetCol(7), true));

        // Special (C01~C11) — dark crimson bg, diamond marker
        Save(48, 48, ABILITY_PATH + "/ability_c01.png", DrawSpecialC01);
        Save(48, 48, ABILITY_PATH + "/ability_c02.png", DrawSpecialC02);
        Save(48, 48, ABILITY_PATH + "/ability_c03.png", DrawSpecialC03);
        Save(48, 48, ABILITY_PATH + "/ability_c04.png", DrawSpecialC04);
        Save(48, 48, ABILITY_PATH + "/ability_c05.png", DrawSpecialC05);
        Save(48, 48, ABILITY_PATH + "/ability_c06.png", DrawSpecialC06);
        Save(48, 48, ABILITY_PATH + "/ability_c07.png", DrawSpecialC07);
        Save(48, 48, ABILITY_PATH + "/ability_c08.png", DrawSpecialC08);
        Save(48, 48, ABILITY_PATH + "/ability_c09.png", DrawSpecialC09);
        Save(48, 48, ABILITY_PATH + "/ability_c10.png", DrawSpecialC10);
        Save(48, 48, ABILITY_PATH + "/ability_c11.png", DrawSpecialC11);

        // Mastery (D01~D04) — dark green bg, crown corner badge
        Save(48, 48, ABILITY_PATH + "/ability_d01.png", DrawMasteryD01);
        Save(48, 48, ABILITY_PATH + "/ability_d02.png", DrawMasteryD02);
        Save(48, 48, ABILITY_PATH + "/ability_d03.png", DrawMasteryD03);
        Save(48, 48, ABILITY_PATH + "/ability_d04.png", DrawMasteryD04);

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(ABILITY_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 어빌리티 아이콘 46장 생성 완료.");
    }

    // ─────────────────────────────────────────────────────
    //  어빌리티 아이콘 공용 프레임
    // ─────────────────────────────────────────────────────

    // target index → border color: 0=All(white) 1=Knight(red) 2=Archer(green) 3=Mage(purple)
    //   4=Shield(teal) 5=Melee(orange) 6=Ranged(cyan) 7=General(gold) 8=Soldier(steel)
    static Color32 TargetCol(int idx)
    {
        switch (idx)
        {
            case 1: return Hex("CC3333");
            case 2: return Hex("33BB44");
            case 3: return Hex("9933FF");
            case 4: return Hex("22BBCC");
            case 5: return Hex("FF8833");
            case 6: return Hex("33CCFF");
            case 7: return Hex("D4A840");
            case 8: return Hex("7799BB");
            default: return Hex("CCCCDD");
        }
    }

    static void DrawAbilIcon(P p, Action<P, Color32, bool> symDraw, Color32 borderCol, bool advanced)
    {
        AbilBg(p, advanced);
        p.RoundedBorder(8, 2, borderCol);
        symDraw(p, borderCol, advanced);
        if (advanced) DrawAdvancedStar(p);
    }

    static void DrawAbilIconDual(P p, Action<P, Color32, bool> symMain, Action<P> symSub,
                                   Color32 borderCol, bool advanced)
    {
        AbilBg(p, advanced);
        p.RoundedBorder(8, 2, borderCol);
        symMain(p, borderCol, advanced);
        symSub(p);
        if (advanced) DrawAdvancedStar(p);
    }

    static void AbilBg(P p, bool advanced)
    {
        if (advanced) p.BgGradient(Hex("0E0902"), Hex("2A1E06"));
        else          p.BgGradient(Hex("060810"), Hex("0E1830"));
    }

    // 고급 등급 표시 — 우상단 금별
    static void DrawAdvancedStar(P p)
    {
        int x = 40, y = 8;
        p.FillCircle(x, y, 5, Hex("2A1E06"));
        // 별 5각
        for (int i = 0; i < 5; i++)
        {
            float a1 = (i * 72 - 90) * Mathf.Deg2Rad;
            float a2 = (i * 72 - 90 + 36) * Mathf.Deg2Rad;
            int ox = x + Mathf.RoundToInt(Mathf.Cos(a1) * 4);
            int oy = y + Mathf.RoundToInt(Mathf.Sin(a1) * 4);
            int ix = x + Mathf.RoundToInt(Mathf.Cos(a2) * 2);
            int iy = y + Mathf.RoundToInt(Mathf.Sin(a2) * 2);
            p.DrawLine(ox, oy, ix, iy, Gold, 1);
        }
        p.FillCircle(x, y, 2, Gold);
    }

    // ─────────────────────────────────────────────────────
    //  특수(Special) 어빌리티 아이콘
    // ─────────────────────────────────────────────────────

    static void SpecialBg(P p) => p.BgGradient(Hex("120008"), Hex("320020"));

    // 특수 등급 마커 — 우상단 보라 다이아몬드
    static void DrawSpecialDiamond(P p)
    {
        int x = 40, y = 8;
        p.FillCircle(x, y, 5, Hex("120008"));
        p.FillTri(x, y - 5, x - 4, y, x + 4, y, Hex("DD44FF"));
        p.FillTri(x, y + 5, x - 4, y, x + 4, y, Hex("AA22CC"));
        p.DrawLine(x, y - 5, x - 4, y, Hex("EE88FF"), 1);
        p.DrawLine(x, y - 5, x + 4, y, Hex("EE88FF"), 1);
    }

    // C01 — 흡혈 강습 (OnAttack: 검 + 핏방울)
    static void DrawSpecialC01(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("CC2244"));
        // 검
        p.FillTri(24, 6, 20, 11, 28, 11, Silver);
        p.FillRect(22, 11, 5, 20, Silver);
        p.FillRect(21, 11, 3, 18, Tint(Silver, White, 0.4f));
        p.FillRRect(16, 31, 16, 5, 2, Hex("AA2222"));
        p.FillRRect(22, 36, 5, 6, 2, Wood);
        // 핏방울 오른쪽 하단
        var blood = Hex("EE1133");
        p.FillCircle(37, 38, 7, blood);
        p.FillTri(37, 25, 32, 38, 42, 38, blood);
        p.FillCircleAlpha(34, 30, 2, new Color32(255, 120, 140, 120));
        DrawSpecialDiamond(p);
    }

    // C02 — 철갑 반응 (OnHit: 방패 + 번개)
    static void DrawSpecialC02(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("2266CC"));
        var sc = Hex("3388EE");
        // 방패
        FillShieldShape(p, 22, 7, 44, 10, Hex("0A1A2A"), Hex("123050"));
        DrawShieldOutline(p, 22, 7, 44, 10, sc, 2);
        // 번개 (방패 위)
        p.FillTri(24, 13, 20, 26, 25, 26, Hex("FFEE33"));
        p.FillTri(23, 26, 28, 26, 24, 38, Hex("FFEE33"));
        p.DrawLine(24, 13, 20, 26, Hex("FFFFFF"), 1);
        p.DrawLine(28, 26, 24, 38, Hex("FFFFAA"), 1);
        // 충격 스파크
        p.DrawLine(10, 34, 16, 30, sc, 2);
        p.DrawLine(10, 40, 17, 38, sc, 1);
        p.DrawLine(36, 34, 42, 30, sc, 2);
        DrawSpecialDiamond(p);
    }

    // C03 — 처치 연쇄 (OnEnemyKill: 검 + 연쇄 링)
    static void DrawSpecialC03(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("CC8800"));
        var gc = Hex("FFB800");
        // 검 (대각)
        p.DrawLine(10, 42, 38, 10, Silver, 5);
        p.DrawLine(10, 42, 38, 10, Tint(Silver, White, 0.5f), 2);
        p.FillTri(38, 10, 32, 12, 36, 16, Silver);
        p.FillRRect(8, 40, 8, 4, 2, Gold);
        // 연쇄 링 오른쪽 하단 (두 개)
        p.DrawCircle(34, 36, 6, 2, gc);
        p.DrawCircle(42, 38, 5, 2, Tint(gc, Hex("000000"), 0.3f));
        p.DrawLine(34, 30, 34, 34, gc, 2);
        p.DrawLine(42, 33, 42, 36, Tint(gc, Hex("000000"), 0.3f), 2);
        // 처치 X 표시
        p.DrawLine(16, 28, 24, 20, Red, 2);
        p.DrawLine(24, 28, 16, 20, Red, 2);
        DrawSpecialDiamond(p);
    }

    // C04 — 희생의 힘 (OnSoldierDeath: 병사 실루엣 + 상승 화살표)
    static void DrawSpecialC04(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("8833CC"));
        var pc = Hex("BB44EE");
        // 병사 실루엣 (검은 작은 사람 형태, 중앙 하단)
        p.FillCircle(24, 30, 5, Hex("221133"));
        p.FillRRect(20, 35, 8, 10, 2, Hex("221133"));
        p.DrawCircle(24, 30, 5, 1, Hex("553366"));
        p.DrawCircle(24, 40, 5, 1, Hex("553366"));
        // 상승 화살표 (중앙 → 위)
        p.DrawLine(24, 26, 24, 8, pc, 3);
        p.FillTri(24, 5, 18, 14, 30, 14, pc);
        // 강화 광선 (방사형)
        var glow = new Color32(187, 68, 238, 100);
        p.DrawLine(24, 10, 14, 4,  glow, 1);
        p.DrawLine(24, 10, 34, 4,  glow, 1);
        p.DrawLine(24, 10, 8,  14, glow, 1);
        p.DrawLine(24, 10, 40, 14, glow, 1);
        DrawSpecialDiamond(p);
    }

    // C05 — 고통의 계약 (OnBattleStart: 균열 심장 + 상승 파워)
    static void DrawSpecialC05(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("FF2255"));
        // 심장 베이스 (균열 표현을 위해 어두운 레이어 먼저)
        FillHeart(p, 20, 22, 11, Hex("880011"));
        FillHeart(p, 20, 22, 9,  Hex("BB0022"));
        // 균열 — 그림자 + 하이라이트
        p.DrawLine(20, 13, 15, 22, Hex("220000"), 2);
        p.DrawLine(15, 22, 22, 29, Hex("220000"), 2);
        p.DrawLine(20, 13, 15, 22, Hex("FF5577"), 1);
        p.DrawLine(15, 22, 22, 29, Hex("FF5577"), 1);
        // 혈액 방울 (심장 아래)
        p.FillCircle(18, 37, 4, Hex("CC1133"));
        p.FillTri(14, 35, 22, 35, 18, 43, Hex("CC1133"));
        p.FillCircle(26, 39, 2, Hex("990011"));
        // 상승 파워 화살표 (오른쪽 — 강화 효과 상징)
        var pw = Hex("FF8822");
        p.FillCircleAlpha(36, 26, 9, new Color32(255, 120, 20, 40));
        p.DrawLine(36, 42, 36, 18, pw, 3);
        p.FillTri(36, 11, 29, 19, 43, 19, pw);
        p.DrawLine(36, 18, 28, 11, new Color32(255, 180, 80, 110), 1);
        p.DrawLine(36, 18, 44, 11, new Color32(255, 180, 80, 90),  1);
        DrawSpecialDiamond(p);
    }

    // C06 — 거울 방어 (OnBattleStart: 방패 + 반사 광선)
    static void DrawSpecialC06(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("22AACC"));
        var sc = Hex("44CCEE");
        // 방패 본체
        FillShieldShape(p, 24, 7, 42, 11, Hex("0A1A2A"), Hex("123050"));
        DrawShieldOutline(p, 24, 7, 42, 11, sc, 2);
        // 내부 거울 광택
        p.FillCircleAlpha(24, 26, 9, new Color32(100, 220, 255, 55));
        p.DrawLine(24, 16, 24, 36, new Color32(200, 240, 255, 110), 1);
        p.DrawLine(14, 26, 34, 26, new Color32(200, 240, 255, 110), 1);
        // 반사 광선 (방패 오른쪽 밖으로)
        p.DrawLine(36, 14, 44, 8,  Hex("88EEFF"), 2);
        p.DrawLine(37, 20, 45, 16, Hex("88EEFF"), 1);
        p.DrawLine(37, 26, 45, 24, new Color32(136, 238, 255, 120), 1);
        // 방패 광택 점
        p.FillCircle(18, 19, 2, new Color32(200, 240, 255, 180));
        p.FillCircle(30, 17, 1, new Color32(255, 255, 255, 150));
        DrawSpecialDiamond(p);
    }

    // C07 — 혼령 집결 (OnBattleStart: 혼령 실루엣 + 집결 화살표)
    static void DrawSpecialC07(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("8833AA"));
        var gc = Hex("BB66EE");
        // 왼쪽 혼령
        p.FillCircle(11, 32, 4, new Color32(190, 120, 230, 140));
        p.FillEllipse(11, 37, 4, 5, new Color32(170, 100, 220, 100));
        p.FillTri(7, 41, 11, 44, 15, 41, new Color32(170, 100, 220, 70));
        p.FillCircle(9,  31, 1, new Color32(230, 180, 255, 200));
        p.FillCircle(13, 31, 1, new Color32(230, 180, 255, 200));
        // 오른쪽 혼령
        p.FillCircle(35, 34, 3, new Color32(165, 100, 215, 120));
        p.FillEllipse(35, 39, 3, 4, new Color32(150, 85, 205, 90));
        p.FillTri(32, 42, 35, 45, 38, 42, new Color32(150, 85, 205, 60));
        p.FillCircle(33, 33, 1, new Color32(215, 165, 250, 180));
        p.FillCircle(37, 33, 1, new Color32(215, 165, 250, 180));
        // 중앙 집결 화살표
        p.DrawLine(24, 44, 24, 22, gc, 3);
        p.FillTri(24, 13, 17, 23, 31, 23, gc);
        p.FillCircleAlpha(24, 16, 8, new Color32(187, 102, 238, 55));
        DrawSpecialDiamond(p);
    }

    // C08 — 황금 탐욕 (시스템: 금화 더미 + 빛나는 코인)
    static void DrawSpecialC08(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("CCAA00"));
        var gc = Hex("D4A840");
        // 뒤쪽 금화 더미
        p.FillEllipse(26, 38, 10, 4, Hex("8B6820"));
        p.FillRRect(16, 26, 20, 12, 2, Hex("8B6820"));
        p.FillEllipse(26, 26, 10, 4, Hex("A07820"));
        // 앞쪽 금화 더미
        p.FillEllipse(22, 42, 10, 4, Hex("8B6820"));
        p.FillRRect(12, 30, 20, 12, 2, Hex("C89030"));
        p.FillEllipse(22, 30, 10, 4, gc);
        p.FillRect(12, 30, 20, 3, new Color32(255, 255, 255, 30));
        p.DrawLine(12, 34, 32, 34, Tint(gc, Hex("000000"), 0.35f), 1);
        p.DrawLine(12, 38, 32, 38, Tint(gc, Hex("000000"), 0.35f), 1);
        // 빛나는 금화 (개별, 우상단)
        p.FillCircle(32, 17, 7, gc);
        p.DrawCircle(32, 17, 7, 1, Hex("FFD060"));
        p.FillCircle(32, 17, 4, Hex("FFE070"));
        p.FillCircleAlpha(30, 14, 2, new Color32(255, 255, 200, 160));
        // 반짝임 파티클
        p.FillCircle(10, 16, 1, new Color32(255, 220, 68, 200));
        p.FillCircle(8,  24, 1, new Color32(255, 200, 50, 160));
        p.FillCircle(36, 8,  2, new Color32(255, 220, 68, 180));
        DrawSpecialDiamond(p);
    }

    // C09 — 성장 촉진 (시스템: 상승 화살표 + 별 + 경험치 오브)
    static void DrawSpecialC09(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("22BB44"));
        var gc = Hex("44EE88");
        // 경험치 오브들 (하단)
        p.FillCircle(10, 38, 4, new Color32(50, 220, 120, 200));
        p.FillCircleAlpha(8,  36, 2, new Color32(200, 255, 220, 150));
        p.FillCircle(36, 36, 3, new Color32(50, 220, 120, 175));
        p.FillCircle(18, 43, 3, new Color32(50, 200, 100, 155));
        // 중앙 상승 화살표
        p.DrawLine(24, 43, 24, 23, gc, 3);
        p.FillTri(24, 15, 17, 25, 31, 25, gc);
        // 상단 별 (5각 채우기)
        var sc = Hex("44FF88");
        for (int i = 0; i < 5; i++)
        {
            float a1 = (i * 72 - 90) * Mathf.Deg2Rad;
            float a2 = (i * 72 - 90 + 36) * Mathf.Deg2Rad;
            float a3 = (i * 72 - 90 + 72) * Mathf.Deg2Rad;
            int ox  = 24 + Mathf.RoundToInt(Mathf.Cos(a1) * 7);
            int oy  = 10 + Mathf.RoundToInt(Mathf.Sin(a1) * 7);
            int ix  = 24 + Mathf.RoundToInt(Mathf.Cos(a2) * 3);
            int iy  = 10 + Mathf.RoundToInt(Mathf.Sin(a2) * 3);
            int ox2 = 24 + Mathf.RoundToInt(Mathf.Cos(a3) * 7);
            int oy2 = 10 + Mathf.RoundToInt(Mathf.Sin(a3) * 7);
            p.FillTri(24, 10, ox, oy, ix, iy, sc);
            p.FillTri(24, 10, ix, iy, ox2, oy2, sc);
        }
        p.FillCircle(24, 10, 3, sc);
        p.FillCircleAlpha(22, 8, 2, new Color32(255, 255, 255, 160));
        p.FillCircleAlpha(24, 10, 10, new Color32(68, 238, 136, 35));
        DrawSpecialDiamond(p);
    }

    // C10 — 시간 왜곡 (OnBattleStart: 시계 + 왜곡 소용돌이)
    static void DrawSpecialC10(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("6633BB"));
        var cc = Hex("9966EE");
        // 시계 외곽
        p.DrawCircle(23, 27, 14, 2, cc);
        p.FillCircleAlpha(23, 27, 14, new Color32(100, 60, 180, 25));
        // 눈금 (4방향)
        p.DrawLine(23, 14, 23, 17, cc, 2);
        p.DrawLine(23, 37, 23, 40, cc, 2);
        p.DrawLine(10, 27, 13, 27, cc, 2);
        p.DrawLine(33, 27, 36, 27, cc, 2);
        // 시계 침
        p.DrawLine(23, 27, 23, 17, Hex("CCAAFF"), 2); // 분침
        p.DrawLine(23, 27, 31, 27, Hex("CCAAFF"), 2); // 시침
        p.FillCircle(23, 27, 2, Hex("CCAAFF"));
        // 왼쪽 하단 왜곡 나선
        var wc = new Color32(150, 80, 220, 150);
        DrawArcPath(p, 9, 43, 8,   0, 200, wc, 1);
        DrawArcPath(p, 9, 43, 5,  30, 210, new Color32(150, 80, 220, 100), 1);
        // 빠른 흐름 속도선
        p.DrawLine(36,  8, 44, 12, new Color32(180, 130, 255, 150), 1);
        p.DrawLine(38, 14, 46, 16, new Color32(180, 130, 255, 120), 1);
        p.DrawLine(38, 20, 46, 20, new Color32(180, 130, 255, 100), 1);
        DrawSpecialDiamond(p);
    }

    // C11 — 쌍신 공격 (OnBattleStart: 교차 쌍검 + 임팩트)
    static void DrawSpecialC11(P p)
    {
        SpecialBg(p);
        p.RoundedBorder(8, 2, Hex("CC8833"));
        var oc = Hex("FFAA44");
        // 검 1 (좌상 → 우하)
        p.DrawLine(8,  8, 40, 40, Silver, 4);
        p.DrawLine(8,  8, 40, 40, Tint(Silver, White, 0.5f), 2);
        p.FillTri(8, 8, 14, 10, 10, 16, Silver);          // 검끝 1
        p.FillRRect(35, 37, 8, 3, 1, Gold);                // 가드 1
        // 검 2 (우상 → 좌하)
        p.DrawLine(40,  8,  8, 40, Silver, 4);
        p.DrawLine(40,  8,  8, 40, Tint(Silver, White, 0.5f), 2);
        p.FillTri(40, 8, 34, 10, 38, 16, Silver);          // 검끝 2
        p.FillRRect(5,  37, 8, 3, 1, Gold);                // 가드 2
        // 교차 임팩트
        p.FillCircleAlpha(24, 24, 9, new Color32(255, 170, 68, 90));
        p.FillCircle(24, 24, 4, oc);
        p.FillCircleAlpha(22, 22, 2, new Color32(255, 255, 255, 160));
        // 방사 스파크
        p.DrawLine(24, 24, 14, 14, new Color32(255, 170, 68, 90), 1);
        p.DrawLine(24, 24, 34, 14, new Color32(255, 170, 68, 90), 1);
        p.DrawLine(24, 24, 8,  24, new Color32(255, 150, 50, 70), 1);
        p.DrawLine(24, 24, 40, 24, new Color32(255, 150, 50, 70), 1);
        DrawSpecialDiamond(p);
    }

    // ─────────────────────────────────────────────────────
    //  달인(Mastery) 어빌리티 아이콘
    // ─────────────────────────────────────────────────────

    static void MasteryBg(P p) => p.BgGradient(Hex("021008"), Hex("063020"));

    // 달인 등급 마커 — 우상단 녹색 미니 왕관
    static void DrawMasteryCorner(P p)
    {
        int x = 40, y = 8;
        p.FillCircle(x, y, 5, Hex("021008"));
        // 왕관 베이스
        p.FillRect(x - 4, y, 9, 4, Hex("44CC66"));
        // 세 꼭짓점
        p.FillTri(x - 4, y,     x - 4, y - 4, x - 2, y, Hex("44CC66"));
        p.FillTri(x,     y,     x,     y - 5, x + 2, y, Hex("66EE88"));
        p.FillTri(x + 4, y,     x + 2, y,     x + 4, y - 4, Hex("44CC66"));
    }

    // D01 — 기사 달인 (돌진 기사)
    static void DrawMasteryD01(P p)
    {
        MasteryBg(p);
        p.RoundedBorder(8, 2, Hex("CC3333"));
        var rc = Hex("EE4444");
        // 기사 갑옷 실루엣
        p.FillCircle(18, 16, 8, Hex("223344"));
        p.FillRRect(12, 24, 14, 14, 2, Hex("223344"));
        p.DrawCircle(18, 16, 8, 2, rc);
        p.DrawLine(12, 24, 26, 24, rc, 1);
        p.DrawLine(12, 38, 26, 38, rc, 1);
        p.DrawLine(12, 24, 12, 38, rc, 1);
        p.DrawLine(26, 24, 26, 38, rc, 1);
        // 돌진 화살표
        p.DrawLine(28, 26, 42, 26, rc, 3);
        p.FillTri(46, 26, 37, 19, 37, 33, rc);
        // 속도 잔상
        p.DrawLine(24, 32, 38, 32, Tint(rc, Hex("000000"), 0.45f), 1);
        p.DrawLine(22, 37, 36, 37, Tint(rc, Hex("000000"), 0.55f), 1);
        DrawMasteryCorner(p);
    }

    // D02 — 궁수 달인 (이중 화살)
    static void DrawMasteryD02(P p)
    {
        MasteryBg(p);
        p.RoundedBorder(8, 2, Hex("33BB44"));
        var gc = Hex("44DD55");
        // 활
        p.DrawLine(8, 8,  12, 24, gc, 2);
        p.DrawLine(12, 24, 8, 40, gc, 2);
        p.DrawLine(8, 8,  8,  40, Tint(gc, Hex("000000"), 0.5f), 1);
        // 시위
        p.DrawLine(8,  8, 12, 18, Tint(gc, White, 0.3f), 1);
        p.DrawLine(8, 40, 12, 30, Tint(gc, White, 0.3f), 1);
        // 화살 두 발
        p.DrawLine(12, 18, 44, 20, Silver, 2);
        p.FillTri(44, 20, 38, 15, 40, 22, Silver);
        p.DrawLine(12, 30, 44, 32, Tint(Silver, gc, 0.35f), 2);
        p.FillTri(44, 32, 38, 27, 40, 34, Tint(Silver, gc, 0.35f));
        DrawMasteryCorner(p);
    }

    // D03 — 마법사 달인 (마법 구슬 + 번개 프록)
    static void DrawMasteryD03(P p)
    {
        MasteryBg(p);
        p.RoundedBorder(8, 2, Hex("9933FF"));
        var mc = Hex("BB55FF");
        // 마법 구슬
        p.FillCircle(22, 30, 12, Hex("110022"));
        p.DrawCircle(22, 30, 12, 2, mc);
        p.FillCircleAlpha(17, 25, 5, new Color32(187, 85, 255, 100));
        // 번개 (스킬 프록)
        p.FillTri(22, 10, 17, 22, 23, 22, Hex("FFEE33"));
        p.FillTri(21, 22, 27, 22, 22, 34, Hex("FFEE33"));
        p.DrawLine(22, 10, 17, 22, Hex("FFFFFF"), 1);
        p.DrawLine(27, 22, 22, 34, Hex("FFFFAA"), 1);
        // 스파크
        p.DrawLine(34, 14, 42, 10, mc, 2);
        p.DrawLine(34, 20, 43, 18, mc, 1);
        p.DrawLine(34, 26, 43, 26, mc, 1);
        DrawMasteryCorner(p);
    }

    // D04 — 방패병 달인 (방패 + 반사파)
    static void DrawMasteryD04(P p)
    {
        MasteryBg(p);
        p.RoundedBorder(8, 2, Hex("22BBCC"));
        var tc = Hex("33DDEE");
        // 방패
        FillShieldShape(p, 20, 6, 40, 14, Hex("0A1A1A"), Hex("0E2828"));
        DrawShieldOutline(p, 20, 6, 40, 14, tc, 2);
        // 십자 문장
        p.FillRect(17, 14, 7, 3, Tint(tc, White, 0.2f));
        p.FillRect(19, 10, 3, 12, Tint(tc, White, 0.2f));
        // 반사파
        p.DrawLine(38, 20, 46, 14, tc, 2);
        p.DrawLine(38, 27, 47, 23, Tint(tc, Hex("000000"), 0.3f), 2);
        p.DrawLine(38, 34, 46, 32, Tint(tc, Hex("000000"), 0.5f), 1);
        DrawMasteryCorner(p);
    }

    // ─────────────────────────────────────────────────────
    //  스텟 심볼 그리기
    // ─────────────────────────────────────────────────────

    // 체력 — 하트
    static void DrawSymHp(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("FF5577") : Hex("DD3355");
        FillHeart(p, 24, 23, 13, c);
        p.FillCircleAlpha(19, 17, 4, new Color32(255, 255, 255, 60));
    }
    static void DrawSymHpSm(P p) => FillHeart(p, 38, 36, 6, Hex("DD3355"));

    // 공격 — 검
    static void DrawSymAtk(P p, Color32 accent, bool adv)
    {
        var blade = adv ? Silver : Hex("AAAAAA");
        p.FillTri(24, 6, 20, 10, 28, 10, blade);
        p.FillRect(22, 10, 5, 22, blade);
        p.FillRect(21, 11, 3, 20, Tint(blade, White, 0.4f));
        p.FillRRect(16, 32, 16, 5, 2, Gold);
        p.FillRRect(22, 37, 5, 7, 2, Wood);
        p.FillCircle(24, 46, 4, DkGold);
    }
    static void DrawSymAtkSm(P p)
    {
        p.DrawLine(34, 30, 44, 20, Hex("AAAAAA"), 3);
        p.FillTri(34, 30, 30, 34, 37, 37, Hex("AAAAAA"));
    }

    // 공격속도 — 검 + 속도선
    static void DrawSymASpd(P p, Color32 accent, bool adv)
    {
        var lc = new Color32(accent.r, accent.g, accent.b, 120);
        p.DrawLine(6, 18, 22, 18, lc, 3);
        p.DrawLine(6, 24, 20, 24, lc, 2);
        p.DrawLine(6, 30, 22, 30, lc, 3);
        var blade = adv ? Silver : Hex("AAAAAA");
        p.DrawLine(24, 8, 42, 40, blade, 5);
        p.DrawLine(23, 8, 41, 40, Tint(blade, White, 0.5f), 2);
        p.FillTri(42, 40, 36, 38, 40, 34, blade);
        p.FillRRect(22, 6, 5, 3, 1, Gold);
    }
    static void DrawSymASpdSm(P p)
    {
        p.DrawLine(32, 34, 44, 28, Hex("CCCCCC"), 2);
        p.DrawLine(32, 40, 44, 34, new Color32(204, 204, 204, 120), 1);
    }

    // 이동속도 — 발자국 + 화살표
    static void DrawSymMSpd(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("88EEFF") : Hex("44AADD");
        p.DrawLine(10, 40, 38, 12, c, 3);
        p.FillTri(38, 12, 30, 12, 38, 20, c);
        p.DrawLine(4,  38, 14, 38, new Color32(c.r, c.g, c.b, 120), 2);
        p.DrawLine(8,  44, 18, 44, new Color32(c.r, c.g, c.b, 80),  2);
        p.DrawLine(6,  32, 16, 32, new Color32(c.r, c.g, c.b, 100), 1);
        // 발자국
        p.FillEllipse(18, 36, 4, 3, Tint(c, Hex("000000"), 0.4f));
        p.FillEllipse(28, 26, 4, 3, Tint(c, Hex("000000"), 0.4f));
    }
    static void DrawSymMSpdSm(P p)
    {
        p.DrawLine(34, 40, 44, 30, Hex("44AADD"), 2);
        p.FillTri(44, 30, 38, 30, 44, 36, Hex("44AADD"));
    }

    // 방어 — 방패
    static void DrawSymDef(P p, Color32 accent, bool adv)
    {
        var dark = adv ? Hex("1A2A1A") : Hex("1A2030");
        var mid  = adv ? Hex("2A4A2A") : Hex("1A4050");
        FillShieldShape(p, 24, 6, 46, 10, dark, mid);
        DrawShieldOutline(p, 24, 6, 46, 10, accent, 2);
        p.FillCircle(24, 28, 7, dark);
        p.DrawCircle(24, 28, 7, 1, accent);
        p.FillRRect(23, 22, 3, 12, 1, accent);
        p.FillRRect(18, 27, 12, 3, 1, accent);
    }
    static void DrawSymDefSm(P p)
    {
        FillShieldShape(p, 38, 30, 44, 28, Hex("1A2030"), Hex("1A4050"));
        DrawShieldOutline(p, 38, 30, 44, 28, Hex("22BBCC"), 1);
    }

    // 사거리 — 조준선
    static void DrawSymRange(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("FFEE88") : Hex("DDCC44");
        p.DrawCircle(24, 24, 14, 2, c);
        p.DrawCircle(24, 24, 8,  1, new Color32(c.r, c.g, c.b, 130));
        p.DrawLine(24, 6,  24, 14, c, 2);
        p.DrawLine(24, 34, 24, 42, c, 2);
        p.DrawLine(6,  24, 14, 24, c, 2);
        p.DrawLine(34, 24, 42, 24, c, 2);
        p.FillCircle(24, 24, 3, c);
        // 화살
        p.DrawLine(32, 32, 44, 44, Hex("C8A840"), 3);
        p.FillTri(44, 44, 38, 44, 44, 38, Hex("C8A840"));
    }

    // 치명 — 별+조준
    static void DrawSymCrit(P p, Color32 accent, bool adv)
    {
        var c = adv ? Hex("FFDD33") : Hex("FFBB22");
        p.DrawCircle(24, 24, 16, 1, new Color32(c.r, c.g, c.b, 80));
        p.DrawLine(24, 8,  24, 40, c, 1);
        p.DrawLine(8,  24, 40, 24, c, 1);
        p.DrawLine(11, 11, 37, 37, c, 1);
        p.DrawLine(37, 11, 11, 37, c, 1);
        p.FillCircle(24, 24, 5, Hex("332200"));
        p.DrawCircle(24, 24, 5, 1, c);
        p.FillCircle(24, 24, 2, c);
        // 번쩍임
        p.FillTri(24, 5, 22, 10, 26, 10, c);
        p.FillTri(24, 43, 22, 38, 26, 38, c);
    }

    // 스킬 쿨다운 감소 — 시계 화살표 (소형, 우하단)
    static void DrawSymCdrSm(P p)
    {
        var c = Hex("9966DD");
        p.DrawCircle(38, 36, 6, 1, c);
        p.DrawLine(38, 36, 38, 30, c, 1);
        p.DrawLine(38, 36, 42, 38, c, 1);
    }

    // 왕관 — 장군 대상
    static void DrawSymCrown(P p, Color32 accent, bool adv)
    {
        var base1 = adv ? Hex("C89030") : Hex("A06820");
        var gemC  = adv ? Hex("88CCFF") : Red;
        p.FillRRect(10, 28, 28, 9, 1, base1);
        p.FillTri(10, 28, 13, 14, 17, 28, Gold);
        p.FillTri(21, 28, 24, 10, 27, 28, Gold);
        p.FillTri(31, 28, 35, 14, 38, 28, Gold);
        p.FillRect(10, 28, 28, 3, new Color32(255, 255, 255, 30));
        p.FillCircle(13, 14, 3, gemC);
        p.FillCircle(24, 10, 4, gemC);
        p.FillCircle(35, 14, 3, gemC);
        p.FillCircleAlpha(23, 9, 2, new Color32(255, 255, 255, 150));
        // 광택
        p.FillCircleAlpha(13, 13, 1, new Color32(255, 255, 255, 130));
    }

    [MenuItem(ProjectKMenu.Icon + "직업·스킬 아이콘", priority = ProjectKMenu.IconPrio + 11)]
    public static void GenerateAllIcons()
    {
        EnsureDir(CLASS_PATH);
        EnsureDir(SKILL_PATH);

        // 직업 아이콘 (64×64)
        Save(64, 64, CLASS_PATH + "/knight_icon.png",      DrawKnight);
        Save(64, 64, CLASS_PATH + "/archer_icon.png",       DrawArcher);
        Save(64, 64, CLASS_PATH + "/mage_icon.png",         DrawMage);
        Save(64, 64, CLASS_PATH + "/shieldbearer_icon.png", DrawShieldBearer);

        // 스킬 아이콘 (48×48)
        Save(48, 48, SKILL_PATH + "/skill_heavy_strike.png",      DrawHeavyStrike);
        Save(48, 48, SKILL_PATH + "/skill_volley_fire.png",        DrawVolleyFire);
        Save(48, 48, SKILL_PATH + "/skill_leap_strike.png",        DrawLeapStrike);
        Save(48, 48, SKILL_PATH + "/skill_heal_aura.png",          DrawHealAura);
        Save(48, 48, SKILL_PATH + "/skill_target_heal.png",        DrawTargetHeal);
        Save(48, 48, SKILL_PATH + "/skill_charge_soldier.png",     DrawChargeSoldier);
        Save(48, 48, SKILL_PATH + "/skill_summon_skeleton.png",    DrawSummonSkeleton);
        Save(48, 48, SKILL_PATH + "/skill_poison_zone.png",        DrawPoisonZone);
        Save(48, 48, SKILL_PATH + "/skill_meteor.png",             DrawMeteor);
        Save(48, 48, SKILL_PATH + "/skill_blizzard.png",           DrawBlizzard);
        Save(48, 48, SKILL_PATH + "/skill_sacrifice_soldier.png",  DrawSacrificeSoldier);
        Save(48, 48, SKILL_PATH + "/skill_bind.png",               DrawBind);
        Save(48, 48, SKILL_PATH + "/skill_suicide_soldier.png",    DrawSuicideSoldier);
        Save(48, 48, SKILL_PATH + "/skill_berserker.png",          DrawBerserker);
        Save(48, 48, SKILL_PATH + "/skill_iron_shield.png",        DrawIronShield);
        Save(48, 48, SKILL_PATH + "/skill_arrow_rain.png",         DrawArrowRain);
        Save(48, 48, SKILL_PATH + "/skill_battle_cry.png",         DrawBattleCry);
        Save(48, 48, SKILL_PATH + "/skill_shockwave.png",          DrawShockwave);
        Save(48, 48, SKILL_PATH + "/skill_swift_strike.png",       DrawSwiftStrike);
        Save(48, 48, SKILL_PATH + "/skill_summon_elite.png",       DrawSummonElite);

        AssetDatabase.Refresh();
        // Sprite 임포트 설정 적용
        ApplySpriteImportSettings(CLASS_PATH, 64);
        ApplySpriteImportSettings(SKILL_PATH, 48);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 아이콘 24장 생성 완료.");
    }

    // ─────────────────────────────────────────────────────
    //  ■ 직업 아이콘
    // ─────────────────────────────────────────────────────

    static void DrawKnight(P p)
    {
        int W = p.W, H = p.H, cx = W / 2;
        p.BgGradient(Knight_BgDark, Knight_BgMid);
        p.RoundedBorder(10, 2, Knight_Rim);
        // 검 날 (넓고 듬직하게)
        p.FillTri(cx - 7, 10, cx + 7, 10, cx, 4, Silver);        // 칼끝 삼각형
        p.FillRect(cx - 7, 10, 14, 34, Silver);                   // 날 몸체
        p.FillRect(cx - 3, 11, 6, 32, Tint(Silver, White, 0.5f)); // 능선
        // 가드
        p.FillRRect(cx - 14, 44, 28, 6, 3, Gold);
        p.FillCircle(cx - 14, 47, 3, Red);
        p.FillCircle(cx + 14, 47, 3, Red);
        // 손잡이
        p.FillRRect(cx - 4, 50, 8, 10, 2, Wood);
        p.DrawLine(cx - 4, 53, cx + 4, 53, Tint(Wood, Hex("000000"), 0.35f), 1);
        p.DrawLine(cx - 4, 56, cx + 4, 56, Tint(Wood, Hex("000000"), 0.35f), 1);
        // 폼멜
        p.FillCircle(cx, 62, 5, Gold);
        p.FillCircle(cx, 62, 2, Yellow);
    }

    static void DrawArcher(P p)
    {
        int W = p.W, H = p.H;
        p.BgGradient(Archer_BgDark, Archer_BgMid);
        p.RoundedBorder(10, 2, Archer_Rim);

        // 활 — 당긴 상태 (활 몸체: 왼쪽 C자 곡선)
        var bowCol  = Hex("A07830");
        var bowHigh = Hex("D4A840");
        // 활 커브 (여러 선분으로 근사)
        DrawBowCurve(p, 22, 8, 56, bowCol, 5);   // 두꺼운 갈색
        DrawBowCurve(p, 22, 8, 56, bowHigh, 2);   // 하이라이트
        // 활 팁
        p.FillCircle(22, 8,  4, Gold);
        p.FillCircle(22, 56, 4, Gold);
        // 손잡이
        p.FillRRect(19, 26, 8, 12, 3, Hex("5A3010"));
        // 시위 — V자 당겨진 형태 (시위 당긴 지점 x=46, y=32)
        p.DrawLine(22, 9,  46, 32, Hex("E8E8CC"), 2);
        p.DrawLine(22, 55, 46, 32, Hex("E8E8CC"), 2);
        p.FillCircle(46, 32, 3, Hex("E8E8CC"));
        // 화살 샤프트
        p.DrawLine(10, 32, 46, 32, Gold, 3);
        // 화살촉 (왼쪽 방향)
        p.FillTri(10, 32, 20, 27, 20, 37, Silver);
        // 깃털
        p.FillTri(44, 32, 36, 27, 38, 32, Green);
        p.FillTri(44, 32, 36, 37, 38, 32, Tint(Green, Hex("000000"), 0.3f));
    }

    // 활 커브 근사 (cubic bezier M22,8 C10,18 10,46 22,56)
    static void DrawBowCurve(P p, int x0, int y0, int y1, Color32 col, int thickness)
    {
        // 조절점: (10,18) (10,46) → 단순 근사
        int steps = 40;
        float[] bx = { x0, 10, 10, x0 };
        float[] by = { y0, y0 + (y1 - y0) * 0.22f, y0 + (y1 - y0) * 0.78f, y1 };
        int px = x0, py = y0;
        for (int i = 1; i <= steps; i++)
        {
            float t  = i / (float)steps;
            float t2 = t * t, t3 = t2 * t;
            float mt = 1 - t, mt2 = mt * mt, mt3 = mt2 * mt;
            int nx = Mathf.RoundToInt(mt3*bx[0] + 3*mt2*t*bx[1] + 3*mt*t2*bx[2] + t3*bx[3]);
            int ny = Mathf.RoundToInt(mt3*by[0] + 3*mt2*t*by[1] + 3*mt*t2*by[2] + t3*by[3]);
            p.DrawLine(px, py, nx, ny, col, thickness);
            px = nx; py = ny;
        }
    }

    static void DrawMage(P p)
    {
        int W = p.W, H = p.H;
        p.BgGradient(Mage_BgDark, Mage_BgMid);
        p.RoundedBorder(10, 2, Mage_Rim);

        var orbInner = Hex("AACCFF");
        var orbOuter = Hex("2211AA");
        var orbMid   = Hex("6644FF");

        // 구슬 글로우
        p.FillCircleAlpha(36, 18, 14, new Color32(102, 68, 255, 40));
        // 구슬 본체
        p.FillCircleGrad(36, 18, 11, orbInner, orbMid, orbOuter);
        // 구슬 하이라이트
        p.FillCircleAlpha(32, 13, 5, new Color32(255, 255, 255, 90));
        // 구슬 내부 마법 선
        p.DrawCircle(36, 18, 6, 1, new Color32(200, 170, 255, 100));
        p.DrawLine(36, 12, 36, 24, new Color32(200, 170, 255, 80), 1);
        p.DrawLine(30, 18, 42, 18, new Color32(200, 170, 255, 80), 1);
        // 지팡이 본체 (하단 좌방향)
        var staffCol  = Hex("8B6820");
        var staffHigh = Hex("C8A040");
        p.DrawLine(36, 28, 24, 58, staffCol,  6);
        p.DrawLine(35, 29, 23, 57, staffHigh, 2);
        // 구슬-지팡이 연결 장식
        p.DrawLine(30, 27, 36, 27, Gold, 2);
        p.DrawLine(42, 27, 36, 27, Gold, 2);
        // 지팡이 끝 장식
        p.FillCircle(23, 59, 4, Gold);
        p.FillCircle(23, 59, 2, Yellow);
        // 마법 파티클
        p.FillCircle(18, 28, 2, new Color32(170, 136, 255, 180));
        p.FillCircle(50, 32, 2, new Color32(136, 170, 255, 160));
        p.FillCircle(14, 40, 2, new Color32(204, 136, 255, 140));
        p.FillCircle(52, 14, 2, new Color32(255, 170, 255, 140));
    }

    static void DrawShieldBearer(P p)
    {
        int W = p.W, H = p.H, cx = W / 2;
        p.BgGradient(Shield_BgDark, Shield_BgMid);
        p.RoundedBorder(10, 2, Shield_Rim);

        var steelDk = Hex("334455");
        var steelMd = Hex("1A4A5A");
        var steelLt = Hex("2A6A7A");
        var rimCol  = Teal;

        // 방패 본체 (히터 방패) — 다각형 근사
        FillShieldShape(p, cx, 7, 52, 14, steelDk, steelMd);
        // 테두리
        DrawShieldOutline(p, cx, 7, 52, 14, rimCol, 2);
        // 내부 선 (장식)
        DrawShieldOutline(p, cx, 11, 47, 18, new Color32(68, 204, 221, 70), 1);
        // 철판 질감
        p.DrawLine(20, 16, 20, 52, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(cx, 10, cx, 56, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(44, 16, 44, 52, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(10, 24, 54, 24, new Color32(153, 170, 204, 40), 1);
        p.DrawLine(10, 36, 54, 36, new Color32(153, 170, 204, 40), 1);
        // 리벳
        p.FillCircle(15, 16, 3, Teal); p.FillCircle(49, 16, 3, Teal);
        p.FillCircle(11, 30, 3, Teal); p.FillCircle(53, 30, 3, Teal);
        p.FillCircle(15, 44, 3, Teal); p.FillCircle(49, 44, 3, Teal);
        // 중앙 엠블럼
        p.FillCircle(cx, 33, 9, steelDk);
        p.DrawCircle(cx, 33, 9, 2, Teal);
        // 십자
        p.FillRRect(cx - 1, 25, 3, 16, 1, Teal);
        p.FillRRect(cx - 8, 32, 16, 3, 1, Teal);
        p.FillCircle(cx, 33, 3, Hex("AACCDD"));
        p.FillCircle(cx - 1, 32, 2, new Color32(255,255,255,70));
    }

    // 방패 채우기 (대략적인 히터 방패 모양)
    static void FillShieldShape(P p, int cx, int top, int bottom, int topY,
                                 Color32 dark, Color32 mid)
    {
        for (int y = topY; y <= bottom; y++)
        {
            float t  = (float)(y - topY) / (bottom - topY);
            float halfW;
            if (t < 0.6f)  halfW = Mathf.Lerp(24, 22, t / 0.6f);
            else           halfW = Mathf.Lerp(22, 0,  (t - 0.6f) / 0.4f);
            int x0 = cx - Mathf.RoundToInt(halfW);
            int x1 = cx + Mathf.RoundToInt(halfW);
            for (int x = x0; x <= x1; x++)
            {
                float blend = (float)(y - topY) / (bottom - topY);
                p.BlendPixel(x, y, Color32.Lerp(mid, dark, blend * blend));
            }
        }
    }

    static void DrawShieldOutline(P p, int cx, int top, int bottom, int topY, Color32 col, int w)
    {
        for (int y = topY; y <= bottom; y++)
        {
            float t = (float)(y - topY) / (bottom - topY);
            float halfW;
            if (t < 0.6f)  halfW = Mathf.Lerp(24, 22, t / 0.6f);
            else           halfW = Mathf.Lerp(22, 0,  (t - 0.6f) / 0.4f);
            int x0 = cx - Mathf.RoundToInt(halfW);
            int x1 = cx + Mathf.RoundToInt(halfW);
            for (int i = 0; i < w; i++)
            {
                p.BlendPixel(x0 + i, y, col);
                p.BlendPixel(x1 - i, y, col);
            }
        }
        // 상단 가로선
        for (int x = cx - 24; x <= cx + 24; x++)
            for (int i = 0; i < w; i++) p.BlendPixel(x, topY + i, col);
    }

    // ─────────────────────────────────────────────────────
    //  ■ 스킬 아이콘 (48×48)
    // ─────────────────────────────────────────────────────

    static void DrawHeavyStrike(P p)
    {
        int W = p.W, H = p.H, cx = W / 2;
        p.BgGradient(Hex("0E0604"), Hex("4A2010"));
        p.RoundedBorder(8, 1, Hex("CC5520"));
        // 해머 머리
        p.FillRRect(cx - 11, 8, 22, 14, 3, Hex("888888"));
        p.FillRect(cx - 11, 8, 22, 6, Hex("AAAAAA")); // 상단 하이라이트
        // 해머 손잡이
        p.FillRRect(cx - 3, 22, 6, 14, 2, Wood);
        p.DrawLine(cx - 3, 26, cx + 3, 26, Tint(Wood, Hex("000000"), 0.4f), 1);
        p.DrawLine(cx - 3, 30, cx + 3, 30, Tint(Wood, Hex("000000"), 0.4f), 1);
        // 충격 이펙트 라인 (아래)
        p.DrawLine(10, 42, 20, 34, Orange, 2);
        p.DrawLine(cx, 44, cx, 36, Orange, 2);
        p.DrawLine(38, 42, 28, 34, Orange, 2);
        // 충격 글로우
        p.FillCircleAlpha(cx, 36, 10, new Color32(255, 100, 50, 30));
    }

    static void DrawVolleyFire(P p)
    {
        int W = p.W;
        p.BgGradient(Hex("060E02"), Hex("1A3A0A"));
        p.RoundedBorder(8, 1, Hex("44AA22"));
        var arrowCol = Hex("C8A840");
        for (int i = 0; i < 3; i++)
        {
            int y = 14 + i * 10;
            p.DrawLine(8, y, 38, y, arrowCol, 3);        // 샤프트
            p.FillTri(38, y, 30, y - 5, 30, y + 5, Silver); // 화살촉
            p.FillTri(8, y, 16, y - 4, 14, y, Green);     // 깃 위
            p.FillTri(8, y, 16, y + 4, 14, y, Tint(Green, Hex("000000"), 0.25f)); // 깃 아래
        }
        // 속도선
        p.DrawLine(36, 10, 44, 10, Hex("88FF44"), 1);
        p.DrawLine(38, 24, 46, 24, Hex("88FF44"), 1);
        p.DrawLine(36, 38, 44, 38, Hex("88FF44"), 1);
    }

    static void DrawLeapStrike(P p)
    {
        p.BgGradient(Hex("0E0502"), Hex("3A1A04"));
        p.RoundedBorder(8, 1, Hex("CC7722"));
        // 착지 충격
        p.FillCircleAlpha(22, 40, 14, new Color32(255, 136, 50, 35));
        p.DrawCircle(22, 40, 8, 1, new Color32(255, 136, 50, 130));
        // 도약 궤적 (포물선 점선)
        DrawArc(p, 8, 38, 32, 12, Hex("FFAA44"), 1, true);
        // 인물 실루엣 — 점프 포즈
        p.FillCircle(34, 11, 4, Hex("DDDDDD")); // 머리
        p.DrawLine(34, 15, 34, 24, Hex("DDDDDD"), 3);  // 몸통
        p.DrawLine(34, 18, 40, 12, Hex("DDDDDD"), 2);  // 팔 (칼 올림)
        p.DrawLine(40, 12, 44,  8, Silver, 2);          // 검
        p.FillTri(44, 8, 41, 11, 43, 13, Silver);      // 검끝
        p.DrawLine(34, 24, 30, 32, Hex("DDDDDD"), 2);  // 다리1
        p.DrawLine(34, 24, 38, 30, Hex("DDDDDD"), 2);  // 다리2
    }

    static void DrawHealAura(P p)
    {
        int cx = p.W / 2, cy = p.H / 2;
        p.BgGradient(Hex("020A02"), Hex("0A2E0A"));
        p.RoundedBorder(8, 1, Hex("22AA44"));
        // 오라 원
        p.FillCircleAlpha(cx, cy, 22, new Color32(34, 255, 102, 25));
        p.DrawCircle(cx, cy, 18, 1, new Color32(34, 204, 85,  110));
        p.DrawCircle(cx, cy, 14, 1, new Color32(51, 221, 102, 90));
        // 십자 (치유)
        p.FillRRect(cx - 3, cy - 12, 6, 24, 3, Hex("44FF88"));
        p.FillRRect(cx - 12, cy - 3, 24, 6, 3, Hex("44FF88"));
        p.FillCircleAlpha(cx, cy, 6, new Color32(136, 255, 170, 180));
        p.FillCircle(cx, cy, 4, Hex("88FFAA"));
        p.FillCircleAlpha(cx - 2, cy - 2, 2, new Color32(255, 255, 255, 140));
        // 파티클
        p.FillCircle(10, 14, 2, new Color32(68, 255, 136, 180));
        p.FillCircle(38, 12, 2, new Color32(68, 255, 136, 150));
        p.FillCircle(8,  34, 2, new Color32(68, 255, 136, 130));
        p.FillCircle(40, 36, 2, new Color32(68, 255, 136, 150));
    }

    static void DrawTargetHeal(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("030803"), Hex("0E2A0E"));
        p.RoundedBorder(8, 1, Hex("33BB44"));
        // 하트
        FillHeart(p, cx, 22, 14, Red);
        // 하트 하이라이트
        p.FillCircleAlpha(cx - 5, 15, 4, new Color32(255, 255, 255, 60));
        // 십자 (치유)
        p.FillRRect(cx - 2, 16, 4, 12, 1, new Color32(255, 255, 255, 180));
        p.FillRRect(cx - 6, 20, 12, 4, 1, new Color32(255, 255, 255, 180));
        // 조준선
        p.DrawLine(cx, 8, cx, 13, Hex("33BB44"), 1);
        p.DrawLine(cx, 33, cx, 38, Hex("33BB44"), 1);
        p.DrawLine(10, 22, 15, 22, Hex("33BB44"), 1);
        p.DrawLine(33, 22, 38, 22, Hex("33BB44"), 1);
        p.DrawCircle(cx, 22, 14, 1, new Color32(51, 187, 68, 80));
        // 힐 화살표
        p.FillTri(10, 34, 7, 40, 13, 40, Hex("44FF88"));
        p.DrawLine(10, 40, 10, 46, Hex("44FF88"), 2);
        p.FillTri(38, 34, 35, 40, 41, 40, Hex("44FF88"));
        p.DrawLine(38, 40, 38, 46, Hex("44FF88"), 2);
    }

    static void FillHeart(P p, int cx, int cy, int r, Color32 c)
    {
        for (int y = cy - r; y <= cy + r * 2; y++)
        {
            for (int x = cx - r - 2; x <= cx + r + 2; x++)
            {
                float nx = (x - cx) / (float)r;
                float ny = (y - cy) / (float)r;
                // 하트 방정식 근사
                float dx1 = nx + 0.5f, dy1 = ny + 0.6f;
                float dx2 = nx - 0.5f, dy2 = ny + 0.6f;
                bool in1 = dx1 * dx1 + dy1 * dy1 <= 0.9f;
                bool in2 = dx2 * dx2 + dy2 * dy2 <= 0.9f;
                bool inLow = (nx * nx * 0.6f + (ny - 0.4f) * (ny - 0.4f)) <= 1.0f && ny > -0.3f;
                if ((in1 || in2) && y <= cy + r) p.BlendPixel(x, y, c);
                else if (inLow) p.BlendPixel(x, y, c);
            }
        }
    }

    static void DrawChargeSoldier(P p)
    {
        p.BgGradient(Hex("03090E"), Hex("10303A"));
        p.RoundedBorder(8, 1, Hex("22AACC"));
        // 속도선
        p.DrawLine(4, 20, 18, 20, new Color32(34, 170, 204, 100), 2);
        p.DrawLine(4, 24, 14, 24, new Color32(34, 170, 204, 80),  2);
        p.DrawLine(4, 28, 18, 28, new Color32(34, 170, 204, 100), 2);
        // 방패
        FillShieldShape(p, 28, 18, 44, 14, Hex("1A7A9A"), Hex("1A4A5A"));
        DrawShieldOutline(p, 28, 18, 44, 14, Teal, 2);
        // 방패 엠블럼
        p.FillCircle(28, 31, 5, Hex("0A4A5A"));
        p.FillRRect(27, 26, 3, 10, 1, Teal);
        p.FillRRect(22, 30, 12, 3, 1, Teal);
        // 돌격 화살표
        p.FillTri(44, 24, 36, 19, 36, 29, Teal);
        p.FillRect(28, 22, 8, 4, Teal);
        // 글로우
        p.FillCircleAlpha(44, 24, 8, new Color32(34, 204, 255, 40));
    }

    static void DrawSummonSkeleton(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("060310"), Hex("1A1030"));
        p.RoundedBorder(8, 1, Hex("8855CC"));
        // 소환진
        p.DrawCircle(cx, 40, 10, 1, new Color32(136, 85, 204, 80));
        p.FillCircleAlpha(cx, 24, 18, new Color32(136, 85, 204, 20));
        // 해골 두개골
        p.FillEllipse(cx, 16, 11, 9, Hex("CCCCAA"));
        p.FillCircleAlpha(cx - 3, 13, 4, new Color32(255, 255, 255, 50));
        // 눈 소켓
        p.FillEllipse(cx - 4, 15, 3, 4, Hex("220033"));
        p.FillEllipse(cx + 4, 15, 3, 4, Hex("220033"));
        p.FillCircleAlpha(cx - 4, 15, 2, new Color32(153, 51, 255, 200));
        p.FillCircleAlpha(cx + 4, 15, 2, new Color32(153, 51, 255, 200));
        // 코
        p.DrawLine(cx, 19, cx - 1, 21, Hex("888866"), 1);
        p.DrawLine(cx, 19, cx + 1, 21, Hex("888866"), 1);
        // 이빨
        p.FillRRect(cx - 7, 23, 14, 3, 1, Hex("888866"));
        for (int i = 0; i < 4; i++) p.DrawLine(cx - 6 + i*4, 23, cx - 6 + i*4, 26, Hex("CCCCAA"), 1);
        // 상승선
        p.DrawLine(cx, 44, cx, 28, new Color32(136, 85, 204, 100), 1);
        // 파티클
        p.FillCircle(10, 32, 2, new Color32(153, 51, 255, 180));
        p.FillCircle(38, 30, 2, new Color32(153, 51, 255, 160));
        p.FillCircle(14, 42, 2, new Color32(153, 51, 255, 120));
        p.FillCircle(36, 44, 2, new Color32(153, 51, 255, 120));
    }

    static void DrawPoisonZone(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("030802"), Hex("0E2A06"));
        p.RoundedBorder(8, 1, Hex("66AA11"));
        // 바닥 독 지대
        p.FillEllipse(cx, 40, 18, 5, new Color32(34, 85, 0, 150));
        p.FillCircleAlpha(cx, 40, 14, new Color32(136, 238, 34, 20));
        // 독 구름 (여러 원)
        p.FillCircleAlpha(18, 28, 9,  new Color32(136, 238, 34, 80));
        p.FillCircleAlpha(28, 26, 10, new Color32(100, 200, 20, 90));
        p.FillCircleAlpha(22, 24, 8,  new Color32(120, 220, 25, 70));
        // 해골 심볼
        p.FillCircle(cx, 24, 7, Hex("1A3A00"));
        p.DrawCircle(cx, 24, 7, 1, Hex("88EE22"));
        p.FillCircle(cx - 3, 22, 2, Hex("88EE22"));
        p.FillCircle(cx + 3, 22, 2, Hex("88EE22"));
        p.DrawLine(cx - 3, 27, cx + 3, 27, Hex("88EE22"), 2);
        p.DrawLine(cx - 2, 26, cx - 2, 29, Hex("88EE22"), 1);
        p.DrawLine(cx + 2, 26, cx + 2, 29, Hex("88EE22"), 1);
        // 독 방울
        p.FillEllipse(12, 17, 3, 4, Hex("88EE22"));
        p.FillCircle(12, 14, 2, new Color32(170, 255, 170, 150));
        p.FillEllipse(36, 20, 2, 3, Hex("88EE22"));
    }

    static void DrawMeteor(P p)
    {
        p.BgGradient(Hex("0A0302"), Hex("2A0E04"));
        p.RoundedBorder(8, 1, Hex("CC4400"));
        // 화염 꼬리
        p.DrawLine(40, 6, 22, 36, new Color32(255, 102, 0, 50),  10);
        p.DrawLine(40, 6, 22, 36, new Color32(255, 170, 68, 120), 5);
        p.DrawLine(40, 6, 22, 36, new Color32(255, 221, 136, 200),2);
        // 착지 폭발
        p.FillCircleAlpha(22, 40, 10, new Color32(255, 102, 0, 50));
        p.DrawLine(10, 46, 14, 38, Orange, 2);
        p.DrawLine(22, 47, 22, 40, Orange, 2);
        p.DrawLine(34, 46, 30, 38, Orange, 2);
        p.DrawLine(8,  38, 14, 36, Orange, 2);
        p.DrawLine(36, 38, 30, 36, Orange, 2);
        // 운석 본체
        p.FillCircleGrad(22, 34, 9, Hex("FFCC44"), Hex("FF6600"), Hex("882200"));
        // 운석 크레이터
        p.FillCircleAlpha(18, 31, 3, new Color32(0, 0, 0, 80));
        p.FillCircleAlpha(25, 36, 2, new Color32(0, 0, 0, 70));
        // 파편
        p.FillCircle(38, 20, 2, Orange);
        p.FillCircle(42, 28, 2, Yellow);
    }

    static void DrawBlizzard(P p)
    {
        int cx = p.W / 2, cy = p.H / 2;
        p.BgGradient(Hex("020509"), Hex("081830"));
        p.RoundedBorder(8, 1, Hex("4488CC"));
        var iceCol = Hex("AADDFF");
        // 글로우
        p.FillCircleAlpha(cx, cy, 20, new Color32(136, 204, 255, 20));
        // 6축 눈결정
        p.DrawLine(cx, 4,  cx, 44, iceCol, 2);
        p.DrawLine(7,  13, 41, 35, iceCol, 2);
        p.DrawLine(7,  35, 41, 13, iceCol, 2);
        // 각 가지 측면 돌기
        p.DrawLine(cx, 14, cx - 4, 18, iceCol, 1); p.DrawLine(cx, 14, cx + 4, 18, iceCol, 1);
        p.DrawLine(cx, 34, cx - 4, 30, iceCol, 1); p.DrawLine(cx, 34, cx + 4, 30, iceCol, 1);
        p.DrawLine(31, 11, 29, 16, iceCol, 1);     p.DrawLine(31, 11, 33, 15, iceCol, 1);
        p.DrawLine(17, 37, 19, 32, iceCol, 1);     p.DrawLine(17, 37, 15, 33, iceCol, 1);
        p.DrawLine(37, 31, 32, 30, iceCol, 1);     p.DrawLine(37, 31, 35, 26, iceCol, 1);
        p.DrawLine(11, 17, 16, 18, iceCol, 1);     p.DrawLine(11, 17, 13, 22, iceCol, 1);
        // 중심 원
        p.FillCircle(cx, cy, 4, iceCol);
        p.FillCircleAlpha(cx, cy, 6, new Color32(170, 221, 255, 100));
        // 파티클
        p.FillCircle(8,  10, 2, new Color32(170, 221, 255, 150));
        p.FillCircle(40,  8, 2, new Color32(170, 221, 255, 130));
        p.FillCircle(6,  38, 2, new Color32(170, 221, 255, 130));
        p.FillCircle(42, 40, 2, new Color32(170, 221, 255, 150));
    }

    static void DrawSacrificeSoldier(P p)
    {
        p.BgGradient(Hex("080502"), Hex("2A1A04"));
        p.RoundedBorder(8, 1, Hex("CC8822"));
        // 병사 실루엣 (희미하게)
        p.FillCircleAlpha(16, 28, 4, new Color32(139, 96, 32, 130));
        p.DrawLine(16, 32, 16, 42, new Color32(139, 96, 32, 100), 3);
        // 에너지 흡수 선
        p.DrawLine(16, 27, 32, 10, new Color32(255, 170, 34, 180), 2);
        // 에너지 구슬
        p.FillCircleAlpha(32, 10, 6, new Color32(255, 204, 68, 80));
        p.FillCircle(32, 10, 4, Hex("FFAA22"));
        p.FillCircle(30, 8,  2, new Color32(255, 255, 255, 150));
        // 장군 강화 이펙트
        p.FillCircleAlpha(36, 30, 7, new Color32(255, 170, 34, 50));
        p.FillCircle(36, 24, 4, Hex("DDDDDD"));
        p.DrawLine(36, 28, 36, 36, Hex("DDDDDD"), 3);
        // 강화 화살표
        p.FillTri(36, 14, 33, 20, 39, 20, Yellow);
        p.FillTri(36, 10, 33, 16, 39, 16, new Color32(255, 170, 34, 180));
        // 파티클
        p.FillCircle(22, 20, 2, new Color32(255, 204, 68, 150));
        p.FillCircle(26, 16, 2, new Color32(255, 204, 68, 130));
    }

    static void DrawBind(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("060402"), Hex("1E100A"));
        p.RoundedBorder(8, 1, Hex("886633"));
        // 마법 봉인 원
        p.DrawCircle(cx, cx, 14, 1, new Color32(204, 136, 51, 100));
        p.FillCircleAlpha(cx, cx, 14, new Color32(204, 136, 51, 20));
        // 쇠사슬 (타원 링크들)
        DrawChainLink(p, 14, 11, -30, Hex("AAAAAA"), Hex("CCCCCC"));
        DrawChainLink(p, 22,  9,  30, Hex("BBBBBB"), Hex("DDDDDD"));
        DrawChainLink(p, 30, 12, -30, Hex("AAAAAA"), Hex("CCCCCC"));
        DrawChainLink(p, 10, 23,  60, Hex("AAAAAA"), Hex("CCCCCC"));
        DrawChainLink(p, 38, 23, -60, Hex("AAAAAA"), Hex("CCCCCC"));
        // 중앙 타겟 (속박된 형태)
        p.FillCircle(cx, 24, 6, Hex("331A0A"));
        p.DrawLine(cx - 4, 21, cx + 4, 21, Hex("777777"), 2);
        p.DrawLine(cx - 4, 24, cx + 4, 24, Hex("777777"), 2);
        p.DrawLine(cx - 4, 27, cx + 4, 27, Hex("777777"), 2);
        // 자물쇠
        p.FillRRect(cx - 4, 34, 8, 7, 2, Hex("8B7040"));
        DrawArcPath(p, cx, 34, 4, 180, 360, Hex("8B7040"), 3);
        p.FillCircle(cx, 37, 2, Hex("5A3A10"));
    }

    static void DrawChainLink(P p, int cx, int cy, int rotDeg, Color32 outer, Color32 inner)
    {
        // 타원 링크 근사
        float rad = rotDeg * Mathf.Deg2Rad;
        for (int a = 0; a < 360; a += 4)
        {
            float ar = a * Mathf.Deg2Rad;
            float lx = Mathf.Cos(ar) * 5, ly = Mathf.Sin(ar) * 3.5f;
            float rx = lx * Mathf.Cos(rad) - ly * Mathf.Sin(rad);
            float ry = lx * Mathf.Sin(rad) + ly * Mathf.Cos(rad);
            int px = cx + Mathf.RoundToInt(rx);
            int py = cy + Mathf.RoundToInt(ry);
            p.BlendPixel(px, py, outer);
            p.BlendPixel(px + 1, py, outer);
        }
    }

    static void DrawSuicideSoldier(P p)
    {
        p.BgGradient(Hex("0A0302"), Hex("2A1004"));
        p.RoundedBorder(8, 1, Hex("CC4400"));
        // 폭발 글로우
        p.FillCircleAlpha(28, 26, 16, new Color32(255, 102, 0, 40));
        // 폭발 방사선
        foreach (var pair in new[]{(28,8),(40,12),(46,26),(40,40),(28,46),(16,40),(10,26),(16,12)})
            p.DrawLine(28, 26, pair.Item1, pair.Item2, new Color32(255, 170, 68, 180), 2);
        // 폭발 중심
        p.FillCircleGrad(28, 26, 10, White, Hex("FFEE44"), Hex("FF6600"));
        p.FillCircleAlpha(28, 26, 6, new Color32(255, 255, 255, 120));
        // 병사 실루엣 (달려가는)
        p.FillCircleAlpha(13, 22, 4, new Color32(136, 136, 136, 180));
        p.DrawLine(13, 26, 13, 33, new Color32(136, 136, 136, 160), 3);
        p.DrawLine(13, 28, 9,  25, new Color32(136, 136, 136, 140), 2);
        p.DrawLine(13, 28, 17, 26, new Color32(136, 136, 136, 140), 2);
        p.DrawLine(13, 33, 10, 39, new Color32(136, 136, 136, 140), 2);
        p.DrawLine(13, 33, 16, 38, new Color32(136, 136, 136, 140), 2);
        // 방향 화살표
        p.DrawLine(18, 26, 22, 26, new Color32(255, 136, 51, 150), 1);
    }

    static void DrawBerserker(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("0C0202"), Hex("3A0808"));
        p.RoundedBorder(8, 1, Hex("CC2222"));
        // 화염 배경
        p.FillCircleAlpha(cx, 32, 16, new Color32(255, 68, 0, 30));
        // 불꽃 (좌)
        for (int y = 44; y > 14; y -= 4)
        {
            float t = (44 - y) / 30f;
            int x = (int)(cx - 10 + Mathf.Sin(t * 8) * 3);
            p.FillCircleAlpha(x, y, 4 + (int)(t * 3), new Color32(255, 100, 0, (byte)(80 * (1 - t))));
        }
        // 불꽃 (우)
        for (int y = 44; y > 14; y -= 4)
        {
            float t = (44 - y) / 30f;
            int x = (int)(cx + 10 - Mathf.Sin(t * 8) * 3);
            p.FillCircleAlpha(x, y, 4 + (int)(t * 3), new Color32(255, 68, 0, (byte)(80 * (1 - t))));
        }
        // 교차 쌍검
        FillRRectRotated(p, cx, cx, 32, 5, -35, Hex("D0D0D0"), Hex("888888")); // 검1
        FillRRectRotated(p, cx, cx, 32, 5,  35, Hex("D0D0D0"), Hex("888888")); // 검2
        // 교차점 분노 글로우
        p.FillCircleAlpha(cx, cx, 5, new Color32(255, 34, 0, 180));
        p.FillCircle(cx, cx, 3, Hex("FFAA44"));
    }

    static void DrawIronShield(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("03080E"), Hex("0E2030"));
        p.RoundedBorder(8, 1, Hex("4488BB"));
        // 방어 글로우
        FillShieldShape(p, cx, 5, 46, 10, Hex("334455"), Hex("1A4A5A"));
        DrawShieldOutline(p, cx, 5, 46, 10, Teal, 2);
        // 철판 질감
        p.DrawLine(17, 12, 17, 42, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(cx, 8,  cx, 44, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(31, 12, 31, 42, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(9,  20, 39, 20, new Color32(153, 170, 204, 50), 1);
        p.DrawLine(9,  30, 39, 30, new Color32(153, 170, 204, 50), 1);
        // 리벳
        p.FillCircle(11, 13, 2, Teal); p.FillCircle(37, 13, 2, Teal);
        p.FillCircle(8,  25, 2, Teal); p.FillCircle(40, 25, 2, Teal);
        p.FillCircle(11, 38, 2, Teal); p.FillCircle(37, 38, 2, Teal);
        // 중앙 엠블럼
        p.FillCircle(cx, 28, 8, Hex("334455"));
        p.DrawCircle(cx, 28, 8, 2, Teal);
        p.FillRRect(cx - 1, 21, 3, 14, 1, Teal);
        p.FillRRect(cx - 7, 27, 14, 3, 1, Teal);
        p.FillCircle(cx, 28, 3, Hex("AACCDD"));
        p.FillCircleAlpha(cx - 1, 27, 2, new Color32(255,255,255,70));
    }

    static void DrawArrowRain(P p)
    {
        p.BgGradient(Hex("020602"), Hex("0A1E0A"));
        p.RoundedBorder(8, 1, Hex("228833"));
        // 구름
        p.FillEllipse(12, 10, 7, 5, Hex("334433"));
        p.FillEllipse(22,  8, 9, 6, Hex("334433"));
        p.FillEllipse(34, 10, 8, 5, Hex("334433"));
        p.FillEllipse(24, 12, 18, 6, Hex("2A3A2A"));
        // 화살 비 (5개)
        int[] xs = { 11, 18, 24, 31, 38 };
        int[] ys = { 16, 14, 14, 16, 18 };
        int[] ye = { 38, 42, 44, 42, 38 };
        for (int i = 0; i < 5; i++)
        {
            p.DrawLine(xs[i], ys[i], xs[i], ye[i], Hex("C8A840"), 2);  // 샤프트
            p.FillTri(xs[i], ye[i], xs[i] - 3, ye[i] - 6, xs[i] + 3, ye[i] - 6, Silver); // 촉
            p.FillTri(xs[i], ys[i], xs[i] - 3, ys[i] + 5, xs[i], ys[i] + 4, Green);      // 깃 왼
            p.FillTri(xs[i], ys[i], xs[i] + 3, ys[i] + 5, xs[i], ys[i] + 4, Tint(Green, Hex("000000"), 0.25f)); // 깃 오른
        }
        // 착지 글로우
        p.FillEllipse(24, 46, 16, 2, new Color32(34, 170, 51, 60));
    }

    static void DrawBattleCry(P p)
    {
        p.BgGradient(Hex("080602"), Hex("2A1E04"));
        p.RoundedBorder(8, 1, Hex("CC9922"));
        // 함성 음파 (동심 호)
        DrawSoundWave(p, 28, 24, 12, Hex("FFCC44"), 2, 0.8f);
        DrawSoundWave(p, 28, 24, 18, Hex("FFAA22"), 2, 0.55f);
        DrawSoundWave(p, 28, 24, 24, Hex("FF8800"), 1, 0.35f);
        // 인물
        p.FillCircle(16, 16, 5, Hex("DDDDDD"));
        p.FillCircleAlpha(14, 14, 2, new Color32(255, 255, 255, 60));
        // 입 (함성)
        p.FillEllipse(16, 19, 3, 2, Hex("CC3333"));
        // 몸통
        p.DrawLine(16, 21, 16, 32, Hex("DDDDDD"), 4);
        // 팔 (올린 포즈)
        p.DrawLine(16, 24, 8, 18, Hex("DDDDDD"), 3);
        p.DrawLine(16, 24, 24, 21, Hex("DDDDDD"), 3);
        // 다리
        p.DrawLine(16, 32, 11, 42, Hex("DDDDDD"), 3);
        p.DrawLine(16, 32, 21, 42, Hex("DDDDDD"), 3);
        // 강화 화살표
        p.FillTri(8, 42, 5, 36, 11, 36, Yellow);
        p.DrawLine(8, 36, 8, 28, Yellow, 2);
    }

    static void DrawSoundWave(P p, int cx, int cy, int r, Color32 col, int thick, float alpha)
    {
        var c = new Color32(col.r, col.g, col.b, (byte)(col.a * alpha));
        for (int a = -70; a <= 70; a++)
        {
            float rad = a * Mathf.Deg2Rad;
            int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * r);
            int y = cy - Mathf.RoundToInt(Mathf.Sin(rad) * r);
            for (int t = 0; t < thick; t++)
            {
                float nr = (r + t) / (float)r;
                int nx = cx + Mathf.RoundToInt(Mathf.Cos(rad) * (r + t));
                int ny = cy - Mathf.RoundToInt(Mathf.Sin(rad) * (r + t));
                p.BlendPixel(nx, ny, c);
            }
        }
    }

    static void DrawShockwave(P p)
    {
        p.BgGradient(Hex("060402"), Hex("1A100A"));
        p.RoundedBorder(8, 1, Hex("CC6622"));
        // 충격파 호
        DrawShockArc(p, 8, 34, Hex("FF8833"), 4, 0.7f);
        DrawShockArc(p, 6, 40, Hex("FFAA55"), 3, 0.5f);
        DrawShockArc(p, 4, 46, Hex("FFCC77"), 2, 0.35f);
        // 발원 주먹
        p.FillCircleAlpha(8, 36, 7, new Color32(255, 102, 34, 50));
        p.FillCircle(8, 36, 5, Hex("3A1400"));
        p.DrawCircle(8, 36, 5, 1, new Color32(255, 136, 51, 180));
        p.FillRRect(5, 33, 7, 5, 2, Hex("DDDDCC"));
        p.FillRRect(4, 37, 9, 3, 1, Hex("CCCCBB"));
        // 파편
        p.FillCircle(36, 12, 2, Orange);
        p.FillCircle(42, 20, 2, Yellow);
        p.FillCircle(38,  8, 2, Yellow);
    }

    static void DrawShockArc(P p, int ofsX, int ofsY, Color32 col, int thick, float alpha)
    {
        var c = new Color32(col.r, col.g, col.b, (byte)(col.a * alpha));
        // 부채꼴 호 (오른쪽 상단 방향 ~ Q16,8 40,24 형태)
        int steps = 40;
        float x0 = ofsX, y0 = ofsY;
        float cx2 = 16, cy2 = 8;
        float x2 = 44, y2 = 26;
        int ppx = (int)x0, ppy = (int)y0;
        for (int i = 1; i <= steps; i++)
        {
            float t  = i / (float)steps;
            float mt = 1 - t;
            int nx = Mathf.RoundToInt(mt * mt * x0 + 2 * mt * t * cx2 + t * t * x2);
            int ny = Mathf.RoundToInt(mt * mt * y0 + 2 * mt * t * cy2 + t * t * y2);
            for (int w = 0; w < thick; w++) p.DrawLine(ppx, ppy + w, nx, ny + w, c, 1);
            ppx = nx; ppy = ny;
        }
    }

    static void DrawSwiftStrike(P p)
    {
        p.BgGradient(Hex("060502"), Hex("1E1A04"));
        p.RoundedBorder(8, 1, Hex("CCBB11"));
        // 속도 잔상
        p.DrawLine(4, 20, 22, 20, new Color32(255, 238, 68, 40), 5);
        p.DrawLine(4, 24, 18, 24, new Color32(255, 238, 68, 30), 4);
        p.DrawLine(4, 28, 22, 28, new Color32(255, 238, 68, 25), 3);
        p.DrawLine(6, 18, 24, 18, new Color32(255, 238, 68, 100), 1);
        p.DrawLine(4, 22, 22, 22, new Color32(255, 238, 68, 100), 1);
        p.DrawLine(6, 26, 20, 26, new Color32(255, 238, 68, 90),  1);
        // 검 (25도 기울기)
        FillRotatedRect(p, 20, 20, 30, 6, 25, Hex("D8D8D8"), Hex("888888"));
        // 검 가드
        FillRotatedRect(p, 20, 24, 18, 4, 25, Gold, DkGold);
        // 베기 이펙트
        p.FillCircleAlpha(40, 16, 10, new Color32(255, 238, 68, 50));
        p.DrawLine(30, 10, 46, 30, Hex("FFEE44"), 3);
        p.DrawLine(34,  8, 48, 24, new Color32(255, 204, 34, 150), 2);
        p.DrawLine(28, 12, 44, 32, new Color32(255, 204, 34, 80),  1);
    }

    static void DrawSummonElite(P p)
    {
        int cx = p.W / 2;
        p.BgGradient(Hex("060402"), Hex("1E140A"));
        p.RoundedBorder(8, 1, Hex("CCAA22"));
        // 소환진
        p.DrawCircle(cx, 38, 10, 1, new Color32(204, 153, 34, 100));
        p.FillCircleAlpha(cx, 38, 8, new Color32(204, 153, 34, 25));
        // 왕관 베이스
        p.FillRRect(10, 22, 28, 10, 1, Hex("C8922A"));
        // 왕관 포인트 3개
        p.FillTri(10, 22, 14, 10, 18, 22, Hex("D4A030"));
        p.FillTri(20, 22, 24,  8, 28, 22, Hex("D4A030"));
        p.FillTri(30, 22, 34, 10, 38, 22, Hex("D4A030"));
        p.FillRect(10, 22, 28, 3, new Color32(255, 255, 255, 30)); // 하이라이트
        // 왕관 보석
        p.FillCircle(14, 10, 3, Red);
        p.FillCircle(24,  8, 3, Hex("4488FF"));
        p.FillCircle(34, 10, 3, Red);
        // 측면 보석
        p.FillCircle(10, 27, 2, Hex("22CCAA"));
        p.FillCircle(38, 27, 2, Hex("22CCAA"));
        // 중앙 보석
        p.FillCircle(cx, 27, 4, Hex("4488FF"));
        p.FillCircleAlpha(cx - 2, 25, 2, new Color32(255, 255, 255, 140));
        // 소환 파티클
        p.FillCircle(12, 40, 2, new Color32(255, 204, 68, 150));
        p.FillCircle(36, 42, 2, new Color32(255, 204, 68, 130));
        p.FillCircle(20, 44, 2, new Color32(255, 170, 34, 120));
        p.FillCircle(30, 44, 2, new Color32(255, 170, 34, 110));
    }

    // ─────────────────────────────────────────────────────
    //  ■ 도우미 — 회전 사각형 그리기
    // ─────────────────────────────────────────────────────
    static void FillRotatedRect(P p, int cx, int cy, int length, int height,
                                 float angleDeg, Color32 mainCol, Color32 edgeCol)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        float hL = length / 2f, hH = height / 2f;
        for (int y = cy - length; y <= cy + length; y++)
        for (int x = cx - length; x <= cx + length; x++)
        {
            float lx =  (x - cx) * cos + (y - cy) * sin;
            float ly = -(x - cx) * sin + (y - cy) * cos;
            if (Mathf.Abs(lx) <= hL && Mathf.Abs(ly) <= hH)
            {
                bool edge = Mathf.Abs(ly) > hH - 1.5f;
                p.BlendPixel(x, y, edge ? edgeCol : mainCol);
            }
        }
    }

    static void FillRRectRotated(P p, int cx, int cy, int length, int height,
                                   float angleDeg, Color32 mainCol, Color32 edgeCol)
        => FillRotatedRect(p, cx, cy, length, height, angleDeg, mainCol, edgeCol);

    static void DrawArc(P p, int x0, int y0, int x2, int y2, Color32 col, int thick, bool dashed)
    {
        // 2차 베지어 호 (포물선)
        int cx2 = (x0 + x2) / 2, cy2 = Mathf.Min(y0, y2) - 20;
        int steps = 30, ppx = x0, ppy = y0;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps, mt = 1 - t;
            int nx = Mathf.RoundToInt(mt*mt*x0 + 2*mt*t*cx2 + t*t*x2);
            int ny = Mathf.RoundToInt(mt*mt*y0 + 2*mt*t*cy2 + t*t*y2);
            if (!dashed || (i % 4 < 2)) p.DrawLine(ppx, ppy, nx, ny, col, thick);
            ppx = nx; ppy = ny;
        }
    }

    static void DrawArcPath(P p, int cx, int cy, int r, int startDeg, int endDeg, Color32 col, int thick)
    {
        int ppx = cx + Mathf.RoundToInt(Mathf.Cos(startDeg * Mathf.Deg2Rad) * r);
        int ppy = cy - Mathf.RoundToInt(Mathf.Sin(startDeg * Mathf.Deg2Rad) * r);
        for (int a = startDeg + 5; a <= endDeg; a += 5)
        {
            int nx = cx + Mathf.RoundToInt(Mathf.Cos(a * Mathf.Deg2Rad) * r);
            int ny = cy - Mathf.RoundToInt(Mathf.Sin(a * Mathf.Deg2Rad) * r);
            p.DrawLine(ppx, ppy, nx, ny, col, thick);
            ppx = nx; ppy = ny;
        }
    }

    // ─────────────────────────────────────────────────────
    //  ■ 유틸
    // ─────────────────────────────────────────────────────
    static Color32 Tint(Color32 a, Color32 b, float t) => Color32.Lerp(a, b, t);

    static Color32 Hex(string h)
    {
        h = h.TrimStart('#');
        byte r = Convert.ToByte(h.Substring(0, 2), 16);
        byte g = Convert.ToByte(h.Substring(2, 2), 16);
        byte b = Convert.ToByte(h.Substring(4, 2), 16);
        return new Color32(r, g, b, 255);
    }

    // ═══════════════════════════════════════════════════════
    //  로비 버튼 아이콘
    // ═══════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Icon + "로비 버튼 아이콘", priority = ProjectKMenu.IconPrio + 18)]
    public static void GenerateLobbyButtonIcons()
    {
        EnsureDir(LOBBY_BTN_PATH);

        Save(64, 64, LOBBY_BTN_PATH + "/btn_disassemble.png", DrawBtnDisassemble);
        Save(64, 64, LOBBY_BTN_PATH + "/btn_ability.png",     DrawBtnAbility);
        Save(64, 64, LOBBY_BTN_PATH + "/btn_relic.png",       DrawBtnRelic);

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(LOBBY_BTN_PATH, 64);
        AssetDatabase.SaveAssets();
        Debug.Log("[IconGenerator] 로비 버튼 아이콘 3장 생성 완료.");
    }

    // 장비 분해 버튼 아이콘 — 망치 + 검 교차 (파괴 느낌)
    static void DrawBtnDisassemble(P p)
    {
        p.BgGradient(Hex("120A04"), Hex("3A1A08"));
        p.RoundedBorder(10, 2, Hex("BB6622"));

        // 검 (왼쪽 대각선)
        var bladeCol  = Hex("CCCCCC");
        var bladeEdge = Hex("888888");
        FillRRectRotated(p, 28, 36, 32, 4, -40, bladeCol, bladeEdge);
        // 검 가드
        FillRRectRotated(p, 24, 32, 10, 3,  50, Hex("D4A840"), Hex("8B6820"));

        // 망치 머리 (오른쪽 상단)
        p.FillRRect(36, 8, 18, 12, 3, Hex("888888"));
        p.FillRect(36, 8, 18, 5, Hex("AAAAAA"));
        // 망치 자루
        FillRRectRotated(p, 40, 36, 22, 4, 40, Hex("7A4020"), Hex("5A2A10"));

        // 파편 파티클
        p.FillCircle(18, 44, 2, Hex("FFAA44"));
        p.FillCircle(14, 38, 2, new Color32(255, 170, 68, 180));
        p.FillCircle(50, 20, 2, Hex("FFAA44"));
        p.FillCircle(46, 14, 2, new Color32(255, 170, 68, 180));
    }

    // 어빌리티 목록 버튼 아이콘 — 빛나는 책 + 스탯 기호
    static void DrawBtnAbility(P p)
    {
        p.BgGradient(Hex("04080E"), Hex("101840"));
        p.RoundedBorder(10, 2, Hex("4466CC"));

        // 책 표지
        p.FillRRect(14, 12, 28, 36, 3, Hex("1A2A5A"));
        p.FillRect(14, 12, 28, 4, Hex("2A3A7A"));
        p.DrawLine(14, 12, 14, 48, Hex("4466AA"), 2);
        // 페이지 라인
        p.DrawLine(20, 22, 36, 22, Hex("4466CC"), 1);
        p.DrawLine(20, 29, 36, 29, Hex("4466CC"), 1);
        p.DrawLine(20, 36, 33, 36, Hex("4466CC"), 1);

        // 빛 효과 (우상단)
        p.FillCircleAlpha(46, 18, 10, new Color32(100, 160, 255, 40));
        p.FillCircle(46, 18, 5, Hex("88BBFF"));
        p.FillCircle(46, 18, 3, Hex("CCDDFF"));
        // 방사선
        p.DrawLine(46, 8, 46, 13, Hex("88BBFF"), 1);
        p.DrawLine(46, 23, 46, 28, Hex("88BBFF"), 1);
        p.DrawLine(36, 18, 41, 18, Hex("88BBFF"), 1);
        p.DrawLine(51, 18, 56, 18, Hex("88BBFF"), 1);
    }

    // 유물 버튼 아이콘 — 보라 보석 + 빛줄기
    static void DrawBtnRelic(P p)
    {
        p.BgGradient(Hex("0A0418"), Hex("220A44"));
        p.RoundedBorder(10, 2, Hex("9944CC"));

        // 보석 (다이아몬드 형)
        int cx = 32, cy = 32;
        // 상단 삼각
        p.FillTri(cx, cy - 14, cx - 12, cy, cx + 12, cy, Hex("CC66FF"));
        p.FillTri(cx, cy - 14, cx - 10, cy - 2, cx + 10, cy - 2, Hex("DD99FF"));
        // 하단 삼각
        p.FillTri(cx, cy + 16, cx - 12, cy, cx + 12, cy, Hex("8822AA"));
        // 하이라이트
        p.FillTri(cx - 2, cy - 12, cx - 10, cy - 1, cx, cy - 3, new Color32(255, 255, 255, 80));

        // 빛줄기 방사
        var rayCol = new Color32(200, 100, 255, 60);
        p.DrawLine(cx, cy - 22, cx, cy - 16, Hex("CC66FF"), 1);
        p.DrawLine(cx + 16, cy - 16, cx + 13, cy - 12, Hex("CC66FF"), 1);
        p.DrawLine(cx - 16, cy - 16, cx - 13, cy - 12, Hex("CC66FF"), 1);

        // 파티클
        p.FillCircle(16, 18, 2, new Color32(200, 100, 255, 180));
        p.FillCircle(48, 16, 2, new Color32(200, 100, 255, 160));
        p.FillCircle(50, 46, 2, new Color32(200, 100, 255, 140));
        p.FillCircle(14, 44, 2, new Color32(200, 100, 255, 150));
    }

    static void Save(int w, int h, string assetPath, Action<P> draw)
    {
        var painter = new P(w, h);
        draw(painter);
        painter.Save(assetPath);
    }

    static void EnsureDir(string assetPath)
    {
        string full = Path.Combine(Application.dataPath, "..", assetPath);
        Directory.CreateDirectory(full);
    }

    static void ApplySpriteImportSettings(string folder, int size)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType          = TextureImporterType.Sprite;
            importer.spriteImportMode     = SpriteImportMode.Single;
            importer.spritePivot          = new Vector2(0.5f, 0.5f);
            importer.filterMode           = FilterMode.Bilinear;
            importer.textureCompression   = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize       = 128;
            importer.alphaIsTransparency  = true;
            importer.SaveAndReimport();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ■ Painter — 픽셀 그리기 헬퍼
    // ═══════════════════════════════════════════════════════
    class P
    {
        public int W, H;
        readonly Color32[] px;

        public P(int w, int h)
        {
            W = w; H = h;
            px = new Color32[w * h];
        }

        int Idx(int x, int y) => (H - 1 - y) * W + x;

        public void BlendPixel(int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            int i = Idx(x, y);
            if (c.a == 255) { px[i] = c; return; }
            float a = c.a / 255f, ea = px[i].a / 255f;
            float oa = a + ea * (1 - a);
            if (oa < 0.001f) { px[i] = default; return; }
            px[i] = new Color32(
                (byte)Mathf.RoundToInt((c.r * a + px[i].r * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt((c.g * a + px[i].g * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt((c.b * a + px[i].b * ea * (1 - a)) / oa),
                (byte)Mathf.RoundToInt(oa * 255));
        }

        // ── 배경 ──────────────────────────────────────────
        public void BgGradient(Color32 dark, Color32 mid)
        {
            int cx = W / 2, cy = H / 2;
            float maxD = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                // 둥근 모서리 마스크 (r=10)
                const int R = 10;
                int dx = 0, dy = 0;
                if (x < R && y < R)       { dx = R-x; dy = R-y; }
                else if (x>=W-R && y<R)   { dx = x-(W-R-1); dy = R-y; }
                else if (x<R && y>=H-R)   { dx = R-x; dy = y-(H-R-1); }
                else if (x>=W-R && y>=H-R){ dx = x-(W-R-1); dy = y-(H-R-1); }
                if (dx*dx + dy*dy > R*R && (dx>0||dy>0)) continue;

                float t = Mathf.Clamp01(Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy)) / maxD);
                BlendPixel(x, y, Color32.Lerp(mid, dark, t * t));
            }
        }

        // ── 테두리 ────────────────────────────────────────
        public void RoundedBorder(int r, int thick, Color32 col)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int dx = 0, dy = 0;
                bool corner = false;
                if (x < r && y < r)         { dx=r-x;   dy=r-y;   corner=true; }
                else if(x>=W-r && y<r)      { dx=x-(W-r-1); dy=r-y; corner=true; }
                else if(x<r && y>=H-r)      { dx=r-x;   dy=y-(H-r-1); corner=true; }
                else if(x>=W-r && y>=H-r)   { dx=x-(W-r-1); dy=y-(H-r-1); corner=true; }

                bool onEdge;
                if (corner)
                {
                    float d = Mathf.Sqrt(dx*dx + dy*dy);
                    onEdge = d >= r - thick && d <= r;
                }
                else
                {
                    onEdge = x < thick || x >= W - thick || y < thick || y >= H - thick;
                }
                if (onEdge) BlendPixel(x, y, col);
            }
        }

        // ── 도형 ──────────────────────────────────────────
        public void FillRect(int x, int y, int w, int h, Color32 c)
        {
            for (int py=y; py<y+h; py++) for (int px2=x; px2<x+w; px2++) BlendPixel(px2,py,c);
        }

        public void FillRRect(int x, int y, int w, int h, int rad, Color32 c)
        {
            for (int py=y; py<y+h; py++)
            for (int px2=x; px2<x+w; px2++)
            {
                int dx=0, dy=0;
                if (px2<x+rad && py<y+rad)     { dx=x+rad-px2; dy=y+rad-py; }
                else if(px2>=x+w-rad && py<y+rad){ dx=px2-(x+w-rad-1); dy=y+rad-py; }
                else if(px2<x+rad && py>=y+h-rad){ dx=x+rad-px2; dy=py-(y+h-rad-1); }
                else if(px2>=x+w-rad && py>=y+h-rad){ dx=px2-(x+w-rad-1); dy=py-(y+h-rad-1); }
                if (dx*dx+dy*dy <= rad*rad || (dx==0&&dy==0)) BlendPixel(px2,py,c);
            }
        }

        public void FillCircle(int cx, int cy, int r, Color32 c)
        {
            for (int y=cy-r; y<=cy+r; y++) for (int x=cx-r; x<=cx+r; x++)
                if ((x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r) BlendPixel(x,y,c);
        }

        public void FillCircleAlpha(int cx, int cy, int r, Color32 c)
            => FillCircle(cx, cy, r, c);

        public void DrawCircle(int cx, int cy, int r, int thick, Color32 c)
        {
            for (int y=cy-r-thick; y<=cy+r+thick; y++) for (int x=cx-r-thick; x<=cx+r+thick; x++)
            {
                int d2 = (x-cx)*(x-cx)+(y-cy)*(y-cy);
                if (d2>=(r-thick)*(r-thick) && d2<=(r+thick)*(r+thick)) BlendPixel(x,y,c);
            }
        }

        public void FillCircleGrad(int cx, int cy, int r, Color32 inner, Color32 mid, Color32 outer)
        {
            for (int y=cy-r; y<=cy+r; y++) for (int x=cx-r; x<=cx+r; x++)
            {
                int d2 = (x-cx)*(x-cx)+(y-cy)*(y-cy);
                if (d2 > r*r) continue;
                float t = Mathf.Sqrt(d2) / r;
                Color32 c = t < 0.5f ? Color32.Lerp(inner, mid, t*2) : Color32.Lerp(mid, outer, (t-0.5f)*2);
                BlendPixel(x, y, c);
            }
        }

        public void FillEllipse(int cx, int cy, int rx, int ry, Color32 c)
        {
            for (int y=cy-ry; y<=cy+ry; y++) for (int x=cx-rx; x<=cx+rx; x++)
            {
                float dx = (float)(x-cx)/rx, dy2 = (float)(y-cy)/ry;
                if (dx*dx+dy2*dy2 <= 1f) BlendPixel(x,y,c);
            }
        }

        public void DrawLine(int x1, int y1, int x2, int y2, Color32 c, int thick=1)
        {
            int dx = Mathf.Abs(x2-x1), dy = Mathf.Abs(y2-y1);
            int sx = x1<x2?1:-1, sy = y1<y2?1:-1;
            int err = dx-dy, x=x1, y=y1;
            int h2 = thick/2;
            while (true)
            {
                for (int py=y-h2; py<=y+h2; py++) for (int px2=x-h2; px2<=x+h2; px2++) BlendPixel(px2,py,c);
                if (x==x2&&y==y2) break;
                int e2=2*err;
                if (e2>-dy){err-=dy;x+=sx;}
                if (e2< dx){err+=dx;y+=sy;}
            }
        }

        public void FillTri(int x1,int y1, int x2,int y2, int x3,int y3, Color32 c)
        {
            int minX=Mathf.Min(x1,Mathf.Min(x2,x3)), maxX=Mathf.Max(x1,Mathf.Max(x2,x3));
            int minY=Mathf.Min(y1,Mathf.Min(y2,y3)), maxY=Mathf.Max(y1,Mathf.Max(y2,y3));
            for (int py=minY; py<=maxY; py++) for (int px2=minX; px2<=maxX; px2++)
            {
                float d1=Sign(px2,py,x1,y1,x2,y2), d2=Sign(px2,py,x2,y2,x3,y3), d3=Sign(px2,py,x3,y3,x1,y1);
                if (!((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0))) BlendPixel(px2,py,c);
            }
        }
        float Sign(int px,int py,int x1,int y1,int x2,int y2) => (px-x2)*(y1-y2)-(float)(x1-x2)*(py-y2);

        public void Save(string assetPath)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
