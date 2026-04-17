using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  PopupPrefabCreator.cs
//  Tools > Project K > Create Popup Prefabs
//  BattleResultPopup / PausePopup / LoadingPopup 프리팹을
//  Assets/_project/2.Prefabs/UI/ 에 자동 생성한다.
// ============================================================

public static class PopupPrefabCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI";

    [MenuItem("Tools/Project K/Create Popup Prefabs")]
    static void CreateAll()
    {
        CreateBattleResultPopup();
        CreatePausePopup();
        CreateLoadingPopup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PopupPrefabCreator] 팝업 프리팹 3종 생성 완료");
    }

    // ── BattleResultPopup ─────────────────────────────────────

    static void CreateBattleResultPopup()
    {
        var root  = CreateRoot<BattleResultPopup>("BattleResultPopup", 620, 460);
        var popup = root.GetComponent<BattleResultPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));
        var resultText  = AddTMP(root, "ResultText", "승리!", 52, FontStyles.Bold);
        var subText     = AddTMP(root, "SubText", "모든 적을 물리쳤습니다!", 22, FontStyles.Normal);
        var statsText   = AddTMP(root, "StatsText", "처치  0\n웨이브  0 / 0", 18, FontStyles.Normal);
        var confirmBtn  = AddButton(root, "ConfirmButton", "확인", new Color(0.20f, 0.55f, 0.20f));

        SetRect(resultText.rectTransform, new Vector2(0,  130), new Vector2(560,  70));
        SetRect(subText.rectTransform,    new Vector2(0,   50), new Vector2(560,  40));
        SetRect(statsText.rectTransform,  new Vector2(0,  -30), new Vector2(400,  70));
        SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0, -165), new Vector2(200, 50));

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",     (int)PopupType.BattleResult);
        SetObj (so, "_resultText",    resultText);
        SetObj (so, "_subText",       subText);
        SetObj (so, "_statsText",     statsText);
        SetObj (so, "_confirmButton", confirmBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "BattleResultPopup");
    }

    // ── PausePopup ────────────────────────────────────────────

    static void CreatePausePopup()
    {
        var root  = CreateRoot<PausePopup>("PausePopup", 460, 400);
        var popup = root.GetComponent<PausePopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));
        var title      = AddTMP(root, "TitleText", "일시 정지", 32, FontStyles.Bold);
        var resumeBtn  = AddButton(root, "ResumeButton",  "계속하기",  new Color(0.20f, 0.55f, 0.20f));
        var restartBtn = AddButton(root, "RestartButton", "다시 시작", new Color(0.55f, 0.45f, 0.10f));
        var quitBtn    = AddButton(root, "QuitButton",    "종료",      new Color(0.55f, 0.15f, 0.15f));

        SetRect(title.rectTransform,                      new Vector2(0,  130), new Vector2(400, 50));
        SetRect(resumeBtn.GetComponent<RectTransform>(),  new Vector2(0,   45), new Vector2(320, 50));
        SetRect(restartBtn.GetComponent<RectTransform>(), new Vector2(0,  -20), new Vector2(320, 50));
        SetRect(quitBtn.GetComponent<RectTransform>(),    new Vector2(0,  -85), new Vector2(320, 50));

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",     (int)PopupType.Pause);
        SetObj (so, "_resumeButton",  resumeBtn.GetComponent<Button>());
        SetObj (so, "_restartButton", restartBtn.GetComponent<Button>());
        SetObj (so, "_quitButton",    quitBtn.GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "PausePopup");
    }

    // ── LoadingPopup ──────────────────────────────────────────

    static void CreateLoadingPopup()
    {
        var root  = new GameObject("LoadingPopup", typeof(RectTransform));
        root.AddComponent<CanvasGroup>();
        var popup = root.AddComponent<LoadingPopup>();

        // 전체 화면 스트레치
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        AddBgPanel(root, new Color(0.05f, 0.05f, 0.08f, 1f));
        var titleText  = AddTMP(root, "TitleText",  "배틀 준비 중",    36, FontStyles.Bold);
        var statusText = AddTMP(root, "StatusText", "장군 소환 중...", 22, FontStyles.Normal);

        SetRect(titleText.rectTransform,  new Vector2(0,  30), new Vector2(600, 60));
        SetRect(statusText.rectTransform, new Vector2(0, -30), new Vector2(500, 40));

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",   (int)PopupType.Loading);
        SetObj (so, "_titleText",   titleText);
        SetObj (so, "_statusText",  statusText);
        so.ApplyModifiedProperties();

        Save(root, "LoadingPopup");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    static GameObject CreateRoot<T>(string name, float w, float h) where T : PopupBase
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.AddComponent<CanvasGroup>();
        go.AddComponent<T>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(w, h);

        return go;
    }

    static void AddBgPanel(GameObject parent, Color color)
    {
        var go = new GameObject("BgPanel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.transform.SetAsFirstSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = color;
    }

    static TextMeshProUGUI AddTMP(GameObject parent, string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static GameObject AddButton(GameObject parent, string objName, string label, Color bgColor)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static void SetEnum(SerializedObject so, string field, int value)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.intValue = value;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
    }

    static void Save(GameObject root, string fileName)
    {
        string path = $"{SavePath}/{fileName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[PopupPrefabCreator] 저장: {path}");
    }
}
