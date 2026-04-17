using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  SkillSlotUI.cs
//  스킬 슬롯 하나. 아이콘 이미지 + 쿨다운 오버레이(Filled/Radial360) 구성.
//
//  Hierarchy 예시:
//    SkillSlot (SkillSlotUI)
//      ├ SkillBg       (Image — 슬롯 배경)
//      ├ Icon          (Image — 스킬 아이콘)
//      ├ CooldownOverlay (Image — Filled, Radial360, 반시계 / 검정 반투명)
//      ├ CooldownText  (TMP   — 남은 초 숫자, 쿨중에만 표시)
//      └ ReadyGlow     (Image — 준비 완료 노란 테두리)
// ============================================================

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] Image           _iconImage;
    [SerializeField] Image           _cooldownOverlay;   // Filled / Radial 360
    [SerializeField] TextMeshProUGUI _cooldownText;      // 남은 초 표시
    [SerializeField] GameObject      _readyGlow;

    // ── 공개 API ─────────────────────────────────────────────

    public void SetIcon(Sprite icon)
    {
        if (_iconImage != null)
            _iconImage.sprite = icon;
    }

    /// <summary>
    /// 쿨다운 상태를 갱신한다.
    /// remaining ≤ 0 이면 준비 완료 — 오버레이·텍스트 숨기고 readyGlow 표시.
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

        if (_readyGlow != null)
            _readyGlow.SetActive(ready);
    }
}
