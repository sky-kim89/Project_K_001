using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  StageData.cs
//  런타임에 new 로 생성하는 스테이지 데이터.
//  WaveData 목록은 StageGenerator 가 절차적으로 채운다.
// ============================================================

public class StageData
{
    public BattleMode     Mode;
    public int            StageNumber;
    public int            EnergyCost;
    public int            GoldReward;
    public int            StoneReward;
    public int            ExpReward;
    public int            ShardReward;
    public int            GeneralStoneReward;   // 장군 강화석 — 등급업 재화
    public int            EquipBoxReward;
    public int            DailyClearLimit;   // 0 = 무제한
    public Sprite         PreviewSprite;
    public List<WaveData> Waves = new();

    public bool IsRunElite { get; private set; }

    public string DisplayName => Mode switch
    {
        BattleMode.Normal => $"일반 스테이지 {StageNumber}",
        BattleMode.Elite  => $"엘리트 스테이지 {StageNumber}",
        _                 => $"스테이지 {StageNumber}",
    };

    /// <summary>런 엘리트 스테이지로 복제. 몬스터 스텟 ×1.1, 엘리트 출현 ×2.</summary>
    public static StageData AsElite(StageData src)
    {
        var clone = new StageData
        {
            Mode            = src.Mode,
            StageNumber     = src.StageNumber,
            EnergyCost      = src.EnergyCost,
            GoldReward      = (int)(src.GoldReward * 1.3f),
            StoneReward     = src.StoneReward,
            ExpReward       = src.ExpReward,
            ShardReward     = src.ShardReward,
            GeneralStoneReward = src.GeneralStoneReward,
            EquipBoxReward  = src.EquipBoxReward,
            DailyClearLimit = src.DailyClearLimit,
            PreviewSprite   = src.PreviewSprite,
            IsRunElite      = true,
        };
        // 웨이브 복사 (깊은 복사 불필요 — EnemySpawner가 읽기만 함)
        clone.Waves.AddRange(src.Waves);
        return clone;
    }
}
