using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  TraitIconUI.cs
//  특성 아이콘 1개를 표시하는 재사용 컴포넌트.
//
//  ■ 재사용 위치
//    - MainPanel 캐릭터 카드 (GeneralCandidateCardUI)
//    - 인게임 특성 획득 팝업
//    - 이벤트 화면 특성 표시
//    → 특성 아이콘이 필요한 모든 UI에서 이 컴포넌트를 사용할 것
//
//  ■ 사용법
//    traitIconUI.Setup(TraitType.KnightCommand);
//    traitIconUI.Setup(traitDataInstance);
//    traitIconUI.CloseTooltip();
//
//  Inspector 연결 (TraitIconSlotBuilder · MainPanelCreator 자동):
//    _iconImage : 아이콘 이미지
//    _iconBtn   : 클릭 버튼 — 클릭 시 툴팁 표시
//    _tooltip   : InfoTooltipUI (이름/설명/스탯 공용 툴팁, 기본 비활성)
//
//  툴팁 열기·닫기 동작은 InfoTooltipUI 가 전담한다 —
//  보상 카드(RewardCardUI)도 같은 컴포넌트를 써서 동작이 일치한다.
// ============================================================

public class TraitIconUI : MonoBehaviour
{
    [SerializeField] Image         _iconImage;
    [SerializeField] Button        _iconBtn;
    [SerializeField] InfoTooltipUI _tooltip;

    TraitData _data;
    bool      _showStat;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(TraitType type, bool showStat = true)
    {
        var db   = TraitDatabase.Current;
        Setup(db?.Get(type), showStat);
    }

    public void Setup(TraitData data, bool showStat = true)
    {
        CloseTooltip();

        _data     = data;
        _showStat = showStat;

        if (_iconImage != null)
        {
            _iconImage.sprite = data?.Icon;
            _iconImage.color  = data?.Icon != null
                ? Color.white
                : new Color(0.25f, 0.25f, 0.38f);
        }

        if (_iconBtn != null)
        {
            _iconBtn.onClick.RemoveAllListeners();
            if (data != null) _iconBtn.onClick.AddListener(OpenTooltip);
        }
    }

    /// <summary>
    /// TraitData 가 아닌 것을 같은 슬롯에 띄운다 (난이도 디버프 등).
    ///
    /// 난이도 디버프는 특성이 아니라 TraitDatabase 에 없다. 그렇다고 TraitData 로
    /// 만들면 상점·이벤트 추첨 풀에 섞여 플레이어가 '적 강화' 를 사 버린다.
    /// 그래서 데이터는 따로 두고 표시만 이 슬롯을 빌려 쓴다 —
    /// 툴팁 동작이 일반 특성과 완전히 같아진다.
    /// </summary>
    public void SetupCustom(Sprite icon, string title, string desc, string stat = "")
    {
        CloseTooltip();
        _data = null;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.color  = icon != null ? Color.white : new Color(0.25f, 0.25f, 0.38f);
        }

        if (_iconBtn != null)
        {
            _iconBtn.onClick.RemoveAllListeners();
            _iconBtn.onClick.AddListener(() => _tooltip.Show(title, desc, stat));
        }
    }

    public void CloseTooltip()
    {
        if (_tooltip != null) _tooltip.Close();
    }

    // 슬롯이 꺼질 때(특성 목록 갱신 등) 툴팁만 화면에 남지 않게 한다.
    void OnDisable() => CloseTooltip();

    // ── 내부 ─────────────────────────────────────────────────

    void OpenTooltip()
    {
        // 보유 중인 특성이므로 누적 스택까지 포함해 보여준다.
        string stat = _showStat ? AbilityUIHelper.BuildStatText(_data) : "";
        _tooltip.Show(_data.TraitName, _data.Description, stat);
    }
}
