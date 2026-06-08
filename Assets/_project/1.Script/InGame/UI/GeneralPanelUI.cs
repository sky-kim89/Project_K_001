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
//  Hierarchy 예시:
//    GeneralPanel (GeneralPanelUI, CanvasGroup)
//      ├ Portrait        (Image — _portraitBg)
//      │  └ PortraitIcon (Image — _portraitIcon, 선택)
//      ├ NameText        (TMP — _nameText)
//      ├ GradeText       (TMP — _gradeText, 선택)
//      ├ HpBar           (Image — Filled Horizontal, _hpFill)
//      ├ HpText          (TMP — _hpText, 선택)
//      ├ SoldierBar      (Image — Filled Horizontal, _soldierFill)
//      ├ SoldierText     (TMP — _soldierText, 선택)
//      ├ SkillSlot       (SkillSlotUI — _skillSlot)
//      └ BuffSlot0~3     (Image — _buffSlots[])
// ============================================================

public class GeneralPanelUI : MonoBehaviour
{
    [Header("초상화")]
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitIcon;  // CharacterBuilder Texture 에서 추출한 Idle_0 스프라이트
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _gradeText;

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

    // ── 직업·등급 표현 상수 ─────────────────────────────────────
    static readonly Color[] s_JobColors =
    {
        new Color(0.80f, 0.22f, 0.22f, 1f),  // Knight       — 붉은
        new Color(0.22f, 0.70f, 0.28f, 1f),  // Archer       — 녹색
        new Color(0.25f, 0.42f, 0.90f, 1f),  // Mage         — 파랑
        new Color(0.28f, 0.60f, 0.75f, 1f),  // ShieldBearer — 청록
    };

    static readonly string[] s_GradeLabels = { "", "UC", "R", "U", "Epic" };

    // ── 런타임 상태 ─────────────────────────────────────────────
    EntityManager          _em;
    Entity                 _entity;
    EntityQuery            _soldierQuery;
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
        int jobIdx = Mathf.Clamp((int)job, 0, s_JobColors.Length - 1);
        if (_portraitBg   != null) _portraitBg.color    = s_JobColors[jobIdx];
        if (_portraitIcon != null) _portraitIcon.sprite = portrait;
        if (_nameText     != null) _nameText.text        = bridge.UnitName ?? bridge.name;
        if (_gradeText    != null) _gradeText.text       = s_GradeLabels[0];  // 등급 공개 시 교체
        if (_skillSlot    != null) _skillSlot.SetIcon(skillIcon);

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

        float cur   = _em.GetComponentData<HealthComponent>(_entity).CurrentHp;
        float ratio = Mathf.Clamp01(cur / _maxHp);

        // anchorMax.x 방식: Image.type 에 무관하게 동작
        if (_hpFill != null) _hpFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        if (_hpText != null) _hpText.text = $"{Mathf.CeilToInt(cur)}/{Mathf.RoundToInt(_maxHp)}";
    }

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
        if (cg != null) cg.alpha = dead ? 0.38f : 1f;
    }

    // ── 정리 ─────────────────────────────────────────────────

    void OnDestroy()
    {
        if (_em != default)
            _soldierQuery.Dispose();
    }
}
