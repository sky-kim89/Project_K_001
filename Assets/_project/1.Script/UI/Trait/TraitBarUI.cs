using UnityEngine;

// ============================================================
//  TraitBarUI.cs
//  상단 바(TopBar) 왼쪽의 특성 표시 스트립.
//
//  1행 = 일반 특성 (TraitType < 1000)
//  2행 = 직업 시너지 특성 (TraitType >= 1000)
//
//  BattlePanel 이 아니라 TopBar 에 붙는다 — 어느 패널을 보고 있든
//  현재 런의 특성이 계속 보인다. 런이 없으면 슬롯이 전부 꺼져 안 보인다.
//
//  갱신 시점:
//    RunTraitData.OnTraitsChanged        — 특성 획득/제거/초기화
//    JobSynergyEvaluator.OnSynergiesChanged — 배치 변경으로 시너지 재계산
//
//  Inspector 연결: TopBarCreator 가 자동으로 채운다.
// ============================================================

public class TraitBarUI : MonoBehaviour
{
    [Header("일반 특성 (1행)")]
    [SerializeField] TraitIconUI[] _traitIcons;

    [Header("시너지 특성 (2행)")]
    [SerializeField] TraitIconUI[] _synergyIcons;

    void OnEnable()
    {
        RunTraitData.OnTraitsChanged           += Refresh;
        JobSynergyEvaluator.OnSynergiesChanged += Refresh;
        DifficultyData.OnChanged               += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        RunTraitData.OnTraitsChanged           -= Refresh;
        JobSynergyEvaluator.OnSynergiesChanged -= Refresh;
        DifficultyData.OnChanged               -= Refresh;
    }

    // ── 난이도 디버프 ─────────────────────────────────────────

    /// <summary>
    /// 현재 난이도의 디버프를 1행 앞쪽에 채운다. 채운 개수를 돌려준다.
    /// 출정(디버프 없음)이면 0 을 돌려주고 아무것도 안 그린다.
    /// </summary>
    int FillDifficultyDebuffs()
    {
        var tier = DifficultyConfig.CurrentTier();
        if (tier == null) return 0;

        var sprites = SpriteManager.Instance;
        int idx = 0;

        foreach (var d in tier.ActiveDebuffs())
        {
            if (idx >= _traitIcons.Length) break;

            _traitIcons[idx].SetupCustom(
                sprites != null ? sprites.Get(d.IconKey()) : null,
                d.Label(),
                DifficultyConfig.Flavor(d),
                tier.DescribeDebuff(d));
            _traitIcons[idx].gameObject.SetActive(true);
            idx++;
        }
        return idx;
    }

    // ── 갱신 ──────────────────────────────────────────────────

    public void Refresh()
    {
        RefreshNormal();
        RefreshSynergy();
    }

    void RefreshNormal()
    {
        var traitData = UserDataManager.Instance?.Get<RunTraitData>();
        var db        = TraitDatabase.Current;

        // 난이도 디버프를 특성 앞에 먼저 깐다.
        // 별도 UI 를 만들지 않고 같은 줄에 끼워 넣는다 — 플레이어 입장에서
        // "지금 이 판에 걸려 있는 것" 이라는 성격이 특성과 똑같기 때문이다.
        int idx = FillDifficultyDebuffs();
        if (traitData != null && db != null)
        {
            foreach (var t in traitData.AcquiredTraits)
            {
                if ((int)t >= SynergyIdBase) continue;   // 시너지는 2행으로 분리

                // DB 에 없는 특성은 회색 빈칸으로 보이므로 아예 건너뛴다.
                // (빈칸이 보인다면 TraitCreator 에서 SO 를 만들지 않은 것이다)
                var data = db.Get(t);
                if (data == null)
                {
                    Debug.LogWarning($"[TraitBarUI] TraitDatabase 에 {t} 가 없습니다 — " +
                                     "Tools > Project K > 데이터 생성 > 특성 을 실행하세요.");
                    continue;
                }

                if (idx >= _traitIcons.Length) break;
                _traitIcons[idx].Setup(data);
                _traitIcons[idx].gameObject.SetActive(true);
                idx++;
            }
        }
        for (; idx < _traitIcons.Length; idx++)
            _traitIcons[idx].gameObject.SetActive(false);
    }

    void RefreshSynergy()
    {
        var db  = TraitDatabase.Current;
        int idx = 0;

        if (db != null)
        {
            foreach (var t in JobSynergyEvaluator.GetActiveSynergies())
            {
                var data = db.Get(t);
                if (data == null) continue;
                if (idx >= _synergyIcons.Length) break;
                // 시너지도 스탯 줄을 켠다 — 설명문에서 수치를 뺐으므로
                // 이 줄을 끄면 효과를 확인할 방법이 없다.
                _synergyIcons[idx].Setup(data);
                _synergyIcons[idx].gameObject.SetActive(true);
                idx++;
            }
        }
        for (; idx < _synergyIcons.Length; idx++)
            _synergyIcons[idx].gameObject.SetActive(false);
    }

    const int SynergyIdBase = 1000;
}
