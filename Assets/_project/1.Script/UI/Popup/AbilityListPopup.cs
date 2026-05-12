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

        // 동일 ID 중복 카운트
        var counts = new Dictionary<AbilityId, int>();
        foreach (var id in runData.HeldAbilities)
        {
            if (!counts.ContainsKey(id)) counts[id] = 0;
            counts[id]++;
        }

        if (_headerCountTmp != null)
            _headerCountTmp.text = $"{runData.HeldAbilities.Count}개 보유";

        AbilityData firstData = null;
        foreach (var (id, cnt) in counts)
        {
            var data = db.Get(id);
            if (data == null) continue;
            firstData ??= data;

            var item = CreateListItem(data, cnt);
            item.transform.SetParent(_listContent, false);
            _listItems.Add(item);
        }

        if (firstData != null) SelectAbility(firstData);
    }

    GameObject CreateListItem(AbilityData data, int count)
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
        if (bg != null) bg.color = GetGradeColor(data.Grade) * 0.25f + new Color(0.06f, 0.07f, 0.12f, 1f);

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
        { gradeTmp.text = GetGradeLabel(data.Grade); gradeTmp.color = GetGradeColor(data.Grade); }

        // 대상
        var targetTmp = go.transform.Find("TargetText")?.GetComponent<TextMeshProUGUI>();
        if (targetTmp != null) targetTmp.text = GetTargetLabel(data.Target);

        // ×N 카운트 뱃지
        var countGo  = go.transform.Find("CountBadge");
        var countTmp = countGo?.GetComponent<TextMeshProUGUI>();
        if (countGo != null)  countGo.gameObject.SetActive(count > 1);
        if (countTmp != null) countTmp.text = $"×{count}";

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
        HighlightListItem(data.Id);
    }

    void RefreshDetail()
    {
        if (_selected == null) return;
        var d  = _selected;
        var gc = GetGradeColor(d.Grade);

        if (_infoIcon     != null) { _infoIcon.sprite = d.Icon; _infoIcon.enabled = d.Icon != null; }
        if (_infoGradeBar != null)   _infoGradeBar.color = gc;
        if (_infoGradeTmp != null) { _infoGradeTmp.text = GetGradeLabel(d.Grade); _infoGradeTmp.color = gc; }
        if (_infoNameTmp  != null)   _infoNameTmp.text  = d.AbilityName;
        if (_infoTargetTmp!= null)   _infoTargetTmp.text = GetTargetLabel(d.Target);

        // 기존 스탯 행 제거
        foreach (var r in _infoRows) { if (r) Destroy(r); }
        _infoRows.Clear();

        if (_infoStatContent == null || _infoStatTemplate == null) return;

        if (d.Grade == AbilityGrade.Special)
        {
            // 발동 조건
            AddInfoRow($"<color=#888888>발동 조건</color>  {GetTriggerLabel(d.GetTriggerType())}", gc);
            // 실제 효과 설명
            var desc = d.Description;
            if (!string.IsNullOrEmpty(desc))
                AddInfoRow($"<color=#888888>효과</color>  {desc}", Color.white);
        }
        else
        {
            // Value1 / Value2 는 float 비율 (예: 0.08 = 8%)
            AddInfoRow($"<color=#888888>{StatLabel(d.Stat1)}</color>  +{d.Value1 * 100f:0.#}%", Color.white);
            if (d.HasStat2)
                AddInfoRow($"<color=#888888>{StatLabel(d.Stat2)}</color>  +{d.Value2 * 100f:0.#}%", Color.white);
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
        foreach (var id in runData.HeldAbilities)
        {
            var d = db.Get(id);
            if (d == null || d.Grade == AbilityGrade.Special) continue;

            var k1 = (d.Stat1, d.Target);
            totals[k1] = totals.GetValueOrDefault(k1) + d.Value1;
            if (d.HasStat2)
            {
                var k2 = (d.Stat2, d.Target);
                totals[k2] = totals.GetValueOrDefault(k2) + d.Value2;
            }
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
            string label = $"{StatLabel(key.stat)}({GetTargetLabel(key.target)})";
            var go  = Instantiate(_totalStatTemplate.gameObject, _totalStatContent);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text  = $"<color=#888888>{label}</color>  <color=#{StatBonusColors.Ability}>+{total * 100f:0.#}%</color>";
            tmp.color = Color.white;
            go.SetActive(true);
            _totalRows.Add(go);
        }
    }

    // ── 선택 하이라이트 ────────────────────────────────────────

    void HighlightListItem(AbilityId selectedId)
    {
        foreach (var go in _listItems)
        {
            if (go == null) continue;
            // 아이템 이름이 AbilityId 이름과 일치하면 강조
            bool isSelected = go.name == selectedId.ToString();
            var bg = go.GetComponent<Image>();
            if (bg == null) continue;
            var baseColor = _selected != null
                ? GetGradeColor(_selected.Grade) * 0.25f + new Color(0.06f, 0.07f, 0.12f, 1f)
                : new Color(0.08f, 0.09f, 0.14f, 1f);

            // 이름으로 올바르게 강조
            var d = AbilityDatabase.Current?.Get((AbilityId)System.Enum.Parse(typeof(AbilityId), go.name, true));
            if (d != null)
            {
                var gc = GetGradeColor(d.Grade);
                bg.color = (go.name == selectedId.ToString())
                    ? gc * 0.35f + new Color(0.06f, 0.07f, 0.12f, 1f)
                    : gc * 0.20f + new Color(0.06f, 0.07f, 0.12f, 1f);
            }
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

    // ── 레이블 헬퍼 ───────────────────────────────────────────

    static string GetGradeLabel(AbilityGrade g) => g switch
    {
        AbilityGrade.Normal   => "일반",
        AbilityGrade.Advanced => "고급",
        AbilityGrade.Special  => "특수",
        _                     => "?"
    };

    static Color GetGradeColor(AbilityGrade g) => g switch
    {
        AbilityGrade.Normal   => new Color(0.70f, 0.70f, 0.75f),
        AbilityGrade.Advanced => new Color(0.40f, 0.72f, 1.00f),
        AbilityGrade.Special  => new Color(1.00f, 0.80f, 0.20f),
        _                     => Color.white
    };

    static string GetTargetLabel(AbilityTarget t) => t switch
    {
        AbilityTarget.All              => "전체",
        AbilityTarget.Job_Knight       => "기사",
        AbilityTarget.Job_Archer       => "궁수",
        AbilityTarget.Job_Mage         => "마법사",
        AbilityTarget.Job_ShieldBearer => "방패병",
        AbilityTarget.Range_Melee      => "근거리",
        AbilityTarget.Range_Ranged     => "원거리",
        AbilityTarget.Unit_General     => "장군",
        AbilityTarget.Unit_Soldier     => "병사",
        _                              => "?"
    };

    static string GetTriggerLabel(PassiveTrigger t) => t switch
    {
        PassiveTrigger.OnAttack       => "공격 시",
        PassiveTrigger.OnHit          => "피격 시",
        PassiveTrigger.OnEnemyKill    => "처치 시",
        PassiveTrigger.OnSoldierDeath => "병사 사망 시",
        PassiveTrigger.OnSkillUse     => "스킬 사용 시",
        _                             => "즉시"
    };

    static string StatLabel(StatType t) => t switch
    {
        StatType.MaxHp               => "최대 체력",
        StatType.Attack              => "공격력",
        StatType.AttackSpeed         => "공격속도",
        StatType.MoveSpeed           => "이동속도",
        StatType.Defense             => "방어율",
        StatType.AttackRange         => "사거리",
        StatType.CritChance          => "치명타",
        StatType.SkillCooldownReduce => "스킬 쿨감",
        _                            => t.ToString()
    };
}
