using System;
using UnityEngine;

public enum BakedFootPhase
{
    Unknown = 0,
    LeftFootDown = 1,
    RightFootDown = 2
}

[Serializable]
public struct RootMotionSample
{
    public Vector3 LocalVelocity;
    public float AngularVelocityY;
}

[CreateAssetMenu(
    fileName = "RootMotionData",
    menuName = "Scriptable Objects/Animation/Root Motion Data")]
public class RootMotionData : ScriptableObject
{
    [Header("Animation Source")]
    [SerializeField] private AnimationClip _clip = null;

    [Tooltip("Zero uses the source clip duration. A positive value changes the recommended playback speed.")]
    [SerializeField, Min(0f)] private float _targetDuration = 0f;

    [Header("Baked Curves")]
    [SerializeField, Min(0f)] private float _sourceDuration;
    [SerializeField, Min(0f)] private float _playbackSpeed = 1f;
    [SerializeField] private AnimationCurve _localVelocityX = new AnimationCurve();
    [SerializeField] private AnimationCurve _localVelocityY = new AnimationCurve();
    [SerializeField] private AnimationCurve _localVelocityZ = new AnimationCurve();
    [SerializeField] private AnimationCurve _angularVelocityY = new AnimationCurve();
    [SerializeField] private Vector3 _totalLocalOffset;
    [SerializeField] private float _totalYaw;
    [SerializeField, Range(0f, 1f)] private float _rotationFinishedNormalizedTime;
    [SerializeField] private BakedFootPhase _endFootPhase;
    [SerializeField] private bool _isBaked;

    [Header("Bake Metadata")]
    [SerializeField] private int _bakedSampleRate;
    [SerializeField] private string _bakedAtUtc;

    public AnimationClip Clip => _clip;
    public float TargetDuration => _targetDuration;
    public float SourceDuration => _sourceDuration;
    public float PlaybackSpeed => _playbackSpeed;
    public float RuntimeDuration => _sourceDuration / Mathf.Max(_playbackSpeed, 0.0001f);
    public Vector3 TotalLocalOffset => _totalLocalOffset;
    public float TotalYaw => _totalYaw;
    public float RotationFinishedNormalizedTime => _rotationFinishedNormalizedTime;
    public float RotationFinishedRuntimeTime =>
        _rotationFinishedNormalizedTime * RuntimeDuration;
    public BakedFootPhase EndFootPhase => _endFootPhase;
    public bool IsBaked => _isBaked;
    public int BakedSampleRate => _bakedSampleRate;
    public string BakedAtUtc => _bakedAtUtc;

    public RootMotionSample Evaluate(float normalizedTime, bool applyPlaybackSpeed = true)
    {
        float time = Mathf.Clamp01(normalizedTime);
        var sample = new RootMotionSample
        {
            LocalVelocity = new Vector3(
                EvaluateCurve(_localVelocityX, time),
                EvaluateCurve(_localVelocityY, time),
                EvaluateCurve(_localVelocityZ, time)),
            AngularVelocityY = EvaluateCurve(_angularVelocityY, time)
        };

        if (applyPlaybackSpeed)
        {
            sample.LocalVelocity *= _playbackSpeed;
            sample.AngularVelocityY *= _playbackSpeed;
        }

        return sample;
    }

    public void SetBakedData(
        float sourceDuration,
        float playbackSpeed,
        AnimationCurve velocityX,
        AnimationCurve velocityY,
        AnimationCurve velocityZ,
        AnimationCurve angularVelocityY,
        Vector3 totalLocalOffset,
        float totalYaw,
        float rotationFinishedNormalizedTime,
        BakedFootPhase endFootPhase,
        int sampleRate,
        string bakedAtUtc)
    {
        _sourceDuration = sourceDuration;
        _playbackSpeed = playbackSpeed;
        _localVelocityX = velocityX ?? new AnimationCurve();
        _localVelocityY = velocityY ?? new AnimationCurve();
        _localVelocityZ = velocityZ ?? new AnimationCurve();
        _angularVelocityY = angularVelocityY ?? new AnimationCurve();
        _totalLocalOffset = totalLocalOffset;
        _totalYaw = totalYaw;
        _rotationFinishedNormalizedTime = Mathf.Clamp01(rotationFinishedNormalizedTime);
        _endFootPhase = endFootPhase;
        _bakedSampleRate = sampleRate;
        _bakedAtUtc = bakedAtUtc;
        _isBaked = true;
    }

    public void ClearBakedData()
    {
        _sourceDuration = 0f;
        _playbackSpeed = 1f;
        _localVelocityX = new AnimationCurve();
        _localVelocityY = new AnimationCurve();
        _localVelocityZ = new AnimationCurve();
        _angularVelocityY = new AnimationCurve();
        _totalLocalOffset = Vector3.zero;
        _totalYaw = 0f;
        _rotationFinishedNormalizedTime = 0f;
        _endFootPhase = BakedFootPhase.Unknown;
        _bakedSampleRate = 0;
        _bakedAtUtc = string.Empty;
        _isBaked = false;
    }

    private static float EvaluateCurve(AnimationCurve curve, float time)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(time) : 0f;
    }
}
