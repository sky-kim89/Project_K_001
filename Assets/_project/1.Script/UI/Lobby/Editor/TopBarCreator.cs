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
    const string PrefabPath   = "Assets/_project/2.Prefabs/UI/TopBar.prefab";
    const float  BarH         = 180f;    // LobbyPrefabCreator.TopBarH 와 동일하게 유지
    const float  LobbyCanvasW = 1920f;   // 로비 캔버스는 1920×1080 (가로)

    // ── 특성 스트립 (예전 프로필 아이콘 자리) ──────────────────
    //  일반 특성(최대 20) + 직업 시너지(최대 14) 를 한 줄에 이어 붙인다.
    //  GridLayoutGroup 은 활성 자식만 배치하므로 꺼진 슬롯은 자동으로 접히고,
    //  일반 → 시너지 순서로 빈틈없이 이어진다.
    //
    //  ⚠ 칸 수에 맞춰 아이콘을 줄이지 말 것.
    //     특성은 플레이어가 항상 확인해야 하는 정보라 크기가 우선이다.
    //     34칸을 한 줄에 욱여넣으면 42px 까지 내려가는데 그러면 안 보인다.
    //     → 아이콘을 크게 두고, 한 줄 용량(18칸)을 넘으면 둘째 줄로 흐르게 한다.
    //       한 줄:  18 × (80 + 6) - 6 = 1542 ≤ TraitStripW(1564)
    //       두 줄:  80 × 2 + 6 = 166 ≤ BarH(180)  — 상단 바 안에 그대로 들어간다
    //     실제 플레이에서 18개를 넘길 일은 거의 없어 평소엔 한 줄로 보인다.
    const float TraitStripX    = 16f;
    const float TraitStripW    = 1564f;
    const float TraitIconSize  = 80f;
    const float TraitGap       = 6f;
    const int   TraitColCount  = 18;   // 한 줄에 들어가는 칸 수
    const int   TraitSlotMax   = 20;
    const int   SynergySlotMax = 14;

    static readonly Color BarColor         = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color GoldColor        = new Color(1.00f, 0.80f, 0.20f, 1f);
    static readonly Color SettingsBtnColor = new Color(0.18f, 0.18f, 0.26f, 1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem(ProjectKMenu.Lobby + "TopBar", priority = ProjectKMenu.PrefabPrio + 11)]
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

        // 플레이어 프로필(아이콘 + 레벨 배지)은 제거했다.
        // 그 자리에 현재 런의 특성 스트립이 들어간다.
        BuildTraitStrip(bar);

        // 재화는 골드만 남기고 오른쪽 끝(설정 버튼 앞)에 붙인다.
        // 젬·에너지 위젯은 표시하지 않는다.
        //   텍스트 폭 = widgetW - IconSm(64) - 16 = 120 → FontMd 42 로 "22.2k" 수용
        //   (더 길어지면 CurrencyWidget 의 AutoSize 가 줄인다)
        float widgetW  = 200f;
        float widgetH  = UIScale.IconSm + 10f;
        float rightEnd = LobbyCanvasW - 120f;                          // 설정 버튼(우측 96px) 앞
        float goldCx   = rightEnd - widgetW / 2f - LobbyCanvasW / 2f;  // SetRect 는 캔버스 중앙 기준

        BuildCurrencyWidget(bar, "GoldGroup", GoldColor, eItem.Gold, new Vector2(goldCx, 0), widgetW, widgetH);

        // 메뉴 버튼 (우상단) — 일시 정지 팝업(사운드 토글·환생)을 연다
        // ⚙ 글리프는 폰트에 없다 (□ 로 렌더됨) → 3줄 막대 아이콘으로 대체
        var settingsBtn = CreateButton(bar, "SettingsBtn", "", SettingsBtnColor, UIScale.FontMd);
        settingsBtn.AddComponent<LobbyMenuButton>();
        {
            var face = settingsBtn.transform.Find("Body").gameObject;
            for (int i = -1; i <= 1; i++)
                EditorUIBuilder.Bar(face, $"Line{i + 1}", 38f, 5f, 0f,
                                    new Vector2(0f, i * 12f), Color.white);
        }
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

    // ── 특성 스트립 빌드 ──────────────────────────────────────

    static void BuildTraitStrip(GameObject bar)
    {
        // 박스 높이는 두 줄분. 한 줄만 찼을 때는 MiddleLeft 가 세로 가운데로 맞춘다.
        float stripH = TraitIconSize * 2f + TraitGap;

        var strip = new GameObject("TraitBar", typeof(RectTransform));
        strip.transform.SetParent(bar.transform, false);
        {
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0.5f);
            rt.anchorMax        = new Vector2(0f, 0.5f);
            rt.pivot            = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(TraitStripX, 0f);
            rt.sizeDelta        = new Vector2(TraitStripW, stripH);
        }

        var glg = strip.AddComponent<GridLayoutGroup>();
        glg.childAlignment  = TextAnchor.MiddleLeft;
        glg.cellSize        = new Vector2(TraitIconSize, TraitIconSize);
        glg.spacing         = new Vector2(TraitGap, TraitGap);
        glg.padding         = new RectOffset(0, 0, 0, 0);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = TraitColCount;

        var ui = strip.AddComponent<TraitBarUI>();

        // 자식 순서 = 표시 순서. 일반 특성 먼저, 그 뒤에 시너지.
        var traitIcons   = new TraitIconUI[TraitSlotMax];
        for (int i = 0; i < TraitSlotMax; i++)
            traitIcons[i] = TraitIconSlotBuilder.Build(strip, i, TraitIconSize);

        var synergyIcons = new TraitIconUI[SynergySlotMax];
        for (int i = 0; i < SynergySlotMax; i++)
            synergyIcons[i] = TraitIconSlotBuilder.Build(strip, TraitSlotMax + i, TraitIconSize);

        var so = new SerializedObject(ui);
        so.Update();
        SetObjArray(so, "_traitIcons",   traitIcons);
        SetObjArray(so, "_synergyIcons", synergyIcons);
        so.ApplyModifiedProperties();
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

        // 줄바꿈 금지 — 자릿수가 늘어나면 두 줄로 깨지는 대신 폰트를 줄인다.
        valueText.textWrappingMode  = TextWrappingModes.NoWrap;
        valueText.overflowMode      = TextOverflowModes.Overflow;
        valueText.enableAutoSizing  = true;
        valueText.fontSizeMin       = UIScale.FontSm;
        valueText.fontSizeMax       = UIScale.FontMd;

        var widget = group.AddComponent<CurrencyWidget>();
        var wSo    = new SerializedObject(widget);
        wSo.FindProperty("_item").intValue                   = (int)item;
        wSo.FindProperty("_amountText").objectReferenceValue = valueText;
        wSo.FindProperty("_icon").objectReferenceValue       = icon;
        wSo.ApplyModifiedProperties();
    }

    // ── UI 헬퍼 ──────────────────────────────────────────────

    static GameObject CreatePanel(GameObject parent, string name, Color color)
        => EditorUIBuilder.Panel(parent, name, color);

    static Image CreateImage(GameObject parent, string name, Color color)
        => EditorUIBuilder.Img(parent, name, color);

    static TextMeshProUGUI CreateTMP(GameObject parent, string name,
                                     string text, float size, FontStyles style)
        => EditorUIBuilder.TMP(parent, name, text, size, style);

    // UI 규칙: 누를 수 있는 버튼은 음각 처리 (EditorUIBuilder.RaisedTextBtn)
    static GameObject CreateButton(GameObject parent, string name,
                                   string label, Color bgColor, float fontSize)
        => EditorUIBuilder.RaisedTextBtn(parent, name, label, fontSize, bgColor).gameObject;

    static void AnchorTop(GameObject go, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(0, -height);
        rt.offsetMax = new Vector2(0, 0);
    }

    static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        => EditorUIBuilder.Center(rt, pos, size);

    static void SetObjArray(SerializedObject so, string field, TraitIconUI[] items)
        => EditorUIBuilder.SetObjArray(so, field,
            System.Array.ConvertAll(items, i => (Object)i), "TopBarCreator");
}
#endif
