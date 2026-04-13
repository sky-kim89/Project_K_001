using UnityEditor;
using UnityEngine;

// ============================================================
//  EffectKeyLinker.cs
//  Unity 메뉴 BattleGame > Link Effect Keys to Skills 실행 시
//  모든 ActiveSkillData SO 에 이펙트 풀 키와 자동반납 딜레이를 일괄 적용한다.
//
//  ■ 키 배정 근거 (EffectPrefabGenerator 주석 기반)
//    BaseEffectKey  : 스킬 시작 위치 이펙트 (존 유지형 포함)
//    CasterEffectKey: 시전자 위치 이펙트 (타격/버프 연출)
//    TargetEffectKey: 피격 대상 위치 이펙트
//
//  ■ DespawnDelay 선정 기준
//    - 즉발 이펙트 : 파티클 재생 시간 + 0.5f 여유
//    - 루프 이펙트 : EffectDuration(zone/buff 지속시간) + 1~2f 여유
//    - 메테오 Base : Runner 내부에서 delay + DespawnDelay 로 계산되므로
//                   여유분(1.5f)만 설정하면 됨
//
//  ■ 사용법
//    BattleGame > Link Effect Keys to Skills
// ============================================================

public static class EffectKeyLinker
{
    const string kDataPath = "Assets/_project/Data/Actives";

    [MenuItem("BattleGame/Link Effect Keys to Skills")]
    public static void LinkAll()
    {
        // Link(assetName, baseKey, casterKey, targetKey, despawnDelay)
        // ──────────────────────────────────────────────────────────────────────
        // ① HeavyStrike — 강타 (돌진 타격)
        //    Base  = 출발 먼지, Caster = 타격 섬광, Target = 피격 섬광
        Link("Active_HeavyStrike",
            baseKey:   "FX_Dust_Dash",
            casterKey: "FX_Slash_Impact",
            targetKey: "FX_Slash_Impact",
            delay:     1.0f);

        // ② VolleyFire — 일제 사격
        //    Caster = 발사 연출 (전체 일제사격이므로 피격 이펙트는 자체 처리)
        Link("Active_VolleyFire",
            baseKey:   "",
            casterKey: "FX_Arrow_Volley",
            targetKey: "",
            delay:     0.8f);

        // ③ LeapStrike — 도약 강타
        //    Base = 출발 먼지, Caster = 착지 충격파, Target = 피격 섬광
        Link("Active_LeapStrike",
            baseKey:   "FX_Dust_Dash",
            casterKey: "FX_Leap_Land",
            targetKey: "FX_Slash_Impact",
            delay:     1.5f);

        // ④ HealAura — 치유 오라 (루프, EffectDuration 동안 지속)
        //    Caster = 오라 (loop) → 딜레이를 EffectDuration + 여유로 설정
        Link("Active_HealAura",
            baseKey:   "",
            casterKey: "FX_Heal_Aura",
            targetKey: "",
            delay:     4.5f);

        // ⑤ TargetHeal — 집중 치유 (단일 대상)
        //    Target = 치유 십자 심볼
        Link("Active_TargetHeal",
            baseKey:   "",
            casterKey: "",
            targetKey: "FX_Heal_Target",
            delay:     1.5f);

        // ⑥ ChargeSoldier — 돌격 병사 소환
        //    Base = 병사 출발 먼지, Target = 충돌 임팩트
        Link("Active_ChargeSoldier",
            baseKey:   "FX_Dust_Dash",
            casterKey: "",
            targetKey: "FX_Charge_Impact",
            delay:     1.0f);

        // ⑦ SummonSkeleton — 스켈레톤 소환
        //    Base = 소환진 마법진
        Link("Active_SummonSkeleton",
            baseKey:   "FX_Summon_Circle",
            casterKey: "",
            targetKey: "",
            delay:     2.5f);

        // ⑧ PoisonZone — 독성 지대 (루프 존, EffectDuration ≈ 8f)
        //    Base = 독 안개 (Zone Runner 에서 BaseEffectKey 를 zone 지속 시간만큼 유지)
        Link("Active_PoisonZone",
            baseKey:   "FX_Poison_Zone",
            casterKey: "",
            targetKey: "",
            delay:     10.0f);

        // ⑨ Meteor — 메테오
        //    Base  = 낙하 예고 마커 (Runner: delay + DespawnDelay 로 자동 연장)
        //    Target = 착탄 폭발
        Link("Active_Meteor",
            baseKey:   "FX_Meteor_Warning",
            casterKey: "",
            targetKey: "FX_Meteor_Explosion",
            delay:     2.0f);

        // ⑩ Blizzard — 블리자드 (루프 존, EffectDuration ≈ 8f)
        //    Base = 눈보라 영역
        Link("Active_Blizzard",
            baseKey:   "FX_Blizzard",
            casterKey: "",
            targetKey: "",
            delay:     10.0f);

        // ⑪ SacrificeSoldier — 병사 희생
        //    Caster = 흡수 연출 (시전자), Target = 사망 연출 (병사)
        Link("Active_SacrificeSoldier",
            baseKey:   "",
            casterKey: "FX_Absorb",
            targetKey: "FX_Sacrifice",
            delay:     1.5f);

        // ⑫ Bind — 속박 (지속 상태이상, EffectDuration ≈ 3f)
        //    Target = 속박 링 (루프)
        Link("Active_Bind",
            baseKey:   "",
            casterKey: "",
            targetKey: "FX_Bind",
            delay:     4.0f);

        // ⑬ SuicideSoldier — 자폭 병사
        //    Base = 병사 출발 먼지, Target = 폭발
        Link("Active_SuicideSoldier",
            baseKey:   "FX_Dust_Dash",
            casterKey: "",
            targetKey: "FX_Explosion",
            delay:     2.0f);

        // ⑭ Berserker — 광전사 (루프 버프, EffectDuration ≈ 5f)
        //    Caster = 분노 오라 (loop)
        Link("Active_Berserker",
            baseKey:   "",
            casterKey: "FX_Berserk",
            targetKey: "",
            delay:     6.0f);

        // ⑮ IronShield — 철벽 방어 (루프 버프, EffectDuration ≈ 5f)
        //    Caster = 방어막 링 (loop)
        Link("Active_IronShield",
            baseKey:   "",
            casterKey: "FX_Shield_Up",
            targetKey: "",
            delay:     6.0f);

        // ⑯ ArrowRain — 화살 비 (루프 존, EffectDuration ≈ 5f)
        //    Base = 화살비 낙하 영역 (loop)
        Link("Active_ArrowRain",
            baseKey:   "FX_Arrow_Rain_Zone",
            casterKey: "",
            targetKey: "",
            delay:     7.0f);

        // ⑰ BattleCry — 전투 함성
        //    Caster = 별빛 폭발 (즉발 버프 연출)
        Link("Active_BattleCry",
            baseKey:   "",
            casterKey: "FX_Battle_Cry",
            targetKey: "",
            delay:     1.5f);

        // ⑱ Shockwave — 충격파 (전방 부채꼴)
        //    Caster = 충격파 발사 연출
        Link("Active_Shockwave",
            baseKey:   "",
            casterKey: "FX_Shockwave",
            targetKey: "",
            delay:     1.0f);

        // ⑲ SwiftStrike — 신속 연격 (루프 버프, EffectDuration ≈ 5f)
        //    Caster = 속도 잔상 오라 (loop)
        Link("Active_SwiftStrike",
            baseKey:   "",
            casterKey: "FX_Speed_Up",
            targetKey: "",
            delay:     6.0f);

        // ⑳ SummonElite — 정예 소환
        //    Base = 소환진 마법진 (SummonSkeleton 과 동일)
        Link("Active_SummonElite",
            baseKey:   "FX_Summon_Circle",
            casterKey: "",
            targetKey: "",
            delay:     2.5f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[EffectKeyLinker] ✓ 20개 액티브 스킬 이펙트 키 연동 완료.");
    }

    // ── 내부 헬퍼 ───────────────────────────────────────────────────────

    static void Link(
        string assetName,
        string baseKey,
        string casterKey,
        string targetKey,
        float  delay)
    {
        string path = $"{kDataPath}/{assetName}.asset";
        var    so   = AssetDatabase.LoadAssetAtPath<ActiveSkillData>(path);

        if (so == null)
        {
            Debug.LogWarning($"[EffectKeyLinker] SO 없음: {path}");
            return;
        }

        so.BaseEffectKey      = baseKey;
        so.CasterEffectKey    = casterKey;
        so.TargetEffectKey    = targetKey;
        so.EffectDespawnDelay = delay;

        EditorUtility.SetDirty(so);

        Debug.Log($"[EffectKeyLinker] {assetName}\n" +
                  $"  Base={Quote(baseKey)}  Caster={Quote(casterKey)}  Target={Quote(targetKey)}  Delay={delay}s");
    }

    static string Quote(string s) => string.IsNullOrEmpty(s) ? "(없음)" : s;
}
