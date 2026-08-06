using UnityEngine;
using System;

public class GraviteModule
{
    private Actor actor;
    private ActorMovement movement;
    public float verticalVelocity;
    private float CurrentGravite;
    public GraviteModule(Actor actor,ActorMovement movement)
    {
        this.actor=actor!=null
            ?actor
            :throw new ArgumentNullException(nameof(actor));
        this.movement=movement??
            throw new ArgumentNullException(nameof(movement));
    }
    public void GraviteTick()
    {
        CurrentGravite=actor.controllerSO.Gravite;
        if(actor.characterController.isGrounded)
        {
            verticalVelocity=actor.controllerSO.GroundedVelocity;
            return;
        }
        //否则按速度更新
        if(verticalVelocity>0)CurrentGravite=actor.controllerSO.Gravite*actor.controllerSO.UpFactor;
        else if(verticalVelocity<0)
        {
            CurrentGravite=actor.controllerSO.Gravite*actor.controllerSO.FallFactor;
        }
        //再额外判断是否处于最高点
        if(verticalVelocity>-actor.controllerSO.HoldSpeed&&verticalVelocity<actor.controllerSO.HoldSpeed)CurrentGravite=actor.controllerSO.Gravite*actor.controllerSO.HoldFactor;
        

        //作用到速度
        verticalVelocity+=CurrentGravite*TickTime.deltaTime;
        if(verticalVelocity<actor.controllerSO.MaxfallSpeed)verticalVelocity=actor.controllerSO.MaxfallSpeed;
    }
    public void GraviteSumbit()
    {

        MovementRequest request=MovementRequest.Default;
        request.Source="Gravite";
        request.WorldPositionDelta=Vector3.up*verticalVelocity*TickTime.deltaTime;
        movement.Submit(request);
    }
}
