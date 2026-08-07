using System;
using System.Collections.Generic;

// ============================================================
//  RunSequenceGenerator.cs
//  런(30스테이지)의 스테이지 타입 시퀀스를 무작위로 생성.
//
//  고정 규칙:
//    - 1스테이지(index 0)      → 항상 일반
//    - 5의 배수 스테이지        → 엘리트 (index 4, 9, 14, 19, 24, 29)
//    - 2~4스테이지(index 1~3)  → 이 중 한 칸은 반드시 상점
//  랜덤 배치:
//    - 상점  : 위 확정 1개 포함 총 4개
//    - 이벤트: 3개
//
//  ⚠ 1스테이지를 일반으로 고정하는 이유
//    런 시작(= 신규 시작·패배 후 환생) 직후 로비에 들어서자마자
//    상점·이벤트 팝업이 자동으로 뜨면 무슨 상황인지 알 수 없다.
//    첫 스테이지는 전투로 시작해 바로 진입할 수 있어야 한다.
//
//  ⚠ 2~4스테이지에 상점을 하나 보장하는 이유
//    첫 전투 보상을 바로 써 볼 수 있어야 런의 성장 흐름이 잡힌다.
//    완전 무작위면 상점이 10스테이지 뒤에 몰릴 수 있다.
//
//  ⚠ 규칙을 바꾸면 IsValid() 도 같이 고칠 것
//    시퀀스는 세이브에 남는다. IsValid() 가 옛 세이브를 걸러 내
//    StageProgressData.EnsureRunSequence() 가 새로 뽑게 한다.
// ============================================================

public static class RunSequenceGenerator
{
    /// <summary>상점이 반드시 하나 들어가는 구간 (index — 2~4스테이지).</summary>
    public const int GuaranteedShopMin = 1;
    public const int GuaranteedShopMax = 3;

    const int ShopCount  = 4;
    const int EventCount = 3;

    public static RunStageType[] Generate(int total = 30)
    {
        var seq = new RunStageType[total];
        var rng = new Random();

        // 기본: 일반
        for (int i = 0; i < total; i++) seq[i] = RunStageType.Normal;

        // 5의 배수 → 엘리트 (index 4, 9, 14, 19, 24, 29)
        for (int i = 4; i < total; i += 5)
            seq[i] = RunStageType.Elite;

        // 배치 후보 = index 1 이후의 일반 스테이지 (첫 스테이지는 후보에서 제외)
        var pool = new List<int>();
        for (int i = 1; i < total; i++)
            if (seq[i] == RunStageType.Normal) pool.Add(i);

        // ① 2~4스테이지 중 한 칸은 확정 상점
        int guaranteed = PickInRange(pool, GuaranteedShopMin, GuaranteedShopMax, rng);
        seq[guaranteed] = RunStageType.Shop;
        pool.Remove(guaranteed);

        // ② 나머지 상점 + 이벤트 무작위 배치
        PlaceRandom(seq, pool, RunStageType.Shop,  ShopCount - 1, rng);
        PlaceRandom(seq, pool, RunStageType.Event, EventCount,    rng);

        return seq;
    }

    /// <summary>
    /// 저장된 시퀀스가 현재 규칙을 지키는지 검사.
    /// 규칙을 바꾼 뒤에도 옛 세이브가 그대로 쓰이는 것을 막는다.
    /// </summary>
    public static bool IsValid(RunStageType[] seq)
    {
        if (seq == null || seq.Length == 0) return false;
        if (seq[0] != RunStageType.Normal) return false;

        for (int i = GuaranteedShopMin; i <= GuaranteedShopMax && i < seq.Length; i++)
            if (seq[i] == RunStageType.Shop) return true;

        return false;
    }

    // ── 내부 ─────────────────────────────────────────────────

    // 구간에 후보가 없으면 그대로 터뜨린다 — 규칙이 깨진 채로 넘어가면 안 된다.
    static int PickInRange(List<int> pool, int min, int max, Random rng)
    {
        var candidates = pool.FindAll(i => i >= min && i <= max);
        return candidates[rng.Next(candidates.Count)];
    }

    static void PlaceRandom(RunStageType[] seq, List<int> pool, RunStageType type, int count, Random rng)
    {
        for (int placed = 0; placed < count && pool.Count > 0; placed++)
        {
            int pick = rng.Next(pool.Count);
            seq[pool[pick]] = type;
            pool.RemoveAt(pick);
        }
    }
}
