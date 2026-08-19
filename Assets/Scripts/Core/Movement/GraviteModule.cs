using UnityEngine;
using System;

public class GraviteModule
{
    private Actor actor;
    private bool hasGroundSample;
    private bool wasGrounded;

    public float verticalVelocity;
    private float CurrentGravite;
    public bool IsGrounded{get;private set;}
    public bool JustLanded{get;private set;}
    public float LastImpactVelocityY{get;private set;}
    public float LastImpactSpeed=>Mathf.Max(0f,-LastImpactVelocityY);
    public GraviteModule(Actor actor)
    {
        this.actor=actor!=null
            ?actor
            :throw new ArgumentNullException(nameof(actor));
    }

    public void BeginTick()
    {
        bool grounded=
            actor.characterController.isGrounded&&
            verticalVelocity<=0f;

        JustLanded=hasGroundSample&&grounded&&!wasGrounded;
        if(JustLanded)
            LastImpactVelocityY=verticalVelocity;

        hasGroundSample=true;
        wasGrounded=grounded;
        IsGrounded=grounded;

        if(IsGrounded)
            verticalVelocity=actor.actorSO.controllerSO.GroundedVelocity;
    }

    public void GraviteTick()
    {
        CurrentGravite=actor.actorSO.controllerSO.Gravite;
        // 起跳位移发生前 CharacterController 仍会报告接地，不能覆盖向上速度。
        if(IsGrounded&&verticalVelocity<=0f)
        {
            verticalVelocity=actor.actorSO.controllerSO.GroundedVelocity;
            return;
        }
        //否则按速度更新
        if(verticalVelocity>0)CurrentGravite=actor.actorSO.controllerSO.Gravite*actor.actorSO.controllerSO.UpFactor;
        else if(verticalVelocity<0)
        {
            CurrentGravite=actor.actorSO.controllerSO.Gravite*actor.actorSO.controllerSO.FallFactor;
        }
        //再额外判断是否处于最高点
        if(verticalVelocity>-actor.actorSO.controllerSO.HoldSpeed&&verticalVelocity<actor.actorSO.controllerSO.HoldSpeed)CurrentGravite=actor.actorSO.controllerSO.Gravite*actor.actorSO.controllerSO.HoldFactor;
        

        //作用到速度
        verticalVelocity+=CurrentGravite*TickTime.deltaTime;
        if(verticalVelocity<actor.actorSO.controllerSO.MaxfallSpeed)verticalVelocity=actor.actorSO.controllerSO.MaxfallSpeed;
    }
}
