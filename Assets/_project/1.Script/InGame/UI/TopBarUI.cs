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
//    - 우상단: [배속 토글] [일시 정지]
//
//  ■ 배속은 버튼 하나짜리 토글이다
//    누를 때마다 1× → 2× → 3× → 1× 로 돌아간다.
//    ⚠ 예전엔 1×/2×/3× 버튼 3개가 따로 있어 상단 폭을 셋이서 나눠 먹었다.
//      셋 중 하나만 켜져 있으므로 정보량은 같은데 자리만 3배로 썼다.
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

    [Header("킬 카운터")]
    [SerializeField] TextMeshProUGUI _killCountText;

    [Header("배속 토글 (누를 때마다 1× → 2× → 3×)")]
    [SerializeField] Button          _speedButton;
    [SerializeField] TextMeshProUGUI _speedLabel;
    // ⚠ 버튼 본체가 아니라 그 위에 얹은 색 띠를 가리킨다.
    //   본체는 Button.targetGraphic 이라 색을 바꾸면 눌림 색 역산이 어긋난다.
    [SerializeField] Image           _speedFace;

    [Header("일시 정지")]
    [SerializeField] Button _pauseButton;

    /// <summary>토글 순서. 여기에 값을 더하면 버튼이 그대로 따라간다.</summary>
    static readonly float[] SpeedSteps = { 1f, 2f, 3f };

    static readonly Color[] SpeedColors =
    {
        new Color(0.18f, 0.30f, 0.46f, 1f),   // 1× — 차분한 남색
        new Color(0.24f, 0.46f, 0.36f, 1f),   // 2× — 청록
        new Color(0.52f, 0.36f, 0.10f, 1f),   // 3× — 금빛 (가장 빠름)
    };

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
        _pauseButton?.onClick.AddListener(OpenPausePopup);

        _speedIndex = 0;
        ApplySpeed();

        BattleManager.OnUnitKilled += HandleUnitKilled;
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
        BattleManager.OnUnitKilled -= HandleUnitKilled;
        if (_em != default) _bossQuery.Dispose();
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
    }

    void RefreshKillCount()
    {
        if (_killCountText != null)
            _killCountText.text = $"처치 {_killCount}";
    }

    // ── 이벤트 ─────────────────────────────────────────────────

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

        _speedIndex = (_speedIndex + 1) % SpeedSteps.Length;
        ApplySpeed();
    }

    void ApplySpeed()
    {
        float speed = SpeedSteps[_speedIndex];
        Time.timeScale = speed;

        if (_speedLabel != null) _speedLabel.text  = $"{speed:0}×";
        if (_speedFace  != null) _speedFace.color  = SpeedColors[_speedIndex];
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return m > 0 ? $"{m}:{s:D2}" : $"{s}s";
    }
}
