using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  TopBarUI.cs
//  인게임 상단 HUD.
//
//  표시 항목:
//    - 웨이브 텍스트 / 진행 바 / 경과 타이머
//    - 보스 HP 바 (BossComponent 엔티티가 존재할 때만 표시)
//    - 적 처치 수 (BattleManager.OnUnitKilled 이벤트)
//    - 우상단: [AUTO 토글] [배속 토글] [일시 정지]
//
//  ■ 배속은 버튼 하나짜리 토글이다
//    누를 때마다 1× → 2× → 3× → 1× 로 돌아간다.
//    ⚠ 단, 도는 범위는 '시간의 고삐'(R_BattleSpeed) 유물이 정한다.
//      0레벨 = 1× 고정(버튼 비활성), Lv1 = 2× 까지, Lv2 = 3× 까지.
//    ⚠ 예전엔 1×/2×/3× 버튼 3개가 따로 있어 상단 폭을 셋이서 나눠 먹었다.
//      셋 중 하나만 켜져 있으므로 정보량은 같은데 자리만 3배로 썼다.
//
//  ■ AUTO 는 아군 장수 스킬 자동 사용 토글이다
//    꺼져 있으면 장수 카드의 스킬 슬롯을 눌러야만 스킬이 나간다
//    (ActiveSkillAISystem 이 BattleSettingsData.AutoSkillEnabled 를 본다).
//
//  ■ 배속·AUTO 는 둘 다 저장된다 (BattleSettingsData)
//    누를 때마다 섹션에 쓰고 RequestSave() 한다. 다음 전투에 그대로 복원된다.
//
//  ⚠ 씬을 떠날 때 timeScale 을 1 로 되돌린다
//    저장된 3× 를 그대로 두고 로비로 나가면 로비 연출까지 3배로 돈다.
//    저장값(SpeedIndex)은 남으므로 다음 전투에서는 다시 3× 로 시작한다.
//
//  ⚠ 일시 정지 중에는 배속을 바꾸지 않는다
//    PausePopup 이 timeScale=0 을 걸고 닫힐 때 이전 값으로 되돌린다.
//    그 사이에 배속을 건드리면 팝업이 0 을 덮어써서 게임이 멈춘 채로 돌아온다.
// ============================================================

public class TopBarUI : MonoBehaviour
{
    [Header("웨이브 정보")]
    [SerializeField] TextMeshProUGUI _waveText;
    [SerializeField] Image           _waveProgressFill;
    [SerializeField] TextMeshProUGUI _waveTimerText;

    [Header("보스 HP (보스 없을 때 숨김)")]
    [SerializeField] GameObject      _bossHpRoot;
    [SerializeField] Image           _bossHpFill;
    [SerializeField] TextMeshProUGUI _bossHpText;

    [Header("보스 스킬 쿨다운 (HP 바 아래 작은 아이콘)")]
    [SerializeField] Image[]           _bossSkillIcons;     // 아이콘
    [SerializeField] Image[]           _bossSkillCooldowns; // Radial360 오버레이
    [SerializeField] TextMeshProUGUI[] _bossSkillTimers;    // 남은 초 숫자

    [Header("킬 카운터")]
    [SerializeField] TextMeshProUGUI _killCountText;

    [Header("배속 토글 (누를 때마다 1× → 2× → 3×)")]
    [SerializeField] Button          _speedButton;
    [SerializeField] TextMeshProUGUI _speedLabel;
    // ⚠ 버튼 본체가 아니라 그 위에 얹은 색 띠를 가리킨다.
    //   본체는 Button.targetGraphic 이라 색을 바꾸면 눌림 색 역산이 어긋난다.
    [SerializeField] Image           _speedFace;

    [Tooltip("배속이 잠겨 있을 때 이유를 알려 주는 툴팁. 배속 버튼 아래로 펼쳐진다.")]
    [SerializeField] InfoTooltipUI    _speedLockTooltip;

    [Header("AUTO 토글 (스킬 자동 사용)")]
    [SerializeField] Button          _autoButton;
    [SerializeField] TextMeshProUGUI _autoLabel;
    // 배속과 같은 이유로 버튼 본체가 아니라 그 위 색 띠를 가리킨다.
    [SerializeField] Image           _autoFace;

    [Header("일시 정지")]
    [SerializeField] Button _pauseButton;

    /// <summary>토글 순서. 여기에 값을 더하면 버튼이 그대로 따라간다.</summary>
    static readonly float[] SpeedSteps = { 1f, 2f, 3f };

    // ⚠ 처음부터 3× 를 쓸 수 있는 게 아니다
    //   '시간의 고삐'(R_BattleSpeed) 유물이 여는 만큼만 돈다 —
    //   0레벨이면 1× 고정이라 버튼이 아예 안 눌리고, Lv1 이면 1×↔2×, Lv2 라야 3× 까지 간다.
    //   SpeedSteps 를 직접 세지 말 것. 유물을 빼먹으면 잠금이 통째로 풀린다.
    static int UnlockedSpeedCount
        => Mathf.Clamp(RelicApplier.GetBattleSpeedStepCount(), 1, SpeedSteps.Length);

    // 잠겨 있을 때 눌렀을 때의 안내. 유물 이름을 직접 박아 두지 않는다 —
    // 유물 이름이 바뀌면 여기도 같이 틀려지므로 DB 에서 읽어 온다.
    const string SpeedLockTitle = "전투 배속";
    const string SpeedLockDesc  = "배속 유물을 습득한 뒤 시도해 주세요.";

    static readonly Color[] SpeedColors =
    {
        new Color(0.18f, 0.30f, 0.46f, 1f),   // 1× — 차분한 남색
        new Color(0.24f, 0.46f, 0.36f, 1f),   // 2× — 청록
        new Color(0.52f, 0.36f, 0.10f, 1f),   // 3× — 금빛 (가장 빠름)
    };

    // 배속이 잠겼을 때의 띠 색 — 1× 정상 색보다 확실히 죽은 회색.
    static readonly Color SpeedLockedFace = new Color(0.26f, 0.27f, 0.32f, 1f);

    // AUTO 켜짐/꺼짐 — 띠 색과 글자 색을 함께 바꾼다.
    // 6px 띠 하나만으로는 켜졌는지 한눈에 안 잡힌다.
    static readonly Color AutoOnFace   = new Color(0.30f, 0.82f, 0.45f, 1f);
    static readonly Color AutoOffFace  = new Color(0.28f, 0.30f, 0.38f, 1f);
    static readonly Color AutoOnLabel  = Color.white;
    static readonly Color AutoOffLabel = new Color(0.55f, 0.58f, 0.66f, 1f);

    // ── 런타임 상태 ─────────────────────────────────────────────
    int         _killCount;
    float       _waveElapsed;
    int         _speedIndex;
    BattleState _prevState = BattleState.None;
    EntityManager _em;
    EntityQuery   _bossQuery;

    // ── 초기화 ──────────────────────────────────────────────────

    void Awake()
    {
        _speedButton?.onClick.AddListener(CycleSpeed);
        _autoButton?.onClick.AddListener(ToggleAuto);
        _pauseButton?.onClick.AddListener(OpenPausePopup);

        // 저장된 조작 설정 복원 (배속 · 자동 스킬)
        var settings = UserDataManager.Instance.Get<BattleSettingsData>();
        // 유물을 초기화(환생 리셋)했으면 저장된 3× 가 남아 있어도 잠긴 단계다 — 잘라 낸다.
        _speedIndex = Mathf.Clamp(settings.SpeedIndex, 0, UnlockedSpeedCount - 1);
        ApplySpeed();
        ApplyAuto(settings.AutoSkill);

        BattleManager.OnUnitKilled     += HandleUnitKilled;

        // ⚠ 처치 수·경과 시간은 '이번 전투' 의 값이다
        //   씬이 상주하면서 이 오브젝트도 계속 살아 있으므로,
        //   새 전투를 준비할 때 직접 0 으로 되돌려야 지난 판 숫자가 이어지지 않는다.
        BattleManager.OnBattlePrepared += ResetBattleCounters;
    }

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        _em = world.EntityManager;
        _bossQuery = _em.CreateEntityQuery(
            ComponentType.ReadOnly<BossComponent>(),
            ComponentType.ReadOnly<HealthComponent>(),
            ComponentType.ReadOnly<StatComponent>(),
            ComponentType.Exclude<DeadTag>());
    }

    void OnDestroy()
    {
        BattleManager.OnUnitKilled     -= HandleUnitKilled;
        BattleManager.OnBattlePrepared -= ResetBattleCounters;
        if (_em != default) _bossQuery.Dispose();

        // 배속을 켠 채 씬을 나가면 로비까지 그 속도로 돈다 (저장값은 그대로 둔다)
        Time.timeScale = 1f;
    }

    // ── 프레임 갱신 ─────────────────────────────────────────────

    void LateUpdate()
    {
        if (BattleManager.Instance == null) return;

        var ctx = BattleManager.Instance.Context;
        if (ctx == null) return;

        // 웨이브 타이머: InWave 상태 진입 시 리셋 후 증가
        if (ctx.State == BattleState.InWave && _prevState != BattleState.InWave)
            _waveElapsed = 0f;

        if (ctx.State == BattleState.InWave)
            _waveElapsed += Time.deltaTime;

        _prevState = ctx.State;

        RefreshWave(ctx);
        RefreshBossHp();
        RefreshKillCount();
    }

    // ── 세부 갱신 ─────────────────────────────────────────────

    void RefreshWave(BattleContext ctx)
    {
        int   total   = Mathf.Max(1, ctx.TotalWaves);
        int   current = Mathf.Max(1, ctx.CurrentWave);  // 0 방지 (초기화 전 프레임 대응)
        float progress = total > 1 ? Mathf.Clamp01((float)(current - 1) / (total - 1)) : 1f;

        // 무한 보스 구간에서는 웨이브 대신 몇 번째 보스인지 보여준다
        if (_waveText != null)
            _waveText.text = ctx.EndlessBossIndex > 0
                ? $"무한 보스 {ctx.EndlessBossIndex}"
                : $"웨이브 {current} / {total}";
        if (_waveProgressFill != null) _waveProgressFill.fillAmount = progress;
        if (_waveTimerText    != null) _waveTimerText.text          = FormatTime(_waveElapsed);
    }

    void RefreshBossHp()
    {
        if (_bossHpRoot == null || _em == default) return;

        _em.CompleteAllTrackedJobs();

        if (_bossQuery.IsEmpty)
        {
            _bossHpRoot.SetActive(false);
            return;
        }

        _bossHpRoot.SetActive(true);

        var arr  = _bossQuery.ToEntityArray(Allocator.Temp);
        var boss = arr[0];
        arr.Dispose();

        float cur   = _em.GetComponentData<HealthComponent>(boss).CurrentHp;
        float maxHp = Mathf.Max(1f, _em.GetComponentData<StatComponent>(boss).Final[StatType.MaxHp]);
        float ratio = Mathf.Clamp01(cur / maxHp);

        if (_bossHpFill != null) _bossHpFill.fillAmount = ratio;
        if (_bossHpText != null) _bossHpText.text       = $"보스   {Mathf.CeilToInt(cur):N0} / {Mathf.RoundToInt(maxHp):N0}";

        RefreshBossSkills(boss);
    }

    // ── 보스 스킬 쿨다운 ──────────────────────────────────────
    //
    //  대표 스킬 1개 + 패턴 슬롯(돌진·분쇄 강타)을 한 줄에 늘어놓는다.
    //  보스가 뭘 들고 있고 언제 터지는지 보이면 "갑자기 죽었다" 가 줄어든다.
    //  오토배틀이라 피할 수는 없지만, 무슨 일이 벌어질지는 알아야 한다.
    void RefreshBossSkills(Entity boss)
    {
        if (_bossSkillIcons == null || _bossSkillIcons.Length == 0) return;

        int idx = 0;
        var sprites = SpriteManager.Instance;

        // ① 대표 스킬
        if (_em.HasComponent<GeneralActiveSkillComponent>(boss))
        {
            var s = _em.GetComponentData<GeneralActiveSkillComponent>(boss);
            ShowBossSkill(idx++, (ActiveSkillId)s.SkillId, s.CooldownRemaining, s.Cooldown, sprites);
        }

        // ② 패턴 슬롯
        if (_em.HasBuffer<ActiveSkillSlot>(boss))
        {
            var slots = _em.GetBuffer<ActiveSkillSlot>(boss, true);
            for (int i = 0; i < slots.Length && idx < _bossSkillIcons.Length; i++)
                ShowBossSkill(idx++, (ActiveSkillId)slots[i].SkillId,
                              slots[i].CooldownRemaining, slots[i].Cooldown, sprites);
        }

        for (; idx < _bossSkillIcons.Length; idx++)
            if (_bossSkillIcons[idx] != null)
                _bossSkillIcons[idx].transform.parent.gameObject.SetActive(false);
    }

    void ShowBossSkill(int i, ActiveSkillId id, float remaining, float total, SpriteManager sprites)
    {
        if (i >= _bossSkillIcons.Length || _bossSkillIcons[i] == null) return;

        var icon = _bossSkillIcons[i];
        icon.transform.parent.gameObject.SetActive(true);

        string key = id.IconKey();
        icon.sprite = (sprites != null && key != null) ? sprites.Get(key) : null;
        icon.color  = icon.sprite != null ? Color.white : new Color(0.3f, 0.3f, 0.42f);

        bool ready = remaining <= 0f;

        if (_bossSkillCooldowns != null && i < _bossSkillCooldowns.Length
                                        && _bossSkillCooldowns[i] != null)
        {
            _bossSkillCooldowns[i].gameObject.SetActive(!ready);
            _bossSkillCooldowns[i].fillAmount =
                ready ? 0f : Mathf.Clamp01(remaining / Mathf.Max(total, 0.001f));
        }

        // 남은 초를 숫자로. 링만으로는 "곧 온다" 가 정확히 안 읽힌다.
        // 올림이라 마지막 1초 동안 "1" 로 머문다 — 소수점이 빠르게 굴러가면
        // 오히려 시선을 끌어 전투를 못 본다. 장수 카드 쿨다운도 같은 규칙이다.
        if (_bossSkillTimers != null && i < _bossSkillTimers.Length
                                     && _bossSkillTimers[i] != null)
        {
            var t = _bossSkillTimers[i];
            t.gameObject.SetActive(!ready);
            if (!ready) t.text = Mathf.CeilToInt(remaining).ToString();
        }
    }

    void RefreshKillCount()
    {
        if (_killCountText != null)
            _killCountText.text = $"처치 {_killCount}";
    }

    // ── 이벤트 ─────────────────────────────────────────────────

    /// <summary>새 전투 준비 — 처치 수·경과 시간을 0 으로.</summary>
    void ResetBattleCounters()
    {
        _killCount   = 0;
        _waveElapsed = 0f;
        RefreshKillCount();

        // ⚠ 배속 잠금은 전투를 준비할 때마다 다시 본다
        //   Awake 는 인게임 씬이 처음 올라올 때 딱 한 번만 돈다. 씬이 상주하므로
        //   그 뒤에 로비에서 '시간의 고삐'를 강화해도 버튼은 계속 잠긴 채였다
        //   — 유물을 배웠는데 배속이 안 눌리던 원인이다.
        //   반대로 환생으로 유물이 초기화되면 저장된 3× 를 다시 잘라내야 한다.
        var settings = UserDataManager.Instance?.Get<BattleSettingsData>();
        _speedIndex  = Mathf.Clamp(settings?.SpeedIndex ?? 0, 0, UnlockedSpeedCount - 1);
        ApplySpeed();
    }

    void HandleUnitKilled(TeamType team)
    {
        if (team == TeamType.Enemy)
        {
            _killCount++;
            RefreshKillCount();
        }
    }

    // ── 일시 정지 ─────────────────────────────────────────────

    void OpenPausePopup()
    {
        if (PopupManager.Instance == null) return;
        if (PopupManager.Instance.IsOpen(PopupType.Pause)) return;
        PopupManager.Instance.Open(PopupType.Pause);
    }

    // ── 배속 토글 ─────────────────────────────────────────────

    void CycleSpeed()
    {
        // 일시 정지 중(timeScale=0)이면 무시 — PausePopup 이 닫히며 값을 되돌린다.
        // 여기서 바꾸면 그 복원값이 0 으로 덮여 게임이 멈춘 채 돌아온다.
        if (Time.timeScale <= 0f) return;

        // 아직 못 여는 상태면 왜 안 되는지 알려 준다.
        //
        // ⚠ 버튼을 비활성(interactable=false)으로 두면 안 된다
        //   눌러도 onClick 이 오지 않아 이유를 말할 기회 자체가 없다.
        //   회색으로 죽어 있는 버튼은 "고장" 으로 읽히지, "조건 미달" 로 읽히지 않는다.
        if (UnlockedSpeedCount <= 1)
        {
            ShowSpeedLockTooltip();
            return;
        }

        // 해금된 단계 안에서만 순환한다 (유물 0레벨이면 1× 하나뿐이라 제자리).
        _speedIndex = (_speedIndex + 1) % UnlockedSpeedCount;
        ApplySpeed();

        var settings = UserDataManager.Instance.Get<BattleSettingsData>();
        settings.SetSpeedIndex(_speedIndex);
        UserDataManager.Instance.RequestSave();
    }

    void ApplySpeed()
    {
        float speed = SpeedSteps[_speedIndex];
        Time.timeScale = speed;

        if (_speedLabel != null) _speedLabel.text  = $"{speed:0}×";
        if (_speedFace  != null) _speedFace.color  = SpeedColors[_speedIndex];

        // ⚠ 잠겨 있어도 버튼은 살려 둔다
        //   예전엔 여기서 interactable=false 를 줬는데, 그러면 눌러도 아무 일이
        //   일어나지 않아 "왜 안 되는지" 를 말할 자리가 사라진다.
        //   대신 눌렀을 때 CycleSpeed 가 툴팁으로 답한다.
        if (_speedButton != null) _speedButton.interactable = true;

        // 잠긴 상태는 색으로 미리 알린다 — 눌러 보기 전에도 구분되게.
        if (_speedFace != null && UnlockedSpeedCount <= 1)
            _speedFace.color = SpeedLockedFace;
    }

    /// <summary>
    /// 배속이 왜 안 되는지 알려 준다.
    ///
    /// 유물 이름은 DB 에서 읽는다 — 여기에 문자열로 박아 두면 유물 이름을
    /// 고쳤을 때 안내만 옛 이름으로 남는다.
    /// </summary>
    void ShowSpeedLockTooltip()
    {
        if (_speedLockTooltip == null) return;

        string relicName = RelicDatabase.Current != null
            ? RelicDatabase.Current.NameOfSystemEffect(RelicSystemEffect.BattleSpeedUnlock)
            : null;

        string desc = string.IsNullOrEmpty(relicName)
            ? SpeedLockDesc
            : $"'{relicName}' 유물을 습득한 뒤 시도해 주세요.";

        _speedLockTooltip.ShowAnchored(
            _speedButton.transform as RectTransform, SpeedLockTitle, desc, "");
    }

    // ── 자동 스킬 토글 ─────────────────────────────────────────

    void ToggleAuto()
    {
        ApplyAuto(!BattleSettingsData.AutoSkillEnabled);
        UserDataManager.Instance.RequestSave();
    }

    /// <summary>설정값 기록 + 버튼 표시를 한 번에. 상태의 정본은 BattleSettingsData 다.</summary>
    void ApplyAuto(bool on)
    {
        UserDataManager.Instance.Get<BattleSettingsData>().SetAutoSkill(on);

        if (_autoFace  != null) _autoFace.color  = on ? AutoOnFace  : AutoOffFace;
        if (_autoLabel != null) _autoLabel.color = on ? AutoOnLabel : AutoOffLabel;
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return m > 0 ? $"{m}:{s:D2}" : $"{s}s";
    }
}
