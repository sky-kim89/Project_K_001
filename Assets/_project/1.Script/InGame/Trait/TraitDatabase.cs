using System;
using UnityEngine;

// ============================================================
//  TraitDatabase.cs
//  TraitData SO 컬렉션. Resources/TraitDatabase.asset 에 저장.
// ============================================================

[CreateAssetMenu(fileName = "TraitDatabase", menuName = "ProjectK/TraitDatabase")]
public class TraitDatabase : ScriptableObject
{
    static TraitDatabase _instance;
    public static TraitDatabase Current
        => _instance != null ? _instance
                             : _instance = Resources.Load<TraitDatabase>("TraitDatabase");

    [SerializeField] TraitData[] _traits = Array.Empty<TraitData>();

    public TraitData   Get(TraitType type) => Array.Find(_traits, t => t != null && t.TraitType == type);
    public TraitData[] GetAll()            => _traits;
}
