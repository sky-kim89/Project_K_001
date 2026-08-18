using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  RunStarter.cs
//  "장수 하나를 골라 런을 시작한다" 를 담당하는 단일 진입점.
//
//  ■ 왜 따로 뺐나
//    이 절차를 밟는 곳이 둘이다 —
//      · MainPanelUI     : 플레이어가 카드를 골라 시작
//      · LobbyManager    : 설치 후 첫 실행에서 기사를 자동 선택
//    한쪽에만 특성 배정이나 시너지 재계산을 빠뜨리면 그 경로로 시작한 런만
//    조용히 특성이 비어 있게 된다. 절차는 여기 하나뿐이어야 한다.
//
//  ■ 순서가 중요하다
//    UnitData 등록 → 배치 → RunInProgress → 시너지 → 직업 특성 → 저장.
//    시너지(JobSynergyEvaluator)는 배치 목록을 읽으므로 배치 뒤라야 한다.
// ============================================================

public static class RunStarter
{
    /// <summary>
    /// 해당 직업의 시작 후보 장수 하나를 굴린다.
    /// GameplayConfig.MainPanelCandidates 에 프리셋 이름이 있으면 그걸 쓰고,
    /// 비어 있으면 그 직업 이름 풀에서 무작위로 뽑는다.
    ///
    /// ⚠ 시작 장수는 등급을 Epic 으로 올려 준다 (GradeUpCount 보정)
    ///   태생 등급이 낮은 이름이 걸렸다고 첫 런이 불리해지면 안 된다.
    /// </summary>
    public static UnitEntry RollCandidate(UnitJob job)
    {
        var    presets    = GameplayConfig.Current.MainPanelCandidates;
        int    slot       = (int)job;
        string presetName = (presets != null && slot < presets.Length) ? presets[slot].Name : "";

        string chosen = !string.IsNullOrEmpty(presetName)
            ? presetName
            : RollNameForJob(job);

        UnitGrade birth = UnitJobRoller.GetBirthGrade(chosen);
        return new UnitEntry
        {
            UnitName     = chosen,
            Level        = 1,
            Exp          = 0,
            GradeUpCount = Mathf.Max(0, (int)UnitGrade.Epic - (int)birth),
        };
    }

    /// <summary>
    /// 고른 장수로 런을 시작한다 — 등록·배치·특성·시너지까지 마치고 저장한다.
    /// 이 호출 뒤에 LobbyManager.StartBattle() 을 부르면 바로 전투로 들어간다.
    /// </summary>
    public static void BeginRun(UnitEntry selected)
    {
        var udm        = UserDataManager.Instance;
        var unitData   = udm.Get<UnitData>();
        var deployData = udm.Get<DeploymentData>();
        var progress   = udm.Get<StageProgressData>();

        if (!unitData.HasUnit(selected.UnitName))
            unitData.AddUnit(new UnitEntry
            {
                UnitName     = selected.UnitName,
                Level        = selected.Level,
                Exp          = selected.Exp,
                GradeUpCount = selected.GradeUpCount,
            });

        // 첫 빈 슬롯에 배치 (전부 차 있으면 0번)
        int emptySlot = 0;
        for (int i = 0; i < 5; i++)
        {
            if (string.IsNullOrEmpty(deployData.GetUnitAt(i))) { emptySlot = i; break; }
        }
        deployData.Deploy(selected.UnitName, emptySlot);

        progress.RunInProgress = true;

        // 배치가 끝난 뒤라야 직업 시너지가 이 장수를 센다
        JobSynergyEvaluator.Recalculate();

        TraitType jobTrait = UnitJobRoller.GetJob(selected.UnitName) switch
        {
            UnitJob.Knight       => TraitType.KnightCommand,
            UnitJob.Archer       => TraitType.ArcherPrecision,
            UnitJob.Mage         => TraitType.MageArcane,
            UnitJob.ShieldBearer => TraitType.ShieldFortress,
            _                    => TraitType.None,
        };
        if (jobTrait != TraitType.None)
            udm.Get<RunTraitData>().AddTrait(jobTrait);

        udm.SaveAll();
    }

    // ── 내부 ─────────────────────────────────────────────────

    static string RollNameForJob(UnitJob job)
    {
        List<string> allNames = UserDataManager.Instance.Get<UnitData>().GetAvailableNames();

        var bucket = new List<string>();
        foreach (string nm in allNames)
            if (UnitJobRoller.GetJob(nm) == job) bucket.Add(nm);

        if (bucket.Count > 0) return bucket[Random.Range(0, bucket.Count)];
        if (allNames.Count > 0) return allNames[Random.Range(0, allNames.Count)];
        return $"용사{(int)job + 1}";
    }
}
