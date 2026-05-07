using System;
using UnityEngine;

// ============================================================
//  AbilityDatabase.cs
//  AbilityData SO 컬렉션. Resources/AbilityDatabase.asset 에 저장.
// ============================================================

[CreateAssetMenu(fileName = "AbilityDatabase", menuName = "ProjectK/AbilityDatabase")]
public class AbilityDatabase : ScriptableObject
{
    static AbilityDatabase _instance;
    public static AbilityDatabase Current
        => _instance != null ? _instance
                             : _instance = Resources.Load<AbilityDatabase>("AbilityDatabase");

    [SerializeField] AbilityData[] _abilities = Array.Empty<AbilityData>();

    public AbilityData   Get(AbilityId id)
        => Array.Find(_abilities, a => a != null && a.Id == id);

    public AbilityData[] GetAll()
        => _abilities;

    public AbilityData[] GetByGrade(AbilityGrade grade)
        => Array.FindAll(_abilities, a => a != null && a.Grade == grade);
}
