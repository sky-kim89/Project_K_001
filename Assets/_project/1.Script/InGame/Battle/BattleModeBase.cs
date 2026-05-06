using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  BattleModeBase.cs
//  배틀 모드 추상 베이스 클래스.
//
//  파생 클래스 목록 (예정):
//    NormalMode       — 일반 모드
//    GoldDungeon      — 골드 던전 (추후 구현)
//    SpecialDungeon   — 특수 던전 (추후 구현)
//
//  BattleManager 가 이 클래스를 통해 모드와 통신한다.
//  모드별로 웨이브 구성, 스포너 설정, 보상 등을 오버라이드한다.
// ============================================================

public abstract class BattleModeBase
{
    /// <summary>이 모드의 BattleMode 값. BattleContext.Mode 설정에 사용된다.</summary>
    public abstract BattleMode Mode { get; }

    protected BattleContext  Context;
    protected AllySpawner    AllySpawner;
    protected EnemySpawner   EnemySpawner;

    // ── 초기화 ────────────────────────────────────────────────

    /// <summary>
    /// 배틀 시작 시 BattleManager 가 호출.
    /// context, 스포너 참조를 주입하고 초기 설정을 수행한다.
    /// </summary>
    public virtual void Initialize(BattleContext context, AllySpawner ally, EnemySpawner enemy)
    {
        Context      = context;
        AllySpawner  = ally;
        EnemySpawner = enemy;

        Context.TotalWaves  = GetTotalWaves();
        Context.CurrentWave = 0;
    }

    // ── 파생 클래스에서 구현해야 하는 메서드 ─────────────────

    /// <summary>총 웨이브 수를 반환한다.</summary>
    protected abstract int GetTotalWaves();

    /// <summary>
    /// 현재 웨이브의 아군 스폰 항목을 반환한다.
    /// null 또는 빈 리스트 반환 시 아군 스폰 생략.
    /// </summary>
    public abstract List<SpawnEntry> GetAllySpawnEntries(int wave);

    /// <summary>
    /// 현재 웨이브의 적군 스폰 항목을 반환한다.
    /// </summary>
    public abstract List<SpawnEntry> GetEnemySpawnEntries(int wave);

    /// <summary>
    /// 스테이지 클리어(전 웨이브 완료) 시 보상을 계산해 context 에 누적한다.
    /// 파생 클래스에서 보상 종류(골드·아이템 등)를 다르게 구성할 수 있다.
    /// </summary>
    public abstract void ApplyStageClearReward();

    // ── 파생 클래스에서 오버라이드 가능한 훅 ─────────────────

    /// <summary>각 웨이브 시작 직전 호출. 연출·로직 추가 가능.</summary>
    public virtual void OnWaveStart(int wave) { }

    /// <summary>웨이브 클리어 직후 호출. 보상 계산 전에 실행됨.</summary>
    public virtual void OnWaveClear(int wave) { }

    /// <summary>배틀 승리 시 호출.</summary>
    public virtual void OnBattleVictory() { }

    /// <summary>배틀 패배 시 호출.</summary>
    public virtual void OnBattleDefeat() { }
}
