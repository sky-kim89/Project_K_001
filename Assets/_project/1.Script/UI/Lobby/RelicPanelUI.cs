using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  RelicPanelUI.cs
//  로비 유물 탭 — 카드 그리드(7열) + 스테이지 기반 환생 시스템.
//
//  정렬: 스탯 유물 → 재화 유물 → 어빌리티 유물
//
//  Inspector 연결:
//    _pointText       : 환생 포인트 숫자 TMP (헤더 아이콘 옆)
//    _scrollContent   : GridLayoutGroup 부모 Transform (Content)
//    _cardTemplate    : 비활성 카드 GO (런타임 복사 대상)
//    _reincarnateBtn  : 즉시 환생 버튼
//    _reincLabel      : 버튼 우측 텍스트 TMP ("{pts}pt 획득")
// ============================================================

public class RelicPanelUI : MonoBehaviour
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

    [Header("뒤로가기")]
    [SerializeField] Button _backBtn;

    // ── 캐시 ──────────────────────────────────────────────────
    RelicInventoryData _inventory;
    ReincarnationData  _reincData;
    RelicDatabase      _db;
    StageProgressData  _stageData;

    // ── 생명주기 ──────────────────────────────────────────────

    void OnEnable()
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

        if (_backBtn != null)
        {
            _backBtn.onClick.RemoveAllListeners();
            _backBtn.onClick.AddListener(() =>
                GetComponentInParent<LobbyNavUI>().Switch(5)); // MainPanel(5)
        }

        Refresh();
    }

    // ── 전체 갱신 ─────────────────────────────────────────────

    public void Refresh()
    {
        UpdatePointText();
        UpdateReincarnateBtn();
        RebuildGrid();
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
        int  pts      = ReincarnationData.CalculateReincarnationPoints(cleared);

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

    void RebuildGrid()
    {
        if (_scrollContent == null || _cardTemplate == null) return;

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
            descTmp.text = data.GetDescription(Mathf.Max(level, 1));

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

        _inventory.LevelUp(id, maxLevel);
        UserDataManager.Instance?.RequestSave();
        Refresh();
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
