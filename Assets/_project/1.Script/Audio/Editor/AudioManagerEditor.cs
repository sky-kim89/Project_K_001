using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(12);
        GUI.backgroundColor = new Color(0.5f, 0.9f, 0.5f);

        if (GUILayout.Button("Load SFX Clips From Folder", GUILayout.Height(32)))
            LoadClips((AudioManager)target);

        GUI.backgroundColor = Color.white;
    }

    static void LoadClips(AudioManager manager)
    {
        const string folderPath = "Assets/_project/5.Audio/SFX";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[AudioManagerEditor] 폴더가 없습니다: {folderPath}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
        var      found = new System.Collections.Generic.List<AudioClip>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;

            // 파일명이 SfxKey 값이어야 AudioManager 가 분류할 수 있다.
            // 안 맞는 파일을 배열에 넣어 두면 런타임에 경고만 쌓이므로 여기서 거른다.
            if (!System.Enum.TryParse(clip.name, out SfxKey key) || key == SfxKey.None)
            {
                Debug.LogWarning($"[AudioManagerEditor] '{clip.name}' 은 SfxKey 에 없는 이름이라 건너뜁니다.");
                continue;
            }
            found.Add(clip);
        }

        var so   = new SerializedObject(manager);
        var prop = so.FindProperty("_clips");

        prop.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = found[i];

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();

        // 연결이 빠진 키를 바로 알려 준다 — 무음으로 남으면 원인을 찾기 어렵다.
        foreach (SfxKey key in System.Enum.GetValues(typeof(SfxKey)))
        {
            if (key == SfxKey.None) continue;
            if (!found.Exists(c => c.name == key.ToString()))
                Debug.LogWarning($"[AudioManagerEditor] SfxKey.{key} 에 해당하는 클립이 없습니다 — 무음으로 남습니다.");
        }

        Debug.Log($"[AudioManagerEditor] {found.Count}개 효과음 로드 완료 ({folderPath})");
    }
}
