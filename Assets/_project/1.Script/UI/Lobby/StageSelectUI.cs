using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  StageSelectUI.cs
//  BattlePanel 전체 컨트롤러.
//
//  좌측: 5개 DeploySlotUI (배치 슬롯)
//  우측: 스테이지 정보 + 어빌리티 목록 버튼 + 용병 구매 버튼 + 전투 시작 버튼
//
//  Inspector 연결:
//    _deploySlots[0~4] : DeploySlotUI 컴포넌트
//    _stageText        : "스테이지 N 도전" TMP
//    _progressText     : "N 스테이지 클리어" TMP
//    _abilityListBtn   : 어빌리티 목록 버튼
//    _hireBtn          : 용병 구매 버튼
//    _battleStartBtn   : 전투 시작 버튼
// ============================================================

public class StageSelectUI : MonoBehaviour
{
    [Header("배치 슬롯 (0~4)")]
    [SerializeField] DeploySlotUI[] _deploySlots;

    [Header("스테이지 정보")]
    [SerializeField] TextMeshProUGUI _stageText;
    [SerializeField] TextMeshProUGUI _progressText;

    [Header("버튼")]
    [SerializeField] Button _abilityListBtn;
    [SerializeField] Button _hireBtn;
    [SerializeField] Button _battleStartBtn;

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()
    {
        LobbyManager.OnStageChanged += OnStageChanged;
        BindButtons();
        Refresh();
    }

    void OnDisable()
    {
        LobbyManager.OnStageChanged -= OnStageChanged;
    }

    // ── 버튼 연결 ─────────────────────────────────────────────

    void BindButtons()
    {
        _abilityListBtn?.onClick.RemoveAllListeners();
        _abilityListBtn?.onClick.AddListener(() =>
            PopupManager.Instance?.Open<AbilityListPopup>(PopupType.AbilityList));

        _hireBtn?.onClick.RemoveAllListeners();
        _hireBtn?.onClick.AddListener(OpenMercenaryShop);

        _battleStartBtn?.onClick.RemoveAllListeners();
        _battleStartBtn?.onClick.AddListener(() => LobbyManager.Instance?.StartBattle());
    }

    // ── 전체 갱신 ─────────────────────────────────────────────

    public void Refresh()
    {
        RefreshStageInfo();
        RefreshSlots();
    }

    void OnStageChanged(StageData _) => RefreshStageInfo();

    void RefreshStageInfo()
    {
        var progress = UserDataManager.Instance?.Get<StageProgressData>();
        int cleared  = progress?.ClearedNormalStages ?? 0;
        int current  = cleared + 1;

        if (_stageText    != null) _stageText.text    = $"스테이지 {current} 도전";
        if (_progressText != null) _progressText.text = cleared > 0
            ? $"{cleared} 스테이지 클리어"
            : "첫 번째 스테이지";
    }

    void RefreshSlots()
    {
        if (_deploySlots == null) return;
        for (int i = 0; i < _deploySlots.Length; i++)
        {
            int capturedIdx = i;
            _deploySlots[i]?.Setup(capturedIdx,
                onEmpty:    () => OpenMercenaryShop(),
                onOccupied: (entry, slot) => OpenHeroDetail(entry, slot));
        }
    }

    // ── 팝업 열기 ─────────────────────────────────────────────

    void OpenMercenaryShop()
    {
        PopupManager.Instance?.Open<MercenaryShopPopup>(
            PopupType.MercenaryShop,
            onClose: Refresh);
    }

    void OpenHeroDetail(UnitEntry entry, int slot)
    {
        PopupManager.Instance?.Open<HeroDetailPopup>(
            PopupType.HeroDetail,
            onClose: Refresh)
            ?.Setup(entry, slot);
    }
}
