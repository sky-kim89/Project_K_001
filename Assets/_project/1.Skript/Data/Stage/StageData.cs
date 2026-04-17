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
    public int            DailyClearLimit;   // 0 = 무제한
    public Sprite         PreviewSprite;
    public List<WaveData> Waves = new();

    public string DisplayName => Mode switch
    {
        BattleMode.Normal => $"일반 스테이지 {StageNumber}",
        BattleMode.Elite  => $"엘리트 스테이지 {StageNumber}",
        _                 => $"스테이지 {StageNumber}",
    };
}
