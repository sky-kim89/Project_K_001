using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  LobbyScenePatcher.cs
//  Tools > Project K > 로비 UI > Patch Lobby Scene
//
//  씬의 LobbyCanvas 를 기준으로:
//    - 각 패널이 이미 있으면 그대로 사용
//    - 없으면 Assets/_project/2.Prefabs/UI/Lobby/ 에서 프리팹 인스턴스 생성
//    - NavBar 버튼 이름·라벨을 인덱스 순서로 강제 정리
//    - LobbyNavUI 에 버튼·패널 연결
//    - 유물 탭 잔재(RelicPanel 인스턴스·NavBtn_유물) 제거
//
//  탭 순서: [0]상점 [1]영웅 [2]전투 [3]프로필
//
//  ⚠ 유물은 더 이상 탭이 아니다 (RelicPopup)
//    탭은 서로를 끈다 — 유물을 보고 돌아오면 MainPanel 이 다시 OnEnable 을 타
//    고르던 장수가 새로 추첨됐다. PopupManager 가 위에 띄우는 팝업으로 옮겼다.
// ============================================================

public static class LobbyScenePatcher
{
    const string PrefabBase = "Assets/_project/2.Prefabs/UI/Lobby/";

    // MainPanel은 index 4 — NavBar 버튼 없이 코드로만 전환 (RunInProgress 기반)
    static readonly string[] PanelNames = { "ShopPanel", "HeroPanel", "BattlePanel", "ProfilePanel", "MainPanel" };
    static readonly string[] BtnNames   = { "NavBtn_상점", "NavBtn_영웅", "NavBtn_전투", "NavBtn_프로필" };
    static readonly string[] BtnLabels  = { "상점",        "영웅",        "전투",        "프로필" };

    // 팝업으로 옮겨간 옛 탭 — 씬에 남아 있으면 지운다
    static readonly string[] RetiredPanels = { "RelicPanel", "RelicPopup" };
    static readonly string[] RetiredBtns   = { "NavBtn_유물" };

    [MenuItem(ProjectKMenu.Setup + "Lobby 씬 패치", priority = ProjectKMenu.SetupPrio + 2)]
    static void Patch()
    {
        var canvas = GameObject.Find("LobbyCanvas");
        if (canvas == null)
        {
            Debug.LogError("[LobbyScenePatcher] LobbyCanvas 를 찾을 수 없습니다.");
            return;
        }

        // 팝업으로 옮겨간 옛 탭 제거 — 인덱스가 밀리기 전에 먼저 치운다
        RemoveRetired(canvas);

        // 패널 — 씬에 있으면 그대로, 없으면 프리팹 인스턴스
        var panels = new GameObject[PanelNames.Length];
        for (int i = 0; i < PanelNames.Length; i++)
            panels[i] = EnsureChild(canvas, PanelNames[i]);

        // NavBar 버튼 이름·라벨 인덱스 기반 정리
        FixNavBar(canvas);

        // LobbyNavUI 에 버튼·패널 연결
        WireNavUI(canvas, panels);

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("[LobbyScenePatcher] 완료. Ctrl+S 로 씬을 저장하세요.");
    }

    // ── 옛 탭 제거 ────────────────────────────────────────────

    /// <summary>
    /// 팝업으로 옮겨간 화면의 씬 잔재를 지운다.
    ///
    /// ⚠ FixNavBar 보다 먼저 돌아야 한다
    ///   NavBar 버튼은 이름이 아니라 **자식 순서**로 이름을 다시 붙인다.
    ///   유물 버튼을 남겨 둔 채 이름만 밀면 프로필 버튼에 유물 아이콘이 박힌다.
    /// </summary>
    static void RemoveRetired(GameObject canvas)
    {
        foreach (string name in RetiredPanels)
        {
            var t = canvas.transform.Find(name);
            if (t == null) continue;
            Object.DestroyImmediate(t.gameObject);
            Debug.Log($"[LobbyScenePatcher] 옛 탭 제거: {name} (팝업으로 이동)");
        }

        var navBar = canvas.transform.Find("NavBar");
        if (navBar == null) return;

        foreach (string name in RetiredBtns)
        {
            var t = navBar.Find(name);
            if (t == null) continue;
            Object.DestroyImmediate(t.gameObject);
            Debug.Log($"[LobbyScenePatcher] 옛 탭 버튼 제거: {name}");
        }
    }

    // ── 패널 확인/생성 ────────────────────────────────────────

    static GameObject EnsureChild(GameObject canvas, string childName)
    {
        var existing = canvas.transform.Find(childName);
        if (existing != null)
        {
            Debug.Log($"[LobbyScenePatcher] {childName} 기존 인스턴스 사용");
            return existing.gameObject;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabBase}{childName}.prefab");
        if (prefab == null)
        {
            Debug.LogError($"[LobbyScenePatcher] 프리팹 없음: {PrefabBase}{childName}.prefab");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        go.SetActive(false);

        // NavBar 앞에 삽입
        var navBar = canvas.transform.Find("NavBar");
        if (navBar != null)
            go.transform.SetSiblingIndex(navBar.GetSiblingIndex());

        Debug.Log($"[LobbyScenePatcher] {childName} 프리팹 인스턴스 생성");
        return go;
    }

    // ── NavBar 버튼 정리 ──────────────────────────────────────

    static void FixNavBar(GameObject canvas)
    {
        var navBar = canvas.transform.Find("NavBar");
        if (navBar == null) { Debug.LogError("[LobbyScenePatcher] NavBar 없음"); return; }

        for (int i = 0; i < BtnNames.Length && i < navBar.childCount; i++)
        {
            var child = navBar.GetChild(i);
            child.gameObject.name = BtnNames[i];

            var tmp = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = BtnLabels[i];
        }
    }

    // ── LobbyNavUI 연결 ───────────────────────────────────────

    static void WireNavUI(GameObject canvas, GameObject[] panels)
    {
        var navUI  = canvas.GetComponent<LobbyNavUI>() ?? canvas.AddComponent<LobbyNavUI>();
        var navBar = canvas.transform.Find("NavBar");
        var so     = new SerializedObject(navUI);
        so.Update();

        // NavButtons
        var btnsProp = so.FindProperty("_navButtons");
        btnsProp.arraySize = BtnNames.Length;
        for (int i = 0; i < BtnNames.Length; i++)
        {
            var t = navBar?.Find(BtnNames[i]);
            btnsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                t != null ? t.GetComponent<Button>() : null;
        }

        // Panels — 위에서 직접 찾은 레퍼런스 그대로 사용
        var panelsProp = so.FindProperty("_panels");
        panelsProp.arraySize = panels.Length;
        for (int i = 0; i < panels.Length; i++)
            panelsProp.GetArrayElementAtIndex(i).objectReferenceValue = panels[i];

        so.FindProperty("_defaultTab").intValue = 2; // 전투 탭
        so.ApplyModifiedProperties();

        Debug.Log("[LobbyScenePatcher] LobbyNavUI 연결 완료");
    }
}
