using System.Collections.Generic;
using System.Linq;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.Common.Scripts.Utils;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ============================================================
//  HeroPanelUI.cs
//  영웅(장군) 탭 패널
//
//  레이아웃:
//    LeftPanel  (430px) — 초상화 + 스탯 + 장비 + 스킬
//    RightPanel (648px) — 2열 카드 스크롤 리스트
//
//  초상화 흐름:
//    SelectHero → _portraitBridge.ApplyAlly() → CharacterBuilder.Rebuild()
//    → Texture.Idle_0 프레임 Sprite 추출 (InGameHUD 동일 방식)
// ============================================================

public class HeroPanelUI : MonoBehaviour
{
    // ── 초상화 ────────────────────────────────────────────────
    [Header("초상화")]
    [SerializeField] Image                _portraitBg;     // 직업 색상 배경
    [SerializeField] Image                _portraitImage;  // 추출된 캐릭터 스프라이트
    [SerializeField] UnitAppearanceBridge _portraitBridge; // PortraitPreview GO 의 Bridge

    // ── 기본 정보 (초상화 하단 오버레이) ─────────────────────
    [Header("기본 정보")]
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _levelText;
    [SerializeField] Image           _gradeBadge;
    [SerializeField] TextMeshProUGUI _gradeText;
    [SerializeField] TextMeshProUGUI _jobText;

    // ── 스탯 ──────────────────────────────────────────────────
    [Header("스탯")]
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] TextMeshProUGUI _atkText;
    [SerializeField] TextMeshProUGUI _defText;
    [SerializeField] TextMeshProUGUI _spdText;
    [SerializeField] TextMeshProUGUI _atkSpdText;
    [SerializeField] TextMeshProUGUI _rangeText;
    [SerializeField] TextMeshProUGUI _soldierCountText;
    [SerializeField] TextMeshProUGUI _cmdPwrText;

    // ── 장비 슬롯 ─────────────────────────────────────────────
    [Header("장비")]
    [SerializeField] Button          _equip0Btn;
    [SerializeField] TextMeshProUGUI _equip0NameText;
    [SerializeField] Image           _equip0GradeBar;
    [SerializeField] Button          _equip1Btn;
    [SerializeField] TextMeshProUGUI _equip1NameText;
    [SerializeField] Image           _equip1GradeBar;

    // ── 스킬 ──────────────────────────────────────────────────
    [Header("스킬")]
    [SerializeField] TextMeshProUGUI _activeSkillText;
    [SerializeField] TextMeshProUGUI _activeSkillDescText;

    // ── 패시브 스킬 ───────────────────────────────────────────
    [Header("패시브 스킬")]
    [SerializeField] TextMeshProUGUI _passive0Text;
    [SerializeField] TextMeshProUGUI _passive1Text;
    [SerializeField] TextMeshProUGUI _passive2Text;

    // ── 스킬 DB (로비 전용 직접 참조) ─────────────────────────
    [Header("스킬 DB")]
    [SerializeField] ActiveSkillDatabase  _activeSkillDatabase;
    [SerializeField] PassiveSkillDatabase _passiveSkillDatabase;

    // ── 영웅 목록 ─────────────────────────────────────────────
    [Header("영웅 목록")]
    [SerializeField] Transform  _listContent;
    [SerializeField] HeroCardUI _cardPrefab;

    // ── 직업 배경색 (GeneralPanelUI.s_JobColors 와 동일) ──────
    static readonly Color[] JobBgColors =
    {
        new Color(0.50f, 0.14f, 0.14f),  // Knight       — 붉은
        new Color(0.14f, 0.45f, 0.18f),  // Archer       — 녹색
        new Color(0.16f, 0.27f, 0.56f),  // Mage         — 파랑
        new Color(0.18f, 0.38f, 0.48f),  // ShieldBearer — 청록
    };

    // ── 런타임 ────────────────────────────────────────────────
    readonly List<HeroCardUI> _cards = new();
    UnitEntry  _selected;
    Texture2D  _portraitTexture;

    // ── 라이프사이클 ──────────────────────────────────────────

    void Awake()
    {
        _equip0Btn?.onClick.AddListener(() => OnEquipSlotClick(0));
        _equip1Btn?.onClick.AddListener(() => OnEquipSlotClick(1));
        FixStatLabelAlignment();
    }

    void OnEnable() => Refresh();

    // ── 공개 API ──────────────────────────────────────────────

    public void Refresh()
    {
        var units = UserDataManager.Instance?.Get<UnitData>()?.Units;
        if (units == null) return;

        BuildCardList(units);
        if (_cards.Count > 0)
            SelectHero(_cards[0].Entry);
    }

    // ── 내부 ──────────────────────────────────────────────────

    void BuildCardList(IReadOnlyList<UnitEntry> units)
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        foreach (var entry in units)
        {
            var card = Instantiate(_cardPrefab, _listContent);
            card.Setup(entry, SelectHero);
            _cards.Add(card);
        }
    }

    void SelectHero(UnitEntry entry)
    {
        _selected = entry;
        foreach (var c in _cards)
            c.SetSelected(c.Entry == entry);
        UpdateDetail(entry);
    }

    void UpdateDetail(UnitEntry entry)
    {
        UnitJob  job  = UnitJobRoller.GetJob(entry.UnitName);
        UnitStat stat = GeneralStatRoller.Roll(entry.UnitName, entry.Level, entry.Grade);

        // ── 초상화 ──────────────────────────────────────────
        UpdatePortrait(entry, job);

        // ── 기본 정보 ────────────────────────────────────────
        _nameText.text  = entry.UnitName;
        _levelText.text = $"Lv.{entry.Level}";
        _jobText.text   = JobStyle.GetLabel(job);
        _gradeText.text = GradeStyle.GetLabel(entry.Grade);

        Color gc          = GradeStyle.GetColor(entry.Grade);
        _gradeBadge.color = gc;
        _gradeText.color  = gc;

        // ── 스탯 ────────────────────────────────────────────
        _hpText.text           = $"{stat.Get(StatType.MaxHp):N0}";
        _atkText.text          = $"{stat.Get(StatType.Attack):N0}";
        _defText.text          = $"{stat.Get(StatType.Defense) * 100f:F1}%";
        _spdText.text          = $"{stat.Get(StatType.MoveSpeed):F1}";
        if (_atkSpdText      != null) _atkSpdText.text      = $"{stat.Get(StatType.AttackSpeed):F2}/초";
        if (_rangeText       != null) _rangeText.text       = $"{stat.Get(StatType.AttackRange):F1}";
        _soldierCountText.text = $"{Mathf.RoundToInt(stat.Get(StatType.SoldierCount))}명";
        if (_cmdPwrText      != null) _cmdPwrText.text      = $"{stat.Get(StatType.CommandPower):N0}";

        // ── 장비 ────────────────────────────────────────────
        var equipDb = EquipmentDatabase.Current;
        RefreshEquipSlot(0, entry, equipDb, _equip0NameText, _equip0GradeBar);
        RefreshEquipSlot(1, entry, equipDb, _equip1NameText, _equip1GradeBar);

        // ── 스킬 ────────────────────────────────────────────
        var activeDb  = _activeSkillDatabase != null ? _activeSkillDatabase : ActiveSkillDatabase.Current;
        var rolledId  = ActiveSkillRoller.Roll(entry.UnitName, job, activeDb);
        var skillData = activeDb?.Get(rolledId);
        _activeSkillText.text = GetKoreanSkillName(rolledId);
        if (_activeSkillDescText != null)
            _activeSkillDescText.text = skillData != null ? skillData.Description : "";

        // ── 패시브 스킬 ─────────────────────────────────────
        var passiveDb = _passiveSkillDatabase != null ? _passiveSkillDatabase : PassiveSkillDatabase.Current;
        var (p0, p1, p2) = PassiveSkillRoller.Roll(entry.UnitName);
        byte slots = PassiveSkillRoller.GetActiveSlotCount(entry.Grade);
        RefreshPassiveSlot(_passive0Text, passiveDb, p0, slots >= 1);
        RefreshPassiveSlot(_passive1Text, passiveDb, p1, slots >= 2);
        RefreshPassiveSlot(_passive2Text, passiveDb, p2, slots >= 3);
    }

    void UpdatePortrait(UnitEntry entry, UnitJob job)
    {
        // 직업 배경색
        if (_portraitBg != null)
            _portraitBg.color = JobBgColors[Mathf.Clamp((int)job, 0, JobBgColors.Length - 1)];

        if (_portraitBridge == null || _portraitImage == null) return;

        var builder = _portraitBridge.GetComponent<CharacterBuilder>();
        if (builder == null) return;

        // SpriteCollection 미할당 시 Resources 에서 로드 (Resources/SpriteCollection.asset)
        if (builder.SpriteCollection == null)
        {
            builder.SpriteCollection = Resources.Load<SpriteCollection>("SpriteCollection");
            if (builder.SpriteCollection == null)
            {
                Debug.LogWarning("[HeroPanelUI] SpriteCollection 을 Resources 에서 찾을 수 없습니다.");
                return;
            }
        }

        // 외형 데이터 적용 — Rebuild() 는 Character GO 가 필요하므로 사용 불가
        var data = AllyAppearanceRoller.Roll(entry.UnitName, job, entry.Grade);
        builder.Body    = data.Body;
        builder.Head    = data.Head;
        builder.Ears    = data.Ears;
        builder.Eyes    = data.Eyes;
        builder.Hair    = data.Hair;
        builder.Armor   = data.Armor;
        builder.Helmet  = data.Helmet;
        builder.Mask    = data.Mask;
        builder.Horns   = data.Horns;
        builder.Cape    = data.Cape;
        builder.Weapon  = data.Weapon;
        builder.Shield  = data.Shield;
        builder.Back    = data.Back;
        builder.Firearm = "";  // Character.Firearm.Detached 접근 필요 → 초상화에선 생략

        // BuildLayers() 는 Character 없이도 동작 — Firearm 레이어만 제외됨
        var layers = builder.BuildLayers();
        if (layers.Count == 0) return;

        if (_portraitTexture == null)
            _portraitTexture = new Texture2D(576, 928) { filterMode = FilterMode.Point };

        TextureHelper.MergeLayers(_portraitTexture, layers.Values.ToArray());

        _portraitImage.sprite = ExtractPortraitSprite(_portraitTexture);
        _portraitImage.color  = Color.white;
    }

    void RefreshEquipSlot(int slot, UnitEntry entry, EquipmentDatabase db,
                          TextMeshProUGUI nameText, Image gradeBar)
    {
        string id    = (entry.RunEquipSlots != null && slot < entry.RunEquipSlots.Length)
                       ? entry.RunEquipSlots[slot] : "";
        var    equip = db?.Get(id);

        if (equip == null)
        {
            nameText.text    = "없음";
            if (gradeBar != null) gradeBar.color = new Color(0.25f, 0.25f, 0.30f);
            return;
        }

        int enhance      = (entry.RunEquipEnhance != null && slot < entry.RunEquipEnhance.Length)
                           ? entry.RunEquipEnhance[slot] : 0;
        nameText.text    = enhance > 0 ? $"{equip.EquipmentName} +{enhance}" : equip.EquipmentName;
        if (gradeBar != null) gradeBar.color = GradeStyle.GetColor(equip.Grade);
    }

    void OnEquipSlotClick(int slot)
    {
        // TODO: 장비 선택 팝업 열기
        Debug.Log($"[HeroPanelUI] 장비 슬롯 {slot} 클릭 — 영웅: {_selected?.UnitName}");
    }

    // 스탯 행의 Label TMP를 Left 정렬로 맞춤 (프리팹이 Right로 생성됐을 경우 런타임 보정)
    void FixStatLabelAlignment()
    {
        FixStatLabel(_hpText);
        FixStatLabel(_atkText);
        FixStatLabel(_defText);
        FixStatLabel(_spdText);
        FixStatLabel(_atkSpdText);
        FixStatLabel(_rangeText);
        FixStatLabel(_soldierCountText);
        FixStatLabel(_cmdPwrText);
    }

    static void FixStatLabel(TextMeshProUGUI valueText)
    {
        if (valueText == null) return;
        foreach (Transform child in valueText.transform.parent)
        {
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null && tmp != valueText)
            {
                tmp.alignment = TextAlignmentOptions.Left;
                break;
            }
        }
    }

    void RefreshPassiveSlot(TextMeshProUGUI text, PassiveSkillDatabase db,
                            PassiveSkillType type, bool active)
    {
        if (text == null) return;
        if (!active)
        {
            text.text  = "—";
            text.color = new Color(0.40f, 0.40f, 0.40f);
            return;
        }
        var data   = db != null ? db.Get(type) : null;
        text.text  = data != null ? data.SkillName : type.ToString();
        text.color = Color.white;
    }

    static string GetKoreanSkillName(ActiveSkillId id) => id switch
    {
        ActiveSkillId.HeavyStrike      => "강타",
        ActiveSkillId.VolleyFire       => "일제 사격",
        ActiveSkillId.LeapStrike       => "도약 강타",
        ActiveSkillId.HealAura         => "치유 오라",
        ActiveSkillId.TargetHeal       => "집중 치유",
        ActiveSkillId.ChargeSoldier    => "돌격 병사",
        ActiveSkillId.SummonSkeleton   => "스켈레톤 소환",
        ActiveSkillId.PoisonZone       => "독성 지대",
        ActiveSkillId.Meteor           => "메테오",
        ActiveSkillId.Blizzard         => "블리자드",
        ActiveSkillId.SacrificeSoldier => "병사 희생",
        ActiveSkillId.Bind             => "속박",
        ActiveSkillId.SuicideSoldier   => "자폭 병사",
        ActiveSkillId.Berserker        => "광전사",
        ActiveSkillId.IronShield       => "철벽 방어",
        ActiveSkillId.ArrowRain        => "화살 비",
        ActiveSkillId.BattleCry        => "전투 함성",
        ActiveSkillId.Shockwave        => "충격파",
        ActiveSkillId.SwiftStrike      => "신속 연격",
        ActiveSkillId.SummonElite      => "정예 소환",
        _                              => id.ToString(),
    };

    // ── InGameHUD.GetPortraitSprite() 와 동일한 추출 로직 ────

    static Sprite ExtractPortraitSprite(Texture2D texture)
    {
        var l  = CharacterBuilder.Layout["Idle_0"];
        int fx = l[0], fy = l[1], fw = l[2], fh = l[3];

        var pixels = texture.GetPixels(fx, fy, fw, fh);
        int minX = fw, maxX = 0, minY = fh, maxY = 0;

        for (int py = 0; py < fh; py++)
            for (int px = 0; px < fw; px++)
                if (pixels[py * fw + px].a > 0.01f)
                {
                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;
                }

        if (minX > maxX || minY > maxY)
            return Sprite.Create(texture, new Rect(fx, fy, fw, fh),
                new Vector2(0.5f, 0.5f), 16, 0, SpriteMeshType.FullRect);

        const int pad = 2;
        minX = Mathf.Max(0,      minX - pad);
        minY = Mathf.Max(0,      minY - pad);
        maxX = Mathf.Min(fw - 1, maxX + pad);
        maxY = Mathf.Min(fh - 1, maxY + pad);

        return Sprite.Create(
            texture,
            new Rect(fx + minX, fy + minY, maxX - minX + 1, maxY - minY + 1),
            new Vector2(0.5f, 0.5f), 16, 0, SpriteMeshType.FullRect);
    }
}
