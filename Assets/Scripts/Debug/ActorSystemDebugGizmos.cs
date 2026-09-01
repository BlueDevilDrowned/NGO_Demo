using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Draws the data owned by an Actor's logical systems in the Scene view.
/// Add this component to an Actor when inspecting prediction/authority state.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Actor))]
public sealed class ActorSystemDebugGizmos : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool drawOnlyWhenSelected = true;
    [SerializeField] private bool drawLabels = true;
    [Min(0.01f)] [SerializeField] private float axisLength = 0.35f;

    [Header("Camera")]
    [SerializeField] private bool drawCamera = true;
    [SerializeField] private bool drawAuthoritativeCamera = true;
    [SerializeField] private bool drawCameraPivotReference = true;
    [SerializeField] private bool drawOutputCamera = true;
    [Min(0.01f)] [SerializeField] private float cameraRayLength = 2f;

    [Header("Aim")]
    [SerializeField] private bool drawAim;
    [Min(0.01f)] [SerializeField] private float aimRayLength = 2f;

    [Header("Locomotion")]
    [SerializeField] private bool drawLocomotion;
    [Min(0.01f)] [SerializeField] private float locomotionRayLength = 1.5f;

    [Header("Input")]
    [SerializeField] private bool drawInput;

    [Header("Perspective")]
    [SerializeField] private bool drawPerspective;

    private Actor actor;

    private static readonly Color LocalCameraColor = new(0.1f, 1f, 0.25f, 1f);
    private static readonly Color AuthoritativeCameraColor = new(1f, 0.2f, 0.15f, 1f);
    private static readonly Color OutputCameraColor = new(0.2f, 0.65f, 1f, 1f);
    private static readonly Color AimColor = new(1f, 0.75f, 0.1f, 1f);
    private static readonly Color LocomotionColor = new(0.7f, 0.3f, 1f, 1f);
    private static readonly Color InputColor = new(0.1f, 0.9f, 0.9f, 1f);

    private void Awake()
    {
        actor = GetComponent<Actor>();
    }

    private void OnValidate()
    {
        actor = GetComponent<Actor>();
        axisLength = Mathf.Max(0.01f, axisLength);
        cameraRayLength = Mathf.Max(0.01f, cameraRayLength);
        aimRayLength = Mathf.Max(0.01f, aimRayLength);
        locomotionRayLength = Mathf.Max(0.01f, locomotionRayLength);
    }

    private void OnDrawGizmos()
    {
        if (drawOnlyWhenSelected && !SelectionContainsThisObject())
            return;

        if (actor == null)
            actor = GetComponent<Actor>();
        if (actor == null)
            return;

        if (drawCamera)
            DrawCameraData(actor.cameraSystem?.data, "Local camera", LocalCameraColor);

        if (drawAuthoritativeCamera && actor.simulation != null)
            DrawCameraData(actor.simulation.cameraData, "Authoritative camera", AuthoritativeCameraColor);

        if (drawCameraPivotReference)
            DrawCameraPivotReference();

        if (drawOutputCamera && actor.IsOwner)
            DrawOutputCamera();

        if (drawAim)
            DrawAimData();

        if (drawLocomotion)
            DrawLocomotionData();

        if (drawInput)
            DrawInputData();

        if (drawPerspective)
            DrawPerspectiveData();
    }

    private void DrawCameraData(ActorCameraData? optionalData, string label, Color color)
    {
        if (!optionalData.HasValue)
            return;

        ActorCameraData data = optionalData.Value;
        if (!IsFinite(data.ViewOrigin) || !IsFinite(data.ViewDirection) ||
            data.ViewDirection.sqrMagnitude <= 0.000001f)
            return;

        Vector3 direction = data.ViewDirection.normalized;
        Gizmos.color = color;
        Gizmos.DrawSphere(data.ViewOrigin, axisLength * 0.12f);
        DrawAxes(data.ViewOrigin, Quaternion.Euler(data.ViewPitch, data.ViewYaw, 0f), axisLength, color);
        DrawArrow(data.ViewOrigin, direction, cameraRayLength, color);

        if (drawLabels)
        {
            DrawLabel(data.ViewOrigin + Vector3.up * axisLength,
                $"{label}\nYaw {data.ViewYaw:0.0}  Pitch {data.ViewPitch:0.0}");
        }
    }

    private void DrawOutputCamera()
    {
        Transform output = actor.cameraSystem?.rig?.OutputTransform;
        if (output == null)
            return;

        Gizmos.color = OutputCameraColor;
        DrawAxes(output.position, output.rotation, axisLength, OutputCameraColor);
        DrawArrow(output.position, output.forward, cameraRayLength, OutputCameraColor);
        if (drawLabels)
            DrawLabel(output.position + Vector3.up * axisLength, "Output camera");
    }

    private void DrawCameraPivotReference()
    {
        Transform pivot = actor.firstCameraPivot;
        if (pivot == null)
            return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pivot.position, axisLength * 0.2f);
        DrawArrow(pivot.position, pivot.forward, axisLength, Color.white);

        if (actor.simulation != null && IsFinite(actor.simulation.cameraData.ViewOrigin))
        {
            Gizmos.color = AuthoritativeCameraColor;
            Gizmos.DrawLine(pivot.position, actor.simulation.cameraData.ViewOrigin);
        }

        if (drawLabels)
            DrawLabel(pivot.position + Vector3.up * axisLength * 0.5f, "First camera pivot");
    }

    private void DrawAimData()
    {
        if (actor.simulation == null)
            return;

        Vector3 origin = actor.simulation.cameraData.ViewOrigin;
        Vector3 target = actor.simulation.aimData.TargetPosition;
        if (!IsFinite(origin) || !IsFinite(target))
            return;

        Gizmos.color = AimColor;
        Gizmos.DrawSphere(target, axisLength * 0.16f);
        Gizmos.DrawLine(origin, target);
        DrawArrow(target, (target - origin).normalized, aimRayLength * 0.15f, AimColor);
        if (drawLabels)
            DrawLabel(target + Vector3.up * axisLength * 0.5f, "Authoritative aim target");
    }

    private void DrawLocomotionData()
    {
        if (actor.simulation == null)
            return;

        Vector3 direction = actor.simulation.locomotionData.DesiredWorldMoveDirection;
        if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            return;

        Vector3 origin = actor.transform.position + Vector3.up * axisLength;
        Gizmos.color = LocomotionColor;
        DrawArrow(origin, direction.normalized, locomotionRayLength * Mathf.Clamp01(direction.magnitude), LocomotionColor);
        if (drawLabels)
            DrawLabel(origin + Vector3.up * axisLength, $"Locomotion: {actor.simulation.locomotionData.stateType}");
    }

    private void DrawInputData()
    {
        if (actor.simulation == null)
            return;

        ActorInputData input = actor.simulation.inputData;
        Vector3 origin = actor.transform.position + Vector3.up * axisLength * 2f;
        Vector3 move = new(input.InputMove.x, 0f, input.InputMove.y);

        Gizmos.color = InputColor;
        if (move.sqrMagnitude > 0.000001f)
            DrawArrow(origin, move.normalized, axisLength * Mathf.Clamp01(move.magnitude), InputColor);
        if (drawLabels)
            DrawLabel(origin + Vector3.up * axisLength,
                $"Input move {input.InputMove}\nLook {input.InputLook}\nHeld {input.Held}");
    }

    private void DrawPerspectiveData()
    {
        string mode = actor.perspectiveSystem == null
            ? "Unavailable"
            : $"Authority {actor.perspectiveSystem.AuthoritativeMode}\nPresentation {actor.perspectiveSystem.PresentationMode}";
        if (drawLabels)
            DrawLabel(actor.transform.position + Vector3.up * (axisLength * 3f), mode);
    }

    private void DrawAxes(Vector3 origin, Quaternion rotation, float length, Color color)
    {
        DrawArrow(origin, rotation * Vector3.right, length, Color.red);
        DrawArrow(origin, rotation * Vector3.up, length, Color.green);
        DrawArrow(origin, rotation * Vector3.forward, length, color);
    }

    private static void DrawArrow(Vector3 origin, Vector3 direction, float length, Color color)
    {
        if (direction.sqrMagnitude <= 0.000001f || !IsFinite(direction))
            return;

        Vector3 normalized = direction.normalized;
        Vector3 end = origin + normalized * length;
        Gizmos.color = color;
        Gizmos.DrawLine(origin, end);
        float headLength = Mathf.Min(length * 0.2f, 0.25f);
        Vector3 side = Vector3.Cross(normalized, Mathf.Abs(Vector3.Dot(normalized, Vector3.up)) > 0.95f
            ? Vector3.right
            : Vector3.up).normalized;
        Gizmos.DrawLine(end, end - normalized * headLength + side * headLength * 0.5f);
        Gizmos.DrawLine(end, end - normalized * headLength - side * headLength * 0.5f);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void DrawLabel(Vector3 position, string text)
    {
#if UNITY_EDITOR
        Handles.Label(position, text);
#endif
    }

    private bool SelectionContainsThisObject()
    {
#if UNITY_EDITOR
        return Selection.Contains(gameObject);
#else
        return false;
#endif
    }
}
