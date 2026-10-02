using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  PixelSkin.cs  [Editor Only]
//  Creator 가 만든 UI 계층을 저장 직전에 PixelTheme 스프라이트로 갈아 입힌다.
//
//  사용: PrefabUtility.SaveAsPrefabAsset 바로 앞에서  PixelSkin.Apply(root);
//
//  ■ 왜 Creator 마다 고치지 않고 '패스' 로 하나
//    팝업 Creator 13개 + 로비·인게임이 각자 창·헤더·버튼을 그린다. 하나씩 다시 짜면
//    화면마다 스킨이 조금씩 갈라진다. 다행히 만드는 규칙은 통일돼 있다 —
//      · 버튼   : EditorUIBuilder.RaisedBtn 구조  Root(Button) → Shadow + Body(TopEdge·BottomEdge)
//      · 팝업 창 : 루트 바로 아래 "Panel" (+ 앞 형제 "Border")
//      · 헤더   : Panel 아래 "Header" / "HeaderBar"
//    이 세 가지만 알아보고 바꾼다. 나머지(칸·카드·목록)는 각 화면 색을 그대로 둔다.
//
//  ■ MainPanel 은 거치지 않는다 — 처음부터 PixelTheme 으로 짠 화면이다.
//
//  ⚠ 런타임이 버튼 Body 색을 바꾸는 화면이 있다 (탭 켜짐/꺼짐 등)
//    그 색은 스프라이트에 곱해진다 — 어두운 색을 넣으면 판이 어두워질 뿐 깨지지는 않는다.
// ============================================================
public static class PixelSkin
{
    const string BlueBtn   = "ui_button_blue_9slice";
    const string GoldBtn   = "ui_button_gold_9slice";
    const string TealBtn   = "ui_button_teal_9slice";
    const string SquareBtn = "ui_button_square_9slice";
    const string Frame     = "ui_panel_frame_9slice";

    const float FrameScale = 0.7f;
    const float FrameInset = 12f;   // 프레임 금테(≈11) 안쪽 — 헤더가 금테를 덮지 않게

    static readonly Color RedTint  = new Color(1.00f, 0.45f, 0.40f, 1f);   // 금 판 × 붉은 색조 = 적동
    static readonly Color DimTint  = new Color(0.66f, 0.70f, 0.86f, 1f);   // 파란 판을 한 톤 죽인다
    static readonly Color GrayTint = new Color(0.72f, 0.72f, 0.78f, 1f);

    public static void Apply(GameObject root)
    {
        // 순회 중에 Shadow·Edge 를 지우므로 목록을 먼저 떠 둔다
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (t == null) continue;   // 앞에서 지운 Shadow·Edge
            if (t.TryGetComponent(out Button btn)) SkinButton(btn);
        }
        foreach (var t in all)
        {
            if (t == null || t.parent != root.transform || t.name != "Panel") continue;
            SkinWindow(t);
        }
    }

    // ── 버튼 ─────────────────────────────────────────────────

    static void SkinButton(Button btn)
    {
        var shadow = btn.transform.Find("Shadow");
        var bodyT  = btn.transform.Find("Body");
        if (shadow == null || bodyT == null) return;          // RaisedBtn 구조가 아니다
        if (bodyT.Find("TopEdge") == null) return;

        var body = bodyT.GetComponent<Image>();
        Color face = body.color;

        Object.DestroyImmediate(shadow.gameObject);
        Object.DestroyImmediate(bodyT.Find("TopEdge").gameObject);
        Object.DestroyImmediate(bodyT.Find("BottomEdge").gameObject);

        // 그림자 노출분(lift)을 없애고 판이 버튼 전체를 채운다
        var brt = (RectTransform)bodyT;
        brt.offsetMin = new Vector2(brt.offsetMin.x, 0f);

        string name = btn.name;
        bool square = name.Contains("Close") || name == "InfoBtn";
        var (file, tint) = square ? (SquareBtn, Pick(face).tint) : Pick(face);

        float scale = BorderScale(btn, square ? 120f : 160f);
        body.sprite = EditorUIBuilder.PixelSprite(file);
        body.type   = Image.Type.Sliced;
        body.color  = tint;
        body.pixelsPerUnitMultiplier = 1f / scale;

        // ⚠ 판을 꽉 채운 아이콘은 테두리 안쪽으로 들인다 (2026-10-02)
        //   인게임 스킬 슬롯처럼 아이콘이 버튼을 4px 만 남기고 덮으면
        //   새 판의 금테(≈12px × scale)가 아이콘 밑에 깔려 안 보인다.
        float edge = (file == BlueBtn || file == TealBtn ? 10f : 12f) * scale + 2f;
        var icon = bodyT.Find("Icon") as RectTransform;
        if (icon != null && icon.anchorMin == Vector2.zero && icon.anchorMax == Vector2.one)
        {
            icon.offsetMin = Vector2.Max(icon.offsetMin, new Vector2(edge, edge));
            icon.offsetMax = Vector2.Min(icon.offsetMax, new Vector2(-edge, -edge));
        }

        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = Color.white;
        cb.pressedColor     = new Color(0.68f, 0.68f, 0.74f, 1f);
        cb.selectedColor    = Color.white;
        cb.disabledColor    = new Color(0.42f, 0.42f, 0.48f, 1f);
        btn.colors = cb;
    }

    /// <summary>원래 바탕색 계열로 판을 고른다 — 버튼의 뜻(확인·위험·보조)을 색으로 유지.</summary>
    static (string file, Color tint) Pick(Color face)
    {
        Color.RGBToHSV(face, out float h, out float s, out float v);
        if (s < 0.2f)                 return (BlueBtn, v < 0.25f ? DimTint : GrayTint);   // 회색·검정
        if (h >= 0.07f && h <= 0.19f) return (GoldBtn, Color.white);                      // 금·주황 = 주요 행동
        if (h >= 0.30f && h <= 0.55f) return (TealBtn, Color.white);                      // 초록·청록 = 확인
        if (h < 0.06f || h > 0.92f)   return (GoldBtn, RedTint);                          // 빨강 = 위험·닫기
        return (BlueBtn, v < 0.35f ? DimTint : Color.white);                              // 파랑·보라 = 일반
    }

    // 9-slice 모서리 축척 — 작은 버튼일수록 모서리를 줄인다 (가운데가 사라지지 않게)
    static float BorderScale(Button btn, float full)
    {
        var rt = (RectTransform)btn.transform;
        float h = rt.rect.height;
        if (h < 1f && btn.TryGetComponent(out LayoutElement le)) h = le.preferredHeight;
        if (h < 1f) h = 100f;
        return Mathf.Clamp(h / full, 0.3f, 0.7f);
    }

    // ── 팝업 창 ──────────────────────────────────────────────

    static void SkinWindow(Transform panelT)
    {
        var panel = panelT.GetComponent<Image>();
        if (panel == null) return;

        panel.sprite = EditorUIBuilder.PixelSprite(Frame);
        panel.type   = Image.Type.Sliced;
        panel.color  = Color.white;
        panel.pixelsPerUnitMultiplier = 1f / FrameScale;

        // 앞 형제 테두리는 프레임 금테가 대신한다
        var border = panelT.parent.Find("Border");
        if (border != null && border.TryGetComponent(out Image bImg)) bImg.enabled = false;

        // ⚠ 헤더 밑 강조선을 끈다 (2026-10-02)
        //   평면 UI 에선 헤더 경계였지만, 금테 프레임 안에서는 창을 가로지르는
        //   엉뚱한 선이 되고 그 위에 걸친 글자(HeroDetail 지휘력 안내)와 겹쳤다.
        var accent = panelT.Find("AccentLine");
        if (accent != null && accent.TryGetComponent(out Image aImg)) aImg.enabled = false;

        // ⚠ 헤더에 ui_header_row 를 씌우지 않는다 (2026-10-02)
        //   그 그림의 아래 테두리가 헤더 밑에 가로선으로 남았다. 헤더 색은 화면마다의
        //   정체성(분해=적동, 어빌리티=보라 …)이라 그대로 두고, 금테만 피한다.
        //
        // ⚠ Panel 의 자식은 프레임 '위' 에 그려진다 (UI 규칙 3)
        //   가장자리까지 꽉 찬 자식 배경(헤더·본문·하단 바)은 금테를 덮는다 → 금테 두께만큼 들인다.
        foreach (Transform child in panelT)
            InsetFromFrame((RectTransform)child);
    }

    // 패널 가장자리에 붙은 불투명 배경만 들인다 (글자·버튼처럼 테두리가 없는 것은 건드리지 않는다)
    static void InsetFromFrame(RectTransform rt)
    {
        if (!rt.TryGetComponent(out Image img) || !img.enabled || img.color.a < 0.5f) return;
        if (img.sprite != null) return;   // 스프라이트가 있는 것(아이콘·스킨 버튼)은 제 모양이 있다

        Vector2 min = rt.offsetMin, max = rt.offsetMax;
        if (rt.anchorMin.x == 0f && min.x < FrameInset) min.x = FrameInset;
        if (rt.anchorMax.x == 1f && max.x > -FrameInset) max.x = -FrameInset;
        if (rt.anchorMin.y == 0f && min.y < FrameInset) min.y = FrameInset;
        if (rt.anchorMax.y == 1f && max.y > -FrameInset) max.y = -FrameInset;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }
}
