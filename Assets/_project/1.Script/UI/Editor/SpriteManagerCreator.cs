#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// ============================================================
//  SpriteManagerCreator.cs  [Editor Only]
//  SpriteAtlas 8종 + SpriteManager.asset 자동 생성 도구.
//
//  사용법:
//    Unity 메뉴 → BattleGame → 데이터 생성 → 스프라이트 매니저 생성
//
//  아틀라스 소스 폴더 → spriteatlas 파일:
//    Items/                          → Atlas_Items.spriteatlas
//    Classes/ + Skills/              → Atlas_General.spriteatlas
//    Equipments/                     → Atlas_Equipments.spriteatlas
//    Abilities/                      → Atlas_Abilities.spriteatlas
//    Traits/                         → Atlas_Traits.spriteatlas
//    Relics/                         → Atlas_Relics.spriteatlas
//    StageNodes/                     → Atlas_StageNodes.spriteatlas
//    LobbyBtns/                      → Atlas_LobbyBtns.spriteatlas
// ============================================================

public static class SpriteManagerCreator
{
    const string ResourcesDir = "Assets/Resources";
    const string ManagerPath  = "Assets/Resources/SpriteManager.asset";
    const string IconRoot     = "Assets/_project/3.Textures/Icons";

    const string ItemAtlasPath      = IconRoot + "/Atlas_Items.spriteatlas";
    const string GenAtlasPath       = IconRoot + "/Atlas_General.spriteatlas";
    const string EquipAtlasPath     = IconRoot + "/Atlas_Equipments.spriteatlas";
    const string AbilityAtlasPath   = IconRoot + "/Atlas_Abilities.spriteatlas";
    const string TraitAtlasPath     = IconRoot + "/Atlas_Traits.spriteatlas";
    const string RelicAtlasPath     = IconRoot + "/Atlas_Relics.spriteatlas";
    const string StageNodeAtlasPath = IconRoot + "/Atlas_StageNodes.spriteatlas";
    const string LobbyBtnAtlasPath  = IconRoot + "/Atlas_LobbyBtns.spriteatlas";

    [MenuItem("BattleGame/데이터 생성/스프라이트 매니저 생성")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        // ── 아틀라스 8종 생성 ────────────────────────────────
        var itemAtlas      = CreateAtlas(ItemAtlasPath,      new[] { $"{IconRoot}/Items" });
        var genAtlas       = CreateAtlas(GenAtlasPath,       new[] { $"{IconRoot}/Classes", $"{IconRoot}/Skills" });
        var equipAtlas     = CreateAtlas(EquipAtlasPath,     new[] { $"{IconRoot}/Equipments" });
        var abilityAtlas   = CreateAtlas(AbilityAtlasPath,   new[] { $"{IconRoot}/Abilities" });
        var traitAtlas     = CreateAtlas(TraitAtlasPath,     new[] { $"{IconRoot}/Traits" });
        var relicAtlas     = CreateAtlas(RelicAtlasPath,     new[] { $"{IconRoot}/Relics" });
        var stageNodeAtlas = CreateAtlas(StageNodeAtlasPath, new[] { $"{IconRoot}/StageNodes" });
        var lobbyBtnAtlas  = CreateAtlas(LobbyBtnAtlasPath,  new[] { $"{IconRoot}/LobbyBtns" });

        AssetDatabase.SaveAssets();

        // ── SpriteManager.asset 생성 / 갱신 ─────────────────
        var manager = AssetDatabase.LoadAssetAtPath<SpriteManager>(ManagerPath);
        if (manager == null)
        {
            manager = ScriptableObject.CreateInstance<SpriteManager>();
            AssetDatabase.CreateAsset(manager, ManagerPath);
        }

        var so = new SerializedObject(manager);
        so.Update();
        so.FindProperty("_itemAtlas")      .objectReferenceValue = itemAtlas;
        so.FindProperty("_generalAtlas")   .objectReferenceValue = genAtlas;
        so.FindProperty("_equipmentAtlas") .objectReferenceValue = equipAtlas;
        so.FindProperty("_abilityAtlas")   .objectReferenceValue = abilityAtlas;
        so.FindProperty("_traitAtlas")     .objectReferenceValue = traitAtlas;
        so.FindProperty("_relicAtlas")     .objectReferenceValue = relicAtlas;
        so.FindProperty("_stageNodeAtlas") .objectReferenceValue = stageNodeAtlas;
        so.FindProperty("_lobbyBtnAtlas")  .objectReferenceValue = lobbyBtnAtlas;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SpriteManagerCreator] 완료 — 아틀라스 8종, SpriteManager.asset 갱신.");
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
                Debug.LogWarning($"[SpriteManagerCreator] 폴더 없음(아직 미생성): {sourceFolders[i]}");
            packables[i] = folder;
        }

        SpriteAtlasExtensions.Add(atlas, packables);
        EditorUtility.SetDirty(atlas);
        return atlas;
    }
}
#endif
