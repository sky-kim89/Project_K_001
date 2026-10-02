using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  PausePopup.cs
//  일시 정지 / 메뉴 팝업.
//
//  열릴 때: Time.timeScale = 0 (게임 정지)
//  닫힐 때: Time.timeScale 원복 (PopupBase 는 unscaledDeltaTime 사용)
//
//  ■ 여는 곳이 둘이다
//    인게임  TopBarUI 의 일시 정지 버튼
//    로비    TopBar 의 메뉴 버튼 (LobbyMenuButton)
//
//  ■ 항목
//    계속하기        → Close() (배속 복원)
//    효과음 / 배경음 → BattleSettingsData 볼륨. AudioManager 가 즉시 반영한다.
//    알림(Android)    → 다음 날부터 3일간 19:00 로컬 알림 토글.
//    즉시 환생하기   → BattleManager.Surrender() → 아군 전멸과 같은 경로로 패배 처리
//                     → InGameManager.HandleDefeat() 가 ReincarnationPopup 을 연다
//
//    ⚠ 예전엔 "다시 시작"·"종료" 가 있었지만 둘 다 Debug.Log 만 찍는 빈 껍데기였다.
//      런 도중 나갈 길은 환생 하나뿐이라 선택지를 그대로 둘 이유가 없다.
//
//  ⚠ "즉시 환생하기" 는 전투 중에만 보인다
//    로비에서는 포기할 전투가 없다 (Surrender 는 그냥 무시된다). 안 눌리는 버튼을
//    띄워 두는 대신 행을 접고 패널을 그만큼 줄인다 — 빈칸이 남으면 더 어색하다.
//    행 높이는 Creator 가 _surrenderRowH 로 넘겨 준다.
//
//  ⚠ Surrender() 전에 Time.timeScale 을 되돌려야 한다
//    timeScale=0 인 채로 패배 팝업이 뜨면 그 팝업의 애니메이션·버튼이 멈춘 것처럼 보인다.
//    (PopupBase 는 unscaledDeltaTime 을 쓰지만 그 뒤의 전투 정리 코루틴은 아니다)
//
//  Hierarchy — PopupPrefabCreator.CreatePausePopup() 이 만든다.
// ============================================================

public class PausePopup : PopupBase
{
    [Header("선택지")]
    [SerializeField] Button _resumeButton;
    [SerializeField] Button _reincarnateButton;

    [Header("사운드 볼륨")]
    [SerializeField] Slider          _sfxSlider;
    [SerializeField] TextMeshProUGUI _sfxValue;
    [SerializeField] Slider          _bgmSlider;
    [SerializeField] TextMeshProUGUI _bgmValue;

    [Header("언어")]
    [SerializeField] TMP_Dropdown _languageDropdown;

    [Header("Android 알림")]
    [SerializeField] Button          _notificationButton;
    [SerializeField] Image           _notificationPill;
    [SerializeField] TextMeshProUGUI _notificationState;

    [Header("전투 전용 행 접기 (Creator 가 채운다)")]
    [SerializeField] RectTransform _panelRect;
    [SerializeField] RectTransform _borderRect;
    [SerializeField] float         _panelFullH;
    [SerializeField] float         _surrenderRowH;
    [SerializeField] float         _notificationRowH;
    [SerializeField] float         _reincarnateY;

    /// <summary>테두리가 패널 밖으로 드러나는 두께 — Creator 의 값과 같아야 한다.</summary>
    const float BorderOutset = 6f;

    static readonly Color PillOn  = new Color(0.16f, 0.50f, 0.34f, 1f);
    static readonly Color PillOff = new Color(0.28f, 0.28f, 0.34f, 1f);

    float _prevTimeScale = 1f;
    bool  _surrendering;

    protected override void Awake()
    {
        base.Awake();
        ShowLegacyChoiceLabel(_resumeButton);
        ShowLegacyChoiceLabel(_reincarnateButton);
    }

    // 이전 92px 프리팹은 라벨과 설명을 반 칸씩 나눠 TMP 한 줄 높이조차
    // 확보하지 못한다. Creator 로 다시 굽기 전에도 버튼 이름만은 보이게 한다.
    static void ShowLegacyChoiceLabel(Button button)
    {
        if (((RectTransform)button.transform).sizeDelta.y >= UIScale.RowMd + UIScale.RowSm + 24f)
            return;

        Transform body = button.transform.Find("Body");
        body.Find("Hint").gameObject.SetActive(false);
        var label = body.Find("Label").GetComponent<TextMeshProUGUI>();
        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 0f);
        rect.offsetMax = new Vector2(-16f, 0f);
        label.alignment = TextAlignmentOptions.Center;
    }

    protected override void OnBeforeOpen()
    {
        // 현재 배속 저장 후 정지
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _surrendering  = false;

        // 팝업은 풀에서 재사용되므로 매번 정리하고 다시 붙인다
        _resumeButton?.onClick.RemoveAllListeners();
        _resumeButton?.onClick.AddListener(() => Close());

        _reincarnateButton?.onClick.RemoveAllListeners();
        _reincarnateButton?.onClick.AddListener(OnReincarnateClicked);

        _sfxSlider?.onValueChanged.RemoveAllListeners();
        _sfxSlider?.onValueChanged.AddListener(SetSfxVolume);

        _bgmSlider?.onValueChanged.RemoveAllListeners();
        _bgmSlider?.onValueChanged.AddListener(SetBgmVolume);

        // 언어 — 목록은 LocalizationManager 의 고정 표기(각 언어의 제 이름)로 채운다.
        //   ⚠ 열 때마다 다시 채운다. 팝업이 풀에서 재사용되므로 리스너가 쌓이면 안 된다.
        //
        // ⚠ 언어 이름은 **원어 고정**이다
        //   LocalizedText 가 붙어 있으면 "日本語" 가 지금 언어로 되번역돼,
        //   영어를 고르면 목록이 전부 영어가 된다 — 제 나라 말을 찾을 수가 없다.
        foreach (var label in _languageDropdown.GetComponentsInChildren<LocalizedText>(true))
            label.enabled = false;

        // ⚠ 글꼴에 **손대지 않는다**
        //   라벨의 글꼴을 런타임 CJK 폰트로 갈아 끼우면 캡션만 보이고 펼친 목록의 줄이
        //   전부 빈칸이 되며, 플레이를 나갈 때 파괴돼 MissingReferenceException 까지 남긴다.
        //   필요한 가나·한자는 **미리 구운 에셋**(LanguagePickerFont)이 기본 폰트의
        //   폴백표에 박혀 있다 — 굽는 곳: Tools > Project K > 아이콘·텍스처 > 언어 선택 폰트
        _languageDropdown.onValueChanged.RemoveAllListeners();
        _languageDropdown.ClearOptions();
        _languageDropdown.AddOptions(new List<string>(LocalizationManager.SupportedLanguageNames));
        _languageDropdown.onValueChanged.AddListener(SetLanguage);

        _notificationButton?.onClick.RemoveAllListeners();
        _notificationButton?.onClick.AddListener(ToggleNotifications);

        ApplyContext();
        RefreshSound();
        RefreshLanguage();
        RefreshNotifications();
    }

    protected override void OnAfterClose()
    {
        // 배속 복원 — 포기했으면 1× 로 되돌린다 (환생 팝업·로비를 배속으로 볼 이유가 없다)
        Time.timeScale = _surrendering ? 1f : _prevTimeScale;

        if (_surrendering)
            BattleManager.Instance?.Surrender();
    }

    // ── 문맥 (전투 중인가) ───────────────────────────────────

    /// <summary>
    /// 전투 중이 아니면 "즉시 환생하기" 행을 접고 패널을 그만큼 줄인다.
    ///
    /// 버튼들은 패널 위쪽에 고정돼 있으므로(pivot 상단) 높이만 줄이면
    /// 남은 항목의 자리는 그대로다.
    /// </summary>
    void ApplyContext()
    {
        // LobbyManager 가 없다 = 인게임 씬만 떠 있는 상태 → 전투로 본다
        bool inBattle = LobbyManager.Instance == null
                     || LobbyManager.Instance.Flow == LobbyFlow.Battle;
        bool showNotifications = AndroidLocalNotificationManager.IsSupported;

        _reincarnateButton?.gameObject.SetActive(inBattle);
        _notificationButton?.gameObject.SetActive(showNotifications);

        if (_reincarnateButton != null)
        {
            var rt  = _reincarnateButton.transform as RectTransform;
            var pos = rt.anchoredPosition;
            pos.y = -(_reincarnateY - (showNotifications ? 0f : _notificationRowH));
            rt.anchoredPosition = pos;
        }

        if (_panelRect == null || _panelFullH <= 0f) return;

        float h = _panelFullH;
        if (!inBattle)          h -= _surrenderRowH;
        if (!showNotifications) h -= _notificationRowH;
        _panelRect.sizeDelta = new Vector2(_panelRect.sizeDelta.x, h);
        if (_borderRect != null)
            _borderRect.sizeDelta = new Vector2(_borderRect.sizeDelta.x, h + BorderOutset);
    }

    // ── 사운드 볼륨 ──────────────────────────────────────────

    void SetSfxVolume(float volume)
    {
        var s = UserDataManager.Instance.Get<BattleSettingsData>();
        s.SetSfxVolume(volume);
        UserDataManager.Instance.RequestSave();
        _sfxValue.text = VolumeText(volume);
    }

    void SetBgmVolume(float volume)
    {
        var s = UserDataManager.Instance.Get<BattleSettingsData>();
        s.SetBgmVolume(volume);
        UserDataManager.Instance.RequestSave();
        _bgmValue.text = VolumeText(volume);
    }

    void RefreshSound()
    {
        var s = UserDataManager.Instance.Get<BattleSettingsData>();
        _sfxSlider.SetValueWithoutNotify(s.SavedSfxVolume);
        _sfxValue.text = VolumeText(s.SavedSfxVolume);
        _bgmSlider.SetValueWithoutNotify(s.SavedBgmVolume);
        _bgmValue.text = VolumeText(s.SavedBgmVolume);
    }

    void SetLanguage(int index)
    {
        var localization = LocalizationManager.Instance;
        localization.SetLanguageIndex(index);
        RefreshLanguage();
    }

    void RefreshLanguage()
    {
        var localization = LocalizationManager.Instance;
        _languageDropdown.SetValueWithoutNotify(localization.CurrentLanguageIndex);
        _languageDropdown.RefreshShownValue();
    }

    void ToggleNotifications()
    {
        AndroidLocalNotificationManager.ToggleFromSettings();
        RefreshNotifications();
    }

    void RefreshNotifications()
    {
        if (!AndroidLocalNotificationManager.IsSupported) return;
        if (AndroidLocalNotificationManager.PermissionNeeded)
        {
            _notificationPill.color = new Color(0.58f, 0.38f, 0.12f, 1f);
            _notificationState.text = LocalizationManager.Instance.Get("권한 필요");
            return;
        }

        SetPill(_notificationPill, _notificationState, AndroidLocalNotificationManager.Enabled);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) RefreshNotifications();
    }

    /// <summary>
    /// 상태 알약을 갱신한다.
    ///
    /// ⚠ 버튼 본체(Body) 색은 건드리지 않는다
    ///   Body 는 Button.targetGraphic 이라 눌림 색이 그 색에 곱해진다 —
    ///   여기서 바꾸면 EditorUIBuilder.TintFor 로 역산해 둔 눌림 색이 어긋난다.
    ///   그래서 상태는 버튼 위에 얹은 별도 이미지로 보여 준다 (TopBarUI 의 배속 띠와 같은 방식).
    /// </summary>
    static void SetPill(Image pill, TextMeshProUGUI label, bool on)
    {
        if (pill  != null) pill.color = on ? PillOn : PillOff;
        if (label != null) label.text = LocalizationManager.Instance.Get(on ? "켜짐" : "꺼짐");
    }

    static string VolumeText(float volume) => $"{Mathf.RoundToInt(volume * 100f)}%";

    // ── 버튼 핸들러 ──────────────────────────────────────────

    /// <summary>
    /// 즉시 환생 — 이 팝업이 완전히 닫힌 **뒤에** 패배를 처리한다.
    /// 여기서 바로 Surrender() 를 부르면 환생 팝업이 열리는 도중에
    /// 이 팝업의 닫기 애니메이션이 겹쳐 두 팝업이 동시에 보인다.
    /// </summary>
    void OnReincarnateClicked()
    {
        _surrendering = true;
        Close();
    }
}
