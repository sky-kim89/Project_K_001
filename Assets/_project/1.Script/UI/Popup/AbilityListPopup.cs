using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  AbilityListPopup.cs
//  어빌리티 전체 목록 확인 팝업.
//
//  레이아웃:
//    ┌─ Header ─────────────────────────────────┐
//    │  IconScroll (6열 그리드, 스크롤)         │
//    ├──────────────────────────────────────────┤
//    │  InfoPanel (좌)  │  StatPanel (우)       │
//    └──────────────────┴───────────────────────┘
//
//  사용법:
//    PopupManager.Instance.Open<AbilityListPopup>(PopupType.AbilityList);
// ============================================================

public class AbilityListPopup : PopupBase
{
    [Header("아이콘 그리드")]
    [SerializeField] Transform _iconGridContent;

    [Header("어빌리티 정보 패널 (좌)")]
    [SerializeField] Image           _infoIcon;
    [SerializeField] Image           _infoGradeBar;
    [SerializeField] TextMeshProUGUI _infoGradeTmp;
    [SerializeField] TextMeshProUGUI _infoNameTmp;
    [SerializeField] TextMeshProUGUI _infoTargetTmp;

    [Header("스텟 목록 패널 (우)")]
    [SerializeField] Transform       _statListContent;
    [SerializeField] TextMeshProUGUI _statRowTemplate;   // 비활성 템플릿

    [Header("닫기")]
    [SerializeField] Button _closeBtn;

    AbilityData                _selected;
    readonly List<GameObject>  _iconBtns = new();
    readonly List<GameObject>  _statRows = new();

    // ── PopupBase 훅 ─────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _closeBtn?.onClick.AddListener(() => Close());
    }

    protected override void OnAfterOpen()
    {
        BuildIconGrid();
    }

    protected override void OnAfterClose()
    {
        ClearAll();
    }

    // ── 아이콘 그리드 ─────────────────────────────────────────

    void BuildIconGrid()
    {
        if (_iconGridContent == null) return;
        var db = AbilityDatabase.Current;
        if (db == null) return;

        AbilityData first = null;
        foreach (var ability in db.GetAll())
        {
            if (ability == null) continue;
            first ??= ability;
            var btn = CreateIconBtn(ability);
            btn.transform.SetParent(_iconGridContent, false);
            _iconBtns.Add(btn);
        }

        if (first != null) SelectAbility(first);
    }

    GameObject CreateIconBtn(AbilityData data)
    {
        // 루트 (보더 역할)
        var root = new GameObject(data.Id.ToString(), typeof(RectTransform), typeof(Image));
        var rootImg = root.GetComponent<Image>();
        rootImg.color = GetGradeColor(data.Grade) * 0.7f;

        // 아이콘 이미지
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.07f, 0.07f);
        iconRt.anchorMax = new Vector2(0.93f, 0.93f);
        iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
        var iconImg = iconGo.GetComponent<Image>();
        if (data.Icon != null) { iconImg.sprite = data.Icon; iconImg.preserveAspect = true; }
        else                     iconImg.color = new Color(0.25f, 0.25f, 0.30f);

        // 버튼 컴포넌트
        var btn = root.AddComponent<Button>();
        var captured = data;
        btn.onClick.AddListener(() => SelectAbility(captured));

        return root;
    }

    // ── 선택 처리 ─────────────────────────────────────────────

    void SelectAbility(AbilityData data)
    {
        _selected = data;
        RefreshInfo();
        RefreshStats();
    }

    void RefreshInfo()
    {
        if (_selected == null) return;
        var d  = _selected;
        var gc = GetGradeColor(d.Grade);

        if (_infoIcon     != null) { _infoIcon.sprite = d.Icon; _infoIcon.enabled = d.Icon != null; }
        if (_infoGradeBar != null)   _infoGradeBar.color = gc;
        if (_infoGradeTmp != null) { _infoGradeTmp.text = GetGradeLabel(d.Grade); _infoGradeTmp.color = gc; }
        if (_infoNameTmp  != null)   _infoNameTmp.text  = d.AbilityName;
        if (_infoTargetTmp != null)  _infoTargetTmp.text = GetTargetLabel(d.Target);
    }

    void RefreshStats()
    {
        foreach (var r in _statRows) { if (r) Destroy(r); }
        _statRows.Clear();

        if (_statListContent == null || _selected == null) return;
        var d = _selected;

        if (d.Grade == AbilityGrade.Special)
        {
            AddStatRow($"발동 조건", GetTriggerLabel(d.GetTriggerType()), GetGradeColor(d.Grade));
            AddStatRow("효과", d.AbilityName, Color.white);
        }
        else
        {
            AddStatRow(StatLabel(d.Stat1), $"+{d.Value1 * 100f:0.#}%", Color.white);
            if (d.HasStat2)
                AddStatRow(StatLabel(d.Stat2), $"+{d.Value2 * 100f:0.#}%", Color.white);
        }
    }

    void AddStatRow(string label, string value, Color valColor)
    {
        if (_statRowTemplate == null || _statListContent == null) return;
        var go  = Instantiate(_statRowTemplate.gameObject, _statListContent);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text  = $"<color=#AAAAAA>{label}</color>  {value}";
        tmp.color = valColor;
        go.SetActive(true);
        _statRows.Add(go);
    }

    void ClearAll()
    {
        foreach (var b in _iconBtns) { if (b) Destroy(b); }
        _iconBtns.Clear();
        foreach (var r in _statRows) { if (r) Destroy(r); }
        _statRows.Clear();
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
        StatType.CritChance          => "치명타 확률",
        StatType.SkillCooldownReduce => "스킬 쿨감",
        _                            => t.ToString()
    };
}
