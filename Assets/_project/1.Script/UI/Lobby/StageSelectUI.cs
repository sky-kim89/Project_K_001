using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  StageSelectUI.cs
//  BattlePanel 전체 컨트롤러.
//
//  좌측: 5개 DeploySlotUI (배치 슬롯 — 점유 칸을 누르면 장수 상세)
//        빈 칸은 "빈 자리" 표시만 하고 눌리지 않는다.
//        용병 고용은 런 상점(RunShopPopup)에서만 한다.
//  우측: 스테이지 정보 + 어빌리티 목록 버튼 + 장비 분해 버튼 + 전투 시작 버튼
//
//  특성 아이콘 목록은 여기 없다 — TopBar 의 TraitBarUI 가 담당한다.
//
//  상점·이벤트는 버튼이 없다. 해당 스테이지에 도착하면(=이 패널이 켜지면)
//  팝업이 자동으로 뜬다 — 스테이지당 한 번만. TryAutoOpenStagePopup() 참고.
//
//  상점 스테이지도 EventPopup 으로 연다. 상점 팝업이 예고 없이 뜨면
//  무슨 상황인지 읽히지 않아, "행상인의 좌판" 이벤트를 거쳐
//  '상품을 본다' 를 골랐을 때 RunShopPopup 이 열리게 했다.
//  (1스테이지는 RunSequenceGenerator 가 항상 일반으로 고정하므로
//   런 시작·환생 직후에는 여기서 아무것도 뜨지 않는다)
//
//  Inspector 연결:
//    _deploySlots[0~4] : DeploySlotUI 컴포넌트
//    _stageText        : "스테이지 N 도전" TMP
//    _progressText     : "N 스테이지 클리어" TMP
//    _abilityListBtn   : 어빌리티 목록 버튼
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

    [Header("버튼")]
    [SerializeField] Button          _abilityListBtn;
    [SerializeField] Button          _relicBtn;
    [SerializeField] Button          _disassembleBtn;
    [SerializeField] Button          _battleStartBtn;

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()
    {
        LobbyManager.OnStageChanged += OnStageChanged;
        BindButtons();
        Refresh();
        StartCoroutine(AutoOpenStagePopupNextFrame());
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

        _disassembleBtn?.onClick.RemoveAllListeners();
        _disassembleBtn?.onClick.AddListener(() =>
            PopupManager.Instance?.Open<DisassemblePopup>(PopupType.Disassemble));

        _battleStartBtn?.onClick.RemoveAllListeners();
        _battleStartBtn?.onClick.AddListener(() => LobbyManager.Instance?.StartBattle());
    }

    // ── 상점·이벤트 자동 오픈 ─────────────────────────────────

    /// <summary>
    /// 로비에 도착했을 때 현재 스테이지가 상점/이벤트면 팝업을 자동으로 띄운다.
    /// 한 프레임 미루는 이유: 이 패널은 LobbyNavUI.Switch() 가 켜므로
    /// OnEnable 시점엔 PopupManager 초기화가 끝나지 않았을 수 있다.
    /// </summary>
    IEnumerator AutoOpenStagePopupNextFrame()
    {
        yield return null;
        TryAutoOpenStagePopup();
    }

    void TryAutoOpenStagePopup()
    {
        var progress = UserDataManager.Instance.Get<StageProgressData>();
        var type     = progress.CurrentStageType;
        if (type != RunStageType.Shop && type != RunStageType.Event) return;

        // 스테이지당 한 번만 — 탭을 오갈 때마다 다시 뜨면 안 된다.
        if (progress.AutoPopupShownStage == progress.CurrentRunStage) return;
        progress.AutoPopupShownStage = progress.CurrentRunStage;
        UserDataManager.Instance.RequestSave();

        var db = EventDatabase.Current;
        OpenEventPopup(type == RunStageType.Shop
            ? db.Get(EventDatabase.ShopEventId)
            : db.GetRandom());
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

        // 시퀀스가 없거나(환생 후 씬 미리로드) 규칙에 어긋나면 즉시 다시 뽑는다
        if (progress != null && progress.EnsureRunSequence())
            UserDataManager.Instance.RequestSave();

        int stageIndex = progress?.CurrentRunStage ?? 0;

        // 스테이지 번호만 남긴다.
        // "도전"·스테이지 타입·"N / 30 스테이지" 는 전부 진행바가 이미 보여 주는
        // 정보라 중복이었다 — 화면 가운데를 비워 두는 쪽이 읽기 쉽다.
        if (_stageText     != null) _stageText.text = $"스테이지 {stageIndex + 1}";
        if (_progressText  != null) _progressText.gameObject.SetActive(false);
        if (_stageTypeText != null) _stageTypeText.gameObject.SetActive(false);

        // 런 진행바 갱신
        var seq = progress?.GetRunSequence();
        if (_progressBar != null && seq != null && seq.Length > 0)
            _progressBar.Refresh(seq, stageIndex);

        // 상점은 자동으로 열리므로 이 버튼은 항상 "전투 시작" 이다.
        var label = _battleStartBtn?.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = "전투 시작";
    }

    void RefreshSlots()
    {
        if (_deploySlots == null) return;
        int activeSlots = RelicApplier.GetTotalActiveGeneralSlots();
        for (int i = 0; i < _deploySlots.Length; i++)
        {
            _deploySlots[i]?.Setup(i,
                locked:     i >= activeSlots,
                onOccupied: (entry, slot) => OpenHeroDetail(entry, slot));
        }
    }

    // ── 팝업 열기 ─────────────────────────────────────────────

    void OpenHeroDetail(UnitEntry entry, int slot)
    {
        var popup = PopupManager.Instance.Open<HeroDetailPopup>(PopupType.HeroDetail);
        popup.SetOnClose(Refresh);
        popup.Setup(entry);
    }

    void OpenEventPopup(EventData evt)
    {
        if (evt == null)
        {
            Debug.LogError("[StageSelectUI] 이벤트를 찾지 못했습니다 — " +
                           "Tools > Project K > 데이터 생성 > 이벤트 를 실행하세요.");
            return;
        }

        var popup = PopupManager.Instance.Open<EventPopup>(PopupType.Event);
        popup.SetOnClose(Refresh);   // 이벤트 보상이 특성·슬롯을 바꾸므로 닫히면 갱신
        popup.Setup(evt)
             .SetupAbilityResources(
                 AbilityDatabase.Current,
                 UserDataManager.Instance.Get<RunAbilityData>(),
                 UserDataManager.Instance.Get<RelicInventoryData>(),
                 RelicDatabase.Current,
                 UserDataManager.Instance.Get<ReincarnationData>());
    }
}
