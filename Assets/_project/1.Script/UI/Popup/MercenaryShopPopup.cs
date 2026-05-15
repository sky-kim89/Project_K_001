using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  MercenaryShopPopup.cs
//  용병 상점 팝업.
//
//  [빈 슬롯 있음] CandidateView → 3명 랜덤 후보 카드
//    카드 클릭 → DetailView (스탯·스킬 확인)
//    [고용] → UnitData 추가 + 빈 슬롯 배치 + 팝업 닫기
//    [포기] → DetailView 닫고 CandidateView 복귀
//
//  [빈 슬롯 없음] SlotFullView → 현재 용병 목록 + 개별 [해고] 버튼
//
//  Inspector 연결:
//    _candidateView      : 후보 카드 뷰 GO
//    _candidateCards[0~2]: MercCandidateCardUI 3개
//    _detailView         : 상세 뷰 GO
//    _slotFullView       : 슬롯 꽉 찼을 때 뷰 GO
//    _fullList           : SlotFullView 안의 목록 부모 Transform
//    _fullRowTemplate    : 목록 행 GO (비활성 템플릿)
//    _detailGradeBorder, _detailPortraitBg, _detailPortraitImg,
//    _detailPortraitBridge, _detailNameText, _detailGradeText,
//    _detailJobText, _detailHpText, _detailAtkText, _detailDefText,
//    _detailSpdText, _detailSoldierText, _detailActiveSkillText,
//    _detailPassive0~2Text : 상세 뷰 필드들
//    _hireBtn, _passBtn, _passShardText : 하단 버튼
//    _closeBtn : 닫기 버튼
// ============================================================

public class MercenaryShopPopup : PopupBase
{
    [Header("뷰 컨테이너")]
    [SerializeField] GameObject _candidateView;
    [SerializeField] GameObject _detailView;
    [SerializeField] GameObject _slotFullView;

    [Header("후보 카드 (CandidateView)")]
    [SerializeField] MercCandidateCardUI[] _candidateCards;   // 3개

    [Header("상세 뷰 (DetailView)")]
    [SerializeField] Image                _detailGradeBorder;
    [SerializeField] Image                _detailPortraitBg;
    [SerializeField] Image                _detailPortraitImg;
    [SerializeField] UnitAppearanceBridge _detailPortraitBridge;
    [SerializeField] TextMeshProUGUI      _detailNameText;
    [SerializeField] TextMeshProUGUI      _detailGradeText;
    [SerializeField] TextMeshProUGUI      _detailJobText;
    [SerializeField] TextMeshProUGUI      _detailHpText;
    [SerializeField] TextMeshProUGUI      _detailAtkText;
    [SerializeField] TextMeshProUGUI      _detailDefText;
    [SerializeField] TextMeshProUGUI      _detailSpdText;
    [SerializeField] TextMeshProUGUI      _detailActiveSkillText;
    [SerializeField] TextMeshProUGUI      _detailPassive0Text;
    [SerializeField] TextMeshProUGUI      _detailPassive1Text;
    [SerializeField] TextMeshProUGUI      _detailPassive2Text;
    [SerializeField] Button          _hireBtn;
    [SerializeField] Button          _passBtn;
    [SerializeField] TextMeshProUGUI _passShardText;

    [Header("슬롯 꽉 찼을 때 뷰 (SlotFullView)")]
    [SerializeField] Transform  _fullList;
    [SerializeField] GameObject _fullRowTemplate;

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    // ── 런타임 상태 ──────────────────────────────────────────

    readonly List<UnitEntry>   _candidates = new();
    UnitEntry                  _selected;
    Texture2D                  _detailTexture;
    readonly List<GameObject>  _fullRows   = new();

    // ── 생명주기 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
        _hireBtn ?.onClick.AddListener(OnHire);
        _passBtn ?.onClick.AddListener(OnPass);
    }

    protected override void OnAfterOpen()
    {
        GenerateCandidates();
        RefreshView();
    }

    protected override void OnAfterClose()
    {
        ClearFullRows();
    }

    // ── 후보 생성 ─────────────────────────────────────────────

    void GenerateCandidates()
    {
        _candidates.Clear();
        var unitData = UserDataManager.Instance?.Get<UnitData>();
        if (unitData == null) return;

        // 미보유 이름 목록
        var pool  = unitData.GetAvailableNames();
        int count = Mathf.Min(3, pool.Count);

        // Fisher-Yates 부분 셔플로 3명 랜덤 픽
        for (int i = 0; i < count; i++)
        {
            int idx      = Random.Range(i, pool.Count);
            (pool[i], pool[idx]) = (pool[idx], pool[i]);
            _candidates.Add(new UnitEntry { UnitName = pool[i], Level = 1, Exp = 0 });
        }
    }

    // ── 뷰 전환 ──────────────────────────────────────────────

    void RefreshView()
    {
        int emptySlot = GetFirstEmptySlot();
        bool hasFreeSlot = emptySlot >= 0;

        if (_candidateView != null) _candidateView.SetActive(hasFreeSlot && _selected == null);
        if (_detailView    != null) _detailView   .SetActive(hasFreeSlot && _selected != null);
        if (_slotFullView  != null) _slotFullView .SetActive(!hasFreeSlot);

        if (hasFreeSlot)
        {
            if (_selected == null) BuildCandidateCards();
            else                   FillDetailView(_selected);
        }
        else
        {
            BuildFullList();
        }
    }

    void BuildCandidateCards()
    {
        if (_candidateCards == null) return;
        for (int i = 0; i < _candidateCards.Length; i++)
        {
            if (_candidateCards[i] == null) continue;
            if (i < _candidates.Count)
            {
                int capturedIdx = i;
                _candidateCards[i].gameObject.SetActive(true);
                _candidateCards[i].Setup(_candidates[i], () => SelectCandidate(_candidates[capturedIdx]));
            }
            else
            {
                _candidateCards[i].gameObject.SetActive(false);
            }
        }
    }

    void SelectCandidate(UnitEntry entry)
    {
        _selected = entry;
        RefreshView();
    }

    void FillDetailView(UnitEntry entry)
    {
        UnitJob        job    = UnitJobRoller.GetJob(entry.UnitName);
        HeroStatResult result = HeroStatResolver.Resolve(entry);
        Color          gc     = GradeStyle.GetColor(entry.Grade);

        if (_detailGradeBorder != null) _detailGradeBorder.color = gc;
        if (_detailGradeText   != null) { _detailGradeText.text  = GradeStyle.GetLabel(entry.Grade); _detailGradeText.color = gc; }
        if (_detailNameText    != null) _detailNameText.text    = entry.UnitName;
        if (_detailJobText     != null) _detailJobText.text     = JobStyle.GetLabel(job);
        if (_detailHpText  != null) _detailHpText.text  = $"{result.Total(StatType.MaxHp):N0}";
        if (_detailAtkText != null) _detailAtkText.text = $"{result.Total(StatType.Attack):N0}";
        if (_detailDefText != null) _detailDefText.text = $"{result.Total(StatType.Defense) * 100f:F0}%";
        if (_detailSpdText != null) _detailSpdText.text = $"{result.Total(StatType.MoveSpeed):F1}";

        FillDetailSkills(job, entry);

        int shards = CalcShards(entry);
        if (_passShardText != null) _passShardText.text = $"{shards} 조각";

        UnitPortraitHelper.Render(entry.UnitName, job, entry.Grade,
            _detailPortraitBridge, _detailPortraitBg, _detailPortraitImg, ref _detailTexture);
    }

    void FillDetailSkills(UnitJob job, UnitEntry entry)
    {
        var activeDb  = ActiveSkillDatabase.Current;
        var passiveDb = PassiveSkillDatabase.Current;

        if (_detailActiveSkillText != null && activeDb != null)
        {
            var activeId   = ActiveSkillRoller.Roll(entry.UnitName, job, activeDb);
            var activeData = activeDb.Get(activeId);
            _detailActiveSkillText.text = activeData != null ? activeData.SkillName : "-";
        }

        var (s0, s1, s2) = PassiveSkillRoller.Roll(entry.UnitName);
        int slotCount    = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
        PassiveSkillType[] passives  = { s0, s1, s2 };
        var                passTexts = new[] { _detailPassive0Text, _detailPassive1Text, _detailPassive2Text };

        for (int i = 0; i < passTexts.Length; i++)
        {
            if (passTexts[i] == null) continue;
            if (i < slotCount && passiveDb != null)
            {
                var pd = passiveDb.Get(passives[i]);
                passTexts[i].gameObject.SetActive(true);
                passTexts[i].text = pd != null ? pd.SkillName : "-";
            }
            else
            {
                passTexts[i].gameObject.SetActive(false);
            }
        }
    }

    // ── SlotFullView ──────────────────────────────────────────

    void BuildFullList()
    {
        ClearFullRows();
        if (_fullList == null || _fullRowTemplate == null) return;

        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        if (unitData == null) return;

        for (int i = 0; i < 5; i++)
        {
            string name = deployData?.GetUnitAt(i) ?? "";
            if (string.IsNullOrEmpty(name)) continue;
            var entry = unitData.GetUnit(name);
            if (entry == null) continue;

            var row = Instantiate(_fullRowTemplate, _fullList);
            row.SetActive(true);

            var nameT = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var jobT  = row.transform.Find("JobText") ?.GetComponent<TextMeshProUGUI>();
            var fireB = row.transform.Find("FireBtn") ?.GetComponent<Button>();

            UnitJob job = UnitJobRoller.GetJob(entry.UnitName);
            if (nameT != null) nameT.text = entry.UnitName;
            if (jobT  != null) jobT.text  = JobStyle.GetLabel(job);

            var gradeBar = row.transform.Find("GradeBar")?.GetComponent<Image>();
            if (gradeBar != null) gradeBar.color = GradeStyle.GetColor(entry.Grade);

            if (fireB != null)
            {
                var captured = entry;
                fireB.onClick.RemoveAllListeners();
                fireB.onClick.AddListener(() => FireMercenary(captured));
            }

            _fullRows.Add(row);
        }
    }

    void FireMercenary(UnitEntry entry)
    {
        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        var itemData   = UserDataManager.Instance?.Get<ItemData>();

        int shards = CalcShards(entry);
        itemData?.Add(eItem.SoldierShard, shards);
        deployData?.Undeploy(entry.UnitName);
        unitData?.RemoveUnit(entry.UnitName);
        UserDataManager.Instance?.RequestSave();

        _selected = null;
        GenerateCandidates();
        RefreshView();
    }

    // ── 고용 / 포기 ───────────────────────────────────────────

    void OnHire()
    {
        if (_selected == null) return;

        int emptySlot = GetFirstEmptySlot();
        if (emptySlot < 0) return;

        var unitData   = UserDataManager.Instance?.Get<UnitData>();
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();

        unitData?.AddUnit(_selected);
        deployData?.Deploy(_selected.UnitName, emptySlot);
        UserDataManager.Instance?.RequestSave();

        Close();
    }

    void OnPass()
    {
        _selected = null;
        RefreshView();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    int GetFirstEmptySlot()
    {
        var deployData = UserDataManager.Instance?.Get<DeploymentData>();
        if (deployData == null) return -1;
        for (int i = 0; i < 5; i++)
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) return i;
        return -1;
    }

    static int CalcShards(UnitEntry e)
    {
        int[] bases = { 5, 10, 20, 35, 60 };
        return bases[Mathf.Clamp((int)e.Grade, 0, bases.Length - 1)] + e.Level / 5;
    }

    void ClearFullRows()
    {
        foreach (var r in _fullRows)
            if (r != null) Destroy(r);
        _fullRows.Clear();
    }
}
