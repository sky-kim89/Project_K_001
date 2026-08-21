using System;
using UnityEngine;

// ============================================================
//  RelicDatabase.cs
//  RelicData SO 컬렉션. Resources/RelicDatabase.asset 에 저장.
// ============================================================

[CreateAssetMenu(fileName = "RelicDatabase", menuName = "ProjectK/RelicDatabase")]
public class RelicDatabase : ScriptableObject
{
    static RelicDatabase _instance;
    public static RelicDatabase Current
        => _instance != null ? _instance
                             : _instance = Resources.Load<RelicDatabase>("RelicDatabase");

    [SerializeField] RelicData[] _relics = Array.Empty<RelicData>();

    public RelicData   Get(RelicId id)
        => Array.Find(_relics, r => r != null && r.Id == id);

    public RelicData[] GetAll()
        => _relics;

    public RelicData[] GetByRarity(RelicRarity rarity)
        => Array.FindAll(_relics, r => r != null && r.Rarity == rarity);

    public RelicData[] GetByEffectType(RelicEffectType type)
        => Array.FindAll(_relics, r => r != null && r.EffectType == type);

    /// <summary>
    /// 특정 시스템 효과를 여는 유물. 없으면 null.
    ///
    /// "그 유물을 얻어야 열립니다" 같은 안내를 쓰는 쪽이 유물 이름을 문자열로
    /// 박아 두지 않게 하려고 둔다 — 이름을 고치면 안내만 옛 이름으로 남는다.
    /// </summary>
    public RelicData FindBySystemEffect(RelicSystemEffect effect)
        => Array.Find(_relics, r => r != null
                                 && r.EffectType == RelicEffectType.System
                                 && r.SystemEffect == effect);

    /// <summary>해당 시스템 효과를 여는 유물의 이름. 없으면 빈 문자열.</summary>
    public string NameOfSystemEffect(RelicSystemEffect effect)
        => FindBySystemEffect(effect)?.RelicName ?? "";
}
