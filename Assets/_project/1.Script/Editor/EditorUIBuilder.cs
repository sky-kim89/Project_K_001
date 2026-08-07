// ============================================================
//  EditorUIBuilder.cs  [Editor Only]
//  프리팹 Creator 들이 공유하는 UI 생성 헬퍼.
//
//  통합 배경
//    Creator 11개가 각자 Make*/Create*/Add* 이름으로 똑같은 헬퍼를
//    복사해 쓰고 있었다. 이름만 다르고 본문은 동일했으며, 일부는
//    alignment 지정이 빠지는 등 미묘하게 어긋나 있었다.
//    → 본문을 이 파일로 모으고, 각 Creator 는 기존 이름을
//      한 줄 포워더로 남긴다 (호출부는 그대로 유지).
//
//  주의
//    Panel() 의 parent null 허용은 "루트 오브젝트 생성" 용도다.
//    그 외 인자는 null 을 방어하지 않는다 — 잘못 넘기면 즉시 터져야 한다.
// ============================================================
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class EditorUIBuilder
{
    // ══════════════════════════════════════════════════════════
    //  계층 생성
    // ══════════════════════════════════════════════════════════

    /// <summary>RectTransform + 지정 컴포넌트를 붙인 빈 GameObject.</summary>
    public static GameObject Go(string name, GameObject parent, params Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (components != null)
            foreach (var c in components) go.AddComponent(c);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    /// <summary>RectTransform + Image 패널. parent 가 null 이면 루트로 생성된다.</summary>
    public static GameObject Panel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    /// <summary>Panel() 과 동일하되 Image 를 반환.</summary>
    public static Image Img(GameObject parent, string name, Color color)
        => Panel(parent, name, color).GetComponent<Image>();

    /// <summary>
    /// TextMeshProUGUI 생성. center=false 면 alignment 를 건드리지 않는다
    /// (TMP 기본값 TopLeft 유지 — DisassemblePopup 등이 이 동작에 의존).
    /// </summary>
    public static TextMeshProUGUI TMP(GameObject parent, string name, string text,
                                      float size, FontStyles style, bool center = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = Color.white;
        if (center) tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    /// <summary>Image + Button + 풀스트레치 Label(TMP) 구조의 버튼.</summary>
    public static GameObject Btn(GameObject parent, string name, string label,
                                 Color bgColor, float fontSize, bool boldLabel = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<Image>().color = bgColor;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        Stretch(labelGo);

        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = boldLabel ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    /// <summary>버튼에 밝기 기반 하이라이트/프레스 전이를 적용.</summary>
    public static void TintTransition(GameObject btnGo, Color baseColor, float amount = 0.20f)
    {
        var btn = btnGo.GetComponent<Button>();
        var cb  = btn.colors;
        cb.highlightedColor = Color.Lerp(baseColor, Color.white, amount);
        cb.pressedColor     = Color.Lerp(baseColor, Color.black, amount);
        btn.colors = cb;
    }

    // ══════════════════════════════════════════════════════════
    //  입체 버튼 — 누를 수 있는 요소의 프로젝트 표준
    // ══════════════════════════════════════════════════════════
    //  ★ UI 규칙: 누를 수 있는 버튼은 반드시 이 헬퍼로 만든다.
    //    평평한 사각형은 버튼인지 라벨인지 구분이 안 된다.
    //
    //  구조
    //    Root (Button)
    //    ├─ Shadow   전체를 채움 → 아래 lift 만큼이 Body 밖으로 노출 (두께)
    //    └─ Body     상단 정렬, 높이 = H - lift   ← Button.targetGraphic
    //        ├─ TopEdge     상단 2px 밝은 선   (빛 받는 면)
    //        └─ BottomEdge  하단 4px 어두운 선 (음각)
    //
    //  내용(라벨·아이콘)은 반드시 out 으로 받은 body 아래에 넣는다.
    //  Root 에 직접 넣으면 눌렸을 때 같이 내려가지 않아 어색해진다.

    public const float BtnLift = 6f;   // 그림자 노출 폭 = 떠 있는 정도

    /// <summary>입체 버튼 루트를 만들고 내용 컨테이너(body)를 돌려준다.</summary>
    public static Button RaisedBtn(GameObject parent, string name, Color face,
                                   out GameObject body, float lift = BtnLift)
        => RaisedBtnOn(Go(name, parent), face, out body, lift);

    /// <summary>
    /// 이미 있는 루트를 입체 버튼으로 만든다.
    /// 템플릿 프리팹처럼 루트가 먼저 정해진 경우에 쓴다
    /// (자식 경로가 "Body/..." 로 유지된다).
    /// </summary>
    public static Button RaisedBtnOn(GameObject root, Color face,
                                     out GameObject body, float lift = BtnLift)
    {
        var shadow = Go("Shadow", root);
        var shImg = shadow.AddComponent<Image>();
        shImg.color         = Color.Lerp(face, Color.black, 0.82f);
        shImg.raycastTarget = false;
        Stretch(shadow);

        body = Go("Body", root);
        body.AddComponent<Image>().color = face;
        var bRt = body.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0f);   bRt.anchorMax = new Vector2(1f, 1f);
        bRt.offsetMin = new Vector2(0f, lift); bRt.offsetMax = Vector2.zero;

        BtnEdge(body, "TopEdge",    Color.Lerp(face, Color.white, 0.30f), true,  2f);
        BtnEdge(body, "BottomEdge", Color.Lerp(face, Color.black, 0.45f), false, 4f);

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = body.GetComponent<Image>();
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = TintFor(Color.Lerp(face, Color.white, 0.14f), face);
        cb.pressedColor     = TintFor(Color.Lerp(face, Color.black, 0.28f), face);
        cb.selectedColor    = Color.white;
        cb.disabledColor    = TintFor(Color.Lerp(face, Color.black, 0.45f), face);
        cb.fadeDuration     = 0.08f;
        btn.colors = cb;
        return btn;
    }

    /// <summary>가운데 라벨이 있는 입체 버튼.</summary>
    public static Button RaisedTextBtn(GameObject parent, string name, string label,
                                       float fontSize, Color face, float lift = BtnLift)
    {
        var btn = RaisedBtn(parent, name, face, out var body, lift);
        var tmp = TMP(body, "Label", label, fontSize, FontStyles.Bold);
        tmp.color         = Color.white;
        tmp.raycastTarget = false;
        Stretch(tmp.gameObject);
        return btn;
    }

    static void BtnEdge(GameObject parent, string name, Color color, bool top, float thickness)
    {
        var go = Go(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot     = new Vector2(0.5f, top ? 1f : 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0f, thickness);
    }

    /// <summary>
    /// Button.colors 는 targetGraphic 색에 곱해진다.
    /// 원하는 최종 색을 얻으려면 기준색으로 나눈 값을 넣어야 한다.
    /// </summary>
    public static Color TintFor(Color want, Color baseColor) => new Color(
        baseColor.r <= 0.001f ? 1f : Mathf.Clamp01(want.r / baseColor.r),
        baseColor.g <= 0.001f ? 1f : Mathf.Clamp01(want.g / baseColor.g),
        baseColor.b <= 0.001f ? 1f : Mathf.Clamp01(want.b / baseColor.b),
        1f);

    /// <summary>부모를 가득 채우는 배경 Image 를 첫 자식으로 삽입.</summary>
    public static GameObject BgPanel(GameObject parent, Color color)
    {
        var go = Panel(parent, "BgPanel", color);
        go.transform.SetAsFirstSibling();
        Stretch(go);
        return go;
    }

    // ══════════════════════════════════════════════════════════
    //  팝업 공용 시각 언어
    // ══════════════════════════════════════════════════════════
    //  "어두운 남색 패널 + 헤더 밴드 + 강조선 + 섹션 라벨" 계열.
    //  EventPopup 에서 시작해 BattleResult / Reincarnation 이 공유한다.
    //  승패색·확인버튼색처럼 팝업 하나에만 쓰는 강조색은 여기 두지 않는다.

    public static class Pop
    {
        public static readonly Color PanelBg     = new Color(0.070f, 0.075f, 0.130f, 1f);
        public static readonly Color PanelBorder = new Color(0.24f,  0.30f,  0.52f,  1f);
        public static readonly Color HeaderBg    = new Color(0.095f, 0.110f, 0.200f, 1f);
        public static readonly Color TitleShadow = new Color(0.02f,  0.02f,  0.06f,  0.85f);
        public static readonly Color SectionLbl  = new Color(0.66f,  0.70f,  0.86f,  1f);
        public static readonly Color Divider     = new Color(0.26f,  0.29f,  0.44f,  0.85f);
        public static readonly Color SlotBg      = new Color(0.105f, 0.115f, 0.190f, 1f);
        public static readonly Color SubText     = new Color(0.72f,  0.76f,  0.90f,  1f);
        public static readonly Color TabActive   = new Color(0.24f,  0.40f,  0.74f,  1f);
        public static readonly Color TabInactive = new Color(0.155f, 0.175f, 0.275f, 1f);
    }

    /// <summary>
    /// 가운데 글자 + 좌우 라인 형태의 섹션 구분 라벨 (상단 밴드 배치).
    /// contentW 는 라인 길이 계산용 — 라벨 줄의 실제 폭이다.
    /// </summary>
    public static GameObject SectionLabel(GameObject parent, string text, float yFromTop,
                                          float contentW, float sidePad)
    {
        const float LabelW = 200f;
        const float Gap    = 16f;
        float h = UIScale.RowSm;   // FontSm 한 줄이 잘리지 않는 최소 높이 (UI 규칙 5)

        var row = Go($"Section_{text}", parent);
        AnchorTop(row.GetComponent<RectTransform>(), yFromTop, h, sidePad * 2f);

        var label = TMP(row, "Label", text, UIScale.FontSm, FontStyles.Bold);
        label.color         = Pop.SectionLbl;
        label.raycastTarget = false;
        Center(label.rectTransform, Vector2.zero, new Vector2(LabelW, h));

        for (int side = 0; side < 2; side++)
        {
            var line = Go(side == 0 ? "LineL" : "LineR", row);
            var img  = line.AddComponent<Image>();
            img.color         = Pop.Divider;
            img.raycastTarget = false;
            var rt = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(side == 0 ? 0f : 1f, 0.5f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot     = new Vector2(side == 0 ? 0f : 1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2((contentW - LabelW) * 0.5f - Gap, 2f);
        }
        return row;
    }

    // ══════════════════════════════════════════════════════════
    //  기호 도형 — 폰트 글리프 대신 Image 로 그린다
    // ══════════════════════════════════════════════════════════
    //  ⚠ 기본 폰트(LiberationSans SDF)에 없는 글자: ★ ✔ ✕ ▶ ◀ ▲ ⚙ 🔒
    //    (› — × 는 있다. 한글은 폴백 폰트가 처리한다)
    //    아틀라스가 Static 이라 런타임에 글리프를 채울 수도 없어,
    //    없는 글자는 □(두부)로 그려져 UI 에 그대로 노출된다.
    //    장식 기호는 아래 헬퍼로 만들 것 — 폰트에 의존하지 않는다.

    /// <summary>회전한 막대 하나. 기호를 조립하는 기본 단위.</summary>
    public static GameObject Bar(GameObject parent, string name, float length, float thickness,
                                 float angleDeg, Vector2 offset, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);

        var img = go.GetComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta        = new Vector2(length, thickness);
        rt.localEulerAngles = new Vector3(0f, 0f, angleDeg);
        return go;
    }

    /// <summary>체크 표시 (✔ 대체). size = 외접 정사각형 한 변.</summary>
    public static GameObject CheckMark(GameObject parent, string name, float size, Color color)
    {
        var root = Go(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        float t = Mathf.Max(3f, size * 0.14f);
        Bar(root, "Short", size * 0.42f, t,  45f, new Vector2(-size * 0.20f, -size * 0.12f), color);
        Bar(root, "Long",  size * 0.80f, t, -45f, new Vector2( size * 0.10f,  size * 0.06f), color);
        return root;
    }

    /// <summary>
    /// 위로 향한 삼각형 (▲ 대체) — "업(상승)" 표시용.
    /// 가로 막대를 계단식으로 쌓아 그린다. 도트 느낌이라 이 게임 아트와도 맞는다.
    /// </summary>
    public static GameObject TriangleUp(GameObject parent, string name, float size, Color color)
    {
        var root = Go(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        const int Steps = 5;
        float rowH = size / Steps;
        for (int i = 0; i < Steps; i++)
        {
            // 위에서 i번째 줄 — 아래로 갈수록 넓어진다.
            // 높이에 +1 을 더해 줄 사이에 실오라기 같은 틈이 생기지 않게 한다.
            float w = size * (i + 1) / Steps;
            float y = size * 0.5f - rowH * (i + 0.5f);
            Bar(root, $"Row{i}", w, rowH + 1f, 0f, new Vector2(0f, y), color);
        }
        return root;
    }

    /// <summary>X 표시 (✕ 대체) — 닫기 버튼용.</summary>
    public static GameObject XMark(GameObject parent, string name, float size, Color color)
    {
        var root = Go(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);

        float t = Mathf.Max(3f, size * 0.13f);
        Bar(root, "A", size * 0.92f, t,  45f, Vector2.zero, color);
        Bar(root, "B", size * 0.92f, t, -45f, Vector2.zero, color);
        return root;
    }

    /// <summary>꺾쇠 (› ‹ ▶ ◀ 대체). dirDeg: 0=우, 180=좌, 90=위, -90=아래.</summary>
    public static GameObject Chevron(GameObject parent, string name, float size,
                                     float dirDeg, Color color)
    {
        var root = Go(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(size, size);
        rt.localEulerAngles = new Vector3(0f, 0f, dirDeg);

        float t = Mathf.Max(3f, size * 0.16f);
        float h = size * 0.52f;
        Bar(root, "Up",   h, t, -45f, new Vector2(-size * 0.10f,  size * 0.19f), color);
        Bar(root, "Down", h, t,  45f, new Vector2(-size * 0.10f, -size * 0.19f), color);
        return root;
    }

    /// <summary>자물쇠 (🔒 대체) — 잠금 상태 표시.</summary>
    public static GameObject PadLock(GameObject parent, string name, float size, Color color)
    {
        var root = Go(name, parent);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        float t  = Mathf.Max(3f, size * 0.11f);
        float bw = size * 0.62f;          // 몸통 폭
        float bh = size * 0.46f;          // 몸통 높이
        float sw = size * 0.36f;          // 고리 폭

        // 몸통
        var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
        bodyGo.transform.SetParent(root.transform, false);
        var bImg = bodyGo.GetComponent<Image>();
        bImg.color = color; bImg.raycastTarget = false;
        var bRt = bodyGo.GetComponent<RectTransform>();
        bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.5f);
        bRt.pivot     = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(0f, -size * 0.16f);
        bRt.sizeDelta        = new Vector2(bw, bh);

        // 고리 (ㄷ 자 — 좌·우 세로 + 상단 가로)
        float shTop = size * 0.36f;
        Bar(root, "ShackleL", size * 0.30f, t, 90f, new Vector2(-sw * 0.5f, shTop * 0.62f), color);
        Bar(root, "ShackleR", size * 0.30f, t, 90f, new Vector2( sw * 0.5f, shTop * 0.62f), color);
        Bar(root, "ShackleT", sw + t,       t,  0f, new Vector2(0f,         shTop),         color);
        return root;
    }

    /// <summary>마름모 (★ 대체) — 태그·불릿용 소형 장식.</summary>
    public static GameObject Diamond(GameObject parent, string name, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);

        var img = go.GetComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(size, size);
        rt.localEulerAngles = new Vector3(0f, 0f, 45f);
        return go;
    }

    // ══════════════════════════════════════════════════════════
    //  RectTransform
    // ══════════════════════════════════════════════════════════

    /// <summary>부모를 가득 채우도록 앵커·오프셋 설정.</summary>
    /// <summary>
    /// 이름으로 자손을 찾는다 (직계 우선 → 깊이 우선).
    /// Creator 가 다른 Creator 의 프리팹(HeroCard 등)에서 필드를 뽑아 쓸 때 사용한다.
    /// 그쪽 계층이 한 단계 깊어져도 연결이 끊기지 않는다.
    /// </summary>
    public static T FindDeep<T>(Transform root, string name) where T : Component
    {
        var direct = root.Find(name);
        if (direct != null) return direct.GetComponent<T>();

        foreach (Transform child in root)
        {
            var found = FindDeep<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void Stretch(GameObject go) => Stretch(go.GetComponent<RectTransform>());

    /// <summary>부모 중앙 기준 고정 크기 배치.</summary>
    public static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    /// <summary>상단 스트레치 배치 — 위에서 yFromTop 만큼 내려온 높이 height 영역.</summary>
    public static void AnchorTop(RectTransform rt, float yFromTop, float height, float padH = 0f)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta        = new Vector2(-padH, height);
    }

    // ══════════════════════════════════════════════════════════
    //  레이아웃
    // ══════════════════════════════════════════════════════════

    /// <summary>LayoutElement 를 붙이고 preferred 크기 지정 (0 이하는 건너뜀).</summary>
    public static LayoutElement LE(GameObject go, float width, float height)
    {
        if (!go.TryGetComponent<LayoutElement>(out var le))
            le = go.AddComponent<LayoutElement>();
        if (width  > 0f) le.preferredWidth  = width;
        if (height > 0f) le.preferredHeight = height;
        return le;
    }

    /// <summary>재화 위젯용 가로 레이아웃 (아이콘 + 수량 텍스트).</summary>
    public static void CostHlg(GameObject go)
    {
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing                = 3f;
    }

    /// <summary>레이아웃 안에서 아이콘 크기를 고정.</summary>
    public static void IconLE(Image img, float size)
    {
        img.rectTransform.sizeDelta = new Vector2(size, size);
        var le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = size;
        le.preferredHeight = size;
        le.minWidth        = size;
        le.minHeight       = size;
    }

    // ══════════════════════════════════════════════════════════
    //  SerializedObject 필드 연결
    // ══════════════════════════════════════════════════════════
    //  필드를 못 찾으면 LogError — 프리팹은 만들어지지만 연결이 빠진
    //  상태로 조용히 넘어가면 안 된다. (필드명 변경·삭제를 즉시 노출)

    public static void SetObj(SerializedObject so, string field, UnityEngine.Object obj, string tag)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogError($"[{tag}] 직렬화 필드 없음: {field}"); return; }
        prop.objectReferenceValue = obj;
    }

    public static void SetObjArray(SerializedObject so, string field,
                                   UnityEngine.Object[] objs, string tag)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogError($"[{tag}] 직렬화 배열 필드 없음: {field}"); return; }
        prop.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
    }

    public static void SetEnum(SerializedObject so, string field, int value, string tag)
    {
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogError($"[{tag}] 직렬화 필드 없음: {field}"); return; }
        prop.intValue = value;
    }
}
