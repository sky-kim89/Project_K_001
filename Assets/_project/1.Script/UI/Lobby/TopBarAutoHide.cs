using UnityEngine;

// ============================================================
//  TopBarAutoHide.cs
//  메인 화면(장수 선택)에서는 상단바를 숨긴다.
//
//  ■ 왜 상단바 쪽이 이 규칙을 갖는가
//    LobbyMenuButton 과 같은 이유다 — TopBar 는 로비 패널 **전부의 위에** 얹혀 있다.
//    "누가 나를 숨기는가" 를 패널마다 적게 하면 패널이 늘어날 때마다 같은 코드가 생기고,
//    한 곳을 빠뜨리면 그 화면에서만 상단바가 남는다.
//    상단바가 자기 노출 규칙을 들고 있는 쪽이 안전하다.
//
//  ■ 왜 메인 화면에서 숨기는가
//    ① 이 화면은 장수 하나를 크게 보여 주는 자리다. 재화·특성 줄은 출전 준비 화면
//      (BattlePanel)에서 볼 것이지, 장수를 고르는 동안 볼 정보가 아니다.
//    ② 상단바(180px)가 좌측 타이틀 이미지(300px)의 위쪽을 덮고 있었다.
//      숨기면 "PIXEL GENERAL" 타이틀이 온전히 드러난다.
//
//  ⚠ GameObject 를 끄지 않는다 — CanvasGroup 으로 감춘다
//    SetActive(false) 로 끄면 이 컴포넌트도 같이 멈춰 OnDisable 로 구독이 풀린다.
//    그러면 메인 화면을 떠나도 자기를 다시 켤 주체가 없어 상단바가 영영 사라진다.
//    루트는 계속 살려 두고 알파·레이캐스트만 내린다.
//
//  붙이는 곳: TopBarCreator 가 TopBar 루트에 자동으로 붙인다.
// ============================================================

[RequireComponent(typeof(CanvasGroup))]
public class TopBarAutoHide : MonoBehaviour
{
    CanvasGroup _group;

    void Awake() => _group = GetComponent<CanvasGroup>();

    void OnEnable()
    {
        MainPanelUI.OnShown  += Hide;
        MainPanelUI.OnHidden += Show;

        // ⚠ 상단바가 메인 화면보다 늦게 살아날 수 있다
        //   패널은 LobbyNavUI.Start 가 켠다. 그보다 늦게 이 OnEnable 이 돌면
        //   OnShown 은 이미 지나간 뒤라 상단바가 메인 화면 위에 남는다.
        //   신호를 기다리지 말고 지금 상태를 직접 본다.
        //   (FindAnyObjectByType 은 꺼진 오브젝트를 제외하므로 '떠 있는' 것만 잡힌다)
        SetVisible(FindAnyObjectByType<MainPanelUI>() == null);
    }

    void OnDisable()
    {
        MainPanelUI.OnShown  -= Hide;
        MainPanelUI.OnHidden -= Show;
    }

    void Hide() => SetVisible(false);
    void Show() => SetVisible(true);

    void SetVisible(bool on)
    {
        _group.alpha          = on ? 1f : 0f;
        _group.blocksRaycasts = on;
        _group.interactable   = on;
    }
}
