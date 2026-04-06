using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using UnityEngine;

// ============================================================
//  UnitAppearanceBridge.cs
//  RuntimeBridge ↔ CharacterBuilder 를 연결하는 외형 적용 컴포넌트.
//
//  사용법:
//    유닛 프리팹에 CharacterBuilder 와 함께 부착한다.
//    RuntimeBridge.Initialize() 내에서:
//      GetComponent<UnitAppearanceBridge>()?.ApplyAlly(unitName, job, grade);
//      GetComponent<UnitAppearanceBridge>()?.ApplyEnemy(race, unitName);
//
//  주의:
//    [DefaultExecutionOrder(-100)] 로 CharacterBuilderBase.Awake()(order 0) 보다
//    먼저 실행해 RebuildOnStart = false 를 선점한다.
//    이렇게 하면 SpriteCollection / Character 가 없어도 NullReference 가 발생하지 않는다.
//
//  풀 재사용 대응:
//    Apply* 호출 시 항상 새 외형을 빌드한다.
// ============================================================

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterBuilder))]
public class UnitAppearanceBridge : MonoBehaviour
{
    CharacterBuilder _builder;

    void Awake()
    {
        _builder = GetComponent<CharacterBuilder>();
        // CharacterBuilderBase.Awake() 보다 먼저 실행돼 Rebuild() 자동 호출을 막는다.
        _builder.RebuildOnStart = false;
    }

    // ── 공개 API ─────────────────────────────────────────────

    /// <summary>아군 외형 적용 (unitName 시드 + 직업 + 등급 기반).</summary>
    public void ApplyAlly(string unitName, UnitJob job, UnitGrade grade)
    {
        EnsureBuilder();
        if (_builder == null) return;

        UnitAppearanceData data = AllyAppearanceRoller.Roll(unitName, job, grade);
        Apply(data);
    }

    /// <summary>적군 외형 적용 (종족 고정 + unitName 시드 무기).</summary>
    public void ApplyEnemy(EnemyRace race, string unitName)
    {
        EnsureBuilder();
        if (_builder == null) return;

        UnitAppearanceData data = EnemyAppearanceRoller.Roll(race, unitName);
        Apply(data);
    }

    // ── 내부 ─────────────────────────────────────────────────

    void EnsureBuilder()
    {
        if (_builder == null)
            _builder = GetComponent<CharacterBuilder>();
    }

    void Apply(UnitAppearanceData data)
    {
        _builder.Body    = data.Body;
        _builder.Head    = data.Head;
        _builder.Ears    = data.Ears;
        _builder.Eyes    = data.Eyes;
        _builder.Hair    = data.Hair;
        _builder.Armor   = data.Armor;
        _builder.Helmet  = data.Helmet;
        _builder.Mask    = data.Mask;
        _builder.Horns   = data.Horns;
        _builder.Cape    = data.Cape;
        _builder.Weapon  = data.Weapon;
        _builder.Shield  = data.Shield;
        _builder.Back    = data.Back;
        _builder.Firearm = data.Firearm;

        _builder.Rebuild();
    }
}
