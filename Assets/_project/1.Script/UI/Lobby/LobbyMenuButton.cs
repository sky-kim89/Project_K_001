using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  LobbyMenuButton.cs
//  로비 상단바의 메뉴 버튼(≡) — 일시 정지 팝업을 연다.
//
//  ■ 왜 컴포넌트 하나짜리인가
//    TopBar 는 프리팹이고 로비 패널 전부의 위에 얹혀 있다. 패널 쪽 스크립트가
//    이 버튼을 찾아 붙이게 하면 패널마다 같은 코드가 생기고, 한 곳을 빠뜨리면
//    그 화면에서만 메뉴가 죽는다. 버튼이 자기 동작을 들고 있는 쪽이 안전하다.
//
//  ■ PausePopup 은 로비에서도 쓴다
//    전투 중이 아니면 "즉시 환생하기" 행을 스스로 접는다 (PausePopup.ApplyContext).
//    로비 전용 메뉴 팝업을 따로 만들면 사운드 토글이 두 벌이 된다.
//
//  붙이는 곳: TopBarCreator 가 SettingsBtn 에 자동으로 붙인다.
// ============================================================

[RequireComponent(typeof(Button))]
public class LobbyMenuButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Open);
    }

    static void Open()
    {
        // 이미 열려 있으면 그대로 둔다 — 다시 Open 하면 PopupManager 가
        // 기존 팝업을 닫고 새로 여느라 경고를 찍고 화면이 한 번 깜빡인다.
        if (PopupManager.Instance.IsOpen(PopupType.Pause)) return;
        PopupManager.Instance.Open<PausePopup>(PopupType.Pause);
    }
}
