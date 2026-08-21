using UnityEngine;

// ============================================================
//  CodexApplier.cs
//  도감 수집 보너스를 스탯에 적용하는 단일 진입점.
//
//  수집 1종당 공격력·체력 +0.5% (CodexData.BonusPerEntry).
//
//  ■ 경로가 하나다
//    HeroStatPipeline 6단계에서 ApplyToGeneralStat 한 번만 불린다.
//    표시도 전투도 그 결과를 읽으므로 어긋날 자리가 없다.
//
//  ■ 장군에게만 건다
//    병사 스탯은 장군 스탯 × SoldierRuntimeBridge.StatRatio 로 파생되므로
//    장군이 오르면 병사도 자동으로 오른다.
//    ⚠ 병사에 따로 걸면 이중 적용이다.
//
//  ■ 배율은 여정 시작 시점에 박힌다
//    여정 도중에 새로 채운 도감은 이번 여정에 즉시 반영되지 않고 다음 여정부터
//    걸린다 (CodexData.LockForRun). 표시용 PendingRatio 가 그 예정값이다.
//
//  ■ 왜 공격력·체력뿐인가
//    방어율·쿨감처럼 0~1 비율 스탯에 %를 곱하면 수집이 쌓일수록
//    상한에 그냥 도달해 버린다. 도감은 "전반적으로 세진다" 를 담당하고
//    특수 스탯은 유물·특성이 담당한다.
// ============================================================

public static class CodexApplier
{
    /// <summary>도감 버프가 붙는 스탯.</summary>
    static readonly StatType[] BuffedStats = { StatType.Attack, StatType.MaxHp };

    /// <summary>
    /// 지금 실제로 걸리는 도감 배율. 수집이 없거나 세이브가 없으면 0.
    /// ⚠ 여정 중에는 시작 시점에 박힌 값이다 (CodexData.LockForRun).
    /// </summary>
    public static float BonusRatio
    {
        get
        {
            var codex = UserDataManager.Instance?.Get<CodexData>();
            return codex != null ? codex.StatBonusRatio : 0f;
        }
    }

    /// <summary>다음 여정부터 걸릴 배율 — 여정 중에 채운 것까지 포함한다.</summary>
    public static float PendingRatio
    {
        get
        {
            var codex = UserDataManager.Instance?.Get<CodexData>();
            return codex != null ? codex.PendingStatBonusRatio : 0f;
        }
    }

    /// <summary>장군 UnitStat 에 도감 몫을 더한다 (표시·전투 공용 — HeroStatPipeline 6단계).</summary>
    public static void ApplyToGeneralStat(UnitStat stat)
    {
        float ratio = BonusRatio;
        if (stat == null || ratio <= 0f) return;

        foreach (var type in BuffedStats)
        {
            float delta = stat.Get(type) * ratio;
            if (Mathf.Abs(delta) < 0.001f) continue;

            stat.Add(type, delta, "codex");
        }
    }
}
