using System;
using System.Collections.Generic;

// ============================================================
//  RunSequenceGenerator.cs
//  런(30스테이지)의 스테이지 타입 시퀀스를 무작위로 생성.
//
//  고정 규칙:
//    - 5의 배수 스테이지(5, 10, 15, 20, 25, 30) → 엘리트
//  랜덤 배치:
//    - 상점: 비체크포인트 중 4개 무작위 배치
//    - 이벤트: 비체크포인트 중 3개 무작위 배치 (추후 구현)
// ============================================================

public static class RunSequenceGenerator
{
    public static RunStageType[] Generate(int total = 30)
    {
        var seq = new RunStageType[total];
        var rng = new Random();

        // 기본: 일반
        for (int i = 0; i < total; i++) seq[i] = RunStageType.Normal;

        // 5의 배수 → 엘리트 (index 4, 9, 14, 19, 24, 29)
        for (int i = 4; i < total; i += 5)
            seq[i] = RunStageType.Elite;

        // 일반 스테이지 인덱스 수집
        var normalIndices = new List<int>();
        for (int i = 0; i < total; i++)
            if (seq[i] == RunStageType.Normal) normalIndices.Add(i);

        // 상점 4개 무작위 배치
        PlaceRandom(seq, normalIndices, RunStageType.Shop, 4, rng);

        // 이벤트 3개 무작위 배치
        PlaceRandom(seq, normalIndices, RunStageType.Event, 3, rng);

        return seq;
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
