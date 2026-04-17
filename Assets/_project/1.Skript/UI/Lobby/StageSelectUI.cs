using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  StageSelectUI.cs
//  로비 중앙 — 스테이지 선택 전체를 담당하는 UI 컴포넌트.
//
//  LobbyManager.OnStageChanged 이벤트를 구독해 자동 갱신.
//  버튼 클릭 → LobbyManager API 호출.
// ============================================================

public class StageSelectUI : MonoBehaviour
{
    [Header("탭")]
    [SerializeField] Button _normalTabBtn;
    [SerializeField] Button _eliteTabBtn;
    [SerializeField] Color  _tabActiveColor   = new Color(0.20f, 0.70f, 0.90f, 1f);
    [SerializeField] Color  _tabInactiveColor = new Color(0.22f, 0.22f, 0.28f, 1f);

    [Header("스테이지 정보")]
    [SerializeField] TextMeshProUGUI _stageNameText;
    [SerializeField] TextMeshProUGUI _bestRecordText;

    [Header("프리뷰")]
    [SerializeField] Image  _previewImage;
    [SerializeField] Button _prevBtn;
    [SerializeField] Button _nextBtn;

    [Header("전투")]
    [SerializeField] Button          _battleStartBtn;
    [SerializeField] TextMeshProUGUI _energyCostText;
    [SerializeField] TextMeshProUGUI _progressText;

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()  => LobbyManager.OnStageChanged += Refresh;
    void OnDisable() => LobbyManager.OnStageChanged -= Refresh;

    void Start()
    {
        _normalTabBtn?  .onClick.AddListener(() => LobbyManager.Instance.SetTab(BattleMode.Normal));
        _eliteTabBtn?   .onClick.AddListener(() => LobbyManager.Instance.SetTab(BattleMode.Elite));
        _prevBtn?       .onClick.AddListener(() => LobbyManager.Instance.Navigate(-1));
        _nextBtn?       .onClick.AddListener(() => LobbyManager.Instance.Navigate(1));
        _battleStartBtn?.onClick.AddListener(() => LobbyManager.Instance.StartBattle());

        if (LobbyManager.Instance != null)
            Refresh(LobbyManager.Instance.CurrentStage);
    }

    // ── 갱신 ─────────────────────────────────────────────────

    void Refresh(StageData stage)
    {
        if (LobbyManager.Instance == null) return;

        var tab = LobbyManager.Instance.CurrentTab;

        // 탭 색상
        SetTabColor(_normalTabBtn, tab == BattleMode.Normal);
        SetTabColor(_eliteTabBtn,  tab == BattleMode.Elite);

        // 내비 버튼 활성화
        if (_prevBtn != null) _prevBtn.interactable = LobbyManager.Instance.CanNavigate(-1);
        if (_nextBtn != null) _nextBtn.interactable = LobbyManager.Instance.CanNavigate(1);

        if (stage == null) return;

        if (_stageNameText  != null) _stageNameText.text  = stage.DisplayName;
        if (_bestRecordText != null) _bestRecordText.text = "최고 기록  --:--";   // TODO: UserData 연동
        if (_energyCostText != null) _energyCostText.text = $"⚡  {stage.EnergyCost}";
        if (_progressText   != null)
        {
            int limit = stage.DailyClearLimit > 0 ? stage.DailyClearLimit : 0;
            _progressText.text = limit > 0
                ? $"{stage.DisplayName} 클리어  0 / {limit}"
                : stage.DisplayName;
        }

        if (_previewImage != null)
        {
            _previewImage.sprite  = stage.PreviewSprite;
            _previewImage.enabled = stage.PreviewSprite != null;
        }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────

    void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? _tabActiveColor : _tabInactiveColor;
    }
}
