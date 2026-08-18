using System.Collections.Generic;
using Unity.Entities;
using BattleGame.Units;
using UnityEngine;
using UnityEngine.UI;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;

// ============================================================
//  InGameHUD.cs
//  인게임 HUD 루트 컨트롤러.
//
//  역할:
//    - GeneralRuntimeBridge.OnSpawned 를 구독 → 장군 패널 동적 생성
//    - Start() 에서 이미 스폰된 장군 스캔 (씬 초기화 타이밍 대응)
//    - 포트레이트: CharacterBuilder.Texture 에서 Idle_0 프레임을 Sprite 로 잘라 표시
//
//  Inspector 설정:
//    TopBar              — TopBarUI 컴포넌트가 붙은 오브젝트
//    GeneralPanelPrefab  — GeneralPanelUI 가 붙은 프리팹
//    GeneralPanelContainer — 패널을 자식으로 붙일 부모 Transform
//    MaxGeneralPanels    — 표시할 최대 장군 수 (기본 5)
//    SkillIcons          — ActiveSkillId 인덱스에 맞는 스프라이트 배열 (0번 = 빈 슬롯용)
// ============================================================

public class InGameHUD : MonoBehaviour
{
    [Header("서브 UI")]
    [SerializeField] TopBarUI _topBar;

    [Header("장군 패널 풀")]
    [SerializeField] GameObject _generalPanelPrefab;
    [SerializeField] Transform  _generalPanelContainer;
    [SerializeField] int        _maxGeneralPanels = 5;


    // ── 런타임 ─────────────────────────────────────────────────
    readonly List<GeneralPanelUI> _panels = new();

    // ── 초기화 ──────────────────────────────────────────────────

    void Awake()
    {
        GeneralRuntimeBridge.OnSpawned += HandleGeneralSpawned;
        BattleManager.OnBattlePrepared += ClearGeneralPanels;
        SetupFixedSlotLayout();
    }

    /// <summary>
    /// 장수 카드를 전부 치운다. 새 전투를 준비할 때마다 호출된다.
    ///
    /// ⚠ 카드는 스폰 때 만들기만 하고 지우는 곳이 없었다
    ///   예전엔 전투가 끝나면 InGame 씬이 내려가 카드도 함께 사라졌다.
    ///   씬이 상주하는 지금은 카드가 계속 쌓인다 — 지난 판의 장수 카드가
    ///   옛 스탯 그대로 남고(체력 0 짜리 유령 카드), 5칸이 차면 새 장수는
    ///   아예 카드를 못 받아 "세팅이 안 된 것처럼" 보인다.
    /// </summary>
    void ClearGeneralPanels()
    {
        foreach (var panel in _panels)
            if (panel != null) Destroy(panel.gameObject);

        _panels.Clear();
    }

    // ── 카드 폭 고정 (항상 5칸 기준) ──────────────────────────
    //
    //  컨테이너의 childForceExpandWidth 를 켜 두면 카드가 인원수에 맞춰 늘어난다.
    //  2명이면 화면 절반씩 차지하고 5명이면 좁아져서, 용병을 고용할수록
    //  같은 장군의 카드가 계속 작아진다 — 슬롯 위치도 매번 달라진다.
    //  → 강제 확장을 끄고 카드마다 "1/5 폭" 을 직접 지정한다.
    //
    //  ⚠ 씬 프리팹을 다시 만들지 않아도 되도록 런타임에서도 끈다
    //    (UISetupTool 쪽도 같이 고쳐 뒀지만, 이미 저장된 씬에는 옛 설정이 남아 있다)

    void SetupFixedSlotLayout()
    {
        if (_generalPanelContainer == null) return;
        if (!_generalPanelContainer.TryGetComponent<HorizontalLayoutGroup>(out var hlg)) return;

        hlg.childForceExpandWidth = false;
        hlg.childAlignment        = TextAnchor.LowerLeft;   // 슬롯 위치가 늘 같아야 한다
    }

    /// <summary>카드 하나가 차지할 폭 — 컨테이너를 항상 _maxGeneralPanels 칸으로 나눈 값.</summary>
    void ApplyFixedSlotWidth(GameObject card)
    {
        if (_generalPanelContainer is not RectTransform contRT) return;

        float pad = 0f, spacing = 0f;
        if (_generalPanelContainer.TryGetComponent<HorizontalLayoutGroup>(out var hlg))
        {
            pad     = hlg.padding.left + hlg.padding.right;
            spacing = hlg.spacing;
        }

        int   slots = Mathf.Max(1, _maxGeneralPanels);
        float width = (contRT.rect.width - pad - spacing * (slots - 1)) / slots;
        if (width <= 0f) return;   // 레이아웃 전이라 폭을 모른다 — 다음 프레임에 자동으로 잡힌다

        if (!card.TryGetComponent<LayoutElement>(out var le))
            le = card.AddComponent<LayoutElement>();

        le.preferredWidth = width;
        le.flexibleWidth  = 0f;
    }

    void Start()
    {
        UIClickSfx.Bind(gameObject);   // 상단바 버튼(배속·AUTO·일시정지)에 클릭음

        // Awake 구독 이전에 이미 스폰된 장군 처리
        // (AllySpawner 실행 순서가 빠를 경우 OnSpawned 를 놓칠 수 있음)
        var existing = FindObjectsByType<GeneralRuntimeBridge>(FindObjectsSortMode.None);
        foreach (var b in existing)
        {
            if (_panels.Count >= _maxGeneralPanels) break;
            bool alreadyAdded = false;
            foreach (var p in _panels)
            {
                if (p.LinkedBridge == b) { alreadyAdded = true; break; }
            }
            if (!alreadyAdded) HandleGeneralSpawned(b);
        }
    }

    void OnDestroy()
    {
        GeneralRuntimeBridge.OnSpawned -= HandleGeneralSpawned;
        BattleManager.OnBattlePrepared -= ClearGeneralPanels;
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    void HandleGeneralSpawned(GeneralRuntimeBridge bridge)
    {
        if (_generalPanelPrefab == null || _generalPanelContainer == null) return;
        if (_panels.Count >= _maxGeneralPanels) return;

        var go    = Instantiate(_generalPanelPrefab, _generalPanelContainer);
        ApplyFixedSlotWidth(go);

        var panel = go.GetComponent<GeneralPanelUI>();
        if (panel == null)
        {
            Debug.LogWarning("[InGameHUD] GeneralPanelPrefab 에 GeneralPanelUI 없음");
            Destroy(go);
            return;
        }

        Sprite portrait  = GetPortraitSprite(bridge);
        Sprite skillIcon = ResolveSkillIcon(bridge);

        panel.Setup(bridge, portrait, skillIcon);
        _panels.Add(panel);
    }

    // ── 포트레이트 스프라이트 추출 ────────────────────────────
    // CharacterBuilder.Rebuild() 가 합성한 Texture2D 에서
    // Idle_0 프레임(64×64)을 Sprite 로 잘라 반환한다.
    // 카메라·RenderTexture 불필요 — CPU 복사도 없음.

    static Sprite GetPortraitSprite(GeneralRuntimeBridge bridge)
    {
        var builder = bridge.GetComponent<CharacterBuilder>();
        if (builder == null || builder.Texture == null) return null;

        var l = CharacterBuilder.Layout["Idle_0"]; // [x, y, w, h, pivotX, pivotY]
        int fx = l[0], fy = l[1], fw = l[2], fh = l[3];

        // 프레임 내 불투명 픽셀의 실제 경계를 찾아 tight crop
        // → 투명 여백 제거, 캐릭터가 portrait 박스를 꽉 채움
        var pixels = builder.Texture.GetPixels(fx, fy, fw, fh);
        int minX = fw, maxX = 0, minY = fh, maxY = 0;
        for (int py = 0; py < fh; py++)
        {
            for (int px = 0; px < fw; px++)
            {
                if (pixels[py * fw + px].a > 0.01f)
                {
                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;
                }
            }
        }

        // 불투명 픽셀이 없으면 전체 프레임 사용
        if (minX > maxX || minY > maxY)
            return Sprite.Create(builder.Texture,
                new Rect(fx, fy, fw, fh), new Vector2(0.5f, 0.5f),
                16, 0, SpriteMeshType.FullRect);

        // 약간의 여백(2px) 추가 후 프레임 범위 클램프
        const int pad = 2;
        minX = Mathf.Max(0,      minX - pad);
        minY = Mathf.Max(0,      minY - pad);
        maxX = Mathf.Min(fw - 1, maxX + pad);
        maxY = Mathf.Min(fh - 1, maxY + pad);

        return Sprite.Create(
            builder.Texture,
            new Rect(fx + minX, fy + minY, maxX - minX + 1, maxY - minY + 1),
            new Vector2(0.5f, 0.5f),
            16, 0, SpriteMeshType.FullRect);
    }

    // ── 스킬 아이콘 결정 ─────────────────────────────────────

    static Sprite ResolveSkillIcon(GeneralRuntimeBridge bridge)
    {
        var link = bridge.GetComponent<EntityLink>();
        if (link == null || link.Entity == Entity.Null) return null;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return null;

        var em = world.EntityManager;
        if (!em.HasComponent<GeneralActiveSkillComponent>(link.Entity)) return null;

        int skillId = em.GetComponentData<GeneralActiveSkillComponent>(link.Entity).SkillId;
        var id      = (ActiveSkillId)skillId;
        var key     = id.IconKey();
        return key != null ? SpriteManager.Instance?.Get(key) : null;
    }
}
