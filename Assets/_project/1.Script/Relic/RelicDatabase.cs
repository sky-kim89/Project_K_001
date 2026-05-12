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
}
