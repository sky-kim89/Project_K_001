using TMPro;
using UnityEngine;

// ============================================================
//  StageProgressBarUI.cs
//  다음 스테이지 타입을 아이콘으로 표시하는 슬라이딩 윈도우 UI.
//
//  Inspector 연결 (BattlePanelCreator 자동):
//    _nodeContainer : 노드 부모 (HorizontalLayoutGroup 설정됨)
//    _nodePrefab    : StageNodeUI.prefab
//    _arrowPrefab   : StageArrowUI.prefab
//
//  스프라이트는 SpriteManager(Atlas_StageNodes) 에서 자동 조회.
// ============================================================
public class StageProgressBarUI : MonoBehaviour
{
    [SerializeField] Transform   _nodeContainer;
    [SerializeField] StageNodeUI _nodePrefab;
    [SerializeField] GameObject  _arrowPrefab;

    const int VisibleCount = 7;

    static readonly Color ColArrow   = new(0.30f, 0.32f, 0.42f);
    static readonly Color ColCleared = new(0.18f, 0.20f, 0.28f);

    StageNodeUI[]     _nodePool;
    TextMeshProUGUI[] _arrowPool;

    void Awake() => Init();

    public void Init()
    {
        if (_nodePool != null) return;

        _nodePool  = new StageNodeUI[VisibleCount];
        _arrowPool = new TextMeshProUGUI[VisibleCount - 1];

        // 컨테이너 순서: Node0, Arrow0, Node1, Arrow1, ..., Arrow5, Node6
        for (int i = 0; i < VisibleCount; i++)
        {
            var node = Instantiate(_nodePrefab, _nodeContainer);
            node.gameObject.SetActive(false);
            _nodePool[i] = node;

            if (i < VisibleCount - 1)
            {
                var arrowGo = Instantiate(_arrowPrefab, _nodeContainer);
                arrowGo.SetActive(false);
                _arrowPool[i] = arrowGo.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    public void Refresh(RunStageType[] seq, int currentStage)
    {
        if (_nodePool == null) Init();

        if (seq == null || seq.Length == 0)
        {
            HideAll();
            return;
        }

        int half     = 2;
        int startIdx = Mathf.Max(0, currentStage - half);
        int endIdx   = Mathf.Min(seq.Length - 1, startIdx + VisibleCount - 1);
        startIdx     = Mathf.Max(0, endIdx - VisibleCount + 1);
        int count    = endIdx - startIdx + 1;

        for (int j = 0; j < count; j++)
        {
            int  seqIdx    = startIdx + j;
            bool isCurrent = seqIdx == currentStage;
            bool isCleared = seqIdx  <  currentStage;

            _nodePool[j].gameObject.SetActive(true);
            _nodePool[j].Setup(seq[seqIdx], isCurrent, isCleared);

            if (j < count - 1)
            {
                _arrowPool[j].gameObject.SetActive(true);
                _arrowPool[j].color = (seqIdx + 1) < currentStage ? ColCleared : ColArrow;
            }
        }

        for (int n = count;     n < _nodePool.Length;  n++) _nodePool[n].gameObject.SetActive(false);
        for (int a = count - 1; a < _arrowPool.Length; a++) _arrowPool[a].gameObject.SetActive(false);
    }

    void HideAll()
    {
        foreach (var n in _nodePool)  n.gameObject.SetActive(false);
        foreach (var a in _arrowPool) a.gameObject.SetActive(false);
    }
}
