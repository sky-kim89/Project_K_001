using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  CodexCatalog.cs
//  도감에 실릴 "전체 목록" 을 4개 DB 에서 긁어오는 단일 진입점.
//
//  ⚠ 총 종수를 상수로 박지 말 것
//    장수 이름·장비·특성은 계속 늘어난다. 어딘가에 439 같은 숫자를 적어 두면
//    항목이 늘어난 날부터 진행률이 조용히 틀려진다. 전부 DB 에서 센다.
//
//  ⚠ 도감의 "미보유" 는 존재 자체를 숨기는 게 아니다
//    칸은 그대로 두고 이름·아이콘만 ? 로 가린다 (CodexEntry.Owned=false).
//    몇 칸이 비었는지 보여야 모으고 싶어진다.
//
//  ⚠ 설명·스탯 문구는 각 분류의 정본 헬퍼를 그대로 쓴다
//    특성은 AbilityUIHelper.BuildStatText, 장비는 EquipmentData.Description …
//    도감에서 따로 문장을 만들면 같은 항목이 화면마다 다르게 설명된다.
// ============================================================

public enum CodexCategory
{
    Equipment = 0,
    Ability   = 1,
    Trait     = 2,
    General   = 3,
}

/// <summary>도감 한 칸.</summary>
public struct CodexEntry
{
    public string Name;        // 미보유면 화면에 안 쓴다 (? 로 대체)
    public Sprite Icon;        // 없을 수 있다 (장수)
    public bool   Owned;
    public Color  Accent;      // 테두리 색 — 등급이 있으면 등급색

    /// <summary>이름 아래 한 줄. 장수는 "직업 · 등급", 나머지는 등급.</summary>
    public string SubLabel;

    // 눌렀을 때 띄울 내용 (미보유면 안 쓴다)
    public string Desc;
    public string StatLine;

    /// <summary>장수 탭에서만 채워진다 — 상세 팝업을 열 이름.</summary>
    public string GeneralName;
}

public static class CodexCatalog
{
    /// <summary>등급이 없는 항목의 테두리 색.</summary>
    static readonly Color NeutralAccent = new Color(0.32f, 0.36f, 0.52f);

    public static string Label(CodexCategory c) => c switch
    {
        CodexCategory.Equipment => "장비",
        CodexCategory.Ability   => "어빌리티",
        CodexCategory.Trait     => "특성",
        CodexCategory.General   => "장수",
        _                       => "?",
    };

    /// <summary>한 분류의 전체 칸 목록. 보유 여부까지 채워서 돌려준다.</summary>
    public static List<CodexEntry> Build(CodexCategory category)
    {
        var codex  = UserDataManager.Instance?.Get<CodexData>();
        var result = new List<CodexEntry>();

        switch (category)
        {
            case CodexCategory.Equipment:
            {
                var db = EquipmentDatabase.Current;
                if (db == null) break;
                foreach (var e in db.Equipments)
                {
                    if (e == null) continue;
                    result.Add(new CodexEntry
                    {
                        Name     = e.EquipmentName,
                        Icon     = e.Icon,
                        Owned    = codex != null && codex.HasEquip(e.EquipmentId),
                        Accent   = GradeStyle.GetColor(e.Grade),
                        SubLabel = GradeStyle.GetLabel(e.Grade),
                        Desc     = e.Description,
                        StatLine = EquipStatLine(e),
                    });
                }
                break;
            }

            case CodexCategory.Ability:
            {
                var db = AbilityDatabase.Current;
                if (db == null) break;
                foreach (var a in db.GetAll())
                {
                    if (a == null) continue;
                    result.Add(new CodexEntry
                    {
                        Name     = a.AbilityName,
                        Icon     = a.Icon,
                        Owned    = codex != null && codex.HasAbility(a.Id),
                        Accent   = AbilityAccent(a.Grade),
                        SubLabel = AbilityGradeLabel(a.Grade),
                        Desc     = AbilityDesc(a),
                        StatLine = AbilityStatLine(a),
                    });
                }
                break;
            }

            case CodexCategory.Trait:
            {
                var db = TraitDatabase.Current;
                if (db == null) break;
                foreach (var t in db.GetAll())
                {
                    if (t == null) continue;
                    result.Add(new CodexEntry
                    {
                        Name     = t.TraitName,
                        Icon     = t.Icon,
                        Owned    = codex != null && codex.HasTrait(t.TraitType),
                        Accent   = NeutralAccent,
                        Desc     = t.Description,
                        // 스택 누적치는 런 상태라 도감에선 뺀다 — 도감은 항목 자체의 설명이다
                        StatLine = AbilityUIHelper.BuildStatText(t, showAccumulated: false),
                    });
                }
                break;
            }

            case CodexCategory.General:
            {
                // 장수는 SO 가 아니라 이름 풀이 원본이다 (UnitData.AllNames).
                // 초상화는 런타임 합성이라 여기서 만들지 않는다 —
                // 화면에 들어온 칸만 GeneralPortraitProvider 에 따로 요청한다.
                foreach (var name in UnitData.AllNames)
                {
                    UnitGrade birth = UnitJobRoller.GetBirthGrade(name);
                    UnitJob   job   = UnitJobRoller.GetJob(name);

                    // 등급 뒤의 숫자 = 품질 점수(0~10). 같은 등급이라도 굴림이
                    // 좋았는지 나빴는지가 갈리므로 등급만으로는 세기를 알 수 없다.
                    // HeroDetailPopup 의 등급 칩과 같은 함수를 쓴다 — 두 화면의 숫자가 어긋나면 안 된다.
                    string label = $"{JobStyle.GetLabel(job)} · {GradeStyle.GetLabelWithQuality(birth, name)}";

                    result.Add(new CodexEntry
                    {
                        Name        = name,
                        Icon        = null,
                        Owned       = codex != null && codex.HasGeneral(name),
                        Accent      = GradeStyle.GetColor(birth),
                        SubLabel    = label,
                        Desc        = label,
                        StatLine    = null,
                        GeneralName = name,
                    });
                }

                // 좋은 것부터 — 400칸을 이름 순으로 늘어놓으면 무엇이 귀한지 안 보인다.
                // 등급이 같으면 품질로 다시 가른다 (같은 영웅이라도 9와 2는 완전히 다른 장수다).
                //
                // ⚠ 정렬은 목록을 만든 뒤 한 번만 한다
                //   Progress() 도 이 함수를 쓰므로 여기서 정렬해 두면
                //   화면·치트·진행률이 전부 같은 순서를 본다.
                result.Sort((a, b) =>
                {
                    var ga = UnitJobRoller.GetBirthGrade(a.GeneralName);
                    var gb = UnitJobRoller.GetBirthGrade(b.GeneralName);
                    if (ga != gb) return gb.CompareTo(ga);                    // 등급 내림차순

                    int qa = GradeStyle.QualityScore(a.GeneralName);
                    int qb = GradeStyle.QualityScore(b.GeneralName);
                    if (qa != qb) return qb.CompareTo(qa);                    // 품질 내림차순

                    return string.CompareOrdinal(a.Name, b.Name);             // 그래도 같으면 이름순
                });
                break;
            }
        }

        return result;
    }

    /// <summary>전 분류 합계 (수집, 전체).</summary>
    public static (int owned, int total) TotalProgress()
    {
        int owned = 0, total = 0;
        foreach (CodexCategory c in System.Enum.GetValues(typeof(CodexCategory)))
        {
            var (o, t) = Progress(c);
            owned += o;
            total += t;
        }
        return (owned, total);
    }

    public static (int owned, int total) Progress(CodexCategory category)
    {
        var list  = Build(category);
        int owned = 0;
        foreach (var e in list) if (e.Owned) owned++;
        return (owned, list.Count);
    }

    // ── 문구 조립 ────────────────────────────────────────────

    static string EquipStatLine(EquipmentData e)
    {
        if (e.StatEntries == null || e.StatEntries.Count == 0) return null;

        var sb = new System.Text.StringBuilder();
        foreach (var s in e.StatEntries)
        {
            if (sb.Length > 0) sb.Append('\n');
            // 강화 0단계 = 기본값. 도감은 "이런 장비가 있다" 를 보여 주는 곳이다.
            sb.Append($"{LocalizationManager.Instance.Get(s.Stat.ToString())} " +
                      AbilityUIHelper.FormatStatValue(s.Stat, e.GetStatValue(s, 0)));
        }
        return sb.ToString();
    }

    static string AbilityDesc(AbilityData a)
    {
        // Special 은 효과를 코드로 들고 있어 Description 을 직접 만든다
        string d = a.Description;
        return string.IsNullOrEmpty(d) ? null : d;
    }

    static string AbilityStatLine(AbilityData a)
    {
        if (a.Grade == AbilityGrade.Special) return null;   // Description 이 이미 다 말한다

        var sb = new System.Text.StringBuilder();
        sb.Append($"{LocalizationManager.Instance.Get(a.Stat1.ToString())} " +
                  AbilityUIHelper.FormatStatValue(a.Stat1, a.Value1));
        if (a.HasStat2)
            sb.Append($"\n{LocalizationManager.Instance.Get(a.Stat2.ToString())} " +
                      AbilityUIHelper.FormatStatValue(a.Stat2, a.Value2));
        return sb.ToString();
    }

    static string AbilityGradeLabel(AbilityGrade grade) => grade switch
    {
        AbilityGrade.Normal   => "일반",
        AbilityGrade.Advanced => "고급",
        AbilityGrade.Special  => "특수",
        AbilityGrade.Mastery  => "달인",
        _                     => "",
    };

    static Color AbilityAccent(AbilityGrade grade) => grade switch
    {
        AbilityGrade.Normal   => new Color(0.55f, 0.58f, 0.68f),
        AbilityGrade.Advanced => new Color(0.25f, 0.62f, 1.00f),
        AbilityGrade.Special  => new Color(1.00f, 0.60f, 0.10f),
        _                     => NeutralAccent,
    };
}
