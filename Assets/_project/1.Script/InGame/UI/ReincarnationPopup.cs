using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  ReincarnationPopup.cs
//  패배 시 환생 전용 팝업.
//  초상화·스탯바·어빌리티 아이콘 스트립을 포함한 런 요약 화면.
//
//  레이아웃 (위 → 아래):
//    "패배" 제목 / 웨이브·처치 / 총피해·DPS / 어빌리티 스트립(HScroll)
//    딜·탱·힐 탭 / 장수별 StatBar 목록(VScroll)
//    포인트 패널 / 환생 버튼
// ============================================================

public class ReincarnationPopup : PopupBase
{
    public override bool BlockBackgroundClose => true;

    [SerializeField] TextMeshProUGUI _subText;
    [SerializeField] TextMeshProUGUI _statsText;

    [Header("어빌리티 스트립")]
    [SerializeField] Transform _abilityIconContent;   // HLG + CSF, 런타임 생성

    [Header("통계 탭")]
    [SerializeField] Button[] _tabButtons;            // 0=딜, 1=탱, 2=힐
    [SerializeField] Image[]  _tabButtonBgs;

    [Header("장수 목록")]
    [SerializeField] Transform        _generalArea;          // ScrollRect content (VLG)
    [SerializeField] GeneralStatRowUI _generalRowTemplate;   // 비활성 프리팹

    [Header("환생 포인트")]
    [SerializeField] TextMeshProUGUI _currentPtsText;
    [SerializeField] TextMeshProUGUI _earnPtsText;
    [SerializeField] TextMeshProUGUI _totalPtsText;
    [SerializeField] Button          _reincarnateBtn;

    int           _earnPoints;
    CombatStatTab _currentTab = CombatStatTab.Damage;
    BattleContext _context;
    readonly List<GeneralStatRowUI> _generalRows = new();

    static readonly Color TabActiveColor   = new Color(0.25f, 0.45f, 0.85f);
    static readonly Color TabInactiveColor = new Color(0.15f, 0.18f, 0.28f);

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _reincarnateBtn.onClick.AddListener(OnReincarnate);

        if (_tabButtons == null) return;
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            if (_tabButtons[i] == null) continue;
            int captured = i;
            _tabButtons[i].onClick.AddListener(() => OnTabClicked((CombatStatTab)captured));
        }
    }

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(BattleContext context, int killCount)
    {
        _context    = context;
        _currentTab = CombatStatTab.Damage;

        int   currentWave = context?.CurrentWave ?? 0;
        int   totalWaves  = context?.TotalWaves  ?? 0;
        float elapsedSec  = context?.BattleElapsedSeconds ?? 0f;

        _subText.text = $"웨이브 {currentWave} / {totalWaves}  ·  처치 {killCount}명";

        float totalDmg = 0f;
        if (context?.CombatStats != null && context.CombatStats.Count > 0)
            foreach (var e in context.CombatStats) totalDmg += e.TotalDamageDealt;
        else if (BattleStatsTracker.Instance != null)
            foreach (var e in BattleStatsTracker.Instance.GetAllEntries()) totalDmg += e.TotalDamageDealt;

        float dps = elapsedSec > 0f ? totalDmg / elapsedSec : 0f;
        _statsText.text = $"총 피해  {FormatNum(totalDmg)}  |  DPS  {FormatNum(dps)}";

        BuildAbilityStrip();
        BuildGeneralRows(context);
        RefreshTabHighlight();

        var reincarData = UserDataManager.Instance?.Get<ReincarnationData>();
        var progress    = UserDataManager.Instance?.Get<StageProgressData>();
        int cleared     = progress?.ClearedNormalStages ?? 0;
        _earnPoints     = ReincarnationData.CalculateReincarnationPoints(cleared);
        int current     = reincarData?.ReincarnationPoints ?? 0;

        _currentPtsText.text = $"보유  {current} pt";
        _earnPtsText.text    = $"+{_earnPoints} pt";
        _totalPtsText.text   = $"환생 후  {current + _earnPoints} pt";
    }

    // ── 어빌리티 스트립 ────────────────────────────────────────

    void BuildAbilityStrip()
    {
        if (_abilityIconContent == null) return;
        foreach (Transform child in _abilityIconContent) Destroy(child.gameObject);

        var runAbility = UserDataManager.Instance?.Get<RunAbilityData>();
        if (runAbility == null) return;
        var db = AbilityDatabase.Current;
        if (db == null) return;

        foreach (var id in runAbility.OwnedAbilityIds)
        {
            var data = db.Get(id);
            if (data == null) continue;
            CreateAbilityTile(data, runAbility.GetLevel(id));
        }
    }

    void CreateAbilityTile(AbilityData data, int level)
    {
        const float tileW = 76f;
        const float tileH = 76f;

        var tile = new GameObject(data.Id.ToString(), typeof(RectTransform), typeof(Image));
        tile.transform.SetParent(_abilityIconContent, false);
        tile.GetComponent<RectTransform>().sizeDelta = new Vector2(tileW, tileH);

        var gc = AbilityUIHelper.GradeColor(data.Grade);
        tile.GetComponent<Image>().color = gc * 0.25f + new Color(0.06f, 0.07f, 0.12f, 1f);

        // 아이콘 (하단 이름 영역 제외한 나머지)
        if (data.Icon != null)
        {
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(tile.transform, false);
            var rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 20f); rt.offsetMax = new Vector2(-4f, -4f);
            var img = iconGo.GetComponent<Image>();
            img.sprite = data.Icon; img.preserveAspect = true; img.color = Color.white;
        }

        // 이름 (하단 20px)
        var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(tile.transform, false);
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(1f, 0f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.anchoredPosition = new Vector2(0f, 2f);
        nameRt.sizeDelta = new Vector2(0f, 18f);
        var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text = data.AbilityName; nameTmp.fontSize = 18f;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.color = Color.white;

        // 레벨 뱃지 (좌상단, MaxLevel>1 일 때만)
        if (data.MaxLevel > 1)
        {
            var lvGo = new GameObject("Lv", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvGo.transform.SetParent(tile.transform, false);
            var lvRt = lvGo.GetComponent<RectTransform>();
            lvRt.anchorMin = new Vector2(0f, 1f); lvRt.anchorMax = new Vector2(0f, 1f);
            lvRt.pivot = new Vector2(0f, 1f);
            lvRt.anchoredPosition = new Vector2(2f, -2f);
            lvRt.sizeDelta = new Vector2(40f, 20f);
            var lvTmp = lvGo.GetComponent<TextMeshProUGUI>();
            lvTmp.text = $"Lv{level}";
            lvTmp.fontSize = 17f; lvTmp.fontStyle = FontStyles.Bold;
            lvTmp.alignment = TextAlignmentOptions.Left;
            lvTmp.color = level >= data.MaxLevel
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(0.7f, 0.85f, 1f);
        }
    }

    // ── 장수 행 ───────────────────────────────────────────────

    void BuildGeneralRows(BattleContext context)
    {
        foreach (var row in _generalRows)
            if (row != null) Destroy(row.gameObject);
        _generalRows.Clear();

        if (_generalArea == null || _generalRowTemplate == null) return;

        IEnumerable<GeneralStatEntry> entries =
            (context?.CombatStats != null && context.CombatStats.Count > 0)
                ? (IEnumerable<GeneralStatEntry>)context.CombatStats
                : BattleStatsTracker.Instance?.GetAllEntries();

        if (entries == null) return;

        float elapsedSec = context?.BattleElapsedSeconds ?? 0f;
        float maxValue   = CalcMaxValue(_currentTab);
        foreach (var e in entries)
        {
            var row = Instantiate(_generalRowTemplate, _generalArea);
            row.gameObject.SetActive(true);
            row.Setup(e.GeneralName);
            row.SetStats(e);
            row.SetDPS(elapsedSec);
            row.RefreshTab(_currentTab, maxValue);
            _generalRows.Add(row);
        }
    }

    // ── 탭 전환 ──────────────────────────────────────────────

    void OnTabClicked(CombatStatTab tab)
    {
        if (_currentTab == tab) return;
        _currentTab = tab;
        RefreshTabHighlight();
        RefreshAllRowBars();
    }

    void RefreshTabHighlight()
    {
        if (_tabButtonBgs == null) return;
        for (int i = 0; i < _tabButtonBgs.Length; i++)
        {
            if (_tabButtonBgs[i] == null) continue;
            _tabButtonBgs[i].color = (int)_currentTab == i ? TabActiveColor : TabInactiveColor;
        }
    }

    void RefreshAllRowBars()
    {
        float maxValue = CalcMaxValue(_currentTab);
        foreach (var row in _generalRows)
            if (row != null) row.RefreshTab(_currentTab, maxValue);
    }

    float CalcMaxValue(CombatStatTab tab)
    {
        IEnumerable<GeneralStatEntry> entries =
            (_context?.CombatStats != null && _context.CombatStats.Count > 0)
                ? (IEnumerable<GeneralStatEntry>)_context.CombatStats
                : BattleStatsTracker.Instance?.GetAllEntries();

        if (entries == null) return 1f;

        float max = 0f;
        foreach (var s in entries)
        {
            float v = tab switch
            {
                CombatStatTab.Damage => s.TotalDamageDealt,
                CombatStatTab.Tank   => s.DamageTaken + s.SoldierDamageTaken + s.DamageAbsorbed,
                CombatStatTab.Heal   => s.HealingDone,
                _                    => 0f,
            };
            if (v > max) max = v;
        }
        return max > 0f ? max : 1f;
    }

    // ── 환생 버튼 ─────────────────────────────────────────────

    void OnReincarnate()
    {
        var udm = UserDataManager.Instance;

        // 영구 데이터 — 환생 포인트 적립
        var reincarData = udm?.Get<ReincarnationData>();
        reincarData?.EarnPoints(_earnPoints);
        reincarData?.ResetOnReincarnation();

        // 런 초기화 — 어빌리티·특성·스테이지 진행
        udm?.Get<RunAbilityData>()?.SetDefaults();
        udm?.Get<RunTraitData>()?.SetDefaults();
        udm?.Get<StageProgressData>()?.SetDefaults();  // RunInProgress=false → MainPanel으로 이동

        // 장수 정보 초기화
        udm?.Get<UnitData>()?.SetDefaults();
        udm?.Get<DeploymentData>()?.SetDefaults();

        // 런 장비 인벤토리 초기화 (런 중 획득한 장비는 환생 시 소멸)
        udm?.Get<EquipInventoryData>()?.SetDefaults();

        // 골드만 0으로 초기화 (젬 등 영구 재화는 유지)
        var items = udm?.Get<ItemData>();
        if (items != null)
        {
            int gold = items.Get(eItem.Gold);
            if (gold > 0) items.Spend(eItem.Gold, gold);
        }

        // SaveAll: 씬 전환 전 즉시 저장 (RequestSave는 다음 프레임 실행 → 씬 전환 시 누락 가능)
        udm?.SaveAll();
        Close(() => LobbyManager.Instance.ReturnToLobby());
    }

    static string FormatNum(float v)
    {
        if (v >= 1_000_000f) return $"{v / 1_000_000f:0.0}M";
        if (v >= 1_000f)     return $"{v / 1_000f:0.0}K";
        return $"{(int)v}";
    }
}
