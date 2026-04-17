using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ============================================================
//  SplashSetupTool.cs
//  Tools > Project K > Setup Splash Scene
//
//  현재 열린 씬에 Splash 에 필요한 모든 오브젝트/컴포넌트를
//  하나의 루트(SplashRoot) 아래 생성하고 SplashBootstrap 필드를
//  자동으로 연결한다.
//
//  생성 구조:
//    SplashRoot  (Canvas / CanvasScaler / GraphicRaycaster /
//                 CanvasGroup / SplashBootstrap)
//    ├── Background        — 전체 화면 배경
//    ├── LogoArea          — 상단 로고 + 타이틀 텍스트
//    │   ├── LogoImage
//    │   └── TitleText
//    ├── BottomGroup       — 하단 프로그레스 영역
//    │   ├── ProgressBarBg
//    │   │   └── ProgressBarFill   ← SplashBootstrap._progressBarFill
//    │   ├── StatusText            ← SplashBootstrap._statusText
//    │   └── VersionText
//    EventSystem           — 씬에 없으면 자동 생성
// ============================================================

public static class SplashSetupTool
{
    // ── 색상 팔레트 ───────────────────────────────────────────
    static readonly Color BgColor          = new Color(0.035f, 0.035f, 0.063f, 1f);
    static readonly Color LogoPlaceholder  = new Color(0.15f,  0.15f,  0.25f,  1f);
    static readonly Color BarBgColor       = new Color(0.12f,  0.12f,  0.20f,  1f);
    static readonly Color BarFillColor     = new Color(0.20f,  0.70f,  0.90f,  1f);
    static readonly Color StatusColor      = new Color(0.70f,  0.70f,  0.80f,  1f);
    static readonly Color VersionColor     = new Color(0.35f,  0.35f,  0.45f,  1f);

    // ── 진입점 ────────────────────────────────────────────────

    [MenuItem("Tools/Project K/Setup Splash Scene")]
    static void Setup()
    {
        // 중복 생성 방지
        if (GameObject.Find("SplashRoot") != null)
        {
            EditorUtility.DisplayDialog("Setup Splash Scene",
                "SplashRoot 가 이미 씬에 존재합니다.\n기존 오브젝트를 삭제한 뒤 다시 실행하세요.", "확인");
            return;
        }

        var root = BuildRoot();
        EnsureEventSystem();

        Undo.RegisterCreatedObjectUndo(root, "Setup Splash Scene");
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log("[SplashSetupTool] SplashRoot 생성 완료 — Inspector 에서 씬 이름을 확인하세요.");
    }

    // ── 루트 생성 ─────────────────────────────────────────────

    static GameObject BuildRoot()
    {
        // ── SplashRoot ─────────────────────────────────────
        var root = new GameObject("SplashRoot");

        // Canvas
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        // CanvasScaler
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // CanvasGroup — SplashBootstrap._splashCanvas 에 연결됨
        var splashCg = root.AddComponent<CanvasGroup>();

        // SplashBootstrap
        var bootstrap = root.AddComponent<SplashBootstrap>();

        // ── Background ─────────────────────────────────────
        var bg = CreatePanel(root, "Background", BgColor);
        Stretch(bg);

        // ── LogoArea ───────────────────────────────────────
        var logoArea = CreateEmpty(root, "LogoArea");
        {
            var rt = logoArea.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.offsetMin        = new Vector2(0,  -900f);
            rt.offsetMax        = new Vector2(0,  -160f);
        }

        // 로고 이미지 (플레이스홀더 — 실제 Sprite 교체)
        var logoImg = CreateImage(logoArea, "LogoImage", LogoPlaceholder);
        {
            var rt = logoImg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.35f);
            rt.anchorMax = new Vector2(0.75f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            logoImg.preserveAspect = true;
        }

        // 타이틀 텍스트
        var titleTmp = CreateTMP(logoArea, "TitleText", "PROJECT K", 64, FontStyles.Bold);
        {
            var rt = titleTmp.rectTransform;
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0.30f);
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            titleTmp.color      = Color.white;
            titleTmp.characterSpacing = 8f;
        }

        // ── BottomGroup ────────────────────────────────────
        var bottomGroup = CreateEmpty(root, "BottomGroup");
        {
            var rt = bottomGroup.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.offsetMin = new Vector2(0,  160f);
            rt.offsetMax = new Vector2(0,  560f);
        }

        // ProgressBarBg
        var barBg = CreatePanel(bottomGroup, "ProgressBarBg", BarBgColor);
        {
            var rt = barBg.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta        = new Vector2(860f, 14f);

            // 둥근 모서리처럼 보이게 — 기본 Image, Sprite 교체 시 SlicedSprite 권장
        }

        // ProgressBarFill — SplashBootstrap._progressBarFill 에 연결됨
        var fillImg = CreateImage(barBg, "ProgressBarFill", BarFillColor);
        {
            var img = fillImg;
            img.type      = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 0f;

            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // StatusText — SplashBootstrap._statusText 에 연결됨
        var statusTmp = CreateTMP(bottomGroup, "StatusText", "초기화 중...", 22, FontStyles.Normal);
        {
            var rt = statusTmp.rectTransform;
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -70f);
            rt.sizeDelta        = new Vector2(860f, 40f);
            statusTmp.color     = StatusColor;
        }

        // VersionText (장식용, 왼쪽 하단)
        var verTmp = CreateTMP(bottomGroup, "VersionText", "v0.1.0", 20, FontStyles.Normal);
        {
            var rt = verTmp.rectTransform;
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(60f, 0f);
            rt.sizeDelta        = new Vector2(200f, 36f);
            verTmp.alignment    = TextAlignmentOptions.Left;
            verTmp.color        = VersionColor;
        }

        // ── SplashBootstrap 직렬화 필드 연결 ──────────────
        var so = new SerializedObject(bootstrap);
        SetObj(so, "_progressBarFill", fillImg);
        SetObj(so, "_statusText",      statusTmp);
        SetObj(so, "_splashCanvas",    splashCg);
        so.ApplyModifiedProperties();

        return root;
    }

    // ── EventSystem ───────────────────────────────────────────

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(es, "Setup Splash Scene - EventSystem");
        Debug.Log("[SplashSetupTool] EventSystem 생성 완료");
    }

    // ── 생성 헬퍼 ─────────────────────────────────────────────

    static GameObject CreateEmpty(GameObject parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
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

    // ── RectTransform 헬퍼 ───────────────────────────────────

    /// <summary>부모에 전체 스트레치</summary>
    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetObj(SerializedObject so, string field, Object obj)
    {
        var prop = so.FindProperty(field);
        if (prop != null) prop.objectReferenceValue = obj;
        else Debug.LogWarning($"[SplashSetupTool] 필드를 찾을 수 없음: {field}");
    }
}
