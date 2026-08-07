using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  SkillIconUI.cs
//  스킬(액티브·패시브) 아이콘 1칸을 표시하는 재사용 컴포넌트.
//  TraitIconUI 와 같은 구조 — 아이콘 + 누르면 InfoTooltipUI.
//
//  ■ 재사용 위치
//    - MainPanel 장수 선택 카드 (GeneralCandidateCardUI)
//    - 앞으로 스킬 아이콘이 필요한 모든 UI
//
//  ■ 아이콘 출처가 둘로 갈린다
//    액티브 : ActiveSkillData 에 Sprite 필드가 없다 →
//             ActiveSkillId.IconKey() 로 SpriteManager 에서 꺼낸다.
//    패시브 : PassiveSkillData.Icon 을 그대로 쓴다.
//
//  ■ 잠긴 칸은 숨기지 않는다
//    등급이 낮아 아직 못 쓰는 패시브 칸은 SetLocked() 로 흐리게 남긴다.
//    칸을 아예 없애면 "등급을 올리면 늘어난다" 는 정보가 사라진다.
// ============================================================

public class SkillIconUI : MonoBehaviour
{
    [SerializeField] Image         _frame;       // 테두리 — 액티브 / 패시브 / 잠김 구분
    [SerializeField] Image         _iconImage;
    [SerializeField] Button        _iconBtn;
    [SerializeField] InfoTooltipUI _tooltip;

    public static readonly Color ActiveFrame  = new(0.94f, 0.70f, 0.26f, 1f);   // 금빛 = 액티브
    public static readonly Color PassiveFrame = new(0.34f, 0.56f, 0.92f, 1f);   // 파랑 = 패시브
    public static readonly Color LockedFrame  = new(0.16f, 0.17f, 0.26f, 1f);
    static readonly Color LockedIcon  = new(0.22f, 0.23f, 0.34f, 1f);

    string _title, _desc, _stat;

    // ── 공개 API ─────────────────────────────────────────────

    public void SetActiveSkill(ActiveSkillId id, ActiveSkillData data)
    {
        string title = data != null && !string.IsNullOrEmpty(data.SkillName)
            ? data.SkillName
            : LocalizationManager.Instance.Get(id.ToString());

        Bind(SpriteManager.Instance?.Get(id.IconKey()), ActiveFrame,
             title, data != null ? data.Description : "",
             data != null ? $"쿨타임 {data.Cooldown:0.#}초" : "");
    }

    public void SetPassiveSkill(PassiveSkillData data)
    {
        if (data == null) { SetLocked(); return; }
        Bind(data.Icon, PassiveFrame, data.SkillName, data.Description, "");
    }

    /// <summary>등급이 모자라 아직 열리지 않은 칸.</summary>
    public void SetLocked()
    {
        CloseTooltip();
        _title = _desc = _stat = null;

        if (_frame     != null) _frame.color      = LockedFrame;
        if (_iconImage != null) { _iconImage.sprite = null; _iconImage.color = LockedIcon; }
        if (_iconBtn   != null)
        {
            _iconBtn.onClick.RemoveAllListeners();
            _iconBtn.interactable = false;
        }
    }

    public void CloseTooltip()
    {
        if (_tooltip != null) _tooltip.Close();
    }

    void OnDisable() => CloseTooltip();

    // ── 내부 ─────────────────────────────────────────────────

    void Bind(Sprite icon, Color frame, string title, string desc, string stat)
    {
        CloseTooltip();

        _title = title;
        _desc  = desc;
        _stat  = stat;

        if (_frame != null) _frame.color = frame;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.color  = icon != null ? Color.white : LockedIcon;
        }

        if (_iconBtn != null)
        {
            _iconBtn.onClick.RemoveAllListeners();
            _iconBtn.interactable = true;
            _iconBtn.onClick.AddListener(OpenTooltip);
        }
    }

    void OpenTooltip() => _tooltip.Show(_title, _desc, _stat);
}
