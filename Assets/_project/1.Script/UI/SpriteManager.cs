using UnityEngine;
using UnityEngine.U2D;

// ============================================================
//  SpriteManager.cs
//  게임 전역 스프라이트 조회 ScriptableObject 싱글턴.
//
//  초기화:
//    Assets/Resources/SpriteManager.asset 에 배치하면
//    씬 로드 전에 자동으로 로드된다.
//
//  사용법:
//    Sprite icon = SpriteManager.Instance.Get("item_gold");
//
//  아틀라스 구성:
//    _itemAtlas    — 재화·아이템 아이콘  (item_*.png)
//    _generalAtlas — 직업·스킬 아이콘   (knight_icon.png / skill_*.png)
//
//  스프라이트 이름은 PNG 파일명(확장자 제외)과 동일해야 한다.
// ============================================================

[CreateAssetMenu(fileName = "SpriteManager", menuName = "ProjectK/SpriteManager")]
public class SpriteManager : ScriptableObject
{
    public static SpriteManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoLoad() => Instance = Resources.Load<SpriteManager>("SpriteManager");

    [Header("아이템 / 재화 아이콘")]
    [SerializeField] SpriteAtlas _itemAtlas;

    [Header("장군 관련 (직업 + 스킬) 아이콘")]
    [SerializeField] SpriteAtlas _generalAtlas;

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>
    /// 스프라이트 이름으로 조회. 아이템 → 장군 순서로 탐색.
    /// 없으면 null 반환.
    /// </summary>
    public Sprite Get(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        Sprite s;
        if (_itemAtlas    != null && (s = _itemAtlas.GetSprite(spriteName))    != null) return s;
        if (_generalAtlas != null && (s = _generalAtlas.GetSprite(spriteName)) != null) return s;

        return null;
    }

    public Sprite GetItem   (string name) => _itemAtlas?.GetSprite(name);
    public Sprite GetGeneral(string name) => _generalAtlas?.GetSprite(name);
}
