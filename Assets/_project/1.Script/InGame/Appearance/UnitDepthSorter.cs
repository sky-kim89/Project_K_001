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

    /// <summary>order 가 int 범위를 벗어나지 않도록 Y 를 잘라 둔다.</summary>
    const float MaxY = 200f;

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
        _renderers  = GetComponentsInChildren<SpriteRenderer>(true);
        _baseOrders = new int[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _baseOrders[i] = _renderers[i].sortingOrder;
    }

    void LateUpdate()
    {
        if (_renderers == null || _renderers.Length == 0) return;

        // 아래(Y 작음)에 있을수록 앞 = order 가 커야 한다
        float y      = Mathf.Clamp(transform.position.y, -MaxY, MaxY);
        int   offset = -Mathf.RoundToInt(y * Step);
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
    public void Rescan()
    {
        Cache();
        _lastOffset = int.MinValue;
    }
}
