using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// ============================================================
//  EventDatabaseCreator.cs  [Editor Only]
//  이벤트 SO 10종 + EventDatabase.asset 자동 생성 도구.
//
//  Tools > Project K > Event > Create Event Database
//
//  생성 위치:
//    Assets/_project/Data/Events/   ← 개별 EventData SO
//    Assets/Resources/EventDatabase.asset
//
//  이벤트 목록:
//    01. InjuredSoldier     — 부상당한 병사  (선택지형)
//    02. MysteriousPotion   — 신비한 묘약    (선택지형)
//    03. MerchantOffer      — 상인의 제안    (선택지형)
//    04. BloodAltar         — 피의 제단      (선택지형)
//    05. Crossroads         — 갈림길의 첩자  (선택지형)
//    06. AbilityDiscovery   — 어빌리티 발견  (즉시보상형)
//    07. LoneVeteran        — 고독한 노병    (즉시보상형)
//    08. AbandonedWarehouse — 방치된 창고    (즉시보상형)
//    09. WarRelic           — 전쟁 유물      (즉시보상형)
//    10. BlackMarket        — 상인의 밀거래  (선택지형)
//    11. TravelingMerchant  — 행상인의 좌판  (상점 스테이지 전용)
//    12. StragglerSoldiers  — 패잔병 무리    (선택지형 · 용병 고용)
//    13. WanderingMercenary — 떠돌이 용병    (선택지형 · 용병 고용)
//    14. PromisingSoldier   — 눈에 띄는 병사 (선택지형 · 용병 고용)
//
//  ⚠ 12~14 로 고용되는 장수는 아군 영웅 외형이고 직업도 무작위다
//    (기사·궁수·법사·방패병). 적을 포섭하는 컨셉이나 특정 직업을 가리키는
//    이름("검객" 등)을 쓰면 실제로 뽑히는 장수와 그림이 어긋난다.
//
//  ⚠ 11번은 랜덤 이벤트 풀에 들어가면 안 된다.
//    상점 스테이지에서만 EventDatabase.ShopEventId 로 직접 꺼내 쓰고,
//    GetRandom() 은 이 ID 를 제외한다.
// ============================================================

public static class EventDatabaseCreator
{
    const string DataRoot  = "Assets/_project/Data";
    const string EventDir  = "Assets/_project/Data/Events";
    const string DBPath    = "Assets/Resources/EventDatabase.asset";
    const string IllustDir = "Assets/_project/3.Textures/Events";

    // ── 이벤트 ↔ 삽화 매핑 ────────────────────────────────────
    //  삽화 8종을 이벤트 10종에 배분한다 (분위기가 겹치면 공유).
    //  PNG 는 아이콘·텍스처 > 이벤트 일러스트 로 생성한다.
    static readonly Dictionary<string, string> IllustMap = new()
    {
        { "InjuredSoldier",     "evt_soldier"  },  // 부상당한 병사
        { "MysteriousPotion",   "evt_potion"   },  // 신비한 묘약
        { "MerchantOffer",      "evt_merchant" },  // 상인의 제안
        { "BloodAltar",         "evt_shrine"   },  // 피의 제단
        { "Crossroads",         "evt_ambush"   },  // 갈림길의 첩자
        // 발견형 4종은 전용 삽화를 쓴다 — 무엇을 발견했는지가 그림으로 읽혀야
        // "발견 → 행동 → 보상" 의 첫 박자가 성립한다 (공용 삽화는 사건이 안 보인다)
        { "AbilityDiscovery",   "evt_tome"      },  // 어빌리티 발견 — 펼쳐진 전술서
        { "LoneVeteran",        "evt_veteran"   },  // 고독한 노병 — 모닥불 옆 실루엣
        { "AbandonedWarehouse", "evt_warehouse" },  // 방치된 창고 — 상자 더미
        { "WarRelic",           "evt_relic"     },  // 전쟁 유물 — 묻힌 갑옷과 전투석
        { "BlackMarket",        "evt_merchant" },  // 상인의 밀거래
        { "TravelingMerchant",  "evt_merchant" },  // 행상인의 좌판 (상점 스테이지)
        { "StragglerSoldiers",  "evt_soldier"  },  // 패잔병 무리
        { "WanderingMercenary", "evt_forest"   },  // 떠돌이 용병
        { "PromisingSoldier",   "evt_soldier"  },  // 눈에 띄는 병사
    };

    [MenuItem(ProjectKMenu.Data + "이벤트", priority = ProjectKMenu.DataPrio + 18)]
    public static void CreateAll()
    {
        // ── 폴더 준비 ──────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(DataRoot))
            AssetDatabase.CreateFolder("Assets/_project", "Data");
        if (!AssetDatabase.IsValidFolder(EventDir))
            AssetDatabase.CreateFolder(DataRoot, "Events");
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // ── Database 로드 또는 생성 ────────────────────────────
        var db = AssetDatabase.LoadAssetAtPath<EventDatabase>(DBPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EventDatabase>();
            AssetDatabase.CreateAsset(db, DBPath);
        }
        var so  = new SerializedObject(db);
        var arr = so.FindProperty("_events");
        arr.ClearArray();

        // ── 01. 부상당한 병사 ─────────────────────────────────
        Add(arr, Make("InjuredSoldier", "부상당한 병사",
            "아군 병사 하나가 깊은 상처를 입고 막사 귀퉁이에서 신음하고 있습니다.\n" +
            "처치 곤란한 상황, 어떻게 하시겠습니까?",
            Choice("전선에 내보낸다",
                "병사를 전선에 배치했습니다. 그는 장렬히 싸우다 쓰러졌습니다.\n[병사 1명 영구 감소]",
                Reward(EventRewardType.RemoveSoldier, intVal: 1, "병사 -1 (영구)")),
            Choice("치료시킨다",
                "병사를 정성껏 돌봤습니다. 상처가 아물며 다시 일어섰습니다.\n[병사 1명 영구 획득]",
                Reward(EventRewardType.AddSoldier, intVal: 1, "병사 +1 (영구)")),
            Choice("그냥 지나친다",
                "외면하고 자리를 떴습니다. 병사의 신음 소리가 귓가에 남습니다.")
        ));

        // ── 02. 신비한 묘약 ───────────────────────────────────
        Add(arr, Make("MysteriousPotion", "신비한 묘약",
            "쓰러진 적에게서 정체불명의 약병을 발견했습니다.\n" +
            "색깔은 영롱하지만 냄새가 좀 이상합니다.",
            ChoiceProb("전부 마신다",
                successText : "단숨에 마셨습니다! 강한 힘이 솟구칩니다.\n[랜덤 강화 특성 2개 획득]",
                failText    : "단숨에 마셨습니다! 몸이 달아오르다가 속이 뒤틀립니다.\n[랜덤 강화 특성 1개 / 랜덤 부작용 특성 1개 획득]",
                rate        : 0.5f,
                successRewards: Rewards(
                    Reward(EventRewardType.RandomTraitBuff, "랜덤 강화 특성 획득"),
                    Reward(EventRewardType.RandomTraitBuff, "랜덤 강화 특성 추가 획득")),
                failRewards: Rewards(
                    Reward(EventRewardType.RandomTraitBuff,   "랜덤 강화 특성 획득"),
                    Reward(EventRewardType.RandomTraitDebuff, "랜덤 부작용 특성 획득"))),
            ChoiceProb("반만 마신다",
                successText : "절반만 마셨습니다. 몸이 가벼워집니다.\n[랜덤 강화 특성 1개 획득]",
                failText    : "절반만 마셨는데 이상한 기운이 퍼집니다.\n[랜덤 부작용 특성 1개 획득]",
                rate        : 0.5f,
                successRewards: Rewards(Reward(EventRewardType.RandomTraitBuff,   "랜덤 강화 특성 획득")),
                failRewards:   Rewards(Reward(EventRewardType.RandomTraitDebuff, "랜덤 부작용 특성 획득"))),
            Choice("버린다",
                "수상한 약을 멀리 던져버렸습니다.")
        ));

        // ── 03. 상인의 제안 ───────────────────────────────────
        Add(arr, Make("MerchantOffer", "상인의 제안",
            "낡은 짐마차에서 상인이 손짓합니다.\n" +
            "\"장군님, 좋은 물건이 있습죠. 조금 비싸지만 후회는 없을 겁니다.\"",
            Choice("비싸게 산다",
                "금화를 건넸습니다. 그가 건넨 물건에서 희미한 빛이 납니다.\n[골드 소모 / 유익한 특성 획득]",
                ScaledReward(EventRewardType.SpendItem, eItem.Gold, 60),
                Reward(EventRewardType.RandomTraitBuff, "유익한 특성 획득")),
            Choice("그냥 지나친다",
                "고개를 젓고 발걸음을 옮겼습니다.")
        ));

        // ── 04. 피의 제단 ─────────────────────────────────────
        Add(arr, Make("BloodAltar", "피의 제단",
            "숲 속 빈터에 이끼 낀 제단이 있습니다.\n" +
            "어떤 이들은 여기서 힘을 빌렸다고 합니다. 그 대가는 각자 달랐지만.",
            Choice("피를 바친다",
                "손바닥을 그어 피를 제단에 흘렸습니다. 강렬한 힘이 솟구치지만 몸이 허약해집니다.\n[최대체력 -15% / 어빌리티 2택]",
                Reward(EventRewardType.AddTrait,          intVal: (int)TraitType.Event_BloodPact, "피의 계약: 최대체력 -15%"),
                Reward(EventRewardType.OpenAbilitySelect, intVal: 2, "어빌리티 2택")),
            Choice("무기를 올린다",
                "귀한 강화석을 제단 위에 올렸습니다. 무기에서 빛이 납니다.\n[장비 강화석 3개 소모 / 어빌리티 1택]",
                costHint: "강화석 3",
                Reward(EventRewardType.SpendItem,         eItem.EquipUpgradeStone, 3, "강화석 -3"),
                Reward(EventRewardType.OpenAbilitySelect, intVal: 1, "어빌리티 1택")),
            Choice("제단을 부순다",
                "제단에 주먹을 날려 부숴버렸습니다. 저주가 흩어지며 아군에게 퍼졌습니다.\n[방어율 -10% 특성 획득]",
                Reward(EventRewardType.AddTrait, intVal: (int)TraitType.Event_AltarCurse, "제단의 저주: 방어율 -10%"))
        ));

        // ── 05. 갈림길의 첩자 ─────────────────────────────────
        Add(arr, Make("Crossroads", "갈림길의 첩자",
            "풀숲에서 결박된 적 첩자를 발견했습니다.\n" +
            "어떻게 하시겠습니까?",
            Choice("처형한다",
                "단호하게 결단을 내렸습니다. 아군이 그 결의를 보며 사기가 오릅니다.\n[공격력 +8% 특성]",
                Reward(EventRewardType.AddTrait, intVal: (int)TraitType.Event_ExecutionMorale, "처형의 사기: 공격력 +8%")),
            Choice("정보를 받아낸다",
                "첩자에게서 적진의 이동 경로를 캐냈습니다.\n[경험치 획득 +20% 특성]",
                Reward(EventRewardType.AddTrait, intVal: (int)TraitType.Event_SpyInfo, "첩자 정보: 경험치 +20%")),
            Choice("풀어준다",
                "첩자를 풀어줬습니다. 그는 뭔가를 남기고 사라졌습니다.\n[골드 획득 / 다음 스테이지 → 엘리트]",
                ScaledReward(EventRewardType.AddItem, eItem.Gold, 180),
                Reward(EventRewardType.NextStageElite, "다음 스테이지 → 엘리트"))
        ));

        // ── 06. 어빌리티 발견 ────────────────────────────────
        //  ⚠ 즉시보상(MakeInstant)에서 한 박자 있는 형태로 바꿨다
        //    예전엔 팝업이 열리자마자 어빌리티 선택창이 떠서, 플레이어는
        //    "무엇 때문에" 고르는지 모른 채 카드부터 봤다. 지금은
        //    발견 서술 → 행동 버튼 → 그 결과로 보상이 열린다.
        Add(arr, Make("AbilityDiscovery", "어빌리티 발견",
            "무너진 서고 바닥에서 전조가 새겨진 상자를 찾았습니다.\n" +
            "봉인을 뜯자 빛바랜 전술서 한 권이 모습을 드러냅니다.",
            Choice("전술서를 펼친다",
                "전조에 숨겨진 고대의 전술을 찾았습니다.\n" +
                "페이지를 넘길수록 새로운 싸움법이 머릿속에 그려집니다.\n[어빌리티 1택]",
                Reward(EventRewardType.OpenAbilitySelect, intVal: 1, "어빌리티 1택"))
        ));

        // ── 07. 고독한 노병 ──────────────────────────────────
        Add(arr, Make("LoneVeteran", "고독한 노병",
            "한쪽 다리가 불편한 노병이 막사 밖에 홀로 앉아 있습니다.\n" +
            "\"장군님, 제가 당신 같았을 때의 이야기를 해드릴까요?\"",
            Choice("이야기를 듣는다",
                "노병은 젊은 날의 행군을 밤새 들려주었습니다.\n" +
                "그 걸음걸이를 흉내 내자 부대의 발이 한결 가벼워집니다.\n[이동속도 +10% 특성]",
                Reward(EventRewardType.AddTrait, intVal: (int)TraitType.Event_VeteranHeritage, "노병의 유산: 이동속도 +10%"))
        ));

        // ── 08. 방치된 창고 ──────────────────────────────────
        Add(arr, Make("AbandonedWarehouse", "방치된 창고",
            "길가의 허름한 창고 문이 반쯤 열려 있습니다.\n" +
            "안을 들여다보니 오래된 상자들이 먼지를 뒤집어쓴 채 쌓여 있습니다.",
            Choice("상자를 열어본다",
                "못질을 뜯어내자 기름 먹인 천에 싸인 장비 한 벌이 나왔습니다.\n" +
                "구석의 낡은 자루에서는 동전 소리가 납니다.\n[장비 박스 1개 + 골드 획득]",
                Reward(EventRewardType.AddItem, eItem.EquipBox, 1, "장비 박스 +1"),
                ScaledReward(EventRewardType.AddItem, eItem.Gold, 220))
        ));

        // ── 09. 전쟁 유물 ────────────────────────────────────
        Add(arr, Make("WarRelic", "전쟁 유물",
            "전장터 구석에서 오래된 전쟁의 흔적을 발견했습니다.\n" +
            "녹슨 갑옷 조각에서 아직도 전투의 기운이 느껴집니다.",
            Choice("유물을 수습한다",
                "흙을 걷어내자 갑옷에서 떨어져 나온 강화석 조각이 드러났습니다.\n" +
                "먼저 스러진 이름들이 손끝에 닿는 듯합니다.\n[장비 강화석 3개 + 환생 포인트 5 획득]",
                Reward(EventRewardType.AddItem, eItem.EquipUpgradeStone,   3, "장비 강화석 +3"),
                Reward(EventRewardType.AddItem, eItem.ReincarnationPoint,  5, "환생 포인트 +5"))
        ));

        // ── 10. 상인의 밀거래 ─────────────────────────────────
        Add(arr, Make("BlackMarket", "상인의 밀거래",
            "수상한 행색의 상인이 슬쩍 다가옵니다.\n" +
            "\"장군님... 공식 루트에선 구하기 어려운 물건이죠.\"",
            Choice("골드로 산다",
                "금화를 건네자 상인이 묵직한 상자를 넘겼습니다.\n[골드 소모 / 장비 박스 2개]",
                ScaledReward(EventRewardType.SpendItem, eItem.Gold, 85),
                Reward(EventRewardType.AddItem,   eItem.EquipBox,   2, "장비 박스 +2")),
            Choice("강화석으로 산다",
                "손때 묻은 강화석을 대가로 치렀습니다. 상인이 환하게 웃으며 상자를 건넸습니다.\n[장비 강화석 8개 소모 / 장비 박스 2개]",
                costHint: "강화석 8",
                Reward(EventRewardType.SpendItem, eItem.EquipUpgradeStone, 8, "장비 강화석 -8"),
                Reward(EventRewardType.AddItem,   eItem.EquipBox,          2, "장비 박스 +2")),
            Choice("거절한다",
                "시선을 피하며 자리를 떠났습니다.")
        ));

        // ── 11. 행상인의 좌판 (상점 스테이지 전용) ────────────
        //  상점 팝업이 예고 없이 뜨면 무슨 상황인지 읽히지 않는다.
        //  행상인을 만나는 장면을 먼저 보여주고, 좌판을 들여다보는
        //  선택을 했을 때 비로소 RunShopPopup 이 열린다.
        Add(arr, Make("TravelingMerchant", "행상인의 좌판",
            "길목에 짐마차 한 대가 서 있습니다. 행상인이 천막을 걷어 좌판을 펼칩니다.\n" +
            "\"먼 길 오셨군요, 장군님. 무기도 있고, 비법도 있고, 사람도 있습니다.\n" +
            " 값만 치르신다면 말이죠.\"",
            Choice("상품을 본다",
                "행상인이 좌판 위의 천을 걷었습니다.\n무엇을 살지 고르십시오.",
                Reward(EventRewardType.OpenRunShop, "상점 열기")),
            Choice("그냥 지나친다",
                "행상인에게 눈길만 주고 발걸음을 옮겼습니다.\n" +
                "\"...다음 길목에서 또 뵙지요.\"")
        ));

        // ── 12. 패잔병 무리 (용병 고용 — 무료) ────────────────
        //  고용을 고르면 MercenaryShopPopup 이 이어서 열린다.
        //  "돌려보낸다" 에 보상을 달지 않는 이유: 고용 팝업 안에 이미
        //  '전부 돌려보내 용병 조각으로 바꾼다' 가 있다. 여기서 또 주면 이중 보상이다.
        Add(arr, Make("StragglerSoldiers", "패잔병 무리",
            "무너진 전선에서 빠져나온 병사 몇이 무기를 든 채 다가옵니다.\n" +
            "\"소속을 잃었습니다. 싸울 곳만 있으면 됩니다.\"",
            Choice("무리를 받아준다",
                "무리를 부대에 들였습니다. 그중 쓸 만한 자를 한 명 골라 보십시오.\n[용병 고용]",
                Reward(EventRewardType.OpenMercenary, "용병 고용")),
            Choice("돌려보낸다",
                "먹일 입을 늘릴 여유가 없습니다. 무리는 말없이 발길을 돌렸습니다.")
        ));

        // ── 13. 떠돌이 용병 (용병 고용 — 골드) ────────────────
        Add(arr, Make("WanderingMercenary", "떠돌이 용병",
            "야영지 불빛을 보고 낯선 이가 찾아왔습니다. 행색은 남루하지만 눈매가 매섭습니다.\n" +
            "\"값을 쳐주신다면 이 한 몸 맡기겠습니다.\"",
            Choice("선금을 치른다",
                "금화를 세어 건넸습니다. 그가 짐을 풀고 막사 한켠에 자리를 잡습니다.\n[골드 소모 / 용병 고용]",
                ScaledReward(EventRewardType.SpendItem, eItem.Gold, 70),
                Reward(EventRewardType.OpenMercenary, "용병 고용")),
            Choice("거절한다",
                "고개를 젓자 그는 어깨를 으쓱하고 어둠 속으로 사라졌습니다.")
        ));

        // ── 14. 눈에 띄는 병사 (용병 고용 — 골드 또는 조각) ───
        //  용병 조각으로도 살 수 있는 유일한 고용 경로.
        //  조각은 병사를 늘리는 재화이므로 "병사 중에서 발탁한다" 는 이 이벤트에만 붙인다.
        Add(arr, Make("PromisingSoldier", "눈에 띄는 병사",
            "훈련장 한쪽에서 병사 하나가 눈에 들어옵니다.\n" +
            "창을 쥔 자세도, 대열을 읽는 눈도 여느 병사와 다릅니다.",
            Choice("정식으로 발탁한다",
                "그를 장수로 세웠습니다. 새 갑주가 제법 어울립니다.\n[골드 소모 / 용병 고용]",
                ScaledReward(EventRewardType.SpendItem, eItem.Gold, 55),
                Reward(EventRewardType.OpenMercenary, "용병 고용")),
            Choice("부대에서 인재를 추린다",
                "병사들 사이에서 될 만한 자를 추려 장수로 올렸습니다.\n[용병 조각 20 소모 / 용병 고용]",
                costHint: "용병 조각 20",
                Reward(EventRewardType.SpendItem,     eItem.SoldierShard, 20, "용병 조각 -20"),
                Reward(EventRewardType.OpenMercenary, "용병 고용")),
            Choice("그냥 둔다",
                "아직은 대열 안에 두기로 했습니다.")
        ));

        // ── 저장 ──────────────────────────────────────────────
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EventDatabaseCreator] 이벤트 {arr.arraySize}종 + EventDatabase.asset 생성 완료");
    }

    // ══════════════════════════════════════════════════════════
    //  빌더 헬퍼
    // ══════════════════════════════════════════════════════════

    // ── EventData 생성 ────────────────────────────────────────

    static EventData Make(string id, string title, string body, params EventChoice[] choices)
    {
        var data = LoadOrCreate(id);
        data.EventId           = id;
        data.Title             = title;
        data.Body              = body;
        data.Choices           = choices;
        data.InstantRewards    = null;
        data.InstantResultText = string.Empty;
        EditorUtility.SetDirty(data);
        return data;
    }

    /// <summary>
    /// 선택지 없이 열자마자 보상을 주는 이벤트.
    ///
    /// ⚠ 지금 이걸 쓰는 이벤트는 하나도 없다 — 일부러 그렇다
    ///   팝업이 열리는 순간 보상이 튀어나오면 "무엇 때문에 받았는지" 가 안 보여
    ///   뜬금없다는 인상만 남는다. 발견형 4종(어빌리티 발견·고독한 노병·
    ///   방치된 창고·전쟁 유물)은 전부 선택지 1개짜리로 바꿔
    ///   서술 → 행동 버튼 → 보상 순서를 만들었다.
    ///   새 이벤트도 같은 이유로 Make + Choice 를 쓸 것.
    ///   (EventPopup 의 즉시보상 경로 자체는 살아 있어 언제든 되살릴 수 있다)
    /// </summary>
    static EventData MakeInstant(string id, string title, string body,
        string resultText, params EventReward[] rewards)
    {
        var data = LoadOrCreate(id);
        data.EventId           = id;
        data.Title             = title;
        data.Body              = body;
        data.Choices           = null;
        data.InstantRewards    = rewards;
        data.InstantResultText = resultText;
        EditorUtility.SetDirty(data);
        return data;
    }

    static EventData LoadOrCreate(string id)
    {
        string path = $"{EventDir}/Event_{id}.asset";
        var data = AssetDatabase.LoadAssetAtPath<EventData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EventData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.Illustration = LoadIllust(id);
        return data;
    }

    // 매핑·파일이 없으면 LogError — 삽화가 조용히 빠지면 팝업이 빈 상자로 뜬다.
    static Sprite LoadIllust(string id)
    {
        if (!IllustMap.TryGetValue(id, out var file))
        {
            Debug.LogError($"[EventDatabaseCreator] 삽화 매핑 없음: {id} — IllustMap 에 추가하세요.");
            return null;
        }

        string path = $"{IllustDir}/{file}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"[EventDatabaseCreator] 삽화를 Sprite 로 읽지 못함: {path}\n" +
                           "Tools > Project K > 아이콘·텍스처 > 이벤트 일러스트 를 먼저 실행하세요.");
        return sprite;
    }

    // ── 선택지 ────────────────────────────────────────────────

    static EventChoice Choice(string label, string resultText,
        string costHint = "", params EventReward[] rewards)
    {
        return new EventChoice
        {
            Label          = label,
            ResultText     = resultText,
            SuccessRate    = 1f,
            SuccessRewards = rewards,
            FailRewards    = System.Array.Empty<EventReward>(),
            CostHint       = costHint,
        };
    }

    static EventChoice Choice(string label, string resultText, params EventReward[] rewards)
        => Choice(label, resultText, "", rewards);

    static EventChoice ChoiceProb(string label,
        string successText, string failText, float rate,
        EventReward[] successRewards, EventReward[] failRewards)
    {
        // ⚠ failText 를 반드시 담는다
        //   예전엔 이 값을 받고도 버려서 EventChoice 에 성공 텍스트만 남았다.
        //   실패해도 성공 글이 떠서 "강화 특성 2개 획득" 옆에 저주 특성이 붙는 그림이 나왔다.
        return new EventChoice
        {
            Label          = label,
            ResultText     = successText,
            FailResultText = failText,
            SuccessRate    = rate,
            SuccessRewards = successRewards,
            FailRewards    = failRewards,
        };
    }

    // ── 보상 ─────────────────────────────────────────────────

    static EventReward Reward(EventRewardType type, string desc = "")
        => new EventReward { Type = type, Item = eItem.None, IntValue = 0, Description = desc };

    static EventReward Reward(EventRewardType type, int intVal, string desc = "")
        => new EventReward { Type = type, Item = eItem.None, IntValue = intVal, Description = desc };

    static EventReward Reward(EventRewardType type, eItem item, int amount, string desc = "")
        => new EventReward { Type = type, Item = item, IntValue = amount, Description = desc };

    /// <summary>
    /// 스테이지 보상 대비 비율로 정하는 보상/비용. percent 는 % 값이다.
    ///
    /// ⚠ 고정 골드는 후반에 무의미해진다
    ///   30스테이지 클리어 보상이 1,950 골드인데 이벤트가 100 골드를 주면
    ///   "얻었다" 는 감각이 없다. 비율로 두면 언제 만나도 체감이 같다.
    ///
    /// ■ 기준선
    ///   획득 : 180~250%  — 스테이지 클리어를 웃돌아야 "한몫 챙겼다" 가 된다.
    ///                      이벤트는 런에 10칸뿐이고 그중 골드를 주는 건 일부다.
    ///                      클리어 보상보다 적게 주면 이벤트를 만날 이유가 없다.
    ///   비용 : 55~85%    — 한 스테이지 수입을 통째로 쓰는 무게.
    ///
    /// ⚠ Description 을 비워 둔다
    ///   실제 수량은 매번 다르다. EventPopup 이 그때그때 계산해 표시한다.
    /// </summary>
    static EventReward ScaledReward(EventRewardType type, eItem item, int percent)
        => new EventReward { Type = type, Item = item, IntValue = percent, ScaleByStageReward = true };

    static EventReward[] Rewards(params EventReward[] r) => r;

    // ── 배열 등록 ─────────────────────────────────────────────

    static void Add(SerializedProperty arr, EventData data)
    {
        int idx = arr.arraySize;
        arr.arraySize = idx + 1;
        arr.GetArrayElementAtIndex(idx).objectReferenceValue = data;
    }
}
