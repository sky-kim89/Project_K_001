using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  PopupPrefabCreator.cs
//  Tools > Project K > Popup > Create Popup Prefabs
//  BattleResultPopup / PausePopup / LoadingPopup 프리팹 자동 생성.
//  크기·폰트는 UIScale 상수를 참조한다.
// ============================================================

public static class PopupPrefabCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI";

    [MenuItem("Tools/Project K/Popup/Create Popup Prefabs")]
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
        var root  = CreateRoot<BattleResultPopup>("BattleResultPopup", 860, 760);
        var popup = root.GetComponent<BattleResultPopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        var resultText = AddTMP(root, "ResultText", "승리!", UIScale.FontXl, FontStyles.Bold);
        var subText    = AddTMP(root, "SubText", "모든 적을 물리쳤습니다!", UIScale.FontSm, FontStyles.Normal);
        var statsText  = AddTMP(root, "StatsText", "처치  0   |   웨이브  0 / 0", UIScale.FontSm, FontStyles.Normal);
        var confirmBtn = AddButton(root, "ConfirmButton", "확인", new Color(0.20f, 0.55f, 0.20f), UIScale.FontMd);

        SetRect(resultText.rectTransform,                      new Vector2(0,  270), new Vector2(760, 100));
        SetRect(subText.rectTransform,                         new Vector2(0,  165), new Vector2(760,  50));
        SetRect(statsText.rectTransform,                       new Vector2(0,   90), new Vector2(700,  50));
        // 보상 카드 영역은 Inspector에서 _rewardArea 연결 (y: -80 ~ -280 사용)
        SetRect(confirmBtn.GetComponent<RectTransform>(),      new Vector2(0, -310), new Vector2(400, UIScale.BtnMd));

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
        // 버튼 3개 + 여백 기준으로 높이 산출
        float btnH   = UIScale.BtnSm;
        float gap    = 24f;
        float totalH = btnH * 3 + gap * 2;           // 3버튼 높이
        float popupH = totalH + UIScale.FontLg + 120; // 제목 + 상하 여백

        var root  = CreateRoot<PausePopup>("PausePopup", 720, popupH);
        var popup = root.GetComponent<PausePopup>();

        AddBgPanel(root, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        var title = AddTMP(root, "TitleText", "일시 정지", UIScale.FontLg, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0, popupH / 2 - 80), new Vector2(640, 70));

        // 버튼 3개: 중앙 기준 위→아래
        float btnStep = btnH + gap;
        float btn1Y   =  btnStep;
        float btn2Y   =  0;
        float btn3Y   = -btnStep;

        var resumeBtn  = AddButton(root, "ResumeButton",  "계속하기",  new Color(0.20f, 0.55f, 0.20f), UIScale.FontMd);
        var restartBtn = AddButton(root, "RestartButton", "다시 시작", new Color(0.55f, 0.45f, 0.10f), UIScale.FontMd);
        var quitBtn    = AddButton(root, "QuitButton",    "종료",      new Color(0.55f, 0.15f, 0.15f), UIScale.FontMd);

        SetRect(resumeBtn .GetComponent<RectTransform>(), new Vector2(0, btn1Y), new Vector2(560, btnH));
        SetRect(restartBtn.GetComponent<RectTransform>(), new Vector2(0, btn2Y), new Vector2(560, btnH));
        SetRect(quitBtn   .GetComponent<RectTransform>(), new Vector2(0, btn3Y), new Vector2(560, btnH));

        var so = new SerializedObject(popup);
        SetEnum(so, "_popupType",     (int)PopupType.Pause);
        SetObj (so, "_resumeButton",  resumeBtn .GetComponent<Button>());
        SetObj (so, "_restartButton", restartBtn.GetComponent<Button>());
        SetObj (so, "_quitButton",    quitBtn   .GetComponent<Button>());
        so.ApplyModifiedProperties();

        Save(root, "PausePopup");
    }

    // ── LoadingPopup ──────────────────────────────────────────

    static void CreateLoadingPopup()
    {
        var root = new GameObject("LoadingPopup", typeof(RectTransform));
        root.AddComponent<CanvasGroup>();
        var popup = root.AddComponent<LoadingPopup>();

        // 전체 화면 스트레치
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        AddBgPanel(root, new Color(0.05f, 0.05f, 0.08f, 1f));

        var titleText  = AddTMP(root, "TitleText",  "배틀 준비 중",    UIScale.FontLg, FontStyles.Bold);
        var statusText = AddTMP(root, "StatusText", "장군 소환 중...", UIScale.FontMd, FontStyles.Normal);
        statusText.color = new Color(0.75f, 0.75f, 0.75f);

        SetRect(titleText .rectTransform, new Vector2(0,  50), new Vector2(800, 80));
        SetRect(statusText.rectTransform, new Vector2(0, -50), new Vector2(700, 60));

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

    static GameObject AddButton(GameObject parent, string objName, string label, Color bgColor, float fontSize)
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
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
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
