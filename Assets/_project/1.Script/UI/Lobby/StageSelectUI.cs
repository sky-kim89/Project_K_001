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
    [SerializeField] TextMeshProUGUI   _stageText;
    [SerializeField] TextMeshProUGUI   _progressText;
    [SerializeField] TextMeshProUGUI   _stageTypeText;     // 현재 스테이지 타입 표시

    [Header("런 진행바")]
    [SerializeField] StageProgressBarUI _progressBar;

    [Header("특성 아이콘 목록 (빨간 영역)")]
    [SerializeField] TraitIconUI[]     _traitIcons;        // 보유 특성 표시 아이콘

    [Header("스테이지 타입 아이콘 (우측)")]
    [SerializeField] GameObject        _shopIcon;          // 상점 스테이지 아이콘
    [SerializeField] GameObject        _eventIcon;         // 이벤트 스테이지 아이콘

    [Header("버튼")]
    [SerializeField] Button          _abilityListBtn;
    [SerializeField] Button          _relicBtn;
    [SerializeField] Button          _hireBtn;
    [SerializeField] TextMeshProUGUI _hireCostText;
    [SerializeField] Button          _disassembleBtn;
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

        _relicBtn?.onClick.RemoveAllListeners();
        _relicBtn?.onClick.AddListener(() =>
            GetComponentInParent<LobbyNavUI>()?.Switch(3));

        _hireBtn?.onClick.RemoveAllListeners();
        _hireBtn?.onClick.AddListener(() => OpenMercenaryShop());

        _disassembleBtn?.onClick.RemoveAllListeners();
        _disassembleBtn?.onClick.AddListener(() =>
            PopupManager.Instance?.Open<DisassemblePopup>(PopupType.Disassemble));

        _battleStartBtn?.onClick.RemoveAllListeners();
        _battleStartBtn?.onClick.AddListener(() => LobbyManager.Instance?.StartBattle());

        // 상점 아이콘 → RunShopPopup 직접 오픈
        _shopIcon?.GetComponent<Button>()?.onClick.RemoveAllListeners();
        _shopIcon?.GetComponent<Button>()?.onClick.AddListener(() =>
            PopupManager.Instance?.Open<RunShopPopup>(PopupType.RunShop));
    }

    // ── 전체 갱신 ─────────────────────────────────────────────

    public void Refresh()
    {
        RefreshStageInfo();
        RefreshSlots();
        RefreshHireBtn();
        RefreshTraitIcons();
    }

    void OnStageChanged(StageData _) => RefreshStageInfo();

    void RefreshStageInfo()
    {
        var progress    = UserDataManager.Instance?.Get<StageProgressData>();
        int stageIndex  = progress?.CurrentRunStage ?? 0;
        int stageNum    = stageIndex + 1;
        int maxStage    = StageConfig.Current?.NormalStageCount ?? 30;
        var stageType   = progress?.CurrentStageType ?? RunStageType.Normal;

        if (_stageText    != null) _stageText.text    = $"스테이지 {stageNum} 도전";
        if (_progressText != null) _progressText.text = $"{stageIndex} / {maxStage} 스테이지";
        if (_stageTypeText != null)
        {
            _stageTypeText.text = stageType switch
            {
                RunStageType.Elite => "★ 엘리트",
                RunStageType.Shop  => "🛒 상점",
                RunStageType.Event => "? 이벤트",
                _                  => "일반",
            };
        }

        // 런 진행바 갱신
        var seq = progress?.GetRunSequence();
        if (_progressBar != null && seq != null && seq.Length > 0)
            _progressBar.Refresh(seq, stageIndex);

        // 우측 스테이지 타입 아이콘 표시/숨김
        _shopIcon?.SetActive(stageType == RunStageType.Shop);
        _eventIcon?.SetActive(stageType == RunStageType.Event);

        // 전투 시작 버튼 텍스트 변경
        var label = _battleStartBtn?.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = stageType == RunStageType.Shop ? "상점 입장" : "전투 시작";
    }

    void RefreshTraitIcons()
    {
        if (_traitIcons == null) return;
        var traitData = UserDataManager.Instance?.Get<RunTraitData>();
        int idx = 0;
        if (traitData != null)
        {
            foreach (var t in traitData.AcquiredTraits)
            {
                if (idx >= _traitIcons.Length) break;
                _traitIcons[idx]?.Setup(t);
                _traitIcons[idx]?.gameObject.SetActive(true);
                idx++;
            }
        }
        // 남은 슬롯 숨김
        for (; idx < _traitIcons.Length; idx++)
            _traitIcons[idx]?.gameObject.SetActive(false);
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

        // 빈 슬롯이 있으면 슬롯 자체가 고용 버튼 — 모두 찼을 때만 HireBtn 표시
        bool allFull = AreAllSlotsFull();
        _hireBtn.gameObject.SetActive(allFull);
        if (!allFull) return;

        int  cost   = GameplayConfig.Current.HireMercenaryCost;
        int  gold   = UserDataManager.Instance.Get<ItemData>().Get(eItem.Gold);
        bool canUse = gold >= cost;

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
        int cost = GameplayConfig.Current.HireMercenaryCost;
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
        popup.Setup(entry);
    }
}
