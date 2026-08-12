using System;
using Unity.Netcode;
using UnityEngine;

public class InteractSystem
{
    private readonly Actor actor;
    private readonly InteractSO config;
    private bool active = true;
    private IRayInteractable displayed;
    private NetworkObject displayedNetworkObject;

    public float RayShowDistance => config != null ? config.RayShowDistance : 0f;
    public float RayInteractDistance => config != null ? config.RayInteractDistance : 0f;

    public InteractSystem(Actor actor, InteractSO config)
    {
        this.actor = actor;
        this.config = config;
    }

    public void Tick()
    {
        if (!active || actor == null || !actor.IsOwner || !actor.IsClient || config == null)
        {
            ClearDisplayed();
            return;
        }

        Transform camera = ActorCameraController.Instance?.OutputTransform;
        if (camera == null || RayShowDistance <= 0f)
        {
            ClearDisplayed();
            return;
        }

        IRayInteractable next = null;
        NetworkObject nextNetworkObject = null;
        RaycastHit info = default;
        if (TryGetRayHit(camera.position, camera.forward, out info))
        {
            IRayInteractable entry = info.collider.GetComponentInParent<IRayInteractable>();
            if (entry != null && entry.CanShow(actor))
            {
                next = entry;
                nextNetworkObject = info.collider.GetComponentInParent<NetworkObject>();
            }
        }

        if (displayed != next)
        {
            if (displayed != null)
                displayed.OnLookExit(actor);

            displayed = next;
            displayedNetworkObject = nextNetworkObject;
            if (displayed != null)
                displayed.OnLookEnter(actor);
        }

        if (displayed == null || displayedNetworkObject == null)
            return;

        if (info.distance <= RayInteractDistance &&
            displayed.CanInteract(actor) &&
            actor.runTimeData.Input.WasPressed(InputButtons.InputInteract))
        {
            Debug.Log("try interact");
            actor.RequestInteract(displayedNetworkObject);
        }
    }

    private void ClearDisplayed()
    {
        if (displayed != null)
            displayed.OnLookExit(actor);

        displayed = null;
        displayedNetworkObject = null;
    }

    private bool TryGetRayHit(Vector3 origin, Vector3 direction, out RaycastHit closestHit)
    {
        closestHit = default;
        LayerMask layerMask = config != null ? config.InteractRayLayer : Physics.DefaultRaycastLayers;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            RayShowDistance,
            layerMask,
            QueryTriggerInteraction.Ignore);

        // RaycastAll 的返回顺序没有契约，必须显式按射线距离排序。
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (IsControlledActorHit(hit.collider))
                continue;

            // 第一个非自身碰撞体就是射线的最终命中物。
            // 如果它不是交互物，后面的物体也不能穿过它被选中。
            closestHit = hit;
            return true;
        }

        return false;
    }

    private bool IsControlledActorHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        return hitCollider.GetComponentInParent<Actor>() == actor;
    }
}
