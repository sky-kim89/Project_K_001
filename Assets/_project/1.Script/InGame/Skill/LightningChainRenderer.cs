using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  LightningChainRenderer.cs
//  활성 전기 체인 전부를 메시 하나에 그리는 배치 렌더러.
//
//  ■ 왜 필요한가
//    폭우 사격은 착탄마다 A→B 선을 하나씩 띄운다. 병사까지 이 특성을 쓰면서
//    동시에 수십 발이 뜨게 됐는데, 선 하나를 GameObject + LineRenderer 로 만들면
//    인스턴스 수가 곧 드로우콜 수가 된다 (60발 = 60콜).
//
//    선은 결국 사각형 두어 개다. 전부 한 메시에 밀어 넣고 MeshRenderer 하나로
//    그리면 몇 개가 뜨든 드로우콜은 항상 1 이다.
//
//  ■ 왜 프리팹에서 머티리얼을 읽어 오나
//    Resources 에 같은 머티리얼을 하나 더 두면 원본과 갈라진다.
//    FX_RedLightning_Chain 프리팹이 이미 그 머티리얼을 들고 풀에 등록돼 있으므로
//    거기서 sharedMaterial 만 빌려 쓴다 (출처 하나 유지).
//    → 프리팹은 더 이상 스폰되지 않지만 "머티리얼 보관처" 로 남는다.
//
//  ■ 좌표계
//    2D 라 빌보드 계산이 필요 없다. 방향 d 와 그 수직 perp 만으로 사각형이 나온다.
//    메시는 월드 좌표로 만들고 트랜스폼은 원점에 둔다.
//
//  사용:
//    LightningChainRenderer.Instance.Add(from, to, life);
// ============================================================

[DisallowMultipleComponent]
public class LightningChainRenderer : MonoBehaviour
{
    // ── 튜닝 값 ──────────────────────────────────────────────

    /// <summary>
    /// 동시 표시 상한. 드로우콜은 개수와 무관하게 1 이므로 남는 비용은
    /// 매 프레임 메시 재구성(CPU)과 가산 블렌딩 오버드로우(GPU) 뿐이다.
    ///
    /// 실사용 기준 동시 체인은 RainFireSplash.EffectLife × 초당 체인 수로 정해진다
    /// — 수명 0.28초에 최대 400발/초면 약 112개. 순간 몰릴 때를 감안해 여유를 둔 값이다.
    /// 넘치는 분은 피해만 들어가고 선은 생략된다.
    /// </summary>
    public const int MaxChains = 160;

    const int SortingOrder = 200;      // 이펙트 대역 — EffectPrefabGenerator.EffectSortingOrder 와 같은 값

    // ⚠ 머티리얼 블렌드가 SrcAlpha One 이다 — 화면에 더해지는 양 = RGB × 알파.
    //   즉 알파가 곧 밝기다. 알파 0 인 정점은 아예 보이지 않는다.
    //   A(착탄한 적) → B(체인이 튄 적) 방향이며, "맞았다" 는 신호는 B 쪽이라
    //   B 를 더 굵고 밝게 잡는다. (예전 프리팹은 B 에 스파크·글로우 파티클을 얹어
    //   그 역할을 대신했는데, 그걸 걷어냈으니 선 자체가 그 몫까지 해야 한다)

    // ① 바깥 글로우 — 넓고 붉게 깔린다
    const float GlowWidthA = 0.34f;
    const float GlowWidthB = 0.46f;
    static readonly Color GlowColorA = new Color(1f, 0.18f, 0.10f, 0.45f);
    static readonly Color GlowColorB = new Color(1f, 0.30f, 0.16f, 0.75f);

    // ② 코어 — 얇고 희게, 글로우 위에 더해져 중심이 백열한다
    const float CoreWidthA = 0.10f;
    const float CoreWidthB = 0.16f;
    static readonly Color CoreColorA = new Color(1f, 0.85f, 0.75f, 0.85f);
    static readonly Color CoreColorB = new Color(1f, 0.97f, 0.92f, 1.00f);

    // ③ 착탄 섬광 — B 위치에 십자로 겹치는 짧은 막대 두 개.
    //    걷어낸 ImpactSparks / RedGlow 의 역할을 대신한다.
    const float FlareSize = 0.42f;
    static readonly Color FlareColor = new Color(1f, 0.55f, 0.40f, 1f);

    // 수명의 이 비율까지는 완전히 불투명, 이후 페이드아웃
    const float HoldRatio = 0.45f;

    // 체인 하나가 쓰는 최대 사각형 수 (글로우 + 코어 + 섬광 2)
    const int QuadsPerChain = 4;
    const int VertsPerQuad  = 6;    // 스테이션 3개 (양 끝 + 중간)
    const int TrisPerQuad   = 12;   // 삼각형 4개 × 3

    // ── 상태 ─────────────────────────────────────────────────

    struct Chain
    {
        public Vector3 A, B;
        public float   Age, Life;
    }

    readonly List<Chain>   _chains = new(MaxChains);
    readonly List<Vector3> _verts  = new(MaxChains * QuadsPerChain * VertsPerQuad);
    readonly List<Color>   _colors = new(MaxChains * QuadsPerChain * VertsPerQuad);
    readonly List<Vector2> _uvs    = new(MaxChains * QuadsPerChain * VertsPerQuad);
    readonly List<int>     _tris   = new(MaxChains * QuadsPerChain * TrisPerQuad);

    Mesh         _mesh;
    MeshRenderer _renderer;

    // ── 싱글턴 (필요할 때 자동 생성) ─────────────────────────
    //  Singleton<T> 는 씬 배치를 전제로 하고 DontDestroyOnLoad 를 건다.
    //  이 렌더러는 전투 씬과 수명을 같이하는 편이 맞으므로 직접 만든다.

    static LightningChainRenderer _instance;

    public static LightningChainRenderer Instance
    {
        get
        {
            // Unity 의 == 오버로드 덕에 씬이 바뀌어 파괴된 인스턴스도 null 로 걸러진다
            if (_instance != null) return _instance;

            var go = new GameObject(nameof(LightningChainRenderer));
            _instance = go.AddComponent<LightningChainRenderer>();
            return _instance;
        }
    }

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>from → to 로 전기 체인 하나를 life 초 동안 그린다.</summary>
    public void Add(Vector3 from, Vector3 to, float life)
    {
        if (life <= 0f) return;

        // 넘치면 그리지 않는다 — 피해는 호출자가 이미 적용했고,
        // 128발이 겹친 화면에서 129번째 선은 어차피 보이지 않는다.
        if (_chains.Count >= MaxChains) return;

        _chains.Add(new Chain { A = from, B = to, Age = 0f, Life = life });

#if UNITY_EDITOR
        // 첫 한 발만 알린다 — "호출 자체가 안 되는 것" 과 "그려지지 않는 것" 을 구분하기 위해.
        if (!_loggedFirstChain)
        {
            _loggedFirstChain = true;
            Debug.Log($"[LightningChainRenderer] 첫 체인 등록 {from} → {to} " +
                      $"(머티리얼: {(_renderer != null && _renderer.sharedMaterial != null ? _renderer.sharedMaterial.name : "없음")})");
        }
#endif
    }

#if UNITY_EDITOR
    bool _loggedFirstChain;
#endif

    /// <summary>첫 발에서 머티리얼 조회·메시 할당이 몰리지 않도록 미리 깨워 둔다.</summary>
    public static void Prepare() { _ = Instance; }

    // ── 초기화 ───────────────────────────────────────────────

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        transform.position = Vector3.zero;   // 메시를 월드 좌표로 만든다

        _mesh = new Mesh { name = "LightningChains" };
        _mesh.MarkDynamic();

        var filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = _mesh;

        _renderer = gameObject.AddComponent<MeshRenderer>();
        _renderer.sharedMaterial          = ResolveMaterial();
        _renderer.sortingOrder            = SortingOrder;
        _renderer.shadowCastingMode       = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows          = false;
        _renderer.lightProbeUsage         = UnityEngine.Rendering.LightProbeUsage.Off;
        _renderer.reflectionProbeUsage    = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        _renderer.enabled                 = false;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (_mesh != null) Destroy(_mesh);
    }

    // 풀에 등록된 FX 프리팹의 LineRenderer 머티리얼을 빌려 쓴다.
    //
    // 실패하면 머티리얼이 null 이 되고, MeshRenderer 는 아무것도 그리지 않은 채
    // 에러도 안 낸다 — "왜 안 보이지" 로 시간을 버리기 딱 좋은 실패 방식이라
    // 반드시 원인을 로그로 남기고, 최소한 흰 사각형이라도 보이게 대체한다.
    static Material ResolveMaterial()
    {
        var pool = PoolController.Instance;
        if (pool == null)
        {
            Debug.LogWarning("[LightningChainRenderer] PoolController 가 아직 없습니다 — 다음 프레임에 재시도합니다.");
            return null;
        }

        var prefab = pool.GetPrefab(PoolType.Effect, RainFireSplash.EffectKey);
        if (prefab == null)
        {
            Debug.LogError(
                $"[LightningChainRenderer] Effect 풀에 '{RainFireSplash.EffectKey}' 프리팹이 없습니다.\n" +
                "→ Tools > Project K > 프리팹 생성 > 이펙트 로 프리팹을 만든 뒤\n" +
                "  InGame 씬의 PoolController 에서 [Load Prefabs From Folder] 를 실행하고 씬을 저장하세요.");
            return FallbackMaterial();
        }

        if (!prefab.TryGetComponent<LineRenderer>(out var lr) || lr.sharedMaterial == null)
        {
            Debug.LogError(
                $"[LightningChainRenderer] '{RainFireSplash.EffectKey}' 루트에 LineRenderer 또는 머티리얼이 없습니다.\n" +
                "→ Tools > Project K > 프리팹 생성 > 이펙트 를 다시 실행해 프리팹을 갱신하세요.");
            return FallbackMaterial();
        }

        return lr.sharedMaterial;
    }

    // 텍스처는 없지만 최소한 눈에는 보인다 — 문제가 화면에 드러나야 고칠 수 있다.
    static Material FallbackMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;

        return new Material(shader) { name = "LightningChains_Fallback" };
    }

    // ── 매 프레임 갱신 ───────────────────────────────────────

    void LateUpdate()
    {
        float dt = Time.deltaTime;   // 배속(timeScale)을 그대로 따른다

        for (int i = _chains.Count - 1; i >= 0; i--)
        {
            Chain c = _chains[i];
            c.Age += dt;

            if (c.Age >= c.Life) _chains.RemoveAt(i);
            else                 _chains[i] = c;
        }

        if (_chains.Count == 0)
        {
            if (_renderer.enabled)
            {
                _mesh.Clear();
                _renderer.enabled = false;
            }
            return;
        }

        // Awake 가 PoolController 보다 먼저 돈 경우를 대비한 지연 재시도.
        // 머티리얼이 비면 아무것도 안 보이는데 원인이 드러나지 않으므로 여기서 한 번 더 잡는다.
        if (_renderer.sharedMaterial == null)
            _renderer.sharedMaterial = ResolveMaterial();

        Rebuild();
        _renderer.enabled = true;
    }

    // 체인 하나 = 글로우 + 코어 + 착탄 섬광(십자) → 사각형 최대 4개.
    // 전부 같은 메시·같은 머티리얼이라 몇 겹을 쌓아도 드로우콜은 여전히 1 이다.
    void Rebuild()
    {
        _verts.Clear();
        _colors.Clear();
        _uvs.Clear();
        _tris.Clear();

        foreach (Chain c in _chains)
        {
            float t    = c.Age / c.Life;
            float fade = Fade(t);

            // ① 바깥 글로우 → ② 코어 순서로 겹쳐 더한다
            AddBeam(c.A, c.B, GlowWidthA, GlowWidthB, GlowColorA, GlowColorB, fade);
            AddBeam(c.A, c.B, CoreWidthA, CoreWidthB, CoreColorA, CoreColorB, fade);

            // ③ 착탄 섬광 — 맞은 적(B) 위치에서 짧게 터졌다 사라진다
            float flare = FlareSize * FlarePulse(t);
            if (flare <= 0.001f) continue;

            Vector3 d = c.B - c.A;
            d.z = 0f;
            if (d.sqrMagnitude < 1e-8f) continue;
            d.Normalize();
            Vector3 perp = new Vector3(-d.y, d.x, 0f);

            AddBeam(c.B - d    * flare, c.B + d    * flare, flare, flare, FlareColor, FlareColor, fade);
            AddBeam(c.B - perp * flare, c.B + perp * flare, flare, flare, FlareColor, FlareColor, fade);
        }

        // 버텍스가 줄어든 프레임에 이전 인덱스가 남지 않도록 먼저 비운다
        _mesh.Clear();
        _mesh.SetVertices(_verts);
        _mesh.SetColors(_colors);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetTriangles(_tris, 0);

        // ⚠ 바운드를 직접 크게 잡는다.
        //   이 메시는 트랜스폼을 원점에 두고 월드 좌표로 정점을 만든다. 자동 계산된
        //   바운드에 의존하면 갱신 타이밍이 어긋난 프레임에 절두체 컬링으로 통째로
        //   사라질 수 있는데, 그 역시 아무 에러 없이 "그냥 안 보임" 으로 나타난다.
        //   전장 전체를 덮는 상자 하나면 컬링 문제를 원천 차단할 수 있고,
        //   어차피 화면에 항상 무언가는 떠 있으므로 컬링으로 아낄 것도 없다.
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

#if UNITY_EDITOR
        if (!_loggedFirstMesh && _verts.Count > 0)
        {
            _loggedFirstMesh = true;
            Debug.Log($"[LightningChainRenderer] 메시 생성 — 체인 {_chains.Count} / 정점 {_verts.Count} / 삼각형 {_tris.Count / 3}" +
                      $" / sortingOrder {_renderer.sortingOrder} / enabled {_renderer.enabled}" +
                      $" / mat {(_renderer.sharedMaterial != null ? _renderer.sharedMaterial.name : "없음")}");
        }
#endif
    }

#if UNITY_EDITOR
    bool _loggedFirstMesh;
#endif

    // a → b 로 이어지는 사각형 띠 하나. 스테이션 3개(양 끝 + 중간)로
    // 굵기·색이 부드럽게 이어진다.
    void AddBeam(Vector3 a, Vector3 b, float widthA, float widthB,
                 Color colorA, Color colorB, float fade)
    {
        Vector3 d = b - a;
        d.z = 0f;

        float len = d.magnitude;
        if (len < 0.0001f) return;
        d /= len;

        Vector3 perp = new Vector3(-d.y, d.x, 0f);   // XY 평면 수직
        Vector3 mid  = (a + b) * 0.5f;

        Vector3 hA = perp * (widthA * 0.5f);
        Vector3 hM = perp * ((widthA + widthB) * 0.25f);
        Vector3 hB = perp * (widthB * 0.5f);

        Color colorM = Color.Lerp(colorA, colorB, 0.5f);

        int i = _verts.Count;

        _verts.Add(a   + hA); _verts.Add(a   - hA);
        _verts.Add(mid + hM); _verts.Add(mid - hM);
        _verts.Add(b   + hB); _verts.Add(b   - hB);

        AddColor(colorA, fade); AddColor(colorA, fade);
        AddColor(colorM, fade); AddColor(colorM, fade);
        AddColor(colorB, fade); AddColor(colorB, fade);

        _uvs.Add(new Vector2(0f,   1f)); _uvs.Add(new Vector2(0f,   0f));
        _uvs.Add(new Vector2(0.5f, 1f)); _uvs.Add(new Vector2(0.5f, 0f));
        _uvs.Add(new Vector2(1f,   1f)); _uvs.Add(new Vector2(1f,   0f));

        // ⚠ 머티리얼이 Cull Back(_Cull: 2) 이라 감는 방향이 반대면 통째로 안 그려진다.
        //   에러도 경고도 없이 그냥 사라지기 때문에 원인을 찾기가 매우 어렵다.
        //
        //   그래서 양쪽 감기를 다 넣는다. 같은 자리에 앞·뒤 삼각형이 겹치지만
        //   백페이스 컬링이 둘 중 하나를 반드시 지우므로 실제로 그려지는 건 언제나 하나다.
        //   (가산 블렌딩에서 두 번 그려져 두 배로 밝아지는 일은 생기지 않는다)
        //   인덱스만 두 배로 늘 뿐이고 — 체인 128발 기준 1,536개 — 비용은 무시할 수준이다.
        AddQuad(i + 0, i + 1, i + 2, i + 3);
        AddQuad(i + 2, i + 3, i + 4, i + 5);
    }

    // (a0,a1) → (b0,b1) 두 스테이션을 잇는 사각형. 앞뒤 양방향으로 감는다.
    void AddQuad(int a0, int a1, int b0, int b1)
    {
        _tris.Add(a0); _tris.Add(a1); _tris.Add(b0);
        _tris.Add(a1); _tris.Add(b1); _tris.Add(b0);

        _tris.Add(b0); _tris.Add(a1); _tris.Add(a0);
        _tris.Add(b0); _tris.Add(b1); _tris.Add(a1);
    }

    void AddColor(Color c, float fade)
    {
        c.a *= fade;
        _colors.Add(c);
    }

    // 수명 앞쪽 HoldRatio 구간은 그대로, 이후 선형으로 사라진다.
    static float Fade(float t)
        => t <= HoldRatio ? 1f : 1f - (t - HoldRatio) / (1f - HoldRatio);

    // 착탄 섬광은 터지자마자 가장 크고 수명 절반에서 사라진다.
    static float FlarePulse(float t) => Mathf.Max(0f, 1f - t * 2f);
}
