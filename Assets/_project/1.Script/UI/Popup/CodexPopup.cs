using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  CodexPopup.cs
//  도감 — 장비·어빌리티·특성·장수를 한 번이라도 획득했는지 보여 준다.
//
//  ■ 구조
//    탭 4개 + 격자 하나. 전체 화면.
//    칸을 누르면
//      · 장비·어빌리티·특성 → InfoTooltipUI (이름/설명/스탯)
//      · 장수               → HeroDetailPopup 프리뷰 (자세히 보기와 같은 화면)
//    미보유 칸은 눌러도 아무 일도 없다 — 안 만나 본 것의 정보를 주면 도감이 아니다.
//
//  ⚠ 장수는 툴팁이 아니라 팝업이다
//    장수 정보는 스탯 9종 + 스킬 + 병사까지 붙어 툴팁 3줄에 안 들어간다.
//    상점 미리보기(SetupPreview)와 같은 화면을 그대로 쓴다 — 두 곳이 다르면
//    같은 장수가 화면마다 다르게 보인다.
//
//  ⚠ 미보유 칸도 목록에서 빼지 않는다
//    칸이 있어야 얼마나 남았는지 보인다. 빼면 도감이 아니라 보유함이다.
//
//  사용법:
//    PopupManager.Instance.Open<CodexPopup>(PopupType.Codex);
// ============================================================

public class CodexPopup : PopupBase
{
    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _progressTmp;   // "수집 84 / 439"
    [SerializeField] TextMeshProUGUI _bonusTmp;      // "공격력·체력 +42.0%"

    [Header("탭 (장비·어빌리티·특성·장수 순)")]
    [SerializeField] Button[]          _tabButtons;
    [SerializeField] TextMeshProUGUI[] _tabLabels;
    [SerializeField] Image[]           _tabBodies;   // 선택 표시용 배경

    [Header("격자")]
    [SerializeField] RecycleGridScroll _grid;   // 보이는 만큼만 만들어 돌려 쓴다

    [Header("정보 툴팁")]
    [SerializeField] InfoTooltipUI _tooltip;

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    // 셀 자식 이름 — Creator 가 만드는 구조와 반드시 일치해야 한다
    const string FramePath = "Frame";
    const string FillPath  = "Frame/Fill";
    const string IconPath  = "Frame/Fill/Icon";
    const string NamePath  = "Frame/Fill/Name";
    const string SubPath   = "Frame/Fill/Sub";

    static readonly Color LockedAccent = new Color(0.16f, 0.18f, 0.26f);
    static readonly Color LockedFill   = new Color(0.075f, 0.082f, 0.125f);
    static readonly Color OwnedFill    = new Color(0.125f, 0.140f, 0.215f);
    static readonly Color OwnedTextC   = new Color(0.92f, 0.95f, 1.00f);
    static readonly Color LockedTextC  = new Color(0.34f, 0.37f, 0.47f);

    CodexCategory _tab = CodexCategory.Equipment;

    // 현재 탭의 목록. 셀이 재사용되므로 인덱스로 다시 꺼내 쓴다.
    readonly List<CodexEntry> _entries = new();

    // 셀 → 지금 그 칸이 맡고 있는 장수 이름. 늦게 도착한 초상화가
    // 이미 다른 장수로 바뀐 칸에 박히는 걸 막는 표식이다.
    readonly Dictionary<GameObject, string> _cellNames = new();

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        _closeBtn?.onClick.AddListener(() => Close());

        if (_tabButtons != null)
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var cat = (CodexCategory)i;   // 배열 순서 = enum 순서
                if (_tabButtons[i] == null) continue;
                _tabButtons[i].onClick.AddListener(() => SelectTab(cat));
            }
        }

    }

    protected override void OnBeforeOpen()
    {
        // 지난번에 수확 모드로 열렸을 수 있다 — 팝업은 풀에서 재사용된다.
        _gainEntries = null;

        if (_tabButtons != null)
            foreach (var b in _tabButtons)
                if (b != null) b.gameObject.SetActive(true);
    }

    protected override void OnAfterOpen()
    {
        // ⚠ 수확 모드면 탭 화면으로 되돌리지 않는다
        //   OpenRoutine 이 열기 애니메이션 뒤에 이걸 부르는데, SetupRunGains 는
        //   Open() 이 돌아온 직후(=애니메이션 전)에 불린다. 여기서 SelectTab 을
        //   그냥 부르면 방금 깔아 둔 수확 목록이 평소 도감으로 덮인다.
        if (_gainEntries != null) { ShowGains(); return; }

        SelectTab(_tab);
    }

    protected override void OnAfterClose() => CloseTooltip();

    // ── 이번 여정 수확 모드 ──────────────────────────────────
    //
    //  ⚠ 도감 버프는 여정 경계에서 한 번에 들어온다 (CodexData.LockForRun)
    //    그래서 환생 직후 장수가 갑자기 세져 있는데 이유가 화면 어디에도 없었다.
    //    "이번 여정에 새로 채운 것 + 그래서 오른 공격력·체력" 을 한 화면에 편다.
    //
    //  도감 화면을 그대로 쓴다 — 칸·아이콘·툴팁이 이미 여기 다 있고,
    //  목록만 갈아 끼우면 되는 일에 화면을 하나 더 만들 이유가 없다.

    List<CodexEntry> _gainEntries;   // null = 평소 도감

    /// <summary>환생 뒤 "이번 여정에 새로 채운 것" 만 한 번에 뿌린다.</summary>
    public void SetupRunGains(CodexRunGains gains)
    {
        _gainEntries = CodexCatalog.BuildGains(gains);
        ShowGains();
    }

    void ShowGains()
    {
        if (_tooltip != null && _tooltip.IsOpen) _tooltip.Close();

        // 탭은 끈다 — 지금 화면은 "이번에 채운 것" 하나뿐이라 고를 것이 없다.
        if (_tabButtons != null)
            foreach (var b in _tabButtons)
                if (b != null) b.gameObject.SetActive(false);

        int   count = _gainEntries.Count;
        float after = CodexApplier.BonusRatio * 100f;
        float delta = count * CodexData.BonusPerEntry * 100f;

        if (_progressTmp != null)
            _progressTmp.text = $"이번 여정 수확 <color=#{StatBonusColors.Codex}>{count}</color>종";

        if (_bonusTmp != null)
            _bonusTmp.text = $"공격력·체력 +{after - delta:F1}% → " +
                             $"<color=#{StatBonusColors.Codex}>+{after:F1}%</color>";

        _entries.Clear();
        _entries.AddRange(_gainEntries);
        _grid?.Bind(_entries.Count, BindCell);
        _grid?.ScrollToTop();
    }

    // ── 탭 ───────────────────────────────────────────────────

    void SelectTab(CodexCategory category)
    {
        _tab = category;

        if (_tooltip != null && _tooltip.IsOpen) _tooltip.Close();

        RefreshHeader();
        RefreshTabs();
        RefreshGrid();

        // 탭을 바꾸면 항상 맨 위부터 — 400칸짜리 장수 탭에서 중간부터 보이면 당황스럽다
        _grid?.ScrollToTop();
    }

    void RefreshTabs()
    {
        if (_tabLabels == null) return;

        for (int i = 0; i < _tabLabels.Length; i++)
        {
            var cat        = (CodexCategory)i;
            var (owned, t) = CodexCatalog.Progress(cat);
            bool active    = cat == _tab;

            if (_tabLabels[i] != null)
            {
                _tabLabels[i].text  = $"{CodexCatalog.Label(cat)}  <size=80%>{owned}/{t}</size>";
                _tabLabels[i].color = active ? Color.white : LockedTextC;
            }

            if (_tabBodies != null && i < _tabBodies.Length && _tabBodies[i] != null)
                _tabBodies[i].color = active
                    ? CodexTabColors.TabActive
                    : CodexTabColors.TabInactive;
        }
    }

    void RefreshHeader()
    {
        var (owned, total) = CodexCatalog.TotalProgress();

        if (_progressTmp != null)
            _progressTmp.text = $"수집 <color=#{StatBonusColors.Codex}>{owned}</color> / {total}";

        if (_bonusTmp != null)
        {
            // 실제로 걸리는 값과 같은 출처를 쓴다 — 여기서 따로 계산하면 표시만 어긋난다
            float pct     = CodexApplier.BonusRatio  * 100f;
            float pending = CodexApplier.PendingRatio * 100f;

            _bonusTmp.text = $"공격력·체력 <color=#{StatBonusColors.Codex}>+{pct:F1}%</color>";

            // 여정 중에 채운 몫은 다음 여정부터 붙는다 — 안 적어 두면
            // "도감을 채웠는데 스탯이 그대로" 로 보인다.
            if (pending > pct + 0.01f)
                _bonusTmp.text += $"  <size=75%>(다음 여정 +{pending:F1}%)</size>";
        }
    }

    // ── 격자 ─────────────────────────────────────────────────

    void RefreshGrid()
    {
        if (_grid == null) return;

        _entries.Clear();
        _entries.AddRange(CodexCatalog.Build(_tab));

        // 400칸을 다 만들지 않는다 — 화면에 걸치는 만큼만 만들고 돌려 쓴다
        _grid.Bind(_entries.Count, BindCell);
    }

    // ⚠ 셀은 재사용된다 — 모든 상태를 매번 덮어써야 한다
    //   "보유일 때만" 칠하는 식으로 두면 그 칸이 미보유 항목으로 넘어갈 때
    //   이전 색·아이콘·리스너가 그대로 남는다.
    void BindCell(int index, GameObject cell)
    {
        if (index < 0 || index >= _entries.Count) return;

        var entry = _entries[index];

        // 장수 칸이 아니면 표식을 지운다 — 안 지우면 탭을 바꿔도
        // 옛 요청이 "아직 유효" 로 판정돼 엉뚱한 칸에 얼굴이 박힌다.
        if (string.IsNullOrEmpty(entry.GeneralName)) _cellNames.Remove(cell);

        Paint(cell, entry);

        if (!cell.TryGetComponent<Button>(out var btn)) return;

        btn.onClick.RemoveAllListeners();
        // 미보유는 누를 게 없다 — 버튼을 꺼서 눌리는 느낌도 주지 않는다
        btn.interactable = entry.Owned;

        if (!entry.Owned) return;

        var owner = cell.GetComponent<RectTransform>();
        btn.onClick.AddListener(() => OnCellClicked(entry, owner));
    }

    void Paint(GameObject cell, CodexEntry entry)
    {
        var frameTr = cell.transform.Find(FramePath);
        var fillTr  = cell.transform.Find(FillPath);
        var iconTr  = cell.transform.Find(IconPath);
        var nameTr  = cell.transform.Find(NamePath);

        // 테두리 — 칸 사이 간격이 보이려면 이게 있어야 한다.
        // 보유는 등급색, 미보유는 눌러 죽인 회색.
        if (frameTr != null && frameTr.TryGetComponent<Image>(out var frame))
            frame.color = entry.Owned ? entry.Accent : LockedAccent;

        if (fillTr != null && fillTr.TryGetComponent<Image>(out var fill))
            fill.color = entry.Owned ? OwnedFill : LockedFill;

        if (iconTr != null && iconTr.TryGetComponent<Image>(out var icon))
        {
            if (entry.Owned && !string.IsNullOrEmpty(entry.GeneralName))
                RequestPortrait(cell, icon, entry.GeneralName);
            else
            {
                // 아이콘이 없는 항목(미보유 장수 등)은 이미지를 끄고 이름만 남긴다
                bool show    = entry.Owned && entry.Icon != null;
                icon.sprite  = show ? entry.Icon : null;
                icon.enabled = show;
            }
        }

        if (nameTr != null && nameTr.TryGetComponent<TextMeshProUGUI>(out var tmp))
        {
            tmp.text  = entry.Owned ? entry.Name : "?";
            tmp.color = entry.Owned ? OwnedTextC : LockedTextC;
        }

        // 직업 · 등급 — 등급색을 그대로 쓴다. 테두리와 같은 색이라 눈이 바로 묶어 읽는다.
        var subTr = cell.transform.Find(SubPath);
        if (subTr != null && subTr.TryGetComponent<TextMeshProUGUI>(out var sub))
        {
            // 미보유는 등급까지 가린다 — 뭘 못 만났는지 알려주면 도감이 아니다
            sub.text  = entry.Owned ? (entry.SubLabel ?? "") : "";
            sub.color = entry.Accent;
        }
    }

    // ── 장수 초상화 ──────────────────────────────────────────
    //
    //  초상화는 런타임 합성물이라 즉시 나오지 않는다 (GeneralPortraitProvider 주석 참고).
    //  캐시에 있으면 그 자리에서, 없으면 몇 프레임 뒤에 채워진다.
    //
    //  ⚠ 셀이 재사용되므로 "지금도 그 장수인가" 를 두 번 확인한다
    //    ① StillWanted — 합성 직전. 화면 밖으로 나간 칸이면 아예 만들지 않는다.
    //    ② 콜백 안     — 합성하는 사이 스크롤로 다른 장수가 들어왔을 수 있다.
    //    이 확인이 없으면 스크롤을 굴릴 때 엉뚱한 칸에 남의 얼굴이 박힌다.

    void RequestPortrait(GameObject cell, Image icon, string unitName)
    {
        _cellNames[cell] = unitName;

        var cached = GeneralPortraitProvider.GetCached(unitName);
        if (cached != null)
        {
            icon.sprite  = cached;
            icon.enabled = true;
            return;
        }

        // 아직 없다 — 이전 장수의 얼굴이 남아 있지 않게 먼저 비운다
        icon.sprite  = null;
        icon.enabled = false;

        GeneralPortraitProvider.Request(
            unitName,
            stillWanted: () => cell != null && _cellNames.TryGetValue(cell, out var n) && n == unitName,
            onReady: sprite =>
            {
                if (cell == null || icon == null) return;
                if (!_cellNames.TryGetValue(cell, out var now) || now != unitName) return;

                icon.sprite  = sprite;
                icon.enabled = true;
            });
    }

    // ── 클릭 ─────────────────────────────────────────────────

    void OnCellClicked(CodexEntry entry, RectTransform owner)
    {
        if (!entry.Owned) return;

        // 장수 — 자세히 보기와 같은 화면을 띄운다
        if (!string.IsNullOrEmpty(entry.GeneralName))
        {
            OpenGeneralDetail(entry.GeneralName);
            return;
        }

        // 나머지 — 누른 칸 아래에 띄운다.
        //
        // ⚠ Show 가 아니라 ShowAnchored 다
        //   Show 는 툴팁을 누른 칸의 자식으로 옮긴다. 그러면 격자를 다시 그릴 때
        //   칸과 함께 툴팁까지 파괴돼 그 뒤로는 영영 뜨지 않는다.
        //   ShowAnchored 는 부모를 건드리지 않고 위치만 맞춘다.
        if (_tooltip != null)
            _tooltip.ShowAnchored(owner, entry.Name, entry.Desc, entry.StatLine);
    }

    void OpenGeneralDetail(string unitName)
    {
        // 보유 중인 장수면 실제 데이터를, 아니면 태생 등급 기준 프리뷰를 보여 준다.
        // (도감은 "예전에 만나 본" 장수도 담으므로 지금은 없을 수 있다)
        var owned = UserDataManager.Instance?.Get<UnitData>()?.GetUnit(unitName);

        // ⚠ GradeUpCount 는 반드시 0 이다
        //   Grade => min(BirthGrade + GradeUpCount, Epic) 이라
        //   Epic - BirthGrade 로 채우면 **전원 영웅**으로 보인다.
        //   MainPanelUI 의 후보 장수는 그 강제 Epic 이 의도된 사양이지만,
        //   도감은 "이 장수가 원래 어떤 등급인가" 를 보여 주는 곳이라 정반대다.
        //   (RunShopPopup·MercenaryShopPopup 도 같은 이유로 0 이다)
        var entry = owned ?? new UnitEntry
        {
            UnitName     = unitName,
            Level        = 1,
            Exp          = 0,
            GradeUpCount = 0,
        };

        var popup = PopupManager.Instance.Open<HeroDetailPopup>(PopupType.HeroDetail);
        if (popup == null) return;

        // 도감에서 연 건 관리 화면이 아니다 — 성장·장비·해고를 숨긴다
        popup.SetupPreview(entry);
    }

    // 툴팁은 셀 위치를 기준으로 떠 있다 — 목록이 바뀌면 가리키던 칸이 다른 항목이 된다
    void CloseTooltip()
    {
        if (_tooltip != null && _tooltip.IsOpen) _tooltip.Close();
    }
}

// 탭 색 — EditorUIBuilder.Pop 은 Editor 폴더에 있어 런타임에서 참조할 수 없다.
// 값이 달라지면 열었을 때와 탭을 바꿈 때 색이 튀니 같은 값을 유지할 것.
static class CodexTabColors
{
    public static readonly Color TabActive   = new Color(0.24f, 0.40f, 0.74f, 1f);
    public static readonly Color TabInactive = new Color(0.155f, 0.175f, 0.275f, 1f);
}
