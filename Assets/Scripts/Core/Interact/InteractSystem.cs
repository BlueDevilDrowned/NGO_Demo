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

    /// <summary>
    /// 每帧调用的更新方法，用于处理射线交互的检测和显示
    /// </summary>
    public void Tick()
    {
        // 检查是否满足交互条件：激活状态、拥有者身份、客户端身份、配置存在
        if (!active || actor == null || !actor.IsOwner || !actor.IsClient || config == null)
        {
            ClearDisplayed(); // 如果条件不满足，清除当前显示内容
            return;
        }

        // 获取相机变换对象，并检查相机是否存在且射线显示距离有效
        Transform camera = ActorCameraController.Instance?.OutputTransform;
        if (camera == null || RayShowDistance <= 0f)
        {
            ClearDisplayed(); // 如果相机无效或距离无效，清除当前显示内容
            return;
        }

        // 初始化下一个交互对象和网络对象
        IRayInteractable next = null;
        NetworkObject nextNetworkObject = null;
        RaycastHit info = default;
        // 尝试获取射线碰撞信息
        if (TryGetRayHit(camera.position, camera.forward, out info))
        {
            // 获取碰撞体上的交互接口组件，并检查是否可以显示
            IRayInteractable entry = info.collider.GetComponentInParent<IRayInteractable>();
            if (entry != null && entry.CanShow(actor))
            {
                next = entry;
                nextNetworkObject = info.collider.GetComponentInParent<NetworkObject>();
            }
        }

        // 如果当前显示的对象发生变化，处理进入和退出事件
        if (displayed != next)
        {
            if (displayed != null)
                displayed.OnLookExit(actor); // 旧对象退出时的处理

            displayed = next; // 更新当前显示对象
            displayedNetworkObject = nextNetworkObject; // 更新网络对象
            if (displayed != null)
                displayed.OnLookEnter(actor); // 新对象进入时的处理
        }

        // 如果没有显示对象或网络对象，直接返回
        if (displayed == null || displayedNetworkObject == null)return;

        // 检查是否满足交互条件：距离在交互范围内、可以交互、按下交互按钮
        if (info.distance <= RayInteractDistance &&
            displayed.CanInteract(actor) &&
            actor.runTimeData.Input.WasPressed(InputButtons.InputInteract))
        {
            Debug.Log("try interact"); // 输出交互尝试日志
            actor.RequestInteract(displayedNetworkObject); // 请求交互
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
