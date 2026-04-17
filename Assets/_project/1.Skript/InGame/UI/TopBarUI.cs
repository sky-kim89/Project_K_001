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
//    - 배속 버튼 (1× / 2× / 3×  → Time.timeScale 직접 설정)
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

    [Header("배속 버튼")]
    [SerializeField] Button _speed1xButton;
    [SerializeField] Button _speed2xButton;
    [SerializeField] Button _speed3xButton;
    [SerializeField] Color  _activeSpeedColor   = new Color(1.00f, 0.80f, 0.20f, 1f);
    [SerializeField] Color  _inactiveSpeedColor = new Color(0.50f, 0.50f, 0.50f, 1f);

    [Header("일시 정지")]
    [SerializeField] Button _pauseButton;

    // ── 런타임 상태 ─────────────────────────────────────────────
    int         _killCount;
    float       _waveElapsed;
    BattleState _prevState = BattleState.None;
    EntityManager _em;
    EntityQuery   _bossQuery;

    // ── 초기화 ──────────────────────────────────────────────────

    void Awake()
    {
        _speed1xButton?.onClick.AddListener(() => SetSpeed(1f));
        _speed2xButton?.onClick.AddListener(() => SetSpeed(2f));
        _speed3xButton?.onClick.AddListener(() => SetSpeed(3f));
        UpdateSpeedButtonUI(1f);

        _pauseButton?.onClick.AddListener(OpenPausePopup);

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

        if (_waveText         != null) _waveText.text              = $"Wave {current} / {total}";
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
        if (_bossHpText != null) _bossHpText.text       = $"{Mathf.CeilToInt(cur)} / {Mathf.RoundToInt(maxHp)}";
    }

    void RefreshKillCount()
    {
        if (_killCountText != null)
            _killCountText.text = _killCount.ToString();
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

    // ── 배속 버튼 ─────────────────────────────────────────────

    void SetSpeed(float speed)
    {
        Time.timeScale = speed;
        UpdateSpeedButtonUI(speed);
    }

    void UpdateSpeedButtonUI(float currentSpeed)
    {
        SetBtnColor(_speed1xButton, Mathf.Approximately(currentSpeed, 1f));
        SetBtnColor(_speed2xButton, Mathf.Approximately(currentSpeed, 2f));
        SetBtnColor(_speed3xButton, Mathf.Approximately(currentSpeed, 3f));
    }

    void SetBtnColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? _activeSpeedColor : _inactiveSpeedColor;
    }

    // ── 유틸 ─────────────────────────────────────────────────

    static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return m > 0 ? $"{m}:{s:D2}" : $"{s}s";
    }
}
