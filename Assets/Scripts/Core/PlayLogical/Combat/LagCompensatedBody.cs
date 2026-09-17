using System;
using System.Collections.Generic;
using UnityEngine;

public enum LagCompensatedBodyType : byte
{
    Hitbox,
    Pickup
}

/// <summary>Describes one source collider in the offline-built rewind tree.</summary>
[Serializable]
public sealed class LagCompensatedColliderNode
{
    public string path;
    public Collider source;
    public Vector3 rootLocalPosition;
    public Quaternion rootLocalRotation = Quaternion.identity;
    public Vector3 rootLocalScale = Vector3.one;
    public ColliderType type;
}

public enum ColliderType : byte { Box, Sphere, Capsule, Other }

/// <summary>
/// Registers a root object for future lag compensation and stores its collider tree.
/// The tree is built in the editor; runtime only resolves the serialized references.
/// </summary>
[DisallowMultipleComponent]
public sealed class LagCompensatedBody : MonoBehaviour
{
    [SerializeField, HideInInspector] private LagCompensatedBodyType bodyType;
    [SerializeField] private bool includeInactiveColliders = true;
    [SerializeField, HideInInspector] private List<LagCompensatedColliderNode> nodes = new();

    public LagCompensatedBodyType BodyType => bodyType;
    public IReadOnlyList<LagCompensatedColliderNode> Nodes => nodes;

    public void SetBodyType(LagCompensatedBodyType value)
    {
        bodyType = value;
    }

    private void OnEnable()
    {
        LagCompensationWorld.TryRegister(this);
    }

    private void OnDisable()
    {
        LagCompensationWorld.TryUnregister(this);
    }

    public void RebuildOfflineTree()
    {
        nodes ??= new List<LagCompensatedColliderNode>();
        nodes.Clear();

        Transform root = transform;
        if (bodyType == LagCompensatedBodyType.Hitbox)
        {
            HitboxManager manager = GetComponentInChildren<HitboxManager>(true);
            if (manager == null)
            {
                Debug.LogWarning(
                    $"{name} has no HitboxManager, so its rewind collider tree is empty.",
                    this);
                return;
            }

            IReadOnlyList<Hitbox> hitboxes = manager.Hitboxes;
            for (int i = 0; i < hitboxes.Count; i++)
                AddNode(root, hitboxes[i] != null ? hitboxes[i].Collider : null);
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactiveColliders);
        for (int i = 0; i < colliders.Length; i++)
            AddNode(root, colliders[i]);
    }

    public bool TryGetNode(int index, out LagCompensatedColliderNode node)
    {
        node = null;
        return nodes != null && index >= 0 && index < nodes.Count && (node = nodes[index]) != null;
    }

    private static ColliderType GetColliderType(Collider collider)
    {
        if (collider is BoxCollider) return ColliderType.Box;
        if (collider is SphereCollider) return ColliderType.Sphere;
        if (collider is CapsuleCollider) return ColliderType.Capsule;
        return ColliderType.Other;
    }

    private void AddNode(Transform root, Collider collider)
    {
        if (collider == null) return;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i]?.source == collider)
                return;
        }

        Transform current = collider.transform;
        nodes.Add(new LagCompensatedColliderNode
        {
            path = BuildPath(root, current),
            source = collider,
            rootLocalPosition = root.InverseTransformPoint(current.position),
            rootLocalRotation = Quaternion.Inverse(root.rotation) * current.rotation,
            rootLocalScale = DivideScale(current.lossyScale, root.lossyScale),
            type = GetColliderType(collider)
        });
    }

    private static string BuildPath(Transform root, Transform target)
    {
        if (target == root) return string.Empty;
        var names = new List<string>();
        for (Transform current = target; current != null && current != root; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    private static Vector3 DivideScale(Vector3 value, Vector3 divisor)
    {
        return new Vector3(
            Mathf.Abs(divisor.x) > Mathf.Epsilon ? value.x / divisor.x : value.x,
            Mathf.Abs(divisor.y) > Mathf.Epsilon ? value.y / divisor.y : value.y,
            Mathf.Abs(divisor.z) > Mathf.Epsilon ? value.z / divisor.z : value.z);
    }
}
