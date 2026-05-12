using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  LobbyPrefabCreator.cs
//  Tools > Project K > 로비 UI > Create Lobby Prefab
//  로비 Canvas 프리팹을 Assets/_project/2.Prefabs/UI/ 에 생성한다.
//  크기·폰트는 UIScale 상수를 참조한다.
//
//  생성 구조:
//    LobbyCanvas (Canvas, CanvasScaler UIScale.RefW×RefH)
//    ├── Background
//    ├── TopBar          (TopBarCreator.Build 호출)
//    ├── NavBar
//    └── StageSelectPanel
// ============================================================

public static class LobbyPrefabCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI";

    // ── 레이아웃 상수 (UIScale 외 로비 전용) ─────────────────
    const float TopBarH = 180f;
    const float NavBarH = 160f;
    const float TabH    = UIScale.BtnSm;

    // ── 색상 팔레트 ───────────────────────────────────────────
    static readonly Color BgColor          = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color BarColor         = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color PanelColor       = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color TabActiveColor   = new Color(0.20f, 0.70f, 0.90f, 1f);
    static readonly Color TabInactiveColor = new Color(0.22f, 0.22f, 0.28f, 1f);
    static readonly Color BattleBtnColor   = new Color(0.11f, 0.72f, 0.58f, 1f);
    static readonly Color ArrowBtnColor    = new Color(0.25f, 0.25f, 0.35f, 0.70f);
    static readonly Color PreviewBgColor   = new Color(0.04f, 0.04f, 0.09f, 1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/로비 UI/Create Lobby Prefab")]
    static void CreateLobby()
    {
        var root = CreateCanvas("LobbyCanvas");

        CreatePanel(root, "Background", BgColor);
        Stretch(root.transform.Find("Background").gameObject, 0, 0, 0, 0);

        TopBarCreator.Build(root);
        CreateNavBar(root);
        BattlePanelCreator.Build(root);

        PrefabUtility.SaveAsPrefabAsset(root, $"{SavePath}/LobbyCanvas.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LobbyPrefabCreator] LobbyCanvas.prefab 생성 완료");
    }

    // ── NavBar ────────────────────────────────────────────────

    static GameObject CreateNavBar(GameObject parent)
    {
        var bar = CreatePanel(parent, "NavBar", BarColor);
        AnchorBottom(bar, NavBarH);

        string[] icons = { "홈", "영웅", "전투", "상점", "프로필" };
        float step   = UIScale.RefWidth / icons.Length;
        float startX = -UIScale.RefWidth / 2f + step / 2f;

        for (int i = 0; i < icons.Length; i++)
        {
            var btn = CreateButton(bar, $"NavBtn_{icons[i]}", icons[i], PanelColor, UIScale.FontSm);
            SetRect(btn.GetComponent<RectTransform>(),
                new Vector2(startX + step * i, 0), new Vector2(step - 16f, NavBarH - 20f));

            if (i == 2) btn.GetComponent<Image>().color = TabActiveColor;
        }
        return bar;
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

    static GameObject CreateCanvas(string name)
    {
        var go     = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(UIScale.RefWidth, UIScale.RefHeight);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = UIScale.Match;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Image CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CreateTMP(GameObject parent, string name, string text, float size, FontStyles style)
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

    static GameObject CreateButton(GameObject parent, string name, string label, Color bgColor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var lRt = labelGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    // ── RectTransform 헬퍼 ───────────────────────────────────

    static void Stretch(GameObject go, float left, float bottom, float right, float top)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left,   bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    static void AnchorBottom(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = new Vector2(0, 0);
        rt.offsetMax = new Vector2(0, height);
    }

    static void AnchorTopInside(GameObject go, float height, float offsetFromTop)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -(offsetFromTop + height));
        rt.offsetMax = new Vector2(0, -offsetFromTop);
    }

    static void AnchorBottomInside(GameObject go, float height, float offsetFromBottom)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.offsetMin = new Vector2(0, offsetFromBottom);
        rt.offsetMax = new Vector2(0, offsetFromBottom + height);
    }

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
    }
}
