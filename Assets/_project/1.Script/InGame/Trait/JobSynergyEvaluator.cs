using System;
using System.Collections.Generic;

// ============================================================
//  JobSynergyEvaluator.cs
//  배치된 장수의 직업 조합에 따라 직업 시너지 특성을 계산하고
//  RunTraitData 에 자동으로 추가·제거한다.
//
//  호출 시점: 장수 배치·해제·고용·해고 직후
//  UI 갱신 : OnSynergiesChanged 이벤트 구독
//
//  규칙:
//    - TraitType >= 1000 인 특성은 이 클래스만 추가/제거한다.
//    - 열화(3-스택) 시너지는 5-스택 시너지가 활성되면 자동 제거된다.
//    - 시너지는 저장되므로 로드 시 재계산 불필요 (배치 변경 시에만 재계산).
// ============================================================

public static class JobSynergyEvaluator
{
    public static event Action OnSynergiesChanged;

    struct SynergyDef
    {
        public TraitType               Type;
        public (UnitJob job, int min)[] Conditions;
        public TraitType               SupersededBy; // None이면 대체 없음
    }

    static readonly SynergyDef[] _defs =
    {
        // ── 조합형 ───────────────────────────────────────────
        new SynergyDef
        {
            Type       = TraitType.Synergy_VanguardCross,
            Conditions = new[] { (UnitJob.Knight, 1), (UnitJob.Archer, 1) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_MagicShield,
            Conditions = new[] { (UnitJob.Knight, 1), (UnitJob.Mage, 1) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_IronWallLine,
            Conditions = new[] { (UnitJob.Knight, 1), (UnitJob.ShieldBearer, 1) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_BalancedHost,
            Conditions = new[] { (UnitJob.Knight, 1), (UnitJob.Archer, 1), (UnitJob.Mage, 1), (UnitJob.ShieldBearer, 1) },
        },
        // ── 복합형 ───────────────────────────────────────────
        new SynergyDef
        {
            Type       = TraitType.Synergy_RangedFirenet,
            Conditions = new[] { (UnitJob.Archer, 2), (UnitJob.Mage, 2) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_IronVanguard,
            Conditions = new[] { (UnitJob.Knight, 2), (UnitJob.ShieldBearer, 2) },
        },
        // ── 열화 (3-스택) ─────────────────────────────────────
        new SynergyDef
        {
            Type         = TraitType.Synergy_KnightSquad,
            Conditions   = new[] { (UnitJob.Knight, 3) },
            SupersededBy = TraitType.Synergy_KnightOrder,
        },
        new SynergyDef
        {
            Type         = TraitType.Synergy_ArcherSquad,
            Conditions   = new[] { (UnitJob.Archer, 3) },
            SupersededBy = TraitType.Synergy_ArrowLegion,
        },
        new SynergyDef
        {
            Type         = TraitType.Synergy_MageSquad,
            Conditions   = new[] { (UnitJob.Mage, 3) },
            SupersededBy = TraitType.Synergy_GreatMageCorp,
        },
        new SynergyDef
        {
            Type         = TraitType.Synergy_ShieldSquad,
            Conditions   = new[] { (UnitJob.ShieldBearer, 3) },
            SupersededBy = TraitType.Synergy_Ironclad,
        },
        // ── 5-스택 ───────────────────────────────────────────
        new SynergyDef
        {
            Type       = TraitType.Synergy_KnightOrder,
            Conditions = new[] { (UnitJob.Knight, 5) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_ArrowLegion,
            Conditions = new[] { (UnitJob.Archer, 5) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_GreatMageCorp,
            Conditions = new[] { (UnitJob.Mage, 5) },
        },
        new SynergyDef
        {
            Type       = TraitType.Synergy_Ironclad,
            Conditions = new[] { (UnitJob.ShieldBearer, 5) },
        },
    };

    // ── 공개 API ─────────────────────────────────────────────

    public static void Recalculate()
    {
        var deploy    = UserDataManager.Instance?.Get<DeploymentData>();
        var traitData = UserDataManager.Instance?.Get<RunTraitData>();
        if (deploy == null || traitData == null) return;

        // 배치된 장수의 직업별 카운트 집계
        var jobCounts = new Dictionary<UnitJob, int>();
        foreach (var unitName in deploy.GetDeployedUnits())
        {
            var job = UnitJobRoller.GetJob(unitName);
            jobCounts.TryGetValue(job, out int cnt);
            jobCounts[job] = cnt + 1;
        }

        // 기존 시너지 전부 제거 후 재계산
        traitData.RemoveSynergies();

        foreach (var def in _defs)
        {
            if (!IsConditionMet(def.Conditions, jobCounts)) continue;

            // 열화 시너지: 상위 5-스택 조건도 충족되면 이 열화는 스킵
            if (def.SupersededBy != TraitType.None)
            {
                var superseder = FindDef(def.SupersededBy);
                if (superseder.HasValue && IsConditionMet(superseder.Value.Conditions, jobCounts))
                    continue;
            }

            traitData.AddTrait(def.Type);
        }

        UserDataManager.Instance?.RequestSave();
        OnSynergiesChanged?.Invoke();
    }

    public static List<TraitType> GetActiveSynergies()
    {
        var traitData = UserDataManager.Instance?.Get<RunTraitData>();
        var result    = new List<TraitType>();
        if (traitData == null) return result;
        foreach (var t in traitData.AcquiredTraits)
            if ((int)t >= 1000) result.Add(t);
        return result;
    }

    // ── 내부 ─────────────────────────────────────────────────

    static bool IsConditionMet((UnitJob job, int min)[] conditions, Dictionary<UnitJob, int> jobCounts)
    {
        foreach (var (job, min) in conditions)
        {
            jobCounts.TryGetValue(job, out int cnt);
            if (cnt < min) return false;
        }
        return true;
    }

    static SynergyDef? FindDef(TraitType type)
    {
        foreach (var def in _defs)
            if (def.Type == type) return def;
        return null;
    }
}
