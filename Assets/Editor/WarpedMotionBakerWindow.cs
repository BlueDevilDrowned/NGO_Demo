using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class WarpedMotionBakerWindow : EditorWindow
{
    private const string PreferenceName = "WarpedMotion";

    private enum SampleRateMode
    {
        FromClip,
        Fps60,
        Fps120
    }

    [SerializeField] private WarpedMotionData _targetData;
    [SerializeField] private GameObject _samplePrefab;
    [SerializeField] private UnityEngine.Object _animationSource;
    [SerializeField] private SampleRateMode _sampleRateMode = SampleRateMode.Fps60;
    [SerializeField] private Vector2 _scroll;

    [MenuItem("Tools/NGO/Warped Motion Baker")]
    private static void Open()
    {
        GetWindow<WarpedMotionBakerWindow>("Warped Motion Baker");
    }

    private void OnEnable()
    {
        LoadPreferences();
    }

    private void OnDisable()
    {
        SavePreferences();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Warped Motion Baker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bakes local XYZ velocity, yaw velocity, warp segments, end foot phase, and hand IK weight. " +
            "Vault finds the highest Y point; Dodge finds the farthest horizontal point; Simple uses the end point.",
            MessageType.Info);

        WarpedMotionData previousTarget = _targetData;
        EditorGUI.BeginChangeCheck();
        _targetData = (WarpedMotionData)EditorGUILayout.ObjectField(
            "Target Data",
            _targetData,
            typeof(WarpedMotionData),
            false);
        _samplePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Sample Prefab",
            _samplePrefab,
            typeof(GameObject),
            false);
        _sampleRateMode = (SampleRateMode)EditorGUILayout.EnumPopup("Sample Rate", _sampleRateMode);
        bool windowSettingsChanged = EditorGUI.EndChangeCheck();

        if (_targetData != previousTarget)
            _animationSource = _targetData != null ? _targetData.Clip : null;

        if (windowSettingsChanged)
            SavePreferences();

        if (GUILayout.Button("Create Data Asset"))
            CreateDataAsset();

        if (_targetData == null)
            return;

        EditorGUILayout.Space();
        DrawDataSettings();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(_samplePrefab == null || _targetData.Clip == null))
        {
            if (GUILayout.Button("Bake Warped Motion", GUILayout.Height(34f)))
                Bake();
        }

        DrawBakeStatus();
    }

    private void DrawDataSettings()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(250f));
        DrawAnimationSource();

        var serializedData = new SerializedObject(_targetData);
        serializedData.Update();
        EditorGUILayout.PropertyField(serializedData.FindProperty("_type"));
        EditorGUILayout.PropertyField(serializedData.FindProperty("_warpPoints"), true);
        EditorGUILayout.PropertyField(serializedData.FindProperty("_handIKWeightCurve"));
        EditorGUILayout.EndScrollView();

        if (serializedData.ApplyModifiedProperties())
        {
            _targetData.ClearBakedData();
            EditorUtility.SetDirty(_targetData);
        }
    }

    private void DrawAnimationSource()
    {
        UnityEngine.Object displayedSource = _animationSource != null
            ? _animationSource
            : _targetData.Clip;
        UnityEngine.Object newSource = EditorGUILayout.ObjectField(
            new GUIContent(
                "Animation Source",
                "Accepts an AnimationClip or a single-clip Animancer TransitionAsset."),
            displayedSource,
            typeof(UnityEngine.Object),
            false);

        if (displayedSource != null && displayedSource is not AnimationClip)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Resolved Clip", _targetData.Clip, typeof(AnimationClip), false);
        }

        if (newSource == displayedSource)
            return;

        if (!AnimationBakerEditorUtility.TryResolveClip(newSource, out AnimationClip clip, out string error))
        {
            EditorUtility.DisplayDialog("Invalid animation source", error, "OK");
            return;
        }

        Undo.RecordObject(_targetData, "Assign Warped Motion Animation Source");
        var serializedData = new SerializedObject(_targetData);
        serializedData.FindProperty("_clip").objectReferenceValue = clip;
        serializedData.ApplyModifiedPropertiesWithoutUndo();
        _targetData.ClearBakedData();
        EditorUtility.SetDirty(_targetData);
        _animationSource = newSource;
        SavePreferences();
    }

    private void DrawBakeStatus()
    {
        if (!_targetData.IsBaked)
        {
            EditorGUILayout.HelpBox("Not baked", MessageType.None);
            return;
        }

        string sampleRate = _targetData.BakedSampleRate > 0
            ? $"{_targetData.BakedSampleRate} FPS"
            : "source clip FPS";
        EditorGUILayout.HelpBox(
            $"Baked {_targetData.BakedDuration:F3}s at {sampleRate}\n" +
            $"Offset {_targetData.TotalBakedLocalOffset:F3} | Yaw {_targetData.TotalBakedYaw:F1} | " +
            $"{_targetData.EndFootPhase}\n" +
            $"Warp points: {_targetData.WarpPoints.Count} | {_targetData.BakedAtUtc} UTC",
            MessageType.None);
    }

    private void CreateDataAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Warped Motion Data",
            "WarpedMotionData",
            "asset",
            "Choose where to save the baked data asset.");
        if (string.IsNullOrEmpty(path))
            return;

        var data = CreateInstance<WarpedMotionData>();
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        _targetData = data;
        _animationSource = null;
        Selection.activeObject = data;
        SavePreferences();
    }

    private void LoadPreferences()
    {
        _targetData = AnimationBakerEditorUtility.LoadObject<WarpedMotionData>(PreferenceKey("TargetData"));
        _samplePrefab = AnimationBakerEditorUtility.LoadObject<GameObject>(PreferenceKey("SamplePrefab"));
        _animationSource = AnimationBakerEditorUtility.LoadObject<UnityEngine.Object>(PreferenceKey("AnimationSource"));
        _sampleRateMode = (SampleRateMode)Mathf.Clamp(
            EditorPrefs.GetInt(PreferenceKey("SampleRate"), (int)SampleRateMode.Fps60),
            0,
            Enum.GetValues(typeof(SampleRateMode)).Length - 1);

        if (_animationSource == null && _targetData != null)
            _animationSource = _targetData.Clip;
    }

    private void SavePreferences()
    {
        AnimationBakerEditorUtility.SaveObject(PreferenceKey("TargetData"), _targetData);
        AnimationBakerEditorUtility.SaveObject(PreferenceKey("SamplePrefab"), _samplePrefab);
        AnimationBakerEditorUtility.SaveObject(PreferenceKey("AnimationSource"), _animationSource);
        EditorPrefs.SetInt(PreferenceKey("SampleRate"), (int)_sampleRateMode);
    }

    private static string PreferenceKey(string settingName)
    {
        return AnimationBakerEditorUtility.GetProjectPreferenceKey(PreferenceName, settingName);
    }

    private void Bake()
    {
        if (!ValidateSetup())
            return;

        Undo.RecordObject(_targetData, "Bake Warped Motion");

        try
        {
            EditorUtility.DisplayProgressBar(
                "Warped Motion Bake",
                $"Sampling {_targetData.Clip.name}",
                0.25f);
            BakeClip();
            EditorUtility.SetDirty(_targetData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Warped bake complete",
                $"Baked {_targetData.Clip.name} with {_targetData.WarpPoints.Count} warp point(s).",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Bake failed", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private bool ValidateSetup()
    {
        if (_targetData == null || _targetData.Clip == null)
        {
            EditorUtility.DisplayDialog("Missing data", "Assign a data asset and animation clip.", "OK");
            return false;
        }

        if (_samplePrefab == null)
        {
            EditorUtility.DisplayDialog("Missing prefab", "Assign a sample character prefab.", "OK");
            return false;
        }

        if (_samplePrefab.GetComponentInChildren<Animator>(true) == null)
        {
            EditorUtility.DisplayDialog("Missing Animator", "The sample prefab needs an Animator.", "OK");
            return false;
        }

        return true;
    }

    private void BakeClip()
    {
        GameObject instance = null;
        PlayableGraph graph = default;

        try
        {
            instance = PrefabUtility.IsPartOfPrefabAsset(_samplePrefab)
                ? (GameObject)PrefabUtility.InstantiatePrefab(_samplePrefab)
                : Instantiate(_samplePrefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            Animator animator = instance.GetComponentInChildren<Animator>(true);
            if (animator == null)
                throw new InvalidOperationException("The instantiated sample prefab has no Animator.");

            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
            animator.Rebind();

            graph = PlayableGraph.Create($"WarpedMotionBake_{_targetData.Clip.name}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, _targetData.Clip);
            clipPlayable.SetApplyFootIK(false);
            clipPlayable.SetApplyPlayableIK(false);
            output.SetSourcePlayable(clipPlayable);
            graph.Play();
            graph.Evaluate(0f);

            int sampleRate = ResolveSampleRate(_targetData.Clip);
            float duration = Mathf.Max(_targetData.Clip.length, 0.001f);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
            float deltaTime = duration / sampleCount;

            var velocityX = new AnimationCurve();
            var velocityY = new AnimationCurve();
            var velocityZ = new AnimationCurve();
            var angularVelocityY = new AnimationCurve();
            var cumulativeOffsets = new Vector3[sampleCount + 1];
            var cumulativeYaws = new float[sampleCount + 1];

            Quaternion startRotation = animator.transform.rotation;
            Vector3 totalWorldOffset = Vector3.zero;
            float totalYaw = 0f;

            for (int i = 1; i <= sampleCount; i++)
            {
                Quaternion previousRotation = animator.transform.rotation;
                graph.Evaluate(deltaTime);

                Vector3 worldDelta = animator.deltaPosition;
                Quaternion deltaRotation = animator.deltaRotation;
                Vector3 localDelta = Quaternion.Inverse(previousRotation) * worldDelta;
                Vector3 localVelocity = localDelta / deltaTime;
                float yawDelta = ExtractSignedYaw(deltaRotation);
                float yawVelocity = yawDelta / deltaTime;
                float normalizedTime = (float)i / sampleCount;

                if (i == 1)
                {
                    AddVelocityKeys(
                        0f,
                        localVelocity,
                        yawVelocity,
                        velocityX,
                        velocityY,
                        velocityZ,
                        angularVelocityY);
                }

                AddVelocityKeys(
                    normalizedTime,
                    localVelocity,
                    yawVelocity,
                    velocityX,
                    velocityY,
                    velocityZ,
                    angularVelocityY);

                totalWorldOffset += worldDelta;
                totalYaw += yawDelta;
                cumulativeOffsets[i] = Quaternion.Inverse(startRotation) * totalWorldOffset;
                cumulativeYaws[i] = totalYaw;
            }

            SetLinearTangents(velocityX);
            SetLinearTangents(velocityY);
            SetLinearTangents(velocityZ);
            SetLinearTangents(angularVelocityY);

            List<WarpedMotionPoint> points = BuildWarpPoints(
                _targetData.Type,
                _targetData.WarpPoints,
                cumulativeOffsets,
                cumulativeYaws,
                sampleCount);

            _targetData.SetBakedData(
                points,
                duration,
                velocityX,
                velocityY,
                velocityZ,
                angularVelocityY,
                cumulativeOffsets[sampleCount],
                totalYaw,
                DetectEndFootPhase(animator),
                sampleRate,
                DateTime.UtcNow.ToString("O"));
        }
        finally
        {
            if (graph.IsValid())
                graph.Destroy();

            if (instance != null)
                DestroyImmediate(instance);
        }
    }

    private static List<WarpedMotionPoint> BuildWarpPoints(
        WarpedMotionType type,
        IReadOnlyList<WarpedMotionPoint> configuredPoints,
        Vector3[] cumulativeOffsets,
        float[] cumulativeYaws,
        int sampleCount)
    {
        var points = new List<WarpedMotionPoint>();

        switch (type)
        {
            case WarpedMotionType.Vault:
                int apexIndex = 0;
                for (int i = 1; i <= sampleCount; i++)
                {
                    if (cumulativeOffsets[i].y > cumulativeOffsets[apexIndex].y)
                        apexIndex = i;
                }

                points.Add(CreatePoint("Apex", (float)apexIndex / sampleCount));
                break;

            case WarpedMotionType.Dodge:
                int farthestIndex = 0;
                float farthestSqrDistance = 0f;
                for (int i = 1; i <= sampleCount; i++)
                {
                    Vector3 offset = cumulativeOffsets[i];
                    float sqrDistance = offset.x * offset.x + offset.z * offset.z;
                    if (sqrDistance > farthestSqrDistance)
                    {
                        farthestSqrDistance = sqrDistance;
                        farthestIndex = i;
                    }
                }

                points.Add(CreatePoint("MaxDodge", (float)farthestIndex / sampleCount));
                break;

            case WarpedMotionType.Simple:
                points.Add(CreatePoint("End", 1f));
                break;

            default:
                CopyConfiguredPoints(configuredPoints, points);
                break;
        }

        bool shouldEnsureEnd = type != WarpedMotionType.Custom;
        if (shouldEnsureEnd && !HasEndPoint(points))
            points.Add(CreatePoint("End", 1f));

        points.Sort((left, right) => left.NormalizedTime.CompareTo(right.NormalizedTime));
        BakePointSegments(points, cumulativeOffsets, cumulativeYaws, sampleCount);
        return points;
    }

    private static void CopyConfiguredPoints(
        IReadOnlyList<WarpedMotionPoint> source,
        List<WarpedMotionPoint> destination)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            WarpedMotionPoint point = source[i];
            if (point == null)
                continue;

            destination.Add(new WarpedMotionPoint
            {
                PointName = string.IsNullOrWhiteSpace(point.PointName) ? $"Point {i + 1}" : point.PointName,
                NormalizedTime = Mathf.Clamp01(point.NormalizedTime),
                TargetPositionOffset = point.TargetPositionOffset
            });
        }
    }

    private static void BakePointSegments(
        List<WarpedMotionPoint> points,
        Vector3[] cumulativeOffsets,
        float[] cumulativeYaws,
        int sampleCount)
    {
        Vector3 previousOffset = Vector3.zero;
        float previousYaw = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            WarpedMotionPoint point = points[i];
            int frameIndex = Mathf.Clamp(
                Mathf.RoundToInt(point.NormalizedTime * sampleCount),
                0,
                sampleCount);

            Vector3 cumulativeOffset = cumulativeOffsets[frameIndex];
            float cumulativeYaw = cumulativeYaws[frameIndex];
            Quaternion segmentStartRotation = Quaternion.Euler(0f, previousYaw, 0f);
            point.BakedLocalOffset =
                Quaternion.Inverse(segmentStartRotation) * (cumulativeOffset - previousOffset);
            point.BakedLocalRotation = Quaternion.Euler(0f, cumulativeYaw - previousYaw, 0f);
            previousOffset = cumulativeOffset;
            previousYaw = cumulativeYaw;
        }
    }

    private static WarpedMotionPoint CreatePoint(string name, float normalizedTime)
    {
        return new WarpedMotionPoint
        {
            PointName = name,
            NormalizedTime = Mathf.Clamp01(normalizedTime)
        };
    }

    private static bool HasEndPoint(List<WarpedMotionPoint> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null && points[i].NormalizedTime >= 0.98f)
                return true;
        }

        return false;
    }

    private static void AddVelocityKeys(
        float normalizedTime,
        Vector3 localVelocity,
        float yawVelocity,
        AnimationCurve velocityX,
        AnimationCurve velocityY,
        AnimationCurve velocityZ,
        AnimationCurve angularVelocityY)
    {
        velocityX.AddKey(normalizedTime, localVelocity.x);
        velocityY.AddKey(normalizedTime, localVelocity.y);
        velocityZ.AddKey(normalizedTime, localVelocity.z);
        angularVelocityY.AddKey(normalizedTime, yawVelocity);
    }

    private int ResolveSampleRate(AnimationClip clip)
    {
        return _sampleRateMode switch
        {
            SampleRateMode.Fps60 => 60,
            SampleRateMode.Fps120 => 120,
            _ => clip != null && clip.frameRate > 0f ? Mathf.RoundToInt(clip.frameRate) : 30
        };
    }

    private static float ExtractSignedYaw(Quaternion deltaRotation)
    {
        Vector3 forward = deltaRotation * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.000001f)
            return 0f;

        return Vector3.SignedAngle(Vector3.forward, forward.normalized, Vector3.up);
    }

    private static BakedFootPhase DetectEndFootPhase(Animator animator)
    {
        if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid)
            return BakedFootPhase.Unknown;

        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (left == null || right == null)
            return BakedFootPhase.Unknown;

        float leftHeight = animator.transform.InverseTransformPoint(left.position).y;
        float rightHeight = animator.transform.InverseTransformPoint(right.position).y;
        return leftHeight <= rightHeight
            ? BakedFootPhase.LeftFootDown
            : BakedFootPhase.RightFootDown;
    }

    private static void SetLinearTangents(AnimationCurve curve)
    {
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyBroken(curve, i, true);
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }
}
