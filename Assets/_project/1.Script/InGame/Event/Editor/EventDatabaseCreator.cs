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
        { "AbilityDiscovery",   "evt_mystery"  },  // 어빌리티 발견
        { "LoneVeteran",        "evt_soldier"  },  // 고독한 노병
        { "AbandonedWarehouse", "evt_dark"     },  // 방치된 창고
        { "WarRelic",           "evt_forest"   },  // 전쟁 유물
        { "BlackMarket",        "evt_merchant" },  // 상인의 밀거래
        { "TravelingMerchant",  "evt_merchant" },  // 행상인의 좌판 (상점 스테이지)
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
                successText : "단숨에 마셨습니다! 강한 힘이 솟구칩니다.\n[강화 특성 2개 획득]",
                failText    : "단숨에 마셨습니다! 몸이 달아오르다가 속이 뒤틀립니다.\n[강화 특성 1개 획득 / 부작용 특성 획득]",
                rate        : 0.5f,
                successRewards: Rewards(
                    Reward(EventRewardType.RandomTraitBuff, "강화 특성 획득"),
                    Reward(EventRewardType.RandomTraitBuff, "강화 특성 추가 획득")),
                failRewards: Rewards(
                    Reward(EventRewardType.RandomTraitBuff,   "강화 특성 획득"),
                    Reward(EventRewardType.RandomTraitDebuff, "부작용 특성 획득"))),
            ChoiceProb("반만 마신다",
                successText : "절반만 마셨습니다. 몸이 가벼워집니다.\n[강화 특성 1개 획득]",
                failText    : "절반만 마셨는데 이상한 기운이 퍼집니다.\n[디버프 특성 획득]",
                rate        : 0.5f,
                successRewards: Rewards(Reward(EventRewardType.RandomTraitBuff,   "강화 특성 획득")),
                failRewards:   Rewards(Reward(EventRewardType.RandomTraitDebuff, "디버프 특성 획득"))),
            Choice("버린다",
                "수상한 약을 멀리 던져버렸습니다.")
        ));

        // ── 03. 상인의 제안 ───────────────────────────────────
        Add(arr, Make("MerchantOffer", "상인의 제안",
            "낡은 짐마차에서 상인이 손짓합니다.\n" +
            "\"장군님, 좋은 물건이 있습죠. 조금 비싸지만 후회는 없을 겁니다.\"",
            Choice("비싸게 산다",
                "금화를 건넸습니다. 그가 건넨 물건에서 희미한 빛이 납니다.\n[골드 200 소모 / 유익한 특성 획득]",
                costHint: "골드 200",
                Reward(EventRewardType.SpendItem,       eItem.Gold, 200, "골드 -200"),
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
                "첩자를 풀어줬습니다. 그는 뭔가를 남기고 사라졌습니다.\n[골드 150 획득 / 다음 스테이지 → 엘리트]",
                Reward(EventRewardType.AddItem,        eItem.Gold, 150, "골드 +150"),
                Reward(EventRewardType.NextStageElite, "다음 스테이지 → 엘리트"))
        ));

        // ── 06. 어빌리티 발견 (즉시보상) ─────────────────────
        Add(arr, MakeInstant("AbilityDiscovery", "어빌리티 발견",
            "오래된 전술서를 발견했습니다. 빛바랜 페이지에서 강력한 전술이 눈에 들어옵니다.",
            "전술서를 읽었습니다. 새로운 능력을 습득할 기회입니다.\n[어빌리티 1택]",
            Reward(EventRewardType.OpenAbilitySelect, intVal: 1, "어빌리티 1택")
        ));

        // ── 07. 고독한 노병 (즉시보상) ───────────────────────
        Add(arr, MakeInstant("LoneVeteran", "고독한 노병",
            "한쪽 다리가 불편한 노병이 막사 밖에 홀로 앉아 있습니다.\n" +
            "\"장군님, 제가 당신 같았을 때의 이야기를 해드릴까요?\"",
            "노병의 이야기를 들었습니다. 발걸음이 한결 가벼워집니다.\n[이동속도 +10% 특성]",
            Reward(EventRewardType.AddTrait, intVal: (int)TraitType.Event_VeteranHeritage, "노병의 유산: 이동속도 +10%")
        ));

        // ── 08. 방치된 창고 (즉시보상) ───────────────────────
        Add(arr, MakeInstant("AbandonedWarehouse", "방치된 창고",
            "길가의 허름한 창고 문이 반쯤 열려 있습니다.\n" +
            "안을 들여다보니 오래된 상자들이 쌓여 있습니다.",
            "창고를 뒤졌습니다. 먼지 쌓인 상자 안에 쓸만한 것들이 있습니다.\n[장비 박스 1개 + 골드 100 획득]",
            Reward(EventRewardType.AddItem, eItem.EquipBox, 1,   "장비 박스 +1"),
            Reward(EventRewardType.AddItem, eItem.Gold,     100, "골드 +100")
        ));

        // ── 09. 전쟁 유물 (즉시보상) ─────────────────────────
        Add(arr, MakeInstant("WarRelic", "전쟁 유물",
            "전장터 구석에서 오래된 전쟁의 흔적을 발견했습니다.\n" +
            "녹슨 갑옷 조각에서 아직도 전투의 기운이 느껴집니다.",
            "유물을 조심스럽게 수습했습니다. 전투에 도움이 될 것 같습니다.\n[전투석 2개 + 환생 포인트 5 획득]",
            Reward(EventRewardType.AddItem, eItem.BattleStone,        2, "전투석 +2"),
            Reward(EventRewardType.AddItem, eItem.ReincarnationPoint,  5, "환생 포인트 +5")
        ));

        // ── 10. 상인의 밀거래 ─────────────────────────────────
        Add(arr, Make("BlackMarket", "상인의 밀거래",
            "수상한 행색의 상인이 슬쩍 다가옵니다.\n" +
            "\"장군님... 공식 루트에선 구하기 어려운 물건이죠.\"",
            Choice("골드로 산다",
                "금화를 건네자 상인이 묵직한 상자를 넘겼습니다.\n[골드 300 소모 / 장비 박스 2개]",
                costHint: "골드 300",
                Reward(EventRewardType.SpendItem, eItem.Gold,     300, "골드 -300"),
                Reward(EventRewardType.AddItem,   eItem.EquipBox,   2, "장비 박스 +2")),
            Choice("전투석으로 산다",
                "희귀한 전투석을 대가로 치렀습니다. 상인이 환하게 웃으며 상자를 건넸습니다.\n[전투석 3개 소모 / 장비 박스 2개]",
                costHint: "전투석 3",
                Reward(EventRewardType.SpendItem, eItem.BattleStone, 3, "전투석 -3"),
                Reward(EventRewardType.AddItem,   eItem.EquipBox,    2, "장비 박스 +2")),
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
        return new EventChoice
        {
            Label          = label,
            ResultText     = successText,  // 성공 텍스트 (UI에서 분기 표시 가능)
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

    static EventReward[] Rewards(params EventReward[] r) => r;

    // ── 배열 등록 ─────────────────────────────────────────────

    static void Add(SerializedProperty arr, EventData data)
    {
        int idx = arr.arraySize;
        arr.arraySize = idx + 1;
        arr.GetArrayElementAtIndex(idx).objectReferenceValue = data;
    }
}
