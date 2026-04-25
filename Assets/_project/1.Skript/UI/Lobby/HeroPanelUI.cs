using System.Collections.Generic;
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
    [SerializeField] TextMeshProUGUI _soldierCountText;

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
    UnitEntry _selected;

    // ── 라이프사이클 ──────────────────────────────────────────

    void Awake()
    {
        _equip0Btn?.onClick.AddListener(() => OnEquipSlotClick(0));
        _equip1Btn?.onClick.AddListener(() => OnEquipSlotClick(1));
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
        _hpText.text          = $"{stat.Get(StatType.MaxHp):N0}";
        _atkText.text         = $"{stat.Get(StatType.Attack):N0}";
        _defText.text         = $"{stat.Get(StatType.Defense) * 100f:F1}%";
        _spdText.text         = $"{stat.Get(StatType.MoveSpeed):F1}";
        _soldierCountText.text = $"{Mathf.RoundToInt(stat.Get(StatType.SoldierCount))}명";

        // ── 장비 ────────────────────────────────────────────
        var equipDb = EquipmentDatabase.Current;
        RefreshEquipSlot(0, entry, equipDb, _equip0NameText, _equip0GradeBar);
        RefreshEquipSlot(1, entry, equipDb, _equip1NameText, _equip1GradeBar);

        // ── 스킬 ────────────────────────────────────────────
        var activeDb  = ActiveSkillDatabase.Current;
        var rolledId  = ActiveSkillRoller.Roll(entry.UnitName, job, activeDb);
        var skillData = activeDb?.Get(rolledId);
        _activeSkillText.text = skillData?.SkillName ?? rolledId.ToString();
    }

    void UpdatePortrait(UnitEntry entry, UnitJob job)
    {
        // 직업 배경색
        if (_portraitBg != null)
            _portraitBg.color = JobBgColors[Mathf.Clamp((int)job, 0, JobBgColors.Length - 1)];

        if (_portraitBridge == null || _portraitImage == null) return;

        _portraitBridge.ApplyAlly(entry.UnitName, job, entry.Grade);

        var builder = _portraitBridge.GetComponent<CharacterBuilder>();
        if (builder?.Texture == null) return;

        _portraitImage.sprite = ExtractPortraitSprite(builder.Texture);
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
