using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  PopupManager.cs
//  팝업 전체를 관리하는 Singleton.
//
//  사용법:
//    PopupManager.Instance.Open(PopupType.Alert);
//    var p = PopupManager.Instance.Open<AlertPopup>(PopupType.Alert);
//
//    PopupManager.Instance.Close(PopupType.Alert);
//    PopupManager.Instance.CloseTop();
//    PopupManager.Instance.CloseAll();
//
//    bool opened = PopupManager.Instance.IsOpen(PopupType.Alert);
//    var  p      = PopupManager.Instance.Get<AlertPopup>(PopupType.Alert);
// ============================================================

public class PopupManager : Singleton<PopupManager>
{
    [Header("팝업 프리팹 목록 (PopupBase.PopupType 으로 자동 분류)")]
    [SerializeField] PopupBase[] _prefabs;

    [Header("팝업 루트 (없으면 자동 생성)")]
    [SerializeField] Transform _popupRoot;

    [Header("블로커 색상")]
    [SerializeField] Color _blockerColor = new Color(0f, 0f, 0f, 0.45f);

    // ── 내부 자료구조 ─────────────────────────────────────────

    readonly Dictionary<PopupType, PopupBase> _prefabMap  = new();
    readonly Dictionary<PopupType, PopupBase> _pool       = new();   // 재사용 대기
    readonly Dictionary<PopupType, PopupBase> _openByType = new();   // 현재 열린 것
    readonly List<PopupBase>                  _stack      = new();   // 열린 순서

    GameObject _blocker;   // 단일 블로커

    // ── Unity 생명주기 ────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        foreach (var p in _prefabs)
            if (p != null) _prefabMap[p.PopupType] = p;
        EnsurePopupRoot();
    }

    // ── 공개 API — 열기 ──────────────────────────────────────

    public PopupBase Open(PopupType type, Action onClose = null)
        => RegisterAndOpen(type, onClose);

    public T Open<T>(PopupType type, Action onClose = null) where T : PopupBase
        => RegisterAndOpen(type, onClose) as T;

    // ── 공개 API — 닫기 ──────────────────────────────────────

    public void Close(PopupType type, Action onComplete = null)
    {
        if (_openByType.TryGetValue(type, out var popup))
            popup.Close(onComplete);
    }

    public void CloseTop(Action onComplete = null)
    {
        if (_stack.Count > 0)
            _stack[_stack.Count - 1].Close(onComplete);
    }

    public void CloseAll(Action onComplete = null)
    {
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            var popup = _stack[i];
            if (popup != null && popup.IsOpen)
                popup.Close(i == 0 ? onComplete : null);
        }
    }

    // ── 공개 API — 조회 ──────────────────────────────────────

    public bool IsOpen(PopupType type) => _openByType.ContainsKey(type);
    public bool HasAnyOpen             => _stack.Count > 0;
    public int  OpenCount              => _stack.Count;

    public T Get<T>(PopupType type) where T : PopupBase
    {
        _openByType.TryGetValue(type, out var popup);
        return popup as T;
    }

    // ── 내부 — 팝업 생성·등록 ────────────────────────────────

    PopupBase RegisterAndOpen(PopupType type, Action onClose)
    {
        EnsurePopupRoot();

        if (!_prefabMap.TryGetValue(type, out var prefab))
        {
            Debug.LogWarning($"[PopupManager] PopupType.{type} 에 등록된 프리팹이 없습니다.");
            return null;
        }

        // 같은 타입이 이미 열려 있으면 기존 것 먼저 닫기
        if (_openByType.TryGetValue(type, out var existing))
        {
            if (existing != null)
            {
                Debug.LogWarning($"[PopupManager] {type} 이미 열려 있음 → 기존 팝업 닫기.");
                existing.Close();
            }
            else
            {
                // 씬 언로드 등으로 오브젝트가 파괴된 경우 — 스택/맵에서 제거만
                _openByType.Remove(type);
                _stack.RemoveAll(p => p == null || p.PopupType == type);
            }
        }

        // 풀에서 꺼내거나 새로 생성
        _pool.TryGetValue(type, out var popup);
        if (popup != null)
        {
            _pool.Remove(type);
        }
        else
        {
            _pool.Remove(type); // 파괴된 항목이 있으면 제거
            var go = Instantiate(prefab.gameObject, _popupRoot);
            popup = go.GetComponent<PopupBase>();
            if (popup == null)
            {
                Debug.LogWarning($"[PopupManager] {prefab.name} 에 PopupBase 컴포넌트가 없습니다.");
                Destroy(go);
                return null;
            }
        }

        // 등록
        _openByType[type] = popup;
        _stack.Add(popup);

        // 블로커를 가장 아래 팝업 바로 아래에 배치
        UpdateBlocker();

        popup.OpenInternal(HandlePopupClosed, onClose);
        return popup;
    }

    // ── 내부 — 팝업 닫힘 콜백 ────────────────────────────────

    void HandlePopupClosed(PopupBase popup)
    {
        _openByType.Remove(popup.PopupType);
        _stack.Remove(popup);

        popup.gameObject.SetActive(false);
        _pool[popup.PopupType] = popup;

        UpdateBlocker();
    }

    // ── 내부 — 블로커 ─────────────────────────────────────────

    void UpdateBlocker()
    {
        if (_stack.Count == 0)
        {
            if (_blocker != null) _blocker.SetActive(false);
            return;
        }

        if (_blocker == null) _blocker = CreateBlocker();
        _blocker.SetActive(true);

        // 스택 첫 번째(가장 아래) 팝업 바로 아래에 배치
        int idx = _stack[0].transform.GetSiblingIndex();
        _blocker.transform.SetSiblingIndex(Mathf.Max(0, idx - 1));
    }

    GameObject CreateBlocker()
    {
        var go = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_popupRoot, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color         = _blockerColor;
        img.raycastTarget = true;

        return go;
    }

    // ── 내부 — 루트 보장 ─────────────────────────────────────

    void EnsurePopupRoot()
    {
        if (_popupRoot != null) return;
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) { _popupRoot = canvas.transform; return; }
        _popupRoot = new GameObject("PopupRoot").transform;
        Debug.LogWarning("[PopupManager] Canvas 를 찾지 못해 PopupRoot 를 임시 생성했습니다.");
    }
}
