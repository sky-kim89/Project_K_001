using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  UnitData.cs
//  보유 유닛 목록 저장 섹션.
//
//  보유 데이터:
//  - 보유 유닛 목록 (유닛ID, 레벨, 경험치, 장착 스킬)
//
//  새 유닛 필드 추가 시: UnitEntry 내부에 필드만 추가하면 됨.
// ============================================================

public class UnitData : ISaveSection
{
    public SaveKey SaveKey => SaveKey.UnitData;

    // ── 런타임 접근용 프로퍼티 ───────────────────────────────

    public IReadOnlyList<UnitEntry> Units => _raw.Units;

    // ── 내부 직렬화 데이터 ───────────────────────────────────

    UnitRawData _raw = new();

    // ── 데이터 갱신 메서드 ───────────────────────────────────

    public void AddUnit(UnitEntry entry)
    {
        _raw.Units.Add(entry);
    }

    public void RemoveUnit(string unitId)
    {
        _raw.Units.RemoveAll(u => u.UnitName == unitId);
    }

    public UnitEntry GetUnit(string unitId)
    {
        return _raw.Units.Find(u => u.UnitName == unitId);
    }

    public bool HasUnit(string unitId)
    {
        return _raw.Units.Exists(u => u.UnitName == unitId);
    }

    public void SetUnitLevel(string unitId, int level)
    {
        UnitEntry entry = GetUnit(unitId);
        if (entry != null) entry.Level = level;
    }

    public int AddUnitExp(string unitId, int amount)
    {
        UnitEntry entry = GetUnit(unitId);
        if (entry == null) return 0;

        entry.Exp += amount;

        int levelsGained = 0;
        int expPerLevel  = GameplayConfig.Current.ExpPerLevel;
        while (entry.Exp >= entry.Level * expPerLevel)
        {
            entry.Exp -= entry.Level * expPerLevel;
            entry.Level++;
            levelsGained++;
        }
        return levelsGained;
    }

    // ── 런 장비 슬롯 관리 (회귀 시 ClearAllEquipments 호출) ──

    public void SetEquipment(string unitId, int slot, string equipId, int enhanceLevel)
    {
        if (slot < 0 || slot >= 2) return;
        var entry = GetUnit(unitId);
        if (entry == null) return;
        entry.EnsureEquipArrays();
        entry.RunEquipSlots[slot]   = equipId ?? "";
        entry.RunEquipEnhance[slot] = enhanceLevel;
    }

    public void AddSoldierBonus(string unitId, int amount)
    {
        var entry = GetUnit(unitId);
        if (entry != null) entry.SoldierBonus += amount;
    }

    public string PickAvailableName()
    {
        var available = new List<string>(s_namePool.Length);
        foreach (var n in s_namePool)
            if (!HasUnit(n)) available.Add(n);
        if (available.Count == 0) return null;
        return available[UnityEngine.Random.Range(0, available.Count)];
    }

    public List<string> GetAvailableNames()
    {
        var available = new List<string>(s_namePool.Length);
        foreach (var n in s_namePool)
            if (!HasUnit(n)) available.Add(n);
        return available;
    }

    public void RemoveEquipment(string unitId, int slot)
    {
        if (slot < 0 || slot >= 2) return;
        var entry = GetUnit(unitId);
        if (entry == null) return;
        entry.EnsureEquipArrays();
        entry.RunEquipSlots[slot]   = "";
        entry.RunEquipEnhance[slot] = 0;
    }

    /// <summary>회귀(런 종료) 시 모든 장군의 런 장비를 초기화.</summary>
    public void ClearAllEquipments()
    {
        foreach (var entry in _raw.Units)
        {
            entry.RunEquipSlots   = new string[2];
            entry.RunEquipEnhance = new int[2];
        }
    }

    // ── ISaveSection ─────────────────────────────────────────

    public string Serialize()
    {
        return JsonUtility.ToJson(_raw);
    }

    public void Deserialize(string json)
    {
        _raw = JsonUtility.FromJson<UnitRawData>(json) ?? new UnitRawData();
    }

    public void SetDefaults()
    {
        _raw = new UnitRawData();
        var name = s_namePool[UnityEngine.Random.Range(0, s_namePool.Length)];
        _raw.Units.Add(new UnitEntry { UnitName = name, Level = 1, Exp = 0 });
    }

    static readonly string[] s_namePool =
    {
        // ── 1~100 (기존) ──────────────────────────────────────
        "아서",     "드레이크",  "마커스",   "알드릭",   "레온",     "가레스",   "트리스탄",  "이반",     "로한",     "케인",
        "오웬",     "덱스터",   "그레이엄",  "페닉스",   "레이번",   "시리우스",  "막시무스",  "아론",     "브렌던",   "도미닉",
        "에릭",     "펠릭스",   "가브리엘",  "헥터",     "재스퍼",   "카일",     "말콤",     "나단",     "오스카",   "패트릭",
        "로드릭",   "솔로몬",   "티모시",   "빅터",     "월터",     "요나스",   "아킬레스",  "발리안",   "다미안",   "에드워드",
        "프레드릭",  "길버트",   "해롤드",   "야코브",   "킬리안",   "로렌즈",   "마그누스",  "니콜라스",  "피어스",   "레이먼드",
        "시그프리드", "발데마르",  "크레이그",  "라이덴",   "에반",     "코너",     "알렉산더",  "버나드",   "다리우스",  "에이든",
        "핀리",     "그리핀",   "헌터",     "아이작",   "자렛",     "라이언",   "모건",     "오리온",   "퀸시",     "리드",
        "샘슨",     "타이터스",  "빈센트",   "윌리엄",   "자비에르",  "아드리안",  "블레이즈",  "세드릭",   "더글라스",  "엘리엇",
        "플린",     "가빈",     "허큘리스",  "이고르",   "줄리안",   "케리건",   "레오",     "매슈",     "올리버",   "필립",
        "랄프",     "스탠리",   "토마스",   "울리히",   "발렌틴",   "웨슬리",   "젤드리스",  "알폰스",   "베르트랑",  "시그마",

        // ── 101~200 (이베리아·이탈리아 계열) ──────────────────
        "라파엘",   "로렌조",   "디에고",   "마테우스",  "페드로",   "후안",     "엔리케",   "리카르도",  "파블로",   "세바스티안",
        "마티아스",  "이그나시오", "살바도르",  "아우구스토", "호아킨",   "레안드로",  "세르지오",  "로베르토",  "체사레",   "아마데오",
        "에밀리오",  "파우스토",  "발레리오",  "마우리시오", "프란체스코", "지아코모",  "도나토",   "피에트로",  "폴리도로",  "토르콰토",
        "베네데토",  "니콜로",   "로돌포",   "발다사레",  "젠틸레",   "세르피코",  "포르투나토", "벨리사리오", "코시모",   "알도브란도",

        // ── 141~200 (북유럽 바이킹 계열) ──────────────────────
        "라그나르",  "에이릭",   "군나르",   "할도르",   "시구르드",  "토르발",   "스베인",   "이바르",   "뵈르크",   "하콘",
        "힐마르",   "스테이나르", "에이나르",  "비야르니",  "토르게이르", "아스게이르", "헤임달",   "오라르",   "아른베른",  "스카르디",

        // ── 201~300 (고대 로마·프랑크 계열) ──────────────────
        "가이우스",  "루시우스",  "티베리우스", "플라비우스", "옥타비우스", "카시우스",  "세베루스",  "스키피오",  "파비우스",  "갈바",
        "클라우디우스","코르넬리우스","아그리파",  "브루투스",  "포스투무스", "아우렐리우스","유니우스",  "만리우스",  "레굴루스",  "카밀루스",
        "클로드",   "에밀",     "미셸",     "앙투안",   "피에르",   "루이",     "롤랑",     "기욤",     "앙드레",   "샤를",
        "앙리",     "제라르",   "고드프루아", "보두앵",   "조프루아",  "에우도",   "위그",     "오도",     "티보",     "드로고",
        "울프강",   "디트리히",  "하인리히",  "루돌프",   "콘라드",   "게르하르트", "알브레히트", "프리드리히", "에버하르트", "헬무트",
        "만프레트",  "귄터",     "볼프",     "카를",     "한스",     "베른하르트", "클라우스",  "오토",     "지그문트",  "하르트만",

        // ── 301~400 (켈트·슬라브·동방 계열) ──────────────────
        "던컨",     "알라스테어", "핀바르",   "콜럼",     "로난",     "브레낸",   "코난",     "카란",     "파드리히",  "달라흐",
        "퍼시발",   "갤러해드",  "랜슬롯",   "가웨인",   "보르스",   "베디비어",  "루칸",     "케이",     "아르투어",  "이렌",
        "페리클레스", "알키비아스", "리산드로스", "크세노폰",  "클레온",   "니키아스",  "알케타스",  "테라메네스", "트라시볼로", "이피크라테",
        "드미트리",  "블라디미르", "알렉세이",  "보리스",   "스타니슬라프","세르게이",  "파벨",     "안드레이",  "니콜라이",  "미하일",
        "야로슬라프", "미로슬라프", "라디슬라프", "카지미르",  "비톨트",   "그레고르",  "스티에판",  "타데우스",  "체스와프",  "지기스문트",
        "루스탐",   "티무르",   "알탄",     "아르슬란",  "바가투르",  "타르칸",   "에를란",   "잘마",     "다우렌",   "이스마일",
        "카림",     "타리크",   "자이드",   "파루크",   "살라흐",   "나시르",   "마르완",   "오마르",   "유수프",   "타흐밀",
        "샤푸르",   "아르다시르", "발라쉬",   "코로스",   "파르나케스", "티그라네스", "후스로",   "바흐람",   "파르파크",  "아르다반",
        "고드윈",   "에드먼드",  "에설레드",  "알프레드",  "우흐트레드", "시게베르트", "에오프릭",  "라드반",   "라흐발드",  "이겐베르트",
        "아르망",   "오스발트",  "베르나르",  "이벨린",   "레날두스",  "드루몽",   "아모리",   "셀레우스",  "트라야누스", "에게리우스",
    };

    // ── 직렬화 전용 내부 클래스 ──────────────────────────────

    [Serializable]
    class UnitRawData
    {
        public List<UnitEntry> Units = new();
    }
}

// ── 유닛 항목 ─────────────────────────────────────────────────

[Serializable]
public class UnitEntry
{
    public string UnitName;         // PoolController 풀 키와 동일하게 저장 (스폰 시 PoolKey 로 사용)
    public int    Level        = 1;
    public int    Exp          = 0;
    public int    GradeUpCount = 0;   // 등급 업그레이드 횟수 (태생 등급은 UnitName 시드로 결정)

    // 태생 등급은 이름 시드로 결정적 계산 — 저장 불필요
    public UnitGrade BirthGrade => UnitJobRoller.GetBirthGrade(UnitName);
    // 현재 등급 = 태생 등급 + 업그레이드 횟수 (최대 Epic)
    public UnitGrade Grade      => (UnitGrade)Mathf.Min((int)BirthGrade + GradeUpCount, (int)UnitGrade.Epic);

    // 직업(UnitJob)은 UnitName 시드로 결정적 배정 — UnitJobRoller.GetJob(UnitName) 으로 조회

    // ── 용병 보너스 (용병조각으로 영구 증가) ─────────────────
    public int SoldierBonus = 0;

    // ── 런 장비 슬롯 (회귀 시 초기화) ─────────────────────────
    /// <summary>슬롯 0~1 장착 중인 장비 ID. 비어있으면 "".</summary>
    public string[] RunEquipSlots   = new string[2];
    /// <summary>슬롯별 강화 레벨 (0 = 미강화).</summary>
    public int[]    RunEquipEnhance = new int[2];

    internal void EnsureEquipArrays()
    {
        if (RunEquipSlots == null || RunEquipSlots.Length < 2)
            RunEquipSlots = new string[2];
        if (RunEquipEnhance == null || RunEquipEnhance.Length < 2)
            RunEquipEnhance = new int[2];
    }
}
