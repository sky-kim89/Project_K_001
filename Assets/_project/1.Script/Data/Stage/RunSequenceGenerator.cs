using System;

// ============================================================
//  RunSequenceGenerator.cs
//  런(30스테이지)의 스테이지 타입 시퀀스를 생성.
//
//  고정 규칙:
//    - 1~2스테이지(index 0,1) → 항상 일반
//    - 5의 배수 스테이지    → 엘리트 = 허들 (index 4, 9, 14, 19, 24, 29)
//    - 허들 바로 앞 스테이지 → 상점    (index 3, 8, 13, 18, 23, 28)
//  간격 배치:
//    - 이벤트: 10개를 2~4 스테이지 간격으로 고르게 배치
//
//  결과 배치 (30칸 기준):
//    일반 8 · 상점 6 · 엘리트 6 · 이벤트 10
//
//  ⚠ 1~2스테이지를 일반으로 고정하는 이유
//    ① 런 시작(= 신규 시작·패배 후 환생) 직후 로비에 들어서자마자
//      상점·이벤트 팝업이 자동으로 뜨면 무슨 상황인지 알 수 없다.
//      첫 스테이지는 전투로 시작해 바로 진입할 수 있어야 한다.
//    ② 2스테이지는 로비 튜토리얼이 도는 칸이다.
//      첫 승리 직후 BattleResult → Lobby → HeroStat 튜토리얼이 이어지는데,
//      로비에 닿는 순간 이벤트·상점 팝업이 먼저 떠 버리면 튜토리얼이
//      그 팝업 뒤에 가려 출전 화면을 가리키게 된다.
//      (예전엔 2스테이지가 91% 확률로 이벤트였다 — FillRemaining 이
//       남은 이벤트를 앞쪽부터 채우는데 그 시작이 index 1 이었다.)
//
//  ⚠ 상점을 허들 직전에 고정하는 이유
//    허들(5의 배수)은 스텟 배율이 뛰는 관문이다. 그 앞에서 반드시 한 번
//    장비·용병·특성을 정비할 수 있어야 한다. 무작위로 뿌리면 상점이 허들
//    직후에 몰려 정작 필요한 순간에 빈손으로 들어가게 된다.
//
//  ⚠ 이벤트를 간격으로 배치하는 이유
//    무작위로 뿌리면 두 칸 연속으로 붙거나 여덟 스테이지 넘게 안 나오는
//    구간이 생긴다. 이벤트는 특성·용병을 얻는 거의 유일한 칸이라
//    "언제 또 나오나" 가 예측되어야 런 계획이 선다.
//
//  ⚠ 규칙을 바꾸면 IsValid() 도 같이 고칠 것
//    시퀀스는 세이브에 남는다. IsValid() 가 옛 세이브를 걸러 내
//    StageProgressData.EnsureRunSequence() 가 새로 뽑게 한다.
//    단 진행 중인 런은 다시 뽑지 않는다 — 진행 인덱스가 날아가기 때문이다.
// ============================================================

public static class RunSequenceGenerator
{
    /// <summary>허들(엘리트) 스테이지 주기. StageConfig.IsHurdle 과 반드시 같아야 한다.</summary>
    public const int HurdleInterval = 5;

    /// <summary>
    /// 앞에서 이만큼은 무조건 일반 전투다 (index 0 부터 셈).
    /// 상점·이벤트·엘리트 어느 것도 여기 못 들어온다.
    /// 튜토리얼이 도는 구간이라 자동 팝업이 끼면 안 된다 — 파일 상단 주석 참고.
    /// </summary>
    public const int ReservedNormalStages = 2;

    const int EventCount  = 10;
    const int EventGapMin = 2;   // 이벤트 사이 최소 스테이지 간격
    const int EventGapMax = 4;   // 이벤트 사이 최대 스테이지 간격

    public static RunStageType[] Generate(int total = 30)
    {
        var seq = new RunStageType[total];
        var rng = new Random();

        // 기본: 일반
        for (int i = 0; i < total; i++) seq[i] = RunStageType.Normal;

        // 허들 = 엘리트, 그 앞 칸 = 상점
        for (int i = HurdleInterval - 1; i < total; i += HurdleInterval)
        {
            seq[i] = RunStageType.Elite;

            int shop = i - 1;
            if (shop >= ReservedNormalStages) seq[shop] = RunStageType.Shop;
        }

        PlaceEvents(seq, rng, EventCount);
        return seq;
    }

    /// <summary>
    /// 저장된 시퀀스가 현재 규칙을 지키는지 검사.
    /// 규칙을 바꾼 뒤에도 옛 세이브가 그대로 쓰이는 것을 막는다.
    /// </summary>
    public static bool IsValid(RunStageType[] seq)
    {
        if (seq == null || seq.Length == 0) return false;

        // 앞의 고정 일반 구간 — 여기에 이벤트·상점이 들어간 옛 세이브를 걸러 낸다.
        for (int i = 0; i < ReservedNormalStages && i < seq.Length; i++)
            if (seq[i] != RunStageType.Normal) return false;

        for (int i = HurdleInterval - 1; i < seq.Length; i += HurdleInterval)
        {
            if (seq[i] != RunStageType.Elite) return false;

            int shop = i - 1;
            if (shop >= ReservedNormalStages && seq[shop] != RunStageType.Shop) return false;
        }
        return true;
    }

    // ── 내부 ─────────────────────────────────────────────────

    // 이벤트를 EventGapMin~Max 간격으로 놓는다.
    // 목표 지점이 상점·엘리트면 뒤쪽 일반 칸으로 밀리므로 실제 간격은 조금 더 벌어진다.
    // 남은 칸이 빠듯해지면 간격을 좁혀 개수를 맞춘다 — 안 그러면 후반 이벤트가 잘려 나간다.
    static void PlaceEvents(RunStageType[] seq, Random rng, int count)
    {
        int cursor = 0;   // 고정 일반 구간에서 간격만큼 건너뛰며 시작한다

        for (int placed = 0; placed < count; placed++)
        {
            int maxGap = (seq.Length - cursor) / (count - placed);
            if (maxGap < EventGapMin) maxGap = EventGapMin;
            if (maxGap > EventGapMax) maxGap = EventGapMax;

            cursor += rng.Next(EventGapMin, maxGap + 1);

            // ⚠ 고정 일반 구간은 넘어서 시작한다
            //   지금은 간격 최솟값이 구간 길이와 같아 저절로 지켜지지만,
            //   둘 중 하나만 바꿔도 조용히 깨진다. 규칙을 값에 기대지 않는다.
            if (cursor < ReservedNormalStages) cursor = ReservedNormalStages;

            // 상점·엘리트 칸은 건너뛴다
            while (cursor < seq.Length && seq[cursor] != RunStageType.Normal) cursor++;

            // 끝까지 밀렸다 — 남은 이벤트는 앞쪽 빈 칸에 채운다.
            // 간격보다 개수가 우선이다 (이벤트가 특성·용병의 주 공급원이라).
            if (cursor >= seq.Length)
            {
                FillRemaining(seq, count - placed);
                return;
            }

            seq[cursor] = RunStageType.Event;
        }
    }

    // ⚠ 고정 일반 구간 뒤부터 채운다
    //   여기서 index 1 부터 채우는 바람에 2스테이지가 91% 확률로 이벤트가 됐다.
    //   앞쪽부터 메우는 성격상 이 시작점이 곧 "가장 자주 이벤트가 되는 칸" 이다.
    static void FillRemaining(RunStageType[] seq, int remaining)
    {
        for (int i = ReservedNormalStages; i < seq.Length && remaining > 0; i++)
        {
            if (seq[i] != RunStageType.Normal) continue;
            seq[i] = RunStageType.Event;
            remaining--;
        }
    }
}
