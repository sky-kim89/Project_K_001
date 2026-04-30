using System;
using System.Linq;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.Common.Scripts.Utils;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  HeroCardUI.cs
//  영웅 목록에서 장군 1명을 표시하는 카드 컴포넌트.
//  초상화는 PortraitPreview GO 의 CharacterBuilder 로 생성.
// ============================================================

public class HeroCardUI : MonoBehaviour
{
    [SerializeField] Image                _gradeBorder;
    [SerializeField] Image                _portraitBg;
    [SerializeField] Image                _portraitImage;
    [SerializeField] UnitAppearanceBridge _portraitBridge;
    [SerializeField] TextMeshProUGUI      _nameText;
    [SerializeField] TextMeshProUGUI      _levelText;
    [SerializeField] TextMeshProUGUI      _gradeText;
    [SerializeField] TextMeshProUGUI      _jobText;
    [SerializeField] TextMeshProUGUI      _hpText;
    [SerializeField] TextMeshProUGUI      _atkText;
    [SerializeField] TextMeshProUGUI      _defText;
    [SerializeField] TextMeshProUGUI      _soldierText;
    [SerializeField] Button               _button;

    // HeroPanelUI 와 동일한 직업 배경색
    static readonly Color[] JobBgColors =
    {
        new Color(0.50f, 0.14f, 0.14f),  // Knight
        new Color(0.14f, 0.45f, 0.18f),  // Archer
        new Color(0.16f, 0.27f, 0.56f),  // Mage
        new Color(0.18f, 0.38f, 0.48f),  // ShieldBearer
    };

    public UnitEntry Entry { get; private set; }

    Action<UnitEntry> _onSelect;
    Texture2D         _portraitTexture;

    // ── 공개 API ─────────────────────────────────────────────

    public void Setup(UnitEntry entry, Action<UnitEntry> onSelect)
    {
        Entry     = entry;
        _onSelect = onSelect;

        if (_nameText  != null) _nameText.text  = entry.UnitName;
        if (_levelText != null) _levelText.text = $"Lv.{entry.Level}";
        if (_gradeText != null) _gradeText.text = GradeStyle.GetLabel(entry.Grade);

        Color gc = GradeStyle.GetColor(entry.Grade);
        if (_gradeBorder != null) _gradeBorder.color = gc;
        if (_gradeText   != null) _gradeText.color   = gc;

        UnitJob  job  = UnitJobRoller.GetJob(entry.UnitName);
        UnitStat stat = GeneralStatRoller.Roll(entry.UnitName, entry.Level, entry.Grade);
        if (_jobText     != null) _jobText.text     = JobStyle.GetLabel(job);
        if (_hpText      != null) _hpText.text      = $"{stat.Get(StatType.MaxHp):N0}";
        if (_atkText     != null) _atkText.text      = $"{stat.Get(StatType.Attack):N0}";
        if (_defText     != null) _defText.text      = $"{stat.Get(StatType.Defense) * 100f:F1}%";
        if (_soldierText != null) _soldierText.text  = $"{Mathf.RoundToInt(stat.Get(StatType.SoldierCount))}명";

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelect?.Invoke(Entry));
        }

        UpdatePortrait(entry);
    }

    public void SetSelected(bool selected)
    {
        if (_gradeBorder == null) return;
        _gradeBorder.color = selected
            ? Color.white
            : GradeStyle.GetColor(Entry?.Grade ?? UnitGrade.Normal);
    }

    // ── 초상화 ───────────────────────────────────────────────

    void UpdatePortrait(UnitEntry entry)
    {
        UnitJob job = UnitJobRoller.GetJob(entry.UnitName);

        if (_portraitBg != null)
            _portraitBg.color = JobBgColors[Mathf.Clamp((int)job, 0, JobBgColors.Length - 1)];

        if (_portraitBridge == null || _portraitImage == null) return;

        var builder = _portraitBridge.GetComponent<CharacterBuilder>();
        if (builder == null) return;

        // SpriteCollection 미할당이면 Resources 에서 로드
        if (builder.SpriteCollection == null)
        {
            builder.SpriteCollection = Resources.Load<SpriteCollection>("SpriteCollection");
            if (builder.SpriteCollection == null) return;
        }

        // 외형 데이터 적용 (Rebuild() 대신 BuildLayers() 사용 — Character GO 불필요)
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
        builder.Firearm = "";  // Character.Firearm.Detached 접근 필요 → 초상화에서 생략

        var layers = builder.BuildLayers();
        if (layers.Count == 0) return;

        if (_portraitTexture == null)
            _portraitTexture = new Texture2D(576, 928) { filterMode = FilterMode.Point };

        TextureHelper.MergeLayers(_portraitTexture, layers.Values.ToArray());
        _portraitImage.sprite = ExtractPortraitSprite(_portraitTexture);
        _portraitImage.color  = Color.white;
    }

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
