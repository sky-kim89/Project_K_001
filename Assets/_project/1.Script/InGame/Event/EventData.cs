using System;
using UnityEngine;

// ============================================================
//  EventData.cs
//  이벤트 ScriptableObject — 제목·본문·선택지·보상 정의.
//
//  이벤트 유형:
//    A. 선택지형 : Choices 배열에 2~4개 항목 → 플레이어가 선택
//    B. 즉시보상형: Choices 비움 → InstantRewards 즉시 지급
//
//  보상 적용: EventRewardHandler.Apply()
//  팝업 열기: PopupManager.Instance.Open<EventPopup>(PopupType.Event).Setup(data)
// ============================================================

// ── 보상 종류 ────────────────────────────────────────────────

public enum EventRewardType
{
    None              = 0,
    AddTrait          = 1,   // IntValue = (int)TraitType
    AddItem           = 2,   // Item = eItem 종류, IntValue = 수량 (양수)
    SpendItem         = 3,   // Item = eItem 종류, IntValue = 소모량 (양수)
    AddSoldier        = 4,   // IntValue = 병사 수 (런 영구 +)
    RemoveSoldier     = 5,   // IntValue = 병사 수 (런 영구 -)
    HealHpPercent     = 6,   // FloatValue = 비율 (0.10 = 10%)
    NextStageElite    = 7,   // 다음 스테이지를 엘리트로 강제 변경
    OpenAbilitySelect = 8,   // IntValue = 어빌리티 선택 팝업 연속 횟수
    RandomTraitBuff   = 9,   // 버프 특성 풀에서 랜덤 1개
    RandomTraitDebuff = 10,  // 디버프 특성 풀에서 랜덤 1개
    OpenRunShop       = 11,  // 런 상점 팝업을 이어서 연다 (행상인 이벤트)
    OpenMercenary     = 12,  // 용병 고용 팝업을 이어서 연다 (무료 — 비용은 SpendItem 으로 따로 건다)
}

// ── 보상 단위 ────────────────────────────────────────────────

[Serializable]
public class EventReward
{
    public EventRewardType Type;
    [Tooltip("AddItem / SpendItem 일 때 아이템 종류")]
    public eItem           Item      = eItem.None;
    public int             IntValue;
    public float           FloatValue;
    [Tooltip("UI 결과 텍스트에 표시할 설명 (예: '골드 +200'). 비어도 됨.\n" +
             "ScaleByStageReward 를 켜면 비워 둘 것 — 수량이 매번 달라져 고정 문구가 거짓말이 된다.")]
    public string          Description;

    [Tooltip("IntValue 를 '해당 스테이지 클리어 보상 골드 대비 %' 로 해석한다.\n" +
             "예) IntValue=40 → 그 스테이지 보상의 40%\n\n" +
             "⚠ 스테이지 번호를 곱하지 않는다\n" +
             "  클리어 보상은 500 → 1,950 으로 3.9배밖에 안 는다.\n" +
             "  번호를 곱하면 30배가 되어 후반 비용이 수입을 짓눌러 버린다.")]
    public bool            ScaleByStageReward;
}

// ── 선택지 ───────────────────────────────────────────────────

[Serializable]
public class EventChoice
{
    [Tooltip("버튼에 표시되는 선택지 텍스트")]
    public string Label;

    [Tooltip("선택 후 본문에 표시될 결과 텍스트")]
    [TextArea(2, 6)]
    public string ResultText;

    [Range(0f, 1f)]
    [Tooltip("1.0 = 항상 성공 분기. 미만이면 난수로 SuccessRewards / FailRewards 분기")]
    public float SuccessRate = 1f;

    [Tooltip("성공(또는 항상) 시 지급 보상")]
    public EventReward[] SuccessRewards;

    [Tooltip("SuccessRate < 1 일 때 실패 시 지급 보상")]
    public EventReward[] FailRewards;

    [Tooltip("'필요: 골드 200' 등 비용/조건 힌트 — 버튼 우측 소자로 표시")]
    public string CostHint;
}

// ── EventData ScriptableObject ───────────────────────────────

[CreateAssetMenu(menuName = "BattleGame/Event/Event Data", fileName = "Event_")]
public class EventData : ScriptableObject
{
    [Tooltip("고유 식별자 (EventDatabase.Get() 검색용)")]
    public string EventId;

    [Tooltip("팝업 타이틀")]
    public string Title;

    [Tooltip("이벤트 본문 (대화체 서술)")]
    [TextArea(4, 10)]
    public string Body;

    [Tooltip("선택 사항 — 팝업 상단 삽화")]
    public Sprite Illustration;

    [Tooltip("선택지형 이벤트. 비어 있으면 즉시 보상 이벤트로 처리")]
    public EventChoice[] Choices;

    [Tooltip("즉시 보상 이벤트 전용 보상 목록")]
    public EventReward[] InstantRewards;

    [Tooltip("즉시 보상 이벤트에서 '확인' 전에 표시될 결과 텍스트")]
    [TextArea(2, 5)]
    public string InstantResultText;
}
