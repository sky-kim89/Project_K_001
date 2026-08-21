using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RelicPopup.cs
//  유물 강화 화면 — 카드 그리드 + 스테이지 기반 환생 시스템.
//
//  정렬: 스탯 유물 → 재화 유물 → 어빌리티 유물
//
//  ⚠ 예전엔 로비 탭(RelicPanel, index 3)이었다 — 팝업으로 옮겼다
//    탭은 서로를 끈다. 유물을 보고 돌아오면 MainPanel 이 꺼졌다 켜지면서
//    OnEnable 이 다시 돌아 **고르던 장수가 새로 추첨**됐다.
//    유물은 잠깐 들렀다 나오는 화면이라 아래 화면을 유지해야 한다.
//    → PopupManager.Instance.Open<RelicPopup>(PopupType.Relic)
//
//  Inspector 연결:
//    _pointText       : 환생 포인트 숫자 TMP (헤더 아이콘 옆)
//    _scrollContent   : GridLayoutGroup 부모 Transform (Content)
//    _cardTemplate    : 비활성 카드 GO (런타임 복사 대상)
//    _reincarnateBtn  : 즉시 환생 버튼
//    _reincLabel      : 버튼 우측 텍스트 TMP ("{pts}pt 획득")
// ============================================================

public class RelicPopup : PopupBase
{
    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _pointText;

    [Header("그리드")]
    [SerializeField] Transform  _scrollContent;
    [SerializeField] GameObject _cardTemplate;

    [Header("하단")]
    [SerializeField] Button          _reincarnateBtn;
    [SerializeField] TextMeshProUGUI _reincLabel;
    [SerializeField] Button          _resetBtn;

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    // ── 캐시 ──────────────────────────────────────────────────
    RelicInventoryData _inventory;
    ReincarnationData  _reincData;
    RelicDatabase      _db;
    StageProgressData  _stageData;

    // ── 생명주기 ──────────────────────────────────────────────

    // ⚠ 여는 시점마다 다시 묶는다 (Awake 가 아니라)
    //   PopupManager 는 닫힌 팝업을 풀에 넣어 재사용한다 — Awake 는 한 번뿐이다.
    //   반대로 유물 보유·포인트는 열 때마다 달라지므로 여기서 다시 읽어야 한다.
    protected override void OnBeforeOpen()
    {
        _inventory = UserDataManager.Instance?.Get<RelicInventoryData>();
        _reincData = UserDataManager.Instance?.Get<ReincarnationData>();
        _stageData = UserDataManager.Instance?.Get<StageProgressData>();
        _db        = RelicDatabase.Current;

        if (_reincarnateBtn != null)
        {
            _reincarnateBtn.onClick.RemoveAllListeners();
            _reincarnateBtn.onClick.AddListener(OnReincarnate);
        }

        if (_resetBtn != null)
        {
            _resetBtn.onClick.RemoveAllListeners();
            _resetBtn.onClick.AddListener(OnRelicReset);
        }

        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveAllListeners();
            _closeBtn.onClick.AddListener(() => Close());
        }

        Refresh();
    }

    // ── 전체 갱신 ─────────────────────────────────────────────

    public void Refresh()
    {
        UpdatePointText();
        UpdateReincarnateBtn();
        RebuildGrid();

        // 카드는 매번 새로 만들어진다 — PopupBase 가 연 직후 한 번 건 클릭음은
        // 강화 한 번이면 통째로 사라진다. 갱신할 때마다 다시 건다(중복은 알아서 건너뛴다).
        UIClickSfx.Bind(gameObject);
    }

    void UpdatePointText()
    {
        if (_pointText == null) return;
        int pts = _reincData?.ReincarnationPoints ?? 0;
        _pointText.text = pts.ToString();
    }

    void UpdateReincarnateBtn()
    {
        if (_reincarnateBtn == null) return;

        int  cleared  = _stageData?.ClearedNormalStages ?? 0;
        bool canReinc = ReincarnationData.CanReincarnate(cleared);
        int  pts      = ReincarnationData.PreviewPoints(cleared);   // 난이도 배율 포함

        _reincarnateBtn.interactable = canReinc;

        var leftText   = _reincarnateBtn.transform.Find("LeftText");
        var iconTr     = _reincarnateBtn.transform.Find("ReincPtIcon");
        var inactiveTr = _reincarnateBtn.transform.Find("InactiveText");

        if (leftText   != null) leftText.gameObject.SetActive(canReinc);
        if (iconTr     != null) iconTr.gameObject.SetActive(canReinc);
        if (inactiveTr != null) inactiveTr.gameObject.SetActive(!canReinc);

        if (_reincLabel != null)
        {
            _reincLabel.gameObject.SetActive(canReinc);
            if (canReinc)
                _reincLabel.text = $"{pts}pt 획득";
        }
    }

    // ── 카드 그리드 재구성 ────────────────────────────────────

    /// <summary>
    /// 유물별 카드 — 강화 연출을 걸 자리를 찾는 데 쓴다.
    ///
    /// ⚠ RebuildGrid 가 카드를 매번 새로 만든다
    ///   강화 직전에 잡아 둔 카드는 Refresh 가 끝나는 순간 파괴돼 있다.
    ///   그 참조에 연출을 걸면 펀치가 첫 프레임에 사라진다 — 새로 만들어진
    ///   카드를 다시 찾아야 플레이어가 보는 그 카드가 튄다.
    /// </summary>
    readonly Dictionary<RelicId, RectTransform> _cardByRelic = new();

    void RebuildGrid()
    {
        if (_scrollContent == null || _cardTemplate == null) return;

        _cardByRelic.Clear();

        for (int i = _scrollContent.childCount - 1; i >= 0; i--)
        {
            var child = _scrollContent.GetChild(i).gameObject;
            if (child != _cardTemplate) Destroy(child);
        }

        if (_db == null) return;

        var sorted = _db.GetAll()
            .Where(d => d != null)
            .OrderBy(d => (int)d.GetCategory())
            .ThenBy(d => (int)d.Rarity);

        foreach (var data in sorted)
        {
            var card = Instantiate(_cardTemplate, _scrollContent);
            card.SetActive(true);
            SetupCard(card, data);
            _cardByRelic[data.Id] = card.transform as RectTransform;
        }
    }

    // ── 카드 초기화 ───────────────────────────────────────────

    void SetupCard(GameObject card, RelicData data)
    {
        // 모든 유물은 0레벨로 존재한다 — "미보유" 상태는 없다.
        // owned 자리에 들어가던 판정은 "강화했는가(level > 0)" 로 바뀐다.
        int  level = _inventory?.GetLevel(data.Id) ?? 0;
        bool owned = level > 0;

        // 희귀도 상단 바
        var rarityBorder = card.transform.Find("RarityBorder")?.GetComponent<Image>();
        if (rarityBorder != null)
            rarityBorder.color = owned ? RelicStyle.GetColor(data.Rarity)
                                       : new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // 아이콘
        var iconImg = card.transform.Find("IconBg/IconImage")?.GetComponent<Image>();
        if (iconImg != null)
        {
            if (data.Icon != null)
            {
                iconImg.sprite = data.Icon;
                iconImg.color  = owned ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
            else
            {
                iconImg.sprite = null;
                iconImg.color  = owned
                    ? RelicStyle.GetColor(data.Rarity) * 0.6f
                    : new Color(0.25f, 0.25f, 0.25f, 0.5f);
            }
        }

        bool isInfinite = data.Rarity == RelicRarity.Common;

        // 레벨 뱃지 (아이콘 우하단) — 0레벨도 표시한다.
        // 0 이 보여야 "아직 아무 효과도 없다" 는 것이 드러난다.
        var levelBadgeTr = card.transform.Find("IconBg/LevelBadge");
        if (levelBadgeTr != null) levelBadgeTr.gameObject.SetActive(true);
        var levelTmp = levelBadgeTr?.GetComponentInChildren<TextMeshProUGUI>();
        if (levelTmp != null)
        {
            levelTmp.text = (!isInfinite && level >= data.MaxLevel) ? "MAX" : $"Lv.{level}";
        }

        // 이름
        var nameTmp = card.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTmp != null)
        {
            nameTmp.text  = data.RelicName;
            // 0레벨은 흐리게 — 다만 이름은 읽혀야 하므로 너무 어둡게 두지 않는다
            nameTmp.color = owned ? Color.white : new Color(0.68f, 0.70f, 0.76f);
        }

        // 설명 (0레벨이면 1레벨 미리보기 — 강화하면 뭐가 붙는지 보여준다)
        var descTmp = card.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
        if (descTmp != null)
        {
            // 스탯 창의 '유물' 색과 같은 보라 — 색만 보고 출처를 알 수 있게 한다
            descTmp.text  = data.GetDescription(Mathf.Max(level, 1));
            descTmp.color = StatBonusColors.RelicColor;
        }

        // 비용 / 버튼
        var costTmp    = card.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
        var upgradeBtn = card.transform.Find("UpgradeBtn")?.GetComponent<Button>();

        if (!isInfinite && level >= data.MaxLevel)
        {
            if (costTmp    != null) costTmp.text = "";
            if (upgradeBtn != null) { SetBtnLabel(upgradeBtn, "최대"); upgradeBtn.interactable = false; }
        }
        else
        {
            int  cost      = data.LevelUpCost(level);
            bool canAfford = (_reincData?.ReincarnationPoints ?? 0) >= cost;

            // 비용은 강화 버튼 위에 겹쳐 있다 (카드 직계 자식 "CostText").
            // 포인트가 모자라면 붉게 — 버튼이 왜 안 눌리는지 여기서 바로 보인다.
            if (costTmp != null)
            {
                costTmp.text  = $"{cost} pt";
                costTmp.color = canAfford ? new Color(1f, 0.85f, 0.35f)
                                          : new Color(1f, 0.48f, 0.45f);
            }
            if (upgradeBtn != null)
            {
                SetBtnLabel(upgradeBtn, "강화");
                upgradeBtn.interactable = canAfford;
                upgradeBtn.onClick.RemoveAllListeners();
                var capturedId   = data.Id;
                var capturedMax  = isInfinite ? int.MaxValue : data.MaxLevel;
                var capturedCost = cost;
                upgradeBtn.onClick.AddListener(() => TryLevelUp(capturedId, capturedMax, capturedCost));
            }
        }
    }

    static void SetBtnLabel(Button btn, string label)
    {
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
    }

    // ── 강화 ──────────────────────────────────────────────────

    void TryLevelUp(RelicId id, int maxLevel, int cost)
    {
        if (_reincData == null || _inventory == null) return;
        if (!_reincData.TrySpendPoints(cost)) return;

        // ⚠ 클릭 지점을 Refresh 전에 잡아 둔다
        //   Refresh 가 카드를 전부 다시 만드는데, 새 카드는 GridLayoutGroup 이
        //   프레임 끝에 정렬하기 전까지 템플릿 자리에 겹쳐 있다.
        //   그 카드 위치로 터뜨리면 어느 유물을 올려도 늘 같은 자리에서 튄다.
        Vector2 click = UIJuice.CapturePointer();

        _inventory.LevelUp(id, maxLevel);
        UserDataManager.Instance?.RequestSave();
        Refresh();

        // ⚠ 카드는 Refresh 뒤에 다시 찾는다 — 그 전 카드는 이미 파괴됐다
        //   자리는 click 이 책임지고, 카드는 펀치(어느 유물이 올랐는지)만 맡는다.
        //   펀치는 스케일이라 레이아웃이 늦게 잡혀도 어긋나지 않는다.
        if (_cardByRelic.TryGetValue(id, out var card))
            UIJuice.RelicUp(card, _inventory.GetLevel(id), click);
    }

    // ── 즉시 환생 ─────────────────────────────────────────────

    void OnReincarnate()
    {
        int cleared = _stageData?.ClearedNormalStages ?? 0;
        if (!ReincarnationData.CanReincarnate(cleared)) return;

        UserDataManager.Instance?.Reincarnate();
        LobbyManager.Instance?.ResetToFirstStage();
        Refresh();
    }

    // ── 유물 초기화 ───────────────────────────────────────────

    void OnRelicReset()
    {
        if (_inventory == null || _reincData == null) return;
        int refund = _inventory.ResetAll();
        _reincData.EarnPoints(refund);
        UserDataManager.Instance?.RequestSave();
        Refresh();
    }
}
