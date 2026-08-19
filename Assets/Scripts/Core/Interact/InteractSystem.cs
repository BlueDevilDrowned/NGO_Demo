using System;
using UnityEngine;

public sealed class InteractSystem : IActorOwnershipSystem
{
    private readonly Actor actor;
    private readonly InteractSO config;
    private readonly RaycastHit[] hitBuffer=new RaycastHit[32];

    private IRayInteractable displayed;
    private bool isDisposed;

    public InteractSystem(Actor actor,InteractSO config)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        this.config=config;
        actor.RegisterSystem(this);
    }
    /// <summary>
    ///客户端表现层，主要处理能否交互等提示信息
    /// </summary>
    public void PresentationUpdate()
    {
        if(isDisposed||!actor.IsClient||!actor.IsOwner||config==null)
        {
            ClearDisplayed();
            return;
        }

        Transform camera=actor.cameraSystem.rig?.OutputTransform;
        if(camera==null)
        {
            ClearDisplayed();
            return;
        }

        IRayInteractable next=null;
        if(TryRaycast(
           camera.position,
           camera.forward,
           config.RayShowDistance,
           out RaycastHit hit))
        {
            IRayInteractable candidate=
                hit.collider.GetComponentInParent<IRayInteractable>();
            if(candidate!=null&&candidate.CanShow(actor))
                next=candidate;
        }

        SetDisplayed(next);
    }
    /// <summary>
    /// 服务器交互层，主要负责真是交互上判断能否交互，并执行交互逻辑
    /// </summary>
    public void ServerTick()
    {
        if(isDisposed||!actor.IsServer||config==null||
           !actor.simulation.inputData.WasPressed(InputButtons.InputInteract))
            return;

        ActorCameraData camera=actor.simulation.cameraData;
        if(!IsValidServerView(in camera))return;
        if(!TryRaycast(
           camera.ViewOrigin,
           camera.ViewDirection,
           config.RayInteractDistance,
           out RaycastHit hit))
            return;

        IRayInteractable target=
            hit.collider.GetComponentInParent<IRayInteractable>();
        if(target!=null&&target.CanInteract(actor))
            target.OnInteractServer(actor);
    }
    /// <summary>
    /// 依旧是忽略自身
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <param name="hit"></param>
    /// <returns></returns>
    private bool TryRaycast(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit hit)
    {
        return ActorRaycastUtility.TryRaycastIgnoringActor(
            origin,
            direction,
            distance,
            config.InteractRayLayer,
            QueryTriggerInteraction.Ignore,
            actor,
            hitBuffer,
            out hit);
    }

    private bool IsValidServerView(in ActorCameraData camera)
    {
        if(!IsFinite(camera.ViewOrigin)||!IsFinite(camera.ViewDirection)||
           camera.ViewDirection.sqrMagnitude<=0.000001f)
            return false;

        Vector3 reference=actor.cameraPivot!=null
            ?actor.cameraPivot.position
            :actor.transform.position;
        float maxOffset=config.MaxViewOriginOffset;
        return (camera.ViewOrigin-reference).sqrMagnitude<=maxOffset*maxOffset;
    }
    /// <summary>
    /// 判断看到的物体是否更换，并执行离开进入逻辑
    /// </summary>
    /// <param name="next"></param>
    private void SetDisplayed(IRayInteractable next)
    {
        if(ReferenceEquals(displayed,next))return;

        displayed?.OnLookExit(actor);
        displayed=next;
        displayed?.OnLookEnter(actor);
    }

    private void ClearDisplayed()
    {
        SetDisplayed(null);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }

    public void OnGainedOwnership()
    {
        ClearDisplayed();
    }

    public void OnLostOwnership()
    {
        ClearDisplayed();
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        ClearDisplayed();
    }
}
