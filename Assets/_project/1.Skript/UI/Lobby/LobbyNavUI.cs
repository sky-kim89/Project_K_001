using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  LobbyNavUI.cs
//  하단 내비게이션 바 — 탭 버튼과 컨텐츠 패널을 연결한다.
//
//  Inspector 설정:
//    NavButtons : 하단 버튼 5개 (홈·영웅·전투·상점·프로필 순서)
//    Panels     : 대응하는 컨텐츠 패널 5개 (같은 순서)
//    DefaultTab : 첫 활성 탭 인덱스 (기본 2 = 전투)
//
//  패널 추가 방법:
//    1. Panels 배열에 새 패널 GameObject 연결
//    2. NavButtons 배열에 대응 버튼 연결
//    (인덱스가 일치하면 자동 연결)
// ============================================================

public class LobbyNavUI : MonoBehaviour
{
    [SerializeField] Button[]     _navButtons;
    [SerializeField] GameObject[] _panels;
    [SerializeField] int          _defaultTab = 2;

    [SerializeField] Color _activeColor   = new Color(0.20f, 0.70f, 0.90f, 1f);
    [SerializeField] Color _inactiveColor = new Color(0.22f, 0.22f, 0.28f, 1f);

    int _activeIndex;

    // ── 생명주기 ──────────────────────────────────────────────

    void Start()
    {
        for (int i = 0; i < _navButtons.Length; i++)
        {
            int idx = i;
            _navButtons[i]?.onClick.AddListener(() => Switch(idx));
        }
        Switch(_defaultTab);
    }

    // ── 공개 API ──────────────────────────────────────────────

    public void Switch(int index)
    {
        if (index < 0 || index >= _panels.Length) return;
        _activeIndex = index;

        for (int i = 0; i < _panels.Length; i++)
            _panels[i]?.SetActive(i == index);

        for (int i = 0; i < _navButtons.Length; i++)
        {
            if (_navButtons[i] == null) continue;
            var img = _navButtons[i].GetComponent<Image>();
            if (img != null) img.color = i == index ? _activeColor : _inactiveColor;
        }
    }
}
