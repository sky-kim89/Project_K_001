using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  BattlePanelCreator.cs
//  Tools > Project K > 로비 UI > Create BattlePanel Prefab
//  또는 LobbyPrefabCreator.CreateLobby() 에서 Build(root) 로 호출된다.
//
//  저장: Assets/_project/2.Prefabs/UI/Lobby/BattlePanel.prefab
//
//  생성 구조:
//    BattlePanel (TopBar~NavBar 사이 채움)
//    ├── TabRow          (상단 TabH+16px)
//    │   ├── NormalTab
//    │   └── EliteTab
//    ├── StageInfo       (TabRow 아래 160px)
//    │   ├── StageNameText
//    │   └── BestRecordText
//    ├── PreviewArea     (중간)
//    │   ├── PreviewBg
//    │   ├── PreviewImage
//    │   ├── PrevBtn
//    │   ├── NextBtn          ← 원본 위치 그대로 (460, 0)
//    │   └── AbilityListBtn   ← NextBtn 바로 아래
//    └── BattleArea      (하단 280px)
//        ├── ProgressText
//        ├── BattleStartBtn
//        └── EnergyCostText
// ============================================================

public static class BattlePanelCreator
{
    const string SavePath = "Assets/_project/2.Prefabs/UI/Lobby/BattlePanel.prefab";

    const float TopBarH = 180f;
    const float NavBarH = 160f;
    const float TabH    = UIScale.BtnSm;

    static readonly Color BarColor         = new Color(0.07f, 0.07f, 0.13f, 1f);
    static readonly Color PanelColor       = new Color(0.09f, 0.09f, 0.16f, 1f);
    static readonly Color TabActiveColor   = new Color(0.20f, 0.70f, 0.90f, 1f);
    static readonly Color TabInactiveColor = new Color(0.22f, 0.22f, 0.28f, 1f);
    static readonly Color BattleBtnColor   = new Color(0.11f, 0.72f, 0.58f, 1f);
    static readonly Color ArrowBtnColor    = new Color(0.25f, 0.25f, 0.35f, 0.70f);
    static readonly Color PreviewBgColor   = new Color(0.04f, 0.04f, 0.09f, 1f);
    static readonly Color AbilityBtnColor  = new Color(0.18f, 0.18f, 0.28f, 0.90f);

    [MenuItem("Tools/Project K/로비 UI/Create BattlePanel Prefab")]
    static void CreateStandalone()
    {
        var canvas = new GameObject("_TempCanvas", typeof(RectTransform));
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(UIScale.RefWidth, UIScale.RefHeight);

        var panel = Build(canvas);
        PrefabUtility.SaveAsPrefabAsset(panel, SavePath);
        Object.DestroyImmediate(canvas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattlePanelCreator] BattlePanel.prefab 생성 완료");
    }

    // LobbyPrefabCreator 에서 호출
    public static GameObject Build(GameObject parent)
    {
        var panel = new GameObject("BattlePanel", typeof(RectTransform));
        panel.transform.SetParent(parent.transform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, NavBarH);
        rt.offsetMax = new Vector2(0, -TopBarH);

        var ui = panel.AddComponent<StageSelectUI>();

        // ── TabRow ────────────────────────────────────────────
        var tabRow = new GameObject("TabRow", typeof(RectTransform));
        tabRow.transform.SetParent(panel.transform, false);
        AnchorTopInside(tabRow, TabH + 16f, 0);

        var normalTab = CreateButton(tabRow, "NormalTab", "일반",   TabActiveColor,   UIScale.FontMd);
        var eliteTab  = CreateButton(tabRow, "EliteTab",  "엘리트", TabInactiveColor, UIScale.FontMd);
        SetRect(normalTab.GetComponent<RectTransform>(), new Vector2(-220, 0), new Vector2(360, TabH));
        SetRect(eliteTab .GetComponent<RectTransform>(), new Vector2( 220, 0), new Vector2(360, TabH));

        // ── StageInfo ─────────────────────────────────────────
        const float infoH = 160f;
        var infoArea = new GameObject("StageInfo", typeof(RectTransform));
        infoArea.transform.SetParent(panel.transform, false);
        AnchorTopInside(infoArea, infoH, TabH + 16f);

        var stageName  = CreateTMP(infoArea, "StageNameText",  "일반 스테이지 1", UIScale.FontLg, FontStyles.Bold);
        var bestRecord = CreateTMP(infoArea, "BestRecordText", "최고 기록  --:--", UIScale.FontSm, FontStyles.Normal);
        SetRect(stageName .GetComponent<RectTransform>(), new Vector2(0,  40), new Vector2(900, 70));
        SetRect(bestRecord.GetComponent<RectTransform>(), new Vector2(0, -40), new Vector2(900, 48));
        bestRecord.color = new Color(0.7f, 0.7f, 0.7f);

        // ── BattleArea ────────────────────────────────────────
        const float battleAreaH = 280f;
        var battleArea = new GameObject("BattleArea", typeof(RectTransform));
        battleArea.transform.SetParent(panel.transform, false);
        AnchorBottomInside(battleArea, battleAreaH, 0);

        var progressText = CreateTMP(battleArea, "ProgressText", "스테이지 1 클리어  0 / 1", UIScale.FontSm, FontStyles.Normal);
        SetRect(progressText.GetComponent<RectTransform>(), new Vector2(0, 110), new Vector2(800, 48));
        progressText.color = new Color(0.65f, 0.65f, 0.65f);

        var battleBtn   = CreateButton(battleArea, "BattleStartBtn", "전투 시작", BattleBtnColor, UIScale.FontLg);
        var battleBtnRt = battleBtn.GetComponent<RectTransform>();
        battleBtnRt.anchorMin        = new Vector2(0.08f, 0f);
        battleBtnRt.anchorMax        = new Vector2(0.92f, 0f);
        battleBtnRt.anchoredPosition = new Vector2(0, UIScale.BtnLg / 2f + 10f);
        battleBtnRt.sizeDelta        = new Vector2(0, UIScale.BtnLg);
        battleBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var energyText = CreateTMP(battleArea, "EnergyCostText", "⚡  5", UIScale.FontSm, FontStyles.Normal);
        SetRect(energyText.GetComponent<RectTransform>(), new Vector2(0, -60), new Vector2(400, 48));
        energyText.color = new Color(0.4f, 0.9f, 1.0f);

        // ── PreviewArea ───────────────────────────────────────
        var previewArea = new GameObject("PreviewArea", typeof(RectTransform));
        previewArea.transform.SetParent(panel.transform, false);
        var previewRt = previewArea.GetComponent<RectTransform>();
        previewRt.anchorMin = Vector2.zero;
        previewRt.anchorMax = Vector2.one;
        previewRt.offsetMin = new Vector2(0, battleAreaH);
        previewRt.offsetMax = new Vector2(0, -(TabH + 16f + infoH));

        var previewBg  = CreateImage(previewArea, "PreviewBg",    PreviewBgColor);
        var previewImg = CreateImage(previewArea, "PreviewImage", new Color(1, 1, 1, 0));
        previewImg.preserveAspect = true;

        var previewBgRt = previewBg.GetComponent<RectTransform>();
        previewBgRt.anchorMin = new Vector2(0.05f, 0.05f);
        previewBgRt.anchorMax = new Vector2(0.95f, 0.95f);
        previewBgRt.offsetMin = previewBgRt.offsetMax = Vector2.zero;

        var previewImgRt = previewImg.GetComponent<RectTransform>();
        previewImgRt.anchorMin = new Vector2(0.1f, 0.1f);
        previewImgRt.anchorMax = new Vector2(0.9f, 0.9f);
        previewImgRt.offsetMin = previewImgRt.offsetMax = Vector2.zero;

        const float arrowSize = 110f;
        var prevBtn = CreateButton(previewArea, "PrevBtn", "<", ArrowBtnColor, UIScale.FontLg);
        var nextBtn = CreateButton(previewArea, "NextBtn", ">", ArrowBtnColor, UIScale.FontLg);
        SetRect(prevBtn.GetComponent<RectTransform>(), new Vector2(-460, 0), new Vector2(arrowSize, arrowSize));
        SetRect(nextBtn.GetComponent<RectTransform>(), new Vector2( 460, 0), new Vector2(arrowSize, arrowSize));
        prevBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        nextBtn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // AbilityListBtn — NextBtn 아래, NextBtn 위치 변경 없음
        // NextBtn 하단: y=0 - arrowSize/2 = -55, 간격 12, 버튼 높이 56 → center y = -55-12-28 = -95
        var abilityBtn = CreateButton(previewArea, "AbilityListBtn", "어빌리티", AbilityBtnColor, UIScale.FontSm);
        SetRect(abilityBtn.GetComponent<RectTransform>(), new Vector2(460, -95), new Vector2(130, 56));

        // ── StageSelectUI 필드 연결 ───────────────────────────
        var so = new SerializedObject(ui);
        so.Update();
        SetObj(so, "_normalTabBtn",   normalTab.GetComponent<Button>());
        SetObj(so, "_eliteTabBtn",    eliteTab .GetComponent<Button>());
        SetObj(so, "_stageNameText",  stageName);
        SetObj(so, "_bestRecordText", bestRecord);
        SetObj(so, "_previewImage",   previewImg);
        SetObj(so, "_prevBtn",        prevBtn.GetComponent<Button>());
        SetObj(so, "_nextBtn",        nextBtn.GetComponent<Button>());
        SetObj(so, "_abilityListBtn", abilityBtn.GetComponent<Button>());
        SetObj(so, "_battleStartBtn", battleBtn.GetComponent<Button>());
        SetObj(so, "_energyCostText", energyText);
        SetObj(so, "_progressText",   progressText);
        so.ApplyModifiedProperties();

        return panel;
    }

    // ── UI 생성 헬퍼 ─────────────────────────────────────────

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
        var lGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lGo.transform.SetParent(go.transform, false);
        var lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = lRt.offsetMax = Vector2.zero;
        var tmp = lGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    // ── RectTransform 헬퍼 ───────────────────────────────────

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
