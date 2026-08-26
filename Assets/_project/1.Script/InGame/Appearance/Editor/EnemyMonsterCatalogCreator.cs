#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

public static class EnemyMonsterCatalogCreator
{
    const string SavePath = "Assets/Resources/EnemyMonsterCatalog.asset";
    const string Root = "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Bonus/Monsters/";

    [MenuItem(ProjectKMenu.Data + "몬스터 외형", priority = ProjectKMenu.DataPrio + 19)]
    public static void Create()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<EnemyMonsterCatalog>(SavePath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<EnemyMonsterCatalog>();
            AssetDatabase.CreateAsset(catalog, SavePath);
        }

        catalog.Hog   = Load("Hog");
        catalog.Slug  = Load("Slug");
        catalog.Troll = Load("Troll");
        catalog.Wolf  = Load("Wolf");

        Validate(catalog.Hog);
        Validate(catalog.Slug);
        Validate(catalog.Troll);
        Validate(catalog.Wolf);

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyMonsterCatalogCreator] 저장: {SavePath}");
    }

    static SpriteLibraryAsset Load(string monster)
        => AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>($"{Root}{monster}/SpriteLibrary.asset");

    static void Validate(SpriteLibraryAsset library)
    {
        foreach (string category in new[] { "Idle", "Ready", "Run", "Attack", "Death" })
            Debug.Assert(library.GetSprite(category, "0") != null,
                         $"[EnemyMonsterCatalogCreator] {library.name}: {category}_0 없음");
    }
}
#endif
