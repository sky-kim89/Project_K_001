using Unity.Collections;
using Unity.Entities;
using BattleGame.Units;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;

// ============================================================
//  GeneralPanelUI.cs
//  장군 한 명의 HUD 패널.
//
//  GeneralRuntimeBridge.OnSpawned 이벤트를 받아 InGameHUD 가 Setup() 을 호출한다.
//  LateUpdate 마다 ECS 에서 HP·병사 수·스킬 쿨다운·버프를 읽어 UI 를 갱신한다.
//
//  ■ 표시 규칙 (2026-08-08 개편)
//    · 좌측 등급 색 바 + 초상화 위 직업 칩 — 로비 장수 카드와 같은 언어.
//    · HP 바는 비율에 따라 초록 → 노랑 → 빨강 으로 색이 변한다.
//      숫자를 읽지 않아도 위험한 장군이 한눈에 잡힌다.
//    · 사망 시 흐리게 + "전사" 배지.
//
//  ⚠ 모든 글자는 UIScale.FontSm(34) 이상이다
//    예전엔 11~24px 를 직접 박아 놔서 실기에서 읽히지 않았다 (UI 규칙 4).
//    칸 높이도 UIScale.Line(폰트) 이상으로 잡아야 아래가 잘리지 않는다 (규칙 5).
//
//  Hierarchy — UISetupTool.CreateGeneralPanelPrefab() 이 만든다.
// ============================================================

public class GeneralPanelUI : MonoBehaviour
{
    [Header("초상화 · 신원")]
    [SerializeField] Image                _gradeBorder;   // 좌측 등급 색 바
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitIcon;  // CharacterBuilder Texture 에서 추출한 Idle_0 스프라이트
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _jobChipText;   // 초상화 좌하단 직업 칩
    [SerializeField] TextMeshProUGUI      _gradeText;
    [SerializeField] GameObject           _deadBadge;     // "전사" 배지

    [Header("HP")]
    [SerializeField] Image                _hpFill;
    [SerializeField] TextMeshProUGUI      _hpText;

    [Header("병사")]
    [SerializeField] Image                _soldierFill;
    [SerializeField] TextMeshProUGUI      _soldierText;

    [Header("스킬")]
    [SerializeField] SkillSlotUI          _skillSlot;

    [Header("버프 아이콘 슬롯")]
    [SerializeField] Image[]              _buffSlots;
    [SerializeField] TextMeshProUGUI[]       _buffStackTexts;

    // ── 직업·HP 색 ─────────────────────────────────────────────
    static readonly Color[] s_JobColors =
    {
        new Color(0.80f, 0.22f, 0.22f, 1f),  // Knight       — 붉은
        new Color(0.22f, 0.70f, 0.28f, 1f),  // Archer       — 녹색
        new Color(0.25f, 0.42f, 0.90f, 1f),  // Mage         — 파랑
        new Color(0.28f, 0.60f, 0.75f, 1f),  // ShieldBearer — 청록
    };

    // HP 비율에 따른 바 색 — 숫자를 안 읽어도 위험도가 보인다
    static readonly Color HpFull = new Color(0.35f, 0.90f, 0.45f, 1f);   // 초록
    static readonly Color HpWarn = new Color(0.95f, 0.80f, 0.25f, 1f);   // 노랑
    static readonly Color HpLow  = new Color(0.92f, 0.26f, 0.24f, 1f);   // 빨강

    // ── 런타임 상태 ─────────────────────────────────────────────
    EntityManager          _em;
    Entity                 _entity;
    EntityQuery            _soldierQuery;

    /// <summary>
    /// 마지막으로 읽은 최대 체력.
    ///
    /// ⚠ 이 값은 캐시가 아니라 '직전 프레임의 사본' 이다
    ///   최대 체력은 고정값이 아니다 — UnitStatusEffectSystem 이 매 프레임
    ///   Final[MaxHp] 를 버프로 다시 계산하고, 늘어난 만큼 CurrentHp 까지 올린다
    ///   (특성 K4 전우의 분노 = 병사 사망마다 +1%, 전투함성·장비·어빌리티 등).
    ///   롤 시점의 Base 를 분모로 박아 두면 바가 100% 에 붙고 "1350/1200" 이 찍힌다.
    ///   RefreshHp 가 매 프레임 갱신하고, 여기 남은 값은 엔티티가 사라진 뒤
    ///   사망 표시에만 쓴다.
    /// </summary>
    float                  _maxHp;
    int                    _maxSoldierCount;
    bool                   _initialized;
    readonly int[]         _grpIds    = new int[16];
    readonly int[]         _grpCounts = new int[16];

    /// <summary>중복 생성 방지를 위해 InGameHUD 가 참조하는 브릿지.</summary>
    public GeneralRuntimeBridge LinkedBridge { get; private set; }

    // ── 공개 API ─────────────────────────────────────────────────

    /// <summary>
    /// GeneralRuntimeBridge.Initialize() 완료 직후 InGameHUD 가 호출.
    /// portrait = 직업 초상화 스프라이트 (없으면 null 허용).
    /// skillIcon = 배정된 액티브 스킬 아이콘 스프라이트 (없으면 null 허용).
    /// </summary>
    public void Setup(GeneralRuntimeBridge bridge, Sprite portrait, Sprite skillIcon)
    {
        var link = bridge.GetComponent<EntityLink>();
        if (link == null)
        {
            Debug.LogWarning("[GeneralPanelUI] GeneralRuntimeBridge 에 EntityLink 없음");
            return;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        LinkedBridge = bridge;
        _em          = world.EntityManager;
        _entity      = link.Entity;

        // ── 초기 스탯값 ──────────────────────────────────────
        //
        //  ⚠ 최대 체력은 여기서 확정하지 않는다 — 첫 프레임까지의 씨앗일 뿐이다
        //    버프로 계속 변하는 값이라 RefreshHp 가 매 프레임 다시 읽는다 (_maxHp 주석 참고).
        //
        //  ⚠ 병사 수는 반대로 여기서 확정한다
        //    분모는 "실제로 스폰된 병사 수" 라야 한다. SpawnSoldiers 도 같은 롤 값
        //    (Base[SoldierCount])으로 스폰하므로 둘이 일치한다. 전투 중 병사 수 버프는
        //    이미 세워 둔 병사를 늘리지 않으니 분모도 따라 움직이면 안 된다.
        var stat         = bridge.GetRolledStat();
        _maxHp           = Mathf.Max(1f, stat.Get(StatType.MaxHp));
        _maxSoldierCount = Mathf.Max(1, Mathf.RoundToInt(stat.Get(StatType.SoldierCount)));

        // ── 병사 ECS 쿼리 생성 (DeadTag 없는 병사만) ─────────
        _soldierQuery = _em.CreateEntityQuery(
            ComponentType.ReadOnly<SoldierComponent>(),
            ComponentType.Exclude<DeadTag>());

        // ── 직업 읽기 (ECS) ───────────────────────────────────
        UnitJob job = UnitJob.Knight;
        if (_em.Exists(_entity) && _em.HasComponent<UnitJobComponent>(_entity))
            job = _em.GetComponentData<UnitJobComponent>(_entity).Job;

        // ── UI 초기값 설정 ────────────────────────────────────
        int       jobIdx = Mathf.Clamp((int)job, 0, s_JobColors.Length - 1);
        UnitGrade grade  = bridge.Grade;

        if (_portraitBg   != null) _portraitBg.color    = s_JobColors[jobIdx];
        if (_portraitIcon != null) _portraitIcon.sprite = portrait;
        if (_nameText     != null) _nameText.text       = bridge.UnitName ?? bridge.name;
        if (_jobChipText  != null) _jobChipText.text    = JobStyle.GetLabel(job);
        if (_skillSlot    != null)
        {
            _skillSlot.SetIcon(skillIcon);
            _skillSlot.BindClick(UseActiveSkill);
        }

        // 등급 — 로비와 같은 색·라벨을 쓴다 (GradeStyle 이 정본)
        if (_gradeBorder != null) _gradeBorder.color = GradeStyle.GetColor(grade);
        if (_gradeText   != null)
        {
            _gradeText.text  = GradeStyle.GetLabel(grade);
            _gradeText.color = GradeStyle.GetColor(grade);
        }

        if (_deadBadge != null) _deadBadge.SetActive(false);

        foreach (var slot in _buffSlots)
            if (slot != null) slot.gameObject.SetActive(false);

        gameObject.SetActive(true);
        _initialized = true;
    }

    // ── 프레임 갱신 ─────────────────────────────────────────────

    void LateUpdate()
    {
        if (!_initialized || _entity == Entity.Null) return;

        _em.CompleteAllTrackedJobs();

        if (!_em.Exists(_entity))
        {
            ApplyDeadState(true);
            return;
        }

        bool dead = _em.HasComponent<DeadTag>(_entity);
        ApplyDeadState(dead);
        if (dead)
        {
            // 사망 시 HP 0 표시
            if (_hpFill != null) _hpFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            if (_hpText != null) _hpText.text = $"0/{Mathf.RoundToInt(_maxHp)}";
            RefreshSoldiers();  // 장군 사망 후에도 잔존 병사 수 갱신
            return;
        }

        RefreshHp();
        RefreshSoldiers();
        RefreshSkill();
        RefreshBuffs();
    }

    // ── 개별 갱신 ─────────────────────────────────────────────

    void RefreshHp()
    {
        if (!_em.HasComponent<HealthComponent>(_entity)) return;

        // 분모부터 최신으로 맞춘다 — 버프가 최대 체력을 올린 프레임에 CurrentHp 도 함께 오른다.
        // 낡은 분모를 쓰면 cur > _maxHp 가 되어 바가 꽉 찬 채로 굳는다.
        _maxHp = CurrentMaxHp();

        float cur   = _em.GetComponentData<HealthComponent>(_entity).CurrentHp;
        float ratio = Mathf.Clamp01(cur / _maxHp);

        // anchorMax.x 방식: Image.type 에 무관하게 동작
        if (_hpFill != null)
        {
            _hpFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            _hpFill.color = HpColorFor(ratio);
        }
        if (_hpText != null) _hpText.text = $"{Mathf.CeilToInt(cur)}/{Mathf.RoundToInt(_maxHp)}";
    }

    /// <summary>
    /// 지금 이 순간의 최대 체력 — 버프까지 반영된 값.
    ///
    /// ⚠ Base 가 아니라 Final 을 읽는다
    ///   Base 는 성장·장비로 확정된 롤 값이고, 전투 중 버프는 Final 에만 얹힌다.
    ///   전투 시스템이 전부 Final 로 판정하므로 HUD 도 같은 값을 봐야 한다.
    ///
    /// 스탯 컴포넌트가 없는 프레임(스폰 직후 등)에는 직전 값을 그대로 쓴다.
    /// </summary>
    float CurrentMaxHp()
    {
        if (!_em.HasComponent<StatComponent>(_entity)) return _maxHp;
        return Mathf.Max(1f, _em.GetComponentData<StatComponent>(_entity).Final[StatType.MaxHp]);
    }

    /// <summary>50% 위는 초록→노랑, 아래는 노랑→빨강.</summary>
    static Color HpColorFor(float ratio)
        => ratio > 0.5f
            ? Color.Lerp(HpWarn, HpFull, (ratio - 0.5f) * 2f)
            : Color.Lerp(HpLow,  HpWarn, ratio * 2f);

    void RefreshSoldiers()
    {
        int alive = 0;

        if (!_soldierQuery.IsEmpty)
        {
            var arr = _soldierQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < arr.Length; i++)
            {
                if (_em.GetComponentData<SoldierComponent>(arr[i]).GeneralEntity == _entity)
                    alive++;
            }
            arr.Dispose();
        }

        float ratio = Mathf.Clamp01((float)alive / _maxSoldierCount);
        if (_soldierFill != null) _soldierFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        if (_soldierText != null) _soldierText.text       = $"{alive}/{_maxSoldierCount}";
    }

    void RefreshSkill()
    {
        if (_skillSlot == null) return;
        if (!_em.HasComponent<GeneralActiveSkillComponent>(_entity)) return;

        var skill = _em.GetComponentData<GeneralActiveSkillComponent>(_entity);
        _skillSlot.UpdateCooldown(skill.CooldownRemaining, skill.Cooldown);
        _skillSlot.SetUsable(SkillUsePolicy.CanUseNow(_em, _entity));
    }

    // ── 수동 스킬 사용 ────────────────────────────────────────
    //
    //  AUTO 토글이 꺼져 있으면 이 클릭이 스킬을 내보내는 유일한 경로다.
    //  판정은 자동 발동(ActiveSkillAISystem)과 같은 SkillUsePolicy 를 쓴다 —
    //  여기만 느슨하게 두면 사거리 밖에서 눌러 쿨다운만 날리게 된다.
    //
    //  태그만 붙이고 끝낸다. 쿨다운 리셋과 실행 이벤트 생성은
    //  ActiveSkillCooldownSystem 이 같은 프레임에 처리한다.

    void UseActiveSkill()
    {
        if (!_initialized || _entity == Entity.Null) return;
        if (!SkillUsePolicy.CanUseNow(_em, _entity)) return;

        _em.AddComponent<UseActiveSkillTag>(_entity);
    }

    void RefreshBuffs()
    {
        if (_buffSlots == null || _buffSlots.Length == 0) return;
        if (!_em.HasBuffer<StatusEffectBufferElement>(_entity)) return;

        var buffs  = _em.GetBuffer<StatusEffectBufferElement>(_entity, true);
        int unique = 0;

        // 활성 버프를 (SourceType, SourceId) 기준으로 그룹화 (스택 카운트)
        for (int i = 0; i < buffs.Length; i++)
        {
            bool active = buffs[i].Duration < 0f || buffs[i].Remaining > 0f;
            if (!active) continue;

            int key = PackKey(buffs[i].SourceType, buffs[i].SourceId);
            if (key == 0) continue;

            bool found = false;
            for (int k = 0; k < unique; k++)
            {
                if (_grpIds[k] == key) { _grpCounts[k]++; found = true; break; }
            }
            if (!found && unique < _grpIds.Length)
            {
                _grpIds[unique]    = key;
                _grpCounts[unique] = 1;
                unique++;
            }
        }

        // 버프 슬롯 표시 — 아이콘 없는 그룹은 슬롯 소비 안 함
        int slotIdx = 0;
        for (int g = 0; g < unique && slotIdx < _buffSlots.Length; g++)
        {
            Sprite icon = ResolveBuffIcon(_grpIds[g]);
            if (icon == null) continue;
            ShowSlot(slotIdx++, icon, _grpCounts[g]);
        }

        // 미사용 슬롯 비활성화
        for (int i = slotIdx; i < _buffSlots.Length; i++)
        {
            if (_buffSlots[i] != null) _buffSlots[i].gameObject.SetActive(false);
        }
    }

    static int PackKey(BuffSourceType type, int id)
        => type == BuffSourceType.None ? 0 : ((int)type << 24) | (id & 0xFFFFFF);

    void ShowSlot(int idx, Sprite icon, int stackCount)
    {
        var slot = _buffSlots[idx];
        if (slot == null) return;
        slot.gameObject.SetActive(true);
        slot.sprite = icon;

        if (_buffStackTexts != null && idx < _buffStackTexts.Length)
        {
            var txt = _buffStackTexts[idx];
            if (txt != null)
            {
                bool show = stackCount > 1;
                txt.gameObject.SetActive(show);
                if (show) txt.text = stackCount.ToString();
            }
        }
    }

    Sprite ResolveBuffIcon(int packedKey)
    {
        var sourceType = (BuffSourceType)(packedKey >> 24);
        int sourceId   = packedKey & 0xFFFFFF;
        return sourceType switch
        {
            BuffSourceType.ActiveSkill => GetActiveSkillIcon(sourceId),
            BuffSourceType.Passive     => GetPassiveIcon(sourceId),
            BuffSourceType.Ability     => GetAbilityIcon(sourceId),
            BuffSourceType.Equipment   => GetEquipSlotIcon(sourceId),
            _                          => null,
        };
    }

    Sprite GetActiveSkillIcon(int skillId)
    {
        string key = ((ActiveSkillId)skillId).IconKey();
        if (key == null) return null;
        var manager = SpriteManager.Instance;
        return manager != null ? manager.Get(key) : null;
    }

    Sprite GetPassiveIcon(int typeId)
    {
        var db = PassiveSkillDatabase.Current;
        if (db == null) return null;
        var data = db.Get((PassiveSkillType)typeId);
        return data != null ? data.Icon : null;
    }

    Sprite GetAbilityIcon(int abilityId)
    {
        var db = AbilityDatabase.Current;
        if (db == null) return null;
        var data = db.Get((AbilityId)abilityId);
        return data != null ? data.Icon : null;
    }

    Sprite GetEquipSlotIcon(int slotIndex)
    {
        if (!_em.HasComponent<GeneralTriggerSetComponent>(_entity)) return null;
        var trigSet = _em.GetComponentObject<GeneralTriggerSetComponent>(_entity);
        if (trigSet == null || slotIndex >= trigSet.EquipSlots.Length) return null;
        var equip = trigSet.EquipSlots[slotIndex];
        return equip != null ? equip.Icon : null;
    }

    void ApplyDeadState(bool dead)
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = dead ? 0.42f : 1f;
        if (_deadBadge != null && _deadBadge.activeSelf != dead)
            _deadBadge.SetActive(dead);

        // 죽은 장수 카드는 눌러도 안 나간다 — RefreshSkill 이 도는 경로가 아니므로 여기서 끈다
        if (dead && _skillSlot != null) _skillSlot.SetUsable(false);
    }

    // ── 정리 ─────────────────────────────────────────────────

    void OnDestroy()
    {
        if (_em != default)
            _soldierQuery.Dispose();
    }
}
