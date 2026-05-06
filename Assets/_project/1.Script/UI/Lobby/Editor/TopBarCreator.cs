#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  TopBarCreator.cs
//  Tools > Project K > 로비 UI > Create TopBar Prefab
//  크기·폰트는 UIScale 상수를 참조한다.
//
//  CurrencyWidget 연결 시 intValue 사용 (enumValueIndex 는 None=-1 로 인해
//  인덱스가 1씩 밀리므로 사용 금지).
// ============================================================

public static class TopBarCreator
{
    const string PrefabPath = "Assets/_project/2.Prefabs/UI/TopBar.prefab";
    const float  BarH       = 180f;   // LobbyPrefabCreator.TopBarH 와 동일하게 유지

    static readonly Color BarColor         = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color IconBgColor      = new Color(0.22f, 0.22f, 0.32f, 1f);
    static readonly Color GoldColor        = new Color(1.00f, 0.80f, 0.20f, 1f);
    static readonly Color GemColor         = new Color(0.60f, 0.40f, 1.00f, 1f);
    static readonly Color EnergyColor      = new Color(0.30f, 0.90f, 1.00f, 1f);
    static readonly Color SettingsBtnColor = new Color(0.18f, 0.18f, 0.26f, 1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/로비 UI/Create TopBar Prefab")]
    public static void Create()
    {
        var go = Build(null);
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        Debug.Log("[TopBarCreator] TopBar.prefab 생성 완료");
    }

    // ── 공개 빌더 (LobbyPrefabCreator 에서 호출) ─────────────

    public static GameObject Build(GameObject parent)
    {
        var bar = CreatePanel(parent, "TopBar", BarColor);
        AnchorTop(bar, BarH);

        float iconSize = UIScale.IconMd;

        // 플레이어 아이콘 (왼쪽)
        var iconImg = CreateImage(bar, "PlayerIcon", IconBgColor);
        SetRect(iconImg.GetComponent<RectTransform>(), new Vector2(-460, 0), new Vector2(iconSize, iconSize));

        // 레벨 배지
        var lvText = CreateTMP(bar, "LevelText", "Lv.1", UIScale.FontSm, FontStyles.Bold);
        SetRect(lvText.rectTransform, new Vector2(-460, -(iconSize / 2f + 22f)), new Vector2(iconSize + 20f, 36f));

        // 통화 위젯 — 간격은 위젯 너비(180) 기준으로 균등 배치
        float widgetW = 180f;
        float widgetH = UIScale.IconSm + 10f;
        BuildCurrencyWidget(bar, "GoldGroup",   GoldColor,   eItem.Gold,   new Vector2( 80, 0), widgetW, widgetH);
        BuildCurrencyWidget(bar, "GemGroup",    GemColor,    eItem.Gem,    new Vector2(280, 0), widgetW, widgetH);
        BuildCurrencyWidget(bar, "EnergyGroup", EnergyColor, eItem.Energy, new Vector2(460, 0), widgetW, widgetH);

        // 설정 버튼 (우상단)
        var settingsBtn = CreateButton(bar, "SettingsBtn", "⚙", SettingsBtnColor, UIScale.FontMd);
        {
            var rt = settingsBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16, -16);
            rt.sizeDelta        = new Vector2(80, 80);
        }

        return bar;
    }

    // ── CurrencyWidget 빌드 ───────────────────────────────────

    static void BuildCurrencyWidget(GameObject parent, string name,
                                    Color iconColor, eItem item,
                                    Vector2 pos, float w, float h)
    {
        var group = new GameObject(name, typeof(RectTransform));
        group.transform.SetParent(parent.transform, false);
        SetRect(group.GetComponent<RectTransform>(), pos, new Vector2(w, h));

        float iconSz = UIScale.IconSm;
        var icon = CreateImage(group, "Icon", iconColor);
        SetRect(icon.GetComponent<RectTransform>(), new Vector2(-w / 2f + iconSz / 2f + 4f, 0), new Vector2(iconSz, iconSz));

        var valueText = CreateTMP(group, "Value", "0", UIScale.FontMd, FontStyles.Bold);
        SetRect(valueText.rectTransform, new Vector2(iconSz / 2f + 8f, 0), new Vector2(w - iconSz - 16f, h));
        valueText.alignment = TextAlignmentOptions.Left;

        var widget = group.AddComponent<CurrencyWidget>();
        var wSo    = new SerializedObject(widget);
        wSo.FindProperty("_item").intValue                   = (int)item;
        wSo.FindProperty("_amountText").objectReferenceValue = valueText;
        wSo.FindProperty("_icon").objectReferenceValue       = icon;
        wSo.ApplyModifiedProperties();
    }

    // ── UI 헬퍼 ──────────────────────────────────────────────

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) go.transform.SetParent(parent.transform, false);
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

    static TextMeshProUGUI CreateTMP(GameObject parent, string name,
                                     string text, float size, FontStyles style)
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

    static GameObject CreateButton(GameObject parent, string name,
                                   string label, Color bgColor, float fontSize)
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

    static void AnchorTop(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -height);
        rt.offsetMax = new Vector2(0, 0);
    }

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }
}
#endif
