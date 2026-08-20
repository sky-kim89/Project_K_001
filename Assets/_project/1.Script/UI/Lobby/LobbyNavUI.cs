using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  LobbyNavUI.cs
//  하단 내비게이션 바 — 탭 버튼과 컨텐츠 패널을 연결한다.
//
//  Inspector 설정:
//    NavButtons : 하단 버튼 4개 (상점·영웅·전투·프로필 순서)
//    Panels     : [0]상점 [1]영웅 [2]전투 [3]프로필 [4]MainPanel
//                 MainPanel 은 버튼 없이 코드로만 켠다 (LobbyManager)
//    DefaultTab : 첫 활성 탭 인덱스 (기본 2 = 전투)
//
//  ⚠ 유물은 탭이 아니다 — RelicPopup 이다
//    탭은 서로를 끈다. 유물을 들렀다 오면 MainPanel 이 다시 OnEnable 을 타
//    고르던 장수가 새로 추첨됐다. 잠깐 들르는 화면은 팝업으로 덮는다.
//
//  패널 추가 방법:
//    1. Panels 배열에 새 패널 GameObject 연결
//    2. NavButtons 배열에 대응 버튼 연결
//    (인덱스가 일치하면 자동 연결)
//
//  ⚠ Switch 는 자기 자신에게 되돌아올 수 있다
//    SetActive(false) 는 그 자리에서 OnDisable 을 돌린다. MainPanelUI 는
//    OnDisable 에서 데모 전투를 끝내고, 그 신호가 LobbyManager 의 흐름
//    (NotifyDemoRunning → Returning → Clean → SelectInitialPanel)을 타고
//    Switch 로 되돌아온다. 재진입을 막지 않으면 탭 전환이 통째로 깨진다.
// ============================================================

public class LobbyNavUI : MonoBehaviour
{
    [SerializeField] Button[]     _navButtons;
    [SerializeField] GameObject[] _panels;
    [SerializeField] int          _defaultTab = 2;

    [SerializeField] Color _activeColor   = new Color(0.20f, 0.70f, 0.90f, 1f);
    [SerializeField] Color _inactiveColor = new Color(0.22f, 0.22f, 0.28f, 1f);

    int  _activeIndex;
    bool _switching;

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

        // ⚠ 전환 도중에 들어온 요청은 버린다
        //   패널을 끄면 그 자리에서 OnDisable 이 돌고, 거기서 시작된 흐름이
        //   Switch 를 다시 부른다 (파일 상단 주석 참고). 그대로 통과시키면
        //     ① 아직 끄는 중인 패널에 SetActive 가 또 들어가
        //        "GameObject is already being activated or deactivated" 경고가 뜨고
        //     ② 중첩 호출이 패널을 제 인덱스 기준으로 다 바꿔 놓은 뒤
        //        바깥 루프가 남은 칸을 이어서 덮어, 어느 패널도 안 켜진 빈 화면이 남는다.
        //   먼저 시작한 쪽이 사용자가 실제로 누른 탭이므로 그쪽을 끝까지 살린다.
        if (_switching) return;
        _switching = true;

        try
        {
            _activeIndex = index;

            for (int i = 0; i < _panels.Length; i++)
                _panels[i]?.SetActive(i == index);

            // 탭을 켤 때마다 그 패널의 버튼에 클릭음을 건다.
            // 카드·슬롯이 런타임에 만들어지므로 Start 에서 한 번만 걸면 새 버튼이 빠진다.
            // 이미 걸린 버튼은 UIClickSfxMark 로 건너뛰므로 반복 호출해도 싸다.
            UIClickSfx.Bind(gameObject);

            for (int i = 0; i < _navButtons.Length; i++)
            {
                if (_navButtons[i] == null) continue;
                var img = _navButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == index ? _activeColor : _inactiveColor;
            }
        }
        finally
        {
            // 예외가 나도 플래그는 반드시 푼다 — 안 그러면 탭이 영영 안 바뀐다.
            _switching = false;
        }
    }
}
