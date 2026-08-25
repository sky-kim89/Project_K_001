using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 진행바의 노드 하나.
// BattlePanelCreator 가 StageNodeUI.prefab 으로 생성하고
// StageProgressBarUI.Init() 에서 Instantiate 후 재활용한다.
public class StageNodeUI : MonoBehaviour
{
    public const float NodeSize    = 52f;
    public const float NodeCurrent = 62f;   // NodeSize × ~1.2

    /// <summary>테두리 두께. Bg 를 이만큼 안쪽으로 넣어 테를 드러낸다.</summary>
    public const float BorderThick = 3f;

    [SerializeField] Image           _bg;
    [SerializeField] Image           _border;
    [SerializeField] Image           _icon;
    [SerializeField] TextMeshProUGUI _label;
    [SerializeField] GameObject      _marker;
    [SerializeField] LayoutElement   _le;

    /// <summary>
    /// 라벨 + 그 뒤에 깔리는 판. 켜고 끄는 대상은 이쪽이다.
    ///
    /// ⚠ 라벨만 있으면 배경 위에서 안 읽힌다
    ///   "이벤트" 는 보라(0.65,0.28,0.85)라 로비 배경에 그대로 묻혔다.
    ///   글자 뒤에 어두운 판을 깔고 글자색도 흰색 쪽으로 당긴다.
    ///   판이 없는 옛 프리팹에서는 라벨 자신을 켠다.
    /// </summary>
    [SerializeField] GameObject _labelRoot;

    GameObject LabelRoot => _labelRoot != null ? _labelRoot : _label.gameObject;

    // ── 색 ────────────────────────────────────────────────────
    //
    //  ⚠ 판(Bg)은 전부 같은 색이고, 속성은 **테두리**로만 말한다 (2026-08-26)
    //    예전엔 판 자체를 속성 색으로 칠했다. 빨강·파랑·보라 판이 한 줄에 늘어서니
    //    진행바가 아니라 색 띠처럼 보였고, 판 위에 얹힌 아이콘도 색마다 다른 바탕에
    //    앉아 어떤 칸은 아이콘이 묻혔다.
    //    판을 하나로 통일하면 아이콘 대비가 어디서나 같고, 속성은 테두리로 읽힌다.
    public static readonly Color ColBg = new(0.16f, 0.18f, 0.26f);

    // 테두리 — 속성별. 일반은 일부러 죽인다 (평범한 칸이 소리칠 이유가 없다)
    static readonly Color ColNormal  = new(0.42f, 0.46f, 0.60f);
    static readonly Color ColElite   = new(0.85f, 0.22f, 0.22f);
    static readonly Color ColShop    = new(0.18f, 0.62f, 0.90f);
    static readonly Color ColEvent   = new(0.65f, 0.28f, 0.85f);
    static readonly Color ColCurrent = new(1.00f, 0.85f, 0.15f);
    static readonly Color ColCleared = new(0.20f, 0.22f, 0.30f);

    public void Setup(RunStageType type, bool isCurrent, bool isCleared)
    {
        float size          = isCurrent ? NodeCurrent : NodeSize;
        _le.preferredWidth  = size;
        _le.preferredHeight = size;

        // 판은 언제나 같은 색 — 속성·상태는 테두리가 말한다.
        _bg.color = ColBg;
        if (_border != null) _border.color = BorderColor(type, isCurrent, isCleared);

        // SpriteManager 를 통해 atlas 스프라이트 조회
        string spriteName = $"stage_{type.ToString().ToLower()}";
        var sprite = SpriteManager.Instance?.Get(spriteName);
        _icon.enabled = sprite != null;
        if (sprite != null)
        {
            _icon.sprite = sprite;
            _icon.color  = isCleared ? new Color(0.35f, 0.37f, 0.48f) : Color.white;
        }

        string labelText = TypeName(type);
        bool   showLabel = !string.IsNullOrEmpty(labelText) && !isCleared;
        LabelRoot.SetActive(showLabel);
        if (showLabel)
        {
            _label.text  = labelText;
            _label.color = isCurrent ? ColCurrent : LabelColor(type);
        }

        _marker.SetActive(isCurrent);
    }

    /// <summary>
    /// 테두리 색 — 속성과 상태를 여기 하나로 말한다.
    ///
    /// 순서가 중요하다: 지나온 칸(cleared)이 먼저다.
    /// 클리어한 엘리트 칸이 계속 빨갛게 남으면 아직 남은 관문처럼 보인다.
    /// </summary>
    static Color BorderColor(RunStageType t, bool isCurrent, bool isCleared)
    {
        if (isCleared) return ColCleared;
        if (isCurrent) return ColCurrent;
        return t switch
        {
            RunStageType.Elite => ColElite,
            RunStageType.Shop  => ColShop,
            RunStageType.Event => ColEvent,
            _                  => ColNormal,
        };
    }

    // 어두운 판 위에 얹히는 글자라 원색 그대로는 어둡다 — 흰색 쪽으로 당겨 읽히게 한다.
    // 테두리와 같은 계열을 쓰므로 라벨과 테가 한 덩어리로 읽힌다.
    static Color LabelColor(RunStageType t) => Color.Lerp(t switch
    {
        RunStageType.Elite => ColElite,
        RunStageType.Shop  => ColShop,
        RunStageType.Event => ColEvent,
        _                  => new Color(0.55f, 0.58f, 0.70f),
    }, Color.white, 0.35f);

    static string TypeName(RunStageType t) => t switch
    {
        RunStageType.Elite => "엘리트",
        RunStageType.Shop  => "상점",
        RunStageType.Event => "이벤트",
        _                  => "",
    };
}
