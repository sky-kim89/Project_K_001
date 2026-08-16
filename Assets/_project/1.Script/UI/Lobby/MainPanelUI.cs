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
//    4. "게임 시작" → 선택 장수를 UnitData에 등록, BattlePanel 전환
//
//  Inspector 연결 (MainPanelCreator 자동):
//    _card       : GeneralCandidateCardUI (단일 카드)
//    _relicBtn   : 유물 관리 버튼 (RelicPanel tab 3 이동)
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

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()
    {
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
            _relicBtn.onClick.AddListener(() =>
                GetComponentInParent<LobbyNavUI>()?.Switch(3));
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
    }

    // ── 후보 생성 (직업별 1명) ────────────────────────────────

    void GenerateCandidates()
    {
        _candidates = new UnitEntry[4];

        // 랜덤 폴백용 직업별 버킷 (프리셋 이름이 비어있는 슬롯에 사용)
        var allNames   = UserDataManager.Instance.Get<UnitData>().GetAvailableNames();
        var jobBuckets = new List<string>[4];
        for (int i = 0; i < 4; i++) jobBuckets[i] = new List<string>();
        foreach (string nm in allNames)
            jobBuckets[(int)UnitJobRoller.GetJob(nm)].Add(nm);

        var presets = GameplayConfig.Current.MainPanelCandidates;
        for (int i = 0; i < 4; i++)
        {
            string presetName = (presets != null && i < presets.Length) ? presets[i].Name : "";
            if (!string.IsNullOrEmpty(presetName))
            {
                UnitGrade birth = UnitJobRoller.GetBirthGrade(presetName);
                _candidates[i] = new UnitEntry
                {
                    UnitName     = presetName,
                    Level        = 1,
                    Exp          = 0,
                    GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)birth),
                };
            }
            else
            {
                string chosen = jobBuckets[i].Count > 0
                    ? jobBuckets[i][Random.Range(0, jobBuckets[i].Count)]
                    : allNames.Count > 0 ? allNames[Random.Range(0, allNames.Count)] : $"용사{i + 1}";
                UnitGrade birth = UnitJobRoller.GetBirthGrade(chosen);
                _candidates[i] = new UnitEntry
                {
                    UnitName     = chosen,
                    Level        = 1,
                    Exp          = 0,
                    GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)birth),
                };
            }
        }
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

        UnitEntry         selected   = _candidates[_currentPage];
        UnitData          unitData   = UserDataManager.Instance.Get<UnitData>();
        DeploymentData    deployData = UserDataManager.Instance.Get<DeploymentData>();
        StageProgressData progress   = UserDataManager.Instance.Get<StageProgressData>();

        if (!unitData.HasUnit(selected.UnitName))
            unitData.AddUnit(new UnitEntry
            {
                UnitName     = selected.UnitName,
                Level        = selected.Level,
                Exp          = selected.Exp,
                GradeUpCount = selected.GradeUpCount,
            });

        int emptySlot = 0;
        for (int i = 0; i < 5; i++)
        {
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) { emptySlot = i; break; }
        }

        deployData.Deploy(selected.UnitName, emptySlot);
        progress.RunInProgress = true;
        JobSynergyEvaluator.Recalculate();

        // 직업 특성 배정
        TraitType jobTrait = UnitJobRoller.GetJob(selected.UnitName) switch
        {
            UnitJob.Knight       => TraitType.KnightCommand,
            UnitJob.Archer       => TraitType.ArcherPrecision,
            UnitJob.Mage         => TraitType.MageArcane,
            UnitJob.ShieldBearer => TraitType.ShieldFortress,
            _                    => TraitType.None,
        };
        if (jobTrait != TraitType.None)
            UserDataManager.Instance.Get<RunTraitData>().AddTrait(jobTrait);

        PopupManager.Instance.Close(PopupType.HeroDetail);
        UserDataManager.Instance.SaveAll();

        GetComponentInParent<LobbyNavUI>().Switch(2); // BattlePanel
    }
}
