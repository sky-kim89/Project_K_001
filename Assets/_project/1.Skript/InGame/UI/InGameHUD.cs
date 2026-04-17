using System.Collections.Generic;
using Unity.Entities;
using BattleGame.Units;
using UnityEngine;
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

    [Header("아이콘 (ActiveSkillId 순서 — 0번은 None / 빈 칸)")]
    [SerializeField] Sprite[] _skillIcons;

    // ── 런타임 ─────────────────────────────────────────────────
    readonly List<GeneralPanelUI> _panels = new();

    // ── 초기화 ──────────────────────────────────────────────────

    void Awake()
    {
        GeneralRuntimeBridge.OnSpawned += HandleGeneralSpawned;
    }

    void Start()
    {
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
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    void HandleGeneralSpawned(GeneralRuntimeBridge bridge)
    {
        if (_generalPanelPrefab == null || _generalPanelContainer == null) return;
        if (_panels.Count >= _maxGeneralPanels) return;

        var go    = Instantiate(_generalPanelPrefab, _generalPanelContainer);
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

    Sprite ResolveSkillIcon(GeneralRuntimeBridge bridge)
    {
        if (_skillIcons == null || _skillIcons.Length == 0) return null;

        var link = bridge.GetComponent<EntityLink>();
        if (link == null || link.Entity == Entity.Null) return null;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return null;

        var em = world.EntityManager;
        if (!em.HasComponent<GeneralActiveSkillComponent>(link.Entity)) return null;

        int skillId = em.GetComponentData<GeneralActiveSkillComponent>(link.Entity).SkillId;
        return skillId >= 0 && skillId < _skillIcons.Length ? _skillIcons[skillId] : null;
    }
}
