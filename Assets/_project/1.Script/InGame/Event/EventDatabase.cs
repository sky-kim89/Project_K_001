using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================
//  EventDatabase.cs
//  모든 EventData SO 를 등록하는 데이터베이스.
//
//  사용법:
//    var db  = EventDatabase.Current;
//    var evt = db.GetRandom();                   // 완전 랜덤
//    var evt = db.Get("InjuredSoldier");          // ID 지정
//    var evt = db.GetRandomByTag(EventTag.Combat); // 태그 필터
//
//  에셋 경로: Assets/Resources/EventDatabase.asset
//  에디터 생성: Tools > Project K > Event > Create Event Database
// ============================================================

[CreateAssetMenu(menuName = "BattleGame/Event/Event Database", fileName = "EventDatabase")]
public class EventDatabase : ScriptableObject
{
    /// <summary>
    /// 상점 스테이지 전용 이벤트 ID.
    /// 상점은 팝업이 바로 뜨지 않고 이 이벤트("행상인의 좌판")를 거쳐 열린다.
    /// 랜덤 이벤트 풀에는 나오면 안 되므로 GetRandom() 이 제외한다.
    /// </summary>
    public const string ShopEventId = "TravelingMerchant";

    [SerializeField] EventData[] _events;

    static EventDatabase _current;
    public static EventDatabase Current
    {
        get
        {
            if (_current == null)
                _current = Resources.Load<EventDatabase>("EventDatabase");
            return _current;
        }
    }

    // ── 조회 ─────────────────────────────────────────────────

    public EventData Get(string id)
        => System.Array.Find(_events, e => e != null && e.EventId == id);

    public EventData[] GetAll()
        => _events != null ? _events.Where(e => e != null).ToArray() : System.Array.Empty<EventData>();

    /// <summary>이벤트 스테이지용 랜덤 추첨. 상점 전용 이벤트는 뽑히지 않는다.</summary>
    public EventData GetRandom() => GetRandomExcluding(ShopEventId);

    public EventData GetRandomExcluding(string excludeId)
    {
        var valid = GetAll().Where(e => e.EventId != excludeId).ToArray();
        return valid.Length == 0 ? null : valid[Random.Range(0, valid.Length)];
    }

    /// <summary>
    /// 이번 런에 아직 안 나온 이벤트 중에서 추첨한다.
    /// 전부 소진되면 다시 전체 풀에서 뽑는다 — 이벤트 칸이 빈손으로 지나가면 안 된다.
    /// </summary>
    public EventData GetRandomUnseen(IEnumerable<string> seenIds)
    {
        var pool   = GetAll().Where(e => e.EventId != ShopEventId).ToArray();
        var unseen = seenIds == null
            ? pool
            : pool.Where(e => !seenIds.Contains(e.EventId)).ToArray();

        var pick = unseen.Length > 0 ? unseen : pool;
        return pick.Length == 0 ? null : pick[Random.Range(0, pick.Length)];
    }
}
