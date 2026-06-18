using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  TraitIconUI.cs
//  특성 아이콘 1개를 표시하는 재사용 컴포넌트.
//
//  ■ 재사용 위치
//    - MainPanel 캐릭터 카드 (GeneralCandidateCardUI)
//    - 인게임 특성 획득 팝업
//    - 이벤트 화면 특성 표시
//    → 특성 아이콘이 필요한 모든 UI에서 이 컴포넌트를 사용할 것
//
//  ■ 사용법
//    traitIconUI.Setup(TraitType.KnightCommand);
//    traitIconUI.Setup(traitDataInstance);
//    traitIconUI.CloseTooltip();
//
//  Inspector 연결 (MainPanelCreator 자동):
//    _iconImage   : 아이콘 이미지
//    _iconBtn     : 클릭 버튼 — 클릭 시 툴팁 토글
//    _tooltip     : 메시지 박스 루트 GO (기본 비활성)
//    _tooltipName : 툴팁 특성 이름 TMP
//    _tooltipDesc : 툴팁 특성 설명 TMP
// ============================================================

public class TraitIconUI : MonoBehaviour
{
    [SerializeField] Image           _iconImage;
    [SerializeField] Button          _iconBtn;
    [SerializeField] GameObject      _tooltip;
    [SerializeField] TextMeshProUGUI _tooltipName;
    [SerializeField] TextMeshProUGUI _tooltipDesc;
    [SerializeField] TextMeshProUGUI _tooltipStat;

    bool      _skipFrame;
    Transform _tooltipOriginalParent;

    void Awake()
    {
        if (_tooltip != null)
            _tooltipOriginalParent = _tooltip.transform.parent;
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(TraitType type, bool showStat = true)
    {
        var db   = TraitDatabase.Current;
        Setup(db?.Get(type), showStat);
    }

    public void Setup(TraitData data, bool showStat = true)
    {
        CloseTooltip();

        if (_iconImage != null)
        {
            _iconImage.sprite = data?.Icon;
            _iconImage.color  = data?.Icon != null
                ? Color.white
                : new Color(0.25f, 0.25f, 0.38f);
        }

        if (_iconBtn != null)
        {
            _iconBtn.onClick.RemoveAllListeners();
            if (data != null) _iconBtn.onClick.AddListener(OpenTooltip);
        }

        if (_tooltipName != null) _tooltipName.text = data?.TraitName ?? "—";
        if (_tooltipDesc != null) _tooltipDesc.text  = data?.Description ?? "";

        if (_tooltipStat != null)
        {
            if (!showStat)
            {
                _tooltipStat.gameObject.SetActive(false);
            }
            else
            {
                string statText = AbilityUIHelper.BuildStatText(data != null ? data.Effects : null);
                _tooltipStat.text = statText;
                _tooltipStat.gameObject.SetActive(statText.Length > 0);
            }
        }
    }

    public void CloseTooltip()
    {
        if (_tooltip == null) return;
        _tooltip.SetActive(false);
        if (_tooltipOriginalParent != null)
            _tooltip.transform.SetParent(_tooltipOriginalParent, false);
        _skipFrame = false;
    }

    // ── 내부 ─────────────────────────────────────────────────

    void OpenTooltip()
    {
        if (_tooltip == null) return;
        var rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        _tooltip.transform.SetParent(rootCanvas.transform, true);
        _tooltip.transform.SetAsLastSibling();
        _tooltip.SetActive(true);
        _skipFrame = true;
    }

    void Update()
    {
        if (_tooltip == null || !_tooltip.activeSelf) return;

        if (_skipFrame) { _skipFrame = false; return; }

        // 어떤 마우스 버튼이든 클릭 발생 시 툴팁 닫기
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            _tooltip.transform.position = Vector3.zero;
            CloseTooltip();
        }
    }

}
