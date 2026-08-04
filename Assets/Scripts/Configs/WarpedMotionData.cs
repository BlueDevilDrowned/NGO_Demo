using System;
using System.Collections.Generic;
using UnityEngine;

public enum WarpedMotionType
{
    None = 0,
    Vault = 1,
    Dodge = 2,
    Simple = 3,
    Custom = 4
}

[Serializable]
public class WarpedMotionPoint
{
    public string PointName = "Point";

    [Range(0f, 1f)]
    public float NormalizedTime = 1f;

    [Tooltip("Extra local-space offset applied to the runtime target.")]
    public Vector3 TargetPositionOffset;

    [Header("Baked Segment Data")]
    [Tooltip("Animation displacement from the previous warp point to this point, in start-local space.")]
    public Vector3 BakedLocalOffset;

    [Tooltip("Animation rotation from the previous warp point to this point.")]
    public Quaternion BakedLocalRotation = Quaternion.identity;
}

[Serializable]
public struct WarpedMotionSample
{
    public Vector3 LocalVelocity;
    public float AngularVelocityY;
    public float HandIKWeight;
}

[CreateAssetMenu(
    fileName = "WarpedMotionData",
    menuName = "Scriptable Objects/Animation/Warped Motion Data")]
public class WarpedMotionData : ScriptableObject
{
    [Header("Animation Source")]
    [SerializeField] private AnimationClip _clip = null;
    [SerializeField] private WarpedMotionType _type = WarpedMotionType.Simple;

    [Header("Warp Targets")]
    [Tooltip("None and Custom keep these points. Vault, Dodge, and Simple regenerate them when baked.")]
    [SerializeField] private List<WarpedMotionPoint> _warpPoints = new List<WarpedMotionPoint>();

    [Tooltip("Normalized-time weight used by the runtime hand IK layer.")]
    [SerializeField] private AnimationCurve _handIKWeightCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 0f));

    [Header("Baked Curves")]
    [SerializeField, Min(0f)] private float _bakedDuration;
    [SerializeField] private AnimationCurve _localVelocityX = new AnimationCurve();
    [SerializeField] private AnimationCurve _localVelocityY = new AnimationCurve();
    [SerializeField] private AnimationCurve _localVelocityZ = new AnimationCurve();
    [SerializeField] private AnimationCurve _angularVelocityY = new AnimationCurve();
    [SerializeField] private Vector3 _totalBakedLocalOffset;
    [SerializeField] private float _totalBakedYaw;
    [SerializeField] private BakedFootPhase _endFootPhase;
    [SerializeField] private bool _isBaked;

    [Header("Bake Metadata")]
    [SerializeField] private int _bakedSampleRate;
    [SerializeField] private string _bakedAtUtc;

    public AnimationClip Clip => _clip;
    public WarpedMotionType Type => _type;
    public IReadOnlyList<WarpedMotionPoint> WarpPoints => _warpPoints;
    public AnimationCurve HandIKWeightCurve => _handIKWeightCurve;
    public float BakedDuration => _bakedDuration;
    public Vector3 TotalBakedLocalOffset => _totalBakedLocalOffset;
    public float TotalBakedYaw => _totalBakedYaw;
    public BakedFootPhase EndFootPhase => _endFootPhase;
    public bool IsBaked => _isBaked;
    public int BakedSampleRate => _bakedSampleRate;
    public string BakedAtUtc => _bakedAtUtc;

    public WarpedMotionSample Evaluate(float normalizedTime)
    {
        float time = Mathf.Clamp01(normalizedTime);
        return new WarpedMotionSample
        {
            LocalVelocity = new Vector3(
                EvaluateCurve(_localVelocityX, time),
                EvaluateCurve(_localVelocityY, time),
                EvaluateCurve(_localVelocityZ, time)),
            AngularVelocityY = EvaluateCurve(_angularVelocityY, time),
            HandIKWeight = Mathf.Clamp01(EvaluateCurve(_handIKWeightCurve, time))
        };
    }

    public bool TryGetWarpSegment(
        int index,
        out WarpedMotionPoint point,
        out float startNormalizedTime,
        out float duration)
    {
        point = null;
        startNormalizedTime = 0f;
        duration = 0f;

        if (!_isBaked || _warpPoints == null || index < 0 || index >= _warpPoints.Count)
            return false;

        point = _warpPoints[index];
        if (point == null)
            return false;

        startNormalizedTime = index > 0 && _warpPoints[index - 1] != null
            ? _warpPoints[index - 1].NormalizedTime
            : 0f;
        duration = Mathf.Max(0f, point.NormalizedTime - startNormalizedTime) * _bakedDuration;
        return true;
    }

    public Vector3 GetCumulativeBakedOffset(int inclusivePointIndex)
    {
        Vector3 offset = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        if (_warpPoints == null)
            return offset;

        int last = Mathf.Min(inclusivePointIndex, _warpPoints.Count - 1);
        for (int i = 0; i <= last; i++)
        {
            if (_warpPoints[i] != null)
            {
                offset += rotation * _warpPoints[i].BakedLocalOffset;
                rotation *= _warpPoints[i].BakedLocalRotation;
            }
        }

        return offset;
    }

    public void SetBakedData(
        List<WarpedMotionPoint> warpPoints,
        float duration,
        AnimationCurve velocityX,
        AnimationCurve velocityY,
        AnimationCurve velocityZ,
        AnimationCurve angularVelocityY,
        Vector3 totalLocalOffset,
        float totalYaw,
        BakedFootPhase endFootPhase,
        int sampleRate,
        string bakedAtUtc)
    {
        _warpPoints = warpPoints ?? new List<WarpedMotionPoint>();
        _bakedDuration = duration;
        _localVelocityX = velocityX ?? new AnimationCurve();
        _localVelocityY = velocityY ?? new AnimationCurve();
        _localVelocityZ = velocityZ ?? new AnimationCurve();
        _angularVelocityY = angularVelocityY ?? new AnimationCurve();
        _totalBakedLocalOffset = totalLocalOffset;
        _totalBakedYaw = totalYaw;
        _endFootPhase = endFootPhase;
        _bakedSampleRate = sampleRate;
        _bakedAtUtc = bakedAtUtc;
        _isBaked = true;
    }

    public void ClearBakedData()
    {
        _bakedDuration = 0f;
        _localVelocityX = new AnimationCurve();
        _localVelocityY = new AnimationCurve();
        _localVelocityZ = new AnimationCurve();
        _angularVelocityY = new AnimationCurve();
        _totalBakedLocalOffset = Vector3.zero;
        _totalBakedYaw = 0f;
        _endFootPhase = BakedFootPhase.Unknown;
        _bakedSampleRate = 0;
        _bakedAtUtc = string.Empty;
        _isBaked = false;

        if (_warpPoints == null)
            return;

        for (int i = 0; i < _warpPoints.Count; i++)
        {
            WarpedMotionPoint point = _warpPoints[i];
            if (point == null)
                continue;

            point.BakedLocalOffset = Vector3.zero;
            point.BakedLocalRotation = Quaternion.identity;
        }
    }

    private static float EvaluateCurve(AnimationCurve curve, float time)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(time) : 0f;
    }
}
