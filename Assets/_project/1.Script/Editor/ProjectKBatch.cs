// ============================================================
//  ProjectKBatch.cs  [Editor Only]
//  각 그룹의 "▶ 전체 생성" 메뉴.
//
//  개별 Creator 를 의존 순서대로 한 번에 돌린다.
//  새 Creator 를 추가하면 여기에도 한 줄 넣을 것 — 안 넣으면
//  전체 생성에서 빠지고, 그때부터 다시 메뉴가 어긋나기 시작한다.
// ============================================================
using UnityEditor;
using UnityEngine;

public static class ProjectKBatch
{
    // ══════════════════════════════════════════════════════════
    //  프리팹 전체
    // ══════════════════════════════════════════════════════════
    //  순서 주의
    //    ① HeroPanel — HeroCard/EquipCard 를 만든다.
    //       BattlePanel · Mercenary · RunShop 이 HeroPanelCreator.BuildCardPrefab()
    //       을 호출하므로 카드 레이아웃이 먼저 확정돼야 한다.
    //    ② 인게임 — RewardCard 가 BattleResultPopup 보다 먼저.
    //    ③ 팝업 — BattleResult 가 RewardCard/ExpRow 를 참조.

    [MenuItem(ProjectKMenu.Prefab + "▶ 전체 생성", priority = ProjectKMenu.PrefabPrio)]
    public static void CreateAllPrefabs()
    {
        // ── 로비 ──────────────────────────────────────────────
        HeroPanelCreator.Create();
        TopBarCreator.Create();
        MainPanelCreator.Run();
        BattlePanelCreator.CreateStandalone();
        RelicPanelCreator.CreateStandalone();

        // ── 인게임 ────────────────────────────────────────────
        UISetupTool.MenuCreateGeneralPanel();
        UISetupTool.MenuCreateRewardCard();

        // ── 팝업 ──────────────────────────────────────────────
        PopupPrefabCreator.CreateAll();          // BattleResult · Pause · Loading · Ability×2 · ExpRow
        EquipComparePopupCreator.Create();
        DisassemblePopupCreator.Create();
        HeroDetailPopupCreator.Create();
        RunShopPopupCreator.Create();
        ReincarnationPopupCreator.Create();
        MercenaryPopupCreator.Create();
        EventPopupCreator.Create();

        // ── 이펙트 ────────────────────────────────────────────
        EffectPrefabGenerator.GenerateAll();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectKBatch] ✓ 프리팹 전체 생성 완료");
    }

    // ══════════════════════════════════════════════════════════
    //  데이터 전체 (ScriptableObject · Database)
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Data + "▶ 전체 생성", priority = ProjectKMenu.DataPrio)]
    public static void CreateAllData()
    {
        ActiveSkillCreator.CreateAllActiveSkills();
        GameAssetCreator.CreateAllPassiveSkills();
        TraitCreator.CreateAllTraits();
        AbilityCreator.Create();
        RelicCreator.CreateAll();
        EquipmentCreator.CreateAllEquipments();
        EventDatabaseCreator.CreateAll();
        GameAssetCreator.CreateStageConfig();
        SpriteManagerCreator.Create();          // 아틀라스는 아이콘 생성 뒤에 다시 돌릴 것

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectKBatch] ✓ 데이터 전체 생성 완료");
    }

    // ══════════════════════════════════════════════════════════
    //  아이콘·텍스처 전체
    // ══════════════════════════════════════════════════════════

    [MenuItem(ProjectKMenu.Icon + "▶ 전체 생성", priority = ProjectKMenu.IconPrio)]
    public static void CreateAllIcons()
    {
        IconGenerator.GenerateAllIcons();        // 직업 · 스킬
        IconGenerator.GenerateTraitIcons();
        IconGenerator.GenerateAbilityIcons();
        PassiveIconGenerator.GeneratePassiveIcons();
        RelicIconGenerator.GenerateAll();
        ItemIconGenerator.GenerateAll();
        EquipmentIconGenerator.GenerateEquipmentIcons();
        IconGenerator.GenerateStageNodeIcons();
        IconGenerator.GenerateLobbyButtonIcons();
        EventIllustrationGenerator.Generate();
        EffectTextureGenerator.GenerateAll();

        // 아이콘이 전부 생긴 뒤 아틀라스를 다시 묶는다
        SpriteManagerCreator.Create();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectKBatch] ✓ 아이콘·텍스처 전체 생성 완료");
    }
}
