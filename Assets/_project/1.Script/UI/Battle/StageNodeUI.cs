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

    [SerializeField] Image           _bg;
    [SerializeField] Image           _icon;
    [SerializeField] TextMeshProUGUI _label;
    [SerializeField] GameObject      _marker;
    [SerializeField] LayoutElement   _le;

    static readonly Color ColNormal  = new(0.30f, 0.33f, 0.46f);
    static readonly Color ColElite   = new(0.85f, 0.22f, 0.22f);
    static readonly Color ColShop    = new(0.18f, 0.62f, 0.90f);
    static readonly Color ColEvent   = new(0.65f, 0.28f, 0.85f);
    static readonly Color ColCurrent = new(1.00f, 0.85f, 0.15f);
    static readonly Color ColCleared = new(0.18f, 0.20f, 0.28f);

    public void Setup(RunStageType type, bool isCurrent, bool isCleared)
    {
        float size          = isCurrent ? NodeCurrent : NodeSize;
        _le.preferredWidth  = size;
        _le.preferredHeight = size;

        _bg.color = NodeColor(type, isCurrent, isCleared);

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
        _label.gameObject.SetActive(showLabel);
        if (showLabel)
        {
            _label.text  = labelText;
            _label.color = isCurrent ? ColCurrent : LabelColor(type);
        }

        _marker.SetActive(isCurrent);
    }

    static Color NodeColor(RunStageType t, bool isCurrent, bool isCleared)
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

    static Color LabelColor(RunStageType t) => t switch
    {
        RunStageType.Elite => ColElite,
        RunStageType.Shop  => ColShop,
        RunStageType.Event => ColEvent,
        _                  => new Color(0.55f, 0.58f, 0.70f),
    };

    static string TypeName(RunStageType t) => t switch
    {
        RunStageType.Elite => "엘리트",
        RunStageType.Shop  => "상점",
        RunStageType.Event => "이벤트",
        _                  => "",
    };
}
