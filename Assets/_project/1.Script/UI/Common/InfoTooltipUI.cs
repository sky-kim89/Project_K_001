using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  InfoTooltipUI.cs
//  "이름 / 설명 / 스탯" 3줄짜리 공용 툴팁.
//
//  ■ 붙이는 위치
//    툴팁 패널 GameObject 자신에게 붙인다 (기본 비활성).
//    소유자(TraitIconUI · RewardCardUI)가 참조를 들고 Show/Close 를 호출한다.
//
//  ■ 동작
//    Show()  : 루트 캔버스로 옮겨 최상단에 띄운다 (부모에 가려지지 않게).
//    Update(): 아무 곳이나 클릭하면 닫는다. 여는 클릭은 _skipFrame 으로 무시.
//    Close() : 원래 부모로 되돌리고 비활성화.
//
//  Update 는 툴팁이 열려 있는 동안(=활성)에만 돈다.
//
//  Inspector 연결: TraitIconSlotBuilder · MainPanelCreator · UISetupTool 자동.
// ============================================================

public class InfoTooltipUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _descText;
    [SerializeField] TextMeshProUGUI _statText;

    Transform     _originalParent;
    RectTransform _rect;
    Vector2       _originalAnchored;   // 프리팹이 정해 둔 "부모 아래에서의 자리"
    bool          _parentCaptured;
    bool          _skipFrame;

    public bool IsOpen => gameObject.activeSelf;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>내용을 채우고 띄운다. desc·stat 이 비면 해당 줄은 숨긴다.</summary>
    public void Show(string title, string desc, string stat)
    {
        CaptureParent();

        _nameText.text = title;

        _descText.text = desc ?? "";
        _descText.gameObject.SetActive(!string.IsNullOrEmpty(desc));

        _statText.text = stat ?? "";
        _statText.gameObject.SetActive(!string.IsNullOrEmpty(stat));

        // 옮기기 전에 반드시 원래 자리로 되돌린다.
        //
        // ⚠ 위치가 누적으로 밀리던 원인
        //   나갈 때는 SetParent(root, true) — 월드 위치를 지키느라
        //   anchoredPosition 이 루트 캔버스 기준으로 새로 계산된다.
        //   돌아올 때는 SetParent(parent, false) — 그 계산된 값을 그대로 둔다.
        //   즉 한 번 열고 닫을 때마다 "부모 아래에서의 자리" 가 어긋나고,
        //   다음 Show 는 그 어긋난 월드 위치에서 또 계산해 오차가 쌓인다.
        //   (몇 번 누르면 Pos Y 가 42896 처럼 화면 밖으로 튄다)
        Restore();

        gameObject.SetActive(true);

        // 부모(아이콘·카드)에 가려지지 않도록 루트 캔버스 최상단으로 옮긴다.
        var canvas = _originalParent.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var root = canvas.rootCanvas;
            transform.SetParent(root.transform, true);
            transform.SetAsLastSibling();
            FitIntoCanvas(root.transform as RectTransform);
        }

        _skipFrame = true;   // 여는 클릭이 그대로 닫기로 이어지지 않게
    }

    // ── 화면 안으로 밀어넣기 ─────────────────────────────────
    //  툴팁은 기본적으로 소유 아이콘의 좌하단에서 아래로 펼쳐진다.
    //  아이콘이 화면 아래쪽·오른쪽 끝에 있으면 그대로 두면 밖으로 나간다.
    //    · 아래로 펼칠 자리가 없으면 → 아이콘 위로 뒤집는다
    //    · 오른쪽이 넘치면        → 왼쪽으로 민다
    //  뒤집기는 "화면 중심보다 아래" 가 아니라 "실제로 넘치는가" 로 판단한다.
    //  넘치지 않는데 굳이 뒤집으면 툴팁이 아이콘을 가려 더 불편하다.
    void FitIntoCanvas(RectTransform canvasRt)
    {
        if (canvasRt == null) return;

        const float Margin = 12f;

        // CSF 가 높이를 잡기 전이면 rect.height 가 0 이다 — 먼저 확정시킨다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);

        float cw = canvasRt.rect.width, ch = canvasRt.rect.height;
        float w  = _rect.rect.width,    h  = _rect.rect.height;

        // 앵커 (0,0) · 피벗 (0,1) → anchoredPosition = 툴팁 좌상단 (캔버스 좌하단 기준)
        Vector2 pos = _rect.anchoredPosition;

        // ── 세로 ─────────────────────────────────────────────
        if (pos.y - h < Margin)
        {
            // 아이콘 위로 뒤집는다. 지금 위치는 "아이콘 아래 4px" 이므로
            // 아이콘 아래변 = pos.y + 4, 여기에 아이콘 높이를 더하면 윗변이다.
            float ownerH   = _originalParent is RectTransform ort ? ort.rect.height : 0f;
            float ownerTop = pos.y + 4f + ownerH;
            pos.y = ownerTop + 4f + h;
        }
        pos.y = Mathf.Clamp(pos.y, h + Margin, ch - Margin);

        // ── 가로 ─────────────────────────────────────────────
        if (pos.x + w > cw - Margin) pos.x = cw - Margin - w;
        if (pos.x < Margin)          pos.x = Margin;

        _rect.anchoredPosition = pos;
    }

    public void Close()
    {
        Restore();
        gameObject.SetActive(false);
        _skipFrame = false;
    }

    // ── 내부 ─────────────────────────────────────────────────

    // 툴팁 GO 는 처음부터 비활성이라 Awake 가 늦게 돈다.
    // 첫 Show 때(=아직 옮기기 전) 원래 부모와 자리를 잡아 둔다.
    void CaptureParent()
    {
        if (_parentCaptured) return;
        _rect             = GetComponent<RectTransform>();
        _originalParent   = transform.parent;
        _originalAnchored = _rect.anchoredPosition;
        _parentCaptured   = true;
    }

    /// <summary>부모·자리를 프리팹이 정해 둔 상태로 되돌린다 (여러 번 불러도 안전).</summary>
    void Restore()
    {
        if (!_parentCaptured) return;

        if (transform.parent != _originalParent)
            transform.SetParent(_originalParent, false);

        _rect.anchoredPosition = _originalAnchored;
    }

    void Update()
    {
        if (_skipFrame) { _skipFrame = false; return; }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            Close();
    }
}
