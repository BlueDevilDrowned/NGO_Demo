using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

internal enum LagCompensationDebugShapeType : byte
{
    Box,
    Sphere,
    Capsule
}

internal struct LagCompensationDebugShape
{
    public LagCompensationDebugShapeType Type;
    public Vector3 Center;
    public Quaternion Rotation;
    public Vector3 Size;
    public Vector3 Axis;
    public float Radius;
    public float SegmentHalfLength;
    public bool IsHitCollider;

    public static bool TryCreate(
        Collider collider,
        bool isHitCollider,
        out LagCompensationDebugShape shape)
    {
        shape = default;
        if (collider == null) return false;

        Transform target = collider.transform;
        Vector3 scale = Abs(target.lossyScale);
        shape.Rotation = target.rotation;
        shape.IsHitCollider = isHitCollider;

        switch (collider)
        {
            case BoxCollider box:
                shape.Type = LagCompensationDebugShapeType.Box;
                shape.Center = target.TransformPoint(box.center);
                shape.Size = Vector3.Scale(box.size, scale);
                return true;

            case SphereCollider sphere:
                shape.Type = LagCompensationDebugShapeType.Sphere;
                shape.Center = target.TransformPoint(sphere.center);
                shape.Radius = sphere.radius * Mathf.Max(scale.x, scale.y, scale.z);
                return true;

            case CapsuleCollider capsule:
                Vector3 localAxis = capsule.direction switch
                {
                    0 => Vector3.right,
                    2 => Vector3.forward,
                    _ => Vector3.up,
                };
                float axisScale = capsule.direction switch
                {
                    0 => scale.x,
                    2 => scale.z,
                    _ => scale.y,
                };
                float radiusScale = capsule.direction switch
                {
                    0 => Mathf.Max(scale.y, scale.z),
                    2 => Mathf.Max(scale.x, scale.y),
                    _ => Mathf.Max(scale.x, scale.z),
                };
                shape.Type = LagCompensationDebugShapeType.Capsule;
                shape.Center = target.TransformPoint(capsule.center);
                shape.Axis = target.TransformDirection(localAxis).normalized;
                shape.Radius = capsule.radius * radiusScale;
                shape.SegmentHalfLength = Mathf.Max(
                    0f,
                    capsule.height * axisScale * 0.5f - shape.Radius);
                return true;

            default:
                return false;
        }
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}

[DisallowMultipleComponent]
public sealed class LagCompensationDebugDrawer : MonoBehaviour
{
    private static readonly Color ShotTickColor = new(0.1f, 0.85f, 1f, 1f);
    private static readonly Color HitTickColor = new(0.15f, 0.8f, 0.45f, 1f);
    private static readonly Color HitColliderColor = new(0.55f, 1f, 0.1f, 1f);
    private static readonly Color CurrentColor = new(1f, 1f, 1f, 0.9f);
    private static readonly Color TrajectoryColor = new(1f, 0.55f, 0.1f, 1f);
    private static readonly Color MissColor = new(1f, 0.2f, 0.15f, 1f);

    private readonly List<ShotRecord> records = new();
    private LagCompensationSystem system;
    private LagCompensationConfig config;
    private uint tickRate = 60;

#if UNITY_EDITOR
    private GUIStyle labelStyle;
    private GUIStyle tickStyle;
#endif

    internal void Initialize(
        LagCompensationSystem owner,
        LagCompensationConfig settings,
        uint networkTickRate)
    {
        system = owner;
        config = settings;
        tickRate = networkTickRate > 0 ? networkTickRate : 1u;
    }

    internal void BeginShot(
        Actor shooter,
        uint projectileId,
        uint shotTick,
        uint receiveTick,
        uint presentationTick,
        uint inputTick,
        Vector3 origin)
    {
        if (!IsEnabled) return;
        PruneExpired();
        if (config.ClearDebugOnNextShot)
            records.Clear();
        while (records.Count >= config.MaxDebugShots)
            records.RemoveAt(0);

        records.Add(new ShotRecord
        {
            Shooter = shooter,
            ProjectileId = projectileId,
            ShotTick = shotTick,
            ReceiveTick = receiveTick,
            PresentationTick = presentationTick,
            InputTick = inputTick,
            Origin = origin,
            LabelPosition = origin,
            LastUpdatedTime = Time.realtimeSinceStartup,
        });

    }

    internal void RecordSegment(
        Actor shooter,
        uint projectileId,
        uint simulationTick,
        Vector3 from,
        Vector3 to,
        bool hit)
    {
        ShotRecord record = Find(shooter, projectileId);
        if (record == null) return;

        record.Segments.Add(new TrajectorySegment
        {
            Tick = simulationTick,
            From = from,
            To = to,
            Hit = hit,
        });
        record.LastSimulationTick = simulationTick;
        record.LabelPosition = to;
        record.LastUpdatedTime = Time.realtimeSinceStartup;
    }

    internal void CompleteHit(
        Actor shooter,
        uint projectileId,
        uint hitSimulationTick,
        uint resolveTick,
        LagCompensatedBody targetBody,
        Collider hitCollider,
        Vector3 point,
        Vector3 normal,
        in ProjectileHitResult result)
    {
        ShotRecord record = Find(shooter, projectileId);
        if (record == null) return;

        record.Completed = true;
        record.HasHit = true;
        record.HitSimulationTick = hitSimulationTick;
        record.ResolveTick = resolveTick;
        record.HitPoint = point;
        record.HitNormal = normal;
        record.Damage = result.Damage;
        record.TargetName = result.Target != null
            ? result.Target.name
            : targetBody != null
                ? targetBody.name
                : hitCollider != null
                    ? hitCollider.name
                    : "Static";
        record.HitLocation = result.Hitbox != null
            ? result.Location.ToString()
            : "None";
        record.LabelPosition = point;
        record.LastUpdatedTime = Time.realtimeSinceStartup;

        if (system == null || targetBody == null) return;
        record.TimelineFrames.Clear();
        int receiveAfterHit = TickDifference(record.ReceiveTick, hitSimulationTick);
        uint startTick = receiveAfterHit >= 0 ? hitSimulationTick : record.ReceiveTick;
        uint endTick = receiveAfterHit >= 0 ? record.ReceiveTick : hitSimulationTick;
        int span = Mathf.Abs(receiveAfterHit);

        int interval = config != null
            ? config.DebugHitboxSampleIntervalTicks
            : 1;
        for (int elapsed = 0; elapsed < span; elapsed += interval)
        {
            uint tick = startTick + (uint)elapsed;

            DebugFrame frame = new()
            {
                Tick = tick,
                Color = tick == hitSimulationTick
                    ? HitTickColor
                    : tick == record.ReceiveTick
                        ? ShotTickColor
                        : CurrentColor,
            };
            system.CaptureHistoricalDebugShapes(
                targetBody,
                tick,
                tick == hitSimulationTick ? hitCollider : null,
                frame.Shapes);
            record.TimelineFrames.Add(frame);
        }

        if (span > 0)
        {
            DebugFrame hitFrame = new()
            {
                Tick = endTick,
                Color = endTick == hitSimulationTick
                    ? HitTickColor
                    : ShotTickColor,
            };
            system.CaptureHistoricalDebugShapes(
                targetBody,
                endTick,
                endTick == hitSimulationTick ? hitCollider : null,
                hitFrame.Shapes);
            record.TimelineFrames.Add(hitFrame);
        }
        else if (record.TimelineFrames.Count == 0)
        {
            DebugFrame hitFrame = new()
            {
                Tick = endTick,
                Color = HitTickColor,
            };
            system.CaptureHistoricalDebugShapes(
                targetBody,
                endTick,
                hitCollider,
                hitFrame.Shapes);
            record.TimelineFrames.Add(hitFrame);
        }
    }

    internal void CompleteExpired(
        Actor shooter,
        uint projectileId,
        uint simulationTick,
        uint resolveTick,
        Vector3 endPoint)
    {
        ShotRecord record = Find(shooter, projectileId);
        if (record == null) return;

        record.Completed = true;
        record.ResolveTick = resolveTick;
        record.HitSimulationTick = simulationTick;
        record.LabelPosition = endPoint;
        record.LastUpdatedTime = Time.realtimeSinceStartup;
    }

    private bool IsEnabled => config != null && config.EnableDebugDraw;

    private void Update()
    {
        if (IsEnabled)
            PruneExpired();
    }

    private void PruneExpired()
    {
        float duration = config != null ? config.DebugDrawDurationSeconds : 0f;
        if (duration <= 0f) return;

        float now = Time.realtimeSinceStartup;
        for (int i = records.Count - 1; i >= 0; i--)
        {
            ShotRecord record = records[i];
            if (record.Completed && now - record.LastUpdatedTime >= duration)
                records.RemoveAt(i);
        }
    }

    private ShotRecord Find(Actor shooter, uint projectileId)
    {
        for (int i = records.Count - 1; i >= 0; i--)
        {
            ShotRecord record = records[i];
            if (record.Shooter == shooter && record.ProjectileId == projectileId)
                return record;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (!IsEnabled) return;
        PruneExpired();

#if UNITY_EDITOR
        CompareFunction previousZTest = UnityEditor.Handles.zTest;
        UnityEditor.Handles.zTest = CompareFunction.Always;
#endif
        for (int i = 0; i < records.Count; i++)
            DrawRecord(records[i]);
#if UNITY_EDITOR
        UnityEditor.Handles.zTest = previousZTest;
#endif
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawRecord(ShotRecord record)
    {
        for (int i = 0; i < record.TimelineFrames.Count; i++)
        {
            DebugFrame frame = record.TimelineFrames[i];
            DrawShapes(frame.Shapes, frame.Color);
        }

        for (int i = 0; i < record.Segments.Count; i++)
        {
            TrajectorySegment segment = record.Segments[i];
            Gizmos.color = segment.Hit ? HitColliderColor : TrajectoryColor;
            Gizmos.DrawLine(segment.From, segment.To);
            Gizmos.DrawSphere(segment.To, segment.Hit ? 0.035f : 0.015f);
#if UNITY_EDITOR
            if (config.DrawDebugTickLabels)
                UnityEditor.Handles.Label(segment.To, segment.Tick.ToString(), GetTickStyle());
#endif
        }

        if (record.HasHit)
        {
            Gizmos.color = HitColliderColor;
            Gizmos.DrawSphere(record.HitPoint, 0.06f);
            Gizmos.DrawLine(record.HitPoint, record.HitPoint + record.HitNormal * 0.35f);
        }
        else if (record.Completed)
        {
            Gizmos.color = MissColor;
            Gizmos.DrawWireSphere(record.LabelPosition, 0.06f);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            record.LabelPosition + Vector3.up * 0.25f,
            BuildLabel(record),
            GetLabelStyle());
#endif
    }

    private static void DrawShapes(
        List<LagCompensationDebugShape> shapes,
        Color defaultColor)
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            LagCompensationDebugShape shape = shapes[i];
            Gizmos.color = shape.IsHitCollider ? HitColliderColor : defaultColor;
            DrawShape(in shape, 0f);
            if (shape.IsHitCollider)
                DrawShape(in shape, 0.025f);
        }
    }

    private static void DrawShape(in LagCompensationDebugShape shape, float expansion)
    {
        Gizmos.matrix = Matrix4x4.identity;
        switch (shape.Type)
        {
            case LagCompensationDebugShapeType.Box:
                Gizmos.matrix = Matrix4x4.TRS(
                    shape.Center,
                    shape.Rotation,
                    Vector3.one);
                Gizmos.DrawWireCube(
                    Vector3.zero,
                    shape.Size + Vector3.one * expansion);
                break;

            case LagCompensationDebugShapeType.Sphere:
                Gizmos.DrawWireSphere(shape.Center, shape.Radius + expansion * 0.5f);
                break;

            case LagCompensationDebugShapeType.Capsule:
                DrawWireCapsule(in shape, expansion * 0.5f);
                break;
        }
        Gizmos.matrix = Matrix4x4.identity;
    }

    private static void DrawWireCapsule(
        in LagCompensationDebugShape shape,
        float expansion)
    {
        float radius = shape.Radius + expansion;
        Vector3 axis = shape.Axis.sqrMagnitude > 0.000001f
            ? shape.Axis.normalized
            : Vector3.up;
        Vector3 endOffset = axis * shape.SegmentHalfLength;
        Vector3 a = shape.Center - endOffset;
        Vector3 b = shape.Center + endOffset;
        Gizmos.DrawWireSphere(a, radius);
        Gizmos.DrawWireSphere(b, radius);

        Vector3 tangentA = Vector3.Cross(axis, Vector3.up);
        if (tangentA.sqrMagnitude <= 0.000001f)
            tangentA = Vector3.Cross(axis, Vector3.right);
        tangentA.Normalize();
        Vector3 tangentB = Vector3.Cross(axis, tangentA).normalized;
        Gizmos.DrawLine(a + tangentA * radius, b + tangentA * radius);
        Gizmos.DrawLine(a - tangentA * radius, b - tangentA * radius);
        Gizmos.DrawLine(a + tangentB * radius, b + tangentB * radius);
        Gizmos.DrawLine(a - tangentB * radius, b - tangentB * radius);
    }

#if UNITY_EDITOR
    private string BuildLabel(ShotRecord record)
    {
        int presentationToExecuteTicks = TickDifference(
            record.ReceiveTick,
            record.PresentationTick);
        int rollbackOffsetTicks = TickDifference(
            record.ReceiveTick,
            record.ShotTick);
        int totalTicks = record.Completed
            ? TickDifference(record.ResolveTick, record.ShotTick)
            : TickDifference(record.LastSimulationTick, record.ShotTick);
        int flightTicks = record.Completed
            ? TickDifference(record.HitSimulationTick, record.ShotTick)
            : Mathf.Max(0, totalTicks);
        int latenessTicks = record.Completed
            ? TickDifference(record.ResolveTick, record.HitSimulationTick)
            : 0;

        StringBuilder text = new(256);
        text.Append("Client Presentation Tick: ").Append(record.PresentationTick)
            .Append("\nInput Tick: ").Append(record.InputTick)
            .Append("\nServer Receive Tick: ").Append(record.ReceiveTick)
            .Append("\nRollback Tick: ").Append(record.ShotTick)
            .Append("\nHit Simulation Tick: ")
            .Append(record.Completed ? record.HitSimulationTick : record.LastSimulationTick)
            .Append("\nResolve Tick: ")
            .Append(record.Completed ? record.ResolveTick.ToString() : "Pending")
            .Append("\nPresentation->Execute Delay: ")
            .Append(FormatDelay(presentationToExecuteTicks))
            .Append("\nRollback Offset: ").Append(FormatDelay(rollbackOffsetTicks))
            .Append("\nProjectile Time: ").Append(FormatDelay(flightTicks))
            .Append("\nDamage Lateness: ").Append(FormatDelay(latenessTicks));

        if (record.HasHit)
        {
            text.Append("\nDamage: ").Append(record.Damage.ToString("0.##"))
                .Append("\nTarget: ").Append(record.TargetName)
                .Append(" / ").Append(record.HitLocation);
        }
        else if (record.Completed)
        {
            text.Append("\nResult: Expired / Miss");
        }

        text.Append("\nCyan=Receive Tick  White=Sampled Tick  Green=Hit Tick");
        return text.ToString();
    }

    private GUIStyle GetLabelStyle()
    {
        if (labelStyle != null) return labelStyle;
        labelStyle = new GUIStyle(UnityEditor.EditorStyles.helpBox)
        {
            fontSize = 11,
            normal = { textColor = Color.white },
        };
        return labelStyle;
    }

    private GUIStyle GetTickStyle()
    {
        if (tickStyle != null) return tickStyle;
        tickStyle = new GUIStyle(UnityEditor.EditorStyles.miniLabel)
        {
            fontSize = 9,
            normal = { textColor = TrajectoryColor },
        };
        return tickStyle;
    }
#endif

    private string FormatDelay(int ticks)
    {
        float milliseconds = Mathf.Max(0, ticks) * 1000f / tickRate;
        return $"{Mathf.Max(0, ticks)} ticks / {milliseconds:0.0} ms";
    }

    private static int TickDifference(uint current, uint previous)
    {
        return unchecked((int)(current - previous));
    }

    private sealed class ShotRecord
    {
        public Actor Shooter;
        public uint ProjectileId;
        public uint ShotTick;
        public uint PresentationTick;
        public uint InputTick;
        public uint ReceiveTick;
        public uint LastSimulationTick;
        public uint HitSimulationTick;
        public uint ResolveTick;
        public Vector3 Origin;
        public Vector3 LabelPosition;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float Damage;
        public float LastUpdatedTime;
        public bool Completed;
        public bool HasHit;
        public string TargetName;
        public string HitLocation;
        public readonly List<TrajectorySegment> Segments = new(32);
        public readonly List<DebugFrame> TimelineFrames = new(16);
    }

    private struct TrajectorySegment
    {
        public uint Tick;
        public Vector3 From;
        public Vector3 To;
        public bool Hit;
    }

    private sealed class DebugFrame
    {
        public uint Tick;
        public Color Color;
        public readonly List<LagCompensationDebugShape> Shapes = new(16);
    }
}
