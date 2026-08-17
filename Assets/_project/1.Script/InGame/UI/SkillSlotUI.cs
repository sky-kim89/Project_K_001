using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  SkillSlotUI.cs
//  스킬 슬롯 하나. **누르면 스킬이 나가는 버튼**이다 (AUTO 가 꺼져 있을 때의 유일한 발동 수단).
//
//  Hierarchy — UISetupTool.CreateGeneralPanelPrefab() 이 만든다.
//    SkillSlot (SkillSlotUI + Button — EditorUIBuilder.RaisedBtnOn)
//      ├ Shadow
//      └ Body                ← Button.targetGraphic
//          ├ TopEdge / BottomEdge
//          ├ Icon            (Image — 스킬 아이콘)
//          ├ CooldownOverlay (Image — Filled, Radial360, 반시계 / 검정 반투명)
//          └ CooldownText    (TMP   — 남은 초 숫자, 쿨중에만 표시)
//
//  ⚠ 준비 완료 표시로 노란 커버를 덮지 않는다
//    예전엔 ReadyGlow(노란 반투명 판)가 슬롯 전체를 덮어 아이콘이 안 보였다.
//    쿨다운 오버레이가 걷히는 것 자체가 준비 완료 신호다.
//
//  ⚠ "쿨은 찼지만 지금은 못 쓴다" 를 따로 보여준다
//    공격 스킬은 적이 사거리 안에 들어와야 나간다 (SkillUsePolicy).
//    그 사이 아이콘을 어둡게 죽여 두지 않으면 눌러도 아무 일이 없는 것처럼 보인다.
// ============================================================

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] Button          _button;
    [SerializeField] Image           _iconImage;
    [SerializeField] Image           _cooldownOverlay;   // Filled / Radial 360
    [SerializeField] TextMeshProUGUI _cooldownText;      // 남은 초 표시

    // 사거리 밖 등으로 아직 못 쓸 때의 아이콘 색 — 회색이 아니라 어둡게만 죽인다
    static readonly Color IconUsable   = Color.white;
    static readonly Color IconUnusable = new Color(0.45f, 0.47f, 0.55f, 1f);

    // ── 공개 API ─────────────────────────────────────────────

    public void SetIcon(Sprite icon)
    {
        if (_iconImage != null)
            _iconImage.sprite = icon;
    }

    /// <summary>클릭 시 호출할 동작을 건다. 재사용 대비로 기존 리스너를 먼저 비운다.</summary>
    public void BindClick(UnityAction action)
    {
        if (_button == null) return;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(action);
    }

    /// <summary>
    /// 쿨다운 상태를 갱신한다.
    /// remaining ≤ 0 이면 준비 완료 — 오버레이·텍스트를 숨긴다.
    /// </summary>
    public void UpdateCooldown(float remaining, float total)
    {
        bool  ready = remaining <= 0f;
        float fill  = ready ? 0f : Mathf.Clamp01(remaining / Mathf.Max(total, 0.001f));

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = fill;
            _cooldownOverlay.gameObject.SetActive(!ready);
        }

        if (_cooldownText != null)
        {
            _cooldownText.gameObject.SetActive(!ready);
            _cooldownText.text = ready ? "" : Mathf.CeilToInt(remaining).ToString();
        }
    }

    /// <summary>지금 눌러서 나가는 상태인지 표시한다 (사거리·경직·사망 포함).</summary>
    public void SetUsable(bool usable)
    {
        if (_iconImage != null) _iconImage.color = usable ? IconUsable : IconUnusable;
        if (_button    != null) _button.interactable = usable;
    }
}
