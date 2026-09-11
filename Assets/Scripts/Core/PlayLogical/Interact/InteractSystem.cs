using System;
using UnityEngine;
using System.Collections.Generic;

public sealed class InteractSystem : IActorOwnershipSystem
{
    private readonly Actor actor;
    private readonly InteractSO config;
    private readonly RaycastHit[] hitBuffer=new RaycastHit[32];

    private IRayInteractable displayed;
    private readonly List<ItemInteractionOption> options = new();
    private int selectedOption;
    public IReadOnlyList<ItemInteractionOption> CurrentOptions => options;
    public int SelectedOptionIndex => selectedOption;
    public string SelectedOptionId => selectedOption >= 0 && selectedOption < options.Count
        ? options[selectedOption].Id : string.Empty;

    public void PrepareInput(ref ActorInputData input)
    {
        if(!actor.IsOwner || isDisposed) return;
        if(displayed is WorldItemPickup pickup && pickup.IsSpawned && options.Count>0)
        {
            if(Mathf.Abs(input.InputScroll.y)>0.01f) CycleOption(input.InputScroll.y>0 ? -1 : 1);
            input.InputScroll=Vector2.zero;
            input.InteractionTarget=pickup.NetworkObjectId;
            input.InteractionOption=SelectedOptionId;
        }
    }
    private bool isDisposed;
    private InteractionOptionsUI ui;

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

        ActorCameraData camera=actor.cameraSystem.data;
        if(!IsFinite(camera.ViewOrigin)||
           !IsFinite(camera.ViewDirection)||
           camera.ViewDirection.sqrMagnitude<=0.000001f)
        {
            ClearDisplayed();
            return;
        }

        IRayInteractable next=null;
        if(TryRaycast(
           camera.ViewOrigin,
           camera.ViewDirection,
           config.RayShowDistance,
           out RaycastHit hit))
        {
            IRayInteractable candidate=
                hit.collider.GetComponentInParent<IRayInteractable>();
            if(candidate!=null&&candidate.CanShow(actor))
                next=candidate;
        }

        SetDisplayed(next);
        if(ui==null && options.Count>0) ui=InteractionOptionsUI.Create(this);
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
        if(target==null || !target.CanInteract(actor)) return;
        if(target is WorldItemPickup pickup)
        {
            var input=actor.simulation.inputData;
            if(pickup.NetworkObjectId!=input.InteractionTarget || input.InteractionOption.IsEmpty) return;
            pickup.ExecuteInteraction(input.InteractionOption.ToString(),actor);
        }
        else target.OnInteractServer(actor);
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

        Vector3 reference=actor.firstCameraPivot!=null
            ?actor.firstCameraPivot.position
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
        if(ReferenceEquals(displayed,next))
        {
            RefreshOptions();
            return;
        }

        displayed?.OnLookExit(actor);
        displayed=next;
        displayed?.OnLookEnter(actor);
        options.Clear();
        selectedOption = 0;
        RefreshOptions();
    }

    private void RefreshOptions()
    {
        string previous=SelectedOptionId;
        options.Clear();
        if(displayed is IInteractionOptionProvider provider)
            options.AddRange(provider.GetInteractionOptions(actor));
        selectedOption=0;
        for(int i=0;i<options.Count;i++)
            if(options[i].Id==previous) { selectedOption=i; break; }
    }

    public void CycleOption(int direction)
    {
        if (options.Count == 0) return;
        selectedOption = (selectedOption + direction % options.Count + options.Count) % options.Count;
    }

    private void ClearDisplayed()
    {
        SetDisplayed(null);
        if(ui!=null) UnityEngine.Object.Destroy(ui.gameObject);
        ui=null;
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
