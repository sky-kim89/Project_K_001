#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// ============================================================
//  SpriteManagerCreator.cs  [Editor Only]
//  SpriteAtlas 2종 + SpriteManager.asset 자동 생성 도구.
//
//  사용법:
//    Unity 메뉴 → BattleGame → 데이터 생성 → 스프라이트 매니저 생성
//
//  생성 위치:
//    Assets/Resources/SpriteManager.asset
//    Assets/_project/3.Textures/Icons/Atlas_Items.spriteatlas
//    Assets/_project/3.Textures/Icons/Atlas_General.spriteatlas
//
//  아틀라스 소스 폴더:
//    Atlas_Items   ← Assets/_project/3.Textures/Icons/Items/
//    Atlas_General ← Assets/_project/3.Textures/Icons/Classes/
//                    Assets/_project/3.Textures/Icons/Skills/
// ============================================================

public static class SpriteManagerCreator
{
    const string ResourcesDir   = "Assets/Resources";
    const string ManagerPath    = "Assets/Resources/SpriteManager.asset";
    const string IconRoot       = "Assets/_project/3.Textures/Icons";
    const string ItemAtlasPath  = "Assets/_project/3.Textures/Icons/Atlas_Items.spriteatlas";
    const string GenAtlasPath   = "Assets/_project/3.Textures/Icons/Atlas_General.spriteatlas";

    [MenuItem("BattleGame/데이터 생성/스프라이트 매니저 생성")]
    public static void Create()
    {
        // ── Resources 폴더 ──────────────────────────────────
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // ── 아틀라스 생성 ────────────────────────────────────
        var itemAtlas = CreateAtlas(ItemAtlasPath, new[]
        {
            $"{IconRoot}/Items",
        });

        var genAtlas = CreateAtlas(GenAtlasPath, new[]
        {
            $"{IconRoot}/Classes",
            $"{IconRoot}/Skills",
        });

        AssetDatabase.SaveAssets();

        // ── SpriteManager.asset 생성 / 갱신 ─────────────────
        var manager = AssetDatabase.LoadAssetAtPath<SpriteManager>(ManagerPath);
        if (manager == null)
        {
            manager = ScriptableObject.CreateInstance<SpriteManager>();
            AssetDatabase.CreateAsset(manager, ManagerPath);
        }

        var so = new SerializedObject(manager);
        so.FindProperty("_itemAtlas").objectReferenceValue    = itemAtlas;
        so.FindProperty("_generalAtlas").objectReferenceValue = genAtlas;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SpriteManagerCreator] 완료\n  Items  atlas: {ItemAtlasPath}\n  General atlas: {GenAtlasPath}\n  Manager: {ManagerPath}");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    static SpriteAtlas CreateAtlas(string atlasPath, string[] sourceFolders)
    {
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        // 기존 packable 초기화 후 폴더 재등록
        SpriteAtlasExtensions.Remove(atlas, SpriteAtlasExtensions.GetPackables(atlas));

        var packables = new Object[sourceFolders.Length];
        for (int i = 0; i < sourceFolders.Length; i++)
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(sourceFolders[i]);
            if (folder == null)
                Debug.LogWarning($"[SpriteManagerCreator] 폴더 없음: {sourceFolders[i]}");
            packables[i] = folder;
        }

        SpriteAtlasExtensions.Add(atlas, packables);
        EditorUtility.SetDirty(atlas);
        return atlas;
    }
}
#endif
