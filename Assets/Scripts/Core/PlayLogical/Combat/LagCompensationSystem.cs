using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side history capture and rewind queries. Rewind proxies live in the
/// gameplay scene but are enabled only for the duration of a rewind query.
/// </summary>
[DisallowMultipleComponent]
public sealed class LagCompensationSystem : MonoBehaviour
{
    [SerializeField] private LagCompensationConfig config;

    private readonly Dictionary<LagCompensatedBody, BodyRuntime> activeBodies = new();
    private readonly List<BodyRuntime> retiredBodies = new();
    private readonly Dictionary<Collider, ProxyBinding> bindings = new();

    private NetworkManager networkManager;
    private GameObject proxyContainer;
    private LagCompensationDebugDrawer debugDrawer;
    private RaycastHit[] rewindHits;
    private int proxyLayer = -1;
    private int proxyMask;
    private int captureIntervalTicks = 1;
    private int historyTicks = 1;
    private uint currentTick;
    private uint lastCapturedTick;
    private bool hasCapturedTick;
    private bool initialized;
    private bool tickSubscribed;

    public LagCompensationConfig Config => config;
    public bool IsInitialized => initialized;
    public uint CurrentTick => currentTick;

    public bool TryResolveRewindTick(uint requestedTick, out uint rewindTick)
    {
        rewindTick = 0;
        if (!initialized || networkManager == null || !networkManager.IsServer)
            return false;

        currentTick = (uint)networkManager.NetworkTickSystem.ServerTime.Tick;
        int age = TickDifference(currentTick, requestedTick);
        if (age < 0 || age > historyTicks)
            return false;

        rewindTick = requestedTick;
        return true;
    }

    public bool TryGetBodyRootPose(
        LagCompensatedBody body,
        uint rewindTick,
        out Vector3 position,
        out Quaternion rotation,
        out Vector3 scale)
    {
        position = default;
        rotation = Quaternion.identity;
        scale = Vector3.one;
        return initialized && body != null &&
               activeBodies.TryGetValue(body, out BodyRuntime runtime) &&
               runtime.TryGetRootPose(rewindTick, out position, out rotation, out scale);
    }

    public bool TryGetBodyRootPose(
        LagCompensatedBody body,
        uint rewindTick,
        out ActorRootPose pose)
    {
        pose=default;
        if(!TryGetBodyRootPose(
               body,
               rewindTick,
               out Vector3 position,
               out Quaternion rotation,
               out Vector3 scale))
            return false;

        pose=new ActorRootPose(position,rotation,scale);
        return true;
    }

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("LagCompensationSystem requires a LagCompensationConfig.", this);
            enabled = false;
            return;
        }

        if (!LagCompensationWorld.TrySetSystem(this))
        {
            Debug.LogError("Only one LagCompensationSystem can be active.", this);
            enabled = false;
            return;
        }

        LagCompensationWorld.Configure(config);
    }

    private void Update()
    {
        if (!initialized)
            TryInitializeServer();
    }

    private void OnDestroy()
    {
        UnsubscribeTick();
        DisposeAllBodies();
        if (proxyContainer != null)
            Destroy(proxyContainer);

        LagCompensationWorld.ClearSystem(this);
        LagCompensationWorld.ClearConfiguration(config);
    }

    public void Register(LagCompensatedBody body)
    {
        if (!initialized || body == null || activeBodies.ContainsKey(body))
            return;

        BodyRuntime runtime = CreateRuntime(body);
        if (runtime == null)
            return;

        activeBodies.Add(body, runtime);
        runtime.Capture(currentTick, captureIntervalTicks, true);
    }

    public void Unregister(LagCompensatedBody body)
    {
        if (!initialized || body == null || !activeBodies.Remove(body, out BodyRuntime runtime))
            return;

        runtime.CaptureInactive(currentTick);
        runtime.RetiredTick = currentTick;
        retiredBodies.Add(runtime);
    }

    /// <summary>
    /// Queries current static geometry and historical registered bodies, then
    /// returns the closest result for authoritative projectile simulation.
    /// </summary>
    public bool Raycast(
        uint rewindTick,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        int collisionMask,
        LagCompensatedBody ignoredBody,
        out LagCompensatedHit result)
    {
        result = default;
        if (!initialized || direction.sqrMagnitude <= 0.000001f || maxDistance <= 0f)
            return false;

        direction.Normalize();
        int staticMask = config.StaticCollisionMask.value & collisionMask;
        RaycastHit staticHit = default;
        bool hasStaticHit = staticMask != 0 && Physics.defaultPhysicsScene.Raycast(
            origin,
            direction,
            out staticHit,
            maxDistance,
            staticMask,
            QueryTriggerInteraction.Collide);

        float rewindDistance = hasStaticHit ? staticHit.distance : maxDistance;
        bool hasRewindHit = TryRaycastRewind(
            rewindTick,
            origin,
            direction,
            rewindDistance,
            collisionMask,
            ignoredBody,
            out RaycastHit rewindHit,
            out ProxyBinding binding);

        if (hasRewindHit)
        {
            result = new LagCompensatedHit(
                binding.BodyType == LagCompensatedBodyType.Hitbox
                    ? LagCompensatedHitType.Hitbox
                    : LagCompensatedHitType.Pickup,
                binding.Body,
                binding.SourceCollider,
                binding.SourceHitbox,
                rewindHit.point,
                rewindHit.normal,
                rewindHit.distance);
            return true;
        }

        if (!hasStaticHit)
            return false;

        result = new LagCompensatedHit(
            LagCompensatedHitType.Static,
            null,
            staticHit.collider,
            null,
            staticHit.point,
            staticHit.normal,
            staticHit.distance);
        return true;
    }

    internal void BeginDebugShot(
        Actor shooter,
        uint projectileId,
        uint shotTick,
        uint receiveTick,
        uint presentationTick,
        uint inputTick,
        Vector3 origin)
    {
        debugDrawer?.BeginShot(
            shooter,
            projectileId,
            shotTick,
            receiveTick,
            presentationTick,
            inputTick,
            origin);
    }

    internal void RecordDebugSegment(
        Actor shooter,
        uint projectileId,
        uint simulationTick,
        Vector3 from,
        Vector3 to,
        bool hit)
    {
        debugDrawer?.RecordSegment(shooter, projectileId, simulationTick, from, to, hit);
    }

    internal void CompleteDebugHit(
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
        debugDrawer?.CompleteHit(
            shooter,
            projectileId,
            hitSimulationTick,
            resolveTick,
            targetBody,
            hitCollider,
            point,
            normal,
            in result);
    }

    internal void CompleteDebugExpired(
        Actor shooter,
        uint projectileId,
        uint simulationTick,
        uint resolveTick,
        Vector3 endPoint)
    {
        debugDrawer?.CompleteExpired(
            shooter,
            projectileId,
            simulationTick,
            resolveTick,
            endPoint);
    }

    internal void CaptureHistoricalDebugShapes(
        LagCompensatedBody body,
        uint tick,
        Collider hitCollider,
        List<LagCompensationDebugShape> output)
    {
        output.Clear();
        if (!TryGetRuntime(body, out BodyRuntime runtime) ||
            !runtime.Apply(tick, captureIntervalTicks))
            return;

        runtime.CopyProxyDebugShapes(hitCollider, output);
    }

    internal void CaptureCurrentDebugShapes(
        LagCompensatedBody body,
        Collider hitCollider,
        List<LagCompensationDebugShape> output)
    {
        output.Clear();
        if (TryGetRuntime(body, out BodyRuntime runtime))
            runtime.CopyCurrentDebugShapes(hitCollider, output);
    }

    private bool TryInitializeServer()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || !networkManager.IsServer)
            return false;

        proxyLayer = LayerMask.NameToLayer(config.RewindProxyLayerName);
        if (proxyLayer < 0)
        {
            Debug.LogError(
                $"Create a Unity layer named '{config.RewindProxyLayerName}' before starting lag compensation.",
                this);
            enabled = false;
            return false;
        }

        proxyMask = 1 << proxyLayer;
        uint tickRate = networkManager.NetworkConfig.TickRate;
        captureIntervalTicks = config.GetCaptureIntervalTicks(tickRate);
        historyTicks = Mathf.Max(1, Mathf.CeilToInt(config.HistoryDurationSeconds * tickRate));
        rewindHits = new RaycastHit[config.RaycastBufferSize];
        proxyContainer = new GameObject("LagCompensationProxies");
        // Keep proxies grouped under this system without inheriting its pose.
        proxyContainer.transform.SetParent(transform, true);
        proxyContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        SetWorldScale(proxyContainer.transform, Vector3.one);
        proxyContainer.SetActive(false);

        if (config.EnableDebugDraw)
        {
            GameObject debugObject = new("LagCompensationDebug");
            debugObject.transform.SetParent(transform, false);
            debugDrawer = debugObject.AddComponent<LagCompensationDebugDrawer>();
            debugDrawer.Initialize(this, config, tickRate);
        }

        initialized = true;
        currentTick = (uint)networkManager.NetworkTickSystem.ServerTime.Tick;
        SubscribeTick();

        foreach (LagCompensatedBody body in LagCompensationWorld.Bodies)
            Register(body);

        return true;
    }

    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        Vector3 parentScale = target.parent != null
            ? target.parent.lossyScale
            : Vector3.one;
        target.localScale = new Vector3(
            Mathf.Abs(parentScale.x) > Mathf.Epsilon ? worldScale.x / parentScale.x : worldScale.x,
            Mathf.Abs(parentScale.y) > Mathf.Epsilon ? worldScale.y / parentScale.y : worldScale.y,
            Mathf.Abs(parentScale.z) > Mathf.Epsilon ? worldScale.z / parentScale.z : worldScale.z);
    }

    private void SubscribeTick()
    {
        if (tickSubscribed) return;
        networkManager.NetworkTickSystem.Tick += HandleNetworkTick;
        tickSubscribed = true;
    }

    private void UnsubscribeTick()
    {
        if (!tickSubscribed) return;
        if (networkManager != null && networkManager.NetworkTickSystem != null)
            networkManager.NetworkTickSystem.Tick -= HandleNetworkTick;
        tickSubscribed = false;
    }

    private void HandleNetworkTick()
    {
        if (!initialized || networkManager == null || !networkManager.IsServer)
            return;

        currentTick = (uint)networkManager.NetworkTickSystem.ServerTime.Tick;
        if (hasCapturedTick && currentTick == lastCapturedTick)
            return;
        if (currentTick % (uint)captureIntervalTicks != 0)
            return;

        foreach (BodyRuntime runtime in activeBodies.Values)
            runtime.Capture(currentTick, captureIntervalTicks, false);

        PruneRetiredBodies();
        lastCapturedTick = currentTick;
        hasCapturedTick = true;
    }

    private BodyRuntime CreateRuntime(LagCompensatedBody body)
    {
        IReadOnlyList<LagCompensatedColliderNode> definitions = body.Nodes;
        if (definitions == null || definitions.Count == 0)
        {
            Debug.LogWarning(
                $"{body.name} has no offline rewind collider tree. Rebuild it in the inspector.",
                body);
            return null;
        }

        GameObject root = new GameObject($"Rewind_{body.name}");
        root.layer = proxyLayer;
        root.transform.SetParent(proxyContainer.transform, false);
        var nodes = new List<ProxyNode>(definitions.Count);

        for (int i = 0; i < definitions.Count; i++)
        {
            LagCompensatedColliderNode definition = definitions[i];
            if (definition == null || definition.source == null)
                continue;

            GameObject nodeObject = new GameObject($"{i}_{definition.source.name}");
            nodeObject.layer = proxyLayer;
            nodeObject.transform.SetParent(root.transform, false);
            nodeObject.transform.localPosition = definition.rootLocalPosition;
            nodeObject.transform.localRotation = definition.rootLocalRotation;
            nodeObject.transform.localScale = definition.rootLocalScale;

            Collider proxyCollider = CloneCollider(definition.source, nodeObject);
            if (proxyCollider == null)
            {
                Destroy(nodeObject);
                continue;
            }

            proxyCollider.isTrigger = true;
            var node = new ProxyNode(
                definition.source,
                definition.source.GetComponent<Hitbox>(),
                nodeObject.transform,
                proxyCollider);
            nodes.Add(node);
        }

        if (nodes.Count == 0)
        {
            Destroy(root);
            Debug.LogWarning($"{body.name} has no supported rewind colliders.", body);
            return null;
        }

        var runtime = new BodyRuntime(
            body,
            body.GetComponent<Actor>(),
            root,
            nodes.ToArray(),
            config.GetHistoryCapacity(networkManager.NetworkConfig.TickRate));

        for (int i = 0; i < runtime.Nodes.Length; i++)
        {
            ProxyNode node = runtime.Nodes[i];
            bindings[node.ProxyCollider] = new ProxyBinding(
                body,
                body.BodyType,
                node.SourceCollider,
                node.SourceHitbox);
        }

        return runtime;
    }

    private static Collider CloneCollider(Collider source, GameObject target)
    {
        switch (source)
        {
            case BoxCollider box:
            {
                BoxCollider clone = target.AddComponent<BoxCollider>();
                clone.center = box.center;
                clone.size = box.size;
                return clone;
            }
            case SphereCollider sphere:
            {
                SphereCollider clone = target.AddComponent<SphereCollider>();
                clone.center = sphere.center;
                clone.radius = sphere.radius;
                return clone;
            }
            case CapsuleCollider capsule:
            {
                CapsuleCollider clone = target.AddComponent<CapsuleCollider>();
                clone.center = capsule.center;
                clone.radius = capsule.radius;
                clone.height = capsule.height;
                clone.direction = capsule.direction;
                return clone;
            }
            default:
                return null;
        }
    }

    private bool TryRaycastRewind(
        uint rewindTick,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        int collisionMask,
        LagCompensatedBody ignoredBody,
        out RaycastHit closestHit,
        out ProxyBinding closestBinding)
    {
        closestHit = default;
        closestBinding = default;
        bool found = false;
        float closestDistance = maxDistance;

        foreach (BodyRuntime runtime in activeBodies.Values)
            runtime.Apply(rewindTick, captureIntervalTicks);
        for (int i = 0; i < retiredBodies.Count; i++)
            retiredBodies[i].Apply(rewindTick, captureIntervalTicks);

        proxyContainer.SetActive(true);
        try
        {
            Physics.SyncTransforms();
            int count = Physics.defaultPhysicsScene.Raycast(
                origin,
                direction,
                rewindHits,
                maxDistance,
                proxyMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = rewindHits[i];
                if (candidate.distance >= closestDistance ||
                    !bindings.TryGetValue(candidate.collider, out ProxyBinding binding) ||
                    binding.SourceCollider == null ||
                    (collisionMask & (1 << binding.SourceCollider.gameObject.layer)) == 0 ||
                    binding.Body == ignoredBody)
                    continue;

                closestDistance = candidate.distance;
                closestHit = candidate;
                closestBinding = binding;
                found = true;
            }
        }
        finally
        {
            proxyContainer.SetActive(false);
        }

        return found;
    }

    private void PruneRetiredBodies()
    {
        for (int i = retiredBodies.Count - 1; i >= 0; i--)
        {
            BodyRuntime runtime = retiredBodies[i];
            if (TickDifference(currentTick, runtime.RetiredTick) <= historyTicks)
                continue;

            retiredBodies.RemoveAt(i);
            DisposeRuntime(runtime);
        }
    }

    private bool TryGetRuntime(
        LagCompensatedBody body,
        out BodyRuntime runtime)
    {
        if (body != null && activeBodies.TryGetValue(body, out runtime))
            return true;

        for (int i = 0; i < retiredBodies.Count; i++)
        {
            BodyRuntime candidate = retiredBodies[i];
            if (candidate.Body != body) continue;
            runtime = candidate;
            return true;
        }

        runtime = null;
        return false;
    }

    private void DisposeAllBodies()
    {
        foreach (BodyRuntime runtime in activeBodies.Values)
            DisposeRuntime(runtime);
        activeBodies.Clear();

        for (int i = 0; i < retiredBodies.Count; i++)
            DisposeRuntime(retiredBodies[i]);
        retiredBodies.Clear();
        bindings.Clear();
        initialized = false;
    }

    private void DisposeRuntime(BodyRuntime runtime)
    {
        for (int i = 0; i < runtime.Nodes.Length; i++)
            bindings.Remove(runtime.Nodes[i].ProxyCollider);
        if (runtime.ProxyRoot != null)
            Destroy(runtime.ProxyRoot);
    }

    private static int TickDifference(uint current, uint previous)
    {
        return unchecked((int)(current - previous));
    }

    private readonly struct ProxyBinding
    {
        public readonly LagCompensatedBody Body;
        public readonly LagCompensatedBodyType BodyType;
        public readonly Collider SourceCollider;
        public readonly Hitbox SourceHitbox;

        public ProxyBinding(
            LagCompensatedBody body,
            LagCompensatedBodyType bodyType,
            Collider sourceCollider,
            Hitbox sourceHitbox)
        {
            Body = body;
            BodyType = bodyType;
            SourceCollider = sourceCollider;
            SourceHitbox = sourceHitbox;
        }
    }

    private sealed class ProxyNode
    {
        public readonly Collider SourceCollider;
        public readonly Hitbox SourceHitbox;
        public readonly Transform ProxyTransform;
        public readonly Collider ProxyCollider;

        public ProxyNode(
            Collider sourceCollider,
            Hitbox sourceHitbox,
            Transform proxyTransform,
            Collider proxyCollider)
        {
            SourceCollider = sourceCollider;
            SourceHitbox = sourceHitbox;
            ProxyTransform = proxyTransform;
            ProxyCollider = proxyCollider;
        }
    }

    private struct NodePose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Enabled;
    }

    private sealed class HistoryFrame
    {
        public uint Tick;
        public bool Exists;
        public Vector3 RootPosition;
        public Quaternion RootRotation;
        public Vector3 RootScale;
        public readonly NodePose[] Nodes;

        public HistoryFrame(int nodeCount)
        {
            Nodes = new NodePose[nodeCount];
        }
    }

    private sealed class BodyRuntime
    {
        private readonly HistoryFrame[] history;
        private readonly Actor actor;
        private int nextIndex;
        private int count;

        public readonly LagCompensatedBody Body;
        public readonly LagCompensatedBodyType BodyType;
        public readonly GameObject ProxyRoot;
        public readonly ProxyNode[] Nodes;
        public uint RetiredTick;

        public BodyRuntime(
            LagCompensatedBody body,
            Actor actor,
            GameObject proxyRoot,
            ProxyNode[] nodes,
            int historyCapacity)
        {
            Body = body;
            this.actor = actor;
            BodyType = body.BodyType;
            ProxyRoot = proxyRoot;
            Nodes = nodes;
            history = new HistoryFrame[historyCapacity];
            for (int i = 0; i < history.Length; i++)
                history[i] = new HistoryFrame(nodes.Length);
        }

        public void Capture(uint tick, int intervalTicks, bool force)
        {
            if (Body == null) return;
            if (!force && BodyType == LagCompensatedBodyType.Pickup && !HasPickupChanged())
                return;

            HistoryFrame latest = Latest;
            if (BodyType == LagCompensatedBodyType.Pickup && latest != null &&
                TickDifference(tick, latest.Tick) > intervalTicks)
            {
                WriteCopy(latest, tick - (uint)intervalTicks);
            }

            WriteCurrent(tick);
        }

        public void CaptureInactive(uint tick)
        {
            HistoryFrame frame = NextFrame();
            frame.Tick = tick;
            frame.Exists = false;
        }

        public bool Apply(uint tick, int intervalTicks)
        {
            if (!TryFindFrames(tick, out HistoryFrame previous, out HistoryFrame next))
            {
                ProxyRoot.SetActive(false);
                return false;
            }

            HistoryFrame selected = previous;
            float t = 0f;
            int gap = TickDifference(next.Tick, previous.Tick);
            if (next != previous && gap > 0)
            {
                int elapsed = TickDifference(tick, previous.Tick);
                bool canInterpolate = BodyType == LagCompensatedBodyType.Hitbox ||
                                      gap <= intervalTicks * 2;
                if (canInterpolate)
                    t = Mathf.Clamp01(elapsed / (float)gap);
                else if (TickDifference(tick, next.Tick) >= 0)
                    selected = next;
            }

            if (!previous.Exists || !next.Exists)
            {
                selected = next != previous && TickDifference(tick, next.Tick) >= 0
                    ? next
                    : previous;
                t = 0f;
            }

            if (!selected.Exists)
            {
                ProxyRoot.SetActive(false);
                return false;
            }

            ProxyRoot.SetActive(true);
            if (BodyType == LagCompensatedBodyType.Pickup)
            {
                ApplyRoot(previous, next, selected, t);
                ApplyPickupNodes(selected);
            }
            else
            {
                ApplyHitboxNodes(previous, next, selected, t);
            }
            return true;
        }

        private void WriteCurrent(uint tick)
        {
            HistoryFrame frame = NextFrame();
            frame.Tick = tick;
            frame.Exists = Body != null && Body.isActiveAndEnabled && Body.gameObject.activeInHierarchy;
            if (!frame.Exists) return;

            Transform root = Body.transform;
            frame.RootPosition = root.position;
            frame.RootScale = root.lossyScale;
            frame.RootRotation =
                BodyType == LagCompensatedBodyType.Hitbox &&
                actor?.rootPoseSystem != null
                    ? actor.rootPoseSystem.AuthoritativePose.Rotation
                    : root.rotation;
            for (int i = 0; i < Nodes.Length; i++)
            {
                ProxyNode node = Nodes[i];
                Collider source = node.SourceCollider;
                bool sourceEnabled = source != null && source.enabled && source.gameObject.activeInHierarchy;
                frame.Nodes[i].Enabled = sourceEnabled;
                if (BodyType == LagCompensatedBodyType.Pickup || source == null)
                    continue;

                Transform sourceTransform = source.transform;
                frame.Nodes[i].Position = sourceTransform.position;
                frame.Nodes[i].Rotation = sourceTransform.rotation;
                frame.Nodes[i].Scale = sourceTransform.lossyScale;
            }
        }

        private bool HasPickupChanged()
        {
            HistoryFrame latest = Latest;
            if (latest == null) return true;

            bool exists = Body != null && Body.isActiveAndEnabled && Body.gameObject.activeInHierarchy;
            if (latest.Exists != exists) return true;
            if (!exists) return false;

            Transform root = Body.transform;
            if ((root.position - latest.RootPosition).sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(root.rotation, latest.RootRotation) > 0.01f ||
                (root.lossyScale - latest.RootScale).sqrMagnitude > 0.00000001f)
                return true;

            for (int i = 0; i < Nodes.Length; i++)
            {
                Collider source = Nodes[i].SourceCollider;
                bool enabled = source != null && source.enabled && source.gameObject.activeInHierarchy;
                if (latest.Nodes[i].Enabled != enabled)
                    return true;
            }
            return false;
        }

        private void WriteCopy(HistoryFrame source, uint tick)
        {
            HistoryFrame target = NextFrame();
            target.Tick = tick;
            target.Exists = source.Exists;
            target.RootPosition = source.RootPosition;
            target.RootRotation = source.RootRotation;
            target.RootScale = source.RootScale;
            Array.Copy(source.Nodes, target.Nodes, source.Nodes.Length);
        }

        private HistoryFrame NextFrame()
        {
            HistoryFrame frame = history[nextIndex];
            nextIndex = (nextIndex + 1) % history.Length;
            count = Mathf.Min(count + 1, history.Length);
            return frame;
        }

        private HistoryFrame Latest => count == 0
            ? null
            : history[(nextIndex - 1 + history.Length) % history.Length];

        private bool TryFindFrames(
            uint tick,
            out HistoryFrame previous,
            out HistoryFrame next)
        {
            previous = null;
            next = null;
            int oldest = (nextIndex - count + history.Length) % history.Length;
            for (int i = 0; i < count; i++)
            {
                HistoryFrame frame = history[(oldest + i) % history.Length];
                int relative = TickDifference(tick, frame.Tick);
                if (relative >= 0)
                    previous = frame;
                if (relative <= 0)
                {
                    next = frame;
                    break;
                }
            }

            if (previous == null) return false;
            next ??= previous;
            return true;
        }

        public bool TryGetRootPose(
            uint tick,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale)
        {
            position = default;
            rotation = Quaternion.identity;
            scale = Vector3.one;
            if (!TryFindFrames(tick, out HistoryFrame previous, out HistoryFrame next) ||
                !previous.Exists)
                return false;

            int gap = TickDifference(next.Tick, previous.Tick);
            if (next != previous && next.Exists && gap > 0)
            {
                float t = Mathf.Clamp01(TickDifference(tick, previous.Tick) / (float)gap);
                position = Vector3.Lerp(previous.RootPosition, next.RootPosition, t);
                rotation = Quaternion.Slerp(previous.RootRotation, next.RootRotation, t);
                scale = Vector3.Lerp(previous.RootScale, next.RootScale, t);
                return true;
            }

            position = previous.RootPosition;
            rotation = previous.RootRotation;
            scale = previous.RootScale;
            return true;
        }

        public void CopyProxyDebugShapes(
            Collider hitCollider,
            List<LagCompensationDebugShape> output)
        {
            if (!ProxyRoot.activeSelf) return;
            for (int i = 0; i < Nodes.Length; i++)
            {
                ProxyNode node = Nodes[i];
                if (!node.ProxyCollider.enabled ||
                    !LagCompensationDebugShape.TryCreate(
                        node.ProxyCollider,
                        node.SourceCollider == hitCollider,
                        out LagCompensationDebugShape shape))
                    continue;
                output.Add(shape);
            }
        }

        public void CopyCurrentDebugShapes(
            Collider hitCollider,
            List<LagCompensationDebugShape> output)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                ProxyNode node = Nodes[i];
                Collider source = node.SourceCollider;
                if (source == null || !source.enabled ||
                    !source.gameObject.activeInHierarchy ||
                    !LagCompensationDebugShape.TryCreate(
                        source,
                        source == hitCollider,
                        out LagCompensationDebugShape shape))
                    continue;
                output.Add(shape);
            }
        }

        private void ApplyRoot(
            HistoryFrame previous,
            HistoryFrame next,
            HistoryFrame selected,
            float t)
        {
            Transform root = ProxyRoot.transform;
            if (t > 0f)
            {
                root.SetPositionAndRotation(
                    Vector3.Lerp(previous.RootPosition, next.RootPosition, t),
                    Quaternion.Slerp(previous.RootRotation, next.RootRotation, t));
                root.localScale = Vector3.Lerp(previous.RootScale, next.RootScale, t);
            }
            else
            {
                root.SetPositionAndRotation(selected.RootPosition, selected.RootRotation);
                root.localScale = selected.RootScale;
            }
        }

        private void ApplyPickupNodes(HistoryFrame selected)
        {
            for (int i = 0; i < Nodes.Length; i++)
                Nodes[i].ProxyCollider.enabled = selected.Nodes[i].Enabled;
        }

        private void ApplyHitboxNodes(
            HistoryFrame previous,
            HistoryFrame next,
            HistoryFrame selected,
            float t)
        {
            Transform root = ProxyRoot.transform;
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.localScale = Vector3.one;
            for (int i = 0; i < Nodes.Length; i++)
            {
                ProxyNode node = Nodes[i];
                NodePose pose = selected.Nodes[i];
                node.ProxyCollider.enabled = pose.Enabled;
                if (!pose.Enabled) continue;

                if (t > 0f)
                {
                    NodePose a = previous.Nodes[i];
                    NodePose b = next.Nodes[i];
                    node.ProxyTransform.SetPositionAndRotation(
                        Vector3.Lerp(a.Position, b.Position, t),
                        Quaternion.Slerp(a.Rotation, b.Rotation, t));
                    node.ProxyTransform.localScale = Vector3.Lerp(a.Scale, b.Scale, t);
                }
                else
                {
                    node.ProxyTransform.SetPositionAndRotation(pose.Position, pose.Rotation);
                    node.ProxyTransform.localScale = pose.Scale;
                }
            }
        }

        private static int TickDifference(uint current, uint previous)
        {
            return unchecked((int)(current - previous));
        }
    }
}
