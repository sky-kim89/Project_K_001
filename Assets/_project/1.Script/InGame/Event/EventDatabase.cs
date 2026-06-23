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

    public EventData GetRandom()
    {
        var valid = GetAll();
        return valid.Length == 0 ? null : valid[Random.Range(0, valid.Length)];
    }

    public EventData GetRandomExcluding(string excludeId)
    {
        var valid = GetAll().Where(e => e.EventId != excludeId).ToArray();
        return valid.Length == 0 ? GetRandom() : valid[Random.Range(0, valid.Length)];
    }
}
