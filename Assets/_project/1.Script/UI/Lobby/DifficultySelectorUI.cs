using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DifficultySelectorUI.cs
//  MainPanel 좌측 사이드 하단의 난이도 선택 패널.
//
//    난이도
//    ‹  [아이콘]  어려움  ›
//    적이 더 강해지고 수도 늘어난다.
//    [광포][물량]              ← 클릭하면 수치 툴팁
//
//  ■ 캐릭터 카드와 가로로 겹치지 않게 좌측에 둔다
//    처음엔 시작 버튼 위(카드와 같은 x)에 뒀는데 카드 하단의
//    '자세히 보기' 버튼과 겹쳐 둘 다 못 읽혔다.
//
//  ■ 팝업을 안 쓴다
//    난이도는 출전 직전에 확인하는 정보다. 팝업을 한 번 더 열게 하면
//    대부분 기본값으로 그냥 시작하고, 난이도 시스템이 있는 줄도 모른다.
//
//  ■ 디버프는 아이콘으로 보여주고 눌러서 자세히 본다
//    이름만 나열하면 '광포' 가 뭔지 알 수 없다. 특성과 같은 슬롯
//    (TraitIconUI)을 써서 툴팁 동작까지 똑같이 맞췄다.
//
//  ■ 런 도중에는 잠긴다
//    30스테이지를 쉬운 등급으로 깔고 마지막만 올리는 악용을 막는다.
// ============================================================

public class DifficultySelectorUI : MonoBehaviour
{
    [SerializeField] Image           _tierIcon;
    [SerializeField] TextMeshProUGUI _tierLabel;
    [SerializeField] TextMeshProUGUI _summaryLabel;   // 요약 설명
    [SerializeField] TextMeshProUGUI _rewardLabel;    // 환생 포인트 배율
    [SerializeField] TextMeshProUGUI _lockLabel;      // 잠김/런중 안내
    [SerializeField] TextMeshProUGUI _noDebuffLabel;  // 제약 없을 때 "없음"
    [SerializeField] Button          _prevBtn;
    [SerializeField] Button          _nextBtn;
    [SerializeField] TraitIconUI[]   _debuffIcons;    // 특성과 같은 슬롯 재사용
    [SerializeField] Image[]         _stepMarks;      // 5칸 단계 게이지

    static readonly Color[] TierColors =
    {
        new Color(0.62f, 0.66f, 0.74f),   // 쉬움   — 강철
        new Color(0.25f, 0.76f, 0.66f),   // 보통   — 청록
        new Color(1.00f, 0.63f, 0.20f),   // 어려움 — 주황
        new Color(1.00f, 0.27f, 0.13f),   // 지옥   — 적색
        new Color(0.71f, 0.34f, 1.00f),   // 불지옥 — 보라
    };

    void OnEnable()
    {
        _prevBtn?.onClick.RemoveAllListeners();
        _prevBtn?.onClick.AddListener(() => Step(-1));
        _nextBtn?.onClick.RemoveAllListeners();
        _nextBtn?.onClick.AddListener(() => Step(+1));

        DifficultyData.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable() => DifficultyData.OnChanged -= Refresh;

    // ── 조작 ──────────────────────────────────────────────────

    void Step(int delta)
    {
        var data = UserDataManager.Instance?.Get<DifficultyData>();
        if (data == null || RunLocked) return;

        int next = Mathf.Clamp((int)data.SelectedTier + delta, 0, data.MaxSelectableIndex);
        data.Select((DifficultyTier)next);
        UserDataManager.Instance.RequestSave();
    }

    static bool RunLocked =>
        UserDataManager.Instance?.Get<StageProgressData>()?.RunInProgress ?? false;

    // ── 표시 ──────────────────────────────────────────────────

    public void Refresh()
    {
        var data = UserDataManager.Instance?.Get<DifficultyData>();
        if (data == null) return;

        DifficultyTier tier = data.SelectedTier;
        Color          col  = TierColors[Mathf.Clamp((int)tier, 0, TierColors.Length - 1)];

        if (_tierLabel != null)
        {
            _tierLabel.text  = tier.Label();
            _tierLabel.color = col;
        }

        if (_tierIcon != null)
        {
            var sprites = SpriteManager.Instance;
            _tierIcon.sprite = sprites != null ? sprites.Get(tier.IconKey()) : null;
            _tierIcon.color  = _tierIcon.sprite != null ? Color.white : col * 0.6f;
        }

        if (_summaryLabel != null)
        {
            _summaryLabel.text  = tier.Summary();
            _summaryLabel.color = new Color(0.72f, 0.76f, 0.86f);
        }

        // 단계 게이지 — 현재 등급까지 색이 차오른다.
        // 잠긴 구간은 더 어둡게 해서 '어디까지 열렸는지' 도 같이 보인다.
        if (_stepMarks != null)
        {
            for (int i = 0; i < _stepMarks.Length; i++)
            {
                if (_stepMarks[i] == null) continue;
                _stepMarks[i].color =
                    i <= (int)tier                 ? col
                  : i <= data.MaxSelectableIndex   ? new Color(0.28f, 0.31f, 0.40f)
                  :                                  new Color(0.15f, 0.16f, 0.22f);
            }
        }

        var entry = DifficultyConfig.Current?.Get(tier);

        if (_rewardLabel != null)
        {
            float mul = entry?.ReincarnationMultiplier ?? 1f;
            _rewardLabel.text = $"환생 포인트  ×{mul:0.0#}";
            // 배율이 1이면 보상이 없다는 뜻이라 강조하지 않는다
            _rewardLabel.color = mul > 1.001f
                ? new Color(0.45f, 0.86f, 0.62f)
                : new Color(0.45f, 0.48f, 0.58f);
        }

        RefreshDebuffIcons(entry);

        // 잠금 안내 — 왜 못 올리는지 알려 준다.
        bool locked = RunLocked;
        bool atMax  = (int)tier >= data.MaxSelectableIndex;

        if (_lockLabel != null)
        {
            if (locked)
            {
                _lockLabel.text = "전투 중에는 바꿀 수 없습니다";
                _lockLabel.gameObject.SetActive(true);
            }
            else if (atMax && data.MaxSelectableIndex < TierColors.Length - 1)
            {
                var nextTier = (DifficultyTier)(data.MaxSelectableIndex + 1);
                _lockLabel.text = $"{tier.Label()} 완주 시 {nextTier.Label()} 해금";
                _lockLabel.gameObject.SetActive(true);
            }
            else
            {
                _lockLabel.gameObject.SetActive(false);
            }
        }

        if (_prevBtn != null) _prevBtn.interactable = !locked && (int)tier > 0;
        if (_nextBtn != null) _nextBtn.interactable = !locked && !atMax;
    }

    void RefreshDebuffIcons(DifficultyConfig.TierEntry entry)
    {
        if (_debuffIcons == null || _debuffIcons.Length == 0) return;

        var list    = entry?.ActiveDebuffs();
        var sprites = SpriteManager.Instance;

        int idx = 0;
        if (list != null)
        {
            foreach (var d in list)
            {
                if (idx >= _debuffIcons.Length) break;

                _debuffIcons[idx].SetupCustom(
                    sprites != null ? sprites.Get(d.IconKey()) : null,
                    d.Label(),
                    DifficultyConfig.Flavor(d),
                    entry.DescribeDebuff(d));
                _debuffIcons[idx].gameObject.SetActive(true);
                idx++;
            }
        }

        for (int i = idx; i < _debuffIcons.Length; i++)
            _debuffIcons[i].gameObject.SetActive(false);

        // 아이콘이 하나도 없으면 그 줄이 통째로 비어 레이아웃이 무너져 보인다
        if (_noDebuffLabel != null) _noDebuffLabel.gameObject.SetActive(idx == 0);
    }
}
