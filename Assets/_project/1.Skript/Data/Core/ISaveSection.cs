// ============================================================
//  ISaveSection.cs
//  저장 가능한 데이터 섹션 인터페이스.
//
//  UserDataManager 에 등록된 모든 섹션은 이 인터페이스를 구현한다.
//  SaveAll() 호출 시 등록된 섹션들이 순서대로 직렬화·저장된다.
//
//  새 데이터 섹션 추가 방법:
//  1. ISaveSection 을 구현하는 클래스 생성
//  2. UserDataManager.RegisterSection() 으로 등록
// ============================================================

// ── 저장 키 ──────────────────────────────────────────────────
// 새 섹션 추가 시 여기에 값을 추가한다.
public enum SaveKey
{
    UserData = 0,
    UnitData = 1,
    // 추후: InventoryData = 2, QuestData = 3, ...
}

public interface ISaveSection
{
    /// <summary>이 섹션의 저장 키.</summary>
    SaveKey SaveKey { get; }

    /// <summary>데이터를 JSON 문자열로 직렬화해 반환.</summary>
    string Serialize();

    /// <summary>JSON 문자열로부터 데이터를 복원.</summary>
    void Deserialize(string json);

    /// <summary>데이터가 없을 때 기본값으로 초기화.</summary>
    void SetDefaults();
}
