using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  DeploySlotRowUI.cs
//  영웅 상세 패널 — 배치 위치 선택 줄 (슬롯 5개).
//
//  Inspector 연결:
//    _slotButtons[5]  : 각 슬롯의 클릭 버튼
//    _slotBgs[5]      : 슬롯 배경 Image (색상으로 상태 표시)
//    _slotNumTexts[5] : "1"~"5" 번호 텍스트 (선택)
//
//  슬롯 색상:
//    내가 배치됨    → ColMine  (초록 강조)
//    다른 장수 배치 → ColOther (파란 강조)
//    비어 있음      → ColEmpty (진한 회색)
//
//  클릭 동작:
//    내가 배치된 슬롯 클릭 → 배치 해제
//    빈/다른 장수 슬롯 클릭 → 현재 장수 배치 (다른 장수가 있으면 밀어냄)
// ============================================================

public class DeploySlotRowUI : MonoBehaviour
{
    [SerializeField] Button[]          _slotButtons;
    [SerializeField] Image[]           _slotBgs;
    [SerializeField] TextMeshProUGUI[] _slotNumTexts;

    static readonly Color ColEmpty = new Color(0.20f, 0.20f, 0.23f);
    static readonly Color ColMine  = new Color(0.18f, 0.65f, 0.28f);
    static readonly Color ColOther = new Color(0.22f, 0.54f, 0.92f);

    UnitEntry      _current;
    DeploymentData _data;

    /// <summary>배치 상태가 변경될 때 발생. HeroPanelUI 가 카드 뱃지를 갱신하는 데 사용.</summary>
    public event Action OnDeployChanged;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(UnitEntry entry, DeploymentData data)
    {
        _current = entry;
        _data    = data;

        for (int i = 0; i < _slotButtons.Length; i++)
        {
            int slot = i;
            _slotButtons[i].onClick.RemoveAllListeners();
            _slotButtons[i].onClick.AddListener(() => OnSlotClick(slot));
        }

        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < _slotBgs.Length; i++)
        {
            string occupant = _data?.GetUnitAt(i) ?? "";
            bool isMine  = !string.IsNullOrEmpty(occupant) && occupant == _current?.UnitName;
            bool isOther = !string.IsNullOrEmpty(occupant) && !isMine;

            _slotBgs[i].color = isMine ? ColMine : isOther ? ColOther : ColEmpty;

            if (_slotNumTexts != null && i < _slotNumTexts.Length)
                _slotNumTexts[i].text = (i + 1).ToString();
        }
    }

    // ── 내부 ─────────────────────────────────────────────────

    void OnSlotClick(int slot)
    {
        if (_current == null || _data == null) return;

        string occupant = _data.GetUnitAt(slot);
        if (occupant == _current.UnitName)
            _data.UndeploySlot(slot);
        else
            _data.Deploy(_current.UnitName, slot);

        UserDataManager.Instance.RequestSave();
        Refresh();
        OnDeployChanged?.Invoke();
    }
}
