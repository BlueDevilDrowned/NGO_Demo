using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class RootMotionBakerWindow : EditorWindow
{
    private enum SampleRateMode
    {
        FromClip,
        Fps60,
        Fps120
    }

    [SerializeField] private RootMotionData _targetData;
    [SerializeField] private GameObject _samplePrefab;
    [SerializeField] private SampleRateMode _sampleRateMode = SampleRateMode.Fps60;
    [SerializeField, Min(0f)] private float _rotationTolerance = 0.5f;

    [MenuItem("Tools/NGO/Root Motion Baker")]
    private static void Open()
    {
        GetWindow<RootMotionBakerWindow>("Root Motion Baker");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Single Clip Root Motion Baker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bakes one AnimationClip into normalized local XYZ velocity and yaw velocity curves, " +
            "plus total displacement, total yaw, rotation finish time, and ending foot phase.",
            MessageType.Info);

        _targetData = (RootMotionData)EditorGUILayout.ObjectField(
            "Target Data",
            _targetData,
            typeof(RootMotionData),
            false);
        _samplePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Sample Prefab",
            _samplePrefab,
            typeof(GameObject),
            false);
        _sampleRateMode = (SampleRateMode)EditorGUILayout.EnumPopup("Sample Rate", _sampleRateMode);
        _rotationTolerance = EditorGUILayout.FloatField("Rotation Tolerance", _rotationTolerance);

        if (GUILayout.Button("Create Data Asset"))
            CreateDataAsset();

        if (_targetData == null)
            return;

        DrawDataSettings();

        using (new EditorGUI.DisabledScope(_samplePrefab == null || _targetData.Clip == null))
        {
            if (GUILayout.Button("Bake Animation Clip", GUILayout.Height(34f)))
                Bake();
        }

        DrawStatus();
    }

    private void DrawDataSettings()
    {
        var serializedData = new SerializedObject(_targetData);
        serializedData.Update();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedData.FindProperty("_clip"));
        EditorGUILayout.PropertyField(serializedData.FindProperty("_targetDuration"));

        if (serializedData.ApplyModifiedProperties())
        {
            _targetData.ClearBakedData();
            EditorUtility.SetDirty(_targetData);
        }
    }

    private void DrawStatus()
    {
        if (!_targetData.IsBaked)
        {
            EditorGUILayout.HelpBox("Not baked", MessageType.None);
            return;
        }

        EditorGUILayout.HelpBox(
            $"Source {_targetData.SourceDuration:F3}s | Runtime {_targetData.RuntimeDuration:F3}s | " +
            $"Speed {_targetData.PlaybackSpeed:F3}\n" +
            $"Offset {_targetData.TotalLocalOffset:F3} | Yaw {_targetData.TotalYaw:F1} | " +
            $"Rotation end {_targetData.RotationFinishedRuntimeTime:F3}s | {_targetData.EndFootPhase}\n" +
            $"{_targetData.BakedSampleRate} FPS | {_targetData.BakedAtUtc} UTC",
            MessageType.None);
    }

    private void CreateDataAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Root Motion Data",
            "RootMotionData",
            "asset",
            "Choose where to save the baked data asset.");
        if (string.IsNullOrEmpty(path))
            return;

        var data = CreateInstance<RootMotionData>();
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        _targetData = data;
        Selection.activeObject = data;
    }

    private void Bake()
    {
        if (!ValidateSetup())
            return;

        Undo.RecordObject(_targetData, "Bake Root Motion");
        try
        {
            BakeClip();
            EditorUtility.SetDirty(_targetData);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Bake complete", $"Baked {_targetData.Clip.name}.", "OK");
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

        if (_samplePrefab == null || _samplePrefab.GetComponentInChildren<Animator>(true) == null)
        {
            EditorUtility.DisplayDialog("Missing prefab", "Assign a prefab containing an Animator.", "OK");
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
            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
            animator.Rebind();

            graph = PlayableGraph.Create($"RootMotionBake_{_targetData.Clip.name}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, _targetData.Clip);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            output.SetSourcePlayable(playable);
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
            var cumulativeYaw = new float[sampleCount + 1];

            Quaternion startRotation = animator.transform.rotation;
            Vector3 totalWorldOffset = Vector3.zero;
            float totalYaw = 0f;

            for (int i = 1; i <= sampleCount; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Root Motion Bake",
                    $"Sampling {_targetData.Clip.name}",
                    (float)i / sampleCount);

                Quaternion previousRotation = animator.transform.rotation;
                graph.Evaluate(deltaTime);

                Vector3 worldDelta = animator.deltaPosition;
                Vector3 localVelocity = Quaternion.Inverse(previousRotation) * worldDelta / deltaTime;
                float yawDelta = ExtractSignedYaw(animator.deltaRotation);
                float yawVelocity = yawDelta / deltaTime;
                float normalizedTime = (float)i / sampleCount;

                if (i == 1)
                    AddKeys(0f, localVelocity, yawVelocity, velocityX, velocityY, velocityZ, angularVelocityY);
                AddKeys(normalizedTime, localVelocity, yawVelocity, velocityX, velocityY, velocityZ, angularVelocityY);

                totalWorldOffset += worldDelta;
                totalYaw += yawDelta;
                cumulativeYaw[i] = totalYaw;
            }

            SetLinearTangents(velocityX);
            SetLinearTangents(velocityY);
            SetLinearTangents(velocityZ);
            SetLinearTangents(angularVelocityY);

            float playbackSpeed = _targetData.TargetDuration > 0.01f
                ? duration / _targetData.TargetDuration
                : 1f;
            _targetData.SetBakedData(
                duration,
                playbackSpeed,
                velocityX,
                velocityY,
                velocityZ,
                angularVelocityY,
                Quaternion.Inverse(startRotation) * totalWorldOffset,
                totalYaw,
                FindRotationFinishedTime(cumulativeYaw, sampleCount),
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

    private float FindRotationFinishedTime(float[] cumulativeYaw, int sampleCount)
    {
        float finalYaw = cumulativeYaw[sampleCount];
        int index = sampleCount;
        while (index >= 0 && Mathf.Abs(cumulativeYaw[index] - finalYaw) <= _rotationTolerance)
            index--;
        return index < 0 ? 0f : (float)Mathf.Min(index + 1, sampleCount) / sampleCount;
    }

    private int ResolveSampleRate(AnimationClip clip)
    {
        return _sampleRateMode switch
        {
            SampleRateMode.Fps60 => 60,
            SampleRateMode.Fps120 => 120,
            _ => clip.frameRate > 0f ? Mathf.RoundToInt(clip.frameRate) : 30
        };
    }

    private static void AddKeys(
        float time,
        Vector3 velocity,
        float yawVelocity,
        AnimationCurve x,
        AnimationCurve y,
        AnimationCurve z,
        AnimationCurve yaw)
    {
        x.AddKey(time, velocity.x);
        y.AddKey(time, velocity.y);
        z.AddKey(time, velocity.z);
        yaw.AddKey(time, yawVelocity);
    }

    private static float ExtractSignedYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;
        return forward.sqrMagnitude < 0.000001f
            ? 0f
            : Vector3.SignedAngle(Vector3.forward, forward.normalized, Vector3.up);
    }

    private static BakedFootPhase DetectEndFootPhase(Animator animator)
    {
        if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid)
            return BakedFootPhase.Unknown;

        Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (left == null || right == null)
            return BakedFootPhase.Unknown;

        float leftY = animator.transform.InverseTransformPoint(left.position).y;
        float rightY = animator.transform.InverseTransformPoint(right.position).y;
        return leftY <= rightY ? BakedFootPhase.LeftFootDown : BakedFootPhase.RightFootDown;
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
