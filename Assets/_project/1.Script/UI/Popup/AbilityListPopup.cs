using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilityListPopup.cs
//  현재 런에서 보유 중인 어빌리티 목록 확인 팝업.
//
//  레이아웃 (좌우 분할):
//    ┌── Header ──────────────────────────────────────┐
//    │  LEFT (상세 + 총스탯)  │  RIGHT (보유 목록)    │
//    └────────────────────────┴───────────────────────┘
//
//  좌측:
//    - 선택된 어빌리티 상세 (아이콘/등급/이름/대상/효과)
//    - 보유 어빌리티 전체 스탯 합산
//  우측:
//    - RunAbilityData 보유 목록 (중복 시 ×N 뱃지)
//
//  사용법:
//    PopupManager.Instance.Open<AbilityListPopup>(PopupType.AbilityList);
// ============================================================

public class AbilityListPopup : PopupBase
{
    [Header("헤더")]
    [SerializeField] TextMeshProUGUI _headerCountTmp;   // "10개 보유"

    [Header("선택 어빌리티 상세 (좌)")]
    [SerializeField] Image           _infoIcon;
    [SerializeField] Image           _infoGradeBar;     // 등급 컬러 인디케이터 바
    [SerializeField] TextMeshProUGUI _infoGradeTmp;
    [SerializeField] TextMeshProUGUI _infoNameTmp;
    [SerializeField] TextMeshProUGUI _infoTargetTmp;
    [SerializeField] Transform       _infoStatContent;  // 스탯 행 부모
    [SerializeField] TextMeshProUGUI _infoStatTemplate; // 비활성 템플릿

    [Header("총 스탯 합산 (좌 하단)")]
    [SerializeField] Transform       _totalStatContent;
    [SerializeField] TextMeshProUGUI _totalStatTemplate;

    [Header("보유 목록 (우)")]
    [SerializeField] Transform       _listContent;      // ScrollRect content
    [SerializeField] GameObject      _listItemTemplate; // 비활성 아이템 템플릿

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    AbilityData               _selected;
    readonly List<GameObject> _listItems   = new();
    readonly List<GameObject> _infoRows    = new();
    readonly List<GameObject> _totalRows   = new();

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
    }

    protected override void OnAfterOpen()
    {
        BuildList();
        BuildTotalStats();
    }

    protected override void OnAfterClose()
    {
        ClearAll();
    }

    // ── 보유 목록 구성 ────────────────────────────────────────

    void BuildList()
    {
        ClearList();

        var runData = UserDataManager.Instance?.Get<RunAbilityData>();
        if (runData == null) return;

        var db = AbilityDatabase.Current;
        if (db == null) return;

        int uniqueCount = 0;
        AbilityData firstData = null;
        foreach (var id in runData.OwnedAbilityIds)
        {
            var data = db.Get(id);
            if (data == null) continue;
            uniqueCount++;
            firstData ??= data;

            var item = CreateListItem(data, runData.GetLevel(id));
            item.transform.SetParent(_listContent, false);
            _listItems.Add(item);
        }

        if (_headerCountTmp != null)
            _headerCountTmp.text = $"{uniqueCount}종 보유";

        if (firstData != null) SelectAbility(firstData);
    }

    GameObject CreateListItem(AbilityData data, int level)
    {
        GameObject go;
        if (_listItemTemplate != null)
        {
            go = Instantiate(_listItemTemplate, _listContent);
            go.SetActive(true);
        }
        else
        {
            go = new GameObject(data.Id.ToString(), typeof(RectTransform), typeof(Image));
        }
        go.name = data.Id.ToString();   // 하이라이트 조회용 — Instantiate 후 "(Clone)" 제거

        // 배경 등급 색
        var bg = go.GetComponent<Image>();
        if (bg != null) bg.color = AbilityUIHelper.GradeColor(data.Grade) * 0.25f + new Color(0.06f, 0.07f, 0.12f, 1f);

        // 아이콘
        var iconImg = go.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImg != null && data.Icon != null)
        { iconImg.sprite = data.Icon; iconImg.preserveAspect = true; iconImg.color = Color.white; }

        // 이름
        var nameTmp = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTmp != null) nameTmp.text = data.AbilityName;

        // 등급
        var gradeTmp = go.transform.Find("GradeText")?.GetComponent<TextMeshProUGUI>();
        if (gradeTmp != null)
        { gradeTmp.text = LocalizationManager.Instance.Get(data.Grade.ToString()); gradeTmp.color = AbilityUIHelper.GradeColor(data.Grade); }

        // 대상
        var targetTmp = go.transform.Find("TargetText")?.GetComponent<TextMeshProUGUI>();
        if (targetTmp != null) targetTmp.text = LocalizationManager.Instance.Get(data.Target.ToString());

        // Lv N/MaxN 레벨 뱃지
        var countGo  = go.transform.Find("CountBadge");
        var countTmp = countGo?.GetComponent<TextMeshProUGUI>();
        bool showLv  = data.MaxLevel > 1;
        if (countGo  != null) countGo.gameObject.SetActive(showLv);
        if (countTmp != null)
        {
            countTmp.text  = $"Lv {level}/{data.MaxLevel}";
            countTmp.color = level >= data.MaxLevel ? new Color(1f, 0.85f, 0.2f) : Color.white;
        }

        // 선택 버튼
        var btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        var captured = data;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SelectAbility(captured));

        return go;
    }

    // ── 선택 어빌리티 상세 ────────────────────────────────────

    void SelectAbility(AbilityData data)
    {
        _selected = data;
        RefreshDetail();
        RefreshHighlightColors(data.Id);
    }

    void RefreshDetail()
    {
        if (_selected == null) return;
        var d  = _selected;
        var gc = AbilityUIHelper.GradeColor(d.Grade);

        if (_infoIcon     != null) { _infoIcon.sprite = d.Icon; _infoIcon.enabled = d.Icon != null; }
        if (_infoGradeBar != null)   _infoGradeBar.color = gc;
        if (_infoGradeTmp != null) { _infoGradeTmp.text = LocalizationManager.Instance.Get(d.Grade.ToString()); _infoGradeTmp.color = gc; }
        if (_infoNameTmp  != null)   _infoNameTmp.text  = d.AbilityName;
        if (_infoTargetTmp!= null)   _infoTargetTmp.text = LocalizationManager.Instance.Get(d.Target.ToString());

        // 기존 스탯 행 제거
        foreach (var r in _infoRows) { if (r) Destroy(r); }
        _infoRows.Clear();

        if (_infoStatContent == null || _infoStatTemplate == null) return;

        if (d.Grade == AbilityGrade.Special || d.Grade == AbilityGrade.Mastery)
        {
            AddInfoRow($"<color=#888888>발동 조건</color>  {LocalizationManager.Instance.Get(d.GetTriggerType().ToString())}", gc);
            var desc = d.Description;
            if (!string.IsNullOrEmpty(desc))
                AddInfoRow($"<color=#888888>효과</color>  {desc}", Color.white);
        }
        else
        {
            var runData = UserDataManager.Instance?.Get<RunAbilityData>();
            int lv      = runData != null ? runData.GetLevel(d.Id) : 0;
            int maxLv   = d.MaxLevel;

            string LvTag() => lv > 0 ? $"  <color=#aaaaaa>Lv {lv}/{maxLv}</color>" : string.Empty;
            int    mult   = lv > 0 ? lv : 1;

            AddInfoRow($"<color=#888888>{LocalizationManager.Instance.Get(d.Stat1.ToString())}</color>  " +
                       StatBonusColors.Wrap(StatSource.Ability,
                           $"{AbilityUIHelper.FormatStatValue(d.Stat1, d.Value1 * mult)}{LvTag()}"), Color.white);
            if (d.HasStat2)
                AddInfoRow($"<color=#888888>{LocalizationManager.Instance.Get(d.Stat2.ToString())}</color>  " +
                           StatBonusColors.Wrap(StatSource.Ability,
                               $"{AbilityUIHelper.FormatStatValue(d.Stat2, d.Value2 * mult)}{LvTag()}"), Color.white);
        }
    }

    void AddInfoRow(string richText, Color color)
    {
        if (_infoStatTemplate == null || _infoStatContent == null) return;
        var go  = Instantiate(_infoStatTemplate.gameObject, _infoStatContent);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text  = richText;
        tmp.color = color;
        go.SetActive(true);
        _infoRows.Add(go);
    }

    // ── 총 스탯 합산 ──────────────────────────────────────────

    void BuildTotalStats()
    {
        foreach (var r in _totalRows) { if (r) Destroy(r); }
        _totalRows.Clear();

        if (_totalStatContent == null || _totalStatTemplate == null) return;

        var runData = UserDataManager.Instance?.Get<RunAbilityData>();
        if (runData == null) return;

        var db = AbilityDatabase.Current;
        if (db == null) return;

        // (StatType, AbilityTarget) 쌍별 합산 — 대상이 다른 동일 스탯은 별도 행으로 표시
        var totals = new Dictionary<(StatType stat, AbilityTarget target), float>();

        // Special 신고분은 따로 모은다 — 합치는 규칙이 다르다 (아래 참고)
        var special = new Dictionary<(StatType stat, AbilityTarget target), float>();
        var preview = new Dictionary<StatType, float>();

        foreach (var id in runData.HeldAbilities)
        {
            var d = db.Get(id);
            if (d == null || d.Grade == AbilityGrade.Mastery) continue;

            // Special 은 Stat1/Value1 이 비어 있고 효과가 OnTrigger 코드에 있다.
            // 스스로 신고한 상시 효과만 합산에 넣는다 (AbilityData.CollectPreviewStats 참고).
            if (d.Grade == AbilityGrade.Special)
            {
                preview.Clear();
                d.CollectPreviewStats(preview);

                foreach (var kv in preview)
                {
                    var key = (kv.Key, d.Target);
                    special[key] = special.GetValueOrDefault(key) + kv.Value;
                }
                continue;
            }

            var k1 = (d.Stat1, d.Target);
            totals[k1] = totals.GetValueOrDefault(k1) + d.Value1;
            if (d.HasStat2)
            {
                var k2 = (d.Stat2, d.Target);
                totals[k2] = totals.GetValueOrDefault(k2) + d.Value2;
            }
        }

        // ⚠ 쿨감만 잔여 곱연산으로 합친다 (HeroStatResolver 와 같은 규칙)
        //   시간 왜곡은 전투에서 기존 쿨감과 곱해서 합쳐진다. 여기서 더해 버리면
        //   이 패널 숫자와 장수 상세의 쿨타임 줄이 서로 안 맞는다.
        foreach (var kv in special)
        {
            totals[kv.Key] = kv.Key.stat == StatType.SkillCooldownReduce
                ? HeroStatResult.CombineResidual(totals.GetValueOrDefault(kv.Key), kv.Value)
                : totals.GetValueOrDefault(kv.Key) + kv.Value;
        }

        // StatType 순 → 같은 스탯 내에서 AbilityTarget 순 정렬
        var sorted = new List<KeyValuePair<(StatType stat, AbilityTarget target), float>>(totals);
        sorted.Sort((a, b) =>
        {
            int cmp = a.Key.stat.CompareTo(b.Key.stat);
            return cmp != 0 ? cmp : a.Key.target.CompareTo(b.Key.target);
        });

        foreach (var (key, total) in sorted)
        {
            string label   = $"{LocalizationManager.Instance.Get(key.stat.ToString())}({LocalizationManager.Instance.Get(key.target.ToString())})";
            string valStr  = AbilityUIHelper.FormatStatValue(key.stat, total);
            var go  = Instantiate(_totalStatTemplate.gameObject, _totalStatContent);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text  = $"<color=#888888>{label}</color>  <color=#{StatBonusColors.Ability}>{valStr}</color>";
            tmp.color = Color.white;
            go.SetActive(true);
            _totalRows.Add(go);
        }
    }

    // ── 클린업 ───────────────────────────────────────────────

    void ClearList()
    {
        foreach (var g in _listItems) { if (g) Destroy(g); }
        _listItems.Clear();
    }

    void ClearAll()
    {
        ClearList();
        foreach (var r in _infoRows)  { if (r) Destroy(r); }
        _infoRows.Clear();
        foreach (var r in _totalRows) { if (r) Destroy(r); }
        _totalRows.Clear();
        _selected = null;
    }

    // ── 하이라이트 재계산 (선택 배경색) ─────────────────────────

    void RefreshHighlightColors(AbilityId selectedId)
    {
        foreach (var go in _listItems)
        {
            if (go == null) continue;
            var d = AbilityDatabase.Current?.Get(
                (AbilityId)System.Enum.Parse(typeof(AbilityId), go.name, true));
            if (d == null) continue;
            var gc = AbilityUIHelper.GradeColor(d.Grade);
            var bg = go.GetComponent<Image>();
            if (bg != null)
                bg.color = (go.name == selectedId.ToString())
                    ? gc * 0.35f + new Color(0.06f, 0.07f, 0.12f, 1f)
                    : gc * 0.20f + new Color(0.06f, 0.07f, 0.12f, 1f);
        }
    }
}
