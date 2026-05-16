using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  StageSelectUI.cs
//  BattlePanel 전체 컨트롤러.
//
//  좌측: 용병 구매 버튼(상단) + 5개 DeploySlotUI (배치 슬롯)
//  우측: 스테이지 정보 + 어빌리티 목록 버튼 + 유물 버튼 + 전투 시작 버튼
//
//  Inspector 연결:
//    _deploySlots[0~4] : DeploySlotUI 컴포넌트
//    _stageText        : "스테이지 N 도전" TMP
//    _progressText     : "N 스테이지 클리어" TMP
//    _abilityListBtn   : 어빌리티 목록 버튼
//    _hireBtn          : 용병 구매 버튼 (DeployArea 상단)
//    _relicBtn         : 유물 탭 이동 버튼 (ActionArea)
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
    [SerializeField] Button          _abilityListBtn;
    [SerializeField] Button          _hireBtn;
    [SerializeField] TextMeshProUGUI _hireCostText;
    [SerializeField] Button          _relicBtn;
    [SerializeField] Button          _battleStartBtn;

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
        _hireBtn?.onClick.AddListener(() => OpenMercenaryShop());

        _relicBtn?.onClick.RemoveAllListeners();
        _relicBtn?.onClick.AddListener(() =>
            GetComponentInParent<LobbyNavUI>()?.Switch(3));

        _battleStartBtn?.onClick.RemoveAllListeners();
        _battleStartBtn?.onClick.AddListener(() => LobbyManager.Instance?.StartBattle());
    }

    // ── 전체 갱신 ─────────────────────────────────────────────

    public void Refresh()
    {
        RefreshStageInfo();
        RefreshSlots();
        RefreshHireBtn();
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
                onEmpty:    () => OpenMercenaryShop(capturedIdx),
                onOccupied: (entry, slot) => OpenHeroDetail(entry, slot));
        }
    }

    // ── 팝업 열기 ─────────────────────────────────────────────

    void RefreshHireBtn()
    {
        if (_hireBtn == null) return;

        // 5개 슬롯 모두 채워졌을 때만 활성화 (빈 슬롯이 있으면 슬롯 자체가 고용 버튼)
        bool allFull   = AreAllSlotsFull();
        int  cost      = GameplayConfig.Current?.HireMercenaryCost ?? 500;
        int  gold      = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
        bool canUse    = allFull && gold >= cost;

        _hireBtn.interactable = canUse;
        if (_hireCostText != null)
            _hireCostText.color = canUse
                ? new Color(1f, 0.85f, 0.20f)
                : new Color(0.55f, 0.45f, 0.10f);
    }

    bool AreAllSlotsFull()
    {
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        if (deployData == null) return false;
        for (int i = 0; i < 5; i++)
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) return false;
        return true;
    }

    // targetSlot: 클릭한 빈 슬롯 인덱스(-1이면 HireBtn에서 열림 = SlotFullView 경유)
    void OpenMercenaryShop(int targetSlot = -1)
    {
        int cost = GameplayConfig.Current?.HireMercenaryCost ?? 500;
        int gold = UserDataManager.Instance?.Get<ItemData>()?.Get(eItem.Gold) ?? 0;
        if (gold < cost)
        {
            // 토스트 팝업 노출
            Debug.Log("골드가 부족합니다!");      
            return;
        }

        var popup = PopupManager.Instance.Open<MercenaryShopPopup>(PopupType.MercenaryShop);
        popup.SetOnClose(Refresh);
        popup.Setup(targetSlot);
    }

    void OpenHeroDetail(UnitEntry entry, int slot)
    {
        var popup = PopupManager.Instance.Open<HeroDetailPopup>(PopupType.HeroDetail);
        popup.SetOnClose(Refresh);
        popup.Setup(entry, slot);
    }
}
