using UnityEngine;
using UnityEngine.U2D;

// ============================================================
//  SpriteManager.cs
//  게임 전역 스프라이트 조회 ScriptableObject 싱글턴.
//
//  초기화:
//    Assets/Resources/SpriteManager.asset 에 배치.
//    씬 로드 전 자동 로드.
//
//  사용법:
//    Sprite icon = SpriteManager.Instance.Get("item_gold");
//
//  아틀라스 → 폴더 매핑:
//    _itemAtlas       ← Icons/Items/
//    _generalAtlas    ← Icons/Classes/ + Icons/Skills/
//    _equipmentAtlas  ← Icons/Equipments/
//    _abilityAtlas    ← Icons/Abilities/
//    _traitAtlas      ← Icons/Traits/
//    _relicAtlas      ← Icons/Relics/
//    _stageNodeAtlas  ← Icons/StageNodes/
//    _lobbyBtnAtlas   ← Icons/LobbyBtns/
//
//  스프라이트 이름은 PNG 파일명(확장자 제외)과 동일해야 한다.
// ============================================================

[CreateAssetMenu(fileName = "SpriteManager", menuName = "ProjectK/SpriteManager")]
public class SpriteManager : ScriptableObject
{
    public static SpriteManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoLoad() => Instance = Resources.Load<SpriteManager>("SpriteManager");

    [Header("아이템 / 재화")]
    [SerializeField] SpriteAtlas _itemAtlas;

    [Header("장군 (직업 + 스킬)")]
    [SerializeField] SpriteAtlas _generalAtlas;

    [Header("장비")]
    [SerializeField] SpriteAtlas _equipmentAtlas;

    [Header("어빌리티")]
    [SerializeField] SpriteAtlas _abilityAtlas;

    [Header("특성")]
    [SerializeField] SpriteAtlas _traitAtlas;

    [Header("유물")]
    [SerializeField] SpriteAtlas _relicAtlas;

    [Header("스테이지 노드")]
    [SerializeField] SpriteAtlas _stageNodeAtlas;

    [Header("로비 버튼")]
    [SerializeField] SpriteAtlas _lobbyBtnAtlas;

    // 전체 아틀라스를 순서대로 검색. 없으면 null.
    public Sprite Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        Sprite s;
        if (_itemAtlas      != null && (s = _itemAtlas.GetSprite(name))      != null) return s;
        if (_generalAtlas   != null && (s = _generalAtlas.GetSprite(name))   != null) return s;
        if (_equipmentAtlas != null && (s = _equipmentAtlas.GetSprite(name)) != null) return s;
        if (_abilityAtlas   != null && (s = _abilityAtlas.GetSprite(name))   != null) return s;
        if (_traitAtlas     != null && (s = _traitAtlas.GetSprite(name))     != null) return s;
        if (_relicAtlas     != null && (s = _relicAtlas.GetSprite(name))     != null) return s;
        if (_stageNodeAtlas != null && (s = _stageNodeAtlas.GetSprite(name)) != null) return s;
        if (_lobbyBtnAtlas  != null && (s = _lobbyBtnAtlas.GetSprite(name))  != null) return s;

        return null;
    }
}
