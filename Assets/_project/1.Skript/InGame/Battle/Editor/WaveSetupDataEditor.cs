using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ============================================================
//  WaveSetupDataEditor.cs
//  WaveSetupData SO 의 커스텀 Inspector.
//
//  제공 버튼:
//  ┌─────────────────────────────────────────────────┐
//  │  [+ 웨이브 추가]   [마지막 웨이브 제거]          │
//  │                                                 │
//  │  Wave 1 ▼                                       │
//  │    아군  [+ 아군 항목 추가]                      │
//  │      PoolKey / UnitType / Count / Delay...       │
//  │    적군  [+ 적군 항목 추가]                      │
//  │      PoolKey / UnitType / Count / Delay...       │
//  │    골드 보상: [___]                              │
//  │  Wave 2 ▼                                       │
//  │    ...                                          │
//  └─────────────────────────────────────────────────┘
// ============================================================

[CustomEditor(typeof(WaveSetupData))]
public class WaveSetupDataEditor : Editor
{
    // 각 웨이브 섹션의 펼침 상태
    readonly List<bool> _waveFoldouts = new();

    // 버튼 스타일 (OnEnable 이후 초기화)
    GUIStyle _btnAdd;
    GUIStyle _btnRemove;
    GUIStyle _waveHeader;

    void OnEnable()
    {
        SyncFoldoutLists();
    }

    public override void OnInspectorGUI()
    {
        InitStyles();

        var data = (WaveSetupData)target;
        SyncFoldoutLists();

        serializedObject.Update();

        // ── 상단 요약 ─────────────────────────────────────────
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"총 웨이브: {data.Waves.Count}",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── 웨이브 목록 ───────────────────────────────────────
        for (int waveIndex = 0; waveIndex < data.Waves.Count; waveIndex++)
        {
            DrawWave(data, waveIndex);
            EditorGUILayout.Space(4);
        }

        // ── 웨이브 추가 / 제거 버튼 ───────────────────────────
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ 웨이브 추가", _btnAdd))
        {
            Undo.RecordObject(data, "Add Wave");
            data.Waves.Add(new WaveData());
            _waveFoldouts.Add(true);
            EditorUtility.SetDirty(data);
        }

        GUI.enabled = data.Waves.Count > 0;
        if (GUILayout.Button("마지막 웨이브 제거", _btnRemove))
        {
            Undo.RecordObject(data, "Remove Wave");
            data.Waves.RemoveAt(data.Waves.Count - 1);
            EditorUtility.SetDirty(data);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    // ── 웨이브 하나 그리기 ────────────────────────────────────

    void DrawWave(WaveSetupData data, int waveIndex)
    {
        WaveData wave = data.Waves[waveIndex];

        // 웨이브 헤더 (접기/펼치기)
        string header = $"Wave {waveIndex + 1}  " +
                        $"(적군 {TotalCount(wave.EnemyEntries)}  " +
                        $"골드 {wave.GoldReward}  " +
                        $"종족 {wave.DefaultRace})";

        _waveFoldouts[waveIndex] = EditorGUILayout.BeginFoldoutHeaderGroup(
            _waveFoldouts[waveIndex], header, _waveHeader);

        if (_waveFoldouts[waveIndex])
        {
            EditorGUI.indentLevel++;

            // 골드 보상 / 기본 종족
            Undo.RecordObject(data, "Edit Wave");
            wave.GoldReward  = EditorGUILayout.IntField("골드 보상", wave.GoldReward);
            wave.DefaultRace = (EnemyRace)EditorGUILayout.EnumPopup("기본 종족", wave.DefaultRace);

            // 기본 종족을 모든 적군 항목에 일괄 적용
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("전체 항목에 종족 적용", _btnAdd, GUILayout.Width(150)))
            {
                Undo.RecordObject(data, "Apply DefaultRace");
                foreach (var e in wave.EnemyEntries) e.EnemyRace = wave.DefaultRace;
                EditorUtility.SetDirty(data);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 적군 항목
            DrawEntryList(data, wave, wave.EnemyEntries,
                label: "적군 스폰 항목", addLabel: "+ 적군 추가", defaultType: SpawnUnitType.Enemy);

            // 웨이브 삭제 버튼
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button($"Wave {waveIndex + 1} 삭제", _btnRemove, GUILayout.Width(120)))
            {
                Undo.RecordObject(data, "Remove Wave");
                data.Waves.RemoveAt(waveIndex);
                EditorUtility.SetDirty(data);
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorUtility.SetDirty(data);
    }

    // ── SpawnEntry 목록 그리기 ────────────────────────────────

    void DrawEntryList(WaveSetupData data, WaveData wave, List<SpawnEntry> entries,
                       string label, string addLabel, SpawnUnitType defaultType)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry entry = entries[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 항목 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"[{i}]  {entry.UnitType}  ×{entry.Count}  \"{entry.Name}\"  ({entry.EnemyRace})",
                EditorStyles.miniLabel);

            if (GUILayout.Button("×", _btnRemove, GUILayout.Width(20), GUILayout.Height(18)))
            {
                Undo.RecordObject(data, "Remove SpawnEntry");
                entries.RemoveAt(i);
                EditorUtility.SetDirty(data);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            // 항목 필드
            Undo.RecordObject(data, "Edit SpawnEntry");
            entry.Name          = EditorGUILayout.TextField("Name (시드)", entry.Name);
            entry.UnitType      = (SpawnUnitType)EditorGUILayout.EnumPopup("Unit Type", entry.UnitType);
            entry.EnemyRace     = (EnemyRace)EditorGUILayout.EnumPopup("Enemy Race", entry.EnemyRace);
            entry.Count         = EditorGUILayout.IntField("Count", entry.Count);
            entry.DelayBefore   = EditorGUILayout.FloatField("Delay Before (초)", entry.DelayBefore);
            entry.DelayBetween  = EditorGUILayout.FloatField("Delay Between (초)", entry.DelayBetween);

            EditorGUILayout.EndVertical();
        }

        // 항목 추가 버튼
        if (GUILayout.Button(addLabel, _btnAdd))
        {
            Undo.RecordObject(data, "Add SpawnEntry");
            entries.Add(new SpawnEntry { UnitType = defaultType, Count = 1, DelayBetween = 0.5f, EnemyRace = wave.DefaultRace });
            EditorUtility.SetDirty(data);
        }

        EditorGUI.indentLevel--;
    }

    // ── 유틸리티 ─────────────────────────────────────────────

    static int TotalCount(List<SpawnEntry> entries)
    {
        int n = 0;
        foreach (var e in entries) n += e.Count;
        return n;
    }

    void SyncFoldoutLists()
    {
        var data = (WaveSetupData)target;
        while (_waveFoldouts.Count < data.Waves.Count) { _waveFoldouts.Add(true); }
    }

    void InitStyles()
    {
        if (_btnAdd != null) return;

        _btnAdd = new GUIStyle(GUI.skin.button)
        {
            normal  = { textColor = new Color(0.2f, 0.8f, 0.2f) },
            fontStyle = FontStyle.Bold,
        };

        _btnRemove = new GUIStyle(GUI.skin.button)
        {
            normal  = { textColor = new Color(0.9f, 0.3f, 0.3f) },
            fontStyle = FontStyle.Bold,
        };

        _waveHeader = new GUIStyle(EditorStyles.foldoutHeader)
        {
            fontStyle = FontStyle.Bold,
        };
    }
}
