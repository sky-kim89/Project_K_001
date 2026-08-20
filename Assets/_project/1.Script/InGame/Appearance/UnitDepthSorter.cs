using UnityEngine;

// ============================================================
//  UnitDepthSorter.cs
//  유닛의 앞뒤(그리는 순서)를 **발 위치 Y** 로 정한다.
//
//  ■ 왜 필요한가 — 축 정렬만으로는 큰 유닛이 항상 뒤로 간다
//    프로젝트는 GraphicsSettings 의 Transparency Sort Axis = (0,1,0) 으로
//    스프라이트를 Y 로 정렬한다. 그런데 이 정렬이 쓰는 값은 transform 위치가
//    아니라 **렌더러 bounds 의 중심** 이다.
//    유닛의 원점은 발밑(그림자 Square 가 0,0 에 있다)이고 스프라이트는 위로
//    뻗으므로, bounds 중심은 늘 원점보다 (키/2) 만큼 위에 있다.
//    보스는 루트 스케일이 2 배라 이 (키/2) 도 두 배가 된다 —
//    같은 자리에 서 있어도 정렬값이 훨씬 커져 **항상 뒤에 그려졌다.**
//
//  ■ 해결 — sortingOrder 를 Y 로 직접 준다
//    sortingOrder 는 축 정렬보다 먼저 적용되므로 크기와 무관하게 발 위치로만
//    앞뒤가 갈린다. 같은 order 안에서는 기존 축 정렬이 미세 순서를 맡는다
//    (같은 크기끼리는 bounds 중심 차이가 곧 발 위치 차이라 그대로 맞다).
//
//  ⚠ Step 은 한 유닛 안의 레이어 간격보다 커야 한다
//    프리팹은 Body=100 · Renderer(무기)=105 로 5 만큼 벌어져 있다.
//    Step 이 그보다 작으면 뒤에 선 유닛의 무기가 앞 유닛의 몸을 덮는다.
//
//  ⚠ 그림자(Square = -1)처럼 음수로 잡아 둔 레이어도 같은 폭으로 따라간다
//    프리팹의 상대 순서를 그대로 유지해야 한 유닛의 구성이 흐트러지지 않는다.
//
//  붙이는 곳: UnitRuntimeBridge 가 스폰 시 자동으로 붙인다.
// ============================================================

[DefaultExecutionOrder(20)]   // EntityLink(0) · UnitAnimationSync(10) 뒤 — 위치가 확정된 다음
public class UnitDepthSorter : MonoBehaviour
{
    /// <summary>Y 1 유닛당 벌어지는 sortingOrder 폭.</summary>
    const int Step = 8;

    /// <summary>
    /// 정렬에 쓰는 Y 반경. 화면 클램프 범위(카메라 orthographicSize)와 같은 규모다.
    /// 이 밖으로 나간 유닛은 양 끝에 몰려 같은 order 를 쓰고, 미세 순서는 축 정렬이 맡는다.
    /// </summary>
    const float HalfBand = 4f;

    /// <summary>
    /// 오프셋 상한 = HalfBand * 2 * Step. 이 값이 곧 '유닛 정렬 띠' 의 두께다.
    ///
    /// ⚠ 이 띠는 배경(-2)과 이펙트(200) 사이에 갇혀 있어야 한다
    ///   프리팹 기준값은 그림자 -1 · 몸통 100 · 무기 105 다. 오프셋은 0 이상이라
    ///   그림자가 배경 아래로 내려가지 않고, 최대 64 라 무기도 169 에서 멈춘다.
    ///   (이펙트는 51종이 200, 범위 표시 4종이 90 이다 — 200 까지 31 의 여유를 둔다)
    ///   예전엔 상한이 없어 재사용 때마다 값이 밀려 올라갔고(그 버그는 Cache 에서 고쳤다),
    ///   200 을 넘는 순간 **유닛이 이펙트를 덮어** 폭발·타격 연출이 통째로 가려졌다.
    ///   (순교 폭발이 안 보이던 증상이 이것이다)
    /// </summary>
    const int MaxOffset = (int)(HalfBand * 2f) * Step;   // 64

    SpriteRenderer[] _renderers;
    int[]            _baseOrders;
    int              _lastOffset = int.MinValue;

    void Awake() => Cache();

    void OnEnable()
    {
        // 풀에서 다시 꺼내면 이전 오프셋이 남아 첫 프레임을 건너뛴다 — 강제로 다시 계산
        _lastOffset = int.MinValue;
    }

    void Cache()
    {
        // ⚠ 적용해 둔 오프셋을 먼저 벗긴다
        //   여기서 읽는 sortingOrder 가 다음 기준값이 된다. 이전 오프셋이 얹힌 채로
        //   읽으면 그 오프셋이 기준값으로 굳어, 풀에서 꺼낼 때마다 정렬값이
        //   Step 만큼 계속 밀려 올라간다 (재사용 5번이면 100 → 300).
        //   Rebuild 로 사라진 렌더러는 null 이라 알아서 건너뛰고,
        //   새로 붙은 렌더러는 프리팹 값 그대로라 벗길 것이 없다.
        if (_renderers != null && _lastOffset != int.MinValue)
        {
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].sortingOrder -= _lastOffset;
        }

        _renderers  = GetComponentsInChildren<SpriteRenderer>(true);
        _baseOrders = new int[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _baseOrders[i] = _renderers[i].sortingOrder;

        _lastOffset = int.MinValue;
    }

    void LateUpdate()
    {
        if (_renderers == null || _renderers.Length == 0) return;

        // 아래(Y 작음)에 있을수록 앞 = order 가 커야 한다.
        // 띠 위쪽(Y = +HalfBand)이 0, 아래쪽(Y = -HalfBand)이 MaxOffset 이다.
        float y      = Mathf.Clamp(transform.position.y, -HalfBand, HalfBand);
        int   offset = Mathf.Clamp(Mathf.RoundToInt((HalfBand - y) * Step), 0, MaxOffset);
        if (offset == _lastOffset) return;   // 값이 그대로면 렌더러를 건드리지 않는다
        _lastOffset = offset;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].sortingOrder = _baseOrders[i] + offset;
        }
    }

    /// <summary>
    /// 외형이 새로 조립돼 SpriteRenderer 구성이 바뀌었을 때 다시 훑는다.
    /// (CharacterBuilder.Rebuild 처럼 자식을 갈아 끼우는 경로에서 부른다)
    /// </summary>
    public void Rescan() => Cache();
}
