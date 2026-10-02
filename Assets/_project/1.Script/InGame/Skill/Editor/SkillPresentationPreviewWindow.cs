using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ============================================================
//  SkillPresentationPreviewWindow.cs  [Editor Only]
//
//  스킬 SO 에 연결된 이펙트와 SFX 를 한 창에서 비교한다.
//  실제 SkillSfxDelay 를 적용해 예고 → 착탄음 순서도 그대로 확인한다.
//  전투 실행이나 풀 초기화 없이 PreviewRenderUtility 안에서만 재생하므로
//  현재 씬은 건드리지 않는다.
// ============================================================

public sealed class SkillPresentationPreviewWindow : EditorWindow
{
    const string DatabasePath = "Assets/Resources/ActiveSkillDatabase.asset";
    const string EffectFolder = "Assets/_project/2.Prefabs/Effect";
    const string SfxFolder = "Assets/_project/5.Audio/SFX";

    enum TierFilter { All, Normal, Rare, Boss }

    sealed class PreviewEffect
    {
        public GameObject Root;
        public double StartedAt;
        public ParticleSystem[] Particles;
    }

    readonly List<ActiveSkillData> _skills = new();
    readonly List<PreviewEffect> _effects = new();
    readonly HashSet<string> _spawnedKeys = new();

    PreviewRenderUtility _preview;
    Vector2 _listScroll;
    Vector2 _detailScroll;
    string _search = "";
    TierFilter _filter;
    ActiveSkillData _selected;

    bool _playing;
    bool _impactSpawned;
    bool _loop;
    double _startedAt;
    float _previewDuration;
    float _previewScale = 1f;
    string _status = "스킬을 선택한 뒤 함께 재생을 누르세요.";

    [MenuItem(ProjectKMenu.Tool + "스킬 이펙트·사운드 미리보기",
              priority = ProjectKMenu.ToolPrio + 30)]
    static void Open()
    {
        var window = GetWindow<SkillPresentationPreviewWindow>();
        window.titleContent = new GUIContent("스킬 연출 테스트");
        window.minSize = new Vector2(900f, 620f);
        window.Show();
    }

    void OnEnable()
    {
        CreatePreview();
        LoadSkills();
        EditorApplication.update += Tick;
    }

    void OnDisable()
    {
        EditorApplication.update -= Tick;
        StopPreview();
        _preview.Cleanup();
        _preview = null;
    }

    void CreatePreview()
    {
        _preview = new PreviewRenderUtility();
        _preview.camera.cameraType = CameraType.Preview;
        _preview.camera.orthographic = true;
        _preview.camera.orthographicSize = 4.2f;
        _preview.camera.transform.position = new Vector3(0f, 0f, -10f);
        _preview.camera.transform.rotation = Quaternion.identity;
        _preview.camera.nearClipPlane = 0.01f;
        _preview.camera.farClipPlane = 100f;
        _preview.camera.clearFlags = CameraClearFlags.SolidColor;
        _preview.camera.backgroundColor = new Color(0.045f, 0.05f, 0.075f, 1f);
        _preview.ambientColor = Color.white;
    }

    void LoadSkills()
    {
        var database = AssetDatabase.LoadAssetAtPath<ActiveSkillDatabase>(DatabasePath);
        if (database == null)
            throw new InvalidOperationException($"ActiveSkillDatabase 가 없습니다: {DatabasePath}");

        _skills.Clear();
        foreach (var skill in database.Entries)
            if (skill != null) _skills.Add(skill);

        _skills.Sort((a, b) => ((int)a.SkillId).CompareTo((int)b.SkillId));
        if (_selected == null && _skills.Count > 0) _selected = _skills[0];
    }

    void OnGUI()
    {
        DrawTopBar();
        EditorGUILayout.Space(4f);

        EditorGUILayout.BeginHorizontal();
        DrawSkillList();
        DrawDetails();
        EditorGUILayout.EndHorizontal();
    }

    void DrawTopBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("검색", GUILayout.Width(30f));
        _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField,
                                      GUILayout.Width(210f));
        GUILayout.Space(8f);
        _filter = (TierFilter)EditorGUILayout.EnumPopup(_filter, EditorStyles.toolbarPopup,
                                                        GUILayout.Width(82f));
        GUILayout.FlexibleSpace();
        _loop = GUILayout.Toggle(_loop, "반복", EditorStyles.toolbarButton,
                                 GUILayout.Width(52f));
        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            LoadSkills();
        EditorGUILayout.EndHorizontal();
    }

    void DrawSkillList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(270f));
        EditorGUILayout.LabelField($"액티브 스킬 ({VisibleCount()}/{_skills.Count})",
                                   EditorStyles.boldLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUI.skin.box);
        foreach (var skill in _skills)
        {
            if (!IsVisible(skill)) continue;

            bool selected = skill == _selected;
            var old = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.38f, 0.7f, 1f) : TierColor(skill);

            string state = HasAudio(skill) && HasAnyEffect(skill) ? "●" : "!";
            if (GUILayout.Button($"{state}  {(int)skill.SkillId:00}  {skill.SkillName}",
                                 selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton,
                                 GUILayout.Height(25f)))
                SelectSkill(skill);

            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox("● 연결 정상  /  ! 음원 또는 이펙트 누락",
                                MessageType.None);
        EditorGUILayout.EndVertical();
    }

    void DrawDetails()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (_selected == null)
        {
            EditorGUILayout.HelpBox("표시할 스킬이 없습니다.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
        DrawSkillHeader(_selected);
        DrawTransport(_selected);
        DrawPreviewArea(_selected);
        DrawMapping(_selected);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawSkillHeader(ActiveSkillData skill)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{skill.SkillName}  [{skill.SkillId}]",
                                   EditorStyles.largeLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("SO 선택", GUILayout.Width(70f)))
        {
            Selection.activeObject = skill;
            EditorGUIUtility.PingObject(skill);
        }
        EditorGUILayout.EndHorizontal();

        string tier = skill.SkillId.IsBossPattern() ? "보스" : skill.IsRare ? "희귀" : "일반";
        EditorGUILayout.LabelField($"{tier} · 쿨다운 {skill.Cooldown:0.##}초 · " +
                                   $"반경 {skill.EffectRadius:0.##}", EditorStyles.miniLabel);
        EditorGUILayout.HelpBox(skill.Description, MessageType.None);
    }

    void DrawTransport(ActiveSkillData skill)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = HasAudio(skill);
        if (GUILayout.Button("▶ 사운드만", GUILayout.Height(34f))) PlayAudioOnly(skill);

        GUI.enabled = HasAnyEffect(skill);
        if (GUILayout.Button("▶ 이펙트만", GUILayout.Height(34f))) PlayEffectsOnly(skill);

        GUI.enabled = HasAudio(skill) && HasAnyEffect(skill);
        var old = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.42f, 0.88f, 0.58f);
        if (GUILayout.Button("▶ 이펙트 + 사운드", GUILayout.Height(34f))) PlayCombined(skill);
        GUI.backgroundColor = old;

        GUI.enabled = true;
        if (GUILayout.Button("■ 정지", GUILayout.Width(72f), GUILayout.Height(34f))) StopPreview();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(_status, EditorStyles.centeredGreyMiniLabel);
    }

    void DrawPreviewArea(ActiveSkillData skill)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("미리보기 배율", GUILayout.Width(88f));
        float nextScale = EditorGUILayout.Slider(_previewScale, 0.35f, 2.5f);
        if (!Mathf.Approximately(nextScale, _previewScale))
        {
            _previewScale = nextScale;
            foreach (var effect in _effects)
                effect.Root.transform.localScale = Vector3.one * _previewScale;
        }
        EditorGUILayout.EndHorizontal();

        Rect rect = GUILayoutUtility.GetRect(300f, 10000f, 260f, 420f,
                                              GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
        {
            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.camera.Render();
            Texture texture = _preview.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        GUI.Box(rect, GUIContent.none);
        GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f),
                  $"{skill.SkillName}  ·  SFX 지연 {skill.SkillSfxDelay:0.##}초",
                  EditorStyles.whiteMiniLabel);
    }

    void DrawMapping(ActiveSkillData skill)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("연결 상태", EditorStyles.boldLabel);

        AudioClip castClip = FindCastAudio(skill);
        if (castClip != null)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("선행 SFX", castClip, typeof(AudioClip), false);
            EditorGUILayout.LabelField("선행음 재생 시점", "시전 즉시", EditorStyles.miniLabel);
        }

        AudioClip clip = FindAudio(skill);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField(castClip == null ? "SFX" : "착탄 SFX", clip, typeof(AudioClip), false);

        if (clip == null)
            EditorGUILayout.HelpBox($"{skill.SkillSfx} 파일을 찾을 수 없습니다.", MessageType.Error);
        else
            EditorGUILayout.LabelField("재생 시점",
                skill.SkillSfxDelay <= 0f ? "시전 즉시" : $"시전 후 {skill.SkillSfxDelay:0.##}초",
                EditorStyles.miniLabel);

        DrawEffectRow("시전자", skill.CasterEffectKey);
        DrawEffectRow("피격", skill.TargetEffectKey);
        DrawEffectRow("기본/범위", skill.BaseEffectKey);
    }

    void DrawEffectRow(string label, string key)
    {
        GameObject prefab = FindEffect(key);
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField(label, prefab, typeof(GameObject), false);

        GUI.enabled = prefab != null;
        if (GUILayout.Button("▶", GUILayout.Width(34f))) PlaySingleEffect(key);
        if (GUILayout.Button("찾기", GUILayout.Width(44f)))
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(key) && prefab == null)
            EditorGUILayout.HelpBox($"{label} 이펙트 누락: {key}", MessageType.Error);
    }

    void SelectSkill(ActiveSkillData skill)
    {
        StopPreview();
        _selected = skill;
        _detailScroll = Vector2.zero;
        _status = "사운드만 또는 함께 재생으로 매칭을 비교하세요.";
        Repaint();
    }

    void PlayAudioOnly(ActiveSkillData skill)
    {
        BeginSequence(skill, true, false);
        _status = $"{skill.SkillName}: 실제 타이밍으로 사운드만 재생 중";
    }

    void PlayEffectsOnly(ActiveSkillData skill)
    {
        BeginSequence(skill, false, true);
        _status = $"{skill.SkillName}: 이펙트만 재생 중";
    }

    void PlayCombined(ActiveSkillData skill)
    {
        BeginSequence(skill, true, true);
        _status = skill.SkillSfxDelay <= 0f
            ? $"{skill.SkillName}: 이펙트와 사운드 동시 재생"
            : $"{skill.SkillName}: 선행음 → {skill.SkillSfxDelay:0.##}초 뒤 착탄음";
    }

    void PlaySingleEffect(string key)
    {
        StopPreview();
        _startedAt = EditorApplication.timeSinceStartup;
        SpawnEffect(key, _startedAt);
        _previewDuration = 3f;
        _playing = true;
        _impactSpawned = true;
        _status = $"{key}: 이펙트만 재생 중";
    }

    void BeginSequence(ActiveSkillData skill, bool withAudio, bool withEffects)
    {
        StopPreview();
        _startedAt = EditorApplication.timeSinceStartup;
        _playing = true;
        _impactSpawned = skill.SkillSfxDelay <= 0f;
        _playWithAudio = withAudio;
        _playWithEffects = withEffects;

        AudioClip castClip = FindCastAudio(skill);
        if (withAudio && castClip != null) AudioPreview.Play(castClip);

        if (skill.SkillSfxDelay <= 0f)
        {
            if (withEffects) SpawnAllEffects(skill, _startedAt);
            if (withAudio) AudioPreview.Play(FindAudio(skill));
        }
        else if (withEffects)
        {
            SpawnCastEffects(skill, _startedAt);
        }

        AudioClip clip = FindAudio(skill);
        float soundTail = withAudio ? clip.length : 0f;
        float castTail = withAudio && castClip != null ? castClip.length : 0f;
        float effectTail = withEffects ? skill.EffectDespawnDelay : 0f;
        _previewDuration = Mathf.Clamp(Mathf.Max(effectTail, castTail,
            skill.SkillSfxDelay + soundTail + 0.25f), 1.25f, 6f);
    }

    bool _playWithAudio;
    bool _playWithEffects;

    void Tick()
    {
        if (!_playing) return;

        double now = EditorApplication.timeSinceStartup;
        float elapsed = (float)(now - _startedAt);

        if (!_impactSpawned && elapsed >= _selected.SkillSfxDelay)
        {
            if (_playWithEffects) SpawnImpactEffects(_selected, now);
            if (_playWithAudio) AudioPreview.Play(FindAudio(_selected));
            _impactSpawned = true;
        }

        SimulateEffects(now);

        if (elapsed >= _previewDuration)
        {
            if (_loop)
            {
                ActiveSkillData skill = _selected;
                bool withAudio = _playWithAudio;
                bool withEffects = _playWithEffects;
                BeginSequence(skill, withAudio, withEffects);
            }
            else
            {
                _playing = false;
                _status = $"{_selected.SkillName}: 재생 완료";
            }
        }

        Repaint();
    }

    void SpawnCastEffects(ActiveSkillData skill, double at)
    {
        if (skill.SkillId is ActiveSkillId.Bisect or ActiveSkillId.ArrowStorm or ActiveSkillId.BloodPrice)
            SpawnEffect(skill.CasterEffectKey, at);

        if (skill.SkillId is not ActiveSkillId.Bisect and not ActiveSkillId.BloodPrice)
            SpawnEffect(skill.BaseEffectKey, at);
    }

    void SpawnImpactEffects(ActiveSkillData skill, double at)
    {
        if (skill.SkillId is not ActiveSkillId.Bisect and not ActiveSkillId.ArrowStorm and not ActiveSkillId.BloodPrice)
            SpawnEffect(skill.CasterEffectKey, at);

        SpawnEffect(skill.TargetEffectKey, at);

        if (skill.SkillId is ActiveSkillId.Bisect or ActiveSkillId.BloodPrice)
            SpawnEffect(skill.BaseEffectKey, at);
    }

    void SpawnAllEffects(ActiveSkillData skill, double at)
    {
        SpawnEffect(skill.CasterEffectKey, at);
        SpawnEffect(skill.TargetEffectKey, at);
        SpawnEffect(skill.BaseEffectKey, at);
    }

    void SpawnEffect(string key, double at)
    {
        if (string.IsNullOrEmpty(key) || !_spawnedKeys.Add(key)) return;

        GameObject prefab = FindEffect(key);
        if (prefab == null)
            throw new InvalidOperationException($"이펙트 프리팹이 없습니다: {key}");

        var root = Instantiate(prefab);
        root.name = $"Preview_{key}";
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one * _previewScale;
        root.SetActive(true);

        _preview.AddSingleGO(root);
        var particles = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particle in particles) particle.Play(true);

        _effects.Add(new PreviewEffect
        {
            Root = root,
            StartedAt = at,
            Particles = particles,
        });
    }

    void SimulateEffects(double now)
    {
        foreach (var effect in _effects)
        {
            float localTime = Mathf.Max(0f, (float)(now - effect.StartedAt));
            foreach (var particle in effect.Particles)
                particle.Simulate(localTime, true, true, false);
        }
    }

    void StopPreview()
    {
        AudioPreview.Stop();
        _playing = false;
        _impactSpawned = false;
        _playWithAudio = false;
        _playWithEffects = false;

        foreach (var effect in _effects)
            if (effect.Root != null) DestroyImmediate(effect.Root);

        _effects.Clear();
        _spawnedKeys.Clear();
        Repaint();
    }

    int VisibleCount()
    {
        int count = 0;
        foreach (var skill in _skills)
            if (IsVisible(skill)) count++;
        return count;
    }

    bool IsVisible(ActiveSkillData skill)
    {
        bool searchMatch = string.IsNullOrWhiteSpace(_search)
            || skill.SkillName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
            || skill.SkillId.ToString().IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        if (!searchMatch) return false;

        return _filter switch
        {
            TierFilter.Normal => !skill.IsRare && !skill.SkillId.IsBossPattern(),
            TierFilter.Rare   => skill.IsRare && !skill.SkillId.IsBossPattern(),
            TierFilter.Boss   => skill.SkillId.IsBossPattern(),
            _                 => true,
        };
    }

    static Color TierColor(ActiveSkillData skill)
    {
        if (skill.SkillId.IsBossPattern()) return new Color(0.72f, 0.42f, 0.42f);
        if (skill.IsRare) return new Color(0.67f, 0.48f, 0.82f);
        return Color.white;
    }

    static bool HasAudio(ActiveSkillData skill) => FindAudio(skill) != null;

    static bool HasAnyEffect(ActiveSkillData skill)
        => FindEffect(skill.CasterEffectKey) != null
        || FindEffect(skill.TargetEffectKey) != null
        || FindEffect(skill.BaseEffectKey) != null;

    static AudioClip FindAudio(ActiveSkillData skill)
    {
        if (skill.SkillSfx == SfxKey.None) return null;
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{skill.SkillSfx}.wav");
    }

    static AudioClip FindCastAudio(ActiveSkillData skill)
    {
        if (skill.CastSfx == SfxKey.None) return null;
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{skill.CastSfx}.wav");
    }

    static GameObject FindEffect(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{EffectFolder}/{key}.prefab");
    }

    static class AudioPreview
    {
        static readonly Type AudioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        public static void Play(AudioClip clip)
        {
            Stop();
            MethodInfo method = AudioUtil.GetMethod("PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

            if (method == null)
                throw new MissingMethodException("UnityEditor.AudioUtil.PlayPreviewClip 을 찾을 수 없습니다.");

            method.Invoke(null, new object[] { clip, 0, false });
        }

        public static void Stop()
        {
            MethodInfo method = AudioUtil.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);

            if (method == null)
                throw new MissingMethodException("UnityEditor.AudioUtil.StopAllPreviewClips 를 찾을 수 없습니다.");

            method.Invoke(null, null);
        }
    }
}
