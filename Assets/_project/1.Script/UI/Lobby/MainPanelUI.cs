using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MainPanelUI.cs
//  런 시작 전 장수 선택 화면.
//
//  흐름:
//    1. OnEnable → 직업별 1명씩 4명의 후보 장수 생성
//    2. ◀ / ▶ 화살표로 후보를 한 명씩 슬라이드
//    3. 현재 페이지 = 자동 선택 (StartBtn 항상 활성)
//    4. "게임 시작" → RunStarter.BeginRun() 후 곧바로 1스테이지 전투 시작
//                     (BattlePanel 출전 화면은 거치지 않는다)
//
//  Inspector 연결 (MainPanelCreator 자동):
//    _card       : GeneralCandidateCardUI (단일 카드)
//    _relicBtn   : 유물 관리 버튼 (RelicPopup 을 위에 띄운다 — 이 화면은 유지)
//    _startBtn   : 게임 시작 버튼
//    _prevBtn    : ◀ 이전 화살표
//    _nextBtn    : ▶ 다음 화살표
//    _pageDots   : 페이지 점 인디케이터 × 4
//
//  후보 배치 순서:
//    [0] Knight  [1] Archer  [2] Mage  [3] ShieldBearer
// ============================================================

public class MainPanelUI : MonoBehaviour
{
    [Header("배경")]
    [SerializeField] Image _backgroundImage;

    [Header("단일 카드")]
    [SerializeField] GeneralCandidateCardUI _card;

    [Header("내비게이션")]
    [SerializeField] Button  _prevBtn;
    [SerializeField] Button  _nextBtn;
    [SerializeField] Image[] _pageDots;

    [Header("버튼")]
    [SerializeField] Button _relicBtn;
    [SerializeField] Button _codexBtn;
    [SerializeField] Button _startBtn;
    [SerializeField] Button _refreshBtn;

    int         _currentPage = 0;
    UnitEntry[] _candidates;

    /// <summary>
    /// 이 화면이 떴다 — 튜토리얼이 이 신호로 유물 안내를 건다.
    ///
    /// ⚠ 여기서 TryPlay 를 부르지 않는다
    ///   "언제 띄울지" 는 TutorialManager 가 통째로 소유한다 (트리거 한 곳 모으기).
    ///   패널은 "떴다" 만 알린다.
    /// </summary>
    public static event System.Action OnShown;

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()
    {
        // 배경 데모 전투 — 정적인 화면을 피한다. 실전과는 완전히 분리돼 있다.
        // 로비 배경(Background)과 이 패널의 BackgroundImage 를 내려야 전장이 비친다.
        SceneDirector.Ensure().RequestArenaBackdrop(true);
        LobbyDemoBattle.Ensure().Begin();

        _currentPage = 0;

        GenerateCandidates();
        RefreshCard();
        RefreshDots();

        if (_prevBtn != null)
        {
            _prevBtn.onClick.RemoveAllListeners();
            _prevBtn.onClick.AddListener(OnPrevClicked);
        }

        if (_nextBtn != null)
        {
            _nextBtn.onClick.RemoveAllListeners();
            _nextBtn.onClick.AddListener(OnNextClicked);
        }

        if (_relicBtn != null)
        {
            _relicBtn.onClick.RemoveAllListeners();
            // ⚠ 탭 전환(Switch)으로 열지 않는다
            //   탭은 이 패널을 끈다 → OnEnable 이 다시 돌아 후보가 새로 추첨되고
            //   고르던 장수가 사라진다. 팝업으로 덮어 이 화면을 그대로 살려 둔다.
            //   닫히면 유물 강화가 반영된 스탯으로 카드만 다시 그린다.
            _relicBtn.onClick.AddListener(() =>
                PopupManager.Instance.Open<RelicPopup>(PopupType.Relic)
                            .SetOnClose(RefreshCard));
        }

        if (_codexBtn != null)
        {
            _codexBtn.onClick.RemoveAllListeners();
            _codexBtn.onClick.AddListener(() =>
                PopupManager.Instance.Open<CodexPopup>(PopupType.Codex));
        }

        if (_startBtn != null)
        {
            _startBtn.onClick.RemoveAllListeners();
            _startBtn.onClick.AddListener(OnStartPressed);
            _startBtn.interactable = true;
        }

        if (_refreshBtn != null)
        {
            _refreshBtn.onClick.RemoveAllListeners();
            _refreshBtn.onClick.AddListener(RerollCandidate);
        }

        // 버튼을 다 묶은 뒤에 알린다 — 튜토리얼이 곧바로 유물 버튼을 누르게 한다.
        OnShown?.Invoke();
    }

    void OnDisable()
    {
        // 패널을 떠나면 판을 닫는다 — 로비 어딘가에서 유닛이 계속 싸우고 있으면 안 된다.
        LobbyDemoBattle.Instance?.End();
        SceneDirector.Instance?.RequestArenaBackdrop(false);
    }

    // ── 후보 생성 (직업별 1명) ────────────────────────────────

    void GenerateCandidates()
    {
        // 후보를 굴리는 규칙은 RunStarter 가 갖는다 — 자동 시작 경로와 같은 장수가 나와야 한다.
        _candidates = new UnitEntry[4];
        for (int i = 0; i < 4; i++)
            _candidates[i] = RunStarter.RollCandidate((UnitJob)i);
    }

    // ── 화살표 내비게이션 ─────────────────────────────────────

    void OnPrevClicked()
    {
        _currentPage = (_currentPage - 1 + 4) % 4;
        RefreshCard();
        RefreshDots();
    }

    void OnNextClicked()
    {
        _currentPage = (_currentPage + 1) % 4;
        RefreshCard();
        RefreshDots();
    }

    // ── 새로 고침 (현재 페이지 동일 직업에서 재추첨) ─────────────

    void RerollCandidate()
    {
        if (_candidates == null) return;

        UnitJob      currentJob  = UnitJobRoller.GetJob(_candidates[_currentPage].UnitName);
        string       currentName = _candidates[_currentPage].UnitName;
        var          allNames    = UserDataManager.Instance.Get<UnitData>().GetAvailableNames();

        var bucket = new List<string>();
        foreach (string nm in allNames)
            if (UnitJobRoller.GetJob(nm) == currentJob && nm != currentName)
                bucket.Add(nm);

        // 동일 직업 후보가 없으면 직업 무관 다른 이름에서 선택
        if (bucket.Count == 0)
        {
            foreach (string nm in allNames)
                if (nm != currentName) bucket.Add(nm);
        }

        if (bucket.Count == 0) return;

        string chosen = bucket[Random.Range(0, bucket.Count)];
        UnitGrade birth = UnitJobRoller.GetBirthGrade(chosen);
        _candidates[_currentPage] = new UnitEntry
        {
            UnitName     = chosen,
            Level        = 1,
            Exp          = 0,
            GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)birth),
        };
        RefreshCard();

        // HeroDetailPopup이 열려 있으면 새 캐릭터 정보 동기화
        PopupManager.Instance
            .Get<HeroDetailPopup>(PopupType.HeroDetail)
            ?.SetupPreview(_candidates[_currentPage]);
    }

    // ── 카드 / 도트 갱신 ──────────────────────────────────────

    void RefreshCard()
    {
        if (_card == null || _candidates == null) return;

        _card.Setup(
            index:    _currentPage,
            entry:    _candidates[_currentPage],
            onSelect: null,
            onDetail: e => PopupManager.Instance
                               .Open<HeroDetailPopup>(PopupType.HeroDetail, noBlocker: true)
                               .SetupPreview(e));

        _card.SetSelected(false);
    }

    void RefreshDots()
    {
        if (_pageDots == null) return;

        for (int i = 0; i < _pageDots.Length; i++)
        {
            if (_pageDots[i] == null) continue;

            bool active = (i == _currentPage);
            _pageDots[i].color = active
                ? Color.white
                : new Color(0.35f, 0.35f, 0.55f, 0.8f);

            var le = _pageDots[i].GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth  = active ? 14f : 10f;
                le.preferredHeight = active ? 14f : 10f;
            }
            var rt = _pageDots[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                float s = active ? 14f : 10f;
                rt.sizeDelta = new Vector2(s, s);
            }
        }
    }

    // ── 게임 시작 ─────────────────────────────────────────────

    void OnStartPressed()
    {
        if (_candidates == null) return;

        // 등록·배치·특성·시너지·저장은 전부 RunStarter 안에 있다
        RunStarter.BeginRun(_candidates[_currentPage]);

        PopupManager.Instance.Close(PopupType.HeroDetail);

        // ⚠ BattlePanel(출전 화면)을 거치지 않고 곧장 1스테이지로 들어간다
        //   장수를 막 고른 참이라 그 화면에서 더 정할 것이 없다 —
        //   한 번 더 누르게 하면 시작까지의 클릭만 늘어난다.
        LobbyManager.Instance.StartBattle();
    }
}
